# Weapons & Combat Phase 1 Recap

Status: Complete

## Scope

Phase 1 made Fae Bolt the explicit Player default presentation, preserved temporary firearm pickup ownership, and routed Player, Police, and Military shots through authored visual weapon origins with the existing numerical fallback.

## Implemented

- Added WeaponPresentation to WeaponConfig: Fae Bolt, Pistol, Shotgun, and Assault Rifle.
- Marked the four default weapon assets with their corresponding presentation values.
- Preserved PlayerWeaponInventory as the only temporary firearm ownership path and its immediate fallback to Fae Bolt after ammo exhaustion.
- Added project-owned Player presentation children under the animated casting hand: FaeBoltMuzzle, PlayerPistolVisual, PlayerShotgunVisual, and PlayerAssaultRifleVisual.
- Added project-owned Police pistol/shotgun muzzle anchors and Military rifle/shotgun visual/muzzle anchors under the animated weapon hierarchies.
- Added automatic active presentation selection: Fae Bolt hides firearm meshes; a temporary pickup activates only its matching firearm mesh; expiry restores Fae Bolt.
- Added explicit muzzle-origin routing to WeaponController and RangedAttackController. Authored transforms win; missing references use the configured MuzzleOffset or fallback offset.
- Kept projectile count, speed, range, cooldown, damage, spread, collision, pooling, pickup ammo, and drop rules unchanged.

## Verification

- Full EditMode suite: 134 passed, 0 failed, 0 skipped.
- Play Mode smoke: entered and exited normally.
- Live startup inspection: FaeBoltMuzzle=True, PlayerPistolVisual=False, PlayerShotgunVisual=False, PlayerAssaultRifleVisual=False.
- Console has no compile/runtime errors. Remaining entries are the known Unity Performance Testing setup/result messages.

## Phase 2 entry points

1. Reuse the existing pooled ProjectileWeaponLauncher/ArrowProjectile lifecycle for magic versus firearm visual selection.
2. Add the smallest project-owned Fae Bolt and firearm tracer/muzzle/impact presentation from the approved Epic Toon FX shortlist.
3. Preserve the existing six-pellet shotgun gameplay volley while showing only two deterministic representative tracers.
