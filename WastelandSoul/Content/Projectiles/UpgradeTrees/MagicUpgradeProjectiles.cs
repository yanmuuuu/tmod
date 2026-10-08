using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树 · 魔法弹幕
	//   SteelArcBolt   精钢弧光（穿透 2，命中带电）
	//   FrostLance     霜矛（穿透 3，340 像素内轻微追踪，命中霜冻）
	// 表现只写在自己身上：少量同色尘粒，不做挂在所有弹幕上的线状拖尾。
	// ====================================================================================

	/// <summary>精钢弧光：无重力直飞、穿透 2 个敌人，命中附加 2 秒带电。</summary>
	public class SteelArcBolt : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 100;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.65f;
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

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric);
				dust.noGravity = true;
				dust.scale = 0.85f;
				dust.velocity *= 0.4f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Electrified, 120);
		}
	}

	/// <summary>霜矛：穿透 3，在 340 像素内朝最近的敌人轻轻转向，命中霜冻 4 秒。</summary>
	public class FrostLance : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
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
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation();

			NPC target = UpgradeTreeAim.NearestEnemy(Projectile.Center, 340f);

			if (target != null) {
				float speed = Projectile.velocity.Length();
				Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.06f);
			}

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch);
				dust.noGravity = true;
				dust.scale = 0.8f;
				dust.velocity *= 0.3f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Frostburn2, 240);
		}
	}
}
