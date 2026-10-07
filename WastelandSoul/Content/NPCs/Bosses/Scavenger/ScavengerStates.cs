using InnoVault.StateMachines;
using Microsoft.Xna.Framework;

namespace WastelandSoul.Content.NPCs.Bosses.Scavenger
{
	// 说明：
	//  · 每个状态都标了 [VaultState(id, typeof(ScavengerContext))]，由 InnoVault 在加载期反射收集，
	//    状态切换只同步一个整数 ID，联机时不需要再手写 ai[] 同步代码。
	//  · 计时改用框架自带的 VaultState.Timer（OnEnter 自动清零），不再手写 ai[1] 倒计时。
	//  · 返回非 null 即请求切换状态；框架默认服务端权威，客户端会被动同步过去。

	/// <summary>待机：悬浮跟随，倒计时结束后挑一个攻击。</summary>
	[VaultState(0, typeof(ScavengerContext))]
	public class ScavengerIdleState : VaultState<ScavengerContext>
	{
		public override string StateName => "待机";

		public override void OnEnter(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnEnter(machine, ctx);
			ctx.AttackDelay = Scavenger.AttackInterval(ctx.Npc);
		}

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);

			if (ctx.Target != null) {
				Scavenger.Hover(ctx, true);
			}

			if (Timer < ctx.AttackDelay) {
				return null;
			}

			return Scavenger.ChooseNextState(ctx);
		}
	}

	/// <summary>阶段切换演出：只负责改阶段序号、播提示，然后回到待机。</summary>
	[VaultState(1, typeof(ScavengerContext))]
	public class ScavengerPhaseTwoState : VaultState<ScavengerContext>
	{
		public override string StateName => "阶段二·过载修复";

		public override void OnEnter(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnEnter(machine, ctx);
			Scavenger.OnPhaseStart(ctx.Npc, 1);
		}

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);
			return Timer >= 40 ? new ScavengerIdleState() : null;
		}
	}

	/// <summary>阶段切换演出（三阶段）。</summary>
	[VaultState(2, typeof(ScavengerContext))]
	public class ScavengerPhaseThreeState : VaultState<ScavengerContext>
	{
		public override string StateName => "阶段三·协议崩溃";

		public override void OnEnter(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnEnter(machine, ctx);
			Scavenger.OnPhaseStart(ctx.Npc, 2);
		}

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);
			return Timer >= 40 ? new ScavengerIdleState() : null;
		}
	}

	/// <summary>机械臂横扫前摇：压进近战距离。</summary>
	[VaultState(3, typeof(ScavengerContext))]
	public class ScavengerSweepWindupState : VaultState<ScavengerContext>
	{
		public override string StateName => "机械臂前摇";

		public override void OnEnter(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnEnter(machine, ctx);
			Scavenger.PlayWindupSound(ctx.Npc);
		}

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);

			if (ctx.Target != null) {
				Scavenger.LungeTowards(ctx.Npc, ctx.Target, 120f, 12f);
			}

			if (Timer >= Scavenger.SweepWindup) {
				Scavenger.ExecuteSweep(ctx.Npc, ctx.Target);
				return new ScavengerSweepingState();
			}

			return null;
		}
	}

	/// <summary>机械臂横扫中：继续压住玩家，让机械臂与机体都保持在接触范围内。</summary>
	[VaultState(4, typeof(ScavengerContext))]
	public class ScavengerSweepingState : VaultState<ScavengerContext>
	{
		public override string StateName => "机械臂横扫";

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);

			if (ctx.Target != null) {
				Scavenger.LungeTowards(ctx.Npc, ctx.Target, 110f, 9f);
			}

			return Timer >= 40 ? new ScavengerIdleState() : null;
		}
	}

	/// <summary>锁定射击前摇。</summary>
	[VaultState(5, typeof(ScavengerContext))]
	public class ScavengerBurstWindupState : VaultState<ScavengerContext>
	{
		public override string StateName => "锁定前摇";

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);

			if (ctx.Target != null) {
				Scavenger.Hover(ctx, false);
			}

			if (Timer >= Scavenger.BurstWindup) {
				Scavenger.FireBurst(ctx.Npc, ctx.Target);
				return new ScavengerIdleState();
			}

			return null;
		}
	}

	/// <summary>过载警报：机体抖动，然后冲锋。</summary>
	[VaultState(6, typeof(ScavengerContext))]
	public class ScavengerChargeWindupState : VaultState<ScavengerContext>
	{
		public override string StateName => "过载警报";

		public override void OnEnter(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnEnter(machine, ctx);
			// 进入过载序列：这次结束不算「玩家击败」
			ctx.Npc.ai[3] = Scavenger.OverloadFailureMarker;
			Scavenger.PlayOverloadWarning(ctx);
		}

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);

			ctx.Npc.velocity *= 0.90f;
			Scavenger.EmitOverloadDust(ctx.Npc);

			if (Timer >= Scavenger.ChargeWindup && ctx.Target != null) {
				Scavenger.ExecuteCharge(ctx.Npc, ctx.Target);
				return new ScavengerChargingState();
			}

			return null;
		}
	}

	/// <summary>冲锋中：每帧朝目标缓慢转向，撞墙或超时即自毁。</summary>
	[VaultState(7, typeof(ScavengerContext))]
	public class ScavengerChargingState : VaultState<ScavengerContext>
	{
		public override string StateName => "过载冲锋";

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);

			if (ctx.Target != null) {
				Scavenger.SteerCharge(ctx.Npc, ctx.Target);
			}

			Scavenger.MarkChargeHit(ctx.Npc);

			if (Timer >= 90 || Scavenger.ChargeBlocked(ctx.Npc)) {
				Scavenger.BeginSelfDestruct(ctx.Npc);
				return new ScavengerSelfDestructState();
			}

			return null;
		}
	}

	/// <summary>解体倒计时。</summary>
	[VaultState(8, typeof(ScavengerContext))]
	public class ScavengerSelfDestructState : VaultState<ScavengerContext>
	{
		public override string StateName => "过载解体";

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);

			ctx.Npc.velocity *= 0.85f;

			if (Timer >= 30) {
				Scavenger.Explode(ctx.Npc);
			}

			return null;
		}
	}

	/// <summary>
	/// 机体撞击前摇：短暂蓄力（先悬浮保持中距离），然后整个人压向玩家。
	/// <para/>这是阶段一/二真正会「碰到玩家」的手段——只靠悬浮 + 机械臂横扫，机体永远够不到人。
	/// </summary>
	[VaultState(9, typeof(ScavengerContext))]
	public class ScavengerSlamWindupState : VaultState<ScavengerContext>
	{
		public override string StateName => "撞击前摇";

		public override void OnEnter(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnEnter(machine, ctx);
			Scavenger.PlayWindupSound(ctx.Npc);
		}

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);

			if (ctx.Target != null) {
				Scavenger.Hover(ctx, true);
			}

			if (Timer >= Scavenger.SlamWindup && ctx.Target != null) {
				Scavenger.ExecuteSlam(ctx.Npc, ctx.Target);
				return new ScavengerSlamState();
			}

			return null;
		}
	}

	/// <summary>机体撞击中：朝玩家撞过去，接触伤害提高一档；撞完恢复正常。</summary>
	[VaultState(10, typeof(ScavengerContext))]
	public class ScavengerSlamState : VaultState<ScavengerContext>
	{
		public override string StateName => "机体撞击";

		public override IVaultState<ScavengerContext> OnUpdate(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnUpdate(machine, ctx);

			if (ctx.Target != null) {
				Scavenger.SteerSlam(ctx.Npc, ctx.Target);
			}

			return Timer >= 40 ? new ScavengerIdleState() : null;
		}

		public override void OnExit(VaultStateMachine<ScavengerContext> machine, ScavengerContext ctx)
		{
			base.OnExit(machine, ctx);
			Scavenger.EndSlam(ctx.Npc);
		}
	}
}
