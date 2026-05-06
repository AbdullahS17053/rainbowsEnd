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

## Implemented bootstrap files
- Assets/_Project/Runtime/Core/Bootstrap/GameBootstrap.cs: persistent bootstrap MonoBehaviour that loads MainArena from Boot
- Assets/_Project/Runtime/Core/SceneManagement/SceneNames.cs: shared scene name constants
- Assets/_Project/Editor/Phase0ProjectSetup.cs: editor menu command that generates scenes, build settings entries, and the input actions asset
- Assets/_Project/Editor/Phase1CoreSetup.cs: editor menu command that generates default config assets for the first gameplay systems
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
- Assets/_Project/Runtime/Systems/Pooling/IPoolable.cs and PrefabPool.cs: first-pass pooling contract and prefab pool component
- Assets/_Project/Tests/EditMode/HealthStateTests.cs and CountdownTimerTests.cs: deterministic EditMode coverage for the first core runtime primitives

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

## Known ambiguities
- Fae tree banking is present in the docs, but it is not yet confirmed as a mandatory MVP mechanic
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