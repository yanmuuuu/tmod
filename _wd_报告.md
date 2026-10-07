# ArmamentDisplay (= WeaponDisplay) 只读分析报告

## 1. 定位

- 任务名 `ArmamentDisplay`，工坊目录里**没有**叫这个名字的 `.tmod`。
- 实际它是 `WeaponDisplay.tmod` 的**内部 mod 名**：`Info` 二进制中 `displayName = ArmamentDisplay`，`version = 1.20.1`，`buildVersion = 2023.6.25.31`，
  author = 逆风而行的信鸽, 阿汪, Cyril。
- 工坊 ID `2763158325`，两个版本子目录（`LastWriteTime` 相同，无法按时间区分）：
  - `2022.9\WeaponDisplay.tmod` (320027 B)
  - `2023.6\WeaponDisplay.tmod` (321916 B) ← 取此版本分析
  - 本地 `Mods\WeaponDisplay.tmod` 大小同为 321916 B，与 2023.6 一致。
- `workshop.json` Tags: New Content / Utilities / Quality of Life / Gameplay Tweaks / Visual Tweaks / 中英双语 / 1.4.3 / 1.4.4。

## 2. 条目清单（28 项，2023.6）

**没有任何 `.cs` 源文件，没有 `build.txt`，没有 README/Library 字样。**

```
WeaponDisplay.dll            34816 -> 15174     <-- 唯一代码
WeaponDisplay.pdb            21576 -> 12275
Info                          1341 ->   875
workshop.json                  112 ->   112
Properties/launchSettings.json 403 ->   403
description.txt               1213 ->   790
Localization/en-US.hjson      3899 ->  1389
Localization/zh-Hans.hjson    3552 ->  1410
icon.png                      4881 ->  4881
ItemInWorld/Light0..2.rawimg                    <-- 掉落物光效贴图
Shader/BaseTex(.rawimg/-1/1)   ~1 MB each       <-- 刀光底图
Shader/AniTex(.rawimg/-1/1)    153612 each
Shader/ItemGlowEffect.xnb, ItemGlowEffect2.xnb
Shader/ShaderSwooshEffect.xnb
Shader/Style_12.rawimg, Style_18.rawimg, ItemFlame_3823.rawimg
Weapons/第一分形.rawimg, 旧日支配者.rawimg, 旧日支配者(真).rawimg ...
```

## 3. 依赖声明

- 容器内**无 `build.txt`**，故**无 `modReferences`**。
- 程序集引用（Mono.Cecil `AssemblyReferences`）：
  `System.Runtime 6.0.0.0`, `tModLoader 1.4.4.9`, `System.Collections 6.0.0.0`, `FNA 22.7.0.0`,
  `MonoMod.Utils 23.3.22.1`, `Mono.Cecil 0.11.4.0`, `TerrariaHooks 0.0.0.0`, `log4net 2.0.8.0`, `ReLogic 1.0.0.0`。
- 反查 TypeRef 的 Scope：`FNA 33 类 / tModLoader 52 类 / TerrariaHooks 7 类 / MonoMod.Utils 5 类 / ReLogic 3 类`
  —— **0 个第三方模组类型**。
- 结论：**自包含，不依赖其它 mod，也不被设计成被依赖**。

## 4. 性质判定：功能/内容模组，**不是库**

证据：

1. 无 `Mod.Call` 互操作入口；无 `ModSystem` 对外服务注册以外的扩展点。
2. 核心渲染全部 `private`：`WeaponDisplay::DrawSwoosh(Player, Color)`、`DrawSwoosh_AlphaBlend(Player, Color)`。
3. Shader 资产名硬编码在 IL 里（`WeaponDisplay/Shader/BaseTex`、`WeaponDisplay/Shader/AniTex`），
   且绘制时直接占用 `GraphicsDevice.Textures[0..2]` 与 `SamplerStates`。
4. 靠 IL Hook 改动原版：`Terraria.On_Player::ItemCheck_ManageRightClickFeatures_ShieldRaise`、
   `Terraria.DataStructures.On_PlayerDrawLayers::DrawPlayer_27_HeldItem`、
   `Terraria.IL_Player::ItemCheck_MeleeHitNPCs`。
5. 自带头部硬编码的原版物品黑名单（930/494/3542/3476/4707/4818/4952/4760/4715 等跳过绘制）。
6. 自带内容：`Weapons.第一分形`（First Fractal）、`Weapons.旧日支配者`、`Weapons.真旧日支配者`、`Projectiles.旧日剑气`。

## 5. 对外可见成员（public）

```
WeaponDisplay.Configuration : ModConfig          // 客户端视觉设置
  bool ShowWeapon; DyeSlot DyeUsed; bool ShowGlow; float GlowLighting;
  float WeaponScale; bool LightItem; int LightItemNum; bool CoolerSwooshActive;
  float IsLighterDecider; bool UseItemTexForSwoosh; bool ItemAdditive;
  bool ToolsNoUseNewSwooshEffect;  override ConfigScope Mode => Client

WeaponDisplay.ConfigurationServer : ModConfig    // 服务端/游戏性设置
  bool UseHitbox; int ItemAttackCD; bool CanParry;
  List<ItemDefinition> ParryItems;  override ConfigScope Mode

WeaponDisplay.DyeSlot : enum { None=0, Head=1, Body=2, Leg=3 }

WeaponDisplay.CustomVertexInfo : struct, IVertexType
  Vector2 Position; Color Color; Vector3 TexCoord;
  ctor(Vector2, Color, Vector3) / ctor(Vector2, Vector3)

WeaponDisplay.WeaponDisplay : Mod
  static Effect ItemEffect { get; }         <-- protected static 字段 + public getter
  static Effect ColorfulEffect { get; }
  override Load/Unload/HandlePacket

WeaponDisplay.WeaponDisplayPlayer : ModPlayer
  static bool ShowWeapon;
  List<Vector2> itemOldPositions; float kValue; bool negativeDir;
  float rotationForShadow; Vector2 HitboxPosition; bool UseSlash;
  override ModifyDrawInfo(ref PlayerDrawSet); OnEnterWorld();

WeaponDisplay.SlashGlobalItem : GlobalItem
  override AppliesToEntity / UseItemHitbox / CanUseItem / UseStyle / Load
  // 触发条件（IL 实证）: item.useStyle == 1 && item.DamageType == DamageClass.Melee
  //   且 UseStyle 里再判 player.GetModPlayer<WeaponDisplayPlayer>().UseSlash
  //   且 ToolsNoUse 时不作用于 axe/hammer/pick

WeaponDisplay.WeaponParry : ModSystem
  // Hook On_Player_ItemCheck_ManageRightClickFeatures_ShieldRaise
  // 依据 ConfigGameplay.CanParry + ParryItems(ItemDefinition) + player.hasRaisableShield

WeaponDisplay.ItemInWorld.ItemLight : GlobalItem
  override PreDrawInWorld(...)   // 掉落物光效

WeaponDisplay.WeaponDisplayMethods : internal static
  static float Lerp(float t, float from, float to, bool clamp)   // internal，不可用

WeaponDisplay.HandleNetwork : internal
  MessageType { BasicStats=0, Hitbox=1 }
```

## 6. 刀光渲染的真实调用序列（IL 实证，非猜测）

`WeaponDisplay::DrawSwoosh`：

1. 取 `ColorfulEffect` / `ItemEffect`，任一为空则 return。
2. `float value = player.itemAnimation / (player.itemAnimationMax - 1);`
   再算 `1 - 2(1-value)² - (-1)(1-value)` 得到摆动插值；`negativeDir` 翻转。
3. 组装 **6 个 `CustomVertexInfo`**（一个 quad 的两个三角形 + 尾巴），存进 `List<CustomVertexInfo>`。
4. `Main.spriteBatch.End()` →
   `Main.spriteBatch.Begin(SpriteSortMode, BlendState.Additive, SamplerState, DepthStencilState, RasterizerState, Effect, SpriteViewMatrix.TransformationMatrix)`。
5. `ColorfulEffect.Parameters["uTransform"].SetValue(Matrix)`、`Parameters["uTime"].SetValue(float)`。
6. 绑定贴图：
   `Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("WeaponDisplay/Shader/BaseTex", AssetRequestMode.ImmediateLoad).Value;`
   `Main.graphics.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("WeaponDisplay/Shader/AniTex", ...).Value;`
   并设置 `SamplerStates[0..2] = SamplerState.LinearWrap`。
7. `ColorfulEffect.CurrentTechnique.Passes[0].Apply();`
8. `Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertexList.ToArray(), 0, vertexList.Count / 3);`
9. 恢复 `graphics.GraphicsDevice.RasterizerState`，`spriteBatch.End()` 重启。

## 7. 我们能不能用它做"武器"的表现

**结论：不能作为代码层依赖来"调用"它的挥砍/光效；只能把它当成一个被动生效的第三方视觉层。**

### 可用（无需改代码）

- 我们的武器只要是 **标准近战**（`Item.useStyle = 1` + `DamageType = DamageClass.Melee` + `noUseGraphic = false`），
  玩家订阅本 mod 后就会**自动**获得它的刀光绘制与碰撞箱（碰撞箱受 `ConfigurationServer.UseHitbox` 控制）。
- 反向读取它的时序来同步我们自己的特效：
  `var wd = player.GetModPlayer<WeaponDisplayPlayer>();` 读 `wd.UseSlash` / `wd.kValue` / `wd.negativeDir` /
  `wd.rotationForShadow` / `wd.HitboxPosition` / `wd.itemOldPositions`，或用静态 `WeaponDisplay.ItemEffect`、
  `WeaponDisplay.ColorfulEffect` 拿它的 Effect 对象。

### 不可用

- **无法**给我们的自定义挥砍轨迹（例如 `VertexStrip` 轨迹、自定义挥砍弹幕）复用它的 `DrawSwoosh`：`private`。
- **无法**通过 `Mod.Call` / `ModSystem` 注册或取用它的刀光算法、Shader 参数接口：不存在。
- **无法**从源码学习：容器内无 `.cs`，`build.txt` 也不存在；只能反汇编 IL。
- 它的 Shader 资产名硬编码为 `WeaponDisplay/Shader/...`，路径不可参数化，我们不能换贴图。
- 它不走 `GlobalItem.UseStyle` 的通用扩展点，而是直接 `On_PlayerDrawLayers.DrawPlayer_27_HeldItem`
  接管原版绘制，我们若也接管同一 hook 会互相顶掉。

### 风险

1. **强制玩家安装**：作为 `modReferences` 依赖会强制所有玩家订阅这个第三方中文视觉 mod；
   本机 `enabled.json`（WastelandSoul/MagicBuilder/SilkyUIFramework/CheatSheet/ImproveGame/InnoVault/WastelandSoulCN/BossChecklist）里**并未启用它**。
2. **无源码**：与"结论必须基于真实源码"的工程原则冲突，只能依赖 IL 反汇编，升级/排错代价高。
3. **IL Hook 冲突面大**：`On_Player`、`IL_Player`、`On_PlayerDrawLayers` 三个核心点，与其它战斗/绘制模组冲突概率高。
4. **作者自述适配性有限**："因为模组武器的实现方法各不相同，所以这个模组无法适配于所有模组武器。"
5. **停更风险**：最后一版是 2023.6（1.4.3 时代构建，标称兼容 1.4.4），此后无更新。
6. **硬编码黑名单/特判**：原版物品 ID 白黑名单、`shoot` 类型特判散落在 IL 中，不是通用实现。
7. **它自带测试武器**（第一分形 等），依赖它意味着连带引入其内容与配方。

### 建议

自研刀光：`CustomVertexInfo`(Position/Color/TexCoord) + `DrawUserPrimitives(PrimitiveType.TriangleList, ...)`
+ `ModContent.Request<Texture2D>` 绑定到 `GraphicsDevice.Textures` + 自己的 `.fx`
——这正是本 mod 的做法，可以作为**设计参考**，但**不要代码级依赖**。

## 8. 分析产物（均在工作区内，未修改任何工程文件）

- `_wd_2023_6\`：解包出的 Info / description.txt / Localization / DLL / PDB
- `_wd_api.txt`：Mono.Cecil 全量类型/成员/引用清单（286 行）
- `_wd_il.txt`：11 个关键方法的完整 IL 反汇编（2907 行）
- `tools\_wd_dump.py`、`tools\_wd_cecil.ps1`、`tools\_wd_il.ps1`、`tools\_wd_info.py`：本次使用的分析脚本
