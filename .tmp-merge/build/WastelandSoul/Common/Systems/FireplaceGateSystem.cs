using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Content.Tiles;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// 击败清道夫后，在出生点旁边放下壁炉入口。挖掉了会再放一次，避免主线断掉。
	/// </summary>
	public class FireplaceGateSystem : ModSystem
	{
		public override void PostUpdateWorld()
		{
			if (!WastelandStorySystem.fireplaceOpened || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}

			if (Main.GameUpdateCount % 60u != 0u) {
				return;
			}

			if (GateStillThere()) {
				return;
			}

			if (!TryPlace()) {
				return;
			}

			if (!WastelandStorySystem.GatePlaced) {
				if (Main.netMode != NetmodeID.Server) {
					Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.GatePlaced"), new Color(226, 150, 80));
				}
			}

			WastelandStorySystem.GatePlaced = true;
			WastelandStorySystem.Sync();
		}

		private static bool GateStillThere()
		{
			if (!WastelandStorySystem.GatePlaced) {
				return false;
			}

			int x = WastelandStorySystem.GateX;
			int y = WastelandStorySystem.GateY;

			if (!WorldGen.InWorld(x, y)) {
				return false;
			}

			return Main.tile[x, y].HasTile && Main.tile[x, y].TileType == ModContent.TileType<FireplaceGate>();
		}

		private static bool TryPlace()
		{
			int originX = Main.spawnTileX - 18;
			int floor = SurfaceYAt(originX);

			if (floor < 0) {
				return false;
			}

			for (int dx = -2; dx <= 4; dx++) {
				for (int dy = 1; dy <= 6; dy++) {
					WorldGen.KillTile(originX + dx, floor - dy, false, false, true);
				}
			}

			int placeX = originX + 1;
			int placeY = floor - 1;
			WorldGen.PlaceTile(placeX, placeY, ModContent.TileType<FireplaceGate>(), mute: true, forced: true);
			WastelandStorySystem.GateX = placeX;
			WastelandStorySystem.GateY = placeY;

			if (Main.netMode == NetmodeID.Server) {
				NetMessage.SendTileSquare(-1, placeX, placeY, 5);
			}

			return true;
		}

		private static int SurfaceYAt(int x)
		{
			if (!WorldGen.InWorld(x, 100)) {
				return -1;
			}

			for (int y = 60; y < Main.maxTilesY - 200; y++) {
				if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType] && !Main.tileSolidTop[Main.tile[x, y].TileType]) {
					return y;
				}
			}

			return -1;
		}
	}

	/// <summary>进出壁炉子世界。API 用反射调用，避免前置小版本改了参数个数就编不过。</summary>
	public static class FireplaceTravel
	{
		public static void Enter()
		{
			if (Main.netMode == NetmodeID.Server) {
				return;
			}

			if (!Invoke("Enter")) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.FireplaceEnterFailed"), new Color(220, 160, 90));
				return;
			}

			if (Main.netMode != NetmodeID.Server) {
				Main.NewText(Language.GetTextValue("Mods.WastelandSoul.Messages.EnteredFireplace"), new Color(180, 210, 255));
			}
		}

		public static void Exit()
		{
			if (Main.netMode == NetmodeID.Server) {
				return;
			}

			Invoke("Exit");
		}

		private static bool Invoke(string name)
		{
			System.Type system = typeof(SubworldLibrary.SubworldSystem);
			bool called = false;

			foreach (System.Reflection.MethodInfo method in system.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
				if (method.Name != name) {
					continue;
				}

				System.Reflection.MethodInfo target = method;

				if (method.IsGenericMethodDefinition) {
					System.Reflection.ParameterInfo[] generic = method.GetGenericArguments();

					if (generic.Length != 1) {
						continue;
					}

					target = method.MakeGenericMethod(typeof(Content.Subworlds.FireplaceSubworld));
				}
				else if (name == "Enter") {
					continue;
				}

				System.Reflection.ParameterInfo[] parameters = target.GetParameters();
				object[] args = new object[parameters.Length];

				for (int i = 0; i < parameters.Length; i++) {
					if (parameters[i].HasDefaultValue) {
						args[i] = parameters[i].DefaultValue;
					}
					else if (parameters[i].ParameterType.IsValueType) {
						args[i] = System.Activator.CreateInstance(parameters[i].ParameterType);
					}
					else {
						args[i] = null;
					}
				}

				try {
					target.Invoke(null, args);
					called = true;
					break;
				}
				catch (System.Reflection.TargetInvocationException) {
					continue;
				}
			}

			return called;
		}
	}
}
