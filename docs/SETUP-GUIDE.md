# Cats vs Capybaras — Unity Setup Guide

Complete step-by-step instructions for going from zero to a playable game in Unity 6.3.

---

## Prerequisites

- **macOS** with Apple Silicon (M1/M2/M3/M4)
- **Unity Hub** — download from [unity.com/download](https://unity.com/download)
- **Unity 6.3** — install via Unity Hub (include **iOS Build Support** module)
- **Xcode 15+** — from Mac App Store (required for iOS builds)
- **Apple Developer Account** — $99/year, only needed when ready for App Store

> If Unity fails to open, ensure Rosetta 2 is installed:
> ```
> softwareupdate --install-rosetta
> ```

---

## Step 1: Create the Unity Project

1. Open **Unity Hub**
2. Click **New Project** (top-right)
3. Select the **2D (URP)** template
4. Set **Project name** to `CatsVsCapybaras`
5. Set **Location** to wherever you keep projects
6. Click **Create project**
7. Wait for the editor to open (first time takes 2-5 minutes)

---

## Step 2: Configure for iOS

### 2a. Add iOS Build Profile

1. Go to **File → Build Profiles**
2. Click **Add Build Profile**
3. Select **iOS**
4. Close the Build Profiles window

### 2b. Player Settings

1. Go to **Edit → Project Settings → Player**
2. Click the **iOS tab** (phone icon)
3. Set these fields:

| Setting | Value |
|---------|-------|
| Company Name | Your name or studio |
| Product Name | Cats vs Capybaras |
| Bundle Identifier | `com.yourname.catsvscapybaras` |
| Target minimum iOS Version | 15.0 |
| Scripting Backend | IL2CPP |
| Target Architectures | ARM64 only |
| Default Orientation | Landscape Left |
| Allowed Orientations | Landscape Left + Landscape Right (check both) |

---

## Step 3: Create Folder Structure

In the **Project window** (bottom panel), right-click in `Assets/` and create this hierarchy using **Create → Folder**:

```
Assets/
├── Scripts/
│   ├── Core/
│   ├── Characters/
│   ├── Weapons/
│   ├── Environment/
│   ├── Camera/
│   ├── Input/
│   ├── UI/
│   └── Effects/
├── Prefabs/
│   ├── Characters/
│   ├── Projectiles/
│   └── Effects/
├── ScriptableObjects/
│   └── Weapons/
├── Scenes/
├── Art/
│   ├── Characters/
│   ├── Terrain/
│   ├── UI/
│   └── Backgrounds/
├── Audio/
│   ├── SFX/
│   └── Music/
├── Materials/
└── PhysicsMaterials/
```

---

## Step 4: Import Scripts

### 4a. Copy the files

From **Finder** (not Unity), copy the contents of the repo's `src/` folder into your Unity project:

```
src/Core/*           →  Assets/Scripts/Core/
src/Characters/*     →  Assets/Scripts/Characters/
src/Weapons/*        →  Assets/Scripts/Weapons/
src/Environment/*    →  Assets/Scripts/Environment/
src/Camera/*         →  Assets/Scripts/Camera/
src/Input/*          →  Assets/Scripts/Input/
src/UI/*             →  Assets/Scripts/UI/
src/Effects/*        →  Assets/Scripts/Effects/
```

Or from terminal:

```bash
cp -r /path/to/cats-vs-capybaras/src/* /path/to/CatsVsCapybaras/Assets/Scripts/
```

### 4b. Wait for compilation

1. Switch back to Unity — it will detect the new files and compile
2. Watch the bottom-left status bar for "Compiling..." to finish
3. Open **Window → General → Console** and check for errors

### 4c. Import TextMeshPro

If you see errors about `TMPro`:

1. Go to **Window → TextMeshPro → Import TMP Essential Resources**
2. Click **Import** in the dialog
3. Wait for recompilation

After this step, the Console should show **zero errors**.

---

## Step 5: Set Up Physics Layers

### 5a. Create Layers

1. Go to **Edit → Project Settings → Tags and Layers**
2. Expand **Layers** section
3. Add these custom layers:

| Slot | Layer Name |
|------|-----------|
| User Layer 8 | `Terrain` |
| User Layer 9 | `Characters` |
| User Layer 10 | `Projectiles` |
| User Layer 11 | `Boundary` |

### 5b. Collision Matrix

1. Go to **Edit → Project Settings → Physics 2D**
2. Scroll down to **Layer Collision Matrix**
3. Configure which layers collide:

| | Terrain | Characters | Projectiles | Boundary |
|---|---|---|---|---|
| **Terrain** | — | Yes | Yes | — |
| **Characters** | Yes | Yes | — | Yes |
| **Projectiles** | Yes | Yes | — | Yes |
| **Boundary** | — | Yes | Yes | — |

Key rule: **Projectiles do NOT collide with Characters** at the physics layer level. Explosion damage uses `Physics2D.OverlapCircleAll` which ignores the collision matrix, so characters still take explosion damage.

### 5c. Physics 2D Settings

While in Physics 2D settings, verify:
- **Gravity Y**: `-9.81` (default)

---

## Step 6: Create the Terrain

### 6a. Prepare terrain art

1. Create or find a terrain PNG image (~2500×500 pixels)
   - Solid pixels = ground
   - Transparent pixels = sky/holes/canyon
   - Include a gap in the middle for the canyon (cats on left, capybaras on right)
2. Place the PNG in `Assets/Art/Terrain/`

### 6b. Configure terrain sprite import

1. Select the terrain PNG in the Project window
2. In the **Inspector**, set these import settings:

| Setting | Value |
|---------|-------|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single |
| Pixels Per Unit | 100 |
| **Read/Write** | **Enabled** (critical!) |
| Generate Mip Maps | Disabled |
| Filter Mode | Point (no filter) |
| Compression | None |

3. Click **Apply**

> **Read/Write must be enabled** — the TerrainDestruction script modifies pixels at runtime. Without this, you'll get errors.

### 6c. Create Terrain GameObject

1. In the **Hierarchy**, right-click → **Create Empty** → name it `Terrain`
2. Add components (use **Add Component** button in Inspector):

| Component | Settings |
|-----------|----------|
| **SpriteRenderer** | Drag your terrain sprite into the Sprite field |
| **PolygonCollider2D** | Unity auto-generates from sprite alpha (leave defaults) |
| **TerrainDestruction** | (script) — references auto-populate from RequireComponent |

3. Set the **Layer** dropdown (top of Inspector) to **Terrain**
4. Set **Position** to `(12.5, 0, 0)` — centers a 25-unit-wide terrain
5. Verify the **TerrainDestruction** component shows:
   - `Terrain Renderer` → the SpriteRenderer on this object
   - `Terrain Collider` → the PolygonCollider2D on this object
   - `Solid Threshold` → 0.1

---

## Step 7: Create Character Prefabs

You need **4 characters**: 2 cats (Team 0) and 2 capybaras (Team 1).

### 7a. Create the first character

1. In the Hierarchy, right-click → **2D Object → Sprites → Square** (placeholder sprite)
2. Rename it `Cat_Miso`
3. Set **Transform**:
   - Position: `(3, 5, 0)` — will drop onto terrain via gravity
   - Scale: `(0.8, 0.8, 1)` — adjust to taste
4. Add components:

| Component | Settings |
|-----------|----------|
| **Rigidbody2D** | Gravity Scale: `1`, Freeze Rotation Z: `checked`, Collision Detection: `Continuous` |
| **CapsuleCollider2D** | Direction: Vertical, Size: `(0.6, 0.9)`, Offset: `(0, 0)` — adjust to fit sprite |
| **CharacterController2D** | See field values below |

5. Set the **Layer** dropdown to **Characters**

### 7b. Configure CharacterController2D fields

| Field | Value |
|-------|-------|
| Character Name | `Miso` |
| Team Index | `0` (Cats) |
| Walk Speed | `3` |
| Jump Force | `9` |
| Ground Layers | Click the dropdown → check **Terrain** and **Boundary** |
| Ground Check Distance | `0.15` |
| Ground Check Offset | `(0, -0.5)` |
| Max Health | `100` |
| Fall Damage Threshold | `3` |
| Fall Damage Multiplier | `10` |

### 7c. Create remaining characters

Duplicate `Cat_Miso` three times and configure:

| Name | Character Name | Team Index | Position X |
|------|---------------|------------|-----------|
| `Cat_Miso` | Miso | 0 | 3 |
| `Cat_Nova` | Nova | 0 | 5 |
| `Capy_Bubba` | Bubba | 1 | 20 |
| `Capy_Coco` | Coco | 1 | 22 |

All Y positions should be `5` (they'll fall onto the terrain).

### 7d. (Optional) Save as prefabs

1. Drag each character from the Hierarchy into `Assets/Prefabs/Characters/`
2. This creates reusable prefabs for future levels

---

## Step 8: Create Projectile Prefabs

Create one prefab for each weapon type.

### 8a. Carrot Projectile

1. Hierarchy → right-click → **2D Object → Sprites → Triangle** (placeholder)
2. Rename to `CarrotProjectile`
3. Set Scale to `(0.3, 0.3, 1)`
4. Set **SpriteRenderer** color to orange `(1, 0.5, 0.1)`
5. Add components:

| Component | Settings |
|-----------|----------|
| **Rigidbody2D** | Gravity Scale: `0` (ProjectileBase sets to 1 on launch) |
| **CircleCollider2D** | Radius: `0.1` |
| **CarrotProjectile** | (script) — inherits from ProjectileBase |

6. Set Layer to **Projectiles**
7. (Optional) Add **TrailRenderer**:
   - Width: `0.15` → `0`
   - Time: `0.3`
   - Material: Default-Line or create a simple material
   - Color: orange gradient
8. Drag into `Assets/Prefabs/Projectiles/` → click **Original Prefab**
9. **Delete from scene** (it's a prefab now, spawned at runtime)

### 8b. Bomb Projectile

1. Create another sprite → rename `BombProjectile`
2. Set Scale to `(0.4, 0.4, 1)`, color to dark gray
3. Add components:

| Component | Settings |
|-----------|----------|
| **Rigidbody2D** | Gravity Scale: `0` |
| **CircleCollider2D** | Radius: `0.15` |
| **BombProjectile** | Max Bounces: `3`, Fuse Time: `3` |

4. **Create a PhysicsMaterial2D** for bouncing:
   - Right-click in `Assets/PhysicsMaterials/` → **Create → 2D → Physics Material 2D**
   - Name it `BouncyBomb`
   - Set **Bounciness**: `0.6`, **Friction**: `0.3`
   - Assign it to the Rigidbody2D's **Material** field
5. Set Layer to **Projectiles**
6. Save as prefab in `Assets/Prefabs/Projectiles/`, delete from scene

### 8c. Banana Projectile

1. Create another sprite → rename `BananaProjectile`
2. Set Scale to `(0.35, 0.35, 1)`, color to yellow
3. Add components:

| Component | Settings |
|-----------|----------|
| **Rigidbody2D** | Gravity Scale: `0` |
| **CircleCollider2D** | Radius: `0.12` |
| **BananaProjectile** | Spin Speed: `540`, Gravity Multiplier: `1.2` |

4. Set Layer to **Projectiles**
5. Save as prefab, delete from scene

---

## Step 9: Create Weapon ScriptableObjects

### 9a. Create weapon data assets

1. In the Project window, navigate to `Assets/ScriptableObjects/Weapons/`
2. Right-click → **Create → Cats vs Capybaras → Weapon Data**
3. Create **three** assets, named: `Weapon_Carrot`, `Weapon_Bomb`, `Weapon_Banana`

### 9b. Configure each weapon

Select each asset and fill in the Inspector:

**Weapon_Carrot**

| Field | Value |
|-------|-------|
| Weapon Name | `Carrot` |
| Description | `Direct hit. Explodes on contact.` |
| Projectile Prefab | Drag `CarrotProjectile` prefab here |
| Damage | `32` |
| Explosion Radius | `1.9` |
| Min Power | `5` |
| Max Power | `15` |
| Starting Ammo | `-1` (infinite) |

**Weapon_Bomb**

| Field | Value |
|-------|-------|
| Weapon Name | `Bomb` |
| Description | `Bounces 3 times. 3-second fuse.` |
| Projectile Prefab | Drag `BombProjectile` prefab here |
| Damage | `55` |
| Explosion Radius | `3.25` |
| Min Power | `4` |
| Max Power | `12` |
| Starting Ammo | `2` |

**Weapon_Banana**

| Field | Value |
|-------|-------|
| Weapon Name | `Banana` |
| Description | `Arcing spin. Explodes on contact.` |
| Projectile Prefab | Drag `BananaProjectile` prefab here |
| Damage | `38` |
| Explosion Radius | `2.4` |
| Min Power | `5` |
| Max Power | `14` |
| Starting Ammo | `3` |

---

## Step 10: Set Up the Camera

1. Select **Main Camera** in the Hierarchy
2. Verify it has a **Camera** component set to:
   - Projection: **Orthographic**
   - Size: `5`
3. Add the **GameCamera** script
4. Configure:

| Field | Value |
|-------|-------|
| Character Follow Damping | `0.15` |
| Projectile Follow Damping | `0.05` |
| Follow Offset | `(0, 1)` |
| Default Pan Duration | `1` |
| Default Shake Magnitude | `0.2` |
| Default Shake Duration | `0.3` |
| World Width | `25` |
| World Min Y | `-2` |
| World Max Y | `8` |
| Default Ortho Size | `5` |

5. Set camera position to `(12.5, 3, -10)` — centered on map

---

## Step 11: Create Manager GameObjects

### 11a. GameManager + TurnManager

1. Hierarchy → right-click → **Create Empty** → name it `GameManager`
2. Add two scripts: **GameManager** and **TurnManager**
3. Also add the **TeamManager** script to this same object

### 11b. Input Manager

1. Hierarchy → right-click → **Create Empty** → name it `InputManager`
2. Add the **PlayerInputHandler** script
3. Set **Game Camera** field: drag the **Main Camera** from Hierarchy into this slot

### 11c. Wire GameManager Inspector references

Select the `GameManager` object and fill in the **GameManager** component:

| Field | Drag from Hierarchy |
|-------|-------------------|
| Turn Manager | `GameManager` object (same object — it has the TurnManager component) |
| Team Manager | `GameManager` object (same object) |
| Game Camera | `Main Camera` |
| Input Handler | `InputManager` |
| Hud Manager | (leave empty for now — set after creating UI in Step 12) |
| Terrain | `Terrain` |
| Weapons | Set size to `3`, then drag: `Weapon_Carrot`, `Weapon_Bomb`, `Weapon_Banana` |
| Max Wind Strength | `5` |

### 11d. Wire TurnManager Inspector references

On the same `GameManager` object, fill in the **TurnManager** component:

| Field | Drag from Hierarchy |
|-------|-------------------|
| Turn Duration | `35` |
| Resolving Delay | `1.5` |
| Transition Duration | `1` |
| Game Manager | `GameManager` object (same object) |
| Team Manager | `GameManager` object (same object) |
| Game Camera | `Main Camera` |
| Input Handler | `InputManager` |

### 11e. Wire TeamManager

On the same `GameManager` object, fill in the **TeamManager** component:

1. Set **Teams** array size to `2`

2. **Element 0** (Cats):
   - Team Name: `Cats`
   - Team Color: orange `(1, 0.6, 0.2, 1)`
   - Characters: size `2`
     - Element 0: drag `Cat_Miso` from Hierarchy
     - Element 1: drag `Cat_Nova` from Hierarchy

3. **Element 1** (Capybaras):
   - Team Name: `Capybaras`
   - Team Color: green `(0.5, 0.8, 0.3, 1)`
   - Characters: size `2`
     - Element 0: drag `Capy_Bubba` from Hierarchy
     - Element 1: drag `Capy_Coco` from Hierarchy

---

## Step 12: Create the UI

### 12a. Create Canvas

1. Hierarchy → right-click → **UI → Canvas**
2. Select the Canvas and configure:
   - Render Mode: **Screen Space - Overlay**
   - **Canvas Scaler** component:
     - UI Scale Mode: **Scale With Screen Size**
     - Reference Resolution: `1920 × 1080`
     - Match: `0.5`

### 12b. Add HUD Manager

1. Select the Canvas
2. Add the **HUDManager** script

### 12c. Create Timer Text

1. Right-click Canvas → **UI → Text - TextMeshPro**
2. Name it `TimerText`
3. Set Rect Transform:
   - Anchor: top-center
   - Pos: `(0, -30, 0)`
   - Size: `(100, 50)`
4. Set TMP text: `35`, Font Size: `36`, Alignment: Center, Color: white
5. Drag `TimerText` into HUDManager's **Timer Text** slot

### 12d. Create Active Character Display

1. Create another **Text - TextMeshPro** → name `ActiveTeamText`
   - Anchor: top-left, Pos: `(120, -20)`, Size: `(200, 30)`
   - Font Size: `20`, Color: white
2. Create another → name `ActiveCharacterText`
   - Anchor: top-left, Pos: `(120, -50)`, Size: `(200, 30)`
   - Font Size: `16`, Color: white
3. Drag both into HUDManager's **Active Team Text** and **Active Character Text** slots

### 12e. Create Wind Text

1. Create **Text - TextMeshPro** → name `WindText`
   - Anchor: top-right, Pos: `(-120, -20)`, Size: `(150, 30)`
   - Font Size: `18`, Color: `(0.7, 0.85, 1)`
2. Drag into HUDManager's **Wind Text** slot

### 12f. Create Phase Text

1. Create **Text - TextMeshPro** → name `PhaseText`
   - Anchor: top-center, Pos: `(0, -70)`, Size: `(200, 30)`
   - Font Size: `14`, Color: `(0.7, 0.7, 0.7)`
2. Drag into HUDManager's **Phase Text** slot

### 12g. Create Weapon Panel

1. Right-click Canvas → **UI → Panel** → name `WeaponPanel`
2. Set Rect Transform:
   - Anchor: bottom-center
   - Pos: `(0, 40, 0)`
   - Size: `(300, 60)`
3. Add a **Horizontal Layout Group** component:
   - Spacing: `5`
   - Child Alignment: Middle Center
4. Drag `WeaponPanel` into HUDManager's **Weapon Panel** slot

### 12h. Create Weapon Button Prefab

1. Right-click `WeaponPanel` → **UI → Button - TextMeshPro**
2. Name it `WeaponButton`
3. Set size: `(90, 50)`
4. Set the child TMP text: font size `14`
5. Drag `WeaponButton` into `Assets/Prefabs/UI/` to save as prefab
6. **Delete it from the WeaponPanel** (HUDManager creates buttons dynamically)
7. Drag the prefab into HUDManager's **Weapon Button Prefab** slot

### 12i. Create Game Over Panel

1. Right-click Canvas → **UI → Panel** → name `GameOverPanel`
2. Add a **CanvasGroup** component (required for fade animation)
3. Set CanvasGroup: Alpha: `0`, Blocks Raycasts: unchecked, Interactable: unchecked
4. Center it: Anchor stretch-all, Left/Right/Top/Bottom: `200`
5. Set Image color to `(0, 0, 0, 0.8)` (semi-transparent black)
6. Add child **Text - TextMeshPro** → name `GameOverText`
   - Font size: `48`, Alignment: Center, Color: yellow
   - Text: `Team Wins!`
7. Add child **UI → Button - TextMeshPro** → name `RestartButton`
   - Set button text to `Restart`
   - Position below the text
8. Drag into HUDManager slots:
   - `GameOverPanel` → **Game Over Panel**
   - `GameOverText` → **Game Over Text**
   - `RestartButton` → **Restart Button**

### 12j. Health Bars (optional for first test)

For each of the 4 characters, create a health bar:

1. Create **UI → Image** → name `HealthBar_Miso`
2. Set Image Type: **Filled**, Fill Method: Horizontal
3. Set color to green
4. Size: `(100, 10)`, position in a team panel

Drag all 4 health bar Images into HUDManager's **Health Bar Fills** array (size 4).

> Health bars are optional for initial testing — the game works without them.

### 12k. Final HUD wiring

Select the Canvas and fill in remaining HUDManager slots:
- **World Canvas**: drag the Canvas itself (for damage popups)

Now go back to the **GameManager** object and drag the Canvas into the **Hud Manager** slot.

---

## Step 13: Create Death Zone

1. Hierarchy → right-click → **Create Empty** → name `DeathZone`
2. Add **BoxCollider2D**:
   - Is Trigger: **checked**
   - Size: `(50, 2)`
   - Offset: `(0, 0)`
3. Add the **DeathZone** script
4. Set **Layer** to **Boundary**
5. Position at `(12.5, -5, 0)` — below the map where characters would drown

---

## Step 14: Create Side Boundaries (Optional)

To prevent characters from walking off the map edges:

1. Create Empty → name `LeftWall`
   - Add **BoxCollider2D**: Size `(1, 20)`, not trigger
   - Position: `(-0.5, 5, 0)`
   - Layer: **Boundary**

2. Create Empty → name `RightWall`
   - Add **BoxCollider2D**: Size `(1, 20)`, not trigger
   - Position: `(25.5, 5, 0)`
   - Layer: **Boundary**

---

## Step 15: Save the Scene

1. **File → Save As** → navigate to `Assets/Scenes/`
2. Save as `GameScene.unity`
3. Go to **File → Build Profiles**
4. Drag `GameScene` into the **Scenes In Build** list

---

## Step 16: Test in Editor

1. Press the **Play** button (top-center)
2. You should see:
   - Characters drop onto terrain via gravity
   - Timer counting down from 35
   - Active character indicator in HUD

### Editor Controls

| Input | Action |
|-------|--------|
| **Arrow Keys** or **A/D** | Move active character left/right |
| **Space** | Jump |
| **Mouse position** | Aim direction (from character to cursor) |
| **Left-click hold** | Charge power (power bar fills up) |
| **Left-click release** | Fire projectile |
| **1 / 2 / 3** | Select Carrot / Bomb / Banana |

### What to verify

- [ ] Characters land on terrain and don't fall through
- [ ] Arrow keys move the active character
- [ ] Space makes the character jump
- [ ] Clicking and releasing fires a projectile
- [ ] Projectile arcs with gravity and wind
- [ ] Hitting terrain creates a destruction crater
- [ ] Characters take damage from nearby explosions
- [ ] Turn switches to the other team after a shot resolves
- [ ] Camera follows the projectile, then pans to next character
- [ ] Timer expiration ends the turn without firing
- [ ] Character death triggers elimination
- [ ] Game Over displays when all characters on one team die
- [ ] Restart button resets everything

### Troubleshooting

| Problem | Fix |
|---------|-----|
| Characters fall through terrain | Terrain must have PolygonCollider2D; character must be on Characters layer; collision matrix must allow Characters↔Terrain |
| "TMPro not found" error | Window → TextMeshPro → Import TMP Essential Resources |
| Terrain destruction doesn't work | Terrain sprite must have **Read/Write Enabled** in import settings |
| Projectile doesn't collide with terrain | Projectile must be on Projectiles layer; collision matrix must allow Projectiles↔Terrain |
| No input response | Check that InputManager has PlayerInputHandler, and Game Camera field is assigned |
| Characters don't take explosion damage | ProjectileBase uses `Physics2D.OverlapCircleAll` — characters need a Collider2D |
| Camera doesn't move | Main Camera must have GameCamera script; TurnManager must reference it |
| Weapons array empty | GameManager → Weapons array must have 3 WeaponData assets assigned |
| NullReferenceException on start | Check all Inspector references on GameManager, TurnManager, and TeamManager |

---

## Step 17: Build for iOS

1. **File → Build Profiles** → select your iOS profile
2. Click **Build**
3. Choose output folder: `Builds/iOS`
4. Wait for build (3-10 minutes first time)
5. Open the generated `Unity-iPhone.xcodeproj` in Xcode
6. In Xcode:
   - Select the **Unity-iPhone** target
   - Go to **Signing & Capabilities**
   - Set **Team** to your Apple Developer account
   - Connect your iPhone via USB
   - Click **Run** (▶)

### Common iOS Build Issues

| Issue | Fix |
|-------|-----|
| Signing error | Set Team in Xcode Signing & Capabilities |
| "Unsupported architecture" | Player Settings → ARM64 only |
| Touch not working | PlayerInputHandler handles touch automatically — verify it's in the scene |
| Low FPS | Profile with Xcode Instruments; consider reducing terrain texture size |

---

## Inspector Reference Cheat Sheet

Quick reference for every Inspector slot that needs to be wired:

### GameManager (on `GameManager` object)

| Slot | Drag This |
|------|-----------|
| Turn Manager | `GameManager` (same object) |
| Team Manager | `GameManager` (same object) |
| Game Camera | `Main Camera` |
| Input Handler | `InputManager` |
| Hud Manager | `Canvas` |
| Terrain | `Terrain` |
| Weapons [0] | `Weapon_Carrot` (ScriptableObject) |
| Weapons [1] | `Weapon_Bomb` (ScriptableObject) |
| Weapons [2] | `Weapon_Banana` (ScriptableObject) |

### TurnManager (on `GameManager` object)

| Slot | Drag This |
|------|-----------|
| Game Manager | `GameManager` (same object) |
| Team Manager | `GameManager` (same object) |
| Game Camera | `Main Camera` |
| Input Handler | `InputManager` |

### TeamManager (on `GameManager` object)

| Slot | Value |
|------|-------|
| Teams [0] → Team Name | `Cats` |
| Teams [0] → Team Color | Orange |
| Teams [0] → Characters [0] | `Cat_Miso` |
| Teams [0] → Characters [1] | `Cat_Nova` |
| Teams [1] → Team Name | `Capybaras` |
| Teams [1] → Team Color | Green |
| Teams [1] → Characters [0] | `Capy_Bubba` |
| Teams [1] → Characters [1] | `Capy_Coco` |

### PlayerInputHandler (on `InputManager` object)

| Slot | Drag This |
|------|-----------|
| Game Camera | `Main Camera` |

### CharacterController2D (on each character)

| Slot | Value |
|------|-------|
| Ground Layers | `Terrain` + `Boundary` checked |

### Projectile Prefabs (CarrotProjectile, BombProjectile, BananaProjectile)

| Slot | Value |
|------|-------|
| Trail Renderer | (optional) self-reference if you added one |
| Explosion VFX Prefab | (optional) assign when you create explosion particles |

---

## Hierarchy Summary

When complete, your Hierarchy should look like:

```
GameScene
├── Main Camera          [GameCamera]
├── GameManager          [GameManager, TurnManager, TeamManager]
├── InputManager         [PlayerInputHandler]
├── Terrain              [SpriteRenderer, PolygonCollider2D, TerrainDestruction]
├── Cat_Miso             [Rigidbody2D, CapsuleCollider2D, CharacterController2D]
├── Cat_Nova             [Rigidbody2D, CapsuleCollider2D, CharacterController2D]
├── Capy_Bubba           [Rigidbody2D, CapsuleCollider2D, CharacterController2D]
├── Capy_Coco            [Rigidbody2D, CapsuleCollider2D, CharacterController2D]
├── DeathZone            [BoxCollider2D (trigger), DeathZone]
├── LeftWall             [BoxCollider2D]
├── RightWall            [BoxCollider2D]
├── Canvas               [Canvas, CanvasScaler, HUDManager]
│   ├── TimerText        [TextMeshProUGUI]
│   ├── ActiveTeamText   [TextMeshProUGUI]
│   ├── ActiveCharacterText [TextMeshProUGUI]
│   ├── WindText         [TextMeshProUGUI]
│   ├── PhaseText        [TextMeshProUGUI]
│   ├── WeaponPanel      [HorizontalLayoutGroup]
│   └── GameOverPanel    [CanvasGroup]
│       ├── GameOverText [TextMeshProUGUI]
│       └── RestartButton [Button]
└── EventSystem          [auto-created with Canvas]
```

---

## Key Values Quick Reference

| Parameter | Value | Where to Set |
|-----------|-------|-------------|
| World Width | 25 units | Terrain sprite + GameCamera |
| Camera Ortho Size | 5 | GameCamera |
| Turn Timer | 35 seconds | TurnManager |
| Gravity | -9.81 | Edit → Project Settings → Physics 2D |
| Walk Speed | 3 units/sec | CharacterController2D |
| Jump Force | 9 | CharacterController2D |
| Max Health | 100 | CharacterController2D |
| Resolving Delay | 1.5 seconds | TurnManager |
| Pan Duration | 1 second | TurnManager / GameCamera |
| Shake Duration | 0.3 seconds | GameCamera |
| Max Wind | 5 | GameManager |

---

*Follow these steps exactly and you'll have a playable Cats vs Capybaras prototype running in Unity 6.3.*
