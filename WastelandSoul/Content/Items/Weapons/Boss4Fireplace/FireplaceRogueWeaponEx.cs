using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss4Fireplace
{
	/// <summary>
	/// Fireplace Guardian Glaive MK-II（壁炉守卫 · B 线 · 专属掉落）
	/// <para/>设计定位：【补全】原设计数据在此处被截断，按同 Boss 同职业 A/B 线的规律（B 线 = A 线 × 1.25）补写。
	/// </summary>
	public class FireplaceRogueWeaponEx : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 93;
		protected override int UseTime => 16;
		protected override float Knockback => 5.5f;
		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Fireplace.FireplaceRogueProjectileEX>();

		protected override float ShootSpeed => 15.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从壁炉守卫的掉落袋开出。
	}
}
