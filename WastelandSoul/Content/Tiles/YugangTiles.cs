using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Tiles
{
	/// <summary>
	/// **瑜钢合金**：壁炉堡垒与祈塔的建材，旧时代把「壁炉」浇铸成一体用的东西。
	///
	/// <para/>观感：接近原版**黑曜石**的哑光块体，但整体偏**青银间色**——
	/// 本体是暗青灰（<see cref="YugangAlloy"/>），每隔一段嵌一条亮银青的**合金压条**
	/// （<see cref="YugangTrim"/>），两者交错铺出"青银相间"的墙面。
	///
	/// <para/>**无法挖取**：<see cref="CanKillTile"/> 直接返回 false
	/// （它同时也让爆炸伤不到它 —— 原版爆炸前会问同一个判定）。
	/// 这是刻意的：设施结构是"旧时代留下的、比你更硬的东西"，也防止玩家把剧情结构拆了。
	/// </summary>
	public class YugangAlloy : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			Main.tileLavaDeath[Type] = false;
			Main.tileMergeDirt[Type] = false;
			Main.tileNoFail[Type] = true;

			TileID.Sets.DoesntPlaceWithTileReplacement[Type] = false;
			TileID.Sets.CanBeClearedDuringGeneration[Type] = false;

			// 200 = 精金 / 钛金镐的稿力，也就是玩家要求的"精金镐以上才能挖"
			MinPick = 200;
			MineResist = 4f;
			HitSound = SoundID.Tink;
			DustType = DustID.Stone;
			AddMapEntry(new Color(88, 172, 178), Language.GetText("Mods.WastelandSoul.Tiles.YugangAlloy.MapEntry"));
		}

		// 本批按玩家要求：不再「完全挖不动」（也不重写 CanKillTile），
		// 而是靠 MinPick = 200 —— 精金 / 钛金镐以上才挖得动。
	}

	/// <summary>瑜钢合金**压条**：与 <see cref="YugangAlloy"/> 交错铺出的亮银青嵌条，同样是精金镐以上可挖。</summary>
	public class YugangTrim : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			Main.tileLavaDeath[Type] = false;
			Main.tileMergeDirt[Type] = false;
			Main.tileNoFail[Type] = true;
			Main.tileLighted[Type] = true;          // 微微自发光，压条在暗处也看得出

			MinPick = 200;           // 同 YusangAlloy：精金镐以上
			MineResist = 4f;
			HitSound = SoundID.Tink;
			DustType = DustID.Silver;
			AddMapEntry(new Color(186, 226, 232), Language.GetText("Mods.WastelandSoul.Tiles.YugangTrim.MapEntry"));
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			// 本批按玩家要求"再亮一些"把压条的自发光抬了一档（0.05/0.11/0.13 → 0.10/0.18/0.21）。
			// 目标是"看得清结构轮廓与房间形状"，不是把墙照成白天 —— 所以仍然远低于火把/宝石微光，
			// 只让青银压条在暗处自己浮出来。
			r = 0.10f;
			g = 0.18f;
			b = 0.21f;
		}


	}
}
