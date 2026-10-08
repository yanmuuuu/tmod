# -*- coding: utf-8 -*-
# 武器扩充包（Rust 锈蚀 / Scrap 废铁）全部贴图。
#
# 纯 PIL 程序化生成，不依赖外部素材；生成尺寸必须和 C# 里的
#   Item.width / Item.height        （物品）
#   Projectile.width / Projectile.height（弹幕）
#   32x32                            （增益图标）
# 完全一致。改尺寸时两边一起改。
#
# 运行： python tools/gen_weaponpack_art.py
# 预览： art-inbox/preview_weaponpack.png

import math
import os
import random
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOD = os.path.join(ROOT, "WastelandSoul")
INBOX = os.path.join(ROOT, "art-inbox")

# ---------------------------------------------------------------- 配色
# RUST：锈蚀铁 + 木柄（前期，脏、暖、饱和度低）
RUST = {
    "base": (122, 74, 45),
    "dark": (72, 43, 27),
    "light": (176, 116, 72),
    "metal": (142, 142, 150),
    "metald": (84, 84, 92),
    "edge": (44, 28, 20),
    "energy": (222, 138, 62),
    "wood": (96, 66, 42),
    "woods": (60, 40, 26),
    "lens": (226, 158, 78),
}

# SCRAP：精钢 + 青绿能量（前期后段，冷、亮、有能量感）
SCRAP = {
    "base": (98, 108, 118),
    "dark": (50, 58, 68),
    "light": (158, 170, 182),
    "metal": (182, 192, 202),
    "metald": (104, 114, 126),
    "edge": (32, 40, 50),
    "energy": (86, 200, 186),
    "wood": (88, 62, 42),
    "woods": (56, 38, 24),
    "lens": (96, 224, 208),
}


# ---------------------------------------------------------------- 小工具
def mix(a, b, t):
    t = max(0.0, min(1.0, t))
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def shade(c, k):
    return tuple(max(0, min(255, int(round(c[i] * k)))) for i in range(3))


def blank(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def opaque(c):
    return (c[0], c[1], c[2], 255)


def dot(px, w, h, x, y, col, a=255):
    if 0 <= x < w and 0 <= y < h:
        px[int(x), int(y)] = (col[0], col[1], col[2], a)


def disc(px, w, h, cx, cy, r, col, a=255):
    r2 = r * r
    for y in range(int(cy - r) - 1, int(cy + r) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            if (x - cx) ** 2 + (y - cy) ** 2 <= r2:
                dot(px, w, h, x, y, col, a)


def ring(px, w, h, cx, cy, r, thick, col):
    for y in range(int(cy - r - thick) - 1, int(cy + r + thick) + 2):
        for x in range(int(cx - r - thick) - 1, int(cx + r + thick) + 2):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
            if r - thick <= d <= r:
                dot(px, w, h, x, y, col)


def erase_disc(px, w, h, cx, cy, r):
    r2 = r * r
    for y in range(int(cy - r) - 1, int(cy + r) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            if (x - cx) ** 2 + (y - cy) ** 2 <= r2 and 0 <= x < w and 0 <= y < h:
                px[int(x), int(y)] = (0, 0, 0, 0)


def bar(px, w, h, x0, y0, x1, y1, thick, col):
    # 任意方向的粗线段
    steps = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
    for i in range(steps + 1):
        t = i / steps
        cx = x0 + (x1 - x0) * t
        cy = y0 + (y1 - y0) * t
        disc(px, w, h, cx, cy, thick * 0.5, col)


def speckle(img, rng, dark, light, chance=0.16):
    # 金属噪点：只在已有像素上做深浅扰动
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            roll = rng.random()
            if roll < chance * 0.45:
                px[x, y] = (dark[0], dark[1], dark[2], a)
            elif roll < chance * 0.45 + chance * 0.35:
                px[x, y] = (light[0], light[1], light[2], a)
    return img


def outline(img, col):
    # 给不透明像素外侧补一圈描边（原版贴图的基本路数）
    px = img.load()
    w, h = img.size
    add = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] != 0:
                continue
            near = False
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] > 128:
                    near = True
                    break
            if near:
                add.append((x, y))
    for x, y in add:
        px[x, y] = opaque(col)
    return img


def orb_img(size, core, mid, rim, alpha_soft=True):
    # 径向渐变球：核心亮、边缘暗，最外圈羽化
    img = blank(size, size)
    px = img.load()
    c = (size - 1) / 2.0
    r = size / 2.0
    for y in range(size):
        for x in range(size):
            d = ((x - c) ** 2 + (y - c) ** 2) ** 0.5 / r
            if d > 1.0:
                continue
            if d < 0.3:
                col = core
            elif d < 0.62:
                col = mix(core, mid, (d - 0.3) / 0.32)
            else:
                col = mix(mid, rim, (d - 0.62) / 0.38)
            a = 255
            if alpha_soft and d > 0.85:
                a = int(255 * (1.0 - (d - 0.85) / 0.15))
            px[x, y] = (col[0], col[1], col[2], max(0, min(255, a)))
    return img


# ---------------------------------------------------------------- 形状：物品
def blade_item(w, h, rng, pal, wide=0.0, glow=None, guard=1.0):
    # 砍刀 / 巨剑：刀柄在左下、刀尖在右上（原版剑类贴图惯例）
    img = blank(w, h)
    px = img.load()
    x0, y0 = 2.0, h - 3.0
    x1, y1 = w - 2.0, 2.0
    steps = int((w + h) * 2)
    for i in range(steps + 1):
        t = i / steps
        cx = x0 + (x1 - x0) * t
        cy = y0 + (y1 - y0) * t
        if t < 0.20:
            # 刀柄
            hw = 1.5
            col = pal["wood"] if rng.random() > 0.4 else pal["woods"]
        elif t < 0.26:
            # 护手
            hw = 2.6 * guard + wide * 0.6
            col = pal["metald"]
        else:
            hw = (1.35 + 3.1 * (1.0 - t) ** 0.60) + wide * (1.0 - t * 0.6)
            if glow is not None and t > 0.3:
                hw += 1.0
        for k in range(-int(hw) - 1, int(hw) + 2):
            if abs(k) > hw:
                continue
            ox = int(round(cx + k * 0.7071))
            oy = int(round(cy + k * 0.7071))
            if t < 0.20:
                col = pal["wood"] if rng.random() > 0.35 else pal["woods"]
            elif t < 0.26:
                col = pal["metal"] if k < 0 else pal["metald"]
            else:
                if glow is not None and abs(k) > hw - 1.2:
                    col = glow
                elif k < -hw * 0.35:
                    col = pal["light"]
                elif k > hw * 0.35:
                    col = pal["dark"]
                else:
                    col = pal["metal"] if rng.random() > 0.35 else pal["base"]
            dot(px, w, h, ox, oy, col)
    # 刀尖加亮
    dot(px, w, h, x1, y1, pal["light"])
    dot(px, w, h, x1 - 1, y1, pal["light"])
    speckle(img, rng, pal["dark"], pal["light"], 0.20)
    return outline(img, pal["edge"])


def staff_item(w, h, rng, pal, orb_a, orb_b, orb_c, big=0.0):
    # 法杖 / 召唤杖：木杆 + 顶端能量球 + 金属箍
    img = blank(w, h)
    px = img.load()
    x0, y0 = 3.0, h - 3.0
    x1, y1 = w - 5.0, 6.0
    bar(px, w, h, x0, y0, x1, y1, 3.0, pal["wood"])
    bar(px, w, h, x0 + 1, y0, x1 + 0.5, y1 + 0.5, 1.0, shade(pal["wood"], 0.7))
    for t in (0.30, 0.58):
        cx = x0 + (x1 - x0) * t
        cy = y0 + (y1 - y0) * t
        disc(px, w, h, cx, cy, 2.0, pal["metald"])
        disc(px, w, h, cx, cy, 1.0, pal["metal"])
    r = min(w, h) * (0.20 + big * 0.05)
    orb = orb_img(int(r * 2) + 3, orb_a, orb_b, orb_c)
    disc(px, w, h, x1 + 1.0, y1 - 1.0, r + 1.6, pal["metald"])
    img.alpha_composite(orb, (int(x1 + 1 - orb.width / 2), int(y1 - 1 - orb.height / 2)))
    # 抓握能量球的金属爪
    for k in (-1, 1):
        bar(px, w, h, x1 + 1 + k * (r + 1), y1 - 1 + 1.5, x1 + 1 + k * (r - 0.5), y1 - 1 - 1.5, 1.2, pal["metal"])
    speckle(img, rng, pal["dark"], pal["light"], 0.22)
    return outline(img, pal["edge"])


def gun_item(w, h, rng, pal, kind):
    # 枪械：枪托在左、枪管朝右。kind = shotgun / nailgun / railgun
    img = blank(w, h)
    px = img.load()
    mid = int(h * 0.45)
    # 枪托（木）
    for y in range(mid - 3, mid + 4):
        for x in range(1, max(2, int(w * 0.22))):
            if abs(y - mid) <= 3 - (x * 3 // max(1, int(w * 0.22))):
                dot(px, w, h, x, y, pal["wood"] if (x + y) % 3 else pal["woods"])
    # 机匣
    if kind == "railgun":
        rx0, rx1 = int(w * 0.20), int(w * 0.42)
    elif kind == "nailgun":
        rx0, rx1 = int(w * 0.22), int(w * 0.50)
    else:
        rx0, rx1 = int(w * 0.20), int(w * 0.46)
    for y in range(mid - 4, mid + 4):
        for x in range(rx0, rx1):
            col = pal["metald"] if y > mid + 1 else pal["metal"]
            dot(px, w, h, x, y, col)
    # 握把
    for y in range(mid + 3, min(h, mid + 9)):
        for x in range(rx0, rx0 + max(2, int(w * 0.07))):
            dot(px, w, h, x, y, pal["woods"] if (x + y) % 4 else pal["wood"])
    # 枪管
    if kind == "shotgun":
        for off in (-3, 1):
            for y in range(mid + off, mid + off + 3):
                for x in range(rx1 - 2, w - 2):
                    col = pal["metal"] if y == mid + off else pal["metald"]
                    dot(px, w, h, x, y, col)
                dot(px, w, h, w - 2, mid + off + 1, pal["edge"])
    elif kind == "nailgun":
        for y in range(mid - 3, mid):
            for x in range(rx1 - 2, w - 2):
                dot(px, w, h, x, y, pal["metal"] if y == mid - 3 else pal["metald"])
        # 上方弹鼓
        disc(px, w, h, rx0 + 4, mid - 7, 4.0, pal["metald"])
        disc(px, w, h, rx0 + 4, mid - 7, 2.2, pal["base"])
    else:
        for y in range(mid - 2, mid + 2):
            for x in range(rx1 - 2, w - 2):
                dot(px, w, h, x, y, pal["metal"] if y == mid - 2 else pal["metald"])
        # 磁轨线圈 + 能量线
        for t in (0.55, 0.72, 0.89):
            cx = int(rx1 + (w - 2 - rx1) * t)
            for y in range(mid - 5, mid + 5):
                dot(px, w, h, cx, y, pal["metald"])
                dot(px, w, h, cx + 1, y, pal["base"])
        for x in range(rx1 + 2, w - 3):
            dot(px, w, h, x, mid, pal["energy"])
        # 顶部瞄具
        for x in range(int(w * 0.30), int(w * 0.44)):
            for y in range(mid - 9, mid - 5):
                dot(px, w, h, x, y, pal["metald"])
        dot(px, w, h, int(w * 0.37), mid - 7, pal["lens"])
    speckle(img, rng, pal["dark"], pal["light"], 0.18)
    return outline(img, pal["edge"])


def book_item(w, h, rng, pal, rune):
    img = blank(w, h)
    px = img.load()
    for y in range(3, h - 3):
        for x in range(2, w - 2):
            col = pal["base"] if (x + y) % 5 else pal["dark"]
            dot(px, w, h, x, y, col)
    # 书脊
    for y in range(3, h - 3):
        for x in range(2, 6):
            dot(px, w, h, x, y, pal["metald"] if x % 2 else pal["metal"])
    # 金属包角
    for x in range(2, w - 2):
        dot(px, w, h, x, 3, pal["metal"])
        dot(px, w, h, x, h - 4, pal["metald"])
    for y in range(3, h - 3):
        dot(px, w, h, w - 3, y, pal["metald"])
    # 封面符文
    cx, cy = w * 0.60, h * 0.50
    ring(px, w, h, cx, cy, min(w, h) * 0.24, 1.2, rune)
    disc(px, w, h, cx, cy, min(w, h) * 0.10, rune)
    disc(px, w, h, cx, cy, min(w, h) * 0.05, pal["light"])
    speckle(img, rng, pal["dark"], pal["light"], 0.16)
    return outline(img, pal["edge"])


def yoyo_item(w, h, rng, pal, hub):
    img = blank(w, h)
    px = img.load()
    cx, cy = w * 0.56, h * 0.44
    r = min(w, h) * 0.33
    disc(px, w, h, cx, cy, r, pal["metald"])
    disc(px, w, h, cx, cy, r * 0.78, pal["metal"])
    disc(px, w, h, cx, cy, r * 0.45, pal["base"])
    disc(px, w, h, cx, cy, r * 0.18, hub)
    ring(px, w, h, cx, cy, r, 1.2, pal["edge"])
    # 绳
    bar(px, w, h, 1.0, h - 2.0, cx - r * 0.9, cy + r * 0.4, 1.0, (196, 190, 176))
    speckle(img, rng, pal["dark"], pal["light"], 0.18)
    return outline(img, pal["edge"])


def shuriken_item(size, rng, pal, glow=None):
    # 四角手里剑：四个三角刃 + 中心毂（中间挖空）
    img = blank(size, size)
    px = img.load()
    d = ImageDraw.Draw(img)
    c = (size - 1) / 2.0
    arm = size * 0.46
    for ang in (0, 90, 180, 270):
        rad = ang * math.pi / 180.0
        dx, dy = math.cos(rad), math.sin(rad)
        pxx, pyy = -dy, dx
        tip = (c + dx * arm, c + dy * arm)
        b1 = (c + pxx * size * 0.14 - dx * size * 0.05, c + pyy * size * 0.14 - dy * size * 0.05)
        b2 = (c - pxx * size * 0.14 - dx * size * 0.05, c - pyy * size * 0.14 - dy * size * 0.05)
        d.polygon([tip, b1, b2], fill=opaque(pal["metal"]))
        m1 = ((tip[0] + b1[0]) / 2.0, (tip[1] + b1[1]) / 2.0)
        m2 = ((tip[0] + b2[0]) / 2.0, (tip[1] + b2[1]) / 2.0)
        d.polygon([tip, m1, m2], fill=opaque(pal["base"]))
        if glow is not None:
            d.line([tip, m1, m2, tip], fill=opaque(glow), width=1)
    disc(px, size, size, c, c, size * 0.17, pal["metald"])
    disc(px, size, size, c, c, size * 0.10, pal["metal"])
    erase_disc(px, size, size, c, c, size * 0.06)
    speckle(img, rng, pal["dark"], pal["light"], 0.20)
    return img


def chakram_item(size, rng, pal, glow=None):
    img = blank(size, size)
    px = img.load()
    c = (size - 1) / 2.0
    r = size * 0.44
    ring(px, size, size, c, c, r, size * 0.13, pal["metal"])
    ring(px, size, size, c, c, r, 1.2, pal["edge"])
    ring(px, size, size, c, c, r * 0.80, 1.1, pal["metald"])
    # 四片外刃
    for ang in (30, 120, 210, 300):
        rad = ang * math.pi / 180.0
        dx, dy = math.cos(rad), math.sin(rad)
        for t in range(0, int(size * 0.30)):
            x = c + dx * (r + t * 0.8)
            y = c + dy * (r + t * 0.8)
            wdt = max(0.6, 1.8 - t * 0.05)
            col = glow if (glow is not None and t > size * 0.16) else pal["metald"]
            disc(px, size, size, x, y, wdt, col)
    # 中心毂
    disc(px, size, size, c, c, size * 0.16, pal["base"])
    disc(px, size, size, c, c, size * 0.08, glow if glow is not None else pal["metald"])
    erase_disc(px, size, size, c, c, size * 0.05)
    speckle(img, rng, pal["dark"], pal["light"], 0.18)
    return img


def saw_item(size, rng, pal, glow=None):
    img = blank(size, size)
    px = img.load()
    c = (size - 1) / 2.0
    r = size * 0.36
    # 锯齿
    for i in range(0, 360, 18):
        rad = i * 3.14159 / 180.0
        for t in range(0, int(size * 0.18)):
            x = c + math.cos(rad) * (r + t * 0.7)
            y = c + math.sin(rad) * (r + t * 0.7)
            disc(px, size, size, x, y, max(0.6, 1.6 - t * 0.06), pal["metal"])
        rad2 = (i + 9) * 3.14159 / 180.0
        for t in range(0, int(size * 0.10)):
            x = c + math.cos(rad2) * (r + t * 0.7)
            y = c + math.sin(rad2) * (r + t * 0.7)
            disc(px, size, size, x, y, 1.0, pal["metald"])
    disc(px, size, size, c, c, r, pal["metal"])
    disc(px, size, size, c, c, r * 0.72, pal["base"])
    ring(px, size, size, c, c, r, 1.1, pal["edge"])
    for i in range(0, 360, 45):
        rad = i * 3.14159 / 180.0
        for t in range(0, int(r * 0.6)):
            dot(px, size, size, int(c + math.cos(rad) * t), int(c + math.sin(rad) * t), pal["metald"])
    disc(px, size, size, c, c, size * 0.10, pal["metald"])
    disc(px, size, size, c, c, size * 0.05, glow if glow is not None else pal["edge"])
    speckle(img, rng, pal["dark"], pal["light"], 0.20)
    return img


def grenade_item(size, rng, pal, liquid):
    img = blank(size, size)
    px = img.load()
    c = (size - 1) / 2.0
    r = size * 0.36
    disc(px, size, size, c, c + 1, r, pal["metald"])
    disc(px, size, size, c - 1, c, r * 0.75, pal["metal"])
    # 裂纹 + 液体窗
    for i in range(0, size, 3):
        dot(px, size, size, c - r * 0.4 + i * 0.3, c - r * 0.5 + (i % 4), pal["dark"])
    disc(px, size, size, c + r * 0.35, c - r * 0.25, r * 0.22, liquid)
    # 顶盖 + 拉环
    for x in range(int(c - 3), int(c + 4)):
        dot(px, size, size, x, int(c - r), pal["metald"])
        dot(px, size, size, x, int(c - r) + 1, pal["metal"])
    bar(px, size, size, c + 2, c - r - 1, c + 5, c - r - 4, 1.2, pal["metal"])
    ring(px, size, size, c + 5, c - r - 5, 2.0, 1.0, pal["metal"])
    speckle(img, rng, pal["dark"], pal["light"], 0.18)
    return outline(img, pal["edge"])


# ---------------------------------------------------------------- 形状：弹幕
def shard_proj(size, rng, pal, glow=None):
    img = blank(size, size)
    px = img.load()
    c = size * 0.5
    import math
    pts = []
    n = 6
    for i in range(n):
        ang = i * 360.0 / n + rng.uniform(-14, 14)
        rad = ang * 3.14159 / 180.0
        rr = c * (0.95 if i % 2 == 0 else 0.55)
        pts.append((c + math.cos(rad) * rr, c + math.sin(rad) * rr))
    d = ImageDraw.Draw(img)
    d.polygon(pts, fill=opaque(pal["metal"]))
    d.polygon([(c, c)] + pts[:3], fill=opaque(pal["base"]))
    if glow is not None:
        d.line(pts + [pts[0]], fill=opaque(glow), width=1)
    speckle(img, rng, pal["dark"], pal["light"], 0.22)
    return img


def droplet_proj(size, rng, col, hi, glow=None):
    img = blank(size, size)
    px = img.load()
    c = (size - 1) / 2.0
    import math
    for y in range(size):
        for x in range(size):
            dx = (x - c) / (size * 0.5)
            dy = (y - c) / (size * 0.5)
            d = (dx * dx + dy * dy) ** 0.5
            # 上尖下圆的水滴
            tail = max(0.0, -dy) * 0.75
            if d + tail * 0.9 <= 1.0:
                col2 = mix(col, hi, max(0.0, 1.0 - d * 1.2))
                a = 255 if d + tail * 0.9 < 0.82 else int(255 * (1.0 - (d + tail * 0.9 - 0.82) / 0.18))
                px[x, y] = (col2[0], col2[1], col2[2], max(0, min(255, a)))
    if glow is not None:
        dot(px, size, size, c, c, glow)
        dot(px, size, size, c - 1, c + 1, glow)
    return img


def pellet_proj(size, rng, col, hi):
    img = blank(size, size)
    px = img.load()
    c = (size - 1) / 2.0
    disc(px, size, size, c, c, size * 0.46, col)
    disc(px, size, size, c - size * 0.14, c - size * 0.14, size * 0.22, hi)
    return img


def nail_proj(w, h, rng, pal):
    img = blank(w, h)
    px = img.load()
    for y in range(h):
        for x in range(1, w - 1):
            dot(px, w, h, x, y, pal["metal"] if x > 1 else pal["metald"])
    for x in range(0, w):
        dot(px, w, h, x, 0, pal["metal"])
        dot(px, w, h, x, 1, pal["light"])
    speckle(img, rng, pal["dark"], pal["light"], 0.16)
    return img


def slug_proj(w, h, rng, pal, glow):
    img = blank(w, h)
    px = img.load()
    for x in range(w):
        t = x / (w - 1.0)
        hh = h * 0.5 * (1.0 - 0.55 * t * t)
        for y in range(h):
            if abs(y - (h - 1) / 2.0) <= hh:
                col = pal["metal"] if abs(y - (h - 1) / 2.0) < hh * 0.4 else pal["metald"]
                dot(px, w, h, x, y, col)
    for x in range(2, w - 2):
        dot(px, w, h, x, int((h - 1) / 2.0), glow)
    dot(px, w, h, w - 1, int((h - 1) / 2.0), pal["light"])
    speckle(img, rng, pal["dark"], pal["light"], 0.14)
    return img


def wave_proj(w, h, rng, pal, energy):
    # 新月形冲击波：大椭圆挖掉一个上移的椭圆
    mask = Image.new("L", (w, h), 0)
    md = ImageDraw.Draw(mask)
    md.ellipse((-w * 0.25, -h * 0.55, w * 0.75, h * 1.35), fill=255)
    md.ellipse((-w * 0.10, -h * 1.45, w * 1.10, h * 0.72), fill=0)
    img = blank(w, h)
    px = img.load()
    for y in range(h):
        for x in range(w):
            if mask.getpixel((x, y)) == 0:
                continue
            t = y / (h - 1.0)
            col = mix(pal["light"], pal["base"], t)
            if t > 0.72:
                col = energy
            px[x, y] = opaque(col)
    speckle(img, rng, pal["dark"], pal["light"], 0.14)
    return img


def drone_proj(w, h, rng, pal, eye):
    img = blank(w, h)
    px = img.load()
    cx, cy = w * 0.5, h * 0.52
    # 机体
    for y in range(int(cy - h * 0.22), int(cy + h * 0.22)):
        for x in range(int(cx - w * 0.22), int(cx + w * 0.22)):
            col = pal["metal"] if y < cy else pal["base"]
            if (x + y) % 7 == 0:
                col = pal["dark"]
            dot(px, w, h, x, y, col)
    # 两侧旋翼
    for k in (-1, 1):
        disc(px, w, h, cx + k * w * 0.34, cy - h * 0.08, w * 0.11, pal["metald"])
        disc(px, w, h, cx + k * w * 0.34, cy - h * 0.08, w * 0.05, pal["metal"])
        bar(px, w, h, cx + k * w * 0.22, cy - h * 0.05, cx + k * w * 0.34, cy - h * 0.08, 1.2, pal["metald"])
    # 眼 + 天线 + 挂架
    disc(px, w, h, cx, cy + h * 0.02, w * 0.07, eye)
    bar(px, w, h, cx, cy - h * 0.22, cx + w * 0.06, cy - h * 0.40, 1.0, pal["metald"])
    dot(px, w, h, cx + w * 0.06, cy - h * 0.42, eye)
    for k in (-1, 1):
        bar(px, w, h, cx + k * w * 0.12, cy + h * 0.20, cx + k * w * 0.18, cy + h * 0.36, 1.0, pal["metald"])
    speckle(img, rng, pal["dark"], pal["light"], 0.20)
    return outline(img, pal["edge"])


def spitter_proj(w, h, rng, pal, eye, acid):
    img = blank(w, h)
    px = img.load()
    cx, cy = w * 0.48, h * 0.56
    # 机体
    for y in range(int(cy - h * 0.26), int(cy + h * 0.20)):
        for x in range(int(cx - w * 0.28), int(cx + w * 0.24)):
            col = pal["metal"] if y < cy else pal["base"]
            if (x * 2 + y) % 9 == 0:
                col = pal["dark"]
            dot(px, w, h, x, y, col)
    # 头顶储液罐
    for y in range(int(cy - h * 0.40), int(cy - h * 0.24)):
        for x in range(int(cx - w * 0.16), int(cx + w * 0.14)):
            dot(px, w, h, x, y, pal["metald"])
    for y in range(int(cy - h * 0.37), int(cy - h * 0.27)):
        for x in range(int(cx - w * 0.11), int(cx + w * 0.09)):
            dot(px, w, h, x, y, acid)
    # 喷嘴
    for y in range(int(cy - h * 0.06), int(cy + h * 0.06)):
        for x in range(int(cx + w * 0.22), int(cx + w * 0.42)):
            dot(px, w, h, x, y, pal["metal"] if y % 2 else pal["metald"])
    dot(px, w, h, cx + w * 0.42, cy, acid)
    # 眼 + 腿
    disc(px, w, h, cx - w * 0.10, cy - h * 0.06, w * 0.07, eye)
    for k in (-1, 1):
        bar(px, w, h, cx + k * w * 0.14, cy + h * 0.18, cx + k * w * 0.26, cy + h * 0.40, 1.2, pal["metald"])
    speckle(img, rng, pal["dark"], pal["light"], 0.20)
    return outline(img, pal["edge"])


def warden_proj(w, h, rng, pal, eye, energy):
    img = blank(w, h)
    px = img.load()
    cx, cy = w * 0.50, h * 0.55
    # 躯干
    for y in range(int(cy - h * 0.28), int(cy + h * 0.24)):
        for x in range(int(cx - w * 0.26), int(cx + w * 0.26)):
            col = pal["metal"] if y < cy else pal["base"]
            if (x + y * 2) % 11 == 0:
                col = pal["dark"]
            dot(px, w, h, x, y, col)
    # 肩甲
    for k in (-1, 1):
        for y in range(int(cy - h * 0.32), int(cy - h * 0.16)):
            for x in range(int(cx + k * w * 0.18), int(cx + k * w * 0.34)):
                dot(px, w, h, x, y, pal["metald"])
    # 面罩
    for y in range(int(cy - h * 0.20), int(cy - h * 0.08)):
        for x in range(int(cx - w * 0.14), int(cx + w * 0.14)):
            dot(px, w, h, x, y, pal["edge"])
    for x in range(int(cx - w * 0.10), int(cx + w * 0.10)):
        dot(px, w, h, x, int(cy - h * 0.14), eye)
    # 左盾 / 右炮
    for y in range(int(cy - h * 0.22), int(cy + h * 0.22)):
        for x in range(int(cx - w * 0.44), int(cx - w * 0.28)):
            dot(px, w, h, x, y, pal["metald"] if y % 3 else pal["metal"])
    ring(px, w, h, cx - w * 0.36, cy, w * 0.10, 1.2, energy)
    for y in range(int(cy - h * 0.08), int(cy + h * 0.08)):
        for x in range(int(cx + w * 0.26), int(cx + w * 0.52)):
            dot(px, w, h, x, y, pal["metal"] if y % 2 else pal["metald"])
    dot(px, w, h, cx + w * 0.52, cy, energy)
    speckle(img, rng, pal["dark"], pal["light"], 0.20)
    return outline(img, pal["edge"])


def blast_proj(size, rng, inner, outer):
    img = blank(size, size)
    c = (size - 1) / 2.0
    for r, a in ((0.30, 110), (0.40, 80), (0.49, 50)):
        layer = blank(size, size)
        lp = layer.load()
        rr = size * r
        for y in range(size):
            for x in range(size):
                d = ((x - c) ** 2 + (y - c) ** 2) ** 0.5
                if abs(d - rr) < size * 0.055:
                    lp[x, y] = (outer[0], outer[1], outer[2], a)
        img.alpha_composite(layer)
    px = img.load()
    for _ in range(int(size * 0.8)):
        ang = rng.uniform(0, 2.0 * math.pi)
        rad = rng.uniform(size * 0.10, size * 0.46)
        x = int(c + math.cos(ang) * rad)
        y = int(c + math.sin(ang) * rad)
        dot(px, size, size, x, y, inner)
    return img


# ---------------------------------------------------------------- 增益图标
def buff_icon(size, kind, bg, fg, accent):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    c = (size - 1) / 2.0
    # 底：圆形徽章
    disc(px, size, size, c, c, size * 0.48, bg)
    ring(px, size, size, c, c, size * 0.48, 1.6, fg)
    ring(px, size, size, c, c, size * 0.40, 0.8, shade(fg, 0.6))

    if kind == "drone":
        small = drone_proj(int(size * 0.74), int(size * 0.74), random.Random(1),
                           {"metal": fg, "metald": shade(fg, 0.7), "base": shade(fg, 0.85),
                            "dark": shade(fg, 0.5), "light": (240, 240, 240), "edge": (0, 0, 0)}, accent)
        img.alpha_composite(small, (int((size - small.width) / 2), int((size - small.height) / 2) - 1))
        for k in (-1, 1):
            for i in range(size // 4):
                dot(px, size, size, c + k * (size * 0.40 + i * 0.15), c - size * 0.18 + i * 0.6, accent)
    elif kind == "spitter":
        d = droplet_proj(int(size * 0.52), random.Random(2), fg, accent)
        img.alpha_composite(d, (int((size - d.width) / 2) - 3, int((size - d.height) / 2) - 1))
        for i in range(3):
            disc(px, size, size, size * 0.70 + i * 1.6, c - size * 0.10 + i * size * 0.11, size * 0.055, accent)
    elif kind == "spitterex":
        d = droplet_proj(int(size * 0.52), random.Random(2), fg, accent)
        img.alpha_composite(d, (int((size - d.width) / 2) - 4, int((size - d.height) / 2) - 1))
        # 闪电
        pts = [(0.70, 0.20), (0.62, 0.46), (0.70, 0.46), (0.58, 0.80), (0.74, 0.48), (0.66, 0.48)]
        d2 = ImageDraw.Draw(img)
        d2.line([(size * a, size * b) for a, b in pts], fill=opaque(accent), width=2)
    else:  # warden
        d = ImageDraw.Draw(img)
        d.polygon([(size * 0.50, size * 0.16), (size * 0.82, size * 0.30),
                   (size * 0.82, size * 0.56), (size * 0.50, size * 0.84),
                   (size * 0.18, size * 0.56), (size * 0.18, size * 0.30)],
                  fill=opaque(fg), outline=opaque(shade(fg, 0.55)))
        d.line([(size * 0.50, size * 0.26), (size * 0.50, size * 0.74)], fill=opaque(accent), width=2)
        d.line([(size * 0.26, size * 0.46), (size * 0.74, size * 0.46)], fill=opaque(accent), width=2)
        disc(px, size, size, c, c, size * 0.10, accent)
    return img


# ---------------------------------------------------------------- 输出
def save(img, rel):
    path = os.path.join(MOD, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print("  %-58s %dx%d" % (rel.replace("\\", "/"), img.width, img.height))
    return img


def main():
    rng_root = random.Random(20260614)
    made = []   # (label, image)

    rust_i = os.path.join("Content", "Items", "Weapons", "Rust")
    scrap_i = os.path.join("Content", "Items", "Weapons", "Scrap")
    rust_p = os.path.join("Content", "Projectiles", "Rust")
    scrap_p = os.path.join("Content", "Projectiles", "Scrap")
    buffs = os.path.join("Content", "Buffs")

    print("== Rust 线物品 ==")
    made.append(("RustCleaver", save(blade_item(34, 34, random.Random(1), RUST), os.path.join(rust_i, "RustCleaver.png"))))
    made.append(("ScrapYoyo", save(yoyo_item(30, 30, random.Random(2), RUST, RUST["energy"]), os.path.join(rust_i, "ScrapYoyo.png"))))
    made.append(("RustCleaverEX", save(blade_item(36, 36, random.Random(3), RUST, wide=0.6, glow=RUST["energy"]), os.path.join(rust_i, "RustCleaverEX.png"))))
    made.append(("RustBoltWand", save(staff_item(32, 32, random.Random(4), RUST, (236, 226, 190), (208, 150, 84), (110, 62, 34)), os.path.join(rust_i, "RustBoltWand.png"))))
    made.append(("RustAcidTome", save(book_item(28, 28, random.Random(5), RUST, (150, 208, 96)), os.path.join(rust_i, "RustAcidTome.png"))))
    made.append(("RustBoltWandEX", save(staff_item(34, 34, random.Random(6), RUST, (255, 244, 208), RUST["energy"], (120, 58, 26), big=1.0), os.path.join(rust_i, "RustBoltWandEX.png"))))
    made.append(("ScrapShotgun", save(gun_item(44, 18, random.Random(7), RUST, "shotgun"), os.path.join(rust_i, "ScrapShotgun.png"))))
    made.append(("RustNailgun", save(gun_item(36, 18, random.Random(8), RUST, "nailgun"), os.path.join(rust_i, "RustNailgun.png"))))
    made.append(("ScrapShotgunEX", save(gun_item(46, 20, random.Random(9), RUST, "shotgun"), os.path.join(rust_i, "ScrapShotgunEX.png"))))
    made.append(("RustDroneStaff", save(staff_item(36, 36, random.Random(10), RUST, (255, 226, 176), (232, 150, 70), (118, 60, 30)), os.path.join(rust_i, "RustDroneStaff.png"))))
    made.append(("RustSpitterStaff", save(staff_item(34, 34, random.Random(11), RUST, (206, 240, 168), (132, 196, 88), (58, 96, 40)), os.path.join(rust_i, "RustSpitterStaff.png"))))
    made.append(("RustSpitterStaffEX", save(staff_item(38, 38, random.Random(12), RUST, (232, 255, 196), (146, 226, 84), (56, 104, 36), big=1.0), os.path.join(rust_i, "RustSpitterStaffEX.png"))))
    made.append(("RustShuriken", save(shuriken_item(18, random.Random(13), RUST), os.path.join(rust_i, "RustShuriken.png"))))
    made.append(("ScrapGrenade", save(grenade_item(18, random.Random(14), RUST, (168, 216, 96)), os.path.join(rust_i, "ScrapGrenade.png"))))
    made.append(("RustShurikenEX", save(shuriken_item(20, random.Random(15), RUST, glow=RUST["energy"]), os.path.join(rust_i, "RustShurikenEX.png"))))

    print("== Scrap 线物品 ==")
    made.append(("ScrapGreatsword", save(blade_item(46, 46, random.Random(21), SCRAP, wide=2.2, guard=1.4), os.path.join(scrap_i, "ScrapGreatsword.png"))))
    made.append(("RebarBoomerang", save(chakram_item(30, random.Random(22), SCRAP, glow=SCRAP["light"]), os.path.join(scrap_i, "RebarBoomerang.png"))))
    made.append(("ScrapNovaStaff", save(staff_item(34, 34, random.Random(23), SCRAP, (232, 214, 255), (150, 108, 226), (60, 40, 110)), os.path.join(scrap_i, "ScrapNovaStaff.png"))))
    made.append(("ScrapNovaStaffEX", save(staff_item(36, 36, random.Random(24), SCRAP, (250, 240, 255), (176, 122, 255), (68, 40, 124), big=1.0), os.path.join(scrap_i, "ScrapNovaStaffEX.png"))))
    made.append(("ScrapRailgun", save(gun_item(48, 22, random.Random(25), SCRAP, "railgun"), os.path.join(scrap_i, "ScrapRailgun.png"))))
    made.append(("ScrapRailgunEX", save(gun_item(50, 24, random.Random(26), SCRAP, "railgun"), os.path.join(scrap_i, "ScrapRailgunEX.png"))))
    made.append(("ScrapWardenStaff", save(staff_item(38, 38, random.Random(27), SCRAP, (208, 250, 244), (96, 208, 194), (34, 84, 82)), os.path.join(scrap_i, "ScrapWardenStaff.png"))))
    made.append(("ScrapBuzzsaw", save(saw_item(24, random.Random(28), SCRAP), os.path.join(scrap_i, "ScrapBuzzsaw.png"))))
    made.append(("ScrapChakram", save(chakram_item(22, random.Random(29), SCRAP, glow=SCRAP["energy"]), os.path.join(scrap_i, "ScrapChakram.png"))))

    print("== Rust 线弹幕 ==")
    made.append(("RustShardSlash", save(shard_proj(18, random.Random(41), RUST), os.path.join(rust_p, "RustShardSlash.png"))))
    made.append(("RustShardSlashEX", save(shard_proj(20, random.Random(42), RUST, glow=RUST["energy"]), os.path.join(rust_p, "RustShardSlashEX.png"))))
    made.append(("ScrapYoyoProjectile", save(yoyo_item(20, 20, random.Random(43), RUST, RUST["energy"]), os.path.join(rust_p, "ScrapYoyoProjectile.png"))))
    made.append(("RustBolt", save(orb_img(14, (255, 246, 214), RUST["energy"], (120, 58, 30)), os.path.join(rust_p, "RustBolt.png"))))
    made.append(("RustBoltEX", save(orb_img(16, (255, 252, 236), (250, 176, 88), (140, 62, 24)), os.path.join(rust_p, "RustBoltEX.png"))))
    made.append(("RustAcidSpray", save(droplet_proj(12, random.Random(44), (154, 206, 92), (226, 250, 176)), os.path.join(rust_p, "RustAcidSpray.png"))))
    made.append(("ScrapPellet", save(pellet_proj(10, random.Random(45), RUST["metald"], RUST["light"]), os.path.join(rust_p, "ScrapPellet.png"))))
    made.append(("ScrapPelletEX", save(pellet_proj(10, random.Random(46), (150, 170, 178), (226, 244, 248)), os.path.join(rust_p, "ScrapPelletEX.png"))))
    made.append(("RustNail", save(nail_proj(8, 8, random.Random(47), RUST), os.path.join(rust_p, "RustNail.png"))))
    made.append(("RustDrone", save(drone_proj(28, 28, random.Random(48), RUST, (255, 96, 72)), os.path.join(rust_p, "RustDrone.png"))))
    made.append(("RustSpitter", save(spitter_proj(30, 30, random.Random(49), RUST, (255, 150, 72), (168, 220, 104)), os.path.join(rust_p, "RustSpitter.png"))))
    made.append(("RustSpitterEX", save(spitter_proj(34, 34, random.Random(50), RUST, (255, 178, 88), (196, 244, 120)), os.path.join(rust_p, "RustSpitterEX.png"))))
    made.append(("RustSpitterShot", save(droplet_proj(12, random.Random(51), (152, 202, 92), (228, 250, 178)), os.path.join(rust_p, "RustSpitterShot.png"))))
    made.append(("RustSpitterShotEX", save(droplet_proj(14, random.Random(52), (170, 224, 96), (240, 255, 200), glow=(255, 255, 220)), os.path.join(rust_p, "RustSpitterShotEX.png"))))
    made.append(("RustShurikenProj", save(shuriken_item(16, random.Random(53), RUST), os.path.join(rust_p, "RustShurikenProj.png"))))
    made.append(("RustShurikenProjEX", save(shuriken_item(18, random.Random(54), RUST, glow=RUST["energy"]), os.path.join(rust_p, "RustShurikenProjEX.png"))))
    made.append(("ScrapGrenadeProj", save(grenade_item(16, random.Random(55), RUST, (168, 216, 96)), os.path.join(rust_p, "ScrapGrenadeProj.png"))))
    made.append(("ScrapGrenadeBlast", save(blast_proj(60, random.Random(56), (236, 208, 150), (208, 146, 76)), os.path.join(rust_p, "ScrapGrenadeBlast.png"))))

    print("== Scrap 线弹幕 ==")
    made.append(("ScrapShockwave", save(wave_proj(40, 24, random.Random(61), SCRAP, SCRAP["energy"]), os.path.join(scrap_p, "ScrapShockwave.png"))))
    made.append(("RebarBoomerangProj", save(chakram_item(22, random.Random(62), SCRAP, glow=SCRAP["light"]), os.path.join(scrap_p, "RebarBoomerangProj.png"))))
    made.append(("ScrapNova", save(orb_img(24, (246, 236, 255), (152, 108, 230), (52, 34, 100)), os.path.join(scrap_p, "ScrapNova.png"))))
    made.append(("ScrapNovaEX", save(orb_img(28, (255, 250, 255), (176, 120, 255), (60, 34, 116)), os.path.join(scrap_p, "ScrapNovaEX.png"))))
    made.append(("ScrapNovaFragment", save(shard_proj(12, random.Random(63), SCRAP, glow=(198, 150, 255)), os.path.join(scrap_p, "ScrapNovaFragment.png"))))
    made.append(("ScrapRailSlug", save(slug_proj(14, 8, random.Random(64), SCRAP, SCRAP["energy"]), os.path.join(scrap_p, "ScrapRailSlug.png"))))
    made.append(("ScrapRailSlugEX", save(slug_proj(16, 10, random.Random(65), SCRAP, (232, 255, 250)), os.path.join(scrap_p, "ScrapRailSlugEX.png"))))
    made.append(("ScrapWarden", save(warden_proj(34, 34, random.Random(66), SCRAP, (255, 120, 96), SCRAP["energy"]), os.path.join(scrap_p, "ScrapWarden.png"))))
    made.append(("ScrapWardenShot", save(slug_proj(14, 14, random.Random(67), SCRAP, SCRAP["energy"]), os.path.join(scrap_p, "ScrapWardenShot.png"))))
    made.append(("ScrapBuzzsawProj", save(saw_item(24, random.Random(68), SCRAP), os.path.join(scrap_p, "ScrapBuzzsawProj.png"))))
    made.append(("ScrapChakramProj", save(chakram_item(22, random.Random(69), SCRAP, glow=SCRAP["energy"]), os.path.join(scrap_p, "ScrapChakramProj.png"))))

    print("== 增益图标 ==")
    # 注意：仆从维持 Buff 的类写在 Content/Projectiles/Rust|Scrap 里，
    # 所以图标必须和类同目录（tML 的贴图路径 = 类的命名空间路径）。
    made.append(("RustDroneBuff", save(buff_icon(32, "drone", (58, 46, 34), (168, 156, 140), (240, 150, 70)), os.path.join(rust_p, "RustDroneBuff.png"))))
    made.append(("RustSpitterBuff", save(buff_icon(32, "spitter", (44, 56, 36), (150, 172, 138), (168, 220, 104)), os.path.join(rust_p, "RustSpitterBuff.png"))))
    made.append(("RustSpitterBuffEX", save(buff_icon(32, "spitterex", (40, 58, 38), (168, 194, 150), (200, 248, 120)), os.path.join(rust_p, "RustSpitterBuffEX.png"))))
    made.append(("ScrapWardenBuff", save(buff_icon(32, "warden", (34, 50, 54), (170, 190, 196), (96, 214, 198)), os.path.join(scrap_p, "ScrapWardenBuff.png"))))

    # 预览拼图（每格放大到 96 高，方便肉眼查像素）
    cols = 10
    cell = 104
    rows = (len(made) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell + 8, rows * cell + 8), (22, 24, 28, 255))
    for i, (label, img) in enumerate(made):
        scale = max(2, int(cell * 0.78 / max(img.width, img.height)))
        thumb = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
        ox = 4 + (i % cols) * cell + (cell - thumb.width) // 2
        oy = 4 + (i // cols) * cell + (cell - thumb.height) // 2
        sheet.alpha_composite(thumb, (ox, oy))
    os.makedirs(INBOX, exist_ok=True)
    preview = os.path.join(INBOX, "preview_weaponpack.png")
    sheet.save(preview)
    print("共 %d 张，预览： %s" % (len(made), preview))


if __name__ == "__main__":
    main()
