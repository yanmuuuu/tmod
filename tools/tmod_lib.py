"""通用 .tmod 解析库。

tModLoader 私有容器格式（非 zip）：
  "TMOD" | 7bit长度+版本串 | 20字节哈希 | ... | 7bit长度+模组名 | 7bit长度+模组版本
  | int32 条目数 | [7bit长度+条目名 | int32 原始大小 | int32 压缩大小] * N
  | 依次排列的数据块（raw deflate，未压缩则原样存储）

条目表的定位是自动的：扫描候选锚点并校验「表尾 + 所有数据块长度 == 文件长度」。
"""
import struct
import zlib


def _read_7bit(data, pos):
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


def _parse_entries(data, pos, count):
    entries = []
    for _ in range(count):
        if pos >= len(data):
            raise ValueError("条目表越界")
        name_len = data[pos]
        pos += 1
        if not 1 <= name_len <= 200 or pos + name_len + 8 > len(data):
            raise ValueError("条目名长度异常")
        name = data[pos:pos + name_len].decode("utf-8", "replace")
        pos += name_len
        size, csize = struct.unpack_from("<ii", data, pos)
        pos += 8
        if size < 0 or csize < 0 or csize > len(data):
            raise ValueError("条目大小异常")
        entries.append((name, size, csize))
    return entries, pos


def find_entry_table(data):
    """返回 (entries, data_start)；找不到返回 (None, None)。

    校验方式：数据块总长必须不超过文件剩余长度，且「剩余量」最小者胜出。
    不同 tModLoader 版本的 .tmod 可能带尾部签名/校验区，所以不用精确相等。
    """
    limit = min(len(data), 4 * 1024 * 1024)  # 表头一定在文件前部
    best = None
    best_leftover = None

    for pos in range(8, limit):
        name_len = data[pos]
        if not 1 <= name_len <= 200:
            continue
        if pos + 1 + name_len > len(data) or not data[pos + 1:pos + 1 + name_len].endswith(b".dll"):
            continue
        if any(c < 0x20 or c > 0x7E for c in data[pos + 1:pos + 1 + name_len]):
            continue

        count = struct.unpack_from("<i", data, pos - 4)[0]
        if not 1 <= count <= 5000:
            continue

        try:
            entries, end = _parse_entries(data, pos, count)
        except Exception:  # noqa: BLE001
            continue

        total = sum(csize for _, _, csize in entries)
        leftover = len(data) - end - total

        if leftover < 0 or leftover > 8192:
            continue

        if best_leftover is None or leftover < best_leftover:
            best, best_leftover = (entries, end), leftover

    if best is None:
        return None, None

    return best


def read_tmod(path, want=None):
    """读取 .tmod，返回 {条目名: 解压后的字节}。

    want: 可选的关键字列表，只解压名字包含这些关键字的条目（大模组时省内存/时间）。
    """
    with open(path, "rb") as handle:
        data = handle.read()

    if data[:4] != b"TMOD":
        raise ValueError("不是 .tmod 文件")

    entries, pos = find_entry_table(data)
    if entries is None:
        raise ValueError("找不到条目表（格式可能不是 tModLoader 1.4.x）")

    out = {}
    for name, _size, csize in entries:
        blob = data[pos:pos + csize]
        pos += csize
        if want and not any(w.lower() in name.lower() for w in want):
            continue
        try:
            out[name] = zlib.decompressobj(-15).decompress(blob)
        except zlib.error:
            out[name] = blob  # 未压缩存储
    return out


def list_entries(path):
    with open(path, "rb") as handle:
        data = handle.read()
    entries, _pos = find_entry_table(data)
    if entries is None:
        raise ValueError("找不到条目表")
    return entries
