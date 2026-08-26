# Upgrade Screen Feedback — Phase 2 Recap

Date: 2026-08-26
Status: Complete

## Scope

Phase 2 updates the shop’s upgrade catalog data and balance values.

## Implemented

- Added `ShopUpgradeEffectType.WeaponRange` as the new upgrade effect type.
- Halved the five existing upgrade costs using floor rounding: `20→10`, `25→12`, `15→7`, `50→25`, `75→37`.
- Added the sixth catalog entry: `Cone Reach Up`, cost `12`, maximum purchases `3`, value `+2` world units per purchase.
- Description: `Increase firing-cone and arrow range by 2 units per purchase.`

## Files

- `Assets/_Project/Runtime/Core/Config/ShopConfig.cs`
- `Assets/_Project/Settings/Balance/SO_ShopConfig.asset`

## Verification

- `ShopConfig.cs` validates with zero diagnostics.
- Unity console contains zero errors and warnings after import.
- Existing focused EditMode shop tests passed: 2/2.
- Unity remained in Edit mode with `MainArena_3D` active.

## Next phase boundary

The new data is catalog-ready. Runtime application of the range bonus and final six-row UI presentation remain separate follow-up work.
