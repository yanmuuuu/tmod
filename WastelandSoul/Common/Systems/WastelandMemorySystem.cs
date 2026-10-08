using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
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

			return bossIndex > 0 && TryHandIn(player, bossIndex) ? 1 : 0;
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
