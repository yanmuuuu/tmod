using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;
using WastelandSoul.Content.Buffs;
using WastelandSoul.Content.Projectiles.Scavenger;

namespace WastelandSoul.Content.Projectiles.Signal
{
	/// <summary>锈颚砍刀甩出的热铁片。</summary>
	public class SignalRustShard : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 36;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
			Projectile.velocity *= 0.98f;

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				WastelandFxSystem.Ember(Projectile.Center, Projectile.velocity * 0.08f, new Color(255, 140, 48), 0.55f, 12);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 90);

			if (!Main.dedServ) {
				WastelandFxSystem.Burst(Projectile.Center, 6, new Color(255, 130, 40), 2.4f);
			}
		}
	}

	/// <summary>提灯法典的灯火，会轻轻拐向附近的敌人。</summary>
	public class SignalLanternMote : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.45f;
		}

		public override void AI()
		{
			NPC target = Find(260f);

			if (target != null) {
				Vector2 desired = target.Center - Projectile.Center;

				if (desired.LengthSquared() > 1f) {
					desired = Vector2.Normalize(desired) * 8.2f;
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.08f);
				}
			}

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				WastelandFxSystem.Glow(Projectile.Center, new Color(140, 220, 255), 0.45f, 8);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (!Main.dedServ) {
				WastelandFxSystem.Flakes(Projectile.Center, 4, new Color(214, 226, 236));
				WastelandFxSystem.Flash(Projectile.Center, new Color(170, 220, 255), 0.7f);
			}
		}

		private NPC Find(float range)
		{
			NPC best = null;
			float bestDistance = range;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.immortal) {
					continue;
				}

				float distance = Vector2.Distance(npc.Center, Projectile.Center);

				if (distance < bestDistance) {
					bestDistance = distance;
					best = npc;
				}
			}

			return best;
		}
	}

	/// <summary>小灯丢出的同一颗灯火，伤害算在召唤上。</summary>
	public class SignalWispShot : SignalLanternMote
	{
		public override string Texture => "WastelandSoul/Content/Projectiles/Signal/SignalLanternMote";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.DamageType = DamageClass.Summon;
		}
	}

	/// <summary>线圈钉枪的钉子。</summary>
	public class SignalCoilNail : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 50;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.extraUpdates = 1;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Main.dedServ) {
				return;
			}

			Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 28f;
			WastelandFxSystem.Bolt(Projectile.Center, end, new Color(120, 220, 255));
			WastelandFxSystem.Spark(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f), new Color(255, 170, 70), 0.6f, 10, 0.04f);
		}
	}

	/// <summary>余烬扇扔出去的那一片。</summary>
	public class SignalEmberFanProj : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 40;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Projectile.rotation += 0.35f * Projectile.direction;

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				WastelandFxSystem.Ember(Projectile.Center, Vector2.Zero, new Color(255, 120, 40), 0.45f, 10);
			}
		}
	}

	/// <summary>信号灯芯召出的小灯。跟着人，隔一会儿丢一颗灯火。</summary>
	public class SignalWispMinion : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<SignalWispBuff>();

		protected override int ShotType => ModContent.ProjectileType<SignalWispShot>();

		protected override float AttackInterval => 70f;

		protected override float HoverHeight => 64f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.DamageType = DamageClass.Summon;
		}

		public override void AI()
		{
			base.AI();

			if (!Projectile.active || Main.dedServ || Main.GameUpdateCount % 10u != 0u) {
				return;
			}

			WastelandFxSystem.Glow(Projectile.Center, new Color(120, 210, 255), 0.4f, 8);
		}
	}
}
