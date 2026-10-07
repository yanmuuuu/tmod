"""把 .tmod 里的贴图导出并拼成「带编号的对照图」，方便挑参照。

用法:
  python make_contact_sheet.py <tmod路径> <输出目录> [关键字过滤] [每行个数]
"""
import io
import os
import struct
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from tmod_lib import read_tmod  # noqa: E402


def rawimg_to_image(payload):
    if payload[:8] == b"\x89PNG\r\n\x1a\n":
        return Image.open(io.BytesIO(payload)).convert("RGBA")

    for head in (12, 8, 16):
        if len(payload) <= head:
            continue
        body = payload[head:]
        for off in (0, 4):
            if len(payload) < off + 8:
                continue
            w, h = struct.unpack_from("<ii", payload, off)
            if not (0 < w <= 8192 and 0 < h <= 8192):
                continue
            if w * h * 4 != len(body):
                continue
            return Image.frombytes("RGBA", (w, h), body, "raw", "BGRA")

    return None


def main():
    tmod, outdir = sys.argv[1], sys.argv[2]
    keyword = sys.argv[3] if len(sys.argv) > 3 else ""
    per_row = int(sys.argv[4]) if len(sys.argv) > 4 else 6

    os.makedirs(outdir, exist_ok=True)
    entries = read_tmod(tmod, want=[keyword] if keyword else [".rawimg"])

    images = []
    for name in sorted(entries):
        if not name.endswith(".rawimg"):
            continue
        img = rawimg_to_image(entries[name])
        if img is None or img.width < 8 or img.height < 8:
            continue
        images.append((name, img))

    print("可用贴图: %d 张" % len(images))

    cell = 190
    label_h = 22
    scale_cap = 150
    rows = (len(images) + per_row - 1) // per_row
    sheet = Image.new("RGBA", (per_row * cell, max(rows, 1) * (cell + label_h)), (28, 30, 34, 255))
    draw = ImageDraw.Draw(sheet)

    listing = []
    for index, (name, img) in enumerate(images):
        col, row = index % per_row, index // per_row
        x0, y0 = col * cell, row * (cell + label_h)

        thumb = img.copy()
        # 只缩不放，保持像素风的清晰
        if thumb.width > scale_cap or thumb.height > scale_cap:
            ratio = min(scale_cap / thumb.width, scale_cap / thumb.height)
            thumb = thumb.resize((max(1, int(thumb.width * ratio)), max(1, int(thumb.height * ratio))), Image.NEAREST)

        # 放在格子中央
        px = x0 + (cell - thumb.width) // 2
        py = y0 + (cell - thumb.height) // 2
        sheet.alpha_composite(thumb, (px, py))

        # 编号 + 文件名
        draw.rectangle((x0 + 2, y0 + 2, x0 + 30, y0 + 22), fill=(60, 64, 72, 255))
        draw.text((x0 + 7, y0 + 5), "%d" % index, fill=(255, 220, 120, 255))
        short = name.rsplit("/", 1)[-1].replace(".rawimg", "")
        draw.text((x0 + 6, y0 + cell + 4), "%d  %s" % (index, short[:24]), fill=(210, 214, 220, 255))

        listing.append((index, name, img.size))

    sheet_path = os.path.join(outdir, "contact_sheet.png")
    sheet.save(sheet_path)
    print("对照图:", sheet_path, sheet.size)

    print("\n编号清单:")
    for index, name, size in listing:
        print("  %3d  %-58s %s" % (index, name, size))


if __name__ == "__main__":
    main()
