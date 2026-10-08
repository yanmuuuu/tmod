using Terraria;
using Terraria.ID;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Materials
{
	/// <summary>
	/// 锈蚀齿轮：废土机械小怪身上拆下来的传动件。
	/// <para/>掉落来源（困难模式之后变多）：锈甲冲锋兽 / 废料跳虫 / 疾风鬼火 / 喷吐蝇 /
	/// 穴居攀爬者 / 灰蜱蝠 / 污染黏体 / 齿轮群母体 / 废料收割者。
	/// <para/>这是**新加的通用材料**，暂时没有参与任何配方 —— 留给后续批次做机械类合成。
	/// </summary>
	public class RustedGear : WastelandMaterial
	{
		protected override int IconSize => 20;

		protected override int Rarity => ItemRarityID.Orange;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 12);
	}
}
