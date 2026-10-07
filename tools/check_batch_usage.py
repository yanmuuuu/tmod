# -*- coding: utf-8 -*-
"""检查「在 SpriteBatch 上 Draw 之前，批次到底是开着的吗」。

为什么需要它（这次的 **Main engine crash**）：

    System.InvalidOperationException: Draw was called, but Begin has not yet been called.
       at WastelandTrailRenderer.Draw(...)
       at WastelandSwingDrawSystem.PostDrawTiles()
    [Main Thread/FATAL] [tML]: Main engine crash

`ModSystem.PostDrawTiles` 里 `Main.spriteBatch` **不在 Begin 状态**（原版在这之前已经 End 了），
直接 Draw 会抛异常，而绘制发生在主线程绘制流程里 → tML 直接判定引擎崩溃、游戏退出。
表现层代码里原本还写着"不重新 Begin/End 批次"这条**错误经验**，正是它导致了这次崩溃。

判定规则（保守，只抓明确错误的写法）：
  * 某个方法里有 `spriteBatch.Draw(...)`：
      - 如果方法名是框架**保证批次已开**的钩子（PreDraw / PostDraw / Draw / DrawTiles 之类）→ 放过；
      - 否则该方法必须**自己有** `spriteBatch.Begin(...)`；
  * `PostDrawTiles` 特判：**必须**同时有 Begin 和 End，且 End 要在 finally 里
    （异常时也不能留下"开着不关"的批次）。
"""
import io
import os
import re
import sys

MOD = r"E:\开发\WastelandSoul"

# 框架调用这些钩子时，批次是开着的（弹幕 / 物品 / 图格绘制等）
PROVIDED_BATCH_HOOKS = (
    "PreDraw", "PostDraw", "Draw", "PreDrawTiles", "DrawTiles", "PostDrawTiles", "DrawPlayer",
)

DRAW = re.compile(r"\b(?:Main\.)?spriteBatch\.Draw\s*\(")
BEGIN = re.compile(r"\b(?:Main\.)?spriteBatch\.Begin\s*\(")
END = re.compile(r"\b(?:Main\.)?spriteBatch\.End\s*\(")


def strip_comments(text):
    """去掉 // 行注释与 /* */ 块注释 —— 不剥的话 `//spriteBatch.Begin(` 会被当成真的 Begin
    （第一次反向验证就是这么被骗过去的）。字符串字面量里的 // 极少见，这里不考虑。"""
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    return re.sub(r"//[^\n]*", "", text)


def methods(text):
    """粗略切出「方法名 + 方法体」：靠大括号配对，够用且不会误伤。"""
    found = []
    header = re.compile(r"(?:public|private|protected|internal)[^;{}()]*\b(\w+)\s*\([^;{}]*\)\s*(?:where[^{]*)?\{")

    for match in header.finditer(text):
        name = match.group(1)
        start = match.end() - 1
        depth = 0
        index = start

        while index < len(text):
            if text[index] == "{":
                depth += 1
            elif text[index] == "}":
                depth -= 1

                if depth == 0:
                    break

            index += 1

        found.append((name, text[start:index + 1]))

    return found


def main():
    if not os.path.isdir(MOD):
        print("!! 找不到工程目录: %s" % MOD)
        return 1

    problems = []
    checked = 0

    for dirpath, dirs, files in os.walk(MOD):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin", ".vs")]

        for name in files:
            if not name.endswith(".cs"):
                continue

            path = os.path.join(dirpath, name)
            text = strip_comments(io.open(path, encoding="utf-8-sig", errors="replace").read())
            relative = os.path.relpath(path, MOD)
            bodies = methods(text)
            file_draws = bool(DRAW.search(text))

            # PostDrawTiles 特判：只要这个文件里存在 spriteBatch.Draw（哪怕是本文件里的辅助方法画的，
            # 我们的刀光就是这样），PostDrawTiles 就必须自己 Begin/End。
            # 第一版只检查"PostDrawTiles 体内有没有直接 Draw"，结果辅助方法一包装就漏检了。
            for method, body in bodies:
                if method != "PostDrawTiles" or not file_draws:
                    continue

                checked += 1
                has_begin = bool(BEGIN.search(body))
                has_end = bool(END.search(body))

                if not (has_begin and has_end):
                    problems.append("%s: PostDrawTiles 必须自己 Begin/End（该文件里有 spriteBatch.Draw）"
                                    "—— 否则 Draw 会抛 InvalidOperationException 并导致 Main engine crash"
                                    % relative)
                elif "finally" not in body:
                    problems.append("%s: PostDrawTiles 的 End 没有放在 finally 里"
                                    "（绘制异常会留下开着不关的批次）" % relative)

            for method, body in bodies:
                if not DRAW.search(body):
                    continue

                checked += 1
                has_begin = bool(BEGIN.search(body))
                has_end = bool(END.search(body))

                if method == "PostDrawTiles":
                    continue

                if has_begin != has_end:
                    problems.append("%s: %s 里 Begin/End 不成对" % (relative, method))

                if not has_begin and method not in PROVIDED_BATCH_HOOKS:
                    problems.append("%s: %s 里直接 Draw 但方法内没有 Begin —— 除非这是框架保证批次已开的钩子"
                                    "（%s）" % (relative, method, ", ".join(PROVIDED_BATCH_HOOKS[:3])))

    if not checked:
        print("!! 一个 spriteBatch.Draw 都没扫到 —— 检查器路径写错了？")
        return 1

    if problems:
        print("!! 发现 %d 处 SpriteBatch 用法问题:" % len(problems))
        for item in problems:
            print("   -", item)
        print("CHECK FAILED")
        return 1

    print("[OK] 扫过 %d 处 spriteBatch.Draw：批次状态都有保证（PostDrawTiles 自己 Begin/End、"
          "钩子交给框架）" % checked)
    return 0


if __name__ == "__main__":
    sys.exit(main())
