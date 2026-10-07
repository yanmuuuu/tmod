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

		public override void Load()
		{
			if (Main.dedServ) {
				return;
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
		}

		public override void Unload()
		{
			glow?.Dispose();
			glow = null;

			for (int i = 0; i < Particles.Length; i++) {
				Particles[i].Active = false;
			}
		}

		public static void Draw()
		{
			if (Main.dedServ || glow == null) {
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

				Particles[i].Draw(glow, pixel);
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

	/// <summary>本模组 Boss 周围的常驻氛围，以及弹幕拖尾、命中爆开。</summary>
	public class WastelandFxGlobal : GlobalNPC
	{
		public override void PostAI(NPC npc)
		{
			if (Main.dedServ || !npc.boss || npc.ModNPC == null || npc.ModNPC.Mod.Name != "WastelandSoul") {
				return;
			}

			string name = npc.ModNPC.Name;
			Color color = BossColor(name);

			if (Main.GameUpdateCount % 2u == 0u) {
				WastelandFxSystem.Glow(npc.Center, color * 0.45f, 2.8f, 3);
			}

			if (Main.GameUpdateCount % 6u != 0u) {
				return;
			}

			if (name == "AshHeart") {
				WastelandFxSystem.Embers(npc.Center + Main.rand.NextVector2Circular(24f, 18f), 2, color);
			}
			else if (name == "FireplaceGuardian") {
				WastelandFxSystem.Motes(npc.Center, 70f, 1, color);
			}
			else if (name == "Archivist") {
				WastelandFxSystem.Motes(npc.Center, 84f, 1, color);
			}
			else {
				WastelandFxSystem.Spark(npc.Center, Main.rand.NextVector2Circular(2f, 2f), color, 0.7f, 18, 0.05f);
			}

			if (name == "Scavenger" && Main.rand.NextBool(18)) {
				Vector2 end = npc.Center + Main.rand.NextVector2Circular(90f, 50f);
				WastelandFxSystem.Bolt(npc.Center, end, new Color(140, 220, 255));
			}
		}

		internal static Color BossColor(string name)
		{
			return name switch {
				"AshHeart" => new Color(255, 120, 40),
				"FireplaceGuardian" => new Color(150, 210, 255),
				"Archivist" => new Color(170, 196, 255),
				_ => new Color(190, 170, 140)
			};
		}
	}

	/// <summary>本模组弹幕自动拉出火花，消失时再爆一小团。</summary>
	public class WastelandFxProjectile : GlobalProjectile
	{
		public override void PostAI(Projectile projectile)
		{
			if (Main.dedServ || projectile.ModProjectile == null || projectile.ModProjectile.Mod.Name != "WastelandSoul") {
				return;
			}

			if (projectile.ModProjectile is Content.Projectiles.Vfx.WastelandSpark) {
				return;
			}

			int stride = projectile.minion || projectile.sentry ? 8 : 2;

			if (Main.GameUpdateCount % (uint)stride != 0u) {
				return;
			}

			Color color = Color.Lerp(ColorFor(projectile), Color.White, 0.35f);
			Vector2 back = projectile.velocity.LengthSquared() > 1f ? -projectile.velocity * 0.15f : Main.rand.NextVector2Circular(0.4f, 0.4f);
			WastelandFxSystem.Spark(projectile.Center, back, color, projectile.hostile ? 0.9f : 0.55f, 14, 0f);
		}

		public override void OnKill(Projectile projectile, int timeLeft)
		{
			if (Main.dedServ || projectile.ModProjectile == null || projectile.ModProjectile.Mod.Name != "WastelandSoul") {
				return;
			}

			if (projectile.ModProjectile is Content.Projectiles.Vfx.WastelandSpark) {
				return;
			}

			int count = timeLeft > 3 ? 12 : 5;
			WastelandFxSystem.Burst(projectile.Center, count, ColorFor(projectile), timeLeft > 3 ? 5.5f : 2.4f);
		}

		private static Color ColorFor(Projectile projectile)
		{
			string space = projectile.ModProjectile.GetType().Namespace ?? string.Empty;

			if (space.Contains("AshHeart")) {
				return new Color(255, 130, 48);
			}

			if (space.Contains("Fireplace")) {
				return new Color(160, 214, 255);
			}

			if (space.Contains("Archivist")) {
				return new Color(176, 200, 255);
			}

			if (!projectile.hostile) {
				return WastelandEffectColors.For(projectile.DamageType);
			}

			return new Color(180, 176, 160);
		}
	}
}
