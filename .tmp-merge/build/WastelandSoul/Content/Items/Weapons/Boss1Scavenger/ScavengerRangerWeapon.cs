using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Scrap Pistol（清道夫 · A 线 · 可合成）
	/// <para/>设计定位：弹幕设想：废料手枪，消耗火枪子弹，弹道笔直、无下坠，射速中等（21）；每次射击有 10% 概率追加一发偏移 8° 的副弹（伤害 60%，即 9）。数值 15 对齐 The Undertaker(15)，低于 Musket(24)，对手感是「可靠但不爆表」的前期枪械；用木材做枪托、凝胶密封，符合废土拼装设定。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ScavengerRangerWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Ranged;
		protected override int Damage => 15;
		protected override int UseTime => 21;
		protected override float Knockback => 3.0f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerPistolRound>();

		protected override float ShootSpeed => 11.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Ranged(Item);
		}

		public override void AddRecipes()
		{
			// 分支 1：ScavengerScrap / IronBar / Gel / Wood
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(12)
				.AddIngredient(ItemID.IronBar, 14)
				.AddIngredient(ItemID.Gel, 6)
				.AddIngredient(ItemID.Wood, 14)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			// 分支 2：ScavengerScrap / LeadBar / Gel / Wood
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(12)
				.AddIngredient(ItemID.LeadBar, 14)
				.AddIngredient(ItemID.Gel, 6)
				.AddIngredient(ItemID.Wood, 14)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
