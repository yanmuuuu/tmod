using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 射手弹幕
	//   ScrapRailSlug     电磁炮弹（速度 24、穿透 6、每帧多推 2 次）
	//   ScrapRailSlugEX   强化弹（速度 27、穿透 8）
	// ====================================================================================

	/// <summary>电磁炮弹：极快、笔直、高穿透，飞行时留一道青绿能量痕。</summary>
	public class ScrapRailSlug : ModProjectile
	{
		/// <summary>穿透数量。</summary>
		protected virtual int PierceCount => 6;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = PierceCount;
			Projectile.timeLeft = 60;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.5f;
			Projectile.extraUpdates = 2;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(6)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverFlame);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(180, 190, 210), 0.4f, 8);
				}
				dust.scale = 0.8f;
				dust.velocity *= 0.2f;
			}
		}
	}

	/// <summary>强化电磁炮弹：穿透 8、飞得更快更久，命中时溅出能量火花。</summary>
	public class ScrapRailSlugEX : ScrapRailSlug
	{
		protected override int PierceCount => 8;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 16;
			Projectile.height = 10;
			Projectile.timeLeft = 70;
			Projectile.light = 0.6f;
			Projectile.extraUpdates = 3;
		}

		public override void AI()
		{
			base.AI();

			if (Main.rand.NextBool(5)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(180, 190, 210), 0.4f, 8);
				}
				dust.scale = 0.7f;
				dust.velocity *= 0.2f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 5; i++) {
				Dust dust = Dust.NewDustDirect(target.position, target.width, target.height, DustID.SilverFlame);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(180, 190, 210), 0.4f, 8);
				}
				dust.scale = 0.9f;
				dust.velocity *= 1.6f;
			}
		}
	}
}
