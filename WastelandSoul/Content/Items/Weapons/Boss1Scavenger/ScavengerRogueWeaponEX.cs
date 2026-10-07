using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Scrap Knife MK-II（清道夫 · B 线 · 专属掉落）
	/// <para/>设计定位：掉落袋专属，投掷伤害类；每次开出 150–200 个（可堆叠 999，重刷 Boss 可持续获取，避免一次性用完）。弹幕设想：旋转废料裂片，飞行中轻微下坠，命中后分裂出 3 片散射碎片（各 5 伤害），近距离贴脸爆发很高、远距离命中率差。18 介于 Poisoned Knife(13) 与 Bone(20) 之间，高于 A 线 14，属于「更强 + 更有个性」的掉落专属。
	/// </summary>
	public class ScavengerRogueWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 18;
		protected override int UseTime => 12;
		protected override float Knockback => 3.0f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerCaltropEX>();

		protected override float ShootSpeed => 12.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从清道夫的掉落袋开出。
	}
}
