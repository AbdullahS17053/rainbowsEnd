# Rainbow's End — UI Counters Phase 0 Recap

Status: complete. Phase 0 established the live scene/HUD targets and the
installed Synty assets to reuse. No gameplay code, prefab, or scene object was
changed.

## Active scene and editor checkpoint

- Unity instance: `rainbowsEnd@8545bd2c`.
- Unity version: `6000.0.67f1`.
- Active scene: `Assets/_Project/Scenes/MainArena_3D.unity`.
- Editor state: idle, not playing, not compiling, ready for tools.
- Canvas reference resolution: `1920 x 1080`; `CameraRoot` aspect is `16:9`.
- Evidence capture: `Assets/Screenshots/screenshot-20260826-215123.png`.
  The current non-playing Game view does not show a populated run-status HUD.

## Live scene targets

| Responsibility | Scene path | Instance | Current state |
| --- | --- | ---: | --- |
| HUD canvas | `UI` | `4378790` | Active uGUI `Canvas`, `CanvasScaler`, `GraphicRaycaster` |
| HUD parent | `UI/HudRoot` | `4378806` | Active `RectTransform` |
| Legacy gold presenter | `UI/HudRoot/GoldCounterPlaceholder` | `4398918` | Inactive; `EconomyHudPresenter` + `TextMeshProUGUI` |
| Other existing presenter | `UI/HudRoot/Player_Level` | `4417084` | Active; also has `EconomyHudPresenter` and three visual children |
| Shop panel | `UI/HudRoot/ShopPanel` | `4385330` | Existing panel; keep clear of the new group |
| Outcome panel | `UI/HudRoot/RunOutcomePanel` | `4387076` | Existing panel; keep clear of the new group |
| Shop controller | `GameplayRoot/ShopSystems/FaeTreeShop` | `4391174` | Existing `ShopController`; tree remains a spawn anchor, not a currency deposit |
| Run owner | `GameplayRoot/RunSystems/RunDirector` | `4410350` | Existing `RunDirector`; owns run lifecycle/outcome |
| Pot currency owner | `GameplayRoot/PotRegistry` | `4378716` | Existing `PotRegistry`; live pot list is currently empty while not playing |
| Player | `GameplayRoot/Player` | `4417832` | Existing `PlayerRoot` |
| Player weapon inventory | component on `GameplayRoot/Player` | `-3994828` | Default Fae Bolt active; temporary shots are `0` for the default weapon |

The current gold presenter still references `EconomyState` instance `4392726`.
Phase 1 will move the displayed/spendable gold path to `PotRegistry` and will
not mirror pot gold back into `EconomyState`.

## Runtime/pooling sources confirmed

The three enemy controller types are pooled and therefore have no active
controller instances in the edit-mode scene search. Their project-local prefab
sources are:

- `Assets/_Project/Prefabs/PF_Traveller.prefab`
- `Assets/_Project/Prefabs/PF_Police.prefab`
- `Assets/_Project/Prefabs/PF_Military.prefab`

The active scene contains `TravellerPool`, `PolicePool`, and `MilitaryPool`
objects, plus `EnemySpawner`, `PoliceSpawner`, and `MilitarySpawner`. Runtime
statistics must attach to the existing `RunDirector` and pooled controller
death/escape paths, not generic pool despawn.

## Synty assets selected for reuse

Package assets are references only; none are to be edited.

- Generic row frame: `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/ActionBar/_Parts/HUD_FantasyWarrior_Container_01.prefab`.
- Gold icon: `Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/Icons_Resources/ICON_SM_Item_Ingot_Gold_01.png`.
- Timer frame: `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/Timed/HUD_FantasyWarrior_GameTime_01.prefab`.
- Timer icon candidates: `Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/Icons_Status/ICON_FantasyWarrior_Status_Time_01_Clean.png`, plus the matching `Underlay` and `Stroke` layers when needed.
- Ammo/bullet UI part: `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/Player_Health_Equipment/_Parts/HUD_FantasyWarrior_ToggleBullet_01.prefab`.
- Kill icon: `Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/HUD/SPR_HUD_FantasyWarrior_Symbol_Skull_01.png`.

The installed kill event-log prefab was inspected but is an animated event-log
item, so it is not the static counter row. No clearly readable escape-specific
icon was found; the escape row will use the same Synty container with the
explicit `Escapes` TextMeshPro label.

## Agreed HUD layout checkpoint

Create one project-local `RunStatusHud` group under `UI/HudRoot`, anchored in
the upper-left at the existing gold-counter location. Keep the group compact
and vertically ordered:

1. `Pot Gold`
2. `Ammo`
3. `Time`
4. `Kills`
5. `Escapes`

Use the existing gold placeholder bounds as the first-row reference, place
subsequent rows below it, and verify against the active `Player_Level` bounds
before saving. The group must not cover player health/level UI, the crosshair,
Q shop/pause controls, or the outcome panel. All five rows remain visible and
use the approved labels and formats from the main plan.

## Verification and rollback

- Verified active scene/editor state through Unity MCP.
- Verified live HUD, shop, run, pot, player, and pooling references through
  scene hierarchy/component inspection.
- Verified exact Synty asset paths and prefab component contents.
- No runtime code, prefab, or scene serialization changed.
- Generated screenshot and existing generated files remain uncommitted.
- Rollback is limited to removing this recap/checkpoint note; there is no
  gameplay rollback required.

Next phase: make live Pot Gold the only spendable/displayed gold path.
