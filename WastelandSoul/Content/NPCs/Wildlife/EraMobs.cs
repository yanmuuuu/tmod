using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Systems;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons.CLine;

namespace WastelandSoul.Content.NPCs.Wildlife
{
	/// <summary>
	/// 四个时期的野生敌人。子世界（很小的地图）里不刷新，避免壁炉大厅被怪填满。
	/// </summary>
	public abstract class EraMob : ModNPC
	{
		protected abstract int ChipMin { get; }

		protected abstract int ChipMax { get; }

		protected abstract int[] RareWeapons { get; }

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
		}

		public override void FindFrame(int frameHeight)
		{
			NPC.frameCounter += 1.0;

			if (NPC.frameCounter < 7.0) {
				return;
			}

			NPC.frameCounter = 0.0;
			NPC.frame.Y += frameHeight;

			if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[Type]) {
				NPC.frame.Y = 0;
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Chip>(), 1, ChipMin, ChipMax));

			if (RareWeapons.Length > 0) {
				npcLoot.Add(ItemDropRule.OneFromOptions(22, RareWeapons));
			}
		}

		protected static bool OutsideSubworld(NPCSpawnInfo spawnInfo)
		{
			return Main.maxTilesX >= 1200 && !spawnInfo.PlayerSafe && spawnInfo.Player.active;
		}
	}

	public class ScrapCrawler : EraMob
	{
		protected override int ChipMin => 1;
		protected override int ChipMax => 3;

		protected override int[] RareWeapons => new[] {
			ModContent.ItemType<ScavengerCWarrior>(),
			ModContent.ItemType<ScavengerCMage>(),
			ModContent.ItemType<ScavengerCRanger>(),
			ModContent.ItemType<ScavengerCSummoner>()
		};

		public override void SetDefaults()
		{
			NPC.width = 40;
			NPC.height = 30;
			NPC.damage = 16;
			NPC.defense = 4;
			NPC.lifeMax = 70;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = 40f;
			NPC.knockBackResist = 0.6f;
			NPC.aiStyle = 3;
			AIType = NPCID.Zombie;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!OutsideSubworld(spawnInfo) || Main.hardMode || !spawnInfo.Player.ZoneOverworldHeight || !Main.dayTime) {
				return 0f;
			}

			return 0.045f;
		}
	}

	public class IndexMoth : EraMob
	{
		protected override int ChipMin => 2;
		protected override int ChipMax => 6;

		protected override int[] RareWeapons => new[] {
			ModContent.ItemType<ArchivistCWarrior>(),
			ModContent.ItemType<ArchivistCMage>(),
			ModContent.ItemType<ArchivistCRanger>(),
			ModContent.ItemType<ArchivistCSummoner>()
		};

		public override void SetDefaults()
		{
			NPC.width = 36;
			NPC.height = 36;
			NPC.damage = 26;
			NPC.defense = 8;
			NPC.lifeMax = 140;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 80f;
			NPC.knockBackResist = 0.5f;
			NPC.noGravity = true;
			NPC.aiStyle = 2;
			AIType = NPCID.DemonEye;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!OutsideSubworld(spawnInfo) || !WastelandStorySystem.scavengerDefeated || !spawnInfo.Player.ZoneDirtLayerHeight) {
				return 0f;
			}

			return 0.05f;
		}
	}

	public class AshStalker : EraMob
	{
		protected override int ChipMin => 4;
		protected override int ChipMax => 10;

		protected override int[] RareWeapons => new[] {
			ModContent.ItemType<AshHeartCWarrior>(),
			ModContent.ItemType<AshHeartCMage>(),
			ModContent.ItemType<AshHeartCRanger>(),
			ModContent.ItemType<AshHeartCSummoner>()
		};

		public override void SetDefaults()
		{
			NPC.width = 48;
			NPC.height = 48;
			NPC.damage = 48;
			NPC.defense = 18;
			NPC.lifeMax = 380;
			NPC.HitSound = SoundID.NPCHit3;
			NPC.DeathSound = SoundID.NPCDeath6;
			NPC.value = 200f;
			NPC.knockBackResist = 0.35f;
			NPC.aiStyle = 3;
			AIType = NPCID.PossessedArmor;
			NPC.lavaImmune = true;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!OutsideSubworld(spawnInfo) || !Main.hardMode || !WastelandStorySystem.archivistDefeated) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneRockLayerHeight && !spawnInfo.Player.ZoneUnderworldHeight) {
				return 0f;
			}

			return 0.04f;
		}
	}

	public class HearthWarden : EraMob
	{
		protected override int ChipMin => 6;
		protected override int ChipMax => 12;

		protected override int[] RareWeapons => new[] {
			ModContent.ItemType<FireplaceCWarrior>(),
			ModContent.ItemType<FireplaceCMage>(),
			ModContent.ItemType<FireplaceCRanger>(),
			ModContent.ItemType<FireplaceCSummoner>()
		};

		public override void SetDefaults()
		{
			NPC.width = 44;
			NPC.height = 60;
			NPC.damage = 64;
			NPC.defense = 28;
			NPC.lifeMax = 720;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = 400f;
			NPC.knockBackResist = 0.2f;
			NPC.noGravity = true;
			NPC.aiStyle = 14;
			AIType = NPCID.CaveBat;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!OutsideSubworld(spawnInfo) || !Main.hardMode || Main.dayTime || !WastelandStorySystem.ashHeartDefeated) {
				return 0f;
			}

			if (!spawnInfo.Player.ZoneOverworldHeight) {
				return 0f;
			}

			return 0.035f;
		}

		public override void PostAI()
		{
			if (!Main.dedServ) {
				Lighting.AddLight(NPC.Center, 0.3f, 0.45f, 0.7f);
			}
		}
	}
}
