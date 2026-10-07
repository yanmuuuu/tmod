using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss3AshHeart
{
	/// <summary>
	/// Ash Heart Cinder Staff MK-II（灰烬之心 · B 线 · 专属掉落）
	/// <para/>设计定位：召唤两只『炉心余烬』：中距离喷吐灰烬团（穿透 1 次），命中给敌人挂 3 秒灼烧减益，并给自己附近的小范围提供微弱余温光环（纯视觉氛围，不叠伤害）。54 略高于乌鸦法杖(55 → 持平/微调)与致命球法杖(50)，两只固定上限，走『质』而非『量』。
	/// </summary>
	public class AshHeartSummonerWeaponEx : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 54;
		protected override int UseTime => 26;
		protected override float Knockback => 3.5f;
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.LateBosses.AshHeartEmberMinionEX>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.LateBosses.AshHeartEmberBuffEX>(), 16);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从灰烬之心的掉落袋开出。
	}
}
