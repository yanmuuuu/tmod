import io, os, re, sys
sys.path.insert(0, r'E:\开发\tools')

MOD = r'E:\开发\WastelandSoul'
EN = os.path.join(MOD, 'Localization', 'en-US_Mods.WastelandSoul.hjson')

BLOCKS = ['ScrapGrenade', 'ScrapBuzzsaw']
FLAT = ['ScrapGrenadeProj.DisplayName', 'ScrapGrenadeBlast.DisplayName', 'ScrapBuzzsawProj.DisplayName']

lines = io.open(EN, encoding='utf-8').read().splitlines(True)
out, rb, rf = [], [], []
i, n = 0, len(lines)
bp = re.compile(r'^\t([A-Za-z0-9_]+)\s*:\s*\{\s*$')
fp = re.compile(r'^\t([A-Za-z0-9_.]+)\s*:\s*(.*)$')

while i < n:
    m = bp.match(lines[i].rstrip('\n'))
    if m and m.group(1) in BLOCKS:
        rb.append(m.group(1))
        i += 1
        while i < n and lines[i].strip() != '}':
            i += 1
        i += 1
        if i < n and lines[i].strip() == '':
            i += 1
        continue
    m2 = fp.match(lines[i].rstrip('\n'))
    if m2 and m2.group(1) in FLAT:
        rf.append(m2.group(1))
        i += 1
        continue
    out.append(lines[i])
    i += 1

io.open(EN, 'w', encoding='utf-8', newline='').write(''.join(out))
print('hjson blocks removed:', rb)
print('hjson flat removed:', rf)

# ---- PNGs that belonged only to those deleted classes ----
PNGS = [
    r'Content\Items\Weapons\Rust\ScrapGrenade.png',
    r'Content\Items\Weapons\Scrap\ScrapBuzzsaw.png',
    r'Content\Projectiles\Rust\ScrapGrenadeBlast.png',
    r'Content\Projectiles\Rust\ScrapGrenadeProj.png',
    r'Content\Projectiles\Scrap\ScrapBuzzsawProj.png',
]
for rel in PNGS:
    p = os.path.join(MOD, rel)
    if os.path.isfile(p):
        os.remove(p)
        print('png removed:', rel)
    else:
        print('png NOT FOUND:', rel)
