using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 世界生成的底层笔刷。
	///
	/// <para/>⚠️ **不要写 <c>Main.tile[x, y]</c> 来改格子**（本项目在这里翻过车，两种写法都不行，别再试）：
	/// <list type="bullet">
	/// <item><c>Main.tile[x, y] = tile;</c> → <c>CS0200</c>：<c>Tilemap</c> 的索引器**只读**（没有公开 setter）；</item>
	/// <item><c>Main.tile[x, y].HasTile = true;</c> → <c>CS1612</c>：索引器返回的是 <c>Tile</c>（**结构体**）的副本，
	/// 编译器不允许改它的成员；</item>
	/// <item>把副本取出来改、再"写回去"同样无效 —— 那只是在改一个局部变量，**静默不生效**。</item>
	/// </list>
	/// 写入统一走 <see cref="WorldGen"/>：<see cref="WorldGen.PlaceTile"/> /
	/// <see cref="WorldGen.KillTile"/> / <see cref="WorldGen.PlaceWall"/> / <see cref="WorldGen.PlaceLiquid"/>。
	/// 读取仍然可以直接读 <c>Main.tile[x, y].HasTile</c>（读副本没问题），本类用它跳过重复写入。
	///
	/// <para/>性能：壁炉世界约 **900×1300 ≈ 117 万格**。每格一次 WorldGen 调用可以接受（生成一次几秒），
	/// 但有两件必须做的事：
	/// 1. 写入前先比对，已经是目标状态就跳过；
	/// 2. 框架化**只对改动过的矩形**做（<see cref="FrameRegion"/>），不要全世界刷一遍。
	/// </summary>
	public static class WorldPaint
	{
		/// <summary>本局生成一共写入了多少格（给日志用，诊断「生成到底跑没跑」）。</summary>
		public static long Writes;

		/// <summary>坐标是否在世界内。</summary>
		public static bool InWorld(int x, int y)
		{
			return x >= 0 && x < Main.maxTilesX && y >= 0 && y < Main.maxTilesY;
		}

		/// <summary>该格是不是这个方块（用来跳过重复写入）。</summary>
		public static bool IsTile(int x, int y, ushort type)
		{
			return InWorld(x, y) && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == type;
		}

		/// <summary>该格是实心方块吗。</summary>
		public static bool HasTile(int x, int y)
		{
			return InWorld(x, y) && Main.tile[x, y].HasTile;
		}

		/// <summary>该格有液体吗。</summary>
		public static bool HasLiquid(int x, int y)
		{
			return InWorld(x, y) && Main.tile[x, y].LiquidAmount > 0;
		}

		// ==================== 单格写入 ====================

		/// <summary>放一格方块。</summary>
		public static void SetTile(int x, int y, ushort type)
		{
			Writes++;
			if (!InWorld(x, y)) {
				return;
			}

			WorldGen.KillTile(x, y, false, false, true);
			WorldGen.PlaceTile(x, y, type, mute: true, forced: true);
		}

		/// <summary>只改方块类型（已经是目标方块就跳过）。</summary>
		public static void Retile(int x, int y, ushort type)
		{
			if (IsTile(x, y, type)) {
				return;
			}

			SetTile(x, y, type);
		}

		/// <summary>掏空一格（方块去掉；液体不在这里处理）。</summary>
		public static void ClearTile(int x, int y)
		{
			Writes++;
			if (!HasTile(x, y)) {
				return;
			}

			WorldGen.KillTile(x, y, false, false, true);
		}

		/// <summary>铺背景墙（0 = 拆墙）。</summary>
		public static void SetWall(int x, int y, ushort wall)
		{
			Writes++;
			if (!InWorld(x, y) || Main.tile[x, y].WallType == wall) {
				return;
			}

			if (wall == 0) {
				WorldGen.KillWall(x, y, false);
				return;
			}

			WorldGen.PlaceWall(x, y, wall, mute: true);
		}

		/// <summary>放液体：<paramref name="liquidType"/> 0 = 水、1 = 岩浆（空格子才生效）。</summary>
		public static void SetLiquid(int x, int y, byte amount, byte liquidType = 0)
		{
			Writes++;
			if (!InWorld(x, y) || Main.tile[x, y].HasTile) {
				return;
			}

			WorldGen.PlaceLiquid(x, y, liquidType, amount);
		}

		// ==================== 区域原语 ====================

		/// <summary>铺一块实心矩形（含边界）。</summary>
		public static void Rect(int left, int top, int right, int bottom, ushort type)
		{
			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					Retile(x, y, type);
				}
			}
		}

		/// <summary>把一块矩形掏空（墙保留）。</summary>
		public static void Carve(int left, int top, int right, int bottom)
		{
			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					ClearTile(x, y);
				}
			}
		}

		/// <summary>给一块矩形铺背景墙。</summary>
		public static void WallRect(int left, int top, int right, int bottom, ushort wall)
		{
			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					SetWall(x, y, wall);
				}
			}
		}

		/// <summary>只给"没有方块"的格子铺墙。</summary>
		public static void WallRectOpenOnly(int left, int top, int right, int bottom, ushort wall)
		{
			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					if (!InWorld(x, y) || Main.tile[x, y].HasTile) {
						continue;
					}

					SetWall(x, y, wall);
				}
			}
		}

		/// <summary>矩形液体（只在空格子里生效）。</summary>
		public static void LiquidRect(int left, int top, int right, int bottom, byte amount, byte liquidType = 0)
		{
			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					SetLiquid(x, y, amount, liquidType);
				}
			}
		}

		/// <summary>实心矩形边框（画"墙壳"用，中间留空）。</summary>
		public static void Frame(int left, int top, int right, int bottom, ushort type, int thickness = 1)
		{
			for (int t = 0; t < thickness; t++) {
				for (int x = left + t; x <= right - t; x++) {
					Retile(x, top + t, type);
					Retile(x, bottom - t, type);
				}

				for (int y = top + t; y <= bottom - t; y++) {
					Retile(left + t, y, type);
					Retile(right - t, y, type);
				}
			}
		}

		/// <summary>横线。</summary>
		public static void HLine(int left, int right, int y, ushort type)
		{
			for (int x = left; x <= right; x++) {
				Retile(x, y, type);
			}
		}

		/// <summary>竖线。</summary>
		public static void VLine(int x, int top, int bottom, ushort type)
		{
			for (int y = top; y <= bottom; y++) {
				Retile(x, y, type);
			}
		}

		/// <summary>实心椭圆（原版做矿脉 / 灰烬丘就是这类形状）。</summary>
		public static void Ellipse(int centerX, int centerY, float radiusX, float radiusY, ushort type)
		{
			EachEllipse(centerX, centerY, radiusX, radiusY, (x, y) => Retile(x, y, type));
		}

		/// <summary>椭圆形掏洞。</summary>
		public static void EllipseCarve(int centerX, int centerY, float radiusX, float radiusY)
		{
			EachEllipse(centerX, centerY, radiusX, radiusY, (x, y) => ClearTile(x, y));
		}

		private static void EachEllipse(int centerX, int centerY, float radiusX, float radiusY, Action<int, int> action)
		{
			if (radiusX <= 0f || radiusY <= 0f) {
				return;
			}

			int left = (int)MathF.Floor(centerX - radiusX);
			int right = (int)MathF.Ceiling(centerX + radiusX);
			int top = (int)MathF.Floor(centerY - radiusY);
			int bottom = (int)MathF.Ceiling(centerY + radiusY);

			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					float dx = (x - centerX) / radiusX;
					float dy = (y - centerY) / radiusY;

					if (dx * dx + dy * dy <= 1f) {
						action(x, y);
					}
				}
			}
		}

		// ==================== 框架化 ====================

		/// <summary>
		/// 只对一块矩形做框架化（方块 + 墙）。
		/// <para/>⚠️ 不要"全世界刷一遍"：117 万格 × 2 遍 = 230 万次调用，
		/// 按区域做只需要覆盖真正改动过的地方。
		/// </summary>
		public static void FrameRegion(int left, int top, int right, int bottom)
		{
			left = Math.Max(left, 0);
			top = Math.Max(top, 0);
			right = Math.Min(right, Main.maxTilesX - 1);
			bottom = Math.Min(bottom, Main.maxTilesY - 1);

			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					WorldGen.SquareTileFrame(x, y, true);
				}
			}

			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					WorldGen.SquareWallFrame(x, y, true);
				}
			}
		}

		// ==================== 噪声（原版地表起伏用的就是分层噪声） ====================
		//
		// 自己写一个确定性的**值噪声 + 分形叠加**，不依赖 Main.rand：
		// 同一种子每次生成出来的地形完全一样，调试和截图都对得上。

		private static float Hash(int seed, int index)
		{
			unchecked {
				int h = (seed * 374761393) + (index * 668265263);
				h = (h ^ (h >> 13)) * 1274126177;
				return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
			}
		}

		private static float Smooth(float t)
		{
			return t * t * (3f - 2f * t);
		}

		/// <summary>一维值噪声。</summary>
		public static float ValueNoise(int seed, float x)
		{
			int i = (int)MathF.Floor(x);
			float f = x - i;

			return MathHelper.Lerp(Hash(seed, i), Hash(seed, i + 1), Smooth(f));
		}

		/// <summary>一维分形噪声（返回 0..1）。</summary>
		public static float Fractal(int seed, float x, int octaves = 4, float persistence = 0.5f)
		{
			float total = 0f;
			float amplitude = 1f;
			float frequency = 1f;
			float norm = 0f;

			for (int i = 0; i < octaves; i++) {
				total += ValueNoise(seed + i * 7919, x * frequency) * amplitude;
				norm += amplitude;
				amplitude *= persistence;
				frequency *= 2f;
			}

			return norm <= 0f ? 0f : total / norm;
		}

		/// <summary>二维值噪声。</summary>
		public static float ValueNoise2D(int seed, float x, float y)
		{
			int ix = (int)MathF.Floor(x);
			int iy = (int)MathF.Floor(y);
			float fx = Smooth(x - ix);
			float fy = Smooth(y - iy);

			float a = Hash(seed, (ix * 73856093) ^ (iy * 19349663));
			float b = Hash(seed, ((ix + 1) * 73856093) ^ (iy * 19349663));
			float c = Hash(seed, (ix * 73856093) ^ ((iy + 1) * 19349663));
			float d = Hash(seed, ((ix + 1) * 73856093) ^ ((iy + 1) * 19349663));

			return MathHelper.Lerp(MathHelper.Lerp(a, b, fx), MathHelper.Lerp(c, d, fx), fy);
		}

		/// <summary>二维分形噪声。</summary>
		public static float Fractal2D(int seed, float x, float y, int octaves = 3, float persistence = 0.5f)
		{
			float total = 0f;
			float amplitude = 1f;
			float frequency = 1f;
			float norm = 0f;

			for (int i = 0; i < octaves; i++) {
				total += ValueNoise2D(seed + i * 104729, x * frequency, y * frequency) * amplitude;
				norm += amplitude;
				amplitude *= persistence;
				frequency *= 2f;
			}

			return norm <= 0f ? 0f : total / norm;
		}
	}
}
