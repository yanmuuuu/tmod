# -*- coding: utf-8 -*-
"""壁炉环境这一批的贴图：瑜钢合金（本体 + 压条）、有害气体减益图标、防毒面具。

风格：接近原版**黑曜石**的哑光块体，但整体偏**青银间色**。
纯程序化生成（PIL），不依赖任何外部素材，和 tools/gen_batch16_art.py 一个路子。

输出（尺寸必须和代码里的用法对上）：
  WastelandSoul/Content/Tiles/YugangAlloy.png    16x16   普通方块
  WastelandSoul/Content/Tiles/YugangTrim.png     16x16   普通方块（压条）
  WastelandSoul/Content/Buffs/GasPoison.png      32x32   减益图标
  WastelandSoul/Content/Items/Accessories/GasMask.png 24x24 饰品
另存一张拼图预览到 art-inbox/preview_fireplace.png，方便肉眼检查。
"""
import os
import random

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOD = os.path.join(ROOT, "WastelandSoul")

# 瑜钢合金配色：**黑曜石般的暗底 + 随机分布的青白点**（玩家要求"青白以点状随机分布"）
ALLOY_BASE = (26, 30, 36)
ALLOY_EDGE = (16, 19, 24)
DOT_BRIGHT = (226, 250, 252)
DOT_MID = (168, 232, 238)
DOT_DIM = (98, 152, 162)

TRIM_BASE = (42, 56, 64)
TRIM_EDGE = (24, 32, 38)


def noise_block(base, dark, light, silver, seed, silver_chance=0.10):
    """一块 16x16 的哑光金属：细碎噪点 + 左上高光 + 零星银点。"""
    rng = random.Random(seed)
    img = Image.new("RGBA", (16, 16), base + (255,))
    pixels = img.load()

    for y in range(16):
        for x in range(16):
            roll = rng.random()

            if roll < silver_chance:
                pixels[x, y] = silver + (255,)
            elif roll < silver_chance + 0.22:
                pixels[x, y] = dark + (255,)
            elif roll < silver_chance + 0.42:
                pixels[x, y] = light + (255,)

    # 左上高光 / 右下压暗，做出"块体"的立体感（原版方块就是这个套路）
    for i in range(16):
        if rng.random() < 0.75:
            pixels[i, 0] = light + (255,)
        if rng.random() < 0.55:
            pixels[0, i] = light + (255,)
        if rng.random() < 0.75:
            pixels[i, 15] = dark + (255,)
        if rng.random() < 0.55:
            pixels[15, i] = dark + (255,)

    return img


def _seam(img, base, edge_dark, edge_light):
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


def gas_poison_icon():
    """有害气体：**灰绿骷髅** + 背后两团稀薄的浊气。

    前两版都用"实心气团"表现，缩到 32px 一律读成灌木/西兰花。
    32px 的图标靠剪影最好认 —— 骷髅 + 稀薄烟雾是"毒"的通用符号。
    """
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1) 背后的浊气（半透明，只做氛围，别盖住骷髅）
    for cx, cy, radius in ((10, 22, 7), (22, 21, 7)):
        draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=(150, 162, 138, 130))

    # 2) 上扬的尾迹
    for i in range(5):
        for x, y in ((9 - i // 2, 12 - i), (23 + i // 2, 11 - i)):
            if 0 <= y < 32:
                draw.ellipse((x - 1, y - 1, x + 1, y + 1), fill=(160, 172, 146, 170))

    # 3) 骷髅：颅骨 + 下颌
    draw.ellipse((9, 8, 22, 22), fill=(224, 230, 216, 255), outline=(58, 66, 52, 255))
    draw.rectangle((12, 20, 19, 25), fill=(210, 218, 202, 255), outline=(58, 66, 52, 255))

    # 眼窝 / 鼻腔
    draw.ellipse((11, 12, 15, 17), fill=(44, 54, 42, 255))
    draw.ellipse((16, 12, 20, 17), fill=(44, 54, 42, 255))
    draw.polygon(((15, 17), (17, 17), (16, 20)), fill=(44, 54, 42, 255))

    # 牙缝
    for x in range(13, 19, 2):
        draw.line((x, 21, x, 24), fill=(70, 80, 64, 255))

    return img


def gas_mask_icon():
    """防毒面具：深色面罩 + 两个圆形滤毒罐 + 头带。"""
    img = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 面罩主体
    draw.ellipse((4, 6, 19, 20), fill=(74, 84, 92, 255), outline=(38, 46, 52, 255))
    draw.ellipse((6, 8, 17, 17), fill=(96, 108, 116, 255))

    # 两个滤毒罐
    for cx in (8, 15):
        draw.ellipse((cx - 3, 15, cx + 3, 21), fill=(58, 66, 72, 255), outline=(30, 36, 40, 255))
        draw.ellipse((cx - 2, 16, cx + 2, 20), fill=(140, 168, 176, 255))

    # 镜片
    for cx in (9, 15):
        draw.ellipse((cx - 2, 5, cx + 2, 9), fill=(150, 196, 206, 255), outline=(40, 52, 58, 255))

    # 头带
    draw.line((0, 11, 4, 12), fill=(52, 58, 64, 255), width=2)
    draw.line((19, 12, 23, 11), fill=(52, 58, 64, 255), width=2)

    return img


def save(image, relative_path, scale=None):
    path = os.path.join(MOD, relative_path)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    image.save(path)
    print("  %-52s %dx%d" % (relative_path, image.width, image.height))
    return image


# ======================================================================================
#  宇宙星空背景（玩家要求「背景都改为宇宙，但不受宇宙的重力改变影响」）
#
#  这些图放在模组根的 Backgrounds/ 目录下 —— tModLoader 只把"名字叫 Backgrounds 的
#  目录（含子目录）"里的贴图自动注册成背景贴图，代码里用
#  BackgroundTextureLoader.TryGetBackgroundSlot(Mod, "Backgrounds/xxx") 取槽位。
#
#  尺寸按原版地表背景的规格出：三层都是 1024x512；地下背景那 4 张按原版约定
#  160x16（两层交界）/ 160x96（层内）出。
# ======================================================================================

FAR_TOP = (6, 8, 18)
FAR_BOTTOM = (16, 14, 40)


def put_star(pixels, width, height, x, y, color, alpha, wrap=False):
    """点一颗星；wrap=True 时按上下左右环绕补画（给会平铺的地下背景用，接缝看不出来）。"""
    offsets = (-width, 0, width) if wrap else (0,)

    for ox in offsets:
        for oy in offsets:
            px = x + ox
            py = y + oy

            if 0 <= px < width and 0 <= py < height:
                r, g, b = color
                pixels[px, py] = (r, g, b, alpha)


def starfield(width, height, seed, star_count, opaque, nebula=0, bright=0, wrap=False):
    """程序化星空：可选的不透明深空底色 + 星云 + 星点 + 少量带光晕的亮星。"""
    rng = random.Random(seed)

    if opaque:
        img = Image.new("RGBA", (width, height), FAR_TOP + (255,))
        draw = ImageDraw.Draw(img)

        for y in range(height):
            t = y / float(max(height - 1, 1))
            color = (
                int(FAR_TOP[0] + (FAR_BOTTOM[0] - FAR_TOP[0]) * t),
                int(FAR_TOP[1] + (FAR_BOTTOM[1] - FAR_TOP[1]) * t),
                int(FAR_TOP[2] + (FAR_BOTTOM[2] - FAR_TOP[2]) * t),
                255,
            )
            draw.line((0, y, width, y), fill=color)
    else:
        img = Image.new("RGBA", (width, height), (0, 0, 0, 0))

    # 1) 星云：几张低透明度的色斑，叠在星点后面
    if nebula > 0:
        haze = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        haze_draw = ImageDraw.Draw(haze)

        for i in range(nebula):
            cx = rng.randint(0, width)
            cy = rng.randint(0, height)
            rx = rng.randint(width // 8, width // 3)
            ry = rng.randint(height // 8, height // 3)
            tints = ((96, 60, 168), (44, 96, 150), (150, 84, 96), (60, 130, 130))
            tint = tints[i % len(tints)]

            haze_draw.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=tint + (46,))

        haze = haze.filter(ImageFilter.GaussianBlur(24))
        img = Image.alpha_composite(img, haze)

    pixels = img.load()

    # 2) 星点：越亮越少
    tints = ((255, 255, 255), (206, 224, 255), (255, 232, 196), (196, 240, 255))

    for _ in range(star_count):
        x = rng.randint(0, width - 1)
        y = rng.randint(0, height - 1)
        alpha = rng.randint(90, 235)
        put_star(pixels, width, height, x, y, tints[rng.randrange(len(tints))], alpha, wrap)

    # 3) 亮星：带一圈光晕，视差一动就能看出深浅
    for _ in range(bright):
        x = rng.randint(0, width - 1)
        y = rng.randint(0, height - 1)
        base = rng.randint(3, 5)
        color = tints[rng.randrange(len(tints))]

        for step in range(base, 0, -1):
            alpha = int(255 * (1.0 - (step / float(base + 1))) * 0.55)
            put_star(pixels, width, height, x, y, color, alpha, wrap)

        for ox in (-1, 0, 1):
            for oy in (-1, 0, 1):
                put_star(pixels, width, height, x + ox, y + oy, color, 255, wrap)

    return img


def starfield_far():
    """最远层：不透明的深空（把原版天空底色整个盖住）+ 银河带。"""
    return starfield(1024, 512, seed=770101, star_count=520, opaque=True, nebula=3, bright=10)


def starfield_middle():
    """中间层：透明底，稀薄星云 + 中等亮度的星。"""
    return starfield(1024, 512, seed=770202, star_count=300, opaque=False, nebula=2, bright=6)


def starfield_close():
    """最近层：透明底，几颗大亮星，视差最大。"""
    return starfield(1024, 512, seed=770303, star_count=110, opaque=False, bright=14)


def underground_starfield(width, height, seed, star_count):
    """地下背景：不透明深空 + 星点（可平铺，接缝处看不出重复）。"""
    return starfield(width, height, seed=seed, star_count=star_count,
                     opaque=True, nebula=0, bright=1, wrap=True)


def main():
    print("生成壁炉环境贴图：")
    alloy = save(yugang_alloy(), os.path.join("Content", "Tiles", "YugangAlloy.png"))
    trim = save(yugang_trim(), os.path.join("Content", "Tiles", "YugangTrim.png"))
    gas = save(gas_poison_icon(), os.path.join("Content", "Buffs", "GasPoison.png"))
    mask = save(gas_mask_icon(), os.path.join("Content", "Items", "Accessories", "GasMask.png"))

    print("生成壁炉宇宙背景（Backgrounds/ 目录会被 tModLoader 自动注册成背景贴图）：")
    far = save(starfield_far(), os.path.join("Backgrounds", "FireplaceStarfieldFar.png"))
    middle = save(starfield_middle(), os.path.join("Backgrounds", "FireplaceStarfieldMiddle.png"))
    close = save(starfield_close(), os.path.join("Backgrounds", "FireplaceStarfieldClose.png"))
    ug0 = save(underground_starfield(160, 16, 880101, 26),
               os.path.join("Backgrounds", "FireplaceStarfieldUG0.png"))
    ug1 = save(underground_starfield(160, 96, 880202, 90),
               os.path.join("Backgrounds", "FireplaceStarfieldUG1.png"))
    ug2 = save(underground_starfield(160, 16, 880303, 26),
               os.path.join("Backgrounds", "FireplaceStarfieldUG2.png"))
    ug3 = save(underground_starfield(160, 96, 880404, 90),
               os.path.join("Backgrounds", "FireplaceStarfieldUG3.png"))

    # 预览拼图（放大 8 倍看像素）
    scale = 8
    tiles = Image.new("RGBA", (16 * 8 * 2 + 24, 16 * 8), (24, 26, 30, 255))
    tiles.paste(alloy.resize((128, 128), Image.NEAREST), (0, 0))
    tiles.paste(trim.resize((128, 128), Image.NEAREST), (136, 0))

    preview = Image.new("RGBA", (400, 320), (24, 26, 30, 255))
    preview.paste(tiles, (8, 8))
    preview.paste(gas.resize((160, 160), Image.NEAREST), (232, 30))
    preview.paste(mask.resize((120, 120), Image.NEAREST), (8, 92))

    # 背景缩略图：三层各来一张
    preview.paste(far.resize((120, 60)), (136, 92))
    preview.paste(middle.resize((120, 60)), (264, 92))
    preview.paste(close.resize((120, 60)), (136, 158))
    preview.paste(ug1.resize((120, 72)), (264, 158))
    preview_path = os.path.join(ROOT, "art-inbox", "preview_fireplace.png")
    os.makedirs(os.path.dirname(preview_path), exist_ok=True)
    preview.save(preview_path)
    print("预览: %s" % preview_path)


if __name__ == "__main__":
    main()
