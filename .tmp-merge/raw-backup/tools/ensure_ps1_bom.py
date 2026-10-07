# -*- coding: utf-8 -*-
r"""给必须带 UTF-8 BOM 的脚本补上 BOM（幂等，可反复运行）。

为什么需要：本机只有 Windows PowerShell 5.1，它会把**没有 BOM** 的 .ps1 按 ANSI 读。
`ws_pipeline.ps1` 里有 `E:\开发\...` 这样的中文路径字面量，一旦按 ANSI 解码就会变成
`E:\寮€鍙...`，参数全部失效（文档里已经踩过一次）。

所以铁律：**每次用编辑工具改过 ws_pipeline.ps1 之后，都要跑一次这个脚本。**

用法:
  python ensure_ps1_bom.py                     # 检查 + 自动补 BOM
  python ensure_ps1_bom.py --check             # 只检查，缺 BOM 时返回 1
"""
import io
import os
import sys

TARGETS = [
    r"E:\开发\tools\ws_pipeline.ps1",
    r"E:\开发\tools\build_and_loadtest.ps1",
]

BOM = b"\xef\xbb\xbf"


def main():
    check_only = "--check" in sys.argv
    problems = 0

    for path in TARGETS:
        if not os.path.exists(path):
            print("SKIP  %s（不存在）" % path)
            continue

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

    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
