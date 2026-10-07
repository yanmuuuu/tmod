using Terraria;
using Terraria.ID;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>废土小怪掉落的芯片。拿去给智械人换这个时期做不出来的饰品。</summary>
	public class Chip : WastelandMaterial
	{
		protected override int IconSize => 18;

		protected override int Rarity => ItemRarityID.Green;

		protected override int SellPrice => Item.sellPrice(silver: 8);
	}
}
