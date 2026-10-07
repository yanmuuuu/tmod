using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Scrap Cleaver（清道夫 · A 线 · 可合成）
	/// <para/>设计定位：【占位名/材料约定】专属材料暂定 ScavengerScrap（清道夫废料），由清道夫掉落 4–6 个（专家袋 6–9）。定位：前期宽刃砍刀，长度略长于 Iron Broadsword(12) 、略短于 Light's Bane(17)；挥砍时甩出 1 片废料碎片（弹幕伤害 6、穿透 1 个敌人、不受重力影响），手感厚重、击退偏高，是前期最稳的近战平A。配方为「专属废料 + 铁/铅锭 + 凝胶 + 木材」，贴合史莱姆王时期的材料池，铁砧制作。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ScavengerWarriorWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override int Damage => 19;
		protected override int UseTime => 23;
		protected override float Knockback => 6.0f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerScrapShard>();

		protected override float ShootSpeed => 10.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Melee(Item);
		}

		public override void AddRecipes()
		{
			// 分支 1：ScavengerScrap / IronBar / Gel / Wood
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(8)
				.AddIngredient(ItemID.IronBar, 12)
				.AddIngredient(ItemID.Gel, 6)
				.AddIngredient(ItemID.Wood, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			// 分支 2：ScavengerScrap / LeadBar / Gel / Wood
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(8)
				.AddIngredient(ItemID.LeadBar, 12)
				.AddIngredient(ItemID.Gel, 6)
				.AddIngredient(ItemID.Wood, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
