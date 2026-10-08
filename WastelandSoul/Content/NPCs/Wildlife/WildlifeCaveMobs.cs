using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Buffs;
using Terraria.GameContent.ItemDropRules;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.NPCs.Wildlife
{
	// ====================================================================================
	// 地下 / 洞穴生态位：
	//   · CaveCrawler  贴墙爬行（重力由代码接管，靠"贴墙回弹"实现在墙上爬）
	//   · AshTickBat   蝙蝠式乱飞（游荡向量 + 抢脸俯冲 + 被打散）
	// 两只都没有做地形改动，也不需要碰 Main.tile。
	// ====================================================================================

	/// <summary>
	/// 穴居攀爬者：多足、会**贴墙往上爬**并追着玩家走；被逼急了会横向扑一下。
	/// <para/>实现要点：不用原版的爬墙 AI（蜘蛛那套），而是
	/// 「朝目标方向推 → 撞墙就把水平速度压向墙面并给爬升分量」，正面撞不到墙就自然下落。
	/// 这样在上坡、天花板、竖直墙面三种地形上都能顺着走，不需要额外寻路。
	/// </summary>
	public class CaveCrawler : ModNPC
	{
		private const float ClimbSpeed = 3.0f;
		private const float PushSpeed = 0.6f;
		private const float WalkSpeed = 2.4f;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.width = 36;
			NPC.height = 36;
			NPC.damage = 26;
			NPC.defense = 8;
			NPC.lifeMax = 150;
			NPC.HitSound = SoundID.NPCHit2;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 110f;
			NPC.knockBackResist = 0.35f;
			NPC.noGravity = true;      // 重力在 AI 里手动给，免得贴墙时被拽下来
			NPC.noTileCollide = true;  // 不靠引擎推，自己判断墙面
			NPC.aiStyle = -1;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!WildlifeAI.OutsideSubworld(spawnInfo)) {
				return 0f;
			}

			// 地下开始就有；困难模式之后变多（那时它是基础威胁之一）
			if (!spawnInfo.Player.ZoneDirtLayerHeight && !spawnInfo.Player.ZoneRockLayerHeight) {
				return 0f;
			}

			return Main.hardMode ? 0.06f : 0.04f;
		}

		public override void FindFrame(int frameHeight)
		{
			// 爬得快时腿也动得快
			WildlifeAI.AdvanceFrame(NPC, Math.Abs(NPC.velocity.Y) > 1.2f ? 4 : 8);
		}

		public override void AI()
		{
			// localAI[0]：扑击的方向（0 表示不扑）
			// localAI[1]：扑击剩余时间
			// localAI[2]：被卡住的连续计时（>26 就换方向）
			// localAI[3]：左右搜索方向
			if (!WildlifeAI.FindClosestPlayer(NPC, 760f, out Player target)) {
				NPC.velocity.X *= 0.94f;
				NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.3f, 6f);
				WildlifeAI.AdvanceFrame(NPC, 9);
				return;
			}

			if (NPC.localAI[3] == 0f) {
				NPC.localAI[3] = 1f;
			}

			Vector2 toTarget = target.Center - NPC.Center;
			float distance = toTarget.Length();
			float dir = Math.Abs(toTarget.X) < 6f ? NPC.localAI[3] : Math.Sign(toTarget.X);
			NPC.spriteDirection = Math.Sign(dir) == 0 ? NPC.spriteDirection : Math.Sign(dir);

			// ---------- 扑击 ----------
			if (NPC.localAI[1] > 0f) {
				NPC.localAI[1] -= 1f;
				NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(NPC.localAI[0] * 5.6f, -2.6f), 0.16f);
				WildlifeAI.AdvanceFrame(NPC, 4);
				return;
			}

			if (WildlifeAI.IsGrounded(NPC) && distance < 130f && NPC.localAI[1] <= 0f && Main.rand.NextBool(40)) {
				NPC.localAI[0] = Math.Sign(toTarget.X);
				NPC.localAI[1] = 22f;
				NPC.netUpdate = true;

				if (!Main.dedServ) {
					SoundEngine.PlaySound(SoundID.NPCHit2 with { Pitch = 0.3f, Volume = 0.4f }, NPC.Center);
				}

				return;
			}

			// ---------- 爬行 ----------
			Vector2 desired = Vector2.Normalize(toTarget + new Vector2(0f, toTarget.X * 0.22f)) * WalkSpeed;
			NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.09f);

			bool attached = false;

			if (Math.Abs(dir) > 0.05f) {
				if (WildlifeAI.ApplyWallCling(NPC, dir, ClimbSpeed, PushSpeed)) {
					attached = true;
				}
				else if (WildlifeAI.ApplyCeilingCling(NPC, dir, WalkSpeed * 0.8f)) {
					attached = true;
				}
			}

			if (!attached) {
				NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.22f, 5f);
			}

			// ---------- 卡住检测：几乎没位移就换搜索方向，再给一下蹬腿 ----------
			if (NPC.velocity.Length() < 0.22f) {
				NPC.localAI[2] += 1f;

				if (NPC.localAI[2] > 26f) {
					NPC.localAI[3] = -NPC.localAI[3];
					NPC.velocity.Y = -3.4f;
					NPC.localAI[2] = 0f;
				}
			}
			else {
				NPC.localAI[2] = 0f;
			}

			WildlifeAI.AdvanceFrame(NPC, Math.Abs(NPC.velocity.Y) > 1.2f ? 4 : 8);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			if (Main.dedServ || NPC.life > 0) {
				return;
			}

			for (int i = 0; i < 14; i++) {
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default, 1.2f);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 3, 6));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 2, 1, 3));
			npcLoot.Add(ItemDropRule.OneFromOptions(40, WildlifeAI.ArchivistCLine()));
		}
	}

	/// <summary>
	/// 灰蜱蝠：蝙蝠式乱飞 —— 无规律游荡，靠得够近就**直线抢脸俯冲**，撞墙后被打散重来。
	/// <para/>与鬼火的差异：鬼火是"盘旋—锁定—俯冲"的可读节奏，它是"乱飞 + 突然抢脸"，
	/// 命中前没有明显前摇，但会撞墙、撞完有一段僵直。
	/// </summary>
	public class AshTickBat : ModNPC
	{
		private const float WanderSpeed = 3.4f;
		private const float SwoopSpeed = 8.6f;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.width = 34;
			NPC.height = 34;
			NPC.damage = 24;
			NPC.defense = 6;
			NPC.lifeMax = 110;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath4;
			NPC.value = 85f;
			NPC.knockBackResist = 0.8f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!WildlifeAI.OutsideSubworld(spawnInfo)) {
				return 0f;
			}

			// 洞穴层（石层）为主，困难模式之后整片地下都有
			if (!spawnInfo.Player.ZoneRockLayerHeight && !spawnInfo.Player.ZoneDirtLayerHeight) {
				return 0f;
			}

			return 0.05f;
		}

		public override void FindFrame(int frameHeight)
		{
			WildlifeAI.AdvanceFrame(NPC, NPC.localAI[0] == 1f ? 3 : 6);
		}

		public override void AI()
		{
			// localAI[0]：0 = 游荡，1 = 抢脸俯冲
			// localAI[1]：状态计时
			// localAI[2] / localAI[3]：游荡目标点
			if (!WildlifeAI.FindClosestPlayer(NPC, 700f, out Player target)) {
				NPC.velocity *= 0.96f;
				WildlifeAI.AdvanceFrame(NPC, 8);
				return;
			}

			Vector2 toTarget = target.Center - NPC.Center;
			float distance = toTarget.Length();

			switch ((int)NPC.localAI[0]) {
				case 0:
					NPC.localAI[1] += 1f;

					// 每 20~40 帧换一个游荡目标（乱飞的关键）
					if (NPC.localAI[1] > 22f || NPC.localAI[2] == 0f) {
						NPC.localAI[1] = 0f;
						NPC.localAI[2] = target.Center.X + Main.rand.Next(-320, 321);
						NPC.localAI[3] = target.Center.Y + Main.rand.Next(-240, 121);
					}

					Vector2 wanderTarget = new Vector2(NPC.localAI[2], NPC.localAI[3]);
					Vector2 steer = wanderTarget - NPC.Center;

					if (steer.LengthSquared() > 1f) {
						steer = Vector2.Normalize(steer) * WanderSpeed;
					}

					NPC.velocity = Vector2.Lerp(NPC.velocity, steer + Main.rand.NextVector2Circular(1.1f, 1.1f), 0.07f);

					if (distance < 260f && Math.Abs(toTarget.Y) < 200f && Main.rand.NextBool(50)) {
						NPC.localAI[0] = 1f;
						NPC.localAI[1] = 0f;
						NPC.velocity = Vector2.Normalize(toTarget) * SwoopSpeed;
						NPC.netUpdate = true;

						if (!Main.dedServ) {
							SoundEngine.PlaySound(SoundID.NPCDeath4 with { Pitch = 0.5f, Volume = 0.4f }, NPC.Center);
						}
					}

					break;

				default:
					// 抢脸：一路直线，撞到实体方块就散开
					NPC.localAI[1] += 1f;

					bool bumped = WildlifeAI.CheckDirection(NPC, Math.Sign(NPC.velocity.X))
						|| Collision.SolidCollision(NPC.position + new Vector2(0f, NPC.velocity.Y > 0f ? 8f : -8f), NPC.width, 4);

					if (bumped || NPC.localAI[1] > 46f || distance > 620f) {
						NPC.localAI[0] = 0f;
						NPC.localAI[1] = 0f;
						NPC.localAI[2] = 0f;
						NPC.velocity *= -0.35f;
						NPC.netUpdate = true;

						if (bumped && !Main.dedServ) {
							SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = -0.2f, Volume = 0.4f }, NPC.Center);
						}
					}

					break;
			}

			NPC.spriteDirection = NPC.velocity.X > 0.05f ? 1 : (NPC.velocity.X < -0.05f ? -1 : NPC.spriteDirection);
			NPC.rotation = MathHelper.Clamp(NPC.velocity.Y * 0.05f, -0.5f, 0.5f);
			WildlifeAI.AdvanceFrame(NPC, NPC.localAI[0] == 1f ? 3 : 6);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 2, 5));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 2, 1, 3));
			npcLoot.Add(ItemDropRule.OneFromOptions(45, WildlifeAI.ArchivistCLine()));
		}
	}
}
