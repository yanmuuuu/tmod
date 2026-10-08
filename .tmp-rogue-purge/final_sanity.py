import io, os, re, sys
sys.path.insert(0, r'E:\开发\tools')
from check_localization import parse_keys  # noqa

MOD = r'E:\开发\WastelandSoul'

# 1) brace balance per .cs file
bad = []
for root, dirs, files in os.walk(MOD):
    dirs[:] = [d for d in dirs if d not in ('obj', 'bin')]
    for f in files:
        if not f.endswith('.cs'):
            continue
        p = os.path.join(root, f)
        src = io.open(p, encoding='utf-8').read()
        # strip strings / comments crudely for a balance estimate
        s = re.sub(r'//[^\n]*', '', src)
        s = re.sub(r'/\*.*?\*/', '', s, flags=re.S)
        s = re.sub(r'@"(?:[^"]|"")*"', '""', s)
        s = re.sub(r'"(?:\\.|[^"\\])*"', '""', s)
        s = re.sub(r"'(?:\\.|[^'\\])'", "''", s)
        if s.count('{') != s.count('}'):
            bad.append((os.path.relpath(p, MOD), s.count('{'), s.count('}')))

print('=== BRACE MISMATCH FILES ===')
for b in bad:
    print('  ', b)
if not bad:
    print('   (none)')

# 2) hjson sections well-formed (parse_keys already tolerant) -> just count
EN = os.path.join(MOD, 'Localization', 'en-US_Mods.WastelandSoul.hjson')
keys = parse_keys(io.open(EN, encoding='utf-8').read(), 'Mods.WastelandSoul')
print()
print('en-US keys:', len(keys))
roguey = [k for k in keys if re.search(r'Rogue|Throwing|Chakram|Shuriken|Boomerang|Caltrop|CinderRing|ShatterBlast', k)]
print('rogue-ish keys left:', roguey if roguey else '(none)')
