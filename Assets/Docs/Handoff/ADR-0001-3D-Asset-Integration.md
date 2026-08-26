# ADR-0001: 3D Asset Integration Approach

Status: Accepted; current implementation amended 2026-08-26
Date: 2026-07-21

## Context

The Rainbow's End MVP was implemented with 2D placeholder sprites (SpriteRenderer
quads) over 2D physics (Rigidbody2D, Physics2D, BoxCollider2D). All gameplay
logic — movement, aim, combat, trail harvesting, pot interactions, enemy AI,
economy, orchestration — runs against 2D colliders and the XY plane at Z=0.

The implementation has advanced beyond the original camera proposal: the
current `MainArena_3D` scene uses a perspective 3/4 presentation camera while
retaining the same 2D gameplay coordinate system.

A set of POLYGON and INTERFACE Synty asset packs has been imported:
- POLYGON Meadow Forest (Nature Biome) — environment
- POLYGON Fantasy Kingdom — fairy character + fae tree props
- POLYGON City Pack — civilian + police characters
- POLYGON Military Pack — military characters + weapons
- POLYGON Goblin Locomotion — animation pack (deferred to polish)
- INTERFACE Fantasy Menus — UI (deferred to polish)
- INTERFACE Fantasy Warrior HUD — UI (one element used this integration)
- POLYGON Particle FX — rainbow trail + arrow visuals

POLYGON assets are 3D meshes with Y-up orientation and their own Animator
controllers. A decision was needed on how to integrate without rewriting the
entire gameplay codebase.

## Decisions

### 1. Architecture — Hybrid (2D physics + 3D visuals)
Keep ALL gameplay logic on 2D colliders and Rigidbody2D. 3D models are pure
decoration: attached as children of gameplay GameObjects, with no 3D colliders
and no 3D Rigidbody. The 2D collider on the parent GameObject continues to drive
physics, hit detection, pot capture, enemy separation, and projectile overlap.

Rationale: zero changes to combat/movement/pot/enemy code. Lowest risk. The 3D
meshes render as visual children in the current 3/4 camera. The whole 13-phase
gameplay implementation remains untouched by the visual layer.

Rejected alternative: 2.5D (3D colliders, XY-locked movement). Would require
porting every Physics2D call to Physics3D, reworking WeaponHitUtility,
PotTrailCaptureSensor, EnemySeparationUtility, RainbowTrailSegment overlap, and
every controller. ~10x the work for marginal visual gain in the MVP.

### 2. Camera — Perspective 3/4 (current implementation)
The original orthographic proposal is superseded by the current
`MainArena_3D` camera. As observed on 2026-08-26, `CameraRoot` is perspective
with a 60 degree field of view, position approximately `(0, -25.52, -12)`, and
rotation approximately `(310, 0, 0)`. `PlayerAimResolver` projects pointer rays
onto the XY gameplay plane, so movement, spawn, and combat coordinates remain
unchanged.

Rationale: preserves the authoritative 2D simulation while giving the imported
3D assets a readable diorama presentation.

### 3. Scene — Duplicate Meadow Forest Demo as MainArena_3D
Duplicate `Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Scene/Demo.unity`
to `Assets/_Project/Scenes/MainArena_3D.unity`. Strip demo-only objects, add the
project's composition root (ArenaSceneContext, gameplay roots). Copy gameplay
GameObjects from MainArena.unity into MainArena_3D.unity. Keep MainArena.unity
as a 2D fallback backup.

Rationale: gives us a fully-dressed forest environment to play in, preserves the
existing MainArena as a rollback path, and the editor can drive most of the work.

### 4. Render Pipeline — Finalize URP
Create URP renderer and pipeline assets under `Assets/_Project/Settings/`,
assign in Project Settings > Graphics. Use the Unity Render Pipeline Converter
to upgrade imported POLYGON materials from Standard/ specular to URP shaders.

Rationale: project manifest already depends on URP 17.0.4 but the renderer
assets were never finalized (per 04_Context.md). Future tilt-shift polish and
post-processing work assumed URP. Switching later means re-doing materials.

### 5. Lighting — Single Directional Light + Ambient
Add one Directional Light at back-top angle (e.g. rotation 45, -30, 0),
intensity ~1.0, no shadows or soft shadows only. Add mild ambient environment
lighting for shadow fill.

Rationale: cheap, readable, MVP-appropriate. Avoids baking and heavy post-FX.
Demo scene lighting was not copied because it couples our scene to demo-only
objects.

### 6. Asset-to-Role Map (locked)
- Player → `Assets/PolygonFantasyKingdom/Prefabs/Characters/SM_Chr_Fairy_01.prefab`
- Traveller (variable per spawn) → `Assets/Synty/PolygonCity/Prefabs/Characters/Character_BusinessMan_Shirt.prefab` and variants (BusinessMan_Suit, BusinessWoman, Female_Coat, Female_Jacket, Male_Hoodie, Male_Jacket)
- Police → `Assets/Synty/PolygonCity/Prefabs/Characters/Character_Female_Police.prefab` and `Character_Male_Police.prefab`
- Military → current serialized prefab uses `Assets/Synty/PolygonMilitary/Prefabs/Characters/SM_Chr_Soldier_Male_02.prefab` and variants
- Unicorn stand-in → `Assets/SimpleFarmAnimals/Prefabs/SA_Animal_Horse_Palomino.prefab` as a temporary moving 3D visual on the unicorn GameObject until a real unicorn is sourced
- Pot → current serialized prefab uses `Assets/PolygonFantasyKingdom/Prefabs/Props/SM_Prop_Pot_01.prefab`
- Fae tree anchor → `Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/Props/SM_Env_Tree_Fruit_01.prefab`
- Rainbow trail cosmetic overlay on unicorn → current serialized prefab uses `Assets/PolygonParticleFX/Prefabs/FX_RainbowGlow_02.prefab`
- Combat projectile visuals → current serialized `SpikyFireTrail` on `PF_BowArrow` and `Assets/Synty/PolygonMilitary/Prefabs/Items/SM_Item_Bullet_01.prefab` on `PF_PoliceProjectile` / `PF_MilitaryProjectile`

### 7. Animation — All Idle this phase
Use each POLYGON character prefab's bundled Animator controller and idle state
only. No locomotion wiring. The Goblin Locomotion `AC_Polygon.controller` and
walk/run transitions are deferred to a later polish phase.

Rationale: matches the "core integration only" scope. Movement still works
because Rigidbody2D drives the parent GameObject; the 3D model just does not
walk-animate. Adding locomotion wiring across PlayerRoot, TravellerController,
PoliceController, MilitaryController is a clean separate phase.

### 8. Visual Scope — Core integration only
This integration covers: environment dressing, character 3D swaps, projectile
visual swap, one Fantasy Warrior HUD element for the gold counter. Deferred to
polish phases: full HUD swap, shop panel mutation (Fantasy Menus), outcome
screen mutation, weapon pickup visuals, impact Particle FX, fae tree banking UI,
star rating UI.

### 9. Verification — Manual play-test per phase
After each phase, the user enters Play mode and runs an explicit checklist
(defined in `05_AssetIntegration_Plan.md`). No EditMode tests are added during
this integration. Matches the existing 03_Progress.md "awaiting manual Unity
verification" pattern.

### 10. Trail code — Untouched
`RainbowTrailEmitter` and `RainbowTrailSegment` pooling/activation/timestamp
logic stays exactly as implemented. Trail segment GameObjects continue to drive
harvesting via their Transform position. The visual SpriteRenderer on each
segment may be disabled (cosmetic-only) since `PotTrailCaptureSensor` queries
overlap by 2D colliders, not by SpriteRenderer visibility. A
`FX_RainbowGlow_02` particle effect child on the unicorn provides the visible
rainbow; the pooled segment sprites can stay invisible or be fully disabled.

## Consequences

- Positive: All 13 phases of implemented gameplay code continue to work
  unchanged. Visual upgrade is mechanical (prefab children + HUD ref).
- Positive: MainArena.unity 2D version preserved as a known-good rollback.
- Positive: URP foundation laid now for future tilt-shift and post-FX work.
- Negative: Visual mismatch — animated-looking 3D characters stuck in idle
  pose while moving. Acceptable for MVP; polish phase covers it.
- Negative: Health-stage color feedback (sprite color changes) doesn't show
  on 3D meshes. PotDrainFeedbackPresenter sprite halo retained on pots so
  drain state stays visible; character health-state feedback deferred.
- Negative: Unicorn is currently a Palomino horse model until a real unicorn is sourced.
  Cosmetic only; trail harvesting and collision gameplay intact.

## Rollback Path
Anything breaks → load MainArena.unity (2D version, untouched) and continue
gameplay there. Per-phase rollback: re-enable the SpriteRenderer child on the
affected prefab and disable the 3D child GameObject.
