using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.Scavenger
{
	// ====================================================================================
	// Boss1「清道夫」专属弹幕（A 线四职业；召唤师暂时仍用原版仆从，见各武器类里的 TODO）。
	// 共同特点：废料感——轻微减速/下坠、金属火花粒子、命中带一点灼烧或穿透。
	// 数值都很保守（弹幕伤害跟随武器面板），不引入任何新依赖。
	// ====================================================================================

	/// <summary>战士：挥砍时甩出的废料碎片。穿透 1 个敌人，无重力，略微减速。</summary>
	public class ScavengerScrapShard : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.35f * Projectile.direction;
			Projectile.velocity *= 0.985f;

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}
	}

	/// <summary>法师：带电火花团。命中后留下残渣灼烧（简化为原版着火减益）。</summary>
	public class ScavengerSpark : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.5f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric);
				dust.noGravity = true;
				dust.scale = 0.9f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// 残渣灼烧：先用原版着火代替，等专属减益做好再换
			target.AddBuff(BuffID.OnFire, 120);
		}
	}

	/// <summary>射手：废料手枪弹。直线、无下坠、射速快。</summary>
	public class ScavengerPistolRound : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.4f;
			Projectile.alpha = 20;
			Projectile.extraUpdates = 1;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.noGravity = true;
				dust.scale = 0.7f;
			}
		}
	}
}
