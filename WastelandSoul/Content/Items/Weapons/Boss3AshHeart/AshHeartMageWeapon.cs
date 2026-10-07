using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss3AshHeart
{
	/// <summary>
	/// Ash Heart Core Staff（灰烬之心 · A 线 · 可合成）
	/// <para/>设计定位：弹幕设想『灰烬心核』：射出一颗缓慢下坠的心核（有微重力、轻微追踪），撞到地形/敌人后炸成一团滞留 5 秒的灰烬云，云内每秒对敌人结算一次伤害。蓝耗 16。单体 DPS 略低于幽灵法杖(65)，但封路/清群明显更强，是『磁球+火焰』的混合手感。
	/// <para/>制作站点：秘银砧（血肉墙之后装备站点）
	/// </summary>
	public class AshHeartMageWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 62;
		protected override int UseTime => 19;
		protected override float Knockback => 4.5f;
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartMageProjectile>();

		protected override float ShootSpeed => 9.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 16);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshHeartAlloyBar>(10)
				.AddIngredient(ItemID.ChlorophyteBar, 8)
				.AddIngredient(ItemID.Ectoplasm, 12)
				.AddIngredient(ItemID.HallowedBar, 6)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
