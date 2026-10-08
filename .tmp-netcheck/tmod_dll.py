import os
import sys

sys.path.insert(0, r'E:\开发\tools')
from tmod_lib import read_tmod  # noqa: E402

tmod = os.path.expandvars(r'%USERPROFILE%\Documents\My Games\Terraria\tModLoader\Mods\WastelandSoul.tmod')
entries = read_tmod(tmod)
names = [n for n in entries if n.endswith('.dll') or n.endswith('.pdb')]
print('entries:', names[:10], 'total', len(entries))
out = r'E:\开发\.tmp-netcheck\tmod_extract'
os.makedirs(out, exist_ok=True)
for n in names:
    dest = os.path.join(out, os.path.basename(n))
    with open(dest, 'wb') as fh:
        fh.write(entries[n])
    print('wrote', dest, len(entries[n]))
