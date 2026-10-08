using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 战士武器（前期后段 / 铁砧；材料来自精钢与归档者残片）
	// 数值对齐 Night's Edge(42) 之前的一档：27 ~ 30，靠「招式形态」区分：
	//   ScrapGreatsword  慢速重击 + 地面冲击波（穿透多目标）
	//   RebarBoomerang   钢筋回旋镖（飞出去再回来，可反复命中）
	// ====================================================================================

	/// <summary>
	/// Scrap Greatsword（废铁巨剑）
	/// <para/>定位：慢速重击。伤害 27、使用时间 32（明显慢）、击退 7；挥砍时在地面推出一道
	/// 40x24 的冲击波（继承武器伤害、穿透 5 个敌人、越飞越大但会迅速消散）。
	/// 手感：单体重击不如同期 Night's Edge，但「一剑扫一排」的清线效率是这条线最高的。
	/// </summary>
	public class ScrapGreatsword : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 27;
		protected override int UseTime => 32;
		protected override float Knockback => 7f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 2);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scrap.ScrapShockwave>();
		protected override float ShootSpeed => 7f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
			Item.width = 46;
			Item.height = 46;
			Item.scale = 1.3f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(10)
				.AddIngredient(ItemID.Bone, 20)
				.AddIngredient(ItemID.IronBar, 15)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(10)
				.AddIngredient(ItemID.Bone, 20)
				.AddIngredient(ItemID.LeadBar, 15)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}

	/// <summary>
	/// Rebar Boomerang（钢筋回旋镖）
	/// <para/>定位：可反复命中的投掷近战。伤害 22、使用时间 22；飞出 35 帧后自动回到手上，
	/// 全程穿透（本地无敌帧 12），来回都能打到同一个敌人，是「拉怪+消耗」的走位武器。
	/// 与 Enchanted Boomerang(17) 相比伤害更高但不会自动追踪，需要玩家自己站好位。
	/// </summary>
	public class RebarBoomerang : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 22;
		protected override int UseTime => 22;
		protected override float Knockback => 6f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 1, silver: 60);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scrap.RebarBoomerangProj>();
		protected override float ShootSpeed => 13f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Item.width = 30;
			Item.height = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;        // 伤害全在飞出去的钢筋上
			Item.noUseGraphic = true;
			Item.autoReuse = false;     // 原版回旋镖的手感：等它回来再丢下一发
			Item.UseSound = SoundID.Item19;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient(ItemID.IronBar, 10)
				.AddIngredient(ItemID.Wood, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			CreateRecipe()
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient(ItemID.LeadBar, 10)
				.AddIngredient(ItemID.Wood, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
