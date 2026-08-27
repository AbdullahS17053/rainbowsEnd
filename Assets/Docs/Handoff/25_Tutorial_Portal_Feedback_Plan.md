# Tutorial and Portal Feedback Implementation Plan

Status: Planned

## Client request

Add an ordered, in-run tutorial that teaches Pot pickup, Rainbow Trail harvesting, portal placement, and the Upgrade Screen. Each instruction disappears as soon as its approved gameplay condition succeeds. Every card has one global opt-out checkbox. Also make portal entry more forgiving.

## Approved decisions

- Show one **Tutorial Card** at a time, in this exact order, on every new run unless the player has opted out:
  1. `Pick up and put down the Pot of Gold by pressing the 'E' button.`
  2. `Place the pot of gold on the Rainbow Trail to make gold. Protect what's yours.`
  3. `Place a portal by right-clicking, only two portals at a time.`
  4. `Spend/Upgrade by pressing the 'Q' button.`
- The first card appears at run start. Although its copy mentions putting the pot down, it completes immediately after the player **successfully picks up** a Pot of Gold, as requested.
- The sequence is ordered but not persisted step-by-step. When a new run begins, it starts again at card one.
- Every card shows the same global checkbox: `Don't show tutorial prompts again`.
  - Checking it immediately hides the current card and suppresses all present and future tutorial cards across runs.
  - Store only this one boolean with `PlayerPrefs`; save immediately when it changes.
  - The opt-out is intentionally one-way for this version. Do not add a Settings screen, a reset button, or per-step preferences.
- Tutorial Cards are non-blocking: no pause, time-scale change, input disable, modal backdrop, or close button. The player must be able to perform the instructed action while reading the card.
- Use installed Synty Interface/HUD assets for the card, checkbox, frame, and typography. Create project-owned scene objects or a project-local prefab variant only; never edit package assets.
- Place the card at bottom centre so it does not cover the top-left Run Status HUD, gameplay aim area, Upgrade Screen, or outcome UI.
- Completion conditions are authoritative gameplay outcomes, not raw key/button attempts:
  - Pot card: `PlayerPotCarryController.IsCarryingPot` becomes true.
  - Trail card: any live Pot first reaches `PotHarvestStatus.Harvesting` from an active Rainbow Trail.
  - Portal card: `PortalRegistry.ActivePortals.Count` becomes greater than zero after a successful placement.
  - Upgrade card: `ShopController.IsShopOpen` becomes true after the Upgrade Screen actually opens.
- Keep the two-portal rule unchanged. Increase the portal's **effective world-space entry radius** from approximately `0.36` to `0.60` units without scaling the portal visuals.

## Existing implementation facts

- `PlayerPotCarryController` already exposes `IsCarryingPot` and only assigns its carried Pot after `PotController.TryPickUp(...)` succeeds.
- `PotController` already exposes `HarvestStatus`; its harvesting state is only reached after a valid active Trail is captured.
- `PortalRegistry.TryPlacePortal(...)` returns success only after it rents and configures a portal; the registry already caps `MaxActivePortals` at two and replaces the oldest on a third placement.
- `ShopController.OpenShop()` is the one successful-open seam, but its `_isShopOpen` state is private. Add a read-only `IsShopOpen` property; do not duplicate shop input handling.
- `PortalInstance` owns a `CircleCollider2D` trigger. The live `PF_Portal` prefab currently uses local radius `0.45` at uniform root scale `(1,1,1)`, yielding a `0.45` world-space entry radius. The older `0.36` estimate came from a legacy setup assumption and is not the current prefab baseline.

## Target design

### Responsibilities

| Component | Responsibility | Must not do |
| --- | --- | --- |
| `TutorialSequenceState` | Holds the ordered active tutorial step and advances only when the active step is completed. Pure C# for focused EditMode tests. | Read Unity scene state, persist preferences, or draw UI. |
| `TutorialSequenceController` | Resolves the existing player, Pot registry, portal registry, and shop; evaluates only the active step; persists the one global opt-out; drives the view. | Own gameplay state, listen for raw inputs, pause the game, or create a generic event framework. |
| `TutorialPromptView` | Shows/hides the card, renders configured copy, resets the toggle on each show, and reports an opt-out toggle change to its controller. | Decide completion, access PlayerPrefs, or modify gameplay. |
| Existing gameplay owners | Continue to own pickup, harvesting, portal placement, and shop opening. Expose only the tiny missing read-only seam (`ShopController.IsShopOpen`) and a Pot registry harvest query if needed. | Reference tutorial UI or tutorial classes. |

### Data and persistence

1. Define the four `TutorialStepId` values in their required order: `PickUpPot`, `PlaceOnTrail`, `PlacePortal`, `OpenUpgrades`.
2. Keep the message for each step in one serialized ordered definition list on `TutorialSequenceController` so designers can adjust copy/asset wiring in the scene without changing controller logic. Validate that all four IDs appear once and in order; do not silently reorder malformed data.
3. Keep `TutorialSequenceState` in memory only. It starts at `PickUpPot` when a run/scene starts, ignores a non-active completion, advances one step at a time, and reports finished after `OpenUpgrades` completes.
4. Use one namespaced PlayerPrefs key, for example `RainbowsEnd.TutorialPromptsSuppressed`. Read it once at startup and call `PlayerPrefs.Save()` only when the player checks opt-out.
5. Do not create a profile, ScriptableObject save asset, analytics event, generic notification bus, or per-step persistence table.

## Guardrails

- Preserve the existing `E`, right-click, and `Q` input paths. The tutorial observes successful state; it never consumes input or adds alternate bindings.
- Do not let the first prompt require a drop even though its copy mentions it. The client explicitly approved completion on pickup.
- The Trail step must require `PotHarvestStatus.Harvesting`, not `HasGold`, stored gold, a visual glow, or a dropped Pot. Gold can arrive by other paths.
- The portal card must not complete for an input attempt blocked by cooldown or unavailable pooling. It only completes once an active portal exists.
- The Upgrade card must not complete for a Q press unless the shop is truly open.
- Keep `PortalConfig.MaxActivePortals == 2` and all pairing/re-entry-lockout behavior unchanged.
- Treat `PortalConfig.EntryRadiusWorldUnits` as gameplay tuning, separate from visual scale. Convert it to the collider's local radius at configuration time using the live uniform portal scale, so `0.60` remains the effective world radius.
- Do not change portal art, VFX, placement distance, exit offset, cooldown, layer masks, or teleport target filtering as part of the radius change.
- Use only project-local scripts under `Assets/_Project/Runtime` and project-local scene/prefab instances. Do not modify Synty or Epic Toon FX package source assets.
- Before serialized scene/prefab mutation, checkpoint the working tree. After every script edit, wait for Unity compilation and inspect the Console.

## Phase 0 — Snapshot UI, scene, and portal baseline

### Goal

Identify the exact live scene targets and reusable Synty parts before code or serialized scene data changes.

### Steps

1. Reconnect Unity MCP and confirm the editor is idle and `Assets/_Project/Scenes/MainArena_3D.unity` is active. Stop this phase if Unity is unavailable; do not make serialized guesses from YAML.
2. Inspect `UI/HudRoot`, the canvas scaler, existing Upgrade Screen, Run Status HUD, and outcome panel at the target Game view resolution. Record safe bottom-centre bounds for the card.
3. Search installed Synty Interface/HUD assets for an existing framed panel/card, matching checkbox/toggle art, and the current Grenze TMP font. Preview them at gameplay scale. Prefer a card whose ornamentation matches the current Fantasy Warrior HUD; do not import new artwork.
4. Inspect the live `PF_Portal`/portal pool instance, its `CircleCollider2D`, root scale, sorting, VFX children, and `PortalConfig` asset. Record the actual current effective radius and confirm the root scale is uniform before selecting the local-radius conversion.
5. Verify the four runtime seams listed above against current source and scene references. In particular, verify the active Pot registry can enumerate live Pots and the existing Shop Controller is the sole Q-open owner.
6. Record selected asset paths, scene object paths, current portal scale/radius, and version-control checkpoint in the Phase 0 recap before proceeding.

### Acceptance criteria

- The active scene, Canvas, Shop panel, Portal prefab/pool, and exact Synty assets are identified.
- Bottom-centre placement is proven not to cover the HUD or Upgrade Screen.
- No scripts, package assets, scene objects, or prefabs are changed.

### Rollback

No implementation changes exist; retain only the inspection notes.

## Phase 1 — Implement the ordered tutorial state and opt-out

### Goal

Create a small, testable controller/view boundary that expresses the approved sequence without coupling gameplay classes to UI.

### Code steps

1. Add `TutorialStepId` and a focused pure `TutorialSequenceState` under the existing project UI/runtime namespace. Give it the fixed four-step order, current-step access, `TryComplete(activeStep)`, finished state, and reset. A mismatched completion must be ignored; a matching completion advances exactly once.
2. Add `TutorialSequenceController` as a scene-owned MonoBehaviour. It owns serialized references to `TutorialPromptView`, `PlayerRoot`, `PotRegistry`, `PortalRegistry`, `ShopController`, and the ordered serialized step definitions/copy. Resolve missing scene references through the existing ProjectContext/scene owners once, retrying only while a reference is absent.
3. In the controller, read the single PlayerPrefs opt-out key during startup. If set, hide the view and skip all tutorial evaluation. Otherwise reset `TutorialSequenceState` for this run and present the first configured card.
4. Evaluate only the active step in `Update`; this is four small existing-state checks, not a polling framework:
   - pickup: `playerRoot.PotCarryController.IsCarryingPot`;
   - Trail: a dedicated `PotRegistry.HasHarvestingPot` read-only query that checks live, non-destroyed Pots for `HarvestStatus == Harvesting`;
   - portal: `portalRegistry.ActivePortals.Count > 0`;
   - upgrades: `shopController.IsShopOpen`.
   On success, call `TryComplete` for that active step, hide the completed card, then show the next card immediately. After the last step, hide it permanently for that run.
5. Add only the two tiny missing read-only gameplay seams:
   - `PotRegistry.HasHarvestingPot`, calculated from its existing registered live Pots;
   - `ShopController.IsShopOpen => _isShopOpen`.
   Do not add tutorial callbacks, event buses, duplicate input reads, or references from gameplay classes back into UI.
6. Add `TutorialPromptView` with TMP text, card root, global-opt-out Toggle, and a controller-wired callback. When shown, it sets the requested text and clears the Toggle without invoking its callback. When checked, it calls one controller method that writes the preference, saves it, clears in-memory progress, and hides the card immediately.
7. Use the exact approved copy and checkbox wording. Keep all click/raycast behavior limited to the Toggle; the panel itself must not intercept gameplay input.

### Automated verification

Add focused EditMode tests without a mocking framework:

1. `TutorialSequenceState` starts at `PickUpPot`, rejects completion of a later step, advances each matching step once, and finishes after `OpenUpgrades`.
2. Reset returns the state to `PickUpPot`; it does not store per-step completion.
3. The global opt-out key suppresses sequence initialization and is immediately persisted when opted out. Isolate PlayerPrefs with setup/teardown that removes only this namespaced key.
4. `PotRegistry.HasHarvestingPot` returns false for no Pots, null/destroyed Pots, idle Pots, and carried Pots; returns true only for an active harvesting Pot.
5. `ShopController.IsShopOpen` is false after initialization, true only after a valid open, and false after close/disable. Keep this test small; manual testing covers input and full panel behavior.

### Manual verification

1. Start a fresh run with no saved opt-out: only card one appears and gameplay remains live.
2. Press E near a valid Pot: card one disappears and card two appears. A failed pickup leaves card one visible.
3. Tick `Don't show tutorial prompts again` from any card: it disappears immediately and no later card appears during that or a subsequent run.
4. Without opting out, restart a run after any partial progress: card one appears again. No individual tutorial completion persists.

### Acceptance criteria

- The sequence advances only in order and only from approved successful gameplay state.
- The sole persistent datum is the global opt-out.
- No tutorial code changes gameplay inputs, time scale, or existing state ownership.
- Focused tests pass and Unity compiles without new Console errors.

### Rollback

Remove the tutorial scripts, the two read-only properties, and their tests as one feature unit. Existing Pot, portal, and shop behavior remains unchanged.

## Phase 2 — Author and integrate the Synty Tutorial Card

### Goal

Deliver a readable, non-blocking tutorial presentation that belongs to the active MainArena HUD.

### Scene and UI steps

1. In `MainArena_3D`, add one `TutorialPrompt` child beneath the existing gameplay Canvas/HUD root. Keep it inactive in edit time only if the controller reliably enables it at run start; do not duplicate the Canvas, EventSystem, or TMP font asset.
2. Build the card from the Phase 0 selected Synty panel/frame, Synty toggle parts, and existing Grenze TMP font. Keep assets as scene instances or a project-local reusable prefab variant under `Assets/_Project`; never save into the Synty package directory.
3. Anchor the card at bottom centre. Use the phase-zero safe area and canvas scaler conventions. Make the message area wide enough for the longest approved copy at supported resolutions, with wrapping and no truncation.
4. Add the exact text label and the global checkbox label. The checkbox is initially unchecked every time a card is shown; it is not a per-card acknowledgement.
5. Wire `TutorialSequenceController` and `TutorialPromptView` serialized references. Prefer inspector references for Canvas/UI objects; use the existing service registry only for runtime systems already registered there.
6. Confirm the panel root has no image raycast target and no full-screen blocker. The Toggle remains usable and its visual state matches the selected Synty style.
7. Verify card ordering relative to the Run Status HUD, crosshair, Upgrade Screen, and outcome panel. The card must not cover upgrade buttons when Q opens; the final card disappears as the shop opens.

### Manual visual verification

1. Capture Game View screenshots at the intended game resolution for every card. Confirm a single card is visible, text is readable, and Synty styling is coherent with the existing HUD.
2. Move, aim, fire, pick up/drop a Pot, place portals, and open/close Q while a card is visible. Confirm there is no input interception except the checkbox.
3. Confirm the card remains visible through a failed pickup, missed Trail drop, portal cooldown attempt, and invalid Q open; it disappears on the real success on the next rendered frame.
4. Confirm the global checkbox can be clicked and does not block any UI button behind it while the panel is hidden.

### Acceptance criteria

- One polished Synty card presents the active instruction at bottom centre.
- The card is non-blocking, readable, and does not duplicate or obscure existing UI.
- All four exact messages and the exact global opt-out label are present.

### Rollback

Delete only the TutorialPrompt scene objects/project-local prefab variant and controller references. Keep existing HUD, Shop, and outcome UI intact.

## Phase 3 — Make portal entry radius configurable and forgiving

### Goal

Increase portal entry forgiveness to the approved effective world radius while retaining the existing visuals and all two-portal/teleport behavior.

### Code and data steps

1. Add `[Min(0.01f)] EntryRadiusWorldUnits` to `PortalConfig`, defaulting to `0.60f`, with an Inspector tooltip stating that it is the effective world-space trigger radius and does not resize the VFX.
2. In `PortalInstance.Configure`, after it resolves/configures its `CircleCollider2D`, convert `EntryRadiusWorldUnits` into the local collider radius using the live uniform portal transform scale. Preserve `isTrigger = true`.
3. Guard the conversion against zero scale. If the portal root is changed to non-uniform scale later, fail visibly in validation/logging and keep the configured radius behavior explicit rather than silently applying an unpredictable ellipse. Do not change the visual transform scale to compensate.
4. Update the project-local portal prefab's baseline collider to the Phase 0 matching local value (`0.60` at the current uniform scale `1`) so its edit-time gizmo matches runtime behavior. Runtime config remains authoritative.
5. Keep `PortalRegistry.TryPlacePortal`, `MaxActivePortals`, VFX slot assignment, pool sizing, placement distance, exit offset, teleport target filtering, and re-entry lockout untouched.
6. Do not rerun legacy setup utilities such as `Phase10PortalSetup`; they target older setup assumptions and may overwrite newer MainArena work. Apply the focused prefab/config change through Unity MCP.

### Automated verification

1. Add/extend focused Portal tests to confirm the config default is `0.60f`, the runtime collider receives the correct scale-adjusted local radius, and the collider stays a trigger.
2. Retain/execute existing `PortalRegistryTests` to prove a third placement still removes the oldest and active portal count never exceeds two.
3. Add a narrow `PortalInstance` test for one valid teleport through the enlarged trigger and re-entry lockout, only if it can reuse existing fixtures. Do not invent a large physics-test harness; manually verify real player movement if the fixture is not already small.

### Manual verification

1. Place one portal, approach its edge from several directions with the player, and confirm entry feels reliably forgiving before visual overlap is required.
2. Verify player, Unicorn, and Pot still teleport once to the paired exit and do not immediately bounce back.
3. Place a third portal: only two remain, the oldest is removed, and the card text accurately matches behavior.
4. Confirm portal VFX appearance, placement position, cooldown, and exit location look unchanged apart from easier trigger entry.

### Acceptance criteria

- The effective portal entry radius is approximately `0.60` world units at the active prefab scale.
- The portal visual size is unchanged.
- Two-portal, pooling, pairing, and re-entry behavior continue to pass existing tests and manual checks.

### Rollback

Revert the `PortalConfig` field/default, `PortalInstance` radius application, and the focused portal prefab collider value together. Do not revert unrelated portal VFX or registry code.

## Phase 4 — End-to-end regression and client acceptance

### Goal

Prove the tutorial and portal tuning work with the real run loop, UI layering, pause/shop state, Pots, and pooled portals.

### Required automated checks

1. Run all new tutorial state/preference tests and portal-radius tests.
2. Run existing focused Pot, Player carry, Pot harvest, Shop/upgrade, Portal registry, and pooling tests.
3. Run the relevant EditMode suite once focused tests are green. Distinguish pre-existing baseline failures from regressions introduced by this feature.
4. After every script edit and at final scene save, inspect Unity Console errors and warnings. Fix any new failure before scene/prefab work proceeds.

### Required manual playthrough

1. Clear only the tutorial opt-out PlayerPrefs key for the test profile, start a run, and verify the exact sequence: pickup, successful Trail harvest, successful portal, open Q.
2. At every step, attempt the related input unsuccessfully first; verify the card remains. Then complete the real action and verify exactly one next card appears.
3. Tick opt-out from each of the four cards in separate fresh checks. Verify immediate hide and suppression after scene reload/new run. Do not add a player-facing re-enable control.
4. Restart during partial progress without opt-out. Verify the sequence restarts at Pot pickup.
5. Check bottom-centre UI at expected resolutions, while the player is moving/firing, with tall grass on screen, with the shop open, and during run outcome.
6. Exercise the enlarged portal trigger with player, Unicorn, and Pot; verify pairs, third-portal replacement, cooldown, and re-entry lockout.

### Completion criteria

- Every tutorial card uses approved copy, is ordered, non-blocking, and advances only from the approved successful state.
- Global opt-out is the only persistent tutorial datum and behaves one-way as agreed.
- Portal entry is materially more forgiving at `0.60` world units without visual or teleport regressions.
- New focused tests pass, relevant regression tests pass, manual checks pass, and the Console has no new errors/warnings.

## Phase 5 — Handoff, graph refresh, and checkpoint

### Goal

Leave implementation and future changes easy to resume without retaining generated noise.

### Steps

1. Add Phase recaps with exact changed file paths, scene paths, Synty asset paths, PlayerPrefs key, serialized references, final portal scale/local radius/world radius, automated results, manual results, and Console state.
2. Update `CONTEXT.md` only with the agreed gameplay language: Tutorial Sequence, Tutorial Card, Tutorial Opt-Out, Tutorial Run Progress, completion definitions, and Portal Entry Radius. Keep it free of script names and implementation mechanics.
3. Refresh Graphify incrementally only after all code, scene, prefab, and handoff documents are final. Verify it includes the Tutorial controller/view links to player carry, Pot harvesting, Portal registry, Shop, and `PortalConfig`/`PortalInstance` radius relationship.
4. Inspect the exact version-control diff, stage only tutorial/portal/doc files from this feature, run a whitespace check, and create one focused checkpoint. Leave unrelated graph caches, screenshots, and user work untouched.

### Final recap template

- Phase completed:
- Approved decisions:
- Files and serialized assets changed:
- Synty assets reused:
- Persistent key and behavior:
- Portal radius before/after:
- Automated verification:
- Manual verification:
- Console state:
- Deferred work:
- Next step:

## Explicitly deferred

- A tutorial settings screen, reset/re-enable option, per-step completion persistence, account/profile save system, localization, analytics, voice-over, or tutorial replay menu.
- A generic event bus, quest system, objective framework, notification/toast framework, or a cross-feature input abstraction.
- New UI packages, custom art, Synty package edits, custom shaders, animation changes, or sound effects.
- Changes to portal visual size, VFX, placement distance, cooldown, exit offset, target eligibility, pair rules, or re-entry lockout beyond the entry-radius tuning.
