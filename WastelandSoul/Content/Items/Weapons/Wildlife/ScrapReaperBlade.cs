using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Buffs;

namespace WastelandSoul.Content.Items.Weapons.Wildlife
{
	/// <summary>
	/// 废料收割者之刃：迷你 Boss「废料收割者」必掉的一把魔法武器。
	/// <para/>手感：射出一团带追踪的废料（<see cref="Content.Projectiles.Wildlife.ReaperScrapShot"/> 的友方版），
	/// 命中时给自己挂一层「废铁护盾」——这层护盾只加 4 点防御、持续 4 秒，
	/// 定位是"迷你 Boss 的过渡奖励"，不抢 Boss 武器的位置。
	/// <para/>没有任何配方（只从掉落来），介绍里也不写出处。
	/// </summary>
	public class ScrapReaperBlade : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;

		protected override int Damage => 32;

		protected override int UseTime => 19;

		protected override float Knockback => 4.5f;

		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int SellPrice => Item.sellPrice(gold: 4);

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Wildlife.ReaperFriendlyScrap>();

		protected override float ShootSpeed => 11f;

		public override void SetDefaults()
		{
			base.SetDefaults();

			// 魔法武器：吃魔力；基类已经设好 useStyle = Shoot、noMelee = true
			Item.mana = 9;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 兜底：弹幕自己也会挂（ReaperFriendlyScrap.OnHitNPC），这里再保一层
			player.AddBuff(ModContent.BuffType<ScrapShield>(), 240);
		}
	}
}
