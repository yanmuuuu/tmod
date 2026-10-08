using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace WastelandSoul.Content.Tiles
{
	// ====================================================================================
	// Boss 旗帜（4 面，1x2 悬挂装饰图块）
	//
	// 槽位按**原版敌人旗帜**的约定做：`TileObjectData.Style1x2Top`
	// （1 格宽 2 格高，挂在上面一格的底面）。贴图 `Content/Tiles/<类名>.png` = 18x34：
	//   宽 = 16 + 2(padding)， 高 = 16 + 2(padding) + 16 —— 与 `CoordinateHeights = {16,16}`
	//   精确对齐，**不会**像 3x3 奖杯那样出现底部采样钳制。
	//
	// 获取方式：用对应 Boss 掉的材料 + 丝线在**织布机**上缝出来（配方在合成界面里看得见，
	// 所以物品介绍里不写制作方法）。
	// ====================================================================================

	/// <summary>清道夫旗帜。</summary>
	public class ScavengerBanner : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(168, 108, 66), Language.GetText("Mods.WastelandSoul.Tiles.ScavengerBanner.MapEntry"));
			DustType = DustID.Smoke;
		}
	}

	/// <summary>归档者旗帜。</summary>
	public class ArchivistBanner : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(186, 206, 226), Language.GetText("Mods.WastelandSoul.Tiles.ArchivistBanner.MapEntry"));
			DustType = DustID.Smoke;
		}
	}

	/// <summary>灰烬之心旗帜。</summary>
	public class AshHeartBanner : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(226, 96, 54), Language.GetText("Mods.WastelandSoul.Tiles.AshHeartBanner.MapEntry"));
			DustType = DustID.Smoke;
		}
	}

	/// <summary>壁炉守卫旗帜。</summary>
	public class FireplaceGuardianBanner : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(150, 176, 206), Language.GetText("Mods.WastelandSoul.Tiles.FireplaceGuardianBanner.MapEntry"));
			DustType = DustID.Smoke;
		}
	}
}
