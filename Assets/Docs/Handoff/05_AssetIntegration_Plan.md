# Rainbow's End Asset Integration Plan

Companion to ADR-0001-3D-Asset-Integration.md. Execute phases in order. Each
phase has a single goal, a list of editor steps (driven by the user), any code
steps (driven by the implementing model), and a verification checklist. Do not
advance to the next phase until the verification checklist passes.

Read ADR-0001 first for the locked decisions and rationale. This document
expands each decision into observable, runnable steps.

## Conventions used in this plan

- "Prefab mode" means opening a `.prefab` asset in the Inspector (Project
  window > right-click > Open Prefab, or double-click). Changes here propagate
  to every instance in every scene.
- "Child of VisualRoot" means: open the prefab, find the GameObject referenced
  by the parent MonoBehaviour's `visualRoot` serialized field (typically a
  child named `Visual` or `SpriteVisual`), and add the new 3D GameObject as a
  child of that root.
- Local rotation `(90, 0, 0)` means Euler angles X=90, Y=0, Z=0. POLYGON
  characters are authored Y-up; rotating +90 on X lays them flat with their
  head pointing toward +Y in our XY-plane world, readable from straight above.
  Signs may need to be inverted to (270, 0, 0) or (-90, 0, 0) depending on the
  prefab; verify visually in the Scene view before pinning the value.
- All gameplay code (Rigidbody2D, Physics2D, BoxCollider2D) is authoritative.
  3D models must NOT have a Collider, Collider2D, Rigidbody, or Rigidbody2D
  of their own. The 3D mesh is a visual sibling/child only.
- After any prefab edit, click Overrides > Apply in the Inspector to persist.

## Phase A — Render Pipeline and Lighting Foundation

### Goal
URP renderer asset exists and is assigned in Graphics Settings. POLYGON
materials converted from Standard to URP shaders. A Directional Light exists
in MainArena_3D. No gameplay changes.

### Editor steps

1. Create URP pipeline asset:
   - Project window > `Assets/_Project/Settings/` (create folder if missing).
   - Right-click > Create > Rendering > URP Asset (Universal Render Pipeline
     Asset). Name it `URP_Asset`.
   - Right-click > Create > Rendering > URP Renderer (Universal Renderer
     Data). Name it `URP_Renderer`.
   - In `URP_Asset`, the Renderer List should reference `URP_Renderer`.

2. Assign the pipeline asset:
   - Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings
     → assign `URP_Asset`.
   - Edit > Project Settings > Quality > Quality Levels → for each level, set
     the Render Pipeline Asset to `URP_Asset` (or remove extra quality levels
     and keep one).
   - Verify: the Game view in any scene is no longer pink/magenta — that
     means URP is active.

3. Convert POLYGON materials to URP:
   - Window > Rendering > Render Pipeline Converter.
   - Converter: "Material Upgrade" (Standard to URP).
   - Select all materials under `Assets/Synty/`, `Assets/PolygonFantasyKingdom/`,
     `Assets/PolygonParticleFX/`. (Hold Shift or use the selection filter.)
   - Click "Convert Selected Assets".
   - Verify: open `Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Scene/
     Demo.unity`. Materials render shaded, not pink. Console has no
     "Material has no shader for this pipeline" warnings.

4. Add a Directional Light to MainArena_3D:
   - Open `Assets/_Project/Scenes/MainArena_3D.unity` if it already exists
     (it will not exist yet — this step is performed after Phase B creates
     the scene; defer this part of Phase A to the end, or create a quick
     scratch light scene now and reapply in Phase B).
   - GameObject > Light > Directional Light.
   - Inspector: Rotation X=45, Y=-30, Z=0. Intensity = 1.0. Type = Directional.
   - Shadow Type = No Shadows (acceptable for MVP perf) or Soft Shadows with
     a low distance.
   - Add mild ambient: Window > Rendering > Lighting > Environment tab. Set
     Environment Lighting to Color (not Gradient), pick a soft sky-tone like
     #B0C4DE at intensity ~0.6.

### Code steps
None.

### Verification checklist
- [ ] URP_Asset assigned in Project Settings > Graphics
- [ ] POLYGON demo scene opens with no pink/magenta materials
- [ ] Console has zero "shader not supported" warnings
- [ ] Directional Light added to a scene containing a POLYGON prop; prop is
      shaded (not pure black, not full-bright white)

### Rollback
Remove URP_Asset from Graphics Settings. Standard materials will render under
Built-In Render Pipeline again.

---

## Phase B — MainArena_3D Scene Bootstrap

### Goal
Create `Assets/_Project/Scenes/MainArena_3D.unity` from the Meadow Forest
demo. Strip demo extras. Add the project's composition roots
(ArenaSceneContext, gameplay roots, camera). Update build settings so Boot
loads MainArena_3D.

### Editor steps

1. Duplicate the demo scene:
   - In Project window, select `Assets/Synty/PolygonNatureBiomes/
     PNB_Meadow_Forest/Scene/Demo.unity`.
   - Ctrl+D to duplicate. Drag the duplicate into `Assets/_Project/Scenes/`.
   - Rename the duplicate to `MainArena_3D.unity`.

2. Open MainArena_3D. Strip demo-only objects:
   - Delete: demo showcase canvas, demo post-FX volume, demo cameras, demo
     reflection probes, demo lighting rig scripts that aren't standard Unity
     lights. Keep ground, trees, and any ambient environment you want visible.
   - Keep the demo's Directional Light if it exists (you may delete the one
     from Phase A scratch and use this one — tune its rotation/intensity).
   - Aim to leave the scene with: ground plane / terrain, ~15-30 environment
     props (trees, rocks) forming a clearing, and the one Directional Light.

3. Create empty root GameObjects for composition:
   - In Hierarchy, create empty GameObjects named exactly:
     - `WorldRoot`
     - `EnvironmentRoot`
     - `GameplayRoot`
     - `SpawnPointsRoot`
     - `EffectsRoot`
     - `UIRoot`
     - `CameraRoot`
   - Parent all remaining demo environment props under `EnvironmentRoot` (so
     we can toggle visibility cleanly later). Parent ground/terrain under
     `EnvironmentRoot` as well.

4. Camera setup:
   - `CameraRoot` GameObject: add a `Camera` component.
   - Camera: Orthographic = true. Orthographic Size = 7. Clear Flags = Solid
     Color. Background = (0.11, 0.13, 0.15, 1).
   - Position the CameraRoot above the arena clearing at roughly the center
     of where you want gameplay to happen. The XY position of the camera
     defines the on-screen arena center. Set Camera Z = -10 (so it looks
     down +Z toward the XY gameplay plane).
   - The camera should look straight down: rotation X=90, Y=0, Z=0 (looking
     +Z). Verify in Scene view.

5. ArenaSceneContext GameObject:
   - Create empty GameObject named `ArenaSceneContext`.
   - Add component `ArenaSceneContext` (the RainbowsEnd.Core.SceneManagement
     type — the project already defines it).
   - In the Inspector, assign:
     - WorldRoot → WorldRoot
     - EnvironmentRoot → EnvironmentRoot
     - GameplayRoot → GameplayRoot
     - SpawnPointsRoot → SpawnPointsRoot
     - EffectsRoot → EffectsRoot
     - UIRoot → UIRoot
     - MainCamera → the camera on CameraRoot
   - ArenaBounds and FaeTreeAnchor will be added in Phase D (after the
     bounds authoring object and fae tree are placed). Leave them null for
     now — IsConfigured will report false. Acceptable for Phase B.

6. Update build settings:
   - File > Build Profiles (Unity 6) or Build Settings (older 6000 names).
   - Add `Boot.unity` at index 0.
   - Add `MainArena_3D.unity` at index 1.
   - Remove `MainArena.unity` from build (or keep it disabled in the list as
     a fallback — recommended).

7. Update scene name constant or GameBootstrap target (code step, see below).

### Code steps
Update `Assets/_Project/Runtime/Core/SceneManagement/SceneNames.cs` so the
MainArena constant matches the new scene name. The existing GameBootstrap uses
this constant to load the gameplay scene from Boot. Two options:

Option B1 (rename safe — recommended): Update the `MainArena` constant value
to `"MainArena_3D"`. The constant name stays `MainArena` (callers don't
change), only its string value changes.

```csharp
// In SceneNames.cs
public const string MainArena = "MainArena_3D";
```

Option B2: Add a new `MainArena3D` constant and update GameBootstrap to load
it. More churn, not recommended.

### Verification checklist
- [ ] MainArena_3D.unity exists at Assets/_Project/Scenes/
- [ ] Scene contains WorldRoot, EnvironmentRoot, GameplayRoot, SpawnPointsRoot,
      EffectsRoot, UIRoot, CameraRoot, ArenaSceneContext GameObjects
- [ ] Camera is Orthographic, Size 7, looking straight down
- [ ] Build settings include Boot at index 0 and MainArena_3D at index 1
- [ ] SceneNames.MainArena equals "MainArena_3D"
- [ ] Enter Play mode from Boot — ProjectContext boots, MainArena_3D loads,
      no errors in console. (Gameplay won't be present yet — that's Phase C.)
- [ ] Console: no "ArenaSceneContext is missing scene references" warning
      after Phase C wires ArenaBounds and FaeTree, OR a tolerable warning
      during Phase B only.

### Rollback
Restore SceneNames.MainArena to "MainArena". Set build settings to use
MainArena.unity instead of MainArena_3D.unity. Delete MainArena_3D.unity.

---

## Phase C — Gameplay Object Migration

### Goal
All gameplay GameObjects from MainArena.unity are copies into MainArena_3D.unity,
under GameplayRoot, with ArenaSceneContext references resolving to the new
scene's instances. Full gameplay loop runs in the new scene with the old 2D
placeholder sprites still in place. This phase proves the scene migration
didn't break anything before we start swapping visuals in later phases.

### Editor steps

1. Open both MainArena.unity and MainArena_3D.unity. Use multi-scene editing:
   - Open MainArena.unity first.
   - File > Add Scene to open MainArena_3D.unity alongside it (H.instance should
     indicate multiple roots).
   - Or: open MainArena.unity alone, then use Copy on GameObjects, switch to
     MainArena_3D.unity, Paste. Either workflow works; multi-scene is faster.

2. Copy each gameplay GameObject from MainArena into MainArena_3D's GameplayRoot.
   The canonical list (search MainArena hierarchy for these): 
   - Player instance (typically named `Player` or `PF_Player`-rooted GameObject)
   - Unicorn instance (typically `Unicorn` or `PF_Unicorn`)
   - Pot instances (×3 — search for `Pot` GameObjects under GameplayRoot)
   - EnemySpawner (Traveller spawner)
   - PoliceSpawner
   - MilitarySpawner
   - RunDirector
   - PoolRegistry (or the per-scene pools root, typically `Pools` containing
     RainbowTrailPool, ArrowProjectilePool, ImpactEffectPool, GoldPickupPool,
     PoliceProjectilePool, etc.)
   - EventSystem
   - All UI Canvas GameObjects under UIRoot (HUD, ShopPanel,
     RunOutcomePanel, prompt text, etc.)
   - SpawnPoints markers under SpawnPointsRoot (search for
     SpawnPointMarker components)
   - CoverBlocker and ConcealmentZone GameObjects under EnvironmentRoot (these
     are gameplay blockers/concealers — they go with EnvironmentRoot, not
     GameplayRoot, since they shape the world)

   For each GameObject: copy (Ctrl+C in MainArena), select GameplayRoot in
   MainArena_3D (or EnvironmentRoot / SpawnPointsRoot / UIRoot as appropriate
   for the object category), paste (Ctrl+V).

3. Re-resolve ArenaSceneContext references:
   - Select each copied GameObject that has serialized references to scene
     objects (like ArenaSceneContext, ArenaBoundsAuthoring, FaeTreeAnchor).
     Reassign those references to the new scene's instances.
   - Specific known references to check:
     - PlayerRoot's PlayerAimController.worldCamera → new scene's CameraRoot
       camera (or set to null and let it fall back to ArenaSceneContext.MainCamera)
     - UnicornController's ArenaBoundsAuthoring reference (if any) → new
     - RainbowTrailEmitter.ResolvePool expects EffectsRoot.Find("RainbowTrailPool")
       — verify the pool was copied and named exactly "RainbowTrailPool"

4. Save the scene.

### Code steps
None. This is a pure editor migration.

### Verification checklist
- [ ] Play from Boot → MainArena_3D loads
- [ ] Player moves with WASD, aims with mouse, fires arrows (2D sprite visual)
- [ ] Unicorn roams leaving a 2D rainbow trail
- [ ] Pots (3) visible as 2D sprites, harvest when overlapping trail, gold counts up in HUD
- [ ] Waves progress: travellers spawn, drain pots, flee carrying gold
- [ ] Police spawn, ranged-attack player, drop weapons, player picks up
- [ ] Military spawn, steal pots, return to combat
- [ ] Shop opens on Interact, upgrades apply
- [ ] Portal spell places portals, teleports player/unicorn/pots
- [ ] Run outcome: win on final wave clear, lose on all pots destroyed, restart works
- [ ] Console has zero errors during a 2-3 minute playthrough

### Rollback
Delete the copied GameplayRoot contents from MainArena_3D and use MainArena
again. Nothing in this phase touches the existing 2D scene.

---

## Phase D — Arena Environment Dressing

### Goal
Forest environment props arranged inside the arena. ArenaBoundsAuthoring size
and position match the visible clearing. FaeTreeAnchor GameObject placed at a
fruit tree in the scene.

### Editor steps

1. Locate or create the ArenaBoundsAuthoring GameObject:
   - In MainArena_3D, create empty GameObject named `ArenaBounds` under
     GameplayRoot.
   - Add component `ArenaBoundsAuthoring`.
   - Configure the Size field to match the clearing in your dressed
     environment (e.g. Width=14, Height=10 if your visible play area is that
     size — orthographicSize=7 shows about 14 units tall, so adjust camera
     or bounds to match).
   - Center the ArenaBounds GameObject at the center of the clearing.
   - Verify the camera (CameraRoot) is centered above the same X,Y point.

2. Place environment props inside the arena:
   - Pull props from `Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/
     Prefabs/` into EnvironmentRoot.
   - Suggested count: ~12-20 trees on the perimeter, ~5-10 rocks/bushes
     scattered. These are visual; they don't block movement.
   - Keep a clear ground area inside the ArenaBounds rectangle — gameplay
     needs open space for player, unicorn, pots, waves.

3. Place CoverBlocker and ConcealmentZone gameplay objects inside the arena:
   - If CoverBlocker / ConcealmentZone objects were copied in Phase C, move
     them to visually-sensible positions (next to rocks, behind bushes).
     These are the actual gameplay-blocking colliders.
   - If not yet present, create empty GameObjects with CoverBlocker /
     ConcealmentZone components placed at cover visuals.

4. Create / place the FaeTreeAnchor:
   - Drag `Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/Props/
     SM_Env_Tree_Fruit_01.prefab` into the scene at a chosen anchor position
     (typically one corner of the arena).
   - Add a child GameObject named `FaeTreeAnchor` with the
     `FaeTreeAnchor` component. Or place the FaeTreeAnchor component on the
     tree GameObject directly (less clean but works since it's just a marker).
   - Wire ArenaSceneContext.FaeTreeAnchor → this GameObject.
   - Wire ArenaSceneContext.ArenaBounds → the ArenaBounds GameObject.

5. Verify ArenaSceneContext.IsConfigured no longer warns. All required
   fields should be assigned.

### Code steps
None.

### Verification checklist
- [ ] ArenaBounds GameObject exists under GameplayRoot, ArenaBoundsAuthoring
      Size covers the visible clearing
- [ ] ~12-20 environment trees placed on perimeter of EnvironmentRoot
- [ ] FaeTreeAnchor GameObject exists with FaeTreeAnchor component
- [ ] ArenaSceneContext has ArenaBounds and FaeTreeAnchor references assigned
- [ ] ArenaSceneContext.IsConfigured returns true (no Awake warning in console)
- [ ] Play — player still clamps to arena bounds, no clipping through props
      (because CoverBlocker handles gameplay blocking, decorative trees have
      no colliders and the player walks through their visual mesh — verify
      this reads acceptably visually)

### Rollback
Remove the placed props. ArenaBoundsAuthoring retains its prior size.

---

## Phase E — Player 3D Swap

### Goal
Player GameObject renders the Fairy mesh from PolygonFantasyKingdom. All
gameplay still works. Health-stage visual feedback stays invisible this phase
(acceptable; deferred to polish).

### Editor steps

1. Open `Assets/_Project/Prefabs/PF_Player.prefab` in prefab mode. (If PF_Player
   lives at a different path, search Project window for "PF_Player".)

2. Locate the SpriteRenderer child of VisualRoot:
   - In the prefab hierarchy, find the child referenced by PlayerRoot's
     `visualRenderer` serialized field. Typically named `Visual` or
     `SpriteVisual`.
   - This GameObject has the SpriteRenderer. Delete its SpriteRenderer
     component (or disable the SpriteRenderer component — disabling is safer
     for rollback: Component > uncheck "Sprite Renderer" in inspector).

3. Add the fairy mesh as a child:
   - Drag `Assets/PolygonFantasyKingdom/Prefabs/Characters/SM_Chr_Fairy_01.prefab`
     as a child of VisualRoot (or as a sibling of the disabled sprite child).
   - Name it `FairyVisual`.
   - Set its local Position = (0, 0, 0).
   - Set its local Rotation = (90, 0, 0) or (-90, 0, 0) — whichever makes
     the fairy stand upright in the Scene view. Verify in Scene view
     (when X rotation is 90, the fairy's Y-up body should now extend along
     +Y in our XY plane).
   - Set its local Scale = (1, 1, 1) initially. If the fairy is much
     larger/smaller than the previous sprite, tune scale to roughly
     0.5-1.5. The previous sprite was a SpriteRenderer quad about 1 unit
     in size; match apparent diameter.

4. Leave the fairy's bundled Animator at idle:
   - The fairy prefab includes its own Animator + controller. Leave the
     controller as-is. No parameter wiring.
   - The fairy will remain in its idle pose while moving. Confirmed
     acceptable by ADR-0001 (animation polish is a later phase).

5. Click Overrides > Apply to save the prefab.

6. Verify in MainArena_3D: select the Player GameObject. Confirm
   FairyVisual is a child, the SpriteRenderer is disabled, and the
   2D BoxCollider2D on the Player root remains intact (do NOT add any 3D
   colliders to the fairy).

### Code steps
None. PlayerRoot.VisualRenderer color feedback (for health stages, knockback
tint) will be visually inactive — confirmed by ADR-0001 as acceptable for
this phase. No defensive code needed.

### Verification checklist
- [ ] PF_Player prefab has FairyVisual child under VisualRoot
- [ ] SpriteRenderer on the old visual child is disabled
- [ ] Fairy local rotation makes it upright in Scene view
- [ ] No 3D Collider on the fairy; only BoxCollider2D on the Player root
- [ ] Play — player moves with WASD/controller
- [ ] Player aims with mouse and via controller
- [ ] Player fires arrows (2D sprite arrow — Phase I swaps it)
- [ ] Player takes damage; HP HUD still drives
- [ ] Player can pick up weapons (pickup interaction unaffected)
- [ ] Player can carry/interact with pots
- [ ] Player can interact with shop
- [ ] Player can cast portals
- [ ] Fairy model visible on screen, follows player movement

### Rollback
Re-enable the disabled SpriteRenderer on the visual child. Delete the
FairyVisual GameObject. Apply prefab overrides.

---

## Phase F — Enemy 3D Swaps

### Goal
PF_Traveller, PF_Police, PF_Military prefabs each render their respective
POLYGON 3D character. Enemy AI unchanged. Spawners still instantiate these
prefabs as before.

### Editor steps

1. Open `Assets/_Project/Prefabs/PF_Traveller.prefab` (search Project if the
   path differs) in prefab mode.

2. Disable the SpriteRenderer on the visual child:
   - Locate the child referenced by TravellerController's `visualRenderer`
     (or whatever the controller accesses — check the controller's
     serialized fields).
   - Uncheck the SpriteRenderer component.

3. Add civilian 3D mesh as child:
   - Drag `Assets/Synty/PolygonCity/Prefabs/Characters/Character_BusinessMan_Shirt.prefab`
     as child of VisualRoot, name it `CivilianVisual`.
   - Rotation (90, 0, 0) tuned for upright appearance.
   - Scale ~ (1, 1, 1).
   - Apply prefab overrides.

4. Repeat for PF_Police:
   - Disable the polyline sprite.
   - Drag `Assets/Synty/PolygonCity/Prefabs/Characters/Character_Male_Police.prefab`
     (or Female_Police), name `PoliceVisual`, rotation/scale as above.
   - Apply prefab overrides.

5. Repeat for PF_Military:
   - Disable the sprite.
   - Drag `Assets/Synty/PolygonMilitary/Prefabs/Characters/SM_Chr_Soldier_Male_01.prefab`
     (or variants Male_02, Female_01, Female_02 for variety), name
     `MilitaryVisual`, rotation/scale as above.
   - Apply prefab overrides.

6. Optional variety for travellers:
   - EnemySpawner instantiates PF_Traveller per wave. If you want traveller
     variety (rotating through civilian prefabs), see the Code steps below.
   - Otherwise, all travellers spawn as BusinessMan_Shirt. Acceptable for MVP.

7. Verify in scene: each enemy pool sample is now a 3D civilian / police /
   military character. The 2D BoxCollider2D on the prefab root remains intact.

### Code steps (optional)
If traveller variety is requested, add a `GameObject[] travellerVisualVariants`
serialized field to EnemySpawner, and on each spawn (after Rent), instantiate
a randomly-selected variant as a child of VisualRoot on the spawned instance.
Default to the single-prefab path (no code change) unless variety is requested.

Recommended: skip variety for MVP. One civilian look per enemy type is fine.

### Verification checklist
- [ ] PF_Traveller prefab has CivilianVisual child, SpriteRenderer disabled
- [ ] PF_Police prefab has PoliceVisual child, SpriteRenderer disabled
- [ ] PF_Military prefab has MilitaryVisual child, SpriteRenderer disabled
- [ ] No 3D colliders added to any enemy root or visual child
- [ ] Play — traveller wave spawns 3D civilians walking (with idle pose) toward pots
- [ ] Police wave spawns 3D police, ranged-attack player
- [ ] Military wave spawns 3D soldiers, steal pots, return to combat
- [ ] Enemy death drops gold and weapon pickups still function
- [ ] HealthStage feedback on enemies (sprite color) is visually inactive — known

### Rollback
Per prefab: re-enable the disabled SpriteRenderer, delete the 3D visual child.

---

## Phase G — Unicorn 3D Stand-in + Rainbow Particle Overlay

### Goal
Unicorn GameObject renders as a temporary 3D civilian (BusinessMan_Shirt). A
`FX_RainbowGlow_01` particle effect on the unicorn provides the visible
rainbow. RainbowTrailEmitter and RainbowTrailSegment pooling kept intact —
the segment 2D colliders still drive harvesting, but their SpriteRenderers
may be disabled so the only visible rainbow is the particle cosmetic.

### Editor steps

1. Open `Assets/_Project/Prefabs/PF_Unicorn.prefab` in prefab mode.

2. Disable the SpriteRenderer on the unicorn visual child:
   - Locate the child referenced by UnicornController's `visualRenderer`
     (or equivalent). Uncheck SpriteRenderer.

3. Add the civilian stand-in:
   - Drag `Character_BusinessMan_Shirt.prefab` as child of VisualRoot.
   - Name `UnicornStandIn`.
   - Rotation (90, 0, 0) for upright.

4. Add the rainbow particle effect:
   - Drag `Assets/PolygonParticleFX/Prefabs/FX_RainbowGlow_01.prefab` as
     a child of VisualRoot (or as a child of the prefab root, near
     `UnicornStandIn`). Name `RainbowCosmeticFX`.
   - Position (0, 0, 0). Rotation (90, 0, 0) so the particle system emits
     in our XY plane (ParticleSystems authored Y-up will need to be rotated
     to emit "into the camera" along Z).
   - Configure theParticleSystem:
     - Set Looping = true.
     - Set Play On Awake = true.
     - Set Emission Rate to something modest (e.g. 10/s).
     - Set Shape to a small sphere or cone centered on the unicorn.
     - Optionally set Start Color to a rainbow hue if the prefab doesn't
       already.

5. Disable the rainbow trail segment SpriteRenderer visuals:
   - Open `Assets/_Project/Prefabs/PF_RainbowTrailSegment.prefab` (search
     Project if path differs).
   - Disable the SpriteRenderer on the segment (or keep it and set its
     alpha to a low value, e.g. 0.2-0.3 — the segment's 2D collider still
     drives PotTrailCaptureSensor overlap queries).
   - Recommendation: fully disable the SpriteRenderer to avoid visual
     double-rainbow. The segment's 2D trigger collider remains for the
     capture sensor.

6. Apply all prefab overrides.

### Code steps
None. The trail harvesting code paths are unchanged. `PotTrailCaptureSensor`
queries Physics2D overlap, which works regardless of SpriteRenderer state.

### Verification checklist
- [ ] PF_Unicorn prefab has UnicornStandIn child with BusinessMan mesh
- [ ] PF_Unicorn prefab has RainbowCosmeticFX child with FX_RainbowGlow_01
- [ ] ParticleSystem loops and emits in the XY plane (visible from camera)
- [ ] PF_RainbowTrailSegment SpriteRenderer is disabled
- [ ] PF_RainbowTrailSegment BoxCollider2D remains intact (gameplay)
- [ ] Play — unicorn roams emitting visible rainbow sparkle particles
- [ ] Pots placed in the trail's path still harvest on overlap (verify by
      playing near a pot and watching the gold counter increment when the
      unicorn-targeting trail passes over)
- [ ] Unicorn collision with player still knocks player back and dazes

### Rollback
Re-enable the unicorn sprite SpriteRenderer, delete the civilian child,
delete the particle effect child. Re-enable the rainbow trail segment
SpriteRenderer.

---

## Phase H — Pot and Fae Tree 3D Swaps

### Goal
All three pots render as `SM_Env_Planter_Pot_01` (or variants 02, 03 for
variety). Fae tree anchor (already placed in Phase D) confirms
`SM_Env_Tree_Fruit_01` visual. Pot drain visual feedback preserved via
PotDrainFeedbackPresenter sprite halo (left enabled).

### Editor steps

1. Open `Assets/_Project/Prefabs/PF_Pot.prefab` in prefab mode.

2. Disable the SpriteRenderer on the pot visual child:
   - Locate the child referenced by PotController's `visualRenderer`.
   - Disable / uncheck the SpriteRenderer component.

3. Add the planter pot 3D mesh:
   - Drag `Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/Props/
     SM_Env_Planter_Pot_01.prefab` as child of VisualRoot, name `PotVisual`.
   - Position (0, 0, 0). Rotation (0, 0, 0) (pots are symmetric — no
     upright rotation needed; if they appear sideways, set to (90, 0, 0)).
   - Scale ~ (1, 1, 1). Tune to match prior sprite size.

4. Pot variety across the three pots in the scene (optional):
   - Open MainArena_3D scene.
   - For each of the three Pot instances in GameplayRoot, optionally
     replace their PotVisual child with `SM_Env_Planter_Pot_02.prefab`
     or `_03.prefab`. Or leave all three identical. Aesthetic preference.

5. Preserve PotDrainFeedbackPresenter halo:
   - The PotDrainFeedbackPresenter GameObject (search the pot hierarchy)
     uses its own SpriteRenderer to show drain state. Leave that
     SpriteRenderer ENABLED — it's our only visual drain indicator this
     phase. If the halo sprite is small or invisible behind the 3D pot,
     offset its Z so it sits above the pot top (e.g. Z = -0.5 so it
     renders closer to the camera, on top of the 3D mesh).

6. Apply prefab overrides.

7. Fae tree (already dressed in Phase D) — verify no further changes needed.

### Code steps
None.

### Verification checklist
- [ ] PF_Pot prefab has PotVisual child with SM_Env_Planter_Pot_01 mesh
- [ ] SpriteRenderer on the pot visual child disabled
- [ ] PotDrainFeedbackPresenter SpriteRenderer still enabled
- [ ] All three pots in MainArena_3D scene render as 3D planters
- [ ] Planters visually accept rainbow trail overlap (gold starts incrementing)
- [ ] Unicorn collision destroys pots as before
- [ ] Drain halo color (orange/red) visible above the 3D pot mesh
- [ ] Pots can be carried by player (carry interaction unaffected)
- [ ] Military can steal pots (pot disappears as before on theft)

### Rollback
Per pot prefab: re-enable the pot sprite, delete the planter mesh child.

---

## Phase I — Combat Projectile Visual Upgrade

### Goal
Player arrow projectile renders as `FX_Direction_Arrows_01` (POLYGON Particle
FX). Police and military projectiles either use the same swap or remain 2D
if they use a separate prefab (verify which and apply same pattern).

### Editor steps

1. Determine which projectile prefab each ranged attacker uses:
   - Search the Project for `ArrowProjectile.prefab`.
   - Check PoliceSpawner and MilitarySpawner (or RangedAttackController)
     for their projectile prefab assignments.
   - Police and military may share the same projectile prefab as the player
     (one prefab, shared pool) or have their own (e.g. PF_PoliceProjectile,
     PF_MilitaryProjectile). Apply the visual swap to each.

2. Open `ArrowProjectile.prefab` in prefab mode.

3. Disable the SpriteRenderer on the projectile visual child:
   - Locate the sprite child. Disable the SpriteRenderer component.

4. Add the 3D arrow FX:
   - Drag `Assets/PolygonParticleFX/Prefabs/FX_Direction_Arrows_01.prefab`
     as child of the projectile's VisualRoot (or as sibling of the
     disabled sprite), name `ArrowVisualFX`.
   - Position (0, 0, 0). Rotation (90, 0, 0) or as needed so the arrow
     points the projectile's forward direction.
   - Particle System: Looping = true (so the arrow trail persists while
     traveling), Play On Awake = true.
   - Tune emission rate and lifetime as you see fit; default prefab settings
     are a good starting point.

5. Apply prefab overrides.

6. Repeat for PF_PoliceProjectile / PF_MilitaryProjectile (or whatever
   their projectile prefabs are named) if they exist as separate prefabs.

### Code steps
None. ArrowProjectile movement code unchanged — Rigidbody2D on the prefab
root still drives travel and the 2D colliders drive hit detection.

### Verification checklist
- [ ] ArrowProjectile prefab has ArrowVisualFX child with Particle FX
- [ ] Projectile SpriteRenderer disabled
- [ ] Projectile BoxCollider2D / trigger 2D intact (gameplay hit detection)
- [ ] Play — player arrow fires visible FX arrow
- [ ] Arrow hits enemies (damage applies — verify by killing a traveller)
- [ ] Police projectiles (if separate prefab also swapped) render FX arrows
- [ ] Military projectiles render FX arrows
- [ ] Pots are NOT hit by projectiles (pot projectile immunity preserved —
      the 2D collider on the pot is still recognized by WeaponHitUtility's
      IsPotCollider check)

### Rollback
Re-enable projectile SpriteRenderer, delete the FX child.

---

## Phase J — HUD Element Swap (Gold Counter)

### Goal
Replace the placeholder TMP gold counter with one Fantasy Warrior HUD
component for the gold readout. Other UI (shop panel, outcome screen,
health bar, weapon icons) stay placeholder for this phase.

### Editor steps

1. Identify the gold counter prefab in InterfaceFantasyWarriorHUD:
   - Open `Assets/Synty/InterfaceFantasyWarriorHUD/Samples/Scenes/
     02_Demo_FantasyWarrior_Components_HPBars_Stats.unity`.
   - Look for a "Gold" or "Coin" or "Value Stat" widget — likely a
     TMP_Text label centered on a coin icon background.
   - Right-click that GameObject in the demo scene > "Open Original
     Prefab" or note the prefab path in the Inspector.
   - Alternatively, search `Assets/Synty/InterfaceFantasyWarriorHUD/`
     for prefabs matching "Gold" or "Stat" — pick the gold/coin readout.

2. Bring the gold HUD prefab into MainArena_3D:
   - Open MainArena_3D.
   - Drag the gold HUD prefab into UIRoot (or into the existing HUD Canvas).
   - Set its RectTransform anchors and position to match where the old
     gold TMP_Text was (typically top-right corner).

3. Update EconomyHudPresenter assignment:
   - Select the existing HUD Canvas GameObject containing
     `EconomyHudPresenter`. 
   - In Inspector > EconomyHudPresenter, the `goldText` serialized field
     (TMP_Text) currently points at the old gold label.
   - Reassign `goldText` to the new Fantasy Warrior HUD readout's TMP_Text
     child (or the parent if it has a TMP_Text component directly).
   - Disable the old gold TMP_Text GameObject (or delete it) — leave the
     new HUD element as the active visible gold counter.

4. Save the scene.

### Code steps
None. EconomyHudPresenter already writes gold to a TMP_Text field — only the
assigned target changed.

### Verification checklist
- [ ] Fantasy Warrior HUD gold component is in the HUD Canvas
- [ ] EconomyHudPresenter.goldText points to the new HUD TMP_Text
- [ ] Old placeholder gold TMP_Text is disabled or removed
- [ ] Play — gold starts at 0 and increments as pots harvest, displayed in
      the Fantasy Warrior HUD readout
- [ ] Gold spent on shop upgrades visibly decrements the HUD counter
- [ ] Restart correctly resets the HUD counter to 0

### Rollback
Re-assign EconomyHudPresenter.goldText to the old placeholder TMP_Text.
Re-enable the old placeholder. Delete the Fantasy Warrior HUD component.

---

## Phase K — Final Verification and Sign-Off

### Goal
End-to-end playthrough covering every gameplay system with 3D assets in
place. Any failure sends us to a per-phase rollback before sign-off.

### Manual checklist (Play from Boot, run a full 5-wave run)

- [ ] Boot → MainArena_3D loads without console errors
- [ ] Player moves with WASD and controller left-stick
- [ ] Player aims with mouse and right-stick
- [ ] Player fires arrows (3D arrow FX visible)
- [ ] Player takes damage, HP HUD decrements
- [ ] Player gets downed at zero HP, recovers after configured 3s
- [ ] Player knockback + daze on unicorn collision (DOTween-driven)
- [ ] Unicorn roams as 3D civilian, emits rainbow sparkle particles
- [ ] Unicorn-trail overlap on pots starts harvesting (gold increments)
- [ ] Three pots (or fewer if destroyed) tracked correctly
- [ ] Unicorn-pot collision destroys pots (gameplay intact)
- [ ] Traveller wave: 3D civilians spawn, seek pots, drain gold, flee with gold, die drop gold
- [ ] Police wave: 3D police spawn, ranged-attack player with FX arrows, drop weapons
- [ ] Military wave: 3D soldiers spawn, steal pots, return to combat, drop gold + weapon
- [ ] Player picks up temporary weapon, fires it, falls back to bow when ammo depleted
- [ ] Shop interaction opens panel, upgrades apply to current weapon visibly
- [ ] Portal spell places paired portals, teleports player/unicorn/pots
- [ ] Wave 5 cleared → win condition fires, outcome panel shows
- [ ] Or all pots destroyed → lose condition fires, outcome panel shows
- [ ] Restart from outcome panel restores Time.timeScale = 1 and reloads
      MainArena_3D cleanly
- [ ] At least one full 2-minute playthrough with NO console errors
- [ ] FPS stays readable (no obvious stutter with 3D models on screen)

### Acceptance
All checklist items pass → 3D Asset Integration phase complete. Update
`03_Progress.md` with a recap entry and mark this phase done.

### Known visual debt (deferred to polish phases)
- Goblin Locomotion `AC_Polygon.controller` wiring across PlayerRoot,
  TravellerController, PoliceController, MilitaryController (walk/run/idle)
- Health-stage readability on 3D character meshes (color/substance shader/
  HUD indicator)
- Shop panel mutation to Interface Fantasy Menus
- Outcome panel mutation to Interface Fantasy Menus death/loading screens
- Weapon pickup visuals: held-3D-mesh attachment for SM_Wep_Rifle_01 etc.
- Impact VFX: PolygonParticleFX FX_ShardMagic_Explosion_01 / FX_Sparks_01
  on hit, via existing WeaponImpactFeedbackSpawner
- Real unicorn asset hunt (replace BusinessMan stand-in)
- Pipe Unicorn rainbow particle with the actual trail emission timing for
  tighter visual alignment with segment timestamps

### Rollback
Full rollback to 2D version: set build settings to use MainArena.unity,
set SceneNames.MainArena = "MainArena", disable MainArena_3D in build
list, ignore MainArena_3D. All gameplay code remains unchanged.