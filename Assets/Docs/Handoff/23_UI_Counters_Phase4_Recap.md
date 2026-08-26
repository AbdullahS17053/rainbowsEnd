# Rainbow's End - UI Counters Phase 4 Recap

Status: Complete.

## Step completed

Phase 4 regression verification for the UI counters feature is complete. The
active scene remains Assets/_Project/Scenes/MainArena_3D.unity.

## Approved decisions

- Pot Gold remains the single displayed and spendable run currency.
- Ammo uses the infinity symbol for the default weapon and a remaining-shot value for temporary weapons.
- Time is displayed as scaled elapsed MM:SS.
- Kills are combined across hostile types.
- Escapes count only successful positive-gold theft escapes.
- The five values remain in one compact Synty-styled HUD group.

## Files and serialized assets checked

- Assets/_Project/Runtime/UI/RunStatusHudPresenter.cs
- Assets/_Project/Tests/EditMode/UI/RunStatusHudPresenterTests.cs
- Assets/_Project/Scenes/MainArena_3D.unity
- Assets/Docs/Handoff/18_UI_Counters_Feedback_Plan.md
- Assets/Docs/Handoff/22_UI_Counters_Phase3_Recap.md

The scene hierarchy check found five active rows under
UI/HudRoot/RunStatusHud: PotGoldRow, AmmoRow, TimeRow, KillsRow,
and EscapesRow. No missing-reference errors appeared in the console.

## Automated verification

- Full EditMode suite: 91 tests completed.
- New RunStatusHudPresenterTests: 6/6 passed.
- Focused pot/economy/shop/player/run/enemy/HUD set: 45 tests completed; all
  feature-relevant tests passed.
- The full and focused runs reported only the four pre-existing baseline
  failures:
  - WeaponHitUtilityTests.TryResolveHit_IgnoresFriendlyTargets
  - TravellerTargetSelectionUtilityTests.SelectBestCandidateIndex_FallsBackToNearestPotWhenNoPotIsHarvesting
  - RunWaveControllerTests.Tick_DoesNotSpawn_WhenNoPotHasGoldOrHarvesting
  - PortalRegistryTests.TryPlacePortal_PairsTwoPortalsAndReplacesOldestOnThirdPlacement
- Unity's Performance Testing setup/result-save log entries were informational,
  not additional failures.

## Manual verification

- Play-mode screenshots showed all five labels in the upper-left gameplay HUD:
  Pot Gold: 0, Ammo: infinity symbol, Time: 00:05, Kills: 0, and Escapes: 0.
- The timer advanced during live play (00:05 to 00:21).
- Editor pause held the displayed timer at 00:26 across repeated screenshots;
  resume returned the run to active play mode.
- The HUD remained readable over gameplay and did not cover the central play area.
- Evidence screenshots: Assets/Screenshots/screenshot-20260827-014837.png,
  Assets/Screenshots/screenshot-20260827-014900.png, and
  Assets/Screenshots/screenshot-20260827-014951.png.

## Console state

After stopping play mode, Unity reported no errors or warnings caused by this
feature. The remaining entries were the known Unity Performance Testing
setup/result-save messages.

## Deferred work

The current MCP session did not provide keyboard/game-view input injection, so
the exact Q key path was not directly driven in this pass. The following
end-to-end interactions also remain explicit manual follow-up rather than
being claimed as completed here: temporary-weapon firing to zero, multi-pot
currency spending and military pickup deposit, one kill per hostile type,
positive-gold versus zero-gold traveller exits, and win/loss outcome freezing.
The available formatter, inventory, pot, economy, shop, run-stat, wave, and
enemy tests remain green except for the listed baseline failures.

## Next step

Refresh Graphify and create the Phase 5 handoff/checkpoint after the final
source and documentation changes are committed.