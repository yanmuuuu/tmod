using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss3AshHeart
{
	/// <summary>
	/// Ash Heart Ember Rifle MK-II（灰烬之心 · B 线 · 专属掉落）
	/// <para/>设计定位：『余烬迫击炮』：抛射弧线弹，落点炸出 4 秒灰烬火池（对区域内敌人持续结算），直接命中额外结算一次爆炸伤害。量级对标掷矛器(75/慢)，把 A 线的高射速换成高单发 + 区域封锁，专门用来处理成堆的小怪与地面站桩 Boss。
	/// </summary>
	public class AshHeartRangerWeaponEx : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 78;
		protected override int UseTime => 28;
		protected override float Knockback => 6.0f;
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartRangerProjectileEX>();

		protected override float ShootSpeed => 15.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从灰烬之心的掉落袋开出。
	}
}
