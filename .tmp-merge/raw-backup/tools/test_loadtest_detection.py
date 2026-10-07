# -*- coding: utf-8 -*-
"""反向验证：拿**真实抓到**的坏加载日志回放，确认流水线的判定规则一定会报失败。

背景（第四次「自检骗自己」，这次差点让汉化补丁整个失效）：

  tModLoader 每次加载都会把 `zh-Hans_Mods.WastelandSoul.hjson` 改名成 `.hjson.legacy`
  （`File.Move(..., overwrite: false)`）。上一次留下的 `.legacy` 还在时，这一步抛
  `System.IO.IOException`，tModLoader 的做法是**禁用汉化补丁与主模组**，然后
  **不带这两个模组再跑一遍**——那一遍照样打印 `Adding Content`（别的模组）并走到
  `Choose World`。

  旧的判定只有 `Disabling`（日志里写的是 `automatically disabled`），而且
  `Adding Content` 那条断言在匹配**内部名**，而日志打的是**显示名** → 永远 0 条命中。
  于是「补丁被整个禁用」这件事一路绿灯。

本测试做两件事（都从**真实来源**读，不是抄一份镜像）：

  1. 从 `ws_pipeline.ps1` 里**读出现网使用的 `$patterns`**（保证测试不会与流水线漂移），
     回放 `fixtures/loadtest_disabled_mod.log`（关键行摘自 2026-10-07 的真实日志
     `E:\\开发\\.tml-loadtest-cn\\out-215337.log`），断言：
       * 坏行必须被 `$patterns` 命中；
       * 两个模组的 displayName（从各自 `build.txt` 读）都能在 `Adding Content` 里找到，
         并且都能在 `Unloading:` 里找到 —— 即新的「加了又被卸载」判定必然触发。
  2. 造一份**正常**日志，断言它不会误报（0 条坏行、0 条卸载）。

退出码 0 = 判定规则有效；非 0 = 规则失效（漏报或误报）。
"""
import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PIPELINE = os.path.join(HERE, "ws_pipeline.ps1")
FIXTURE = os.path.join(HERE, "fixtures", "loadtest_disabled_mod.log")

MOD_SOURCES = [r"E:\开发\WastelandSoul", r"E:\开发\WastelandSoulCN"]


def read_patterns():
    """从流水线源码里取出现网使用的失败模式（不镜像、不硬编码）。"""
    text = io.open(PIPELINE, encoding="utf-8-sig").read()
    match = re.search(r"^\s*\$patterns\s*=\s*'([^']+)'", text, re.M)

    if not match:
        print("!! 在 ws_pipeline.ps1 里找不到 $patterns")
        return None

    return match.group(1)


def display_name(source):
    build = os.path.join(source, "build.txt")

    if not os.path.exists(build):
        return None

    for line in io.open(build, encoding="utf-8-sig").read().splitlines():
        if re.match(r"^\s*displayName\s*=", line):
            return line.split("=", 1)[1].strip()

    return None


def main():
    patterns = read_patterns()

    if patterns is None:
        return 1

    print("流水线失败模式: %s" % patterns)

    if not os.path.exists(FIXTURE):
        print("!! 找不到坏日志样本: %s" % FIXTURE)
        return 1

    bad_log = io.open(FIXTURE, encoding="utf-8").read().splitlines()
    failed = []

    # ---------------------------------------------------------------- 1) 坏日志必须报错
    bad_lines = [line for line in bad_log if re.search(patterns, line, re.I)]

    if not bad_lines:
        print("!! 坏日志一行都没命中 —— 判定规则漏报！")
        failed.append("missed-bad-log")
    else:
        print("坏日志命中 %d 行（应当 >= 1）:" % len(bad_lines))
        for line in bad_lines[:5]:
            print("   * %s" % line.strip())

    for source in MOD_SOURCES:
        name = display_name(source)

        if not name:
            print("!! 读不到 displayName: %s" % source)
            failed.append("no-displayname")
            continue

        added = [line for line in bad_log if ("Adding Content: " + name) in line]
        unloaded = [line for line in bad_log
                    if line.strip().startswith("Unloading:")
                    and line.split("Unloading:", 1)[1].strip().startswith(name)]

        print("  '%s': Adding Content %d 条 / Unloading %d 条" % (name, len(added), len(unloaded)))

        if not added:
            failed.append("fixture-missing-added:" + name)

        if not unloaded:
            print("!! 「加了又被卸载」这条判定在这份坏日志上不会触发 —— 漏报！")
            failed.append("missed-unload:" + name)

    # ---------------------------------------------------------------- 2) 正常日志不许误报
    good_log = list(bad_log[:7]) + [
        "Adding Recipes...",
        "Terraria Server v1.4.4.9 - tModLoader v2026.8.3.0",
        "Choose World: ",
    ]
    false_positives = [line for line in good_log if re.search(patterns, line, re.I)]

    if false_positives:
        print("!! 正常日志被误判:")
        for line in false_positives:
            print("   * %s" % line.strip())
        failed.append("false-positive")

    for source in MOD_SOURCES:
        name = display_name(source)
        unloaded = [line for line in good_log
                    if line.strip().startswith("Unloading:")
                    and line.split("Unloading:", 1)[1].strip().startswith(name)]

        if unloaded:
            failed.append("false-unload:" + name)

    if failed:
        print("TEST FAILED: " + ", ".join(failed))
        return 1

    print("TEST OK: 坏日志必被判定为失败，正常日志不会误报")
    return 0


if __name__ == "__main__":
    sys.exit(main())
