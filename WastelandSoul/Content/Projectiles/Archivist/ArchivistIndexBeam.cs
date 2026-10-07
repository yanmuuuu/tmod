using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ArchivistNpc = WastelandSoul.Content.NPCs.Bosses.Archivist.Archivist;

namespace WastelandSoul.Content.Projectiles.Archivist
{
	/// <summary>
	/// 索引光束：从归档者眼部射出的细长激光。
	/// <para/>过程 = 明显亮光预警（细线闪烁，玩家来得及走位）→ 横扫 → 锁定段。
	/// <para/>ai[0] = 本体 whoAmI；ai[1] = 从生成到结束的总帧数；朝向每帧从本体的 <c>ai[3]</c> 读取
	/// （角度由 <see cref="ArchivistNpc.StepBeamAngle"/> 维护，这样联网时只需要同步本体）。
	/// <para/>⚠️ 朝向**只能有一个推进者**：<c>ArchivistIndexBeamState</c> 每帧推进 <c>ai[3]</c>，
	/// 这里只读不写。以前两边各推一次，横扫速度是设计值的两倍（34 帧扫了约 200° 而不是 100°）。
	/// <para/>视觉上用 1x1 白点拉伸成激光（<c>PreDraw</c> 里画），所以贴图只作为备用亮片。
	/// <para/>伤害也完全由沿光束线段的判定负责（<see cref="Colliding"/> 已关掉默认碰撞）。
	/// </summary>
	public class ArchivistIndexBeam : ModProjectile
	{
		/// <summary>
		/// 光束最大长度（像素）。
		/// <para/>⚠️ 早期 Boss 不做成"横穿屏幕"的东西：原来 1650（约 100 格）几乎覆盖整个屏幕，
		/// 玩家除了硬吃没有别的选择。现在 620（约 39 格），配合横扫依然是"必须走位"的招。
		/// </summary>
		private const float MaxLength = 620f;

		/// <summary>光束命中宽度（像素）：细长激光，靠走位躲。</summary>
		private const float BeamHitWidth = 14f;

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 24;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;      // 扫过的路径上可以打中多个玩家
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 120;
		}

		/// <summary>
		/// 关掉默认的矩形碰撞伤害：这道激光的判伤走线段（见 <see cref="DamageAlongBeam"/>），
		/// 否则玩家贴到本体眼部时会被"小碰撞箱"多打一次。
		/// <para/>注意 tModLoader 里这个钩子的返回类型是 <c>bool?</c>（<c>null</c> = 走默认逻辑）。
		/// </summary>
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			return false;
		}

		public override bool? CanDamage()
		{
			// 只有真正开火之后才判伤害；预警段不伤（否则"预警"就变成偷袭了）
			return Projectile.localAI[0] >= ArchivistNpc.BeamWindup ? null : false;
		}

		public override void AI()
		{
			int bossIndex = (int)Projectile.ai[0];

			if (bossIndex < 0 || bossIndex >= Main.maxNPCs || !Main.npc[bossIndex].active) {
				Projectile.Kill();
				return;
			}

			NPC boss = Main.npc[bossIndex];

			Projectile.localAI[0] += 1f;

			if (Projectile.localAI[0] > Projectile.ai[1]) {
				Projectile.Kill();
				return;
			}

			Vector2 origin = BeamOrigin(boss);

			// 只读本体的 ai[3]：推进由状态类负责（见类型注释里的"只能有一个推进者"）
			float angle = boss.ai[3];

			Projectile.Center = origin;
			Projectile.velocity = Vector2.Zero;
			Projectile.rotation = angle;

			// 命中判定自己走线段（贴图只是视觉，长度随脉冲变化）
			DamageAlongBeam(origin, angle);

			if (!Main.dedServ) {
				EmitBeamDust(origin, angle);
				bool charging = Projectile.localAI[0] < ArchivistNpc.BeamWindup;

				if (!charging && (int)Projectile.localAI[0] % 4 == 0) {
					Vector2 direction = angle.ToRotationVector2();
					Common.Effects.WastelandFxSystem.Bolt(origin, origin + direction * MaxLength, new Color(190, 220, 255));
					Common.Effects.WastelandFxSystem.Glow(origin, new Color(210, 230, 255), 1.4f, 6);
				}
			}
		}

		/// <summary>眼部位置：本体中心朝面向方向偏一点，看起来是从传感器射出来的。</summary>
		private static Vector2 BeamOrigin(NPC boss)
		{
			return boss.Center + new Vector2(boss.spriteDirection * 26f, -14f);
		}

		/// <summary>沿光束线段逐个检测玩家（比矩形碰撞可靠，也不受贴图长短影响）。</summary>
		private void DamageAlongBeam(Vector2 origin, float angle)
		{
			// 伤害只由服务端结算：客户端的 Hurt 调用会让血条抖动并与服务端打架
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			Vector2 end = origin + angle.ToRotationVector2() * MaxLength;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];

				if (!player.active || player.dead || player.ghost || player.immune) {
					continue;
				}

				if (DistanceToSegment(player.Center, origin, end) > BeamHitWidth + player.width * 0.35f) {
					continue;
				}

				PlayerDeathReason reason = PlayerDeathReason.ByProjectile(player.whoAmI, Projectile.whoAmI);
				player.Hurt(reason, Projectile.damage, angle.ToRotationVector2().X >= 0f ? 1 : -1);
			}
		}

		/// <summary>点到线段的距离。</summary>
		private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
		{
			Vector2 segment = end - start;
			float lengthSquared = segment.LengthSquared();

			if (lengthSquared < 1f) {
				return Vector2.Distance(point, start);
			}

			float t = MathHelper.Clamp(Vector2.Dot(point - start, segment) / lengthSquared, 0f, 1f);
			return Vector2.Distance(point, start + segment * t);
		}

		private void EmitBeamDust(Vector2 origin, float angle)
		{
			Vector2 direction = angle.ToRotationVector2();
			bool charging = Projectile.localAI[0] < ArchivistNpc.BeamWindup;

			if (charging) {
				// 亮光预警：越接近开火，眼部聚光越亮、越密
				float progress = Projectile.localAI[0] / ArchivistNpc.BeamWindup;

				for (int i = 0; i < (progress > 0.6f ? 3 : 1); i++) {
					Vector2 position = origin + direction * Main.rand.NextFloat(20f, 260f * progress + 20f);
					Dust dust = Dust.NewDustDirect(position, 4, 4, DustID.BlueTorch, 0f, 0f);
					dust.noGravity = true;
					dust.scale = 1.2f + progress;
				}

				return;
			}

			// 开火后：沿光束喷出纸屑与冷光
			for (int i = 0; i < 4; i++) {
				Vector2 position = origin + direction * Main.rand.NextFloat(0f, MaxLength);
				Dust dust = Dust.NewDustDirect(position, 4, 4, Main.rand.NextBool(3) ? DustID.Bone : DustID.BlueTorch, -direction.X * 2f, -direction.Y * 2f);
				dust.noGravity = true;
				dust.scale = 1.1f;
			}
		}

		/// <summary>光束视觉：预警段是短而细的聚光线，开火段拉满全屏。</summary>
		public override bool PreDraw(ref Color lightColor)
		{
			Vector2 origin = Projectile.Center - Main.screenPosition;
			float angle = Projectile.rotation;
			bool charging = Projectile.localAI[0] < ArchivistNpc.BeamWindup;
			float length = charging ? 60f + Projectile.localAI[0] * 8f : MaxLength;

			var pixel = TextureAssets.MagicPixel.Value;
			Color glow = (charging ? new Color(120, 170, 255) : new Color(132, 190, 255)) * 0.55f;
			Color core = charging ? new Color(180, 210, 255) * 0.6f : new Color(226, 240, 255);

			Main.EntitySpriteDraw(pixel, origin, null, glow, angle, new Vector2(0f, 0.5f),
				new Vector2(length, charging ? 7f : 17f), SpriteEffects.None, 0f);
			Main.EntitySpriteDraw(pixel, origin, null, core, angle, new Vector2(0f, 0.5f),
				new Vector2(length, charging ? 3f : 7f), SpriteEffects.None, 0f);

			return false;
		}
	}
}
