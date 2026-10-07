# -*- coding: utf-8 -*-
"""一次性内容修改：掉落专属武器不再在介绍里写"仅从 XX 掉落袋开出"。

改两处：
  1. 主模组 en-US 本地化：删掉 `Dropped only from the ... Treasure Bag` 整行；
  2. `sync_cn_translation.py` 的译文表：同一条中文尾巴（`\\n仅从…掉落袋开出`）一起去掉，
     否则下次同步又会被写回中文文件里。
"""
import io
import re

EN = r"E:\开发\WastelandSoul\Localization\en-US_Mods.WastelandSoul.hjson"
TABLE = r"E:\开发\tools\sync_cn_translation.py"

# ---------------- 1) en-US ----------------
text = io.open(EN, encoding="utf-8").read()
lines = text.split("\n")
kept = [line for line in lines
        if not re.match(r"^\s*Dropped only from the .* Treasure Bag\s*$", line)]
io.open(EN, "w", encoding="utf-8", newline="\n").write("\n".join(kept))
print("en-US: 删除 %d 行" % (len(lines) - len(kept)))

# ---------------- 2) 译文表 ----------------
table = io.open(TABLE, encoding="utf-8").read()
table, count = re.subn(u"\\\\n仅从[^\"]*?掉落袋开出", u"", table)
io.open(TABLE, "w", encoding="utf-8", newline="\n").write(table)
print("译文表: 清理 %d 条" % count)
