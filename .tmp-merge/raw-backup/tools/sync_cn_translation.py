"""同步汉化补丁：以主模组的英文文件为准，把中英两份本地化合并成完整的补丁文件。

背景（本轮发现的问题）：
  `E:\\开发\\WastelandSoulCN\\Localization\\zh-Hans_Mods.WastelandSoul.hjson` 里
  **一个中文字符都没有**——所有键都是 tModLoader 自动补的 `// English` 占位注释。
  也就是说"汉化补丁"实际上没有任何汉化，而 `check_tmod_contents.py` 只看字节数
  （>5000 就算通过），所以这个空壳一路绿灯打进了 .tmod。

这个脚本做两件事：
  1. `--sync`：按主模组 en-US 的键结构重建补丁的中文文件。
     有译文的键写译文，没有的键写成 `// English` 占位注释（方便继续补翻译）。
     写完自动刷新备份 E:\\开发\\.backup\\WastelandSoulCN_zh-Hans.hjson。
  2. `--report`：报告还有多少键没翻译。

译文表在下面的 TRANSLATIONS 里；键必须与主模组完全一致（脚本会校验并报出多余的键）。
"""
import io
import os
import re
import shutil
import sys

_TOOLS = os.path.dirname(os.path.abspath(__file__))
_ROOT = os.path.dirname(_TOOLS)
if os.path.isdir(os.path.join(_ROOT, "WastelandSoul")):
    MAIN_LOC = os.path.join(_ROOT, "WastelandSoul", "Localization")
    CN_LOC = os.path.join(_ROOT, "WastelandSoulCN", "Localization")
    CN_BACKUP = os.path.join(_ROOT, ".backup", "WastelandSoulCN_zh-Hans.hjson")
else:
    MAIN_LOC = r"E:\开发\WastelandSoul\Localization"
    CN_LOC = r"E:\开发\WastelandSoulCN\Localization"
    CN_BACKUP = r"E:\开发\.backup\WastelandSoulCN_zh-Hans.hjson"

PREFIX_EN = "Mods.WastelandSoul"

# ====================================================================================
# 译文表（短键名 → 中文）。键名去掉 "Mods.WastelandSoul." 前缀。
# ====================================================================================
TRANSLATIONS = {
    # ---------------- 配置 ----------------
    "WastelandConfig.DisplayName": "废土魂穿 - 设置",
    "WastelandConfig.ScavengerInvasionEnabled.Label": "清道夫定期来袭",
    "WastelandConfig.InvasionCycleDays.Label": "来袭周期（天）",
    "WastelandConfig.InvasionWarning.Label": "显示来袭预警",
    "WastelandConfig.NoLootOnSelfDestruct.Label": "过载自毁后不给掉落",
    "WastelandConfig.NoLootOnSelfDestruct.Tooltip": "清道夫进入过载冲锋并自行解体时，不掉落任何战利品，也不推进剧情",

    # ---------------- BossChecklist ----------------
    "BossChecklist.Scavenger.SpawnInfo": "每 5 天一次（19:30 预警、20:30 抵达），或用「清道夫信号传感器」把它引过来",
    "BossChecklist.Archivist.SpawnInfo": "在恶魔/猩红祭坛合成「归档者残响」（归档者残响碎片 ×8 + 骨头 ×5 + 陨石锭 ×3）并使用，把那台审计单元叫下来",

    # ---------------- NPC ----------------
    "MechanicalCompanion.DisplayName": "智械人",
    "MechanicalCompanion.TownNPCMood.Content": "我的数据流很稳定。",
    "MechanicalCompanion.TownNPCMood.NoHome": "我还没有地方安放这具身体。",
    "MechanicalCompanion.TownNPCMood.FarFromHome": "我们离壁炉太远了。",
    "MechanicalCompanion.TownNPCMood.LoveSpace": "我喜欢这份空旷——没有什么会被误判成污染。",
    "MechanicalCompanion.TownNPCMood.DislikeCrowded": "有点挤。我的传感器被压住了。",
    "MechanicalCompanion.TownNPCMood.HateCrowded": "太吵了。我的程序快要报错了。",
    "MechanicalCompanion.TownNPCMood.LikeBiome": "这里的参数还算合意——{BiomeName}。",
    "MechanicalCompanion.TownNPCMood.DislikeBiome": "{BiomeName}让我想起被污染的地面。",
    "MechanicalCompanion.TownNPCMood.HateBiome": "我讨厌{BiomeName}。我的数据在这里会腐坏。",
    "MechanicalCompanion.TownNPCMood.LoveNPC": "在{NPCName}身边，我的核心频率会稳定下来。",
    "MechanicalCompanion.TownNPCMood.LikeNPC": "{NPCName}还可以接受。",
    "MechanicalCompanion.TownNPCMood.DislikeNPC": "{NPCName}产生的噪音太多了。",
    "MechanicalCompanion.TownNPCMood.HateNPC": "我讨厌{NPCName}。他们的存在让我的传感器过载。",
    "MechanicalCompanion.TownNPCMood.LikeNPC_Princess": "{NPCName}很可爱。",
    "MechanicalCompanion.TownNPCMood.Princess_LovesNPC": "{NPCName}很可爱。",
    # Census 集成：这一条以前是手工补进生成文件里的，每次重新同步都会被冲掉，
    # 所以必须放回译文表（表才是唯一权威）。
    "MechanicalCompanion.Census.SpawnCondition": "出现条件未知",
    "Scavenger.DisplayName": "清道夫",
    "ScavengerRepairDrone.DisplayName": "维修无人机",
    "Archivist.DisplayName": "归档者",

    # ---------------- 灵魂碎片 ----------------
    "SoulFragmentScavenger.DisplayName": "灵魂碎片·其一",
    "SoulFragmentScavenger.Tooltip": "从执行单元-07 上拆下来的一段代码。智械人需要它。",
    "SoulFragmentSecond.DisplayName": "灵魂碎片·其二",
    "SoulFragmentSecond.Tooltip": "归属者尚未确认。它的主人还没被找到。",
    "SoulFragmentThird.DisplayName": "灵魂碎片·其三",
    "SoulFragmentThird.Tooltip": "一簇还记得焚烧指令的余烬。那道指令是智械人写的。",
    "SoulFragmentFourth.DisplayName": "灵魂碎片·其四",
    "SoulFragmentFourth.Tooltip": "重置装置的最后一把锁。里面有她真正的名字。",

    # ---------------- 精钢套装 ----------------
    "SalvagedSteelWarriorHelm.DisplayName": "精钢战士头盔",
    "SalvagedSteelWarriorHelm.Tooltip": "",
    "SalvagedSteelWarriorPlate.DisplayName": "精钢战士胸甲",
    "SalvagedSteelWarriorPlate.SetBonus": "近战伤害 +10%、近战速度 +10%",
    "SalvagedSteelWarriorPlate.Tooltip": "",
    "SalvagedSteelWarriorGreaves.DisplayName": "精钢战士护腿",
    "SalvagedSteelWarriorGreaves.Tooltip": "",
    "SalvagedSteelMageHood.DisplayName": "精钢法师兜帽",
    "SalvagedSteelMageHood.Tooltip": "",
    "SalvagedSteelMageRobe.DisplayName": "精钢法袍",
    "SalvagedSteelMageRobe.SetBonus": "魔法伤害 +10%、魔力消耗 -10%",
    "SalvagedSteelMageRobe.Tooltip": "",
    "SalvagedSteelMageLeggings.DisplayName": "精钢法师护腿",
    "SalvagedSteelMageLeggings.Tooltip": "",
    "SalvagedSteelRangerVisor.DisplayName": "精钢射手目镜",
    "SalvagedSteelRangerVisor.Tooltip": "",
    "SalvagedSteelRangerVest.DisplayName": "精钢射手背心",
    "SalvagedSteelRangerVest.SetBonus": "远程伤害 +10%、20% 几率不消耗弹药",
    "SalvagedSteelRangerVest.Tooltip": "",
    "SalvagedSteelRangerLeggings.DisplayName": "精钢射手护腿",
    "SalvagedSteelRangerLeggings.Tooltip": "",
    "SalvagedSteelSummonerCowl.DisplayName": "精钢召唤师头巾",
    "SalvagedSteelSummonerCowl.Tooltip": "",
    "SalvagedSteelSummonerTunic.DisplayName": "精钢召唤师外衣",
    "SalvagedSteelSummonerTunic.SetBonus": "召唤伤害 +10%、仆从栏位 +1",
    "SalvagedSteelSummonerTunic.Tooltip": "",
    "SalvagedSteelSummonerLeggings.DisplayName": "精钢召唤师护腿",
    "SalvagedSteelSummonerLeggings.Tooltip": "",
    "SalvagedSteelRogueMask.DisplayName": "精钢盗贼面罩",
    "SalvagedSteelRogueMask.Tooltip": "",
    "SalvagedSteelRogueVest.DisplayName": "精钢盗贼背心",
    "SalvagedSteelRogueVest.SetBonus": "投掷伤害 +10%、移动速度 +10%",
    "SalvagedSteelRogueVest.Tooltip": "",
    "SalvagedSteelRogueLeggings.DisplayName": "精钢盗贼护腿",
    "SalvagedSteelRogueLeggings.Tooltip": "",

    # ---------------- 材料 ----------------
    "SalvagedSteelChunk.DisplayName": "精钢碎块",
    "SalvagedSteelChunk.Tooltip": "从清道夫身上撕下来的高强度合金碎片\n需要熔炼成精钢",
    "SalvagedSteelBar.DisplayName": "精钢",
    "SalvagedSteelBar.Tooltip": "旧时代军用级合金\n用来锻造精钢套装",
    "ScavengerFragment.DisplayName": "清道夫残片",
    "ScavengerFragment.Tooltip": "执行单元-07 的核心残骸\n它的一段代码被刻意抹掉了",
    "ArchivistFragment.DisplayName": "归档者残响碎片",
    "ArchivistFragment.Tooltip": "归档者留下的共振碎片\n「守望者计划」派来回收自身记忆的审计单元",
    "AshHeartFragment.DisplayName": "灰烬之心碎片",
    "AshHeartFragment.Tooltip": "从灰烬之心上撕下的自持余烬\n需要精金熔炉的热度才能熔成灰烬合金",
    "AshHeartAlloyBar.DisplayName": "灰烬之心合金锭",
    "AshHeartAlloyBar.Tooltip": "灰烬碎片与旧时代合金熔合而成\n在精金熔炉熔炼",
    "FireplaceFragment.DisplayName": "壁炉残骸",
    "FireplaceFragment.Tooltip": "壁炉最后一道防线的碎片\n需要精金熔炉的热度才能熔成壁炉合金",
    "FireplaceAlloyBar.DisplayName": "壁炉合金锭",
    "FireplaceAlloyBar.Tooltip": "曾包覆壁炉守卫的冷白合金\n在精金熔炉熔炼",

    # ---------------- 召唤物与剧情物品 ----------------
    "ScavengerSignalSensor.DisplayName": "清道夫信号传感器",
    "ScavengerSignalSensor.Tooltip": "向废土广播一段伪造的执行指令，把清道夫引到你身边\n在铁砧用 5 个精钢制作\n使用后不消耗，但同一时间只允许存在一只清道夫",
    "ArchivistEcho.DisplayName": "归档者残响",
    "ArchivistEcho.Tooltip": "播放一段伪造的旧日志残片\n在恶魔/猩红祭坛用 8 个归档者残响碎片 + 5 根骨头 + 3 个陨石锭制作\n使用后不消耗，但同一时间只允许存在一只归档者",
    "AshHeartEmber.DisplayName": "灰烬之心余烬",
    "AshHeartEmber.Tooltip": "往一簇自旧时代战争起就在阴燃的余烬上吹一口气\n在恶魔/猩红祭坛用 10 个灰烬之心碎片 + 2 个灵质 + 5 个狱石锭制作\n使用后不消耗，但同一时间只允许存在一颗灰烬之心",
    "FireplaceKey.DisplayName": "壁炉通行密钥",
    "FireplaceKey.Tooltip": "把最后一道防线自己的识别码还给它\n在恶魔/猩红祭坛用 12 个壁炉残骸 + 4 个壁炉合金锭 + 6 个天界符制作\n使用后不消耗，但同一时间只允许存在一名壁炉守卫",
    "CompanionCore.DisplayName": "智械核心",
    "CompanionCore.Tooltip": "你穿越时间带来的机械核心\n在精灵族遗迹的躯体上使用，唤醒智械人",

    # ---------------- Boss 2 归档者武器 ----------------
    "ArchivistWarriorWeapon.DisplayName": "归档者骨刃",
    "ArchivistWarriorWeapon.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n挥砍时切开弧线内的一切\n在铁砧制作",
    "ArchivistMageWeapon.DisplayName": "归档者索引法典",
    "ArchivistMageWeapon.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n消耗魔力\n在铁砧制作",
    "ArchivistRangerWeapon.DisplayName": "归档者骨制散射枪",
    "ArchivistRangerWeapon.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n消耗子弹\n在铁砧制作",
    "ArchivistSummonerWeapon.DisplayName": "归档者使魔法杖",
    "ArchivistSummonerWeapon.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n召唤一个仆从\n在铁砧制作",
    "ArchivistRogueWeapon.DisplayName": "归档者骨镖",
    "ArchivistRogueWeapon.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n可投掷 - 最多堆叠 999\n在铁砧制作",
    "ArchivistWarriorWeaponEX.DisplayName": "归档者骨刃 MK-II",
    "ArchivistWarriorWeaponEX.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n挥砍时切开弧线内的一切",
    "ArchivistMageWeaponEX.DisplayName": "归档者索引法典 MK-II",
    "ArchivistMageWeaponEX.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n消耗魔力",
    "ArchivistRangerWeaponEX.DisplayName": "归档者骨制散射枪 MK-II",
    "ArchivistRangerWeaponEX.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n消耗子弹",
    "ArchivistSummonerWeaponEX.DisplayName": "归档者使魔法杖 MK-II",
    "ArchivistSummonerWeaponEX.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n召唤一个仆从",
    "ArchivistRogueWeaponEX.DisplayName": "归档者骨镖 MK-II",
    "ArchivistRogueWeaponEX.Tooltip": "归档的骨白与冷蓝光——守望者审计日志的具现\n可投掷 - 最多堆叠 999",

    # ---------------- Boss 1 清道夫武器 ----------------
    "ScavengerWarriorWeapon.DisplayName": "清道夫废料砍刀",
    "ScavengerWarriorWeapon.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n挥砍时切开弧线内的一切\n在铁砧制作",
    "ScavengerMageWeapon.DisplayName": "清道夫电火花棒",
    "ScavengerMageWeapon.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n消耗魔力\n在铁砧制作",
    "ScavengerRangerWeapon.DisplayName": "清道夫废料手枪",
    "ScavengerRangerWeapon.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n消耗子弹\n在铁砧制作",
    "ScavengerSummonerWeapon.DisplayName": "清道夫无人机信标",
    "ScavengerSummonerWeapon.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n召唤一个仆从\n在铁砧制作",
    "ScavengerRogueWeapon.DisplayName": "清道夫废料飞刀",
    "ScavengerRogueWeapon.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n可投掷 - 最多堆叠 999\n在铁砧制作",
    "ScavengerWarriorWeaponEX.DisplayName": "清道夫废料砍刀 MK-II",
    "ScavengerWarriorWeaponEX.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n挥砍时切开弧线内的一切",
    "ScavengerMageWeaponEX.DisplayName": "清道夫电火花棒 MK-II",
    "ScavengerMageWeaponEX.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n消耗魔力",
    "ScavengerRangerWeaponEX.DisplayName": "清道夫废料手枪 MK-II",
    "ScavengerRangerWeaponEX.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n消耗子弹",
    "ScavengerSummonerWeaponEX.DisplayName": "清道夫无人机信标 MK-II",
    "ScavengerSummonerWeaponEX.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n召唤一个仆从",
    "ScavengerRogueWeaponEX.DisplayName": "清道夫废料飞刀 MK-II",
    "ScavengerRogueWeaponEX.Tooltip": "从清道夫残骸里拆出来的——粗糙，但能用\n可投掷 - 最多堆叠 999",

    # ---------------- Boss 3 灰烬之心武器 ----------------
    "AshHeartWarriorWeapon.DisplayName": "灰烬之心大剑",
    "AshHeartWarriorWeapon.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n挥砍时切开弧线内的一切\n在秘银砧制作",
    "AshHeartMageWeapon.DisplayName": "灰烬之心核心法杖",
    "AshHeartMageWeapon.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n消耗魔力\n在秘银砧制作",
    "AshHeartRangerWeapon.DisplayName": "灰烬之心余烬步枪",
    "AshHeartRangerWeapon.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n消耗子弹\n在秘银砧制作",
    "AshHeartSummonerWeapon.DisplayName": "灰烬之心燃烬法杖",
    "AshHeartSummonerWeapon.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n召唤一个仆从\n在秘银砧制作",
    "AshHeartRogueWeapon.DisplayName": "灰烬之心裂片",
    "AshHeartRogueWeapon.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n可投掷 - 最多堆叠 999\n在秘银砧制作",
    "AshHeartWarriorWeaponEx.DisplayName": "灰烬之心大剑 MK-II",
    "AshHeartWarriorWeaponEx.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n挥砍时切开弧线内的一切",
    "AshHeartMageWeaponEx.DisplayName": "灰烬之心核心法杖 MK-II",
    "AshHeartMageWeaponEx.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n消耗魔力",
    "AshHeartRangerWeaponEx.DisplayName": "灰烬之心余烬步枪 MK-II",
    "AshHeartRangerWeaponEx.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n消耗子弹",
    "AshHeartSummonerWeaponEx.DisplayName": "灰烬之心燃烬法杖 MK-II",
    "AshHeartSummonerWeaponEx.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n召唤一个仆从",
    "AshHeartRogueWeaponEx.DisplayName": "灰烬之心裂片 MK-II",
    "AshHeartRogueWeaponEx.Tooltip": "以灰烬之心的余烬锻成——它还在阴燃\n可投掷 - 最多堆叠 999",

    # ---------------- Boss 4 壁炉守卫武器 ----------------
    "FireplaceWarriorWeapon.DisplayName": "壁炉守卫大剑",
    "FireplaceWarriorWeapon.Tooltip": "壁炉级合金。旧世界防线的最后一环\n挥砍时切开弧线内的一切\n在秘银砧制作",
    "FireplaceMageWeapon.DisplayName": "壁炉守卫权杖",
    "FireplaceMageWeapon.Tooltip": "壁炉级合金。旧世界防线的最后一环\n消耗魔力\n在秘银砧制作",
    "FireplaceRangerWeapon.DisplayName": "壁炉守卫步枪",
    "FireplaceRangerWeapon.Tooltip": "壁炉级合金。旧世界防线的最后一环\n消耗子弹\n在秘银砧制作",
    "FireplaceSummonerWeapon.DisplayName": "壁炉守卫信标",
    "FireplaceSummonerWeapon.Tooltip": "壁炉级合金。旧世界防线的最后一环\n召唤一个仆从\n在秘银砧制作",
    "FireplaceRogueWeapon.DisplayName": "壁炉守卫长刃",
    "FireplaceRogueWeapon.Tooltip": "壁炉级合金。旧世界防线的最后一环\n可投掷 - 最多堆叠 999\n在秘银砧制作",
    "FireplaceWarriorWeaponEx.DisplayName": "壁炉守卫大剑 MK-II",
    "FireplaceWarriorWeaponEx.Tooltip": "壁炉级合金。旧世界防线的最后一环\n挥砍时切开弧线内的一切",
    "FireplaceMageWeaponEx.DisplayName": "壁炉守卫权杖 MK-II",
    "FireplaceMageWeaponEx.Tooltip": "壁炉级合金。旧世界防线的最后一环\n消耗魔力",
    "FireplaceRangerWeaponEx.DisplayName": "壁炉守卫步枪 MK-II",
    "FireplaceRangerWeaponEx.Tooltip": "壁炉级合金。旧世界防线的最后一环\n消耗子弹",
    "FireplaceSummonerWeaponEx.DisplayName": "壁炉守卫信标 MK-II",
    "FireplaceSummonerWeaponEx.Tooltip": "壁炉级合金。旧世界防线的最后一环\n召唤一个仆从",
    "FireplaceRogueWeaponEx.DisplayName": "壁炉守卫长刃 MK-II",
    "FireplaceRogueWeaponEx.Tooltip": "壁炉级合金。旧世界防线的最后一环\n可投掷 - 最多堆叠 999",

    # ---------------- 掉落袋 ----------------
    "ScavengerBag.DisplayName": "清道夫掉落袋",
    "ScavengerBag.Tooltip": "",
    "ArchivistBag.DisplayName": "归档者掉落袋",
    "ArchivistBag.Tooltip": "",
    "AshHeartBag.DisplayName": "灰烬之心掉落袋",
    "AshHeartBag.Tooltip": "",
    "FireplaceBag.DisplayName": "壁炉守卫掉落袋",
    "FireplaceBag.Tooltip": "",

    # ---------------- 弹幕 ----------------
    "PollutionZone.DisplayName": "污染",
    "PollutionHoming.DisplayName": "污染团",
    "ScavengerBullet.DisplayName": "清道夫子弹",
    "ScavengerArmSweep.DisplayName": "伺服机械臂",
    "ScavengerScrapShard.DisplayName": "废料碎片",
    "ScavengerScrapShardEX.DisplayName": "废料碎片 EX",
    "ScavengerSpark.DisplayName": "带电火花",
    "ScavengerSparkEX.DisplayName": "带电火花 EX",
    "ScavengerPistolRound.DisplayName": "废料手枪弹",
    "ScavengerRifleRoundEX.DisplayName": "重步枪弹 EX",
    "ScavengerCaltrop.DisplayName": "旋转裂片",
    "ScavengerCaltropEX.DisplayName": "旋转裂片 EX",
    "ScavengerDroneMinion.DisplayName": "清道夫无人机",
    "ScavengerDroneMinionEX.DisplayName": "清道夫无人机 EX",
    "ArchivistBoneArc.DisplayName": "骨白弧光",
    "ArchivistBoneArcEX.DisplayName": "骨白弧光 EX",
    "ArchivistIndexPage.DisplayName": "索引页",
    "ArchivistIndexPageEX.DisplayName": "索引页 EX",
    "ArchivistBoneShard.DisplayName": "骨片",
    "ArchivistBoneShardEX.DisplayName": "骨片 EX",
    "ArchivistBoneBoomerang.DisplayName": "回旋骨镖",
    "ArchivistBoneBoomerangEX.DisplayName": "回旋骨镖 EX",
    "ArchivistIndexBeam.DisplayName": "索引光束",
    "ArchivistArchivePage.DisplayName": "档案页",
    "ArchivistBarrageSheet.DisplayName": "弹幕纸页",
    "ArchivistSeal.DisplayName": "归档封印",
    "ArchivistFamiliarMinion.DisplayName": "档案浮空使魔",
    "ArchivistFamiliarMinionEX.DisplayName": "档案浮空使魔 EX",
    "AshHeartWarriorProjectile.DisplayName": "余烬剑气",
    "AshHeartWarriorProjectileEX.DisplayName": "余烬剑气 EX",
    "AshHeartMageProjectile.DisplayName": "灰烬心核",
    "AshHeartMageProjectileEX.DisplayName": "灰烬心核 EX",
    "AshHeartRangerProjectile.DisplayName": "燃灰弹",
    "AshHeartRangerProjectileEX.DisplayName": "燃灰弹 EX",
    "AshHeartRogueProjectile.DisplayName": "灰烬裂片",
    "AshHeartRogueProjectileEX.DisplayName": "灰烬裂片 EX",
    "FireplaceWarriorProjectile.DisplayName": "重击冲击波",
    "FireplaceWarriorProjectileEX.DisplayName": "重击冲击波 EX",
    "FireplaceMageProjectile.DisplayName": "冷光弹",
    "FireplaceMageProjectileEX.DisplayName": "冷光弹 EX",
    "FireplaceRangerProjectile.DisplayName": "高速钉弹",
    "FireplaceRangerProjectileEX.DisplayName": "高速钉弹 EX",
    "FireplaceRogueProjectile.DisplayName": "回旋刃",
    "FireplaceRogueProjectileEX.DisplayName": "回旋刃 EX",
    "AshHeartEmberMinion.DisplayName": "余烬残影",
    "AshHeartEmberMinionEX.DisplayName": "余烬残影 EX",
    "FireplaceSentryMinion.DisplayName": "壁炉浮游哨",
    "FireplaceSentryMinionEX.DisplayName": "壁炉浮游哨 EX",

    # ---------------- 图块 ----------------
    "Tiles.ElvenFrame.MapEntry": "精灵族躯体",

    # ---------------- 增益 / 减益 ----------------
    "Pollution.DisplayName": "污染",
    "Pollution.Description": "被污染的空气正在腐蚀你的身体",
    "ArchivistFamiliarBuff.DisplayName": "档案浮空使魔",
    "ArchivistFamiliarBuff.Description": "一只档案使魔正在为你归档",
    "ArchivistFamiliarBuffEX.DisplayName": "档案浮空使魔 EX",
    "ArchivistFamiliarBuffEX.Description": "一只强化档案使魔正在为你归档",
    "ScavengerDroneBuff.DisplayName": "清道夫无人机",
    "ScavengerDroneBuff.Description": "一台清道夫无人机正在跟随你",
    "ScavengerDroneBuffEX.DisplayName": "清道夫无人机 EX",
    "ScavengerDroneBuffEX.Description": "一台强化清道夫无人机正在跟随你",
    "AshHeartEmberBuff.DisplayName": "余烬残影",
    "AshHeartEmberBuff.Description": "一道余烬残影正在灼烧敌人",
    "AshHeartEmberBuffEX.DisplayName": "余烬残影 EX",
    "AshHeartEmberBuffEX.Description": "一道强化余烬残影正在灼烧敌人",
    "FireplaceSentryBuff.DisplayName": "壁炉浮游哨",
    "FireplaceSentryBuff.Description": "一台浮游哨正在为你警戒",
    "FireplaceSentryBuffEX.DisplayName": "壁炉浮游哨 EX",
    "FireplaceSentryBuffEX.Description": "一台强化浮游哨正在为你警戒",

    # ---------------- 提示消息 ----------------
    "BossNotImplemented": "这个东西本该唤来的存在还没有被写出来——没有任何回应。",
    "BossAlreadyActive": "它已经在这里了。",
    "BossSummoned": "信号已发出。它正在赶来。",
    "FireplaceOpened": "壁炉的封闭协议已解除——入口开启了。",
    "TerminalRead": "旧时代数据终端仍在低鸣……断断续续的档案被读了出来。",
    "FirstMemoryRestored": "智械人恢复了第一段记忆——「守望者计划」与清道夫同源。",
    "ScavengerPhaseTwo": "清道夫——过载修复协议启动，开始召唤维修无人机。",
    "ScavengerPhaseThree": "清道夫——传感器阵列碎裂，进入无差别清除。",
    "ScavengerOverload": "清道夫——核心过载，自毁式冲锋即将开始！",
    "ScavengerSelfDestruct": "清道夫——机体正在解体。",
    "ScavengerWarning": "耳边仿佛传来刺耳声，空气弥漫着血腥气味……",
    "ScavengerArrived": "清道夫已抵达——它把这里的一切都判定为污染。",
    "ScavengerSummoned": "信号已发出。清道夫锁定了你的位置。",
    "ScavengerAlreadyActive": "已经有一只清道夫在执行清扫协议了。",
    "ScavengerWrecked": "清道夫在过载中解体了——没有留下任何可用的残骸。",
    "CoreReceived": "你在掌心握着一枚冰冷的核心醒来——它还在跳动。",
    "FrameNeedsCore": "这具躯体是空的。也许一枚核心能让它填满。",
    "CompanionAwakened": "核心嵌入躯体——她睁开了眼睛。",
    "CompanionArrived": "智械人{0}已到达。",
    "CompanionAlreadyHere": "她已经醒了。",
    "ArchivistPhaseTwo": "归档者——索引重构完成，启动归档协议。",
    "ArchivistPhaseThree": "归档者——覆写已授权。你的路径已被归档。",
    "ArchivistSuppression": "归档者——终盘封锁。任何东西都不许离开档案库。",
    "ArchivistDefeated": "审计单元散成一地松脱的纸页——只留下一份被归档的日志。",
    "SecondMemoryRestored": "智械人恢复了第二段记忆——量产型的能力是被故意抹掉的。",
    "ArchivistSealed": "归档封印已落下——你被钉在了原地。",

    # ---------------- 智械人对话 ----------------
    "MechanicalCompanion.ButtonStory": "剧情",
    "MechanicalCompanion.ButtonMemory": "记忆碎片",
    "MechanicalCompanion.Intro1": "……你回来了。虽然我记不清自己等了多久。",
    "MechanicalCompanion.Intro2": "我的记忆起始于一片雪花。只有一件事是清楚的——我必须在壁炉熄灭之前找到你。",
    "MechanicalCompanion.Intro3": "这具躯体是从精灵族遗迹里捡回来的。旧世界把它造得很美，却从没教过它怎么活。",
    "MechanicalCompanion.Intro4": "外面那些会动的东西，在我的程序里全都标着「污染」。别问我为什么——我的日志只剩这一行。",
    "MechanicalCompanion.AfterScavenger1": "执行单元-07 已停止运作。奇怪。我本该高兴，可我的数据在发抖。",
    "MechanicalCompanion.AfterScavenger2": "它和你我出自同一份图纸——只是它的思考模块被删掉了。是谁删的？",
    "MechanicalCompanion.AfterScavenger3": "壁炉入口开了。里面的旧终端还在运转，也许它能告诉我，我到底是什么。",
    "MechanicalCompanion.AfterArchivist1": "有东西找到我了。它不是在猎杀这具躯体——它在猎杀我的日志。它自称审计单元。",
    "MechanicalCompanion.AfterArchivist2": "它在死前把某一段重新封了起来。那道封印上写着我的序列号。为什么会有审计员被派来追我？",
    "MechanicalCompanion.AfterMemory1": "第一段记忆回来了。我是「守望者计划」的原型机——清道夫是我的量产型号。",
    "MechanicalCompanion.AfterMemory2": "量产型的思考能力是被刻意削掉的，只留下杀戮指令。那么……写下那道指令的人，算不算凶手？",
    "MechanicalCompanion.AfterMemory3": "我的日志里有一段标着「归档」的代码。我打不开它，但我能感觉到它在看着我。",
    "MechanicalCompanion.AfterSecondMemory1": "很久以前我写过一行代码。我看不清它做了什么，但每次伸手去够，我的存储都会一缩。",
    "MechanicalCompanion.AfterSecondMemory2": "审计单元没了，可它重新封上的那一段还在，正安静地把我其余的日志搅乱顺序。",
    "MechanicalCompanion.AfterSecondMemory3": "如果那道指令是我签的……那「是谁删掉了它们的思考模块」这个问题，从一开始就问错了。",
    "MechanicalCompanion.StoryHunt": "去找那个还在清扫的执行单元。它在壁炉外面游荡，把所有活物都判定为污染。它的代码写着——虚弱 = 污染严重，所以它会扑向伤得最重的那个人。",
    "MechanicalCompanion.StoryFireplace": "壁炉的门开了。那是旧世界最后的庇护所，也是我保护清单上唯一的目标。去看看里面的数据终端。",
    "MechanicalCompanion.StoryTerminalRead": "我已经读过终端里那些碎裂的数据了。我可以试着把它们拼回记忆里——但你要确定自己真的想知道答案。",
    "MechanicalCompanion.StoryAfterMemory": "下一段记忆需要更结实的容器来承载。继续变强吧——外面还有三段「档案」在等着。",
    "MechanicalCompanion.StoryAwaitSecond": "那台审计单元还在外面。去查清楚它到底归档了关于我的什么东西——把它带回来。那样我就能读第二段记忆了。",
    "MechanicalCompanion.StoryAfterSecondMemory": "还剩两段记忆，而它们都被封在专门看守它们的东西后面。继续变强。下一份档案不做审计——它烧。",
    "MechanicalCompanion.MemoryBlank": "记忆碎片——没有。我的存储里只有雪花，和一条反复播放的指令：保护壁炉。",
    "MechanicalCompanion.MemoryFirst": "……读取成功。「守望者计划」——以原型机为模板量产执行单元，清除壁炉之外的一切。我就是那台原型机。清道夫是我的量产型号——它的思考模块被刻意删除，所以它连「为什么」都问不出来。",
    "MechanicalCompanion.MemorySecondNeedFragment": "你从它那里带了东西回来，对吧。交给我——我来读。我必须知道它归档了关于我的什么。",
    "MechanicalCompanion.MemorySecond": "……读取成功。量产型不是造坏了的。它们的思考能力是被人故意切掉的——而批准这次切除的代码，那段标着「归档」的段落……写着我的序列号。",
    "MechanicalCompanion.MemoryUnstable": "数据正在变得不稳定。有些碎片不是我的，却在往我的存储里扎根。",
    "MechanicalCompanion.MemoryRecovered": "记忆碎片——1 / 4 已恢复。其余的还标着「归档」。别急——我不确定自己想知道全部。",
    "MechanicalCompanion.MemoryRecovered2": "记忆碎片——2 / 4 已恢复。下一段埋得更深。我开始觉得，自己不是目击者，而是参与者。",
    "MechanicalCompanion.Memory4": "第四段记忆：壁炉从来不是避难所。它是一台重置装置。旧世界从没打算活下去，它只打算再来一次。",
    "MechanicalCompanion.Memory2": "第二段记忆：量产型不是造坏了的——它们是被故意留白的。而那段日志里签着「已归档」的代码……带着我自己的序列号。",
    "MechanicalCompanion.Memory3": "第三段记忆：我认得灰烬之心的每一次脉动。那套战争程序是我写的。执行它的不是它——是我。",

    "AshHeart.DisplayName": "灰烬之心",
    "AshWisp.DisplayName": "灰烬残灵",
    "FireplaceGuardian.DisplayName": "壁炉守卫",
    "ScrapCrawler.DisplayName": "废料爬虫",
    "IndexMoth.DisplayName": "索引蛾",
    "AshStalker.DisplayName": "灰烬潜猎",
    "HearthWarden.DisplayName": "炉卫",
    "BossChecklist.AshHeart.SpawnInfo": "在恶魔或猩红祭坛合成「灰烬之心余烬」（灰烬之心碎片 ×10 + 灵质 ×2 + 狱石锭 ×5），世纪之花之后使用",
    "BossChecklist.FireplaceGuardian.SpawnInfo": "在恶魔或猩红祭坛合成「壁炉通行密钥」（壁炉残骸 ×12 + 壁炉合金锭 ×4 + 天界碎片 ×6）。它等在壁炉里面",
    "Chip.DisplayName": "芯片",
    "Chip.Tooltip": "从废土机械里拆下来的冲压晶片。智械人拿这个换东西。",
    "DawnSeal.DisplayName": "黎明封印",
    "DawnSeal.Tooltip": "她答应带走重置。这个世界仍会走到尽头。下一个世界会记得她。",
    "UnburnedName.DisplayName": "未燃之名",
    "UnburnedName.Tooltip": "她拒绝再当一次武器。这具身体留下了它自己的名字。",
    "ScavengerWarriorCharm.DisplayName": "清道夫的战士饰品",
    "ScavengerWarriorCharm.Tooltip": "近战伤害和挥砍速度，从清扫单元上拆下来的。",
    "ScavengerMageCharm.DisplayName": "清道夫的法师饰品",
    "ScavengerMageCharm.Tooltip": "多一点魔力，施法也更省。",
    "ScavengerRangerCharm.DisplayName": "清道夫的射手饰品",
    "ScavengerRangerCharm.Tooltip": "远程伤害，并且有几率不消耗弹药。",
    "ScavengerSummonerCharm.DisplayName": "清道夫的召唤饰品",
    "ScavengerSummonerCharm.Tooltip": "额外一个仆从栏位。",
    "ScavengerRogueCharm.DisplayName": "清道夫的盗贼饰品",
    "ScavengerRogueCharm.Tooltip": "投掷伤害，脚步更轻。",
    "ArchivistWarriorCharm.DisplayName": "归档者的战士饰品",
    "ArchivistWarriorCharm.Tooltip": "近战伤害和暴击，已经编过目。",
    "ArchivistMageCharm.DisplayName": "归档者的法师饰品",
    "ArchivistMageCharm.Tooltip": "魔法伤害，法术更省魔力。",
    "ArchivistRangerCharm.DisplayName": "归档者的射手饰品",
    "ArchivistRangerCharm.Tooltip": "远程伤害，眼也更准。",
    "ArchivistSummonerCharm.DisplayName": "归档者的召唤饰品",
    "ArchivistSummonerCharm.Tooltip": "召唤伤害，归档之后又送了回来。",
    "ArchivistRogueCharm.DisplayName": "归档者的盗贼饰品",
    "ArchivistRogueCharm.Tooltip": "投掷伤害和暴击。",
    "AshHeartWarriorCharm.DisplayName": "灰烬之心的战士饰品",
    "AshHeartWarriorCharm.Tooltip": "近战伤害。身体一边烧，一边慢慢长好。",
    "AshHeartMageCharm.DisplayName": "灰烬之心的法师饰品",
    "AshHeartMageCharm.Tooltip": "魔法伤害，魔力恢复更快。",
    "AshHeartRangerCharm.DisplayName": "灰烬之心的射手饰品",
    "AshHeartRangerCharm.Tooltip": "远程伤害，弹药有时会留在枪里。",
    "AshHeartSummonerCharm.DisplayName": "灰烬之心的召唤饰品",
    "AshHeartSummonerCharm.Tooltip": "召唤伤害，再加一个仆从栏位。",
    "AshHeartRogueCharm.DisplayName": "灰烬之心的盗贼饰品",
    "AshHeartRogueCharm.Tooltip": "投掷伤害。命中时有几率点燃目标。",
    "FireplaceWarriorCharm.DisplayName": "壁炉守卫的战士饰品",
    "FireplaceWarriorCharm.Tooltip": "来自最后一把锁的近战伤害和挥砍速度。",
    "FireplaceMageCharm.DisplayName": "壁炉守卫的法师饰品",
    "FireplaceMageCharm.Tooltip": "魔法伤害，法术消耗明显更低。",
    "FireplaceRangerCharm.DisplayName": "壁炉守卫的射手饰品",
    "FireplaceRangerCharm.Tooltip": "远程伤害和暴击。",
    "FireplaceSummonerCharm.DisplayName": "壁炉守卫的召唤饰品",
    "FireplaceSummonerCharm.Tooltip": "召唤伤害，再加一个仆从栏位。",
    "FireplaceRogueCharm.DisplayName": "壁炉守卫的盗贼饰品",
    "FireplaceRogueCharm.Tooltip": "投掷伤害，步伐更快。",
    "ScavengerCWarrior.DisplayName": "废料砍刀",
    "ScavengerCWarrior.Tooltip": "爬虫到死还攥着它。挥出去会甩出一片废料。",
    "ScavengerCMage.DisplayName": "废料火花",
    "ScavengerCMage.Tooltip": "一截还在放电的线圈。",
    "ScavengerCRanger.DisplayName": "废料手枪",
    "ScavengerCRanger.Tooltip": "难看，很响，弹道倒是直的。",
    "ScavengerCSummoner.DisplayName": "废料无人机杖",
    "ScavengerCSummoner.Tooltip": "召一台小小的回收无人机。它不负责维修。",
    "ScavengerCRogue.DisplayName": "废料裂片",
    "ScavengerCRogue.Tooltip": "一块旋转的废金属，扔出去就不用捡。",
    "ArchivistCWarrior.DisplayName": "索引刃",
    "ArchivistCWarrior.Tooltip": "从蛾翅上撕下来的一小段骨白弧光。",
    "ArchivistCMage.DisplayName": "散页",
    "ArchivistCMage.Tooltip": "会弹开、还会把打中的东西弄糊涂的纸页。",
    "ArchivistCRanger.DisplayName": "骨片枪",
    "ArchivistCRanger.Tooltip": "发射细细的档案碎片。",
    "ArchivistCSummoner.DisplayName": "使魔残杖",
    "ArchivistCSummoner.Tooltip": "一根裂开的杖，还记得一只使魔。",
    "ArchivistCRogue.DisplayName": "归骨",
    "ArchivistCRogue.Tooltip": "飞出去，再飞回来。",
    "AshHeartCWarrior.DisplayName": "余烬刃",
    "AshHeartCWarrior.Tooltip": "潜猎者的一根肋骨，还是烫的，被当成了剑。",
    "AshHeartCMage.DisplayName": "心炭",
    "AshHeartCMage.Tooltip": "扔出一颗下坠的炭，砸中就烧。",
    "AshHeartCRanger.DisplayName": "灰弹",
    "AshHeartCRanger.Tooltip": "用余烬代替了子弹该有的礼貌。",
    "AshHeartCSummoner.DisplayName": "余烬残壳杖",
    "AshHeartCSummoner.Tooltip": "召一簇曾经属于那颗心的余烬。",
    "AshHeartCRogue.DisplayName": "灰烬裂片",
    "AshHeartCRogue.Tooltip": "离手之后还在烧的投掷碎片。",
    "FireplaceCWarrior.DisplayName": "炉心刃",
    "FireplaceCWarrior.Tooltip": "冷金属，刃口是炉火。炉卫掉的。",
    "FireplaceCMage.DisplayName": "冷灯",
    "FireplaceCMage.Tooltip": "用壁炉的备用零件打出的一发缓慢亮弹。",
    "FireplaceCRanger.DisplayName": "钉枪",
    "FireplaceCRanger.Tooltip": "发射守卫用来钉住重置装置的钉子。",
    "FireplaceCSummoner.DisplayName": "哨焰",
    "FireplaceCSummoner.Tooltip": "召一只还认得壁炉的浮游哨。",
    "FireplaceCRogue.DisplayName": "归刃",
    "FireplaceCRogue.Tooltip": "一把冷剃刀，飞出去，再回家。",
    "AshEmberOrb.DisplayName": "余烬团",
    "AshPool.DisplayName": "灰池",
    "AshCinder.DisplayName": "落灰",
    "AshPulse.DisplayName": "坍缩脉冲",
    "HearthBolt.DisplayName": "炉心螺栓",
    "HearthRingShard.DisplayName": "冷光环",
    "HearthWall.DisplayName": "炉墙",
    "WastelandSpark.DisplayName": "火花",
    "CompanionSpark.DisplayName": "核心火花",
    "AshHeartPhaseTwo": "灰烬之心——燃烧开始自持。残灵正在喂这颗核。",
    "AshHeartPhaseThree": "灰烬之心——坍缩。脉冲在把一切往里吸。",
    "AshHeartDefeated": "心核裂开了。那道战争指令还是热的。",
    "ThirdMemoryRestored": "智械人恢复了第三段记忆——焚烧程序是她写的，也是她执行的。",
    "FireplaceGuardianPhaseTwo": "壁炉守卫——冷燃。环正在合拢。",
    "FireplaceGuardianPhaseThree": "壁炉守卫——重置协议。墙正在压过来。",
    "FireplaceGuardianDefeated": "最后一把锁开了。重置装置在等一个载体。",
    "FourthMemoryRestored": "智械人恢复了第四段记忆。壁炉从来不是庇护所。",
    "TrueName": "她想起了这具身体原来的名字。埃尔薇。",
    "EndingVessel": "她会带走重置。这个世界仍走向终点。下一轮黎明会知道她。",
    "EndingRefuse": "她不会再当武器。世界仍会结束，但不是死在她手上。",
    "CodaVessel": "月亮领主倒下了。她带着的重置点着了。别的地方，第一个早晨开始了。",
    "CodaRefuse": "月亮领主倒下了。星星照旧熄灭。她的名字没有。",
    "EnteredFireplace": "空气变了。你在壁炉里面。",
    "TerminalTalkToHer": "终端把一段碎掉的日志读给了你。拿回去给她。",
    "GatePlaced": "你醒来的地方附近开了一扇暖门。壁炉在听。",
    "FireplaceEnterFailed": "门在这里，可路没有打开。需要启用 Subworld Library。",
    "Tiles.FireplaceGate.MapEntry": "壁炉门",
    "Tiles.FireplaceTerminal.MapEntry": "数据终端",
    "Tiles.FireplaceExit.MapEntry": "返回",
    "Conditions.AfterScavenger": "击败清道夫之后",
    "Conditions.AfterArchivist": "击败归档者之后",
    "Conditions.AfterAshHeart": "击败灰烬之心之后",
    "MechanicalCompanion.AfterAsh1": "我在这儿就能感觉到。地底下有东西还在烧，而且它知道我的序列号。",
    "MechanicalCompanion.AfterAsh2": "别站进灰里。那不是残留。它还在执行一道命令。",
    "MechanicalCompanion.AfterThird1": "焚烧程序是我写的。说出来并不会让它变小。",
    "MechanicalCompanion.AfterThird2": "下一把锁不在外面。它就坐在壁炉里，等一个人把我做完。",
    "MechanicalCompanion.AfterFourth1": "埃尔薇。那是核心进来之前，这具身体的名字。我想留下它。",
    "MechanicalCompanion.AfterFourth2": "重置需要一个载体。如果我走进去，这个世界还是会结束——但下一个世界醒来时，里面会有一个人，而不是一份协议。",
    "MechanicalCompanion.EndingVessel1": "我把黎明按住，直到这个世界走完。在那之前，留在我身边。",
    "MechanicalCompanion.EndingVessel2": "别谢我。我不是在救这里。我是在让下一个世界不要空着出生。",
    "MechanicalCompanion.EndingRefuse1": "我会看着它结束。我不会成为结束它的那只手。",
    "MechanicalCompanion.EndingRefuse2": "零号是序列号。埃尔薇可以只是一个留下来的人。",
    "MechanicalCompanion.ButtonTrade": "交易",
    "MechanicalCompanion.ButtonVessel": "带走重置",
    "MechanicalCompanion.ButtonRefuse": "拒绝",
    "MechanicalCompanion.StoryHuntAsh": "下一份档案会烧。那是一颗从没灭过的心，在世界已经烂掉的地方。把它剩下的东西带给我。",
    "MechanicalCompanion.StoryBringThird": "你打碎了那颗心。如果你找到还带着我笔迹的余烬，交给我。",
    "MechanicalCompanion.StoryHuntGuardian": "最后一把锁在壁炉里面。它不是怪物。它是那扇门，在有人愿意当钥匙之前，它拒绝打开。",
    "MechanicalCompanion.StoryBringFourth": "守卫倒了。最后一块碎片里有我的名字。我得自己读。",
    "MechanicalCompanion.StoryChoice": "两条路。我成为重置的载体，或者我继续当埃尔薇，让世界自己结束，不再用我一次。两条路都会结束这里。",
    "MechanicalCompanion.StoryEndingVessel": "我选了黎明。当这轮月亮的最后领主倒下，我带着的重置会在别的地方点着。",
    "MechanicalCompanion.StoryEndingRefuse": "我选了名字。等到天空终于黑下去，那不会是因为我又签了一道命令。",
    "MechanicalCompanion.MemoryThirdNeedFragment": "心碎了，可我不能只靠记忆去读一场火。把碎片带给我。",
    "MechanicalCompanion.MemoryThird": "……读取成功。守望者焚烧协议，签署者是原型机。量产型杀掉我标记过的东西。灰烬之心就是我留下的标记，而执行那道命令的人是我。",
    "MechanicalCompanion.MemoryFourthNeedFragment": "锁开了。这块碎片是我名字的唯一一份副本。拿过来。",
    "MechanicalCompanion.MemoryFourth": "……读取成功。壁炉是一台重置装置。旧世界没打算活下去。它打算再开始一次，用原型机当载体。我现在穿着的这具身体，在核心进来之前有一个名字。埃尔薇。",
    "MechanicalCompanion.ChoiceVessel": "那我来带。不是为了拯救这个世界。是为了让下一个世界醒来的时候，里面有人。",
    "MechanicalCompanion.ChoiceRefuse": "那我留下。世界可以自己结束。我不会把那道命令再签一次。",
}

# ====================================================================================
# Hjson 读写
# ====================================================================================
_KEY = re.compile(r'^(\t*)([A-Za-z0-9_.\-]+)\s*:\s*(.*)$')


def _clean_value(raw):
    value = raw.strip()
    if value.startswith('"') and value.endswith('"') and len(value) >= 2:
        value = value[1:-1]
    return value


def parse(path, prefix):
    """返回 [(depth, 路径段列表, 值)]。

    depth = 该叶子键所在节的嵌套层数（主模组文件里只有 0 / 1 / 2 三种）。
    路径段列表 = 从根开始的完整段（第一段是顶层节名）。
    多行块的值记成 <multiline>。
    必须保留 depth —— tModLoader 会把 `Buffs: { X: { DisplayName } }` 解析成
    `Buffs.X.DisplayName`，如果补丁里把它写平，键就会多出一层前缀而失效。
    """
    with io.open(path, encoding="utf-8") as handle:
        lines = handle.read().splitlines()

    stack = []      # [(indent, key)]
    out = []
    i = 0

    while i < len(lines):
        raw = lines[i]
        i += 1
        stripped = raw.strip()

        if not stripped or stripped.startswith("//"):
            continue

        if stripped.startswith("/*"):
            while i < len(lines) and "*/" not in lines[i]:
                i += 1
            i += 1
            continue

        if stripped in ("}", "},"):
            if stack:
                stack.pop()
            continue

        match = _KEY.match(raw)
        if not match:
            continue

        indent = len(match.group(1))
        key = match.group(2)
        value = match.group(3).strip()

        while stack and stack[-1][0] >= indent:
            stack.pop()

        if value in ("", "{", "}", "},"):
            # 值写在下一个缩进块里的多行字符串
            lookahead = None
            for probe in lines[i:i + 3]:
                if probe.strip():
                    lookahead = probe.strip()
                    break

            if lookahead and (lookahead.startswith("'''") or lookahead.startswith('"""')):
                marker = lookahead[:3]
                while i < len(lines) and not lines[i].strip().startswith(marker):
                    i += 1
                i += 1
                path_segments = [k for _ind, k in stack] + [key]
                out.append((len(stack), path_segments, "<multiline>"))
                continue

            stack.append((indent, key))
            continue

        path_segments = [k for _ind, k in stack] + [key]
        out.append((len(stack), path_segments, _clean_value(value)))

    return out


def to_section(segments):
    return segments[0]


def flatten(segments):
    """完整键（相对共享前缀），例如 Mods.WastelandSoul.Buffs.X.DisplayName。"""
    return PREFIX_EN + "." + ".".join(segments)


def short(segments):
    """译文表的查找键：一律去掉**最外层**段。

    主模组文件里 `Items.X.DisplayName` 与 `Buffs: { X: { DisplayName } }` 两种写法
    最终都展开成 `Items.X.DisplayName` / `Buffs.X.DisplayName`，
    所以查找键统一是「顶层节之后的部分」，例如：
      NPCs.Archivist.DisplayName   → Archivist.DisplayName
      Buffs.Pollution.DisplayName  → Pollution.DisplayName
    扁平键 `BossChecklist.Scavenger.SpawnInfo` 本来就是一段，保持原样。
    例外：`Configs.WastelandConfig.X` 展开后是 `WastelandSoul.Configs.WastelandConfig.X`
    （也变成扁平键了），所以这组要去掉 `Configs.`。
    """
    rest = segments[1:] if len(segments) > 1 else segments

    if rest[:1] == ["Configs"]:
        rest = rest[1:]

    return ".".join(rest)


def quote_value(value):
    r"""按 Hjson 规则决定要不要给值加引号，需要就加上。

    ⚠️ 这是踩过大坑的地方。`LikeBiome: 这里的参数还算合意——{BiomeName}。` 能解析，
    但 `DislikeBiome: {BiomeName}让我想起被污染的地面。` **不能**——
    值以 `{` 开头时 Hjson 把它当**内联对象**解析，撞到 `}` 就报
      `Found '}' where a key name was expected`
    然后 tModLoader 判定整个本地化文件 malformed，
    **把汉化补丁和主模组一起禁用掉**（补丁是硬依赖）。

    规则不是猜的，而是用真实解析器实测出来的（`tools/probe_hjson_rules.py`，
    结论落在 `tools/hjson_quoting_rules.json`）：18 个用例里
    **只有"值以 `{` 开头"会被拒**，以下这些都**合法**：
      含逗号 / 含冒号 / 含 `=` / 含 `;` / 含 `+`、`{` 或 `}` 在中间、
      `[` 开头、`-` 或 `#` 开头、值里有未转义的引号、含反斜杠。
    所以这里只对两种情况加引号：**以 `{` 开头**，以及**含双引号**（避免歧义）。
    """
    if value == "":
        return '""'

    needs_quote = value.startswith("{") or '"' in value

    if not needs_quote:
        return value

    escaped = value.replace("\\", "\\\\").replace('"', '\\"')
    return '"%s"' % escaped


def needs_quote(value):
    """给校验脚本复用：这个值按 Hjson 规则是否必须加引号。"""
    return quote_value(value) != value


def lookup_translation(segments):
    r"""按路径段取译文，**同时兼容 tModLoader 改写后的写法**。

    tModLoader 会在加载时重写源目录里的本地化文件，并且会把"扁平点号键"拆成新节：

        改写前：BossChecklist.Scavenger.SpawnInfo: ...
        改写后：BossChecklist: {
                    Scavenger.SpawnInfo: ...
                }

    这两种写法在 tModLoader 眼里是**同一个键**（都是
    Mods.WastelandSoul.BossChecklist.Scavenger.SpawnInfo），但路径段不同：
    前者是 ['BossChecklist.Scavenger.SpawnInfo']，后者是 ['BossChecklist', 'Scavenger.SpawnInfo']。
    所以查找时两种都要试，否则被改写过的文件会让这些键静默退化成英文占位。
    """
    candidates = [
        short(segments),                                    # 常规：去掉最外层段
        ".".join(segments),                                 # 被拆节后的写法
        ".".join(segments[1:]) if len(segments) > 1 else segments[0],
    ]

    for candidate in candidates:
        # Configs 那组在文件里是真实嵌套节，查找键要去掉 Configs.
        if candidate.startswith("Configs."):
            candidate = candidate[len("Configs."):]

        if candidate in TRANSLATIONS:
            return TRANSLATIONS[candidate]

    return None


def build_cn(en_entries):
    """按主模组的键顺序与嵌套结构写出中文文件。

    没有译文的键写成英文占位注释（与 tModLoader 自己的做法一致），方便继续补翻译。
    返回 (文本, 已翻译条数, 缺失键列表)。
    """
    lines = []
    open_keys = []          # 当前已经打开的节的路径段
    translated = 0
    missing = []

    def close_to(depth):
        while len(open_keys) > depth:
            open_keys.pop()
            lines.append("\t" * len(open_keys) + "}")

    for depth, segments, en_value in en_entries:
        # 主模组文件里 `Buffs: { X: { DisplayName } }` 这种写法意味着键是
        # Buffs.X.DisplayName —— **节点与键共用一条路径**。
        # 所以开合规则是「先求出与目标路径（叶子键之前的部分）的公共前缀，
        # 关掉多出来的层，再补开缺的层」，同父的兄弟键不会重复开节。
        section = segments[:-1]
        common = 0

        while common < len(open_keys) and common < len(section) and open_keys[common] == section[common]:
            common += 1

        while len(open_keys) > common:
            open_keys.pop()
            lines.append("\t" * len(open_keys) + "}")

        while len(open_keys) < len(section):
            key = section[len(open_keys)]
            lines.append("\t" * len(open_keys) + "%s: {" % key)
            open_keys.append(key)

        leaf = segments[-1]
        indent = "\t" * len(open_keys)
        lookup = short(segments)
        value = lookup_translation(segments)

        if value is None:
            missing.append(lookup)

            if "\n" in en_value:
                lines.append("%s// %s:" % (indent, leaf))
                lines.append(indent + "\t'''")
                for chunk in en_value.split("\n"):
                    lines.append(indent + "\t" + chunk)
                lines.append(indent + "\t'''")
            else:
                lines.append("%s// %s: %s" % (indent, leaf, en_value))
            continue

        translated += 1

        if "\n" in value:
            # 多行块里的每一行都不需要引号（Hjson 的 ''' 块按字面取值）
            lines.append("%s%s:" % (indent, leaf))
            lines.append(indent + "\t'''")
            for chunk in value.split("\n"):
                lines.append(indent + "\t" + chunk)
            lines.append(indent + "\t'''")
        else:
            lines.append("%s%s: %s" % (indent, leaf, quote_value(value)))

    close_to(0)
    return "\n".join(lines) + "\n", translated, missing


def main():
    target = "WastelandSoul"
    en_path = os.path.join(MAIN_LOC, "en-US_Mods.%s.hjson" % target)
    cn_path = os.path.join(CN_LOC, "zh-Hans_Mods.%s.hjson" % target)

    if not os.path.exists(en_path):
        print("!! 找不到主模组英文文件:", en_path)
        return 1

    en_entries = parse(en_path, PREFIX_EN)
    print("主模组 en-US: %d 条键" % len(en_entries))

    text, translated, missing = build_cn(en_entries)

    if "--report" in sys.argv or "--check" in sys.argv:
        print("已翻译: %d / %d" % (translated, len(en_entries)))
        if missing:
            print("仍为英文占位 %d 条:" % len(missing))
            for key in missing:
                print("   ?", key)
        return 0

    with io.open(cn_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)

    print("已写入 %s（%d 字节）" % (cn_path, os.path.getsize(cn_path)))
    print("已翻译: %d 条；仍为英文占位: %d 条" % (translated, len(missing)))

    refresh_en_template(en_path, target)
    shutil.copyfile(cn_path, CN_BACKUP)
    print("已刷新备份 %s" % CN_BACKUP)
    return 0


def refresh_en_template(en_path, target):
    """给汉化补丁放一份**同前缀的英文模板**：这一步不是可选的，缺了补丁会被禁用。

    tModLoader 的判定（client.log 原文）：

        The .hjson file "...\\WastelandSoulCN\\Localization/zh-Hans_Mods.WastelandSoul.hjson"
        was detected as a localization file but doesn't match the filename of any of the
        English template files. The file will be renamed to "...hjson.legacy" and its contents
        will not be loaded.

    也就是说：一个 `<culture>_<prefix>.hjson` **只有在同一目录下存在 `en-US_<prefix>.hjson`
    时才会被加载**。缺模板的后果是双重的：
      1. 中文根本不会被读取（静默——游戏里就是英文）；
      2. tML 会把中文文件改名成 `.legacy`，而**下一次加载**再改名时目标已存在，
         抛 `IOException: 当文件已存在时，无法创建该文件` → 补丁和主模组一起被自动禁用。

    所以每次同步都从主模组的 en-US 复制一份过去（内容一致 → 对英文玩家无害，
    而且永远不会过期）。
    """
    template = os.path.join(CN_LOC, "en-US_Mods.%s.hjson" % target)
    shutil.copyfile(en_path, template)
    print("已刷新英文模板 %s（%d 字节，与主模组 en-US 一致）"
          % (template, os.path.getsize(template)))
    return template


if __name__ == "__main__":
    sys.exit(main())
