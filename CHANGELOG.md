# EchoProtocol — Changelog & Technical Rationale

This document serves as the single source of truth for all code changes, bug fixes, refactors, and future architectural additions across the **EchoProtocol** codebase. Every entry details **what was changed**, **why it was changed (root cause / motivation)**, and **the resulting behavioral impact**.

---

## Table of Contents
1. [Change Philosophy & Architecture](#change-philosophy--architecture)
2. [Completed Changes](#completed-changes)
   - [EnemyAI.cs](#1-assets-scripts-enemyaics)
   - [PlayerShooting.cs](#2-assets-scripts-playershootingcs)
3. [Planned / Future Changes Roadmap](#planned--future-changes-roadmap)
4. [Contribution & Maintenance Standard](#contribution--maintenance-standard)

---

## Change Philosophy & Architecture

EchoProtocol is built on **tactical stealth and acoustic awareness**. Changes made to the codebase adhere to three core design rules:
1. **Physical Sound Propagation**: Sound must always have a visual and physical representation (`SoundRipple`) and trigger predictable AI investigation.
2. **Stealth Fairness**: AI must obey sensory limitations (field-of-view cones, line of sight, acoustic ranges). Wallhacks or omnidirectional vision undermine stealth gameplay.
3. **Defensive Programming**: Unity components must handle missing Inspector references gracefully (`Camera.main`, `FindWithTag`) and avoid hardcoded spatial assumptions (e.g., assuming ground height is always $Y = 0$).

---

## Completed Changes

### 1. `Assets/Scripts/EnemyAI.cs`

#### A. Line of Sight Obstacle Raycast Bug
* **What was changed**: Replaced the hardcoded `100f` max raycast distance in `HasLineOfSightToPlayer()` with the exact distance to the player (`directionToPlayer.magnitude`), and normalized the direction vector.
* **Why (Root Cause)**: The previous code passed an unnormalized vector with a hardcoded `100f` distance to `Physics.Raycast(...)`. If any obstacle (like a wall) existed **behind** the player up to 100 meters away, the ray passed through the player (who is not on `obstacleMask`) and struck the wall. Because an obstacle hit was detected, the method returned `false`, blinding the enemy even if the player stood right in front of them in plain view.
* **Impact**: Enemies can now reliably spot the player in rooms and corridors regardless of background geometry.

#### B. 360-Degree Vision Cone & Stealth Detection
* **What was changed**: Introduced `fieldOfViewAngle` (default `110f` degrees) in `CheckForPlayerSight()`. Vision now requires:
  $$\text{Angle}(\text{transform.forward}, \text{directionToPlayer}) \le \frac{\text{fieldOfViewAngle}}{2}$$
  with a close-proximity fallback of $2.5\text{ m}$ (for touching/melee distance).
* **Why (Root Cause)**: Previously, only distance and line-of-sight were checked without verifying whether the enemy was actually facing the player. Enemies had "eyes on the back of their heads," instantly detecting players sneaking up from behind.
* **Impact**: Players can now crouch and sneak directly behind enemies without triggering immediate alerts.

#### C. Removal of Wallhack Pathfinding
* **What was changed**: Added `lastKnownPlayerPosition`. In `EngagePlayer()`, when line-of-sight is lost, the NavMeshAgent is directed to `lastKnownPlayerPosition` instead of `player.position`.
* **Why (Root Cause)**: When sight was lost, the enemy continued updating `agent.SetDestination(player.position)` every single frame for 6 seconds. This allowed the AI to dynamically follow the player's real-time turns through solid walls.
* **Impact**: Enemies now run to the last spot they saw the player. If the player turns a corner and hides, the enemy investigates the corner rather than pre-aiming through walls.

#### D. Healing Coroutine Lifecycle & "Ghost" Coroutine Elimination
* **What was changed**:
  1. Stored a `Coroutine healingCoroutine` reference and implemented `StopHealingCoroutine()`.
  2. Handled `EnemyState.Healing` inside `TakeDamage()`.
  3. Made `HealOverTime()` check line-of-sight every frame (`yield return null`) while accumulating a 1-second tick timer.
* **Why (Root Cause)**:
  1. The coroutine previously paused execution with `yield return new WaitForSeconds(1f)`. During this 1-second pause, the enemy was completely blind and unresponsive to the player.
  2. When taking damage below `retreatThreshold`, the coroutine was never stopped with `StopCoroutine()`. The old coroutine continued running in the background ("ghost coroutine"), eventually overriding health and firing alarms.
  3. `TakeDamage()` omitted `EnemyState.Healing` in its transition check. If an enemy healed above 30 HP and was shot, it completely ignored the damage.
* **Impact**: Enemies immediately cancel healing and fight back when shot; no rogue background coroutines run.

#### E. Retreat-to-Healing Jitter & Freezing Fix
* **What was changed**: In `EvadePlayer()`, added a condition requiring the agent to be close to its flee destination (`remainingDistance <= stoppingDistance + 1.0f`) in addition to breaking line of sight before transitioning to `Healing`.
* **Why (Root Cause)**: Previously, `EvadePlayer()` checked `if (!HasLineOfSightToPlayer())`. The instant the enemy ran behind a pillar or corner for a fraction of a second, it stopped dead in its tracks to heal. When the player peeked, it fled again, causing rapid jittering and freezing in doorways.
* **Impact**: Enemies now run fully into cover before attempting to heal.

#### F. Alarm Beacon Clustering Resolution
* **What was changed**: In `TrackAlarmBeacon()`, added an arrival check (`remainingDistance <= stoppingDistance + 1.5f`). Once an ally reaches the alerting enemy, it clears `activeAlarmBeacon` and transitions to `SearchRandomly`.
* **Why (Root Cause)**: If the alerting enemy survived, incoming allies stayed locked in `InvestigateAlarm` forever, bumping into each other indefinitely without ever searching the area.
* **Impact**: Allied backup spreads out and actively searches the surrounding area upon arrival.

#### G. Inspector Fallbacks & Damage Occlusion
* **What was changed**:
  1. Added `GameObject.FindWithTag("Player")` fallback in `Start()`.
  2. Added an obstacle raycast in `ShootAtPlayer()` before applying damage.
  3. Updated deprecated Unity 6 `FindObjectsByType` call to `FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude)`.
* **Why (Root Cause)**: Enemies spawned dynamically had null `player` references. Furthermore, enemy hitscan damage was applied directly through walls without verifying bullet clearance.
* **Impact**: Eliminates runtime null-reference crashes and wall-penetrating enemy gunfire.

---

### 2. `Assets/Scripts/PlayerShooting.cs`

#### A. Fire Rate Cooldown Limit
* **What was changed**: Added `fireRate = 0.25f` and `nextFireTime` gating in `Update()`.
* **Why (Root Cause)**: Shooting was triggered solely by `Input.GetMouseButtonDown(0)` with zero cooldown. Players could spam-click or use autoclickers to fire dozens of rounds in fractions of a second, melting enemies and bypassing alarm systems.
* **Impact**: Establishes a controlled, tactical semi-automatic fire rate.

#### B. Dynamic Ground Snapping for Sound Ripples
* **What was changed**: In `SpawnSoundRipple()`, replaced the hardcoded `floorPos = new Vector3(x, 0.01f, z)` with a downward raycast:
  ```csharp
  if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 5f, hitLayers, QueryTriggerInteraction.Ignore))
  {
      spawnPos = groundHit.point + Vector3.up * 0.01f;
  }
  ```
* **Why (Root Cause)**: Hardcoding $Y = 0.01\text{ m}$ meant sound ripples spawned at the world origin height. On stairs, catwalks, ramps, or upper floors, ripples spawned in mid-air or under the floor geometry.
* **Impact**: Ripples now conform accurately to any elevation, slope, or terrain geometry.

#### C. Hitbox & Child Collider Support
* **What was changed**: Replaced `hit.transform.CompareTag("Enemy")` with `hit.collider.GetComponentInParent<EnemyAI>()`.
* **Why (Root Cause)**: Standard humanoid character models and ragdolls place colliders on child bones (Head, Torso, Limbs). `hit.transform` pointed to the child object, which lacked the "Enemy" tag and the `EnemyAI` script, causing valid shots to register as misses.
* **Impact**: Hits on any part of the enemy's body or collider hierarchy reliably register damage.

#### D. Trigger Volume Penetration (`QueryTriggerInteraction.Ignore`)
* **What was changed**: Added `QueryTriggerInteraction.Ignore` to both `Physics.Raycast` (shooting) and `Physics.OverlapSphere` (acoustic alerts).
* **Why (Root Cause)**: Invisible level triggers (zone boundaries, sound ripple colliders, checkpoints) intercepted bullet raycasts, blocking shots from hitting enemies behind them.
* **Impact**: Bullets pass through trigger volumes and only collide with solid physical geometry and characters.

#### E. Acoustic Deduplication via `HashSet<EnemyAI>`
* **What was changed**: In `AlertNearbyEnemies()`, queried colliders are processed through a `HashSet<EnemyAI>`:
  ```csharp
  HashSet<EnemyAI> alertedEnemies = new HashSet<EnemyAI>();
  foreach (var hitCollider in hitColliders)
  {
      EnemyAI enemy = hitCollider.GetComponentInParent<EnemyAI>();
      if (enemy != null && !alertedEnemies.Contains(enemy))
      {
          alertedEnemies.Add(enemy);
          enemy.InvestigateSound(transform.position);
      }
  }
  ```
* **Why (Root Cause)**: An enemy model with multiple colliders (e.g. CharacterController + CapsuleCollider + hitboxes) caused `InvestigateSound()` to fire multiple times redundantly within the same frame.
* **Impact**: Clean, single acoustic notifications per enemy per gunshot.

#### F. Camera Null Fallback & Range Rebalance
* **What was changed**: Added `Camera.main` fallback in `Start()` if `playerCamera` is unassigned; updated default weapon range from `20f` to `50f`.
* **Why (Root Cause)**: Missing inspector links crashed the shooting routine; a 20-meter range was shorter than the 25-meter sound ripple, causing situations where enemies heard shots they could not return fire on.
* **Impact**: Robust initialization and aligned combat distances.

---

### 3. Dynamic Surface Bullet Holes & Impact Effects

#### A. Physical `MeshCollider` Removal (`Assets/Prefab/Bullet Hole.prefab`)
* **What was changed**: Removed the solid `MeshCollider` component from `Bullet Hole.prefab` and replaced it with the `BulletHole` lifecycle script.
* **Why (Root Cause)**: The bullet hole prefab previously had an active, solid `MeshCollider` (`m_IsTrigger: 0`). When placed in the world, the flat 2D sticker acted as a solid physical obstacle: it intercepted subsequent bullets from reaching targets behind it, and blocked player/AI movement.
* **Impact**: Bullet holes are now purely visual/cosmetic decals with zero physics interference.

#### B. Material Alpha Transparency (`Assets/Material/BulletMat.mat`)
* **What was changed**: Assigned `Bullet Hole 1.png` texture (`guid: 9ccad5df4a6564027bdc5c4bc579b839`) to `_BaseMap` and switched the URP material surface to `Transparent` (`_Surface: 1`, `_Blend: 0`, `_ZWrite: 0`, `RenderType: Transparent`).
* **Why (Root Cause)**: The material had no texture assigned and was set to `Opaque`, causing bullet holes to render as solid white or magenta blocks.
* **Impact**: Bullet impacts render with clean, crisp alpha transparency.

#### C. Auto-Fade & Destruction Lifecycle (`Assets/Scripts/BulletHole.cs`)
* **What was changed**: Created `BulletHole.cs` with configurable `lifetime = 15f` and `fadeDuration = 3f`.
* **Why (Root Cause)**: Without a lifetime manager, bullet holes accumulate in memory indefinitely, causing draw-call bottlenecks and frame drops.
* **Impact**: Bullet holes persist for 12 seconds, smoothly fade out transparency over 3 seconds, and automatically self-destroy without visual popping.

#### D. Surface Normal Alignment & Z-Fighting Prevention (`PlayerShooting.cs` & `EnemyAI.cs`)
* **What was changed**: Added `SpawnBulletHole(RaycastHit hit)` to both shooting systems:
  ```csharp
  Vector3 spawnPos = hit.point + hit.normal * 0.002f;
  Quaternion spawnRot = Quaternion.LookRotation(-hit.normal);
  GameObject hole = Instantiate(bulletHolePrefab, spawnPos, spawnRot, hit.transform);
  hole.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
  ```
* **Why (Root Cause)**: Raw quads flush with wall meshes cause Z-fighting flickering. Also, non-rotated textures look visibly repetitive.
* **Impact**: Bullet holes spawn on any wall, floor, or ceiling, dynamically oriented to the surface angle with randomized rotation and zero flickering.

---

### 4. Dynamic Weapon Muzzle Flash (Player & Enemy)

#### A. Stylized Starburst Texture (`Assets/Sprites/MuzzleFlash.png`)
* **What was changed**: Created a high-resolution 512x512 transparent starburst texture with an intense white-hot core transitioning to golden-yellow and orange radial spikes.
* **Why (Root Cause)**: Weapons previously fired without visual muzzle feedback, diminishing tactical feel and visual confirmation of gunshots.
* **Impact**: High-contrast, stylized weapon discharge VFX.

#### B. Emissive URP Material (`Assets/Material/MuzzleFlashMat.mat`)
* **What was changed**: Configured a URP transparent material with HDR emission (`_EmissionColor: (2.5, 1.8, 0.6)`) and double-sided rendering (`_Cull: 0`).
* **Why (Root Cause)**: Standard unlit or opaque shaders either look flat or render with black boundary boxes in modern render pipelines.
* **Impact**: Emits a bright luminous glow that integrates with post-processing bloom.

#### C. Flash & Light Pulse Controller (`Assets/Scripts/MuzzleFlash.cs`)
* **What was changed**: Created `MuzzleFlash.cs` managing flash duration (`0.05s`), randomized roll rotation around the barrel axis (`0–360°`), and a warm dynamic `Point Light` (intensity `2.5`, range `3.5m`).
* **Why (Root Cause)**: A muzzle flash must illuminate the weapon, hands, and immediate surroundings, but must vanish rapidly (within 2–3 frames) without lingering or looking identical on repeated shots.
* **Impact**: Snappy visual feedback with realistic local lighting pulses.

#### D. Zero-Garbage-Collection (GC) Integration (`PlayerShooting.cs` & `EnemyAI.cs`)
* **What was changed**: Attached the `MuzzleFlash` GameObject directly to the tip of `SilencedPistol` on both the player camera and enemy prefab. Added `TriggerFlash()` calls in both shooting methods.
* **Why (Root Cause)**: Instantiating and destroying muzzle flash prefabs on every shot causes memory fragmentation and garbage collection frame spikes during high fire rates.
* **Impact**: Zero runtime allocation overhead; the flash moves smoothly with weapon movement and recoil.

---

### 5. In-Scene Menu System (Rolled Back)
* **Status**: **Rolled back** per user request.
* **Why (Root Cause)**: The in-scene `MenuManager` script was initializing in paused state (`Time.timeScale = 0f` and `isGamePaused = true`), waiting for UI panels that were not yet wired in the Unity hierarchy. This froze gameplay execution (input, physics, camera look).
* **Impact**: 
  - Removed `MenuManager.cs` and removed `MenuManager` GameObject from `SampleScene.unity`.
  - Restored default cursor locking in `PlayerMovement.Start()`.
  - Removed pause guards from `PlayerMovement.Update()` and `PlayerShooting.Update()`.
  - Full player locomotion, weapon discharge, muzzle flash, and enemy AI are immediately active upon scene launch.
### 5. Translucent In-Scene Main Menu & Pause Menu System

#### A. Minimalist State Controller (`Assets/Scripts/MenuManager.cs`)
* **What was changed**: Created a clean, easily-explainable ~110 line `MenuManager.cs` managing transitions between `MainMenu`, `Gameplay`, and `Paused`.
  - Toggles time freeze (`Time.timeScale = 0f` vs `1f`).
  - Handles mouse cursor capture (`Cursor.lockState = CursorLockMode.None` vs `CursorLockMode.Locked`).
  - Toggles visibility of `mainMenuPanel` and `pauseMenuPanel`.
  - Supports automatic button event wiring (`WireButtons()`) so button clicks function out-of-the-box even without manual Inspector wiring.
  - Implements defensive initialization: if `mainMenuPanel` is missing or unassigned, it immediately defaults to gameplay without freezing `Time.timeScale`.
* **Why (Root Cause)**: Previous procedural UI generation code was overly complex and unintuitive to explain, while the un-wired script previously caused a paused-game freeze on startup.
* **Impact**: Intuitive, easy-to-explain controller that never freezes the game.

#### B. In-Scene Translucent Canvas Hierarchy (`SampleScene.unity` & `MenuCanvas.prefab`)
* **What was changed**: Integrated a full `MenuCanvas` and `EventSystem` directly into `SampleScene.unity` and saved a standalone prefab at `Assets/Prefab/MenuCanvas.prefab`:
  - **Screen Space - Overlay Canvas** with high-DPI `CanvasScaler` (1920x1080).
  - **`MainMenuPanel`**: Dark translucent overlay (`#0D121F` at 88% opacity) displaying "ECHO PROTOCOL" title, "ACOUSTIC STEALTH OPERATION" subtitle, "START MISSION" button, "QUIT GAME" button, and a controls cheat sheet.
  - **`PauseMenuPanel`**: Dark translucent overlay (`#0D121F` at 88% opacity) displaying "OPERATION PAUSED", "RESUME", "RESTART MISSION", "MAIN MENU", and "QUIT GAME" buttons.
  - **`EventSystem`**: Configured with `StandaloneInputModule` for input handling.
* **Why (Root Cause)**: Standard Unity UI works via scene GameObjects and prefabs rather than runtime code generation. Pre-populating the hierarchy ensures the UI renders immediately with zero setup required by the user.
* **Impact**: Crisp, tactile, translucent UI overlay that renders the paused 3D level in the background.

#### C. Input Leakage Prevention (`PlayerMovement.cs` & `PlayerShooting.cs`)
* **What was changed**:
  - Gated `PlayerMovement.Update()` and `PlayerShooting.Update()` behind `if (MenuManager.isGamePaused) return;`.
  - Configured `PlayerMovement.Start()` to only lock the cursor when not launching into a menu (`if (!MenuManager.isGamePaused)`).
* **Why (Root Cause)**: Clicking UI buttons previously triggered weapon discharge or jerked the first-person camera.
* **Impact**: Clean pointer interactions when menus are active; instant seamless FPS control when resumed.

---

### 6. Tactical Gameplay HUD System

#### A. HUD Controller (`Assets/Scripts/PlayerHUD.cs`)
* **What was changed**: Created a clean, decoupled `PlayerHUD.cs` managing all real-time in-game visual feedback:
  - **Dynamic Health Bar**: `Image` fill amount ($0.0 \to 1.0$) coupled with smooth color transitions: Healthy Green/Cyan (`#1FD98C`), Warning Amber (`#FFCC26`), and Critical Red (`#F23333`).
  - **Health Numerical Readout**: Real-time `${current} / ${max} HP` text.
  - **Stealth & Acoustic Status Indicator**: Monitors player velocity and sprint keys to display `ACOUSTIC: SILENT`, `ACOUSTIC: WALKING (STEALTH)`, or `ACOUSTIC: SPRINTING (NOISY)`.
  - **Damage Flash / Red Vignette**: Fullscreen subtle red flash triggered automatically when health decreases, smoothly fading out over $0.33\text{s}$.
  - **Aim Reticle / Crosshair**: Centered subtle dot reticle providing precise targeting without cluttering vision.
* **Why (Root Cause)**: The prototype lacked health indicators, crosshairs, and stealth awareness, leaving the player blind to their survival status and aim orientation.
* **Impact**: Complete tactical situational awareness and instant combat feedback.

#### B. Component Exposure (`PlayerHealth.cs`)
* **What was changed**: Added `public int CurrentHealth => currentHealth;` property.
* **Why (Root Cause)**: `currentHealth` was private with no accessor, preventing external UI scripts from reading the player's health state cleanly.
* **Impact**: Read-only exposure without violating encapsulation.

#### C. Menu & HUD Lifecycle Coordination (`MenuManager.cs`)
* **What was changed**: Added `hudPanel` GameObject reference to `MenuManager.cs`. Coordinated `hudPanel.SetActive(true)` on `StartGame()` and `ResumeGame()`, and `hudPanel.SetActive(false)` during `ShowMainMenu()` and `PauseGame()`.
* **Why (Root Cause)**: The HUD should not overlap main menu screens or pause menus.
* **Impact**: Clean UI state transitions: menus and gameplay HUD are never displayed concurrently.

---

## Planned / Future Changes Roadmap

The following improvements are queued for implementation in future milestones:

| Component | Planned Change | Rationale / Problem Addressed |
| :--- | :--- | :--- |
| **`SoundRipple.cs`** | Acoustic Occlusion via Obstacle Raycast | Sound currently passes straight through thick concrete walls unimpeded. Adding occlusion dampening will make indoor stealth more realistic. |
| **`PlayerMovement.cs`** | Crouch Locomotion & Sneak Speed | Provide a dedicated crouch state (`Ctrl`) with reduced speed and zero ripple radius for creeping close to guards. |
| **`PlayerShooting.cs`** | Ammo, Reloading, & Magazine HUD | Add limited magazine capacity (e.g., 7 rounds) and reload animations/timers to demand ammo economy. |
| **UI & HUD** | Stealth Detection Indicator & Health Bar | Provide visual player feedback on current health, alert status of nearby guards, and sound meter. |
| **`EnemyAI.cs`** | Stun / Flinch Animations | Integrate damage reaction animations so enemies flinch when shot rather than continuing uninterrupted. |
| **Audio** | Sound Occlusion / Low-Pass Filtering | Apply low-pass audio filters when listening to gunshots or ripples through walls. |

---

## Contribution & Maintenance Standard

When making future changes to any script in this project:
1. **Always record the change here**: Add a subsection under [Completed Changes](#completed-changes) with the file path.
2. **Follow the What / Why / Impact format**:
   - **What was changed**: Explicit methods, variables, or structures modified.
   - **Why (Root Cause)**: The bug, limitation, or design flaw that motivated the edit.
   - **Impact**: How gameplay, performance, or stability behaves differently now.
3. **Keep roadmap updated**: Move completed items from the roadmap to completed changes.

