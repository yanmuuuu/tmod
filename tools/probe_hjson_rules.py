# -*- coding: utf-8 -*-
"""探明 tModLoader 打包的 Hjson 解析器**到底接受哪些未加引号的值**。

做法：二分定位。每轮把所有"还没定论"的探针文件塞进探针模组跑一次；
tModLoader 会把坏文件逐个报告出来（好的文件照常解析），
所以一轮就能把"本轮里坏的那些"挑出来，剩下的进下一轮。约 4 轮收敛。

结论写进 E:\\开发\\tools\\hjson_quoting_rules.json，供 check_localization.py 使用。

用法: python probe_hjson_rules.py
"""
import io
import json
import os
import re
import shutil
import subprocess
import sys
import time

TML_DIR = r"e:\steam\steamapps\common\tModLoader"
PROBE_MOD = r"E:\开发\HjsonParseProbe"
PROBE_LOC = os.path.join(PROBE_MOD, "Localization")
SAVE_DIR = r"E:\开发\.tml-build"
TEST_DIR = r"E:\开发\.tml-loadtest-probe"
RULES_PATH = r"E:\开发\tools\hjson_quoting_rules.json"

# 用例名 -> 原始值（写进 `Probe: { <键>: <值> }`）
CASES = {
    "Plain": "hello world",
    "CommaMid": "Every 5 days (warning at 19:30, arrival at 20:30)",
    "ColonMid": "每 5 天一次（19:30 预警、20:30 抵达）",
    "BraceStart": "{BiomeName}让我想起被污染的地面。",
    "BraceMid": "这里的参数还算合意——{BiomeName}。",
    "BracketStart": "[NPCName] is here",
    "BracketMid": "talk to [NPCName] now",
    "QuoteMid": 'he said "hi" loudly',
    "DashStart": "-10 percent",
    "HashStart": "#1 sample",
    "Backslash": "path C:\\temp\\x",
    "BraceQuoted": '"{BiomeName}让我想起被污染的地面。"',
    "BraceMidQuoted": '"这里的参数还算合意——{BiomeName}。"',
    "PlusSigns": "8 个碎片 + 5 根骨头 + 3 个陨石锭",
    "EqualsSign": "a = b",
    "SemicolonMid": "first; second",
    "CommaEnd": "trailing comma,",
    "CurlyEnd": "ends with brace}",
}


def write_probe(active):
    """只把 active 里的用例写成探针文件，其余清掉。"""
    for name in os.listdir(PROBE_LOC):
        if name.startswith("en-US_Probe"):
            os.remove(os.path.join(PROBE_LOC, name))

    for case in active:
        content = "Probe: {\n\t%s: %s\n}\n" % (case, CASES[case])
        with io.open(os.path.join(PROBE_LOC, "en-US_Probe%s.hjson" % case),
                     "w", encoding="utf-8", newline="\n") as handle:
            handle.write(content)


def build_and_load(timeout=150):
    """构建探针并加载一次，返回被判 malformed 的用例名集合。"""
    subprocess.run(
        ["dotnet", os.path.join(TML_DIR, "tModLoader.dll"), "-build", PROBE_MOD,
         "-tmlsavedirectory", SAVE_DIR],
        cwd=TML_DIR, capture_output=True, text=True, errors="replace")

    shutil.rmtree(TEST_DIR, ignore_errors=True)
    os.makedirs(os.path.join(TEST_DIR, "Mods"), exist_ok=True)
    shutil.copyfile(os.path.join(SAVE_DIR, "Mods", "HjsonParseProbe.tmod"),
                    os.path.join(TEST_DIR, "Mods", "HjsonParseProbe.tmod"))
    with io.open(os.path.join(TEST_DIR, "Mods", "enabled.json"), "w", encoding="utf-8") as handle:
        handle.write('["HjsonParseProbe"]')

    out_path = os.path.join(TEST_DIR, "out.log")
    with io.open(out_path, "w", encoding="utf-8") as out:
        proc = subprocess.Popen(
            ["dotnet", os.path.join(TML_DIR, "tModLoader.dll"), "-server",
             "-tmlsavedirectory", TEST_DIR],
            cwd=TML_DIR, stdout=out, stderr=subprocess.DEVNULL)

        deadline = time.time() + timeout
        while time.time() < deadline:
            time.sleep(2)
            try:
                text = io.open(out_path, encoding="utf-8", errors="replace").read()
            except OSError:
                continue
            if "Choose World" in text:
                break

        proc.terminate()
        try:
            proc.wait(timeout=10)
        except subprocess.TimeoutExpired:
            proc.kill()

    text = io.open(out_path, encoding="utf-8", errors="replace").read()
    rejected = set(re.findall(r"en-US_Probe(\w+)\.hjson", text))
    return rejected


def main():
    remaining = set(CASES)
    accepted = set()
    rejected = set()

    round_no = 0
    while remaining and round_no < 6:
        round_no += 1
        active = sorted(remaining)
        print("第 %d 轮：测试 %d 个用例" % (round_no, len(active)))

        write_probe(active)
        bad = build_and_load()

        if not bad:
            accepted |= remaining
            print("   全部通过")
            break

        # 只把"本轮确实被判坏"的留下，其余定为通过
        bad &= remaining
        accepted |= (remaining - bad)
        rejected |= bad
        remaining = bad

        print("   坏 %d 个: %s" % (len(bad), ", ".join(sorted(bad))))

    results = {case: ("reject" if case in rejected else "accept") for case in CASES}

    with io.open(RULES_PATH, "w", encoding="utf-8") as handle:
        json.dump(results, handle, ensure_ascii=False, indent=2, sort_keys=True)

    print("\n=== 结论 ===")
    for case in sorted(results):
        print("  %-7s %s" % (results[case], case))
    print("\n已写入 %s" % RULES_PATH)

    write_probe(CASES)   # 还原全部探针文件，方便手工复查
    return 0


if __name__ == "__main__":
    sys.exit(main())
