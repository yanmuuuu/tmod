using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss4Fireplace
{
	/// <summary>
	/// Fireplace Guardian Scepter（壁炉守卫 · A 线 · 可合成）
	/// <para/>设计定位：【补全】原设计数据在此处被截断，按同 Boss 同职业 A/B 线的规律（B 线 = A 线 × 1.25）补写。
	/// <para/>制作站点：秘银砧（血肉墙之后装备站点）
	/// </summary>
	public class FireplaceMageWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 74;
		protected override int UseTime => 18;
		protected override float Knockback => 5.0f;
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Fireplace.FireplaceMageProjectile>();

		protected override float ShootSpeed => 10.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 18);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<FireplaceAlloyBar>(12)
				.AddIngredient(ItemID.ChlorophyteBar, 10)
				.AddIngredient(ItemID.Ectoplasm, 14)
				.AddIngredient(ItemID.HallowedBar, 8)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
