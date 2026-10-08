# -*- coding: utf-8 -*-
"""按玩家要求改瑜钢合金贴图：**青白色以点状随机分布**（暗底 + 随机青白点）。

同时把压条也改成同一套点状语言（底色略亮 + 一条银线），保持两者是"同一种材料"。
只改 tools/gen_fireplace_art.py 里的配色与绘制函数，然后重新生成那两张 PNG。
"""
import io
import os
import subprocess
import sys

TOOL = r"E:\开发\tools\gen_fireplace_art.py"

NEW_BLOCK = '''# 瑜钢合金配色：**黑曜石般的暗底 + 随机分布的青白点**（玩家要求"青白以点状随机分布"）
ALLOY_BASE = (26, 30, 36)
ALLOY_EDGE = (16, 19, 24)
DOT_BRIGHT = (226, 250, 252)
DOT_MID = (168, 232, 238)
DOT_DIM = (98, 152, 162)

TRIM_BASE = (42, 56, 64)
TRIM_EDGE = (24, 32, 38)
'''

NEW_FUNCS = '''def dot_block(base, edge, seed, bright=0.10, mid=0.16, dim=0.10, edge_shade=True):
    """暗底 + 随机青白点。点数密度由 bright/mid/dim 三个阈值控制。"""
    rng = random.Random(seed)
    img = Image.new("RGBA", (16, 16), base + (255,))
    pixels = img.load()

    for y in range(16):
        for x in range(16):
            roll = rng.random()

            if roll < bright:
                pixels[x, y] = DOT_BRIGHT + (255,)
            elif roll < bright + mid:
                pixels[x, y] = DOT_MID + (255,)
            elif roll < bright + mid + dim:
                pixels[x, y] = DOT_DIM + (255,)

    if edge_shade:
        for i in range(16):
            pixels[i, 0] = edge + (255,)
            pixels[0, i] = edge + (255,)
            pixels[i, 15] = edge + (255,)
            pixels[15, i] = edge + (255,)

    return img


def yugang_alloy():
    """瑜钢合金本体：暗底 + 点状随机青白（方块之间靠边缘压暗区分）。"""
    return dot_block(ALLOY_BASE, ALLOY_EDGE, seed=20261008, bright=0.10, mid=0.18, dim=0.11)


def yugang_trim():
    """压条：同一套点状语言，底色略亮，中间横贯一条银线 —— 铺成条带时仍是"青银相间"。"""
    img = dot_block(TRIM_BASE, TRIM_EDGE, seed=77002, bright=0.16, mid=0.22, dim=0.10, edge_shade=False)
    draw = ImageDraw.Draw(img)

    for x in range(16):
        draw.point((x, 2), fill=DOT_BRIGHT + (255,))
        draw.point((x, 3), fill=DOT_MID + (255,))
        draw.point((x, 12), fill=(198, 226, 234, 255))
        draw.point((x, 13), fill=DOT_BRIGHT + (255,))

    return img
'''

text = io.open(TOOL, encoding="utf-8").read()

# 1) 替换配色常量块
start = text.index("# 瑜钢合金配色")
end = text.index("def noise_block(")
text = text[:start] + NEW_BLOCK + "\n\n" + text[end:]

# 2) 替换 yugang_alloy / yugang_trim 两个函数（到 gas_poison_icon 之前）
start = text.index("def yugang_alloy(")
end = text.index("def gas_poison_icon(")
text = text[:start] + NEW_FUNCS + "\n\n" + text[end:]

io.open(TOOL, "w", encoding="utf-8", newline="\n").write(text)
print("已更新 gen_fireplace_art.py（点状青白）")

# 3) 重新生成（只关心那两张图 + 预览）
python = sys.executable
proc = subprocess.run([python, TOOL], capture_output=True, text=True, encoding="utf-8", errors="replace")
for line in (proc.stdout or "").strip().split("\n"):
    if "Yugang" in line or "预览" in line:
        print("  " + line.strip())
if proc.returncode != 0:
    print((proc.stderr or "")[-400:])
