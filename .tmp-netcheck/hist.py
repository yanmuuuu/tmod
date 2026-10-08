import os
import sys

sys.path.insert(0, r'E:\开发\tools')
from tmod_lib import read_tmod  # noqa: E402

cands = []
for d in os.listdir(r'E:\开发'):
    if d.startswith('.tml-build') or d.startswith('.tml-loadtest'):
        p = os.path.join(r'E:\开发', d, 'Mods', 'WastelandSoul.tmod')
        if os.path.isfile(p):
            cands.append(p)
cands.sort(key=os.path.getmtime)

outroot = r'E:\开发\.tmp-netcheck\hist'
os.makedirs(outroot, exist_ok=True)
for p in cands:
    stamp = os.path.getmtime(p)
    import datetime
    name = datetime.datetime.fromtimestamp(stamp).strftime('%H%M%S') + '_' + os.path.basename(os.path.dirname(os.path.dirname(p)))
    try:
        entries = read_tmod(p)
        dll = [n for n in entries if n.endswith('.dll')]
        if not dll:
            continue
        dest = os.path.join(outroot, name.replace(':', '_') + '.dll')
        with open(dest, 'wb') as fh:
            fh.write(entries[dll[0]])
        print('%-40s size=%d -> %s' % (name, len(entries[dll[0]]), os.path.basename(dest)))
    except Exception as exc:
        print(name, 'ERR', exc)
