using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Drone Beacon（清道夫 · A 线 · 可合成）
	/// <para/>设计定位：弹幕设想：召唤 1 只「废料无人机」小仆从，接触伤害 10、占用 1 个仆从位，每 1.5 秒发射一发 4 伤害的废料弹（可轻微追踪最近敌人）。与 Slime Staff(8) 同级但更主动，低于 Hornet Staff(11–12)/Imp Staff(17)，属于史莱姆王时期合理的第二只召唤兽；凝胶×14 体现「黏合废铁」的设定。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ScavengerSummonerWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Summon;
		protected override int Damage => 10;
		protected override int UseTime => 30;
		protected override float Knockback => 2.0f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerDroneMinion>();

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Summon(Item, ModContent.BuffType<Content.Projectiles.Scavenger.ScavengerDroneBuff>(), 10);
		}

		public override void AddRecipes()
		{
			// 分支 1：ScavengerScrap / IronBar / Gel / Wood
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(14)
				.AddIngredient(ItemID.IronBar, 6)
				.AddIngredient(ItemID.Gel, 14)
				.AddIngredient(ItemID.Wood, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			// 分支 2：ScavengerScrap / LeadBar / Gel / Wood
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(14)
				.AddIngredient(ItemID.LeadBar, 6)
				.AddIngredient(ItemID.Gel, 14)
				.AddIngredient(ItemID.Wood, 10)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
