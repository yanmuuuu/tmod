using System;
using InnoVault.StateMachines;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Common.Systems;
using WastelandSoul.Content.Items.Bags;
using WastelandSoul.Content.Items.Decor;
using WastelandSoul.Content.Projectiles.AshHeartBoss;
using WastelandSoul.Content.Projectiles.Vfx;

namespace WastelandSoul.Content.NPCs.Bosses.AshHeart
{
	/// <summary>
	/// Boss 3：灰烬之心。旧时代战争留下的自持污染源，世纪之花之后。
	/// <para/>三阶段：余烬追踪、灰雨留缝、向心脉冲。阶段二会召来给它续燃的灰烬残灵。
	/// </summary>
	[AutoloadBossHead]
	public class AshHeart : ModNPC
	{
		public const float PhaseTwoThreshold = 0.68f;
		public const float PhaseThreeThreshold = 0.34f;

		internal const int AttackIntervalPhaseOne = 150;
		internal const int AttackIntervalPhaseTwo = 112;
		internal const int AttackIntervalPhaseThree = 82;

		internal const int VolleyWindup = 36;
		internal const int PoolWindup = 28;
		internal const int RainWindup = 42;
		internal const int PulseWindup = 48;

		internal const int ContactDamage = 64;
		internal const int OrbDamage = 46;
		internal const int PoolDamage = 28;
		internal const int RainDamage = 40;
		internal const int PulseDamage = 58;

		internal const float HoverDefault = 220f;
		internal const float HoverHigh = 280f;
		internal const float HoverSpeedFar = 9f;
		internal const float HoverSpeedNear = 5.5f;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			Music = MusicLoader.GetMusicSlot(Mod, "Music/AshHeart");
		}

		public override void SetDefaults()
		{
			NPC.width = 110;
			NPC.height = 110;
			NPC.damage = ContactDamage;
			NPC.defDamage = ContactDamage;
			NPC.defense = 34;
			NPC.lifeMax = 34000;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.lavaImmune = true;
			NPC.boss = true;
			NPC.aiStyle = -1;
			NPC.npcSlots = 16f;
			NPC.value = Item.buyPrice(gold: 20);
			NPC.HitSound = SoundID.NPCHit3;
			NPC.DeathSound = SoundID.NPCDeath14;
		}

		public override void AI()
		{
			AshHeartContext context = AshHeartContext.For(NPC);
			context.Target = FindTarget(NPC);

			if (context.Target == null) {
				AshHeartContext.Release(NPC.whoAmI);
				NPC.velocity.Y -= 0.18f;
				NPC.EncourageDespawn(30);
				return;
			}

			NPC.target = context.Target.whoAmI;
			context.Machine.Update();
			NPC.spriteDirection = context.Target.Center.X > NPC.Center.X ? 1 : -1;
			NPC.rotation = MathHelper.Clamp(NPC.velocity.X * 0.01f, -0.18f, 0.18f);

			if (!Main.dedServ) {
				Lighting.AddLight(NPC.Center, 1.15f, 0.42f, 0.12f);

				if (Main.rand.NextBool(3)) {
					Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Torch);
					dust.noGravity = true;
					dust.velocity = Main.rand.NextVector2Circular(1.4f, 1.4f);
					dust.scale = 1.3f;
				}
			}
		}

		internal static Player FindTarget(NPC npc)
		{
			Player best = null;
			float bestDistance = 4200f;

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

		internal static void Hover(AshHeartContext context, float distance)
		{
			NPC npc = context.Npc;

			if (context.Target == null) {
				return;
			}

			Vector2 hoverPoint = context.Target.Center + new Vector2(0f, -distance);
			Vector2 offset = hoverPoint - npc.Center;
			float speed = offset.Length() > 480f ? HoverSpeedFar : HoverSpeedNear;

			if (offset.LengthSquared() > 36f) {
				npc.velocity = (npc.velocity * 12f + Vector2.Normalize(offset) * speed) / 13f;
			}
			else {
				npc.velocity *= 0.9f;
			}
		}

		internal static int RollAttackDelay(AshHeartContext context)
		{
			return (int)context.Npc.ai[0] switch {
				>= 2 => AttackIntervalPhaseThree + Main.rand.Next(-8, 9),
				1 => AttackIntervalPhaseTwo + Main.rand.Next(-10, 11),
				_ => AttackIntervalPhaseOne + Main.rand.Next(-14, 15)
			};
		}

		internal static IVaultState<AshHeartContext> ChooseNextState(AshHeartContext context)
		{
			int phase = (int)context.Npc.ai[0];
			int cycle = (int)context.Npc.ai[2];
			context.Npc.ai[2] += 1f;

			if (phase >= 2) {
				return (cycle % 3) switch {
					0 => new AshHeartPulseState(),
					1 => new AshHeartRainState(),
					_ => new AshHeartVolleyState()
				};
			}

			if (phase == 1) {
				return (cycle % 4) switch {
					0 => new AshHeartRainState(),
					1 => new AshHeartVolleyState(),
					2 => new AshHeartPoolState(),
					_ => new AshHeartPulseState()
				};
			}

			return cycle % 2 == 0 ? new AshHeartVolleyState() : new AshHeartPoolState();
		}

		internal static void OnPhaseStart(NPC npc, int phase)
		{
			npc.ai[0] = phase;
			npc.damage = ContactDamage;
			npc.defDamage = ContactDamage;
			npc.velocity *= 0.25f;
			npc.netUpdate = true;

			if (Main.dedServ) {
				return;
			}

			SoundEngine.PlaySound(SoundID.Roar, npc.Center);
			WastelandSpark.Burst(npc.Center, phase == 2 ? 28 : 18, 1, 4.2f);

			if (phase == 1) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.AshHeartPhaseTwo"), new Color(255, 140, 60));
			}
			else if (phase == 2) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.AshHeartPhaseThree"), new Color(255, 90, 40));
			}
		}

		internal static void ThrowOrbs(AshHeartContext context, int count, float speed)
		{
			if (context.Target == null || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			Vector2 aim = context.Target.Center - context.Npc.Center;

			if (aim.LengthSquared() < 1f) {
				aim = Vector2.UnitY;
			}

			float step = 0.22f;
			float start = -step * (count - 1) * 0.5f;

			for (int i = 0; i < count; i++) {
				Vector2 velocity = Vector2.Normalize(aim).RotatedBy(start + step * i) * speed;

				Projectile.NewProjectile(
					context.Npc.GetSource_FromAI(),
					context.Npc.Center,
					velocity,
					ModContent.ProjectileType<AshEmberOrb>(),
					OrbDamage,
					1f,
					Main.myPlayer);
			}
		}

		internal static void DropPool(AshHeartContext context)
		{
			if (context.Target == null || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			Vector2 spot = context.Target.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), 8f);

			Projectile.NewProjectile(
				context.Npc.GetSource_FromAI(),
				spot,
				Vector2.Zero,
				ModContent.ProjectileType<AshPool>(),
				PoolDamage,
				0f,
				Main.myPlayer);
		}

		/// <summary>从玩家头顶落下灰雨，随机留两道不相邻的空档。</summary>
		internal static void SpawnRain(AshHeartContext context)
		{
			if (context.Target == null || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			int columns = (int)context.Npc.ai[0] >= 2 ? 11 : 9;
			float spacing = 70f;
			float left = context.Target.Center.X - spacing * (columns - 1) * 0.5f;
			int gapA = Main.rand.Next(columns);
			int gapB = (gapA + columns / 2) % columns;

			for (int i = 0; i < columns; i++) {
				if (i == gapA || i == gapB) {
					continue;
				}

				Vector2 position = new Vector2(left + spacing * i, context.Target.Center.Y - 520f);

				Projectile.NewProjectile(
					context.Npc.GetSource_FromAI(),
					position,
					new Vector2(0f, 7.2f),
					ModContent.ProjectileType<AshCinder>(),
					RainDamage,
					1f,
					Main.myPlayer);
			}
		}

		internal static void SpawnPulse(AshHeartContext context)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			Projectile.NewProjectile(
				context.Npc.GetSource_FromAI(),
				context.Npc.Center,
				Vector2.Zero,
				ModContent.ProjectileType<AshPulse>(),
				PulseDamage,
				0f,
				Main.myPlayer,
				context.Npc.whoAmI);
		}

		internal static void SpawnWisps(AshHeartContext context, int count)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			int type = ModContent.NPCType<AshWisp>();
			int alive = 0;

			for (int i = 0; i < Main.maxNPCs; i++) {
				if (Main.npc[i].active && Main.npc[i].type == type && (int)Main.npc[i].ai[0] == context.Npc.whoAmI) {
					alive++;
				}
			}

			for (int i = alive; i < count; i++) {
				int index = NPC.NewNPC(
					new EntitySource_SpawnNPC(),
					(int)context.Npc.Center.X,
					(int)context.Npc.Center.Y,
					type,
					0,
					context.Npc.whoAmI,
					i);

				if (index >= 0 && index < Main.maxNPCs) {
					Main.npc[index].netUpdate = true;
				}
			}
		}

		public override void FindFrame(int frameHeight)
		{
			NPC.frameCounter += 1.0;

			if (NPC.frameCounter >= 8.0) {
				NPC.frameCounter = 0.0;
				NPC.frame.Y += frameHeight;

				if (NPC.frame.Y >= Main.npcFrameCount[Type] * frameHeight) {
					NPC.frame.Y = 0;
				}
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AshHeartBag>(), 1));
			// 10% 奖杯（与原版 Boss 掉落奖杯的做法一致）
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AshHeartTrophy>(), 10));
		}

		public override void OnKill()
		{
			AshHeartContext.Release(NPC.whoAmI);
			WastelandStorySystem.MarkAshHeartDefeated();
		}

		public override bool CheckActive()
		{
			return false;
		}

		public override void BossHeadRotation(ref float rotation)
		{
			rotation = NPC.rotation;
		}
	}
}
