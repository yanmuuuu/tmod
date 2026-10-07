# -*- coding: utf-8 -*-
"""给译文表补上四首 Boss 曲的中文显示名（键名 = Music/ 下的文件名）。"""
import io

PATH = r"E:\开发\tools\sync_cn_translation.py"
ANCHOR = u'    "CompanionArrived": "智械人{0}已到达。",'

ADD = u"""
    # 四首 Boss 曲的显示名（键名必须和 Music/ 下的文件名一致）
    "Music.Scavenger": "执行单元-07",
    "Music.Archivist": "审计单元",
    "Music.AshHeart": "不曾熄灭的心",
    "Music.FireplaceGuardian": "最后一道门","""

text = io.open(PATH, encoding="utf-8").read()

if "Music.Scavenger" in text:
    print("已经补过了，跳过")
else:
    assert ANCHOR in text, "找不到锚点"
    text = text.replace(ANCHOR, ANCHOR + ADD, 1)
    io.open(PATH, "w", encoding="utf-8", newline="\n").write(text)
    print("译文表已补 4 条音乐名")
