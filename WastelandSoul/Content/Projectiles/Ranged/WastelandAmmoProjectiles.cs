using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.Ranged
{
	// ====================================================================================
	// 三种实弹（钉子 / 冷光 / 余烬）与一种法术弹药（残页）。
	// 全部走 aiStyle = -1（自己写 AI），**不挂全局拖尾**——
	// 每种弹幕只在自己的 AI 里画属于自己材质的那点碎屑，视觉差异靠颜色与命中特效区分。
	// ====================================================================================

	/// <summary>
	/// 废料钉：直线飞行的铁钉，命中时炸出一撮铁屑。
	/// <para/>视觉：极小、贴图细长、飞行时朝速度方向旋转；没有光。
	/// </summary>
	public class ScrapNailProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 300;
			Projectile.ignoreWater = false;
			Projectile.extraUpdates = 1;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			// 铁钉会掉：给一点点重力，远距离需要抬枪口
			Projectile.velocity.Y += 0.03f;

			if (Projectile.velocity.LengthSquared() > 0.01f) {
				Projectile.rotation = Projectile.velocity.ToRotation();
			}

			if (!Main.dedServ && Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Iron);
				dust.velocity = -Projectile.velocity * 0.08f;
				dust.noGravity = true;
				dust.scale = 0.6f;
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 6; i++) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Iron, 0f, 0f, 100, default, 0.8f);
			}
		}
	}

	/// <summary>
	/// 冷光弹：会发光的冷冻弹头，穿透 1 个敌人，命中附加霜冻。
	/// <para/>视觉：青白色光点 + 冷气尾迹，Projectile.light 让它在洞里也能照明。
	/// </summary>
	public class ColdlightProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 2;      // 打穿一个再停
			Projectile.tileCollide = true;
			Projectile.timeLeft = 280;
			Projectile.light = 0.6f;
			Projectile.extraUpdates = 1;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			if (Projectile.velocity.LengthSquared() > 0.01f) {
				Projectile.rotation = Projectile.velocity.ToRotation();
			}

			if (!Main.dedServ) {
				// 冷气尾迹
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch);
				dust.velocity = -Projectile.velocity * 0.15f;
				dust.noGravity = true;
				dust.scale = 0.9f;
				dust.alpha = 40;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Frostburn, 60 * 4);
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 10; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch);
				dust.velocity *= 1.4f;
				dust.noGravity = true;
				dust.scale = 1.1f;
			}

			SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f }, Projectile.Center);
		}
	}

	/// <summary>
	/// 余烬弹：命中后还在烧的重弹，穿透 2 个敌人，命中附加着火了。
	/// <para/>视觉：暖橙色火星 + 命中时一圈灰烬，弹体稍大、速度稍慢。
	/// </summary>
	public class EmberShellProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 3;      // 打穿两个再停
			Projectile.tileCollide = true;
			Projectile.timeLeft = 260;
			Projectile.light = 0.5f;
			Projectile.extraUpdates = 1;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.velocity *= 0.995f;   // 重弹头，速度会衰减

			if (Projectile.velocity.LengthSquared() > 0.01f) {
				Projectile.rotation = Projectile.velocity.ToRotation();
			}

			if (!Main.dedServ) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.velocity = -Projectile.velocity * 0.2f;
				dust.noGravity = true;
				dust.scale = 1.1f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 60 * 4);
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 12; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.velocity *= 1.6f;
				dust.noGravity = true;
				dust.scale = 1.2f;
			}

			for (int i = 0; i < 6; i++) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 120, default, 1f);
			}

			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f }, Projectile.Center);
		}
	}

	/// <summary>
	/// 残页：翻滚着飞出去的一页纸，穿透 4 个敌人，命中时飘出几粒"判词"碎光。
	/// <para/>视觉：方形纸页持续自转（不是朝速度方向对齐），速度慢但穿透高。
	/// </summary>
	public class TornPageProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 5;      // 打穿四个再停
			Projectile.tileCollide = false;  // 纸会飘过瓦砾
			Projectile.timeLeft = 240;
			Projectile.light = 0.3f;
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0f) {
				Projectile.localAI[1] = 1f;
				if (!Main.dedServ) {
					WastelandFxSystem.StyleStrike(0, Projectile.Center, Projectile.velocity);
				}
			}
			Projectile.rotation += 0.22f * (Projectile.velocity.X >= 0f ? 1f : -1f);
			Projectile.velocity *= 0.99f;

			if (!Main.dedServ && Main.rand.NextBool(5)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch);
				dust.velocity *= 0.3f;
				dust.noGravity = true;
				dust.scale = 0.7f;
				dust.color = new Color(226, 214, 180);
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 8; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch);
				dust.velocity *= 1.2f;
				dust.noGravity = true;
				dust.scale = 0.9f;
				dust.color = new Color(226, 214, 180);
			}
		}
	}
}
