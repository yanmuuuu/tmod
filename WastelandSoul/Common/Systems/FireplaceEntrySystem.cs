using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Content.Subworlds;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 「第一次进壁炉」的自动收尾：让玩家**进去就能看到生成好的世界**，不用自己出去再进一次。
	///
	/// <para/>=== 为什么需要这个补丁（SubworldLibrary 2.3.0.1 / buildVersion 2026.6.3.6 的实际机制） ===
	/// 反编译前置后确认的事实（都是 IL 级别核过的，不是猜的）：
	/// <list type="bullet">
	/// <item>进子世界时 <c>SubworldSystem.ExitWorldCallBack</c> 会开一个 Task 跑
	/// <c>SubworldSystem.LoadWorld()</c>；<c>LoadWorld()</c> 只有两条路：
	/// 有 <c>.wld</c> 就 <c>Subworld.ReadFile(reader)</c>（读档），没文件就
	/// <c>SubworldSystem.LoadSubworld()</c>（生成）；两条路最后都一样地做
	/// <c>Main.sectionManager.SetAllSectionsLoaded()</c> + <c>Main.QueueMainThreadAction(SpawnPlayer)</c>。</item>
	/// <item><c>Subworld.OnEnter()</c> 名字像"进入之后的钩子"，其实在
	/// <c>ExitWorldCallBack</c> 里**先于** <c>LoadWorld()</c> 被调用（IL 顺序：
	/// <c>current.OnEnter()</c> → … → <c>LoadWorld()</c>），在里面根本碰不到新世界的数据；
	/// <c>OnLoad()</c> / <c>PostReadFile()</c> 都在生成/读档流程内部。</item>
	/// <item><c>SubworldSystem</c> 的公开面只有 <c>Current</c> / <c>CurrentPath</c> /
	/// <c>IsActive</c> / <c>AnyActive</c> / <c>Enter</c> / <c>Exit</c> /
	/// <c>MovePlayerToSubworld</c> / <c>MovePlayerToMainWorld</c> / <c>GetIndex</c> /
	/// <c>CopyWorldData</c> / <c>ReadCopiedWorldData</c> / <c>StartSubserver</c> /
	/// <c>PreloadSubserver</c> / <c>StopSubserver</c> / <c>SendTo*</c> —— **没有"重新加载 /
	/// 刷新图格"的入口**，前置自己的生成路径也已经调过 <c>SetAllSectionsLoaded</c>，
	/// 所以没有"漏调某个刷新"可以补（方案 b 不成立）。</item>
	/// </list>
	/// 因此采用方案 (a)：**新生成的那一次，由我们把玩家本来要手动做的那一遍"出去再进来"自动做掉**。
	/// 第二次进入走的是读档路径（<c>ReadFile</c>），方块正常，这也是玩家验证过的现象。
	///
	/// <para/>=== 什么时候才触发（两道闸门，缺一不可） ===
	/// <list type="number">
	/// <item><see cref="FireplaceSubworld.GeneratedFresh"/> —— **本次进入是"新生成"**，不是读档。
	/// 只在真正生成的那一次才需要补，老存档 / 联机提前生成过的存档进门时**一点都不会被打扰**。</item>
	/// <item><see cref="WastelandStorySystem.fireplaceSeen"/> —— 本存档还没做过这件事。
	/// 一旦决定要做就**立刻**置位（随存档保存 + 联机同步），所以哪怕中途失败、玩家半路自己跑掉，
	/// 也绝不会再来第二次；最坏情况只是退回"玩家自己出去再进一次"的老样子。</item>
	/// </list>
	///
	/// <para/>=== 只在单人（netMode 0）生效 ===
	/// 联机时客户端是**重连到子世界专用服务端**拿世界的（SubworldLibrary 的 subserver 机制），
	/// 走的是普通联机进世界流程，不存在"生成完不刷新"这条路径；在客户端上再踢一次反而会把玩家
	/// 从队友身边挤走。所以这里只在 <see cref="NetmodeID.SinglePlayer"/> 下动手。
	/// </summary>
	public class FireplaceEntrySystem : ModSystem
	{
		/// <summary>进入壁炉后等多久再自动重载（tick）。90 ≈ 1.5 秒：
		/// 够玩家看清"我进来了"、也来得及读提示，又短到不像卡住。</summary>
		private const int SettleDelay = 90;

		/// <summary>回到主世界后、正式"再进一次"之前再等多久（tick）。
		/// 这 1 秒不是装饰：SubworldLibrary 的 <c>Enter&lt;T&gt;()</c> 开头有一句
		/// <c>if (current != cache) return false;</c>，而 <c>cache</c> 是在
		/// <c>Player.Hooks.OnEnterWorld</c> 里才被同步成 <c>current</c> 的；
		/// 主世界刚落地那一瞬间两者可能还没对齐，这时候敲门会被无声地拒掉。</summary>
		private const int GraceDelay = 60;

		/// <summary>每个阶段的最长等待（tick）。超时就放弃 —— 绝不把玩家困在状态机里。
		/// 20 秒足够：Exit() 一调 <c>Main.gameMenu</c> 就变 true，计时器其实只在
		/// "还在壁炉里、Exit 没生效"这种情况下才会真的往下走。</summary>
		private const int PhaseTimeout = 60 * 20;

		/// <summary>等"重新进入壁炉"的最长时间（tick）。壁炉的 <c>.wld</c> 已经存在，
		/// 这一步只是读档，十几秒足够；超了就当没进去、安静重试。</summary>
		private const int ReturnTimeout = 60 * 15;

		/// <summary>"再进一次"最多敲几次门（第一次正常提示，之后安静重试；
		/// 用来兜住 <c>current != cache</c> 那个窗口）。</summary>
		private const int MaxReenterAttempts = 3;

		/// <summary>状态机的阶段。</summary>
		private enum Phase
		{
			/// <summary>还没进门，或本存档早就处理过了。</summary>
			Idle,

			/// <summary>已经在壁炉里，等那 <see cref="SettleDelay"/> 个 tick。</summary>
			Settling,

			/// <summary>已经请求退出，等回到主世界；回到之后还要过一个 <see cref="GraceDelay"/>。</summary>
			Leaving,

			/// <summary>已经把"再进一次"发出去了，等壁炉重新加载完。</summary>
			Returning,

			/// <summary>本存档结束了，不再做任何事。</summary>
			Done
		}

		private Phase phase = Phase.Idle;
		private int timer;
		private int grace;
		private int attempts;

		/// <summary>本机是不是"单人"这一侧（见类注释：联机不做这件事）。</summary>
		private static bool RunsHere()
		{
			return !Main.dedServ && Main.netMode == NetmodeID.SinglePlayer;
		}

		/// <summary>
		/// 现在是不是"人真的站在壁炉里"。
		/// <para/>⚠️ 必须同时看 <c>Main.gameMenu</c>：SubworldLibrary 在
		/// <c>BeginEntering</c> 里就先把 <c>current</c> 换成目标子世界了，所以**整个加载/生成过程**
		/// 里 <c>IsActive&lt;FireplaceSubworld&gt;()</c> 都是 true。
		/// <c>Main.gameMenu</c> 只在 <c>SpawnPlayer()</c>（世界真的加载完、玩家落位）时才变回 false，
		/// 用它才能把"正在生成"和"已经进来"分开。
		/// </summary>
		private static bool InFireplace()
		{
			return !Main.gameMenu && SubworldSystem.IsActive<FireplaceSubworld>();
		}

		/// <inheritdoc/>
		public override void PostUpdateWorld()
		{
			if (!RunsHere()) {
				return;
			}

			switch (phase) {
				case Phase.Idle:
					TryStart();
					return;

				case Phase.Settling:
					StepSettling();
					return;

				case Phase.Leaving:
					StepLeaving();
					return;

				case Phase.Returning:
					StepReturning();
					return;
			}
		}

		/// <summary>人在壁炉里、而且是新生成的一次 → 置位 + 报信 + 进倒计时。</summary>
		private void TryStart()
		{
			if (WastelandStorySystem.fireplaceSeen) {
				// 本存档做过了：彻底退出状态机，之后每次进门都不再被碰
				phase = Phase.Done;
				return;
			}

			if (!InFireplace() || !FireplaceSubworld.GeneratedFresh) {
				return;
			}

			// ⚠️ 顺序很重要：**先置位再动手**。这样即便下面的 Exit / Enter 失败，
			// 也不会重复触发（否则玩家会被反复踢出世界）。
			WastelandStorySystem.MarkFireplaceSeen();

			Mod.Logger.Info("FireplaceEntrySystem: 壁炉是本存档首次生成，1.5 秒后自动补一次「出去再进来」");

			Main.NewText(
				Language.GetTextValue("Mods.WastelandSoul.Messages.FireplaceForming"),
				new Microsoft.Xna.Framework.Color(226, 170, 120));

			timer = SettleDelay;
			phase = Phase.Settling;
		}

		/// <summary>等 1.5 秒然后退出壁炉。</summary>
		private void StepSettling()
		{
			if (!InFireplace()) {
				// 玩家在这 1.5 秒里自己出去了：他已经在外面，别再 Exit（会把他来回踢），
				// 也不要再 Enter —— 世界早就生成好了，他自己回来时看到的就是好的。
				Mod.Logger.Info("FireplaceEntrySystem: 玩家在自动重载前自己离开了壁炉，本次不再动手");
				phase = Phase.Done;
				return;
			}

			if (--timer > 0) {
				return;
			}

			FireplaceTravel.Exit();
			timer = PhaseTimeout;
			grace = GraceDelay;
			phase = Phase.Leaving;
		}

		/// <summary>等回到主世界（再留一点余量），然后请求再进一次。</summary>
		private void StepLeaving()
		{
			// Exit 之后 Main.gameMenu 会被置 true（加载中 → PostUpdateWorld 本来也不会被调），
			// 等它变回 false 且已经不在子世界里，才算"主世界重新加载完"。
			if (!Main.gameMenu && !SubworldSystem.IsActive<FireplaceSubworld>()) {
				if (--grace > 0) {
					return;
				}

				// 第一次正常提示，之后安静重试
				FireplaceTravel.Enter(announce: attempts == 0);
				attempts++;
				timer = ReturnTimeout;
				phase = Phase.Returning;
				return;
			}

			if (--timer <= 0) {
				Mod.Logger.Warn("FireplaceEntrySystem: 等不到退出壁炉，放弃本次自动重载（玩家可自己再进一次）");
				phase = Phase.Done;
			}
		}

		/// <summary>等壁炉重新加载完；到了就收工。</summary>
		private void StepReturning()
		{
			if (InFireplace()) {
				Mod.Logger.Info("FireplaceEntrySystem: 已重新进入壁炉，本次收尾完成");
				phase = Phase.Done;
				return;
			}

			if (--timer > 0) {
				return;
			}

			// 可能是 current != cache 的窗口把门挡住了 —— 安静地再敲一次
			if (attempts < MaxReenterAttempts) {
				Mod.Logger.Info("FireplaceEntrySystem: 还没进壁炉，安静重试第 " + (attempts + 1) + " 次");
				FireplaceTravel.Enter(announce: false);
				attempts++;
				timer = ReturnTimeout;
				return;
			}

			Mod.Logger.Warn("FireplaceEntrySystem: 重试 " + attempts + " 次仍没能重新进入壁炉，放弃（玩家可自己再进一次）");
			phase = Phase.Done;
		}
	}
}
