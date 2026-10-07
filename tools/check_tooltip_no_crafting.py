# -*- coding: utf-8 -*-
"""检查物品介绍里**不许再出现"制作方法"**（工作台 / 祭坛 / 熔炉那句）。

你的要求：制作站与配方在合成界面里已经写清楚，介绍里再写一遍是冗余信息。

判定：
  * 主模组 en-US：物品 Tooltip 里出现 `Crafted at ...` / `Smelted at ...` / `Crafted from ...` → 失败；
  * 汉化补丁 zh-Hans：出现 `在铁砧` / `在秘银砧` / `在恶魔/猩红祭坛用` / `在精金熔炉熔炼` → 失败；
  * 例外：`BossChecklist.*.SpawnInfo` —— 那条**本来就该写"怎么召唤 Boss"**（Boss 清单界面用），
    所以按前缀放行；召唤物的 Tooltip 不在例外里。
"""
import io
import os
import re
import sys

EN = r"E:\开发\WastelandSoul\Localization\en-US_Mods.WastelandSoul.hjson"
CN = r"E:\开发\WastelandSoulCN\Localization\zh-Hans_Mods.WastelandSoul.hjson"

EN_BAD = re.compile(r"\b(?:Crafted at|Smelted at|Crafted from)\b")
CN_BAD = re.compile(u"在铁砧|在秘银砧|在恶魔/猩红祭坛用|在精金熔炉熔炼|在熔炉")

# 允许保留的键前缀（BossChecklist 的召唤说明是刻意的）
ALLOW = ("BossChecklist.",)


def scan(path, bad, label):
    if not os.path.exists(path):
        print("!! 找不到 %s" % path)
        return ["missing-file"]

    problems = []
    key = ""

    for number, line in enumerate(io.open(path, encoding="utf-8-sig").read().split("\n"), start=1):
        stripped = line.strip()

        # 顶层键形如 `Xxx: {` 或 `Xxx.Yyy: value`
        match = re.match(r"^([A-Za-z0-9_.]+)\s*:", stripped)

        if match:
            key = match.group(1)

        if not bad.search(line):
            continue

        if key.startswith(ALLOW):
            continue

        problems.append("%s:%d  %s  <- %s" % (os.path.basename(path), number, stripped[:80], key))

    for item in problems:
        print("   -", item)

    return problems


def main():
    problems = []
    problems += scan(EN, EN_BAD, "en-US")
    problems += scan(CN, CN_BAD, "zh-Hans")

    if problems:
        print("!! 介绍里又出现了制作方法（%d 处）：玩家在合成界面就能看到，属于冗余信息" % len(problems))
        print("CHECK FAILED")
        return 1

    print("[OK] 物品介绍里没有制作方法信息（BossChecklist 的召唤说明按设计保留）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
