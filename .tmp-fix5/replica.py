# -*- coding: utf-8 -*-
"""壁炉生成几何复刻（离线取证用，不属于交付物）。

目的：把 FireplaceTerrain / FireplaceBuildings / FireplaceVault 的**写格顺序**在 Python 里
照抄一遍，然后回答几个只能靠坐标回答的问题：

  1. 有没有「先砌好的瑜钢合金，后来又被挖掉/覆盖成别的方块」？在哪些坐标？
  2. 生成结束后，结构内部有没有不该有的空洞？
  3. 岩石层里的 Sand / Silt（受重力）有没有下面是空气的？掉了之后会落在哪？会不会落进结构？
  4. 门厅 -> 竖井 -> 主通道 -> 地宫 / 地下通道 到底通不通？

状态：0=空气 1=其他实体（灰烬/土/石…）2=瑜钢合金 3=瑜钢压条 4=门 5=平台
"""
import io
import re
import sys
from collections import deque

LAYOUT = r"E:\开发\WastelandSoul\Content\Subworlds\FireplaceLayout.cs"

TEXT = io.open(LAYOUT, encoding="utf-8-sig").read()
V = {}
DECLS = [(m.group(1), m.group(2).strip()) for m in
         re.finditer(r"public\s+const\s+int\s+(\w+)\s*=\s*([^;]+);", TEXT)]
for _ in range(len(DECLS) + 1):
    moved = False
    for name, expr in DECLS:
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
         for m in re.finditer(r'new\s+VaultRoom\(\s*"([^"]+)"\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*\)', TEXT)]

W = H = 0
GRID = None
TRACE = []          # (x, y, old, new, step)
STEP = ["?"]


def build_grid():
    global GRID, W, H
    W, H = V["Width"], V["Height"]
    GRID = [[1] * W for _ in range(H)]        # 1 = 任意实体（地形）


def get(x, y):
    if 0 <= x < W and 0 <= y < H:
        return GRID[y][x]
    return 1


def setv(x, y, v, why="?"):
    if not (0 <= x < W and 0 <= y < H):
        return
    old = GRID[y][x]
    if old == v:
        return
    GRID[y][x] = v
    TRACE.append((x, y, old, v, STEP[0], why))


# ---------------- 几何判定（照抄 FireplaceVault） ----------------

CX, CY = V["VaultCenterX"], V["VaultCenterY"]
RX, RY = V["VaultRadiusX"], V["VaultRadiusY"]
SHELL = V["VaultShell"]
IRX, IRY = RX - SHELL, RY - SHELL
TOP, BOTTOM = V["VaultTop"], V["VaultBottom"]
LEFT, RIGHT = V["VaultLeft"], V["VaultRight"]
FLOOR_Y = V["VaultFloorY"]
PLAT = V["VaultPlatformThickness"]
SLAB_TOP, SLAB_BOTTOM = V["VaultSlabTop"], V["VaultSlabBottom"]
SLAB_L, SLAB_R = V["VaultSlabLeft"], V["VaultSlabRight"]
STORY_TOP = V["VaultStoryTop"]
UPPER_TOP, UPPER_BOTTOM = V["VaultUpperTop"], V["VaultUpperBottom"]
CEIL_TOP, CEIL_BOTTOM = V["VaultCeilingTop"], V["VaultCeilingBottom"]
TUN_L, TUN_R, TUN_TOP, TUN_BOT, TUN_SHELL = (V["TunnelLeft"], V["TunnelRight"],
                                             V["TunnelTop"], V["TunnelBottom"], V["TunnelShell"])
FORT_L, FORT_R, FORT_TOP, FORT_BOTTOM = V["FortLeft"], V["FortRight"], V["FortTop"], V["FortBottom"]
FORT_SHELL = V["FortShell"]
HALL_L, HALL_R, HALL_TOP, HALL_BOTTOM = V["HallLeft"], V["HallRight"], V["HallTop"], V["HallBottom"]
HEARTH_L, HEARTH_R = V["HearthLeft"], V["HearthRight"]
TERM_X, TERM_Y = V["TerminalX"], V["TerminalY"]
EXIT_X = V["ExitX"]
SHAFT_L, SHAFT_R = V["FortShaftLeft"], V["FortShaftRight"]
LAD_W_L, LAD_W_R = V["VaultWestLadderX"], V["VaultWestLadderRight"]
LAD_E_L, LAD_E_R = V["VaultEastLadderX"], V["VaultEastLadderRight"]
LANE_L = TUN_L - TUN_SHELL
ARENA_L, ARENA_R, ARENA_TOP, ARENA_BOTTOM = (V["ArenaLeft"], V["ArenaRight"],
                                             V["ArenaTop"], V["ArenaBottom"])
TOWER_L, TOWER_R, TOWER_TOP, TOWER_BOTTOM = (V["TowerLeft"], V["TowerRight"],
                                             V["TowerTop"], V["TowerBottom"])


def in_world(x, y):
    return 0 <= x < W and 0 <= y < H


def in_ellipse(x, y, rx, ry):
    dx = (x - CX) / float(rx)
    dy = (y - CY) / float(ry)
    return dx * dx + dy * dy <= 1.0


def in_outer(x, y):
    return in_world(x, y) and in_ellipse(x, y, RX, RY)


def in_inner(x, y):
    return in_world(x, y) and in_ellipse(x, y, IRX, IRY)


def in_fort(x, y):
    return FORT_L - 1 <= x <= FORT_R + 1 and y <= FORT_BOTTOM + 1


def in_fort_shaft(x, y):
    """与 C# FireplaceVault.InFortressShaft 一致（本批新增）。"""
    return (SHAFT_L - 1 <= x <= SHAFT_R + 1
            and HALL_BOTTOM <= y <= TUN_TOP + 1)


def in_lane(x, y):
    return x >= LANE_L and TUN_TOP - TUN_SHELL <= y <= TUN_BOT + TUN_SHELL


def can_build(x, y):
    return in_inner(x, y) and not in_fort(x, y) and not in_lane(x, y)


# ---------------- WorldPaint 原语 ----------------

def carve(l, t, r, b, why="carve"):
    for x in range(l, r + 1):
        for y in range(t, b + 1):
            if get(x, y) != 0:
                setv(x, y, 0, why)


def rect(l, t, r, b, v, why="rect"):
    for x in range(l, r + 1):
        for y in range(t, b + 1):
            setv(x, y, v, why)


def frame(l, t, r, b, v, th, why="frame"):
    for k in range(th):
        for x in range(l + k, r - k + 1):
            setv(x, t + k, v, why)
            setv(x, b - k, v, why)
        for y in range(t + k, b - k + 1):
            setv(l + k, y, v, why)
            setv(r - k, y, v, why)


def hline(l, r, y, v, why="hline"):
    for x in range(l, r + 1):
        setv(x, y, v, why)


def vline(x, t, b, v, why="vline"):
    for y in range(t, b + 1):
        setv(x, y, v, why)


ALLOY, TRIM = 2, 3


# ---------------- 1) 灰烬原野（只做「实体」，沙另行统计） ----------------

def terrain_solid():
    """把地形写成实体；吐出的沙/泥沙位置由另外的噪声统计给出（这里不细分）。"""
    for x in range(W):
        top = V["SurfaceBase"]       # 地表高度这里不精确复刻，结构都在 860 以下，够用
        for y in range(top, H):
            setv(x, y, 1, "terrain")


# ---------------- 2) 堡垒（FireplaceBuildings.BuildFortress） ----------------

def build_fortress():
    left, right, top, bottom, shell = FORT_L, FORT_R, FORT_TOP, FORT_BOTTOM, FORT_SHELL

    carve(left - 1, top - 1, right + 1, bottom + 2, "fort:carve")
    frame(left, top, right, bottom, ALLOY, shell, "fort:shell")

    for y in range(top, bottom + 1):
        if (y - top) % 12 == 0:
            hline(left, right, y, TRIM, "fort:trim")

    frame(HALL_L, HALL_TOP, HALL_R, HALL_BOTTOM, ALLOY, 1, "fort:hallframe")
    carve(left + shell, top + shell, right - shell, bottom - shell, "fort:inner")

    # 3.5) 门厅地板（本批新增：内腔 Carve 把 Frame(hall) 一起挖掉了，必须重铺）
    rect(HALL_L, HALL_BOTTOM, HALL_R, HALL_BOTTOM + 2, ALLOY, "fort:hallfloor")
    hline(HALL_L, HALL_R, HALL_BOTTOM, TRIM, "fort:hallfloor-trim")

    # 与塔、与地下通道连通（竖井从 hallBottom 起挖，本批改了）
    carve(TOWER_L + 6, top - 1, TOWER_R - 6, HALL_TOP, "fort:towerlink")
    carve(SHAFT_L, HALL_BOTTOM, SHAFT_R, TUN_TOP - 2, "fort:shaft")

    # 炉膛
    carve(HEARTH_L, HALL_BOTTOM - 2, HEARTH_R, HALL_BOTTOM, "fort:hearth")
    rect(HEARTH_L - 3, HALL_BOTTOM + 1, HEARTH_R + 3, HALL_BOTTOM + 1, 1, "fort:hearthfloor")
    for y in range(HALL_BOTTOM - 2, HALL_BOTTOM + 2):
        for i in range(3):
            setv(HEARTH_L - 3 + i, y, 1, "fort:hearthwall")
            setv(HEARTH_R + 3 - i, y, 1, "fort:hearthwall")
    hline(HEARTH_L, HEARTH_R, HALL_BOTTOM, 1, "fort:livingfire")
    hline(HEARTH_L - 16, HEARTH_L - 4, HALL_BOTTOM, TRIM, "fort:hearthtrim")
    hline(HEARTH_R + 4, HEARTH_R + 16, HALL_BOTTOM, TRIM, "fort:hearthtrim")

    # 终端 + 返回门
    tl, tr, ty = TERM_X - 12, TERM_X + 12, TERM_Y
    hline(tl, tr, ty, 1, "fort:terminal")
    hline(tl, tr, ty - 1, 1, "fort:terminal")
    carve(TERM_X - 1, ty - 6, TERM_X + 1, ty - 3, "fort:terminalcarve")
    setv(TERM_X, ty - 3, 1, "fort:terminalblock")
    carve(EXIT_X - 1, HALL_BOTTOM - 4, EXIT_X + 1, HALL_BOTTOM - 1, "fort:exitcarve")
    setv(EXIT_X, HALL_BOTTOM - 1, 1, "fort:exitblock")

    # DecorateHall 的主干（走廊 / 侧房）
    mid_y = HALL_TOP + (HALL_BOTTOM - HALL_TOP) // 2
    hline(HALL_L, HALL_R, mid_y, ALLOY, "fort:midfloor")
    carve(HALL_L + 2, mid_y - 4, HALL_R - 2, mid_y - 1, "fort:midcarve")
    carve(HALL_L + 6, mid_y, HALL_L + 9, mid_y, "fort:midgap")
    carve(HALL_R - 9, mid_y, HALL_R - 6, mid_y, "fort:midgap")

    for i in range(3):
        rl = HALL_L + 10 + (i * 48)
        rr = rl + 30
        if rr >= HALL_R - 10:
            break
        vline(rr, mid_y - 12, mid_y - 1, TRIM, "fort:roomwall")
        carve(rl, mid_y - 12, rr - 1, mid_y - 2, "fort:roomcarve")
        hline(rl, rr - 1, mid_y - 1, ALLOY, "fort:roomfloor")


# ---------------- 3) 祈塔（只做壳体/楼板，够判断与地宫的重叠） ----------------

def build_tower():
    shell = V["TowerShell"]
    frame(TOWER_L, TOWER_TOP, TOWER_R, TOWER_BOTTOM, ALLOY, shell, "tower:shell")
    for y in range(TOWER_TOP, TOWER_BOTTOM + 1):
        if (y - TOWER_TOP) % 20 == 0:
            hline(TOWER_L, TOWER_R, y, TRIM, "tower:trim")
    carve(TOWER_L + shell, TOWER_TOP + shell, TOWER_R - shell, TOWER_BOTTOM - 1, "tower:inner")
    floors = (TOWER_BOTTOM - TOWER_TOP) // V["TowerFloorHeight"]
    for i in range(1, floors):
        y = TOWER_BOTTOM - (i * V["TowerFloorHeight"])
        hline(TOWER_L + shell, TOWER_R - shell, y, ALLOY, "tower:floor")
        for x in range(TOWER_L + shell, TOWER_L + shell + 8):
            setv(x, y, 0, "tower:shaft")
    for y in range(TOWER_BOTTOM - 5, TOWER_TOP + 4, -5):
        for x in range(TOWER_L + shell + 1, TOWER_L + shell + 8):
            setv(x, y, 5, "tower:platform")


# ---------------- 4) 地下通道（FireplaceBuildings.BuildTunnel） ----------------

def build_tunnel():
    left, right, top, bottom, shell = TUN_L, TUN_R, TUN_TOP, TUN_BOT, TUN_SHELL
    carve(left - shell, top - shell, right + shell, bottom + shell, "tun:carve")
    frame(left - shell, top - shell, right + shell, bottom + shell, ALLOY, shell, "tun:shell")
    hline(left, right, top, TRIM, "tun:trimtop")
    hline(left, right, bottom - 1, TRIM, "tun:trimfloor")
    for x in range(left + 20, right - 20 + 1, 44):
        frame(x, top + 2, x + 1, bottom - 2, TRIM, 1, "tun:pillar")
        frame(x + 3, top + 2, x + 4, bottom - 2, TRIM, 1, "tun:pillar")
    rect(right + 1, top - shell, right + shell + 4, bottom + shell, ALLOY, "tun:endcap")
    hline(right + 1, right + shell + 4, top + 8, TRIM, "tun:endtrim")


# ---------------- 5) 椭球地宫（FireplaceVault.Build） ----------------

def v_open_fortress_shaft():
    carve(SHAFT_L, HALL_BOTTOM, SHAFT_R, TUN_TOP + 1, "vault:openshaft")
    vline(SHAFT_L - 2, SLAB_TOP, TUN_TOP, TRIM, "vault:shafttrim")
    vline(SHAFT_R + 2, SLAB_TOP, TUN_TOP, TRIM, "vault:shafttrim")


def v_build_shell():
    step = V["VaultTrimStep"]
    for y in range(TOP, BOTTOM + 1):
        band = (y - TOP) % step == 0
        for x in range(LEFT, RIGHT + 1):
            if not in_outer(x, y) or in_inner(x, y) or in_fort(x, y):
                continue
            setv(x, y, TRIM if band else ALLOY, "vault:shell")


def v_build_cavity():
    bottom = FLOOR_Y + PLAT - 1
    for y in range(TOP, bottom + 1):
        for x in range(LEFT, RIGHT + 1):
            if can_build(x, y):
                setv(x, y, 0, "vault:cavity")
    carve(TUN_L, TUN_TOP + 1, TUN_R, TUN_BOT - 2, "vault:tunnelmouth")


def v_build_slab():
    for y in range(SLAB_TOP, SLAB_BOTTOM + 1):
        for x in range(SLAB_L, SLAB_R + 1):
            if can_build(x, y) and not in_fort_shaft(x, y):
                setv(x, y, ALLOY, "vault:slab")
    for y in range(SLAB_TOP, TUN_TOP):
        for x in range(TUN_L, RIGHT + 1):
            if can_build(x, y) and not in_fort_shaft(x, y):
                setv(x, y, ALLOY, "vault:slab-east")
    # 堡垒底壳与楼板之间那条 1 格空腔：只补空气、不动竖井
    for x in range(FORT_L, FORT_R + 1):
        y = FORT_BOTTOM + 1
        if in_fort_shaft(x, y) or get(x, y) != 0:
            continue
        setv(x, y, ALLOY, "vault:seal1121")


def v_build_platform():
    bottom = FLOOR_Y + PLAT - 1
    for y in range(FLOOR_Y, bottom + 1):
        for x in range(LEFT, RIGHT + 1):
            if can_build(x, y):
                setv(x, y, TRIM if y == FLOOR_Y else ALLOY, "vault:platform")
    hline(TUN_L, RIGHT, FLOOR_Y, TRIM, "vault:platform-tunnel")


def v_fill_wall(l, r, t, b, why="vault:wall"):
    if r < l:
        return
    for x in range(l, r + 1):
        v = TRIM if (x == l or x == r) else ALLOY
        for y in range(t, b + 1):
            if in_inner(x, y):
                setv(x, y, v, why)


def v_story_walls():
    top, bottom = STORY_TOP, FLOOR_Y - 1
    wall_a = ROOMS[0][3] + 1
    wall_b = ROOMS[1][1] - 1
    wall_c = ROOMS[1][3] + 1
    wall_d = ROOMS[2][1] - 1
    wall_e = ROOMS[2][3] + 1
    wall_f = TUN_L - TUN_SHELL - 1
    v_fill_wall(wall_a, wall_b, top, bottom)
    v_fill_wall(wall_c, wall_d, top, bottom)
    v_fill_wall(wall_e, wall_f, top, bottom)
    door_east = TUN_L - 1
    place_door((wall_a + wall_b) // 2, FLOOR_Y, wall_a, wall_b)
    place_door((wall_c + wall_d) // 2, FLOOR_Y, wall_c, wall_d)
    place_door((wall_e + door_east) // 2, FLOOR_Y, wall_e, door_east)


def v_build_wing(outer_l, outer_r, west, east, attic_door_on_west):
    top, bottom = UPPER_TOP, UPPER_BOTTOM
    for y in range(CEIL_TOP, CEIL_BOTTOM + 1):
        for x in range(outer_l, outer_r + 1):
            if in_inner(x, y):
                setv(x, y, ALLOY, "vault:wing-ceiling")
    wall_a, wall_b = outer_l, west[1] - 1
    wall_c, wall_d = west[3] + 1, east[1] - 1
    wall_e, wall_f = east[3] + 1, outer_r
    v_fill_wall(wall_a, wall_b, top, bottom, "vault:wing-wall")
    v_fill_wall(wall_c, wall_d, top, bottom, "vault:wing-wall")
    v_fill_wall(wall_e, wall_f, top, bottom, "vault:wing-wall")
    place_door((wall_c + wall_d) // 2, SLAB_TOP, wall_c, wall_d)
    if attic_door_on_west:
        place_door(wall_a + 1, SLAB_TOP, wall_a, wall_b)
    else:
        place_door(wall_f - 1, SLAB_TOP, wall_e, wall_f)


def v_ladder(l, r, floor_y, top_y):
    for y in range(top_y, floor_y + 1):
        for x in range(l, r + 1):
            setv(x, y, 0, "vault:ladder-carve")
    for y in range(floor_y - 4, top_y - 1, -5):
        for x in range(l, r + 1):
            setv(x, y, 5, "vault:ladder-platform")


def place_door(x, floor_y, carve_l, carve_r):
    bottom = floor_y - 1
    carve(carve_l, bottom - 2, carve_r, bottom, "vault:door-hole")
    for i in range(3):
        setv(x, bottom - i, 4, "vault:door")


def v_decorate():
    for room in ROOMS:
        upper = room[2] == UPPER_TOP
        pass
    # 只复刻会改方块的：地面宝石微光（Retile 地板）与房间内地板
    for name, left, top, right, bottom in ROOMS:
        upper = top == UPPER_TOP
        floor_y = SLAB_TOP if upper else FLOOR_Y
        for x in range(left + 2, right - 2 + 1, 4):
            pass


def build_vault():
    v_open_fortress_shaft()
    v_build_shell()
    v_build_cavity()
    v_build_slab()
    v_build_platform()
    v_story_walls()
    if len(ROOMS) >= 7:
        v_build_wing(162, 278, ROOMS[3], ROOMS[4], True)
        v_build_wing(624, 738, ROOMS[5], ROOMS[6], False)
    v_ladder(LAD_W_L, LAD_W_R, FLOOR_Y - 1, UPPER_TOP)
    v_ladder(LAD_E_L, LAD_E_R, FLOOR_Y - 1, UPPER_TOP)
    v_decorate()
    # 最后一步：通道嘴重开后把通道立柱补回来（本批新增的调用）
    for x in range(TUN_L + 20, TUN_R - 20 + 1, 44):
        for (a, b) in ((x, x + 1), (x + 3, x + 4)):
            for yy in range(TUN_TOP + 2, TUN_BOT - 2 + 1):
                for xx in range(a, b + 1):
                    setv(xx, yy, TRIM, "vault:pillar-restore")


# ---------------- 主流程 ----------------

def run():
    build_grid()
    STEP[0] = "1 地形"
    terrain_solid()
    STEP[0] = "2 堡垒"
    build_fortress()
    STEP[0] = "3 祈塔"
    build_tower()
    STEP[0] = "4 通道"
    build_tunnel()
    STEP[0] = "5 地宫"
    build_vault()
    STEP[0] = "6 收尾"


def report_alloy_loss():
    print("=" * 90)
    print("A) 「先砌合金、后被改掉」的格子（生成期内）")
    lost = [t for t in TRACE if t[2] in (ALLOY, TRIM) and t[3] not in (ALLOY, TRIM)]
    print("   总数:", len(lost))
    from collections import defaultdict
    bywhy = defaultdict(int)
    for x, y, o, n, st, why in lost:
        bywhy[(st, why, o, n)] += 1
    for k in sorted(bywhy, key=lambda k: -bywhy[k]):
        print("   %-40s %6d" % (" / ".join(str(i) for i in k), bywhy[k]))
    return lost


def report_air_gaps():
    print("=" * 90)
    print("B) 结构保护区里的可疑空洞")
    # 堡垒底壳与地宫楼板之间的 y=1121 那一行
    gap = [x for x in range(FORT_L, FORT_R + 1) if get(x, FORT_BOTTOM + 1) == 0]
    print("   y=%d（堡垒底 %d 与楼板 %d 之间）空气列数: %d  x=%s..%s"
          % (FORT_BOTTOM + 1, FORT_BOTTOM, SLAB_TOP, len(gap),
             min(gap) if gap else None, max(gap) if gap else None))
    # 竖井是否被楼板补板封住（x 524..538, y 1122..1125）
    blocked = [(x, y) for y in range(SLAB_TOP, TUN_TOP) for x in range(SHAFT_L, SHAFT_R + 1)
               if get(x, y) in (ALLOY, TRIM, 1)]
    print("   竖井 %d..%d 在 y=%d..%d 的实心格: %d  %s"
          % (SHAFT_L, SHAFT_R, SLAB_TOP, TUN_TOP - 1, len(blocked),
             blocked[:6]))


def flood(start):
    seen = {start}
    q = deque([start])
    while q:
        x, y = q.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if (nx, ny) in seen or not (0 <= nx < W and 0 <= ny < H):
                continue
            if get(nx, ny) in (0, 4, 5):
                seen.add((nx, ny))
                q.append((nx, ny))
    return seen


def report_connectivity():
    print("=" * 90)
    print("C) 连通性")
    start = (V["SpawnX"], HALL_BOTTOM - 1)
    print("   落点 (%d,%d) 状态=%s" % (start[0], start[1], get(*start)))
    # 从门厅底部往下（落点可能悬空，用门厅地板上方那一格）
    probe = (SHAFT_L + 2, HALL_BOTTOM - 2)
    if get(*probe) != 0:
        probe = (400, HALL_BOTTOM - 3)
    seen = flood(probe) if get(*probe) == 0 else set()
    print("   洪水填充起点 %s（空气=%s），可达格 %d" % (probe, get(*probe), len(seen)))
    checks = [
        ("门厅(400,%d)" % (HALL_BOTTOM - 2), (400, HALL_BOTTOM - 2)),
        ("竖井中段(%d,%d)" % (SHAFT_L + 2, 1125), (SHAFT_L + 2, 1125)),
        ("通道东段", (TUN_R - 10, (TUN_TOP + TUN_BOT) // 2)),
        ("地宫一层中央大厅", (400, 1160)),
        ("地宫平台上方", (400, FLOOR_Y - 1)),
        ("东升降井顶", (LAD_E_L + 1, UPPER_TOP + 1)),
        ("西升降井顶", (LAD_W_L + 1, UPPER_TOP + 1)),
    ]
    for label, (x, y) in checks:
        print("   %-24s (%d,%d) = %s  可达=%s" % (label, x, y, get(x, y), (x, y) in seen))
    for name, left, top, right, bottom in ROOMS:
        cx = (left + right) // 2
        print("   房间 %-16s 中心(%d,%d)=%s 可达=%s 地板(%d,%d)=%s"
              % (name, cx, (top + bottom) // 2, get(cx, (top + bottom) // 2),
                 (cx, (top + bottom) // 2) in seen, cx, bottom + 1, get(cx, bottom + 1)))


def report_structure_box():
    print("=" * 90)
    print("D) 地宫掏空与堡垒/通道的重叠（按坐标算）")
    print("   堡垒盒: x %d..%d, y %d..%d" % (FORT_L, FORT_R, FORT_TOP, FORT_BOTTOM))
    print("   通道  : x %d..%d, y %d..%d（+壳 %d）" % (TUN_L, TUN_R, TUN_TOP, TUN_BOT, TUN_SHELL))
    print("   椭球  : 中心(%d,%d) 半径 %dx%d  y %d..%d" % (CX, CY, RX, RY, TOP, BOTTOM))
    print("   内腔  : 半径 %dx%d  y %d..%d" % (IRX, IRY, CY - IRY, CY + IRY))
    # 内腔与通道带的交叠
    ov = 0
    for y in range(TUN_TOP - TUN_SHELL, TUN_BOT + TUN_SHELL + 1):
        for x in range(TUN_L - TUN_SHELL, TUN_R + TUN_SHELL + 1):
            if in_inner(x, y):
                ov += 1
    print("   内腔与通道带重叠格数: %d" % ov)
    ov2 = 0
    for y in range(FORT_TOP, FORT_BOTTOM + 1):
        for x in range(FORT_L, FORT_R + 1):
            if in_inner(x, y):
                ov2 += 1
    print("   内腔与堡垒盒重叠格数: %d（被 InFortressBox 挡掉）" % ov2)
    # 壳体是否在通道内形成幕墙
    curtain = []
    for y in range(TUN_TOP + 1, TUN_BOT - 1):
        for x in range(TUN_L, TUN_R + 1):
            if in_outer(x, y) and not in_inner(x, y):
                curtain.append((x, y))
    print("   椭球外壳落在通道内空里的格数: %d  %s" % (len(curtain), curtain[:8]))


def report_pillars_and_floor():
    print("=" * 90)
    print("E) 通道立柱 / 门厅地板 / 竖井（本批修补效果）")
    total = 0
    trim = 0
    for x in range(TUN_L + 20, TUN_R - 20 + 1, 44):
        for (a, b) in ((x, x + 1), (x + 3, x + 4)):
            for yy in range(TUN_TOP + 2, TUN_BOT - 2 + 1):
                for xx in range(a, b + 1):
                    total += 1
                    if get(xx, yy) == TRIM:
                        trim += 1
    print("   立柱应有 %d 格，实际压条 %d 格" % (total, trim))
    floor_missing = [x for x in range(HALL_L, HALL_R + 1)
                     if get(x, HALL_BOTTOM) == 0 and not (HEARTH_L <= x <= HEARTH_R)
                     and not (SHAFT_L <= x <= SHAFT_R)]
    print("   门厅地板 y=%d：缺格 %d（火塘/竖井除外）" % (HALL_BOTTOM, len(floor_missing)))
    print("   落点 (%d,%d) 上方那格 = %s（0=空气，玩家站得住）"
          % (V["SpawnX"], HALL_BOTTOM - 1, get(V["SpawnX"], HALL_BOTTOM - 1)))
    shaft = [(x, y) for y in range(SLAB_TOP, TUN_TOP + 2) for x in range(SHAFT_L, SHAFT_R + 1)
             if get(x, y) != 0]
    print("   竖井 %d..%d y=%d..%d 剩余实心格：%d" % (SHAFT_L, SHAFT_R, SLAB_TOP, TUN_TOP + 1, len(shaft)))


if __name__ == "__main__":
    run()
    lost = report_alloy_loss()
    report_air_gaps()
    report_connectivity()
    report_structure_box()
    report_pillars_and_floor()
