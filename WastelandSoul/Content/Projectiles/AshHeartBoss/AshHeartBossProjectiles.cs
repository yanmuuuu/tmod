using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.AshHeartBoss
{
	/// <summary>缓慢转向的余烬团。能甩掉，但停下来会被追上。</summary>
	public class AshEmberOrb : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 240;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.45f;
		}

		public override void AI()
		{
			Player target = Find();

			if (target != null) {
				Vector2 desired = target.Center - Projectile.Center;

				if (desired.LengthSquared() > 1f) {
					desired = Vector2.Normalize(desired) * 7.2f;
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.045f);
				}
			}

			Projectile.rotation += 0.2f;

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 1.1f;
			}
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info)
		{
			target.AddBuff(BuffID.OnFire, 180);
		}

		private Player Find()
		{
			Player best = null;
			float bestDistance = 900f;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];

				if (!player.active || player.dead || player.ghost) {
					continue;
				}

				float distance = Vector2.Distance(player.Center, Projectile.Center);

				if (distance < bestDistance) {
					bestDistance = distance;
					best = player;
				}
			}

			return best;
		}
	}

	/// <summary>落在玩家脚下的余灰，站进去会持续灼烧。</summary>
	public class AshPool : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 24;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			Projectile.rotation = 0f;

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.velocity.Y = -0.6f;
				dust.noGravity = true;

				if (Main.rand.NextBool(2)) {
					Vector2 drift = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-0.8f, -0.15f));
					Common.Effects.WastelandFxSystem.Smoke(Projectile.Center, drift, new Color(96, 78, 68), 0.75f, 28);
				}
			}
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info)
		{
			target.AddBuff(BuffID.OnFire, 90);
		}
	}

	/// <summary>从上方落下的灰烬，不追踪，用来逼玩家走进留出的空档。</summary>
	public class AshCinder : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.25f;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info)
		{
			target.AddBuff(BuffID.OnFire, 120);
		}
	}

	/// <summary>
	/// 从本体扩出去的一圈热浪。只伤害正好踩在环上的玩家，免疫帧防止一圈打多次。
	/// </summary>
	public class AshPulse : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.alpha = 40;
		}

		public override void AI()
		{
			int boss = (int)Projectile.ai[0];

			if (boss >= 0 && boss < Main.maxNPCs && Main.npc[boss].active) {
				Projectile.Center = Main.npc[boss].Center;
			}

			Projectile.ai[1] += 9f;
			float radius = Projectile.ai[1];

			if (!Main.dedServ && radius < 20f) {
				Common.Effects.WastelandFxSystem.Ring(Projectile.Center, new Color(255, 140, 50), 20f, 520f, 36);
				Common.Effects.WastelandFxSystem.Flash(Projectile.Center, new Color(255, 200, 120), 2.2f);
			}

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				for (int i = 0; i < Main.maxPlayers; i++) {
					Player player = Main.player[i];

					if (!player.active || player.dead || player.ghost || player.immune) {
						continue;
					}

					float distance = Vector2.Distance(player.Center, Projectile.Center);

					if (distance > radius || distance < radius - 18f) {
						continue;
					}

					PlayerDeathReason reason = PlayerDeathReason.ByProjectile(player.whoAmI, Projectile.whoAmI);
					int direction = player.Center.X >= Projectile.Center.X ? 1 : -1;
					player.Hurt(reason, Projectile.damage, direction);
				}
			}

			if (!Main.dedServ) {
				for (int i = 0; i < 3; i++) {
					Vector2 spot = Projectile.Center + Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * radius;
					Dust dust = Dust.NewDustPerfect(spot, DustID.Torch);
					dust.noGravity = true;
					dust.scale = 1.4f;
				}

				if (Main.GameUpdateCount % 2u == 0u) {
					float angle = Main.rand.NextFloat(MathHelper.TwoPi);
					Vector2 spot = Projectile.Center + angle.ToRotationVector2() * radius;
					Common.Effects.WastelandFxSystem.Ember(spot, angle.ToRotationVector2() * Main.rand.NextFloat(0.6f, 1.8f), new Color(255, 150, 60), 0.85f, 18);
				}
			}

			if (radius > 560f) {
				Projectile.Kill();
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			return false;
		}
	}
}
