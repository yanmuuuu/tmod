"""静态校验模组资源：贴图路径与帧布局是否与代码一致。

模拟 tModLoader 的贴图路径约定（命名空间路径 + 类名 + .png），检查：
  - 每个 ModNPC / ModItem / ModProjectile / ModBuff 是否都有对应贴图；
  - [AutoloadHead] 的 _Head.png、[AutoloadBossHead] 的 _Head_Boss.png 是否存在；
  - 贴图高度能否被 Main.npcFrameCount[Type] 整除（否则帧会错位）。
"""
import os
import re
import struct
import sys

_TOOLS = os.path.dirname(os.path.abspath(__file__))
_ROOT = os.path.dirname(_TOOLS)
MOD_ROOT = os.path.join(_ROOT, "WastelandSoul") if os.path.isdir(os.path.join(_ROOT, "WastelandSoul")) else r"E:\开发\WastelandSoul"

PAT_NS = re.compile(r"namespace\s+([A-Za-z0-9_.]+)")
PAT_CLASS = re.compile(r"class\s+(\w+)\s*:\s*(ModNPC|ModItem|ModProjectile|ModBuff|ModTile|ModWall)\b")
# 宽松版：任何 class A : B 都收，用来沿继承链判断
PAT_ANY_CLASS = re.compile(r"class\s+(\w+)\s*:\s*([A-Za-z_][\w.]*)")
PAT_FRAMES = re.compile(r"Main\.npcFrameCount\[Type\]\s*=\s*(\d+)")
PAT_PROJFRAMES = re.compile(r"Main\.projFrames\[Type\]\s*=\s*(\d+)")

# 需要贴图才能加载的基类
NEEDS_TEXTURE = {"ModNPC", "ModItem", "ModProjectile", "ModBuff", "ModTile", "ModWall"}


def collect_bases():
    """整个工程里 class 名 -> 直接基类名（用于沿继承链解析）。"""
    bases = {}

    for root, dirs, files in os.walk(MOD_ROOT):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin")]
        for name in files:
            if not name.endswith(".cs"):
                continue
            with open(os.path.join(root, name), encoding="utf-8") as handle:
                src = handle.read()
            for match in PAT_ANY_CLASS.finditer(src):
                bases.setdefault(match.group(1), match.group(2).split(".")[-1])

    return bases


def resolve_content_base(cls, bases, limit=12):
    """沿继承链往上找，返回 ModNPC/ModItem/... 之一；不是内容类返回 None。

    这一步很关键：以前只看「直接基类」，所以派生自自定义基类的物品
    （武器/材料/掉落袋/灵魂碎片等）全都被漏掉了，校验形同虚设。
    """
    seen = set()
    current = cls

    for _ in range(limit):
        base = bases.get(current)

        if base is None:
            return None
        if base in NEEDS_TEXTURE:
            return base
        if base in seen:
            return None

        seen.add(base)
        current = base

    return None

# 帧高固定约定：城镇 NPC 每帧 40x56，头部 16x16
TOWN_NPC_FRAME = (40, 56)
HEAD_SIZE = (16, 16)


def png_size(path):
    with open(path, "rb") as handle:
        data = handle.read(26)
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", data[16:24])


def main():
    problems = []
    ok_count = 0
    checked = 0
    bases = collect_bases()

    for root, dirs, files in os.walk(MOD_ROOT):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin")]
        for name in files:
            if not name.endswith(".cs"):
                continue

            path = os.path.join(root, name)
            with open(path, encoding="utf-8") as handle:
                src = handle.read()

            ns_match = PAT_NS.search(src)
            if not ns_match:
                continue
            namespace = ns_match.group(1)

            if not namespace.startswith("WastelandSoul."):
                continue
            rel_dir = namespace[len("WastelandSoul."):].replace(".", os.sep)

            for cls_match in PAT_ANY_CLASS.finditer(src):
                cls = cls_match.group(1)
                base = resolve_content_base(cls, bases)
                if base is None:
                    continue

                # 抽象类不会被 tModLoader 自动加载，不需要贴图
                preceding_for_abstract = src[max(0, cls_match.start() - 60):cls_match.start()]
                if "abstract" in preceding_for_abstract:
                    continue

                checked += 1
                # 属性写在 class 声明附近，取类声明前 400 字符判断
                preceding = src[max(0, cls_match.start() - 400):cls_match.start()]

                tex_rel = os.path.join(rel_dir, cls + ".png")
                tex_abs = os.path.join(MOD_ROOT, tex_rel)

                if not os.path.exists(tex_abs):
                    problems.append("缺失贴图: %s (%s)" % (tex_rel, cls))
                    continue

                size = png_size(tex_abs)
                ok_count += 1
                note = "%dx%d" % size

                if base == "ModNPC":
                    frames_match = PAT_FRAMES.search(src)

                    if "AutoloadHead" in preceding:
                        head_rel = os.path.join(rel_dir, cls + "_Head.png")
                        head_abs = os.path.join(MOD_ROOT, head_rel)

                        if not os.path.exists(head_abs):
                            problems.append("缺失头部贴图: %s" % head_rel)
                        else:
                            head_size = png_size(head_abs)
                            if head_size != HEAD_SIZE:
                                problems.append("头部贴图尺寸应为 16x16，实际 %dx%d (%s)"
                                                % (head_size[0], head_size[1], head_rel))

                        if size[0] != TOWN_NPC_FRAME[0]:
                            problems.append("城镇 NPC 帧宽应为 40，实际 %d (%s)" % (size[0], tex_rel))

                    if "AutoloadBossHead" in preceding:
                        boss_head_rel = os.path.join(rel_dir, cls + "_Head_Boss.png")
                        if not os.path.exists(os.path.join(MOD_ROOT, boss_head_rel)):
                            problems.append("缺失 Boss 头像贴图: %s" % boss_head_rel)

                    if frames_match:
                        frames = int(frames_match.group(1))
                        if frames <= 0 or size[1] % frames != 0:
                            problems.append("帧高不整除: %s 高 %d / %d 帧" % (tex_rel, size[1], frames))
                        else:
                            note += "  帧 %d × 高 %d" % (frames, size[1] // frames)

                print("OK  %-58s %-22s %s" % (tex_rel.replace(os.sep, "/"), cls, note))

    print("\n已检查可加载类型: %d，贴图正常: %d" % (checked, ok_count))

    if problems:
        print("\n!! 发现 %d 个问题:" % len(problems))
        for item in problems:
            print("   -", item)
        return 1

    print("OK  所有贴图路径与帧布局校验通过")
    return 0


if __name__ == "__main__":
    sys.exit(main())
