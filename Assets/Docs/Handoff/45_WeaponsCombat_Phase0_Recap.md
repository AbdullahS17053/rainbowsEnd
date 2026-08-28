# Weapons & Combat Phase 0 Recap

Status: Complete

## Scope

Phase 0 established the current weapon, projectile, pooling, presentation, and facing seams without changing gameplay behavior.

## Verified baseline

- Unity 6.0.0.67f1 is connected to the rainbowsEnd project.
- Active scene: Assets/_Project/Scenes/MainArena_3D.unity.
- Editor returned to edit mode after the smoke capture.
- Full EditMode suite: 132 passed, 0 failed, 0 skipped.
- Console contains only the known Unity Performance Testing setup/result messages; no compile or runtime errors were reported.

## Current combat flow

- Player firing is owned by WeaponController.
- Police and Military firing are owned by RangedAttackController.
- All three routes launch through ProjectileWeaponLauncher into pooled ArrowProjectile instances.
- Hit resolution and impact feedback are already centralized in WeaponHitUtility and WeaponImpactFeedbackSpawner.
- The current projectile origin is the body position plus the configured MuzzleOffset or fallback offset. No authored weapon/casting-hand muzzle anchors exist yet.
- WeaponConfig currently contains combat numbers only; it has no presentation/loadout discriminator.
- PlayerWeaponInventory already uses Fae Bolt as the default and swaps to a temporary picked-up firearm until its ammo is exhausted.
- Player aiming rotates PlayerRoot.VisualRoot; Police and Military directly rotate their visual roots during combat/travel. The animation bridge samples locomotion in LateUpdate.

## Serialized asset audit

- PF_Player references SO_Weapon_FaeBolt_Default for both WeaponController and PlayerWeaponInventory; no starting pistol reference is present.
- PF_Police contains inactive/active pistol and shotgun visual children and uses the configured Police weapon asset at runtime.
- PF_Military contains the Synty assault-rifle visual and uses the configured Military weapon asset at runtime.
- PF_PoliceProjectile and PF_MilitaryProjectile share the pooled ArrowProjectile component and currently include a Synty bullet mesh child.
- PF_BowArrow already reuses Epic Toon FX trail components, and PF_BowImpact already reuses Synty gunshot feedback. Candidate Epic Toon FX assets remain package-owned and must be reused through project-owned prefab references.

## Phase 0 decisions carried forward

- Keep Fae Bolt mechanics unchanged; change only its presentation.
- Keep the player firearm override/fallback behavior and remove only the visible starting pistol state.
- Keep projectile physics, damage, range, hit detection, and pool lifecycle unchanged while adding visual tracers.
- Preserve six physical shotgun pellets and show two representative tracers.
- Apply the dynamic hip-fire cone to the player only: moving uses 70% range and 1.75x cone width; stationary uses the configured values.
- Use authored casting-hand and weapon-muzzle transforms, with the numerical offset retained only as fallback.
- Aim/fight targets control body and weapon facing; travel direction controls facing outside combat.
- Reuse compatible Epic Toon FX assets; do not edit package source.

## Phase 1 entry points

1. Add the smallest presentation discriminator/origin seam needed by the existing config and launcher flow.
2. Add project-owned authored anchors to the player, Police, and Military visual hierarchies.
3. Route player, Police, and Military launches through those anchors with the existing fallback preserved.
4. Add focused tests for inventory boundaries and origin precedence/fallback before changing visual projectile presentation.
