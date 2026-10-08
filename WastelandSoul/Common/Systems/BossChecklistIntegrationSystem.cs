using System;
using System.Collections.Generic;
using Terraria.Localization;
using Terraria.ModLoader;
using WastelandSoul.Common.Bosses;
using WastelandSoul.Common.Configs;
using WastelandSoul.Common.Systems;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.NPCs.Bosses.Scavenger;

namespace WastelandSoul.Common.Systems
{
	/// <summary>
	/// BossChecklist 整合（**软依赖**：没装 BossChecklist 也能正常加载）。
	/// <para/>BossChecklist 2.x 的登记方式是
	/// <c>Call("LogBoss", Mod, 内部名, 进度值, downed回调, NPC列表, 额外信息字典)</c>，
	/// 必须在 AddRecipes 之前调用，所以放在 PostSetupContent。
	/// <para/>额外信息里的字符串用 <c>$</c> 前缀表示"这是个本地化键"。
	/// <para/>登记内容全部来自 <see cref="WastelandBossRegistry"/>：新增 Boss 只要在那张表里加一行。
	/// </summary>
	public class BossChecklistIntegrationSystem : ModSystem
	{
		public override void PostSetupContent()
		{
			if (!ModLoader.TryGetMod("BossChecklist", out Mod bossChecklist)) {
				return;
			}

			foreach (WastelandBossEntry entry in WastelandBossRegistry.All) {
				// 还没实现的 Boss 先不登记：注册表里的 Implemented 是唯一开关，
				// 这样以后加新 Boss 只要先填表、写完 NPC 再翻标志位。
				if (!entry.Implemented) {
					continue;
				}

				int npcType = WastelandBossRegistry.ResolveNpcType(entry.InternalName);

				if (npcType <= 0) {
					continue;
				}

				try {
					LogBoss(bossChecklist, entry, npcType);
				}
				catch (Exception exception) {
					Mod.Logger.Warn($"BossChecklist 登记 {entry.InternalName} 失败：" + exception.Message);
				}
			}
		}

		private void LogBoss(Mod bossChecklist, WastelandBossEntry entry, int npcType)
		{
			// 进度标记回调：按代号取对应的世界剧情标志位（后续 Boss 照这里补一条）
			Func<bool> downed = entry.InternalName switch {
				"Scavenger" => () => WastelandStorySystem.scavengerDefeated,
				"Archivist" => () => WastelandStorySystem.archivistDefeated,
				"AshHeart" => () => WastelandStorySystem.ashHeartDefeated,
				"FireplaceGuardian" => () => WastelandStorySystem.fireplaceGuardianDefeated,
				_ => () => false
			};

			// ⚠️ 这几个键名与类型是**逐行核过** BossChecklist 1.4.4 分支的 EntryInfo.cs 得出的
			// （那条分支的 mod-call API 版本号是 v2.0.0，LogBoss 就是它的新接口）：
			//   * displayName / spawnInfo 必须是 LocalizedText —— 传 `"$键名"` 字符串会被**静默忽略**，
			//     然后退回"自动注册"，结果召唤说明会显示成 "Spawn conditions unknown"；
			//   * 收集项键名是 collectibles（List<int>）；
			//   * 召唤物键名是 spawnItems（**复数**，List<int>|int）；
			//   * **没有** bossBag 这种键：掉落袋只要放进 collectibles，BossChecklist 自己会认出宝物袋。
			LocalizedText displayName = Language.GetText("Mods.WastelandSoul.NPCs." + entry.InternalName + ".DisplayName");
			LocalizedText spawnInfo = Language.GetText("Mods.WastelandSoul.BossChecklist." + entry.InternalName + ".SpawnInfo");

			// 清单收集项：掉落袋 + 对应的灵魂碎片 + 奖杯 + 旗帜
			// （奖杯是 10% 掉落，旗帜用 Boss 材料在织布机上缝 —— 两个都算「这件战利品拿到了吗」）
			List<int> collectibles = new List<int>();
			List<int> spawnItems = new List<int>();

			int summonItem = WastelandBossRegistry.ResolveSummonItemType(entry.InternalName);

			if (summonItem > 0) {
				spawnItems.Add(summonItem);
			}

			int bagItem = WastelandBossRegistry.ResolveBagItemType(entry.InternalName);

			if (bagItem > 0) {
				collectibles.Add(bagItem);
			}

			int soulFragment = WastelandBossRegistry.ResolveSoulFragmentType(entry.Index);

			if (soulFragment > 0) {
				collectibles.Add(soulFragment);
			}

			int trophyItem = WastelandBossRegistry.ResolveTrophyItemType(entry.InternalName);

			if (trophyItem > 0) {
				collectibles.Add(trophyItem);
			}

			int bannerItem = WastelandBossRegistry.ResolveBannerItemType(entry.InternalName);

			if (bannerItem > 0) {
				collectibles.Add(bannerItem);
			}

			Dictionary<string, object> extra = new Dictionary<string, object> {
				{ "displayName", displayName },
				{ "spawnInfo", spawnInfo }
			};

			if (spawnItems.Count > 0) {
				extra["spawnItems"] = spawnItems;
			}

			if (collectibles.Count > 0) {
				extra["collectibles"] = collectibles;
			}

			bossChecklist.Call(
				"LogBoss",
				Mod,
				entry.InternalName,
				entry.Progression,
				downed,
				new List<int> { npcType },
				extra);
		}
	}
}
