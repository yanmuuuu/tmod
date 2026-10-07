"""四首 Boss 曲：各自的速度、调式和配器，立体声，结尾交叉淡化以便循环。"""
import os
import wave

import numpy as np

RATE = 32000
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "WastelandSoul", "Music")


def hz(midi):
    return 440.0 * (2.0 ** ((midi - 69) / 12.0))


def adsr(length, attack, decay, sustain, release):
    n = length
    a = max(1, int(attack * n))
    d = max(1, int(decay * n))
    r = max(1, int(release * n))
    s = max(1, n - a - d - r)
    env = np.concatenate([
        np.linspace(0.0, 1.0, a, endpoint=False),
        np.linspace(1.0, sustain, d, endpoint=False),
        np.full(s, sustain),
        np.linspace(sustain, 0.0, r, endpoint=False),
    ])
    if len(env) < n:
        env = np.pad(env, (0, n - len(env)))
    return env[:n]


def voice(kind, freq, n, dur):
    t = np.arange(n) / RATE
    w = 2.0 * np.pi * freq
    if kind == "bass":
        wave_l = np.tanh(2.4 * np.sin(w * t)) * 0.8 + 0.25 * np.sin(w * 0.5 * t)
        return wave_l * adsr(n, 0.01, 0.12, 0.55, 0.18)
    if kind == "pad":
        wave_l = (
            np.sin(w * t)
            + 0.45 * np.sin(w * 1.003 * t)
            + 0.22 * np.sin(w * 2.0 * t)
        )
        return wave_l * adsr(n, 0.18, 0.2, 0.7, 0.35)
    if kind == "bell":
        mod = np.sin(w * 2.71 * t) * np.exp(-t * 5.5)
        wave_l = np.sin(w * t + mod * 2.2) * np.exp(-t * 2.4)
        return wave_l
    if kind == "pluck":
        wave_l = np.sin(w * t) + 0.35 * np.sin(w * 2 * t) + 0.12 * np.sin(w * 3 * t)
        return wave_l * np.exp(-t * (6.0 / max(dur, 0.05)))
    if kind == "lead":
        wave_l = np.tanh(1.6 * np.sin(w * t)) + 0.25 * np.sin(w * 2 * t)
        return wave_l * adsr(n, 0.02, 0.08, 0.62, 0.12)
    if kind == "kick":
        freq_t = 150.0 * np.exp(-t * 26.0) + 42.0
        phase = np.cumsum(freq_t) / RATE * 2.0 * np.pi
        return np.sin(phase) * np.exp(-t * 7.5)
    if kind == "snare":
        noise = np.random.uniform(-1.0, 1.0, n)
        tone = np.sin(2.0 * np.pi * 190.0 * t) * np.exp(-t * 28.0)
        return (noise * 0.65 + tone) * np.exp(-t * 16.0)
    if kind == "hat":
        noise = np.random.uniform(-1.0, 1.0, n)
        noise = noise - np.convolve(noise, np.ones(8) / 8.0, mode="same")
        return noise * np.exp(-t * 46.0)
    wave_l = np.sin(w * t)
    return wave_l * adsr(n, 0.02, 0.1, 0.5, 0.1)


def mix(buf, start, dur, midi, amp, kind, pan):
    if dur <= 0.02:
        return
    i0 = int(start * RATE)
    n = int(dur * RATE)
    if i0 >= len(buf) or n <= 8:
        return
    n = min(n, len(buf) - i0)
    sample = voice(kind, hz(midi) if midi else 80.0, n, dur) * amp
    left = np.clip(1.0 - pan, 0.0, 1.0)
    right = np.clip(1.0 + pan, 0.0, 1.0)
    # pan -1..1, keep both channels audible
    gain_l = 0.5 * (1.0 - pan)
    gain_r = 0.5 * (1.0 + pan)
    buf[i0:i0 + n, 0] += sample * gain_l
    buf[i0:i0 + n, 1] += sample * gain_r
    del left, right


def loopify(stereo, fade):
    n = int(fade * RATE)
    w = np.linspace(0.0, 1.0, n)[:, None]
    head = stereo[:n] * w + stereo[-n:] * (1.0 - w)
    return np.concatenate([head, stereo[n:-n]], axis=0)


def render(bpm, bars, events, fade=0.4):
    seconds = bars * 4.0 * 60.0 / bpm + 1.2
    buf = np.zeros((int(seconds * RATE), 2), dtype=np.float64)
    beat = 60.0 / bpm
    for start_beat, length_beats, midi, amp, kind, pan in events:
        mix(buf, start_beat * beat, length_beats * beat, midi, amp, kind, pan)
    peak = np.max(np.abs(buf))
    if peak > 1e-6:
        buf *= 0.86 / peak
    buf = loopify(buf, fade)
    return np.clip(buf, -1.0, 1.0)


def drums(bpm_events, bars, style):
    events = []
    for bar in range(bars):
        base = bar * 4
        if style == "drive":
            for step in range(8):
                events.append((base + step * 0.5, 0.12, 36, 0.34 if step % 2 == 0 else 0.12, "kick" if step % 2 == 0 else "hat", 0.15))
            events.append((base + 1, 0.16, 40, 0.22, "snare", -0.05))
            events.append((base + 3, 0.16, 40, 0.22, "snare", -0.05))
        elif style == "clock":
            for step in range(4):
                events.append((base + step, 0.05, 90, 0.08, "hat", 0.35))
            if bar % 2 == 0:
                events.append((base, 0.4, 36, 0.12, "kick", 0.0))
        elif style == "pulse":
            for step in range(4):
                events.append((base + step, 0.18, 36, 0.28, "kick", 0.0))
                events.append((base + step + 0.5, 0.06, 90, 0.1, "hat", 0.4))
            events.append((base + 2, 0.14, 40, 0.16, "snare", 0.1))
        else:
            events.append((base, 0.55, 36, 0.22, "kick", 0.0))
            events.append((base + 2, 0.2, 40, 0.1, "snare", 0.05))
            for step in range(4):
                events.append((base + step + 0.5, 0.05, 90, 0.05, "hat", 0.25))
    return events


def chord(events, beat, dur, root, intervals, amp, kind, pan):
    for interval in intervals:
        events.append((beat, dur, root + interval, amp, kind, pan))


def scavenger():
    bars = 16
    events = drums(None, bars, "drive")
    progression = [50, 50, 46, 45]  # D2, D2, Bb1, A1
    for bar in range(bars):
        root = progression[bar % 4]
        chord(events, bar * 4, 3.6, root, (0, 7), 0.22, "bass", 0.0)
        chord(events, bar * 4, 3.8, root + 12, (0, 3, 7), 0.05, "pad", 0.35)
    melody = [62, 65, 69, 65, 62, 60, 69, 65, 58, 57, 65, 62, 69, 67, 65, 64]
    beat = 0.0
    index = 0
    while beat < bars * 4 - 1:
        events.append((beat, 0.85, melody[index % len(melody)] + (12 if index % 32 >= 16 else 0), 0.16, "lead", -0.25))
        beat += 1.0
        index += 1
    for bar in range(0, bars, 4):
        events.append((bar * 4, 0.3, 86, 0.07, "bell", 0.45))
    return render(104, bars, events)


def archivist():
    bars = 12
    events = drums(None, bars, "clock")
    progression = [57, 50, 52, 57, 53, 50, 56, 57]  # A2 Dm Em Am F Dm E Am
    qualities = [(0, 3, 7), (0, 3, 7), (0, 3, 7), (0, 3, 7), (0, 4, 7), (0, 3, 7), (0, 4, 7), (0, 3, 7)]
    for bar in range(bars):
        root = progression[bar % 8]
        chord(events, bar * 4, 3.8, root, (0, 7), 0.1, "bass", 0.0)
        chord(events, bar * 4, 4.0, root + 12, qualities[bar % 8], 0.07, "pad", 0.4)
        chord(events, bar * 4, 2.2, root + 24, qualities[bar % 8], 0.09, "bell", -0.15)
    melody = [81, 84, 88, 84, 81, 80, 76, 77, 80, 84, 88, 91, 88, 84, 81, 80]
    beat = 0.0
    index = 0
    while beat < bars * 4 - 1:
        events.append((beat, 0.7, melody[index % len(melody)], 0.13, "pluck", -0.35))
        beat += 0.75
        index += 1
    return render(76, bars, events)


def ash_heart():
    bars = 16
    events = drums(None, bars, "pulse")
    # E harmonic minor: Em Em C B
    progression = [52, 52, 48, 47]
    qualities = [(0, 3, 7), (0, 3, 7), (0, 4, 7), (0, 4, 7)]
    arp = (0, 3, 7, 12, 7, 3)
    for bar in range(bars):
        root = progression[bar % 4]
        quality = qualities[bar % 4]
        chord(events, bar * 4, 1.8, root, (0, 7), 0.2, "bass", 0.0)
        chord(events, bar * 4 + 2, 1.6, root, (0, 7), 0.16, "bass", 0.0)
        chord(events, bar * 4, 3.8, root + 12, quality, 0.045, "pad", 0.3)
        for step in range(8):
            interval = arp[step % len(arp)]
            events.append((bar * 4 + step * 0.5, 0.42, root + 24 + interval, 0.07, "pluck", 0.2 if step % 2 else -0.1))
    melody = [64, 67, 71, 72, 71, 67, 64, 63, 67, 71, 76, 75, 71, 67, 64, 62]
    beat = 0.0
    index = 0
    while beat < bars * 4 - 1:
        events.append((beat, 0.9, melody[index % len(melody)] + 12, 0.14, "lead", -0.2))
        beat += 1.0
        index += 1
    return render(128, bars, events)


def guardian():
    bars = 14
    events = drums(None, bars, "hymn")
    progression = [50, 53, 48, 50, 46, 53, 43, 50]
    qualities = [(0, 3, 7), (0, 4, 7), (0, 4, 7), (0, 3, 7), (0, 3, 7), (0, 4, 7), (0, 4, 7), (0, 3, 7)]
    for bar in range(bars):
        root = progression[bar % 8]
        chord(events, bar * 4, 3.9, root, (0, 7), 0.16, "bass", 0.0)
        chord(events, bar * 4, 4.0, root + 12, qualities[bar % 8], 0.08, "pad", 0.25)
        chord(events, bar * 4, 2.8, root + 24, qualities[bar % 8], 0.11, "bell", -0.2)
        events.append((bar * 4 + 2, 1.4, root + 36, 0.05, "bell", 0.45))
    melody = [62, 65, 69, 69, 67, 65, 64, 62, 65, 67, 69, 72, 69, 67, 65, 62]
    beat = 0.0
    index = 0
    while beat < bars * 4 - 1:
        events.append((beat, 0.95, melody[index % len(melody)] + 12, 0.15, "lead", -0.15))
        beat += 1.0
        index += 1
    return render(84, bars, events)


def save(name, stereo):
    path = os.path.join(OUT, name + ".wav")
    os.makedirs(OUT, exist_ok=True)
    pcm = (np.clip(stereo, -1.0, 1.0) * 32767.0).astype(np.int16)
    with wave.open(path, "w") as handle:
        handle.setnchannels(2)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        handle.writeframes(pcm.tobytes())
    mono = stereo.mean(axis=1)
    spec = np.abs(np.fft.rfft(mono))
    freqs = np.fft.rfftfreq(len(mono), 1.0 / RATE)
    total = spec.sum() + 1e-9
    low = spec[freqs < 180].sum() / total
    mid = spec[(freqs >= 180) & (freqs < 2000)].sum() / total
    high = spec[freqs >= 2000].sum() / total
    width = np.mean(np.abs(stereo[:, 0] - stereo[:, 1]))
    seconds = len(stereo) / RATE
    print("%s  %.1fs  low %.2f mid %.2f high %.2f  width %.3f  %s" % (
        name, seconds, low, mid, high, width, path))
    if low < 0.05 or mid < 0.15 or high < 0.02 or width < 0.01:
        raise SystemExit("配器太平: " + name)


def main():
    np.random.seed(7)
    save("Scavenger", scavenger())
    save("Archivist", archivist())
    save("AshHeart", ash_heart())
    save("FireplaceGuardian", guardian())


if __name__ == "__main__":
    main()
