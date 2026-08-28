# Police Mobs — Phase 1 Recap

## Scope completed

- Added `PoliceMobConfig` as the police-specific ScriptableObject for loadout references and later police tuning.
- Added deterministic loadout selection through `PoliceLoadoutSelectionUtility`.
- Configured the default police mob for a 50/50 pistol-versus-shotgun split.
- Added the Synty shotgun mesh to `PF_Police` and kept pistol/shotgun visuals mutually exclusive at runtime.
- Reused the project-owned `Military.controller` on the police character Animator, preserving the existing `Speed` and `Fire` parameters for the combat phase.
- Passed the selected weapon configuration into each spawned police controller.
- Kept the existing legacy weapon-drop path as a compatibility fallback; death-drop behavior remains Phase 3.
- Wired the active `PoliceSpawner` to `SO_PoliceMob_Default` and the existing `GoldPickupPool` for later Phase 3 use.

## Configured defaults

| Setting | Value |
| --- | ---: |
| Pistol chance | 50% |
| Aim hold | 0.25 seconds |
| Reposition window | 0.75 seconds |
| Death-drop chance | 33% |
| Weapon reward split | 50% |
| Pistol reward ammo | 12 |
| Shotgun reward ammo | 6 |
| Gold reward amount | 10 |

## Files and assets

- `Assets/_Project/Runtime/Core/Config/PoliceMobConfig.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceLoadoutSelectionUtility.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceSpawner.cs`
- `Assets/_Project/Runtime/Gameplay/Enemies/PoliceController.cs`
- `Assets/_Project/Tests/EditMode/Enemies/PoliceLoadoutSelectionUtilityTests.cs`
- `Assets/_Project/Editor/Phase14PoliceLoadoutSetup.cs`
- `Assets/_Project/ScriptableObjects/Defaults/SO_PoliceMob_Default.asset`
- `Assets/_Project/Prefabs/PF_Police.prefab`
- `Assets/_Project/Scenes/MainArena_3D.unity`

## Verification

- Focused loadout tests: **3 passed, 0 failed**.
- Full EditMode suite: **106 passed, 0 failed, 0 skipped**.
- Unity console after setup: no new compiler or setup errors.

## Deferred to later phases

- Phase 2: movement/aim/fire/reposition combat cycle and laser telegraph.
- Phase 3: staggered police wave counts and 33% death rewards.
- Phase 4: runtime smoke test, balance pass, and graph refresh.
