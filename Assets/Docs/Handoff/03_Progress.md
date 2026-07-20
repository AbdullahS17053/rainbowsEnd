# Rainbow's End Progress Tracker

## Current status
- Planning status: complete enough to start implementation
- Implementation status: in progress
- Current phase: 3D Asset Integration (Phase A of 05_AssetIntegration_Plan.md)
- Current focus: integrating imported POLYGON / INTERFACE Synty assets over the existing 2D gameplay logic
- Recommended next executable task: Phase A — finalize URP renderer assets and convert POLYGON materials

## Phase tracker
- [x] Phase 0 - Project Bootstrap (implemented, awaiting manual Unity verification)
- [x] Phase 1 - Core Architecture Skeleton (implemented, awaiting manual Unity verification)
- [x] Phase 2 - Arena Scene Baseline (implemented, awaiting manual Unity verification)
- [x] Phase 3 - Player Input and Movement (implemented, awaiting manual Unity verification)
- [x] Phase 4 - Combat Foundation (implemented, awaiting manual Unity verification)
- [x] Phase 5 - Unicorn Roaming and Rainbow Trail (implemented, awaiting manual Unity verification)
- [x] Phase 6 - Pot Interaction and First Gold Loop (implemented, awaiting manual Unity verification)
- [x] Phase 7 - First Enemy Pressure Loop (implemented, awaiting manual Unity verification)
- [x] Phase 8 - Multi-Pot Economy and World Rules (implemented, awaiting manual Unity verification)
- [x] Phase 9 - Police and Combat Expansion (implemented, awaiting manual Unity verification)
- [x] Phase 10 - Portal System as First Spell (implemented, awaiting manual Unity verification)
- [x] Phase 11 - Shop and Upgrade Loop (implemented, awaiting manual Unity verification)
- [x] Phase 12 - Escalation and Run Orchestration (implemented, awaiting manual Unity verification)
- [/] Phase 13 - MVP Polish and Stability (partially implemented)
- [x] Post-MVP: Military Enemy (implemented, awaiting manual Unity verification)

## Completion rules
A phase should be marked complete only when all three are true:
- The feature works end-to-end in the editor
- Focused verification has been run
- A recap has been written covering outcome, approach, code review notes, verification, and progress

Phases 0-12: all code implemented and compiling; awaiting manual Unity verification for each.
Phase 13: partially implemented (knockback, separation, traveller flee, pot projectile fix done).
Post-MVP Military: implemented and compiling; awaiting manual Unity verification.

## What remains
- Phase 0-12: implemented but still need manual Unity verification and in-scene testing
- Phase 13: partially implemented — unicorn knockback, enemy separation, traveller steal-and-flee, and pot projectile immunity done; remaining polish items (pause controller, audio hooks, tilt-shift spike) not yet started
- Post-MVP Military: implemented (state machine, spawning, wave integration, pot theft, drops) but needs manual Unity verification
- Renderer direction is assumed to be URP 2D unless explicitly changed later
- Fae tree banking is confirmed and should be treated as a required future MVP mechanic
- Post-MVP remaining: more spells, status effects, traps, richer weapons, deeper AI, visual polish

## Testing queue
- EditMode tests for deterministic logic such as timers, health transitions, economy scaling, and wave sequencing
- PlayMode or manual tests for movement, aiming, harvesting, enemy drain, portals, shop upgrades, wave progression, outcomes, and reset flow
- Manual tests needed: unicorn knockback/daze, enemy anti-stacking separation, traveller steal-and-flee behavior, military mob (Combat/Stealing/Escaping states), military pot theft, military drops, pot projectile immunity
- Smoke and soak testing before MVP signoff

## Open decisions and assumptions
- Renderer: URP 2D by default
- Input: Unity Input System
- Platform target: PC with keyboard and mouse plus controller
- Implementation style: fully custom code, no dependency on Top Down Engine
- Portals remain in MVP scope and now have a first runtime slice implemented, but still need manual Unity verification
- Game structure: level-based rather than endless
- Orientation: landscape
- Cinemachine is available and should be used where camera rig value exists, not by default everywhere
- DOTween is available and should be used for feedback and presentation, not for authoritative gameplay state
- Military has been implemented as a Post-MVP extension (state machine, pot theft, wave integration)
- Enemy separation uses boids-style approach with OverlapCircle rather than hard collision resolution
- Travellers use 3-state machine (Seeking→Draining→Fleeing) with edge-based escape
- Pot projectile immunity is enforced at WeaponHitUtility layer (not layer masks or faction changes)

## Progress log
- 2026-05-06: Reviewed project docs and Unity baseline
- 2026-05-06: Drafted the MVP implementation plan
- 2026-05-06: Added memory-based handoff bundle for implementation, progress, and project context
- 2026-05-06: Mirrored the onboarding and planning bundle into Assets/Docs/Handoff
- 2026-05-06: Began Phase 0 implementation work
- 2026-05-06: Added Input System, URP, and test framework package declarations to Packages/manifest.json
- 2026-05-06: Created the initial Assets/_Project folder structure and assembly definitions
- 2026-05-06: Added GameBootstrap, scene constants, an EditMode test, and the Phase0ProjectSetup Unity editor automation script
- 2026-05-06: Confirmed Boot and MainArena scenes, build settings entries, and the IA_Gameplay input asset exist in the project
- 2026-05-06: Implemented the first Phase 1 foundation slice with damage contracts, health state and component, countdown timer, config ScriptableObjects, pooling surface, and deterministic EditMode tests
- 2026-05-06: Added the Phase1CoreSetup Unity editor automation script to generate default config assets
- 2026-05-07: Implemented Phase 1 composition wiring with a typed service registry, persistent ProjectContext bootstrap, ProjectConfigRegistry asset type, and new deterministic EditMode tests
- 2026-05-07: Added ArenaSceneContext and updated the Phase0 scene generator so MainArena has a scene-local composition root
- 2026-05-07: Implemented the first Phase 2 arena authoring slice with arena bounds, cover and concealment markers, fae tree anchor, spawn markers, and the Phase2ArenaSetup Unity editor automation script
- 2026-05-07: Fixed a Phase2ArenaSetup missing-reference issue by reacquiring the UI root transform after adding Canvas to the UI root object
- 2026-05-07: Switched Phase2ArenaSetup placeholder generation from quad meshes to SpriteRenderers backed by a generated placeholder sprite asset
- 2026-05-07: Recorded client clarifications that the game is landscape and level-based, with player downed state, pot-loss game-over rules, and star ratings based on banked pots
- 2026-05-07: Confirmed Cinemachine package availability and DOTween plugin availability for future camera and presentation work
- 2026-05-07: Implemented the first Phase 3 player slice with player runtime components, Phase3PlayerSetup, and deterministic EditMode tests for aim and movement utilities
- 2026-05-08: Wired PlayerConfig.AimTurnSpeed into PlayerAimController for smoothed facing and migrated PrefabPool internals to Unity's built-in ObjectPool while preserving the existing pool API and callbacks
- 2026-05-08: Implemented the first Phase 4 combat slice with a held-fire bow hitscan pipeline, movement-based spread, pooled impact feedback, Phase4CombatSetup, and deterministic EditMode tests for weapon runtime state and spread math
- 2026-05-08: Converted the Phase 4 bow from hitscan to pooled projectile arrows, added projectile hit filtering utilities and tests, and updated Phase4CombatSetup to generate arrow pooling and migrate old prefab state
- 2026-05-08: Implemented the first Phase 5 unicorn slice with arena-bounded roaming, pooled timestamped rainbow trail segments, Phase5UnicornSetup, and deterministic EditMode tests for roaming utility behavior
- 2026-05-08: Fixed rainbow trail segment alpha reuse after pooling and updated unicorn roaming to choose successive targets on different arena boundary sides for less repetitive edge movement
- 2026-05-08: Implemented the first Phase 6 pot slice with trail-overlap harvesting, continuous contact-based state transitions, EconomyState, HUD gold presentation, Phase6PotSetup, and deterministic EditMode tests for pot harvesting and economy multipliers
- 2026-05-08: Fixed the Phase 6 pot so it only harvests while actually overlapping live rainbow trail segments and resumes more reliably after movement by querying current overlaps directly instead of relying on one-shot trigger capture
- 2026-05-08: Switched the first HUD economy binding from legacy UI.Text to TextMesh Pro and updated Phase6PotSetup to import TMP essentials automatically when missing
- 2026-05-11: Implemented the first Phase 7 enemy pressure slice with pooled travellers, active-pot target selection, gold drain through EconomyState, pooled gold pickups, Phase7EnemySetup, and deterministic EditMode tests for traveller target selection and economy drain
- 2026-05-11: Fixed the first Phase 7 follow-up issues by letting arrows damage trigger-collider travellers while ignoring non-damageable trigger volumes, making traveller targeting fall back beyond engagement range, separating enemy drain feedback from the pot harvest color, and adding WaveConfig.InitialWaveDelaySeconds to the spawner
- 2026-05-11: Corrected the Phase 7 traveller behavior so pot drain depends only on direct pot contact rather than unicorn-trail harvesting state, and restored strict EngagementRange selection by removing the out-of-range fallback
- 2026-05-11: Implemented the first Phase 8 world-rules slice with PotRegistry, three-pot scene placement, dedicated pot drain alerts, unicorn-driven pot destruction, Phase8WorldRulesSetup, and deterministic EditMode tests for active-pot counting and unicorn pot destruction behavior
- 2026-05-11: Implemented the first Phase 9 police combat slice with pooled police projectiles, a dedicated PoliceSpawner, temporary ammo-limited weapon pickups, player weapon fallback logic, health-stage feedback, Phase9PoliceCombatSetup, and focused EditMode tests for inventory and cooldown preservation
- 2026-05-11: Applied late Phase 9 follow-up fixes so travellers retarget away from depleted gold sources, the shotgun uses configurable multishot volley behavior, and the player now enters the configured downed state before recovering
- 2026-05-11: Implemented the first Phase 10 portal slice with PlayerPortalController, PortalRegistry, paired PortalInstance teleportation, Phase10PortalSetup, and focused EditMode coverage for oldest-portal replacement and pairing rules
- 2026-05-11: Attempted focused Unity batch validation for the new portal registry tests, but the run was blocked because another Unity instance already had the project open
- 2026-05-13: Replaced fixed traveller gold and police weapon drops with configurable random drop-option lists, added shared EnemyDropOptions selection helpers plus EditMode coverage, and updated Phase7EnemySetup and Phase9PoliceCombatSetup to seed default drop entries on rerun
- 2026-05-13: Replaced physics-based player pot pushing with a toggle-based carry baseline, added PlayerPotCarryController plus focused PotController carry tests, disabled bow fire while carrying, and updated PF_Player plus the Phase 3 and Phase 9 setup passes to include the new interaction flow
- 2026-05-13: Implemented the first Phase 11 shop slice with ShopConfig, PlayerUpgradeRuntime, a fae-tree anchored ShopController, shop UI presenters, runtime weapon upgrade hooks, the rerunnable Phase11ShopSetup pass, and focused EditMode coverage for upgrade math and cooldown overrides
- 2026-06-04: Implemented the first Phase 12 level-orchestration slice with SO-authored exact-count waves, director-controlled traveller and police spawning, pot-loss lose condition, final-wave-clear win condition, restartable outcome UI, a rerunnable Phase12LevelOrchestrationSetup pass, and focused EditMode coverage for wave sequencing rules
- 2026-06-04: Attempted Unity batch EditMode tests for Phase 12, but the run was blocked because another Unity instance already had the project open; verified refreshed Runtime, Editor, and EditMode test assemblies compile with dotnet build
- 2026-06-17: Implemented unicorn knockback/daze on player collision — added PlayerKnockbackDistance/PlayerDazeDurationSeconds to UnicornConfig, daze state to PlayerRoot with DOTween .DOMove() for smooth push, detection in UnicornController collision handlers, updated IsDowned checks to IsDisabled in Shop/Portal controllers
- 2026-06-17: Implemented enemy anti-stacking separation — created EnemySeparationUtility.cs with Physics2D.OverlapCircle (ContactFilter2D), added SeparationRadius/SeparationStrength to EnemyConfig, applied in TravellerController and PoliceController
- 2026-06-17: Implemented traveller steal-and-flee — added 3-state machine (Seeking→Draining→Fleeing), StealRate/StealCapacity/FleeSpeedMultiplier to EnemyConfig, stolen gold tracking in PotDrain, GetNearestEdgePoint on ArenaBoundsAuthoring, death drops pocket change + stolen gold
- 2026-06-17: Implemented Military mob — added MilitaryCount to WaveDefinition (7-param ctor), RunSpawnType.Military, MilitaryController.cs (Combat/Stealing/Escaping states, pot-carrying), MilitarySpawner.cs (gold+weapon drops), military carry/destroy on PotController, updated EnemySeparationUtility, added configs to ProjectConfigRegistry, created Phase13MilitarySetup.cs editor automation
- 2026-06-17: Fixed military issues — return to Combat after successful pot theft (not despawn), fixed color by setting useCurrentColorAsHealthy=false + explicit healthyColor, fixed drop pools using FindPoolInParent by name, added defaultGoldDropAmount fallback, null safety in SpawnWeaponPickup/GoldPickup
- 2026-06-17: Fixed military projectiles destroying pots — root cause in WeaponHitUtility.IsValidDamageableTarget where pots (FactionId.Player + IDamageable) passed faction check; added IsPotCollider() helper walking parent chain for PotController, early-returns false before faction/damageable checks; covers all projectile types (bow, police, military); verified all three assemblies compile with 0 errors

## Asset Integration (3D visual upgrade)
- ADR-0001-3D-Asset-Integration.md: decisions record (locked 2026-07-21)
- 05_AssetIntegration_Plan.md: phased plan (Phases A-K) — execute in order
- Decision summary:
  - Hybrid architecture: 2D physics authoritative, 3D models decorative
  - Orthographic straight top-down camera preserved
  - New scene MainArena_3D.unity duplicated from Meadow Forest Demo
  - URP renderer finalized; POLYGON materials converted to URP
  - Asset map: Fairy=player, City civilians=Traveller, City police=Police, Military=Military, BusinessMan=unicorn stand-in, SM_Env_Planter_Pot=pots, SM_Env_Tree_Fruit=fae tree, FX_RainbowGlow=unicorn cosmetic, FX_Direction_Arrows=projectiles
  - All characters Idle this phase (bundled Animators); Goblin Locomotion deferred to polish
  - Visual scope: env + characters + one HUD element swap only
  - Verification: manual play-test per phase against checklist in 05_AssetIntegration_Plan.md

## Next recap template
Use this after each completed implementation step:
- Step completed:
- Outcome:
- Approach:
- Code review notes:
- Verification performed:
- Current phase:
- Next step:
- Blockers or decisions:
