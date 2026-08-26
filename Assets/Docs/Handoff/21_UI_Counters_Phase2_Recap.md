# UI Counters Feedback - Phase 2 Recap

Status: Complete. Phase 3 remains pending.

## Delivered

- Added per-run `ElapsedRunSeconds`, `KillCount`, and
  `SuccessfulTheftEscapeCount` state to `RunDirector`.
- Reset all three values through the existing `Configure`/`InitializeRun`
  lifecycle.
- Counted elapsed scaled time only while no outcome is reported, preserving the
  existing Q pause and outcome freeze behavior.
- Registered `RunDirector` with the existing project service registry and kept a
  scene lookup fallback for enemy event reporting.
- Counted one kill from each guarded traveller, police, and military death
  handler.
- Counted a traveller escape only when positive `PotDrain.StolenGold` exists,
  before pool return, with a per-instance guard reset on reuse.
- Added focused `RunDirector` EditMode tests for accumulation and reset.

## Files changed

- `Assets/_Project/Runtime/Gameplay/Orchestration/RunDirector.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/TravellerController.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceController.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/MilitaryController.cs`
- `Assets/_Project/Tests/EditMode/Orchestration/RunDirectorTests.cs`

## Verification

- Unity 6.0.0.67f1 refreshed and compiled with no current compile errors.
- New `RunDirectorTests`: 2/2 passed.
- Combined new tests plus `RunWaveControllerTests`: 7/8 passed. The existing
  known failure remains `Tick_DoesNotSpawn_WhenNoPotHasGoldOrHarvesting`
  (`Expected: 0`, `But was: 1`); Phase 2 does not change wave gating.
- Full EditMode run: 85 tests executed; the four known baseline failures remain
  in friendly-hit filtering, traveller target fallback, wave spawning, and
  portal replacement. No Phase 2 test failed.
- Console contains only the prior Unity Test Framework setup/result-save
  messages and no Phase 2 compiler error.
- Integrated pause, outcome, hostile-death, escape, and pooled-reuse checks are
  intentionally deferred to Phase 4.

## Reference behavior

- Timer is scaled run time and stops after outcome reporting.
- Kill count includes travellers, police, and military deaths exactly once per
  guarded death path.
- Escape count includes only a traveller that leaves after stealing positive
  gold; zero-stolen retreats are excluded.

## Next step

Phase 3: author the compact Synty-styled Run Status HUD and bind it to Pot Gold,
ammo, timer, kills, and escapes.
