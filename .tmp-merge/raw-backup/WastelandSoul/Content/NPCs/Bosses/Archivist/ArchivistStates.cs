using InnoVault.StateMachines;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;

namespace WastelandSoul.Content.NPCs.Bosses.Archivist
{
	// ====================================================================================
	// 归档者的状态机（InnoVault）。状态 ID 连续编号，不要跳号。
	// 槽位：ai[0] = 阶段序号、ai[2] = 出招轮换计数、ai[3] = 索引光束朝向；ai[1] 由框架占用。
	//
	// 三阶段（血量 100% / 65% / 30%）：
	//   一「检索」：索引光束（先横扫再锁定）+ 档案页（弹墙 2 次 / 命中困惑）
	//   二「归档」：弹幕墙（留缝隙）+ 归档封印（定住玩家后高伤突刺）
	//   三「覆写」：轨迹回放（沿玩家刚才走过的路径）+ 归档封印
	//   终盘（≤15%）：终盘压制——高频弹幕墙
	// ====================================================================================

	/// <summary>检索待机：中距离悬浮，倒计时结束后按阶段挑下一招（见 <see cref="Archivist.ChooseNextState"/>）。</summary>
	[VaultState(0, typeof(ArchivistContext))]
	public class ArchivistIdleState : VaultState<ArchivistContext>
	{
		public override string StateName => "检索待机";

		public override void OnEnter(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnEnter(machine, ctx);
			ctx.AttackDelay = Archivist.RollAttackDelay(ctx);
			Archivist.SetContactDamage(ctx.Npc, Archivist.ContactDamage);
		}

		public override IVaultState<ArchivistContext> OnUpdate(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnUpdate(machine, ctx);

			Archivist.Hover(ctx, Archivist.HoverDefault);

			return Timer >= ctx.AttackDelay ? Archivist.ChooseNextState(ctx) : null;
		}
	}

	/// <summary>阶段二「归档」演出：吼叫 + 提示，然后回到待机。</summary>
	[VaultState(1, typeof(ArchivistContext))]
	public class ArchivistPhaseTwoState : VaultState<ArchivistContext>
	{
		public override string StateName => "阶段二·归档";

		public override void OnEnter(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnEnter(machine, ctx);
			Archivist.OnPhaseStart(ctx.Npc, 1);
		}

		public override IVaultState<ArchivistContext> OnUpdate(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnUpdate(machine, ctx);
			ctx.Npc.velocity *= 0.92f;

			return Timer >= 45 ? new ArchivistIdleState() : null;
		}
	}

	/// <summary>阶段三「覆写」演出：提示「你的路径已被归档」。</summary>
	[VaultState(2, typeof(ArchivistContext))]
	public class ArchivistPhaseThreeState : VaultState<ArchivistContext>
	{
		public override string StateName => "阶段三·覆写";

		public override void OnEnter(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnEnter(machine, ctx);

			// 提示文本由 OnPhaseStart → Messages.ArchivistPhaseThree 发出（本地化走文件，
			// 主模组里不许有硬编码中文，否则汉化补丁机制会被绕过）。
			Archivist.OnPhaseStart(ctx.Npc, 2);
		}

		public override IVaultState<ArchivistContext> OnUpdate(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnUpdate(machine, ctx);
			ctx.Npc.velocity *= 0.92f;

			return Timer >= 50 ? new ArchivistIdleState() : null;
		}
	}

	/// <summary>
	/// 索引光束：前摇时眼部亮光预警（并且慢速跟踪），开火后先横扫、再锁定。
	/// <para/>光束本体是一个跟着本体走的独立弹幕：这里只负责维护朝向（<c>ai[3]</c>）。
	/// </summary>
	[VaultState(3, typeof(ArchivistContext))]
	public class ArchivistIndexBeamState : VaultState<ArchivistContext>
	{
		private int duration;

		public override string StateName => "索引光束";

		public override void OnEnter(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnEnter(machine, ctx);
			Archivist.PlayCastSound(ctx.Npc);
			ctx.Npc.velocity *= 0.35f;

			// 起手对齐一次，之后 ai[3] 就是这道光束的朝向
			ctx.Npc.ai[3] = 0f;
			duration = Archivist.BeamWindup + Archivist.BeamSweepTicks + Archivist.BeamLockTicks;
			ctx.Npc.netUpdate = true;

			Archivist.SpawnIndexBeam(ctx);
		}

		public override IVaultState<ArchivistContext> OnUpdate(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnUpdate(machine, ctx);

			// 悬浮 / 朝向维护都交给本体；光束弹幕每帧自己读 ai[3]
			// ⚠️ ai[3] 的推进**只能发生在这里**——弹幕那边只读。两边都推会让横扫速度翻倍。
			Archivist.StepBeamAngle(ctx, Timer);
			Archivist.Hover(ctx, Archivist.HoverHigh);

			return Timer >= duration ? new ArchivistIdleState() : null;
		}
	}

	/// <summary>档案页：前摇后呈扇形甩出 3~5 张可弹墙的索引页（阶段三更多）。</summary>
	[VaultState(4, typeof(ArchivistContext))]
	public class ArchivistPageVolleyState : VaultState<ArchivistContext>
	{
		public override string StateName => "档案页";

		public override void OnEnter(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnEnter(machine, ctx);
			Archivist.PlayCastSound(ctx.Npc);
			ctx.Npc.velocity *= 0.4f;
		}

		public override IVaultState<ArchivistContext> OnUpdate(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnUpdate(machine, ctx);

			Archivist.Hover(ctx, Archivist.HoverDefault);

			if (Timer < Archivist.PageWindup) {
				return null;
			}

			// 阶段越高，同一轮甩出的页越多
			int count = (int)ctx.Npc.ai[0] >= 2 ? 5 : 3;
			Archivist.ThrowIndexPages(ctx, count, 9f);

			return new ArchivistIdleState();
		}
	}

	/// <summary>
	/// 弹幕墙：沿一侧排开一整列纸页向玩家推进，**每列留两个可躲的缝隙**。
	/// <para/>墙生成后就不再改变方向——缝隙位置固定，玩家看得到也躲得掉（不是无解弹幕）。
	/// </summary>
	[VaultState(5, typeof(ArchivistContext))]
	public class ArchivistBarrageWallState : VaultState<ArchivistContext>
	{
		public override string StateName => "弹幕墙";

		public override void OnEnter(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnEnter(machine, ctx);
			Archivist.PlayCastSound(ctx.Npc);
			ctx.Npc.velocity *= 0.3f;
		}

		public override IVaultState<ArchivistContext> OnUpdate(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnUpdate(machine, ctx);

			Archivist.Hover(ctx, Archivist.HoverHigh);

			if (Timer < Archivist.WallWindup) {
				return null;
			}

			if (Timer == Archivist.WallWindup) {
				// 阶段一用不到这招；阶段二 9 列、阶段三 11 列（更高更密，但缝隙规则不变）
				int rows = (int)ctx.Npc.ai[0] >= 2 ? 11 : 9;
				Archivist.SpawnBarrageWall(ctx, rows, Archivist.BarrageDamage);

				if (!Main.dedServ) {
					SoundEngine.PlaySound(SoundID.Item24, ctx.Npc.Center);
				}
			}

			return Timer >= Archivist.WallWindup + Archivist.WallAdvanceTicks ? new ArchivistIdleState() : null;
		}
	}

	/// <summary>
	/// 归档封印 + 蓄力突刺：先在玩家脚下落封印（0.5 秒预警 → 定住），
	/// 封印期间本体蓄力，封印结束时甩出一次高伤突刺。
	/// <para/>突刺伤害 55，是这一招真正要躲的东西——定身只是压缩玩家的反应窗口。
	/// </summary>
	[VaultState(6, typeof(ArchivistContext))]
	public class ArchivistSealLungeState : VaultState<ArchivistContext>
	{
		public override string StateName => "归档封印";

		public override void OnEnter(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnEnter(machine, ctx);
			Archivist.PlayCastSound(ctx.Npc);
			ctx.Npc.velocity *= 0.2f;

			if (!Main.dedServ) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.ArchivistSealed"), new Color(176, 190, 220));
			}

			Archivist.PlaceSeal(ctx);
		}

		public override IVaultState<ArchivistContext> OnUpdate(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnUpdate(machine, ctx);

			if (Timer < Archivist.SealWindup) {
				Archivist.Hover(ctx, 240f);
				return null;
			}

			if (Timer == Archivist.SealWindup) {
				Archivist.PlaySealSound(ctx.Npc);
				Archivist.SetContactDamage(ctx.Npc, Archivist.LungeDamage);
				Archivist.DashTowards(ctx, Archivist.LungeSpeed);
			}

			Archivist.SteerLunge(ctx);

			if (Timer >= Archivist.SealWindup + Archivist.LungeTicks) {
				return new ArchivistIdleState();
			}

			return null;
		}

		public override void OnExit(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnExit(machine, ctx);
			Archivist.SetContactDamage(ctx.Npc, Archivist.ContactDamage);
			ctx.Npc.velocity *= 0.4f;
		}
	}

	/// <summary>
	/// 轨迹回放（阶段三「覆写」的招牌）：
	/// 先静默 1 秒报警（<see cref="Archivist.OverwriteTelegraph"/>），
	/// 然后按「最近 → 更早」的顺序，沿玩家刚才走过的路径逐点落下纸页——「你的路径已被归档」。
	/// </summary>
	[VaultState(7, typeof(ArchivistContext))]
	public class ArchivistOverwriteState : VaultState<ArchivistContext>
	{
		private int nextIndex;
		private bool firing;

		public override string StateName => "轨迹覆写";

		public override void OnEnter(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnEnter(machine, ctx);
			Archivist.PlayCastSound(ctx.Npc);
			ctx.Npc.velocity *= 0.3f;

			firing = false;
			nextIndex = ctx.TargetTrail.Count - 1;   // 从最新记录点开始往回放
		}

		public override IVaultState<ArchivistContext> OnUpdate(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnUpdate(machine, ctx);

			Archivist.Hover(ctx, Archivist.HoverHigh);

			if (!firing) {
				// 1 秒预警：让玩家看见"轨迹正在被高亮"，然后选择立刻变向
				if (Timer < Archivist.OverwriteTelegraph) {
					return null;
				}

				firing = true;
			}

			float sinceStart = Timer - Archivist.OverwriteTelegraph;

			if (sinceStart % Archivist.OverwriteFireInterval != 0f) {
				return null;
			}

			if (Timer > Archivist.OverwriteTelegraph) {
				Archivist.ReplayTrailPoint(ctx, nextIndex);
				nextIndex -= 3;   // 每次回退 3 个采样点（约 6 帧的位移），轨迹会被完整铺满
			}

			if (nextIndex < 0) {
				return new ArchivistIdleState();
			}

			return null;
		}
	}

	/// <summary>
	/// 终盘压制：血低于 15% 后不再留手，连续推高密度弹幕墙（间隔更短、列数更多）。
	/// <para/>仍然遵守"留缝隙"的规则——它变得更快，但没有变成无解。
	/// </summary>
	[VaultState(8, typeof(ArchivistContext))]
	public class ArchivistSuppressionState : VaultState<ArchivistContext>
	{
		public override string StateName => "终盘压制";

		public override void OnEnter(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnEnter(machine, ctx);
			Archivist.PlayCastSound(ctx.Npc);
			ctx.Npc.velocity *= 0.25f;

			// 只在第一次进入终盘时报一次（这招会被反复进入，不设标志会刷屏）
			if (!Main.dedServ && !ctx.SuppressionAnnounced) {
				ctx.SuppressionAnnounced = true;
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.ArchivistSuppression"), new Color(150, 176, 226));
			}
		}

		public override IVaultState<ArchivistContext> OnUpdate(VaultStateMachine<ArchivistContext> machine, ArchivistContext ctx)
		{
			base.OnUpdate(machine, ctx);

			Archivist.Hover(ctx, 300f);

			if (Timer < 24) {
				return null;
			}

			if (Timer == 24) {
				Archivist.SpawnBarrageWall(ctx, 11, Archivist.SuppressionDamage);

				// 顺带补一手扇形页，封住缝隙两侧
				Archivist.ThrowIndexPages(ctx, 3, 10f);

				if (!Main.dedServ) {
					SoundEngine.PlaySound(SoundID.Item24, ctx.Npc.Center);
				}
			}

			return Timer >= 24 + 72 ? new ArchivistIdleState() : null;
		}
	}
}
