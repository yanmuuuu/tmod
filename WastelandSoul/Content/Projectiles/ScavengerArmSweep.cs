using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles
{
	/// <summary>
	/// 机械臂横扫：阶段一/二的范围近战技，前摇明显、范围大。
	/// <para/>ai[0] = 起始角度；ai[1] = 挥舞方向（±1）；ai[2] = Boss 的 whoAmI。
	/// <para/>弹幕本身不做常规移动，而是每 tick 沿 Boss 为圆心的圆弧推进，形成扫地式的伤害带。
	/// </summary>
	public class ScavengerArmSweep : ModProjectile
	{
		/// <summary>挥舞总时长（tick）。</summary>
		private const int SweepDuration = 40;

		/// <summary>机械臂长度（像素）。</summary>
		private const float ArmLength = 104f;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;    // 扫过的路径上可以命中多个玩家
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = SweepDuration;
		}

		public override void AI()
		{
			int bossIndex = (int)Projectile.ai[2];

			if (bossIndex < 0 || bossIndex >= Main.maxNPCs || !Main.npc[bossIndex].active) {
				Projectile.Kill();
				return;
			}

			NPC boss = Main.npc[bossIndex];

			// progress: 0 → 1，对应机械臂从起始角度扫过 180°
			float progress = 1f - Projectile.timeLeft / (float)SweepDuration;
			float angle = Projectile.ai[0] + progress * MathHelper.Pi * Projectile.ai[1];

			Projectile.Center = boss.Center + angle.ToRotationVector2() * ArmLength;
			Projectile.velocity = Vector2.Zero;
			Projectile.rotation = angle;
			Projectile.spriteDirection = Projectile.ai[1] >= 0f ? 1 : -1;

			// 扫过的轨迹留下火星与烟尘
			if (!Main.dedServ && Main.rand.NextBool(2)) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 120, default, 1.1f);
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 120, default, 0.9f);
			}
		}
	}
}
