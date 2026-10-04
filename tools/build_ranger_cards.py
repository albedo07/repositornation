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
# Measured card frames in Altar_ClassCards.png (= Core.cs AltarCardBounds): Cleric, Sword Master, Spellcaster.
CARD = {"Cleric": (594, 12, 582, 282), "Sword Master": (8, 301, 580, 284), "Spellcaster": (1183, 592, 583, 282)}
# (card donor, hue, saturation scale, scene, scene crop x-range fraction)
CARDS = [("Cleric", 115, 0.85, "warrior", (0.50, 1.0)),
         ("Spellcaster", 170, 0.80, "sword_master", (0.48, 1.0)),
         ("Sword Master", 95, 0.80, "archmage", (0.45, 1.0))]
PANEL = (252, 22, 563, 263)   # art panel interior inside the frame (card px)


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


def panel_scene(name, frac, size, hue):
    sc = Image.open(os.path.join(SCENES, name + ".jpg")).convert("RGB")
    w, h = sc.size
    crop = sc.crop((int(w * frac[0]), 0, int(w * frac[1]), h))
    pw, ph = size
    asp = pw / float(ph)
    cw, ch = crop.size
    if cw / float(ch) > asp:
        nw = int(ch * asp)
        crop = crop.crop(((cw - nw) // 2, 0, (cw - nw) // 2 + nw, ch))
    else:
        nh = int(cw / asp)
        crop = crop.crop((0, (ch - nh) // 2, cw, (ch - nh) // 2 + nh))
    crop = rehue(crop.resize(size, Image.LANCZOS), hue, 0.75)
    # Lighten like the existing card panels (painterly, parchment light).
    a = np.asarray(crop, dtype=np.float64)
    a = 255 - (255 - a) * 0.85
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def main():
    sheet = Image.open(os.path.join(A, "Altar_ClassCards.png")).convert("RGB")
    out = Image.new("RGB", (1760, 283), (0, 0, 0))
    for i, (donor, hue, sat, scene, frac) in enumerate(CARDS):
        x, y, w, h = CARD[donor]
        card = sheet.crop((x, y, x + w, y + h)).resize((580, 283), Image.LANCZOS)
        card = rehue(card, hue, sat)
        x0, y0, x1, y1 = PANEL
        art = panel_scene(scene, frac, (x1 - x0, y1 - y0), hue)
        mask = Image.new("L", (x1 - x0, y1 - y0), 0)
        ImageDraw.Draw(mask).rounded_rectangle([6, 6, x1 - x0 - 7, y1 - y0 - 7], radius=22, fill=255)
        mask = mask.filter(ImageFilter.GaussianBlur(5))
        card.paste(art, (x0, y0), mask)
        out.paste(card, (i * 590, 0))
    out.save(os.path.join(A, "Altar_RangerCards.png"))
    print("wrote Altar_RangerCards.png")


if __name__ == "__main__":
    main()
