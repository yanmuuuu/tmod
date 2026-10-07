using System.Collections.Generic;

namespace WastelandSoul.Common
{
	/// <summary>
	/// 非内容类本地化键的集中登记表。
	/// <para/>内容键（Items / NPCs / Projectiles / Tiles / Buffs 下的 DisplayName、Tooltip 等）
	/// 由 tModLoader 自动注册；但 <c>Dialogue.*</c>、<c>Messages.*</c> 这类自定义键必须显式注册，
	/// 否则 <see cref="Terraria.Localization.Language.GetTextValue"/> 只会返回键名本身。
	/// </summary>
	public static class WastelandText
	{
		public const string DialoguePrefix = "Dialogue.MechanicalCompanion.";

		public static readonly IReadOnlyList<string> CustomKeys = new List<string> {
			// ---- 提示 ----
			"Messages.BossNotImplemented",
			"Messages.FireplaceOpened",
			"Messages.TerminalRead",
			"Messages.FirstMemoryRestored",
			"Messages.ScavengerPhaseTwo",
			"Messages.ScavengerPhaseThree",
			"Messages.ScavengerOverload",
			"Messages.ScavengerSelfDestruct",
			"Messages.ScavengerWarning",
			"Messages.ScavengerArrived",
			"Messages.ScavengerSummoned",
			"Messages.ScavengerAlreadyActive",
			"Messages.ScavengerWrecked",
			"Messages.CoreReceived",
			"Messages.FrameNeedsCore",
			"Messages.CompanionAwakened",
			"Messages.CompanionAlreadyHere",

			// ---- Boss 2 归档者 ----
			"Messages.ArchivistPhaseTwo",
			"Messages.ArchivistPhaseThree",
			"Messages.ArchivistSuppression",
			"Messages.ArchivistDefeated",
			"Messages.SecondMemoryRestored",
			"Messages.ArchivistSealed",

			"Messages.AshHeartPhaseTwo",
			"Messages.AshHeartPhaseThree",
			"Messages.AshHeartDefeated",
			"Messages.ThirdMemoryRestored",
			"Messages.FireplaceGuardianPhaseTwo",
			"Messages.FireplaceGuardianPhaseThree",
			"Messages.FireplaceGuardianDefeated",
			"Messages.FourthMemoryRestored",
			"Messages.TrueName",
			"Messages.EndingVessel",
			"Messages.EndingRefuse",
			"Messages.CodaVessel",
			"Messages.CodaRefuse",
			"Messages.EnteredFireplace",
			"Messages.TerminalTalkToHer",
			"Messages.GatePlaced",
			"Messages.FireplaceEnterFailed",

			"Conditions.AfterScavenger",
			"Conditions.AfterArchivist",
			"Conditions.AfterAshHeart",

			"Tiles.FireplaceGate.MapEntry",
			"Tiles.FireplaceTerminal.MapEntry",
			"Tiles.FireplaceExit.MapEntry",

			DialoguePrefix + "ButtonTrade",
			DialoguePrefix + "ButtonVessel",
			DialoguePrefix + "ButtonRefuse",

			DialoguePrefix + "AfterAsh1",
			DialoguePrefix + "AfterAsh2",
			DialoguePrefix + "AfterThird1",
			DialoguePrefix + "AfterThird2",
			DialoguePrefix + "AfterFourth1",
			DialoguePrefix + "AfterFourth2",
			DialoguePrefix + "EndingVessel1",
			DialoguePrefix + "EndingVessel2",
			DialoguePrefix + "EndingRefuse1",
			DialoguePrefix + "EndingRefuse2",

			DialoguePrefix + "StoryHuntAsh",
			DialoguePrefix + "StoryBringThird",
			DialoguePrefix + "StoryHuntGuardian",
			DialoguePrefix + "StoryBringFourth",
			DialoguePrefix + "StoryChoice",
			DialoguePrefix + "StoryEndingVessel",
			DialoguePrefix + "StoryEndingRefuse",

			DialoguePrefix + "MemoryThird",
			DialoguePrefix + "MemoryThirdNeedFragment",
			DialoguePrefix + "MemoryFourth",
			DialoguePrefix + "MemoryFourthNeedFragment",
			DialoguePrefix + "ChoiceVessel",
			DialoguePrefix + "ChoiceRefuse",

			DialoguePrefix + "ButtonStory",
			DialoguePrefix + "ButtonMemory",

			DialoguePrefix + "Intro1",
			DialoguePrefix + "Intro2",
			DialoguePrefix + "Intro3",
			DialoguePrefix + "Intro4",

			DialoguePrefix + "AfterScavenger1",
			DialoguePrefix + "AfterScavenger2",
			DialoguePrefix + "AfterScavenger3",

			DialoguePrefix + "AfterArchivist1",
			DialoguePrefix + "AfterArchivist2",

			DialoguePrefix + "AfterMemory1",
			DialoguePrefix + "AfterMemory2",
			DialoguePrefix + "AfterMemory3",

			DialoguePrefix + "AfterSecondMemory1",
			DialoguePrefix + "AfterSecondMemory2",
			DialoguePrefix + "AfterSecondMemory3",

			DialoguePrefix + "StoryHunt",
			DialoguePrefix + "StoryFireplace",
			DialoguePrefix + "StoryTerminalRead",
			DialoguePrefix + "StoryAfterMemory",
			DialoguePrefix + "StoryAwaitSecond",
			DialoguePrefix + "StoryAfterSecondMemory",

			DialoguePrefix + "MemoryBlank",
			DialoguePrefix + "MemoryFirst",
			DialoguePrefix + "MemorySecond",
			DialoguePrefix + "MemorySecondNeedFragment",
			DialoguePrefix + "MemoryRecovered2",
			DialoguePrefix + "MemoryUnstable",
			DialoguePrefix + "MemoryRecovered"
		};
	}
}
