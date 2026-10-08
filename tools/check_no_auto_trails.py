# -*- coding: utf-8 -*-
"""**合并守则**检查器：禁止「自动挂在所有弹幕 / NPC 上的线状特效」。

玩家的原话（两次）：
  1. 「用mod里面的武器时发射的弹幕和boss发射的弹幕都会有一条白线，这样会干扰视野」
  2. 「把我之前跟你说的影响视线的光线去掉，合并之后又加回来了，之后合并也不要这个」

它被加回来过两次，两次都藏在"通用表现层"里：
  * 第一版：`WastelandTrailGlobalProjectile : GlobalProjectile` —— 给**每一个**本模组弹幕
    记 12 个历史点、用 1x1 白点拉成 16px 宽的带子（无职业伤害的弹幕还会落到近白兜底色）。
  * 第二版（合并另一条线时带回来的）：`WastelandFxProjectile : GlobalProjectile` 的 `PostAI`
    —— 每 2 帧在所有本模组弹幕身后喷一颗 `Color.Lerp(color, White, 0.35f)` 的火花；
    外加清道夫氛围每 ~18 帧一道折线闪电、归档者光束上叠了一条 620px 的近白闪电线。

所以规则写死在这里（**跨分支、永久生效**）：

  A. `GlobalProjectile` 子类的**每帧**钩子（AI / PostAI / PreDraw / PostDraw / PostDrawBehind）
     里不许出现任何特效生成或拖尾绘制；
     （`OnKill` 之类的**一次性**钩子允许 —— 命中爆开是原地小团粒子，不遮挡视野。）
  B. `GlobalNPC` 子类的每帧钩子里不许出现 `Bolt`（线状）。
  C. `WastelandFxSystem.Bolt(...)` 只允许出现在 `Content/` 下（具体某个招式自己的表现），
     不许出现在 `Common/`（通用/氛围层）。

要加拖尾：在那个弹幕自己的 `ModProjectile.PreDraw/PostDraw` 里手动画，短历史 + 明确颜色。
"""
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from check_batch_usage import strip_comments  # noqa: E402

MOD = r"E:\开发\WastelandSoul"

PER_FRAME_HOOKS = ("AI", "PostAI", "PreDraw", "PostDraw", "PostDrawBehind", "DrawBehind", "Draw")
FX_CALLS = re.compile(
    r"WastelandTrailRenderer\s*\.\s*Draw|WastelandFxSystem\s*\.\s*(Spark|Glow|Embers|Motes|Flash|Ring|Burst|Bolt)")
BOLT = re.compile(r"WastelandFxSystem\s*\.\s*Bolt\s*\(")


def classes(text):
    """产出 (类名, 基类, 类体, 类体在原文里的起始下标)。"""
    for match in re.finditer(r"(?:public|internal)?\s*(?:sealed\s+|abstract\s+|partial\s+)*class\s+(\w+)\s*:\s*([\w.<>,\s]+?)\s*\{", text):
        name = match.group(1)
        base = match.group(2).strip()
        depth = 1
        index = match.end()

        while index < len(text) and depth > 0:
            if text[index] == "{":
                depth += 1
            elif text[index] == "}":
                depth -= 1
            index += 1

        yield name, base, text[match.end():index - 1], match.end()


def methods_of(body):
    """极简方法切分：够用即可（找 `签名 {` 然后配对花括号）。"""
    for match in re.finditer(r"(?:public|private|protected|internal|static|override|virtual|sealed|\s)+[\w.<>\[\]]+\s+(\w+)\s*\([^;{}]*\)\s*\{", body):
        name = match.group(1)
        depth = 1
        index = match.end()

        while index < len(body) and depth > 0:
            if body[index] == "{":
                depth += 1
            elif body[index] == "}":
                depth -= 1
            index += 1

        yield name, match.start(), body[match.end():index - 1]


def main():
    if not os.path.isdir(MOD):
        print("!! 找不到工程目录: %s" % MOD)
        return 1

    problems = []
    scanned = 0

    for dirpath, dirs, files in os.walk(MOD):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin", ".vs")]

        for name in files:
            if not name.endswith(".cs"):
                continue

            path = os.path.join(dirpath, name)
            relative = os.path.relpath(path, MOD)
            raw = io.open(path, encoding="utf-8-sig", errors="replace").read()
            text = strip_comments(raw)
            lines = raw.split("\n")

            for class_name, base, body, body_start in classes(text):
                is_projectile = "GlobalProjectile" in base
                is_npc = "GlobalNPC" in base

                if not (is_projectile or is_npc):
                    continue

                scanned += 1

                for method, offset, method_body in methods_of(body):
                    if method not in PER_FRAME_HOOKS:
                        continue

                    if is_projectile and FX_CALLS.search(method_body):
                        line = text.count("\n", 0, body_start + offset) + 1
                        problems.append(
                            "%s:%d  %s.%s() 在**每帧**给所有弹幕挂特效/拖尾 —— 玩家明确要求去掉"
                            "（影响视线），合并也不许加回来" % (relative, line, class_name, method))

                    if is_npc and BOLT.search(method_body):
                        line = text.count("\n", 0, body_start + offset) + 1
                        problems.append(
                            "%s:%d  %s.%s() 在**每帧**给 NPC 画折线闪电（线状特效）—— 同上，禁止"
                            % (relative, line, class_name, method))

            # 规则 C：Bolt 只能出现在具体内容里
            if BOLT.search(text) and relative.replace("\\", "/").startswith("Common/"):
                problems.append(
                    "%s  通用/氛围层（Common/）里调用了 WastelandFxSystem.Bolt —— "
                    "线状特效只允许出现在具体招式自己的 Content/ 代码里" % relative)

    if problems:
        print("!! 发现 %d 处「自动挂在所有弹幕/NPC 上的线状特效」：" % len(problems))
        for item in problems:
            print("   -", item)
        print("CHECK FAILED")
        return 1

    print("[OK] 扫过 %d 个 GlobalProjectile/GlobalNPC：没有自动拖尾或线状特效"
          "（规则见本文件顶部「合并守则」）" % scanned)
    return 0


if __name__ == "__main__":
    sys.exit(main())
