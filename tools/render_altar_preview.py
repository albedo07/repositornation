"""Design preview: the Dragon's Altar Class selection on the universal Immortal Heroes panel.
Same chrome as the Skill Tree: left panel = the three Classes as nodes, right panel = the focused
Class (Blessing, Class skills, Advancements), footer = what goes on your hotbar + CHOOSE plaque.
Mockup only (DejaVu Serif stands in for Averia Serif; class glyphs are placeholders for real icon art).
Usage: python3 tools/render_altar_preview.py -> docs/previews/ALTAR_*.png
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

import render_states as rs

A, OUT = rs.A, rs.OUT
GLYPH = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
INK = (58, 38, 22)
SUBJECT = (128, 78, 18)

CLASSES = {
    "Warrior": {
        "hue": 4, "glyph": "⚔", "node": "lightning_zap",
        "lore": "Steel, grit and an iron will. Warriors stand at the front and refuse to fall.",
        "blessing": "WARRIOR'S BLESSING",
        "lines": [("Hyper Armor", "against hits under 30% of Max HP"), ("Parry", "2x parry bonus"),
                  ("Run / Jump", "+20 / +20")],
        "skills": [("Heavy Slash", None), ("Impact Wave", None), ("Impact Punch", None)],
        "advances": [("SWORD MASTER", "The Way of the Sword"), ("MERCENARY", "Warfreak")],
    },
    "Cleric": {
        "hue": 205, "glyph": "✝", "node": "righteous_strike",
        "lore": "Faith made manifest. Clerics strike with holy lightning and mend the wounds of the faithful.",
        "blessing": "CLERIC'S BLESSING",
        "lines": [("Shields", "1.5x Block Force and Block Armor"), ("Weapons", "Shield + Staff allowed"),
                  ("Movement", "no penalty from Shields, Staves, 1H Clubs"), ("Vitality", "+35 Max HP, +20% HP Regen")],
        "skills": [("Lightning Zap", "Icon_lightning_zap.png"), ("Righteous Strike", "Icon_righteous_strike_Normal.png"),
                   ("Holy Wave", "Icon_holy_wave.png")],
        "advances": [("PALADIN", "Holy Trinity"), ("PRIEST", "Bless Thy Sinners")],
    },
    "Sorcerer": {
        "hue": 275, "glyph": "✦", "node": "holy_wave",
        "lore": "Raw arcane power drawn from Eitr. Sorcerers trade the shield for devastating spells.",
        "blessing": "SORCERER'S BLESSING - WARLOCK",
        "lines": [("Eitr", "+65 Max Eitr, +35% Eitr Regen, half regen delay"), ("Melee", "-70% creature melee damage"),
                  ("Restriction", "cannot Block or Parry")],
        "skills": [("Flame Burst", None), ("Glacial Descent", None), ("Stonefang Eruption", None)],
        "advances": [("WIZARD", "Archmage"), ("SPELLCASTER", "Yin and Yang")],
    },
}
ORDER = ("Warrior", "Cleric", "Sorcerer")


def glyph_font(size):
    return ImageFont.truetype(GLYPH, size)


def class_node(img, node_id, cls, selected):
    """Re-skin a Class panel node: frame tinted to the Class color, interior = emblem glyph, plate = name."""
    box, (cx, pb) = rs.NODES[node_id]
    x0, y0, x1, y1 = box
    img = rs.recolor_hue(img, (x0 - 6, y0 - 6, x1 + 6, y1 + 6), 150, 230, CLASSES[cls]["hue"])
    # interior
    ix0, iy0, ix1, iy1 = x0 + 13, y0 + 14, x1 - 13, y1 - 14
    w, h = ix1 - ix0, iy1 - iy0
    yy, xx = np.mgrid[0:h, 0:w]
    r = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    hue = CLASSES[cls]["hue"]
    base = np.array(Image.new("RGB", (1, 1), "hsv(%d,70%%,55%%)" % hue).getpixel((0, 0)), dtype=np.float32)
    grad = base[None, None, :] * np.clip(1.0 - r * 0.75, 0.18, 1.0)[..., None] * 0.55
    img.paste(Image.fromarray(grad.clip(0, 255).astype(np.uint8)).convert("RGBA"), (ix0, iy0))
    d = ImageDraw.Draw(img)
    f = glyph_font(40)
    g = CLASSES[cls]["glyph"]
    gb = d.textbbox((0, 0), g, font=f)
    gx = (ix0 + ix1) / 2 - (gb[2] + gb[0]) / 2
    gy = (iy0 + iy1) / 2 - (gb[3] + gb[1]) / 2
    d.text((gx + 1, gy + 2), g, font=f, fill=(0, 0, 0, 170))
    d.text((gx, gy), g, font=f, fill=(250, 238, 205))
    # nameplate text
    img = rs.inpaint_h(img, (cx - 46, pb - 21, cx + 46, pb - 5), grain=0.6)
    d = ImageDraw.Draw(img)
    rs.text_c(d, cx, pb - 20, cls, rs.font(13), INK)
    if selected:
        glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(glow).rounded_rectangle((x0 - 7, y0 - 7, x1 + 7, y1 + 7), radius=10, outline=(255, 214, 120, 255), width=3)
        img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(3)))
        img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(1)))
    return img


def recolor_header(img, hue):
    box = (338, 60, 976, 131)
    reg = np.asarray(img.convert("RGB").crop(box), dtype=np.float32) / 255.0
    h, s, v = rs.hsv_arrays(reg)
    rose = ((h >= 315) | (h <= 15)) & (s > 0.15)
    if hue in (0, 4):
        return img
    target = np.array(Image.new("RGB", (1, 1), "hsv(%d,55%%,45%%)" % hue).getpixel((0, 0)), dtype=np.float32) / 255.0
    wgt = (np.clip((s - 0.15) / 0.15, 0, 1) * rose)[..., None]
    lum = reg.mean(axis=2, keepdims=True)
    tinted = target * (0.55 + lum * 0.9)
    out = reg * (1 - wgt) + tinted * wgt
    img.paste(Image.fromarray((out.clip(0, 1) * 255).astype(np.uint8)).convert("RGBA"), box[:2])
    return img


def line(d, x, y, subject, value, size=13):
    fs = rs.font(size)
    fv = rs.font(size, False)
    d.text((x, y), subject, font=fs, fill=SUBJECT)
    w = d.textlength(subject, font=fs)
    d.text((x + w, y), " - " + value, font=fv, fill=INK)


def plaque_text(img, cx, cy, w, h, title, sub):
    pl = rs.plaque(w, h)
    img.alpha_composite(pl, (int(cx - w / 2), int(cy - h / 2)))
    d = ImageDraw.Draw(img)
    rs.text_c(d, cx, cy - h * 0.30, title, rs.font(13), rs.GOLD, shadow=(0, 0, 0))
    rs.text_c(d, cx, cy + h * 0.06, sub, rs.font(10, False), (226, 214, 186))


def render(focus):
    cfg = CLASSES[focus]
    img = Image.open(os.path.join(A, "Cleric_Paladin_PreAdvance.png")).convert("RGBA")
    d = ImageDraw.Draw(img)

    # Title bar subtitle
    img = rs.inpaint_h(img, (118, 43, 372, 61), grain=0.6)
    d = ImageDraw.Draw(img)
    rs.text_c(d, 245, 46, "DRAGON'S  ALTAR   •   CHOOSE YOUR CLASS", rs.font(11), (196, 178, 140))

    # Left panel: the three Classes
    img = rs.inpaint_h(img, rs.CLERIC_TITLE)
    rs.header_title(img, 214, 102, "CLASSES", 22)
    d = ImageDraw.Draw(img)
    rs.text_c(d, 204, 124, "CHOOSE ONE", rs.font(10), (96, 84, 70))
    img = rs.remove_lz_badge(img)
    for cls in ORDER:
        img = class_node(img, CLASSES[cls]["node"], cls, cls == focus)

    # Right panel: focused Class
    img = rs.inpaint_h(img, rs.PALADIN_TITLE)
    img = rs.neutral_body(img, rs.AC_BODY, rs.neutral_parchment((rs.AC_BODY[2] - rs.AC_BODY[0], rs.AC_BODY[3] - rs.AC_BODY[1]), seed=5),
                          keep_until_y=129, keep_boxes=((478, 125, 524, 150),))
    img = recolor_header(img, cfg["hue"] if focus != "Warrior" else 0)
    if focus != "Cleric":
        img = rs.inpaint_h(img, rs.PALADIN_CREST, grain=1.0)
        em = rs.compass_emblem(66)
        img.alpha_composite(em, (552 - 33, 108 - 33))
    rs.header_title(img, 672, 102, focus.upper(), 22)
    d = ImageDraw.Draw(img)
    rs.text_c(d, 675, 124, "BASE CLASS", rs.font(10), (92, 52, 40))

    x = 372
    y = 150
    fi = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf", 12)
    d.text((x, y), cfg["lore"], font=fi, fill=(92, 70, 48))
    y += 30
    d.text((x, y), cfg["blessing"], font=rs.font(14), fill=(122, 40, 30))
    y += 24
    for subject, value in cfg["lines"]:
        line(d, x, y, subject, value)
        y += 20

    y += 14
    d.text((x, y), "CLASS SKILLS", font=rs.font(14), fill=(122, 40, 30))
    y += 24
    for i, (name, icon) in enumerate(cfg["skills"]):
        cx = x + 40 + i * 120
        if icon:
            tex = Image.open(os.path.join(A, icon)).convert("RGBA").resize((49, 54), Image.LANCZOS)
            img.alpha_composite(tex, (cx - 24, y))
        else:
            sock = Image.open(os.path.join(A, "Slot_Empty.png")).convert("RGBA").resize((49, 54), Image.LANCZOS)
            img.alpha_composite(sock, (cx - 24, y))
            d = ImageDraw.Draw(img)
            ini = "".join(wd[0] for wd in name.split())
            rs.text_c(d, cx, y + 17, ini, rs.font(15), (236, 220, 180), shadow=(0, 0, 0))
        d = ImageDraw.Draw(img)
        rs.text_c(d, cx, y + 58, name, rs.font(11), INK)
    y += 84

    d.text((x, y), "ADVANCES AT LV 16 INTO", font=rs.font(14), fill=(122, 40, 30))
    y += 26
    for i, (title, mastery) in enumerate(cfg["advances"]):
        plaque_text(img, x + 115 + i * 250, y + 26, 220, 52, title, mastery + " (Mastery)")
    d = ImageDraw.Draw(img)
    y += 66
    d.text((x, y), "Class Reset at the Altar is free until you Advance.", font=fi, fill=(92, 70, 48))

    # Footer: info box, hotbar preview, Grace locked, CHOOSE plaque
    img = rs.inpaint_h(img, (40, 541, 196, 606), grain=0.6)
    d = ImageDraw.Draw(img)
    rs.text_c(d, 118, 552, "DRAGON'S ALTAR", rs.font(14), (236, 206, 130), shadow=(0, 0, 0))
    rs.text_c(d, 118, 573, "Your Class skills start", rs.font(10, False), (226, 214, 186))
    rs.text_c(d, 118, 586, "on slots 1 - 3.", rs.font(10, False), (226, 214, 186))
    for i, cx in enumerate(rs.SLOT_CENTERS):
        name, icon = cfg["skills"][i] if i < 3 else ("", None)
        if icon:
            tex = Image.open(os.path.join(A, icon)).convert("RGBA").resize((49, 54), Image.LANCZOS)
            img.alpha_composite(tex, (cx - 24, 551))
        else:
            rs.empty_socket(img, cx, locked=i >= 3)
            if name:
                d = ImageDraw.Draw(img)
                rs.text_c(d, cx, 566, "".join(wd[0] for wd in name.split()), rs.font(13), (236, 220, 180), shadow=(0, 0, 0))
    rs.erase_grace(img, locked=True)
    rs.hotkey_labels(img, [(cx, "M4 + " + str(i + 1), (237, 214, 158) if i < 3 else (140, 124, 96)) for i, cx in enumerate(rs.SLOT_CENTERS)]
                     + [(691, "M4 + R", (140, 124, 96))])
    plaque = Image.open(os.path.join(A, "Confirm_Plaque.png")).convert("RGBA").resize((160, 42), Image.LANCZOS)
    img.alpha_composite(plaque, (872 - 80, 578 - 21))
    d = ImageDraw.Draw(img)
    rs.text_c(d, 872, 563, "CHOOSE", rs.font(15), (250, 214, 120), shadow=(0, 0, 0))
    rs.text_c(d, 872, 583, focus.upper(), rs.font(9), (236, 220, 180), shadow=(0, 0, 0))
    return img


if __name__ == "__main__":
    for cls in ("Cleric", "Warrior", "Sorcerer"):
        render(cls).convert("RGB").resize((1180, 772), Image.LANCZOS).save(os.path.join(OUT, "ALTAR_ClassSelect_%s.png" % cls))
    print("altar previews written")
