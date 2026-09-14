# ECHO PROTOCOL — Tactical Acoustic Infiltration Prototype

> **Unity 6 / WebGL Prototype**  
> **Playable WebGL Demo**: [https://ayushsaini437.github.io/EchoProtocol_Prototype/](https://ayushsaini437.github.io/EchoProtocol_Prototype/)

---

## 🎯 Executive Overview

**Echo Protocol** is a fast-paced, tactical first-person stealth prototype where **sound is physical, visible, and lethal**. Set in a hostile industrial compound, you play as a lone operative tasked with neutralizing an alert hostile force.

Every action in the environment produces expanding **acoustic shockwaves**:
* **Sprinting** casts periodic audio ripples that attract patrolling sentries.
* **Firing your weapon** discharges a wide acoustic wave that unmasks your position.
* **Jumping & landing** triggers a heavy impact thud that reverberates across the ground.
* **Stealth takedowns** reward huge score multipliers before hostiles sound the radio alarm.

---

## 🎮 Controls

| Action | Input | Acoustic Signature |
| :--- | :--- | :--- |
| **Move** | `W` `A` `S` `D` | Silent / Low Profile |
| **Sprint** | `Left Shift` + Movement | Periodic $10\text{m}$ Sound Ripples |
| **Jump** | `Space` | Silent in air; **$14\text{m}$ Impact Thud** on landing |
| **Aim / Look** | `Mouse` | Smooth calibrated first-person look |
| **Fire Silenced Pistol** | `Left Mouse Button` | Direct hitscan; creates gunshot ripple |
| **Pause / Resume** | `Escape` | Toggles translucent tactical pause menu |

---

## 🧠 Question #1: Build a Playable Prototype — Design Document

### 1. The Pitch
* **What is it?** A tactical first-person infiltration shooter where sound propagation is visually weaponized.
* **Who is it for?** Fans of tactical stealth and high-stakes action (*Metal Gear Solid*, *SUPERHOT*, *Splinter Cell*).
* **Why would someone play it?** The tension between speed and silence. Moving fast gives you tactical momentum, but the expanding acoustic ripples give away your exact location to enemies who will coordinate, flank, and radio for reinforcements.

### 2. Core Loop & First Session
* **The First Few Minutes**: The player spawns outside an enemy facility. Hostiles are scattered across sectors, occluded behind cover. The player spots an enemy, evaluates line of sight, executes a clean silenced headshot for $+750\text{ PTS}$, then sprints to cover—only to realize the sprint acoustic ripples just alerted a two-man patrol around the corner.
* **The Day-1 Hook**: High-skill rating system ($S$, $A$, $B$, $C$ Rank) based on clear time, health preserved, and stealth kill ratio. Achieving an **S-Rank (Ghost Agent)** requires precision routing, acoustic baiting, and zero alarms.

### 3. Progression & Metagame
* **Day-1 to Day-7 Retention**:
  * **Operative Loadouts**: Unlockable tactical gear (e.g., sound-dampening boots to suppress sprint noise, subsonic rounds, acoustic decoy grenades).
  * **Daily Contracts**: Procedurally populated infiltration scenarios with varying modifier conditions (e.g., "Laser Tripwires", "Heavy Fog", "Radio Jamming").
  * **Endless Compound Mode**: Waves of adaptive hostiles testing how long the operative can survive once the global compound alarm triggers.

### 4. Monetization (Fair & Player-First)
* **Premium or Battle Pass Model**:
  * Free-to-play base prototype with zero pay-to-win mechanics.
  * Monetization focused purely on cosmetic operative skins, custom weapon chassis, muzzle flash visual effects, and customized radar HUD themes.

### 5. AI in the Game & in Development
* **In-Game AI**:
  * **Multi-State Behavior Trees**: Hostiles transition intelligently between `Idle`, `InvestigateSound`, `InvestigateAlarm`, `Attack`, `Retreat` (fleeing to cover when health $< 30\%$), and `Healing`.
  * **Acoustic Investigation**: Sentries investigate the exact origin coordinates of sound ripples.
  * **Line-of-Sight & Cone FOV**: Natural $110^\circ$ vision cone ensures you can sneak behind sentries for stealth eliminations without omniscient 360-degree wallhacks.
  * **Anti-Clustering Layout**: Dynamic spawning guarantees hostiles are spread across the 200m facility in lone sentries or duo pairs, never forming unfair death-balls.
* **AI in Development**:
  * Used advanced agentic coding for rapid architecture, bug resolution (physX self-collision fixes, WebGL lifecycles, coyote-time jump physics), and math optimization.

### 6. Shipping & Soft Launch Metrics
* **What to Test First**: Time-to-Kill (TTK) balance, player movement responsiveness, and WebAssembly 60 FPS performance in low-end browser tabs.
* **Kill Triggers**: Low Day-1 retention ($< 35\%$), or if playtesters report feeling frustrated by alert cascades without counterplay.
* **Key Numbers to Watch**:
  * **D1 Retention**: Benchmark $\ge 40\%$.
  * **D7 Retention**: Benchmark $\ge 16\%$.
  * **Average Session Length**: Target $12–16\text{ minutes}$ across 3–4 runs.
  * **Mission Completion Rate**: Target $\sim 45\%$ on initial attempt.

---

## 🛠️ Technical Architecture

* **Engine**: Unity 6 (Universal Render Pipeline - URP)
* **Target Platform**: WebGL (Zero-Compression enabled for instant GitHub Pages / itch.io compatibility)
* **Key Scripts**:
  * [`GameManager.cs`](file:///Assets/Scripts/GameManager.cs): Mission director, occlusion-filtered random enemy spawning, scoring, and rank calculation.
  * [`EnemyAI.cs`](file:///Assets/Scripts/EnemyAI.cs): 7-state tactical enemy AI with NavMesh pathfinding, hearing, and health regeneration.
  * [`PlayerMovement.cs`](file:///Assets/Scripts/PlayerMovement.cs): First-person controller with coyote time, jump buffering, mouse look dampening, and acoustic ripple generation.
  * [`PlayerShooting.cs`](file:///Assets/Scripts/PlayerShooting.cs): Hitscan firearm with dynamic bullet holes, muzzle flash, and noise propagation.
  * [`TacticalMinimap.cs`](file:///Assets/Scripts/TacticalMinimap.cs): 100m overhead orthographic radar with dynamic enemy blips and orientation compass.
  * [`MenuManager.cs`](file:///Assets/Scripts/MenuManager.cs): Translucent UI manager handling Main Menu, Pause, Victory, and Defeat screens with WebGL-safe quit/restart.

---

## 🚀 Building & Running Locally

1. Open project in **Unity 6 (6000.0+)**.
2. Open scene: `Assets/Scenes/SampleScene.unity`.
3. Press **Play** in Editor, or select **File > Build Profiles > Web - Desktop - Development > Build And Run**.

