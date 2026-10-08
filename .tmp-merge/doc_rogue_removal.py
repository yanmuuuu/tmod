# -*- coding: utf-8 -*-
"""文档同步：盗贼职业及其全部内容已按要求移除。

1) 两份文档里的「五职业」统一改成「四职业」；
2) 在 开发说明.md 末尾追加「批次 30：移除盗贼职业」记录；
3) 在 故事线.md 的武器清单处加一行说明（保持正文不改写，避免误伤历史叙述）。
"""
import io

DOC = r"E:\开发\WastelandSoul\开发说明.md"
STORY = r"E:\开发\WastelandSoul\故事线.md"

NOTE = u'''
---

## 批次 30：移除盗贼（Rogue）职业 —— 玩家要求「把所有关于职业盗贼的东西删去」

**原则**：只删盗贼，**战士 / 射手 / 法师 / 召唤 四个职业一字未动**。

**删除内容（由内容代理执行，父代理转述并归档）**

| 类别 | 数量 | 说明 |
| --- | --- | --- |
| 整文件删除 | **14 个 .cs** | 4 个 Boss 的盗贼武器（含 EX）8 个 + 锈蚀/废料盗贼武器 2 个 + 盗贼升级树 1 个 + 3 个盗贼弹幕文件 |
| 随宿主文件删段 | **16 弹幕 + 18 物品类** | 各 Boss 的回旋/飞镖/爆炸弹幕、盗贼护甲（面罩/背心/护腿）、齿轮镖/锯齿环/余烬环等 |
| 贴图 | **49 张 PNG**（443 → 394） | 前后差集逐项核对：无多删、无漏删 |
| 本地化 | 英文 **减 59 键 → 729 键**；译文表 **删 71 条短键** + 改写 4 条复合 Tooltip | `check_cn_parity` 729/729，0 缺失 / 0 多余 / 0 未翻译 |
| 掉落表 | 4 个 Boss 的每职业一件 → **每 Boss 四件**（无空位、无注释残留） | C 线小怪池 5→4 职业；清道夫掉落包、芯片商店 3 个盗贼饰品下架 |
| 代码引用 | `WastelandWeaponKit.Rogue()`、`CLineKind.Rogue` 枚举与 case、`WastelandEffectColors` 的 `DamageClass.Throwing` 配色分支、`ScavengerGraceHood` 的投掷 +5% | 注释「五职业」→「四职业」 |

**保留但属共用基础设施**（复核后确认不是盗贼专属）：
- `RebarBoomerang` / `RebarBoomerangProj`：名字像投掷，**实际是近战伤害**的回旋武器 → 保留；
- `AccessoryUpgradeTreeRogue.cs` 实为**召唤向**饰品树（拾荒者芯片 → 强化拾荒者芯片 → 废土主宰核心：召唤伤害、拾取/金币/魔力星磁吸、仆从栏位 +1），
  已**删净其中的投掷加成**并重命名为 `AccessoryUpgradeTreeChip.cs`（类名不变）→ 三件饰品保留；
- `WastelandExploration` 里的原版 `Shuriken`/`EnchantedBoomerang` 掉落、`MechanicalCompanion` 里原版 `AttackType` 注释 → 保留。

**额外删除（超出玩家列举范围，但确属盗贼/投掷）**：`RustShuriken`(+EX)、`ScrapGrenade`、`ScrapBuzzsaw`。

**验收**：编译 **0 errors / 0 warnings**；`check_assets` / `check_batch_usage` / `check_load_thread_safety` / `check_no_auto_trails` / `check_subworld_scope` / `check_tooltip_no_crafting` / `check_fireplace_layout` / `check_localization` / `verify_batch16` 全绿；`check_tmod_contents` 主模组 **392 贴图与源目录一致**。

**每个 Boss 现在的四件（A 线可合成 / B 线仅掉落袋）**

| Boss | 战士 | 射手 | 法师 | 召唤 |
| --- | --- | --- | --- | --- |
| 清道夫 | 废料砍刀 | 废料手枪 | 电火花棒 | 无人机信标 |
| 归档者 | 骨刃 | 骨制散射枪 | 索引法典 | 使魔法杖 |
| 灰烬之心 | 大剑 | 余烬步枪 | 核心法杖 | 燃烬法杖 |
| 壁炉守卫 | 大剑 | 步枪 | 权杖 | 信标 |

（均含 MK-II 版；掉落袋靠 `CollectExclusiveWeapons()` 扫描目录自动收集，无需改袋子代码。）
'''


def tweak(path, extra=None):
    text = io.open(path, encoding="utf-8").read()
    before = text
    text = text.replace("五职业", "四职业").replace("五个职业", "四个职业").replace("5 职业", "4 职业")

    if extra and extra not in text:
        if not text.endswith("\n"):
            text += "\n"
        text += extra

    if text != before:
        io.open(path, "w", encoding="utf-8", newline="\n").write(text)
        print("已更新 %s" % path)
    else:
        print("%s 无需改动" % path)


tweak(DOC, NOTE)
tweak(STORY, u"\n> 注：盗贼（Rogue）职业及其全部武器/饰品/弹幕/贴图已于**批次 30** 按要求移除；\n"
             u"> 本文附录里的“盗贼”条目仅作为历史记录保留，游戏内已不存在。\n")

# 校验
for path in (DOC, STORY):
    text = io.open(path, encoding="utf-8").read()
    print("%s：'五职业' 残留 %d 处，'盗贼' 出现 %d 处" % (
        path.split("\\")[-1], text.count("五职业"), text.count("盗贼")))
