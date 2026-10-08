using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.Rust
{
	// ====================================================================================
	// 「锈蚀」线 · 战士弹幕
	//   · RustShardSlash     砍刀甩出的锈铁碎片（穿透 2，轻微减速）
	//   · RustShardSlashEX   强化版碎片（穿透 3，命中着火，带火星拖尾）
	//   · ScrapYoyoProjectile 悠悠球本体（原版 aiStyle 99，只补粒子）
	// 说明：碎片类弹幕**不做自动挂线状特效**，只在自己飞行时掉少量铁屑/火星。
	// ====================================================================================

	/// <summary>砍刀甩出的锈铁碎片：无重力、略微减速、穿透 2 个敌人。</summary>
	public class RustShardSlash : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.38f * Projectile.direction;
			Projectile.velocity *= 0.99f;

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}
	}

	/// <summary>强化砍刀的碎片：穿透 3、命中点着火，飞行时掉火星。</summary>
	public class RustShardSlashEX : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 80;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.35f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.46f * Projectile.direction;
			Projectile.velocity *= 0.992f;

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 0.9f;
			}

			if (Main.rand.NextBool(5)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.7f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 120);
		}
	}

	/// <summary>废料悠悠球本体：运动完全交给原版 aiStyle 99（线长/速度由 Sets 决定）。</summary>
	public class ScrapYoyoProjectile : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = 4f;
			ProjectileID.Sets.YoyosMaximumRange[Type] = 190f;
			ProjectileID.Sets.YoyosTopSpeed[Type] = 10.5f;
		}

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.aiStyle = 99;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.MeleeNoSpeed;
			Projectile.penetrate = -1;
			Projectile.scale = 1f;
			Projectile.extraUpdates = 0;
		}

		public override void AI()
		{
			// aiStyle 99 已经把运动做完了，这里只加「转起来掉铁屑」的表现
			if (Main.rand.NextBool(6)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.velocity *= 0.3f;
			}
		}
	}
}
