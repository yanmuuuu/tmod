import os
import sys

sys.path.insert(0, r"E:\开发\tools")
import tmod_lib

path = r"C:\Users\p老师\Documents\My Games\Terraria\tModLoader\Mods\InnoVault.tmod"
outdir = r"E:\开发\.tmp-research\inno"
os.makedirs(outdir, exist_ok=True)

entries = tmod_lib.list_entries(path)
print("ENTRY COUNT:", len(entries))
for name, size, csize in entries:
    print("  %-40s size=%-9d csize=%d" % (name, size, csize))

payloads = tmod_lib.read_tmod(path)
for name, blob in payloads.items():
    if name.lower().endswith((".dll", ".pdb", ".xml", ".json", ".txt")) or name == "Info":
        ext = os.path.splitext(name)[1] or ".txt"
        safe = name.replace("/", "_").replace("\\", "_")
        with open(os.path.join(outdir, safe), "wb") as fh:
            fh.write(blob)
        print("WROTE", safe, len(blob))
