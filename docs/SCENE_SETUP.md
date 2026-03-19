# Cats vs Capybaras — Scene Setup Guide
**GameScene.unity** | Unity 2D URP | Target: iPhone + iPad

---

## Quick Reference: Key Numbers

| Setting | Value |
|---|---|
| Pixels Per Unit (PPU) | **100** |
| Camera Ortho Size | **4.0** |
| Terrain world size | **28.14 × 15.36 units** |
| Terrain position | **(14.07, 2.30, 0)** — centers it so ground ≈ Y=0 |
| Character prefab scale | **(0.55, 0.55, 1)** — Worms-style small |
| Cat spawn X range | **2.3 – 3.9** |
| Capybara spawn X range | **23.1 – 24.8** |

---

## Step 1 — Terrain GameObject

1. In GameScene, create an empty GameObject → name it **`Terrain`**
2. Add component: **SpriteRenderer**
   - Sprite: `desert-terrain` (the single sprite from desert-terrain.png)
   - Sorting Layer: `Background` (create this layer if it doesn't exist)
   - Order in Layer: `0`
3. Add component: **PolygonCollider2D**
   - Unity will auto-generate the collision shape from the sprite alpha
4. Add component: **TerrainDestruction** (your existing script)
5. Set Transform:
   - Position: `(14.07, 2.30, 0)`
   - Scale: `(1, 1, 1)`
6. Set Layer to **`Terrain`** (create if needed — used by `CharacterController2D.groundLayers`)

> **Why position (14.07, 2.30)?**
> Terrain is 28.14 units wide (center pivot = 14.07 on X) and 15.36 units tall.
> The playable ground surface is ~35% from the bottom of the image, which puts it at ≈ Y=0.

---

## Step 2 — Camera

1. Select the **Main Camera** in the scene
2. Set **Projection**: Orthographic
3. Set **Size**: `4`
4. Attach **GameCamera.cs** (already updated with correct bounds)
5. Set Transform Position Z to `-10`

**Inspector values for GameCamera:**
- World Width: `28.14`
- World Min Y: `-3`
- World Max Y: `10`
- Default Ortho Size: `4`
- Character Follow Damping: `0.15`
- Projectile Follow Damping: `0.05`

---

## Step 3 — Character Prefab Setup

For each character prefab (Cat and Capybara):

1. Add **SpriteRenderer**
   - Sprite: `Idle` (from mochi-character-board)
   - Sorting Layer: `Characters`
   - Order in Layer: `1`
2. Add **Rigidbody2D**
   - Gravity Scale: `3`
   - Freeze Rotation: ✓ Z
   - Collision Detection: `Continuous`
3. Add **CapsuleCollider2D**
   - Size: `(0.5, 0.8)` — tuned to character visual bounds at scale 0.55
   - Direction: Vertical
4. Add **CharacterController2D** (your existing script)
   - Walk Speed: `3`
   - Jump Force: `9`
   - Max Health: `100`
5. Add **SpriteAnimator** (new script)
   - See Step 4 below for clip setup
6. Set Transform Scale: `(0.55, 0.55, 1)`

---

## Step 4 — SpriteAnimator Clip Setup

In the **SpriteAnimator** Inspector, add 6 clips:

| State | Frames | FPS | Loop |
|---|---|---|---|
| `Idle` | `[Idle]` | 1 | ✓ |
| `Walking` | `[Walk_R, Walk_R2]` | 8 | ✓ |
| `Jumping` | `[Jump]` | 1 | ✓ |
| `Falling` | `[Jump]` | 1 | ✓ |
| `Hurt` | `[Hurt]` | 6 | ✗ |
| `Dead` | `[Downed]` | 1 | ✗ |

> **Note:** `Walk_L` is available if you want leftward walking to use a different pose.
> Currently `CharacterController2D` handles direction by flipping `SpriteRenderer.flipX`,
> so Walk_R works for both directions automatically.

---

## Step 5 — GameManager GameObject

1. Create empty GameObject → **`GameManager`**
2. Attach: `GameManager`, `TurnManager`, `TeamManager`, `PlayerInputHandler`, `HUDManager`
3. Wire all Inspector references:
   - GameManager → assign TurnManager, TeamManager, GameCamera, PlayerInputHandler, HUDManager, TerrainDestruction
   - TurnManager → assign GameManager, TeamManager, GameCamera, PlayerInputHandler
   - TeamManager → define 2 teams:
     - Team 0: Name=`Cats`, Color=orange, Characters=[Cat1, Cat2]
     - Team 1: Name=`Capybaras`, Color=green, Characters=[Capy1, Capy2]
4. Populate **Weapons** array with: `Carrot.asset`, `Bomb.asset`, `Banana.asset`

---

## Step 6 — Character Placement in Scene

Place 4 character GameObjects in the scene at these positions:

| Name | Team | Position |
|---|---|---|
| `Cat_Miso` | 0 (Cats) | `(2.3, 0.5, 0)` |
| `Cat_Nova` | 0 (Cats) | `(3.9, 0.5, 0)` |
| `Capy_Bubba` | 1 (Capys) | `(23.1, 0.5, 0)` |
| `Capy_Lulu` | 1 (Capys) | `(24.8, 0.5, 0)` |

> Y=0.5 places them just above ground level. `CharacterController2D` will
> handle snapping to terrain via physics on first frame.

---

## Step 7 — Sorting Layers (Project Settings)

Go to **Edit → Project Settings → Tags and Layers → Sorting Layers**
Add in this order (bottom to top):

1. `Background` — terrain, sky
2. `Midground` — props, decorations
3. `Characters` — all character sprites
4. `Projectiles` — weapons in flight
5. `UI` — HUD overlays

---

## Step 8 — Physics Layers (Layer Collision Matrix)

Go to **Edit → Project Settings → Physics 2D → Layer Collision Matrix**

Create layers: `Terrain`, `Characters`, `Projectiles`

| | Terrain | Characters | Projectiles |
|---|---|---|---|
| Terrain | ✓ | ✓ | ✓ |
| Characters | ✓ | ✗ | ✓ |
| Projectiles | ✓ | ✓ | ✗ |

> Characters don't collide with each other — they walk through,
> matching the HTML prototype behaviour.

---

## Step 9 — Canvas / UI (iPhone + iPad)

1. Create a **Canvas** → **`HUD Canvas`**
2. Render Mode: `Screen Space - Overlay`
3. Add **Canvas Scaler**:
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1334 × 750` (iPhone landscape base)
   - Screen Match Mode: `Match Width or Height`
   - Match: `0.5` (balances iPhone and iPad)
4. Attach **HUDManager** to the canvas

---

## Verified Numbers Summary

```
desert-terrain.png
  isReadable:    1        ← FIXED (was 0, broke TerrainDestruction)
  maxTextureSize: 4096   ← FIXED (was 2048, cropped 2814px terrain)
  PPU:           100
  Sprite Mode:   Multiple (single sprite)

mochi-character-board.png
  Sprites:       12 named sprites (6 cols × 2 rows)
  Cell size:     117 × 161 px (row 1), 117 × 169 px (row 2)
  PPU:           100

Camera
  Ortho Size:    4.0
  Visible area:  14.2 × 8.0 units (iPhone 16:9) — 51% of map
                 10.7 × 8.0 units (iPad 4:3) — 38% of map

Character
  Sprite size:   1.17 × 1.61 units (raw, at 100 PPU)
  Prefab scale:  0.55
  On screen:     ~11% of screen height — small/Worms-style ✓
```
