# Upgrade Screen Feedback — Phase 4 Recap

## Scope

Implemented the `Cone Reach Up` purchase as a runtime weapon-range upgrade. The configured value is applied consistently to target acquisition, the visible firing cone, and arrow travel distance. The cone angle is unchanged.

## Runtime changes

- `PlayerUpgradeRuntime` now stores `WeaponRange` purchases and exposes `ResolveWeaponRange(baseRange)`.
- `WeaponController` resolves the upgraded range for targeting, editor gizmos, runtime cone visualization, and each fired volley.
- `ProjectileWeaponLauncher` accepts an optional range override and passes it to every projectile in the volley.
- `ArrowProjectile` uses the resolved range for travel clamping and despawn. Existing callers that omit the override keep the weapon-config range.

## Verification

- `PlayerUpgradeRuntimeTests.ApplyPurchase_UpdatesResolvedWeaponStats`: passed; confirms 8 base range + 2 upgrade = 10 resolved range.
- `PlayerUpgradeRuntimeTests.CanPurchase_BlocksPurchasesBeyondConfiguredMax`: passed.
- Unity Play mode smoke check: entered and exited successfully without gameplay errors.
- `MainArena_3D` scene validation: 0 issues, 0 missing scripts, 0 broken prefabs.

## Remaining manual check

In a normal play session, purchase `Cone Reach Up` and visually confirm that the cone and arrow reach extend by 2 world units per purchase, up to three purchases.
