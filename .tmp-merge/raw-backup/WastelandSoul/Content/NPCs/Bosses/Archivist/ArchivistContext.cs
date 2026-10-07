using System.Collections.Generic;
using InnoVault.StateMachines;
using Microsoft.Xna.Framework;
using Terraria;

namespace WastelandSoul.Content.NPCs.Bosses.Archivist
{
	/// <summary>
	/// 归档者的状态机上下文：**一只 NPC 一份**（ModNPC 实例是所有同类共用的，所以状态不能放实例字段）。
	/// <para/>状态机本体（含"当前状态 ID"的网络同步）由 InnoVault 的
	/// <see cref="NpcStateMachine{TContext}"/> 负责，存在 <c>NPC.ai[1]</c>。
	/// <para/>槽位分配：
	/// <list type="bullet">
	/// <item><c>ai[0]</c> = 阶段序号（0 检索 / 1 归档 / 2 覆写）</item>
	/// <item><c>ai[1]</c> = 状态机（框架占用）</item>
	/// <item><c>ai[2]</c> = 出招轮换计数</item>
	/// <item><c>ai[3]</c> = 索引光束的起始角度（发射时锁定，供弹幕读取）</item>
	/// </list>
	/// </summary>
	public class ArchivistContext : INpcStateContext
	{
		/// <summary>状态机占用的 <c>NPC.ai[]</c> 槽位。</summary>
		public const int AiSlot = 1;

		/// <summary>轨迹采样间隔（帧）。2 帧一采 → 120 个采样点 ≈ 最近 4 秒。</summary>
		public const int TrailSampleInterval = 2;

		/// <summary>轨迹最多保留多少个采样点（「覆写」阶段回放最近约 2 秒用）。</summary>
		public const int MaxTrailPoints = 60;

		private static readonly Dictionary<int, ArchivistContext> Cache = new Dictionary<int, ArchivistContext>();

		/// <inheritdoc/>
		public NPC Npc { get; }

		/// <summary>驱动这只归档者的状态机。</summary>
		public NpcStateMachine<ArchivistContext> Machine { get; private set; }

		/// <summary>本次的当前目标。</summary>
		public Player Target { get; set; }

		/// <summary>待机状态下多久之后发动下一次攻击（每轮重新掷一次）。</summary>
		public int AttackDelay { get; set; }

		/// <summary>「终盘压制」的提示是否已经播过（这招会被反复进入，只报一次）。</summary>
		public bool SuppressionAnnounced { get; set; }

		/// <summary>
		/// 玩家最近的移动轨迹（「覆写」阶段沿这条路径回放攻击——「你的路径已被归档」）。
		/// <para/>[0] 是最旧的采样点，末尾是最新的。采样在 <c>Archivist.TrackTarget</c> 里做。
		/// </summary>
		public readonly List<Vector2> TargetTrail = new List<Vector2>();

		private ArchivistContext(NPC npc)
		{
			Npc = npc;
		}

		/// <summary>取得（必要时创建）该 NPC 的上下文，并保证状态机已初始化到「检索待机」。</summary>
		public static ArchivistContext For(NPC npc)
		{
			if (!Cache.TryGetValue(npc.whoAmI, out ArchivistContext context) || context.Npc != npc) {
				context = new ArchivistContext(npc);
				context.Machine = new NpcStateMachine<ArchivistContext>(context, AiSlot);
				context.Machine.SetInitialState(new ArchivistIdleState());

				// 阶段：HP 跌破阈值时一次性切到对应的「阶段演出」状态。
				// 阈值降序由框架保证——血从 70% 直接掉到 30% 时只会触发最严格的那条。
				PhaseController.For(context.Machine)
					.OnHpBelow(c => (float)c.Npc.life / c.Npc.lifeMax, Archivist.PhaseTwoThreshold, () => new ArchivistPhaseTwoState(), label: "阶段二")
					.OnHpBelow(c => (float)c.Npc.life / c.Npc.lifeMax, Archivist.PhaseThreeThreshold, () => new ArchivistPhaseThreeState(), label: "阶段三")
					.Apply();

				Cache[npc.whoAmI] = context;
			}

			return context;
		}

		/// <summary>死亡/退场后清理，避免 whoAmI 被复用后拿到旧上下文。</summary>
		public static void Release(int whoAmI)
		{
			Cache.Remove(whoAmI);
		}

		/// <summary>换世界时清空。</summary>
		public static void Clear()
		{
			Cache.Clear();
		}
	}
}
