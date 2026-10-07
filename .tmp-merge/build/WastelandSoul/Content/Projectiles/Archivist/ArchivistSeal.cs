using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArchivistNpc = WastelandSoul.Content.NPCs.Bosses.Archivist.Archivist;

namespace WastelandSoul.Content.Projectiles.Archivist
{
	/// <summary>
	/// 归档者的「归档封印」：先落下一圈索引符文做 0.5 秒预警，然后把玩家**短时间封在原地**。
	/// <para/>ai[0] = 被封印的玩家 whoAmI。
	/// <para/>这是纯控制类弹幕（不给伤害），真正的威胁是封印期间本体蓄力的那次高伤突刺。
	/// <para/>⚠️ 预警 / 定身时长直接读 <see cref="ArchivistNpc"/> 顶部的常量：
	/// 以前这里各写一份 const，改 Boss 常量时封印毫无反应（数值集中原则在这里最容易破）。
	/// </summary>
	public class ArchivistSeal : ModProjectile
	{
		/// <summary>封印半径。</summary>
		private const float SealRadius = 46f;

		public override void SetDefaults()
		{
			Projectile.width = 72;
			Projectile.height = 72;
			Projectile.hostile = false;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = ArchivistNpc.SealWarnTicks + ArchivistNpc.SealHoldTicks;
		}

		private Player SealedPlayer()
		{
			int index = (int)Projectile.ai[0];

			if (index < 0 || index >= Main.maxPlayers) {
				return null;
			}

			Player player = Main.player[index];

			return player.active && !player.dead ? player : null;
		}

		public override void AI()
		{
			Player player = SealedPlayer();

			// 预警阶段跟随玩家（封印还没锁死），之后固定在原地
			if (Projectile.timeLeft > ArchivistNpc.SealHoldTicks && player != null) {
				Projectile.Center = player.Center;
			}

			Projectile.velocity = Vector2.Zero;
			Projectile.rotation += 0.03f;

			bool holding = Projectile.timeLeft <= ArchivistNpc.SealHoldTicks;

			if (!Main.dedServ) {
				EmitSealDust(holding);
			}

			// 封印生效：把玩家钉在圈里（位置与速度都锁住）
			if (holding && player != null && Vector2.Distance(player.Center, Projectile.Center) <= SealRadius) {
				player.velocity = Vector2.Zero;
				player.position = Projectile.Center - player.Size * 0.5f;
				player.fallStart = (int)(player.position.Y / 16f);
			}
		}

		private void EmitSealDust(bool holding)
		{
			int count = holding ? 3 : 2;

			for (int i = 0; i < count; i++) {
				// 符文沿圆周向内收拢：预警时向外飘，封住后向内压
				float angle = Main.rand.NextFloat(MathHelper.TwoPi);
				Vector2 offset = angle.ToRotationVector2() * (SealRadius + Main.rand.NextFloat(-6f, 10f));
				Vector2 position = Projectile.Center + offset;
				Vector2 velocity = holding ? -offset * 0.05f : offset * 0.03f;

				Dust dust = Dust.NewDustDirect(position, 4, 4, holding ? DustID.BlueTorch : DustID.Bone, velocity.X, velocity.Y);
				dust.noGravity = true;
				dust.scale = holding ? 1.1f : 0.8f;
			}
		}

		public override bool? CanDamage()
		{
			return false;   // 纯控制，不造成伤害
		}
	}
}
