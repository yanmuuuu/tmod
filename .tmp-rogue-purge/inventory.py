import os, re, json

MOD = r'E:\开发\WastelandSoul'

cls_re = re.compile(r'^\s*(?:public|internal)\s+(?:abstract\s+|static\s+|sealed\s+)*(?:class|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)', re.M)

results = {'files': [], 'pngs': []}
for root, dirs, files in os.walk(MOD):
    for f in files:
        p = os.path.join(root, f)
        rel = os.path.relpath(p, MOD)
        if f.endswith('.cs'):
            with open(p, encoding='utf-8') as fh:
                txt = fh.read()
            names = cls_re.findall(txt)
            results['files'].append({'path': rel, 'classes': names, 'lines': txt.count(chr(10))})
        elif f.endswith('.png'):
            results['pngs'].append(rel)

with open(r'E:\开发\.tmp-rogue-purge\inventory.json', 'w', encoding='utf-8') as fh:
    json.dump(results, fh, ensure_ascii=False, indent=1)

print('cs files', len(results['files']), 'pngs', len(results['pngs']))
