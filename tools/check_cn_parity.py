# -*- coding: utf-8 -*-
"""检查主模组英文与汉化补丁中文的键是否一一对应，并报告仍未翻译的键。"""
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from check_localization import parse_keys  # noqa: E402

EN = r"E:\开发\WastelandSoul\Localization\en-US_Mods.WastelandSoul.hjson"
CN = r"E:\开发\WastelandSoulCN\Localization\zh-Hans_Mods.WastelandSoul.hjson"
PREFIX = "Mods.WastelandSoul"


def main():
    en = parse_keys(io.open(EN, encoding="utf-8").read(), PREFIX)
    cn = parse_keys(io.open(CN, encoding="utf-8").read(), PREFIX)

    print("en-US 键数: %d" % len(en))
    print("zh-Hans 键数: %d" % len(cn))

    missing = sorted(set(en) - set(cn))
    extra = sorted(set(cn) - set(en))

    # 英文原文本就是空的键（例如自动生成的 Tooltip: ""）不需要中文，不算缺失
    missing_required = [k for k in missing if en[k] not in ('""', "", "<multiline>")]
    missing_optional = [k for k in missing if k not in missing_required]

    print("中文缺失（必须有）: %d" % len(missing_required))
    for key in missing_required:
        print("   -", key, "|", en[key][:60])

    print("中文缺失（英文原文为空，可省）: %d" % len(missing_optional))
    for key in missing_optional:
        print("   ·", key)

    print("中文多余（英文里已不存在）: %d" % len(extra))
    for key in extra:
        print("   +", key)

    untranslated = []

    for key, value in cn.items():
        if value in ('""', "", "<multiline>"):
            continue
        if not re.search(r"[\u4e00-\u9fff]", value):
            untranslated.append(key)

    print("非空但没有中文的键: %d" % len(untranslated))
    for key in untranslated:
        print("   ?", key, "=", cn[key][:70])

    return 1 if (missing_required or extra or untranslated) else 0


if __name__ == "__main__":
    sys.exit(main())
