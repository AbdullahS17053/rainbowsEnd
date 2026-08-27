# Tutorial and Portal Feedback - Phase 2 Recap

Date: 2026-08-27  
Status: Complete  
Scene: `Assets/_Project/Scenes/MainArena_3D.unity`  
Unity: `6000.0.67f1`  

## Goal

Integrate one readable, non-blocking tutorial card into the existing MainArena HUD using the selected Synty UI style. Keep the controller active while the card itself can hide.

## Scene integration

- Added `UI/HudRoot/TutorialSystem` with the existing `TutorialSequenceController`.
- Added `UI/HudRoot/TutorialSystem/TutorialPrompt` as an instance of:
  `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/Objectives_Story_Location/HUD_FantasyWarrior_Event_DialogueBox_01.prefab`.
- The controller parent stays active; the card child is inactive in edit time and is enabled by the controller at run start.
- The card is bottom-centre anchored at `900 x 260` UI units with a `72` unit bottom offset.
- The tutorial system is ordered before `ShopPanel`, so the upgrade screen renders above it. Existing `RunStatusHud` remains unobstructed at the top-left.
- The card uses the Synty frame/background and existing Synty label/font styling. The original dialogue label is the message field.
- The opt-out row uses a standard Unity Toggle with a Synty frame sprite, a gold checkmark, and the exact label:
  `Don't show tutorial prompts again`.
- Card graphics have raycast targets disabled; only the Toggle background receives UI clicks. No full-screen blocker or second Canvas/EventSystem was added.
- All four controller step definitions and exact client messages are serialized on the scene object. Gameplay references are assigned to the existing PlayerRoot, PotRegistry, PortalRegistry, and ShopController.

## Play Mode verification

- Cleared only `RainbowsEnd.TutorialPromptsSuppressed` for the verification run.
- First prompt appeared in the live Game View with the existing HUD still visible.
- Game time continued while the prompt was visible, confirming the card is non-blocking.
- The longer Trail instruction wrapped to two readable lines without truncation.
- Live scene verification returned all four exact messages in order.
- Checking the Toggle persisted the preference (`1`) and immediately hid the card.
- A fresh run advanced the tutorial to `OpenUpgrades`; opening the real `ShopController` set `IsShopOpen = true`, completed the sequence, and hid the card.
- Temporary screenshots used for review were removed after verification; no screenshot assets remain in the worktree.

The MCP-driven checks exercised the real scene objects and runtime controller seams. Physical keyboard/mouse playthrough of E, right-click, and Q remains part of the final client smoke test.

## Validation

- Unity remained on `MainArena_3D` and exited Play Mode cleanly.
- No project scripts were changed in this phase, so the Phase 1 compile/test baseline remains applicable.
- Scene save completed through Unity MCP after the card hierarchy and serialized references were assigned.

## Files changed

- `Assets/_Project/Scenes/MainArena_3D.unity`

## Phase boundary

Phase 2 changes only the MainArena scene UI integration. It does not change the portal collider/configuration or gameplay input code. Phase 3 will apply the approved `0.60` world-unit portal entry radius without resizing portal VFX.

## Acceptance

- [x] One Synty tutorial card is integrated into the existing HUD.
- [x] Card is bottom-centre, readable, and non-blocking.
- [x] Exact four messages and global opt-out label are present.
- [x] Checkbox persists the global opt-out and hides the card.
- [x] Shop ordering and final-card hide behavior are verified.
- [x] Existing Canvas, EventSystem, HUD, and ShopPanel were preserved.