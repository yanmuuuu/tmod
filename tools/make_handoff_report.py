# -*- coding: utf-8 -*-
"""生成一份验收快照 E:\\开发\\验收报告.md（随时可重跑，用于交付/交接）。

内容：装机包的哈希与体积、工程规模、本地化键数、关键检查器的结果、以及仍未做项。
用法：python tools/make_handoff_report.py
"""
import hashlib
import io
import os
import re
import subprocess
import sys

ROOT = r"E:\开发"
MOD = os.path.join(ROOT, "WastelandSoul")
CN = os.path.join(ROOT, "WastelandSoulCN")
TOOLS = os.path.join(ROOT, "tools")
REAL_MODS = os.path.join(os.environ["USERPROFILE"], "Documents", "My Games", "Terraria", "tModLoader", "Mods")

CHECKS = [
    "check_assets.py", "check_batch_usage.py", "check_load_thread_safety.py",
    "check_no_auto_trails.py", "check_subworld_scope.py", "check_fireplace_layout.py",
    "check_localization.py", "check_tooltip_no_crafting.py", "check_cn_parity.py",
    "check_cn_template.py", "test_key_regex.py", "verify_batch16.py",
]


def sha256(path, limit=None):
    h = hashlib.sha256()
    with open(path, "rb") as handle:
        while True:
            chunk = handle.read(1 << 20)
            if not chunk:
                break
            h.update(chunk)
    return h.hexdigest()[:8].upper()


def count(root, pattern):
    total = 0
    for dirpath, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin", ".vs")]
        total += sum(1 for f in files if f.endswith(pattern))
    return total


def localized_keys(path):
    if not os.path.exists(path):
        return 0
    text = io.open(path, encoding="utf-8-sig", errors="replace").read()
    return len(re.findall(r"^\s*[A-Za-z0-9_.]+\s*:", text, re.M))


def run_checks():
    python = sys.executable
    rows = []
    for name in CHECKS:
        path = os.path.join(TOOLS, name)
        if not os.path.exists(path):
            rows.append((name, "MISSING", ""))
            continue
        proc = subprocess.run([python, path], capture_output=True, text=True, encoding="utf-8", errors="replace")
        last = ""
        for line in (proc.stdout or "").strip().split("\n")[::-1]:
            if line.strip():
                last = line.strip()
                break
        rows.append((name, "OK" if proc.returncode == 0 else "FAIL(%d)" % proc.returncode, last[:70]))
    return rows


def main():
    lines = [u"# 废土魂穿 / WastelandSoul — 验收快照", ""]

    import datetime
    lines.append(u"生成时间：%s" % datetime.datetime.now().strftime("%Y/%m/%d %H:%M:%S"))
    lines.append("")

    lines.append(u"## 1. 装机包")
    lines.append("")
    lines.append(u"| 包 | 体积 | 哈希（前 8 位） | 修改时间 |")
    lines.append(u"| --- | --- | --- | --- |")

    for name in ("WastelandSoul.tmod", "WastelandSoulCN.tmod"):
        path = os.path.join(REAL_MODS, name)
        if os.path.exists(path):
            stamp = datetime.datetime.fromtimestamp(os.path.getmtime(path)).strftime("%m-%d %H:%M:%S")
            lines.append(u"| %s | %.2f MB | `%s` | %s |" % (
                name, os.path.getsize(path) / 1048576.0, sha256(path), stamp))
        else:
            lines.append(u"| %s | **未装机** | - | - |" % name)

    lines += ["", u"## 2. 工程规模", ""]
    lines.append(u"- 源文件（.cs）：**%d**" % count(MOD, ".cs"))
    lines.append(u"- 贴图（.png）：**%d**" % count(MOD, ".png"))
    lines.append(u"- 本地化键：英文 **%d** / 中文 **%d**" % (
        localized_keys(os.path.join(MOD, "Localization", "en-US_Mods.WastelandSoul.hjson")),
        localized_keys(os.path.join(CN, "Localization", "zh-Hans_Mods.WastelandSoul.hjson"))))

    lines += ["", u"## 3. 静态检查器", "", u"| 检查器 | 结果 | 末行 |", u"| --- | --- | --- |"]
    for name, result, last in run_checks():
        lines.append(u"| `%s` | %s | %s |" % (name, result, last.replace("|", "/")))

    lines += ["", u"## 4. 仍需人工确认 / 未做", "",
              u"- 实机验收：壁炉首次进入自动成型、3D 探针（拿「芯片」看头顶机器人）、奖杯 10% 掉落、灵魂碎片只给一次。",
              u"- 需要美术/编码器：智械人 3D 模型与骨骼动画（OBJ 无骨架，须 glTF）、四首 Boss 曲 wav→ogg 转码、Boss 旗帜/纪念章以外的美术。",
              u"- 详细批次记录见 `WastelandSoul/开发说明.md`（批次 1–28+）。"]

    out = os.path.join(ROOT, u"验收报告.md")
    io.open(out, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    print(u"已生成：%s" % out)

    for row in run_checks.__wrapped__() if False else []:
        pass


if __name__ == "__main__":
    main()
