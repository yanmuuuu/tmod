# -*- coding: utf-8 -*-
"""检查「加载期钩子里有没有调图形 / 音频 API」。

为什么必须要查（真实事故，服务端自检抓不到）：

    System.Threading.ThreadStateException: most FNA3D audio/graphics functions
    must be called on the main thread
    [.NET TP Worker/INFO] [tML]: Disabling Mod: WastelandSoul

tModLoader 的模组加载跑在**线程池工作线程**上，而 FNA3D 的图形 / 音频调用**只能在主线程**。
第一版粒子系统把 `new Texture2D(Main.instance.GraphicsDevice, 64, 64)` 写在了
`ModSystem.Load()` 里 → 客户端启动直接抛异常、**整个模组被禁用**。

⚠️ 为什么 `ws_pipeline.ps1` 的隔离加载自检没抓到：
   它跑的是 `-server`，`Main.dedServ == true`，客户端专属代码（包括那段 `if (Main.dedServ) return;`
   之后的图形创建）**根本不会执行**。所以这类问题必须在**静态检查**这一层拦住。

判定：下面这些**加载期钩子**里出现图形 / 音频资源创建 → 失败
（正确做法是主线程懒加载，例如第一次 `Draw()` 里建、或 `Main.RunOnMainThread`）：

    Load / Unload / PostSetupContent / SetStaticDefaults / SetupContent /
    LoadData / AddRecipes / OnModLoad / LoadWorldData / PostSetupContent
"""
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from check_batch_usage import methods, strip_comments  # noqa: E402

MOD = r"E:\开发\WastelandSoul"

LOAD_HOOKS = (
    "Load", "Unload", "PostSetupContent", "SetStaticDefaults", "SetupContent",
    "LoadData", "AddRecipes", "OnModLoad", "LoadWorldData", "LoadContent",
)

# 只能在主线程调的东西
OFFENDERS = [
    (re.compile(r"new\s+Texture2D\s*\("), "new Texture2D(...)"),
    (re.compile(r"new\s+RenderTarget2D\s*\("), "new RenderTarget2D(...)"),
    (re.compile(r"new\s+SoundEffect\s*\("), "new SoundEffect(...)"),
    (re.compile(r"new\s+Effect\s*\("), "new Effect(...)"),
    (re.compile(r"new\s+SpriteBatch\s*\("), "new SpriteBatch(...)"),
    (re.compile(r"Main\.instance\.GraphicsDevice"), "Main.instance.GraphicsDevice"),
    (re.compile(r"\.SetData\s*\("), "Texture2D.SetData(...)"),
]


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
            text = strip_comments(io.open(path, encoding="utf-8-sig", errors="replace").read())

            for method, body in methods(text):
                if method not in LOAD_HOOKS:
                    continue

                scanned += 1

                for pattern, label in OFFENDERS:
                    match = pattern.search(body)

                    if not match:
                        continue

                    line = text.count("\n", 0, text.index(body) + match.start()) + 1
                    problems.append("%s:%d  加载期钩子 %s() 里出现 %s —— "
                                    "加载在线程池工作线程上，FNA3D 图形/音频只能在主线程调用，"
                                    "客户端会抛 ThreadStateException 并**禁用整个模组**"
                                    % (relative, line, method, label))

    if problems:
        print("!! 发现 %d 处「加载期调用图形/音频 API」:" % len(problems))
        for item in problems:
            print("   -", item)
        print("CHECK FAILED")
        return 1

    print("[OK] 扫过 %d 个加载期钩子：没有在其中创建图形/音频资源" % scanned)
    return 0


if __name__ == "__main__":
    sys.exit(main())
