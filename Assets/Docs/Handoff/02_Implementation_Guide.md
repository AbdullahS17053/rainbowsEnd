# Rainbow's End MVP Implementation Guide

This document is the execution playbook for building the MVP from a blank Unity 6 project. It is written for handoff use, so another developer or coding assistant should be able to resume from this file without re-reading the design docs first.

## Working rules
- Build fully custom gameplay code from scratch.
- Use composition-first MonoBehaviour architecture.
- Keep all balanceable values in ScriptableObjects.
- Pool any object spawned repeatedly at runtime: rainbow trail segments, projectiles, impact VFX, floating feedback, portal visuals, and pickup visuals.
- Ship a thin playable slice first, then expand to the full MVP.
- Treat portals as the first spell, not as a one-off mechanic.
- Keep placeholder art simple and replaceable: primitive sprites, flat colors, minimal animation dependencies.

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
- That setup pass generates default config assets for player, enemy, unicorn, pot, weapon, portal, economy, and wave authoring.

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

Implementation notes:
- Avoid giant manager classes. Prefer a few focused services with narrow responsibilities.
- Keep config data separate from runtime mutable state.
- If a runtime stat can change due to upgrades, use a runtime state or modifier layer instead of editing ScriptableObjects at runtime.

Exit criteria:
- Config assets can be created from the Unity Create menu.
- Pooling can spawn and return a simple test object.
- Health and damage events compile and have at least one deterministic EditMode test.

## Phase 2: Arena Scene Baseline
Goal: create the first playable stage shell using primitives only.

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
4. Create the play area using a simple sprite or quad for ground.
5. Create primitive placeholder objects for rocks and logs and place them as cover blockers.
6. Create placeholder concealment zones using trigger colliders for bushes or grass.
7. Create a FaeTree placeholder object at a readable anchor point.
8. Create empty objects around the arena edges for enemy spawn markers.
9. Create empty objects for initial pot spawn positions.
10. Add a CameraRoot and Main Camera if not already present.
11. Set camera framing so the whole arena is visible at once.
12. If using URP, add but disable or keep minimal any post-processing volume hooks.
13. Create a Canvas under UI for HUD elements.
14. Add a simple gold counter text placeholder.

Scripts and components to attach:
- ArenaBoundsAuthoring or equivalent on SceneContext or World
- SpawnPointMarker on each spawn marker
- ConcealmentZone on bush trigger objects
- CoverBlocker or collision-only authoring on rock and log objects
- EconomyHudView or equivalent on the HUD root later when economy exists

Exit criteria:
- Arena loads with a stable camera and clean hierarchy.
- Cover blocks movement and line-of-sight layers can be reserved.
- Concealment areas are clearly placed and selectable in scene view.

## Phase 3: Player Input and Movement
Goal: make the player fully controllable before broader gameplay.

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

Implementation notes:
- Keep input reading separate from movement logic.
- Controller aim and mouse aim should both flow into the same aim abstraction.
- Do not mix combat and movement logic in one class.

Exit criteria:
- Player moves with keyboard and controller.
- Player can aim with mouse and controller stick.
- Facing updates cleanly.
- Scene can show interact or push radius with debug gizmos if useful.

## Phase 4: Combat Foundation
Goal: build a reusable weapon pipeline with the bow first.

Unity editor steps:
1. Create a SO_Weapon_Bow config asset.
2. Create pooled projectile or hitscan feedback prefabs as placeholders depending on the final bow implementation.
3. Add combat components to PF_Player.
4. Create an Effects root in the scene for pooled runtime visuals if needed.

Scripts and components expected:
- WeaponController
- WeaponRuntimeState
- WeaponConfig
- FireCooldown component or shared timer utility usage
- Projectile or hitscan resolver
- Impact feedback spawner
- Accuracy or cone spread calculator

Implementation notes:
- Make spread rules data-driven.
- Movement penalties should sample the player movement state, not duplicate it.
- If using hitscan for the bow initially, still keep the feedback path decoupled so projectile weapons later can reuse the same interfaces.

Exit criteria:
- Bow auto-fire or held-fire behavior works according to design.
- Standing still improves accuracy relative to moving.
- Damage can be applied to a placeholder target.

## Phase 5: Unicorn Roaming and Rainbow Trail
Goal: implement the unicorn and the trail system that powers the economy.

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

Implementation notes:
- Keep trail data separate from visuals if possible.
- Timestamp the trail so harvesting can later use capture duration or freshness without re-authoring the system.
- Keep unicorn movement bounded by the arena.

Exit criteria:
- Unicorn roams within the arena.
- Trail spawns and cleans up reliably with pooling.
- Trail segments expose enough data for pot capture.

## Phase 6: Pot Interaction and First Gold Loop
Goal: make one pot capture the rainbow trail and generate gold.

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

Implementation notes:
- The pot should have explicit states such as Idle, Harvesting, Interrupted, Destroyed.
- Moving the pot must cancel harvesting cleanly.
- Keep harvest calculation configurable so the three-pot multiplier can slot in later.

Exit criteria:
- Player can push the pot.
- Pot can capture a rainbow trail.
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

Exit criteria:
- Travellers spawn at edges.
- They seek an active pot.
- They drain gold if uninterrupted.
- They can be damaged and killed.
- Gold drops can be collected.

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

Exit criteria:
- Up to three pots can be active.
- Harvest scaling follows the planned progression.
- Unicorn collision destroys pots correctly.
- Traveller drain feedback is visible and understandable.

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

Exit criteria:
- Police spawn and apply ranged pressure.
- Weapon drops and ammo behavior work.
- HP-stage feedback is visible.

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

## Phase 12: Escalation and Run Orchestration
Goal: make the arena replayable with rising pressure.

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