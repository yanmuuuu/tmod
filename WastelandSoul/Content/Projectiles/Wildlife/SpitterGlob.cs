using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;
using WastelandSoul.Content.Buffs;

namespace WastelandSoul.Content.Projectiles.Wildlife
{
	/// <summary>
	/// 酸囊喷吐蝇吐出的污染酸液团：直线飞行、轻微受重力、命中给「污染」减益。
	/// <para/>不追踪 —— 玩家横移就能躲开，这样远程怪的压迫感来自数量而不是必中。
	/// </summary>
	public class SpitterGlob : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 300;
			Projectile.light = 0.25f;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.velocity.Y += 0.09f;
			Projectile.rotation += 0.14f * (Projectile.velocity.X >= 0f ? 1f : -1f);

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.velocity *= 0.25f;
				dust.noGravity = true;
				dust.scale = 0.85f;
				dust.color = new Color(140, 180, 90);
			}
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info)
		{
			target.AddBuff(ModContent.BuffType<Pollution>(), 240);
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			SoundEngine.PlaySound(SoundID.Item17 with { Pitch = -0.3f, Volume = 0.5f }, Projectile.Center);

			for (int i = 0; i < 10; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.velocity = Main.rand.NextVector2Circular(2.4f, 2.4f);
				dust.scale = 1.1f;
				dust.color = new Color(140, 180, 90);
			}
		}
	}
}
