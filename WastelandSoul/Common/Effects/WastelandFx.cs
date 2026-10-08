using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace WastelandSoul.Common.Effects
{
	/// <summary>
	/// 客户端粒子。不参与伤害，也不走弹幕同步。
	/// <para/>画法对齐常见的泰拉瑞亚粒子：加法光和透明烟分开画、寿命用曲线而不是匀速淡出、
	/// 火花沿速度拉成一条、冲击波用一整张软环、闪电是端点固定的折线（外层宽、芯更亮）。
	/// 贴图全部运行时生成，不新增资源，也不引用别的模组。
	/// <para/>⚠️ 贴图必须在主线程懒加载，见 <see cref="EnsureTextures"/>。
	/// </summary>
	public class WastelandFxSystem : ModSystem
	{
		private const int Capacity = 1024;
		private const int BoltCapacity = 32;
		private const int BoltPoints = 12;

		private const byte KindGlow = 0;
		private const byte KindSpark = 1;
		private const byte KindRing = 2;
		private const byte KindSmoke = 3;
		private const byte KindEmber = 4;
		private const byte KindMote = 5;
		private const byte KindFlake = 6;
		private const byte KindLine = 7;

		private const byte BlendAdd = 0;
		private const byte BlendAlpha = 1;

		private const float RingTexRadius = 46f;

		private static readonly Particle[] Particles = new Particle[Capacity];
		private static readonly int[] Free = new int[Capacity];
		private static readonly Bolt[] Bolts = new Bolt[BoltCapacity];
		private static readonly Vector2[,] BoltPath = new Vector2[BoltCapacity, BoltPoints];

		private static int freeCount;

		private static Texture2D glow;
		private static Texture2D ringTex;
		private static Texture2D streak;
		private static Texture2D puff;

		static WastelandFxSystem()
		{
			ResetPool();
		}

		/// <summary>
		/// ⚠️ **不要**在 <c>Load()</c> 里创建贴图。
		/// <para/>tModLoader 的模组加载跑在线程池上，FNA3D 的图形调用只能在主线程执行。
		/// 在 <c>Load()</c> 里 <c>new Texture2D</c> 会直接把整个模组禁用，而且服务端加载自检抓不到。
		/// 第一次绘制（<c>PostDrawTiles</c>，主线程）时才建。
		/// </summary>
		private static bool EnsureTextures()
		{
			if (glow != null && ringTex != null && streak != null && puff != null) {
				return true;
			}

			if (Main.dedServ || Main.instance?.GraphicsDevice == null) {
				return false;
			}

			glow ??= CreateGlow();
			ringTex ??= CreateRing();
			streak ??= CreateStreak();
			puff ??= CreatePuff();
			return glow != null && ringTex != null && streak != null && puff != null;
		}

		public override void Unload()
		{
			// 卸载也在加载线程上：直接 Dispose 会再触发一次 "must be called on the main thread"。
			Texture2D glowTexture = glow;
			Texture2D ringTexture = ringTex;
			Texture2D streakTexture = streak;
			Texture2D puffTexture = puff;
			glow = null;
			ringTex = null;
			streak = null;
			puff = null;
			ResetPool();

			if (glowTexture == null && ringTexture == null && streakTexture == null && puffTexture == null) {
				return;
			}

			Main.RunOnMainThread(() => {
				glowTexture?.Dispose();
				ringTexture?.Dispose();
				streakTexture?.Dispose();
				puffTexture?.Dispose();
			});
		}

		public override void PostUpdateDusts()
		{
			Update();
		}

		/// <summary>
		/// 粒子只在这里推进和绘制。不要在这里画刀光或给所有弹幕拉线——
		/// 那些会挡视野，最新代码里已经拿掉，见 <c>WastelandEffectLayer</c> 顶部说明。
		/// </summary>
		public override void PostDrawTiles()
		{
			if (Main.dedServ) {
				return;
			}

			try {
				Draw();
			}
			catch (Exception exception) {
				Mod.Logger.Warn("WastelandFxSystem: 画粒子失败（已忽略，不影响游戏）", exception);
			}
		}

		public static void Update()
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < Particles.Length; i++) {
				if (Particles[i].Active) {
					Particles[i].Step(i);
				}
			}

			for (int i = 0; i < Bolts.Length; i++) {
				if (!Bolts[i].Active) {
					continue;
				}

				Bolts[i].Life -= 1f;

				if (Bolts[i].Life <= 0f) {
					Bolts[i].Active = false;
				}
			}
		}

		public static void Draw()
		{
			if (Main.dedServ || !HasWork() || !EnsureTextures()) {
				return;
			}

			SpriteBatch batch = Main.spriteBatch;
			bool alpha = false;
			bool additive = false;

			for (int i = 0; i < Particles.Length; i++) {
				if (!Particles[i].Active) {
					continue;
				}

				if (Particles[i].Blend == BlendAlpha) {
					alpha = true;
				}
				else {
					additive = true;
				}
			}

			for (int i = 0; i < Bolts.Length; i++) {
				if (Bolts[i].Active) {
					additive = true;
					break;
				}
			}

			if (alpha) {
				batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp,
					DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

				try {
					DrawKind(KindSmoke);
					DrawKind(KindFlake);
				}
				finally {
					batch.End();
				}
			}

			if (additive) {
				batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
					DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

				try {
					DrawKind(KindGlow);
					DrawKind(KindEmber);
					DrawKind(KindMote);
					DrawKind(KindSpark);
					DrawKind(KindRing);
					DrawKind(KindLine);
					DrawBolts();
				}
				finally {
					batch.End();
				}
			}
		}

		public static void Glow(Vector2 position, Color color, float scale, int life)
		{
			Spawn(KindGlow, BlendAdd, position, Vector2.Zero, color, color, scale, scale * 0.55f, life, 0f, 1f, 0f, 0f, 0f);
		}

		public static void Spark(Vector2 position, Vector2 velocity, Color color, float scale, int life, float gravity)
		{
			Spawn(KindSpark, BlendAdd, position, velocity, color, Color.Lerp(color, Color.White, 0.35f),
				scale, scale * 0.35f, life, gravity, 0.96f, velocity.ToRotation(), 0f, 0f);
		}

		public static void Burst(Vector2 position, int count, Color color, float speed)
		{
			if (Main.dedServ) {
				return;
			}

			Flash(position, color, 1.15f);
			Ring(position, color, 14f, 28f + speed * 12f, 16);
			count = Math.Clamp(count, 0, 24);

			for (int i = 0; i < count; i++) {
				float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.1f, 0.1f);
				Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(speed * 0.45f, speed);
				Spark(position, velocity, Color.Lerp(color, Color.White, Main.rand.NextFloat(0.15f, 0.55f)),
					Main.rand.NextFloat(0.65f, 1.2f), Main.rand.Next(14, 26), 0.035f);
			}

			if (count >= 6) {
				Lines(position, count / 3, color, 12f + speed * 2.2f);
			}
		}

		/// <summary>命中或阶段切换：闪白、两圈冲击波、辐射火花和几道短光线。</summary>
		public static void Impact(Vector2 position, Color color, float power)
		{
			if (Main.dedServ) {
				return;
			}

			power = Math.Clamp(power, 0.35f, 2.2f);
			Flash(position, color, 1.2f * power);
			Ring(position, color, 10f * power, 48f + 26f * power, 18);
			Ring(position, Color.Lerp(color, Color.White, 0.5f), 6f, 26f * power, 11);

			int sparks = Math.Clamp((int)(7f * power), 4, 16);

			for (int i = 0; i < sparks; i++) {
				float angle = MathHelper.TwoPi * i / sparks + Main.rand.NextFloat(-0.14f, 0.14f);
				Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(1.6f, 4.4f) * power;
				Spark(position, velocity, Color.Lerp(color, Color.White, Main.rand.NextFloat(0.2f, 0.7f)),
					Main.rand.NextFloat(0.7f, 1.25f), Main.rand.Next(12, 22), 0.03f);
			}

			Lines(position, Math.Clamp((int)(5f * power), 3, 10), Color.Lerp(color, Color.White, 0.35f), 14f + 12f * power);
		}

		/// <summary>
		/// 各武器线自己的短爆发。0 破碎，1 光暗，2 余烬，3 冷炉，其余是废料火花。
		/// 一层亮核、一层主体、一圈晕、外围碎屑，避免整段弹道都一样亮。
		/// </summary>
		public static void StyleStrike(int theme, Vector2 position, Vector2 direction)
		{
			if (Main.dedServ) {
				return;
			}

			Vector2 forward = direction.LengthSquared() > 0.25f ? Vector2.Normalize(direction) : Main.rand.NextVector2Unit();

			switch (theme) {
				case 0:
					Glow(position, new Color(255, 220, 160), 0.55f, 8);
					Spark(position, forward * 2.4f, new Color(255, 120, 40), 0.75f, 12, 0.05f);
					Lines(position, 3, new Color(160, 120, 80), 18f);
					Smoke(position, new Vector2(0f, -0.45f), new Color(72, 64, 58), 0.45f, 16);
					break;
				case 1:
					Flash(position, new Color(232, 240, 255), 0.65f);
					Flakes(position, 3, new Color(226, 232, 242));
					Ring(position, new Color(70, 90, 180), 6f, 30f, 12);
					Spark(position, -forward * 1.2f, new Color(40, 50, 90), 0.4f, 10, 0f);
					break;
				case 2:
					Glow(position, new Color(255, 244, 220), 0.6f, 8);
					Embers(position, 3, new Color(255, 110, 30));
					Ring(position, new Color(255, 80, 20), 8f, 34f, 12);
					break;
				case 3:
					Glow(position, new Color(230, 250, 255), 0.5f, 8);
					Ring(position, new Color(140, 210, 255), 6f, 28f, 12);
					Motes(position, 18f, 2, new Color(180, 230, 255));
					Spark(position, forward * 1.6f, new Color(190, 240, 255), 0.5f, 10, 0f);
					break;
				default:
					Glow(position, new Color(255, 210, 140), 0.45f, 8);
					Spark(position, forward * 2f, new Color(200, 150, 80), 0.6f, 11, 0.04f);
					Lines(position, 2, new Color(170, 140, 90), 14f);
					break;
			}
		}

		public static void Embers(Vector2 position, int count, Color color)
		{
			if (Main.dedServ || count <= 0) {
				return;
			}

			count = Math.Clamp(count, 1, 32);

			for (int i = 0; i < count; i++) {
				Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.15f, 1.15f), Main.rand.NextFloat(-2.6f, -0.35f));
				Ember(position, velocity, color, Main.rand.NextFloat(0.55f, 1.15f), Main.rand.Next(22, 42));
			}
		}

		public static void Ember(Vector2 position, Vector2 velocity, Color color, float scale, int life)
		{
			Spawn(KindEmber, BlendAdd, position, velocity, color, Darken(color),
				scale, scale * 0.2f, life, -0.012f, 0.986f, 0f, 0f, Main.rand.NextFloat(MathHelper.TwoPi));
		}

		public static void Motes(Vector2 center, float radius, int count, Color color)
		{
			if (Main.dedServ || count <= 0 || radius < 4f) {
				return;
			}

			count = Math.Clamp(count, 1, 24);

			for (int i = 0; i < count; i++) {
				float angle = MathHelper.TwoPi * i / count + Main.GameUpdateCount * 0.02f + Main.rand.NextFloat(-0.2f, 0.2f);
				float speed = Main.rand.NextFloat(0.85f, 1.35f);
				Vector2 position = center + angle.ToRotationVector2() * radius;
				Vector2 velocity = (angle + MathHelper.PiOver2).ToRotationVector2() * speed;
				Spawn(KindMote, BlendAdd, position, velocity, color, Color.Lerp(color, Color.White, 0.45f),
					Main.rand.NextFloat(0.45f, 0.8f), 0.2f, Main.rand.Next(20, 32), 0f, 1f, 0f, 0f, speed / radius);
			}
		}

		public static void Smoke(Vector2 position, Vector2 velocity, Color color, float scale, int life)
		{
			Spawn(KindSmoke, BlendAlpha, position, velocity, color, Color.Lerp(color, Color.Black, 0.45f),
				scale, scale * 1.9f, life, -0.008f, 0.94f,
				Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(-0.03f, 0.03f), 0f);
		}

		public static void Flakes(Vector2 position, int count, Color color)
		{
			if (Main.dedServ || count <= 0) {
				return;
			}

			count = Math.Clamp(count, 1, 24);

			for (int i = 0; i < count; i++) {
				Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.7f, 0.35f));
				Spawn(KindFlake, BlendAlpha, position + Main.rand.NextVector2Circular(8f, 8f), velocity,
					color, Color.Lerp(color, Color.White, 0.25f),
					Main.rand.NextFloat(0.7f, 1.25f), 0.35f, Main.rand.Next(24, 42), 0.02f, 0.99f,
					Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(-0.22f, 0.22f), Main.rand.NextFloat(MathHelper.TwoPi));
			}
		}

		public static void Lines(Vector2 position, int count, Color color, float length)
		{
			if (Main.dedServ || count <= 0) {
				return;
			}

			count = Math.Clamp(count, 1, 16);

			for (int i = 0; i < count; i++) {
				float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.12f, 0.12f);
				Spawn(KindLine, BlendAdd, position, Vector2.Zero, color, Color.Lerp(color, Color.White, 0.4f),
					length, length * 0.15f, Main.rand.Next(8, 13), 0f, 1f, angle, 0f, Main.rand.NextFloat(1.6f, 2.6f));
			}
		}

		/// <summary>沿一条直线撒几颗火花。给激光、扫击这种本来就有形状的招用，不再铺一整道闪电。</summary>
		public static void Along(Vector2 from, Vector2 to, Color color, int count)
		{
			if (Main.dedServ || count <= 0) {
				return;
			}

			Vector2 delta = to - from;
			float lengthSquared = delta.LengthSquared();

			if (lengthSquared < 16f) {
				return;
			}

			count = Math.Clamp(count, 1, 12);
			Vector2 direction = delta / MathF.Sqrt(lengthSquared);
			Vector2 side = new Vector2(-direction.Y, direction.X);

			for (int i = 0; i < count; i++) {
				float along = (i + Main.rand.NextFloat(0.2f, 0.8f)) / count;
				Vector2 position = from + delta * along + side * Main.rand.NextFloat(-7f, 7f);
				Spark(position, -direction * Main.rand.NextFloat(0.3f, 1.5f),
					Color.Lerp(color, Color.White, Main.rand.NextFloat(0.2f, 0.6f)),
					Main.rand.NextFloat(0.4f, 0.85f), Main.rand.Next(8, 14), 0f);
			}
		}

		public static void Flash(Vector2 position, Color color, float scale)
		{
			Spawn(KindGlow, BlendAdd, position, Vector2.Zero, color, color, scale * 2.05f, scale * 0.15f, 11, 0f, 1f, 0f, 0f, 0f);
			Spawn(KindGlow, BlendAdd, position, Vector2.Zero, Color.Lerp(color, Color.White, 0.72f), Color.White,
				scale * 0.85f, scale * 0.05f, 7, 0f, 1f, 0f, 0f, 0f);
		}

		public static void Ring(Vector2 position, Color color, float from, float to, int life)
		{
			Spawn(KindRing, BlendAdd, position, Vector2.Zero, color, Color.Lerp(color, Color.White, 0.25f), from, to, life, 0f, 1f, 0f, 0f, 0f);
		}

		public static void Bolt(Vector2 from, Vector2 to, Color color)
		{
			if (Main.dedServ) {
				return;
			}

			Vector2 delta = to - from;
			float length = delta.Length();

			if (length < 8f) {
				return;
			}

			SpawnBolt(from, to, color, 2.15f, 11);

			if (length > 90f && Main.rand.NextBool(2)) {
				Vector2 direction = delta / length;
				Vector2 side = new Vector2(-direction.Y, direction.X);
				float along = Main.rand.NextFloat(0.35f, 0.72f);
				Vector2 mid = from + delta * along;
				float sign = Main.rand.NextBool() ? 1f : -1f;
				Vector2 end = mid + side * (Main.rand.NextFloat(28f, 76f) * sign) + direction * Main.rand.NextFloat(-16f, 34f);
				SpawnBolt(mid, end, color, 1.15f, 8);
			}
		}

		private static void Spawn(byte kind, byte blend, Vector2 position, Vector2 velocity, Color color, Color fadeColor,
			float scale, float scaleTo, int life, float gravity, float drag, float rotation, float spin, float extra)
		{
			if (Main.dedServ || life <= 0) {
				return;
			}

			int slot = TakeSlot();

			Particles[slot] = new Particle {
				Active = true,
				Kind = kind,
				Blend = blend,
				Position = position,
				Velocity = velocity,
				Life = life,
				MaxLife = life,
				Scale = scale,
				ScaleTo = scaleTo,
				Rotation = rotation,
				Spin = spin,
				Gravity = gravity,
				Drag = drag,
				Extra = extra,
				Color = color,
				FadeColor = fadeColor
			};
		}

		private static void SpawnBolt(Vector2 from, Vector2 to, Color color, float width, int life)
		{
			Vector2 delta = to - from;
			float length = delta.Length();

			if (length < 8f || life <= 0) {
				return;
			}

			int slot = 0;
			float oldest = float.MaxValue;
			bool found = false;

			for (int i = 0; i < Bolts.Length; i++) {
				if (!Bolts[i].Active) {
					slot = i;
					found = true;
					break;
				}

				if (Bolts[i].Life < oldest) {
					oldest = Bolts[i].Life;
					slot = i;
				}
			}

			if (!found && oldest == float.MaxValue) {
				return;
			}

			Vector2 direction = delta / length;
			Vector2 side = new Vector2(-direction.Y, direction.X);
			int count = Math.Clamp((int)(length / 30f) + 1, 4, BoltPoints);
			float jag = Math.Clamp(length * 0.075f, 6f, 18f);

			BoltPath[slot, 0] = from;
			BoltPath[slot, count - 1] = to;

			for (int i = 1; i < count - 1; i++) {
				float along = i / (float)(count - 1);
				float falloff = MathF.Sin(along * MathF.PI);
				BoltPath[slot, i] = from + direction * (length * along) + side * (Main.rand.NextFloat(-jag, jag) * falloff);
			}

			for (int i = 1; i < count - 1; i++) {
				BoltPath[slot, i] += side * Main.rand.NextFloat(-jag * 0.28f, jag * 0.28f);
			}

			BoltPath[slot, 0] = from;
			BoltPath[slot, count - 1] = to;

			Bolts[slot] = new Bolt {
				Active = true,
				Life = life,
				MaxLife = life,
				Width = width,
				Color = color,
				Count = count
			};
		}

		private static int TakeSlot()
		{
			if (freeCount > 0) {
				return Free[--freeCount];
			}

			int slot = 0;
			float oldest = float.MaxValue;

			for (int i = 0; i < Particles.Length; i++) {
				if (Particles[i].Life < oldest) {
					oldest = Particles[i].Life;
					slot = i;
				}
			}

			return slot;
		}

		private static void Kill(int index)
		{
			if ((uint)index >= Capacity || !Particles[index].Active) {
				return;
			}

			Particles[index].Active = false;

			if (freeCount < Capacity) {
				Free[freeCount++] = index;
			}
		}

		private static void ResetPool()
		{
			freeCount = 0;

			for (int i = 0; i < Particles.Length; i++) {
				Particles[i] = default;
				Free[freeCount++] = i;
			}

			for (int i = 0; i < Bolts.Length; i++) {
				Bolts[i].Active = false;
			}
		}

		private static bool HasWork()
		{
			for (int i = 0; i < Particles.Length; i++) {
				if (Particles[i].Active) {
					return true;
				}
			}

			for (int i = 0; i < Bolts.Length; i++) {
				if (Bolts[i].Active) {
					return true;
				}
			}

			return false;
		}

		private static void DrawKind(byte kind)
		{
			for (int i = 0; i < Particles.Length; i++) {
				if (Particles[i].Active && Particles[i].Kind == kind) {
					Particles[i].Draw();
				}
			}
		}

		private static void DrawBolts()
		{
			Texture2D pixel = TextureAssets.MagicPixel.Value;

			for (int slot = 0; slot < Bolts.Length; slot++) {
				if (!Bolts[slot].Active || Bolts[slot].Count < 2) {
					continue;
				}

				bool visible = false;

				for (int i = 0; i < Bolts[slot].Count; i++) {
					if (OnScreen(BoltPath[slot, i], 64f)) {
						visible = true;
						break;
					}
				}

				if (!visible) {
					continue;
				}

				float fade = MathF.Pow(Bolts[slot].Life / Bolts[slot].MaxLife, 0.8f);
				float width = Bolts[slot].Width * (0.82f + 0.18f * ((int)Bolts[slot].Life % 2 == 0 ? 1f : 0.62f));
				Color color = AdditiveInk(Bolts[slot].Color, fade * 0.5f);
				Color core = AdditiveInk(Color.Lerp(Bolts[slot].Color, Color.White, 0.72f), fade);

				for (int i = 1; i < Bolts[slot].Count; i++) {
					DrawSegment(pixel, BoltPath[slot, i - 1], BoltPath[slot, i], color, width * 3.1f);
					DrawSegment(pixel, BoltPath[slot, i - 1], BoltPath[slot, i], core, MathF.Max(1.15f, width * 0.8f));
				}

				for (int i = 0; i < Bolts[slot].Count; i++) {
					Main.spriteBatch.Draw(glow, BoltPath[slot, i] - Main.screenPosition, null, color,
						0f, new Vector2(32f, 32f), 0.14f * width, SpriteEffects.None, 0f);
				}
			}
		}

		private static void DrawSegment(Texture2D pixel, Vector2 from, Vector2 to, Color color, float width)
		{
			Vector2 delta = to - from;
			float length = delta.Length();

			if (length < 0.5f || width < 0.4f) {
				return;
			}

			Main.spriteBatch.Draw(pixel, (from + to) * 0.5f - Main.screenPosition, null, color, delta.ToRotation(),
				new Vector2(0.5f, 0.5f), new Vector2(length + width * 0.35f, width), SpriteEffects.None, 0f);
		}

		private static bool OnScreen(Vector2 world, float margin)
		{
			Vector2 screen = world - Main.screenPosition;
			return screen.X >= -margin && screen.Y >= -margin
				&& screen.X <= Main.screenWidth + margin && screen.Y <= Main.screenHeight + margin;
		}

		private static Color Darken(Color color)
		{
			return new Color(color.R / 4, color.G / 5, color.B / 5);
		}

		private static Color AdditiveInk(Color color, float fade)
		{
			// 加法混合只压低 alpha。RGB 再乘一次的话，光斑会按淡出的平方变暗。
			float alpha = Math.Clamp(fade * (color.A / 255f), 0f, 1f);
			return new Color(color.R, color.G, color.B, (int)(alpha * 255f));
		}

		private static Color AlphaInk(Color color, float fade)
		{
			// 透明混合不要先把 RGB 乘进 alpha，否则烟和纸屑会发灰。
			float alpha = Math.Clamp(fade * (color.A / 255f), 0f, 1f);
			return new Color(color.R, color.G, color.B, (int)(alpha * 255f));
		}

		private static Texture2D CreateGlow()
		{
			const int size = 64;
			Color[] pixels = new Color[size * size];
			Vector2 center = new Vector2(31.5f, 31.5f);

			for (int y = 0; y < size; y++) {
				for (int x = 0; x < size; x++) {
					float distance = Vector2.Distance(new Vector2(x, y), center) / 32f;
					float alpha = MathF.Exp(-distance * distance * 3.4f);
					byte a = (byte)(Math.Clamp(alpha, 0f, 1f) * 255f);
					pixels[y * size + x] = new Color(a, a, a, a);
				}
			}

			return Upload(size, size, pixels);
		}

		private static Texture2D CreateRing()
		{
			const int size = 128;
			Color[] pixels = new Color[size * size];
			Vector2 center = new Vector2(63.5f, 63.5f);

			for (int y = 0; y < size; y++) {
				for (int x = 0; x < size; x++) {
					float distance = Vector2.Distance(new Vector2(x, y), center);
					float ridge = MathF.Exp(-MathF.Pow((distance - RingTexRadius) / 3.6f, 2f));
					float halo = MathF.Exp(-MathF.Pow((distance - RingTexRadius) / 9.5f, 2f)) * 0.42f;
					byte a = (byte)(Math.Clamp(ridge + halo, 0f, 1f) * 255f);
					pixels[y * size + x] = new Color(a, a, a, a);
				}
			}

			return Upload(size, size, pixels);
		}

		private static Texture2D CreateStreak()
		{
			const int width = 64;
			const int height = 16;
			Color[] pixels = new Color[width * height];

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					float along = (x + 0.5f) / width - 0.5f;
					float across = (y + 0.5f) / height - 0.5f;
					float alpha = MathF.Exp(-MathF.Pow(along * 2.35f, 2f)) * MathF.Exp(-MathF.Pow(across * 2.7f, 2f));
					byte a = (byte)(Math.Clamp(alpha, 0f, 1f) * 255f);
					pixels[y * width + x] = new Color(a, a, a, a);
				}
			}

			return Upload(width, height, pixels);
		}

		private static Texture2D CreatePuff()
		{
			const int size = 64;
			Color[] pixels = new Color[size * size];

			for (int y = 0; y < size; y++) {
				for (int x = 0; x < size; x++) {
					float nx = (x - 31.5f) / 32f;
					float ny = (y - 31.5f) / 32f;
					float warp = 0.84f + 0.16f * MathF.Sin(nx * 8.5f + ny * 6.5f);
					float alpha = MathF.Exp(-(nx * nx + ny * ny) * 3.2f * warp);
					byte a = (byte)(Math.Clamp(alpha, 0f, 1f) * 255f);
					pixels[y * size + x] = new Color(255, 255, 255, a);
				}
			}

			return Upload(size, size, pixels);
		}

		private static Texture2D Upload(int width, int height, Color[] pixels)
		{
			Texture2D texture = new Texture2D(Main.instance.GraphicsDevice, width, height);
			texture.SetData(pixels);
			return texture;
		}

		private struct Bolt
		{
			public bool Active;
			public float Life;
			public float MaxLife;
			public float Width;
			public int Count;
			public Color Color;
		}

		private struct Particle
		{
			public bool Active;
			public byte Kind;
			public byte Blend;
			public Vector2 Position;
			public Vector2 Velocity;
			public float Life;
			public float MaxLife;
			public float Scale;
			public float ScaleTo;
			public float Rotation;
			public float Spin;
			public float Gravity;
			public float Drag;
			public float Extra;
			public Color Color;
			public Color FadeColor;

			public void Step(int index)
			{
				Life -= 1f;

				if (Life <= 0f) {
					Kill(index);
					return;
				}

				if (Kind != KindRing && Kind != KindLine) {
					Position += Velocity;
					Velocity *= Drag;
					Velocity.Y += Gravity;
				}

				if (Kind == KindEmber) {
					Velocity.X += MathF.Sin(Life * 0.33f + Extra) * 0.05f;
				}
				else if (Kind == KindMote && Extra != 0f) {
					Velocity = Velocity.RotatedBy(Extra);
				}
				else if (Kind == KindFlake) {
					Velocity.X += MathF.Sin(Life * 0.22f + Extra) * 0.035f;
				}

				Rotation += Spin;
			}

			public void Draw()
			{
				if (MaxLife <= 0f) {
					return;
				}

				float lifeRatio = Life / MaxLife;
				float age = 1f - lifeRatio;
				float margin = Kind == KindRing ? Math.Abs(MathHelper.Lerp(Scale, ScaleTo, age)) + 48f : 120f;

				if (!OnScreen(Position, margin)) {
					return;
				}

				Vector2 screen = Position - Main.screenPosition;

				switch (Kind) {
					case KindRing:
						DrawRing(screen, lifeRatio, age);
						break;
					case KindSpark:
						DrawSpark(screen, lifeRatio);
						break;
					case KindSmoke:
						DrawSmoke(screen, lifeRatio, age);
						break;
					case KindEmber:
						DrawEmber(screen, lifeRatio, age);
						break;
					case KindMote:
						DrawMote(screen, lifeRatio);
						break;
					case KindFlake:
						DrawFlake(screen, lifeRatio, age);
						break;
					case KindLine:
						DrawLine(screen, lifeRatio, age);
						break;
					default:
						DrawGlow(screen, lifeRatio, age);
						break;
				}
			}

			private void DrawGlow(Vector2 screen, float lifeRatio, float age)
			{
				float fade = MathF.Pow(lifeRatio, 0.72f);
				float scale = MathHelper.Lerp(Scale, ScaleTo, age);
				Main.spriteBatch.Draw(glow, screen, null, AdditiveInk(Color, fade * 0.5f), 0f, new Vector2(32f, 32f), 0.58f * scale, SpriteEffects.None, 0f);
				Main.spriteBatch.Draw(glow, screen, null, AdditiveInk(Color.Lerp(Color, Color.White, 0.45f), fade),
					0f, new Vector2(32f, 32f), 0.26f * scale, SpriteEffects.None, 0f);
			}

			private void DrawSpark(Vector2 screen, float lifeRatio)
			{
				float fade = MathF.Pow(lifeRatio, 0.55f);
				float speed = Velocity.Length();
				Color ink = AdditiveInk(Color.Lerp(FadeColor, Color, fade), fade);

				if (speed > 0.35f) {
					float length = Math.Clamp(9f + speed * 4.6f, 10f, 44f) * Math.Max(0.45f, Scale);
					float thick = Math.Clamp(6.2f * Scale, 2.2f, 13f);
					float rotation = Velocity.ToRotation();
					Vector2 origin = new Vector2(32f, 8f);
					Main.spriteBatch.Draw(streak, screen, null, AdditiveInk(Color, fade * 0.8f), rotation, origin,
						new Vector2(length / 64f, thick / 16f), SpriteEffects.None, 0f);
					Main.spriteBatch.Draw(streak, screen, null, AdditiveInk(Color.Lerp(Color, Color.White, 0.62f), fade),
						rotation, origin, new Vector2(length / 64f * 0.7f, thick / 16f * 0.32f), SpriteEffects.None, 0f);
					Vector2 head = screen + Velocity / speed * (length * 0.28f);
					Main.spriteBatch.Draw(glow, head, null, ink, 0f, new Vector2(32f, 32f), 0.16f * Scale, SpriteEffects.None, 0f);
					return;
				}

				Main.spriteBatch.Draw(glow, screen, null, ink, 0f, new Vector2(32f, 32f), 0.3f * Scale, SpriteEffects.None, 0f);
			}

			private void DrawRing(Vector2 screen, float lifeRatio, float age)
			{
				float fade = MathF.Pow(lifeRatio, 1.15f);
				float radius = MathHelper.Lerp(Scale, ScaleTo, age);
				float spriteScale = Math.Max(0.02f, radius / RingTexRadius);
				Vector2 origin = new Vector2(64f, 64f);
				Main.spriteBatch.Draw(ringTex, screen, null, AdditiveInk(Color, fade * 0.45f), 0f, origin, spriteScale * 1.16f, SpriteEffects.None, 0f);
				Main.spriteBatch.Draw(ringTex, screen, null, AdditiveInk(Color.Lerp(Color, Color.White, 0.4f), fade),
					0f, origin, spriteScale, SpriteEffects.None, 0f);
			}

			private void DrawSmoke(Vector2 screen, float lifeRatio, float age)
			{
				float fade = age < 0.18f ? age / 0.18f : lifeRatio / 0.82f;
				float scale = MathHelper.Lerp(Scale, ScaleTo, age);
				Color ink = AlphaInk(Color.Lerp(FadeColor, Color, fade), fade * 0.72f);
				Main.spriteBatch.Draw(puff, screen, null, ink, Rotation, new Vector2(32f, 32f), 0.55f * scale, SpriteEffects.None, 0f);
			}

			private void DrawEmber(Vector2 screen, float lifeRatio, float age)
			{
				float fade = MathF.Pow(lifeRatio, 0.6f);
				float flicker = 0.5f + 0.5f * MathF.Abs(MathF.Sin(Life * 0.85f + Extra));
				float scale = MathHelper.Lerp(Scale, ScaleTo, age);
				Main.spriteBatch.Draw(glow, screen, null, AdditiveInk(Color, fade * flicker * 0.5f), 0f, new Vector2(32f, 32f), 0.42f * scale, SpriteEffects.None, 0f);
				Main.spriteBatch.Draw(glow, screen, null, AdditiveInk(Color.Lerp(Color, Color.White, 0.35f), fade * flicker),
					0f, new Vector2(32f, 32f), 0.16f * scale, SpriteEffects.None, 0f);
			}

			private void DrawMote(Vector2 screen, float lifeRatio)
			{
				float fade = MathF.Pow(lifeRatio, 0.5f);
				float twinkle = 0.62f + 0.38f * MathF.Sin(Life * 0.55f + Extra * 40f);
				float scale = MathHelper.Lerp(ScaleTo, Scale, fade);
				Color ink = AdditiveInk(Color, fade * twinkle);
				Main.spriteBatch.Draw(glow, screen, null, ink, 0f, new Vector2(32f, 32f), 0.24f * scale, SpriteEffects.None, 0f);
			}

			private void DrawFlake(Vector2 screen, float lifeRatio, float age)
			{
				float fade = age < 0.12f ? age / 0.12f : lifeRatio / 0.88f;
				float scale = MathHelper.Lerp(Scale, ScaleTo, age);
				Color ink = AlphaInk(Color.Lerp(FadeColor, Color, fade), fade);
				Texture2D pixel = TextureAssets.MagicPixel.Value;
				Main.spriteBatch.Draw(pixel, screen, null, ink, Rotation, new Vector2(0.5f, 0.5f),
					new Vector2(8.5f * scale, 2.6f * scale), SpriteEffects.None, 0f);
			}

			private void DrawLine(Vector2 screen, float lifeRatio, float age)
			{
				float fade = MathF.Pow(lifeRatio, 0.75f);
				float length = MathHelper.Lerp(Scale, ScaleTo, age);
				Texture2D pixel = TextureAssets.MagicPixel.Value;
				Main.spriteBatch.Draw(pixel, screen, null, AdditiveInk(Color, fade * 0.55f), Rotation, new Vector2(0f, 0.5f),
					new Vector2(length, Extra * 2.1f), SpriteEffects.None, 0f);
				Main.spriteBatch.Draw(pixel, screen, null, AdditiveInk(Color.Lerp(Color, Color.White, 0.65f), fade),
					Rotation, new Vector2(0f, 0.5f), new Vector2(length * 0.72f, Math.Max(1f, Extra * 0.45f)), SpriteEffects.None, 0f);
			}
		}
	}
}
