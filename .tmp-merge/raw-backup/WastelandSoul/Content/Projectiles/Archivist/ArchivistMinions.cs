using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Projectiles.Scavenger;

namespace WastelandSoul.Content.Projectiles.Archivist
{
	// ====================================================================================
	// 召唤师专属仆从（Boss2 归档者）。与 Boss1 共用 WastelandMinionBase 的跟随/索敌/射击逻辑，
	// 这里只写差异。同样遵守「仆从弹幕 + 专属 Buff 成对」的规则。
	// ====================================================================================

	/// <summary>A 线 · 档案浮空使魔：悬停较高，每 1.5 秒投出一枚索引页。</summary>
	public class ArchivistFamiliarMinion : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<ArchivistFamiliarBuff>();

		protected override int ShotType => ModContent.ProjectileType<ArchivistIndexPage>();

		protected override float HoverHeight => 74f;

		protected override float AttackInterval => 90f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 28;
			Projectile.height = 28;
		}
	}

	/// <summary>A 线仆从的维持 Buff。</summary>
	public class ArchivistFamiliarBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<ArchivistFamiliarMinion>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			} else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	/// <summary>B 线专属 · 归档哨塔：射得更勤、射程更远、弹幕更强。
	/// （设计里写的是"哨兵"，这里先按仆从实现，等哨兵位机制需要时再改。）</summary>
	public class ArchivistFamiliarMinionEX : WastelandMinionBase
	{
		protected override int BuffType => ModContent.BuffType<ArchivistFamiliarBuffEX>();

		protected override int ShotType => ModContent.ProjectileType<ArchivistIndexPageEX>();

		protected override float HoverHeight => 74f;

		protected override float AttackInterval => 48f;

		protected override float Range => 760f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.width = 34;
			Projectile.height = 34;
		}
	}

	/// <summary>B 线仆从的维持 Buff。</summary>
	public class ArchivistFamiliarBuffEX : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<ArchivistFamiliarMinionEX>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			} else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
