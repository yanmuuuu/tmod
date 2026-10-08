using System;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using WastelandSoul.Content.Backgrounds;
using WastelandSoul.Content.Tiles;

namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 壁炉的**人造结构**：堡垒（半埋在地表下的设施）、堡前的祈塔、以及塔下那条笔直的地下通道。
	///
	/// <para/>建材与配色按设定：
	/// <list type="bullet">
	/// <item>结构全部用**瑜钢合金**（<see cref="YugangAlloy"/>，无法挖取），
	/// 每隔一段嵌亮银青的<see cref="YugangTrim"/>压条 —— 这就是"青银间色"的来源；</item>
	/// <item>墙体（背景墙）用原版**地狱砖墙** <c>WallID.HellstoneBrick</c>；</item>
	/// <item>内饰一律用原版家具：火把 / 吊灯 / 台座 / 桌椅 / 书架 / 金属栏杆 / 翡翠微光。</item>
	/// </list>
	///
	/// <para/>生成顺序很重要：**先铺满壳 → 压条带 → 再掏空内部**。
	/// 这样压条只会留在墙壳上，内部自然是干净的（反过来就得手工擦）。
	/// </summary>
	public static class FireplaceBuildings
	{
		private static ushort Alloy => (ushort)ModContent.TileType<YugangAlloy>();

		private static ushort Trim => (ushort)ModContent.TileType<YugangTrim>();

		// ====================================================================================
		//  堡垒
		// ====================================================================================

		public static void BuildFortress(GenerationProgress progress, GameConfiguration configuration)
		{
			// 冒烟测试会传 null 进来（不构造 GameConfiguration，免得拉进 Newtonsoft 触发警告）
			progress ??= new GenerationProgress();
			FireplaceGenLog.Start("2/6 壁炉堡垒");
			progress.Message = "正在浇铸壁炉堡垒……";

			int left = FireplaceLayout.FortLeft;
			int right = FireplaceLayout.FortRight;
			int top = FireplaceLayout.FortTop;
			int bottom = FireplaceLayout.FortBottom;
			int shell = FireplaceLayout.FortShell;

			// 1) 把灰烬地形让开，再浇外壳
			WorldPaint.Carve(left - 1, top - 1, right + 1, bottom + 2);
			WorldPaint.Frame(left, top, right, bottom, Alloy, shell);

			// 2) 青银相间：每 12 行横贯一条压条（稍后掏空内部，压条只会留在壳上）
			for (int y = top; y <= bottom; y++) {
				if ((y - top) % 12 != 0) {
					continue;
				}

				WorldPaint.HLine(left, right, y, Trim);
			}

			progress.Set(0.25);

			// 3) 门厅（室内）
			int hallLeft = FireplaceLayout.HallLeft;
			int hallRight = FireplaceLayout.HallRight;
			int hallTop = FireplaceLayout.HallTop;
			int hallBottom = FireplaceLayout.HallBottom;

			WorldPaint.Frame(hallLeft, hallTop, hallRight, hallBottom, Alloy, 1);
			WorldPaint.Carve(left + shell, top + shell, right - shell, bottom - shell);
			WorldPaint.WallRect(hallLeft - 2, hallTop - 2, hallRight + 2, hallBottom + 2, WallID.HellstoneBrick);

			// 3.5) 门厅地板：**必须真的有**（真 bug 修）。
			// 上面那一次 Carve(内腔) 把 Frame(hall) 一起挖掉了 —— 因为门厅矩形整个落在
			// 285..615 × 865..1115 里面，而 Frame 排在 Carve 之前。后果：玩家进门时
			// Main.spawnTileY=HallBottom=1080 那一格是**空气**，人会直接往下掉到堡垒底壳
			// （1116），火塘/终端/返回门全都悬在半空。
			// 这里按 HallBottom 铺 3 层（顶层压条走边），炉膛与竖井在后面的步骤里再从
			// 这块地板上开口（BuildHearth 的 Carve、FortShaft 的 Carve 都排在它之后）。
			WorldPaint.Rect(hallLeft, hallBottom, hallRight, hallBottom + 2, Alloy);
			WorldPaint.HLine(hallLeft, hallRight, hallBottom, Trim);

			// 4) 与塔、与地下通道连通
			WorldPaint.Carve(FireplaceLayout.TowerLeft + 6, top - 1, FireplaceLayout.TowerRight - 6, hallTop);

			// ⚠️ 竖井从 **hallBottom** 起挖：旧写法从 hallBottom+1 起，
			// 新铺的门厅地板那一行（1080）会把井口封住。
			WorldPaint.Carve(FireplaceLayout.FortShaftLeft, hallBottom,
				FireplaceLayout.FortShaftRight, FireplaceLayout.TunnelTop - 2);
			WorldPaint.WallRect(FireplaceLayout.FortShaftLeft, hallBottom,
				FireplaceLayout.FortShaftRight, FireplaceLayout.TunnelTop - 2, WallID.HellstoneBrick);

			progress.Set(0.45);

			BuildHearth();
			BuildTerminalAndExit();
			DecorateHall();

			progress.Set(1.0);
		}

		/// <summary>壁炉本体：门厅正中下沉的火塘，塘底铺**生命火**（永远燃着、自带光）。</summary>
		private static void BuildHearth()
		{
			int floor = FireplaceLayout.HallBottom;
			int left = FireplaceLayout.HearthLeft;
			int right = FireplaceLayout.HearthRight;

			// 炉膛：地面往下挖 3 格
			WorldPaint.Carve(left, floor - 2, right, floor);

			// 炉膛围壁
			WorldPaint.Rect(left - 3, floor + 1, right + 3, floor + 1, TileID.ObsidianBrick);

			for (int y = floor - 2; y <= floor + 1; y++) {
				for (int i = 0; i < 3; i++) {
					WorldPaint.Retile(left - 3 + i, y, TileID.ObsidianBrick);
					WorldPaint.Retile(right + 3 - i, y, TileID.ObsidianBrick);
				}
			}

			// 火：生命火铺在塘底
			WorldPaint.HLine(left, right, floor, TileID.LivingFire);

			// 炉台：左右各一段合金台面 + 压条走边
			WorldPaint.HLine(left - 16, left - 4, floor, Trim);
			WorldPaint.HLine(right + 4, right + 16, floor, Trim);
		}

		/// <summary>数据终端 + 返回门。</summary>
		private static void BuildTerminalAndExit()
		{
			int floor = FireplaceLayout.HallBottom;

			// 数据终端：台体 + 指示灯 + 背后的地狱砖墙
			int terminalLeft = FireplaceLayout.TerminalX - 12;
			int terminalRight = FireplaceLayout.TerminalX + 12;
			int terminalY = FireplaceLayout.TerminalY;

			WorldPaint.HLine(terminalLeft, terminalRight, terminalY, TileID.MetalBars);
			WorldPaint.HLine(terminalLeft, terminalRight, terminalY - 1, TileID.MetalBars);

			for (int x = terminalLeft + 2; x <= terminalRight - 2; x += 3) {
				WorldPaint.Retile(x, terminalY - 2, TileID.EmeraldGemspark);
			}

			// 放终端方块本体（3x3，底部中心要对准空气格）
			int terminalBottom = terminalY - 3;
			WorldPaint.Carve(FireplaceLayout.TerminalX - 1, terminalBottom - 2, FireplaceLayout.TerminalX + 1, terminalBottom);
			WorldGen.PlaceTile(FireplaceLayout.TerminalX, terminalBottom, ModContent.TileType<FireplaceTerminal>(), mute: true, forced: true);

			// 返回门
			int exitBottom = floor - 1;
			WorldPaint.Carve(FireplaceLayout.ExitX - 1, exitBottom - 2, FireplaceLayout.ExitX + 1, exitBottom);
			WorldGen.PlaceTile(FireplaceLayout.ExitX, exitBottom, ModContent.TileType<FireplaceExit>(), mute: true, forced: true);
		}

		/// <summary>门厅内饰：立柱、栏杆、火把、吊灯、桌椅、书架 —— 全用原版家具。</summary>
		private static void DecorateHall()
		{
			int left = FireplaceLayout.HallLeft;
			int right = FireplaceLayout.HallRight;
			int top = FireplaceLayout.HallTop;
			int floor = FireplaceLayout.HallBottom;
			int hearthLeft = FireplaceLayout.HearthLeft;
			int hearthRight = FireplaceLayout.HearthRight;

			// 合金立柱：每 40 格一对（压条材质，和暗色墙面形成青银对比）
			for (int x = left + 32; x <= right - 32; x += 40) {
				if (x > hearthLeft - 24 && x < hearthRight + 24) {
					continue;       // 别把柱子插在火塘前面
				}

				WorldPaint.Frame(x, top + 2, x + 3, floor - 2, Trim, 1);
			}

			// 地面火把（跳过炉膛范围）：本批从 24 格加密到 12 格
			for (int x = left + 8; x <= right - 8; x += 12) {
				if (x >= hearthLeft - 6 && x <= hearthRight + 6) {
					continue;
				}

				WorldGen.PlaceTile(x, floor - 1, TileID.Torches, mute: true, forced: true);
			}

			// 地面走线：钻石/翡翠微光连着铺（和地宫一个做法，玩家要求"连续的宝石微光走线"）
			// ⚠️ 两条禁区必须让开：炉膛范围，以及**堡垒竖井的井口**（在那儿铺方块等于把
			// 门厅 → 地下通道的唯一通路堵死，本批新增的地板走线很容易踩到这个坑）。
			for (int x = left + 4; x <= right - 4; x += 2) {
				if (x >= hearthLeft - 4 && x <= hearthRight + 4) {
					continue;
				}

				if (x >= FireplaceLayout.FortShaftLeft - 1 && x <= FireplaceLayout.FortShaftRight + 1) {
					continue;
				}

				WorldPaint.Retile(x, floor, x % 6 == 0 ? TileID.DiamondGemspark : TileID.EmeraldGemspark);
			}

			// 吊灯：从天花板垂下来（本批从 80 格加密到 36 格）
			for (int x = left + 36; x <= right - 36; x += 36) {
				WorldGen.PlaceObject(x, top + 1, TileID.Chandeliers, true, 0, 0, -1, -1);
			}

			// 桌椅 / 书架：贴着侧墙摆几组，像旧时代的避难所
			// 中层走廊：门厅拦一道走廊出来，让"堡垒"看起来是分区的而不是一个大空房。
			// 本批：走廊楼板从 **1 格加厚到 5 格**（玩家反馈"太空、没有实体感"），
			// 两端各留一条 6 格宽 × 6 格高的上下贯通口，人还是能从下层走到上层。
			int midY = top + (floor - top) / 2;
			WorldPaint.Rect(left, midY, right, midY + 4, Alloy);
			WorldPaint.HLine(left, right, midY, Trim);
			WorldPaint.Carve(left + 2, midY - 4, right - 2, midY - 1);
			WorldPaint.WallRect(left + 2, midY - 4, right - 2, midY - 1, WallID.HellstoneBrick);

			// 走廊两端留上下贯通的口子（打穿整块 5 格厚楼板 + 背景墙）
			WorldPaint.Carve(left + 6, midY, left + 11, midY + 4);
			WorldPaint.Carve(right - 11, midY, right - 6, midY + 4);
			WorldPaint.WallRect(left + 6, midY, left + 11, midY + 4, WallID.HellstoneBrick);
			WorldPaint.WallRect(right - 11, midY, right - 6, midY + 4, WallID.HellstoneBrick);

			// 走廊照明：火把 + 地面走线（走线让开两端那两条上下贯通口）
			for (int x = left + 14; x <= right - 14; x += 14) {
				WorldGen.PlaceTile(x, midY - 5, TileID.Torches, mute: true, forced: true);
			}

			for (int x = left + 12; x <= right - 12; x += 2) {
				if (x >= left + 5 && x <= left + 12) {
					continue;
				}

				if (x >= right - 12 && x <= right - 5) {
					continue;
				}

				WorldPaint.Retile(x, midY, x % 6 == 0 ? TileID.DiamondGemspark : TileID.EmeraldGemspark);
			}

			// 两侧小房间：用 **5 格厚**隔墙（两侧压条 + 中间合金）隔出 3 间，
			// 宽度刻意做成 30 / 40 / 24（大中小混排，不再等距切格子）。
			int[] roomWidths = { 30, 40, 24 };
			int cursor = left + 10;

			for (int i = 0; i < roomWidths.Length; i++) {
				int roomLeft = cursor;
				int roomRight = roomLeft + roomWidths[i] - 1;
				int wallLeft = roomRight + 1;
				int wallRight = wallLeft + 4;                 // 5 格厚隔墙（含两端压条）

				if (wallRight >= right - 10) {
					break;
				}

				FillPartition(wallLeft, wallRight, midY - 12, midY - 1);
				WorldPaint.Carve(roomLeft, midY - 12, roomRight, midY - 2);
				WorldPaint.HLine(roomLeft, roomRight, midY - 1, Alloy);
				WorldPaint.WallRect(roomLeft, midY - 12, roomRight, midY - 2, WallID.HellstoneBrick);

				PlaceFurnitureGroup(roomLeft + 6, midY - 2);

				WorldGen.PlaceTile(roomLeft + 3, midY - 13, TileID.Torches, mute: true, forced: true);

				cursor = wallRight + 1;
			}

			PlaceFurnitureGroup(left + 14, floor - 1);
			PlaceFurnitureGroup(right - 30, floor - 1);

			// 金属栏杆：在门厅两侧隔出小平台
			for (int x = left + 60; x <= left + 100; x++) {
				WorldPaint.Retile(x, floor - 10, TileID.MetalBars);
			}

			for (int x = right - 100; x <= right - 60; x++) {
				WorldPaint.Retile(x, floor - 10, TileID.MetalBars);
			}
		}

		private static void PlaceFurnitureGroup(int x, int floor)
		{
			// 桌子（2x1）+ 两把椅子（1x2）+ 书架（3x4）
			WorldGen.PlaceObject(x, floor, TileID.Tables, true, 0, 0, -1, -1);
			WorldGen.PlaceObject(x - 3, floor, TileID.Chairs, true, 0, 0, -1, 1);
			WorldGen.PlaceObject(x + 4, floor, TileID.Chairs, true, 0, 0, -1, -1);
			WorldGen.PlaceObject(x + 7, floor - 3, TileID.Bookcases, true, 0, 0, -1, -1);
		}

		/// <summary>
		/// 填一道**内部隔墙**：两侧外皮压条、中间合金本体 —— 和地宫的
		/// <c>FireplaceVault.FillWall</c> 同一套"青银间色"配色。
		///
		/// <para/>本批把堡垒与塔里那些 1~2 格厚的隔断一律加厚到 **4~6 格**
		/// （玩家反馈「房间之间要像墙，不要像纸」）。加厚之后门洞/走廊必须重新对齐，
		/// 所以调用点都紧跟着一次把整堵墙打穿的 <c>Carve</c>。
		/// </summary>
		private static void FillPartition(int left, int right, int top, int bottom)
		{
			if (right < left || bottom < top) {
				return;
			}

			for (int x = left; x <= right; x++) {
				ushort type = (x == left || x == right) ? Trim : Alloy;

				for (int y = top; y <= bottom; y++) {
					WorldPaint.Retile(x, y, type);
				}
			}
		}

		// ====================================================================================
		//  祈塔（通往宇宙）
		// ====================================================================================

		public static void BuildTower(GenerationProgress progress, GameConfiguration configuration)
		{
			// 冒烟测试会传 null 进来（不构造 GameConfiguration，免得拉进 Newtonsoft 触发警告）
			progress ??= new GenerationProgress();
			FireplaceGenLog.Start("3/6 祈塔");
			progress.Message = "正在把祈塔浇向天顶……";

			int left = FireplaceLayout.TowerLeft;
			int right = FireplaceLayout.TowerRight;
			int top = FireplaceLayout.TowerTop;
			int bottom = FireplaceLayout.TowerBottom;
			int shell = FireplaceLayout.TowerShell;

			// 1) 外壳 + 压条带，再掏空
			WorldPaint.Frame(left, top, right, bottom, Alloy, shell);

			for (int y = top; y <= bottom; y++) {
				if ((y - top) % 20 != 0) {
					continue;
				}

				WorldPaint.HLine(left, right, y, Trim);
			}

			WorldPaint.Carve(left + shell, top + shell, right - shell, bottom - 1);
			WorldPaint.WallRect(left + shell, top + shell, right - shell, bottom - 1, WallID.HellstoneBrick);

			progress.Set(0.3);

			// 2) 楼板 + 靠左的攀登竖井
			int floors = (bottom - top) / FireplaceLayout.TowerFloorHeight;

			for (int i = 1; i < floors; i++) {
				int y = bottom - (i * FireplaceLayout.TowerFloorHeight);

				WorldPaint.HLine(left + shell, right - shell, y, Alloy);

				// 竖井口：左边留 8 格不封
				for (int x = left + shell; x < left + shell + 8; x++) {
					WorldPaint.ClearTile(x, y);
				}
			}

			// 攀登平台（原版木平台，可以从下面跳上去）
			for (int y = bottom - 5; y > top + 4; y -= 5) {
				for (int x = left + shell + 1; x <= left + shell + 7; x++) {
					WorldGen.PlaceTile(x, y, TileID.Platforms, mute: true, forced: true);
				}
			}

			progress.Set(0.55);

			// 3) 长窗（每 30 行一对），窗格用原版玻璃
			for (int y = top + 12; y <= bottom - 12; y += 30) {
				for (int dy = 0; dy < 3; dy++) {
					WorldPaint.Retile(left, y + dy, TileID.Glass);
					WorldPaint.Retile(right, y + dy, TileID.Glass);
					WorldPaint.Retile(left + 1, y + dy, TileID.Glass);
					WorldPaint.Retile(right - 1, y + dy, TileID.Glass);
				}
			}

			progress.Set(0.7);

			// 4) 每层装饰：交替「休息层」（桌椅书架）/「设备层」（压条台 + 微光指示灯）
			for (int i = 1; i < floors; i++) {
				int y = bottom - (i * FireplaceLayout.TowerFloorHeight);

				if (i % 3 == 1) {
					PlaceFurnitureGroup(right - 22, y - 1);
				}
				else if (i % 3 == 2) {
					// 小房间：用 **4 格厚**隔墙（两侧压条 + 中间合金）隔出右侧一间，
					// 门洞把整堵墙打穿（旧写法是 1 格厚的一列，加厚后必须一起打通）。
					int roomTop = y - 16;
					int dividerLeft = left + (right - left) / 2;
					int dividerRight = dividerLeft + 3;

					FillPartition(dividerLeft, dividerRight, roomTop, y - 1);
					WorldPaint.Carve(dividerRight + 1, roomTop, right - shell - 1, y - 2);
					WorldPaint.WallRect(dividerRight + 1, roomTop, right - shell - 1, y - 2, WallID.HellstoneBrick);
					WorldPaint.Carve(dividerLeft, y - 4, dividerRight, y - 2);   // 门洞：4 格宽
					PlaceFurnitureGroup(right - 12, y - 1);                      // 让开 450~453 那堵墙
				}
				else {
					for (int x = left + shell + 12; x <= right - shell - 12; x += 14) {
						WorldPaint.Retile(x, y - 1, TileID.EmeraldGemspark);
					}

					WorldPaint.HLine(left + shell + 10, right - shell - 10, y - 6, TileID.MetalBars);
				}

				// 每层：地板连着铺一条微光走线（本批新增，塔内不再"一层一盏灯"那么暗）
				// ⚠️ 从 left+shell+12 起：左边 left+shell ~ +7 是上下贯通的攀登竖井口，
				// 在那里铺方块会把井口堵死。
				for (int x = left + shell + 12; x <= right - shell - 2; x += 2) {
					WorldPaint.Retile(x, y, x % 6 == 0 ? TileID.DiamondGemspark : TileID.EmeraldGemspark);
				}

				// 每层一盏吊灯，塔内不至于全黑
				WorldGen.PlaceObject(right - 8, y + 2, TileID.Chandeliers, true, 0, 0, -1, -1);
			}

			progress.Set(0.85);

			BuildArena();

			progress.Set(1.0);
		}

		/// <summary>
		/// 塔顶的 Boss 场地：**先做壳与装饰，中间留空**（后续战斗设计还没定）。
		/// 天花开一段玻璃天窗，站在场地上抬头就是宇宙。
		///
		/// <para/>这一批按玩家要求把照明铺满：地面走一圈翡翠微光 + 火把、
		/// 两侧墙面竖向指示灯带、天花一排吊灯、中央台座上摆蜡烛，
		/// 站在场地上任何位置都能一眼看清边界（场地尺寸没动）。
		///
		/// <para/>另外补一段**接驳颈**：场地整块比塔顶高 12 格（为了让 ArenaBottom=288
		/// 仍落在太空线 840×0.35=294 以上），而且原来的"打通"写法排在铺地板之前、
		/// 之后又被 HLine 的地板盖回去，实际是**堵死的**。这里改成先掏空、再补两侧合金壁。
		/// </summary>
		private static void BuildArena()
		{
			int left = FireplaceLayout.ArenaLeft;
			int right = FireplaceLayout.ArenaRight;
			int top = FireplaceLayout.ArenaTop;
			int bottom = FireplaceLayout.ArenaBottom;

			WorldPaint.Frame(left, top, right, bottom, Alloy, 2);

			for (int y = top; y <= bottom; y++) {
				if ((y - top) % 14 != 0) {
					continue;
				}

				WorldPaint.HLine(left, right, y, Trim);
			}

			WorldPaint.Carve(left + 2, top + 2, right - 2, bottom - 2);
			WorldPaint.WallRect(left + 2, top + 2, right - 2, bottom - 2, WallID.HellstoneBrick);

			// 地板：合金 + 中央台座（压条）
			WorldPaint.HLine(left + 2, right - 2, bottom - 1, Alloy);

			for (int x = left + 40; x <= right - 40; x++) {
				WorldPaint.Retile(x, bottom - 2, Trim);
			}

			// 天窗：天花板中间镶一段玻璃
			for (int x = left + 60; x <= right - 60; x += 3) {
				WorldPaint.Retile(x, top, TileID.Glass);
				WorldPaint.Retile(x + 1, top, TileID.Glass);
				WorldPaint.Retile(x, top + 1, TileID.Glass);
				WorldPaint.Retile(x + 1, top + 1, TileID.Glass);
			}

			// 四角立柱，先把空间撑起来
			for (int i = 0; i < 4; i++) {
				int x = left + 12 + (i * (right - left - 24) / 3);
				WorldPaint.Frame(x, top + 4, x + 3, bottom - 3, Trim, 1);
			}

			// 接驳颈：场地地板 → 塔身内部（先整体掏空，再把两侧补成 2 格厚的合金壁）
			int neckLeft = FireplaceLayout.ArenaNeckLeft;
			int neckRight = FireplaceLayout.ArenaNeckRight;
			int neckBottom = FireplaceLayout.TowerTop + FireplaceLayout.TowerShell;

			WorldPaint.Carve(FireplaceLayout.TowerLeft, bottom - 3,
				FireplaceLayout.TowerRight, neckBottom);

			for (int y = bottom - 3; y <= neckBottom; y++) {
				WorldPaint.Retile(FireplaceLayout.TowerLeft, y, Alloy);
				WorldPaint.Retile(FireplaceLayout.TowerLeft + 1, y, Alloy);
				WorldPaint.Retile(FireplaceLayout.TowerRight - 1, y, Alloy);
				WorldPaint.Retile(FireplaceLayout.TowerRight, y, Alloy);
			}

			WorldPaint.Carve(neckLeft, bottom - 3, neckRight, neckBottom);
			WorldPaint.WallRect(neckLeft, bottom - 3, neckRight, neckBottom, WallID.HellstoneBrick);

			BuildArenaLights(left, right, top, bottom, neckLeft, neckRight);
		}

		/// <summary>塔顶场地的照明：沿四边走线 + 天花吊灯 + 中央台座的蜡烛。</summary>
		private static void BuildArenaLights(int left, int right, int top, int bottom, int neckLeft, int neckRight)
		{
			int floorSolid = bottom - 1;     // 地板实心行（台座以外都走这一行）
			int pedestal = bottom - 2;       // 中央台座（比地板高一格）

			// 1) 地面走线：翡翠 / 钻石微光**连着铺**，边界一眼可见
			//    （本批从"纯翡翠"改成两条色带交替，整体更亮一档）
			for (int x = left + 2; x <= right - 2; x += 2) {
				if (x >= neckLeft - 2 && x <= neckRight + 2) {
					continue;
				}

				WorldPaint.Retile(x, floorSolid, x % 6 == 0 ? TileID.DiamondGemspark : TileID.EmeraldGemspark);
			}

			// 2) 两侧墙面：竖向指示灯带（每 2 行一档，本批从 3 行加密）
			for (int y = top + 4; y <= bottom - 8; y += 2) {
				WorldPaint.Retile(left + 2, y, y % 6 == 0 ? TileID.DiamondGemspark : TileID.EmeraldGemspark);
				WorldPaint.Retile(right - 2, y, y % 6 == 0 ? TileID.DiamondGemspark : TileID.EmeraldGemspark);
			}

			// 3) 地面火把
			for (int x = left + 6; x <= right - 6; x += 12) {
				if (x >= neckLeft - 6 && x <= neckRight + 6) {
					continue;
				}

				WorldGen.PlaceTile(x, (x >= left + 40 && x <= right - 40) ? pedestal - 1 : floorSolid - 1,
					TileID.Torches, mute: true, forced: true);
			}

			// 4) 中央台座：压条走边 + 翡翠微光断续 + 蜡烛
			for (int x = left + 40; x <= right - 40; x++) {
				if (x >= neckLeft - 2 && x <= neckRight + 2) {
					continue;
				}

				WorldPaint.Retile(x, pedestal, x % 12 == 0 ? TileID.EmeraldGemspark : Trim);
			}

			for (int x = left + 44; x <= right - 44; x += 12) {
				if (x >= neckLeft - 2 && x <= neckRight + 2) {
					continue;
				}

				WorldGen.PlaceTile(x, pedestal - 1, TileID.Candles, mute: true, forced: true);
			}

			// 5) 天花板吊灯（本批从 40 格加密到 26 格）
			for (int x = left + 18; x <= right - 18; x += 26) {
				if (x >= neckLeft - 8 && x <= neckRight + 8) {
					continue;
				}

				WorldGen.PlaceObject(x, top + 2, TileID.Chandeliers, true, 0, 0, -1, -1);
			}
		}

		// ====================================================================================
		//  地下通道
		// ====================================================================================

		/// <summary>
		/// 地下通道的立柱：每 44 列一对（压条材质），做出"偌大笔直"的纵深。
		///
		/// <para/>⚠️ 单独抽成一个方法是因为它会被**调用两次**：一次是
		/// <see cref="BuildTunnel"/> 正常生成，另一次是 <c>FireplaceVault.Build</c> 的最后 ——
		/// 地宫那一步的"重开通道嘴"会把通道内空整段再掏一遍（椭球壳正好压在通道上），
		/// 顺带把这些立柱（几何复刻数出来是 **1818 格压条**）也挖掉。写成幂等的
		/// <see cref="WorldPaint.Frame"/> 调用，补回来时不会叠、也不会破坏通道壳。
		/// </summary>
		internal static void BuildTunnelPillars()
		{
			int left = FireplaceLayout.TunnelLeft;
			int right = FireplaceLayout.TunnelRight;
			int top = FireplaceLayout.TunnelTop;
			int bottom = FireplaceLayout.TunnelBottom;

			for (int x = left + 20; x <= right - 20; x += 44) {
				WorldPaint.Frame(x, top + 2, x + 1, bottom - 2, Trim, 1);
				WorldPaint.Frame(x + 3, top + 2, x + 4, bottom - 2, Trim, 1);
			}
		}

		public static void BuildTunnel(GenerationProgress progress, GameConfiguration configuration)
		{
			// 冒烟测试会传 null 进来（不构造 GameConfiguration，免得拉进 Newtonsoft 触发警告）
			progress ??= new GenerationProgress();
			FireplaceGenLog.Start("4/6 地下通道");
			progress.Message = "正在凿通通往未知的直道……";

			int left = FireplaceLayout.TunnelLeft;
			int right = FireplaceLayout.TunnelRight;
			int top = FireplaceLayout.TunnelTop;
			int bottom = FireplaceLayout.TunnelBottom;
			int shell = FireplaceLayout.TunnelShell;

			// 1) 先掏空（含壳的位置），再包壳、再掏内腔
			WorldPaint.Carve(left - shell, top - shell, right + shell, bottom + shell);
			WorldPaint.Frame(left - shell, top - shell, right + shell, bottom + shell, Alloy, shell);
			WorldPaint.WallRect(left - shell, top - shell, right + shell, bottom + shell, WallID.HellstoneBrick);

			// 2) 上下压条：一条在顶、一条在走道面
			WorldPaint.HLine(left, right, top, Trim);
			WorldPaint.HLine(left, right, bottom - 1, Trim);

			progress.Set(0.4);

			// 3) 立柱：每 44 列一对（压条材质），做出"偌大笔直"的纵深
			BuildTunnelPillars();

			progress.Set(0.6);

			// 4) 照明：地面火把（本批 14 → 8 格）+ 地面中央的微光走线 + 顶板走线
			for (int x = left + 8; x <= right - 8; x += 8) {
				WorldGen.PlaceTile(x, bottom - 2, TileID.Torches, mute: true, forced: true);
			}

			for (int x = left + 6; x <= right - 6; x += 2) {
				WorldPaint.Retile(x, bottom - 2, x % 6 == 0 ? TileID.DiamondGemspark : TileID.EmeraldGemspark);
			}

			// 顶板也铺一条：60 格高的直道只靠地面灯会有"上半截是黑的"
			for (int x = left + 6; x <= right - 6; x += 2) {
				WorldPaint.Retile(x, top, x % 6 == 0 ? TileID.EmeraldGemspark : TileID.DiamondGemspark);
			}

			progress.Set(0.8);

			// 5) 尽头：封死的合金隔板（"通往未知"就停在这里，后面怎么改还没定）
			WorldPaint.Rect(right + 1, top - shell, right + shell + 4, bottom + shell, Alloy);
			WorldPaint.HLine(right + 1, right + shell + 4, top + 8, Trim);

			progress.Set(1.0);
		}

		// ====================================================================================
		//  收尾
		// ====================================================================================

		/// <summary>框架化（只刷改动过的区域）+ 落点 + 世界分层高度。</summary>
		public static void Finish(GenerationProgress progress, GameConfiguration configuration)
		{
			// 冒烟测试会传 null 进来（不构造 GameConfiguration，免得拉进 Newtonsoft 触发警告）
			progress ??= new GenerationProgress();
			FireplaceGenLog.Start("6/6 收尾");
			progress.Message = "正在校准壁炉的边界……";

			// 地表：从最低的地表高度往上留 8 格，一直刷到世界底
			int highest = FireplaceLayout.SurfaceBase;

			for (int x = 0; x < FireplaceLayout.Width; x++) {
				highest = Math.Min(highest, FireplaceTerrain.Surface[x]);
			}

			WorldPaint.FrameRegion(0, Math.Max(highest - 10, 0), FireplaceLayout.Width - 1, FireplaceLayout.Height - 1);
			progress.Set(0.5);

			// 结构三块：堡垒 / 塔与场地 / 通道 / 椭球地宫
			WorldPaint.FrameRegion(FireplaceLayout.ArenaLeft - 4, FireplaceLayout.ArenaTop - 4,
				FireplaceLayout.ArenaRight + 4, FireplaceLayout.FortBottom + 4);

			WorldPaint.FrameRegion(FireplaceLayout.FortLeft - 4, FireplaceLayout.FortTop - 4,
				FireplaceLayout.FortRight + 4, FireplaceLayout.FortBottom + 4);

			WorldPaint.FrameRegion(FireplaceLayout.TunnelLeft - 6, FireplaceLayout.TunnelTop - 6,
				FireplaceLayout.TunnelRight + 8, FireplaceLayout.TunnelBottom + 6);

			WorldPaint.FrameRegion(FireplaceLayout.VaultLeft - 2, FireplaceLayout.VaultTop - 2,
				FireplaceLayout.VaultRight + 2, FireplaceLayout.VaultBottom + 2);
			progress.Set(0.8);

			// 世界分层：地表与岩层高度（决定背景、太空判定、NPC 生成等）
			Main.worldSurface = FireplaceLayout.SurfaceBase;
			Main.rockLayer = FireplaceLayout.RockTop;

			// 背景样式：这里只是把"刚生成完的那一帧"的初值定下来。
			// ⚠️ 真正每帧生效的是 FireplaceStarfieldSceneEffect（见那边注释：
			// Main.DrawBG 每帧都会用 GetPreferredBGStyleForPlayer() 把 Main.bgStyle 盖掉，
			// 而那个方法只认场景效果带过来的 SurfaceBackgroundStyle）。
			Main.bgStyle = ModContent.GetInstance<FireplaceStarfieldBackground>().Slot;

			// 落点：进门直接站在门厅里
			Main.spawnTileX = FireplaceLayout.SpawnX;

			ModLoader.GetMod("WastelandSoul").Logger.Info(string.Format("[壁炉生成] 全部完成：累计写入 {0} 格；出生点 ({1}, {2})；worldSurface={3}",
				WorldPaint.Writes, Main.spawnTileX, Main.spawnTileY, Main.worldSurface));
			Main.spawnTileY = FireplaceLayout.SpawnY;

			progress.Set(1.0);
		}
	}
}
