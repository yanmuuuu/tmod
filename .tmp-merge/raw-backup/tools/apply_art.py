# -*- coding: utf-8 -*-
"""把 `E:\\开发\\art-inbox` 里的 AI 出图处理成模组能用的像素贴图。

每张图的处理链（顺序很重要）：
  1. **抠品红**：按「品红程度」`min(R,B) - G` 算 alpha，而不是只看颜色相等——
     AI 图边缘有抗锯齿，硬键会留紫边；
  2. **去紫边**：把半透明像素的 RGB 换成最近的不透明像素颜色（色溢修正），
     否则缩放之后每个边缘都是脏紫色；
  3. 按 alpha 裁到包围盒；
  4. 等比缩放到目标画布（大比例用 BOX 面积平均，小比例用 LANCZOS）；
  5. alpha 阈值化（< 32 直接透明）+ 调色板量化（透明区的 RGB 不会污染调色板）；
  6. 居中写入目标路径；多帧资产按帧**纵向**堆叠。

用法：
    python E:\\开发\\tools\\apply_art.py            # 处理 art-inbox 里现有的图
    python E:\\开发\\tools\\apply_art.py --preview  # 额外生成 6 倍放大对照图（人工核对用）
"""
import os
import sys

import numpy as np
from PIL import Image, ImageOps

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from art_common import (  # noqa: E402
    bleed_fix, key_magenta, quantize, scale_into, threshold_alpha,
)
from split_sheet import split  # noqa: E402

INBOX = r"E:\开发\art-inbox"
MAIN = r"E:\开发\WastelandSoul"
PREVIEW = os.path.join(INBOX, "preview_P0.png")
SHEET_PREVIEW = os.path.join(INBOX, "preview_sheets.png")

# (art-inbox 文件名, 模组内相对路径, 目标画布, 颜色数, 内容占比)
ASSETS = [
    # 本体留 6% 边距：4 帧是靠整体上下位移做的悬浮循环，撑满高度会被切掉 2px
    ("Archivist_f0.png", r"Content\NPCs\Bosses\Archivist\Archivist.png", (110, 110), 30, 0.94),
    ("Archivist_head.png", r"Content\NPCs\Bosses\Archivist\Archivist_Head_Boss.png", (34, 34), 20, 0.94),
    ("ArchivistIndexBeam.png", r"Content\Projectiles\Archivist\ArchivistIndexBeam.png", (48, 20), 10, 1.00),
    ("ArchivistArchivePage.png", r"Content\Projectiles\Archivist\ArchivistArchivePage.png", (20, 20), 14, 0.94),
    ("ArchivistBarrageSheet.png", r"Content\Projectiles\Archivist\ArchivistBarrageSheet.png", (24, 52), 14, 0.98),
    ("ArchivistSeal.png", r"Content\Projectiles\Archivist\ArchivistSeal.png", (72, 72), 24, 0.98),
    ("ArchivistEcho.png", r"Content\Items\Summons\ArchivistEcho.png", (26, 26), 18, 0.94),
    ("ArchivistBag.png", r"Content\Items\Bags\ArchivistBag.png", (32, 32), 22, 0.94),
    ("ArchivistFragment.png", r"Content\Items\Materials\ArchivistFragment.png", (24, 24), 14, 0.94),
]



def process(path, size, colors, fill):
    image = Image.open(path).convert("RGBA")
    image = key_magenta(image)
    image = bleed_fix(image)
    image, placed = scale_into(image, size, fill)
    image = threshold_alpha(image)
    image = quantize(image, colors)
    return image, placed


def stack_frames(base, frames):
    """把单帧图派生/堆叠成纵向帧表：frames = [(dy, 亮度系数), ...]。"""
    width, height = base.size
    sheet = Image.new("RGBA", (width, height * len(frames)), (0, 0, 0, 0))

    for index, (dy, gain) in enumerate(frames):
        frame = base

        if gain != 1.0:
            array = np.asarray(frame).astype(np.float32)
            array[..., :3] = np.clip(array[..., :3] * gain, 0, 255)
            frame = Image.fromarray(array.astype(np.uint8), "RGBA")

        sheet.alpha_composite(frame, (0, index * height + dy))

    return sheet


def make_preview(pairs):
    """6 倍最近邻放大对照图，肉眼核对用。"""
    zoom = 6
    padding = 8
    tiles = []

    for image, label in pairs:
        tiles.append((image.resize((image.width * zoom, image.height * zoom), Image.NEAREST), label))

    width = sum(tile.width for tile, _ in tiles) + padding * (len(tiles) + 1)
    height = max(tile.height for tile, _ in tiles) + padding * 2

    sheet = Image.new("RGBA", (width, height), (32, 34, 40, 255))
    x = padding

    for tile, _ in tiles:
        sheet.alpha_composite(tile, (x, padding + (height - padding * 2 - tile.height) // 2))
        x += tile.width + padding

    sheet.save(PREVIEW)


# ======================================================================================
#  contact sheet（一张图里画了多个素材）的处理
# ======================================================================================

# 智械人：帧序按原版城镇 NPC 约定 —— 0-3 下 / 4-7 上 / 8-11 右 / 12-15 左 / 16-20 站立 / 21-24 使用
COMPANION_CONTENT_H = 42      # 内容高度（像素）：参照本机真实城镇 NPC 的 20x38 与旧占位的 29x43
COMPANION_BASELINE = 53       # 脚底所在行（帧高 56，留 3px）

# (sheet 文件名, 期望格数, 类型, 参数)
SHEET_JOBS = [
    {
        "sheet": "Scavenger_sheet.png", "count": 9, "kind": "frames",
        # 3x3 矩阵 = 悬浮 高/中/低 x 机械臂 收/半/全；
        # 取 6 帧做成收臂->伸臂->收臂的循环，接回第 0 帧时手臂正好收回
        "picks": [0, 4, 8, 7, 3, 1],
        "target": r"Content\NPCs\Bosses\Scavenger\Scavenger.png",
        "size": (110, 110), "colors": 30, "fill": 0.94, "align": "bottom",
    },
    {
        "sheet": "ScavengerRepairDrone_sheet.png", "count": 4, "kind": "frames",
        "picks": [0, 1, 2, 3],
        "target": r"Content\NPCs\Bosses\Scavenger\ScavengerRepairDrone.png",
        "size": (30, 26), "colors": 24, "fill": 0.94, "align": "bottom",
    },
    {
        "sheet": "SoulFragments_sheet.png", "count": 4, "kind": "cells",
        "colors": 20, "fill": 0.92,
        "items": [
            (0, r"Content\Items\Soul\SoulFragmentScavenger.png", (24, 24)),
            (1, r"Content\Items\Soul\SoulFragmentSecond.png", (24, 24)),
            (2, r"Content\Items\Soul\SoulFragmentThird.png", (24, 24)),
            (3, r"Content\Items\Soul\SoulFragmentFourth.png", (24, 24)),
        ],
    },
    {
        # 4 行（Boss）x 5 列（职业）；列宽按各职业的既有画布：战 44 / 法 38 / 射 44 / 召 40 / 盗 34
        "sheet": "Weapons_sheet.png", "count": 20, "kind": "cells",
        "colors": 26, "fill": 0.94,
        "items": [
            (0, r"Content\Items\Weapons\Boss1Scavenger\ScavengerWarriorWeapon.png", (44, 44)),
            (1, r"Content\Items\Weapons\Boss1Scavenger\ScavengerMageWeapon.png", (38, 38)),
            (2, r"Content\Items\Weapons\Boss1Scavenger\ScavengerRangerWeapon.png", (44, 44)),
            (3, r"Content\Items\Weapons\Boss1Scavenger\ScavengerSummonerWeapon.png", (40, 40)),
            (4, r"Content\Items\Weapons\Boss1Scavenger\ScavengerRogueWeapon.png", (34, 34)),
            (5, r"Content\Items\Weapons\Boss2Archivist\ArchivistWarriorWeapon.png", (44, 44)),
            (6, r"Content\Items\Weapons\Boss2Archivist\ArchivistMageWeapon.png", (38, 38)),
            (7, r"Content\Items\Weapons\Boss2Archivist\ArchivistRangerWeapon.png", (44, 44)),
            (8, r"Content\Items\Weapons\Boss2Archivist\ArchivistSummonerWeapon.png", (40, 40)),
            (9, r"Content\Items\Weapons\Boss2Archivist\ArchivistRogueWeapon.png", (34, 34)),
            (10, r"Content\Items\Weapons\Boss3AshHeart\AshHeartWarriorWeapon.png", (44, 44)),
            (11, r"Content\Items\Weapons\Boss3AshHeart\AshHeartMageWeapon.png", (38, 38)),
            (12, r"Content\Items\Weapons\Boss3AshHeart\AshHeartRangerWeapon.png", (44, 44)),
            (13, r"Content\Items\Weapons\Boss3AshHeart\AshHeartSummonerWeapon.png", (40, 40)),
            (14, r"Content\Items\Weapons\Boss3AshHeart\AshHeartRogueWeapon.png", (34, 34)),
            (15, r"Content\Items\Weapons\Boss4Fireplace\FireplaceWarriorWeapon.png", (44, 44)),
            (16, r"Content\Items\Weapons\Boss4Fireplace\FireplaceMageWeapon.png", (38, 38)),
            (17, r"Content\Items\Weapons\Boss4Fireplace\FireplaceRangerWeapon.png", (44, 44)),
            (18, r"Content\Items\Weapons\Boss4Fireplace\FireplaceSummonerWeapon.png", (40, 40)),
            (19, r"Content\Items\Weapons\Boss4Fireplace\FireplaceRogueWeapon.png", (34, 34)),
        ],
    },
    {
        # 3 行（头盔 / 胸甲 / 护腿）x 5 列（职业）—— 注意各职业的部位名不同（Hood/Robe/Visor/Vest/Cowl/Mask…）
        "sheet": "SalvagedSteelArmor_sheet.png", "count": 15, "kind": "cells",
        "colors": 22, "fill": 0.92,
        "items": [
            (0, r"Content\Items\Armor\SalvagedSteelWarriorHelm.png", (22, 22)),
            (1, r"Content\Items\Armor\SalvagedSteelRangerVisor.png", (22, 22)),
            (2, r"Content\Items\Armor\SalvagedSteelMageHood.png", (22, 22)),
            (3, r"Content\Items\Armor\SalvagedSteelSummonerCowl.png", (22, 22)),
            (4, r"Content\Items\Armor\SalvagedSteelRogueMask.png", (22, 22)),
            (5, r"Content\Items\Armor\SalvagedSteelWarriorPlate.png", (22, 22)),
            (6, r"Content\Items\Armor\SalvagedSteelRangerVest.png", (22, 22)),
            (7, r"Content\Items\Armor\SalvagedSteelMageRobe.png", (22, 22)),
            (8, r"Content\Items\Armor\SalvagedSteelSummonerTunic.png", (22, 22)),
            (9, r"Content\Items\Armor\SalvagedSteelRogueVest.png", (22, 22)),
            (10, r"Content\Items\Armor\SalvagedSteelWarriorGreaves.png", (22, 22)),
            (11, r"Content\Items\Armor\SalvagedSteelRangerLeggings.png", (22, 22)),
            (12, r"Content\Items\Armor\SalvagedSteelMageLeggings.png", (22, 22)),
            (13, r"Content\Items\Armor\SalvagedSteelSummonerLeggings.png", (22, 22)),
            (14, r"Content\Items\Armor\SalvagedSteelRogueLeggings.png", (22, 22)),
        ],
    },
]


def finish(image, colors):
    """裁剪后的收尾：阈值化 + 量化。"""
    return quantize(threshold_alpha(image), colors)


def common_scale(crops, size, fill):
    """所有格子用**同一个缩放比**（否则同一只 Boss 的各帧会忽大忽小）。"""
    widest = max(crop.width for crop in crops)
    tallest = max(crop.height for crop in crops)
    return min(size[0] * fill / widest, size[1] * fill / tallest)


def place(crop, size, scale, align="center"):
    """按给定缩放比缩放并放进画布（bottom = 底边对齐，用于会飞的 Boss / 无人机）。"""
    resample = Image.BOX if scale < (1.0 / 3.0) else Image.LANCZOS
    small = crop.resize((max(1, int(round(crop.width * scale))),
                         max(1, int(round(crop.height * scale)))), resample)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    x = (size[0] - small.width) // 2
    y = size[1] - small.height - 4 if align == "bottom" else (size[1] - small.height) // 2
    canvas.alpha_composite(small, (x, max(0, y)))
    return canvas


def tilt_frame(base, dy, split=26):
    """把头部区域整体上下移动 dy 像素 —— 做出「低头 / 抬头」。

    ⚠️ 头部要贴在 **y = dy**（不是 split + dy）：`head` 这一段是从 y=0 裁到 y=split 的，
    里面已经带着自己的行偏移。第一版写成 `split + dy`，结果整个头被复制到了胸口
    （看起来像"两个头"），而且上下只动了 1 像素。

    分界线取 y=26（脖子处）：头往下移时和身体重叠更多（不会露空）；
    头往上移时把身体顶部那一行往上复制几行补住脖子。
    """
    out = Image.new("RGBA", base.size, (0, 0, 0, 0))
    body = base.crop((0, split, base.width, base.height))
    head = base.crop((0, 0, base.width, split))

    out.alpha_composite(body, (0, split))

    if dy < 0:
        # 头往上会让脖子处露空：用身体顶部那一行往上复制 |dy| 行补住
        filler = body.crop((0, 0, base.width, 1)).resize((base.width, -dy), Image.NEAREST)
        out.alpha_composite(filler, (0, split + dy))

    out.alpha_composite(head, (0, dy))
    return out


def build_companion(crops, target, head_target):
    """4 个朝向 -> 25 帧城镇 NPC 帧表（40x1400）+ 16x16 头像。

    帧布局（按你的要求改过：**只要左右两个朝向，不要正背面**）：

        0-3    侧身朝右行走
        4-7    **偶尔低头 / 抬头**的小动作（原版"朝上走"那一排腾出来的）
               4 = 中立 / 5 = 低头 / 6 = 中立 / 7 = 抬头
        8-11   侧身朝右行走
        12-15  侧身朝左行走（朝右那张镜像）
        16-20  站立（正面）
        21-24  使用 / 攻击（举着核心）
    """
    front, side, _back, attack = crops
    left = ImageOps.mirror(side)          # 侧身只有朝右一张，朝左镜像即可
    _ = _back                            # 背面不再使用（原版那两个朝向按你的要求去掉）

    scaled = {}

    for key, pose in (("front", front), ("side", side), ("left", left), ("attack", attack)):
        ratio = COMPANION_CONTENT_H / float(pose.height)
        resample = Image.BOX if ratio < (1.0 / 3.0) else Image.LANCZOS
        scaled[key] = pose.resize((max(1, int(round(pose.width * ratio))),
                                   max(1, int(round(pose.height * ratio)))), resample)

    def compose(key, dy=0):
        pose = scaled[key]
        frame = Image.new("RGBA", (40, 56), (0, 0, 0, 0))
        x = (40 - pose.width) // 2
        y = COMPANION_BASELINE - pose.height - dy
        frame.alpha_composite(pose, (max(0, x), max(0, y)))
        return frame

    walk_bob = (0, -1, 0, 1)
    idle_bob = (0, -1, 0, 1, 0)
    attack_bob = (0, -2, -3, -2)

    sheet = Image.new("RGBA", (40, 56 * 25), (0, 0, 0, 0))
    order = ["side", "side", "side", "left"]      # 0-3 下 / 4-7 上 / 8-11 右 / 12-15 左

    for facing in range(4):
        for step in range(4):
            sheet.alpha_composite(compose(order[facing], walk_bob[step]),
                                  (0, (facing * 4 + step) * 56))

    # 4-7：低头 / 抬头（正面，静止动作）
    # 幅度取 3 像素：2px 在游戏里几乎看不出来（42px 高的角色），3px 动起来才明显，
    # 又不会顶到帧边界（低头时内容落在 y=14..55 之内）
    front_neutral = compose("front")
    sheet.alpha_composite(front_neutral, (0, 4 * 56))
    sheet.alpha_composite(tilt_frame(front_neutral, 3), (0, 5 * 56))      # 低头
    sheet.alpha_composite(front_neutral, (0, 6 * 56))
    sheet.alpha_composite(tilt_frame(front_neutral, -3), (0, 7 * 56))     # 抬头

    for index in range(5):                        # 16-20 站立
        sheet.alpha_composite(compose("front", idle_bob[index]), (0, (16 + index) * 56))

    for index in range(4):                        # 21-24 使用 / 攻击
        sheet.alpha_composite(compose("attack", attack_bob[index]), (0, (21 + index) * 56))

    os.makedirs(os.path.dirname(os.path.join(MAIN, target)), exist_ok=True)
    sheet.save(os.path.join(MAIN, target))

    # 头像：取正面姿态的头部条带（上 38%），再按内容裁紧缩到 16x16
    strip = front.crop((0, 0, front.width, max(8, int(front.height * 0.38))))
    box = strip.getchannel("A").getbbox()

    if box:
        strip = strip.crop(box)

    head = place(strip, (16, 16), min(16 / float(strip.width), 16 / float(strip.height)))
    head = finish(head, 20)
    head.save(os.path.join(MAIN, head_target))

    return sheet, head


def run_sheets(want_preview):
    if not os.path.exists(os.path.join(INBOX, "Companion_sheet.png")):
        return None

    previews = []

    # ---- 智械人：4 朝向 -> 25 帧 ----
    image, groups = split(Image.open(os.path.join(INBOX, "Companion_sheet.png")).convert("RGBA"), 4)
    crops = [image.crop(box) for box, _ in groups]

    if len(crops) != 4:
        raise SystemExit("!! 智械人那张图应该拆出 4 个朝向，实际 %d" % len(crops))

    body, head = build_companion(crops,
                                 r"Content\NPCs\Town\MechanicalCompanion.png",
                                 r"Content\NPCs\Town\MechanicalCompanion_Head.png")
    print("  [OK] Companion_sheet.png  -> 25 帧帧表 (40x1400) + 头像 (16x16)")
    previews.append(body.crop((0, 0, 40, 56 * 6)))
    previews.append(head)

    # ---- 其余：通用 frames / cells ----
    for job in SHEET_JOBS:
        path = os.path.join(INBOX, job["sheet"])

        if not os.path.exists(path):
            print("  [SKIP] art-inbox 里没有: %s" % job["sheet"])
            continue

        image, groups = split(Image.open(path).convert("RGBA"), job["count"])
        crops = [image.crop(box) for box, _ in groups]

        if len(crops) != job["count"]:
            raise SystemExit("!! %s 应该拆出 %d 格，实际 %d" % (job["sheet"], job["count"], len(crops)))

        if job["kind"] == "frames":
            picks = job.get("picks") or list(range(len(crops)))
            chosen = [crops[index] for index in picks]
            scale = common_scale(chosen, job["size"], job["fill"])
            sheet = Image.new("RGBA", (job["size"][0], job["size"][1] * len(chosen)), (0, 0, 0, 0))

            for index, crop in enumerate(chosen):
                frame = finish(place(crop, job["size"], scale, job.get("align", "center")),
                               job["colors"])
                sheet.alpha_composite(frame, (0, index * job["size"][1]))

            target = os.path.join(MAIN, job["target"])
            os.makedirs(os.path.dirname(target), exist_ok=True)
            sheet.save(target)
            print("  [OK] %-32s -> %-58s %s（%d 帧，取格 %s）"
                  % (job["sheet"], job["target"], sheet.size, len(chosen), picks))
            previews.append(sheet)
        else:
            for index, relative, size in job["items"]:
                crop = crops[index]
                scale = min(size[0] * job["fill"] / crop.width, size[1] * job["fill"] / crop.height)
                icon = finish(place(crop, size, scale), job["colors"])
                target = os.path.join(MAIN, relative)
                os.makedirs(os.path.dirname(target), exist_ok=True)
                icon.save(target)
                previews.append(icon)

            print("  [OK] %-32s -> %d 个图标已写入" % (job["sheet"], len(job["items"])))

    if want_preview and previews:
        make_sheet_preview(previews)

    return previews


def make_sheet_preview(images):
    """把这一批产物拼成对照图（每格 8 倍最近邻）。"""
    zoom = 8
    padding = 10
    tiles = [image.resize((image.width * zoom, image.height * zoom), Image.NEAREST)
             for image in images]
    columns = 8
    rows = (len(tiles) + columns - 1) // columns
    cell_w = max(tile.width for tile in tiles) + padding
    cell_h = max(tile.height for tile in tiles) + padding
    sheet = Image.new("RGBA", (columns * cell_w + padding, rows * cell_h + padding), (30, 32, 38, 255))

    for index, tile in enumerate(tiles):
        x = padding + (index % columns) * cell_w
        y = padding + (index // columns) * cell_h
        sheet.alpha_composite(tile, (x + (cell_w - padding - tile.width) // 2,
                                     y + (cell_h - padding - tile.height) // 2))

    sheet.save(SHEET_PREVIEW)
    print("  对照图: %s" % SHEET_PREVIEW)


def main():
    want_preview = "--preview" in sys.argv
    missing = []
    preview_pairs = []

    if "--sheets-only" not in sys.argv:
        for name, relative, size, colors, fill in ASSETS:
            source = os.path.join(INBOX, name)

            if not os.path.exists(source):
                missing.append(name)
                continue

            image, placed = process(source, size, colors, fill)

            if name == "Archivist_f0.png":
                # 本体：出了 f1~f3 就用**真实帧**（原样堆叠，不做位移）；
                # 只出了一帧就派生成 4 帧的悬浮循环（上下 2 像素 + 一帧略暗，做纸页翻动感）。
                real_frames = []

                for index in range(4):
                    candidate = os.path.join(INBOX, "Archivist_f%d.png" % index)

                    if os.path.exists(candidate):
                        real_frames.append(process(candidate, size, colors, fill)[0])

                if len(real_frames) >= 2:
                    sheet = Image.new("RGBA", (size[0], size[1] * 4), (0, 0, 0, 0))

                    for index in range(4):
                        sheet.alpha_composite(real_frames[index % len(real_frames)], (0, index * size[1]))

                    image = sheet
                    print("     （用了 %d 张真实帧）" % len(real_frames))
                else:
                    image = stack_frames(image, [(2, 1.0), (0, 1.0), (-2, 0.88), (0, 1.0)])
                    print("     （只有 f0，已派生 4 帧悬浮循环；补 f1~f3 会自动改用真实帧）")

                placed = (placed[0], placed[1] * 4)

            target = os.path.join(MAIN, relative)
            os.makedirs(os.path.dirname(target), exist_ok=True)
            image.save(target)
            print("  [OK] %-26s -> %-64s %s  内容 %dx%d" % (name, relative, image.size, placed[0], placed[1]))

            preview_pairs.append((image if image.height <= 200 else image.crop((0, 0, image.width, image.height // 4)), name))

    if missing:
        print("  [SKIP] art-inbox 里没有: %s" % ", ".join(missing))

    if "--p0-only" not in sys.argv:
        run_sheets(want_preview)

    # 图标换完之后，穿着贴图（40x1120 的 20 帧装备帧表）必须跟着重生成：
    # 否则就会出现"图标是新的、穿在身上还是旧的"（见 gen_armor_equip.py 的说明）
    if "--p0-only" not in sys.argv and "--no-armor" not in sys.argv:
        import gen_armor_equip  # noqa: E402
        gen_armor_equip.main()

    if want_preview and preview_pairs:
        make_preview(preview_pairs)
        print("  预览图: %s" % PREVIEW)

    return 0


if __name__ == "__main__":
    sys.exit(main())
