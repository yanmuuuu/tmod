# 废土魂穿 · AI 出图规格书 与 3D 模型路线

> 用途：你把下面的 prompt 逐条丢给 ChatGPT 出图，把图放进 `E:\开发\art-inbox\`，
> **剩下的活（去背、缩放、量化、拼帧表、写进模组、跑校验与装机）全部由我做**。
> 本文件里的画布尺寸、帧数、文件名都是**代码已经写死的硬约束**——按这个交，我不用改一行代码；
> 不按这个交，我就要改代码（容易引入回归）。
>
> 生成日期：2026-10-07　对应工程：`E:\开发\WastelandSoul`（tModLoader 1.4.4.9）

---

## 交付进度（每收一批就更新这里）

| 批次 | 内容 | 状态 |
| --- | --- | --- |
| **P0** | 归档者本体 + 血条头像 + 光束 + 档案页 + 弹幕墙纸页 + 归档封印 + 召唤物 + 掉落袋 + 材料（**9 张**） | ✅ 已装机 |
| **P1** | 智械人 **4 个朝向一张 sheet**（正 / 侧 / 背 / 举核心） | ✅ 已装机（我派生了 25 帧行走/站立/使用） |
| **P2** | 清道夫 3×3 姿态 sheet（我取 6 帧）+ 无人机 4 帧 + 灵魂碎片 4 枚 | ✅ 已装机 |
| **P3** | 40 把武器总表（4 Boss × 5 职业，**你标注"暂定"**） | ✅ 已装机（20 张 A 线图标；EX 版由程序放大生成） |
| **P4** | 精钢套装 15 件图标（3 部位 × 5 职业 sheet） | ✅ 已装机（`40×1120` 装备帧表仍保持程序生成） |
| P5 | 3D 模型 | ⏳ 未开始 |

> ✅ **好消息：不用一张一张出了。** 你后来直接发的**带文字标签的整张 contact sheet**
> （`Warrior (44x44)` / `hover high` / `arm tucked` 这类标注）我也能处理——
> `tools/split_sheet.py` 会按连通块把每个素材抠出来，**文字标签自动丢弃**，
> 飘在旁边的光球/星点/火星会被**吸收**进同一个素材（靠"到主体的包围盒间距 ≤ 10px"判定）。
> 所以以后**一张图里排满多个素材更省额度**，只要：素材之间留出明显的品红间隔、
> 标签放在格子外面的空白处、每个素材画大一点。

**收到图之后的处理命令**（我自己跑，你不用管）：

```bat
python E:\开发\tools\apply_art.py --preview   :: 抠图/缩放/量化/拼帧表，并生成对照预览图
powershell -NoProfile -ExecutionPolicy Bypass -File E:\开发\tools\ws_pipeline.ps1
```

> ⚠️ **正式美术落地后不要再跑 `gen_sprites.py`**：它是占位图生成器，会覆盖同名贴图。
> 现在它有防护——`art-inbox\raw\` 里有正式美术时会**直接拒绝运行**（要强行生成得加 `--force`）。
>
> 想给归档者补真 4 帧？把 `Archivist_f1.png` / `f2.png` / `f3.png` 放进 `art-inbox` 即可，
> `apply_art.py` 会自动改用真实帧（不再用位移派生）。

### 还需要你在游戏里确认的两件事（我没法替你验证）

1. **智械人左右朝向**：侧身只画了朝右一张，朝左是镜像。帧序我按原版城镇 NPC 惯例排在
   `8-11 = 右 / 12-15 = 左`。如果你在游戏里看到她**横着走时是背对着走**，
   说一声——把两排对调即可（一行代码，不用重画）。
2. **清道夫 6 帧的循环**：我取的是「悬浮高/中/低 × 机械臂收/半/全」里的 6 格
   （`[0, 4, 8, 7, 3, 1]`，收臂→伸臂→收臂，能无缝接回第 0 帧）。觉得哪一格不好看，
   我换一格就行。

---

## 0. 怎么用（三步）与额度策略

1. **一次只出一张图。** ChatGPT 出的是 1024×1024 级别的插画，**不会给你精确像素尺寸**，
   而且一张图里画 4 帧大概率对不齐网格。所以：**一帧一张图**，我负责拼成帧表。
2. **按 P0 → P1 → … 的顺序出。** 每一级都是"出完就能立刻换进游戏"的完整单元。
   额度有限时，**P0 全部出完（约 13 张）就能把 Boss2 归档者换成正式美术**。
3. **图存成 `E:\开发\art-inbox\<我给的编号>.png`**，编号见每节的「文件名」。
   不满意就重出一张覆盖它，不用管旧的。

**省额度的三个诀窍**（很重要）：

- 先用**同一段 prompt** 出第 1 张，满意后再改最后一句出第 2、3、4 张——风格才一致。
- **背景一定要纯品红 `#FF00FF`**。不要透明底（ChatGPT 给不了真透明），不要白底
  （骨白系素材会和白底糊在一起），不要渐变/投影/网格/文字。
- **别让它画小图。** 让它画"单个体、占画面 80%"，缩到 20px 是我的活（最近邻 + 调色板量化）。

**统一前缀**（每条 prompt 都带上，我下面每节已经写好，不用你再拼）：

```
pixel art game sprite, retro 16-bit Terraria style, single object, centered,
flat pure magenta #FF00FF background, no shadow, no ground, no text, no watermark,
no border, no frame, no multiple copies, no perspective, front view,
bold 1-pixel dark outline, limited palette, flat shading, crisp hard edges, no blur
```

**台词级别的风格关键词**（按 Boss 主题挑一个，别混）：

| Boss | 主题 | 关键词 |
| --- | --- | --- |
| 1 清道夫 | 冷灰废料 | `cold grey scrap metal, rust, hydraulic pistons, oily dark steel` |
| 2 归档者 | 骨白冷蓝档案 | `bone-white paper, pale ceramic plates, cold blue index glow, grey metal frame` |
| 3 灰烬之心 | 暖橙余烬 | `charred black rock, glowing orange embers, ash, molten cracks` |
| 4 壁炉守卫 | 冷白金属 | `pale white polished alloy, blue-white furnace light, brushed steel` |
| 城镇 NPC 智械人 | 金发长耳长袍 | `anime-styled android girl, long pointed ears, blond hair, brown-red robe with gold trim, blue eyes` |

---

## 1. 尺寸 / 帧布局硬约束（**不许改**，否则要动代码）

Terraria 的帧表规矩：**帧是纵向堆叠的**，每帧宽 = 整张图宽，每帧高 = 图高 ÷ 帧数。

| 资产 | 目标文件（模组内路径） | 整图尺寸 | 帧数 | 单帧 |
| --- | --- | --- | --- | --- |
| 归档者本体 | `Content/NPCs/Bosses/Archivist/Archivist.png` | **110 × 440** | 4 | 110×110 |
| 归档者血条头像 | `...\Archivist_Head_Boss.png` | 34 × 34 | 1 | — |
| 索引光束 | `Content/Projectiles/Archivist/ArchivistIndexBeam.png` | 48 × 20 | 1 | — |
| 档案页弹幕 | `...\ArchivistArchivePage.png` | 20 × 20 | 1 | — |
| 弹幕墙纸页 | `...\ArchivistBarrageSheet.png` | 24 × 52 | 1 | — |
| 归档封印 | `...\ArchivistSeal.png` | 72 × 72 | 1 | — |
| 召唤物「归档者残响」 | `Content/Items/Summons/ArchivistEcho.png` | 26 × 26 | 1 | — |
| 掉落袋 `ArchivistBag` | `Content/Items/Bags/ArchivistBag.png` | 32 × 32 | 1 | — |
| 材料 `ArchivistFragment` | `Content/Items/Materials/ArchivistFragment.png` | 24 × 24 | 1 | — |
| 灵魂碎片·其二 | `Content/Items/Soul/SoulFragmentSecond.png` | 24 × 24 | 1 | — |
| 清道夫本体 | `Content/NPCs/Bosses/Scavenger/Scavenger.png` | 110 × 660 | 6 | 110×110 |
| 清道夫血条头像 | `...\Scavenger_Head_Boss.png` | 34 × 34 | 1 | — |
| 维修无人机 | `...\ScavengerRepairDrone.png` | 30 × 104 | 4 | 30×26 |
| 智械人本体 | `Content/NPCs/Town/MechanicalCompanion.png` | 40 × 1400 | 25 | 40×56 |
| 智械人头像 | `...\MechanicalCompanion_Head.png` | 16 × 16 | 1 | — |
| 武器图标（20 把 A 线） | `Content/Items/Weapons/<Boss><Class>Weapon.png` | 见 §3.4 | 1 | — |
| 精钢套装（15 件图标） | `Content/Items/Armor/SalvagedSteel<Class><Piece>.png` | 22 × 22 | 1 | — |
| 精钢套装装备帧表 | `..._Head/_Body/_Legs.png` | 40 × 1120 | 20 | 40×56 |

| 其它 Boss 弹幕 | 现有尺寸（可保持） | 说明 |
| --- | --- | --- |
| `ScavengerScrapShard` / `ScavengerSpark` / `ScavengerPistolRound` / `ScavengerCaltrop` | 14×14 / 16×16 / 8×8 / 13×13 | 同族 EX 版是 A 线放大，**不用单独出图** |
| `AshHeart*` / `Fireplace*` 弹幕 | 10×10 ~ 44×44 | 同上 |
| 减益 / 仆从 Buff 图标 | 32 × 32 | 每 Boss 一对（普通 + EX） |

> **EX 版不用出图**：`*EX.png` 全部由 A 线贴图程序放大 + 加发光生成，保持风格一致。

---

## 2. 我拿到图之后会做什么（你不用担心技术细节）

1. **去背**：把纯品红抠成透明（边缘 1px 收缩，避免紫边）。
2. **裁切 + 居中**：按不透明包围盒裁紧，再按目标画布居中（留 1~2px 安全边）。
3. **缩放**：只允许**最近邻**缩放（绝不双线性），再按 16~24 色量化 + 去半透明杂边。
   → 所以 **AI 图越"简单大胆"缩出来越好看**：细节在 110px 以下全丢。
4. **拼帧表**：按帧号纵向堆叠成一张 PNG，写进上表的路径。
5. **校验 + 装机**：`check_assets.py`（尺寸/帧高整除）+ 汉化对齐 + 隔离加载自检 + 复制进游戏 Mods。
6. **改一行登记**：换图不需要改代码，因为尺寸/帧数没变。

---

## 3. 出图清单（按优先级）

### P0 · Boss2 归档者（13 张，一次就能换完整套）

**P0-1 本体 4 帧** → 文件 `Archivist_f0.png` … `Archivist_f4.png`（编号 f0~f3）

> 归档者是一台**悬浮的档案终端**：骨白纸页 + 灰金属框 + 单只冷蓝传感器眼 + 挂在顶上的小红书签 +
> 身边飘着几张散页。整体是**对称正视图**（它对玩家不转身，只有轻微浮动）。

每张都用这段，只改最后一行括号里的浮动状态：

```
pixel art game sprite, retro 16-bit Terraria style, single object, centered,
flat pure magenta #FF00FF background, no shadow, no ground, no text, no watermark,
no border, no frame, multiple copies forbidden, front view,
bold 1-pixel dark outline, limited palette, flat shading, crisp hard edges, no blur.
A floating levitating archive terminal boss: a vertical rectangular bone-white paper console
with pale ceramic plates, thin dark grey metal frame and corner rivets, one large glowing
cold-blue circular sensor eye in the upper half, rows of faint grey index lines on the paper,
a small dark-red bookmark ribbon hanging from the top edge, three loose paper sheets drifting
beside the body. Palette: bone white #D6D2C0, cold blue #6096DC, dark blue #34548C,
grey frame #787E8E, one dark-red accent. Pose: hovering, bobbing 2 pixels UP, loose pages
slightly higher.
```

后三张只替换最后一句：
- `Archivist_f1.png` → `bobbing 0 pixels (neutral), loose pages at mid height.`
- `Archivist_f2.png` → `bobbing 2 pixels DOWN, sensor eye slightly dimmer (darker blue).`
- `Archivist_f3.png` → `bobbing 0 pixels (neutral), sensor eye brightest, loose pages lower.`

**P0-2 血条头像** → `Archivist_head.png`

```
pixel art game sprite, retro 16-bit Terraria style, single object, centered,
flat pure magenta #FF00FF background, no shadow, no text, no border, front view,
bold 1-pixel dark outline, limited palette, crisp hard edges, no blur.
A boss map icon: a small bone-white archive folder / document cover with two grey index lines
and a cold-blue glowing circular stamp seal in the lower right corner, dark grey frame,
simple bold silhouette readable at 34x34 pixels. Palette: bone white, cold blue, dark grey.
```

**P0-3 索引光束** → `ArchivistIndexBeam.png`

```
pixel art game sprite, horizontal thin laser beam, centered, flat pure magenta #FF00FF background,
no text, no border, crisp hard edges, no blur.
A thin cold-white energy beam core pointing right: a bright pure-white 2-pixel center line,
a pale blue 6-pixel glow band around it, a brighter concentrated muzzle flare at the LEFT end,
tapering to a sharp point at the RIGHT end. Palette: white, pale blue #B4D2FF, cold blue #6096DC.
```

**P0-4 档案页弹幕** → `ArchivistArchivePage.png`

```
pixel art game sprite, retro 16-bit Terraria style, single object, centered,
flat pure magenta #FF00FF background, no shadow, no text, no border, top-down view,
bold 1-pixel dark outline, limited palette, crisp hard edges, no blur.
A single sheet of bone-white paper flying, seen from above: slightly curled corner folded at the
top right, four faint grey text lines, a small cold-blue circular stamp in the middle,
dark grey edge. Simple and bold, must read at 20x20 pixels. Palette: bone white, grey, cold blue.
```

**P0-5 弹幕墙纸页** → `ArchivistBarrageSheet.png`

```
pixel art game sprite, retro 16-bit Terraria style, single object, centered,
flat pure magenta #FF00FF background, no shadow, no text, no border, top-down view,
bold 1-pixel dark outline, limited palette, crisp hard edges, no blur.
A tall narrow vertical sheet of bone-white paper, taller than wide, with six faint grey text
lines and one thick dark-blue horizontal binding band across the middle, slightly curled edges,
dark grey outline. Must read as an upright page at 24x52 pixels. Palette: bone white, grey, dark blue.
```

**P0-6 归档封印** → `ArchivistSeal.png`

```
pixel art game sprite, retro 16-bit Terraria style, single object, centered,
flat pure magenta #FF00FF background, no shadow, no text, no border, top-down view,
bold outline, limited palette, crisp hard edges, no blur.
A circular magic seal seen from above: thin cold-blue outer ring, a second pale-blue inner ring,
eight short radial rune notches around the ring, a translucent dark blue disc in the middle,
and a small bone-white paper seal tag with two dark ink lines at the center.
Palette: cold blue #6096DC, pale blue #B4D2FF, dark blue #34548C, bone white.
```

**P0-7 召唤物「归档者残响」** → `ArchivistEcho.png`

```
pixel art game sprite, retro 16-bit Terraria style, single object, centered,
flat pure magenta #FF00FF background, no shadow, no text, no border, front view,
bold 1-pixel dark outline, limited palette, crisp hard edges, no blur.
A summoning item icon: a jagged broken shard of a bone-white archive tablet with a cold-blue
glowing core in its center, and two pale-blue sound-wave arcs radiating from the left side.
Palette: bone white, cold blue, pale blue, dark grey. Must read at 26x26 pixels.
```

**P0-8 掉落袋** → `ArchivistBag.png`

```
pixel art game sprite, retro 16-bit Terraria style, single object, centered,
flat pure magenta #FF00FF background, no shadow, no text, no border, front view,
bold 1-pixel dark outline, limited palette, crisp hard edges, no blur.
A treasure bag: a bone-white paper satchel with a cold-blue wax seal in the middle,
two dark grey leather straps, three loose paper corners sticking out of the top.
Palette: bone white, grey, cold blue. Must read at 32x32 pixels.
```

**P0-9 材料** → `ArchivistFragment.png`

```
pixel art game sprite, retro 16-bit Terraria style, single object, centered,
flat pure magenta #FF00FF background, no shadow, no text, no border, top-down view,
bold 1-pixel dark outline, limited palette, crisp hard edges, no blur.
A crafting material icon: a jagged bone-white ceramic shard with a cold-blue glowing crack
running through it, dark grey edges. Must read at 24x24 pixels.
```

> P0-10 ~ P0-13 = §3.4 里"归档者"那一行的 4 把武器图标（战士/法师/射手/召唤师/盗贼取 4 把非召唤师 + 召唤师，见 3.4）。
> 灵魂碎片**不要单独出一张**：它是一套 4 枚的家族，要么 4 枚一起出（P2），要么保持现状。

---

### P1 · 城镇 NPC 智械人（**3 张 + 1 张攻击**，我派生 25 帧）

25 帧的行走表**不要交给 AI**（对不齐）。你只要出 **正面 / 侧面 / 背面 / 举核心** 4 张，
我用像素级位移 + 抬腿帧派生出行走循环（这是原版 2D 帧动画的常规做法，肉眼看不出拼接）。

文件：`Companion_front.png`、`Companion_side.png`、`Companion_back.png`、`Companion_attack.png`

```
pixel art game sprite, retro 16-bit Terraria style, single character, centered,
flat pure magenta #FF00FF background, no shadow, no ground, no text, no border,
bold 1-pixel dark outline, limited palette, flat shading, crisp hard edges, no blur.
A town NPC: a slender android girl, about 20x32 pixels of body inside a 40x56 frame,
anime-styled, long pointed elf ears, long blond hair with 3 shade steps, cold blue eyes,
brown-red long robe with gold trim at the hem, cuffs and belt, small metal shoulder plate,
standing idle, [正面：facing the viewer / 侧面：facing right in 3/4 view, only one eye visible /
背面：seen from behind, no face / 攻击：raising her right hand, a glowing cyan core in her palm].
Palette: blond yellow, brown-red robe, gold trim, brown skin, cold blue glow.
```

> 出图顺序建议：先出**侧身**（3/4 视角最能定角色），再正面、背面、攻击。
> 附带产物：头像 16×16 我用正面图缩出来，不用单独出。

---

### P2 · 清道夫 Boss1 + 无人機 + 4 枚灵魂碎片

- **清道夫本体 6 帧** → `Scavenger_f0.png` … `f5.png`（110×110/帧）
  悬浮杀戮机械：冷灰废料外壳、液压活塞手臂、一只红色光学传感器、油污与锈迹。
  6 帧 = 3 帧悬浮上下 + 3 帧机械臂外张（前摇）。
  统一 prompt 见 §0，对象描述：`A hulking floating scrap-metal execution unit: a squat
  industrial chassis of cold grey rusted steel, exposed hydraulic pistons, a single red
  optical sensor in a cracked visor, a heavy segmented mechanical arm on the right side,
  oil stains, torn cables.` 帧尾注：`hovering high / mid / low` 与 `arm tucked / arm half /
  arm fully extended outward`.
- **维修无人机 4 帧** → `ScavengerRepairDrone_f0.png` … `f3.png`（30×26/帧）：
  `A small floating repair drone: a compact grey cylinder body, four tiny thrusters with blue
  flame, a hexagonal welding plate on the front, a thin antenna.`
- **4 枚灵魂碎片**（要一起出，风格才统一）：`SoulFragmentScavenger/Second/Third/Fourth.png`，24×24：
  `A floating soul fragment crystal: a faceted translucent shard with an inner glow, thin dark
  edge; Scavenger = cold grey-blue, Archivist = pale bone white, AshHeart = ember orange,
  FireplaceGuardian = pale furnace white-blue.`

---

### P3 · 40 把武器图标（只出 20 张 A 线，EX 我做）

图标尺寸（按职业固定）：战士 44×44 / 射手 44×44 / 法师 38×38 / 召唤师 40×40 / 盗贼 34×34。

| Boss | 武器（A 线类名） | 画布 |
| --- | --- | --- |
| 清道夫 | `ScavengerWarriorWeapon` / `MageWeapon` / `RangerWeapon` / `SummonerWeapon` / `RogueWeapon` | 44/38/44/40/34 |
| 归档者 | `ArchivistWarriorWeapon` / `MageWeapon` / `RangerWeapon` / `SummonerWeapon` / `RogueWeapon` | 同上 |
| 灰烬之心 | `AshHeartWarriorWeapon` / …（注意 EX 后缀是小写 `Ex`） | 同上 |
| 壁炉守卫 | `FireplaceWarriorWeapon` / … | 同上 |

模板（换掉 `<形状描述>` 与主题词）：

```
pixel art game sprite, retro 16-bit Terraria style, single weapon icon, centered, diagonal 45 degree,
flat pure magenta #FF00FF background, no shadow, no text, no border,
bold 1-pixel dark outline, limited palette, flat shading, crisp hard edges, no blur.
<形状描述>, made of <该 Boss 主题材质>. Holds a readable silhouette at <画布> pixels.
```

`<形状描述>` 参考：战士 = 大剑/斧刃；法师 = 法杖顶部嵌一颗发光核心；射手 = 枪管/弩；
召唤师 = 短杖 + 悬浮小符文；盗贼 = 回旋镖/投掷匕首。

---

### P4 · 精钢套装（15 件图标；**装备帧表不要用 AI**）

- **物品图标 22×22**：`SalvagedSteel<职业><部位>.png`（5 职业 × 头/胸/腿）。
  模板：`pixel art inventory icon of a single piece of salvaged steel armor: <头盔/胸甲/护腿>,
  brushed dark steel plates with visible rivets, orange rust edges, simple bold shape,
  readable at 22x22 pixels.`
- **装备帧表 40×1120（20 帧 × 40×56）不要交给 AI**：它是**绑定原版玩家骨架**的动画图集，
  20 帧分别是 4 朝向 × 行走/站立/跳跃/使用，AI 出的图无法对齐骨架，硬缩会撕裂。
  → 保持程序生成，或者以后单独找人按原版模板画。

---

### P5 · 3D 模型（见第 4 节，属于另一条产线）

AI 出图在这一环只能当**三视图参考**（正/侧/背），模型本体必须用 Blender 或 3D 生成工具。

---

## 4. 3D 路线：InnoVault `Models3D`（已查实，可行）

结论先说：**InnoVault 的 `Models3D` 原生支持骨骼蒙皮 glTF 动画，做真 3D Boss 是通的**，
不用自己写渲染器。依据是它 `.tmod` 里内嵌的**原始 C# 源码**（263 个 .cs，已解到
`E:\开发\.tmp-research\inno_src\`）+ 官方 wiki（`innovault.wiki/en/advanced/models3d/`）+ 官方示例。

### 4.1 硬性规格（**确凿**，来自源码原文）

| 项 | 要求 |
| --- | --- |
| 格式 | **只支持 `.obj` 和 `.gltf`**；`.glb` 明确不支持（源码原文 `binary .glb chunks are not supported yet`）；`.fbx` 完全不支持 |
| 贴图 | **必须独立 PNG 文件**（同目录、相对路径引用）；内嵌 / data URI 会被拒 |
| 导出方式 | Blender：**glTF Separate (.gltf + .bin + .png)** —— 不是 Binary(.glb)，也不是 Embedded |
| 坐标系 | 保持 **+Y Up**（InnoVault 导入时自动 `Y = -Y`，美术**不要自己翻**，否则上下颠倒） |
| 缩放 | 导入不换算单位；最终大小由代码里的 `Model3DInstance.Scale`（= 每单位多少像素）控制。建议模型包围盒高 **1.0~3.0 单位** |
| 动画 | 每个 Action 变成 `animation.name`，**区分大小写**；建议统一小写 `idle` / `attack` / `spawn` / `death` |
| 插值 | **用 Linear**（贝塞尔/三次会被降级为 LINEAR 并告警） |
| 蒙皮 | 每顶点 **≤ 4 根骨骼**权重；支持多骨架；骨骼命名无要求 |
| 材质 | 只有**一张 baseColor 贴图 + BasicEffect 方向光**；法线/金属/粗糙贴图**不会被读取** → 明暗细节请**烘进基础色贴图**。材质越少越好（每个材质 = 一次 draw call） |
| 不支持 | morph target / 形态键（会被告警跳过）、阴影（`CastShadow`/`ReceiveShadow` 是未实现的保留字段） |
| 预算（**经验推测**，源码无硬上限） | 每模型 < 2 万三角、骨骼 < 60 根（CPU 逐顶点蒙皮 + 每帧上传顶点） |

### 4.2 代码接线（我已经能写，等模型文件到位）

```csharp
// 1) 资源：静态属性必须带 setter；服务器端为 null，加载失败是 Empty，两种都要判
[VaultLoaden("Assets/Models/Archivist")]     // 探测 Archivist.obj → Archivist.gltf
public static Vault3DModel Archivist3D { get; set; }

// 2) 客户端建实例并常驻（不要每逻辑帧 Submit：高刷屏会闪）
if (!Main.dedServ) {
    var anim = new AnimationPlayer(asset);
    anim.Play("idle", fadeIn: 0.2f);
    model = new Model3DInstance(asset) {
        Layer = Model3DLayer.BeforePlayers,
        Scale = Vector3.One * 2f,       // 每单位 2 像素
        LightingEnabled = true,         // 默认 false，不开没立体感
        Animation = anim,
    };
    Model3DRenderer.RegisterPersistent(model);
}
// 3) AI() 里更新位置（⚠️ 别在 PreDrawInstance 里改，会滞后一帧）
model.Position = NPC.Center;
model.Rotation = new Vector3(0f, NPC.rotation, 0f);
// 4) OnKill 里 UnregisterPersistent，否则实体死了模型还在画
```

### 4.3 3D 的现实产线（三条路，按性价比排）

1. **ChatGPT 出三视图 → Blender 手工建模**（推荐做招牌 Boss）：
   你让 ChatGPT 出正/侧/背 + 线框参考（纯白底、正交视图），交给会建模的人
   （或你自己）建低模 + 绑骨骼 + 导出分离式 glTF。归档者是**对称机械体**，
   是最省事的试点：没有面部绑定、没有布料。
2. **图生 3D 工具**（Meshy / Tripo / Rodin 之类）：能出网格 + 自动骨骼，
   但免费额度比 ChatGPT 还紧，且导出多为 `.glb`——**要用 Blender 转存成分离式 `.gltf`**，
   贴图也要重新指向独立 PNG。
3. **保守路线**：Boss 保持 2D 帧表（P0~P4 那套），只给**关键道具/召唤物**做 3D 展示。

> ⚠️ 诚实提醒：**`Models3D` 我还没有在本工程里编译验证过**。上面的 API 签名来自
> InnoVault DLL 的 Cecil 转储 + 其内嵌源码 + 官方示例，可信度高，但第一次接线时
> 仍会踩编译期的坑（例如类型名、`VaultLoaden` 的用法）。真要做，建议**先拿一个
> 立方体走通全链路**（能显示、能动、能卸载），再上正式模型。

---

## 5. 交付约定（照抄即可）

```
E:\开发\art-inbox\
├─ Archivist_f0.png      ← P0-1 第 1 帧
├─ Archivist_f1.png      ← P0-1 第 2 帧
├─ Archivist_f2.png      ← P0-1 第 3 帧
├─ Archivist_f3.png      ← P0-1 第 4 帧
├─ Archivist_head.png    ← P0-2 血条头像
├─ ArchivistIndexBeam.png
├─ ArchivistArchivePage.png
├─ ArchivistBarrageSheet.png
├─ ArchivistSeal.png
├─ ArchivistEcho.png
├─ ArchivistBag.png
├─ ArchivistFragment.png
├─ ScavengerWarriorWeapon.png …（P0 的 4 把归档者武器按类名命名）
├─ Companion_side.png / _front.png / _back.png / _attack.png
└─ Scavenger_f0.png … Scavenger_f5.png
```

- **文件名就叫这个**（我按类名对照写入模组，改名字我得手动映射，容易错）。
- 出完一批（比如 P0 全部）在对话里说一声「art-inbox 里 P0 出好了」，
  我会自动：去背 → 缩放量化 → 拼帧表 → 写进工程 → `check_assets` → 跑流水线 → 装机。
- 每张图我都会留一份**原图副本**（`E:\开发\art-inbox\raw\`），改坏了能回退。
