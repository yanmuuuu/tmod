using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace TexProbe
{
	/// <summary>
	/// 只做一件事：把原版 Terraria 的 `Projectile_*.xnb` 读成宽高，供模组声明
	/// `Projectile.width/height` 时与**原版贴图尺寸**对齐（复用原版贴图时必须一致，
	/// 否则 tModLoader 会报尺寸不匹配）。
	/// <para/>用 FNA 自己的 `TextureDataFromStreamEXT` 解码：它就是 `Texture2D.FromStream`
	/// 内部真正干活的那一步，**不需要 GraphicsDevice**，所以在纯控制台里也能跑。
	/// </summary>
	internal static class Program
	{
		private const string VanillaImages = @"E:\steam\steamapps\common\Terraria\Content\Images";

		private static int Main(string[] args)
		{
			string prefix = "Projectile";
			int start = 0;

			if (args.Length > 0 && args[0] == "--prefix") {
				prefix = args[1];
				start = 2;
			}

			for (int i = start; i < args.Length; i++) {
				string name = prefix + "_" + args[i] + ".xnb";
				string path = Path.Combine(VanillaImages, name);

				if (!File.Exists(path)) {
					Console.WriteLine("{0,-6} MISSING", args[i]);
					continue;
				}

				try {
					int width = 0;
					int height = 0;
					byte[] pixels = null;

					using (FileStream stream = File.OpenRead(path)) {
						Texture2D.TextureDataFromStreamEXT(stream, ref width, ref height, ref pixels, 0, 0, false);
					}

					Console.WriteLine("{0,-6} {1}x{2}", args[i], width, height);
				}
				catch (Exception error) {
					Console.WriteLine("{0,-6} ERROR {1}: {2}", args[i], error.GetType().Name, error.Message);
				}
			}

			return 0;
		}
	}
}
