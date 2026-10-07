import sys
p = sys.argv[1]
data = open(p, "rb").read()
print("len", len(data))
print(repr(data[:300]))
print("---- strings ----")
import re
for m in re.finditer(rb"[\x20-\x7e]{4,}", data):
    print(m.start(), m.group().decode("ascii", "replace"))
