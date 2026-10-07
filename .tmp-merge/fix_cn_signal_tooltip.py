# -*- coding: utf-8 -*-
"""合并 D 线后，它的译文表把 `ScavengerSignalSensor` 的制作说明又带回来了 —— 去掉。"""
import io

PATH = r"E:\开发\tools\sync_cn_translation.py"
OLD = u'"ScavengerSignalSensor.Tooltip": "向废土广播一段伪造的执行指令，把清道夫引到你身边\\n在铁砧用 5 个精钢制作\\n使用后不消耗，但同一时间只允许存在一只清道夫",'
NEW = u'"ScavengerSignalSensor.Tooltip": "向废土广播一段伪造的执行指令，把清道夫引到你身边\\n使用后不消耗，但同一时间只允许存在一只清道夫",'

text = io.open(PATH, encoding="utf-8").read()

if OLD not in text:
    print("没找到那条（可能已经清了）：%s" % ("已清理" if u"在铁砧用 5 个精钢制作" not in text else "格式对不上"))
else:
    io.open(PATH, "w", encoding="utf-8", newline="\n").write(text.replace(OLD, NEW, 1))
    print("已去掉 ScavengerSignalSensor 的制作说明")
