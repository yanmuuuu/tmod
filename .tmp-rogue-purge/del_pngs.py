import os

ROOT = r'E:\开发\WastelandSoul'

PNGS = [
    r'Content\Items\Accessories\ScavengerRogueCharm.png',
    r'Content\Items\Accessories\ArchivistRogueCharm.png',
    r'Content\Items\Accessories\AshHeartRogueCharm.png',
    r'Content\Items\Accessories\FireplaceRogueCharm.png',
    r'Content\Items\Armor\SalvagedSteelRogueMask.png',
    r'Content\Items\Armor\SalvagedSteelRogueMask_Head.png',
    r'Content\Items\Armor\SalvagedSteelRogueVest.png',
    r'Content\Items\Armor\SalvagedSteelRogueVest_Body.png',
    r'Content\Items\Armor\SalvagedSteelRogueLeggings.png',
    r'Content\Items\Armor\SalvagedSteelRogueLeggings_Legs.png',
    r'Content\Items\Weapons\Boss1Scavenger\ScavengerRogueWeapon.png',
    r'Content\Items\Weapons\Boss1Scavenger\ScavengerRogueWeaponEX.png',
    r'Content\Items\Weapons\Boss2Archivist\ArchivistRogueWeapon.png',
    r'Content\Items\Weapons\Boss2Archivist\ArchivistRogueWeaponEX.png',
    r'Content\Items\Weapons\Boss3AshHeart\AshHeartRogueWeapon.png',
    r'Content\Items\Weapons\Boss3AshHeart\AshHeartRogueWeaponEx.png',
    r'Content\Items\Weapons\Boss4Fireplace\FireplaceRogueWeapon.png',
    r'Content\Items\Weapons\Boss4Fireplace\FireplaceRogueWeaponEx.png',
    r'Content\Items\Weapons\CLine\ScavengerCRogue.png',
    r'Content\Items\Weapons\CLine\ArchivistCRogue.png',
    r'Content\Items\Weapons\CLine\AshHeartCRogue.png',
    r'Content\Items\Weapons\CLine\FireplaceCRogue.png',
    r'Content\Items\Weapons\Rust\RustShuriken.png',
    r'Content\Items\Weapons\Rust\RustShurikenEX.png',
    r'Content\Items\Weapons\Scrap\ScrapChakram.png',
    r'Content\Items\UpgradeTrees\RustedGearShuriken.png',
    r'Content\Items\UpgradeTrees\SalvagedSteelChakram.png',
    r'Content\Items\UpgradeTrees\AshHeartCinderRing.png',
    r'Content\Projectiles\Scavenger\ScavengerCaltrop.png',
    r'Content\Projectiles\Scavenger\ScavengerCaltropEX.png',
    r'Content\Projectiles\Archivist\ArchivistBoneBoomerang.png',
    r'Content\Projectiles\Archivist\ArchivistBoneBoomerangEX.png',
    r'Content\Projectiles\AshHeart\AshHeartRogueProjectile.png',
    r'Content\Projectiles\AshHeart\AshHeartRogueProjectileEX.png',
    r'Content\Projectiles\Fireplace\FireplaceRogueProjectile.png',
    r'Content\Projectiles\Fireplace\FireplaceRogueProjectileEX.png',
    r'Content\Projectiles\Rust\RustShurikenProj.png',
    r'Content\Projectiles\Rust\RustShurikenProjEX.png',
    r'Content\Projectiles\Scrap\ScrapChakramProj.png',
    r'Content\Projectiles\UpgradeTrees\RustedGearBoomerang.png',
    r'Content\Projectiles\UpgradeTrees\SalvagedSteelChakramProj.png',
    r'Content\Projectiles\UpgradeTrees\ChakramShatterBlast.png',
    r'Content\Projectiles\UpgradeTrees\CinderRingEmber.png',
]

missing = []
removed = 0
for rel in PNGS:
    p = os.path.join(ROOT, rel)
    if os.path.isfile(p):
        os.remove(p)
        removed += 1
    else:
        missing.append(rel)

print('removed =', removed, 'of', len(PNGS))
for m in missing:
    print('  NOT FOUND:', m)
