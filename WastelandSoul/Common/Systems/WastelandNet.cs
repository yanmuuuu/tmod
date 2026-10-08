using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 模组网络层的**唯一入口**：所有 <c>ModPacket</c> 的读与写都必须走这里。
	///
	/// <para/>===✅ 为什么要有这个文件（本轮联机修 bug 的核心）===
	/// 玩家实测的崩溃是服务端 <c>HandlePacket</c> 里的
	/// <c>System.IO.IOException: Read underflow N of M bytes caused by WastelandSoul</c>。
	/// 反编译 tModLoader 2026.8.3.0 后确认它的判定是（<c>ModNet.HandleModPacket</c>）：
	/// <code>
	/// int start = reader.BaseStream.Position;          // 读完"模组序号"之后的起点
	/// int len   = length - (NetModCount &gt;= 256 ? 2 : 1); // 本模组这段负载的字节数
	/// reader.BaseStream.Length - start == len          // ← 不等就抛 Read underflow
	/// </code>
	/// 也就是说：**收包方读完的字节数必须和发包方写出的字节数完全相等**，
	/// 而且它**不关心**读的是什么类型 —— 少读 1 个字节、多读 1 个字节都会直接抛异常。
	///
	/// <para/>=== 已经抓到的真凶（0.1 版实测 DLL 的 IL 与源码逐字节核对）===
	/// 4 号包（<see cref="WastelandStorySystem.PacketKindPlayerProgress"/>）的**包类型字节**：
	/// <code>
	/// 发包（客户端）：packet.Write(WastelandStorySystem.PacketKindPlayerProgress); // ← 实参是 byte 常量，
	///                                                                             //   但形参推断成 int
	///                 → C# 重载决议选中 BinaryWriter.Write(Int32) → 写出 4 字节
	/// 收包（服务端）：byte kind = reader.ReadByte();                              // ← 只消费 1 字节
	/// </code>
	/// 实测 DLL 的 IL 是 <c>IL_001B: callvirt BinaryWriter::Write</c>（实参 <c>ldc.i4.4</c>），
	/// 与 <c>ReceivePacket</c> 里的 <c>BinaryReader::ReadByte()</c> 对不上 —— 一读一写差 3 字节，
	/// 服务端每收到一次个人进度同步就抛一次 Read underflow，并把这条 socket 的报文流位置打乱，
	/// 于是紧接着的下一段报文也被解析成垃圾（日志里那两条 6 / 9 字节的 underflow 就是这么来的），
	/// 玩家身上的表现就是"网络加载失败 / 子世界没有方块 / 被弹回主世界"。
	///
	/// <para/>=== 从此以后怎么保证不再漂移 ===
	/// <list type="number">
	/// <item>**成对**：每个消息的读与写写在同一段代码里（<see cref="ReceivePacket"/> 的 case 与
	/// <see cref="SendRequest"/> / <see cref="Sync"/>
	/// 等发送方法一一对应），字段顺序、类型、字节数一眼可对；</item>
	/// <item>**显式**：一律用 <c>WriteByte</c>/<c>WriteInt32</c>/<c>WriteBool</c>，绝不再用
	/// <c>packet.Write(x)</c> 这种由重载决议决定宽度的写法（这就是本次的根因）；</item>
	/// <item>**有账本**：<see cref="NetWriter.Count"/> 累计本包**真实写出**的字节，包类型分流时
	/// 用 <see cref="NetReader.NoteKind"/> 记下"读类型字节消耗了几个字节"；
	/// <see cref="DebugLogPackets"/> 打开时会把两端逐条打出来（含 kind / 字节数 / 剩余），
	/// 复现问题时直接看日志即可；</item>
	/// <item>**有兜底**：<see cref="NetReader"/> 的读取方法一律"先看剩余字节够不够"，
	/// 不够就 <c>NetLog.Warn</c> 一条带 kind 与字段名的中文日志并返回 false，
	/// **绝不抛异常**（绝不把 tModLoader 的 HandlePacket 打崩、也绝不打乱报文流）。</item>
	/// </list>
	///
	/// <para/>=== 各包的字段 / 类型 / 字节账本（修后）===
	/// 下表由 <see cref="ReceivePacket"/> 与各发送方法一一对应，改动任意一侧都必须同步改另一侧。
	/// <code>
	/// 1 = StorySync          ：kind(1) + 15×bool + 5×int32 + string
	///                          （12 个剧情标志 + GatePlaced/Gate2Placed/fireplaceSeen；
	///                            endingChoice + GateX/GateY + GateX2/GateY2）
	/// 2 = FireplaceTravel    ：kind(1) + inner(1) [+ 各 inner 自己的负载，见 FireplaceTravelNet]
	/// 3 = StoryRequest       ：kind(1) + action(1) [+ 负载：1/3 无；2 = int32；4 = float×2；5 = byte×2]
	/// 4 = PlayerProgress     ：kind(1) + bool×4 + int32
	/// 5 = FragmentDelivered  ：kind(1) + byte + byte + bool
	/// </code>
	/// </summary>
	public static class WastelandNet
	{
		/// <summary>
		/// 打开后会把每一段收到的/发出的模组包按
		/// <c>kind / action / 真实字节数 / 已用字节数</c> 打一条 <c>NetLog.Debug</c>。
		/// 排查"读少/读多了几个字节"这类问题时把它设成 true 再复现一次即可。
		/// </summary>
		public static bool DebugLogPackets;

		/// <summary>本模组的 <c>Mod</c> 实例（惰性取，别在静态字段初始化里碰 <c>ModLoader</c>）。</summary>
		private static Mod Self => ModContent.GetInstance<global::WastelandSoul.WastelandSoul>();

		/// <summary>
		/// 复刻 tModLoader <c>ModNet.HandleModPacket</c> 里"本模组这段负载有多少字节"的算法：
		/// <code>
		/// int len = length - (NetModCount &gt;= 256 ? 2 : 1);
		/// </code>
		/// 模组序号在 256 个以内占 1 字节、否则占 2 字节（与 <c>HandleModPacket</c> 读序号的方式一致）。
		///
		/// <para/>⚠️ <c>ModNet.netMods</c> 是 **internal** 字段（Cecil 已核，编译期不可见），
		/// 所以这里用反射读"已同步的模组数"；读不到时按"&lt; 256"处理（即 1 字节）。
		/// ⚠️ 目前最外层**没有** <c>length</c> 可用（<c>Mod.HandlePacket</c> 签名里没有），
		/// 所以这个方法暂时只用作"将来能拿到长度时"的换算工具与单元测试参照。
		/// </summary>
		public static int ModPayloadLength(int length)
		{
			int overhead = NetModCount() >= 256 ? 2 : 1;
			int payload = length - overhead;

			return payload < 0 ? 0 : payload;
		}

		private static System.Reflection.FieldInfo netModsField;
		private static bool netModsFieldResolved;

		/// <summary>已同步的模组数量（读不到就按 0 算）。</summary>
		private static int NetModCount()
		{
			try {
				if (!netModsFieldResolved) {
					netModsFieldResolved = true;
					netModsField = typeof(ModNet).GetField("netMods",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
				}

				if (netModsField?.GetValue(null) is Array mods) {
					return mods.Length;
				}
			}
			catch (Exception) {
				// 反射被挡 / 字段改名：一律按"不足 256 个模组"处理，绝不影响收包
			}

			return 0;
		}

		/// <summary>取一条新包。取不到时写一条 WARN 并返回 null（调用方必须判空）。</summary>
		public static ModPacket NewPacket()
		{
			Mod self = Self;

			if (self == null) {
				return null;
			}

			ModPacket packet = self.GetPacket();

			if (packet == null) {
				NetLog.Warn("WastelandNet: GetPacket() 返回 null，本包没有发出");
			}

			return packet;
		}

		/// <summary>
		/// 发一条 <c>kind + action</c> 的请求包（**客户端 → 服务端**单向）。
		///
		/// <para/>对应的收包方是 <see cref="ReceivePacket"/> → <see cref="ReceiveRequest"/>。
		/// 这里写 2 个字节、那边读 2 个字节，多出来的负载由
		/// <paramref name="payload"/> 自己按需写（并且必须在 <see cref="ReceiveRequest"/>
		/// 对应分支里按同样的字段顺序读回来）。
		/// </summary>
		public static bool SendRequest(byte kind, byte action, Action<NetWriter> payload)
		{
			ModPacket packet = NewPacket();

			if (packet == null) {
				return false;
			}

			bool isServer = Main.netMode == NetmodeID.Server;

			try {
				NetWriter writer = new NetWriter(packet);
				writer.WriteByte(kind);
				writer.WriteByte(action);
				payload?.Invoke(writer);
				packet.Send();
				LogOut("请求", kind, action, writer.Count, isServer);
				return true;
			}
			catch (Exception exception) {
				NetLog.Warn("WastelandNet: 请求包发送失败（kind " + kind + " action " + action + "）："
					+ exception.GetType().Name + ": " + exception.Message);
				return false;
			}
		}

		/// <summary>
		/// 收包总入口：读"消息大类型"并分发给对应处理者。
		///
		/// <para/>⚠️ <b>类型字节必须只读 1 个字节</b>：所有发送方都用
		/// <see cref="NetWriter.WriteByte"/> 写类型，这就是本次修掉的根因
		/// （旧代码里 <c>SyncPlayer</c> 用 <c>packet.Write(常量)</c> 写出了 4 字节的 int）。
		/// </summary>
		public static void ReceivePacket(BinaryReader reader, int whoAmI, int remaining)
		{
			NetReader head = new NetReader(reader, remaining, "头部", whoAmI);

			if (!head.TryReadByte("kind", out byte kind)) {
				return;
			}

			LogIn(kind, 0, head.Consumed, remaining);

			switch (kind) {
				case WastelandStorySystem.PacketKindStory:
					WastelandStorySystem.ReceiveStorySync(head);
					break;

				case WastelandStorySystem.PacketKindFireplaceTravel:
					FireplaceTravelNet.ReceiveInnerPacket(head);
					break;

				case WastelandStorySystem.PacketKindStoryRequest:
					ReceiveRequest(head, whoAmI);
					break;

				case WastelandStorySystem.PacketKindPlayerProgress:
					Common.Players.WastelandPlayer.ReceiveProgress(head, whoAmI);
					break;

				case WastelandStorySystem.PacketKindFragmentDelivered:
					WastelandMemorySystem.ReceiveDeliveryAck(head);
					break;

				default:
					NetLog.Warn("WastelandNet: 收到未知的消息大类型 " + kind + "（" + head.Consumed
						+ " 字节已读，本段共 " + remaining + " 字节）——已忽略，请检查客户端与服务端的模组版本是否一致");
					break;
			}
		}

		/// <summary>
		/// 服务端处理一条 <see cref="WastelandStorySystem.PacketKindStoryRequest"/> 请求。
		/// <para/>字段账本（与 <see cref="WastelandStorySystem.RequestDataTerminalRead"/> 等发送方一一对应）：
		/// <list type="bullet">
		/// <item>action 1 = 读终端：无负载；</item>
		/// <item>action 2 = 选结局：int32；</item>
		/// <item>action 3 = fireplaceSeen：无负载；</item>
		/// <item>action 4 = 召唤智械人：float x + float y；</item>
		/// <item>action 5 = 交付碎片：byte bossIndex + byte source。</item>
		/// </list>
		/// </summary>
		private static void ReceiveRequest(NetReader reader, int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server) {
				return;   // 客户端不该收到请求包（防伪造包影响本机状态）
			}

			if (whoAmI < 0 || whoAmI >= Main.maxPlayers) {
				return;
			}

			Player player = Main.player[whoAmI];

			if (player == null || !player.active) {
				return;
			}

			if (!reader.TryReadByte("StoryRequest.action", out byte action)) {
				return;
			}

			LogIn(WastelandStorySystem.PacketKindStoryRequest, action, reader.Consumed, reader.RemainingAtStart);

			switch (action) {
				case WastelandStorySystem.StoryRequest.DataTerminalRead:
					// 无负载：故意**立刻 return**，而不是走共用的收尾 —— 收尾会把本段剩下的字节跳过，
					// 那是给"解析出错的旧客户端"用的宽容路径；正常包不该多出任何字节。
					WastelandStorySystem.MarkDataTerminalRead();
					return;

				case WastelandStorySystem.StoryRequest.EndingChoice:
					if (reader.TryReadInt32("StoryRequest.endingChoice", out int choice)) {
						WastelandStorySystem.ChooseEnding(player, choice);
					}

					return;

				case WastelandStorySystem.StoryRequest.FireplaceSeen:
					WastelandStorySystem.MarkFireplaceSeen();
					return;

				case WastelandStorySystem.StoryRequest.CompanionSummon:
					if (reader.TryReadSingle("StoryRequest.summon.X", out float x)
						&& reader.TryReadSingle("StoryRequest.summon.Y", out float y)) {
						Content.NPCs.Town.MechanicalCompanion.SummonOnServer(new Microsoft.Xna.Framework.Vector2(x, y));
					}

					return;

				case WastelandStorySystem.StoryRequest.DeliverFragment:
					if (reader.TryReadByte("StoryRequest.fragment.bossIndex", out byte bossIndex)
						&& reader.TryReadByte("StoryRequest.fragment.source", out byte source)) {
						WastelandMemorySystem.HandleDeliveryRequest(player, bossIndex, source);
					}

					return;

				default:
					NetLog.Warn("WastelandNet: 玩家 " + whoAmI + " 发来未知的剧情请求动作 " + action
						+ "，已忽略（服务端与客户端模组版本可能不一致）");
					break;
			}

			// 只有"未知动作"会走到这里：这一段剩下的字节读不懂，统一跳过并记一条日志。
			// 宽容处理是为了让**老客户端多发几个字节**时服务端还能正常跑，而不是把报文流打乱。
			reader.SkipRemaining();
		}

		/// <summary>本机打包一段固定字节数的负载后真发出去（只为日志与断言服务，避免各处重复）。</summary>
		internal static void LogOut(string what, byte kind, byte action, int bytes, bool serverSide)
		{
			if (!DebugLogPackets) {
				return;
			}

			NetLog.Debug(string.Format("WastelandNet: 发出 {0} kind={1} action={2} 共 {3} 字节（{4}）",
				what, kind, action, bytes, serverSide ? "服务端" : "客户端"));
		}

		/// <summary>收包日志：kind / action / 已读字节 / 本段总字节。</summary>
		internal static void LogIn(byte kind, byte action, int consumed, int total)
		{
			if (!DebugLogPackets) {
				return;
			}

			NetLog.Debug(string.Format("WastelandNet: 收到 kind={0} action={1}，已读 {2} 字节 / 本段 {3} 字节",
				kind, action, consumed, total));
		}

		/// <summary>
		/// 逐字段核对用的"字节账本"：把发送侧真实写出的字节数与**人工登记**的期望值比对。
		/// 不一致只写一条 WARN（不抛异常），用来在改动协议时第一时间发现漂移。
		/// </summary>
		internal static void AssertBytes(string what, byte kind, int expected, int actual)
		{
			if (expected == actual) {
				return;
			}

			NetLog.Warn(string.Format(
				"WastelandNet: 【协议漂移】{0}（kind {1}）人工登记 {2} 字节，实际写出 {3} 字节 —— 请同步修改登记值与收包侧字段表",
				what, kind, expected, actual));
		}
	}

	/// <summary>
	/// 写侧：<see cref="BinaryWriter"/> 的**显式宽度**封装。
	///
	/// <para/>为什么不让调用方直接用 <c>ModPacket.Write(x)</c>：那会由 C# 重载决议决定写几个字节
	/// （<c>Write(byte)</c> = 1、<c>Write(int)</c> = 4、<c>Write(bool)</c> = 1），
	/// 而上游哪天把字段类型从 <c>byte</c> 改成 <c>int</c>（或者反过来）而不改收包侧，
	/// 就会直接变成玩家实测的那种 <c>Read underflow</c> 崩溃 —— 本次根因就是这个。
	/// 这里所有方法名都带宽度后缀，改宽度时编译期就会逼着改名字。
	///
	/// <para/><see cref="Count"/> 是**真实写出**的字节数，供日志与断言使用。
	/// </summary>
	public sealed class NetWriter
	{
		private readonly BinaryWriter writer;

		/// <summary>已经写出去的字节数（含 <c>WriteString</c> 的 7 位长度前缀）。</summary>
		public int Count { get; private set; }

		public NetWriter(BinaryWriter writer)
		{
			this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
		}

		/// <summary>消息大类型 / 动作编号 / 任意"取值范围就是 1 字节"的标志：**恒定 1 字节**。</summary>
		public void WriteByte(byte value)
		{
			writer.Write(value);
			Count += 1;
		}

		public void WriteBool(bool value)
		{
			writer.Write(value);
			Count += 1;
		}

		public void WriteInt32(int value)
		{
			writer.Write(value);
			Count += 4;
		}

		public void WriteSingle(float value)
		{
			writer.Write(value);
			Count += 4;
		}

		/// <summary>
		/// 字符串：1 个 7 位编码长度前缀（长度 &lt; 128 时正好 1 字节）+ UTF-8 内容。
		/// <para/>⚠️ 长度前缀**可能超过 1 字节**（字符串 ≥ 128 字节时是 2 字节），
		/// 所以这里按真实字节数累计，不能写死 1。
		/// </summary>
		public void WriteString(string value)
		{
			string text = value ?? string.Empty;
			writer.Write(text);
			Count += EncodedLength(text.Length) + Encoding.UTF8.GetByteCount(text);
		}

		/// <summary>7 位编码长度前缀自身占几个字节。</summary>
		private static int EncodedLength(int value)
		{
			int bytes = 1;

			while (value >= 0x80) {
				value >>= 7;
				bytes++;
			}

			return bytes;
		}
	}

	/// <summary>
	/// 读侧：<see cref="BinaryReader"/> 的**有界**封装。
	///
	/// <para/>⚠️ 这是本轮修 bug 的"兜底断言"所在，但要说清它**能**与**不能**做什么：
	/// <list type="bullet">
	/// <item><b>能做</b>：把"剩余字节够不够读这个字段"变成一次显式判断 ——
	/// 不够就 <c>NetLog.Warn</c> 一条带字段名与 kind 的中文日志并返回 false，
	/// **绝不抛异常**。tModLoader 的 Read underflow 之所以会把玩家打崩，
	/// 就是因为异常从 <c>HandlePacket</c> 里逃了出去；关在这里面之后，
	/// 最坏结果只是"这一条包没生效"。</item>
	/// <item><b>不能做</b>：最外层**拿不到本段的字节数**
	/// —— <c>Mod.HandlePacket(reader, whoAmI)</c> 没有 <c>length</c>，
	/// 能算出它的 <c>ModNet.HandleModPacket</c> 是 tModLoader 的 internal 路径（Cecil 已核）。
	/// 所以最外层传 -1（无预算），<see cref="Need"/> 放行；
	/// 真正的防线是**收发包逐字段对称**（见 <see cref="WastelandNet"/> 类注释的账本）。</item>
	/// </list>
	/// </summary>
	public sealed class NetReader
	{
		private readonly BinaryReader reader;
		private readonly string label;
		private readonly int remainingAtStart;
		private readonly bool unbounded;
		private readonly int lowerBound;

		/// <summary>
		/// 本包发送者的玩家槽位（<c>HandlePacket</c> 给的 <c>whoAmI</c>）。
		/// **服务端只认这个值**，不信包里任何"我是几号玩家"的字段（否则可以改别人的进度）。
		/// 客户端上它是 255（无意义）。
		/// </summary>
		public int WhoAmI { get; }

		/// <summary>本子段开始时的总字节数（日志用；不预判长度时为 0）。</summary>
		public int RemainingAtStart => remainingAtStart;

		/// <summary>本子段**已经读掉**的字节数。</summary>
		public int Consumed => (int)(reader.BaseStream.Position - lowerBound);

		/// <summary>本子段**还剩**多少字节没读（不预判长度时恒为 <see cref="int.MaxValue"/>）。</summary>
		public int Remaining => unbounded ? int.MaxValue : remainingAtStart - Consumed;

		/// <param name="remaining">
		/// 本子段的字节预算；**传负数表示"没有预算信息"**（最外层就是这样，
		/// 因为 tModLoader 的 <c>Mod.HandlePacket</c> 拿不到 <c>length</c>）。
		/// 没有预算时 <see cref="Need"/> 永远放行，防线退化成"读到缓冲区尽头时
		/// <see cref="BinaryReader"/> 自己会抛，但那已经不是我们的调用栈"——
		/// ⚠️ 所以协议对称性仍是**唯一**的真防线，<c>Need</c> 只是把它守得更早。
		/// </param>
		public NetReader(BinaryReader reader, int remaining, string label, int whoAmI = -1)
		{
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			this.label = label ?? string.Empty;
			unbounded = remaining < 0;
			remainingAtStart = unbounded ? 0 : remaining;
			lowerBound = (int)reader.BaseStream.Position;
			WhoAmI = whoAmI;
		}

		/// <summary>
		/// 划出一个子段：把当前位置当作子段的起点，并给它一段独立的字节预算。
		/// 用于"内层还要再分 0/1/2/3/4"这种嵌套协议（例如壁炉进出包）。
		/// </summary>
		public NetReader Fork(int remaining, string childLabel)
		{
			return new NetReader(reader, remaining,
				string.IsNullOrEmpty(label) ? childLabel : label + "/" + childLabel, WhoAmI);
		}

		/// <summary>剩余字节够不够读 <paramref name="need"/> 个；不够就记一条明确的中文日志。</summary>
		public bool Need(int need, string field)
		{
			if (Remaining >= need) {
				return true;
			}

			NetLog.Warn(string.Format(
				"WastelandNet: 【收包字节不足】{0} → 字段 {1} 需要 {2} 字节，实际只剩 {3} 字节"
				+ "（本段共 {4} 字节）——已安全放弃本包，不再继续读。"
				+ "这通常说明客户端与服务端的模组版本不一致，请两边都重装同一份构建",
				label, field, need, Remaining, remainingAtStart));
			return false;
		}

		/// <summary>
		/// 把本段剩下的字节全部跳过（宽容处理读不懂的旧客户端包，避免打乱后面的报文）。
		/// <para/>⚠️ 没有字节预算时（<see cref="Remaining"/> = <see cref="int.MaxValue"/>）**什么都不做**：
		/// 那时候"剩下多少"是未知的，硬跳只会跳到缓冲区外面去。
		/// </summary>
		public void SkipRemaining()
		{
			int left = Remaining;

			if (unbounded || left <= 0) {
				return;
			}

			reader.BaseStream.Seek(left, SeekOrigin.Current);
			NetLog.Debug("WastelandNet: " + label + " 跳过 " + left + " 个读不懂的字节（不影响后面的包）");
		}

		public bool TryReadByte(string field, out byte value)
		{
			value = 0;

			if (!Need(1, field)) {
				return false;
			}

			value = reader.ReadByte();
			return true;
		}

		public bool TryReadBool(string field, out bool value)
		{
			value = false;

			if (!Need(1, field)) {
				return false;
			}

			value = reader.ReadBoolean();
			return true;
		}

		public bool TryReadInt32(string field, out int value)
		{
			value = 0;

			if (!Need(4, field)) {
				return false;
			}

			value = reader.ReadInt32();
			return true;
		}

		public bool TryReadSingle(string field, out float value)
		{
			value = 0f;

			if (!Need(4, field)) {
				return false;
			}

			value = reader.ReadSingle();
			return true;
		}

		/// <summary>
		/// 字符串的兜底：先看**长度前缀本身**够不够（7 位编码最长 5 字节），
		/// 读出长度后再核对内容字节够不够。绝不让 <c>ReadString</c> 抛 <c>IOException</c>。
		/// </summary>
		public bool TryReadString(string field, out string value)
		{
			value = string.Empty;

			if (!Need(1, field + ".len")) {
				return false;
			}

			long start = reader.BaseStream.Position;
			int length;

			try {
				length = reader.Read7BitEncodedInt();
			}
			catch (Exception exception) {
				NetLog.Warn("WastelandNet: 【收包失败】" + label + " → 字段 " + field
					+ " 的长度前缀读不出来：" + exception.GetType().Name);
				return false;
			}

			int prefix = (int)(reader.BaseStream.Position - start);

			if (length < 0 || prefix + length > Remaining) {
				NetLog.Warn(string.Format(
					"WastelandNet: 【收包字符串越界】{0} → 字段 {1} 声明 {2} 字节（前缀 {3} 字节），"
					+ "但本段只剩 {4} 字节：按服务端与客户端协议不一致处理，放弃本包",
					label, field, length, prefix, Remaining));
				// 把长度前缀读掉的部分退回去，让 SkipRemaining 的账目保持正确
				reader.BaseStream.Seek(-prefix, SeekOrigin.Current);
				return false;
			}

			try {
				value = reader.ReadString();
				return true;
			}
			catch (Exception exception) {
				NetLog.Warn("WastelandNet: 【收包失败】" + label + " → 字段 " + field
					+ " 读取异常：" + exception.GetType().Name + ": " + exception.Message);
				return false;
			}
		}
	}

	/// <summary>
	/// 模组网络层用的**安全日志器**（独立成一个类是因为 <see cref="NetWriter"/> /
	/// <see cref="NetReader"/> 不是 <c>Mod</c>/<c>ModSystem</c> 的子类，拿不到那个
	/// 只读的 <c>Logger</c> 属性；裸写 <c>Logger</c> 在它们里面编译不过）。
	///
	/// <para/>取不到日志器时（理论上只在模组极早期的加载阶段）退化成
	/// <c>Console.WriteLine</c>，绝不因为"日志写不出去"再抛一次异常
	/// —— 收包路径上任何一次抛异常都会变成玩家的联机崩溃。
	/// </summary>
	internal static class NetLog
	{
		private static log4net.ILog Logger
		{
			get
			{
				try {
					return ModLoader.GetMod("WastelandSoul")?.Logger;
				}
				catch (Exception) {
					return null;
				}
			}
		}

		public static void Info(string message)
		{
			log4net.ILog log = Logger;

			if (log != null) {
				log.Info(message);
			}
			else {
				Console.WriteLine("[WastelandSoul] " + message);
			}
		}

		public static void Warn(string message)
		{
			log4net.ILog log = Logger;

			if (log != null) {
				log.Warn(message);
			}
			else {
				Console.WriteLine("[WastelandSoul][WARN] " + message);
			}
		}

		public static void Debug(string message)
		{
			log4net.ILog log = Logger;

			if (log != null) {
				log.Debug(message);
			}
			else {
				Console.WriteLine("[WastelandSoul][DEBUG] " + message);
			}
		}
	}
}
