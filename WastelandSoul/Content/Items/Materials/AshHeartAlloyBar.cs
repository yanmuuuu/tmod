using Terraria;
using Terraria.ID;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>
	/// Ash Heart Alloy Bar（灰烬合金锭）。
	/// <para/>灰烬之核与旧时代合金熔合而成。
	/// <para/>在精金熔炉上熔炼。
	/// </summary>
	public class AshHeartAlloyBar : WastelandMaterial
	{
		protected override int IconSize => 24;

		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int SellPrice => Item.sellPrice(gold: 2);

		public override void AddRecipes()
		{
			// 血肉墙之后：**精金熔炉**（含钛金熔炉）熔炼
			CreateRecipe()
				.AddIngredient<AshHeartFragment>(2)
				.AddTile(WastelandCraftingStations.HardmodeForge)
				.Register();
		}
	}
}
