using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Buffs;

namespace WastelandSoul.Content.Projectiles
{
	/// <summary>
	/// 污染团：清道夫「过载修复」阶段的核心威胁。
	/// <para/>缓慢追踪玩家，隔一段时间就会撞上来；速度与转向都慢，所以跑得动就能甩开一段。
	/// <para/>命中或消散时会留下一小片污染区域。
	/// </summary>
	public class PollutionHoming : ModProjectile
	{
		/// <summary>追踪速度（像素/tick）。比玩家走速略慢，跑位就能拉开。</summary>
		private const float HomingSpeed = 5.4f;

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 480;
			Projectile.alpha = 30;
		}

		public override void AI()
		{
			Player target = FindTarget();

			if (target != null) {
				Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * HomingSpeed;
				// 转向很慢：给出躲避空间
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.05f);
			}
			else {
				Projectile.velocity *= 0.98f;
			}

			Projectile.rotation += Projectile.velocity.X * 0.02f;

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 120, default, 1.2f);
			}
		}

		private static Player FindTarget()
		{
			Player best = null;
			float bestDistance = float.MaxValue;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];

				if (!player.active || player.dead || player.ghost) {
					continue;
				}

				float distance = Vector2.Distance(player.Center, Main.player[i].Center);

				if (distance < bestDistance) {
					bestDistance = distance;
					best = player;
				}
			}

			return best;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info)
		{
			target.AddBuff(ModContent.BuffType<Pollution>(), 300);
		}

		public override void OnKill(int timeLeft)
		{
			// 落地/消散时留下小片污染区域（保留「污染区域」这一设定）
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<PollutionZone>(), 14, 0f, Main.myPlayer);
			}

			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 12; i++) {
				Vector2 velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, velocity.X, velocity.Y, 100, default, 1.4f);
			}
		}
	}
}
