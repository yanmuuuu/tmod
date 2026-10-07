"""校验本地化文件：语法/层级 + 代码里引用的键是否都存在。

关键规则（这次踩坑学到的）：
  tModLoader 的本地化文件名 `Localization/<culture>_<prefix>.hjson` 里的 <prefix>
  是**共享前缀**，文件内容必须**相对该前缀**书写。
  例如文件 zh-Hans_Mods.WastelandSoul.hjson 的 prefix = "Mods.WastelandSoul"，
  内容应直接写 Items: { ... }，而不是再套一层 Mods: { WastelandSoul: {...} }。
  多写一层会导致所有键变成双前缀，本地化静默失效（只剩按类名自动生成的英文名）。

另外 tModLoader 会自动把文件重写成 Hjson 风格（无引号键值），所以这里用缩进解析，
同时兼容严格 JSON 与 Hjson。
"""
import os
import re
import sys

_TOOLS = os.path.dirname(os.path.abspath(__file__))
_ROOT = os.path.dirname(_TOOLS)
MOD_ROOT = os.path.join(_ROOT, "WastelandSoul") if os.path.isdir(os.path.join(_ROOT, "WastelandSoul")) else r"E:\开发\WastelandSoul"
LOC_DIR = os.path.join(MOD_ROOT, "Localization")

# 汉化补丁的中文文件在主模组目录之外——**它才是上次出事的那个文件**，
# 所以引号规则检查必须同时覆盖两处。
_PATCH = os.path.join(_ROOT, "WastelandSoulCN", "Localization")
PATCH_LOC_DIR = _PATCH if os.path.isdir(_PATCH) else r"E:\开发\WastelandSoulCN\Localization"
CHECK_DIRS = [LOC_DIR, PATCH_LOC_DIR]

# 完整键字面量的候选（闭合引号之后的一切交给 finish_key 判断是否合法）。
PAT_FULL = re.compile(r'"(Mods\.WastelandSoul\.[A-Za-z0-9_.]*)"')


def is_complete_key(source, quote_end):
    """闭合引号之后的位置是否说明这是一个**完整键字面量**。

    排除两种不是完整键的写法：
      1) 后面（允许空白）跟 "+" —— 拼接片段（如 "...Memory" + Utils.Clamp(...)）；
      2) 后面（允许空白）跟 "(" —— 方法名（如 Language.GetTextValue），不是键。

    不要试图用一条正则搞定：写 ``"..."\\s*(?!\\+)`` 时 ``\\s*`` 是贪婪的，
    负向断言一旦失败就会回退成"吃掉 0 个空白"，断言看到的是空格而不是 ``+``，
    匹配照样成功（这个坑实测踩过）。所以改成匹配后再按位置判断。
    """
    rest = source[quote_end:].lstrip()

    if rest.startswith("+"):
        return False

    if rest.startswith("("):
        return False

    return True


def unquoted_hjson_hazard(value):
    r"""值**没加引号**、但按 Hjson 规则必须加时，返回原因；否则返回 None。

    这是踩过一次致命坑之后加的检查：`check_localization.py` 原来只按缩进解析，
    **完全不看 Hjson 的引号规则**，于是放过了这一类错误——
      DislikeBiome: {BiomeName}让我想起被污染的地面。
    值以 `{` 开头时 Hjson 会把它当**内联对象**解析，撞到 `}` 就报
      Found '}' where a key name was expected
    tModLoader 随即判定整个本地化文件 malformed，
    **把汉化补丁和主模组一起禁用**（补丁是硬依赖），而编译 0/0、静态校验全绿。

    规则来自**真实解析器的实测**（`tools/probe_hjson_rules.py` →
    `tools/hjson_quoting_rules.json`）：18 个用例里只有"值以 `{` 开头"被拒；
    含逗号 / 冒号 / `=` / `;` / `+`、`{` 在中间、`[` 开头、`-`/`#` 开头、
    含未转义引号、含反斜杠**全都合法**。
    所以这里只报一件事：**未加引号的值以 `{` 开头**。
    误报会让人不敢改真问题，所以宁可只报这一条确定的。
    """
    if value == "" or value in ('""', "<multiline>"):
        return None

    if value.startswith('"') and value.endswith('"') and len(value) >= 2:
        return None

    if value.startswith("{"):
        return "值以 '{' 开头（Hjson 会当成内联对象解析，导致整个文件 malformed）"

    return None
PAT_CONCAT = re.compile(r'DialogueKey \+ "([A-Za-z0-9_]+)"(?:\s*\+\s*Main\.rand\.Next\((\d+),\s*(\d+)\))?')
DIALOGUE_PREFIX = "Mods.WastelandSoul.Dialogue.MechanicalCompanion."


def parse_keys(text, prefix):
    """按缩进解析 Hjson/JSON，返回 {完整键: 值}。"""
    lines = text.splitlines()
    stack = []          # [(indent, key)]
    out = {}
    i = 0

    while i < len(lines):
        raw = lines[i]
        i += 1

        body = raw.split("//")[0] if raw.strip().startswith("//") else raw
        if not body.strip():
            continue

        indent = len(body) - len(body.lstrip("\t "))
        stripped = body.strip()

        # 多行块（''' 或 """）——已在下面按键处理时跳过
        if stripped.startswith("'''") or stripped.startswith('"""'):
            continue

        match = re.match(r'^"?(?P<key>[A-Za-z0-9_.\-$]+)"?\s*:\s*(?P<value>.*)$', stripped)
        if not match:
            continue

        key = match.group("key")
        value = match.group("value").strip()

        while stack and stack[-1][0] >= indent:
            stack.pop()

        path = [k for _ind, k in stack] + [key]

        if value in ("", "{", "[", "$parentVal:"):
            # 可能是节，也可能是「值写在下一个缩进块里的多行字符串」
            lookahead = None
            for probe in lines[i:i + 3]:
                if probe.strip():
                    lookahead = probe.strip()
                    break

            if lookahead and (lookahead.startswith("'''") or lookahead.startswith('"""')):
                out[(prefix + "." + ".".join(path)) if prefix else ".".join(path)] = "<multiline>"
                marker = lookahead[:3]
                while i < len(lines) and not lines[i].strip().startswith(marker):
                    i += 1
                i += 1
                continue

            if value == "" and lookahead and ":" not in lookahead:
                # 形如 `Key:` 后面直接跟一个值
                out[(prefix + "." + ".".join(path)) if prefix else ".".join(path)] = lookahead
                i += 1
                continue

            stack.append((indent, key))
            continue

        full = (prefix + "." + ".".join(path)) if prefix else ".".join(path)
        out[full] = value

    return out


def collect_defined():
    defined = {}
    problems = []

    for folder in CHECK_DIRS:
        if not os.path.isdir(folder):
            continue

        for name in sorted(os.listdir(folder)):
            if not name.endswith(".hjson"):
                continue

            parts = name[:-len(".hjson")].split("_", 1)
            culture = parts[0]
            prefix = parts[1] if len(parts) > 1 else ""

            with open(os.path.join(folder, name), encoding="utf-8") as handle:
                text = handle.read()

            keys = parse_keys(text, prefix)
            defined.setdefault(culture, set()).update(keys.keys())

            # ---- 引号规则检查（不做这一步就会漏掉"整个模组被禁用"级别的错误）----
            for line_no, raw in enumerate(text.splitlines(), 1):
                stripped = raw.strip()

                if not stripped or stripped.startswith("//") or stripped.startswith("'''"):
                    continue
                if stripped.startswith("/*") or stripped.startswith("*") or stripped.endswith("*/"):
                    continue
                if stripped in ("{", "}", "},", "'''", '"""'):
                    continue
                if stripped.startswith('"""'):
                    continue

                match = re.match(r'^"?[A-Za-z0-9_.\-$]+"?\s*:\s*(?P<value>.+)$', stripped)
                if not match:
                    continue

                value = match.group("value").strip()

                if not value or value in ("{", "}"):
                    continue
                if value.startswith("'''") or value.startswith('"""'):
                    continue
                if value.startswith('"') and value.endswith('"') and len(value) >= 2:
                    continue

                reason = unquoted_hjson_hazard(value)
                if reason:
                    problems.append("%s\\%s:%d %s -> %s"
                                    % (os.path.basename(folder), name, line_no, reason, value[:60]))

            print("OK  %-42s %4d 条  (前缀: %s)" % (name, len(keys), prefix or "<无>"))

    return defined, problems


def collect_used():
    used = set()
    for root, _dirs, files in os.walk(MOD_ROOT):
        if os.sep + "obj" in root or os.sep + "bin" in root:
            continue
        for name in files:
            if not name.endswith(".cs"):
                continue
            with open(os.path.join(root, name), encoding="utf-8") as handle:
                src = handle.read()
            for match in PAT_FULL.finditer(src):
                key = match.group(1)

                # 末尾带点号的是 "Mods.WastelandSoul." + 变量 这类拼接片段，不是完整键
                if key.endswith(".") or not is_complete_key(src, match.end()):
                    continue

                used.add(key)
            for match in PAT_CONCAT.finditer(src):
                base, low, high = match.group(1), match.group(2), match.group(3)
                if low:
                    for i in range(int(low), int(high)):
                        used.add(DIALOGUE_PREFIX + base + str(i))
                else:
                    used.add(DIALOGUE_PREFIX + base)
    return used


def main():
    defined, quote_problems = collect_defined()
    used = collect_used()

    print("\n代码中引用的键: %d" % len(used))
    failed = False

    # ---- 引号规则：这一项能拦住"整个本地化文件被判 malformed、模组被禁用"的致命错误 ----
    if quote_problems:
        failed = True
        print("\n!! 有 %d 处值未加引号但按 Hjson 规则必须加（会导致 tModLoader 判定文件 malformed 并禁用模组）:"
              % len(quote_problems))
        for item in quote_problems:
            print("   -", item)
    else:
        print("OK  引号规则检查通过（未加引号的值里没有 Hjson 结构字符）")

    for culture, keys in defined.items():
        missing = sorted(used - keys)
        if missing:
            failed = True
            print("\n!! [%s] 缺失 %d 条:" % (culture, len(missing)))
            for key in missing:
                print("   -", key)
        else:
            print("OK  [%s] 代码引用的键全部存在" % culture)

    all_keys = set()
    for keys in defined.values():
        all_keys |= keys

    auto = (".DisplayName", ".Tooltip", ".Description", ".MapEntry", ".SetBonus")
    unused = sorted(k for k in all_keys - used if not k.endswith(auto))

    if unused:
        print("\n提示：%d 条键当前未被代码直接引用（可能是内容自动键或预留）:" % len(unused))
        for key in unused:
            print("   ?", key)

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
