"""试验性解出 Terraria XNB 的压缩方式与贴图尺寸。

尝试多种「压缩头 + 载荷」假设，用是否包含 Texture2DReader 字串判断解压成功，
成功后再解析 Texture2D 头部得到宽高。
"""
import struct
import sys
import zlib

sys.path.insert(0, r"E:\开发\tools")
from read_xnb import lz4_decompress, read_7bit_int, read_string  # noqa: E402


def candidates(data):
    for off in (10, 14, 18, 22):
        for wb, label in ((-15, "rawdeflate"), (15, "zlib"), (47, "gzip")):
            try:
                out = zlib.decompressobj(wb).decompress(data[off:])
                if len(out) > 64:
                    yield off, label, out
            except Exception:  # noqa: BLE001
                pass
        try:
            out = lz4_decompress(data[off:], 0)
            if len(out) > 64:
                yield off, "lz4", out
        except Exception:  # noqa: BLE001
            pass


def parse_tex(content):
    pos = 0
    readers, pos = read_7bit_int(content, pos)
    names = []
    for _ in range(readers):
        s, pos = read_string(content, pos)
        names.append(s)
        pos += 4
    _shared, pos = read_7bit_int(content, pos)
    _typeid, pos = read_7bit_int(content, pos)
    fmt, w, h, mips, size = struct.unpack_from("<iiiii", content, pos)
    return names, (w, h, fmt, mips, size)


def main():
    for path in sys.argv[1:]:
        data = open(path, "rb").read()
        print("=== %s  (%d bytes)  flags=0x%02x" % (path.rsplit("\\", 1)[-1], len(data), data[5]))
        hit = False
        for off, label, out in candidates(data):
            if b"Texture2DReader" in out:
                try:
                    names, tex = parse_tex(out)
                    print("   OK off=%-3d %-11s len=%-8d %dx%d fmt=%d mips=%d  readers=%s"
                          % (off, label, len(out), tex[0], tex[1], tex[2], tex[3], names))
                except Exception as e:  # noqa: BLE001
                    print("   OK off=%-3d %-11s len=%d but header parse failed: %r" % (off, label, len(out), e))
                hit = True
        if not hit:
            print("   no candidate produced Texture2DReader")
            print("   head bytes:", data[:24].hex(" "))


if __name__ == "__main__":
    main()
