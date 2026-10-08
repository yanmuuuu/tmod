using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Items.Decor
{
	// ====================================================================================
	// Boss 奖杯（装饰物）
	//
	// 掉落：对应 Boss 的 `ModifyNPCLoot` 里 `ItemDropRule.Common(<奖杯>, 10)` = 10%。
	// 迷你 Boss「废料收割者」同样 10%（它没有掉落袋，战利品直接进背包）。
	// 放下去是 3x3 图块（`Content/Tiles/<类名>.png`，54x48），挖掉返还自己。
	//
	// 说明：
	//   * 物品图标 32x32，与图块**同一块素材**分别缩放得到（见 tools\apply_boss_art.py）；
	//   * `Item.createTile` 用 `Content.Tiles.<类名>` 限定写法 —— 奖杯的图块类与物品类**同名**，
	//     同命名空间的名字优先，所以必须限定到 Tiles 命名空间（和 ScavengerBag 的写法一致）。
	// ====================================================================================

	/// <summary>清道夫奖杯：机械头骨。</summary>
	public class ScavengerTrophy : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Content.Tiles.ScavengerTrophy>();
			Item.placeStyle = 0;
		}
	}

	/// <summary>归档者奖杯：审计面具。</summary>
	public class ArchivistTrophy : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Content.Tiles.ArchivistTrophy>();
			Item.placeStyle = 0;
		}
	}

	/// <summary>灰烬之心奖杯：燃烧心脏。</summary>
	public class AshHeartTrophy : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Content.Tiles.AshHeartTrophy>();
			Item.placeStyle = 0;
		}
	}

	/// <summary>壁炉守卫奖杯：壁炉金属门。</summary>
	public class FireplaceGuardianTrophy : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Content.Tiles.FireplaceGuardianTrophy>();
			Item.placeStyle = 0;
		}
	}

	/// <summary>迷你 Boss「废料收割者」奖杯：锈爪齿轮。</summary>
	public class ScrapReaperTrophy : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Content.Tiles.ScrapReaperTrophy>();
			Item.placeStyle = 0;
		}
	}
}
