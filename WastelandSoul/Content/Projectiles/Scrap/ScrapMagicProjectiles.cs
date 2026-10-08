using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 法师弹幕
	//   ScrapNova          慢速法球，命中/超时后炸成 4 片追踪碎片
	//   ScrapNovaEX        强化版（继承）：炸成 6 片、母弹更快更大
	//   ScrapNovaFragment  碎片（穿 2、轻微追踪）
	// ====================================================================================

	/// <summary>新星球：慢速飞行，命中或飞满 1.6 秒后炸成若干追踪碎片。</summary>
	public class ScrapNova : ModProjectile
	{
		/// <summary>炸裂出的碎片数量。</summary>
		protected virtual int FragmentCount => 4;

		/// <summary>碎片伤害 = 母弹伤害 / 该值。</summary>
		protected virtual int FragmentDamageDivisor => 3;

		/// <summary>碎片弹幕类型。</summary>
		protected virtual int FragmentType => ModContent.ProjectileType<ScrapNovaFragment>();

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 100;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.6f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.12f * Projectile.direction;

			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch);
				dust.noGravity = true;
				dust.scale = 1.0f;
				dust.velocity *= 0.3f;
			}
		}

		public override void OnKill(int timeLeft)
		{
			Burst();
		}

		/// <summary>炸裂：径向均匀抛出碎片（只在本地玩家侧生成，避免联机重复）。</summary>
		protected virtual void Burst()
		{
			if (Projectile.owner == Main.myPlayer) {
				for (int i = 0; i < FragmentCount; i++) {
					float angle = MathHelper.TwoPi * i / FragmentCount + Main.rand.NextFloat(-0.18f, 0.18f);
					Vector2 velocity = angle.ToRotationVector2() * 7f;

					Projectile.NewProjectile(
						Projectile.GetSource_Death(),
						Projectile.Center,
						velocity,
						FragmentType,
						Projectile.damage / FragmentDamageDivisor,
						2f,
						Projectile.owner);
				}
			}

			for (int i = 0; i < 14; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch);
				dust.noGravity = true;
				dust.scale = 1.1f;
				dust.velocity *= 1.8f;
			}
		}
	}

	/// <summary>强化新星球：更大、更慢但炸得更多（6 片），母弹本身伤害也更高（由武器提供）。</summary>
	public class ScrapNovaEX : ScrapNova
	{
		protected override int FragmentCount => 6;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.timeLeft = 110;
			Projectile.light = 0.7f;
		}

		public override void AI()
		{
			base.AI();

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverFlame);
				dust.noGravity = true;
				dust.scale = 0.9f;
				dust.velocity *= 0.3f;
			}
		}
	}

	/// <summary>新星碎片：小、快、穿透 2，并在 220 像素内轻微追踪敌人。</summary>
	public class ScrapNovaFragment : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 55;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.4f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.36f * Projectile.direction;

			NPC target = FindTarget(220f);

			if (target != null) {
				Vector2 desired = Vector2.Normalize(target.Center - Projectile.Center) * Projectile.velocity.Length();
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.10f);
			}

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch);
				dust.noGravity = true;
				dust.scale = 0.8f;
				dust.velocity *= 0.2f;
			}
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
}
