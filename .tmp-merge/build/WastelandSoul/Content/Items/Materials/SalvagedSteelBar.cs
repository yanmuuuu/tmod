using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>
	/// 精钢：精钢碎块经熔炉重熔后的产物。
	/// <para/>材料链第二环：五职业精钢套装（开发优先级 3）的合成材料。
	/// </summary>
	public class SalvagedSteelBar : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 20;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(silver: 12);
			Item.rare = ItemRarityID.Blue;
		}

		public override void AddRecipes()
		{
			// 精钢碎块 →（熔炉）→ 精钢
			// 比例 2:1：一场 Boss 战约掉 20~30 碎块，够做一套防具的量级，避免刷到肝爆
			CreateRecipe()
				.AddIngredient<SalvagedSteelChunk>(2)
				.AddTile(TileID.Furnaces)
				.Register();
		}
	}
}
