using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss3AshHeart
{
	/// <summary>
	/// Ash Heart Core Staff MK-II（灰烬之心 · B 线 · 专属掉落）
	/// <para/>设计定位：『灰烬之心』本体：射出一颗悬停 0.5 秒的心核，随后爆开成 6 枚弱追踪余烬火球（各自独立命中判定）。高于幽灵法杖(65)近 20%，代价是蓝耗 24 且弹道有延迟——需要预判站位，手感『先蓄后爆』，是典型的专属奖励级法杖。
	/// </summary>
	public class AshHeartMageWeaponEx : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 78;
		protected override int UseTime => 24;
		protected override float Knockback => 5.0f;
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartMageProjectileEX>();

		protected override float ShootSpeed => 10.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 18);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从灰烬之心的掉落袋开出。
	}
}
