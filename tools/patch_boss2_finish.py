# -*- coding: utf-8 -*-
"""⚠️⚠️ 一次性历史脚本 —— 【不要重跑】⚠️⚠️

它的作用早已被正式代码取代，而且**部分改动已在批次 11 回滚**，重跑会重新制造这些问题：

  1. 会重新创建重复召唤物 `ArchivistSummon`（Boss2 正确的召唤物是 `ArchivistEcho`：
     残响碎片 ×8 + 骨头 ×5 + 陨石锭 ×3，见「开发说明.md」Boss2 设计要点）；
  2. 会把 `Archivist.png` 覆盖成 **84×336**，而 `Archivist.cs` 的命中框是 110×110
     （正确帧表是 `gen_sprites.py` 生成的 **110×440**）；
  3. 会绕过译文表，把中文直接塞进**生成文件**（下次同步就被冲掉）。

要重新生成归档者的占位美术，请用 `python E:\开发\tools\gen_sprites.py`；
要改剧情标志位 / 注册表 / 召唤物，请直接改对应源码文件。保留此文件只为留档。

---- 以下为原始说明 ----
把 Boss2 归档者补到可编译可游玩状态：
1) WastelandStorySystem 增加 archivistDefeated 标志与 MarkArchivistDefeated()
2) WastelandBossRegistry.ResolveNpcType 增加 Archivist 分支，并把 Implemented 置 true
3) 新增召唤物 ArchivistSummon（归档者的索引，祭坛合成）
4) 生成本体帧表 / Boss 头像 / 召唤物贴图
5) 给召唤物补中文名
"""
import io
import os
import re
import shutil

MAIN = r"E:\开发\WastelandSoul"
STORY = os.path.join(MAIN, r"Common\Systems\WastelandStorySystem.cs")
REGISTRY = os.path.join(MAIN, r"Common\Bosses\WastelandBossRegistry.cs")
SUMMON = os.path.join(MAIN, r"Content\Items\Summons\ArchivistSummon.cs")
PATCH_LOC = r"E:\开发\WastelandSoulCN\Localization\zh-Hans_Mods.WastelandSoul.hjson"
BACKUP = r"E:\开发\.backup\WastelandSoulCN_zh-Hans.hjson"


# ---------------------------------------------------------------- 1) 剧情标志位
def patch_story():
    text = io.open(STORY, encoding="utf-8").read()

    if "MarkArchivistDefeated" in text:
        print("  [跳过] 剧情标志位已存在")
        return

    method = u'''
		/// <summary>Boss2 归档者是否已被击败（记忆主线第二段的前置）。</summary>
		public static bool archivistDefeated;

		/// <summary>标记归档者已被击败（幂等）。</summary>
		public static void MarkArchivistDefeated()
		{
			archivistDefeated = true;
		}
'''

    idx = text.rfind("\n\t}")
    if idx < 0:
        print("  !! 找不到类的结尾，未修改剧情文件")
        return

    io.open(STORY, "w", encoding="utf-8").write(text[:idx] + method + text[idx:])
    print("  [OK] 已加入 archivistDefeated / MarkArchivistDefeated")


# ---------------------------------------------------------------- 2) 注册表分支
def patch_registry():
    text = io.open(REGISTRY, encoding="utf-8").read()

    if 'case "Archivist"' in text:
        print("  [跳过] 注册表分支已存在")
        return

    old = "return ModContent.NPCType<Scavenger>();"
    if old not in text:
        print("  !! 注册表里找不到 Scavenger 分支，未修改")
        return

    new = (old +
           u'\n\t\t\t\tcase "Archivist":\n'
           u'\t\t\t\t\treturn ModContent.NPCType<Content.NPCs.Bosses.Archivist.Archivist>();')

    text = text.replace(old, new, 1)
    # 把归档者那行的 Implemented 置为 true（只动紧跟 InternalName = "Archivist" 的那一处）
    text = re.sub(r'(InternalName = "Archivist",\s*\n\s*Progression = [\d.]+f,\s*\n\s*Implemented = )false',
                  r'\1true', text)

    io.open(REGISTRY, "w", encoding="utf-8").write(text)
    print("  [OK] 已加入 Archivist 分支")


# ---------------------------------------------------------------- 3) 召唤物
SUMMON_CS = u'''using Terraria.ID;
using Terraria.ModLoader;
using WastelandSoul.Common.ItemBases;
using WastelandSoul.Content.Items.Materials;
using WastelandSoul.Content.NPCs.Bosses.Archivist;

namespace WastelandSoul.Content.Items.Summons
{
	/// <summary>
	/// 归档者的索引：在**恶魔/猩红祭坛**上合成的召唤物，把归档者引到玩家身边。
	/// <para/>不消耗、同一时间只允许一只（逻辑在 WastelandSummonItem 基类里）。
	/// </summary>
	public class ArchivistSummon : WastelandSummonItem
	{
		protected override int BossType => ModContent.NPCType<Archivist>();

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ArchivistFragment>(6)
				.AddIngredient(ItemID.Bone, 15)
				.AddIngredient(ItemID.MeteoriteBar, 8)
				.AddTile(WastelandCraftingStations.SummonAltar)
				.Register();
		}
	}
}
'''


def write_summon():
    if os.path.exists(SUMMON):
        print("  [跳过] 召唤物已存在")
        return

    io.open(SUMMON, "w", encoding="utf-8").write(SUMMON_CS)
    print("  [OK] 已写入 ArchivistSummon.cs")


# ---------------------------------------------------------------- 4) 贴图
def draw_textures():
    from PIL import Image, ImageDraw

    body = (214, 218, 228, 255)
    dark = (120, 128, 148, 255)
    glow = (150, 200, 255, 255)

    # 本体：4 帧，每帧 84x84（一本悬浮的档案终端）
    sheet = Image.new("RGBA", (84, 84 * 4), (0, 0, 0, 0))

    for frame in range(4):
        img = Image.new("RGBA", (84, 84), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = (0, -2, 0, 2)[frame]

        d.rounded_rectangle((14, 22 + bob, 70, 66 + bob), radius=6, fill=body, outline=dark, width=3)
        d.rounded_rectangle((20, 28 + bob, 64, 60 + bob), radius=3, fill=(238, 240, 248, 255))
        for y in (34, 40, 46, 52):
            d.line((26, y + bob, 58, y + bob), fill=(168, 176, 196, 255), width=2)
        # 悬挂的书签
        d.polygon([(38, 22 + bob), (46, 22 + bob), (46, 10 + bob), (42, 15 + bob), (38, 10 + bob)],
                  fill=(196, 88, 96, 255), outline=dark)
        # 眼部冷光
        d.ellipse((32, 12 + bob, 52, 24 + bob), fill=glow, outline=dark, width=2)
        d.ellipse((38, 15 + bob, 46, 21 + bob), fill=(255, 255, 255, 255))
        # 侧翼索引片
        d.polygon([(6, 40 + bob), (14, 34 + bob), (14, 56 + bob), (6, 50 + bob)], fill=dark)
        d.polygon([(78, 40 + bob), (70, 34 + bob), (70, 56 + bob), (78, 50 + bob)], fill=dark)

        sheet.alpha_composite(img, (0, frame * 84))

    sheet.save(os.path.join(MAIN, r"Content\NPCs\Bosses\Archivist\Archivist.png"))

    # Boss 头像（地图图标）
    head = Image.new("RGBA", (40, 40), (0, 0, 0, 0))
    d = ImageDraw.Draw(head)
    d.rounded_rectangle((4, 10, 36, 32), radius=4, fill=body, outline=dark, width=2)
    d.ellipse((14, 4, 26, 14), fill=glow, outline=dark, width=2)
    head.save(os.path.join(MAIN, r"Content\NPCs\Bosses\Archivist\Archivist_Head_Boss.png"))

    # 召唤物
    item = Image.new("RGBA", (26, 26), (0, 0, 0, 0))
    d = ImageDraw.Draw(item)
    d.rounded_rectangle((5, 6, 21, 22), radius=3, fill=body, outline=dark, width=2)
    d.line((8, 11, 18, 11), fill=(168, 176, 196, 255))
    d.line((8, 15, 18, 15), fill=(168, 176, 196, 255))
    d.polygon([(12, 6), (15, 6), (15, 1), (13.5, 3.5), (12, 1)], fill=(196, 88, 96, 255))
    item.save(os.path.join(MAIN, r"Content\Items\Summons\ArchivistSummon.png"))

    print("  [OK] 已生成 本体帧表 / Boss 头像 / 召唤物 贴图")


# ---------------------------------------------------------------- 5) 中文名
def patch_localization():
    text = io.open(PATCH_LOC, encoding="utf-8").read()

    if "ArchivistSummon.DisplayName" in text:
        print("  [跳过] 召唤物中文名已存在")
        return

    anchor = "Items: {\n"
    if anchor not in text:
        print("  !! 找不到 Items 段，未加中文名")
        return

    add = (anchor +
           u'\tArchivistSummon.DisplayName: 归档者的索引\n'
           u'\tArchivistSummon.Tooltip: 在恶魔或猩红祭坛上合成。把它引出来——它一直在回收你不该记起的东西。\n')

    io.open(PATCH_LOC, "w", encoding="utf-8").write(text.replace(anchor, add, 1))
    shutil.copyfile(PATCH_LOC, BACKUP)
    print("  [OK] 已加召唤物中文名并刷新备份")


def main():
    patch_story()
    patch_registry()
    write_summon()
    draw_textures()
    patch_localization()


if __name__ == "__main__":
    main()
