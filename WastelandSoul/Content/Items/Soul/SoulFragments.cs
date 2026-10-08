using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Soul
{
	// ====================================================================================
	// 灵魂碎片：**任务道具**，四个 Boss 各掉一枚，是智械人恢复记忆的必需品。
	//
	// 规则（任务道具化改动）：
	//   1. **每个世界只给一次** —— 掉落袋按 WastelandMemorySystem.ShouldGrantSoulFragment 判断
	//      （这段记忆还没恢复、且玩家背包/银行里没有这枚碎片才发）；
	//   2. **交给她就被消耗** —— WastelandMemorySystem.TryHandIn：碎片从背包/银行里扣掉，
	//      然后由她自行读取、更新记忆（第一段也支持这条路，另有壁炉数据终端那条）；
	//   3. 玩家自己用不了它（见基类 SoulFragment 的 SetDefaults / CanUseItem）。
	// ====================================================================================

	/// <summary>灵魂碎片·其一：清道夫（执行单元-07）残骸里析出的那一段代码。</summary>
	public class SoulFragmentScavenger : SoulFragment
	{
		public override int BossIndex => 1;
	}

	/// <summary>灵魂碎片·其二（Boss 2，概念待定）。</summary>
	public class SoulFragmentSecond : SoulFragment
	{
		public override int BossIndex => 2;
	}

	/// <summary>灵魂碎片·其三（Boss 3，概念待定）。</summary>
	public class SoulFragmentThird : SoulFragment
	{
		public override int BossIndex => 3;
	}

	/// <summary>灵魂碎片·其四（Boss 4，概念待定）。</summary>
	public class SoulFragmentFourth : SoulFragment
	{
		public override int BossIndex => 4;
	}
}
