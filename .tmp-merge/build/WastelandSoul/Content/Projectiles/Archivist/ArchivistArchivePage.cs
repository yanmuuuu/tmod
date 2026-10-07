using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.Archivist
{
	/// <summary>
	/// 归档者的「档案页」：可**弹墙 2 次**的索引页，命中玩家后附加困惑。
	/// <para/>ai[0] = 已经弹过几次墙。
	/// <para/>和法师武器的索引页是同一份设定，但这一版是敌方弹幕：纸页更薄、命中带困惑。
	/// </summary>
	public class ArchivistArchivePage : ModProjectile
	{
		/// <summary>最多弹墙次数（设计要点：2 次）。</summary>
		private const int MaxBounces = 2;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.35f;
		}

		public override void AI()
		{
			// 纸页翻飞：自转 + 一点空气阻力，越飞越慢但不会停
			Projectile.rotation += 0.14f * (Projectile.velocity.X >= 0f ? 1f : -1f);
			Projectile.velocity *= 0.9985f;

			if (!Main.dedServ && Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch, 0f, 0f);
				dust.noGravity = true;
				dust.scale = 0.7f;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (Projectile.ai[0] >= MaxBounces) {
				return true;   // 弹完两次就落地消失
			}

			Projectile.ai[0] += 1f;

			if (Projectile.velocity.X != oldVelocity.X) {
				Projectile.velocity.X = -oldVelocity.X;
			}

			if (Projectile.velocity.Y != oldVelocity.Y) {
				Projectile.velocity.Y = -oldVelocity.Y;
			}

			if (!Main.dedServ) {
				for (int i = 0; i < 6; i++) {
					Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Bone, 0f, 0f);
					dust.noGravity = true;
					dust.scale = 0.9f;
				}
			}

			return false;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info)
		{
			// 命中挂困惑：这正是"档案被调换"的手感
			target.AddBuff(BuffID.Confused, 180);
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 8; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Bone, 0f, 0f);
				dust.noGravity = true;
				dust.scale = 0.9f;
			}
		}
	}
}
