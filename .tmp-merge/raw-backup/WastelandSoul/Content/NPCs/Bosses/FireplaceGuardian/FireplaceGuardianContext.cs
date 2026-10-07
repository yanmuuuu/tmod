using System.Collections.Generic;
using InnoVault.StateMachines;
using Terraria;

namespace WastelandSoul.Content.NPCs.Bosses.FireplaceGuardian
{
	/// <summary>
	/// 壁炉守卫的状态机上下文。槽位与另外两只 Boss 相同：
	/// <c>ai[0]</c> 阶段，<c>ai[1]</c> 状态机，<c>ai[2]</c> 出招轮换。
	/// </summary>
	public class FireplaceGuardianContext : INpcStateContext
	{
		public const int AiSlot = 1;

		private static readonly Dictionary<int, FireplaceGuardianContext> Cache = new Dictionary<int, FireplaceGuardianContext>();

		public NPC Npc { get; }

		public NpcStateMachine<FireplaceGuardianContext> Machine { get; private set; }

		public Player Target { get; set; }

		public int AttackDelay { get; set; }

		private FireplaceGuardianContext(NPC npc)
		{
			Npc = npc;
		}

		public static FireplaceGuardianContext For(NPC npc)
		{
			if (!Cache.TryGetValue(npc.whoAmI, out FireplaceGuardianContext context) || context.Npc != npc) {
				context = new FireplaceGuardianContext(npc);
				context.Machine = new NpcStateMachine<FireplaceGuardianContext>(context, AiSlot);
				context.Machine.SetInitialState(new FireplaceGuardianIdleState());

				PhaseController.For(context.Machine)
					.OnHpBelow(c => (float)c.Npc.life / c.Npc.lifeMax, FireplaceGuardian.PhaseTwoThreshold, () => new FireplaceGuardianPhaseTwoState(), label: "阶段二")
					.OnHpBelow(c => (float)c.Npc.life / c.Npc.lifeMax, FireplaceGuardian.PhaseThreeThreshold, () => new FireplaceGuardianPhaseThreeState(), label: "阶段三")
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
