using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Buffs;
using Terraria.GameContent.ItemDropRules;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons.CLine;

namespace WastelandSoul.Content.NPCs.Wildlife
{
	// ====================================================================================
	// 小怪扩充（批次 W7）：一群 AI 各自不同的小怪。
	//
	// 设计原则（对着玩家「要行为差异，不要只是换色」的要求）：
	//   · 白天地面：跳跃扑咬（ScrapLeaper）/ 蓄力冲锋（RustCharger）
	//   · 夜晚空中：俯冲追踪（GaleWisp）/ 保持距离远程喷吐（SpitterFly）
	//   · 地下洞窟：贴墙爬行（CaveCrawler）/ 乱飞抢脸（AshTickBat）
	//   · 困难模式：贴脸挂减益（PollutionSlime）/ 召唤齿轮群（GearSwarm）/ 治疗+护盾（CinderMender）
	//   · 迷你 Boss：废料收割者（ScrapReaper，两段招式）
	//
	// 注意：本文件里的怪**不继承** EraMobs.cs 的 EraMob —— 那个抽象基类刻意为「四个时期」
	// 的通用怪设计（4 帧 + 纯掉芯片），这里的怪要各自写 AI 与掉落，所以各写各的。
	// EraMobs.cs 是别的代理在维护的文件，本批次一个字符都没有动它。
	// ====================================================================================

	/// <summary>
	/// 小怪共用的工具：目标选择、站地判定、贴墙/贴顶判定、前方法向速度。
	/// <para/>不继承 <see cref="ModNPC"/>，纯静态工具。
	/// </summary>
	public static class WildlifeAI
	{
		/// <summary>子世界（壁炉，很小的地图）里不刷新，避免把大厅填满怪。</summary>
		public static bool OutsideSubworld(NPCSpawnInfo spawnInfo)
		{
			return Main.maxTilesX >= 1200 && !spawnInfo.PlayerSafe && spawnInfo.Player.active;
		}

		/// <summary>取最近的存活玩家（不限定视线，近战怪不需要）。</summary>
		public static bool FindClosestPlayer(NPC npc, float maxDistance, out Player target)
		{
			target = null;
			float best = maxDistance;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];

				if (!player.active || player.dead) {
					continue;
				}

				float distance = Vector2.Distance(player.Center, npc.Center);

				if (distance < best) {
					best = distance;
					target = player;
					npc.target = i;
				}
			}

			return target != null;
		}

		/// <summary>直线视野（被墙挡住就看不见）。远程怪用它，免得隔墙喷。</summary>
		public static bool CanSee(NPC npc, Player player)
		{
			return Collision.CanHitLine(npc.position, npc.width, npc.height, player.position, player.width, player.height);
		}

		/// <summary>脚下有没有实体方块（站在地上 / 站在墙上都算）。</summary>
		public static bool IsGrounded(NPC npc)
		{
			return Collision.SolidCollision(new Vector2(npc.position.X, npc.Bottom.Y), npc.width, 4);
		}

		/// <summary>朝 dir 的方向贴墙时，把速度压到墙面上并给一个爬升分量。</summary>
		public static bool ApplyWallCling(NPC npc, float dir, float climbSpeed, float pushSpeed)
		{
			bool wall = CheckDirection(npc, dir);

			if (!wall) {
				return false;
			}

			npc.velocity.X = dir * pushSpeed;

			if (npc.velocity.Y > -climbSpeed) {
				npc.velocity.Y -= 0.22f;
			}

			npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y, -climbSpeed, 2f);
			return true;
		}

		/// <summary>贴天花板：把上升速度压掉，并给一点水平推力。</summary>
		public static bool ApplyCeilingCling(NPC npc, float dir, float crawlSpeed)
		{
			if (!Collision.SolidCollision(new Vector2(npc.position.X, npc.position.Y - 4f), npc.width, 4)) {
				return false;
			}

			npc.velocity.Y = 1f;

			if (Math.Abs(npc.velocity.X) < crawlSpeed) {
				npc.velocity.X += dir * 0.12f;
			}

			return true;
		}

		/// <summary>dir 方向（±1）的指定距离处是否有实体方块。距离按格给。</summary>
		public static bool CheckDirection(NPC npc, float dir, float tiles = 1.1f)
		{
			float probe = npc.width * 0.5f + tiles * 16f;
			Vector2 point = npc.Center + new Vector2(dir * probe, -npc.height * 0.15f);
			return Collision.SolidCollision(point, 4, 4);
		}

		/// <summary>地面怪用：前方是墙 / 是坑就跳（返回 true 表示已经起跳）。</summary>
		public static bool JumpIfBlocked(NPC npc, float dir, float jumpForce, float forwardSpeed)
		{
			bool blocked = Math.Abs(dir) > 0.05f && CheckDirection(npc, dir);
			bool gap = false;

			if (!blocked && Math.Abs(dir) > 0.05f) {
				Vector2 ahead = npc.Bottom + new Vector2(dir * (npc.width * 0.5f + 34f), 6f);
				gap = !Collision.SolidCollision(ahead, 6, 8);
			}

			if (!blocked && !gap) {
				return false;
			}

			npc.velocity.Y = -jumpForce;
			npc.velocity.X = dir * forwardSpeed;
			npc.netUpdate = true;
			return true;
		}

		/// <summary>把帧计数器按给定间隔推进（帧表竖直堆叠）。</summary>
		public static void AdvanceFrame(NPC npc, int interval)
		{
			npc.frameCounter += 1.0;

			if (npc.frameCounter < interval) {
				return;
			}

			npc.frameCounter = 0.0;
			int frameHeight = npc.frame.Height;
			npc.frame.Y += frameHeight;

			if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type]) {
				npc.frame.Y = 0;
			}
		}

		/// <summary>
		/// 地面怪的通用走法：朝 dir 加速到 moveSpeed，脚下没有实体方块时补重力，
		/// 撞墙 / 遇坑按需要起跳。所有地面小怪共用这一份，手感参数各自给。
		/// </summary>
		public static void MoveGround(NPC npc, float dir, float moveSpeed, float acceleration, float gravity, float jumpForce)
		{
			if (Math.Abs(dir) > 0.05f) {
				npc.velocity.X += Math.Sign(dir) * acceleration;
				npc.velocity.X = MathHelper.Clamp(npc.velocity.X, -moveSpeed, moveSpeed);
				npc.spriteDirection = Math.Sign(dir);
			}
			else {
				npc.velocity.X *= 0.9f;
			}

			if (jumpForce > 0f) {
				JumpIfBlocked(npc, dir, jumpForce, moveSpeed * 1.1f);
			}

			MoveGroundStep(npc, gravity);
		}

		/// <summary>只跑"重力 + 落地压速"这一步（不碰水平速度），用于自己管水平移动的怪。</summary>
		public static void MoveGroundStep(NPC npc, float gravity = 0.4f)
		{
			if (IsGrounded(npc)) {
				if (npc.velocity.Y > 0f) {
					npc.velocity.Y = 0f;
				}

				return;
			}

			npc.velocity.Y += gravity;
			npc.velocity.Y = Math.Min(npc.velocity.Y, 9.5f);
		}

		/// <summary>
		/// 体型大的地面怪爬台阶：贴着一格高的坎就别硬撞，直接抬一下。
		/// <para/>原版 Boss 也都做类似处理，否则 44×30 以上的怪会被一格地形卡住。
		/// </summary>
		public static void StepUpIfLedge(NPC npc, float dir)
		{
			if (Math.Abs(dir) < 0.05f || !IsGrounded(npc)) {
				return;
			}

			// 前方 1 格内有实体墙、但 2 格高处没有 → 是可以迈过去的台阶
			bool low = CheckDirection(npc, dir, 0.6f);
			bool high = CheckDirection(npc, dir, 1.9f);

			if (low && !high && npc.velocity.Y >= 0f) {
				npc.velocity.Y = -3.6f;
			}
		}

		/// <summary>一整轮循环里的小怪掉落池（本模组的「C 线」不可制作武器）。</summary>
		public static int[] EarlyCLine()
		{
			return new[] {
				ModContent.ItemType<ScavengerCWarrior>(),
				ModContent.ItemType<ScavengerCMage>(),
				ModContent.ItemType<ScavengerCRanger>(),
				ModContent.ItemType<ScavengerCSummoner>()
			};
		}

		public static int[] ArchivistCLine()
		{
			return new[] {
				ModContent.ItemType<ArchivistCWarrior>(),
				ModContent.ItemType<ArchivistCMage>(),
				ModContent.ItemType<ArchivistCRanger>(),
				ModContent.ItemType<ArchivistCSummoner>()
			};
		}

		public static int[] AshHeartCLine()
		{
			return new[] {
				ModContent.ItemType<AshHeartCWarrior>(),
				ModContent.ItemType<AshHeartCMage>(),
				ModContent.ItemType<AshHeartCRanger>(),
				ModContent.ItemType<AshHeartCSummoner>()
			};
		}
	}

	// ====================================================================================
	// 地表白天 · 废料跳虫
	// ====================================================================================

	/// <summary>
	/// 废料跳虫：蹲下蓄力 → 整只弹起来扑咬（原版史莱姆跳跃的"有前摇版"）。
	/// <para/>与清道夫小怪的差异：不走路追人，靠**一次大跳**接近；落点靠预瞄而不是追踪，
	/// 所以玩家横移就能躲开，形成"读前摇"的节奏。
	/// </summary>
	public class ScrapLeaper : ModNPC
	{
		private const float MoveSpeed = 1.45f;
		private const float Acceleration = 0.16f;
		private const int WindupTime = 34;
		private const int RecoverTime = 22;
		private const float LeapSpeedX = 6.6f;
		private const float LeapSpeedY = 8.4f;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.width = 40;
			NPC.height = 30;
			NPC.damage = 18;
			NPC.defense = 6;
			NPC.lifeMax = 85;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = 55f;
			NPC.knockBackResist = 0.5f;
			NPC.aiStyle = -1;
			NPC.lavaImmune = false;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!WildlifeAI.OutsideSubworld(spawnInfo) || Main.hardMode || !Main.dayTime) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneOverworldHeight) {
				return 0f;
			}

			return 0.05f;
		}

		public override void FindFrame(int frameHeight)
		{
			WildlifeAI.AdvanceFrame(NPC, 8);
		}

		public override void AI()
		{
			// localAI[0]：0 = 正常爬行，1 = 蓄力，2 = 空中，3 = 落地硬直
			// localAI[1]：状态计时
			if (!WildlifeAI.FindClosestPlayer(NPC, 780f, out Player target)) {
				NPC.velocity.X *= 0.9f;
				NPC.velocity.Y += 0.4f;
				WildlifeAI.AdvanceFrame(NPC, 10);
				return;
			}

			float dir = NPC.DirectionTo(target.Center).X;
			bool grounded = WildlifeAI.IsGrounded(NPC);

			switch ((int)NPC.localAI[0]) {
				case 0:
					// 爬行接近 + 遇墙/遇坑就小跳
					WildlifeAI.MoveGround(NPC, dir, MoveSpeed, Acceleration, 0.42f, 5.6f);

					if (grounded) {
						NPC.velocity.X *= 0.96f;
					}

					if (grounded && Math.Abs(NPC.Center.X - target.Center.X) < 340f) {
						NPC.localAI[0] = 1f;
						NPC.localAI[1] = 0f;
						NPC.velocity.X = 0f;
						NPC.netUpdate = true;
					}

					break;

				case 1:
					// 蓄力：原地抖 + 冒灰
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.85f;

					if (!Main.dedServ && NPC.localAI[1] % 4f == 0f) {
						Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke);
						dust.velocity *= 0.3f;
						dust.scale = 0.8f;
					}

					if (NPC.localAI[1] >= WindupTime) {
						Vector2 aim = Vector2.Normalize(target.Center - NPC.Center);
						NPC.velocity = new Vector2(aim.X * LeapSpeedX, -(LeapSpeedY * 0.55f) + aim.Y * 2f);
						NPC.localAI[0] = 2f;
						NPC.localAI[1] = 0f;
						NPC.netUpdate = true;

						if (!Main.dedServ) {
							Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath13 with { Pitch = 0.3f, Volume = 0.5f }, NPC.Center);
						}
					}

					break;

				case 2:
					// 空中：轻微朝目标修正（只修一点，所以能躲）
					NPC.localAI[1] += 1f;

					if (NPC.velocity.Y < 0f) {
						NPC.velocity.X += Math.Sign(dir) * 0.045f;
					}

					if (NPC.localAI[1] > 10f && grounded) {
						NPC.localAI[0] = 3f;
						NPC.localAI[1] = 0f;

						if (!Main.dedServ) {
							for (int i = 0; i < 10; i++) {
								Dust.NewDust(NPC.Bottom, NPC.width, 6, DustID.Smoke, 0f, 0f, 120, default, 1.1f);
							}
						}
					}

					break;

				default:
					// 落地硬直：给玩家一段可反击的窗口
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.8f;

					if (NPC.localAI[1] >= RecoverTime) {
						NPC.localAI[0] = 0f;
						NPC.localAI[1] = 0f;
					}

					break;
			}

			WildlifeAI.AdvanceFrame(NPC, NPC.velocity.Y != 0f ? 6 : 9);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 1, 3));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 3, 1, 2));
			npcLoot.Add(ItemDropRule.OneFromOptions(60, WildlifeAI.EarlyCLine()));
		}
	}

	// ====================================================================================
	// 地表白天 · 锈甲冲锋兽
	// ====================================================================================

	/// <summary>
	/// 锈甲冲锋兽：看见玩家先**跺地蓄力**，然后沿直线高速冲锋；撞墙会自己眩晕。
	/// <para/>与跳虫的差异：走的是"直线加速"而不是抛物线，玩家需要**侧向拉开**而不是跳开；
	/// 冲锋撞墙的自我硬直是刻意留的反制点。
	/// </summary>
	public class RustCharger : ModNPC
	{
		private const float WalkSpeed = 1.2f;
		private const float ChargeSpeed = 7.4f;
		private const int ChargeWindup = 42;
		private const int StunTime = 70;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.width = 44;
			NPC.height = 30;
			NPC.damage = 24;
			NPC.defense = 10;
			NPC.lifeMax = 130;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = 90f;
			NPC.knockBackResist = 0.3f;
			NPC.aiStyle = -1;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!WildlifeAI.OutsideSubworld(spawnInfo) || Main.hardMode || !Main.dayTime) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneOverworldHeight) {
				return 0f;
			}

			// 比跳虫少见一点（它更危险）
			return 0.035f;
		}

		public override void FindFrame(int frameHeight)
		{
			// 冲锋时帧放快，读得出来
			WildlifeAI.AdvanceFrame(NPC, NPC.localAI[0] == 2f ? 4 : 9);
		}

		public override void AI()
		{
			// localAI[0]：0 = 游走，1 = 蓄力，2 = 冲锋中，3 = 撞墙眩晕
			// localAI[1]：计时
			// localAI[2]：冲锋锁定的水平方向
			if (!WildlifeAI.FindClosestPlayer(NPC, 900f, out Player target)) {
				NPC.velocity.X *= 0.9f;
				WildlifeAI.MoveGround(NPC, 0f, WalkSpeed, 0.1f, 0.4f, 0f);
				return;
			}

			float dir = NPC.DirectionTo(target.Center).X;
			bool grounded = WildlifeAI.IsGrounded(NPC);
			float distance = Math.Abs(NPC.Center.X - target.Center.X);

			switch ((int)NPC.localAI[0]) {
				case 0:
					WildlifeAI.MoveGround(NPC, dir, WalkSpeed, 0.12f, 0.4f, 6.2f);

					if (grounded && distance < 460f && distance > 90f && Math.Abs(target.Center.Y - NPC.Center.Y) < 200f) {
						NPC.localAI[0] = 1f;
						NPC.localAI[1] = 0f;
						NPC.netUpdate = true;
					}

					break;

				case 1:
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.8f;

					// 跺地扬尘，越接近冲锋扬得越多
					if (!Main.dedServ && NPC.localAI[1] % 5f == 0f) {
						Dust dust = Dust.NewDustDirect(NPC.Bottom - new Vector2(NPC.width * 0.5f, 4f), NPC.width, 4, DustID.Smoke);
						dust.velocity.Y = -1.2f - NPC.localAI[1] / 60f;
						dust.velocity.X *= 0.4f;
					}

					if (NPC.localAI[1] >= ChargeWindup) {
						NPC.localAI[2] = Math.Sign(target.Center.X - NPC.Center.X);
						NPC.velocity.X = NPC.localAI[2] * ChargeSpeed;
						NPC.velocity.Y = -2.2f;   // 一点小跃，避免被一格台阶卡住
						NPC.localAI[0] = 2f;
						NPC.localAI[1] = 0f;
						NPC.netUpdate = true;

						if (!Main.dedServ) {
							Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar with { Pitch = 0.45f, Volume = 0.5f }, NPC.Center);
						}
					}

					break;

				case 2:
					NPC.localAI[1] += 1f;
					NPC.velocity.X = NPC.localAI[2] * ChargeSpeed;
					NPC.velocity.Y += 0.28f;
					NPC.velocity.Y = Math.Min(NPC.velocity.Y, 8f);
					NPC.spriteDirection = (int)NPC.localAI[2];

					// 冲锋尘土
					if (!Main.dedServ && Main.rand.NextBool(2)) {
						Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke);
						dust.velocity.X = -NPC.localAI[2] * 1.2f;
						dust.scale = 0.9f;
					}

					bool hitWall = WildlifeAI.CheckDirection(NPC, NPC.localAI[2]);

					if (hitWall || NPC.localAI[1] > 150f) {
						NPC.localAI[0] = hitWall ? 3f : 0f;
						NPC.localAI[1] = 0f;
						NPC.velocity.X *= 0.2f;
						NPC.netUpdate = true;

						if (hitWall && !Main.dedServ) {
							Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4, NPC.Center);
						}

						if (hitWall) {
							NPC.localAI[0] = 3f;
						}
					}

					break;

				default:
					// 撞墙眩晕：完全不设防
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.85f;

					if (!Main.dedServ && NPC.localAI[1] % 8f == 0f) {
						Dust.NewDust(NPC.Top, NPC.width, 4, DustID.Smoke, 0f, -0.8f, 120, default, 1f);
					}

					if (NPC.localAI[1] >= StunTime) {
						NPC.localAI[0] = 0f;
						NPC.localAI[1] = 0f;
						NPC.netUpdate = true;
					}

					break;
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 2, 4));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 2, 1, 3));
			npcLoot.Add(ItemDropRule.OneFromOptions(45, WildlifeAI.EarlyCLine()));
		}
	}

	// ====================================================================================
	// 夜晚 · 疾风鬼火（飞行追踪）
	// ====================================================================================

	/// <summary>
	/// 疾风鬼火：慢悠悠飘着，进入距离后**锁定并俯冲**一次，冲完爬升重来。
	/// <para/>与恶魔眼的差异：恶魔眼是一直蹭，它是"盘旋 → 锁定 → 直线俯冲"，
	/// 俯冲前有 24 帧对准（还能被躲），命中判定集中在俯冲那一下。
	/// </summary>
	public class GaleWisp : ModNPC
	{
		private const float HoverSpeed = 2.1f;
		private const float DiveSpeed = 9.2f;
		private const int LockTime = 24;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.width = 40;
			NPC.height = 30;
			NPC.damage = 22;
			NPC.defense = 6;
			NPC.lifeMax = 95;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath39;
			NPC.value = 75f;
			NPC.knockBackResist = 0.65f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!WildlifeAI.OutsideSubworld(spawnInfo) || Main.dayTime) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneOverworldHeight) {
				return 0f;
			}

			return 0.055f;
		}

		public override void FindFrame(int frameHeight)
		{
			// 俯冲时翅膀拍得飞快
			WildlifeAI.AdvanceFrame(NPC, NPC.localAI[0] == 2f ? 3 : 7);
		}

		public override void AI()
		{
			// localAI[0]：0 = 盘旋，1 = 锁定，2 = 俯冲，3 = 爬升复位
			if (!WildlifeAI.FindClosestPlayer(NPC, 820f, out Player target)) {
				NPC.velocity *= 0.96f;
				WildlifeAI.AdvanceFrame(NPC, 8);
				return;
			}

			Vector2 toTarget = target.Center - NPC.Center;
			float distance = toTarget.Length();
			Vector2 direction = distance < 1f ? -Vector2.UnitY : toTarget / distance;

			// 朝向：飞行怪用 spriteDirection
			NPC.spriteDirection = NPC.velocity.X > 0.05f ? 1 : (NPC.velocity.X < -0.05f ? -1 : NPC.spriteDirection);

			switch ((int)NPC.localAI[0]) {
				case 0:
					// 盘旋：朝玩家方向给一个带正弦抖动的缓速推力
					NPC.localAI[1] += 1f;
					float sway = (float)Math.Sin(NPC.localAI[1] * 0.06f) * 0.55f;
					Vector2 hover = new Vector2(direction.X + sway, direction.Y * 0.5f - 0.35f);
					hover = Vector2.Normalize(hover) * HoverSpeed;
					NPC.velocity = Vector2.Lerp(NPC.velocity, hover, 0.06f);

					if (distance < 320f) {
						NPC.localAI[0] = 1f;
						NPC.localAI[1] = 0f;
						NPC.netUpdate = true;
					}

					break;

				case 1:
					// 锁定：减速、对准、抖动（给玩家反应时间）
					NPC.localAI[1] += 1f;
					NPC.velocity = Vector2.Lerp(NPC.velocity, -direction * 1.2f, 0.12f);
					NPC.velocity += Main.rand.NextVector2Circular(0.35f, 0.35f);

					if (!Main.dedServ && Main.rand.NextBool(3)) {
						Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Electric);
						dust.velocity *= 0.2f;
						dust.noGravity = true;
						dust.scale = 0.7f;
					}

					if (NPC.localAI[1] >= LockTime) {
						NPC.velocity = direction * DiveSpeed;
						NPC.localAI[0] = 2f;
						NPC.localAI[1] = 0f;
						NPC.netUpdate = true;
					}

					break;

				case 2:
					// 俯冲：直线飞，飞满 40 帧或离太远就爬升
					NPC.localAI[1] += 1f;

					if (!Main.dedServ) {
						Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Electric);
						dust.velocity = -NPC.velocity * 0.12f;
						dust.noGravity = true;
						dust.scale = 0.85f;
					}

					if (NPC.localAI[1] > 40f || distance > 620f) {
						NPC.localAI[0] = 3f;
						NPC.localAI[1] = 0f;
					}

					break;

				default:
					// 爬升复位
					NPC.localAI[1] += 1f;
					NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(direction.X * 1.5f, -3.4f), 0.08f);

					if (NPC.localAI[1] > 38f) {
						NPC.localAI[0] = 0f;
						NPC.localAI[1] = 0f;
					}

					break;
			}

			NPC.rotation = NPC.velocity.X * 0.04f;
			WildlifeAI.AdvanceFrame(NPC, NPC.localAI[0] == 2f ? 3 : 7);

			if (!Main.dedServ) {
				Lighting.AddLight(NPC.Center, 0.16f, 0.34f, 0.42f);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 2, 5));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 2, 1, 2));
			npcLoot.Add(ItemDropRule.OneFromOptions(50, WildlifeAI.EarlyCLine()));
		}
	}

	// ====================================================================================
	// 夜晚 · 酸囊喷吐蝇（远程喷吐）
	// ====================================================================================

	/// <summary>
	/// 酸囊喷吐蝇：**刻意保持 220~330 像素距离**，停下来喷一口酸液再横移换位。
	/// <para/>它就是那种"你冲它就退、你退它就跟"的骚扰单位；近战贴脸时它没有近战伤害优势。
	/// </summary>
	public class SpitterFly : ModNPC
	{
		private const int SpitCooldown = 110;
		private const float PreferredMin = 200f;
		private const float PreferredMax = 330f;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.width = 36;
			NPC.height = 30;
			NPC.damage = 16;
			NPC.defense = 5;
			NPC.lifeMax = 90;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 80f;
			NPC.knockBackResist = 0.7f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;   // 会撞墙，所以会自己绕
			NPC.aiStyle = -1;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!WildlifeAI.OutsideSubworld(spawnInfo) || Main.dayTime) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneOverworldHeight) {
				return 0f;
			}

			return 0.04f;
		}

		public override void FindFrame(int frameHeight)
		{
			WildlifeAI.AdvanceFrame(NPC, NPC.localAI[0] == 1f ? 5 : 8);
		}

		public override void AI()
		{
			// localAI[0]：0 = 横移走位，1 = 停下喷吐
			// localAI[1]：喷吐冷却
			// localAI[2]：横移方向
			if (!WildlifeAI.FindClosestPlayer(NPC, 900f, out Player target)) {
				NPC.velocity *= 0.95f;
				WildlifeAI.AdvanceFrame(NPC, 8);
				return;
			}

			Vector2 toTarget = target.Center - NPC.Center;
			float distance = toTarget.Length();
			int facing = Math.Sign(toTarget.X);
			float dir = facing;

			if (facing != 0) {
				NPC.spriteDirection = facing;
			}

			if (NPC.localAI[2] == 0f) {
				NPC.localAI[2] = Main.rand.NextBool() ? 1f : -1f;
			}

			// 与玩家的理想高度差（悬在玩家头上一点）
			float wantedY = target.Center.Y - 60f;
			float verticalError = wantedY - NPC.Center.Y;

			switch ((int)NPC.localAI[0]) {
				case 0:
					// 走位：太近就退、太远就进、合适就横移
					float horizontal;

					if (distance < PreferredMin) {
						horizontal = -dir * 2.4f;
					}
					else if (distance > PreferredMax) {
						horizontal = dir * 2.2f;
					}
					else {
						horizontal = NPC.localAI[2] * 1.6f;

						// 横移到头就换向
						if (Math.Abs(NPC.Center.X - target.Center.X) < 90f) {
							NPC.localAI[2] = -NPC.localAI[2];
						}
					}

					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, horizontal, 0.08f);
					NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, MathHelper.Clamp(verticalError * 0.02f, -2.2f, 2.2f), 0.08f);
					break;

				default:
					// 喷吐：停住，抬高一点，前摇 22 帧后出弹
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.9f;
					NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -0.6f, 0.1f);

					if (NPC.localAI[1] == 22f) {
						if (!Main.dedServ && Main.netMode != NetmodeID.MultiplayerClient) {
							Vector2 shootDirection = Vector2.Normalize(target.Center - NPC.Center) * 7.2f;
							Projectile.NewProjectile(
								NPC.GetSource_FromAI(),
								NPC.Center,
								shootDirection,
								ModContent.ProjectileType<Content.Projectiles.Wildlife.SpitterGlob>(),
								NPC.damage / 2,
								2f,
								Main.myPlayer);
						}

						if (!Main.dedServ) {
							Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
						}
					}

					if (NPC.localAI[1] >= 34f) {
						NPC.localAI[0] = 0f;
						NPC.localAI[1] = 0f;
					}

					break;
			}

			// 冷却递减；到点且看得见才切进喷吐状态
			if (NPC.localAI[1] > 0f && NPC.localAI[0] == 0f) {
				NPC.localAI[1] -= 1f;
			}
			else if (NPC.localAI[0] == 0f) {
				SpitCooldownTick(target, distance);
			}

			WildlifeAI.AdvanceFrame(NPC, NPC.localAI[0] == 1f ? 5 : 8);
		}

		/// <summary>冷却计数与进入喷吐状态的判定。</summary>
		private void SpitCooldownTick(Player target, float distance)
		{
			NPC.localAI[3] += 1f;

			if (NPC.localAI[3] < SpitCooldown || distance > 520f) {
				return;
			}

			if (!WildlifeAI.CanSee(NPC, target)) {
				return;
			}

			NPC.localAI[3] = 0f;
			NPC.localAI[0] = 1f;
			NPC.localAI[1] = 0f;
			NPC.netUpdate = true;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 2, 4));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 2, 1, 2));
			npcLoot.Add(ItemDropRule.OneFromOptions(50, WildlifeAI.EarlyCLine()));
		}
	}
}
