import io, re
EN = r'E:\开发\WastelandSoul\Localization\en-US_Mods.WastelandSoul.hjson'
CN = r'E:\开发\WastelandSoulCN\Localization\zh-Hans_Mods.WastelandSoul.hjson'
txt = io.open(EN, encoding='utf-8').read()
cn = io.open(CN, encoding='utf-8').read()

def grab(text, key):
    m = re.search(r'^\t' + re.escape(key) + r'\s*:\s*\{\s*\n\t\tDisplayName:\s*(.+)$', text, re.M)
    return m.group(1).strip() if m else '<MISSING>'

bosses = [('Boss1 清道夫', 'Scavenger'), ('Boss2 归档者', 'Archivist'),
          ('Boss3 灰烬之心', 'AshHeart'), ('Boss4 壁炉守卫', 'Fireplace')]
classes = [('战士', 'Warrior'), ('射手', 'Ranger'), ('法师', 'Mage'), ('召唤', 'Summoner')]

out = []
for label, pre in bosses:
    out.append('=== %s ===' % label)
    for ccn, c in classes:
        for suf in ('Weapon', 'WeaponEX', 'WeaponEx'):
            k = pre + c + suf
            if grab(txt, k) != '<MISSING>':
                out.append('  %-4s %-38s CN=%s' % (ccn, k, grab(cn, k)))
                break
    out.append('')

io.open(r'E:\开发\.tmp-rogue-purge\boss_names.txt', 'w', encoding='utf-8').write('\n'.join(out))
print('written')
