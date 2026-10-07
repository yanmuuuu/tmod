using System.Collections.Generic;
using InnoVault.StateMachines;
using Terraria;

namespace WastelandSoul.Content.NPCs.Bosses.Scavenger
{
	/// <summary>
	/// 清道夫的状态机上下文：**一只 NPC 一份**。
	/// <para/>状态机本体（含"当前状态 ID"的网络同步）由 InnoVault 的
	/// <see cref="NpcStateMachine{TContext}"/> 负责，存在 <c>NPC.ai[1]</c>；
	/// 这里只放"这一只清道夫"的具体运行时数据。
	/// <para/>槽位分配：ai[1] = 状态机（框架占用）、ai[3] = 掉落判定标记、ai[0] = 阶段序号。
	/// </summary>
	public class ScavengerContext : INpcStateContext
	{
		/// <summary>状态机占用的 <c>NPC.ai[]</c> 槽位。</summary>
		public const int AiSlot = 1;

		private static readonly Dictionary<int, ScavengerContext> Cache = new Dictionary<int, ScavengerContext>();

		/// <inheritdoc/>
		public NPC Npc { get; }

		/// <summary>驱动这只清道夫的状态机。</summary>
		public NpcStateMachine<ScavengerContext> Machine { get; private set; }

		/// <summary>本次的当前目标。</summary>
		public Player Target { get; set; }

		/// <summary>待机状态下多久之后发动下一次攻击（每轮重新掷一次）。</summary>
		public int AttackDelay { get; set; }

		private ScavengerContext(NPC npc)
		{
			Npc = npc;
		}

		/// <summary>
		/// 取得（必要时创建）该 NPC 的上下文，并保证状态机已初始化到「待机」。
		/// </summary>
		public static ScavengerContext For(NPC npc)
		{
			if (!Cache.TryGetValue(npc.whoAmI, out ScavengerContext context) || context.Npc != npc) {
				context = new ScavengerContext(npc);
				context.Machine = new NpcStateMachine<ScavengerContext>(context, AiSlot);
				context.Machine.SetInitialState(new ScavengerIdleState());

				// 阶段：HP 跌破阈值时一次性切到对应的「阶段演出」状态。
				// 阈值降序由框架保证——血从 70% 直接掉到 30% 时只会触发最严格的那条，不会被弱阶段抢先。
				PhaseController.For(context.Machine)
					.OnHpBelow(c => (float)c.Npc.life / c.Npc.lifeMax, Scavenger.PhaseTwoThreshold, () => new ScavengerPhaseTwoState(), label: "阶段二")
					.OnHpBelow(c => (float)c.Npc.life / c.Npc.lifeMax, Scavenger.PhaseThreeThreshold, () => new ScavengerPhaseThreeState(), label: "阶段三")
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
