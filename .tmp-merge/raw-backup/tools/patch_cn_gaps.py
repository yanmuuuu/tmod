# -*- coding: utf-8 -*-
"""⚠️ 一次性历史脚本 —— 【不要重跑】⚠️

它补的那两条中文要么已作废（`ArchivistSummon` 在批次 11 被删），要么已**放回译文表**
（`MechanicalCompanion.Census.SpawnCondition`，见 `sync_cn_translation.py` 的 TRANSLATIONS）。
重跑只会往生成文件里塞内容，而生成文件每次 `sync_cn_translation.py` 都会被整体重写。

要补中文，请把译文加进 `sync_cn_translation.py` 的 TRANSLATIONS，然后跑一次它。

---- 以下为原始说明 ----
补齐中文对齐检查指出的缺口（tModLoader 加载时会重写该文件，所以这些键必须每次补）。

缺的两条：
  Items.ArchivistSummon.DisplayName   归档者的索引
  NPCs.MechanicalCompanion.Census.SpawnCondition   出现条件未知（Census 集成用）
顺带补回被重写掉的 ArchivistSummon.Tooltip。
"""
import io
import re
import shutil

PATCH = r"E:\开发\WastelandSoulCN\Localization\zh-Hans_Mods.WastelandSoul.hjson"
BACKUP = r"E:\开发\.backup\WastelandSoulCN_zh-Hans.hjson"

ITEMS = [
    ("ArchivistSummon.DisplayName", u"归档者的索引"),
    ("ArchivistSummon.Tooltip", u"在恶魔或猩红祭坛上合成。把它引出来——它一直在回收你不该记起的东西。"),
]

CENSUS = [
    ("Census.SpawnCondition", u"出现条件未知"),
]


def has_key(text, key):
    return re.search(r"^\s*%s\s*:" % re.escape(key), text, re.M) is not None


def insert_after(text, anchor, entries):
    if anchor not in text:
        return text, []

    missing = [(k, v) for k, v in entries if not has_key(text, k)]

    if not missing:
        return text, []

    block = "".join(u"\t%s: %s\n" % (k, v) for k, v in missing)
    return text.replace(anchor, anchor + block, 1), [k for k, _ in missing]


def main():
    text = io.open(PATCH, encoding="utf-8").read()

    text, added_items = insert_after(text, "Items: {\n", ITEMS)
    text, added_census = insert_after(text, "MechanicalCompanion: {\n", CENSUS)

    io.open(PATCH, "w", encoding="utf-8").write(text)
    shutil.copyfile(PATCH, BACKUP)

    print("  Items 段补入:", added_items)
    print("  MechanicalCompanion 段补入:", added_census)
    print("  备份已刷新")


if __name__ == "__main__":
    main()
