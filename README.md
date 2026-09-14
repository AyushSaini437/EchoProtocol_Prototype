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
A tactical FPS infiltration where sound propagation can be used against you. This is for the fans of players who love tactical stealth while having high-stakes action.The tension between speed and silence. Moving fast gives you tactical momentum, but the expanding acoustic ripples give away your exact location to enemies who will coordinate, flank, and radio for reinforcements.

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

---

## Question #2: Gameplay Insights & Strategy

I would like to pick Action-Shooter genre and most famous games fo this genre are PUGB, Free Fire, and CODM in my perspective. <br>

Now as when mobile BR games were first made, they were hit in their genre in mobile gaming industry but as the time passed, there were so many games with the same idea that it became stale. The three famous PC BR game that are Warzone, Apex Legends, and Fortnite all came for mobile as well but they never got that much attention they have over PC on mobile so they never got famous in mobile industry. I am saying this because I have tried all six of these games on mobile and the issue I would like to point out is that for games like PUBG Mobile, CODM, and Free Fire they were already there before these three (Warzone, Apex Legends, and Fortnite) and were very complex not hard for someone who were trying these games for the first time in their life.
1. In warzone, the main issue was first of all the size of the game was veri big even for the mobiles and was very complex for new players and the second main issue was the graphics. The graphics was very choppy, and all the objects were rendered in way when we use LOD on objects and their say was as you play the game, the graphics will get btter.
2. For Apex, their game was not hard or complex but the idea was pretty new for the BR game, like player abilities and as the game was for mobile, the mobile used to get very hot in very short span of time.
3. For Fortnite, the issue is the game mechanic and game size as one the main game mechanic in fortnite is gathering builind materials and building traps and covers which was very hard to do over the mobiles.

<br>

As for the solution to all 6 the games I just listed, in PUBG Mobile, what players/users wants the traditional game back where the players would be dropped on a map and would try to win while fighting with other players. As of now the player count even in PUBG is dropped so much and one of the reason is this that I just told and second and the most important reason is the cosmetics of this game. What KRAFTON always do in their new update is that they collaborate with some other companies and bring their cosmetics into their game which leads in no originality and sometimes players just quit the game and **this same issue is with Free Fire also.**

<br>

What I would love to do in these games is first of all try to optimize these games and second would take player surveys on regular basis. Would try to keep the game to its original form as if once the game faces the backlash from players, it is very hard to gain their trust and this same thing is happening right now with Activision the developers of COD as their are facing the back lash from players globally for their new upcoming COD:MW game.<br>
And then would try to keep the controls as simple as possible and won't clutter the player screen with so many buttons.
The things that keeps a player opening the app many times a day is that they can spent their small amount of time in-game with friends which make them happy but then again if the game isn't liked by many players then the app opening retention will also drop.<br>
And the main idea of monetization in F2P games are though ads and buying cosmetics with in-game currency which can only be obtained through spending money.

---

## Question #3: Design Specification

### Feature Name: "Danger Close" (The Kinetic Graze System)

#### 1. Why I Chose This Feature (Rationale)
When playing mobile survivor-like games like *Survivor.io* or *Vampire Survivors*, the biggest problem I noticed is that after playing for 4 or 5 minutes, the game starts to get boring. Once you get 3 or 4 good weapons, your character shoots automatically and kills everything. You just keep moving the joystick in circles or running away from enemies, and sometimes you can even stand in one place without doing anything. There is no real challenge, and players don't have any reason to take risks.

I chose the **"Danger Close"** (Kinetic Graze) feature because it fixes this exact problem:
1. **Rewards Skill and Courage**: Instead of just running away all the time, if you move very close to the enemy horde without touching them, you charge up an Overdrive meter.
2. **Simple Mobile Controls**: Mobile games are played with just one thumb on the screen. This feature does not add any new buttons. You just play normally with the joystick, but you focus on spacing and moving near enemies.
3. **High Risk, High Reward**: Casual players can still play it safe by running away, but players who want fast clears and more fun can take the risk of dodging inches away from enemies to get massive attack bursts.

<br>

#### 2. Design Intent (How it Works for the Player)
The main idea is to make close calls feel fun and exciting.

* **The Graze Ring**: There is a soft glowing circle around your player that shows your danger zone.
* **Visual & Sound Feedback**: When enemies walk inside this ring without touching your player's body, yellow sparks fly between your player and the enemies with a zap sound effect, and your yellow **Overdrive Meter** fills up quickly.
* **The Overdrive Mode**: When the meter reaches 100%, your player automatically goes into **Overdrive Mode** for 5 seconds:
  * The edges of the mobile screen glow with yellow electric light.
  * All your weapons shoot 3 times faster and do 50% extra damage.
  * A shockwave pushes all small enemies 2 meters away so you don't get trapped.
  * Your player moves 20% faster so you can cut right through the swarm.

<br>

#### 3. Fully Detailed Technical Rules & Balance

##### A. How the Distance Works (Ring Sizes)
* **Player Body Hitbox**: **0.4 meters**. If an enemy touches this inner circle, you take damage and your graze stops.
* **Graze Ring**: **1.2 meters**.
* **The Danger Zone (Sweet Spot)**: The area between **0.4m and 1.2m** (a 0.8m wide donut around the player). Any enemy walking inside this area charges your meter.

##### B. How the Overdrive Meter Fills and Drains
* **How Fast It Fills**:
  * Each normal enemy inside the danger zone gives **+5% meter every second**.
  * If you dodge past 4 enemies at the same time: $4 \times 5\% = +20\%$ per second (your bar fills completely in **5 seconds**).
  * Tough mini-bosses and elites give **+15% per second**.
* **Max Enemy Cap**: Only up to 10 enemies are counted at once (+50% per second max). This stops the bar from instantly hitting 100% if you brush against a huge swarm for half a second.
* **Meter Drain (No Camping)**: If you run away and no enemies are inside your danger zone for more than **1.5 seconds**, the bar starts draining by **-8% every second**. You have to stay close to the action to keep your charge.

##### C. State Transition Table

| Game State | When It Happens | What It Does In Game | Visual & Sound Effect |
| :--- | :--- | :--- | :--- |
| **Normal (Idle)** | No enemies in danger zone; meter $< 100\%$ | Normal movement and auto-attacks | Graze ring is faint white and dim |
| **Charging (Grazing)** | 1 or more enemies inside 0.4m – 1.2m zone | Meter fills up based on enemy count; score multiplier goes up | Yellow sparks arc from enemies to player; rising hum sound |
| **Overdrive Active** | Meter hits $100\%$ | Lasts **5.0 seconds**; +200% attack speed; +50% damage; knockback wave | Golden shockwave bursts out; screen border glows gold; heavy bass drop |
| **Cooldown** | 5 seconds of Overdrive finish | Meter goes to $0\%$; 2.5 second cooldown before you can charge again | Ring turns dark grey; steam particles come off player |

##### D. Special Rules & Fixing Exploits
1. **What happens if you get hit?**  
   If an enemy touches your body while you are grazing, you immediately **lose 25% of your Overdrive meter**. Also, while your player is flashing red (invincible for 0.5 seconds), you cannot charge the meter. This stops players from cheating by taking damage on purpose just to fill the bar.
2. **Boss Fights**:  
   Big stage bosses cannot be pushed back by the shockwave. Instead, triggering Overdrive near a boss **breaks 30% of the boss's shield bar**, making close-range play useful during 1v1 boss fights.
3. **New Upgrade Cards for this Feature**:  
   When leveling up mid-run, players can pick new cards to make their graze stronger:
   * **Static Discharge (Level 1)**: Grazing enemies zaps the nearest enemy with lightning for 40 damage every 0.5s.
   * **Slipstream (Level 2)**: Move 15% faster whenever 2 or more enemies are inside your danger zone.
   * **Supercharger (Level 3)**: Overdrive lasts 7.5 seconds (instead of 5.0s) and gives you a 1-hit shield.

<br>

#### 4. UI Wireframes & Screen Mockups

##### Wireframe 1: Standard In-Game HUD (Active Grazing)
```text
+-------------------------------------------------------------+
| [LVL 14] [=======EXP BAR=======]          [TIME: 07:42] [II] |
|                                             [KILLS: 1,420]   |
+-------------------------------------------------------------+
|                                                             |
|                    [Horde Swarm]                            |
|                     *  *  *  *                              |
|                   *  *  *  *  *                             |
|                                                             |
|                       .---.  <--- (1.2m Graze Ring)         |
|                      /  *  \  <-- Enemy in Graze zone       |
|                     |   P   |     (Sparks flying!)          |
|                      \     /                                |
|                       '---'                                 |
|                  [====65%====] <--- (Overdrive Meter)       |
|                   [==HP: 85%]                               |
|                                                             |
|                                                             |
|      ( O ) <--- Floating Virtual Joystick                   |
|     (Thumb)     (Only 1 control needed!)                    |
|                                                             |
+-------------------------------------------------------------+
```

##### Wireframe 2: Overdrive Trigger State (Hyper-Mode Active)
```text
+=============================================================+
|# GOLDEN SCREEN VIGNETTE PULSING #           [TIME: 07:45] [II]
+-------------------------------------------------------------+
|                                                             |
|               \ \ \  KINETIC SHOCKWAVE  / / /                |
|             (Knocks back nearby horde 2 meters)             |
|                                                             |
|                     *    *    *                             |
|                    <-*  [P]  *->                            |
|                     *    *    *                             |
|               / / /  RAPID FIRE 3x  \ \ \                   |
|                                                             |
|                  [|||||| 4.2s ||||||]                       |
|                 OVERDRIVE ACTIVE! (+50% DMG)                |
|                   [==HP: 85%]                               |
|                                                             |
|                                                             |
|      ( O )                                                  |
|                                                             |
+=============================================================+
```

##### Wireframe 3: Mid-Run Upgrade Selection with Graze Synergy
```text
+-------------------------------------------------------------+
|                         CHOOSE UPGRADE                      |
|                     Rerolls: 1 | Banish: 2                  |
+-------------------------------------------------------------+
|  +--------------------+  +--------------------+             |
|  | [ICON: LIGHTNING]  |  | [ICON: DRONE]      |             |
|  | STATIC DISCHARGE   |  | GUARDIAN DRONE     |             |
|  | ★ ★ ☆ ☆ ☆          |  | ★ ★ ★ ★ ☆          |             |
|  |                    |  |                    |             |
|  | Danger Close Graze |  | Spawns an armed    |             |
|  | shocks nearby foes |  | drone that orbits  |             |
|  | for 40 dmg/0.5s.   |  | and fires laser.   |             |
|  |                    |  |                    |             |
|  | [ SELECT ]         |  | [ SELECT ]         |             |
|  +--------------------+  +--------------------+             |
|  +--------------------+                                     |
|  | [ICON: BOOTS]      |                                     |
|  | SLIPSTREAM         |                                     |
|  | ★ ☆ ☆ ☆ ☆          |                                     |
|  |                    |                                     |
|  | +15% Move Speed    |                                     |
|  | while enemies are  |                                     |
|  | in Graze Ring.     |                                     |
|  |                    |                                     |
|  | [ SELECT ]         |                                     |
|  +--------------------+                                     |
+-------------------------------------------------------------+
```


