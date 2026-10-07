# -*- coding: utf-8 -*-
"""用 tModLoader 自带的 Hjson 解析器真解析本地化文件（而不是只做缩进检查）。

为什么需要它：`check_localization.py` 用的是自写的缩进解析器，
**不校验 Hjson 的引号规则**，所以放过了一个致命错误：
  DislikeBiome: {BiomeName}让我想起被污染的地面。
未加引号的 Hjson 值里出现 `{` / `}` 会被当成语法，tModLoader 直接报
  Found '}' where a key name was expected
并**把整个模组禁用掉**（连主模组一起禁用，因为补丁是硬依赖）。

用法:
  python check_hjson.py                      # 校验主模组 + 汉化补丁的所有本地化文件
  python check_hjson.py <文件.hjson>          # 校验单个文件
"""
import os
import sys

HJSON_CANDIDATES = [
    r"E:\steam\steamapps\common\tModLoader\Libraries\Hjson",
]

TARGET_DIRS = [
    r"E:\开发\WastelandSoul\Localization",
    r"E:\开发\WastelandSoulCN\Localization",
]


def find_hjson():
    """找到 tModLoader 自带的 Hjson.dll。"""
    for root in HJSON_CANDIDATES:
        if not os.path.isdir(root):
            continue
        for dirpath, _dirs, files in os.walk(root):
            for name in files:
                if name.lower() == "hjson.dll":
                    return os.path.join(dirpath, name)
    return None


def main():
    dll = find_hjson()
    if not dll:
        print("!! 找不到 tModLoader 自带的 Hjson.dll，无法做真解析校验")
        print("   查找位置:", HJSON_CANDIDATES)
        return 2

    print("使用解析器: %s" % dll)

    # 通过 pythonnet 不可用时，退回到调用 dotnet 的方式；
    # 这里优先尝试 pythonnet（bundled python 通常没有，所以会走 fallback）。
    try:
        import clr  # noqa: F401
        have_clr = True
    except Exception:  # noqa: BLE001
        have_clr = False

    targets = []

    if len(sys.argv) > 1 and not sys.argv[1].startswith("-"):
        targets = [sys.argv[1]]
    else:
        for folder in TARGET_DIRS:
            if not os.path.isdir(folder):
                continue
            for name in sorted(os.listdir(folder)):
                if name.endswith(".hjson"):
                    targets.append(os.path.join(folder, name))

    if not have_clr:
        print("!! bundled python 没有 pythonnet，无法内联调用 .NET 解析器。")
        print("   请改用 tools/hjson_check/（dotnet 小工具）或 check_hjson_via_dotnet.py")
        return 3

    clr.AddReference(dll)
    from Hjson import JsonValue  # type: ignore  # noqa: N813

    failed = 0

    for path in targets:
        with open(path, encoding="utf-8") as handle:
            text = handle.read()

        try:
            JsonValue.Parse(text)
            print("OK   %s" % path)
        except Exception as exc:  # noqa: BLE001
            failed += 1
            print("FAIL %s\n     %s" % (path, exc))

    print("\n失败 %d / %d" % (failed, len(targets)))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
