using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Rust
{
	// ====================================================================================
	// 「锈蚀」线 · 射手武器（前期 / 铁砧）
	// 全部消耗火枪子弹（AmmoID.Bullet），但**发射自己的弹幕**（在 Shoot 里手动生成并返回 false）。
	// 三把的区别：霰弹（一次 4 颗、散布大）/ 射钉枪（极快、单发低伤、穿透 2）/ 强化霰弹（5 颗、能弹一次墙）
	// ====================================================================================

	/// <summary>
	/// Scrap Shotgun（废料霰弹枪）
	/// <para/>定位：前期近距清怪。每颗弹丸 12 伤害、一次 4 颗（全中 48），散布 14 度、使用时间 34；
	/// 单颗伤害低于 Boomstick(19) 的弹丸，靠「弹丸多 + 消耗同一发子弹」形成贴脸爆发，远距离几乎打不满。
	/// </summary>
	public class ScrapShotgun : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 12;
		protected override int UseTime => 34;
		protected override float Knockback => 5f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(gold: 1);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.ScrapPellet>();
		protected override float ShootSpeed => 11.5f;

		/// <summary>弹丸数量。</summary>
		protected virtual int PelletCount => 4;

		/// <summary>散布角度（度）。</summary>
		protected virtual float SpreadDegrees => 14f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
			Item.width = 44;
			Item.height = 18;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			// 一次散出 PelletCount 颗弹丸；弹药仍按 Item.useAmmo 正常消耗
			for (int i = 0; i < PelletCount; i++) {
				Vector2 spread = velocity.RotatedByRandom(MathHelper.ToRadians(SpreadDegrees));
				Vector2 speed = spread * Main.rand.NextFloat(0.88f, 1.12f);

				Projectile.NewProjectile(source, position, speed, ShootType, damage, knockback, player.whoAmI);
			}

			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.IronBar, 14)
				.AddIngredient(ItemID.Wood, 16)
				.AddIngredient(ItemID.Gel, 8)
				.AddIngredient<SalvagedSteelChunk>(6)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.LeadBar, 14)
				.AddIngredient(ItemID.Wood, 16)
				.AddIngredient(ItemID.Gel, 8)
				.AddIngredient<SalvagedSteelChunk>(6)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Rust Nailgun（锈蚀射钉枪）
	/// <para/>定位：前期持续输出。伤害 8、使用时间 7（和 Minishark 同档射速），散布 6 度、弹幕穿透 2 个敌人。
	/// 单发很低，靠「钉子在人群里串起来」体现价值；没有击退（1.5），清小史莱姆很爽，打 Boss 只是稳定磨血。
	/// </summary>
	public class RustNailgun : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 8;
		protected override int UseTime => 7;
		protected override float Knockback => 1.5f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(gold: 1, silver: 20);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustNail>();
		protected override float ShootSpeed => 15f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
			Item.width = 36;
			Item.height = 18;
			Item.UseSound = SoundID.Item11;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			Vector2 spread = velocity.RotatedByRandom(MathHelper.ToRadians(6f));

			Projectile.NewProjectile(source, position, spread, ShootType, damage, knockback, player.whoAmI);

			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(10)
				.AddIngredient(ItemID.IronBar, 10)
				.AddIngredient(ItemID.Gel, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(10)
				.AddIngredient(ItemID.LeadBar, 10)
				.AddIngredient(ItemID.Gel, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Scrap Shotgun MK-II（废料霰弹枪 · 强化型）
	/// <para/>定位：霰弹枪的强化版，**继承 <see cref="ScrapShotgun"/>**：弹丸 5 颗、单颗 15 伤害、
	/// 散布收紧到 11 度、使用时间 30。弹丸换成能**弹一次墙**的强化弹（穿透 2），打墙角的敌人特别舒服。
	/// </summary>
	public class ScrapShotgunEX : ScrapShotgun
	{
		protected override int Damage => 15;
		protected override int UseTime => 30;
		protected override float Knockback => 5.5f;
		protected override int SellPrice => Item.sellPrice(gold: 2);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.ScrapPelletEX>();
		protected override float ShootSpeed => 12.5f;
		protected override int PelletCount => 5;
		protected override float SpreadDegrees => 11f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 46;
			Item.height = 20;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ScrapShotgun>()
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient(ItemID.Gel, 12)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
