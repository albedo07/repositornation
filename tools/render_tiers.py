"""Ideation previews for Tier display + Tier Points (Paladin Lv40 example).

Scene: Class tree locked after Advancement (Lightning Zap 4/7, Righteous Strike 7/7 Ascended, Holy Wave 3/7).
AC: Goddess Relic 5/5, Judgement Hammer 2/5 (+1 pending, selected), Shield Charge 2/5, Fallen Angel 1/5,
Ray of Hope 0/5, Electric Smite 1/3 (auto). 12 AC points earned, 10 spent, 1 pending -> 1 left.
Usage: python3 tools/render_tiers.py -> docs/previews/TIER_*.png
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter

import render_states as rs
from render_states import NODES, SLOT_CENTERS, font, text_c

ROOT = rs.ROOT
A = rs.A
OUT = rs.OUT

TIERS = {  # skill: (confirmed, pending, max)
    "lightning_zap": (4, 0, 7),
    "righteous_strike": (7, 0, 7),
    "holy_wave": (3, 0, 7),
    "goddess_relic": (5, 0, 5),
    "judgement_hammer": (2, 1, 5),
    "shield_charge": (2, 0, 5),
    "fallen_angel": (1, 0, 5),
    "ray_of_hope": (0, 0, 5),
    "electric_smite": (1, 0, 3),
}
SELECTED = "judgement_hammer"
HOTBAR = ("righteous_strike", "goddess_relic", "judgement_hammer", "shield_charge", "fallen_angel", "ray_of_hope",
          "electric_smite")
PERMANENT = ("righteous_strike", "goddess_relic", "judgement_hammer", "electric_smite")


# ---------------------------------------------------------------- stars
def star(size, state, scale=6):
    S = size * scale
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = S / 2.0

    def pts(r_out, r_in):
        p = []
        for i in range(10):
            r = r_out if i % 2 == 0 else r_in
            a = -math.pi / 2 + i * math.pi / 5
            p.append((c + r * math.cos(a), c + 0.04 * S + r * math.sin(a)))
        return p

    outer, inner = S * 0.50, S * 0.21
    if state == "pending":
        glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        ImageDraw.Draw(glow).polygon(pts(outer, inner), fill=(120, 230, 255, 200))
        im.alpha_composite(glow.filter(ImageFilter.GaussianBlur(S * 0.07)))
        d = ImageDraw.Draw(im)
    if state == "empty":
        d.polygon(pts(outer, inner), fill=(96, 70, 34, 255))
        d.polygon(pts(outer - scale * 1.3, inner - scale * 0.6), fill=(44, 34, 24, 235))
    elif state == "pending":
        d.polygon(pts(outer, inner), fill=(40, 96, 120, 255))
        d.polygon(pts(outer - scale * 1.3, inner - scale * 0.6), fill=(168, 238, 255, 255))
    else:
        d.polygon(pts(outer, inner), fill=(110, 72, 20, 255))
        d.polygon(pts(outer - scale * 1.3, inner - scale * 0.6), fill=(246, 198, 86, 255))
        d.polygon(pts(outer * 0.28, inner * 0.28), fill=(255, 232, 160, 255))
    return im.resize((size, size), Image.LANCZOS)


def star_row(img, cx, y, skill, size, gap=2):
    done, pend, mx = TIERS[skill]
    total = mx * size + (mx - 1) * gap
    x = int(round(cx - total / 2.0))
    for i in range(mx):
        st = "full" if i < done else ("pending" if i < done + pend else "empty")
        img.alpha_composite(star(size, st), (x + i * (size + gap), y))
    return x, x + total


def star_size(skill):
    return 19 if skill == "electric_smite" else (13 if TIERS[skill][2] == 7 else 16)


def fraction_text(skill):
    done, pend, mx = TIERS[skill]
    return "%d/%d" % (done + pend, mx)


# ---------------------------------------------------------------- scene
def class_recolor(img):
    """Fix the art for this scene: Lightning Zap becomes a normal (Cyan) skill and Righteous Strike
    becomes Paladin's Ascended MC skill (Magenta + permanent badge)."""
    badge = Image.open(os.path.join(A, "Badge_Permanent.png")).convert("RGBA").resize((23, 23), Image.LANCZOS)
    img = rs.recolor_hue(img, NODES["lightning_zap"][0], 280, 345, 186)
    img = rs.remove_lz_badge(img)
    img = rs.recolor_hue(img, NODES["righteous_strike"][0], 160, 215, 305)
    img.alpha_composite(badge, (218, 283))
    return img


def hotbar(img):  # returns img
    for cx, skill in zip(SLOT_CENTERS, HOTBAR):
        tex = Image.open(os.path.join(A, "Icon_" + skill + ".png")).convert("RGBA").resize((49, 54), Image.LANCZOS)
        img.alpha_composite(tex, (cx - 24, 551))
        if skill in PERMANENT:
            b = Image.open(os.path.join(A, "Badge_Permanent.png")).convert("RGBA").resize((19, 19), Image.LANCZOS)
            img.alpha_composite(b, (cx + 14, 547))
    # Ascended Righteous Strike icon in Magenta like its node.
    img = rs.recolor_hue(img, (SLOT_CENTERS[0] - 24, 551, SLOT_CENTERS[0] + 13, 605), 160, 215, 305)
    rs.hotkey_labels(img, [(cx, str(i + 1), (237, 214, 158)) for i, cx in enumerate(SLOT_CENTERS)]
                     + [(691, "M4 + R", (237, 214, 158))])
    return img


def header_points(img):
    """Replace the 'hover for ...' line with the Tier Points counter (hover the title still shows Blessing/Mastery)."""
    img = rs.inpaint_h(img, rs.CLERIC_SUB)
    img = rs.inpaint_h(img, rs.PALADIN_SUB)
    d = ImageDraw.Draw(img)
    text_c(d, 204, 123, "TIER POINTS  0  ·  LOCKED", font(10), (96, 84, 70))
    f = font(11)
    label, num = "TIER POINTS  ", "1"
    w = d.textlength(label + num, font=f)
    x = 675 - w / 2
    d.text((x, 123), label, font=f, fill=(92, 52, 40))
    d.text((x + d.textlength(label, font=f), 123), num, font=f, fill=(176, 40, 30))
    return img


def plus_minus(img, left_x, right_x, cy, size=16):
    plus = Image.open(os.path.join(A, "Tier_Plus.png")).convert("RGBA").resize((size, size), Image.LANCZOS)
    minus = Image.open(os.path.join(A, "Tier_Minus.png")).convert("RGBA").resize((size, size), Image.LANCZOS)
    img.alpha_composite(minus, (int(left_x) - size - 4, int(cy - size / 2)))
    img.alpha_composite(plus, (int(right_x) + 4, int(cy - size / 2)))


def confirm(img):
    plaque = Image.open(os.path.join(A, "Confirm_Plaque.png")).convert("RGBA").resize((137, 36), Image.LANCZOS)
    px, py = 872 - 68, 578 - 18
    img.alpha_composite(plaque, (px, py))
    d = ImageDraw.Draw(img)
    text_c(d, px + 68, py + 5, "CONFIRM", font(13), (250, 214, 120), shadow=(0, 0, 0))
    text_c(d, px + 68, py + 22, "1 PENDING", font(8), (236, 220, 180), shadow=(0, 0, 0))


def tooltip(img, x, y):
    lines = [("ATTACK - JUDGEMENT HAMMER", font(11), (250, 214, 120)),
             ("Tier 2/5  →  3/5 (pending)", font(10, False), (168, 238, 255)),
             ("Damage +20%  →  +30%", font(10, False), (226, 214, 186)),
             ("Each Tier: +10% damage and healing", font(9, False), (160, 150, 130))]
    w, h = 236, 78
    box = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(box)
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=6, fill=(18, 16, 14, 236), outline=(176, 136, 66, 255), width=1)
    ty = 7
    for text, f, col in lines:
        d.text((10, ty), text, font=f, fill=col)
        ty += 17
    img.alpha_composite(box, (x, y))


def base_scene():
    img = Image.open(os.path.join(A, "Cleric_Paladin_Reference.png")).convert("RGBA")
    img = class_recolor(img)
    img = hotbar(img)
    img = header_points(img)
    confirm(img)
    return img


# ---------------------------------------------------------------- options
def option_stars(with_fraction):
    img = base_scene()
    d = ImageDraw.Draw(img)
    for skill in TIERS:
        box, (cx, pb) = NODES[skill]
        size = star_size(skill)
        x0, x1 = star_row(img, cx, pb + 3, skill, size)
        right = x1
        if with_fraction:
            f = font(9)
            t = fraction_text(skill)
            col = (40, 120, 150) if TIERS[skill][1] else (96, 66, 30)
            d.text((x1 + 4, pb + 3 + (size - 11) / 2.0), t, font=f, fill=col)
            right = x1 + 4 + d.textlength(t, font=f)
        if skill == SELECTED:
            plus_minus(img, x0, right, pb + 3 + size / 2.0)
    tooltip(img, 470, 404)
    name = "TIER_C_StarsAndCount.png" if with_fraction else "TIER_A_Stars.png"
    rs.save(img, name)


def option_fraction():
    img = base_scene()
    d = ImageDraw.Draw(img)
    for skill in TIERS:
        box, (cx, pb) = NODES[skill]
        t = fraction_text(skill)
        f = font(10)
        w = int(d.textlength(t, font=f)) + 10
        x, y = box[0] - 3, box[1] - 3
        pend = TIERS[skill][1] > 0
        d.rounded_rectangle((x, y, x + w, y + 15), radius=4, fill=(20, 18, 16, 235),
                            outline=(120, 220, 245, 255) if pend else (176, 136, 66, 255))
        col = (168, 238, 255) if pend else ((250, 214, 120) if TIERS[skill][0] == TIERS[skill][2] else (226, 214, 186))
        d.text((x + 5, y + 1), t, font=f, fill=col)
        if skill == SELECTED:
            plus_minus(img, cx - 2, cx + 2, pb + 12, size=18)
    tooltip(img, 470, 404)
    rs.save(img, "TIER_B_Count.png")


def legend():
    """Close-up of the star states."""
    W, H = 640, 150
    img = Image.new("RGBA", (W, H), (233, 220, 193, 255))
    d = ImageDraw.Draw(img)
    text_c(d, W // 2, 10, "TIER STARS (Class 7 · Advancement 5 · Ultimate 3)", font(15), rs.INK)
    rows = [("Unlocked, 0 Tiers (usable)", (0, 0, 5)), ("2 Tiers", (2, 0, 5)),
            ("2 Tiers + 1 pending", (2, 1, 5)), ("Maxed", (5, 0, 5))]
    for i, (label, t) in enumerate(rows):
        cx = 85 + i * 157
        TIERS["_demo"] = t
        star_row(img, cx, 52, "_demo", 22, gap=3)
        text_c(d, cx, 92, label, font(11, False), rs.INK)
    del TIERS["_demo"]
    text_c(d, W // 2, 122, "Gold = confirmed   ·   Glowing cyan = pending (not saved until CONFIRM)",
           font(11, False), (100, 78, 52))
    img.convert("RGB").resize((W * 2, H * 2), Image.LANCZOS).save(os.path.join(OUT, "TIER_Legend.png"))


if __name__ == "__main__":
    option_stars(False)
    option_fraction()
    option_stars(True)
    legend()
    print("tier previews written")
