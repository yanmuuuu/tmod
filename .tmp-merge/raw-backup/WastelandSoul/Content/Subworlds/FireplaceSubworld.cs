using System.Collections.Generic;
using SubworldLibrary;
using Terraria.GameContent.Generation;
using Terraria.WorldBuilding;

namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 「壁炉」—— 旧世界的最后庇护所，做成一整个**独立的小世界**（Subworld Library 维度）。
	///
	/// <para/>为什么用子世界而不是在主世界挖一块：壁炉要表现的是「和外面那片废土不是同一个地方」，
	/// 独立世界才有干净的尺寸与光照语义；而且 Subworld Library 会把太空 / 两侧海洋 / 地狱
	/// 从子世界里去掉，所以世界可以做得非常小而不出问题。
	///
	/// <para/>尺寸 **600 × 600 格**：小到能一眼看完，又足够放下一座设施与两条走廊。
	///
	/// <para/>关于 API（都被编译器纠正过，别再凭印象写）：
	/// <list type="bullet">
	/// <item><c>Width</c> / <c>Height</c> / <c>Tasks</c> 是**抽象属性**，必须 override；
	/// <c>Tasks</c> 要返回一个列表，不能 <c>Tasks.Add(...)</c>。</item>
	/// <item><c>FileName</c> 是普通属性（不是 virtual），只能读。</item>
	/// <item><c>SetupContent()</c> 是 sealed，内容初始化要写在 <c>OnLoad()</c> 里。</item>
	/// </list>
	/// </summary>
	public class FireplaceSubworld : Subworld
	{
		private readonly List<GenPass> tasks = new List<GenPass>();

		/// <summary>世界宽度（格）。</summary>
		public override int Width => 600;

		/// <summary>世界高度（格）。</summary>
		public override int Height => 600;

		/// <summary>生成步骤：整座壁炉设施由一步搞定（见 <see cref="FireplaceStructure"/>）。</summary>
		public override List<GenPass> Tasks => tasks;

		/// <inheritdoc/>
		public override void OnLoad()
		{
			// 只跑我们自己那一步：壁炉是人工设施，
			// 不需要原版的地形 / 洞穴 / 矿物 / 丛林等生成步骤（那会让小世界变得乱七八糟）。
			tasks.Add(new PassLegacy("WastelandSoul: 壁炉设施", FireplaceStructure.Build));
		}
	}
}
