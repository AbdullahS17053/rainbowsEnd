# Unicorn Feedback Implementation Plan

Status: Phase 4 complete

## Client request

- On Unicorn collision, knock back and briefly stun the Player unless the Player is carrying the Pot of Gold. A carried Pot remains vulnerable to Unicorn contact.
- Make the Rainbow Trail visually float.
- Give a clear warning when the Unicorn's current route will hit a Pot of Gold.
- Preserve the active Rainbow Trail when the Unicorn uses a Portal; trail emission resumes at the exit instead of appearing to clear and restart.
- Improve Unicorn roaming so it does not loop around the map's upper third.

## Approved decisions

- **Pot-carry immunity:** a Player carrying a Pot receives no Unicorn knockback or daze. The Pot is still processed by the existing Unicorn-Pot collision path and can be destroyed.
- Non-carrying Unicorn hits use the existing PlayerRoot daze path with defaults of **1 world-unit knockback** and **0.5 seconds** of daze. The values remain Inspector-tunable on the existing UnicornConfig/SO_Unicorn_Default.
- The short Unicorn daze reuses the Player's existing spinning-star cue only. It does not replay the longer health-depletion stun or stand-up animations.
- **Pot-impact warning:** use a thin red LineRenderer from the Unicorn to its current target. It appears only while the direct route intersects a vulnerable Pot, whether that Pot is grounded or carried. It hides immediately when no Pot is threatened.
- **Trail float:** apply a subtle continuous vertical bob to the existing FX_RainbowGlow_02 child of each RainbowTrailSegment. The pooled segment root and its CircleCollider2D stay fixed on the XY gameplay plane.
- **Portal trail continuity:** Unicorn portal travel never clears current trail segments or resets TrailAgeSeconds. The existing trail remains capturable and the emitter attempts an ordinary segment at the portal exit. There is intentionally no artificial rainbow bridge between portal endpoints.
- **Cross-map return:** the Unicorn may still use the whole arena. After it reaches a top-edge target, its next target must be on the bottom edge. This prevents repeated upper-area loops without turning the top third into a dead zone.
- Reuse the existing UnicornController, RainbowTrailEmitter, RainbowTrailSegment, PotRegistry/PotController, PlayerRoot, PortalInstance, PrefabPool, UnicornConfig, and the military LineRenderer setup pattern. Do not add an event bus, a new manager, a new pool, a generic trajectory framework, a new settings asset, or package dependency.

## Existing implementation facts

- MainArena_3D currently uses a 2D XY gameplay plane with ArenaBounds WorldRect X [-13.05, 13.05] and Y [-17.45, 17.45].
- UnicornController moves directly toward a boundary target, clamps the final point to ArenaBounds, and exposes CurrentTarget and CurrentVelocity. It currently permits every edge as a follow-up target, including repeated top/side paths.
- UnicornController currently calls TryDestroyPot before TryKnockbackPlayer for trigger and collision callbacks. TryKnockbackPlayer always calls PlayerRoot.ApplyKnockbackAndDaze for any Player collider.
- PlayerRoot already exposes IsCarryingPot and owns ApplyKnockbackAndDaze. The existing daze disables ordinary player control but currently has no separate short-daze visual cue. The Player prefab already has the reusable stunnedStars effect.
- SO_Unicorn_Default currently authorizes PlayerKnockbackDistance = 1 and PlayerDazeDurationSeconds = 1.5. Phase 1 changes only the approved daze default to 0.5 after baseline confirmation.
- RainbowTrailEmitter owns trail age, pooled segment emission, and ClearAllTrail. The only runtime gameplay caller of ClearAllTrail is the existing successful Pot harvest capture. PortalInstance has no Unicorn-specific trail notification today.
- RainbowTrailSegment is a pooled root with a fixed CircleCollider2D and a disabled SpriteRenderer. Its visible child is the existing FX_RainbowGlow_02 prefab instance.
- PortalInstance teleports a Unicorn by setting its Rigidbody2D position. It does not clear trail state, force an exit segment, or reset Unicorn target state.
- PF_Military already uses a configured world-space LineRenderer for the red aim telegraph. Its widths, color, sorting order, visibility discipline, and serialized-reference pattern are the project-local reference for the Unicorn warning.
- The existing bounded trail-cell registry must remain intact: active segments persist until Pot capture and are returned only by the normal capture cleanup. Floating presentation and portal travel must not alter cell registration, timestamps, harvesting, pooling, or capture triggers.

## Ownership and non-goals

| Owner | Responsibility | Must not own |
| --- | --- | --- |
| UnicornController | Collision exception, threat evaluation, warning visibility, current target selection, top-to-bottom routing rule. | Player life-state replacement, Pot destruction rewrite, trail pool ownership. |
| UnicornConfig / SO_Unicorn_Default | Existing collision tuning plus minimal Unicorn-owned warning visual tuning when a live Inspector adjustment is useful. | Scene references, per-Pot runtime state, new global settings. |
| PlayerRoot | Existing knockback/daze action and short-daze star visibility. | Unicorn movement, Pot collision selection, portal trail lifecycle. |
| RainbowTrailEmitter | Existing age/segment lifecycle plus a narrow Unicorn-teleported request that preserves the active trail and attempts exit emission. | Portal pairing, Pot harvest math, generic teleport event dispatch. |
| RainbowTrailSegment / PF_RainbowTrailSegment | FX-child visual bob and clean pooled reset. | Collider movement, harvest/capture rules, segment root motion. |
| PortalInstance | After its existing Unicorn teleport, directly notify that Unicorn's emitter. | Trail cleanup, forced target routing, a reusable event system. |
| PotRegistry / PotController | Existing live Pot ownership and vulnerability truth. | A duplicate warning state or new damage path. |
| UnicornRoamingUtility | Deterministic top-edge follow-up rule and its focused tests. | NavMesh/pathfinding framework, obstacle avoidance system. |

Do not change Player recovery-veil behavior, Pot harvesting/economy, Portal pairing/re-entry/cooldowns, trail expiry rules, active trail-cell coalescing, ArenaBounds itself, enemy AI, shared materials, layers/tags, or Synty/Epic Toon FX source assets.

## Phase 0 - Baseline, exact seams, and repro (complete)

### Goal

Record the live starting state and prove the affected collision, trail, portal, visual, and routing seams before implementation.

### Steps

1. Confirm through Unity MCP that MainArena_3D is active, the editor is idle, compilation is complete, and the Console is captured before changes. Record the current relevant working-tree changes and leave unrelated work untouched.
2. Inspect PF_Unicorn, PF_RainbowTrailSegment, PF_Player, SO_Unicorn_Default, MainArena_3D ArenaBounds, RainbowTrailPool, and the active Portal prefab. Record serialized references, collider states, segment FX-child transform, Player stunnedStars reference, pool prewarm/max, and actual Unicorn warning-material behavior from PF_Military.
3. Trace every caller of UnicornController.TryKnockbackPlayer/PlayerRoot.ApplyKnockbackAndDaze, RainbowTrailEmitter.ClearAllTrail/Configure, RainbowTrailSegment.ReturnAllToPool, and PortalInstance.TeleportTarget. Confirm the only normal trail clear remains successful Pot capture.
4. Trace PotRegistry/PotController carry and collision data. Establish the exact geometry source that can represent both a grounded Pot and a carried Pot for warning prediction; do not infer threat from a disabled or stale collider.
5. In Play Mode, reproduce and record:
   - a direct Unicorn hit on a non-carrying Player;
   - a direct Unicorn hit while the Player carries the Pot;
   - Unicorn contact with that carried Pot;
   - a threatened grounded Pot and a threatened carried Pot;
   - Unicorn portal traversal before and after a Pot captures the trail;
   - repeated top-edge reaches and the next selected boundary target.
6. Run focused baseline EditMode coverage for Unicorn roaming/trail segment lifecycle, Pot registry/capture, PlayerRoot/pot carry, portal registry, and pooling. Record any failure as baseline; do not weaken existing tests.

### Acceptance criteria

- Collision precedence, Pot geometry, warning rendering reference, portal seam, current trail lifecycle, and target-selection rule are verified from the live project.
- The visual bob axis is confirmed as presentation-only depth/height, never a movement of the XY collider root.
- No gameplay, prefab, scene, config, or source change is made.

### Rollback

No implementation changes exist; retain the baseline recap only.

## Phase 0 completion record

Completed 2026-08-28 as a read-only baseline pass.

- Unity 6000.0.67f1; active scene 'Assets/_Project/Scenes/MainArena_3D.unity'; editor returned idle after the observation.
- 'ArenaBounds' is centered at (0, 0) with size (26.10, 34.90), giving world rect x=-13.05..13.05, y=-17.45..17.45.
- 'PF_Unicorn' has a Dynamic/Continuous Rigidbody2D, a trigger CircleCollider2D with radius 0.40, 'UnicornController', and 'RainbowTrailEmitter'. The emitter resolves 'RainbowTrailPool' at runtime through 'EffectsRoot'.
- 'PF_RainbowTrailSegment' has a trigger CircleCollider2D with radius 0.16; its pooled root remains the gameplay object and its visual children are 'FX_RainbowGlow_02', 'FX_RainbowGlow_Sparkles_01', and 'FX_RainbowGlow_Ring_01'.
- 'PF_Player' exposes 'PlayerRoot.IsCarryingPot'; the 'StunFeedback' object is inactive by default while the existing 'Stars' feedback object is present and active in the prefab.
- 'PF_Portal' contains a trigger CircleCollider2D and 'PortalInstance'. Runtime portal traversal currently resolves Unicorns and teleports them with no trail notification. The live scene has one 'PortalRegistry'; its pool contains three portal instances in the baseline snapshot.
- Live baseline snapshot after approximately five seconds of play: one Unicorn, one active pot, three portal instances from the portal pool, and 48 RainbowTrailSegment components present in the scene. The Unicorn was moving toward a boundary target; the pot was grounded, not carried, not destroyed, and empty.
- Collision order is Pot first, then Player. 'UnicornController.TryKnockbackPlayer' currently always calls 'PlayerRoot.ApplyKnockbackAndDaze'; it has no 'IsCarryingPot' exception.
- Trail clearing is currently owned by successful pot trail capture through 'PotController'; the emitter has no portal-continuity hook.
- 'SelectNextBoundarySide' currently allows 'Top' to choose 'Bottom', 'Left', or 'Right'; the planned cross-map rule has not been implemented.
- 'SO_Unicorn_Default' currently has speed 2.5, retarget interval 5, arrival distance 0.2, corner padding 1, trail interval 0.1, trail cell size 0.32, knockback 1, and daze 1.5 seconds. Phase 1 will change only the daze default to 0.5 seconds.
- Focused baseline EditMode run: 9 passed, 0 failed, 0 skipped across Unicorn roaming, Rainbow Trail Segment, PlayerRoot, and PortalRegistry tests.
- Console was clear after the run; no project compilation or runtime errors remained. Unity Test Framework setup/result-save messages were cleared as editor noise.

No gameplay, prefab, scene, or configuration changes were made in Phase 0.

## Phase 1 - Collision rule, short-daze cue, and Pot-impact warning (complete)

### Goal

Make Unicorn danger readable while preserving the existing Player/Pot damage owners.

### Steps

1. Keep UnicornController's current Pot-first collision ordering. Add one narrow early return in TryKnockbackPlayer when PlayerRoot.IsCarryingPot is true. Do not disable Unicorn, Player, or Pot colliders as a workaround.
2. Set SO_Unicorn_Default.PlayerKnockbackDistance to 1 and PlayerDazeDurationSeconds to 0.5. Retain the existing Inspector fields and their validation; do not add a second stun config.
3. Extend PlayerRoot's existing daze lifecycle to enable the already-assigned stunnedStars object for Unicorn daze, and disable it on normal daze recovery, disable/reset, and all existing health-depletion/recovery transitions. Do not trigger the long Stunned or StandUp Animator states for this half-second effect.
4. Reuse the PF_Military LineRenderer pattern on PF_Unicorn: one serialized, disabled-by-default, world-space line with two points, red color, correct sorting order, and no separate pooled warning prefab. Reuse the existing rendered material/setup where valid; do not edit the military prefab.
5. In UnicornController, evaluate its current direct segment from live Unicorn position to CurrentTarget against the authoritative live Pot set. Show the line only through the threatened Pot; hide it for no Pot, destroyed Pot, after Pot destruction, after retarget, and when the target/path is invalid.
6. Include both grounded and carried Pots in threat evaluation. Use the same Pot transform/geometry that can still be hit by the Unicorn, not a new flag and not a Player-only approximation. If more than one Pot lies on the route, terminate the line at the first threatened Pot.
7. Keep warning tuning local to UnicornConfig only if actual Inspector tuning is needed after the live preview: color, start/end width, and a small hit-padding are acceptable. Do not add duration, damage, or new targeting settings because the warning is continuously projected from the existing movement target.

### Automated verification

1. Add/extend PlayerRoot coverage proving Unicorn daze activates the stars, clears them on recovery/reset, and does not alter the established health-depletion animation sequence.
2. Add focused Unicorn collision tests for non-carrying knockback/daze, carrying-player immunity, and Pot-first collision processing. The carrying test must prove the Pot path remains eligible while Player knockback is skipped.
3. Add deterministic route-intersection tests for no Pot, grounded Pot, carried Pot, first-of-two Pots, off-route Pot, destroyed Pot, and a target change. Keep geometry as a small Unicorn-local/helper method only if it enables the test; do not introduce a reusable prediction module.
4. Retain PlayerRoot recovery, Pot carry/drop, Pot registry, Pot destruction, projectile immunity, and existing military telegraph tests.

### Manual verification

1. Let the Unicorn hit the Player without a Pot. Verify a 1-unit push, half-second control lock, spinning stars, then exact control restoration with no long downed animation.
2. Repeat while carrying the Pot. Verify the Player phases through with no push, daze, stars, health/depletion change, or loss of carrying state; verify the Pot still receives ordinary Unicorn contact behavior.
3. Place/hold Pots across the Unicorn's route. Verify the red line is thin, readable, ends at the first vulnerable Pot, continuously follows current target changes, includes a carried Pot, and disappears immediately when danger ends.
4. Verify no red warning, stale stars, or null-reference error remains after Pot destruction, drop/pickup, player recovery, scene restart, or repeated collisions.

### Acceptance criteria

- Carrying a Pot protects only the Player from Unicorn knockback/daze.
- The warning predicts only a real first Pot threat and never creates another damage/collision authority.
- Daze feedback is visible, bounded to the approved half-second, and cannot leak into Player downed/recovery presentation.

### Rollback

Remove the Player carry guard, short-daze star toggle, Unicorn line reference/evaluation, and optional UnicornConfig warning fields together. Restore the previous serialized daze value only if the feature is fully rolled back.

## Phase 1 completion record

Completed 2026-08-28.

- Unicorn collisions preserve pot-first ordering. A carried pot is resolved from the colliding player through 'PlayerRoot.PotCarryController.CarriedPot', so the pot remains vulnerable while the player is protected.
- Non-carrying player contact still uses the existing shared 'PlayerRoot.ApplyKnockbackAndDaze' path. Carrying players exit before knockback or daze is applied.
- Temporary Unicorn daze now shows the existing PlayerRoot 'stunnedStars' visual and clears it when the daze recovers. Downed/recovery animation ownership remains unchanged.
- The Unicorn config default and 'SO_Unicorn_Default' asset now use a 0.5 second player daze. Knockback remains 1 world unit.
- 'PF_Unicorn' now contains an assigned, disabled-by-default LineRenderer copied from the known-good military telegraph setup: world-space, two positions, red, width 0.08 to 0.02, sorting order 30.
- The warning line projects along the Unicorn current boundary target and terminates at the first vulnerable pot intersected by the route. It uses the pot root CircleCollider geometry even while that collider is disabled for carrying; destroyed/inactive pots are ignored.
- Focused EditMode verification passed 12/12, including the new carried-pot collision guard, disabled-carried-pot warning geometry, and temporary daze star feedback tests.
- A six-second Play Mode smoke pass completed with one runtime Unicorn and no project compilation/runtime errors. The warning line remained hidden because the live route did not threaten the grounded pot.
- Console was cleared after verification.

No portal, trail-floating, or path-routing behavior was changed in Phase 1.

## Phase 2 - Floating trail presentation and portal continuity (complete)

### Goal

Improve Trail readability without moving gameplay coverage or changing the approved capture-only cleanup rule.

### Steps

1. In RainbowTrailSegment, cache the authored local position of the existing FX_RainbowGlow_02 child and apply a small sine-wave bob on the visual height/depth axis only. The pooled root Transform, CircleCollider2D, static active-segment list, active-cell dictionary, timestamp, and capture sensor remain untouched.
2. Reset the FX child to its authored local position in every pooled lifecycle boundary: Awake/setup, OnSpawned, OnDespawned, Activate, ReturnAllToPool, disable, and re-rent. Do not modify the Epic Toon FX source prefab.
3. Keep bob amplitude/frequency as minimal serialized presentation fields on RainbowTrailSegment only when actual Inspector tuning is needed; default to a subtle readable movement. Do not add a global visual config or animate the root to preserve XY physics.
4. Add one narrow RainbowTrailEmitter method for Unicorn portal traversal. It must preserve _trailStartTime, active trail cells, existing segments, and pool ownership while requesting an immediate normal emission attempt at the exit.
5. In PortalInstance, after its existing Unicorn teleport succeeds, directly invoke that Unicorn-specific emitter method. Do not notify Player/Pot, clear trail state, alter portal pair/re-entry logic, or create a generic teleport event.
6. Ensure the exit request respects the existing cell-occupancy check. If the exit is already covered, it creates no duplicate; otherwise it rents/configures a normal segment. Subsequent emission retains existing movement/cooldown rules.
7. Confirm Pot capture remains the only ordinary caller that clears the active Rainbow Trail. Portal traversal must not reset TrailAgeSeconds, harvest duration calculations, segment timestamps, or the static cell lookup.

### Automated verification

1. Add focused RainbowTrailSegment presentation lifecycle coverage proving visual local position is restored after spawn/despawn/re-rent while the root/collider gameplay position does not change.
2. Add emitter portal-continuity coverage: active cells and TrailAgeSeconds survive a Unicorn portal notification; the exit requests at most one normal segment; an occupied exit cell does not duplicate; a later Pot capture still clears all cells.
3. Retain/run trail cell, PotTrailCaptureSensor, PotHarvestState, PotRegistry, PrefabPool, PortalRegistry, and Unicorn roaming tests.

### Manual verification

1. Watch a long uncaptured Trail. Verify a subtle living/floating effect that does not drift, change capture placement, expose collider gaps, or accumulate offset through pooling.
2. Put a Pot on an older and newer floating segment. Verify normal capture, gold rate/duration, clear-on-capture, and subsequent new Trail behavior.
3. Send the Unicorn through both portal directions before and after trail capture. Verify pre-entry Trail stays visible/capturable, emission continues from the exit, no false age reset occurs, no endpoint bridge is drawn, and no stale visual reappears on pooled segments.

### Acceptance criteria

- Trail motion is presentation-only and remains pool-safe.
- Portal traversal retains Trail continuity and age without changing portal or Pot gameplay.
- Revisited cells, capture cleanup, and existing pooled behavior remain correct.

### Rollback

Remove only the segment visual-bob state and Unicorn portal-notification path. Preserve the existing Trail pool, cell registry, and capture lifecycle.

## Phase 2 completion record

Completed 2026-08-28.

- RainbowTrailSegment now finds the existing FX_RainbowGlow_02 child, caches its authored local position, and applies a subtle local-Z sine bob with serialized presentation defaults of amplitude 0.04 and frequency 2.5. The pooled root, XY collider, active-cell registry, and timestamp are not moved or rewritten.
- Pool lifecycle reset restores the authored visual-child position through the existing OnSpawned/OnDespawned reset path. No Epic Toon FX source prefab was edited.
- RainbowTrailEmitter.NotifyUnicornTeleported clears only the spawn cooldown, preserves _trailStartTime and active segments, and attempts one ordinary exit segment through the existing pool/cell-occupancy path.
- PortalInstance directly notifies only a teleported Unicorn after the existing Rigidbody2D/Transform teleport. Player/Pot portal behavior, pairing, and re-entry lockout remain unchanged.
- Focused EditMode verification passed 8/8: trail cell behavior, pooled visual reset, emitter age/cell continuity, PortalRegistry behavior, and Unicorn regression tests.
- Play Mode smoke produced no errors. Active pooled trail telemetry observed the visual child moving from local Z 0 to approximately 0.002 between samples. A temporary production PortalInstance trigger check moved the Unicorn from the first portal to the paired exit with no errors.
- Console was cleared after verification.

No cross-map routing behavior was changed in Phase 2.

## Phase 3 - Cross-map return routing (complete)

### Goal

Prevent repeated upper-area loops while retaining whole-map Unicorn movement.

### Steps

1. Change the existing UnicornRoamingUtility follow-up selection so a previous Top boundary target always selects Bottom next. Keep the existing initial random side selection and all other non-top transition behavior unchanged.
2. Preserve current direct bounded movement, retarget interval, target-arrival distance, corner padding, and ArenaBounds clamp. Do not introduce NavMesh, obstacle avoidance, a top-third exclusion volume, a new roam-zone asset, or per-frame path replanning.
3. Confirm the direct route now produces an intentional cross-map traversal after a top-edge arrival. Portal exits remain valid anywhere and do not create an extra exclusion rule.
4. Update only the existing Unicorn roaming tests with the explicit Top -> Bottom expectation. Keep tests for valid side selection, edge intersection, boundary targets, MoveTowards, and clamp behavior.

### Automated verification

1. Add a deterministic test that Top always transitions to Bottom across random-value boundaries.
2. Assert that initial target selection and Right/Bottom/Left follow-up distributions retain their existing allowed sides.
3. Run all Unicorn movement/trail, Player knockback, Portal, Pot, and pooling tests together to confirm target policy did not affect collision/trail timing.

### Manual verification

1. Observe multiple full Unicorn cycles, including a top-edge arrival. Verify it leaves toward the bottom rather than chain-bouncing through upper side targets.
2. Confirm it still visibly enters the top third on its cross-map route and can use portals there; the region is not blocked or made dead.
3. Confirm no boundary jitter, clamp stall, target-at-current-position loop, or trail gap occurs at the top/bottom transition.

### Acceptance criteria

- A Top arrival always begins a Bottom-directed return.
- The whole map remains traversable by Unicorn movement and portals.
- No new navigation/pathfinding system, dead zone, or route regression is introduced.

### Rollback

Restore only the prior Top follow-up random selection and its corresponding test expectation.

## Phase 3 completion record

Completed 2026-08-28.

- UnicornRoamingUtility.SelectNextBoundarySide now returns Bottom for every random value when the previous boundary side is Top. This is a follow-up route rule, not a map restriction: the Unicorn can still traverse the full arena on the cross-map return.
- Initial side selection remains unchanged. Right, Bottom, and Left follow-up choices retain their existing random behavior. UnicornController movement, ArenaBounds clamping, target arrival, portals, trail continuity, and Phase 1/2 collision/visual behavior were not changed.
- Added deterministic coverage for Top follow-up values 0, 0.49, 0.5, 0.99, and 1.
- Focused EditMode verification passed 12/12 across Unicorn roaming, trail, Unicorn collision/warning, and PortalRegistry tests.
- Ten-second Play Mode smoke completed with no project runtime or compilation errors. Console was cleared after verification.

No Phase 4 full-suite or final handoff work was performed yet.

## Phase 4 - Regression, documentation, and handoff (complete)

### Goal

Prove the full Unicorn feature in MainArena_3D and leave a clean implementation record.

### Required automated checks

1. Run focused tests for Unicorn roaming, trajectory threat selection, collision/carry exception, PlayerRoot daze feedback, trail presentation lifecycle, portal continuity, Pot capture, and pooling.
2. Run relevant EditMode suites for Player, Pots, Unicorn, Spells, Core/Health, combat/projectile immunity, and pooling.
3. Run the full EditMode suite. Fix feature regressions; report unrelated failures separately with exact test names and baseline evidence.
4. After each source edit, wait for Unity compilation and inspect Console errors. Before closing, inspect PF_Unicorn, PF_RainbowTrailSegment, PF_Player, SO_Unicorn_Default, the trail pool, ArenaBounds, portal prefab, and active scene wiring.

### Required manual smoke test

1. Test non-carrying versus carrying Player collision and Pot contact exactly as approved.
2. Test warning onset/clearing for grounded and carried Pots, multiple Pots, Pot destruction, retargeting, and restart.
3. Test repeated trail pool rent/return, floating visual stability, old/new Trail capture, and both portal directions.
4. Observe several top-edge cycles and portal exits to verify cross-map return, full-map access, Trail continuity, and no upper-area loop.
5. Record final Console state and any tuning retained after visual review.

### Completion criteria

- The Player-only carry immunity, 1-unit/0.5-second Unicorn daze, star cue, red Pot-impact line, floating Trail, portal continuity, and Top -> Bottom return rule match the approved decisions.
- Existing Pot destruction, harvesting, Trail persistence/capture cleanup, portals, Player recovery, pooling, and map access behave as before except for the approved feedback.
- Focused/relevant/full tests pass, manual smoke passes, and Unity Console has no new errors or warnings.

### Handoff steps

1. Add a recap after each completed phase with exact source/prefab/config/test paths, serialized values, automated evidence, manual observations, and Console state.
2. Keep the context glossary limited to approved gameplay terms: Pot-carry immunity, Unicorn daze, Pot-impact warning, Trail float, Portal trail continuity, and Cross-map return.
3. Refresh Graphify incrementally after final code/tests/docs. Verify the graph connects UnicornController, UnicornRoamingUtility, UnicornConfig, PlayerRoot, PotController/PotRegistry, RainbowTrailEmitter, RainbowTrailSegment, PortalInstance, PrefabPool, PF_Unicorn, and PF_RainbowTrailSegment. If semantic extraction is unavailable, record the code-only refresh rather than inventing metadata.
4. Inspect the exact diff and preserve unrelated user work. Checkpoint only the Unicorn feature when requested.

## Phase 4 completion record

Completed 2026-08-29.

- Final MainArena_3D wiring inspection passed: PF_Unicorn, PF_RainbowTrailSegment, PF_Player, PF_Portal, SO_Unicorn_Default, ArenaBounds, EffectsRoot, and the runtime RainbowTrailPool were found. PF_Unicorn has its warning LineRenderer assigned and disabled by default; PF_RainbowTrailSegment has FX_RainbowGlow_02 and its root collider; PF_Player retains stun feedback; SO_Unicorn_Default is knockback 1 and daze 0.5 seconds.
- Full EditMode verification passed 131/131 tests with 0 failures and 0 skipped. This includes the Phase 1 collision/warning/daze coverage, Phase 2 trail/portal continuity coverage, and Phase 3 Top -> Bottom routing coverage.
- The user-completed manual smoke test was reported passing. An additional ten-second Play Mode smoke pass completed with no project compilation/runtime errors.
- Final Unity Console was inspected and cleared. Editor returned idle with MainArena_3D active and no pending compilation.
- Graphify was refreshed after the final code/test pass.

The complete Unicorn feedback feature is ready for handoff.

## Explicitly deferred

- A new Unicorn attack state, damage model, health rework, player death/recovery change, audio, camera shake, screen effects, particle explosion, trail bridge, Trail expiry/fade/cap, Trail mesh/LineRenderer rewrite, new pooling system, generic trajectory system, generic teleport event bus, NavMesh/obstacle avoidance for Unicorn, top-third blocking volume, runtime settings UI, or map redesign.
- Any change to Pot economy/harvest timing, Portal placement/pairing/re-entry, enemy AI/waves, Player concealment/recovery rules, package assets, or shared material assets.
