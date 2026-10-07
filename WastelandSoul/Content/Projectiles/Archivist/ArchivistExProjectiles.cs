namespace WastelandSoul.Content.Projectiles.Archivist
{
	// ====================================================================================
	// Boss2 B 线（掉落袋专属）弹幕：A 线的强化版，靠继承复用行为（弹墙/困惑/折返）。
	// ====================================================================================

	/// <summary>专属·骨白剑气：飞得更远（活 50 帧）、穿透 2、更亮。</summary>
	public class ArchivistBoneArcEX : ArchivistBoneArc
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 50;
			Projectile.light = 0.45f;
		}
	}

	/// <summary>专属·索引页：穿透 3、活得更久、更亮（弹墙与困惑行为自动继承）。</summary>
	public class ArchivistIndexPageEX : ArchivistIndexPage
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 300;
			Projectile.light = 0.5f;
		}
	}

	/// <summary>专属·骨片：更大、穿透 3、活得更久。</summary>
	public class ArchivistBoneShardEX : ArchivistBoneShard
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 200;
		}
	}

	/// <summary>专属·封存骨标：穿透 6、活得更久（折返行为自动继承）。</summary>
	public class ArchivistBoneBoomerangEX : ArchivistBoneBoomerang
	{
		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.penetrate = 6;
			Projectile.timeLeft = 360;
		}
	}
}
