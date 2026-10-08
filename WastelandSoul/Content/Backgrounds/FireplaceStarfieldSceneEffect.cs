using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using WastelandSoul.Content.Subworlds;

namespace WastelandSoul.Content.Backgrounds
{
	/// <summary>
	/// 「在壁炉里 = 背景是宇宙」这件事的**载体**。
	///
	/// <para/>tML 的背景样式不是自己"挂"上去的：<c>Main.DrawBG</c> 每帧调
	/// <c>Main.GetPreferredBGStyleForPlayer()</c>，那个方法会去问**当前场景效果**
	/// （<c>Player.CurrentSceneEffect</c>）要 <c>SurfaceBackgroundStyle</c> /
	/// <c>UndergroundBackgroundStyle</c> 的槽位（优先级要 ≥
	/// <c>SceneEffectPriority.BiomeHigh</c>，否则原版按自己的生物群系逻辑选）。
	/// 所以这里用一个常驻的场景效果把两个星空样式交给壁炉。
	///
	/// <para/>为什么不用 <c>ModBiome</c>：生物群系会顺带影响刷怪 / 图鉴 / 音乐等一堆东西，
	/// 而这里只要"换个天"，用最轻的 <see cref="ModSceneEffect"/> 就够。
	/// 音乐这一项现在由 <see cref="Music"/> 统一接管（见下面的优先级说明）。
	///
	/// <para/>⚠️ 关于低重力：本效果**只**给背景槽位，不碰
	/// <c>Main.worldSurface</c>/<c>Main.rockLayer</c>，更不会去置 <c>Player.ZoneSpace</c>。
	/// 太空低重力是原版按 `y &lt; worldSurface × 0.35` 自己算的，壁炉里那条线仍在 294，
	/// 只有塔顶那块场地本来就落在线上（和改动前一样）。
	///
	/// <para/>⚠️ 优先级为什么是 <c>Environment (4)</c> 而不是 <c>BiomeHigh (3)</c>
	/// （把 <c>Terraria.Main.UpdateAudio_DecideOnNewMusic</c> 的 IL 逐条看了一遍）：
	/// 原版选曲是一串「if (…) { newMusic = X; return; }」的阶梯，模组场景效果的音乐是在
	/// 几个固定的优先档位上被插进去的，从高到低是 **8 / 7 / 6 / 5 / 4 / 3 / 2 / 1**：
	/// <code>
	///   ... if (priority &gt;= 8) { newMusic = 场景音乐; return; }
	///       if (priority &gt;= 7) { ... }   // 入侵 / 事件音乐仍然优先
	///       if (priority &gt;= 6) { ... }
	///       if (priority &gt;= 5) { ... }
	///       if (priority &gt;= 4) { newMusic = 场景音乐; return; }   &lt;-- Environment
	///       if (player.position.Y &gt; UnderworldLayer * 16) { newMusic = 36; return; }  // 地狱强制换曲
	///       if (priority &gt;= 3) { newMusic = 场景音乐; return; }   &lt;-- BiomeHigh 在这里就晚了
	/// </code>
	/// 关键就是那条**地狱判定**：<c>Main.UnderworldLayer = maxTilesY - 200</c>，壁炉世界是
	/// 1300-200 = **1100**，而堡垒下半（1116~1120）、地下通道（1126~1194）与整个椭球地宫
	/// （1018~1282）都在它以下 —— 只给 BiomeHigh(3) 的话，玩家一进地宫就会听到原版地狱曲。
	/// 取 Environment(4) 正好压过地狱曲，同时**保留**入侵 / Boss（&gt;= 5）自己的音乐。
	/// </summary>
	public class FireplaceStarfieldSceneEffect : ModSceneEffect
	{
		/// <summary>
		/// 优先级取 <c>Environment (4)</c>：压过原版的地狱（岩浆层）强制换曲，但不去抢
		/// 入侵 / 事件 / Boss（≥5 档）的音乐。依据见类注释里那条 IL 阶梯。
		/// </summary>
		public override SceneEffectPriority Priority => SceneEffectPriority.Environment;

		/// <summary>
		/// 壁炉世界里**全程**同一首曲子（玩家要求"所有位置的音乐统一"）。
		///
		/// <para/>为什么写在场景效果上而不是去改 <c>Main.newMusic</c>：场景效果是 tML 给的
		/// 正规入口（<c>SceneEffectLoader.UpdateMusic</c> 会把它交给原版的选曲阶梯），
		/// 而且 <see cref="IsSceneEffectActive"/> 已经判了"在壁炉子世界"，
		/// **主世界的音乐一点都不会受影响**（这一条是硬要求）。
		///
		/// <para/>四王自己的 <c>Music</c>（<c>ModNPC.Music</c>）走的是另一条路：
		/// 它们的 <c>SceneEffectPriority</c> 只要 ≥ 5，仍会在更高的档位上胜出；
		/// 现在四个 Boss 都没设，所以壁炉里打 Boss 时听的也是这一首 —— 这正是玩家要的"统一"。
		/// </summary>
		public override int Music => MusicLoader.GetMusicSlot(Mod, "Music/Archivist");

		/// <inheritdoc/>
		public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle =>
			ModContent.GetInstance<FireplaceStarfieldBackground>();

		/// <inheritdoc/>
		public override ModUndergroundBackgroundStyle UndergroundBackgroundStyle =>
			ModContent.GetInstance<FireplaceStarfieldUnderground>();

		/// <summary>只要人在壁炉子世界里，就一直生效。</summary>
		public override bool IsSceneEffectActive(Player player)
		{
			return SubworldSystem.IsActive<FireplaceSubworld>();
		}
	}
}
