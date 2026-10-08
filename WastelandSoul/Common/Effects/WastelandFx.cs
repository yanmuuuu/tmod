using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace WastelandSoul.Common.Effects
{
	/// <summary>
	/// 客户端粒子。做法对齐常见的加法混合粒子：软光斑、沿速度拉长的火花、
	/// 扩散冲击环、折线闪电。不参与伤害，也不走弹幕同步。
	/// </summary>
	public class WastelandFxSystem : ModSystem
	{
		private const int Capacity = 720;

		private static readonly Particle[] Particles = new Particle[Capacity];

		private static Texture2D glow;

		/// <summary>
		/// ⚠️ **不要**在 <c>Load()</c> 里创建贴图。
		///
		/// <para/>tModLoader 的模组加载跑在**线程池工作线程**上，而 FNA3D 的图形 / 音频调用
		/// **只能在主线程**执行。第一版把 `new Texture2D(Main.instance.GraphicsDevice, 64, 64)`
		/// 写在 `Load()` 里，客户端启动时直接抛：
		/// <code>
		/// System.Threading.ThreadStateException: most FNA3D audio/graphics functions
		/// must be called on the main thread
		/// → Disabling Mod: WastelandSoul
		/// </code>
		/// 整个模组被禁用（而且**服务端加载自检抓不到**：`-server` 下 Main.dedServ 为 true，
		/// 这段本来就被跳过了）。
		///
		/// <para/>所以改成**主线程懒加载**：第一次绘制（PostDrawTiles，主线程）时才建。
		/// </summary>
		private static Texture2D EnsureGlow()
		{
			if (glow != null || Main.dedServ) {
				return glow;
			}

			glow = new Texture2D(Main.instance.GraphicsDevice, 64, 64);
			Color[] pixels = new Color[64 * 64];
			Vector2 center = new Vector2(31.5f, 31.5f);

			for (int y = 0; y < 64; y++) {
				for (int x = 0; x < 64; x++) {
					float distance = Vector2.Distance(new Vector2(x, y), center) / 32f;
					float alpha = System.MathF.Exp(-distance * distance * 4.2f);
					byte a = (byte)(System.Math.Clamp(alpha, 0f, 1f) * 255f);
					pixels[y * 64 + x] = new Color(a, a, a, a);
				}
			}

			glow.SetData(pixels);
			return glow;
		}

		public override void Unload()
		{
			// 卸载同样在加载线程上跑：直接 Dispose 图形资源会再触发一次
			// "must be called on the main thread"。交给主线程去释放；
			// 主线程没空（正在退出游戏）也无所谓 —— 64x64 只有 16 KB，GC 会收。
			Texture2D texture = glow;
			glow = null;

			if (texture != null) {
				Main.RunOnMainThread(() => texture.Dispose());
			}

			for (int i = 0; i < Particles.Length; i++) {
				Particles[i].Active = false;
			}
		}

		public static void Draw()
		{
			if (Main.dedServ) {
				return;
			}

			bool any = false;

			for (int i = 0; i < Particles.Length; i++) {
				if (Particles[i].Active) {
					any = true;
					break;
				}
			}

			if (!any) {
				return;
			}

			// 贴图在这里（主线程）懒加载 —— 见 EnsureGlow 的注释：不能在 Load() 里建
			Texture2D texture = EnsureGlow();

			if (texture == null) {
				return;
			}

			SpriteBatch batch = Main.spriteBatch;
			batch.Begin(
				SpriteSortMode.Deferred,
				BlendState.Additive,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				Main.Rasterizer,
				null,
				Main.GameViewMatrix.TransformationMatrix);

			Texture2D pixel = TextureAssets.MagicPixel.Value;

			for (int i = 0; i < Particles.Length; i++) {
				if (!Particles[i].Active) {
					continue;
				}

				Particles[i].Draw(texture, pixel);
			}

			batch.End();
		}

		public static void Update()
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < Particles.Length; i++) {
				if (Particles[i].Active) {
					Particles[i].Step();
				}
			}
		}

		public static void Glow(Vector2 position, Color color, float scale, int life)
		{
			Spawn(0, position, Vector2.Zero, color, scale, scale * 0.6f, life, 0f);
		}

		public static void Spark(Vector2 position, Vector2 velocity, Color color, float scale, int life, float gravity)
		{
			Spawn(1, position, velocity, color, scale, scale * 0.2f, life, gravity);
		}

		public static void Burst(Vector2 position, int count, Color color, float speed)
		{
			if (Main.dedServ) {
				return;
			}

			Flash(position, color, 1.6f);
			Ring(position, color, 18f, 90f, 26);

			for (int i = 0; i < count; i++) {
				float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.08f, 0.08f);
				Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(speed * 0.45f, speed);
				Spark(position, velocity, Color.Lerp(color, Color.White, Main.rand.NextFloat(0.2f, 0.7f)), Main.rand.NextFloat(0.7f, 1.4f), Main.rand.Next(16, 32), 0.04f);
			}
		}

		public static void Embers(Vector2 position, int count, Color color)
		{
			for (int i = 0; i < count; i++) {
				Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.1f, 1.1f), Main.rand.NextFloat(-2.8f, -0.4f));
				Spark(position, velocity, color, Main.rand.NextFloat(0.45f, 1f), Main.rand.Next(20, 40), -0.02f);
			}
		}

		public static void Motes(Vector2 center, float radius, int count, Color color)
		{
			for (int i = 0; i < count; i++) {
				float angle = MathHelper.TwoPi * i / count + Main.GameUpdateCount * 0.02f;
				Vector2 position = center + angle.ToRotationVector2() * radius;
				Vector2 velocity = (angle + MathHelper.PiOver2).ToRotationVector2() * 1.4f;
				Spark(position, velocity, color, 0.55f, 16, 0f);
			}
		}

		public static void Flash(Vector2 position, Color color, float scale)
		{
			Spawn(0, position, Vector2.Zero, color, scale * 1.8f, scale * 0.2f, 10, 0f);
		}

		public static void Ring(Vector2 position, Color color, float from, float to, int life)
		{
			Spawn(2, position, Vector2.Zero, color, from, to, life, 0f);
		}

		public static void Bolt(Vector2 from, Vector2 to, Color color)
		{
			if (Main.dedServ) {
				return;
			}

			Vector2 point = from;
			Vector2 direction = to - from;
			float length = direction.Length();

			if (length < 8f) {
				return;
			}

			direction /= length;
			Vector2 side = new Vector2(-direction.Y, direction.X);
			int steps = System.Math.Clamp((int)(length / 36f), 4, 14);
			float step = length / steps;

			for (int i = 0; i < steps; i++) {
				Vector2 next = from + direction * step * (i + 1) + side * Main.rand.NextFloat(-14f, 14f);

				if (i == steps - 1) {
					next = to;
				}

				Vector2 mid = (point + next) * 0.5f;
				Vector2 velocity = next - point;
				Spawn(1, mid, velocity * 0.02f, color, 1.1f, 0.3f, 8, 0f);
				point = next;
			}
		}

		private static void Spawn(byte kind, Vector2 position, Vector2 velocity, Color color, float scale, float scaleTo, int life, float gravity)
		{
			if (Main.dedServ || life <= 0) {
				return;
			}

			int slot = -1;
			float oldest = float.MaxValue;

			for (int i = 0; i < Particles.Length; i++) {
				if (!Particles[i].Active) {
					slot = i;
					break;
				}

				if (Particles[i].Life < oldest) {
					oldest = Particles[i].Life;
					slot = i;
				}
			}

			if (slot < 0) {
				return;
			}

			Particles[slot] = new Particle {
				Active = true,
				Kind = kind,
				Position = position,
				Velocity = velocity,
				Life = life,
				MaxLife = life,
				Scale = scale,
				ScaleTo = scaleTo,
				Gravity = gravity,
				Color = color
			};
		}

		private struct Particle
		{
			public bool Active;
			public byte Kind;
			public Vector2 Position;
			public Vector2 Velocity;
			public float Life;
			public float MaxLife;
			public float Scale;
			public float ScaleTo;
			public float Gravity;
			public Color Color;

			public void Step()
			{
				Life -= 1f;

				if (Life <= 0f) {
					Active = false;
					return;
				}

				Position += Velocity;
				Velocity *= 0.96f;
				Velocity.Y += Gravity;
			}

			public void Draw(Texture2D glowTexture, Texture2D pixel)
			{
				float fade = Life / MaxLife;
				float scale = MathHelper.Lerp(ScaleTo, Scale, fade);
				Color ink = Color * fade;
				Vector2 screen = Position - Main.screenPosition;

				if (Kind == 2) {
					int spokes = 20;

					for (int i = 0; i < spokes; i++) {
						Vector2 spot = screen + (MathHelper.TwoPi * i / spokes).ToRotationVector2() * scale;
						Main.spriteBatch.Draw(glowTexture, spot, null, ink * 0.85f, 0f, new Vector2(32f, 32f), 0.28f, SpriteEffects.None, 0f);
					}

					return;
				}

				if (Kind == 1 && Velocity.LengthSquared() > 0.2f) {
					float length = System.Math.Clamp(Velocity.Length() * 3.2f, 8f, 36f);
					Main.spriteBatch.Draw(
						pixel,
						screen,
						null,
						ink,
						Velocity.ToRotation(),
						new Vector2(0.5f, 0.5f),
						new Vector2(length, 2.2f * scale),
						SpriteEffects.None,
						0f);
				}

				Main.spriteBatch.Draw(
					glowTexture,
					screen,
					null,
					ink,
					0f,
					new Vector2(32f, 32f),
					0.35f * scale,
					SpriteEffects.None,
					0f);
			}
		}
	}
}
