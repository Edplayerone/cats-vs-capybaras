#!/usr/bin/env python3
"""
Cats vs Capybaras — Cartoon Style Capybara Sprite Sheet
Thick-outline, round, soft, plushie aesthetic
128x128 per frame, anti-aliased
"""

from PIL import Image, ImageDraw
import math

CELL = 128
FRAMES = 6
TRANSPARENT = (0, 0, 0, 0)

# Palette — warm, soft, plushie capybara
OUTLINE = (45, 35, 30)            # Dark brown outline
BODY = (175, 130, 85)             # Warm brown body
BODY_SHADOW = (150, 108, 68)      # Slightly darker for depth
BELLY = (210, 180, 145)           # Light cream belly
SNOUT = (200, 165, 125)           # Lighter snout area
NOSE = (65, 45, 35)               # Dark nose
NOSE_HIGHLIGHT = (90, 65, 50)     # Nose shine
EYE_BLACK = (25, 20, 18)          # Eye
EYE_WHITE = (255, 252, 245)       # Eye highlight dot
BLUSH = (210, 145, 130, 120)      # Soft pink cheek blush (semi-transparent)
MOUTH = (100, 65, 50)             # Mouth line
EAR_INNER = (155, 115, 78)        # Inner ear
FEET = (140, 100, 65)             # Feet slightly darker
HURT_RED = (220, 80, 70)          # Hurt tint
STAR_YELLOW = (255, 230, 80)      # Impact stars
HALO_GOLD = (255, 240, 150, 160)  # Halo

OUTLINE_W = 3  # Outline thickness


def draw_rounded_rect(draw, bbox, fill, outline_color, outline_width, radius=10):
    """Draw a rounded rectangle with outline."""
    x0, y0, x1, y1 = bbox
    # Fill
    draw.rounded_rectangle(bbox, radius=radius, fill=fill)
    # Outline
    draw.rounded_rectangle(bbox, radius=radius, outline=outline_color, width=outline_width)


def draw_ellipse_outlined(draw, bbox, fill, outline_color=OUTLINE, width=OUTLINE_W):
    """Draw a filled ellipse with outline."""
    draw.ellipse(bbox, fill=fill, outline=outline_color, width=width)


def draw_body(draw, ox=0, oy=0, squish_x=0, squish_y=0):
    """Draw the main capybara body — big rounded rectangle."""
    # Main body (chunky oval)
    body_bbox = (22 + ox - squish_x, 38 + oy - squish_y, 100 + ox + squish_x, 95 + oy + squish_y)
    draw_ellipse_outlined(draw, body_bbox, BODY)

    # Belly highlight (lighter oval inside lower body)
    belly_bbox = (35 + ox, 60 + oy, 90 + ox + squish_x, 90 + oy + squish_y)
    draw.ellipse(belly_bbox, fill=BELLY)


def draw_head(draw, ox=0, oy=0, tilt=0):
    """Draw the capybara head — round with blunt snout."""
    # Head (circle-ish)
    head_bbox = (58 + ox, 15 + oy + tilt, 112 + ox, 65 + oy + tilt)
    draw_ellipse_outlined(draw, head_bbox, BODY)

    # Snout (protruding oval on right side of head)
    snout_bbox = (92 + ox, 32 + oy + tilt, 122 + ox, 58 + oy + tilt)
    draw_ellipse_outlined(draw, snout_bbox, SNOUT)

    # Nose (dark oval at tip of snout)
    nose_bbox = (110 + ox, 38 + oy + tilt, 122 + ox, 48 + oy + tilt)
    draw_ellipse_outlined(draw, nose_bbox, NOSE, OUTLINE, 2)
    # Nose highlight
    draw.ellipse((113 + ox, 40 + oy + tilt, 117 + ox, 44 + oy + tilt), fill=NOSE_HIGHLIGHT)


def draw_eyes(draw, ox=0, oy=0, state="normal", tilt=0):
    """Draw eyes based on state."""
    ey = 30 + oy + tilt
    ex = 74 + ox

    if state == "normal":
        # Big round eye
        draw.ellipse((ex, ey, ex + 12, ey + 14), fill=EYE_BLACK, outline=OUTLINE, width=2)
        # White highlight
        draw.ellipse((ex + 3, ey + 3, ex + 7, ey + 7), fill=EYE_WHITE)

    elif state == "happy":
        # Closed happy eye (arc)
        draw.arc((ex, ey + 2, ex + 12, ey + 14), start=200, end=340, fill=EYE_BLACK, width=3)

    elif state == "x_eyes":
        # X eyes for hurt/dead
        cx, cy = ex + 6, ey + 7
        draw.line((cx - 5, cy - 5, cx + 5, cy + 5), fill=HURT_RED, width=3)
        draw.line((cx - 5, cy + 5, cx + 5, cy - 5), fill=HURT_RED, width=3)

    elif state == "dizzy":
        # Spiral/dizzy eyes
        cx, cy = ex + 6, ey + 7
        draw.arc((cx - 6, cy - 6, cx + 6, cy + 6), start=0, end=270, fill=EYE_BLACK, width=2)
        draw.arc((cx - 3, cy - 3, cx + 3, cy + 3), start=90, end=360, fill=EYE_BLACK, width=2)


def draw_ears(draw, ox=0, oy=0, tilt=0, perked=False):
    """Draw small round ears on top of head."""
    ear_y = 14 + oy + tilt
    if perked:
        ear_y -= 4

    # Left ear (small circle)
    draw_ellipse_outlined(draw, (62 + ox, ear_y, 74 + ox, ear_y + 10), BODY)
    draw.ellipse((65 + ox, ear_y + 2, 71 + ox, ear_y + 8), fill=EAR_INNER)

    # Right ear
    draw_ellipse_outlined(draw, (82 + ox, ear_y - 2, 94 + ox, ear_y + 8), BODY)
    draw.ellipse((85 + ox, ear_y, 91 + ox, ear_y + 6), fill=EAR_INNER)


def draw_mouth(draw, ox=0, oy=0, state="neutral", tilt=0):
    """Draw mouth based on expression."""
    mx = 98 + ox
    my = 50 + oy + tilt

    if state == "neutral":
        # Small content line
        draw.arc((mx, my, mx + 12, my + 8), start=0, end=180, fill=MOUTH, width=2)
    elif state == "smile":
        draw.arc((mx - 2, my - 2, mx + 14, my + 10), start=0, end=180, fill=MOUTH, width=2)
    elif state == "open":
        # Open mouth (ouch!)
        draw.ellipse((mx + 2, my, mx + 12, my + 10), fill=(80, 40, 35), outline=OUTLINE, width=2)
    elif state == "tongue":
        draw.arc((mx, my, mx + 12, my + 8), start=0, end=180, fill=MOUTH, width=2)
        # Tongue
        draw.ellipse((mx + 4, my + 5, mx + 12, my + 14), fill=(210, 120, 120), outline=OUTLINE, width=2)


def draw_cheek_blush(draw, ox=0, oy=0, tilt=0):
    """Soft pink blush circles on cheeks."""
    # Create a temporary image for the semi-transparent blush
    bx = 88 + ox
    by = 48 + oy + tilt
    draw.ellipse((bx, by, bx + 12, by + 7), fill=(220, 160, 150))


def draw_legs(draw, ox=0, oy=0, pose="standing"):
    """Draw four stubby legs."""
    leg_y_top = 82 + oy
    leg_y_bot = 108 + oy

    if pose == "standing":
        positions = [(30 + ox, 38 + ox), (48 + ox, 56 + ox), (68 + ox, 76 + ox), (84 + ox, 92 + ox)]
        for lx0, lx1 in positions:
            draw_rounded_rect(draw, (lx0, leg_y_top, lx1, leg_y_bot), FEET, OUTLINE, OUTLINE_W, radius=4)
            # Little paw line
            draw.line((lx0 + 2, leg_y_bot - 4, lx1 - 2, leg_y_bot - 4), fill=OUTLINE, width=1)

    elif pose == "walk_spread":
        # Back legs
        draw_rounded_rect(draw, (26 + ox, leg_y_top - 2, 36 + ox, leg_y_bot + 2), FEET, OUTLINE, OUTLINE_W, radius=4)
        draw_rounded_rect(draw, (50 + ox, leg_y_top, 60 + ox, leg_y_bot - 2), FEET, OUTLINE, OUTLINE_W, radius=4)
        # Front legs
        draw_rounded_rect(draw, (66 + ox, leg_y_top, 76 + ox, leg_y_bot - 2), FEET, OUTLINE, OUTLINE_W, radius=4)
        draw_rounded_rect(draw, (88 + ox, leg_y_top - 2, 98 + ox, leg_y_bot + 2), FEET, OUTLINE, OUTLINE_W, radius=4)

    elif pose == "walk_together":
        # Legs closer together (passing position), body slightly higher
        positions = [(36 + ox, 46 + ox), (50 + ox, 60 + ox), (72 + ox, 82 + ox), (80 + ox, 90 + ox)]
        for lx0, lx1 in positions:
            draw_rounded_rect(draw, (lx0, leg_y_top - 4, lx1, leg_y_bot - 4), FEET, OUTLINE, OUTLINE_W, radius=4)

    elif pose == "tucked":
        # Tucked up legs (jumping)
        positions = [(34 + ox, 42 + ox), (48 + ox, 56 + ox), (68 + ox, 76 + ox), (82 + ox, 90 + ox)]
        for lx0, lx1 in positions:
            draw_rounded_rect(draw, (lx0, leg_y_top + 2, lx1, leg_y_top + 14), FEET, OUTLINE, OUTLINE_W, radius=4)

    elif pose == "splayed":
        # Splayed out (hurt)
        draw_rounded_rect(draw, (18 + ox, leg_y_top, 28 + ox, leg_y_bot + 4), FEET, OUTLINE, OUTLINE_W, radius=4)
        draw_rounded_rect(draw, (44 + ox, leg_y_top + 2, 54 + ox, leg_y_bot + 6), FEET, OUTLINE, OUTLINE_W, radius=4)
        draw_rounded_rect(draw, (68 + ox, leg_y_top + 2, 78 + ox, leg_y_bot + 6), FEET, OUTLINE, OUTLINE_W, radius=4)
        draw_rounded_rect(draw, (94 + ox, leg_y_top, 104 + ox, leg_y_bot + 4), FEET, OUTLINE, OUTLINE_W, radius=4)


def draw_tail(draw, ox=0, oy=0, wag=0):
    """Draw the tiny tail stub."""
    tx = 22 + ox
    ty = 52 + oy + wag
    draw_ellipse_outlined(draw, (tx - 6, ty, tx + 4, ty + 8), BODY_SHADOW, OUTLINE, 2)


def draw_impact_stars(draw, x, y, size=8):
    """Draw cartoon impact stars."""
    for angle_deg in range(0, 360, 45):
        angle = math.radians(angle_deg)
        inner_r = size * 0.4
        outer_r = size
        # Star point
        px = x + math.cos(angle) * outer_r
        py = y + math.sin(angle) * outer_r
        draw.line((x, y, int(px), int(py)), fill=STAR_YELLOW, width=2)
    draw.ellipse((x - 3, y - 3, x + 3, y + 3), fill=(255, 255, 255))


def draw_halo(draw, cx, y):
    """Draw a floating halo."""
    draw.ellipse((cx - 18, y - 5, cx + 18, y + 5), outline=(255, 230, 120), width=3)
    draw.ellipse((cx - 16, y - 3, cx + 16, y + 3), outline=(255, 245, 180), width=1)


# === FRAME GENERATORS ===

def frame_idle():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    draw_tail(draw)
    draw_legs(draw, pose="standing")
    draw_body(draw)
    draw_head(draw)
    draw_ears(draw)
    draw_eyes(draw, state="normal")
    draw_mouth(draw, state="smile")
    draw_cheek_blush(draw)

    return img


def frame_walk1():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    draw_tail(draw, wag=-3)
    draw_legs(draw, pose="walk_spread")
    draw_body(draw)
    draw_head(draw)
    draw_ears(draw)
    draw_eyes(draw, state="normal")
    draw_mouth(draw, state="neutral")
    draw_cheek_blush(draw)

    return img


def frame_walk2():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    draw_tail(draw, wag=3)
    draw_legs(draw, pose="walk_together")
    draw_body(draw, oy=-3)
    draw_head(draw, oy=-3)
    draw_ears(draw, oy=-3)
    draw_eyes(draw, oy=-3, state="happy")
    draw_mouth(draw, oy=-3, state="smile")
    draw_cheek_blush(draw, oy=-3)

    return img


def frame_jump():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    oy = -12

    draw_tail(draw, oy=oy, wag=-5)
    draw_legs(draw, oy=oy, pose="tucked")
    draw_body(draw, oy=oy)
    draw_head(draw, oy=oy)
    draw_ears(draw, oy=oy, perked=True)
    draw_eyes(draw, oy=oy, state="normal")
    draw_mouth(draw, oy=oy, state="neutral")
    draw_cheek_blush(draw, oy=oy)

    return img


def frame_hurt():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    oy = 5

    draw_tail(draw, oy=oy, wag=5)
    draw_legs(draw, oy=oy, pose="splayed")
    draw_body(draw, oy=oy, squish_x=3, squish_y=-3)
    draw_head(draw, oy=oy, tilt=3)
    draw_ears(draw, oy=oy, tilt=3)
    draw_eyes(draw, oy=oy, state="x_eyes", tilt=3)
    draw_mouth(draw, oy=oy, state="open", tilt=3)

    # Impact stars
    draw_impact_stars(draw, 20, 20, size=10)
    draw_impact_stars(draw, 110, 15, size=7)

    return img


def frame_dead():
    img = Image.new("RGBA", (CELL, CELL), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    # Flipped on back — draw everything rotated
    # Body (horizontal, higher up since on back)
    body_bbox = (14, 50, 95, 105)
    draw_ellipse_outlined(draw, body_bbox, BODY)

    # Belly exposed (facing up)
    draw.ellipse((24, 52, 85, 85), fill=BELLY)

    # Head flopped to side
    head_bbox = (72, 65, 120, 110)
    draw_ellipse_outlined(draw, head_bbox, BODY)

    # Snout sideways
    snout_bbox = (100, 82, 125, 108)
    draw_ellipse_outlined(draw, snout_bbox, SNOUT)

    # X eyes
    ex, ey = 84, 74
    draw.line((ex - 4, ey - 4, ex + 4, ey + 4), fill=HURT_RED, width=3)
    draw.line((ex - 4, ey + 4, ex + 4, ey - 4), fill=HURT_RED, width=3)

    # Tongue out
    draw.ellipse((108, 100, 118, 112), fill=(210, 120, 120), outline=OUTLINE, width=2)

    # Legs sticking up (comical)
    leg_positions = [(22, 32), (40, 50), (58, 68), (74, 84)]
    for lx0, lx1 in leg_positions:
        draw_rounded_rect(draw, (lx0, 25, lx1, 52), FEET, OUTLINE, OUTLINE_W, radius=4)
        # Paw pad circles on bottom of feet (now visible since upside down)
        cx = (lx0 + lx1) // 2
        draw.ellipse((cx - 3, 27, cx + 3, 33), fill=BELLY)

    # Halo
    draw_halo(draw, 55, 18)

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
        frame.save(f"assets/sprites/cartoon/capybara_{name}.png")

    sheet.save("assets/sprites/cartoon/capybara_sheet_128.png")

    # Also save at 2x (256px per frame) for high-res
    sheet_2x = sheet.resize((CELL * FRAMES * 2, CELL * 2), Image.LANCZOS)
    sheet_2x.save("assets/sprites/cartoon/capybara_sheet_256.png")

    print(f"Generated {len(frames)} cartoon capybara frames:")
    for i, (name, _) in enumerate(frames):
        print(f"  Frame {i}: {name}")
    print(f"\nSaved to assets/sprites/cartoon/")
    print(f"  Sheet: {CELL * FRAMES}x{CELL} (128px) + {CELL * FRAMES * 2}x{CELL * 2} (256px)")


if __name__ == "__main__":
    assemble_sheet()
