# Tutorial + Portal Feedback — Phase 3 Recap

Status: Complete

## Goal

Make portal entry more forgiving by increasing the effective trigger radius to `0.60` world units without changing portal art, VFX, placement, pairing, teleport, cooldown, or re-entry behavior.

## Implemented

- Added `PortalConfig.EntryRadiusWorldUnits`, defaulting to `0.60f`.
- Added an Inspector tooltip clarifying that the value is the effective world-space trigger radius and does not resize VFX.
- `PortalInstance.Configure` now applies the configured world radius to its `CircleCollider2D` using the live uniform root scale:
  - world radius `0.60` at scale `1` becomes local radius `0.60`;
  - world radius `0.60` at scale `2` becomes local radius `0.30`.
- The collider remains a trigger.
- Zero or non-uniform X/Y scale logs an explicit error and skips radius conversion instead of silently applying an unpredictable circular trigger.
- Updated the project-local `PF_Portal` prefab collider to local radius `0.60` at its current uniform scale `(1,1,1)`.
- Serialized `SO_Portal_Default.EntryRadiusWorldUnits` to `0.60`.

## Preserved behavior

- Maximum active portals remains two.
- Third placement still removes the oldest portal.
- Portal pairing, VFX slot assignment, pool behavior, placement distance, exit offset, cooldown, target filtering, and re-entry lockout were not changed.
- No package source assets were edited.

## Automated verification

Focused EditMode run:

- `PortalRegistryTests.TryPlacePortal_PairsTwoPortalsAndReplacesOldestOnThirdPlacement` — passed.
- `PortalRegistryTests.TryPlacePortal_AppliesWorldEntryRadiusToScaledTrigger` — passed.
- Result: **2 total, 2 passed, 0 failed, 0 skipped**.

Unity MCP asset verification returned:

- `PF_Portal` root scale: `(1.00, 1.00, 1.00)`
- `PF_Portal` collider radius: `0.6`
- `PF_Portal` collider trigger: `True`
- `SO_Portal_Default.EntryRadiusWorldUnits`: `0.6`

## Manual verification

Physical player/Unicorn/Pot approach and portal re-entry checks remain part of the Phase 4 end-to-end smoke test. The focused test proves the scale-aware trigger configuration and existing portal-pair replacement behavior.

## Files changed

- `Assets/_Project/Runtime/Core/Config/PortalConfig.cs`
- `Assets/_Project/Runtime/Gameplay/Spells/PortalInstance.cs`
- `Assets/_Project/Tests/EditMode/Spells/PortalRegistryTests.cs`
- `Assets/_Project/Prefabs/PF_Portal.prefab`
- `Assets/_Project/ScriptableObjects/Defaults/SO_Portal_Default.asset`

## Phase boundary

Phase 3 changes only portal entry-radius configuration and its focused test coverage. Tutorial UI and sequence behavior remain as completed in Phases 1–2. Full tutorial/portal playthrough, graph refresh, and final handoff remain for later phases.

## Acceptance

- [x] Effective portal entry radius is `0.60` world units at the active prefab scale.
- [x] Portal visuals are unchanged.
- [x] The collider remains a trigger.
- [x] Two-portal replacement behavior passes.
- [x] Scale conversion is covered by a focused test.
- [x] No new compile errors were reported; the existing Unity Test Framework result-save entry remains the only console item classified as an exception.

