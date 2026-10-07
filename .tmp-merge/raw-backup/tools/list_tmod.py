"""列出 .tmod 里的条目（可过滤关键字，看贴图/本地化文件命名）。

用法:
  python list_tmod.py <tmod路径> [关键字]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from tmod_lib import list_entries  # noqa: E402


def main():
    path = sys.argv[1]
    keyword = sys.argv[2] if len(sys.argv) > 2 else ""

    entries = list_entries(path)
    hits = [e for e in entries if keyword.lower() in e[0].lower()]
    print("%s  条目总数 %d，匹配 %d" % (os.path.basename(path), len(entries), len(hits)))
    for name, size, csize in hits[:80]:
        print("  %-72s %8d -> %8d" % (name, size, csize))


if __name__ == "__main__":
    main()
