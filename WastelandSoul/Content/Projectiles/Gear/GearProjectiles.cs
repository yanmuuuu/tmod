using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.Gear
{
	// ====================================================================================
	// 装备扩充包：饰品/防具触发的弹幕（只有两个，都是"被打之后"的表现 + 效果）。
	//   GearReboundShard  炉火镜面反弹的余烬碎片（友好，真伤害）
	//   GearColdPulse     炉火护盾碎盾时泄出的冷火环（友好，但不造成伤害，只上减速）
	// ====================================================================================

	/// <summary>
	/// 反弹的余烬碎片。<c>Projectile.ai[0]</c> 是配色：0 余烬（橙），1 冷火（青白）。
	/// <para/>伤害由创建方给出（炉卫镜面 = 受到伤害的 35%），这里只负责飞出去和打一下。
	/// </summary>
	public class GearReboundShard : ModProjectile
	{
		/// <summary>余烬配色（炉火镜面）。</summary>
		public const float PaletteEmber = 0f;

		/// <summary>冷火配色（留给以后复用）。</summary>
		public const float PaletteHearth = 1f;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Generic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.6f;
			Projectile.aiStyle = 0;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity *= 0.985f;
			Projectile.alpha = (int)MathHelper.Lerp(0f, 150f, 1f - Projectile.timeLeft / 90f);

			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
					Projectile.ai[0] >= 0.5f ? DustID.Silver : DustID.Torch);
				dust.noGravity = true;
				dust.scale = 0.85f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(Projectile.ai[0] >= 0.5f ? BuffID.Frostburn : BuffID.OnFire, 180);
		}
	}

	/// <summary>
	/// 炉火护盾碎盾时泄出的一圈冷火。**不造成伤害**，只把范围内的敌人挂上"霜冻"与"缓慢"，
	/// 伤害数值走 <c>ai[0]</c> 只是为了以后想做伤害时不用改结构（当前恒为 0 伤害）。
	/// </summary>
	public class GearColdPulse : ModProjectile
	{
		/// <summary>扩散半径。</summary>
		private const float MaxRadius = 190f;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Generic;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.light = 0.5f;
			Projectile.aiStyle = 0;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 40;
		}

		public override bool? CanDamage()
		{
			return false;      // 纯表现 + 减益，不给伤害
		}

		public override void AI()
		{
			Projectile.Center = Main.player[Projectile.owner].Center;
			Projectile.velocity = Vector2.Zero;

			if (Projectile.localAI[0] == 0f) {
				Projectile.localAI[0] = 1f;

				if (!Main.dedServ) {
					WastelandFxSystem.Ring(Projectile.Center, new Color(170, 222, 255), 12f, MaxRadius, 26);
					WastelandFxSystem.Burst(Projectile.Center, 10, new Color(196, 234, 255), 5f);
				}
			}

			float progress = 1f - Projectile.timeLeft / 40f;
			float radius = MathHelper.Lerp(24f, MaxRadius, progress);

			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];

				if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.immortal) {
					continue;
				}

				if (Vector2.Distance(npc.Center, Projectile.Center) > radius + npc.width * 0.5f) {
					continue;
				}

				npc.AddBuff(BuffID.Slow, 120);
				npc.AddBuff(BuffID.Frostburn, 180);
			}

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Silver);
				dust.noGravity = true;
				dust.scale = 1f;
				dust.velocity *= 0.3f;
			}
		}
	}
}
