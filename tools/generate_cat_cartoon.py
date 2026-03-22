#!/usr/bin/env python3
"""
Cats vs Capybaras — Cartoon Style Cat Sprite Sheet
Thick-outline, expressive, orange tabby
128x128 per frame, anti-aliased
"""

from PIL import Image, ImageDraw
import math

CELL = 128
FRAMES = 6
TRANSPARENT = (0, 0, 0, 0)

# Cat palette — orange tabby
OUTLINE = (45, 35, 30)
BODY = (235, 165, 60)             # Bright orange
BODY_SHADOW = (210, 140, 45)      # Darker orange
BELLY = (250, 225, 180)           # Cream chest/belly
STRIPE = (195, 120, 35)           # Tabby stripes
EAR_INNER = (230, 155, 155)       # Pink inner ear
NOSE = (230, 145, 145)            # Pink nose
EYE_GREEN = (70, 170, 70)         # Green iris
EYE_PUPIL = (20, 18, 15)          # Black pupil (vertical slit)
EYE_WHITE = (255, 252, 245)       # Eye highlight
WHISKER = (60, 50, 42)            # Dark whiskers
MOUTH = (100, 65, 50)
FEET = (215, 150, 50)             # Paws
TAIL = (225, 155, 50)             # Tail
HURT_RED = (220, 80, 70)
STAR_YELLOW = (255, 230, 80)

OUTLINE_W = 3


def draw_ellipse_outlined(draw, bbox, fill, outline_color=OUTLINE, width=OUTLINE_W):
    draw.ellipse(bbox, fill=fill, outline=outline_color, width=width)


def draw_rounded_rect(draw, bbox, fill, outline_color, outline_width, radius=10):
    draw.rounded_rectangle(bbox, radius=radius, fill=fill)
    draw.rounded_rectangle(bbox, radius=radius, outline=outline_color, width=outline_width)


def draw_body(draw, ox=0, oy=0, squish_x=0, squish_y=0):
    """Cat body — slightly sleeker than capybara but still chunky."""
    body_bbox = (24 + ox - squish_x, 40 + oy - squish_y, 96 + ox + squish_x, 92 + oy + squish_y)
    draw_ellipse_outlined(draw, body_bbox, BODY)

    # Belly
    belly_bbox = (36 + ox, 58 + oy, 88 + ox + squish_x, 88 + oy + squish_y)
    draw.ellipse(belly_bbox, fill=BELLY)

    # Tabby stripes on back
    stripe_y = 44 + oy - squish_y
    for sx in range(40 + ox, 85 + ox, 12):
        draw.rounded_rectangle((sx, stripe_y, sx + 6, stripe_y + 14), radius=3, fill=STRIPE)


def draw_head(draw, ox=0, oy=0, tilt=0):
    """Cat head — rounder, with prominent pointed ears."""
    # Head circle
    head_bbox = (60 + ox, 12 + oy + tilt, 116 + ox, 64 + oy + tilt)
    draw_ellipse_outlined(draw, head_bbox, BODY)

    # Cheek fluff (slightly wider at bottom of head)
    draw.ellipse((56 + ox, 35 + oy + tilt, 78 + ox, 58 + oy + tilt), fill=BODY)
    draw.ellipse((98 + ox, 35 + oy + tilt, 120 + ox, 58 + oy + tilt), fill=BODY)

    # White muzzle area
    muzzle_bbox = (72 + ox, 40 + oy + tilt, 104 + ox, 60 + oy + tilt)
    draw.ellipse(muzzle_bbox, fill=BELLY)


def draw_ears(draw, ox=0, oy=0, tilt=0, flat=False):
    """Pointy cat ears — triangular."""
    ear_y = 8 + oy + tilt
    if flat:
        ear_y += 8  # Ears flatten when hurt/dead

    # Left ear (triangle)
    left_ear = [(62 + ox, 32 + oy + tilt), (58 + ox, ear_y - 4), (78 + ox, 26 + oy + tilt)]
    draw.polygon(left_ear, fill=BODY, outline=OUTLINE)
    # Inner ear
    inner_left = [(64 + ox, 30 + oy + tilt), (61 + ox, ear_y + 2), (74 + ox, 26 + oy + tilt)]
    draw.polygon(inner_left, fill=EAR_INNER)

    # Right ear
    right_ear = [(98 + ox, 26 + oy + tilt), (118 + ox, ear_y - 4), (114 + ox, 32 + oy + tilt)]
    draw.polygon(right_ear, fill=BODY, outline=OUTLINE)
    # Inner ear
    inner_right = [(100 + ox, 26 + oy + tilt), (115 + ox, ear_y + 2), (112 + ox, 30 + oy + tilt)]
    draw.polygon(inner_right, fill=EAR_INNER)


def draw_eyes(draw, ox=0, oy=0, state="normal", tilt=0):
    """Cat eyes — big with vertical pupils."""
    # Two eyes
    for ex_base in [70 + ox, 94 + ox]:
        ey = 28 + oy + tilt
        ex = ex_base

        if state == "normal":
            # Big oval eye
            draw.ellipse((ex, ey, ex + 14, ey + 16), fill=EYE_GREEN, outline=OUTLINE, width=2)
            # Vertical slit pupil
            draw.ellipse((ex + 5, ey + 3, ex + 9, ey + 13), fill=EYE_PUPIL)
            # Highlight
            draw.ellipse((ex + 3, ey + 3, ex + 7, ey + 7), fill=EYE_WHITE)

        elif state == "sly":
            # Half-closed confident eyes
            draw.ellipse((ex, ey + 4, ex + 14, ey + 14), fill=EYE_GREEN, outline=OUTLINE, width=2)
            draw.ellipse((ex + 5, ey + 6, ex + 9, ey + 12), fill=EYE_PUPIL)
            draw.ellipse((ex + 3, ey + 5, ex + 6, ey + 8), fill=EYE_WHITE)

        elif state == "wide":
            # Wide surprised eyes
            draw.ellipse((ex - 1, ey - 2, ex + 15, ey + 18), fill=EYE_GREEN, outline=OUTLINE, width=2)
            draw.ellipse((ex + 4, ey + 2, ex + 10, ey + 14), fill=EYE_PUPIL)
            draw.ellipse((ex + 3, ey + 2, ex + 7, ey + 6), fill=EYE_WHITE)

        elif state == "x_eyes":
            cx = ex + 7
            cy = ey + 8
            draw.line((cx - 5, cy - 5, cx + 5, cy + 5), fill=HURT_RED, width=3)
            draw.line((cx - 5, cy + 5, cx + 5, cy - 5), fill=HURT_RED, width=3)

        elif state == "happy":
            draw.arc((ex, ey + 4, ex + 14, ey + 16), start=200, end=340, fill=EYE_PUPIL, width=3)


def draw_nose_mouth(draw, ox=0, oy=0, state="neutral", tilt=0):
    """Cat nose and mouth."""
    nx = 84 + ox
    ny = 48 + oy + tilt

    # Nose (small inverted triangle)
    nose_pts = [(nx, ny), (nx + 8, ny), (nx + 4, ny + 5)]
    draw.polygon(nose_pts, fill=NOSE, outline=OUTLINE)

    if state == "neutral":
        # Y-shaped mouth
        draw.line((nx + 4, ny + 5, nx + 4, ny + 10), fill=MOUTH, width=2)
        draw.arc((nx - 2, ny + 6, nx + 6, ny + 14), start=0, end=180, fill=MOUTH, width=2)
        draw.arc((nx + 2, ny + 6, nx + 10, ny + 14), start=0, end=180, fill=MOUTH, width=2)

    elif state == "smile":
        draw.line((nx + 4, ny + 5, nx + 4, ny + 9), fill=MOUTH, width=2)
        draw.arc((nx - 4, ny + 5, nx + 5, ny + 14), start=0, end=180, fill=MOUTH, width=2)
        draw.arc((nx + 3, ny + 5, nx + 12, ny + 14), start=0, end=180, fill=MOUTH, width=2)

    elif state == "yowl":
        # Big open yowling mouth
        draw.ellipse((nx - 2, ny + 4, nx + 10, ny + 18), fill=(80, 40, 35), outline=OUTLINE, width=2)
        # Fangs
        draw.polygon([(nx, ny + 5), (nx + 3, ny + 10), (nx - 1, ny + 8)], fill=EYE_WHITE)
        draw.polygon([(nx + 8, ny + 5), (nx + 5, ny + 10), (nx + 9, ny + 8)], fill=EYE_WHITE)

    elif state == "tongue":
        draw.line((nx + 4, ny + 5, nx + 4, ny + 9), fill=MOUTH, width=2)
        draw.ellipse((nx + 1, ny + 8, nx + 8, ny + 16), fill=(210, 120, 120), outline=OUTLINE, width=2)


def draw_whiskers(draw, ox=0, oy=0, tilt=0):
    """Draw whiskers on both sides of face."""
    wy = 48 + oy + tilt

    # Left whiskers
    draw.line((60 + ox, wy, 44 + ox, wy - 4), fill=WHISKER, width=1)
    draw.line((60 + ox, wy + 4, 42 + ox, wy + 4), fill=WHISKER, width=1)
    draw.line((60 + ox, wy + 8, 44 + ox, wy + 12), fill=WHISKER, width=1)

    # Right whiskers
    draw.line((116 + ox, wy, 132 + ox, wy - 4), fill=WHISKER, width=1)  # may clip but ok
    draw.line((116 + ox, wy + 4, 134 + ox, wy + 4), fill=WHISKER, width=1)
    draw.line((116 + ox, wy + 8, 132 + ox, wy + 12), fill=WHISKER, width=1)


def draw_legs(draw, ox=0, oy=0, pose="standing"):
    """Draw four cat legs."""
    leg_y_top = 80 + oy
    leg_y_bot = 108 + oy

    if pose == "standing":
        positions = [(32 + ox, 42 + ox), (50 + ox, 58 + ox), (66 + ox, 74 + ox), (82 + ox, 92 + ox)]
        for lx0, lx1 in positions:
            draw_rounded_rect(draw, (lx0, leg_y_top, lx1, leg_y_bot), FEET, OUTLINE, OUTLINE_W, radius=5)
            # Paw
            draw.ellipse((lx0 - 1, leg_y_bot - 6, lx1 + 1, leg_y_bot + 1), fill=BELLY, outline=OUTLINE, width=2)

    elif pose == "walk_spread":
        offsets = [(-4, 2), (4, -2), (-2, 4), (4, -4)]
        base_positions = [(32 + ox, 42 + ox), (50 + ox, 58 + ox), (66 + ox, 74 + ox), (82 + ox, 92 + ox)]
        for (lx0, lx1), (dx, dy) in zip(base_positions, offsets):
            draw_rounded_rect(draw, (lx0 + dx, leg_y_top + dy, lx1 + dx, leg_y_bot + dy), FEET, OUTLINE, OUTLINE_W, radius=5)
            draw.ellipse((lx0 + dx - 1, leg_y_bot + dy - 6, lx1 + dx + 1, leg_y_bot + dy + 1), fill=BELLY, outline=OUTLINE, width=2)

    elif pose == "walk_together":
        positions = [(38 + ox, 48 + ox), (52 + ox, 60 + ox), (68 + ox, 76 + ox), (78 + ox, 88 + ox)]
        for lx0, lx1 in positions:
            draw_rounded_rect(draw, (lx0, leg_y_top - 4, lx1, leg_y_bot - 4), FEET, OUTLINE, OUTLINE_W, radius=5)
            draw.ellipse((lx0 - 1, leg_y_bot - 10, lx1 + 1, leg_y_bot - 3), fill=BELLY, outline=OUTLINE, width=2)

    elif pose == "tucked":
        positions = [(36 + ox, 44 + ox), (50 + ox, 58 + ox), (68 + ox, 76 + ox), (82 + ox, 90 + ox)]
        for lx0, lx1 in positions:
            draw_rounded_rect(draw, (lx0, leg_y_top, lx1, leg_y_top + 12), FEET, OUTLINE, OUTLINE_W, radius=5)

    elif pose == "splayed":
        draw_rounded_rect(draw, (20 + ox, leg_y_top, 30 + ox, leg_y_bot + 4), FEET, OUTLINE, OUTLINE_W, radius=5)
        draw_rounded_rect(draw, (46 + ox, leg_y_top + 2, 56 + ox, leg_y_bot + 6), FEET, OUTLINE, OUTLINE_W, radius=5)
        draw_rounded_rect(draw, (68 + ox, leg_y_top + 2, 78 + ox, leg_y_bot + 6), FEET, OUTLINE, OUTLINE_W, radius=5)
        draw_rounded_rect(draw, (94 + ox, leg_y_top, 104 + ox, leg_y_bot + 4), FEET, OUTLINE, OUTLINE_W, radius=5)


def draw_tail(draw, ox=0, oy=0, curve="up"):
    """Draw cat tail — long and curvy."""
    tx = 20 + ox
    ty = 48 + oy

    if curve == "up":
        # Graceful upward curve
        points = [(tx + 8, ty + 10), (tx, ty + 5), (tx - 4, ty - 5), (tx - 2, ty - 15), (tx + 4, ty - 22)]
        draw.line(points, fill=TAIL, width=7, joint="curve")
        draw.line(points, fill=OUTLINE, width=9, joint="curve")
        draw.line(points, fill=TAIL, width=6, joint="curve")
        # Tail tip
        draw.ellipse((tx + 1, ty - 25, tx + 8, ty - 19), fill=BODY_SHADOW, outline=OUTLINE, width=2)

    elif curve == "down":
        points = [(tx + 8, ty + 10), (tx, ty + 12), (tx - 6, ty + 18), (tx - 4, ty + 25)]
        draw.line(points, fill=OUTLINE, width=9, joint="curve")
        draw.line(points, fill=TAIL, width=6, joint="curve")

    elif curve == "puffed":
        # Puffed up scared tail
        draw.line([(tx + 8, ty + 10), (tx, ty), (tx - 2, ty - 12)], fill=OUTLINE, width=11, joint="curve")
        draw.line([(tx + 8, ty + 10), (tx, ty), (tx - 2, ty - 12)], fill=TAIL, width=8, joint="curve")
        # Extra puff
        draw_ellipse_outlined(draw, (tx - 8, ty - 18, tx + 6, ty - 6), TAIL, OUTLINE, 2)

    elif curve == "limp":
        points = [(tx + 8, ty + 10), (tx + 2, ty + 18), (tx - 4, ty + 22)]
        draw.line(points, fill=OUTLINE, width=9, joint="curve")
        draw.line(points, fill=TAIL, width=6, joint="curve")


def draw_impact_stars(draw, x, y, size=8):
    for angle_deg in range(0, 360, 45):
        angle = math.radians(angle_deg)
        px = x + math.cos(angle) * size
        py = y + math.sin(angle) * size
        draw.line((x, y, int(px), int(py)), fill=STAR_YELLOW, width=2)
    draw.ellipse((x - 3, y - 3, x + 3, y + 3), fill=(255, 255, 255))


def draw_halo(draw, cx, y):
    draw.ellipse((cx - 18, y - 5, cx + 18, y + 5), outline=(255, 230, 120), width=3)
    draw.ellipse((cx - 16, y - 3, cx + 16, y + 3), outline=(255, 245, 180), width=1)


# === FRAMES ===

def frame_idle():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)
    draw_tail(draw, curve="up")
    draw_legs(draw, pose="standing")
    draw_body(draw)
    draw_head(draw)
    draw_ears(draw)
    draw_eyes(draw, state="sly")
    draw_nose_mouth(draw, state="smile")
    draw_whiskers(draw)
    return img


def frame_walk1():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)
    draw_tail(draw, curve="up")
    draw_legs(draw, pose="walk_spread")
    draw_body(draw)
    draw_head(draw)
    draw_ears(draw)
    draw_eyes(draw, state="normal")
    draw_nose_mouth(draw, state="neutral")
    draw_whiskers(draw)
    return img


def frame_walk2():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)
    draw_tail(draw, curve="down")
    draw_legs(draw, pose="walk_together")
    draw_body(draw, oy=-3)
    draw_head(draw, oy=-3)
    draw_ears(draw, oy=-3)
    draw_eyes(draw, oy=-3, state="happy")
    draw_nose_mouth(draw, oy=-3, state="smile")
    draw_whiskers(draw, oy=-3)
    return img


def frame_jump():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)
    oy = -12
    draw_tail(draw, oy=oy, curve="up")
    draw_legs(draw, oy=oy, pose="tucked")
    draw_body(draw, oy=oy)
    draw_head(draw, oy=oy)
    draw_ears(draw, oy=oy)
    draw_eyes(draw, oy=oy, state="wide")
    draw_nose_mouth(draw, oy=oy, state="neutral")
    draw_whiskers(draw, oy=oy)
    return img


def frame_hurt():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)
    oy = 5
    draw_tail(draw, oy=oy, curve="puffed")
    draw_legs(draw, oy=oy, pose="splayed")
    draw_body(draw, oy=oy, squish_x=3, squish_y=-3)
    draw_head(draw, oy=oy, tilt=3)
    draw_ears(draw, oy=oy, tilt=3, flat=True)
    draw_eyes(draw, oy=oy, state="x_eyes", tilt=3)
    draw_nose_mouth(draw, oy=oy, state="yowl", tilt=3)
    draw_whiskers(draw, oy=oy, tilt=3)
    draw_impact_stars(draw, 18, 18, size=10)
    draw_impact_stars(draw, 120, 12, size=7)
    return img


def frame_dead():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    # Body on back
    body_bbox = (14, 50, 92, 102)
    draw_ellipse_outlined(draw, body_bbox, BODY)
    draw.ellipse((24, 52, 82, 82), fill=BELLY)

    # Tabby stripes (now on sides)
    for sy in range(54, 75, 12):
        draw.rounded_rectangle((18, sy, 28, sy + 6), radius=2, fill=STRIPE)
        draw.rounded_rectangle((80, sy, 90, sy + 6), radius=2, fill=STRIPE)

    # Head flopped
    head_bbox = (68, 68, 122, 112)
    draw_ellipse_outlined(draw, head_bbox, BODY)
    draw.ellipse((78, 88, 112, 108), fill=BELLY)

    # Ears flopped
    draw.polygon([(72, 72), (62, 68), (70, 82)], fill=BODY, outline=OUTLINE)
    draw.polygon([(74, 72), (64, 70), (72, 80)], fill=EAR_INNER)
    draw.polygon([(118, 72), (126, 68), (120, 82)], fill=BODY, outline=OUTLINE)

    # X eyes
    for ex in [80, 102]:
        ey = 80
        draw.line((ex - 4, ey - 4, ex + 4, ey + 4), fill=HURT_RED, width=3)
        draw.line((ex - 4, ey + 4, ex + 4, ey - 4), fill=HURT_RED, width=3)

    # Tongue out
    draw.ellipse((92, 98, 102, 110), fill=(210, 120, 120), outline=OUTLINE, width=2)

    # Legs up
    leg_positions = [(20, 30), (38, 48), (56, 66), (72, 82)]
    for lx0, lx1 in leg_positions:
        draw_rounded_rect(draw, (lx0, 24, lx1, 52), FEET, OUTLINE, OUTLINE_W, radius=5)
        draw.ellipse((lx0, 26, lx1, 34), fill=BELLY, outline=OUTLINE, width=2)

    # Tail limp
    draw.line([(12, 76), (6, 84), (4, 94)], fill=OUTLINE, width=9, joint="curve")
    draw.line([(12, 76), (6, 84), (4, 94)], fill=TAIL, width=6, joint="curve")

    # Halo
    draw_halo(draw, 52, 16)

    return img


def assemble_sheet():
    import os
    os.makedirs("assets/sprites/cartoon", exist_ok=True)

    sheet = Image.new("RGBA", (CELL * FRAMES, CELL), TRANSPARENT)

    frames = [
        ("idle", frame_idle),
        ("walk1", frame_walk1),
        ("walk2", frame_walk2),
        ("jump", frame_jump),
        ("hurt", frame_hurt),
        ("dead", frame_dead),
    ]

    for i, (name, func) in enumerate(frames):
        frame = func()
        sheet.paste(frame, (i * CELL, 0))
        frame.save(f"assets/sprites/cartoon/cat_{name}.png")

    sheet.save("assets/sprites/cartoon/cat_sheet_128.png")

    sheet_2x = sheet.resize((CELL * FRAMES * 2, CELL * 2), Image.LANCZOS)
    sheet_2x.save("assets/sprites/cartoon/cat_sheet_256.png")

    print(f"Generated {len(frames)} cartoon cat frames:")
    for i, (name, _) in enumerate(frames):
        print(f"  Frame {i}: {name}")
    print(f"\nSaved to assets/sprites/cartoon/")


if __name__ == "__main__":
    assemble_sheet()
