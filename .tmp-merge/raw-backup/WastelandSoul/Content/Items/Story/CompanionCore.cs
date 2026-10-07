using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Systems;
using WastelandSoul.Content.NPCs.Town;

namespace WastelandSoul.Content.Items.Story
{
	/// <summary>
	/// 智械核心：主人公魂穿时随身携带的东西。
	/// <para/>开局自动进入背包；**直接使用**即可把智械人送到身边（核心会被消耗），
	/// 她到达时会自报名字（随机候选，见 <see cref="MechanicalCompanion.CandidateNames"/>）。
	/// <para/>也可以留到最后，在精灵族遗迹的「精灵族躯体」上右键 —— 两条入口共用同一套逻辑。
	/// </summary>
	public class CompanionCore : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.maxStack = 1;
			Item.uniqueStack = true;
			Item.value = 0;
			Item.rare = ItemRarityID.Quest;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.consumable = true;      // 用掉它，换她到达
			Item.noMelee = true;
			Item.UseSound = SoundID.Item4;
		}

		/// <summary>她已经在了就别浪费核心（返回 false → 不消耗、给提示）。</summary>
		public override bool CanUseItem(Player player)
		{
			if (WastelandStorySystem.companionAwakened
				|| NPC.AnyNPCs(ModContent.NPCType<MechanicalCompanion>())) {
				if (player.whoAmI == Main.myPlayer) {
					Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.CompanionAlreadyHere"),
						180, 200, 255);
				}

				return false;
			}

			return true;
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer) {
				return true;
			}

			MechanicalCompanion.TrySummon(player, player.GetSource_ItemUse(Item));
			return true;
		}
	}
}
