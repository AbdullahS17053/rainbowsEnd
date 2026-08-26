# Rainbow's End Upgrade Screen Feedback Plan

Status: proposed. This document turns the client feedback into small,
independently verifiable implementation phases. It is deliberately scoped to
the existing shop, input, and weapon systems; do not add a new pause framework,
currency type, or upgrade UI framework for this work.

## Client feedback in scope

1. `Q` opens and closes the upgrade screen anywhere in the arena, like a pause
   overlay. The Fae Tree remains in the game but is no longer the interaction
   point for the screen.
2. Reduce every applicable upgrade price by 50%.
3. Add an upgrade that increases the firing cone's length.

## Verified current state

- `IA_Gameplay.inputactions` already binds `ShopInteract` to `Q` and gamepad
  north. Keep those bindings; this is a behavior change, not an input-system
  rewrite.
- `ShopController` currently opens only when `CanInteractWithShop()` succeeds.
  That predicate requires the Fae Tree, the configured tree radius, a player
  who is neither disabled nor carrying a pot, and all shop dependencies.
- When open, `ShopController` already pauses with `Time.timeScale = 0` and
  disables player movement, aim, pot carry, weapon, and portal controllers.
  `Q`/`ShopInteract` and Escape/`Pause` already close the panel. Reuse this
  mechanism.
- `FaeTreeAnchor` is still used as the spawn origin for the existing pot
  purchase effects. Do not remove the anchor, its scene object, or its visual.
- `SO_ShopConfig.asset` currently has five definitions: Damage (20), Fire Rate
  (25), Accuracy (15), second Pot (50), and third Pot (75). The live
  `MainArena_3D` UI has five matching button views; the legacy setup pass still
  creates only three.
- `Phase11ShopSetup` is an old bootstrap pass: it opens `MainArena.unity`,
  resets the shop asset to three entries, and creates three buttons. The active
  presentation scene is `MainArena_3D.unity`. Do not run this setup pass as-is.
- `WeaponController` uses `WeaponConfig.Range` for auto-fire targeting and the
  visible cone. `ArrowProjectile` independently uses the same config value for
  projectile lifetime. A range upgrade must override both paths or the player
  could target enemies that arrows cannot reach.

## Finalized decisions

1. **Cone meaning:** “length” means **range/reach**. The target-acquisition
   cone, its visible mesh, and arrow maximum distance increase together; its
   angle does not change.
2. **Price scope and rounding:** reduce all five current definitions by 50% and
   round down to an integer: `20 -> 10`, `25 -> 12`, `15 -> 7`, `50 -> 25`, and
   `75 -> 37`.
3. **Catalog scope:** show all five existing definitions and the new range
   upgrade, for a six-entry catalog. This intentionally makes the already
   configured second- and third-pot purchases reachable.
4. **Initial range-upgrade balance:** append `Cone Reach Up`, cost `12`, maximum
   purchases `3`, value `2` world units per purchase. Its description is:
   `Increase firing-cone and arrow range by 2 units per purchase.` These are
   normal `ShopConfig` values and may be rebalanced later without code changes.
## Conventions and guardrails

- Work on `Assets/_Project/Scenes/MainArena_3D.unity`, not the legacy
  `MainArena.unity`, unless the user explicitly asks to migrate both.
- Keep `ShopConfig` as the authored source of balance values. Runtime purchases
  modify only `PlayerUpgradeRuntime`; they must never edit ScriptableObject
  assets during play.
- Append new enum values. Never reorder existing `ShopUpgradeEffectType` values,
  because the values are serialized in `SO_ShopConfig.asset`.
- Preserve the existing Q/gamepad-north and Escape/gamepad-start bindings. Do
  not add a second action or duplicate a binding.
- No new dependencies, ScriptableObject types, scene roots, or pause manager.
- Stop after each phase and record its result in the recap format in
  `03_Progress.md`; do not bundle unverified phases together.

## Phase 0 — Lock scope and take a safe snapshot

### Goal

Record the finalized scope and establish the exact scene/config state to edit.
This phase changes no runtime code.

### Implementation steps

1. Record the finalized cone interpretation, `Cone Reach Up` balance, price
   table, and six-entry catalog in this plan's execution recap.
2. In Unity, open `MainArena_3D.unity` and locate the existing
   `ShopPanelPresenter`, `ShopController`, `FaeTreeAnchor`, and shop button
   GameObjects. Confirm they are the objects used in play mode.
3. Inspect `SO_ShopConfig.asset` and record its existing upgrade order before
   editing it. Do not run `Rainbows End/Setup/Run Phase 11 Shop Setup`.
4. Make a normal version-control checkpoint before serialized scene/asset work.

### Acceptance criteria

- The finalized choices are written down.
- The implementation target is confirmed as `MainArena_3D`.
- The current tree, shop panel, and config assets are identified.

### Rollback

No runtime changes exist. Revert only the checkpoint/document note if the
client changes the requested behavior.

### Phase 0 execution recap — 2026-08-26

- Step completed: inspected the live Unity scene, shop controller, shop panel,
  Fae Tree, and balance asset through Unity MCP.
- Outcome: `MainArena_3D` is active, loaded, build index 1, and not dirty.
  `GameplayRoot/ShopSystems/FaeTreeShop` has the expected `ShopController`;
  `UI/HudRoot/ShopPanel` has five `ShopUpgradeButtonView` entries; and the
  active 3D tree mesh is `EnvironmentRoot/Props/FaeTreeAnchor/SM_Env_Tree_Large_01`.
- Code/config snapshot: the existing `ShopConfig` asset is confirmed as a
  `RainbowsEnd.Core.Config.ShopConfig` with the five definitions documented
  above. `ShopPanel` is currently a 750x750 panel with five 96-pixel rows, so
  Phase 3 must add one row and adjust the panel/layout as needed.
- Checkpoint: no runtime, scene, or serialized asset changes were made in
  Phase 0. The worktree was inspected before implementation; only the new
  handoff plan and Graphify working files are untracked. Take the normal
  version-control checkpoint immediately before Phase 1 serialized/code work.
- Verification: Unity editor state reported `ready_for_tools: true` with no
  blocking reasons; the active scene was not dirty. `manage_asset get_components`
  was not applicable because `ShopConfig` is a ScriptableObject, so its type and
  serialized values were verified from the asset and `manage_asset get_info`.
- Current phase: Phase 0 complete; Phase 1 ready.
- Next step: create the version-control checkpoint, then implement the global
  Q toggle and remove tree/radius gating.
- Blockers or decisions: none; client decisions are finalized above.
## Phase 1 — Make the upgrade panel globally toggleable

### Goal

Pressing Q (or the existing gamepad-north binding) opens the upgrade panel from
any arena location; pressing it again closes it. The panel continues to pause
the current run through the already-implemented lock/restore path.

### Code changes

1. In `Gameplay/Shop/ShopController.cs`, split the current access check into a
   dependency check for opening the screen. The new check must require only the
   dependencies needed to display and purchase upgrades: `shopConfig`,
   `panelPresenter`, `playerRoot`, `inputReader`, `upgradeRuntime`,
   `economyState`, and at least one configured upgrade.
2. Remove Fae Tree presence, player distance, interaction radius, carrying-pot,
   and disabled-state checks from the Q-open gate. Keep ordinary null guards.
3. In `Update()`, when closed, call `OpenShop()` directly on
   `ShopInteractPressedThisFrame`; do not calculate tree proximity. When open,
   keep `ShopInteractPressedThisFrame` as the toggle-close input and retain
   `PausePressedThisFrame` as a close/cancel input.
4. Remove now-dead radius-only members/methods from `ShopController` (the
   fallback radius and `IsPlayerWithinInteractionRadius`). Keep the
   `FaeTreeAnchor` reference because pot-purchase effects still spawn around
   it.
5. Never show `ShopPrompt` from this controller after the change. Update the
   instructions text in `UI/ShopPanelPresenter.cs` to state the real controls,
   for example: `Press 1-9 or click an upgrade. Press Q or Escape to close.`
   Do not change `PlayerInputReader` or the input-actions asset.

### Manual verification

1. From Boot, stand far from the Fae Tree and press Q: the panel opens.
2. While open, verify player movement, aiming, firing, portal casting, enemy
   movement, and wave timers are paused; the cursor/buttons remain usable.
3. Press Q: the panel closes and exactly the controllers that were enabled
   before opening are enabled again.
4. Open while carrying a pot and while far from the tree; close with Q and
   Escape. Confirm the carried pot is still owned by the player after closing.
5. Confirm no tree interaction prompt appears and the Fae Tree still renders.

### Automated verification

- Existing compilation and shop EditMode tests must pass.
- Add a focused `ShopController` test only if the project already has a simple
  fixture for its dependencies. Do not create a large mock framework just to
  test this one gate; the manual pause/toggle check above is the acceptance test.

### Rollback

Revert the `ShopController` and `ShopPanelPresenter` changes. The tree-gated
interaction behavior returns without affecting purchased-upgrade data.

## Phase 2 — Add the cone-length runtime upgrade

### Goal

Implement one data-driven upgrade effect that extends the active weapon's
effective targeting range, visible cone length, and projectile travel distance
by the same resolved amount.

### Code changes

1. In `Core/Config/ShopConfig.cs`, append `WeaponRange` after `PotPurchase3`.
   Do not change the integer values of the five existing members.
2. In `Gameplay/Player/PlayerUpgradeRuntime.cs`, add one accumulated range
   bonus field; clear it in `ResetPurchasedUpgrades()`; apply
   `valuePerPurchase` for the new enum case; and expose
   `ResolveWeaponRange(float baseRange)`. This resolver is additive and clamps
   at zero or greater.
3. In `Gameplay/Combat/WeaponController.cs`, make `ResolveTargetingRange()`
   call the new runtime resolver when available. This automatically updates
   target search, line-of-sight maximum distance, editor gizmo, and the runtime
   cone mesh because they already use `ResolveTargetingRange()`.
4. Thread a named optional range override from `WeaponController.FireShot()`
   through `ProjectileWeaponLauncher.LaunchVolley()` / `LaunchResolved()` to
   `ArrowProjectile.Launch()`. Store the resolved maximum distance on the
   projectile and use it instead of `WeaponConfig.Range` for its remaining
   distance and despawn test. Keep the current config range as the default when
   no override is supplied so enemy launchers and existing callers are unchanged.
5. Do not mutate `WeaponConfig.Range`; the modifier must work for the active
   default weapon and temporary dropped weapons without changing source assets.

### Automated verification

Extend `Tests/EditMode/Shop/PlayerUpgradeRuntimeTests.cs` with one focused test:

- Apply `Cone Reach Up` once to a base range of `8`; assert the resolved value
  is `10`, and assert reset restores `8`.

Keep the existing damage, cooldown, spread, and max-purchase assertions. Extend
only existing tests; do not introduce a new testing framework.

### Manual verification

1. With no range upgrade, place a hostile just beyond the base cone/arrow
   range. It must not be selected or hit.
2. Buy one range upgrade and place the same hostile inside the new range. The
   target must be selected, lie within the visible cone, and be hit before the
   arrow despawns.
3. Repeat with a temporary weapon drop whose base range differs from the bow.
   The bonus must apply once to that weapon's base range, not carry over as a
   mutated config value.
4. Verify cover still blocks targets/visual cone exactly as before.

### Rollback

Revert the enum, runtime resolver, and range-override plumbing together. Do not
revert the asset definition alone while the enum remains referenced.

## Phase 3 — Author balance data and make the visible catalog match it

### Goal

Update the active shop asset with approved prices and the new range definition,
then expose exactly the approved number of entries in the `MainArena_3D` shop
panel.

### Asset and UI steps

1. Edit `Settings/Balance/SO_ShopConfig.asset` and set the five current costs
   to `10`, `12`, `7`, `25`, and `37` in their existing order.
2. Append the sixth definition exactly as follows: display name `Cone Reach Up`;
   description `Increase firing-cone and arrow range by 2 units per purchase.`;
   effect type `WeaponRange`; cost `12`; maximum purchase count `3`; value per
   purchase `2`. The description must continue to state both targeting and
   arrow reach.
3. In `MainArena_3D`, set `ShopPanelPresenter.upgradeButtonViews` to six.
   Reuse `ShopUpgradeButtonView`; it already supports arbitrary array lengths
   and indices.
4. Add the one missing button GameObject, with the same `Button`, `Image`,
   `TMP_Text`, and `ShopUpgradeButtonView` composition as the existing entries.
   Resize/reposition the panel and buttons so all six 96-pixel rows are readable
   at the gameplay camera resolution. Do not add scrolling, a new presenter, or
   a prefab system; increase the panel height if the existing layout does not
   fit.
5. Confirm each index maps to the matching definition order: button 1 maps to
   upgrade index 0, button 2 to index 1, and so on. Keyboard shortcuts 1-9 are
   already index-based and need no code change.
6. Do not run `Phase11ShopSetup` as part of this phase. If a later task needs a
   rerunnable shop setup, update that script separately to target `MainArena_3D`
   and preserve the authored catalog instead of resetting it to three defaults.
### Verification checklist

- All six displayed costs match the finalized balance table and the button affordance
  changes correctly with gold.
- Every approved upgrade is visible, clickable, and reachable by its matching
  number key.
- Maxed entries show their existing maxed state and cannot spend gold.
- Insufficient-gold purchase attempts do not spend gold or change level.
- The panel has no clipped text, overlapping buttons, or hidden active entries.

### Rollback

Restore the serialized shop asset and `MainArena_3D` panel from the Phase 0
checkpoint. Code from Phases 1-2 remains compatible with the original entries.

## Phase 4 — End-to-end regression and handoff

### Goal

Verify the feedback works together without regressing the game loop or existing
upgrade behavior, then leave an exact recap for the next model.

### Required checks

1. Run the focused EditMode shop tests and combat tests, including
   `PlayerUpgradeRuntimeTests` and `WeaponAutoFireTargetingUtilityTests`.
2. Check the Unity Console after compilation and after a Boot playthrough. Fix
   errors introduced by this work before declaring the phase complete.
3. Run one short combat loop: open Q screen away from the Fae Tree, buy an
   existing upgrade and the new range upgrade, resume, fire at a target near
   the new range limit, then complete/lose the encounter normally.
4. Verify Escape closes the upgrade overlay without creating a second global
   pause state. Do not claim a full pause-menu feature was implemented.
5. Update `03_Progress.md` with the implementation date, files changed, test
   results, unresolved decisions, and the next recommended task.

### Completion criteria

- Q toggles the panel anywhere and resumes correctly.
- The Fae Tree remains visible and no longer gates the panel.
- Approved prices and catalog are displayed and charged correctly.
- The cone-length upgrade affects targeting, visualization, and arrow
  reach consistently.
- Focused tests pass and the Unity Console has no new errors or warnings.

### Final recap template

- Step completed:
- Approved decisions:
- Files and serialized assets changed:
- Outcome:
- Verification performed:
- Console/test result:
- Deferred work:
- Next step:

## Explicitly deferred

- A general pause menu, settings screen, scene-wide pause ownership system, or
  a new input action. The existing shop overlay already meets the requested
  pause-like behavior.
- A cone-width upgrade, unless the client clarifies that “length” meant angle.
- A generic scrollable/dynamic shop framework. Reuse the existing presenter and
  add only the slots approved for the current catalog.
- Changes to Fae Tree visuals, banking design, or unrelated scene dressing.
