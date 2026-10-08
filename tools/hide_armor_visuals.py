# -*- coding: utf-8 -*-
"""把精钢套装的**穿身贴图**清空（保留原文件尺寸，只把像素全透明）。

玩家要求：装备穿在身上的视觉效果先关掉，等做出对应的 3D/自绘模型再开。
做法上不删文件、也不改代码 —— 直接把 `_Head/_Body/_Legs` 三个装备贴图整张透明化，
尺寸不变（`check_assets.py` 的尺寸校验照样过），要恢复时重跑 `tools/gen_armor_equip.py` 即可。
"""
import glob
import os

from PIL import Image

MOD = r"E:\开发\WastelandSoul"
PATTERN = os.path.join(MOD, "Content", "Items", "Armor", "SalvagedSteel*_*.png")
BACKUP = os.path.join(MOD, "..", ".backup", "armor_equip_visuals")

os.makedirs(BACKUP, exist_ok=True)

targets = sorted(glob.glob(PATTERN))
print("找到 %d 张装备贴图" % len(targets))

for path in targets:
    image = Image.open(path).convert("RGBA")
    name = os.path.basename(path)

    # 第一次运行时把原图备份走（要恢复视觉效果直接把备份拷回来）
    keep = os.path.join(BACKUP, name)

    if not os.path.exists(keep):
        image.save(keep)

    blank = Image.new("RGBA", image.size, (0, 0, 0, 0))
    blank.save(path)
    print("  %-44s %dx%d -> 全透明" % (name, image.width, image.height))

print("原图备份在 %s" % BACKUP)
