"""从 .tmod 导出贴图为 PNG（.rawimg = 12字节头 + w*h*4 像素）。

用法:
  python extract_tmod_images.py <tmod路径> <输出目录> [路径关键字] [最多导出数]
"""
import io
import os
import struct
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from tmod_lib import read_tmod  # noqa: E402


def rawimg_to_image(payload):
    if payload[:8] == b"\x89PNG\r\n\x1a\n":
        return Image.open(io.BytesIO(payload)).convert("RGBA"), 0, "png"

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
            return Image.frombytes("RGBA", (w, h), body, "raw", "BGRA"), head, "BGRA"

    return None, None, None


def main():
    tmod, outdir = sys.argv[1], sys.argv[2]
    keyword = sys.argv[3] if len(sys.argv) > 3 else ""
    limit = int(sys.argv[4]) if len(sys.argv) > 4 else 40

    os.makedirs(outdir, exist_ok=True)
    entries = read_tmod(tmod, want=[keyword] if keyword else [".rawimg"])

    images = [n for n in entries if n.endswith(".rawimg")]
    print("%s: 命中 %d 个贴图" % (os.path.basename(tmod), len(images)))

    done = 0
    for name in sorted(images):
        if done >= limit:
            break
        img, head, order = rawimg_to_image(entries[name])
        if img is None:
            continue
        dest = os.path.join(outdir, name.replace("/", os.sep).replace(".rawimg", ".png"))
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        img.save(dest)
        print("  %-66s %s" % (name, img.size))
        done += 1

    print("导出 %d 张 -> %s" % (done, outdir))


if __name__ == "__main__":
    main()
