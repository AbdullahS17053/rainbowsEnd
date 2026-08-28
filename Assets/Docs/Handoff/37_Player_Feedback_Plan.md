# Player Feedback Implementation Plan

Status: Phase 4 complete

## Client request

- Make the player transparent in bushes and tall-grass ConcealmentZones, except while firing or carrying a Pot of Gold.
- Add clear stun feedback using the imported `Stunned.fbx` and `Stand Up.fbx` animations.
- When health reaches zero, stun the player for one second, then grant a two-second recovery period: full health, invulnerability, 35% opacity, no firing, and only pot/upgrade interactions. Picking up a pot restores full visibility early but does not end recovery protection or the firing lockout.

## Approved decisions

- **Concealment is presentation-only.** It does not alter enemy detection, targeting, navigation, or attack selection. A later stealth feature may build on the same player visibility state.
- A player in a `ConcealmentZone` is rendered at **35% opacity**, unless carrying a pot or inside the configurable post-shot reveal window. The default reveal window is **0.25 seconds**; every shot renews it, so held fire keeps the player visible.
- A health depletion enters a **one-second stun**: no movement, aim, fire, pot interaction, portal use, or shop use. The player plays `Stunned.fbx` and shows the existing Epic Toon FX `StunnedCirclingStarsSimple` effect.
- The stun is followed by a **two-second recovery veil**: health is restored to full, all incoming damage and unicorn daze are ignored, and the player is at configurable 35% opacity. Movement and aim return. Firing and portal placement remain disabled; pot pickup/drop and the Q upgrade screen remain available.
- Picking up a pot during the recovery veil makes the player fully visible immediately. It does **not** end the remaining invulnerability, recovery timer, or firing/portal lockout.
- Gameplay timing is authoritative: source clips are adapted to the one-second stun and a short stand-up exit. Do not make the player wait 2.13 seconds for `Stunned.fbx` or 8.30 seconds for `Stand Up.fbx`.
- All timings and opacity are inspector-tunable on the existing `PlayerConfig`; do not add a separate settings asset or runtime tuning menu.

## Existing implementation facts

- `PlayerRoot` is the existing player-state owner. It subscribes to `Health.Depleted`, disables player components while downed, resets health to full on recovery, and already owns the player, movement, aim, pot-carry, inventory, and weapon references.
- `Health.ApplyDamage` is the common damage endpoint reached by player projectiles/contact damage through `IDamageable`. It currently has no invulnerability gate, so recovery invulnerability must be enforced there rather than separately in every enemy.
- `PlayerRoot.ApplyKnockbackAndDaze` is directly called by unicorn collision logic and must also reject recovery-veil daze.
- `WeaponController.ShouldAttemptFire` already blocks fire while carrying a pot. It is the correct shared firing gate to extend for the recovery veil and the successful-shot seam for reveal timing.
- `ShopController` and `PlayerPortalController` currently use `PlayerRoot.IsDisabled`; the shop may stay available during recovery, while portal placement needs an explicit recovery guard.
- `PlayerPotCarryController` already raises/clears `IsCarryingPot` through the normal E interaction. No second pickup path is needed.
- `ConcealmentZone` exposes `Contains(Vector2)` over its trigger `BoxCollider2D`. It is suitable for visual-state checks without adding trigger bookkeeping or changing world geometry.
- `PF_Player` uses shared `Player.controller` and has one `SpriteRenderer` plus multiple child `SkinnedMeshRenderer`s. Alpha must update all visible player renderers with material-property overrides; changing shared materials would affect other characters.
- `Stunned.fbx` is a Humanoid clip of 2.13 seconds and `Stand Up.fbx` is a Humanoid clip of 8.30 seconds. Epic Toon FX already provides `Assets/Epic Toon FX/Prefabs/Combat/Brawling/Stun/StunnedCirclingStarsSimple.prefab`.

## Target state and precedence

| State | Health/damage | Input and interactions | Appearance | Exit |
| --- | --- | --- | --- | --- |
| Normal | Ordinary `Health` behavior | Existing behavior | Fully visible, unless concealed | Enter/leave zone, fire, carry, or health depletion |
| Concealed | Ordinary `Health` behavior | Existing behavior | Configured 35% opacity | Fire reveal, carry a pot, leave all zones, or health depletion |
| Revealed in concealment | Ordinary `Health` behavior | Existing behavior | Fully visible | Reveal timer expires, then concealed if still eligible |
| Carrying a pot | Ordinary `Health` behavior | Existing pot rules; firing already blocked | Fully visible | Drop/destroy pot, then re-evaluate concealment |
| Stunned | Health depleted; no recovery hit can re-enter state | Movement, aim, fire, pot, portal, and Q disabled | Normal concealment/carry precedence; circling stars remain visible | One configurable second expires |
| Recovery veil | Full health; all damage and daze ignored | Movement/aim, E pot action, and Q shop allowed; fire/portals blocked | Configured 35% opacity, unless carrying a pot | Two configurable seconds expire |

Precedence for player body opacity: **carrying pot or shot reveal = 100%; recovery veil = configured recovery opacity; eligible concealment = configured concealed opacity; otherwise = 100%**. The stun stars are a separate cue and remain readable.

## Ownership and guardrails

| Owner | Responsibility | Must not own |
| --- | --- | --- |
| `PlayerRoot` | Player life state, state timers, allowed-action queries, visibility precedence, and recovery transitions. | Enemy AI changes, duplicate pot interaction, material asset mutation. |
| `Health` | One shared invulnerability gate for all `ApplyDamage` callers. | Player animation, timer ownership, UI. |
| `PlayerConfig` / `SO_Player_Default` | Stun duration, recovery duration, concealment/recovery opacity, and post-shot reveal duration. | Scene-specific references or VFX ownership. |
| `WeaponController` | Existing successful-shot flow; consult player firing permission and notify reveal only after a shot actually fires. | Timers, renderer alpha, input-map rewrites. |
| `PlayerPortalController` | Respect player portal permission during recovery. | Duplicate life-state logic. |
| Player-only Animator controller and `PF_Player` | Imported state clips, child stun effect, transitions, and serialized references. | Editing the shared `Player.controller`, Synty source assets, or enemy controllers. |

- Keep AI unchanged. Do not add line-of-sight, aggro-loss, target-reset, or enemy-specific concealment conditions.
- Do not change layers, tags, collider enabling, Rigidbody simulation, pooling, run-loss rules, pot economics, or home-tree behavior as an invulnerability shortcut.
- Do not alter shared renderer materials. Use `MaterialPropertyBlock` (after confirming the relevant shader color property) on player renderers only.
- Do not add a generic state-machine framework, event bus, new input action map, new pool, or new ScriptableObject. The existing player root, health component, config asset, weapon gate, and prefab cover the work.
- Keep the two new animation states player-only. Duplicate the controller into `Assets/_Project/Art/Animators/`; never modify `Player.controller`, which other prefabs may use.
- Preserve `IsDisabled` semantics for true stun/daze only. Add narrow `CanFire` / `CanUsePortals` queries rather than redefining every existing consumer around recovery.

## Phase 0 - Baseline and asset fit

### Goal

Verify the active player setup, all damage/daze callers, material alpha path, and usable source ranges before any changes.

### Steps

1. Confirm through Unity MCP that `MainArena_3D` is active and the editor is idle. Capture Console errors/warnings, the current working-tree checkpoint, `PF_Player`, `SO_Player_Default`, and player/pool references. Preserve unrelated changes.
2. Trace every caller of `Health.ApplyDamage`, `PlayerRoot.ApplyKnockbackAndDaze`, `PlayerRoot.IsDisabled`, and `WeaponController.ShouldAttemptFire`. Record exactly which paths must honor invulnerability, firing lockout, Q access, and portal lockout.
3. Inspect `PF_Player` in prefab mode: identify every player body renderer, its material/shader color property, and the visual/root/head transform suitable for `StunnedCirclingStarsSimple`. Confirm a `MaterialPropertyBlock` can change alpha without modifying a shared material or hiding wings/character meshes.
4. Preview `Stunned.fbx` and `Stand Up.fbx` against the Fairy avatar. Identify the useful stand-up frame range, import it as a player-only non-looping clip/state, and determine its transition duration. The source FBXs remain untouched.
5. Inspect existing `Player.controller` parameters and transitions. Record a player-only duplicate/controller path and confirm no enemy prefab depends on that copy.
6. Run focused baseline EditMode tests for `Health`, `PlayerRoot`, player pot carry, `WeaponController`, and portal/shop gating. Record failures as baseline only; do not weaken tests.

### Phase 0 Recap (2026-08-28)

- Unity MCP baseline: instance `rainbowsEnd@8545bd2c`, Unity `6000.0.67f1`, active scene `Assets/_Project/Scenes/MainArena_3D.unity`, EditMode idle, not compiling, and no test job running.
- The working tree was already dirty in handoff/context and Graphify outputs. Phase 0 made no gameplay, prefab, scene, or asset changes.
- Runtime damage trace: `Health.ApplyDamage` is the shared `IDamageable` endpoint for `WeaponHitUtility`; `PotController` also calls it directly for unicorn and military pot destruction. `Health` raises `Damaged`/`Depleted` after `HealthState` applies damage, and there is currently no invulnerability guard.
- Daze trace: `UnicornController` is the only runtime caller of `PlayerRoot.ApplyKnockbackAndDaze`. `PlayerRoot.IsDisabled` is consumed by portal placement, portal entry, and military/police combat targeting. `WeaponController.ShouldAttemptFire` is private and only called by its own update loop; it currently blocks only while carrying the pot. Q is handled by `ShopController`; E pot interaction is handled by `PlayerPotCarryController`.
- `PF_Player` contains one `VisualRoot` `SpriteRenderer` using `Sprites/Default` with `_Color`, plus multiple `SkinnedMeshRenderer`s using the shared `Universal Render Pipeline/Lit` material with `_BaseColor`. This proves the per-instance `MaterialPropertyBlock` route is viable without changing shared materials. The main animator is on `Fairy Visual` and uses the shared `Player.controller`; the wings animator has no controller. `StunnedCirclingStarsSimple` contains two child transforms and two particle systems and is suitable for a player-local head/visual-root attachment.
- Animation asset data is valid and non-looping: `Stunned.fbx` clip `mixamo.com` is 2.133 seconds at 30 FPS; `Stand Up.fbx` clip `mixamo.com` is 8.267 seconds at 30 FPS; neither has animation events. `Player.controller` currently has only `Speed` and `Fire` parameters with `Idle`, goblin `Walk`, and `Shoot` states. The exact useful stand-up subrange still requires visual preview against the Fairy avatar during Phase 3; source FBXs remain untouched.
- Baseline tests: EditMode completed `119/119 passed`, `0 failed`, `0 skipped`. After the run, the console contained only Unity Test Framework prebuild/result/postbuild messages; no compiler or gameplay error was reported. Before the run, the filtered error/warning console was empty.

### Acceptance criteria

- Damage, daze, fire, portal, pot, shop, renderer, and animation seams are identified from the live project.
- The player-specific material alpha path and FX attachment are proven safe in prefab preview.
- The current tests and Console state are recorded with no feature edits.

### Rollback

No implementation changes exist; retain only the documented baseline.

## Phase 1 - Configured life state and universal recovery protection

### Goal

Replace the old three-second downed wait with the approved one-second stun and two-second recovery veil without introducing an enemy-by-enemy damage workaround.

### Steps

1. Extend the existing `PlayerConfig` and default player asset with inspector-tunable values: `StunDurationSeconds = 1`, `RecoveryVeilDurationSeconds = 2`, `ConcealedOpacity = 0.35`, `RecoveryVeilOpacity = 0.35`, and `ConcealmentRevealDurationSeconds = 0.25`. Preserve the existing serialized downed duration through a safe rename/migration if the field name changes.
2. In `PlayerRoot`, replace the old downed timer flow with the smallest explicit life-state representation that distinguishes normal, stunned, and recovery veil while preserving the existing separate unicorn-daze behavior.
3. On health depletion: enter stun, disable movement/aim/fire/pot/portal/Q, start the configured timer, and keep the current run alive. Do not reset health yet.
4. On stun expiry: restore health to full, enable `Health` invulnerability, enter recovery veil, re-enable movement/aim/pot/Q, keep firing and portals locked, and start the recovery timer.
5. On recovery expiry or object disable: clear invulnerability/timers and restore ordinary permissions. Guard duplicate depletion, cancellation, scene unload, and component disable so no stale invulnerability survives.
6. Add the one shared `Health` invulnerability check to `ApplyDamage`; incoming damage must report zero application and must not invoke `Damaged`/`Depleted` while invulnerable. Add the matching early return to unicorn daze/knockback through `PlayerRoot`.
7. Add narrow public player permission queries consumed by existing callers: weapon fire false during stun/recovery/carry, portal false during stun/recovery, pot true only during normal/recovery, shop true only during normal/recovery. Keep the existing Q pause behavior and E carry behavior otherwise unchanged.

### Automated verification

1. Extend `Health` coverage with one deterministic invulnerability test: damage applies normally, does not apply while guarded, then applies again after the guard clears.
2. Add focused `PlayerRoot` lifecycle tests: depletion enters exactly one second of stun; stun blocks all player actions; recovery restores full health and allows only the approved actions; damage/daze are ignored during recovery; normal damage resumes after expiry; disable/reset clears the state.
3. Extend weapon and portal tests to prove recovery blocks firing/portal placement while allowing the existing pot and shop paths.

### Manual verification

1. Take lethal damage in an open area. Verify one second of complete stun, then full-health recovery for exactly two seconds, followed by ordinary vulnerability.
2. During recovery, walk/aim, press E near a pot, press Q, hold fire, and right-click for a portal. Only movement, aim, E, and Q may succeed.
3. Stand in unicorn contact and receive ranged/contact damage during recovery. Verify no health loss, no daze, and no timer restart.

### Acceptance criteria

- The exact configured 1s stun + 2s recovery behavior works through one shared player/health path.
- All enemy damage and unicorn daze respect recovery protection without per-enemy exceptions.
- Recovery does not leave the player invulnerable, input-locked, or health-depleted after it ends or the object disables.

### Phase 1 Recap (2026-08-28)

- Added inspector-tunable StunDurationSeconds (1.0), RecoveryVeilDurationSeconds (2.0), ConcealedOpacity (0.35), RecoveryVeilOpacity (0.35), and ConcealmentRevealDurationSeconds (0.25) to PlayerConfig and migrated the default asset safely from the old serialized downed-duration field.
- Reworked PlayerRoot into explicit stunned/recovery behavior using the existing timer and component gates: depletion starts stun without resetting health; expiry restores full health and starts invulnerability; recovery expiry and disable clear all protection.
- Added the shared Health.SetInvulnerable / ApplyDamage guard, so every damage caller is protected centrally. Player daze/knockback is rejected during stun/recovery.
- Added narrow permission queries and wired existing systems: firing and portals are blocked during both states; E pot interaction and Q shop access return during recovery; IsDisabled remains true only for stun/daze to preserve existing AI semantics.
- Added HealthTests.Invulnerability_BlocksDamageUntilCleared and PlayerRootTests for depletion, action permissions, recovery protection, and disable cleanup. The EditMode suite passed 122/122 with 0 failures and 0 skipped.
- No scene, prefab, animation, renderer, concealment, or VFX changes were made in this phase. The Console contained only Unity Test Framework prebuild/result/postbuild messages after testing; no compiler or gameplay errors were present.

### Rollback

Restore `PlayerRoot`'s existing downed-timer flow and remove the config fields plus `Health` guard together. No scene data needs recovery.

## Phase 2 - Concealment and recovery presentation

### Goal

Make player transparency readable and deterministic across all existing player renderers, pot carrying, shots, zones, and recovery.

### Steps

1. Extend `PlayerRoot` directly—do not add a one-use presentation framework—to cache the player renderers under `visualRoot`, cache their original color data, and apply alpha with `MaterialPropertyBlock` only to the player instance.
2. Resolve ConcealmentZone membership with the existing `Contains` API. Cache active zones at setup and re-evaluate the player body position without adding trigger events, tags, layers, or scene markers.
3. Add a player-local post-shot reveal timer. `WeaponController` calls it only after `FireShot` succeeds, so empty inputs, cooldown rejection, carrying a pot, and recovery lockout cannot reveal the player.
4. Apply the approved precedence every frame: carrying pot or reveal timer wins at full opacity; recovery veil uses configurable 35%; otherwise an eligible concealment zone uses configurable 35%; ordinary state uses full opacity.
5. On E pickup during recovery, refresh immediately to full opacity while retaining the recovery timer, health invulnerability, and fire/portal lockout. On drop, expiry, despawn/disable, and scene teardown, return to the correct current alpha and clear property overrides safely.
6. Keep the effect visual-only. Do not pass concealment state to enemy controllers, target-selection utilities, NavMesh, projectiles, or wave systems.

### Automated verification

1. Add focused pure/state tests for opacity precedence: normal, concealed, revealed-shot timer, carrying, recovery, recovery + carry, drop during recovery, and timer expiry.
2. Add a `WeaponController` test proving only a successful shot invokes the reveal path and recovery/carry cannot invoke it.
3. Test that concealment state has no change to enemy targeting APIs or damage results outside recovery invulnerability.

### Manual verification

1. Walk through every bush/tall-grass zone and its edge. Verify all player meshes fade to 35% together, with no shared-material changes on civilians, police, or military.
2. Fire once, hold fire, stop firing, pick up/drop a pot, and leave/re-enter zones. Verify exact visual precedence and the configurable reveal window.
3. Die/recover inside and outside concealment. Verify recovery opacity is readable, pot pickup returns full visibility immediately, and ordinary concealment resumes after recovery when appropriate.

### Acceptance criteria

- Transparency is consistent across the complete player visual, does not mutate shared assets, and always resolves through the approved precedence.
- Concealment remains purely visual; enemy behavior is unchanged.
- All opacity/reveal tuning values can be changed in SO_Player_Default without code changes.

### Rollback

Remove the renderer override/reveal logic from `PlayerRoot` and new config fields. Clear property blocks in the same change so the player returns fully visible.

### Phase 2 Recap (2026-08-28)

- Added player-local presentation state to PlayerRoot: active child renderers are cached under VisualRoot, active ConcealmentZone instances are cached at setup, and alpha is applied through per-renderer MaterialPropertyBlock overrides.
- Because the live Fairy character uses opaque URP/Lit materials, runtime material instances are created only for the active player renderers and switched to transparent blending. Shared Polygon Fantasy Kingdom materials remain unchanged.
- Implemented the approved precedence: carrying a pot or the post-shot reveal window forces full visibility; recovery and concealment use their respective configurable opacity; ordinary state restores full visibility. Pot pickup/drop calls refresh the presentation immediately.
- WeaponController notifies PlayerRoot only after the existing successful LaunchVolley path, so rejected fire attempts do not reveal the player. No enemy targeting, navigation, damage, or wave logic was changed.
- Added PlayerPresentationTests.Presentation_UsesConcealmentRevealAndRecoveryPrecedence, including a shared-material immutability assertion. Focused Phase 2 tests passed 2/2; the full EditMode suite passed 123/123 with 0 failures and 0 skipped.
- Play Mode smoke check found three active player renderers; all reported 0.35 alpha inside a live concealment zone, and the Fairy URP/Lit renderers reported runtime transparent materials with no asset path. Play Mode was stopped cleanly; the Console contained only Unity Test Framework result messages.

## Phase 3 - Player-only stun animation and visual designator

### Goal

Integrate the imported Humanoid clips and existing stars with reliable, pool-safe-style reset behavior while keeping shared controller assets unchanged.

### Steps

1. Duplicate `Player.controller` to a project-owned player-feedback controller and assign it only to `PF_Player`. Keep existing locomotion and fire parameters/transitions unchanged.
2. Add player-only `Stunned` and `StandUp` parameters/states. Enter `Stunned` exactly once per depletion; disable self-transition re-entry so the clip cannot restart every frame. Exit through a short `StandUp` transition when recovery begins, then return cleanly to locomotion.
3. Configure the state clip speed so `Stunned` fits one second and the `Stand Up` state is a short recovery transition. The current implementation accelerates the imported clips in the player-only controller; it does not edit the source FBXs or make movement wait for the full 8.30-second clip.
4. Add `StunnedCirclingStarsSimple` as an inactive player-owned visual child at the confirmed head/visual-root transform. Enable it for stun only, disable it at recovery start, and explicitly clear it in disable/reset paths. The player is singleton, so do not create a VFX pool.
5. Keep stars independent of body alpha so the cue stays visible in a ConcealmentZone. Verify the effect is not blocked by renderer material overrides or sorting.
6. Update `CharacterAnimationBridge` only if a minimal public trigger/bool passthrough is necessary. Do not replace it, modify enemy animator controllers, or add a generic animation command layer.

### Automated verification

1. Add a compact player animation-state test or animator-parameter assertion: depletion sets stun, recovery triggers stand-up, and reset clears both states and the stars reference/active state.
2. Run player, health, weapon, portal, pot-carry, and existing animation bridge tests together.

### Manual verification

1. Trigger lethal damage repeatedly, including near the end of a wave and while concealed/carrying. Verify no frozen first frame, repeated restart, sliding stand-up, or lingering star effect.
2. Verify `Stunned` is readable for the exact one-second game rule, the short stand-up transition is visible, and movement returns at recovery start.
3. Verify every other character still uses its assigned controller and remains unchanged.

### Acceptance criteria

- Stun and stand-up presentation are visible, timely, and player-only.
- The circling-star cue is unmistakable and never persists after recovery, disable, or new run.
- Shared `Player.controller`, enemy controllers, source FBXs, and package assets remain unchanged.

### Phase 3 Recap (2026-08-28)

- Duplicated `Assets/_Project/Art/Animators/Player.controller` to the player-only `Assets/_Project/Art/Animators/PlayerFeedback.controller`; the shared controller remains unchanged.
- Added `Stun` and `StandUp` trigger parameters, `Stunned` and `StandUp` states, and the player-only AnyState -> Stunned -> StandUp -> Idle flow. The imported `Stunned.fbx` clip is speed-adapted to the one-second gameplay stun; the full imported `Stand Up.fbx` clip is compressed to a short 0.75-second exit state with an exit-time transition, so recovery does not wait 8.27 seconds.
- Updated `PlayerRoot` to trigger each animation once per depletion/recovery transition, reset both triggers on disable/new enable, and manage the existing `StunnedCirclingStarsSimple` effect through an inactive `StunFeedback` child. The stars are attached outside `VisualRoot`, so the Phase 2 body-opacity overrides do not fade the cue.
- Assigned the new controller and serialized feedback references on `Assets/_Project/Prefabs/PF_Player.prefab`. No `CharacterAnimationBridge`, enemy animator, source FBX, or Epic Toon FX asset was modified.
- Added `PlayerFeedbackAnimationTests` for controller states, trigger conditions, prefab references, and reset-ready stars. Focused Phase 3 tests passed `4/4`; the full EditMode suite passed `125/125` with `0` failures and `0` skipped.
- Live Unity MCP verification captured the player in `Stunned` with stars active, then in `StandUp` with stars inactive and recovery active. Play Mode was stopped cleanly. The Console contained only the existing Unity Test Framework prebuild/result/postbuild messages; no compiler or gameplay errors were introduced.

### Rollback

Reassign `PF_Player` to the existing controller and remove the child VFX/animation parameters. The state and concealment work remain independently reversible.

## Phase 4 - Regression, tuning, docs, and graph refresh

### Goal

Prove the final behavior in play, preserve tuning ownership, and update future-agent context.

### Steps

1. Run all focused tests from Phases 1–3, then the full EditMode suite. Investigate actual regressions; do not suppress or rewrite unrelated tests.
2. Clear and re-read the Unity Console after compilation and each smoke pass. Resolve new errors/warnings from this feature before acceptance.
3. Perform a full manual smoke pass: open/covered terrain, firing/held fire, carrying/dropping pot, stun/recovery, Q/E/right-click permissions, incoming military/police/unicorn pressure, and state expiry/early pot pickup.
4. Verify inspector tuning in `SO_Player_Default`: change each duration, alpha, and reveal value to a visibly distinct temporary value, run the scenario, then restore approved defaults of 1s, 2s, 0.35, 0.35, and 0.25s.
5. Update this plan with phase recaps, exact asset paths, test counts, Console outcome, known limitations, and manual-test result. Update `04_Context.md` only with confirmed current-state facts.
6. Run incremental Graphify update after code/docs changes. Record the graph size/health result and state plainly that Unity serialized assets are not represented as standalone code-graph nodes.

### Acceptance criteria

- Focused and full EditMode tests pass with no introduced Console errors or warnings.
- Client-approved visible behavior works in the active arena and every value is inspector-tunable.
- Handoff documentation and project graph describe the implemented state without claiming future work is complete.

### Rollback

Revert only the player-feedback code, prefab/controller, config, test, and documentation changes as one feature slice. Keep unrelated working-tree changes intact.

### Phase 4 Recap (2026-08-28)

- Focused player feedback/recovery tests passed `6/6` with `0` failures and `0` skipped. The final full EditMode suite passed `125/125` with `0` failures and `0` skipped.
- Unity MCP final state: instance `rainbowsEnd@8545bd2c`, active scene `Assets/_Project/Scenes/MainArena_3D.unity`, Play Mode stopped, editor idle, and not compiling.
- The user completed the manual smoke pass and reported the visual behavior as working, including concealment, firing/reveal, pot carry, stun/recovery, permissions, and expiry/early pickup scenarios.
- Inspector tuning was verified by applying distinct temporary values to all five `SO_Player_Default` fields and restoring the approved defaults: `1`, `2`, `0.35`, `0.35`, and `0.25`.
- The post-test Console contained only the existing Unity Test Framework prebuild/result/postbuild messages and the normal test-results save log; no compiler or gameplay error was introduced.
- Incremental Graphify refresh completed in code-only mode because this environment has no LLM API key for the changed image/document assets. Final graph: `4,570` nodes, `8,165` edges, and `302` communities; the regenerated `graph.html` and `GRAPH_REPORT.md` are current. A read-only integrity check found `0` missing endpoints and `0` self-loops. Graphify also reported its existing shared-C#-namespace node-collision warnings; these are graph-model limitations, not Unity compile/runtime errors. Unity serialized assets such as prefabs, ScriptableObjects, and Animator controllers are not standalone code-graph nodes.
## Explicitly deferred

- Enemy AI awareness, target loss, aggro reset, or firing rules based on player concealment.
- New stealth UI, detection meter, audio cues, post-processing, shaders, material copies, or a global visibility framework.
- New input bindings, respawn/checkpoint system, game-over rule changes, or pot-economy changes.
- Changes to police, military, civilian, unicorn, shared player, or Synty source Animator controllers.
