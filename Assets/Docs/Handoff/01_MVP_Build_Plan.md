# Rainbow's End MVP Build Plan

This roadmap is the high-level implementation sequence for the MVP. It is intentionally ordered to ship a thin playable slice first, then expand into the fuller MVP without forcing large rewrites later.

## Core goals
- Build fully custom gameplay code from scratch in Unity 6.
- Use composition-first MonoBehaviour architecture.
- Keep balanceable values in ScriptableObjects.
- Use object pooling for frequently spawned runtime objects.
- Support keyboard and mouse plus controller from the start.
- Keep the MVP landscape-oriented and level-based rather than endless.
- Keep the future full game in mind, especially spells, enemy growth, and presentation polish.
- Use Cinemachine and DOTween as supporting tools for presentation and camera work, not for core simulation rules.

## Phase roadmap
1. Phase 0 - Project Bootstrap
Establish project packages, input setup, renderer direction, folder structure, scenes, bootstrap code, and handoff documents.

2. Phase 1 - Core Architecture Skeleton
Add the reusable foundation: bootstrap composition, shared contracts, config assets, health and damage plumbing, timer and cooldown utilities, and object pooling.

3. Phase 2 - Arena Scene Baseline
Create the first single-screen arena using placeholder primitives, world bounds, cover, concealment, spawn points, fae tree anchor, and HUD shell.

4. Phase 3 - Player Input and Movement
Implement controllable player movement, facing, aim abstraction, push interaction, and debug visualization for core combat readability.

5. Phase 4 - Combat Foundation
Build the weapon pipeline with the bow first, including cooldown, spread, movement penalties, hit resolution, and pooled effects placeholders.

6. Phase 5 - Unicorn and Rainbow Trail
Implement roaming unicorn behavior and the timestamped pooled rainbow trail system that powers harvesting.

7. Phase 6 - Pot and Gold Loop
Implement one pot first: pushing, trail capture, harvest state transitions, gold generation, cancellation on movement, and gold HUD.

8. Phase 7 - First Enemy Pressure Loop
Add Travellers so the player can defend a harvesting pot, lose gold, kill enemies, and collect dropped gold.

9. Phase 8 - Multi-Pot Economy and Pot World Rules
Expand to the full three-pot MVP rule set, add multiplier scaling, pot destruction by unicorn collision, and clearer drain feedback.

10. Phase 9 - Police and Combat Expansion
Add Police ranged pressure, weapon drops, ammo-limited secondary weapons, and health-stage feedback.

11. Phase 10 - Portal System as First Spell
Implement portals using a reusable spell/cooldown framework so the full spell system can grow from the same structure later.

12. Phase 11 - Shop and Upgrade Loop
Add a minimal in-run shop with upgrades that modify runtime stats instead of editing config assets directly, and keep fae tree banking in mind as a required run-outcome mechanic.

13. Phase 12 - Level Orchestration and Run Outcome
Add the level or encounter director system to scale pressure over time, coordinate enemy progression, handle level completion, and support the confirmed game-over and star-rating outcome rules.

14. Phase 13 - MVP Polish and Stability
Focus on usability, readability, feedback, smoke testing, balancing, and a possible non-blocking tilt-shift spike if the gameplay is already stable.

15. Post-MVP Extensions
Military, more spells, status effects, traps, richer weapons, deeper AI, and stronger presentation polish.

## Architecture constraints
- Treat the game loop as level-based, with room for more levels to be added later.
- Treat portals as spells from day one.
- Keep Military outside the MVP critical path.
- Delay tilt-shift polish until gameplay is stable.
- Fae tree banking is confirmed and should be treated as a required later MVP mechanic.
- Player downed time is a gameplay rule and should stay data-driven.
- Use Cinemachine for fixed framing, future camera shake, and transitions when needed.
- Use DOTween for presentation, UI, and feedback tweens only, not for authoritative gameplay state.

## Delivery rule
After each completed implementation step, write a recap covering:
- Outcome
- Approach
- Code review notes
- Verification performed
- Progress state and next step