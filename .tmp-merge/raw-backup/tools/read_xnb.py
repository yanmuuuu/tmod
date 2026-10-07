"""读取 Terraria .xnb 贴图尺寸（XNB/XNA Texture2D，支持 LZ4 压缩）。

用法: python read_xnb.py <path> [<path> ...]
"""
import struct
import sys


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


def read_string(data, pos):
    n, pos = read_7bit_int(data, pos)
    return data[pos:pos + n].decode("utf-8", "replace"), pos + n


def lz4_decompress(src, expected):
    """纯 Python LZ4 block 解压（MonoGame XNB 使用标准 LZ4 block 格式）。"""
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


def xnb_content(path):
    with open(path, "rb") as f:
        data = f.read()
    if data[:3] != b"XNB":
        raise ValueError("not an XNB file")
    platform, version, flags = data[3], data[4], data[5]
    if flags & 0x80:
        decompressed, compressed = struct.unpack_from("<II", data, 10)
        content = lz4_decompress(data[18:18 + compressed], decompressed)
    else:
        content = data[10:]
    return content, platform, version, flags


def tex_info(path):
    content, platform, version, flags = xnb_content(path)
    pos = 0
    readers, pos = read_7bit_int(content, pos)
    for _ in range(readers):
        _s, pos = read_string(content, pos)
        pos += 4  # reader version
    _shared, pos = read_7bit_int(content, pos)
    _typeid, pos = read_7bit_int(content, pos)
    fmt, w, h, mips, size = struct.unpack_from("<iiiii", content, pos)
    return "%4dx%-5d  w/40=%-6.2f h/56=%-6.2f fmt=%d mips=%d v%d p%d flags=0x%02x" % (
        w, h, (w / 40.0), (h / 56.0), fmt, mips, version, platform, flags)


if __name__ == "__main__":
    for p in sys.argv[1:]:
        try:
            print("%-46s %s" % (p.split("Images")[-1], tex_info(p)))
        except Exception as e:
            print("%-46s ERROR %r" % (p.split("Images")[-1], e))
