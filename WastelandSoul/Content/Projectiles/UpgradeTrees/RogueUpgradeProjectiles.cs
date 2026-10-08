using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Projectiles.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树（七）· 盗贼（投掷）弹幕
	//   RustedGearBoomerang          单段回旋（去程 30 帧 → 回手，全程穿透）
	//   SalvagedSteelChakramProj     回旋 + 命中爆开一圈锯齿碎刃
	//   ChakramShatterBlast          锯齿爆裂判定（44x44，短命，每个敌人只吃一次）
	//   AshHeartCinderRingProj       三段相位：去程 → 悬停切割（点燃/削防）→ 回手
	//   CinderRingEmber              命中溅出的追踪余烬（点燃 + 霜冻）
	//
	// 表现只写在各弹幕自己的 AI 里：少量同色尘粒，**不做**挂在所有弹幕上的线状拖尾。
	// ====================================================================================

	/// <summary>
	/// 单段回旋投掷物的公共逻辑：飞出去 <see cref="OutTime"/> 帧后转回玩家手上，
	/// 全程穿透（靠本地无敌帧限制同一敌人的命中频率）——所以单件也能一直丢。
	/// </summary>
	public class RustedGearBoomerang : ModProjectile
	{
		/// <summary>去程持续帧数。</summary>
		protected virtual float OutTime => 30f;

		/// <summary>回程的转向力度（越大回得越急）。</summary>
		protected virtual float ReturnLerp => 0.16f;

		/// <summary>回程速度。</summary>
		protected virtual float ReturnSpeed => 15f;

		/// <summary>自转速度。</summary>
		protected virtual float SpinRate => 0.45f;

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 600;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 14;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Player player = Main.player[Projectile.owner];

			if (!player.active || player.dead) {
				Projectile.Kill();
				return;
			}

			Projectile.rotation += SpinRate * Projectile.direction;

			Flight(player);

			if (Main.rand.NextBool(6)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.velocity *= 0.2f;
			}
		}

		/// <summary>去程 / 回程两段；三段相位的那一件会整个重写它。</summary>
		protected virtual void Flight(Player player)
		{
			if (Projectile.ai[0] == 0f) {
				// 去程：略减速，时间到了切回程
				Projectile.ai[1] += 1f;
				Projectile.velocity *= 0.985f;

				if (Projectile.ai[1] > OutTime) {
					Projectile.ai[0] = 1f;
				}

				return;
			}

			ReturnToOwner(player);
		}

		/// <summary>回程：朝玩家拉过去，贴到 48 像素内就收手（本地判定，不掉落）。</summary>
		protected void ReturnToOwner(Player player)
		{
			Vector2 toPlayer = player.Center - Projectile.Center;

			if (toPlayer.Length() < 48f) {
				Projectile.Kill();
				return;
			}

			toPlayer.Normalize();
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, toPlayer * ReturnSpeed, ReturnLerp);
		}
	}

	/// <summary>精钢锯齿环：回旋更快、命中频率更密，撞到敌人时炸开一圈锯齿碎刃。</summary>
	public class SalvagedSteelChakramProj : RustedGearBoomerang
	{
		/// <summary>两次爆裂之间的最短间隔（帧），避免打群怪时弹幕爆炸。</summary>
		private const float BlastCooldown = 25f;

		protected override float OutTime => 32f;

		protected override float ReturnLerp => 0.18f;

		protected override float ReturnSpeed => 16f;

		protected override float SpinRate => 0.52f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.light = 0.2f;
			Projectile.localNPCHitCooldown = 12;
		}

		protected override void Flight(Player player)
		{
			base.Flight(player);

			if (Projectile.ai[2] > 0f) {
				Projectile.ai[2] -= 1f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[2] > 0f || Projectile.owner != Main.myPlayer) {
				return;
			}

			Projectile.ai[2] = BlastCooldown;

			Projectile.NewProjectile(
				Projectile.GetSource_FromThis(),
				target.Center,
				Vector2.Zero,
				ModContent.ProjectileType<ChakramShatterBlast>(),
				(int)(Projectile.damage * 0.5f),
				Projectile.knockBack,
				Projectile.owner);
		}
	}

	/// <summary>锯齿爆裂：一个 44x44 的短命判定框，每个敌人只吃一次伤害（不会伤到自己）。</summary>
	public class ChakramShatterBlast : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 44;
			Projectile.height = 44;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 3;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
			Projectile.aiStyle = 0;
			Projectile.alpha = 40;
		}

		public override void AI()
		{
			Projectile.alpha += 70;

			if (Projectile.alpha > 255) {
				Projectile.alpha = 255;
			}

			for (int i = 0; i < 2; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Silver);
				dust.noGravity = true;
				dust.scale = 0.9f;
				dust.velocity *= 1.6f;
			}
		}
	}

	/// <summary>
	/// 灰烬之心余烬环：三段相位。
	/// <list type="number">
	/// <item>去程 28 帧（慢下来）；</item>
	/// <item>在原地**悬停切割 45 帧**：这一段命中频率压到 8 帧、命中点燃 4 秒并削防 3 秒；</item>
	/// <item>回手。</item>
	/// </list>
	/// 每次命中还会溅出 1 枚追踪余烬（上限 5 枚，用 <c>localAI[0]</c> 计数，只在主人这一侧累加）。
	/// </summary>
	public class AshHeartCinderRingProj : RustedGearBoomerang
	{
		private const float HoverTime = 45f;
		private const float MaxEmbers = 5f;

		/// <summary>去程帧数（重写基类的同名属性，三段相位里只用它切第一段）。</summary>
		protected override float OutTime => 28f;

		protected override float ReturnLerp => 0.20f;

		protected override float ReturnSpeed => 17f;

		protected override float SpinRate => 0.62f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.light = 0.45f;
			Projectile.localNPCHitCooldown = 8;
		}

		protected override void Flight(Player player)
		{
			if (Projectile.ai[0] == 0f) {
				// 一：去程
				Projectile.ai[1] += 1f;
				Projectile.velocity *= 0.985f;

				if (Projectile.ai[1] > OutTime) {
					Projectile.ai[0] = 1f;
					Projectile.ai[1] = 0f;
				}

				return;
			}

			if (Projectile.ai[0] == 1f) {
				// 二：悬停在落点上原地切割
				Projectile.ai[1] += 1f;
				Projectile.velocity *= 0.88f;

				if (Projectile.ai[1] > HoverTime) {
					Projectile.ai[0] = 2f;
				}

				if (Main.rand.NextBool(2)) {
					Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
					dust.noGravity = true;
					dust.scale = 0.9f;
					dust.velocity *= 0.2f;
				}

				return;
			}

			// 三：回手
			ReturnToOwner(player);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire3, 240);
			target.AddBuff(BuffID.Ichor, 180);

			if (Projectile.localAI[0] >= MaxEmbers || Projectile.owner != Main.myPlayer) {
				return;
			}

			Projectile.localAI[0] += 1f;

			Vector2 offset = new Vector2(Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-10f, 10f));
			Vector2 speed = Main.rand.NextVector2Circular(5f, 5f);

			Projectile.NewProjectile(
				Projectile.GetSource_FromThis(),
				target.Center + offset,
				speed,
				ModContent.ProjectileType<CinderRingEmber>(),
				(int)(Projectile.damage * 0.4f),
				Projectile.knockBack * 0.5f,
				Projectile.owner);
		}
	}

	/// <summary>余烬：朝 440 像素内最近的敌人缓慢转向，命中一次即消失（点燃 + 霜冻）。</summary>
	public class CinderRingEmber : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Throwing;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.4f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation += 0.4f * Projectile.direction;

			NPC target = GearTreeAim.NearestEnemy(Projectile.Center, 440f);

			if (target != null) {
				float speed = Projectile.velocity.Length();
				Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.09f);
			}

			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.velocity *= 0.2f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire3, 180);
			target.AddBuff(BuffID.Frostburn2, 120);
		}
	}
}
