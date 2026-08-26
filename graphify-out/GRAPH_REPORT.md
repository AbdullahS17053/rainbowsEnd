# Graph Report - Assets/_Project/Runtime  (2026-08-26)

## Corpus Check
- 93 files · ~25,004 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1105 nodes · 2316 edges · 40 communities (30 shown, 10 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 91 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Core Runtime Definitions
- Arena Scene Wiring
- Bootstrap & Configuration
- Damage & Health
- Weapon Combat
- Unicorn Controller
- Shop & Upgrades
- Run Orchestration
- Timers & Effects
- Pooling
- Auto-Fire Targeting
- Player Combat
- LineRenderer References
- Weapon Pickup
- Enemy Drops
- Enemy Separation
- Gold Pickup
- Military Combat & Spawning
- Police Combat & Spawning
- Traveller Pot Drain
- Rect Type References
- Traveller Target Selection
- Input & Player Aiming
- Camera References
- Vector3 References
- Player Movement
- Player Pot Carry
- Pot Harvesting
- Economy HUD
- Pot Drain Feedback
- Portal System
- Sprite Renderer References
- Dictionary References
- Unity Object References
- Component References
- Rainbow Trail Emitter
- Rainbow Trail Segment
- BoxCollider2D References
- Poolable Lifecycle
- Character Animation

## God Nodes (most connected - your core abstractions)
1. `PotController` - 67 edges
2. `TravellerController` - 56 edges
3. `WeaponController` - 54 edges
4. `MilitaryController` - 54 edges
5. `PlayerRoot` - 43 edges
6. `PrefabPool` - 42 edges
7. `PoliceController` - 37 edges
8. `RainbowsEnd.Core.Config` - 34 edges
9. `ShopController` - 34 edges
10. `UnicornController` - 30 edges

## Surprising Connections (you probably didn't know these)
- `FactionId` --references--> `PlayerRoot`  [EXTRACTED]
  Core/Common/FactionId.cs → Gameplay/Player/PlayerRoot.cs
- `FactionId` --references--> `PotController`  [EXTRACTED]
  Core/Common/FactionId.cs → Gameplay/Pots/PotController.cs
- `FactionId` --references--> `UnicornController`  [EXTRACTED]
  Core/Common/FactionId.cs → Gameplay/Unicorn/UnicornController.cs
- `Health` --references--> `PlayerRoot`  [EXTRACTED]
  Core/Common/Health.cs → Gameplay/Player/PlayerRoot.cs
- `Health` --references--> `PotController`  [EXTRACTED]
  Core/Common/Health.cs → Gameplay/Pots/PotController.cs

## Import Cycles
- None detected.

## Communities (40 total, 10 thin omitted)

### Community 0 - "Core Runtime Definitions"
Cohesion: 0.06
Nodes (25): RainbowsEnd.Core.Bootstrap, RainbowsEnd.Core.Common, RainbowsEnd.Core.Config, RainbowsEnd.Core.SceneManagement, string, RainbowsEnd.Core.Utilities, RainbowsEnd.Gameplay.Combat, RainbowsEnd.Gameplay.Enemies (+17 more)

### Community 10 - "Arena Scene Wiring"
Cohesion: 0.05
Nodes (23): GameBootstrap, MonoBehaviour, bool, string, IEnumerator, ArenaSceneContext, Transform, Camera (+15 more)

### Community 8 - "Bootstrap & Configuration"
Cohesion: 0.07
Nodes (23): IReadOnlyServiceRegistry, ProjectContext, string, bool, GameObject, ServiceRegistry, Dictionary, ActorConfigBase (+15 more)

### Community 7 - "Damage & Health"
Cohesion: 0.06
Nodes (23): DamageInfo, Vector2, Object, DamageResult, Health, float, bool, HealthStageFeedbackPresenter (+15 more)

### Community 4 - "Weapon Combat"
Cohesion: 0.06
Nodes (29): FactionId, WeaponConfig, ArrowProjectile, LayerMask, float, Collider2D, Vector2, Object (+21 more)

### Community 14 - "Unicorn Controller"
Cohesion: 0.12
Nodes (12): ITeamAffiliation, UnicornController, Rigidbody2D, Collider2D, SpriteRenderer, Vector2, float, Collision2D (+4 more)

### Community 2 - "Shop & Upgrades"
Cohesion: 0.06
Nodes (22): ShopUpgradeEffectType, ShopUpgradeDefinition, string, int, float, ShopConfig, PlayerUpgradeRuntime, float (+14 more)

### Community 11 - "Run Orchestration"
Cohesion: 0.08
Nodes (19): WaveDefinition, string, float, int, RunDirector, bool, RunOutcome, RunSpawnType (+11 more)

### Community 15 - "Timers & Effects"
Cohesion: 0.10
Nodes (8): CountdownTimer, CombatImpactEffect, float, PotHarvestState, PotHarvestStatus, PlayerPortalController, float, Vector2

### Community 18 - "Pooling"
Cohesion: 0.18
Nodes (7): PrefabPool, GameObject, int, Transform, bool, IObjectPool, Vector3

### Community 28 - "Auto-Fire Targeting"
Cohesion: 0.47
Nodes (3): WeaponAutoFireTargetingUtility, Vector2, IReadOnlyList

### Community 9 - "Player Combat"
Cohesion: 0.08
Nodes (21): float, int, bool, Color, List, Material, Vector2, FireControlMode (+13 more)

### Community 25 - "Weapon Pickup"
Cohesion: 0.26
Nodes (4): WeaponPickup, Collider2D, WeaponConfig, int

### Community 16 - "Enemy Drops"
Cohesion: 0.13
Nodes (10): GoldDropOption, int, WeaponDropOption, EnemyDropSelectionUtility, IReadOnlyList, MilitarySpawner, List, int (+2 more)

### Community 27 - "Enemy Separation"
Cohesion: 0.40
Nodes (4): EnemySeparationUtility, Collider2D, ContactFilter2D, Vector2

### Community 26 - "Gold Pickup"
Cohesion: 0.29
Nodes (3): GoldPickup, Collider2D, int

### Community 13 - "Military Combat & Spawning"
Cohesion: 0.08
Nodes (23): Transform, SpriteRenderer, Rigidbody2D, Collider2D, int, bool, Vector2, MilitaryState (+15 more)

### Community 5 - "Police Combat & Spawning"
Cohesion: 0.06
Nodes (29): Transform, SpriteRenderer, Rigidbody2D, Collider2D, int, bool, PoliceSpawner, List (+21 more)

### Community 3 - "Traveller Pot Drain"
Cohesion: 0.06
Nodes (26): CircleCollider2D, List, float, Rigidbody2D, CircleCollider2D, SpriteRenderer, Color, List (+18 more)

### Community 29 - "Traveller Target Selection"
Cohesion: 0.47
Nodes (4): TravellerTargetCandidate, Vector2, TravellerTargetSelectionUtility, IReadOnlyList

### Community 6 - "Input & Player Aiming"
Cohesion: 0.06
Nodes (25): PlayerAimController, Transform, Camera, float, Vector2, Vector2, PlayerInputReader, InputActionAsset (+17 more)

### Community 20 - "Player Movement"
Cohesion: 0.24
Nodes (6): Rigidbody2D, Vector2, PlayerMovementController, PlayerRoot, PlayerInputReader, ArenaBoundsAuthoring

### Community 19 - "Player Pot Carry"
Cohesion: 0.23
Nodes (3): PlayerPotCarryController, float, Vector2

### Community 1 - "Pot Harvesting"
Cohesion: 0.06
Nodes (18): PotController, Rigidbody2D, Collider2D, SpriteRenderer, Color, TextMeshPro, HashSet, bool (+10 more)

### Community 17 - "Economy HUD"
Cohesion: 0.13
Nodes (8): EconomyState, HashSet, bool, float, Object, EconomyHudPresenter, TMP_Text, string

### Community 22 - "Pot Drain Feedback"
Cohesion: 0.25
Nodes (6): PotDrainFeedbackPresenter, SpriteRenderer, Color, float, Vector3, bool

### Community 12 - "Portal System"
Cohesion: 0.08
Nodes (19): Collider2D, int, Vector2, Rigidbody2D, Transform, PortalRegistry, string, List (+11 more)

### Community 24 - "Rainbow Trail Emitter"
Cohesion: 0.18
Nodes (3): RainbowTrailEmitter, string, float

### Community 23 - "Rainbow Trail Segment"
Cohesion: 0.26
Nodes (5): RainbowTrailSegment, SpriteRenderer, List, Color, bool

### Community 21 - "Character Animation"
Cohesion: 0.16
Nodes (8): Quaternion, CharacterAnimationBridge, Rigidbody2D, Animator, string, Transform, bool, float

## Knowledge Gaps
- **1 isolated node(s):** `FireControlMode`
  These have ≤1 connection - possible missing edges or undocumented components.
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `PotController` connect `Pot Harvesting` to `Core Runtime Definitions`, `Weapon Combat`, `Input & Player Aiming`, `Damage & Health`, `Bootstrap & Configuration`, `Arena Scene Wiring`, `Unicorn Controller`, `Timers & Effects`, `Economy HUD`, `Player Pot Carry`, `Pot Drain Feedback`, `Rainbow Trail Emitter`?**
  _High betweenness centrality (0.142) - this node is a cross-community bridge._
- **Why does `MilitaryController` connect `Military Combat & Spawning` to `Core Runtime Definitions`, `Traveller Pot Drain`, `Police Combat & Spawning`, `Arena Scene Wiring`, `Portal System`, `Enemy Drops`, `Character Animation`?**
  _High betweenness centrality (0.106) - this node is a cross-community bridge._
- **Why does `PlayerRoot` connect `Input & Player Aiming` to `Core Runtime Definitions`, `Pot Harvesting`, `Shop & Upgrades`, `Weapon Combat`, `Damage & Health`, `Bootstrap & Configuration`, `Player Combat`, `Arena Scene Wiring`, `Unicorn Controller`, `Timers & Effects`, `Player Pot Carry`?**
  _High betweenness centrality (0.089) - this node is a cross-community bridge._
- **What connects `FireControlMode` to the rest of the system?**
  _1 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Core Runtime Definitions` be split into smaller, more focused modules?**
  _Cohesion score 0.05707450444292549 - nodes in this community are weakly interconnected._
- **Should `Arena Scene Wiring` be split into smaller, more focused modules?**
  _Cohesion score 0.05333333333333334 - nodes in this community are weakly interconnected._
- **Should `Bootstrap & Configuration` be split into smaller, more focused modules?**
  _Cohesion score 0.06561085972850679 - nodes in this community are weakly interconnected._