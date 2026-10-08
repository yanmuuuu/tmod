using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Rust
{
	// ====================================================================================
	// 「锈蚀」线 · 战士武器（前期 / 铁砧）
	// 风格：废土上拆下来的旧军用钢 + 木柄，伤害对齐 Iron Broadsword(12) ~ Light's Bane(17)。
	// 三把的区别：宽刃平A（挥砍+甩碎片）/ 悠悠球（持续贴身输出）/ 强化宽刃（碎片能穿透并带火星）。
	// ====================================================================================

	/// <summary>
	/// Rust Cleaver（锈蚀砍刀）
	/// <para/>定位：前期宽刃平A。伤害 16 落在 Iron Broadsword(12) 与 Light's Bane(17) 之间；
	/// 挥砍时朝鼠标甩出一片 18x18 的锈铁碎片（继承武器伤害、穿透 2 个敌人、不受重力影响）。
	/// 手感：比同期的剑略重（使用时间 21），击退偏高，适合卡位砍。
	/// </summary>
	public class RustCleaver : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 16;
		protected override int UseTime => 21;
		protected override float Knockback => 5.5f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(silver: 60);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustShardSlash>();
		protected override float ShootSpeed => 11.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
			Item.width = 34;
			Item.height = 34;
			Item.scale = 1.05f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(8)
				.AddIngredient(ItemID.IronBar, 10)
				.AddIngredient(ItemID.Wood, 12)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(8)
				.AddIngredient(ItemID.LeadBar, 10)
				.AddIngredient(ItemID.Wood, 12)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Scrap Yoyo（废料悠悠球）
	/// <para/>定位：前期贴身持续输出。伤害 14、线长 190、最高速度 10.5，介于 Wooden Yoyo(9) 与 Rally(14) 之间；
	/// 好处是「不用瞄准、几乎不耗操作」，代价是必须靠近敌人。旋转时掉铁屑粒子（只属于这把武器）。
	/// </summary>
	public class ScrapYoyo : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.MeleeNoSpeed;
		protected override int Damage => 14;
		protected override int UseTime => 25;
		protected override float Knockback => 3.5f;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int SellPrice => Item.sellPrice(silver: 70);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.ScrapYoyoProjectile>();
		protected override float ShootSpeed => 15f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 30;
			Item.height = 30;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.channel = true;          // 悠悠球必须按住不放
			Item.noUseGraphic = true;     // 手里不显示握持贴图
			Item.noMelee = true;
			Item.autoReuse = false;
			Item.UseSound = SoundID.Item1;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.IronBar, 12)
				.AddIngredient(ItemID.Wood, 8)
				.AddIngredient(ItemID.Gel, 6)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.LeadBar, 12)
				.AddIngredient(ItemID.Wood, 8)
				.AddIngredient(ItemID.Gel, 6)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Rust Cleaver MK-II（锈蚀砍刀 · 强化型）
	/// <para/>定位：砍刀的强化版，**继承 <see cref="RustCleaver"/>** 只改数值与弹幕。
	/// 伤害 23、使用时间 19、击退 6.2；甩出的是带火星的碎片（20x20、穿透 3、命中附带着火 2 秒）。
	/// 手感从「卡位砍」变成「边砍边推线」，但射速仍慢于同期短剑，不会取代剑类定位。
	/// </summary>
	public class RustCleaverEX : RustCleaver
	{
		protected override int Damage => 23;
		protected override int UseTime => 19;
		protected override float Knockback => 6.2f;
		protected override int SellPrice => Item.sellPrice(gold: 1, silver: 50);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Rust.RustShardSlashEX>();
		protected override float ShootSpeed => 13f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 36;
			Item.height = 36;
			Item.scale = 1.12f;
		}

		public override void AddRecipes()
		{
			// 强化版：拿基础款 + 精钢锭回炉
			CreateRecipe()
				.AddIngredient<RustCleaver>()
				.AddIngredient<SalvagedSteelBar>(6)
				.AddIngredient(ItemID.Gel, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
