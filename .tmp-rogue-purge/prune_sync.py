import io, re, shutil

P = r'E:\开发\tools\sync_cn_translation.py'
shutil.copyfile(P, r'E:\开发\.tmp-rogue-purge\sync_cn_translation.py.bak')

raw = io.open(P, encoding='utf-8').read()
lines = raw.split('\n')

# key-line removal: exact stripped key names whose class was deleted
DROP = set("""
SalvagedSteelRogueMask.DisplayName SalvagedSteelRogueMask.Tooltip
SalvagedSteelRogueVest.DisplayName SalvagedSteelRogueVest.SetBonus SalvagedSteelRogueVest.Tooltip
SalvagedSteelRogueLeggings.DisplayName SalvagedSteelRogueLeggings.Tooltip
ArchivistRogueWeapon.DisplayName ArchivistRogueWeapon.Tooltip
ArchivistRogueWeaponEX.DisplayName ArchivistRogueWeaponEX.Tooltip
ScavengerRogueWeapon.DisplayName ScavengerRogueWeapon.Tooltip
ScavengerRogueWeaponEX.DisplayName ScavengerRogueWeaponEX.Tooltip
AshHeartRogueWeapon.DisplayName AshHeartRogueWeapon.Tooltip
AshHeartRogueWeaponEx.DisplayName AshHeartRogueWeaponEx.Tooltip
FireplaceRogueWeapon.DisplayName FireplaceRogueWeapon.Tooltip
FireplaceRogueWeaponEx.DisplayName FireplaceRogueWeaponEx.Tooltip
ScavengerCaltrop.DisplayName ScavengerCaltropEX.DisplayName
ArchivistBoneBoomerang.DisplayName ArchivistBoneBoomerangEX.DisplayName
AshHeartRogueProjectile.DisplayName AshHeartRogueProjectileEX.DisplayName
FireplaceRogueProjectile.DisplayName FireplaceRogueProjectileEX.DisplayName
ScavengerRogueCharm.DisplayName ScavengerRogueCharm.Tooltip
ArchivistRogueCharm.DisplayName ArchivistRogueCharm.Tooltip
AshHeartRogueCharm.DisplayName AshHeartRogueCharm.Tooltip
FireplaceRogueCharm.DisplayName FireplaceRogueCharm.Tooltip
ScavengerCRogue.DisplayName ScavengerCRogue.Tooltip
ArchivistCRogue.DisplayName ArchivistCRogue.Tooltip
AshHeartCRogue.DisplayName AshHeartCRogue.Tooltip
FireplaceCRogue.DisplayName FireplaceCRogue.Tooltip
RustShuriken.DisplayName RustShuriken.Tooltip
RustShurikenEX.DisplayName RustShurikenEX.Tooltip
ScrapChakram.DisplayName ScrapChakram.Tooltip
RustedGearShuriken.DisplayName RustedGearShuriken.Tooltip
SalvagedSteelChakram.DisplayName SalvagedSteelChakram.Tooltip
AshHeartCinderRing.DisplayName AshHeartCinderRing.Tooltip
RustShurikenProj.DisplayName RustShurikenProjEX.DisplayName
ScrapChakramProj.DisplayName
RustedGearBoomerang.DisplayName SalvagedSteelChakramProj.DisplayName
ChakramShatterBlast.DisplayName AshHeartCinderRingProj.DisplayName CinderRingEmber.DisplayName
""".split())

keyline = re.compile(r'^\s*"([^"]+)"\s*:')
kept = []
removed = []
for l in lines:
    m = keyline.match(l)
    if m and m.group(1) in DROP:
        removed.append(m.group(1))
        continue
    kept.append(l)

raw2 = '\n'.join(kept)
print('removed key lines:', len(removed))
for r in removed:
    print('   -', r)

# composite tooltip rewrites (values only; Chinese text kept free of ASCII double quotes)
SUBS = [
    ('"ScavengerChip.Tooltip":',
     '"ScavengerChip.Tooltip": "召唤伤害 +8%，被丢下的物品和红心会从更远处飞向你"'),
    ('"ReinforcedScavengerChip.Tooltip":',
     '"ReinforcedScavengerChip.Tooltip": "召唤伤害 +12%，金币和魔力星也会飞向你，受到的伤害降低 5%"'),
    ('"WastelandOverlordCore.Tooltip":',
     '"WastelandOverlordCore.Tooltip": "召唤伤害 +15%，仆从栏位 +1，受到的伤害降低 8%，幸运也更高一点"'),
    ('"ScavengerGraceHood.Tooltip":',
     '"ScavengerGraceHood.Tooltip": "远程伤害 +5%"'),
]

out_lines = raw2.split('\n')
hit = []
for idx, l in enumerate(out_lines):
    st = l.strip()
    for prefix, replacement in SUBS:
        if st.startswith(prefix):
            out_lines[idx] = '    ' + replacement
            hit.append(prefix)
            break

raw3 = '\n'.join(out_lines)
missing = [p for p, _ in SUBS if p not in hit]
print('tooltip rewrites applied:', len(hit))
for h in hit:
    print('   ~', h)
if missing:
    print('TOOLTIP PREFIX NOT FOUND:')
    for m in missing:
        print('   !', m)

io.open(P, 'w', encoding='utf-8', newline='\n').write(raw3)
print('written', P)
