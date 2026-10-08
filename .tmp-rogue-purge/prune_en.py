import io, os, re

EN = r'E:\开发\WastelandSoul\Localization\en-US_Mods.WastelandSoul.hjson'

# top-level key names whose whole `Name: { ... }` block must go
DROP_BLOCKS = [
    'SalvagedSteelRogueMask', 'SalvagedSteelRogueVest', 'SalvagedSteelRogueLeggings',
    'ScavengerRogueWeapon', 'ScavengerRogueWeaponEX',
    'ArchivistRogueWeapon', 'ArchivistRogueWeaponEX',
    'AshHeartRogueWeapon', 'AshHeartRogueWeaponEx',
    'FireplaceRogueWeapon', 'FireplaceRogueWeaponEx',
    'ScavengerRogueCharm', 'ArchivistRogueCharm', 'AshHeartRogueCharm', 'FireplaceRogueCharm',
    'ScavengerCRogue', 'ArchivistCRogue', 'AshHeartCRogue', 'FireplaceCRogue',
    'RustShuriken', 'RustShurikenEX', 'ScrapChakram',
    'RustedGearShuriken', 'SalvagedSteelChakram', 'AshHeartCinderRing',
]

# flat `Name.Suffix: value` lines whose class was deleted
DROP_FLAT = [
    'ArchivistBoneBoomerang.DisplayName', 'ArchivistBoneBoomerangEX.DisplayName',
    'ScavengerCaltrop.DisplayName', 'ScavengerCaltropEX.DisplayName',
    'AshHeartRogueProjectile.DisplayName', 'AshHeartRogueProjectileEX.DisplayName',
    'FireplaceRogueProjectile.DisplayName', 'FireplaceRogueProjectileEX.DisplayName',
    'RustShurikenProj.DisplayName', 'RustShurikenProjEX.DisplayName',
    'ScrapChakramProj.DisplayName',
    'RustedGearBoomerang.DisplayName', 'SalvagedSteelChakramProj.DisplayName',
    'ChakramShatterBlast.DisplayName', 'CinderRingEmber.DisplayName',
]

lines = io.open(EN, encoding='utf-8').read().splitlines(True)
out = []
removed_blocks = []
removed_flat = []
i = 0
n = len(lines)

block_pat = re.compile(r'^\t([A-Za-z0-9_]+)\s*:\s*\{\s*$')
flat_pat = re.compile(r'^\t([A-Za-z0-9_.]+)\s*:\s*(.*)$')

while i < n:
    line = lines[i]
    m = block_pat.match(line.rstrip('\n'))
    if m and m.group(1) in DROP_BLOCKS:
        name = m.group(1)
        removed_blocks.append(name)
        i += 1
        # eat until the matching closing brace at tab depth 1
        while i < n:
            stripped = lines[i].strip()
            if stripped == '}':
                i += 1
                break
            i += 1
        # eat one following blank line
        if i < n and lines[i].strip() == '':
            i += 1
        continue

    m2 = flat_pat.match(line.rstrip('\n'))
    if m2 and m2.group(1) in DROP_FLAT:
        removed_flat.append(m2.group(1))
        i += 1
        continue

    out.append(line)
    i += 1

io.open(EN, 'w', encoding='utf-8', newline='').write(''.join(out))

print('blocks removed:', len(removed_blocks))
for b in removed_blocks:
    print('   -', b)
print('flat keys removed:', len(removed_flat))
for f in removed_flat:
    print('   -', f)
