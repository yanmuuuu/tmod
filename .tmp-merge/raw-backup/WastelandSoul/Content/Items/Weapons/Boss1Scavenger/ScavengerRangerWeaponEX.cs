using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Scrap Pistol MK-II（清道夫 · B 线 · 专属掉落）
	/// <para/>设计定位：掉落袋专属。弹幕设想：废料重步枪，消耗火枪子弹，单发高伤 22、射速慢一档（23），子弹命中敌人或物块后弹跳 1 次（第二跳伤害衰减为 40%）。定位是「点杀精英」而非扫射，22 低于 Musket(24) 且明显低于 Boomstick 的三连发爆发，符合掉落专属的略强定位且有辨识度。
	/// </summary>
	public class ScavengerRangerWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 22;
		protected override int UseTime => 23;
		protected override float Knockback => 4.0f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerRifleRoundEX>();

		protected override float ShootSpeed => 13.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从清道夫的掉落袋开出。
	}
}
