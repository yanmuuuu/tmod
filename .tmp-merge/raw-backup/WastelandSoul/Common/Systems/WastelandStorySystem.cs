using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 智械人的剧情阶段。对话按阶段分支。
	/// </summary>
	public enum CompanionStage
	{
		Intact,
		FirstMemory,
		SecondMemory,
		ThirdMemory,
		FourthMemory,
		Ending
	}

	/// <summary>
	/// 世界级剧情进度。标志位随世界存档保存，联机时用模组包同步。
	/// </summary>
	public class WastelandStorySystem : ModSystem
	{
		public const int EndingNone = 0;
		public const int EndingVessel = 1;
		public const int EndingRefuse = 2;

		public const string TrueName = "埃尔薇";

		public static bool scavengerDefeated;
		public static bool archivistDefeated;
		public static bool ashHeartDefeated;
		public static bool fireplaceGuardianDefeated;
		public static bool fireplaceOpened;
		public static bool dataTerminalRead;
		public static bool firstMemoryRestored;
		public static bool secondMemoryRestored;
		public static bool thirdMemoryRestored;
		public static bool fourthMemoryRestored;
		public static bool companionAwakened;
		public static bool codaPlayed;
		public static int endingChoice;
		public static string companionName = string.Empty;

		public static int GateX;
		public static int GateY;
		public static bool GatePlaced;

		public static CompanionStage CompanionMemoryStage
		{
			get
			{
				if (endingChoice != EndingNone) {
					return CompanionStage.Ending;
				}

				if (fourthMemoryRestored) {
					return CompanionStage.FourthMemory;
				}

				if (thirdMemoryRestored) {
					return CompanionStage.ThirdMemory;
				}

				if (secondMemoryRestored) {
					return CompanionStage.SecondMemory;
				}

				if (firstMemoryRestored) {
					return CompanionStage.FirstMemory;
				}

				return CompanionStage.Intact;
			}
		}

		public override void ClearWorld()
		{
			scavengerDefeated = false;
			archivistDefeated = false;
			ashHeartDefeated = false;
			fireplaceGuardianDefeated = false;
			fireplaceOpened = false;
			dataTerminalRead = false;
			firstMemoryRestored = false;
			secondMemoryRestored = false;
			thirdMemoryRestored = false;
			fourthMemoryRestored = false;
			companionAwakened = false;
			codaPlayed = false;
			endingChoice = EndingNone;
			companionName = string.Empty;
			GatePlaced = false;
			GateX = 0;
			GateY = 0;

			Content.NPCs.Bosses.Scavenger.ScavengerContext.Clear();
			Content.NPCs.Bosses.Archivist.ArchivistContext.Clear();
			Content.NPCs.Bosses.AshHeart.AshHeartContext.Clear();
			Content.NPCs.Bosses.FireplaceGuardian.FireplaceGuardianContext.Clear();
		}

		public override void SaveWorldData(TagCompound tag)
		{
			tag["scavenger"] = scavengerDefeated;
			tag["archivist"] = archivistDefeated;
			tag["ashHeart"] = ashHeartDefeated;
			tag["guardian"] = fireplaceGuardianDefeated;
			tag["fireplace"] = fireplaceOpened;
			tag["terminal"] = dataTerminalRead;
			tag["memory1"] = firstMemoryRestored;
			tag["memory2"] = secondMemoryRestored;
			tag["memory3"] = thirdMemoryRestored;
			tag["memory4"] = fourthMemoryRestored;
			tag["companion"] = companionAwakened;
			tag["coda"] = codaPlayed;
			tag["ending"] = endingChoice;
			tag["gatePlaced"] = GatePlaced;
			tag["gateX"] = GateX;
			tag["gateY"] = GateY;

			if (!string.IsNullOrEmpty(companionName)) {
				tag["companionName"] = companionName;
			}
		}

		public override void LoadWorldData(TagCompound tag)
		{
			scavengerDefeated = tag.GetBool("scavenger");
			archivistDefeated = tag.GetBool("archivist");
			ashHeartDefeated = tag.GetBool("ashHeart");
			fireplaceGuardianDefeated = tag.GetBool("guardian");
			fireplaceOpened = tag.GetBool("fireplace");
			dataTerminalRead = tag.GetBool("terminal");
			firstMemoryRestored = tag.GetBool("memory1");
			secondMemoryRestored = tag.GetBool("memory2");
			thirdMemoryRestored = tag.GetBool("memory3");
			fourthMemoryRestored = tag.GetBool("memory4");
			companionAwakened = tag.GetBool("companion");
			codaPlayed = tag.GetBool("coda");
			endingChoice = tag.GetInt("ending");
			GatePlaced = tag.GetBool("gatePlaced");
			GateX = tag.GetInt("gateX");
			GateY = tag.GetInt("gateY");
			companionName = tag.GetString("companionName") ?? string.Empty;
		}

		public override void NetSend(BinaryWriter writer)
		{
			WriteStory(writer);
		}

		public override void NetReceive(BinaryReader reader)
		{
			ReadStory(reader);
		}

		public override void PostUpdateWorld()
		{
			if (codaPlayed || endingChoice == EndingNone || !NPC.downedMoonlord) {
				return;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			codaPlayed = true;
			string key = endingChoice == EndingVessel
				? "Mods.WastelandSoul.Messages.CodaVessel"
				: "Mods.WastelandSoul.Messages.CodaRefuse";
			Announce(key, new Color(230, 220, 180));
			Sync();
		}

		public static void WriteStory(BinaryWriter writer)
		{
			writer.Write(scavengerDefeated);
			writer.Write(archivistDefeated);
			writer.Write(ashHeartDefeated);
			writer.Write(fireplaceGuardianDefeated);
			writer.Write(fireplaceOpened);
			writer.Write(dataTerminalRead);
			writer.Write(firstMemoryRestored);
			writer.Write(secondMemoryRestored);
			writer.Write(thirdMemoryRestored);
			writer.Write(fourthMemoryRestored);
			writer.Write(companionAwakened);
			writer.Write(codaPlayed);
			writer.Write(endingChoice);
			writer.Write(GatePlaced);
			writer.Write(GateX);
			writer.Write(GateY);
			writer.Write(companionName ?? string.Empty);
		}

		public static void ReadStory(BinaryReader reader)
		{
			scavengerDefeated = reader.ReadBoolean();
			archivistDefeated = reader.ReadBoolean();
			ashHeartDefeated = reader.ReadBoolean();
			fireplaceGuardianDefeated = reader.ReadBoolean();
			fireplaceOpened = reader.ReadBoolean();
			dataTerminalRead = reader.ReadBoolean();
			firstMemoryRestored = reader.ReadBoolean();
			secondMemoryRestored = reader.ReadBoolean();
			thirdMemoryRestored = reader.ReadBoolean();
			fourthMemoryRestored = reader.ReadBoolean();
			companionAwakened = reader.ReadBoolean();
			codaPlayed = reader.ReadBoolean();
			endingChoice = reader.ReadInt32();
			GatePlaced = reader.ReadBoolean();
			GateX = reader.ReadInt32();
			GateY = reader.ReadInt32();
			companionName = reader.ReadString();
		}

		/// <summary>把当前剧情标志发给另一端。单人模式什么都不做。</summary>
		public static void Sync()
		{
			if (Main.netMode == NetmodeID.SinglePlayer) {
				return;
			}

			ModPacket packet = ModContent.GetInstance<WastelandSoul.WastelandSoul>().GetPacket();
			packet.Write((byte)1);
			WriteStory(packet);
			packet.Send();
		}

		public static void ReceivePacket(BinaryReader reader, int whoAmI)
		{
			byte kind = reader.ReadByte();

			if (kind != 1) {
				return;
			}

			ReadStory(reader);

			if (Main.netMode == NetmodeID.Server) {
				Sync();
			}
		}

		public static void MarkScavengerDefeated()
		{
			if (scavengerDefeated) {
				return;
			}

			scavengerDefeated = true;
			fireplaceOpened = true;
			Announce("Mods.WastelandSoul.Messages.FireplaceOpened", new Color(226, 122, 74));
			Sync();
		}

		public static void MarkArchivistDefeated()
		{
			if (archivistDefeated) {
				return;
			}

			archivistDefeated = true;
			Announce("Mods.WastelandSoul.Messages.ArchivistDefeated", new Color(176, 190, 224));
			Sync();
		}

		public static void MarkAshHeartDefeated()
		{
			if (ashHeartDefeated) {
				return;
			}

			ashHeartDefeated = true;
			Announce("Mods.WastelandSoul.Messages.AshHeartDefeated", new Color(255, 140, 70));
			Sync();
		}

		public static void MarkFireplaceGuardianDefeated()
		{
			if (fireplaceGuardianDefeated) {
				return;
			}

			fireplaceGuardianDefeated = true;
			Announce("Mods.WastelandSoul.Messages.FireplaceGuardianDefeated", new Color(170, 210, 255));
			Sync();
		}

		public static void MarkDataTerminalRead()
		{
			if (dataTerminalRead) {
				return;
			}

			dataTerminalRead = true;
			Announce("Mods.WastelandSoul.Messages.TerminalRead", new Color(150, 200, 230));
			Sync();
		}

		public static void MarkCompanionArrived(string name)
		{
			companionAwakened = true;

			if (!string.IsNullOrEmpty(name)) {
				companionName = name;
			}

			Sync();
		}

		public static void RestoreFirstMemory()
		{
			if (firstMemoryRestored) {
				return;
			}

			firstMemoryRestored = true;
			Announce("Mods.WastelandSoul.Messages.FirstMemoryRestored", new Color(180, 200, 255));
			Sync();
		}

		public static void RestoreSecondMemory()
		{
			if (secondMemoryRestored) {
				return;
			}

			secondMemoryRestored = true;
			Announce("Mods.WastelandSoul.Messages.SecondMemoryRestored", new Color(160, 190, 240));
			Sync();
		}

		public static void RestoreThirdMemory()
		{
			if (thirdMemoryRestored) {
				return;
			}

			thirdMemoryRestored = true;
			Announce("Mods.WastelandSoul.Messages.ThirdMemoryRestored", new Color(255, 160, 90));
			Sync();
		}

		public static void RestoreFourthMemory()
		{
			if (fourthMemoryRestored) {
				return;
			}

			fourthMemoryRestored = true;
			companionName = TrueName;
			RenameCompanion();
			Announce("Mods.WastelandSoul.Messages.FourthMemoryRestored", new Color(210, 220, 255));
			Announce("Mods.WastelandSoul.Messages.TrueName", new Color(240, 220, 160));
			Sync();
		}

		public static void ChooseEnding(Player player, int choice)
		{
			if (endingChoice != EndingNone || !fourthMemoryRestored) {
				return;
			}

			endingChoice = choice == EndingRefuse ? EndingRefuse : EndingVessel;
			string key = endingChoice == EndingVessel
				? "Mods.WastelandSoul.Messages.EndingVessel"
				: "Mods.WastelandSoul.Messages.EndingRefuse";
			Announce(key, new Color(230, 210, 160));

			if (player != null) {
				int relic = endingChoice == EndingVessel
					? ModContent.ItemType<Content.Items.Story.DawnSeal>()
					: ModContent.ItemType<Content.Items.Story.UnburnedName>();
				player.QuickSpawnItem(player.GetSource_GiftOrReward(), relic);
			}

			Sync();
		}

		private static void RenameCompanion()
		{
			int type = ModContent.NPCType<Content.NPCs.Town.MechanicalCompanion>();

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (npc.active && npc.type == type) {
					npc.GivenName = TrueName;
				}
			}
		}

		private static void Announce(string key, Color color)
		{
			if (Main.netMode == NetmodeID.Server) {
				Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key), color);
				return;
			}

			Main.NewText(Language.GetTextValue(key), color);
		}
	}
}
