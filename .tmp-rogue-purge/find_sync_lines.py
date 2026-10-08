import io

P = r'E:\开发\tools\sync_cn_translation.py'
lines = io.open(P, encoding='utf-8').read().splitlines()

import re
for i, l in enumerate(lines, 1):
    s = l.strip()
    if s.startswith('"') and ('Rogue' in s or 'Caltrop' in s or 'Chakram' in s or 'Shuriken' in s
                              or 'Boomerang' in s or 'CinderRing' in s or 'ShatterBlast' in s):
        print(i, '|', s)
