import io, re

P = r'E:\开发\tools\sync_cn_translation.py'
raw = io.open(P, encoding='utf-8').read()
lines = raw.split('\n')

DROP = {'ScrapGrenade.DisplayName', 'ScrapGrenade.Tooltip',
        'ScrapGrenadeProj.DisplayName', 'ScrapGrenadeBlast.DisplayName'}
keyline = re.compile(r'^\s*"([^"]+)"\s*:')
kept, removed = [], []
for l in lines:
    m = keyline.match(l)
    if m and m.group(1) in DROP:
        removed.append(m.group(1))
        continue
    kept.append(l)

io.open(P, 'w', encoding='utf-8', newline='\n').write('\n'.join(kept))
print('sync keys removed:', removed)
