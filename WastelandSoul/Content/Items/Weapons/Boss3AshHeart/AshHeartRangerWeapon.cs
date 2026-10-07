using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss3AshHeart
{
	/// <summary>
	/// Ash Heart Ember Rifle（灰烬之心 · A 线 · 可合成）
	/// <para/>设计定位：『燃灰步枪』：连射型枪械，量级对标金星马格南(50/9)——单发略低、射速略慢，但每发有 20% 概率替换为燃烧弹，命中附加 3 秒『着火了！』并溅出两粒低伤火星。吃子弹、依赖弹药供给，定位是高射速的持续输出而非爆发。
	/// <para/>制作站点：秘银砧（血肉墙之后装备站点）
	/// </summary>
	public class AshHeartRangerWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 46;
		protected override int UseTime => 11;
		protected override float Knockback => 3.0f;
		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.AshHeart.AshHeartRangerProjectile>();

		protected override float ShootSpeed => 13.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshHeartAlloyBar>(10)
				.AddIngredient(ItemID.ChlorophyteBar, 12)
				.AddIngredient(ItemID.HallowedBar, 8)
				.AddIngredient(ItemID.Ectoplasm, 6)
				.AddTile(WastelandCraftingStations.HardmodeAnvil)
				.Register();
		}
	}
}
