using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Spark Rod（清道夫 · A 线 · 可合成）
	/// <para/>设计定位：弹幕设想：射出带电火花团，速度中等、无重力，命中后留下 0.5 秒残渣灼烧（每跳 2 伤害）；耗蓝 8。伤害 16 落在该时期宝石法杖区间（紫水晶 14→钻石 23）内，射速略快、单发不高，属于「点射过渡杖」，不会抢走 Wand of Sparking 的零蓝定位，也不会越级到 Water Bolt/Crimson Rod。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ScavengerMageWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Magic;
		protected override int Damage => 16;
		protected override int UseTime => 22;
		protected override float Knockback => 3.0f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerSpark>();

		protected override float ShootSpeed => 8.5f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Magic(Item, 8);
		}

		public override void AddRecipes()
		{
			// 分支 1：ScavengerScrap / IronBar / Gel / Wood
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(10)
				.AddIngredient(ItemID.IronBar, 8)
				.AddIngredient(ItemID.Gel, 12)
				.AddIngredient(ItemID.Wood, 8)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			// 分支 2：ScavengerScrap / LeadBar / Gel / Wood
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(10)
				.AddIngredient(ItemID.LeadBar, 8)
				.AddIngredient(ItemID.Gel, 12)
				.AddIngredient(ItemID.Wood, 8)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
