using Terraria.ModLoader;

namespace WastelandSoul.Content.Backgrounds
{
	/// <summary>
	/// 壁炉的**宇宙星空背景**（地下层：洞穴 / 岩石层 / 地狱段）。
	///
	/// <para/>为什么还要单独做地下这一套：壁炉的门厅（y≈900~1080）和椭球地宫（y≈1018~1282）
	/// 都在 <c>Main.rockLayer</c>=950 以下，原版那时画的是**洞穴背景**（灰色岩壁视差层），
	/// 不是一个"地表背景"能盖住的 —— 玩家在地下抬头也一样该看见星空。
	///
	/// <para/>⚠️ 槽位是 **7 个**，不是 4 个（这一批按 IL 改正）：<c>Main.DrawBackground</c> 里
	/// 是 <c>int[] array = new int[7]</c> + `switch (Main.undergroundBackground)` 逐层填，
	/// 而 <c>UndergroundBackgroundStylesLoader</c> 用 <c>Initialize(22)</c> 建表 ——
	/// **模组样式号 ≥ 22，原版那段 switch 一个槽都不填**，数组保持默认 0。
	/// 旧实现只填 4 个槽（贴图也只有 4 张），剩下 3 槽就是"原版背景贴图 0"，
	/// 于是地下会漏出原版的天空/岩壁层。现在改成**按实际槽位数循环填满**。
	///
	/// <para/>低重力为什么不会被带出来：这个类只填**背景贴图的槽位**（纯渲染），
	/// 和 <c>Player.ZoneSpace</c>、世界高度、玩家参数都没有关系。
	/// 详见 <see cref="FireplaceStarfieldBackground"/> 顶部的说明。
	/// </summary>
	public class FireplaceStarfieldUnderground : ModUndergroundBackgroundStyle
	{
		/// <summary>四张程序化星空的路径（由 <c>tools/gen_fireplace_art.py</c> 生成）。</summary>
		private static readonly string[] Paths = {
			"Backgrounds/FireplaceStarfieldUG0",
			"Backgrounds/FireplaceStarfieldUG1",
			"Backgrounds/FireplaceStarfieldUG2",
			"Backgrounds/FireplaceStarfieldUG3",
		};

		/// <summary>
		/// 把星空贴图**填满整个槽位数组**（贴图不够就循环用；槽位比贴图少也不会越界）。
		///
		/// <para/>用 <c>TryGetBackgroundSlot</c>：贴图没打包进来时它只返回 false
		/// （这里给 -1，tML 会跳过这一层），不会在绘制线程里抛异常。
		/// </summary>
		internal static void FillStarfield(Mod mod, int[] textureSlots)
		{
			if (textureSlots == null || textureSlots.Length == 0) {
				return;
			}

			for (int i = 0; i < textureSlots.Length; i++) {
				string path = Paths[i % Paths.Length];

				textureSlots[i] = BackgroundTextureLoader.TryGetBackgroundSlot(mod, path, out int slot)
					? slot
					: -1;
			}
		}

		/// <inheritdoc/>
		public override void FillTextureArray(int[] textureSlots)
		{
			FillStarfield(Mod, textureSlots);
		}
	}
}
