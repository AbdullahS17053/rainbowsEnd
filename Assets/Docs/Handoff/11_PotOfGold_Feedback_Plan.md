# Rainbow's End — Pot of Gold Feedback Plan

Status: proposed. This plan turns the approved client feedback into small,
independently verifiable phases. It is intentionally limited to the existing
pot, unicorn, carry/drop, configuration, pooling, and Unity prefab systems.
Do not add a general VFX framework, custom shaders, packages, or gameplay
rules that were not approved.

## Client feedback in scope

1. Double the gold a Pot of Gold harvests each second.
2. Show a rainbow effect rising into the sky while a pot is harvesting from a
   Rainbow Trail.
3. Show a magical gold/yellow glow whenever a pot contains uncollected gold,
   so it remains visible in tall grass or bushes.
4. Show a magical pink/purple burst when the unicorn runs into and destroys a
   pot.
5. Reject pot drops on hard movement-blocking barriers such as logs and walls,
   because they make a pot inaccessible to mobs.

## Verified current state

- The live gameplay target is `Assets/_Project/Scenes/MainArena_3D.unity`.
  The legacy `Phase6PotSetup` pass instead opens `MainArena.unity`; do **not**
  run that pass for this work.
- `SO_Pot_Default.asset` has `BaseHarvestGoldPerSecond = 1`. `PotController`
  sends this value, plus the existing `TrailAgeGoldMultiplier`, to
  `PotHarvestState`. Changing the base to `2` doubles every harvest rate while
  leaving auto-banking and enemy drain untouched.
- `PotController` already owns pot harvesting, stored gold, carried/destroyed
  state, and a handcrafted `HarvestFlare` made from runtime sprite objects.
  It starts when a timed harvest begins and is destroyed when harvesting stops,
  the pot is picked up, disabled, or destroyed.
- `UnicornController` handles both trigger and collision contact and routes
  either one through `PotController.DestroyByUnicorn`. That is the single
  visual-effect hook; pot destruction behavior already belongs there.
- `PlayerPotCarryController.DropCarriedPot()` currently clears its carried-pot
  reference and drops directly at the aimed anchor. It has no placement check.
- `CoverBlocker` already defines a hard world barrier through
  `BlocksMovement`, with a non-trigger `BoxCollider2D`. It is the correct
  existing marker for logs/walls; grass and non-blocking scenery must not be
  rejected.
- Existing reusable assets include Polygon Particle FX's
  `FX_RainbowGlow_02` and Epic Toon FX candidates such as
  `MagicAuraYellow`, `MagicPillarBlast*`, `MagicExplosionPink`, and
  `GlitterExplosionPink`. Final prefab selection must be made by visual
  preview, not filename alone.
- The existing Graphify map confirms the relevant dependency chain:
  `PlayerPotCarryController -> PotController -> PotHarvestState /`
  `PotTrailCaptureSensor`, with `UnicornController -> DestroyByUnicorn` for
  the collision path.

## Finalized decisions and domain language

- **Base Harvest Rate** means the configured gold-per-second value before the
  trail-age multiplier. Set it from `1` to `2`; do not alter auto-bank or drain
  rates.
- **Harvest Flair** means one looping upward rainbow particle effect that is
  active only while `PotHarvestState.IsTimedHarvestActive` is true. It replaces
  the old sprite-built flare; do not show two harvest effects.
- **Pot Glow** means a persistent gold/yellow magical aura while
  `StoredGold > 0` and the pot is not destroyed. It indicates stored,
  uncollected gold, not current earning.
- **Unicorn Impact Burst** means one compact pink/purple magical particle burst
  at the pot when the existing unicorn-destruction event occurs. It is cosmetic
  only: no splash damage, stun, drops, reward, or timing change.
- **Hard Barrier** means a `CoverBlocker` whose `BlocksMovement` value is true.
  **Valid Pot Drop** means a drop position whose pot footprint does not overlap
  a Hard Barrier. Invalid drops keep the pot carried; there is no snapping.
- Reuse existing Polygon Particle FX and Epic Toon FX assets. Do not import
  new assets or packages. Use a gold/yellow aura, a rainbow upward flair, and
  a pink/purple impact burst.

## Guardrails

- Work only on the active `MainArena_3D` pot prefab/instance after verifying
  their prefab connection. Preserve `MainArena.unity` and do not rerun legacy
  setup passes.
- Treat `PotConfig` as authored balance data. Do not write to a ScriptableObject
  at runtime.
- Keep all existing unicorn pot-destruction behavior. The new burst is placed
  before/alongside the existing lethal damage call and must be guarded so one
  collision cannot create duplicates.
- Do not instantiate effects every frame. The glow and flair are authored child
  effects enabled/disabled by state. The rare destruction burst may be one-shot
  instantiated; use native Particle System stop/destroy behavior or an existing
  pool if the chosen prefab already has one.
- Do not add tags, layers, a second collider system, or a new scene manager for
  placement. Query `CoverBlocker` and its existing `BlocksMovement` property.
- Keep the invalid-drop behavior deliberately minimal: retain the pot in hand.
  Do not add a new prompt, sound, UI system, or automatic nearest-valid-position
  search unless a later client request requires it.
- Make a normal version-control checkpoint before changing serialized prefab,
  scene, or balance assets. Check the Unity console after every script edit and
  use the active Unity instance only when it reports ready for tools.

## Phase 0 — Reconnect, snapshot, and choose authored effects

### Goal

Confirm the exact active scene/prefab targets and select three existing assets
by appearance before changing gameplay or serialization.

### Implementation steps

1. Reconnect Unity MCP, confirm the editor is idle, and verify that
   `MainArena_3D.unity` is the active scene. If the editor is unavailable, do
   not modify assets; record that the implementation is waiting for Unity.
2. Locate the active Pot of Gold instance, inspect its prefab source, its
   `PotController`, body collider, visual-root hierarchy, and existing child
   effects. Confirm the body footprint used for placement validation.
3. Locate all `CoverBlocker` objects in `MainArena_3D`; verify that the logs
   and walls intended to be off-limits have `BlocksMovement = true`, while
   grass/bush colliders do not use that marker.
4. Preview existing candidates in a temporary/editor-only context. Select and
   record one asset for each approved role:
   - Pot Glow: gold/yellow looping aura; start with Epic Toon FX
     `MagicAuraYellow`.
   - Harvest Flair: looping particle that visibly travels upward and can read
     as rainbow; start by previewing `FX_RainbowGlow_02` and the available
     `MagicPillarBlast*` variants.
   - Unicorn Impact Burst: compact pink/purple one-shot; start with
     `MagicExplosionPink` or `GlitterExplosionPink`.
5. Confirm selected effects have no gameplay colliders, no unwanted audio, and
   no package-asset edits are required. If a selected one-shot does not clean
   itself up, create the smallest project-local prefab variant and set native
   `ParticleSystemStopAction.Destroy`; do not write a custom lifetime manager.
6. Record the exact asset paths, local offsets, scale, and particle sorting
   order in the Phase 0 recap. Take the version-control checkpoint only after
   the targets are confirmed.

### Acceptance criteria

- `MainArena_3D`, the real pot prefab, and all relevant `CoverBlocker`s are
  identified without running `Phase6PotSetup`.
- One existing asset is chosen for each visual role after preview.
- The selected effects are readable against the 3/4 camera and do not create
  physical interaction.

### Rollback

No code or serialized game asset changes are made in this phase. Discard only
the notes/checkpoint if visual direction changes.

## Phase 1 — Apply and protect the doubled harvest balance

### Goal

Change the sole authored base harvest value from 1 to 2 and leave all other
economy rates exactly as they are.

### Asset and code steps

1. Edit `Assets/_Project/ScriptableObjects/Defaults/SO_Pot_Default.asset` and
   set `BaseHarvestGoldPerSecond` to `2`. Do not change
   `TrailAgeGoldMultiplier`, `AutoBankGoldPerSecond`, `MaxStoredGold`, enemy
   drain configuration, or any economy asset.
2. Do not change `PotHarvestUtility.CalculateBaseHarvestGoldPerSecond` or the
   harvest-state formula; it already multiplies the authored base rate by the
   existing trail-age factor.
3. Add the smallest focused EditMode assertion in the existing pot-harvest test
   area that proves a base rate of `2` yields `2` gold for one second at a
   multiplier of `1`, and that the trail-age multiplier still composes with the
   base rate. If the project convention supports asset loading in EditMode,
   additionally assert the default pot asset is authored as `2`.

### Manual verification

1. Place a pot on a fresh Rainbow Trail and observe one second of harvesting:
   the stored-gold increase is double the previous baseline.
2. Repeat with an aged trail. The increase remains exactly twice the old value
   at the same age because the age multiplier remains active.
3. Confirm automatic banking and a traveller's pot drain operate at their old
   rates.

### Acceptance criteria

- The default asset reads `2` base gold/second.
- Harvest math preserves the existing age multiplier.
- No non-harvest economy rate has changed.

### Rollback

Restore only `BaseHarvestGoldPerSecond` to `1` and revert the focused test.

## Phase 2 — Replace the harvest flare and add state-driven pot feedback

### Goal

Use authored child particle effects to make harvesting and stored gold readable
without runtime sprite construction or per-frame effect allocation.

### Code steps

1. In `Gameplay/Pots/PotController.cs`, remove the old runtime
   `PlayHarvestFlare` implementation that creates `GameObject`, `SpriteRenderer`,
   rainbow color arrays, and DOTween animation. Remove its private transform
   state and any now-unused `DG.Tweening` import.
2. Add only the serialized references needed by the existing controller:
   - a root GameObject for Pot Glow;
   - a root GameObject for Harvest Flair;
   - the selected Unicorn Impact Burst prefab (or a project-local one-shot
     variant).
   Keep these fields on `PotController`; do not introduce a generic feedback
   manager, interface, event bus, or new ScriptableObject type.
3. Centralize visual state in a small private method called from the existing
   state transitions and `ApplyVisualState()` path:
   - enable Pot Glow when `StoredGold > 0` and the pot is not destroyed;
   - enable Harvest Flair only while timed harvesting is active and the pot is
     not carried/destroyed;
   - disable both on pickup, forced harvest stop, disable, and destruction.
   Use `SetActive`/the selected Particle System's normal play/stop behavior;
   never create a looping effect in `Update()`.
4. In `DestroyByUnicorn`, play the one-shot Unicorn Impact Burst exactly once,
   before the existing lethal damage is applied. Reuse the current `IsDestroyed`
   guard and add no damage, reward, or physics code.
5. Keep the gold label, drain feedback, health feedback, registry state, and
   `ApplyWorldInteractionState()` behavior unchanged.

### Automated verification

- Preserve all existing `PotHarvestStateTests`.
- Add one focused test for the pure state predicate if it can be extracted
  without new test infrastructure: no stored gold means no Pot Glow; stored
  gold means Pot Glow; only timed harvest means Harvest Flair. If the current
  controller's Unity-object setup makes that larger than a small test, retain
  the existing state tests and make the visual-state checks manual.
- Compile after the edit; resolve every new console error before prefab work.

### Manual verification

1. Pot with zero stored gold: no glow and no flair.
2. Pot harvesting: rainbow flair loops upward; gold/yellow aura is also visible
   once gold has accrued.
3. Harvest ends with stored gold remaining: flair stops; glow remains.
4. Pick up a harvesting pot: flair stops immediately. Drop it, harvest again,
   and verify no duplicate effects exist.
5. Destroy a pot by unicorn collision: one pink/purple burst plays and the pot
   retains all existing destruction behavior.

### Rollback

Revert the `PotController` feedback fields/methods and remove authored effect
children/references together. The old harvest visual can be restored only as a
single rollback unit; do not leave both systems active.

## Phase 3 — Prevent hard-barrier drops at the shared drop seam

### Goal

Reject player drops whose pot footprint overlaps a movement-blocking
`CoverBlocker`, while preserving normal drops onto valid ground and cosmetic
terrain.

### Code steps

1. Add the smallest placement query needed on the existing pot/carry path. It
   must evaluate the actual pot body-collider footprint at a candidate position
   and inspect overlapped colliders for a parent `CoverBlocker` with
   `BlocksMovement == true`.
2. Prefer a `PotController` helper such as `CanDropAt(Vector2 worldPosition)` so
   the collider dimensions remain owned by the pot. Use one normal
   `Physics2D.OverlapCircleAll`/collider-footprint query at button press time;
   this path is infrequent, so no custom non-alloc buffer is needed.
3. In `PlayerPotCarryController.DropCarriedPot()`, calculate the candidate
   anchor first. If it is invalid, return before assigning `_carriedPot = null`;
   the pot remains carried. If valid, preserve the existing drop sequence.
4. Ignore the pot's own colliders in the query. Reject only
   `CoverBlocker.BlocksMovement`, not arbitrary colliders, trigger volumes,
   Rainbow Trail segments, tall grass, bushes, enemies, or other scenery.
5. Do not auto-snap to an alternative location and do not add a new invalid-drop
   HUD/audio cue in this scope.

### Automated verification

Add a compact EditMode physics test, for example `PotPlacementTests`:

1. Create a pot with its normal `Rigidbody2D`/body collider and a nearby
   `CoverBlocker` with `BlocksMovement = true`; sync 2D transforms; assert the
   candidate drop is rejected.
2. Move the candidate outside the blocker footprint; assert it is accepted.
3. Create a trigger/non-blocking collider without an enabled movement-blocking
   `CoverBlocker`; assert it remains valid.

Keep the fixture self-contained and use existing Unity/NUnit assemblies. Do not
add mocks or a new placement framework.

### Manual verification

1. Pick up a pot, aim at each active log/wall, and press the drop input. The
   pot remains visibly carried and does not end up on the obstacle.
2. Aim at valid ground beside the same barrier. The pot drops normally.
3. Drop in tall grass and near Rainbow Trail segments. Both remain valid when
   no movement-blocking barrier overlaps.
4. Confirm normal pot pickup/drop, enemy target selection, and unicorn collision
   behavior still work.

### Rollback

Revert only the candidate validation/helper and restore the original direct
drop sequence. No scene data needs reverting unless barrier metadata was fixed
in Phase 0.

## Phase 4 — Author the selected effect assets on the active pot prefab

### Goal

Bind the selected existing effects to the real 3D pot prefab and preserve their
appearance through existing and newly spawned pots.

### Unity authoring steps

1. With Unity MCP and a ready editor, open the confirmed pot prefab source.
   Do not modify the imported Epic Toon FX or Polygon package prefab directly.
2. Add or reuse three clearly named children on the pot prefab:
   `PotGlow`, `HarvestFlair`, and only if needed a project-local
   `PotUnicornImpactBurst` prefab reference. Nest the selected reusable effects
   under those roots or make a project-local variant when native stop/destroy
   setup is required.
3. Configure local position, scale, rotation, render sorting, simulation space,
   and play-on-awake so:
   - Pot Glow surrounds/above the pot and is visible through foliage without
     obscuring gameplay;
   - Harvest Flair rises above the pot and is clearly distinct from the
     unicorn's trail;
   - Unicorn Impact Burst is compact and does not linger after destruction.
4. Set Glow and Flair initially inactive, then assign their roots and the burst
   prefab to the serialized `PotController` fields. Apply the same prefab change
   to every active pot variant if Phase 0 identifies more than one.
5. Confirm the `MainArena_3D` pot instance inherits the prefab update. Do not
   create scene-only overrides unless the client intentionally wants a unique
   scene pot.
6. Save the prefab and active scene, validate the scene, and inspect a gameplay
   camera screenshot before moving on.

### Acceptance criteria

- Effects are present on the active 3D pot and newly purchased/spawned pots.
- No package asset was modified.
- Effects have no collision/trigger side effects and no clipped/hidden rendering
  under the gameplay camera.
- The scene validator reports no missing scripts or broken prefabs.

### Rollback

Revert the pot prefab/scene checkpoint and the serialized `PotController`
references. Keep package assets untouched.

## Phase 5 — End-to-end regression, documentation, and graph refresh

### Goal

Verify the complete requested loop together and leave a concise handoff record.

### Required checks

1. Run all focused pot EditMode tests, including existing harvest-state tests
   and the new placement test. Run related unicorn and economy tests if their
   assemblies already provide focused names.
2. Enter Play mode in `MainArena_3D`. Check the Unity Console after compile,
   after a harvest cycle, and after a unicorn destruction. Fix errors introduced
   by this work before completing the phase.
3. Run this manual flow in one session:
   - drop a pot on valid ground;
   - place it on the live Rainbow Trail and observe 2 gold/sec base harvesting
     with upward Harvest Flair;
   - move it into tall grass after it has stored gold and confirm Pot Glow stays
     visible;
   - attempt an invalid log/wall drop and verify the pot remains carried;
   - allow unicorn impact and verify one cosmetic burst plus the unchanged pot
     destruction result.
4. Inspect a gameplay screenshot/video frame for effect readability and confirm
   no effect is confused with a Rainbow Trail, gold pickup, or projectile.
5. Update the progress/handoff record with exact prefab paths selected, files
   changed, test results, console state, visual offsets, and any deferred art
   tuning. Update the existing Graphify corpus incrementally after the final
   source/document changes; do not claim graph refresh if the tool is offline.

### Completion criteria

- Default pot harvest base rate is exactly 2 gold/sec before the existing age
  multiplier.
- Flair is continuous only during harvesting; glow persists while stored gold
  remains; both are absent when appropriate.
- Unicorn impact produces exactly one cosmetic pink/purple burst without a
  gameplay side effect.
- Player cannot drop a pot onto any movement-blocking log/wall, but can drop on
  valid ground and non-blocking foliage.
- Focused tests pass, scene validation passes, and Unity Console contains no
  new errors or warnings.

### Final recap template

- Step completed:
- Selected reusable assets and authored prefab paths:
- Approved behavior implemented:
- Files and serialized assets changed:
- Verification performed:
- Console/test result:
- Deferred work:
- Next step:

## Explicitly deferred

- A generic VFX/event/pooling framework, custom particle shaders, new packages,
  new art, or changes to Epic Toon FX/Polygon package assets.
- Audio, camera shake, screen shake, particles that affect gameplay, and any
  explosion damage/reward/knockback.
- Automatic nearest-valid pot placement, a drop-warning UI, or a sound when a
  blocked drop is rejected.
- Reclassifying non-blocking scenery as a barrier, changing enemy navigation,
  or validating non-player scripted/military pot movement.
- Balance changes beyond `BaseHarvestGoldPerSecond = 2`.
