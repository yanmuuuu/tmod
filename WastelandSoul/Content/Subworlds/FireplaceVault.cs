using System;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using WastelandSoul.Content.Tiles;

namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 堡垒下方的**椭球地宫**：一个 372×132 的瑜钢合金椭球空腔，隔绝墙 5 格厚，
	/// 内表面每 14 行嵌一条 <see cref="YugangTrim"/> 压条（青银间色）。
	///
	/// <para/>玩家原话：「底下的部分太空旷」+「用通道把每一层的房间的路连起来」。
	/// 所以里面分成 **7 间大小不一的房间**（一层 3 间、二层两翼各 2 间），
	/// 每间一扇原版木门，通路是：
	/// <code>
	///   门厅 → 堡垒竖井（x=524~538）→ 地宫主通道（地下通道伸进来的那一段，y=1131~1188）
	///        → 一层三间（门）→ 西升降井 → 二层阁楼 → 左翼两间（门）
	///        → 东升降井 → 右翼两间（门）
	/// </code>
	///
	/// <para/>为什么不直接写 <c>Main.tile[x, y]</c>：见 <see cref="WorldPaint"/> 的注释
	/// （索引器只读 / 结构体副本两种写法都编译不过或者静默无效），这里一律走 <see cref="WorldPaint"/>。
	///
	/// <para/>⚠️ 生成步骤**只在真正进入子世界时**跑一次，没法在编辑器里试。所以：
	/// <list type="bullet">
	/// <item>所有循环都先过 <see cref="WorldPaint.InWorld"/> 或 <see cref="CanBuild"/>，越界一律跳过；</item>
	/// <item>堡垒投影（<see cref="InFortressBox"/>）与地下通道走廊（<see cref="InTunnelLane"/>）
	/// 是两块**禁区**：一格都不写，保证门厅 / 壁炉 / 终端 / 通道不会被地宫挖穿或堵死；</item>
	/// <item>多格家具（门 / 书架 / 吊灯）放不进去时原版只是静默返回 false，
	/// 所以 <see cref="PlaceDoor"/> / <see cref="PlaceFurniture"/> 会**按实际落位核对**，
	/// 失败就在日志里留一条，绝不留下"看着生成了其实门没放上"的坑；</item>
	/// <item>几何不变量（房间在椭球内、不压禁区、平台与通道走道面齐平……）由
	/// <c>tools/check_fireplace_layout.py</c> 静态核对。</item>
	/// </list>
	/// </summary>
	public static class FireplaceVault
	{
		private static ushort Alloy => (ushort)ModContent.TileType<YugangAlloy>();

		private static ushort Trim => (ushort)ModContent.TileType<YugangTrim>();

		// ====================================================================================
		//  几何判定
		// ====================================================================================

		private static bool InEllipse(int x, int y, float radiusX, float radiusY)
		{
			float dx = (x - FireplaceLayout.VaultCenterX) / radiusX;
			float dy = (y - FireplaceLayout.VaultCenterY) / radiusY;

			return (dx * dx) + (dy * dy) <= 1f;
		}

		/// <summary>椭球外表面以内（含壳）。</summary>
		public static bool InOuterVault(int x, int y)
		{
			return WorldPaint.InWorld(x, y)
				&& InEllipse(x, y, FireplaceLayout.VaultRadiusX, FireplaceLayout.VaultRadiusY);
		}

		/// <summary>椭球内表面以内（不含壳）—— 空腔本体。</summary>
		public static bool InInnerVault(int x, int y)
		{
			return WorldPaint.InWorld(x, y)
				&& InEllipse(x, y,
					FireplaceLayout.VaultRadiusX - FireplaceLayout.VaultShell,
					FireplaceLayout.VaultRadiusY - FireplaceLayout.VaultShell);
		}

		/// <summary>
		/// 堡垒投影（含 1 格外扩）：地宫在这一块里**一格都不写**。
		/// 椭球上缘（y=1018）比堡垒底（1120）还高，不设这块禁区的话壳体会直接砌进门厅里。
		/// </summary>
		public static bool InFortressBox(int x, int y)
		{
			return x >= FireplaceLayout.FortLeft - 1
				&& x <= FireplaceLayout.FortRight + 1
				&& y <= FireplaceLayout.FortBottom + 1;
		}

		/// <summary>
		/// 地下通道的走廊带（含内衬与端头）：地宫不在这里填任何东西，
		/// 通道必须一路是通的（椭球壳会正好压在通道上，靠后面的"重开通道嘴"处理）。
		/// </summary>
		public static bool InTunnelLane(int x, int y)
		{
			return x >= FireplaceLayout.TunnelLeft - FireplaceLayout.TunnelShell
				&& y >= FireplaceLayout.TunnelTop - FireplaceLayout.TunnelShell
				&& y <= FireplaceLayout.TunnelBottom + FireplaceLayout.TunnelShell;
		}

		/// <summary>
		/// **堡垒竖井**那一条（含两侧压条）——地宫的补板要绕开它。
		///
		/// <para/>⚠️ 这是一个真 bug 的修法（玩家反馈"进地宫的路被合金堵死"）：
		/// 旧顺序是 <see cref="OpenFortressShaft"/>（开挖）→ <see cref="BuildSlab"/>。
		/// 而 `InFortressBox` 的判定是 `y &lt;= FortBottom + 1 = 1121`，
		/// 竖井的 y=1122~1125 这 4 行既不在堡垒盒里、也不在通道带里
		/// （通道带从 TunnelTop-Shell=1126 起），于是 `BuildSlab` 的"东侧补板"
		/// 正好把 **15 列 × 4 行 = 60 格合金**填回竖井里 —— 门厅到地下通道/地宫**彻底不通**
		/// （几何复刻 + 洪水填充：82686 格可达，竖井、通道、7 间房全部不可达）。
		/// </summary>
		private static bool InFortressShaft(int x, int y)
		{
			return x >= FireplaceLayout.FortShaftLeft - 1
				&& x <= FireplaceLayout.FortShaftRight + 1
				&& y >= FireplaceLayout.HallBottom
				&& y <= FireplaceLayout.TunnelTop + 1;
		}

		/// <summary>地宫结构可以写的格子：在椭球内、且不碰堡垒与通道两块禁区。</summary>
		private static bool CanBuild(int x, int y)
		{
			return InInnerVault(x, y) && !InFortressBox(x, y) && !InTunnelLane(x, y);
		}

		// ====================================================================================
		//  主流程
		// ====================================================================================

		public static void Build(GenerationProgress progress, GameConfiguration configuration)
		{
			// 冒烟测试会传 null 进来（不构造 GameConfiguration，免得拉进 Newtonsoft 触发警告）
			progress ??= new GenerationProgress();
			FireplaceGenLog.Start("5/6 椭球地宫");
			progress.Message = "正在浇铸堡垒下方的椭球地宫……";

			OpenFortressShaft();
			progress.Set(0.10);

			BuildShell();
			progress.Set(0.30);

			BuildCavity();
			BuildWalls();
			progress.Set(0.45);

			BuildSlab();
			BuildPlatform();
			progress.Set(0.60);

			BuildStoryWalls();
			progress.Set(0.68);

			BuildWings();
			progress.Set(0.78);

			BuildLadders();
			progress.Set(0.86);

			Decorate();
			progress.Set(0.92);

			// ⚠️ 必须放在最后：`BuildCavity` 的"重开通道嘴"是把通道内空整段再掏一遍
			// （椭球壳会正好压在通道上），它顺手也会把**通道自己的立柱**（瑜钢压条，
			// 一层 1818 格）一起挖掉 —— 几何复刻数出来的。立柱归通道那一步管，
			// 所以掏完嘴之后按同一套写法补回来（幂等，重复进生成也不会叠）。
			FireplaceBuildings.BuildTunnelPillars();
			progress.Set(1.0);

			FireplaceGenLog.Note(string.Format("地宫：房间 {0} 间，椭球 {1}x{2} @ ({3}, {4})，壳厚 {5}",
				FireplaceLayout.VaultRooms.Length,
				FireplaceLayout.VaultRadiusX * 2, FireplaceLayout.VaultRadiusY * 2,
				FireplaceLayout.VaultCenterX, FireplaceLayout.VaultCenterY,
				FireplaceLayout.VaultShell));
			FireplaceGenLog.Done("5/6 椭球地宫");
		}

		/// <summary>
		/// 打通堡垒竖井：门厅地板（<see cref="FireplaceLayout.HallBottom"/>）那一行原本是合金，
		/// 竖井只挖到 <c>TunnelTop - 2</c>，通道顶壳又被通道那一步重新砌上，
		/// 于是"门厅 → 竖井 → 通道"中间隔着两层，谁也下不去。
		/// 这里一次挖到通道内空（<c>TunnelTop + 1</c>），地宫才有唯一的入口。
		/// </summary>
		private static void OpenFortressShaft()
		{
			WorldPaint.Carve(FireplaceLayout.FortShaftLeft, FireplaceLayout.HallBottom,
				FireplaceLayout.FortShaftRight, FireplaceLayout.TunnelTop + 1);

			// 井口的青银缘口：两侧各一条压条，下来的人一眼能看出这是"入口"
			WorldPaint.VLine(FireplaceLayout.FortShaftLeft - 2, FireplaceLayout.VaultSlabTop,
				FireplaceLayout.TunnelTop, Trim);
			WorldPaint.VLine(FireplaceLayout.FortShaftRight + 2, FireplaceLayout.VaultSlabTop,
				FireplaceLayout.TunnelTop, Trim);
		}

		/// <summary>椭球外壳：5 格厚合金，内表面每 14 行一条压条（青银间色）。</summary>
		private static void BuildShell()
		{
			int step = FireplaceLayout.VaultTrimStep;

			for (int y = FireplaceLayout.VaultTop; y <= FireplaceLayout.VaultBottom; y++) {
				bool band = (y - FireplaceLayout.VaultTop) % step == 0;

				for (int x = FireplaceLayout.VaultLeft; x <= FireplaceLayout.VaultRight; x++) {
					if (!InOuterVault(x, y) || InInnerVault(x, y) || InFortressBox(x, y)) {
						continue;
					}

					WorldPaint.Retile(x, y, band ? Trim : Alloy);
				}
			}
		}

		/// <summary>
		/// 掏出空腔（平台底面以上），随后把被壳体压住的地下通道内空**重开一遍**：
		/// 通道从世界边缘伸进来，椭球壳会正好砌在它嘴上，不重开就走不进来。
		/// </summary>
		private static void BuildCavity()
		{
			int bottom = FireplaceLayout.VaultFloorY + FireplaceLayout.VaultPlatformThickness - 1;

			for (int y = FireplaceLayout.VaultTop; y <= bottom; y++) {
				for (int x = FireplaceLayout.VaultLeft; x <= FireplaceLayout.VaultRight; x++) {
					if (!CanBuild(x, y)) {
						continue;
					}

					WorldPaint.ClearTile(x, y);
				}
			}

			// 通道嘴：只重开内空（不动通道自己的壳与压条，通道仍是"笔直一条"）。
			// 挖到 TunnelBottom-2 为止：再往下那一行是通道的走道面压条，留着不挖，
			// 免得通道尽头出现一格台阶。
			WorldPaint.Carve(FireplaceLayout.TunnelLeft, FireplaceLayout.TunnelTop + 1,
				FireplaceLayout.TunnelRight, FireplaceLayout.TunnelBottom - 2);
		}

		/// <summary>
		/// 背景墙：椭球范围内一律铺原版**地狱砖墙**（<c>WallID.HellstoneBrick</c>，安全墙），
		/// 空腔看着才像"浇铸出来的设施"而不是天然洞；堡垒投影那一段不动（门厅的墙照旧）。
		/// </summary>
		private static void BuildWalls()
		{
			for (int y = FireplaceLayout.VaultTop; y <= FireplaceLayout.VaultBottom; y++) {
				for (int x = FireplaceLayout.VaultLeft; x <= FireplaceLayout.VaultRight; x++) {
					if (!InOuterVault(x, y) || InFortressBox(x, y)) {
						continue;
					}

					WorldPaint.SetWall(x, y, WallID.HellstoneBrick);
				}
			}
		}

		/// <summary>
		/// 一层天花板 / 二层楼板：整段填合金，房间的墙与门就从这块楼板里"抠"出来。
		///
		/// <para/>⚠️ 两条修补（都是几何复刻 + 洪水填充抓出来的）：
		/// <list type="number">
		/// <item>只写 <see cref="CanBuild"/> 之外还要绕开**堡垒竖井**（见
		/// <see cref="InFortressShaft"/>）：竖井的 y=1122~1125 落在堡垒盒与通道带之间的
		/// 缝里，不绕开就会被这块补板填死；</item>
		/// <item>顺手把 `y = FortBottom + 1 = 1121` 那一行**补实**：堡垒那一步的
		/// `Carve(left-1, top-1, right+1, bottom+2)` 在壳体外面留了 1 格施工余量
		/// （y=1121、x=279/621 都是空气），而 `InFortressBox` 保护的是 `y &lt;= 1121`，
		/// 于是楼板从 1122 起铺 —— 中间留了一条 1 格高的水平空腔（复刻里 339 列）。
		/// 补上它，堡垒底壳就真的坐在楼板上。</item>
		/// </list>
		/// </summary>
		private static void BuildSlab()
		{
			// 中间楼板（左端留出通往阁楼的观景口：VaultSlabLeft ~ VaultSlabRight）
			for (int y = FireplaceLayout.VaultSlabTop; y <= FireplaceLayout.VaultSlabBottom; y++) {
				for (int x = FireplaceLayout.VaultSlabLeft; x <= FireplaceLayout.VaultSlabRight; x++) {
					if (!CanBuild(x, y) || InFortressShaft(x, y)) {
						continue;
					}

					WorldPaint.Retile(x, y, Alloy);
				}
			}

			// 东侧补板：通道顶壳（TunnelTop-4 起）以上那 5 行，托住二层右翼的房间
			for (int y = FireplaceLayout.VaultSlabTop; y < FireplaceLayout.TunnelTop; y++) {
				for (int x = FireplaceLayout.TunnelLeft; x <= FireplaceLayout.VaultRight; x++) {
					if (!CanBuild(x, y) || InFortressShaft(x, y)) {
						continue;
					}

					WorldPaint.Retile(x, y, Alloy);
				}
			}

			// 堡垒底壳与楼板之间那条 1 格空腔：只补现在还是空气的格子（不动竖井）
			for (int x = FireplaceLayout.FortLeft; x <= FireplaceLayout.FortRight; x++) {
				int y = FireplaceLayout.FortBottom + 1;

				if (InFortressShaft(x, y) || WorldPaint.HasTile(x, y)) {
					continue;
				}

				WorldPaint.Retile(x, y, Alloy);
			}
		}

		/// <summary>主平台：地宫一层的整块地面，顶面与地下通道的走道面齐平（检查器核对）。</summary>
		private static void BuildPlatform()
		{
			int bottom = FireplaceLayout.VaultFloorY + FireplaceLayout.VaultPlatformThickness - 1;

			for (int y = FireplaceLayout.VaultFloorY; y <= bottom; y++) {
				for (int x = FireplaceLayout.VaultLeft; x <= FireplaceLayout.VaultRight; x++) {
					if (!CanBuild(x, y)) {
						continue;
					}

					WorldPaint.Retile(x, y, y == FireplaceLayout.VaultFloorY ? Trim : Alloy);
				}
			}

			// 通道嘴那一段的地面在重开通道时被挖掉了，这里按同一条走道面补回来（一路平）
			WorldPaint.HLine(FireplaceLayout.TunnelLeft, FireplaceLayout.VaultRight,
				FireplaceLayout.VaultFloorY, Trim);
		}

		// ====================================================================================
		//  一层：分隔墙 + 门
		// ====================================================================================

		private static void BuildStoryWalls()
		{
			FireplaceLayout.VaultRoom[] rooms = FireplaceLayout.VaultRooms;
			int top = FireplaceLayout.VaultStoryTop;
			int bottom = FireplaceLayout.VaultFloorY - 1;

			if (rooms.Length < 3) {
				return;
			}

			// 三间房之间两道隔墙，房间东头与主通道之间再一道
			int wallA = rooms[0].Right + 1;
			int wallB = rooms[1].Left - 1;
			int wallC = rooms[1].Right + 1;
			int wallD = rooms[2].Left - 1;
			int wallE = rooms[2].Right + 1;
			int wallF = FireplaceLayout.TunnelLeft - FireplaceLayout.TunnelShell - 1;

			FillWall(wallA, wallB, top, bottom);
			FillWall(wallC, wallD, top, bottom);
			FillWall(wallE, wallF, top, bottom);

			// 每道隔墙正中一扇原版木门（门下方就是平台顶面；门洞要打穿整堵墙）。
			// ⚠️ 东头那道门还得**再往东打 4 格**：x=516~519 是地下通道自己的侧壳
			// （通道那一步砌的，5 格厚墙之外的额外一层），门洞不打通它，人就出不去。
			int doorEast = FireplaceLayout.TunnelLeft - 1;

			PlaceDoor((wallA + wallB) / 2, FireplaceLayout.VaultFloorY, wallA, wallB);
			PlaceDoor((wallC + wallD) / 2, FireplaceLayout.VaultFloorY, wallC, wallD);
			PlaceDoor((wallE + doorEast) / 2, FireplaceLayout.VaultFloorY, wallE, doorEast);
		}

		/// <summary>填一道隔墙：两侧外皮用压条（青银相间），中间是本体合金。</summary>
		private static void FillWall(int left, int right, int top, int bottom)
		{
			if (right < left) {
				return;
			}

			for (int x = left; x <= right; x++) {
				ushort type = (x == left || x == right) ? Trim : Alloy;

				for (int y = top; y <= bottom; y++) {
					if (!InInnerVault(x, y)) {
						continue;
					}

					WorldPaint.Retile(x, y, type);
				}
			}
		}

		// ====================================================================================
		//  二层：两翼的房间
		// ====================================================================================

		private static void BuildWings()
		{
			FireplaceLayout.VaultRoom[] rooms = FireplaceLayout.VaultRooms;

			if (rooms.Length < 7) {
				return;
			}

			// 左翼（168~270）：外墙 162~278，靠阁楼那面（西）开门
			BuildWing(162, 278, rooms[3], rooms[4], true);
			// 右翼（630~732）：外墙 624~738，靠阁楼那面（东）开门
			BuildWing(624, 738, rooms[5], rooms[6], false);
		}

		private static void BuildWing(int outerLeft, int outerRight, FireplaceLayout.VaultRoom west,
			FireplaceLayout.VaultRoom east, bool atticDoorOnWest)
		{
			int top = FireplaceLayout.VaultCeilingTop;
			int bottom = FireplaceLayout.VaultUpperBottom;

			// 顶板
			for (int y = top; y <= FireplaceLayout.VaultCeilingBottom; y++) {
				for (int x = outerLeft; x <= outerRight; x++) {
					if (!InInnerVault(x, y)) {
						continue;
					}

					WorldPaint.Retile(x, y, Alloy);
				}
			}

			// 两翼：外墙 / 中间隔墙 / 外墙
			int wallA = outerLeft;
			int wallB = west.Left - 1;
			int wallC = west.Right + 1;
			int wallD = east.Left - 1;
			int wallE = east.Right + 1;
			int wallF = outerRight;

			FillWall(wallA, wallB, top, bottom);
			FillWall(wallC, wallD, top, bottom);
			FillWall(wallE, wallF, top, bottom);

			// 两间之间一扇门（门洞打穿整堵隔墙）
			PlaceDoor((wallC + wallD) / 2, FireplaceLayout.VaultSlabTop, wallC, wallD);

			// 靠阁楼那面外墙再开一扇（阁楼是从升降井上来的，得能进房间）
			if (atticDoorOnWest) {
				PlaceDoor(wallA + 1, FireplaceLayout.VaultSlabTop, wallA, wallB);
			}
			else {
				PlaceDoor(wallF - 1, FireplaceLayout.VaultSlabTop, wallE, wallF);
			}
		}

		// ====================================================================================
		//  竖井 / 升降井
		// ====================================================================================

		/// <summary>
		/// 两条升降井：西井从一层西头房间里打穿楼板通到二层阁楼；
		/// 东井从主通道（通道顶壳）打穿到二层右翼房间。井里每 5 格一层原版木平台当梯子
		/// （和祈塔里的爬升做法一致）。
		/// </summary>
		private static void BuildLadders()
		{
			LadderShaft(FireplaceLayout.VaultWestLadderX, FireplaceLayout.VaultWestLadderRight,
				FireplaceLayout.VaultFloorY - 1, FireplaceLayout.VaultUpperTop);
			LadderShaft(FireplaceLayout.VaultEastLadderX, FireplaceLayout.VaultEastLadderRight,
				FireplaceLayout.VaultFloorY - 1, FireplaceLayout.VaultUpperTop);
		}

		private static void LadderShaft(int left, int right, int floorY, int topY)
		{
			if (right < left || topY > floorY) {
				return;
			}

			for (int y = topY; y <= floorY; y++) {
				for (int x = left; x <= right; x++) {
					if (!WorldPaint.InWorld(x, y)) {
						continue;
					}

					WorldPaint.ClearTile(x, y);
				}
			}

			for (int y = floorY - 4; y >= topY; y -= 5) {
				for (int x = left; x <= right; x++) {
					if (!WorldPaint.InWorld(x, y)) {
						continue;
					}

					WorldGen.PlaceTile(x, y, TileID.Platforms, mute: true, forced: true);
				}
			}
		}

		// ====================================================================================
		//  内饰：照明 / 书架 / 桌椅 / 藤蔓
		// ====================================================================================

		private static void Decorate()
		{
			FireplaceLayout.VaultRoom[] rooms = FireplaceLayout.VaultRooms;
			int torches = 0;
			int chandeliers = 0;
			int bookcases = 0;
			int vines = 0;

			foreach (FireplaceLayout.VaultRoom room in rooms) {
				bool upper = room.Top == FireplaceLayout.VaultUpperTop;
				int floorY = upper ? FireplaceLayout.VaultSlabTop : FireplaceLayout.VaultFloorY;
				int ceilingY = upper ? FireplaceLayout.VaultCeilingBottom : FireplaceLayout.VaultSlabBottom;

				// 地面火把：贴着地板，每 12 格一盏（本批从 16 加密到 12：玩家反馈整体偏暗）
				for (int x = room.Left + 3; x <= room.Right - 3; x += 12) {
					if (!RoomAir(room, x, floorY - 1)) {
						continue;
					}

					WorldGen.PlaceTile(x, floorY - 1, TileID.Torches, mute: true, forced: true);

					if (WorldPaint.IsTile(x, floorY - 1, TileID.Torches)) {
						torches++;
					}
				}

				// 地面指示灯：翡翠微光**连着铺**（每 2 格一格，玩家要的"连续微光走线"）
				for (int x = room.Left + 2; x <= room.Right - 2; x += 2) {
					if (RoomAir(room, x, floorY)) {
						continue;
					}

					WorldPaint.Retile(x, floorY, x % 6 == 0
						? TileID.DiamondGemspark
						: TileID.EmeraldGemspark);
				}

				// 天花板走线：钻石微光（比翡翠亮一档）沿顶板铺一条，房间轮廓一眼可见。
				// ⚠️ 两条升降井的竖井穿过这块楼板/顶板（x=152~155 与 702~705），
				// 在那儿铺方块会把井道堵死，必须让开。
				for (int x = room.Left + 1; x <= room.Right - 1; x += 2) {
					if (InLadderColumn(x)) {
						continue;
					}

					WorldPaint.Retile(x, ceilingY, x % 4 == 0
						? TileID.EmeraldGemspark
						: TileID.DiamondGemspark);
				}

				// 吊灯：从天花板上垂下来（每 20 格一盏，本批从 28 加密）
				for (int x = room.Left + 8; x <= room.Right - 8; x += 20) {
					if (PlaceHanging(x, ceilingY, TileID.Chandeliers)) {
						chandeliers++;
					}
				}

				// 长明烛台：每 18 格一盏（矮，不挡视线，但把地面提亮）
				for (int x = room.Left + 9; x <= room.Right - 9; x += 18) {
					if (RoomAir(room, x, floorY - 1)) {
						WorldGen.PlaceTile(x, floorY - 1, TileID.Candles, mute: true, forced: true);
					}
				}

				// 书架（3×4）：贴墙摆一两组
				if (room.Width >= 60) {
					if (PlaceFurniture(room, room.Left + 12, floorY, TileID.Bookcases, 3, 4)) {
						bookcases++;
					}

					if (PlaceFurniture(room, room.Right - 12, floorY, TileID.Bookcases, 3, 4)) {
						bookcases++;
					}
				}

				// 桌椅：旧时代的驻地感
				PlaceFurniture(room, room.Left + room.Width / 2, floorY, TileID.Tables, 3, 2);

				// 藤蔓：从天花板下沿垂下来（原版藤蔓要附在方块下方，逐格放）
				for (int x = room.Left + 6; x <= room.Right - 6; x += 20) {
					vines += PlaceVines(x, ceilingY + 1, 4 + (x % 5));
				}
			}

			// 主通道（地下通道伸进来那一段）也补一轮照明：火把每 12 格、地面连铺微光
			for (int x = FireplaceLayout.TunnelLeft + 20; x <= FireplaceLayout.VaultRight - 10; x += 12) {
				WorldGen.PlaceTile(x, FireplaceLayout.VaultFloorY - 1, TileID.Torches, mute: true, forced: true);

				if (WorldPaint.IsTile(x, FireplaceLayout.VaultFloorY - 1, TileID.Torches)) {
					torches++;
				}
			}

			for (int x = FireplaceLayout.TunnelLeft + 6; x <= FireplaceLayout.VaultRight - 6; x += 2) {
				WorldPaint.Retile(x, FireplaceLayout.VaultFloorY, x % 6 == 0
					? TileID.DiamondGemspark
					: TileID.EmeraldGemspark);
			}

			// 阁楼（二层顶上那圈穹顶空间）里沿壳挂几盏 —— 壳的内表面就是穹顶，
			// 找到每列最下面那一格壳体、从它下面开始挂。
			for (int x = FireplaceLayout.VaultCenterX - 260; x <= FireplaceLayout.VaultCenterX + 260; x += 55) {
				int ceiling = AtticCeiling(x);

				if (ceiling > 0) {
					vines += PlaceVines(x, ceiling, 5);
				}
			}

			FireplaceGenLog.Note(string.Format("地宫照明：火把 {0}、吊灯 {1}、书架 {2}、藤蔓 {3} 格",
				torches, chandeliers, bookcases, vines));
		}

		/// <summary>这一列是不是某条升降井的井道（±1 格余量，避免把井口铺死）。</summary>
		private static bool InLadderColumn(int x)
		{
			return (x >= FireplaceLayout.VaultWestLadderX - 1 && x <= FireplaceLayout.VaultWestLadderRight + 1)
				|| (x >= FireplaceLayout.VaultEastLadderX - 1 && x <= FireplaceLayout.VaultEastLadderRight + 1);
		}

		/// <summary>这一格在这间房里、而且现在是空的（家具/火把只能往空处放）。</summary>
		private static bool RoomAir(FireplaceLayout.VaultRoom room, int x, int y)
		{
			return x >= room.Left && x <= room.Right && y >= room.Top && y <= room.Bottom
				&& WorldPaint.InWorld(x, y) && !WorldPaint.HasTile(x, y);
		}

		/// <summary>穹顶在这一列的内表面行号（第一格内空）；这一列被堡垒投影挡着就返回 -1。</summary>
		private static int AtticCeiling(int x)
		{
			if (!WorldPaint.InWorld(x, 0)) {
				return -1;
			}

			for (int y = FireplaceLayout.VaultTop; y <= FireplaceLayout.VaultUpperBottom; y++) {
				if (InFortressBox(x, y)) {
					return -1;
				}

				if (InInnerVault(x, y)) {
					return y;
				}
			}

			return -1;
		}

		/// <summary>
		/// 放一扇原版木门。
		///
		/// <para/>⚠️ 这里的关键不是"把门放上去"，而是**门洞要打穿整堵墙**：
		/// 隔墙有 5~6 格厚，如果只在墙中间放一格门、两侧墙体不动，玩家根本走不过去
		/// （门格被实心墙夹在中间）。所以先按 <paramref name="carveLeft"/>~<paramref name="carveRight"/>
		/// 把整段墙在这 3 行上掏通，再把门放在中间那一列 —— 这一列就成了墙里唯一的通路。
		///
		/// <para/>门本身的"锚点约定"在不同版本里是"底部那一格"或"顶上一格"两种，
		/// 而且放不下时 <c>WorldGen.PlaceObject</c> 只是静默返回 false，
		/// 所以两种锚点都试，并且用"门格有没有真的出现在该在的地方"来判定。
		/// </summary>
		/// <param name="x">门所在列（必须在 <paramref name="carveLeft"/>~<paramref name="carveRight"/> 中间）。</param>
		/// <param name="floorY">门下方那一格实心地板的行号；门应当占 floorY-3 ~ floorY-1。</param>
		/// <param name="carveLeft">这堵墙的左边（含）。</param>
		/// <param name="carveRight">这堵墙的右边（含）。</param>
		private static bool PlaceDoor(int x, int floorY, int carveLeft, int carveRight)
		{
			int bottom = floorY - 1;

			for (int attempt = 0; attempt < 2; attempt++) {
				int anchor = attempt == 0 ? bottom : bottom - 2;

				// 门洞：整堵墙在这 3 行上掏通（门就卡在中间那一列）
				WorldPaint.Carve(carveLeft, bottom - 2, carveRight, bottom);
				WorldGen.PlaceObject(x, anchor, TileID.ClosedDoor, true, 0, 0, -1, -1);

				if (DoorColumn(x, bottom)) {
					return true;
				}

				if (DoorColumn(x, bottom - 2)) {
					return true;
				}
			}

			FireplaceGenLog.Note(string.Format("门没放上：(x={0}, 地板 y={1})", x, floorY));
			return false;
		}

		private static bool DoorColumn(int x, int bottomY)
		{
			for (int i = 0; i < 3; i++) {
				if (!WorldPaint.IsTile(x, bottomY - i, TileID.ClosedDoor)) {
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// 放一件原版多格家具（桌子 3×2 / 书架 3×4）。锚点约定同样两种都试，
		/// 再用"目标区域里到底有没有这个方块"核对；清空间只清房间内空，不动墙与门。
		/// </summary>
		private static bool PlaceFurniture(FireplaceLayout.VaultRoom room, int centerX, int floorY,
			ushort type, int width, int height)
		{
			int half = width / 2;

			for (int attempt = 0; attempt < 3; attempt++) {
				int anchor = floorY - (attempt * Math.Max(height - 1, 1));

				// 清空间只清"这间房的内空"（不动墙、门、别的房间）
				for (int x = centerX - half; x <= centerX + half; x++) {
					for (int y = anchor - height - 1; y <= anchor + 1; y++) {
						if (x < room.Left || x > room.Right || y < room.Top || y > room.Bottom) {
							continue;
						}

						if (!InInnerVault(x, y) || InTunnelLane(x, y)) {
							continue;
						}

						WorldPaint.ClearTile(x, y);
					}
				}

				WorldGen.PlaceObject(centerX, anchor, type, true, 0, 0, -1, -1);

				if (AnyTileIn(centerX - width, anchor - height - 1, centerX + width, anchor + 2, type)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>放吊灯这类"挂在顶板下面"的 3×3：锚点在顶行，两种情况都试。</summary>
		private static bool PlaceHanging(int centerX, int ceilingBottomY, ushort type)
		{
			for (int attempt = 0; attempt < 2; attempt++) {
				int anchor = ceilingBottomY + 1 + attempt;

				WorldGen.PlaceObject(centerX, anchor, type, true, 0, 0, -1, -1);

				if (AnyTileIn(centerX - 1, anchor - 1, centerX + 1, anchor + 3, type)) {
					return true;
				}
			}

			return false;
		}

		private static bool AnyTileIn(int left, int top, int right, int bottom, ushort type)
		{
			for (int x = left; x <= right; x++) {
				for (int y = top; y <= bottom; y++) {
					if (WorldPaint.IsTile(x, y, type)) {
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>从某一行往下挂一条原版藤蔓（藤蔓要附着在方块下方，逐格放、放不上就停）。</summary>
		private static int PlaceVines(int x, int topY, int length)
		{
			int placed = 0;

			for (int i = 0; i < length; i++) {
				int y = topY + i;

				if (!WorldPaint.InWorld(x, y) || WorldPaint.HasTile(x, y)) {
					break;
				}

				WorldGen.PlaceTile(x, y, TileID.Vines, mute: true, forced: true);

				if (!WorldPaint.IsTile(x, y, TileID.Vines)) {
					break;
				}

				placed++;
			}

			return placed;
		}
	}
}
