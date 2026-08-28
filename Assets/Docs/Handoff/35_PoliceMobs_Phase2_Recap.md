# Police Mobs — Phase 2 Recap

## Scope completed

- Replaced continuous police firing with a private three-phase cycle:
  `Approach` → `AimHold` → `Reposition`.
- Police path toward the player until the selected weapon's preferred range.
- Police stop, face the player, and hold the armed idle for the configured 0.25 seconds.
- A successful cycle fires exactly one normal weapon action. A shotgun's existing volley remains one cycle action.
- Failed fire attempts stay in the aim phase and retry safely without starting movement.
- Police strafe for the configured 0.75 seconds after a successful attack and never call `TryFire` while repositioning.
- Existing separation, path refresh, cover/line-of-sight, Rigidbody movement, `Speed`, and `Fire` animation wiring remain the source of truth.
- Invalid, disabled, out-of-range, blocked, dead, despawned, and pooled states reset velocity, timers, path, and phase.

## Code and tests

- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceController.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceSpawner.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceCombatCycleUtility.cs`
- `Assets/_Project/Tests/EditMode/Enemies/PoliceCombatCycleUtilityTests.cs`

The shared `PoliceMobConfig` is passed into each spawned controller, so the aim and reposition durations remain Inspector-tunable.

## Verification

- Focused cycle tests: **4 passed, 0 failed**.
- Full EditMode suite: **110 passed, 0 failed, 0 skipped**.
- PoliceController and helper validation: **0 errors**.
- Play Mode launch: successful; no new gameplay console errors. Automated observation stopped at the existing tutorial modal before a wave could spawn police.

## Deferred by approved plan

- Red laser telegraph, new aim parameters, clips, audio, muzzle VFX, and controller edits remain intentionally deferred.
- Wave counts and exclusive death rewards remain Phase 3.
