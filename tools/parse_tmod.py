"""完整解析 .tmod：条目表 + 数据块，导出 Info / DLL / PDB 并检查内容。

用来确认命令行构建到底编译了什么、元数据是否正常。
"""
import re
import struct
import sys
import zlib

path = sys.argv[1]
data = open(path, "rb").read()
print("file size:", len(data), " magic:", data[:4])

# 条目表：找到第一个条目名 "\x11WastelandSoul.dll"
p0 = data.find(b"\x11WastelandSoul.dll")
if p0 < 0:
    print("找不到条目表")
    sys.exit(1)

count = struct.unpack_from("<i", data, p0 - 4)[0]
print("entry count @%d = %d" % (p0 - 4, count))

pos = p0
entries = []
for _ in range(count):
    nl = data[pos]
    pos += 1
    name = data[pos:pos + nl].decode("utf-8", "replace")
    pos += nl
    size, csize = struct.unpack_from("<ii", data, pos)
    pos += 8
    entries.append((name, size, csize))
    print("  entry %-22s size=%-7d compressed=%d" % (name, size, csize))

print("数据块起点:", pos)
payloads = {}
for name, size, csize in entries:
    blob = data[pos:pos + csize]
    pos += csize
    try:
        out = zlib.decompressobj(-15).decompress(blob)
    except Exception as exc:  # noqa: BLE001
        print("  !! %s 解压失败 %r" % (name, exc))
        continue
    payloads[name] = out
    print("  %s 解压后 %d 字节 (表头声称 %d)" % (name, len(out), size))

# --- Info 内容 ---
info = payloads.get("Info", b"")
print("\n=== Info 原文 ===")
try:
    print(info.decode("utf-8"))
except UnicodeDecodeError:
    print(info.hex(" "))

# --- DLL 里的类型名 ---
dll = payloads.get("WastelandSoul.dll", b"")
names = set(re.findall(rb"[\x20-\x7e]{6,}", dll))
interesting = [n.decode() for n in names if b"Wasteland" in n or b"Scavenger" in n or b"Mechanical" in n]
print("\n=== DLL 中与模组相关的可读字符串 ===")
print("总可读串 %d 个；相关串 %s" % (len(names), interesting if interesting else "（无）"))

# --- PDB 里的源文件路径 ---
pdb = payloads.get("WastelandSoul.pdb", b"")
paths = sorted({m.decode("utf-8", "replace") for m in re.findall(rb"[A-Za-z]:\\[^\x00]{6,140}\.cs", pdb)})
print("\n=== PDB 中记录的源文件路径（前 30 条）===")
for item in paths[:30]:
    print("  ", item)
print("路径总数:", len(paths))
