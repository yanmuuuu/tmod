# -*- coding: utf-8 -*-
# 一次性排查：找出工程里"扩展名像图片、内容不是图片"的文件（构建期会打印
# WARN: Image loading failed: unknown image type）。
import os
import sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r"E:\开发\WastelandSoul"
IMAGE_EXT = (".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tga")


def looks_like_image(ext, head):
    if ext == ".png":
        return head[:8] == b"\x89PNG\r\n\x1a\n"
    if ext in (".jpg", ".jpeg"):
        return head[:2] == b"\xff\xd8"
    if ext == ".bmp":
        return head[:2] == b"BM"
    if ext == ".gif":
        return head[:3] == b"GIF"
    return True


bad = []
count = 0

for dirpath, dirnames, filenames in os.walk(ROOT):
    dirnames[:] = [d for d in dirnames if d not in ("obj", "bin", ".vs")]

    for name in filenames:
        ext = os.path.splitext(name)[1].lower()

        if ext not in IMAGE_EXT:
            continue

        count += 1
        path = os.path.join(dirpath, name)

        try:
            with open(path, "rb") as handle:
                head = handle.read(16)
        except OSError as error:
            bad.append((path, "unreadable: %s" % error))
            continue

        if not looks_like_image(ext, head):
            bad.append((path, "head=%r" % head[:8]))

print("扫描图片 %d 个，异常 %d 个" % (count, len(bad)))

for path, why in bad:
    print("  !!", os.path.relpath(path, ROOT), why)
