using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;
using WastelandSoul.Content.Projectiles.Rust;

namespace WastelandSoul.Content.Projectiles.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 召唤仆从 + 专属 Buff + 仆从弹幕
	//   ScrapWarden / ScrapWardenBuff / ScrapWardenShot
	// 仆从公共逻辑复用 Rust 线的 PackMinionBase（跟随 / 找目标 / 射击 / Buff 维持）。
	// 手感：撞击型（Charger）+ 慢速重炮，射速慢但单发高、击退强。
	// ====================================================================================

	/// <summary>废铁守卫：主动顶到敌人面前撞击，每 80 帧打出一发穿透重弹。</summary>
	public class ScrapWarden : PackMinionBase
	{
		protected override int BuffType => ModContent.BuffType<ScrapWardenBuff>();

		protected override int ShotType => ModContent.ProjectileType<ScrapWardenShot>();

		protected override float AttackInterval => 80f;

		protected override float Range => 640f;

		protected override float HoverHeight => 60f;

		protected override float ShotSpeed => 8.5f;

		protected override bool Charger => true;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 34;
			Projectile.height = 34;
		}

		public override void AI()
		{
			base.AI();

			if (Main.rand.NextBool(6)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverFlame);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(180, 190, 210), 0.4f, 8);
				}
				dust.scale = 0.7f;
				dust.velocity *= 0.2f;
			}
		}
	}

	/// <summary>废铁守卫的维持 Buff。</summary>
	public class ScrapWardenBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<ScrapWarden>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>守卫射出的重弹：穿透 2、击退偏高、带青绿能量痕。</summary>
	public class ScrapWardenShot : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 80;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.45f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(180, 190, 210), 0.4f, 8);
				}
				dust.scale = 0.8f;
				dust.velocity *= 0.2f;
			}
		}
	}
}
