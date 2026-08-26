# Upgrade Screen Feedback — Phase 1 Recap

Date: 2026-08-26
Status: Complete

## Scope

Phase 1 implements the global upgrade-screen toggle and keeps the existing gameplay lock/pause behavior.

## Implemented

- `Q` uses the existing `ShopInteract` input action and opens the upgrade screen from any player location.
- `Q` closes the upgrade screen while it is open.
- `Escape` continues to close the upgrade screen through the existing `Pause` input action.
- Opening the screen no longer checks the Fae Tree, interaction radius, disabled-player state, or carried-pot state.
- The Fae Tree reference remains in `ShopController` for existing pot-purchase spawn behavior.
- The existing time-scale and controller lock/restore path is reused; no new pause manager was added.
- The instructions text now tells the player to use `Q` or `Escape` to close the screen.

## Files

- `Assets/_Project/Runtime/Gameplay/Shop/ShopController.cs`
- `Assets/_Project/Runtime/UI/ShopPanelPresenter.cs`

## Verification

- Unity editor returned to Edit mode with `MainArena_3D` active.
- `ShopController.cs` and `ShopPanelPresenter.cs` validated with zero diagnostics.
- Console cleared and rechecked: zero errors and zero warnings.
- Focused EditMode tests passed: 2/2.
  - `ApplyPurchase_UpdatesResolvedWeaponStats`
  - `CanPurchase_BlocksPurchasesBeyondConfiguredMax`
- Play-mode smoke check entered and exited successfully.

## Manual acceptance still required

Because the Unity MCP session has no keyboard-injection tool, press `Q` during gameplay to confirm the panel opens anywhere, press `Q` again to close it, and press `Escape` to close it as well. Confirm movement, aiming, weapon firing, and time progression pause while open and restore after closing.
