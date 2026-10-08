using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.Rust
{
	// ====================================================================================
	// 「锈蚀」线 · 射手弹幕
	//   · ScrapPellet     霰弹弹丸（轻下坠、穿透 1）
	//   · ScrapPelletEX   强化弹丸（穿透 2 + 撞墙弹一次）
	//   · RustNail        射钉（无重力、穿透 2、速度很快）
	// ====================================================================================

	/// <summary>霰弹弹丸：轻微下坠，命中即消失。</summary>
	public class ScrapPellet : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 45;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.3f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity.Y += 0.02f;

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}
	}

	/// <summary>强化弹丸：穿透 2，撞到方块会弹一次（用 ai[0] 记次数）。</summary>
	public class ScrapPelletEX : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 60;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.35f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity.Y += 0.012f;

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverFlame);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Projectile.ai[0] += 1f;

			if (Projectile.ai[0] > 1f) {
				return true;   // 弹过一次了，第二次落地就消失
			}

			if (Projectile.velocity.X != oldVelocity.X) {
				Projectile.velocity.X = -oldVelocity.X * 0.65f;
			}

			if (Projectile.velocity.Y != oldVelocity.Y) {
				Projectile.velocity.Y = -oldVelocity.Y * 0.65f;
			}

			return false;
		}
	}

	/// <summary>射钉：无重力、笔直、穿透 2 个敌人。</summary>
	public class RustNail : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 60;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.extraUpdates = 1;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (Main.rand.NextBool(5)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}
	}
}
