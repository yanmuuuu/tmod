# -*- coding: utf-8 -*-
"""记录批次 26（灵魂碎片改造 + Boss 旗帜奖杯接线 + 最终装机）。"""
import io

DOC = r"E:\开发\WastelandSoul\开发说明.md"

SECTION = u'''
---

## 批次 26：灵魂碎片任务道具化 · Boss 旗帜与奖杯 · 最终装机

### 1. 灵魂碎片（按玩家要求改造）

| 要求 | 实现 |
| --- | --- |
| **每个世界只给一次** | `WastelandMemorySystem.ShouldGrantSoulFragment(player, bossIndex)` = 「该段记忆未恢复（`first/second/third/fourthMemoryRestored` 或 `soulFragmentsDelivered ≥ 序号`）」**且**「背包 + 银行（bank/bank2/bank3/bank4）里都没有这枚」。掉落袋不满足就**不给、也不占掉落位**（没有其它发放路径，已 grep 确认） |
| **消耗型任务道具** | `SoulFragment` 基类：`maxStack = 1`、`consumable = true`、`useStyle = None`、`CanUseItem → false`、稀有度 `ItemRarityID.Quest`、售价 0 —— 玩家自己右键用不了，不会乱点跳剧情 |
| **消耗后她自主更新记忆** | `HasNextFragment()` = 带着任意一枚即出现交付选项；交付走 `TryHandIn`：先真的扣掉（背包→银行兜底），未恢复则 `RestoreMemory(序号)` + 提示；**已读过壁炉终端再交碎片·其一**照常消耗、只提示「这段记录早就归档过了」，**不重播**剧情。老存档里堆积的多余碎片可以一枚枚交掉清背包 |

### 2. Boss 旗帜与奖杯（玩家 ChatGPT 出图 → 切图接线）

- 素材 `art-inbox/raw/banners_trophies.png`（1983×793 品红底）；工具 `tools/apply_boss_art.py`：抠品红 → 渗色修复 → **行投影分行、行内列投影分格**（两排格子宽度不同，未假设统一网格）→ 每格 bbox → 缩放；
- 新增 **9 图块 + 9 物品**：4 面旗帜（`TileObjectData.Style1x2Top`，18×34）+ 5 座奖杯（`Style3x3`，54×48，与既有 3×3 图块约定逐字一致），物品图标 32×32 / 24×32；
- 掉落：4 个 Boss + 迷你 Boss「废料收割者」各 **10%**（清道夫那条挂在既有「被玩家正常击败」条件上，自毁不掉）；旗帜在**织布机**上做（Boss 材料 ×1 + 丝线 ×3）；
- `BossChecklist` 收集项每 Boss 从 2 项变 **4 项**（袋 + 灵魂碎片 + 奖杯 + 旗帜）；
- 本地化 +27 键（9 物品显示名/说明 + 9 图块地图名），同步后 **759/759、0 占位**。

### 3. 最终验收与装机

| 项目 | 结果 |
| --- | --- |
| 编译 | ✅ 主模组 + 汉化补丁 `0 errors / 0 warnings` |
| 静态校验 | ✅ 18 项全绿（`check_assets` **371 个可加载类型**全过；本地化 **759/759**） |
| 加载自检 | ✅ 服务端 / 客户端两条路径都通过，无 Disabling / malformed / legacy |
| 装机 | ✅ `WastelandSoul.tmod` **18,945,702** 字节（sha `63172914`）/ `WastelandSoulCN.tmod` 44,504 字节（sha `AEDF12ED`） |
| 流水线 | ✅ `PIPELINE RESULT: all green`（03:35） |

### 4. 待玩家拍板 / 仍未做

1. 迷你 Boss 奖杯命名：现为 `ScrapReaperTrophy`（废料收割者奖杯），玩家若想叫「锈爪齿轮」可改；
2. 奖杯贴图 54×48（项目既有约定）vs 54×54（零采样钳制）——现在视觉无痕，可选改；
3. **仍未做（需要美术 / 编码器）**：智械人人物模型与 3D（InnoVault Models3D）、四首 Boss 曲 `wav → ogg` 转码（本机无 ffmpeg/ogg 编码器，包体 18.9 MB 几乎全是 wav）、实机战斗平衡实测。
'''

text = io.open(DOC, encoding="utf-8").read()

if "批次 26" not in text:
    if not text.endswith("\n"):
        text += "\n"
    io.open(DOC, "w", encoding="utf-8", newline="\n").write(text + SECTION)
    print("已追加 批次 26")
else:
    print("已存在")
