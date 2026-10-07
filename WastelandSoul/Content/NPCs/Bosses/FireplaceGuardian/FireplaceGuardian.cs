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
using WastelandSoul.Content.Projectiles.FireplaceBoss;
using WastelandSoul.Content.Projectiles.Vfx;

namespace WastelandSoul.Content.NPCs.Bosses.FireplaceGuardian
{
	/// <summary>
	/// Boss 4：壁炉守卫。壁炉重置装置的最后一道锁，月亮领主之前。
	/// <para/>冷白金属。招式留缝：螺栓波、炉心冲撞、扩散环、向内合拢但停住的火墙。
	/// </summary>
	[AutoloadBossHead]
	public class FireplaceGuardian : ModNPC
	{
		public const float PhaseTwoThreshold = 0.62f;
		public const float PhaseThreeThreshold = 0.32f;

		internal const int AttackIntervalPhaseOne = 140;
		internal const int AttackIntervalPhaseTwo = 108;
		internal const int AttackIntervalPhaseThree = 78;

		internal const int BoltWindup = 40;
		internal const int SlamWindup = 34;
		internal const int SlamTicks = 30;
		internal const int RingWindup = 36;
		internal const int WallWindup = 44;

		internal const int ContactDamage = 90;
		internal const int SlamDamage = 128;
		internal const int BoltDamage = 52;
		internal const int RingDamage = 46;
		internal const int WallDamage = 64;

		internal const float HoverDefault = 200f;
		internal const float HoverHigh = 250f;
		internal const float SlamSpeed = 15f;
		internal const float MaxTurnPerTick = 5f;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 6;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			Music = MusicLoader.GetMusicSlot(Mod, "Music/FireplaceGuardian");
		}

		public override void SetDefaults()
		{
			NPC.width = 110;
			NPC.height = 110;
			NPC.damage = ContactDamage;
			NPC.defDamage = ContactDamage;
			NPC.defense = 48;
			NPC.lifeMax = 78000;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.boss = true;
			NPC.aiStyle = -1;
			NPC.npcSlots = 20f;
			NPC.value = Item.buyPrice(gold: 40);
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
		}

		public override void AI()
		{
			FireplaceGuardianContext context = FireplaceGuardianContext.For(NPC);
			context.Target = FindTarget(NPC);

			if (context.Target == null) {
				FireplaceGuardianContext.Release(NPC.whoAmI);
				NPC.velocity.Y -= 0.16f;
				NPC.EncourageDespawn(30);
				return;
			}

			NPC.target = context.Target.whoAmI;
			context.Machine.Update();
			NPC.spriteDirection = context.Target.Center.X > NPC.Center.X ? 1 : -1;
			NPC.rotation = MathHelper.Clamp(NPC.velocity.X * 0.008f, -0.16f, 0.16f);

			if (!Main.dedServ) {
				Lighting.AddLight(NPC.Center, 0.55f, 0.75f, 1.05f);
			}
		}

		internal static Player FindTarget(NPC npc)
		{
			Player best = null;
			float bestDistance = 4600f;

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

		internal static void Hover(FireplaceGuardianContext context, float distance)
		{
			if (context.Target == null) {
				return;
			}

			NPC npc = context.Npc;
			Vector2 hoverPoint = context.Target.Center + new Vector2(0f, -distance);
			Vector2 offset = hoverPoint - npc.Center;
			float speed = offset.Length() > 500f ? 11f : 6.5f;

			if (offset.LengthSquared() > 36f) {
				npc.velocity = (npc.velocity * 11f + Vector2.Normalize(offset) * speed) / 12f;
			}
			else {
				npc.velocity *= 0.9f;
			}
		}

		internal static void DashTowards(FireplaceGuardianContext context)
		{
			if (context.Target == null) {
				return;
			}

			Vector2 direction = context.Target.Center - context.Npc.Center;

			if (direction.LengthSquared() < 1f) {
				direction = Vector2.UnitY;
			}

			context.Npc.velocity = Vector2.Normalize(direction) * SlamSpeed;
		}

		internal static void SteerSlam(FireplaceGuardianContext context)
		{
			if (context.Target == null) {
				return;
			}

			NPC npc = context.Npc;
			float speed = npc.velocity.Length();

			if (speed < 1f) {
				speed = SlamSpeed;
			}

			float current = npc.velocity.ToRotation();
			Vector2 toTarget = context.Target.Center - npc.Center;
			float wanted = toTarget.LengthSquared() > 1f ? toTarget.ToRotation() : current;
			float delta = MathHelper.WrapAngle(wanted - current);
			float maxTurn = MathHelper.ToRadians(MaxTurnPerTick);
			npc.velocity = (current + MathHelper.Clamp(delta, -maxTurn, maxTurn)).ToRotationVector2() * speed;
		}

		internal static int RollAttackDelay(FireplaceGuardianContext context)
		{
			return (int)context.Npc.ai[0] switch {
				>= 2 => AttackIntervalPhaseThree + Main.rand.Next(-6, 7),
				1 => AttackIntervalPhaseTwo + Main.rand.Next(-8, 9),
				_ => AttackIntervalPhaseOne + Main.rand.Next(-12, 13)
			};
		}

		internal static IVaultState<FireplaceGuardianContext> ChooseNextState(FireplaceGuardianContext context)
		{
			int phase = (int)context.Npc.ai[0];
			int cycle = (int)context.Npc.ai[2];
			context.Npc.ai[2] += 1f;

			if (phase >= 2) {
				return (cycle % 4) switch {
					0 => new FireplaceGuardianWallState(),
					1 => new FireplaceGuardianSlamState(),
					2 => new FireplaceGuardianRingState(),
					_ => new FireplaceGuardianBoltState()
				};
			}

			if (phase == 1) {
				return (cycle % 3) switch {
					0 => new FireplaceGuardianRingState(),
					1 => new FireplaceGuardianBoltState(),
					_ => new FireplaceGuardianSlamState()
				};
			}

			return cycle % 2 == 0 ? new FireplaceGuardianBoltState() : new FireplaceGuardianSlamState();
		}

		internal static void OnPhaseStart(NPC npc, int phase)
		{
			npc.ai[0] = phase;
			npc.damage = ContactDamage;
			npc.defDamage = ContactDamage;
			npc.velocity *= 0.2f;
			npc.netUpdate = true;

			if (Main.dedServ) {
				return;
			}

			SoundEngine.PlaySound(SoundID.Roar, npc.Center);
			WastelandSpark.Burst(npc.Center, 22, 2, 3.6f);

			if (phase == 1) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.FireplaceGuardianPhaseTwo"), new Color(180, 220, 255));
			}
			else if (phase == 2) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.FireplaceGuardianPhaseThree"), new Color(140, 190, 255));
			}
		}

		internal static void SpawnBolts(FireplaceGuardianContext context)
		{
			if (context.Target == null || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			int rows = (int)context.Npc.ai[0] >= 2 ? 9 : 7;
			float spacing = 72f;
			float side = context.Target.Center.X >= context.Npc.Center.X ? -1f : 1f;
			float spawnX = context.Target.Center.X + side * 780f;
			float startY = context.Target.Center.Y - spacing * (rows - 1) * 0.5f;
			int gap = Main.rand.Next(rows);

			for (int i = 0; i < rows; i++) {
				if (i == gap || i == (gap + 3) % rows) {
					continue;
				}

				Projectile.NewProjectile(
					context.Npc.GetSource_FromAI(),
					new Vector2(spawnX, startY + spacing * i),
					new Vector2(-side * 8.4f, 0f),
					ModContent.ProjectileType<HearthBolt>(),
					BoltDamage,
					1f,
					Main.myPlayer);
			}
		}

		internal static void SpawnRing(FireplaceGuardianContext context)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			int count = (int)context.Npc.ai[0] >= 2 ? 14 : 10;
			float gap = Main.rand.Next(count);

			for (int i = 0; i < count; i++) {
				if (i == gap || i == (gap + 1) % count) {
					continue;
				}

				Vector2 velocity = (MathHelper.TwoPi * i / count).ToRotationVector2() * 6.4f;

				Projectile.NewProjectile(
					context.Npc.GetSource_FromAI(),
					context.Npc.Center,
					velocity,
					ModContent.ProjectileType<HearthRingShard>(),
					RingDamage,
					1f,
					Main.myPlayer);
			}
		}

		/// <summary>左右两道火墙向玩家合拢，在中间停下，留下一条能站的缝。</summary>
		internal static void SpawnWalls(FireplaceGuardianContext context)
		{
			if (context.Target == null || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			float centerX = context.Target.Center.X;
			float top = context.Target.Center.Y - 220f;

			for (int i = 0; i < 8; i++) {
				float y = top + i * 56f;

				Projectile.NewProjectile(
					context.Npc.GetSource_FromAI(),
					new Vector2(centerX - 420f, y),
					new Vector2(3.2f, 0f),
					ModContent.ProjectileType<HearthWall>(),
					WallDamage,
					0f,
					Main.myPlayer,
					centerX - 70f);

				Projectile.NewProjectile(
					context.Npc.GetSource_FromAI(),
					new Vector2(centerX + 420f, y),
					new Vector2(-3.2f, 0f),
					ModContent.ProjectileType<HearthWall>(),
					WallDamage,
					0f,
					Main.myPlayer,
					centerX + 70f);
			}
		}

		public override void FindFrame(int frameHeight)
		{
			NPC.frameCounter += 1.0;

			if (NPC.frameCounter >= 7.0) {
				NPC.frameCounter = 0.0;
				NPC.frame.Y += frameHeight;

				if (NPC.frame.Y >= Main.npcFrameCount[Type] * frameHeight) {
					NPC.frame.Y = 0;
				}
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FireplaceBag>(), 1));
		}

		public override void OnKill()
		{
			FireplaceGuardianContext.Release(NPC.whoAmI);
			WastelandStorySystem.MarkFireplaceGuardianDefeated();
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
