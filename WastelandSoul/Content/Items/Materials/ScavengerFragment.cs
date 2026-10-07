using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>
	/// 清道夫残片：执行单元-07 的核心残骸，带有一段被故意阉割的代码。
	/// <para/>剧情道具 + 后续合成材料（开发优先级 4/5 接入壁炉数据终端）。
	/// </summary>
	public class ScavengerFragment : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 20;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.LightRed;
		}
	}
}
