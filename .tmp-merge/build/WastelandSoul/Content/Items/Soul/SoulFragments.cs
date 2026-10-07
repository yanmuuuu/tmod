using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Soul
{
	// ====================================================================================
	// 灵魂碎片：每个 Boss 掉一枚，是智械人恢复记忆的必需品（剧情伏笔）。
	// 目前只做"物品 + 进度记录"这一层框架；交付流程（在对话里把碎片交给她）
	// 与后续三个 Boss 的正式命名，等 Boss 2~4 的概念定下来再补。
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
