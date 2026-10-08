using Terraria.ID;
using WastelandSoul.Common.Bosses;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Summons
{
	// ====================================================================================
	// 召唤物扩充（3 个）。全部派生自 WastelandSummonItem：
	//   * 使用后**不消耗**；
	//   * 同一时间只允许存在一只对应的 Boss（基类统一判定并给出提示）；
	//   * 生成点在玩家斜上方 520~760 距离处。
	// 指向的 Boss 都用 WastelandBossRegistry.ResolveNpcType("代号") 解析，
	// 这样 Boss 本体没做/没启用的时候不会编译失败，而是给玩家一句提示。
	// ⚠️ 物品介绍里不许写制作方法。
	// ====================================================================================

	/// <summary>
	/// 猎杀信标：把清道夫的"清除程序"重新点亮一次，它会顺着信标找过来。
	/// <para/>定位：Boss 1（<c>Scavenger</c>）的**早期可重复召唤物**——
	/// 原版的入侵周期是 5 天一次，等不起的玩家可以用这个直接开。
	/// </summary>
	public class ScavengerBeacon : WastelandSummonItem
	{
		protected override int BossType => WastelandBossRegistry.ResolveNpcType("Scavenger");

		protected override float SpawnHeight => -320f;

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RustedGear>(8)
				.AddIngredient<CircuitBoard>(3)
				.AddIngredient(ItemID.DemoniteBar, 5)
				.AddTile(WastelandCraftingStations.SummonAltar)
				.Register();

			// 猩红世界用猩红矿，走同一条配方
			CreateRecipe()
				.AddIngredient<RustedGear>(8)
				.AddIngredient<CircuitBoard>(3)
				.AddIngredient(ItemID.CrimtaneBar, 5)
				.AddTile(WastelandCraftingStations.SummonAltar)
				.Register();
		}
	}

	/// <summary>
	/// 审计申请单：一张填好的、要求"复核原型机记忆"的申请书。
	/// <para/>定位：Boss 2（<c>Archivist</c>）的**替代召唤路径**，
	/// 比原来的「归档者回响」便宜一点，但需要旧世界电路板（要打过索引蛾）。
	/// </summary>
	public class AuditRequest : WastelandSummonItem
	{
		protected override int BossType => WastelandBossRegistry.ResolveNpcType("Archivist");

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ArchivistFragment>(6)
				.AddIngredient<CircuitBoard>(4)
				.AddIngredient(ItemID.Book, 2)
				.AddIngredient(ItemID.Bone, 8)
				.AddTile(WastelandCraftingStations.SummonAltar)
				.Register();
		}
	}

	/// <summary>
	/// 余烬引信：一根还在阴燃的引信，插进灰里就能把灰烬之心重新叫醒。
	/// <para/>定位：Boss 3（<c>AshHeart</c>）的**替代召唤路径**，
	/// 材料偏消耗品（焦炭 + 灰烬结晶），适合刷材料时反复用。
	/// </summary>
	public class EmberFuse : WastelandSummonItem
	{
		protected override int BossType => WastelandBossRegistry.ResolveNpcType("AshHeart");

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshCrystal>(6)
				.AddIngredient<Coke>(10)
				.AddIngredient(ItemID.HellstoneBar, 4)
				.AddIngredient(ItemID.SoulofNight, 3)
				.AddTile(WastelandCraftingStations.SummonAltar)
				.Register();
		}
	}
}
