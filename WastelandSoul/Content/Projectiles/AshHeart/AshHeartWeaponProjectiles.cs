using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.AshHeart
{
	// ====================================================================================
	// Boss3「灰烬之心」专属弹幕（A 线四职业 + B 线继承强化版）。
	// 主题：余烬 —— 暖橙/暗红、命中留灼烧、弹道偏"沉"，与清道夫的废料感区分开。
	// B 线一律**继承 A 线**，只改尺寸/穿透/存活/发光，行为自动复用。
	// （两个召唤师仆从尚未做，见武器类里的 TODO。）
	// ====================================================================================

	/// <summary>战士：余烬剑气。慢速、穿透 2、命中挂灼烧。</summary>
	public class AshHeartWarriorProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.friendly = true;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 60;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.5f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity *= 0.975f;
			Projectile.alpha = (int)MathHelper.Lerp(0f, 190f, 1f - Projectile.timeLeft / 60f);

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 0.9f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 180);
		}
	}

	/// <summary>法师：灰烬心核。轻微下坠，命中挂灼烧。</summary>
	public class AshHeartMageProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.7f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.1f * Projectile.direction;
			Projectile.velocity.Y += 0.08f;

			if (Projectile.velocity.Y > 10f) {
				Projectile.velocity.Y = 10f;
			}

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 1.1f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 240);
		}
	}

	/// <summary>射手：燃灰弹。高速直线，命中挂灼烧。</summary>
	public class AshHeartRangerProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.6f;
			Projectile.extraUpdates = 1;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.FireworkFountain_Red);
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 150);
		}
	}

	// ---------------------------- B 线：继承 A 线做强化 ----------------------------

	/// <summary>专属·余烬巨刃波：更大、穿透 4、活更久。</summary>
	public class AshHeartWarriorProjectileEX : AshHeartWarriorProjectile
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 34;
			Projectile.height = 34;
			Projectile.penetrate = 4;
			Projectile.timeLeft = 80;
			Projectile.light = 0.8f;
		}
	}

	/// <summary>专属·炉心心核：更大、穿透 2、更亮。</summary>
	public class AshHeartMageProjectileEX : AshHeartMageProjectile
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 220;
			Projectile.light = 1f;
		}
	}

	/// <summary>专属·燃灰重弹：更粗、穿透 2、飞得更稳。</summary>
	public class AshHeartRangerProjectileEX : AshHeartRangerProjectile
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.penetrate = 2;
			Projectile.extraUpdates = 2;
			Projectile.light = 0.8f;
		}
	}
}
