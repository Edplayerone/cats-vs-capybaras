# Cats vs Capybaras

A 2D turn-based physics combat game inspired by Worms Armageddon. **Target platform: iOS App Store.**

## Project Structure

```
cats-vs-capybaras/
├── assets/              # Art, sprites, character designs
│   ├── map-poc.png      # Map proof of concept
│   └── mochi-character-board.png
├── Scripts/             # C# starter scripts (organized by subsystem)
│   ├── Core/            # GameManager, TurnManager
│   ├── Characters/      # CharacterController2D
│   ├── Weapons/         # ProjectileBase, WeaponDefinitions
│   ├── Environment/     # TerrainDestruction
│   ├── Camera/          # CameraController
│   ├── Input/           # TouchInputManager
│   └── UI/              # UIManager
├── docs/                # Documentation
│   ├── SETUP-GUIDE.md   # Step-by-step Unity setup
│   └── CatsVsCapybaras-Unity-DevPackage.docx  # Architecture doc
├── prototype/           # Playable prototypes
│   └── cats-vs-capybaras.html  # Working HTML prototype
└── .git/                # Version control
```

## Quick Start

1. **Read the Setup Guide**: `docs/SETUP-GUIDE.md`
2. **Review Architecture**: `docs/CatsVsCapybaras-Unity-DevPackage.docx`
3. **Test Prototype**: Open `prototype/cats-vs-capybaras.html` in a browser
4. **Create Unity Project**: Follow Step 1-2 in SETUP-GUIDE.md

## Game Design Pillars

- **Turn-Based Combat**: 35-second turns, 5 phases (Move → Aim → Fire → Resolve → Transition)
- **Destructible Terrain**: Pixel-based destruction with dynamic collider updates
- **Physics-Based**: Gravity, wind, projectile arcs
- **2v2 Gameplay**: 2 cats vs 2 capybaras
- **iOS-Ready**: Touch controls (D-pad + drag-to-aim)

## Key Systems

| System | Script | Status |
|--------|--------|--------|
| Game State | GameManager.cs | ✓ Done |
| Turn Flow | TurnManager.cs | ✓ Done |
| Character | CharacterController2D.cs | ✓ Done |
| Projectiles | ProjectileBase.cs + WeaponDefinitions.cs | ✓ Done |
| Terrain | TerrainDestruction.cs | ✓ Done |
| Camera | CameraController.cs | ✓ Done |
| Input | TouchInputManager.cs | ✓ Done |
| UI | UIManager.cs | ✓ Done |

## Development Roadmap

**Week 1-2**: Terrain + Characters (movement, gravity, health)
**Week 3-4**: Projectiles + Camera (carrot weapon, follow mechanics)
**Week 5-6**: Turn System + Touch Input (full game loop)
**Week 7-8**: UI + More Weapons (bomb, banana, health bars)
**Week 9-10**: Audio + Polish (SFX, music, particles, shake)
**Week 11-12**: iOS Build + Testing (Xcode, device testing, submission)

## Character Roster

- **Cats (Team A)** — Team A faction
- **Capybaras (Team B)** — Team B faction
- **Mochi** — New character design (see `assets/mochi-character-board.png`)

## Getting Help

- **Design Questions**: Refer to GDD (in progress)
- **Code Questions**: See inline comments in Scripts/ (XML docs)
- **Setup Issues**: Check SETUP-GUIDE.md troubleshooting section
- **Architecture**: Open CatsVsCapybaras-Unity-DevPackage.docx

## Version Control

```bash
git add .
git commit -m "Initial commit: starter scripts and design assets"
git push origin main
```

## License

TBD

---

*Built with Unity 6.3 LTS. iOS deployment ready.*
