import sys, os
sys.path.insert(0, r"E:\开发\tools")
from tmod_lib import read_tmod, list_entries

path = sys.argv[1]
out = sys.argv[2]
os.makedirs(out, exist_ok=True)
want = sys.argv[3:] if len(sys.argv) > 3 else None

for name, size, csize in list_entries(path):
    print(f"{name}\t{size}\t{csize}")

files = read_tmod(path, want=want)
for name, blob in files.items():
    target = os.path.join(out, name.replace("/", "__").replace("\\", "__"))
    with open(target, "wb") as h:
        h.write(blob)
    print("WROTE", target, len(blob))
