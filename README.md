# Cats vs Capybaras

A 2D turn-based physics combat game inspired by Worms Armageddon. **Target platform: iOS.**

## Architecture Overview

```
src/                          → Copy into Unity's Assets/Scripts/
├── Core/
│   ├── GameManager.cs        Central coordinator: state, weapons, wind, restart
│   └── TurnManager.cs        Turn lifecycle: Action → Firing → Resolving → Transitioning
├── Characters/
│   ├── CharacterController2D.cs  Movement, health, knockback, fall damage, weapon firing
│   └── TeamManager.cs        Team composition, round-robin turns, ammo tracking, win condition
├── Weapons/
│   ├── WeaponData.cs          ScriptableObject: damage, radius, power, ammo
│   ├── ProjectileBase.cs      Abstract base: launch, wind, collision, explosion, terrain destruction
│   └── Projectiles.cs         CarrotProjectile, BombProjectile, BananaProjectile
├── Environment/
│   ├── TerrainDestruction.cs  Pixel-level destruction, texture cloning, collider rebuild
│   └── DeathZone.cs           Kill trigger for map boundaries
├── Camera/
│   └── GameCamera.cs          Follow (character/projectile), pan-to-target, hold, screen shake
├── Input/
│   └── PlayerInputHandler.cs  Touch (iOS) + mouse/keyboard (editor), events for move/aim/fire
├── UI/
│   └── HUDManager.cs          Timer, health bars, weapon panel, wind, game over, damage popups
└── Effects/
    └── ExplosionEffect.cs     Self-destroying particle wrapper
```

## Key Design Decisions

- **Event-driven**: Systems communicate via C# events (`Action<T>`), keeping coupling low
- **Input-agnostic characters**: `CharacterController2D` receives commands (move, fire, jump) — plug in player input or future AI
- **Flexible teams**: `TeamManager.TeamConfig[]` supports any number of teams with any number of characters
- **ScriptableObject weapons**: Create new weapons via `Create → Cats vs Capybaras → Weapon Data`
- **Per-team ammo**: Tracked in `TeamManager` per weapon slot, -1 = infinite
- **Unity 6.3 compatible**: Uses `linearVelocity`, `FindAnyObjectByType`, no deprecated APIs

## Turn Flow

```
BeginTurn()
  ├── Activate character, start 35s timer, enable input
  ├── Phase: Action (player moves, aims, fires)
  │     ├── Timer expires → EndTurn()
  │     └── Fire requested → spawn projectile
  ├── Phase: Firing (camera follows projectile)
  │     └── Projectile hits → explode, damage, terrain destruction
  ├── Phase: Resolving (1.5s pause, check win condition)
  └── Phase: Transitioning (camera pans to next character)
        └── BeginTurn() for next team
```

## Quick Start

1. **Create Unity Project**: Unity Hub → New → 2D (URP) → "CatsVsCapybaras"
2. **Copy scripts**: Copy `src/*` into `Assets/Scripts/`
3. **Import TMPro**: Window → TextMeshPro → Import TMP Essential Resources
4. **Create layers**: Terrain (8), Characters (9), Projectiles (10), Boundary (11)
5. **Create weapon assets**: Right-click → Create → Cats vs Capybaras → Weapon Data (×3)
6. **Build scene**: Terrain sprite + characters + managers + camera + UI canvas
7. **Wire Inspector references**: GameManager → all subsystems; TeamManager → character arrays

See `docs/SETUP-GUIDE.md` for detailed step-by-step instructions.

## Weapon Roster

| Weapon | Damage | Radius | Behavior | Ammo |
|--------|--------|--------|----------|------|
| Carrot | 32 | 1.9 | Direct hit, explodes on contact | ∞ |
| Bomb | 55 | 3.25 | Bounces 3×, 3s fuse timer | 2 |
| Banana | 38 | 2.4 | Arcing spin, explodes on contact | 3 |

## Prototype

Open `prototype/cats-vs-capybaras.html` in a browser to play the reference implementation.

## Project Structure

```
cats-vs-capybaras/
├── src/                Scripts (organized by subsystem)
├── assets/             Art assets and character boards
├── docs/               Setup guide and architecture doc
├── prototype/          Playable HTML5 prototype
└── .git/
```

## Development Roadmap

| Week | Focus | Deliverables |
|------|-------|-------------|
| 1-2 | Terrain + Characters | Destructible terrain, movement, gravity, health |
| 3-4 | Projectiles + Camera | All 3 weapons working, camera follow/pan/shake |
| 5-6 | Turn System + Touch | Full turn loop, iOS touch controls |
| 7-8 | UI + Polish | Health bars, timer, weapon panel, particles |
| 9-10 | Audio + Effects | SFX, music, screen shake, damage popups |
| 11-12 | iOS Build + Testing | Xcode build, device testing, App Store prep |

---

*Built with Unity 6.3 · iOS deployment ready · 13 scripts · 0 compile errors*
