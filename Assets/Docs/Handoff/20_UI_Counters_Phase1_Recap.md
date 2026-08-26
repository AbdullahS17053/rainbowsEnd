# UI Counters Feedback - Phase 1 Recap

Status: Complete. Phase 2 remains pending.

## Delivered

- Removed `PotConfig.AutoBankGoldPerSecond` and the serialized default value.
- Removed the hidden `EconomyState` wallet and `EconomyConfig.StartingGold`.
- Kept `EconomyState` only for active-harvester tracking and harvest multipliers.
- Added capacity-aware `PotController.TryStoreGold(float)`.
- Changed `PotRegistry` to aggregate fractional gold once, display the floored combined value, deposit oldest-live-pot first, and spend oldest-live-pot first.
- Rebound `EconomyHudPresenter`, `ShopController`, and `ShopPanelPresenter` to `PotRegistry`.
- Changed `GoldPickup` to deposit into live pots and remain collectible when capacity is unavailable.
- Updated Phase 6 and legacy Phase 11 setup callers.

## Verification

- Unity 6.0.0.67f1 refreshed and compiled with no current compile errors.
- Focused EditMode run: 10/10 passed.
- Full EditMode run: 83 tests executed; the 10 Phase 1 tests passed. Four unrelated known failures remain in friendly-hit filtering, traveller target fallback, wave spawning, and portal replacement.
- Manual combined playthrough is intentionally deferred to Phase 4, where the new HUD and run counters are available together.

## Reference behavior

Pot Gold is the only spendable balance. It is the sum of gold held by live pots; no gold is deposited at the Fae Tree or stored in `EconomyState`.
