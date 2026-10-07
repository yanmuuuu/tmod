using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.Items.Weapons;

namespace WastelandSoul.Content.Items.Weapons.Boss1Scavenger
{
	/// <summary>
	/// Scavenger Scrap Knife（清道夫 · A 线 · 可合成）
	/// <para/>设计定位：投掷伤害类（DamageClass.Throwing），可堆叠 999，一次合成产出 50 个。数值 14 介于 Throwing Knife(12)/Poisoned Knife(13) 与 Bone(20) 之间；命中后有 20% 概率碎裂、弹射到 12 格内最近的敌人（碎片伤害 7、不穿透），形成「废料回收」手感。用木柄+废料刃的廉价量产设定，鼓励前期走位投掷流。
	/// <para/>制作站点：铁砧（前期装备站点）
	/// </summary>
	public class ScavengerRogueWeapon : WastelandClassWeapon
	{
		protected override DamageClass Class => DamageClass.Throwing;
		protected override int Damage => 14;
		protected override int UseTime => 14;
		protected override float Knockback => 2.5f;
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int ShootType => ModContent.ProjectileType<Content.Projectiles.Scavenger.ScavengerCaltrop>();

		protected override float ShootSpeed => 12.0f;

		public override void SetDefaults()
		{
			base.SetDefaults();
			WastelandWeaponKit.Rogue(Item);
		}

		public override void AddRecipes()
		{
			// 分支 1：ScavengerScrap / IronBar / Gel / Wood
			CreateRecipe(50)
				.AddIngredient<SalvagedSteelChunk>(3)
				.AddIngredient(ItemID.IronBar, 2)
				.AddIngredient(ItemID.Gel, 2)
				.AddIngredient(ItemID.Wood, 2)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();

			// 分支 2：ScavengerScrap / LeadBar / Gel / Wood
			CreateRecipe(50)
				.AddIngredient<SalvagedSteelChunk>(3)
				.AddIngredient(ItemID.LeadBar, 2)
				.AddIngredient(ItemID.Gel, 2)
				.AddIngredient(ItemID.Wood, 2)
				.AddTile(WastelandCraftingStations.EarlyAnvil)
				.Register();
		}
	}
}
