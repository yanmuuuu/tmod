import sys, os
sys.path.insert(0, r"E:\开发\tools")
from tmod_lib import read_tmod
for p in [r"e:\steam\steamapps\workshop\content\1281930\2785100219\2026.6\SubworldLibrary.tmod",
          r"e:\steam\steamapps\workshop\content\1281930\2785100219\2025.9\SubworldLibrary.tmod",
          r"e:\steam\steamapps\workshop\content\1281930\2785100219\2025.6\SubworldLibrary.tmod"]:
    try:
        f = read_tmod(p, want=["Info"])
        info = f.get("Info", b"").decode("utf-8", "replace")
        line = [l for l in info.splitlines() if "version" in l.lower() or "buildVersion" in l]
        print(p)
        print("   ", " | ".join(line[:4]))
    except Exception as e:
        print(p, "ERR", e)
