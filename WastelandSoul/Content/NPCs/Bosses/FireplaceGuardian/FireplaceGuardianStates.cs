using InnoVault.StateMachines;
using Terraria.Audio;
using Terraria.ID;

namespace WastelandSoul.Content.NPCs.Bosses.FireplaceGuardian
{
	[VaultState(0, typeof(FireplaceGuardianContext))]
	public class FireplaceGuardianIdleState : VaultState<FireplaceGuardianContext>
	{
		public override string StateName => "炉心待机";

		public override void OnEnter(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnEnter(machine, ctx);
			ctx.AttackDelay = FireplaceGuardian.RollAttackDelay(ctx);
			ctx.Npc.damage = FireplaceGuardian.ContactDamage;
			ctx.Npc.defDamage = FireplaceGuardian.ContactDamage;
		}

		public override IVaultState<FireplaceGuardianContext> OnUpdate(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnUpdate(machine, ctx);
			FireplaceGuardian.Hover(ctx, FireplaceGuardian.HoverDefault);
			return Timer >= ctx.AttackDelay ? FireplaceGuardian.ChooseNextState(ctx) : null;
		}
	}

	[VaultState(1, typeof(FireplaceGuardianContext))]
	public class FireplaceGuardianPhaseTwoState : VaultState<FireplaceGuardianContext>
	{
		public override string StateName => "阶段二·冷燃";

		public override void OnEnter(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnEnter(machine, ctx);
			FireplaceGuardian.OnPhaseStart(ctx.Npc, 1);
		}

		public override IVaultState<FireplaceGuardianContext> OnUpdate(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnUpdate(machine, ctx);
			ctx.Npc.velocity *= 0.92f;
			return Timer >= 44 ? new FireplaceGuardianIdleState() : null;
		}
	}

	[VaultState(2, typeof(FireplaceGuardianContext))]
	public class FireplaceGuardianPhaseThreeState : VaultState<FireplaceGuardianContext>
	{
		public override string StateName => "阶段三·重置";

		public override void OnEnter(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnEnter(machine, ctx);
			FireplaceGuardian.OnPhaseStart(ctx.Npc, 2);
		}

		public override IVaultState<FireplaceGuardianContext> OnUpdate(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnUpdate(machine, ctx);
			ctx.Npc.velocity *= 0.9f;
			return Timer >= 50 ? new FireplaceGuardianIdleState() : null;
		}
	}

	[VaultState(3, typeof(FireplaceGuardianContext))]
	public class FireplaceGuardianBoltState : VaultState<FireplaceGuardianContext>
	{
		/// <summary>本招是否已经触发过（用 >= 判断，避免掉帧直接跨过阈值）。</summary>
		private bool firedFireplaceGuardianBolt;
		public override string StateName => "螺栓波";

		public override void OnEnter(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			firedFireplaceGuardianBolt = false;
			base.OnEnter(machine, ctx);

			if (!Terraria.Main.dedServ) {
				SoundEngine.PlaySound(SoundID.Item12, ctx.Npc.Center);
			}
		}

		public override IVaultState<FireplaceGuardianContext> OnUpdate(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnUpdate(machine, ctx);
			FireplaceGuardian.Hover(ctx, FireplaceGuardian.HoverHigh);

			if (!firedFireplaceGuardianBolt && Timer >= FireplaceGuardian.BoltWindup) {
				firedFireplaceGuardianBolt = true;
				FireplaceGuardian.SpawnBolts(ctx);
			}

			return Timer >= FireplaceGuardian.BoltWindup + 50 ? new FireplaceGuardianIdleState() : null;
		}
	}

	[VaultState(4, typeof(FireplaceGuardianContext))]
	public class FireplaceGuardianSlamState : VaultState<FireplaceGuardianContext>
	{
		/// <summary>本招是否已经触发过（用 >= 判断，避免掉帧直接跨过阈值）。</summary>
		private bool firedFireplaceGuardianSlam;
		public override string StateName => "炉心冲撞";

		public override void OnEnter(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			firedFireplaceGuardianSlam = false;
			base.OnEnter(machine, ctx);
			ctx.Npc.velocity *= 0.2f;
		}

		public override IVaultState<FireplaceGuardianContext> OnUpdate(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnUpdate(machine, ctx);

			if (Timer < FireplaceGuardian.SlamWindup) {
				FireplaceGuardian.Hover(ctx, FireplaceGuardian.HoverDefault);
				return null;
			}

			if (!firedFireplaceGuardianSlam && Timer >= FireplaceGuardian.SlamWindup) {
				firedFireplaceGuardianSlam = true;
				ctx.Npc.damage = FireplaceGuardian.SlamDamage;
				ctx.Npc.defDamage = FireplaceGuardian.SlamDamage;
				FireplaceGuardian.DashTowards(ctx);

				if (!Terraria.Main.dedServ) {
					SoundEngine.PlaySound(SoundID.Item14, ctx.Npc.Center);
				}
			}

			FireplaceGuardian.SteerSlam(ctx);
			return Timer >= FireplaceGuardian.SlamWindup + FireplaceGuardian.SlamTicks
				? new FireplaceGuardianIdleState()
				: null;
		}
	}

	[VaultState(5, typeof(FireplaceGuardianContext))]
	public class FireplaceGuardianRingState : VaultState<FireplaceGuardianContext>
	{
		/// <summary>本招是否已经触发过（用 >= 判断，避免掉帧直接跨过阈值）。</summary>
		private bool firedFireplaceGuardianRing;
		public override string StateName => "冷光环";

		public override void OnEnter(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			// 这一招的触发标记必须每次进入复位（`fired*` 用 >= 判断，见上面的字段注释）
			firedFireplaceGuardianRing = false;
			base.OnEnter(machine, ctx);
		}

		public override IVaultState<FireplaceGuardianContext> OnUpdate(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnUpdate(machine, ctx);
			FireplaceGuardian.Hover(ctx, FireplaceGuardian.HoverHigh);

			if (!firedFireplaceGuardianRing && Timer >= FireplaceGuardian.RingWindup) {
				firedFireplaceGuardianRing = true;
				FireplaceGuardian.SpawnRing(ctx);
				if (!Terraria.Main.dedServ) {
					SoundEngine.PlaySound(SoundID.Item28, ctx.Npc.Center);
				}
			}

			return Timer >= FireplaceGuardian.RingWindup + 16 ? new FireplaceGuardianIdleState() : null;
		}
	}

	[VaultState(6, typeof(FireplaceGuardianContext))]
	public class FireplaceGuardianWallState : VaultState<FireplaceGuardianContext>
	{
		/// <summary>本招是否已经触发过（用 >= 判断，避免掉帧直接跨过阈值）。</summary>
		private bool firedFireplaceGuardianWall;
		public override string StateName => "合拢火墙";

		public override void OnEnter(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			// 同上：每次进入复位触发标记
			firedFireplaceGuardianWall = false;
			base.OnEnter(machine, ctx);
		}

		public override IVaultState<FireplaceGuardianContext> OnUpdate(VaultStateMachine<FireplaceGuardianContext> machine, FireplaceGuardianContext ctx)
		{
			base.OnUpdate(machine, ctx);
			FireplaceGuardian.Hover(ctx, FireplaceGuardian.HoverHigh);

			if (!firedFireplaceGuardianWall && Timer >= FireplaceGuardian.WallWindup) {
				firedFireplaceGuardianWall = true;
				FireplaceGuardian.SpawnWalls(ctx);

				if (!Terraria.Main.dedServ) {
					SoundEngine.PlaySound(SoundID.Item74, ctx.Npc.Center);
				}
			}

			return Timer >= FireplaceGuardian.WallWindup + 90 ? new FireplaceGuardianIdleState() : null;
		}
	}
}
