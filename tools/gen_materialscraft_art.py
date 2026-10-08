"""WastelandSoul 材料 / 消耗品 / 弹药 / 召唤物 贴图生成器。

生成物全部是程序化像素画（PIL），风格统一：暗部轮廓 + 中间调 + 高光 + 少量脏点。
尺寸必须和 C# 里的 Item.width/height 一致，否则物品图标会被拉伸。

用法：
    python gen_materials_pack_art.py
"""

import os
import random

from PIL import Image, ImageDraw

ROOT = r"E:\开发\WastelandSoul"
PREVIEW_DIR = r"E:\开发\art-inbox"
RNG = random.Random(20261009)

# ---------------------------------------------------------------- 调色板
OUTLINE = (28, 24, 22, 255)
RUST_D = (86, 48, 30, 255)
RUST = (128, 74, 44, 255)
RUST_L = (172, 112, 66, 255)
STEEL_D = (66, 70, 76, 255)
STEEL = (112, 118, 126, 255)
STEEL_L = (168, 174, 182, 255)
COAL_D = (30, 28, 30, 255)
COAL = (54, 50, 52, 255)
COAL_L = (86, 80, 82, 255)
BOARD_D = (28, 66, 48, 255)
BOARD = (44, 104, 70, 255)
GOLD = (196, 156, 56, 255)
COOL_D = (32, 88, 104, 255)
COOL = (78, 168, 186, 255)
COOL_L = (176, 230, 240, 255)
ASH_D = (92, 80, 84, 255)
ASH = (146, 132, 136, 255)
ASH_L = (206, 196, 198, 255)
EMBER_D = (128, 44, 20, 255)
EMBER = (208, 92, 32, 255)
EMBER_L = (248, 176, 72, 255)
PAPER_D = (150, 136, 108, 255)
PAPER = (206, 192, 160, 255)
PAPER_L = (238, 228, 202, 255)
GLASS = (150, 210, 220, 230)
GREEN = (108, 176, 96, 255)
GREEN_L = (168, 216, 132, 255)


def save(img, rel):
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print("wrote %-62s %s" % (rel, img.size))


def new(size):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def noise(d, size, box, color, count, rng=None):
    """在 box 范围内撒 count 个单像素脏点。"""
    r = rng or RNG
    x0, y0, x1, y1 = box
    for _ in range(count):
        x = r.randint(x0, x1 - 1)
        y = r.randint(y0, y1 - 1)
        d.point((x, y), fill=color)


# ---------------------------------------------------------------- 材料 6 种
def art_rusted_gear():
    """20x20：锈蚀齿轮（八角齿 + 中孔）。"""
    size = 20
    img, d = new(size)
    d.ellipse((2, 2, 17, 17), fill=RUST_D, outline=OUTLINE)
    d.ellipse((4, 4, 15, 15), fill=RUST)
    # 齿
    for (bx0, by0, bx1, by1) in [(8, 0, 12, 4), (8, 16, 12, 20), (0, 8, 4, 12), (16, 8, 20, 12)]:
        d.rectangle((bx0, by0, bx1 - 1, by1 - 1), fill=RUST_D, outline=OUTLINE)
    d.ellipse((7, 5, 12, 10), fill=RUST_L)
    d.ellipse((8, 8, 11, 11), fill=OUTLINE)
    noise(d, size, (3, 3, 17, 17), RUST_L, 10)
    return img


def art_coke():
    """20x20：焦炭块（多面体 + 高光）。"""
    size = 20
    img, d = new(size)
    d.polygon([(3, 12), (6, 4), (14, 3), (17, 10), (14, 17), (6, 17)], fill=COAL, outline=OUTLINE)
    d.polygon([(6, 4), (14, 3), (12, 9), (7, 10)], fill=COAL_L)
    d.polygon([(7, 10), (12, 9), (14, 17), (6, 17)], fill=COAL_D)
    d.line([(7, 10), (12, 9)], fill=OUTLINE)
    noise(d, size, (4, 4, 17, 17), COAL_L, 8)
    return img


def art_circuit_board():
    """22x20：旧世界电路板（绿色基板 + 金线 + 芯片）。"""
    img, d = new(22)
    d.rectangle((1, 3, 20, 17), fill=BOARD, outline=OUTLINE)
    d.rectangle((2, 4, 19, 5), fill=(58, 128, 88, 255))
    # 走线
    for y in (8, 11, 14):
        d.line([(4, y), (17, y)], fill=GOLD)
    d.line([(9, 8), (9, 14)], fill=GOLD)
    d.line([(15, 8), (15, 11)], fill=GOLD)
    # 芯片
    d.rectangle((6, 9, 11, 13), fill=(38, 38, 44, 255), outline=OUTLINE)
    d.point((7, 10), fill=(120, 124, 132, 255))
    # 焊点
    for x in (4, 17):
        for y in (8, 11, 14):
            d.point((x, y), fill=(238, 208, 110, 255))
    return img


def art_coolant():
    """20x20：冷却液罐（金属罐 + 蓝色液面）。"""
    img, d = new(20)
    d.rectangle((5, 6, 14, 18), fill=STEEL_D, outline=OUTLINE)
    d.rectangle((6, 7, 13, 17), fill=(92, 98, 106, 255))
    d.rectangle((6, 10, 13, 17), fill=COOL_D)
    d.rectangle((6, 11, 13, 12), fill=COOL)
    d.rectangle((7, 8, 12, 9), fill=COOL_L)
    d.rectangle((7, 2, 12, 5), fill=STEEL, outline=OUTLINE)
    d.rectangle((8, 0, 11, 1), fill=STEEL_L, outline=OUTLINE)
    d.line([(7, 7), (7, 17)], fill=STEEL_L)
    noise(d, 20, (6, 8, 14, 18), STEEL_L, 6)
    return img


def art_ash_crystal():
    """20x20：灰烬结晶（竖直棱柱 + 冷灰高光）。"""
    img, d = new(20)
    d.polygon([(9, 1), (15, 8), (13, 18), (6, 18), (4, 8)], fill=ASH_D, outline=OUTLINE)
    d.polygon([(9, 1), (11, 8), (9, 18), (6, 18), (4, 8)], fill=ASH)
    d.polygon([(9, 1), (11, 8), (9, 12)], fill=ASH_L)
    d.line([(9, 3), (9, 17)], fill=(232, 226, 228, 255))
    noise(d, 20, (5, 4, 15, 18), ASH_L, 6)
    return img


def art_steel_bundle():
    """22x20：精钢废料束（三根钢条 + 绑带）。"""
    img, d = new(22)
    for i, y in enumerate((4, 8, 12)):
        shade = STEEL if i % 2 == 0 else STEEL_D
        d.rectangle((2, y, 19, y + 3), fill=shade, outline=OUTLINE)
        d.line([(3, y + 1), (18, y + 1)], fill=STEEL_L)
    # 绑带
    d.rectangle((7, 2, 10, 17), fill=RUST_D, outline=OUTLINE)
    d.rectangle((14, 2, 17, 17), fill=RUST, outline=OUTLINE)
    return img


# ---------------------------------------------------------------- 消耗品 6 种
def bottle_base(d, liquid, liquid_light, liquid_dark, cork=True, bubbles=4):
    """15x15 药水瓶通用底：瓶颈在左上，瓶身在右下（原版药水就是斜的）。"""
    d.polygon([(4, 3), (7, 3), (7, 6), (11, 7), (12, 13), (4, 14)], fill=liquid_dark, outline=OUTLINE)
    # 液面
    d.polygon([(5, 8), (11, 9), (11, 13), (5, 13)], fill=liquid)
    d.polygon([(5, 8), (8, 8), (8, 13), (5, 13)], fill=liquid_light)
    if cork:
        d.rectangle((3, 1, 8, 3), fill=(122, 84, 46, 255), outline=OUTLINE)
    for i in range(bubbles):
        x = 6 + (i % 2) * 3
        y = 10 + (i // 2) * 2
        d.point((x, y), fill=(255, 255, 255, 220))
    d.line([(4, 14), (12, 13)], fill=(40, 40, 44, 255))


def art_gas_filter_potion():
    """18x18：滤芯药剂（灰绿液体 + 一点白气）。"""
    img, d = new(18)
    bottle_base(d, (110, 140, 96, 255), (156, 188, 132, 255), (66, 92, 62, 255))
    d.point((13, 4), fill=(224, 236, 226, 200))
    d.point((14, 6), fill=(206, 224, 214, 180))
    return img


def art_miners_solution():
    """18x18：掘进液（土褐色液体 + 一颗小齿轮装饰）。"""
    img, d = new(18)
    bottle_base(d, (156, 112, 60, 255), (198, 154, 92, 255), (96, 66, 34, 255))
    d.ellipse((11, 3, 16, 8), fill=RUST_D, outline=OUTLINE)
    d.ellipse((13, 5, 14, 6), fill=OUTLINE)
    return img


def art_cold_spotlight():
    """18x18：冷光补剂（青蓝液体 + 冷光点）。"""
    img, d = new(18)
    bottle_base(d, COOL, COOL_L, COOL_D)
    d.point((12, 3), fill=(236, 252, 255, 235))
    d.point((14, 5), fill=(206, 244, 252, 210))
    d.point((13, 8), fill=(190, 236, 248, 190))
    return img


def art_nanite_salve():
    """18x18：纳米修复膏（浅绿膏体 + 十字）。"""
    img, d = new(18)
    bottle_base(d, (96, 168, 132, 255), (150, 214, 176, 255), (52, 104, 80, 255), bubbles=2)
    d.line([(6, 11), (10, 11)], fill=(240, 255, 248, 255))
    d.line([(8, 9), (8, 13)], fill=(240, 255, 248, 255))
    return img


def art_beast_whistle():
    """18x18：兽哨余响（骨白哨子 + 红绳）。"""
    img, d = new(18)
    d.polygon([(3, 8), (12, 5), (14, 9), (12, 13), (3, 12)], fill=(226, 216, 196, 255), outline=OUTLINE)
    d.polygon([(4, 9), (11, 8), (11, 11), (4, 11)], fill=(196, 184, 162, 255))
    d.ellipse((10, 7, 13, 11), fill=OUTLINE)
    d.line([(3, 10), (0, 12)], fill=(178, 62, 52, 255))
    d.line([(3, 9), (0, 8)], fill=(178, 62, 52, 255))
    return img


def art_ashen_ration():
    """20x20：灰烬口粮（压缩饼干 + 包装纸）。"""
    img, d = new(20)
    d.polygon([(2, 7), (17, 5), (19, 14), (4, 16)], fill=PAPER_D, outline=OUTLINE)
    d.polygon([(3, 8), (16, 6), (17, 9), (4, 11)], fill=PAPER)
    d.polygon([(4, 12), (17, 10), (18, 13), (5, 15)], fill=(168, 148, 116, 255))
    # 折痕
    d.line([(10, 6), (11, 15)], fill=OUTLINE)
    d.line([(5, 8), (6, 15)], fill=(178, 160, 130, 255))
    noise(d, 20, (3, 6, 19, 16), (120, 106, 84, 255), 8)
    return img


# ---------------------------------------------------------------- 弹药 4 种
def art_scrap_nail():
    """14x14：废料钉。"""
    img, d = new(14)
    d.polygon([(1, 9), (10, 2), (12, 4), (3, 11)], fill=STEEL, outline=OUTLINE)
    d.polygon([(10, 2), (13, 3), (12, 4)], fill=STEEL_L)
    d.polygon([(1, 9), (3, 11), (0, 12)], fill=STEEL_D)
    d.line([(3, 8), (10, 3)], fill=STEEL_L)
    noise(d, 14, (2, 3, 12, 11), RUST_L, 5)
    return img


def art_coldlight_round():
    """14x14：冷光弹（青白弹头 + 光晕）。"""
    img, d = new(14)
    d.ellipse((1, 2, 12, 13), fill=COOL_D)
    d.ellipse((3, 4, 10, 11), fill=COOL)
    d.ellipse((5, 5, 9, 9), fill=COOL_L)
    d.ellipse((1, 2, 12, 13), outline=OUTLINE)
    d.point((5, 5), fill=(255, 255, 255, 255))
    d.point((11, 11), fill=(180, 230, 240, 200))
    return img


def art_ember_shell():
    """14x14：余烬弹（橙红弹头 + 火星）。"""
    img, d = new(14)
    d.ellipse((1, 2, 12, 13), fill=EMBER_D)
    d.ellipse((3, 4, 10, 11), fill=EMBER)
    d.ellipse((4, 4, 8, 8), fill=EMBER_L)
    d.ellipse((1, 2, 12, 13), outline=OUTLINE)
    d.point((10, 4), fill=(252, 214, 130, 230))
    d.point((3, 10), fill=(240, 160, 70, 200))
    return img


def art_torn_page():
    """16x16：残页（撕裂的纸 + 两行字）。"""
    img, d = new(16)
    d.polygon([(2, 3), (13, 2), (14, 12), (6, 14), (2, 12)], fill=PAPER, outline=OUTLINE)
    d.polygon([(3, 4), (12, 3), (12, 6), (3, 7)], fill=PAPER_L)
    # 撕口
    d.polygon([(6, 14), (8, 11), (10, 14)], fill=(0, 0, 0, 0))
    d.line([(5, 8), (12, 7)], fill=(96, 84, 66, 255))
    d.line([(5, 10), (11, 9)], fill=(96, 84, 66, 255))
    d.line([(5, 12), (9, 11)], fill=(96, 84, 66, 255))
    noise(d, 16, (3, 3, 14, 13), PAPER_D, 5)
    return img


# ---------------------------------------------------------------- 召唤物 3 种
def art_scavenger_beacon():
    """26x26：猎杀信标（三脚架天线 + 红灯）。"""
    img, d = new(26)
    # 三脚架
    d.line([(13, 12), (5, 24)], fill=STEEL_D, width=2)
    d.line([(13, 12), (21, 24)], fill=STEEL_D, width=2)
    d.line([(13, 12), (13, 24)], fill=STEEL, width=2)
    # 主体
    d.rectangle((9, 6, 17, 13), fill=STEEL, outline=OUTLINE)
    d.rectangle((10, 7, 13, 12), fill=STEEL_L)
    # 天线
    d.line([(13, 6), (13, 1)], fill=STEEL_L, width=1)
    d.line([(13, 3), (17, 2)], fill=STEEL_L)
    # 红灯
    d.ellipse((10, 15, 16, 21), fill=EMBER_D, outline=OUTLINE)
    d.ellipse((12, 17, 14, 19), fill=EMBER_L)
    return img


def art_audit_request():
    """26x26：审计申请单（盖了红章的表格）。"""
    img, d = new(26)
    d.rectangle((4, 2, 21, 24), fill=PAPER, outline=OUTLINE)
    d.rectangle((5, 3, 20, 6), fill=PAPER_L)
    for y in range(9, 21, 3):
        d.line([(6, y), (19, y)], fill=(120, 108, 88, 255))
    d.line([(6, 21), (14, 21)], fill=(120, 108, 88, 255))
    # 红章
    d.ellipse((13, 12, 22, 21), outline=(186, 52, 44, 255), width=2)
    d.line([(15, 14), (20, 19)], fill=(186, 52, 44, 255))
    d.line([(20, 14), (15, 19)], fill=(186, 52, 44, 255))
    return img


def art_ember_fuse():
    """26x26：余烬引信（阴燃的绳子 + 火星）。"""
    img, d = new(26)
    # 引信曲线
    pts = [(3, 22), (9, 19), (7, 14), (13, 11), (11, 6), (17, 3)]
    for i in range(len(pts) - 1):
        d.line([pts[i], pts[i + 1]], fill=(150, 116, 74, 255), width=3)
    for i in range(len(pts) - 1):
        d.line([pts[i], pts[i + 1]], fill=(198, 158, 104, 255), width=1)
    # 顶端火星
    d.ellipse((14, 0, 21, 7), fill=EMBER_D, outline=OUTLINE)
    d.ellipse((16, 2, 19, 5), fill=EMBER_L)
    d.point((22, 5), fill=(252, 214, 130, 230))
    d.point((13, 8), fill=(240, 150, 60, 200))
    return img


# ---------------------------------------------------------------- 信息类 2 种
def art_wasteland_map():
    """22x22：废土地图碎片（卷起的图纸 + 红线）。"""
    img, d = new(22)
    d.rectangle((2, 5, 19, 18), fill=PAPER_D, outline=OUTLINE)
    d.rectangle((3, 6, 18, 17), fill=PAPER)
    # 地形线
    d.line([(4, 13), (8, 9), (12, 12), (17, 8)], fill=(150, 118, 74, 255))
    d.line([(4, 15), (17, 15)], fill=(178, 160, 130, 255))
    # 红色标记
    d.line([(7, 8), (9, 10)], fill=(190, 58, 48, 255))
    d.line([(9, 8), (7, 10)], fill=(190, 58, 48, 255))
    d.ellipse((12, 11, 16, 15), outline=(190, 58, 48, 255))
    noise(d, 22, (3, 6, 19, 18), PAPER_D, 10)
    return img


def art_scrap_cache():
    """24x20：废料宝匣（铁皮箱 + 锁扣）。"""
    img, d = new(24)
    d.rectangle((1, 5, 22, 18), fill=RUST_D, outline=OUTLINE)
    d.rectangle((2, 6, 21, 11), fill=RUST)
    d.rectangle((2, 12, 21, 17), fill=(104, 62, 40, 255))
    # 箱盖缝
    d.line([(2, 11), (21, 11)], fill=OUTLINE)
    # 铁箍
    d.rectangle((5, 5, 7, 18), fill=STEEL_D, outline=OUTLINE)
    d.rectangle((16, 5, 18, 18), fill=STEEL_D, outline=OUTLINE)
    # 锁扣
    d.rectangle((10, 9, 14, 15), fill=GOLD, outline=OUTLINE)
    d.point((12, 11), fill=OUTLINE)
    d.point((12, 13), fill=OUTLINE)
    noise(d, 24, (2, 6, 22, 18), RUST_L, 12)
    return img


# ---------------------------------------------------------------- 主流程
ITEMS = [
    ("Content/Items/Materials/RustedGear.png", art_rusted_gear),
    ("Content/Items/Materials/Coke.png", art_coke),
    ("Content/Items/Materials/CircuitBoard.png", art_circuit_board),
    ("Content/Items/Materials/Coolant.png", art_coolant),
    ("Content/Items/Materials/AshCrystal.png", art_ash_crystal),
    ("Content/Items/Materials/SalvagedSteelBundle.png", art_steel_bundle),
    ("Content/Items/Consumables/GasFilterPotion.png", art_gas_filter_potion),
    ("Content/Items/Consumables/MinersSolution.png", art_miners_solution),
    ("Content/Items/Consumables/ColdSpotlight.png", art_cold_spotlight),
    ("Content/Items/Consumables/NaniteSalve.png", art_nanite_salve),
    ("Content/Items/Consumables/BeastWhistle.png", art_beast_whistle),
    ("Content/Items/Consumables/AshenRation.png", art_ashen_ration),
    ("Content/Items/Ammo/ScrapNail.png", art_scrap_nail),
    ("Content/Items/Ammo/ColdlightRound.png", art_coldlight_round),
    ("Content/Items/Ammo/EmberShell.png", art_ember_shell),
    ("Content/Items/Ammo/TornPage.png", art_torn_page),
    ("Content/Items/Summons/ScavengerBeacon.png", art_scavenger_beacon),
    ("Content/Items/Summons/AuditRequest.png", art_audit_request),
    ("Content/Items/Summons/EmberFuse.png", art_ember_fuse),
    ("Content/Items/Info/WastelandMap.png", art_wasteland_map),
    ("Content/Items/Info/ScrapCache.png", art_scrap_cache),
]

BUFFS = [
    "GasFilterBuff",
    "MinersSolutionBuff",
    "ColdSpotlightBuff",
    "NaniteSalveBuff",
    "BeastWhistleBuff",
    "AshenRationBuff",
]

PROJECTILES = [
    "ScrapNailProjectile",
    "ColdlightProjectile",
    "EmberShellProjectile",
    "TornPageProjectile",
]


def make_buff_icon(kind):
    """32x32 Buff 图标：圆底 + 各自的主题符号。"""
    size = 32
    img, d = new(size)
    ring = {
        "GasFilterBuff": ((40, 70, 52, 255), (96, 152, 104, 255)),
        "MinersSolutionBuff": ((78, 56, 26, 255), (168, 126, 62, 255)),
        "ColdSpotlightBuff": ((24, 62, 84, 255), (86, 176, 200, 255)),
        "NaniteSalveBuff": ((28, 74, 58, 255), (92, 190, 148, 255)),
        "BeastWhistleBuff": ((84, 40, 30, 255), (204, 108, 74, 255)),
        "AshenRationBuff": ((74, 62, 40, 255), (188, 164, 108, 255)),
    }[kind]
    d.ellipse((1, 1, 30, 30), fill=ring[0], outline=ring[1], width=2)
    d.ellipse((4, 4, 27, 27), fill=(ring[0][0] + 14, ring[0][1] + 14, ring[0][2] + 14, 255))

    if kind == "GasFilterBuff":
        d.ellipse((10, 10, 21, 22), fill=(214, 224, 214, 255), outline=OUTLINE)
        d.rectangle((11, 22, 20, 25), fill=(150, 158, 150, 255), outline=OUTLINE)
    elif kind == "MinersSolutionBuff":
        d.polygon([(6, 24), (16, 7), (23, 12), (12, 26)], fill=STEEL, outline=OUTLINE)
        d.polygon([(16, 7), (23, 12), (18, 15)], fill=STEEL_L)
    elif kind == "ColdSpotlightBuff":
        d.ellipse((9, 9, 22, 22), fill=(196, 240, 250, 255))
        for (dx, dy) in [(0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, 1), (-1, 1), (1, -1)]:
            d.line([(16 + dx * 8, 16 + dy * 8), (16 + dx * 11, 16 + dy * 11)], fill=(226, 250, 255, 255))
    elif kind == "NaniteSalveBuff":
        d.line([(10, 16), (22, 16)], fill=(238, 255, 246, 255), width=3)
        d.line([(16, 10), (16, 22)], fill=(238, 255, 246, 255), width=3)
        d.ellipse((6, 6, 11, 11), fill=(180, 240, 214, 220))
    elif kind == "BeastWhistleBuff":
        d.polygon([(6, 14), (20, 9), (24, 16), (20, 23), (6, 21)], fill=(230, 220, 200, 255), outline=OUTLINE)
        d.ellipse((20, 13, 25, 19), fill=OUTLINE)
    elif kind == "AshenRationBuff":
        d.polygon([(6, 12), (25, 9), (27, 21), (8, 24)], fill=(214, 198, 162, 255), outline=OUTLINE)
        d.line([(16, 10), (17, 23)], fill=OUTLINE)
    return img


def make_projectile_icon(kind):
    """弹药弹幕贴图（尺寸与 Projectile.width/height 一致）。"""
    if kind == "ScrapNailProjectile":
        size = 14
        img, d = new(size)
        d.polygon([(1, 9), (10, 2), (12, 4), (3, 11)], fill=STEEL, outline=OUTLINE)
        d.polygon([(10, 2), (13, 3), (12, 4)], fill=STEEL_L)
        d.line([(3, 8), (10, 3)], fill=STEEL_L)
        return img
    if kind == "ColdlightProjectile":
        size = 12
        img, d = new(size)
        d.polygon([(0, 5), (8, 2), (11, 5), (8, 9), (0, 7)], fill=COOL, outline=OUTLINE)
        d.polygon([(1, 5), (8, 4), (9, 6), (2, 7)], fill=COOL_L)
        d.point((9, 4), fill=(255, 255, 255, 255))
        return img
    if kind == "EmberShellProjectile":
        size = 16
        img, d = new(size)
        d.ellipse((1, 3, 13, 14), fill=EMBER_D, outline=OUTLINE)
        d.ellipse((3, 5, 11, 12), fill=EMBER)
        d.ellipse((4, 5, 8, 9), fill=EMBER_L)
        d.polygon([(12, 6), (15, 8), (12, 10)], fill=EMBER_L, outline=OUTLINE)
        return img
    # TornPageProjectile
    size = 16
    img, d = new(size)
    d.rectangle((2, 2, 13, 13), fill=PAPER, outline=OUTLINE)
    d.rectangle((3, 3, 12, 5), fill=PAPER_L)
    for y in (7, 9, 11):
        d.line([(4, y), (11, y)], fill=(120, 108, 88, 255))
    noise(d, size, (3, 3, 13, 13), PAPER_D, 5)
    return img


def make_preview(images):
    """把所有图标排成一张预览图。"""
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    cell = 40
    cols = 6
    rows = (len(images) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell + 8, rows * cell + 8), (34, 32, 36, 255))
    for index, (label, img) in enumerate(images):
        cx = 4 + (index % cols) * cell
        cy = 4 + (index // cols) * cell
        # 放大整数倍，保持像素锐利
        scale = max(1, 32 // max(img.width, img.height))
        big = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
        sheet.alpha_composite(big, (cx + (cell - big.width) // 2, cy + (cell - big.height) // 2))
    out = os.path.join(PREVIEW_DIR, "preview_materialscraft.png")
    sheet.save(out)
    print("preview -> %s %s" % (out, sheet.size))


def main():
    images = []
    for rel, fn in ITEMS:
        img = fn()
        save(img, rel)
        images.append((os.path.basename(rel), img))

    for name in BUFFS:
        img = make_buff_icon(name)
        save(img, "Content/Buffs/%s.png" % name)
        images.append((name, img))

    for name in PROJECTILES:
        img = make_projectile_icon(name)
        save(img, "Content/Projectiles/Ranged/%s.png" % name)
        images.append((name, img))

    make_preview(images)
    print("done: %d icons" % len(images))


if __name__ == "__main__":
    main()
