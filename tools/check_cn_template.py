# -*- coding: utf-8 -*-
"""校验汉化补丁满足 tModLoader 的「英文模板」规则 —— 不满足就会静默失效并禁用模组。

tModLoader 的原文（client.log）：

    The .hjson file "...\\WastelandSoulCN\\Localization/zh-Hans_Mods.WastelandSoul.hjson"
    was detected as a localization file but doesn't match the filename of any of the
    English template files. The file will be renamed to "...hjson.legacy" and its contents
    will not be loaded.

规则：**同一目录下必须存在 `en-US_<prefix>.hjson`，`<culture>_<prefix>.hjson` 才会被加载。**
缺模板的后果是双重的（都属于"静默失效"）：

  1. 中文根本不会被读取 —— 游戏里显示英文，但编译/加载全绿；
  2. tML 把中文文件改名成 `.legacy`，**下一次**加载再改名时 `File.Move(overwrite:false)`
     撞名抛 `IOException`，于是 `Disabling Mod: WastelandSoulCN` + 主模组一起被禁用。

本检查器同时兜住第二层：只要目录里还剩 `*.legacy`，就说明上一次加载发生了改名，直接判失败。
"""
import io
import os
import re
import sys

PATCH_LOC = r"E:\开发\WastelandSoulCN\Localization"
MAIN_LOC = r"E:\开发\WastelandSoul\Localization"
PREFIX = "Mods.WastelandSoul"

PATTERN = re.compile(r"^(?P<culture>[a-zA-Z-]+)_(?P<prefix>.+)\.hjson$")


def main():
    if not os.path.isdir(PATCH_LOC):
        print("!! 找不到汉化补丁目录: %s" % PATCH_LOC)
        return 1

    files = sorted(os.listdir(PATCH_LOC))
    failed = []
    checked = 0

    legacy = [name for name in files if name.endswith(".legacy")]

    if legacy:
        print("!! 目录里残留 %d 个 .legacy（说明上一次加载把某个本地化文件改名了）：" % len(legacy))
        for name in legacy:
            print("     " + name)
        print("   → 下一次加载会因此抛 IOException 并禁用模组；跑一次 ws_pipeline.ps1 会自动清掉。")
        failed.append("legacy-leftover")
    else:
        print("[OK] 没有 *.legacy 残留")

    for name in files:
        if name.endswith(".legacy"):
            continue

        match = PATTERN.match(name)

        if not match:
            continue

        prefix = match.group("prefix")
        culture = match.group("culture")
        template = os.path.join(PATCH_LOC, "en-US_%s.hjson" % prefix)
        checked += 1

        if not os.path.exists(template):
            print("!! %s（%s）在补丁目录里没有对应的英文模板 en-US_%s.hjson"
                  % (name, culture, prefix))
            print("   → tML 会把它改名成 .legacy 并且**不加载里面的内容**（中文等于没生效）")
            failed.append("missing-template:" + name)
            continue

        print("[OK] %s 有英文模板 en-US_%s.hjson（%d 字节）"
              % (name, prefix, os.path.getsize(template)))

        # 模板与主模组的 en-US 不一致只提示，不算失败：
        # tML 自己会重写这些文件，内容本来就可能被削成"最小集"
        main_en = os.path.join(MAIN_LOC, "en-US_%s.hjson" % prefix)

        if os.path.exists(main_en):
            with io.open(template, "rb") as handle:
                template_bytes = handle.read()
            with io.open(main_en, "rb") as handle:
                main_bytes = handle.read()

            if template_bytes != main_bytes:
                print("     [NOTE] 与主模组 en-US 不完全一致（tML 重写过，正常；"
                      "流水线每次都从主模组重新复制模板）")

    if checked == 0:
        print("!! 补丁目录里一个 `<culture>_<prefix>.hjson` 都没有 —— 中文不可能生效")
        failed.append("no-localization-file")

    if failed:
        print("CHECK FAILED: " + ", ".join(failed))
        return 1

    print("CHECK OK: 汉化补丁满足 tML 的英文模板规则（中文会被真正加载）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
