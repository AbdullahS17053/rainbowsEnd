# Tutorial Global Opt-Out Click Fix

Status: Complete

## Symptom

The global tutorial opt-out checkbox was visible but did not respond to a pointer click.

## Root cause

The reused Synty dialogue prefab retains `CanvasGroup` state for its animated layout. The scene instance had both inherited groups disabled:

- `TutorialPrompt/Content`: `interactable = false`, `blocksRaycasts = false`
- `TutorialPrompt/Content/Name`: `interactable = false`, `blocksRaycasts = false`

Because the checkbox is inside those groups, Unity’s `GraphicRaycaster` discarded the pointer hit even though the checkbox image itself had `raycastTarget = true` and the `Toggle` was interactable.

## Fix

Updated the project-local `MainArena_3D` tutorial card instance so the static card’s `Content` and `Name` `CanvasGroup` components have:

- `interactable = true`
- `blocksRaycasts = true`

The card’s non-toggle graphics remain non-raycastable, so the tutorial remains non-blocking outside the checkbox.

## Verification

- Fresh Play Mode run showed the tutorial card with `suppressed = false`.
- `GraphicRaycaster` hit `OptOutToggle` at its screen-space center.
- A Unity pointer-click event changed `isOn` to `true`.
- The runtime listener set `IsTutorialSuppressed = true`, hid the card, and wrote `RainbowsEnd.TutorialPromptsSuppressed = 1`.
- The preference was cleared after the check so the developer profile is not left opted out.
- Tutorial controller regression tests: **4/4 passed**.
- Unity editor returned to idle and not compiling.

## Files changed

- `Assets/_Project/Scenes/MainArena_3D.unity`
- `Assets/Docs/Handoff/31_Tutorial_OptOut_Click_Fix_Recap.md`

