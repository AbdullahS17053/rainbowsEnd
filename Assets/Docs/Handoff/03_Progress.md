# Rainbow's End Progress Tracker

## Current status
- Planning status: complete enough to start implementation
- Implementation status: started
- Current phase: Phase 11 - Shop and Upgrade Loop
- Current focus: run the Phase 11 shop setup in Unity and verify Interact opens the fae-tree shop near the anchor, gold purchases succeed through the shop UI or number keys, and damage, fire-rate, and accuracy upgrades immediately affect the player's current weapon behavior
- Recommended next executable task: open the project in Unity, run Rainbows End/Setup/Run Phase 11 Shop Setup, then verify the player can open the shop near the fae tree with Interact, buy the default upgrades with gold, see affordability and level state update in the panel, and confirm the upgraded bow fires faster, hits harder, and tightens spread without changing the source weapon assets

## Phase tracker
- [ ] Phase 0 - Project Bootstrap
- [ ] Phase 1 - Core Architecture Skeleton
- [ ] Phase 2 - Arena Scene Baseline
- [ ] Phase 3 - Player Input and Movement
- [ ] Phase 4 - Combat Foundation
- [ ] Phase 5 - Unicorn Roaming and Rainbow Trail
- [ ] Phase 6 - Pot Interaction and First Gold Loop
- [ ] Phase 7 - First Enemy Pressure Loop
- [ ] Phase 8 - Multi-Pot Economy and World Rules
- [ ] Phase 9 - Police and Combat Expansion
- [ ] Phase 10 - Portal System as First Spell
- [ ] Phase 11 - Shop and Upgrade Loop
- [ ] Phase 12 - Escalation and Run Orchestration
- [ ] Phase 13 - MVP Polish and Stability
- [ ] Post-MVP extensions

## Completion rules
A phase should be marked complete only when all three are true:
- The feature works end-to-end in the editor
- Focused verification has been run
- A recap has been written covering outcome, approach, code review notes, verification, and progress

## What remains
- Phase 0 still needs URP renderer assets and any related pipeline assignment if URP is to be fully finalized in-project
- Phase 1 still needs the remaining composition and service wiring before it can be considered complete
- Phase 2 still needs manual verification in Unity and any scene-specific tuning after the automated setup pass
- Phase 3 still needs manual verification in Unity and any scene-specific tuning after the automated setup pass
- Phase 4 now has the first combat runtime slice implemented but still needs manual Unity verification and setup execution in-scene
- Phase 5 now has the first unicorn and trail runtime slice implemented but still needs manual Unity verification and setup execution in-scene
- Phase 6 now has the first pot and economy runtime slice implemented but still needs manual Unity verification and setup execution in-scene
- Phase 7 now has the first traveller pressure slice implemented but still needs manual Unity verification and setup execution in-scene
- Phase 8 now has the first multi-pot and world-rules slice implemented but still needs manual Unity verification and setup execution in-scene
- Phase 9 now has the first police and combat-expansion slice implemented but still needs manual Unity verification and setup execution in-scene
- Phase 10 now has the first portal-system slice implemented but still needs manual Unity verification and setup execution in-scene
- Phase 11 now has the first shop-and-upgrade slice implemented but still needs manual Unity verification and setup execution in-scene
- All later implementation phases remain in progress or not started
- Renderer direction is assumed to be URP 2D unless explicitly changed later
- Fae tree banking is confirmed and should be treated as a required future MVP mechanic

## Testing queue
- EditMode tests for deterministic logic such as timers, health transitions, and economy scaling
- PlayMode or manual tests for movement, aiming, harvesting, enemy drain, portals, shop upgrades, and reset flow
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