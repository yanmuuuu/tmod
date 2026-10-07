# -*- coding: utf-8 -*-
"""把 MemoryTextKey 里的字符串拼接换成完整字面量数组。

原因：静态校验器扫描 C# 源码里的本地化键时，会把 "…Memory" + stage 的前缀
当成一个真实键 `…Dialogue.MechanicalCompanion.Memory` 来查表，于是报"缺失"。
改成四个完整字面量后既通过校验，也更直观。
"""
import io

PATH = r"E:\开发\WastelandSoul\Common\Systems\WastelandMemorySystem.cs"

OLD = '''		/// <summary>某段记忆的本地化键（台词写在本地化文件里，方便改文案）。</summary>
		public static string MemoryTextKey(int stage)
		{
			return "Mods.WastelandSoul.Dialogue.MechanicalCompanion.Memory" + Utils.Clamp(stage, 1, TotalMemories);
		}'''

NEW = '''		/// <summary>四段记忆的本地化键（**完整字面量**，不要用字符串拼接——静态校验器会把前缀当成真键）。</summary>
		private static readonly string[] MemoryKeys = {
			"Mods.WastelandSoul.Dialogue.MechanicalCompanion.Memory1",
			"Mods.WastelandSoul.Dialogue.MechanicalCompanion.Memory2",
			"Mods.WastelandSoul.Dialogue.MechanicalCompanion.Memory3",
			"Mods.WastelandSoul.Dialogue.MechanicalCompanion.Memory4"
		};

		/// <summary>某段记忆的本地化键。</summary>
		public static string MemoryTextKey(int stage)
		{
			return MemoryKeys[Utils.Clamp(stage, 1, TotalMemories) - 1];
		}'''


def main():
    with io.open(PATH, encoding="utf-8") as handle:
        text = handle.read()

    if "MemoryKeys" in text:
        print("  已经是字面量数组，跳过")
        return

    if OLD not in text:
        print("!! 没找到目标片段，未修改。请手动检查 MemoryTextKey")
        return

    with io.open(PATH, "w", encoding="utf-8") as handle:
        handle.write(text.replace(OLD, NEW, 1))

    print("  已改为完整字面量数组")


if __name__ == "__main__":
    main()
