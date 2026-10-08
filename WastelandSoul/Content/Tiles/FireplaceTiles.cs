using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using WastelandSoul.Common.Players;
using WastelandSoul.Common.Systems;

namespace WastelandSoul.Content.Tiles
{
	/// <summary>主世界里的壁炉门。右键进入壁炉子世界。</summary>
	public class FireplaceGate : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(226, 140, 70), Language.GetText("Mods.WastelandSoul.Tiles.FireplaceGate.MapEntry"));
			DustType = DustID.Torch;
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.75f;
			g = 0.38f;
			b = 0.12f;
		}

		public override bool RightClick(int i, int j)
		{
			FireplaceTravel.Enter();
			return true;
		}
	}

	/// <summary>壁炉大厅里的旧时代终端。读过之后，智械人才能拼第一段记忆。</summary>
	public class FireplaceTerminal : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(120, 180, 220), Language.GetText("Mods.WastelandSoul.Tiles.FireplaceTerminal.MapEntry"));
			DustType = DustID.Electric;
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.2f;
			g = 0.45f;
			b = 0.7f;
		}

		public override bool RightClick(int i, int j)
		{
			bool alreadyRestored = WastelandStorySystem.firstMemoryRestored;

			WastelandStorySystem.MarkDataTerminalRead();

			if (!Main.dedServ && Main.LocalPlayer.whoAmI == Main.myPlayer && WastelandStorySystem.firstMemoryRestored) {
				// 第一段记忆就是在这一刻由终端放出来的 —— 把「个人进度」三个标志接上。
				WastelandPlayer modPlayer = Main.LocalPlayer.GetModPlayer<WastelandPlayer>();

				modPlayer.heardFirstMemory = true;
				modPlayer.knowsWatchmanProtocol = true;

				if (!alreadyRestored) {
					Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.TerminalTalkToHer"), new Color(160, 200, 230));
				}
			}

			return true;
		}
	}

	/// <summary>壁炉里的返回门。库自己的返回按钮也还能用。</summary>
	public class FireplaceExit : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);
			AddMapEntry(new Color(200, 200, 210), Language.GetText("Mods.WastelandSoul.Tiles.FireplaceExit.MapEntry"));
		}

		public override bool RightClick(int i, int j)
		{
			FireplaceTravel.Exit();
			return true;
		}
	}
}
