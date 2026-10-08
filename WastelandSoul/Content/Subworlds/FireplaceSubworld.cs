using System.Collections.Generic;
using System.IO;
using SubworldLibrary;
using Terraria.GameContent.Generation;
using Terraria.WorldBuilding;

namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 「壁炉」—— 旧世界的最后庇护所，做成一整个**独立的小世界**（Subworld Library 维度）。
	///
	/// <para/>为什么用子世界而不是在主世界挖一块：壁炉要表现的是「和外面那片废土不是同一个地方」，
	/// 独立世界才有干净的尺寸与光照语义；而且 Subworld Library 会把太空 / 两侧海洋 / 地狱
	/// 从子世界里去掉，所以世界可以做得小而不出问题（但这里刻意做**高**，因为塔要通往宇宙）。
	///
	/// <para/>尺寸 **900 × 1300**，分层见 <see cref="FireplaceLayout"/>。
	///
	/// <para/>生成分六步（每步一个 <see cref="GenPass"/>，进度条上能看到在干什么）：
	/// <list type="number">
	/// <item>灰烬原野（噪声地表 + 分层地层 + 灰烬丘 + 少量岩浆/污染水 + 黑曜石巨岩）</item>
	/// <item>堡垒（瑜钢合金外壳 + 地狱砖墙 + 门厅/壁炉/终端/返回门 + 原版家具）</item>
	/// <item>祈塔（14 层塔身 + 塔顶 Boss 场地）</item>
	/// <item>地下通道（笔直、60 格高、通到世界边缘）</item>
	/// <item>椭球地宫（堡垒下方的合金椭球空腔 + 房间/门/竖井，见 <see cref="FireplaceVault"/>）</item>
	/// <item>收尾（按区域框架化 + 落点 + worldSurface/rockLayer + 背景样式初值）</item>
	/// </list>
	///
	/// <para/>关于 API（都被编译器纠正过，别再凭印象写）：
	/// <list type="bullet">
	/// <item><c>Width</c> / <c>Height</c> / <c>Tasks</c> 是**抽象属性**，必须 override；
	/// <c>Tasks</c> 要返回一个列表，不能 <c>Tasks.Add(...)</c>。</item>
	/// <item><c>FileName</c> 是普通属性（不是 virtual），只能读。</item>
	/// <item><c>SetupContent()</c> 是 sealed，内容初始化要写在 <c>OnLoad()</c> 里。</item>
	/// </list>
	/// </summary>
	public class FireplaceSubworld : Subworld
	{
		private readonly List<GenPass> tasks = new List<GenPass>();

		/// <summary>
		/// 本次进入壁炉是**新生成**的（而不是从 <c>.wld</c> 读档）。
		///
		/// <para/>为什么需要这个区分：SubworldLibrary 的 <c>LoadWorld()</c> 有两条路 ——
		/// 有存档文件时走 <c>Subworld.ReadFile(reader)</c>（读档），没文件时走
		/// <c>SubworldSystem.LoadSubworld()</c>（生成）；**两条路最后都会调一次
		/// <c>Subworld.OnLoad()</c>**，所以光看 <c>OnLoad()</c> 分不出是哪条。
		/// 而 <c>ReadFile</c> 只在读档路径被调用（IL 已核：<c>LoadWorldFile</c> 里
		/// <c>status = current != null ? current.ReadFile(reader) : WorldFile.LoadWorld_Version2(reader)</c>），
		/// 且它一定发生在 <c>OnLoad()</c> **之前**，所以用它当旗标是最省事的判据。
		///
		/// <para/>用途见 <see cref="Common.Systems.FireplaceEntrySystem"/>：新生成的那一次
		/// 需要补一次重载，读档的那次不需要。
		/// </summary>
		public static bool GeneratedFresh { get; private set; }

		/// <summary>本次进入是否真的读过 <c>.wld</c>（只在 <see cref="ReadFile"/> 里置位，
		/// 在 <see cref="OnLoad"/> 里消费并清零）。</summary>
		private static bool readFromFile;

		/// <summary>世界宽度（格）。</summary>
		public override int Width => FireplaceLayout.Width;

		/// <summary>世界高度（格）。</summary>
		public override int Height => FireplaceLayout.Height;

		/// <summary>生成步骤。</summary>
		public override List<GenPass> Tasks => tasks;

		/// <summary>
		/// 读档入口（只有"壁炉已经有 <c>.wld</c>"时才会被前置调用）。
		/// <para/>这里只做一件事：留个"读过档"的记号，真正的加载仍然交给基类
		/// （基类实现就是 <c>WorldFile.LoadWorld_Version2(reader)</c> + 补 Name / Metadata，
		/// IL 已核对，不会漏掉任何步骤）。
		/// </summary>
		public override int ReadFile(BinaryReader reader)
		{
			readFromFile = true;
			return base.ReadFile(reader);
		}

		/// <inheritdoc/>
		public override void OnLoad()
		{
			// 先定调"这次是新生成还是读档"——后面注册步骤时抛异常也不会让这个判断留在上一次的状态。
			// 联机客户端要额外排除：它根本不本地生成世界（世界是子世界专用服务端发过来的），
			// 但前置会在 Player.Hooks.OnEnterWorld 里再调一次 OnLoad()，那时 readFromFile 也是 false。
			// 第三项兜住"存档坏了"这条窄路：文件在但读失败时，前置会回落到 LoadSubworld() 真去生成，
			// 这时候 ReadFile 已经调过了，光看 readFromFile 会把"刚生成"误判成"读档"。
			// WorldGen.loadFailed 每次 LoadWorld 开头都会清零，用它是安全的。
			GeneratedFresh = (!readFromFile || Terraria.WorldGen.loadFailed)
				&& Terraria.Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient;
			readFromFile = false;

			Mod.Logger.Info("[壁炉] 本次进入：" + (GeneratedFresh ? "本机首次生成" : "从存档读入"));

			// 只跑我们自己这几步：壁炉是"被浇铸出来的人工世界"，
			// 不需要原版的地形 / 洞穴 / 矿物 / 丛林等生成步骤（那会把结构打乱）。
			//
			// ⚠️ 先判空再注册：SubworldLibrary 每次**进入**子世界都会调一次 OnLoad()，
			// 无脑 Add 会让步骤表随进门次数线性膨胀（第二次进门 12 步、第三次 18 步…）。
			// 平时只有首次进入才会真的跑到生成，所以这是个潜伏 bug；一旦 .wld 被删掉重生成
			// 就会原地把每个建筑盖 N 遍。这里只保留"第一次注册"的语义。
			if (tasks.Count == 0) {
				tasks.Add(new PassLegacy("WastelandSoul: 灰烬原野", FireplaceTerrain.Build));
				tasks.Add(new PassLegacy("WastelandSoul: 壁炉堡垒", FireplaceBuildings.BuildFortress));
				tasks.Add(new PassLegacy("WastelandSoul: 祈塔", FireplaceBuildings.BuildTower));
				tasks.Add(new PassLegacy("WastelandSoul: 地下通道", FireplaceBuildings.BuildTunnel));
				tasks.Add(new PassLegacy("WastelandSoul: 椭球地宫", FireplaceVault.Build));
				tasks.Add(new PassLegacy("WastelandSoul: 收尾", FireplaceBuildings.Finish));
			}

			Mod.Logger.Info(string.Format("[壁炉生成] 已注册 {0} 个生成步骤（世界 {1}x{2}）",
				tasks.Count, FireplaceLayout.Width, FireplaceLayout.Height));
			FireplaceGenLog.Reset();
		}
	}
}
