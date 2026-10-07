# -*- coding: utf-8 -*-
"""自检：确认 check_localization 不会把拼接片段/方法名当成完整本地化键。

背景：`WastelandMemorySystem.MemoryTextKey()` 里有
  "Mods.WastelandSoul.Dialogue.MechanicalCompanion.Memory" + Utils.Clamp(...)
这种拼接片段。一开始的取键正则把它当成了完整键 "…Memory"，
于是校验器报"缺失 1 条"，而那条键本来就不该存在（真实键是 Memory1~Memory4）。

第一次修的写法 `"..."\s*(?!\+)` 是错的：`\s*` 贪婪，负向断言失败后会回退成
"吃掉 0 个空白"，断言看到的是空格而不是 `+`，匹配照样成功。
现在改成「匹配候选 + 按闭合引号后的字符判断」，本脚本就是这条规则的回归测试。
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from check_localization import PAT_FULL, is_complete_key  # noqa: E402

CASES = [
    # (代码行, 是否应被当成完整键)
    ('\t\t\treturn "Mods.WastelandSoul.Dialogue.MechanicalCompanion.Memory" + Utils.Clamp(stage, 1, TotalMemories);', False),
    ('\t\treturn "Mods.WastelandSoul.Dialogue.MechanicalCompanion.MemoryUnstable";', True),
    ('\t\tstring key = "Mods.WastelandSoul." + suffix;', False),
    ('\t\tLanguage.GetTextValue("Mods.WastelandSoul.Messages.FireplaceOpened")', True),
    ('\t\ta = "Mods.WastelandSoul.Messages.BossSummoned";', True),
    ('\t\tGetLocalization("Mods.WastelandSoul.Messages.TerminalRead");', True),
    ('\t\tGetLocalization("Mods.WastelandSoul.Messages.TerminalRead"  );', True),
]


def collect(line):
    return [m.group(1) for m in PAT_FULL.finditer(line) if is_complete_key(line, m.end())]


def main():
    bad = 0

    for line, expected in CASES:
        hits = collect(line)
        ok = bool(hits) == expected
        print("%s  expect=%-5s hits=%s" % ("OK " if ok else "BAD", expected, hits))
        if not ok:
            bad += 1

    print("失败: %d" % bad)
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
