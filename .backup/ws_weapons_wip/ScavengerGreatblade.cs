using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger.ScavengerDrops
{
	/// <summary>
	/// Scavenger Greatblade（清道夫大刀）：Boss 1「清道夫」掉落 · 战士。
	/// <para/>手感照原版**村正大刀（Muramasa）**那一类「宽刃大刀」：出手快、挥砍弧线宽、
	/// 没有附加效果，靠穿透吃饭。它每次挥砍会让刀锋甩出一道**月牙刀光**，
	/// 刀光穿透 5 个敌人、沿飞行方向极轻微扩散（越远越宽），贴脸砍和隔着两三个身位砍都吃满。
	/// <para/>掉落：清道夫的掉落袋（见 <see cref="Content.Items.Bags.ScavengerBag"/>）。
	/// </summary>
	public class ScavengerGreatblade : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 23;
		protected override int UseTime => 26;
		protected override float Knockback => 7f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerCrescentSlash>();

		/// <summary>刀光出手速度：略快于玩家跑速，挥出去就压住正前方。</summary>
		protected override float ShootSpeed => 9.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);

			Item.width = 46;
			Item.height = 46;
			Item.scale = 1.05f;
			Item.crit = 5;              // 初始暴击 5%（玩家指定）
			Item.autoReuse = true;
			Item.useTurn = true;
		}
	}
}
