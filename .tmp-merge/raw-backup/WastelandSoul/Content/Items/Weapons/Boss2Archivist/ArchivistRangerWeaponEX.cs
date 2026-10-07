using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Bone Scattergun MK-II（归档者 · B 线 · 专属掉落）
	/// <para/>设计定位：专属掉落（仅掉落袋）。陨星手炮：单发高速陨星弹，飞行中留下短暂尾迹并对路径上的敌人造成 50% 触碰伤害，命中后小范围爆裂并散射 3 枚骨刺。射速慢、单发收益高，对付骷髅王后成群小怪与硬直目标都强势，但清杂不如连发枪。
	/// </summary>
	public class ArchivistRangerWeaponEX : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 32;
		protected override int UseTime => 22;
		protected override float Knockback => 6.0f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistBoneShardEX>();

		protected override float ShootSpeed => 14.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
		}

		// B 线为专属掉落：**不写任何 AddRecipes()**，只能从归档者的掉落袋开出。
	}
}
