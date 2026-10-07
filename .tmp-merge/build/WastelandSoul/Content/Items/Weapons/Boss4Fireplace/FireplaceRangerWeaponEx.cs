using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss4Fireplace
{
	/// <summary>
	/// Fireplace Guardian Rifle MK-II（壁炉守卫 · B 线 · 专属掉落）
	/// <para/>设计定位：【补全】原设计数据在此处被截断，按同 Boss 同职业 A/B 线的规律（B 线 = A 线 × 1.25）补写。
	/// </summary>
	public class FireplaceRangerWeaponEx : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 72;
		protected override int UseTime => 24;
		protected override float Knockback => 7.0f;
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Fireplace.FireplaceRangerProjectileEX>();

		protected override float ShootSpeed => 16.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从壁炉守卫的掉落袋开出。
	}
}
