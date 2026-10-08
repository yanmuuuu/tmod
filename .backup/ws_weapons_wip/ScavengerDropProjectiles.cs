using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Buffs;

namespace WastelandSoul.Content.Projectiles.Scavenger
{
	// ====================================================================================
	// 五把「清道夫掉落」武器的弹幕，**贴图一律复用原版**（玩家要求：不要自己画出图）。
	// 做法：ModProjectile 里 override Texture => null（告诉 tModLoader 没有自己的贴图），
	//       然后在 PreDraw 里用 TextureAssets.Projectile[原版ID] 自己画。
	// 注意：Projectile.width/height 必须与该原版贴图的宽高一致，否则 tModLoader 会报
	//       "texture dimensions do not match"（这五个数值是用 tools/read_vanilla_tex.py 读出来的）。
	// ====================================================================================

	/// <summary>
	/// 战士「清道夫大刀」的**月牙刀光**。
	/// <para/>外形复用原版 <see cref="ProjectileID.SwordBeam"/>（真断钢/断钢剑那道月牙刀光），
	/// 但手感是我们自己的：穿透 5 个敌人、沿飞行方向极轻微扩散，命中没有附加效果（照村正那一类）。
	/// </summary>
	public class ScavengerCrescentSlash : ModProjectile
	{
		/// <summary>与 <c>Projectile_116.xnb</c>（原版 SwordBeam）的宽高一致。</summary>
		public const int VanillaWidth = 32;

		/// <summary>与 <c>Projectile_116.xnb</c> 一致。</summary>
		public const int VanillaHeight = 32;

		/// <summary>复用原版贴图：本弹幕不提供自己的 PNG。</summary>
		public override string Texture => null;

		public override void SetDefaults()
		{
			Projectile.width = VanillaWidth;
			Projectile.height = VanillaHeight;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 5;          // 宽刃大刀：一刀扫过去能连穿
			Projectile.timeLeft = 40;
			Projectile.tileCollide = false;    // 刀光不该被墙挡住
			Projectile.ignoreWater = true;
			Projectile.light = 0.45f;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;

			// 画的是原版贴图，所以偏移量一律归零，免得叠上原版 SwordBeam 的预设偏移
			Projectile.DrawOffsetX = 0;
			Projectile.DrawOriginOffsetX = 0f;
			Projectile.DrawOriginOffsetY = 0f;
		}

		public override void AI()
		{
			// 月牙始终朝着飞行方向；整体是"甩出去"的感觉，所以不需要额外自转
			Projectile.rotation = Projectile.velocity.ToRotation();

			// 飞行中极轻微扩散：越飞越宽（但速度不变，只是视觉与判定都放大一点点）
			if (Projectile.localAI[0] < 24f) {
				Projectile.localAI[0] += 1f;
			}

			Projectile.scale = 1f + Projectile.localAI[0] * 0.008f;

			if (!Main.dedServ && Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SilverFlame);
				dust.velocity *= 0.2f;
				dust.noGravity = true;
				dust.scale = 0.85f;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[ProjectileID.SwordBeam].Value;

			if (texture == null) {
				return false;
			}

			Main.EntitySpriteDraw(
				texture,
				Projectile.Center - Main.screenPosition,
				null,
				Color.White,
				Projectile.rotation,
				new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
				Projectile.scale,
				SpriteEffects.None,
				0f);

			return false;   // 已经自己画完，不要再走默认绘制
		}
	}

	/// <summary>
	/// 射手「污染炮」的**凝胶污染弹**。
	/// <para/>外形复用原版 <see cref="ProjectileID.SlimeGun"/>（史莱姆枪那团黏液），
	/// 手感是炮：单发高、速度慢、带一点下坠；命中或落地时炸出一小片**污染云**
	/// （<see cref="ScavengerPollutionCloud"/>，挂「污染」减益），命中敌人直接挂 Pollution。
	/// </summary>
	public class ScavengerGelRound : ModProjectile
	{
		/// <summary>与 <c>Projectile_406.xnb</c>（原版 SlimeGun）的宽高一致。</summary>
		public const int VanillaWidth = 16;

		/// <summary>与 <c>Projectile_406.xnb</c> 一致。</summary>
		public const int VanillaHeight = 16;

		public override string Texture => null;

		public override void SetDefaults()
		{
			Projectile.width = VanillaWidth;
			Projectile.height = VanillaHeight;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.light = 0.35f;
			Projectile.alpha = 10;

			Projectile.DrawOffsetX = 0;
			Projectile.DrawOriginOffsetX = 0f;
			Projectile.DrawOriginOffsetY = 0f;
		}

		public override void AI()
		{
			Projectile.rotation += Projectile.velocity.X * 0.03f;

			// 炮类手感：慢、沉，带一点点下坠
			Projectile.velocity.Y += 0.045f;

			if (Projectile.velocity.Y > 9f) {
				Projectile.velocity.Y = 9f;
			}

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.velocity *= 0.25f;
				dust.noGravity = true;
				dust.scale = 1.1f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(ModContent.BuffType<Pollution>(), 300);
			SpawnCloud();
		}

		public override void OnKill(int timeLeft)
		{
			SpawnCloud();
		}

		/// <summary>炸出一小片污染云（只有服务端生成，靠弹幕同步带到各客户端）。</summary>
		private void SpawnCloud()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			Projectile.NewProjectile(
				Projectile.GetSource_Death(),
				Projectile.Center,
				Vector2.Zero,
				ModContent.ProjectileType<ScavengerPollutionCloud>(),
				(int)(Projectile.damage * 0.35f),
				0f,
				Projectile.owner);

			if (Main.dedServ) {
				return;
			}

			for (int i = 0; i < 10; i++) {
				Vector2 velocity = Main.rand.NextVector2Circular(2.6f, 1.4f);
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, velocity.X, velocity.Y, 110, default, 1.3f);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[ProjectileID.SlimeGun].Value;

			if (texture == null) {
				return false;
			}

			Main.EntitySpriteDraw(
				texture,
				Projectile.Center - Main.screenPosition,
				null,
				Color.White,
				Projectile.rotation,
				new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
				Projectile.scale,
				SpriteEffects.None,
				0f);

			return false;
		}
	}

	/// <summary>
	/// 污染云：凝胶污染弹砸出来的小片污染地带。
	/// <para/>外形复用原版 <see cref="ProjectileID.ToxicCloud"/>（毒云），
	/// 只持续约 4 秒、范围小，进入范围的敌人会被挂「污染」并持续掉血。
	/// </summary>
	public class ScavengerPollutionCloud : ModProjectile
	{
		/// <summary>与 <c>Projectile_511.xnb</c>（原版 ToxicCloud）的宽高一致。</summary>
		public const int VanillaWidth = 32;

		/// <summary>与 <c>Projectile_511.xnb</c> 一致。</summary>
		public const int VanillaHeight = 32;

		/// <summary>存在时间（tick）。</summary>
		private const int Lifetime = 240;

		public override string Texture => null;

		public override void SetDefaults()
		{
			Projectile.width = VanillaWidth;
			Projectile.height = VanillaHeight;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;          // 反复伤害进入范围的敌人
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.alpha = 40;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;  // 每 0.5 秒跳一次伤害

			Projectile.DrawOffsetX = 0;
			Projectile.DrawOriginOffsetX = 0f;
			Projectile.DrawOriginOffsetY = 0f;
		}

		public override void AI()
		{
			// 原地慢慢扩散：前 1 秒长大，之后缓慢收缩
			float age = 1f - Projectile.timeLeft / (float)Lifetime;
			Projectile.scale = age < 0.25f ? 0.6f + age * 1.6f : 1.05f - (age - 0.25f) * 0.35f;

			Projectile.velocity = Vector2.Zero;

			if (!Main.dedServ && Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
				dust.velocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.1f, -0.3f));
				dust.noGravity = true;
				dust.scale = 1.2f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(ModContent.BuffType<Pollution>(), 240);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[ProjectileID.ToxicCloud].Value;

			if (texture == null) {
				return false;
			}

			// 云是慢慢转的，看起来才不像一张贴图钉在地上
			float rotation = Projectile.timeLeft * 0.006f;

			Main.EntitySpriteDraw(
				texture,
				Projectile.Center - Main.screenPosition,
				null,
				Color.White * 0.82f,
				rotation,
				new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
				Projectile.scale,
				SpriteEffects.None,
				0f);

			return false;
		}
	}
}
