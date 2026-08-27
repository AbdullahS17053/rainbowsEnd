# Tutorial and Portal Feedback — Phase 0 Recap

Date: 2026-08-27  
Status: Complete  
Scene: `Assets/_Project/Scenes/MainArena_3D.unity`  
Unity: `6000.0.67f1`  

## Baseline result

- Unity MCP is connected to `rainbowsEnd@8545bd2c`.
- The editor is idle, not in Play Mode, not compiling, and `MainArena_3D` is active.
- The Unity Console returned zero errors and zero warnings during this phase.
- Phase 0 made no gameplay, scene, prefab, or package-source changes.

## Verified tutorial integration anchors

| Concern | Live/source seam | Phase 2 usage |
| --- | --- | --- |
| Tutorial card parent | `UI/HudRoot` (`RectTransform` fills the 1920x1080 canvas) | Add the card under `HudRoot`, bottom-centre, leaving `RunStatusHud` at top-left unobstructed. |
| Existing HUD | `UI/HudRoot/RunStatusHud` is 320x400 at the top-left | Preserve its current order and bounds. |
| Upgrade panel | `UI/HudRoot/ShopPanel`, 850x850 centred and inactive until opened | Tutorial completion observes the existing shop-open state; no second Q/input owner. |
| Player pickup | `GameplayRoot/Player` → `PlayerPotCarryController` | Observe the existing successful carry state. |
| Trail harvesting | `GameplayRoot/PotRegistry` → registered live `PotController` objects | Add only the planned read-only harvesting query. |
| Portal placement | `GameplayRoot/SpellSystems/PortalRegistry` | Observe active portal count after successful registry placement. |
| Upgrade opening | `GameplayRoot/ShopSystems/FaeTreeShop` → `ShopController` | Add only the planned read-only `IsShopOpen` property. |

## Synty assets inspected

Selected for reuse in Phase 2:

- Card shell: `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/Objectives_Story_Location/HUD_FantasyWarrior_Event_DialogueBox_01.prefab`
  - Provides a framed Synty panel with background/frame layers, layout groups, animation, and TMP labels.
- Optional compact alternative: `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/Popups_Notifications/HUD_FantasyWarrior_Tooltip_01.prefab`
  - Provides a framed notification/tooltip layout with icon support and a key-button region.
- Input visual: `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/Input_Interactions/_Parts/Input_FantasyWarrior_KeyButton_01.prefab`
  - Provides the existing Synty keyboard/button treatment for `E`, right-click, and `Q` presentation.
- Existing project font: `Assets/Synty/InterfaceFantasyWarriorHUD/Fonts/Grenze/Grenze-SemiBold SDF.asset`.
- Synty has no generic `Checkbox`/`Checkmark` prefab under the inspected HUD prefab/sprite paths. Use a standard `UnityEngine.UI.Toggle` with the Synty card/button treatment rather than adding a custom asset or editing the package.

## Portal baseline

- Prefab: `Assets/_Project/Prefabs/PF_Portal.prefab`.
- Root scale: `(1,1,1)`; uniform.
- Root trigger: `CircleCollider2D`, `isTrigger = true`, local radius `0.45`.
- Current effective world-space entry radius: `0.45` units.
- Portal prefab contains the existing round portal particle/VFX children; Phase 3 must not resize them.
- Config asset: `Assets/_Project/ScriptableObjects/Defaults/SO_Portal_Default.asset`.
- Current serialized portal values include `CooldownSeconds = 0.5`, `MaxActivePortals = 2`, `PlacementDistance = 1.5`, `ExitOffsetDistance = 0.85`, and `ReentryLockoutSeconds = 0.25`.
- Approved target remains `0.60` world units, with the collider conversion kept separate from visual scale.

## Worktree note

Existing user changes remain uncommitted. Phase 0 did not stage, revert, or checkpoint them. Current known feature files added/modified by this planning work are:

- `CONTEXT.md`
- `Assets/Docs/Handoff/25_Tutorial_Portal_Feedback_Plan.md`
- `Assets/Docs/Handoff/26_Tutorial_Portal_Phase0_Recap.md`

Generated Unity `.meta` files are expected after import. Unrelated project changes and graphify caches remain out of scope.

## Phase 0 acceptance

- [x] Active Unity scene and editor readiness verified.
- [x] Existing gameplay/UI seams verified against the live scene.
- [x] Synty card, tooltip, key-button, font, and checkbox fallback recorded.
- [x] Current portal prefab scale/radius/config recorded.
- [x] Plan corrected to match the live portal baseline.
- [x] No implementation changes made.