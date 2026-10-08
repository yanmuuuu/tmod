# -*- coding: utf-8 -*-
"""壁炉世界生成的**布局不变量**检查。

为什么需要：世界生成只在玩家**真正进入子世界**时才跑，`ws_pipeline.ps1` 的隔离加载自检
（`-server`）和客户端自检都覆盖不到它 —— 生成器里写错一个常量，要等你进门那一刻才炸，
而且是在子世界里炸（可能直接把世界卡在半成品状态）。

这个检查器把"生成前就该成立"的几何关系从源码里读出来核对：
  1. 世界尺寸与各结构都在世界范围内；
  2. 堡垒 / 高塔 / 塔顶场地 / 地下通道 的嵌套与相接关系正确；
  3. 门厅地板在门厅底面、出生点落在地板之上（进入壁炉不会卡在墙里/掉出去）；
  4. 地表基准高度：**这一批把地表抬到 840，堡垒整体埋进灰烬里**，所以原来的
     "FortTop < SurfaceBase（半埋）"改成了"屋顶在地表以下、塔身从灰烬里钻出来"；
  5. 塔顶场地确实落在"太空"高度（worldSurface × 0.35 以上）—— 场地整块上移 12 格就是为此；
  6. 塔身确实从堡垒屋顶一直通到塔顶场地（中间没有断层），层高能整除；
  7. **椭球地宫**：椭球在世界内、壳厚 4~6、平台顶面与地下通道走道面齐平、
     每一间房都落在椭球内空里、都不压堡垒投影与通道走廊带、房间互不重叠、
     升降井真的落在它要服务的房间里（不然"连通"只是嘴上说说）。
"""
import io
import os
import re
import sys

LAYOUT = r"E:\开发\WastelandSoul\Content\Subworlds\FireplaceLayout.cs"


def constants(path):
    """读出 `public const int X = 表达式;`（表达式里可以引用先前解析出来的常量）。

    多趟解析：常量在文件里的顺序不必和依赖顺序一致（例如 FortShaftLeft 引用了
    后面才定义的 TunnelLeft），一趟扫不出来就再扫一趟，直到没有新值为止。
    """
    text = io.open(path, encoding="utf-8-sig").read()
    declarations = []

    for match in re.finditer(r"public\s+const\s+int\s+(\w+)\s*=\s*([^;]+);", text):
        declarations.append((match.group(1), match.group(2).strip()))

    values = {}

    for _ in range(len(declarations) + 1):
        progressed = False

        for name, expression in declarations:
            if name in values:
                continue

            resolved = expression

            for known, value in values.items():
                resolved = re.sub(r"\b%s\b" % known, str(value), resolved)

            try:
                values[name] = int(eval(resolved, {"__builtins__": {}}, {}))     # noqa: S307
                progressed = True
            except Exception:
                pass

        if not progressed:
            break

    return values


def rooms(path):
    """读出地宫的房间清单（`new VaultRoom("名字", l, t, r, b)`）。"""
    text = io.open(path, encoding="utf-8-sig").read()
    pattern = re.compile(
        r'new\s+VaultRoom\(\s*"([^"]+)"\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*\)')

    return [(m.group(1), int(m.group(2)), int(m.group(3)), int(m.group(4)), int(m.group(5)))
            for m in pattern.finditer(text)]


def main():
    if not os.path.exists(LAYOUT):
        print("!! 找不到 %s" % LAYOUT)
        return 1

    v = constants(LAYOUT)
    vault_rooms = rooms(LAYOUT)
    problems = []

    def need(condition, message):
        if not condition:
            problems.append(message)

    w = v["Width"]
    h = v["Height"]

    # 1) 都在世界里
    for name in ("FortLeft", "FortRight", "FortTop", "FortBottom",
                 "HallLeft", "HallRight", "HallTop", "HallBottom",
                 "TowerLeft", "TowerRight", "TowerTop", "TowerBottom",
                 "ArenaLeft", "ArenaRight", "ArenaTop", "ArenaBottom",
                 "TunnelLeft", "TunnelRight", "TunnelTop", "TunnelBottom",
                 "SpawnX", "SpawnY"):
        value = v.get(name)

        if value is None:
            problems.append("常量 %s 读不出来" % name)
            continue

        if name.endswith(("Left", "Right", "X")) and not (0 <= value < w):
            problems.append("%s=%d 超出世界宽度 0..%d" % (name, value, w - 1))

        if name.endswith(("Top", "Bottom", "Y")) and not (0 <= value < h):
            problems.append("%s=%d 超出世界高度 0..%d" % (name, value, h - 1))

    # 2) 嵌套 / 相接
    need(v["FortLeft"] < v["HallLeft"] < v["HallRight"] < v["FortRight"], "门厅没有落在堡垒内部")
    need(v["FortTop"] < v["HallTop"] < v["HallBottom"] < v["FortBottom"], "门厅的上下边界不在堡垒内部")
    need(v["FortTop"] <= v["TowerBottom"] <= v["FortTop"] + 4, "塔基没有坐在堡垒屋顶上（TowerBottom 应当等于 FortTop）")
    need(v["TowerTop"] < v["TowerBottom"], "塔身高度为负")
    need(v["TowerLeft"] > v["FortLeft"] and v["TowerRight"] < v["FortRight"], "塔身比堡垒还宽，塔基落不到屋顶上")
    need(v["ArenaTop"] < v["ArenaBottom"] and v["ArenaBottom"] <= v["TowerTop"] + 4, "塔顶场地没有接在塔身上端")
    need(v["ArenaLeft"] < v["TowerLeft"] and v["ArenaRight"] > v["TowerRight"], "塔顶场地没有比塔身宽（爬上去会撞墙）")
    need(v["TunnelTop"] > v["HallBottom"], "地下通道跑到门厅上面去了")
    need(v["TunnelLeft"] >= v["HallLeft"], "地下通道与门厅没有可连通的竖井")
    need(v["TunnelTop"] < v["TunnelBottom"], "通道高度为负")
    need(v["TunnelRight"] > v["TunnelLeft"] + 200, "通道太短，达不到世界边缘")

    # 3) 出生点：站在门厅地板上
    need(v["SpawnX"] > v["HallLeft"] and v["SpawnX"] < v["HallRight"], "出生点 x 不在门厅里")
    need(v["SpawnY"] == v["HallBottom"], "出生点 y 应当就是门厅地板那一格（玩家会站在它上面）")

    # 4) 地表高度（本批改版）：灰烬地表抬到 840，
    #    堡垒屋顶 860 落在**地表以下**（整体埋进灰烬），祈塔从灰烬里钻出来。
    surface = v["SurfaceBase"]
    need(v["FortTop"] > v["SurfaceBase"],
         "堡垒屋顶没有埋在灰烬地表以下（本批要求地表抬到屋顶之上：FortTop=860 > SurfaceBase=840）")
    need(v["TowerTop"] < surface < v["TowerBottom"],
         "祈塔没有从灰烬地表里钻出来（应当 TowerTop < SurfaceBase < TowerBottom）")
    need(surface < v["HallTop"], "门厅跑到灰烬地表上面了（门厅应当是地下的）")
    need(v["RockTop"] == surface + v["CrustDepth"], "岩石层高度与地表+壳厚不一致")

    # 5) 塔顶场地在太空（原版太空阈值 = worldSurface × 0.35）
    space_line = surface * 0.35
    need(v["ArenaBottom"] < space_line,
         "塔顶场地整体不在太空高度（ArenaBottom=%d 应小于 %.1f）" % (v["ArenaBottom"], space_line))

    # 6) 塔身层高能整除，楼板才不会在塔顶/塔基留半层
    if v["TowerFloorHeight"] > 0:
        span = v["TowerBottom"] - v["TowerTop"]
        need(span % v["TowerFloorHeight"] == 0, "塔身高度 %d 不能被层高 %d 整除" % (span, v["TowerFloorHeight"]))
        need(span // v["TowerFloorHeight"] >= 8, "塔楼层数少于 8 层，'尽量做大'没做到")

    # 7) 椭球地宫
    cx = v["VaultCenterX"]
    cy = v["VaultCenterY"]
    rx = v["VaultRadiusX"]
    ry = v["VaultRadiusY"]
    shell = v["VaultShell"]

    need(4 <= shell <= 6, "地宫隔绝墙厚 %d 不在玩家要求的 4~6 格" % shell)
    need(12 <= v["VaultTrimStep"] <= 16, "地宫压条行距 %d 不在玩家要求的 12~16 行" % v["VaultTrimStep"])

    # 椭球要落在世界里，四周留出世界边缘的余量（生成时还要靠 WorldPaint.InWorld 兜底）
    margin = 8
    need(v["VaultLeft"] >= margin, "椭球左边 %d 贴到世界边缘了" % v["VaultLeft"])
    need(v["VaultRight"] <= w - 1 - margin, "椭球右边 %d 贴到世界边缘了" % v["VaultRight"])
    need(v["VaultTop"] >= margin, "椭球上边 %d 贴到世界顶了" % v["VaultTop"])
    need(v["VaultBottom"] <= h - 1 - margin, "椭球下边 %d 贴到世界底了" % v["VaultBottom"])
    need(v["VaultTop"] > v["RockTop"], "椭球上缘跑到灰烬层里去了（应当整块在岩石层）")

    # 平台顶面必须和地下通道的走道面同高：从竖井下来一路是平的
    need(v["VaultFloorY"] == v["TunnelBottom"] - 1,
         "地宫平台顶面 %d 没有对齐通道走道面 %d" % (v["VaultFloorY"], v["TunnelBottom"] - 1))
    need(v["VaultSlabTop"] > v["VaultUpperBottom"], "二层楼板压到了二层房间的内空")
    need(v["VaultStoryTop"] > v["VaultSlabBottom"], "一层房间的内空顶行没有落在一层天花板以下")
    need(v["VaultFloorY"] > v["VaultStoryTop"], "一层房间的内空上下界反了")

    # 堡垒竖井必须落在通道走廊带里（否则挖下来砸在通道顶壳上，下不去）
    lane_left = v["TunnelLeft"] - v["TunnelShell"]
    need(v["FortShaftLeft"] > lane_left and v["FortShaftRight"] < v["VaultRight"],
         "堡垒竖井没有落在通道走廊带的横向范围里")
    need(v["FortLeft"] < v["FortShaftLeft"] and v["FortShaftRight"] < v["FortRight"],
         "堡垒竖井不在堡垒的横向范围里")
    need(v["FortShaftRight"] > v["FortShaftLeft"], "堡垒竖井宽度为负")

    # 房间
    need(6 <= len(vault_rooms) <= 10, "地宫房间数 %d 不在玩家要求的 6~10 间" % len(vault_rooms))

    inner_rx = rx - shell
    inner_ry = ry - shell

    def inside_ellipse(x, y, tolerance=1e-6):
        dx = (x - cx) / float(inner_rx)
        dy = (y - cy) / float(inner_ry)
        return dx * dx + dy * dy <= 1.0 + tolerance

    def in_fortress_box(x, y):
        return (v["FortLeft"] - 1 <= x <= v["FortRight"] + 1) and y <= v["FortBottom"] + 1

    def in_tunnel_lane(x, y):
        return (x >= lane_left) and (v["TunnelTop"] - v["TunnelShell"] <= y <= v["TunnelBottom"] + v["TunnelShell"])

    for name, left, top, right, bottom in vault_rooms:
        need(right > left and bottom > top, "房间「%s」的矩形反了" % name)
        need(right - left + 1 >= 40, "房间「%s」太窄（%d 格），家具放不下" % (name, right - left + 1))
        need(bottom - top + 1 >= 20, "房间「%s」太矮（%d 格）" % (name, bottom - top + 1))

        for x, y in ((left, top), (right, top), (left, bottom), (right, bottom)):
            need(inside_ellipse(x, y),
                 "房间「%s」的角 (%d, %d) 跑到椭球内空外面了" % (name, x, y))

        # 房间不许压到堡垒投影（否则会把门厅挖穿）与通道走廊带（否则会把通道堵死）
        for x in (left, right):
            for y in (top, bottom):
                need(not in_fortress_box(x, y), "房间「%s」压到堡垒投影了：(%d, %d)" % (name, x, y))
                need(not in_tunnel_lane(x, y), "房间「%s」压到地下通道走廊带了：(%d, %d)" % (name, x, y))

        need(left >= 0 and right < w and top >= 0 and bottom < h,
             "房间「%s」超出世界范围" % name)

    for i in range(len(vault_rooms)):
        for j in range(i + 1, len(vault_rooms)):
            a = vault_rooms[i]
            b = vault_rooms[j]
            overlap = (a[1] <= b[3] and b[1] <= a[3] and a[2] <= b[4] and b[2] <= a[4])

            need(not overlap, "房间「%s」与「%s」重叠了" % (a[0], b[0]))

    # 7.5) 隔墙厚度：同一层里**挨着**的两间房之间的缝就是隔墙，本批要求 4~6 格
    #      （缝 > 20 格的是阁楼 / 主通道那种大空档，不是隔墙，跳过）。
    for band_top in sorted({r[2] for r in vault_rooms}):
        band = sorted([r for r in vault_rooms if r[2] == band_top], key=lambda r: r[1])

        for a, b in zip(band, band[1:]):
            gap = b[1] - a[3] - 1

            if gap <= 20:
                need(4 <= gap <= 6,
                     "「%s」与「%s」之间隔墙只有 %d 格（本批要求 4~6 格实体隔墙）"
                     % (a[0], b[0], gap))

    # 一层最后一间房与主通道之间那道隔墙（FireplaceVault 里 wallE~wallF）
    story = sorted([r for r in vault_rooms if r[2] == v["VaultStoryTop"]], key=lambda r: r[1])

    if story:
        gap = lane_left - story[-1][3] - 1
        need(4 <= gap <= 6,
             "一层「%s」与主通道之间隔墙 %d 格（本批要求 4~6 格）" % (story[-1][0], gap))

    # 升降井要真的落在它服务的房间里（西井 = 一层第一间；东井 = 二层右翼靠东那间）
    west_ladder = (v["VaultWestLadderX"], v["VaultWestLadderRight"])
    east_ladder = (v["VaultEastLadderX"], v["VaultEastLadderRight"])
    story_room = min((r for r in vault_rooms if r[2] == v["VaultStoryTop"]), key=lambda r: r[1], default=None)
    upper_rooms = [r for r in vault_rooms if r[2] == v["VaultUpperTop"]]

    if story_room is None:
        problems.append("读不到一层房间（Top 应当等于 VaultStoryTop）")
    else:
        need(story_room[1] <= west_ladder[0] and west_ladder[1] <= story_room[3],
             "西升降井没落在一层「%s」里" % story_room[0])

    if len(upper_rooms) >= 2:
        east_room = max(upper_rooms, key=lambda r: r[3])
        need(east_room[1] <= east_ladder[0] and east_ladder[1] <= east_room[3],
             "东升降井没落在二层「%s」里" % east_room[0])
        need(v["VaultEastLadderX"] > lane_left,
             "东升降井没落在通道走廊带上（爬不上去）")
    else:
        problems.append("二层房间少于 2 间，两翼没有做出来")

    if problems:
        print("!! 壁炉布局有 %d 处问题：" % len(problems))
        for item in problems:
            print("   -", item)
        print("CHECK FAILED")
        return 1

    print("[OK] 壁炉布局不变量成立：%dx%d，塔 %d 层 / 高 %d 格，场地 y=%d..%d（太空线 %.0f），出生点 (%d, %d)"
          % (v["Width"], v["Height"], (v["TowerBottom"] - v["TowerTop"]) // v["TowerFloorHeight"],
             v["TowerBottom"] - v["TowerTop"], v["ArenaTop"], v["ArenaBottom"], space_line,
             v["SpawnX"], v["SpawnY"]))
    print("     地宫椭球 %dx%d @ (%d, %d)，壳厚 %d，房间 %d 间，平台顶面 y=%d（对齐通道走道面）"
          % (rx * 2, ry * 2, cx, cy, shell, len(vault_rooms), v["VaultFloorY"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
