# Pot of Gold Feedback — Phase 1 Recap

## Change completed

Updated `Assets/_Project/ScriptableObjects/Defaults/SO_Pot_Default.asset`:

- `BaseHarvestGoldPerSecond`: `1` → `2`

This doubles the default pot's base gold generation before the existing trail-age multiplier is applied.

## Explicitly unchanged

- `TrailAgeGoldMultiplier`: `0.35`
- `AutoBankGoldPerSecond`: `0.5`
- Storage limit and pot health
- Enemy drain behavior
- Harvest state transitions and trail capture behavior

The verified read-back was:

```text
BaseHarvestGoldPerSecond=2; TrailAgeGoldMultiplier=0.35; AutoBankGoldPerSecond=0.5
```

## Tests

All four existing `PotHarvestStateTests` passed:

- `CaptureTrail_ThenGenerateGold_UsesHarvestMultiplier`
- `MovingPastCancelDelay_InterruptsHarvest`
- `ReleaseTrail_StopsHarvestAndReturnsIdle`
- `InterruptedHarvest_ResumesWhenTrailIsStillPresentAndMovementStops`

A full EditMode run executed 77 tests and reported six unrelated existing failures in combat targeting, economy removal, pot draining, traveller targeting, wave orchestration, and portal replacement. No Phase 1 code or test failure points to the changed pot configuration asset.

## Unity validation

- Active scene: `MainArena_3D`; not dirty.
- Scene validation: 0 issues, 0 missing scripts, 0 broken prefabs.
- Editor: idle and not compiling.
- Console after test-run cleanup: 0 entries.

## Next phase

Phase 2 can now replace the old harvest flare and wire the state-driven glow/flair behavior. No FX or prefab work was included in Phase 1.
