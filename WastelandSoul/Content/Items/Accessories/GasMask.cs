using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Buffs;
using WastelandSoul.Content.Subworlds;

namespace WastelandSoul.Content.Items.Accessories
{
	/// <summary>
	/// **防毒面具**：戴着它进壁炉世界就不会被有害气体侵蚀（<see cref="GasPoison"/>）。
	///
	/// <para/>设定上它是"旧时代工人留下的最后一批装备"，所以配方走废土这边已有的材料
	/// （精钢 + 玻璃镜片），不需要动到壁炉世界里那些**挖不走**的瑜钢合金。
	/// </summary>
	public class GasMask : WastelandAccessory
	{
		protected override int Rarity => WastelandRarityTiers.Early;

		protected override int SellPrice => Item.sellPrice(gold: 1);

		protected override void UpdateWastelandAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<FireplaceAtmospherePlayer>().gasMaskEquipped = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<Content.Items.Materials.SalvagedSteelBar>(), 5)
				.AddIngredient(ItemID.Glass, 3)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
