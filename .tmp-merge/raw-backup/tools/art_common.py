# -*- coding: utf-8 -*-
"""AI 出图 -> 模组贴图 的公共底层（`apply_art.py` 与 `split_sheet.py` 共用）。

放在单独模块里是为了避免两个脚本互相 import 形成循环。
"""
import numpy as np
from PIL import Image


def key_magenta(image):
    """品红背景 -> alpha。`min(R,B) - G` 对骨白 / 冷蓝 / 深蓝全部安全（都是负值或很小）。"""
    array = np.asarray(image.convert("RGBA")).astype(np.int16)
    red, green, blue, alpha = array[..., 0], array[..., 1], array[..., 2], array[..., 3]

    score = np.minimum(red, blue) - green
    fade = np.clip((90.0 - score) / 60.0, 0.0, 1.0)

    array[..., 3] = (alpha * fade).astype(np.int16)
    return Image.fromarray(array.astype(np.uint8), "RGBA")


def bleed_fix(image, rounds=3):
    """把半透明像素的 RGB 换成邻近的不透明像素颜色（消除品红色溢，否则缩放后边缘发紫）。"""
    array = np.asarray(image).astype(np.uint8).copy()
    alpha = array[..., 3]
    solid = alpha >= 250

    filled = array[..., :3].astype(np.int16)
    mask = solid.copy()

    for _ in range(rounds):
        neighbours = []
        for shift_y, shift_x in ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (-1, 1), (1, -1), (1, 1)):
            shifted_mask = np.roll(np.roll(mask, shift_y, axis=0), shift_x, axis=1)
            shifted_rgb = np.roll(np.roll(filled, shift_y, axis=0), shift_x, axis=1)
            neighbours.append((shifted_mask, shifted_rgb))

        for neighbour_mask, neighbour_rgb in neighbours:
            take = neighbour_mask & ~mask
            filled[take] = neighbour_rgb[take]
            mask |= take

    use = alpha < 250
    array[..., :3] = np.where(use[..., None], filled, array[..., :3])
    return Image.fromarray(array, "RGBA")


def quantize(image, colors):
    """只对 RGB 做调色板量化，alpha 原样保留（透明区 RGB 先填成不透明均值，避免污染调色板）。"""
    alpha = image.getchannel("A")
    rgb = image.convert("RGB")

    opaque = np.asarray(alpha) > 0
    if opaque.any():
        mean = np.asarray(rgb).astype(np.float32)[opaque].mean(axis=0).astype(np.uint8)
        data = np.asarray(rgb).copy()
        data[~opaque] = mean
        rgb = Image.fromarray(data, "RGB")

    palette = min(colors, max(2, len(rgb.getcolors(maxcolors=1 << 24) or [(0, (0, 0, 0))])))
    quantized = rgb.quantize(colors=palette, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGBA")
    quantized.putalpha(alpha)
    return quantized


def threshold_alpha(image, low=32):
    array = np.asarray(image).copy()
    alpha = array[..., 3]
    array[..., 3] = np.where(alpha < low, 0, alpha)
    return Image.fromarray(array, "RGBA")


def scale_into(image, size, fill):
    """等比缩放并居中放进目标画布（返回 (画布, (内容宽, 内容高))）。"""
    target_w, target_h = size
    box = image.getchannel("A").getbbox()

    if box is None:
        raise SystemExit("!! 抠完之后整张图都是透明的（背景色不对？）")

    image = image.crop(box)
    avail_w, avail_h = target_w * fill, target_h * fill
    ratio = min(avail_w / image.width, avail_h / image.height)

    new_w = max(1, int(round(image.width * ratio)))
    new_h = max(1, int(round(image.height * ratio)))
    resample = Image.BOX if ratio < (1.0 / 3.0) else Image.LANCZOS
    image = image.resize((new_w, new_h), resample)

    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(image, ((target_w - new_w) // 2, (target_h - new_h) // 2))
    return canvas, (new_w, new_h)
