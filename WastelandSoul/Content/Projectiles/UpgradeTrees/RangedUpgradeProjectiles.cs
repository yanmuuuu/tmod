using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.Effects;

namespace WastelandSoul.Content.Projectiles.UpgradeTrees
{
	// ====================================================================================
	// 升级衍生树 · 远程弹幕
	//   SteelBuckshot    精钢霰弹（弹速更快、穿透 2）
	//   EmberFlechette   燃烬箭弹（穿透 3、命中点燃 4 秒）
	// 表现只写在自己身上：少量同色尘粒，不做挂在所有弹幕上的线状拖尾。
	// ====================================================================================

	/// <summary>精钢霰弹：笔直、下坠很小、穿透 2 个敌人（<c>extraUpdates = 1</c> 让弹速翻倍）。</summary>
	public class SteelBuckshot : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 60;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.35f;
			Projectile.extraUpdates = 1;
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
			Projectile.velocity.Y += 0.006f;

			if (Main.rand.NextBool(4)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Silver);
				dust.noGravity = true;
				dust.scale = 0.6f;
				dust.velocity *= 0.2f;
			}
		}
	}

	/// <summary>燃烬箭弹：高速、穿透 3，命中点燃 4 秒（火种留在伤口里）。</summary>
	public class EmberFlechette : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.5f;
			Projectile.extraUpdates = 1;
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
			Projectile.velocity.Y += 0.004f;

			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch);
				dust.noGravity = true;
				dust.scale = 0.8f;
				dust.velocity *= 0.25f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire3, 240);
		}
	}
}
