namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 「壁炉」子世界的**布局常量**：生成器、剧情触发点、门/终端位置全部从这里取，
	/// 避免同一个坐标写死在几个文件里（改尺寸时只改这一处）。
	///
	/// <para/>世界尺寸 **900 × 1300**。竖直方向的分层（这一批把灰烬地表从 900 抬到了 **840**，
	/// 于是堡垒整体被埋进灰烬底下，只有祈塔从灰烬里钻出来）：
	/// <code>
	///   y=0    ~ 138   太空
	///   y=138  ~ 288   塔顶 Boss 场地（整块都在太空线 worldSurface×0.35=294 以上）
	///   y=288  ~ 300   塔顶接驳颈（场地抬高了 12 格，这里补一段空心合金脖子接回塔身）
	///   y=300  ~ 860   塔身（14 层，青银相间的瑜钢合金壳）
	///   y=840          灰烬地表基准（起伏 ±26）
	///   y=860  ~ 1120  堡垒（**全埋**在灰烬下：屋顶 860 在地表 840 以下 20 格）
	///   y=1130 ~ 1190  地下通道（笔直、60 格高，通到世界边缘的"未知"）
	///   y=1018 ~ 1282  椭球地宫（堡垒下方的合金空腔，见 <see cref="FireplaceVault"/>）
	///   y=1295 ~ 1300  世界底部的实心岩层
	/// </code>
	///
	/// <para/>椭球地宫的几何全部由 <see cref="FireplaceVault"/> 消费，房间清单写在
	/// <see cref="VaultRooms"/> 里；<c>tools/check_fireplace_layout.py</c> 会把下面这些
	/// 数字读出来核对（房间必须在椭球内、不能压到堡垒与地下通道、每间都要有门）。
	/// </summary>
	public static class FireplaceLayout
	{
		// ---------------- 世界 ----------------
		public const int Width = 900;
		public const int Height = 1300;

		/// <summary>灰烬地表的基准高度（本批从 900 抬到 840：堡垒外的方块填得更高，堡垒只剩塔尖露头）。</summary>
		public const int SurfaceBase = 840;

		/// <summary>地表起伏幅度（格）。</summary>
		public const int SurfaceAmplitude = 26;

		/// <summary>灰烬 / 土层厚度，之下转岩石。</summary>
		public const int CrustDepth = 110;

		/// <summary>岩石层起始高度（对应 <c>Main.rockLayer</c>）。</summary>
		public const int RockTop = SurfaceBase + CrustDepth;

		// ---------------- 堡垒 ----------------
		// 位置不变：地表抬到 840 之后，屋顶 860 落在地表以下 20 格 -> 堡垒整体埋进灰烬里，
		// 只有祈塔从地表钻出来（塔身 300~860，塔基照旧坐在堡垒屋顶上）。
		public const int FortLeft = 280;
		public const int FortRight = 620;
		public const int FortTop = 860;
		public const int FortBottom = 1120;

		/// <summary>门厅（室内可站立空间）。</summary>
		public const int HallLeft = 320;
		public const int HallRight = 580;
		public const int HallTop = 900;
		public const int HallBottom = 1080;

		/// <summary>壁炉火塘的左右边界（门厅正中）。</summary>
		public const int HearthLeft = 420;
		public const int HearthRight = 480;

		/// <summary>数据终端所在的 y。</summary>
		public const int TerminalY = 1024;

		/// <summary>数据终端中心 x。</summary>
		public const int TerminalX = 520;

		/// <summary>返回门（主世界出口）中心 x。</summary>
		public const int ExitX = 356;

		/// <summary>堡垒外壳厚度（玩家要求隔绝墙加厚）。</summary>
		public const int FortShell = 5;

		/// <summary>
		/// 堡垒通往下方的**竖井**左右边（门厅地板 → 地下通道，地宫靠它接入）。
		/// <para/>⚠️ 这一段竖井原本只挖到 <c>TunnelTop - 2</c>，会被通道自己的顶壳堵住一格；
		/// 地宫那一步（<see cref="FireplaceVault.Build"/>）会把整段竖井重新打通。
		/// </summary>
		public const int FortShaftLeft = TunnelLeft + 4;
		public const int FortShaftRight = TunnelLeft + 18;

		// ---------------- 高塔 ----------------
		// 玩家反馈「塔太宽了、没什么东西要放」-> 从 140 宽收到 40 宽（塔顶战斗平台尺寸不变）
		public const int TowerLeft = 430;
		public const int TowerRight = 470;
		public const int TowerTop = 300;
		public const int TowerBottom = 860;

		/// <summary>
		/// 塔身层高（每层一块合金楼板）。
		/// <para/>⚠️ 必须能整除塔身高度（860-300=560）—— 否则最上面会留半层楼板，
		/// 这个不变量由 <c>tools/check_fireplace_layout.py</c> 盯着。560/40 = **14 层**。
		/// </summary>
		public const int TowerFloorHeight = 40;

		/// <summary>塔壁厚度。</summary>
		public const int TowerShell = 2;

		// ---------------- 塔顶 Boss 场地 ----------------
		// 地表抬到 840 之后太空线变成 840×0.35 = 294，场地整块上移 12 格（尺寸 300×150 不变），
		// 保证 ArenaBottom=288 仍然在太空线以上。
		public const int ArenaLeft = 300;
		public const int ArenaRight = 600;
		public const int ArenaTop = 138;
		public const int ArenaBottom = 288;

		/// <summary>场地与塔身之间的接驳颈（空心合金，16 格高）。</summary>
		public const int ArenaNeckLeft = TowerLeft + 2;
		public const int ArenaNeckRight = TowerRight - 2;

		// ---------------- 地下通道 ----------------
		public const int TunnelLeft = 520;
		public const int TunnelRight = Width - 8;
		public const int TunnelTop = 1130;
		public const int TunnelBottom = 1190;

		/// <summary>通道内衬厚度（同上，加厚）。</summary>
		public const int TunnelShell = 4;

		// ---------------- 椭球地宫 ----------------
		/// <summary>椭球中心。</summary>
		public const int VaultCenterX = 450;
		public const int VaultCenterY = 1150;

		/// <summary>
		/// 椭球半径。
		/// <para/>玩家给的是「约 380×190」，但 190 的半轴在这个 1300 高的世界里放不下
		/// （中心 1160 + 190 = 1350 &gt; 1299），而且会把堡垒、地下通道全吃进去，
		/// 所以按「能放下、留出世界边缘余量、避开堡垒与通道」重算成 **372×132**：
		/// 横向 x=78~822（离世界两边各留 77+ 格），纵向 y=1018~1282（离世界底留 17 格）。
		/// </summary>
		public const int VaultRadiusX = 372;
		public const int VaultRadiusY = 132;

		/// <summary>隔绝墙（合金壳）厚度：玩家要求 4~6 格厚。</summary>
		public const int VaultShell = 5;

		/// <summary>内表面墙裙（压条）的行距：玩家要求 12~16 行一条。</summary>
		public const int VaultTrimStep = 14;

		public const int VaultLeft = VaultCenterX - VaultRadiusX;
		public const int VaultRight = VaultCenterX + VaultRadiusX;
		public const int VaultTop = VaultCenterY - VaultRadiusY;
		public const int VaultBottom = VaultCenterY + VaultRadiusY;

		/// <summary>
		/// 主平台（地宫一层地面）的**顶面行**。
		/// <para/>刻意对齐地下通道的走道面（通道地板压条在 <c>TunnelBottom - 1</c>），
		/// 这样从竖井下来、走出通道都是一路平的（检查器会核对这条等式）。
		/// </summary>
		public const int VaultFloorY = TunnelBottom - 1;

		/// <summary>主平台厚度（顶面那一行算在内）。</summary>
		public const int VaultPlatformThickness = 6;

		// ---- 一层（主层）----
		/// <summary>一层房间内空的顶行（天花板下沿）。</summary>
		public const int VaultStoryTop = 1140;

		/// <summary>一层天花板 / 二层楼板：这一整段填实心合金（房间的墙就靠它"抠"出来）。</summary>
		public const int VaultSlabTop = 1121;
		public const int VaultSlabBottom = 1139;

		/// <summary>中间楼板的横向范围（左端留出观景口，右端接东侧升降井）。</summary>
		public const int VaultSlabLeft = 130;
		public const int VaultSlabRight = 623;

		/// <summary>东侧升降井（通道顶壳上开洞，通到二层右翼的房间）。</summary>
		public const int VaultEastLadderX = 702;
		public const int VaultEastLadderRight = 705;

		/// <summary>西侧升降井（一层西头房间里开洞，直通二层阁楼）。</summary>
		public const int VaultWestLadderX = 152;
		public const int VaultWestLadderRight = 155;

		// ---- 二层（两翼）----
		/// <summary>二层房间内空的上下界。</summary>
		public const int VaultUpperTop = 1078;
		public const int VaultUpperBottom = 1120;

		/// <summary>二层房间的顶板。</summary>
		public const int VaultCeilingTop = 1075;
		public const int VaultCeilingBottom = 1077;

		// ---- 一层分隔墙 ----
		// 房间之间不留缝：分隔墙的范围就是「左房右界+1 ~ 右房左界-1」，
		// 门开在墙中间那一列。
		public const int VaultDoorFloor = VaultFloorY - 1;

		/// <summary>椭球地宫里的一个房间（记录的是**内空**范围）。</summary>
		public readonly struct VaultRoom
		{
			/// <summary>房间名（只用于日志与检查器）。</summary>
			public readonly string Name;

			/// <summary>内空左边界。</summary>
			public readonly int Left;

			/// <summary>内空顶行。</summary>
			public readonly int Top;

			/// <summary>内空右边界。</summary>
			public readonly int Right;

			/// <summary>内空底行。</summary>
			public readonly int Bottom;

			public VaultRoom(string name, int left, int top, int right, int bottom)
			{
				Name = name;
				Left = left;
				Top = top;
				Right = right;
				Bottom = bottom;
			}

			/// <summary>房间内空高度。</summary>
			public int Height => Bottom - Top + 1;

			/// <summary>房间内空宽度。</summary>
			public int Width => Right - Left + 1;
		}

		/// <summary>
		/// 椭球地宫的**房间清单**（7 间：一层 3 间 + 二层两翼各 2 间；此外 x≥516 那段是
		/// 地下通道伸进来的主通道，不算房间）。
		/// <para/>一层三间在 x=100~510（x≥516 是地下通道伸进来的主通道，不能立墙），
		/// 二层四间只能落在堡垒投影（x=277~623）两侧的两翼里。
		/// 每间房至少有一扇原版木门，网络经「堡垒竖井 → 主通道 → 各房间」连通。
		///
		/// <para/>⚠️ 尺寸刻意做成**大中小混排**（玩家反馈"实在太均匀 / 太空"）：
		/// 一层 71 / 57 / **273**，二层左上 41/60、右上 57/41。房间之间留下的缝就是隔墙 ——
		/// 本批要求 4~6 格，这里实际是 **5 或 6 格**（<c>FireplaceVault.FillWall</c> 用
		/// 两侧压条 + 中间合金把整条缝填满）。改这些数字要同时跑
		/// <c>tools/check_fireplace_layout.py</c> 与 <c>tools/check_fireplace_vault_connectivity.py</c>。
		/// </summary>
		public static readonly VaultRoom[] VaultRooms = new VaultRoom[]
		{
			new VaultRoom("一层·废料仓", 100, 1140, 170, 1188),      // 71 宽（中）
			new VaultRoom("一层·修械间", 176, 1140, 232, 1188),      // 57 宽（小）
			new VaultRoom("一层·中央大厅", 238, 1140, 510, 1188),    // 273 宽（大，隔墙 511~515）
			new VaultRoom("二层·档案库", 168, 1078, 208, 1120),      // 41 宽（小，隔墙 209~213）
			new VaultRoom("二层·寝舱", 214, 1078, 273, 1120),        // 60 宽（中，外墙 274~278）
			new VaultRoom("二层·观测室", 630, 1078, 686, 1120),      // 57 宽（中，隔墙 687~691）
			new VaultRoom("二层·配电间", 692, 1078, 732, 1120),      // 41 宽（小，外墙 733~738）
		};

		// ---------------- 出生点 ----------------
		/// <summary>玩家进入壁炉时的落点（门厅地面）。</summary>
		public const int SpawnX = 400;
		public const int SpawnY = HallBottom;
	}
}
