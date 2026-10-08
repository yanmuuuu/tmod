import json, re, os, sys

MOD = r'E:\开发\WastelandSoul'
inv = json.load(open(r'E:\开发\.tmp-rogue-purge\inventory.json', encoding='utf-8'))

# declared class name -> file
decl = {}
for f in inv['files']:
    for c in f['classes']:
        decl.setdefault(c, []).append(f['path'])

pngs = set(os.path.splitext(p)[0].replace('\\', '/') for p in inv['pngs'])

# candidate rogue tokens
TOK = re.compile(r'Rogue|Caltrop|Chakram|Shuriken|Boomerang|CinderRing|GearBoomerang|ShatterBlast|Flechette|ReinforcedScavengerChip|WastelandOverlordCore|ReinforcedFilterMask|RustedGear|SalvagedSteelChakram|AshHeartCinderRing|ScavengerChip', re.I)

cands = set()
for f in inv['files']:
    p = f['path']
    base = os.path.basename(p)
    hit = bool(TOK.search(base))
    for c in f['classes']:
        if TOK.search(c):
            hit = True
    if hit:
        cands.add(p)

print('=== CANDIDATE FILES ===')
for p in sorted(cands):
    print(' ', p)

# who references each declared class
allrefs = {}
for name in sorted(decl):
    pat = re.compile(r'\b' + re.escape(name) + r'\b')
    refs = []
    for f in inv['files']:
        with open(os.path.join(MOD, f['path']), encoding='utf-8') as fh:
            txt = fh.read()
        n = len(pat.findall(txt))
        if n:
            refs.append((f['path'], n))
    allrefs[name] = refs

print()
print('=== REFERENCE TABLE (rogue-ish class names) ===')
for name in sorted(decl):
    if not TOK.search(name):
        continue
    rs = allrefs[name]
    if len(rs) == 1 and rs[0][0] == decl[name][0] and rs[0][1] <= 2:
        print(f'{name:44s} ONLY-IN {decl[name][0]}')
    else:
        print(f'{name:44s} ' + '; '.join(f'{p}({n})' for p, n in rs))

# png matching rogue-ish names
print()
print('=== ROGUE-ISH PNGs ===')
for p in sorted(pngs):
    b = os.path.basename(p)
    if TOK.search(b):
        print(' ', p)
