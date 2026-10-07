using System.Collections.Generic;
using Terraria;
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

		/// <summary>交出某一只 Boss 对应的那一枚碎片。交错了不会往前跳。</summary>
		public static bool TryDeliver(Player player, int bossIndex)
		{
			int itemType = SoulFragmentTypeForBoss(bossIndex);

			if (itemType <= 0 || player == null || !player.HasItem(itemType)) {
				return false;
			}

			player.ConsumeItem(itemType);
			Common.Players.WastelandPlayer modPlayer = player.GetModPlayer<Common.Players.WastelandPlayer>();

			if (modPlayer.soulFragmentsDelivered < bossIndex) {
				modPlayer.soulFragmentsDelivered = bossIndex;
			}

			return true;
		}

		public static int DeliverCarriedSoulFragments(Player player)
		{
			int delivered = 0;

			for (int bossIndex = 1; bossIndex <= TotalMemories; bossIndex++) {
				if (TryDeliver(player, bossIndex)) {
					delivered++;
					break;
				}
			}

			return delivered;
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
