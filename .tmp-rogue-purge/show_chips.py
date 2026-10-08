import io
P = r'E:\开发\tools\sync_cn_translation.py'
lines = io.open(P, encoding='utf-8').read().split('\n')
want = ('ScavengerChip.', 'WastelandOverlordCore.', 'ScavengerGraceHood.', 'ScavengerGraceJacket.', 'ReinforcedScavengerChip.')
for i, l in enumerate(lines, 1):
    if any(w in l for w in want):
        print(i, l.strip())
