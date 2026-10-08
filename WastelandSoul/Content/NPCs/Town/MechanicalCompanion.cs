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
		/// 朝向与帧。
		///
		/// <para/>玩家要求：**向左走就朝左、向右走就朝右**，并且**不要正面 / 背面朝像**。
		/// 所以：
		/// <list type="number">
		/// <item>帧表（<c>MechanicalCompanion.png</c>，25 帧）已经重排成**全部侧身像**
		/// （见 <c>tools/reorient_companion.py</c>）：0-3 行走 / 4 站立 / 5 低头 / 6 站立 / 7 抬头 /
		/// 8-15 行走 / 16-20 站立 / 21-24 攻击；</item>
		/// <item>左右不各画一套，而是**按走路方向设 spriteDirection**，让游戏水平翻转 ——
		/// 这样"走路方向 = 朝向"永远一致，也不会再冒出正面像；</item>
		/// <item>小动作（低头/抬头）仍然强制用 5 / 7 帧，并且只在站定时触发（见 <see cref="AI"/>）。</item>
		/// </list>
		/// </summary>
		public override void FindFrame(int frameHeight)
		{
			// 1) 朝向 = 走路方向（站着不动时保持上一次朝向）
			if (NPC.velocity.X > 0.05f) {
				NPC.direction = 1;
			}
			else if (NPC.velocity.X < -0.05f) {
				NPC.direction = -1;
			}

			NPC.spriteDirection = NPC.direction;

			// 2) 小动作期间强制用低头 / 抬头帧
			if (NPC.localAI[1] > 0f) {
				int frame = NPC.localAI[2] == 1f ? 5 : 7;   // 5 = 低头，7 = 抬头
				NPC.frame.Y = frame * frameHeight;
				NPC.frameCounter = 0.0;
				return;
			}

			// 3) 其余情况交给原版城镇 NPC 的帧逻辑（AnimationType = Guide）
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

		/// <summary>
		/// 身上（背包或银行）带着灵魂碎片时，第二个按钮就是「交付记忆碎片」。
		/// <para/>**四段都走这条路**：记忆还没恢复的那枚用来推进剧情；
		/// 要是那段记忆早就恢复过（比如第一段是壁炉数据终端触发的），这枚就是**补交** ——
		/// 照样能交、照样被消耗，只是不再重播那段剧情。
		/// </summary>
		private static bool HasNextFragment()
		{
			return WastelandMemorySystem.NextCarriedFragmentIndex(Main.LocalPlayer) > 0;
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

		/// <summary>
		/// 交付碎片的对话：**消耗碎片 → 她自己更新记忆 → 回一句台词**。
		/// <para/>四段共用一个入口（<see cref="WastelandMemorySystem.TryHandIn"/>）：
		/// 记忆还没恢复的那枚推进剧情；已经恢复过的那枚当**补交**处理 ——
		/// 碎片被消耗掉、给她一句「已归档」，但**不重播**那段记忆。
		/// </summary>
		private string GetMemoryDialogue()
		{
			Player player = Main.LocalPlayer;
			int bossIndex = WastelandMemorySystem.NextCarriedFragmentIndex(player);

			if (bossIndex <= 0) {
				return Language.GetTextValue(DialogueKey + "MemoryBlank");
			}

			bool alreadyRecovered = WastelandMemorySystem.MemoryAlreadyRecovered(player, bossIndex);

			if (!WastelandMemorySystem.TryHandIn(player, bossIndex)) {
				return Language.GetTextValue(DialogueKey + "MemoryBlank");
			}

			if (alreadyRecovered) {
				return Language.GetTextValue(DialogueKey + "MemorySupplemented");
			}

			switch (bossIndex) {
				case 1:
					// 第一段的正文由 RestoreFirstMemory 播报，这里只回一句进度，不重复台词
					return Language.GetTextValue(DialogueKey + "MemoryRecovered");
				case 2:
					return Language.GetTextValue(DialogueKey + "MemorySecond");
				case 3:
					return Language.GetTextValue(DialogueKey + "MemoryThird");
				case 4:
					return Language.GetTextValue(DialogueKey + "MemoryFourth");
				default:
					return Language.GetTextValue(DialogueKey + "MemoryBlank");
			}
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
