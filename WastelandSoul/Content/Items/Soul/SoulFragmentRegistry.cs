using System.Collections.Generic;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Items.Soul
{
	/// <summary>
	/// 灵魂碎片的集中索引：给"玩家身上带了几枚"这类判断用。
	/// <para/>新增 Boss 时，只要把它的碎片类型加进 <see cref="AllTypes"/> 即可。
	/// </summary>
	public static class SoulFragmentRegistry
	{
		/// <summary>按 Boss 顺序排列的灵魂碎片类型。</summary>
		public static readonly IReadOnlyList<int> AllTypes = new List<int> {
			ModContent.ItemType<SoulFragmentScavenger>(),
			ModContent.ItemType<SoulFragmentSecond>(),
			ModContent.ItemType<SoulFragmentThird>(),
			ModContent.ItemType<SoulFragmentFourth>()
		};
	}
}
