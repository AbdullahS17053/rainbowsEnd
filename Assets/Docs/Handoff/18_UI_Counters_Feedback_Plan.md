# Rainbow's End — Run Status HUD Feedback Plan

Status: Phase 0 complete; Phase 1 complete; Phase 2 complete; Phase 3 pending. This plan turns the approved UI feedback into independently
verifiable phases. It reuses the active 3D scene, the existing pot, shop,
enemy, run-director, TextMeshPro, and Synty Fantasy Warrior HUD systems. Do
not add packages, a generic event bus, a second currency wallet, or a new UI
framework.

## Client feedback in scope

1. The on-screen gold counter must equal the gold currently held in Pots of
   Gold. Gold is not deposited at the Fae Tree.
2. Add an always-visible ammo counter.
3. Add an elapsed run timer.
4. Add a combined kill counter.
5. Add a successful civilian/thief escape counter.
6. Use the installed Synty Interface/Fantasy Warrior HUD assets for the UI.

## Verified current state

- The active gameplay scene is `Assets/_Project/Scenes/MainArena_3D.unity`.
  Do not modify legacy `MainArena.unity` or run legacy setup passes against it.
- `PotController` stores harvested gold in `_storedGold` and no longer auto-banks
  it. `PotRegistry` is the authoritative aggregate for live pot gold.
  `EconomyHudPresenter` and the shop display/spend that aggregate, removing
  the separate wallet that caused the reported 80-on-pot/20-on-HUD mismatch.- `PotRegistry` already owns registration order, active-pot filtering, and
  aggregate stored-gold queries. It is the smallest suitable source for a
  shared Pot Gold balance.
- `GoldPickup`, spawned by military deaths, deposits into the oldest live pot
  through `PotRegistry` and remains in the world when all live pots are full.
- `PlayerWeaponInventory` owns temporary-weapon shots. Its default weapon has
  no finite ammo; `RemainingTemporaryShots` is zero when the default weapon is
  active.
- `RunDirector` owns run initialization and outcome handling. The existing
  Q upgrade panel pauses via `Time.timeScale = 0`; outcomes also freeze time.
  It now also owns per-run elapsed seconds, combined hostile kills, and successful theft escapes; these reset on run initialization and freeze after outcome reporting.

- Each enemy controller already has a one-time death path guarded by
  `_deathHandled`. A traveller has a distinct `DespawnFleeing` path after
  reaching the arena edge. Its `PotDrain.StolenGold` is available before pool
  reset.
- Reusable Synty assets include Fantasy Warrior HUD containers, game-time
  prefabs/status icons, bullet UI parts, a skull symbol, and a gold-ingot
  resource icon. Use package assets as references only; do not edit imported
  package prefabs or textures.

## Finalized decisions and domain language

- **Pot Gold** is the single spendable balance of the run. It is the combined
  gold physically held by all live Pots of Gold. The HUD and shop use this
  value; there is no Fae Tree deposit or hidden bank.
- **Displayed Pot Gold** is `floor(sum of stored fractional gold)`. Fractions
  remain stored so harvest progress is never discarded.
- **Pot Gold Spending Order** is oldest live pot first, then newer live pots
  until an upgrade cost is covered.
- **Gold Pickup Deposit** means a collected military gold pickup is stored in
  the oldest live pot. If no live pot can accept it, the pickup remains in the
  world; it must not disappear into an invisible balance.
- **Ammo Counter** is always visible. It shows `∞` for the default weapon and
  the remaining-shot count for a temporary weapon pickup.
- **Run Timer** is elapsed scaled run time in `MM:SS`. It starts at run start,
  pauses with the Q upgrade screen and outcome freeze, and does not count real
  wall-clock time.
- **Kill Counter** counts all defeated hostile units: travellers, police, and
  military. It does not need to distinguish the source of their defeat.
- **Successful Theft Escape** means a traveller reaches the arena edge after
  stealing a positive amount of Pot Gold. A retreat with zero stolen gold is not
  counted.
- **Run Status HUD** is one compact, always-visible Synty-styled group showing
  Pot Gold, ammo, time, kills, and escapes.
- **Run Statistics** are per-run. Pot Gold, time, kills, and escapes reset when
  a new run starts and stay frozen on the outcome until the next run.

## Guardrails

- Keep `EconomyState` only for the existing harvest-multiplier service during
  this task. It must stop being a gold wallet for the HUD, shop, and pickups.
  Do not create a synchronizer that mirrors Pot Gold back into it.
- Remove the auto-bank path rather than setting up another deposit destination.
  Delete dead `TransferAllGold` behavior if the caller search remains empty.
- Keep pot gold authoritative in `PotController`/`PotRegistry`; do not place
  a mutable gold total in the HUD.
- Keep kill/escape/time state on the existing `RunDirector`, which already
  owns run lifecycle. Do not introduce a singleton, generic score manager,
  event bus, ScriptableObject state asset, or persistence system.
- Use a thin UI presenter that reads state and formats labels. Five values are
  small enough to refresh by comparison each frame; no subscription framework
  is needed.
- Preserve pooling: record a successful traveller escape before returning it to
  the pool, and reset any per-instance report guard on spawn/activation.
- Never edit Synty package assets. Author only scene-local UI instances or
  project-local prefab variants under `Assets/_Project` when a reusable
  variant is genuinely required.
- Before serialized scene/prefab work, take a normal version-control
  checkpoint. After every script edit, wait for compile completion and inspect
  the Unity Console.

## Phase 0 — Snapshot the active HUD and choose existing Synty parts

### Goal

Confirm the live scene/UI target and record the exact Synty assets to reuse.
This phase changes no gameplay code or serialized gameplay data.

### Implementation steps

1. Reconnect Unity MCP, confirm the editor is idle, and verify
   `MainArena_3D.unity` is active. If Unity is unavailable, stop before scene
   mutation and record the implementation as waiting for the editor.
2. Inspect `UI/HudRoot`, the existing `EconomyHudPresenter`, the shop
   panel, canvas scaler, and current camera resolution. Record the existing
   gold-label object and its prefab/scene ownership.
3. Inspect `RunDirector`, `PotRegistry`, `PlayerWeaponInventory`, and
   the three enemy-controller prefabs/instances. Confirm their active scene
   references and prefab sources before adding fields.
4. Preview only existing Synty candidates at gameplay scale. Start with a
   Fantasy Warrior HUD container for each row, the gold-ingot icon, game-time
   prefab/status-time icon, bullet UI part, and skull symbol. For escapes, use
   the same container with the explicit TMP label `Escapes` unless a clearly
   readable installed icon is found; do not import art or mislabel an unrelated
   icon.
5. Define one compact group layout that extends the existing gold location:
   Pot Gold, Ammo, Time, Kills, Escapes. Confirm it does not cover player
   health, the crosshair, pause/shop controls, or the outcome panel.
6. Take a version-control checkpoint only after targets and asset paths are
   recorded.

### Acceptance criteria

- The active `MainArena_3D` HUD and all authoritative runtime owners are
  identified.
- Exact Synty asset paths and intended row order are recorded.
- No package assets, scene objects, or runtime code are changed.

### Rollback

No runtime or serialized changes exist. Discard only Phase 0 notes/checkpoint.

## Phase 1 — Make Pot Gold the only spendable gold path

Status: Complete. Automated coverage passed; the combined manual playthrough remains in Phase 4.

### Goal

Use live pot storage as the one gold source for HUD display, shop affordability,
upgrade spending, and gold pickups. Eliminate automatic banking.

### Code and data steps

1. In `Core/Config/PotConfig.cs` and
   `ScriptableObjects/Defaults/SO_Pot_Default.asset`, remove/retire the
   auto-bank setting rather than merely hiding it. In `PotController.Update`,
   remove the branch that subtracts stored gold and calls
   `EconomyState.AddGold`. Continue using `EconomyState` only for the
   existing active-harvester multiplier.
2. In `PotController`, add the smallest explicit storage helper required by
   shared callers, e.g. `TryStoreGold(float amount)`, returning the amount
   accepted after the existing max-storage limit. Route harvesting through the
   same capacity rule. Keep `DrainGold` as the only removal primitive and
   remove `TransferAllGold` if no live caller remains.
3. In `PotRegistry`, expose the authoritative combined fractional total and
   derive `TotalStoredGold` with one floor of the combined total, not a sum of
   separately floored pots. Add:
   - an oldest-live-pot deposit helper that attempts each eligible pot in
     registration order until all pickup gold is stored or capacity is reached;
   - an atomic `TrySpendGold(int cost)` that first verifies sufficient
     combined stored value, then drains oldest live pots first.
   Destroyed/null pots are skipped in both paths.
4. Change `EconomyHudPresenter` to resolve `PotRegistry` and display
   `Pot Gold: {TotalStoredGold}`. Remove `EconomyState.GoldChanged`
   subscription logic; refresh only when the displayed integer changes.
   Preserve the existing TMP object and keep the presenter thin.
5. Change `ShopController` and `ShopPanelPresenter` to resolve/pass
   `PotRegistry`, use `TotalStoredGold` for affordability and visible shop
   gold, and call `TrySpendGold` only after the existing upgrade validity
   checks. Preserve the current failure path: no upgrade level or pot balance
   changes on insufficient gold or a maxed entry.
6. Update every compile-time caller of `ShopController.Configure`, including
   the legacy `Phase11ShopSetup` editor utility, to pass/resolve
   `PotRegistry`. Do not run that legacy setup pass and do not let it overwrite
   `MainArena_3D`.
7. Change `GoldPickup` to resolve `PotRegistry` and deposit into live pots.
   Return the pooled pickup only after its full amount is accepted. If capacity
   or live pots are unavailable, leave the pickup collectible and do not credit
   `EconomyState`.
8. Search all `EconomyState.AddGold`, `RemoveGold`, and
   `TransferAllGold` callers before closing the phase. Any remaining gold
   wallet use must be deliberate and documented; no HUD/shop/pickup path may
   use it.

### Automated verification

Extend `Tests/EditMode/Pots/PotRegistryTests.cs` using existing Unity/NUnit
fixtures:

1. Store fractional gold in two pots and assert displayed Pot Gold is the floor
   of their combined amount.
2. Assert a successful cost drains the oldest pot first and then the next pot,
   with the displayed total reduced by the cost.
3. Assert insufficient Pot Gold leaves every pot unchanged.
4. Assert destroyed pots are ignored for aggregate, deposit, and spending.
5. Assert a deposit goes to the oldest live pot and carries to the next only if
   needed by capacity.

Add a focused `GoldPickup` test only if the existing pickup/pool fixture makes
it small; otherwise cover its scene behavior manually in Phase 4. Do not add a
mocking framework.

### Manual verification

1. Let a pot harvest until its world label reads a visible amount. The Pot Gold
   HUD reaches the same whole-number value and does not lag behind a hidden
   bank.
2. Leave a pot harvesting near the Fae Tree and far from it. The value must not
   transfer based on tree location or time.
3. Open Q shop and confirm its balance and affordance match the HUD. Buy an
   upgrade using multiple pots; observe the oldest pot drain first.
4. Kill a military enemy, collect its gold pickup, and confirm Pot Gold and the
   oldest pot increase together. No separate gold counter changes.
5. Confirm pot harvest multipliers, traveller drain, and gold labels still work.

### Acceptance criteria

- Pot Gold is the only displayed and spendable balance.
- No automatic transfer to `EconomyState` occurs.
- Shop and pickup behavior use the same physical pot balance.
- No credit is lost when a pickup cannot be stored.
- Focused registry tests pass and the console has no new compile errors.

### Rollback

Revert the PotController/PotRegistry/shop/HUD/pickup changes as one currency
unit. Do not leave a shop reading one balance while the HUD shows another.

## Phase 2 — Add minimal per-run statistic state at existing event seams

Status: Complete. Runtime counters and event wiring are implemented; focused Phase 2 tests pass. Manual pause/outcome and integrated gameplay checks remain in Phase 4.

### Goal

Track elapsed scaled time, combined enemy kills, and successful theft escapes
without altering spawning, combat, pooling, or outcome rules.

### Code steps

1. Add `ElapsedRunSeconds`, `KillCount`, and
   `SuccessfulTheftEscapeCount` to the existing `RunDirector`. Reset all
   three inside its existing `InitializeRun` path. Increase elapsed time using
   `Time.deltaTime` only while no outcome has been reported; this naturally
   pauses under the current Q/outcome `Time.timeScale = 0` behavior.
2. Add small public record methods on `RunDirector`, such as
   `RecordKill()` and `RecordSuccessfulTheftEscape()`. Register/resolve the
   existing director through the project context only if that is the smallest
   shared lookup seam; do not add a new global manager.
3. In each guarded one-time death handler—
   `TravellerController.HandleDeath`, `PoliceController.HandleDeath`, and
   `MilitaryController.HandleDeath`—record exactly one kill after the handler
   has confirmed it is the first death. Do not use generic pool-despawn
   notifications; they also cover non-death cleanup.
4. In `TravellerController.DespawnFleeing`, before the pool return resets
   `PotDrain`, record an escape only when `PotDrain.StolenGold > 0`.
   Add/reset one local report guard so a pooled traveller cannot report the same
   escape twice.
5. Do not count military pot destruction, police/military cleanup, a traveller
   retreat with zero stolen gold, player death, or a pot destruction as an
   escape/kill statistic.

### Automated verification

Add small EditMode coverage near the existing orchestration/enemy tests:

1. Verify a new run starts with zero elapsed/kills/escapes and resets them when
   `RunDirector.InitializeRun` is reached through its existing configuration
   path.
2. Verify each public record method increments once and never produces a
   negative value.
3. Add a traveller-focused test for the predicate: zero stolen gold does not
   report an escape; positive stolen gold reports one exactly once. If creating
   a pooled controller fixture grows beyond a focused test, isolate only the
   pure predicate/helper and keep the actual pooled-edge behavior in manual
   verification.
4. Retain existing wave, enemy, pot-drain, and pooling tests unchanged.

### Manual verification

1. Start a run: Time starts at `00:00`, kills and escapes start at zero.
2. Open Q for several seconds: time does not advance. Close Q: it resumes.
3. Defeat one traveller, police, and military: kills increase by three total.
4. Let a traveller steal gold and reach the grass/map edge: escapes increase by
   one. Interrupt another traveller before it steals: escapes do not increase.
5. Win or lose: values stop changing and remain visible until the next run.

### Acceptance criteria

- Stats reset exactly with the run lifecycle.
- Timer respects existing pause/outcome behavior.
- Every hostile death reports one kill, and only successful theft exits report
  an escape.
- Existing wave completion and pooled despawn behavior are unchanged.

### Rollback

Revert only the new RunDirector fields/methods and the three enemy-controller
record calls together. No wave configuration or pooling asset needs reverting.

## Phase 3 — Author the compact Synty Run Status HUD

### Goal

Present all approved values as one readable, always-visible HUD group without
a visual redesign or package edits.

### UI and scene steps

1. In `MainArena_3D`, create or extend one `RunStatusHud` child under the
   active HUD canvas at the Phase 0-confirmed gold location. Keep the existing
   gold TMP label in this group; do not create a second gold counter.
2. Reuse selected Synty Fantasy Warrior container instances for each row.
   Use the selected gold, bullet, time, and skull artwork. The escape row may
   use a labeled Synty container when no semantically accurate installed icon
   exists. Keep all imported assets immutable.
3. Add four TMP labels for ammo, time, kills, and escapes. Keep one existing
   gold TMP label, now driven by `EconomyHudPresenter`. Use the approved
   visible text:
   - `Pot Gold: {wholeGold}`
   - `Ammo: ∞` or `Ammo: {remainingShots}`
   - `Time: MM:SS`
   - `Kills: {count}`
   - `Escapes: {count}`
4. Add one small project script, e.g. `RunStatusHudPresenter`, containing only
   serialized TMP references plus resolved `PlayerWeaponInventory` and
   `RunDirector` references. It formats ammo/time/kills/escapes and writes a
   label only when its formatted value changes. It must not own mutable
   gameplay state.
5. Resolve scene references through the existing `PlayerRoot` and
   `RunDirector`/project context, with inspector references preferred. Avoid
   `FindFirstObjectByType` every frame; resolve once, retry only if a reference
   is missing.
6. Keep the HUD visible in normal play and behind/inert beneath the existing
   modal shop/outcome panels as appropriate. It must not intercept pointer
   clicks, block shop buttons, or alter canvas scaling.

### Automated verification

- Add compact formatting tests for `MM:SS` boundary values (`0`, `59`,
  `60`, and a multi-minute value) if formatting can be a pure helper.
- Add a small ammo-format test for default `∞` and temporary remaining shots
  if it can reuse `PlayerWeaponInventoryTests`.
- Do not test Synty prefab art through code. Use the manual visual checklist.

### Manual verification

1. At the intended gameplay resolution, verify all five rows are visible,
   readable, aligned, and neither clipped nor overlapping the health bar,
   crosshair, shop, or outcome UI.
2. Verify the default weapon shows `∞`; collect a temporary weapon and fire
   it until the display decreases and returns to `∞`.
3. Verify Pot Gold, shop gold, and pot labels agree through harvesting,
   spending, draining, and pickup collection.
4. Verify `00:59 -> 01:00` formatting and timer pause/resume.
5. Verify kill/escape updates are immediately readable and do not produce
   duplicate rows or duplicated TMP labels after a run reset.

### Acceptance criteria

- One compact Synty-styled group presents all five approved values.
- The interface contains no duplicated gold counter or non-Synty replacement
  art.
- The UI is legible and non-interactive during play.

### Rollback

Restore the HUD canvas/scene from the Phase 0 checkpoint and remove only the
new presenter/serialized references. Currency and stat code remain independently
revertible through their earlier phase checkpoints.

## Phase 4 — Cross-system regression and edge-case verification

### Goal

Prove the complete loop uses one currency and accurate per-run counters without
regressing the pot, shop, combat, enemy, or pause loops.

### Required automated checks

1. Run the focused PotRegistry, pot-harvest, pot-drain, economy multiplier,
   shop/player-upgrade, player-weapon-inventory, run-wave, and enemy tests.
2. Run all newly added currency, statistic, and formatter tests.
3. Check compile output after every script edit. Resolve new errors before
   scene work; distinguish the known Unity test-framework result-save message
   from a failed test run.

### Required manual playthrough

1. Start a fresh run and record the initial five counter values.
2. Harvest into one pot, then multiple pots. Confirm HUD/shop Pot Gold is the
   floored combined value; spend an upgrade and confirm oldest-first drainage.
3. Collect a military gold pickup. Confirm it reaches Pot Gold, not a hidden
   bank.
4. Exercise default and temporary weapons; validate ammo display through the
   final temporary shot.
5. Kill one of every hostile type; then permit one thief with stolen gold to
   escape and one zero-stolen traveller to retreat. Validate exact counts.
6. Open/close Q and finish a win/loss. Verify time pauses correctly and all
   run statistics remain frozen on outcome.
7. Inspect the gameplay camera for Synty HUD readability in tall grass and
   during the upgrade/outcome UI. Confirm Unity Console has no new errors or
   warnings caused by this work.

### Completion criteria

- The reported 80-versus-20 discrepancy cannot occur: pot-held gold, HUD gold,
  and shop affordability share the same Pot Gold source.
- Gold never auto-banks at the Fae Tree or into the legacy wallet.
- Ammo shows `∞` for default and remaining shots for a temporary weapon.
- Timer, kills, and successful escapes follow the approved definitions.
- Focused tests pass, manual checks pass, and the active scene is saved without
  missing references.

## Phase 5 — Handoff, graph refresh, and checkpoint

### Goal

Leave a reliable reference for the next implementation task.

### Steps

1. Update the appropriate handoff/progress recap with exact files, scene paths,
   selected Synty asset paths, prefab ownership, counter positions, test
   results, console state, and any visual tuning deferred by the client.
2. Update the Graphify corpus incrementally only after all final source and
   document changes are complete. Verify the graph includes the Pot Gold,
   RunDirector, enemy, shop, and HUD relationship changes before claiming it is
   current.
3. Inspect the exact version-control diff, stage only files from this feature,
   run a whitespace check, and commit one focused change set. Leave unrelated
   generated screenshots, metadata, and graph-cache files untouched unless the
   user explicitly includes them.

### Final recap template

- Step completed:
- Approved decisions:
- Files and serialized assets changed:
- Synty assets reused:
- Automated verification:
- Manual verification:
- Console state:
- Deferred work:
- Next step:

## Explicitly deferred

- A persistent/lifetime profile, leaderboard, analytics, post-run scorecard, or
  save system.
- A generic scoring/currency/event framework or a second currency wallet.
- Currency animations, pickup fly-to-HUD effects, sounds, counter tweening, and
  toast notifications.
- New UI packages, imported art, custom shaders, or changes to Synty package
  assets.
- Per-enemy kill breakdowns, escape categories beyond successful theft escapes,
  a wave countdown, and a player-selectable pot to spend from.
- Rebalancing shop costs, harvest rates, drain rates, pickup values, or weapon
  drops beyond routing their already-authored values through Pot Gold.
