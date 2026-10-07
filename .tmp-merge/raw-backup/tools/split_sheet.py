# -*- coding: utf-8 -*-
"""把「一整张 contact sheet」拆成一个个独立的素材（AI 出图常把多个姿态/道具画在一张图上）。

为什么不能按固定网格切：
  * 用户的图里**带文字标签**（`hover high` / `Warrior (44x44)` / 文件名…），文字在格子外面；
  * 素材本身可能**由多块组成**（召唤师法杖周围飘着光球、灵魂碎片周围有星点），
    它们是独立的连通块，按"面积取前 N 大"会把光球和星点丢掉。

所以流程是：
  1. 抠品红得到 alpha 掩码；
  2. 连通块标记（PIL 的 C 实现 floodfill，够快）；
  3. 面积大的块当"主干"（每个素材至少有一个），按 y 分行、行内按 x 排序，
     取**期望数量**个主干；
  4. 每个主干按包围盒外扩一定比例，把落在这个范围内的**小块**（光球/星点/火星）吸收进来；
     文字标签落在格子外，不会被吸收；
  5. 输出每个素材的裁剪图 + 一张带编号的对照图，人工核对。

用法：
    python split_sheet.py <sheet.png> <期望数量> <输出目录> [--pad 0.25] [--minshare 0.002]
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from art_common import bleed_fix, key_magenta  # noqa: E402


def components(image, alpha_min=14):
    """连通块标记：返回 [{bbox, area, center}]（bbox = 左, 上, 右, 下）。

    ⚠️ 用 **RGB** 掩码做 floodfill，不要用 "L"：Pillow 12 的
    `ImageDraw.floodfill` 在 "L" 图上什么都不填（实测 count==0），RGB 才正常。
    """
    alpha = np.asarray(image)[..., 3]
    mask = ((alpha > alpha_min) * 255).astype(np.uint8)
    work = Image.fromarray(np.dstack([mask, mask, mask]), "RGB")
    data = mask.copy()
    found = []
    ink = (128, 128, 128)

    while True:
        hits = np.argwhere(data > 0)

        if not len(hits):
            break

        seed = (int(hits[0][1]), int(hits[0][0]))
        ImageDraw.floodfill(work, seed, ink, thresh=0)
        filled = np.asarray(work)[..., 0] == 128
        ys, xs = np.nonzero(filled)

        found.append({
            "bbox": (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1),
            "area": int(filled.sum()),
        })

        data[filled] = 0
        work = Image.fromarray(np.dstack([data, data, data]), "RGB")

    for item in found:
        left, top, right, bottom = item["bbox"]
        item["center"] = ((left + right) / 2.0, (top + bottom) / 2.0)

    return found


def group_rows(items, gap_ratio=0.6):
    """按 y 中心分行：相邻中心差超过「中位高度 × gap_ratio」就换行。"""
    if not items:
        return []

    heights = sorted(item["bbox"][3] - item["bbox"][1] for item in items)
    median_height = heights[len(heights) // 2]
    threshold = median_height * gap_ratio

    ordered = sorted(items, key=lambda item: item["center"][1])
    rows = [[ordered[0]]]

    for item in ordered[1:]:
        if abs(item["center"][1] - rows[-1][-1]["center"][1]) > threshold:
            rows.append([])

        rows[-1].append(item)

    for row in rows:
        row.sort(key=lambda item: item["center"][0])

    return rows


def split(image, count, absorb=10.0, min_share=0.03):
    """拆出 count 个素材，返回 (抠好的整图, [(bbox, 说明)])。

    规则（都是按真实素材调出来的）：
      * **主干** = 面积 ≥ 最大块 3% 的连通块（文字字形通常 < 1%，光球/星点 < 3%）；
      * 面积最大的 count 个主干当**种子**（每个素材至少有一个主干）；
      * 其余所有块按「到最近种子的包围盒间距」归并，间距 ≤ absorb 像素才吸收
        （武器图标周围的光球间距 0~6、灵魂碎片周围星点 2~30、文字标签 ≥ 27，
         所以 10 像素是个安全的阈值；整块并进来只有当它比种子小很多时才做）。
    """
    image = key_magenta(image)
    image = bleed_fix(image)

    found = components(image)

    if len(found) < count:
        raise SystemExit("!! 只找到 %d 个连通块，少于期望的 %d 个" % (len(found), count))

    biggest = max(item["area"] for item in found)
    order = sorted(range(len(found)), key=lambda i: -found[i]["area"])
    seed_ids = order[:count]
    seeds = [found[i] for i in seed_ids]

    for seed in seeds:
        seed["members"] = [seed]

    for index in order[count:]:
        item = found[index]
        best, best_gap = None, None

        for seed in seeds:
            gap = box_gap(seed["bbox"], item["bbox"])

            if best_gap is None or gap < best_gap:
                best, best_gap = seed, gap

        if best_gap is not None and best_gap <= absorb and item["area"] < best["area"] * 0.5:
            best["members"].append(item)

    rows = group_rows(seeds)
    flat = [seed for row in rows for seed in row]

    groups = []

    for seed in flat:
        members = seed["members"]
        box = (
            min(item["bbox"][0] for item in members),
            min(item["bbox"][1] for item in members),
            max(item["bbox"][2] for item in members),
            max(item["bbox"][3] for item in members),
        )
        groups.append((box, "body %dpx + %d satellites" % (seed["area"], len(members) - 1)))

    return image, groups


def box_gap(a, b):
    """两个包围盒之间的间距（相交/相邻为 0）。"""
    import math
    dx = max(0, max(a[0] - b[2], b[0] - a[2]))
    dy = max(0, max(a[1] - b[3], b[1] - a[3]))
    return math.hypot(dx, dy)


def main():
    if len(sys.argv) < 4:
        print(__doc__)
        return 2

    path, count, outdir = sys.argv[1], int(sys.argv[2]), sys.argv[3]
    absorb = 10.0

    if "--absorb" in sys.argv:
        absorb = float(sys.argv[sys.argv.index("--absorb") + 1])

    os.makedirs(outdir, exist_ok=True)

    image, groups = split(Image.open(path).convert("RGBA"), count, absorb)
    crops = []

    for index, (box, note) in enumerate(groups):
        crop = image.crop(box)
        crop.save(os.path.join(outdir, "cell_%02d.png" % index))
        crops.append(crop)
        print("  cell_%02d  box=%s  %dx%d  %s" % (index, box, crop.width, crop.height, note))

    zoom = 2
    pad_px = 10
    tiles = [crop.resize((crop.width * zoom, crop.height * zoom), Image.NEAREST) for crop in crops]
    width = sum(tile.width for tile in tiles) + pad_px * (len(tiles) + 1)
    height = max(tile.height for tile in tiles) + pad_px * 2

    sheet = Image.new("RGBA", (width, height), (30, 32, 38, 255))
    drawer = ImageDraw.Draw(sheet)
    x = pad_px

    for index, tile in enumerate(tiles):
        sheet.alpha_composite(tile, (x, pad_px))
        drawer.text((x + 4, pad_px + 2), str(index), fill=(255, 220, 90, 255))
        x += tile.width + pad_px

    preview = os.path.join(outdir, "cells_preview.png")
    sheet.save(preview)
    print("  对照图: %s" % preview)
    return 0


if __name__ == "__main__":
    sys.exit(main())
