# -*- coding: utf-8 -*-
"""校验汉化补丁的**备份快照**是不是「干净的那一份」。

为什么需要这个检查器（一次真实的自我复制事故）：
  tModLoader 在**构建 / 加载**模组时会重写模组源目录里的本地化文件，而且只写回
  它认为"本次真正用到的键"。汉化补丁的键属于主模组前缀，所以被打包时源文件已经
  被削成了空壳（`// English` 占位注释）。

  流水线原来的第 4 步是「从源文件刷新备份 → 装机 → 从备份还原」——
  那一步发生在**加载自检之后**，于是它把"已经被削过的文件"存成了备份，
  下次 `--restore` 再把损坏的版本还原回来。备份变成了损坏的传播者。

现在的原则：
  * `sync_cn_translation.py` 里的译文表 + 主模组 en-US 文件 = **唯一权威**；
  * 快照必须在**任何构建 / 加载自检之前**拍（那时源文件还是刚生成的那一份）；
  * 本检查器负责证明"快照 == 权威输出、并且里面真的有汉字"。

判定：
  FAIL  : 备份缺失 / 备份与权威输出不一致 / 备份里汉字不足 1000
  NOTE  : 源文件与权威输出不一致（说明它被加载过程重写过；流水线末尾会重新生成）
"""
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import sync_cn_translation as sync

MIN_HAN = 1000


def han_count(text):
    return len(re.findall(u"[\u4e00-\u9fff]", text))


def normalize(text):
    return text.replace("\r\n", "\n").rstrip("\n")


def first_diff(expected, actual):
    """给出第一处差异的行号与内容，便于定位。"""
    a = normalize(expected).split("\n")
    b = normalize(actual).split("\n")

    for index in range(max(len(a), len(b))):
        left = a[index] if index < len(a) else "<文件结束>"
        right = b[index] if index < len(b) else "<文件结束>"

        if left != right:
            return index + 1, left.strip()[:70], right.strip()[:70]

    return 0, "", ""


def main():
    en_path = os.path.join(sync.MAIN_LOC, "en-US_Mods.WastelandSoul.hjson")
    cn_path = os.path.join(sync.CN_LOC, "zh-Hans_Mods.WastelandSoul.hjson")

    if not os.path.exists(en_path):
        print(u"!! 找不到主模组英文文件: %s" % en_path)
        return 1

    entries = sync.parse(en_path, sync.PREFIX_EN)
    expected, translated, missing = sync.build_cn(entries)

    print(u"权威输出: %d 键 / 已翻译 %d / 英文占位 %d" % (len(entries), translated, len(missing)))

    failed = []

    if not os.path.exists(sync.CN_BACKUP):
        print(u"!! 备份不存在: %s" % sync.CN_BACKUP)
        return 1

    backup = io.open(sync.CN_BACKUP, encoding="utf-8").read()
    backup_han = han_count(backup)

    if normalize(backup) != normalize(expected):
        line, left, right = first_diff(expected, backup)
        print(u"!! 备份与权威输出不一致（第 %d 行）" % line)
        print(u"   权威: %s" % left)
        print(u"   备份: %s" % right)
        failed.append("backup-stale")
    else:
        print(u"[OK] 备份 == 权威输出（%d 字节）" % os.path.getsize(sync.CN_BACKUP))

    if backup_han < MIN_HAN:
        print(u"!! 备份里只有 %d 个汉字（要求 >= %d）" % (backup_han, MIN_HAN))
        failed.append("backup-no-chinese")
    else:
        print(u"[OK] 备份含 %d 个汉字" % backup_han)

    if missing:
        print(u"[WARN] 仍有 %d 个键是英文占位（check_cn_parity 会单独报）" % len(missing))

    if os.path.exists(cn_path):
        source = io.open(cn_path, encoding="utf-8").read()

        if normalize(source) != normalize(expected):
            print(u"[NOTE] 源文件已被构建/加载重写（%d 字节），流水线末尾会重新生成——"
                  u"快照不受影响，这才是重点" % os.path.getsize(cn_path))
        else:
            print(u"[OK] 源文件也是干净的（%d 字节）" % os.path.getsize(cn_path))

    if failed:
        print(u"CHECK FAILED: " + ", ".join(failed))
        return 1

    print(u"CHECK OK: 快照干净且在任何加载自检之前就已拍好")
    return 0


if __name__ == "__main__":
    sys.exit(main())
