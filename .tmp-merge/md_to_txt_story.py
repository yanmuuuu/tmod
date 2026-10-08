# -*- coding: utf-8 -*-
"""把 故事线.md 转成**纯文本**（发给别人看的版本）：去掉所有 Markdown 标记，表格改成缩进式条目。

输出：E:\\开发\\废土魂穿_故事线.txt（UTF-8 带 BOM，Windows 记事本/微信/QQ 打开都不乱码）
"""
import io
import os
import re

SRC = r"E:\开发\WastelandSoul\故事线.md"
DST = r"E:\开发\废土魂穿_故事线.txt"

text = io.open(SRC, encoding="utf-8").read()
out = []

for raw in text.split("\n"):
    line = raw.rstrip()

    # 表格分隔行丢掉
    if re.match(r"^\s*\|[\s:|-]+\|\s*$", line):
        continue

    # 表格行 -> "  左：右"
    if line.strip().startswith("|") and line.strip().endswith("|"):
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        cells = [c for c in cells if c != ""]

        if len(cells) >= 2:
            head, body = cells[0], "  ".join(cells[1:])
            body = body.replace("<br/>", "\n      ")
            out.append("  · %s —— %s" % (head, body))
        elif cells:
            out.append("  " + cells[0])

        continue

    # 标题
    if line.startswith("# "):
        title = line[2:].strip()
        out.append(title)
        out.append("=" * max(8, len(title) * 2))
        out.append("")
        continue

    if line.startswith("## "):
        title = line[3:].strip()
        out.append("")
        out.append("─" * 46)
        out.append("【%s】" % title)
        out.append("─" * 46)
        continue

    if line.startswith("### "):
        out.append("")
        out.append("◆ " + line[4:].strip())
        continue

    # 引用块
    if line.startswith(">"):
        line = "  " + line.lstrip("> ").rstrip()

    # 行内标记
    line = line.replace("<br/>", "\n")
    line = re.sub(r"\*\*(.+?)\*\*", r"\1", line)
    line = re.sub(r"`(.+?)`", r"\1", line)

    out.append(line)

plain = "\n".join(out)
plain = re.sub(r"\n{3,}", "\n\n", plain).strip() + "\n"

# UTF-8 带 BOM（记事本/手机端最稳）
with io.open(DST, "wb") as handle:
    handle.write(b"\xef\xbb\xbf" + plain.encode("utf-8"))

print("已生成：%s（%.1f KB，%d 行）" % (DST, os.path.getsize(DST) / 1024.0, plain.count("\n")))
print("---- 前 24 行预览 ----")
for i, line in enumerate(plain.split("\n")[:24], 1):
    print("%3d| %s" % (i, line[:76]))
