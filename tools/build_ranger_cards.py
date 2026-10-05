"""v0.24.0 placeholder Altar cards for Ranger / Acrobat / Bowmaster (until the user's paintings).
Each card reuses an existing painted card (frame + emblem) re-hued to the class colour (gold kept),
with a figure-free landscape from the Art Refresh scenes in the art panel.
Output: ImmortalHeroesAssets/Altar_RangerCards.png (3 cards of 580x283, 590 px apart) read by
Core.cs AltarCardSprite.
Usage: python3 tools/build_ranger_cards.py
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
SCENES = os.path.join(ROOT, "docs", "source_art", "art_refresh", "scenes")
# v0.24.2: the user's Ranger / Acrobat / Bowmaster portraits (square, never stretched: cover-crop).
CARDS_ART = os.path.join(ROOT, "docs", "source_art", "ranger_kali", "cards")
# Measured card frames in Altar_ClassCards.png (= Core.cs AltarCardBounds): Cleric, Sword Master, Spellcaster.
CARD = {"Cleric": (594, 12, 582, 282), "Sword Master": (8, 301, 580, 284), "Spellcaster": (1183, 592, 583, 282),
        "Wizard": (595, 592, 581, 282)}
# (card donor, hue, saturation scale, scene, scene crop x-range fraction)
CARDS = [("Cleric", 115, 0.85, "ranger", 0.30),
         ("Spellcaster", 170, 0.80, "acrobat", 0.30),
         ("Wizard", 95, 0.80, "bowmaster", 0.30)]
PANEL = (252, 22, 563, 263)
EMBLEM = (126, 141, 112)       # emblem disc (card px): centre + radius inside the outer ring   # art panel interior inside the frame (card px)


def rehue(img, hue, sat_scale):
    a = np.asarray(img.convert("RGB"), dtype=np.float64) / 255.0
    mx, mn = a.max(2), a.min(2)
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    d = np.maximum(mx - mn, 1e-6)
    h = np.zeros_like(mx)
    h = np.where(mx == r, ((g - b) / d) % 6, h)
    h = np.where(mx == g, (b - r) / d + 2, h)
    h = np.where(mx == b, (r - g) / d + 4, h)
    h *= 60.0
    gold = (h >= 22) & (h <= 62) & (s > 0.35)
    sel = (s > 0.08) & ~gold
    nh = np.full_like(h, hue) / 60.0
    c = mx * s * sat_scale
    x = c * (1 - np.abs(nh % 2 - 1))
    m0 = mx - c
    i = np.floor(nh).astype(int) % 6
    z = 0 * c
    rr = np.choose(i, [c, x, z, z, x, c]) + m0
    gg = np.choose(i, [x, c, c, x, z, z]) + m0
    bb = np.choose(i, [z, z, x, c, c, x]) + m0
    new = np.stack([rr, gg, bb], -1)
    out = np.where(sel[..., None], new, a)
    return Image.fromarray(np.clip(out * 255, 0, 255).astype(np.uint8))


def panel_scene(name, focus_y, size, hue):
    """Portrait cover-cropped to the art panel (keeps aspect), lightly lifted."""
    sc = Image.open(os.path.join(CARDS_ART, name + ".jpg")).convert("RGB")
    w, h = sc.size
    pw, ph = size
    ch = int(w * ph / float(pw))
    y0 = int(np.clip(focus_y * h - ch / 2.0, 0, h - ch))
    crop = sc.crop((0, y0, w, y0 + ch)).resize(size, Image.LANCZOS)
    a = np.asarray(crop, dtype=np.float64)
    # v0.24.3: no parchment wash (it read as a faded sheet); just a light lift so the dark
    # portrait sits in the same brightness range as the other painted cards.
    # v0.24.4: high-key like the painted cards (bright midtones, colour kept) so the shared 30%
    # label veil disappears into the art instead of reading as a parchment slab on a dark portrait.
    out = 255.0 * np.power(a / 255.0, 0.5)
    grey = out.mean(axis=2, keepdims=True)
    out = grey + (out - grey) * 1.25
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def bow_emblem(card):
    """Bowmaster: the Archmage emblem (gold crescent + staff) turned 90 degrees reads as a drawn
    longbow with its arrow pointing forward. Only the round emblem turns (circular cut inside the
    outer gold ring, which is symmetric), the card frame is untouched."""
    cx, cy, r = EMBLEM
    patch = card.crop((cx - r, cy - r, cx + r, cy + r)).rotate(-90, resample=Image.BICUBIC)
    mask = Image.new("L", (2 * r * 4, 2 * r * 4), 0)
    ImageDraw.Draw(mask).ellipse([0, 0, 2 * r * 4 - 1, 2 * r * 4 - 1], fill=255)
    mask = mask.resize((2 * r, 2 * r), Image.LANCZOS)
    out = card.copy()
    out.paste(patch, (cx - r, cy - r), mask)
    return out


def main():
    sheet = Image.open(os.path.join(A, "Altar_ClassCards.png")).convert("RGB")
    out = Image.new("RGB", (1760, 283), (0, 0, 0))
    for i, (donor, hue, sat, scene, frac) in enumerate(CARDS):
        x, y, w, h = CARD[donor]
        card = sheet.crop((x, y, x + w, y + h)).resize((580, 283), Image.LANCZOS)
        card = rehue(card, hue, sat)
        x0, y0, x1, y1 = PANEL
        art = panel_scene(scene, frac, (x1 - x0, y1 - y0), hue)
        if donor == "Wizard":
            card = bow_emblem(card)
        mask = Image.new("L", (x1 - x0, y1 - y0), 0)
        ImageDraw.Draw(mask).rounded_rectangle([2, 2, x1 - x0 - 3, y1 - y0 - 3], radius=24, fill=255)
        mask = mask.filter(ImageFilter.GaussianBlur(1.5))
        card.paste(art, (x0, y0), mask)
        out.paste(card, (i * 590, 0))
    out.save(os.path.join(A, "Altar_RangerCards.png"))
    print("wrote Altar_RangerCards.png")


if __name__ == "__main__":
    main()
