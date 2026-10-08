using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 智械人的剧情阶段。对话按阶段分支。
	/// </summary>
	public enum CompanionStage
	{
		Intact,
		FirstMemory,
		SecondMemory,
		ThirdMemory,
		FourthMemory,
		Ending
	}

	/// <summary>
	/// 世界级剧情进度。标志位随世界存档保存，联机时用模组包同步。
	/// </summary>
	public class WastelandStorySystem : ModSystem
	{
		public const int EndingNone = 0;
		public const int EndingVessel = 1;
		public const int EndingRefuse = 2;

		public const string TrueName = "埃尔薇";

		/// <summary>
		/// 模组包的第一个字节 = **消息大类型**。整张表在这里集中登记（唯一入口是
		/// <c>WastelandSoul.HandlePacket</c>），新增任何包都必须先在这里占一个号：
		/// <list type="bullet">
		/// <item><see cref="PacketKindStory"/> = 1：剧情标志同步（服务端 → 客户端，单向）；</item>
		/// <item><see cref="PacketKindFireplaceTravel"/> = 2：壁炉门进出 / 首次成型
		/// （<see cref="FireplaceTravelNet"/>，内部再分 0~4）；</item>
		/// <item><see cref="PacketKindStoryRequest"/> = 3：客户端请求服务端做一次世界状态变更；</item>
		/// <item><see cref="PacketKindPlayerProgress"/> = 4：玩家个人进度（客户端 → 服务端）；</item>
		/// <item><see cref="PacketKindFragmentDelivered"/> = 5：碎片交付回执（服务端 → 单个客户端）。</item>
		/// </list>
		/// </summary>
		public const byte PacketKindStory = 1;

		/// <summary>见 <see cref="PacketKindStory"/> 的说明。</summary>
		public const byte PacketKindFireplaceTravel = 2;

		/// <summary>
		/// 3 号包：**客户端 → 服务端**的世界状态变更请求（<see cref="StoryRequest"/>）。
		///
		/// <para/>为什么需要它：世界状态只能由服务端写（见 <see cref="IsAuthoritativeSide"/>），
		/// 而"读终端 / 选结局 / 召唤智械人 / 交付碎片"这些动作的发起方都是客户端。
		/// 客户端只发这个请求包，服务端校验后再改字段并 <see cref="Sync"/>。
		/// </summary>
		public const byte PacketKindStoryRequest = 3;

		/// <summary>4 号包：**客户端 → 服务端**的玩家个人进度同步（<c>WastelandPlayer</c>）。</summary>
		public const byte PacketKindPlayerProgress = 4;

		/// <summary>5 号包：**服务端 → 发起交付的那名客户端**的回执（碎片交付完成）。</summary>
		public const byte PacketKindFragmentDelivered = 5;

		/// <summary><see cref="PacketKindStoryRequest"/> 包里的动作编号（第 2 个字节）。</summary>
		public static class StoryRequest
		{
			/// <summary>读壁炉里的数据终端（<see cref="MarkDataTerminalRead"/> 的客户端入口）。</summary>
			public const byte DataTerminalRead = 1;

			/// <summary>结局选择，后跟 <c>int</c> 选项。</summary>
			public const byte EndingChoice = 2;

			/// <summary>"本存档已补过首次进壁炉的重载"（<see cref="MarkFireplaceSeen"/> 的客户端入口）。</summary>
			public const byte FireplaceSeen = 3;

			/// <summary>召唤智械人，后跟 <c>float x</c> / <c>float y</c>（落点）。</summary>
			public const byte CompanionSummon = 4;

			/// <summary>交付第 N 枚灵魂碎片，后跟 <c>byte bossIndex</c> / <c>byte source</c>。</summary>
			public const byte DeliverFragment = 5;
		}

		/// <summary>
		/// 本机是不是"权威那一侧"（单机也算）。
		///
		/// <para/>联机里的分工：服务端 / 单机直接改字段 + <see cref="Sync"/>；
		/// 客户端**一格都不写**，只发 <see cref="PacketKindStoryRequest"/> 请求包。
		/// 所有 <c>Mark*</c> / <c>Restore*</c> 入口都以这个判断开头。
		/// </summary>
		public static bool IsAuthoritativeSide => Main.netMode != NetmodeID.MultiplayerClient;

		public static bool scavengerDefeated;
		public static bool archivistDefeated;
		public static bool ashHeartDefeated;
		public static bool fireplaceGuardianDefeated;
		public static bool fireplaceOpened;
		public static bool dataTerminalRead;
		public static bool firstMemoryRestored;
		public static bool secondMemoryRestored;
		public static bool thirdMemoryRestored;
		public static bool fourthMemoryRestored;
		public static bool companionAwakened;
		public static bool codaPlayed;
		public static int endingChoice;
		public static string companionName = string.Empty;

		/// <summary>壁炉入口**第 1 组坐标**（主世界**左侧**海洋那一扇；老存档里可能是出生点旁那扇旧的）。</summary>
		public static int GateX;

		/// <summary>见 <see cref="GateX"/>。</summary>
		public static int GateY;

		/// <summary>第 1 组坐标是不是有效（放过门）。</summary>
		public static bool GatePlaced;

		/// <summary>壁炉入口**第 2 组坐标**（主世界**右侧**海洋那一扇）。</summary>
		public static int GateX2;

		/// <summary>见 <see cref="GateX2"/>。</summary>
		public static int GateY2;

		/// <summary>第 2 组坐标是不是有效（放过门）。</summary>
		public static bool Gate2Placed;

		/// <summary>
		/// 本存档**已经做过**「首次进壁炉自动重载」这件事。
		///
		/// <para/>为什么需要它：SubworldLibrary 首次**生成**子世界那一次，客户端不会像读档那样
		/// 再刷新一遍，玩家第一眼看到的是空世界，必须出去再进一次才显示。
		/// <see cref="FireplaceEntrySystem"/> 会替玩家把这一次重载自动做掉 —— 而"只做一次"就靠这个旗标。
		/// 它随存档保存、也随 <see cref="Sync"/> 联机同步，所以重进存档、联机都一样不会重复触发。
		/// </summary>
		public static bool fireplaceSeen;

		public static CompanionStage CompanionMemoryStage
		{
			get
			{
				if (endingChoice != EndingNone) {
					return CompanionStage.Ending;
				}

				if (fourthMemoryRestored) {
					return CompanionStage.FourthMemory;
				}

				if (thirdMemoryRestored) {
					return CompanionStage.ThirdMemory;
				}

				if (secondMemoryRestored) {
					return CompanionStage.SecondMemory;
				}

				if (firstMemoryRestored) {
					return CompanionStage.FirstMemory;
				}

				return CompanionStage.Intact;
			}
		}

		public override void ClearWorld()
		{
			scavengerDefeated = false;
			archivistDefeated = false;
			ashHeartDefeated = false;
			fireplaceGuardianDefeated = false;
			fireplaceOpened = false;
			dataTerminalRead = false;
			firstMemoryRestored = false;
			secondMemoryRestored = false;
			thirdMemoryRestored = false;
			fourthMemoryRestored = false;
			companionAwakened = false;
			codaPlayed = false;
			endingChoice = EndingNone;
			companionName = string.Empty;
			GatePlaced = false;
			GateX = 0;
			GateY = 0;
			Gate2Placed = false;
			GateX2 = 0;
			GateY2 = 0;
			fireplaceSeen = false;

			Content.NPCs.Bosses.Scavenger.ScavengerContext.Clear();
			Content.NPCs.Bosses.Archivist.ArchivistContext.Clear();
			Content.NPCs.Bosses.AshHeart.AshHeartContext.Clear();
			Content.NPCs.Bosses.FireplaceGuardian.FireplaceGuardianContext.Clear();
		}

		public override void SaveWorldData(TagCompound tag)
		{
			tag["scavenger"] = scavengerDefeated;
			tag["archivist"] = archivistDefeated;
			tag["ashHeart"] = ashHeartDefeated;
			tag["guardian"] = fireplaceGuardianDefeated;
			tag["fireplace"] = fireplaceOpened;
			tag["terminal"] = dataTerminalRead;
			tag["memory1"] = firstMemoryRestored;
			tag["memory2"] = secondMemoryRestored;
			tag["memory3"] = thirdMemoryRestored;
			tag["memory4"] = fourthMemoryRestored;
			tag["companion"] = companionAwakened;
			tag["coda"] = codaPlayed;
			tag["ending"] = endingChoice;
			tag["gatePlaced"] = GatePlaced;
			tag["gateX"] = GateX;
			tag["gateY"] = GateY;
			tag["gate2Placed"] = Gate2Placed;
			tag["gateX2"] = GateX2;
			tag["gateY2"] = GateY2;
			tag["fireplaceSeen"] = fireplaceSeen;

			if (!string.IsNullOrEmpty(companionName)) {
				tag["companionName"] = companionName;
			}
		}

		public override void LoadWorldData(TagCompound tag)
		{
			scavengerDefeated = tag.GetBool("scavenger");
			archivistDefeated = tag.GetBool("archivist");
			ashHeartDefeated = tag.GetBool("ashHeart");
			fireplaceGuardianDefeated = tag.GetBool("guardian");
			fireplaceOpened = tag.GetBool("fireplace");
			dataTerminalRead = tag.GetBool("terminal");
			firstMemoryRestored = tag.GetBool("memory1");
			secondMemoryRestored = tag.GetBool("memory2");
			thirdMemoryRestored = tag.GetBool("memory3");
			fourthMemoryRestored = tag.GetBool("memory4");
			companionAwakened = tag.GetBool("companion");
			codaPlayed = tag.GetBool("coda");
			endingChoice = tag.GetInt("ending");
			GatePlaced = tag.GetBool("gatePlaced");
			GateX = tag.GetInt("gateX");
			GateY = tag.GetInt("gateY");
			// 老存档没有这三个键：GetBool/GetInt 会给 false / 0，
			// FireplaceGateSystem 看到"这一侧没有立着的门"就会把右侧海洋那扇补上。
			Gate2Placed = tag.GetBool("gate2Placed");
			GateX2 = tag.GetInt("gateX2");
			GateY2 = tag.GetInt("gateY2");
			fireplaceSeen = tag.GetBool("fireplaceSeen");
			companionName = tag.GetString("companionName") ?? string.Empty;
		}

		public override void NetSend(BinaryWriter writer)
		{
			WriteStory(writer);
		}

		public override void NetReceive(BinaryReader reader)
		{
			// ⚠️ 世界数据同步这条路的边界由 tModLoader 自己用 BinaryIO 的长度前缀框住，
			// 所以这里不需要额外的字节预算（传 int.MaxValue = 不预判）。
			ReadStory(reader, int.MaxValue);
		}

		public override void PostUpdateWorld()
		{
			// 兜底 / 老存档迁移：第一段记忆现在由「读壁炉数据终端」触发（见 MarkDataTerminalRead）。
			// 旧存档里终端读过、但记忆还停在「交付清道夫碎片」那条路上的，这里直接补上，
			// 免得玩家的主线永远卡在旧路径。
			if (dataTerminalRead && !firstMemoryRestored) {
				RestoreFirstMemory();
			}

			if (codaPlayed || endingChoice == EndingNone || !NPC.downedMoonlord) {
				return;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			codaPlayed = true;
			string key = endingChoice == EndingVessel
				? "Mods.WastelandSoul.Messages.CodaVessel"
				: "Mods.WastelandSoul.Messages.CodaRefuse";
			Announce(key, new Color(230, 220, 180));
			Sync();
		}

		public static void WriteStory(BinaryWriter writer)
		{
			NetWriter net = new NetWriter(writer);
			WriteStoryPayload(net);
		}

		/// <summary>
		/// 剧情标志的**唯一**线格式定义（与 <see cref="ReadStoryPayload"/> 成对）。
		///
		/// <para/>⚠️ 联机 4 个消息的字节账本见 <see cref="WastelandNet"/> 的类注释。
		/// ⚠️ 这里**故意**不用 BitByte 打包 bool：打包能省十几个字节，但漏写一个字段的话
		/// 偏移会整体错位（比崩溃更难查）；1 字节 1 个 bool 的话，字段数一眼可数。
		/// </summary>
		private static void WriteStoryPayload(NetWriter writer)
		{
			writer.WriteBool(scavengerDefeated);
			writer.WriteBool(archivistDefeated);
			writer.WriteBool(ashHeartDefeated);
			writer.WriteBool(fireplaceGuardianDefeated);
			writer.WriteBool(fireplaceOpened);
			writer.WriteBool(dataTerminalRead);
			writer.WriteBool(firstMemoryRestored);
			writer.WriteBool(secondMemoryRestored);
			writer.WriteBool(thirdMemoryRestored);
			writer.WriteBool(fourthMemoryRestored);
			writer.WriteBool(companionAwakened);
			writer.WriteBool(codaPlayed);
			writer.WriteInt32(endingChoice);
			writer.WriteBool(GatePlaced);
			writer.WriteInt32(GateX);
			writer.WriteInt32(GateY);
			writer.WriteBool(Gate2Placed);
			writer.WriteInt32(GateX2);
			writer.WriteInt32(GateY2);
			writer.WriteBool(fireplaceSeen);
			writer.WriteString(companionName);
		}

		public static void ReadStory(BinaryReader reader, int remaining)
		{
			NetReader net = new NetReader(reader, remaining, "StorySync");

			if (!ReadStoryPayload(net)) {
				return;   // 读不完整（版本不一致 / 被截断）：保持旧值，绝不让 HandlePacket 抛异常
			}

			OnStorySyncedOnClient();
		}

		/// <summary>
		/// 与 <see cref="WriteStoryPayload"/> **逐字段一一对应**的读取。
		/// 任何一步字节不够都会立刻返回 false（由 <see cref="NetReader"/> 记明确日志）。
		/// </summary>
		private static bool ReadStoryPayload(NetReader reader)
		{
			if (!reader.TryReadBool("story.scavenger", out scavengerDefeated)
				|| !reader.TryReadBool("story.archivist", out archivistDefeated)
				|| !reader.TryReadBool("story.ashHeart", out ashHeartDefeated)
				|| !reader.TryReadBool("story.guardian", out fireplaceGuardianDefeated)
				|| !reader.TryReadBool("story.fireplace", out fireplaceOpened)
				|| !reader.TryReadBool("story.terminal", out dataTerminalRead)
				|| !reader.TryReadBool("story.memory1", out firstMemoryRestored)
				|| !reader.TryReadBool("story.memory2", out secondMemoryRestored)
				|| !reader.TryReadBool("story.memory3", out thirdMemoryRestored)
				|| !reader.TryReadBool("story.memory4", out fourthMemoryRestored)
				|| !reader.TryReadBool("story.companion", out companionAwakened)
				|| !reader.TryReadBool("story.coda", out codaPlayed)
				|| !reader.TryReadInt32("story.ending", out endingChoice)
				|| !reader.TryReadBool("story.gatePlaced", out GatePlaced)
				|| !reader.TryReadInt32("story.gateX", out GateX)
				|| !reader.TryReadInt32("story.gateY", out GateY)
				|| !reader.TryReadBool("story.gate2Placed", out Gate2Placed)
				|| !reader.TryReadInt32("story.gateX2", out GateX2)
				|| !reader.TryReadInt32("story.gateY2", out GateY2)
				|| !reader.TryReadBool("story.fireplaceSeen", out fireplaceSeen)
				|| !reader.TryReadString("story.companionName", out companionName)) {
				return false;
			}

			companionName ??= string.Empty;
			return true;
		}

		/// <summary>把当前剧情标志发给所有客户端。单人 / **客户端**上什么都不做。</summary>
		public static void Sync()
		{
			if (Main.netMode == NetmodeID.SinglePlayer) {
				return;
			}

			// ⚠️ 客户端**不许**把世界状态推给服务端（那样客户端就能伪造剧情进度）。
			// 联机里客户端要改世界状态只有一条路：发 PacketKindStoryRequest 请求包，
			// 由服务端校验后自己改、自己 Sync。1 号包因此是"服务端 → 客户端"单向的。
			if (Main.netMode != NetmodeID.Server) {
				return;
			}

			// ⚠️ 必须写 global:: 前缀：本文件在 WastelandSoul.Common.Systems 命名空间下，
			// 裸写 `WastelandSoul.WastelandSoul` 会被解析成"类型 WastelandSoul 里的嵌套类型 WastelandSoul"（CS0426）
			ModPacket packet = WastelandNet.NewPacket();

			if (packet == null) {
				return;
			}

			// ⚠️ 类型字节一律走 NetWriter.WriteByte —— 也就是**恒定 1 个字节**。
			// 旧代码这里是 packet.Write(PacketKindStory)：实参是 byte 常量，但形参推断成 int，
			// 重载决议会选中 Write(Int32) 写出 4 个字节；1 号包当时恰好靠"发包侧也写 4 字节、
			// 收包侧只读 1 字节"以外的路径掩盖了问题（见 WastelandNet 类注释里的 4 号包事故）。
			NetWriter writer = new NetWriter(packet);
			writer.WriteByte(PacketKindStory);
			WriteStoryPayload(writer);
			packet.Send();

			WastelandNet.AssertBytes("剧情同步 Sync()", PacketKindStory, StoryPayloadBytes(), writer.Count);
		}

		/// <summary>
		/// 剧情同步包的**期望字节数**（人工登记值，用于 <see cref="WastelandNet.AssertBytes"/> 兜底断言）：
		/// <list type="bullet">
		/// <item><b>15 个 bool</b>（15 字节）：12 个剧情标志 + <c>GatePlaced</c> + <c>Gate2Placed</c> + <c>fireplaceSeen</c>；</item>
		/// <item><b>5 个 int32</b>（20 字节）：<c>endingChoice</c> + <c>GateX/GateY</c> + <c>GateX2/GateY2</c>；</item>
		/// <item>外加名字的 7 位长度前缀 + UTF-8 内容。</item>
		/// </list>
		///
		/// <para/>⚠️ 这里**故意**手写成一个显式算式而不是"数一数字段"：
		/// 以后谁加了字段却忘了改这个数，日志里就会出现一条【协议漂移】WARN。
		/// ⚠️ 顺手修掉了这次改动前就存在的漂移：旧值是 <c>12 + 12</c>（漏算了
		/// <c>GatePlaced</c> / <c>fireplaceSeen</c> 两个 bool），每次 <see cref="Sync"/> 都会
		/// 白写一条 WARN —— 记在显式算式里就不会再漏。
		/// </summary>
		private static int StoryPayloadBytes()
		{
			int nameBytes = System.Text.Encoding.UTF8.GetByteCount(companionName ?? string.Empty);
			int prefix = 1;

			for (int v = nameBytes; v >= 0x80; v >>= 7) {
				prefix++;
			}

			return 15 + 20 + prefix + nameBytes;
		}

		// ==================================================================================
		// 客户端 → 服务端的四种"请求服务端改世界状态"（3 号包）
		//
		// ⚠️ 每个方法的负载必须与 WastelandNet.ReceiveRequest 里对应分支的字段表**逐字段一致**：
		//     action 1 读终端   ：无负载
		//     action 2 选结局   ：int32
		//     action 3 见过壁炉 ：无负载
		//     action 4 召唤智械人：float X + float Y
		//     action 5 交付碎片 ：byte bossIndex + byte source
		// ==================================================================================

		/// <summary>客户端请求"读壁炉数据终端"。</summary>
		public static void RequestDataTerminalRead()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				return;
			}

			// 服务端的 1 号包回来时补一次客户端侧收尾（个人标志 + 提示语）：
			// 这两件事原本是 FireplaceTiles.cs 在本地同步做完的，现在字段要等服务端确认。
			pendingTerminalEcho = true;

			WastelandNet.SendRequest(PacketKindStoryRequest, StoryRequest.DataTerminalRead, null);
		}

		/// <summary>客户端请求"选结局"。</summary>
		public static void RequestEndingChoice(int choice)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				return;
			}

			WastelandNet.SendRequest(PacketKindStoryRequest, StoryRequest.EndingChoice,
				writer => writer.WriteInt32(choice));
		}

		/// <summary>客户端请求"记下本存档已补过首次进壁炉的重载"。</summary>
		public static void RequestFireplaceSeen()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				return;
			}

			WastelandNet.SendRequest(PacketKindStoryRequest, StoryRequest.FireplaceSeen, null);
		}

		/// <summary>客户端请求"在 spot 处召唤智械人"（服务端生成 + 置世界标志）。</summary>
		public static void RequestCompanionSummon(Vector2 spot)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				return;
			}

			// 落点由客户端给（它在遗迹躯体上右键时算出来的位置），服务端只负责"生成"这件权威的事。
			WastelandNet.SendRequest(PacketKindStoryRequest, StoryRequest.CompanionSummon, writer => {
				writer.WriteSingle(spot.X);
				writer.WriteSingle(spot.Y);
			});
		}

		/// <summary>客户端请求"交付第 bossIndex 枚碎片（它在 source 号容器里）"。</summary>
		public static void RequestFragmentDelivery(int bossIndex, int source)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				return;
			}

			// 两个字段都是 byte：范围在 WastelandMemorySystem.HandleDeliveryRequest 里会再校验一次。
			WastelandNet.SendRequest(PacketKindStoryRequest, StoryRequest.DeliverFragment, writer => {
				writer.WriteByte((byte)bossIndex);
				writer.WriteByte((byte)source);
			});
		}

		/// <summary>
		/// 本模组包的**统一收包入口**（由 <c>WastelandSoul.HandlePacket</c> 调用）。
		///
		/// <para/>⚠️ <paramref name="remaining"/> 是本模组这一段负载的总字节数。
		/// ⚠️ **tModLoader 那边拿不到这个值**：<c>Mod.HandlePacket</c> 的签名里没有它，
		/// 而能算出它的 <c>ModNet.HandleModPacket(BinaryReader, int whoAmI, int length)</c>
		/// 是 tModLoader 的 internal 路径，我们插不进去（Cecil 已核）。
		/// 所以默认值给 <see cref="int.MaxValue"/> = "不预判长度"，
		/// 真正的防线落在 <see cref="NetReader"/> 的"读不到就记日志、绝不抛异常"上
		/// —— tModLoader 的 Read underflow 之所以会把玩家打崩，就是因为异常从
		/// <c>HandlePacket</c> 里逃了出去；把它关在 NetReader 里面，玩家最多是"这一条包没生效"。
		///
		/// <para/>这个参数保留的意义：单测 / 将来的诊断路径可以传入精确值，
		/// 让 <see cref="NetReader.Need"/> 提前发现截断（而不是等读到缓冲区尽头）。
		/// </summary>
		public static void ReceivePacket(BinaryReader reader, int whoAmI, int remaining = int.MaxValue)
		{
			WastelandNet.ReceivePacket(reader, whoAmI, remaining);
		}

		/// <summary>服务端处理的 1 号包（剧情全量同步）。**只有客户端才有意义**。</summary>
		internal static void ReceiveStorySync(NetReader reader)
		{
			// 1 号包是服务端单向广播。服务端不该收到它 —— 收到只可能是客户端伪造，一律忽略
			// （旧版这里会 ReadStory 把客户端发来的值抄进世界状态，那等于放弃权威）。
			if (Main.netMode == NetmodeID.Server) {
				return;
			}

			if (!ReadStoryPayload(reader)) {
				return;
			}

			OnStorySyncedOnClient();
		}

		/// <summary>本机刚请求过"读终端"、还没等到服务端确认（只在客户端有意义）。</summary>
		private static bool pendingTerminalEcho;

		/// <summary>
		/// 客户端收到服务端 1 号包之后的收尾。
		///
		/// <para/>只做一件本地事：如果本机刚请求过"读终端"，就把**玩家个人**的两个标志接上
		/// 并提示去和智械人说话 —— 与 <c>FireplaceTiles.cs</c> 里原本在本地做的那两件事等价，
		/// 只是改成"等世界状态真的由服务端确认之后"再做。
		/// </summary>
		private static void OnStorySyncedOnClient()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient || !pendingTerminalEcho) {
				return;
			}

			if (!dataTerminalRead) {
				return;   // 服务端还没认这次读取，继续等
			}

			pendingTerminalEcho = false;

			if (Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers) {
				return;
			}

			Common.Players.WastelandPlayer modPlayer = Main.LocalPlayer.GetModPlayer<Common.Players.WastelandPlayer>();
			bool firstTime = !modPlayer.heardFirstMemory;

			modPlayer.heardFirstMemory = true;
			modPlayer.knowsWatchmanProtocol = true;

			if (firstTime) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.TerminalTalkToHer"), new Color(160, 200, 230));
			}
		}

		public static void MarkScavengerDefeated()
		{
			// 世界状态只由服务端写。⚠️ 客户端**不要**把这个当成"服务端没收到"：
			// NPC 死亡在服务端一定会走一遍 OnKill（掉落、downedBoss 都是这么做的），
			// 所以这里直接丢弃是安全的，也堵掉了客户端伪造"我打完 Boss 了"的路。
			if (!IsAuthoritativeSide) {
				return;
			}

			if (scavengerDefeated) {
				return;
			}

			scavengerDefeated = true;
			fireplaceOpened = true;
			Announce("Mods.WastelandSoul.Messages.FireplaceOpened", new Color(226, 122, 74));
			Sync();
		}

		public static void MarkArchivistDefeated()
		{
			if (!IsAuthoritativeSide) {
				return;
			}

			if (archivistDefeated) {
				return;
			}

			archivistDefeated = true;
			Announce("Mods.WastelandSoul.Messages.ArchivistDefeated", new Color(176, 190, 224));
			Sync();
		}

		public static void MarkAshHeartDefeated()
		{
			if (!IsAuthoritativeSide) {
				return;
			}

			if (ashHeartDefeated) {
				return;
			}

			ashHeartDefeated = true;
			Announce("Mods.WastelandSoul.Messages.AshHeartDefeated", new Color(255, 140, 70));
			Sync();
		}

		public static void MarkFireplaceGuardianDefeated()
		{
			if (!IsAuthoritativeSide) {
				return;
			}

			if (fireplaceGuardianDefeated) {
				return;
			}

			fireplaceGuardianDefeated = true;
			Announce("Mods.WastelandSoul.Messages.FireplaceGuardianDefeated", new Color(170, 210, 255));
			Sync();
		}

		public static void MarkDataTerminalRead()
		{
			// 读终端是**客户端**发起的（右键图块），所以客户端这一侧改成发请求包。
			if (!IsAuthoritativeSide) {
				RequestDataTerminalRead();
				return;
			}

			if (dataTerminalRead) {
				return;
			}

			dataTerminalRead = true;
			Announce("Mods.WastelandSoul.Messages.TerminalRead", new Color(150, 200, 230));

			// 主线改动：第一段记忆改由「读壁炉里的数据终端」这一刻触发。
			// 旧路径（击败清道夫后把灵魂碎片·其一交给智械人）已废弃；
			// 碎片交付只保留给第二段及以后的记忆。
			bool alreadyRestored = firstMemoryRestored;

			RestoreFirstMemory();

			// RestoreFirstMemory 会自己 Sync；只有它提前返回（记忆早就恢复过）时才需要在这里补发，
			// 否则终端标志会漏同步。
			if (alreadyRestored) {
				Sync();
			}
		}

		/// <summary>
		/// 记下"本存档已经替玩家补过首次进壁炉的重载"。
		/// <para/>⚠️ 调用方要在**动手之前**调它（见 <see cref="FireplaceEntrySystem"/>）：
		/// 宁可这次重载失败，也不能因为失败而反复把玩家踢出世界。
		/// </summary>
		public static void MarkFireplaceSeen()
		{
			// 客户端这一侧（例如以后有人从客户端那条路调它）改成发请求包：
			// 服务端收到后自己置位并 Sync，不会出现"只有本机记得做过"的分叉。
			if (!IsAuthoritativeSide) {
				RequestFireplaceSeen();
				return;
			}

			if (fireplaceSeen) {
				return;
			}

			fireplaceSeen = true;
			Sync();
		}

		public static void MarkCompanionArrived(string name)
		{
			// 只由服务端（或单机的 TrySummon / SummonOnServer）调用；客户端走召唤请求包。
			if (!IsAuthoritativeSide) {
				return;
			}

			companionAwakened = true;

			if (!string.IsNullOrEmpty(name)) {
				companionName = name;
			}

			Sync();
		}

		public static void RestoreFirstMemory()
		{
			if (!IsAuthoritativeSide) {
				return;
			}

			if (firstMemoryRestored) {
				return;
			}

			firstMemoryRestored = true;
			Announce("Mods.WastelandSoul.Messages.FirstMemoryRestored", new Color(180, 200, 255));
			// 记忆本体现在在「读终端」这一刻放出来（原先是在对话里由她当面读）
			Announce("Mods.WastelandSoul.Dialogue.MechanicalCompanion.MemoryFirst", new Color(180, 200, 255));
			Sync();
		}

		public static void RestoreSecondMemory()
		{
			if (!IsAuthoritativeSide) {
				return;
			}

			if (secondMemoryRestored) {
				return;
			}

			secondMemoryRestored = true;
			Announce("Mods.WastelandSoul.Messages.SecondMemoryRestored", new Color(160, 190, 240));
			Sync();
		}

		public static void RestoreThirdMemory()
		{
			if (!IsAuthoritativeSide) {
				return;
			}

			if (thirdMemoryRestored) {
				return;
			}

			thirdMemoryRestored = true;
			Announce("Mods.WastelandSoul.Messages.ThirdMemoryRestored", new Color(255, 160, 90));
			Sync();
		}

		public static void RestoreFourthMemory()
		{
			if (!IsAuthoritativeSide) {
				return;
			}

			if (fourthMemoryRestored) {
				return;
			}

			fourthMemoryRestored = true;
			companionName = TrueName;
			RenameCompanion();
			Announce("Mods.WastelandSoul.Messages.FourthMemoryRestored", new Color(210, 220, 255));
			Announce("Mods.WastelandSoul.Messages.TrueName", new Color(240, 220, 160));
			Sync();
		}

		public static void ChooseEnding(Player player, int choice)
		{
			// 结局选择也是客户端发起的（NPC 对话按钮）：客户端只发请求，服务端校验后落库。
			if (!IsAuthoritativeSide) {
				RequestEndingChoice(choice);
				return;
			}

			if (endingChoice != EndingNone || !fourthMemoryRestored) {
				return;
			}

			endingChoice = choice == EndingRefuse ? EndingRefuse : EndingVessel;
			string key = endingChoice == EndingVessel
				? "Mods.WastelandSoul.Messages.EndingVessel"
				: "Mods.WastelandSoul.Messages.EndingRefuse";
			Announce(key, new Color(230, 210, 160));

			if (player != null) {
				int relic = endingChoice == EndingVessel
					? ModContent.ItemType<Content.Items.Story.DawnSeal>()
					: ModContent.ItemType<Content.Items.Story.UnburnedName>();
				player.QuickSpawnItem(player.GetSource_GiftOrReward(), relic);
			}

			Sync();
		}

		private static void RenameCompanion()
		{
			int type = ModContent.NPCType<Content.NPCs.Town.MechanicalCompanion>();

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (npc.active && npc.type == type) {
					npc.GivenName = TrueName;
				}
			}
		}

		/// <summary>世界事件公告：服务端广播给所有人，单机 / 客户端本地显示。</summary>
		public static void Announce(string key, Color color)
		{
			AnnounceFormat(key, color);
		}

		/// <summary>同上，但带格式化参数（例如「智械人 零号 已到达」，名字是参数）。</summary>
		public static void AnnounceFormat(string key, Color color, params object[] args)
		{
			if (Main.netMode == NetmodeID.Server) {
				Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key, args), color);
				return;
			}

			Main.NewText(Language.GetTextValue(key, args), color);
		}
	}
}
