using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles
{
	/// <summary>智械人自卫时射出的细小核心火花。伤害低，只为把靠近房子的怪推开。</summary>
	public class CompanionSpark : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 50;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.4f;
			Projectile.DamageType = DamageClass.Magic;
		}

		public override void AI()
		{
			Projectile.rotation += 0.3f;

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueTorch);
				dust.noGravity = true;
				dust.scale = 0.7f;
			}
		}
	}
}
