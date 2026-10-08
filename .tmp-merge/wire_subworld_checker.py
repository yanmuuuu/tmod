# -*- coding: utf-8 -*-
"""把 check_subworld_scope 接进 ws_pipeline.ps1（幂等；插在 check_no_auto_trails 之后）。"""
import io

PATH = r"E:\开发\tools\ws_pipeline.ps1"
ANCHOR = "RunChecker \"$PSScriptRoot\\check_no_auto_trails.py\" 'check_no_auto_trails'"

ADD = """
# 【批次 28 的教训】改地形的全局系统必须有子世界守卫：壁炉门系统曾在子世界里继续按主世界
# 坐标"放门"，每秒啃掉 81x4 格，把堡垒和塔顶一层层吃掉
RunChecker "$PSScriptRoot\\check_subworld_scope.py" 'check_subworld_scope'"""

text = io.open(PATH, encoding="utf-8-sig").read()

if "check_subworld_scope" in text:
    print("已经接好了，跳过")
else:
    if ANCHOR not in text:
        raise SystemExit("找不到锚点，pipeline 可能被别的代理改过；请手动确认")
    text = text.replace(ANCHOR, ANCHOR + ADD, 1)
    io.open(PATH, "w", encoding="utf-8-sig", newline="\r\n").write(text)
    print("已插入 check_subworld_scope")
