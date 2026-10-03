"""Design preview: the existing Dragon's Altar menu, re-skinned in the Immortal Heroes Skill Tree style.
Same structure and text as AlbedosCustomClasses.Core.cs:
  Base Classes page  - left list (icon + name + role), right detail (IDENTITY / CORE MECHANICS / PLAYSTYLE,
                       starter kit + advancements), Choose <Class> / View Advancements, Reset / Close, status.
  Advancement page   - < Base Classes, base summary, left list of the 2 Advancements, right detail, Choose <AC>.
  Confirmation popup - CONFIRM YOUR CHOICE + Yes, Confirm / Cancel.
Mockup only: DejaVu Serif stands in for Averia Serif; class glyphs stand in for real icon art.
Usage: python3 tools/render_altar_preview.py -> docs/previews/ALTAR_*.png
"""
import os
import re

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

import render_states as rs

A, OUT = rs.A, rs.OUT
GLYPH = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
SERIF_REG = "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf"
INK = (58, 38, 22)
HEAD = (122, 40, 30)
LIGHT = (226, 214, 186)

# Text copied from AlbedosCustomClasses.Core.cs
BASE = {
    "Warrior": {"role": "Front-line physical bruiser", "hue": 4, "glyph": "⚔",
                "desc": "<b>IDENTITY</b>\nWarrior is the direct melee front-liner: simple to understand, difficult to bully, and built to stay close while forcing enemies to respect physical pressure.\n\n<b>CORE MECHANICS</b>\nHyper Armor protects the Warrior from interruption by ordinary hits below the class threshold. Heavy Slash punishes with Broken Bones, Impact Wave controls a line in front of you, and Impact Punch gives fast close-range Blunt pressure plus Small-enemy Stun.\n\n<b>PLAYSTYLE</b>\nBest for players who want to commit to melee, trade confidently, control space with physical attacks, and later specialize into speed or overwhelming AoE.",
                "kit": ["Heavy Slash", "Impact Wave", "Impact Punch"], "adv": ["Sword Master", "Mercenary"]},
    "Cleric": {"role": "Holy hybrid support", "hue": 205, "glyph": "✝",
               "desc": "<b>IDENTITY</b>\nCleric mixes holy offense with healing and setup. It can contribute meaningful damage without giving up the ability to rescue itself or a party.\n\n<b>CORE MECHANICS</b>\nShield Weapon Mastery multiplies Block Force and Block Power of every Shield by 1.5x. Divine Duality allows a Staff and Shield to be equipped together. Lightning Zap gives immediate cone pressure, Righteous Strike calls lightning onto a physical target and applies Expose, and Holy Wave restores health around the caster.\n\n<b>PLAYSTYLE</b>\nBest for players who like flexible roles, strong defense, Staff + Shield magic, lightning and holy effects, and having answers for both damage and recovery.",
               "kit": ["Lightning Zap", "Righteous Strike", "Holy Wave"], "adv": ["Paladin", "Priest"],
               "summary": "Holy battlemage foundation mixing Lightning pressure, healing and flexible divine equipment."},
    "Sorcerer": {"role": "Eitr-first magic specialist", "hue": 275, "glyph": "✦",
                 "desc": "<b>IDENTITY</b>\nSorcerer is the magic-first base class. Its power comes from Eitr management, magical damage and large spell effects rather than conventional weapon damage.\n\n<b>CORE MECHANICS</b>\nArcane Blood grants +30% Eitr Regen, +40 flat Max Eitr and +30% Magic Damage to Eitr-based attacks and skills. Flame Burst supplies fast Fire pressure, Glacial Descent gives a large Frost impact, and Stonefang Eruption controls the ground with Blunt/Pierce damage, Stun and Cripple.\n\n<b>PLAYSTYLE</b>\nBest for players who want spell rotations, resource management, ranged control and spectacular magic, then specialize into deliberate charged casting or extremely mobile rapid casting.",
                 "kit": ["Flame Burst", "Glacial Descent", "Stonefang Eruption"], "adv": ["Wizard", "Spellcaster"]},
}
ADV = {
    "Paladin": {"role": "Holy impact and resilient offense", "glyph": "✚", "hue": 345,
                "desc": "<b>IDENTITY</b>\nPaladin is Cleric's durable battle branch: a holy bruiser that can specialize toward magic or weapon-and-shield pressure.\n\n<b>MECHANICS</b>\nChoose one locked passive. Elemental Savant grants +25% elemental damage, +30 flat Eitr and +30% Eitr Regen. Holy Knight grants +25% Movement Speed, +35 HP, +35 Stamina, +30% HP/Stamina Regen and +75% Attack Speed while any weapon is paired with any Shield.\n\n<b>BEST FOR</b>\nPlayers who want either durable elemental casting or aggressive weapon-and-shield combat.",
                "skills": "Goddess Relic  |  Ray of Hope  |  Shield Charge  |  Electric Smite (Ultimate)", "passive": ("PASSIVE CHOICE", "Elemental Savant  |  Holy Knight")},
    "Priest": {"role": "Relics, healing and team support", "glyph": "☩", "hue": 190,
               "desc": "<b>IDENTITY</b>\nPriest is the tactical Cross-field support branch. Lightning Relic and Holy Relic establish battlefield anchors. Divine Intervention and Heaven's Judgement can self-cast or Cross Cast from an aimed active Cross; Grand Cross is deliberately self-cast only.\n\n<b>MECHANICS</b>\nGrand Sigil adds +30% of current Armor. Its Priest death-save retains the 1 HP + 50% recovery behavior on a 20-minute cooldown.\n\n<b>BEST FOR</b>\nPlayers who want battlefield anchors, ranged support placement and a support identity built around staying alive.",
               "skills": "Lightning Relic  |  Holy Relic  |  Divine Intervention  |  Grand Cross  |  Heaven's Judgement  |  Lightning Tempest (Ultimate)", "passive": ("PASSIVE", "Grand Sigil")},
}
ROW_NODES = ("lightning_zap", "righteous_strike", "holy_wave")


def serif(size, bold=True):
    return rs.font(size, bold)


def node_row(img, node_id, name, role, hue, glyph, selected):
    """A Class/Advancement entry = a tree node (tinted frame + emblem) with its name plate and role."""
    box, (cx, pb) = rs.NODES[node_id]
    x0, y0, x1, y1 = box
    img = rs.recolor_hue(img, (x0 - 6, y0 - 6, x1 + 6, y1 + 6), 150, 230, hue)
    ix0, iy0, ix1, iy1 = x0 + 13, y0 + 14, x1 - 13, y1 - 14
    w, h = ix1 - ix0, iy1 - iy0
    yy, xx = np.mgrid[0:h, 0:w]
    r = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    base = np.array(Image.new("RGB", (1, 1), "hsv(%d,70%%,55%%)" % hue).getpixel((0, 0)), dtype=np.float32)
    grad = base[None, None, :] * np.clip(1.0 - r * 0.75, 0.18, 1.0)[..., None] * 0.55
    img.paste(Image.fromarray(grad.clip(0, 255).astype(np.uint8)).convert("RGBA"), (ix0, iy0))
    d = ImageDraw.Draw(img)
    f = ImageFont.truetype(GLYPH, 40)
    gb = d.textbbox((0, 0), glyph, font=f)
    gx = (ix0 + ix1) / 2 - (gb[2] + gb[0]) / 2
    gy = (iy0 + iy1) / 2 - (gb[3] + gb[1]) / 2
    d.text((gx + 1, gy + 2), glyph, font=f, fill=(0, 0, 0, 170))
    d.text((gx, gy), glyph, font=f, fill=(250, 238, 205))
    img = rs.inpaint_h(img, (cx - 50, pb - 21, cx + 50, pb - 5), grain=0.6)
    d = ImageDraw.Draw(img)
    rs.text_c(d, cx, pb - 20, name.upper(), serif(12), INK)
    rw = d.textlength(role, font=serif(10))
    img = rs.veil(img, (cx - rw / 2 - 8, pb + 2, cx + rw / 2 + 8, pb + 18), (236, 226, 204), 215, radius=7)
    d = ImageDraw.Draw(img)
    rs.text_c(d, cx, pb + 3, role, serif(10), HEAD)
    if selected:
        glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(glow).rounded_rectangle((x0 - 7, y0 - 7, x1 + 7, y1 + 7), radius=10, outline=(255, 214, 120, 255), width=3)
        img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(3)))
        img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(1)))
    return img


def recolor_band(img, box, hue):
    reg = np.asarray(img.convert("RGB").crop(box), dtype=np.float32) / 255.0
    h, s, v = rs.hsv_arrays(reg)
    rose = ((h >= 315) | (h <= 15)) & (s > 0.15)
    target = np.array(Image.new("RGB", (1, 1), "hsv(%d,55%%,45%%)" % hue).getpixel((0, 0)), dtype=np.float32) / 255.0
    wgt = (np.clip((s - 0.15) / 0.15, 0, 1) * rose)[..., None]
    lum = reg.mean(axis=2, keepdims=True)
    out = reg * (1 - wgt) + target * (0.55 + lum * 0.9) * wgt
    img.paste(Image.fromarray((out.clip(0, 1) * 255).astype(np.uint8)).convert("RGBA"), box[:2])
    return img


def wrap(d, text, f, width):
    words, lines, cur = text.split(" "), [], ""
    for wd in words:
        t = (cur + " " + wd).strip()
        if d.textlength(t, font=f) > width and cur:
            lines.append(cur)
            cur = wd
        else:
            cur = t
    if cur:
        lines.append(cur)
    return lines


def rich_block(d, x, y, width, text, size=12):
    """<b>HEADING</b> lines in dark red, paragraphs wrapped in ink - the Core.cs description format."""
    body = ImageFont.truetype(SERIF_REG, size)
    for para in text.split("\n"):
        m = re.match(r"<b>(.*)</b>$", para.strip())
        if m:
            d.text((x, y), m.group(1), font=serif(13), fill=HEAD)
            y += 19
        elif para.strip() == "":
            y += 7
        else:
            for ln in wrap(d, para, body, width):
                d.text((x, y), ln, font=body, fill=INK)
                y += size + 4
    return y


def plaque_button(img, cx, cy, w, h, text, sub=None, enabled=True):
    pl = Image.open(os.path.join(A, "Confirm_Plaque.png")).convert("RGBA").resize((w, h), Image.LANCZOS)
    if not enabled:
        g = np.asarray(pl, dtype=np.float32)
        g[..., :3] = g[..., :3].mean(axis=2, keepdims=True) * 0.6
        pl = Image.fromarray(g.astype(np.uint8))
    img.alpha_composite(pl, (int(cx - w / 2), int(cy - h / 2)))
    d = ImageDraw.Draw(img)
    col = (250, 214, 120) if enabled else (150, 140, 120)
    if sub:
        rs.text_c(d, cx, cy - h * 0.34, text, serif(13), col, shadow=(0, 0, 0))
        rs.text_c(d, cx, cy + h * 0.06, sub, serif(8), LIGHT, shadow=(0, 0, 0))
    else:
        rs.text_c(d, cx, cy - 8, text, serif(13), col, shadow=(0, 0, 0))


def chrome(subtitle, left_title, left_sub):
    img = Image.open(os.path.join(A, "Cleric_Paladin_PreAdvance.png")).convert("RGBA")
    img = rs.inpaint_h(img, (118, 43, 372, 61), grain=0.6)
    d = ImageDraw.Draw(img)
    rs.text_c(d, 245, 46, subtitle, serif(11), (196, 178, 140))
    img = rs.inpaint_h(img, rs.CLERIC_TITLE)
    rs.header_title(img, 220, 102, left_title, 15 if len(left_title) > 9 else 22)
    d = ImageDraw.Draw(img)
    rs.text_c(d, 204, 124, left_sub, serif(10), (96, 84, 70))
    img = rs.remove_lz_badge(img)
    # Footer: status (left box), action bar (middle), RESET (right panel). Grace box becomes the Altar sigil.
    img = rs.inpaint_h(img, (40, 541, 196, 606), grain=0.6)
    img = rs.inpaint_h(img, (205, 546, 600, 606), grain=0.6)
    rs.erase_grace(img, locked=False)
    em = rs.compass_emblem(46)
    img.alpha_composite(em, (691 - 23, 570 - 23))
    return img


def right_panel(img, title, role, hue, crest):
    img = rs.inpaint_h(img, rs.PALADIN_TITLE)
    img = rs.neutral_body(img, rs.AC_BODY, rs.neutral_parchment((rs.AC_BODY[2] - rs.AC_BODY[0], rs.AC_BODY[3] - rs.AC_BODY[1]), seed=5),
                          keep_until_y=129, keep_boxes=((478, 125, 524, 150),))
    if hue is not None:
        img = recolor_band(img, (338, 60, 976, 131), hue)
    if not crest:
        img = rs.inpaint_h(img, rs.PALADIN_CREST, grain=1.0)
        em = rs.compass_emblem(66)
        img.alpha_composite(em, (552 - 33, 108 - 33))
    rs.header_title(img, 672, 102, title.upper(), 22 if len(title) < 10 else 18)
    d = ImageDraw.Draw(img)
    rs.text_c(d, 675, 124, role.upper(), serif(10), (92, 52, 40))
    return img


def status(img, base, adv):
    d = ImageDraw.Draw(img)
    rs.text_c(d, 118, 550, "DRAGON'S ALTAR", serif(13), (236, 206, 130), shadow=(0, 0, 0))
    rs.text_c(d, 118, 571, "Base class: " + base, serif(10, False), LIGHT)
    rs.text_c(d, 118, 586, "Advancement: " + adv, serif(10, False), LIGHT)
    rs.text_c(d, 691, 607, "ALTAR", serif(9), (150, 132, 100))
    plaque_button(img, 872, 578, 150, 40, "RESET", "CLASS RESET")


def base_page(focus="Cleric"):
    img = chrome("DRAGON'S  ALTAR   •   BASE CLASSES", "BASE CLASSES", "CHOOSE ONE")
    for node_id, cls in zip(ROW_NODES, ("Warrior", "Cleric", "Sorcerer")):
        c = BASE[cls]
        img = node_row(img, node_id, cls, c["role"], c["hue"], c["glyph"], cls == focus)
    c = BASE[focus]
    img = right_panel(img, focus, c["role"], None if focus == "Warrior" else c["hue"], focus == "Cleric")
    d = ImageDraw.Draw(img)
    y = rich_block(d, 368, 146, 590, c["desc"])
    y += 8
    d.text((368, y), "STARTER KIT", font=serif(13), fill=HEAD)
    d.text((368 + d.textlength("STARTER KIT", font=serif(13)) + 12, y), "  |  ".join(c["kit"]), font=serif(12, False), fill=INK)
    y += 20
    d.text((368, y), "ADVANCEMENTS", font=serif(13), fill=HEAD)
    d.text((368 + d.textlength("ADVANCEMENTS", font=serif(13)) + 12, y), "  |  ".join(c["adv"]), font=serif(12, False), fill=INK)
    plaque_button(img, 305, 578, 210, 42, "CHOOSE", focus.upper())
    plaque_button(img, 505, 578, 190, 42, "VIEW", "ADVANCEMENTS")
    status(img, "None", "None")
    return img


def advancement_page(parent="Cleric", focus="Paladin", chosen_class="Cleric"):
    img = chrome("DRAGON'S  ALTAR   •   ADVANCEMENTS", "ADVANCEMENTS", parent.upper() + " BRANCHES")
    pc = BASE[parent]
    rows = (("lightning_zap", pc["adv"][0]), ("righteous_strike", pc["adv"][1]))
    for node_id, name in rows:
        a = ADV[name]
        img = node_row(img, node_id, name, a["role"], a["hue"], a["glyph"], name == focus)
    # Third slot = the old "< Base Classes" button, as a node.
    img = node_row(img, "holy_wave", "Back", "Base Classes", 30, "\u21a9", False)
    a = ADV[focus]
    img = right_panel(img, focus, a["role"], None if focus == "Paladin" else 200, True)
    d = ImageDraw.Draw(img)
    # Base class summary (was the line above the panel in the old Altar).
    d.text((368, 144), parent.upper() + "  -  " + pc["role"].upper(), font=serif(11), fill=(140, 96, 40))
    d.text((368, 160), pc["summary"], font=ImageFont.truetype(SERIF_REG, 11), fill=(92, 70, 48))
    y = rich_block(d, 368, 184, 590, a["desc"])
    y += 8
    for label, value in (("SKILLS", a["skills"]), a["passive"]):
        d.text((368, y), label, font=serif(13), fill=HEAD)
        lw = d.textlength(label, font=serif(13)) + 12
        for i, ln in enumerate(wrap(d, value, ImageFont.truetype(SERIF_REG, 12), 590 - lw)):
            d.text((368 + lw, y + i * 16), ln, font=ImageFont.truetype(SERIF_REG, 12), fill=INK)
            last = i
        y += 20 + 16 * last
    can = chosen_class == parent
    plaque_button(img, 405, 578, 250, 42, "CHOOSE", focus.upper() if can else "CHOOSE " + parent.upper() + " FIRST", enabled=can)
    status(img, chosen_class or "None", "None")
    return img


def confirmation():
    img = base_page("Cleric")
    dim = Image.new("RGBA", img.size, (0, 0, 0, 150))
    img.alpha_composite(dim)
    pw, ph = 480, 236
    x0, y0 = (1011 - pw) // 2, (662 - ph) // 2
    card = rs.neutral_parchment((pw, ph), seed=9)
    mask = Image.new("L", (pw, ph), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, pw - 1, ph - 1), radius=14, fill=255)
    img.paste(card, (x0, y0), mask)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((x0 + 4, y0 + 4, x0 + pw - 5, y0 + 44), radius=10, fill=(24, 36, 62, 255))
    d.rounded_rectangle((x0, y0, x0 + pw - 1, y0 + ph - 1), radius=14, outline=(110, 76, 26, 255), width=5)
    d.rounded_rectangle((x0 + 2, y0 + 2, x0 + pw - 3, y0 + ph - 3), radius=13, outline=rs.GOLD + (255,), width=2)
    for cx in (x0 + 26, x0 + pw - 26):
        d.polygon([(cx, y0 + 18), (cx + 6, y0 + 24), (cx, y0 + 30), (cx - 6, y0 + 24)], fill=rs.GOLD + (255,))
    rs.text_c(d, 505, y0 + 14, "CONFIRM YOUR CHOICE", serif(17), (236, 206, 130), shadow=(0, 0, 0))
    body = ImageFont.truetype(SERIF_REG, 12)
    lines = ["Choose Cleric as your Base Class?", "",
             "Changing your Base Class clears any current Advancement", "and class-specific locked choices.", "",
             "Are you definite with this choice?"]
    yy = y0 + 60
    for i, ln in enumerate(lines):
        if ln:
            rs.text_c(d, 505, yy, ln, serif(12) if i == 0 else body, HEAD if i == 0 else INK)
        yy += 16
    plaque_button(img, 505 - 108, y0 + ph - 36, 190, 40, "YES, CONFIRM")
    plaque_button(img, 505 + 108, y0 + ph - 36, 190, 40, "CANCEL")
    return img


if __name__ == "__main__":
    def save(im, name):
        im.convert("RGB").resize((1180, 772), Image.LANCZOS).save(os.path.join(OUT, name))
    for old in os.listdir(OUT):
        if old.startswith("ALTAR_ClassSelect_"):
            os.remove(os.path.join(OUT, old))
    save(base_page("Cleric"), "ALTAR_1_BaseClasses_Cleric.png")
    save(base_page("Warrior"), "ALTAR_1_BaseClasses_Warrior.png")
    save(advancement_page("Cleric", "Paladin", "Cleric"), "ALTAR_2_Advancements_Paladin.png")
    save(advancement_page("Cleric", "Priest", ""), "ALTAR_2_Advancements_Priest_NotCleric.png")
    save(confirmation(), "ALTAR_3_Confirmation.png")
    print("altar previews written")
