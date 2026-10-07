using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using WastelandSoul.Content.Tiles;

namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 「壁炉」设施的生成器：在一张 600×600 的空白世界上盖出一座封闭的地下设施。
	///
	/// <para/>**不调用任何原版地形生成**，全部自己铺——这是人工建筑，
	/// 让世界生成器去加洞穴和矿物只会把结构打乱。
	///
	/// <para/>布局（坐标单位：格）：
	/// <code>
	///   y=300 ~ 500   主厅：240 宽 × 200 高（大理石地面 + 蓝砖侧墙）
	///   y=498 ~ 500   壁炉：主厅正中，下沉火塘 + 生命火
	///   y=428         数据终端：主厅右侧的旧时代控制台
	///   左右两侧      走廊（y=380~430）通向末端房间，后续放小怪
	/// </code>
	///
	/// <para/>⚠️ **不要直接写 <c>Main.tile[x, y]</c>**：它是 <c>Tilemap</c> 这个**结构体**的
	/// 索引器，而 <c>Main.tile</c> 本身是属性，setter 作用在临时副本上——
	/// 读出来改（CS1612）和写回去（CS0200）都会被编译器拒绝。
	/// 这里统一走 <see cref="WorldGen.PlaceTile"/> / <see cref="WorldGen.KillTile"/>，
	/// 背景墙走 <see cref="WorldGen.PlaceWall"/>。
	/// </summary>
	public static class FireplaceStructure
	{
		// ---------------- 关键尺寸 ----------------
		internal const int HallLeft = 180;
		internal const int HallRight = 420;
		internal const int HallTop = 300;
		internal const int HallBottom = 500;

		/// <summary>墙壁厚度。</summary>
		private const int Wall = 2;

		/// <summary>壁炉火塘的左右边界。</summary>
		internal const int HearthLeft = 270;
		internal const int HearthRight = 330;

		/// <summary>数据终端所在的 y。</summary>
		internal const int TerminalY = 428;

		/// <summary>走廊的上下边界。</summary>
		private const int CorridorTop = 380;
		private const int CorridorBottom = 430;

		public static void Build(GenerationProgress progress, GameConfiguration configuration)
		{
			progress.Message = "正在浇铸壁炉……";

			// ---------- 1. 全世界填成实心石（设施外壳的底料） ----------
			FillSolidStone();

			progress.Set(0.3);

			// ---------- 2. 掏空主厅与两条走廊 ----------
			CarveRoom(HallLeft, HallTop, HallRight, HallBottom);
			CarveRoom(60, CorridorTop, HallLeft - 1, CorridorBottom);
			CarveRoom(HallRight + 1, CorridorTop, 540, CorridorBottom);

			progress.Set(0.45);

			// ---------- 3. 内衬：地面 / 天花 / 侧墙 / 背景墙 ----------
			BuildInterior();

			progress.Set(0.65);

			// ---------- 4. 壁炉本体 ----------
			BuildHearth();

			// ---------- 5. 数据终端 ----------
			BuildTerminal();

			progress.Set(0.8);

			// ---------- 6. 立柱与照明 ----------
			Decorate();
			PlaceStoryTiles();

			progress.Set(0.9);

			// ---------- 7. 统一框架化 ----------
			SquareFrameAll();

			progress.Set(1.0);
		}

		/// <summary>全世界铺实心石。</summary>
		private static void FillSolidStone()
		{
			for (int x = 0; x < Main.maxTilesX; x++) {
				for (int y = 0; y < Main.maxTilesY; y++) {
					PutTile(x, y, TileID.Stone);
				}
			}
		}

		/// <summary>主厅内衬：地面用大理石，天花用大理石方块，侧墙用蓝砖。</summary>
		private static void BuildInterior()
		{
			// 地面
			for (int x = HallLeft - Wall; x <= HallRight + Wall; x++) {
				for (int y = HallBottom + 1; y <= HallBottom + Wall; y++) {
					PutTile(x, y, TileID.Marble);
				}
			}

			// 天花板
			for (int x = HallLeft - Wall; x <= HallRight + Wall; x++) {
				for (int y = HallTop - Wall; y < HallTop; y++) {
					PutTile(x, y, TileID.MarbleBlock);
				}
			}

			// 左右侧墙
			for (int y = HallTop - Wall; y <= HallBottom + Wall; y++) {
				for (int x = HallLeft - Wall; x < HallLeft; x++) {
					PutTile(x, y, TileID.BlueDungeonBrick);
				}

				for (int x = HallRight + 1; x <= HallRight + Wall; x++) {
					PutTile(x, y, TileID.BlueDungeonBrick);
				}
			}

			// 走廊内衬
			BuildCorridorLining(60, HallLeft - 1);
			BuildCorridorLining(HallRight + 1, 540);

			// 背景墙：让室内不透明，看起来才是"室内"
			FillBackWall(HallLeft, HallTop, HallRight, HallBottom, WallID.BlueDungeon);
			FillBackWall(60, CorridorTop, HallLeft - 1, CorridorBottom, WallID.Gray);
			FillBackWall(HallRight + 1, CorridorTop, 540, CorridorBottom, WallID.Gray);
		}

		private static void BuildCorridorLining(int left, int right)
		{
			for (int x = left; x <= right; x++) {
				PutTile(x, CorridorTop - 1, TileID.MarbleBlock);
				PutTile(x, CorridorBottom + 1, TileID.Marble);
			}
		}

		/// <summary>
		/// 壁炉本体：主厅正中的下沉火塘 + 炉台。
		/// <para/>塘底铺**生命火**（永远燃着、自带光），四周用黑曜石砖围出炉膛，
		/// 两侧留出大理石台面——让"壁炉"这个名字在地图上真的看得出是个炉子。
		/// </summary>
		private static void BuildHearth()
		{
			const int floor = HallBottom;

			// 炉膛：把地面往下挖 3 格
			for (int x = HearthLeft; x <= HearthRight; x++) {
				for (int y = floor - 2; y <= floor; y++) {
					ClearTile(x, y);
				}
			}

			// 炉膛底与外壁
			for (int x = HearthLeft - 3; x <= HearthRight + 3; x++) {
				PutTile(x, floor + 1, TileID.ObsidianBrick);
			}

			for (int y = floor - 2; y <= floor + 1; y++) {
				for (int i = 0; i < 3; i++) {
					PutTile(HearthLeft - 3 + i, y, TileID.ObsidianBrick);
					PutTile(HearthRight + 3 - i, y, TileID.ObsidianBrick);
				}
			}

			// 火：生命火铺在塘底
			for (int x = HearthLeft; x <= HearthRight; x++) {
				PutTile(x, floor, TileID.LivingFire);
			}

			// 炉台：左右各一段大理石台面
			for (int x = HearthLeft - 14; x < HearthLeft - 3; x++) {
				PutTile(x, floor, TileID.Marble);
			}

			for (int x = HearthRight + 4; x <= HearthRight + 14; x++) {
				PutTile(x, floor, TileID.Marble);
			}
		}

		/// <summary>数据终端：主厅右侧的一段旧时代控制台（金属栏 + 发光指示灯）。</summary>
		private static void BuildTerminal()
		{
			const int left = 340;
			const int right = 366;
			const int y = TerminalY;

			// 台体
			for (int x = left; x <= right; x++) {
				PutTile(x, y, TileID.MetalBars);
				PutTile(x, y - 1, TileID.MetalBars);
			}

			// 指示灯
			for (int x = left + 2; x <= right - 2; x += 3) {
				PutTile(x, y - 2, TileID.EmeraldGemspark);
			}

			// 终端背后的背景墙，让它看着像嵌在壁上的
			FillBackWall(left - 3, y - 8, right + 3, y - 1, WallID.ObsidianBrick);
		}

		/// <summary>终端和返回门。底部中心对准空气格，家具才会站在台子或地面上。</summary>
		private static void PlaceStoryTiles()
		{
			int terminalBottom = TerminalY - 3;

			for (int dx = -1; dx <= 1; dx++) {
				for (int dy = 0; dy <= 2; dy++) {
					ClearTile(353 + dx, terminalBottom - dy);
				}
			}

			WorldGen.PlaceTile(353, terminalBottom, ModContent.TileType<FireplaceTerminal>(), mute: true, forced: true);

			int exitBottom = HallBottom;

			for (int dx = -1; dx <= 1; dx++) {
				for (int dy = 0; dy <= 2; dy++) {
					ClearTile(HallLeft + 12 + dx, exitBottom - dy);
				}
			}

			WorldGen.PlaceTile(HallLeft + 12, exitBottom, ModContent.TileType<FireplaceExit>(), mute: true, forced: true);
		}

		/// <summary>支撑柱与火把——让 240×200 的大厅不至于空荡。</summary>
		private static void Decorate()
		{
			const int floor = HallBottom;

			// 两排大理石立柱
			for (int x = HallLeft + 40; x <= HallRight - 40; x += 80) {
				for (int y = HallTop + 2; y < floor - 2; y++) {
					PutTile(x, y, TileID.MarbleColumn);
					PutTile(x + 1, y, TileID.MarbleColumn);
				}
			}

			// 地面火把
			for (int x = HallLeft + 6; x <= HallRight - 6; x += 24) {
				// 别插到炉膛里
				if (x >= HearthLeft - 4 && x <= HearthRight + 4) {
					continue;
				}

				PutTile(x, floor - 1, TileID.Torches);
			}

			// 走廊火把
			for (int x = 66; x <= HallLeft - 8; x += 20) {
				PutTile(x, CorridorBottom - 1, TileID.Torches);
			}

			for (int x = HallRight + 8; x <= 534; x += 20) {
				PutTile(x, CorridorBottom - 1, TileID.Torches);
			}
		}

		// ==================== 低层工具 ====================

		private static bool InWorld(int x, int y)
		{
			return x >= 0 && x < Main.maxTilesX && y >= 0 && y < Main.maxTilesY;
		}

		/// <summary>放一格方块（走 WorldGen，<c>forced: true</c> 表示不问相邻条件强行放）。</summary>
		private static void PutTile(int x, int y, ushort type)
		{
			if (!InWorld(x, y)) {
				return;
			}

			WorldGen.KillTile(x, y, false, false, true);
			WorldGen.PlaceTile(x, y, type, mute: true, forced: true);
		}

		private static void ClearTile(int x, int y)
		{
			if (!InWorld(x, y)) {
				return;
			}

			WorldGen.KillTile(x, y, false, false, true);
		}

		/// <summary>把一块矩形掏空。</summary>
		private static void CarveRoom(int left, int top, int right, int bottom)
		{
			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					ClearTile(x, y);
				}
			}
		}

		/// <summary>给一块区域铺背景墙（贴着方块的位置也铺，室内才像室内）。</summary>
		private static void FillBackWall(int left, int top, int right, int bottom, ushort wallType)
		{
			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					if (!InWorld(x, y)) {
						continue;
					}

					// 只给空位铺墙：方块所在格自带的墙不用动，避免覆盖结构本身
					if (Main.tile[x, y].HasTile) {
						continue;
					}

					WorldGen.PlaceWall(x, y, wallType, mute: true);
				}
			}
		}

		/// <summary>
		/// 全世界做一次框架化。
		/// <para/>⚠️ <c>WorldGen.SquareTileFrame(i, j, resetFrame)</c> 是**按单格**框架化的，
		/// 没有"按区域"的重载（之前误以为有，少传参数在编译期就报了）。
		/// 这里能直接遍历整个小世界：600×600 = 36 万格，而且跑在世界生成阶段、不在游戏循环里。
		/// </summary>
		private static void SquareFrameAll()
		{
			for (int x = 0; x < Main.maxTilesX; x++) {
				for (int y = 0; y < Main.maxTilesY; y++) {
					WorldGen.SquareTileFrame(x, y, true);
				}
			}

			for (int x = 0; x < Main.maxTilesX; x++) {
				for (int y = 0; y < Main.maxTilesY; y++) {
					WorldGen.SquareWallFrame(x, y, true);
				}
			}
		}
	}
}
