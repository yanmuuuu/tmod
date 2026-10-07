using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using WastelandSoul.Common.Systems;
using WastelandSoul.Content.Items.Story;
using WastelandSoul.Content.NPCs.Town;

namespace WastelandSoul.Content.Tiles
{
	/// <summary>
	/// 精灵族躯体：遗迹中央那具空壳。
	/// <para/>拿着「智械核心」对它右键，智械人就会在此苏醒。
	/// </summary>
	public class ElvenFrame : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;

			// 3x3 的家具有标准锚定与帧处理，省去手写 multi-tile 逻辑
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);

			AddMapEntry(new Color(176, 208, 224), Language.GetText("Mods.WastelandSoul.Tiles.ElvenFrame.MapEntry"));

			DustType = -1;
		}

		public override bool RightClick(int i, int j)
		{
			Player player = Main.LocalPlayer;
			Tile tile = Main.tile[i, j];

			// 换算到多格图块的左上角
			int originX = i - tile.TileFrameX / 18;
			int originY = j - tile.TileFrameY / 18;

			if (WastelandStorySystem.companionAwakened) {
				ShowMessage(player, "Mods.WastelandSoul.Messages.CompanionAlreadyHere", 180, 200, 255);
				return true;
			}

			if (!player.HasItem(ModContent.ItemType<CompanionCore>())) {
				ShowMessage(player, "Mods.WastelandSoul.Messages.FrameNeedsCore", 180, 200, 255);
				return true;
			}

			// 与「直接用智械核心」走同一套逻辑（随机取名 + 到达提示 + 状态位），
			// 区别只是落点在躯体正下方
			int feetY = (originY + 3) * 16;
			Vector2 spot = new Vector2(originX * 16 + 24, feetY - 40);

			if (MechanicalCompanion.TrySummon(player, spot, new EntitySource_SpawnNPC())) {
				// 核心被消耗掉：和物品路径保持一致
				player.ConsumeItem(ModContent.ItemType<CompanionCore>(), true);
			}

			return true;
		}

		private static void ShowMessage(Player player, string key, byte r, byte g, byte b)
		{
			// 联机广播方案待定（见文档「联机暂时搁置」）
			if (!Main.dedServ && player.whoAmI == Main.myPlayer) {
				Main.NewText(Language.GetTextValue(key), r, g, b);
			}
		}
	}
}
