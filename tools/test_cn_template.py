# -*- coding: utf-8 -*-
"""反向验证：把汉化补丁的**英文模板**临时挪走，`check_cn_template.py` 必须报错。

为什么值得单独测一次：
  这条规则（`<culture>_<prefix>.hjson` 必须有同目录的 `en-US_<prefix>.hjson` 才会被加载）
  已经**两次静默生效**过：
    * 中文根本没被加载（游戏里全是英文），而编译、静态校验、加载自检全绿；
    * 它改名出的 `.legacy` 让下一次加载抛 IOException，把补丁和主模组一起禁用。
  校验器如果本身失效（比如路径写错、匹配不到文件），我们会第三次踩同一个坑——
  所以这里故意破坏现场，断言校验器**必须**失败，然后还原。
"""
import io
import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PATCH_LOC = r"E:\开发\WastelandSoulCN\Localization"
TEMPLATE = os.path.join(PATCH_LOC, "en-US_Mods.WastelandSoul.hjson")
PARKED = TEMPLATE + ".parked-for-test"
CHECKER = os.path.join(HERE, "check_cn_template.py")


def run_checker():
    env = dict(os.environ)
    env["PYTHONIOENCODING"] = "utf-8"
    proc = subprocess.run([sys.executable, CHECKER], capture_output=True, text=True,
                          encoding="utf-8", errors="replace", env=env)
    return proc.returncode, (proc.stdout or "") + (proc.stderr or "")


def main():
    if not os.path.exists(TEMPLATE):
        print("!! 模板不存在，先跑一次 sync_cn_translation.py 再生成本测试：%s" % TEMPLATE)
        return 1

    code, output = run_checker()

    if code != 0:
        print("!! 前提不成立：模板在的情况下校验器都失败了")
        print(output)
        return 1

    print("[OK] 模板存在时校验器通过")

    shutil.move(TEMPLATE, PARKED)
    problems = []

    try:
        code, output = run_checker()

        if code == 0:
            print("!! 模板被挪走后校验器**仍然通过** —— 它是个摆设！")
            problems.append("checker-blind")
        elif "missing-template" not in output:
            print("!! 校验器失败了，但不是因为缺模板：")
            print(output)
            problems.append("wrong-reason")
        else:
            print("[OK] 模板被挪走后校验器正确报错 missing-template")
            for line in output.splitlines():
                if "missing-template" in line or "英文模板" in line:
                    print("     " + line.strip())
    finally:
        shutil.move(PARKED, TEMPLATE)

        if not os.path.exists(TEMPLATE):
            print("!! 还原失败，模板还在 %s，请手工改回" % PARKED)
            return 1

        print("[OK] 已还原模板")

    code, output = run_checker()

    if code != 0:
        print("!! 还原后校验器仍然失败：")
        print(output)
        problems.append("not-restored")

    if problems:
        print("TEST FAILED: " + ", ".join(problems))
        return 1

    print("TEST OK: 缺英文模板一定会被判定为失败，并且现场能还原")
    return 0


if __name__ == "__main__":
    sys.exit(main())
