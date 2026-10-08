using System;
using Terraria;
using Terraria.ModLoader;

namespace WastelandSoul.Content.Subworlds
{
	/// <summary>
	/// 壁炉生成的**步骤日志**。玩家反馈"进门只有一片空世界、日志里却什么都没有"，
	/// 所以每一步都留一条痕：开始 / 结束 + 写入了多少格。
	/// 只要有一步抛异常，日志就会停在那一步的"开始"（异常本身由 tML 记录）。
	/// </summary>
	internal static class FireplaceGenLog
	{
		private static int index;

		public static void Reset()
		{
			index = 0;
			WorldPaint.Writes = 0;
		}

		/// <summary>补一条普通说明（统计数字这类）。</summary>
		public static void Note(string message)
		{
			ModLoader.GetMod("WastelandSoul").Logger.Info("[壁炉生成] " + message);
		}

		public static void Start(string name)
		{
			index++;
			ModLoader.GetMod("WastelandSoul").Logger.Info(string.Format(
				"[壁炉生成] 步骤 {0} {1} 开始：世界 {2}x{3}（布局期望 {4}x{5}）",
				index, name, Main.maxTilesX, Main.maxTilesY, FireplaceLayout.Width, FireplaceLayout.Height));
		}

		public static void Done(string name)
		{
			ModLoader.GetMod("WastelandSoul").Logger.Info(string.Format("[壁炉生成] 步骤 {0} {1} 完成：累计写入 {2} 格",
				index, name, WorldPaint.Writes));
		}
	}
}
