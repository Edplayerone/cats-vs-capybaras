# Bear Game - Sprite Production Guide

## Reference Style
Kawaii/chibi cream-colored bear, soft rounded body, small brown ears, black dot eyes, pink cheeks, short limbs, warm golden-brown palette. Flat shading with soft outline.

---

## Step-by-Step Process

### Step 1: Establish the Character Sheet (BASE PROMPT)

Upload your reference image to ChatGPT and use this prompt:

> **Prompt:**
> "Using this character as the exact reference, create a clean character model sheet on a flat transparent background. Show the character in 4 views: front, back, 3/4 left, 3/4 right. Keep the exact same art style — kawaii, soft fur texture, cream/golden color, pink cheeks, small brown ears, black dot eyes. Each view should be the same size and evenly spaced. No background, no shadows on the ground. PNG format."

Save result to: `reference/character-sheet.png`

---

### Step 2: Idle Animation Sprites

> **Prompt:**
> "Using this character reference, create a horizontal sprite strip showing 4 frames of a gentle idle/breathing animation. Frame 1: neutral standing. Frame 2: slight squish down. Frame 3: neutral. Frame 4: slight stretch up. Same art style, same character, flat transparent background. Each frame the same size (256x256px), evenly spaced in a row. PNG format."

Save result to: `sprites/idle/idle-strip.png`

---

### Step 3: Walk Cycle Sprites

> **Prompt:**
> "Using this character reference, create a horizontal sprite strip showing 6 frames of a walk cycle (side view, facing right). Frame 1: standing. Frame 2: right foot forward. Frame 3: passing. Frame 4: left foot forward. Frame 5: passing. Frame 6: back to start. Same kawaii art style, same character design. Flat transparent background. Each frame 256x256px, evenly spaced. PNG format."

Save result to: `sprites/walk/walk-right-strip.png`

---

### Step 4: Jump Sprites

> **Prompt:**
> "Using this character reference, create a horizontal sprite strip showing 4 frames of a jump animation (front view). Frame 1: crouch/squat preparing to jump. Frame 2: launching upward with arms up. Frame 3: at peak of jump, happy expression. Frame 4: landing with slight squish. Same kawaii art style, flat transparent background. Each frame 256x256px. PNG format."

Save result to: `sprites/jump/jump-strip.png`

---

### Step 5: Run Cycle Sprites

> **Prompt:**
> "Using this character reference, create a horizontal sprite strip showing 6 frames of a fast run cycle (side view, facing right). More exaggerated motion than walking — body leaning forward, arms pumping, legs with wider stride. Same kawaii bear character, same art style. Flat transparent background. Each frame 256x256px. PNG format."

Save result to: `sprites/run/run-right-strip.png`

---

### Step 6: Emotion Sprites

> **Prompt:**
> "Using this character reference, create 6 emotion portraits of this kawaii bear in a grid (2 rows x 3 columns). Emotions: happy (big smile, closed eyes), sad (teardrop, frown), angry (puffed cheeks, furrowed brows), surprised (wide eyes, open mouth), sleepy (half-closed eyes, yawning), love (heart eyes, blushing). Same art style, flat transparent background. Each portrait 256x256px. PNG format."

Save result to: `sprites/emotions/emotions-grid.png`

---

### Step 7: Collectible Items

> **Prompt:**
> "In the same kawaii art style as this bear character, create a set of 8 collectible game items in a grid (2 rows x 4 columns): golden honey jar, red apple, pink donut, blue gem, silver coin, green leaf, orange star, purple mushroom. Cute, rounded, soft shading. Flat transparent background. Each item 128x128px. PNG format."

Save result to: `sprites/items/collectibles-grid.png`

---

## Tips for Best Results

1. **Always upload the reference image** with every prompt so the style stays consistent
2. **Say "exact same character"** to reduce style drift
3. **Specify "flat transparent background"** every time — makes game integration easier
4. **If a result drifts**, reply: "Make it match the reference more closely — same proportions, same colors, same line weight"
5. **Regenerate** if needed — AI image gen isn't always consistent on first try
6. **Request PNG** format for transparency support

## File Organization

```
bear-game/
├── reference/           <- Original art + character sheet
│   └── original-character.png
├── sprites/
│   ├── idle/            <- Breathing/idle animation frames
│   ├── walk/            <- Walk cycle frames
│   ├── jump/            <- Jump animation frames
│   ├── run/             <- Run cycle frames
│   ├── emotions/        <- Expression variants
│   └── items/           <- Collectible objects
├── prompts/             <- This guide + any custom prompts
│   └── sprite-guide.md
└── exports/             <- Final processed sprites for game engine
```
