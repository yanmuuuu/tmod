using InnoVault.StateMachines;
using Terraria.Audio;
using Terraria.ID;

namespace WastelandSoul.Content.NPCs.Bosses.AshHeart
{
	[VaultState(0, typeof(AshHeartContext))]
	public class AshHeartIdleState : VaultState<AshHeartContext>
	{
		public override string StateName => "余烬待机";

		public override void OnEnter(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnEnter(machine, ctx);
			ctx.AttackDelay = AshHeart.RollAttackDelay(ctx);
			ctx.Npc.damage = AshHeart.ContactDamage;
			ctx.Npc.defDamage = AshHeart.ContactDamage;
		}

		public override IVaultState<AshHeartContext> OnUpdate(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnUpdate(machine, ctx);
			AshHeart.Hover(ctx, AshHeart.HoverDefault);
			return Timer >= ctx.AttackDelay ? AshHeart.ChooseNextState(ctx) : null;
		}
	}

	[VaultState(1, typeof(AshHeartContext))]
	public class AshHeartPhaseTwoState : VaultState<AshHeartContext>
	{
		public override string StateName => "阶段二·自持";

		public override void OnEnter(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnEnter(machine, ctx);
			AshHeart.OnPhaseStart(ctx.Npc, 1);
			AshHeart.SpawnWisps(ctx, 2);
		}

		public override IVaultState<AshHeartContext> OnUpdate(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnUpdate(machine, ctx);
			ctx.Npc.velocity *= 0.92f;
			return Timer >= 46 ? new AshHeartIdleState() : null;
		}
	}

	[VaultState(2, typeof(AshHeartContext))]
	public class AshHeartPhaseThreeState : VaultState<AshHeartContext>
	{
		public override string StateName => "阶段三·坍缩";

		public override void OnEnter(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnEnter(machine, ctx);
			AshHeart.OnPhaseStart(ctx.Npc, 2);
		}

		public override IVaultState<AshHeartContext> OnUpdate(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnUpdate(machine, ctx);
			ctx.Npc.velocity *= 0.9f;
			return Timer >= 50 ? new AshHeartIdleState() : null;
		}
	}

	[VaultState(3, typeof(AshHeartContext))]
	public class AshHeartVolleyState : VaultState<AshHeartContext>
	{
		public override string StateName => "余烬扇";

		public override void OnEnter(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnEnter(machine, ctx);
			ctx.Npc.velocity *= 0.4f;

			if (!Terraria.Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item20, ctx.Npc.Center);
			}
		}

		public override IVaultState<AshHeartContext> OnUpdate(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnUpdate(machine, ctx);
			AshHeart.Hover(ctx, AshHeart.HoverDefault);

			if (Timer < AshHeart.VolleyWindup) {
				return null;
			}

			int count = (int)ctx.Npc.ai[0] >= 2 ? 7 : (int)ctx.Npc.ai[0] == 1 ? 5 : 4;
			AshHeart.ThrowOrbs(ctx, count, 7.5f);
			return new AshHeartIdleState();
		}
	}

	[VaultState(4, typeof(AshHeartContext))]
	public class AshHeartPoolState : VaultState<AshHeartContext>
	{
		public override string StateName => "灰池";

		public override IVaultState<AshHeartContext> OnUpdate(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnUpdate(machine, ctx);
			AshHeart.Hover(ctx, AshHeart.HoverHigh);

			if (Timer < AshHeart.PoolWindup) {
				return null;
			}

			AshHeart.DropPool(ctx);

			if ((int)ctx.Npc.ai[0] >= 1) {
				AshHeart.DropPool(ctx);
			}

			return new AshHeartIdleState();
		}
	}

	[VaultState(5, typeof(AshHeartContext))]
	public class AshHeartRainState : VaultState<AshHeartContext>
	{
		/// <summary>本招是否已经触发过（用 >= 判断，避免掉帧直接跨过阈值）。</summary>
		private bool firedAshHeartRain;
		public override string StateName => "灰雨";

		public override void OnEnter(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			firedAshHeartRain = false;
			base.OnEnter(machine, ctx);

			if (!Terraria.Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item45, ctx.Npc.Center);
			}
		}

		public override IVaultState<AshHeartContext> OnUpdate(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnUpdate(machine, ctx);
			AshHeart.Hover(ctx, AshHeart.HoverHigh);

			if (Timer < AshHeart.RainWindup) {
				return null;
			}

			if (!firedAshHeartRain && Timer >= AshHeart.RainWindup) {
				firedAshHeartRain = true;
				AshHeart.SpawnRain(ctx);
			}

			return Timer >= AshHeart.RainWindup + 70 ? new AshHeartIdleState() : null;
		}
	}

	[VaultState(6, typeof(AshHeartContext))]
	public class AshHeartPulseState : VaultState<AshHeartContext>
	{
		/// <summary>本招是否已经触发过（用 >= 判断，避免掉帧直接跨过阈值）。</summary>
		private bool firedAshHeartPulse;
		public override string StateName => "坍缩脉冲";

		public override void OnEnter(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			firedAshHeartPulse = false;
			base.OnEnter(machine, ctx);
			ctx.Npc.velocity *= 0.2f;

			if (!Terraria.Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item14, ctx.Npc.Center);
			}
		}

		public override IVaultState<AshHeartContext> OnUpdate(VaultStateMachine<AshHeartContext> machine, AshHeartContext ctx)
		{
			base.OnUpdate(machine, ctx);
			AshHeart.Hover(ctx, AshHeart.HoverDefault);

			if (!firedAshHeartPulse && Timer >= AshHeart.PulseWindup) {
				firedAshHeartPulse = true;
				AshHeart.SpawnPulse(ctx);
			}

			return Timer >= AshHeart.PulseWindup + 20 ? new AshHeartIdleState() : null;
		}
	}
}
