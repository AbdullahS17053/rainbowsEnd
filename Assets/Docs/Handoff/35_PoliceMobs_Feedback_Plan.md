# Police Mobs Feedback Implementation Plan

Status: Approved for implementation

## Client request

Make police mobs visibly run to the player, pause and raise their weapon before firing, then reposition before firing again. Give every police mob an equal pistol/shotgun loadout chance, increase staggered police spawns in later waves, and provide a controlled 33% death-drop chance.

## Approved decisions

- **Police Combat Cycle:** run to weapon-specific preferred range; stop and hold the armed aim pose for **0.25 seconds**; make one normal weapon attack; then run/strafe for **0.75 seconds** before repeating. Police cannot fire while moving.
- Reuse the proven `Military.controller` Animator wiring and `CharacterAnimationBridge` on the police visual. It already exposes `Speed` and `Fire`. The armed idle during the 0.25-second hold is the raise/aim presentation; do not author new animation clips or a new Animator controller.
- Every police mob receives one permanent spawn loadout: **50% pistol / 50% shotgun**. The held Synty mesh changes to match the assigned loadout and a weapon reward, if any, matches it too.
- **Police Death Drop:** one **33% total** roll at death. A successful roll creates exactly one reward: 50% equipped weapon with ammo, or 50% **10 Pot Gold**. Weapon pickup ammo is **12 pistol** or **6 shotgun**. The other 67% creates no reward. No ammo-only pickup is added.
- Police totals are **0 / 0 / 3 / 4 / 5** in Waves 1–5. Keep the existing wave interval, concurrency caps, and three-type round-robin scheduler; it already staggers entrances.
- Add a focused Inspector-tunable `PoliceMobConfig` ScriptableObject. Existing `EnemyConfig`, `WeaponConfig`, and `WaveConfig` retain movement, weapon-stat, and wave ownership.

## Existing implementation facts

- `PoliceController` currently paths toward the player, stops at `WeaponConfig.Range * 0.7`, and calls `RangedAttackController.TryFire(...)` continuously whenever the player is valid, visible, and in range. That produces stationary fire and has no combat-cycle state.
- `CharacterAnimationBridge` writes Rigidbody velocity magnitude to an Animator `Speed` float and exposes `TriggerFire()`. `PF_Police` has the bridge and a Synty police visual child, but its animation wiring is absent/incomplete in the active prefab.
- `Assets/_Project/Art/Animators/Military.controller` and `Police.controller` both use the compatible `Speed` float and `Fire` trigger. `Military.controller` is the approved source to reuse; it must not be edited for this feature.
- `PF_Police` already holds `SM_Wep_Pistol_01`. Installed Synty assets also include `Assets/Synty/PolygonMilitary/Prefabs/Weapons/SM_Wep_Shotgun_01.prefab`.
- `PoliceSpawner` currently assigns one fixed pistol config and resolves generic weapon drops at spawn. `WeaponPickup` is already a combined weapon-and-ammo pooled pickup. `GoldPickup` is already pooled and deposits collected value into Pot Gold.
- `SO_Weapon_Pistol_Default` and `SO_Weapon_Shotgun_Default` already own projectile count, range, cooldown, spread, damage, and muzzle offset. Preserve their values; a shotgun attack is its existing six-projectile volley, not six Police Combat Cycle shots.
- `SO_Wave_Default` currently uses police counts `0/0/1/2/3`. `RunWaveController` already rotates Traveller → Police → Military among types that still have unspawned entries and enforces the serialized interval/concurrency limits.

## Target design

### Configuration ownership

| Owner | Tunable values | Must not own |
| --- | --- | --- |
| New `SO_PoliceMob_Default` (`PoliceMobConfig`) | Pistol/shotgun references, pistol chance `0.5`, aim hold `0.25`, reposition `0.75`, drop chance `0.33`, weapon-versus-gold split `0.5`, pistol/shotgun reward ammo `12/6`, gold `10`, held pistol/shotgun prefab references. | General combat/projectile logic, wave totals, player settings. |
| Existing `SO_Enemy_Police_Default` | Police health, move speed, separation, detection/engagement range. | Loadout odds, combat-phase timers, rewards. |
| Existing pistol/shotgun `WeaponConfig` assets | Range, damage, cooldown, projectile count, spread, and projectile presentation. | AI decisions, death rewards. |
| Existing `SO_Wave_Default` | Police counts `0/0/3/4/5`, start delays, interval, concurrency. | Spawn-order algorithm, combat phases. |

`PoliceSpawner` owns the shared police config, rolls one loadout when it rents an instance, configures existing pools, and passes resolved data to `PoliceController`. `PoliceController` owns only its transient combat phase, selected loadout, held-mesh state, and one death outcome. Do not introduce a behaviour tree, generic enemy framework, event bus, new projectile type, or new pool.

### Police Combat Cycle

1. **Approach:** path toward the player until the current weapon's existing preferred range. Apply current separation and movement speed. Velocity drives `Speed`, so the model visibly runs.
2. **Aim hold:** stop movement, face the player, and hold the existing armed idle for `AimHoldSeconds` (default 0.25). Validate target, line of sight, health, and weapon throughout.
3. **Fire:** make exactly one successful call to existing `RangedAttackController.TryFire(...)`; call `CharacterAnimationBridge.TriggerFire()` only for that successful call. Pistol means one projectile; shotgun retains its configured volley.
4. **Reposition:** choose one lateral direction once and move/strafe for `RepositionSeconds` (default 0.75), still respecting separation, arena/pathing/cover fallback rules. Do not fire in this phase.
5. Reset to Approach if the player is invalid, obstructed, disabled, dead, the police mob dies/despawns, or pooling reactivates the object. Never leave stale loadout, timer, velocity, or held-mesh state on a pooled instance.

## Guardrails

- Roll a loadout only once per spawn. Never reroll per frame, shot, reposition, or death.
- Do not alter the shared `RangedAttackController`, `WeaponRuntimeState`, projectile launcher, damage, hit, or pooling behaviour. It remains authoritative for weapon cooldown and volleys.
- Count only a successful `TryFire` as the Fire step; failed cooldown/pool attempts remain in Aim Hold or retry safely instead of starting reposition without a shot.
- Reuse project-owned Animation/weapon assets. Do not edit Synty package assets or either shared Animator controller. Add/enable project-prefab children/references only.
- Use the existing police Rigidbody velocity to drive `Speed`. No transform-only movement and no animation event is required for gameplay timing.
- Held pistol/shotgun objects are inactive by default or reset deterministically on pool spawn/despawn. Exactly one is active while a police mob is live.
- Apply the drop chance once inside death handling. A successful reward selects exactly one branch; weapon and gold can never both appear.
- Keep the current police target (the player), player detection rules, civilian/military behaviour, Pot theft rules, and HUD/economy flow unchanged.

## Phase 0 — Baseline and wiring snapshot

### Goal

Record the active 3D-scene police setup and prove the reusable animation/weapon/pool references before edits.

### Steps

1. Use Unity MCP to confirm `Assets/_Project/Scenes/MainArena_3D.unity` is active, Editor is idle, and no compilation/domain reload is pending. Capture the Console error/warning baseline and working-tree checkpoint.
2. Inspect the active `PoliceSpawner`, `RunDirector`, police/projectile/impact/weapon/gold-pickup pools, `PF_Police`, `PF_Military`, and the wave asset. Record serialized references plus pool prewarm/max capacity. Do not rely on YAML alone for scene wiring.
3. Inspect `PF_Military`'s Animator and `CharacterAnimationBridge` references, then compare it to `PF_Police`. Confirm `Military.controller` runs correctly on the police Synty humanoid rig and `Speed`/`Fire` are the parameter names actually used.
4. Inspect the police hand hierarchy and preview the Synty pistol/shotgun prefabs. Identify the existing pistol hand attach point, the shotgun's safe local transform, and which objects must toggle per loadout. Do not edit package-source prefabs.
5. Record baseline values: police counts `0/0/1/2/3`; selected pistol/shotgun weapon values; default police move/detection values; active pool sizes; existing focused test results.
6. Run focused pre-change tests: `EnemyDropSelectionUtilityTests`, `RunWaveControllerTests`, relevant weapon runtime tests, and police/enemy tests. Treat unrelated failures as baseline evidence only; do not weaken tests.

### Acceptance criteria

- The live scene objects, pool capacities, animator source, rig compatibility, and held-weapon attachment point are identified.
- Baseline Console/test status and serialized values are recorded.
- No code, prefab, scene, package, or ScriptableObject changes are made.

### Rollback

No implementation exists; retain Phase 0 notes only.

## Phase 1 — Police tuning, fixed loadouts, and visible weapons

### Goal

Create designer-owned police tuning and give every rented police mob a stable, visible pistol or shotgun loadout.

### Code and asset steps

1. Add `PoliceMobConfig` under `Assets/_Project/Runtime/Core/Config`. Keep it a small serialized `ScriptableObject` with safe properties/validation only. Include the approved loadout, timing, reward, ammo, gold, and held-prefab references—nothing shared or speculative.
2. Create `Assets/_Project/ScriptableObjects/Defaults/SO_PoliceMob_Default.asset`. Assign `SO_Weapon_Pistol_Default`, `SO_Weapon_Shotgun_Default`, and project-owned held pistol/shotgun references. Enter the approved defaults from the table above.
3. Update `PoliceSpawner` to hold `PoliceMobConfig`. In its existing rent path, roll one `Random.value` boundary-safe 50/50 selection, derive matching reward ammo, configure existing projectile/impact pools, and pass the selected `WeaponConfig`, selected held-weapon identity, and shared config to the rented controller.
4. Update `PoliceController.Activate(...)` and pool lifecycle methods to store/clear its selected loadout and config, then configure the existing `RangedAttackController` once per activation with the selected weapon.
5. Add project-owned pistol and shotgun visual children/references at the existing police right-hand attach point. Preserve compatible local transform values from Phase 0; enable only the selected one. The original pistol must no longer remain visible on shotgun police.
6. Copy the exact working Animator/bridge references from `PF_Military` to `PF_Police` through Unity MCP. Use `Military.controller` and its existing Speed/Fire contract; do not duplicate or modify controller assets.
7. Set the new config reference and pool references on the live `MainArena_3D` police spawner. Avoid broad legacy setup scripts that can overwrite current 3D scene work.

### Automated verification

1. Add one focused pure/helper EditMode test for 50/50 loadout boundaries: a roll below 0.5 selects pistol; 0.5 and above selects shotgun (or document/test the exact chosen inclusive boundary).
2. Test safe spawn failure when neither usable weapon is configured: the rented object returns to its pool and no null weapon reaches `RangedAttackController.Configure`.
3. Test a resolved weapon reward carries the selected pistol/12 or shotgun/6 ammo. Retain existing projectile, pickup, and pool tests.

### Manual verification

1. Spawn a representative sample of police. Verify near-even pistol/shotgun frequency, stable per-life loadouts, correct fire behaviour, and matching held mesh.
2. Recycle pooled police repeatedly. Verify no previous shotgun/pistol mesh, config, velocity, or Animator state leaks to the next renter.
3. Verify `Speed` animates while the Rigidbody moves and `Fire` triggers once for a successful attack.

### Acceptance criteria

- All police-only tuning is visible in one Inspector asset.
- Every live police mob has one valid permanent loadout and exactly one matching held mesh.
- Existing weapon/pickup/pool systems remain reused and unchanged.

### Rollback

Remove the police config reference, pass-through, and held visual children as one change; restore the fixed pistol assignment.

## Phase 2 — Run, aim, fire, reposition combat

### Goal

Replace continuous stationary police fire with the approved readable movement cycle and actual animation-driven presentation.

### Code steps

1. Add the smallest private phase/timer state to `PoliceController`: `Approach`, `AimHold`, and `Reposition`. Keep it local to police rather than changing a shared enemy state system.
2. Preserve the existing path refresh, separation, player validation, cover/line-of-sight test, and preferred-range calculation. During Approach, assign run velocity toward the player; the already-wired bridge drives the animation Speed float.
3. When valid range/visibility is reached, clear run velocity, face the target, start the 0.25-second Aim Hold, and retain armed idle. No projectile is launched during this hold.
4. At the end of Aim Hold, call existing `TryFire` once. If it succeeds, trigger existing `Fire` once and enter Reposition. If cooldown/pool availability rejects it, keep a safe aim state and retry without advancing the cycle.
5. At Reposition start, choose left/right once. For 0.75 seconds, move laterally at configured police move speed plus bounded existing separation. Reuse current path/cover fallback so police do not deliberately drive through hard obstacles. Do not call `TryFire` in this phase.
6. On invalid target, blocked line, disable/death, despawn, or pool reset: clear phase timers/direction/path and zero velocity. Return to Approach only when the normal player-validity conditions resume.
7. Keep `visualRoot` facing the target during Aim Hold/Fire and movement facing during run/reposition as established by the compatible bridge/prefab setup. Do not add an Aim Animator parameter, animation events, a new clip, or a new controller for this request.

### Automated verification

1. Add a focused testable helper/state test proving: Approach reaches Aim Hold; Aim Hold prevents fire until 0.25 seconds; one successful shot begins Reposition; Reposition prevents firing until 0.75 seconds; an interruption resets cleanly.
2. Test that a shotgun's existing multi-projectile volley is one cycle fire action, not multiple Police Combat Cycle attacks.
3. Retain/run `WeaponRuntimeState` cooldown, projectile, hit, and pool tests. Confirm the cycle never bypasses the normal weapon cooldown.

### Manual verification

1. Observe both police loadouts in an open arena. Confirm visible running, a readable stop/armed aim hold, one attack, then visible lateral movement before the next attack.
2. Test cover blockage, target disable/death, obstacle navigation, close-range player movement, police death in every phase, and pool reuse. Confirm no sliding, continuous fire, stuck aiming, or stale velocity.
3. Compare animation pace and physical movement. Tune only `SO_Enemy_Police_Default.MoveSpeed`, `AimHoldSeconds`, or `RepositionSeconds` if needed; do not bake tuning values into code.

### Acceptance criteria

- Police visibly run and reposition; they never fire while moving.
- Every completed cycle has one normal pistol shot or one normal shotgun volley after its aim hold.
- Targeting, cover, weapons, damage, and pooling retain their existing ownership.

### Rollback

Remove the three private combat phases/timers and restore the former continuous-fire branch. Keep Phase 1 loadouts if independently desired.

## Phase 3 — Later-wave police totals and exclusive rewards

### Goal

Increase later-wave police pressure while preserving staggered entrances and make police rewards controlled, pooled, and unambiguous.

### Code and asset steps

1. Update the active wave asset through Unity MCP: police totals Waves 1–5 = `0, 0, 3, 4, 5`. Leave start delay, spawn interval, and max concurrency at their current authored values unless Phase 4 playtesting demonstrates a specific bottleneck.
2. Do not change `RunWaveController` scheduling logic: it already round-robins Traveller → Police → Military and enforces interval/concurrency gates, which is the approved stagger behaviour.
3. Replace police's pre-resolved generic weapon drop with a clear death-time result: roll against `PoliceMobConfig.DeathDropChance`; if successful, choose weapon-vs-gold by its configured split. Use the controller's assigned loadout for matching weapon/ammo.
4. Reuse the existing `WeaponPickup` and `GoldPickup` pools. A weapon reward carries its configured ammo; a gold reward is 10 and follows the existing Pot Gold deposit flow.
5. Reset any per-life reward/loadout fields in all police pooling lifecycle hooks. Ensure death/return can execute once only, exactly as current `_deathHandled` guards intend.

### Automated verification

1. Extend `RunWaveControllerTests` only where asset/default coverage needs it: assert exact police totals `0/0/3/4/5`, existing interval behavior, existing max-concurrency stall/resume, and existing three-type rotation. Do not rewrite the scheduler.
2. Add deterministic reward-selection boundary tests: no reward outside 0.33; successful reward produces exactly one branch; branch matching uses pistol/12 or shotgun/6; gold equals 10; weapon and gold never both spawn.
3. Run existing enemy-drop, weapon pickup, Gold Pickup/Pot Gold, wave/director, projectile, and pooling tests. Preserve military/traveller drops exactly.

### Manual verification

1. Play fresh runs through Waves 3–5 with harvesting Pot Gold available. Confirm police counts 3/4/5, mixed spawn order, and individual stagger rather than a police burst.
2. Defeat a representative sample of both police loadouts. Confirm roughly one third create exactly one reward, the held/drop weapon matches, reward ammo is correct, gold is 10, and no pooled pickup persists.
3. Collect rewards. Confirm weapon/ammo HUD behavior and Pot Gold deposits retain their current rules.

### Acceptance criteria

- Waves contain the approved police totals and retain existing stagger/concurrency behaviour.
- Each police death yields zero or one pooled reward, never two.
- Police changes do not alter military, traveller, Pot theft, or economy behavior.

### Rollback

Restore police wave counts `0/0/1/2/3` and reinstate prior police drop fields together. Leave the shared scheduler and unrelated enemy data untouched.

## Phase 4 — Regression, documentation, and handoff

### Goal

Verify the complete feature in the actual run loop and leave a reliable implementation record.

### Required automated checks

1. Run all new focused loadout, cycle, reward, and wave-value tests.
2. Run relevant existing EditMode suites: police/enemy, weapon runtime/projectile, pickup/economy, pool, and run-wave/director tests.
3. Run the full EditMode suite. Fix every new regression; clearly separate unrelated pre-existing failures from feature results.
4. After every script edit, wait for Unity compilation and inspect Console errors/warnings. Before handoff, inspect `PF_Police`, the active `PoliceSpawner`, the active wave asset, and Console one final time.

### Required manual smoke test

1. Spawn at least ten police in a representative run. Confirm close-to-even loadout distribution, mesh/loadout matching, armed aim hold, one fire action, run/reposition, and clean pooling reset.
2. Test pistol and shotgun at range, against cover, while the player moves, and during death/despawn. Verify no sliding, stationary continuous firing, or null-reference/animation errors.
3. Play Waves 3–5 and verify totals 3/4/5 with the pre-existing stagger and concurrency gate.
4. Collect multiple weapon and gold rewards. Verify the 33% exclusive rule, ammo/gold values, Pot Gold deposit, and no duplicate pickups.
5. Capture a representative gameplay screenshot/video for client review showing a police run → aim → fire → reposition cycle.

### Completion criteria

- Approved settings are Inspector-editable in `PoliceMobConfig` and their established existing config assets.
- Police visibly run, aim, fire, and reposition exactly as approved.
- Loadout, wave, and reward rules match the approved numbers.
- Focused and regression tests pass, manual smoke passes, and no new Unity Console errors/warnings exist.

### Handoff steps

1. Add phase recaps with exact changed paths, serialized defaults, test results, manual results, and final Console state.
2. Update `CONTEXT.md` with only confirmed terms: Police Combat Cycle, Police Loadout, and Police Death Drop. Keep it gameplay-language only, not implementation detail.
3. Refresh Graphify incrementally after implementation. Verify it connects `PoliceMobConfig`, `PoliceSpawner`, `PoliceController`, `CharacterAnimationBridge`, `RangedAttackController`, `RunWaveController`, `WeaponPickup`, and `GoldPickup`.
4. Inspect the exact diff and whitespace, preserve unrelated user work, and checkpoint only this feature's code/assets/tests/docs when requested.

## Explicitly deferred

- New police animation clips, an Aim Animator parameter, reloads, audio, muzzle VFX, red aim lasers, weapon-specific AI tactics, or weapon rarity.
- Ammo-only rewards, multiple simultaneous rewards, new pickup types, new pools, a generic drop framework, or a generic enemy state machine/behaviour tree.
- Changes to player detection, police health/move speed/weapon damage, civilian/military behavior, Pot theft, maps, or package source assets.
- Difficulty scaling, wave timing/cap changes, or runtime/player-facing tuning UI beyond the approved Inspector assets.
