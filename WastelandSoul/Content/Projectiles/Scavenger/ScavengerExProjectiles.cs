namespace WastelandSoul.Content.Projectiles.Scavenger
{
	// ====================================================================================
	// Boss1 B 线（掉落袋专属）弹幕：A 线的强化版。
	// 用继承而不是复制，改数值只需要在一个地方调；行为（灼烧/减速等）自动继承。
	// ====================================================================================

	/// <summary>专属·废料碎片：更大、穿透 3、发光。</summary>
	public class ScavengerScrapShardEX : ScavengerScrapShard
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 220;
			Projectile.light = 0.3f;
		}
	}

	/// <summary>专属·带电火花团：更大、穿透 2、更亮。</summary>
	public class ScavengerSparkEX : ScavengerSpark
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 150;
			Projectile.light = 0.7f;
		}
	}

	/// <summary>专属·重步枪弹：更粗、穿透 2、额外更新（飞得更快更稳）。</summary>
	public class ScavengerRifleRoundEX : ScavengerPistolRound
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.penetrate = 2;
			Projectile.extraUpdates = 2;
			Projectile.light = 0.5f;
		}
	}

	/// <summary>专属·旋转裂片：更大、穿透 5、活得更久。</summary>
	public class ScavengerCaltropEX : ScavengerCaltrop
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.penetrate = 5;
			Projectile.timeLeft = 300;
		}
	}
}
