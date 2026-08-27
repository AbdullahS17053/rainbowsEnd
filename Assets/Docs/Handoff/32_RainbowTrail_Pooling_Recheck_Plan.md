# Rainbow Trail Pooling Recheck and Bounded-Trail Plan

Status: Implemented — focused verification complete; long-run profiling pending

## Corrected conclusion

The `(Clone)` suffix is normal Unity naming, not evidence that pooling failed. `PrefabPool` creates a prefab instance when it must grow, and Unity preserves that name even after the instance is returned and rented again.

The existing gameplay rule is also intentional: a Rainbow Trail segment stays visible and interactable until a Pot first captures it and begins its timed harvest. Do **not** add time-based trail expiry. `UnicornConfig.TrailLifetimeSeconds` is currently unused and must not be reactivated as an expiry rule.

`PotController` already calls `RainbowTrailEmitter.ClearAllTrail()` immediately after successful capture. `RainbowTrailSegment.ReturnAllToPool()` returns every active segment to `RainbowTrailPool`.

The remaining performance risk is different: while no Pot has captured the current trail, every emission rents another active SpriteRenderer/Collider2D. Pooling cannot cap that active count. It only prevents allocation churn after objects are returned. The Unicorn continues moving and emitting immediately after `ClearAllTrail()`, which is the current implementation's way to begin the next trail.

## Recheck evidence

- `RainbowTrailEmitter.Update()` emits while the Unicorn has non-zero velocity; it has no Pot/harvest condition.
- `PotController` captures a valid overlapping segment, starts timed harvesting, clears all existing trail segments, then marks that Pot's current drop as consumed.
- During a live Play Mode probe, `ClearAllTrail()` reduced the active segment count to `0`. With the Unicorn still moving, the count was `21` again after `2.5` seconds. This confirms return-to-pool works and a new trail begins straight away.
- The active scene pool prewarms `48` segments. Its `maxPoolSize` limits inactive retained objects only; it does not limit how many active segments can exist.

## Required gameplay invariant

1. A trail must remain visible and capturable until a Pot starts harvesting from it.
2. Capturing a trail must calculate the existing age/rate/duration exactly as it does today, then return the captured trail segments to the pool.
3. A new trail may begin after capture only if that is desired for a later Pot drop. This is the current behavior and is not changed by this plan unless Phase 0 confirms the client wants emission paused during harvesting.
4. No segment may disappear merely because it is old, and no arbitrary active-count cap may silently remove a valid part of a trail.

## Proposed minimal solution

If profiling confirms the active trail has a material cost, keep the current pooled prefab and represent each finite arena cell at most once per active trail. When the Unicorn revisits an already-covered cell, skip the additional rent; keep the original active segment and timestamp. This makes the maximum active count finite for the arena while preserving a visible, capturable trail until Pot capture.

This is deliberately not a LineRenderer/mesh rewrite, a new pooling framework, or an expiry system. It reuses `RainbowTrailEmitter`, `RainbowTrailSegment`, `PrefabPool`, and the existing global cleanup path.

## Phase 0 — Confirm the performance boundary and next-trail rule

### Goal

Decide whether a code change is justified and remove the ambiguity about trail emission after a Pot captures the current trail.

### Steps

1. Record the active `MainArena_3D` world bounds, Unicorn move speed, `TrailSpawnIntervalSeconds`, segment collider footprint, and current pool prewarm/maximum values. Calculate the current emission rate and a conservative active-segment count for a 30-second and 120-second uncaptured trail.
2. In Play Mode, run two representative cases without changing project data:
   - leave a Pot unplaced while the Unicorn roams for two minutes;
   - place a Pot on a valid trail, confirm all old segments return, then observe whether a fresh trail is intended to form while the Pot harvests.
3. Capture Unity Profiler/Stats evidence for CPU, managed allocations, draw calls, Physics2D contacts, and active `RainbowTrailSegment` count at the start, after 30 seconds, and after 120 seconds. The count—not `(Clone)` names—is the pooling health signal.
4. Confirm the product rule for the fresh trail after capture:
   - keep the current behavior if it is meant to support a later pickup/drop or another Pot;
   - pause emission while harvesting only if the client explicitly wants no next trail until harvesting ends or the Pot is moved/destroyed.
5. Set an acceptance budget from the measured target platform. Do not invent a hard object cap before this measurement, because a cap would silently remove valid gameplay trail.

### Acceptance criteria

- The team has a measured peak active-segment count and frame/allocation baseline.
- The next-trail rule is recorded as either current continuous emission or an approved pause boundary.
- No runtime, scene, prefab, config, or package files change in this phase.

### Decision gate

- If the 120-second representative run stays within the agreed frame/allocation budget, retain the current implementation. `(Clone)` names require no fix.
- If it exceeds budget, continue to Phase 1. Do not use `TrailLifetimeSeconds` as a shortcut.

## Phase 1 — Define bounded spatial trail coverage

### Goal

Bound repeated Unicorn passes through the finite arena without making a valid trail expire.

### Technical steps

1. Add one serialized, minimum-positive `TrailCellSizeWorldUnits` setting to `UnicornConfig`. Set its initial value from the measured segment visual/collider footprint in Phase 0, not from an arbitrary round number. It is a visual-density tuning value, not a lifetime or object-count limit.
2. Quantize the Unicorn's world position into a stable `Vector2Int` cell key using that cell size. Positions in the same cell represent the same visible trail coverage.
3. Extend the existing `RainbowTrailSegment` active registry with a cell-to-segment lookup. It must expose the smallest required operation: determine whether an active segment already covers a cell and register/remove a segment's cell.
4. Before `RainbowTrailEmitter` rents, ask whether the current cell is already active. If it is, skip that emission. If not, rent/configure/register exactly as today.
5. Preserve the timestamp of the first segment in a cell. `PotTrailCaptureSensor` already selects the oldest overlapping active segment; preserving that timestamp maintains existing harvest-age behavior for revisited paths.
6. Make `ReturnAllToPool()` clear both the existing active list and the new cell lookup. `OnDespawned()` must also remove only its own lookup entry, so a stale pooled segment cannot erase a newer segment that happens to use the same cell.
7. Keep `PotController` capture calculations, `ClearAllTrail()`, pool names, prefab art, colliders, and `TrailSpawnIntervalSeconds` unchanged. Do not add a second pool, event bus, manager, or runtime object.
8. Remove the unused `TrailLifetimeSeconds` setting only after a repository search confirms no asset, test, tool, or documentation still treats it as supported gameplay tuning. Re-save the Unicorn config asset through Unity so serialization remains valid.

### Acceptance criteria

- Revisiting the same covered area does not rent another active segment.
- Previously covered trail remains visible and can still trigger Pot capture.
- A successful capture returns all current cells/segments, and the next trail starts with an empty cell lookup.
- Maximum active segments are bounded by arena coverage and selected cell density, rather than uncaptured play duration.

### Rollback

Revert the cell-size setting, lookup, emitter admission check, and their focused tests together. Existing pooled segment behavior remains intact.

## Phase 2 — Implement with focused verification

### Goal

Apply the smallest code change that enforces spatial uniqueness and prove it does not alter harvesting.

### Code steps

1. Add the cell-key calculation beside the existing Rainbow Trail code. Keep it a small deterministic method; do not create a generic grid framework.
2. Update `RainbowTrailSegment.Activate(...)` to receive/store its cell key and register it after the existing pool ownership/timestamp setup.
3. Update `OnDespawned()` and `ReturnAllToPool()` to remove registry state safely before a segment can be reused.
4. Update `RainbowTrailEmitter.SpawnTrailSegment()` to return early for an already-active cell, before `PrefabPool.Rent(...)`. The existing cooldown still governs how often the emitter asks to create coverage.
5. If Phase 0 approves pausing the next trail during harvesting, implement that as a separate, explicitly scoped change after the cell work. Its resume owner must be the real Pot harvest lifecycle; do not infer harvesting from raw input or private polling in the emitter.

### Automated verification

1. Add focused EditMode coverage for stable world-position-to-cell conversion, including negative coordinates and positions on a cell boundary.
2. Add a small Rainbow Trail lifecycle test proving that registering the same cell twice retains one active entry, and that returning all segments clears the lookup for a fresh trail.
3. Retain/run existing `PrefabPoolTests`, `PotHarvestStateTests`, `PotTrailCaptureSensorTests`, `PotPlacementTests`, and Unicorn roaming tests. The new work must not alter Pot age/rate/duration or placement behavior.
4. After each script change, wait for Unity compilation and inspect Console errors before serialized config work.

### Manual verification

1. Watch the Unicorn traverse the same route repeatedly before Pot placement. The trail remains visually continuous, but the active segment count plateaus after arena coverage rather than rising each lap.
2. Place a Pot on old and recently covered trail positions. Confirm capture, gold rate, harvest duration, visuals, and all-segment return remain correct.
3. After capture, confirm the approved next-trail behavior from Phase 0 and verify the new path can populate cells again.
4. Run the 120-second representative roam again and compare the active count, frame time, allocations, and draw calls against Phase 0.

### Acceptance criteria

- No new compiler or Console errors.
- Focused trail checks and relevant baseline gameplay tests pass.
- The active count plateaus within the calculated arena-cell budget while an uncaptured trail persists.
- Pot capture remains the only normal cleanup event for that trail.

## Phase 3 — Handoff and graph refresh

### Goal

Leave the corrected gameplay language and measured result clear for future work.

### Steps

1. Add a Phase recap with the approved next-trail rule, before/after active counts, selected cell size, computed arena-cell ceiling, profiler result, test result, and manual checks.
2. Update `CONTEXT.md` only with the domain rule: Rainbow Trail segments persist until Pot capture; repeated coverage is spatially coalesced; capture clears the current trail and begins the next approved trail phase.
3. Refresh Graphify incrementally after code, config, tests, and recap are final. Verify the graph connects `RainbowTrailEmitter`, `RainbowTrailSegment`, `PotController`, `PotTrailCaptureSensor`, `PotHarvestState`, and `PrefabPool`.
4. Inspect the exact diff, run a whitespace check, and checkpoint only the Rainbow Trail code/config/test/document changes. Leave unrelated work untouched.

## Explicitly deferred

- Time-based trail expiry, fading, automatic deletion, or a hard active-object cap.
- A LineRenderer, dynamic mesh, tilemap, DOTS/ECS rewrite, new pooling package, generic spatial-index framework, or an additional trail manager.
- Renaming Unity pooled instances to remove `(Clone)`; that changes only Editor presentation and has no runtime benefit.
- Any decision to pause the Unicorn's next trail during harvesting until the product rule is explicitly confirmed.
