# -*- coding: utf-8 -*-
"""把四首 Boss 曲从 32kHz 立体声降采样到 22.05kHz 立体声，缩小包体（18.4MB -> ~9.5MB）。

为什么做得成、且零代码风险：
  * tModLoader 的 MusicLoader 明确支持 `.mp3/.ogg/.wav`（我早先用 Cecil 在 IL 里核对过字面量），
    所以**保持 .wav 扩展名**、只改采样率，代码里 `GetMusicSlot(Mod, "Music/Archivist")` 一行都不用动；
  * 本机**没有** ffmpeg、也没有 soundfile/pyogg（无 OGG 编码器），但 **numpy 可用**，
    所以用 numpy + 标准库 wave 自己做"线性插值降采样 + 保持立体声"。
  * 原文件先备份到 `.backup/music_orig/`，要还原直接拷回即可。

用法：python tools/shrink_music.py [--rate 22050] [--dry]
"""
import io
import os
import shutil
import sys
import wave

import numpy as np

MOD = r"E:\开发\WastelandSoul"
MUSIC = os.path.join(MOD, "Music")
BACKUP = os.path.join(os.path.dirname(MOD), ".backup", "music_orig")


def shrink(path, target_rate, dry=False):
    with wave.open(path, "rb") as handle:
        params = handle.getparams()
        frames = handle.readframes(params.nframes)
        rate = params.framerate
        channels = params.nchannels
        width = params.sampwidth

    if width != 2:
        print("  跳过（不是 16bit）：%s" % os.path.basename(path))
        return None

    if rate <= target_rate:
        print("  跳过（采样率已 <= 目标）：%s" % os.path.basename(path))
        return None

    data = np.frombuffer(frames, dtype="<i2").reshape(-1, channels).astype(np.float32)

    # 线性插值降采样（对合成音乐足够，且不会引入咔哒声）
    new_count = int(round(data.shape[0] * target_rate / float(rate)))
    index = np.linspace(0, data.shape[0] - 1, new_count)
    low = np.floor(index).astype(np.int64)
    high = np.minimum(low + 1, data.shape[0] - 1)
    frac = (index - low).astype(np.float32)[:, None]
    resampled = data[low] * (1.0 - frac) + data[high] * frac

    # 90% 音量留一点余量，避免插值过冲削顶
    resampled = np.clip(resampled * 0.9, -32768, 32767).astype("<i2")

    out = path + ".tmp"
    with wave.open(out, "wb") as handle:
        handle.setnchannels(channels)
        handle.setsampwidth(2)
        handle.setframerate(target_rate)
        handle.writeframes(resampled.tobytes())

    before = os.path.getsize(path)
    after = os.path.getsize(out)

    if dry:
        os.remove(out)
        return before, after

    os.makedirs(BACKUP, exist_ok=True)
    keep = os.path.join(BACKUP, os.path.basename(path))

    if not os.path.exists(keep):
        shutil.copyfile(path, keep)

    os.replace(out, path)
    return before, after


def main():
    args = sys.argv[1:]
    target = 22050

    if "--rate" in args:
        target = int(args[args.index("--rate") + 1])

    dry = "--dry" in args

    files = sorted(f for f in os.listdir(MUSIC) if f.lower().endswith(".wav"))
    print("目标采样率 %d Hz，共 %d 个音乐文件%s" % (target, len(files), "（试运行，不写盘）" if dry else ""))

    total_before = total_after = 0

    for name in files:
        path = os.path.join(MUSIC, name)
        result = shrink(path, target, dry)

        if result is None:
            continue

        before, after = result
        total_before += before
        total_after += after
        print("  %-28s %6.2f MB -> %6.2f MB" % (name, before / 1048576.0, after / 1048576.0))

    if total_before:
        print("合计 %.2f MB -> %.2f MB（省 %.0f%%）" % (
            total_before / 1048576.0, total_after / 1048576.0,
            (1 - total_after / float(total_before)) * 100))

    if not dry:
        print("原文件备份在 %s（要还原直接拷回 Music/）" % BACKUP)


if __name__ == "__main__":
    main()
