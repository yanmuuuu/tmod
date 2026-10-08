"""读取原版 Terraria 1.4.4 的 Projectile_*.xnb 贴图尺寸（只用标准库）。

1.4.4 的 xnb 是 **XNB v5 / platform 119 / flags 0x80** 的 LZ4 block 压缩格式：
    0..2   'XNB'
    3      platform (119)
    4      version (5)
    5      flags (0x80 = LZ4 压缩)
    6..9   decompressedSize (uint32 LE)
    10..13 compressedSize   (uint32 LE)
    14..   压缩数据（LZ4 block）
解压后的内容是 DXGI 格式的 Texture2D：
    readerCount (7bit) + readerName(cstring) + readerVersion(int32)   × readerCount
    sharedResourceCount (7bit)
    typeId (7bit)  —— 0 表示 Texture2D
    surfaceFormat (int32) / width (int32) / height (int32) / mipCount (int32) / dataSize (int32)

用法:
    python read_vanilla_tex.py 116 132 459 ...
    python read_vanilla_tex.py --item Projectile
"""
import os
import struct
import sys

VANILLA = r"E:\steam\steamapps\common\Terraria\Content\Images"


def read_7bit_int(data, pos):
    result = 0
    bits = 0
    while True:
        b = data[pos]
        pos += 1
        result |= (b & 0x7F) << bits
        if not (b & 0x80):
            break
        bits += 7
    return result, pos


def lz4_decompress(src, expected):
    out = bytearray()
    pos = 0
    n = len(src)
    while pos < n:
        token = src[pos]
        pos += 1
        lit_len = token >> 4
        if lit_len == 15:
            while True:
                b = src[pos]
                pos += 1
                lit_len += b
                if b != 255:
                    break
        out += src[pos:pos + lit_len]
        pos += lit_len
        if pos >= n:
            break
        offset = src[pos] | (src[pos + 1] << 8)
        pos += 2
        if offset == 0:
            break
        match_len = token & 0x0F
        if match_len == 15:
            while True:
                b = src[pos]
                pos += 1
                match_len += b
                if b != 255:
                    break
        match_len += 4
        start = len(out) - offset
        for i in range(match_len):
            out.append(out[start + i])
    return bytes(out[:expected]) if expected else bytes(out)


def xnb_size(path):
    with open(path, "rb") as handle:
        data = handle.read()
    if data[:3] != b"XNB":
        raise ValueError("not an XNB file")
    flags = data[5]
    if flags & 0x80:
        decompressed, compressed = struct.unpack_from("<II", data, 6)
        content = lz4_decompress(data[14:14 + compressed], decompressed)
    else:
        content = data[6:]
    pos = 0
    readers, pos = read_7bit_int(content, pos)
    for _ in range(readers):
        size, pos = read_7bit_int(content, pos)
        pos += size + 4
    _shared, pos = read_7bit_int(content, pos)
    _type_id, pos = read_7bit_int(content, pos)
    _fmt, width, height = struct.unpack_from("<iii", content, pos)
    return width, height


def main():
    args = sys.argv[1:]
    prefix = "Projectile"
    if args and args[0] == "--item":
        prefix = args[1]
        args = args[2:]
    for raw in args:
        name = "%s_%s.xnb" % (prefix, raw)
        path = os.path.join(VANILLA, name)
        if not os.path.exists(path):
            print("%-10s MISSING" % raw)
            continue
        try:
            width, height = xnb_size(path)
            print("%-10s %3dx%-3d" % (raw, width, height))
        except Exception as error:  # noqa: BLE001 - 诊断脚本，坏文件直接报出来
            print("%-10s ERROR %r" % (raw, error))
    return 0


if __name__ == "__main__":
    sys.exit(main())
