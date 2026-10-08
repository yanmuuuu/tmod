using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Decor
{
	// ====================================================================================
	// Boss 旗帜（可制作的 1x2 悬挂装饰）
	//
	// 配方：对应 Boss 的材料 x1 + 丝线 x3，在**织布机**（TileID.Loom）上缝。
	// 选材料而不是选奖杯：奖杯本身是 10% 掉落，把旗帜挂在它后面会让"想挂旗"变成看脸；
	// 材料来自掉落袋（保底），打完 Boss 一定凑得齐。
	// 只要 1 份材料是刻意的：清道夫的掉落袋只给 1~2 份清道夫残骸
	// （归档者 / 灰烬之心 / 壁炉守卫分别是 8~14 / 10~16 / 12~18），
	// 要 5 份就得把清道夫再打三遍 —— 一件装饰品不该有这个门槛。
	//
	// ⚠️ 物品介绍里**不许**写制作方法（工作台那句在合成界面里已经有了，见
	//    tools\check_tooltip_no_crafting.py），所以 Tooltip 只写"这是什么"。
	// ====================================================================================

	/// <summary>清道夫旗帜。</summary>
	public class ScavengerBanner : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Content.Tiles.ScavengerBanner>();
			Item.placeStyle = 0;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ScavengerFragment>(1)
				.AddIngredient(ItemID.Silk, 3)
				.AddTile(TileID.Loom)
				.Register();
		}
	}

	/// <summary>归档者旗帜。</summary>
	public class ArchivistBanner : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Content.Tiles.ArchivistBanner>();
			Item.placeStyle = 0;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ArchivistFragment>(1)
				.AddIngredient(ItemID.Silk, 3)
				.AddTile(TileID.Loom)
				.Register();
		}
	}

	/// <summary>灰烬之心旗帜。</summary>
	public class AshHeartBanner : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Content.Tiles.AshHeartBanner>();
			Item.placeStyle = 0;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshHeartFragment>(1)
				.AddIngredient(ItemID.Silk, 3)
				.AddTile(TileID.Loom)
				.Register();
		}
	}

	/// <summary>壁炉守卫旗帜。</summary>
	public class FireplaceGuardianBanner : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Content.Tiles.FireplaceGuardianBanner>();
			Item.placeStyle = 0;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<FireplaceFragment>(1)
				.AddIngredient(ItemID.Silk, 3)
				.AddTile(TileID.Loom)
				.Register();
		}
	}
}
