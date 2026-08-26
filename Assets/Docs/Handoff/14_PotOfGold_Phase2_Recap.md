# Pot of Gold Feedback — Phase 2 Recap

## Change completed

Updated `Assets/_Project/Runtime/Gameplay/Pots/PotController.cs` to use authored effect references instead of constructing the harvest flare at runtime.

### Serialized references added

- `potGlow`: root GameObject for the persistent stored-gold aura.
- `harvestFlair`: root GameObject for the looping rainbow harvest effect.
- `unicornImpactBurstPrefab`: one-shot impact prefab reference.

The fields are intentionally unassigned until Phase 4 prefab authoring, so this phase does not alter the active prefab or scene.

### State behavior added

- Pot Glow is active only when the pot has stored gold and is not destroyed, carried, or being taken by the military.
- Harvest Flair is active only while the pot status is `Harvesting` and the pot is not destroyed, carried, or being taken by the military.
- Both effects stop through the existing stop, pickup, disable, and destruction paths.
- The visual-state method still controls the existing pot color and drain feedback.

### Legacy behavior removed

- Removed the runtime `GameObject`/`SpriteRenderer` rainbow segment construction.
- Removed the DOTween color animation and its private flare transform.
- No looping effect is created or allocated from `Update()`.

### Unicorn destruction feedback

`DestroyByUnicorn` now instantiates the referenced impact prefab at the pot position exactly once, guarded by the existing `IsDestroyed` check, before applying the existing lethal damage. No gameplay damage, reward, stun, or physics behavior was added.

## Verification

- Unity compiled successfully after the edit; console had 0 errors.
- Script validation: 0 errors and 2 generic advisory warnings about existing `Update()` patterns.
- All four existing `PotHarvestStateTests` passed.
- Legacy flare symbols/imports are absent from `PotController.cs`.
- Active scene remained `MainArena_3D` and was not modified.

## Deferred to Phase 4

Assign the selected particle assets to the active `PF_Pot` prefab, set their initial inactive state, tune scale/offset/sorting for the gameplay camera, and verify the one-shot explosion cleanup visually. Manual FX readability checks cannot pass until those references are authored.
