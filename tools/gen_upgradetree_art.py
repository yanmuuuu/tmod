# -*- coding: utf-8 -*-
# 升级衍生树（4 条）的程序化贴图生成。
#
# 尺寸约定（必须与代码里的碰撞盒/帧布局一致）：
#     单手/双手武器图标     40x40 / 46x46
#     枪械图标              48x20 / 50x22
#     饰品图标              24x24
#     护甲图标              22x22
#     护甲穿身帧表          40x1120 = 20 帧 x 56 高（_Head / _Body / _Legs）
#     弹幕                  12x12 ~ 46x46（见下面各自注释）
#
# 帧表锚点沿用原版玩家骨架（与 tools/gen_gear_art.py 一致）：
#     头 8..20 / 身 20..36 / 腿 35..53，相邻部位重叠 1 像素，穿起来不露身体。
#
# 本脚本只写自己新增的文件，不删除、不覆盖任何别人生成的美术资源。
import math
import os

from PIL import Image, ImageDraw

ROOT = r"E:\开发\WastelandSoul"
ITEM_DIR = os.path.join(ROOT, r"Content\Items\UpgradeTrees")
PROJ_DIR = os.path.join(ROOT, r"Content\Projectiles\UpgradeTrees")
INBOX = r"E:\开发\art-inbox"

FRAME_W, FRAME_H, FRAMES = 40, 56, 20
BOB = (0, 0, 1, 1, 0, 0, -1, -1)

BOX_HEAD = (13, 8, 27, 20)
BOX_BODY = (11, 20, 29, 36)
BOX_LEGS = (13, 35, 27, 53)

# 配色：(描边, 主色, 亮色, 点缀色)
STEEL = ((30, 34, 40), (110, 120, 132), (198, 208, 220), (92, 152, 192))
ARCHIVIST = ((34, 26, 52), (108, 86, 160), (198, 180, 240), (255, 222, 140))
ASH_ALLOY = ((40, 26, 22), (128, 84, 64), (214, 168, 120), (255, 150, 60))
HEARTH_ALLOY = ((22, 34, 48), (110, 150, 186), (206, 232, 250), (255, 178, 90))

CREATED = []


def save(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    CREATED.append((path, img.size))
    print("wrote %-58s %s" % (os.path.relpath(path, ROOT), img.size))


def blank(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


# ============================================================ 武器：刀剑类

def blade(img, d, start, end, width, palette, runes=0):
    """从 start 到 end 画一把带护手和握柄的刀；runes > 0 时沿刃身刻发光的纹路。"""
    outline, main, light, accent = palette
    sx, sy = start
    ex, ey = end
    dx, dy = ex - sx, ey - sy
    length = math.hypot(dx, dy) or 1.0
    ux, uy = dx / length, dy / length
    px, py = -uy, ux
    half = width / 2.0

    def pt(bx, by, off):
        return (bx + px * off, by + py * off)

    d.polygon([pt(sx, sy, half), pt(ex, ey, half), pt(ex, ey, -half), pt(sx, sy, -half)],
              fill=main, outline=outline)
    d.line([pt(sx, sy, half * 0.35), pt(ex - ux * 2, ey - uy * 2, half * 0.35)], fill=light)

    # 护手：在刀刃根部横一根
    guard = width * 1.6
    d.polygon([pt(sx - ux * 2, sy - uy * 2, guard), pt(sx + ux * 2, sy + uy * 2, guard),
               pt(sx + ux * 2, sy + uy * 2, -guard), pt(sx - ux * 2, sy - uy * 2, -guard)],
              fill=outline)

    # 握柄：往反方向再伸一段
    hx, hy = sx - ux * 9, sy - uy * 9
    d.polygon([pt(sx - ux, sy - uy, half * 0.8), pt(hx, hy, half * 0.8),
               pt(hx, hy, -half * 0.8), pt(sx - ux, sy - uy, -half * 0.8)],
              fill=(72, 52, 36, 255), outline=outline)

    if runes:
        for index in range(runes):
            t = 0.35 + index * 0.16
            cx, cy = sx + dx * t, sy + dy * t
            d.point((int(round(cx)), int(round(cy))), fill=accent)
            d.point((int(round(cx + px * 1.5)), int(round(cy + py * 1.5))), fill=accent)


def build_saber():
    img = blank(40, 40)
    d = ImageDraw.Draw(img)
    blade(img, d, (13, 32), (34, 7), 6, STEEL)
    d.point((33, 6), fill=(226, 240, 255, 255))
    save(img, os.path.join(ITEM_DIR, "SalvagedSteelSaber.png"))


def build_verdict_blade():
    img = blank(46, 46)
    d = ImageDraw.Draw(img)
    blade(img, d, (12, 38), (40, 6), 7, ARCHIVIST, runes=4)
    # 刃根的能量槽
    d.rectangle((12, 30, 16, 34), fill=ARCHIVIST[3], outline=ARCHIVIST[0])
    d.point((41, 5), fill=(255, 250, 220, 255))
    d.point((40, 6), fill=(255, 236, 170, 255))
    save(img, os.path.join(ITEM_DIR, "ArchivistVerdictBlade.png"))


# ============================================================ 武器：枪械

def gun(img, palette, barrel, receiver, stock, accent_boxes=()):
    outline, main, light, accent = palette
    d = ImageDraw.Draw(img)
    d.polygon(barrel, fill=main, outline=outline)
    d.rectangle(receiver, fill=main, outline=outline)
    d.polygon(stock, fill=(74, 58, 44, 255), outline=outline)
    d.line((barrel[0][0] + 2, barrel[0][1] + 1, barrel[2][0] - 3, barrel[0][1] + 1), fill=light)

    for box in accent_boxes:
        d.rectangle(box, fill=accent, outline=outline)


def build_scattergun():
    img = blank(48, 20)
    gun(img, STEEL,
        barrel=[(20, 6), (45, 6), (45, 11), (20, 12)],
        receiver=(10, 5, 22, 14),
        stock=[(2, 6), (11, 5), (11, 15), (2, 17)],
        accent_boxes=[(24, 13, 34, 17), (44, 4, 47, 13), (12, 6, 20, 9)])
    save(img, os.path.join(ITEM_DIR, "SalvagedSteelScattergun.png"))


def build_flechette():
    img = blank(50, 22)
    gun(img, ASH_ALLOY,
        barrel=[(22, 6), (48, 8), (48, 13), (22, 13)],
        receiver=(10, 4, 24, 16),
        stock=[(2, 6), (11, 4), (11, 17), (2, 19)],
        accent_boxes=[(26, 14, 38, 19), (46, 6, 49, 15)])
    d = ImageDraw.Draw(img)
    # 燃烧药室 + 排气孔
    d.rectangle((13, 6, 21, 14), fill=(255, 150, 60, 255), outline=ASH_ALLOY[0])
    d.point((16, 9), fill=(255, 236, 190, 255))
    d.point((18, 11), fill=(255, 236, 190, 255))
    d.line((26, 4, 30, 4), fill=(255, 178, 90, 220))
    d.line((32, 4, 36, 4), fill=(255, 178, 90, 220))
    save(img, os.path.join(ITEM_DIR, "AshHeartFlechette.png"))


# ============================================================ 武器：法杖与法典

def build_arc_wand():
    img = blank(36, 36)
    d = ImageDraw.Draw(img)
    d.line((6, 31, 22, 14), fill=(72, 52, 36, 255), width=5)
    d.line((6, 31, 22, 14), fill=STEEL[1], width=3)
    # 顶端的弧光晶体 + 环绕电极
    d.polygon([(21, 4), (32, 11), (26, 24), (16, 15)], fill=STEEL[3], outline=STEEL[0])
    d.polygon([(22, 8), (28, 12), (24, 20), (19, 15)], fill=(226, 244, 255, 255))
    d.arc((12, 3, 34, 25), start=150, end=330, fill=STEEL[2], width=2)
    d.point((31, 8), fill=(240, 250, 255, 255))
    d.point((18, 9), fill=(240, 250, 255, 200))
    save(img, os.path.join(ITEM_DIR, "SalvagedSteelArcWand.png"))


def build_frost_codex():
    img = blank(40, 40)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((7, 5, 34, 35), radius=2, fill=HEARTH_ALLOY[1], outline=HEARTH_ALLOY[0], width=2)
    d.rectangle((7, 6, 12, 34), fill=(58, 74, 92, 255), outline=HEARTH_ALLOY[0])
    d.ellipse((17, 11, 32, 26), outline=HEARTH_ALLOY[2], width=2)
    d.polygon([(24, 13), (29, 18), (24, 25), (19, 18)], fill=(226, 244, 255, 235),
              outline=HEARTH_ALLOY[0])
    d.point((24, 18), fill=(255, 255, 255, 255))
    d.line((15, 30, 32, 30), fill=HEARTH_ALLOY[3])
    d.point((9, 9), fill=HEARTH_ALLOY[3])
    d.point((31, 32), fill=HEARTH_ALLOY[3])
    save(img, os.path.join(ITEM_DIR, "HearthGuardFrostCodex.png"))


# ============================================================ 饰品

def build_reinforced_mask():
    img = blank(24, 24)
    d = ImageDraw.Draw(img)
    d.ellipse((5, 4, 19, 20), fill=(96, 104, 86, 255), outline=(34, 40, 34, 255), width=2)
    d.ellipse((7, 8, 12, 15), fill=(150, 200, 210, 255), outline=(34, 40, 34, 255))
    d.ellipse((12, 8, 17, 15), fill=(150, 200, 210, 255), outline=(34, 40, 34, 255))
    # 两侧可更换滤芯
    d.rounded_rectangle((1, 8, 6, 17), radius=2, fill=(74, 78, 84, 255), outline=(34, 40, 34, 255))
    d.rounded_rectangle((18, 8, 23, 17), radius=2, fill=(74, 78, 84, 255), outline=(34, 40, 34, 255))
    d.rectangle((1, 11, 6, 13), fill=(92, 152, 192, 255))
    d.rectangle((18, 11, 23, 13), fill=(92, 152, 192, 255))
    d.line((8, 6, 16, 6), fill=(198, 208, 220, 255))
    save(img, os.path.join(ITEM_DIR, "ReinforcedFilterMask.png"))


def build_purifier_mask():
    img = blank(24, 24)
    d = ImageDraw.Draw(img)
    d.ellipse((4, 3, 20, 21), fill=(64, 56, 52, 255), outline=(40, 26, 22, 255), width=2)
    d.ellipse((7, 7, 17, 18), fill=(255, 150, 60, 255), outline=(40, 26, 22, 255))
    d.ellipse((10, 10, 14, 15), fill=(255, 236, 190, 255))
    d.rounded_rectangle((0, 9, 5, 18), radius=2, fill=(74, 78, 84, 255), outline=(40, 26, 22, 255))
    d.rounded_rectangle((19, 9, 24, 18), radius=2, fill=(74, 78, 84, 255), outline=(40, 26, 22, 255))
    d.line((8, 5, 16, 5), fill=(255, 206, 130, 255))
    d.point((5, 6), fill=(255, 236, 190, 230))
    d.point((19, 6), fill=(255, 236, 190, 230))
    save(img, os.path.join(ITEM_DIR, "AshHeartPurifierMask.png"))


# ============================================================ 护甲（图标 + 穿身帧表）

def draw_armor_icon(slot, palette):
    outline, main, light, accent = palette
    img = blank(22, 22)
    d = ImageDraw.Draw(img)

    if slot == "Head":
        d.rounded_rectangle((4, 4, 18, 18), radius=4, fill=main, outline=outline)
        d.rectangle((6, 10, 16, 15), fill=outline)
        d.line((6, 6, 16, 6), fill=light)
        d.point((7, 7), fill=accent)
    elif slot == "Body":
        d.rounded_rectangle((4, 3, 18, 19), radius=3, fill=main, outline=outline)
        d.rectangle((3, 3, 7, 9), fill=main, outline=outline)
        d.rectangle((15, 3, 19, 9), fill=main, outline=outline)
        d.rectangle((5, 5, 17, 8), fill=light)
        d.rectangle((10, 8, 12, 18), fill=accent)
        d.line((5, 16, 17, 16), fill=outline)
    else:
        d.rounded_rectangle((4, 3, 10, 19), radius=2, fill=main, outline=outline)
        d.rounded_rectangle((12, 3, 18, 19), radius=2, fill=main, outline=outline)
        d.rectangle((5, 8, 9, 9), fill=accent)
        d.rectangle((13, 8, 17, 9), fill=accent)
        d.line((6, 15, 9, 15), fill=light)
        d.line((13, 15, 16, 15), fill=light)

    return img


def draw_head_frame(d, palette, variant):
    outline, main, light, accent = palette
    left, top, right, bottom = BOX_HEAD

    d.rounded_rectangle((left, top + 1, right, bottom), radius=3, fill=main, outline=outline)
    d.rectangle((left + 1, top + 4, right - 1, top + 7), fill=outline)
    d.line((left + 3, top + 2, right - 4, top + 2), fill=light)

    if variant == "ash":
        # 灰烬合金：面罩 + 两侧进气口 + 炉火色的缝
        d.rectangle((left + 4, top + 8, right - 4, top + 9), fill=accent)
        d.rectangle((left - 1, top + 6, left + 1, top + 10), fill=outline)
        d.rectangle((right - 1, top + 6, right + 1, top + 10), fill=outline)
    else:
        # 炉卫合金：护目条 + 冷光点
        d.rectangle((left + 1, top + 5, right - 1, top + 6), fill=accent)
        d.point((left + 3, top + 5), fill=outline)
        d.point((right - 4, top + 5), fill=outline)
        d.rectangle((left + 5, top + 9, right - 6, top + 10), fill=accent)


def draw_body_frame(d, palette, variant):
    outline, main, light, accent = palette
    left, top, right, bottom = BOX_BODY
    d.rounded_rectangle((left + 2, top, right - 2, bottom), radius=3, fill=main, outline=outline)
    d.rectangle((left, top + 1, left + 3, top + 7), fill=main, outline=outline)
    d.rectangle((right - 3, top + 1, right, top + 7), fill=main, outline=outline)
    d.line((left + 3, top + 3, right - 3, top + 3), fill=light)

    if variant == "ash":
        d.rectangle((left + 4, top + 6, left + 5, bottom - 3), fill=accent)
        d.rectangle((right - 5, top + 6, right - 4, bottom - 3), fill=accent)
        d.rectangle((left + 7, top + 11, right - 7, top + 12), fill=accent)
    else:
        d.rectangle((left + 3, top + 7, right - 4, top + 9), fill=accent)
        d.rectangle((left + 1, top + 13, right - 1, top + 14), fill=light)
        d.rectangle((left + 5, top + 4, right - 6, top + 5), fill=accent)

    d.rectangle((left + 2, bottom - 3, right - 2, bottom - 2), fill=outline)


def draw_legs_frame(d, palette, variant):
    outline, main, light, accent = palette
    left, top, right, bottom = BOX_LEGS
    mid = (left + right) // 2

    for x0, x1 in ((left, mid - 2), (mid + 2, right)):
        d.rectangle((x0, top, x1, bottom), fill=main, outline=outline)
        d.line((x0 + 1, top + 1, x0 + 1, bottom - 1), fill=light)
        d.rectangle((x0 + 1, top + 6, x1 - 1, top + 7), fill=accent)
        d.rectangle((x0 + 1, bottom - 3, x1 - 1, bottom - 1), fill=outline)

    if variant == "hearth":
        d.rectangle((left, top + 12, right, top + 14), fill=main, outline=outline)


def build_sheet(palette, slot, variant):
    if slot == "Head":
        drawer = lambda d: draw_head_frame(d, palette, variant)
    elif slot == "Body":
        drawer = lambda d: draw_body_frame(d, palette, variant)
    else:
        drawer = lambda d: draw_legs_frame(d, palette, variant)

    sheet = blank(FRAME_W, FRAME_H * FRAMES)

    for index in range(FRAMES):
        frame = blank(FRAME_W, FRAME_H)
        drawer(ImageDraw.Draw(frame))
        sheet.alpha_composite(frame, (0, index * FRAME_H + BOB[index % len(BOB)]))

    return sheet


# 套装 -> (前缀, 头/身/腿部件名, 每件的画法, 配色)
ARMOR_SETS = [
    ("AshAlloyWarrior", ("Helm", "Plate", "Greaves"), "ash", ASH_ALLOY),
    ("HearthAlloyWarrior", ("Helm", "Plate", "Greaves"), "hearth", HEARTH_ALLOY),
]


def build_armor():
    slots = ("Head", "Body", "Legs")

    for prefix, parts, variant, palette in ARMOR_SETS:
        for part, slot in zip(parts, slots):
            item = "%s%s" % (prefix, part)
            save(draw_armor_icon(slot, palette), os.path.join(ITEM_DIR, "%s.png" % item))
            save(build_sheet(palette, slot, variant), os.path.join(ITEM_DIR, "%s_%s.png" % (item, slot)))


# ============================================================ 弹幕

def build_projectiles():
    # 精钢碎片 26x26：五边形铁片 + 高光
    shard = blank(26, 26)
    d = ImageDraw.Draw(shard)
    d.polygon([(3, 15), (11, 3), (23, 8), (20, 22), (7, 23)],
              fill=STEEL[1], outline=STEEL[0])
    d.polygon([(9, 10), (18, 8), (15, 17), (7, 17)], fill=STEEL[2])
    d.point((12, 12), fill=(240, 250, 255, 255))
    d.line((3, 15, 11, 3), fill=STEEL[3])
    save(shard, os.path.join(PROJ_DIR, "SteelEdgeShard.png"))

    # 裁决波 46x46：新月（外圆减偏心内圆算出来的多边形）
    wave = blank(46, 46)
    d = ImageDraw.Draw(wave)
    outer, inner = 22, 16
    points = []
    for step in range(25):
        angle = math.radians(58 + step * (244.0 / 24.0))
        points.append((23 + outer * math.cos(angle), 23 + outer * math.sin(angle)))
    for step in range(25):
        angle = math.radians(302 - step * (244.0 / 24.0))
        points.append((30 + inner * math.cos(angle), 23 + inner * math.sin(angle)))
    d.polygon(points, fill=ARCHIVIST[1], outline=ARCHIVIST[0])
    for radius, color in ((17, ARCHIVIST[2]), (11, (232, 226, 255, 220))):
        d.arc((23 - radius, 23 - radius, 23 + radius, 23 + radius),
              start=58, end=302, fill=color, width=2)
    save(wave, os.path.join(PROJ_DIR, "VerdictWave.png"))

    # 裁决碎片 14x14：小菱形
    frag = blank(14, 14)
    d = ImageDraw.Draw(frag)
    d.polygon([(7, 1), (13, 7), (7, 13), (1, 7)], fill=ARCHIVIST[1], outline=ARCHIVIST[0])
    d.polygon([(7, 4), (10, 7), (7, 10), (4, 7)], fill=ARCHIVIST[3])
    save(frag, os.path.join(PROJ_DIR, "VerdictFragment.png"))

    # 精钢霰弹 12x12：圆弹丸
    buck = blank(12, 12)
    d = ImageDraw.Draw(buck)
    d.ellipse((1, 1, 10, 10), fill=STEEL[1], outline=STEEL[0])
    d.ellipse((3, 3, 6, 6), fill=STEEL[2])
    save(buck, os.path.join(PROJ_DIR, "SteelBuckshot.png"))

    # 燃烬箭弹 16x16：细长镖 + 尾焰
    fle = blank(16, 16)
    d = ImageDraw.Draw(fle)
    d.polygon([(15, 8), (5, 3), (5, 13)], fill=(214, 168, 120, 255), outline=ASH_ALLOY[0])
    d.polygon([(1, 8), (6, 5), (6, 11)], fill=(255, 150, 60, 235))
    d.point((12, 8), fill=(255, 240, 210, 255))
    save(fle, os.path.join(PROJ_DIR, "EmberFlechette.png"))

    # 精钢弧光 16x16：电弧球 + 外圈
    arc = blank(16, 16)
    d = ImageDraw.Draw(arc)
    d.ellipse((1, 1, 14, 14), fill=(70, 120, 170, 200), outline=STEEL[0])
    d.ellipse((4, 4, 11, 11), fill=(196, 232, 255, 255))
    d.point((8, 2), fill=(240, 250, 255, 255))
    d.point((2, 9), fill=(240, 250, 255, 220))
    d.point((13, 11), fill=(240, 250, 255, 220))
    save(arc, os.path.join(PROJ_DIR, "SteelArcBolt.png"))

    # 霜矛 18x18：冰晶矛头 + 冷光尾
    lance = blank(18, 18)
    d = ImageDraw.Draw(lance)
    d.polygon([(17, 9), (7, 3), (7, 15)], fill=(196, 228, 248, 255), outline=HEARTH_ALLOY[0])
    d.polygon([(12, 9), (6, 5), (6, 13)], fill=(255, 255, 255, 235))
    d.polygon([(1, 9), (7, 6), (7, 12)], fill=(150, 210, 240, 220))
    save(lance, os.path.join(PROJ_DIR, "FrostLance.png"))


# ============================================================ 预览拼图

def build_preview():
    os.makedirs(INBOX, exist_ok=True)

    names = [
        os.path.join(ITEM_DIR, "SalvagedSteelSaber.png"),
        os.path.join(ITEM_DIR, "ArchivistVerdictBlade.png"),
        os.path.join(ITEM_DIR, "SalvagedSteelScattergun.png"),
        os.path.join(ITEM_DIR, "AshHeartFlechette.png"),
        os.path.join(ITEM_DIR, "SalvagedSteelArcWand.png"),
        os.path.join(ITEM_DIR, "HearthGuardFrostCodex.png"),
        os.path.join(ITEM_DIR, "ReinforcedFilterMask.png"),
        os.path.join(ITEM_DIR, "AshHeartPurifierMask.png"),
        os.path.join(ITEM_DIR, "AshAlloyWarriorHelm.png"),
        os.path.join(ITEM_DIR, "AshAlloyWarriorPlate.png"),
        os.path.join(ITEM_DIR, "AshAlloyWarriorGreaves.png"),
        os.path.join(ITEM_DIR, "HearthAlloyWarriorHelm.png"),
        os.path.join(ITEM_DIR, "HearthAlloyWarriorPlate.png"),
        os.path.join(ITEM_DIR, "HearthAlloyWarriorGreaves.png"),
        os.path.join(PROJ_DIR, "SteelEdgeShard.png"),
        os.path.join(PROJ_DIR, "VerdictWave.png"),
        os.path.join(PROJ_DIR, "VerdictFragment.png"),
        os.path.join(PROJ_DIR, "SteelBuckshot.png"),
        os.path.join(PROJ_DIR, "EmberFlechette.png"),
        os.path.join(PROJ_DIR, "SteelArcBolt.png"),
        os.path.join(PROJ_DIR, "FrostLance.png"),
    ]

    icons = [(os.path.basename(p), Image.open(p).convert("RGBA")) for p in names if os.path.exists(p)]

    scale = 4
    cell = 48 * scale // 2 + 8
    columns = 6
    rows = (len(icons) + columns - 1) // columns
    preview = Image.new("RGBA", (columns * cell + 8, rows * cell + 8), (26, 28, 32, 255))
    d = ImageDraw.Draw(preview)

    for index, (_name, icon) in enumerate(icons):
        column = index % columns
        row = index // columns
        box = 24 * scale
        big = icon.resize((icon.width * scale, icon.height * scale), Image.NEAREST)
        x = 8 + column * cell + (box - big.width) // 2
        y = 8 + row * cell + (box - big.height) // 2
        big = big.crop((-min(0, 0), 0, big.width, big.height))
        preview.alpha_composite(big, (max(8 + column * cell, x), max(8 + row * cell, y)))
        d.rectangle((8 + column * cell, 8 + row * cell,
                     8 + column * cell + box, 8 + row * cell + box),
                    outline=(70, 74, 82, 255))

    target = os.path.join(INBOX, "preview_upgradetree.png")
    preview.save(target)
    print("wrote preview %-52s %s" % ("preview_upgradetree.png", preview.size))


def main():
    build_saber()
    build_verdict_blade()
    build_scattergun()
    build_flechette()
    build_arc_wand()
    build_frost_codex()
    build_reinforced_mask()
    build_purifier_mask()
    build_armor()
    build_projectiles()
    build_preview()
    print("")
    print("total %d texture files." % len(CREATED))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
