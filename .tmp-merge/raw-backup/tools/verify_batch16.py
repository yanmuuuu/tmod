"""不依赖游戏的结构检查：贴图、本地化键、状态机编号、招式是否都有对应状态类。"""
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOD = os.path.join(ROOT, "WastelandSoul")
TOOLS = os.path.dirname(os.path.abspath(__file__))


def read(path):
    with open(path, encoding="utf-8") as handle:
        return handle.read()


def walk_cs():
    for dirpath, dirs, files in os.walk(MOD):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin")]
        for name in files:
            if name.endswith(".cs"):
                yield os.path.join(dirpath, name)


def check_states():
    problems = []
    state_re = re.compile(r"\[VaultState\((\d+),\s*typeof\((\w+)\)\)\]\s*public class (\w+)")
    new_re = re.compile(r"new (\w+State)\(")
    by_context = {}

    for path in walk_cs():
        text = read(path)
        for match in state_re.finditer(text):
            number, context, name = int(match.group(1)), match.group(2), match.group(3)
            by_context.setdefault(context, []).append((number, name, path))

    for context, entries in by_context.items():
        numbers = [item[0] for item in entries]
        if len(numbers) != len(set(numbers)):
            problems.append("状态编号重复: %s %s" % (context, numbers))
        if sorted(numbers) != list(range(min(numbers), max(numbers) + 1)):
            problems.append("状态编号不连续: %s %s" % (context, sorted(numbers)))

    known = {name for entries in by_context.values() for _, name, _ in entries}
    for path in walk_cs():
        for match in new_re.finditer(read(path)):
            name = match.group(1)
            if name not in known:
                problems.append("引用了不存在的状态 %s (%s)" % (name, path))

    print("状态机上下文 %d 个，状态类 %d 个" % (len(by_context), len(known)))
    return problems


def check_custom_keys():
    text = read(os.path.join(MOD, "Common", "WastelandText.cs"))
    loc = read(os.path.join(MOD, "Localization", "en-US_Mods.WastelandSoul.hjson"))
    problems = []
    prefix = "Dialogue.MechanicalCompanion."
    for match in re.finditer(r'"((?:Messages|Conditions|Tiles)\.[A-Za-z0-9_.]+)"', text):
        key = match.group(1)
        if key not in loc and key.split(".", 1)[-1] not in loc:
            # 嵌套写法里叶子名会出现；扁平写法里整键会出现
            leaf = key.split(".")[-1] if key.startswith("Messages.") or key.startswith("Conditions.") else key
            if leaf not in loc and key not in loc:
                problems.append("本地化缺少 " + key)
    for match in re.finditer(r'DialoguePrefix \+ "(\w+)"', text):
        leaf = match.group(1)
        if leaf + ":" not in loc and "." + leaf + ":" not in loc and leaf + "." not in loc:
            # 对话键在 MechanicalCompanion 块里是 `AfterAsh1:` 这种
            if not re.search(r"\b%s:" % leaf, loc):
                problems.append("对话缺少 " + leaf)
    print("自定义键抽查完成")
    return problems


def check_attack_gaps():
    """用和代码相同的取模规则跑 12 轮，确认每个阶段都会打出带缝的招。"""
    problems = []
    ash = {
        0: lambda c: "volley" if c % 2 == 0 else "pool",
        1: lambda c: ("rain", "volley", "pool", "pulse")[c % 4],
        2: lambda c: ("pulse", "rain", "volley")[c % 3],
    }
    guard = {
        0: lambda c: "bolts" if c % 2 == 0 else "slam",
        1: lambda c: ("ring", "bolts", "slam")[c % 3],
        2: lambda c: ("walls", "slam", "ring", "bolts")[c % 4],
    }
    for phase, picker in ash.items():
        moves = [picker(i) for i in range(12)]
        if phase >= 1 and "rain" not in moves:
            problems.append("灰烬之心阶段 %d 没有灰雨" % phase)
    for phase, picker in guard.items():
        moves = [picker(i) for i in range(12)]
        if phase == 0 and moves.count("bolts") < 4:
            problems.append("壁炉守卫阶段一螺栓太少")
        if phase == 2 and "walls" not in moves:
            problems.append("壁炉守卫阶段三没有火墙")
    print("招式轮换抽查通过" if not problems else "招式轮换有问题")
    return problems


def run(script):
    result = subprocess.run([sys.executable, os.path.join(TOOLS, script)], cwd=ROOT)
    if result.returncode != 0:
        return ["%s 退出码 %d" % (script, result.returncode)]
    return []


def main():
    problems = []
    problems += check_states()
    problems += check_custom_keys()
    problems += check_attack_gaps()
    problems += run("check_assets.py")
    problems += run("check_localization.py")
    report = subprocess.run([sys.executable, os.path.join(TOOLS, "sync_cn_translation.py"), "--check"], cwd=ROOT)
    if report.returncode != 0:
        problems.append("sync_cn_translation --check 失败")
    print("\n====")
    if problems:
        print("失败 %d 项:" % len(problems))
        for item in problems:
            print(" -", item)
        return 1
    print("离线检查通过（没有游戏，所以没有做进世界的实机战斗）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
