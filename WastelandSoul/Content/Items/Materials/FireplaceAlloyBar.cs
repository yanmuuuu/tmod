using Terraria;
using Terraria.ID;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>
	/// Fireplace Alloy Bar（壁炉合金锭）。
	/// <para/>曾经包覆着壁炉守卫的冷白合金。
	/// <para/>在精金熔炉上熔炼。
	/// </summary>
	public class FireplaceAlloyBar : WastelandMaterial
	{
		protected override int IconSize => 24;

		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int SellPrice => Item.sellPrice(gold: 4);

		public override void AddRecipes()
		{
			// 血肉墙之后：**精金熔炉**（含钛金熔炉）熔炼
			CreateRecipe()
				.AddIngredient<FireplaceFragment>(2)
				.AddTile(WastelandCraftingStations.HardmodeForge)
				.Register();
		}
	}
}
