using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;
using WastelandSoul.Content.Buffs;
using WastelandSoul.Content.Projectiles.Scavenger;

namespace WastelandSoul.Content.Projectiles.Rift
{
	/// <summary>
	/// 星隙斩。按时间走完：蓄能、沿轨迹撕开、向中心坍缩、爆发、碎片散开。
	/// <c>ai[0]</c> 是规模，1 为满尺寸。
	/// </summary>
	public class StarRiftSlash : ModProjectile
	{
		private static readonly Color Core = new Color(236, 246, 255);
		private static readonly Color Body = new Color(124, 86, 255);
		private static readonly Color Edge = new Color(70, 196, 255);

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 48;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 16;
		}

		public override bool? CanDamage()
		{
			float time = Projectile.ai[1];
			return time > 8f && time < 36f;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			return false;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = Projectile.velocity.LengthSquared() > 0.01f
					? Projectile.velocity.ToRotation()
					: 0f;
			}

			float time = Projectile.ai[1];
			Projectile.ai[1] = time + 1f;
			float power = Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0];
			Vector2 forward = Projectile.localAI[1].ToRotationVector2();
			Projectile.velocity = forward;

			if (time < 8f) {
				Charge(power);
			}
			else if (time < 22f) {
				Projectile.Center += forward * (8f * power);
				Tear(time, power, forward);
			}
			else if (time < 34f) {
				Collapse(power);
			}
			else if (time < 36f) {
				Burst(power);
			}
			else {
				Scatter(power);
			}

			if (time > 46f) {
				Projectile.Kill();
			}
		}

		private void Charge(float power)
		{
			if (Main.dedServ) {
				return;
			}

			WastelandFxSystem.Glow(Projectile.Center, Core, 0.7f * power, 8);
			WastelandFxSystem.Glow(Projectile.Center, Body, 1.3f * power, 10);
			WastelandFxSystem.Motes(Projectile.Center, 14f * power, 1, Edge);
		}

		private void Tear(float time, float power, Vector2 forward)
		{
			Vector2 side = new Vector2(-forward.Y, forward.X);
			float wave = (float)System.Math.Sin(time * 0.7f) * 16f * power;
			Vector2 point = Projectile.Center + side * wave;

			if (Main.dedServ) {
				return;
			}

			WastelandFxSystem.Glow(point, Core, 0.45f * power, 8);
			WastelandFxSystem.Spark(point, side * wave * 0.08f, Body, 0.8f * power, 12, 0f);
			WastelandFxSystem.Spark(Projectile.Center, forward * 0.4f, Edge, 0.45f, 10, 0f);

			if ((int)time % 3 == 0) {
				WastelandFxSystem.Along(Projectile.Center - forward * 18f, point + forward * 10f, Core, 2);
			}
		}

		private void Collapse(float power)
		{
			if (Main.dedServ || Main.GameUpdateCount % 2u != 0u) {
				return;
			}

			WastelandFxSystem.Ring(Projectile.Center, Body, 54f * power, 8f, 12);
			WastelandFxSystem.Glow(Projectile.Center, Core, 0.9f * power, 8);

			for (int i = 0; i < 3; i++) {
				Vector2 from = Projectile.Center + Main.rand.NextVector2Circular(36f * power, 36f * power);
				Vector2 inward = Projectile.Center - from;
				WastelandFxSystem.Spark(from, inward * 0.12f, Color.Lerp(Edge, Core, 0.5f), 0.6f, 12, 0f);
			}
		}

		private void Burst(float power)
		{
			if (Main.dedServ) {
				return;
			}

			WastelandFxSystem.Flash(Projectile.Center, Core, 1.4f * power);
			WastelandFxSystem.Ring(Projectile.Center, Body, 12f, 86f * power, 16);
			WastelandFxSystem.Ring(Projectile.Center, Edge, 8f, 48f * power, 12);
			WastelandFxSystem.Burst(Projectile.Center, (int)(10 * power), Body, 4.2f * power);
		}

		private void Scatter(float power)
		{
			if (Main.dedServ || Main.rand.NextBool(2)) {
				return;
			}

			Vector2 velocity = Main.rand.NextVector2Circular(3.4f, 3.4f) * power;
			WastelandFxSystem.Spark(Projectile.Center, velocity, Core, 0.7f, 16, 0f);
			WastelandFxSystem.Flakes(Projectile.Center, 1, Edge);
		}
	}

	/// <summary>权杖打出的星核，飞一会儿后原地坍成一道星隙斩。</summary>
	public class OrbitMote : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 28;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.6f;
		}

		public override void AI()
		{
			Projectile.velocity *= 0.985f;

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				WastelandFxSystem.Glow(Projectile.Center, new Color(236, 246, 255), 0.5f, 8);
				WastelandFxSystem.Motes(Projectile.Center, 12f, 1, new Color(124, 86, 255));
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			Projectile.NewProjectile(
				Projectile.GetSource_Death(),
				Projectile.Center,
				Projectile.velocity * 0.2f,
				ModContent.ProjectileType<StarRiftSlash>(),
				Projectile.damage,
				Projectile.knockBack,
				Projectile.owner,
				0.75f);
		}
	}

	/// <summary>星弦。钉在目标上时撕开一道较小的裂缝。</summary>
	public class StarstringShot : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 50;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.extraUpdates = 1;
			Projectile.arrow = true;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				WastelandFxSystem.Spark(Projectile.Center, -Projectile.velocity * 0.05f, new Color(170, 210, 255), 0.45f, 10, 0f);
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Open();
		}

		public override void OnKill(int timeLeft)
		{
			if (timeLeft > 0) {
				Open();
			}
		}

		private void Open()
		{
			if (Main.dedServ || Projectile.localAI[0] > 0f) {
				return;
			}

			Projectile.localAI[0] = 1f;
			Projectile.NewProjectile(
				Projectile.GetSource_Death(),
				Projectile.Center,
				Projectile.velocity * 0.15f,
				ModContent.ProjectileType<StarRiftSlash>(),
				(int)(Projectile.damage * 0.65f),
				Projectile.knockBack,
				Projectile.owner,
				0.6f);
		}
	}

	/// <summary>缺环轮。飞到尽头时坍成星隙。</summary>
	public class GapChakramProj : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 32;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Projectile.rotation += 0.4f;
			Projectile.velocity *= 0.98f;

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				WastelandFxSystem.Spark(Projectile.Center, Projectile.velocity * 0.05f, new Color(124, 86, 255), 0.55f, 10, 0f);
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			Projectile.NewProjectile(
				Projectile.GetSource_Death(),
				Projectile.Center,
				Vector2.Zero,
				ModContent.ProjectileType<StarRiftSlash>(),
				Projectile.damage,
				Projectile.knockBack,
				Projectile.owner,
				0.7f);
		}
	}

	/// <summary>虚空种。跟着主人，隔一会儿撕开一道小裂缝。</summary>
	public class VoidSeedMinion : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<VoidSeedBuff>();

		protected override int ShotType => ModContent.ProjectileType<StarRiftSlash>();

		protected override float AttackInterval => 84f;

		protected override float HoverHeight => 70f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.DamageType = DamageClass.Summon;
		}

		public override void AI()
		{
			base.AI();

			if (!Projectile.active || Main.dedServ || Main.GameUpdateCount % 12u != 0u) {
				return;
			}

			WastelandFxSystem.Glow(Projectile.Center, new Color(236, 246, 255), 0.45f, 8);
			WastelandFxSystem.Motes(Projectile.Center, 16f, 1, new Color(124, 86, 255));
		}
	}
}
