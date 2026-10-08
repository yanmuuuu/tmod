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
	// 升级衍生树（一）· 近战武器：锈蚀砍刀 → 精钢锋刃 → 归档者裁决刃
	//
	// 三级差异（**不是纯数值 +10%**）：
	//   锈蚀砍刀（已有）  伤害 16 / 使用 21 / 甩出锈铁碎片：穿透 2、无附加效果
	//   精钢锋刃          伤害 28 / 使用 17 / 甩出精钢碎片：穿透 4、命中**流血 3 秒**
	//   归档者裁决刃      伤害 44 / 使用 14 / 甩出裁决波：穿透 6、命中**削防（脓液）**，
	//                     并且每次命中都会溅出 1 片追踪碎片（最多 4 片）
	//
	// 三级各有自己的配方（在合成界面可见；物品介绍里**不写**制作方法）。
	// ====================================================================================

	/// <summary>
	/// Salvaged Steel Saber（精钢锋刃）
	/// <para/>定位：锈蚀砍刀的第一档进阶。伤害 28、使用时间 17（比基础款快 4 帧）、击退 6.4；
	/// 甩出的是 26x26 的精钢碎片，**穿透 4 个敌人**并让被划到的目标流血 3 秒。
	/// 对齐原版：伤害落在 Muramasa(19) 与 Blade of Grass(28) 之间，形态是"快刀推线"。
	/// </summary>
	public class SalvagedSteelSaber : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 28;
		protected override int UseTime => 17;
		protected override float Knockback => 6.4f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 2);
		protected override int ShootType => ModContent.ProjectileType<SteelEdgeShard>();
		protected override float ShootSpeed => 12.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
			Item.width = 40;
			Item.height = 40;
			Item.scale = 1.10f;
		}

		public override void AddRecipes()
		{
			// 进阶件：上一级武器 + 精钢锭回炉 + 芯片校准刃口
			CreateRecipe()
				.AddIngredient<RustCleaver>()
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient<Chip>(3)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Archivist's Verdict（归档者裁决刃）
	/// <para/>定位：这条树的终阶。伤害 44、使用时间 14、击退 7.2，稀有度黄绿（世纪之花之后）；
	/// 甩出的裁决波 46x46、**穿透 6** 且命中削防（脓液 4 秒），每次命中溅出 1 片追踪碎片。
	/// 与原版同期相比：不如 Terra Blade 的面伤，但"一刀开一条缝 + 自动追猎残血"是它的招牌。
	/// </summary>
	public class ArchivistVerdictBlade : SalvagedSteelSaber
	{
		protected override int Damage => 44;
		protected override int UseTime => 14;
		protected override float Knockback => 7.2f;
		protected override int Rarity => WastelandRarityTiers.MidLate;
		protected override int SellPrice => Item.sellPrice(gold: 6);
		protected override int ShootType => ModContent.ProjectileType<VerdictWave>();
		protected override float ShootSpeed => 14f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 46;
			Item.height = 46;
			Item.scale = 1.20f;
			Item.UseSound = SoundID.Item71;
		}

		public override void AddRecipes()
		{
			// 终阶件：进阶件 + 灰烬之心合金锭重新开刃 + 归档者碎片刻录裁决指令
			CreateRecipe()
				.AddIngredient<SalvagedSteelSaber>()
				.AddIngredient<AshHeartAlloyBar>(8)
				.AddIngredient<ArchivistFragment>(6)
				.AddIngredient<AshCrystal>(4)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
