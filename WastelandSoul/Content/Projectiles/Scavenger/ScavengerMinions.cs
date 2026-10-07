using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.Scavenger
{
	// ====================================================================================
	// 召唤师专属仆从（Boss1 清道夫）：
	//   · 仆从弹幕 + **专属 Buff** 必须成对——用原版 Buff 配自定义仆从会秒消。
	//   · 武器侧必须同时设置 Item.shoot 与 Item.buffType（见 ScavengerSummonerWeapon*.cs）。
	// 公共 AI 收在抽象基类里，两个仆从只写差异。
	// ====================================================================================

	/// <summary>仆从公共逻辑：跟随主人、找目标、按间隔发射弹幕、靠专属 Buff 维持存在。</summary>
	public abstract class WastelandMinionBase : ModProjectile
	{
		/// <summary>维持本仆从的专属 Buff 类型。</summary>
		protected abstract int BuffType { get; }

		/// <summary>仆从发射的弹幕类型。</summary>
		protected abstract int ShotType { get; }

		/// <summary>射击间隔（帧）。</summary>
		protected virtual float AttackInterval => 90f;

		/// <summary>搜索目标的半径。</summary>
		protected virtual float Range => 620f;

		/// <summary>跟随时的漂浮高度（负值在上方）。</summary>
		protected virtual float HoverHeight => 52f;

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 1;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.CultistIsResistantTo[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 2;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
		}

		public override bool? CanCutTiles() => false;

		public override bool MinionContactDamage() => true;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];

			// 主人没了 / 专属 Buff 掉了 → 自己退场（这是仆从"不秒消"的关键）
			if (!owner.active || owner.dead || !owner.HasBuff(BuffType)) {
				Projectile.Kill();
				return;
			}

			Projectile.timeLeft = 2;

			// 跟随：漂在主人身侧（多只仆从会错开）
			Vector2 idle = owner.Center + new Vector2(
				-46f * owner.direction * (1f + Projectile.minionPos * 0.7f),
				-HoverHeight);

			Vector2 toIdle = idle - Projectile.Center;
			float speed = toIdle.Length() > 400f ? 16f : 8f;

			if (toIdle.LengthSquared() > 64f) {
				Projectile.velocity = (Projectile.velocity * 12f + Vector2.Normalize(toIdle) * speed) / 13f;
			} else {
				Projectile.velocity *= 0.9f;
			}

			Projectile.rotation = Projectile.velocity.X * 0.05f;

			// 攻击
			Projectile.ai[0] += 1f;

			if (Projectile.ai[0] < AttackInterval) {
				return;
			}

			NPC target = FindTarget();

			if (target == null) {
				return;
			}

			Projectile.ai[0] = 0f;

			if (Projectile.owner == Main.myPlayer) {
				Vector2 direction = Vector2.Normalize(target.Center - Projectile.Center) * 9f;

				Projectile.NewProjectile(
					Projectile.GetSource_FromAI(),
					Projectile.Center,
					direction,
					ShotType,
					Projectile.damage / 2,
					1f,
					Projectile.owner);
			}
		}

		private NPC FindTarget()
		{
			NPC result = null;
			float best = Range;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.immortal) {
					continue;
				}

				float distance = Vector2.Distance(npc.Center, Projectile.Center);

				if (distance < best) {
					best = distance;
					result = npc;
				}
			}

			return result;
		}
	}

	/// <summary>A 线 · 废料无人机：接触伤害 + 每 1.5 秒一发废料弹。</summary>
	public class ScavengerDroneMinion : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<ScavengerDroneBuff>();

		protected override int ShotType => ModContent.ProjectileType<ScavengerScrapShard>();
	}

	/// <summary>A 线仆从的维持 Buff。</summary>
	public class ScavengerDroneBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<ScavengerDroneMinion>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			} else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>B 线专属 · 强化无人机：射得更勤、弹幕更强。</summary>
	public class ScavengerDroneMinionEX : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<ScavengerDroneBuffEX>();

		protected override int ShotType => ModContent.ProjectileType<ScavengerScrapShardEX>();

		protected override float AttackInterval => 60f;

		protected override float Range => 720f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 32;
			Projectile.height = 32;
		}
	}

	/// <summary>B 线仆从的维持 Buff。</summary>
	public class ScavengerDroneBuffEX : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<ScavengerDroneMinionEX>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			} else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
