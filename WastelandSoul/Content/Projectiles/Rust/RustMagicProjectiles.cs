using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.Rust
{
	// ====================================================================================
	// 「锈蚀」线 · 法师弹幕
	//   · RustBolt       直飞火花团（无重力、穿透 1）
	//   · RustBoltEX     强化火花团（穿透 3 + 340 像素内轻微追踪）
	//   · RustAcidSpray  抛物线酸液（下坠、命中中毒，飞行时掉绿色液滴）
	// ====================================================================================

	/// <summary>火花杖的基础弹：直飞、无重力、穿透 1 个敌人。</summary>
	public class RustBolt : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.6f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(210, 140, 70), 0.4f, 8);
				}
				dust.scale = 0.85f;
				dust.velocity *= 0.4f;
			}
		}
	}

	/// <summary>强化火花团：穿透 3，在 340 像素内朝最近的敌人轻轻转向。</summary>
	public class RustBoltEX : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 110;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.7f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			// 轻微追踪：转向很慢，只是「不容易打空」，不是制导导弹
			NPC target = FindTarget(340f);

			if (target != null) {
				Vector2 desired = Vector2.Normalize(target.Center - Projectile.Center) * Projectile.velocity.Length();
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.06f);
			}

			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(210, 140, 70), 0.4f, 8);
				}
				dust.scale = 0.95f;
				dust.velocity *= 0.4f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 120);
		}

		private NPC FindTarget(float range)
		{
			NPC result = null;
			float best = range;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.immortal || npc.lifeMax <= 5) {
					continue;
				}

				float distance = Vector2.Distance(npc.Center, Projectile.Center);

				if (distance < best) {
					best = distance;
					result = npc;
				}
			}

			return result;
		}
	}

	/// <summary>酸液团：抛物线飞行，命中附中毒；ai[0] 由法典传入，用来做三连的扇形微调。</summary>
	public class RustAcidSpray : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 110;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.3f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			// 出生第一帧按 ai[0] 做一次扇形微调（同一套弹幕复用 3 次）
			if (Projectile.ai[1] == 0f) {
				Projectile.ai[1] = 1f;
				Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[0] * 0.06f);
			}

			Projectile.velocity.Y += 0.075f;
			Projectile.rotation += 0.16f * Projectile.direction;

			if (Projectile.velocity.Y > 12f) {
				Projectile.velocity.Y = 12f;
			}

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GreenMoss);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(210, 140, 70), 0.4f, 8);
				}
				dust.scale = 0.8f;
				dust.velocity *= 0.3f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Poisoned, 180);
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 6; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GreenMoss);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(210, 140, 70), 0.4f, 8);
				}
				dust.scale = 1.0f;
			}
		}
	}
}
