# -*- coding: utf-8 -*-
"""把四段记忆的后三段台词插进两个本地化文件（插在 Memory1 之前，hjson 不关心顺序）。

守卫用「行首键名」正则，避免把 AfterMemory2 之类的键误判成已存在。
"""
import re
import shutil

MAIN = r"E:\开发\WastelandSoul\Localization\en-US_Mods.WastelandSoul.hjson"
PATCH = r"E:\开发\WastelandSoulCN\Localization\zh-Hans_Mods.WastelandSoul.hjson"
BACKUP = r"E:\开发\.backup\WastelandSoulCN_zh-Hans.hjson"

EN = [
    ("Memory2", "Second memory: the mass-produced units were not built broken - they were left blank on purpose. And the code in that log, the part signed Archived... carries my own serial number."),
    ("Memory3", "Third memory: I recognise every pulse of the Ash Heart. I wrote that war program. The one who carried it out was not it - it was me."),
]
# Memory4 已在上一版写入，这里只补缺的两个（重复插入会报错，所以先查再插）

ZH = [
    ("Memory2", "第二段记忆：量产型不是被做坏的，是被故意留白的。而日志里那段署名「归档」的代码……写的是我自己的编号。"),
    ("Memory3", "第三段记忆：灰烬之心的每一次脉动我都认得。那套战争程序是我写的。执行它的不是它，是我。"),
]

ZH_FALLBACK = [
    ("Memory4", "第四段记忆：壁炉从来不是避难所，它是重置装置。旧世界没打算活下去，它只打算重来一次。"),
]


def has_key(text, key):
    return re.search(r"^\s*%s\s*:" % re.escape(key), text, re.M) is not None


def insert_before_memory1(path, entries):
    with open(path, encoding="utf-8") as handle:
        lines = handle.readlines()

    text = "".join(lines)
    missing = [(k, v) for k, v in entries if not has_key(text, k)]
    out = []
    done = False

    for line in lines:
        if not done and "Memory1" in line and ":" in line:
            indent = re.match(r"[\t ]*", line).group(0)
            for key, value in missing:
                out.append("%s%s: %s\n" % (indent, key, value))
            done = True
        out.append(line)

    if done and missing:
        with open(path, "w", encoding="utf-8") as handle:
            handle.write("".join(out))

    return [k for k, _ in missing]


def main():
    print("  en-US  新增:", insert_before_memory1(MAIN, EN))
    print("  zh-Hans 新增:", insert_before_memory1(PATCH, ZH + ZH_FALLBACK))
    shutil.copyfile(PATCH, BACKUP)
    print("  备份已刷新")


if __name__ == "__main__":
    main()
