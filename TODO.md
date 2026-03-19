# Cats vs Capybaras — MVP TODO

> Last Updated: March 15, 2026
>
> Current Status: Playable prototype with core mechanics working
> Target: Shippable MVP on iOS/iPad

---

## 🚨 Critical Path (Week 1 — Ship Blockers)

These must be done before launch. They directly affect gameplay feel and first impression.

### 1. Audio SFX — **HIGHEST IMPACT**
- [ ] Jump/land sound
- [ ] Weapon fire sound
- [ ] Explosion sound
- [ ] Hit/impact sound
- [ ] Consider: UI confirmation sounds

**Why:** Mobile games feel completely dead without sound. This is the quickest high-impact improvement.
**Effort:** 2-3 hours (use freesound.org or OpenGameArt)
**Owner:** —

### 2. Character Sprite Sheets
- [ ] Cat sprite (idle, walk, jump, hurt, dead)
- [ ] Capybara sprite (idle, walk, jump, hurt, dead)
- [ ] Replace placeholder colored blocks with actual sprites
- [ ] Verify sprite scaling/proportions in-game

**Why:** First visual impression. Blocks look amateurish; sprites say "real game."
**Effort:** 4-6 hours (asset store, commission, or free assets)
**Owner:** —

### 3. Fix Character Stuck Bug (Canyon)
- [ ] Debug why character gets stuck in middle canyon
- [ ] Check terrain collision physics (PolygonCollider2D path accuracy)
- [ ] Test movement across entire map width
- [ ] Verify character can escape canyon if pushed there

**Why:** Playability blocker. Game is unplayable if characters trap themselves.
**Effort:** 1-2 hours
**Owner:** —

### 4. Critical HUD Elements
- [ ] Health bar (current/max per character)
- [ ] Ammo/weapon count (shots remaining)
- [ ] Turn indicator (whose turn, team color)
- [ ] Wind indicator (visual direction + magnitude)
- [ ] Timer display (turn time remaining)

**Why:** Players need critical game state info to play effectively.
**Effort:** 1-2 hours
**Owner:** —

### 5. Portrait Orientation Fix
- [ ] Detect when device rotates to portrait
- [ ] Either: Force landscape OR show appropriate letterbox/pillarbox
- [ ] Hide terrain bottom/void in portrait if showing
- [ ] Test both iPad and iPhone orientations

**Why:** Users WILL rotate their devices. Can't let them see off-map content.
**Effort:** 1 hour
**Owner:** —

---

## 🎨 High Priority (Week 2 — Game Feel)

Ship these right after critical path. They make the game feel polished.

### 6. Parallax Background
- [ ] Create 2-3 background layers (clouds, far hills, near hills)
- [ ] Implement parallax scrolling with camera movement
- [ ] Layer 1 (far): 20% camera speed
- [ ] Layer 2 (mid): 50% camera speed
- [ ] Layer 3: Terrain (100% camera speed)

**Why:** Massive perceived quality increase. 2-3 hours for huge visual impact.
**Effort:** 2-3 hours
**Owner:** —

### 7. Projectile & Crater Visuals
- [ ] Replace orange dot with animated carrot sprite
- [ ] Design/create crater impact visual
- [ ] Craters should feel destructive (not just round holes)
- [ ] Consider: Particle effects on impact

**Why:** Destruction feedback makes gameplay feel satisfying.
**Effort:** 2-3 hours
**Owner:** —

### 8. Win/Lose/Game Over Screens
- [ ] Determine winning conditions (last team alive)
- [ ] Create "Team X Wins!" screen
- [ ] Show: Winner, final team status, restart button
- [ ] Add leaderboard/score tracking if time

**Why:** Game needs a clean end state and restart flow.
**Effort:** 1-2 hours
**Owner:** —

### 9. Real Device Testing (iPad/iPhone)
- [ ] Test on actual iPad (primary target)
- [ ] Test on actual iPhone (secondary)
- [ ] Check: Touch responsiveness, performance (60 FPS?), notch handling
- [ ] Document any device-specific bugs

**Why:** Simulator ≠ Reality. Will find platform-specific bugs.
**Effort:** 1-2 hours (testing) + bug fixes TBD
**Owner:** —

---

## 📋 Medium Priority (Post-MVP Polish)

Nice to have, but can ship without these.

- [ ] Character walk/jump animations (not just movement)
- [ ] Screen shake on hits (implemented but tune intensity)
- [ ] Knockback/ragdoll physics on big hits
- [ ] Hit feedback numbers ("+10 dmg" popups)
- [ ] Main menu screen
- [ ] Settings menu (volume, difficulty, accessibility)

---

## 🎁 Nice to Have (Future Updates)

Lower ROI, save for post-launch updates or patches.

- [ ] Particle effects (explosions, trail effects)
- [ ] Background music
- [ ] More weapon types (if time permits)
- [ ] Online multiplayer (future)
- [ ] Character skins/cosmetics
- [ ] Leaderboards

---

## ⚠️ Known Bugs & Tech Debt

| Bug | Status | Priority |
|-----|--------|----------|
| Character stuck in canyon | Open | CRITICAL |
| Portrait orientation shows void | Open | CRITICAL |
| Carrot projectile is orange dot | Open | HIGH |
| Craters are plain round holes | Open | HIGH |
| No audio | Open | HIGH |
| Block sprites look bad | Open | HIGH |
| Camera doesn't follow properly | FIXED | — |

---

## 📊 Phase Timeline

```
Week 1 (MVP Shipping):
  Mon-Tue: Audio SFX + character sprites (parallel)
  Wed:     Bug fixes (stuck character, portrait mode)
  Thu:     HUD implementation
  Fri:     Testing + final polish

Week 2 (Post-Launch Polish):
  Mon-Tue: Parallax + projectile visuals
  Wed-Thu: Game over screens + more testing
  Fri:     Real device testing + bug fixes
```

---

## 🎯 Success Criteria for MVP Launch

- [ ] Game runs at 60 FPS on iPad Air 2023+
- [ ] Character doesn't get stuck anywhere
- [ ] No void/off-map content visible in any orientation
- [ ] Players can hear all critical sounds
- [ ] Can win a game and see winner screen
- [ ] Touch controls feel responsive
- [ ] No critical crashes on device

---

## 📝 Notes

- **Sprites are the single biggest visual upgrade** — prioritize this after audio
- **Audio makes a HUGE difference** — even simple sounds elevate perception massively
- **Portrait mode will break in real usage** — must fix before device testing
- **Device testing will find surprises** — leave time for unexpected bugs
- **Parallax is quick polish** — schedule for Week 2 even if sounds ambitious

---

Generated: 2026-03-15 | Cats vs Capybaras MVP Roadmap
