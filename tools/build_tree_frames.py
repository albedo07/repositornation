"""v0.20.4 universal Skill Tree frames: locked mask + recolored frame overlays.

The painted node frames keep their colour in every state. Only the skill art inside a frame's
opening turns black and white when the skill is locked, and a skill whose category colour differs
from the painted slot (Priest Divine Intervention = Buff Green on the Shield Charge slot, Ascended
skills = Magenta, Ascended Ultimate = Red, ...) gets its frame band recoloured.

Outputs (ImmortalHeroesAssets/):
  Tree_LockMask.png            1011x662, alpha = how much a pixel greys when its node is locked
  Frame_<slot>_<color>.png     the frame band only (alpha), field rect padded by FRAME_PAD
FIELDS / FRAME_PAD / PRIEST_DONOR_FIELDS must match IhFieldRect / IhFramePad / IhPriestArtworkRect
in AlbedosCustomClasses.Advanced.cs.

Usage: python3 tools/build_tree_frames.py
"""
import os
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
W, H = 1011, 662

# Painted opening of every frame (x0, y0, x1, y1 exclusive), measured on Cleric_Paladin_PreAdvance.png.
FIELDS = {
    "lightning_zap": (175, 166, 229, 217),
    "righteous_strike": (175, 295, 228, 345),
    "holy_wave": (175, 420, 228, 469),
    "goddess_relic": (378, 166, 431, 217),
    "judgement_hammer": (379, 295, 433, 345),
    "heavens_light": (390, 418, 444, 469),
    "shield_charge": (553, 166, 605, 217),
    "fallen_angel": (690, 166, 743, 217),
    "ray_of_hope": (691, 295, 743, 345),
    "electric_smite": (863, 227, 952, 314),
}
# Same openings on Cleric_Priest_Artwork.png (scaled to 1011x662); the Priest art is blitted field -> field.
PRIEST_DONOR_FIELDS = {}  # filled by main() and printed for the C# table
PRIEST_DONOR_GUESS = {  # painted frame rects on the donor painting (search windows)
    "lightning_relic": "goddess_relic", "holy_relic": "judgement_hammer", "grand_sigil": "heavens_light",
    "divine_intervention": "shield_charge", "grand_cross": "fallen_angel",
    "heavens_judgement": "ray_of_hope", "lightning_tempest": "electric_smite",
}
FRAME_PAD = {"electric_smite": 13}
DEFAULT_PAD = 10
# Permanent badge diamonds sitting on the frame corner (center x, center y, radius): never greyed.
BADGES = {"goddess_relic": (431, 160, 11), "judgement_hammer": (432, 292, 11), "electric_smite": (949, 219, 15)}
# Painted frame colour of each slot on the universal (pre-Advancement) chassis.
PAINTED = {
    "lightning_zap": "cyan", "righteous_strike": "cyan", "holy_wave": "cyan",
    "goddess_relic": "navy", "judgement_hammer": "navy", "shield_charge": "cyan", "fallen_angel": "cyan",
    "ray_of_hope": "green", "electric_smite": "maroon", "heavens_light": "gold",
}
# Where each colour is sampled from (reference image, slot).
SAMPLES = {
    "cyan": ("Cleric_Paladin_PreAdvance.png", "shield_charge"),
    "navy": ("Cleric_Paladin_PreAdvance.png", "goddess_relic"),
    "green": ("Cleric_Paladin_PreAdvance.png", "ray_of_hope"),
    "maroon": ("Cleric_Paladin_PreAdvance.png", "electric_smite"),
    "magenta": ("Cleric_Paladin_Reference.png", "righteous_strike"),
}
HUE_BAND = {"cyan": (170, 210), "navy": (205, 245), "green": (95, 165), "maroon": (295, 360), "magenta": (280, 335)}
SMALL_COLORS = ("cyan", "navy", "green", "magenta")
ULT_COLORS = ("maroon", "red")


def hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-6
    rc = np.where(m & (mx == r), ((g - b) / np.where(m, d, 1)) % 6, 0)
    gc = np.where(m & (mx == g) & (mx != r), (b - r) / np.where(m, d, 1) + 2, 0)
    bc = np.where(m & (mx == b) & (mx != r) & (mx != g), (r - g) / np.where(m, d, 1) + 4, 0)
    h = (rc + gc + bc) * 60.0
    s = np.where(mx > 1e-6, d / np.where(mx > 1e-6, mx, 1), 0)
    return h % 360, s, mx


def rgb_from_hsv(h, s, v):
    h = (h % 360) / 60.0
    i = np.floor(h).astype(int) % 6
    f = h - np.floor(h)
    p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    out = np.zeros(h.shape + (3,))
    for k, (a, b, c) in enumerate(((v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q))):
        sel = i == k
        out[sel, 0], out[sel, 1], out[sel, 2] = a[sel], b[sel], c[sel]
    return out


def in_band(h, band):
    lo, hi = band
    return (h >= lo) & (h <= hi) if lo <= hi else (h >= lo) | (h <= hi)


def outer(slot):
    p = FRAME_PAD.get(slot, DEFAULT_PAD)
    x0, y0, x1, y1 = FIELDS[slot]
    return x0 - p, y0 - p, x1 + p, y1 + p


def ring_mask(rgb, slot, color, box):
    """Frame band pixels of `color` inside `box` (outer rect), excluding the opening."""
    h, s, v = hsv(rgb)
    band = HUE_BAND["maroon"] if color == "maroon" else HUE_BAND[color]
    m = in_band(h, band) & (s > 0.16) & (v > 0.12)
    if color == "maroon":
        m |= (h <= 12) & (s > 0.3)
    fx0, fy0, fx1, fy1 = FIELDS[slot]
    ox, oy = box[0], box[1]
    m[fy0 - oy:fy1 - oy, fx0 - ox:fx1 - ox] = False
    return m


def stats(img_name, color):
    img = np.asarray(Image.open(os.path.join(A, img_name)).convert("RGB"), dtype=np.float64) / 255.0
    slot = SAMPLES[color][1]
    box = outer(slot)
    rgb = img[box[1]:box[3], box[0]:box[2]]
    m = ring_mask(rgb, slot, color, box)
    h, s, v = hsv(rgb)
    hh = h[m]
    if color == "maroon":
        hh = np.where(hh < 90, hh + 360, hh)
    return np.median(hh) % 360, np.sort(s[m]), np.sort(v[m])


def quantile_map(x, src_sorted, dst_sorted):
    q = np.searchsorted(src_sorted, x) / max(1, len(src_sorted))
    idx = np.clip((q * (len(dst_sorted) - 1)).astype(int), 0, len(dst_sorted) - 1)
    return dst_sorted[idx]


def build_frames(pre, palette):
    made = []
    for slot, painted in PAINTED.items():
        if painted == "gold":
            continue
        box = outer(slot)
        rgb = pre[box[1]:box[3], box[0]:box[2]]
        m = ring_mask(rgb, slot, painted, box)
        h, s, v = hsv(rgb)
        src_h, src_s, src_v = palette[painted]
        targets = ULT_COLORS if slot == "electric_smite" else SMALL_COLORS
        for color in targets:
            if color == painted:
                continue
            dh, ds, dv = palette[color]
            hue_shift = h - src_h
            hue_shift = (hue_shift + 180) % 360 - 180
            nh = dh + hue_shift * 0.5
            ns = quantile_map(s, src_s, ds)
            nv = quantile_map(v, src_v, dv)
            out = rgb_from_hsv(nh, ns, nv)
            alpha = Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.45))
            a = np.maximum(np.asarray(alpha, dtype=np.float64) / 255.0, m * 1.0)
            rgba = np.dstack([out, a])
            name = "Frame_%s_%s.png" % (slot, color)
            Image.fromarray((rgba.clip(0, 1) * 255).astype(np.uint8), "RGBA").save(os.path.join(A, name))
            made.append(name)
    return made


def lock_mask(pre):
    """Opening pixels grey; the gold corner ornaments / badges that reach into the opening keep colour."""
    mask = np.zeros((H, W))
    h, s, v = hsv(pre)
    gold = in_band(h, (22, 62)) & (s > 0.35) & (v > 0.45)
    yy, xx = np.mgrid[0:H, 0:W]
    for slot, (x0, y0, x1, y1) in FIELDS.items():
        inside = (xx >= x0) & (xx < x1) & (yy >= y0) & (yy < y1)
        edge = np.minimum(np.minimum(xx - x0, x1 - 1 - xx), np.minimum(yy - y0, y1 - 1 - yy))
        keep = inside & (edge < 4) & gold if slot != "heavens_light" else inside & (edge < 2) & gold
        m = inside & ~keep
        if slot in BADGES:
            bx, by, br = BADGES[slot]
            m &= (np.abs(xx - bx) + np.abs(yy - by)) > br
        mask = np.maximum(mask, m * 1.0)
    # Grace slot of the footer (its own gold box): the opening only.
    gx0, gy0, gx1, gy1 = 670, 548, 714, 592
    mask[gy0:gy1, gx0:gx1] = 1.0
    alpha = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.4))
    a = np.asarray(alpha)
    rgba = np.dstack([np.full((H, W), 255, np.uint8)] * 3 + [a])
    Image.fromarray(rgba, "RGBA").save(os.path.join(A, "Tree_LockMask.png"))


def edge(prof):
    d = prof[:-1] - prof[1:]
    return int(np.argmax(d)) + 1


def detect_field(lum, frame):
    x, y, w, h = frame
    x0, y0, x1, y1 = x - 4, y - 4, x + w + 4, y + h + 4
    ym, xm = slice(y + h // 4, y + 3 * h // 4), slice(x + w // 4, x + 3 * w // 4)
    left = x0 + edge(lum[ym, x0:x0 + 18].mean(0))
    right = x1 - edge(lum[ym, x1 - 18:x1].mean(0)[::-1])
    top = y0 + edge(lum[y0:y0 + 18, xm].mean(1))
    bottom = y1 - edge(lum[y1 - 18:y1, xm].mean(1)[::-1])
    return left, top, right, bottom


def donor_fields():
    art = Image.open(os.path.join(A, "Cleric_Priest_Artwork.png")).convert("RGB").resize((W, H), Image.LANCZOS)
    lum = np.asarray(art, dtype=np.float64).max(-1) / 255.0
    for pid, slot in PRIEST_DONOR_GUESS.items():
        x0, y0, x1, y1 = FIELDS[slot]
        p = FRAME_PAD.get(slot, DEFAULT_PAD) - 2
        l, t, r, b = detect_field(lum, (x0 - p, y0 - p, x1 - x0 + 2 * p, y1 - y0 + 2 * p))
        # The donor painting follows the same template; the bottom edge detection can catch the
        # frame band below the art, so keep the slot's own size anchored on the detected left/top
        # (top clamped to +/-2 px of the slot) -> no foreign frame pixels enter the opening.
        t = min(max(t, y0 - 2), y0 + 2)
        l = min(max(l, x0 - 2), x0 + 2)
        PRIEST_DONOR_FIELDS[pid] = (l, t, l + (x1 - x0), t + (y1 - y0))


def main():
    pre = np.asarray(Image.open(os.path.join(A, "Cleric_Paladin_PreAdvance.png")).convert("RGB"), dtype=np.float64) / 255.0
    palette = {}
    for color, (img, _slot) in SAMPLES.items():
        palette[color] = stats(img, color)
    mh, ms, mv = palette["maroon"]
    palette["red"] = (356.0, np.clip(ms * 1.25 + 0.05, 0, 1), np.clip(mv * 0.92, 0, 1))
    made = build_frames(pre, palette)
    lock_mask(pre)
    donor_fields()
    print("frames:", len(made))
    for k, v in PRIEST_DONOR_FIELDS.items():
        print("donor", k, v, "w", v[2] - v[0], "h", v[3] - v[1])


if __name__ == "__main__":
    main()
