using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树（八）· 饰品（召唤向）：拾荒者芯片 → 强化拾荒者芯片 → 废土主宰核心
	//
	// 三级差异（每级都是"新的机制"，不是同一条效果加数字）：
	//   拾荒者芯片        召唤伤害 +8%；**物品拾取范围翻倍**（treasureMagnet）+ 红心磁吸（lifeMagnet）
	//   强化拾荒者芯片    召唤 +12%；再补上
	//                     **金币磁吸**（goldRing）与**魔力星磁吸**（manaMagnet）+ 5% 伤害减免
	//   废土主宰核心      **仆从栏位 +1**、召唤 +15%；8% 伤害减免；
	//                     **幸运 +0.3（掉宝率）**，并保留上两级的全部磁吸
	//
	// 全部派生自 WastelandAccessory（每帧重设，脱下即失效）。
	// 每级各有自己的配方（在合成界面可见；物品介绍里**不写**制作方法）。
	// ====================================================================================

	/// <summary>
	/// Scavenger Chip（拾荒者芯片）
	/// <para/>定位：这条饰品的树根。一边歪给召唤，一边解决"捡东西"这件小事：
	/// 召唤伤害 +8%（各职业饰品是 6%），并把**物品拾取范围**拉大
	/// （<c>treasureMagnet</c>）、让红心从更远处飞过来（<c>lifeMagnet</c>）。
	/// </summary>
	public class ScavengerChip : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int SellPrice => Item.sellPrice(gold: 1, silver: 50);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Summon) += 0.08f;

			// 拾取范围：物品 + 红心
			player.treasureMagnet = true;
			player.lifeMagnet = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Chip>(4)
				.AddIngredient<CircuitBoard>(2)
				.AddIngredient<SalvagedSteelBar>(4)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Reinforced Scavenger Chip（强化拾荒者芯片）
	/// <para/>进阶件：多焊了一层屏蔽罩——
	/// 召唤 +12%；磁吸再补金币（<c>goldRing</c>）
	/// 与魔力星（<c>manaMagnet</c>），并额外给 5% 伤害减免。
	/// </summary>
	public class ReinforcedScavengerChip : ScavengerChip
	{
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int SellPrice => Item.sellPrice(gold: 3, silver: 50);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			// 先拿树根那一条（+8% 与两段磁吸），再换成进阶数值，避免同一条加成叠两次
			base.UpdateWastelandAccessory(player, hideVisual);

			player.GetDamage(DamageClass.Summon) += 0.04f;    // 0.08 -> 0.12

			player.goldRing = true;
			player.manaMagnet = true;

			player.endurance += 0.05f;
		}

		public override void AddRecipes()
		{
			// 进阶件：树根 + 精钢外壳 + 归档者的索引碎片做寻物算法
			CreateRecipe()
				.AddIngredient<ScavengerChip>()
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient<Chip>(6)
				.AddIngredient<ArchivistFragment>(3)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Wasteland Overlord Core（废土主宰核心）
	/// <para/>终阶：从"芯片"变成一整颗核心，第一次给出**仆从栏位 +1**（这是树根两级都没有的），
	/// 召唤 +15%；8% 伤害减免；再补一条**幸运 +0.3**（掉宝率）。
	/// 保留前两级的全部磁吸，所以戴它就不用再为"捡东西"腾饰品栏。
	/// </summary>
	public class WastelandOverlordCore : ReinforcedScavengerChip
	{
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int SellPrice => Item.sellPrice(gold: 8);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			// 先拿进阶件那一条（+12% / 磁吸 / 5% 减免），再换终阶数值
			base.UpdateWastelandAccessory(player, hideVisual);

			player.GetDamage(DamageClass.Summon) += 0.03f;    // 0.12 -> 0.15

			player.maxMinions += 1;
			player.endurance += 0.03f;                        // 0.05 -> 0.08
			player.luck += 0.3f;
		}

		public override void AddRecipes()
		{
			// 终阶件：进阶芯片 + 灰烬之心合金锭做核心外壳 + 灰烬结晶 + 电路板重写寻物算法
			CreateRecipe()
				.AddIngredient<ReinforcedScavengerChip>()
				.AddIngredient<AshHeartAlloyBar>(10)
				.AddIngredient<AshCrystal>(6)
				.AddIngredient<CircuitBoard>(5)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
