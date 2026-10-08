using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.Scrap
{
	// ====================================================================================
	// 「废铁重工」线 · 战士弹幕
	//   ScrapShockwave       巨剑拍地推出的新月冲击波（穿透 5，越飞越大后消散）
	//   RebarBoomerangProj   钢筋回旋镖（飞出 35 帧后自动回手，全程穿透）
	// ====================================================================================

	/// <summary>巨剑冲击波：穿透 5 个敌人，飞行中逐渐放大并淡出，纵向速度会被迅速压平。</summary>
	public class ScrapShockwave : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 5;
			Projectile.timeLeft = 45;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.4f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			// 越飞越大（最多 1.6 倍），同时把纵向速度压平 —— 读起来像贴地推出去的
			Projectile.scale = 1f + (45 - Projectile.timeLeft) * 0.014f;
			Projectile.velocity.Y *= 0.90f;

			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverFlame);
				dust.noGravity = true;
				dust.scale = 0.9f;
				dust.velocity *= 0.4f;
			}

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.8f;
			}
		}
	}

	/// <summary>
	/// 钢筋回旋镖：飞出 OutTime 帧后回到玩家手上（
	/// 全程穿透，靠本地无敌帧限制同一敌人的命中频率）。
	/// </summary>
	public class RebarBoomerangProj : ModProjectile
	{
		/// <summary>去程持续帧数。</summary>
		protected virtual float OutTime => 35f;

		/// <summary>回程的转向力度（越大回得越急）。</summary>
		protected virtual float ReturnLerp => 0.16f;

		/// <summary>回程速度。</summary>
		protected virtual float ReturnSpeed => 15f;

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 600;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			Player player = Main.player[Projectile.owner];

			if (!player.active || player.dead) {
				Projectile.Kill();
				return;
			}

			Projectile.rotation += 0.42f * Projectile.direction;

			if (Projectile.ai[0] == 0f) {
				// 去程：略减速，时间到了切回程
				Projectile.ai[1] += 1f;
				Projectile.velocity *= 0.985f;

				if (Projectile.ai[1] > OutTime) {
					Projectile.ai[0] = 1f;
				}
			}
			else {
				Vector2 toPlayer = player.Center - Projectile.Center;

				if (toPlayer.Length() < 48f) {
					Projectile.Kill();
					return;
				}

				toPlayer.Normalize();
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, toPlayer * ReturnSpeed, ReturnLerp);
			}

			if (Main.rand.NextBool(6)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.velocity *= 0.2f;
			}
		}
	}
}
