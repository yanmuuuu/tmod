using Terraria.ModLoader;

namespace WastelandSoul.Content.Backgrounds
{
	/// <summary>
	/// 壁炉的**宇宙星空背景**（地表层）。
	///
	/// <para/>玩家要求：「背景都改为宇宙，但不受宇宙的重力改变影响」。
	///
	/// <para/>**为什么不走 <c>ZoneSpace</c>**：
	/// 原版那套"到太空就变星空"是靠**世界高度**触发的 —— 玩家 y 高过
	/// <c>Main.worldSurface × 0.35</c> 时 <c>Player.ZoneSpace</c> 变真，
	/// 同时带来**低重力**（跳跃/下落手感全变）和星空天幕。
	/// 想只拿星空、不拿低重力，就**不能**动世界高度，也不能把 <c>ZoneSpace</c> 置真：
	/// <list type="bullet">
	/// <item>本次改动**没有**把 <c>Main.worldSurface</c> 压小（它仍然是
	/// <see cref="Content.Subworlds.FireplaceLayout.SurfaceBase"/>=840，太空线 294，
	/// 只有塔顶那块本来就在线上的场地照旧算太空）；</item>
	/// <item>没有碰任何玩家参数（重力 / 跳跃 / 移动都没写）；</item>
	/// <item>只换了**背景样式的编号**：这个类是纯渲染选择器，游戏逻辑一概不参与，
	/// 所以"看起来是宇宙、物理还是壁炉的物理"。</item>
	/// </list>
	///
	/// <para/>怎么接上去（这条路是 tML 官方给的）：
	/// <c>Main.DrawBG</c> 每帧都会调 <c>Main.GetPreferredBGStyleForPlayer()</c> 重新算
	/// <c>Main.bgStyle</c>，而那个方法在优先级 ≥ <c>SceneEffectPriority.BiomeHigh</c> 时
	/// 会直接采用**当前场景效果**带过来的 <c>SurfaceBackgroundStyle.Slot</c>
	/// （见 <c>SurfaceBackgroundStylesLoader.ChooseStyle</c>）。
	/// 所以真正生效的是 <see cref="FireplaceStarfieldSceneEffect"/>：
	/// 它在壁炉子世界里常驻，把本样式塞给场景效果。
	/// 生成收尾里那次 <c>Main.bgStyle = ...</c> 只是让刚生成完那一帧也是星空，不是主路径。
	///
	/// <para/>贴图放在模组根的 <c>Backgrounds/</c> 目录下（只有这个目录名会被 tML
	/// 自动注册成背景贴图），由 <c>tools/gen_fireplace_art.py</c> 纯程序化生成。
	/// 三层都返回槽位；某一层返回 -1（默认实现）时 tML 会跳过绘制，不会报错。
	/// </summary>
	public class FireplaceStarfieldBackground : ModSurfaceBackgroundStyle
	{
		/// <inheritdoc/>
		public override void ModifyFarFades(float[] fades, float transitionSpeed)
		{
			// 通例：把自己那一档推向 1，其它样式推向 0（切换时才有渐变，不会突然换天）
			for (int i = 0; i < fades.Length; i++) {
				if (i == Slot) {
					fades[i] += transitionSpeed;

					if (fades[i] > 1f) {
						fades[i] = 1f;
					}
				}
				else {
					fades[i] -= transitionSpeed;

					if (fades[i] < 0f) {
						fades[i] = 0f;
					}
				}
			}
		}

		/// <summary>
		/// 取背景贴图槽位。
		/// <para/>用 <c>TryGetBackgroundSlot</c> 而不是 <c>GetBackgroundSlot</c>：
		/// 贴图没打包进来时前者只是返回 false（这里给 -1，tML 会跳过这一层的绘制），
		/// 后者会抛异常 —— 背景是在主线程绘制里选的，抛出去就是引擎崩溃。
		/// </summary>
		private int SlotOf(string path)
		{
			return BackgroundTextureLoader.TryGetBackgroundSlot(Mod, path, out int slot) ? slot : -1;
		}

		/// <summary>最远一层：不透明的深空 + 星点（把原版天空底色整个盖住）。</summary>
		public override int ChooseFarTexture()
		{
			return SlotOf("Backgrounds/FireplaceStarfieldFar");
		}

		/// <summary>中间层：稀薄星云 + 中等亮度的星。</summary>
		public override int ChooseMiddleTexture()
		{
			return SlotOf("Backgrounds/FireplaceStarfieldMiddle");
		}

		/// <summary>最近层：几颗带光晕的亮星（视差最大，有纵深感）。</summary>
		public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float xOffset, ref float yOffset)
		{
			scale *= 1.1f;
			parallax *= 1.35;
			return SlotOf("Backgrounds/FireplaceStarfieldClose");
		}
	}
}
