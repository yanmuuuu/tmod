# -*- coding: utf-8 -*-
"""壁炉入口定稿微调（3 处）：
1) 两侧海洋的列范围往岸边挪：距世界边界 70~150 格 → 200~300 格（原版海洋判定是 <340 格，
   所以仍在海洋里，但水更浅，门能立在海平面/水面上，而不是沉到海床）；
2) 英文提示 Messages.GatePlaced 不再说"你醒来的地方附近"（门已经搬到两侧海洋）；
3) 汉化译文表里对应那条中文同步改（CN 补丁由流水线重新生成）。
"""
import io
import os
import re

MOD = r"E:\开发\WastelandSoul"
GATE = os.path.join(MOD, r"Common\Systems\FireplaceGateSystem.cs")
EN = os.path.join(MOD, r"Localization\en-US_Mods.WastelandSoul.hjson")
CN_TABLE = r"E:\开发\tools\sync_cn_translation.py"


def report(name, changed):
    print("  %-28s %s" % (name, changed))


# ---------- 1) 常量：往岸边挪 ----------
text = io.open(GATE, encoding="utf-8-sig").read()
original = text

for const, old, new in (("OceanInsetFar", 70, 200), ("OceanInsetNear", 150, 300)):
    pattern = re.compile(r"(%s\s*=\s*)%d\b" % (const, old))

    if pattern.search(text):
        text = pattern.sub(lambda m: m.group(1) + str(new), text, count=1)
        report(const, "%d -> %d" % (old, new))
    else:
        found = re.search(r"%s\s*=\s*([0-9]+)" % const, text)
        report(const, "未按预期匹配（当前 %s），未改动" % (found.group(1) if found else "找不到"))

if text != original:
    io.open(GATE, "w", encoding="utf-8-sig", newline="\r\n").write(text)

# ---------- 2) 英文提示 ----------
en_text = io.open(EN, encoding="utf-8-sig").read()
m = re.search(r"(\n\s*GatePlaced\s*:\s*)([^\n]+)", en_text)

if m:
    old_en = m.group(2).strip()
    new_en = '"A warm door has opened at the sea\'s edge on both sides of the world. The Fireplace is listening."'
    en_text = en_text[:m.start(2)] + new_en + en_text[m.end(2):]
    io.open(EN, "w", encoding="utf-8-sig", newline="\r\n").write(en_text)
    report("Messages.GatePlaced (EN)", "已改为海面两侧的文案（原：%s）" % old_en[:40])
else:
    report("Messages.GatePlaced (EN)", "找不到键，未改动")

# ---------- 3) 汉化译文表 ----------
cn_text = io.open(CN_TABLE, encoding="utf-8-sig").read()
old_cn = "你醒来的地方附近开了一扇暖门。壁炉在听。"
new_cn = "世界两侧的海面上各开了一扇暖门。壁炉在听。"

if old_cn in cn_text:
    cn_text = cn_text.replace(old_cn, new_cn, 1)
    io.open(CN_TABLE, "w", encoding="utf-8-sig", newline="\r\n").write(cn_text)
    report("CN 译文表 GatePlaced", "已同步中文文案")
else:
    report("CN 译文表 GatePlaced", "旧中文文本未找到（可能已被改写），未改动")
