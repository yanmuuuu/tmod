using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Buffs;

namespace WastelandSoul.Content.Projectiles
{
	/// <summary>
	/// 污染区域：破损推进器与泄漏物在地面形成的持续伤害地带。
	/// <para/>先下落，接触地面后停留；进入区域的玩家会被施加「污染」减益并持续受伤。
	/// </summary>
	public class PollutionZone : ModProjectile
	{
		/// <summary>落地后的存在时间（tick）。</summary>
		private const int Lifetime = 600;

		public override void SetDefaults()
		{
			Projectile.width = 96;
			Projectile.height = 28;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;   // 可以反复伤害进入区域的玩家
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.alpha = 40;
		}

		public override void AI()
		{
			// ai[0]: 0 = 下落中，1 = 已落地
			if (Projectile.ai[0] == 0f) {
				Projectile.velocity.Y += 0.35f;

				if (Projectile.velocity.Y > 9f) {
					Projectile.velocity.Y = 9f;
				}

				Vector2 below = new Vector2(Projectile.Center.X - Projectile.width / 2f, Projectile.Bottom.Y);

				if (Collision.SolidCollision(below, Projectile.width, 8)) {
					Projectile.ai[0] = 1f;
					Projectile.velocity = Vector2.Zero;
					Projectile.timeLeft = Lifetime;

					// 落地污染扩散的视觉
					if (!Main.dedServ) {
						for (int i = 0; i < 16; i++) {
							Vector2 velocity = Main.rand.NextVector2Circular(3f, 1.5f);
							Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, velocity.X, velocity.Y, 100, default, 1.3f);
						}
					}
				}
			}
			else {
				Projectile.velocity = Vector2.Zero;

				// 地面上缓慢冒出的污染气泡
				if (!Main.dedServ && Main.rand.NextBool(4)) {
					Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, -0.8f, 120, default, 1.1f);
				}
			}
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info)
		{
			target.AddBuff(ModContent.BuffType<Pollution>(), 300);
		}
	}
}
