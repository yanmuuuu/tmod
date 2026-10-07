# -*- coding: utf-8 -*-
"""一次性内容修改：介绍里不再写「制作方法」（工作台/祭坛/熔炉那句）。

理由（你的反馈）：制作站与配方在合成界面里已经写得很清楚，介绍里再写一遍是冗余信息。

删的行：
  en-US                                       中文（译文表里对应的尾巴）
  Crafted at an Anvil                         \n在铁砧制作
  Crafted at an Mythril Anvil                 \n在秘银砧制作
  Crafted at a Demon/Crimson Altar from ...   在恶魔/猩红祭坛用 ... 制作\n（在中间，要连换行一起删）
  Crafted at an Anvil from 5 Salvaged Steel  在铁砧用 5 个精钢制作\n
  Smelted at an Adamantite Forge              \n在精金熔炉熔炼

保留的：描述性/用途性的行（例如材料写「用来锻造精钢套装」、BossChecklist 的召唤说明）。
"""
import io
import re

EN = r"E:\开发\WastelandSoul\Localization\en-US_Mods.WastelandSoul.hjson"
TABLE = r"E:\开发\tools\sync_cn_translation.py"

EN_PATTERNS = [
    re.compile(r"^\s*Crafted at an? (?:Mythril )?Anvil(?: from .*)?\s*$"),
    re.compile(r"^\s*Crafted at a Demon/Crimson Altar from .*\s*$"),
    re.compile(r"^\s*Smelted at an Adamantite Forge\s*$"),
]

CN_PATTERNS = [
    (re.compile(u"\\\\n在铁砧制作"), u""),
    (re.compile(u"\\\\n在秘银砧制作"), u""),
    (re.compile(u"在恶魔/猩红祭坛用[^\"]*?制作\\\\n"), u""),   # 在中间的那句，连换行一起删
    (re.compile(u"\\\\n在精金熔炉熔炼"), u""),
]


def main():
    text = io.open(EN, encoding="utf-8").read()
    lines = text.split("\n")
    kept = []
    removed = []

    for line in lines:
        if any(pattern.match(line) for pattern in EN_PATTERNS):
            removed.append(line.strip())
            continue

        kept.append(line)

    io.open(EN, "w", encoding="utf-8", newline="\n").write("\n".join(kept))
    print("en-US: 删除 %d 行" % len(removed))

    for item in removed[:6]:
        print("   - %s" % item)

    if len(removed) > 6:
        print("   ...（其余 %d 行同类）" % (len(removed) - 6))

    table = io.open(TABLE, encoding="utf-8").read()
    total = 0

    for pattern, replacement in CN_PATTERNS:
        table, count = pattern.subn(replacement, table)
        total += count

    io.open(TABLE, "w", encoding="utf-8", newline="\n").write(table)
    print("译文表: 清理 %d 处中文制作说明" % total)


if __name__ == "__main__":
    main()
