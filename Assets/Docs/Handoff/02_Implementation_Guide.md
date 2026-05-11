# Rainbow's End MVP Implementation Guide

This document is the execution playbook for building the MVP from a blank Unity 6 project. It is written for handoff use, so another developer or coding assistant should be able to resume from this file without re-reading the design docs first.

## Working rules
- Build fully custom gameplay code from scratch.
- Use composition-first MonoBehaviour architecture.
- Keep all balanceable values in ScriptableObjects.
- Use TextMesh Pro for current and future HUD and UI text rather than legacy UI.Text.
- Pool any object spawned repeatedly at runtime: rainbow trail segments, projectiles, impact VFX, floating feedback, portal visuals, and pickup visuals.
- Ship a thin playable slice first, then expand to the full MVP.
- Treat portals as the first spell, not as a one-off mechanic.
- Keep placeholder art simple and replaceable: primitive sprites, flat colors, minimal animation dependencies.
- Keep gameplay state deterministic and data-driven; use DOTween only for presentation and feedback.
- Use Cinemachine as a camera rig and feedback tool when it adds value, but do not tie core simulation logic to it.

## Client clarifications
- The game is level-based, with more levels to be added later.
- The game is intended to run in landscape orientation.
- If the player's HP reaches zero, the player is downed for 3 seconds rather than ending the run immediately.
- Game over should eventually occur when the player's pots are all destroyed by the unicorn or taken off-screen by military mobs.
- End-of-run stars should be based on how many pots were banked in the home tree.

## Recommended root structure
Create these folders first under Assets:
- Assets/_Project/Art/Placeholder
- Assets/_Project/Audio
- Assets/_Project/Input
- Assets/_Project/Materials
- Assets/_Project/Prefabs
- Assets/_Project/Scenes
- Assets/_Project/ScriptableObjects
- Assets/_Project/Tests/EditMode
- Assets/_Project/Tests/PlayMode
- Assets/_Project/Runtime/Core
- Assets/_Project/Runtime/Gameplay/Player
- Assets/_Project/Runtime/Gameplay/Combat
- Assets/_Project/Runtime/Gameplay/Unicorn
- Assets/_Project/Runtime/Gameplay/Pots
- Assets/_Project/Runtime/Gameplay/Enemies
- Assets/_Project/Runtime/Gameplay/Spells
- Assets/_Project/Runtime/Systems/Economy
- Assets/_Project/Runtime/Systems/Pooling
- Assets/_Project/Runtime/UI
- Assets/_Project/Settings

## Naming conventions
- Runtime namespace root: RainbowsEnd
- Scene names: Boot, MainArena, Sandbox if needed
- Prefabs: prefix by type, for example PF_Player, PF_Pot, PF_Traveller
- ScriptableObjects: prefix by category, for example SO_PlayerConfig, SO_Weapon_Bow
- Input asset: IA_Gameplay
- Keep authoring MonoBehaviours thin; move logic into focused components and service classes where possible.

## Phase 0: Project Bootstrap
Goal: establish the Unity baseline once so later work does not keep reopening project configuration.

Implemented bootstrap helper:
- The project now includes an editor automation entry point at Assets/_Project/Editor/Phase0ProjectSetup.cs.
- After opening the project in Unity and allowing packages to resolve, run the menu command Rainbows End/Setup/Run Phase 0 Bootstrap.
- That setup pass generates the Boot scene, MainArena scene shell, build settings entries, and the IA_Gameplay input actions asset.

Unity editor steps:
1. Open the project in Unity 6.0.67f1.
2. Open Package Manager and install the Input System package if it is not already present.
3. Install URP and configure a 2D renderer. This is the recommended path because the repo is empty and future tilt-shift or post-processing support is cheaper now than later.
4. Accept the Input System restart prompt if Unity asks to switch active input handling.
5. Create the root folder structure listed above.
6. Create these scenes in Assets/_Project/Scenes: Boot and MainArena.
7. Add Boot and MainArena to Build Settings, with Boot first.
8. Create an Input Actions asset named IA_Gameplay in Assets/_Project/Input.
9. Create action maps for Gameplay and UI.
10. In Gameplay, create actions for Move, Aim, Fire, Interact, CastPortal, Pause.
11. Bind keyboard and mouse plus controller for each action.
12. Generate the C# wrapper for the input asset.
13. Create assembly definitions for Runtime, EditMode tests, and PlayMode tests.
14. Configure sorting layers for Ground, Props, Actors, Effects, UIWorld.
15. Configure layers or tags for Player, Enemy, Pot, Unicorn, Projectile, Portal, Cover, Concealment, Pickup.
16. Create a simple Boot scene object named BootRoot.
17. Attach a bootstrap script such as GameBootstrap to BootRoot.
18. Save the scene.

Scripts and assets to create in this phase:
- GameBootstrap
- Phase0ProjectSetup editor automation
- IA_Gameplay input asset and generated wrapper
- Runtime assembly definition
- EditMode and PlayMode assembly definitions
- URP asset and 2D renderer asset if URP is used

Exit criteria:
- Project opens cleanly.
- Input System works in editor play mode.
- Boot scene loads with no console errors.
- Folder and scene structure exists.

Recap to provide after implementation:
- What project settings changed
- What packages were added
- What baseline assets were created
- What risks remain before gameplay work starts

## Phase 1: Core Architecture Skeleton
Goal: create the minimum reusable architecture for data-driven gameplay.

Implemented Phase 1 helper:
- The project now includes an editor automation entry point at Assets/_Project/Editor/Phase1CoreSetup.cs.
- After opening the project in Unity, run the menu command Rainbows End/Setup/Run Phase 1 Core Setup.
- That setup pass generates default config assets for player, enemy, unicorn, pot, weapon, portal, economy, and wave authoring, plus a ProjectConfigRegistry asset in Resources for bootstrap-time loading.

Unity editor steps:
1. Under Assets/_Project/Runtime/Core, create folders for Bootstrap, Config, Common, State, Utilities.
2. Under Assets/_Project/Runtime/Systems, create Pooling and Time related folders as needed.
3. Create ScriptableObject menu roots for player, enemy, weapon, pot, unicorn, portal, economy, wave, and global settings configs.
4. Create a Settings folder for any project-wide config assets.
5. Create empty config assets so later scene setup can reference real assets instead of null placeholders.

Scripts and assets to create in this phase:
- Core contracts such as IDamageable, ITeamAffiliation, IPoolable, IInteractable, IPortalTraveller
- Phase1CoreSetup editor automation
- Runtime timer or cooldown utilities
- Health component and damage event plumbing
- Team or faction identifiers
- Object pool manager and pooled object component
- Service registration or bootstrap composition layer
- ScriptableObject config classes for player, unicorn, pot, enemy, weapon, portal, wave, and economy

Implemented composition layer files:
- Assets/_Project/Runtime/Core/Bootstrap/IReadOnlyServiceRegistry.cs
- Assets/_Project/Runtime/Core/Bootstrap/ServiceRegistry.cs
- Assets/_Project/Runtime/Core/Bootstrap/ProjectContext.cs
- Assets/_Project/Runtime/Core/Config/ProjectConfigRegistry.cs
- Assets/_Project/Runtime/Core/SceneManagement/ArenaSceneContext.cs

Implementation notes:
- Avoid giant manager classes. Prefer a few focused services with narrow responsibilities.
- Keep config data separate from runtime mutable state.
- If a runtime stat can change due to upgrades, use a runtime state or modifier layer instead of editing ScriptableObjects at runtime.
- The current PrefabPool implementation now wraps Unity's built-in ObjectPool while preserving the project's existing Rent and Return API and IPoolable lifecycle hooks.

Exit criteria:
- Config assets can be created from the Unity Create menu.
- Pooling can spawn and return a simple test object.
- Health and damage events compile and have at least one deterministic EditMode test.

## Phase 2: Arena Scene Baseline
Goal: create the first playable stage shell using primitives only.

Implemented Phase 2 helper:
- The project now includes an editor automation entry point at Assets/_Project/Editor/Phase2ArenaSetup.cs.
- Run the menu command Rainbows End/Setup/Run Phase 2 Arena Setup to upgrade MainArena with typed scene markers and placeholder world geometry without replacing the whole scene.
- That setup pass adds or updates arena bounds, cover blockers, concealment zones, fae tree anchor, spawn markers, a minimal HUD shell, and sprite-backed placeholder visuals generated from a local placeholder sprite asset.

Unity editor steps:
1. Open MainArena.
2. Create a top-level hierarchy:
- SceneContext
- World
- Environment
- GameplayRoot
- SpawnPoints
- UI
3. Under Environment, create child objects for Ground, Cover, Concealment, Props.
4. Create the play area using simple SpriteRenderer-based placeholder visuals for ground and world markers.
5. Create primitive placeholder objects for rocks and logs and place them as cover blockers.
6. Create placeholder concealment zones using trigger colliders for bushes or grass.
7. Create a FaeTree placeholder object at a readable anchor point.
8. Create empty objects around the arena edges for enemy spawn markers.
9. Create empty objects for initial pot spawn positions.
10. Add a CameraRoot and Main Camera if not already present.
11. Set camera framing so the whole arena is visible at once in a landscape layout.
12. If using URP, add but disable or keep minimal any post-processing volume hooks.
13. Create a Canvas under UI for HUD elements.
14. Add a simple gold counter text placeholder.

Camera note:
- Cinemachine is available in the project, but this slice can stay on a fixed orthographic camera until the gameplay loop needs camera rig features or feedback layers.

Scripts and components to attach:
- ArenaBoundsAuthoring or equivalent on SceneContext or World
- SpawnPointMarker on each spawn marker
- ConcealmentZone on bush trigger objects
- CoverBlocker or collision-only authoring on rock and log objects
- EconomyHudView or equivalent on the HUD root later when economy exists

Implemented Phase 2 files:
- Assets/_Project/Runtime/Gameplay/World/ArenaBoundsAuthoring.cs
- Assets/_Project/Runtime/Gameplay/World/SpawnPointKind.cs
- Assets/_Project/Runtime/Gameplay/World/SpawnPointMarker.cs
- Assets/_Project/Runtime/Gameplay/World/CoverBlocker.cs
- Assets/_Project/Runtime/Gameplay/World/ConcealmentZone.cs
- Assets/_Project/Runtime/Gameplay/World/FaeTreeAnchor.cs
- Assets/_Project/Editor/Phase2ArenaSetup.cs

Exit criteria:
- Arena loads with a stable camera and clean hierarchy.
- Cover blocks movement and line-of-sight layers can be reserved.
- Concealment areas are clearly placed and selectable in scene view.

## Phase 3: Player Input and Movement
Goal: make the player fully controllable before broader gameplay.

Implemented Phase 3 helper:
- The project now includes an editor automation entry point at Assets/_Project/Editor/Phase3PlayerSetup.cs.
- Run the menu command Rainbows End/Setup/Run Phase 3 Player Setup to generate PF_Player and place or update a Player instance in MainArena at the player spawn marker.

Unity editor steps:
1. Create a PF_Player prefab under Assets/_Project/Prefabs.
2. Use a placeholder sprite or primitive to represent the leprechaun.
3. Add Rigidbody2D and appropriate Collider2D components.
4. Add a PlayerRoot script or equivalent authoring component.
5. Attach movement, facing, input reader, and push interaction related components.
6. Create a player config asset and assign movement speed, acceleration, and interaction radius values.
7. Create a PlayerSpawn object in MainArena.
8. Place PF_Player in the scene or spawn it from Boot into MainArena depending on the bootstrap approach.

Scripts and components expected:
- PlayerInputReader
- PlayerMovementController
- AimController or FacingController
- PushInteractor or PotPushController
- PlayerConfig reference holder
- Debug cone toggle if this is implemented here

Implemented Phase 3 files:
- Assets/_Project/Runtime/Gameplay/Player/PlayerInputActionNames.cs
- Assets/_Project/Runtime/Gameplay/Player/PlayerInputReader.cs
- Assets/_Project/Runtime/Gameplay/Player/PlayerMovementUtility.cs
- Assets/_Project/Runtime/Gameplay/Player/PlayerAimResolver.cs
- Assets/_Project/Runtime/Gameplay/Player/PlayerRoot.cs
- Assets/_Project/Runtime/Gameplay/Player/PlayerMovementController.cs
- Assets/_Project/Runtime/Gameplay/Player/PlayerAimController.cs
- Assets/_Project/Editor/Phase3PlayerSetup.cs

Implementation notes:
- Keep input reading separate from movement logic.
- Controller aim and mouse aim should both flow into the same aim abstraction.
- Do not mix combat and movement logic in one class.
- The player config now carries downed duration so later HP-state work can implement the confirmed 3-second downed rule without moving that timing into scene logic.
- AimTurnSpeed is now used by PlayerAimController to smooth rotation toward the resolved aim direction rather than snapping instantly.

Exit criteria:
- Player moves with keyboard and controller.
- Player can aim with mouse and controller stick.
- Facing updates cleanly.
- Scene can show interact or push radius with debug gizmos if useful.

## Phase 4: Combat Foundation
Goal: build a reusable weapon pipeline with the bow first.

Implemented Phase 4 helper:
- The project now includes an editor automation entry point at Assets/_Project/Editor/Phase4CombatSetup.cs.
- Run the menu command Rainbows End/Setup/Run Phase 4 Combat Setup to extend PF_Player with the first combat components, create pooled bow arrow and impact prefabs, add arrow and impact pools to MainArena, and place a placeholder TargetDummy for damage verification.

Unity editor steps:
1. Create a SO_Weapon_Bow config asset.
2. Create pooled projectile arrow and impact feedback prefabs as placeholders.
3. Add combat components to PF_Player.
4. Create an Effects root in the scene for pooled runtime visuals if needed.

Scripts and components expected:
- WeaponController
- WeaponRuntimeState
- WeaponConfig
- FireCooldown component or shared timer utility usage
- Projectile launcher and projectile movement/damage resolver
- Impact feedback spawner
- Accuracy or cone spread calculator

Implemented Phase 4 files:
- Assets/_Project/Runtime/Gameplay/Combat/WeaponController.cs
- Assets/_Project/Runtime/Gameplay/Combat/WeaponRuntimeState.cs
- Assets/_Project/Runtime/Gameplay/Combat/WeaponSpreadUtility.cs
- Assets/_Project/Runtime/Gameplay/Combat/WeaponHitResult.cs
- Assets/_Project/Runtime/Gameplay/Combat/WeaponHitUtility.cs
- Assets/_Project/Runtime/Gameplay/Combat/ProjectileWeaponLauncher.cs
- Assets/_Project/Runtime/Gameplay/Combat/ArrowProjectile.cs
- Assets/_Project/Runtime/Gameplay/Combat/WeaponImpactFeedbackSpawner.cs
- Assets/_Project/Runtime/Gameplay/Combat/CombatImpactEffect.cs
- Assets/_Project/Editor/Phase4CombatSetup.cs
- Assets/_Project/Tests/EditMode/Combat/WeaponRuntimeStateTests.cs
- Assets/_Project/Tests/EditMode/Combat/WeaponSpreadUtilityTests.cs
- Assets/_Project/Tests/EditMode/Combat/WeaponHitUtilityTests.cs

Implementation notes:
- Make spread rules data-driven.
- Movement penalties should sample the player movement state, not duplicate it.
- The current bow slice uses held-fire pooled arrow projectiles, movement-based spread from WeaponConfig, and pooled impact feedback.
- Projectile travel and hit filtering are isolated from the fire cadence logic so later weapons can swap projectile behavior without rewriting the player firing loop.

Exit criteria:
- Bow auto-fire or held-fire behavior works according to design.
- Standing still improves accuracy relative to moving.
- Damage can be applied to a placeholder target.

## Phase 5: Unicorn Roaming and Rainbow Trail
Goal: implement the unicorn and the trail system that powers the economy.

Implemented Phase 5 helper:
- The project now includes an editor automation entry point at Assets/_Project/Editor/Phase5UnicornSetup.cs.
- Run the menu command Rainbows End/Setup/Run Phase 5 Unicorn Setup to generate PF_Unicorn and PF_RainbowTrailSegment, add a TrailRoot and RainbowTrailPool to MainArena, and place or update the Unicorn scene instance at the unicorn spawn marker.

Unity editor steps:
1. Create PF_Unicorn with a placeholder sprite or primitive.
2. Add movement components and collider setup.
3. Create a unicorn config asset.
4. Create a pooled PF_RainbowTrailSegment placeholder prefab.
5. Create a UnicornSpawn in MainArena.
6. Add an empty TrailRoot in the scene if you want hierarchy cleanliness for pooled segments.

Scripts and components expected:
- UnicornController
- UnicornRoamingStateMachine or simpler roaming controller
- RainbowTrailEmitter
- RainbowTrailSegment with timestamp data
- TrailPool setup through the shared pool manager

Implemented Phase 5 files:
- Assets/_Project/Runtime/Gameplay/Unicorn/UnicornRoamingUtility.cs
- Assets/_Project/Runtime/Gameplay/Unicorn/UnicornController.cs
- Assets/_Project/Runtime/Gameplay/Unicorn/RainbowTrailEmitter.cs
- Assets/_Project/Runtime/Gameplay/Unicorn/RainbowTrailSegment.cs
- Assets/_Project/Editor/Phase5UnicornSetup.cs
- Assets/_Project/Tests/EditMode/Unicorn/UnicornRoamingUtilityTests.cs

Implementation notes:
- Keep trail data separate from visuals if possible.
- Timestamp the trail so harvesting can later use capture duration or freshness without re-authoring the system.
- Keep unicorn movement bounded by the arena.
- The current slice uses pooled trail segments with spawn timestamps and lifetime-driven fadeout so pot harvesting can later read the same segments without replacing the trail representation.
- The current roaming pass now selects successive target points on arena boundary sides without repeating the same side, which produces better cross-arena movement and reduces edge pacing.

Exit criteria:
- Unicorn roams within the arena.
- Trail spawns and cleans up reliably with pooling.
- Trail segments expose enough data for pot capture.

## Phase 6: Pot Interaction and First Gold Loop
Goal: make one pot capture the rainbow trail and generate gold.

Implemented Phase 6 helper:
- The project now includes an editor automation entry point at Assets/_Project/Editor/Phase6PotSetup.cs.
- Run the menu command Rainbows End/Setup/Run Phase 6 Pot Setup to generate PF_Pot, upgrade PF_RainbowTrailSegment for trail capture triggers, place or update a Pot instance in MainArena, and bind the HUD gold label to a scene EconomyState.

Unity editor steps:
1. Create PF_Pot with placeholder art.
2. Add Rigidbody2D and Collider2D.
3. Add pushable or movable components.
4. Create a pot config asset.
5. Add one pot spawn point to MainArena and place PF_Pot there.
6. Connect the HUD gold text to the economy system once it exists.

Scripts and components expected:
- PotController
- PotHarvestState
- PotTrailCaptureSensor
- GoldEconomyService or EconomyState
- HUD gold presenter

Implemented Phase 6 files:
- Assets/_Project/Runtime/Gameplay/Pots/PotHarvestStatus.cs
- Assets/_Project/Runtime/Gameplay/Pots/PotHarvestUtility.cs
- Assets/_Project/Runtime/Gameplay/Pots/PotHarvestState.cs
- Assets/_Project/Runtime/Gameplay/Pots/PotTrailCaptureSensor.cs
- Assets/_Project/Runtime/Gameplay/Pots/PotController.cs
- Assets/_Project/Runtime/Systems/Economy/EconomyState.cs
- Assets/_Project/Runtime/UI/EconomyHudPresenter.cs
- Assets/_Project/Editor/Phase6PotSetup.cs
- Assets/_Project/Tests/EditMode/Pots/PotHarvestStateTests.cs
- Assets/_Project/Tests/EditMode/Economy/EconomyStateTests.cs

Implementation notes:
- The pot should have explicit states such as Idle, Harvesting, Interrupted, Destroyed.
- Moving the pot must cancel harvesting cleanly.
- Keep harvest calculation configurable so the three-pot multiplier can slot in later.
- The current slice harvests only while the pot is overlapping active rainbow trail segments, uses the oldest overlapping segment timestamp to calculate harvest rate, and routes generated gold through EconomyState so later multi-pot scaling can extend the same service.
- The HUD gold label now uses TextMesh Pro and Phase6PotSetup imports TMP essentials automatically when they are missing from the project.

Exit criteria:
- Player can push the pot.
- Pot only harvests while it remains in the rainbow trail.
- Gold increases while harvesting.
- Moving the pot stops harvest and preserves clean state.

## Phase 7: First Enemy Pressure Loop
Goal: complete the first defend-the-pot loop with Travellers only.

Unity editor steps:
1. Create PF_Traveller.
2. Add Rigidbody2D or movement components and collider setup.
3. Create a traveller config asset.
4. Create a gold pickup prefab PF_GoldPickup.
5. Add a simple enemy spawn controller to SceneContext or GameplayRoot.

Scripts and components expected:
- EnemySpawner
- TravellerController
- TargetSelection for active pot seeking
- PotDrain behavior
- Pickup component for dropped gold
- Death and despawn handling using pooling where appropriate

Implementation notes:
- Keep Traveller AI intentionally simple.
- Pot drain feedback should be easy to notice even with placeholder visuals.
- Enemy spawner should already be data-driven enough to accept future police definitions.
- The current slice prefers harvesting pots within EngagementRange, only targets pots inside that configured range, drains gold through EconomyState whenever the traveller is physically overlapping a non-destroyed pot, and drops pooled gold pickups on traveller death.
- Projectile hits now query trigger colliders but ignore non-damageable trigger volumes, which lets arrows damage traveller bodies without colliding with rainbow trail or pot sensor triggers.
- WaveConfig now includes an initial wave delay so the first traveller burst can be tuned separately from the steady-state spawn interval.

Exit criteria:
- Travellers spawn at edges.
- They seek an active pot.
- They drain gold if uninterrupted.
- They can be damaged and killed.
- Gold drops can be collected.

Implemented Phase 7 files:
- Assets/_Project/Runtime/Gameplay/Enemies/TravellerTargetSelectionUtility.cs
- Assets/_Project/Runtime/Gameplay/Enemies/PotDrain.cs
- Assets/_Project/Runtime/Gameplay/Enemies/GoldPickup.cs
- Assets/_Project/Runtime/Gameplay/Enemies/TravellerController.cs
- Assets/_Project/Runtime/Gameplay/Enemies/EnemySpawner.cs
- Assets/_Project/Editor/Phase7EnemySetup.cs
- Assets/_Project/Tests/EditMode/Enemies/TravellerTargetSelectionUtilityTests.cs

## Phase 8: Multi-Pot Economy and World Rules
Goal: expand the economy from one pot to the MVP ruleset.

Unity editor steps:
1. Duplicate or spawn up to three PF_Pot instances.
2. Create or place all three pot spawn points.
3. Update the HUD to show clearer economy feedback if needed.
4. Add placeholder feedback objects for drain alerts and destruction effects.

Scripts and components expected:
- Economy multiplier logic supporting 1x, 2.5x, 5x scaling
- Pot manager or a small registry service for active pots
- Unicorn collision handling for pot destruction
- Drain feedback presenter or floating text feedback

Implementation notes:
- Keep scaling rules in economy config assets, not hard-coded.
- Pot destruction and pot interruption are different events and should stay separate.
- If the fae tree remains only a landmark for now, do not over-engineer banking logic.
- The current slice adds a small PotRegistry service, expands scene placement to all configured pot spawn markers, destroys pots on unicorn contact, and uses a dedicated drain-alert presenter so enemy pressure does not hijack the pot harvest color state.

Exit criteria:
- Up to three pots can be active.
- Harvest scaling follows the planned progression.
- Unicorn collision destroys pots correctly.
- Traveller drain feedback is visible and understandable.

Implemented Phase 8 files:
- Assets/_Project/Runtime/Gameplay/Pots/PotRegistry.cs
- Assets/_Project/Runtime/Gameplay/Pots/PotDrainFeedbackPresenter.cs
- Assets/_Project/Editor/Phase8WorldRulesSetup.cs
- Assets/_Project/Tests/EditMode/Pots/PotRegistryTests.cs

## Phase 9: Police and Combat Expansion
Goal: deepen combat without jumping to full advanced AI.

Unity editor steps:
1. Create PF_Police.
2. Create SO_Weapon_Pistol and SO_Weapon_Shotgun if shotgun is included in this phase.
3. Create drop prefabs or pickup definitions for weapons and ammo.
4. Create simple muzzle or impact placeholder visuals if needed.

Scripts and components expected:
- PoliceController
- RangedAttackController or shared weapon controller usage for enemies
- Weapon pickup system
- Ammo-limited weapon runtime state
- Health stage feedback presenter for green, yellow, and red readability states

Implementation notes:
- Keep player weapon switching decoupled from pickup logic.
- Reuse the same weapon foundation instead of special-casing enemy weapons.
- The HP stage feedback system should work for player and enemies, even if visuals are minimal now.
- The current slice keeps the player bow as the default weapon, adds a temporary ammo-limited pickup weapon override, reuses projectile combat through a shared RangedAttackController for police, and drives police spawning through a dedicated scene spawner plus rerunnable setup.

Exit criteria:
- Police spawn and apply ranged pressure.
- Weapon drops and ammo behavior work.
- HP-stage feedback is visible.

Implemented Phase 9 files:
- Assets/_Project/Runtime/Gameplay/Combat/RangedAttackController.cs
- Assets/_Project/Runtime/Gameplay/Combat/WeaponPickup.cs
- Assets/_Project/Runtime/Gameplay/Player/PlayerWeaponInventory.cs
- Assets/_Project/Runtime/Core/Common/HealthStageFeedbackPresenter.cs
- Assets/_Project/Runtime/Gameplay/Enemies/PoliceController.cs
- Assets/_Project/Runtime/Gameplay/Enemies/PoliceSpawner.cs
- Assets/_Project/Editor/Phase9PoliceCombatSetup.cs
- Assets/_Project/Tests/EditMode/Player/PlayerWeaponInventoryTests.cs
- Assets/_Project/Tests/EditMode/Combat/WeaponRuntimeStateTests.cs

## Phase 10: Portal System as First Spell
Goal: build the MVP portal feature in a way that supports the future spell roster.

Unity editor steps:
1. Create PF_Portal.
2. Create a portal config asset.
3. Add CastPortal binding and any targeting indicator you want in placeholder form.
4. Add portal visuals under a pooled effects root or prefab setup.

Scripts and components expected:
- SpellController or PlayerSpellController
- PortalInstance
- Portal registry or pair service
- Teleport filter based on allowed traveller types
- Teleport cooldown or anti-loop safety logic

Implementation notes:
- Do not hard-code portal logic directly into the player movement script.
- Allow only player, unicorn, and pots to use portals for MVP.
- Oldest portal replacement should be an explicit tested rule.
- Add a short teleport lockout or exit offset to prevent instant re-entry loops.

Exit criteria:
- Player can place two portals.
- Placing a third replaces the oldest.
- Player, unicorn, and pots can traverse them.
- Disallowed entities do not use them.

## Phase 11: Shop and Upgrade Loop
Goal: let the player spend gold on a minimal set of useful upgrades.

Unity editor steps:
1. Create a simple shop UI panel in the Canvas.
2. Add placeholder buttons for upgrades such as Damage Up, Accuracy Up, Fire Rate Up.
3. Create upgrade config assets or a simple upgrade definition list.
4. Add a shop interaction trigger or hotkey according to the selected UX.

Scripts and components expected:
- ShopController
- UpgradeDefinition config assets or equivalent
- Runtime stat modifier layer
- UI presenter for purchase availability and current gold

Implementation notes:
- Upgrades should modify runtime stats, not source config assets.
- Keep the shop deliberately minimal for MVP.
- Favor transparent upgrade rules over content volume.

Exit criteria:
- Gold can be spent.
- Upgrades apply immediately.
- UI clearly shows affordability and result.

## Phase 12: Level Orchestration and Run Outcome
Goal: make the arena replayable with rising pressure and support the confirmed level-based run structure.

Unity editor steps:
1. Create wave or spawn definition assets.
2. Add a run director object under GameplayRoot or SceneContext.
3. Expose pacing values in ScriptableObjects for easy tuning.

Scripts and components expected:
- RunDirector or WaveDirector
- SpawnTable or wave definition assets
- Difficulty progression rules
- State reset handling for new runs

Implementation notes:
- Keep the escalation logic deterministic enough to tune.
- Do not build advanced flank or suppression AI yet.
- This phase should orchestrate existing systems, not replace them.
- This phase should also own the confirmed run-end rules, including banked-pot star scoring and non-death game-over conditions based on pot loss.

Exit criteria:
- Enemy pressure ramps up predictably.
- Traveller to Police progression works.
- Runs can reset cleanly.

## Phase 13: MVP Polish and Stability
Goal: make the MVP readable, stable, and handoff-ready.

Unity editor steps:
1. Review the scene hierarchy and prefab references for cleanliness.
2. Add pause or reset UI if not already present.
3. Add placeholder audio hooks or event points if actual audio is deferred.
4. Add final HUD elements required for weapon and portal state visibility.
5. If the gameplay is stable, test a minimal tilt-shift or post-processing spike behind a toggle or branch.

Scripts and components expected:
- PauseController
- RunResetController if not already covered
- Additional HUD presenters
- Optional lightweight post-processing toggle hooks

Implementation notes:
- Prioritize gameplay readability and bug removal over visual ambition.
- Pooling stability matters more than cosmetic polish.
- This phase should also clean naming, scene organization, and null-reference risk.

Exit criteria:
- Full MVP loop is playable from start to repeat.
- No major console spam or null reference issues.
- Keyboard and controller both work.
- A fresh smoke test and a longer soak run complete without critical failures.

## Post-MVP extension order
After MVP signoff, the next recommended order is:
1. Military enemy and improved AI state structure
2. Non-portal spells and shared status-effect system
3. Trap placement and trap interaction rules
4. Assault rifle and richer weapon content
5. Visual polish including stronger tilt-shift presentation

## Recap format to use after each implementation step
Each delivery recap should include:
- Outcome: what now works
- Approach: how the solution was structured
- Code review notes: important design choices, tradeoffs, and any technical debt introduced or avoided
- Verification: exact tests performed, both automated and manual
- Progress: what phase is complete, what phase is next, and any blockers or open decisions