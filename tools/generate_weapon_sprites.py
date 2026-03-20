#!/usr/bin/env python3
"""
Cats vs Capybaras — Weapon Projectile Sprite Generator
Generates sprites for: Carrot, Bomb, Banana
"""

from PIL import Image, ImageDraw
import math

TRANSPARENT = (0, 0, 0, 0)
SCALE = 4


def generate_carrot():
    """16x16 carrot projectile."""
    size = 16
    img = Image.new("RGBA", (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    ORANGE = (240, 140, 30)
    ORANGE_DARK = (210, 110, 20)
    TIP = (220, 120, 15)
    GREEN = (60, 160, 50)
    GREEN_DARK = (40, 130, 35)

    # Carrot body (angled, pointing right)
    # Main body
    for y in range(6, 11):
        for x in range(3, 13):
            draw.point((x, y), fill=ORANGE)

    # Taper to point on right
    draw.point((13, 7), fill=ORANGE)
    draw.point((13, 8), fill=ORANGE)
    draw.point((13, 9), fill=ORANGE)
    draw.point((14, 8), fill=TIP)

    # Taper top/bottom
    draw.point((2, 7), fill=ORANGE)
    draw.point((2, 8), fill=ORANGE)
    draw.point((2, 9), fill=ORANGE)

    # Darker lines (carrot texture)
    for x in range(5, 12, 3):
        draw.point((x, 7), fill=ORANGE_DARK)
        draw.point((x, 8), fill=ORANGE_DARK)
        draw.point((x, 9), fill=ORANGE_DARK)

    # Green top/leaves (left side)
    draw.point((1, 6), fill=GREEN)
    draw.point((0, 5), fill=GREEN)
    draw.point((1, 5), fill=GREEN_DARK)
    draw.point((2, 5), fill=GREEN)
    draw.point((0, 4), fill=GREEN)
    draw.point((2, 4), fill=GREEN_DARK)
    draw.point((1, 4), fill=GREEN)
    draw.point((1, 7), fill=GREEN)
    draw.point((0, 7), fill=GREEN)
    draw.point((0, 8), fill=GREEN_DARK)
    draw.point((1, 9), fill=GREEN)
    draw.point((0, 10), fill=GREEN)
    draw.point((1, 10), fill=GREEN_DARK)

    img.save("assets/sprites/projectile_carrot_16.png")
    scaled = img.resize((size * SCALE, size * SCALE), Image.NEAREST)
    scaled.save("assets/sprites/projectile_carrot_64.png")
    print("  Carrot: done")


def generate_bomb():
    """16x16 bomb — classic round bomb with fuse."""
    size = 16
    img = Image.new("RGBA", (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    BLACK = (30, 30, 35)
    DARK = (50, 50, 55)
    HIGHLIGHT = (80, 80, 90)
    FUSE = (160, 130, 80)
    SPARK = (255, 220, 60)
    SPARK2 = (255, 160, 30)

    # Bomb body (circle)
    center = 8
    radius = 5
    for y in range(size):
        for x in range(size):
            dx = x - center
            dy = y - (center + 1)
            dist = math.sqrt(dx * dx + dy * dy)
            if dist <= radius:
                if dist <= radius - 2:
                    draw.point((x, y), fill=BLACK)
                elif dx < 0 and dy < 0:
                    draw.point((x, y), fill=HIGHLIGHT)
                else:
                    draw.point((x, y), fill=DARK)

    # Highlight (shiny spot)
    draw.point((5, 7), fill=HIGHLIGHT)
    draw.point((6, 7), fill=HIGHLIGHT)
    draw.point((5, 8), fill=(100, 100, 110))

    # Fuse top
    draw.point((9, 3), fill=FUSE)
    draw.point((10, 2), fill=FUSE)
    draw.point((11, 1), fill=FUSE)
    draw.point((10, 3), fill=DARK)

    # Spark
    draw.point((12, 0), fill=SPARK)
    draw.point((11, 0), fill=SPARK2)
    draw.point((13, 0), fill=SPARK2)
    draw.point((12, 1), fill=SPARK2)

    img.save("assets/sprites/projectile_bomb_16.png")
    scaled = img.resize((size * SCALE, size * SCALE), Image.NEAREST)
    scaled.save("assets/sprites/projectile_bomb_64.png")
    print("  Bomb: done")


def generate_banana():
    """16x16 banana — curved yellow shape."""
    size = 16
    img = Image.new("RGBA", (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    YELLOW = (250, 220, 50)
    YELLOW_DARK = (220, 190, 30)
    TIP_BROWN = (140, 110, 50)
    SPOT = (180, 150, 40)

    # Banana curve (crescent shape)
    # Top curve
    points = [
        (3, 10), (4, 9), (5, 8), (6, 7), (7, 6), (8, 6),
        (9, 6), (10, 6), (11, 7), (12, 8),
    ]
    for x, y in points:
        draw.point((x, y), fill=YELLOW)
        draw.point((x, y + 1), fill=YELLOW)
        draw.point((x, y + 2), fill=YELLOW_DARK)

    # Extra body thickness
    for x, y in [(6, 8), (7, 7), (8, 7), (9, 7), (10, 7), (7, 8), (8, 8), (9, 8), (10, 8)]:
        draw.point((x, y), fill=YELLOW)

    # Stem end (left)
    draw.point((2, 11), fill=TIP_BROWN)
    draw.point((3, 11), fill=TIP_BROWN)

    # Tip end (right)
    draw.point((13, 9), fill=TIP_BROWN)
    draw.point((13, 8), fill=TIP_BROWN)

    # Brown spots
    draw.point((7, 9), fill=SPOT)
    draw.point((10, 7), fill=SPOT)

    img.save("assets/sprites/projectile_banana_16.png")
    scaled = img.resize((size * SCALE, size * SCALE), Image.NEAREST)
    scaled.save("assets/sprites/projectile_banana_64.png")
    print("  Banana: done")


def generate_explosion_frames():
    """4-frame explosion animation sprite sheet."""
    size = 32
    frames = 4
    sheet = Image.new("RGBA", (size * frames, size), TRANSPARENT)

    colors_by_frame = [
        # Frame 0: small flash (white/yellow center)
        [(255, 255, 200), (255, 220, 60), (255, 160, 30)],
        # Frame 1: expanding fireball
        [(255, 255, 150), (255, 180, 40), (255, 100, 20)],
        # Frame 2: big explosion
        [(255, 200, 80), (255, 120, 20), (200, 60, 10)],
        # Frame 3: smoke dissipating
        [(180, 160, 140), (140, 130, 120), (100, 95, 90)],
    ]
    radii = [3, 7, 11, 9]

    for f in range(frames):
        center_x = f * size + size // 2
        center_y = size // 2
        r = radii[f]
        colors = colors_by_frame[f]

        for y in range(size):
            for x in range(size):
                dx = (f * size + x) - center_x
                dy = y - center_y
                dist = math.sqrt(dx * dx + dy * dy)

                if dist <= r * 0.3:
                    alpha = 255
                    color = colors[0]
                elif dist <= r * 0.6:
                    alpha = 220
                    color = colors[1]
                elif dist <= r:
                    alpha = int(180 * (1 - dist / r) + 40)
                    color = colors[2]
                else:
                    continue

                sheet.putpixel((f * size + x, y), (*color, alpha))

    sheet.save("assets/sprites/explosion_sheet_32.png")
    scaled = sheet.resize((size * frames * 2, size * 2), Image.NEAREST)
    scaled.save("assets/sprites/explosion_sheet_64.png")
    print("  Explosion (4 frames): done")


if __name__ == "__main__":
    import os
    os.makedirs("assets/sprites", exist_ok=True)
    print("Generating weapon sprites...")
    generate_carrot()
    generate_bomb()
    generate_banana()
    generate_explosion_frames()
    print("\nAll weapon sprites generated!")
