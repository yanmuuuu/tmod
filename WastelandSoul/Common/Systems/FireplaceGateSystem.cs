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
	/// 击败清道夫后，在**主世界**出生点旁边放下壁炉入口。挖掉了会再放一次，避免主线断掉。
	///
	/// <para/>⚠️ 这个系统**只能**在主世界跑。子世界里也有自己的返回方式（壁炉里是
	/// <see cref="FireplaceExit"/>），而且子世界的"出生点"结构和主世界完全不是一回事 ——
	/// 一旦在子世界里跑，下面这套「找地表 → 清空间 → 放门」就会把子世界的结构**当成地表来挖**。
	///
	/// <para/>实测日志（<c>client.log</c>，玩家进门那一次）里就是这样：03:56:19 进壁炉，
	/// <c>04:00:05</c> 起每秒刷一条「出生点附近 40 格内都放不下壁炉入口，下一轮继续重试」，
	/// 一直刷到 <c>04:03:5x</c>（≈228 条），然后才回到主世界 —— 也就是说这 228 秒全在
	/// **壁炉子世界内部**，每一条背后都是 80 列 × (3 列 × 4 行) 的
	/// <see cref="WorldGen.KillTile"/>。见 <see cref="TryPlaceAt"/> 的注释：
	/// 旧写法把那 4 行里的**地脚砖**也清掉了，于是 <see cref="SurfaceYAt"/> 每轮都往下
	/// 找到新的一层"地表"，**每秒往下啃 4 行、宽 81 格**（子世界里 Main.spawnTileX=400，
	/// 于是这一条正好切过塔顶场地外壳 y=138、塔顶地板 y=287、灰烬层、堡垒屋顶壳 y=860~864、
	/// 门厅楼板 y=970）—— 玩家看到的就是「一部分瑜钢合金自己消失了（被破坏）」。
	/// </summary>
	public class FireplaceGateSystem : ModSystem
	{
		/// <summary>连续失败的轮数：只用来把"每秒一条"的警告压成"每 30 轮一条"。</summary>
		private int failedRounds;

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

			if (GateStillThere()) {
				failedRounds = 0;
				return;
			}

			if (!TryPlace()) {
				return;
			}

			failedRounds = 0;

			if (!WastelandStorySystem.GatePlaced) {
				if (Main.netMode != NetmodeID.Server) {
					Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.GatePlaced"), new Color(226, 150, 80));
				}
			}

			WastelandStorySystem.GatePlaced = true;
			WastelandStorySystem.Sync();
		}

		private static bool GateStillThere()
		{
			if (!WastelandStorySystem.GatePlaced) {
				return false;
			}

			int x = WastelandStorySystem.GateX;
			int y = WastelandStorySystem.GateY;

			if (!WorldGen.InWorld(x, y)) {
				return false;
			}

			return Main.tile[x, y].HasTile && Main.tile[x, y].TileType == ModContent.TileType<FireplaceGate>();
		}

		/// <summary>
		/// 在出生点附近找一块平地放门。
		///
		/// <para/>⚠️ 这里修过一个**真 bug**（玩家反馈"新开的世界找不到壁炉入口，老世界却有"）：
		/// 旧版只试 `spawnTileX - 18` 这一个位置，而且 `PlaceTile` 之后**不看结果就 return true**，
		/// 于是 `GatePlaced` 被置位、重试逻辑再也不跑 —— 一旦那个位置放不下，这个世界就**永远没有入口**了。
		/// 现在改成：沿出生点左右各扫 40 格找平地，放完**逐个核对 3×3 区域里真的出现了门**，
		/// 放不下就返回 false 让上层继续重试，并写一条日志。
		///
		/// <para/>⚠️⚠️ 第二轮修（玩家反馈"进去一段时间后合金自己消失了"）：<b>重试必须是幂等的</b>。
		/// 旧版的失败重试会**每秒把地形往下啃**（见 <see cref="TryPlaceAt"/>），
		/// 所以这里同时做了两件事：
		/// <list type="number">
		/// <item>日志不再每秒刷一条，改成每 30 轮一条（日志本身不是问题，但 228 条一样的告警会淹掉真问题）；</item>
		/// <item>失败时**不重置** `failedRounds`，让同一次卡住只留少量痕迹。</item>
		/// </list>
		/// </summary>
		private bool TryPlace()
		{
			int gateType = ModContent.TileType<FireplaceGate>();

			for (int offset = 0; offset <= 40; offset++) {
				foreach (int sign in offset == 0 ? new[] { 1 } : new[] { 1, -1 }) {
					int x = Main.spawnTileX + (offset * sign);

					if (TryPlaceAt(x, gateType)) {
						return true;
					}
				}
			}

			failedRounds++;

			if (failedRounds == 1 || failedRounds % 30 == 0) {
				Mod.Logger.Warn(string.Format(
					"FireplaceGateSystem: 出生点附近 40 格内都放不下壁炉入口（连续第 {0} 轮），下一轮继续重试",
					failedRounds));
			}

			return false;
		}

		/// <summary>
		/// 在某一列上试放门；成功返回 true 并记下坐标。
		///
		/// <para/>⚠️ 这一格**不能**清（玩家反馈"进去一段时间后一部分瑜钢合金自己消失了"的真根因）：
		/// <list type="bullet">
		/// <item>旧写法 `dy = 0..3` 把 `floor` 那一行**地脚砖**也一起 `KillTile` 掉了。
		/// 门是 3×3，占的是 `floor-3 ~ floor-1` 三行，`floor` 这一行是它的**地基**，本来就不该动；</item>
		/// <item>清掉地脚砖之后，<see cref="SurfaceYAt"/> 下一轮从 y=60 再往下找"第一块实心"就会
		/// 直接**跳过**这一层，落到更下面一层 —— 于是"每秒重试一次"变成"每秒往下挖 4 行、宽 81 格"，
		/// 在壁炉子世界里是沿着 x=360~440 把塔顶场地、灰烬层、堡垒屋顶壳（瑜钢合金）与门厅楼板
		/// 一路啃穿；</item>
		/// <item>留下地脚砖之后，重试会清到同一批（已经是空气的）格子 —— **幂等**，不再往下走；
		/// 而且门真的有了地基，不会放上又立刻因为悬空被判掉。</item>
		/// </list>
		/// </summary>
		private bool TryPlaceAt(int x, int gateType)
		{
			int floor = SurfaceYAt(x);

			if (floor < 0) {
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

			// 核对：3×3 区域里真的出现了门才认成功（否则让上层重试）
			for (int dx = -1; dx <= 1; dx++) {
				for (int dy = -3; dy <= 0; dy++) {
					int tx = x + dx;
					int ty = floor - 1 + dy;

					if (!WorldGen.InWorld(tx, ty)) {
						continue;
					}

					if (Main.tile[tx, ty].HasTile && Main.tile[tx, ty].TileType == gateType) {
						WastelandStorySystem.GateX = tx;
						WastelandStorySystem.GateY = ty;

						if (Main.netMode == NetmodeID.Server) {
							NetMessage.SendTileSquare(-1, x, floor - 2, 5);
						}

						return true;
					}
				}
			}

			return false;
		}

		private static int SurfaceYAt(int x)
		{
			if (!WorldGen.InWorld(x, 100)) {
				return -1;
			}

			for (int y = 60; y < Main.maxTilesY - 200; y++) {
				if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType] && !Main.tileSolidTop[Main.tile[x, y].TileType]) {
					return y;
				}
			}

			return -1;
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
