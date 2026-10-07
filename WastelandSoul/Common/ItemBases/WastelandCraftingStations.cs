using Terraria.ID;

namespace WastelandSoul.Common.ItemBases
{
	/// <summary>
	/// 制作途径（工作站点）规定：**按时期划分**，与四个 Boss 的定位一一对应。
	/// <list type="bullet">
	/// <item>**召唤物**：一律在**恶魔祭坛 / 猩红祭坛**上合成（原版 Boss 召唤物的传统做法）</item>
	/// <item>**装备与可制作武器**：前期（史莱姆王 ~ 骷髅王）用**铁砧**；血肉墙之后用**秘银砧**</item>
	/// <item>**Boss 材料熔炼**：前期用**熔炉**；血肉墙之后用**精金熔炉**</item>
	/// </list>
	/// 说明：原版里 铁砧/铅砧、秘银砧/山铜砧、精金熔炉/钛金熔炉、恶魔祭坛/猩红祭坛
	/// 分别共用同一个 TileID，所以这里各写一个常量即可覆盖两个世界邪恶/矿物分支。
	/// </summary>
	public static class WastelandCraftingStations
	{
		/// <summary>召唤物站点：恶魔祭坛 / 猩红祭坛。</summary>
		public const int SummonAltar = TileID.DemonAltar;

		/// <summary>前期装备站点：铁砧（含铅砧）。</summary>
		public const int EarlyAnvil = TileID.Anvils;

		/// <summary>血肉墙之后装备站点：秘银砧（含山铜砧）。</summary>
		public const int HardmodeAnvil = TileID.MythrilAnvil;

		/// <summary>前期材料熔炼：熔炉。</summary>
		public const int EarlyForge = TileID.Furnaces;

		/// <summary>血肉墙之后材料熔炼：精金熔炉（含钛金熔炉）。</summary>
		public const int HardmodeForge = TileID.AdamantiteForge;
	}
}
