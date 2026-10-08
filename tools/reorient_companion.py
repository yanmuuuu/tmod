# -*- coding: utf-8 -*-
"""按玩家要求重排智械人的帧表：**只要左右朝向，删掉正面/背面**。

现在的表（40x1400，25 帧）里，第 4-7 帧（低头/抬头）和第 16-20 帧（站立）用的是**正面**像，
而第 8-11 / 12-15 才是侧身 —— 于是走动时朝向和走路方向对不上，还会突然出现正面像。

新表布局（全部是**侧身朝右**的图，游戏里靠 spriteDirection 水平翻转来表现朝左）：

    0-3    侧身行走（4 帧）
    4      站立（中立）
    5      低头
    6      站立（中立）
    7      抬头
    8-11   侧身行走（与 0-3 同源，供原版"向右走"那一排用）
    12-15  侧身行走（与 8-11 相同，供原版"向左走"那一排用；左右由翻转表现，不再各画一套）
    16-20  站立（5 帧同图，原版站立排）
    21-24  使用 / 攻击（保留原攻击像）

这样**表里不再有任何正面/背面像**，朝向完全由「走路方向 → spriteDirection」决定。
"""
import os
import shutil

from PIL import Image

MOD = r"E:\开发\WastelandSoul"
SHEET = os.path.join(MOD, "Content", "NPCs", "Town", "MechanicalCompanion.png")
BACKUP = os.path.join(MOD, "..", ".backup", "MechanicalCompanion.before-reorient.png")

FRAME_W, FRAME_H, COUNT = 40, 56, 25
TILT_SPLIT = 22          # 头部起始行（apply_art.tilt_frame 用的同一个分界）


def tilt(frame, dy):
    """把头部区域整体上下移动 dy 像素（低头 / 抬头）。"""
    head = frame.crop((0, TILT_SPLIT, FRAME_W, FRAME_H))
    out = frame.copy()
    out.paste((0, 0, 0, 0), (0, TILT_SPLIT, FRAME_W, FRAME_H))
    out.alpha_composite(head, (0, TILT_SPLIT + dy))
    return out


def main():
    sheet = Image.open(SHEET).convert("RGBA")

    if sheet.height != FRAME_H * COUNT:
        raise SystemExit("帧表高度不对：%d（期望 %d）" % (sheet.height, FRAME_H * COUNT))

    if not os.path.exists(BACKUP):
        shutil.copyfile(SHEET, BACKUP)
        print("原表备份: %s" % BACKUP)

    old = [sheet.crop((0, i * FRAME_H, FRAME_W, (i + 1) * FRAME_H)) for i in range(COUNT)]
    walk = old[0:4]                      # 侧身行走（原表 0-3 就是侧身）
    neutral = old[0]                     # 拿第一帧当"站立"
    new = []

    new += walk                          # 0-3
    new += [neutral, tilt(neutral, 3), neutral, tilt(neutral, -3)]   # 4-7 站立/低头/站立/抬头
    new += old[8:12]                     # 8-11 侧身行走
    new += old[8:12]                     # 12-15 同上（左右靠翻转，不再各画一套）
    new += [neutral] * 5                 # 16-20 站立
    new += old[21:25]                    # 21-24 攻击

    assert len(new) == COUNT, len(new)

    out = Image.new("RGBA", (FRAME_W, FRAME_H * COUNT), (0, 0, 0, 0))

    for i, frame in enumerate(new):
        out.alpha_composite(frame, (0, i * FRAME_H))

    out.save(SHEET)
    print("已重排为 %dx%d（%d 帧），全部侧身像" % (out.width, out.height, COUNT))

    preview = Image.new("RGBA", (FRAME_W * 5, FRAME_H * 5), (28, 30, 34, 255))

    for i in range(COUNT):
        preview.alpha_composite(new[i], ((i % 5) * FRAME_W, (i // 5) * FRAME_H))

    path = os.path.join(MOD, "..", "art-inbox", "preview_companion_reorient.png")
    preview.resize((preview.width * 3, preview.height * 3), Image.NEAREST).save(path)
    print("预览: %s" % path)


if __name__ == "__main__":
    main()
