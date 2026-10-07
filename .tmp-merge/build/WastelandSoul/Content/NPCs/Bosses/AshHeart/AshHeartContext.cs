using System.Collections.Generic;
using InnoVault.StateMachines;
using Terraria;

namespace WastelandSoul.Content.NPCs.Bosses.AshHeart
{
	/// <summary>
	/// 灰烬之心的状态机上下文。ModNPC 是单例，所以每只 Boss 的状态都放在这里。
	/// <para/>槽位：<c>ai[0]</c> 阶段，<c>ai[1]</c> 状态机，<c>ai[2]</c> 出招轮换。
	/// </summary>
	public class AshHeartContext : INpcStateContext
	{
		public const int AiSlot = 1;

		private static readonly Dictionary<int, AshHeartContext> Cache = new Dictionary<int, AshHeartContext>();

		public NPC Npc { get; }

		public NpcStateMachine<AshHeartContext> Machine { get; private set; }

		public Player Target { get; set; }

		public int AttackDelay { get; set; }

		private AshHeartContext(NPC npc)
		{
			Npc = npc;
		}

		public static AshHeartContext For(NPC npc)
		{
			if (!Cache.TryGetValue(npc.whoAmI, out AshHeartContext context) || context.Npc != npc) {
				context = new AshHeartContext(npc);
				context.Machine = new NpcStateMachine<AshHeartContext>(context, AiSlot);
				context.Machine.SetInitialState(new AshHeartIdleState());

				PhaseController.For(context.Machine)
					.OnHpBelow(c => (float)c.Npc.life / c.Npc.lifeMax, AshHeart.PhaseTwoThreshold, () => new AshHeartPhaseTwoState(), label: "阶段二")
					.OnHpBelow(c => (float)c.Npc.life / c.Npc.lifeMax, AshHeart.PhaseThreeThreshold, () => new AshHeartPhaseThreeState(), label: "阶段三")
					.Apply();

				Cache[npc.whoAmI] = context;
			}

			return context;
		}

		public static void Release(int whoAmI)
		{
			Cache.Remove(whoAmI);
		}

		public static void Clear()
		{
			Cache.Clear();
		}
	}
}
