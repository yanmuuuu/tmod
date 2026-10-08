import sys, os, zlib
sys.path.insert(0, r"E:\开发\tools")
from tmod_lib import read_tmod
p = r"C:\Users\p老师\Documents\My Games\Terraria\tModLoader\Mods\SubworldLibrary.tmod"
print("exists", os.path.exists(p))
files = read_tmod(p)
for k, v in files.items():
    print(k, len(v))
out = r"E:\开发\.tmp-swdump"
for k, v in files.items():
    with open(os.path.join(out, os.path.basename(k)), "wb") as h:
        h.write(v)
print("--- Info ---")
print(files.get("Info", b"").decode("utf-8", "replace"))
