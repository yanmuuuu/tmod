using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.Vfx
{
	/// <summary>
	/// 纯表现用的软粒子。不造成伤害。<c>ai[0]</c> 是配色：0 废金属，1 余烬，2 冷火，3 纸页。
	/// </summary>
	public class WastelandSpark : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = false;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 36;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.alpha = 20;
		}

		public override void AI()
		{
			Projectile.velocity *= 0.9f;
			Projectile.rotation += Projectile.velocity.X * 0.05f;
			Projectile.alpha += 7;

			if (Projectile.alpha >= 255) {
				Projectile.Kill();
			}
		}

		public override Color? GetAlpha(Color lightColor)
		{
			Color tint = (int)Projectile.ai[0] switch {
				1 => new Color(255, 140, 50),
				2 => new Color(170, 220, 255),
				3 => new Color(190, 210, 255),
				_ => new Color(180, 180, 190)
			};

			tint *= 1f - Projectile.alpha / 255f;
			return tint;
		}

		public static void Burst(Vector2 center, int count, int palette, float speed)
		{
			Color color = palette switch {
				1 => new Color(255, 120, 40),
				2 => new Color(160, 214, 255),
				3 => new Color(186, 206, 255),
				_ => new Color(190, 176, 150)
			};

			float power = MathHelper.Clamp(0.85f + speed * 0.12f, 0.8f, 1.7f);
			WastelandFxSystem.Impact(center, color, power);

			if (palette == 1) {
				WastelandFxSystem.Embers(center, Math.Max(4, count / 3), color);
				WastelandFxSystem.Smoke(center, new Vector2(0f, -0.7f), new Color(84, 68, 60), 1.15f, 34);
			}
			else if (palette == 2) {
				WastelandFxSystem.Ring(center, color, 22f, 96f + speed * 16f, 24);
				WastelandFxSystem.Motes(center, 52f, 6, color);
			}
			else if (palette == 3) {
				WastelandFxSystem.Flakes(center, Math.Max(6, count / 2), new Color(220, 228, 236));
			}
			else {
				WastelandFxSystem.Smoke(center, new Vector2(0f, -0.35f), new Color(116, 110, 102), 1f, 28);
			}
		}
	}
}
