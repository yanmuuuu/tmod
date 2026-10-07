using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.NPCs.Bosses.Scavenger
{
	/// <summary>
	/// 维修无人机：清道夫「过载修复」阶段的产物。
	/// <para/>不攻击玩家，只给 Boss 回血；玩家必须优先清理它们，否则 Boss 血量会被拖住。
	/// </summary>
	public class ScavengerRepairDrone : ModNPC
	{
		private const int HealInterval = 30;
		private const int HealAmount = 25;
		private const float HoverRadius = 120f;

		public override void SetStaticDefaults()
		{
			// 4 帧纵向排列的悬浮动画
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 30;
			NPC.height = 26;
			NPC.aiStyle = -1;
			NPC.damage = 0;            // 不攻击玩家
			NPC.defDamage = 0;
			NPC.defense = 6;
			NPC.lifeMax = 180;         // 待定项：具体数值
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.knockBackResist = 0.25f;
			NPC.npcSlots = 0f;
			NPC.value = 0f;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
		}

		public override void AI()
		{
			// ai[0] = 父级 Boss 的 whoAmI；ai[1] = 分配的环绕槽位（由 Boss 召唤时写入）
			int bossIndex = (int)NPC.ai[0];

			if (bossIndex < 0 || bossIndex >= Main.maxNPCs) {
				Despawn();
				return;
			}

			NPC boss = Main.npc[bossIndex];

			if (!boss.active || boss.life <= 0) {
				Despawn();
				return;
			}

			// 阶段三（协议崩溃）起机体不再接受维修，无人机自行退场
			if ((int)boss.ai[0] >= 2) {
				Despawn();
				return;
			}

			int slot = (int)NPC.ai[1];
			float orbitAngle = slot * MathHelper.TwoPi / 3f + Main.GameUpdateCount * 0.012f;
			Vector2 hoverPoint = boss.Center + orbitAngle.ToRotationVector2() * HoverRadius;

			Vector2 toHover = hoverPoint - NPC.Center;
			NPC.velocity = NPC.velocity * 0.90f + toHover * 0.06f;
			NPC.rotation = NPC.velocity.X * 0.04f;

			// 治疗：定时给 Boss 回血（只有服务端/单机实际改血量，客户端靠同步表现）
			NPC.localAI[0] += 1f;

			if (NPC.localAI[0] >= HealInterval) {
				NPC.localAI[0] = 0f;

				if (Main.netMode != NetmodeID.MultiplayerClient) {
					int heal = Math.Min(HealAmount, boss.lifeMax - boss.life);

					if (heal > 0) {
						boss.life += heal;
						boss.HealEffect(heal, true);
					}
				}

				// 维修光束：Boss 与无人机之间的一串火花
				if (!Main.dedServ) {
					for (int i = 0; i < 8; i++) {
						float t = i / 8f;
						Vector2 point = Vector2.Lerp(NPC.Center, boss.Center, t);

						Dust dust = Dust.NewDustPerfect(point, DustID.Torch, Vector2.Zero, 100, default, 1.1f);
						dust.noGravity = true;
					}
				}
			}

			// 破损推进器的污染颗粒
			if (!Main.dedServ && Main.rand.NextBool(6)) {
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, -1f, 120, default, 0.9f);
			}
		}

		private void Despawn()
		{
			NPC.active = false;

			if (Main.netMode == NetmodeID.Server) {
				NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
			}
		}

		public override void FindFrame(int frameHeight)
		{
			NPC.frameCounter += 1.0;

			if (NPC.frameCounter >= 5.0) {
				NPC.frameCounter = 0.0;
				NPC.frame.Y += frameHeight;

				if (NPC.frame.Y >= Main.npcFrameCount[Type] * frameHeight) {
					NPC.frame.Y = 0;
				}
			}
		}
	}
}
