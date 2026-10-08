using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger.ScavengerDrops
{
	/// <summary>
	/// Steel Discharger（精钢放电器）：Boss 1「清道夫」掉落 · 法师。
	/// <para/>机制照原版那把**按住蓄力、松手放电**的法师放电武器：
	/// 按住不放时在掌心堆电荷（越蓄越亮、电量越足），松手瞬间放出一道**链状闪电**——
	/// 先打最近的敌人，再跳到附近 2~4 个敌人身上，每一跳伤害递减；
	/// 命中的敌人都挂上**带电（Electrified）+ 缓慢（Slow）**。
	/// <para/>蓄力 3 个档位（0.5s / 1s / 1.5s），档位越高链数越多、总伤越高。
	/// <para/>掉落：清道夫的掉落袋（见 <see cref="Content.Items.Bags.ScavengerBag"/>）。
	/// </summary>
	public class SteelDischarger : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 20;
		protected override int UseTime => 26;
		protected override float Knockback => 3.5f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.SteelDischargeCharge>();

		/// <summary>放电弹幕本身只负责表现，真正打人靠链状闪电（见钢制放电弹幕）。</summary>
		protected override float ShootSpeed => 1f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 7);

			Item.width = 34;
			Item.height = 34;
			Item.crit = 5;              // 初始暴击 5%（玩家指定）
			Item.channel = true;        // 按住蓄力、松手放电
			Item.autoReuse = true;
			Item.useTurn = false;
			Item.UseSound = SoundID.Item15;
		}
	}
}
