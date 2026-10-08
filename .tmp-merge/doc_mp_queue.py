# -*- coding: utf-8 -*-
"""把「联机适配」写成待办队列的**第一项**（含验收标准与技术方案），放进 开发说明.md。"""
import io

DOC = r"E:\开发\WastelandSoul\开发说明.md"

HEAD = "## 〇之二、待办队列（第一项：联机适配）"

SECTION = u'''## 〇之二、待办队列（下一轮开工顺序）

> 开工时段按「〇」节的排期大前提：**只在 08:00–12:00 与 14:00–18:00 之外**动手。
> 队列由玩家指定顺序，**未完成的项不放行下一项**（每项都要编译 0/0 + 全部检查器绿 + 装机）。

### 【第 1 项 · 最高优先】**联机适配**

**玩家要求（原文要点）**：
1. **与服务端进度相同** —— 一切剧情/世界进度以**服务端为权威**，客户端只做显示；
2. **游戏体验相同** —— 联机与单机应当是同一种体验，不允许"某些功能只在单机成立"；
3. **把只在单机成立的部分尽量改成联机可用**；
4. **绝不修改其它下载来的模组**（InnoVault / SubworldLibrary / BossChecklist 等一律用**原版发布件 + 公开 API**），
   否则加入服务器的人无法用干净的依赖包正常获得与体验本模组。

**当前已知的单机专属 / 未同步点（代码取证）**：

| 位置 | 现状 | 目标 |
| --- | --- | --- |
| `Common\\Systems\\FireplaceGateSystem.cs` | 对 `MultiplayerClient` 直接 return；进入子世界走反射 `SubworldSystem.Enter<T>()`，而 SLL 的 `BeginEntering` 在 `netMode == 2`（专用服务端）**直接 return** ⇒ 服务端上根本进不去 | 改为**服务端权威**：客户端右键门 → 发 `ModPacket` 请求 → 服务端校验 `fireplaceOpened` 后用 `SubworldSystem.MovePlayerToSubworld<T>(whoAmI)` 移动该玩家；返回走 `MovePlayerToMainWorld(whoAmI)` |
| `Common\\Systems\\FireplaceEntrySystem.cs` | 判断 `!Main.dedServ && netMode == SinglePlayer` ⇒ **只单机生效** | 让"首次生成后补一次重载"在**服务端**对**该玩家**执行（host&play 与联机都要成立）；若 SLL 在服务端本就会生成完整世界，则改为"由服务端广播一次刷新/重进" |
| `Content\\Subworlds\\FireplaceAtmosphere.cs`（有害气体 / 污染水） | 在 `ModPlayer` 里 `player.AddBuff(...)`，客户端自己加 ⇒ 联机里通常只是**本地显示**、服务端不认（掉血可能不生效） | 改为**服务端施加**（`Main.netMode != MultiplayerClient` 时 AddBuff，让 tML 同步给客户端）；客户端只保留浮尘等纯视觉 |
| `Content\\Items\\Story\\*`（灵魂碎片交付）/ `WastelandMemorySystem` | 交付放在对话钩子里，消耗与记忆推进的**权威性**未校验 | 客户端发"交付第 N 枚"的请求包 → **服务端**扣物品 + 推进记忆 + `WastelandStorySystem.Sync()` |
| 盗贼「精钢双刃」（设计中） | 双刃同插后收回必暴 ×2 的判定 | 判定**放服务端**（联机一致） |
| `Common\\Systems\\ScavengerInvasionSystem.cs` / `WastelandStorySystem` | 已有 `netMode` 分支与 `NetSend/NetReceive/Sync` | 复核一遍：世界级状态是否**每次变更都同步**、客户端是否**从不**写世界状态 |
| 迷你 Boss 召唤物（`WastelandItemBases`） | `MultiplayerClient` 直接 return（召唤走服务端）✓ 写法正确 | 保持，作为其它系统的参照样板 |

**验收标准（必须逐条验证，不接受"应该能"）**：
1. **进度一致**：新客户端中途加入 → 看到与服务器一致的剧情阶段（门是否开启、终端是否读过、四段记忆、结局选择）；
2. **双端体验一致**：进门/返回、有害气体掉血、碎片交付、Boss 战与掉落，在联机与单机表现一致；
3. **依赖零改动**：`InnoVault.tmod` / `SubworldLibrary.tmod` / `BossChecklist.tmod` 的**文件哈希与本轮开工前一致**；
   本模组只调用它们的公开 API（或只读反射），**不得**要求玩家使用任何魔改版依赖；
4. **服务端权威**：所有"世界状态"的写入都只发生在服务端；客户端只读 + 发请求；
5. **可复现的自检**：把「无头服务端 + 客户端」两条路径都纳入流水线（客户端那半需要玩家配合跑一次并回日志）；
6. 编译 0 errors / 0 warnings、25 项静态检查全绿、装机 SHA256 校验通过。

**依赖哈希基线（开工前记录，收工后核对）**：见 `E:\\开发\\.dependency-hashes.txt`（由流水线/检查脚本生成）。

---

'''

text = io.open(DOC, encoding="utf-8").read()

if HEAD in text:
    print("队列项已存在，跳过")
else:
    lines = text.split("\n")
    # 插在「〇」节之后（即第一个 "## 一、" 之前）
    insert_at = next((i for i, line in enumerate(lines) if line.startswith("## 一、")), 0)
    lines = lines[:insert_at] + SECTION.split("\n") + lines[insert_at:]
    io.open(DOC, "w", encoding="utf-8", newline="\n").write("\n".join(lines))
    print("已写入待办队列（第 1 项：联机适配）")

# 顺手记录依赖模组的哈希基线
import hashlib
import os

mods = os.path.join(os.environ["USERPROFILE"], "Documents", "My Games", "Terraria", "tModLoader", "Mods")
rows = []

for name in ("InnoVault.tmod", "SubworldLibrary.tmod", "BossChecklist.tmod", "WastelandSoul.tmod", "WastelandSoulCN.tmod"):
    path = os.path.join(mods, name)

    if os.path.exists(path):
        digest = hashlib.sha256(open(path, "rb").read()).hexdigest()[:16].upper()
        rows.append("%s  %s  %d 字节" % (name, digest, os.path.getsize(path)))

io.open(r"E:\开发\.dependency-hashes.txt", "w", encoding="utf-8", newline="\n").write(
    "依赖模组哈希基线（开工前）\n" + "\n".join(rows) + "\n")

print("已记录依赖哈希基线：")
for row in rows:
    print("  " + row)
