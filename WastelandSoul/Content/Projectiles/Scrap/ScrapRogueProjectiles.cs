using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 盗贼（投掷）弹幕
	//   ScrapBuzzsawProj   锯片（飞行 → 撞墙后原地旋转 5 秒，持续伤害）
	//   ScrapChakramProj   四刃环（继承钢筋回旋镖的去程/回程逻辑，改投掷伤害与命中频率）
	// ====================================================================================

	/// <summary>
	/// 锯片：飞出去时受重力影响，撞到方块就卡住原地旋转；
	/// 旋转期间用本地无敌帧（20 帧）反复伤害同一个敌人。
	/// </summary>
	public class ScrapBuzzsawProj : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 300;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.ai[0] == 0f) {
				// 去程：抛物线下坠 + 高速自转
				Projectile.rotation += 0.62f * Projectile.direction;
				Projectile.velocity.Y += 0.12f;
				Projectile.velocity *= 0.996f;
			}
			else {
				// 卡住：原地高速旋转，速度迅速归零
				Projectile.rotation += 0.55f * Projectile.direction;
				Projectile.velocity *= 0.82f;
			}

			if (Main.rand.NextBool(5)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.velocity *= 0.3f;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			// 撞墙不消失，改为卡住原地旋转
			Projectile.ai[0] = 1f;
			Projectile.velocity = oldVelocity * 0.30f;
			Projectile.netUpdate = true;

			return false;
		}
	}

	/// <summary>四刃环：去程 30 帧后回手，全程穿透、命中频率限制 10 帧。</summary>
	public class ScrapChakramProj : RebarBoomerangProj
	{
		protected override float OutTime => 30f;

		protected override float ReturnLerp => 0.14f;

		protected override float ReturnSpeed => 16f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			base.AI();

			if (Main.rand.NextBool(5)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}
	}
}
