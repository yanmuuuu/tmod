using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;

namespace WastelandSoul.Content.Items.Bags
{
	// ====================================================================================
	// 四个 Boss 的掉落袋。打开时保证：材料给到保底数量 + 灵魂碎片必定掉落 + 随机开出一把专属武器。
	// 专属武器池由基类按「子目录 + EX 后缀」自动收集，见 WastelandBossBag.CollectExclusiveWeapons。
	//
	// 命名空间必须与文件夹一致（tModLoader 按命名空间找贴图），
	// 所以这里的类贴图都在 Content\Items\Bags\ 下。
	// ====================================================================================

	/// <summary>Boss 1 · 清道夫：精钢碎块 20~30 + 清道夫残片 1~2 + 灵魂碎片·其一 + 清道夫·专属武器。</summary>
	public class ScavengerBag : WastelandBossBag
	{
		protected override int BossIndex => 1;

		protected override List<(int Type, int Min, int Max)> Materials => new List<(int, int, int)> {
			(ModContent.ItemType<SalvagedSteelChunk>(), 20, 30),
			(ModContent.ItemType<ScavengerFragment>(), 1, 2)
		};

		protected override int[] PrefixPool => WastelandPrefixTiers.Early;
	}

	/// <summary>Boss 2 · 归档者：归档者残响 8~14 + 骨头 15~25 + 灵魂碎片·其二 + 归档者·专属武器。</summary>
	public class ArchivistBag : WastelandBossBag
	{
		protected override int BossIndex => 2;

		protected override List<(int Type, int Min, int Max)> Materials => new List<(int, int, int)> {
			(ModContent.ItemType<ArchivistFragment>(), 8, 14),
			(ItemID.Bone, 15, 25)
		};

		protected override int[] PrefixPool => WastelandPrefixTiers.Early;
	}

	/// <summary>Boss 3 · 灰烬之心：灰烬之核 10~16 + 灵质 2~4 + 灵魂碎片·其三 + 灰烬之心·专属武器。</summary>
	public class AshHeartBag : WastelandBossBag
	{
		protected override int BossIndex => 3;

		protected override List<(int Type, int Min, int Max)> Materials => new List<(int, int, int)> {
			(ModContent.ItemType<AshHeartFragment>(), 10, 16),
			(ItemID.Ectoplasm, 2, 4)
		};

		protected override int[] PrefixPool => WastelandPrefixTiers.Mid;
	}

	/// <summary>Boss 4 · 壁炉守卫：壁炉残骸 12~18 + 壁炉合金锭 4~8 + 灵魂碎片·其四 + 壁炉守卫·专属武器。</summary>
	public class FireplaceBag : WastelandBossBag
	{
		protected override int BossIndex => 4;

		protected override List<(int Type, int Min, int Max)> Materials => new List<(int, int, int)> {
			(ModContent.ItemType<FireplaceFragment>(), 12, 18),
			(ModContent.ItemType<FireplaceAlloyBar>(), 4, 8)
		};

		protected override int[] PrefixPool => WastelandPrefixTiers.Late;
	}
}
