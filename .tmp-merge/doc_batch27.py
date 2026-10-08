# -*- coding: utf-8 -*-
"""记录批次 27：首次进门自动成型 + 3D 探针 + 奖杯命名 + 06:00 关机守卫 + 失效自检的说明。"""
import io

DOC = r"E:\开发\WastelandSoul\开发说明.md"

SECTION = u'''
---

## 批次 27：首次进门自动成型 · 3D 探针 · 奖杯命名 · 06:00 关机守卫

### 1. 「第一次进壁炉就能看到世界」（SubworldLibrary 机制的查证结论）

把 `SubworldLibrary.tmod`（2.3.0.1）反编译到 IL 级别核对，得到三条**决定性事实**：

1. `SubworldSystem.LoadWorld()` 只有两条路：有 `.wld` → `Subworld.ReadFile()`（读档）；没文件 → `LoadSubworld()`（生成）。
   **两条路都会**调 `current.OnLoad()` + `Main.sectionManager.SetAllSectionsLoaded()` + `Main.QueueMainThreadAction(SpawnPlayer)`
   —— 也就是说**没有"生成路径漏调刷新"这种可补的调用点**。
2. `Subworld.OnEnter()` 是在 `ExitWorldCallBack` 里被调的，**先于** `LoadWorld()`；名字像"进入之后"，实际碰不到新世界数据。
3. `SubworldSystem` / `Subworld` 的公开面里**没有任何"重新加载 / 刷新图格 / 重发 section"的入口**；
   `BeginEntering(int)` 在 `Main.netMode == 2`（专用服务端）时**直接 return**。

**因此采用的实现**（`Common/Systems/FireplaceEntrySystem.cs`）：
进入壁炉 → 提示「壁炉正在成型……请稍候」→ 1.5 秒后自动 `Exit()` → 主世界等 1 秒 → 自动 `Enter()`
（等价于玩家手动"出去再进一次"这一步，按构造必然生效）。

**"只触发一次"的三重保证**：
- `FireplaceSubworld.GeneratedFresh`：`ReadFile()` **只在读档路径**被调用，所以在 `OnLoad()` 里能区分"本次是本机首次生成"还是"读档" ——
  老存档 / 联机存档进门**完全不会被踢**；
- `WastelandStorySystem.fireplaceSeen`：随存档保存 + 联机同步，**先置位再动手**，中途失败或玩家半路自己出去都不再重来；
- 状态机各阶段都有超时，且**只在单人**生效（联机走 subserver 重连，不存在这条路径）。

### 2. ⚠️ 失效的工具：`FireplaceGenSmokeTest`（保留但请勿当验收手段）

它依赖 `SubworldSystem.Enter<T>()` 在**无头服务端**里进子世界；但按上面第 3 条，
`BeginEntering` 在专用服务端直接 return —— **这条路根本不通**（此前的 gentest 日志停在 `Choose World` 就是这个原因）。
所以「壁炉世界生成」目前**只能靠实机进门验证**：日志里会有
`[壁炉生成] 已注册 N 个生成步骤` / `步骤 i/N … 开始/完成：累计写入 M 格` / `全部完成`。

### 3. 3D（InnoVault Models3D）——不需要 Blender

读 InnoVault 内嵌源码得到的事实：

| 项 | 结论 |
| --- | --- |
| 格式 | 只有 **`.obj`(+`.mtl`)** 与 **`.gltf` 2.0**；**无 .fbx**，`.glb` 明确不支持（源码注释） |
| 贴图 | MTL 的 `map_Kd` 会走 `ImmediateLoad`（**加载期工作线程**）→ 我们**故意不写 `map_Kd`**，改在主线程首次绘制时挂贴图 |
| 动画 | OBJ 路径**没有骨骼/动画**（`IsSkinned == false`）；蒙皮只在 glTF |
| 批处理 | `Model3DRenderer` 自己管 `End → 画到自己的 RT → 合成 → Begin`，**不要自己再 Begin/End** |
| 打包 | **实测**：`.obj`/`.mtl` 作为原样文件入包（探针包 440 条目含 5 obj + 5 mtl） |

产物：`tools/gen_3d_models.py`（纯 Python 写 OBJ）→ 5 个模型（探针机器人 / 精钢头盔·胸甲·护腿 / 智械人低模人形），
关节分组按原版骨骼语义命名；另有软件光栅离线预览（不需要开游戏）。
游戏内**探针**：`Common/Models/Wasteland3DPreview.cs` —— 玩家**手持「芯片」**时头顶转一个方块机器人，
日志核对点 `3D 探针加载成功：… 264 顶点 / 132 三角形`。

### 4. 奖杯命名

迷你 Boss 奖杯显示名统一为 **「锈爪齿轮奖杯」/ `Rusted Claw Trophy`**（物品名与图块地图名一致；类名/文件名/贴图未动）。

### 5. 06:00 关机守卫（玩家要求"完成则关机"）

- 计划任务 **`WastelandWorkGuard`**（06:00 起每 10 分钟一次）调用 `tools/shutdown_when_done.ps1`；
- 规则：完成标志 `E:\开发\.work-done` **不存在 → 不关机**；有 `tModLoader/Terraria/dotnet` 在跑 → **推迟**（不打断游戏与存档）；
  两者都满足 → `shutdown /s /t 120` 并**删除标志与任务**（一次性，重启不会再触发）；
- 取消：`shutdown /a`（只取消这次）或 `schtasks /Delete /TN "WastelandWorkGuard" /F`（彻底取消）。
'''

text = io.open(DOC, encoding="utf-8").read()

if "批次 27" not in text:
    if not text.endswith("\n"):
        text += "\n"
    io.open(DOC, "w", encoding="utf-8", newline="\n").write(text + SECTION)
    print("已追加 批次 27")
else:
    print("已存在")
