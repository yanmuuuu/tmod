using System;
using InnoVault.StateMachines;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Common.Systems;
using WastelandSoul.Content.Items.Bags;
using WastelandSoul.Content.Projectiles.Archivist;

namespace WastelandSoul.Content.NPCs.Bosses.Archivist
{
	/// <summary>
	/// Boss 2：归档者（Archivist）。
	/// <para/>「守望者计划」的审计单元，专门回收原型机外泄的记忆——它的出现解释了智械人的数据为何开始不稳定。
	/// <para/>定位：骷髅王之后（BossChecklist 进度 3.5）。
	/// <para/>美术风格：档案 / 索引 / 纸页 + 骨白冷色（与清道夫的冷灰废料、灰烬之心的暖橙余烬、壁炉守卫的冷白金属区分）。
	/// <para/>行为逻辑交给 InnoVault 的状态机：三阶段共 9 个 <c>[VaultState]</c> 状态，见 <c>ArchivistStates.cs</c>。
	/// </summary>
	[AutoloadBossHead]
	public class Archivist : ModNPC
	{
		// ==================== 阶段阈值 ====================

		/// <summary>阶段二「归档」（65%）。</summary>
		public const float PhaseTwoThreshold = 0.65f;

		/// <summary>阶段三「覆写」（30%）。</summary>
		public const float PhaseThreeThreshold = 0.30f;

		/// <summary>「终盘压制」：血低于 15% 后不再轮换常规招式，改为高频弹幕压制。</summary>
		public const float SuppressionThreshold = 0.15f;

		// ==================== 攻击间隔（越小越凶） ====================

		internal const int AttackIntervalPhaseOne = 165;
		internal const int AttackIntervalPhaseTwo = 120;
		internal const int AttackIntervalPhaseThree = 95;

		/// <summary>终盘压制阶段的攻击间隔（比阶段三再快一档）。</summary>
		internal const int AttackIntervalSuppressed = 62;

		// ==================== 各招式前摇 / 持续 ====================

		internal const int BeamWindup = 42;                // 索引光束：瞄准前摇（亮光预警）
		internal const int BeamSweepTicks = 34;            // 索引光束：横扫段
		internal const int BeamLockTicks = 22;             // 索引光束：锁定段
		internal const float BeamSweepDegreesPerTick = 3f; // 索引光束：横扫每帧转动角度（×34 帧 ≈ 100°）
		internal const float BeamAimDegreesPerTick = 2.9f; // 索引光束：前摇跟踪每帧最大转角

		internal const int PageWindup = 40;                // 抛索引页前摇
		internal const int WallWindup = 48;                // 弹幕墙起手
		internal const int WallAdvanceTicks = 96;          // 弹幕墙推进时长
		internal const int SealWindup = 42;                // 归档封印前摇（期间玩家被封住）
		internal const int SealWarnTicks = 30;             // 封印落地后的预警（0.5 秒），之后才真正封住
		internal const int SealHoldTicks = 30;             // 封印把玩家定住的时长（ArchivistSeal 直接读这两个常量）
		internal const int LungeTicks = 34;                // 突刺持续
		internal const int OverwriteTelegraph = 60;        // 轨迹回放的 1 秒预警
		internal const int OverwriteFireInterval = 5;      // 回放时每 5 帧落一格

		// ==================== 伤害（设计要点给定的定位数值） ====================

		/// <summary>接触伤害。</summary>
		internal const int ContactDamage = 40;

		/// <summary>突刺期间的接触伤害。</summary>
		internal const int LungeDamage = 55;

		/// <summary>索引光束伤害。</summary>
		internal const int BeamDamage = 30;

		/// <summary>索引页伤害。</summary>
		internal const int PageDamage = 18;

		/// <summary>弹幕墙纸页伤害。</summary>
		internal const int BarrageDamage = 22;

		/// <summary>终盘压制纸页伤害。</summary>
		internal const int SuppressionDamage = 24;

		// ==================== 运动参数 ====================

		internal const float HoverDefault = 260f;
		internal const float HoverHigh = 320f;

		/// <summary>
		/// 突刺速度。原来 15 显得"贴脸就到"，初期 Boss 下调到 13。
		/// </summary>
		internal const float LungeSpeed = 13f;

		internal const float MaxTurnPerTickLunge = 6f;     // 突刺每 tick 最多转 6°

		/// <summary>悬浮速度：追得远时快一点、接近时慢下来（原来 13 / 8，整体下调）。</summary>
		internal const float HoverSpeedFar = 10.5f;
		internal const float HoverSpeedNear = 6.5f;

		public override void SetStaticDefaults()
		{
			// 本体贴图：110 宽 × 4 帧 × 110 高（纵向排列，与原版 Boss 约定一致）
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.MPAllowedEnemies[Type] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 110;
			NPC.height = 110;
			NPC.damage = ContactDamage;
			NPC.defDamage = ContactDamage;
			NPC.defense = 22;              // ⚠️ 待定项：开发说明只给了伤害与血量，防御先按"骷髅王后时期"取 22
			NPC.lifeMax = 9000;            // 设计要点：普通模式约 9000
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.boss = true;
			NPC.aiStyle = -1;              // 自定义 AI（由状态机驱动）
			NPC.npcSlots = 12f;
			NPC.value = Item.buyPrice(gold: 12);
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			Music = MusicLoader.GetMusicSlot(Mod, "Music/Archivist");
		}

		// ==================== 主 AI ====================

		public override void AI()
		{
			ArchivistContext context = ArchivistContext.For(NPC);
			context.Target = FindTarget(NPC);

			// 没有可攻击目标：缓慢上浮退场，避免卡在世界里
			if (context.Target == null) {
				ArchivistContext.Release(NPC.whoAmI);
				NPC.velocity.X *= 0.95f;
				NPC.velocity.Y -= 0.2f;
				NPC.EncourageDespawn(30);
				return;
			}

			NPC.target = context.Target.whoAmI;

			// 「覆写」阶段的素材：持续记录玩家轨迹（与状态无关，一直跑）
			TrackTarget(context);

			// 状态推进 + 阶段判定（PhaseController 在血量跌破阈值时一次性切阶段）
			context.Machine.Update();

			// 始终面向目标
			NPC.spriteDirection = context.Target.Center.X > NPC.Center.X ? 1 : -1;
			NPC.rotation = MathHelper.Clamp(NPC.velocity.X * 0.012f, -0.22f, 0.22f);
		}

		/// <summary>目标选择：最近的存活玩家（归档者不做"优先攻击弱者"那套，它只归档最近的样本）。</summary>
		internal static Player FindTarget(NPC npc)
		{
			Player best = null;
			float bestDistance = 3600f;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];

				if (!player.active || player.dead || player.ghost) {
					continue;
				}

				float distance = Vector2.Distance(player.Center, npc.Center);

				if (distance < bestDistance) {
					bestDistance = distance;
					best = player;
				}
			}

			return best;
		}

		/// <summary>按固定间隔把目标位置压进轨迹表（只保留最近 <see cref="ArchivistContext.MaxTrailPoints"/> 个采样点）。</summary>
		private static void TrackTarget(ArchivistContext context)
		{
			if (Main.GameUpdateCount % ArchivistContext.TrailSampleInterval != 0u) {
				return;
			}

			context.TargetTrail.Add(context.Target.Center);

			while (context.TargetTrail.Count > ArchivistContext.MaxTrailPoints) {
				context.TargetTrail.RemoveAt(0);
			}
		}

		// ==================== 运动辅助（供状态类调用） ====================

		/// <summary>中距离悬浮：停在目标斜上方，靠得太近就往外挪——它要保持"能看清整个档案"的距离。</summary>
		internal static void Hover(ArchivistContext context, float distance)
		{
			NPC npc = context.Npc;

			if (context.Target == null) {
				return;
			}

			Vector2 hoverPoint = context.Target.Center + new Vector2(0f, -distance);
			Vector2 offset = hoverPoint - npc.Center;
			float speed = offset.Length() > 420f ? HoverSpeedFar : HoverSpeedNear;

			if (offset.LengthSquared() > 36f) {
				npc.velocity = (npc.velocity * 10f + Vector2.Normalize(offset) * speed) / 11f;
			}
			else {
				npc.velocity *= 0.92f;
			}
		}

		/// <summary>朝目标甩出一次位移（突刺用）。</summary>
		internal static void DashTowards(ArchivistContext context, float speed)
		{
			if (context.Target == null) {
				return;
			}

			Vector2 direction = context.Target.Center - context.Npc.Center;

			if (direction.LengthSquared() < 1f) {
				direction = Vector2.UnitY;
			}

			context.Npc.velocity = Vector2.Normalize(direction) * speed;
		}

		/// <summary>突刺途中轻微修正方向（能拐，但拐不快——它毕竟是纸页做的审计单元）。</summary>
		internal static void SteerLunge(ArchivistContext context)
		{
			if (context.Target == null) {
				return;
			}

			NPC npc = context.Npc;
			float speed = npc.velocity.Length();

			if (speed < 1f) {
				speed = LungeSpeed;
			}

			float current = npc.velocity.ToRotation();
			Vector2 toTarget = context.Target.Center - npc.Center;
			float wanted = toTarget.LengthSquared() > 1f ? toTarget.ToRotation() : current;
			float delta = MathHelper.WrapAngle(wanted - current);
			float maxTurn = MathHelper.ToRadians(MaxTurnPerTickLunge);

			npc.velocity = (current + MathHelper.Clamp(delta, -maxTurn, maxTurn)).ToRotationVector2() * speed;
		}

		/// <summary>接触伤害的临时改写与恢复（突刺期间抬高一档）。</summary>
		internal static void SetContactDamage(NPC npc, int damage)
		{
			npc.damage = damage;
			npc.defDamage = damage;
		}

		// ==================== 攻击：索引光束 ====================

		/// <summary>
		/// 索引光束的当前朝向：前摇阶段跟踪玩家（缓慢跟随），发射后先横扫再锁定。
		/// <para/>角度存在 <c>ai[3]</c>，由状态类每帧调用并写回。
		/// </summary>
		internal static float StepBeamAngle(ArchivistContext context, int timer)
		{
			NPC npc = context.Npc;

			if (context.Target == null) {
				return npc.ai[3];
			}

			float wanted = (context.Target.Center - npc.Center).ToRotation();

			if (npc.ai[3] == 0f) {
				npc.ai[3] = wanted;   // 起手对齐一次
			}

			if (timer < BeamWindup) {
				// 前摇：慢速跟踪（亮光预警的同时给出"它在瞄你"的信号）
				float delta = MathHelper.WrapAngle(wanted - npc.ai[3]);
				float maxTurn = MathHelper.ToRadians(BeamAimDegreesPerTick);
				npc.ai[3] = MathHelper.WrapAngle(npc.ai[3] + MathHelper.Clamp(delta, -maxTurn, maxTurn));
			}
			else if (timer < BeamWindup + BeamSweepTicks) {
				// 横扫段：朝一侧匀速扫过约 100°（起手时离目标的方向决定扫向）
				int sweepDirection = MathHelper.WrapAngle(wanted - npc.ai[3]) >= 0f ? 1 : -1;
				npc.ai[3] = MathHelper.WrapAngle(npc.ai[3] + MathHelper.ToRadians(BeamSweepDegreesPerTick) * sweepDirection);
			}
			else {
				// 锁定段：重新锁到玩家身上，之后不再转动
				float delta = MathHelper.WrapAngle(wanted - npc.ai[3]);
				npc.ai[3] = MathHelper.WrapAngle(npc.ai[3] + MathHelper.Clamp(delta, -0.12f, 0.12f));
			}

			return npc.ai[3];
		}

		/// <summary>把索引光束挂在眼部：生成一条跟着本体走的激光（服务端生成，位置每帧同步）。</summary>
		internal static void SpawnIndexBeam(ArchivistContext context)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			Projectile.NewProjectile(
				context.Npc.GetSource_FromAI(),
				context.Npc.Center,
				Vector2.Zero,
				ModContent.ProjectileType<ArchivistIndexBeam>(),
				BeamDamage,
				0f,
				Main.myPlayer,
				context.Npc.whoAmI,
				BeamWindup + BeamSweepTicks + BeamLockTicks);
		}

		// ==================== 攻击：档案页 / 弹幕墙 ====================

		/// <summary>抛出索引页（扇形，可弹墙 2 次、命中挂困惑）。</summary>
		internal static void ThrowIndexPages(ArchivistContext context, int count, float speed)
		{
			if (context.Target == null || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			Vector2 aim = context.Target.Center - context.Npc.Center;

			if (aim.LengthSquared() < 1f) {
				aim = Vector2.UnitY;
			}

			float step = 0.18f;
			float start = -step * (count - 1) * 0.5f;

			for (int i = 0; i < count; i++) {
				Vector2 velocity = Vector2.Normalize(aim).RotatedBy(start + step * i) * speed;

				Projectile.NewProjectile(
					context.Npc.GetSource_FromAI(),
					context.Npc.Center,
					velocity,
					ModContent.ProjectileType<ArchivistArchivePage>(),
					PageDamage,
					1f,
					Main.myPlayer);
			}
		}

		/// <summary>
		/// 弹幕墙：沿一侧排开一整列纸页向另一侧推进，**每列随机留一个可躲的缝隙**。
		/// <para/>缝隙之外还有一处固定缺口，保证不会出现"整面墙没缝"的无解情况。
		/// </summary>
		internal static void SpawnBarrageWall(ArchivistContext context, int pageCount, int damage)
		{
			if (context.Target == null || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			NPC npc = context.Npc;
			Player target = context.Target;

			// 从玩家的哪一侧起手：优先从"离得远的一侧"推过来，给玩家留反应时间
			float side = target.Center.X >= npc.Center.X ? -1f : 1f;
			float spawnX = target.Center.X + side * 900f;

			// 竖直方向沿玩家所在的屏幕高度排开
			float centerY = target.Center.Y - 40f;
			float spacing = 76f;
			float startY = centerY - spacing * (pageCount - 1) * 0.5f;

			// 缝隙：随机挑一格（并且再挑一格，两格不会相邻）
			int gapA = Main.rand.Next(pageCount);
			int gapB = (gapA + pageCount / 2 + Main.rand.Next(2)) % pageCount;

			for (int i = 0; i < pageCount; i++) {
				if (i == gapA || i == gapB) {
					continue;   // 留出可躲的缝隙
				}

				Vector2 position = new Vector2(spawnX, startY + spacing * i);

				Projectile.NewProjectile(
					npc.GetSource_FromAI(),
					position,
					new Vector2(-side * 6.2f, 0f),
					ModContent.ProjectileType<ArchivistBarrageSheet>(),
					damage,
					1f,
					Main.myPlayer);
			}
		}

		// ==================== 攻击：归档封印 ====================

		/// <summary>在玩家脚下放下「归档封印」：0.5 秒预警后把玩家短暂封在原地。</summary>
		internal static void PlaceSeal(ArchivistContext context)
		{
			if (context.Target == null || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			Projectile.NewProjectile(
				context.Npc.GetSource_FromAI(),
				context.Target.Center,
				Vector2.Zero,
				ModContent.ProjectileType<ArchivistSeal>(),
				0,
				0f,
				Main.myPlayer,
				context.Target.whoAmI);
		}

		// ==================== 攻击：轨迹回放（覆写） ====================

		/// <summary>
		/// 沿记录下来的玩家轨迹落纸页：「你的路径已被归档」。
		/// <para/>调用方负责先等 1 秒预警（<see cref="OverwriteTelegraph"/>），再开始按
		/// <paramref name="index"/> 从轨迹的最新点往旧点回放。
		/// </summary>
		internal static void ReplayTrailPoint(ArchivistContext context, int index)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			if (index < 0 || index >= context.TargetTrail.Count) {
				return;
			}

			Vector2 point = context.TargetTrail[index];

			Projectile.NewProjectile(
				context.Npc.GetSource_FromAI(),
				point,
				new Vector2(0f, 0.6f),
				ModContent.ProjectileType<ArchivistBarrageSheet>(),
				SuppressionDamage,
				1f,
				Main.myPlayer);
		}

		// ==================== 出招选择 ====================

		/// <summary>掷下一次攻击的等待帧数（终盘压制阶段额外提速）。</summary>
		internal static int RollAttackDelay(ArchivistContext context)
		{
			NPC npc = context.Npc;

			if (npc.life / (float)npc.lifeMax <= SuppressionThreshold) {
				return AttackIntervalSuppressed + Main.rand.Next(-8, 9);
			}

			return (int)npc.ai[0] switch {
				>= 2 => AttackIntervalPhaseThree + Main.rand.Next(-12, 13),
				1 => AttackIntervalPhaseTwo + Main.rand.Next(-15, 16),
				_ => AttackIntervalPhaseOne + Main.rand.Next(-20, 21)
			};
		}

		/// <summary>
		/// 待机结束后挑下一招。轮换规则：
		/// <list type="bullet">
		/// <item>**终盘压制**（≤15%）：不再轮换，全部改为高频弹幕压制</item>
		/// <item>阶段一「检索」：索引光束 / 档案页 交替</item>
		/// <item>阶段二「归档」：弹幕墙 / 档案页 / 归档封印 轮换</item>
		/// <item>阶段三「覆写」：轨迹回放 / 弹幕墙 / 归档封印 轮换（回放占固定一档）</item>
		/// </list>
		/// </summary>
		internal static IVaultState<ArchivistContext> ChooseNextState(ArchivistContext context)
		{
			NPC npc = context.Npc;
			int phase = (int)npc.ai[0];
			int cycle = (int)npc.ai[2];
			npc.ai[2] += 1f;

			// 终盘压制：血低于 15% 后不再有任何"花招"，只剩铺满屏幕的纸页
			if (npc.life / (float)npc.lifeMax <= SuppressionThreshold) {
				return new ArchivistSuppressionState();
			}

			if (phase >= 2) {
				return (cycle % 3) switch {
					0 => new ArchivistOverwriteState(),
					1 => new ArchivistBarrageWallState(),
					_ => new ArchivistSealLungeState()
				};
			}

			if (phase == 1) {
				return (cycle % 3) switch {
					0 => new ArchivistBarrageWallState(),
					1 => new ArchivistPageVolleyState(),
					_ => new ArchivistSealLungeState()
				};
			}

			// 阶段一「检索」：只用两种检索手段
			return cycle % 2 == 0 ? new ArchivistIndexBeamState() : new ArchivistPageVolleyState();
		}

		/// <summary>阶段开始时的表现与重置。</summary>
		internal static void OnPhaseStart(NPC npc, int phase)
		{
			npc.ai[0] = phase;
			SetContactDamage(npc, ContactDamage);
			npc.velocity *= 0.3f;
			npc.netUpdate = true;

			if (Main.dedServ) {
				return;
			}

			SoundEngine.PlaySound(SoundID.Roar, npc.Center);

			if (phase == 1) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.ArchivistPhaseTwo"), 190, 200, 226);
			}
			else if (phase == 2) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.ArchivistPhaseThree"), 150, 176, 226);
			}

			for (int i = 0; i < 36; i++) {
				Vector2 velocity = Main.rand.NextVector2Circular(3.6f, 3.6f);
				Dust.NewDust(npc.position, npc.width, npc.height, DustID.BlueTorch, velocity.X, velocity.Y, 100, default, 1.4f);
				Dust.NewDust(npc.position, npc.width, npc.height, DustID.Bone, velocity.X, velocity.Y, 100, default, 1.2f);
			}
		}

		/// <summary>纸页翻动 / 索引刷新的音效。</summary>
		internal static void PlayCastSound(NPC npc)
		{
			if (!Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item24, npc.Center);
			}
		}

		/// <summary>封印落下的音效。</summary>
		internal static void PlaySealSound(NPC npc)
		{
			if (!Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item14, npc.Center);
			}
		}

		// ==================== 帧动画 ====================

		public override void FindFrame(int frameHeight)
		{
			NPC.frameCounter += 1.0;

			// 纸页翻动式的迟滞动画
			if (NPC.frameCounter >= 7.0) {
				NPC.frameCounter = 0.0;
				NPC.frame.Y += frameHeight;

				if (NPC.frame.Y >= Main.npcFrameCount[Type] * frameHeight) {
					NPC.frame.Y = 0;
				}
			}
		}

		// ==================== 掉落与剧情 ====================

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			// 统一走掉落袋：保底材料 + 灵魂碎片·其二 + 随机词条专属武器
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ArchivistBag>(), 1));
		}

		public override void OnKill()
		{
			ArchivistContext.Release(NPC.whoAmI);
			WastelandStorySystem.MarkArchivistDefeated();
		}

		public override bool CheckActive()
		{
			return false;   // 由 AI 自己判断是否该退场
		}

		public override void BossHeadRotation(ref float rotation)
		{
			rotation = NPC.rotation;
		}
	}
}
