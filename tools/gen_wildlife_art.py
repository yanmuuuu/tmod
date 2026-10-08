# -*- coding: utf-8 -*-
"""小怪扩充（批次 W7）这一批的贴图：11 只小怪 + 1 只迷你 Boss + 2 个物品 + 1 个弹幕。

风格：与既有的 ScrapCrawler / IndexMoth / AshStalker / HearthWarden 统一 ——
     废土灰（哑光冷金属）+ 灰烬棕 + 锈橙，扁平像素块，带一点左上高光 / 右下压暗。
纯程序化生成（PIL），不依赖任何外部素材。
帧表一律**竖直**堆叠，帧高 = 贴图高 / 帧数。

输出清单（尺寸必须和代码里的 NPC.width/height 严格一致）：
  Content/NPCs/Wildlife/ScrapLeaper.png     40x120  4 帧 (40x30)  地表白天·跳跃扑咬
  Content/NPCs/Wildlife/RustCharger.png     44x120  4 帧 (44x30)  地表白天·冲锋
  Content/NPCs/Wildlife/GaleWisp.png        40x120  4 帧 (40x30)  夜晚·飞行追踪
  Content/NPCs/Wildlife/SpitterFly.png      36x120  4 帧 (36x30)  夜晚·远程喷吐
  Content/NPCs/Wildlife/CaveCrawler.png     36x144  4 帧 (36x36)  地下·爬墙
  Content/NPCs/Wildlife/AshTickBat.png      34x136  4 帧 (34x34)  地下·蝙蝠式乱飞
  Content/NPCs/Wildlife/PollutionSlime.png  40x120  4 帧 (40x30)  困难·施加减益
  Content/NPCs/Wildlife/GearSwarm.png       32x32   4 帧 帧高 8   困难·召唤小弟（半径 16 旋转）
  Content/NPCs/Wildlife/CinderMender.png    46x184  4 帧 (46x46)  困难·自我治疗/护盾
  Content/NPCs/Wildlife/ScrapReaper.png     84x252  3 帧 (84x84)  迷你 Boss
  Content/Items/Materials/RustedGear.png    20x20   材料
  Content/Items/Weapons/Wildlife/ScrapReaperBlade.png 32x32 迷你 Boss 必掉武器
  Content/Projectiles/Wildlife/SpitterGlob.png   14x14 喷吐弹
  Content/Projectiles/Wildlife/ReaperScrapShot.png 16x16 迷你 Boss 弹
  Content/Projectiles/Wildlife/ReaperShockwave.png 28x28 迷你 Boss 冲击波
  Content/Buffs/ScrapShield.png             32x32   增益图标（迷你 Boss 护盾，也给玩家看）
另存一张拼图预览到 art-inbox/preview_wildlife.png。
"""
import os
import random

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOD = os.path.join(ROOT, "WastelandSoul")
NPC_DIR = os.path.join(MOD, "Content", "NPCs", "Wildlife")
ITEM_MAT_DIR = os.path.join(MOD, "Content", "Items", "Materials")
ITEM_WEP_DIR = os.path.join(MOD, "Content", "Items", "Weapons", "Wildlife")
PROJ_DIR = os.path.join(MOD, "Content", "Projectiles", "Wildlife")
BUFF_DIR = os.path.join(MOD, "Content", "Buffs")
INBOX = os.path.join(ROOT, "art-inbox")

# ---------------------------------------------------------------- 配色
# 废土冷金属灰
METAL_DARK = (44, 48, 54)
METAL = (78, 84, 92)
METAL_LIGHT = (120, 128, 138)
METAL_HI = (168, 178, 190)
# 灰烬棕
ASH_DARK = (46, 36, 30)
ASH = (84, 66, 52)
ASH_LIGHT = (126, 100, 74)
# 锈橙 / 警示橙
RUST_DARK = (112, 54, 22)
RUST = (188, 92, 32)
RUST_LIGHT = (232, 148, 58)
# 污染绿（对应 Pollution 减益）
POLLUTE_DARK = (40, 62, 34)
POLLUTE = (86, 122, 54)
POLLUTE_LIGHT = (146, 180, 84)
# 冷光青（科技感）
CYAN_DARK = (36, 74, 86)
CYAN = (88, 168, 186)
CYAN_LIGHT = (176, 232, 240)
# 护盾蓝白
SHIELD_DARK = (52, 84, 116)
SHIELD = (120, 178, 224)
SHIELD_LIGHT = (216, 240, 255)
# 灰白（齿 / 骨）
BONE = (196, 188, 170)
BONE_DARK = (128, 120, 104)


def canvas(w, h, frames):
    """建一张 (w, h*frames) 的竖排帧表。"""
    return Image.new("RGBA", (w, h * frames), (0, 0, 0, 0))


def rng_of(seed):
    return random.Random(seed)


def put(img, x, y, color, alpha=255):
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), (color[0], color[1], color[2], alpha))


def grain(img, ox, oy, w, h, base, dark, light, seed, dark_p=0.18, light_p=0.16):
    """在一块矩形区域里撒噪点，做出"废金属"的哑光颗粒感。"""
    rng = rng_of(seed)

    for y in range(oy, oy + h):
        for x in range(ox, ox + w):
            if not (0 <= x < img.width and 0 <= y < img.height):
                continue

            roll = rng.random()

            if roll < dark_p:
                put(img, x, y, dark)
            elif roll < dark_p + light_p:
                put(img, x, y, light)
            else:
                put(img, x, y, base)


def ellipse_fill(img, cx, cy, rx, ry, color, seed=0, shade=None, highlight=None):
    rng = rng_of(seed)

    for y in range(int(cy - ry), int(cy + ry) + 1):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            dx = (x - cx) / max(1.0, rx)
            dy = (y - cy) / max(1.0, ry)

            if dx * dx + dy * dy > 1.0:
                continue

            c = color

            if shade is not None and (dx + dy) > 0.45 and rng.random() < 0.75:
                c = shade
            if highlight is not None and (dx + dy) < -0.5 and rng.random() < 0.6:
                c = highlight

            put(img, x, y, c)


def rect_fill(img, x0, y0, x1, y1, color, shade=None, highlight=None, seed=0):
    rng = rng_of(seed)

    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            c = color

            if shade is not None and y >= (y0 + y1) / 2 and rng.random() < 0.5:
                c = shade
            if highlight is not None and y <= y0 + 1 and rng.random() < 0.5:
                c = highlight

            put(img, x, y, c)


def legs(img, x_list, y_top, length, color, phase):
    """三条细腿，相位 phase 决定哪条抬高。"""
    for i, lx in enumerate(x_list):
        lift = 1 if (i % 3) == (phase % 3) else 0
        y0 = y_top - lift

        for k in range(length):
            put(img, lx + (k // 3), y0 + k, color)
        put(img, lx + (length // 3), y0 + length, color)


def outline_dark(img, ox, oy, w, h, color):
    """给区域内不透明的像素描一圈外轮廓（只描外侧）。"""
    src = img.copy()

    for y in range(oy, oy + h):
        for x in range(ox, ox + w):
            if src.getpixel((x, y))[3] != 0:
                continue

            touched = False

            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy

                if ox <= nx < ox + w and oy <= ny < oy + h and src.getpixel((nx, ny))[3] != 0:
                    touched = True
                    break

            if touched:
                put(img, x, y, color)


def save(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print("  %s  %dx%d" % (os.path.relpath(path, ROOT), img.width, img.height))


# ================================================================ 地表白天 · 废料跳虫
def scrap_leaper():
    w, h, frames = 40, 30, 4
    img = canvas(w, h, frames)
    seeds = (3071, 3072, 3073, 3074)

    for f in range(frames):
        oy = f * h
        rng = rng_of(seeds[f])
        crouch = (0, 2, -2, 1)[f]

        # 身体：压扁的椭圆
        body_cx, body_cy = 20, oy + 18 + crouch
        ellipse_fill(img, body_cx, body_cy, 14, 8, METAL, seed=seeds[f], shade=METAL_DARK, highlight=METAL_LIGHT)
        # 分节纹
        for x in (11, 17, 23, 29):
            for y in range(int(body_cy - 6), int(body_cy + 5)):
                if rng.random() < 0.8:
                    put(img, x, y, METAL_DARK)
        # 背上的锈橙警示块
        rect_fill(img, 24, int(body_cy - 5), 30, int(body_cy - 1), RUST, shade=RUST_DARK, highlight=RUST_LIGHT, seed=seeds[f] + 1)
        # 头（前方朝右）
        ellipse_fill(img, 33, int(body_cy - 1), 5, 4, METAL_DARK, seed=seeds[f] + 2, shade=METAL_DARK)
        # 复眼
        put(img, 34, int(body_cy - 3), RUST_LIGHT)
        put(img, 35, int(body_cy - 2), RUST_LIGHT)
        put(img, 30, int(body_cy - 4), RUST)
        # 大颚
        put(img, 37, int(body_cy), BONE)
        put(img, 38, int(body_cy) + 1, BONE)
        put(img, 36, int(body_cy) + 2, BONE_DARK)
        # 腿：跳跃时收拢，落地时展开
        leg_len = (7, 4, 9, 6)[f]
        legs(img, [10, 16, 22], int(body_cy + 6), leg_len, METAL_DARK, f)
        outline_dark(img, 0, oy, w, h, (24, 26, 30))

    return img


# ================================================================ 地表白天 · 锈甲冲锋兽
def rust_charger():
    w, h, frames = 44, 30, 4
    img = canvas(w, h, frames)
    seeds = (4101, 4102, 4103, 4104)

    for f in range(frames):
        oy = f * h
        rng = rng_of(seeds[f])
        bob = (0, 1, 0, -1)[f]

        # 躯干
        rect_fill(img, 8, oy + 10 + bob, 34, oy + 20 + bob, ASH, shade=ASH_DARK, highlight=ASH_LIGHT, seed=seeds[f])
        # 头部（右侧前倾）
        rect_fill(img, 32, oy + 9 + bob, 40, oy + 18 + bob, ASH_DARK, shade=ASH_DARK, highlight=ASH, seed=seeds[f] + 1)
        # 头甲 + 撞角
        rect_fill(img, 30, oy + 7 + bob, 38, oy + 9 + bob, METAL, shade=METAL_DARK, highlight=METAL_LIGHT, seed=seeds[f] + 2)
        for i, (dx, dy) in enumerate(((1, 0), (2, -1), (3, -2), (4, -2))):
            put(img, 38 + dx, oy + 9 + bob + dy, BONE if i % 2 else BONE_DARK)
        put(img, 41, oy + 6 + bob, BONE)
        put(img, 41, oy + 7 + bob, BONE)
        # 眼睛
        put(img, 35, oy + 12 + bob, RUST_LIGHT)
        put(img, 36, oy + 12 + bob, RUST)
        # 背上的锈甲板
        for i in range(3):
            bx = 12 + i * 7
            rect_fill(img, bx, oy + 7 + bob, bx + 5, oy + 10 + bob, RUST, shade=RUST_DARK, highlight=RUST_LIGHT, seed=seeds[f] + i)
        # 四条腿
        for i, lx in enumerate((12, 18, 26, 33)):
            lift = (0, 2, 1, 0)[(i + f) % 4]
            for k in range(8 - lift):
                put(img, lx, oy + 21 + bob + k, ASH_DARK if k % 2 else ASH)
            put(img, lx, oy + 28 + bob, METAL_DARK)
        outline_dark(img, 0, oy, w, h, (22, 20, 18))

    return img


# ================================================================ 夜晚 · 疾风鬼火（飞行追踪）
def gale_wisp():
    w, h, frames = 40, 30, 4
    img = canvas(w, h, frames)
    seeds = (5201, 5202, 5203, 5204)
    spread = (18, 6, 18, 6)
    lift = (0, 4, 0, 4)
    # 扇翅时翅膀在竖直方向上的倾角（上扇 / 下扇）
    tilt = (-1, 1, -1, 1)

    for f in range(frames):
        oy = f * h
        rng = rng_of(seeds[f])
        cy = oy + 16 + lift[f]
        sp = spread[f]

        # 翅膀：一大块实心膜翼，边缘做阶梯状，扇动时整体上下倾斜
        for side in (-1, 1):
            for i in range(sp):
                x0 = 20 + side * (3 + i)
                span = max(2, 10 - abs(i - sp // 2) * 2)
                dy_center = tilt[f] * (i * 0.45)

                for y in range(int(cy + dy_center - span), int(cy + dy_center + span) + 1):
                    c = METAL_LIGHT if i < sp // 2 else METAL

                    if rng.random() < 0.18:
                        c = METAL_HI
                    elif rng.random() < 0.12:
                        c = METAL_DARK

                    put(img, x0, y, c)

            # 翼骨（更深的线）
            for i in range(sp):
                put(img, 20 + side * (3 + i), int(cy + tilt[f] * (i * 0.45)), (36, 40, 46))

            # 翼端的冷光斑点
            for k in range(3):
                put(img, 20 + side * (3 + sp - 2 + (k % 2)), int(cy + tilt[f] * (sp * 0.45)) + k - 1, CYAN)

        # 核心（较大的椭球，中央发亮）
        ellipse_fill(img, 20, cy, 5, 7, CYAN, seed=seeds[f], shade=CYAN_DARK, highlight=CYAN_LIGHT)
        ellipse_fill(img, 20, cy, 3, 4, CYAN_LIGHT, seed=seeds[f] + 5)
        ellipse_fill(img, 20, cy + 1, 1.5, 2, (255, 255, 255), seed=seeds[f] + 6)

        # 两只红点眼
        put(img, 17, cy - 3, (240, 90, 70))
        put(img, 18, cy - 3, (240, 90, 70))
        put(img, 21, cy - 3, (240, 90, 70))
        put(img, 22, cy - 3, (240, 90, 70))

        # 下方三条飘须
        for i in range(3):
            for k in range(3):
                put(img, 18 + i * 2, cy + 8 + k, METAL if k < 2 else METAL_DARK)

        outline_dark(img, 0, oy, w, h, (26, 32, 38))

    return img


# ================================================================ 夜晚 · 酸囊喷吐蝇（远程喷吐）
def spitter_fly():
    w, h, frames = 36, 30, 4
    img = canvas(w, h, frames)
    seeds = (6301, 6302, 6303, 6304)

    for f in range(frames):
        oy = f * h
        rng = rng_of(seeds[f])
        cy = oy + 14
        # 两对透明翅
        for side in (-1, 1):
            for i in range(11):
                x0 = 18 + side * (2 + i)
                span = max(1, 5 - abs(i - 6) // 2)
                for y in range(cy - span, cy + span + 1):
                    put(img, x0, y, (170, 200, 200), 110 if i % 2 else 160)
        # 身体（细长，深绿）
        ellipse_fill(img, 18, cy + 2, 5, 8, POLLUTE_DARK, seed=seeds[f], shade=(30, 46, 26), highlight=POLLUTE)
        # 环节
        for y in range(cy - 3, cy + 9):
            if rng.random() < 0.7:
                put(img, 18, y, (30, 46, 26))
        # 酸囊（鼓起的腹部，亮绿）
        ellipse_fill(img, 18, cy + 8, 4, 3, POLLUTE, seed=seeds[f] + 1, shade=POLLUTE_DARK, highlight=POLLUTE_LIGHT)
        # 头 + 口器
        ellipse_fill(img, 18, cy - 5, 4, 3, POLLUTE_DARK, seed=seeds[f] + 2)
        put(img, 16, cy - 6, POLLUTE_LIGHT)
        put(img, 20, cy - 6, POLLUTE_LIGHT)
        put(img, 17, cy - 8, BONE_DARK)
        put(img, 19, cy - 8, BONE_DARK)
        put(img, 18, cy - 8, BONE)
        # 六条细腿
        legs(img, [13, 17, 21], cy + 8, 5, (36, 40, 34), f)
        outline_dark(img, 0, oy, w, h, (22, 28, 22))

    return img


# ================================================================ 地下 · 穴居攀爬者（爬墙）
def cave_crawler():
    w, h, frames = 36, 36, 4
    img = canvas(w, h, frames)
    seeds = (7401, 7402, 7403, 7404)

    for f in range(frames):
        oy = f * h
        rng = rng_of(seeds[f])
        cx = 18
        shift = (0, 2, 0, -2)[f]
        cy = oy + 20 + shift

        # 甲壳躯干
        ellipse_fill(img, cx, cy, 10, 7, ASH_DARK, seed=seeds[f], shade=(32, 24, 20), highlight=ASH)
        # 背甲棱
        for i in range(4):
            x0 = cx - 7 + i * 4
            for y in range(cy - 6, cy + 1):
                if rng.random() < 0.6:
                    put(img, x0, y, ASH)
        # 头（朝上，爬墙时抬头）
        ellipse_fill(img, cx, cy - 9, 4, 4, ASH_DARK, seed=seeds[f] + 1, shade=(30, 22, 18), highlight=ASH_LIGHT)
        # 四只眼
        for dx, dy in ((-3, -10), (-1, -11), (2, -11), (4, -10)):
            put(img, cx + dx, cy + dy, RUST_LIGHT)
        # 两把螯
        put(img, cx - 5, cy - 11, BONE)
        put(img, cx - 6, cy - 12, BONE)
        put(img, cx - 6, cy - 13, BONE_DARK)
        put(img, cx + 5, cy - 11, BONE)
        put(img, cx + 6, cy - 12, BONE)
        put(img, cx + 6, cy - 13, BONE_DARK)
        # 六条带钩的足（左右各三，向外伸）
        for i in range(3):
            yy = cy - 3 + i * 4
            reach = (9, 11, 9)[i] + (1 if (i + f) % 2 == 0 else 0)
            for k in range(reach):
                put(img, cx - 7 - k, yy + k // 2, METAL_DARK)
                put(img, cx + 7 + k, yy + k // 2, METAL_DARK)
            put(img, cx - 7 - reach, yy + reach // 2, METAL)
            put(img, cx + 7 + reach, yy + reach // 2, METAL)
        outline_dark(img, 0, oy, w, h, (20, 18, 16))

    return img


# ================================================================ 地下 · 灰蜱蝠（蝙蝠式乱飞）
def ash_tick_bat():
    w, h, frames = 34, 34, 4
    img = canvas(w, h, frames)
    seeds = (8501, 8502, 8503, 8504)
    spread = (15, 7, 15, 7)
    flap = (-8, 4, -8, 4)

    for f in range(frames):
        oy = f * h
        rng = rng_of(seeds[f])
        cy = oy + 17 + flap[f] // 4
        sp = spread[f]
        tilt = -1 if flap[f] < 0 else 1

        # 膜翼：实心块 + 锯齿下缘
        for side in (-1, 1):
            for i in range(sp):
                x0 = 17 + side * (2 + i)
                base_span = max(2, 8 - abs(i - sp // 2))
                dy_center = tilt * (i * 0.5)

                for y in range(int(cy + dy_center - base_span), int(cy + dy_center + base_span * 0.6) + 1):
                    c = ASH_LIGHT if i < 3 else (ASH if (y + i) % 3 else ASH_DARK)
                    put(img, x0, y, c)

            # 锯齿下缘：挖掉几格
            for i in range(1, sp, 2):
                for k in range(2):
                    put(img, 17 + side * (2 + i), int(cy + tilt * (i * 0.5) + 3 + k), (0, 0, 0), 0)

            # 翼骨
            for i in range(sp):
                put(img, 17 + side * (2 + i), int(cy + tilt * (i * 0.5)), (26, 20, 16))
                put(img, 17 + side * (2 + i), int(cy + tilt * (i * 0.5)) + 1, (38, 30, 24))

        # 身体（毛茸茸）
        ellipse_fill(img, 17, cy + 1, 5, 7, (28, 22, 18), seed=seeds[f], shade=(18, 14, 12), highlight=(58, 46, 36))

        for _ in range(34):
            x = 17 + rng.randint(-5, 5)
            y = cy + 1 + rng.randint(-7, 7)

            if img.getpixel((x, y))[3] != 0:
                put(img, x, y, (62, 48, 36))

        # 尖耳
        for dx in (-3, 3):
            put(img, 17 + dx, cy - 8, ASH_DARK)
            put(img, 17 + dx, cy - 9, ASH_DARK)
            put(img, 17 + dx + (1 if dx > 0 else -1), cy - 10, ASH_DARK)

        # 发光的眼睛
        put(img, 14, cy - 3, (250, 150, 60))
        put(img, 15, cy - 3, (250, 180, 90))
        put(img, 19, cy - 3, (250, 150, 60))
        put(img, 20, cy - 3, (250, 180, 90))

        # 吸血口器
        put(img, 17, cy + 7, RUST_DARK)
        put(img, 17, cy + 8, RUST_DARK)
        put(img, 16, cy + 7, (90, 40, 20))
        put(img, 18, cy + 7, (90, 40, 20))

        outline_dark(img, 0, oy, w, h, (16, 14, 12))

    return img


# ================================================================ 困难 · 污染黏体（施加减益）
def pollution_slime():
    w, h, frames = 40, 30, 4
    img = canvas(w, h, frames)
    seeds = (9601, 9602, 9603, 9604)
    squash = ((12, 10), (10, 12), (12, 10), (9, 13))

    for f in range(frames):
        oy = f * h
        rng = rng_of(seeds[f])
        rx, ry = squash[f]
        cx, cy = 20, oy + h - 3 - ry

        # 凝胶主体
        ellipse_fill(img, cx, cy, rx, ry, POLLUTE, seed=seeds[f], shade=POLLUTE_DARK, highlight=POLLUTE_LIGHT)
        # 气泡 / 悬浮污染物
        for _ in range(18):
            bx = cx + rng.randint(-rx + 1, rx - 1)
            by = cy + rng.randint(-ry + 1, ry - 1)
            put(img, bx, by, POLLUTE_LIGHT if rng.random() < 0.5 else POLLUTE_DARK)
        # 内部嵌着的废旧金属块
        rect_fill(img, cx - 6, cy - 2, cx - 2, cy + 2, METAL, shade=METAL_DARK, highlight=METAL_LIGHT, seed=seeds[f] + 1)
        rect_fill(img, cx + 2, cy + 1, cx + 5, cy + 3, METAL_DARK, shade=METAL_DARK, highlight=METAL, seed=seeds[f] + 2)
        # 两只无神眼
        put(img, cx - 5, cy - 4, (18, 24, 16))
        put(img, cx - 4, cy - 4, (18, 24, 16))
        put(img, cx + 3, cy - 4, (18, 24, 16))
        put(img, cx + 4, cy - 4, (18, 24, 16))
        outline_dark(img, 0, oy, w, h, (22, 32, 18))

    return img


# ================================================================ 困难 · 齿轮群（召唤小弟）
def gear_swarm():
    """32x32 圆形齿轮，**单帧**（Main.npcFrameCount = 1）。

    为什么不做帧表：齿轮半径 14 像素，而 tModLoader 的帧表是**竖直**堆叠的，
    4 帧就意味着帧高只有 8 像素、只能画出半径 4 的小齿轮。所以这里的"转动"交给代码：
    在 AI 里直接改 `NPC.rotation`，贴图只用这一张 32x32 的静态齿轮。"""
    import math

    w, h = 32, 32
    img = canvas(w, h, 1)
    rng = rng_of(10701)
    cx, cy = 16, 16

    # 齿（12 枚）
    for i in range(12):
        ang = i * math.pi / 6
        tx = cx + math.cos(ang) * 11.0
        ty = cy + math.sin(ang) * 11.0
        rect_fill(img, int(tx) - 1, int(ty) - 1, int(tx) + 1, int(ty) + 1,
                  METAL_LIGHT, shade=METAL_DARK, highlight=METAL_HI, seed=10701 + i)

    # 轮盘
    ellipse_fill(img, cx, cy, 9.5, 9.5, METAL, seed=10701, shade=METAL_DARK, highlight=METAL_LIGHT)

    # 减重孔（透空）
    for i in range(5):
        ang = i * 2 * math.pi / 5
        hx = cx + math.cos(ang) * 5.5
        hy = cy + math.sin(ang) * 5.5

        for y in range(int(hy) - 1, int(hy) + 2):
            for x in range(int(hx) - 1, int(hx) + 2):
                put(img, x, y, (0, 0, 0), 0)

    # 轴心
    ellipse_fill(img, cx, cy, 2.6, 2.6, RUST, seed=10702, shade=RUST_DARK, highlight=RUST_LIGHT)

    # 锈斑
    for _ in range(16):
        x = cx + rng.randint(-9, 9)
        y = cy + rng.randint(-9, 9)

        if img.getpixel((x, y))[3] != 0:
            put(img, x, y, RUST_DARK)

    outline_dark(img, 0, 0, w, h, (24, 26, 30))
    return img


# ================================================================ 困难 · 煤渣修补者（自我治疗 / 护盾）
def cinder_mender():
    w, h, frames = 46, 46, 4
    img = canvas(w, h, frames)
    seeds = (11801, 11802, 11803, 11804)

    for f in range(frames):
        oy = f * h
        rng = rng_of(seeds[f])
        bob = (0, 1, 0, -1)[f]
        cx, cy = 23, oy + 26 + bob

        # 下肢支架
        for lx in (14, 30):
            for k in range(8):
                put(img, lx, cy + 6 + k, METAL_DARK)
            put(img, lx, cy + 14, METAL)
            put(img, lx - 1, cy + 14, METAL)
        # 躯干（冷金属罐体）
        rect_fill(img, cx - 9, cy - 8, cx + 9, cy + 7, METAL, shade=METAL_DARK, highlight=METAL_LIGHT, seed=seeds[f])
        # 罐体环箍
        for y in (cy - 4, cy + 3):
            for x in range(cx - 9, cx + 10):
                put(img, x, y, METAL_DARK)
        # 冷却管（两侧，冷光青）
        for side in (-1, 1):
            for k in range(12):
                put(img, cx + side * 11, cy - 6 + k, CYAN_DARK)
            put(img, cx + side * 11, cy - 2, CYAN)
            put(img, cx + side * 11, cy + 1, CYAN_LIGHT)
        # 头部
        ellipse_fill(img, cx, cy - 13, 7, 6, METAL_DARK, seed=seeds[f] + 1, shade=(32, 36, 42), highlight=METAL)
        # 面罩（单眼）
        rect_fill(img, cx - 5, cy - 15, cx + 5, cy - 12, (24, 28, 34), shade=(18, 22, 26), highlight=METAL_DARK, seed=seeds[f] + 2)
        eye = CYAN_LIGHT if f % 2 == 0 else CYAN
        rect_fill(img, cx - 3, cy - 14, cx + 3, cy - 13, eye, seed=seeds[f] + 3)
        # 头顶的修补喷口
        rect_fill(img, cx - 2, cy - 21, cx + 2, cy - 18, METAL, shade=METAL_DARK, highlight=METAL_LIGHT, seed=seeds[f] + 4)
        # 手臂（一侧持修补喷枪）
        for k in range(7):
            put(img, cx - 12 - k // 2, cy - 5 + k, METAL_DARK)
        for k in range(7):
            put(img, cx + 12 + k // 2, cy - 5 + k, METAL_DARK)
        put(img, cx + 17, cy + 1, RUST)
        put(img, cx + 18, cy + 2, RUST_DARK)
        # 煤渣缝里透出的红光
        for _ in range(10):
            put(img, cx + rng.randint(-8, 8), cy + rng.randint(-7, 6), (200, 70, 30))
        outline_dark(img, 0, oy, w, h, (20, 24, 28))

    return img


# ================================================================ 迷你 Boss · 废料收割者
def scrap_reaper():
    """迷你 Boss：84x84 一帧，3 帧竖直。

    构图（按 84x84 的格子排）：头顶排气管 → 面甲三眼 → 宽肩 → 双臂（左刃 / 右爪）
    → 胸口锈橙核心 → 液压腿。所有元素都收在 84 以内，避免被帧边界切掉。"""
    w, h, frames = 84, 84, 3
    img = canvas(w, h, frames)
    seeds = (12901, 12902, 12903)

    for f in range(frames):
        oy = f * h
        rng = rng_of(seeds[f])
        cx = 42
        bob = (0, -2, 0)[f]
        cy = oy + 46 + bob

        # ---------------- 排气管（头顶，y 6~16）
        for i in range(3):
            tx = cx - 9 + i * 9
            rect_fill(img, tx - 2, oy + 6, tx + 2, oy + 17,
                      METAL_DARK, shade=(30, 34, 40), highlight=METAL, seed=seeds[f] + 20 + i)
            put(img, tx - 1, oy + 6, (96, 96, 104))
            put(img, tx, oy + 6, (120, 120, 128))
            put(img, tx + 1, oy + 6, (72, 72, 80))
            # 管口的锈
            put(img, tx - 2, oy + 16, RUST_DARK)
            put(img, tx + 2, oy + 14, RUST_DARK)

        # ---------------- 头 / 面甲（y 16~34）
        ellipse_fill(img, cx, oy + 27, 14, 11, METAL_DARK, seed=seeds[f] + 9, shade=(28, 32, 38), highlight=METAL)
        rect_fill(img, cx - 11, oy + 22, cx + 11, oy + 30,
                  (22, 26, 32), shade=(16, 20, 24), highlight=(58, 64, 74), seed=seeds[f] + 10)
        # 三只眼（横排）
        eye = (255, 208, 140) if f != 1 else (255, 248, 220)

        for dx in (-7, 0, 7):
            put(img, cx + dx, oy + 25, eye)
            put(img, cx + dx, oy + 26, RUST)
            put(img, cx + dx - 1, oy + 26, RUST_DARK)
            put(img, cx + dx + 1, oy + 26, RUST_DARK)

        # 面甲上的裂纹
        for k in range(5):
            put(img, cx - 8 + k * 4, oy + 28 + (k % 2), RUST_DARK)

        # ---------------- 肩甲（y 34~48）
        for side in (-1, 1):
            ellipse_fill(img, cx + side * 27, oy + 40, 11, 9, METAL, seed=seeds[f] + side, shade=METAL_DARK, highlight=METAL_LIGHT)

            for i in range(4):
                put(img, cx + side * (33 + i - 1), oy + 34 + i, RUST if i % 2 else RUST_DARK)

        # ---------------- 双臂
        # 左臂（画面左）：垂到腰下，末端一把大镰刃
        for k in range(18):
            put(img, cx - 33 - k // 4, oy + 46 + k, METAL_DARK)
            put(img, cx - 34 - k // 4, oy + 46 + k, METAL)

        for k in range(14):
            put(img, cx - 38 - k // 2, oy + 62 + k // 2, BONE if k % 3 else BONE_DARK)
            put(img, cx - 39 - k // 2, oy + 62 + k // 2, BONE_DARK)

        # 右臂（画面右）：向前伸，末端三爪
        for k in range(16):
            put(img, cx + 33 + k // 5, oy + 44 + k, METAL_DARK)
            put(img, cx + 34 + k // 5, oy + 44 + k, METAL)

        for dx in (-4, 0, 4):
            for k in range(11):
                put(img, cx + 37 + dx + k // 4, oy + 58 + k, METAL_DARK if k % 3 else BONE_DARK)

        # ---------------- 躯干（y 42~72）
        ellipse_fill(img, cx, cy, 22, 16, METAL, seed=seeds[f], shade=METAL_DARK, highlight=METAL_LIGHT)
        rect_fill(img, cx - 20, cy + 2, cx + 20, cy + 13,
                  METAL_DARK, shade=(38, 42, 48), highlight=METAL, seed=seeds[f] + 3)
        # 腰线
        for x in range(cx - 20, cx + 21):
            put(img, x, cy + 4, (30, 34, 40))

        # 胸口核心
        core = RUST_LIGHT if f != 1 else (255, 216, 140)
        ellipse_fill(img, cx, cy - 5, 8, 8, RUST, seed=seeds[f] + 5, shade=RUST_DARK, highlight=core)
        ellipse_fill(img, cx, cy - 5, 4, 4, core, seed=seeds[f] + 6)
        ellipse_fill(img, cx, cy - 6, 2, 2, (255, 255, 240), seed=seeds[f] + 7)
        # 核心外圈
        for i in range(24):
            import math
            ang = i * math.pi / 12
            put(img, int(cx + math.cos(ang) * 9.5), int(cy - 5 + math.sin(ang) * 9.5), METAL_DARK)

        # 背上的废料堆（在躯干上方露出来的碎块）
        for i in range(7):
            bx = cx - 18 + rng.randint(0, 36)
            by = cy - 16 + rng.randint(-6, 2)
            rect_fill(img, bx, by, bx + rng.randint(3, 8), by + rng.randint(2, 6),
                      METAL if i % 2 else METAL_DARK, shade=METAL_DARK, highlight=METAL_LIGHT, seed=seeds[f] + 30 + i)

        # ---------------- 腿 / 液压（y 72~83）
        for lx in (cx - 13, cx + 13):
            rect_fill(img, lx - 5, cy + 12, lx + 5, cy + 22,
                      METAL_DARK, shade=(32, 36, 42), highlight=METAL, seed=seeds[f] + lx)

            for k in range(4):
                put(img, lx, cy + 15 + k * 2, RUST_DARK)

            rect_fill(img, lx - 7, cy + 22, lx + 7, cy + 25, METAL, shade=METAL_DARK, highlight=METAL_LIGHT, seed=seeds[f] + lx + 1)

        outline_dark(img, 0, oy, w, h, (18, 20, 24))

    return img


# ================================================================ 物品 · 锈蚀齿轮
def rusted_gear():
    img = Image.new("RGBA", (20, 20), (0, 0, 0, 0))
    import math
    rng = rng_of(14101)

    for i in range(8):
        ang = i * math.pi / 4
        tx = 10 + math.cos(ang) * 7.2
        ty = 10 + math.sin(ang) * 7.2
        rect_fill(img, int(tx) - 1, int(ty) - 1, int(tx) + 1, int(ty) + 1,
                  METAL_LIGHT, shade=METAL_DARK, highlight=METAL_HI, seed=14101 + i)

    ellipse_fill(img, 10, 10, 6, 6, METAL, seed=14101, shade=METAL_DARK, highlight=METAL_LIGHT)
    ellipse_fill(img, 10, 10, 2.4, 2.4, (0, 0, 0), seed=14102)
    # 掏空轴心（透明）
    for y in range(20):
        for x in range(20):
            if (x - 10) ** 2 + (y - 10) ** 2 <= 5.5:
                img.putpixel((x, y), (0, 0, 0, 0))
    # 锈斑
    for _ in range(24):
        x = rng.randint(4, 15)
        y = rng.randint(4, 15)
        if img.getpixel((x, y))[3] != 0:
            img.putpixel((x, y), RUST_DARK + (255,))
    for _ in range(10):
        x = rng.randint(4, 15)
        y = rng.randint(4, 15)
        if img.getpixel((x, y))[3] != 0:
            img.putpixel((x, y), RUST + (255,))
    outline_dark(img, 0, 0, 20, 20, (26, 24, 22))
    return img


# ================================================================ 物品 · 废料收割者之刃
def scrap_reaper_blade():
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    import math
    rng = rng_of(15201)

    # 长柄（左下 → 右上），加粗到 2px
    for k in range(22):
        x = 4 + k
        y = 28 - k
        put(img, x, y, ASH_DARK)
        put(img, x, y - 1, ASH)
        put(img, x + 1, y, ASH_DARK)

    # 刃：从柄顶向右上的一把大镰刃（一条粗弧线 + 内刃亮边）
    for t in range(0, 140):
        u = t / 139.0
        bx = 18 + u * 13
        by = 12 - math.sin(u * math.pi * 0.8) * 10
        thick = 4.6 * (1.0 - u * 0.5)

        for s in range(int(-thick), int(thick) + 1):
            c = METAL

            if s < -thick * 0.45:
                c = METAL_HI
            elif s > thick * 0.45:
                c = METAL_DARK

            put(img, int(bx), int(by) + s, c)

    # 刃上的锈橙灼痕
    for _ in range(34):
        u = rng.random()
        bx = int(18 + u * 13)
        by = int(12 - math.sin(u * math.pi * 0.8) * 10 + rng.randint(-3, 3))
        put(img, bx, by, RUST if rng.random() < 0.6 else RUST_LIGHT)

    # 柄上的缠布
    for k in range(0, 22, 3):
        put(img, 4 + k, 28 - k, RUST_DARK)
        put(img, 5 + k, 27 - k, RUST_DARK)
        put(img, 6 + k, 28 - k, RUST_DARK)

    # 护手
    rect_fill(img, 15, 11, 21, 15, METAL_DARK, shade=(30, 34, 40), highlight=METAL, seed=15202)
    put(img, 18, 10, RUST_LIGHT)
    outline_dark(img, 0, 0, 32, 32, (22, 22, 24))
    return img


# ================================================================ 弹幕
def spitter_glob():
    img = Image.new("RGBA", (14, 14), (0, 0, 0, 0))
    rng = rng_of(16301)

    ellipse_fill(img, 7, 7, 5.5, 5.5, POLLUTE, seed=16301, shade=POLLUTE_DARK, highlight=POLLUTE_LIGHT)

    for _ in range(14):
        put(img, rng.randint(3, 10), rng.randint(3, 10), (30, 46, 26))

    put(img, 5, 5, POLLUTE_LIGHT)
    put(img, 9, 9, POLLUTE_DARK)
    outline_dark(img, 0, 0, 14, 14, (18, 24, 16))
    return img


def reaper_scrap_shot():
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    rng = rng_of(17401)

    # 不规则的废料块
    for y in range(16):
        for x in range(16):
            dx = (x - 7.5) / 6.5
            dy = (y - 7.5) / 6.5

            if dx * dx + dy * dy <= 1.0:
                c = METAL

                if rng.random() < 0.28:
                    c = METAL_DARK
                elif rng.random() < 0.22:
                    c = METAL_LIGHT

                put(img, x, y, c)

    for _ in range(6):
        put(img, rng.randint(3, 12), rng.randint(3, 12), RUST)
    ellipse_fill(img, 7, 7, 2.4, 2.4, RUST_DARK, seed=17402)
    outline_dark(img, 0, 0, 16, 16, (22, 20, 18))
    return img


def reaper_shockwave():
    img = Image.new("RGBA", (28, 28), (0, 0, 0, 0))
    import math

    for y in range(28):
        for x in range(28):
            d = math.hypot(x - 13.5, y - 13.5)

            if 8.0 <= d <= 13.0:
                a = int(255 * (1.0 - abs(d - 10.5) / 2.5))
                a = max(20, min(220, a))
                put(img, x, y, RUST_LIGHT, a)
            elif d < 8.0:
                a = int(120 * (1.0 - d / 8.0))
                put(img, x, y, RUST, max(0, a))

    return img


def scrap_shield():
    """增益图标 32x32：一块拼起来的废铁盾。"""
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    rng = rng_of(18501)
    cx = 16

    # 盾面（上宽下尖）
    for y in range(4, 29):
        t = (y - 4) / 24.0
        half = int(11 * (1.0 - t * 0.75))
        for x in range(cx - half, cx + half + 1):
            c = METAL

            if x < cx - half + 2:
                c = METAL_LIGHT
            elif x > cx + half - 2:
                c = METAL_DARK

            put(img, x, y, c)

    # 补丁板
    for i in range(4):
        bx = cx - 9 + rng.randint(0, 12)
        by = 7 + rng.randint(0, 14)
        rect_fill(img, bx, by, bx + rng.randint(3, 6), by + rng.randint(2, 5),
                  METAL_DARK if i % 2 else METAL_LIGHT, shade=METAL_DARK, highlight=METAL_HI, seed=18501 + i)

    # 中脊 + 锈斑
    for y in range(5, 28):
        put(img, cx, y, METAL_HI)

    for _ in range(20):
        x = cx + rng.randint(-9, 9)
        y = rng.randint(6, 26)

        if img.getpixel((x, y))[3] != 0:
            img.putpixel((x, y), RUST_DARK + (255,))

    outline_dark(img, 0, 0, 32, 32, (20, 20, 22))
    return img


def gear_swarm_hive():
    """齿轮群母体：32x32 一帧、4 帧竖直。

    与「齿轮群」本体刻意区分 —— 本体是一枚旋转的齿轮，母体是一座**六角形的废料巢**：
    外框 + 内部三颗待发射的齿轮 + 地面支脚，静止不动（所以帧间只做"核心明暗呼吸"）。"""
    import math

    w, h, frames = 32, 32, 4
    img = canvas(w, h, frames)
    rng = rng_of(10801)
    cx, cy = 16, 17

    for f in range(frames):
        oy = f * h
        bright = f in (1, 3)

        # 六角外框
        for i in range(6):
            ang = i * math.pi / 3 + math.pi / 6
            x0 = cx + math.cos(ang) * 12
            y0 = cy + math.sin(ang) * 12
            x1 = cx + math.cos(ang + math.pi / 3) * 12
            y1 = cy + math.sin(ang + math.pi / 3) * 12
            steps = 10

            for k in range(steps + 1):
                px = int(x0 + (x1 - x0) * k / steps)
                py = oy + int(y0 + (y1 - y0) * k / steps)

                for t in range(2):
                    put(img, px, py + t, METAL_LIGHT if t == 0 else METAL_DARK)
                    put(img, px + 1, py + t, METAL)

        # 内部底板
        ellipse_fill(img, cx, oy + cy, 10, 10, METAL_DARK, seed=10801 + f, shade=(28, 32, 38), highlight=METAL)

        # 三颗待发的齿轮（核心明暗随帧呼吸）
        for i in range(3):
            ang = i * 2 * math.pi / 3 + 0.5
            gx = int(cx + math.cos(ang) * 5.4)
            gy = oy + int(cy + math.sin(ang) * 5.4)
            core = RUST_LIGHT if bright else RUST

            ellipse_fill(img, gx, gy, 3.2, 3.2, METAL, seed=10802 + i, shade=METAL_DARK, highlight=METAL_HI)
            put(img, gx, gy, core)
            put(img, gx - 1, gy, RUST_DARK)

        # 中央核心
        ellipse_fill(img, cx, oy + cy, 3.0, 3.0, RUST, seed=10810 + f, shade=RUST_DARK, highlight=RUST_LIGHT)
        put(img, cx, oy + cy, (255, 226, 180) if bright else (240, 180, 110))

        # 支脚
        for lx in (cx - 11, cx + 11):
            for k in range(3):
                put(img, lx, oy + cy + 9 + k, METAL_DARK)
            put(img, lx - 1, oy + cy + 11, METAL)

        # 锈斑
        for _ in range(14):
            x = cx + rng.randint(-11, 11)
            y = oy + cy + rng.randint(-11, 11)

            if img.getpixel((x, y))[3] != 0:
                put(img, x, y, RUST_DARK)

        outline_dark(img, 0, oy, w, h, (22, 24, 28))

    return img


def reaper_friendly_scrap():
    """玩家版废料团：16x16，比敌对版更亮一点（带橙色灼痕），好区分敌我。"""
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    rng = rng_of(17501)

    for y in range(16):
        for x in range(16):
            dx = (x - 7.5) / 6.6
            dy = (y - 7.5) / 6.6

            if dx * dx + dy * dy <= 1.0:
                c = METAL_LIGHT

                if rng.random() < 0.26:
                    c = METAL
                elif rng.random() < 0.24:
                    c = METAL_HI

                put(img, x, y, c)

    for _ in range(9):
        put(img, rng.randint(3, 12), rng.randint(3, 12), RUST_LIGHT)

    for _ in range(5):
        put(img, rng.randint(3, 12), rng.randint(3, 12), RUST_DARK)

    ellipse_fill(img, 7, 7, 2.6, 2.6, RUST, seed=17502, shade=RUST_DARK, highlight=(255, 236, 190))
    outline_dark(img, 0, 0, 16, 16, (26, 22, 18))
    return img


# ================================================================ 预览图
def preview(images):
    cols = 4
    rows = (len(images) + cols - 1) // cols
    cell_w = max(i.width for i in images) + 12
    cell_h = max(i.height for i in images) + 12
    sheet = Image.new("RGBA", (cell_w * cols, cell_h * rows), (26, 28, 32, 255))

    for idx, im in enumerate(images):
        cx = (idx % cols) * cell_w + (cell_w - im.width) // 2
        cy = (idx // cols) * cell_h + (cell_h - im.height) // 2
        sheet.alpha_composite(im, (cx, cy))

    return sheet


def main():
    print("gen_wildlife_art: 生成小怪扩充（批次 W7）贴图")

    outputs = []

    def emit(img, path):
        save(img, path)
        outputs.append(img)

    emit(scrap_leaper(), os.path.join(NPC_DIR, "ScrapLeaper.png"))
    emit(rust_charger(), os.path.join(NPC_DIR, "RustCharger.png"))
    emit(gale_wisp(), os.path.join(NPC_DIR, "GaleWisp.png"))
    emit(spitter_fly(), os.path.join(NPC_DIR, "SpitterFly.png"))
    emit(cave_crawler(), os.path.join(NPC_DIR, "CaveCrawler.png"))
    emit(ash_tick_bat(), os.path.join(NPC_DIR, "AshTickBat.png"))
    emit(pollution_slime(), os.path.join(NPC_DIR, "PollutionSlime.png"))
    emit(gear_swarm(), os.path.join(NPC_DIR, "GearSwarm.png"))
    emit(gear_swarm_hive(), os.path.join(NPC_DIR, "GearSwarmHive.png"))
    emit(cinder_mender(), os.path.join(NPC_DIR, "CinderMender.png"))
    emit(scrap_reaper(), os.path.join(NPC_DIR, "ScrapReaper.png"))
    emit(rusted_gear(), os.path.join(ITEM_MAT_DIR, "RustedGear.png"))
    emit(scrap_reaper_blade(), os.path.join(ITEM_WEP_DIR, "ScrapReaperBlade.png"))
    emit(spitter_glob(), os.path.join(PROJ_DIR, "SpitterGlob.png"))
    emit(reaper_scrap_shot(), os.path.join(PROJ_DIR, "ReaperScrapShot.png"))
    emit(reaper_friendly_scrap(), os.path.join(PROJ_DIR, "ReaperFriendlyScrap.png"))
    emit(reaper_shockwave(), os.path.join(PROJ_DIR, "ReaperShockwave.png"))
    emit(scrap_shield(), os.path.join(BUFF_DIR, "ScrapShield.png"))

    os.makedirs(INBOX, exist_ok=True)
    sheet_path = os.path.join(INBOX, "preview_wildlife.png")
    preview(outputs).save(sheet_path)
    print("  预览：%s" % os.path.relpath(sheet_path, ROOT))
    print("gen_wildlife_art: 完成，共 %d 张" % len(outputs))


if __name__ == "__main__":
    main()
