using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.Rust
{
	// ====================================================================================
	// 「锈蚀」线 · 召唤仆从 + 专属 Buff + 仆从弹幕
	//
	// 纪律：仆从弹幕与维持 Buff **必须成对**（用原版 Buff 配自定义仆从会秒消）；
	//       武器侧同时设置 Item.shoot 与 Item.buffType。
	// 手感差异：
	//   RustDrone      撞击型（Charger=true，冲上去撞）
	//   RustSpitter    射击型（每 70 帧一发）
	//   RustSpitterEX  射击型强化（每 55 帧两发扇形、穿透 2）
	// ====================================================================================

	/// <summary>本包仆从的公共逻辑：跟随主人、找目标、按间隔射击、靠专属 Buff 维持存在。</summary>
	public abstract class PackMinionBase : ModProjectile
	{
		/// <summary>维持本仆从的专属 Buff 类型。</summary>
		protected abstract int BuffType { get; }

		/// <summary>仆从发射的弹幕类型；0 表示纯撞击型（不射击）。</summary>
		protected virtual int ShotType => 0;

		/// <summary>射击间隔（帧）。</summary>
		protected virtual float AttackInterval => 90f;

		/// <summary>搜索目标的半径。</summary>
		protected virtual float Range => 620f;

		/// <summary>跟随停留高度（像素，正值表示在主人上方多少）。</summary>
		protected virtual float HoverHeight => 56f;

		/// <summary>射出的弹幕速度。</summary>
		protected virtual float ShotSpeed => 9f;

		/// <summary>射出弹幕的伤害 = 仆从面板伤害 / 该值。</summary>
		protected virtual int ShotDamageDivisor => 2;

		/// <summary>是否像近战仆从一样主动冲向目标。</summary>
		protected virtual bool Charger => false;

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
			Projectile.DamageType = DamageClass.Summon;
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

			// 主人没了 / 专属 Buff 掉了 → 自己退场（这是仆从「不秒消」的关键）
			if (!owner.active || owner.dead || !owner.HasBuff(BuffType)) {
				Projectile.Kill();
				return;
			}

			Projectile.timeLeft = 2;

			NPC target = FindTarget();

			if (Charger && target != null) {
				Vector2 toTarget = target.Center - Projectile.Center;
				float chase = toTarget.Length() > 140f ? 13f : 9f;

				if (toTarget.LengthSquared() > 16f) {
					Projectile.velocity = (Projectile.velocity * 8f + Vector2.Normalize(toTarget) * chase) / 9f;
				}
			}
			else {
				Vector2 idle = owner.Center + new Vector2(
					-48f * owner.direction * (1f + Projectile.minionPos * 0.75f),
					-HoverHeight);

				Vector2 toIdle = idle - Projectile.Center;
				float speed = toIdle.Length() > 400f ? 16f : 8f;

				if (toIdle.LengthSquared() > 64f) {
					Projectile.velocity = (Projectile.velocity * 12f + Vector2.Normalize(toIdle) * speed) / 13f;
				}
				else {
					Projectile.velocity *= 0.9f;
				}
			}

			Projectile.rotation = Projectile.velocity.X * 0.05f;

			if (ShotType <= 0) {
				return;
			}

			Projectile.ai[0] += 1f;

			if (Projectile.ai[0] < AttackInterval || target == null) {
				return;
			}

			Projectile.ai[0] = 0f;

			if (Projectile.owner == Main.myPlayer) {
				ShootAt(target, owner);
			}
		}

		/// <summary>默认单发；强化仆从重写它做扇形散射。</summary>
		protected virtual void ShootAt(NPC target, Player owner)
		{
			Vector2 direction = Vector2.Normalize(target.Center - Projectile.Center) * ShotSpeed;

			Projectile.NewProjectile(
				Projectile.GetSource_FromAI(),
				Projectile.Center,
				direction,
				ShotType,
				Projectile.damage / ShotDamageDivisor,
				1f,
				Projectile.owner);
		}

		protected NPC FindTarget()
		{
			NPC result = null;
			float best = Range;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.immortal || npc.lifeMax <= 5) {
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

	/// <summary>撞击型仆从：不射击，直接冲上去撞（接触伤害）。</summary>
	public class RustDrone : PackMinionBase
	{
		protected override int BuffType => ModContent.BuffType<RustDroneBuff>();

		protected override bool Charger => true;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 28;
			Projectile.height = 28;
		}

		public override void AI()
		{
			base.AI();

			if (Main.rand.NextBool(7)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}
	}

	/// <summary>废料无人机的维持 Buff。</summary>
	public class RustDroneBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<RustDrone>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>射击型仆从：悬浮跟随，每 70 帧吐一发酸弹。</summary>
	public class RustSpitter : PackMinionBase
	{
		protected override int BuffType => ModContent.BuffType<RustSpitterBuff>();

		protected override int ShotType => ModContent.ProjectileType<RustSpitterShot>();

		protected override float AttackInterval => 70f;

		protected override float HoverHeight => 62f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 30;
			Projectile.height = 30;
		}
	}

	/// <summary>喷吐者的维持 Buff。</summary>
	public class RustSpitterBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<RustSpitter>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>喷吐者吐出的酸弹：直飞、命中附中毒。</summary>
	public class RustSpitterShot : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.35f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GreenMoss);
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.velocity *= 0.2f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Poisoned, 150);
		}
	}

	/// <summary>强化射击仆从：射得更勤、一次两发扇形、弹幕能穿透 2 个敌人。</summary>
	public class RustSpitterEX : PackMinionBase
	{
		protected override int BuffType => ModContent.BuffType<RustSpitterBuffEX>();

		protected override int ShotType => ModContent.ProjectileType<RustSpitterShotEX>();

		protected override float AttackInterval => 55f;

		protected override float Range => 700f;

		protected override float HoverHeight => 62f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 34;
			Projectile.height = 34;
		}

		protected override void ShootAt(NPC target, Player owner)
		{
			Vector2 direction = Vector2.Normalize(target.Center - Projectile.Center) * ShotSpeed;

			for (int i = -1; i <= 1; i += 2) {
				Vector2 spread = direction.RotatedBy(MathHelper.ToRadians(7f * i));

				Projectile.NewProjectile(
					Projectile.GetSource_FromAI(),
					Projectile.Center,
					spread,
					ShotType,
					Projectile.damage / ShotDamageDivisor,
					1f,
					Projectile.owner);
			}
		}
	}

	/// <summary>强化喷吐者的维持 Buff。</summary>
	public class RustSpitterBuffEX : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<RustSpitterEX>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>强化酸弹：穿透 2、命中附中毒、飞得稍快。</summary>
	public class RustSpitterShotEX : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 80;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.4f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GreenMoss);
				dust.noGravity = true;
				dust.scale = 0.8f;
				dust.velocity *= 0.2f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Poisoned, 180);
		}
	}
}
