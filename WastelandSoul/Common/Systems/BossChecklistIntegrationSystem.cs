using System;
using System.Collections.Generic;
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
				// 还没实现的 Boss 先不登记
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

			string displayNameKey = "$Mods.WastelandSoul.NPCs." + entry.InternalName + ".DisplayName";
			string spawnInfoKey = "$Mods.WastelandSoul.BossChecklist." + entry.InternalName + ".SpawnInfo";

			bossChecklist.Call(
				"LogBoss",
				Mod,
				entry.InternalName,
				entry.Progression,
				downed,
				new List<int> { npcType },
				new Dictionary<string, object> {
					{ "displayName", displayNameKey },
					{ "spawnInfo", spawnInfoKey }
				});
		}
	}
}
