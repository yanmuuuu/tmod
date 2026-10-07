using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Common.Effects
{
	// ====================================================================================
	// 自研表现层：刀光 / 拖尾。**不依赖任何第三方模组**，也**不新增任何贴图**
	// （用 TextureAssets.MagicPixel 这 1x1 白点拉伸成拖尾段）。
	//
	// 三条铁律：
	//   1) 纯客户端绘制（Main.dedServ 直接返回），绝不影响伤害/AI/判定；
	//   2) **先确认 SpriteBatch 的状态**：弹幕 PostDraw 里批次已经开着（那套是屏幕坐标，
	//      要减 Main.screenPosition）；而 ModSystem.PostDrawTiles 里**批次没开**，
	//      必须自己 Begin/End 并带 Main.GameViewMatrix 变换（用世界坐标）。
	//      ⚠️ 直接 Draw 会抛 `InvalidOperationException: Draw was called, but Begin has not
	//      yet been called`，tML 把它记成 `Main engine crash` —— 整个游戏挂掉（实测踩过）；
	//   3) 每处拖尾都有固定段数上限，历史记录及时清理，不在每帧做大分配。
	// ====================================================================================

	/// <summary>五职业配色（按 DamageClass 区分）。</summary>
	public static class WastelandEffectColors
	{
		public static Color For(DamageClass damageClass)
		{
			if (damageClass == DamageClass.Melee) {
				return new Color(232, 120, 72);      // 战士：橙红
			}
			if (damageClass == DamageClass.Magic) {
				return new Color(140, 120, 240);     // 法师：蓝紫
			}
			if (damageClass == DamageClass.Ranged) {
				return new Color(238, 206, 110);     // 射手：暖黄
			}
			if (damageClass == DamageClass.Summon) {
				return new Color(120, 220, 190);     // 召唤师：青绿
			}
			if (damageClass == DamageClass.Throwing) {
				return new Color(206, 110, 180);     // 盗贼：紫红
			}

			return new Color(200, 200, 200);
		}
	}

	/// <summary>拖尾绘制（1x1 白点分段拉伸 + 渐隐）。</summary>
	public static class WastelandTrailRenderer
	{
		/// <summary>
		/// 沿一串世界坐标点画拖尾。越新的点越粗越亮。
		/// <para/><paramref name="worldSpace"/> = true 时点被当作**世界坐标**（调用方用
		/// <c>Main.GameViewMatrix.TransformationMatrix</c> 开了批次）；
		/// false 时按原版弹幕绘制那套**屏幕坐标**惯例减去 <c>Main.screenPosition</c>。
		/// </summary>
		public static void Draw(SpriteBatch spriteBatch, IList<Vector2> points, Color color,
			float maxWidth, float maxAlpha, bool worldSpace = false)
		{
			int count = points.Count;

			if (count < 2) {
				return;
			}

			Texture2D pixel = TextureAssets.MagicPixel.Value;

			for (int i = 1; i < count; i++) {
				float t = i / (float)(count - 1);          // 0 = 最老，1 = 最新
				Vector2 a = points[i - 1];
				Vector2 b = points[i];
				float length = Vector2.Distance(a, b);
				float width = maxWidth * t;

				if (length < 0.01f || width < 0.5f) {
					continue;
				}

				Vector2 mid = worldSpace ? (a + b) * 0.5f : (a + b) * 0.5f - Main.screenPosition;

				spriteBatch.Draw(
					pixel,
					mid,
					null,
					color * (maxAlpha * t),
					(b - a).ToRotation(),
					new Vector2(0.5f, 0.5f),
					new Vector2(length, width),
					SpriteEffects.None,
					0f);
			}
		}
	}

	// ====================================================================================
	// 【已移除】本模组弹幕的自动拖尾（原 WastelandTrailGlobalProjectile）
	//
	// 你的反馈：用本模组的武器发射弹幕、以及 Boss 发射弹幕时，都会拖出**一条白线**，干扰视野。
	// 原因：这个 GlobalProjectile 会给**每一个**本模组弹幕自动加拖尾（每个弹幕 12 个历史点、
	// 用 1x1 白点拉伸成 16px 宽的段），而配色是按 DamageClass 查表，
	// **敌对弹幕 / 无职业伤害的弹幕全部落到兜底色 `(200,200,200)`** —— 那就是那条白线；
	// 高速弹幕的两个历史点之间还会被拉成一条很长的直线，所以看起来像"白线扫过屏幕"。
	//
	// 结论：**不做自动拖尾**。真要给某个弹幕加拖尾，请在那个弹幕自己的
	// `ModProjectile.PreDraw/PostDraw` 里手动画（那里批次是开着的）、并且用明确的颜色与短历史，
	// 不要再挂 GlobalProjectile 全局加。
	// ====================================================================================

	/// <summary>记录玩家挥砍时刀尖的轨迹（只在客户端算）。</summary>
	public class WastelandSwingTracker : ModPlayer
	{
		private const int MaxPoints = 10;

		public readonly List<Vector2> SwingPoints = new List<Vector2>();

		public Color SwingColor = Color.White;

		public bool Swinging;

		public override void PostUpdate()
		{
			Swinging = false;

			Item item = Player.HeldItem;

			// 只在「用着本模组的近战武器」时记录
			if (item == null || item.IsAir || Player.itemAnimation <= 0
				|| item.ModItem is not WastelandClassWeapon
				|| item.DamageType != DamageClass.Melee) {
				SwingPoints.Clear();
				return;
			}

			Swinging = true;
			SwingColor = WastelandEffectColors.For(item.DamageType);

			// 用玩家手臂末端近似刀尖轨迹
			float rotation = Player.itemRotation;
			Vector2 offset = new Vector2(Player.direction * 24f, -4f).RotatedBy(rotation);
			SwingPoints.Add(Player.MountedCenter + offset);

			while (SwingPoints.Count > MaxPoints) {
				SwingPoints.RemoveAt(0);
			}
		}
	}

	/// <summary>把挥砍弧光画出来（世界坐标系）。</summary>
	public class WastelandSwingDrawSystem : ModSystem
	{
		/// <summary>
		/// 粒子系统（<see cref="WastelandFxSystem"/>）的**推进**：每帧都要跑，和绘制分开。
		/// <para/>⚠️ 合并另一条线的内容时踩过：把 WastelandFx.cs 拿过来了，却保留了自己这份
		/// 重写过的表现层 —— 结果**没有任何地方调用 WastelandFxSystem.Update()/Draw()**，
		/// 新 Boss 的阶段演出、弹幕火花、命中爆开全部"只入队、不更新、不绘制"。
		/// 粒子系统的推进固定挂在这里，绘制在 <see cref="PostDrawTiles"/>。
		/// </summary>
		public override void PostUpdateDusts()
		{
			WastelandFxSystem.Update();
		}

		/// <summary>
		/// ⚠️ 这里踩过一次**引擎级崩溃**（client.log 里的 `Main engine crash`）：
		/// <c>PostDrawTiles</c> 时 <c>Main.spriteBatch</c> **并不在 Begin 状态**，
		/// 直接 Draw 会抛 `InvalidOperationException: Draw was called, but Begin has not yet been called`
		/// → 整个游戏挂掉。所以：
		/// <list type="number">
		/// <item>挥砍弧光先收集要画的东西，没有就不开批次；</item>
		/// <item>自己 <c>Begin</c>（带 <c>Main.GameViewMatrix.TransformationMatrix</c>，
		/// 于是直接传**世界坐标**，不要再减 <c>Main.screenPosition</c>）；</item>
		/// <item><c>End</c> 放在 <c>finally</c> 里，保证异常也不会留下"开着不关"的批次；</item>
		/// <item><c>WastelandFxSystem.Draw()</c> 自己会 Begin/End，**必须无条件调用**
		/// （不能因为"这一次没有挥砍"就跳过，否则粒子永远不显示，新 Boss 的演出全废）。</item>
		/// </list>
		/// </summary>
		public override void PostDrawTiles()
		{
			if (Main.dedServ) {
				return;
			}

			// 1) 先收集：没有任何挥砍时，连 Begin/End 都不做
			List<(IList<Vector2> Points, Color Color)> pending = null;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];

				if (!player.active || player.dead) {
					continue;
				}

				WastelandSwingTracker tracker = player.GetModPlayer<WastelandSwingTracker>();

				if (!tracker.Swinging || tracker.SwingPoints.Count < 2) {
					continue;
				}

				pending ??= new List<(IList<Vector2>, Color)>();
				pending.Add((tracker.SwingPoints, tracker.SwingColor));
			}

			// 2) 挥砍弧光：自己开批次（世界坐标），3) 无论如何都要 End
			// 用 AlphaBlend 而不是 Additive：加法混合下 9 段互相叠加会**饱和成白色**，
			// 又变成一条"白线"（拖尾那边就是因为颜色回退到近白才被当成白线）。
			if (pending != null) {
				try {
					Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
						DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

					try {
						foreach ((IList<Vector2> points, Color color) in pending) {
							WastelandTrailRenderer.Draw(Main.spriteBatch, points, color, 20f, 0.45f, worldSpace: true);

							// 刀尖补一颗光斑，让弧光有"锋"的感觉（入队，下一帧由粒子系统画出来）
							WastelandFxSystem.Glow(points[points.Count - 1], color, 1.3f, 5);
						}
					} finally {
						Main.spriteBatch.End();
					}
				} catch (Exception exception) {
					Mod.Logger.Warn("WastelandSwingDrawSystem: 画挥砍弧光失败（已忽略，不影响游戏）", exception);
				}
			}

			// 4) 粒子系统：**无条件**调用（它自己判空、自己 Begin/End）
			try {
				WastelandFxSystem.Draw();
			} catch (Exception exception) {
				Mod.Logger.Warn("WastelandSwingDrawSystem: 画粒子特效失败（已忽略，不影响游戏）", exception);
			}
		}
	}
}
