using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.Fireplace
{
	// ====================================================================================
	// Boss4「壁炉守卫」专属弹幕（A 线四职业 + B 线继承强化版）。
	// 主题：冷白金属与光束 —— 与 Boss1 的冷灰废料、Boss3 的暖橙余烬都区分开。
	// B 线一律继承 A 线，只改尺寸/穿透/存活/发光。
	// （两个召唤师仆从尚未做，见武器类里的 TODO。）
	// ====================================================================================

	/// <summary>战士：重击冲击波。慢速、穿透 4、渐隐。</summary>
	public class FireplaceWarriorProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 34;
			Projectile.height = 34;
			Projectile.friendly = true;
			Projectile.penetrate = 4;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.45f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity *= 0.97f;
			Projectile.alpha = (int)MathHelper.Lerp(0f, 200f, 1f - Projectile.timeLeft / 70f);

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Silver);
				dust.noGravity = true;
				dust.scale = 0.9f;
			}
		}
	}

	/// <summary>法师：冷光弹。直线、命中挂缓慢。</summary>
	public class FireplaceMageProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 150;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.7f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverCoin);
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Slow, 180);
		}
	}

	/// <summary>射手：高速钉弹。直线、穿透 1。</summary>
	public class FireplaceRangerProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 130;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.5f;
			Projectile.extraUpdates = 1;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Silver);
				dust.noGravity = true;
				dust.scale = 0.7f;
			}
		}
	}

	/// <summary>盗贼：回旋刃。飞出约半秒后折返并回收（沿用归档者骨镖的写法）。</summary>
	public class FireplaceRogueProjectile : ModProjectile
	{
		private const int OutTicks = 28;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.penetrate = 4;
			Projectile.timeLeft = 300;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.4f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.42f * Projectile.direction;
			Projectile.ai[0] += 1f;

			if (Projectile.ai[0] < OutTicks) {
				Projectile.velocity *= 0.985f;
			} else {
				Player owner = Main.player[Projectile.owner];
				Vector2 toOwner = owner.MountedCenter - Projectile.Center;

				Projectile.velocity = Vector2.Normalize(toOwner) * 12f;

				if (toOwner.Length() < 40f) {
					Projectile.Kill();
					return;
				}
			}

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Silver);
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}
	}

	// ---------------------------- B 线：继承 A 线做强化 ----------------------------

	/// <summary>专属·重型冲击波：更大、穿透 6、活更久。</summary>
	public class FireplaceWarriorProjectileEX : FireplaceWarriorProjectile
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 44;
			Projectile.height = 44;
			Projectile.penetrate = 6;
			Projectile.timeLeft = 90;
			Projectile.light = 0.7f;
		}
	}

	/// <summary>专属·冷光重弹：更大、穿透 2、更亮。</summary>
	public class FireplaceMageProjectileEX : FireplaceMageProjectile
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 200;
			Projectile.light = 1f;
		}
	}

	/// <summary>专属·穿甲钉弹：更粗、穿透 2、飞得更快。</summary>
	public class FireplaceRangerProjectileEX : FireplaceRangerProjectile
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.penetrate = 2;
			Projectile.extraUpdates = 2;
			Projectile.light = 0.7f;
		}
	}

	/// <summary>专属·回旋重刃：更大、穿透 6、活更久。</summary>
	public class FireplaceRogueProjectileEX : FireplaceRogueProjectile
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.penetrate = 6;
			Projectile.timeLeft = 360;
		}
	}
}
