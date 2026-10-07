using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Bone Scattergun（归档者 · A 线 · 可合成）
	/// <para/>设计定位：骨质短铳，一次扇形打出 4 枚骨片（每枚面板 55%、单次命中上限 2 枚），弹丸命中或落地后爆成 2 片小额碎片。抬手较慢、无弹道下坠感强，属于中近距离压制枪；对空/多目标时收益明显，贴脸全中收益过高故用命中上限压制。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ArchivistRangerWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 22;
		protected override int UseTime => 30;
		protected override float Knockback => 5.0f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistBoneShard>();

		protected override float ShootSpeed => 12.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ArchivistFragment>(9)
				.AddIngredient(ItemID.MeteoriteBar, 12)
				.AddIngredient(ItemID.Bone, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
