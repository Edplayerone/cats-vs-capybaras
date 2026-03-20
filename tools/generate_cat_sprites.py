#!/usr/bin/env python3
"""
Cats vs Capybaras — Cat Character Sprite Sheet Generator
Generates a 6-frame sprite sheet: Idle, Walk1, Walk2, Jump, Hurt, Dead
Style: Chunky pixel art, 32x32 per frame
"""

from PIL import Image, ImageDraw

CELL = 32
SCALE = 4
FRAMES = 6
TRANSPARENT = (0, 0, 0, 0)

# Cat color palette — orange tabby
BODY = (230, 150, 50)          # Orange
BODY_DARK = (200, 120, 35)    # Darker orange (stripes, shadow)
BODY_LIGHT = (245, 210, 160)  # Cream (belly, chest)
STRIPE = (180, 95, 25)        # Dark tabby stripes
EAR_INNER = (220, 140, 140)   # Pink inner ear
NOSE = (220, 120, 120)        # Pink nose
EYE = (30, 100, 30)           # Green eyes
EYE_PUPIL = (10, 10, 10)      # Black pupil
EYE_WHITE = (240, 240, 230)   # Eye highlight
MOUTH = (180, 90, 90)         # Mouth
FEET = (210, 135, 45)         # Paws
TAIL = (220, 140, 40)         # Tail
WHISKER = (200, 190, 170)     # Whiskers
HURT_TINT = (200, 80, 80)


def create_frame():
    return Image.new("RGBA", (CELL, CELL), TRANSPARENT)


def draw_cat_base(draw, ox=0, oy=0):
    """Draw base cat shape — sleek but chunky."""

    # Body (cats are less blocky than capybaras, slightly arched)
    for y in range(14 + oy, 23 + oy):
        for x in range(7 + ox, 23 + ox):
            draw.point((x, y), fill=BODY)

    # Body top arch
    for x in range(9 + ox, 21 + ox):
        draw.point((x, 13 + oy), fill=BODY)

    # Body bottom
    for x in range(8 + ox, 22 + ox):
        draw.point((x, 23 + oy), fill=BODY)

    # Belly (cream underside)
    for y in range(19 + oy, 23 + oy):
        for x in range(10 + ox, 20 + ox):
            draw.point((x, y), fill=BODY_LIGHT)

    # Tabby stripes on back
    for x in range(10 + ox, 20 + ox, 3):
        draw.point((x, 14 + oy), fill=STRIPE)
        draw.point((x, 15 + oy), fill=STRIPE)
        draw.point((x + 1, 14 + oy), fill=STRIPE)

    # Head (rounder than capybara)
    for y in range(8 + oy, 16 + oy):
        for x in range(19 + ox, 28 + ox):
            draw.point((x, y), fill=BODY)

    # Head top
    for x in range(20 + ox, 27 + ox):
        draw.point((x, 7 + oy), fill=BODY)

    # Ears (pointy! cats have triangular ears)
    # Left ear
    draw.point((20 + ox, 4 + oy), fill=BODY)
    draw.point((20 + ox, 5 + oy), fill=BODY)
    draw.point((21 + ox, 5 + oy), fill=BODY)
    draw.point((20 + ox, 6 + oy), fill=BODY)
    draw.point((21 + ox, 6 + oy), fill=BODY)
    draw.point((22 + ox, 6 + oy), fill=BODY)
    draw.point((21 + ox, 4 + oy), fill=EAR_INNER)
    draw.point((21 + ox, 5 + oy), fill=EAR_INNER)

    # Right ear
    draw.point((26 + ox, 4 + oy), fill=BODY)
    draw.point((25 + ox, 5 + oy), fill=BODY)
    draw.point((26 + ox, 5 + oy), fill=BODY)
    draw.point((24 + ox, 6 + oy), fill=BODY)
    draw.point((25 + ox, 6 + oy), fill=BODY)
    draw.point((26 + ox, 6 + oy), fill=BODY)
    draw.point((25 + ox, 4 + oy), fill=EAR_INNER)
    draw.point((25 + ox, 5 + oy), fill=EAR_INNER)

    # Face details
    # Eyes (big, green, cat-like)
    draw.point((21 + ox, 9 + oy), fill=EYE)
    draw.point((22 + ox, 9 + oy), fill=EYE)
    draw.point((21 + ox, 10 + oy), fill=EYE)
    draw.point((22 + ox, 10 + oy), fill=EYE_PUPIL)
    # Eye highlight
    draw.point((21 + ox, 9 + oy), fill=EYE_WHITE)

    draw.point((25 + ox, 9 + oy), fill=EYE)
    draw.point((26 + ox, 9 + oy), fill=EYE)
    draw.point((25 + ox, 10 + oy), fill=EYE)
    draw.point((26 + ox, 10 + oy), fill=EYE_PUPIL)
    draw.point((25 + ox, 9 + oy), fill=EYE_WHITE)

    # Nose (tiny pink triangle)
    draw.point((23 + ox, 11 + oy), fill=NOSE)
    draw.point((24 + ox, 11 + oy), fill=NOSE)

    # Mouth (little w shape)
    draw.point((22 + ox, 12 + oy), fill=MOUTH)
    draw.point((23 + ox, 13 + oy), fill=MOUTH)
    draw.point((24 + ox, 13 + oy), fill=MOUTH)
    draw.point((25 + ox, 12 + oy), fill=MOUTH)

    # Whiskers
    draw.point((18 + ox, 11 + oy), fill=WHISKER)
    draw.point((19 + ox, 10 + oy), fill=WHISKER)
    draw.point((19 + ox, 12 + oy), fill=WHISKER)
    draw.point((28 + ox, 10 + oy), fill=WHISKER)
    draw.point((28 + ox, 12 + oy), fill=WHISKER)
    draw.point((29 + ox, 11 + oy), fill=WHISKER)


def draw_cat_legs(draw, ox=0, oy=0, spread=0):
    """Draw cat legs."""
    # Back legs
    for y in range(23 + oy, 28 + oy):
        draw.point((10 + ox - spread, y), fill=FEET)
        draw.point((11 + ox - spread, y), fill=FEET)
        draw.point((14 + ox + spread, y), fill=FEET)
        draw.point((15 + ox + spread, y), fill=FEET)

    # Front legs
    for y in range(23 + oy, 28 + oy):
        draw.point((17 + ox - spread, y), fill=FEET)
        draw.point((18 + ox - spread, y), fill=FEET)
        draw.point((20 + ox + spread, y), fill=FEET)
        draw.point((21 + ox + spread, y), fill=FEET)

    # Paw pads (tiny dots at feet)
    draw.point((10 + ox - spread, 28 + oy), fill=BODY_LIGHT)
    draw.point((15 + ox + spread, 28 + oy), fill=BODY_LIGHT)
    draw.point((17 + ox - spread, 28 + oy), fill=BODY_LIGHT)
    draw.point((21 + ox + spread, 28 + oy), fill=BODY_LIGHT)


def draw_cat_tail(draw, ox=0, oy=0, curve=0):
    """Draw curvy cat tail."""
    draw.point((7 + ox, 15 + oy), fill=TAIL)
    draw.point((6 + ox, 14 + oy), fill=TAIL)
    draw.point((5 + ox, 13 + oy + curve), fill=TAIL)
    draw.point((4 + ox, 12 + oy + curve), fill=TAIL)
    draw.point((4 + ox, 11 + oy + curve), fill=TAIL)
    draw.point((5 + ox, 10 + oy + curve), fill=TAIL)  # Tail tip curves up


def frame_idle(img):
    draw = ImageDraw.Draw(img)
    draw_cat_base(draw)
    draw_cat_legs(draw)
    draw_cat_tail(draw)
    return img


def frame_walk1(img):
    draw = ImageDraw.Draw(img)
    draw_cat_base(draw)

    # Legs in stride
    for y in range(23, 28):
        draw.point((9, y), fill=FEET)
        draw.point((10, y), fill=FEET)
    for y in range(23, 29):
        draw.point((15, y), fill=FEET)
        draw.point((16, y), fill=FEET)
    for y in range(23, 29):
        draw.point((17, y), fill=FEET)
        draw.point((18, y), fill=FEET)
    for y in range(23, 28):
        draw.point((21, y), fill=FEET)
        draw.point((22, y), fill=FEET)

    draw.point((9, 27), fill=BODY_LIGHT)
    draw.point((16, 28), fill=BODY_LIGHT)
    draw.point((17, 28), fill=BODY_LIGHT)
    draw.point((22, 27), fill=BODY_LIGHT)

    draw_cat_tail(draw, curve=1)
    return img


def frame_walk2(img):
    draw = ImageDraw.Draw(img)
    draw_cat_base(draw, oy=-1)

    # Legs passing
    for y in range(22, 27):
        draw.point((12, y), fill=FEET)
        draw.point((13, y), fill=FEET)
        draw.point((18, y), fill=FEET)
        draw.point((19, y), fill=FEET)

    draw.point((12, 26), fill=BODY_LIGHT)
    draw.point((13, 26), fill=BODY_LIGHT)
    draw.point((18, 26), fill=BODY_LIGHT)
    draw.point((19, 26), fill=BODY_LIGHT)

    draw_cat_tail(draw, oy=-1, curve=-1)
    return img


def frame_jump(img):
    draw = ImageDraw.Draw(img)
    draw_cat_base(draw, oy=-5)

    # Tucked/stretched legs
    for y in range(18, 22):
        draw.point((10, y), fill=FEET)
        draw.point((11, y), fill=FEET)
        draw.point((14, y), fill=FEET)
        draw.point((15, y), fill=FEET)
        draw.point((18, y), fill=FEET)
        draw.point((19, y), fill=FEET)
        draw.point((21, y), fill=FEET)
        draw.point((22, y), fill=FEET)

    # Tail streams behind (more horizontal in jump)
    draw.point((7, 10), fill=TAIL)
    draw.point((6, 10), fill=TAIL)
    draw.point((5, 10), fill=TAIL)
    draw.point((4, 9), fill=TAIL)
    draw.point((3, 9), fill=TAIL)

    return img


def frame_hurt(img):
    draw = ImageDraw.Draw(img)
    draw_cat_base(draw, oy=2)

    # X eyes
    draw.point((21, 11), fill=HURT_TINT)
    draw.point((22, 12), fill=HURT_TINT)
    draw.point((22, 11), fill=HURT_TINT)
    draw.point((21, 12), fill=HURT_TINT)

    draw.point((25, 11), fill=HURT_TINT)
    draw.point((26, 12), fill=HURT_TINT)
    draw.point((26, 11), fill=HURT_TINT)
    draw.point((25, 12), fill=HURT_TINT)

    # Yowling mouth
    draw.point((22, 14), fill=MOUTH)
    draw.point((23, 15), fill=MOUTH)
    draw.point((24, 15), fill=MOUTH)
    draw.point((25, 14), fill=MOUTH)
    draw.point((23, 16), fill=(180, 60, 60))
    draw.point((24, 16), fill=(180, 60, 60))

    # Splayed legs
    for y in range(25, 29):
        draw.point((8, y), fill=FEET)
        draw.point((9, y), fill=FEET)
        draw.point((15, y), fill=FEET)
        draw.point((16, y), fill=FEET)
        draw.point((18, y), fill=FEET)
        draw.point((19, y), fill=FEET)
        draw.point((23, y), fill=FEET)
        draw.point((24, y), fill=FEET)

    # Impact stars
    draw.point((15, 5), fill=(255, 255, 100))
    draw.point((14, 6), fill=(255, 255, 100))
    draw.point((16, 6), fill=(255, 255, 100))
    draw.point((15, 7), fill=(255, 255, 100))

    # Frazzled tail
    draw.point((6, 17), fill=TAIL)
    draw.point((5, 16), fill=TAIL)
    draw.point((4, 15), fill=TAIL)
    draw.point((3, 14), fill=TAIL)
    draw.point((4, 13), fill=TAIL)  # Puffed up

    return img


def frame_dead(img):
    draw = ImageDraw.Draw(img)

    # Cat on its back, belly up
    for y in range(16, 24):
        for x in range(6, 24):
            draw.point((x, y), fill=BODY)

    # Belly up (cream)
    for y in range(16, 20):
        for x in range(9, 21):
            draw.point((x, y), fill=BODY_LIGHT)

    # Head flopped to side
    for y in range(20, 27):
        for x in range(21, 29):
            draw.point((x, y), fill=BODY)

    # X eyes
    draw.point((23, 22), fill=HURT_TINT)
    draw.point((24, 23), fill=HURT_TINT)
    draw.point((24, 22), fill=HURT_TINT)
    draw.point((23, 23), fill=HURT_TINT)

    draw.point((26, 22), fill=HURT_TINT)
    draw.point((27, 23), fill=HURT_TINT)
    draw.point((27, 22), fill=HURT_TINT)
    draw.point((26, 23), fill=HURT_TINT)

    # Tongue
    draw.point((25, 25), fill=(200, 80, 80))
    draw.point((26, 26), fill=(200, 80, 80))

    # Ears flopped
    draw.point((22, 19), fill=BODY)
    draw.point((27, 19), fill=BODY)
    draw.point((22, 20), fill=EAR_INNER)
    draw.point((27, 20), fill=EAR_INNER)

    # Legs sticking up
    for y in range(10, 16):
        draw.point((9, y), fill=FEET)
        draw.point((10, y), fill=FEET)
        draw.point((13, y), fill=FEET)
        draw.point((14, y), fill=FEET)
        draw.point((17, y), fill=FEET)
        draw.point((18, y), fill=FEET)
        draw.point((20, y), fill=FEET)
        draw.point((21, y), fill=FEET)

    # Halo
    for x in range(12, 19):
        draw.point((x, 8), fill=(255, 255, 200, 180))
    draw.point((11, 9), fill=(255, 255, 200, 180))
    draw.point((19, 9), fill=(255, 255, 200, 180))

    # Tail limp
    draw.point((5, 20), fill=TAIL)
    draw.point((4, 21), fill=TAIL)
    draw.point((3, 22), fill=TAIL)

    return img


def assemble_sprite_sheet():
    sheet_width = CELL * FRAMES
    sheet_height = CELL
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
        scaled_frame = frame.resize((CELL * SCALE, CELL * SCALE), Image.NEAREST)
        scaled_frame.save(f"assets/sprites/cat_{name}.png")

    sheet.save("assets/sprites/cat_sheet_32.png")
    scaled = sheet.resize((sheet_width * SCALE, sheet_height * SCALE), Image.NEAREST)
    scaled.save("assets/sprites/cat_sheet_128.png")
    sheet_2x = sheet.resize((sheet_width * 2, sheet_height * 2), Image.NEAREST)
    sheet_2x.save("assets/sprites/cat_sheet_64.png")

    print(f"Generated {len(frames)} frames:")
    for i, (name, _) in enumerate(frames):
        print(f"  Frame {i}: {name}")
    print(f"\nFiles saved:")
    print(f"  assets/sprites/cat_sheet_32.png  ({sheet_width}x{sheet_height})")
    print(f"  assets/sprites/cat_sheet_64.png  ({sheet_width*2}x{sheet_height*2})")
    print(f"  assets/sprites/cat_sheet_128.png ({sheet_width*SCALE}x{sheet_height*SCALE})")


if __name__ == "__main__":
    import os
    os.makedirs("assets/sprites", exist_ok=True)
    assemble_sprite_sheet()
    print("\nDone!")
