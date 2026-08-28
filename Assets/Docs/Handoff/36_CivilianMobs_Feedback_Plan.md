# Civilian Mobs Feedback Implementation Plan

Status: Approved for implementation

## Client request

- Show the amount a civilian takes from a Pot of Gold above that civilian and make the Pot Gold counter fall by the same amount.
- Replace civilian sliding with a credible running/chasing presentation when a player carries the Pot of Gold. Civilians should close on the Pot, with a modest speed increase.
- Give civilians a visible carry/bag escape presentation after they steal gold.
- Spawn several more civilians per wave while retaining staggered entrances.

## Approved decisions

- **Civilian Theft Display:** show a civilian's cumulative whole stolen Pot Gold above its head while it is draining and fleeing. It resets on pool reuse. Use the existing yellow world-text treatment and `g` suffix; do not create floating per-tick numbers, a Canvas, or a second currency.
- Pot Gold already decreases at the source: `PotDrain.Tick` calls `PotController.DrainGold`, while `EconomyHudPresenter` reads the live combined value from `PotRegistry`. Preserve that ownership; prove it with tests and play-mode verification instead of adding another deduction path.
- **Civilian Carried-Pot Chase:** when a civilian's selected Pot is carried by the player, it visibly runs using `Assets/_Project/Art/Animators/Run_1.fbx`. The Pot remains the target; reaching it retains ordinary contact draining. There is no player damage, knockback, grab, or new tackle mechanic.
- Civilian `MoveSpeed` changes from `2.5` to `3.0` in `SO_Enemy_Traveller_Default`. Keep the existing `FleeSpeedMultiplier` at `1.5`.
- **Civilian Loot Escape:** a civilian that stole a positive amount flees using `Assets/_Project/Art/Animators/Carry.fbx` with one existing bag prop visibly held in its raised arms. No stolen gold means no bag. The pooled instance must clear bag and Animator state on every new life.
- Create a project-owned civilian Animator controller by duplicating the existing compatible `Player.controller`; do not change `Player.controller`, Synty source assets, the shared animation bridge contract, or the player/police/military animator assets.
- Use the existing `SM_Prop_Bag_Explorer_01` as the initial bag candidate. Phase 0 confirms its scale and its safe hand/arm attachment; switch only to another already-imported bag/sack if it demonstrably does not fit the Carry pose.
- Civilian totals for Waves 1-5 are **6 / 8 / 7 / 8 / 9**. Preserve the existing wave start delays, intervals, concurrency caps, edge spawn points, and Traveller -> Police -> Military round-robin scheduler; these mechanisms already create staggered entrances.

## Existing implementation facts

- `PotDrain.Tick` resolves the overlapping Pot, removes gold through `PotController.DrainGold`, adds the exact result to `PotDrain.StolenGold`, and exposes both its drain state and current Pot. It does not use the home tree or `EconomyState`.
- `EconomyHudPresenter` displays `PotRegistry.TotalStoredGold`, the floored sum of live Pots' `StoredGold`. A civilian drain therefore already lowers the HUD's source value in the same update cycle; fractional values are retained internally.
- `TravellerController` owns the pooled civilian's three states: `Seeking`, `Draining`, and `Fleeing`. It already targets the Pot transform, so a carried Pot moves the civilian's target with the player. It stops at Pot-contact distance, drains, then flees to the arena edge. Killing a civilian already returns its stolen amount as a pooled gold pickup.
- `PF_Traveller` already has a Rigidbody2D, `TravellerController`, `PotDrain`, a humanoid Synty visual, an Animator using `Player.controller`, and `CharacterAnimationBridge`. The bridge writes Rigidbody velocity magnitude to the existing `Speed` parameter and faces the visual in its movement direction.
- `Player.controller` has only Idle/Walk locomotion controlled by `Speed`; it is shared and must remain unchanged. `Run_1.fbx` and `Carry.fbx` are imported Humanoid assets, suitable to verify and add to a civilian-only controller.
- `EnemyConfig` already owns movement speed. `SO_Enemy_Traveller_Default` currently has MoveSpeed `2.5`, engagement range `50`, steal rate `1`, steal capacity `10`, and flee multiplier `1.5`.
- `SO_Wave_Default` currently has civilian totals `3 / 5 / 4 / 5 / 6`. `RunWaveController` enforces interval and maximum-concurrency gates and rotates all enemy types, so neither a new civilian spawner nor a new stagger system is needed.
- Existing focused coverage includes `PotDrainTests`, `TravellerTargetSelectionUtilityTests`, and `RunWaveControllerTests`; the latter already proves scheduling, concurrency, and three-enemy-type rotation.

## Target design and ownership

| Owner | Responsibility | Must not own |
| --- | --- | --- |
| `PotDrain` and `PotController` | The single source of truth for drain amount and Pot Gold removal. | HUD formatting, Animator selection, wave totals. |
| `TravellerController` | Civilian transient state, current Pot, theft-label visibility/value, Animator mode flags, and pool-safe visual reset. | A second economy balance, generic AI framework, spawn scheduling. |
| `PF_Traveller` and new project-owned civilian controller | World text child, inactive bag visual, Humanoid clip states, transforms, and Animator references. | Modifying source package models/controllers or gameplay timing. |
| Existing `CharacterAnimationBridge` | Velocity-to-`Speed` locomotion and facing. | New generic presentation state system. |
| `SO_Enemy_Traveller_Default` | Civilian MoveSpeed `3.0` and existing ordinary enemy tuning. | Per-animation or UI settings. |
| `SO_Wave_Default` | Civilian totals `6/8/7/8/9`. | Scheduling algorithm or pool behaviour. |
| `RunWaveController` and `EnemySpawner` | Existing stagger, concurrency, mixed-type rotation, and pooled rental. | Civilian-only spawn coroutine or duplicate queue. |

## Guardrails

- Never subtract the displayed number from Pot Gold a second time. A label is a projection of `PotDrain.StolenGold`; `PotController.DrainGold` remains the only civilian deduction.
- Do not display `0g`, stale text, a bag, or a Carry state on a newly rented civilian. Reset explicitly in every activation, despawn, disable, death, and fallback path that already participates in pooling.
- Do not change civilian target priority, Pot selection, direct-contact radius, steal rate/capacity, death gold-drop value, player health, home-tree logic, or other enemy factions.
- Keep the label a prefab-owned world-space TextMeshPro child. No per-frame allocation, screen-space Canvas, event bus, generic UI manager, new pool, new faction state machine, or new configuration asset is needed.
- Do not modify `Player.controller` or package-source props. Duplicate the controller under `Assets/_Project/Art/Animators/`, and add only project-prefab child/override references.
- The Carry bag is presentation only. It is not a pickup, does not alter the gold drop, and never persists after the civilian is returned to the pool.
- Retain the serialized wave interval and concurrency values even though total counts rise. Difficulty is increased by total pressure over the round, not an unbounded on-screen mob burst.

## Phase 0 - Baseline and asset-wiring snapshot

### Goal

Confirm live civilian setup, existing economy correctness, clip/rig compatibility, safe bag attachment, and current wave/pool values before making changes.

### Steps

1. Through Unity MCP, confirm `Assets/_Project/Scenes/MainArena_3D.unity` is active, the Editor is idle, and no compilation/domain reload is pending. Capture the current Console errors/warnings and working-tree checkpoint; preserve unrelated work.
2. Inspect the active `EnemySpawner`, `RunDirector`, `PF_Traveller`, traveller and gold-pickup pools, edge spawn points, `SO_Enemy_Traveller_Default`, and `SO_Wave_Default`. Record serialized pool capacities, current counts, active configuration references, and the existing 2.5/1.5 civilian movement values.
3. Run a controlled play-mode observation with a Pot containing known gold. Record Pot Gold before, during, and after one civilian drain; confirm the HUD is reading the live Pot total and no home-tree deposit is involved. Record the civilian's current visual/Animator state and its carried-Pot target behaviour.
4. Inspect `Run_1.fbx` and `Carry.fbx` in Unity's Animation Importer: identify the actual clip names, set/verify looping appropriate for continuous movement, confirm Humanoid retargeting on the traveller Synty avatar, and note any import warnings. Do not edit either FBX model source.
5. Inspect the traveller skeleton in prefab mode and preview `Carry` on the visual. Test `SM_Prop_Bag_Explorer_01` at the raised hands/forearms with a non-destructive nested child. Record a stable local transform; choose another existing bag/sack only if this prop clips badly or cannot read as carried money.
6. Inspect the existing Pot `GoldLabel` for its yellow TextMeshPro style and top-down readability. Capture its visible scale/position as the style reference for a traveller-owned label.
7. Run focused pre-change EditMode tests: Pot drain, traveller targeting, Pot registry/economy, wave scheduling, and pooling. Treat unrelated failures only as baseline evidence; do not weaken a test to make the baseline green.

### Acceptance criteria

- The active scene, actual pools, prefab hierarchy, current Console/test baseline, configuration values, clip names, and bag attachment are recorded.
- The direct Pot drain -> PotRegistry -> HUD path is demonstrated before any implementation edits.
- No code, scene, prefab, ScriptableObject, Animator controller, or package asset has changed.

### Rollback

No implementation exists; retain Phase 0 notes only.

### Phase 0 Recap - 2026-08-28

#### Verified Unity baseline

- Unity instance: `rainbowsEnd@8545bd2c`, Unity `6000.0.67f1`, Windows Editor.
- Active scene: `Assets/_Project/Scenes/MainArena_3D.unity`.
- Editor returned to idle Edit Mode after the observation; no compilation or domain reload was pending.
- The active `EnemySpawner` references `SO_Enemy_Traveller_Default`, `SO_Wave_Default`, `SO_Economy_Default`, `TravellerPool`, `GoldPickupPool`, and `SpawnPointsRoot`.
- `RunDirector` references the same wave config, all three faction spawners, `PotRegistry`, and `RunOutcomePanel`.
- `TravellerPool` uses `PF_Traveller`, prewarms `6`, and has max size `64`. `GoldPickupPool` uses `PF_GoldPickup`, prewarms `6`, and has max size `64`. Existing police and military pools each prewarm `2` with max size `64`.
- `SpawnPointsRoot` has the existing player/unicorn/Pot markers, four enemy edge markers (`North`, `South`, `East`, `West`), and the Fae tree marker.

#### Current civilian setup

- `PF_Traveller` currently contains the existing Rigidbody2D, CircleCollider2D, Health, PotDrain, TravellerController, and CharacterAnimationBridge components.
- The traveller visual resolves the shared `Assets/_Project/Art/Animators/Player.controller`; no civilian-only controller, theft label, or bag child exists yet.
- `Player.controller` currently exposes `Speed` and `Fire`, with `Idle`, `Walk`, and `Shoot` states. It remains shared and is unchanged.
- `SO_Enemy_Traveller_Default` is still `MoveSpeed 2.5`, `FleeSpeedMultiplier 1.5`, `StealRate 1`, `StealCapacity 10`, `EngagementRange 50`, `TargetRefreshSeconds 0.25`, `SeparationRadius 2`, and `SeparationStrength 1`.
- `SO_Wave_Default` civilian totals remain `3 / 5 / 4 / 5 / 6`; serialized start delays, intervals, concurrency caps, police totals, and military totals remain unchanged.

#### Economy observation

A runtime-only observation stored `20` gold through the existing Pot API. The result was `PotController.StoredGold 20`, `PotRegistry.TotalStoredGoldValue 20`, and HUD source `20`, with no home-tree deposit path involved. After a civilian reached the Pot, the result was `Pot 19.566`, registry `19.566`, and that civilian's `PotDrain.StolenGold 0.434`; the values sum back to the original `20`. This confirms the current direct drain and live-HUD source are already aligned. No scene or asset was saved from this observation.

#### Animation and bag inspection

- `Run_1.fbx` imports as Humanoid with no importer warnings reported by Unity MCP. Its actual embedded clip is named `mixamo.com`, length `0.733333` seconds at `30` FPS, and currently reports `isLooping false`.
- `Carry.fbx` imports as Humanoid with no importer warnings reported by Unity MCP. Its actual embedded clip is also named `mixamo.com`, length `1.266667` seconds at `30` FPS, and currently reports `isLooping false`.
- The traveller skeleton exposes stable `Hand_L`, `Hand_R`, `Spine_03`, and `Head` paths. The candidate `SM_Prop_Bag_Explorer_01` is a visual mesh with bounds approximately `0.61 x 0.54 x 0.26`; it includes an enabled MeshCollider and should therefore have collision disabled when used as a carried visual. Its exact hand/arm local transform remains a Phase 2 prefab-preview check.
- The existing Pot `GoldLabel` remains the style reference: world-space TextMeshPro, centered, yellow, and positioned above the object.

#### Automated and Console baseline

- Focused EditMode run passed `28/28` tests across PotDrain, traveller target selection, PotRegistry, RunWaveController, and PrefabPool coverage.
- Unity Console contained only the Test Framework setup/result-save messages (`IPrebuildSetup`, `Saving results`, and `IPostBuildCleanup`); no compile error or civilian gameplay exception was reported.
- Temporary Phase 0 screenshots were removed after inspection. No code, scene, prefab, ScriptableObject, Animator controller, or package asset was changed.

#### Phase 0 result

Phase 0 acceptance criteria are met. Phase 1 can begin with the existing Pot drain as the sole economy source, the confirmed prefab/pool wiring, and the two imported clips queued for civilian-controller setup. The only deliberate follow-up from this baseline is the Phase 2 visual preview that establishes the final bag hand/arm transform and disables the candidate prop collider.

## Phase 1 - Theft display and verified live gold deduction

### Goal

Make each stealing/fleeing civilian visibly account for the same Pot Gold that the HUD loses, without creating another economy path.

### Code and prefab steps

1. Add one `TextMeshPro` child to `PF_Traveller` (for example `TheftLabel`) using the established Pot-label font treatment: yellow, centered, readable top-down scale, and a local head-height offset validated in Phase 0. Keep it inactive by default and prefab-owned.
2. Add the smallest serialized reference and private label-refresh code to `TravellerController`; do not create a new presenter/component. Format `Mathf.FloorToInt(potDrain.StolenGold)` as a cumulative `g` value.
3. Show the label only if the civilian has stolen at least one whole gold and is in `Draining` or `Fleeing`. The number updates only when its displayed whole value changes; it remains visible during Fleeing to make the escaping value legible.
4. Hide and clear the label in `Activate`, `OnSpawned`, `OnDespawned`, `OnDisable`, death, and any non-stealing/empty transition. Ensure a civilian killed while carrying stolen gold still uses the existing `SpawnGoldPickup(totalDrop)` path unchanged.
5. Do not edit `PotDrain.Tick`, `PotController.DrainGold`, `PotRegistry`, `EconomyHudPresenter`, `EconomyState`, shop spending, or home-tree code. The display consumes the existing `StolenGold` observation only.

### Automated verification

1. Extend `PotDrainTests` to assert an exact known drain lowers `PotController.StoredGold` and increases `PotDrain.StolenGold` by the same value; retain its current overlapping-Pot coverage.
2. Add one focused traveller lifecycle test covering the label rule: no display before a whole unit is stolen; the label shows the cumulative whole value while draining/fleeing; a pooled reset clears it and subsequent rent does not show stale gold. Use a minimal GameObject setup and existing components rather than a new test framework.
3. Run Pot drain, Pot registry, economy HUD/shop, traveller target selection, gold pickup, and pooling tests. Confirm spending still reads the same combined live Pot value as the HUD.

### Manual verification

1. Put a known integer amount in a Pot and let one civilian drain it. Verify each overhead number increment matches the equivalent Pot Gold decrease, including the HUD's floor behaviour for fractions.
2. Stop the drain by moving/picking up the Pot, killing the civilian, emptying the Pot, and allowing it to escape. Verify visibility and reset rules in all cases, plus existing gold-drop recovery on death.
3. Rent, drain, flee, return, and re-rent the same pooled civilian repeatedly. Verify no label, text, or prior amount leaks into the new life.

### Acceptance criteria

- One civilian's cumulative visible theft equals the Pot Gold it has removed, subject only to the already-defined whole-number HUD display.
- HUD/economy ownership is unchanged and has no duplicate subtraction.
- The pooled traveller begins every life without a visible label or stale theft value.

### Rollback

Remove the `TheftLabel` child and its `TravellerController` reference/refresh code together. The original drain and economy systems require no restoration.

### Phase 1 Recap - 2026-08-28

#### Implemented

- Added the prefab-owned `TheftLabel` child to `Assets/_Project/Prefabs/PF_Traveller.prefab` at local position `(0, 1.1, 0)`. It is inactive by default, uses world-space TextMeshPro, centered yellow text, font size `3.5`, and the established `g` suffix style.
- Added the serialized `TextMeshPro theftLabel` reference and pooled-lifecycle projection to `Assets/_Project/Runtime/Gameplay/Enemies/TravellerController.cs`.
- The label reads only `PotDrain.StolenGold`, shows cumulative whole gold only during `Draining` or `Fleeing`, updates only when the displayed integer changes, and clears on `Configure`, `Activate`, `OnSpawned`, `OnDespawned`, `OnDisable`, death, edge despawn, and non-stealing/empty transitions.
- Kept `PotDrain`, `PotController`, `PotRegistry`, `EconomyHudPresenter`, `EconomyState`, home-tree logic, shop spending, and death gold-pickup ownership unchanged. No second deduction or currency source was added.
- Added `Assets/_Project/Tests/EditMode/Enemies/TravellerControllerTests.cs` covering sub-whole hiding, cumulative whole-value display, draining/fleeing visibility, despawn reset, re-spawn reset, and `PotDrain.StolenGold` reset.

#### Verification

- Unity MCP prefab-stage inspection confirmed the saved `PF_Traveller` root references the `TheftLabel` TextMeshPro component and the child remains inactive on the asset.
- Focused EditMode run passed **29/29** tests: PotDrain, traveller label lifecycle, traveller targeting, PotRegistry, RunWaveController, and PrefabPool.
- Relevant regression run passed **39/39** tests: the same coverage plus RunDirector, HUD formatting, and shop upgrade runtime tests.
- Unity returned to idle Edit Mode with no compilation errors and an empty Console error/warning query after clearing the tool-generated setup messages.
- A play-mode start/stop smoke check completed without gameplay Console errors. The scene did not produce an active traveller during that short observation, so the live drain visual remains covered by the focused lifecycle test and serialized prefab inspection.

#### Phase 1 result

Phase 1 acceptance criteria are met. The civilian theft number is now a pooled, prefab-owned projection of the existing live Pot drain, with no duplicate economy subtraction. Phase 2 can add the carried-Pot chase and loot-escape presentation without changing this ownership.

## Phase 2 - Carried-Pot chase, loot escape, and visible animation

### Goal

Replace civilian sliding with appropriate Humanoid motion and make a successful thief visibly escape with money.

### Animator and prefab steps

1. Duplicate `Assets/_Project/Art/Animators/Player.controller` to a project-owned `Civilian.controller`. Preserve the existing `Speed` parameter and Idle/Walk baseline; never change the shared player controller.
2. Add only two civilian-specific Animator booleans/states: carried-Pot chase uses the verified looping `Run_1` clip, and loot escape uses the verified looping `Carry` clip. Define deterministic priority: Carry while a positive-theft civilian is Fleeing; Run_1 while Seeking a carried Pot; otherwise preserve normal velocity-driven locomotion/idle. Do not add animation events, blend trees, a generic state system, or root-motion gameplay movement.
3. Add a direct `Animator` reference to `TravellerController` (or resolve the existing child once) and set only the two civilian-specific booleans from its current state/current target. `CharacterAnimationBridge` remains responsible for `Speed` and visual facing.
4. Point `PF_Traveller` to `Civilian.controller`, retain `CharacterAnimationBridge`'s current Rigidbody/visual-root references, and verify `Speed` still drives all normal movement. Use Rigidbody velocity as the source of truth; transform-only motion is prohibited.
5. Add one inactive nested `SM_Prop_Bag_Explorer_01` child under the traveller visual hierarchy at the Phase 0 hand/arm attachment. Enable it exactly when `Fleeing` and `StolenGold > 0`; disable it on every other state and every pool lifecycle reset. Do not create, destroy, or pool the bag separately.
6. Update `SO_Enemy_Traveller_Default.MoveSpeed` from `2.5` to `3.0`. Retain FleeSpeedMultiplier `1.5`, target refresh, engagement range, stop radius, navigation pathing, separation, steal rate, and steal capacity.
7. Confirm the civilian continues to target the Pot transform. When the player carries the Pot, `PotController.IsCarried` selects `Run_1` presentation only; ordinary selection, close-gap movement, direct contact, and drain remain untouched.

### Automated verification

1. Add focused testable coverage for civilian presentation state selection: neutral seeking -> no special state; seeking a carried Pot -> Run_1 state; fleeing after positive theft -> Carry plus bag; pooled reset -> neither special state/bag. Keep this as a small local traveller test/helper, not a generic animation abstraction.
2. Retain/run `TravellerTargetSelectionUtilityTests` to prove target priority is unchanged, particularly harvesting/gold Pot selection and engagement range.
3. Run existing movement/pathing, pool, Pot pickup/carry/drop, Pot drain, and gold pickup tests. Confirm no character-animation or null-reference errors are introduced by a missing optional Animator/bag reference.

### Manual verification

1. Carry a gold-bearing Pot and let civilians acquire it. Verify Run_1 produces visible legs/arms running toward the moving Pot, uses the existing navigation/separation, closes to normal drain distance, and causes no player hit, knockback, or grab.
2. Observe normal stationary-Pot seeking, draining, empty-Pot departure, escape after theft, and a civilian that never steals. Verify Idle/normal locomotion, Carry/bag only for a thief, correct hand placement, natural facing, no root-motion drift, and no sliding.
3. Kill a civilian in each state and recycle it multiple times. Verify labels, bag, Animator booleans, velocity, target, drain, and gold-drop behaviour all reset cleanly.
4. Compare physical pace and animation cadence in an open arena. Keep 3.0 unless only a documented visual mismatch warrants tuning the existing ScriptableObject.

### Acceptance criteria

- Civilians visibly run after carried Pots using Run_1 and visibly escape with Carry plus bag only after a positive theft.
- Gameplay targeting, direct drain, collision, movement physics, and gold pickup recovery retain their established ownership.
- There is no slide, stale bag, stale animation, root-motion drift, or pooling leak.

### Rollback

Restore `PF_Traveller` to the original `Player.controller`, remove the project-owned controller/bag child and traveller animation flags, and set MoveSpeed back to `2.5`. Keep Phase 1's independent theft display if desired.

#### Phase 2 implementation update

- Created project-owned `Assets/_Project/Art/Animators/Civilian.controller` by duplicating `Player.controller`. It preserves `Speed`, `Fire`, Idle, Walk, and Shoot, and adds only `RunToCarriedPot` and `CarryLoot` boolean transitions.
- Configured `Assets/_Project/Art/Animators/Run_1.fbx` and `Assets/_Project/Art/Animators/Carry.fbx` as looping Humanoid clips. The controller uses the embedded `mixamo.com` clip from each source asset.
- Extended `Assets/_Project/Runtime/Gameplay/Enemies/TravellerController.cs` with pooled presentation state: `RunToCarriedPot` is selected only while Seeking a carried Pot; `CarryLoot` and the bag are selected only while Fleeing with positive `PotDrain.StolenGold`. All lifecycle, death, and edge-return paths clear both.
- Updated `Assets/_Project/Prefabs/PF_Traveller.prefab` to use `Civilian.controller`, disable root motion, and add one inactive nested `SM_Prop_Rubbish_Bag_01` under the stable right-hand bone. Its serialized local transform now matches the approved Inspector reference: position `(0.13, 0.187, 0.133)`, rotation `(0, 0, 0)`, and scale `(1, 1, 1)`. Its MeshCollider is disabled and the `TravellerController` owns its active state.
- Updated `Assets/_Project/ScriptableObjects/Defaults/SO_Enemy_Traveller_Default.asset` MoveSpeed from `2.5` to `3.0`; FleeSpeedMultiplier remains `1.5`.
- Extended `Assets/_Project/Tests/EditMode/Enemies/TravellerControllerTests.cs` with carried-Pot presentation, positive-theft bag activation, and despawn reset coverage.
- Corrected the two boolean-driven Any State transitions in `Civilian.controller`: their default self-reentry restarted `Run_1`/`Carry` every frame at frame zero. `RunToCarriedPot` and `CarryLoot` now disallow self-transition; the existing player, police, and military controllers are untouched.
- Added looping `Pick.fbx` as the civilian-only Draining state, driven by the pooled TravellerController state. Replaced the Walk-state motion with `Run_1.fbx` in Civilian, Police, and Military controllers only; Player.controller remains unchanged.

#### Phase 2 verification status

- Focused TravellerController run passed **2/2** tests.
- Relevant movement, targeting, Pot carry/drop, Pot drain, Pot registry/placement/harvest, pooling, and wave regression run passed **41/41** tests.
- Unity MCP re-opened the saved prefab and confirmed `Civilian.controller`, four Animator parameters, `applyRootMotion: false`, TravellerController references, inactive bag, and disabled bag MeshCollider.
- The carried visual uses SM_Prop_Rubbish_Bag_01 with the approved local transform and disabled collider.
- A Play-mode Animator check confirmed the civilian enters Pick with its clip advancing; all three enemy Walk states resolve to Run_1, while Player.controller still resolves to its original walk clip.
- A play-mode Animator verification confirmed `RunToCarriedPot` enters and its normalized time advances from `0.000` to `0.273` after the self-reentry correction. Full visual confirmation of Run_1 cadence, hand placement, and Carry escape remains a manual smoke-test item before closing Phase 2.

## Phase 3 - Civilian wave pressure with existing stagger

### Goal

Increase civilian totals without changing the proven wave scheduler or creating a spawn burst.

### Asset and code steps

1. Through Unity MCP, update only `SO_Wave_Default` traveller counts to Waves 1-5 = `6, 8, 7, 8, 9`.
2. Leave every wave's start delay, spawn interval, maximum concurrency, police total, military total, spawn-point root, traveller pool, and `RunWaveController` code unchanged.
3. Do not add timers, coroutines, civilian-specific queues, pool-size inflation, or a duplicate spawn manager. The existing scheduler serializes every successful spawn by its interval, holds at concurrency cap, and round-robins mixed enemy types.
4. Inspect live pool capacities after the asset edit. If actual playtesting proves the existing configured pool cannot satisfy normal concurrency, increase only the existing traveller pool's prewarm/max capacity to the smallest measured value; do not guess or preallocate for every total wave count.

### Automated verification

1. Add/extend the existing default-wave asset test to assert exact traveller totals `6/8/7/8/9`, alongside the existing police totals. Do not replace the scheduling algorithm tests.
2. Run `RunWaveControllerTests` to verify interval gating, concurrency stall/resume, and Traveller -> Police -> Military rotation remain true.
3. Run traveller spawner/pool tests to verify all active travellers are tracked and their returns reopen concurrency as before.

### Manual verification

1. Play a fresh run through Waves 1-5 with a harvesting/gold Pot present. Confirm totals, edge-spawn cycling, per-wave interval, concurrency gating, and no instant civilian mass spawn.
2. In Waves 3-5, verify civilians interleave with police and military in the established round-robin order and retain movement/animation/label/pool correctness under crowd pressure.
3. Profile/observe the highest-pressure wave. Confirm active traveller count does not exceed the authored cap, no rental failure occurs, and pooled reuse does not retain bag/text/Animator state.

### Acceptance criteria

- Wave totals are exactly `6/8/7/8/9` and entrances remain staggered by the existing scheduler.
- Existing police/military counts, concurrency, intervals, and edge spawn behaviour are unchanged.
- No new pool pressure, Console errors, or visible state leakage occurs under the highest wave.

### Rollback

Restore only the traveller counts in `SO_Wave_Default` to `3/5/4/5/6`; leave `RunWaveController`, other faction totals, and shared scheduling untouched.

### Phase 3 Recap - 2026-08-28

#### Implemented

- Updated only `Assets/_Project/ScriptableObjects/Defaults/SO_Wave_Default.asset` traveller counts to Waves 1-5 = `6 / 8 / 7 / 8 / 9`.
- Extended `Assets/_Project/Tests/EditMode/Orchestration/RunWaveControllerTests.cs` so the existing default-wave asset test asserts both approved police totals and the approved traveller totals.
- Kept every start delay, spawn interval, concurrency cap, police/military total, spawn point, pool, and scheduler implementation unchanged.

#### Verification

- `RunWaveControllerTests` and `PrefabPoolTests` passed **11/11** EditMode tests. This retains interval gating, concurrency stall/resume, round-robin faction order, default authored counts, and pool rent/return coverage.
- Serialized inspection confirmed traveller/police/military totals of `6/0/0`, `8/0/0`, `7/3/2`, `8/4/3`, and `9/5/4`; intervals remain `1.8/1.6/1.5/1.3/1.2` seconds and concurrency remains `3/4/5/6/7`.
- `TravellerPool` remains prewarm `6`, max `64`. The highest authored concurrency is only `7`, so no pool adjustment is justified before playtesting.
- Unity Console contained only the known Test Framework setup/result-save/cleanup messages and no feature error.

#### Phase 3 result

Phase 3 implementation and automated verification are complete. Manual Waves 1-5 crowd-pressure validation remains a Phase 4 smoke-test item.

## Phase 4 - Regression, documentation, and handoff

### Goal

Prove the complete civilian feature in the live run loop and leave it documented for the next implementation task.

### Required automated checks

1. Run all new/extended civilian theft-display, lifecycle/pool-reset, animation-state, default-wave, Pot drain, traveller target-selection, and RunWaveController tests.
2. Run relevant existing EditMode suites: Pots/Pot registry/economy, UI HUD/shop, enemies, pathing/movement, GoldPickup, pooling, run director, and waves.
3. Run the full EditMode suite. Fix every feature regression; report unrelated existing failures separately with their exact test names and baseline status.
4. After each script change, wait for compilation and inspect Unity Console errors/warnings. Before handoff, inspect `PF_Traveller`, `Civilian.controller`, `SO_Enemy_Traveller_Default`, `SO_Wave_Default`, pools, active spawner, and Console once more.

### Required manual smoke test

1. Start a fresh run with known Pot Gold. Confirm one civilian drains a visible cumulative number and the HUD loses the same amount without a home-tree interaction.
2. Pick up and move a gold-bearing Pot. Confirm civilians use Run_1 while pursuing it, visibly close the gap, then drain normally at contact without affecting player health/control.
3. Let a thief escape. Confirm Carry animation, correctly placed bag, persistent cumulative theft label during escape, edge despawn, and clean state on its next pooled rental.
4. Kill civilians before theft, while draining, and while fleeing. Confirm bag/label rules, existing recovered stolen-gold pickup, no duplicate currency, and no stale next-life presentation.
5. Play all five waves. Confirm approved totals, stagger/interleaving, crowd behaviour, pool stability, and no new Console errors.

### Completion criteria

- The theft label, counter, Run_1 chase, Carry/bag escape, 3.0 speed, and `6/8/7/8/9` wave totals exactly match approved decisions.
- Pot Gold has one source of truth and civilian drains cannot be double-counted.
- Focused and regression tests pass, manual smoke passes, and Unity reports no new Console errors/warnings.

### Handoff steps

1. Add per-phase recaps containing exact changed paths, serialized defaults, automated results, manual results, pool observations, and final Console state.
2. Keep `CONTEXT.md` limited to confirmed gameplay vocabulary: Civilian Theft Display, Civilian Carried-Pot Chase, Civilian Loot Escape, Civilian Chase Pace, and Civilian Wave Totals.
3. Refresh Graphify incrementally after implementation. Verify the graph connects `TravellerController`, `PotDrain`, `PotController`, `PotRegistry`, `EconomyHudPresenter`, `CharacterAnimationBridge`, `Civilian.controller`, `PF_Traveller`, `EnemySpawner`, `RunWaveController`, and `SO_Wave_Default`.
4. Inspect the exact diff and preserve unrelated user work. Checkpoint only this feature's code/assets/tests/docs when requested.

### Phase 4 Recap - 2026-08-28

#### Final verification

- The user completed the manual civilian smoke test and confirmed the animation behavior was working, including the Run_1 chase and Carry escape flow.
- Focused civilian and wave tests passed **12/12**.
- Wave scheduler and pooling regression tests passed **11/11**.
- Full EditMode suite passed **119/119**, with no feature regressions or unrelated baseline failures.
- Final Unity inspection confirmed the authored civilian totals `6/8/7/8/9`, existing stagger intervals/caps, TravellerPool prewarm `6`/max `64`, `PF_Traveller` presentation references, `Civilian.controller`, and the approved rubbish-bag transform.
- Unity returned to idle Edit Mode with compilation complete. The Console was cleared after verification and returned **0** error/warning entries; the preceding entries were only the known Test Framework setup/result-save/cleanup messages.
- Graphify incremental code refresh completed: **4,642 nodes**, **8,126 edges**, **329 communities**. The graph diagnostic found **0** dangling, missing-endpoint, or collapsed edges; it reported **4** self-loops for awareness. The current graph contains the handoff document and the core runtime links; Unity-only asset names such as `PF_Traveller` and `Civilian.controller` are not represented as standalone Graphify AST nodes.

#### Phase 4 result

Phase 4 is complete. The Civilian Mobs feature is ready for the next client feature, with the documented implementation, automated coverage, user-confirmed smoke test, and refreshed Graphify reference preserved.

## Explicitly deferred

- Actual tackle attacks, player damage/knockback/grab, civilian weapons, new civilian AI states, revised target priority, detection range, drain rate/capacity, or navigation changes.
- Animated/particle/floating coin bursts, per-civilian bag variations, bag pickups, bag physics, audio, root-motion gameplay, animation events, or a generic enemy animation framework.
- A new economy ledger, home-tree banking, separate stolen-gold counter, event bus, runtime settings UI, new pools, wave scheduler rewrite, altered intervals/caps, or changes to police/military rules.
- Any modification to Synty package source assets or shared Player/police/military Animator controllers.
