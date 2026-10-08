# -*- coding: utf-8 -*-
"""为《废土魂穿》程序化生成 InnoVault `Models3D` 能直接吃的 **OBJ + MTL + PNG 贴图**。

为什么要有这个脚本
------------------
玩家的 3D 需求（护甲穿身视觉、智械人人物模型）原先写成 P5「需要 Blender」。
实测结论是 **不需要 Blender**：InnoVault 的 `ObjModelLoader` 只要求一个普通
Wavefront OBJ + 同目录同名 MTL + 一张独立 PNG。方块拼的低模正好用几行 Python
就能写出来，而且这类"机械体 / 硬表面"模型本来就是方块堆出来的最好看。

产物布局（`<输出目录>` 默认 `E:\\开发\\WastelandSoul\\Assets\\Models`）
--------------------------------------------------------------------
    <Name>.obj    几何 + UV + 法线（Y 朝上；InnoVault 导入时自动翻成 Y 朝下）
    <Name>.mtl    每个部件一个材质，**故意不写 map_Kd**
    <Name>.png    UV 图集贴图

⚠️ **为什么 MTL 里故意不写 `map_Kd`**
    `ObjModelLoader.TryLoadTexture()` 用的是 `AssetRequestMode.ImmediateLoad`，
    也就是**在加载期（模组 PostSetupContent，线程池工作线程）就会把贴图推上 GPU**。
    本工程踩过 `ThreadStateException: most FNA3D audio/graphics functions must be
    called on the main thread → Disabling Mod` 的事故，所以规矩是：**加载期只碰
    纯数据（OBJ/MTL 文本），贴图由代码在主线程绘制路径里懒加载**。
    生成的贴图会放在 OBJ 同目录同主名，代码里用
    `ModContent.Request<Texture2D>("<路径去扩展名>")` 一次赋值给
    `Vault3DModel.Materials[...].DiffuseTexture` 即可（见
    `Common/Models/Wasteland3DPreview.cs` 的 `ApplyTextures`）。

坐标 / UV 约定
--------------
* OBJ 里 **Y 朝上**（脚本按这个写）。InnoVault 默认 `ObjAxisConvention.YUpToYDown`，
  导入时做 `(x, -y, z)`，美术**不要自己翻**。
* UV 原点：OBJ 里 v 向上；`ObjImportOptions.FlipTextureV` 默认 true，会写成 `1 - v`，
  正好对上 XNA/MonoGame 的左上原点。所以脚本按「v=1 是贴图**上**边」来写 UV。

用法
----
    python gen_3d_models.py                       # 生成全部（探针盒子 + 护甲 + 智械人）
    python gen_3d_models.py --out <目录>           # 换输出目录
    python gen_3d_models.py --only humanoid        # probe | armor | humanoid | all
    python gen_3d_models.py --list                 # 只打印部件表，不落盘
"""
import argparse
import math
import os
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:  # pragma: no cover
    Image = None

DEFAULT_OUT = r"E:\开发\WastelandSoul\Assets\Models"

# UV 图集：CELLS x CELLS 个格子，每个格子再切成 FACE_COLS x FACE_ROWS 给 6 个面用
CELLS = 4
FACE_COLS = 2
FACE_ROWS = 3
CELL_PX = 64                      # 每个格子的像素边长 -> 贴图 4*64 = 256


# ======================================================================================
# 1. 几何原语
# ======================================================================================
class Part(object):
    """一个轴对齐长方体：中心 (cx,cy,cz)，半尺寸 (sx,sy,sz)，占一个 UV 格子。

    `joint` 只用于**分组注释**（OBJ 的 `g` 行）和导出清单；InnoVault 的 OBJ 路径
    不做骨骼蒙皮，分组信息是给以后手工绑骨/拆分用的。
    """

    __slots__ = ("name", "center", "half", "material", "cell", "joint")

    def __init__(self, name, center, half, material, cell, joint=""):
        self.name = name
        self.center = tuple(float(v) for v in center)
        self.half = tuple(float(v) for v in half)
        self.material = material
        self.cell = cell
        self.joint = joint

    @property
    def triangle_count(self):
        return 12

    def bounds(self):
        cx, cy, cz = self.center
        sx, sy, sz = self.half
        return (cx - sx, cy - sy, cz - sz), (cx + sx, cy + sy, cz + sz)


# 六个面：(法线, 切线, 副切线)；切线 x 副切线 = 法线，保证从外面看是逆时针（外法线朝外）
# 这是硬表面模型不出「黑面 / 反面」的关键，别随手改顺序。
_FACES = (
    ((0, 0, 1), (1, 0, 0), (0, 1, 0)),    # +Z 前
    ((0, 0, -1), (-1, 0, 0), (0, 1, 0)),  # -Z 后
    ((1, 0, 0), (0, 0, -1), (0, 1, 0)),   # +X 右
    ((-1, 0, 0), (0, 0, 1), (0, 1, 0)),   # -X 左
    ((0, 1, 0), (0, 0, 1), (1, 0, 0)),    # +Y 上
    ((0, -1, 0), (0, 0, -1), (1, 0, 0)),  # -Y 下
)

# 每个面在「格子内部」的 UV 子矩形（列, 行），行 0 = 贴图上边
_FACE_SLOT = ((0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2))


def _cell_uv(cell):
    """把格子号 (col,row) 换算成 UV 区间。row 0 在**贴图上方** -> v 大。"""
    col, row = cell
    u0 = col / float(CELLS)
    v1 = 1.0 - row / float(CELLS)
    return u0, v1


def _face_uv(cell, face_index, corner):
    """corner: 0=(0,0) 1=(1,0) 2=(1,1) 3=(0,1)，在面的局部参数空间里。"""
    u0, v1 = _cell_uv(cell)
    du = (1.0 / CELLS) / FACE_COLS
    dv = (1.0 / CELLS) / FACE_ROWS
    col, row = _FACE_SLOT[face_index]
    fu0 = u0 + col * du
    fv1 = v1 - row * dv           # 该子格的上边
    lu, lv = ((0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0))[corner]
    # v 向下走（贴图行号增大方向），所以是 fv1 - lv*dv
    return fu0 + lu * du, fv1 - lv * dv


class ObjBuilder(object):
    """攒顶点/法线/UV，最后吐出一份 OBJ 文本。"""

    def __init__(self, name):
        self.name = name
        self._pos = []
        self._uv = []
        self._nrm = []
        self._faces = []          # (material, group, [(pi,ti,ni), x3])
        self._groups = []         # 保持出现顺序
        self._dedup = {}

    # ---- 内部：三个池子各自去重 ----
    def _p(self, v):
        key = ("v",) + v
        if key not in self._dedup:
            self._pos.append(v)
            self._dedup[key] = len(self._pos)
        return self._dedup[key]

    def _t(self, v):
        key = ("t",) + v
        if key not in self._dedup:
            self._uv.append(v)
            self._dedup[key] = len(self._uv)
        return self._dedup[key]

    def _n(self, v):
        key = ("n",) + v
        if key not in self._dedup:
            self._nrm.append(v)
            self._dedup[key] = len(self._nrm)
        return self._dedup[key]

    def add_box(self, part):
        if part.joint and part.joint not in self._groups:
            self._groups.append(part.joint)
        c = part.center
        s = part.half

        for fi, (normal, tangent, bitangent) in enumerate(_FACES):
            corners = []
            for ci, (a, b) in enumerate(((-1, -1), (1, -1), (1, 1), (-1, 1))):
                p = tuple(c[k] + s[k] * (normal[k] + tangent[k] * a + bitangent[k] * b)
                          for k in range(3))
                u, v = _face_uv(part.cell, fi, ci)
                corners.append((self._p(p), self._t((u, v)), self._n(normal)))
            # 两个三角形：0-1-2 / 0-2-3
            self._faces.append((part.material, part.joint, [corners[0], corners[1], corners[2]]))
            self._faces.append((part.material, part.joint, [corners[0], corners[2], corners[3]]))

    # ---- 输出 ----
    def triangle_count(self):
        return len(self._faces)

    def vertex_count(self):
        """OBJ 里 `v` 的行数（以及去重后的顶点位置数）——和 InnoVault 报告的
        `Vault3DModel.VertexCount` 会不同：后者是 (pos,uv,normal) 三元组去重后的数。"""
        return len(self._pos)

    def unique_vertex_count(self):
        triples = set()
        for _, _, tri in self._faces:
            for t in tri:
                triples.add(t)
        return len(triples)

    def bounds(self):
        if not self._pos:
            return (0, 0, 0), (0, 0, 0)
        lo = [min(p[k] for p in self._pos) for k in range(3)]
        hi = [max(p[k] for p in self._pos) for k in range(3)]
        return tuple(lo), tuple(hi)

    def to_obj(self, mtl_name, parts):
        out = []
        out.append("# 由 tools/gen_3d_models.py 程序化生成，请勿手改")
        out.append("# Generated by tools/gen_3d_models.py - do not edit by hand")
        out.append("# 顶点 Y 朝上；InnoVault 导入时自动翻成 Y 朝下（ObjAxisConvention.YUpToYDown）")
        out.append("# Vertices are Y-up; InnoVault flips to Y-down on import.")
        out.append("mtllib %s" % mtl_name)
        out.append("")

        out.append("# ---- %d 个顶点 / %d positions ----" % (len(self._pos), len(self._pos)))
        for p in self._pos:
            out.append("v %.6f %.6f %.6f" % p)
        out.append("")
        for t in self._uv:
            out.append("vt %.6f %.6f" % t)
        out.append("")
        for n in self._nrm:
            out.append("vn %.6f %.6f %.6f" % n)
        out.append("")

        current_mtl = None
        current_group = None
        for material, group, tri in self._faces:
            if group != current_group:
                out.append("g %s" % (group or "default"))
                current_group = group
            if material != current_mtl:
                out.append("usemtl %s" % material)
                current_mtl = material
            out.append("f %s" % " ".join("%d/%d/%d" % t for t in tri))
        out.append("")
        return "\n".join(out)

    def to_manifest(self, parts, name):
        lo, hi = self.bounds()
        lines = []
        lines.append("模型 %s：%d 个长方体部件 / %d 三角形 / %d 个 OBJ v / %d 个去重顶点"
                     % (name, len(parts), self.triangle_count(),
                        self.vertex_count(), self.unique_vertex_count()))
        lines.append("  包围盒 X[%.3f, %.3f] Y[%.3f, %.3f] Z[%.3f, %.3f]"
                     % (lo[0], hi[0], lo[1], hi[1], lo[2], hi[2]))
        for p in parts:
            lines.append("  %-16s 关节=%-10s 材质=%-16s cell=%s 中心=(%.3f, %.3f, %.3f) 半尺寸=(%.3f, %.3f, %.3f)"
                         % (p.name, p.joint or "-", p.material, p.cell,
                            p.center[0], p.center[1], p.center[2],
                            p.half[0], p.half[1], p.half[2]))
        return "\n".join(lines)


def to_mtl(materials):
    """materials: [(name, (r,g,b)), ...]

    **故意不写 map_Kd** —— 见模块开头的说明。贴图由主线程懒加载后直接赋给
    `Vault3DModel.Materials[name].DiffuseTexture`。
    """
    out = []
    out.append("# 由 tools/gen_3d_models.py 程序化生成")
    out.append("# 故意不含 map_Kd：加载期创建 GPU 贴图会让客户端在")
    out.append("# PostSetupContent（工作线程）触发 ThreadStateException。")
    out.append("# Deliberately no map_Kd: creating GPU textures at load time would")
    out.append("# throw ThreadStateException on the worker thread.")
    out.append("")
    for name, (r, g, b) in materials:
        out.append("newmtl %s" % name)
        out.append("Kd %.6f %.6f %.6f" % (r, g, b))
        out.append("Ka 0.000000 0.000000 0.000000")
        out.append("Ks 0.000000 0.000000 0.000000")
        out.append("d 1.000000")
        out.append("illum 1")
        out.append("")
    return "\n".join(out)


# ======================================================================================
# 2. 程序化贴图
# ======================================================================================
def _hash01(x, y, seed=0):
    h = (x * 374761393 + y * 668265263 + seed * 2246822519) & 0xFFFFFFFF
    h = (h ^ (h >> 13)) * 1274126177 & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0


def _shade(color, k):
    return tuple(max(0, min(255, int(c * k))) for c in color)


def _to255(color):
    """Kd 是 0~1 的浮点，贴图要用 0~255。"""
    return tuple(max(0, min(255, int(round(c * 255.0)))) for c in color)


def build_texture(materials, cells_used, size=None, seed=7):
    """给每个 (材质, 格子) 组合画一块 64x64 面板，再拼进 4x4 图集。

    细节都刻意做得很小很密：模型在游戏里只有几十像素高，太细的东西看不见，
    而"面板线 + 铆钉 + 一道高光"这种大结构缩下去仍然读得出来。
    """
    if Image is None:
        raise RuntimeError("需要 Pillow（python -c 'import PIL'）")

    step = CELL_PX
    total = step * CELLS
    image = Image.new("RGBA", (total, total), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    palette = dict(materials)

    for (material, cell) in cells_used:
        col, row = cell
        ox, oy = col * step, row * step
        base = _to255(palette.get(material, (0.68, 0.71, 0.76)))

        # 底色 + 轻微噪点（避免大面积纯色在 BasicEffect 下显得像塑料）
        for y in range(step):
            for x in range(step):
                n = (_hash01(ox + x, oy + y, seed) - 0.5) * 0.10
                image.putpixel((ox + x, oy + y), _shade(base, 1.0 + n) + (255,))

        # 面板分割线：把格子切成 2x3，正好对上六个面的 UV 槽位
        cell_w = step // FACE_COLS
        cell_h = step // FACE_ROWS
        line = _shade(base, 0.45)
        bright = _shade(base, 1.45)

        for fx in range(FACE_COLS):
            for fy in range(FACE_ROWS):
                x0, y0 = ox + fx * cell_w, oy + fy * cell_h
                x1, y1 = x0 + cell_w - 1, y0 + cell_h - 1
                # 每个面槽位一格：上/左 一道亮高光，下/右 一道暗线 -> 有倒角感
                draw.line([(x0, y0), (x1, y0)], fill=bright + (255,))
                draw.line([(x0, y0), (x0, y1)], fill=bright + (255,))
                draw.line([(x0, y1), (x1, y1)], fill=line + (255,))
                draw.line([(x1, y0), (x1, y1)], fill=line + (255,))

                # 铆钉：四个角各一颗（只在偶数槽位画，让面与面之间有疏密差别）
                if (fx + fy) % 2 == 0:
                    for (rx, ry) in ((3, 3), (cell_w - 5, 3), (3, cell_h - 5), (cell_w - 5, cell_h - 5)):
                        px, py = x0 + rx, y0 + ry
                        draw.rectangle([px, py, px + 1, py + 1], fill=_shade(base, 1.7) + (255,))
                        draw.point((px + 2, py + 2), fill=_shade(base, 0.5) + (255,))
                else:
                    # 散热格栅 / 走线
                    for gx in range(x0 + 5, x1 - 3, 4):
                        draw.line([(gx, y0 + 6), (gx, y1 - 6)], fill=line + (255,))

    return image


# ======================================================================================
# 3. 三个模型
# ======================================================================================
MAT_METAL = "WL_Steel"
MAT_DARK = "WL_DarkSteel"
MAT_ACCENT = "WL_Accent"
MAT_GLOW = "WL_Glow"
MAT_TRIM = "WL_GoldTrim"
MAT_CLOTH = "WL_Robe"

MATERIALS = [
    (MAT_METAL, (0.68, 0.71, 0.76)),
    (MAT_DARK, (0.32, 0.34, 0.39)),
    (MAT_ACCENT, (0.83, 0.45, 0.20)),
    (MAT_GLOW, (0.40, 0.86, 0.92)),
    (MAT_TRIM, (0.85, 0.68, 0.28)),
    (MAT_CLOTH, (0.52, 0.26, 0.20)),
]


def model_probe():
    """最小验证用：一个"机械头 + 躯干 + 两条臂"的小机器人。

    刻意做小（约 1.6 单位高）——它是拿来验证整条 3D 管线通不通的探针，
    不是正式资产。放进游戏里显示在玩家头顶，玩家能肉眼确认"真的画出来了"。
    """
    parts = [
        Part("head", (0.0, 1.34, 0.0), (0.20, 0.16, 0.18), MAT_METAL, (0, 0), "head"),
        Part("visor", (0.0, 1.34, 0.19), (0.15, 0.06, 0.02), MAT_GLOW, (1, 0), "head"),
        Part("neck", (0.0, 1.15, 0.0), (0.06, 0.04, 0.06), MAT_DARK, (2, 0), "head"),
        Part("torso", (0.0, 0.88, 0.0), (0.24, 0.24, 0.15), MAT_METAL, (0, 1), "torso"),
        Part("core", (0.0, 0.88, 0.16), (0.07, 0.07, 0.02), MAT_GLOW, (1, 1), "torso"),
        Part("backpack", (0.0, 0.92, -0.19), (0.17, 0.18, 0.06), MAT_ACCENT, (2, 1), "torso"),
        Part("hip", (0.0, 0.62, 0.0), (0.19, 0.06, 0.13), MAT_DARK, (3, 1), "torso"),
        Part("arm_l", (-0.32, 0.86, 0.0), (0.07, 0.22, 0.09), MAT_DARK, (0, 2), "arm_l"),
        Part("arm_r", (0.32, 0.86, 0.0), (0.07, 0.22, 0.09), MAT_DARK, (0, 2), "arm_r"),
        Part("hand_l", (-0.32, 0.60, 0.0), (0.08, 0.06, 0.10), MAT_METAL, (1, 2), "arm_l"),
        Part("hand_r", (0.32, 0.60, 0.0), (0.08, 0.06, 0.10), MAT_METAL, (1, 2), "arm_r"),
    ]
    return "Wasteland3DProbe", parts


def model_armor():
    """精钢套装三件：头盔 / 胸甲 / 护腿。每个部位单独一个 OBJ，便于以后分别挂到
    `EquipTexture` 或玩家身上的不同位置。中心都放在原点附近，方便代码里直接定位。"""
    helmet = [
        Part("helm_dome", (0.0, 0.10, 0.0), (0.20, 0.14, 0.20), MAT_METAL, (0, 0), "head"),
        Part("helm_brow", (0.0, 0.02, 0.20), (0.18, 0.04, 0.03), MAT_DARK, (1, 0), "head"),
        Part("helm_visor", (0.0, -0.06, 0.19), (0.14, 0.05, 0.02), MAT_GLOW, (2, 0), "head"),
        Part("helm_crest", (0.0, 0.26, -0.02), (0.05, 0.06, 0.16), MAT_ACCENT, (3, 0), "head"),
        Part("helm_vent_l", (-0.19, -0.04, 0.04), (0.03, 0.07, 0.09), MAT_DARK, (0, 1), "head"),
        Part("helm_vent_r", (0.19, -0.04, 0.04), (0.03, 0.07, 0.09), MAT_DARK, (0, 1), "head"),
    ]
    chest = [
        Part("chest_plate", (0.0, 0.06, 0.0), (0.26, 0.26, 0.16), MAT_METAL, (0, 0), "torso"),
        Part("chest_core", (0.0, 0.10, 0.17), (0.08, 0.08, 0.02), MAT_GLOW, (1, 0), "torso"),
        Part("chest_collar", (0.0, 0.33, 0.0), (0.22, 0.04, 0.14), MAT_DARK, (2, 0), "torso"),
        Part("chest_pack", (0.0, 0.06, -0.20), (0.18, 0.20, 0.06), MAT_ACCENT, (3, 0), "torso"),
        Part("chest_rib_l", (-0.25, -0.04, 0.0), (0.06, 0.16, 0.14), MAT_DARK, (0, 1), "torso"),
        Part("chest_rib_r", (0.25, -0.04, 0.0), (0.06, 0.16, 0.14), MAT_DARK, (0, 1), "torso"),
        Part("chest_belt", (0.0, -0.24, 0.0), (0.24, 0.05, 0.15), MAT_DARK, (1, 1), "torso"),
    ]
    legs = [
        Part("leg_hip", (0.0, 0.24, 0.0), (0.19, 0.06, 0.14), MAT_DARK, (0, 0), "hip"),
        Part("leg_l_thigh", (-0.10, 0.06, 0.0), (0.09, 0.16, 0.11), MAT_METAL, (1, 0), "leg_l"),
        Part("leg_l_shin", (-0.10, -0.22, 0.0), (0.08, 0.16, 0.10), MAT_METAL, (2, 0), "leg_l"),
        Part("leg_l_boot", (-0.10, -0.42, 0.01), (0.09, 0.05, 0.13), MAT_ACCENT, (3, 0), "leg_l"),
        Part("leg_r_thigh", (0.10, 0.06, 0.0), (0.09, 0.16, 0.11), MAT_METAL, (1, 0), "leg_r"),
        Part("leg_r_shin", (0.10, -0.22, 0.0), (0.08, 0.16, 0.10), MAT_METAL, (2, 0), "leg_r"),
        Part("leg_r_boot", (0.10, -0.42, 0.01), (0.09, 0.05, 0.13), MAT_ACCENT, (3, 0), "leg_r"),
    ]
    return [("SalvagedSteelHelmet", helmet),
            ("SalvagedSteelChestplate", chest),
            ("SalvagedSteelGreaves", legs)]


def model_humanoid():
    """智械人低模人形（约 1.75 单位高）。

    关节分组：`head` / `torso` / `arm_l` / `arm_r` / `leg_l` / `leg_r`。
    InnoVault 的 OBJ 路径**不做蒙皮**，这些 `g` 组是留给"以后要拆成部件单独旋转，
    或者搬去 glTF 绑骨"时用的定位信息。分组原点就是各关节的旋转中心。
    """
    parts = [
        # 头
        Part("head", (0.0, 1.58, 0.0), (0.17, 0.16, 0.17), MAT_METAL, (0, 0), "head"),
        Part("hair", (0.0, 1.70, -0.05), (0.18, 0.10, 0.16), MAT_TRIM, (1, 0), "head"),
        Part("ear_l", (-0.19, 1.60, -0.02), (0.02, 0.08, 0.04), MAT_TRIM, (2, 0), "head"),
        Part("ear_r", (0.19, 1.60, -0.02), (0.02, 0.08, 0.04), MAT_TRIM, (2, 0), "head"),
        Part("visor", (0.0, 1.56, 0.16), (0.13, 0.04, 0.03), MAT_GLOW, (3, 0), "head"),
        Part("neck", (0.0, 1.40, 0.0), (0.05, 0.04, 0.05), MAT_DARK, (0, 1), "head"),
        # 躯干
        Part("chest", (0.0, 1.18, 0.0), (0.20, 0.20, 0.11), MAT_CLOTH, (1, 1), "torso"),
        Part("shoulder_plate", (0.0, 1.34, 0.0), (0.24, 0.05, 0.12), MAT_METAL, (2, 1), "torso"),
        Part("core", (0.0, 1.18, 0.12), (0.06, 0.06, 0.02), MAT_GLOW, (3, 1), "torso"),
        Part("waist", (0.0, 0.96, 0.0), (0.15, 0.06, 0.09), MAT_TRIM, (0, 2), "torso"),
        Part("robe", (0.0, 0.80, 0.0), (0.21, 0.18, 0.13), MAT_CLOTH, (1, 2), "torso"),
        # 手臂（肩 = 关节原点）
        Part("arm_l_up", (-0.27, 1.16, 0.0), (0.06, 0.16, 0.06), MAT_CLOTH, (2, 2), "arm_l"),
        Part("arm_l_low", (-0.27, 0.90, 0.0), (0.05, 0.14, 0.05), MAT_CLOTH, (3, 2), "arm_l"),
        Part("hand_l", (-0.27, 0.74, 0.0), (0.05, 0.05, 0.05), MAT_METAL, (0, 3), "arm_l"),
        Part("arm_r_up", (0.27, 1.16, 0.0), (0.06, 0.16, 0.06), MAT_CLOTH, (2, 2), "arm_r"),
        Part("arm_r_low", (0.27, 0.90, 0.0), (0.05, 0.14, 0.05), MAT_CLOTH, (3, 2), "arm_r"),
        Part("hand_r", (0.27, 0.74, 0.0), (0.05, 0.05, 0.05), MAT_METAL, (0, 3), "arm_r"),
        # 腿（髋 = 关节原点）
        Part("leg_l_thigh", (-0.10, 0.56, 0.0), (0.08, 0.18, 0.08), MAT_CLOTH, (1, 3), "leg_l"),
        Part("leg_l_shin", (-0.10, 0.24, 0.0), (0.07, 0.16, 0.07), MAT_METAL, (2, 3), "leg_l"),
        Part("leg_l_foot", (-0.10, 0.04, 0.03), (0.08, 0.04, 0.11), MAT_DARK, (3, 3), "leg_l"),
        Part("leg_r_thigh", (0.10, 0.56, 0.0), (0.08, 0.18, 0.08), MAT_CLOTH, (1, 3), "leg_r"),
        Part("leg_r_shin", (0.10, 0.24, 0.0), (0.07, 0.16, 0.07), MAT_METAL, (2, 3), "leg_r"),
        Part("leg_r_foot", (0.10, 0.04, 0.03), (0.08, 0.04, 0.11), MAT_DARK, (3, 3), "leg_r"),
    ]
    return "MechanicalCompanion", parts


# ======================================================================================
# 4. 落盘
# ======================================================================================
def write_model(out_dir, name, parts, manifest_lines, seed=7):
    builder = ObjBuilder(name)
    for part in parts:
        builder.add_box(part)

    os.makedirs(out_dir, exist_ok=True)
    obj_path = os.path.join(out_dir, name + ".obj")
    mtl_path = os.path.join(out_dir, name + ".mtl")
    png_path = os.path.join(out_dir, name + ".png")

    with open(obj_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(builder.to_obj(name + ".mtl", parts))

    with open(mtl_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(to_mtl(MATERIALS))

    cells_used = sorted({(p.material, p.cell) for p in parts})
    image = build_texture(MATERIALS, cells_used, seed=seed)
    image.save(png_path)

    report = builder.to_manifest(parts, name)
    manifest_lines.append(report)
    manifest_lines.append("  文件: %s / %s / %s" % (os.path.basename(obj_path),
                                                  os.path.basename(mtl_path),
                                                  os.path.basename(png_path)))
    manifest_lines.append("  贴图: %dx%d（%d 个 %dx%d 格子）"
                          % (image.width, image.height, CELLS, CELL_PX, CELL_PX))
    manifest_lines.append("")

    print(report)
    print("  文件: %s" % obj_path)
    print("  文件: %s" % mtl_path)
    print("  文件: %s  (%dx%d)" % (png_path, image.width, image.height))
    print("")
    return report


# ======================================================================================
# 5. 自检：用 Python 复刻 InnoVault 的 OBJ 导入算法，回读生成的 OBJ 并核对
# ======================================================================================
# 为什么要有这一段
# ----------------
# 本机**没有 .NET SDK**（只有运行时 + tModLoader 自带的 Roslyn），所以没法把
# `InnoVault.dll` 拖进一个离线宿主里跑 `ObjModelLoader`。退而求其次：把
# `Models3D/Wavefront/ObjParser.cs`、`ObjMeshBuilder.cs`、`ObjModelLoader.cs`
# 的行为**逐条照着写一遍**，用它回读生成的 OBJ，核对：
#   · 面是否都是三角形、索引是否越界
#   · UV 是否都落在 [0,1]
#   · 法线是否都是单位向量
#   · 有没有退化（面积 0）三角形 —— 这是 OBJ 写反绕序/重复顶点的典型症状
#   · (position, uv, normal) 三元组去重后的顶点数 —— 应当等于游戏里
#     `Vault3DModel.VertexCount` 打的日志
# 再用 InnoVault 官方示例 `LowPolyCrystal.obj`（14 v / 24 tri）做交叉验证，
# 确认这段复刻本身没写错。

class _InnoVaultLikeImport(object):
    """`Models3D/Wavefront/ObjParser.cs` + `ObjMeshBuilder.cs` 的等价复刻。"""

    def __init__(self, flip_v=True, y_up_to_y_down=True):
        self.flip_v = flip_v
        self.y_up_to_y_down = y_up_to_y_down
        self.positions = []
        self.texcoords = []
        self.normals = []
        self.faces = []            # (material, [(p,t,n), x3])

    @staticmethod
    def _index(token):
        """InnoVault 的 ObjParser：OBJ 是 1-based，读进来减 1；缺失记 -1。

        负索引（相对索引）在此实现里**不特判**，与源码一致。
        """
        if token == "":
            return -1
        return int(token) - 1

    def parse(self, text):
        material = ""
        for raw in text.splitlines():
            line = raw.strip()
            if not line or line[0] == "#":
                continue
            parts = line.split()
            key = parts[0]
            if key == "v" and len(parts) >= 4:
                self.positions.append(tuple(float(x) for x in parts[1:4]))
            elif key == "vt" and len(parts) >= 3:
                self.texcoords.append((float(parts[1]), float(parts[2])))
            elif key == "vn" and len(parts) >= 4:
                self.normals.append(tuple(float(x) for x in parts[1:4]))
            elif key == "usemtl" and len(parts) >= 2:
                material = " ".join(parts[1:])
            elif key == "f" and len(parts) >= 4:
                verts = []
                for token in parts[1:]:
                    bits = token.split("/")
                    p = self._index(bits[0]) if len(bits) > 0 else -1
                    t = self._index(bits[1]) if len(bits) > 1 else -1
                    n = self._index(bits[2]) if len(bits) > 2 else -1
                    verts.append((p, t, n))
                # 扇形三角化（源码默认 TriangulateNGons = true）
                for i in range(1, len(verts) - 1):
                    self.faces.append((material, [verts[0], verts[i], verts[i + 1]]))

    def build(self):
        """返回 (groups, stats)。groups: [(material, verts, tris)]，verts 是 (pos,uv,normal)。"""
        problems = []
        by_material = {}
        order = []

        if not self.positions and self.faces:
            problems.append("有面但没有顶点")

        for material, tri in self.faces:
            if material not in by_material:
                by_material[material] = []
                order.append(material)
            by_material[material].append(tri)

        groups = []
        for material in order:
            dedup = {}
            verts = []
            indices = []
            for tri in by_material[material]:
                if len(tri) != 3:
                    problems.append("非三角形面（三角化后仍有）")
                    continue
                local = []
                for (pi, ti, ni) in tri:
                    if pi < 0 or pi >= len(self.positions):
                        problems.append("位置索引越界 %d" % pi)
                        continue
                    pos = self.positions[pi]
                    if self.y_up_to_y_down:
                        pos = (pos[0], -pos[1], pos[2])
                    uv = (0.0, 0.0)
                    if 0 <= ti < len(self.texcoords):
                        uv = self.texcoords[ti]
                        if self.flip_v:
                            uv = (uv[0], 1.0 - uv[1])
                    nrm = (0.0, 0.0, 0.0)
                    has_n = 0 <= ni < len(self.normals)
                    if has_n:
                        n = self.normals[ni]
                        nrm = (n[0], -n[1], n[2]) if self.y_up_to_y_down else n
                    key = (pi, ti, ni)
                    if key not in dedup:
                        dedup[key] = len(verts)
                        verts.append([pos, uv, nrm, has_n])
                    local.append(dedup[key])
                if len(local) == 3:
                    indices.extend(local)
            groups.append((material, verts, indices))

        # 法线合法性 / 面法线补全 / 归一化（对应 ObjMeshBuilder 的收尾逻辑）
        degenerate = 0
        for material, verts, indices in groups:
            for i in range(0, len(indices), 3):
                a = verts[indices[i]][0]
                b = verts[indices[i + 1]][0]
                c = verts[indices[i + 2]][0]
                ab = (b[0] - a[0], b[1] - a[1], b[2] - a[2])
                ac = (c[0] - a[0], c[1] - a[1], c[2] - a[2])
                cross = (ab[1] * ac[2] - ab[2] * ac[1],
                         ab[2] * ac[0] - ab[0] * ac[2],
                         ab[0] * ac[1] - ab[1] * ac[0])
                if (cross[0] ** 2 + cross[1] ** 2 + cross[2] ** 2) ** 0.5 < 1e-6:
                    degenerate += 1
            for v in verts:
                n = v[2]
                if n[0] ** 2 + n[1] ** 2 + n[2] ** 2 <= 1e-6:
                    problems.append("零法线（InnoVault 会补成面法线或 +Z）")

        vertex_count = sum(len(v) for _, v, _ in groups)
        triangle_count = sum(len(i) // 3 for _, _, i in groups)
        stats = {
            "groups": len(groups),
            "vertex_count": vertex_count,
            "triangle_count": triangle_count,
            "degenerate": degenerate,
            "problems": problems,
        }
        return groups, stats


def _cross_check_crystal():
    """用 InnoVault 官方示例的 LowPolyCrystal.obj 验证上面的复刻是否写对。"""
    path = r"E:\开发\.tmp-research\example\Assets\Models\LowPolyCrystal.obj"
    if not os.path.exists(path):
        return None
    with open(path, "r", encoding="utf-8") as handle:
        text = handle.read()
    imp = _InnoVaultLikeImport()
    imp.parse(text)
    groups, stats = imp.build()
    ok = (len(imp.positions) == 14 and stats["triangle_count"] == 24
          and stats["degenerate"] == 0 and stats["groups"] == 2)
    return ok, len(imp.positions), stats


def run_verify(out_dir):
    print("=" * 74)
    print("自检：复刻 InnoVault 的 OBJ 导入算法，回读生成的 OBJ")
    print("=" * 74)

    crystal = _cross_check_crystal()
    if crystal is None:
        print("[跳过] 找不到 InnoVault 示例 LowPolyCrystal.obj，无法交叉验证")
    elif crystal[0]:
        print("[OK] 交叉验证：InnoVault 示例 LowPolyCrystal.obj -> %d 顶点 / %d 三角形 / %d 材质分组"
              % (crystal[1], crystal[2]["triangle_count"], crystal[2]["groups"]))
    else:
        print("!! 交叉验证失败：复刻实现可能写错了 -> %s" % (crystal,))

    print("")

    failures = [] if (crystal is None or crystal[0]) else ["复刻实现未通过官方示例交叉验证"]

    # 生成器"本来打算"的部件表 —— 校验 OBJ/贴图时对照用
    parts_by_name = {}
    for job_name, job_parts in (model_probe(), model_humanoid()) + tuple(model_armor()):
        parts_by_name[job_name] = job_parts

    names = ["Wasteland3DProbe", "SalvagedSteelHelmet", "SalvagedSteelChestplate",
             "SalvagedSteelGreaves", "MechanicalCompanion"]

    for name in names:
        obj_path = os.path.join(out_dir, name + ".obj")
        mtl_path = os.path.join(out_dir, name + ".mtl")
        png_path = os.path.join(out_dir, name + ".png")

        if not os.path.exists(obj_path):
            failures.append("%s 不存在" % obj_path)
            continue

        with open(obj_path, "r", encoding="utf-8") as handle:
            text = handle.read()

        imp = _InnoVaultLikeImport()
        imp.parse(text)
        groups, stats = imp.build()

        uv_bad = 0
        for _, verts, _ in groups:
            for v in verts:
                if not (0.0 <= v[1][0] <= 1.0 and 0.0 <= v[1][1] <= 1.0):
                    uv_bad += 1

        # MTL 必须没有 map_Kd（否则加载期会在工作线程建 GPU 贴图）。
        # 注意：注释里**故意**提到了 map_Kd 这个名字，所以要跳过注释行。
        mtl_text = ""
        if os.path.exists(mtl_path):
            with open(mtl_path, "r", encoding="utf-8") as handle:
                mtl_text = handle.read()
        has_map_kd = any(line.strip().startswith("map_Kd")
                         for line in mtl_text.splitlines()
                         if not line.strip().startswith("#"))

        # 贴图必须存在，且**模型实际用到的那些 UV 格子**不能是全透明的。
        #
        # ⚠️ 这里有两套"行号"，写这段时踩了两次，说明白：
        #   · **生成器行号**（本模块 `Part.cell` / `_cell_uv`）：行 0 在贴图**上**边；
        #     `_cell_uv` 把它写成 OBJ 的 v = 1 - row/CELLS（行 0 -> v 接近 1）。
        #   · **OBJ 原始 v**：从 `<Name>.obj` 文本里直接读到的值（上一条那个 v）。
        #   · **贴图像素行**：XNA/PNG 左上原点，行 0 在**上**边。
        #
        #   校验口径：**直接从 OBJ 文本**取每个**面**三个角点的 UV 平均值（面的 UV 质心），
        #   再用 `(col, row) = (floor(u*CELLS), floor((1-v)*CELLS))` 推出格子
        #   （`1-v` 就是 InnoVault 的 `FlipTextureV`）。用质心而不是"最小 u / 最小 v"，
        #   是因为 `_face_uv` 把面的 UV 铺满了整个子格，角和格边界**重合**：
        #   `int(0.75*4)=3`、`int(0.5*4)=2`，直接用角点会算到隔壁格去（写这段时踩了）。
        png_ok = os.path.exists(png_path)
        png_size = None
        missing_cells = set()
        wrong_cells = set()

        expected_cells = {part.cell for part in parts_by_name.get(name, [])}
        actual_cells = set()
        for line in text.splitlines():
            line = line.strip()
            if not line.startswith("f "):
                continue
            corners = []
            for token in line.split()[1:]:
                bits = token.split("/")
                if len(bits) < 2 or bits[1] == "":
                    continue
                corners.append(imp.texcoords[int(bits[1]) - 1])
            if not corners:
                continue
            u = sum(c[0] for c in corners) / len(corners)
            vv = sum(c[1] for c in corners) / len(corners)
            actual_cells.add((min(CELLS - 1, int(u * CELLS)),
                              min(CELLS - 1, int((1.0 - vv) * CELLS))))

        if expected_cells != actual_cells:
            wrong_cells = expected_cells ^ actual_cells

        if png_ok and Image is not None:
            image = Image.open(png_path).convert("RGBA")
            png_size = image.size
            alpha = image.split()[3]
            for (cell_col, cell_row) in actual_cells:
                box = (cell_col * CELL_PX, cell_row * CELL_PX,
                       (cell_col + 1) * CELL_PX, (cell_row + 1) * CELL_PX)
                if alpha.crop(box).getextrema()[1] <= 0:
                    missing_cells.add((cell_col, cell_row))

        print("模型 %-26s 分组=%-2d 顶点=%-5d 三角形=%-4d 退化=%-2d UV越界=%-2d map_Kd=%-5s 贴图=%s"
              % (name, stats["groups"], stats["vertex_count"], stats["triangle_count"],
                 stats["degenerate"], uv_bad, has_map_kd,
                 ("%dx%d" % png_size) if png_size else "缺失"))

        if wrong_cells:
            failures.append("%s: OBJ 用到的 UV 格子与生成器意图不一致 -> %s"
                            % (name, sorted(wrong_cells)))

        if stats["problems"]:
            for p in sorted(set(stats["problems"])):
                print("     !! %s" % p)
                failures.append("%s: %s" % (name, p))
        if stats["degenerate"]:
            failures.append("%s: %d 个退化三角形" % (name, stats["degenerate"]))
        if uv_bad:
            failures.append("%s: %d 个顶点 UV 越界" % (name, uv_bad))
        if has_map_kd:
            failures.append("%s: MTL 里有 map_Kd（加载期会创建 GPU 贴图）" % name)
        if not png_ok:
            failures.append("%s: 缺贴图" % name)
        elif missing_cells:
            failures.append("%s: 贴图里这些 UV 格子是全透明的 %s"
                            % (name, sorted(missing_cells)))

    print("")

    if failures:
        print("!! 自检失败 %d 项：" % len(failures))
        for item in failures:
            print("   -", item)
        return 1

    print("OK  自检通过：OBJ 面/UV/法线/退化、MTL 无 map_Kd、贴图格子均已绘制")
    return 0


# ======================================================================================
# 6. 离线等距预览：不装 Blender / 不开游戏也能肉眼确认模型长得对
# ======================================================================================
# 只做"能看清形状"的程度：正交投影 + 画家算法（按面深度从远到近画）+ 最近邻采样贴图
# + 单向光 Lambert 明暗。不是渲染器，不用来出正式图，只用来抓"部件位置错/穿模/贴图错格"。

def _rotate_y(p, angle):
    c, s = math.cos(angle), math.sin(angle)
    return (p[0] * c + p[2] * s, p[1], -p[0] * s + p[2] * c)


def _rotate_x(p, angle):
    c, s = math.cos(angle), math.sin(angle)
    return (p[0], p[1] * c - p[2] * s, p[1] * s + p[2] * c)


def render_preview(obj_path, png_path, out_path, size=420, yaw=-0.62, pitch=-0.32):
    """把 OBJ + 贴图渲染成一张等距 PNG。"""
    if Image is None:
        raise RuntimeError("需要 Pillow")

    with open(obj_path, "r", encoding="utf-8") as handle:
        lines = handle.read().splitlines()

    positions = []
    texcoords = []
    normals = []
    faces = []
    for line in lines:
        parts = line.split()
        if not parts:
            continue
        if parts[0] == "v":
            positions.append(tuple(float(x) for x in parts[1:4]))
        elif parts[0] == "vt":
            texcoords.append((float(parts[1]), float(parts[2])))
        elif parts[0] == "vn":
            normals.append(tuple(float(x) for x in parts[1:4]))
        elif parts[0] == "f":
            faces.append([tuple(int(x) - 1 for x in tok.split("/")) for tok in parts[1:]
                          if tok.count("/") >= 1])

    if not positions or not faces:
        return None

    texture = Image.open(png_path).convert("RGBA")
    tw, th = texture.size

    # ---- 变换到相机空间 ----
    # 先按 InnoVault 的口径翻 Y（Y 朝上 -> Y 朝下），再绕 Y/Z 转出 3/4 视角
    world = [(p[0], -p[1], p[2]) for p in positions]
    world = [_rotate_y(p, yaw) for p in world]
    world = [_rotate_x(p, pitch) for p in world]

    xs = [p[0] for p in world]
    ys = [p[1] for p in world]
    span = max(max(xs) - min(xs), max(ys) - min(ys), 1e-6)
    scale = size * 0.82 / span
    cx = (max(xs) + min(xs)) * 0.5
    cy = (max(ys) + min(ys)) * 0.5

    def project(p):
        return (size * 0.5 + (p[0] - cx) * scale, size * 0.5 - (p[1] - cy) * scale)

    # 光照方向（相机空间，指向观察者偏左上）
    light = (-0.45, -0.62, 0.64)
    lnorm = math.sqrt(sum(c * c for c in light))
    light = tuple(c / lnorm for c in light)

    image = Image.new("RGBA", (size, size), (18, 18, 22, 255))
    pixels = image.load()
    # 深度缓冲：按面中心排序的"画家算法"对这些互相穿插的方块**不成立**
    # （背面会盖住正面，头盔会渲成"掀开的盖子"）。所以老老实实做逐像素 z 测试。
    zbuf = [[float("inf")] * size for _ in range(size)]

    for face in faces:
        if len(face) < 3:
            continue
        pts = [world[i[0]] for i in face]
        if len(pts) < 3:
            continue

        # 面法线 -> 明暗（XNA 是左手系，这里只用 vn 做着色，不做背面剔除）
        n = normals[face[0][2]] if 0 <= face[0][2] < len(normals) else (0, 0, 1)
        n = _rotate_y((n[0], -n[1], n[2]), yaw)
        n = _rotate_x(n, pitch)
        lambert = abs(n[0] * light[0] + n[1] * light[1] + n[2] * light[2])
        shade = 0.34 + 0.76 * lambert

        screen = [project(p) for p in pts]
        uv = [texcoords[i[1]] if 0 <= i[1] < len(texcoords) else (0.5, 0.5) for i in face]

        # OBJ 的 v 原点在**左下**，贴图像素行原点在**左上**：这里手动翻一次。
        # （游戏里这一步是 InnoVault 的 `ObjImportOptions.FlipTextureV` 做的 —— 只翻一次！）
        uv = [(u, 1.0 - v) for (u, v) in uv]

        # 扫描线 + 重心插值 UV/z（扇形三角化）
        for k in range(1, len(screen) - 1):
            tri = [screen[0], screen[k], screen[k + 1]]
            tri_uv = [uv[0], uv[k], uv[k + 1]]
            tri_z = [pts[0][2], pts[k][2], pts[k + 1][2]]
            _fill_triangle(pixels, zbuf, tri, tri_uv, tri_z, texture, tw, th, shade, size)

    image.save(out_path)
    return out_path


def _fill_triangle(pixels, zbuf, tri, uv, zvals, texture, tw, th, shade, size):
    (x0, y0), (x1, y1), (x2, y2) = tri
    min_x = max(0, int(math.floor(min(x0, x1, x2))))
    max_x = min(size - 1, int(math.ceil(max(x0, x1, x2))))
    min_y = max(0, int(math.floor(min(y0, y1, y2))))
    max_y = min(size - 1, int(math.ceil(max(y0, y1, y2))))

    denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
    if abs(denom) < 1e-9:
        return

    tex_px = texture.load()
    z0, z1, z2 = zvals

    for py in range(min_y, max_y + 1):
        row = zbuf[py]
        for px in range(min_x, max_x + 1):
            fx, fy = px + 0.5, py + 0.5
            w0 = ((y1 - y2) * (fx - x2) + (x2 - x1) * (fy - y2)) / denom
            w1 = ((y2 - y0) * (fx - x2) + (x0 - x2) * (fy - y2)) / denom
            w2 = 1.0 - w0 - w1
            if w0 < -1e-6 or w1 < -1e-6 or w2 < -1e-6:
                continue

            # 深度测试：这个投影下 z 越小越靠近观察者
            z = w0 * z0 + w1 * z1 + w2 * z2
            if z >= row[px]:
                continue

            u = w0 * uv[0][0] + w1 * uv[1][0] + w2 * uv[2][0]
            v = w0 * uv[0][1] + w1 * uv[1][1] + w2 * uv[2][1]
            # UV 是"归一化且上界含 1"的：u=1.0 落在最后一个像素上，
            # 所以用像素中心取整，不能直接 int(u*tw)（u=1.0 会越界，夹紧后整片采到透明像素）。
            tx = min(tw - 1, max(0, int(u * (tw - 1) + 0.5)))
            ty = min(th - 1, max(0, int(v * (th - 1) + 0.5)))
            r, g, b, a = tex_px[tx, ty]
            if a == 0:
                continue
            row[px] = z
            pixels[px, py] = (min(255, int(r * shade)), min(255, int(g * shade)),
                              min(255, int(b * shade)), 255)


def run_preview(out_dir, preview_dir):
    if Image is None:
        print("!! 需要 Pillow")
        return 1

    os.makedirs(preview_dir, exist_ok=True)
    names = [("Wasteland3DProbe", 0.0), ("SalvagedSteelHelmet", 0.0),
             ("SalvagedSteelChestplate", 0.0), ("SalvagedSteelGreaves", 0.0),
             ("MechanicalCompanion", 0.0)]
    made = []
    for name, _ in names:
        obj_path = os.path.join(out_dir, name + ".obj")
        png_path = os.path.join(out_dir, name + ".png")
        if not (os.path.exists(obj_path) and os.path.exists(png_path)):
            continue
        out_path = os.path.join(preview_dir, name + "_preview.png")
        if render_preview(obj_path, png_path, out_path):
            made.append(out_path)
            print("预览: %s" % out_path)

    if not made:
        print("!! 没有可预览的模型")
        return 1
    print("共 %d 张预览" % len(made))
    return 0


def main():
    parser = argparse.ArgumentParser(description="程序化生成 InnoVault Models3D 用的 OBJ/MTL/PNG")
    parser.add_argument("--out", default=DEFAULT_OUT, help="输出目录")
    parser.add_argument("--only", default="all",
                        choices=["all", "probe", "armor", "humanoid"])
    parser.add_argument("--list", action="store_true", help="只打印部件表，不写文件")
    parser.add_argument("--verify", action="store_true",
                        help="用 Python 复刻 InnoVault 的 OBJ 导入算法，回读并自检（并交叉验证 InnoVault 自带示例）")
    parser.add_argument("--preview", metavar="目录",
                        help="额外渲染等距预览图到这个目录（不需要 Blender / 不需要开游戏）")
    args = parser.parse_args()

    if Image is None:
        print("!! 需要 Pillow：pip install Pillow")
        return 1

    if args.verify:
        return run_verify(args.out)

    if args.preview:
        return run_preview(args.out, args.preview)

    wanted = args.only
    manifest = []

    jobs = []
    if wanted in ("all", "probe"):
        jobs.append(model_probe())
    if wanted in ("all", "armor"):
        jobs.extend(model_armor())
    if wanted in ("all", "humanoid"):
        jobs.append(model_humanoid())

    if args.list:
        for name, parts in jobs:
            builder = ObjBuilder(name)
            for part in parts:
                builder.add_box(part)
            print(builder.to_manifest(parts, name))
            print("")
        return 0

    for index, (name, parts) in enumerate(jobs):
        write_model(args.out, name, parts, manifest, seed=7 + index)

    manifest_path = os.path.join(args.out, "MODELS_3D_清单.txt")
    with open(manifest_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("由 tools/gen_3d_models.py 生成 —— 3D 模型清单\n")
        handle.write("=" * 72 + "\n\n")
        handle.write("\n".join(manifest))
    print("清单: %s" % manifest_path)
    print("共 %d 个模型" % len(jobs))
    return 0


if __name__ == "__main__":
    sys.exit(main())
