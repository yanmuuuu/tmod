using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
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
	// 升级衍生树（五）· 魔法武器：锈蚀火花杖 → 精钢弧光法杖 → 壁炉守卫霜语法典
	//
	// 三级差异（**弹幕形态每级都换**）：
	//   锈蚀火花杖（已有）  伤害 15 / 使用 20 / 耗蓝 7  / 单发直飞火花，穿透 1
	//   精钢弧光法杖        伤害 24 / 使用 19 / 耗蓝 9  / **一次三发扇形弧光**，穿透 2、命中带电
	//   壁炉守卫霜语法典    伤害 42 / 使用 17 / 耗蓝 16 / **一次五发追踪霜矛**，穿透 3、命中霜冻
	//
	// 三级各有自己的配方（在合成界面可见；物品介绍里**不写**制作方法）。
	// ====================================================================================

	/// <summary>
	/// Salvaged Steel Arc Wand（精钢弧光法杖）
	/// <para/>定位：火花杖的第一档进阶。伤害 24、使用时间 19、耗蓝 9；
	/// 一次呈 9 度扇形打出**三发弧光**（各自穿透 2 个敌人），命中附加 2 秒带电。
	/// 单发伤害仍低于同期钻石法杖，但"三发同时铺出去"让它清小怪明显更快。
	/// </summary>
	public class SalvagedSteelArcWand : WastelandClassWeapon
	{
		/// <summary>耗蓝（子类会调高）。</summary>
		protected virtual int ManaCost => 9;

		/// <summary>一次打出的弧光数量（奇数，围绕鼠标方向对称展开）。</summary>
		protected virtual int BoltCount => 3;

		/// <summary>相邻两发的夹角（度）。</summary>
		protected virtual float SpreadDegrees => 9f;

		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 24;
		protected override int UseTime => 19;
		protected override float Knockback => 3.5f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 2);
		protected override int ShootType => ModContent.ProjectileType<SteelArcBolt>();
		protected override float ShootSpeed => 11f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, ManaCost);
			Item.width = 36;
			Item.height = 36;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			int half = BoltCount / 2;

			for (int index = -half; index <= half; index++) {
				Vector2 spread = velocity.RotatedBy(MathHelper.ToRadians(index * SpreadDegrees));
				Projectile.NewProjectile(source, position, spread, ShootType, damage, knockback, player.whoAmI);
			}

			return false;
		}

		public override void AddRecipes()
		{
			// 进阶件：上一级法杖 + 精钢锭做导弧管 + 芯片 + 旧世界电路板
			CreateRecipe()
				.AddIngredient<RustBoltWand>()
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient<Chip>(3)
				.AddIngredient<CircuitBoard>(2)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Hearth Guardian Frost Codex（壁炉守卫霜语法典）
	/// <para/>定位：这条树的终阶，稀有度红色（月亮领主之前）。
	/// 伤害 42、使用时间 17、耗蓝 16，一次打出**五发追踪霜矛**（各自穿透 3、命中霜冻 4 秒）。
	/// 与精钢弧光杖的区别：从"铺一片电弧"变成"五枚自动找目标的冷焰"，单体与清场都能打。
	/// </summary>
	public class HearthGuardFrostCodex : SalvagedSteelArcWand
	{
		protected override int ManaCost => 16;
		protected override int BoltCount => 5;
		protected override float SpreadDegrees => 13f;

		protected override int Damage => 42;
		protected override int UseTime => 17;
		protected override float Knockback => 4.5f;
		protected override int Rarity => WastelandRarityTiers.Late;
		protected override int SellPrice => Item.sellPrice(gold: 6);
		protected override int ShootType => ModContent.ProjectileType<FrostLance>();
		protected override float ShootSpeed => 13f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 40;
			Item.height = 40;
			Item.UseSound = SoundID.Item28;
		}

		public override void AddRecipes()
		{
			// 终阶件：进阶杖 + 壁炉合金锭外壳 + 冷却液做冷源 + 灰烬结晶稳定弹道
			CreateRecipe()
				.AddIngredient<SalvagedSteelArcWand>()
				.AddIngredient<FireplaceAlloyBar>(8)
				.AddIngredient<Coolant>(6)
				.AddIngredient<AshCrystal>(4)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
