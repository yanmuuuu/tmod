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
	// 「锈蚀」线 · 盗贼（投掷）武器（前期 / 铁砧）
	// 全部可堆叠 999、投出即消耗（沿用基类 WastelandWeaponKit.Rogue 的形态）。
	//   · RustShuriken     单枚直线手里剑（穿透 3）
	//   · ScrapGrenade     抛物线手雷（定时/落地爆炸，范围伤害）
	//   · RustShurikenEX   一次三枚扇形手里剑（穿透 4）
	// ====================================================================================

	/// <summary>
	/// Rust Shuriken（锈蚀手里剑）
	/// <para/>定位：前期走位投掷。单枚 13 伤害、使用时间 13、穿透 3 个敌人；
	/// 介于 Throwing Knife(12) 与 Bone(20) 之间，一次合成出 75 枚，属于「不心疼地丢」的消耗品。
	/// 手感：速度快、有轻微下坠，需要提前量；比清道夫的裂片更直。
	/// </summary>
	public class RustShuriken : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 13;
		protected override int UseTime => 13;
		protected override float Knockback => 2f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(copper: 12);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustShurikenProj>();
		protected override float ShootSpeed => 13f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
			Item.width = 18;
			Item.height = 18;
			Item.UseSound = SoundID.Item19;
		}

		public override void AddRecipes()
		{
			CreateRecipe(75)
				.AddIngredient(ItemID.IronBar, 4)
				.AddIngredient(ItemID.Wood, 4)
				.AddIngredient(ItemID.StoneBlock, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe(75)
				.AddIngredient(ItemID.LeadBar, 4)
				.AddIngredient(ItemID.Wood, 4)
				.AddIngredient(ItemID.StoneBlock, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Scrap Grenade（废料手雷）
	/// <para/>定位：前期唯一的大范围投掷。直接命中 34 伤害、爆炸范围 60x60、使用时间 30、一次做 12 颗。
	/// 爆炸比原版 Grenade(60) 弱一档（因为「用废料就能量产」），但引信更短（1.7 秒）、落点好算；
	/// 刻意**不会伤到自己**（弹幕 friendly=true 只打敌人），属于纯收益的清群手段。
	/// </summary>
	public class ScrapGrenade : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 34;
		protected override int UseTime => 30;
		protected override float Knockback => 6f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(silver: 8);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.ScrapGrenadeProj>();
		protected override float ShootSpeed => 9f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
			Item.width = 18;
			Item.height = 18;
			Item.UseSound = SoundID.Item1;
		}

		public override void AddRecipes()
		{
			CreateRecipe(12)
				.AddIngredient(ItemID.Gel, 8)
				.AddIngredient(ItemID.IronBar, 6)
				.AddIngredient(ItemID.Wood, 4)
				.AddIngredient(ItemID.StoneBlock, 6)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe(12)
				.AddIngredient(ItemID.Gel, 8)
				.AddIngredient(ItemID.LeadBar, 6)
				.AddIngredient(ItemID.Wood, 4)
				.AddIngredient(ItemID.StoneBlock, 6)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Rust Shuriken MK-II（锈蚀手里剑 · 强化型）
	/// <para/>定位：手里剑的强化版，**继承 <see cref="RustShuriken"/>**。
	/// 单枚 19 伤害、使用时间 11，一次**扇形甩出三枚**（各偏 9 度、穿透 4 个敌人）；
	/// 贴脸三枚全中约 57 点，是前期最强的爆发投掷，但对远处小型目标容易只中一枚。
	/// </summary>
	public class RustShurikenEX : RustShuriken
	{
		protected override int Damage => 19;
		protected override int UseTime => 11;
		protected override float Knockback => 2.5f;
		protected override int SellPrice => Item.sellPrice(silver: 25);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustShurikenProjEX>();
		protected override float ShootSpeed => 14f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 20;
			Item.height = 20;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			// 一次三枚：中间一枚笔直，两侧各偏 9 度
			for (int i = -1; i <= 1; i++) {
				Vector2 spread = velocity.RotatedBy(MathHelper.ToRadians(9f * i));

				Projectile.NewProjectile(source, position, spread, ShootType, damage, knockback, player.whoAmI, i);
			}

			return false;
		}

		public override void AddRecipes()
		{
			// 25 枚基础手里剑 + 4 个精钢锭 → 75 枚强化手里剑
			CreateRecipe(75)
				.AddIngredient<RustShuriken>(25)
				.AddIngredient<SalvagedSteelBar>(4)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
