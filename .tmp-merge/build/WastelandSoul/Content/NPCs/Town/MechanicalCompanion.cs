using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Common.Players;
using WastelandSoul.Common.Systems;

namespace WastelandSoul.Content.NPCs.Town
{
	/// <summary>
	/// 智械人：主人公魂穿时携带的智械核心，在旧世界精灵族遗迹中获得身体。
	/// <para/>外观：精灵（金发、长耳、蓝瞳），棕红色金边长袍。定位：城镇 NPC，住在屋子里，不跟随玩家。
	/// <para/>剧情作用：对话触发、任务引导、记忆恢复、真相揭示。
	/// </summary>
	[AutoloadHead]
	public class MechanicalCompanion : ModNPC
	{
		/// <summary>对话本地化键前缀。所有对话文本都在 Localization/*_Mods.WastelandSoul.hjson 的 Dialogue 节点下。</summary>
		private const string DialogueKey = "Mods.WastelandSoul.Dialogue.MechanicalCompanion.";

		public override void SetStaticDefaults()
		{
			// 城镇 NPC 标准帧参数，与原版向导一致：
			// 25 帧 = 16 帧行走/待机 + 9 帧特殊动作（其中 4 帧为攻击帧）
			Main.npcFrameCount[Type] = 25;
			NPCID.Sets.ExtraFramesCount[Type] = 9;
			NPCID.Sets.AttackFrameCount[Type] = 4;

			// 遇敌时会主动攻击范围内的敌人（智械核心的自卫协议）
			NPCID.Sets.DangerDetectRange[Type] = 700;
			NPCID.Sets.AttackType[Type] = 2; // 2 = 投掷/远程，配合 TownNPCAttackProj
			NPCID.Sets.AttackTime[Type] = 90;
			NPCID.Sets.AttackAverageChance[Type] = 20;

			// 派对帽的 Y 偏移
			NPCID.Sets.HatOffsetY[Type] = 4;
		}

		public override void SetDefaults()
		{
			NPC.townNPC = true;
			NPC.friendly = true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = 7; // 城镇 NPC 被动 AI
			NPC.damage = 10;
			NPC.defense = 15;
			NPC.lifeMax = 250;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.5f;

			// 复制原版向导的帧逻辑，这样精灵表布局可以直接沿用原版约定（25 帧纵向排列）
			AnimationType = NPCID.Guide;

			// 生物群落与邻居偏好（幸福度）
			NPC.Happiness
				.SetBiomeAffection<ForestBiome>(AffectionLevel.Like)      // 喜欢森林
				.SetBiomeAffection<HallowBiome>(AffectionLevel.Like)      // 精灵族遗迹的残留气息
				.SetBiomeAffection<DesertBiome>(AffectionLevel.Dislike)   // 废土化的沙漠
				.SetBiomeAffection<CorruptionBiome>(AffectionLevel.Hate)  // 污染
				.SetNPCAffection(NPCID.Guide, AffectionLevel.Like)
				.SetNPCAffection(NPCID.Steampunker, AffectionLevel.Love)  // 同为旧时代机械
				.SetNPCAffection(NPCID.Cyborg, AffectionLevel.Love)       // 同源技术
				.SetNPCAffection(NPCID.GoblinTinkerer, AffectionLevel.Dislike)
				.SetNPCAffection(NPCID.TaxCollector, AffectionLevel.Hate);
		}

		/// <summary>
		/// 序列名是「零号」。第四段记忆恢复后，她会记起这具精灵躯体原来的名字「埃尔薇」。
		/// </summary>
		public static readonly string[] CandidateNames = {
			"零号"
		};

		public override List<string> SetNPCNameList()
		{
			return new List<string>(CandidateNames);
		}

		/// <summary>随机抽一个候选名字（到达时用；名字还没定，之后换掉这个列表即可）。</summary>
		public static string PickName()
		{
			return CandidateNames[Main.rand.Next(CandidateNames.Length)];
		}

		/// <summary>
		/// 「到达」：把她送到指定位置（默认玩家身边）。消耗不消耗智械核心由调用方决定。
		/// <para/>两条入口共用：用「智械核心」直接召唤（主路径）、在遗迹躯体上右键（保底路径）。
		/// <para/>返回 false 表示没能召唤（她已经在了），调用方**不要**消耗核心。
		/// </summary>
		public static bool TrySummon(Player player, Vector2? position = null, IEntitySource source = null)
		{
			int type = ModContent.NPCType<MechanicalCompanion>();

			if (WastelandStorySystem.companionAwakened || NPC.AnyNPCs(type)) {
				if (!Main.dedServ && player.whoAmI == Main.myPlayer) {
					Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.CompanionAlreadyHere"),
						new Color(180, 200, 255));
				}

				return false;
			}

			string name = string.IsNullOrEmpty(WastelandStorySystem.companionName)
				? PickName()
				: WastelandStorySystem.companionName;

			WastelandStorySystem.MarkCompanionArrived(name);

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				Vector2 spot = position ?? new Vector2(player.Center.X, player.Center.Y - 48);

				int index = NPC.NewNPC(source ?? new EntitySource_SpawnNPC(),
					(int)spot.X, (int)spot.Y, type);

				if (index >= 0 && index < Main.maxNPCs) {
					Main.npc[index].GivenName = name;
					Main.npc[index].netUpdate = true;
				}
			}

			if (!Main.dedServ) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.CompanionArrived", name),
					new Color(226, 200, 120));
			}

			return true;
		}

		// ==================== 偶尔的小动作（低头 / 抬头） ====================

		// 槽位说明：城镇 NPC 的 ai[0..3] 被原版 AI 占用，所以状态一律放 localAI。
		//   localAI[0] = 距下次小动作还有多少帧（只在「站定」时才递减）
		//   localAI[1] = 当前动作剩余帧数（0 = 没有动作）
		//   localAI[2] = 动作类型：1 = 低头，2 = 抬头
		private const float IdleActionMinGap = 60f * 120f;   // 最少 2 分钟
		private const float IdleActionMaxGap = 60f * 180f;   // 最多 3 分钟
		private const float IdleActionHold = 36f;            // 动作持续帧数

		public override void AI()
		{
			NPC npc = NPC;

			if (npc.localAI[0] <= 0f) {
				npc.localAI[0] = Main.rand.Next((int)IdleActionMinGap, (int)IdleActionMaxGap);
			}

			if (npc.localAI[1] > 0f) {
				npc.localAI[1] -= 1f;
				return;
			}

			// 只有站定时才倒数（走动时做低头动作很怪）
			bool standing = npc.velocity.X == 0f && npc.velocity.Y == 0f;

			if (!standing) {
				return;
			}

			npc.localAI[0] -= 1f;

			if (npc.localAI[0] <= 0f) {
				npc.localAI[2] = Main.rand.NextBool() ? 1f : 2f;   // 低头 / 抬头
				npc.localAI[1] = IdleActionHold;
			}
		}

		/// <summary>
		/// 小动作期间强制用帧表中的低头 / 抬头帧（4-7 那一段）。
		/// <para/>帧表布局：0-3 / 8-11 / 12-15 = 左右朝向行走（正背面按你的要求不再需要，
		/// 所以原版"朝上"那一排 4-7 被用来放低头/抬头），16-20 站立，21-24 使用。
		/// </summary>
		public override void FindFrame(int frameHeight)
		{
			if (NPC.localAI[1] > 0f) {
				int frame = NPC.localAI[2] == 1f ? 5 : 7;   // 5 = 低头，7 = 抬头
				NPC.frame.Y = frame * frameHeight;
				NPC.frameCounter = 0.0;
				return;
			}

			// 其余情况交给原版城镇 NPC 的帧逻辑（AnimationType = Guide）
		}

		/// <summary>
		/// 她作为全程引导者，前期就应该能入住（不设 Boss 前置）。
		/// </summary>
		public override bool CanTownNPCSpawn(int numTownNPCs)
		{
			return true;
		}

		// ==================== 对话（按剧情阶段分支） ====================

		/// <summary>
		/// 左键点 NPC 时的默认闲聊。按 WastelandStorySystem 的剧情标志位返回不同内容。
		/// </summary>
		public override string GetChat()
		{
			WastelandPlayer modPlayer = Main.LocalPlayer.GetModPlayer<WastelandPlayer>();
			modPlayer.metCompanion = true;

			if (WastelandStorySystem.endingChoice == WastelandStorySystem.EndingVessel) {
				return Language.GetTextValue(DialogueKey + "EndingVessel" + Main.rand.Next(1, 3));
			}

			if (WastelandStorySystem.endingChoice == WastelandStorySystem.EndingRefuse) {
				return Language.GetTextValue(DialogueKey + "EndingRefuse" + Main.rand.Next(1, 3));
			}

			if (WastelandStorySystem.fourthMemoryRestored) {
				return Language.GetTextValue(DialogueKey + "AfterFourth" + Main.rand.Next(1, 3));
			}

			if (WastelandStorySystem.thirdMemoryRestored) {
				return Language.GetTextValue(DialogueKey + "AfterThird" + Main.rand.Next(1, 3));
			}

			if (WastelandStorySystem.ashHeartDefeated) {
				return Language.GetTextValue(DialogueKey + "AfterAsh" + Main.rand.Next(1, 3));
			}

			if (WastelandStorySystem.secondMemoryRestored) {
				return Language.GetTextValue(DialogueKey + "AfterSecondMemory" + Main.rand.Next(1, 4));
			}

			if (WastelandStorySystem.firstMemoryRestored) {
				return Language.GetTextValue(DialogueKey + "AfterMemory" + Main.rand.Next(1, 4));
			}

			if (WastelandStorySystem.archivistDefeated) {
				return Language.GetTextValue(DialogueKey + "AfterArchivist" + Main.rand.Next(1, 3));
			}

			if (WastelandStorySystem.scavengerDefeated) {
				return Language.GetTextValue(DialogueKey + "AfterScavenger" + Main.rand.Next(1, 4));
			}

			return Language.GetTextValue(DialogueKey + "Intro" + Main.rand.Next(1, 5));
		}

		public override void SetChatButtons(ref string button, ref string button2)
		{
			if (WastelandStorySystem.fourthMemoryRestored && WastelandStorySystem.endingChoice == WastelandStorySystem.EndingNone) {
				button = Language.GetTextValue(DialogueKey + "ButtonVessel");
				button2 = Language.GetTextValue(DialogueKey + "ButtonRefuse");
				return;
			}

			button = Language.GetTextValue(DialogueKey + "ButtonStory");
			button2 = HasNextFragment()
				? Language.GetTextValue(DialogueKey + "ButtonMemory")
				: Language.GetTextValue(DialogueKey + "ButtonTrade");
		}

		public override void OnChatButtonClicked(bool firstButton, ref string shop)
		{
			if (WastelandStorySystem.fourthMemoryRestored && WastelandStorySystem.endingChoice == WastelandStorySystem.EndingNone) {
				WastelandStorySystem.ChooseEnding(Main.LocalPlayer, firstButton ? WastelandStorySystem.EndingVessel : WastelandStorySystem.EndingRefuse);
				Main.npcChatText = firstButton
					? Language.GetTextValue("Mods.WastelandSoul.Dialogue.MechanicalCompanion.ChoiceVessel")
					: Language.GetTextValue("Mods.WastelandSoul.Dialogue.MechanicalCompanion.ChoiceRefuse");
				return;
			}

			if (firstButton) {
				Main.npcChatText = GetStoryDialogue();
				return;
			}

			if (HasNextFragment()) {
				Main.npcChatText = GetMemoryDialogue();
				return;
			}

			shop = "Chips";
		}

		public override void AddShops()
		{
			NPCShop npcShop = new NPCShop(Type, "Chips")
				.Add(Priced<Content.Items.Accessories.ScavengerRangerCharm>(8))
				.Add(Priced<Content.Items.Accessories.ScavengerSummonerCharm>(8))
				.Add(Priced<Content.Items.Accessories.ScavengerRogueCharm>(8))
				.Add(Priced<Content.Items.Accessories.ArchivistWarriorCharm>(16), new Condition("Mods.WastelandSoul.Conditions.AfterScavenger", () => WastelandStorySystem.scavengerDefeated))
				.Add(Priced<Content.Items.Accessories.ArchivistMageCharm>(16), new Condition("Mods.WastelandSoul.Conditions.AfterScavenger", () => WastelandStorySystem.scavengerDefeated))
				.Add(Priced<Content.Items.Accessories.ArchivistRogueCharm>(16), new Condition("Mods.WastelandSoul.Conditions.AfterScavenger", () => WastelandStorySystem.scavengerDefeated))
				.Add(Priced<Content.Items.Accessories.AshHeartMageCharm>(28), new Condition("Mods.WastelandSoul.Conditions.AfterArchivist", () => WastelandStorySystem.archivistDefeated))
				.Add(Priced<Content.Items.Accessories.AshHeartRangerCharm>(28), new Condition("Mods.WastelandSoul.Conditions.AfterArchivist", () => WastelandStorySystem.archivistDefeated))
				.Add(Priced<Content.Items.Accessories.AshHeartSummonerCharm>(28), new Condition("Mods.WastelandSoul.Conditions.AfterArchivist", () => WastelandStorySystem.archivistDefeated))
				.Add(Priced<Content.Items.Accessories.FireplaceWarriorCharm>(48), new Condition("Mods.WastelandSoul.Conditions.AfterAshHeart", () => WastelandStorySystem.ashHeartDefeated))
				.Add(Priced<Content.Items.Accessories.FireplaceSummonerCharm>(48), new Condition("Mods.WastelandSoul.Conditions.AfterAshHeart", () => WastelandStorySystem.ashHeartDefeated))
				.Add(Priced<Content.Items.Accessories.FireplaceRogueCharm>(48), new Condition("Mods.WastelandSoul.Conditions.AfterAshHeart", () => WastelandStorySystem.ashHeartDefeated));
			npcShop.Register();
		}

		public override void ModifyActiveShop(string shopName, Item[] items)
		{
			if (shopName != "Chips") {
				return;
			}

			int currency = ChipCurrencySystem.CurrencyId;

			foreach (Item item in items) {
				if (item == null || item.IsAir) {
					continue;
				}

				item.shopSpecialCurrency = currency;
			}
		}

		private static Item Priced<T>(int chips) where T : ModItem
		{
			return new Item(ModContent.ItemType<T>()) { shopCustomPrice = chips };
		}

		private static bool HasNextFragment()
		{
			Player player = Main.LocalPlayer;

			if (WastelandStorySystem.dataTerminalRead && WastelandStorySystem.scavengerDefeated && !WastelandStorySystem.firstMemoryRestored) {
				return true;
			}

			if (WastelandStorySystem.archivistDefeated && !WastelandStorySystem.secondMemoryRestored && player.HasItem(WastelandMemorySystem.SoulFragmentTypeForBoss(2))) {
				return true;
			}

			if (WastelandStorySystem.ashHeartDefeated && !WastelandStorySystem.thirdMemoryRestored && player.HasItem(WastelandMemorySystem.SoulFragmentTypeForBoss(3))) {
				return true;
			}

			if (WastelandStorySystem.fireplaceGuardianDefeated && !WastelandStorySystem.fourthMemoryRestored && player.HasItem(WastelandMemorySystem.SoulFragmentTypeForBoss(4))) {
				return true;
			}

			return false;
		}

		private string GetStoryDialogue()
		{
			if (WastelandStorySystem.endingChoice == WastelandStorySystem.EndingVessel) {
				return Language.GetTextValue(DialogueKey + "StoryEndingVessel");
			}

			if (WastelandStorySystem.endingChoice == WastelandStorySystem.EndingRefuse) {
				return Language.GetTextValue(DialogueKey + "StoryEndingRefuse");
			}

			if (WastelandStorySystem.fourthMemoryRestored) {
				return Language.GetTextValue(DialogueKey + "StoryChoice");
			}

			if (WastelandStorySystem.fireplaceGuardianDefeated) {
				return Language.GetTextValue(DialogueKey + "StoryBringFourth");
			}

			if (WastelandStorySystem.thirdMemoryRestored) {
				return Language.GetTextValue(DialogueKey + "StoryHuntGuardian");
			}

			if (WastelandStorySystem.ashHeartDefeated) {
				return Language.GetTextValue(DialogueKey + "StoryBringThird");
			}

			if (WastelandStorySystem.secondMemoryRestored) {
				return Language.GetTextValue(DialogueKey + "StoryHuntAsh");
			}

			if (WastelandStorySystem.firstMemoryRestored) {
				return Language.GetTextValue(DialogueKey + "StoryAwaitSecond");
			}

			if (WastelandStorySystem.dataTerminalRead) {
				return Language.GetTextValue(DialogueKey + "StoryTerminalRead");
			}

			if (WastelandStorySystem.scavengerDefeated) {
				return Language.GetTextValue(DialogueKey + "StoryFireplace");
			}

			return Language.GetTextValue(DialogueKey + "StoryHunt");
		}

		private string GetMemoryDialogue()
		{
			Player player = Main.LocalPlayer;

			if (WastelandStorySystem.fireplaceGuardianDefeated && !WastelandStorySystem.fourthMemoryRestored) {
				if (WastelandMemorySystem.TryDeliver(player, 4)) {
					WastelandStorySystem.RestoreFourthMemory();
					return Language.GetTextValue(DialogueKey + "MemoryFourth");
				}

				return Language.GetTextValue(DialogueKey + "MemoryFourthNeedFragment");
			}

			if (WastelandStorySystem.ashHeartDefeated && !WastelandStorySystem.thirdMemoryRestored) {
				if (WastelandMemorySystem.TryDeliver(player, 3)) {
					WastelandStorySystem.RestoreThirdMemory();
					return Language.GetTextValue(DialogueKey + "MemoryThird");
				}

				return Language.GetTextValue(DialogueKey + "MemoryThirdNeedFragment");
			}

			if (WastelandStorySystem.archivistDefeated && !WastelandStorySystem.secondMemoryRestored) {
				if (WastelandMemorySystem.TryDeliver(player, 2)) {
					WastelandStorySystem.RestoreSecondMemory();
					return Language.GetTextValue(DialogueKey + "MemorySecond");
				}

				return Language.GetTextValue(DialogueKey + "MemorySecondNeedFragment");
			}

			if (WastelandStorySystem.scavengerDefeated && WastelandStorySystem.dataTerminalRead && !WastelandStorySystem.firstMemoryRestored) {
				WastelandMemorySystem.TryDeliver(player, 1);
				WastelandStorySystem.RestoreFirstMemory();
				WastelandPlayer modPlayer = player.GetModPlayer<WastelandPlayer>();
				modPlayer.heardFirstMemory = true;
				modPlayer.knowsWatchmanProtocol = true;
				return Language.GetTextValue(DialogueKey + "MemoryFirst");
			}

			if (WastelandStorySystem.scavengerDefeated && !WastelandStorySystem.dataTerminalRead) {
				return Language.GetTextValue(DialogueKey + "StoryFireplace");
			}

			return Language.GetTextValue(DialogueKey + "MemoryBlank");
		}

		// ==================== 城镇 NPC 自卫攻击 ====================
		// 说明：智械人不是战斗单位，这套攻击只用于自卫，数值刻意保守。

		public override void TownNPCAttackStrength(ref int damage, ref float knockback)
		{
			damage = 14;
			knockback = 2f;
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
		{
			cooldown = 60;
			randExtraCooldown = 30;
		}

		public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
		{
			// 占位：沿用原版紫水晶弹，后续替换为智械专属弹幕
			projType = ModContent.ProjectileType<Content.Projectiles.CompanionSpark>();
			attackDelay = 20;
		}

		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
		{
			multiplier = 8f;
			randomOffset = 0.5f;
		}
	}
}
