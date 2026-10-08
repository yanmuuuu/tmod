using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;
using WastelandSoul.Content.Buffs;

namespace WastelandSoul.Content.Projectiles.Wildlife
{
	/// <summary>废料收割者打出的一块旋转废铁（敌对）。直线飞行、略微受重力、撞墙即碎。</summary>
	public class ReaperScrapShot : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 260;
			Projectile.light = 0.3f;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.velocity.Y += 0.06f;
			Projectile.rotation += 0.22f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.velocity *= 0.2f;
				dust.noGravity = true;
				dust.scale = 0.75f;
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = 0.2f, Volume = 0.4f }, Projectile.Center);

			for (int i = 0; i < 8; i++) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Iron, 0f, 0f, 100, default, 1f);
			}
		}
	}

	/// <summary>环身冲击波废料（敌对）：出膛后慢慢减速，带一点自旋拖影。</summary>
	public class ReaperShockwave : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.tileCollide = false;   // 一环扩散弹幕，穿地才有"无死角"的压迫感
			Projectile.timeLeft = 170;
			Projectile.light = 0.4f;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.velocity *= 0.975f;
			Projectile.rotation += 0.13f;

			// 快结束时渐隐
			if (Projectile.timeLeft < 60) {
				Projectile.alpha = Math.Min(255, Projectile.alpha + 4);
			}

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.velocity = -Projectile.velocity * 0.15f;
				dust.noGravity = true;
				dust.scale = 0.9f;
			}
		}
	}

	/// <summary>
	/// 玩家版废料团（<see cref="Content.Items.Weapons.Wildlife.ScrapReaperBlade"/> 的弹幕）：
	/// 飞行时会朝最近的敌人轻微转向，命中后给**使用者**挂一层「废铁护盾」。
	/// <para/>追踪刻意做得很弱（每帧最多转 2.5°），所以它是"好用但不会自己打完"。
	/// </summary>
	public class ReaperFriendlyScrap : ModProjectile
	{
		private const float HomingTurn = 0.044f;   // 约 2.5 度 / 帧
		private const float HomingRange = 420f;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 220;
			Projectile.light = 0.35f;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			// ---------- 轻微追踪 ----------
			int closest = -1;
			float best = HomingRange;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (!npc.CanBeChasedBy(Projectile)) {
					continue;
				}

				float distance = Vector2.Distance(npc.Center, Projectile.Center);

				if (distance < best && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height)) {
					best = distance;
					closest = i;
				}
			}

			if (closest >= 0) {
				Vector2 wanted = Vector2.Normalize(Main.npc[closest].Center - Projectile.Center) * Projectile.velocity.Length();
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted, HomingTurn).SafeNormalize(Vector2.UnitX) * Projectile.velocity.Length();
			}

			Projectile.velocity.Y += 0.03f;
			Projectile.rotation += 0.24f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.velocity *= 0.2f;
				dust.noGravity = true;
				dust.scale = 0.8f;
				dust.color = new Color(230, 150, 80);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 命中给使用者挂护盾
			Player owner = Main.player[Projectile.owner];

			if (owner.active && !owner.dead) {
				owner.AddBuff(ModContent.BuffType<ScrapShield>(), 240);
			}

			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 8; i++) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1.1f);
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			SoundEngine.PlaySound(SoundID.Dig with { Pitch = 0.3f, Volume = 0.35f }, Projectile.Center);

			for (int i = 0; i < 6; i++) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Iron, 0f, 0f, 100, default, 1f);
			}
		}
	}
}
