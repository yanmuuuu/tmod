using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles
{
	/// <summary>
	/// 清道夫的改装炮管子弹。
	/// <para/>ai[0], ai[1] = 锁定的位置；ai[2] = 出膛延迟。
	/// <para/>位置在发射瞬间锁定、之后不再追踪；成组出膛时带随机延迟，对应「设备老化造成的卡顿」。
	/// </summary>
	public class ScavengerBullet : ModProjectile
	{
		private const float BulletSpeed = 11f;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 420;
			Projectile.light = 0.4f;
		}

		public override void AI()
		{
			// ---------- 出膛延迟：在炮口附近抖动，作为预警 ----------
			if (Projectile.ai[2] > 0f) {
				Projectile.ai[2] -= 1f;
				Projectile.velocity *= 0.85f;

				if (!Main.dedServ && Main.rand.NextBool(2)) {
					Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1.2f);
				}

				if (Projectile.ai[2] <= 0f) {
					Vector2 lockedPosition = new Vector2(Projectile.ai[0], Projectile.ai[1]);
					Vector2 direction = lockedPosition - Projectile.Center;

					if (direction.LengthSquared() < 1f) {
						direction = -Vector2.UnitY;
					}

					Projectile.velocity = Vector2.Normalize(direction) * BulletSpeed;

					if (!Main.dedServ) {
						SoundEngine.PlaySound(SoundID.Item11, Projectile.Center);
					}
				}

				return;
			}

			// ---------- 飞行 ----------
			if (Projectile.velocity.LengthSquared() > 0.01f) {
				Projectile.rotation = Projectile.velocity.ToRotation();
			}

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, -Projectile.velocity.X * 0.1f, -Projectile.velocity.Y * 0.1f, 120, default, 1f);
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 8; i++) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.2f);
			}
		}
	}
}
