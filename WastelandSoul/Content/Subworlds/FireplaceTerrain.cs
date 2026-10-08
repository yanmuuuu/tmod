using System;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 壁炉**地表**：灰烬原野。
	///
	/// <para/>世界观：为了保住最后的火种，牠们把堡垒外的土地整个烧成了灰。
	/// 于是外面是一片起伏的灰烬荒原，空气被堡垒排出的有害气体污染（减益见
	/// <see cref="Content.Buffs.GasPoison"/>），低洼处积着一点岩浆与污染水。
	///
	/// <para/>生成手法**照原版地表那套**：
	/// <list type="number">
	/// <item>用**分层噪声**（<see cref="WorldPaint.Fractal"/>，4 个八度）算地表高度 ——
	/// 原版地表就是"低频大起伏 + 高频小起伏"叠出来的，这样才有自然的丘陵感，不是正弦波；</item>
	/// <item>往下按深度分层：最表层灰烬草 → 灰烬 → 灰烬夹土 → 岩石层（再混一点黑曜石脉）；</item>
	/// <item>零散特征：灰烬丘（椭圆堆）、黑曜石巨岩、低洼处的岩浆池与污染水塘。</item>
	/// </list>
	/// </summary>
	public static class FireplaceTerrain
	{
		/// <summary>噪声种子（写死 = 每次生成的地形完全一致，方便对照与截图）。</summary>
		private const int Seed = 20261008;

		/// <summary>算好的地表高度表（其它生成步骤也会用）。</summary>
		public static readonly int[] Surface = new int[FireplaceLayout.Width];

		public static void Build(GenerationProgress progress, GameConfiguration configuration)
		{
			// 冒烟测试会传 null 进来（不构造 GameConfiguration，免得拉进 Newtonsoft 触发警告）
			progress ??= new GenerationProgress();
			FireplaceGenLog.Start("1/6 灰烬原野");
			progress.Message = "正在吹散壁炉外的灰烬……";

			BuildHeightMap();
			progress.Set(0.12);

			BuildCrust();
			progress.Set(0.45);

			BuildDunes();
			progress.Set(0.55);

			BuildSurfaceGrass();
			progress.Set(0.62);

			BuildPebbles();
			progress.Set(0.68);

			BuildAshTrees();
			progress.Set(0.76);

			BuildPools();
			progress.Set(0.86);

			BuildRocks();
			BuildCaves(progress);
			BuildMineShafts(progress);

			// 最后一道：把结构与矿道周围/上方的沙透镜体换成石头（见方法注释）
			HardenSandNearStructures();
			progress.Set(1.0);
		}

		/// <summary>分层噪声算地表高度。</summary>
		private static void BuildHeightMap()
		{
			for (int x = 0; x < FireplaceLayout.Width; x++) {
				// 低频：大丘陵（波长约 90 格）
				float broad = WorldPaint.Fractal(Seed, x / 90f, 4, 0.5f);

				// 高频：碎石与灰堆的小起伏（波长约 17 格），幅度压到 1/4
				float fine = WorldPaint.Fractal(Seed + 1013, x / 17f, 3, 0.5f);

				float offset = ((broad - 0.5f) * 2f * FireplaceLayout.SurfaceAmplitude)
					+ ((fine - 0.5f) * 2f * (FireplaceLayout.SurfaceAmplitude * 0.25f));

				Surface[x] = (int)MathF.Round(FireplaceLayout.SurfaceBase + offset);
			}
		}

		/// <summary>按深度铺地表层与地下岩层。</summary>
		private static void BuildCrust()
		{
			for (int x = 0; x < FireplaceLayout.Width; x++) {
				int top = Surface[x];

				for (int y = top; y < FireplaceLayout.Height; y++) {
					int depth = y - top;
					ushort type;

					if (depth == 0) {
						type = TileID.AshGrass;                 // 表层：灰烬草（原版 1.4.4 自带）
					}
					else if (depth <= 3) {
						type = TileID.Ash;
					}
					else if (depth < FireplaceLayout.CrustDepth) {
						// 灰烬与土交错：用二维噪声决定，看起来像被烧透的旧土壤
						float mix = WorldPaint.Fractal2D(Seed + 77, x / 26f, y / 22f, 3);
						type = mix < 0.42f ? TileID.Dirt : TileID.Ash;
					}
					else {
						// 岩石层：**石 / 沙 / 土 / 泥沙**四样按噪声交错（玩家要求：资源枯竭，不放任何矿石）
						// 三层不同波长的噪声叠出"有层理、有透镜体"的观感，而不是一片纯石头
						float vein = WorldPaint.Fractal2D(Seed + 313, x / 46f, y / 30f, 3);
						float lens = WorldPaint.Fractal2D(Seed + 907, x / 22f, y / 90f, 2);

						if (vein > 0.74f) {
							type = TileID.Sand;                 // 沙透镜体
						}
						else if (vein > 0.62f) {
							type = TileID.Silt;                 // 泥沙
						}
						else if (lens > 0.62f) {
							type = TileID.Dirt;                 // 土夹层
						}
						else if (vein < 0.20f) {
							type = TileID.Ash;                  // 灰烬（烧透的旧土）
						}
						else {
							type = TileID.Stone;
						}

						// 极少数位置给一点黑曜石脉，作为瑜钢合金的观感伏笔
						if (WorldPaint.Fractal2D(Seed + 555, x / 60f, y / 52f, 2) > 0.86f) {
							type = TileID.Obsidian;
						}
					}

					WorldPaint.Retile(x, y, type);
				}
			}
		}

		/// <summary>灰烬丘：在地表堆一批椭圆灰堆，让轮廓不那么"平"。原版做沙丘/雪堆也是这个手法。</summary>
		private static void BuildDunes()
		{
			// 玩家要求「地表稍微像样一些」：灰烬丘从 26 个加密到 38 个，大小差别也拉开
			const int count = 38;

			for (int i = 0; i < count; i++) {
				int x = 30 + (int)(i * (FireplaceLayout.Width - 60) / (float)count);
				int jitter = (int)(WorldPaint.ValueNoise(Seed + 9001, i * 3.7f) * 70f);
				x = Math.Clamp(x + jitter - 35, 12, FireplaceLayout.Width - 13);

				float radiusX = 7f + WorldPaint.ValueNoise(Seed + 500, i * 1.3f) * 17f;
				float radiusY = 3f + WorldPaint.ValueNoise(Seed + 600, i * 1.7f) * 6f;
				int y = Surface[x] + (int)(radiusY * 0.6f);

				WorldPaint.Ellipse(x, y, radiusX, radiusY, TileID.Ash);

				// 丘顶再压一层碎石，别的列也顺便带一点
				if (WorldPaint.ValueNoise(Seed + 700, i * 2.1f) > 0.55f) {
					WorldPaint.Ellipse(x + 3, y - (int)radiusY, radiusX * 0.35f, radiusY * 0.4f, TileID.Stone);
				}
			}
		}

		/// <summary>
		/// 地表整理：把每一列**最高的那格地表**（灰烬 / 土）换成灰烬草。
		/// <para/>为什么需要：<see cref="BuildCrust"/> 只给基准高度那一行铺草，
		/// 之后 <see cref="BuildDunes"/> 堆出来的灰烬丘顶面是裸灰烬。
		/// 这一步让整个荒原都有灰烬草的起伏（玩家要的"地表稍微微像样一些"）。
		/// </summary>
		private static void BuildSurfaceGrass()
		{
			int grassed = 0;

			for (int x = 2; x < FireplaceLayout.Width - 2; x++) {
				if (InsideStructure(x, Surface[x], 4)) {
					continue;
				}

				int from = Math.Max(Surface[x] - 40, 0);

				for (int y = from; y < FireplaceLayout.Height; y++) {
					if (!WorldPaint.HasTile(x, y)) {
						continue;
					}

					// 只认地表层的灰烬 / 土：石头、黑曜石、合金一律不动
					ushort type = Main.tile[x, y].TileType;

					if (type == TileID.Ash || type == TileID.Dirt) {
						WorldPaint.Retile(x, y, TileID.AshGrass);
						grassed++;
					}

					break;
				}
			}

			FireplaceGenLog.Note(string.Format("灰烬草地表 {0} 列", grassed));
		}

		/// <summary>地表碎石：小块石头 / 黑曜石，让荒原看着不那么"铺平过"。</summary>
		private static void BuildPebbles()
		{
			int placed = 0;

			for (int i = 0; i < 140 && placed < 70; i++) {
				int x = 10 + (int)(WorldPaint.ValueNoise(Seed + 8200, i * 1.7f) * (FireplaceLayout.Width - 20));

				if (InsideStructure(x, Surface[x], 4)) {
					continue;
				}

				float radius = 1.1f + WorldPaint.ValueNoise(Seed + 8300, i * 2.3f) * 2.4f;
				int y = Surface[x] - (int)(radius * 0.3f);
				ushort type = WorldPaint.ValueNoise(Seed + 8400, i * 3.1f) > 0.62f
					? TileID.Obsidian
					: TileID.Stone;

				WorldPaint.Ellipse(x, y, radius, radius * 0.7f, type);
				placed++;
			}

			FireplaceGenLog.Note(string.Format("地表碎石 {0} 处", placed));
		}

		/// <summary>
		/// 灰烬树（地狱风格的枯树）：只长在灰烬草上，10~20 棵散在世界里。
		///
		/// <para/>用原版 <see cref="WorldGen.GrowTree"/>：1.4.4 里它自己认得灰烬草
		/// （内部走 <c>WorldGen.AshTreeGroundTest</c> / <c>IsTileTypeFitForTree</c>），
		/// 不用手搭树干 —— 手搭要自己算 <c>TileID.Trees</c> 的 frameX/frameY，很容易歪。
		/// <para/>⚠️ 它的第二个参数是**地面那一格**（不是地面上一格）：IL 里先拿它当
		/// "起点格"跳过树苗，再对同一格做 <c>IsTileTypeFitForTree</c> 与坡度检查。
		/// 所以这里传 <c>Surface[x]</c>，并用返回值 + "树干到底在不在"双保险。
		/// </summary>
		private static void BuildAshTrees()
		{
			int placed = 0;

			for (int i = 0; i < 260 && placed < 16; i++) {
				int x = 14 + (int)(WorldPaint.ValueNoise(Seed + 9100, i * 1.31f) * (FireplaceLayout.Width - 28));
				int ground = Surface[x];

				if (!WorldPaint.InWorld(x, ground) || InsideStructure(x, ground, 6)) {
					continue;
				}

				if (!WorldPaint.IsTile(x, ground, TileID.AshGrass)) {
					continue;
				}

				if (WorldPaint.HasTile(x, ground - 1) || WorldPaint.HasTile(x, ground - 2)
					|| WorldPaint.HasTile(x, ground - 3)) {
					continue;
				}

				WorldGen.GrowTree(x, ground);

				// 树干是 TileID.Trees：长出来了才算一棵（GrowTree 空间不够时会返回 false 且什么都不做）
				for (int y = ground - 1; y >= ground - 6 && y > 0; y--) {
					if (WorldPaint.IsTile(x, y, TileID.Trees)) {
						placed++;
						break;
					}
				}
			}

			FireplaceGenLog.Note(string.Format("灰烬树 {0} 棵", placed));
		}

		/// <summary>
		/// 低洼处的液体：**少量**岩浆 + **少量**污染水。
		/// 污染水用原版水（液体类型 0）表现：视觉上是水，进游戏后湿地会挂上污染减益。
		/// </summary>
		private static void BuildPools()
		{
			int lavaPlaced = 0;
			int waterPlaced = 0;

			for (int x = 24; x < FireplaceLayout.Width - 24 && (lavaPlaced < 9 || waterPlaced < 12); x += 7) {
				// 只在"局部最低点"放液体，水才会待在坑里
				if (Surface[x] < Surface[x - 4] || Surface[x] < Surface[x + 4]) {
					continue;
				}

				bool lava = lavaPlaced <= waterPlaced && lavaPlaced < 9;

				if (lava && lavaPlaced >= 9) {
					continue;
				}

				if (!lava && waterPlaced >= 12) {
					continue;
				}

				float chance = WorldPaint.ValueNoise(Seed + 4242, x * 0.31f);

				if (chance < 0.45f) {
					continue;
				}

				int width = 4 + (int)(WorldPaint.ValueNoise(Seed + 777, x * 0.13f) * 5f);
				int depth = lava ? 3 : 2;
				int top = Surface[x];

				// 挖出一个小盆（比液面低 depth + 1 格，液体才不会溢出去）
				WorldPaint.EllipseCarve(x, top + depth, (width * 0.5f) + 1f, depth + 1.5f);

				// 灌液体：岩浆是 1、水是 0；只灌满盆底那一层（空格子才生效）
				byte liquid = lava ? (byte)1 : (byte)0;
				int half = width / 2;

				for (int dx = -half; dx <= half; dx++) {
					for (int dy = top + depth; dy <= top + depth + 1; dy++) {
						WorldPaint.SetLiquid(x + dx, dy, 255, liquid);
					}
				}

				if (lava) {
					lavaPlaced++;
				}
				else {
					waterPlaced++;
				}
			}
		}


		// ==================== 自然洞穴与矿道 ====================
		//
		// 玩家要求："随机生成一些不干扰原本建筑的矿道和自然矿洞，只不过因为资源枯竭没有矿石。"
		// 所以：**只挖洞、不放任何矿石**；每一处都先做"是否撞到结构"的判定。

		/// <summary>
		/// 某个坐标是否落在塔 / 堡垒 / 地下通道 / **椭球地宫**的保护区里（这些地方不许挖洞、
		/// 也不许留沙）。
		///
		/// <para/>⚠️ 椭球地宫是这一批补上的：<see cref="FireplaceVault"/> 的椭球横向 x=78~822、
		/// 纵向 y=1018~1282，之前**不在**保护区里 —— 于是 <see cref="BuildCaves"/> 与
		/// <see cref="BuildMineShafts"/> 完全可以横穿地宫（它们是第 1 步、地宫是第 5 步，
		/// 虽然地宫后建会把洞盖掉，但矿道的木梁/平台会留在地宫房间里）。
		/// 判据直接用椭球外框（比逐格算椭圆便宜，多保护一点没有副作用）。
		/// </summary>
		internal static bool InsideStructure(int x, int y, int margin = 12)
		{
			return Rect(x, y, FireplaceLayout.FortLeft, FireplaceLayout.FortTop, FireplaceLayout.FortRight, FireplaceLayout.FortBottom, margin)
				|| Rect(x, y, FireplaceLayout.TowerLeft, FireplaceLayout.TowerTop, FireplaceLayout.TowerRight, FireplaceLayout.TowerBottom, margin)
				|| Rect(x, y, FireplaceLayout.ArenaLeft, FireplaceLayout.ArenaTop, FireplaceLayout.ArenaRight, FireplaceLayout.ArenaBottom, margin)
				|| Rect(x, y, FireplaceLayout.TunnelLeft - 6, FireplaceLayout.TunnelTop - 6,
					FireplaceLayout.TunnelRight + 8, FireplaceLayout.TunnelBottom + 6, margin)
				|| Rect(x, y, FireplaceLayout.VaultLeft, FireplaceLayout.VaultTop,
					FireplaceLayout.VaultRight, FireplaceLayout.VaultBottom, margin);
		}

		/// <summary>
		/// 结构保护区（外扩 <see cref="SandGuardMargin"/> 格）里的 **沙 / 泥沙** 全部换成石头。
		///
		/// <para/>为什么需要：<c>TileID.Sand</c> / <c>TileID.Silt</c> 受重力。掏空之后
		/// （地宫空腔、矿道、竖井），上面的沙柱会一路往下漏 —— 原版机制是
		/// <c>WorldGen.SpawnFallingBlockProjectile</c>：它只把**沙自己**那一格
		/// <c>ClearTile()</c> 掉、再生成一个落沙弹幕（落地的 <c>Projectile.Kill</c> 只调
		/// <c>PlaceTile</c>）—— 所以沙**不会破坏**瑜钢合金，但会灌进房间、把地板与家具埋掉，
		/// 玩家看到的就是"结构里出现异样"。
		///
		/// <para/>做法：保护区外扩 24 格（覆盖结构正上方那一段岩层）之内，只要是沙/泥沙一律
		/// 换 <c>TileID.Stone</c> —— 石头不掉，从此没有"往结构里漏沙"的通路。
		/// </summary>
		private const int SandGuardMargin = 24;

		private static void HardenSandNearStructures()
		{
			int changed = 0;

			for (int x = 0; x < FireplaceLayout.Width; x++) {
				// 只扫岩石层以下（地表那几层是灰烬/土，没有沙；宝箱区也不在这儿）
				for (int y = FireplaceLayout.RockTop - 40; y < FireplaceLayout.Height; y++) {
					bool sand = WorldPaint.IsTile(x, y, TileID.Sand) || WorldPaint.IsTile(x, y, TileID.Silt);

					if (!sand || !InsideStructure(x, y, SandGuardMargin)) {
						continue;
					}

					WorldPaint.Retile(x, y, TileID.Stone);
					changed++;
				}
			}

			FireplaceGenLog.Note(string.Format("结构附近沙透镜体换成石头 {0} 格（外扩 {1} 格）",
				changed, SandGuardMargin));
		}

		private static bool Rect(int x, int y, int left, int top, int right, int bottom, int margin)
		{
			return x >= left - margin && x <= right + margin && y >= top - margin && y <= bottom + margin;
		}

		/// <summary>自然矿洞：在岩石层里挖一批大小不一的洞，彼此可能相连（原版洞穴就是这个做法）。</summary>
		private static void BuildCaves(GenerationProgress progress)
		{
			progress.Message = "正在蚀出旧时代的空洞……";

			int carved = 0;

			for (int i = 0; i < 220 && carved < 150; i++) {
				int x = 20 + (int)(WorldPaint.ValueNoise(Seed + 6100, i * 1.7f) * (FireplaceLayout.Width - 40));
				int y = FireplaceLayout.RockTop + 30
					+ (int)(WorldPaint.ValueNoise(Seed + 6200, i * 2.3f) * (FireplaceLayout.Height - FireplaceLayout.RockTop - 60));

				if (InsideStructure(x, y, 14)) {
					continue;
				}

				float radiusX = 5f + (WorldPaint.ValueNoise(Seed + 6300, i * 1.1f) * 16f);
				float radiusY = 4f + (WorldPaint.ValueNoise(Seed + 6400, i * 1.3f) * 9f);

				WorldPaint.EllipseCarve(x, y, radiusX, radiusY);

				// 一半的洞再连一条细通道出去，制造"洞系"的感觉
				if (WorldPaint.ValueNoise(Seed + 6500, i * 0.9f) > 0.5f) {
					int steps = 8 + (int)(WorldPaint.ValueNoise(Seed + 6600, i * 2.1f) * 26f);
					float angle = WorldPaint.ValueNoise(Seed + 6700, i * 3.1f) * 6.28f;
					int cx = x;
					int cy = y;

					for (int s = 0; s < steps; s++) {
						cx += (int)MathF.Round(MathF.Cos(angle) * 3f);
						cy += (int)MathF.Round(MathF.Sin(angle) * 2f);

						if (InsideStructure(cx, cy, 10) || cy <= FireplaceLayout.RockTop) {
							break;
						}

						WorldPaint.EllipseCarve(cx, cy, 3.4f, 2.6f);
					}
				}

				carved++;
			}

			FireplaceGenLog.Note(string.Format("自然矿洞 {0} 处", carved));
		}

		/// <summary>矿道：几条横向主巷 + 竖井，配原版木平台当支撑（没有任何矿石）。</summary>
		private static void BuildMineShafts(GenerationProgress progress)
		{
			progress.Message = "正在凿出废弃的矿道……";

			int built = 0;

			for (int i = 0; i < 6; i++) {
				int y = FireplaceLayout.RockTop + 60
					+ (int)(WorldPaint.ValueNoise(Seed + 7100, i * 2.7f) * (FireplaceLayout.Height - FireplaceLayout.RockTop - 120));

				int left = 30 + (int)(WorldPaint.ValueNoise(Seed + 7200, i * 1.9f) * (FireplaceLayout.Width - 200));
				int right = left + 90 + (int)(WorldPaint.ValueNoise(Seed + 7300, i * 2.3f) * 130f);

				// 撞到结构就整条不要，保证"不干扰原本建筑"
				bool blocked = false;

				for (int x = left; x <= right; x += 6) {
					if (InsideStructure(x, y, 16)) {
						blocked = true;
						break;
					}
				}

				if (blocked) {
					continue;
				}

				for (int x = left; x <= right; x++) {
					WorldPaint.Carve(x, y, x, y + 3);           // 主巷：4 格高
				}

				// 支撑：每隔 14 格一对木梁 + 顶板
				for (int x = left + 4; x <= right - 4; x += 14) {
					WorldPaint.Retile(x, y - 1, TileID.WoodenBeam);
					WorldPaint.Retile(x + 1, y - 1, TileID.WoodenBeam);
					WorldPaint.HLine(x - 1, x + 2, y - 1, TileID.WoodBlock);
				}

				// 竖井：从主巷往上打一条，接到上层
				int shaftX = left + 30 + (int)(WorldPaint.ValueNoise(Seed + 7400, i * 1.3f) * (right - left - 60));
				int shaftTop = y - 40 - (int)(WorldPaint.ValueNoise(Seed + 7500, i * 2.9f) * 50f);

				for (int sy = Math.Max(shaftTop, FireplaceLayout.RockTop + 8); sy <= y; sy++) {
					if (InsideStructure(shaftX, sy, 10)) {
						continue;
					}

					WorldPaint.Carve(shaftX, sy, shaftX + 2, sy);

					if ((y - sy) % 6 == 0) {
						WorldPaint.Retile(shaftX, sy, TileID.Platforms);   // 当梯子用
					}
				}

				built++;
			}

			FireplaceGenLog.Note(string.Format("矿道 {0} 条", built));
		}

		/// <summary>地表散落的黑曜石巨岩 —— 让"灰烬 + 黑曜石"的观感提前立住。</summary>
		private static void BuildRocks()
		{
			for (int i = 0; i < 14; i++) {
				int x = 30 + (int)(WorldPaint.ValueNoise(Seed + 3131, i * 2.9f) * (FireplaceLayout.Width - 60));
				float radius = 3f + WorldPaint.ValueNoise(Seed + 555, i * 1.1f) * 5f;
				int y = Surface[x] - (int)(radius * 0.4f);

				WorldPaint.Ellipse(x, y, radius, radius * 0.7f, TileID.Obsidian);
			}
		}
	}
}
