using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Bosses;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Common.ItemBases
{
	/// <summary>
	/// Boss 掉落袋基类：把该 Boss 的掉落装进一个袋子，打开时保证：
	/// <list type="number">
	/// <item>材料给到**保底数量**（每项自带最小/最大）</item>
	/// <item>**灵魂碎片 100% 掉落**（剧情伏笔）</item>
	/// <item>从装备池里随机取装备，并随机附加**原版词条**（前缀），
	///       词条池按 Boss 时期分档，强度对齐该阶段</item>
	/// </list>
	/// 子类只需要给出：属于第几个 Boss、保底材料、装备池、词条档位。
	/// </summary>
	public abstract class WastelandBossBag : ModItem
	{
		/// <summary>属于第几个 Boss（决定给哪枚灵魂碎片）。</summary>
		protected abstract int BossIndex { get; }

		/// <summary>保底材料：（物品类型, 最少, 最多）。</summary>
		protected abstract List<(int Type, int Min, int Max)> Materials { get; }

		/// <summary>
		/// **袋子专属武器池**（该 Boss 的五职业专属武器）。
		/// <para/>默认**自动收集**：扫描本模组所有 <see cref="WastelandClassWeapon"/> 子类中
		/// 「命名空间属于本 Boss 子目录」且「类名以 EX 结尾」的物品
		/// （子目录名由 <see cref="WastelandBossRegistry"/> 推导，如 Boss1Scavenger）。
		/// 这样以后新增专属武器**不需要改袋子代码**。
		/// <para/>⚠️ 这些武器**只能从袋子开出，绝对不要给它们写合成配方**。
		/// </summary>
		protected virtual List<int> ExclusiveWeapons => CollectExclusiveWeapons();

		/// <summary>本 Boss 的武器子目录名，如 "Boss1Scavenger"。</summary>
		protected virtual string BossFolder =>
			"Boss" + BossIndex + WastelandBossRegistry.All[BossIndex - 1].InternalName;

		/// <summary>按子目录 + EX 后缀自动收集专属武器。</summary>
		protected List<int> CollectExclusiveWeapons()
		{
			List<int> result = new List<int>();
			string folder = BossFolder;

			foreach (ModItem item in Mod.GetContent<ModItem>()) {
				Type type = item.GetType();

				if (type.Namespace != null
					&& type.Namespace.Contains(folder)
					&& type.Name.EndsWith("EX", StringComparison.OrdinalIgnoreCase)) {
					result.Add(item.Type);
				}
			}

			return result;
		}

		/// <summary>每次开出几件专属武器。</summary>
		protected virtual int EquipmentCount => 1;

		/// <summary>该时期允许的原版词条池（哥布林工匠可重铸的那套前缀，按强度分档）。</summary>
		protected abstract int[] PrefixPool { get; }

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.consumable = true;
			Item.rare = ItemRarityID.Purple;
			Item.value = 0;
		}

		public override bool CanRightClick() => true;

		public override void RightClick(Player player)
		{
			// 1) 材料：至少保底数量
			foreach ((int type, int min, int max) in Materials) {
				int count = Main.rand.Next(min, max + 1);
				player.QuickSpawnItem(player.GetSource_OpenItem(Type), type, count);
			}

			// 2) 灵魂碎片：必定掉落
			int soul = WastelandBossRegistry.ResolveSoulFragmentType(BossIndex);

			if (soul > 0) {
				player.QuickSpawnItem(player.GetSource_OpenItem(Type), soul);
			}

			foreach (int charm in Content.Items.Accessories.WastelandAccessoryCatalog.BagDrops(BossIndex)) {
				player.QuickSpawnItem(player.GetSource_OpenItem(Type), charm);
			}

			// 3) 专属武器：随机一件 + 随机原版词条（可在哥布林工匠重铸）
			List<int> pool = ExclusiveWeapons;

			if (pool.Count == 0 || PrefixPool.Length == 0) {
				return;
			}

			for (int i = 0; i < EquipmentCount; i++) {
				Item item = new Item();
				item.SetDefaults(pool[Main.rand.Next(pool.Count)]);
				item.Prefix(PrefixPool[Main.rand.Next(PrefixPool.Length)]);

				player.QuickSpawnItem(player.GetSource_OpenItem(Type), item, 1);
			}
		}
	}

	/// <summary>
	/// 原版词条的分档参考（数值强度从低到高），给各时期 Boss 的掉落袋挑词条用。
	/// </summary>
	public static class WastelandPrefixTiers
	{
		/// <summary>前期（史莱姆王~骷髅王）：常见的通用强化词条。</summary>
		public static readonly int[] Early = {
			PrefixID.Keen, PrefixID.Superior, PrefixID.Forceful, PrefixID.Strong,
			PrefixID.Godly, PrefixID.Demonic
		};

		/// <summary>中期（血肉墙~世纪之花）。</summary>
		public static readonly int[] Mid = {
			PrefixID.Superior, PrefixID.Godly, PrefixID.Demonic, PrefixID.Deadly,
			PrefixID.Unreal, PrefixID.Mythical, PrefixID.Ruthless
		};

		/// <summary>后期（世纪之花之后~月亮领主前）。</summary>
		public static readonly int[] Late = {
			PrefixID.Legendary, PrefixID.Unreal, PrefixID.Mythical, PrefixID.Ruthless,
			PrefixID.Godly, PrefixID.Demonic
		};
	}

	/// <summary>
	/// 灵魂碎片的收集判定（供袋子与对话共用）。
	/// </summary>
	public static class WastelandSoulHelper
	{
	}
}
