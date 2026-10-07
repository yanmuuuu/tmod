# WastelandSoul

WastelandSoul 是一个面向 tModLoader 的 Terraria 模组，聚焦废土主题、剧情推进、Boss 战、城镇 NPC 和多阶段玩法体验。

## 项目简介

- 主题：废土科幻 / 机械废墟 / 残骸传奇
- 核心内容：
  - 智械人城镇 NPC
  - 清道夫 Boss 与阶段战斗
  - 物品、材料、污染系统、剧情进度
  - 中英双语本地化

## 目录结构

- `WastelandSoul/`：主模组代码与内容
- `WastelandSoulCN/`：中文语言包
- `tools/`：辅助脚本与校验工具
- `art-inbox/`：素材收件箱

## 运行方式

1. 安装 tModLoader
2. 把该仓库克隆到本地
3. 将 `WastelandSoul` 作为模组源目录放到 tModLoader 的 ModSources 中
4. 使用 tModLoader 内置“生成模组”或命令行方式编译

示例：

```bat
cd /d E:\steam\steamapps\common\tModLoader
dotnet tModLoader.dll -build E:\开发\WastelandSoul
```

## 代码说明

本项目目前重点在以下内容：

- `WastelandSoul/Common/Systems/`：系统逻辑、剧情状态和全局玩法
- `WastelandSoul/Content/NPCs/`：NPC 与 Boss
- `WastelandSoul/Content/Items/`：物品与材料
- `WastelandSoul/Localization/`：本地化文本

## 许可证

本项目采用 MIT License，详情见 [LICENSE](LICENSE)。

## 贡献

欢迎提交 issue 和 PR。若你想参与开发，建议先阅读 `WastelandSoul/开发说明.md`。

## 状态

当前本仓库适合用于开源协作、模组开发、内容迭代和共享交流。
