# Pot of Gold Feedback — Phase 0 Recap

## Scope completed

Phase 0 covered discovery, dependency tracing, target identification, reusable asset selection, and a no-change validation pass. No gameplay code, balance data, prefab, or scene content was changed.

## Verified implementation targets

- Active scene: `Assets/_Project/Scenes/MainArena_3D.unity`, build index 1. It is loaded and was not dirty after discovery.
- Active pot instance: `GameplayRoot/Pot_01`.
- Pot source prefab: `Assets/_Project/Prefabs/PF_Pot.prefab`.
- Pot authority: `PotController`, with `PotHarvestState` and `PotTrailCaptureSensor` handling harvest state and trail capture.
- Carry/drop flow: `PlayerPotCarryController` calls the pot drop path directly; Phase 3 will validate the destination before clearing the carried state.
- Unicorn destruction flow: `UnicornController` calls `PotController.DestroyByUnicorn` through both trigger and collision paths.

## Balance baseline

`SO_Pot_Default` currently uses:

- `BaseHarvestGoldPerSecond = 1`
- `TrailAgeGoldMultiplier = 0.35`
- `AutoBankGoldPerSecond = 0.5`

Phase 1 changes only the base harvest rate to `2`; trail-age scaling, auto-banking, enemy drain, and storage limits remain unchanged.

## Hard-barrier baseline

`EnvironmentRoot/Cover` contains five `CoverBlocker` objects. The inspected barriers use `BlocksMovement = true` and a non-trigger `BoxCollider2D`. Phase 3 will treat a `CoverBlocker` with `BlocksMovement == true` as an invalid pot destination. Non-blocking scenery and grass remain valid destinations.

## Reusable FX candidates

These package assets were inspected structurally and selected by role. Package files will remain untouched; Phase 4 will author only the minimum project-local references/variants needed by the active pot prefab.

- Pot Glow: `Assets/Epic Toon FX/Prefabs/Combat/Magic/Aura/MagicAuraYellow.prefab`
  - Particle-only prefab; matches the approved persistent gold/yellow stored-gold cue.
- Harvest Flair: `Assets/PolygonParticleFX/Prefabs/FX_RainbowGlow_02.prefab`
  - Particle-only rainbow prefab; matches the approved continuous upward flair while harvesting.
- Unicorn Impact Burst: `Assets/Epic Toon FX/Prefabs/Combat/Explosions/MagicExplosion/MagicExplosionPink.prefab`
  - Compact pink/purple magical explosion candidate. Its root includes an `AudioSource`; if used, Phase 4 will make a project-local variant with audio disabled because no new audio behavior was requested.
- Alternate flair candidate: `Assets/Epic Toon FX/Prefabs/Combat/Magic/PillarBlast/MagicPillarBlastYellow.prefab`
  - Use only if the rainbow candidate does not read clearly as an upward skyward flair in the gameplay camera.

The prefab-stage particle preview was not visually useful in edit mode, so final scale, offset, sorting, loop/stop behavior, and gameplay-camera readability remain Phase 4 tuning items.

## Validation

- Active scene validation: clean, 0 issues.
- Missing scripts: 0.
- Broken prefabs: 0.
- Unity console: 0 errors and 0 warnings.
- Graphify query traced the pot harvest, carry/drop, unicorn-destruction, and cover-barrier paths. The graph was queried, not rebuilt, because no source or scene graph content changed in Phase 0.

## Checkpoint boundary

The approved Phase 0 checkpoint contains only:

- `Assets/Docs/Handoff/11_PotOfGold_Feedback_Plan.md`
- `Assets/Docs/Handoff/12_PotOfGold_Phase0_Recap.md`
- `CONTEXT.md`

Phase 1 can begin from this clean documentation checkpoint. No implementation work should be inferred from the asset candidates until the relevant later phase is started.
