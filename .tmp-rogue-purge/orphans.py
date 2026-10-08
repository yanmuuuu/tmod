import os, re, sys, io
sys.path.insert(0, r'E:\开发\tools')
from check_localization import parse_keys  # noqa

MOD = r'E:\开发\WastelandSoul'
EN = os.path.join(MOD, 'Localization', 'en-US_Mods.WastelandSoul.hjson')
PREFIX = 'Mods.WastelandSoul'

# ---- all declared type names in the mod ----
cls_re = re.compile(r'^\s*(?:public|internal)\s+(?:abstract\s+|static\s+|sealed\s+)*(?:class|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)', re.M)
declared = set()
for root, dirs, files in os.walk(MOD):
    dirs[:] = [d for d in dirs if d not in ('obj', 'bin')]
    for f in files:
        if f.endswith('.cs'):
            txt = io.open(os.path.join(root, f), encoding='utf-8').read()
            declared.update(cls_re.findall(txt))

keys = parse_keys(io.open(EN, encoding='utf-8').read(), PREFIX)

AUTO = ('.DisplayName', '.Tooltip', '.Description', '.MapEntry', '.SetBonus')

orphans = []
for k in sorted(keys):
    short = k[len(PREFIX) + 1:]
    # strip known section prefixes
    cand = short
    for sec in ('Items.', 'Projectiles.', 'Buffs.', 'Tiles.', 'NPCs.'):
        if cand.startswith(sec):
            cand = cand[len(sec):]
            break
    name = cand.split('.')[0]
    if not name:
        continue
    if name in declared:
        continue
    # keys under Configs / BossChecklist / Messages / Dialogue / Conditions are not type keys
    if short.startswith(('Configs.', 'BossChecklist.', 'Messages.', 'Dialogue.', 'Conditions.')):
        continue
    orphans.append((k, keys[k]))

print('=== ORPHAN LOCALIZATION KEYS (no such type) ===')
for k, v in orphans:
    print('  ', k, '=', v[:70])
if not orphans:
    print('   (none)')

# ---- orphan PNGs ----
print()
print('=== ORPHAN PNGs (no such declared type) ===')
orphan_png = []
for root, dirs, files in os.walk(MOD):
    dirs[:] = [d for d in dirs if d not in ('obj', 'bin')]
    for f in files:
        if not f.endswith('.png'):
            continue
        stem = f[:-4]
        base = stem
        for suffix in ('_Head', '_Body', '_Legs', '_Head_Boss', '_Arms'):
            if base.endswith(suffix):
                base = base[:-len(suffix)]
                break
        if base in declared:
            continue
        orphan_png.append(os.path.relpath(os.path.join(root, f), MOD))

for p in sorted(orphan_png):
    print('  ', p)
if not orphan_png:
    print('   (none)')
print()
print('total keys:', len(keys), ' declared types:', len(declared), ' pngs:', sum(1 for r,d,fs in os.walk(MOD) for f in fs if f.endswith('.png')))
