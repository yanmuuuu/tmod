using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss4Fireplace
{
	/// <summary>
	/// Fireplace Guardian Glaive（壁炉守卫 · A 线 · 可合成）
	/// <para/>设计定位：【补全】原设计数据在此处被截断，按同 Boss 同职业 A/B 线的规律（B 线 = A 线 × 1.25）补写。
	/// <para/>制作站点：秘银砧（血肉墙之后装备站点）
	/// </summary>
	public class FireplaceRogueWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 75;
		protected override int UseTime => 14;
		protected override float Knockback => 5.0f;
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Fireplace.FireplaceRogueProjectile>();

		protected override float ShootSpeed => 14.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
		}

		public override void AddRecipes()
		{
			CreateRecipe(25)
				.AddIngredient<FireplaceAlloyBar>(3)
				.AddIngredient(ItemID.ChlorophyteBar, 3)
				.AddIngredient(ItemID.HallowedBar, 2)
				.AddIngredient(ItemID.Ectoplasm, 2)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
