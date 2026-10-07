"""扫描已安装的 .tmod，统计真实模组中城镇 NPC 贴图（body + _Head）的实际尺寸。

用途：确定 tModLoader 城镇 NPC 精灵表的帧布局约定，避免占位贴图帧错位。
"""
import glob
import io
import os
import struct
import zipfile

MODS_DIR = os.path.expanduser(r"~\Documents\My Games\Terraria\tModLoader\Mods")


def png_size(data):
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    w, h = struct.unpack(">II", data[16:24])
    return (w, h)


def main():
    print("mods dir:", MODS_DIR)
    files = glob.glob(os.path.join(MODS_DIR, "*.tmod"))
    print("tmod count:", len(files))
    subdirs = [d for d in glob.glob(os.path.join(MODS_DIR, "*")) if os.path.isdir(d)]
    print("subdirs:", len(subdirs))

    found = []
    for path in files:
        name = os.path.basename(path)
        try:
            with open(path, "rb") as f:
                raw = f.read()
            # .tmod = tModLoader 自有头部 + 标准 zip 体，定位 PK 签名后再当 zip 读
            idx = raw.find(b"PK\x03\x04")
            if idx < 0:
                raise ValueError("no zip payload")
            with zipfile.ZipFile(io.BytesIO(raw[idx:])) as z:
                names = z.namelist()
                for h in [n for n in names if n.endswith("_Head.png")][:6]:
                    body = h[: -len("_Head.png")] + ".png"
                    if body in names:
                        bs = png_size(z.read(body))
                        hs = png_size(z.read(h))
                        if bs:
                            found.append((name, body, bs, hs))
        except Exception as e:  # noqa: BLE001
            print("ERR %-40s %r" % (name, e))

    print("\n=== town NPC body / head textures found: %d ===" % len(found))
    for name, body, bs, hs in found[:40]:
        print("%-28s %-46s body=%-11s head=%s  frames@40w=%.1f" % (
            name[:28], body[-46:], "%dx%d" % bs, hs, bs[0] / 40.0))


if __name__ == "__main__":
    main()
