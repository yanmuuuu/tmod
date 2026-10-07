# -*- coding: utf-8 -*-
r"""给必须带 UTF-8 BOM 的脚本补上 BOM（幂等，可反复运行）。

为什么需要：本机只有 Windows PowerShell 5.1，它会把**没有 BOM** 的 .ps1 按 ANSI 读。
`ws_pipeline.ps1` 里有 `E:\开发\...` 这样的中文路径字面量，一旦按 ANSI 解码就会变成
`E:\寮€鍙...`，参数全部失效（文档里已经踩过一次）。

⚠️ 以前这里写死了两个文件名，结果我新加的 `client_loadtest.ps1` 没被覆盖到，
第一次跑就撞上同一个坑（`missing build output E:\寮€鍙慭.tml-build\...`）。
现在改成**扫描 tools 目录下所有 .ps1** —— 以后新加脚本不会再漏。

所以铁律：**每次用编辑工具改过 .ps1 之后，都要跑一次这个脚本。**

用法:
  python ensure_ps1_bom.py                     # 检查 + 自动补 BOM
  python ensure_ps1_bom.py --check             # 只检查，缺 BOM 时返回 1
"""
import io
import os
import sys

TOOLS = os.path.dirname(os.path.abspath(__file__))

# 例外：明确不需要 BOM 的（目前没有）
SKIP_NAMES = set()

BOM = b"\xef\xbb\xbf"


def targets():
    found = []

    for name in sorted(os.listdir(TOOLS)):
        if not name.lower().endswith(".ps1") or name in SKIP_NAMES:
            continue

        found.append(os.path.join(TOOLS, name))

    return found


def main():
    check_only = "--check" in sys.argv
    problems = 0
    scanned = 0

    for path in targets():
        scanned += 1
        raw = open(path, "rb").read()
        name = os.path.basename(path)

        if raw[:3] == BOM:
            print("OK    %s 已带 BOM" % name)
            continue

        if check_only:
            print("!!    %s 缺少 UTF-8 BOM" % name)
            problems += 1
            continue

        with open(path, "wb") as handle:
            handle.write(BOM + raw)

        print("FIXED %s 已补上 BOM（%d 字节）" % (name, len(raw) + 3))

    print("（扫描了 tools 下 %d 个 .ps1）" % scanned)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
