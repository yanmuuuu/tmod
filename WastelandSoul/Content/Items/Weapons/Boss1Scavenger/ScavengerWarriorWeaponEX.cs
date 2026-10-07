using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Scrap Cleaver MK-II（清道夫 · B 线 · 专属掉落）
	/// <para/>设计定位：仅能从清道夫的掉落袋开出（100%，无需配方）。清道夫本人使用的废料巨刃：伤害 24、攻速比 A 线更快（20）、击退 7；挥砍时扇形射出 3 片废料碎片（各 8 伤害、穿透 1），贴身与中距离都能打。24 仍低于同期 Blade of Grass(28)，也仅略高于 Blood Butcherer(22)，属于「略强于 A 线」而非越级。
	/// </summary>
	public class ScavengerWarriorWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 24;
		protected override int UseTime => 20;
		protected override float Knockback => 7.0f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerScrapShardEX>();

		protected override float ShootSpeed => 11.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从清道夫的掉落袋开出。
	}
}
