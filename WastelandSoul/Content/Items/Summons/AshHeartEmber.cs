using Terraria.ID;
using WastelandSoul.Common.Bosses;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Summons
{
	/// <summary>
	/// 灰烬之心余烬：在**恶魔祭坛 / 猩红祭坛**用「灰烬之心碎片 + 灵质 + 狱石锭」合成。
	/// <para/>往一簇还在阴燃的余烬里吹一口气——它会顺着旧时代的战争信道找回来。
	/// <para/>使用后不消耗；同一时间只允许存在一只（由基类统一处理）。
	/// <para/>⚠️ Boss 3 的 NPC 本体还没做，所以 BossType 现在是 0（用了没有任何反应）。
	/// 等 <c>AshHeart.cs</c> 写出来、注册表里 <c>Implemented</c> 改 true 之后，这里会自动生效。
	/// </summary>
	public class AshHeartEmber : WastelandSummonItem
	{
		protected override int BossType => WastelandBossRegistry.ResolveNpcType("AshHeart");

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<AshHeartFragment>(10)
				.AddIngredient(ItemID.Ectoplasm, 2)
				.AddIngredient(ItemID.HellstoneBar, 5)
				.AddTile(WastelandCraftingStations.SummonAltar)
				.Register();
		}
	}
}
