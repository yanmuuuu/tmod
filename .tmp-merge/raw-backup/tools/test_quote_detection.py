# -*- coding: utf-8 -*-
"""反向验证：确认 check_localization 的引号规则检查**真的能抓到**那个致命错误。

做法：把中文文件里一行已加引号的值改回"以 { 开头且不加引号"，
跑一次校验并断言它报错，然后还原。
校验器如果抓不到，这个测试就失败——避免"检查器是摆设"。
"""
import io
import os
import shutil
import subprocess
import sys

CN = r"E:\开发\WastelandSoulCN\Localization\zh-Hans_Mods.WastelandSoul.hjson"
CHECKER = r"E:\开发\tools\check_localization.py"
GOOD = 'DislikeBiome: "{BiomeName}让我想起被污染的地面。"'
BAD = 'DislikeBiome: {BiomeName}让我想起被污染的地面。'


def run_checker():
    # 必须显式指定 encoding 与 PYTHONIOENCODING：Windows 上 subprocess 默认用 GBK
    # 解码，中文输出会变成乱码，断言"引号规则检查通过"就会永远失败。
    env = dict(os.environ)
    env["PYTHONIOENCODING"] = "utf-8"

    result = subprocess.run([sys.executable, CHECKER], capture_output=True, text=True,
                            encoding="utf-8", errors="replace", env=env)
    return result.returncode, (result.stdout or "") + (result.stderr or "")


def main():
    backup = CN + ".parity-bak"
    shutil.copyfile(CN, backup)

    try:
        text = io.open(CN, encoding="utf-8").read()

        if GOOD not in text:
            print("!! 找不到用来测试的那一行，测试无法进行")
            print("   期望存在:", GOOD)
            return 2

        # ---------- 1. 先确认当前（正确）状态是通过的 ----------
        code_before, out_before = run_checker()

        if code_before != 0 or "引号规则检查通过" not in out_before:
            print("!! 正确状态下校验就不通过（说明有别的问题）")
            print("   exit:", code_before)
            print(out_before[-1500:])
            return 1

        print("OK   正确状态：校验通过（exit 0）")

        # ---------- 2. 故意改坏，校验器必须报错 ----------
        io.open(CN, "w", encoding="utf-8", newline="\n").write(text.replace(GOOD, BAD))

        code_after, out_after = run_checker()
        caught = ("以 '{' 开头" in out_after) or ("引号规则" in out_after and "!!" in out_after)

        if code_after == 0 or not caught:
            print("!! 校验器**没有抓到**故意改坏的引号问题（检查器是摆设）")
            print("   exit:", code_after)
            print(out_after[-2000:])
            return 1

        print("OK   改坏后：校验器抓到了（exit %d）" % code_after)

        # 顺便确认报错里点名了那一行
        for line in out_after.splitlines():
            if "以 '{' 开头" in line:
                print("     " + line.strip())

        return 0

    finally:
        shutil.move(backup, CN)
        print("已还原中文文件")


if __name__ == "__main__":
    sys.exit(main())
