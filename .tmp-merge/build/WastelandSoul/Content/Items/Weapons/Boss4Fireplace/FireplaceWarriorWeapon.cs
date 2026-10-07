using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss4Fireplace
{
	/// <summary>
	/// Fireplace Guardian Greatblade（壁炉守卫 · A 线 · 可合成）
	/// <para/>设计定位：【补全】原设计数据在此处被截断，按同 Boss 同职业 A/B 线的规律（B 线 = A 线 × 1.25）补写。
	/// <para/>制作站点：秘银砧（血肉墙之后装备站点）
	/// </summary>
	public class FireplaceWarriorWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 90;
		protected override int UseTime => 23;
		protected override float Knockback => 7.0f;
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Fireplace.FireplaceWarriorProjectile>();

		protected override float ShootSpeed => 12.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<FireplaceAlloyBar>(12)
				.AddIngredient(ItemID.ChlorophyteBar, 14)
				.AddIngredient(ItemID.HallowedBar, 10)
				.AddIngredient(ItemID.Ectoplasm, 8)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
