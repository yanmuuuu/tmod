# -*- coding: utf-8 -*-
"""装备扩充包（护甲 3 套 + 饰品 12 件 + 2 个饰品弹幕）的程序化贴图生成。

尺寸约定（与游戏内碰撞盒严格对应，改动前先看这里）：
    护甲图标            22 x 22
    护甲穿身帧表        40 x 1120   = 20 帧 x 56 高（_Head / _Body / _Legs）
    饰品图标            24 x 24
    余烬碎片弹幕        14 x 14
    冷火环弹幕          40 x 40

帧表布局沿用原版玩家骨架的锚点（和 tools/gen_armor_equip.py 一致）：
    头 8..20 / 身 20..36 / 腿 35..53，相邻部位重叠 1 像素，穿起来不会露出身体。

注意：本脚本只写自己新增的文件，不删除、不覆盖任何别人生成的美术资源。
"""
import math
import os
import sys

from PIL import Image, ImageDraw

ROOT = r"E:\开发\WastelandSoul"
ARMOR_DIR = os.path.join(ROOT, r"Content\Items\Armor")
ACC_DIR = os.path.join(ROOT, r"Content\Items\Accessories\Gear")
PROJ_DIR = os.path.join(ROOT, r"Content\Projectiles\Gear")
INBOX = r"E:\开发\art-inbox"

FRAME_W, FRAME_H, FRAMES = 40, 56, 20
BOB = (0, 0, 1, 1, 0, 0, -1, -1)

BOX_HEAD = (13, 8, 27, 20)
BOX_BODY = (11, 20, 29, 36)
BOX_LEGS = (13, 35, 27, 53)

# 三套护甲的配色：(描边, 主色, 亮色, 点缀色)
SETS = {
    "ScavengerGrace": ((34, 40, 34), (96, 104, 86), (168, 178, 150), (196, 168, 72)),
    "AshHeart": ((52, 22, 14), (152, 56, 32), (236, 128, 54), (255, 206, 130)),
    "HearthGuard": ((26, 42, 58), (96, 140, 176), (196, 228, 248), (226, 244, 255)),
}

CREATED = []


def save(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    CREATED.append((path, img.size))
    print("wrote %-64s %s" % (os.path.relpath(path, ROOT), img.size))


def blank(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


# ============================================================ 护甲图标（22x22）

def draw_armor_icon(kind, palette):
    outline, main, light, accent = palette
    img = blank(22, 22)
    d = ImageDraw.Draw(img)

    if kind == "Head":
        d.rounded_rectangle((4, 4, 18, 18), radius=4, fill=main, outline=outline)
        d.rectangle((6, 10, 16, 15), fill=outline)
        d.line((6, 6, 16, 6), fill=light)
        d.point((7, 7), fill=accent)
    elif kind == "Body":
        d.rounded_rectangle((4, 3, 18, 19), radius=3, fill=main, outline=outline)
        d.rectangle((3, 3, 7, 9), fill=main, outline=outline)      # 左肩甲
        d.rectangle((15, 3, 19, 9), fill=main, outline=outline)    # 右肩甲
        d.rectangle((5, 5, 17, 8), fill=light)
        d.rectangle((10, 8, 12, 18), fill=accent)
        d.line((5, 16, 17, 16), fill=outline)                      # 腰带
    else:
        d.rounded_rectangle((4, 3, 10, 19), radius=2, fill=main, outline=outline)
        d.rounded_rectangle((12, 3, 18, 19), radius=2, fill=main, outline=outline)
        d.rectangle((5, 8, 9, 9), fill=accent)
        d.rectangle((13, 8, 17, 9), fill=accent)
        d.line((6, 15, 9, 15), fill=light)
        d.line((13, 15, 16, 15), fill=light)

    return img


# ============================================================ 护甲穿身帧表（40x1120）

def draw_head_frame(d, palette, variant):
    outline, main, light, accent = palette
    left, top, right, bottom = BOX_HEAD

    if variant == "hood":
        # 兜帽：尖顶 + 压暗的脸
        d.polygon([(left + 2, top), (right - 2, top), (right, bottom), (left, bottom)],
                  fill=main, outline=outline)
        d.rectangle((left + 3, top + 5, right - 3, bottom - 1), fill=outline)
        d.line((left + 4, top + 2, right - 5, top + 2), fill=light)
        d.point((left + 1, top + 6), fill=accent)
        d.point((right - 1, top + 6), fill=accent)
    elif variant == "mask":
        # 面罩：只盖住下半张脸，上部留出眼睛的高度
        d.rounded_rectangle((left, top + 3, right, bottom), radius=3, fill=main, outline=outline)
        d.rectangle((left + 1, top + 7, right - 1, top + 9), fill=outline)
        d.line((left + 2, top + 4, right - 3, top + 4), fill=light)
        d.rectangle((left + 5, top + 10, right - 6, top + 11), fill=accent)
    else:  # visor
        d.rounded_rectangle((left, top + 1, right, bottom), radius=3, fill=main, outline=outline)
        d.rectangle((left + 1, top + 5, right - 1, top + 7), fill=accent)
        d.point((left + 3, top + 6), fill=outline)
        d.point((right - 4, top + 6), fill=outline)
        d.line((left + 3, top + 2, right - 4, top + 2), fill=light)


def draw_body_frame(d, palette, variant):
    outline, main, light, accent = palette
    left, top, right, bottom = BOX_BODY
    torso = (left + 2, top, right - 2, bottom)

    d.rounded_rectangle(torso, radius=3, fill=main, outline=outline)
    d.rectangle((left, top + 1, left + 3, top + 7), fill=main, outline=outline)     # 肩甲
    d.rectangle((right - 3, top + 1, right, top + 7), fill=main, outline=outline)
    d.line((torso[0] + 1, torso[1] + 3, torso[2] - 2, torso[1] + 3), fill=light)

    if variant == "jacket":
        # 背带：两条斜带
        d.line((torso[0] + 2, torso[1] + 4, torso[2] - 3, torso[3] - 4), fill=accent)
        d.line((torso[0] + 5, torso[1] + 4, torso[2] - 1, torso[3] - 4), fill=accent)
        d.rectangle((torso[0], torso[3] - 4, torso[2], torso[3] - 3), fill=outline)
    elif variant == "plate":
        d.rectangle((torso[0] + 4, torso[1] + 6, torso[0] + 5, torso[3] - 3), fill=accent)
        d.rectangle((torso[2] - 5, torso[1] + 6, torso[2] - 4, torso[3] - 3), fill=accent)
        d.rectangle((torso[0], torso[3] - 4, torso[2], torso[3] - 3), fill=outline)
    else:  # cuirass
        d.rectangle((torso[0] + 3, torso[1] + 7, torso[2] - 4, torso[1] + 9), fill=accent)
        d.rectangle((torso[0], torso[3] - 5, torso[2], torso[3] - 3), fill=outline)
        d.rectangle((torso[0] + 1, torso[1] + 12, torso[2] - 1, torso[1] + 13), fill=light)


def draw_legs_frame(d, palette, variant):
    outline, main, light, accent = palette
    left, top, right, bottom = BOX_LEGS
    mid = (left + right) // 2

    for x0, x1 in ((left, mid - 2), (mid + 2, right)):
        d.rectangle((x0, top, x1, bottom), fill=main, outline=outline)
        d.line((x0 + 1, top + 1, x0 + 1, bottom - 1), fill=light)
        d.rectangle((x0 + 1, top + 6, x1 - 1, top + 7), fill=accent)
        d.rectangle((x0 + 1, bottom - 3, x1 - 1, bottom - 1), fill=outline)

    if variant == "cuirass":
        # 炉卫重甲：膝甲加宽一圈
        d.rectangle((left, top + 12, right, top + 14), fill=main, outline=outline)


def build_sheet(palette, slot, variant):
    if slot == "Head":
        piece_drawer = lambda d: draw_head_frame(d, palette, variant)
    elif slot == "Body":
        piece_drawer = lambda d: draw_body_frame(d, palette, variant)
    else:
        piece_drawer = lambda d: draw_legs_frame(d, palette, variant)

    sheet = blank(FRAME_W, FRAME_H * FRAMES)

    for index in range(FRAMES):
        frame = blank(FRAME_W, FRAME_H)
        piece_drawer(ImageDraw.Draw(frame))
        offset = (0, BOB[index % len(BOB)])
        sheet.alpha_composite(frame, (0, index * FRAME_H + offset[1]))

    return sheet


# 套装 -> (前缀, 头/身/腿的部件名, 部件画法)
ARMOR_SETS = [
    ("ScavengerGrace", "ScavengerGrace", ("Hood", "Jacket", "Boots"), ("hood", "jacket", "greaves")),
    ("AshHeart", "AshHeart", ("Mask", "Plate", "Greaves"), ("mask", "plate", "greaves")),
    ("HearthGuard", "HearthGuard", ("Visor", "Cuirass", "Greaves"), ("visor", "cuirass", "cuirass")),
]


def build_armor():
    for key, prefix, parts, variants in ARMOR_SETS:
        palette = SETS[key]
        slots = ("Head", "Body", "Legs")

        for part, slot, variant in zip(parts, slots, variants):
            item = "%s%s" % (prefix, part)

            save(draw_armor_icon(slot, palette), os.path.join(ARMOR_DIR, "%s.png" % item))
            save(build_sheet(palette, slot, variant), os.path.join(ARMOR_DIR, "%s_%s.png" % (item, slot)))


# ============================================================ 饰品图标（24x24）

def acc_harness(d):
    """拾荒者背带：肩带 + 扣具。"""
    d.line((4, 19, 12, 4), fill=(122, 90, 56, 255), width=3)
    d.line((20, 19, 12, 4), fill=(122, 90, 56, 255), width=3)
    d.rounded_rectangle((8, 3, 16, 9), radius=2, fill=(96, 104, 86, 255), outline=(34, 40, 34, 255))
    d.rectangle((10, 12, 14, 16), fill=(196, 168, 72, 255), outline=(34, 40, 34, 255))
    d.line((4, 19, 20, 19), fill=(74, 78, 84, 255), width=2)


def acc_rebreather(d):
    """净化过滤面罩：圆形滤罐 + 两侧镜片。"""
    d.ellipse((6, 4, 18, 20), fill=(96, 104, 86, 255), outline=(34, 40, 34, 255), width=2)
    d.ellipse((9, 8, 15, 16), fill=(150, 190, 200, 255), outline=(34, 40, 34, 255))
    d.rectangle((2, 9, 6, 14), fill=(74, 78, 84, 255), outline=(34, 40, 34, 255))
    d.rectangle((18, 9, 22, 14), fill=(74, 78, 84, 255), outline=(34, 40, 34, 255))
    d.line((8, 6, 16, 6), fill=(196, 220, 200, 255))


def acc_compass(d):
    """废土指针：歪掉的罗盘。"""
    d.ellipse((2, 2, 22, 22), fill=(96, 104, 86, 255), outline=(34, 40, 34, 255), width=2)
    d.ellipse((5, 5, 19, 19), fill=(40, 46, 40, 255))
    d.polygon([(12, 4), (15, 13), (12, 11)], fill=(226, 120, 60, 255))
    d.polygon([(12, 20), (9, 12), (12, 14)], fill=(200, 204, 208, 255))
    d.ellipse((11, 11, 13, 13), fill=(196, 168, 72, 255))


def acc_breather(d):
    """深渊呼吸器：潜水面罩 + 呼吸管。"""
    d.ellipse((3, 8, 21, 19), fill=(60, 96, 120, 255), outline=(26, 42, 58, 255), width=2)
    d.ellipse((5, 10, 12, 16), fill=(150, 210, 230, 255))
    d.ellipse((13, 10, 19, 16), fill=(150, 210, 230, 255))
    d.line((16, 8, 20, 2), fill=(90, 140, 170, 255), width=3)
    d.rectangle((17, 1, 22, 4), fill=(196, 168, 72, 255), outline=(26, 42, 58, 255))


def acc_boots(d):
    """煤渣踏靴：一双短靴 + 火星。"""
    d.rounded_rectangle((3, 10, 11, 20), radius=2, fill=(72, 60, 52, 255), outline=(40, 30, 24, 255))
    d.rounded_rectangle((13, 10, 21, 20), radius=2, fill=(72, 60, 52, 255), outline=(40, 30, 24, 255))
    d.rectangle((2, 18, 12, 21), fill=(52, 44, 38, 255))
    d.rectangle((12, 18, 22, 21), fill=(52, 44, 38, 255))
    d.rectangle((5, 13, 9, 16), fill=(226, 120, 60, 255))
    d.rectangle((15, 13, 19, 16), fill=(226, 120, 60, 255))
    d.point((4, 7), fill=(255, 206, 130, 255))
    d.point((19, 6), fill=(255, 206, 130, 255))


def acc_lens(d):
    """余烬玻璃透镜：圆形镜片 + 内部余烬。"""
    d.ellipse((2, 2, 22, 22), fill=(120, 88, 60, 255), outline=(52, 22, 14, 255), width=2)
    d.ellipse((5, 5, 19, 19), fill=(180, 130, 90, 160))
    d.ellipse((8, 8, 16, 16), fill=(255, 150, 60, 255))
    d.ellipse((10, 10, 14, 14), fill=(255, 230, 180, 255))
    d.line((5, 8, 12, 5), fill=(255, 255, 255, 180))
    d.rectangle((11, 0, 13, 3), fill=(96, 104, 86, 255))


def acc_core(d):
    """祝圣之核：多面体核心 + 环绕的小点。"""
    d.polygon([(12, 2), (21, 9), (18, 20), (6, 20), (3, 9)], fill=(140, 100, 180, 255),
              outline=(50, 34, 66, 255))
    d.polygon([(12, 5), (18, 10), (15, 17), (9, 17), (6, 10)], fill=(196, 160, 230, 255))
    d.ellipse((10, 9, 14, 13), fill=(255, 245, 210, 255))
    d.point((12, 0), fill=(255, 255, 255, 230))
    d.point((1, 12), fill=(255, 255, 255, 230))
    d.point((22, 12), fill=(255, 255, 255, 230))


def acc_echo(d):
    """灰烬回响护符：吊坠 + 内嵌火种。"""
    d.arc((4, 0, 20, 14), start=200, end=340, fill=(120, 90, 56, 255), width=2)
    d.polygon([(12, 8), (20, 14), (12, 22), (4, 14)], fill=(90, 60, 44, 255),
              outline=(52, 22, 14, 255))
    d.polygon([(12, 11), (17, 14), (12, 19), (7, 14)], fill=(255, 150, 60, 255))
    d.ellipse((10, 13, 14, 17), fill=(255, 235, 190, 255))


def acc_beacon(d):
    """炉火信标：小灯盏 + 光晕。"""
    d.rounded_rectangle((6, 14, 18, 21), radius=2, fill=(96, 140, 176, 255), outline=(26, 42, 58, 255))
    d.polygon([(8, 14), (16, 14), (14, 6), (10, 6)], fill=(196, 228, 248, 255), outline=(26, 42, 58, 255))
    d.ellipse((10, 8, 14, 12), fill=(255, 240, 200, 255))
    d.line((12, 6, 12, 1), fill=(226, 244, 255, 200))
    d.line((5, 4, 8, 7), fill=(226, 244, 255, 160))
    d.line((19, 4, 16, 7), fill=(226, 244, 255, 160))


def acc_aegis(d):
    """炉火护盾：盾牌 + 冷火纹。"""
    d.polygon([(12, 2), (21, 6), (19, 16), (12, 22), (5, 16), (3, 6)], fill=(96, 140, 176, 255),
              outline=(26, 42, 58, 255))
    d.polygon([(12, 5), (18, 8), (16, 15), (12, 19), (8, 15), (6, 8)], fill=(196, 228, 248, 255))
    d.polygon([(12, 8), (15, 12), (12, 16), (9, 12)], fill=(255, 255, 255, 235))
    d.line((12, 2, 12, 0), fill=(226, 244, 255, 200))


def acc_mirror(d):
    """炉火镜面：裂开的镜子。"""
    d.rounded_rectangle((3, 2, 21, 18), radius=3, fill=(150, 190, 210, 255), outline=(26, 42, 58, 255))
    d.line((12, 3, 10, 12), fill=(60, 90, 110, 255))
    d.line((10, 12, 17, 17), fill=(60, 90, 110, 255))
    d.line((10, 12, 5, 16), fill=(60, 90, 110, 255))
    d.line((6, 5, 9, 9), fill=(240, 252, 255, 220))
    d.rectangle((11, 19, 13, 23), fill=(96, 140, 176, 255), outline=(26, 42, 58, 255))


def acc_morrow(d):
    """明日之匣：小方匣 + 冷光缝 + 齿轮角。"""
    d.rounded_rectangle((3, 6, 21, 21), radius=2, fill=(58, 62, 70, 255), outline=(24, 26, 30, 255))
    d.rectangle((5, 12, 19, 14), fill=(226, 244, 255, 255))
    d.rectangle((9, 6, 15, 8), fill=(96, 140, 176, 255))
    d.rectangle((6, 16, 18, 20), fill=(40, 44, 50, 255))
    d.point((5, 4), fill=(226, 244, 255, 200))
    d.point((19, 3), fill=(226, 244, 255, 200))


ACCESSORIES = [
    ("ScavengerHarness", acc_harness),
    ("FilteredRebreather", acc_rebreather),
    ("WastelandCompass", acc_compass),
    ("AbyssBreather", acc_breather),
    ("CinderstepBoots", acc_boots),
    ("EmberglassLens", acc_lens),
    ("ConsecratedCore", acc_core),
    ("AshEchoCharm", acc_echo),
    ("HearthBeacon", acc_beacon),
    ("HearthAegis", acc_aegis),
    ("HearthMirror", acc_mirror),
    ("MorrowFragment", acc_morrow),
]


def build_accessories():
    for name, drawer in ACCESSORIES:
        img = blank(24, 24)
        drawer(ImageDraw.Draw(img))
        save(img, os.path.join(ACC_DIR, "%s.png" % name))

    # 净化过滤面罩刻意沿用已有防毒面具的造型，直接复制一份（不改动原文件）
    gas_mask = os.path.join(ROOT, r"Content\Items\Accessories\GasMask.png")

    if os.path.exists(gas_mask):
        copy = Image.open(gas_mask).convert("RGBA")
        save(copy, os.path.join(ACC_DIR, "FilteredRebreather.png"))


# ============================================================ 饰品弹幕

def build_projectiles():
    shard = blank(14, 14)
    d = ImageDraw.Draw(shard)
    d.polygon([(1, 8), (6, 2), (13, 5), (11, 12), (4, 12)], fill=(150, 60, 34, 255),
              outline=(52, 22, 14, 255))
    d.polygon([(5, 5), (10, 4), (8, 9), (4, 9)], fill=(255, 170, 90, 255))
    d.ellipse((2, 4, 6, 8), fill=(255, 236, 190, 255))
    save(shard, os.path.join(PROJ_DIR, "GearReboundShard.png"))

    pulse = blank(40, 40)
    d = ImageDraw.Draw(pulse)
    d.ellipse((2, 2, 37, 37), outline=(150, 210, 240, 130), width=3)
    d.ellipse((8, 8, 31, 31), outline=(210, 240, 255, 190), width=3)
    d.ellipse((14, 14, 25, 25), outline=(255, 255, 255, 220), width=2)
    for index in range(8):
        angle = index * 45
        x = 20 + int(round(17 * math.cos(math.radians(angle))))
        y = 20 + int(round(17 * math.sin(math.radians(angle))))
        d.point((x, y), fill=(255, 255, 255, 240))
    save(pulse, os.path.join(PROJ_DIR, "GearColdPulse.png"))


# ============================================================ 预览拼图

def build_preview():
    os.makedirs(INBOX, exist_ok=True)

    icons = []
    for name, _drawer in ACCESSORIES:
        path = os.path.join(ACC_DIR, "%s.png" % name)

        if os.path.exists(path):
            icons.append((name, Image.open(path).convert("RGBA")))

    for key, prefix, parts, _variants in ARMOR_SETS:
        for part in parts:
            path = os.path.join(ARMOR_DIR, "%s%s.png" % (prefix, part))

            if os.path.exists(path):
                icons.append(("%s%s" % (prefix, part), Image.open(path).convert("RGBA")))

    scale = 4
    cell = 24 * scale + 8
    columns = 6
    rows = (len(icons) + columns - 1) // columns
    preview = Image.new("RGBA", (columns * cell + 8, rows * cell + 8), (26, 28, 32, 255))
    d = ImageDraw.Draw(preview)

    for index, (name, icon) in enumerate(icons):
        column = index % columns
        row = index // columns
        big = icon.resize((icon.width * scale, icon.height * scale), Image.NEAREST)
        x = 8 + column * cell + (24 * scale - big.width) // 2
        y = 8 + row * cell + (24 * scale - big.height) // 2
        preview.alpha_composite(big, (x, y))
        d.rectangle((8 + column * cell, 8 + row * cell,
                     8 + column * cell + 24 * scale, 8 + row * cell + 24 * scale),
                    outline=(70, 74, 82, 255))

    target = os.path.join(INBOX, "preview_gear.png")
    preview.save(target)
    print("wrote preview %-52s %s" % ("preview_gear.png", preview.size))


def main():
    build_armor()
    build_accessories()
    build_projectiles()
    build_preview()
    print("")
    print("共生成 %d 个贴图文件。" % len(CREATED))
    return 0


if __name__ == "__main__":
    sys.exit(main())
