# Tutorial + Portal Feedback — Phase 4 Recap

Status: Complete

## Goal

Run the tutorial and forgiving portal changes through the existing test suite and live `MainArena_3D` run loop, then record any regressions or remaining client checks.

## Automated verification

The complete available EditMode suite was run after the Phase 3 changes:

- **97 total**
- **97 passed**
- **0 failed**
- **0 skipped**

This includes the tutorial state/controller coverage, Pot and Player interaction coverage, Shop coverage, the new portal-radius test, and the existing two-portal replacement test.

## Live Unity MCP acceptance checks

The active scene was `Assets/_Project/Scenes/MainArena_3D.unity` in Unity `6000.0.67f1`.

- Play Mode started with the tutorial card visible and the exact pickup copy.
- The real `PlayerPotCarryController` pickup seam was exercised; the Pot entered its carried state and the tutorial advanced to the exact Trail message.
- Live portal placement created two paired portals with a `0.60` world-unit trigger. A third placement left exactly two active portals and replaced the oldest.
- The live tutorial state advanced from the Trail probe to the portal condition and showed the exact Upgrade message.
- The real `ShopController.OpenShop` seam opened the shop, paused the run (`Time.timeScale = 0`), and the tutorial hid on the next frame with `IsFinished = true`.
- The live tutorial checkbox invoked the global opt-out, hid the card immediately, and wrote `RainbowsEnd.TutorialPromptsSuppressed = 1`. The key was cleared afterward so the developer test profile is not left opted out.
- After validation, Play Mode was stopped. Unity MCP reported the active scene idle, not playing, and not compiling.

## Manual/client smoke-test note

The MCP acceptance pass exercised the production seams and live scene objects. A physical keyboard/mouse pass remains useful for client sign-off: press `E`, place the Pot on a moving Rainbow Trail, right-click near portal edges with Player/Unicorn/Pot, and press `Q` while checking the final HUD. No code or scene changes are required for that pass.

## Preserved behavior

- Tutorial order, exact copy, non-blocking card behavior, one-way global opt-out, and shop-overlay ordering remain unchanged.
- Portal visuals, placement, cooldown, exit offset, target filtering, pairing, two-portal cap, pooling, and re-entry lockout remain unchanged apart from the approved entry-radius tuning.
- No new package assets or unrelated scene objects were modified.

## Files changed in Phase 4

- `Assets/Docs/Handoff/30_Tutorial_Portal_Phase4_Recap.md`

Earlier feature phases contain the runtime, scene, prefab, config, and focused test changes.

## Phase boundary

Phase 4 adds no gameplay code. It records the full regression and live acceptance evidence. Phase 5 remains for final handoff, Graphify refresh, and the version-control checkpoint.

## Acceptance

- [x] Full EditMode suite passes with no failures.
- [x] Tutorial appears at run start and advances from the production pickup seam.
- [x] Portal entry uses the approved forgiving radius and retains the two-portal rule.
- [x] Shop opening hides the final tutorial prompt and pauses the run.
- [x] Global opt-out hides the prompt and persists through PlayerPrefs.
- [x] Editor returned to idle after validation.
- [ ] Physical keyboard/mouse client smoke test remains for final sign-off.

