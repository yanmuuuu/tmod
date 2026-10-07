import os
import sys

sys.path.insert(0, r"E:\开发\tools")
import tmod_lib

path = r"C:\Users\p老师\Documents\My Games\Terraria\tModLoader\Mods\InnoVault.tmod"
outdir = r"E:\开发\.tmp-research\inno_src"
os.makedirs(outdir, exist_ok=True)

# Only decompress what we need: sources (.cs) + Info/readme
payloads = tmod_lib.read_tmod(path)

count = 0
for name, blob in payloads.items():
    if name.lower().endswith((".cs", ".csproj", ".md", ".txt", ".json", ".fx", ".fxc", ".hjson")):
        safe = name.replace("/", os.sep).replace("\\", os.sep)
        full = os.path.join(outdir, safe)
        os.makedirs(os.path.dirname(full), exist_ok=True)
        if name.lower().endswith(".cs"):
            # strip UTF-8 BOM preservation not needed; write as-is
            with open(full, "wb") as fh:
                fh.write(blob)
            count += 1
        else:
            with open(full, "wb") as fh:
                fh.write(blob)

print("WROTE .cs FILES:", count)
