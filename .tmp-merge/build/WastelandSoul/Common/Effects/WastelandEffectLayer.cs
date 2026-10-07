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
	//   2) 不重新 Begin/End SpriteBatch（只往已经开着的批次里画，避免与其它模组冲突、崩溃）；
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
		/// <para/>注意：世界坐标系绘制要减去 Main.screenPosition。
		/// </summary>
		public static void Draw(SpriteBatch spriteBatch, IList<Vector2> points, Color color, float maxWidth, float maxAlpha)
		{
			int count = points.Count;

			if (count < 2) {
				return;
			}

			Texture2D pixel = TextureAssets.MagicPixel.Value;

			for (int pass = 0; pass < 2; pass++) {
				bool core = pass == 1;

				for (int i = 1; i < count; i++) {
					float t = i / (float)(count - 1);
					Vector2 a = points[i - 1];
					Vector2 b = points[i];
					float length = Vector2.Distance(a, b);
					float width = (core ? maxWidth * 0.42f : maxWidth) * t;

					if (length < 0.01f || width < 0.4f) {
						continue;
					}

					Vector2 mid = (a + b) * 0.5f - Main.screenPosition;
					Color ink = core ? Color.Lerp(color, Color.White, 0.55f) : color;
					float alpha = core ? maxAlpha * t : maxAlpha * 0.28f * t;

					spriteBatch.Draw(
						pixel,
						mid,
						null,
						ink * alpha,
						(b - a).ToRotation(),
						new Vector2(0.5f, 0.5f),
						new Vector2(length, width),
						SpriteEffects.None,
						0f);
				}
			}
		}
	}

	/// <summary>
	/// 本模组弹幕的拖尾。用 <c>projectile.ModProjectile.Mod</c> 判断归属，
	/// 不硬编码任何弹幕类型，所以以后新增弹幕自动就有拖尾。
	/// </summary>
	public class WastelandTrailGlobalProjectile : GlobalProjectile
	{
		private const int MaxPoints = 16;
		private const float MaxWidth = 16f;
		private const float MaxAlpha = 0.55f;

		private readonly List<Vector2> trail = new List<Vector2>();

		/// <summary>每个弹幕一份历史记录。</summary>
		public override bool InstancePerEntity => true;

		private static bool IsOurs(Projectile projectile)
		{
			return projectile.ModProjectile != null;
		}

		public override bool PreAI(Projectile projectile)
		{
			if (Main.dedServ || !IsOurs(projectile)) {
				return true;
			}

			trail.Add(projectile.Center);

			while (trail.Count > MaxPoints) {
				trail.RemoveAt(0);
			}

			return true;
		}

		public override void PostDraw(Projectile projectile, Color lightColor)
		{
			if (Main.dedServ || trail.Count < 2 || !IsOurs(projectile)) {
				return;
			}

			if (projectile.hide) {
				return;
			}

			WastelandTrailRenderer.Draw(Main.spriteBatch, trail, TrailColor(projectile), MaxWidth, MaxAlpha);
		}

		private static Color TrailColor(Projectile projectile)
		{
			if (!projectile.hostile) {
				return WastelandEffectColors.For(projectile.DamageType);
			}

			string space = projectile.ModProjectile.GetType().Namespace ?? string.Empty;

			if (space.Contains("AshHeart")) {
				return new Color(255, 128, 48);
			}

			if (space.Contains("Fireplace")) {
				return new Color(160, 214, 255);
			}

			if (space.Contains("Archivist")) {
				return new Color(176, 196, 255);
			}

			return new Color(168, 176, 168);
		}
	}

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
		public override void PostUpdateDusts()
		{
			WastelandFxSystem.Update();
		}

		public override void PostDrawTiles()
		{
			if (Main.dedServ) {
				return;
			}

			bool swinging = false;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];

				if (!player.active || player.dead) {
					continue;
				}

				WastelandSwingTracker tracker = player.GetModPlayer<WastelandSwingTracker>();

				if (tracker.Swinging && tracker.SwingPoints.Count >= 2) {
					swinging = true;
					break;
				}
			}

			if (swinging) {
				Main.spriteBatch.Begin(
					SpriteSortMode.Deferred,
					BlendState.Additive,
					Main.DefaultSamplerState,
					DepthStencilState.None,
					Main.Rasterizer,
					null,
					Main.GameViewMatrix.TransformationMatrix);

				for (int i = 0; i < Main.maxPlayers; i++) {
					Player player = Main.player[i];

					if (!player.active || player.dead) {
						continue;
					}

					WastelandSwingTracker tracker = player.GetModPlayer<WastelandSwingTracker>();

					if (!tracker.Swinging || tracker.SwingPoints.Count < 2) {
						continue;
					}

					WastelandTrailRenderer.Draw(Main.spriteBatch, tracker.SwingPoints, tracker.SwingColor, 28f, 0.85f);
					Vector2 tip = tracker.SwingPoints[tracker.SwingPoints.Count - 1];
					WastelandFxSystem.Glow(tip, tracker.SwingColor, 1.3f, 5);
				}

				Main.spriteBatch.End();
			}

			WastelandFxSystem.Draw();
		}
	}
}
