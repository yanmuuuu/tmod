# -*- coding: utf-8 -*-
"""生成「穿在身上」的装备帧表（40 宽 × 20 帧 × 56 高）。

为什么需要它（"图标改了，穿在身上为什么没变"）：

  tModLoader 里一件防具的**图标**和**穿在身上的样子**是**两套完全不同的贴图**：

      物品图标          Content/Items/Armor/SalvagedSteelMageHood.png        22x22
      穿在身上的帧表    Content/Items/Armor/SalvagedSteelMageHood_Head.png   40x1120
                                                     ^^^^^ 后缀 = 挂到哪个装备栏

  40x1120 = **20 帧 × 40x56**，绑在原版玩家骨架上。ChatGPT 出的图对不齐这套骨架，
  所以之前只换了图标、身上还是旧的程序占位图。

**这一版的画法（按你的反馈改的）**：
  · **不再把整张图标贴上去**（太繁复、和原版风格差太远），改成用**从图标里提取的配色**
    画精简形状：1 像素深色描边 + 主色块 + 一道亮色高光；
  · **头盔画小**（14x10 左右，压在头部而不是整个头）；
  · **护腿按《我的世界》那种结构**：两条分开的竖直"裤管"，中间留缝，膝盖一道亮色带；
  · 位置锚点沿用旧占位图（不会和玩家骨架跑偏），每 8 帧上下浮动一次。
"""
import os
import sys

from PIL import Image, ImageDraw

MAIN = r"E:\开发\WastelandSoul"
ARMOR_DIR = os.path.join(MAIN, r"Content\Items\Armor")

FRAME_W, FRAME_H, FRAMES = 40, 56, 20
BOB = (0, 0, 1, 1, 0, 0, -1, -1)

# 职业 -> (头部件名, 胸部件名, 腿部件名)
CLASSES = {
    "Warrior": ("Helm", "Plate", "Greaves"),
    "Mage": ("Hood", "Robe", "Leggings"),
    "Ranger": ("Visor", "Vest", "Leggings"),
    "Summoner": ("Cowl", "Tunic", "Leggings"),
    "Rogue": ("Mask", "Vest", "Leggings"),
}

# 放置框（左, 上, 右, 下）——**必须首尾相接**：
# 头 8..20、身 20..36、腿 35..53，相邻部位重叠 1 像素，穿起来不会露出身体。
BOXES = {
    "Head": (13, 8, 27, 20),
    "Body": (11, 20, 29, 36),
    "Legs": (13, 35, 27, 53),
}


def part_of(part):
    if part in ("Helm", "Hood", "Visor", "Cowl", "Mask"):
        return "Head"
    if part in ("Plate", "Robe", "Vest", "Tunic"):
        return "Body"
    return "Legs"


def palette_from_icon(icon):
    """从 AI 图标里取 4 个用途色：描边(最暗) / 主色(中间调里最常见的) / 亮色(最亮) / 点缀色(最艳)。

    主色特意只在**中间亮度**里挑：不然法师那套会被图标上的金色描边带偏，整套变橙。
    """
    icon = icon.convert("RGBA")
    counts = icon.getcolors(maxcolors=1 << 20) or []
    opaque = [(count, color) for count, color in counts if color[3] > 200]

    if not opaque:
        return (60, 62, 70, 255), (150, 152, 160, 255), (205, 208, 214, 255), (196, 120, 60, 255)

    def luma(color):
        return 0.299 * color[0] + 0.587 * color[1] + 0.114 * color[2]

    def saturation(color):
        high, low = max(color[:3]), min(color[:3])
        return 0 if high == 0 else (high - low) / float(high)

    mid = [item for item in opaque if 55 <= luma(item[1]) <= 190] or opaque
    mid.sort(key=lambda item: -item[0])

    main = mid[0][1]
    dark = min(opaque, key=lambda item: luma(item[1]))[1]
    light = max(opaque, key=lambda item: luma(item[1]))[1]
    accent = max(opaque, key=lambda item: saturation(item[1]) * (1.0 + item[0] / 500.0))[1]
    outline = tuple(max(0, int(channel * 0.5)) for channel in dark[:3]) + (255,)

    return outline, main, light, accent


def draw_head(d, kind, palette):
    """小头盔：14x12，坐在肩上（底边和胸甲重叠 1px）。"""
    outline, main, light, accent = palette
    left, top, right, bottom = BOXES["Head"]

    if kind == "Hood":
        # 尖顶兜帽：脸部压暗
        d.polygon([(left + 6, top - 4), (right - 6, top - 4), (right, bottom), (left, bottom)],
                  fill=main, outline=outline)
        d.rectangle((left + 3, top + 4, right - 3, bottom - 1), fill=outline)
        d.line((left + 5, top - 2, right - 6, top - 2), fill=light)
    elif kind == "Visor":
        # 护目镜：一条亮色横带 + 两个深色镜片
        d.rounded_rectangle((left, top + 1, right, bottom), radius=2, fill=main, outline=outline)
        d.rectangle((left + 1, top + 4, right - 1, top + 6), fill=accent)
        d.point((left + 3, top + 5), fill=outline)
        d.point((right - 4, top + 5), fill=outline)
        d.line((left + 3, top + 2, right - 4, top + 2), fill=light)
    elif kind == "Cowl":
        # 圆头巾：正中一颗点缀
        d.rounded_rectangle((left, top + 2, right, bottom), radius=4, fill=main, outline=outline)
        d.point((left + 6, top + 6), fill=accent)
        d.line((left + 3, top + 3, right - 4, top + 3), fill=light)
    elif kind == "Mask":
        # 面罩：只裹下半张脸（眼睛留出来）
        d.rectangle((left + 1, top + 6, right - 1, bottom - 1), fill=main, outline=outline)
        d.line((left + 2, top + 6, right - 3, top + 6), fill=light)
        d.rectangle((left + 3, top + 8, right - 4, top + 9), fill=outline)
    else:  # Helm：全罩盔 + 面部竖缝
        d.rounded_rectangle((left, top + 1, right, bottom), radius=2, fill=main, outline=outline)
        d.rectangle((left + 6, top + 3, right - 6, bottom - 1), fill=outline)
        d.line((left + 2, top + 2, right - 3, top + 2), fill=light)


def draw_body(d, kind, palette):
    """胸甲：18x16 的躯干 + 两个小肩甲，收窄一点，比"整块大方块"贴原版。"""
    outline, main, light, accent = palette
    left, top, right, bottom = BOXES["Body"]
    torso = (left + 2, top, right - 2, bottom)

    if kind in ("Robe", "Tunic"):
        d.polygon([(torso[0] + 1, torso[1]), (torso[2] - 1, torso[1]),
                   (torso[2], torso[3]), (torso[0], torso[3])], fill=main, outline=outline)
        d.line((torso[0] + 2, torso[1] + 2, torso[2] - 3, torso[1] + 2), fill=light)
        d.line((torso[0] + 4, torso[1] + 3, torso[0] + 4, torso[3] - 1), fill=accent)
    else:
        d.rounded_rectangle(torso, radius=2, fill=main, outline=outline)
        d.line((torso[0] + 1, torso[1] + 2, torso[2] - 2, torso[1] + 2), fill=light)
        d.rectangle((torso[0] + 4, torso[1] + 5, torso[0] + 5, torso[3] - 3), fill=accent)
        d.rectangle((torso[0], torso[3] - 3, torso[2], torso[3] - 2), fill=outline)   # 腰带

    # 肩甲（和躯干连成一体，不悬空）
    d.rectangle((left, top + 1, left + 2, top + 6), fill=main, outline=outline)
    d.rectangle((right - 2, top + 1, right, top + 6), fill=main, outline=outline)


def draw_legs(d, palette):
    """护腿：**《我的世界》那种两条分开的竖直裤管**，上端接住胸甲，膝盖一道亮色带。"""
    outline, main, light, accent = palette
    left, top, right, bottom = BOXES["Legs"]
    mid = (left + right) // 2
    legs = ((left, mid - 2), (mid + 2, right))

    for x0, x1 in legs:
        d.rectangle((x0, top, x1, bottom), fill=main, outline=outline)
        d.line((x0 + 1, top + 1, x0 + 1, bottom - 1), fill=light)          # 外侧高光
        d.rectangle((x0 + 1, top + 6, x1 - 1, top + 7), fill=accent)       # 膝盖带
        d.rectangle((x0 + 1, bottom - 3, x1 - 1, bottom - 1), fill=outline)  # 靴口


def draw_piece(part, kind, palette):
    """一个部位的 40x56 单片（透明底）。"""
    layer = Image.new("RGBA", (FRAME_W, FRAME_H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    if part == "Head":
        draw_head(d, kind, palette)
    elif part == "Body":
        draw_body(d, kind, palette)
    else:
        draw_legs(d, palette)

    return layer


def build_sheet(icon, part, kind):
    palette = palette_from_icon(icon)
    piece = draw_piece(part, kind, palette)
    sheet = Image.new("RGBA", (FRAME_W, FRAME_H * FRAMES), (0, 0, 0, 0))

    for index in range(FRAMES):
        frame = Image.new("RGBA", (FRAME_W, FRAME_H), (0, 0, 0, 0))
        frame.alpha_composite(piece, (0, BOB[index % len(BOB)]))
        sheet.alpha_composite(frame, (0, index * FRAME_H))

    return sheet


def main():
    if not os.path.isdir(ARMOR_DIR):
        print("!! 找不到护甲目录: %s" % ARMOR_DIR)
        return 1

    built = 0

    for cls, parts in CLASSES.items():
        for part in parts:
            slot = part_of(part)
            item = "SalvagedSteel%s%s" % (cls, part)
            icon_path = os.path.join(ARMOR_DIR, item + ".png")
            target = os.path.join(ARMOR_DIR, "%s_%s.png" % (item, slot))

            if not os.path.exists(icon_path):
                print("  [SKIP] 没有图标: %s.png" % item)
                continue

            sheet = build_sheet(Image.open(icon_path).convert("RGBA"), slot, part)
            sheet.save(target)
            print("  [OK] %-26s -> %-44s %s" % (item + ".png", "%s_%s.png" % (item, slot),
                                                sheet.size))
            built += 1

    # 旧命名（Helm/Plate/Greaves 版）留下的孤儿：没有任何物品类引用，留着只会让人改错文件。
    # 图标（22x22）和穿着帧表（40x1120）都要清。
    wanted = set()
    wanted_icons = set()

    for cls, parts in CLASSES.items():
        for part in parts:
            wanted.add("SalvagedSteel%s%s_%s.png" % (cls, part, part_of(part)))
            wanted_icons.add("SalvagedSteel%s%s.png" % (cls, part))

    removed = []

    for name in sorted(os.listdir(ARMOR_DIR)):
        if not name.startswith("SalvagedSteel") or not name.endswith(".png"):
            continue

        if not any(name.startswith("SalvagedSteel" + cls) for cls in CLASSES):
            continue

        is_sheet = name.endswith(("_Head.png", "_Body.png", "_Legs.png"))

        if is_sheet:
            if name in wanted:
                continue
        elif name in wanted_icons or "_" in name:
            continue

        os.remove(os.path.join(ARMOR_DIR, name))
        removed.append(name)

    if removed:
        print("  [CLEAN] 删除 %d 个没人引用的旧护甲贴图：%s"
              % (len(removed), ", ".join(removed[:4]) + (" ..." if len(removed) > 4 else "")))

    sheets = len([n for n in os.listdir(ARMOR_DIR)
                  if n.endswith(("_Head.png", "_Body.png", "_Legs.png"))])
    icons = len([n for n in os.listdir(ARMOR_DIR)
                 if n.endswith(".png") and not n.endswith(("_Head.png", "_Body.png", "_Legs.png"))])
    print("  生成 %d 张装备帧表；目录里现有 %d 张帧表 / %d 张图标" % (built, sheets, icons))
    return 0


if __name__ == "__main__":
    sys.exit(main())
