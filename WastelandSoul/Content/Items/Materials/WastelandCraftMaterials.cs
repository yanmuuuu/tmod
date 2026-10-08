using Terraria.ID;
using WastelandSoul.Common.ItemBases;

namespace WastelandSoul.Content.Items.Materials
{
	// ====================================================================================
	// 废土基础材料扩充包（5 种，另加已由别处定义的 RustedGear）。
	// 全部派生自 WastelandMaterial：尺寸 / 堆叠 / 售价 / 稀有度由基类统一处理，
	// 这里只写"差异"（图标边长、稀有度、售价、来源说明）。
	//
	// ⚠️ 约定：**物品介绍里绝对不能写"在什么工作台/祭坛制作"**——那是配方自己的事。
	//         所以下面的 Tooltip 只写"是什么 + 用在什么地方（哪一类成品）"。
	//
	// 掉落来源写在各自动的注释里；实际掉落表由小怪包 / Boss 包负责挂上
	// （本包只提供材料本体，避免去改别人正在改的 NPC 文件）。
	// ⚠️ RustedGear（锈蚀齿轮）由 `Materials/RustedGear.cs` 定义（与本包合并时已去重），
	//    本包只在配方与宝匣掉落里引用它。
	// ====================================================================================

	/// <summary>
	/// 焦炭：把废土上烧剩下的东西再压一遍得到的燃料块，热量不高但非常耐烧。
	/// <para/>用途：熔炼类配方与燃烧系弹药的主要燃料来源。
	/// <para/>来源：任何被火烧过的敌人都有可能掉（灰烬行者、灰烬之心区域的敌人概率更高）。
	/// </summary>
	public class Coke : WastelandMaterial
	{
		protected override int IconSize => 20;

		protected override int Rarity => ItemRarityID.Blue;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 1, copper: 50);
	}

	/// <summary>
	/// 旧世界电路板：保存着旧时代指令的一块板子，铜箔还没完全烂掉。
	/// <para/>用途：归档者相关装备、信息类物品与精密装置。
	/// <para/>来源：索引蛾、归档者的随从，以及壁炉内部的机械残骸。
	/// </summary>
	public class CircuitBoard : WastelandMaterial
	{
		protected override int IconSize => 22;

		protected override int Rarity => ItemRarityID.Green;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 6);
	}

	/// <summary>
	/// 冷却液：密封罐里还剩下半罐，摸上去是凉的。壁炉世界的机器靠它活着。
	/// <para/>用途：热管理相关的护甲、饰品与冷光系弹药。
	/// <para/>来源：维修无人机、壁炉守卫区域的机械敌人。
	/// </summary>
	public class Coolant : WastelandMaterial
	{
		protected override int IconSize => 20;

		protected override int Rarity => ItemRarityID.Green;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 5);
	}

	/// <summary>
	/// 灰烬结晶：污染浓度高到一定程度之后，灰会自己长成晶体。
	/// <para/>用途：灰烬之心时期的武器与药剂（抗污染、再生类）。
	/// <para/>来源：灰烬行者、灰烬之心本体及其随从，以及被污染的灰烬地表。
	/// </summary>
	public class AshCrystal : WastelandMaterial
	{
		protected override int IconSize => 20;

		protected override int Rarity => ItemRarityID.Orange;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 12);
	}

	/// <summary>
	/// 精钢废料束：把精钢碎块重新捆扎而成的半成品，比碎块好带，比合金锭好做。
	/// <para/>用途：精钢套装与过渡期装备的中间材料。
	/// <para/>来源：清道夫本体掉落 / 精钢碎块回收后的产物（由配方链提供，不属于掉落）。
	/// </summary>
	public class SalvagedSteelBundle : WastelandMaterial
	{
		protected override int IconSize => 22;

		protected override int Rarity => ItemRarityID.Green;

		protected override int SellPrice => Terraria.Item.sellPrice(silver: 4);
	}
}
