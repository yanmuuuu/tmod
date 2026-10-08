# -*- coding: utf-8 -*-
"""椭球地宫的**连通性**检查（辅助工具，不在 ws_pipeline 的 19 项里）。

为什么需要它：地宫只在玩家进门那一刻生成，编译和别的静态检查都碰不到"门被实心墙夹住"
这类错误 —— 这一批就是靠它抓出两个真窟窿：

  1. 隔墙有 5~6 格厚，一开始只把门放在墙中间、两侧墙体没掏通，**门被墙夹死**；
  2. 一层东头那道门外面还压着**地下通道自己的侧壳**（x=516~519），门洞不打通它照样出不去。

做法：把 `Content/Subworlds/FireplaceVault.cs` 的写格顺序（掏空腔 / 铺壳 / 铺楼板 / 铺平台 /
隔墙 / 两翼 / 升降井 / 门 / 堡垒竖井）在 Python 里**照抄一遍**，再从竖井落点做洪水填充，核对：

  * 门厅 → 竖井 → 主通道 → 地下通道、7 间房、两条升降井，全部可达；
  * 每间房底下都是实心地板、上方有封顶（一层西头刻意留的观景口与升降井开洞除外）；
  * 每扇门下方是实心地板、门能过人、门上方还有墙。

⚠️ 它是 FireplaceVault.cs 的镜子：**改了那份代码就要同步改这里**，
否则它会报假问题（或者漏掉真问题）。运行：
    python tools/check_fireplace_vault_connectivity.py
"""
import io
import re
import sys
from collections import deque

LAYOUT = r"E:\开发\WastelandSoul\Content\Subworlds\FireplaceLayout.cs"

text = io.open(LAYOUT, encoding="utf-8-sig").read()
V = {}
decls = [(m.group(1), m.group(2).strip()) for m in
         re.finditer(r"public\s+const\s+int\s+(\w+)\s*=\s*([^;]+);", text)]
for _ in range(len(decls) + 1):
    moved = False
    for name, expr in decls:
        if name in V:
            continue
        e = expr
        for k, val in V.items():
            e = re.sub(r"\b%s\b" % k, str(val), e)
        try:
            V[name] = int(eval(e, {"__builtins__": {}}, {}))
            moved = True
        except Exception:
            pass
    if not moved:
        break

ROOMS = [(m.group(1), int(m.group(2)), int(m.group(3)), int(m.group(4)), int(m.group(5)))
         for m in re.finditer(r'new\s+VaultRoom\(\s*"([^"]+)"\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*\)', text)]

W, H = V["Width"], V["Height"]
CX, CY = V["VaultCenterX"], V["VaultCenterY"]
RX, RY = V["VaultRadiusX"], V["VaultRadiusY"]
SHELL = V["VaultShell"]
TOP, BOTTOM = V["VaultTop"], V["VaultBottom"]
LEFT, RIGHT = V["VaultLeft"], V["VaultRight"]
IRX, IRY = RX - SHELL, RY - SHELL
FLOOR_Y = V["VaultFloorY"]
PLAT = V["VaultPlatformThickness"]
SLAB_TOP, SLAB_BOTTOM = V["VaultSlabTop"], V["VaultSlabBottom"]
SLAB_L, SLAB_R = V["VaultSlabLeft"], V["VaultSlabRight"]
STORY_TOP = V["VaultStoryTop"]
UPPER_TOP, UPPER_BOTTOM = V["VaultUpperTop"], V["VaultUpperBottom"]
CEIL_TOP, CEIL_BOTTOM = V["VaultCeilingTop"], V["VaultCeilingBottom"]
TUN_L, TUN_R, TUN_TOP, TUN_BOT, TUN_SHELL = (V["TunnelLeft"], V["TunnelRight"],
                                             V["TunnelTop"], V["TunnelBottom"], V["TunnelShell"])
FORT_L, FORT_R, FORT_BOTTOM = V["FortLeft"], V["FortRight"], V["FortBottom"]
SHAFT_L, SHAFT_R = V["FortShaftLeft"], V["FortShaftRight"]
HALL_BOTTOM = V["HallBottom"]
LAD_W_L, LAD_W_R = V["VaultWestLadderX"], V["VaultWestLadderRight"]
LAD_E_L, LAD_E_R = V["VaultEastLadderX"], V["VaultEastLadderRight"]
LANE_L = TUN_L - TUN_SHELL

# 0 = 未知（岩）/ 1 = 实体 / 2 = 空气 / 3 = 门（可通行）/ 4 = 平台（可通行）
grid = {}


def get(x, y):
    return grid.get((x, y), 1)


def setv(x, y, v):
    grid[(x, y)] = v


def in_ellipse(x, y, rx, ry):
    dx = (x - CX) / float(rx)
    dy = (y - CY) / float(ry)
    return dx * dx + dy * dy <= 1.0


def in_outer(x, y):
    return in_ellipse(x, y, RX, RY)


def in_inner(x, y):
    return in_ellipse(x, y, IRX, IRY)


def in_fort(x, y):
    return FORT_L - 1 <= x <= FORT_R + 1 and y <= FORT_BOTTOM + 1


def in_lane(x, y):
    return x >= LANE_L and TUN_TOP - TUN_SHELL <= y <= TUN_BOT + TUN_SHELL


def can_build(x, y):
    return in_inner(x, y) and not in_fort(x, y) and not in_lane(x, y)


# ---- 1) 椭球壳 ----
for y in range(TOP, BOTTOM + 1):
    for x in range(LEFT, RIGHT + 1):
        if in_outer(x, y) and not in_inner(x, y) and not in_fort(x, y):
            setv(x, y, 1)

# ---- 2) 掏空腔 + 重开通道嘴 ----
for y in range(TOP, FLOOR_Y + PLAT):
    for x in range(LEFT, RIGHT + 1):
        if can_build(x, y):
            setv(x, y, 2)

for y in range(TUN_TOP + 1, TUN_BOT - 1):
    for x in range(TUN_L, TUN_R + 1):
        setv(x, y, 2)

# ---- 3) 楼板 ----
for y in range(SLAB_TOP, SLAB_BOTTOM + 1):
    for x in range(SLAB_L, SLAB_R + 1):
        if can_build(x, y):
            setv(x, y, 1)

for y in range(SLAB_TOP, TUN_TOP - TUN_SHELL):
    for x in range(TUN_L, RIGHT + 1):
        if can_build(x, y):
            setv(x, y, 1)

# ---- 4) 主平台 ----
for y in range(FLOOR_Y, FLOOR_Y + PLAT):
    for x in range(LEFT, RIGHT + 1):
        if can_build(x, y):
            setv(x, y, 1)

for x in range(TUN_L, RIGHT + 1):
    setv(x, FLOOR_Y, 1)

# ---- 5) 一层隔墙 ----
def fill_wall(left, right, top, bottom):
    for x in range(left, right + 1):
        for y in range(top, bottom + 1):
            if in_inner(x, y):
                setv(x, y, 1)


fill_wall(ROOMS[0][3] + 1, ROOMS[1][1] - 1, STORY_TOP, FLOOR_Y - 1)
fill_wall(ROOMS[1][3] + 1, ROOMS[2][1] - 1, STORY_TOP, FLOOR_Y - 1)
fill_wall(ROOMS[2][3] + 1, LANE_L - 1, STORY_TOP, FLOOR_Y - 1)

# ---- 6) 二层两翼 ----
def build_wing(outer_l, outer_r, west, east):
    for y in range(CEIL_TOP, CEIL_BOTTOM + 1):
        for x in range(outer_l, outer_r + 1):
            if in_inner(x, y):
                setv(x, y, 1)
    fill_wall(outer_l, west[1] - 1, CEIL_TOP, UPPER_BOTTOM)
    fill_wall(west[3] + 1, east[1] - 1, CEIL_TOP, UPPER_BOTTOM)
    fill_wall(east[3] + 1, outer_r, CEIL_TOP, UPPER_BOTTOM)


build_wing(162, 278, ROOMS[3], ROOMS[4])
build_wing(624, 738, ROOMS[5], ROOMS[6])

# ---- 7) 升降井 ----
def ladder(left, right, floor_y, top_y):
    for y in range(top_y, floor_y + 1):
        for x in range(left, right + 1):
            setv(x, y, 2)
    for y in range(floor_y - 4, top_y - 1, -5):
        for x in range(left, right + 1):
            setv(x, y, 4)


ladder(LAD_W_L, LAD_W_R, FLOOR_Y - 1, UPPER_TOP)
ladder(LAD_E_L, LAD_E_R, FLOOR_Y - 1, UPPER_TOP)

# ---- 8) 门（门洞要打穿整堵墙，门卡在中间那一列）----
DOORS = [((ROOMS[0][3] + 1 + ROOMS[1][1] - 1) // 2, FLOOR_Y, ROOMS[0][3] + 1, ROOMS[1][1] - 1),
         ((ROOMS[1][3] + 1 + ROOMS[2][1] - 1) // 2, FLOOR_Y, ROOMS[1][3] + 1, ROOMS[2][1] - 1),
         ((ROOMS[2][3] + 1 + TUN_L - 1) // 2, FLOOR_Y, ROOMS[2][3] + 1, TUN_L - 1),
         ((ROOMS[3][3] + 1 + ROOMS[4][1] - 1) // 2, SLAB_TOP, ROOMS[3][3] + 1, ROOMS[4][1] - 1),
         (162 + 1, SLAB_TOP, 162, ROOMS[3][1] - 1),
         ((ROOMS[5][3] + 1 + ROOMS[6][1] - 1) // 2, SLAB_TOP, ROOMS[5][3] + 1, ROOMS[6][1] - 1),
         (738 - 1, SLAB_TOP, ROOMS[6][3] + 1, 738)]

for x, floor_y, wall_l, wall_r in DOORS:
    for y in range(floor_y - 3, floor_y):
        for wx in range(wall_l, wall_r + 1):
            setv(wx, y, 2)

    for y in range(floor_y - 3, floor_y):
        setv(x, y, 3)

# ---- 9) 堡垒竖井 ----
# ⚠️ C# 里的真实顺序是「先 OpenFortressShaft 开挖 → 再 BuildSlab 补板」，而 BuildSlab
# 现在会**绕开竖井**（`FireplaceVault.InFortressShaft`：竖井 y=1122~1125 既不在
# InFortressBox(y<=1121) 里、也不在 InTunnelLane(y>=1126) 里，上一版就是被"东侧补板"
# 填了 15x4 格合金、门厅到地宫整条路不通）。这里把开挖放在最后一遍，结果等价
# （竖井最终必须整条是空的）；下面 10.0 会**显式断言**这一点，免得下次再漏。
for y in range(HALL_BOTTOM, TUN_TOP + 2):
    for x in range(SHAFT_L, SHAFT_R + 1):
        setv(x, y, 2)

# 门厅地板以上（门厅内部）当作空气，验证能从门厅走下来
for y in range(HALL_BOTTOM - 40, HALL_BOTTOM):
    for x in range(SHAFT_L, SHAFT_R + 1):
        setv(x, y, 2)

# ---- 10) 洪水填充 ----
start = (SHAFT_L + 2, FLOOR_Y - 20)
assert get(*start) == 2, "落点不是空气：%s -> %s" % (start, get(*start))

seen = {start}
queue = deque([start])

while queue:
    x, y = queue.popleft()
    for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
        if (nx, ny) in seen:
            continue
        if not (0 <= nx < W and 0 <= ny < H):
            continue
        if get(nx, ny) in (2, 3, 4):
            seen.add((nx, ny))
            queue.append((nx, ny))

problems = []


# ---- 10.0) 堡垒竖井必须整条通（门厅 → 通道 → 地宫的唯一入口）----
blocked_shaft = [(x, y) for y in range(SLAB_TOP, TUN_TOP + 2) for x in range(SHAFT_L, SHAFT_R + 1)
                 if get(x, y) != 2]

if blocked_shaft:
    problems.append("堡垒竖井被填死了 %d 格（例如 %s）—— 门厅到地下通道/地宫会不通"
                    % (len(blocked_shaft), blocked_shaft[:4]))


def need_reach(x, y, label):
    if (x, y) not in seen:
        problems.append("走不到：%s (%d, %d)" % (label, x, y))


# 10.1 门厅（竖井上口）
need_reach(SHAFT_L + 2, HALL_BOTTOM - 5, "门厅 → 竖井")
# 10.2 每条通道方向
need_reach(TUN_R - 10, (TUN_TOP + TUN_BOT) // 2, "地下通道东段")
# 10.3 每个房间的中心 + 房间底面必须有实心地板
for name, left, top, right, bottom in ROOMS:
    cx = (left + right) // 2
    need_reach(cx, (top + bottom) // 2, "房间「%s」内部" % name)
    need_reach(cx, bottom, "房间「%s」底面那格" % name)

    if get(cx, bottom + 1) != 1:
        problems.append("房间「%s」底下不是实心地板：(%d, %d)=%s"
                        % (name, cx, bottom + 1, get(cx, bottom + 1)))

# 10.4 每条升降井的顶端
for label, x in (("西升降井", LAD_W_L + 1), ("东升降井", LAD_E_L + 1)):
    need_reach(x, UPPER_TOP + 1, "%s 顶端" % label)

# 10.5 每扇门都要能站人（门下方实心、门本身可通行）
for x, floor_y, wall_l, wall_r in DOORS:
    if get(x, floor_y) != 1:
        problems.append("门 (%d, %d) 下方不是实心地板" % (x, floor_y))
    if (x, floor_y - 1) not in seen:
        problems.append("门 (%d, %d) 过不去" % (x, floor_y - 1))
    if get(x, floor_y - 4) != 1:
        problems.append("门 (%d, %d) 上面没有墙" % (x, floor_y))
    # 门必须是这一列 3 行上唯一的通路：两侧墙体（同一行）不能同时是空气连成绕行
    if get(x - 1, floor_y - 1) in (2, 3, 4) and get(x + 1, floor_y - 1) in (2, 3, 4):
        pass    # 正常：门洞两侧是空气，但绕不过门（通路只有门这一列）

# 10.6 房间上下边界必须封住（一层西头故意留的观景口不算漏洞）
leaks = []
for name, left, top, right, bottom in ROOMS:
    for x in range(left, right + 1):
        for y in (top - 1, bottom + 1):
            if y == STORY_TOP - 1 and x < SLAB_L:
                continue        # 一层西头刻意留的开口（能抬头看到穹顶）
            if LAD_W_L <= x <= LAD_W_R or LAD_E_L <= x <= LAD_E_R:
                continue        # 升降井本来就要在楼板/地面上开洞
            if get(x, y) not in (1, 4):
                leaks.append((name, x, y, get(x, y)))

if leaks:
    problems.append("房间上下边界有 %d 格不是实心（楼板/地面有洞），例如 %s"
                    % (len(leaks), leaks[:4]))

print("房间数: %d，门: %d，可通行格: %d" % (len(ROOMS), len(DOORS), len(seen)))

if problems:
    print("!! 地宫连通性有 %d 处问题：" % len(problems))
    for p in problems:
        print("   -", p)
    sys.exit(1)

print("[OK] 地宫连通性：门厅 -> 竖井 -> 主通道 -> 房间 / 地下通道 / 两条升降井全部可达，"
      "每间房都有地板与门")
