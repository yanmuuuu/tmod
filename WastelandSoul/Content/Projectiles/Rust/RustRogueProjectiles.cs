using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.Rust
{
	// ====================================================================================
	// 「锈蚀」线 · 盗贼（投掷）弹幕
	//   · RustShurikenProj       手掌大的手里剑（轻下坠、穿透 3）
	//   · RustShurikenProjEX     强化手里剑（穿透 4、飞得更快）
	//   · ScrapGrenadeProj       手雷本体（抛物线 + 1.7 秒引信，落地弹一下）
	//   · ScrapGrenadeBlast      手雷的爆炸判定（60x60、只存活 3 帧、每个敌人只吃一次）
	// ====================================================================================

	/// <summary>手里剑：旋转飞行、轻微下坠、穿透 3 个敌人。</summary>
	public class RustShurikenProj : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 240;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.55f * Projectile.direction;
			Projectile.velocity.Y += 0.03f;

			if (Projectile.velocity.Y > 12f) {
				Projectile.velocity.Y = 12f;
			}

			if (Main.rand.NextBool(6)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}
	}

	/// <summary>强化手里剑：穿透 4、下坠更小、飞得更快。</summary>
	public class RustShurikenProjEX : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = 4;
			Projectile.timeLeft = 260;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.25f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.62f * Projectile.direction;
			Projectile.velocity.Y += 0.02f;

			if (Projectile.velocity.Y > 12f) {
				Projectile.velocity.Y = 12f;
			}

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.velocity *= 0.2f;
			}
		}
	}

	/// <summary>手雷：抛物线飞行，撞墙会弹一下；引信走完或撞到敌人时爆炸。</summary>
	public class ScrapGrenadeProj : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 100;      // 引信：100 帧 ≈ 1.7 秒
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.velocity.Y += 0.24f;

			if (Projectile.velocity.Y > 12f) {
				Projectile.velocity.Y = 12f;
			}

			Projectile.rotation += Projectile.velocity.X * 0.05f;

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.15f;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			// 落地弹一下（真实手雷手感），不直接爆
			if (Projectile.velocity.X != oldVelocity.X) {
				Projectile.velocity.X = -oldVelocity.X * 0.45f;
			}

			if (Projectile.velocity.Y != oldVelocity.Y) {
				Projectile.velocity.Y = -oldVelocity.Y * 0.40f;
			}

			Projectile.velocity.X *= 0.9f;

			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Explode();
		}

		public override void OnKill(int timeLeft)
		{
			Explode();
		}

		private void Explode()
		{
			// ai[1] 当「已经炸过」的标记，避免 OnHitNPC 与 OnKill 重复炸
			if (Projectile.ai[1] == 1f) {
				return;
			}

			Projectile.ai[1] = 1f;

			if (Projectile.owner == Main.myPlayer) {
				// 爆炸伤害取手雷面板的七成：直接命中 = 34(撞击) + 24(爆) ≈ 58，附近敌人吃 24
				Projectile.NewProjectile(
					Projectile.GetSource_Death(),
					Projectile.Center,
					Vector2.Zero,
					ModContent.ProjectileType<ScrapGrenadeBlast>(),
					(int)(Projectile.damage * 0.7f),
					Projectile.knockBack,
					Projectile.owner);
			}

			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

			for (int i = 0; i < 18; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.noGravity = true;
				dust.scale = 1.1f;
				dust.velocity *= 2.2f;
			}

			for (int i = 0; i < 10; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 1.0f;
				dust.velocity *= 1.8f;
			}
		}
	}

	/// <summary>手雷爆炸：一个 60x60 的短命判定框，每个敌人只吃一次伤害。</summary>
	public class ScrapGrenadeBlast : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 60;
			Projectile.height = 60;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 3;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
			Projectile.aiStyle = 0;
			Projectile.alpha = 40;
		}

		public override void AI()
		{
			Projectile.alpha += 70;

			if (Projectile.alpha > 255) {
				Projectile.alpha = 255;
			}
		}
	}
}
