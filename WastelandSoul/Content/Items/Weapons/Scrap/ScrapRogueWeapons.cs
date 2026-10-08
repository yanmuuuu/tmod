using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 盗贼（投掷）武器（前期后段）
	//   ScrapBuzzsaw   落地后原地旋转的锯片（持续伤害，适合封路）—— **掉落专属，无配方**
	//   ScrapChakram   飞出再飞回的四刃环（穿透 6，可来回命中）
	// ====================================================================================

	/// <summary>
	/// Scrap Buzzsaw（废铁锯片）
	/// <para/>定位：封路型投掷。伤害 26、使用时间 20；锯片飞出后撞到方块就**卡在原地旋转 5 秒**，
	/// 每 0.33 秒对同一敌人结算一次（本地无敌帧 20），可以提前丢在狭窄通道里当陷阱。
	/// 本件是**掉落专属**（无合成配方），由掉落包接入时期敌人的掉落表。
	/// </summary>
	public class ScrapBuzzsaw : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 26;
		protected override int UseTime => 20;
		protected override float Knockback => 4f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(silver: 30);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scrap.ScrapBuzzsawProj>();
		protected override float ShootSpeed => 12f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
			Item.width = 24;
			Item.height = 24;
		}

		// 掉落专属：**不写 AddRecipes()**。
	}

	/// <summary>
	/// Scrap Chakram（废铁四刃环）
	/// <para/>定位：可反复命中的投掷。伤害 30、使用时间 22；飞出 30 帧后自动回到手上（穿透 6、本地无敌帧 10），
	/// 来回两趟都能打，配合走位可以「贴身来回刮」。与钢筋回旋镖相比伤害更高、速度更快，但回程更急、更难控制。
	/// </summary>
	public class ScrapChakram : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 30;
		protected override int UseTime => 22;
		protected override float Knockback => 5f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 1);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scrap.ScrapChakramProj>();
		protected override float ShootSpeed => 13.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
			Item.width = 22;
			Item.height = 22;
		}

		public override void AddRecipes()
		{
			CreateRecipe(25)
				.AddIngredient<SalvagedSteelBar>(8)
				.AddIngredient<ArchivistFragment>(5)
				.AddIngredient(ItemID.Bone, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
