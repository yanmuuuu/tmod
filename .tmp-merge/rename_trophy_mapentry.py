# -*- coding: utf-8 -*-
"""把迷你 Boss 奖杯的图块地图名统一成「锈爪齿轮奖杯 / Rusted Claw Trophy」（与新物品名一致）。"""
import io

EN = r"E:\开发\WastelandSoul\Localization\en-US_Mods.WastelandSoul.hjson"
TABLE = r"E:\开发\tools\sync_cn_translation.py"

en = io.open(EN, encoding="utf-8").read()
before = en
en = en.replace("ScrapReaperTrophy.MapEntry: Scrap Reaper Trophy",
                "ScrapReaperTrophy.MapEntry: Rusted Claw Trophy")

if en != before:
    io.open(EN, "w", encoding="utf-8", newline="\n").write(en)
    print("英文 MapEntry 已改")
else:
    print("英文 MapEntry 未匹配（可能已改）")

table = io.open(TABLE, encoding="utf-8").read()
old = u'"ScrapReaperTrophy.MapEntry": "废料收割者奖杯"'
new = u'"ScrapReaperTrophy.MapEntry": "锈爪齿轮奖杯"'

if old in table:
    io.open(TABLE, "w", encoding="utf-8", newline="\n").write(table.replace(old, new, 1))
    print("中文 MapEntry 已改")
else:
    print("中文 MapEntry 未匹配（可能已改）")
