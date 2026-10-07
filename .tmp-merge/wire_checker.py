# -*- coding: utf-8 -*-
"""把 check_load_thread_safety 接进 ws_pipeline.ps1（插在 check_batch_usage 之后）。"""
import io

PATH = r"E:\开发\tools\ws_pipeline.ps1"
ANCHOR = "RunChecker \"$PSScriptRoot\\check_batch_usage.py\"  'check_batch_usage'"

ADD = """
# Loading-phase hooks (Load/Unload/PostSetupContent...) run on a thread-pool worker,
# and FNA3D graphics/audio calls must happen on the main thread: creating a texture
# there throws ThreadStateException in the CLIENT and gets the whole mod disabled.
# The -server load test below can NOT catch that (Main.dedServ short-circuits it).
RunChecker "$PSScriptRoot\\check_load_thread_safety.py" 'check_load_thread_safety'"""

text = io.open(PATH, encoding="utf-8-sig").read()

if "check_load_thread_safety" in text:
    print("已经接好了，跳过")
else:
    assert ANCHOR in text, "找不到锚点"
    text = text.replace(ANCHOR, ANCHOR + ADD, 1)
    io.open(PATH, "w", encoding="utf-8-sig", newline="\r\n").write(text)
    print("已插入 check_load_thread_safety")
