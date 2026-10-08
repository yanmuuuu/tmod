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
	// 「锈蚀」线 · 法师武器（前期 / 铁砧）
	// 数值对齐紫水晶法杖(14) ~ 钻石法杖(23) 这一档，靠「手感差异」区分：
	//   点射杖（快、低蓝耗、单发）/ 酸液法典（三连散射、抛物线、附带中毒）/ 强化点射杖（穿透+追踪、蓝耗更高）
	// ====================================================================================

	/// <summary>
	/// Rust Bolt Wand（锈蚀火花杖）
	/// <para/>定位：前期点射过渡杖。伤害 15、使用时间 20、耗蓝 7，介于紫水晶(14)与黄玉(15)之间；
	/// 弹幕是 14x14 的直飞火花团（无重力、穿透 1、带一点光照），射速比同伤害法杖略快，适合点掉小怪。
	/// </summary>
	public class RustBoltWand : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 15;
		protected override int UseTime => 20;
		protected override float Knockback => 3f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(silver: 80);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustBolt>();
		protected override float ShootSpeed => 9.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 7);
			Item.width = 32;
			Item.height = 32;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Wood, 14)
				.AddIngredient(ItemID.IronBar, 8)
				.AddIngredient(ItemID.Gel, 8)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.Wood, 14)
				.AddIngredient(ItemID.LeadBar, 8)
				.AddIngredient(ItemID.Gel, 8)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Rust Acid Tome（锈蚀酸液法典）
	/// <para/>定位：对群清小怪的法典。单发 11、一次散出 3 团酸液（各 11 伤害）、使用时间 30、耗蓝 12。
	/// 酸液走抛物线（会下坠），命中附「中毒」3 秒；贴脸打满 33 点、远距离只中 1~2 团，鼓励中距离抛投。
	/// 单点伤害明显低于同期法杖，不会抢走钻石法杖的直线输出位。
	/// </summary>
	public class RustAcidTome : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 11;
		protected override int UseTime => 30;
		protected override float Knockback => 2.5f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(silver: 90);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustAcidSpray>();
		protected override float ShootSpeed => 10f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 12);
			Item.width = 28;
			Item.height = 28;
			Item.UseSound = SoundID.Item20;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			// 三连散射：主弹 + 上下各偏 7 度，全部走同一套抛物线
			for (int i = -1; i <= 1; i++) {
				Vector2 spread = velocity.RotatedBy(MathHelper.ToRadians(7f * i));

				Projectile.NewProjectile(source, position, spread, type, damage, knockback, player.whoAmI, i * 0.5f);
			}

			return false;   // 自己生成，阻止默认单发
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Book, 1)
				.AddIngredient<SalvagedSteelChunk>(6)
				.AddIngredient(ItemID.Gel, 14)
				.AddIngredient(ItemID.Wood, 8)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Rust Bolt Wand MK-II（锈蚀火花杖 · 强化型）
	/// <para/>定位：点射杖的强化版，**继承 <see cref="RustBoltWand"/>**。
	/// 伤害 22、使用时间 17、耗蓝 9；火花团变成 16x16 的穿透弹（穿透 3），并在 340 像素内轻微追踪最近敌人。
	/// 对单排小怪很舒服，但蓝耗是基础款的 1.3 倍，长时间输出需要魔力药水支撑。
	/// </summary>
	public class RustBoltWandEX : RustBoltWand
	{
		protected override int Damage => 22;
		protected override int UseTime => 17;
		protected override float Knockback => 3.5f;
		protected override int SellPrice => Item.sellPrice(gold: 1, silver: 80);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustBoltEX>();
		protected override float ShootSpeed => 10.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 34;
			Item.height = 34;
			WastelandWeaponKit.Magic(Item, 9);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RustBoltWand>()
				.AddIngredient<SalvagedSteelBar>(6)
				.AddIngredient<Chip>(8)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
