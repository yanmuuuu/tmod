using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>
	/// 精钢碎块：清道夫身上剥落的高强度合金残片。
	/// <para/>材料链第一环：需要熔炉熔炼成「精钢」。
	/// </summary>
	public class SalvagedSteelChunk : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 18;
			Item.height = 18;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(silver: 3);
			Item.rare = ItemRarityID.Blue;
		}
	}
}
