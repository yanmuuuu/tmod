using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.Archivist
{
	/// <summary>
	/// 「弹幕墙」的单张纸页：沿一侧排开成一整列、横向推进的索引页。
	/// <para/>与 <see cref="ArchivistArchivePage"/> 的区别：**不弹墙、不追踪、只走直线**，
	/// 因为它的威胁来自"整面墙一起压过来"，位置一旦可预测，缝隙就是玩家的生路。
	/// <para/>生成时速度已由本体给定（见 <c>Archivist.SpawnBarrageWall</c>）。
	/// </summary>
	public class ArchivistBarrageSheet : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 52;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 600;
			Projectile.tileCollide = false;    // 墙不该被地形挡住（否则缝隙会随地形变化而"消失"）
			Projectile.ignoreWater = true;
			Projectile.light = 0.25f;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation += 0.06f * (Projectile.velocity.X >= 0f ? 1f : -1f);

			// 轻微上下摆动，让整面墙看起来像被翻动的档案
			Projectile.velocity.Y = (float)System.Math.Sin((Projectile.timeLeft + Projectile.whoAmI * 13) * 0.05f) * 0.25f;

			if (!Main.dedServ && Main.rand.NextBool(5)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Bone, 0f, 0f);
				dust.noGravity = true;
				dust.scale = 0.75f;
				dust.velocity *= 0.4f;
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 6; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, Main.rand.NextBool(3) ? DustID.BlueTorch : DustID.Bone, 0f, 0f);
				dust.noGravity = true;
				dust.scale = 0.85f;
			}
		}
	}
}
