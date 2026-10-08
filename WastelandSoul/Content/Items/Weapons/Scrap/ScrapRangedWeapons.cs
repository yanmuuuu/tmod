using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 射手武器（前期后段 / 铁砧）
	//   ScrapRailgun     一枪一发的高穿透电磁弹（42 伤害、穿透 6、使用时间 44）
	//   ScrapRailgunEX   强化版（继承）：56 伤害、穿透 8、射速更快
	// 两把都消耗火枪子弹（AmmoID.Bullet），但发射自己的弹幕。
	// ====================================================================================

	/// <summary>
	/// Scrap Railgun（废铁电磁炮）
	/// <para/>定位：前期后段的「一发穿透」。伤害 42、使用时间 44、击退 8；
	/// 弹幕是 14x8 的钢针（速度 24、穿透 **6** 个敌人、每 3 帧额外推进一次也就是飞得极快）。
	/// 单发 DPS 低于枪类连射，但面对排成一条的敌人时一发能吃满，是「卡好角度再开枪」的武器。
	/// </summary>
	public class ScrapRailgun : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 42;
		protected override int UseTime => 44;
		protected override float Knockback => 8f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 4);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scrap.ScrapRailSlug>();
		protected override float ShootSpeed => 24f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
			Item.width = 48;
			Item.height = 22;
			Item.useAnimation = 50;     // 抬手蓄力比开火慢一点，视觉上有「充能」感
			Item.UseSound = SoundID.Item11;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity, ShootType, damage, knockback, player.whoAmI);

			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(12)
				.AddIngredient<ArchivistFragment>(8)
				.AddIngredient(ItemID.IronBar, 20)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(12)
				.AddIngredient<ArchivistFragment>(8)
				.AddIngredient(ItemID.LeadBar, 20)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Scrap Railgun MK-II（废铁电磁炮 · 强化型）
	/// <para/>定位：电磁炮的强化版，**继承 <see cref="ScrapRailgun"/>**。
	/// 伤害 56、使用时间 40、弹幕穿透 **8** 个敌人、速度 27；代价是配方要吃掉大量归档者残片。
	/// 属于「血肉墙之前最强的单体远程」，但射速仍然很慢，面对快速小怪容易空枪。
	/// </summary>
	public class ScrapRailgunEX : ScrapRailgun
	{
		protected override int Damage => 56;
		protected override int UseTime => 40;
		protected override float Knockback => 9f;
		protected override int SellPrice => Item.sellPrice(gold: 6);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scrap.ScrapRailSlugEX>();
		protected override float ShootSpeed => 27f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 50;
			Item.height = 24;
			Item.useAnimation = 46;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ScrapRailgun>()
				.AddIngredient<SalvagedSteelBar>(14)
				.AddIngredient<ArchivistFragment>(12)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
