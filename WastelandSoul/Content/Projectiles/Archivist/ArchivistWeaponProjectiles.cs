using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.Archivist
{
	// ====================================================================================
	// Boss2「归档者」专属弹幕（A 线四职业；召唤师与 B 线专属仍待做）。
	// 设计感：骨白 / 档案纸张 / 索引——飞行物偏冷色，命中带困惑或折返。
	// ====================================================================================

	/// <summary>战士：短距骨白弧光。飞得慢、活得短（约 3 格），不穿透，用来补近战贴脸风险。</summary>
	public class ArchivistBoneArc : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 34;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.3f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity *= 0.97f;

			// 越接近消失越暗（用 alpha 模拟弧光淡出）
			Projectile.alpha = (int)MathHelper.Lerp(0f, 200f, 1f - Projectile.timeLeft / 34f);

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Bone);
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}
	}

	/// <summary>法师：索引页。可以弹墙两次，命中附加困惑。</summary>
	public class ArchivistIndexPage : ModProjectile
	{
		private const int MaxBounces = 2;

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 240;
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
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation += 0.12f * Projectile.direction;

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch);
				dust.noGravity = true;
				dust.scale = 0.7f;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			// 弹墙：按已弹次数决定是否还继续飞
			if (Projectile.ai[0] >= MaxBounces) {
				return true;
			}

			Projectile.ai[0] += 1f;

			if (Projectile.velocity.X != oldVelocity.X) {
				Projectile.velocity.X = -oldVelocity.X;
			}
			if (Projectile.velocity.Y != oldVelocity.Y) {
				Projectile.velocity.Y = -oldVelocity.Y;
			}

			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Confused, 180);
		}
	}

	/// <summary>射手：骨片。直线、无重力、穿透 1。（设计里的"一次扇形 4 枚"待改写武器 Shoot 后再做）</summary>
	public class ArchivistBoneShard : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 150;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity.Y *= 0.995f;   // 几乎不下坠

			if (Main.rand.NextBool(5)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Bone);
				dust.noGravity = true;
				dust.scale = 0.7f;
			}
		}
	}
}
