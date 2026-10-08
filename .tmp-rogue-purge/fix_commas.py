import io, re

P = r'E:\开发\tools\sync_cn_translation.py'
lines = io.open(P, encoding='utf-8').read().split('\n')

TARGETS = {
    'ScavengerGraceHood.Tooltip',
    'ScavengerChip.Tooltip',
    'ReinforcedScavengerChip.Tooltip',
    'WastelandOverlordCore.Tooltip',
}

fixed = []
for i, l in enumerate(lines):
    st = l.strip()
    m = re.match(r'^"([^"]+)"\s*:', st)
    if m and m.group(1) in TARGETS and not st.endswith(','):
        # find the last non-space char of the whole line
        stripped_right = l.rstrip()
        if not stripped_right.endswith(','):
            indent_len = len(l) - len(l.lstrip())
            lines[i] = '    ' + st + ','
            fixed.append(m.group(1))

io.open(P, 'w', encoding='utf-8', newline='\n').write('\n'.join(lines))
print('comma-fixed lines:', fixed)
