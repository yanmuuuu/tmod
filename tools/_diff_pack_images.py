# -*- coding: utf-8 -*-
"""对比 .tmod 里的 .rawimg 与源目录的 .png，找出对不上的具体条目。

用法: python _diff_pack_images.py <tmod路径>
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from tmod_lib import list_entries  # noqa: E402

MOD_ROOT = r"E:\开发\WastelandSoul"


def source_pngs():
    result = set()

    for dirpath, dirs, files in os.walk(MOD_ROOT):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin", ".vs")]
        for name in files:
            if name.endswith(".png"):
                rel = os.path.relpath(os.path.join(dirpath, name), MOD_ROOT)
                result.add(rel.replace("\\", "/").lower())

    return result


def main():
    path = sys.argv[1]
    packed = set()

    for name, _size, _csize in list_entries(path):
        if name.endswith(".rawimg"):
            packed.add(name[: -len(".rawimg")].lower() + ".png")

    source = source_pngs()

    print("包内 %d，源目录 %d" % (len(packed), len(source)))
    print("\n只在源目录里（没被打包）:")
    for item in sorted(source - packed):
        print("   -", item)
    print("\n只在包里（源目录已无）:")
    for item in sorted(packed - source):
        print("   +", item)


if __name__ == "__main__":
    main()
