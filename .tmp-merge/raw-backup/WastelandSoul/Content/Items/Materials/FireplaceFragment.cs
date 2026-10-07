using Terraria;
using Terraria.ID;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>
	/// Fireplace Fragment（壁炉残核）。
	/// <para/>壁炉最后一道防线的碎片。
	/// <para/>需要精金熔炉的高温才能熔炼成壁炉合金锭。
	/// </summary>
	public class FireplaceFragment : WastelandMaterial
	{
		protected override int IconSize => 24;

		protected override int Rarity => WastelandRarityTiers.Late;

		protected override int SellPrice => Item.sellPrice(gold: 2);
	}
}
