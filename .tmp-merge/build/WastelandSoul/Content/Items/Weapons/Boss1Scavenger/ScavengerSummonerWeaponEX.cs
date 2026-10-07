using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Drone Beacon MK-II（清道夫 · B 线 · 专属掉落）
	/// <para/>设计定位：掉落袋专属。每次使用召唤 1 只强化无人机，同一时间最多 2 只（占 2 个仆从位）：接触伤害 13，每 1 秒发射一发 5 伤害废料弹（可追踪）。总 DPS 高于 A 线杖，代价是仆从位占用更多；13 仍在 Hornet Staff(11–12) 一档，未越级到 Imp Staff(17)。
	/// </summary>
	public class ScavengerSummonerWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 13;
		protected override int UseTime => 28;
		protected override float Knockback => 2.5f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerDroneMinionEX>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.Scavenger.ScavengerDroneBuffEX>(), 12);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从清道夫的掉落袋开出。
	}
}
