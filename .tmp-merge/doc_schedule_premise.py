# -*- coding: utf-8 -*-
"""把「避开高峰时段」写成 开发说明.md 的**大前提**（放在标题之后、正文之前）。"""
import io

DOC = r"E:\开发\WastelandSoul\开发说明.md"

HEAD = "## 〇、开发排期大前提（硬性，优先于其它一切安排）"

SECTION = u'''## 〇、开发排期大前提（硬性，优先于其它一切安排）

> 这一条是**玩家（用户）明确要求**并写进文档开头的：**所有构建 / 下载 / 批量作业 / 代理长任务，
> 一律避开下面两个高峰时段。**

| 时段 | 处理方式 |
| --- | --- |
| **08:00 – 12:00** | **禁止开工**（不构建、不下载、不派长任务） |
| **14:00 – 18:00** | **禁止开工**（同上） |
| 其余时段 | 正常安排 |

**执行细则**

1. **新任务**：等高峰结束之后再启动；不要在高峰里"先跑一小会儿"。
2. **跨高峰的任务**：必须在**进入高峰之前收尾** —— 能停、能编译、能玩。
3. **被打断的代理/半成品**：其未完成文件**必须移出工程**（放 `E:\开发\.backup\` 下留档）
   或补齐到能编译，**保证仓库任何时候 `dotnet tModLoader -build` 都是 0 errors / 0 warnings**。
4. **已装机版本优先**：任何实验都不得让"游戏里能玩的那一版"变得不可用（先装绿包，再试新东西）。
5. 需要我确认时间时，以本机 `Get-Date` 为准；若当前正处在高峰段，我应当**只停手等待**。

> 立这条规矩的代价参考：**批次 29**（清道夫五把武器那条线在 15:01 被叫停，
> 半成品留下 10 个编译错误；靠把 3 个未完成文件移到 `.backup\\ws_weapons_wip\\` 才恢复 0/0）。
'''

text = io.open(DOC, encoding="utf-8").read()

if HEAD in text:
    print("大前提已存在，跳过")
else:
    lines = text.split("\n")
    # 插到标题行（# ...）与其后的引言块之后、第一个 "## " 之前
    insert_at = next((i for i, line in enumerate(lines) if line.startswith("## ")), 0)
    lines = lines[:insert_at] + SECTION.split("\n") + ["", "---", ""] + lines[insert_at:]
    io.open(DOC, "w", encoding="utf-8", newline="\n").write("\n".join(lines))
    print("已把排期大前提写进文档开头")

# 校验
text = io.open(DOC, encoding="utf-8").read()
print("校验：大前提存在 =", HEAD in text)
for i, line in enumerate(text.split("\n")[:14], 1):
    print("%3d: %s" % (i, line[:78]))
