# Tutorial and Portal Feedback - Phase 1 Recap

Date: 2026-08-27  
Status: Complete  
Scene: `Assets/_Project/Scenes/MainArena_3D.unity`  
Unity: `6000.0.67f1`  

## Goal

Create the smallest testable runtime foundation for the ordered tutorial sequence and the approved global opt-out. Keep gameplay non-blocking and defer scene/UI wiring to Phase 2.

## Implemented runtime foundation

- `TutorialSequenceState` owns the fixed order: PickUpPot, PlaceOnTrail, PlacePortal, OpenUpgrades.
- Completion is sequential. Completing a later step early is ignored.
- `Reset()` starts a fresh in-memory run; no step progress is persisted.
- `TutorialSequenceController` evaluates only the active step and advances after the existing gameplay state reports success.
- The controller reads one persistent PlayerPrefs key: `RainbowsEnd.TutorialPromptsSuppressed`.
- The opt-out is one-way for the current implementation: it saves immediately, resets in-memory progress, hides the prompt, and suppresses future runs.
- `TutorialPromptView` exposes message and card visibility, resets the Toggle when a new step is shown, and forwards the opt-out callback.
- Tutorial prompts do not pause the game, capture input, or add a second input owner.

## Existing gameplay seams added

- `PotRegistry.HasHarvestingPot` returns true only for a live, uncarried pot whose `HarvestStatus` is `Harvesting`.
- `ShopController.IsShopOpen` exposes the existing `_isShopOpen` lifecycle state without changing how the shop opens or closes.
- Pickup completion observes `PlayerPotCarryController.IsCarryingPot`.
- Portal completion observes `PortalRegistry.ActivePortals.Count > 0`.

## Approved tutorial copy

1. `Pick up and put down the Pot of Gold by pressing the 'E' button.`
2. `Place the pot of gold on the Rainbow Trail to make gold. Protect what's yours.`
3. `Place a portal by right-clicking, only two portals at a time.`
4. `Spend/Upgrade by pressing the 'Q' button.`

Opt-out label: `Don't show tutorial prompts again`

## Automated validation

- Unity compilation completed successfully after import.
- Focused EditMode run: 13 total, 13 passed, 0 failed, 0 skipped.
- Covered tests include ordered progression, reset behavior, PlayerPrefs opt-out persistence, live/idle/carried/destroyed harvesting pots, and ShopController open/close/disable lifecycle.
- Final console check returned no code compilation errors. Unity Test Framework emitted its normal test-run setup/result messages only.
- `git diff --check` passed; Git reported only the repository's existing LF/CRLF normalization notices.

## Files changed

- `Assets/_Project/Runtime/UI/TutorialSequenceState.cs`
- `Assets/_Project/Runtime/UI/TutorialPromptView.cs`
- `Assets/_Project/Runtime/UI/TutorialSequenceController.cs`
- `Assets/_Project/Runtime/Gameplay/Pots/PotRegistry.cs`
- `Assets/_Project/Runtime/Gameplay/Shop/ShopController.cs`
- `Assets/_Project/Tests/EditMode/UI/TutorialSequenceTests.cs`
- `Assets/_Project/Tests/EditMode/Pots/PotRegistryTests.cs`

## Phase boundary

No MainArena scene object, Synty prefab, Canvas hierarchy, or portal collider was changed in Phase 1. Phase 2 will create the Synty tutorial card, bind its serialized references, and verify the four prompts in Play Mode.

## Acceptance

- [x] Ordered tutorial state is deterministic and testable.
- [x] Tutorial progress resets per run.
- [x] Global opt-out persists immediately and suppresses future runs.
- [x] Prompt view is non-blocking and callback-driven.
- [x] Trail and shop completion have read-only runtime seams.
- [x] Focused tests pass in Unity.
- [x] No scene/UI integration was pulled into this phase.