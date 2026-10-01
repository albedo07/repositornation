"""Ideation previews for Skill Tree states (no Class / Class only / locked skills).

Builds on the approved v0.15.2 artwork; nothing here is shipped yet.
Usage: python3 tools/render_states.py   -> docs/previews/STATE_*.png
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
OUT = os.path.join(ROOT, "docs", "previews")
SERIF = "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"
SERIF_REG = "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf"

CLASS_PANEL = (32, 87, 310, 523)
AC_PANEL = (337, 88, 977, 523)
# Fill boxes reach up over the header ribbons/emblems so no class art peeks out.
CLASS_FILL = (32, 70, 312, 523)
AC_FILL = (336, 60, 978, 523)
GOLD = (236, 190, 92)
INK = (74, 50, 28)

NODES = {  # icon frame boxes (reference px), nameplate anchor (center x, bottom y)
    "lightning_zap": ((160, 152, 241, 234), (200, 247)),
    "righteous_strike": ((163, 285, 240, 362), (201, 375)),
    "holy_wave": ((163, 410, 240, 488), (201, 499)),
    "goddess_relic": ((365, 152, 444, 234), (404, 247)),
    "judgement_hammer": ((365, 285, 444, 362), (405, 375)),
    "heavens_light": ((377, 405, 456, 478), (416, 499)),
    "shield_charge": ((544, 152, 621, 234), (582, 247)),
    "fallen_angel": ((682, 152, 760, 234), (720, 247)),
    "ray_of_hope": ((682, 285, 760, 362), (720, 375)),
    "electric_smite": ((840, 204, 966, 330), (902, 355)),
}
SLOT_CENTERS = (229, 287, 344, 402, 459, 517, 574)
SLOT_TOP, SLOT_BOTTOM = 549, 598
GRACE_BOX = (663, 541, 720, 598)


def font(size, bold=True):
    return ImageFont.truetype(SERIF if bold else SERIF_REG, size)


def text_c(d, cx, y, text, f, fill, shadow=None):
    w = d.textlength(text, font=f)
    if shadow:
        d.text((cx - w / 2 + 1, y + 1), text, font=f, fill=shadow)
    d.text((cx - w / 2, y), text, font=f, fill=fill)


# ---------------------------------------------------------------- textures / components
def neutral_parchment(size, seed=3):
    """Cream parchment built from the art's own parchment grain, colorized neutral."""
    src = Image.open(os.path.join(ROOT, "docs", "source_art", "Cleric_Paladin_Reference_v0.14.png")).convert("L")
    grain = np.asarray(src.crop((470, 262, 650, 400)), dtype=np.float32)
    grain = (grain - grain.mean()) / (grain.std() + 1e-6)
    tile = np.concatenate([grain, grain[:, ::-1]], axis=1)
    tile = np.concatenate([tile, tile[::-1, :]], axis=0)
    w, h = size
    reps = (h // tile.shape[0] + 1, w // tile.shape[1] + 1)
    g = np.tile(tile, reps)[:h, :w]
    g = np.asarray(Image.fromarray(((g * 18) + 128).clip(0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8)),
                   dtype=np.float32) / 128.0 - 1.0
    rng = np.random.default_rng(seed)
    blotch = rng.normal(0, 1, (h // 40 + 2, w // 40 + 2)).astype(np.float32)
    blotch = np.asarray(Image.fromarray(((blotch * 30) + 128).clip(0, 255).astype(np.uint8)).resize((w, h), Image.BICUBIC),
                        dtype=np.float32) / 128.0 - 1.0
    base = np.array([233, 220, 193], dtype=np.float32)
    lum = 1.0 + g * 0.05 + blotch * 0.035
    yy, xx = np.mgrid[0:h, 0:w]
    vign = 1.0 - 0.10 * (((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    rgb = base[None, None, :] * (lum * vign)[..., None]
    return Image.fromarray(rgb.clip(0, 255).astype(np.uint8)).convert("RGBA")


def rounded_paste(img, patch, box, radius=16):
    x0, y0, x1, y1 = box
    m = Image.new("L", (x1 - x0, y1 - y0), 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, x1 - x0 - 1, y1 - y0 - 1), radius=radius, fill=255)
    img.paste(patch.resize((x1 - x0, y1 - y0)), (x0, y0), m)


def veil(img, box, color, alpha, radius=16):
    x0, y0, x1, y1 = box
    v = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(v).rounded_rectangle(box, radius=radius, fill=color + (alpha,))
    return Image.alpha_composite(img, v)


def padlock(size, scale=4):
    S = size * scale
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    dark = (96, 66, 22, 255)
    gold = GOLD + (255,)
    bw, bh = int(S * 0.70), int(S * 0.50)
    bx, by = (S - bw) // 2, int(S * 0.44)
    sw = int(S * 0.44)
    t = max(2, int(S * 0.09))
    d.arc((S // 2 - sw // 2, int(S * 0.10), S // 2 + sw // 2, int(S * 0.10) + sw), 180, 360, fill=dark, width=t + scale * 2)
    d.arc((S // 2 - sw // 2, int(S * 0.10), S // 2 + sw // 2, int(S * 0.10) + sw), 180, 360, fill=gold, width=t)
    d.line((S // 2 - sw // 2 + t // 2, int(S * 0.10) + sw // 2, S // 2 - sw // 2 + t // 2, by), fill=gold, width=t)
    d.line((S // 2 + sw // 2 - t // 2, int(S * 0.10) + sw // 2, S // 2 + sw // 2 - t // 2, by), fill=gold, width=t)
    d.rounded_rectangle((bx - scale, by - scale, bx + bw + scale, by + bh + scale), radius=int(S * 0.08), fill=dark)
    d.rounded_rectangle((bx, by, bx + bw, by + bh), radius=int(S * 0.07), fill=gold)
    d.rounded_rectangle((bx + scale * 2, by + scale * 2, bx + bw - scale * 2, by + bh - scale * 2), radius=int(S * 0.05),
                        outline=(255, 226, 150, 255), width=scale)
    kx, ky = S // 2, by + bh // 2 - int(S * 0.03)
    k = int(S * 0.06)
    d.ellipse((kx - k, ky - k, kx + k, ky + k), fill=dark)
    d.polygon([(kx - k // 2, ky), (kx + k // 2, ky), (kx + k // 3, ky + int(S * 0.12)), (kx - k // 3, ky + int(S * 0.12))], fill=dark)
    return im.resize((size, size), Image.LANCZOS)


def plaque(w, h, scale=3):
    W, H = w * scale, h * scale
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    notch = int(H * 0.30)
    poly = lambda i: [(notch + i, i), (W - notch - i, i), (W - 1 - i, H // 2), (W - notch - i, H - 1 - i),
                      (notch + i, H - 1 - i), (i, H // 2)]
    d.polygon(poly(0), fill=(110, 76, 26, 255))
    d.polygon(poly(scale), fill=GOLD + (255,))
    d.polygon(poly(int(scale * 2.6)), fill=(19, 31, 58, 245))
    d.line(poly(int(scale * 5)) + [poly(int(scale * 5))[0]], fill=(200, 155, 70, 140), width=scale)
    for cx in (int(H * 0.42), W - int(H * 0.42)):
        k = int(H * 0.07)
        d.polygon([(cx, H // 2 - k), (cx + k, H // 2), (cx, H // 2 + k), (cx - k, H // 2)], fill=GOLD + (255,))
    return im.resize((w, h), Image.LANCZOS)


def lock_node(img, node_id, req=None, padlock_size=22):
    """Locked skill: desaturated + darkened frame/icon (parchment untouched), faded nameplate,
    padlock badge, and the unlock requirement under the nameplate."""
    box, (cx, pb) = NODES[node_id]
    region = np.asarray(img.crop(box).convert("RGB"), dtype=np.float32) / 255.0
    mx, mn = region.max(axis=2), region.min(axis=2)
    sat = (mx - mn) / (mx + 1e-6)
    parchment = (mx > 0.62) & (sat < 0.32)
    gray = region.mean(axis=2, keepdims=True).repeat(3, axis=2)
    locked = gray * 0.50 + np.array([0.035, 0.03, 0.02])
    w = (~parchment).astype(np.float32)[..., None]
    w = np.asarray(Image.fromarray((w[..., 0] * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7)),
                   dtype=np.float32)[..., None] / 255.0
    out = region * (1 - w) + locked * w
    patch = Image.fromarray((out.clip(0, 1) * 255).astype(np.uint8)).convert("RGBA")
    img.paste(patch, box[:2])
    half = 64 if node_id == "electric_smite" else 52
    img = veil(img, (cx - half, pb - 25, cx + half, pb), (236, 226, 204), 125, radius=6)
    lk = padlock(padlock_size)
    img.alpha_composite(lk, (box[2] - padlock_size + 2, box[3] - padlock_size + 2))
    if req:
        d = ImageDraw.Draw(img)
        text_c(d, cx, pb + 2, req, font(10), INK)
    return img


def recolor_hue(img, box, h_lo, h_hi, target, spread=0.35):
    x0, y0, x1, y1 = box
    region = np.asarray(img.crop(box).convert("RGB"), dtype=np.float32) / 255.0
    r, g, b = region[..., 0], region[..., 1], region[..., 2]
    mx, mn = region.max(axis=2), region.min(axis=2)
    dl = mx - mn + 1e-6
    hue = np.where(mx == r, (g - b) / dl % 6, np.where(mx == g, (b - r) / dl + 2, (r - g) / dl + 4)) * 60.0
    sat = np.where(mx > 0, dl / (mx + 1e-6), 0)
    sel = (hue >= h_lo) & (hue <= h_hi) & (sat > 0.18)
    nh = (target + (hue - (h_lo + h_hi) / 2) * spread) % 360
    ns, nv = np.clip(sat, 0, 1), np.clip(mx * 1.05, 0, 1)
    hh = nh / 60.0
    i = np.floor(hh).astype(int) % 6
    f = hh - np.floor(hh)
    p, q, t = nv * (1 - ns), nv * (1 - ns * f), nv * (1 - ns * (1 - f))
    nr = np.choose(i, [nv, q, p, p, t, nv])
    ng = np.choose(i, [t, nv, nv, q, p, p])
    nb = np.choose(i, [p, p, t, nv, nv, q])
    wgt = (np.clip((sat - 0.18) / 0.15, 0, 1) * sel)[..., None]
    out = region * (1 - wgt) + np.stack([nr, ng, nb], axis=2) * wgt
    img.paste(Image.fromarray((out.clip(0, 1) * 255).astype(np.uint8)).convert("RGBA"), (x0, y0))
    return img


_SOCKET = None


def socket_art():
    """Neutral empty slot: the gold slot frame from the hotbar art with a dark recessed interior."""
    global _SOCKET
    if _SOCKET is None:
        src = Image.open(os.path.join(ROOT, "docs", "source_art", "Cleric_Paladin_Reference_v0.14.png")).convert("RGBA")
        frame = src.crop((380, 410, 453, 477)).resize((55, 55), Image.LANCZOS)
        d = ImageDraw.Draw(frame)
        for y in range(8, 47):
            t = (y - 8) / 39.0
            d.line((8, y, 46, y), fill=(int(14 + 8 * t), int(20 + 9 * t), int(34 + 12 * t), 255))
        _SOCKET = frame
    return _SOCKET


def empty_socket(img, cx, locked=False):
    """Empty hotbar slot: neutral gold frame, dark recessed interior."""
    img.alpha_composite(socket_art(), (cx - 27, SLOT_TOP - 3))
    d = ImageDraw.Draw(img)
    k = 3
    cy = (SLOT_TOP + SLOT_BOTTOM) // 2
    if locked:
        img.alpha_composite(padlock(20), (cx - 10, cy - 11))
    else:
        d.polygon([(cx, cy - k), (cx + k, cy), (cx, cy + k), (cx - k, cy)], fill=(150, 118, 60, 255))
    return img


def hotkey_labels(img, labels):
    d = ImageDraw.Draw(img)
    f = font(10)
    for cx, text, col in labels:
        text_c(d, cx, 608, text, f, col, shadow=(0, 0, 0))


def header_text(img, cx, y, title, sub=None, color=INK):
    d = ImageDraw.Draw(img)
    text_c(d, cx, y, title, font(22), color, shadow=(255, 245, 220))
    if sub:
        text_c(d, cx, y + 30, sub, font(11, False), (100, 78, 52))


def base():
    return Image.open(os.path.join(A, "Cleric_Paladin_Reference.png")).convert("RGBA")


def save(img, name):
    img.convert("RGB").resize((1180, 772), Image.LANCZOS).save(os.path.join(OUT, name))


# ---------------------------------------------------------------- states
def state_no_class():
    img = base()
    for panel in (CLASS_FILL, AC_FILL):
        rounded_paste(img, neutral_parchment((panel[2] - panel[0], panel[3] - panel[1]), seed=panel[0]), panel)
    header_text(img, 171, 104, "CLASS", "no Class chosen")
    header_text(img, 657, 104, "ADVANCEMENT", "locked")
    # faint compass emblem behind the message
    em = Image.open(os.path.join(ROOT, "docs", "source_art", "Cleric_Paladin_Reference_v0.14.png")).convert("RGBA").crop((38, 72, 120, 154))
    em = em.resize((150, 150), Image.LANCZOS)
    m = Image.new("L", (150, 150), 0)
    ImageDraw.Draw(m).ellipse((8, 8, 142, 142), fill=40)
    em.putalpha(m.filter(ImageFilter.GaussianBlur(6)))
    img.alpha_composite(em, (505 - 75, 300 - 75))
    pl = plaque(560, 96)
    img.alpha_composite(pl, (505 - 280, 300 - 48))
    d = ImageDraw.Draw(img)
    text_c(d, 505, 272, "CHOOSE A CLASS AT THE ALTAR FIRST", font(18), GOLD, shadow=(0, 0, 0))
    text_c(d, 505, 300, "Visit the Dragon's Altar to begin your path.", font(12, False), (226, 214, 186))
    for cx in SLOT_CENTERS:
        empty_socket(img, cx)
    erase_grace(img, locked=True)
    hotkey_labels(img, [(cx, str(i + 1), (150, 132, 100)) for i, cx in enumerate(SLOT_CENTERS)] + [(691, "M4 + R", (150, 132, 100))])
    save(img, "STATE_A_NoClass.png")


def erase_grace(img, locked):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = GRACE_BOX
    for y in range(y0 + 6, y1 - 5):
        t = (y - y0) / float(y1 - y0)
        d.line((x0 + 6, y, x1 - 6, y), fill=(int(22 + 8 * t), int(20 + 8 * t), int(16 + 8 * t), 255))
    if locked:
        img.alpha_composite(padlock(24), ((x0 + x1) // 2 - 12, (y0 + y1) // 2 - 13))


def class_only_common():
    img = base()
    # Before Advancement every Class skill is interchangeable -> Cyan (Lightning Zap not yet Ascended).
    img = recolor_hue(img, NODES["lightning_zap"][0], 280, 345, 186)
    img = recolor_hue(img, (205, 546, 254, 600), 280, 345, 186)
    for cx in SLOT_CENTERS[3:]:
        empty_socket(img, cx, locked=True)
    # Class hotbar: Lightning Zap / Righteous Strike / Holy Wave in slots 1-3
    src = img.copy()
    rs = src.crop((167, 289, 236, 352)).resize((49, 49), Image.LANCZOS)
    hw = src.crop((167, 414, 236, 477)).resize((49, 49), Image.LANCZOS)
    for cx, icon in ((287, rs), (344, hw)):
        img.paste(icon, (cx - 24, SLOT_TOP))
    erase_grace(img, locked=True)
    hotkey_labels(img, [(cx, str(i + 1), (237, 214, 158) if i < 3 else (140, 124, 96)) for i, cx in enumerate(SLOT_CENTERS)]
                  + [(691, "M4 + R", (140, 124, 96))])
    return img


def state_class_only_sealed():
    img = class_only_common()
    night = neutral_parchment((AC_FILL[2] - AC_FILL[0], AC_FILL[3] - AC_FILL[1]), seed=7)
    a = np.asarray(night.convert("RGB"), dtype=np.float32) / 233.0
    navy = np.array([24, 36, 62], dtype=np.float32)
    night = Image.fromarray((a * navy[None, None, :] * 1.15).clip(0, 255).astype(np.uint8)).convert("RGBA")
    rounded_paste(img, night, AC_FILL)
    d = ImageDraw.Draw(img)
    text_c(d, 657, 104, "ADVANCEMENT", font(24), GOLD, shadow=(0, 0, 0))
    text_c(d, 657, 134, "Sealed until you Advance", font(12, False), (214, 200, 170))
    img.alpha_composite(padlock(84), (657 - 42, 172))
    reqs = [("Reach Level 16", True), ("Max a Class skill to Tier 7", False), ("Complete the Advancement Quest", False)]
    y = 278
    for text, done in reqs:
        mark = "◆" if done else "◇"
        col = (236, 206, 130) if done else (206, 194, 168)
        text_c(d, 657, y, mark + "  " + text, font(14, False), col, shadow=(0, 0, 0))
        y += 28
    text_c(d, 657, 380, "CHOOSE YOUR PATH", font(13), (236, 206, 130), shadow=(0, 0, 0))
    pl = plaque(150, 40)
    for cx, name in ((577, "PALADIN"), (737, "PRIEST")):
        img.alpha_composite(pl, (cx - 75, 404))
        text_c(d, cx, 415, name, font(14), (190, 170, 128), shadow=(0, 0, 0))
    save(img, "STATE_B1_ClassOnly_Sealed.png")


def state_class_only_preview():
    img = class_only_common()
    # The AC tree stays visible as a preview: every AC node locked, panel desaturated + dimmed.
    region = np.asarray(img.crop(AC_PANEL).convert("RGB"), dtype=np.float32)
    gray = region.mean(axis=2, keepdims=True)
    desat = region * 0.30 + gray * 0.70
    img.paste(Image.fromarray(desat.clip(0, 255).astype(np.uint8)).convert("RGBA"), AC_PANEL[:2],
              Image.new("L", (AC_PANEL[2] - AC_PANEL[0], AC_PANEL[3] - AC_PANEL[1]), 255))
    for nid in ("goddess_relic", "judgement_hammer", "heavens_light", "shield_charge", "fallen_angel", "ray_of_hope", "electric_smite"):
        img = lock_node(img, nid)
    img = veil(img, AC_PANEL, (30, 30, 40), 55)
    d = ImageDraw.Draw(img)
    pl = plaque(300, 44)
    img.alpha_composite(pl, (657 - 150, 420))
    text_c(d, 657, 427, "PREVIEW  ·  ADVANCE AT LV 16", font(13), GOLD, shadow=(0, 0, 0))
    text_c(d, 657, 446, "Paladin  |  Priest", font(10, False), (210, 196, 166))
    save(img, "STATE_B2_ClassOnly_Preview.png")


def state_advanced_locked():
    img = base()
    img = lock_node(img, "fallen_angel", "Unlocks at Lv 24")
    img = lock_node(img, "ray_of_hope", "Unlocks at Lv 32")
    img = lock_node(img, "electric_smite", "Ultimate Quest · Lv 36", padlock_size=28)
    # matching hotbar: locked skills show as greyed icons with a small padlock
    for cx in (459, 574):
        box = (cx - 24, SLOT_TOP, cx + 25, SLOT_BOTTOM)
        reg = np.asarray(img.crop(box).convert("RGB"), dtype=np.float32)
        g = reg.mean(axis=2, keepdims=True).repeat(3, axis=2) * 0.5
        img.paste(Image.fromarray(g.astype(np.uint8)).convert("RGBA"), box[:2])
        img.alpha_composite(padlock(18), (cx + 6, SLOT_BOTTOM - 18))
    hotkey_labels(img, [(cx, str(i + 1), (237, 214, 158)) for i, cx in enumerate(SLOT_CENTERS)] + [(691, "M4 + R", (237, 214, 158))])
    save(img, "STATE_C_Paladin_Lv20_LockedSkills.png")


if __name__ == "__main__":
    state_no_class()
    state_class_only_sealed()
    state_class_only_preview()
    state_advanced_locked()
    print("state previews written")
