"""同步翻译：比对主模组的英文键与汉化补丁的中文键，列出待翻译清单。

用法:
  python sync_translation.py             # 只报告缺哪些键
  python sync_translation.py --restore   # 从备份还原补丁的中文文件（重要，见下）

重要已知问题：tModLoader 在**加载模组时**会重写「本模组源目录里的本地化文件」。
补丁模组里的键其实属于主模组，所以被重写时会只剩「本次真正用到的键」，
原文会被削掉。打包进 .tmod 的内容不受影响（玩家拿到的是完整的），
但**开发机上的源文件会被破坏**——所以：
  1) 真正的原文备份在 E:\\开发\\.backup\\WastelandSoulCN_zh-Hans.hjson
  2) 每次跑完加载测试、准备重新构建补丁之前，先 --restore 还原一次
"""
import os
import shutil
import sys

MAIN_LOC = r"E:\开发\WastelandSoul\Localization"
CN_LOC = r"E:\开发\WastelandSoulCN\Localization"
CN_BACKUP = r"E:\开发\.backup\WastelandSoulCN_zh-Hans.hjson"

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from check_localization import parse_keys  # noqa: E402


def load(path, prefix):
    with open(path, encoding="utf-8") as handle:
        return parse_keys(handle.read(), prefix)


def main_localization(culture, target_mod):
    """主模组自己的本地化文件，例如 en-US_Mods.WastelandSoul.hjson。"""
    path = os.path.join(MAIN_LOC, "%s_Mods.%s.hjson" % (culture, target_mod))
    return path if os.path.exists(path) else None


def patch_localization(culture, target_mod):
    """汉化补丁里的目标模组本地化文件。

    必须是扁平前缀写法 Localization/<culture>_Mods.<目标模组>.hjson：
      · 分模组子文件夹（Localization/<目标模组>/...）与分语言文件夹（Localization/<culture>/...）
        在当前 tModLoader 里都会被改名成 .legacy 而失效
      · 扁平写法能被识别并生效（已实测：补丁成功覆盖了主模组的文本）
    """
    path = os.path.join(CN_LOC, "%s_Mods.%s.hjson" % (culture, target_mod))
    return path if os.path.exists(path) else None


def restore_backup():
    if not os.path.exists(CN_BACKUP):
        print("!! 找不到备份:", CN_BACKUP)
        return 1
    shutil.copyfile(CN_BACKUP, os.path.join(CN_LOC, "zh-Hans_Mods.WastelandSoul.hjson"))
    print("已从备份还原补丁的中文文件（%d 字节）" % os.path.getsize(CN_BACKUP))
    return 0


def main():
    target = "WastelandSoul"

    if "--restore" in sys.argv:
        return restore_backup()

    main_en = main_localization("en-US", target)
    cn_zh = patch_localization("zh-Hans", target)

    if not main_en:
        print("!! 找不到主模组的 en-US 本地化文件:", MAIN_LOC)
        return 1
    if not cn_zh:
        print("!! 找不到汉化补丁的 zh-Hans 本地化文件:", CN_LOC,
              "\n   提示：若被 tModLoader 重写/改名，先运行 --restore 还原")
        return 1

    en = load(main_en, "Mods." + target)
    zh = load(cn_zh, "Mods." + target)

    # 自动键（内容类型的 DisplayName/Tooltip 等）由 tModLoader 处理，这里也算进去，缺了就该补
    # 但英文原文本身是空的（例如 tModLoader 自动补的 Tooltip: ""）不算待翻译
    missing = sorted(k for k in set(en) - set(zh) if en[k] not in ("", '""', "<multiline>"))
    extra = sorted(set(zh) - set(en))

    print("主模组 en-US: %d 条" % len(en))
    print("汉化补丁 zh-Hans: %d 条" % len(zh))

    if missing:
        print("\n待翻译 %d 条（汉化补丁里缺失）:" % len(missing))
        for key in missing:
            short = key.replace("Mods.WastelandSoul.", "")
            value = en[key]
            if value in ("<multiline>", '""'):
                value = "(空)"
            print("  %-62s | %s" % (short, value[:70]))
    else:
        print("\n没有缺失的键，汉化补丁是完整的 ✓")

    if extra:
        print("\n汉化补丁里有 %d 条主模组已不存在的键（可能已改名/删除）:" % len(extra))
        for key in extra:
            print("  ", key.replace("Mods.WastelandSoul.", ""))

    if "--fill" in sys.argv and missing:
        print("\n--fill：未实现自动写入（避免覆盖已有译文），请按上面的清单手工补进：")
        print("  ", cn_zh)

    return 0


if __name__ == "__main__":
    sys.exit(main())
