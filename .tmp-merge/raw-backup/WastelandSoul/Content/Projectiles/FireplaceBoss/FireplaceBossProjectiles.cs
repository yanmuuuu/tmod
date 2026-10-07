using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.FireplaceBoss
{
	/// <summary>水平飞过的冷金属螺栓，不追踪。</summary>
	public class HearthBolt : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 8;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 240;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.3f;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch);
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}
	}

	/// <summary>从本体匀速向外飞的环碎片。生成时已经留了相邻的缺口。</summary>
	public class HearthRingShard : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 160;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.35f;
		}

		public override void AI()
		{
			Projectile.rotation += 0.18f;

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Frost);
				dust.noGravity = true;
			}
		}
	}

	/// <summary>
	/// 向内移动的火墙段。<c>ai[0]</c> 是它必须停下的 X，所以两道墙之间会留下一条缝。
	/// </summary>
	public class HearthWall : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 48;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 150;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.4f;
		}

		public override void AI()
		{
			float stopX = Projectile.ai[0];
			bool movingRight = Projectile.velocity.X > 0f;

			if ((movingRight && Projectile.Center.X >= stopX) || (!movingRight && Projectile.Center.X <= stopX)) {
				Projectile.velocity = Vector2.Zero;
				Projectile.Center = new Vector2(stopX, Projectile.Center.Y);
			}

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch);
				dust.noGravity = true;
				dust.scale = 1.1f;
			}
		}
	}
}
