using Terraria;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.CLine
{
	/// <summary>
	/// C 线武器的装配形态。
	/// <para/>⚠️ 这里以前是**字符串**（<c>"melee"</c> / <c>"magic"</c> …），拼错一个字母就会静默落进
	/// <c>default</c> 分支、退化成「手写的一套参数」而且**不报任何错**（审计发现的隐患）。
	/// 改成枚举之后，写错名字是**编译错误**，不可能再静默降级。
	/// </summary>
	public enum CLineKind
	{
		Melee,
		Magic,
		Ranged,
		Summon
	}

	/// <summary>
	/// 小怪小概率掉落的不可制作武器。弹幕复用对应 Boss 的 A 线，数值略低于可制作版，
	/// 这样幸运掉落能提前用，但不会盖过 Boss 武器。
	/// </summary>
	public abstract class CLineWeapon : WastelandClassWeapon
	{
		protected abstract CLineKind Kind { get; }

		protected abstract int Mana { get; }

		protected abstract int SummonBuff { get; }

		public override void SetDefaults()
		{
			base.SetDefaults();

			switch (Kind) {
				case CLineKind.Melee:
					WastelandWeaponKit.Melee(Item);
					break;
				case CLineKind.Magic:
					WastelandWeaponKit.Magic(Item, Mana);
					break;
				case CLineKind.Ranged:
					WastelandWeaponKit.Ranged(Item);
					break;
				case CLineKind.Summon:
					WastelandWeaponKit.Summon(Item, SummonBuff, Mana);
					break;
				default:
					// 枚举写错是编译错误，这里只兜住「以后新增了枚举值却忘了加分支」的情况。
					Mod.Logger.Warn($"CLineWeapon: 未处理的装配形态 {Kind}（{GetType().Name}），已退化成手写实现");
					Item.useStyle = Terraria.ID.ItemUseStyleID.Swing;
					Item.noMelee = true;
					Item.noUseGraphic = true;
					Item.consumable = false;
					Item.maxStack = 1;
					Item.autoReuse = true;
					Item.UseSound = Terraria.ID.SoundID.Item1;
					break;
			}
		}
	}

	public class ScavengerCWarrior : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Melee;
		protected override int Mana => 0;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 16;
		protected override int UseTime => 22;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerScrapShard>();
	}

	public class ScavengerCMage : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Magic;
		protected override int Mana => 6;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 15;
		protected override int UseTime => 24;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerSpark>();
	}

	public class ScavengerCRanger : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Ranged;
		protected override int Mana => 0;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 13;
		protected override int UseTime => 18;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerPistolRound>();
	}

	public class ScavengerCSummoner : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Summon;
		protected override int Mana => 8;
		protected override int SummonBuff => ModContent.BuffType<Content.Projectiles.Scavenger.ScavengerDroneBuff>();
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 8;
		protected override int UseTime => 30;
		protected override int Rarity => WastelandRarityTiers.Early;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerDroneMinion>();
	}

	public class ArchivistCWarrior : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Melee;
		protected override int Mana => 0;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 28;
		protected override int UseTime => 22;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistBoneArc>();
	}

	public class ArchivistCMage : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Magic;
		protected override int Mana => 8;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 26;
		protected override int UseTime => 24;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistIndexPage>();
	}

	public class ArchivistCRanger : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Ranged;
		protected override int Mana => 0;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 24;
		protected override int UseTime => 18;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistBoneShard>();
	}

	public class ArchivistCSummoner : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Summon;
		protected override int Mana => 10;
		protected override int SummonBuff => ModContent.BuffType<Content.Projectiles.Archivist.ArchivistFamiliarBuff>();
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 16;
		protected override int UseTime => 34;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistFamiliarMinion>();
	}

	public class AshHeartCWarrior : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Melee;
		protected override int Mana => 0;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 64;
		protected override int UseTime => 24;
		protected override int Rarity => WastelandRarityTiers.MidLate;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartWarriorProjectile>();
	}

	public class AshHeartCMage : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Magic;
		protected override int Mana => 12;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 58;
		protected override int UseTime => 22;
		protected override int Rarity => WastelandRarityTiers.MidLate;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartMageProjectile>();
	}

	public class AshHeartCRanger : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Ranged;
		protected override int Mana => 0;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 52;
		protected override int UseTime => 16;
		protected override int Rarity => WastelandRarityTiers.MidLate;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartRangerProjectile>();
	}

	public class AshHeartCSummoner : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Summon;
		protected override int Mana => 12;
		protected override int SummonBuff => ModContent.BuffType<Content.Projectiles.LateBosses.AshHeartEmberBuff>();
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 38;
		protected override int UseTime => 26;
		protected override int Rarity => WastelandRarityTiers.MidLate;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.LateBosses.AshHeartEmberMinion>();
	}

	public class FireplaceCWarrior : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Melee;
		protected override int Mana => 0;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 80;
		protected override int UseTime => 22;
		protected override int Rarity => WastelandRarityTiers.Late;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Fireplace.FireplaceWarriorProjectile>();
	}

	public class FireplaceCMage : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Magic;
		protected override int Mana => 14;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 74;
		protected override int UseTime => 20;
		protected override int Rarity => WastelandRarityTiers.Late;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Fireplace.FireplaceMageProjectile>();
	}

	public class FireplaceCRanger : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Ranged;
		protected override int Mana => 0;
		protected override int SummonBuff => 0;
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 70;
		protected override int UseTime => 14;
		protected override int Rarity => WastelandRarityTiers.Late;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Fireplace.FireplaceRangerProjectile>();
	}

	public class FireplaceCSummoner : CLineWeapon
	{
		protected override CLineKind Kind => CLineKind.Summon;
		protected override int Mana => 16;
		protected override int SummonBuff => ModContent.BuffType<Content.Projectiles.LateBosses.FireplaceSentryBuff>();
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 48;
		protected override int UseTime => 24;
		protected override int Rarity => WastelandRarityTiers.Late;
		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.LateBosses.FireplaceSentryMinion>();
	}
}
