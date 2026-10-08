# -*- coding: utf-8 -*-
"""检查「世界上定期重试/改地形的全局系统」有没有**子世界守卫**。

为什么需要（批次 28 的真实事故）：
`FireplaceGateSystem.PostUpdateWorld` 每 60 tick 尝试在出生点旁放一座壁炉门，
`TryPlaceAt()` 每次先 `WorldGen.KillTile` 掉 12 格。它**没有判断自己在哪个世界** ——
玩家进入壁炉子世界后，同一段逻辑仍在跑，而子世界的"出生点上方第一块实心"是塔顶场地外壳，
于是每轮把 `SurfaceYAt()` 的命中点往下推，**每秒往下啃 4 行、宽 81 格**，
一路啃穿塔顶场地 → 灰烬层 → 堡垒屋顶 → 门厅楼板。玩家看到的就是"瑜钢合金自己消失了"。

规则：**在 `ModSystem` 的每帧/每世界 tick 钩子（PostUpdateWorld / PreUpdateWorld / UpdateWorld /
PostUpdate / PreUpdate）里，只要出现"改地形/放家具"的调用，方法体内就必须同时出现子世界判定**
（`SubworldSystem.Current != null` / `SubworldSystem.IsActive<...>()` / `SubworldSystem.AnyActive...`）。

例外：`Content/Subworlds/**` 里的生成器（那些本来就是给子世界生成用的）。
"""
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from check_batch_usage import methods, strip_comments  # noqa: E402

MOD = r"E:\开发\WastelandSoul"

TICK_HOOKS = ("PostUpdateWorld", "PreUpdateWorld", "UpdateWorld", "PostUpdate", "PreUpdate")
MUTATORS = re.compile(
    r"\bWorldGen\s*\.\s*(KillTile|PlaceTile|KillWall|PlaceWall|PlaceLiquid|SquareTileFrame|SquareWallFrame|TileRunner|digTunnel)"
    r"|\bWorldPaint\s*\.\s*(SetTile|Retile|ClearTile|SetWall|SetLiquid|Rect|Carve|WallRect|LiquidRect|Frame|HLine|VLine|Ellipse)")
GUARD = re.compile(r"SubworldSystem\s*\.\s*(Current|IsActive|AnyActive)")

SKIP_DIRS = ("obj", "bin", ".vs")


def main():
    problems = []
    scanned = 0

    for dirpath, dirs, files in os.walk(MOD):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]

        for name in files:
            if not name.endswith(".cs"):
                continue

            path = os.path.join(dirpath, name)
            relative = os.path.relpath(path, MOD)

            # 子世界生成器本人不需要守卫（它就是在子世界里干活的）
            if relative.replace("\\", "/").startswith("Content/Subworlds/"):
                continue

            text = strip_comments(io.open(path, encoding="utf-8-sig", errors="replace").read())

            for method, body in methods(text):
                if method not in TICK_HOOKS:
                    continue

                scanned += 1
                mutator = MUTATORS.search(body)

                if mutator and not GUARD.search(body):
                    line = text.count("\n", 0, text.index(body) + mutator.start()) + 1
                    problems.append(
                        "%s:%d  %s() 里改了地形/放了东西（%s）但**没有子世界守卫** —— "
                        "玩家在子世界里时这段逻辑会用主世界的坐标继续挖，"
                        "批次 28 就是这样把堡垒啃穿的（加 SubworldSystem 判定）"
                        % (relative, line, method, mutator.group(0).strip()))

    if problems:
        print("!! 发现 %d 处「缺子世界守卫」：" % len(problems))
        for item in problems:
            print("   -", item)
        print("CHECK FAILED")
        return 1

    print("[OK] 扫过 %d 个 tick 钩子：改地形的全局系统都有子世界守卫" % scanned)
    return 0


if __name__ == "__main__":
    sys.exit(main())
