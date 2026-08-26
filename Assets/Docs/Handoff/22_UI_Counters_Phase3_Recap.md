# UI Counters Feedback - Phase 3 Recap

Status: Complete. Phase 4 remains pending.

## Delivered

- Created one `RunStatusHud` group under `UI/HudRoot` in the active
  `MainArena_3D` scene.
- Reused the existing `GoldCounterPlaceholder` TMP label as the only gold
  display, moved it under `PotGoldRow`, and set it to `Pot Gold: {wholeGold}`.
  The old active `Player_Level` gold widget is disabled, so the HUD has no
  duplicate gold counter.
- Added Ammo, Time, Kills, and Escapes rows. Ammo defaults to `Ammo: ∞` and
  uses the installed temporary-shot state when available. Time is `MM:SS`.
  Kills and Escapes read the existing per-run `RunDirector` state.
- Added `RunStatusHudPresenter` with cached scene references, change-only label
  writes, and no mutable gameplay state.
- Disabled the Synty sample clock component so the timer is controlled by
  `RunDirector.ElapsedRunSeconds`.
- Set all HUD decoration/text graphics to non-raycast targets; the HUD does not
  intercept gameplay or modal UI input.

## Files changed

- `Assets/_Project/Runtime/UI/RunStatusHudPresenter.cs`
- `Assets/_Project/Tests/EditMode/UI/RunStatusHudPresenterTests.cs`
- `Assets/_Project/Scenes/MainArena_3D.unity`
- `Assets/Docs/Handoff/18_UI_Counters_Feedback_Plan.md`

## Scene layout

- Group: `UI/HudRoot/RunStatusHud`, anchored upper-left, size `320 x 400`.
- Rows: `PotGoldRow`, `AmmoRow`, `TimeRow`, `KillsRow`, `EscapesRow`.
- Row size: `280 x 64`; anchored positions from top: `-24`, `-96`, `-168`,
  `-240`, and `-312`.
- The time row uses `HUD_FantasyWarrior_GameTime_01`; its
  `Label_GameTime` TMP is bound to the run-status presenter.

## Synty assets reused

- `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/ActionBar/_Parts/HUD_FantasyWarrior_Container_01.prefab`
  for Pot Gold, Ammo, Kills, and Escapes.
- `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/Timed/HUD_FantasyWarrior_GameTime_01.prefab`
  for the timer frame.
- `Assets/Synty/InterfaceFantasyWarriorHUD/Prefabs/Player_Health_Equipment/_Parts/HUD_FantasyWarrior_ToggleBullet_01.prefab`
  for the ammo marker.
- `Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/Icons_Resources/ICON_SM_Item_Ingot_Gold_01.png`
  for Pot Gold.
- `Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/Icons_Status/ICON_FantasyWarrior_Status_Time_01_Clean.png`
  was used during the generic timer-row pass; the final Synty game-time frame
  supplies the visible timer artwork.
- `Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/HUD/SPR_HUD_FantasyWarrior_Symbol_Skull_01.png`
  for Kills.
- `Assets/Synty/InterfaceFantasyWarriorHUD/Fonts/Grenze/Grenze-SemiBold SDF.asset`
  for new TMP labels.

## Verification

- Unity MCP reports the active scene as `Assets/_Project/Scenes/MainArena_3D.unity`,
  idle, not compiling, and ready for tools.
- `RunStatusHudPresenterTests`: 6/6 passed, including timer boundaries at
  `00:00`, `00:59`, `01:00`, and a multi-minute value plus default/temporary
  ammo formatting.
- Play-mode screenshot verified all five rows are visible in the upper-left:
  `Assets/Screenshots/screenshot-20260827-013446.png`.
- After clearing the console and completing the final refresh/play/stop check,
  Unity reported 0 error entries.

## Deferred to Phase 4

- Temporary-weapon pickup/fire-down-to-zero behavior in a full playthrough.
- Pot Gold/shop/pot-label agreement while harvesting, spending, draining, and
  collecting pickups.
- Q pause/resume, timer boundary in live play, outcome freeze, exact hostile
  kill counts, and successful theft escape counts.
- Final visual tuning against health, crosshair, shop, and outcome overlays at
  the target gameplay resolution.

## Next step

Phase 4: run the cross-system regression checklist and edge-case playthrough.