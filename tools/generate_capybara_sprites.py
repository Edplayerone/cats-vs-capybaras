#!/usr/bin/env python3
"""
Cats vs Capybaras — Capybara Character Sprite Sheet Generator
Generates a 5-frame sprite sheet: Idle, Walk1, Walk2, Jump, Hurt, Dead
Style: Chunky pixel art, 32x32 per frame, scaled up to 64x64 for clarity
"""

from PIL import Image, ImageDraw

# Frame size
CELL = 32
SCALE = 4  # Output at 128x128 per frame for visibility
FRAMES = 6  # idle, walk1, walk2, jump, hurt, dead

# Capybara color palette
BODY = (139, 105, 72)        # Warm brown
BODY_DARK = (115, 82, 56)    # Darker brown (belly, shadow)
BODY_LIGHT = (165, 130, 95)  # Light brown (face highlight)
NOSE = (60, 40, 30)          # Dark nose
EYE = (20, 15, 10)           # Near-black eye
EYE_WHITE = (240, 235, 225)  # Eye highlight
MOUTH = (90, 60, 45)         # Mouth line
EAR = (120, 88, 62)          # Ear (slightly darker than body)
FEET = (100, 75, 55)         # Feet/legs
BLUSH = (180, 120, 100)      # Subtle cheek blush
HURT_TINT = (200, 80, 80)    # Red tint for hurt
DEAD_TINT = (120, 120, 120)  # Grey tint for dead
TRANSPARENT = (0, 0, 0, 0)


def create_frame():
    """Create a blank 32x32 RGBA frame."""
    return Image.new("RGBA", (CELL, CELL), TRANSPARENT)


def draw_capybara_base(draw, offset_x=0, offset_y=0):
    """Draw the base capybara shape — the chunky rectangular body."""
    ox, oy = offset_x, offset_y

    # Body (main rectangle — capybaras are CHUNKY)
    # Main body block
    for y in range(14 + oy, 25 + oy):
        for x in range(8 + ox, 25 + ox):
            draw.point((x, y), fill=BODY)

    # Round the body top
    for x in range(10 + ox, 23 + ox):
        draw.point((x, 13 + oy), fill=BODY)

    # Round the body bottom
    for x in range(9 + ox, 24 + ox):
        draw.point((x, 25 + oy), fill=BODY)

    # Belly (lighter underside)
    for y in range(20 + oy, 25 + oy):
        for x in range(10 + ox, 23 + ox):
            draw.point((x, y), fill=BODY_DARK)

    # Head (slightly raised, front of body)
    for y in range(9 + oy, 17 + oy):
        for x in range(18 + ox, 28 + ox):
            draw.point((x, y), fill=BODY)

    # Head top round
    for x in range(20 + ox, 27 + ox):
        draw.point((x, 8 + oy), fill=BODY)

    # Snout (capybara's distinctive blunt snout)
    for y in range(11 + oy, 16 + oy):
        for x in range(26 + ox, 30 + ox):
            draw.point((x, y), fill=BODY_LIGHT)

    # Round snout top
    for x in range(27 + ox, 30 + ox):
        draw.point((x, 10 + oy), fill=BODY_LIGHT)

    # Nose (dark, at tip of snout)
    draw.point((29 + ox, 12 + oy), fill=NOSE)
    draw.point((29 + ox, 13 + oy), fill=NOSE)
    draw.point((28 + ox, 12 + oy), fill=NOSE)

    # Eye
    draw.point((24 + ox, 10 + oy), fill=EYE)
    draw.point((25 + ox, 10 + oy), fill=EYE)
    draw.point((24 + ox, 11 + oy), fill=EYE)
    draw.point((25 + ox, 11 + oy), fill=EYE)
    # Eye highlight
    draw.point((25 + ox, 10 + oy), fill=EYE_WHITE)

    # Ear (small, on top of head)
    draw.point((22 + ox, 7 + oy), fill=EAR)
    draw.point((23 + ox, 7 + oy), fill=EAR)
    draw.point((22 + ox, 8 + oy), fill=EAR)
    draw.point((23 + ox, 8 + oy), fill=BODY)

    # Cheek blush
    draw.point((26 + ox, 14 + oy), fill=BLUSH)
    draw.point((27 + ox, 14 + oy), fill=BLUSH)

    # Mouth line
    draw.point((28 + ox, 14 + oy), fill=MOUTH)
    draw.point((29 + ox, 14 + oy), fill=MOUTH)


def draw_legs(draw, left_y=0, right_y=0, ox=0, oy=0):
    """Draw legs with optional Y offsets for animation."""
    # Back left leg
    for y in range(25 + oy, 29 + oy + left_y):
        draw.point((11 + ox, y), fill=FEET)
        draw.point((12 + ox, y), fill=FEET)
    # Back right leg
    for y in range(25 + oy, 29 + oy + right_y):
        draw.point((14 + ox, y), fill=FEET)
        draw.point((15 + ox, y), fill=FEET)
    # Front left leg
    for y in range(25 + oy, 29 + oy + right_y):
        draw.point((19 + ox, y), fill=FEET)
        draw.point((20 + ox, y), fill=FEET)
    # Front right leg
    for y in range(25 + oy, 29 + oy + left_y):
        draw.point((22 + ox, y), fill=FEET)
        draw.point((23 + ox, y), fill=FEET)

    # Feet (little paws at bottom)
    foot_l = 29 + oy + min(left_y, 0)
    foot_r = 29 + oy + min(right_y, 0)
    # Back feet
    draw.point((10 + ox, 28 + oy), fill=FEET)
    draw.point((13 + ox, 28 + oy), fill=FEET)
    # Front feet
    draw.point((18 + ox, 28 + oy), fill=FEET)
    draw.point((24 + ox, 28 + oy), fill=FEET)


def draw_tail(draw, ox=0, oy=0, tail_y=0):
    """Draw the tiny capybara tail stub."""
    draw.point((8 + ox, 16 + oy + tail_y), fill=BODY_DARK)
    draw.point((7 + ox, 15 + oy + tail_y), fill=BODY_DARK)


def frame_idle(img):
    """Idle pose — standing, relaxed, slight smile."""
    draw = ImageDraw.Draw(img)
    draw_capybara_base(draw)
    draw_legs(draw)
    draw_tail(draw)
    return img


def frame_walk1(img):
    """Walk frame 1 — legs spread."""
    draw = ImageDraw.Draw(img)
    draw_capybara_base(draw)

    # Back legs
    for y in range(25, 29):
        draw.point((10, y), fill=FEET)  # back-left forward
        draw.point((11, y), fill=FEET)
    for y in range(25, 30):
        draw.point((15, y), fill=FEET)  # back-right back
        draw.point((16, y), fill=FEET)

    # Front legs
    for y in range(25, 30):
        draw.point((18, y), fill=FEET)  # front-left back
        draw.point((19, y), fill=FEET)
    for y in range(25, 29):
        draw.point((23, y), fill=FEET)  # front-right forward
        draw.point((24, y), fill=FEET)

    # Feet
    draw.point((9, 28), fill=FEET)
    draw.point((16, 29), fill=FEET)
    draw.point((17, 29), fill=FEET)
    draw.point((24, 28), fill=FEET)

    draw_tail(draw, tail_y=1)
    return img


def frame_walk2(img):
    """Walk frame 2 — legs together (passing position)."""
    draw = ImageDraw.Draw(img)
    draw_capybara_base(draw, offset_y=-1)  # Slight bob up

    # Legs closer together
    for y in range(24, 28):
        draw.point((12, y), fill=FEET)
        draw.point((13, y), fill=FEET)
        draw.point((20, y), fill=FEET)
        draw.point((21, y), fill=FEET)

    # Feet
    draw.point((11, 27), fill=FEET)
    draw.point((14, 27), fill=FEET)
    draw.point((19, 27), fill=FEET)
    draw.point((22, 27), fill=FEET)

    draw_tail(draw, oy=-1)
    return img


def frame_jump(img):
    """Jump — body raised, legs tucked, ears perked."""
    draw = ImageDraw.Draw(img)
    draw_capybara_base(draw, offset_y=-4)

    # Tucked legs (shorter, higher)
    for y in range(21, 24):
        draw.point((11, y), fill=FEET)
        draw.point((12, y), fill=FEET)
        draw.point((14, y), fill=FEET)
        draw.point((15, y), fill=FEET)
        draw.point((19, y), fill=FEET)
        draw.point((20, y), fill=FEET)
        draw.point((22, y), fill=FEET)
        draw.point((23, y), fill=FEET)

    draw_tail(draw, oy=-4, tail_y=-1)

    # Perked ear (extra pixel up)
    draw.point((22, 2), fill=EAR)
    draw.point((23, 2), fill=EAR)

    return img


def frame_hurt(img):
    """Hurt — squished, eyes X, red tint."""
    draw = ImageDraw.Draw(img)

    # Draw base but shifted down slightly (squished)
    draw_capybara_base(draw, offset_y=2)

    # Override eyes with X marks
    draw.point((24, 12), fill=HURT_TINT)
    draw.point((25, 13), fill=HURT_TINT)
    draw.point((25, 12), fill=HURT_TINT)
    draw.point((24, 13), fill=HURT_TINT)

    # Open mouth (ouch!)
    draw.point((28, 16), fill=MOUTH)
    draw.point((29, 16), fill=MOUTH)
    draw.point((28, 17), fill=MOUTH)
    draw.point((29, 17), fill=MOUTH)

    # Splayed legs
    for y in range(27, 30):
        draw.point((9, y), fill=FEET)
        draw.point((10, y), fill=FEET)
        draw.point((16, y), fill=FEET)
        draw.point((17, y), fill=FEET)
        draw.point((18, y), fill=FEET)
        draw.point((19, y), fill=FEET)
        draw.point((24, y), fill=FEET)
        draw.point((25, y), fill=FEET)

    # Impact stars
    draw.point((5, 8), fill=(255, 255, 100))
    draw.point((4, 9), fill=(255, 255, 100))
    draw.point((6, 9), fill=(255, 255, 100))
    draw.point((5, 10), fill=(255, 255, 100))

    draw_tail(draw, oy=2, tail_y=1)
    return img


def frame_dead(img):
    """Dead — flipped on back, X eyes, tongue out."""
    draw = ImageDraw.Draw(img)

    # Body flipped (drawn upside-down, on back)
    # Main body block (higher up, representing on-back)
    for y in range(16, 25):
        for x in range(8, 25):
            draw.point((x, y), fill=BODY)

    # Belly exposed (lighter, facing up now)
    for y in range(16, 20):
        for x in range(10, 23):
            draw.point((x, y), fill=BODY_LIGHT)

    # Head (sideways/tilted)
    for y in range(19, 27):
        for x in range(22, 30):
            draw.point((x, y), fill=BODY)

    # Snout
    for y in range(22, 27):
        for x in range(27, 31):
            draw.point((x, y), fill=BODY_LIGHT)

    # X eyes (dead)
    draw.point((24, 21), fill=HURT_TINT)
    draw.point((25, 22), fill=HURT_TINT)
    draw.point((25, 21), fill=HURT_TINT)
    draw.point((24, 22), fill=HURT_TINT)

    # Tongue sticking out
    draw.point((29, 25), fill=(200, 80, 80))
    draw.point((30, 26), fill=(200, 80, 80))
    draw.point((29, 26), fill=(200, 80, 80))

    # Legs sticking up (stiff, comical)
    for y in range(10, 16):
        draw.point((11, y), fill=FEET)
        draw.point((12, y), fill=FEET)
        draw.point((15, y), fill=FEET)
        draw.point((16, y), fill=FEET)
        draw.point((19, y), fill=FEET)
        draw.point((20, y), fill=FEET)

    # Halo
    for x in range(13, 20):
        draw.point((x, 8), fill=(255, 255, 200, 180))
    draw.point((12, 9), fill=(255, 255, 200, 180))
    draw.point((20, 9), fill=(255, 255, 200, 180))

    return img


def assemble_sprite_sheet():
    """Assemble all frames into a horizontal sprite sheet."""
    sheet_width = CELL * FRAMES
    sheet_height = CELL

    # Create at native resolution
    sheet = Image.new("RGBA", (sheet_width, sheet_height), TRANSPARENT)

    frames = [
        ("idle", frame_idle),
        ("walk1", frame_walk1),
        ("walk2", frame_walk2),
        ("jump", frame_jump),
        ("hurt", frame_hurt),
        ("dead", frame_dead),
    ]

    for i, (name, func) in enumerate(frames):
        frame = create_frame()
        func(frame)
        sheet.paste(frame, (i * CELL, 0))
        # Also save individual frame for preview
        scaled_frame = frame.resize((CELL * SCALE, CELL * SCALE), Image.NEAREST)
        scaled_frame.save(f"assets/sprites/capybara_{name}.png")

    # Save native (32px per frame — actual game sprite)
    sheet.save("assets/sprites/capybara_sheet_32.png")

    # Save scaled up (128px per frame — for preview/inspection)
    scaled = sheet.resize((sheet_width * SCALE, sheet_height * SCALE), Image.NEAREST)
    scaled.save("assets/sprites/capybara_sheet_128.png")

    # Also save a version scaled 2x (64px) - good middle ground for game
    sheet_2x = sheet.resize((sheet_width * 2, sheet_height * 2), Image.NEAREST)
    sheet_2x.save("assets/sprites/capybara_sheet_64.png")

    print(f"Generated {len(frames)} frames:")
    for i, (name, _) in enumerate(frames):
        print(f"  Frame {i}: {name}")
    print(f"\nFiles saved:")
    print(f"  assets/sprites/capybara_sheet_32.png  ({sheet_width}x{sheet_height} — native)")
    print(f"  assets/sprites/capybara_sheet_64.png  ({sheet_width*2}x{sheet_height*2} — 2x)")
    print(f"  assets/sprites/capybara_sheet_128.png ({sheet_width*SCALE}x{sheet_height*SCALE} — preview)")
    print(f"  assets/sprites/capybara_*.png (individual frames at {CELL*SCALE}x{CELL*SCALE})")


if __name__ == "__main__":
    import os
    os.makedirs("assets/sprites", exist_ok=True)
    assemble_sprite_sheet()
    print("\nDone! Open capybara_sheet_128.png to preview.")
