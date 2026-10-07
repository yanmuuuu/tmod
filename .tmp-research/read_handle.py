import os

path = r"E:\开发\.tmp-research\inno_src\VaultLoadenHandle.cs"
raw = open(path, "rb").read()
for enc in ("utf-8", "utf-8-sig", "gb18030", "utf-16"):
    try:
        text = raw.decode(enc)
        print("### decoded with", enc, "len", len(text))
        break
    except Exception:
        continue

lines = text.splitlines()
for i, line in enumerate(lines[160:200], start=161):
    print("%4d| %s" % (i, line))
