using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;
using WastelandSoul.Content.Items.Weapons.Scrap;
using WastelandSoul.Content.NPCs.Wildlife;

namespace WastelandSoul.Content.Items.Weapons.Rust
{
	/// <summary>
	/// 武器扩充包的「时期敌人掉落」接线。
	/// <para/>纪律：
	/// <list type="bullet">
	/// <item>**只追加**掉落规则（npcLoot.Add），绝不删除或改动手上已有的规则；</item>
	/// <item>掉落概率都很低（3%~8%），不抢走各时期专属武器（CLine 系列）的位置；</item>
	/// <item>掉落专属的 <see cref="ScrapWardenStaff"/> 没有配方，靠这里获得。</item>
	/// </list>
	/// 时期对应：废料爬行者=清道夫时期；索引蛾=归档者时期；灰烬潜行者/炉卫=困难模式之后（只放少量收集向掉落）。
	/// </summary>
	public class RustPackLootNPC : GlobalNPC
	{
		public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
		{
			// 清道夫时期（地表白天）：锈蚀线的强化版作为稀有掉落
			if (npc.type == ModContent.NPCType<ScrapCrawler>()) {
				npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustCleaverEX>(), 25));
				npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustSpitterStaffEX>(), 33));
			}

			// 归档者时期（地下泥土层）：废铁重工线的掉落专属 + 少量强化件
			if (npc.type == ModContent.NPCType<IndexMoth>()) {
				npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScrapWardenStaff>(), 18));
				npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RustBoltWandEX>(), 25));
			}

			// 困难模式之后：只放「早期强力件」当收集/小号掉落，不参与当前强度
			if (npc.type == ModContent.NPCType<AshStalker>()) {
				npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScrapRailgunEX>(), 30));
			}
		}
	}
}
