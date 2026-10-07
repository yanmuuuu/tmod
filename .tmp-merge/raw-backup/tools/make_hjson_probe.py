# -*- coding: utf-8 -*-
"""生成 Hjson 引号规则的探针本地化文件：**每个模式一个文件**。

为什么要一个模式一个文件：tModLoader 遇到第一个语法错误就中止整个文件的解析，
所以把 12 个模式塞进一个文件只能知道"里面有坏东西"，无法定位到具体哪一条。
拆成独立文件后，坏的文件会被单独禁用，好的文件照常解析，一次跑完就能拿到完整结论。

用法: python make_hjson_probe.py
"""
import io
import os

PROBE_ROOT = r"E:\开发\HjsonParseProbe\Localization"

# 文件名后缀 -> 该文件的原始内容（键: 值）
CASES = [
    ("Plain",
     "\tPlain: hello world"),

    ("CommaMid",
     "\tCommaMid: Every 5 days (warning at 19:30, arrival at 20:30)"),

    ("ColonMid",
     "\tColonMid: 每 5 天一次（19:30 预警、20:30 抵达）"),

    ("BraceStart",
     "\tBraceStart: {BiomeName}让我想起被污染的地面。"),

    ("BraceMid",
     "\tBraceMid: 这里的参数还算合意——{BiomeName}。"),

    ("BracketStart",
     "\tBracketStart: [NPCName] is here"),

    ("BracketMid",
     "\tBracketMid: talk to [NPCName] now"),

    ("QuoteMid",
     "\tQuoteMid: he said \"hi\" loudly"),

    ("DashStart",
     "\tDashStart: -10 percent"),

    ("HashStart",
     "\tHashStart: #1 sample"),

    ("Backslash",
     "\tBackslash: path C:\\temp\\x"),

    ("BraceQuoted",
     "\tBraceQuoted: \"{BiomeName}让我想起被污染的地面。\""),

    ("BraceMidQuoted",
     "\tBraceMidQuoted: \"这里的参数还算合意——{BiomeName}。\""),

    ("PlusSigns",
     "\tPlusSigns: 8 个碎片 + 5 根骨头 + 3 个陨石锭"),

    ("EqualsSign",
     "\tEqualsSign: a = b"),

    ("SemicolonMid",
     "\tSemicolonMid: first; second"),
]


def main():
    os.makedirs(PROBE_ROOT, exist_ok=True)

    # 清掉旧的探针文件
    for name in os.listdir(PROBE_ROOT):
        if name.startswith("en-US_Probe"):
            os.remove(os.path.join(PROBE_ROOT, name))

    for suffix, body in CASES:
        filename = "en-US_Probe%s.hjson" % suffix
        content = "Probe: {\n%s\n}\n" % body

        with io.open(os.path.join(PROBE_ROOT, filename), "w",
                     encoding="utf-8", newline="\n") as handle:
            handle.write(content)

        print("wrote %-34s %s" % (filename, body.strip()[:60]))

    print("\n共 %d 个探针文件" % len(CASES))


if __name__ == "__main__":
    main()
