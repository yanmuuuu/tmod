# -*- coding: utf-8 -*-
"""把瑜钢合金改成**更贴合原版放置方块**的画法（参照原版木材/砖块的贴图语言）：

原版放置方块看起来"成块"的秘密其实就三条，我们照抄：
  1. **接缝**：每格右/下各 1px 压暗（比底色暗一档），左/上各 1px 提亮 —— 铺开后自然出现网格；
  2. **低对比底纹**：底色上只做极轻的噪点（约 5%），不能像现在这样满屏花点；
  3. **点缀成簇**：青白不是均匀撒点，而是像原版矿石/宝石那样**成小簇**（1~2px 的簇心 + 周围一档暗色的晕），
     这样远看是"深色金属里嵌着几点青光"，而不是噪点墙。

保留：仍然是暗底 + 青白点（玩家要的），压条仍走同一套语言 + 一条银线。
"""
import io
import os
import subprocess
import sys

TOOL = r"E:\开发\tools\gen_fireplace_art.py"

FUNCS = '''def _seam(img, base, edge_dark, edge_light):
    """给 16x16 的方块加上原版那种"右/下压暗、左/上提亮"的接缝。"""
    pixels = img.load()

    for i in range(16):
        pixels[i, 15] = edge_dark
        pixels[15, i] = edge_dark
        pixels[i, 0] = edge_light
        pixels[0, i] = edge_light

    return img


def dot_block(base, edge, seed, bright=0.10, mid=0.16, dim=0.10, edge_shade=True):
    """暗底 + **成簇**的青白点缀 + 原版式接缝（不再是均匀撒点）。"""
    rng = random.Random(seed)
    img = Image.new("RGBA", (16, 16), base + (255,))
    pixels = img.load()

    # 1) 极轻的底纹（原版木头/石头也有这种低对比颗粒）
    for y in range(16):
        for x in range(16):
            if rng.random() < 0.06:
                shade = -5 if rng.random() < 0.5 else 6
                pixels[x, y] = tuple(max(0, min(255, c + shade)) for c in base) + (255,)

    # 2) 成簇的青白点缀：先挑簇心，再在周围撒 2~4 个同簇点
    clusters = 3 if bright + mid + dim > 0.3 else 2

    for _ in range(clusters):
        cx = rng.randrange(1, 15)
        cy = rng.randrange(1, 15)
        pixels[cx, cy] = DOT_BRIGHT + (255,)

        for _ in range(rng.randrange(2, 5)):
            nx = min(14, max(1, cx + rng.randrange(-1, 2)))
            ny = min(14, max(1, cy + rng.randrange(-1, 2)))
            pixels[nx, ny] = (DOT_MID if rng.random() < 0.6 else DOT_DIM) + (255,)

        # 簇的一圈留一点点暗晕，远看才有"嵌进去"的感觉
        for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2)):
            nx, ny = cx + dx, cy + dy

            if 1 <= nx <= 14 and 1 <= ny <= 14 and pixels[nx, ny][:3] == base:
                pixels[nx, ny] = DOT_DIM + (200,)

    if edge_shade:
        _seam(img, base, edge + (255,), tuple(min(255, c + 14) for c in base) + (255,))

    return img


def yugang_alloy():
    """瑜钢合金本体：暗底 + 成簇青白点缀 + 原版式接缝。"""
    return dot_block(ALLOY_BASE, ALLOY_EDGE, seed=20261008, bright=0.10, mid=0.18, dim=0.11)


def yugang_trim():
    """压条：同一套语言，底色略亮，中间横贯一条银线。"""
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
start = text.index("def dot_block(")
end = text.index("def gas_poison_icon(")
text = text[:start] + FUNCS + "\n\n" + text[end:]
io.open(TOOL, "w", encoding="utf-8", newline="\n").write(text)
print("已把贴图改成原版式画法（接缝 + 底纹 + 成簇点缀）")

proc = subprocess.run([sys.executable, TOOL], capture_output=True, text=True, encoding="utf-8", errors="replace")
for line in (proc.stdout or "").strip().split("\n"):
    if "Yugang" in line or "预览" in line:
        print("  " + line.strip())

if proc.returncode != 0:
    print((proc.stderr or "")[-400:])
