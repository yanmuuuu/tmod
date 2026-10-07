using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Boneblade MK-II（归档者 · B 线 · 专属掉落）
	/// <para/>设计定位：专属掉落（仅掉落袋）。真近战长剑，挥砍时甩出一道可穿 3 个敌人、飞行约 12 格的骨白剑气（伤害面板 75%），剑气命中后附加 2 秒破甲感（无实际减防，仅视觉/音效强化）。强度贴近永夜边缘但略低，弥补骷髅王后到血肉墙前的真近战空窗。
	/// </summary>
	public class ArchivistWarriorWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 34;
		protected override int UseTime => 18;
		protected override float Knockback => 6.5f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistBoneArcEX>();

		protected override float ShootSpeed => 12.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从归档者的掉落袋开出。
	}
}
