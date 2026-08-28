# Police Mobs — Phase 3 Recap

Status: Completed

## Scope

Phase 3 increased later-wave police pressure and replaced the old police death-drop path with one deterministic, pooled, exclusive reward result.

## Wave defaults

The active `SO_Wave_Default` asset now contains these police totals:

| Wave | Police |
| --- | ---: |
| 1 | 0 |
| 2 | 0 |
| 3 | 3 |
| 4 | 4 |
| 5 | 5 |

Start delays, spawn intervals, max concurrency, and `RunWaveController` scheduling were not changed. Existing round-robin scheduling still staggers entries.

## Reward behavior

- One death-time roll uses `PoliceMobConfig.DeathDropChance` (`0.33` by default).
- A failed roll produces no reward.
- A successful roll produces exactly one reward:
  - `50%`: the police mob's equipped weapon with matching ammo (`12` pistol or `6` shotgun).
  - `50%`: `10` Pot Gold.
- Missing pickup pools/configuration safely fall back to the available reward or produce no reward.
- Existing pooled `WeaponPickup` and `GoldPickup` systems are reused.
- Pool spawn/despawn clears reward, loadout, pool, target, timer, and combat-cycle state.

## Changed paths

- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceDeathRewardUtility.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceController.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceSpawner.cs`
- `Assets/_Project/Editor/Phase15PoliceRewardsSetup.cs`
- `Assets/_Project/ScriptableObjects/Defaults/SO_Wave_Default.asset`
- `Assets/_Project/Tests/EditMode/Enemies/PoliceDeathRewardUtilityTests.cs`

## Verification

- Focused reward + wave tests: **16/16 passed**.
- Full EditMode suite: **117/117 passed**.
- Script validation: no errors in the new helper, test, setup script, spawner, or controller. The controller retains two existing analyzer warnings about Rigidbody operations and string allocation in `Update()`.
- Active scene confirmed: `Assets/_Project/Scenes/MainArena_3D.unity`.
- Active PoliceSpawner confirmed with `SO_PoliceMob_Default` and `GoldPickupPool` references.
- No new Unity compilation errors were reported. The console still contains the earlier Unity Performance Testing setup/result messages.
- Manual reward distribution/playthrough smoke test was completed successfully by the user in Phase 4.

## Phase 4 handoff

Run the full police gameplay smoke test through Waves 3–5, confirm mixed loadouts and exclusive rewards in play, then update the final handoff/context and Graphify records.
