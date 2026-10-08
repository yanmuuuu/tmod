using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树 · 近战弹幕
	//   SteelEdgeShard    精钢碎片（穿透 4，命中流血）
	//   VerdictWave       裁决波（穿透 6，命中削防 + 溅出追踪碎片）
	//   VerdictFragment   裁决碎片（追踪残血目标，命中一次即消失）
	//
	// 表现只写在各弹幕自己的 AI 里：少量同色尘粒，**不做**挂在所有弹幕上的线状拖尾。
	// ====================================================================================

	/// <summary>精钢碎片：无重力、轻微减速，穿透 4 个敌人，命中让目标流血 3 秒。</summary>
	public class SteelEdgeShard : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 4;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.42f * Projectile.direction;
			Projectile.velocity *= 0.992f;

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Silver);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(200, 170, 120), 0.4f, 8);
				}
				dust.scale = 0.8f;
				dust.velocity *= 0.25f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Bleeding, 180);
		}
	}

	/// <summary>
	/// 裁决波：飞行中逐渐放大、纵向速度被压平（贴地推出去的一刀）。
	/// 穿透 6 个敌人，命中削防（脓液 4 秒），并且每次命中最多溅出 4 片追踪碎片。
	/// </summary>
	public class VerdictWave : ModProjectile
	{
		/// <summary>最多溅出的碎片数。</summary>
		private const float MaxFragments = 4f;

		public override void SetDefaults()
		{
			Projectile.width = 46;
			Projectile.height = 46;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 6;
			Projectile.timeLeft = 60;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.45f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.scale = 1f + (60 - Projectile.timeLeft) * 0.012f;
			Projectile.velocity.Y *= 0.90f;
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch);
				dust.noGravity = true;
				if (!Main.dedServ) {
					WastelandFxSystem.Glow(Projectile.Center, new Color(200, 170, 120), 0.4f, 8);
				}
				dust.scale = 0.95f;
				dust.velocity *= 0.3f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Ichor, 240);

			// ai[0] 记已经溅出几片：超过上限就不再生成，避免打群怪时弹幕爆炸
			if (Projectile.ai[0] >= MaxFragments || Projectile.owner != Main.myPlayer) {
				return;
			}

			Projectile.ai[0] += 1f;

			Vector2 offset = new Vector2(Main.rand.NextFloat(-12f, 12f), Main.rand.NextFloat(-12f, 12f));
			Vector2 speed = Main.rand.NextVector2Circular(4f, 4f) + Projectile.velocity * 0.25f;

			Projectile.NewProjectile(
				Projectile.GetSource_FromThis(),
				target.Center + offset,
				speed,
				ModContent.ProjectileType<VerdictFragment>(),
				(int)(Projectile.damage * 0.5f),
				Projectile.knockBack * 0.5f,
				Projectile.owner);
		}
	}

	/// <summary>裁决碎片：朝 420 像素内最近的敌人缓慢转向，命中一次即消失。</summary>
	public class VerdictFragment : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.35f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.5f * Projectile.direction;

			NPC target = UpgradeTreeAim.NearestEnemy(Projectile.Center, 420f);

			if (target != null) {
				float speed = Projectile.velocity.Length();
				Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.09f);

				if (Main.rand.NextBool(5)) {
					Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch);
					dust.noGravity = true;
					if (!Main.dedServ) {
						WastelandFxSystem.Glow(Projectile.Center, new Color(200, 170, 120), 0.4f, 8);
					}
					dust.scale = 0.7f;
					dust.velocity *= 0.2f;
				}
			}
		}
	}

	/// <summary>本包弹幕共用的目标查找（**不是** ModProjectile，不需要贴图）。</summary>
	internal static class UpgradeTreeAim
	{
		/// <summary>返回范围内最近的敌对 NPC；找不到返回 null。</summary>
		public static NPC NearestEnemy(Vector2 from, float maxRange)
		{
			NPC best = null;
			float bestDistance = maxRange * maxRange;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (!npc.active || npc.friendly || npc.immortal || npc.dontTakeDamage || npc.life <= 0) {
					continue;
				}

				float distance = Vector2.DistanceSquared(npc.Center, from);

				if (distance < bestDistance) {
					bestDistance = distance;
					best = npc;
				}
			}

			return best;
		}
	}
}
