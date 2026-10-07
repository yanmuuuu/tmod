using Terraria;
using Terraria.ID;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>
	/// Archivist Fragment（归档者残响）。
	/// <para/>归档者留下的残响。
	/// <para/>守望者计划派来回收自身记忆的审计单元。
	/// </summary>
	public class ArchivistFragment : WastelandMaterial
	{
		protected override int IconSize => 24;

		protected override int Rarity => WastelandRarityTiers.EarlyLate;

		protected override int SellPrice => Item.sellPrice(silver: 10);
	}
}
