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
HOTBAR_CENTERS = (229, 287, 344, 402, 459, 517, 574)  # numbered slot centers (reference px)
SLOT_Y0, SLOT_Y1 = 551, 605                            # slot frame top/bottom incl. shadow
# Default hotbar art order (v0.15.2 approved): skill id per numbered slot.
HOTBAR_DEFAULT = ("lightning_zap", "goddess_relic", "judgement_hammer", "shield_charge",
                  "ray_of_hope", "holy_wave", "electric_smite")
# Skills without hotbar art: (tree icon interior box, hotbar slot whose cyan frame they reuse)
TREE_ONLY_ICONS = {
    "righteous_strike": ((176, 299, 228, 346), 5),
    "fallen_angel": ((695, 167, 747, 214), 3),
}


# v0.15.1/v0.15.2: normal (interchangeable) skills are Cyan, Class and AC alike.
# Signature skills keep the navy/blue frame; Ascended stays magenta.
# Boxes in reference px: tree nodes + their numbered hotbar slots.
CYAN_RECOLOR_BOXES = (
    (544, 154, 621, 235),   # Shield Charge (tree)
    (682, 154, 760, 235),   # Fallen Angel (tree)
    (376, 546, 428, 600),   # Shield Charge (hotbar slot 4)
    (166, 286, 238, 353),   # Righteous Strike (tree) - Class normal = interchangeable = Cyan
    (166, 410, 238, 478),   # Holy Wave (tree)
    (491, 546, 543, 600),   # Holy Wave (hotbar slot 6)
)
CYAN_HUE = 186.0


def recolor_blue_to_cyan(img, box):
    """Shift only blue pixels (hue 190-250) toward cyan; gold, white and dark pixels untouched."""
    x0, y0, x1, y1 = box
    region = np.asarray(img.crop(box).convert("RGB"), dtype=np.float32) / 255.0
    r, g, b = region[..., 0], region[..., 1], region[..., 2]
    mx = region.max(axis=2)
    mn = region.min(axis=2)
    delta = mx - mn + 1e-6
    hue = np.where(mx == r, (g - b) / delta % 6, np.where(mx == g, (b - r) / delta + 2, (r - g) / delta + 4)) * 60.0
    sat = np.where(mx > 0, delta / (mx + 1e-6), 0)
    val = mx
    blue = (hue >= 190) & (hue <= 250) & (sat > 0.18)
    # Compress the blue band around the cyan target, slightly brighter/saturated so it reads as cyan.
    new_h = CYAN_HUE + (hue - 218.0) * 0.35
    new_s = np.clip(sat * 1.08, 0, 1)
    new_v = np.clip(val * 1.12, 0, 1)
    weight = np.clip((sat - 0.18) / 0.15, 0, 1) * blue
    hh = new_h / 60.0
    i = np.floor(hh).astype(int) % 6
    f = hh - np.floor(hh)
    p_ = new_v * (1 - new_s)
    q_ = new_v * (1 - new_s * f)
    t_ = new_v * (1 - new_s * (1 - f))
    choices_r = [new_v, q_, p_, p_, t_, new_v]
    choices_g = [t_, new_v, new_v, q_, p_, p_]
    choices_b = [p_, p_, t_, new_v, new_v, q_]
    nr = np.choose(i, choices_r)
    ng = np.choose(i, choices_g)
    nb = np.choose(i, choices_b)
    out = region.copy()
    for c, n in ((0, nr), (1, ng), (2, nb)):
        out[..., c] = region[..., c] * (1 - weight) + n * weight
    patch = Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8))
    if img.mode == "RGBA":
        patch = patch.convert("RGBA")
    img.paste(patch, (x0, y0))
    return img


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

    # v0.15.0: hotkey labels are drawn (and rebindable) in code, so erase the baked ones.
    for cx in HOTBAR_CENTERS:
        out = erase_box(out, (cx - 9, 607, cx + 9, 622))
    out = erase_box(out, (668, 607, 716, 622))

    for box in CYAN_RECOLOR_BOXES:
        out = recolor_blue_to_cyan(out, box)

    # v0.16.0: the numbered hotbar is dynamic (drag & drop). Export every skill's slot icon,
    # then clear the slot row so the code draws icons / empty sockets on a clean bar.
    export_slot_icons(out)
    out = erase_rows(out, (200, SLOT_Y0 - 4, 602, SLOT_Y1 + 2))

    slot = grace_slot(ref)
    gx0 = GRACE_CX - slot.width // 2
    gy0 = GRACE_BOTTOM - slot.height
    out = add_glow(out, (GRACE_CX, gy0 + slot.height // 2), 44, (255, 200, 90), 105)
    out.alpha_composite(slot, (gx0, gy0))
    return out.convert("RGBA")


def slot_box(cx):
    return (cx - 24, SLOT_Y0, cx + 25, SLOT_Y1)


def export_slot_icons(img):
    rgba = img.convert("RGBA")
    for cx, skill in zip(HOTBAR_CENTERS, HOTBAR_DEFAULT):
        rgba.crop(slot_box(cx)).save(os.path.join(OUT, "Icon_" + skill + ".png"))
    for skill, (interior, frame_slot) in TREE_ONLY_ICONS.items():
        frame = rgba.crop(slot_box(HOTBAR_CENTERS[frame_slot])).copy()
        inset = (7, 8, 7, 8)
        iw = frame.width - inset[0] - inset[2]
        ih = frame.height - inset[1] - inset[3]
        icon = rgba.crop(interior).resize((iw, ih), Image.LANCZOS)
        frame.paste(icon, (inset[0], inset[1]))
        frame.save(os.path.join(OUT, "Icon_" + skill + ".png"))


def erase_rows(img, box, grain=1.6):
    """Clear a strip by interpolating each column between the rows just above and below it."""
    x0, y0, x1, y1 = box
    a = np.asarray(img.convert("RGB"), dtype=np.float32).copy()
    top = a[y0 - 2:y0, x0:x1].mean(axis=0)
    bot = a[y1:y1 + 2, x0:x1].mean(axis=0)
    # Smooth along x so single bright/dark pixels in the edge rows don't become vertical streaks.
    k = np.exp(-0.5 * (np.arange(-24, 25) / 9.0) ** 2)
    k /= k.sum()
    for arr in (top, bot):
        for c in range(3):
            padded = np.pad(arr[:, c], 24, mode="edge")
            arr[:, c] = np.convolve(padded, k, mode="valid")
    t = (np.arange(y1 - y0, dtype=np.float32) + 0.5) / (y1 - y0)
    fill = top[None, :, :] * (1 - t[:, None, None]) + bot[None, :, :] * t[:, None, None]
    fill += np.random.default_rng(11).normal(0, grain, fill.shape[:2])[:, :, None]
    a[y0:y1, x0:x1] = fill.clip(0, 255)
    return Image.fromarray(a.astype(np.uint8)).convert("RGBA")


def empty_socket():
    """Empty numbered slot: the gold slot frame with a dark recessed interior (stored at 2x)."""
    src = Image.open(os.path.join(SRC, "Cleric_Paladin_Reference_v0.14.png")).convert("RGBA")
    w, h = 49 * 2, (SLOT_Y1 - SLOT_Y0) * 2
    frame = src.crop((380, 408, 453, 477)).resize((w, h), Image.LANCZOS)
    d = ImageDraw.Draw(frame)
    inset = 15
    for y in range(inset, h - inset):
        t = (y - inset) / float(h - 2 * inset)
        d.line((inset, y, w - inset, y), fill=(int(14 + 8 * t), int(20 + 9 * t), int(34 + 12 * t), 255))
    cx, cy, k = w // 2, h // 2, 6
    d.polygon([(cx, cy - k), (cx + k, cy), (cx, cy + k), (cx - k, cy)], fill=(150, 118, 60, 255))
    return frame


def permanent_badge(size=48, scale=4):
    """The tree's 'permanent on the hotbar' badge (dark diamond, gold rim, gold star), redrawn crisp."""
    S = size * scale
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = S / 2.0

    def diamond(r):
        return [(c, c - r), (c + r, c), (c, c + r), (c - r, c)]

    d.polygon(diamond(S * 0.49), fill=(70, 46, 16, 255))          # dark outer edge
    d.polygon(diamond(S * 0.45), fill=(226, 182, 92, 255))        # gold rim
    d.polygon(diamond(S * 0.37), fill=(118, 86, 38, 255))         # inner bevel
    d.polygon(diamond(S * 0.33), fill=(34, 24, 14, 255))          # dark face
    glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((c - S * 0.16, c - S * 0.16, c + S * 0.16, c + S * 0.16), fill=(255, 200, 90, 120))
    img = Image.alpha_composite(img, glow.filter(ImageFilter.GaussianBlur(S * 0.05)))
    d = ImageDraw.Draw(img)
    a, b = S * 0.15, S * 0.04                                      # 4-point star
    d.polygon([(c, c - a), (c + b, c - b), (c + a, c), (c + b, c + b), (c, c + a), (c - b, c + b), (c - a, c), (c - b, c - b)],
              fill=(255, 220, 130, 255))
    return img.resize((size, size), Image.LANCZOS)


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


def selection_ring(size=96, scale=4):
    """Soft gold ring drawn around the selected node's icon frame."""
    S = size * scale
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pad = int(S * 0.08)
    d.rounded_rectangle((pad, pad, S - pad, S - pad), radius=int(S * 0.1),
                        outline=(255, 214, 110, 235), width=int(S * 0.035))
    img = img.filter(ImageFilter.GaussianBlur(S * 0.02))
    return img.resize((size, size), Image.LANCZOS)


def class_skill_variants(backdrop):
    """v0.18.0: Righteous Strike is Paladin's Ascended Class skill (Magenta + permanent badge) and
    Lightning Zap is a normal (Cyan) skill. Before Advancement every Class skill is Cyan with no badge.
    The baked "hover for ..." header lines are cleared: the code draws the live Tier Points there."""
    import render_states as rs
    base = rs.recolor_hue(backdrop.copy(), (148, 142, 253, 240), 280, 345, 186)
    base = rs.recolor_hue(base, (154, 146, 248, 238), 345, 360, 186)
    base = rs.recolor_hue(base, (154, 146, 248, 238), 0, 20, 186)
    base = rs.remove_lz_badge(base)
    base = fade_pink_haze(base, (150, 136, 256, 244))
    base = rs.inpaint_h(base, rs.CLERIC_SUB)
    base = rs.inpaint_h(base, rs.PALADIN_SUB)
    pre = base.copy()
    advanced = rs.recolor_hue(base, (150, 276, 252, 368), 160, 215, 305)
    badge = permanent_badge().resize((23, 23), Image.LANCZOS)
    advanced.alpha_composite(badge, (218, 283))
    return advanced, pre


def fade_pink_haze(img, box):
    """Lightning Zap's old Magenta glow leaves a faint pink haze on the parchment; neutralize it."""
    import render_states as rs
    region = np.asarray(img.crop(box).convert("RGB"), dtype=np.float32) / 255.0
    hue, sat, mx = rs.hsv_arrays(region)
    pink = ((hue >= 290) | (hue <= 14)) & (sat < 0.32)
    gray = region.mean(axis=2, keepdims=True)
    warm = gray * np.array([1.03, 1.0, 0.95])
    out = np.where(pink[..., None], warm, region)
    img.paste(Image.fromarray((out.clip(0, 1) * 255).astype(np.uint8)).convert("RGBA"), box[:2])
    return img


def recolor_icon(name, out_name, lo, hi, target):
    import render_states as rs
    icon = Image.open(os.path.join(OUT, name)).convert("RGBA")
    alpha = icon.getchannel("A")
    icon = rs.recolor_hue(icon, (0, 0, icon.width, icon.height), lo, hi, target)
    icon.putalpha(alpha)
    icon.save(os.path.join(OUT, out_name))


def tier_assets():
    import render_tiers as rt
    import render_states as rs
    rt.star(48, "full").save(os.path.join(OUT, "Tier_Star_Full.png"))
    rt.star(48, "pending").save(os.path.join(OUT, "Tier_Star_Pending.png"))
    rt.star(48, "empty").save(os.path.join(OUT, "Tier_Star_Empty.png"))
    rs.padlock(48).save(os.path.join(OUT, "Lock_Padlock.png"))


def main():
    backdrop = build_backdrop()
    advanced, pre = class_skill_variants(backdrop)
    advanced.save(os.path.join(OUT, "Cleric_Paladin_Reference.png"))
    pre.save(os.path.join(OUT, "Cleric_Paladin_PreAdvance.png"))
    # Hotbar icons: Lightning Zap Cyan; Righteous Strike Magenta (Ascended) + Cyan (before Advancement).
    recolor_icon("Icon_righteous_strike.png", "Icon_righteous_strike_Normal.png", 0, 1, 0)
    recolor_icon("Icon_righteous_strike.png", "Icon_righteous_strike.png", 160, 215, 305)
    recolor_icon("Icon_lightning_zap.png", "Icon_lightning_zap.png", 280, 345, 186)
    tier_assets()
    # v0.18.1: Grace slot icon for the in-game hotbar HUD (cut from the footer Grace box).
    advanced.crop((662, 542, 721, 599)).save(os.path.join(OUT, "Icon_heavens_light.png"))
    rounded_button(16, "+").save(os.path.join(OUT, "Tier_Plus.png"))
    rounded_button(16, "-").save(os.path.join(OUT, "Tier_Minus.png"))
    confirm_plaque(160, 42).save(os.path.join(OUT, "Confirm_Plaque.png"))
    empty_socket().save(os.path.join(OUT, "Slot_Empty.png"))
    permanent_badge().save(os.path.join(OUT, "Badge_Permanent.png"))
    print("UI assets written to", OUT)


if __name__ == "__main__":
    main()
