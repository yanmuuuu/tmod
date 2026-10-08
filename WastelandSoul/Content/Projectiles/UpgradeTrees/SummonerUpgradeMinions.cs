using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;
using WastelandSoul.Common.Players;

namespace WastelandSoul.Content.Projectiles.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树（六）· 召唤仆从 + 专属 Buff + 仆从弹幕
	//
	// 纪律：仆从弹幕与维持 Buff **必须成对**（用原版 Buff 配自定义仆从会秒消）；
	//       武器侧同时设置 Item.shoot 与 Item.buffType。
	//
	// 三级的 AI 差异：
	//   RustedGearSentry       近战冲锋（Charger = true，冲上去咬）
	//   SalvagedSteelCharger   近战冲锋 + 出生时拉出 1 只射击同伴
	//   SalvagedSteelSniper    悬浮射击（每 60 帧一发钢钉，命中流血）
	//   AshHeartOrbitSentry    环绕主人转圈切割（接触点燃）+ 每 45 帧投一枚追踪余烬弹
	//
	// 表现只写在各弹幕自己的 AI 里：少量同色尘粒，**不做**挂在所有弹幕上的线状拖尾。
	// ====================================================================================

	/// <summary>本批齿轮仆从的公共逻辑：跟随/冲锋、索敌、按间隔射击、靠专属 Buff 维持存在。</summary>
	public abstract class GearMinionBase : ModProjectile
	{
		/// <summary>维持本仆从的专属 Buff 类型。</summary>
		protected abstract int BuffType { get; }

		/// <summary>仆从发射的弹幕类型；0 表示纯撞击型（不射击）。</summary>
		protected virtual int ShotType => 0;

		/// <summary>射击间隔（帧）。</summary>
		protected virtual float AttackInterval => 60f;

		/// <summary>索敌半径。</summary>
		protected virtual float Range => 640f;

		/// <summary>跟随停留高度（正值表示在主人上方多少像素）。</summary>
		protected virtual float HoverHeight => 58f;

		/// <summary>射出弹幕的速度。</summary>
		protected virtual float ShotSpeed => 10f;

		/// <summary>射出弹幕的伤害 = 仆从面板伤害 / 该值。</summary>
		protected virtual int ShotDamageDivisor => 2;

		/// <summary>是否主动冲向目标（近战冲锋型）。</summary>
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
			Projectile.width = 28;
			Projectile.height = 28;
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
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			Player owner = Main.player[Projectile.owner];

			// 主人没了 / 专属 Buff 掉了 → 自己退场（这是仆从「不秒消」的关键）
			if (!owner.active || owner.dead || !owner.HasBuff(BuffType)) {
				Projectile.Kill();
				return;
			}

			Projectile.timeLeft = 2;

			NPC target = FindTarget();

			Move(owner, target);

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

		/// <summary>默认行为：冲锋型追目标，其余悬浮跟随；环绕型重写整套。</summary>
		protected virtual void Move(Player owner, NPC target)
		{
			if (Charger && target != null) {
				Vector2 toTarget = target.Center - Projectile.Center;
				float chase = toTarget.Length() > 160f ? 13f : 9f;

				if (toTarget.LengthSquared() > 16f) {
					Projectile.velocity = (Projectile.velocity * 8f + Vector2.Normalize(toTarget) * chase) / 9f;
				}

				return;
			}

			Vector2 idle = owner.Center + new Vector2(
				-46f * owner.direction * (1f + SiblingIndex() * 0.8f),
				-HoverHeight);

			Vector2 toIdle = idle - Projectile.Center;
			float speed = toIdle.Length() > 420f ? 16f : 8f;

			if (toIdle.LengthSquared() > 64f) {
				Projectile.velocity = (Projectile.velocity * 12f + Vector2.Normalize(toIdle) * speed) / 13f;
			}
			else {
				Projectile.velocity *= 0.9f;
			}
		}

		/// <summary>默认单发；需要散射/追踪的仆从重写它。</summary>
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

		/// <summary>
		/// 自己这只仆从是同类里的第几只（0 起）。
		/// <para/>不用 <c>Projectile.minionPos</c>：那个字段是给原版仆从自己的 AI 填的，
		/// 这里自己数「先于我生成的同类」更稳，也不依赖别人的字段语义。
		/// </summary>
		protected int SiblingIndex()
		{
			int index = 0;

			for (int i = 0; i < Projectile.whoAmI; i++) {
				var sibling = Main.projectile[i];

				if (sibling.active && sibling.owner == Projectile.owner && sibling.type == Projectile.type) {
					index++;
				}
			}

			return index;
		}

		/// <summary>返回范围内最近的敌对 NPC；找不到返回 null。</summary>
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

	/// <summary>树根仆从：近战冲锋齿轮，不射击，撞上去咬。</summary>
	public class RustedGearSentry : GearMinionBase
	{
		protected override int BuffType => ModContent.BuffType<RustedGearSentryBuff>();

		protected override bool Charger => true;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 28;
			Projectile.height = 28;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			base.AI();

			if (Main.rand.NextBool(7)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}
	}

	/// <summary>锈蚀齿轮哨兵的维持 Buff。</summary>
	public class RustedGearSentryBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<RustedGearSentry>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>
	/// 精钢冲锋齿轮：AI 与树根一致（撞击型），但出生时会**顺手拉出**一只射击同伴，
	/// 于是「吹一次哨」= 一冲一射两只（各占 1 个仆从位）。
	/// </summary>
	public class SalvagedSteelCharger : GearMinionBase
	{
		protected override int BuffType => ModContent.BuffType<SalvagedSteelGearBuff>();

		protected override bool Charger => true;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 30;
			Projectile.height = 30;
		}

		public override void OnSpawn(IEntitySource source)
		{
			// 只由主人这一侧生成同伴，避免联机时两边各刷一只
			if (Projectile.owner != Main.myPlayer) {
				return;
			}

			Projectile.NewProjectile(
				Projectile.GetSource_FromAI(),
				Projectile.Center,
				Vector2.Zero,
				ModContent.ProjectileType<SalvagedSteelSniper>(),
				Projectile.damage,
				Projectile.knockBack,
				Projectile.owner);
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			base.AI();

			if (Main.rand.NextBool(7)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Silver);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}
	}

	/// <summary>精钢射击齿轮：悬浮跟随，每 60 帧朝最近敌人吐一发钢钉。</summary>
	public class SalvagedSteelSniper : GearMinionBase
	{
		protected override int BuffType => ModContent.BuffType<SalvagedSteelGearBuff>();

		protected override int ShotType => ModContent.ProjectileType<SalvagedSteelBolt>();

		protected override float AttackInterval => 60f;

		protected override float HoverHeight => 66f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 28;
			Projectile.height = 28;
		}
	}

	/// <summary>精钢齿轮哨（双联）的维持 Buff：冲锋与射击两只共用。</summary>
	public class SalvagedSteelGearBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			bool alive = player.ownedProjectileCounts[ModContent.ProjectileType<SalvagedSteelCharger>()] > 0
				|| player.ownedProjectileCounts[ModContent.ProjectileType<SalvagedSteelSniper>()] > 0;

			if (alive) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>精钢钢钉：直飞、穿透 1，命中让目标流血 3 秒。</summary>
	public class SalvagedSteelBolt : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 80;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.35f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Silver);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Bleeding, 180);
		}
	}

	/// <summary>
	/// 灰烬之心环绕齿灵：不再跟随在身侧，而是**绕着主人转圈**（同类各占半圈），
	/// 碰到敌人点燃；每 45 帧投出一枚会追踪的余烬弹。
	/// </summary>
	public class AshHeartOrbitSentry : GearMinionBase
	{
		/// <summary>环绕半径。</summary>
		private const float OrbitRadius = 96f;

		protected override int BuffType => ModContent.BuffType<AshHeartGearBuff>();

		protected override int ShotType => ModContent.ProjectileType<AshHeartGearEmber>();

		protected override float AttackInterval => 45f;

		protected override float Range => 700f;

		protected override float ShotSpeed => 8f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 32;
			Projectile.height = 32;
			Projectile.light = 0.3f;
		}

		/// <summary>
		/// 出生时再拉一只同伴，围着主人凑成对位的一圈。
		/// <para/>同伴用 <c>ai[1] = 1</c> 打标记（这个字段本类 AI 完全不用），
		/// 靠标记而不是"数同类的 active 状态"来防止自我复制 —— 出生回调里
		/// 兄弟弹幕的 <c>active</c> 还没写稳，数它会递归刷出一串。
		/// </summary>
		public override void OnSpawn(IEntitySource source)
		{
			if (Projectile.owner != Main.myPlayer || Projectile.ai[1] == 1f) {
				return;
			}

			Projectile.NewProjectile(
				Projectile.GetSource_FromAI(),
				Projectile.Center,
				Vector2.Zero,
				ModContent.ProjectileType<AshHeartOrbitSentry>(),
				Projectile.damage,
				Projectile.knockBack,
				Projectile.owner,
				0f,
				1f);
		}

		protected override void Move(Player owner, NPC target)
		{
			// 两只分处对角，整体缓慢公转
			float angle = MathHelper.TwoPi * (SiblingIndex() % 2) * 0.5f + Main.GameUpdateCount * 0.022f;
			Vector2 desired = owner.Center + angle.ToRotationVector2() * OrbitRadius;
			Vector2 toDesired = desired - Projectile.Center;

			if (toDesired.LengthSquared() > 4f) {
				float speed = MathHelper.Clamp(toDesired.Length() * 0.7f, 4f, 18f);
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.Normalize(toDesired) * speed, 0.28f);
			}
			else {
				Projectile.velocity *= 0.9f;
			}
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			base.AI();

			// 基类 AI 末尾会按速度重设 rotation，所以自转放在它后面覆盖
			Projectile.rotation += 0.38f * Projectile.direction;

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.velocity *= 0.2f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire3, 180);
		}
	}

	/// <summary>
	/// 灰烬之心齿灵哨的维持 Buff。除了维持仆从，还负责**齿轮护盾**：
	/// 防御 +5、免疫击退，并把「护盾生效」写进 <c>WastelandTreePlayer</c>，
	/// 由那个 ModPlayer 追加一条 8% 的减伤乘区。
	/// </summary>
	public class AshHeartGearBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<AshHeartOrbitSentry>()] <= 0) {
				player.DelBuff(buffIndex);
				buffIndex--;
				return;
			}

			player.buffTime[buffIndex] = 18000;

			player.statDefense += 5;
			player.noKnockback = true;
			player.GetModPlayer<WastelandTreePlayer>().gearShield = true;
		}
	}

	/// <summary>余烬弹：出膛后朝 460 像素内最近的敌人缓慢修正方向，命中点燃 + 霜冻。</summary>
	public class AshHeartGearEmber : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.45f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(1, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation += 0.3f * Projectile.direction;

			NPC target = GearTreeAim.NearestEnemy(Projectile.Center, 460f);

			if (target != null) {
				float speed = Projectile.velocity.Length();
				Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.10f);
			}

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 0.8f;
				dust.velocity *= 0.2f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire3, 240);
			target.AddBuff(BuffID.Frostburn2, 180);
		}
	}

	/// <summary>本批齿轮系弹幕共用的目标查找（**不是** ModProjectile，不需要贴图）。</summary>
	internal static class GearTreeAim
	{
		/// <summary>返回范围内最近的敌对 NPC；找不到返回 null。</summary>
		public static NPC NearestEnemy(Vector2 from, float maxRange)
		{
			NPC best = null;
			float bestDistance = maxRange * maxRange;

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (!npc.active || npc.friendly || npc.immortal || npc.dontTakeDamage || npc.life <= 0) {
					continue;
				}

				float distance = Vector2.DistanceSquared(npc.Center, from);

				if (distance < bestDistance) {
					bestDistance = distance;
					best = npc;
				}
			}

			return best;
		}
	}
}
