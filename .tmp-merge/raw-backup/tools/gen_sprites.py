"""生成《废土魂穿》所需的占位贴图（placeholder art）。

帧布局依据 tModLoader 官方 ExampleMod 实测：
  - 城镇 NPC 精灵表 = 40x56 一帧、25 帧纵向排列 → 40x1400
  - 城镇 NPC 头部贴图 = 16x16
美术资源后续需要替换，这里只保证能编译、能加载、帧不错位。
"""
import math
import os
import random
import sys

from PIL import Image, ImageDraw

ROOT = r"E:\开发\WastelandSoul"

# 配色（半废土科技：锈蚀金属 + 污染 + 旧时代军工）
RUST = (122, 74, 48, 255)
RUST_DARK = (84, 50, 33, 255)
STEEL = (128, 132, 138, 255)
STEEL_DARK = (74, 78, 84, 255)
STEEL_LIGHT = (176, 180, 186, 255)
RED_LIGHT = (226, 62, 46, 255)
ROBE = (122, 54, 44, 255)
ROBE_TRIM = (201, 162, 39, 255)
SKIN = (242, 210, 176, 255)
HAIR = (232, 193, 90, 255)
EYE = (58, 111, 216, 255)
SLUDGE = (86, 104, 48, 220)
SLUDGE_DARK = (54, 66, 30, 235)
PURPLE = (124, 74, 138, 255)


def save(img, rel):
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print("wrote %-58s %s" % (rel, img.size))


def icon():
    """模组图标 80x80。"""
    img = Image.new("RGBA", (80, 80), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((3, 3, 76, 76), fill=(74, 70, 62, 255), outline=(38, 36, 32, 255), width=2)
    d.ellipse((22, 22, 57, 57), fill=STEEL, outline=STEEL_DARK, width=2)
    d.ellipse((32, 32, 47, 47), fill=RED_LIGHT)
    save(img, "icon.png")


# ============================================================ 智械人（城镇 NPC）

# 智械人：3~4 阶明暗的像素风配色（参照真实城镇 NPC 贴图的做法：人物约 20x32 居中于 40x56 帧）
C_HAIR_D, C_HAIR, C_HAIR_L = (150, 112, 34, 255), (217, 174, 74, 255), (243, 219, 142, 255)
C_SKIN_D, C_SKIN, C_SKIN_L = (198, 154, 114, 255), (232, 195, 154, 255), (248, 223, 192, 255)
C_ROBE_D, C_ROBE, C_ROBE_L = (104, 45, 37, 255), (142, 64, 51, 255), (170, 88, 72, 255)
C_GOLD_D, C_GOLD, C_GOLD_L = (137, 104, 28, 255), (201, 162, 39, 255), (233, 206, 110, 255)
C_EYE, C_OUT, C_BOOT = (58, 111, 216, 255), (44, 31, 25, 255), (58, 44, 37, 255)


def companion_frame(d, f):
    """画一帧智械人（40x56），**半侧身（3/4 视角）**，与原版城镇 NPC 一致。

    3/4 视角的做法：整体不对称——近侧（右侧）手臂更大更亮、远侧（左侧）手臂收窄压暗；
    脸稍向近侧偏（双眼右移、近侧留出鼻尖）、近耳长远耳短；长袍下摆与腰线也带一点斜度。
    帧序沿用原版城镇 NPC 约定：0-15 = 4 朝向 x 4 帧行走，16-20 = 站立，21-24 = 攻击。
    """
    if f < 16:
        facing = f // 4          # 0 正面(3/4) / 1 侧面 / 2 背面 / 3 正面(3/4，另一相位)
        step = f % 4
    elif f < 21:
        facing, step = 0, (f - 16) % 4
    else:
        facing, step = 0, 0

    attacking = f >= 21
    back = facing == 2
    side = facing == 1
    bob = (0, -1, 0, 1)[step]
    swing = (1, 0, -1, 0)[step]

    head_y = 17 + bob
    eye_y = head_y + 7
    shoulder = head_y + 13
    hem = 52 + (1 if step % 2 else 0)

    # ---- 靴子：近侧在前（3/4 视角） ----
    near_x = 22 + (2 if step % 2 else 0)
    far_x = 14 - (2 if step % 2 else 0)
    d.rectangle((far_x, 49, far_x + 3, 53), fill=(44, 34, 28, 255), outline=C_OUT)
    d.rectangle((near_x, 50, near_x + 4, 53), fill=C_BOOT, outline=C_OUT)

    # ---- 长袍：下宽上窄，左侧（远侧）暗一档，做出体积感 ----
    d.polygon([(15, shoulder), (26, shoulder), (29, hem), (12, hem)], fill=C_ROBE)
    d.polygon([(15, shoulder), (19, shoulder), (17, hem), (12, hem)], fill=C_ROBE_D)
    d.line([(15, shoulder), (12, hem)], fill=C_GOLD_D)
    d.line([(26, shoulder), (29, hem)], fill=C_GOLD)
    d.line([(12, hem), (29, hem)], fill=C_GOLD)
    if not back:
        # 前襟偏近侧，带一点斜度
        d.line([(22, shoulder + 1), (23, hem - 1)], fill=C_GOLD_D)
        d.line([(21, shoulder + 2), (22, hem - 2)], fill=C_ROBE_L)
    d.rectangle((15, shoulder + 12, 27, shoulder + 14), fill=C_GOLD_D, outline=C_GOLD)

    # ---- 手臂：近侧粗亮，远侧细暗 ----
    arm_y = shoulder + 2 + (swing if not back else -swing)
    if attacking:
        d.rectangle((25, shoulder - 6, 30, shoulder + 6), fill=C_ROBE, outline=C_OUT)
        d.rectangle((25, shoulder + 4, 30, shoulder + 7), fill=C_GOLD)
        d.ellipse((25, shoulder - 12, 33, shoulder - 4), fill=(226, 62, 46, 255))
        d.ellipse((27, shoulder - 10, 31, shoulder - 6), fill=(255, 214, 186, 255))
    else:
        d.rectangle((11, arm_y + 1, 14, arm_y + 12), fill=C_ROBE_D, outline=C_OUT)
        d.rectangle((24, arm_y - swing, 28, arm_y + 12 - swing), fill=C_ROBE, outline=C_OUT)
        d.rectangle((11, arm_y + 11, 14, arm_y + 13), fill=C_GOLD_D)
        d.rectangle((24, arm_y + 11 - swing, 28, arm_y + 13 - swing), fill=C_GOLD)

    # ---- 精灵长耳：近耳长、远耳短（3/4 的关键提示） ----
    d.polygon([(13, head_y + 7), (7, head_y - 3), (15, head_y + 3)], fill=C_SKIN_D, outline=C_OUT)
    d.polygon([(26, head_y + 7), (35, head_y - 6), (24, head_y + 2)], fill=C_SKIN, outline=C_OUT)

    # ---- 头：向近侧偏 1 像素 ----
    d.ellipse((15, head_y, 27, head_y + 13), fill=C_SKIN, outline=C_OUT)
    d.point((27, head_y + 8), fill=C_SKIN_D)   # 近侧鼻尖

    # ---- 金发：远侧厚、近侧薄，露出近侧脸颊 ----
    d.ellipse((13, head_y - 3, 26, head_y + 7), fill=C_HAIR, outline=C_OUT)
    d.rectangle((12, head_y + 2, 15, head_y + 14), fill=C_HAIR)
    d.rectangle((24, head_y + 2, 26, head_y + 9), fill=C_HAIR)
    d.line([(16, head_y - 1), (23, head_y - 1)], fill=C_HAIR_L)
    d.line([(14, head_y + 1), (16, head_y + 1)], fill=C_HAIR_D)

    # ---- 五官（背面不画脸） ----
    if not back:
        if side:
            d.rectangle((23, eye_y, 24, eye_y + 1), fill=C_EYE)
            d.point((23, eye_y), fill=(255, 255, 255, 255))
        else:
            d.rectangle((19, eye_y, 20, eye_y + 1), fill=C_EYE)
            d.rectangle((23, eye_y, 24, eye_y + 1), fill=C_EYE)
            d.point((19, eye_y), fill=(255, 255, 255, 255))
            d.point((23, eye_y), fill=(255, 255, 255, 255))
        d.point((21, head_y + 10), fill=(196, 120, 104, 255))


def companion_body():
    img = Image.new("RGBA", (40, 1400), (0, 0, 0, 0))
    for f in range(25):
        frame = Image.new("RGBA", (40, 56), (0, 0, 0, 0))
        companion_frame(ImageDraw.Draw(frame), f)
        img.paste(frame, (0, f * 56))
    save(img, r"Content\NPCs\Town\MechanicalCompanion.png")


def companion_head():
    """16x16 的城镇 NPC 头像（与本体同一套配色：金发、长耳、蓝瞳）。"""
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # 长耳
    d.polygon([(5, 7), (0, 1), (7, 4)], fill=C_SKIN, outline=C_OUT)
    d.polygon([(11, 7), (16, 1), (9, 4)], fill=C_SKIN, outline=C_OUT)
    # 脸
    d.ellipse((3, 3, 13, 15), fill=C_SKIN, outline=C_OUT)
    # 金发
    d.ellipse((2, 0, 14, 9), fill=C_HAIR, outline=C_OUT)
    d.rectangle((2, 5, 4, 12), fill=C_HAIR)
    d.rectangle((12, 5, 14, 12), fill=C_HAIR)
    d.line([(5, 2), (11, 2)], fill=C_HAIR_L)
    # 蓝瞳
    d.rectangle((5, 9, 6, 10), fill=C_EYE)
    d.rectangle((10, 9, 11, 10), fill=C_EYE)
    d.point((5, 9), fill=(255, 255, 255, 255))
    d.point((10, 9), fill=(255, 255, 255, 255))
    save(img, r"Content\NPCs\Town\MechanicalCompanion_Head.png")


# ============================================================ 清道夫 Boss

def draw_rust_patches(d, box, rng, count, base, patch):
    x0, y0, x1, y1 = box
    d.rounded_rectangle(box, radius=14, fill=base)
    for _ in range(count):
        px = rng.randint(x0 + 4, x1 - 8)
        py = rng.randint(y0 + 4, y1 - 8)
        w = rng.randint(5, 14)
        h = rng.randint(3, 9)
        d.ellipse((px, py, px + w, py + h), fill=patch)


def scavenger_frame(f, total=6):
    """画一帧清道夫（110x110）。"""
    rng = random.Random(1000 + f)
    img = Image.new("RGBA", (110, 110), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    wobble = (-1, 0, 1, 1, 0, -1)[f % total]

    # 主推进器（底部破损，喷污染颗粒）
    d.polygon([(34, 84), (76, 84), (68, 102), (42, 102)], fill=STEEL_DARK)
    for i in range(6):
        px = 42 + i * 5 + wobble
        py = 100 + rng.randint(0, 6)
        d.ellipse((px, py, px + 3, py + 3), fill=(70, 78, 52, 200))

    # 焊接炮管（左侧）
    d.rectangle((4, 52 + wobble, 26, 62 + wobble), fill=STEEL_DARK)
    d.rectangle((2, 54 + wobble, 8, 60 + wobble), fill=RUST_DARK)

    # 带电机械臂（右侧）
    d.rectangle((84, 48 - wobble, 106, 58 - wobble), fill=STEEL_DARK)
    d.polygon([(104, 44 - wobble), (110, 52 - wobble), (104, 62 - wobble), (98, 52 - wobble)], fill=STEEL)
    d.ellipse((100, 50 - wobble, 106, 56 - wobble), fill=(120, 210, 255, 220))

    # 圆柱/球形主体 + 锈蚀修补痕迹
    draw_rust_patches(d, (20, 22, 90, 90), rng, 10, STEEL, RUST)
    d.rounded_rectangle((20, 22, 90, 90), radius=14, outline=STEEL_DARK, width=2)
    for _ in range(7):
        px = rng.randint(22, 80)
        py = rng.randint(24, 80)
        d.line((px, py, px + rng.randint(4, 12), py + rng.randint(-4, 4)), fill=STEEL_LIGHT, width=1)

    # 不完整的传感器阵列 + 不稳定红光
    sensor_bright = (255, 60, 40, 255) if f % 2 == 0 else (150, 30, 24, 230)
    for i in range(3):
        sx = 30 + i * 20
        d.ellipse((sx, 10 + i % 2, sx + 10, 20 + i % 2), fill=STEEL_DARK)
    d.ellipse((46, 4, 64, 20), fill=sensor_bright)
    d.ellipse((51, 8, 59, 16), fill=(255, 190, 150, 255))
    return img


def scavenger_body():
    img = Image.new("RGBA", (110, 660), (0, 0, 0, 0))
    for f in range(6):
        img.paste(scavenger_frame(f), (0, f * 110))
    save(img, r"Content\NPCs\Bosses\Scavenger\Scavenger.png")


def scavenger_head():
    img = Image.new("RGBA", (34, 34), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((4, 8, 30, 30), radius=6, fill=STEEL, outline=STEEL_DARK, width=2)
    d.ellipse((11, 1, 23, 13), fill=RED_LIGHT)
    d.ellipse((15, 5, 19, 9), fill=(255, 200, 170, 255))
    d.ellipse((6, 14, 12, 20), fill=RUST)
    save(img, r"Content\NPCs\Bosses\Scavenger\Scavenger_Head_Boss.png")


# ============================================================ 维修无人机

def drone_frame(f):
    img = Image.new("RGBA", (30, 26), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    lift = (0, -1, 0, 1)[f % 4]
    d.rectangle((8, 8 + lift, 22, 20 + lift), fill=STEEL, outline=STEEL_DARK)
    d.rectangle((2, 5 + lift, 9, 8 + lift), fill=STEEL_DARK)
    d.rectangle((21, 5 + lift, 28, 8 + lift), fill=STEEL_DARK)
    # 维修指示灯
    light = (90, 240, 130, 255) if f % 2 == 0 else (40, 140, 80, 255)
    d.ellipse((13, 11 + lift, 17, 15 + lift), fill=light)
    # 底部喷口
    d.rectangle((12, 20 + lift, 18, 23 + lift), fill=RUST_DARK)
    return img


def drone_body():
    img = Image.new("RGBA", (30, 104), (0, 0, 0, 0))
    for f in range(4):
        img.paste(drone_frame(f), (0, f * 26))
    save(img, r"Content\NPCs\Bosses\Scavenger\ScavengerRepairDrone.png")


# ============================================================ 弹幕

def pollution_zone():
    img = Image.new("RGBA", (96, 28), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((2, 4, 94, 27), fill=SLUDGE)
    d.ellipse((10, 10, 60, 26), fill=SLUDGE_DARK)
    rng = random.Random(7)
    for _ in range(26):
        px = rng.randint(4, 88)
        py = rng.randint(6, 24)
        d.ellipse((px, py, px + 3, py + 3), fill=(140, 190, 90, 200) if rng.random() < 0.5 else PURPLE)
    save(img, r"Content\Projectiles\PollutionZone.png")


def scavenger_bullet():
    img = Image.new("RGBA", (14, 14), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((1, 3, 13, 11), fill=STEEL_DARK)
    d.ellipse((3, 4, 11, 10), fill=(255, 120, 60, 255))
    d.ellipse((5, 5, 9, 9), fill=(255, 220, 170, 255))
    d.rectangle((0, 5, 4, 9), fill=RUST_DARK)
    save(img, r"Content\Projectiles\ScavengerBullet.png")


def scavenger_arm():
    """机械臂：以左端为轴，指向右侧（绘制时按角度旋转）。"""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle((4, 28, 40, 38), fill=STEEL, outline=STEEL_DARK)
    d.rectangle((4, 28, 14, 38), fill=RUST_DARK)
    d.rectangle((38, 24, 50, 42), fill=STEEL_DARK)
    # 前端爪
    d.polygon([(50, 22), (62, 30), (50, 33)], fill=STEEL_LIGHT)
    d.polygon([(50, 44), (62, 36), (50, 33)], fill=STEEL_LIGHT)
    # 电流
    d.ellipse((44, 30, 50, 36), fill=(130, 215, 255, 230))
    save(img, r"Content\Projectiles\ScavengerArmSweep.png")


# ============================================================ 物品与减益

def steel_chunk():
    img = Image.new("RGBA", (18, 18), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.polygon([(3, 10), (6, 3), (13, 2), (16, 9), (11, 16), (5, 15)], fill=STEEL, outline=STEEL_DARK)
    d.polygon([(6, 5), (10, 4), (9, 9), (5, 9)], fill=STEEL_LIGHT)
    d.polygon([(11, 12), (15, 10), (14, 14)], fill=RUST)
    save(img, r"Content\Items\Materials\SalvagedSteelChunk.png")


def steel_bar():
    img = Image.new("RGBA", (22, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.polygon([(3, 12), (8, 5), (20, 5), (15, 12)], fill=STEEL_LIGHT)
    d.rectangle((3, 12, 15, 16), fill=STEEL)
    d.polygon([(15, 12), (20, 5), (20, 9), (15, 16)], fill=STEEL_DARK)
    d.line((6, 7, 18, 7), fill=(255, 255, 255, 120))
    save(img, r"Content\Items\Materials\SalvagedSteelBar.png")


def scavenger_fragment():
    img = Image.new("RGBA", (20, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.polygon([(2, 6), (9, 2), (18, 7), (16, 17), (5, 18)], fill=(46, 62, 48, 255), outline=(28, 38, 30, 255))
    for i in range(3):
        d.line((4, 7 + i * 4, 15, 8 + i * 4), fill=(196, 168, 72, 255))
    d.ellipse((8, 9, 12, 13), fill=RED_LIGHT)
    save(img, r"Content\Items\Materials\ScavengerFragment.png")


def signal_sensor():
    """清道夫信号传感器：底座 + 天线 + 信号波。"""
    img = Image.new("RGBA", (26, 26), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # 底座
    d.rounded_rectangle((3, 16, 23, 24), radius=3, fill=STEEL_DARK, outline=(44, 46, 50, 255))
    # 天线杆
    d.rectangle((12, 6, 14, 17), fill=STEEL)
    d.ellipse((11, 3, 15, 7), fill=RED_LIGHT)
    # 信号波
    d.arc((1, 0, 25, 18), start=205, end=335, fill=(226, 120, 90, 220), width=2)
    d.arc((5, 3, 21, 16), start=210, end=330, fill=(255, 170, 120, 240), width=2)
    # 指示灯
    d.ellipse((10, 18, 16, 24), fill=(90, 240, 130, 255))
    save(img, r"Content\Items\Summons\ScavengerSignalSensor.png")


def companion_core():
    """智械核心：开局在背包里的那个核心。"""
    img = Image.new("RGBA", (22, 22), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((2, 2, 20, 20), fill=STEEL_DARK, outline=(44, 46, 50, 255), width=2)
    d.ellipse((7, 7, 15, 15), fill=RED_LIGHT)
    d.ellipse((9, 9, 13, 13), fill=(255, 220, 190, 255))
    d.line((11, 1, 11, 6), fill=STEEL_LIGHT)
    d.line((11, 16, 11, 21), fill=STEEL_LIGHT)
    d.line((1, 11, 6, 11), fill=STEEL_LIGHT)
    d.line((16, 11, 21, 11), fill=STEEL_LIGHT)
    save(img, r"Content\Items\Story\CompanionCore.png")


def elven_frame():
    """精灵族躯体：3x3 图块 → 54x48（三行各 16px）。"""
    img = Image.new("RGBA", (54, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # 底座
    d.rounded_rectangle((4, 34, 50, 47), radius=3, fill=(178, 196, 210, 255), outline=(120, 140, 156, 255))
    # 舱体（精灵风格：白金）
    d.rounded_rectangle((10, 6, 44, 36), radius=8, fill=(206, 220, 232, 255), outline=(140, 160, 176, 255), width=2)
    # 内部空槽
    d.rounded_rectangle((16, 12, 38, 32), radius=6, fill=(64, 78, 92, 255))
    # 长耳状装饰
    d.polygon([(6, 14), (1, 2), (12, 10)], fill=(196, 214, 228, 255))
    d.polygon([(48, 14), (53, 2), (42, 10)], fill=(196, 214, 228, 255))
    # 等待中的指示灯
    d.ellipse((24, 20, 30, 26), fill=(90, 200, 240, 255))
    save(img, r"Content\Tiles\ElvenFrame.png")


def pollution_homing():
    """污染团：追踪玩家的污染弹幕。"""
    img = Image.new("RGBA", (20, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((1, 1, 19, 19), fill=SLUDGE)
    d.ellipse((4, 3, 13, 12), fill=(140, 190, 90, 230))
    d.ellipse((6, 10, 16, 18), fill=SLUDGE_DARK)
    d.ellipse((7, 7, 12, 12), fill=PURPLE)
    save(img, r"Content\Projectiles\PollutionHoming.png")


# ============================================================ 精钢套装（五职业）

# 职业 → (中文名, 配色)。五项套装共用精钢底色，只靠配色区分职业。
ARMOR_CLASSES = {
    "Warrior": ("战士", (196, 138, 92, 255)),
    "Mage": ("法师", (110, 150, 220, 255)),
    "Ranger": ("射手", (128, 190, 120, 255)),
    "Summoner": ("召唤师", (190, 140, 210, 255)),
    "Rogue": ("盗贼", (210, 190, 110, 255)),
}

ARMOR_FRAME_W = 40
ARMOR_FRAME_H = 56
ARMOR_FRAMES = 20   # 与原版玩家贴图一致：40 宽 × 20 帧 × 56 高


def armor_icon(d, kind, tint):
    """22x22 物品图标。"""
    if kind == "Helm":
        d.rounded_rectangle((4, 5, 18, 17), radius=4, fill=STEEL, outline=STEEL_DARK)
        d.rectangle((6, 10, 16, 14), fill=tint)
        d.rectangle((9, 11, 13, 13), fill=(40, 44, 50, 255))
    elif kind == "Plate":
        d.rounded_rectangle((3, 4, 19, 18), radius=3, fill=STEEL, outline=STEEL_DARK)
        d.rectangle((3, 4, 19, 8), fill=tint)
        d.rectangle((9, 9, 13, 17), fill=tint)
    else:
        d.rounded_rectangle((4, 4, 18, 18), radius=3, fill=STEEL, outline=STEEL_DARK)
        d.rectangle((6, 6, 16, 11), fill=tint)
        d.rectangle((6, 13, 16, 17), fill=tint)


def armor_equip(suffix, tint):
    """装备贴图：40 宽 × 20 帧（每帧 40x56）纵向排列。"""
    img = Image.new("RGBA", (ARMOR_FRAME_W, ARMOR_FRAME_H * ARMOR_FRAMES), (0, 0, 0, 0))

    for f in range(ARMOR_FRAMES):
        frame = Image.new("RGBA", (ARMOR_FRAME_W, ARMOR_FRAME_H), (0, 0, 0, 0))
        d = ImageDraw.Draw(frame)
        bob = (0, 0, 1, 1, 0, 0, -1, -1)[f % 8]

        if suffix == "Head":
            d.ellipse((11, 6 + bob, 29, 24 + bob), fill=STEEL, outline=STEEL_DARK)
            d.polygon([(11, 10 + bob), (29, 10 + bob), (26, 20 + bob), (14, 20 + bob)], fill=tint)
        elif suffix == "Body":
            d.rounded_rectangle((7, 20 + bob, 33, 44 + bob), radius=4, fill=STEEL, outline=STEEL_DARK)
            d.rectangle((7, 20 + bob, 33, 26 + bob), fill=tint)
            d.rectangle((15, 28 + bob, 25, 40 + bob), fill=tint)
        else:
            d.rounded_rectangle((10, 30 + bob, 30, 54), radius=3, fill=STEEL, outline=STEEL_DARK)
            d.rectangle((13, 33 + bob, 27, 40 + bob), fill=tint)

        img.paste(frame, (0, f * ARMOR_FRAME_H))

    return img


def salvaged_steel_armor():
    for cls, (_name, tint) in ARMOR_CLASSES.items():
        for kind, suffix in (("Helm", "Head"), ("Plate", "Body"), ("Greaves", "Legs")):
            item_name = "SalvagedSteel%s%s" % (cls, kind)

            icon = Image.new("RGBA", (22, 22), (0, 0, 0, 0))
            armor_icon(ImageDraw.Draw(icon), kind, tint)
            save(icon, r"Content\Items\Armor\%s.png" % item_name)

            save(armor_equip(suffix, tint), r"Content\Items\Armor\%s_%s.png" % (item_name, suffix))


def soul_fragments():
    """4 枚灵魂碎片：同一形状、不同色相，代表 4 个 Boss。"""
    hues = [
        ("SoulFragmentScavenger", (206, 72, 52, 255)),
        ("SoulFragmentSecond", (96, 150, 220, 255)),
        ("SoulFragmentThird", (150, 196, 110, 255)),
        ("SoulFragmentFourth", (186, 120, 214, 255)),
    ]
    for name, tint in hues:
        img = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        # 外壳
        d.polygon([(12, 2), (21, 12), (12, 22), (3, 12)], fill=(58, 60, 68, 255), outline=(30, 32, 38, 255))
        # 内部光
        d.polygon([(12, 6), (18, 12), (12, 18), (6, 12)], fill=tint)
        d.polygon([(12, 8), (15, 12), (12, 15), (9, 12)], fill=(255, 240, 220, 255))
        # 裂纹
        d.line((12, 2, 12, 6), fill=(30, 32, 38, 255))
        d.line((12, 18, 12, 22), fill=(30, 32, 38, 255))
        save(img, r"Content\Items\Soul\%s.png" % name)


def boss_bags():
    """Boss 掉落袋（32x32）。目前只有清道夫袋，后续 Boss 照抄换配色。"""
    bags = [
        ("ScavengerBag", (122, 74, 48, 255), (86, 52, 34, 255)),
    ]
    for name, body, dark in bags:
        img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        # 袋体
        d.rounded_rectangle((4, 10, 28, 29), radius=5, fill=body, outline=(40, 30, 24, 255), width=2)
        # 袋口扎带
        d.rectangle((6, 8, 26, 12), fill=dark, outline=(40, 30, 24, 255))
        d.line((16, 4, 16, 9), fill=(60, 46, 36, 255), width=2)
        # 金属扣与锈斑
        d.ellipse((12, 15, 20, 23), fill=STEEL, outline=STEEL_DARK)
        d.ellipse((14, 17, 18, 21), fill=RED_LIGHT)
        d.ellipse((6, 22, 11, 27), fill=RUST)
        save(img, r"Content\Items\Bags\%s.png" % name)


def pollution_buff():
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((4, 4, 28, 28), fill=(58, 74, 36, 255), outline=PURPLE, width=2)
    d.polygon([(16, 6), (10, 18), (16, 26), (22, 18)], fill=(126, 176, 74, 255))
    d.ellipse((13, 13, 19, 19), fill=PURPLE)
    save(img, r"Content\Buffs\Pollution.png")


# ============================================================ 40 把 Boss 武器 + 配套材料
# 生成约定：
#   - **形状按职业**：战士=大刀 / 法师=法典 / 射手=枪 / 召唤师=召唤杖 / 盗贼=投掷刃（手里剑）
#   - **配色按 Boss**：Boss1 锈红 / Boss2 骨白+蓝 / Boss3 橙红+灰 / Boss4 冷白+金
#   - 尺寸 34~48，与 WastelandClassWeapon 基类的 40x40 面板同一量级
# 这些仍然是占位图：只保证能编译、能加载、能一眼分辨职业与 Boss。

BOSS_WEAPON_PALETTES = {
    "Boss1Scavenger": {
        "main": RUST, "dark": RUST_DARK, "light": (176, 112, 74, 255),
        "accent": RED_LIGHT, "grip": (92, 64, 44, 255),
    },
    "Boss2Archivist": {
        "main": (214, 210, 192, 255), "dark": (120, 126, 142, 255), "light": (244, 242, 230, 255),
        "accent": (96, 150, 220, 255), "grip": (86, 66, 52, 255),
    },
    "Boss3AshHeart": {
        "main": (188, 84, 46, 255), "dark": (86, 78, 76, 255), "light": (236, 150, 84, 255),
        "accent": (255, 206, 96, 255), "grip": (72, 58, 52, 255),
    },
    "Boss4Fireplace": {
        "main": (220, 230, 240, 255), "dark": (128, 150, 168, 255), "light": (250, 252, 255, 255),
        "accent": (201, 162, 39, 255), "grip": (74, 82, 96, 255),
    },
}

# (子目录, 类名, 职业) —— 与 Content\Items\Weapons 下的 .cs 一一对应
BOSS_WEAPONS = [
    ("Boss1Scavenger", "ScavengerWarriorWeapon", "Warrior"),
    ("Boss1Scavenger", "ScavengerMageWeapon", "Mage"),
    ("Boss1Scavenger", "ScavengerRangerWeapon", "Ranger"),
    ("Boss1Scavenger", "ScavengerSummonerWeapon", "Summoner"),
    ("Boss1Scavenger", "ScavengerRogueWeapon", "Rogue"),
    ("Boss1Scavenger", "ScavengerWarriorWeaponEX", "Warrior"),
    ("Boss1Scavenger", "ScavengerMageWeaponEX", "Mage"),
    ("Boss1Scavenger", "ScavengerRangerWeaponEX", "Ranger"),
    ("Boss1Scavenger", "ScavengerSummonerWeaponEX", "Summoner"),
    ("Boss1Scavenger", "ScavengerRogueWeaponEX", "Rogue"),

    ("Boss2Archivist", "ArchivistWarriorWeapon", "Warrior"),
    ("Boss2Archivist", "ArchivistMageWeapon", "Mage"),
    ("Boss2Archivist", "ArchivistRangerWeapon", "Ranger"),
    ("Boss2Archivist", "ArchivistSummonerWeapon", "Summoner"),
    ("Boss2Archivist", "ArchivistRogueWeapon", "Rogue"),
    ("Boss2Archivist", "ArchivistWarriorWeaponEX", "Warrior"),
    ("Boss2Archivist", "ArchivistMageWeaponEX", "Mage"),
    ("Boss2Archivist", "ArchivistRangerWeaponEX", "Ranger"),
    ("Boss2Archivist", "ArchivistSummonerWeaponEX", "Summoner"),
    ("Boss2Archivist", "ArchivistRogueWeaponEX", "Rogue"),

    ("Boss3AshHeart", "AshHeartWarriorWeapon", "Warrior"),
    ("Boss3AshHeart", "AshHeartMageWeapon", "Mage"),
    ("Boss3AshHeart", "AshHeartRangerWeapon", "Ranger"),
    ("Boss3AshHeart", "AshHeartSummonerWeapon", "Summoner"),
    ("Boss3AshHeart", "AshHeartRogueWeapon", "Rogue"),
    ("Boss3AshHeart", "AshHeartWarriorWeaponEx", "Warrior"),
    ("Boss3AshHeart", "AshHeartMageWeaponEx", "Mage"),
    ("Boss3AshHeart", "AshHeartRangerWeaponEx", "Ranger"),
    ("Boss3AshHeart", "AshHeartSummonerWeaponEx", "Summoner"),
    ("Boss3AshHeart", "AshHeartRogueWeaponEx", "Rogue"),

    ("Boss4Fireplace", "FireplaceWarriorWeapon", "Warrior"),
    ("Boss4Fireplace", "FireplaceMageWeapon", "Mage"),
    ("Boss4Fireplace", "FireplaceRangerWeapon", "Ranger"),
    ("Boss4Fireplace", "FireplaceSummonerWeapon", "Summoner"),
    ("Boss4Fireplace", "FireplaceRogueWeapon", "Rogue"),
    ("Boss4Fireplace", "FireplaceWarriorWeaponEx", "Warrior"),
    ("Boss4Fireplace", "FireplaceMageWeaponEx", "Mage"),
    ("Boss4Fireplace", "FireplaceRangerWeaponEx", "Ranger"),
    ("Boss4Fireplace", "FireplaceSummonerWeaponEx", "Summoner"),
    ("Boss4Fireplace", "FireplaceRogueWeaponEx", "Rogue"),
]

BOSS_WEAPON_SIZE = {"Warrior": 44, "Mage": 38, "Ranger": 44, "Summoner": 40, "Rogue": 34}


def wpn_sword(d, S, pal, heavy):
    """战士：45° 斜置大刀（描边 + 刀身 + 高光 + 护手 + 握柄 + 配重）。"""
    main, dark, light = pal["main"], pal["dark"], pal["light"]
    grip = pal["grip"]
    gx, gy = int(S * 0.34), int(S * 0.66)
    tx, ty = S - 5, 5
    px, py = gx - 7, gy + 7
    w = 10 if heavy else 8

    d.line((gx, gy, tx, ty), fill=dark, width=w)
    d.line((gx, gy, tx, ty), fill=main, width=w - 3)
    d.line((gx + 1, gy + 1, tx, ty + 2), fill=light, width=2)

    k = 8 if heavy else 6
    d.line((gx - k, gy - k, gx + k, gy + k), fill=dark, width=5)
    d.line((gx - k + 1, gy - k + 1, gx + k - 1, gy + k - 1), fill=light, width=3)

    d.line((px, py, gx, gy), fill=grip, width=4)
    d.ellipse((px - 4, py - 2, px + 3, py + 5), fill=dark)
    d.ellipse((px - 3, py - 1, px + 2, py + 4), fill=pal["accent"])


def wpn_book(d, S, pal):
    """法师（归档者）：法典。"""
    main, dark, light, accent = pal["main"], pal["dark"], pal["light"], pal["accent"]

    d.polygon([(7, 11), (S - 7, 6), (S - 5, S - 7), (9, S - 5)], fill=dark)
    d.polygon([(9, 13), (S - 9, 8), (S - 7, S - 9), (11, S - 7)], fill=main)
    d.rectangle((S // 2 - 1, 10, S // 2 + 1, S - 8), fill=dark)
    d.line((S // 2 + 1, 12, S // 2 + 1, S - 10), fill=light)

    for i in range(3):
        y = 14 + i * 6
        d.line((13, y, S - 12, y - 3), fill=accent)
    d.ellipse((S // 2 - 5, S // 2 - 4, S // 2 + 5, S // 2 + 6), fill=dark)
    d.ellipse((S // 2 - 3, S // 2 - 2, S // 2 + 3, S // 2 + 4), fill=accent)
    d.ellipse((S // 2 - 1, S // 2, S // 2 + 1, S // 2 + 2), fill=(255, 255, 255, 230))


def wpn_gun(d, S, pal):
    """射手：拼装枪械（枪管 + 机匣 + 握把 + 弹匣 + 能量点）。"""
    main, dark, light, accent = pal["main"], pal["dark"], pal["light"], pal["accent"]
    y = S // 2 - 5

    d.rectangle((10, y, S - 5, y + 9), fill=dark)
    d.rectangle((11, y + 1, S - 6, y + 8), fill=main)
    d.rectangle((12, y + 2, S - 7, y + 4), fill=light)

    d.rectangle((2, y + 3, 11, y + 6), fill=dark)
    d.rectangle((3, y + 4, 11, y + 5), fill=light)

    d.polygon([(14, y + 9), (21, y + 9), (16, S - 5), (10, S - 6)], fill=dark)
    d.polygon([(15, y + 9), (19, y + 9), (15, S - 7), (12, S - 8)], fill=pal["grip"])

    d.rectangle((S // 2, y + 9, S // 2 + 6, y + 14), fill=dark)
    d.ellipse((S - 13, y + 3, S - 8, y + 8), fill=accent)
    d.ellipse((S - 11, y + 4, S - 10, y + 6), fill=(255, 255, 255, 230))


def wpn_staff(d, S, pal):
    """法师：法杖（细杖身 + 顶端宝石 + 缠绕的金属环）。"""
    main, dark, light, accent = pal["main"], pal["dark"], pal["light"], pal["accent"]

    d.line((6, S - 4, S // 2 + 2, 16), fill=dark, width=4)
    d.line((7, S - 5, S // 2 + 3, 17), fill=pal["grip"], width=2)

    cx, cy = S // 2 + 1, 12
    d.ellipse((cx - 6, cy - 6, cx + 6, cy + 6), fill=dark)
    d.ellipse((cx - 5, cy - 5, cx + 5, cy + 5), fill=main)
    d.polygon([(cx, cy - 4), (cx + 4, cy), (cx, cy + 4), (cx - 4, cy)], fill=accent)
    d.polygon([(cx, cy - 2), (cx + 2, cy), (cx, cy + 2), (cx - 2, cy)], fill=light)

    d.arc((cx - 9, cy - 9, cx + 9, cy + 9), start=110, end=250, fill=light, width=2)
    for i in range(3):
        x = 10 + i * 4
        y = S - 14 + i * 4
        d.ellipse((x, y, x + 2, y + 2), fill=accent)


def wpn_summon_staff(d, S, pal):
    """召唤师：召唤杖（杖身 + 悬浮核心 + 环绕碎片）。"""
    main, dark, light, accent = pal["main"], pal["dark"], pal["light"], pal["accent"]

    d.line((7, S - 4, S // 2 + 1, 15), fill=dark, width=5)
    d.line((8, S - 5, S // 2 + 2, 16), fill=pal["grip"], width=3)
    d.line((9, S - 6, S // 2 + 2, 17), fill=light, width=1)

    cx, cy = S // 2 + 1, 13
    d.ellipse((cx - 7, cy - 7, cx + 7, cy + 7), fill=dark)
    d.ellipse((cx - 5, cy - 5, cx + 5, cy + 5), fill=main)
    d.ellipse((cx - 3, cy - 3, cx + 3, cy + 3), fill=accent)
    d.ellipse((cx - 1, cy - 1, cx + 1, cy + 1), fill=(255, 255, 255, 240))

    for angle, radius in ((35, 11), (155, 10), (265, 12)):
        rad = math.radians(angle)
        ox = cx + radius * math.cos(rad)
        oy = cy + radius * math.sin(rad)
        d.polygon([(ox, oy - 2), (ox + 2, oy), (ox, oy + 2), (ox - 2, oy)], fill=light)


def wpn_shuriken(d, S, pal):
    """盗贼：投掷刃（四叶手里剑）。"""
    main, dark, light, accent = pal["main"], pal["dark"], pal["light"], pal["accent"]
    c = S / 2.0
    outer = S / 2.0 - 4
    inner = outer * 0.34

    star = []
    for i in range(8):
        angle = math.pi * i / 4.0 - math.pi / 2.0
        radius = outer if i % 2 == 0 else inner
        star.append((c + radius * math.cos(angle), c + radius * math.sin(angle)))

    d.polygon(star, fill=dark)
    d.polygon([(c + (x - c) * 0.74, c + (y - c) * 0.74) for x, y in star], fill=main)
    d.polygon([(c + (x - c) * 0.40, c + (y - c) * 0.40) for x, y in star], fill=light)
    d.ellipse((c - 4, c - 4, c + 4, c + 4), fill=dark)
    d.ellipse((c - 3, c - 3, c + 3, c + 3), fill=accent)


def boss_weapon_sprite(kind, size, pal, folder):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    if kind == "Warrior":
        wpn_sword(d, size, pal, heavy=folder != "Boss1Scavenger")
    elif kind == "Mage":
        # 归档者是「档案法典」，其余三把走法杖形态
        if folder == "Boss2Archivist":
            wpn_book(d, size, pal)
        else:
            wpn_staff(d, size, pal)
    elif kind == "Ranger":
        wpn_gun(d, size, pal)
    elif kind == "Summoner":
        wpn_summon_staff(d, size, pal)
    else:
        wpn_shuriken(d, size, pal)

    return img


def boss_weapons():
    """40 把 Boss 武器（A 线 + B 线）的占位贴图。"""
    for folder, cls, kind in BOSS_WEAPONS:
        size = BOSS_WEAPON_SIZE[kind]
        img = boss_weapon_sprite(kind, size, BOSS_WEAPON_PALETTES[folder], folder)
        save(img, r"Content\Items\Weapons\%s\%s.png" % (folder, cls))


def boss_materials():
    """Boss 2/3/4 的专属材料：3 种残响碎片 + 2 种合金锭。"""
    fragments = [
        ("ArchivistFragment", (214, 210, 192, 255), (120, 126, 142, 255), (96, 150, 220, 255)),
        ("AshHeartFragment", (188, 84, 46, 255), (86, 78, 76, 255), (255, 206, 96, 255)),
        ("FireplaceFragment", (220, 230, 240, 255), (128, 150, 168, 255), (201, 162, 39, 255)),
    ]
    for name, main, dark, accent in fragments:
        img = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        d.polygon([(3, 9), (10, 2), (21, 8), (18, 21), (6, 22)], fill=main, outline=dark)
        d.polygon([(7, 10), (11, 5), (16, 9), (13, 15), (8, 15)], fill=accent)
        d.line((10, 2, 13, 12), fill=dark)
        d.line((13, 12, 18, 21), fill=dark)
        save(img, r"Content\Items\Materials\%s.png" % name)

    bars = [
        ("AshHeartAlloyBar", (188, 84, 46, 255), (86, 78, 76, 255), (236, 150, 84, 255)),
        ("FireplaceAlloyBar", (220, 230, 240, 255), (128, 150, 168, 255), (201, 162, 39, 255)),
    ]
    for name, main, dark, light in bars:
        img = Image.new("RGBA", (24, 20), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        d.polygon([(3, 13), (8, 5), (21, 5), (16, 13)], fill=light, outline=dark)
        d.rectangle((3, 13, 16, 17), fill=main, outline=dark)
        d.polygon([(16, 13), (21, 5), (21, 9), (16, 17)], fill=dark)
        d.line((6, 8, 19, 8), fill=(255, 255, 255, 130))
        save(img, r"Content\Items\Materials\%s.png" % name)


# ============================================================ Boss 2 归档者
# 美术风格：档案 / 索引 / 纸页 + 骨白冷色。
# 与另外三个主题区分：清道夫冷灰废料、灰烬之心暖橙余烬、壁炉守卫冷白金属。
ARCH_BONE = (214, 210, 192, 255)
ARCH_BONE_D = (140, 138, 124, 255)
ARCH_BONE_L = (244, 242, 230, 255)
ARCH_INK = (96, 150, 220, 255)      # 索引蓝（"墨"）
ARCH_INK_D = (52, 84, 140, 255)
ARCH_FRAME = (120, 126, 142, 255)

ARCH_FRAME_W = 110
ARCH_FRAME_H = 110
ARCH_FRAMES = 4


def archivist_frame(f):
    """一帧归档者（110x110）：悬浮的档案柜式审计单元。

    结构：上半是层层叠起的"档案抽屉"，中间是竖立的索引书脊，
    下方垂着散开的纸页"裙摆"；正面一只冷蓝色单眼传感器。
    帧序 0~3 = 纸页翻动式的轻微起伏。
    """
    img = Image.new("RGBA", (ARCH_FRAME_W, ARCH_FRAME_H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rng = random.Random(1000 + f)
    bob = (0, -2, 0, 2)[f % 4]

    # ---------- 下方散开的纸页裙摆（先画，被主体压住上半） ----------
    for i in range(7):
        spread = (i - 3) * 13
        px = 55 + spread
        py = 74 + bob + abs(spread) * 0.22
        sway = rng.randint(-3, 3)
        d.polygon([(px - 9, py), (px + 9, py - 4 + sway), (px + 7, py + 26), (px - 8, py + 24)],
                  fill=ARCH_BONE, outline=ARCH_FRAME)
        d.line((px - 5, py + 5, px + 5, py + 3), fill=ARCH_BONE_D)
        d.line((px - 5, py + 11, px + 5, py + 9), fill=ARCH_BONE_D)

    # ---------- 主体：档案柜 ----------
    body_box = (18, 20 + bob, 92, 84 + bob)
    d.rounded_rectangle(body_box, radius=8, fill=ARCH_BONE, outline=ARCH_FRAME, width=2)

    # 抽屉分层（三条横向分隔）+ 每层一个拉手
    for row in range(3):
        y = 26 + bob + row * 19
        d.line((20, y, 90, y), fill=ARCH_FRAME)
        d.rounded_rectangle((70, y + 5, 86, y + 12), radius=2, fill=ARCH_BONE_D, outline=ARCH_FRAME)
        # 抽屉缝隙里透出的索引蓝
        d.line((22, y + 2, 66, y + 2), fill=(ARCH_INK[0], ARCH_INK[1], ARCH_INK[2], 90))

    # ---------- 竖立的索引书脊（左右各一道，强调"档案"轮廓） ----------
    d.rounded_rectangle((10, 24 + bob, 20, 82 + bob), radius=3, fill=ARCH_FRAME, outline=(70, 74, 88, 255))
    d.rounded_rectangle((90, 24 + bob, 100, 82 + bob), radius=3, fill=ARCH_FRAME, outline=(70, 74, 88, 255))
    for row in range(3):
        y = 32 + bob + row * 19
        d.line((13, y, 17, y), fill=ARCH_BONE_L)
        d.line((93, y, 97, y), fill=ARCH_BONE_L)

    # ---------- 顶部：翻开的索引册页 ----------
    d.polygon([(16, 20 + bob), (55, 8 + bob), (94, 20 + bob), (55, 26 + bob)],
              fill=ARCH_BONE_L, outline=ARCH_FRAME)
    d.line((55, 8 + bob, 55, 26 + bob), fill=ARCH_FRAME)
    for i in range(3):
        d.line((30 + i * 16, 17 + bob, 40 + i * 16, 13 + bob), fill=ARCH_BONE_D)

    # ---------- 单眼传感器（冷蓝，随帧闪烁） ----------
    eye_bright = f % 2 == 0
    eye = ARCH_INK if eye_bright else ARCH_INK_D
    d.ellipse((40, 44 + bob, 70, 66 + bob), fill=ARCH_FRAME, outline=(70, 74, 88, 255), width=2)
    d.ellipse((45, 48 + bob, 65, 62 + bob), fill=eye)
    d.ellipse((50, 51 + bob, 58, 57 + bob), fill=(226, 240, 255, 255))
    # 传感器两侧的固定铆钉
    d.ellipse((46, 36 + bob, 52, 42 + bob), fill=ARCH_BONE_D)
    d.ellipse((58, 36 + bob, 64, 42 + bob), fill=ARCH_BONE_D)

    # ---------- 漂在体侧的散页（提示"它在归档"） ----------
    for i in range(3):
        ox = 6 if i % 2 == 0 else 96
        oy = 34 + bob + i * 20
        d.rectangle((ox, oy, ox + 8, oy + 11), fill=ARCH_BONE_L, outline=ARCH_FRAME)
        d.line((ox + 2, oy + 4, ox + 6, oy + 4), fill=ARCH_INK)

    return img


def archivist_body():
    """本体贴图：110 宽 × 4 帧 × 110 高（纵向排列）。"""
    img = Image.new("RGBA", (ARCH_FRAME_W, ARCH_FRAME_H * ARCH_FRAMES), (0, 0, 0, 0))
    for f in range(ARCH_FRAMES):
        img.paste(archivist_frame(f), (0, f * ARCH_FRAME_H))
    save(img, r"Content\NPCs\Bosses\Archivist\Archivist.png")


def archivist_head():
    """Boss 血条图标（34x34）：一张封着冷蓝印戳的档案封面。"""
    img = Image.new("RGBA", (34, 34), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((4, 5, 30, 31), radius=4, fill=ARCH_BONE, outline=ARCH_FRAME, width=2)
    d.line((8, 12, 26, 12), fill=ARCH_BONE_D)
    d.line((8, 17, 26, 17), fill=ARCH_BONE_D)
    d.line((8, 22, 20, 22), fill=ARCH_BONE_D)
    d.ellipse((17, 20, 30, 33), fill=ARCH_INK, outline=ARCH_FRAME, width=2)
    d.ellipse((21, 24, 26, 29), fill=(226, 240, 255, 255))
    save(img, r"Content\NPCs\Bosses\Archivist\Archivist_Head_Boss.png")


def archivist_beam():
    """索引光束：细长的冷白激光条（代码里按角度拉伸，这里只保证有 1x1 以上的实心像素）。"""
    img = Image.new("RGBA", (48, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle((0, 8, 47, 11), fill=ARCH_BONE_L)
    d.rectangle((0, 6, 47, 7), fill=(180, 210, 255, 200))
    d.rectangle((0, 12, 47, 13), fill=(180, 210, 255, 200))
    d.rectangle((0, 0, 3, 19), fill=ARCH_INK)
    save(img, r"Content\Projectiles\Archivist\ArchivistIndexBeam.png")


def archivist_archive_page():
    """敌方档案页（20x20）：一张带索引条目的纸页。"""
    img = Image.new("RGBA", (20, 20), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.polygon([(3, 2), (17, 2), (17, 17), (3, 17)], fill=ARCH_BONE, outline=ARCH_FRAME)
    d.polygon([(14, 2), (17, 5), (14, 5)], fill=ARCH_BONE_D)
    for i in range(3):
        d.line((5, 6 + i * 4, 15 - i * 2, 6 + i * 4), fill=ARCH_INK_D)
    d.ellipse((8, 8, 12, 12), fill=ARCH_INK)
    save(img, r"Content\Projectiles\Archivist\ArchivistArchivePage.png")


def archivist_barrage_sheet():
    """弹幕墙的纸页（24x52）：纵向的一页，密排时形成"墙"。"""
    img = Image.new("RGBA", (24, 52), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle((2, 2, 21, 49), fill=ARCH_BONE, outline=ARCH_FRAME, width=2)
    for i in range(6):
        y = 8 + i * 7
        d.line((5, y, 18 - (i % 2) * 4, y), fill=ARCH_INK_D)
    d.rectangle((2, 24, 21, 27), fill=ARCH_INK)
    save(img, r"Content\Projectiles\Archivist\ArchivistBarrageSheet.png")


def archivist_seal():
    """归档封印（72x72）：一圈符文 + 中间的印戳，落下时把玩家钉住。"""
    img = Image.new("RGBA", (72, 72), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((4, 4, 67, 67), outline=ARCH_INK, width=3)
    d.ellipse((12, 12, 59, 59), outline=(180, 210, 255, 200), width=2)
    d.ellipse((22, 22, 49, 49), fill=(40, 52, 74, 190), outline=ARCH_INK, width=2)
    # 符文刻痕
    for i in range(8):
        angle = math.radians(i * 45)
        x1 = 36 + 26 * math.cos(angle)
        y1 = 36 + 26 * math.sin(angle)
        x2 = 36 + 33 * math.cos(angle)
        y2 = 36 + 33 * math.sin(angle)
        d.line((x1, y1, x2, y2), fill=ARCH_BONE_L, width=2)
    # 中间的封条
    d.rectangle((30, 26, 42, 46), fill=ARCH_BONE, outline=ARCH_FRAME)
    d.line((33, 32, 39, 32), fill=ARCH_INK_D)
    d.line((33, 37, 39, 37), fill=ARCH_INK_D)
    save(img, r"Content\Projectiles\Archivist\ArchivistSeal.png")


def archivist_echo():
    """召唤物「归档者残响」（26x26）：一枚封着冷蓝光的骨白残片 + 声波。"""
    img = Image.new("RGBA", (26, 26), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.polygon([(8, 4), (20, 7), (19, 20), (9, 22), (5, 13)], fill=ARCH_BONE, outline=ARCH_FRAME, width=2)
    d.polygon([(11, 8), (17, 10), (16, 17), (12, 18)], fill=ARCH_INK)
    d.ellipse((12, 11, 16, 15), fill=(226, 240, 255, 255))
    # 向外扩散的声波
    d.arc((1, 2, 25, 24), start=110, end=250, fill=(150, 190, 240, 200), width=2)
    d.arc((4, 5, 22, 21), start=115, end=245, fill=(190, 220, 255, 230), width=2)
    save(img, r"Content\Items\Summons\ArchivistEcho.png")


def ash_heart_ember():
    """召唤物「灰烬之心余烬」（26x26）：一簇还在阴燃的余烬 + 火星。"""
    img = Image.new("RGBA", (26, 26), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # 余烬堆
    d.ellipse((4, 12, 22, 24), fill=(86, 78, 76, 255), outline=(52, 46, 44, 255))
    d.ellipse((8, 10, 18, 20), fill=(188, 84, 46, 255))
    d.ellipse((10, 11, 16, 17), fill=(236, 150, 84, 255))
    d.ellipse((12, 12, 14, 15), fill=(255, 226, 170, 255))
    # 上升的火星与火舌
    d.polygon([(13, 2), (16, 10), (10, 10)], fill=(236, 150, 84, 230))
    d.polygon([(13, 5), (15, 10), (11, 10)], fill=(255, 206, 96, 255))
    d.ellipse((6, 4, 9, 7), fill=(255, 180, 90, 200))
    d.ellipse((18, 6, 21, 9), fill=(255, 200, 120, 180))
    save(img, r"Content\Items\Summons\AshHeartEmber.png")


def fireplace_key():
    """召唤物「壁炉通行密钥」（26x26）：冷白合金钥匙 + 中央冷光核心 + 金边。"""
    img = Image.new("RGBA", (26, 26), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cold = (220, 230, 240, 255)
    cold_d = (128, 150, 168, 255)
    gold = (201, 162, 39, 255)
    # 钥匙柄（圆环 + 金边）
    d.ellipse((3, 3, 15, 15), fill=cold, outline=cold_d, width=2)
    d.ellipse((6, 6, 12, 12), fill=(74, 82, 96, 255))
    d.arc((2, 2, 16, 16), start=200, end=340, fill=gold, width=2)
    # 钥匙杆
    d.rectangle((13, 12, 16, 24), fill=cold, outline=cold_d)
    # 匙齿
    d.rectangle((16, 17, 21, 20), fill=cold, outline=cold_d)
    d.rectangle((16, 22, 20, 24), fill=cold, outline=cold_d)
    # 冷光核心
    d.ellipse((6, 6, 12, 12), fill=(150, 200, 240, 255))
    d.ellipse((8, 8, 10, 10), fill=(250, 252, 255, 255))
    save(img, r"Content\Items\Summons\FireplaceKey.png")


def guard_against_overwriting_real_art():
    """⚠️ 这个脚本生成的是**占位美术**，会**直接覆盖**同名贴图。

    一旦你往 `E:\\开发\\art-inbox\\raw\\` 放过 AI 正式美术（见
    `E:\\开发\\美术需求_AI出图规格与3D路线.md`），再跑本脚本就会把正式美术冲掉。
    所以：raw 目录非空时**默认拒绝运行**，要重新生成占位图请显式加 `--force`。
    """
    raw = r"E:\开发\art-inbox\raw"

    if "--force" in sys.argv:
        print("[WARN] --force：正在用占位图覆盖已有贴图（AI 正式美术会被冲掉）")
        return

    if os.path.isdir(raw):
        existing = [name for name in os.listdir(raw) if name.lower().endswith(".png")]

        if existing:
            print("!! 拒绝运行：%s 里有 %d 张正式美术（%s ...）"
                  % (raw, len(existing), ", ".join(sorted(existing)[:3])))
            print("   这些是 AI 出图，本脚本会用占位图覆盖它们。")
            print("   确实要重新生成占位图：python gen_sprites.py --force")
            print("   只想换成正式美术：python apply_art.py --preview")
            sys.exit(2)


if __name__ == "__main__":
    guard_against_overwriting_real_art()
    icon()
    companion_body()
    companion_head()
    scavenger_body()
    scavenger_head()
    drone_body()
    pollution_zone()
    scavenger_bullet()
    scavenger_arm()
    steel_chunk()
    steel_bar()
    scavenger_fragment()
    signal_sensor()
    companion_core()
    elven_frame()
    pollution_homing()
    salvaged_steel_armor()
    soul_fragments()
    boss_bags()
    pollution_buff()
    boss_weapons()
    boss_materials()
    archivist_body()
    archivist_head()
    archivist_beam()
    archivist_archive_page()
    archivist_barrage_sheet()
    archivist_seal()
    archivist_echo()
    ash_heart_ember()
    fireplace_key()
    print("\nall placeholder sprites generated.")
