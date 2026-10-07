using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Spark Rod MK-II（清道夫 · B 线 · 专属掉落）
	/// <para/>设计定位：掉落袋专属。弹幕设想：抛出一颗缓慢飞行的废料法球，命中或飞行 1.2 秒后炸裂为 3 片追踪碎片（各 6 伤害、轻微追踪），耗蓝 12；对群清场明显强于 A 线杖，单点持续输出反而不如 A 线稳定，形成取舍。22 对齐 Crimson Rod(20) 一档，不触及 Demon Scythe(35)。
	/// </summary>
	public class ScavengerMageWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 22;
		protected override int UseTime => 20;
		protected override float Knockback => 4.0f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerSparkEX>();

		protected override float ShootSpeed => 9.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 12);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从清道夫的掉落袋开出。
	}
}
