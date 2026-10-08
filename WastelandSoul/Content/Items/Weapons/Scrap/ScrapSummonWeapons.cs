using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 召唤武器（前期后段）
	//   ScrapWardenStaff  召唤「废铁守卫」：高血厚撞击 + 缓慢重炮（每 80 帧一发）
	// 注意：本件**没有合成配方**，是「时期敌人/Boss 掉落」类物品（掉落表由掉落包统一接入），
	//       这里只保证物品、仆从、Buff、弹幕四件套都是完整可运行的。
	// ====================================================================================

	/// <summary>
	/// Scrap Warden Staff（废铁守卫召唤杖）
	/// <para/>定位：前期后段的「坦克型」召唤。召唤 1 只 16 伤害的废铁守卫（占 1 个仆从位），
	/// 它会顶到敌人面前用装甲撞击（接触伤害不低），同时每 1.33 秒打出一发 8 伤害的重弹（穿透 2、击退 4）。
	/// 与 Imp Staff(17) 相比总伤害接近，但输出更慢、更耐打，适合「召唤流自己躲后面」。
	/// </summary>
	public class ScrapWardenStaff : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 16;
		protected override int UseTime => 30;
		protected override float Knockback => 3f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int SellPrice => Item.sellPrice(gold: 3);
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scrap.ScrapWarden>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.Scrap.ScrapWardenBuff>(), 14);
			Item.width = 38;
			Item.height = 38;
		}

		// 掉落专属：**不写 AddRecipes()**，由掉落包接入时期敌人的掉落表。
	}
}
