"""v0.21.1: real icon art for Righteous Strike, Judgement Hammer and Shield Charge (replaces the
"RS" / "JH" / "SC" letter placeholders) in the tree backdrops and the static hotbar icons.
Style matches the painted icons: pale glowing glyph on a radial field with soft rays; the field's
bottom bar strip is kept. Originals are kept in docs/source_art/backdrops_v0210/.
Usage: python3 tools/paint_skill_icons.py
"""
import math
import os
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
KEEP = os.path.join(ROOT, "docs", "source_art", "backdrops_v0210")
FIELDS = {"righteous_strike": (175, 295, 228, 345), "judgement_hammer": (379, 295, 433, 345),
          "shield_charge": (553, 166, 605, 217)}
BAR_ROWS = 9
# Bottom bar strip donor (the original strips of JH / SC contain letter / shield scraps).
BAR_DONOR = {"righteous_strike": (175, 295, 228, 345), "judgement_hammer": (378, 166, 431, 217), "shield_charge": (690, 166, 743, 217)}
S = 4  # supersampling


def field_background(src, w, h):
    """Radial glow from the original field's own colours + soft rays (no glyph)."""
    a = np.asarray(src.convert("RGB"), dtype=np.float64).reshape(-1, 3)
    lum = a.sum(1)
    order = np.argsort(lum)
    edge = a[order[: len(order) // 8]].mean(0)                       # darkest eighth
    bright = a[order[len(order) // 2: len(order) * 3 // 4]].mean(0)  # mid-bright band
    yy, xx = np.mgrid[0:h, 0:w]
    cx, cy = w / 2.0, h * 0.46
    r = np.hypot((xx - cx) / w, (yy - cy) / h) * 2.0
    t = np.clip(1.0 - r, 0, 1) ** 1.6
    out = edge[None, None, :] * (1 - t[..., None]) + bright[None, None, :] * t[..., None]
    ang = np.arctan2(yy - cy, xx - cx)
    rays = (np.cos(ang * 12) * 0.5 + 0.5) ** 8 * np.clip(1.1 - r, 0, 1) * 30
    out = np.clip(out + rays[..., None], 0, 255)
    return Image.fromarray(out.astype(np.uint8))


def glow_layer(draw_fn, w, h, glow_color, glow_radius):
    shape = Image.new("L", (w, h), 0)
    draw_fn(ImageDraw.Draw(shape))
    glow = shape.filter(ImageFilter.GaussianBlur(glow_radius))
    return shape, glow


def composite(bg, shape, glow, fill, glow_color, glow_alpha=0.85):
    base = bg.convert("RGBA")
    g = Image.new("RGBA", bg.size, glow_color + (0,))
    g.putalpha(glow.point(lambda v: int(min(255, v * glow_alpha * 1.6))))
    base.alpha_composite(g)
    w, h = bg.size
    outline = shape.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(2))
    o = Image.new("RGBA", bg.size, tuple(int(c * 0.30) for c in glow_color) + (0,))
    o.putalpha(outline.point(lambda v: int(v * 0.55)))
    base.alpha_composite(o)
    grad = np.linspace(0, 1, h)[:, None, None]
    top = np.array([255, 253, 240], dtype=np.float64)
    low = np.array(glow_color, dtype=np.float64) * 0.55 + 255 * 0.45
    col = np.broadcast_to(top * (1 - grad * 0.8) + low * (grad * 0.8), (h, w, 3))
    f = Image.fromarray(np.clip(col, 0, 255).astype(np.uint8)).convert("RGBA")
    f.putalpha(shape)
    base.alpha_composite(f)
    return base


def draw_righteous_strike(d, w, h):
    cx = w / 2.0
    # Soft column of holy light behind the bolt.
    d.rectangle([cx - 0.13 * w, 0, cx + 0.13 * w, 0.80 * h], fill=70)
    # Holy pillar of lightning from the sky onto the ground.
    pts = [(cx - 0.05 * w, 0.06 * h), (cx + 0.07 * w, 0.06 * h), (cx + 0.01 * w, 0.30 * h), (cx + 0.10 * w, 0.30 * h),
           (cx - 0.04 * w, 0.58 * h), (cx + 0.05 * w, 0.58 * h), (cx - 0.07 * w, 0.80 * h), (cx - 0.02 * w, 0.58 * h),
           (cx - 0.11 * w, 0.58 * h), (cx - 0.01 * w, 0.33 * h), (cx - 0.10 * w, 0.33 * h)]
    d.polygon(pts, fill=255)
    # Impact: a cross-shaped flare where it lands.
    iy = 0.80 * h
    d.ellipse([cx - 0.30 * w, iy - 0.045 * h, cx + 0.30 * w, iy + 0.045 * h], fill=200)
    for k in range(-1, 2, 2):
        d.polygon([(cx, iy - 0.02 * h), (cx + k * 0.40 * w, iy), (cx, iy + 0.02 * h)], fill=255)
    d.polygon([(cx - 0.02 * w, iy), (cx, iy - 0.16 * h), (cx + 0.02 * w, iy)], fill=255)


def draw_judgement_hammer(d, w, h):
    cx, cy = w * 0.5, h * 0.47
    a = math.radians(-38)
    ca, sa = math.cos(a), math.sin(a)

    def rot(x, y):
        return (cx + x * ca - y * sa, cy + x * sa + y * ca)
    # Handle (along the rotated x axis, pointing down-left), then the head.
    hw, hl = 0.035 * w, 0.40 * w
    d.polygon([rot(-hl, -hw), rot(0.05 * w, -hw), rot(0.05 * w, hw), rot(-hl, hw)], fill=235)
    d.ellipse([rot(-hl, 0)[0] - 0.06 * w, rot(-hl, 0)[1] - 0.06 * w, rot(-hl, 0)[0] + 0.06 * w, rot(-hl, 0)[1] + 0.06 * w], fill=235)
    head_w, head_h = 0.20 * w, 0.27 * w
    b = 0.035 * w  # bevelled corners
    x0, x1 = 0.03 * w, 0.03 * w + head_w
    d.polygon([rot(x0 + b, -head_h), rot(x1 - b, -head_h), rot(x1, -head_h + b), rot(x1, head_h - b), rot(x1 - b, head_h),
               rot(x0 + b, head_h), rot(x0, head_h - b), rot(x0, -head_h + b)], fill=255)
    # Gold-banded ends.
    for yy in (-head_h + 0.07 * w, head_h - 0.07 * w):
        d.polygon([rot(x0, yy - 0.015 * w), rot(x1, yy - 0.015 * w), rot(x1, yy + 0.015 * w), rot(x0, yy + 0.015 * w)], fill=170)
    # Grip wraps.
    for k in range(3):
        gx = -hl + (0.07 + 0.06 * k) * w
        d.polygon([rot(gx, -hw * 1.4), rot(gx + 0.025 * w, -hw * 1.4), rot(gx + 0.025 * w, hw * 1.4), rot(gx, hw * 1.4)], fill=190)
    # Small cross on the head.
    mx, my = rot(x0 + head_w / 2, 0)
    d.rectangle([mx - 0.012 * w, my - 0.09 * w, mx + 0.012 * w, my + 0.09 * w], fill=150)
    d.rectangle([mx - 0.06 * w, my - 0.03 * w, mx + 0.06 * w, my - 0.006 * w], fill=150)


def draw_shield_charge(d, w, h):
    cx, top, bot = w * 0.56, h * 0.14, h * 0.82
    half = 0.25 * w
    outline = [(cx - half, top), (cx + half, top), (cx + half, top + 0.36 * h), (cx, bot), (cx - half, top + 0.36 * h)]
    d.polygon(outline, fill=255)
    inner = [(cx - half * 0.72, top + 0.06 * h), (cx + half * 0.72, top + 0.06 * h), (cx + half * 0.72, top + 0.33 * h),
             (cx, bot - 0.09 * h), (cx - half * 0.72, top + 0.33 * h)]
    d.polygon(inner, fill=120)
    # Forward chevron on the shield face.
    d.polygon([(cx - 0.08 * w, top + 0.15 * h), (cx + 0.10 * w, top + 0.30 * h), (cx - 0.08 * w, top + 0.45 * h),
               (cx - 0.02 * w, top + 0.30 * h)], fill=255)
    # Speed streaks behind it.
    for i, yy in enumerate((0.30, 0.46, 0.62)):
        x1 = cx - half - 0.03 * w
        d.rectangle([x1 - (0.30 - 0.06 * i) * w, yy * h - 0.018 * h, x1, yy * h + 0.018 * h], fill=230)


PAINTERS = {"righteous_strike": draw_righteous_strike, "judgement_hammer": draw_judgement_hammer,
            "shield_charge": draw_shield_charge}


def paint(sid, field_img, donor_img):
    w, h = field_img.size
    W, H = w * S, h * S
    big_src = field_img.resize((W, H), Image.LANCZOS)
    bg = field_background(big_src, W, H)
    shape, glow = glow_layer(lambda d: PAINTERS[sid](d, W, H), W, H, (255, 255, 255), 5 * S)
    tint = tuple(int(v) for v in np.percentile(np.asarray(big_src.convert("RGB")).reshape(-1, 3), 90, axis=0))
    glow_col = tuple(int(0.5 * c + 0.5 * 255) for c in tint)
    art = composite(bg, shape, glow, (250, 252, 245), glow_col).convert("RGB").resize((w, h), Image.LANCZOS)
    # Keep the original bottom bar strip.
    strip = donor_img.resize((w, donor_img.height)).crop((0, donor_img.height - BAR_ROWS, w, donor_img.height))
    art.paste(strip, (0, h - BAR_ROWS))
    return art


def main():
    os.makedirs(KEEP, exist_ok=True)
    for name in ("Cleric_Paladin_PreAdvance.png", "Cleric_Paladin_Reference.png"):
        keep = os.path.join(KEEP, name)
        if not os.path.exists(keep):
            shutil.copy(os.path.join(A, name), keep)
        img = Image.open(keep).convert("RGBA")
        for sid, box in FIELDS.items():
            art = paint(sid, img.crop(box).convert("RGB"), img.crop(BAR_DONOR[sid]).convert("RGB"))
            if name == "Cleric_Paladin_Reference.png" and sid == "righteous_strike":
                # Reference shows Righteous Strike Ascended: keep its Magenta tint.
                ref = np.asarray(img.crop(box).convert("RGB"), dtype=np.float64).mean((0, 1))
                pre = np.asarray(Image.open(os.path.join(KEEP, "Cleric_Paladin_PreAdvance.png")).convert("RGB").crop(box), dtype=np.float64).mean((0, 1))
                arr = np.asarray(art, dtype=np.float64) * (ref / np.maximum(pre, 1))[None, None, :]
                art = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
            img.paste(art.convert("RGBA"), box[:2])
        img.save(os.path.join(A, name))
    # Static icons (Altar, fallback): hotbar frame + new field art, inset 7 / 8 px.
    pre = Image.open(os.path.join(A, "Cleric_Paladin_PreAdvance.png")).convert("RGBA")
    for sid, box in FIELDS.items():
        for icon_name in ("Icon_" + sid + ".png",) + (("Icon_righteous_strike_Normal.png",) if sid == "righteous_strike" else ()):
            path = os.path.join(A, icon_name)
            keep = os.path.join(KEEP, icon_name)
            if not os.path.exists(keep):
                shutil.copy(path, keep)
            icon = Image.open(keep).convert("RGBA")
            iw, ih = icon.width - 14, icon.height - 16
            x0, y0, x1, y1 = box
            fw, fh = x1 - x0 - 6, y1 - y0 - 6
            asp = iw / float(ih)
            cw = min(fw, fh * asp)
            ch = cw / asp
            cx, cy = (x0 + x1) / 2.0, (y0 + y1) / 2.0
            crop = pre.crop((int(round(cx - cw / 2)), int(round(cy - ch / 2)), int(round(cx + cw / 2)), int(round(cy + ch / 2))))
            icon.paste(crop.resize((iw, ih), Image.LANCZOS), (7, 8))
            icon.save(path)
    print("painted", list(FIELDS))


if __name__ == "__main__":
    main()
