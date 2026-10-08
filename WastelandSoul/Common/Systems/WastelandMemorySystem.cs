using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Common.Bosses;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 记忆主线：把「四个 Boss 的灵魂碎片」与「智械人的四段记忆」串起来。
	///
	/// <para/>设计（见 开发说明.md 一之九）：
	/// 1 清道夫 → 她是「守望者计划」原型机，清道夫是量产型
	/// 2 归档者 → 量产型为何被阉割 / 日志里的「归档」代码
	/// 3 灰烬之心 → 她曾参与执行，是毁灭的帮凶
	/// 4 壁炉守卫 → 壁炉真相：世界重置的载体 → 抉择
	///
	/// <para/>碎片是**任务道具**：
	/// <list type="bullet">
	/// <item><b>每个世界只给一次</b> —— 见 <see cref="ShouldGrantSoulFragment"/>：
	/// 这段记忆还没恢复、而且玩家背包/银行里也没有这枚碎片时，掉落袋才会发；</item>
	/// <item><b>交给智械人会被消耗</b> —— 见 <see cref="TryHandIn"/>：
	/// 消耗碎片 → 由她自己读取、更新记忆 → 给玩家提示。</item>
	/// </list>
	///
	/// <para/>这里只负责「碎片交付 / 记忆阶段」这层逻辑，对话文本由智械人的
	/// GetChat 通过 <see cref="MemoryTextKey"/> 取用，避免把台词写死在代码里。
	/// </summary>
	public class WastelandMemorySystem : ModSystem
	{
		/// <summary>记忆总段数（与灵魂碎片数量、Boss 数量一致）。</summary>
		public const int TotalMemories = 4;

		/// <summary>玩家当前解锁到第几段记忆（0 = 还没交付过任何碎片）。</summary>
		public static int MemoryStage(Player player)
		{
			return Utils.Clamp(player.GetModPlayer<Common.Players.WastelandPlayer>().soulFragmentsDelivered, 0, TotalMemories);
		}

		// ==================== 门槛：每个世界只给一次 ====================

		/// <summary>第 bossIndex 段记忆是否已经恢复（世界级标志）。</summary>
		public static bool MemoryRestored(int bossIndex)
		{
			switch (bossIndex) {
				case 1:
					return WastelandStorySystem.firstMemoryRestored;
				case 2:
					return WastelandStorySystem.secondMemoryRestored;
				case 3:
					return WastelandStorySystem.thirdMemoryRestored;
				case 4:
					return WastelandStorySystem.fourthMemoryRestored;
				default:
					return false;
			}
		}

		/// <summary>
		/// 这段记忆对这名玩家来说是否已经拿到过：世界标志已恢复，或他自己早就交付过这一枚。
		/// <para/>这是「每个世界只给一次」的第一个条件。
		/// </summary>
		public static bool MemoryAlreadyRecovered(Player player, int bossIndex)
		{
			if (MemoryRestored(bossIndex)) {
				return true;
			}

			return player != null
				&& player.GetModPlayer<Common.Players.WastelandPlayer>().soulFragmentsDelivered >= bossIndex;
		}

		/// <summary>
		/// 这名玩家现在该不该拿到第 bossIndex 枚碎片（**掉落袋唯一的门槛**）：
		/// 「这段记忆还没恢复」**且**「背包/银行里没有这枚碎片」才发。
		/// <para/>否则反复打同一个 Boss 会刷出一堆用不掉的碎片，把背包塞满。
		/// </summary>
		public static bool ShouldGrantSoulFragment(Player player, int bossIndex)
		{
			if (player == null || bossIndex < 1 || bossIndex > TotalMemories) {
				return false;
			}

			if (MemoryAlreadyRecovered(player, bossIndex)) {
				return false;
			}

			return !HasSoulFragmentAnywhere(player, bossIndex);
		}

		// ==================== 碎片在谁手里 ====================

		/// <summary>背包，或银行（猪猪 / 保险箱 / 护卫熔炉 / 虚空仓库）里有没有这枚碎片。</summary>
		public static bool HasSoulFragmentAnywhere(Player player, int bossIndex)
		{
			int itemType = SoulFragmentTypeForBoss(bossIndex);

			if (player == null || itemType <= 0) {
				return false;
			}

			if (player.HasItem(itemType)) {
				return true;
			}

			return ChestHasItem(player.bank, itemType)
				|| ChestHasItem(player.bank2, itemType)
				|| ChestHasItem(player.bank3, itemType)
				|| ChestHasItem(player.bank4, itemType);
		}

		/// <summary>
		/// 身上带着的、最该交出去的那一枚碎片（0 = 一枚都没有）。
		/// <para/>优先交「记忆还没恢复」的那枚（推进剧情）；全恢复过了就是**补交**，交掉一枚清一枚背包。
		/// </summary>
		public static int NextCarriedFragmentIndex(Player player)
		{
			if (player == null) {
				return 0;
			}

			for (int bossIndex = 1; bossIndex <= TotalMemories; bossIndex++) {
				if (HasSoulFragmentAnywhere(player, bossIndex) && !MemoryAlreadyRecovered(player, bossIndex)) {
					return bossIndex;
				}
			}

			for (int bossIndex = 1; bossIndex <= TotalMemories; bossIndex++) {
				if (HasSoulFragmentAnywhere(player, bossIndex)) {
					return bossIndex;
				}
			}

			return 0;
		}

		// ==================== 交付 ====================

		/// <summary>
		/// 交出某一只 Boss 对应的那一枚碎片：**真的把它从背包/银行里扣掉**，并记下交付进度。
		/// <para/>交错了不会往前跳（进度只增不减）。
		/// </summary>
		public static bool TryDeliver(Player player, int bossIndex)
		{
			int itemType = SoulFragmentTypeForBoss(bossIndex);

			if (itemType <= 0 || player == null) {
				return false;
			}

			if (!ConsumeSoulFragment(player, itemType)) {
				return false;
			}

			Common.Players.WastelandPlayer modPlayer = player.GetModPlayer<Common.Players.WastelandPlayer>();

			if (modPlayer.soulFragmentsDelivered < bossIndex) {
				modPlayer.soulFragmentsDelivered = bossIndex;
			}

			return true;
		}

		/// <summary>
		/// 交付碎片的完整流程：**消耗碎片 → 推进记忆 → 给玩家提示**。
		/// <para/>记忆早就恢复过（例如第一段是壁炉数据终端触发的）时也能交：碎片照常被消耗，
		/// 但**不重复播那段剧情**，只提示「已归档」——不会出现"碎片交不掉、永远躺在背包里"的死路。
		/// </summary>
		public static bool TryHandIn(Player player, int bossIndex)
		{
			if (player == null || bossIndex < 1 || bossIndex > TotalMemories) {
				return false;
			}

			bool alreadyRecovered = MemoryAlreadyRecovered(player, bossIndex);

			if (!TryDeliver(player, bossIndex)) {
				return false;
			}

			if (bossIndex == 1) {
				// 第一段记忆本来还有一条「读壁炉数据终端」的路，终端会把这两个个人标志接上；
				// 直接交碎片这条路也补上，免得同一个剧情节点两套状态。
				Common.Players.WastelandPlayer modPlayer = player.GetModPlayer<Common.Players.WastelandPlayer>();
				modPlayer.heardFirstMemory = true;
				modPlayer.knowsWatchmanProtocol = true;
			}

			if (alreadyRecovered) {
				AnnounceTo(player, "Mods.WastelandSoul.Messages.SoulFragmentSupplemented");
				return true;
			}

			AnnounceTo(player, "Mods.WastelandSoul.Messages.SoulFragmentHandedIn");
			RestoreMemory(bossIndex);
			return true;
		}

		/// <summary>把身上带着的碎片交出去一枚（备用入口，返回交了几枚：0 或 1）。</summary>
		public static int DeliverCarriedSoulFragments(Player player)
		{
			int bossIndex = NextCarriedFragmentIndex(player);

			if (bossIndex <= 0) {
				return 0;
			}

			// 联机：走与对话按钮同一条服务端权威路径（客户端只发请求）。
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				WastelandStorySystem.RequestFragmentDelivery(bossIndex, FragmentSource(player, bossIndex));
				return 1;
			}

			return TryHandIn(player, bossIndex) ? 1 : 0;
		}

		// ==================== 联机：交付的服务端权威流程 ====================
		//
		// 分工（本轮联机适配）：
		//   客户端：只发「我要交付第 N 枚，它在 source 号容器里」请求（WastelandStorySystem.
		//           RequestFragmentDelivery），本地既不扣物品也不推进任何进度；
		//   服务端：校验 → 扣物品 → 推进记忆 → Sync() → 回执；
		//   客户端收到回执：把**自己那份镜像**按同样的位置扣掉（服务端的背包副本只是镜像，
		//           两边必须一起减，否则客户端 UI 会留着一个已经交掉的碎片）。

		/// <summary>碎片不在身上。</summary>
		public const int SourceNone = 0;

		/// <summary>碎片在**主背包**里（原版唯一会同步到服务端的容器）。</summary>
		public const int SourceInventory = 1;

		/// <summary>猪猪存钱罐。</summary>
		public const int SourceBank1 = 2;

		/// <summary>保险箱。</summary>
		public const int SourceBank2 = 3;

		/// <summary>护卫熔炉。</summary>
		public const int SourceBank3 = 4;

		/// <summary>虚空仓库。</summary>
		public const int SourceBank4 = 5;

		/// <summary>
		/// 这枚碎片现在在**哪一类容器**里（<see cref="SourceNone"/> = 身上没有）。
		///
		/// <para/>顺序必须与 <see cref="ConsumeSoulFragment"/> / <see cref="ConsumeFromSource"/> 一致：
		/// 主背包优先，然后才是银行 —— 两边（服务端与客户端）算出来的位置才一样。
		/// </summary>
		public static int FragmentSource(Player player, int bossIndex)
		{
			int itemType = SoulFragmentTypeForBoss(bossIndex);

			if (player == null || itemType <= 0) {
				return SourceNone;
			}

			if (player.HasItem(itemType)) {
				return SourceInventory;
			}

			if (ChestHasItem(player.bank, itemType)) {
				return SourceBank1;
			}

			if (ChestHasItem(player.bank2, itemType)) {
				return SourceBank2;
			}

			if (ChestHasItem(player.bank3, itemType)) {
				return SourceBank3;
			}

			if (ChestHasItem(player.bank4, itemType)) {
				return SourceBank4;
			}

			return SourceNone;
		}

		/// <summary>从**指定那一类**容器里扣掉一枚碎片。</summary>
		public static bool ConsumeFromSource(Player player, int itemType, int source)
		{
			if (player == null || itemType <= 0) {
				return false;
			}

			switch (source) {
				case SourceInventory:
					return player.ConsumeItem(itemType);

				case SourceBank1:
					return ConsumeFromChest(player.bank, itemType);

				case SourceBank2:
					return ConsumeFromChest(player.bank2, itemType);

				case SourceBank3:
					return ConsumeFromChest(player.bank3, itemType);

				case SourceBank4:
					return ConsumeFromChest(player.bank4, itemType);

				default:
					return false;
			}
		}

		/// <summary>
		/// **服务端**处理客户端的「交付第 bossIndex 枚碎片」请求：校验 → 扣物品 → 推进记忆 → 回执。
		///
		/// <para/>⚠️ 银行那条路的局限（原版机制决定，不是偷懒）：猪猪 / 保险箱 / 护卫熔炉 / 虚空仓库
		/// **根本不随网络同步**（原版只有 <c>Player.inventory</c> 会发 <c>MessageID.PlayerInventorySlot</c>），
		/// 服务端上 <c>player.bank*</c> 永远是空的 —— 也就是说服务端**没法**校验、也没法扣银行里的碎片。
		/// 所以：
		/// <list type="bullet">
		/// <item>主背包（<see cref="SourceInventory"/>）：真校验（这一格必须是那枚碎片）+ 服务端扣；</item>
		/// <item>银行（<see cref="SourceBank1"/>~<see cref="SourceBank4"/>）：只接受客户端自证，
		/// 扣物品由客户端收到回执后自己做。信任级别与改之前的"客户端自己扣"相同，
		/// 换来的是"碎片放银行里也能交"这条与单机一致的体验（玩家要求双端体验一致）。</item>
		/// </list>
		/// </summary>
		public static void HandleDeliveryRequest(Player player, int bossIndex, int source)
		{
			if (player == null || !player.active) {
				return;
			}

			if (bossIndex < 1 || bossIndex > TotalMemories) {
				return;
			}

			int itemType = SoulFragmentTypeForBoss(bossIndex);

			if (itemType <= 0 || source < SourceInventory || source > SourceBank4) {
				return;
			}

			bool alreadyRecovered = MemoryAlreadyRecovered(player, bossIndex);

			if (source == SourceInventory && !player.ConsumeItem(itemType)) {
				return;   // 校验失败：服务端这一份里没有这枚碎片 —— 直接拒绝，什么都不做
			}

			ApplyDelivery(player, bossIndex, alreadyRecovered);
			SendDeliveryAck(player, bossIndex, source, alreadyRecovered);
		}

		/// <summary>推进这次交付该推进的东西（个人进度 + 世界级的四段记忆）。</summary>
		private static void ApplyDelivery(Player player, int bossIndex, bool alreadyRecovered)
		{
			Common.Players.WastelandPlayer modPlayer = player.GetModPlayer<Common.Players.WastelandPlayer>();

			if (bossIndex == 1) {
				// 第一段记忆本来还有一条「读壁炉数据终端」的路，终端会把这两个个人标志接上；
				// 直接交碎片这条路也补上，免得同一个剧情节点两套状态。
				modPlayer.heardFirstMemory = true;
				modPlayer.knowsWatchmanProtocol = true;
			}

			if (modPlayer.soulFragmentsDelivered < bossIndex) {
				modPlayer.soulFragmentsDelivered = bossIndex;
			}

			// 记忆早就恢复过（例如第一段是壁炉数据终端触发的）时也照样消耗碎片，
			// 但**不重播**那段剧情 —— 与 TryHandIn 的语义完全一致。
			if (!alreadyRecovered) {
				RestoreMemory(bossIndex);
			}
		}

		/// <summary>把回执发给**发起交付的那名客户端**。</summary>
		private static void SendDeliveryAck(Player player, int bossIndex, int source, bool alreadyRecovered)
		{
			if (Main.netMode != NetmodeID.Server) {
				return;
			}

			ModPacket packet = WastelandNet.NewPacket();

			if (packet == null) {
				return;
			}

			// 字段表（与 ReceiveDeliveryAck 逐字段对应）：kind(1) + byte + byte + bool = 4 字节
			NetWriter writer = new NetWriter(packet);
			writer.WriteByte(WastelandStorySystem.PacketKindFragmentDelivered);
			writer.WriteByte((byte)bossIndex);
			writer.WriteByte((byte)source);
			writer.WriteBool(alreadyRecovered);

			WastelandNet.AssertBytes("碎片交付回执", WastelandStorySystem.PacketKindFragmentDelivered, 4, writer.Count);

			packet.Send(player.whoAmI, -1);
		}

		/// <summary>
		/// **客户端**收到交付回执：把本机这份镜像按服务端说的位置扣掉一枚，
		/// 接上个人进度标志，并给出与单机一致的提示语（世界进度已经由服务端 Sync 过来了）。
		///
		/// <para/>⚠️ 走 <see cref="NetReader"/> 的有界读取：字节不够只会记一条 WARN 并返回，
		/// 绝不会抛 <c>IOException</c> 把 tModLoader 的 <c>HandlePacket</c> 打崩。
		/// </summary>
		public static void ReceiveDeliveryAck(NetReader reader)
		{
			if (!reader.TryReadByte("ack.bossIndex", out byte bossIndexRaw)
				|| !reader.TryReadByte("ack.source", out byte sourceRaw)
				|| !reader.TryReadBool("ack.alreadyRecovered", out bool alreadyRecovered)) {
				return;
			}

			int bossIndex = bossIndexRaw;
			int source = sourceRaw;

			if (Main.netMode != NetmodeID.MultiplayerClient || bossIndex < 1 || bossIndex > TotalMemories) {
				return;
			}

			if (Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers) {
				return;
			}

			Player player = Main.LocalPlayer;
			int itemType = SoulFragmentTypeForBoss(bossIndex);

			if (!ConsumeFromSource(player, itemType, source)) {
				// 兜底：服务端说的位置本机找不到（玩家在这 1 个来回里把碎片挪了位置），
				// 就按老顺序再找一遍，保证"交出去的那一枚"真的从身上消失。
				ConsumeSoulFragment(player, itemType);
			}

			Common.Players.WastelandPlayer modPlayer = player.GetModPlayer<Common.Players.WastelandPlayer>();

			if (bossIndex == 1) {
				modPlayer.heardFirstMemory = true;
				modPlayer.knowsWatchmanProtocol = true;
			}

			if (modPlayer.soulFragmentsDelivered < bossIndex) {
				modPlayer.soulFragmentsDelivered = bossIndex;
			}

			AnnounceTo(player, alreadyRecovered
				? "Mods.WastelandSoul.Messages.SoulFragmentSupplemented"
				: "Mods.WastelandSoul.Messages.SoulFragmentHandedIn");
		}

		/// <summary>推进到第 bossIndex 段记忆（就是 WastelandStorySystem 里那几个 Restore）。</summary>
		public static void RestoreMemory(int bossIndex)
		{
			switch (bossIndex) {
				case 1:
					WastelandStorySystem.RestoreFirstMemory();
					break;
				case 2:
					WastelandStorySystem.RestoreSecondMemory();
					break;
				case 3:
					WastelandStorySystem.RestoreThirdMemory();
					break;
				case 4:
					WastelandStorySystem.RestoreFourthMemory();
					break;
			}
		}

		/// <summary>优先从背包扣一枚碎片；背包里没有（玩家把它放进银行了）就从银行扣。</summary>
		private static bool ConsumeSoulFragment(Player player, int itemType)
		{
			if (player.HasItem(itemType) && player.ConsumeItem(itemType)) {
				return true;
			}

			return ConsumeFromChest(player.bank, itemType)
				|| ConsumeFromChest(player.bank2, itemType)
				|| ConsumeFromChest(player.bank3, itemType)
				|| ConsumeFromChest(player.bank4, itemType);
		}

		private static bool ChestHasItem(Chest chest, int itemType)
		{
			if (chest == null || chest.item == null) {
				return false;
			}

			for (int i = 0; i < chest.item.Length; i++) {
				Item item = chest.item[i];

				if (item != null && !item.IsAir && item.type == itemType) {
					return true;
				}
			}

			return false;
		}

		private static bool ConsumeFromChest(Chest chest, int itemType)
		{
			if (chest == null || chest.item == null) {
				return false;
			}

			for (int i = 0; i < chest.item.Length; i++) {
				Item item = chest.item[i];

				if (item == null || item.IsAir || item.type != itemType) {
					continue;
				}

				item.stack--;

				if (item.stack <= 0) {
					item.TurnToAir();
				}

				return true;
			}

			return false;
		}

		/// <summary>只提示本机玩家（服务端不说话，联机时提示落在交付的那名玩家屏幕上）。</summary>
		private static void AnnounceTo(Player player, string key)
		{
			if (Main.dedServ || player.whoAmI != Main.myPlayer) {
				return;
			}

			Main.NewText(Language.GetTextValue(key), new Color(180, 200, 255));
		}

		/// <summary>某段记忆的本地化键（台词写在本地化文件里，方便改文案）。</summary>
		public static string MemoryTextKey(int stage)
		{
			return "Mods.WastelandSoul.Dialogue.MechanicalCompanion.Memory" + Utils.Clamp(stage, 1, TotalMemories);
		}

		/// <summary>某段记忆对应的 Boss 序号（1 起）。</summary>
		public static int BossIndexForMemory(int stage)
		{
			return Utils.Clamp(stage, 1, TotalMemories);
		}

		/// <summary>该 Boss 的灵魂碎片物品类型（0 表示没有）。</summary>
		public static int SoulFragmentTypeForBoss(int bossIndex)
		{
			return WastelandBossRegistry.ResolveSoulFragmentType(bossIndex);
		}

		/// <summary>给 UI/调试用：当前记忆阶段的一句话描述键。</summary>
		public static IReadOnlyList<string> AllMemoryKeys()
		{
			List<string> keys = new List<string>();

			for (int stage = 1; stage <= TotalMemories; stage++) {
				keys.Add(MemoryTextKey(stage));
			}

			return keys;
		}
	}
}
