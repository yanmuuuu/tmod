using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;
using WastelandSoul.Content.Items.Weapons.Rust;
using WastelandSoul.Content.Projectiles.UpgradeTrees;

namespace WastelandSoul.Content.Items.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树（二）· 远程武器：废料霰弹枪 → 精钢散射枪 → 灰烬之心火铳
	//
	// 三级差异（**形态跟着换，不是同一个弹幕调数值**）：
	//   废料霰弹枪（已有）  单颗 12 / 4 颗 / 散布 14° / 使用 34 / 弹丸命中即消失
	//   精钢散射枪          单颗 17 / 6 颗 / 散布 10° / 使用 30 / 弹丸**穿透 2**、飞行更快
	//   灰烬之心火铳        单颗 26 / 3 颗 / 散布 6°  / 使用 26 / 弹丸穿透 3、命中**点燃 4 秒**，
	//                       速度是霰弹的 1.6 倍——从"贴脸喷"变成"中距离点名"
	//
	// 三级各有自己的配方（在合成界面可见；物品介绍里**不写**制作方法）。
	// ====================================================================================

	/// <summary>
	/// Salvaged Steel Scattergun（精钢散射枪）
	/// <para/>定位：霰弹枪的第一档进阶，**继承 <see cref="ScrapShotgun"/>** 复用多弹丸逻辑。
	/// 单颗 17、一次 6 颗（全中 102）、散布收紧到 10 度、使用时间 30；
	/// 弹丸换成穿透 2 的精钢弹，且 <c>extraUpdates = 1</c>，中距离也不至于全飘。
	/// 与 <c>ScrapShotgunEX</c> 区分：那把靠"撞墙反弹"打拐角，这把靠"穿透 + 弹速"打正面。
	/// </summary>
	public class SalvagedSteelScattergun : ScrapShotgun
	{
		protected override int Damage => 17;
		protected override int UseTime => 30;
		protected override float Knockback => 5.5f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 2, silver: 50);
		protected override int ShootType => ModContent.ProjectileType<SteelBuckshot>();
		protected override float ShootSpeed => 13f;

		protected override int PelletCount => 6;
		protected override float SpreadDegrees => 10f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 48;
			Item.height = 20;
		}

		public override void AddRecipes()
		{
			// 进阶件：上一级枪 + 精钢锭扩膛 + 芯片
			CreateRecipe()
				.AddIngredient<ScrapShotgun>()
				.AddIngredient<SalvagedSteelBar>(10)
				.AddIngredient<Chip>(4)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Ash Heart Flechette（灰烬之心火铳）
	/// <para/>定位：这条树的终阶。单颗 26、一次 3 颗、散布 6 度、使用时间 26，稀有度黄绿；
	/// 发射的燃烬箭弹**穿透 3** 并点燃 4 秒，弹速比霰弹高 60%，是"中期打单体/点燃流"的选择。
	/// 弹丸数量少而精，所以它的定位与基础霰弹枪的"贴脸喷一片"完全不同。
	/// </summary>
	public class AshHeartFlechette : SalvagedSteelScattergun
	{
		protected override int Damage => 26;
		protected override int UseTime => 26;
		protected override float Knockback => 4.5f;
		protected override int Rarity => WastelandRarityTiers.MidLate;
		protected override int SellPrice => Item.sellPrice(gold: 5);
		protected override int ShootType => ModContent.ProjectileType<EmberFlechette>();
		protected override float ShootSpeed => 19f;

		protected override int PelletCount => 3;
		protected override float SpreadDegrees => 6f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 50;
			Item.height = 22;
			Item.UseSound = SoundID.Item38;
		}

		public override void AddRecipes()
		{
			// 终阶件：进阶枪 + 灰烬之心合金锭 + 灰烬结晶做燃烧药室
			CreateRecipe()
				.AddIngredient<SalvagedSteelScattergun>()
				.AddIngredient<AshHeartAlloyBar>(8)
				.AddIngredient<AshCrystal>(5)
				.AddIngredient(ItemID.CursedFlame, 6)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();

			// 猩红世界用脓液替代诅咒焰（与其它包的写法保持一致）
			CreateRecipe()
				.AddIngredient<SalvagedSteelScattergun>()
				.AddIngredient<AshHeartAlloyBar>(8)
				.AddIngredient<AshCrystal>(5)
				.AddIngredient(ItemID.Ichor, 6)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
