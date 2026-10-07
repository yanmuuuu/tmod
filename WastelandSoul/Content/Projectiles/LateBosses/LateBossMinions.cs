using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Projectiles.AshHeart;
using WastelandSoul.Content.Projectiles.Fireplace;
using WastelandSoul.Content.Projectiles.Scavenger;

namespace WastelandSoul.Content.Projectiles.LateBosses
{
	// ====================================================================================
	// Boss3 灰烬之心 / Boss4 壁炉守卫 的召唤师专属仆从。
	// 与 Boss1/Boss2 共用 WastelandMinionBase（跟随/漂浮错位/索敌/射击）。
	//
	// ⚠️ 铁律：仆从必须「仆从弹幕 + 专属 Buff」成对。武器里
	//    WastelandWeaponKit.Summon(Item, <buff>, N) 的第二个参数
	//    必须传这里对应的 Buff，沿用原版 Buff 会让仆从秒消。
	// ====================================================================================

	/// <summary>A 线 · 余烬残影（灰烬之心）：贴地扑咬型，每 1.5 秒喷一发灼烧弹。</summary>
	public class AshHeartEmberMinion : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<AshHeartEmberBuff>();

		protected override int ShotType => ModContent.ProjectileType<AshHeartRangerProjectile>();

		protected override float HoverHeight => 40f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 26;
			Projectile.height = 26;
		}
	}

	public class AshHeartEmberBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<AshHeartEmberMinion>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			} else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>B 线专属 · 炉心余烬：射得更勤、射程更远、弹幕更强。</summary>
	public class AshHeartEmberMinionEX : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<AshHeartEmberBuffEX>();

		protected override int ShotType => ModContent.ProjectileType<AshHeartRangerProjectileEX>();

		protected override float HoverHeight => 40f;

		protected override float AttackInterval => 54f;

		protected override float Range => 780f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 32;
			Projectile.height = 32;
		}
	}

	public class AshHeartEmberBuffEX : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<AshHeartEmberMinionEX>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			} else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>A 线 · 壁炉浮游哨（壁炉守卫）：悬停较高，每 1.5 秒射一发冷光钉弹。</summary>
	public class FireplaceSentryMinion : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<FireplaceSentryBuff>();

		protected override int ShotType => ModContent.ProjectileType<FireplaceRangerProjectile>();

		protected override float HoverHeight => 70f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 28;
			Projectile.height = 28;
		}
	}

	public class FireplaceSentryBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<FireplaceSentryMinion>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			} else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>B 线专属 · 重型浮游哨：射得更勤、射程更远、弹幕穿甲。</summary>
	public class FireplaceSentryMinionEX : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<FireplaceSentryBuffEX>();

		protected override int ShotType => ModContent.ProjectileType<FireplaceRangerProjectileEX>();

		protected override float HoverHeight => 70f;

		protected override float AttackInterval => 50f;

		protected override float Range => 800f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 34;
			Projectile.height = 34;
		}
	}

	public class FireplaceSentryBuffEX : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<FireplaceSentryMinionEX>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			} else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
