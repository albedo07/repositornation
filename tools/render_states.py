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


# ---------------------------------------------------------------- integrated panel neutralizing
CLASS_BODY = (33, 116, 309, 520)
AC_BODY = (338, 116, 976, 520)
CLERIC_EMBLEM = (79, 112, 37)          # cx, cy, r  (generic compass emblem, kept)
CLERIC_TITLE = (140, 86, 268, 118)     # baked "CLERIC"
CLERIC_SUB = (150, 122, 258, 140)      # baked "hover for Blessing" plaque text
PALADIN_TITLE = (596, 86, 724, 118)    # baked "PALADIN"
PALADIN_SUB = (588, 122, 762, 140)
PALADIN_CREST = (512, 66, 592, 150)    # winged cross (Paladin identity)


def hsv_arrays(region):
    r, g, b = region[..., 0], region[..., 1], region[..., 2]
    mx, mn = region.max(axis=2), region.min(axis=2)
    dl = mx - mn + 1e-6
    hue = np.where(mx == r, (g - b) / dl % 6, np.where(mx == g, (b - r) / dl + 2, (r - g) / dl + 4)) * 60.0
    sat = np.where(mx > 0, dl / (mx + 1e-6), 0)
    return hue, sat, mx


def inpaint_h(img, box, grain=1.5, seed=5):
    """Fill a box by interpolating between the columns just left and right of it."""
    x0, y0, x1, y1 = box
    a = np.asarray(img.convert("RGB"), dtype=np.float32).copy()
    left = a[y0:y1, x0 - 3:x0].mean(axis=1)
    right = a[y0:y1, x1:x1 + 3].mean(axis=1)
    t = (np.arange(x1 - x0, dtype=np.float32) + 0.5) / (x1 - x0)
    fill = left[:, None, :] * (1 - t[None, :, None]) + right[:, None, :] * t[None, :, None]
    fill += np.random.default_rng(seed).normal(0, grain, fill.shape[:2])[:, :, None]
    a[y0:y1, x0:x1] = fill.clip(0, 255)
    out = Image.fromarray(a.astype(np.uint8)).convert("RGBA")
    return out


def neutral_body(img, body, texture, keep_until_y=130, keep_boxes=(), corner=34, emblem=None):
    """Replace a panel's scene with `texture`, inside the panel's own inner edge.
    Keeps: header-band pixels in the top strip (saturated band colors + gold), gold ornaments
    near the edges/corners, and an optional emblem circle."""
    x0, y0, x1, y1 = body
    w, h = x1 - x0, y1 - y0
    orig = np.asarray(img.convert("RGB").crop(body), dtype=np.float32) / 255.0
    hue, sat, val = hsv_arrays(orig)
    yy, xx = np.mgrid[0:h, 0:w]

    fill = Image.new("L", (w, h), 0)
    ImageDraw.Draw(fill).rounded_rectangle((0, -30, w - 1, h - 1), radius=15, fill=255)
    fill = np.asarray(fill, dtype=np.float32) / 255.0

    gold = (hue >= 28) & (hue <= 62) & (sat > 0.38) & (val > 0.42)
    band = (sat > 0.30) & (((hue >= 195) & (hue <= 250)) | (hue >= 320) | (hue <= 12))
    top = (yy + y0) < keep_until_y
    for bx0, by0, bx1, by1 in keep_boxes:
        top |= (xx + x0 >= bx0) & (xx + x0 < bx1) & (yy + y0 >= by0) & (yy + y0 < by1)
    # Gold ornaments only overlap the body in the bottom corners.
    corners = (yy > h - 1 - corner) & ((xx < corner) | (xx > w - 1 - corner))
    keep = (top & (band | gold)) | (corners & gold)
    if emblem:
        ex, ey, er = emblem
        keep |= ((xx + x0 - ex) ** 2 + (yy + y0 - ey) ** 2) < er * er
    keep_img = Image.fromarray((keep * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(1.0))
    keep = np.asarray(keep_img, dtype=np.float32) / 255.0
    alpha = np.asarray(Image.fromarray((fill * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2)),
                       dtype=np.float32) / 255.0 * (1 - keep)
    tex = np.asarray(texture.convert("RGB").resize((w, h)), dtype=np.float32) / 255.0
    # Soft inner shadow along the panel edge so the new surface sits *inside* the frame.
    dist = np.minimum.reduce([xx, w - 1 - xx, h - 1 - yy]).astype(np.float32)
    shade = 1.0 - 0.18 * np.exp(-dist / 6.0)
    tex = tex * shade[..., None]
    out = orig * (1 - alpha[..., None]) + tex * alpha[..., None]
    img.paste(Image.fromarray((out.clip(0, 1) * 255).astype(np.uint8)).convert("RGBA"), body[:2])
    return img


def recolor_rose_to_navy(img, box):
    x0, y0, x1, y1 = box
    reg = np.asarray(img.convert("RGB").crop(box), dtype=np.float32) / 255.0
    hue, sat, val = hsv_arrays(reg)
    rose = ((hue >= 315) | (hue <= 15)) & (sat > 0.15)
    wgt = (np.clip((sat - 0.15) / 0.15, 0, 1) * rose)[..., None]
    lum = reg.mean(axis=2, keepdims=True)
    navy = np.array([0.16, 0.25, 0.45]) * (0.55 + lum * 0.9)
    out = reg * (1 - wgt) + navy * wgt
    img.paste(Image.fromarray((out.clip(0, 1) * 255).astype(np.uint8)).convert("RGBA"), box[:2])
    return img


def compass_emblem(size):
    src = Image.open(os.path.join(ROOT, "docs", "source_art", "Cleric_Paladin_Reference_v0.14.png")).convert("RGBA")
    ex, ey, er = CLERIC_EMBLEM
    em = src.crop((ex - er, ey - er, ex + er, ey + er)).resize((size, size), Image.LANCZOS)
    m = Image.new("L", (size * 4, size * 4), 0)
    ImageDraw.Draw(m).ellipse((4, 4, size * 4 - 4, size * 4 - 4), fill=255)
    em.putalpha(m.resize((size, size), Image.LANCZOS))
    return em


def header_title(img, cx, cy, text, size):
    d = ImageDraw.Draw(img)
    f = font(size)
    w = d.textlength(text, font=f)
    for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1), (1, 1)):
        d.text((cx - w / 2 + dx, cy - size * 0.62 + dy), text, font=f, fill=(40, 26, 14))
    d.text((cx - w / 2, cy - size * 0.62), text, font=f, fill=(246, 236, 212))


def neutral_class_panel(img, sub):
    img = inpaint_h(img, CLERIC_TITLE)
    img = inpaint_h(img, CLERIC_SUB, grain=0.8)
    img = neutral_body(img, CLASS_BODY, neutral_parchment((CLASS_BODY[2] - CLASS_BODY[0], CLASS_BODY[3] - CLASS_BODY[1]), seed=11),
                       keep_until_y=124, emblem=CLERIC_EMBLEM)
    header_title(img, 203, 102, "CLASS", 22)
    d = ImageDraw.Draw(img)
    text_c(d, 203, 130, sub, font(11, False), (96, 70, 44))
    return img


def neutral_ac_panel(img, texture, sub):
    img = inpaint_h(img, PALADIN_TITLE)
    img = inpaint_h(img, PALADIN_SUB, grain=0.8)
    img = inpaint_h(img, PALADIN_CREST, grain=1.0)
    img = recolor_rose_to_navy(img, (338, 60, 976, 131))
    img = recolor_rose_to_navy(img, (478, 125, 524, 150))
    img = neutral_body(img, AC_BODY, texture, keep_until_y=129, keep_boxes=((478, 125, 524, 150),))
    em = compass_emblem(66)
    img.alpha_composite(em, (552 - 33, 108 - 33))
    header_title(img, 660, 102, "ADVANCEMENT", 17)
    d = ImageDraw.Draw(img)
    text_c(d, 660, 134, sub, font(11, False), (96, 70, 44) if texture_is_light(texture) else (214, 200, 170))
    return img


def texture_is_light(texture):
    return np.asarray(texture.convert("L")).mean() > 120


def navy_texture(size, seed):
    t = neutral_parchment(size, seed=seed)
    a = np.asarray(t.convert("RGB"), dtype=np.float32) / 233.0
    navy = np.array([24, 36, 62], dtype=np.float32)
    return Image.fromarray((a * navy[None, None, :] * 1.15).clip(0, 255).astype(np.uint8)).convert("RGBA")


def remove_lz_badge(img):
    """Pre-Advancement nothing is permanent: replace Lightning Zap's corner badge with the plain
    frame corner taken from Righteous Strike (same frame style, scaled to LZ's frame)."""
    src = img.copy()
    rs_corner = src.crop((205, 279, 246, 316))          # RS top-right corner (frame + background)
    rs_corner = rs_corner.resize((43, 39), Image.LANCZOS)
    m = Image.new("L", rs_corner.size, 0)
    ImageDraw.Draw(m).rounded_rectangle((2, 2, rs_corner.width - 3, rs_corner.height - 3), radius=6, fill=255)
    img.paste(rs_corner, (202, 146), m.filter(ImageFilter.GaussianBlur(1.5)))
    return img


DEFAULT_HOTBAR = ("lightning_zap", "goddess_relic", "judgement_hammer", "shield_charge",
                  "ray_of_hope", "holy_wave", "electric_smite")


def draw_hotbar(img, layout):
    """Draw the dynamic hotbar exactly like the game: icon per slot, empty socket otherwise."""
    sock = Image.open(os.path.join(A, "Slot_Empty.png")).convert("RGBA").resize((49, 54), Image.LANCZOS)
    for cx, skill in zip(SLOT_CENTERS, layout):
        tex = sock if not skill else Image.open(os.path.join(A, "Icon_" + skill + ".png")).convert("RGBA")
        img.alpha_composite(tex.resize((49, 54), Image.LANCZOS), (cx - 24, 551))
    return img


def base():
    img = Image.open(os.path.join(A, "Cleric_Paladin_Reference.png")).convert("RGBA")
    return draw_hotbar(img, DEFAULT_HOTBAR)


def save(img, name):
    img.convert("RGB").resize((1180, 772), Image.LANCZOS).save(os.path.join(OUT, name))


# ---------------------------------------------------------------- states
def state_no_class():
    img = base()
    img = neutral_class_panel(img, "no Class chosen")
    img = neutral_ac_panel(img, neutral_parchment((AC_BODY[2] - AC_BODY[0], AC_BODY[3] - AC_BODY[1]), seed=7), "locked")
    pl = plaque(560, 96)
    img.alpha_composite(pl, (505 - 280, 318 - 48))
    d = ImageDraw.Draw(img)
    text_c(d, 505, 290, "CHOOSE A CLASS AT THE ALTAR FIRST", font(18), GOLD, shadow=(0, 0, 0))
    text_c(d, 505, 318, "Visit the Dragon's Altar to begin your path.", font(12, False), (226, 214, 186))
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
    # Before Advancement every Class skill is interchangeable -> Cyan, no permanent badge.
    img = recolor_hue(img, NODES["lightning_zap"][0], 280, 345, 186)
    img = remove_lz_badge(img)
    for cx in SLOT_CENTERS[3:]:
        empty_socket(img, cx, locked=True)
    # Slots 1-3: the three Class skills, all cut the same way from their tree nodes.
    src = img.copy()
    crops = ((164, 156, 236, 229), (167, 289, 236, 352), (167, 414, 236, 477))
    for cx, box in zip(SLOT_CENTERS[:3], crops):
        icon = src.crop(box).resize((49, 49), Image.LANCZOS)
        img.alpha_composite(socket_art(), (cx - 27, SLOT_TOP - 3))
        img.paste(icon, (cx - 24, SLOT_TOP))
    erase_grace(img, locked=True)
    hotkey_labels(img, [(cx, str(i + 1), (237, 214, 158) if i < 3 else (140, 124, 96)) for i, cx in enumerate(SLOT_CENTERS)]
                  + [(691, "M4 + R", (140, 124, 96))])
    return img


def state_class_only_sealed():
    img = class_only_common()
    img = neutral_ac_panel(img, navy_texture((AC_BODY[2] - AC_BODY[0], AC_BODY[3] - AC_BODY[1]), 7), "sealed until you Advance")
    d = ImageDraw.Draw(img)
    # Padlock + requirement checklist, centered as one group in the panel body (y 152..520).
    img.alpha_composite(padlock(78), (657 - 39, 202))
    reqs = [("Reach Level 16", True), ("Spend all 14 Class Tier Points", False),
            ("Max a Class skill to Tier 7", False), ("Complete the Advancement Quest", False)]
    y = 304
    for text, done in reqs:
        mark = "\u25c6" if done else "\u25c7"
        col = (236, 206, 130) if done else (206, 194, 168)
        text_c(d, 657, y, mark + "  " + text, font(14, False), col, shadow=(0, 0, 0))
        y += 30
    save(img, "STATE_B1_ClassOnly_Sealed.png")


def state_class_only_blank():
    img = class_only_common()
    img = neutral_ac_panel(img, neutral_parchment((AC_BODY[2] - AC_BODY[0], AC_BODY[3] - AC_BODY[1]), seed=7), "locked")
    pl = plaque(420, 84)
    img.alpha_composite(pl, (657 - 210, 318 - 42))
    d = ImageDraw.Draw(img)
    text_c(d, 657, 294, "ADVANCE AT LV 16", font(20), GOLD, shadow=(0, 0, 0))
    text_c(d, 657, 322, "Complete the Advancement Quest to choose your path.", font(11, False), (226, 214, 186))
    save(img, "STATE_B2_ClassOnly_Blank.png")


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
    state_class_only_blank()
    state_advanced_locked()
    print("state previews written")
