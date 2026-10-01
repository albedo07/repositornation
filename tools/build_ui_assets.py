"""Builds Immortal Heroes UI assets from source art.

v0.15.0:
- Footer (Hotbar area) of the Cleric/Paladin reference is replaced with the approved target
  footer (docs/source_art/Footer_Target.webp), mapped onto the reference frame geometry.
- The wide Heaven's Light box becomes a slot-style Grace box (slightly larger than a skill slot).
- Generates the +/- Tier buttons and the CONFIRM plaque.

Usage: python3 tools/build_ui_assets.py   (needs Pillow + numpy)
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "source_art")
OUT = os.path.join(ROOT, "ImmortalHeroesAssets")

REF_W, REF_H = 1011, 662
SEAM_Y = 526  # inside the dark band between the tree panels and the footer frame

# Reference -> target(1011-space) mapping measured from the frame geometry.
#   x: ref 17..990  -> tgt 16..1000
#   y: ref 523.5..656.5 -> tgt 521.5..652
AX = (1000 - 16) / (990 - 17)
BX = 16 - 17 * AX
AY = (652 - 521.5) / (656.5 - 523.5)
BY = 521.5 - 523.5 * AY

# Grace box geometry (reference space).
SLOT = 49            # numbered hotbar slot size in the new footer
GRACE = 56           # Grace slot: slightly larger, distinct gold frame
GRACE_CX = 691
GRACE_BOTTOM = 597   # bottoms align with the numbered slots


def composite_footer(ref, target):
    tw, th = target.size
    sx, sy = tw / 1011.0, th / 662.0
    # PIL affine maps output(ref) pixel -> input(target original) pixel.
    warped = target.transform(
        (REF_W, REF_H), Image.AFFINE,
        (AX * sx, 0, BX * sx, 0, AY * sy, BY * sy),
        resample=Image.BICUBIC)
    out = ref.copy()
    # Only inside the outer frame (x 15..993); the window margin keeps the reference pixels.
    out.paste(warped.crop((15, SEAM_Y, 993, REF_H)), (15, SEAM_Y))
    # 3px cross-fade so the seam is invisible.
    for i in range(3):
        y = SEAM_Y - 3 + i
        a = (i + 1) / 4.0
        row = Image.blend(ref.crop((15, y, 993, y + 1)), warped.crop((15, y, 993, y + 1)), a)
        out.paste(row, (15, y))
    return out


def erase_box(img, box):
    """Fill a rectangle by interpolating between the panel texture on its left and right."""
    x0, y0, x1, y1 = box
    a = np.asarray(img, dtype=np.float32).copy()
    left = a[y0:y1, x0 - 4:x0].mean(axis=1)
    right = a[y0:y1, x1:x1 + 4].mean(axis=1)
    w = x1 - x0
    t = (np.arange(w, dtype=np.float32) + 0.5) / w
    fill = left[:, None, :] * (1 - t[None, :, None]) + right[:, None, :] * t[None, :, None]
    # Re-introduce the panel's fine grain so the patch is not perfectly smooth.
    rng = np.random.default_rng(7)
    fill += rng.normal(0, 1.2, fill.shape[:2])[:, :, None]
    a[y0:y1, x0:x1] = np.clip(fill, 0, 255)
    return Image.fromarray(a.astype(np.uint8))


def grace_slot(ref_original):
    # The approved Heaven's Light node art (gold frame + star) from the tree.
    crop = (380, 407, 453, 477)
    icon = ref_original.crop(crop).convert("RGBA")
    h = int(round(GRACE * (crop[3] - crop[1]) / float(crop[2] - crop[0])))
    icon = icon.resize((GRACE, h), Image.LANCZOS)
    # Alpha from "not parchment": the frame is gold/dark, the corners outside it are pale pink.
    a = np.asarray(icon, dtype=np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    parchment = (r > 175) & (g > 140) & (b > 130) & (r - b < 70)
    alpha = np.where(parchment, 0, 255).astype(np.uint8)
    # Only knock out parchment that touches the outside (keep the bright star inside).
    keep = np.zeros_like(alpha)
    keep[4:-4, 4:-4] = 255
    alpha = np.maximum(alpha, keep)
    icon.putalpha(Image.fromarray(alpha).filter(ImageFilter.GaussianBlur(0.6)))
    return icon


def add_glow(img, center, radius, color, strength):
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(glow)
    cx, cy = center
    d.ellipse((cx - radius, cy - radius * 0.7, cx + radius, cy + radius * 0.7), fill=color + (strength,))
    glow = glow.filter(ImageFilter.GaussianBlur(radius * 0.45))
    return Image.alpha_composite(img.convert("RGBA"), glow)


def build_backdrop():
    ref = Image.open(os.path.join(SRC, "Cleric_Paladin_Reference_v0.14.png")).convert("RGB")
    target = Image.open(os.path.join(SRC, "Footer_Target.webp")).convert("RGB")
    out = composite_footer(ref, target)

    # Remove the wide HEAVEN'S LIGHT box (keep the baked "M4 + R" under it).
    out = erase_box(out, (634, 531, 751, 606))

    slot = grace_slot(ref)
    gx0 = GRACE_CX - slot.width // 2
    gy0 = GRACE_BOTTOM - slot.height
    out = add_glow(out, (GRACE_CX, gy0 + slot.height // 2), 44, (255, 200, 90), 105)
    out.alpha_composite(slot, (gx0, gy0))
    return out.convert("RGBA")


def rounded_button(size, glyph, scale=4):
    s = size * scale
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    r = int(s * 0.22)
    gold = (236, 190, 92, 255)
    dark_gold = (120, 84, 30, 255)
    d.rounded_rectangle((0, 0, s - 1, s - 1), radius=r, fill=dark_gold)
    d.rounded_rectangle((scale, scale, s - 1 - scale, s - 1 - scale), radius=r - scale, fill=gold)
    inset = int(scale * 2.2)
    # vertical navy gradient interior
    inner = Image.new("RGBA", (s - 2 * inset, s - 2 * inset))
    for y in range(inner.height):
        t = y / max(1, inner.height - 1)
        c = (int(28 - 12 * t), int(46 - 20 * t), int(78 - 30 * t), 255)
        ImageDraw.Draw(inner).line((0, y, inner.width, y), fill=c)
    m = Image.new("L", inner.size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, inner.width - 1, inner.height - 1), radius=r - inset, fill=255)
    img.paste(inner, (inset, inset), m)
    # glyph
    t = int(s * 0.10)
    L = int(s * 0.27)
    c = s // 2
    d.rectangle((c - L, c - t // 2, c + L, c + t // 2), fill=gold)
    if glyph == "+":
        d.rectangle((c - t // 2, c - L, c + t // 2, c + L), fill=gold)
    return img.resize((size * 2, size * 2), Image.LANCZOS)  # stored at 2x for crispness


def confirm_plaque(w, h, scale=4):
    W, H = w * scale, h * scale
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    gold = (236, 190, 92, 255)
    dark_gold = (110, 76, 26, 255)
    notch = int(H * 0.32)
    poly = lambda i: [(notch + i, i), (W - notch - i, i), (W - 1 - i, H // 2), (W - notch - i, H - 1 - i),
                      (notch + i, H - 1 - i), (i, H // 2)]
    d.polygon(poly(0), fill=dark_gold)
    d.polygon(poly(scale), fill=gold)
    d.polygon(poly(int(scale * 2.4)), fill=(20, 34, 62, 255))
    d.line(poly(int(scale * 4.2)) + [poly(int(scale * 4.2))[0]], fill=(200, 155, 70, 150), width=scale)
    for cx in (int(H * 0.55), W - int(H * 0.55)):
        cy = H // 2
        k = int(H * 0.09)
        d.polygon([(cx, cy - k), (cx + k, cy), (cx, cy + k), (cx - k, cy)], fill=gold)
    return img.resize((w * 2, h * 2), Image.LANCZOS)


def main():
    backdrop = build_backdrop()
    backdrop.save(os.path.join(OUT, "Cleric_Paladin_Reference.png"))
    rounded_button(16, "+").save(os.path.join(OUT, "Tier_Plus.png"))
    rounded_button(16, "-").save(os.path.join(OUT, "Tier_Minus.png"))
    confirm_plaque(160, 42).save(os.path.join(OUT, "Confirm_Plaque.png"))
    print("UI assets written to", OUT)


if __name__ == "__main__":
    main()
