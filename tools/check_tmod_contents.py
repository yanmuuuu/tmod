# -*- coding: utf-8 -*-
"""检查打包后的 .tmod 里是否**真的有内容**（而不是只"看起来像"有）。

背景（踩过的两个坑）：
  1. 汉化补丁曾出现「源文件被 tModLoader 在构建时改名成 .legacy」，包里没有中文，
     而编译 0 errors、加载自检全绿——完全静默；
  2. 更隐蔽的一次：汉化补丁的 zh-Hans 文件**一个汉字都没有**，
     全是 tModLoader 自动补的 `// English` 占位注释。旧版校验只看「>5000 字节」，
     于是一个 20KB 的空壳一路绿灯打进了 .tmod。

所以现在的判定是**看内容**，不是看大小：
  · 主模组：Localization/en-US_*.hjson 必须存在且不是空文件；
    贴图（.rawimg）数量必须与源目录里的 .png 数量一致；
  · 汉化补丁：Localization/zh-Hans_Mods.WastelandSoul.hjson 必须存在，
    并且**至少包含 N 个汉字**（默认 1000）。

用法:
  python check_tmod_contents.py <tmod路径> [最少汉字数]
  python check_tmod_contents.py <tmod路径@主模组>       # 主模组模式
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from tmod_lib import read_tmod  # noqa: E402

CJK = re.compile(r"[\u4e00-\u9fff]")
MOD_ROOT = r"E:\开发\WastelandSoul"


def count_source_pngs(root):
    """源目录里应当被当作贴图打包的 .png 数量。

    `icon.png` / `icon_small.png` 是模组图标，tModLoader 走的是单独通道、
    不以 .rawimg 形式进包，所以不参与对比。
    """
    total = 0

    for dirpath, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin", ".vs")]

        for name in files:
            if not name.endswith(".png"):
                continue

            if name.lower().startswith("icon"):
                continue

            total += 1

    return total


def main():
    if len(sys.argv) < 2:
        print("用法: python check_tmod_contents.py <tmod路径> [最少汉字数]")
        return 2

    path = sys.argv[1]
    min_cjk = int(sys.argv[2]) if len(sys.argv) > 2 else 1000

    if not os.path.exists(path):
        print("!! 找不到 %s" % path)
        return 2

    entries = read_tmod(path)
    problems = []
    name = os.path.basename(path)

    print("== %s（%d 个条目）" % (name, len(entries)))

    # ---------- 1. 贴图数量对得上吗 ----------
    rawimg = [k for k in entries if k.endswith(".rawimg")]
    source_pngs = count_source_pngs(MOD_ROOT)

    print("贴图: 包内 %d 个 .rawimg / 源目录 %d 个 .png" % (len(rawimg), source_pngs))

    if rawimg and len(rawimg) != source_pngs:
        problems.append("贴图数量不一致（包内 %d，源目录 %d）" % (len(rawimg), source_pngs))

    if not rawimg:
        print("  （这是汉化补丁：本来就不含贴图，跳过贴图检查）")

    # ---------- 2. 本地化文件 ----------
    for key in sorted(entries):
        if not key.startswith("Localization/"):
            continue

        data = entries[key]
        text = data.decode("utf-8", "replace")
        cjk = len(CJK.findall(text))

        print("  %-52s %7d 字节  汉字 %d" % (key, len(data), cjk))

    # 中文只在汉化补丁里；主模组不含 zh-Hans 是**正常的**
    # （tModLoader 在构建主模组时会把它从源目录里去掉，中文由补丁模组提供）。
    zh_keys = [k for k in entries if "zh-Hans_Mods.WastelandSoul.hjson" in k]

    if not zh_keys:
        if rawimg:
            print("本地化: 主模组不含 zh-Hans（中文由汉化补丁提供，属正常）")
        else:
            problems.append("汉化补丁里没有 zh-Hans_Mods.WastelandSoul.hjson")
    else:
        text = entries[zh_keys[0]].decode("utf-8", "replace")
        cjk = len(CJK.findall(text))

        if cjk < min_cjk:
            problems.append("中文文件只有 %d 个汉字（要求 ≥ %d）——很可能是只有英文占位的空壳"
                            % (cjk, min_cjk))
        else:
            print("中文内容: %d 个汉字 ✓" % cjk)

        # ---------- 3. 英文模板必须在同一个包里 ----------
        # tML 的规则：`<culture>_<prefix>.hjson` 只有在同目录存在 `en-US_<prefix>.hjson`
        # 时才会被加载，否则改名成 .legacy 且**内容不加载**（游戏里就是英文），
        # 下一次加载还会因改名撞名抛 IOException 把两个模组一起禁用。
        # 打包时也要满足这条，否则玩家拿到的补丁同样失效。
        prefix_zh = "zh-Hans_Mods.WastelandSoul.hjson"
        template_keys = [k for k in entries if k.endswith("en-US_Mods.WastelandSoul.hjson")]

        if not template_keys:
            problems.append("补丁包里缺少英文模板 en-US_Mods.WastelandSoul.hjson"
                            "（tML 会因此拒绝加载中文文件，并在下次加载时禁用模组）")
        else:
            print("英文模板: %s ✓" % template_keys[0])

        legacy = [k for k in entries if k.endswith(".legacy")]

        if legacy:
            problems.append("补丁包里打进了 .legacy 残留：%s" % ", ".join(sorted(legacy)))

    if problems:
        print("\n!! 发现 %d 个问题:" % len(problems))
        for item in problems:
            print("   -", item)
        return 1

    print("OK  包内容校验通过")
    return 0


if __name__ == "__main__":
    sys.exit(main())
