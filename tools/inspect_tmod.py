"""检查 .tmod 容器里到底打包了哪些条目（tModLoader 私有格式，非 zip）。"""
import re
import sys

path = sys.argv[1]
data = open(path, "rb").read()

print("size =", len(data))
print("head =", data[:64].hex(" "))
print("ascii head =", data[:64])

runs = re.findall(rb"[\x20-\x7e]{4,}", data)
print("\n可读字符串片段: %d 个" % len(runs))
for run in runs[:60]:
    print("  ", run.decode("ascii", "replace"))

# 统计可能的压缩流
print("\nzlib 头(78 9c/78 01/78 da)出现次数:", len(re.findall(rb"\x78[\x01\x9c\xda]", data)))
print("PK 头出现次数:", data.count(b"PK\x03\x04"))
print("TMOD 魔数:", data[:4])
