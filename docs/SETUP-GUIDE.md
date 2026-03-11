# Cats vs Capybaras — Unity Setup Guide

## Prerequisites

- **macOS** with Apple Silicon (M1/M2/M3/M4) — Rosetta 2 must be installed
- **Unity Hub** (download from unity.com)
- **Unity 6.3 LTS** (install via Unity Hub)
- **Xcode 15+** (from Mac App Store — required for iOS builds)
- **Apple Developer Account** ($99/year — needed only when ready to deploy to App Store)

---

## Step 1: Create the Unity Project

1. Open **Unity Hub** → click **New Project**
2. Select **2D (URP)** template
3. Name it `CatsVsCapybaras`
4. Set location to your preferred folder
5. Click **Create project**

> If Unity Editor fails to open, ensure Rosetta 2 is installed:
> ```
> softwareupdate --install-rosetta
> ```

---

## Step 2: Configure for iOS

### 2a. Select iOS Platform
1. Go to **File → Build Profiles**
2. Click **Add Profile**
3. Select **iOS** as the platform

### 2b. Configure Player Settings
1. Go to **Edit → Project Settings → Player**
2. Configure the following:
   - **Company Name**: Your name or studio
   - **Product Name**: Cats vs Capybaras
   - **Bundle Identifier**: `com.yourname.catsvscapybaras`
   - **Target minimum iOS Version**: 15.0
   - **Scripting Backend**: IL2CPP
   - **Target Architectures**: ARM64 only
   - **Orientation**: Landscape Left + Landscape Right

---

## Step 3: Create Folder Structure

In the Unity Project window, create this folder hierarchy:

```
Assets/
├── Scripts/
│   ├── Core/           ← GameManager.cs, TurnManager.cs
│   ├── Characters/     ← CharacterController2D.cs
│   ├── Weapons/        ← ProjectileBase.cs, WeaponDefinitions.cs
│   ├── Environment/    ← TerrainDestruction.cs
│   ├── Camera/         ← CameraController.cs
│   ├── Input/          ← TouchInputManager.cs
│   └── UI/             ← UIManager.cs
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
└── Materials/
```

---

## Step 4: Import the Starter Scripts

1. Copy all `.cs` files from the `Scripts/` folder (included with this package) into `Assets/Scripts/` following the subfolder structure above
2. Wait for Unity to compile — check the Console for errors
3. If you see `TMPro` errors, go to **Window → TextMeshPro → Import TMP Essential Resources**

---

## Step 5: Create Scenes

### Main Menu Scene
1. **File → New Scene** → save as `Assets/Scenes/MainMenu.unity`
2. Add a Canvas with a "Play" button

### Game Scene
1. **File → New Scene** → save as `Assets/Scenes/GameScene.unity`
2. This is where you'll build the main gameplay
3. Add both scenes to **File → Build Profiles** (MainMenu at index 0, GameScene at index 1)

---

## Step 6: Set Up Physics

### Layers (Edit → Project Settings → Tags and Layers)

| Layer # | Name        | Purpose                    |
|---------|-------------|----------------------------|
| 8       | Terrain     | Destructible ground        |
| 9       | Characters  | Player characters          |
| 10      | Projectiles | Fired projectiles          |
| 11      | Boundary    | Invisible walls/floor      |

### Collision Matrix (Edit → Project Settings → Physics 2D)

| -             | Terrain | Characters | Projectiles | Boundary |
|---------------|---------|------------|-------------|----------|
| **Terrain**   | —       | ✓          | ✓           | —        |
| **Characters**| ✓       | ✓          | —           | ✓        |
| **Projectiles**| ✓      | ✓          | —           | ✓        |
| **Boundary**  | —       | ✓          | ✓           | —        |

- Characters collide with Characters (they block each other)
- Projectiles do NOT collide with the firing character (handled in code via owner reference)

### Tags (Edit → Project Settings → Tags and Layers → Tags)

- `Cat` — for cat characters
- `Capybara` — for capybara characters
- `Terrain` — for destructible terrain
- `DeathZone` — for bottom boundary (instant kill)

---

## Step 7: Build the Game Scene

### 7a. Terrain
1. Create or import a terrain texture (PNG, ~2500×500px, with alpha for holes)
2. Create a new GameObject: **Terrain**
   - Add `SpriteRenderer` → assign terrain sprite
   - Add `PolygonCollider2D` (Unity auto-generates from sprite shape)
   - Add `TerrainDestruction` script
   - Set Layer to **Terrain**

### 7b. Characters
1. Create a character prefab:
   - Sprite with `SpriteRenderer`
   - `Rigidbody2D` (Gravity Scale: 1, Freeze Rotation Z: ✓)
   - `CapsuleCollider2D` (sized to character)
   - `CharacterController2D` script
   - Set Layer to **Characters**
2. Duplicate for each character (2 cats, 2 capybaras)
3. Position cats on the left (~x=2, x=3.5), capybaras on the right (~x=20.5, x=22)

### 7c. Camera
1. Select **Main Camera**
   - Set to **Orthographic**, Size: 5
   - Add `CameraController` script
   - Position at roughly center of map

### 7d. Managers
1. Create empty GameObject: **GameManager**
   - Add `GameManager` script
   - Add `TurnManager` script
   - Assign references in Inspector
2. Create empty GameObject: **InputManager**
   - Add `TouchInputManager` script

### 7e. UI
1. Create **Canvas** (Screen Space - Overlay, Scale With Screen Size, Reference: 1920×1080)
2. Add UI elements: timer text, health bars, weapon panel, wind indicator
3. Add `UIManager` script to Canvas, wire up references

### 7f. Boundaries
1. Create invisible BoxCollider2D objects around the world edges
2. Bottom boundary: add tag `DeathZone` (characters that fall here are eliminated)
3. Set Layer to **Boundary**

---

## Step 8: Create Weapon ScriptableObjects

1. Right-click in `Assets/ScriptableObjects/Weapons/`
2. **Create → Cats vs Capybaras → Weapon Data**
3. Create three weapons:

**Carrot**
- Damage: 32, Radius: 1.9
- Min/Max Power: 5/15
- Ammo: -1 (infinite)
- Assign CarrotProjectile prefab

**Bomb**
- Damage: 55, Radius: 3.25
- Min/Max Power: 4/12
- Ammo: 2 per round
- Assign BombProjectile prefab

**Banana**
- Damage: 38, Radius: 2.4
- Min/Max Power: 5/14
- Ammo: 3 per round
- Assign BananaProjectile prefab

---

## Step 9: Create Projectile Prefabs

For each weapon:
1. Create a new GameObject with a `SpriteRenderer`
2. Add `Rigidbody2D` (Gravity Scale: 1)
3. Add `CircleCollider2D` (small, ~0.1 radius)
4. Attach the correct projectile script (`CarrotProjectile`, `BombProjectile`, or `BananaProjectile`)
5. Optionally add a `TrailRenderer`
6. Set Layer to **Projectiles**
7. Save as prefab in `Assets/Prefabs/Projectiles/`

For the **Bomb**: also add a `PhysicsMaterial2D` with Bounciness: 0.8, Friction: 0.3

---

## Step 10: Test in Editor

1. Press **Play** in the Game Scene
2. Use mouse input (TouchInputManager supports mouse for editor testing)
3. Check the Console for errors
4. Verify: characters respond to input, projectiles fire, terrain destructs, camera follows

---

## Step 11: Build for iOS

1. **File → Build Profiles** (make sure your iOS profile is active)
2. Click the **Build** button in the iOS profile
3. Choose an output folder (e.g., `Builds/iOS`)
4. Wait for the build to complete
5. Open the generated `.xcodeproj` in Xcode
6. In Xcode:
   - Set your **Team** (Signing & Capabilities)
   - Connect your iPhone or select a Simulator
   - Click **Run** (▶)
7. First build takes ~5-10 minutes

### Common iOS Build Issues
- **Signing errors**: Ensure you have a valid provisioning profile in Xcode
- **"Unsupported architecture"**: Make sure ARM64 only is selected in Unity Player Settings
- **Touch not working**: Verify `TouchInputManager` is in the scene and active

---

## Step 12: Connect Claude to Unity (Optional)

For AI-assisted development, you can connect Claude to your Unity project:

1. **Claude Code CLI**: Use Claude Code from your terminal in the project directory. Claude can read/write `.cs` files, run Unity tests, and help debug.

2. **MCP Plugins** (community-maintained):
   - [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)
   - [CoplayDev/unity-mcp](https://github.com/AltanAkwororth/unity-mcp)
   - These let Claude interact directly with the Unity Editor (create GameObjects, modify components, etc.)

3. Install via Unity Package Manager → Add package from git URL

---

## Implementation Sprint Plan

| Week | Focus | Key Deliverables |
|------|-------|-----------------|
| 1 | Terrain + Characters | Destructible terrain, character movement, gravity |
| 2 | Projectiles + Camera | Carrot weapon working, camera follows projectile |
| 3 | Turn System + Touch | Full turn loop, D-pad movement, drag-to-aim |
| 4 | UI + More Weapons | Health bars, timer, bomb + banana weapons |
| 5 | Audio + Polish | SFX, music, particle effects, screen shake |
| 6 | iOS Build + Testing | Build to device, touch optimization, performance |

---

## Quick Reference: Key Values

| Parameter | Value | Location |
|-----------|-------|----------|
| World Width | ~25 units | Terrain sprite scale |
| Canyon | x=8.5 to x=16.5 | Terrain texture gap |
| Camera Ortho Size | 5 | Main Camera |
| Turn Timer | 35 seconds | TurnManager inspector |
| Gravity | -9.81 | Physics 2D settings |
| Character Walk Speed | 3 units/sec | CharacterController2D |
| Explosion Pause | 1 second | CameraController |
| Pan Duration | 1 second | CameraController |

---

*Good luck building Cats vs Capybaras! 🐱 vs 🦫*
