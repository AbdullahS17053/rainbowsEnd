# Police Mobs Plan — Phase 0 Addendum

Status: Phase 0 complete; Phase 1 pending

This addendum is authoritative for Phase 0 findings and supersedes any conflicting wording in `35_PoliceMobs_Feedback_Plan.md`.

## Verified baseline

- Unity MCP instance: `rainbowsEnd@8545bd2c`.
- Unity version: `6000.0.67f1`.
- Active scene: `Assets/_Project/Scenes/MainArena_3D.unity`.
- Editor state: idle; not playing; not compiling; no domain reload pending.
- Console baseline: 0 current errors and 0 current warnings.
- Full EditMode baseline: **103 passed, 0 failed, 0 skipped**.

## Live scene wiring

- `PoliceSpawner` uses `SO_Enemy_Police_Default`, fixed `SO_Weapon_Pistol_Default`, `PolicePool`, `PoliceProjectilePool`, `WeaponImpactPool`, and `WeaponPickupPool`.
- The live police spawner currently has generic/legacy weapon-drop entries for pistol/12 and shotgun/24. It has no GoldPickup pool field; Phase 1/3 must add a reference to the existing `GoldPickupPool` rather than creating another pool.
- Pool snapshot: Police `2/64`, PoliceProjectile `16/64`, WeaponPickup `4/64`, GoldPickup `6/64` (prewarm/max). All inspected pools use `PrefabPool` with collection checks enabled.
- `RunDirector` uses `SO_Wave_Default` and the live `PoliceSpawner`/`MilitarySpawner`.

## Visual and Animator wiring

- `PF_Police` already contains `CharacterAnimationBridge`, with its body, Animator, and visual-root references assigned. Its child Animator currently uses `Assets/_Project/Art/Animators/Police.controller`.
- `PF_Military` uses the same bridge pattern and its child Animator uses `Assets/_Project/Art/Animators/Military.controller`.
- Both controllers expose the required `Speed` float and `Fire` trigger. The approved Phase 1 action is to assign the existing `Military.controller` to the police child Animator after compatibility verification; do not edit or duplicate the controller asset.
- `PF_Police` currently holds `SM_Wep_Pistol_01`. The installed shotgun visual is `Assets/Synty/PolygonMilitary/Prefabs/Weapons/SM_Wep_Shotgun_01.prefab`.

## Serialized gameplay values

- Police: MoveSpeed `3.25`, EngagementRange `50`, SeparationRadius `0.6`, SeparationStrength `2`.
- Pistol: damage `3`, projectile count `1`, cooldown `0.75`, range `8`, cone `35`.
- Shotgun: damage `1`, projectile count `6`, cooldown `0.45`, range `5`, cone `75`.
- Wave police counts are currently `0/0/1/2/3`; military counts are `0/0/2/3/4`. Wave intervals and concurrency caps remain at their current authored values.

## Phase 0 acceptance

- Existing scene, prefab, Animator, weapon, pool, and test seams are identified.
- Baseline tests and Console state are recorded.
- No gameplay, code, prefab, scene, ScriptableObject, or package changes were made.
