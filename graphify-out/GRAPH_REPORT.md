# Graph Report - F:\Unity\rainbowsEnd\Assets\_Project\Runtime  (2026-07-20)

## Corpus Check
- Corpus is ~24,065 words - fits in a single context window. You may not need a graph.

## Summary
- 1016 nodes · 2331 edges · 36 communities (34 shown, 2 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 135 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- CountdownTimer Utility
- Bootstrap & Scene Management
- Shop Config
- Weapon Auto-Fire Targeting
- Pot Drain System
- Damage System
- Portal System
- Wave Config
- Enemy Separation & Ranged Attack
- Actor Config Base
- Unicorn Controller
- Enemy Drop Options
- Weapon Pickup
- Rainbow Trail Emitter
- Player Root
- Economy & Gold Transfer
- Projectile Launcher & Death Handling
- Arena Scene Context
- Player Pot Carry
- Weapon Config & Ranged Attack
- Police Controller
- Police Spawner
- Service Registry
- Arrow Projectile
- Pot Drain Feedback
- Faction & Team Affiliation
- Player Input Reader
- Player Aim Controller
- Player Movement Controller
- Game Bootstrap
- Concealment Zone
- Spawn Point Markers
- Cover Blocker
- Weapon Impact Feedback
- Player Aim Resolver
- Player Movement Utility

## God Nodes (most connected - your core abstractions)
1. `PotController` - 73 edges
2. `TravellerController` - 56 edges
3. `MilitaryController` - 52 edges
4. `WeaponController` - 49 edges
5. `PlayerRoot` - 49 edges
6. `PrefabPool` - 48 edges
7. `RainbowsEnd.Core.Config` - 39 edges
8. `WeaponConfig` - 34 edges
9. `PoliceController` - 34 edges
10. `ShopController` - 34 edges

## Surprising Connections (you probably didn't know these)
- `ArrowProjectile` --references--> `FactionId`  [EXTRACTED]
  Gameplay/Combat/ArrowProjectile.cs → Core/Common/FactionId.cs
- `RangedAttackController` --references--> `FactionId`  [EXTRACTED]
  Gameplay/Combat/RangedAttackController.cs → Core/Common/FactionId.cs
- `MilitaryController` --references--> `FactionId`  [EXTRACTED]
  Gameplay/Enemies/MilitaryController.cs → Core/Common/FactionId.cs
- `PoliceController` --references--> `FactionId`  [EXTRACTED]
  Gameplay/Enemies/PoliceController.cs → Core/Common/FactionId.cs
- `TravellerController` --references--> `FactionId`  [EXTRACTED]
  Gameplay/Enemies/TravellerController.cs → Core/Common/FactionId.cs

## Import Cycles
- None detected.

## Communities (36 total, 2 thin omitted)

### Community 0 - "CountdownTimer Utility"
Cohesion: 0.05
Nodes (20): CountdownTimer, float, CombatImpactEffect, bool, Collider2D, Color, float, HashSet (+12 more)

### Community 1 - "Bootstrap & Scene Management"
Cohesion: 0.08
Nodes (22): string, SceneNames, RainbowsEnd.Gameplay.Player, RainbowsEnd.UI, RainbowsEnd.Systems.Economy, RainbowsEnd.Gameplay.Unicorn, RainbowsEnd.Gameplay.Shop, RainbowsEnd.Core.Bootstrap (+14 more)

### Community 2 - "Shop Config"
Cohesion: 0.06
Nodes (22): float, int, string, ShopConfig, ShopUpgradeDefinition, ShopUpgradeEffectType, float, int (+14 more)

### Community 3 - "Weapon Auto-Fire Targeting"
Cohesion: 0.05
Nodes (20): FireControlMode, IReadOnlyList, Vector2, WeaponAutoFireTargetingUtility, bool, Color, float, int (+12 more)

### Community 4 - "Pot Drain System"
Cohesion: 0.07
Nodes (20): CircleCollider2D, float, List, PotDrain, bool, CircleCollider2D, Color, float (+12 more)

### Community 5 - "Damage System"
Cohesion: 0.06
Nodes (23): Object, Vector2, DamageInfo, DamageResult, bool, float, Health, bool (+15 more)

### Community 6 - "Portal System"
Cohesion: 0.07
Nodes (21): Color, PortalConfig, float, Vector2, PlayerPortalController, Collider2D, Component, Dictionary (+13 more)

### Community 7 - "Wave Config"
Cohesion: 0.06
Nodes (22): float, int, string, WaveDefinition, Func, bool, RunDirector, RunOutcome (+14 more)

### Community 8 - "Enemy Separation & Ranged Attack"
Cohesion: 0.09
Nodes (13): ContactFilter2D, Collider2D, Vector2, EnemySeparationUtility, bool, Collider2D, int, Rigidbody2D (+5 more)

### Community 9 - "Actor Config Base"
Cohesion: 0.10
Nodes (14): ActorConfigBase, EconomyConfig, EnemyConfig, PlayerConfig, PotConfig, ProjectConfigRegistry, UnicornConfig, WaveConfig (+6 more)

### Community 10 - "Unicorn Controller"
Cohesion: 0.13
Nodes (11): Collision2D, Collider2D, float, Rigidbody2D, SpriteRenderer, Vector2, UnicornController, Rect (+3 more)

### Community 11 - "Enemy Drop Options"
Cohesion: 0.12
Nodes (10): int, IReadOnlyList, EnemyDropSelectionUtility, GoldDropOption, WeaponDropOption, HashSet, int, List (+2 more)

### Community 12 - "Weapon Pickup"
Cohesion: 0.10
Nodes (8): Collider2D, int, WeaponConfig, WeaponPickup, Collider2D, int, GoldPickup, IPoolable

### Community 13 - "Rainbow Trail Emitter"
Cohesion: 0.11
Nodes (8): float, string, RainbowTrailEmitter, bool, Color, List, SpriteRenderer, RainbowTrailSegment

### Community 14 - "Player Root"
Cohesion: 0.14
Nodes (10): bool, Collider2D, float, InputActionAsset, Rigidbody2D, SpriteRenderer, Transform, Vector2 (+2 more)

### Community 15 - "Economy & Gold Transfer"
Cohesion: 0.13
Nodes (8): bool, float, HashSet, Object, EconomyState, string, TMP_Text, EconomyHudPresenter

### Community 16 - "Projectile Launcher & Death Handling"
Cohesion: 0.15
Nodes (8): IObjectPool, Quaternion, bool, GameObject, int, Transform, Vector3, PrefabPool

### Community 17 - "Arena Scene Context"
Cohesion: 0.16
Nodes (7): Camera, Transform, ArenaSceneContext, Rect, Vector2, ArenaBoundsAuthoring, FaeTreeAnchor

### Community 18 - "Player Pot Carry"
Cohesion: 0.23
Nodes (3): float, Vector2, PlayerPotCarryController

### Community 19 - "Weapon Config & Ranged Attack"
Cohesion: 0.19
Nodes (7): WeaponConfig, Collider2D, float, Object, Vector2, RangedAttackController, WeaponRuntimeState

### Community 20 - "Police Controller"
Cohesion: 0.18
Nodes (7): bool, Collider2D, int, Rigidbody2D, SpriteRenderer, Transform, PoliceController

### Community 21 - "Police Spawner"
Cohesion: 0.20
Nodes (5): HashSet, int, List, Transform, PoliceSpawner

### Community 22 - "Service Registry"
Cohesion: 0.17
Nodes (6): IReadOnlyServiceRegistry, bool, string, ProjectContext, Dictionary, ServiceRegistry

### Community 23 - "Arrow Projectile"
Cohesion: 0.21
Nodes (8): bool, Collider2D, float, Object, Vector2, ArrowProjectile, LayerMask, RaycastHit2D

### Community 24 - "Pot Drain Feedback"
Cohesion: 0.23
Nodes (6): bool, Color, float, SpriteRenderer, Vector3, PotDrainFeedbackPresenter

### Community 25 - "Faction & Team Affiliation"
Cohesion: 0.31
Nodes (7): FactionId, ITeamAffiliation, Collider2D, Object, string, Vector2, ProjectileWeaponLauncher

### Community 26 - "Player Input Reader"
Cohesion: 0.19
Nodes (6): InputActionAsset, string, Vector2, PlayerInputReader, InputAction, InputActionMap

### Community 27 - "Player Aim Controller"
Cohesion: 0.27
Nodes (5): Camera, float, Transform, Vector2, PlayerAimController

### Community 28 - "Player Movement Controller"
Cohesion: 0.38
Nodes (3): Rigidbody2D, Vector2, PlayerMovementController

### Community 29 - "Game Bootstrap"
Cohesion: 0.25
Nodes (6): bool, string, GameBootstrap, GameObject, IEnumerator, MonoBehaviour

### Community 30 - "Concealment Zone"
Cohesion: 0.32
Nodes (3): BoxCollider2D, Vector2, ConcealmentZone

### Community 31 - "Spawn Point Markers"
Cohesion: 0.29
Nodes (4): SpawnPointKind, int, string, SpawnPointMarker

### Community 32 - "Cover Blocker"
Cohesion: 0.38
Nodes (3): bool, BoxCollider2D, CoverBlocker

### Community 34 - "Player Aim Resolver"
Cohesion: 0.33
Nodes (4): Camera, Vector2, Vector3, PlayerAimResolver

## Knowledge Gaps
- **1 isolated node(s):** `FireControlMode`
  These have ≤1 connection - possible missing edges or undocumented components.
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `PotController` connect `CountdownTimer Utility` to `Bootstrap & Scene Management`, `Pot Drain System`, `Damage System`, `Wave Config`, `Enemy Separation & Ranged Attack`, `Actor Config Base`, `Rainbow Trail Emitter`, `Player Root`, `Economy & Gold Transfer`, `Player Pot Carry`, `Pot Drain Feedback`, `Faction & Team Affiliation`, `Game Bootstrap`?**
  _High betweenness centrality (0.129) - this node is a cross-community bridge._
- **Why does `PlayerRoot` connect `Player Root` to `CountdownTimer Utility`, `Bootstrap & Scene Management`, `Shop Config`, `Weapon Auto-Fire Targeting`, `Damage System`, `Portal System`, `Enemy Separation & Ranged Attack`, `Actor Config Base`, `Player Pot Carry`, `Weapon Config & Ranged Attack`, `Police Controller`, `Faction & Team Affiliation`, `Player Input Reader`, `Player Aim Controller`, `Player Movement Controller`, `Game Bootstrap`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Why does `TravellerController` connect `Pot Drain System` to `CountdownTimer Utility`, `Bootstrap & Scene Management`, `Damage System`, `Wave Config`, `Actor Config Base`, `Weapon Pickup`, `Projectile Launcher & Death Handling`, `Arena Scene Context`, `Faction & Team Affiliation`, `Game Bootstrap`?**
  _High betweenness centrality (0.101) - this node is a cross-community bridge._
- **What connects `FireControlMode` to the rest of the system?**
  _1 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `CountdownTimer Utility` be split into smaller, more focused modules?**
  _Cohesion score 0.05171907140758154 - nodes in this community are weakly interconnected._
- **Should `Bootstrap & Scene Management` be split into smaller, more focused modules?**
  _Cohesion score 0.08322026232473993 - nodes in this community are weakly interconnected._
- **Should `Shop Config` be split into smaller, more focused modules?**
  _Cohesion score 0.06340326340326341 - nodes in this community are weakly interconnected._