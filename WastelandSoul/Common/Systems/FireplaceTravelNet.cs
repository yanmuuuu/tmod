using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WastelandSoul.Content.Subworlds;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 壁炉门 / 返回门的**联机版服务端权威进出**，以及"首次成型"在联机下的收尾。
	///
	/// <para/>=== 为什么必须有一个专门的网络层（本文件存在的理由） ===
	/// 旧写法是客户端右键门时**直接反射调** <c>SubworldSystem.Enter&lt;FireplaceSubworld&gt;()</c>。
	/// 这条路在专用服务端上根本不成立：反编译 SubworldLibrary 2.3.0.1 后确认，
	/// <c>SubworldSystem.BeginEntering(int index)</c> 的第一段 IL 就是
	/// <c>if (Main.netMode == 2) return;</c> —— 服务端自己不加载子世界、也不通知任何人，
	/// 于是"进门"这件事在联机里没有任何权威方，客户端各进各的。
	///
	/// <para/>=== 核对过的 SLL 2.3.0.1 公开签名（Cecil 读 DLL，不是凭印象） ===
	/// <code>
	/// public static void MovePlayerToSubworld(string id, int player);
	/// public static void MovePlayerToSubworld&lt;T&gt;(int player);      // ← 本文件用这个
	/// public static void MovePlayerToMainWorld(int player);
	/// public static bool IsActive&lt;T&gt;();
	/// public static Subworld Current { get; }
	/// </code>
	/// 三者都**只在非 MultiplayerClient 一侧才是"权威调用"**（IL 开头就是
	/// <c>if (netMode == 1 || (netMode == 2 &amp;&amp; current != null)) return;</c>）：
	/// <list type="bullet">
	/// <item><b>专用服务端</b>（<c>netMode == 2</c>）上 <c>current</c> 恒为 <c>null</c> ——
	/// 主世界服务端从不变身成子世界（子世界的生成发生在**另一个进程**：
	/// <c>StartSubserver</c> 里 <c>Process.Start(tModLoader.dll -server -subworld N)</c>），
	/// 所以守卫不成立，方法会真的执行：置 <c>pendingMoves[whoAmI]</c> → 给该客户端发
	/// SLL 自己的 256 号包（客户端据此 <c>current = 目标子世界</c> 并重连到子世界服务端）
	/// → <c>StartSubserver</c> 起子世界进程。</item>
	/// <item><b>联机客户端</b>（<c>netMode == 1</c>）上这三个方法全被守卫挡掉（直接 return），
	/// 所以客户端只能发请求包，这就是本文件存在的意义。</item>
	/// <item><b>单人</b>（<c>netMode == 0</c>）走 <c>BeginEntering</c>，也就是原来那条路 ——
	/// 单机行为**一点都不改**（见 <see cref="FireplaceTravel"/>）。</item>
	/// </list>
	///
	/// <para/>=== 为什么 host &amp; play 仍然进不去（不是本轮没做，是前置不支持） ===
	/// host &amp; play 的宿主客户端 <c>netMode == 0</c>，调 <c>MovePlayerToSubworld</c> 会走
	/// <c>BeginEntering</c> → <c>ExitWorldCallBack</c> → <c>LoadWorld</c>，也就是**让共享进程
	/// 自己变身子世界**：主世界会被卸载、同服的其它玩家一起被赶下车。
	/// 而走"当服务端请求"这条路也不行：SLL 的子世界进程是用
	/// <c>Process.Start(tModLoader.dll -server …)</c> 起的独立进程，宿主客户端**没法连到它**
	/// （<c>SubserverLink</c> 只会把 SLL 自己的 256 号包发给真正的 socket 客户端）。
	/// 所以这里对"宿主本地玩家"保持旧路径（单人式直接进入），联机权威只对
	/// <c>whoAmI != Main.myPlayer</c> 的远端玩家成立 —— 这一点写进注释，免得下次又当成 bug 修。
	///
	/// <para/>=== "首次成型"（<see cref="MassFormed"/>）在联机下的事实依据 ===
	/// 核过 IL：<c>SubworldSystem.LoadWorld()</c> 在 <c>netMode == 1</c>（客户端）时**只**做
	/// <c>current = … / menuMode = 10 / gameMenu = true</c>，然后 <c>ExitWorldCallBack</c> 里
	/// <c>if (netMode != 1) LoadWorld();</c> —— **客户端根本不读也不生成世界**，世界完全由
	/// 子世界服务端生成完再通过网络发过来。生成是同步的：<c>LoadSubworld()</c> 跑完六个
	/// <c>GenPass</c> 才轮到 <c>LoadWorld()</c> 末尾 <c>QueueMainThreadAction(SpawnPlayer)</c>。
	/// 结论：**联机客户端连接到的子世界服务端，世界已经生成完整**，单机那次"新生成没刷新、
	/// 必须出去再进一次"的路径在联机里不存在（单机是本地进程自己生成，<c>OnLoad()</c> 早于
	/// <c>LoadWorld()</c>，所以看得见半成品 —— 这也解释了为什么单机要补一次重载）。
	///
	/// <para/>因此联机这一侧**不做**"进一次就自动重载"的自动状态机（那会给无病的世界白加
	/// 两次加载、还会连累队友），只留两条**有闸门**的兜底：
	/// <list type="number">
	/// <item>客户端只有真的观察到"本机是本次新生成"（<c>FireplaceSubworld.GeneratedFresh</c>，
	/// 它在客户端上被前置的 <c>netMode != 1</c> 判定压成 false）才会请求一次重组；</item>
	/// <item>服务端把"已成型"写成**世界级、随存档保存与同步**的标志 <see cref="MassFormed"/>，
	/// 每个玩家**每个存档只重组一次**，而且**先置位再动手**。</item>
	/// </list>
	/// 两条都不成立时（正常联机）本文件不产生任何进出，玩家体验与单人一致。
	/// </summary>
	public class FireplaceTravelNet : ModSystem
	{
		// ==================================================================================
		// 自定义消息类型
		//
		// 模组只有 WastelandSoul.HandlePacket 这一个入口，所有包都在那里按第一个字节分发：
		//   kind 1 = WastelandStorySystem 的剧情同步（**不要动它的编号**）
		//   kind 2 = 本文件（WastelandStorySystem.PacketKindFireplaceTravel），内部再分：
		//            0 = 客户端请求进壁炉 / 1 = 客户端请求返回 / 2 = 客户端请求成型
		//            3 = 服务端"开始成型（或已成型）" / 4 = 服务端拒绝（带原因键）
		// 详见 FireplaceTravelNet.TravelProtocol。
		// ==================================================================================

		/// <summary>
		/// 壁炉门"附近"的判定半径（格，平方比较）。
		/// 16 格足够宽：右键是**服务端收到包之后**才校验的，这中间隔着一个往返延迟，
		/// 玩家可能已经在跑动了；再近一点就会出现"我明明点了门却被拒"的误伤。
		/// 校验的目的只是挡住**明显的伪造包**（不想进门的玩家没必要伪造），
		/// 所以宁可宽一点也不能挡真人。
		/// </summary>
		private const int GateReachTiles = 16;

		/// <summary>本客户端每次"重新进入壁炉"最多试几次（防呆，正常 1 次就够）。</summary>
		private const int MaxReformEnterAttempts = 2;

		// ==================================================================================
		// 世界级状态（只允许服务端写）
		// ==================================================================================

		/// <summary>
		/// **本存档的壁炉世界已经成型**（曾经完整生成过一次）。
		/// 随世界存档保存、也随 <see cref="SyncMassFormed"/> 联机同步；客户端只读。
		/// "只做一次"就是靠它 + <see cref="reformDone"/> 两道闸门。
		/// </summary>
		public static bool MassFormed { get; private set; }

		/// <summary>
		/// 某个玩家这次重组走到哪一步。**只在服务端用**（不参与同步 —— 客户端不需要知道
		/// 别人的进度）。键是**服务端的** <c>whoAmI</c>（玩家槽位）。
		/// </summary>
		private enum ReformStep
		{
			/// <summary>已经把他移回主世界，等他落地后再送他回壁炉。</summary>
			WaitReturn,

			/// <summary>已经再次送进壁炉，等他进去（或超时）。</summary>
			WaitReenter
		}

		private sealed class ReformState
		{
			public ReformStep Step;
			public int Timer;
		}

		/// <summary>服务端：正在进行重组的玩家（键 = 服务端玩家槽位 whoAmI）。</summary>
		private static readonly Dictionary<int, ReformState> reformDone = new Dictionary<int, ReformState>();

		/// <summary>WaitReturn 阶段的最长等待（tick）。够一次主世界加载：15 秒。</summary>
		private const int ReformReturnTimeout = 60 * 15;

		/// <summary>WaitReenter 阶段的最长等待（tick）。子世界已经生成好，读档十几秒足够。</summary>
		private const int ReformReenterTimeout = 60 * 15;

		/// <summary>两次请求之间至少隔这么久（tick）。防"手抖连点右键"打出连击。</summary>
		private const int ReformCooldown = 60;

		// ==================================================================================
		// 进出流程（由 Content\Tiles\FireplaceTiles.cs 里的壁炉门 / 返回门调用）
		// ==================================================================================

		/// <summary>
		/// 右键**主世界的壁炉门**。
		/// <list type="bullet">
		/// <item>单人：直接走原来的 <c>SubworldSystem.Enter&lt;T&gt;()</c> 反射路径（行为不变）；</item>
		/// <item>联机客户端：只发一个"我要进壁炉"的请求包，由服务端移动 —— 这就是服务端权威；</item>
		/// <item>服务端本地（host &amp; play 宿主）：走单人式直接进入（见类注释里的解释）。</item>
		/// </list>
		/// </summary>
		public static void RequestEnter()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				SendEnterRequest();
				return;
			}

			FireplaceTravel.Enter();
		}

		/// <summary>
		/// 右键**壁炉里的返回门**。
		/// <list type="bullet">
		/// <item>单人：直接 <c>Exit()</c>（行为不变）；</item>
		/// <item>联机客户端：发请求包，服务端用 <c>MovePlayerToMainWorld(whoAmI)</c> 送回；</item>
		/// <item>服务端本地：直接 <c>Exit()</c>。</item>
		/// </list>
		/// </summary>
		public static void RequestExit()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				SendExitRequest();
				return;
			}

			FireplaceTravel.Exit();
		}

		/// <summary>客户端 → 服务端：我要进壁炉。</summary>
		private static void SendEnterRequest()
		{
			string error = TravelProtocol.SendEnterRequest();

			if (!string.IsNullOrEmpty(error)) {
				ModLoader.GetMod("WastelandSoul").Logger.Warn("FireplaceTravelNet: 进入请求发送失败：" + error);
				ShowLocal("Mods.WastelandSoul.Messages.FireplaceEnterFailed", new Color(220, 160, 90));
				return;
			}

			// 包已经发出去了。真正的"进去"由服务端决定，所以这里只报"正在过去"，
			// 绝不能提前喊"已进入"（服务端可能因为没开门/太远而拒绝）。
			ShowLocal("Mods.WastelandSoul.Messages.FireplaceEntering", new Color(180, 210, 255));
		}

		/// <summary>客户端 → 服务端：我要回主世界。</summary>
		private static void SendExitRequest()
		{
			string error = TravelProtocol.SendExitRequest();

			if (!string.IsNullOrEmpty(error)) {
				ModLoader.GetMod("WastelandSoul").Logger.Warn("FireplaceTravelNet: 返回请求发送失败：" + error);
				ShowLocal("Mods.WastelandSoul.Messages.FireplaceEnterFailed", new Color(220, 160, 90));
			}
		}

		// ==================================================================================
		// 收包
		// ==================================================================================

		/// <summary>
		/// 处理壁炉相关的包（**已经把"大类型"字节读掉之后**由
		/// <see cref="WastelandNet.ReceivePacket"/> 转进来）。
		/// 服务端与客户端分成两条完全不同的路：服务端只受理请求，客户端只收状态。
		///
		/// <para/>字段账本（与 <see cref="TravelProtocol.Send"/> / <see cref="SendToClient"/> 成对）：
		/// <list type="bullet">
		/// <item>0 = 进壁炉请求：inner(1)，无负载；</item>
		/// <item>1 = 返回请求：inner(1)，无负载；</item>
		/// <item>2 = 成型请求：inner(1) + int32 requestKind（**4 字节**，不是 1）；</item>
		/// <item>3 = 服务端开始成型：inner(1) + bool MassFormed + int32 whoAmI + string reason；</item>
		/// <item>4 = 服务端拒绝：inner(1) + bool MassFormed + int32 whoAmI + string reason。</item>
		/// </list>
		/// </summary>
		public static void ReceiveInnerPacket(NetReader reader)
		{
			if (!reader.TryReadByte("travel.inner", out byte kind)) {
				return;
			}

			WastelandNet.LogIn(WastelandStorySystem.PacketKindFireplaceTravel, kind, reader.Consumed, reader.RemainingAtStart);

			if (Main.netMode == NetmodeID.Server) {
				ReceiveOnServer(kind, reader);
				return;
			}

			ReceiveOnClient(kind, reader);
		}

		/// <summary>服务端收：客户端只允许请求，**不允许**它自己写任何世界状态。</summary>
		private static void ReceiveOnServer(byte kind, NetReader reader)
		{
			if (kind == TravelProtocol.KindEnter) {
				ServerHandleEnter(reader.WhoAmI);
				return;
			}

			if (kind == TravelProtocol.KindExit) {
				ServerHandleExit(reader.WhoAmI);
				return;
			}

			if (kind == TravelProtocol.KindReform) {
				if (reader.TryReadInt32("travel.reformKind", out int requestedKind)) {
					HandleReformRequest(reader.WhoAmI, requestedKind);
				}

				return;
			}

			// 其它 kind 一律忽略（前向兼容：老客户端多发一个字段也不会打崩服务端）。
			// ⚠️ 但**必须把这一段剩下的字节跳过**：tModLoader 会核对"读完的字节数 == 本段字节数"，
			// 少读一个字节就抛 Read underflow（玩家实测的崩溃就是这个）。
			NetLog.Warn("FireplaceTravelNet: 服务端收到未知的壁炉包内层类型 " + kind + "，跳过剩余 "
				+ reader.Remaining + " 字节（客户端与服务端版本可能不一致）");
			reader.SkipRemaining();
		}

		/// <summary>客户端收：只更新"只读"的显示状态，并推进本机的重组节奏。</summary>
		private static void ReceiveOnClient(byte kind, NetReader reader)
		{
			if (kind == TravelProtocol.KindReformBegin || kind == TravelProtocol.KindReject) {
				if (!reader.TryReadBool("travel.massFormed", out bool massFormed)
					|| !reader.TryReadInt32("travel.whoAmI", out int target)
					|| !reader.TryReadString("travel.reason", out string reason)) {
					return;
				}

				if (massFormed) {
					// 服务端告诉我们"已经成型"，这是世界级真值 —— 客户端只读地收下
					MassFormed = true;
				}

				if (kind == TravelProtocol.KindReject) {
					// 拒绝可能在**任何**阶段到达：进门请求被拒（还没有 sequence），
					// 或者成型重组中途被拒（此时 sequence 是 WaitMainWorld / WaitReenter）。
					// 两种都必须把本机状态清干净 —— 否则客户端会一直等一个永远不会来的"移出/移入"，
					// 玩家的表现就是"点了门没反应，之后再也进不去"（必须报出来，不能静默卡住）。
					bool wasWaiting = sequence != Sequence.Idle;

					sequence = Sequence.Idle;
					reenterAttempts = 0;

					NetLog.Warn("FireplaceTravelNet: 服务端拒绝了本次请求（原因键：" + reason + "），本机状态已复位"
						+ (wasWaiting ? "（原本正在等待成型重组）" : string.Empty));
					ShowRejectReason(reason);
					return;
				}

				// 服务端开始动手：客户端接下来只负责"等自己回到主世界，再请求进一次"
				sequence = Sequence.WaitMainWorld;
				NetLog.Info("FireplaceTravelNet: 服务端已开始成型重组（目标玩家槽位 " + target + "），本机转入等待主世界");
				return;
			}

			// 客户端不该收到进 / 出请求，收到就当没看见（防伪造包影响本机状态）
			NetLog.Warn("FireplaceTravelNet: 客户端收到不该收到的壁炉包内层类型 " + kind + "，已忽略");
			reader.SkipRemaining();
		}

		// ==================================================================================
		// 服务端：进 / 出 的校验与移动
		// ==================================================================================

		/// <summary>
		/// 服务端校验"这个玩家可以进壁炉吗"，可以就移动他。
		/// <para/>服务端权威的三条：① 门开了没有；② 人是不是真在门附近；③ 他现在是不是已经
		/// 在某个子世界里（在就说明状态没对齐，不动，免得两层世界互相覆盖）。
		/// </summary>
		internal static void ServerHandleEnter(int whoAmI)
		{
			if (!ValidatePlayer(whoAmI, out Player player, out string reason)) {
				Reject(whoAmI, reason);
				return;
			}

			if (!WastelandStorySystem.fireplaceOpened) {
				Reject(whoAmI, "Mods.WastelandSoul.Messages.FireplaceNotOpen");
				return;
			}

			// 已经在子世界里：拒绝。SLL 的 MovePlayerToSubworld 自己有 pendingMoves 去重，
			// 但"已经在别的子世界里还去点主世界的门"属于状态错乱，不如明确拒绝。
			if (SubworldSystem.Current != null) {
				Reject(whoAmI, "Mods.WastelandSoul.Messages.FireplaceTravelBusy");
				return;
			}

			if (!NearGate(player)) {
				Reject(whoAmI, "Mods.WastelandSoul.Messages.FireplaceTooFar");
				return;
			}

			if (!TravelProtocol.MoveToSubworld(whoAmI, out string error)) {
				ModLoader.GetMod("WastelandSoul").Logger.Warn(
					"FireplaceTravelNet: MovePlayerToSubworld 调用失败：" + error);
				Reject(whoAmI, "Mods.WastelandSoul.Messages.FireplaceEnterFailed");
			}
		}

		/// <summary>服务端：把这个玩家送回主世界。</summary>
		internal static void ServerHandleExit(int whoAmI)
		{
			if (!ValidatePlayer(whoAmI, out _, out string reason)) {
				Reject(whoAmI, reason);
				return;
			}

			if (!TravelProtocol.MoveToMainWorld(whoAmI, out string error)) {
				ModLoader.GetMod("WastelandSoul").Logger.Warn(
					"FireplaceTravelNet: MovePlayerToMainWorld 调用失败：" + error);
				Reject(whoAmI, "Mods.WastelandSoul.Messages.FireplaceEnterFailed");
			}
		}

		/// <summary>服务端共用的玩家校验（越界 / 死亡 / 宿主本地玩家）。</summary>
		private static bool ValidatePlayer(int whoAmI, out Player player, out string reason)
		{
			// 默认原因给"这条路正忙"：越界 / 玩家不存在 / 已死 / 宿主本地玩家
			// 对玩家来说都是"这次没成，再试一次"的意思，给一句能看懂的话就够了
			// （真正消歧的原因由调用方用更准确的键覆盖，例如没开门 / 太远）。
			reason = "Mods.WastelandSoul.Messages.FireplaceTravelBusy";
			player = null;

			if (whoAmI < 0 || whoAmI >= Main.maxPlayers) {
				return false;
			}

			player = Main.player[whoAmI];

			if (player == null || !player.active) {
				return false;
			}

			if (player.dead) {
				return false;
			}

			// 专用服务端的 Main.myPlayer 是 255，所以这一条只在 host & play 里会命中：
			// 宿主玩家的移动必须走本地 Enter()/Exit()（见类注释：SLL 起的是独立子世界进程，
			// 宿主客户端连不上去）。远端玩家的 whoAmI 永远不等于 Main.myPlayer。
			if (whoAmI == Main.myPlayer) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// 玩家是不是真的站在壁炉门附近（包可以被伪造，位置不能）。
		///
		/// <para/>主世界现在有**两扇**门（左海 / 右海，见 <c>FireplaceGateSystem</c>），
		/// 所以两组坐标任意一组命中都算过 —— 只认其中一组的话，从另一侧海洋进门的人会被误判成"太远"。
		/// </summary>
		private static bool NearGate(Player player)
		{
			bool hasRecord = HasGateRecord(WastelandStorySystem.GateX, WastelandStorySystem.GateY)
				|| HasGateRecord(WastelandStorySystem.GateX2, WastelandStorySystem.GateY2);

			// 两组坐标都没记录（老存档 / 还没同步到位）：位置这条校验就跳过，不能因此把人挡在门外
			if (!hasRecord) {
				return true;
			}

			return NearPoint(player, WastelandStorySystem.GateX, WastelandStorySystem.GateY)
				|| NearPoint(player, WastelandStorySystem.GateX2, WastelandStorySystem.GateY2);
		}

		/// <summary>这一组门坐标是不是有效（记过门的位置；0 是"没记过"的哨兵值）。</summary>
		private static bool HasGateRecord(int gx, int gy)
		{
			return gx > 0 && gy > 0;
		}

		/// <summary>玩家是不是在这一个点周围 <see cref="GateReachTiles"/> 格内。</summary>
		private static bool NearPoint(Player player, int gx, int gy)
		{
			if (!HasGateRecord(gx, gy)) {
				return false;
			}

			float dx = player.Center.X / 16f - gx;
			float dy = player.Center.Y / 16f - gy;

			return dx * dx + dy * dy <= GateReachTiles * GateReachTiles;
		}

		/// <summary>失败一定要给玩家一句明确的话（不能静默什么都不发生）。</summary>
		private static void Reject(int whoAmI, string key)
		{
			SendToClient(whoAmI, TravelProtocol.KindReject, key);
		}

		/// <summary>
		/// 发一条"服务端 → 该客户端"的状态包（带当前已成型标志与可选原因键）。
		///
		/// <para/>字段表（与 <see cref="ReceiveOnClient"/> 逐字段对应）：
		/// <c>kind(1) + inner(1) + bool MassFormed + int32 whoAmI + string reason</c>。
		/// </summary>
		private static void SendToClient(int whoAmI, byte kind, string reason)
		{
			if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers) {
				return;
			}

			ModPacket packet = WastelandNet.NewPacket();

			if (packet == null) {
				return;
			}

			NetWriter writer = new NetWriter(packet);
			writer.WriteByte(WastelandStorySystem.PacketKindFireplaceTravel);
			writer.WriteByte(kind);
			writer.WriteBool(MassFormed);
			writer.WriteInt32(whoAmI);
			writer.WriteString(reason ?? string.Empty);
			packet.Send(whoAmI, -1);

			WastelandNet.AssertBytes("壁炉状态包（发给玩家 " + whoAmI + "）",
				WastelandStorySystem.PacketKindFireplaceTravel, 7 + WireTextBytes(reason), writer.Count);
		}

		/// <summary>
		/// 字符串在线上占的字节数（7 位长度前缀 + UTF-8 内容），
		/// 供 <see cref="WastelandNet.AssertBytes"/> 的"人工登记期望值"使用。
		/// </summary>
		private static int WireTextBytes(string text)
		{
			int content = System.Text.Encoding.UTF8.GetByteCount(text ?? string.Empty);
			int prefix = 1;

			for (int v = content; v >= 0x80; v >>= 7) {
				prefix++;
			}

			return prefix + content;
		}

		// ==================================================================================
		// 服务端：首次成型（每个玩家每个存档只做一次）
		// ==================================================================================

		/// <summary>
		/// 客户端说"我这一趟是新生成，请求成型"。
		///
		/// <para/>校验与"只做一次"：
		/// <list type="number">
		/// <item><b>先置位</b>：一旦决定要做，立刻把世界级标志置上并同步 ——
		/// 哪怕后面的移动失败、玩家半路跑掉，也绝不会再来第二次（宁可这次白做，
		/// 也不能把玩家反复踢出世界）；</item>
		/// <item>每个玩家 key 一个 <see cref="reformDone"/> 条目，做到收工前不会被重复受理；</item>
		/// <item>不在服务端上与 <c>SubworldSystem.Current</c> 较劲：主世界服务端的 <c>current</c>
		/// 恒为 <c>null</c>（子世界在独立进程里），这里只用它挡"我自己都在子世界里"这种怪状态。</item>
		/// </list>
		/// </summary>
		private static void HandleReformRequest(int whoAmI, int requestedKind)
		{
			if (!ValidatePlayer(whoAmI, out Player _, out string reason)) {
				Reject(whoAmI, reason);
				return;
			}

			if (requestedKind != FireplaceTravelNet.RequestKindClientObservedFresh) {
				return;
			}

			if (reformDone.ContainsKey(whoAmI)) {
				return;
			}

			if (SubworldSystem.Current != null) {
				Reject(whoAmI, "Mods.WastelandSoul.Messages.FireplaceTravelBusy");
				return;
			}

			// ① 先置位再动手（世界级标志：随存档保存 + 立刻同步给所有客户端）
			bool firstTime = !MassFormed;

			if (firstTime) {
				MassFormed = true;
				SyncMassFormed();
			}

			// 第二次以后的请求：世界早就成型了，直接告诉他"已经好了"，不要再折腾他
			if (!firstTime) {
				ModLoader.GetMod("WastelandSoul").Logger.Info(
					"FireplaceTravelNet: 壁炉本存档早已成型，忽略玩家 " + whoAmI + " 的重复请求");
				SendToClient(whoAmI, TravelProtocol.KindReformBegin, null);
				return;
			}

			ModLoader.GetMod("WastelandSoul").Logger.Info(string.Format(
				"FireplaceTravelNet: 玩家 {0} 报告壁炉本次新生成，服务端开始一次「移出 → 移入」（本存档仅此一次）",
				whoAmI));

			reformDone[whoAmI] = new ReformState {
				Step = ReformStep.WaitReturn,
				Timer = ReformReturnTimeout
			};

			if (!TravelProtocol.MoveToMainWorld(whoAmI, out string error)) {
				ModLoader.GetMod("WastelandSoul").Logger.Warn(
					"FireplaceTravelNet: 成型重组的「移出」失败：" + error);
				reformDone.Remove(whoAmI);
			}
		}

		/// <summary>服务端告诉该客户端"我开始动手了"（顺带把已成型标志再同步一次）。</summary>
		private static void NotifyClient(int whoAmI, byte kind)
		{
			SendToClient(whoAmI, kind, null);
		}

		/// <summary>服务端推进"移出 → 移入"的两段等待（只服务端跑）。</summary>
		private static void UpdateServerReform()
		{
			if (reformDone.Count == 0) {
				return;
			}

			List<int> finished = null;

			foreach (KeyValuePair<int, ReformState> pair in reformDone) {
				int whoAmI = pair.Key;
				ReformState state = pair.Value;

				if (whoAmI < 0 || whoAmI >= Main.maxPlayers || !Main.player[whoAmI].active) {
					// 人没了（退出 / 掉线）：直接收工，别留一个再也不会推进的条目
					(finished ??= new List<int>()).Add(whoAmI);
					continue;
				}

				if (state.Step == ReformStep.WaitReturn) {
					// 判"他是不是真的回到主世界落地了"：服务端这一侧玩家重新 active，
					// 而且这个 socket 已经回到 State == 10（SLL 把人移出去时会把它改成 1，
					// 重连回主世界走完流程才会回到 10）。比看 Main.gameMenu 准。
					if (IsClientStateInWorld(whoAmI)) {
						state.Step = ReformStep.WaitReenter;
						state.Timer = ReformReenterTimeout;

						if (!TravelProtocol.MoveToSubworld(whoAmI, out string error)) {
							NetLog.Warn("FireplaceTravelNet: 成型重组的「移入」失败：" + error);
							// ⚠️ 必须走 Reject（带原因键）而不是 NotifyClient(KindReject, null)：
							// 后者会让客户端收到一条**空原因**的拒绝，玩家只会看到"什么都没发生"。
							Reject(whoAmI, "Mods.WastelandSoul.Messages.FireplaceEnterFailed");
							(finished ??= new List<int>()).Add(whoAmI);
						}

						// 本 tick 不再扣计时器：刚换的阶段要从完整的超时开始数
						continue;
					}
				}

				if (--state.Timer <= 0) {
					ModLoader.GetMod("WastelandSoul").Logger.Warn(
						"FireplaceTravelNet: 成型重组等待超时（玩家 " + whoAmI + "），放弃本步（不影响正常进出）");
					(finished ??= new List<int>()).Add(whoAmI);
				}
			}

			if (finished != null) {
				foreach (int whoAmI in finished) {
					reformDone.Remove(whoAmI);
				}
			}
		}

		/// <summary>
		/// 这个客户端现在是不是"已经在世界里"。
		/// 用 <c>Netplay.Clients[whoAmI].State == 10</c>：SLL 把玩家移出去时会把
		/// <c>State</c> 改成 1（连接中），重连回主世界走完流程才会回到 10。
		/// 这正是"他是不是真的已经回到主世界落地了"的判据，比看 <c>Main.gameMenu</c> 更准。
		/// </summary>
		private static bool IsClientStateInWorld(int whoAmI)
		{
			try {
				if (Terraria.Netplay.Clients == null || whoAmI >= Terraria.Netplay.Clients.Length) {
					return true;
				}

				Terraria.RemoteClient client = Terraria.Netplay.Clients[whoAmI];

				return client != null && client.State == 10;
			}
			catch (Exception) {
				// 问不出来就别卡住玩家：当成"已回主世界"，让流程往下走（最坏等于立刻再送进去）
				return true;
			}
		}

		// ==================================================================================
		// 客户端：兜底的一次重组
		// ==================================================================================

		/// <summary>客户端本次请求的语义版本号（服务端只认这个值，防老客户端乱请求）。</summary>
		public const int RequestKindClientObservedFresh = 1;

		private enum Sequence
		{
			/// <summary>什么都不做。</summary>
			Idle,

			/// <summary>已经发出请求，等服务端把我移出壁炉。</summary>
			WaitMainWorld,

			/// <summary>已经回到主世界，准备请求再进去一次。</summary>
			WaitReenter
		}

		private static Sequence sequence = Sequence.Idle;
		private static int reenterTimer;
		private static int reenterCooldown;
		private static int reenterAttempts;

		/// <summary>
		/// 客户端：只有"本机真的看到这次是新生成"时才请求一次成型。
		/// <para/>⚠️ 闸门是 <see cref="FireplaceSubworld.GeneratedFresh"/>，它在客户端上被前置
		/// 的 <c>netMode != 1</c> 判定压成 false（见类注释）。所以正常联机（客户端压根不生成
		/// 世界）时这里**一次都不会触发** —— 这条兜底只在"前置的行为变了 / 客户端真的在本地
		/// 生成过世界"这种窄路上生效，而且服务端还有"每存档只做一次"兜底。
		/// </summary>
		private static void UpdateClientReform()
		{
			if (Main.netMode == NetmodeID.SinglePlayer) {
				return;
			}

			if (reenterCooldown > 0) {
				reenterCooldown--;
			}

			switch (sequence) {
				case Sequence.WaitMainWorld:
					// ⚠️ 先排掉"我还在子世界里"这一种：服务端回"已经成型了、不用动手"时，
					// 客户端会收到 KindReformBegin（它把状态设成 WaitMainWorld），但那种情况下
					// 服务端**什么都不会做**，等下去就是永远等 —— 直接收工。
					if (SubworldSystem.Current != null && !Main.gameMenu) {
						sequence = Sequence.Idle;
						break;
					}

					if (!Main.gameMenu && SubworldSystem.Current == null && Main.LocalPlayer.active) {
						sequence = Sequence.WaitReenter;
						reenterTimer = 60; // 主世界刚落地先稳 1 秒：SLL 的 cache 要等 OnEnterWorld 才对齐
					}
					break;

				case Sequence.WaitReenter:
					if (--reenterTimer > 0) {
						break;
					}

					if (SubworldSystem.Current != null) {
						sequence = Sequence.Idle;
						ShowLocal("Mods.WastelandSoul.Messages.FireplaceFormed", new Color(200, 230, 190));
						break;
					}

					if (reenterAttempts >= MaxReformEnterAttempts) {
						sequence = Sequence.Idle;
						ModLoader.GetMod("WastelandSoul").Logger.Warn(
							"FireplaceTravelNet: 成型重组没能在客户端侧收尾，放弃（玩家可自己再走一次门）");
						break;
					}

					reenterAttempts++;
					SendEnterRequest();
					sequence = Sequence.Idle;
					break;
			}
		}

		/// <summary>
		/// 客户端进入壁炉后调用：如果这次是"本机新生成"，就向服务端请求一次成型。
		/// </summary>
		internal static void ClientObservedFreshFireplace()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				return;
			}

			if (!MassFormed && sequence == Sequence.Idle && reenterCooldown <= 0) {
				sequence = Sequence.WaitMainWorld;
				reenterCooldown = ReformCooldown;
				reenterAttempts = 0;

				int error = TravelProtocol.SendReformRequest(RequestKindClientObservedFresh);

				if (error != 0) {
					sequence = Sequence.Idle;
					ModLoader.GetMod("WastelandSoul").Logger.Warn(
						"FireplaceTravelNet: 成型请求发送失败：" + error);
				}
			}
		}

		// ==================================================================================
		// 模组钩子
		// ==================================================================================

		/// <inheritdoc/>
		public override void ClearWorld()
		{
			MassFormed = false;
			reformDone.Clear();
			ResetWorldTracking();
		}

		/// <inheritdoc/>
		public override void SaveWorldData(TagCompound tag)
		{
			tag["fireplaceMassFormed"] = MassFormed;
		}

		/// <inheritdoc/>
		public override void LoadWorldData(TagCompound tag)
		{
			MassFormed = tag.GetBool("fireplaceMassFormed");
		}

		/// <inheritdoc/>
		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(MassFormed);
		}

		/// <inheritdoc/>
		public override void NetReceive(BinaryReader reader)
		{
			MassFormed = reader.ReadBoolean();
		}

		/// <summary>
		/// 世界级标志变了就同步一次（**只有服务端会调**，客户端拿到的永远是服务端算出来的值）。
		/// 走"开始成型"这一种包：它只带一个 bool，客户端按"已成型"收下。
		/// 只发给客户端（`ignoreClient: -1`），服务端不需要收自己的包。
		///
		/// <para/>字段表（与 <see cref="ReceiveOnClient"/> 的 KindReformBegin 分支逐字段对应）：
		/// <c>kind(1) + inner(1) + bool MassFormed + int32 whoAmI(=0) + string reason(="")</c>
		/// 合计 1 + 1 + 1 + 4 + 1 = 8 字节。
		/// </summary>
		public static void SyncMassFormed()
		{
			if (Main.netMode != NetmodeID.Server) {
				return;
			}

			ModPacket packet = WastelandNet.NewPacket();

			if (packet == null) {
				return;
			}

			NetWriter writer = new NetWriter(packet);
			writer.WriteByte(WastelandStorySystem.PacketKindFireplaceTravel);
			writer.WriteByte(TravelProtocol.KindReformBegin);
			writer.WriteBool(MassFormed);
			writer.WriteInt32(0);
			writer.WriteString(string.Empty);
			packet.Send();

			WastelandNet.AssertBytes("已成型广播 SyncMassFormed()",
				WastelandStorySystem.PacketKindFireplaceTravel, 8, writer.Count);
		}

		/// <summary>
		/// 每世界 tick 推进两边各自的节奏。
		///
		/// <para/>⚠️ 联机自检实测（本轮）：**空载的专用服务端（一个客户端都没连）根本不会调
		/// 这个钩子** —— <c>Netplay.HasClients</c> 为 false 时 tModLoader 的世界更新整段跳过
		/// （那次自检里 <c>server.log</c> 一直等到 "Listening on port" 之后 30 秒都没有任何
		/// 世界 tick 输出）。这不是 bug，也不影响进出：成型重组本来就是**由客户端请求触发**
		/// 的，而客户端能请求就说明人已经连上了，那时世界 tick 是跑着的。
		/// 反过来记一笔：不要把"需要服务端权威做、且与服务端无客户端时也要发生"的逻辑挂在这里。
		/// </summary>
		public override void PostUpdateWorld()
		{
			if (Main.netMode == NetmodeID.Server) {
				UpdateServerReform();
				return;
			}

			UpdateClientReform();
		}

		/// <summary>
		/// 离开一个世界（进子世界 / 回主世界 / 回菜单）就把本机的兜底节奏清干净。
		///
		/// <para/>⚠️ **只在 unload 里清，不在 <c>OnWorldLoad</c> 里清**（这一步有真原因）：
		/// 客户端的兜底重组是"进门时观察到新生成 → 记下 <see cref="Sequence.WaitMainWorld"/>
		/// 并请求服务端"；而进子世界时 SLL 的 <c>ExitWorldCallBack</c> 里**先后会依次**调
		/// <c>SystemLoader.OnWorldUnload()</c>（卸载主世界）和 <c>Subworld.OnEnter()</c>，
		/// 客户端真正落进子世界时 tModLoader 还会再走一次"世界加载完成"。
		/// 如果 on-load 也清，就会把刚记下的 <c>WaitMainWorld</c> 清掉 —— 那服务端把人移回主世界之后
		/// 客户端就再也不会请求"再进一次"，兜底整条失效（这是本轮静态复核时发现的一个真坑）。
		/// 只清 unload 就对了：进任何世界之前必然先离开上一个世界。
		/// </summary>
		public override void OnWorldUnload()
		{
			ResetWorldTracking();
		}

		/// <summary>
		/// 进出子世界（或回菜单）就把本机的兜底状态清干净。
		/// <para/>⚠️ 这里**不动 <see cref="MassFormed"/>**：它是世界级真值，由存档与同步决定，
		/// 客户端/服务端都不允许"因为换了世界"就把它清掉。只清本机的节奏状态。
		/// </summary>
		private static void ResetWorldTracking()
		{
			sequence = Sequence.Idle;
			reenterAttempts = 0;
			reenterCooldown = 0;
		}

		/// <summary>本机玩家在本地显示一句话（服务端上什么都不做）。</summary>
		internal static void ShowLocal(string key, Color color)
		{
			if (Main.dedServ || Main.netMode == NetmodeID.Server) {
				return;
			}

			Main.NewText(Language.GetTextValue(key), color);
		}

		/// <summary>
		/// 收包时把服务端给的拒绝原因显示出来。放在这里是为了让
		/// <see cref="ReceiveOnClient"/> 只关心协议，不关心 UI。
		/// </summary>
		private static void ShowRejectReason(string key)
		{
			if (string.IsNullOrEmpty(key)) {
				return;
			}

			ShowLocal(key, new Color(220, 160, 90));
		}

		// ==================================================================================
		// 协议实现（收包/发包都集中在这里，方便和 SubworldLibrary 的 API 一起看）
		// ==================================================================================

		/// <summary>
		/// 进出请求 / 成型请求的收包与 SubworldLibrary API 包装。
		/// 包格式见类顶部"自定义消息类型"那一段。
		/// </summary>
		internal static class TravelProtocol
		{
			public const byte KindEnter = 0;
			public const byte KindExit = 1;
			public const byte KindReform = 2;
			public const byte KindReformBegin = 3;
			public const byte KindReject = 4;

			/// <summary>发"我要进壁炉"。返回错误说明（空 = 发出去了）。</summary>
			public static string SendEnterRequest()
			{
				return Send(KindEnter, false, 0);
			}

			/// <summary>发"我要回主世界"。</summary>
			public static string SendExitRequest()
			{
				return Send(KindExit, false, 0);
			}

			/// <summary>发"请求成型"。返回 0 = 成功，否则是错误码（给日志用）。</summary>
			public static int SendReformRequest(int requestKind)
			{
				string error = Send(KindReform, true, requestKind);

				return string.IsNullOrEmpty(error) ? 0 : 1;
			}

			/// <summary>
			/// 客户端 → 服务端的三种请求。**字段表按 inner 分两种**（这是本轮修掉的第二处不对称）：
			/// <code>
			/// inner 0（进壁炉）：kind(1) + inner(1)                = 2 字节
			/// inner 1（返回）  ：kind(1) + inner(1)                = 2 字节
			/// inner 2（成型）  ：kind(1) + inner(1) + int32(4)     = 6 字节
			/// </code>
			///
			/// <para/>🔴 <b>修前为什么必然崩</b>：旧代码无论 inner 是几都固定写
			/// <c>kind(1) + inner(1) + int32(4) = 6 字节</c>，而服务端
			/// <see cref="ReceiveOnServer"/> 的 0 / 1 号分支**读完 inner 就直接 return**
			/// （只有 2 号才 <c>ReadInt32</c>）。于是进 / 出请求的 4 个字节永远没人读，
			/// tModLoader 的 <c>ModNet.HandleModPacket</c> 立刻抛
			/// <c>IOException: Read underflow 2 of 6 bytes caused by WastelandSoul in HandlePacket</c>
			/// —— 这正是玩家日志 server.log:598 那一条（"2" = 收包侧读了 2 字节，"6" = 本段共 6 字节）。
			/// 而 <c>Main.netMode</c> 的进出请求又恰好是在**进壁炉那一刻**发出的，
			/// 于是紧接着的握手/同步报文也被带偏，玩家的表现就是"网络加载失败、子世界没有方块、被弹回主世界"。
			///
			/// <para/>⚠️ 所以现在的写法是：**写几个字节由"收包侧会不会读"决定**，
			/// 两边共用同一个 <paramref name="withPayload"/> 语义，改一边就必须改另一边。
			/// </summary>
			private static string Send(byte inner, bool withPayload, int payload)
			{
				ModPacket packet = WastelandNet.NewPacket();

				if (packet == null) {
					return "GetPacket 返回 null";
				}

				try {
					NetWriter writer = new NetWriter(packet);
					writer.WriteByte(WastelandStorySystem.PacketKindFireplaceTravel);
					writer.WriteByte(inner);

					if (withPayload) {
						writer.WriteInt32(payload);
					}

					// 人工登记的期望值：
					//   inner 0 / 1（无负载）= 2 字节；inner 2（带 int32 负载）= 6 字节。
					int expected = withPayload ? 6 : 2;

					WastelandNet.AssertBytes("壁炉请求（inner " + inner + "）",
						WastelandStorySystem.PacketKindFireplaceTravel, expected, writer.Count);

					packet.Send();
					return null;
				}
				catch (Exception exception) {
					return exception.Message;
				}
			}

			/// <summary>
			/// 服务端移动一个玩家进壁炉：<c>Main.netMode == 2</c> 时只有这条路能让联机玩家进去
			/// （<c>BeginEntering</c> 在 <c>netMode == 2</c> 直接 return）。
			/// </summary>
			public static bool MoveToSubworld(int whoAmI, out string error)
			{
				error = null;

				try {
					SubworldSystem.MovePlayerToSubworld<FireplaceSubworld>(whoAmI);
					return true;
				}
				catch (Exception exception) {
					error = exception.GetType().Name + ": " + exception.Message;
					return false;
				}
			}

			/// <summary>服务端把玩家送回主世界。</summary>
			public static bool MoveToMainWorld(int whoAmI, out string error)
			{
				error = null;

				try {
					SubworldSystem.MovePlayerToMainWorld(whoAmI);
					return true;
				}
				catch (Exception exception) {
					error = exception.GetType().Name + ": " + exception.Message;
					return false;
				}
			}
		}
	}
}
