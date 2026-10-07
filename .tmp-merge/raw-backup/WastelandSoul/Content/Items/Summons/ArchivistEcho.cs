using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.NPCs.Bosses.Archivist;

namespace WastelandSoul.Content.Items.Summons
{
	/// <summary>
	/// 归档者残响：在**恶魔祭坛 / 猩红祭坛**用「归档者残响碎片 + 骨头 + 陨石锭」合成。
	/// <para/>把一段伪造的旧日志播出去，让审计单元误以为还有记忆没回收干净——它就会主动过来核对。
	/// <para/>使用后不消耗；同一时间只允许存在一只归档者（由基类统一处理）。
	/// </summary>
	public class ArchivistEcho : WastelandSummonItem
	{
		protected override int BossType => ModContent.NPCType<Archivist>();

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ArchivistFragment>(8)
				.AddIngredient(ItemID.Bone, 5)
				.AddIngredient(ItemID.MeteoriteBar, 3)
				.AddTile(WastelandCraftingStations.SummonAltar)
				.Register();
		}
	}
}
