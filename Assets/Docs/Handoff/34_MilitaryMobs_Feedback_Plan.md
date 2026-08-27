# Military Mobs Feedback Implementation Plan

Status: Implemented

## Implementation recap

- Added and wired `Assets/_Project/ScriptableObjects/Defaults/SO_MilitaryMob_Default.asset` through `MilitaryMobConfig`: 50/50 shotgun/assault-rifle loadouts, three-shot bursts, one-second strafe pause, 0.35-second red telegraph, paired/concealed carry thresholds, 33% exclusive death reward, and 24/6 ammo defaults.
- Updated `MilitarySpawner` and `MilitaryController` for per-spawn loadouts, pooled telegraph reset, burst/reposition combat, paired Pot carry, concealment solo carry, partner-loss recovery, and exclusive weapon-or-gold death drops.
- Updated `RunWaveController` to rotate Traveller/Police/Military among remaining wave counts. Serialized Wave 3/4/5 military counts are now 2/3/4; the assault-rifle range is 8.
- Added the persistent disabled `LineRenderer` to `Assets/_Project/Prefabs/PF_Military.prefab`; it is enabled only during the telegraph phase and is reset by pooling lifecycle hooks.
- Added focused tests for exclusive military rewards and three-type wave rotation. Full EditMode suite: 101 passed, 0 failed. Play-mode smoke entered/exited cleanly with no Unity errors or warnings.

## Client request

Make military mobs use an even shotgun/assault-rifle loadout split, shorten assault-rifle range, fire telegraphed three-shot bursts with movement between bursts, arrive more often and more gradually in Waves 3–5, require coordinated Pot theft outside concealment, and use a controlled death-drop table.

## Approved decisions

- Each military mob rolls its loadout once when it spawns: **50% shotgun / 50% assault rifle**. If it drops a weapon, that pickup matches its assigned loadout.
- Reduce assault-rifle range from **14** to **8 world units**. Keep military detection/engagement range at **50**; it is already map-wide, so raising it would not improve acquisition.
- Both loadouts fire a **three-shot Military Burst** at their own existing cooldowns. After every burst they take a **one-second**, non-firing, full-speed strafe left or right before the next burst.
- Show a non-damaging **red aim telegraph** for **0.35 seconds** before every burst. It applies to both loadouts and uses a project-local reuse of an installed beam effect.
- Military counts are: Wave 3 = **2**, Wave 4 = **3**, Wave 5 = **4**. Mixed waves rotate Traveller → Police → Military among types still outstanding, while retaining the existing per-wave interval and concurrency cap.
- Standard Pot theft is a **Paired Carry**: two military mobs must assemble at the Pot and remain present during the carry. If either carrier is lost, drop the Pot and return the survivor to combat.
- A Pot inside a `ConcealmentZone` when theft begins uses **Concealed Solo Carry**: one military mob may carry it to the edge. This eligibility is fixed at theft start and does not change mid-carry.
- On death, a military mob has one **33% total** chance to create exactly one reward: an equally likely weapon pickup (with ammo) or 10 Pot Gold. The other 67% creates no reward. No separate ammo-only pickup is added.
- Weapon-drop ammo defaults are **24** for assault rifles and **6** for shotguns.
- All new military-only values are Inspector-tunable in a dedicated `MilitaryMobConfig` ScriptableObject. Existing `WeaponConfig` remains the range owner; `WaveConfig` remains the wave count/interval/concurrency owner.

## Existing implementation facts

- `MilitarySpawner` currently assigns one assault-rifle config to every mob. `MilitaryController.Activate(...)` configures `RangedAttackController` from that one config.
- `SO_Weapon_AssaultRifle_Default` has range `14`, cooldown `0.25`, and one projectile; the existing shotgun has range `5`, cooldown `0.45`, and six-projectile volleys. `RangedAttackController` already respects the assigned `WeaponConfig`.
- `MilitaryController.TickPoliceStyleCombat()` fires continuously whenever the player is in weapon range. It already separates and paths towards the player, but stops at preferred range; an explicit strafe phase is needed to guarantee movement between bursts.
- `WeaponRuntimeState` already supplies per-shot cooldown. Do not change projectile, spread, damage, or hit logic to implement a burst.
- The installed project includes `Assets/PolygonParticleFX/Prefabs/FX_LazerBeam_01.prefab`; do not edit that package asset. A project-local variant/child is the only permitted visual reuse path.
- `WaveConfig` currently has military counts `0/1/2` in Waves 3/4/5. `RunWaveController` currently alternates traveller/police, then emits military as a final block, although the existing interval and concurrency cap already stagger individual spawns.
- The current theft check only requires `ActiveMilitaryCount >= 2`; one mob picks up the Pot immediately and another follows. `PotController` has only `IsBeingStolenByMilitary`, so paired ownership must be represented by the military controllers, not inferred from that bool.
- `ConcealmentZone` already exists as a trigger-backed gameplay component; visible grass/bush meshes are not authoritative concealment.
- `MilitarySpawner` currently resolves a weapon drop and gold drop independently at spawn, and `MilitaryController.HandleDeath()` spawns both. `WeaponPickup` is a combined weapon-and-ammo pickup; there is no ammo-only pickup. `GoldPickup` and `WeaponPickup` already use pools.

## Target design

### Configuration ownership

| Owner | Tunable values | Keep out of it |
| --- | --- | --- |
| `SO_Weapon_AssaultRifle_Default` | Assault-rifle range `8` and existing weapon behaviour. | Military AI state, waves, drops. |
| `SO_Wave_Default` | Wave 3/4/5 military counts `2/3/4`; existing start delay, interval, and concurrency cap remain available. | Spawn-order algorithm, loadout/drop probabilities. |
| New `SO_MilitaryMob_Default` (`MilitaryMobConfig`) | Shotgun/assault references, shotgun chance `0.5`, burst shots `3`, telegraph `0.35`, reposition `1`, carry counts `2/1`, assembly/tether tuning, drop chance `0.33`, weapon-vs-gold split `0.5`, weapon ammo `24/6`, gold `10`, and beam reference/colour if required. | General player, police, traveller, or Pot rules. |

`MilitarySpawner` owns the `MilitaryMobConfig` reference and passes one resolved loadout plus the shared config to each rented controller. No new service, factory, event bus, generic behaviour-tree, or runtime settings menu is needed.

### Runtime responsibilities

| Component | Responsibility | Must not do |
| --- | --- | --- |
| `MilitaryMobConfig` | Stores all military-only designer tuning in one asset. | Own runtime state or scene references beyond assigned assets. |
| `MilitarySpawner` | Rents a military mob, rolls its permanent loadout, configures its launch/impact pools, and passes config/runtime dependencies. | Decide firing phases, carry coordination, or death outcomes. |
| `MilitaryController` | Owns its combat phase, telegraph, burst count, strafe, assigned loadout, theft participation, and one death roll. | Alter shared weapon/projectile implementation or duplicate pooling. |
| `RunWaveController` | Rotates the next outstanding spawn type fairly and retains existing interval/concurrency gates. | Know military tuning or scene/pool details. |
| `PotController` | Remains the sole Pot movement/destruction owner. | Store military-team state or inspect concealment every frame. |

### Guardrails

- Resolve the loadout at spawn, never per shot, burst, or drop.
- Use existing `WeaponConfig.Range` for rifle shortening; do not add a second “firing cone length” field.
- A burst is three calls through the existing `RangedAttackController.TryFire`, governed by the weapon’s normal cooldown. It is not a three-projectile volley and does not bypass projectile pools.
- The aim line is presentation only: no collider, damage, hitscan, target lock, or new pool. Prefer a disabled child/variant on the already pooled military prefab so each live mob reuses its own instance.
- The strafe direction is chosen once per burst and must respect existing cover/pathing/bounds rules. Do not add a tactical navigation framework.
- Evaluate `ConcealmentZone` overlap only when a military theft team is about to form. Do not poll every visual grass/bush mesh or add a global zone manager.
- The paired leader may move the Pot only after the required participants are within configured assembly distance. The partner must remain alive and within configured tether distance; otherwise stop military carry, drop the Pot at its current world position, and both surviving mobs return to combat.
- Keep the existing Pot’s `CanBePickedUp` protection while military carry is active. A player must not take a Pot from a live carry.
- Apply one death reward roll at death, not at spawn. A successful reward picks exactly one branch. Reuse existing `WeaponPickup` and `GoldPickup` pools.
- Do not change police/traveller detection: all three current enemy configs already use engagement range `50`.
- Do not alter Synty/Epic Toon FX/PolygonParticleFX package source assets. All scene/prefab variants are project-owned.

## Phase 0 — Snapshot the live military baseline

### Goal

Verify the active scene wiring, usable visual asset, concealment colliders, pool capacity, and serialized defaults before changing code or data.

### Steps

1. Confirm Unity MCP is idle with `Assets/_Project/Scenes/MainArena_3D.unity` active. Record the working-tree checkpoint and Console baseline. Stop if Unity is unavailable; do not guess scene references from YAML.
2. Inspect the active `MilitarySpawner`, military pool, projectile/impact/weapon/gold pickup pools, `PF_Military`, and `RunDirector` references. Record pool prewarm/max values and the actual prefab visual hierarchy.
3. Inspect all live `ConcealmentZone` colliders in `MainArena_3D`. Confirm their trigger bounds correspond to the tall-grass/bush areas players perceive as concealment; record whether the Pot’s current collider/position is suitable for an overlap-at-start check.
4. Preview `FX_LazerBeam_01` and its materials in the target URP scene. Establish whether a project-local prefab variant can be tinted red, consistently scaled to weapon range, enabled/disabled safely, and sorted above ground. If it cannot, select an installed tracer/beam asset that can; do not create art or edit package files.
5. Confirm current weapon/drop/wave defaults, then record proposed initial asset values: rifle range `8`, Wave military counts `2/3/4`, shotgun chance `0.5`, burst `3`, telegraph `0.35`, reposition `1`, drop chance `0.33`, branch split `0.5`, gold `10`, ammo `24/6`.
6. Run the existing focused military/drop/wave tests before edits. Treat unrelated failures as baseline evidence, not permission to weaken tests.

### Acceptance criteria

- Exact active scene objects, pools, concealment colliders, and reusable beam path are identified.
- The chosen beam works visually in the active renderer without package edits.
- Baseline test/Console state and serialized values are documented.
- No code, scene, prefab, package, or ScriptableObject changes are made.

### Rollback

No implementation exists; retain only Phase 0 notes.

## Phase 1 — Add consolidated military tuning and loadouts

### Goal

Make the approved values designer-editable, assign a permanent 50/50 weapon at spawn, and preserve the existing combat launcher/pools.

### Code and asset steps

1. Add `MilitaryMobConfig` under `Assets/_Project/Runtime/Core/Config`. Keep it a small `ScriptableObject` with serialized/minimum-validated fields only; no methods beyond safe read-only properties/validation.
2. Create `SO_MilitaryMob_Default.asset` in the existing defaults folder. Assign `SO_Weapon_AssaultRifle_Default`, `SO_Weapon_Shotgun_Default`, and the selected project-local aim-telegraph variant. Enter the approved defaults from Phase 0.
3. Change `MilitarySpawner` from one fixed combat weapon to one `MilitaryMobConfig` reference. At `TrySpawnMilitary`, roll once with `Random.value`; pass the chosen `WeaponConfig` and shared military config into the rented controller. Preserve the existing projectile/impact/pickup pool setup.
4. Update `MilitaryController.Activate(...)` and reset/despawn paths to store/clear the assigned loadout and shared config. Configure existing `RangedAttackController` with the assigned loadout exactly once on activation.
5. Set assault-rifle `WeaponConfig.Range` to `8`. Leave cooldown, damage, projectile speed, spreads, and shotgun values unchanged.
6. Update active scene/prefab serialized references through Unity MCP. Do not rerun broad legacy setup scripts that might overwrite current 3D scene work.

### Automated verification

1. Add small EditMode tests for deterministic loadout boundary rolls: `0.0` resolves shotgun, a roll below `0.5` resolves shotgun, and `0.5+` resolves assault rifle (or document the exact inclusive boundary chosen and test it).
2. Test config validation/default fallbacks: missing weapon references prevent a spawn safely; no null weapon reaches `RangedAttackController.Configure`.
3. Retain/run weapon runtime, spread, projectile, pickup, and pool tests. Confirm the shotgun continues to fire its existing six-projectile volley and rifle one projectile.

### Manual verification

1. Spawn a representative sample of military mobs. Confirm both loadouts occur at roughly equal frequency and remain unchanged for each mob’s lifetime.
2. Confirm assault-rifle mobs close distance before firing at eight units, while shotgun mobs retain their shorter five-unit behaviour.
3. Confirm pooled military reuse does not retain the former loadout or aim-line state.

### Acceptance criteria

- All military-specific values are visible in one dedicated asset; weapon and wave values stay in their existing assets.
- Every military mob has one valid fixed loadout and uses its normal weapon mechanics.
- No new package, pool, projectile type, or generic configuration framework exists.

### Rollback

Remove `MilitaryMobConfig`, its asset/scene reference, and the loadout pass-through together; restore the former assault-rifle assignment.

## Phase 2 — Implement telegraphed burst and reposition combat

### Goal

Replace continuous military fire with readable, reusable three-shot bursts and actual movement between them.

### Code and visual steps

1. Add the smallest private combat phase/timers to `MilitaryController`: aim telegraph, firing burst, and reposition. Keep them separate from existing `MilitaryState` (`Combat`, `Stealing`, `Escaping`) rather than introducing a cross-game AI framework.
2. In Combat, when the target is valid, in range, and unobstructed, begin the telegraph. Aim/rotate exactly as existing combat does, enable the non-damaging beam, and size it to the current assigned weapon’s effective visible aim distance without changing collisions.
3. After `AimTelegraphSeconds`, disable the beam and make up to `BurstShotCount` successful existing `TryFire` calls. Count only successful shots so normal weapon cooldown/pool constraints remain authoritative; trigger the existing fire animation for each successful shot.
4. Once three shots complete, start `RepositionSeconds`. Choose left/right once, move at existing military move speed along the lateral direction, apply existing separation, and keep the character oriented appropriately. Do not fire during this interval.
5. If target validity, line of sight, weapon config, death, stealing, escaping, despawn, or pool return interrupts a phase, clear timers/counts and disable the beam. The next valid Combat encounter begins cleanly.
6. Create a project-owned aim-telegraph child/variant under `PF_Military` based on Phase 0’s selected asset. It must be inactive by default, reset on pool spawn/despawn, have no gameplay collider, and use the approved red presentation.

### Automated verification

1. Add a focused pure/helper test for phase transitions: telegraph → exactly three successful fire opportunities → reposition → telegraph; verify interruption/reset cannot leave the telegraph active.
2. Test that burst count is independent of shotgun projectile count; three shotgun trigger pulls are still three shots, not eighteen “burst shots.”
3. Retain/run existing weapon cooldown, hit, spread, and projectile pool tests. The military change must not permit a shot before `WeaponRuntimeState` allows it.

### Manual verification

1. Watch both loadouts in an unobstructed encounter. Confirm 0.35-second red aim line, three visibly spaced shots, one-second lateral motion, then repeat.
2. Test blocked line of sight, target death/disable, Pot theft transition, and military death during every phase. Confirm no invisible firing and no stuck line.
3. Confirm aim line is readable against grass and ground, but never damages, collides, or persists after pooling.

### Acceptance criteria

- Both loadouts visibly telegraph and fire exactly three trigger pulls per burst.
- Reposition is visibly lateral and non-firing for one second.
- Existing projectile/hit/pool mechanics and animation triggers continue to own their responsibilities.

### Rollback

Remove the private burst phase/telegraph child and return Combat to its former `TryFire` condition. Leave assigned-loadout work intact if desired.

## Phase 3 — Implement paired and concealed Pot carries

### Goal

Make standard Pot theft require a stable two-mob team, while retaining the one-mob concealment exception and existing Pot ownership protections.

### Code steps

1. At the existing switch-to-stealing seam, resolve the nearest eligible gold-bearing Pot as today. At attempted pickup, determine once whether its world position overlaps any active `ConcealmentZone.ZoneCollider`.
2. For a concealed Pot, allow one eligible military mob to become the leader and begin carry immediately. For a non-concealed Pot, reserve/assemble exactly two eligible military mobs targeting the same Pot; do not call `SetMilitaryCarryActive(true)` until both are within the configured assembly distance.
3. Keep the existing leader/follower arrangement as the minimal carry representation: the leader updates Pot position and heads to the nearest arena edge; the partner follows within the configured tether distance. Do not invent a separate physics carry rig, shared state manager, or additional Pot component.
4. While a Paired Carry is active, validate the partner every frame: it must remain active, alive, assigned to the same Pot, and within tether distance. If validation fails, call the existing Pot military-carry release, leave the Pot at its current position, clear both assignment fields, and return all survivors to Combat.
5. If the leader dies/despawns, use the same release routine. If the partner dies/despawns, the leader must release rather than continue alone. Concealed Solo Carry has no partner requirement.
6. Retain existing nearest-edge escape, movement speed, `DestroyByMilitary`, active-carrier pick-up lockout, and kill/outcome paths. Do not alter civilian theft, player carry, Pot harvesting, or concealment art.

### Automated verification

1. Add focused carry-coordination tests for: non-concealed Pot waits for two assembled military; concealed Pot starts with one; standard leader cannot exit with absent/dead/tether-broken partner; and loss releases the Pot exactly once.
2. Retain/run `PotControllerCarryTests`, Pot registry/placement/harvest tests, military pool tests, and run-outcome tests. Verify Pot Gold/harvest state is unchanged by a released theft.
3. Add a narrow `ConcealmentZone` overlap test against a trigger bound if the existing fixtures allow it. Test the decision only at pickup attempt, not ongoing overlap changes.

### Manual verification

1. Place a gold-bearing Pot outside concealment with one military alive: it must not move. With two, both assemble before carry begins.
2. Kill or separate either member during the paired escape: Pot drops at its current location, surviving mob returns to combat, and the player can pick it up again.
3. Place a Pot inside a verified concealment zone: one military completes the full carry, even after leaving the zone.
4. Confirm military cannot steal destroyed, carried, empty, or already-stolen Pots; test map-edge destroy only after a valid active carry.

### Acceptance criteria

- Standard theft never begins or completes with one military mob.
- Concealed Solo Carry is determined at theft start and works independently of foliage visuals.
- A broken pair safely releases, rather than destroys, the Pot.

### Rollback

Revert team assembly/tether fields and restore the previous one-leader/escort logic as one unit. Keep `ConcealmentZone` scene content untouched.

## Phase 4 — Tune waves and exclusive death drops

### Goal

Increase and interleave military pressure in later waves, while making military rewards predictable, pooled, and non-duplicated.

### Code and asset steps

1. Update `SO_Wave_Default`: military counts Wave 3 = `2`, Wave 4 = `3`, Wave 5 = `4`. Preserve existing start delays, intervals, and concurrency caps unless Phase 0/playtest identifies a demonstrated bottleneck.
2. Replace the two-type `_preferTraveller` branch in `RunWaveController` with the smallest rotating next-type state that skips exhausted types. Its sequence for a fully mixed wave is Traveller → Police → Military; when a type is exhausted it advances to the next available type. Preserve all current gating and exact count accounting.
3. Replace military’s independent pre-resolved weapon and gold drops with one clearly named outcome selection at death: no reward for 67%; otherwise 50% assigned weapon plus its configured ammo or 50% configured gold amount. Reuse `WeaponPickup` and `GoldPickup` pools.
4. Keep the selected loadout stored on `MilitaryController` so a successful weapon reward always matches the active shotgun/rifle. Do not use the old generic weapon-drop list for this feature once the dedicated config is authoritative.
5. Keep gold pickup deposit behaviour unchanged: collected gold enters Pot Gold through the existing route.

### Automated verification

1. Extend `RunWaveControllerTests` to prove three-type round-robin order, type skipping after exhaustion, exact 2/3/4 military totals, spawn interval preservation, and max-concurrency stalling/resume.
2. Add deterministic death-drop utility tests for boundaries: no reward before `0.33`; weapon/gold branch at the configured split; weapon reward contains assigned rifle/24 or shotgun/6; gold reward is 10; never both.
3. Retain/run existing `EnemyDropSelectionUtilityTests`, weapon pickup, gold pickup/economy, pooling, and run-stat tests. Preserve police/traveller drops.

### Manual verification

1. Play Waves 3–5 and record the spawn order/timing. Confirm military enters among travellers and police rather than arriving as the final block.
2. Confirm the concurrency cap staggers sustained pressure instead of exceeding its configured ceiling.
3. Defeat a representative sample of both military loadouts. Confirm approximately one third produce exactly one pickup, matching weapon drops use correct ammo, gold drops are 10, and two simultaneous rewards never occur.
4. Pick up gold and weapon rewards; confirm Pot Gold/HUD and ammo/weapon UI follow their existing rules.

### Acceptance criteria

- Waves use approved counts and true three-type interleaving.
- Every military death yields zero or one configured pooled reward, never both.
- Other enemy wave/detection/drop behaviours remain unchanged.

### Rollback

Restore the previous wave counts/scheduler branch and independent military drop fields together. Do not revert unrelated police/traveller logic.

## Phase 5 — Full regression, handoff, and graph refresh

### Goal

Prove the complete military feature in the real run loop and leave a precise record for subsequent work.

### Required automated checks

1. Run all new focused loadout, burst, carry, concealment, drop, and wave-order tests.
2. Run existing relevant EditMode suites: combat/weapon/pool tests, Pot carry/placement/harvest/registry tests, enemy drop tests, and run-wave/director tests.
3. Run the relevant broader EditMode suite after focused checks are green. Separate known baseline failures from feature regressions; fix every new regression.
4. After every script edit, wait for compilation and inspect Console errors/warnings. Before final handoff, inspect the active scene, prefab overrides, and Console one final time.

### Required manual playthrough

1. Spawn/observe multiple military mobs: verify balanced permanent loadouts, rifle range reduction, telegraph, three shots, one-second strafe, pooling reset, and normal hit feedback.
2. Test Pot theft outside/inside concealment, pair assembly, partner loss at several points during escape, player recovery after release, and final edge destruction on valid carries.
3. Play Waves 3–5 from fresh runs with an active/harvesting Pot. Verify `2/3/4` military counts, three-type stagger, existing interval/concurrency gate, and no failed spawn loops.
4. Test at least 30 military deaths across loadouts if practical. Verify the 33% exclusive reward rule, reward contents, Pot Gold deposit, and no duplicate/persistent pooled pickups.
5. Capture a representative gameplay screenshot/video showing the red telegraph, burst/reposition, and two-mob Pot carry for client review.

### Completion criteria

- Approved values are inspector-editable in their intended assets.
- Loadout, combat, wave, carry/concealment, and drop rules behave exactly as approved.
- New focused tests and relevant regressions pass; manual smoke tests pass; no new Unity Console errors/warnings exist.

### Handoff steps

1. Add per-phase recaps with exact changed code/config/scene/prefab/beam-variant paths, serialized default values, test results, manual results, and Console state.
2. Keep `CONTEXT.md` limited to confirmed gameplay language: Concealment Zone, Paired Carry, Concealed Solo Carry, Military Loadout, Military Burst, Military Aim Telegraph, and Military Death Drop.
3. Refresh Graphify incrementally after all feature files are final. Verify it connects `MilitaryMobConfig`, `MilitarySpawner`, `MilitaryController`, `RangedAttackController`, `RunWaveController`, `ConcealmentZone`, `PotController`, and the pickup pools.
4. Inspect the exact diff, run a whitespace check, stage only military-feature documents/code/config/tests, and checkpoint one focused change. Preserve unrelated user work and generated graph cache changes.

## Explicitly deferred

- Visible 3D weapon model swaps, weapon-specific military animations/audio, reload mechanics, ammunition pickups, loot rarity, or multiple drop rewards.
- A generic AI state machine/behaviour tree, navigation rewrite, squad manager, Pot carry physics rig, global concealment registry, or event bus.
- Player-facing tuning/settings UI, difficulty scaling beyond the approved wave values, changes to police/traveller detection, or changes to their drop systems.
- New art/VFX packages, package-source edits, gameplay effects from the laser, or a new aim/laser pooling system.
