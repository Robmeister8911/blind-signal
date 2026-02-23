# Blind Signal

> **Genre:** Top-down Android multiplayer stealth/action
> **Engine:** Unity 6000.3.9f1
> **Backend:** Supabase (Realtime channels + Database)

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture](#2-architecture)
3. [Folder Structure](#3-folder-structure)
4. [Opening the Project in Unity](#4-opening-the-project-in-unity)
5. [Setting up the Supabase C# SDK](#5-setting-up-the-supabase-c-sdk)
6. [Phase 1 – Local Sandbox Setup](#6-phase-1--local-sandbox-setup)
7. [Phase 2 – Networked Sandbox Setup](#7-phase-2--networked-sandbox-setup)
8. [Script Reference](#8-script-reference)
9. [Shader Reference](#9-shader-reference)
10. [Design Formulas](#10-design-formulas)
11. [Extending the Project](#11-extending-the-project)

---

## 1. Project Overview

**Blind Signal** is a top-down multiplayer game where players navigate a dark arena
guided only by acoustic signals – the "pings" produced by their own movement, by
opponent footsteps, and by weapon discharge.

### Core Pillars

| Pillar | Description |
|---|---|
| **Fog of War** | Players cannot see each other directly; they hear acoustic pings whose visibility radius is governed by the `AcousticRange` stat. |
| **Noise Value (V_n)** | Every action produces a noise value: crouch=1, walk=3, sprint=5, gunshot=15. |
| **Dampening** | Upgrading the Dampening rank reduces the noise a player emits, making them harder to detect. |
| **Supabase Realtime** | Pings are broadcast to all connected clients via a Supabase Realtime broadcast channel. |

---

## 2. Architecture

```
┌──────────────────────────────────────────────────────┐
│                     Unity Client                      │
│                                                      │
│  PlayerMovement ──► MatchEventSync.SendPing()        │
│  LinearPulse    ──► MatchEventSync.SendPing()        │
│                                                      │
│  MatchEventSync ──► OnRemotePingReceived (event)     │
│                          │                           │
│                     PingSpawner                      │
│                     (Fog-of-War check)               │
│                          │                           │
│                   Instantiate PingRipple prefab      │
└──────────────────────────────────────────────────────┘
             │                       ▲
     SendPing (broadcast)    Realtime push
             │                       │
┌────────────▼───────────────────────┴────────────────┐
│              Supabase Realtime Channel               │
│           "realtime:public:pings"                    │
└─────────────────────────────────────────────────────┘
```

### Singleton chain

```
SupabaseManager (persistent)
    └── holds Supabase credentials & client reference

MatchEventSync (persistent)
    ├── references SupabaseManager.Instance
    ├── subscribes to Realtime channel on Start()
    └── fires OnRemotePingReceived static event

PingSpawner (scene)
    └── subscribes to MatchEventSync.OnRemotePingReceived
```

---

## 3. Folder Structure

```
Assets/
├── Shaders/
│   └── PingRipple.shader         – Unlit ring shader (transparent, animated via material properties)
└── Scripts/
    ├── Core/                     – (Reserved for future game-loop, state-machine scripts)
    ├── Networking/
    │   ├── SupabaseManager.cs    – Singleton; holds credentials; initialises Supabase client
    │   └── MatchEventSync.cs     – Sends/receives pings; mock channel for offline testing
    ├── Player/
    │   ├── PlayerAttributes.cs   – Stats (dampening, acoustic range) with diminishing-returns formulas
    │   └── PlayerMovement.cs     – Reads VirtualJoystick; moves player; broadcasts movement pings
    ├── UI/
    │   └── VirtualJoystick.cs    – Mobile UI joystick (IPointerDownHandler / IDragHandler)
    ├── Visuals/
    │   ├── PingRipple.cs         – Animates a single ripple prefab (scale up, fade out, self-destruct)
    │   └── PingSpawner.cs        – Listens to MatchEventSync; applies Fog-of-War; spawns ripples
    └── Weapons/
        └── LinearPulse.cs        – Fires a straight projectile; broadcasts muzzle-flash ping (V_n=15)
```

---

## 4. Opening the Project in Unity

1. **Install Unity Hub** and add the **Unity 6000.3.9f1** editor (Android Build Support
   module is required for device builds).
2. In Unity Hub click **Add project from disk** and select the repository root folder
   (the folder that contains `Assets/`, `.gitignore`, and `README.md`).
3. Unity will create the `Library/`, `Temp/` and `Logs/` folders automatically on
   first open – these are git-ignored.
4. Open **Build Settings** (`File → Build Settings`) and switch the Platform to
   **Android**.
5. In **Player Settings** set a Bundle Identifier, minimum API Level (API 26 / Android 8
   recommended), and enable **IL2CPP** scripting backend for release builds.

> **Note:** `.meta`, `.unity` (scene), and `.prefab` files are NOT included in this
> repository – they must be created manually in the Unity Editor as described in
> Sections 6 and 7.  This is intentional; Unity's YAML serialization is environment-
> specific and cannot be reliably generated as plain text.

---

## 5. Setting up the Supabase C# SDK

### 5.1 Install via NuGet / UPM

The recommended approach for Unity is the community **supabase-unity** wrapper.

1. Open the Unity Package Manager (`Window → Package Manager`).
2. Click **+** → **Add package from git URL** and enter:
   ```
   https://github.com/supabase-community/supabase-unity.git#upm
   ```
   (Check the [supabase-unity releases page](https://github.com/supabase-community/supabase-unity)
   for the latest UPM-compatible ref.)
3. Unity will import the SDK and its dependencies (`gotrue-csharp`,
   `realtime-csharp`, `postgrest-csharp`, etc.).

### 5.2 Activate the real implementation

In **`SupabaseManager.cs`** uncomment the `using Supabase;` directive and the
initialisation block inside `InitialiseClient()`.

In **`MatchEventSync.cs`** uncomment the channel subscription / send logic in
`SubscribeToChannel()`, `UnsubscribeFromChannel()`, and `SendPing()`.
Set `_useMockChannel = false` in the Inspector (or default value in the script).

### 5.3 Create the Supabase table / channel

In your Supabase project dashboard:

1. Go to **Table Editor** → **New Table** named `pings` (optional; broadcast channels
   do not require a table, but a table is useful for persistence/logging).
2. Enable **Realtime** on the table if you choose to use Postgres Changes instead of
   broadcast.
3. For the broadcast approach (recommended for low-latency pings), no table is
   needed – the channel name is just a string (e.g. `"pings"`).

---

## 6. Phase 1 – Local Sandbox Setup

In this phase everything runs on a single device with the **mock channel** active.
No internet connection is required.

### 6.1 Create the Bootstrap Scene

1. Create a new scene (`File → New Scene → Basic (Built-in)`).  Save it as
   `Assets/Scenes/Bootstrap.unity`.
2. Create an empty GameObject named **`GameManager`**.
3. Attach the following components to `GameManager`:
   - `SupabaseManager` (leave placeholder credentials as-is for Phase 1)
   - `MatchEventSync` (enable **Use Mock Channel**, set **Mock Ping Interval** to 3)

### 6.2 Create the Ping Ripple Prefab

1. `GameObject → 3D Object → Quad`.  Name it **`PingRipple`**.
2. In the Inspector, set **Rotation** to `(90, 0, 0)` so the quad lies flat on the XZ plane.
3. Create a new **Material** (`Assets/Materials/PingRippleMat.mat`).
4. Set the Material's **Shader** to `BlindSignal/PingRipple`.
5. Assign the material to the Quad's MeshRenderer.
6. Attach the `PingRipple` script to the Quad.
7. Drag the Quad into `Assets/Prefabs/` to save it as a Prefab, then delete the
   scene instance.

### 6.3 Create the Player

1. Create an empty GameObject named **`Player`**.  Add a Tag **`Player`** to it.
2. Add a **Capsule** mesh as a child for the visual representation.
3. Attach these components to the `Player` root:
   - `PlayerAttributes` (leave ranks at 0 for baseline testing)
   - `PlayerMovement` (assign `VirtualJoystick` reference after step 6.4)
4. Optionally add a Camera as a child, looking straight down.

### 6.4 Create the Virtual Joystick UI

1. `GameObject → UI → Canvas`.  Set **Render Mode** to `Screen Space – Overlay`.
2. Inside the Canvas, create a **Panel** and name it **`JoystickBackground`**:
   - Set **Anchor** to bottom-left, position it in the lower-left corner, size ~150×150 px.
   - Set Panel **Image Alpha** to ~0.4 for visibility.
3. Add a child **Image** named **`JoystickHandle`** (circular, ~60×60 px, centred).
4. Attach the `VirtualJoystick` script to **`JoystickBackground`**.
5. Assign **`JoystickHandle`**'s RectTransform to the **Handle** field.
6. Assign the `VirtualJoystick` component to **`PlayerMovement`** → **Joystick** field.

### 6.5 Create the PingSpawner

1. Add a **`PingSpawner`** component to the `GameManager` object.
2. Assign:
   - **Ping Ripple Prefab** → the `PingRipple` Prefab created in step 6.2.
   - **Local Player Transform** → the `Player` GameObject's Transform.
   - **Local Player Attributes** → the `PlayerAttributes` component on `Player`.

### 6.6 Play Mode Test

Press **Play**.  Move the joystick – every 0.5 s a movement ping fires.  The mock
channel also emits random pings every 3 s.  You should see ripple rings appear on
the ground plane wherever pings originate (within the AcousticRange radius).

---

## 7. Phase 2 – Networked Sandbox Setup

### 7.1 Prerequisites

- Supabase SDK installed (Section 5).
- A Supabase project with Realtime enabled.

### 7.2 Enable the Real Network Layer

1. Select the `GameManager` GameObject.
2. In **`SupabaseManager`** enter your real **Supabase URL** and **Anon Key**.
3. In **`MatchEventSync`** uncheck **Use Mock Channel**.
4. Uncomment the SDK code blocks in both scripts as described in Section 5.2.

### 7.3 Multi-device Testing

1. Build and deploy to two Android devices (or one device + Unity Editor).
2. Ensure both are connected to the internet and authenticated (add `GoTrue`
   sign-in if your Supabase tables use RLS).
3. Move one device's player – the other device should display ripple rings within
   its player's AcousticRange.

### 7.4 Adding the Weapon

1. Create an empty child of `Player` named **`Weapon`**, position at `(0, 0.5, 0.5)`.
2. Add an empty child named **`MuzzlePoint`**, position at `(0, 0, 0.4)`.
3. Attach `LinearPulse` to the **`Weapon`** object.
4. Assign:
   - **Projectile Prefab** → a Sphere with a Rigidbody (no gravity; `Is Kinematic`
     false, Gravity Scale 0 in Rigidbody 2D, or set `useGravity = false`).
   - **Muzzle Point** → the `MuzzlePoint` Transform.
   - **Match Event Sync** → the `MatchEventSync` component on `GameManager`.
5. Create a UI **Button** and wire its `OnClick()` event to `LinearPulse.FireWeapon()`.

---

## 8. Script Reference

### `SupabaseManager.cs`
| Field | Type | Description |
|---|---|---|
| `SupabaseUrl` | `string` | Supabase project URL |
| `SupabaseAnonKey` | `string` | Public anon key |
| `Instance` | `SupabaseManager` | Singleton accessor |

### `MatchEventSync.cs`
| Member | Type | Description |
|---|---|---|
| `OnRemotePingReceived` | `event Action<Vector2, float, string>` | Fired when a remote ping arrives |
| `SendPing(pos, v_n, id)` | method | Broadcasts a ping to the channel |
| `_useMockChannel` | `bool` | Toggle offline mock mode |

### `VirtualJoystick.cs`
| Property | Type | Description |
|---|---|---|
| `Direction` | `Vector2` | Normalised input direction `[-1,1]` |
| `Magnitude` | `float` | Input strength `[0,1]` |

### `PlayerAttributes.cs`
| Property | Type | Description |
|---|---|---|
| `DampeningFactor` | `float` | Multiplier applied to noise `[0.2,1]` |
| `AcousticRange` | `float` | Hearing radius in world units `[200,350]` |
| `DampeningRank` | `int` | Underlying rank (setter recalculates stats) |
| `AcousticRangeRank` | `int` | Underlying rank (setter recalculates stats) |

### `PlayerMovement.cs`
| Field | Description |
|---|---|
| `_joystick` | Reference to `VirtualJoystick` |
| `_matchEventSync` | Reference to `MatchEventSync` |
| `_maxSpeed` | Top movement speed (world units/s) |
| `_pingInterval` | Seconds between movement pings |
| `PlayerId` | Unique player identifier |

### `PingRipple.cs`
| Field | Description |
|---|---|
| `_lifetime` | Duration of the animation (seconds) |
| `_maxScale` | Final world-space scale of the ring |
| `SetIntensity(float)` | Colours the ring based on noise value |

### `PingSpawner.cs`
| Field | Description |
|---|---|
| `_pingRipplePrefab` | The `PingRipple` prefab |
| `_localPlayerTransform` | Used for distance checks |
| `_localPlayerAttributes` | Provides `AcousticRange` |

### `LinearPulse.cs`
| Field | Description |
|---|---|
| `_projectilePrefab` | Prefab spawned on fire |
| `_muzzlePoint` | Spawn transform |
| `_projectileSpeed` | Projectile velocity |
| `_fireCooldown` | Minimum time between shots |
| `MuzzleFlashNoiseValue` | Constant `15f` |
| `FireWeapon()` | Call to fire; returns `bool` |

---

## 9. Shader Reference

### `BlindSignal/PingRipple`

An **Unlit Transparent** shader that draws a smooth circular ring on a Quad mesh.

| Property | Type | Description |
|---|---|---|
| `_Color` | Color | Ring colour (RGBA) |
| `_Radius` | Float `[0,1]` | Normalised ring radius in UV space |
| `_Thickness` | Float `[0,0.5]` | Ring thickness in UV space |
| `_Alpha` | Float `[0,1]` | Master opacity multiplier |

`PingRipple.cs` animates `_Alpha` and the Transform scale at runtime.  The `_Radius`
is kept constant at `0.45` so the ring always fills the quad; apparent size is driven
by the Transform scale.

---

## 10. Design Formulas

### Acoustic Range
```
AcousticRange = 200 + 150 * (1 - e^(-0.25 * acoustic_range_rank))
```
- Rank 0 → 200 units
- Rank 5 → ≈ 272 units
- Rank 10 → ≈ 313 units
- Asymptote: 350 units

### Dampening Factor
```
DampeningFactor = 1 - 0.8 * (1 - e^(-0.3 * dampening_rank))
```
- Rank 0 → factor 1.0 (no reduction)
- Rank 5 → factor ≈ 0.43
- Rank 10 → factor ≈ 0.24
- Asymptote: 0.2 (80% max noise reduction)

### Noise Value (V_n)
| Action | Base V_n | After Dampening (rank 5) |
|---|---|---|
| Crouch / Idle | 1 | 0.43 |
| Walk | 3 | 1.29 |
| Sprint | 5 | 2.15 |
| Gunshot | 15 | 6.45 |

---

## 11. Extending the Project

- **Authentication:** Add `GoTrue` sign-in to `SupabaseManager` and propagate the
  authenticated user ID as `PlayerId` throughout.
- **Lobby / Match-making:** Use a Supabase table (`matches`) with Postgres Changes
  to let players join the same session.
- **Additional Weapons:** Subclass or add new `MonoBehaviour` scripts in
  `Assets/Scripts/Weapons/` that call `MatchEventSync.SendPing()` with appropriate
  Noise Values.
- **Upgrade System:** Write a progression manager in `Assets/Scripts/Core/` that
  increments `PlayerAttributes.DampeningRank` and `PlayerAttributes.AcousticRangeRank`.
- **Map / Level Design:** Add colliders and lighting to the scene.  The acoustic
  system works independently of visual rendering, so full darkness is trivially
  achievable by disabling scene lights.
