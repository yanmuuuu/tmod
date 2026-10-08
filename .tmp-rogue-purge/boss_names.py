import io, re
EN = r'E:\开发\WastelandSoul\Localization\en-US_Mods.WastelandSoul.hjson'
txt = io.open(EN, encoding='utf-8').read()

KEYS = [
 'ScavengerWarriorWeapon','ScavengerWarriorWeaponEX','ScavengerMageWeapon','ScavengerMageWeaponEX',
 'ScavengerRangerWeapon','ScavengerRangerWeaponEX','ScavengerSummonerWeapon','ScavengerSummonerWeaponEX',
 'ArchivistWarriorWeapon','ArchivistWarriorWeaponEX','ArchivistMageWeapon','ArchivistMageWeaponEX',
 'ArchivistRangerWeapon','ArchivistRangerWeaponEX','ArchivistSummonerWeapon','ArchivistSummonerWeaponEX',
 'AshHeartWarriorWeapon','AshHeartWarriorWeaponEx','AshHeartMageWeapon','AshHeartMageWeaponEx',
 'AshHeartRangerWeapon','AshHeartRangerWeaponEx','AshHeartSummonerWeapon','AshHeartSummonerWeaponEx',
 'FireplaceWarriorWeapon','FireplaceWarriorWeaponEx','FireplaceMageWeapon','FireplaceMageWeaponEx',
 'FireplaceRangerWeapon','FireplaceRangerWeaponEx','FireplaceSummonerWeapon','FireplaceSummonerWeaponEx',
]
# also grab zh
CN = r'E:\开发\WastelandSoulCN\Localization\zh-Hans_Mods.WastelandSoul.hjson'
cn = io.open(CN, encoding='utf-8').read()

def grab(text, key):
    m = re.search(r'^\t' + re.escape(key) + r'\s*:\s*\{\s*\n\t\tDisplayName:\s*(.+)$', text, re.M)
    return m.group(1).strip() if m else '<MISSING>'

bosses = [('Boss1 清道夫 Scavenger', 'Scavenger'), ('Boss2 归档者 Archivist', 'Archivist'),
          ('Boss3 灰烬之心 AshHeart', 'AshHeart'), ('Boss4 壁炉守卫 Fireplace', 'Fireplace')]
classes = ['Warrior', 'Mage', 'Ranger', 'Summoner']

for label, pre in bosses:
    print('===', label, '===')
    for c in classes:
        # A-line and EX-line key naming differs only in suffix case for some bosses
        a = pre + c + 'Weapon'
        e = pre + c + 'WeaponEX'
        if grab(txt, e) == '<MISSING>':
            e = pre + c + 'WeaponEx'
        print('   %-9s A=%-34s  EN=%-32s | CN=%s' % (c, a, grab(txt, a), grab(cn, a)))
        print('   %-9s B=%-34s  EN=%-32s | CN=%s' % ('', e, grab(txt, e), grab(cn, e)))
