using Terraria.ID;
using WastelandSoul.Common.Bosses;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Summons
{
	/// <summary>
	/// 壁炉守卫密钥：在**恶魔祭坛 / 猩红祭坛**用「壁炉残骸 + 壁炉合金锭 + 天界符」合成。
	/// <para/>把最后一道防线自己的识别码还给它——它认得出这把钥匙，也认得出拿着钥匙的人。
	/// <para/>使用后不消耗；同一时间只允许存在一只（由基类统一处理）。
	/// <para/>⚠️ Boss 4 的 NPC 本体还没做，所以 BossType 现在是 0（用了没有任何反应）。
	/// 等 <c>FireplaceGuardian.cs</c> 写出来、注册表里 <c>Implemented</c> 改 true 之后，这里会自动生效。
	/// </summary>
	public class FireplaceKey : WastelandSummonItem
	{
		protected override int BossType => WastelandBossRegistry.ResolveNpcType("FireplaceGuardian");

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<FireplaceFragment>(12)
				.AddIngredient<FireplaceAlloyBar>(4)
				.AddIngredient(ItemID.FragmentSolar, 6)
				.AddTile(WastelandCraftingStations.SummonAltar)
				.Register();
		}
	}
}
