# Weapons & Combat Feedback Implementation Plan

Status: Phase 3 complete

## Client request

- Name the Player's magic attack **Fae Bolt**. Remove the Player's starting pistol. A firearm pickup stops Fae Bolt auto-fire and uses the picked-up firearm instead.
- Make firearms feel tactical with visible, fast, forceful tracers.
- Ensure the assault rifle is a playable drop.
- Make the Player firing cone dynamic: shorter/wider while moving and longer/narrower while stationary.
- Spawn Player and hostile fire from the firearm muzzle rather than the character body.
- Make Player, Police, and Military body/weapon facing agree with their shot direction, and keep firearms drawn while moving.

## Approved decisions

- **Fae Bolt mechanics remain unchanged.** Its new green-and-gold projectile, short sparkle trail, and magical impact only change presentation. Phase 0 must inventory Epic Toon FX first; reuse a compatible project-owned derivative/prefab and never edit package-source assets.
- The Player starts each run with **Fae Bolt only** and no visible pistol. Existing PlayerWeaponInventory temporary-overrides remain the authority: a picked-up firearm replaces Fae Bolt until its temporary shots reach zero, then it immediately falls back to Fae Bolt.
- The existing **assault rifle is already a playable Military-only drop**. Keep its current 24-shot reward path; do not add it to Police drops.
- Firearms keep their current pooled projectile, speed, range, collision, damage, cooldown, spread, and pooling rules. Add only presentation: amber Player tracers, red/orange Police/Military tracers, brief muzzle flashes, and existing/compatible impact feedback. Do not add reloads, magazines, hit-scan, projectile gravity, penetration, audio, or aim-affecting recoil.
- Pistol and rifle show a tracer for every fired projectile. A shotgun preserves all six physical pellets but shows **two representative tracers** only.
- Fae Bolt fires from a Player casting-hand anchor. Firearms fire from their actual weapon-muzzle anchors. An existing numerical muzzle offset remains only as a safe fallback for missing authoring references.
- Dynamic cone is **Player-only**. At movement above the approved deadzone, use 70% active range and 1.75x active cone width; when stationary, use normal range and width. Values are Inspector-tunable on the existing Player-owned configuration, not copied into every weapon or enemy asset.
- While aiming/fighting, Player, Police, and Military visual body and weapon face the aim target. Outside that state, they face movement direction. The held firearm remains visible in idle, locomotion, and fire states until its Player pickup expires. No new animator clips, reload/holster states, or generic facing framework are in scope.

## Existing implementation facts

- PF_Player and ProjectConfigRegistry already serialize SO_Weapon_FaeBolt_Default as the default WeaponConfig. PlayerWeaponInventory.ActiveWeaponConfig returns the temporary weapon while it has shots, otherwise its default; WeaponController synchronizes that active config after a successful shot consumes temporary ammo.
- MilitarySpawner assigns the configured assault rifle/shotgun loadout and matching drop ammo. MilitaryController rents the existing WeaponPickup through WeaponPickupPool; WeaponPickup already equips the Player's temporary override. The rifle default reward is 24 shots.
- WeaponController currently computes Player fire from the actor body plus WeaponConfig.MuzzleOffset. RangedAttackController does the same from the caller-supplied actor position. PoliceController and MilitaryController currently call RangedAttackController.TryFire(transform.position, direction, ...), so all firearm fire begins at the body.
- ProjectileWeaponLauncher and ArrowProjectile already rent pooled projectiles, apply each WeaponConfig projectile speed/range, resolve CircleCast hits through WeaponHitUtility, spawn the existing impact feedback, then return to their owning pool. This remains the gameplay authority.
- WeaponController currently uses one resolved range for target search, cone drawing, and projectile range override; ResolveAutoFireConeDegrees is static per active WeaponConfig. Existing movement-based accuracy spread remains intact and is separate from the requested cone shape.
- PlayerAimController writes the Player visual root toward current aim. PoliceController and MilitaryController write their visual roots toward the target during combat. CharacterAnimationBridge also has optional movement-facing behavior, so Phase 0 must identify every writer on PF_Player, PF_Police, and PF_Military before changing precedence.
- PF_Police already owns pistol and shotgun visual children; PF_Military owns the assault-rifle visual. Synty provides the held-weapon meshes. Epic Toon FX contains compatible building blocks including TinySparkle, SparkExplosion2D, MetalHit2D, magic textures/materials, and muzzleflash01; suitability must be checked in the live 3/4 camera before selection.

## Ownership and guardrails

| Owner | Responsibility | Must not own |
| --- | --- | --- |
| PlayerWeaponInventory | Existing temporary-firearm ownership and Fae Bolt fallback. | Projectile physics, visual pool lifetime, enemy loadouts. |
| WeaponController | Player-only active-weapon presentation, casting/firearm origin selection, hip-fire posture values, targeting/cone visualization, and Player fire request. | Enemy AI, firearm ammo rules, new inventory architecture. |
| RangedAttackController | Existing enemy cadence/spread plus exact authored firearm muzzle origin. | Player hip-fire rules, loadout choice, damage rewrite. |
| ProjectileWeaponLauncher / ArrowProjectile | Existing pooled launch/hit lifecycle plus minimal projectile-presentation selection and representative shotgun visual suppression. | New ballistic simulation, weapon inventory, target selection. |
| PF_Player / PF_Police / PF_Military | Authored hand/muzzle anchors, held meshes, and compatible local feedback references. | Package-source edits or collider/physics ownership. |
| PlayerAimController, PoliceController, MilitaryController, CharacterAnimationBridge | One unambiguous aim/fight vs travel-facing precedence. | New animation framework or movement rewrite. |
| WeaponConfig and existing PlayerConfig | Existing weapon statistics; minimal presentation discriminator and Player-only hip-fire tuning where required. | Duplicate per-enemy cone tuning or scene references. |

Do not change weapon damage, cooldowns, projectile counts, projectile collision/hit filtering, pot immunity, enemy targeting/range, drop odds, wave composition, Player recovery/concealment, NavMesh movement, character source assets, shared package assets, or the existing ammo HUD semantics.

## Phase 0 — Baseline, exact seams, and FX audit

### Goal

Record the live combat, prefab, pool, animation, and visual starting point before any change.

### Steps

1. Confirm MainArena_3D is active, Edit Mode is idle, compilation is complete, and Console is captured. Record scoped working-tree changes; preserve unrelated work.
2. Inspect PF_Player, PF_Police, PF_Military, PF_WeaponPickup, the Player/Police/Military projectile and impact prefabs/pools, all four WeaponConfig assets, PlayerConfig, ProjectConfigRegistry, Police/Military configs, and relevant scene pool capacities. Record current serialized references, visible weapon children, Animator/controller parameters, CharacterAnimationBridge facing flags, and existing muzzle-offset values.
3. Trace every caller of WeaponController.FireShot/SyncActiveWeaponConfig, RangedAttackController.TryFire, ProjectileWeaponLauncher.LaunchVolley, ArrowProjectile.Launch/ReturnToPool, PlayerAimController.Update, and all direct visual-root rotation writes in Player/Police/Military code. Identify the smallest shared seam for each requested change.
4. Inspect the Player visual skeleton and all held firearms in Prefab Mode. Locate a stable casting-hand transform plus physical muzzle points for pistol, shotgun, and rifle. Verify that they move with the animated visual and are not the 2D physics root. Add nothing during this phase.
5. In Play Mode, record: Fae Bolt firing; pistol, shotgun, and rifle pickup/expiry fallback; Player standing/moving cone; Police/Military fire/telegraph/reposition; a shotgun volley; and Player/hostile body orientation while moving and firing. Capture the current body-origin fire and any facing disagreement as repro evidence.
6. Preview Epic Toon FX candidates in the active camera. Select the smallest compatible existing magic sparkle/impact and firearm muzzle/impact ingredients, or record why the existing project prefab visual is sufficient. Do not use an effect that interferes with 2D hit colliders or obscures the rainbow trail.
7. Run focused baseline EditMode tests: PlayerWeaponInventory, WeaponRuntimeState, WeaponSpreadUtility, WeaponAutoFireTargetingUtility, WeaponVolleyUtility, projectile/hit utilities, pooling, Police, and Military tests. Record failures as baseline only; do not weaken tests.

### Acceptance criteria

- Live weapon/fallback, pool, anchor, facing, and FX seams are proven from scene/prefab state and source.
- The implementation has an approved existing-asset shortlist and no package-source mutation.
- No gameplay, prefab, scene, config, or runtime code changes are made.

### Rollback

No implementation change exists; retain only the Phase 0 recap.

## Phase 1 — Fae Bolt default, firearm presentation selection, and authored origins

### Goal

Make Fae Bolt the unambiguous default, preserve the existing firearm-override path, and give every firing mode a correct transform origin.

### Steps

1. Preserve PlayerWeaponInventory and its fallback semantics. Verify SO_Weapon_FaeBolt_Default remains the Player and registry default. Remove/disable the Player's startup pistol visual; do not remove pistol pickup support.
2. Extend the smallest existing weapon authoring seam with a minimal presentation discriminator: Fae Bolt versus firearm. Reuse WeaponConfig rather than creating a new manager, inventory hierarchy, or generic weapon framework. Configure Fae Bolt as magic and pistol/shotgun/rifle as firearms.
3. Extend WeaponController's existing active-config synchronization to select Fae casting presentation with no firearm mesh, or the correct held pistol/shotgun/rifle mesh while a temporary weapon is active. Maintain the selected mesh in idle, movement, and firing, then restore the Fae state immediately on ammo exhaustion.
4. Add stable child anchors to project prefabs: Player casting hand; Player pistol/shotgun/rifle muzzle; Police pistol/shotgun muzzle; Military rifle/shotgun muzzle. Use empty Transform children parented to the correct animated visual/weapon, adjusted in Prefab Mode from the game camera. Do not place anchors on the 2D physics root.
5. Extend WeaponController and RangedAttackController with the narrowest explicit-origin route so their launch origin uses the current anchor position. When a selected anchor/reference is genuinely absent, preserve the current MuzzleOffset path as defensive fallback; do not silently fire from a body root when an anchor is configured.
6. Pass the selected Police/Military loadout's muzzle to the existing ranged attack path during activation. Pass the Player casting hand for Fae Bolt and the selected held-firearm muzzle for a temporary firearm. Keep weapon pickup/drop data and ammo counts unchanged.

### Automated verification

1. Add focused tests for Player temporary firearm precedence and return-to-Fae fallback if the existing tests do not already cover the exact boundary.
2. Add a small origin-selection test proving an authored transform wins and a missing transform uses the existing offset fallback. Verify no null reference or pooled-launch failure occurs for a missing optional visual reference.
3. Retain all weapon runtime, pickup, projectile, hit, Police, Military, and pool tests.

### Manual verification

1. Start a run: no pistol is visible and Fae Bolt fires from the casting hand.
2. Collect pistol, shotgun, then military rifle; each visibly draws, fires from its muzzle, persists while moving, and restores Fae Bolt after its last shot.
3. Confirm the existing weapon pickup model, ammo counter, damage, cadence, and drop chances remain unchanged.

### Acceptance criteria

- Fae Bolt is the visually honest default with unchanged gameplay.
- Every loaded weapon has one correct authored fire origin; fallback is safe but not used in normal prefab wiring.
- The existing temporary inventory behavior remains the single ownership path.

### Rollback

Restore the serialized default/visual/anchor references and the minimal origin-selection code; no economy, wave, or pickup data requires migration.

## Phase 2 — Tactical projectile presentation without ballistic changes

### Goal

Make firearm fire visibly fast and readable while preserving all combat physics and pool behavior.

### Steps

1. Reuse the existing ProjectileWeaponLauncher and ArrowProjectile lifecycle. Add only the presentation branch necessary to select a green/gold magic visual for Fae Bolt or a tracer visual for firearms. The projectile still uses the existing configured ProjectileSpeed, range, CircleCast, WeaponHitUtility, impact feedback, and ReturnToPool path.
2. Create/project-own the minimal presentation prefabs or visual children from the Phase 0 Epic Toon FX shortlist. Fae Bolt uses a compact green/gold bolt, short sparkle trail, and magical impact. Firearms use thin fast tracers, brief muzzle flash, and a small compatible impact cue. Keep all collider, layer, sorting, and pool-root gameplay components unchanged.
3. Use the existing per-faction projectile/impact pools where compatible. Add a separate pool only if Fae Bolt and firearm presentation cannot safely share a pooled visual state; prewarm and max capacity must be based on the live Phase 0 volley/rate observations. Never Instantiate per shot.
4. At firearm launch, play local visual-only feedback at the resolved muzzle. Keep it short-lived and reset it on pool return/weapon swap. A small local visual kick is allowed only if it is stable on the existing rig; it must not rotate the physics root, modify aim, or change projectile direction.
5. For each shotgun six-pellet volley, preserve all six launch/hit calculations but flag only two deterministic representative pellets for tracer rendering. Pistol and rifle show every projectile. The representative choice must be deterministic per volley, not random gameplay data.
6. Keep player tracer color amber and hostile tracer color red/orange through prefab/material references. Fae Bolt green/gold remains visibly distinct from both. Validate readability against grass, rainbow trail, and tall-environment backgrounds.

### Automated verification

1. Add one focused deterministic check for representative-tracer selection: a six-pellet volley marks exactly two visual tracers while one-projectile firearm volleys mark one. It must not change projectile count or hit resolution.
2. Run projectile launcher, WeaponVolleyUtility, hit, impact, pool, Police, Military, and Player inventory tests. Confirm a failed pool rent still produces no false fire success.

### Manual verification

1. Fire Fae Bolt over grass and rainbow trail: it reads as magic, not a bullet.
2. Fire each firearm while standing and moving: projectile path/tracer appears fast and begins exactly at the muzzle.
3. Observe Police and Military from range: red/orange tracer/muzzle/impact feedback is readable without covering targets or trail gameplay.
4. Fire shotgun repeatedly: exactly two visible traces per six-pellet shot; damage and hit count remain normal.

### Acceptance criteria

- Tactical feedback is visible, faction-readable, pooled, and presentation-only.
- Fae Bolt is visually magical and distinct.
- No new ballistic, reload, ammo, damage, or collision rule exists.

### Rollback

Restore prior projectile/impact visual references and remove only the presentation-selection branch; pools and combat math retain their existing contracts.

## Phase 2 completion record

- Reused the scene's existing ArrowProjectilePool and WeaponImpactPool; no new per-shot Instantiate path or new pool was added.
- Added WeaponVolleyUtility.ShouldRenderTracer: one tracer for single-projectile firearms, exactly two deterministic representatives (indices 1 and 4) for six-pellet shotguns, and unchanged physical launch/hit count.
- Added pooled projectile presentation switching in ArrowProjectile: Fae Bolt enables the existing magic trail and applies green/gold particle colors; firearms disable that trail and enable the shared pooled TrailRenderer.
- Added faction-readable tracer colors: amber/gold for Player and red/orange for hostile fire.
- Added reusable Epic Toon FX muzzle feedback under the authored casting/weapon muzzle anchors and replayed it only after a successful pooled launch.
- Added a pooled magic impact variant to PF_BowImpact; CombatImpactEffect now selects magic or firearm impact visuals and resets both branches on pool lifecycle callbacks.
- Kept ProjectileSpeed, range, CircleCast, WeaponHitUtility, damage, cooldown, projectile count, spread, pool ownership, and impact lifetime/scale contracts unchanged.
- Added the focused deterministic tracer-selection test to WeaponSpreadUtilityTests.

### Phase 2 verification

- Full EditMode suite: 135 passed, 0 failed, 0 skipped.
- Runtime MCP smoke: Fae launch reported magic=True; pistol launch reported one Player tracer; shotgun launch reported six physical projectiles with two amber tracer visuals.
- Prefab checks: PF_BowArrow has the pooled TrailRenderer; Player, Police, and Military authored muzzle anchors contain the reusable muzzle FX; PF_BowImpact contains the inactive magic impact variant.
- Play Mode entered and exited normally. After the verification run, Console had no errors or warnings; only the expected Unity Performance Testing setup/result messages were present.
- Phase 3 remains responsible for Player-only dynamic cone posture and authoritative combat-facing changes.

## Phase 3 — Player hip-fire cone and authoritative combat facing

### Goal

Make Player targeting posture communicate movement trade-offs and eliminate body/weapon direction disagreement.

### Steps

1. Add the approved Player-owned Inspector tuning: movement threshold/deadzone, HipFireRangeMultiplier default 0.70, and HipFireConeMultiplier default 1.75. Validate non-negative values and do not duplicate them across WeaponConfig, Police, or Military configs.
2. In WeaponController, resolve the Player posture once per frame from existing movement normalized speed. Use the resolved range for target search, line-of-sight maximum, runtime cone mesh, editor gizmo, and LaunchVolley range override. Use the resolved cone width for target selection and every visible cone. Do not change WeaponConfig base range, existing upgrade math, projectile speed, or movement-based spread calculation.
3. Stationary/aiming posture uses the active weapon's normal upgraded range and auto-fire cone. Moving posture uses normal range times 0.70 and normal cone times 1.75. The transition must be immediate and free of stale mesh/target values; use the threshold to avoid flicker from near-zero rigidbody noise.
4. Establish a single rotation precedence. PlayerAimController owns Player aim-facing while Player has aim input/active aim; its visual root and all active firearm/casting children rotate together. Configure or minimally extend CharacterAnimationBridge so its locomotion facing cannot overwrite this Player aim result in LateUpdate.
5. For Police and Military combat/reposition/telegraph/fire phases, their controllers own combat target-facing and the held weapon remains parented to the body. When they have no combat aim target and travel to a non-combat destination, explicitly use travel direction. Configure or minimally extend CharacterAnimationBridge only where necessary so it continues to drive Speed but never overwrites that controller decision.
6. Retain existing Run_1 locomotion and fire-trigger behavior. Do not introduce new aim, strafe, holster, reload, or two-handed animation clips. Adjust project-prefab weapon orientation/hand placement only as needed to keep a visually coherent ready pose from the game camera.

### Automated verification

1. Add a focused posture-resolution test covering stationary, exactly-at-threshold, and moving values. It must prove the 0.70/1.75 multipliers affect Player cone/range only and preserve normal values when stationary.
2. Add/retain focused facing tests or safe serialized assertions proving Player/Police/Military combat prefabs do not enable a later movement-facing writer that overrides aim-facing.
3. Run all character animation bridge, Player aim/movement, combat, Police, Military, projectile, and pooled lifecycle tests.

### Manual verification

1. With each Player weapon, stop and move repeatedly: the visible cone becomes longer/narrower while stationary and shorter/wider while moving, matching auto-target eligibility and actual projectile range.
2. Aim the Player opposite travel direction: body, casting hand/firearm, muzzle, cone, and projectile all agree.
3. Watch Police/Military approach, strafe, telegraph, and fire: legs animate at movement speed; their bodies and weapons aim at the Player during combat; they face travel only outside combat.
4. Verify no vertical/2D collider, NavMesh, separation, targeting, or pot-theft behavior changes.

### Acceptance criteria

- Player-only posture visibly and mechanically matches the approved 70%/1.75x trade-off.
- All weapon-firing actors have consistent body, weapon, muzzle, cone, and projectile direction.
- Existing locomotion animation and AI movement retain their prior ownership and behavior.

### Rollback

Restore PlayerConfig posture values/references and the existing facing flags/writes; no weapon or enemy data migration is required.

## Phase 3 completion record

- Added Player-owned HipFireMovementThreshold (0.10), HipFireRangeMultiplier (0.70), and HipFireConeMultiplier (1.75) to PlayerConfig and serialized the approved defaults in SO_Player_Default.
- Added the minimal PlayerWeaponPostureUtility seam: movement above the threshold uses the approved 70% range and 1.75x cone; stationary and exactly-at-threshold use normal values.
- WeaponController resolves posture once per frame after active weapon synchronization and reuses the resolved range/cone for target selection, line of sight, runtime cone visualization, editor gizmo, and projectile range override.
- Confirmed existing facing ownership: PlayerAimController rotates the Player VisualRoot; PoliceController and MilitaryController write combat/travel facing; CharacterAnimationBridge movement-facing is disabled on Player, Police, and Military prefabs, so LateUpdate cannot overwrite those decisions.
- No damage, cooldown, spread, projectile count, collision, pool, NavMesh, enemy AI, or animation clip behavior changed.

### Phase 3 verification

- Focused WeaponSpreadUtilityTests: 5 passed, including stationary, exact-threshold, and moving posture assertions.
- Full EditMode suite: 136 passed, 0 failed, 0 skipped.
- Runtime MCP smoke: stationary Player posture resolved to range 12 and cone 15; moving posture resolved to range 8.4 and cone 26.25, matching 0.70 and 1.75 multipliers.
- Play Mode entered and exited normally; after clearing test-run output, Console reported 0 errors.
- Phase 4 remains responsible for full-round manual regression, pooled cleanup verification, documentation context updates, and Graphify refresh.

## Phase 4 — Regression, live tuning, documentation, and graph refresh

### Goal

Prove the feature end-to-end, tune only approved Inspector values, and leave a current reference trail.

### Steps

1. Run all focused tests from Phases 1-3, then the full EditMode suite. Treat any unrelated failure as baseline evidence only; fix any failure caused by this feature before completion. Check Console after compilation and tests; resolve new errors/warnings rather than masking them.
2. Play a full representative round with Fae Bolt, pistol, shotgun, and military rifle pickups. Cover Fae fallback, weapon expiry, Player standing/moving cone, all muzzle origins, single-round tracers, shotgun two-tracer rule, enemy bursts, enemy teleport/portal edge cases if relevant, and kills/drops.
3. Tune only the approved live authoring values if needed: hip-fire threshold/range/cone multipliers; anchor transforms; short visual scale/lifetime; tracer/material readability; and pool prewarm/max capacity. Record final values. Do not tune damage, cooldown, projectile count, or enemy AI under this request.
4. Confirm all repeated projectile, impact, muzzle feedback, and weapon visual instances reset on pool return, despawn, death, weapon swap, and scene restart. Verify no growth in active objects or stale trails/flash/mesh state during repeated rifle/shotgun fire.
5. Update this plan to Phase 4 complete, add a concise phase recap with final wiring/tuning/test results, update Assets/Docs/Handoff/04_Context.md terminology/current state, and refresh Graphify incrementally. Verify graph links PlayerWeaponInventory, WeaponController, RangedAttackController, ProjectileWeaponLauncher, ArrowProjectile, PlayerAimController, CharacterAnimationBridge, PoliceController, MilitaryController, and WeaponConfig.

### Acceptance criteria

- Full automated coverage passes with no feature-caused console errors.
- Fae Bolt, every firearm, Player cone posture, muzzle fire, body/weapon facing, and pooled cleanup meet the approved behavior in live play.
- Handoff docs and Graphify accurately describe the implemented seams and final tunables.

### Out of scope

- New weapons, reload/magazine systems, weapon switching UI, weapon rarity, ballistics/gravity/penetration, hitscan, audio, multiplayer/networking, new movement AI, new locomotion/aim/reload clips, new input actions, changes to civilian behavior, Police/Military drop odds, or package/source-asset edits.
