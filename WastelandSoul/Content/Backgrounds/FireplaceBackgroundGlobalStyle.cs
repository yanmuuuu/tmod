using SubworldLibrary;
using Terraria.ModLoader;
using WastelandSoul.Content.Subworlds;

namespace WastelandSoul.Content.Backgrounds
{
	/// <summary>
	/// 「壁炉里任何深度都只画星空」的**兜底**层。
	///
	/// <para/>为什么光靠 <see cref="FireplaceStarfieldUnderground"/> 不够（都是 IL 里读过的事实，
	/// 不是猜）：
	/// <list type="number">
	/// <item><b>原版的地下背景槽位数组是 7 个</b>（<c>Main.DrawBackground</c> 里
	/// <c>new int[7]</c> + <c>switch (Main.undergroundBackground)</c> 填 0~6 层），
	/// 而 <c>UndergroundBackgroundStylesLoader</c> 是用 <c>Initialize(22)</c> 建的 ——
	/// 也就是说**模组样式号 ≥ 22，原版那段 switch 一个槽都不会填**，整条数组是默认值 0。
	/// 旧实现只填了 4 个槽（贴图只有 4 张），剩下 3 个槽就是"原版背景贴图 0"
	/// —— 那正是玩家在地下看到的原版残留。这里改成**按 7 槽循环填满**。</item>
	/// <item><b>地狱层是另一套</b>：<c>Main.DrawUnderworldBackground</c> 用
	/// <c>TextureAssets.Underworld[]</c> 画，不走 <c>undergroundBackground</c> 槽位
	/// （触发条件是 <c>screenPosition.Y + screenHeight ≥ (maxTilesY-220)×16</c>，
	/// 壁炉世界里 y ≥ 1080 就会画）。好在 <c>Main.DoDraw</c> 的顺序是
	/// <c>DrawBG()</c>（里面调 DrawUnderworldBackground）→ 之后才 <c>DrawBackground()</c>
	/// （里面画 undergroundBackground 这 7 层），所以**只要 7 层都是我们的不透明星空，
	/// 就会盖住地狱层**。这里把 7 层全填就是这个意思。</item>
	/// <item>把"选样式"也一并钉住：<c>ChooseUndergroundBackgroundStyle</c> /
	/// <c>ChooseSurfaceBackgroundStyle</c> 是原版在算完自己那套之后调的最后一手
	/// （IL：<c>hook.Invoke(ref style)</c> 紧跟在所有生物群系判定之后），
	/// 在壁炉里直接把它们改成星空样式，任何原版判定都抢不走。</item>
	/// </list>
	///
	/// <para/>为什么要有这个类而不是把这些写进 <see cref="FireplaceStarfieldSceneEffect"/>：
	/// 场景效果只能提供给"当前场景效果"这一个通道，而 <c>GlobalBackgroundStyle</c> 是
	/// tML 给"所有背景样式（含原版样式）"的全局钩子 —— 原版自己选中的样式也会经过这里，
	/// 所以这是唯一能保证"没有任何一个原版样式的槽位漏出来"的位置。
	///
	/// <para/>⚠️ 只在壁炉子世界里生效（每次都现查 <c>IsActive</c>），主世界一根手指都不碰。
	/// </summary>
	public class FireplaceBackgroundGlobalStyle : GlobalBackgroundStyle
	{
		private static bool InFireplace => SubworldSystem.IsActive<FireplaceSubworld>();

		/// <inheritdoc/>
		public override void FillUndergroundTextureArray(int style, int[] textureSlots)
		{
			if (InFireplace) {
				FireplaceStarfieldUnderground.FillStarfield(Mod, textureSlots);
			}
		}

		/// <inheritdoc/>
		public override void ChooseUndergroundBackgroundStyle(ref int style)
		{
			if (InFireplace) {
				style = ModContent.GetInstance<FireplaceStarfieldUnderground>().Slot;
			}
		}

		/// <inheritdoc/>
		public override void ChooseSurfaceBackgroundStyle(ref int style)
		{
			if (InFireplace) {
				style = ModContent.GetInstance<FireplaceStarfieldBackground>().Slot;
			}
		}
	}
}
