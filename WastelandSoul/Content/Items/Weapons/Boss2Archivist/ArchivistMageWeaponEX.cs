using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Index Tome MK-II（归档者 · B 线 · 专属掉落）
	/// <para/>设计定位：专属掉落（仅掉落袋）。『归档射线』法杖：持续读条式发射高速细光束，命中时在目标身上标记 1 层『索引』（最多 3 层），第 3 层时追加一次 20 点额外真实结算伤害。耗蓝 12，直线穿透 1 个敌人；单体持续输出是本时期法师上限，但对走位要求高。
	/// </summary>
	public class ArchivistMageWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 30;
		protected override int UseTime => 22;
		protected override float Knockback => 2.0f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistIndexPageEX>();

		protected override float ShootSpeed => 16.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 12);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从归档者的掉落袋开出。
	}
}
