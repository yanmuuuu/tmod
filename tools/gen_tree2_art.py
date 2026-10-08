# -*- coding: utf-8 -*-
# 升级衍生树（第二批）的程序化贴图生成：召唤齿轮系 / 盗贼回旋系 / 拾荒者芯片饰品系。
#
# 画法：先在 SS 倍的画布上画，最后 LANCZOS 缩回目标尺寸 —— 等于自带抗锯齿，
#       线宽与圆边都不会是硬锯齿。
#
# 尺寸约定（必须与代码里的 Item.width/height、Projectile.width/height 一致）：
#   召唤/盗贼物品图标   24x24 ~ 44x44
#   饰品图标            24x24
#   仆从弹幕            28x28 ~ 32x32
#   子弹 / 余烬         12x12 ~ 14x14
#   爆裂判定框          44x44
#   Buff 图标           32x32
#
# 本脚本只写自己新增的文件，不删除、不覆盖别人生成的美术资源。

import math
import os

from PIL import Image, ImageDraw

ROOT = r'E:\开发\WastelandSoul'
ITEM_DIR = os.path.join(ROOT, r'Content\Items\UpgradeTrees')
PROJ_DIR = os.path.join(ROOT, r'Content\Projectiles\UpgradeTrees')
INBOX = r'E:\开发\art-inbox'

SS = 4

# 调色板：(描边, 主色, 亮色, 点缀色)
RUST = ((44, 30, 22, 255), (124, 78, 46, 255), (186, 126, 74, 255), (226, 158, 92, 255))
STEEL = ((26, 32, 40, 255), (110, 122, 138, 255), (196, 210, 226, 255), (104, 172, 220, 255))
ASH = ((46, 24, 16, 255), (146, 82, 50, 255), (228, 164, 106, 255), (255, 146, 56, 255))
PCB = ((18, 36, 26, 255), (52, 112, 74, 255), (132, 200, 144, 255), (238, 212, 118, 255))
GOLD = ((48, 34, 12, 255), (170, 128, 42, 255), (244, 214, 124, 255), (255, 170, 62, 255))
CLEAR = (0, 0, 0, 0)

CREATED = []


def save(image, path):
    folder = os.path.dirname(path)
    if folder:
        os.makedirs(folder, exist_ok=True)
    image.save(path)
    CREATED.append((path, image.size))
    print('wrote %-58s %dx%d' % (os.path.relpath(path, ROOT), image.size[0], image.size[1]))


def polar(cx, cy, radius, degrees):
    angle = math.radians(degrees)
    return (cx + math.cos(angle) * radius, cy + math.sin(angle) * radius)


class Pen(object):
    '''小画板：所有坐标都按"最终像素"给，内部自动乘 SS 超采样。'''

    def __init__(self, width, height):
        self.width = width
        self.height = height
        self.image = Image.new('RGBA', (width * SS, height * SS), CLEAR)
        self.draw = ImageDraw.Draw(self.image)

    def _box(self, x0, y0, x1, y1):
        return [x0 * SS, y0 * SS, x1 * SS, y1 * SS]

    def _width(self, width):
        return max(1, int(round(width * SS)))

    def ellipse(self, cx, cy, rx, ry, fill=None, outline=None, width=1):
        self.draw.ellipse(self._box(cx - rx, cy - ry, cx + rx, cy + ry),
                          fill=fill, outline=outline, width=self._width(width))

    def rect(self, x0, y0, x1, y1, fill=None, outline=None, width=1):
        self.draw.rectangle(self._box(x0, y0, x1, y1),
                            fill=fill, outline=outline, width=self._width(width))

    def polygon(self, points, fill=None, outline=None, width=1):
        pts = [(x * SS, y * SS) for x, y in points]
        self.draw.polygon(pts, fill=fill)

        if outline is not None:
            self.draw.line(pts + [pts[0]], fill=outline, width=self._width(width), joint='curve')

    def line(self, points, fill, width=1):
        pts = [(x * SS, y * SS) for x, y in points]
        self.draw.line(pts, fill=fill, width=self._width(width), joint='curve')

    def dot(self, x, y, radius, fill):
        self.ellipse(x, y, radius, radius, fill=fill)

    def erase(self, cx, cy, rx, ry=None):
        '''把一块区域挖成透明（ImageDraw 是直接写像素，不混合，所以能真的挖洞）。'''
        self.ellipse(cx, cy, rx, rx if ry is None else ry, fill=CLEAR)

    def finish(self, path):
        out = self.image.resize((self.width, self.height), Image.LANCZOS)
        save(out, path)


# ============================================================ 通用零件

def draw_gear(pen, cx, cy, face, teeth, palette, tooth=0.30, hub=0.34, phase=0.0):
    '''带齿的圆盘（齿轮）。face 是齿根半径，齿尖 = face * (1 + tooth)。'''
    outline, main, light, accent = palette
    r_out = face * (1.0 + tooth)
    step = 360.0 / teeth
    pts = []

    for index in range(teeth):
        center = phase + index * step
        half = step * 0.28
        pts.append(polar(cx, cy, face * 0.96, center - step * 0.44))
        pts.append(polar(cx, cy, r_out, center - half))
        pts.append(polar(cx, cy, r_out, center + half))
        pts.append(polar(cx, cy, face * 0.96, center + step * 0.44))

    pen.polygon(pts, fill=main, outline=outline)
    pen.ellipse(cx, cy, face * 0.72, face * 0.72, fill=light, outline=outline)
    pen.erase(cx, cy, face * hub)

    if hub > 0.5:
        pen.ellipse(cx, cy, face * (hub - 0.34), face * (hub - 0.34), fill=accent)


def draw_spiked_ring(pen, cx, cy, radius, spikes, palette, spike=1.30, hole=0.52, phase=0.0):
    '''带尖刺的环（锯齿环 / 爆裂环），中间真的挖空。'''
    outline, main, light, accent = palette
    step = 360.0 / spikes
    pts = []

    for index in range(spikes):
        center = phase + index * step
        pts.append(polar(cx, cy, radius * spike, center))
        pts.append(polar(cx, cy, radius * 0.90, center + step * 0.20))
        pts.append(polar(cx, cy, radius * 0.90, center + step * 0.80))

    pen.polygon(pts, fill=main, outline=outline)
    pen.ellipse(cx, cy, radius * 0.76, radius * 0.76, outline=light)
    pen.erase(cx, cy, radius * hole)


def draw_star(pen, cx, cy, outer, inner, blades, palette, phase=0.0):
    '''风车/手里剑形状：外尖 + 内凹交替。'''
    outline, main, light, accent = palette
    step = 360.0 / blades
    pts = []

    for index in range(blades):
        center = phase + index * step
        pts.append(polar(cx, cy, outer, center))
        pts.append(polar(cx, cy, inner, center + step * 0.5))

    pen.polygon(pts, fill=main, outline=outline)
    pen.ellipse(cx, cy, inner * 0.62, inner * 0.62, fill=light, outline=outline)
    pen.dot(cx, cy, max(1.0, inner * 0.26), accent)


# ============================================================ 召唤：齿轮哨

def whistle_icon(size, palette, teeth, level):
    pen = Pen(size, size)
    outline, main, light, accent = palette
    unit = size / 36.0

    # 吹嘴
    pen.polygon([(4 * unit, 24 * unit), (17 * unit, 19 * unit),
                 (19 * unit, 27 * unit), (5 * unit, 31 * unit)],
                fill=main, outline=outline)
    # 挂环
    pen.ellipse(6 * unit, 28 * unit, 3.0 * unit, 3.0 * unit, outline=light)

    # 齿轮哨体
    draw_gear(pen, 25 * unit, 16 * unit, 8.4 * unit, teeth, palette, tooth=0.34, hub=0.40)

    if level == 1:
        pen.dot(21 * unit, 12 * unit, 1.2 * unit, accent)
    elif level == 2:
        # 精钢：多一圈校准环 + 蓝点
        pen.ellipse(25 * unit, 16 * unit, 10.6 * unit, 10.6 * unit, outline=accent)
        pen.dot(33 * unit, 9 * unit, 1.4 * unit, accent)
    else:
        # 灰烬：齿轮上再接一枚小齿轮，加两点余烬
        draw_gear(pen, 33 * unit, 27 * unit, 4.6 * unit, 6, palette, tooth=0.34, hub=0.40)
        pen.dot(15 * unit, 8 * unit, 1.6 * unit, accent)
        pen.dot(20 * unit, 4 * unit, 1.1 * unit, accent)

    pen.finish(os.path.join(ITEM_DIR, {1: 'RustedGearWhistle.png',
                                       2: 'SalvagedSteelGearWhistle.png',
                                       3: 'AshHeartGearWhistle.png'}[level]))


def sentry_icon(size, palette, teeth, kind):
    pen = Pen(size, size)
    outline, main, light, accent = palette
    unit = size / 28.0
    cx, cy = 14 * unit, 14 * unit

    draw_gear(pen, cx, cy, 8.0 * unit, teeth, palette, tooth=0.30, hub=0.36)

    if kind == 'charger':
        # 前部撞角 + 眼睛
        pen.polygon([(22 * unit, 10 * unit), (27 * unit, 14 * unit), (22 * unit, 18 * unit)],
                    fill=accent, outline=outline)
        pen.dot(17 * unit, 12 * unit, 1.5 * unit, accent)
    elif kind == 'sniper':
        # 侧向炮管 + 准星
        pen.rect(19 * unit, 12 * unit, 27 * unit, 15 * unit, fill=main, outline=outline)
        pen.rect(25 * unit, 12.5 * unit, 26.6 * unit, 14.5 * unit, fill=accent)
        pen.dot(10 * unit, 10 * unit, 1.6 * unit, accent)
    else:
        # 环绕型：两侧火焰翼
        for side in (-1, 1):
            pen.polygon([(cx + side * 11 * unit, cy - 4 * unit),
                         (cx + side * 15 * unit, cy),
                         (cx + side * 11 * unit, cy + 4 * unit)],
                        fill=accent, outline=outline)
        pen.dot(cx, cy, 2.4 * unit, accent)

    names = {
        'root': 'RustedGearSentry.png',
        'charger': 'SalvagedSteelCharger.png',
        'sniper': 'SalvagedSteelSniper.png',
        'orbit': 'AshHeartOrbitSentry.png',
    }
    pen.finish(os.path.join(PROJ_DIR, names[kind]))


def buff_icon(name, palette, glyph):
    '''Buff 图标固定 32x32：圆形徽章 + 内部符号。'''
    pen = Pen(32, 32)
    outline, main, light, accent = palette

    pen.ellipse(16, 16, 15.0, 15.0, fill=main, outline=outline, width=2)
    pen.ellipse(16, 16, 11.6, 11.6, outline=light)

    if glyph == 'gear':
        draw_gear(pen, 16, 16, 6.6, 8, palette, tooth=0.32, hub=0.34)
    elif glyph == 'twin':
        draw_gear(pen, 12.5, 13.5, 4.6, 7, palette, tooth=0.32, hub=0.34)
        draw_gear(pen, 20.0, 19.5, 5.0, 7, palette, tooth=0.32, hub=0.34)
    else:
        draw_gear(pen, 16, 17.5, 6.0, 8, palette, tooth=0.32, hub=0.34)
        pen.polygon([(16, 3.5), (20, 9), (16, 12), (12, 9)], fill=accent, outline=outline)

    pen.dot(5.5, 26.5, 1.6, accent)
    pen.dot(26.5, 5.5, 1.6, accent)
    pen.finish(os.path.join(PROJ_DIR, name))


# ============================================================ 盗贼：回旋系

def rogue_item_icon(size, palette, spikes, level):
    pen = Pen(size, size)
    outline, main, light, accent = palette
    unit = size / 28.0
    cx, cy = size / 2.0, size / 2.0

    if level == 1:
        draw_star(pen, cx, cy, 13.0 * unit, 5.4 * unit, 4, palette)
        draw_gear(pen, cx, cy, 3.2 * unit, 6, palette, tooth=0.34, hub=0.30)
    else:
        draw_spiked_ring(pen, cx, cy, 10.4 * unit, spikes, palette, spike=1.28, hole=0.54)

        if level >= 3:
            # 余烬核心
            pen.ellipse(cx, cy, 4.2 * unit, 4.2 * unit, fill=accent, outline=outline)
            pen.ellipse(cx, cy, 2.0 * unit, 2.0 * unit, fill=(255, 236, 190, 255))
        else:
            pen.ellipse(cx, cy, 2.4 * unit, 2.4 * unit, outline=accent)

    pen.finish(os.path.join(ITEM_DIR, {1: 'RustedGearShuriken.png',
                                       2: 'SalvagedSteelChakram.png',
                                       3: 'AshHeartCinderRing.png'}[level]))


def rogue_projectile_icon(name, size, palette, spikes, core):
    pen = Pen(size, size)
    outline, main, light, accent = palette
    unit = size / 28.0
    cx, cy = size / 2.0, size / 2.0

    if spikes <= 0:
        draw_star(pen, cx, cy, 12.6 * unit, 5.0 * unit, 4, palette)
        draw_gear(pen, cx, cy, 3.0 * unit, 6, palette, tooth=0.34, hub=0.30)
    else:
        draw_spiked_ring(pen, cx, cy, 10.2 * unit, spikes, palette, spike=1.26, hole=0.54)

        if core:
            pen.ellipse(cx, cy, 4.0 * unit, 4.0 * unit, fill=accent, outline=outline)
            pen.ellipse(cx, cy, 1.8 * unit, 1.8 * unit, fill=(255, 236, 190, 255))

    pen.finish(os.path.join(PROJ_DIR, name))


def blast_icon(name, size, palette, spikes):
    pen = Pen(size, size)
    outline, main, light, accent = palette
    cx = cy = size / 2.0
    radius = size * 0.34

    pen.ellipse(cx, cy, radius * 0.95, radius * 0.95, fill=(main[0], main[1], main[2], 150),
                outline=accent)
    step = 360.0 / spikes

    for index in range(spikes):
        center = index * step
        pen.polygon([polar(cx, cy, radius * 0.90, center - step * 0.12),
                     polar(cx, cy, radius * 1.60, center),
                     polar(cx, cy, radius * 0.90, center + step * 0.12)],
                    fill=(accent[0], accent[1], accent[2], 170))
        pen.polygon([polar(cx, cy, radius * 0.30, center - step * 0.20),
                     polar(cx, cy, radius * 0.78, center),
                     polar(cx, cy, radius * 0.30, center + step * 0.20)],
                    fill=(light[0], light[1], light[2], 120))

    pen.finish(os.path.join(PROJ_DIR, name))


# ============================================================ 小型弹幕

def bolt_icon(name, palette):
    pen = Pen(12, 12)
    outline, main, light, accent = palette

    # 钉身
    pen.polygon([(1, 5), (8, 4), (8.6, 7), (1, 7.4)], fill=main, outline=outline)
    # 钉尖
    pen.polygon([(8.4, 3.6), (11.4, 5.8), (8.4, 8.2)], fill=light, outline=outline)
    # 尾翼
    pen.line([(1.6, 4.2), (3.6, 1.6)], fill=accent, width=1)
    pen.line([(1.6, 7.6), (3.6, 10.4)], fill=accent, width=1)
    pen.finish(os.path.join(PROJ_DIR, name))


def ember_icon(name, palette, radius):
    pen = Pen(14, 14)
    outline, main, light, accent = palette

    pen.ellipse(7, 7.5, radius, radius, fill=main, outline=outline)
    pen.ellipse(7, 7.5, radius * 0.66, radius * 0.66, fill=accent)
    pen.ellipse(7, 7.5, radius * 0.30, radius * 0.30, fill=(255, 244, 214, 255))
    pen.polygon([(7, 0.8), (9.2, 4.4), (7, 6.0), (4.8, 4.4)], fill=accent, outline=outline)
    pen.finish(os.path.join(PROJ_DIR, name))


# ============================================================ 饰品：拾荒者芯片

def chip_icon(name, palette, level):
    pen = Pen(24, 24)
    outline, main, light, accent = palette

    # 引脚
    for index in range(3):
        y0 = 5.5 + index * 5.0
        pen.rect(0.8, y0, 3.4, y0 + 2.6, fill=accent)
        pen.rect(20.6, y0, 23.2, y0 + 2.6, fill=accent)

    # 基板
    pen.rect(3, 2.5, 21, 21.5, fill=main, outline=outline)
    pen.rect(4.6, 4.1, 19.4, 19.9, outline=light)

    if level == 1:
        pen.rect(8.4, 8.4, 15.6, 15.6, fill=light, outline=outline)
        pen.dot(12, 12, 1.8, accent)
    elif level == 2:
        for index in range(3):
            pen.rect(6.6 + index * 4.0, 6.0, 8.2 + index * 4.0, 17.8, fill=light)
        pen.rect(6.0, 6.0, 18.0, 18.0, outline=outline)
        pen.dot(12, 12, 2.2, accent)
    else:
        pen.polygon([(12, 5.4), (18, 9.0), (18, 15.6), (12, 19.2), (6, 15.6), (6, 9.0)],
                    fill=light, outline=outline)
        pen.ellipse(12, 12.3, 3.6, 3.6, fill=accent, outline=outline)
        pen.ellipse(12, 12.3, 1.8, 1.8, fill=(255, 244, 214, 255))

    pen.finish(os.path.join(ITEM_DIR, name))


# ============================================================ 预览

def build_preview():
    names = [
        os.path.join(ITEM_DIR, 'RustedGearWhistle.png'),
        os.path.join(ITEM_DIR, 'SalvagedSteelGearWhistle.png'),
        os.path.join(ITEM_DIR, 'AshHeartGearWhistle.png'),
        os.path.join(ITEM_DIR, 'RustedGearShuriken.png'),
        os.path.join(ITEM_DIR, 'SalvagedSteelChakram.png'),
        os.path.join(ITEM_DIR, 'AshHeartCinderRing.png'),
        os.path.join(ITEM_DIR, 'ScavengerChip.png'),
        os.path.join(ITEM_DIR, 'ReinforcedScavengerChip.png'),
        os.path.join(ITEM_DIR, 'WastelandOverlordCore.png'),
        os.path.join(PROJ_DIR, 'RustedGearSentry.png'),
        os.path.join(PROJ_DIR, 'RustedGearSentryBuff.png'),
        os.path.join(PROJ_DIR, 'SalvagedSteelCharger.png'),
        os.path.join(PROJ_DIR, 'SalvagedSteelSniper.png'),
        os.path.join(PROJ_DIR, 'SalvagedSteelGearBuff.png'),
        os.path.join(PROJ_DIR, 'SalvagedSteelBolt.png'),
        os.path.join(PROJ_DIR, 'AshHeartOrbitSentry.png'),
        os.path.join(PROJ_DIR, 'AshHeartGearBuff.png'),
        os.path.join(PROJ_DIR, 'AshHeartGearEmber.png'),
        os.path.join(PROJ_DIR, 'RustedGearBoomerang.png'),
        os.path.join(PROJ_DIR, 'SalvagedSteelChakramProj.png'),
        os.path.join(PROJ_DIR, 'ChakramShatterBlast.png'),
        os.path.join(PROJ_DIR, 'AshHeartCinderRingProj.png'),
        os.path.join(PROJ_DIR, 'CinderRingEmber.png'),
    ]

    icons = [(os.path.basename(p), Image.open(p).convert('RGBA')) for p in names if os.path.exists(p)]

    scale = 4
    cell = 24 * scale + 12
    columns = 6
    rows = (len(icons) + columns - 1) // columns
    preview = Image.new('RGBA', (columns * cell + 12, rows * cell + 12), (26, 28, 32, 255))
    draw = ImageDraw.Draw(preview)

    for index, (name, icon) in enumerate(icons):
        column = index % columns
        row = index // columns
        origin_x = 12 + column * cell
        origin_y = 12 + row * cell

        box = 24 * scale
        big = icon.resize((icon.width * scale, icon.height * scale), Image.NEAREST)
        preview.alpha_composite(big, (origin_x + (box - big.width) // 2,
                                      origin_y + (box - big.height) // 2))
        draw.rectangle((origin_x, origin_y, origin_x + box, origin_y + box), outline=(70, 74, 82, 255))
        draw.text((origin_x + 2, origin_y + box - 12), name[:26], fill=(214, 218, 226, 255))

    os.makedirs(INBOX, exist_ok=True)
    target = os.path.join(INBOX, 'preview_tree2.png')
    preview.save(target)
    print('wrote preview %-52s %dx%d' % ('preview_tree2.png', preview.width, preview.height))


# ============================================================ 主流程

def main():
    whistle_icon(36, RUST, 8, 1)
    whistle_icon(40, STEEL, 9, 2)
    whistle_icon(44, ASH, 10, 3)

    sentry_icon(28, RUST, 8, 'root')
    buff_icon('RustedGearSentryBuff.png', RUST, 'gear')

    sentry_icon(30, STEEL, 9, 'charger')
    sentry_icon(28, STEEL, 7, 'sniper')
    buff_icon('SalvagedSteelGearBuff.png', STEEL, 'twin')
    bolt_icon('SalvagedSteelBolt.png', STEEL)

    sentry_icon(32, ASH, 10, 'orbit')
    buff_icon('AshHeartGearBuff.png', ASH, 'flame')
    ember_icon('AshHeartGearEmber.png', ASH, 5.4)

    rogue_item_icon(24, RUST, 4, 1)
    rogue_item_icon(26, STEEL, 6, 2)
    rogue_item_icon(28, ASH, 8, 3)

    rogue_projectile_icon('RustedGearBoomerang.png', 24, RUST, 0, False)
    rogue_projectile_icon('SalvagedSteelChakramProj.png', 26, STEEL, 6, False)
    blast_icon('ChakramShatterBlast.png', 44, STEEL, 8)
    rogue_projectile_icon('AshHeartCinderRingProj.png', 28, ASH, 8, True)
    ember_icon('CinderRingEmber.png', ASH, 5.0)

    chip_icon('ScavengerChip.png', PCB, 1)
    chip_icon('ReinforcedScavengerChip.png', GOLD, 2)
    chip_icon('WastelandOverlordCore.png', ASH, 3)

    build_preview()

    print('')
    print('total %d texture files.' % len(CREATED))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
