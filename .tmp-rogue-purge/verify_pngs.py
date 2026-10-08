import os, json

ROOT = r'E:\开发\WastelandSoul'

# the 43 intended rogue PNGs
INTENDED = set(x.replace('/', '\\') for x in [
 'Content/Items/Accessories/ScavengerRogueCharm.png','Content/Items/Accessories/ArchivistRogueCharm.png','Content/Items/Accessories/AshHeartRogueCharm.png','Content/Items/Accessories/FireplaceRogueCharm.png',
 'Content/Items/Armor/SalvagedSteelRogueMask.png','Content/Items/Armor/SalvagedSteelRogueMask_Head.png','Content/Items/Armor/SalvagedSteelRogueVest.png','Content/Items/Armor/SalvagedSteelRogueVest_Body.png','Content/Items/Armor/SalvagedSteelRogueLeggings.png','Content/Items/Armor/SalvagedSteelRogueLeggings_Legs.png',
 'Content/Items/Weapons/Boss1Scavenger/ScavengerRogueWeapon.png','Content/Items/Weapons/Boss1Scavenger/ScavengerRogueWeaponEX.png',
 'Content/Items/Weapons/Boss2Archivist/ArchivistRogueWeapon.png','Content/Items/Weapons/Boss2Archivist/ArchivistRogueWeaponEX.png',
 'Content/Items/Weapons/Boss3AshHeart/AshHeartRogueWeapon.png','Content/Items/Weapons/Boss3AshHeart/AshHeartRogueWeaponEx.png',
 'Content/Items/Weapons/Boss4Fireplace/FireplaceRogueWeapon.png','Content/Items/Weapons/Boss4Fireplace/FireplaceRogueWeaponEx.png',
 'Content/Items/Weapons/CLine/ScavengerCRogue.png','Content/Items/Weapons/CLine/ArchivistCRogue.png','Content/Items/Weapons/CLine/AshHeartCRogue.png','Content/Items/Weapons/CLine/FireplaceCRogue.png',
 'Content/Items/Weapons/Rust/RustShuriken.png','Content/Items/Weapons/Rust/RustShurikenEX.png',
 'Content/Items/Weapons/Scrap/ScrapChakram.png',
 'Content/Items/UpgradeTrees/RustedGearShuriken.png','Content/Items/UpgradeTrees/SalvagedSteelChakram.png','Content/Items/UpgradeTrees/AshHeartCinderRing.png',
 'Content/Projectiles/Scavenger/ScavengerCaltrop.png','Content/Projectiles/Scavenger/ScavengerCaltropEX.png',
 'Content/Projectiles/Archivist/ArchivistBoneBoomerang.png','Content/Projectiles/Archivist/ArchivistBoneBoomerangEX.png',
 'Content/Projectiles/AshHeart/AshHeartRogueProjectile.png','Content/Projectiles/AshHeart/AshHeartRogueProjectileEX.png',
 'Content/Projectiles/Fireplace/FireplaceRogueProjectile.png','Content/Projectiles/Fireplace/FireplaceRogueProjectileEX.png',
 'Content/Projectiles/Rust/RustShurikenProj.png','Content/Projectiles/Rust/RustShurikenProjEX.png',
 'Content/Projectiles/Scrap/ScrapChakramProj.png',
 'Content/Projectiles/UpgradeTrees/RustedGearBoomerang.png','Content/Projectiles/UpgradeTrees/SalvagedSteelChakramProj.png','Content/Projectiles/UpgradeTrees/ChakramShatterBlast.png','Content/Projectiles/UpgradeTrees/CinderRingEmber.png',
])

base = json.load(open(r'E:\开发\.tmp-rogue-purge\inventory.json', encoding='utf-8'))
original = set(p.replace('/', '\\') for p in base['pngs'])

now = set()
for root, dirs, files in os.walk(ROOT):
    for f in files:
        if f.endswith('.png'):
            now.add(os.path.relpath(os.path.join(root, f), ROOT))

gone = original - now
added = now - original

print('original png count :', len(original))
print('current  png count :', len(now))
print()
print('=== DELETED ===')
for p in sorted(gone):
    print('   ', p, '' if p in INTENDED else '   UNINTENDED!')
print()
print('=== INTENDED BUT STILL PRESENT ===')
survivors = sorted(INTENDED & now)
for p in survivors:
    print('   ', p)
if not survivors:
    print('    (none)')
print()
print('=== UNEXPECTEDLY ADDED ===')
for p in sorted(added):
    print('   ', p)
if not added:
    print('    (none)')
