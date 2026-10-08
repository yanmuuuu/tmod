using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Content.Tiles;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 击败清道夫后，在**主世界两侧的海洋**各放一座壁炉入口：位置取**海平面高度**上、
	/// 紧贴水面的那一层沙滩。挖掉了会再放一次，避免主线断掉；两侧各记一组坐标
	/// （见 <see cref="WastelandStorySystem.GateX"/> / <see cref="WastelandStorySystem.GateX2"/>），
	/// 老存档（只有出生点旁那一扇）也会把缺的两侧补上。
	///
	/// <para/>⚠️ 这个系统**只能**在主世界跑。子世界里也有自己的返回方式（壁炉里是
	/// <see cref="FireplaceExit"/>），而且子世界的"出生点"结构和主世界完全不是一回事 ——
	/// 一旦在子世界里跑，下面这套「找地表 → 清空间 → 放门」就会把子世界的结构**当成地表来挖**。
	///
	/// <para/>实测日志（<c>client.log</c>，玩家进门那一次）里就是这样：03:56:19 进壁炉，
	/// <c>04:00:05</c> 起每秒刷一条「出生点附近 40 格内都放不下壁炉入口，下一轮继续重试」，
	/// 一直刷到 <c>04:03:5x</c>（≈228 条），然后才回到主世界 —— 也就是说这 228 秒全在
	/// **壁炉子世界内部**，每一条背后都是 81 列 × (3 列 × 4 行) 的
	/// <see cref="WorldGen.KillTile"/>。见 <see cref="TryPlaceAt"/> 的注释：
	/// 旧写法把那 4 行里的**地脚砖**也清掉了，于是下一轮又找到新的一层"地表"，
	/// **每秒往下啃 4 行、宽 81 格**（子世界里 Main.spawnTileX=400，
	/// 于是这一条正好切过塔顶场地外壳 y=138、塔顶地板 y=287、灰烬层、堡垒屋顶壳 y=860~864、
	/// 门厅楼板 y=970）—— 玩家看到的就是「一部分瑜钢合金自己消失了（被破坏）」。
	/// </summary>
	public class FireplaceGateSystem : ModSystem
	{
		/// <summary>门放哪一侧。左侧记第 1 组坐标，右侧记第 2 组。</summary>
		private enum GateSide
		{
			Left,
			Right
		}

		/// <summary>
		/// 海洋列范围：离世界左/右**边缘** 70~150 格。
		/// 这一段在原版世界里就是海洋生物群系（水面 + 海床），也是玩家说的"两侧的海洋"。
		/// </summary>
		private const int OceanInsetNear = 200;

		/// <summary>见 <see cref="OceanInsetNear"/>。</summary>
		private const int OceanInsetFar = 300;

		/// <summary>y 扫描窗口上界：<see cref="Main.worldSurface"/> 之上 30 格（海面就在它附近）。</summary>
		private const int OceanScanUp = 30;

		/// <summary>y 扫描窗口下界：<see cref="Main.worldSurface"/> 之下 60 格（够到海床，又不会掉进深海沟）。</summary>
		private const int OceanScanDown = 60;

		/// <summary>
		/// 判定"门就在海平面上"的允许吃水（格）= 海床比**本列最上面那一格水**低几格。
		///
		/// <para/>门高 3 格（占 `floor-3 ~ floor-1`），所以：
		/// <list type="bullet">
		/// <item>吃水 1~2 格：门有 1~2 行**露出水面**；</item>
		/// <item>吃水 3 格：门顶那一行正好落在水面那一格上 —— 门"踩在水线"上，这是这一段范围里
		/// 能拿到的最好结果（水里那一列的海床上面必然是水，门不可能整扇悬在水面上）；</item>
		/// <item>吃水 &gt; 3 格：门整个没进水里，只能当**兜底**（宁可门在水下，也不能让主线的入口消失）。</item>
		/// </list>
		/// </summary>
		private const int ShorelineDepth = 3;

		/// <summary>连续失败的轮数：只用来把"每秒一条"的警告压成"每 30 轮一条"。</summary>
		private int failedRounds;

		/// <summary>本世界这一局是否已经报过一次两扇门的位置（免得每 60 tick 刷一条同样的 INFO）。</summary>
		private bool reportedPositions;

		/// <inheritdoc/>
		public override void ClearWorld()
		{
			failedRounds = 0;
			reportedPositions = false;
		}

		public override void PostUpdateWorld()
		{
			// 反射进子世界时的真实报错在这里落到日志（提示语只给玩家看，日志才是给人查的）
			string travelError = FireplaceTravel.TakeLastError();

			if (!string.IsNullOrEmpty(travelError)) {
				Mod.Logger.Warn("FireplaceTravel: " + travelError);
			}

			// ⚠️ 子世界里一律不跑（上面注释里那 228 秒的日志就是漏了这一步）。
			// 用 SubworldSystem.Current 而不是 IsActive<壁炉>：别的模组的子世界同样不能被这样挖。
			if (SubworldLibrary.SubworldSystem.Current != null) {
				failedRounds = 0;
				return;
			}

			if (!WastelandStorySystem.fireplaceOpened || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			if (Main.GameUpdateCount % 60u != 0u) {
				return;
			}

			// 两侧**各自**判断：这一侧的海洋里已经有一座"还立着"的门就不动它（幂等）。
			// 老存档的第 1 组坐标是**出生点旁**那扇门 —— 它不在任何一侧的海洋范围里，
			// 所以两侧都会补（详见 TryFindLiveGateInSlot 的注释）。
			bool needLeft = !HasLiveGate(GateSide.Left);
			bool needRight = !HasLiveGate(GateSide.Right);

			if (!needLeft && !needRight) {
				failedRounds = 0;

				// 两扇门都在（读档 / 都已补好）：这一局再报一次坐标，玩家回传这一条就能核对位置
				if (!reportedPositions) {
					reportedPositions = true;
					LogGatePositions();
				}

				return;
			}

			// ⚠️ 两个调用都要真的执行（不能用 && 串起来），否则左边失败时右边永远轮不到
			bool leftOk = !needLeft || TryPlaceSide(GateSide.Left);
			bool rightOk = !needRight || TryPlaceSide(GateSide.Right);

			if (leftOk && rightOk) {
				failedRounds = 0;
			}

			// 这一轮**真的新放了门**才提示 / 同步；只是在等重试的话直接回去（失败日志已在
			// TryPlaceSide 里按 30 轮一条限流）
			if (!(needLeft && leftOk) && !(needRight && rightOk)) {
				return;
			}

			bool announce = !WastelandStorySystem.GatePlaced;

			reportedPositions = true;
			LogGatePositions();

			if (announce && Main.netMode != NetmodeID.Server) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.GatePlaced"), new Color(226, 150, 80));
			}

			WastelandStorySystem.GatePlaced = true;
			WastelandStorySystem.Sync();
		}

		/// <summary>可核对的位置日志：玩家回传这一条就能确认两扇门到底落在哪（本轮验收的凭证）。</summary>
		private void LogGatePositions()
		{
			Mod.Logger.Info(string.Format(
				"FireplaceGateSystem: 左海门 {0} / 右海门 {1} 已放置（主世界两侧海洋，海平面高度）",
				GateText(GateSide.Left), GateText(GateSide.Right)));
		}

		// ==================================================================================
		// 两侧海洋的范围 / 海平面
		// ==================================================================================

		/// <summary>海平面参考（**世界常量**，不随我们自己的改动而变，见 <see cref="OceanSpots"/> 的注释）。</summary>
		private static int SeaLevelY()
		{
			return (int)Main.worldSurface;
		}

		private static int LeftMin()
		{
			return OceanInsetNear;
		}

		private static int LeftMax()
		{
			// 自定义的极小世界不能让范围越过中点，往中间收一点
			return System.Math.Min(OceanInsetFar, (Main.maxTilesX / 2) - 50);
		}

		private static int RightMin()
		{
			return System.Math.Max(Main.maxTilesX - OceanInsetFar, (Main.maxTilesX / 2) + 50);
		}

		private static int RightMax()
		{
			return Main.maxTilesX - OceanInsetNear;
		}

		private static int SideMinX(GateSide side)
		{
			return side == GateSide.Left ? LeftMin() : RightMin();
		}

		private static int SideMaxX(GateSide side)
		{
			return side == GateSide.Left ? LeftMax() : RightMax();
		}

		private static string SideName(GateSide side)
		{
			return side == GateSide.Left ? "左侧海洋" : "右侧海洋";
		}

		private static bool IsOnSide(GateSide side, int x)
		{
			if (side == GateSide.Left) {
				return x >= LeftMin() && x <= LeftMax();
			}

			return x >= RightMin() && x <= RightMax();
		}

		/// <summary>
		/// 这一侧的候选列，**从靠岸的一侧往世界边缘走**。
		/// 近岸的水最浅，也就最接近玩家要的"海平面水平线"；同距离的列里靠岸的优先。
		/// </summary>
		private static IEnumerable<int> ColumnsFromShore(GateSide side)
		{
			if (side == GateSide.Left) {
				for (int x = LeftMax(); x >= LeftMin(); x--) {
					yield return x;
				}

				yield break;
			}

			for (int x = RightMin(); x <= RightMax(); x++) {
				yield return x;
			}
		}

		// ==================================================================================
		// 地形判定（只读）
		// ==================================================================================

		/// <summary>
		/// 是不是"能当地基"的实心地形：实心、不是半砖平台、也不是家具/物件。
		/// 最后一条顺手把壁炉门自己的图块排除了（门被挖掉后残留的图块不能当地基）。
		/// </summary>
		private static bool IsGround(int x, int y)
		{
			if (!WorldGen.InWorld(x, y)) {
				return false;
			}

			Tile tile = Main.tile[x, y];

			return tile.HasTile
				&& Main.tileSolid[tile.TileType]
				&& !Main.tileSolidTop[tile.TileType]
				&& !Main.tileFrameImportant[tile.TileType];
		}

		/// <summary>这格是不是水（海洋里的是水；岩浆 / 微光不算）。</summary>
		private static bool IsWater(int x, int y)
		{
			if (!WorldGen.InWorld(x, y)) {
				return false;
			}

			Tile tile = Main.tile[x, y];

			return tile.LiquidAmount > 0 && tile.LiquidType == 0;
		}

		/// <summary>这块地基的上方 / 两侧是不是紧贴着水（用来确认它确实在海洋的水线附近，而不是内陆的干地）。</summary>
		private static bool HasWaterContact(int x, int y)
		{
			return IsWater(x, y - 1)
				|| IsWater(x - 1, y - 1)
				|| IsWater(x + 1, y - 1)
				|| IsWater(x - 1, y)
				|| IsWater(x + 1, y);
		}

		/// <summary>
		/// 一列上海平面附近的落点（门基座所在的那一格实心）。
		///
		/// <para/>窗口 = <c>[worldSurface - 30, worldSurface + 60]</c>：海面就在 worldSurface
		/// 上下十几格里，这个窗口足够覆盖"水面 → 沙滩"这一段，又不会掉到深海沟去。
		/// <paramref name="hasWater"/> = 这块地基紧贴着水（海洋里的那一层沙滩）。
		/// 窗口里一块实心都没有时返回 -1（由调用方走退化路径）。
		/// </summary>
		private static int OceanFloorY(int x, out bool hasWater)
		{
			hasWater = false;

			int top = System.Math.Max(SeaLevelY() - OceanScanUp, 1);
			int bottom = System.Math.Min(SeaLevelY() + OceanScanDown, Main.maxTilesY - 2);

			for (int y = top; y <= bottom; y++) {
				if (!IsGround(x, y)) {
					continue;
				}

				hasWater = HasWaterContact(x, y);
				return y;
			}

			return -1;
		}

		/// <summary>
		/// 整列最上面那一格水（用来算"海床比水面低几格"）。
		/// ⚠️ 从 <see cref="SeaLevelY"/> 往上找 200 格开始扫：海面有可能落在扫描窗口上界之上，
		/// 那样只看窗口就会把吃水算大，把本来能露出水面的落点误判成深水。
		/// </summary>
		private static int WaterTopY(int x)
		{
			int top = System.Math.Max(SeaLevelY() - 200, 1);
			int bottom = System.Math.Min(SeaLevelY() + OceanScanDown, Main.maxTilesY - 2);

			for (int y = top; y <= bottom; y++) {
				if (IsWater(x, y)) {
					return y;
				}
			}

			return -1;
		}

		/// <summary>退化路径：整列从 y=60 往下找第一块实心（窗口里连实心都没有时才会用到，老写法）。</summary>
		private static int FirstSolidY(int x)
		{
			if (!WorldGen.InWorld(x, 100)) {
				return -1;
			}

			for (int y = 60; y < Main.maxTilesY - 200; y++) {
				if (IsGround(x, y)) {
					return y;
				}
			}

			return -1;
		}

		// ==================================================================================
		// 候选落点
		// ==================================================================================

		/// <summary>
		/// 一侧的候选落点，按下面的优先级排好（同一档里**离海平面越近越靠前**，
		/// 同距离时靠岸的那一列在前）：
		/// <list type="number">
		/// <item><b>挨着水的浅滩</b>（吃水 ≤ <see cref="ShorelineDepth"/>）：门身还立在水面之上
		/// —— 这就是玩家要的"海平面水平线"；</item>
		/// <item><b>其它挨着水的位置</b>：海床比水面低得多也照放（宁可门在水下，也不能没有入口）；</item>
		/// <item><b>窗口里的干地</b>（岸边没被水泡到的沙滩 / 崖顶）；</item>
		/// <item><b>整列第一块实心</b>（老写法的退化路径）。</item>
		/// </list>
		///
		/// <para/>⚠️ 排序**只用 <see cref="Main.worldSurface"/> 这个世界常量**，不用"本列水面的实际 y"：
		/// 门被挖掉重放时那片地形已经被我们动过（3 行被清空、水可能流进来），
		/// 用会变的值排序就可能换成另一列落点，于是被挖的那一列会永远留一段地基（不幂等）。
		/// </summary>
		private static List<Point> OceanSpots(GateSide side)
		{
			List<Point> shore = new List<Point>();
			List<Point> wet = new List<Point>();
			List<Point> dry = new List<Point>();
			List<Point> deep = new List<Point>();

			foreach (int x in ColumnsFromShore(side)) {
				if (!WorldGen.InWorld(x, 100)) {
					continue;
				}

				int floor = OceanFloorY(x, out bool hasWater);

				if (floor < 0) {
					int fallback = FirstSolidY(x);

					if (fallback >= 0) {
						deep.Add(new Point(x, fallback));
					}

					continue;
				}

				Point spot = new Point(x, floor);

				if (!hasWater) {
					dry.Add(spot);
					continue;
				}

				int waterTop = WaterTopY(x);

				// 本列没有水、但旁边有水（水线正好压在列边界上）：按"贴着水面"算
				int depth = waterTop < 0 ? 0 : floor - waterTop;

				if (depth <= ShorelineDepth) {
					shore.Add(spot);
				}
				else {
					wet.Add(spot);
				}
			}

			SortBySeaLevelDistance(shore);
			SortBySeaLevelDistance(wet);
			SortBySeaLevelDistance(dry);
			SortBySeaLevelDistance(deep);

			shore.AddRange(wet);
			shore.AddRange(dry);
			shore.AddRange(deep);

			return shore;
		}

		/// <summary>按"离海平面多远"做**稳定**排序（同样远的保持列表原本的靠岸顺序）。</summary>
		private static void SortBySeaLevelDistance(List<Point> spots)
		{
			for (int i = 1; i < spots.Count; i++) {
				Point current = spots[i];
				int j = i - 1;

				while (j >= 0 && DistanceToSeaLevel(spots[j].Y) > DistanceToSeaLevel(current.Y)) {
					spots[j + 1] = spots[j];
					j--;
				}

				spots[j + 1] = current;
			}
		}

		private static int DistanceToSeaLevel(int y)
		{
			int sea = SeaLevelY();

			return y > sea ? y - sea : sea - y;
		}

		// ==================================================================================
		// 放置
		// ==================================================================================

		/// <summary>
		/// 在**一侧的海洋**里找一列把门放下；这一侧放不下就返回 false（由上层 30 轮一次地重试）。
		/// 候选之间**逐个换列**试，任何一步失败都不会停在同一列上反复啃地形（见 <see cref="TryPlaceAt"/>）。
		/// </summary>
		private bool TryPlaceSide(GateSide side)
		{
			int gateType = ModContent.TileType<FireplaceGate>();

			foreach (Point spot in OceanSpots(side)) {
				if (TryPlaceAt(spot.X, spot.Y, gateType, side)) {
					return true;
				}
			}

			failedRounds++;

			if (failedRounds == 1 || failedRounds % 30 == 0) {
				Mod.Logger.Warn(string.Format(
					"FireplaceGateSystem: {0}（x {1}~{2}）里都放不下壁炉入口（连续第 {3} 轮），下一轮继续重试",
					SideName(side), SideMinX(side), SideMaxX(side), failedRounds));
			}

			return false;
		}

		/// <summary>
		/// 在某一列上试放门；成功返回 true 并把这扇门的坐标记进这一侧的槽位。
		///
		/// <para/>⚠️ <paramref name="floor"/> 这一格**不能**清（玩家反馈"进去一段时间后一部分瑜钢合金自己消失了"的真根因）：
		/// <list type="bullet">
		/// <item>旧写法 `dy = 0..3` 把 `floor` 那一行**地脚砖**也一起 `KillTile` 掉了。
		/// 门是 3×3，占的是 `floor-3 ~ floor-1` 三行，`floor` 这一行是它的**地基**，本来就不该动；</item>
		/// <item>清掉地脚砖之后，下一轮再找"第一块实心"就会直接**跳过**这一层、落到更下面一层 ——
		/// 于是"每秒重试一次"变成"每秒往下挖 4 行、宽 81 格"，在壁炉子世界里是沿着 x=360~440
		/// 把塔顶场地、灰烬层、堡垒屋顶壳（瑜钢合金）与门厅楼板一路啃穿；</item>
		/// <item>留下地脚砖之后，重试只会清到同一批（已经是空气的）格子 —— **幂等**，不再往下走；
		/// 而且门真的有了地基，不会放上又立刻因为悬空被判掉。</item>
		/// </list>
		/// </summary>
		private static bool TryPlaceAt(int x, int floor, int gateType, GateSide side)
		{
			// 地基必须还是实心的（`floor` 是只读扫描出来的，这里再核一次，顺便挡住越界坐标）
			if (floor < 4 || !IsGround(x, floor)) {
				return false;
			}

			// 把门要占的 3×3 空间清出来（门是 Style3x3，锚点在底部中心那一格）——
			// 只清 floor-3 ~ floor-1 这 3 行，floor 那一行是地基，留着。
			for (int dx = -1; dx <= 1; dx++) {
				for (int dy = 1; dy <= 3; dy++) {
					WorldGen.KillTile(x + dx, floor - dy, false, false, true);
				}
			}

			WorldGen.PlaceTile(x, floor - 1, gateType, mute: true, forced: true);

			// 核对：3×3 区域里真的出现了门才认成功（否则让上层换列、或下一轮重试）
			for (int dx = -1; dx <= 1; dx++) {
				for (int dy = -3; dy <= 0; dy++) {
					int tx = x + dx;
					int ty = floor - 1 + dy;

					if (!WorldGen.InWorld(tx, ty)) {
						continue;
					}

					if (Main.tile[tx, ty].HasTile && Main.tile[tx, ty].TileType == gateType) {
						RecordGate(side, tx, ty);

						if (Main.netMode == NetmodeID.Server) {
							NetMessage.SendTileSquare(-1, x, floor - 2, 5);
						}

						return true;
					}
				}
			}

			return false;
		}

		/// <summary>把这一侧的门坐标记进世界数据（左 = 第 1 组，右 = 第 2 组）。</summary>
		private static void RecordGate(GateSide side, int x, int y)
		{
			if (side == GateSide.Left) {
				WastelandStorySystem.GateX = x;
				WastelandStorySystem.GateY = y;
				WastelandStorySystem.GatePlaced = true;
				return;
			}

			WastelandStorySystem.GateX2 = x;
			WastelandStorySystem.GateY2 = y;
			WastelandStorySystem.Gate2Placed = true;
		}

		// ==================================================================================
		// 已记坐标的"还立着吗"
		// ==================================================================================

		/// <summary>这一侧现在是不是已经有**一扇真的还立着**的壁炉门（两组坐标都查）。</summary>
		private static bool HasLiveGate(GateSide side)
		{
			return TryFindLiveGate(side, out _, out _);
		}

		/// <summary>给日志用："(x, y)" 或 "未放置"。</summary>
		private static string GateText(GateSide side)
		{
			int x;
			int y;

			return TryFindLiveGate(side, out x, out y) ? string.Format("({0}, {1})", x, y) : "未放置";
		}

		/// <summary>两组坐标里找一个"符合这一侧"的门。</summary>
		private static bool TryFindLiveGate(GateSide side, out int x, out int y)
		{
			if (TryFindLiveGateInSlot(side, WastelandStorySystem.GatePlaced,
					WastelandStorySystem.GateX, WastelandStorySystem.GateY, out x, out y)) {
				return true;
			}

			return TryFindLiveGateInSlot(side, WastelandStorySystem.Gate2Placed,
				WastelandStorySystem.GateX2, WastelandStorySystem.GateY2, out x, out y);
		}

		/// <summary>
		/// 一个槽位里"坐标已记下 + 图块确实还在 + **而且就在这一侧的海洋范围内**"才算数。
		///
		/// <para/>⚠️ 最后那条是关键（老存档的迁移逻辑就靠它）：老存档的第 1 组坐标是
		/// **出生点旁**那扇门，它虽然还立着，但**不算**"左侧海洋已经有门了" ——
		/// 否则老存档永远补不出左海门。新世界 / 补放之后，两组坐标本来就分别落在两侧的海洋里。
		/// </summary>
		private static bool TryFindLiveGateInSlot(GateSide side, bool placed, int slotX, int slotY, out int x, out int y)
		{
			x = 0;
			y = 0;

			if (!placed || slotX <= 0 || slotY <= 0 || !WorldGen.InWorld(slotX, slotY)) {
				return false;
			}

			if (!IsOnSide(side, slotX)) {
				return false;
			}

			Tile tile = Main.tile[slotX, slotY];

			if (!tile.HasTile || tile.TileType != ModContent.TileType<FireplaceGate>()) {
				return false;
			}

			x = slotX;
			y = slotY;
			return true;
		}
	}

	/// <summary>进出壁炉子世界。API 用反射调用，避免前置小版本改了参数个数就编不过。</summary>
	public static class FireplaceTravel
	{
		/// <summary>反射调用里的真实报错（由 <see cref="TakeLastError"/> 取走写日志）。</summary>
		private static string lastError;

		public static void Enter()
		{
			Enter(announce: true);
		}

		/// <summary>
		/// 同上，但可以压掉提示语。
		/// <para/><paramref name="announce"/> = false 的用途：<see cref="FireplaceEntrySystem"/>
		/// 在首次进门时自动补一次"出去再进来"，重试敲第二次门时不该对着玩家喊
		/// "需要启用 Subworld Library"（那只是"世界还在切换"而已，会把人吓一跳）。
		///
		/// <para/>⚠️ 这里是**本机直接进入**的原始实现，只对"客户端 + 单人世界"这一侧有意义
		/// （SubworldLibrary 的 <c>BeginEntering</c> 在 <c>netMode == 2</c> 直接 return，
		/// 而且客户端侧的 <c>MovePlayer*</c> 都被守卫挡掉）。**联机要走
		/// <see cref="FireplaceTravelNet.RequestEnter"/>**：那边才会发请求、由服务端移动。
		/// 门图块的右键已经改成调 RequestEnter，本方法只留给
		/// <see cref="FireplaceEntrySystem"/>（它只在单人跑）和单机路径复用。
		/// </summary>
		public static void Enter(bool announce)
		{
			if (Main.netMode == NetmodeID.Server) {
				return;
			}

			// 旧写法只看「反射调用没抛异常」就当成功，于是"进入失败"提示几乎永远不会出现。
			// 现在取两级真实结果：
			//   1) 反射是否真的调用到了方法，以及 Enter 自己的返回值；
			//   2) 调完之后 SubworldLibrary 是不是真的把壁炉子世界设成了当前世界。
			// 能问出 (2) 时以 (2) 为准 —— 那才是"真的进去了"。
			bool called = Invoke("Enter", out bool methodSaysOk);
			bool? active = IsSubworldActive();
			bool succeeded = active.HasValue ? active.Value : (called && methodSaysOk);

			if (!announce) {
				return;
			}

			if (succeeded) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.EnteredFireplace"), new Color(180, 210, 255));
				return;
			}

			Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.FireplaceEnterFailed"), new Color(220, 160, 90));
		}

		/// <summary>
		/// 回主世界（本机直接退出）。与 <see cref="Enter(bool)"/> 一样，这是**原始实现**：
		/// 联机要走 <see cref="FireplaceTravelNet.RequestExit"/>，
		/// 由服务端用 <c>MovePlayerToMainWorld(whoAmI)</c> 送回来 —— 联机里子世界是
		/// 另一个进程，客户端自己撤不回来。
		/// </summary>
		public static void Exit()
		{
			if (Main.netMode == NetmodeID.Server) {
				return;
			}

			Invoke("Exit", out _);
		}

		/// <summary>取走并清空最近一次反射调用的报错（调用方负责写进日志）。</summary>
		public static string TakeLastError()
		{
			string error = lastError;
			lastError = null;
			return error;
		}

		/// <summary>
		/// 壁炉子世界现在是不是当前世界。
		/// <para/>返回 <c>null</c> = **问不出来**（前置里没有 <c>IsActive&lt;T&gt;()</c> 这个方法），
		/// 这时一律按"不知道"处理，绝不能当成失败——否则前置一改 API 就会对着成功进入的玩家刷"进入失败"。
		/// <para/>SubworldLibrary 的 <c>Enter&lt;T&gt;()</c> 会**先把 current 换成目标子世界**再开始加载，
		/// 所以调用之后立刻查是准的（IL 已核过：BeginEntering 里就赋了 current）。
		/// </summary>
		private static bool? IsSubworldActive()
		{
			System.Type system = typeof(SubworldLibrary.SubworldSystem);

			foreach (System.Reflection.MethodInfo method in system.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
				if (method.Name != "IsActive" || !method.IsGenericMethodDefinition) {
					continue;
				}

				if (method.GetGenericArguments().Length != 1) {
					continue;
				}

				try {
					object result = method.MakeGenericMethod(typeof(Content.Subworlds.FireplaceSubworld)).Invoke(null, null);

					if (result is bool active) {
						return active;
					}
				}
				catch (System.Exception) {
					// 探测失败 = 问不出来，返回 null 由调用方走"不判定失败"的分支
					return null;
				}
			}

			return null;
		}

		/// <summary>
		/// 反射调用一个静态方法。<paramref name="returnedTrue"/> 是它的真实返回值
		/// （返回类型不是 bool 时按 true 记，例如 <c>Exit()</c>）。
		/// </summary>
		private static bool Invoke(string name, out bool returnedTrue)
		{
			returnedTrue = true;

			System.Type system = typeof(SubworldLibrary.SubworldSystem);
			bool called = false;

			foreach (System.Reflection.MethodInfo method in system.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
				if (method.Name != name) {
					continue;
				}

				System.Reflection.MethodInfo target = method;

				try {
					if (method.IsGenericMethodDefinition) {
						// ⚠️ GetGenericArguments 返回的是 Type[]（泛型参数类型），不是 ParameterInfo[]
						// —— 顺手修掉了合并进来时的这个编译错误
						System.Type[] generic = method.GetGenericArguments();

						if (generic.Length != 1) {
							continue;
						}

						target = method.MakeGenericMethod(typeof(Content.Subworlds.FireplaceSubworld));
					}
					else if (name == "Enter") {
						continue;
					}

					System.Reflection.ParameterInfo[] parameters = target.GetParameters();
					object[] args = new object[parameters.Length];

					for (int i = 0; i < parameters.Length; i++) {
						if (parameters[i].HasDefaultValue) {
							args[i] = parameters[i].DefaultValue;
						}
						else if (parameters[i].ParameterType.IsValueType) {
							args[i] = System.Activator.CreateInstance(parameters[i].ParameterType);
						}
						else {
							args[i] = null;
						}
					}

					object result = target.Invoke(null, args);

					if (target.ReturnType == typeof(bool) && result is bool ok) {
						returnedTrue = ok;
					}

					called = true;
					break;
				}
				catch (System.Reflection.TargetInvocationException exception) {
					// 旧写法把内层异常整个吞掉，出了事在日志里一点痕迹都没有
					lastError = $"{name} 抛异常：{exception.InnerException?.Message ?? exception.Message}";
					continue;
				}
				catch (System.Exception exception) {
					// MakeGenericMethod / 参数构造失败等（旧写法这几步在 try 外面，会直接崩）
					lastError = $"{name} 调用失败：{exception.Message}";
					continue;
				}
			}

			return called;
		}
	}
}
