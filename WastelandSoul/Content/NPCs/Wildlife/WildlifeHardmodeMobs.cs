using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Systems;
using WastelandSoul.Content.Buffs;
using Terraria.GameContent.ItemDropRules;
using WastelandSoul.Content.Items.Decor;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons.Wildlife;
using WastelandSoul.Content.Projectiles;

namespace WastelandSoul.Content.NPCs.Wildlife
{
	// ====================================================================================
	// 困难模式生态位（三种机制怪 + 一只迷你 Boss）：
	//   · PollutionSlime  贴脸挂「污染」减益 + 落地留下污染区
	//   · GearSwarm       齿轮群：环绕玩家转的召唤物，本体是 GearSwarmHive
	//   · CinderMender    给同伴回血、自己开护盾（半免伤），必须先拆
	//   · ScrapReaper     迷你 Boss：两段招式（散射 → 冲击波 + 召唤齿轮群）
	// ====================================================================================

	/// <summary>
	/// 污染黏体：跳过来撞你一下挂「污染」，落地位置会留下一小片污染水洼。
	/// <para/>难度全在"减益 + 残留区域"上：单次伤害不高，但被它拖住会让防御和回血一起掉。
	/// </summary>
	public class PollutionSlime : ModNPC
	{
		private const float HopSpeed = 4.6f;
		private const float HopUp = 7.4f;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.width = 40;
			NPC.height = 30;
			NPC.damage = 38;
			NPC.defense = 16;
			NPC.lifeMax = 260;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 180f;
			NPC.knockBackResist = 0.55f;
			NPC.aiStyle = -1;
			NPC.color = new Color(150, 190, 110);
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!WildlifeAI.OutsideSubworld(spawnInfo) || !Main.hardMode) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneDirtLayerHeight && !spawnInfo.Player.ZoneOverworldHeight) {
				return 0f;
			}

			return 0.05f;
		}

		public override void FindFrame(int frameHeight)
		{
			// 空中把帧放慢（原版史莱姆的做法：滞空时看起来更"凝"）
			WildlifeAI.AdvanceFrame(NPC, NPC.velocity.Y != 0f ? 11 : 7);
		}

		public override void AI()
		{
			// localAI[0]：0 = 蠕动，1 = 蓄跳
			// localAI[1]：计时
			if (!WildlifeAI.FindClosestPlayer(NPC, 820f, out Player target)) {
				NPC.velocity.X *= 0.9f;
				NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.4f, 9f);
				WildlifeAI.AdvanceFrame(NPC, 9);
				return;
			}

			float dir = NPC.DirectionTo(target.Center).X;
			bool grounded = WildlifeAI.IsGrounded(NPC);

			if (grounded && NPC.localAI[0] == 0f) {
				NPC.localAI[1] += 1f;

				// 落地瞬间留一洼污染
				if (NPC.localAI[1] == 1f && !Main.dedServ && Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(3)) {
					Projectile.NewProjectile(
						NPC.GetSource_FromAI(),
						NPC.Bottom - new Vector2(24f, 4f),
						Vector2.Zero,
						ModContent.ProjectileType<PollutionZone>(),
						NPC.damage / 3,
						0f,
						Main.myPlayer);
				}

				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, dir * 1.4f, 0.06f);

				if (NPC.localAI[1] > 34f) {
					NPC.localAI[0] = 1f;
					NPC.localAI[1] = 0f;
				}
			}
			else if (NPC.localAI[0] == 1f) {
				// 蓄跳：压低一拍再弹
				NPC.localAI[1] += 1f;
				NPC.velocity.X *= 0.86f;

				if (NPC.localAI[1] >= 16f) {
					float jumpDir = Math.Abs(dir) < 0.05f ? NPC.spriteDirection : dir;
					NPC.velocity = new Vector2(Math.Sign(jumpDir) * HopSpeed, -HopUp);
					NPC.localAI[0] = 0f;
					NPC.localAI[1] = 0f;
					NPC.netUpdate = true;

					if (!Main.dedServ) {
						SoundEngine.PlaySound(SoundID.NPCDeath1 with { Pitch = -0.4f, Volume = 0.5f }, NPC.Center);
					}
				}
			}
			else {
				// 空中
				NPC.velocity.Y += 0.22f;
				NPC.velocity.Y = Math.Min(NPC.velocity.Y, 9f);

				if (!Main.dedServ && Main.rand.NextBool(4)) {
					Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke);
					dust.velocity *= 0.3f;
					dust.noGravity = true;
					dust.color = new Color(140, 180, 90);
				}
			}

			NPC.spriteDirection = NPC.velocity.X > 0.05f ? 1 : (NPC.velocity.X < -0.05f ? -1 : NPC.spriteDirection);
			WildlifeAI.AdvanceFrame(NPC, NPC.velocity.Y != 0f ? 11 : 7);
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
		{
			target.AddBuff(ModContent.BuffType<Pollution>(), 360);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 5, 11));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 1, 2, 4));
			npcLoot.Add(ItemDropRule.OneFromOptions(30, WildlifeAI.AshHeartCLine()));
		}
	}

	// ====================================================================================
	// 困难模式 · 齿轮群母体 + 齿轮群
	// ====================================================================================

	/// <summary>
	/// 齿轮群母体：自己很弱、不主动打人，但会**持续生产齿轮群**（最多 3 只）绕着玩家转。
	/// <para/>这是"会召唤小弟"的实现方式：小弟是可被清除的实体，不拆母体就一直有。
	/// </summary>
	public class GearSwarmHive : ModNPC
	{
		private const int MaxMinions = 3;
		private const int SpawnCooldown = 150;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.width = 32;
			NPC.height = 32;
			NPC.damage = 12;
			NPC.defense = 20;
			NPC.lifeMax = 230;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = 170f;
			NPC.knockBackResist = 0.4f;
			NPC.noGravity = true;
			NPC.aiStyle = -1;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!WildlifeAI.OutsideSubworld(spawnInfo) || !Main.hardMode) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneRockLayerHeight && !spawnInfo.Player.ZoneDirtLayerHeight) {
				return 0f;
			}

			return 0.03f;
		}

		public override void FindFrame(int frameHeight)
		{
			WildlifeAI.AdvanceFrame(NPC, 9);
		}

		/// <summary>场上已有几只齿轮群。</summary>
		public static int CountMinions(int ownerIndex)
		{
			int count = 0;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (npc.active && npc.type == ModContent.NPCType<GearSwarm>() && (int)npc.ai[0] == ownerIndex) {
					count++;
				}
			}

			return count;
		}

		public override void AI()
		{
			// localAI[0]：生产冷却
			if (!WildlifeAI.FindClosestPlayer(NPC, 900f, out Player target)) {
				NPC.velocity *= 0.95f;
				WildlifeAI.AdvanceFrame(NPC, 9);
				return;
			}

			// 悬浮在玩家斜上方，保持中等距离
			float wantedX = target.Center.X - Math.Sign(target.Center.X - NPC.Center.X) * 220f;
			float wantedY = target.Center.Y - 170f;
			Vector2 steer = new Vector2(wantedX - NPC.Center.X, wantedY - NPC.Center.Y);

			if (steer.LengthSquared() > 1f) {
				steer = Vector2.Normalize(steer) * 2.6f;
			}

			NPC.velocity = Vector2.Lerp(NPC.velocity, steer, 0.05f);
			NPC.spriteDirection = NPC.velocity.X > 0.05f ? 1 : (NPC.velocity.X < -0.05f ? -1 : NPC.spriteDirection);

			// 生产齿轮群
			NPC.localAI[0] += 1f;

			if (NPC.localAI[0] < SpawnCooldown) {
				WildlifeAI.AdvanceFrame(NPC, 9);
				return;
			}

			NPC.localAI[0] = 0f;

			if (Main.dedServ || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			if (CountMinions(NPC.whoAmI) >= MaxMinions) {
				return;
			}

			int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<GearSwarm>());

			if (index >= 0 && index < Main.maxNPCs) {
				NPC minion = Main.npc[index];
				minion.ai[0] = NPC.whoAmI;                                  // 归属
				minion.ai[1] = Main.rand.NextFloat(0f, MathHelper.TwoPi);   // 环绕相位
				minion.netUpdate = true;
			}

			if (!Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item37 with { Pitch = 0.3f, Volume = 0.5f }, NPC.Center);
			}

			WildlifeAI.AdvanceFrame(NPC, 9);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 5; i++) {
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Iron, 0f, 0f, 100, default, 1f);
			}

			if (NPC.life <= 0) {
				for (int i = 0; i < 16; i++) {
					Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default, 1.2f);
				}
			}
		}

		public override void OnKill()
		{
			// 母体死了，它生的小齿轮一起散架
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (npc.active && npc.type == ModContent.NPCType<GearSwarm>() && (int)npc.ai[0] == NPC.whoAmI) {
					npc.life = 0;
					npc.HitEffect();
					npc.active = false;
					npc.netUpdate = true;
				}
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 6, 12));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 1, 3, 6));
			npcLoot.Add(ItemDropRule.OneFromOptions(25, WildlifeAI.AshHeartCLine()));
		}
	}

	/// <summary>
	/// 齿轮群：绕着玩家飞、碰到就刮一下，接触就换位。
	/// <para/>它是"可清除的小弟"：不打母体它会一直补，所以正确解法是先拆母体。
	/// </summary>
	public class GearSwarm : ModNPC
	{
		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 1;   // 单帧贴图，旋转交给 NPC.rotation
		}

		public override void SetDefaults()
		{
			NPC.width = 32;
			NPC.height = 32;
			NPC.damage = 26;
			NPC.defense = 6;
			NPC.lifeMax = 60;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = 20f;
			NPC.knockBackResist = 0.7f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
		}

		public override void AI()
		{
			// ai[0]：母体的 NPC 索引
			// ai[1]：环绕相位
			// localAI[0]：相位推进速度
			NPC owner = null;
			int ownerIndex = (int)NPC.ai[0];

			if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs && Main.npc[ownerIndex].active) {
				owner = Main.npc[ownerIndex];
			}

			if (!WildlifeAI.FindClosestPlayer(NPC, 1200f, out Player target)) {
				NPC.velocity *= 0.95f;
				return;
			}

			// 母体死了就自己散架（上面的 OnKill 也会清，这里是兜底）
			if (owner == null) {
				NPC.localAI[1] += 1f;

				if (NPC.localAI[1] > 60f) {
					NPC.life = 0;
					NPC.active = false;
					NPC.netUpdate = true;
				}

				return;
			}

			float phase = NPC.ai[1] + NPC.localAI[0] * 0.035f;
			NPC.localAI[0] += 1f;

			// 环绕半径带一点正弦抖动，轨迹不是完美圆
			float radius = 88f + (float)Math.Sin(phase * 2.3f) * 26f;
			Vector2 orbit = target.Center + new Vector2((float)Math.Cos(phase) * radius, (float)Math.Sin(phase) * radius * 0.7f - 30f);
			Vector2 steer = orbit - NPC.Center;

			if (steer.LengthSquared() > 1f) {
				steer = Vector2.Normalize(steer) * Math.Min(9f, steer.Length() * 0.14f);
			}

			NPC.velocity = Vector2.Lerp(NPC.velocity, steer, 0.14f);
			NPC.rotation += 0.22f;

			if (!Main.dedServ && Main.rand.NextBool(6)) {
				Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Iron);
				dust.velocity *= 0.2f;
				dust.noGravity = true;
				dust.scale = 0.7f;
			}
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
		{
			// 刮一下就把玩家往外推一点，形成"甩不掉但能被推散"的感觉
			Vector2 push = target.Center - NPC.Center;

			if (push.LengthSquared() > 1f) {
				push = Vector2.Normalize(push) * 3.2f;
				target.velocity += push;
			}
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 3; i++) {
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Iron, 0f, 0f, 100, default, 0.9f);
			}
		}
	}

	// ====================================================================================
	// 困难模式 · 煤渣修补者（自我治疗 / 护盾）
	// ====================================================================================

	/// <summary>
	/// 煤渣修补者：**不直接打你**，但会给附近所有敌对 NPC 回血，并周期性给自己套护盾
	/// （护盾期间受到的伤害减半）。
	/// <para/>设计意图：它是"必须先拆"的支援单位 —— 不清掉它，前面的怪血条会被慢慢顶回去。
	/// 护盾期间面罩变亮、身上冒青色粒子，玩家能直接看出"现在打不动"。
	/// </summary>
	public class CinderMender : ModNPC
	{
		private const int HealInterval = 170;
		private const int HealRange = 520;
		private const int HealAmount = 14;
		private const int ShieldDuration = 240;
		private const int ShieldCooldown = 420;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.width = 46;
			NPC.height = 46;
			NPC.damage = 30;
			NPC.defense = 22;
			NPC.lifeMax = 420;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = 260f;
			NPC.knockBackResist = 0.25f;
			NPC.noGravity = true;
			NPC.aiStyle = -1;
		}

		/// <summary>护盾是否开着（localAI[2] > 0）。</summary>
		private bool Shielded => NPC.localAI[2] > 0f;

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!WildlifeAI.OutsideSubworld(spawnInfo) || !Main.hardMode) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneRockLayerHeight && !spawnInfo.Player.ZoneDirtLayerHeight) {
				return 0f;
			}

			return 0.022f;
		}

		public override void FindFrame(int frameHeight)
		{
			WildlifeAI.AdvanceFrame(NPC, Shielded ? 12 : 8);
		}

		public override void AI()
		{
			// localAI[0]：治疗冷却
			// localAI[1]：护盾冷却
			// localAI[2]：护盾剩余时间
			if (!WildlifeAI.FindClosestPlayer(NPC, 1000f, out Player target)) {
				NPC.velocity *= 0.95f;
				WildlifeAI.AdvanceFrame(NPC, 10);
				return;
			}

			// ---------- 悬浮：站远一点，躲在别的怪后面 ----------
			float side = target.Center.X < NPC.Center.X ? 1f : -1f;
			float wantedX = target.Center.X + side * 260f;
			float wantedY = target.Center.Y - 90f;
			Vector2 steer = new Vector2(wantedX - NPC.Center.X, wantedY - NPC.Center.Y);

			if (steer.LengthSquared() > 1f) {
				steer = Vector2.Normalize(steer) * 2.2f;
			}

			NPC.velocity = Vector2.Lerp(NPC.velocity, steer, 0.05f);
			NPC.spriteDirection = side > 0f ? -1 : 1;

			// ---------- 治疗 ----------
			NPC.localAI[0] += 1f;

			if (NPC.localAI[0] >= HealInterval) {
				NPC.localAI[0] = 0f;
				HealNearbyAllies();
			}

			// ---------- 护盾 ----------
			if (Shielded) {
				NPC.localAI[2] -= 1f;
				// 护盾期间受到的伤害减半：takenDamageMultiplier 是原版自带的乘算减免
				NPC.takenDamageMultiplier = 0.5f;

				if (!Main.dedServ && Main.rand.NextBool(2)) {
					Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Electric);
					dust.velocity = Main.rand.NextVector2Circular(1.2f, 1.2f);
					dust.noGravity = true;
					dust.scale = 0.8f;
					dust.color = new Color(160, 210, 255);
				}

				if (NPC.localAI[2] <= 0f) {
					NPC.localAI[2] = 0f;
					NPC.localAI[1] = 0f;
					NPC.takenDamageMultiplier = 1f;
					NPC.netUpdate = true;
				}
			}
			else {
				NPC.takenDamageMultiplier = 1f;
				NPC.localAI[1] += 1f;

				if (NPC.localAI[1] >= ShieldCooldown) {
					NPC.localAI[1] = 0f;
					NPC.localAI[2] = ShieldDuration;
					NPC.takenDamageMultiplier = 0.5f;
					NPC.netUpdate = true;

					if (!Main.dedServ) {
						SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.2f, Volume = 0.5f }, NPC.Center);
					}
				}
			}

			if (!Main.dedServ) {
				Lighting.AddLight(NPC.Center, Shielded ? 0.28f : 0.12f, Shielded ? 0.4f : 0.22f, 0.42f);
			}

			WildlifeAI.AdvanceFrame(NPC, Shielded ? 12 : 8);
		}

		/// <summary>给范围内所有敌对 NPC（不含自己）回血，每次治疗都画一条可见的修补光。</summary>
		private void HealNearbyAllies()
		{
			bool healed = false;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC other = Main.npc[i];

				if (!other.active || other.whoAmI == NPC.whoAmI || other.friendly || other.life >= other.lifeMax) {
					continue;
				}

				if (Vector2.Distance(other.Center, NPC.Center) > HealRange) {
					continue;
				}

				other.life = Math.Min(other.lifeMax, other.life + HealAmount);
				other.netUpdate = true;
				healed = true;

				if (!Main.dedServ) {
					for (int k = 0; k < 6; k++) {
						Vector2 spawn = Vector2.Lerp(NPC.Center, other.Center, k / 5f);
						Dust dust = Dust.NewDustDirect(spawn, 4, 4, DustID.Electric);
						dust.velocity *= 0.2f;
						dust.noGravity = true;
						dust.scale = 0.9f;
						dust.color = new Color(140, 220, 200);
					}
				}
			}

			if (healed && !Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.45f, Volume = 0.35f }, NPC.Center);
			}
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 4; i++) {
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default, 1f);
			}

			if (NPC.life <= 0) {
				for (int i = 0; i < 20; i++) {
					Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, 0f, 100, default, 1.4f);
				}
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 8, 16));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 1, 3, 7));
			npcLoot.Add(ItemDropRule.OneFromOptions(20, WildlifeAI.AshHeartCLine()));
		}
	}

	// ====================================================================================
	// 迷你 Boss · 废料收割者
	// ====================================================================================

	/// <summary>
	/// 废料收割者（迷你 Boss，血量 2800，介于普通怪与 Boss 之间）：
	/// <list type="number">
	/// <item><b>第一段（&gt;55% 血）</b>：压上来用巨刃横扫，并每 190 帧打一次三连废料散射；</item>
	/// <item><b>第二段（≤55% 血）</b>：核心过载 —— 移速 +40%、冷却砍到 110 帧、
	/// 散射变五连，并新增 <b>冲击波</b>（环身 14 发废料）与<b>召唤齿轮群</b>。</item>
	/// </list>
	/// 两段之间有一次明确的"过载"演出（停下、核心变亮、卸掉一批废料），玩家能读出来换段了。
	/// <para/>必掉一件专属武器（废料收割者之刃）+ 大量芯片与锈蚀齿轮。
	/// </summary>
	public class ScrapReaper : ModNPC
	{
		private const float PhaseOneSpeed = 1.9f;
		private const float PhaseTwoSpeed = 2.7f;
		private const int PhaseOneCooldown = 190;
		private const int PhaseTwoCooldown = 110;
		private const int BarrageWindup = 26;
		private const int ShockwaveWindup = 46;
		private const int MaxMinions = 3;
		private const float PhaseTwoThreshold = 0.55f;

		/// <summary>是否已进入第二阶段。</summary>
		private bool PhaseTwo => NPC.life <= NPC.lifeMax * PhaseTwoThreshold;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 3;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 84;
			NPC.height = 84;
			NPC.damage = 46;
			NPC.defense = 26;
			NPC.lifeMax = 2800;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = 2500f;
			NPC.knockBackResist = 0.16f;
			NPC.aiStyle = -1;
			NPC.boss = false;
			NPC.lavaImmune = true;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			// 只有打赢过清道夫、并且进了困难模式，它才会作为"野外威胁"出现
			if (!WildlifeAI.OutsideSubworld(spawnInfo) || !Main.hardMode || !WastelandStorySystem.scavengerDefeated) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneRockLayerHeight && !spawnInfo.Player.ZoneOverworldHeight) {
				return 0f;
			}

			// 迷你 Boss 刻意刷得稀：大约每 3~5 分钟遇见一次的量级
			return 0.02f;
		}

		public override void FindFrame(int frameHeight)
		{
			// 帧 0 = 待机，帧 1 = 核心过载（蓄力），帧 2 = 攻击
			int wanted;

			if ((int)NPC.localAI[0] == 4f) {
				wanted = 1;
			}
			else if ((int)NPC.localAI[0] >= 1f) {
				wanted = 2;
			}
			else {
				wanted = 0;
			}

			NPC.frame.Y = wanted * frameHeight;
			NPC.frameCounter = 0.0;
		}

		public override void AI()
		{
			// localAI[0]：0 = 推进，1 = 散射前摇，2 = 散射后硬直，3 = 冲击波前摇，4 = 过载过场，5 = 召唤齿轮群
			// localAI[1]：状态计时
			// localAI[2]：攻击冷却
			if (!WildlifeAI.FindClosestPlayer(NPC, 1400f, out Player target)) {
				NPC.velocity.X *= 0.9f;
				WildlifeAI.MoveGroundStep(NPC);
				return;
			}

			float dir = NPC.DirectionTo(target.Center).X;
			float speed = PhaseTwo ? PhaseTwoSpeed : PhaseOneSpeed;
			bool grounded = WildlifeAI.IsGrounded(NPC);

			switch ((int)NPC.localAI[0]) {
				case 0:
					// 推进：压上去近战
					float wanted = Math.Abs(NPC.Center.X - target.Center.X) < 60f ? 0f : dir;
					WildlifeAI.MoveGround(NPC, wanted, speed, 0.14f, 0.4f, 6.6f);
					WildlifeAI.StepUpIfLedge(NPC, wanted);

					if (grounded) {
						NPC.spriteDirection = NPC.velocity.X > 0.05f ? 1 : (NPC.velocity.X < -0.05f ? -1 : NPC.spriteDirection);
					}

					// 过载演出只播一次
					if (PhaseTwo && NPC.localAI[3] == 0f) {
						NPC.localAI[3] = 1f;
						NPC.localAI[0] = 4f;
						NPC.localAI[1] = 0f;
						NPC.velocity.X = 0f;
						NPC.netUpdate = true;
						break;
					}

					NPC.localAI[2] += 1f;

					if (NPC.localAI[2] >= (PhaseTwo ? PhaseTwoCooldown : PhaseOneCooldown) && grounded) {
						NPC.localAI[2] = 0f;

						if (PhaseTwo && Main.rand.NextBool(3)) {
							NPC.localAI[0] = 3f;
						}
						else {
							NPC.localAI[0] = 1f;
						}

						NPC.localAI[1] = 0f;
						NPC.velocity.X *= 0.3f;
						NPC.netUpdate = true;
					}

					break;

				case 1:
					// ---------- 散射前摇 ----------
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.85f;

					if (!Main.dedServ && NPC.localAI[1] % 4f == 0f) {
						Dust dust = Dust.NewDustDirect(NPC.Center - new Vector2(6f, 6f), 12, 12, DustID.Torch);
						dust.velocity *= 0.4f;
						dust.noGravity = true;
						dust.scale = 1.2f;
					}

					if (NPC.localAI[1] >= BarrageWindup) {
						FireBarrage(target);
						NPC.localAI[0] = 2f;
						NPC.localAI[1] = 0f;
						NPC.netUpdate = true;
					}

					break;

				case 2:
					// ---------- 散射后硬直（给玩家窗口） ----------
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.92f;

					if (NPC.localAI[1] >= 34f) {
						NPC.localAI[0] = 0f;
						NPC.localAI[1] = 0f;
					}

					break;

				case 3:
					// ---------- 冲击波前摇 ----------
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.8f;

					if (!Main.dedServ) {
						float radius = 20f + NPC.localAI[1] * 1.1f;

						for (int i = 0; i < 3; i++) {
							float angle = Main.rand.NextFloat(MathHelper.TwoPi);
							Vector2 spawn = NPC.Center + angle.ToRotationVector2() * radius;
							Dust dust = Dust.NewDustDirect(spawn, 4, 4, DustID.Torch);
							dust.velocity = (spawn - NPC.Center) * 0.05f;
							dust.noGravity = true;
							dust.scale = 1.1f;
						}
					}

					if (NPC.localAI[1] >= ShockwaveWindup) {
						FireShockwave();
						SpawnGearMinions();
						NPC.localAI[0] = 2f;
						NPC.localAI[1] = 0f;
						NPC.netUpdate = true;
					}

					break;

				case 4:
					// ---------- 过载演出：卸掉一批废料、核心变亮 ----------
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.9f;

					if (!Main.dedServ) {
						for (int i = 0; i < 4; i++) {
							Vector2 spawn = NPC.Center + Main.rand.NextVector2Circular(34f, 34f);
							Dust dust = Dust.NewDustDirect(spawn, 6, 6, DustID.Smoke);
							dust.velocity = Main.rand.NextVector2Circular(3.4f, 3.4f);
							dust.velocity.Y -= 1.4f;
							dust.scale = 1.3f;
						}

						Lighting.AddLight(NPC.Center, 0.5f, 0.24f, 0.1f);
					}

					if (NPC.localAI[1] == 40f && !Main.dedServ) {
						SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.2f, Volume = 0.7f }, NPC.Center);
					}

					if (NPC.localAI[1] >= 62f) {
						NPC.localAI[0] = 0f;
						NPC.localAI[1] = 0f;
						NPC.netUpdate = true;
					}

					break;

				default:
					// ---------- 召唤齿轮群（只在二阶段） ----------
					NPC.localAI[1] += 1f;
					NPC.velocity.X *= 0.9f;

					if (NPC.localAI[1] >= 40f) {
						NPC.localAI[0] = 0f;
						NPC.localAI[1] = 0f;
					}

					break;
			}

			WildlifeAI.MoveGroundStep(NPC);

			// 二阶段核心发光
			if (PhaseTwo && !Main.dedServ) {
				Lighting.AddLight(NPC.Center, 0.42f, 0.2f, 0.08f);
			}
		}

		/// <summary>三连 / 五连废料散射（朝玩家带一个小扇形）。</summary>
		private void FireBarrage(Player target)
		{
			if (Main.dedServ || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			int count = PhaseTwo ? 5 : 3;
			float spread = PhaseTwo ? 0.42f : 0.3f;
			Vector2 baseDirection = Vector2.Normalize(target.Center - NPC.Center);

			for (int i = 0; i < count; i++) {
				float offset = (i - (count - 1) / 2f) * (spread / Math.Max(1, count - 1));
				Vector2 velocity = baseDirection.RotatedBy(offset) * 8.6f;

				Projectile.NewProjectile(
					NPC.GetSource_FromAI(),
					NPC.Center + velocity * 2f,
					velocity,
					ModContent.ProjectileType<Content.Projectiles.Wildlife.ReaperScrapShot>(),
					NPC.damage / 2,
					3f,
					Main.myPlayer);
			}

			if (!Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item11 with { Pitch = -0.25f, Volume = 0.6f }, NPC.Center);
			}
		}

		/// <summary>环身 14 发冲击波弹幕。</summary>
		private void FireShockwave()
		{
			if (Main.dedServ || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			const int count = 14;

			for (int i = 0; i < count; i++) {
				float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.06f, 0.06f);
				Vector2 velocity = angle.ToRotationVector2() * 7.2f;

				Projectile.NewProjectile(
					NPC.GetSource_FromAI(),
					NPC.Center + velocity * 2.2f,
					velocity,
					ModContent.ProjectileType<Content.Projectiles.Wildlife.ReaperShockwave>(),
					NPC.damage / 2,
					2f,
					Main.myPlayer);
			}

			if (!Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.3f, Volume = 0.6f }, NPC.Center);
			}
		}

		/// <summary>二阶段叫一批齿轮群帮忙（上限 3 只）。</summary>
		private void SpawnGearMinions()
		{
			if (Main.dedServ || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			if (GearSwarmHive.CountMinions(NPC.whoAmI) >= MaxMinions) {
				return;
			}

			int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<GearSwarm>());

			if (index >= 0 && index < Main.maxNPCs) {
				NPC minion = Main.npc[index];
				minion.ai[0] = NPC.whoAmI;
				minion.ai[1] = Main.rand.NextFloat(0f, MathHelper.TwoPi);
				minion.netUpdate = true;
			}
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 4; i++) {
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Iron, 0f, 0f, 100, default, 1.2f);
			}

			if (NPC.life > 0) {
				return;
			}

			for (int i = 0; i < 60; i++) {
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default, 1.6f);
			}

			for (int i = 0; i < 24; i++) {
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, 0f, 100, default, 1.8f);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, 40, 60));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustedGear>(), 1, 8, 14));
			// 必掉专属武器（迷你 Boss 的固定回报）+ 10% 奖杯（和四个 Boss 的奖杯同规格）
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScrapReaperBlade>(), 1));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScrapReaperTrophy>(), 10));
			npcLoot.Add(ItemDropRule.OneFromOptions(2, WildlifeAI.AshHeartCLine()));
		}
	}
}
