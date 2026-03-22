#!/usr/bin/env python3
"""
Cats vs Capybaras — Cartoon Style Weapon Sprites
Thick-outline style matching character art
64x64 per sprite
"""

from PIL import Image, ImageDraw
import math

TRANSPARENT = (0, 0, 0, 0)
OUTLINE = (45, 35, 30)
OUTLINE_W = 3


def generate_carrot():
    """64x64 cartoon carrot projectile."""
    size = 64
    img = Image.new("RGBA", (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    ORANGE = (245, 155, 40)
    ORANGE_DARK = (220, 125, 25)
    GREEN = (75, 180, 60)
    GREEN_DARK = (55, 145, 40)

    # Carrot body (tapered oval pointing right)
    # Main body
    body = [(12, 24), (14, 20), (48, 26), (54, 32), (48, 38), (14, 44), (12, 40)]
    draw.polygon(body, fill=ORANGE, outline=OUTLINE)

    # Tip
    draw.polygon([(48, 28), (60, 32), (48, 36)], fill=ORANGE_DARK, outline=OUTLINE)

    # Texture lines
    for x in range(22, 46, 8):
        draw.line((x, 26, x, 38), fill=ORANGE_DARK, width=2)

    # Green leaf top (left side)
    # Leaf 1 (up-left)
    leaf1 = [(14, 28), (4, 14), (10, 18), (16, 24)]
    draw.polygon(leaf1, fill=GREEN, outline=OUTLINE)

    # Leaf 2 (straight left)
    leaf2 = [(12, 32), (2, 30), (4, 34), (12, 36)]
    draw.polygon(leaf2, fill=GREEN_DARK, outline=OUTLINE)

    # Leaf 3 (down-left)
    leaf3 = [(14, 36), (4, 48), (10, 44), (16, 40)]
    draw.polygon(leaf3, fill=GREEN, outline=OUTLINE)

    img.save("assets/sprites/cartoon/projectile_carrot.png")
    # Also save at 2x
    img_2x = img.resize((128, 128), Image.LANCZOS)
    img_2x.save("assets/sprites/cartoon/projectile_carrot_128.png")
    print("  Carrot: done")


def generate_bomb():
    """64x64 cartoon bomb."""
    size = 64
    img = Image.new("RGBA", (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    BLACK = (35, 32, 30)
    DARK = (55, 50, 48)
    HIGHLIGHT = (90, 85, 80)
    FUSE = (170, 140, 85)
    SPARK_Y = (255, 230, 60)
    SPARK_O = (255, 170, 40)

    # Bomb body (circle)
    draw.ellipse((10, 16, 54, 58), fill=BLACK, outline=OUTLINE, width=3)

    # Highlight (shiny reflection)
    draw.ellipse((18, 24, 30, 36), fill=HIGHLIGHT)
    draw.ellipse((20, 26, 26, 32), fill=(120, 115, 110))

    # Fuse nub (top)
    draw.rounded_rectangle((28, 12, 36, 20), radius=3, fill=DARK, outline=OUTLINE, width=2)

    # Fuse rope
    points = [(32, 12), (36, 6), (42, 4), (48, 6)]
    draw.line(points, fill=FUSE, width=3, joint="curve")
    draw.line(points, fill=OUTLINE, width=4, joint="curve")
    draw.line(points, fill=FUSE, width=2, joint="curve")

    # Spark/flame at fuse end
    draw.ellipse((44, 0, 56, 12), fill=SPARK_Y)
    draw.ellipse((46, 2, 54, 10), fill=SPARK_O)
    draw.ellipse((48, 4, 52, 8), fill=(255, 255, 200))

    # Small spark lines
    for angle_deg in range(0, 360, 60):
        angle = math.radians(angle_deg)
        cx, cy = 50, 6
        px = cx + math.cos(angle) * 8
        py = cy + math.sin(angle) * 8
        draw.line((cx, cy, int(px), int(py)), fill=SPARK_Y, width=1)

    img.save("assets/sprites/cartoon/projectile_bomb.png")
    img_2x = img.resize((128, 128), Image.LANCZOS)
    img_2x.save("assets/sprites/cartoon/projectile_bomb_128.png")
    print("  Bomb: done")


def generate_banana():
    """64x64 cartoon banana."""
    size = 64
    img = Image.new("RGBA", (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    YELLOW = (255, 230, 50)
    YELLOW_DARK = (235, 200, 30)
    BROWN_TIP = (150, 115, 55)
    SPOT = (200, 170, 50)

    # Banana body (crescent/arc shape)
    # Outer curve
    draw.arc((6, 8, 58, 60), start=220, end=360, fill=OUTLINE, width=20)
    draw.arc((6, 8, 58, 60), start=220, end=360, fill=YELLOW, width=16)

    # Inner darker edge
    draw.arc((10, 12, 54, 56), start=230, end=350, fill=YELLOW_DARK, width=4)

    # Stem (top right)
    draw.rounded_rectangle((48, 10, 56, 20), radius=3, fill=BROWN_TIP, outline=OUTLINE, width=2)

    # Bottom tip
    draw.ellipse((6, 40, 14, 48), fill=BROWN_TIP, outline=OUTLINE, width=2)

    # Brown spots (ripe banana!)
    draw.ellipse((24, 22, 28, 26), fill=SPOT)
    draw.ellipse((36, 18, 40, 22), fill=SPOT)
    draw.ellipse((30, 28, 33, 31), fill=SPOT)

    img.save("assets/sprites/cartoon/projectile_banana.png")
    img_2x = img.resize((128, 128), Image.LANCZOS)
    img_2x.save("assets/sprites/cartoon/projectile_banana_128.png")
    print("  Banana: done")


def generate_explosion():
    """4-frame cartoon explosion, 128x128 per frame."""
    frame_size = 128
    frames = 4
    sheet = Image.new("RGBA", (frame_size * frames, frame_size), TRANSPARENT)

    configs = [
        # (radius, colors: [center, mid, outer, outline])
        (12, [(255, 255, 220), (255, 240, 80), (255, 180, 40), (255, 140, 20)]),
        (30, [(255, 255, 180), (255, 200, 50), (255, 140, 30), (240, 100, 15)]),
        (48, [(255, 240, 120), (255, 160, 30), (230, 90, 15), (180, 60, 10)]),
        (38, [(200, 190, 175), (160, 155, 145), (130, 125, 120), (100, 95, 90)]),
    ]

    for f, (radius, colors) in enumerate(configs):
        cx = f * frame_size + frame_size // 2
        cy = frame_size // 2
        frame_img = Image.new("RGBA", (frame_size, frame_size), TRANSPARENT)
        fdraw = ImageDraw.Draw(frame_img)

        # Outer ring
        fdraw.ellipse((64 - radius, 64 - radius, 64 + radius, 64 + radius),
                      fill=colors[2], outline=colors[3], width=3)
        # Mid ring
        r2 = int(radius * 0.65)
        fdraw.ellipse((64 - r2, 64 - r2, 64 + r2, 64 + r2), fill=colors[1])
        # Center
        r3 = int(radius * 0.3)
        fdraw.ellipse((64 - r3, 64 - r3, 64 + r3, 64 + r3), fill=colors[0])

        # Spiky rays (frames 1-2 only)
        if f in [1, 2]:
            for angle_deg in range(0, 360, 30):
                angle = math.radians(angle_deg + f * 15)
                spike_r = radius + 10 + (f * 5)
                px = 64 + math.cos(angle) * spike_r
                py = 64 + math.sin(angle) * spike_r
                fdraw.line((64, 64, int(px), int(py)), fill=colors[1], width=4)

        # Debris particles (frames 2-3)
        if f >= 2:
            import random
            random.seed(42 + f)
            for _ in range(8):
                angle = random.uniform(0, 2 * math.pi)
                dist = random.uniform(radius * 0.8, radius * 1.3)
                dx = int(64 + math.cos(angle) * dist)
                dy = int(64 + math.sin(angle) * dist)
                s = random.randint(2, 5)
                fdraw.ellipse((dx - s, dy - s, dx + s, dy + s), fill=colors[2])

        sheet.paste(frame_img, (f * frame_size, 0))

    sheet.save("assets/sprites/cartoon/explosion_sheet_128.png")
    sheet_half = sheet.resize((frame_size * frames // 2, frame_size // 2), Image.LANCZOS)
    sheet_half.save("assets/sprites/cartoon/explosion_sheet_64.png")
    print("  Explosion (4 frames): done")


if __name__ == "__main__":
    import os
    os.makedirs("assets/sprites/cartoon", exist_ok=True)
    print("Generating cartoon weapon sprites...")
    generate_carrot()
    generate_bomb()
    generate_banana()
    generate_explosion()
    print("\nAll cartoon weapon sprites generated!")
