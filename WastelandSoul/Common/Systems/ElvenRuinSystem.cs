using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Content.Tiles;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 世界生成时把「精灵族遗迹」安置在出生点附近的地表。
	/// <para/>遗迹 = 一小片平整的大理石平台 + 几根断裂石柱，中央立着「精灵族躯体」。
	/// </summary>
	public class ElvenRuinSystem : ModSystem
	{
		private const int RuinWidth = 26;
		private const int ClearHeight = 10;

		/// <summary>
		/// 在世界生成全部结束后安置遗迹。
		/// <para/>用 PostWorldGen 而不是插入 GenPass：既不必依赖 PassLegacy，
		/// 也保证此时 Main.spawnTileX 已经确定。
		/// </summary>
		public override void PostWorldGen()
		{
			GenerateRuin();
		}

		private static void GenerateRuin()
		{
			for (int attempt = 0; attempt < 80; attempt++) {
				int direction = attempt % 2 == 0 ? 1 : -1;
				int baseX = Main.spawnTileX + direction * (38 + attempt * 3);

				if (baseX < 80 || baseX > Main.maxTilesX - 80 - RuinWidth) {
					continue;
				}

				int minY = int.MaxValue;
				int maxY = 0;
				bool valid = true;

				for (int dx = 0; dx <= RuinWidth; dx += 2) {
					int surface = SurfaceYAt(baseX + dx);

					if (surface < 0) {
						valid = false;
						break;
					}

					minY = Math.Min(minY, surface);
					maxY = Math.Max(maxY, surface);
				}

				// 需要一段足够平的地表，否则平台会歪
				if (!valid || maxY - minY > 5) {
					continue;
				}

				BuildRuin(baseX, maxY);
				return;
			}
		}

		private static int SurfaceYAt(int x)
		{
			for (int y = 60; y < Main.maxTilesY - 200; y++) {
				if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType] && !Main.tileSolidTop[Main.tile[x, y].TileType]) {
					return y;
				}
			}

			return -1;
		}

		private static void BuildRuin(int baseX, int floorY)
		{
			// 1) 清出空地
			for (int dx = 0; dx < RuinWidth; dx++) {
				for (int dy = 1; dy <= ClearHeight; dy++) {
					WorldGen.KillTile(baseX + dx, floorY - dy, false, false, true);
					Main.tile[baseX + dx, floorY - dy].WallType = 0;
				}
			}

			// 2) 铺两层大理石地面
			for (int dx = 0; dx < RuinWidth; dx++) {
				for (int dy = 0; dy < 2; dy++) {
					WorldGen.PlaceTile(baseX + dx, floorY + dy, TileID.Marble, mute: true, forced: true);
				}
			}

			// 3) 断裂石柱：高低不一，显得残破
			int[] pillarX = { 1, 6, RuinWidth - 7, RuinWidth - 2 };

			for (int i = 0; i < pillarX.Length; i++) {
				int height = 3 + i % 3;

				for (int dy = 1; dy <= height; dy++) {
					WorldGen.PlaceTile(baseX + pillarX[i], floorY - dy, TileID.Marble, mute: true, forced: true);
				}
			}

			// 4) 中央的精灵族躯体（3x3，立在地面上）
			PlaceFrame(baseX + RuinWidth / 2 - 1, floorY - 3);

			// 5) 让周围重算帧，避免错帧/悬空
			for (int dx = -2; dx < RuinWidth + 2; dx++) {
				for (int dy = -ClearHeight; dy < 4; dy++) {
					WorldGen.SquareTileFrame(baseX + dx, floorY + dy, true);
				}
			}
		}

		private static void PlaceFrame(int originX, int originY)
		{
			// TileObjectData.Style3x3 的 Origin 在底部中间，所以传入「左上级 + (1, 2)」
			WorldGen.PlaceTile(originX + 1, originY + 2, ModContent.TileType<ElvenFrame>(), mute: true, forced: true);
		}
	}
}
