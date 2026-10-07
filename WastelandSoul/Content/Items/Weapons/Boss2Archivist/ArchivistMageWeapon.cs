using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss2Archivist
{
	/// <summary>
	/// Archivist Index Tome（归档者 · A 线 · 可合成）
	/// <para/>设计定位：档案法典（书类），发射缓慢的『索引页』弹幕：飞行速度低于火之花、可弹墙 2 次，命中附加 3 秒困惑，适合地牢/室内拐角利用墙壁折射输出。耗蓝 9，DPS 靠弹幕反射次数而非直射速度，是本时期法师的『控场型』法器。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ArchivistMageWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 21;
		protected override int UseTime => 17;
		protected override float Knockback => 4.5f;
		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Archivist.ArchivistIndexPage>();

		protected override float ShootSpeed => 8.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 9);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ArchivistFragment>(10)
				.AddIngredient(ItemID.MeteoriteBar, 12)
				.AddIngredient(ItemID.Bone, 12)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
