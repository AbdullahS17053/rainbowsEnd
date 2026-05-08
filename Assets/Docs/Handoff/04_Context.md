# Rainbow's End Project Context

## Project summary
Rainbow's End is a single-screen top-down tactical shooter built in Unity. The MVP focuses on the core loop of pushing pots into a unicorn's rainbow trail to harvest gold while defending those pots from enemies. The full game adds more spells, more advanced AI, more weapons, traps, and stronger visual polish, but the MVP should stay lean and prove the central loop first.

## Source documents
- Assets/Docs/Rainbow's End MVP GDD.txt: condensed MVP scope and milestone guidance
- Assets/Docs/Rainbow's End.txt: fuller design and long-term architecture constraints
- Assets/Docs/Close Encounters of the Mini Kind (2025).html: visual inspiration reference only, not a gameplay specification

## Current repository state at implementation start
- Unity version: 6000.0.67f1
- Assets initially contained docs only
- No gameplay scripts, scenes, or prefabs existed at implementation start
- No third-party gameplay framework is present in the repository
- Packages started at the Unity baseline modules only

## Current repository state after bootstrap work
- Assets/_Project/Scenes contains Boot.unity and MainArena.unity
- ProjectSettings/EditorBuildSettings.asset includes Boot and MainArena in build settings
- Assets/_Project/Input contains IA_Gameplay.inputactions
- ProjectSettings/ProjectSettings.asset is configured for the new Input System
- Assets/_Project/Settings is still empty, so URP renderer assets and assignment still need to be finalized if URP is kept as the renderer direction
- Packages/manifest.json now includes Cinemachine
- DOTween is present as an imported plugin under Assets/Plugins/Demigiant/DOTween

## Implemented bootstrap files
- Assets/_Project/Runtime/Core/Bootstrap/GameBootstrap.cs: persistent bootstrap MonoBehaviour that loads MainArena from Boot
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
- Assets/_Project/Runtime/Gameplay/Player/PlayerRoot.cs: player composition root and config bridge
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
- Assets/_Project/Runtime/Gameplay/Pots/PotTrailCaptureSensor.cs and PotController.cs: pushable pot authoring, current trail overlap detection, harvest lifecycle, and visual state changes
- Assets/_Project/Runtime/Systems/Economy/EconomyState.cs: scene-usable economy state with active harvester counting and gold accumulation
- Assets/_Project/Runtime/UI/EconomyHudPresenter.cs: HUD binding for the gold counter placeholder
- Assets/_Project/Tests/EditMode/Pots/PotHarvestStateTests.cs and Assets/_Project/Tests/EditMode/Economy/EconomyStateTests.cs: deterministic EditMode coverage for the first pot and economy slice

## Phase 5 follow-up refinements
- RainbowTrailSegment now preserves its authored base color across pooling cycles so reused trail segments do not stay invisible after fading out once
- UnicornController now chooses successive roam targets on different arena boundary sides instead of clamped local offsets, which produces more readable cross-arena traversal

## Follow-up architecture refinements
- Assets/_Project/Runtime/Systems/Pooling/PrefabPool.cs now uses UnityEngine.Pool.ObjectPool internally instead of a custom queue while keeping the project's existing pool API stable
- Assets/_Project/Runtime/Gameplay/Player/PlayerAimController.cs now consumes PlayerConfig.AimTurnSpeed for smoothed aim-facing behavior
- Assets/_Project/Runtime/Core/SceneManagement/ArenaSceneContext.cs now exposes an optional EffectsRoot so combat visuals can resolve shared scene-local pools without scene references baked into prefabs
- TextMesh Pro should be treated as the standard for current and future HUD and UI text instead of legacy UI.Text

## Agreed planning decisions
- Build fully custom gameplay code from scratch
- Use the Unity Input System
- Support keyboard and mouse plus controller for the MVP
- Start with the thinnest playable slice, then expand to the fuller MVP
- Keep police and portals inside the MVP plan, but not in the first implementation slice
- Use URP 2D from the start unless explicitly changed later
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
- The player is downed for 3 seconds when HP reaches zero
- Game over should be driven by pot-loss conditions rather than immediate player death
- Star ratings should be driven by how many pots are banked in the home tree
- The game should present in landscape

## Known ambiguities
- The exact MVP depth for military enemies remains intentionally deferred
- Tilt-shift is a visual target, not an early production blocker

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
- MainArena: gameplay arena, camera framing, environment blockers, concealment, spawn points, player and unicorn runtime context

## Planned prefab ownership
- PF_Player: movement, aim, combat, spell casting, health, interaction
- PF_Unicorn: roaming, trail emission, collision rules
- PF_Pot: pushable object, harvest state, capture sensor
- PF_Traveller: simple pot-seeking enemy and drain behavior
- PF_Police: ranged enemy using shared combat foundation
- PF_Portal: portal endpoint visual and teleporter component
- PF_GoldPickup: dropped resource pickup
- PF_RainbowTrailSegment: pooled trail element

## Core systems map
- Core bootstrap: startup, service registration, shared contracts
- Input system: player input abstraction for mouse, keyboard, and controller
- Movement system: player and AI locomotion with Rigidbody2D-based interactions
- Combat system: weapon config, fire cadence, spread rules, damage resolution, pickups
- Unicorn trail system: roaming movement plus timestamped trail data
- Pot harvesting system: pot states, trail capture, gold generation, interruption rules
- Enemy system: spawner, traveller logic first, police second, escalation hooks
- Economy system: current gold, scaling multipliers, pickups, shop purchases
- Spell system: portal spell first, reusable for future spells
- Pooling system: shared pooled lifecycle for repeated runtime objects
- UI system: HUD, shop, pause, feedback prompts

## Handoff usage order
1. Read this context file first.
2. Read 01_MVP_Build_Plan.md second.
3. Read 02_Implementation_Guide.md third.
4. Check 03_Progress.md for the current phase and latest recap.
5. Resume from the next unchecked phase unless an explicit blocker is recorded.