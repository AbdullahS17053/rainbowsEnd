# Rainbow's End Project Context

## Project summary
Rainbow's End is a single-screen tactical shooter presented through a perspective 3/4 camera and powered by 2D XY gameplay physics. The MVP focuses on the core loop of pushing pots into a unicorn's rainbow trail to harvest gold while defending those pots from enemies. The full game adds more spells, more advanced AI, more weapons, traps, and stronger visual polish, but the MVP should stay lean and prove the central loop first.

## Source documents
- Assets/Docs/Rainbow's End MVP GDD.txt: condensed MVP scope and milestone guidance
- Assets/Docs/Rainbow's End.txt: fuller design and long-term architecture constraints
- Assets/Docs/Close Encounters of the Mini Kind (2025).html: visual inspiration reference only, not a gameplay specification

## Historical repository state at implementation start
- Unity version: 6000.0.67f1
- Assets initially contained docs only
- No gameplay scripts, scenes, or prefabs existed at implementation start
- No third-party gameplay framework is present in the repository
- Packages started at the Unity baseline modules only

## Historical bootstrap snapshot
- Assets/_Project/Scenes contained Boot.unity and MainArena.unity
- ProjectSettings/EditorBuildSettings.asset initially included Boot and MainArena in build settings
- Assets/_Project/Input contains IA_Gameplay.inputactions
- ProjectSettings/ProjectSettings.asset is configured for the new Input System
- Assets/_Project/Settings was still empty at this snapshot, before the later URP integration
- Packages/manifest.json now includes Cinemachine
- DOTween is present as an imported plugin under Assets/Plugins/Demigiant/DOTween

## Current repository state (2026-08-26)
- Unity version: 6000.0.67f1
- Build scenes are `Boot.unity` (index 0) and `MainArena_3D.unity` (index 1); `SceneNames.MainArena` resolves to `MainArena_3D`
- Unity MCP currently reports `MainArena_3D` loaded in Edit mode, `ArenaSceneContext.IsConfigured = true`, no compilation in progress, and zero current console errors or warnings
- `MainArena_3D` has separate `EnvironmentRoot`, `WorldRoot`, `GameplayRoot`, `SpawnPointsRoot`, `EffectsRoot`, `UIRoot`, `CameraRoot`, and scene-system roots for run, shop, spell, and enemy systems
- `CameraRoot` is currently perspective with a 60 degree field of view and a 3/4 downward angle; `PlayerAimResolver` projects pointer rays onto the XY gameplay plane
- URP assets exist at `Assets/_Project/Settings/URP_Asset.asset` and `Assets/_Project/Settings/URP_Renderer.asset`
- The active scene includes environment dressing, arena bounds, cover blockers, concealment zones, fae-tree anchor, three pot spawn points, four enemy-edge spawn points, NavMesh data, pooled effects, shop UI, outcome UI, and a gold HUD
- 3D visual prefabs are currently serialized for the player Fairy, traveller BusinessMan, police, military soldier, Palomino horse unicorn, Polygon Fantasy Kingdom pot, and `FX_RainbowGlow_02` trail cosmetic; 2D physics components remain authoritative
- `CharacterAnimationBridge` now drives movement speed, optional movement-facing, and fire animation triggers for 3D characters
- `NavMeshPathfinder` is used by traveller, police, and military movement; `CoverBlocker` participates in player/enemy line-of-sight tests
- Gameplay phases 0-12, Phase 13 core polish items, and the Military extension are implemented in source; PlayMode/manual verification and final visual polish are still open
- The working tree has only the earlier MCP package/solution changes before this documentation update; no gameplay source was changed for onboarding

## Graphify reference (2026-08-26)
- Graph scope: `Assets/_Project/Runtime` C# runtime source; Unity scenes, prefabs, ScriptableObjects, imported art, and handoff docs are outside this graph and must be checked separately for serialized wiring and visual status
- Outputs: `graphify-out/graph.json` (machine-readable graph), `graphify-out/GRAPH_REPORT.md` (audit/report), and `graphify-out/graph.html` (interactive view)
- Current graph: 1,105 nodes, 2,316 edges, and 40 communities, refreshed from the 12 changed runtime files after the prior 2026-07-20 graph
- Graph delta: 90 nodes added, 1 removed, 172 edges added, and 187 edges removed during source reconciliation
- Integrity: no missing or dangling endpoints and no collapsed endpoint groups; one benign self-reference remains at `Gameplay/Spells/PortalInstance.cs:L22`
- Refresh guidance: use Graphify incremental update after runtime C# changes, then reconcile scene/prefab state through Unity MCP and update this handoff when behavior or serialized wiring changes

## Implemented bootstrap files
- Assets/_Project/Runtime/Core/Bootstrap/GameBootstrap.cs: persistent bootstrap MonoBehaviour that loads MainArena_3D from Boot
- Assets/_Project/Runtime/Core/Bootstrap/ProjectContext.cs: persistent composition root that loads and registers shared services and core config
- Assets/_Project/Runtime/Core/Bootstrap/IReadOnlyServiceRegistry.cs and ServiceRegistry.cs: typed service access and registration layer for project-level dependencies
- Assets/_Project/Runtime/Core/SceneManagement/SceneNames.cs: shared scene name constants
- Assets/_Project/Runtime/Core/SceneManagement/ArenaSceneContext.cs: scene-local composition root for MainArena references and scene service registration
- Assets/_Project/Editor/Phase0ProjectSetup.cs: editor menu command that generates scenes, build settings entries, and the input actions asset
- Assets/_Project/Editor/Phase1CoreSetup.cs: editor menu command that generates default config assets for the first gameplay systems
- Assets/_Project/Editor/Phase2ArenaSetup.cs: editor menu command that upgrades MainArena with typed arena authoring objects and placeholder layout
- Assets/_Project/Editor/Phase3PlayerSetup.cs: editor menu command that generates PF_Player and places or updates the Player scene instance
- Assets/_Project/Editor/Phase4CombatSetup.cs: editor menu command that adds the first combat slice to PF_Player, creates pooled bow impact visuals, and adds combat validation objects to MainArena
- Assets/_Project/Editor/Phase5UnicornSetup.cs: editor menu command that generates the unicorn and rainbow trail prefabs, wires the trail pool, and places or updates the Unicorn in MainArena
- Assets/_Project/Editor/Phase6PotSetup.cs: editor menu command that generates the pot prefab, upgrades trail segments for capture, wires scene economy HUD state, and places or updates a Pot in MainArena
- Assets/_Project/Editor/Phase12LevelOrchestrationSetup.cs: editor menu command that seeds the five-wave MVP level config, wires the run director, and creates the restartable win/lose outcome panel
- Assets/_Project/Editor/Phase13MilitarySetup.cs: editor menu command that generates PF_Military, military config SOs, pool setup, spawner wiring, and MainArena scene integration
- Assets/_Project/Runtime/RainbowsEnd.Runtime.asmdef: runtime assembly definition
- Assets/_Project/Tests/EditMode/RainbowsEnd.EditModeTests.asmdef: EditMode test assembly definition
- Assets/_Project/Tests/PlayMode/RainbowsEnd.PlayModeTests.asmdef: PlayMode test assembly definition
- Assets/_Project/Editor/RainbowsEnd.Editor.asmdef: editor assembly definition

## Implemented Phase 1 foundation files
- Assets/_Project/Runtime/Core/Common/FactionId.cs: faction identifiers shared across gameplay systems
- Assets/_Project/Runtime/Core/Common/DamageInfo.cs and DamageResult.cs: core damage payload and result types
- Assets/_Project/Runtime/Core/Common/IDamageable.cs and ITeamAffiliation.cs: reusable gameplay contracts
- Assets/_Project/Runtime/Core/Common/HealthState.cs and Health.cs: pure health state plus MonoBehaviour wrapper
- Assets/_Project/Runtime/Core/Utilities/CountdownTimer.cs: deterministic cooldown and timer primitive
- Assets/_Project/Runtime/Core/Config/*.cs: first-pass ScriptableObject config types for player, enemy, unicorn, pot, weapon, portal, economy, and wave authoring
- Assets/_Project/Runtime/Core/Config/ProjectConfigRegistry.cs: bootstrap-loadable config registry asset for shared gameplay authoring data
- Assets/_Project/Runtime/Systems/Pooling/IPoolable.cs and PrefabPool.cs: first-pass pooling contract and prefab pool component
- Assets/_Project/Tests/EditMode/HealthStateTests.cs and CountdownTimerTests.cs: deterministic EditMode coverage for the first core runtime primitives
- Assets/_Project/Tests/EditMode/ServiceRegistryTests.cs and ProjectConfigRegistryTests.cs: deterministic EditMode coverage for the new composition wiring

## Implemented Phase 2 arena files
- Assets/_Project/Runtime/Gameplay/World/ArenaBoundsAuthoring.cs: typed arena bounds authoring with clamp and containment helpers
- Assets/_Project/Runtime/Gameplay/World/SpawnPointKind.cs and SpawnPointMarker.cs: typed spawn marker authoring for player, unicorn, pots, enemy edges, and fae tree references
- Assets/_Project/Runtime/Gameplay/World/CoverBlocker.cs: placeholder cover authoring with blocking collider semantics
- Assets/_Project/Runtime/Gameplay/World/ConcealmentZone.cs: placeholder concealment authoring with trigger collider semantics
- Assets/_Project/Runtime/Gameplay/World/FaeTreeAnchor.cs: fae tree world anchor marker
- Assets/_Project/Art/Placeholder/SPR_SquarePlaceholder.png: generated placeholder sprite used by Phase2ArenaSetup for 2D SpriteRenderer visuals
- Assets/_Project/Tests/EditMode/Scene/ArenaBoundsAuthoringTests.cs: deterministic EditMode coverage for bounds clamping and containment

## Implemented Phase 3 player files
- Assets/_Project/Runtime/Gameplay/Player/PlayerInputActionNames.cs: shared input action map and action names for the player slice
- Assets/_Project/Runtime/Gameplay/Player/PlayerInputReader.cs: Input System bridge for gameplay actions without requiring generated wrapper code
- Assets/_Project/Runtime/Gameplay/Player/PlayerMovementUtility.cs: deterministic top-down velocity step helper
- Assets/_Project/Runtime/Gameplay/Player/PlayerAimResolver.cs: deterministic aim resolution for mouse and controller input
- Assets/_Project/Runtime/Gameplay/Player/PlayerRoot.cs and PlayerPotCarryController.cs: player composition root, input bridge, and toggle-based pot carry baseline that disables bow usage while carrying
- Assets/_Project/Runtime/Gameplay/Player/PlayerMovementController.cs: Rigidbody2D-driven top-down movement controller
- Assets/_Project/Runtime/Gameplay/Player/PlayerAimController.cs: visual facing and aim controller for mouse and controller aiming
- Assets/_Project/Tests/EditMode/Player/PlayerMovementUtilityTests.cs and PlayerAimResolverTests.cs: deterministic EditMode coverage for the Phase 3 utility layer

## Implemented Phase 4 combat files
- Assets/_Project/Runtime/Gameplay/Combat/WeaponController.cs: held-fire combat controller that samples player aim and movement state, applies spread, and launches the bow arrow
- Assets/_Project/Runtime/Gameplay/Combat/WeaponRuntimeState.cs: reusable cooldown state wrapper for weapon cadence
- Assets/_Project/Runtime/Gameplay/Combat/WeaponSpreadUtility.cs: deterministic spread math for standing and moving accuracy
- Assets/_Project/Runtime/Gameplay/Combat/WeaponHitResult.cs: reusable hit payload for projectile collision resolution and feedback
- Assets/_Project/Runtime/Gameplay/Combat/WeaponHitUtility.cs: shared projectile hit filtering and damage application helper for faction-aware combat hits
- Assets/_Project/Runtime/Gameplay/Combat/ProjectileWeaponLauncher.cs and ArrowProjectile.cs: pooled projectile launch and travel path for the bow slice
- Assets/_Project/Runtime/Gameplay/Combat/WeaponImpactFeedbackSpawner.cs and CombatImpactEffect.cs: pooled impact feedback path for the bow slice
- Assets/_Project/Tests/EditMode/Combat/WeaponRuntimeStateTests.cs, WeaponSpreadUtilityTests.cs, and WeaponHitUtilityTests.cs: deterministic EditMode coverage for the Phase 4 combat utility layer

## Implemented Phase 5 unicorn files
- Assets/_Project/Runtime/Gameplay/Unicorn/UnicornRoamingUtility.cs: deterministic movement helpers for bounded roaming target selection and position stepping
- Assets/_Project/Runtime/Gameplay/Unicorn/UnicornController.cs: arena-bounded roaming controller and unicorn runtime anchor
- Assets/_Project/Runtime/Gameplay/Unicorn/RainbowTrailEmitter.cs: pooled trail emission component driven by the unicorn's movement state
- Assets/_Project/Runtime/Gameplay/Unicorn/RainbowTrailSegment.cs: pooled timestamped trail segment with lifetime-driven fadeout and despawn behavior
- Assets/_Project/Tests/EditMode/Unicorn/UnicornRoamingUtilityTests.cs: deterministic EditMode coverage for the unicorn roaming utility layer

## Implemented Phase 6 pot and economy files
- Assets/_Project/Runtime/Gameplay/Pots/PotHarvestStatus.cs, PotHarvestUtility.cs, and PotHarvestState.cs: explicit pot harvest state and deterministic gold-rate logic based on the oldest currently overlapping trail segment timestamp
- Assets/_Project/Runtime/Gameplay/Pots/PotTrailCaptureSensor.cs and PotController.cs: carryable kinematic pot authoring, current trail overlap detection, harvest lifecycle, and visual state changes that suspend harvesting while a player is carrying the pot
- Assets/_Project/Runtime/Systems/Economy/EconomyState.cs: scene-usable economy state with active harvester counting and gold accumulation
- Assets/_Project/Runtime/UI/EconomyHudPresenter.cs: HUD binding for the gold counter placeholder
- Assets/_Project/Tests/EditMode/Pots/PotHarvestStateTests.cs, PotControllerCarryTests.cs, and Assets/_Project/Tests/EditMode/Economy/EconomyStateTests.cs: deterministic EditMode coverage for the first pot and economy slice plus the carry-state transition baseline

## Implemented Phase 7 enemy files
- Assets/_Project/Runtime/Gameplay/Enemies/TravellerTargetSelectionUtility.cs: deterministic target preference for harvesting pots inside EngagementRange with no out-of-range fallback
- Assets/_Project/Runtime/Gameplay/Enemies/PotDrain.cs and TravellerController.cs: simple traveller movement, direct-contact pot pressure, economy drain, death handling, and visual state changes
- Assets/_Project/Runtime/Gameplay/Enemies/GoldPickup.cs, EnemySpawner.cs, and EnemyDropOptions.cs: pooled enemy drops, configurable traveller gold drop selection, player pickup collection, and edge-based traveller spawning
- Assets/_Project/Editor/Phase7EnemySetup.cs: Unity menu automation for PF_Traveller, PF_GoldPickup, traveller and pickup pools, and EnemySpawner scene wiring
- Assets/_Project/Tests/EditMode/Enemies/TravellerTargetSelectionUtilityTests.cs, EnemyDropSelectionUtilityTests.cs, plus EconomyStateTests.cs: deterministic coverage for target preference, random drop selection, and gold drain support
- Phase 7 follow-up fix: arrows now include trigger colliders in projectile queries but WeaponHitUtility ignores non-damageable trigger volumes, so traveller trigger bodies can be hit without rainbow trail or pot sensor false-positives
- Phase 7 follow-up fix: traveller drain feedback uses a distinct red-orange state color instead of the pot's harvest-yellow, and WaveConfig now exposes InitialWaveDelaySeconds for first-burst pacing
- Phase 7 behavior correction: traveller drain now depends on direct pot overlap regardless of whether the pot is currently harvesting, and target selection is again strict about EngagementRange with no out-of-range fallback
- Phase 7 drop refinement: EnemySpawner now supports a serialized gold-drop option list, resolves one gold amount per spawned traveller, and falls back to EconomyConfig min or max values only when no explicit list entries are configured

## Implemented Phase 8 world-rules files
- Assets/_Project/Runtime/Gameplay/Pots/PotRegistry.cs: small scene service that tracks registered pots and exposes active-pot counts for multi-pot rules
- Assets/_Project/Runtime/Gameplay/Pots/PotDrainFeedbackPresenter.cs plus PotController.cs: dedicated drain-alert feedback separated from harvest coloring, pot registration, and explicit unicorn destruction support
- Assets/_Project/Runtime/Gameplay/Enemies/TravellerController.cs and PotDrain.cs: traveller pot queries now prefer the PotRegistry when available and set pot drain-alert state as they pressure targets
- Assets/_Project/Runtime/Gameplay/Unicorn/UnicornController.cs: unicorn-to-pot collision path now destroys pots directly on contact
- Assets/_Project/Editor/Phase8WorldRulesSetup.cs and Assets/_Project/Tests/EditMode/Pots/PotRegistryTests.cs: Unity automation for three-pot scene placement and deterministic coverage for active-pot counting and unicorn destruction behavior

## Implemented Phase 9 combat-expansion files
- Assets/_Project/Runtime/Gameplay/Combat/RangedAttackController.cs: shared projectile firing cadence and spread logic for non-player ranged attackers built on the existing projectile launcher
- Assets/_Project/Runtime/Gameplay/Combat/WeaponPickup.cs plus Assets/_Project/Runtime/Gameplay/Player/PlayerWeaponInventory.cs: pooled temporary weapon drops, ammo-limited pickup state, and automatic fallback to the player's default bow
- Assets/_Project/Runtime/Gameplay/Enemies/PoliceController.cs, PoliceSpawner.cs, and EnemyDropOptions.cs: pooled police spawning from enemy-edge markers, ranged pressure against the player, and configurable random weapon or ammo pickup drops on death
- Assets/_Project/Runtime/Core/Common/HealthStageFeedbackPresenter.cs: lightweight health readability presenter for sprite-color staging across healthy, wounded, critical, and depleted states
- Assets/_Project/Runtime/Gameplay/Combat/WeaponController.cs and WeaponRuntimeState.cs: player weapon flow now syncs against inventory overrides and preserves cooldown when reverting after temporary ammo is exhausted
- Assets/_Project/Editor/Phase9PoliceCombatSetup.cs: Unity menu automation for police config assets, police and pickup prefabs, scene pools, player feedback wiring, and the PoliceSpawner scene object
- Assets/_Project/Tests/EditMode/Player/PlayerWeaponInventoryTests.cs, Assets/_Project/Tests/EditMode/Combat/WeaponRuntimeStateTests.cs, plus Assets/_Project/Tests/EditMode/Enemies/EnemyDropSelectionUtilityTests.cs: deterministic coverage for temporary weapon equip or fallback behavior, configurable drop selection, and cooldown preservation across config swaps

## Implemented Phase 10 portal files
- Assets/_Project/Runtime/Gameplay/Spells/PlayerPortalController.cs: dedicated player spell controller that listens to CastPortal input, enforces portal cooldown, and places portals relative to the current aim direction
- Assets/_Project/Runtime/Gameplay/Spells/PortalRegistry.cs and PortalInstance.cs: active portal ownership, oldest-portal replacement, paired portal state, allowed-entity filtering, and teleport lockout handling for player, unicorn, and pots
- Assets/_Project/Runtime/Core/Config/PortalConfig.cs: portal tuning now includes placement distance in addition to cooldown, max active portals, exit offset, and reentry lockout
- Assets/_Project/Editor/Phase10PortalSetup.cs: Unity menu automation for PF_Portal, portal pool setup, the scene PortalRegistry object, and player spell-controller wiring
- Assets/_Project/Tests/EditMode/Spells/PortalRegistryTests.cs: deterministic coverage for portal pairing and oldest-portal replacement behavior

## Implemented Phase 11 shop files
- Assets/_Project/Runtime/Core/Config/ShopConfig.cs: data-driven shop title, interaction radius, and default upgrade definition list for the fae-tree shop slice
- Assets/_Project/Runtime/Gameplay/Player/PlayerUpgradeRuntime.cs: player-owned runtime upgrade state that accumulates purchases and resolves modified weapon damage, cooldown, and spread without mutating source configs
- Assets/_Project/Runtime/Gameplay/Shop/ShopController.cs plus Assets/_Project/Runtime/UI/ShopPanelPresenter.cs and ShopUpgradeButtonView.cs: Interact-driven fae-tree shop flow, gameplay lock while the shop is open, affordability or level UI state, and number-key or button purchase support
- Assets/_Project/Runtime/Gameplay/Combat/WeaponController.cs, WeaponRuntimeState.cs, ProjectileWeaponLauncher.cs, and ArrowProjectile.cs: player combat path now accepts runtime damage, cooldown, and spread modifiers so shop purchases affect current weapon behavior immediately
- Assets/_Project/Editor/Phase11ShopSetup.cs: Unity menu automation for SO_ShopConfig creation, PF_Player upgrade-runtime wiring, the MainArena shop controller object, EventSystem creation, and the first shop prompt or panel UI
- Assets/_Project/Tests/EditMode/Shop/PlayerUpgradeRuntimeTests.cs plus Assets/_Project/Tests/EditMode/Combat/WeaponRuntimeStateTests.cs: deterministic coverage for purchase cap behavior, upgrade modifier math, and cooldown override support used by fire-rate upgrades

## Implemented Phase 12 orchestration files
- Assets/_Project/Runtime/Core/Config/WaveConfig.cs: level-wave ScriptableObject with exact-count wave definitions for start delay, spawn interval, max concurrent enemies, traveller count, and police count
- Assets/_Project/Runtime/Gameplay/Orchestration/RunOutcome.cs, RunWaveController.cs, and RunDirector.cs: deterministic wave sequencing, director-controlled spawning, pot-loss defeat, final-wave victory, and outcome reporting
- Assets/_Project/Runtime/Gameplay/Enemies/EnemySpawner.cs and PoliceSpawner.cs: existing spawners now act as spawn factories with active counts and explicit TrySpawn methods instead of endless autonomous timers
- Assets/_Project/Runtime/UI/RunOutcomePresenter.cs: simple TextMesh Pro outcome panel presenter with restart that restores Time.timeScale and reloads MainArena_3D
- Assets/_Project/Editor/Phase12LevelOrchestrationSetup.cs: rerunnable Unity automation for wiring MainArena orchestration and outcome UI
- Assets/_Project/Tests/EditMode/Orchestration/RunWaveControllerTests.cs: deterministic coverage for exact counts, traveller-first alternation, max concurrency stalls, clear-to-advance, win, and pot-loss precedence

## Implemented Phase 13 polish files
- Assets/_Project/Runtime/Gameplay/Enemies/EnemySeparationUtility.cs: static boids-style anti-stacking utility using Physics2D.OverlapCircle with ContactFilter2D, shared across traveller, police, and military controllers
- Assets/_Project/Runtime/Core/Config/EnemyConfig.cs (extended): added SeparationRadius, SeparationStrength, StealRate, StealCapacity, FleeSpeedMultiplier fields
- Assets/_Project/Runtime/Core/Config/UnicornConfig.cs (extended): added PlayerKnockbackDistance and PlayerDazeDurationSeconds fields
- Assets/_Project/Runtime/Gameplay/Player/PlayerRoot.cs (extended): daze state with DOTween .DOMove() for smooth knockback, IsDisabled property replacing IsDowned
- Assets/_Project/Runtime/Gameplay/Unicorn/UnicornController.cs (extended): collision handlers trigger player knockback/daze
- Assets/_Project/Runtime/Gameplay/Enemies/TravellerController.cs (extended): 3-state machine (Seeking→Draining→Fleeing), uses EnemySeparationUtility, flees to nearest arena edge
- Assets/_Project/Runtime/Gameplay/Enemies/PotDrain.cs (extended): stolen gold tracking with ResetStolenGold() for traveller death drops
- Assets/_Project/Runtime/Gameplay/Enemies/PoliceController.cs (extended): separation applied alongside existing movement
- Assets/_Project/Runtime/Gameplay/World/ArenaBoundsAuthoring.cs (extended): GetNearestEdgePoint() for edge-based flee targeting
- Assets/_Project/Runtime/Gameplay/Combat/WeaponHitUtility.cs (extended): added IsPotCollider() helper that walks parent chain for PotController, early-returns false in IsValidDamageableTarget to prevent projectiles damaging pots; covers all projectile types (bow, police, military)
- Assets/_Project/Runtime/Gameplay/Shop/ShopController.cs (updated): IsDowned check replaced with IsDisabled
- Assets/_Project/Runtime/Gameplay/Spells/PlayerPortalController.cs (updated): IsDowned check replaced with IsDisabled

## Implemented Post-MVP Military files
- Assets/_Project/Runtime/Gameplay/Enemies/MilitaryController.cs: full state machine with Combat, Stealing, and Escaping states; seeks pots for theft, carries them toward arena edge, uses ranged weapon against player
- Assets/_Project/Runtime/Gameplay/Enemies/MilitarySpawner.cs: spawning with configurable gold+weapon drop resolution via FindPoolInParent by name, defaultGoldDropAmount fallback, null safety in drop spawns
- Assets/_Project/Runtime/Gameplay/Pots/PotController.cs (extended): IsBeingStolenByMilitary flag, DestroyByMilitary method, military interaction path
- Assets/_Project/Runtime/Core/Config/WaveConfig.cs (extended): WaveDefinition now has 7-param constructor including MilitaryCount, RunSpawnType.Military enum value
- Assets/_Project/Runtime/Gameplay/Orchestration/RunWaveController.cs (extended): military active-count tracking, RunSpawnType.Military spawn handling
- Assets/_Project/Runtime/Gameplay/Orchestration/RunDirector.cs (extended): MilitarySpawner factory wiring
- Assets/_Project/Runtime/Core/Config/ProjectConfigRegistry.cs (extended): military config references
- Assets/_Project/Editor/Phase13MilitarySetup.cs: editor automation for military prefab/SO/pool/spawner creation and MainArena wiring
- Military key fixes: return to Combat after theft (not despawn), color via useCurrentColorAsHealthy=false + explicit healthyColor, drop pools via FindPoolInParent by name, defaultGoldDropAmount fallback

## Phase 5 follow-up refinements
- RainbowTrailSegment now preserves its authored base color across pooling cycles so reused trail segments do not stay invisible after fading out once
- UnicornController now chooses successive roam targets on different arena boundary sides instead of clamped local offsets, which produces more readable cross-arena traversal

## Current implementation refinements
- `Assets/_Project/Runtime/Core/Animation/CharacterAnimationBridge.cs`: shared Animator speed/facing/fire bridge used by the current 3D character prefabs
- `Assets/_Project/Runtime/Gameplay/World/NavMeshPathfinder.cs`: shared NavMesh corner path and waypoint steering helper used by enemy controllers
- `Assets/_Project/Runtime/Gameplay/World/CoverBlocker.cs`: cover collider and nearest-block query used by combat LOS checks
- `Assets/_Project/Runtime/Gameplay/World/ConcealmentZone.cs`: trigger-volume authoring for concealment spaces
- `Assets/_Project/Runtime/Gameplay/Player/PlayerAimResolver.cs`: perspective-camera pointer aiming by ray/XY-plane intersection, plus controller deadzone handling
- `Assets/_Project/Runtime/Gameplay/Player/PlayerRoot.cs`: owns player concealment alpha, post-shot reveal, one-second stun, two-second recovery veil, action permissions, and stun/reset presentation.
- `Assets/_Project/Art/Animators/PlayerFeedback.controller` and `Assets/_Project/Prefabs/PF_Player.prefab`: player-only Stunned/StandUp animation flow plus inactive `StunFeedback` stars child; shared `Player.controller` remains unchanged.

## Follow-up architecture refinements
- Assets/_Project/Runtime/Systems/Pooling/PrefabPool.cs now uses UnityEngine.Pool.ObjectPool internally instead of a custom queue while keeping the project's existing pool API stable
- Assets/_Project/Runtime/Gameplay/Player/PlayerAimController.cs now consumes PlayerConfig.AimTurnSpeed for smoothed aim-facing behavior
- Assets/_Project/Runtime/Core/SceneManagement/ArenaSceneContext.cs now exposes an optional EffectsRoot so combat visuals can resolve shared scene-local pools without scene references baked into prefabs
- TextMesh Pro should be treated as the standard for current and future HUD and UI text instead of legacy UI.Text
- Late Phase 9 follow-up: travellers now refresh targets when a current pot stops actively harvesting, weapon configs now support configurable projectile volleys for shotgun-style weapons, and PlayerRoot now owns the configured temporary downed state before recovery

## Phase 13 and Military follow-up refinements
- PlayerRoot now uses IsDisabled (combining downed + daze states) instead of IsDowned; ShopController and PlayerPortalController updated to check IsDisabled rather than IsDowned
- EnemySeparationUtility uses Unity 6's ContactFilter2D parameter for Physics2D.OverlapCircle (replaces deprecated OverlapCircleNonAlloc)
- WeaponHitUtility.IsValidDamageableTarget shortcuts pot projectile immunity before faction/damageable checks — covers all projectile types without layer mask or faction changes
- Military uses FindPoolInParent by pool name instead of FindFirstObjectByType for reliable drop resolution in mixed-pool scenes
- Traveller death drops include both pocket change (gold-drop option) and stolen gold (from PotDrain tracking)
- DOTween used for player knockback presentation (0.15s OutQuad) rather than disabling Rigidbody2D simulation

## Agreed planning decisions
- Build fully custom gameplay code from scratch
- Use the Unity Input System
- Support keyboard and mouse plus controller for the MVP
- Start with the thinnest playable slice, then expand to the fuller MVP
- Keep police and portals inside the MVP plan, but not in the first implementation slice
- Use URP; retain 2D gameplay physics while the current presentation uses the perspective 3/4 camera
- Delay tilt-shift polish until gameplay is stable
- Use ScriptableObjects for gameplay tuning and content configuration
- Use object pooling for repeated runtime objects
- Treat portals as the first spell so the later spell system can reuse the same architecture
- Treat the game as level-based rather than endless
- Keep the game in landscape orientation
- Treat fae tree banking as a required later MVP mechanic
- Use Cinemachine where camera rigging or feedback value exists
- Use DOTween for presentation, UI, and feel work rather than authoritative simulation logic

## Client clarifications
- The game is level-based, with room for more levels to be added later
- The player enters a 1-second stun and then a 2-second recovery veil when HP reaches zero
- Game over should be driven by pot-loss conditions rather than immediate player death
- Star ratings should be driven by how many pots are banked in the home tree
- The game should present in landscape

## Approved player-state terminology
- **Recovery veil**: the two-second, post-stun player state after a health depletion. It restores the player to full health, makes them invulnerable and 35% opaque, and disables firing. Picking up a pot restores full visibility only; it does not end the remaining invulnerability or firing lockout. Movement and aiming remain available; the pot interaction and upgrade menu are the only enabled interactions. The opacity is configurable.
- **Concealed player**: a player inside a ConcealmentZone who is not carrying a pot. They are 35% opaque (configurable). Firing makes them visible for the configurable reveal window (default 0.25 seconds); held fire keeps renewing that window. This feature changes presentation only; enemy targeting is unchanged.
- **Stun presentation**: a health depletion uses the player-only `PlayerFeedback.controller`, plays `Stunned.fbx` with `StunnedCirclingStarsSimple`, then `Stand Up.fbx` as the recovery veil begins.
- **Timing authority**: gameplay timing takes priority over source clip length. The one-second stun and two-second recovery veil are configurable values; the player-only Animator accelerates the imported clips to fit the authored gameplay timing.

## Approved unicorn-state terminology
- **Pot-impact warning**: A thin red projected line from the unicorn to its current target, shown only while that route intersects a vulnerable pot, whether grounded or carried. _Avoid_: pot marker, hit indicator.

- **Pot-carry immunity**: A player carrying a pot ignores unicorn knockback and daze; the carried pot remains vulnerable to unicorn contact. _Avoid_: unicorn invulnerability, pot immunity.

- **Trail float**: A subtle vertical bob applied only to a Rainbow Trail segment's visual child; the segment's pooled root and 2D capture collider remain fixed. _Avoid_: moving trail gameplay, floating collider.

- **Portal trail continuity**: Unicorn portal traversal retains the active trail and begins normal trail emission at the exit point without resetting trail age. _Avoid_: trail restart, portal bridge.

- **Cross-map return**: After reaching the arena's top edge, the unicorn's next roaming target is on the bottom edge, preserving whole-map traversal without upper-area loops. _Avoid_: top-third exclusion, dead zone.

- **Unicorn daze**: The short player-control lock caused by a unicorn impact when the player is not carrying a pot. It includes a one-unit knockback, a half-second duration, and spinning-star feedback. _Avoid_: player downed state, recovery veil.

## Approved weapons and combat terminology
- **Fae Bolt**: the Player's unlimited default magic attack. It keeps the existing projectile mechanics, cooldown, targeting, damage, range, and pooling behavior, but uses a distinct green-and-gold magical projectile/trail/impact presentation. The Player shows no firearm while Fae Bolt is active.
- **Firearm override**: the existing temporary weapon equipped from a pooled WeaponPickup. It replaces Fae Bolt only while it has shots remaining, stays visibly drawn during idle, movement, and firing, and automatically returns the Player to Fae Bolt when exhausted. Assault rifles remain a Military-only drop.
- **Tactical tracer**: presentation-only firearm feedback: a fast visible bullet trail, short muzzle flash, and impact cue. It does not add hitscan, gravity/drop, penetration, reloads, magazines, recoil-driven aim, or alter pooled hit/damage behavior. Player tracers are amber; hostile tracers are red/orange; shotgun volleys show two representative tracers while all pellets still resolve normally.
- **Muzzle origin**: the authored Transform at the actual firearm muzzle, or the Player casting hand for Fae Bolt. It is the projectile and local fire-feedback origin. The old numerical muzzle offset remains a defensive fallback only when an authoring reference is unavailable.
- **Hip-fire cone**: Player-only movement posture while moving. It uses 70% of the active weapon range and 1.75 times its normal auto-fire-cone width, then restores the normal aiming values when stationary. Both multipliers are Inspector-tunable.
- **Aim-facing rule**: while aiming or fighting, a character's complete visual body and held weapon face its aim target; movement never overwrites that direction. Outside that state, the visual faces travel direction.

## Known ambiguities
- Tilt-shift is a visual target, not an early production blocker
- Remaining Post-MVP scope (more spells, status effects, traps) remains deferred

## Planned top-level project structure
Expected additions under Assets/_Project:
- Art/Placeholder
- Audio
- Input
- Materials
- Prefabs
- Scenes
- ScriptableObjects
- Tests/EditMode
- Tests/PlayMode
- Runtime/Core
- Runtime/Gameplay/Player
- Runtime/Gameplay/Combat
- Runtime/Gameplay/Unicorn
- Runtime/Gameplay/Pots
- Runtime/Gameplay/Enemies
- Runtime/Gameplay/Spells
- Runtime/Systems/Economy
- Runtime/Systems/Pooling
- Runtime/UI
- Settings

## Planned scene ownership
- Boot: startup and persistent service bootstrap
- MainArena_3D: current gameplay arena, perspective camera framing, 3D environment, blockers, concealment, spawn points, player and unicorn runtime context
- MainArena: retained 2D fallback scene for rollback/debugging

## Planned prefab ownership
- PF_Player: movement, aim, combat, spell casting, health, interaction
- PF_Unicorn: roaming, trail emission, collision rules
- PF_Pot: pushable object, harvest state, capture sensor
- PF_Traveller: simple pot-seeking enemy and drain behavior
- PF_Police: ranged enemy using shared combat foundation
- PF_Portal: portal endpoint visual and teleporter component
- PF_GoldPickup: dropped resource pickup
- PF_RainbowTrailSegment: pooled trail element
- PF_Military: multi-state enemy with pot-stealing capability

## Core systems map
- Core bootstrap: startup, service registration, shared contracts
- Input system: player input abstraction for mouse, keyboard, and controller
- Movement system: player and AI locomotion with Rigidbody2D-based interactions
- Combat system: weapon config, fire cadence, spread rules, damage resolution, pickups
- Unicorn trail system: roaming movement plus timestamped trail data
- Pot harvesting system: pot states, trail capture, gold generation, interruption rules
- Enemy system: director-controlled wave spawning, traveller logic first, police second, escalation hooks
- Economy system: current gold, scaling multipliers, pickups, shop purchases
- Spell system: portal spell first, reusable for future spells
- Pooling system: shared pooled lifecycle for repeated runtime objects
- UI system: HUD, shop, outcome screen, and feedback prompts; pause controller remains unimplemented

## Handoff usage order
1. Read this context file first.
2. Read 01_MVP_Build_Plan.md second.
3. Read 02_Implementation_Guide.md third.
4. Check 03_Progress.md for the current phase and latest recap.
5. Resume from the next unchecked phase unless an explicit blocker is recorded.
