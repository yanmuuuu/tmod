using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace WastelandSoul.Content.Tiles
{
	// ====================================================================================
	// Boss 奖杯（3x3 装饰图块，5 个：4 个 Boss + 迷你 Boss）
	//
	// 共同约定 —— 与工程里既有的 3x3 图块（FireplaceGate / FireplaceTerminal / FireplaceExit /
	// ElvenFrame）**逐字一致**，这样将来写"3x3 图块尺寸"的检查器不会挑出例外：
	//   * 贴图 54x48（`Content/Tiles/<类名>.png`），`TileObjectData.Style3x3` +
	//     `CoordinateHeights = {16,16,16}`；
	//   * 纯装饰：原地不动、不被岩浆烧掉、没有右键行为；挖掉返还自己（tModLoader 自动登记）。
	//   * 地图上的名字走 `Mods.WastelandSoul.Tiles.<类名>.MapEntry`（既有写法）。
	//
	// ⚠️ 尺寸为什么要写清楚：3x3 槽位是「每行 16px + 行间 2px padding」，
	//    第 3 行从贴图 y=36 开始、16px 高要读到 y=52，而贴图只有 48 高 ——
	//    也就是**最后 4px 会被采样钳制**（重复贴图最后一行）。
	//    这批奖杯缩放后最后一行本来就是一条近乎纯色（std < 3）、亮度 7~15 的深色描边，
	//    钳制出来正好是底座下沿的暗边，看不出是钳制：
	//    按取帧规则做的模拟绘制见 `E:\开发\art-inbox\preview_boss_art_game.png`。
	// ====================================================================================

	/// <summary>清道夫奖杯：机械头骨。清道夫 10% 掉落。</summary>
	public class ScavengerTrophy : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(168, 108, 66), Language.GetText("Mods.WastelandSoul.Tiles.ScavengerTrophy.MapEntry"));
			DustType = DustID.Iron;
		}
	}

	/// <summary>归档者奖杯：审计面具。归档者 10% 掉落。</summary>
	public class ArchivistTrophy : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(186, 206, 226), Language.GetText("Mods.WastelandSoul.Tiles.ArchivistTrophy.MapEntry"));
			DustType = DustID.Bone;
		}
	}

	/// <summary>灰烬之心奖杯：燃烧心脏。灰烬之心 10% 掉落。</summary>
	public class AshHeartTrophy : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(226, 96, 54), Language.GetText("Mods.WastelandSoul.Tiles.AshHeartTrophy.MapEntry"));
			DustType = DustID.Torch;
		}
	}

	/// <summary>壁炉守卫奖杯：壁炉金属门。壁炉守卫 10% 掉落。</summary>
	public class FireplaceGuardianTrophy : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(150, 176, 206), Language.GetText("Mods.WastelandSoul.Tiles.FireplaceGuardianTrophy.MapEntry"));
			DustType = DustID.Silver;
		}
	}

	/// <summary>迷你 Boss「废料收割者」奖杯：锈爪齿轮。废料收割者 10% 掉落。</summary>
	public class ScrapReaperTrophy : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(140, 86, 60), Language.GetText("Mods.WastelandSoul.Tiles.ScrapReaperTrophy.MapEntry"));
			DustType = DustID.Iron;
		}
	}
}
