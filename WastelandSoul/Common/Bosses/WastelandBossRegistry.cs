using System.Collections.Generic;
using Terraria.ModLoader;
using WastelandSoul.Content.NPCs.Bosses.Archivist;
using WastelandSoul.Content.NPCs.Bosses.AshHeart;
using WastelandSoul.Content.NPCs.Bosses.FireplaceGuardian;
using WastelandSoul.Content.NPCs.Bosses.Scavenger;

namespace WastelandSoul.Common.Bosses
{
	/// <summary>
	/// 一个 Boss 在剧情与进度里的定位。
	/// </summary>
	public class WastelandBossEntry
	{
		/// <summary>第几个 Boss（1 起，与灵魂碎片的序号一致）。</summary>
		public int Index;

		/// <summary>内部代号（BossChecklist 用，只能是字母数字）。</summary>
		public string InternalName;

		/// <summary>BossChecklist 进度值（参考原版：史莱姆王≈0.5、骷髅王≈3、世纪之花≈10、月亮领主≈14.5）。</summary>
		public float Progression;

		/// <summary>设计定位（中文，给人看的说明）。</summary>
		public string Theme;

		/// <summary>是否已经实现。</summary>
		public bool Implemented;
	}

	/// <summary>
	/// 四个 Boss 的定位表——**整个模组的进度骨架**。
	/// <para/>跨度：壁炉守卫是月亮领主之前的最后一个 Boss，四个 Boss 全部铺在它之前。
	/// <para/>新增一个 Boss 时：在这里加一行 + 写 NPC + 在 <see cref="ResolveNpcType"/> 里加一条分支即可；
	/// BossChecklist 登记、灵魂碎片索引都会自动跟上。
	/// </summary>
	public static class WastelandBossRegistry
	{
		public static readonly IReadOnlyList<WastelandBossEntry> All = new List<WastelandBossEntry> {
			new WastelandBossEntry {
				Index = 1,
				InternalName = "Scavenger",
				Progression = 0.5f,
				Implemented = true,
				Theme = "废土杀戮机械；壁炉保护清单上的「清除程序」。开局引导目标。"
			},
			new WastelandBossEntry {
				Index = 2,
				InternalName = "Archivist",
				Progression = 3.5f,
				Implemented = true,
				Theme = "「守望者计划」的审计单元，专门回收原型机外泄的记忆——它的存在解释了智械人的数据为何开始不稳定。"
			},
			new WastelandBossEntry {
				Index = 3,
				InternalName = "AshHeart",
				Progression = 10.5f,
				Implemented = true,
				Theme = "旧时代战争留下的自持污染源；击败它才会揭明「世界灭亡不可逆」，以及智械人当年是执行者之一。"
			},
			new WastelandBossEntry {
				Index = 4,
				InternalName = "FireplaceGuardian",
				Progression = 14f,
				Implemented = true,
				Theme = "壁炉的最后防线，守着「世界重置」装置；击败后智械人必须决定是否成为重置的载体。月亮领主前最后一个原创 Boss。"
			}
		};

		/// <summary>
		/// 代号 → NPC 类型。未实现的返回 0。
		/// <para/>注意：必须在内容加载完成后调用（不要在静态字段初始化里调用 ModContent）。
		/// </summary>
		public static int ResolveNpcType(string internalName)
		{
			switch (internalName) {
				case "Scavenger":
					return ModContent.NPCType<Scavenger>();
				case "Archivist":
					return ModContent.NPCType<Archivist>();
				case "AshHeart":
					return ModContent.NPCType<AshHeart>();
				case "FireplaceGuardian":
					return ModContent.NPCType<FireplaceGuardian>();
				default:
					return 0;
			}
		}

		/// <summary>取某个序号对应的灵魂碎片类型（0 表示还没有）。</summary>
		public static int ResolveSoulFragmentType(int bossIndex)
		{
			switch (bossIndex) {
				case 1:
					return ModContent.ItemType<Content.Items.Soul.SoulFragmentScavenger>();
				case 2:
					return ModContent.ItemType<Content.Items.Soul.SoulFragmentSecond>();
				case 3:
					return ModContent.ItemType<Content.Items.Soul.SoulFragmentThird>();
				case 4:
					return ModContent.ItemType<Content.Items.Soul.SoulFragmentFourth>();
				default:
					return 0;
			}
		}

		/// <summary>该 Boss 的召唤物物品类型（0 = 没有）。BossChecklist 的清单条目要用。</summary>
		public static int ResolveSummonItemType(string internalName)
		{
			switch (internalName) {
				case "Scavenger":
					return ModContent.ItemType<Content.Items.Summons.ScavengerSignalSensor>();
				case "Archivist":
					return ModContent.ItemType<Content.Items.Summons.ArchivistEcho>();
				case "AshHeart":
					return ModContent.ItemType<Content.Items.Summons.AshHeartEmber>();
				case "FireplaceGuardian":
					return ModContent.ItemType<Content.Items.Summons.FireplaceKey>();
				default:
					return 0;
			}
		}

		/// <summary>该 Boss 的掉落袋物品类型（0 = 没有）。</summary>
		public static int ResolveBagItemType(string internalName)
		{
			switch (internalName) {
				case "Scavenger":
					return ModContent.ItemType<Content.Items.Bags.ScavengerBag>();
				case "Archivist":
					return ModContent.ItemType<Content.Items.Bags.ArchivistBag>();
				case "AshHeart":
					return ModContent.ItemType<Content.Items.Bags.AshHeartBag>();
				case "FireplaceGuardian":
					return ModContent.ItemType<Content.Items.Bags.FireplaceBag>();
				default:
					return 0;
			}
		}

		/// <summary>
		/// 该 Boss 的奖杯物品类型（0 = 没有）。BossChecklist 的收集项要用。
		/// <para/>奖杯是 10% 掉落，掉落规则写在各自的 <c>ModifyNPCLoot</c> 里
		/// （清道夫那条挂在"被玩家正常击败"的条件上，自毁不给）。
		/// </summary>
		public static int ResolveTrophyItemType(string internalName)
		{
			switch (internalName) {
				case "Scavenger":
					return ModContent.ItemType<Content.Items.Decor.ScavengerTrophy>();
				case "Archivist":
					return ModContent.ItemType<Content.Items.Decor.ArchivistTrophy>();
				case "AshHeart":
					return ModContent.ItemType<Content.Items.Decor.AshHeartTrophy>();
				case "FireplaceGuardian":
					return ModContent.ItemType<Content.Items.Decor.FireplaceGuardianTrophy>();
				default:
					return 0;
			}
		}

		/// <summary>
		/// 该 Boss 的旗帜物品类型（0 = 没有）。旗帜是**可制作**的装饰
		/// （Boss 材料 x5 + 丝线 x3，织布机），也一起进 BossChecklist 的收集项。
		/// </summary>
		public static int ResolveBannerItemType(string internalName)
		{
			switch (internalName) {
				case "Scavenger":
					return ModContent.ItemType<Content.Items.Decor.ScavengerBanner>();
				case "Archivist":
					return ModContent.ItemType<Content.Items.Decor.ArchivistBanner>();
				case "AshHeart":
					return ModContent.ItemType<Content.Items.Decor.AshHeartBanner>();
				case "FireplaceGuardian":
					return ModContent.ItemType<Content.Items.Decor.FireplaceGuardianBanner>();
				default:
					return 0;
			}
		}
	}
}
