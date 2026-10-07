using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss3AshHeart
{
	/// <summary>
	/// Ash Heart Greatblade MK-II（灰烬之心 · B 线 · 专属掉落）
	/// <para/>设计定位：『炉心巨刃』：挥击一次甩出 3 道呈扇形扩散的灰烬浪（可穿透 2 个敌人），落点生成一片 3 秒燃烧区。单发与挥速都压在泰拉刃(95)与圣骑士锤(90)之下但范围更大——A 线的强化版，特色是『清一屏』而不是拼 DPS。
	/// </summary>
	public class AshHeartWarriorWeaponEx : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 90;
		protected override int UseTime => 21;
		protected override float Knockback => 7.5f;
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartWarriorProjectileEX>();

		protected override float ShootSpeed => 12.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从灰烬之心的掉落袋开出。
	}
}
