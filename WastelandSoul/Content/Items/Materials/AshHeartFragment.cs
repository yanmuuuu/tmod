using Terraria;
using Terraria.ID;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>
	/// Ash Heart Fragment（灰烬之核）。
	/// <para/>从灰烬之心上剥离的自持余烬。
	/// <para/>需要精金熔炉的高温才能熔炼成灰烬合金锭。
	/// </summary>
	public class AshHeartFragment : WastelandMaterial
	{
		protected override int IconSize => 24;

		protected override int Rarity => WastelandRarityTiers.MidLate;

		protected override int SellPrice => Item.sellPrice(gold: 1);
	}
}
