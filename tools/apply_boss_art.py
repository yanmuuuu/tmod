# -*- coding: utf-8 -*-
"""把 `art-inbox\raw\banners_trophies.png` 切成 4 面 Boss 旗帜 + 5 个奖杯，并生成模组贴图。

为什么不能按固定网格切（这张图的真实结构）：
  * 上排 4 格（旗帜）、下排 5 格（奖杯）——**两排格子宽度不同**，
    而且每格之间只有品红空隙、格子本身宽窄不一（旗帜 304~327px、奖杯 197~251px）；
  * 所以按「行投影分行 → 行内列投影分格 → 每格取 bbox」来切，
  切完**自己核对数量必须是 4 + 5**，数量对不上直接报错退出，不写出任何贴图。

抠图 / 渗色修复 / 缩放全部复用 `art_common.py`（与 `apply_art.py`、`split_sheet.py` 同一套实现），
不另起一套：`key_magenta`（min(R,B)-G 打分）→ `bleed_fix`（半透明像素取邻近不透明色，去品红溢）
→ 等比缩放（缩小用 BOX）→ `threshold_alpha`（< 32 直接透明）→ `quantize`（调色板量化）。

尺寸（逐个核过，见下面两张常量表的注释）：
  * 奖杯图块：`54x48` —— 与工程**既有 3x3 图块**（FireplaceGate / FireplaceTerminal /
    FireplaceExit / ElvenFrame，全是 54x48）一致；`CoordinateHeights = {16,16,16}`。
    注意：3x3 的槽位是「每行 16px + 行间 2px padding」，所以第 3 行的起点在贴图 y=36，
    16px 高要到 y=52 —— 贴图只有 48 高，最后 4px 会被采样钳制（重复贴图最后一行）。
    实测这 5 个奖杯缩放后**最后一行本来就是一条近乎纯色、亮度 7~15 的深色描边**
    （std < 3），钳制出来就是底座下沿的一条暗边，看不出是钳制 ——
    见 `preview_boss_art_game.png`（按 TileObjectData 取帧规则模拟游戏绘制）。
  * 旗帜图块：`18x34` —— 原版敌人旗帜是 `TileObjectData.Style1x2Top`（1 格宽、2 格高），
    槽位 = 16 宽 + 2 padding，两行各 16 高 + 中间 2px padding = 34 高，**精确对齐、没有钳制**。
  * 物品图标：奖杯 32x32、旗帜 24x32（都从**同一块素材**直接缩放，不从图块二次缩放）。

用法：
    python E:\\开发\\tools\\apply_boss_art.py
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from art_common import (  # noqa: E402
    bleed_fix, key_magenta, quantize, scale_into, threshold_alpha,
)

INBOX = r"E:\开发\art-inbox"
SOURCE = os.path.join(INBOX, "raw", "banners_trophies.png")
SPLIT_PREVIEW = os.path.join(INBOX, "preview_banners_trophies_split.png")
GAME_PREVIEW = os.path.join(INBOX, "preview_boss_art_game.png")
CELL_DIR = os.path.join(INBOX, "cells", "banners_trophies")
MOD_ROOT = r"E:\开发\WastelandSoul"

# 上排 4 格 = 4 面 Boss 旗帜（左 → 右）；下排 5 格 = 5 个奖杯（左 → 右，第 5 个是迷你 Boss）
# 每项 = (贴图/物品的类名, 中文名，只用于日志与预览标注)
TOP_ROW = [
    ("ScavengerBanner", u"清道夫旗帜"),
    ("ArchivistBanner", u"归档者旗帜"),
    ("AshHeartBanner", u"灰烬之心旗帜"),
    ("FireplaceGuardianBanner", u"壁炉守卫旗帜"),
]
BOTTOM_ROW = [
    ("ScavengerTrophy", u"清道夫奖杯（机械头骨）"),
    ("ArchivistTrophy", u"归档者奖杯（审计面具）"),
    ("AshHeartTrophy", u"灰烬之心奖杯（燃烧心脏）"),
    ("FireplaceGuardianTrophy", u"壁炉守卫奖杯（金属门）"),
    ("ScrapReaperTrophy", u"迷你 Boss 废料收割者奖杯（锈爪齿轮）"),
]

TROPHY_TILE_SIZE = (54, 48)     # 3x3 图块，与工程既有 3x3 贴图一致
BANNER_TILE_SIZE = (18, 34)     # Style1x2Top 槽位（16+2 宽；16+2+16 高）
TROPHY_ICON_SIZE = (32, 32)
BANNER_ICON_SIZE = (24, 32)

TROPHY_TILE_COLORS = 48
BANNER_TILE_COLORS = 32
ICON_COLORS = 40

FILL = 1.0                      # 奖杯撑满画布高度：这样底部钳制取到的是底座最后一行，而不是透明
ALPHA_MIN = 32


# ======================================================================================
# 切分
# ======================================================================================

def ink_mask(image, alpha_min=ALPHA_MIN):
    """不透明掩码（抠图之后的 alpha 阈值）。"""
    return np.asarray(image)[..., 3] > alpha_min


def runs(profile):
    """一维投影的连续非空段，返回 [(起点, 终点)]（闭区间）。"""
    found = []
    start = None

    for index, value in enumerate(profile):
        if value > 0 and start is None:
            start = index
        elif value == 0 and start is not None:
            found.append((start, index - 1))
            start = None

    if start is not None:
        found.append((start, len(profile) - 1))

    return found


def split_sheet(image, row_counts):
    """按「行投影 → 行内列投影 → bbox」切图。

    `row_counts` = 每一行期望的格数（本图是 [4, 5]）。返回 [(行号, 列号, bbox)]。
    数量对不上直接 SystemExit（宁可报错也不要写出一堆错位的贴图）。
    """
    mask = ink_mask(image)
    rows = runs(mask.sum(axis=1))

    if len(rows) != len(row_counts):
        raise SystemExit("!! 行投影切出 %d 行，期望 %d 行：%s" % (len(rows), len(row_counts), rows))

    blocks = []

    for row_index, (expected, (top, bottom)) in enumerate(zip(row_counts, rows)):
        band = mask[top:bottom + 1]
        columns = runs(band.sum(axis=0))

        if len(columns) != expected:
            raise SystemExit("!! 第 %d 行切出 %d 格，期望 %d 格：%s"
                             % (row_index, len(columns), expected, columns))

        for column_index, (left, right) in enumerate(columns):
            # 每格再取自己的纵向 bbox（同排各格高度不完全一样）
            inner = band[:, left:right + 1]
            vertical = runs(inner.sum(axis=1))
            box = (left, top + vertical[0][0], right + 1, top + vertical[-1][1] + 1)
            blocks.append((row_index, column_index, box))

    return blocks


def make_split_preview(image, blocks, labels, titles):
    """切分对照图：两排格子按原顺序摆好，每格上方标注「行.列 中文名 类名 尺寸」。"""
    zoom = 3
    padding = 16
    label_height = 26
    cells = []
    font = label_font(18)

    for row_index, column_index, box in blocks:
        crop = image.crop(box)
        tile = crop.resize((crop.width * zoom, crop.height * zoom), Image.NEAREST)
        cells.append((row_index, tile, "%d.%d  %s  %s  %dx%d"
                      % (row_index, column_index, titles[(row_index, column_index)],
                         labels[(row_index, column_index)], crop.width, crop.height)))

    row_ids = sorted({item[0] for item in cells})
    row_widths = []

    for row_index in row_ids:
        width = sum(item[1].width for item in cells if item[0] == row_index)
        count = sum(1 for item in cells if item[0] == row_index)
        row_widths.append(width + padding * (count + 1))

    row_heights = []

    for row_index in row_ids:
        height = max(item[1].height for item in cells if item[0] == row_index)
        row_heights.append(height + padding * 2 + label_height)

    sheet = Image.new("RGBA", (max(row_widths), sum(row_heights)), (30, 32, 38, 255))
    drawer = ImageDraw.Draw(sheet)
    offset_y = 0

    for slot, row_index in enumerate(row_ids):
        x = padding

        for item_row, tile, label in cells:
            if item_row != row_index:
                continue

            sheet.alpha_composite(tile, (x, offset_y + padding + label_height))
            drawer.text((x, offset_y + padding), label, fill=(255, 220, 90, 255), font=font)
            x += tile.width + padding

        offset_y += row_heights[slot]

    sheet.save(SPLIT_PREVIEW)
    print("  切分对照图: %s  (%dx%d)" % (SPLIT_PREVIEW, sheet.width, sheet.height))


# ======================================================================================
# 缩放与输出
# ======================================================================================

def label_font(size=18):
    """预览标注用的字体：优先中文字体（预览里要写中文名），退到 Arial，再退到位图字体。"""
    for path in (r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf",
                 r"C:\Windows\Fonts\arial.ttf"):
        if os.path.exists(path):
            try:
                return ImageFont.truetype(path, size)
            except OSError:
                continue

    return ImageFont.load_default()


def stretch_box(image, size):
    """非等比 BOX 缩放。

    旗帜素材是「竖杆 + 向右飘的旗面」，整体接近正方形（~304x327），
    而敌人旗帜的槽位是 1 格宽 2 格高（18x34）。等比缩放会把旗子缩成中间一小块、
    上下留空，所以这里按槽位直接铺满（BOX = 面积平均，缩小最干净，硬边不糊）。
    """
    return image.resize(size, Image.BOX)


def prepare(image, size, colors, mode):
    """抠好的整图 → 目标画布（mode = fit / stretch）。"""
    if mode == "stretch":
        scaled = stretch_box(image, size)
    else:
        scaled, _placed = scale_into(image, size, FILL)

    scaled = threshold_alpha(scaled)
    return quantize(scaled, colors)


def write(image, relative_path):
    path = os.path.join(MOD_ROOT, relative_path)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    image.save(path)
    print("  %-58s %dx%d" % (relative_path, image.width, image.height))
    return path


# ======================================================================================
# 模拟游戏里的绘制（核对「贴图 → 屏幕」这一步没有钳制痕迹）
# ======================================================================================

def simulate_tile_draw(image, heights, padding=2, tile_size=16):
    """按 TileObjectData 的取帧规则模拟游戏绘制：第 j 行画在屏幕 y = j*16，
    取贴图 y = 前面各行 (高 + padding) 之和；越界时**重复最后一行**（clamp 采样）。

    用途：3x3 图块的贴图只有 48 高、而槽位要到 52，肉眼确认钳制出来的那 4px 干净。
    """
    total = tile_size * (len(heights) - 1) + heights[-1]
    canvas = Image.new("RGBA", (image.width, total), (0, 0, 0, 0))
    array = np.asarray(image)
    sheet_y = 0

    for index, height in enumerate(heights):
        dest_y = index * tile_size

        for step in range(height):
            source = min(sheet_y + step, image.height - 1)
            canvas.paste(Image.fromarray(array[source:source + 1], "RGBA"), (0, dest_y + step))

        sheet_y += height + padding

    return canvas


def make_game_preview(samples):
    """把「模拟游戏绘制」的结果放大摆一排，肉眼核对底部钳制与整体观感。"""
    zoom = 5
    padding = 24
    label_height = 44
    font = label_font(18)
    tiles = [(image.resize((image.width * zoom, image.height * zoom), Image.NEAREST), label)
             for label, image in samples]

    width = sum(tile.width + padding for tile, _label in tiles) + padding
    height = max(tile.height for tile, _label in tiles) + padding * 2 + label_height
    sheet = Image.new("RGBA", (width, height), (34, 36, 44, 255))
    drawer = ImageDraw.Draw(sheet)
    x = padding

    for tile, label in tiles:
        sheet.alpha_composite(tile, (x, padding + label_height))
        drawer.multiline_text((x, padding), label, fill=(255, 220, 90, 255), font=font, spacing=2)
        x += tile.width + padding

    sheet.save(GAME_PREVIEW)
    print("  模拟绘制对照图: %s" % GAME_PREVIEW)


# ======================================================================================

def main():
    if not os.path.exists(SOURCE):
        raise SystemExit("!! 找不到素材: %s" % SOURCE)

    source = Image.open(SOURCE).convert("RGBA")
    print("素材: %s  %dx%d" % (SOURCE, source.width, source.height))

    # 抠图 + 渗色修复（整张图做一次，再按格裁；与 split_sheet.py 的做法一致）
    keyed = bleed_fix(key_magenta(source))

    blocks = split_sheet(keyed, [len(TOP_ROW), len(BOTTOM_ROW)])
    print("切分: 上排 %d 格 + 下排 %d 格" % (sum(1 for row, _c, _b in blocks if row == 0),
                                             sum(1 for row, _c, _b in blocks if row == 1)))

    labels = {}
    titles = {}

    for row_index, column_index, box in blocks:
        name = (TOP_ROW if row_index == 0 else BOTTOM_ROW)[column_index]
        labels[(row_index, column_index)] = name[0]
        titles[(row_index, column_index)] = name[1]
        print("  %d.%d  %-24s %-18s box=%s" % (row_index, column_index, name[0], name[1], box))

    os.makedirs(CELL_DIR, exist_ok=True)

    for row_index, column_index, box in blocks:
        keyed.crop(box).save(os.path.join(CELL_DIR, "%d_%d_%s.png"
                                         % (row_index, column_index, labels[(row_index, column_index)])))

    make_split_preview(keyed, blocks, labels, titles)

    print("奖杯图块 %dx%d / 旗帜图块 %dx%d / 图标 %s %s"
          % (TROPHY_TILE_SIZE + BANNER_TILE_SIZE + (TROPHY_ICON_SIZE, BANNER_ICON_SIZE)))

    samples = []

    for row_index, column_index, box in blocks:
        crop = keyed.crop(box)
        name = (TOP_ROW if row_index == 0 else BOTTOM_ROW)[column_index][0]
        is_banner = row_index == 0

        if is_banner:
            tile = prepare(crop, BANNER_TILE_SIZE, BANNER_TILE_COLORS, "stretch")
            icon = prepare(crop, BANNER_ICON_SIZE, ICON_COLORS, "stretch")
            write(tile, os.path.join("Content", "Tiles", name + ".png"))
            write(icon, os.path.join("Content", "Items", "Decor", name + ".png"))
            samples.append(("%s\n旗帜 18x34" % name[:-len("Banner")], simulate_tile_draw(tile, [16, 16])))
        else:
            tile = prepare(crop, TROPHY_TILE_SIZE, TROPHY_TILE_COLORS, "fit")
            icon = prepare(crop, TROPHY_ICON_SIZE, ICON_COLORS, "fit")
            write(tile, os.path.join("Content", "Tiles", name + ".png"))
            write(icon, os.path.join("Content", "Items", "Decor", name + ".png"))
            samples.append(("%s\n奖杯 54x48" % name[:-len("Trophy")], simulate_tile_draw(tile, [16, 16, 16])))

    make_game_preview(samples)
    print("完成：9 个图块贴图 + 9 个物品图标")
    return 0


if __name__ == "__main__":
    sys.exit(main())
