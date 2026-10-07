# -*- coding: utf-8 -*-
"""把状态机里 `if (Timer == X)` 这种**精确相等**触发改成 `>=` + 一次性标记。

为什么必须改：状态机的 Timer 每帧自增，但如果某一帧被跳过（掉帧、调试断点、
联机同步延迟导致状态晚进一帧），`Timer` 就会**直接越过**那个值，这个招式**永远不出**，
而且不会有任何报错 —— 属于静默失效里最难查的一类。

改法（每个类）：
  * 加一个实例字段 `private bool firedXxx;`（状态类每次切换都是 new 出来的，天然按次计数）；
  * 条件改成 `if (!firedXxx && Timer >= X)`，进入后立刻 `firedXxx = true;`；
  * 顺手在 OnEnter 里把它复位，这样即使框架将来复用实例也不会只出一次招。
"""
import io
import re

FILES = [
    r"E:\开发\WastelandSoul\Content\NPCs\Bosses\AshHeart\AshHeartStates.cs",
    r"E:\开发\WastelandSoul\Content\NPCs\Bosses\FireplaceGuardian\FireplaceGuardianStates.cs",
    r"E:\开发\WastelandSoul\Content\NPCs\Bosses\Archivist\ArchivistStates.cs",
]

TRIGGER = re.compile(r"^(\s*)if \(Timer == (?P<expr>[^)]+)\) \{\s*$")
CLASS = re.compile(r"^\s*(?:public|internal)\s+class\s+(?P<name>\w+)\s*:")
ONENTER = re.compile(r"^\s*public override void OnEnter\([^)]*\)\s*$")


def fix(path):
    lines = io.open(path, encoding="utf-8").read().split("\n")
    out = []
    classes = {}          # 类名 -> [起始行索引, 字段名]
    class_stack = []
    changes = []

    for index, line in enumerate(lines):
        match = CLASS.match(line)

        if match:
            name = match.group("name")
            field = "fired" + name.replace("State", "")
            classes[name] = field
            class_stack.append((name, index))

        out.append(line)

    # 逐行改写触发点：记录每个类里第几个触发点，字段名带序号
    result = []
    counters = {}

    for index, line in enumerate(lines):
        trigger = TRIGGER.match(line)

        if not trigger:
            result.append(line)
            continue

        owner = None

        for name, start in reversed(class_stack):
            if start < index:
                owner = name
                break

        if owner is None:
            result.append(line)
            continue

        counters[owner] = counters.get(owner, 0) + 1
        field = classes[owner] if counters[owner] == 1 else "%s%d" % (classes[owner], counters[owner])
        indent = trigger.group(1)
        expr = trigger.group("expr").strip()

        result.append("%sif (!%s && Timer >= %s) {" % (indent, field, expr))
        result.append("%s\t%s = true;" % (indent, field))
        changes.append((owner, field, expr))

    # 给每个用到的类补字段 + OnEnter 复位
    text = "\n".join(result)
    used = {}

    for owner, field, _ in changes:
        used.setdefault(owner, []).append(field)

    for owner, fields in used.items():
        # 字段：插在类声明那一行之后
        marker = re.search(r"((?:public|internal)\s+class\s+%s\s+:[^\n]*\n)" % re.escape(owner), text)
        declarations = "".join(
            "\t\t/// <summary>本招是否已经触发过（用 >= 判断，避免掉帧直接跨过阈值）。</summary>\n"
            "\t\tprivate bool %s;\n" % field for field in fields)
        text = text[:marker.end(1)] + declarations + text[marker.end(1):]

        # OnEnter 复位：加在该类第一个 OnEnter 的开头
        class_start = text.index(marker.group(1))
        enter = ONENTER.search(text, class_start)

        if enter:
            brace = text.index("{", enter.end())
            reset = "".join("\n\t\t\t%s = false;" % field for field in fields)
            text = text[:brace + 1] + reset + text[brace + 1:]

    io.open(path, "w", encoding="utf-8", newline="\n").write(text)
    return changes


for path in FILES:
    changed = fix(path)
    print("%s: %d 处" % (path.split("\\")[-1], len(changed)))

    for owner, field, expr in changed:
        print("    %s -> !%s && Timer >= %s" % (owner, field, expr))
