using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 法师武器（前期后段 / 铁砧）
	//   ScrapNovaStaff    慢速高伤「新星球」：飞行 1.6 秒或命中后炸成 4 片碎片
	//   ScrapNovaStaffEX  强化版（继承）：44 伤害、炸成 6 片，蓝耗更高
	// 对照：同期 Demon Scythe(35) / Water Bolt(17，穿透) —— 这把是「一发打群」的定位，
	//       单点持续输出不如 Water Bolt，胜在爆发与覆盖面。
	// ====================================================================================

	/// <summary>
	/// Scrap Nova Staff（废铁新星杖）
	/// <para/>定位：前期后段的范围法杖。母弹 30 伤害、飞行速度慢（6.5）、耗蓝 18；
	/// 命中或飞满 1.6 秒后炸成 **4 片追踪碎片**（每片 10 伤害、能穿透 2 个敌人）。
	/// 手感：先「扔出去」，再靠碎片收尾，适合打排成一列的敌人；打单体不如同期法杖稳。
	/// </summary>
	public class ScrapNovaStaff : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 30;
		protected override int UseTime => 36;
		protected override float Knockback => 4f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 2, silver: 40);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scrap.ScrapNova>();
		protected override float ShootSpeed => 6.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 18);
			Item.width = 34;
			Item.height = 34;
			Item.UseSound = SoundID.Item43;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient<ArchivistFragment>(6)
				.AddIngredient(ItemID.Bone, 15)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Scrap Nova Staff MK-II（废铁新星杖 · 强化型）
	/// <para/>定位：新星杖的强化版，**继承 <see cref="ScrapNovaStaff"/>**。
	/// 母弹 44 伤害、炸成 **6 片**碎片（每片 14 伤害、穿透 2）、耗蓝 22、使用时间 32。
	/// 一发满命中理论伤害极高，但蓝耗也高，属于「一掌清一片」的前期后段爆发法器。
	/// </summary>
	public class ScrapNovaStaffEX : ScrapNovaStaff
	{
		protected override int Damage => 44;
		protected override int UseTime => 32;
		protected override float Knockback => 4.5f;
		protected override int SellPrice => Item.sellPrice(gold: 4);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scrap.ScrapNovaEX>();
		protected override float ShootSpeed => 7f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 36;
			Item.height = 36;
			WastelandWeaponKit.Magic(Item, 22);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ScrapNovaStaff>()
				.AddIngredient<SalvagedSteelBar>(10)
				.AddIngredient<ArchivistFragment>(10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
