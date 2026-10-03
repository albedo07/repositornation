"""Approximate in-game render of the Dragon's Altar from the shipped assets + the same concept-px layout
as AlbedosCustomClasses.Core.cs (DejaVu Serif stands in for Averia Serif).
Usage: python3 tools/render_altar_ingame.py -> docs/previews/ALTAR_v0.19.2_*.png
"""
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
OUT = os.path.join(ROOT, "docs", "previews")
S = 2  # backdrop scale
BOLD = "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"
REG = "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf"
CARDS = {"Warrior": (49, 240), "Cleric": (49, 360), "Sorcerer": (49, 482)}

DATA = {
    "Cleric": ("Holy hybrid support",
               [("IDENTITY", "Cleric mixes holy offense with healing and setup. It can contribute meaningful damage without giving up the ability to rescue itself or a party."),
                ("CORE MECHANICS", "Shield Weapon Mastery multiplies Block Force and Block Power of every Shield by 1.5x. Divine Duality allows a Staff and Shield to be equipped together. Lightning Zap gives immediate cone pressure, Righteous Strike calls lightning onto a physical target and applies Expose, and Holy Wave restores health around the caster."),
                ("PLAYSTYLE", "Best for players who like flexible roles, strong defense, Staff + Shield magic, lightning and holy effects, and having answers for both damage and recovery.")],
               ["Lightning Zap", "Righteous Strike", "Holy Wave"], ["Paladin", "Priest"]),
    "Sorcerer": ("Eitr-first magic specialist",
                 [("IDENTITY", "Sorcerer is the magic-first base class. Its power comes from Eitr management, magical damage and large spell effects rather than conventional weapon damage."),
                  ("CORE MECHANICS", "Arcane Blood grants +30% Eitr Regen, +40 flat Max Eitr and +30% Magic Damage to Eitr-based attacks and skills. Flame Burst supplies fast Fire pressure, Glacial Descent gives a large Frost impact, and Stonefang Eruption controls the ground with Blunt/Pierce damage, Stun and Cripple. The planned emergency Arcane Blood teleport remains intentionally unimplemented until its safe-destination rules are designed."),
                  ("PLAYSTYLE", "Best for players who want spell rotations, resource management, ranged control and spectacular magic, then specialize into deliberate charged casting or extremely mobile rapid casting.")],
                 ["Flame Burst", "Glacial Descent", "Stonefang Eruption"], ["Wizard", "Spellcaster"]),
    "Paladin": ("Holy impact and resilient offense",
                [("IDENTITY", "Paladin is Cleric's durable battle branch: a holy bruiser that can specialize toward magic or weapon-and-shield pressure."),
                 ("MECHANICS", "Choose one locked passive. Elemental Savant grants +25% elemental damage, +30 flat Eitr and +30% Eitr Regen. Holy Knight grants +25% Movement Speed, +35 HP, +35 Stamina, +30% HP/Stamina Regen and +75% Attack Speed while any weapon is paired with any Shield. Existing Paladin skills remain unchanged in this foundation pass."),
                 ("BEST FOR", "Players who want either durable elemental casting or aggressive weapon-and-shield combat.")],
                None, ["Paladin", "Priest"]),
}
ICONS = {"Lightning Zap": "Icon_lightning_zap.png", "Righteous Strike": "Icon_righteous_strike_Normal.png", "Holy Wave": "Icon_holy_wave.png"}


def f(path, px):
    return ImageFont.truetype(path, int(round(px * S)))


def text_c(d, cx, y, t, font, fill):
    w = d.textlength(t, font=font)
    d.text((cx * S - w / 2, y * S), t, font=font, fill=fill)


def paste(img, name, x, y, w=None, h=None, alpha=1.0):
    p = Image.open(os.path.join(A, name)).convert("RGBA")
    if w:
        p = p.resize((int(w * S), int(h * S)), Image.LANCZOS)
    if alpha < 1:
        a = p.getchannel("A").point(lambda v: int(v * alpha))
        p.putalpha(a)
    img.alpha_composite(p, (int(x * S), int(y * S)))


def wrap(d, text, font, width):
    out, cur = [], ""
    for w in text.split():
        t = (cur + " " + w).strip()
        if d.textlength(t, font=font) > width * S and cur:
            out.append(cur)
            cur = w
        else:
            cur = t
    if cur:
        out.append(cur)
    return out


def description(img, sections, top=224, bottom=468):
    d = ImageDraw.Draw(img)
    size = 10.6
    while size > 7.5:
        body = f(REG, size)
        h = sum(size + 9 + len(wrap(d, p, body, 300)) * (size + 3.2) + size * 0.45 for _, p in sections)
        if h <= bottom - top:
            break
        size -= 0.5
    body = f(REG, size)
    y = top
    for head, para in sections:
        paste(img, "Altar_HeadingStar.png", 365, y + 2, 11, 11)
        hf = f(BOLD, size + 1.4)
        d.text((380 * S, (y - 1) * S), head, font=hf, fill=(132, 30, 24))
        lx = 386 + d.textlength(head, font=hf) / S
        paste(img, "Altar_HeadingStar.png", lx, y + 3, 9, 9)
        d.line(((lx + 11) * S, (y + 7) * S, 630 * S, (y + 7) * S), fill=(160, 112, 52), width=2)
        y += size + 9
        for ln in wrap(d, para, body, 300):
            d.text((365 * S, y * S), ln, font=body, fill=(42, 32, 26))
            y += size + 3.2
        y += size * 0.45


def common(img, selected, base="None", adv="None"):
    d = ImageDraw.Draw(img)
    d.text((303 * S, 127 * S), base, font=f(BOLD, 10.5), fill=(140, 26, 20))
    d.text((512 * S, 127 * S), adv, font=f(BOLD, 10.5), fill=(140, 26, 20))
    x, y = CARDS[selected]
    paste(img, "Altar_CardHighlight.png", x - 10, y - 8, 298, 126)


def header(img, title, role, cx=550, top=161):
    d = ImageDraw.Draw(img)
    size = 22.0
    while size > 13 and d.textlength(title.upper(), font=f(BOLD, size)) / S > 170:
        size -= 0.5
    tw = d.textlength(title.upper(), font=f(BOLD, size)) / S
    paste(img, "Altar_HeadingStar.png", cx - tw / 2 - 22, top + 8, 14, 14)
    paste(img, "Altar_HeadingStar.png", cx + tw / 2 + 8, top + 8, 14, 14)
    d = ImageDraw.Draw(img)
    text_c(d, cx, top + 2, title.upper(), f(BOLD, size), (247, 235, 204))
    text_c(d, cx, top + 32, role.upper().replace(" ", "  "), f(BOLD, 9), (220, 210, 184))


def button(img, x, y, w, h, text, navy, enabled=True):
    d = ImageDraw.Draw(img)
    col = (245, 235, 210) if navy else (36, 40, 66)
    if not enabled:
        col = (140, 140, 140)
    size = 11 if len(text) > 18 else (14.5 if navy else 11.5)
    text_c(d, x + w / 2, y + h / 2 - size * 0.7, text, f(BOLD, size), col)


def emblems(img, names, focused):
    d = ImageDraw.Draw(img)
    for cx, n in zip((625, 704), names):
        if n == focused:
            paste(img, "Altar_EmblemGlow.png", cx - 31, 500, 62, 62)
        paste(img, "Altar_Emblem_" + n.replace(" ", "") + ".png", cx - 23, 508, 46, 46)
        text_c(d, cx, 557, n, f(REG, 9), (42, 32, 26))


def base_page(cls):
    img = Image.open(os.path.join(A, "Altar_Backdrop.png")).convert("RGBA")
    role, sections, kit, adv = DATA[cls]
    common(img, cls)
    header(img, cls, role)
    description(img, sections)
    d = ImageDraw.Draw(img)
    for cx, name in zip((395, 461, 526), kit):
        if name in ICONS:
            paste(img, ICONS[name], cx - 20, 509, 40, 42)
        else:
            paste(img, "Slot_Empty.png", cx - 20, 509, 40, 42)
            text_c(d, cx, 520, "".join(w[0] for w in name.split()), f(BOLD, 12), (240, 228, 200))
        text_c(d, cx, 554, name, f(REG, 7.8), (42, 32, 26))
    emblems(img, adv, "")
    button(img, 365, 575, 202, 47, "Choose " + cls, True)
    button(img, 577, 576, 173, 44, "View Advancements", False)
    return img


def adv_page(ac, parent="Cleric", chosen=True):
    img = Image.open(os.path.join(A, "Altar_Backdrop.png")).convert("RGBA")
    role, sections, _, pair = DATA[ac]
    common(img, parent, base=parent if chosen else "None")
    header(img, ac, role)
    description(img, sections)
    paste(img, "Altar_BottomLeftBlank.png", 356, 480, 210, 92)
    d = ImageDraw.Draw(img)
    text_c(d, 462, 486, "S K I L L S", f(BOLD, 10.5), (132, 30, 24))
    body = f(REG, 9)
    y = 503
    for ln in wrap(d, "Goddess Relic, Ray of Hope, Shield Charge, Electric Smite (Ultimate)", body, 192):
        d.text((368 * S, y * S), ln, font=body, fill=(42, 32, 26))
        y += 12
    y += 5
    d.text((368 * S, y * S), "PASSIVE CHOICE", font=f(BOLD, 9), fill=(42, 32, 26))
    d.text((368 * S + d.textlength("PASSIVE CHOICE  ", font=f(BOLD, 9)), y * S), "Elemental Savant, Holy Knight", font=body, fill=(42, 32, 26))
    emblems(img, pair, ac)
    button(img, 365, 575, 202, 47, ("Choose " + ac) if chosen else ("Choose " + parent + " First"), True, chosen)
    button(img, 577, 576, 173, 44, "< Base Classes", False)
    return img


def confirm():
    img = base_page("Cleric")
    dim = Image.new("RGBA", img.size, (0, 0, 0, 140))
    img.alpha_composite(dim)
    dw, dh = 425, 238
    dx, dy = (802 - dw) / 2, (687 - dh) / 2
    paste(img, "Altar_Dialog.png", dx, dy, dw, dh)
    header(img, "Confirm", "Your Choice", dx + 198, dy + 9)
    d = ImageDraw.Draw(img)
    lines = ["Choose Cleric as your Base Class?", "", "Changing your Base Class clears any current", "Advancement and class-specific locked choices.", "", "Are you definite with this choice?"]
    y = dy + 66
    for ln in lines:
        if ln:
            text_c(d, dx + 154, y, ln, f(BOLD if "Choose" in ln else REG, 10.5), (42, 32, 26))
        y += 15
    button(img, dx + 15, dy + 181, 202, 47, "Yes, Confirm", True)
    button(img, dx + 227, dy + 182, 173, 44, "Cancel", False)
    return img


def save(im, name):
    im.convert("RGB").resize((1203, 1030), Image.LANCZOS).save(os.path.join(OUT, name))


if __name__ == "__main__":
    save(base_page("Cleric"), "ALTAR_v0.19.2_BaseClasses_Cleric.png")
    save(base_page("Sorcerer"), "ALTAR_v0.19.2_BaseClasses_Sorcerer.png")
    save(adv_page("Paladin"), "ALTAR_v0.19.2_Advancements_Paladin.png")
    save(confirm(), "ALTAR_v0.19.2_Confirmation.png")
    print("altar in-game previews written")


# ---------------------------------------------------------------- v0.19.3 Advancement page
AC_SKILLS = {
    "Paladin": ["Goddess Relic", "Judgement Hammer", "Shield Charge", "Fallen Angel", "Ray of Hope", "Electric Smite"],
    "Priest": ["Lightning Relic", "Holy Relic", "Divine Intervention", "Grand Cross", "Heaven's Judgement", "Lightning Tempest"],
    "Sword Master": ["Moonlight Splitter", "Crescent Cleave", "Blade Storm", "Frenzied Charge", "Eclipse", "Halfmoon Slash"],
    "Mercenary": ["Stomp", "Circle Swing", "Bonecrusher", "Seismic Guillotine", "Reaver's Orbit", "Whirlwind"],
}
AC_PASSIVE = {"Paladin": ("Holy Trinity", "Heaven's Light"), "Priest": ("Bless Thy Sinners", "Grand Sigil"),
              "Sword Master": ("The Way of the Sword", "Knight's Guidance"), "Mercenary": ("Warfreak", "Battlecry")}
AC_ROLE = {"Paladin": "Holy impact and resilient offense", "Priest": "Relics, healing and team support",
           "Sword Master": "Fast sword specialist", "Mercenary": "Control and brutal AoE pressure"}
AC_CARD_ROLE = {"Sword Master": "Speed + spirit swordplay", "Mercenary": "Control + brutal AoE"}
AC_DESC = {
    "Priest": [("IDENTITY", "Priest is the tactical Cross-field support branch. Lightning Relic and Holy Relic establish battlefield anchors. Divine Intervention and Heaven's Judgement can self-cast or Cross Cast from an aimed active Cross; Grand Cross is deliberately self-cast only."),
               ("MECHANICS", "Grand Sigil now adds +30% of current Armor. Its Priest death-save retains the 1 HP + 50% recovery behavior on a 20-minute cooldown; allies within 20m of a living Priest can receive the same save on their own 35-minute cooldown. The activatable barrier remains."),
               ("BEST FOR", "Players who want battlefield anchors, ranged support placement and a support identity built around staying alive long enough to keep everyone else alive.")],
    "Sword Master": [("IDENTITY", "Sword Master turns Warrior into a high-tempo sword specialist built around fast attacks, broad spirit slashes and precise burst windows."),
                     ("MECHANICS", "Moonlight Splitter fires three extra-wide Ghost slashes across the full 50m. Crescent Cleave spreads five large ground cleaves across a very wide 120 degree cone for 20m. Judgement Cut remains the four-charge spatial slash sphere for now. The Way of the Sword grants +100% Attack Speed only while exactly one Sword is equipped with an empty off-hand; the old activatable Sword Mastery burst is retired."),
                     ("BEST FOR", "Aggressive players who want flashy sword pressure, ranged melee, charge management and a fast combo rhythm rather than tanking through everything.")],
}
SKILL_ICONS = {n: "Icon_" + n.lower().replace("'", "").replace(" ", "_") + ".png" for n in
               ["Goddess Relic", "Judgement Hammer", "Shield Charge", "Fallen Angel", "Ray of Hope", "Electric Smite", "Lightning Relic",
                "Holy Relic", "Divine Intervention", "Grand Cross", "Heaven's Judgement", "Lightning Tempest", "Grand Sigil", "Heaven's Light"]}


def skill_icon(img, name, x, y, size):
    d = ImageDraw.Draw(img)
    if name in SKILL_ICONS:
        paste(img, SKILL_ICONS[name], x, y, size, size)
    else:
        paste(img, "Slot_Empty.png", x, y, size, size)
        text_c(d, x + size / 2, y + size * 0.28, "".join(w[0] for w in name.split())[:3], f(BOLD, size * 0.34), (240, 228, 200))


def section_header(img, x, y, text, line_end):
    d = ImageDraw.Draw(img)
    paste(img, "Altar_HeadingStar.png", x, y + 2, 10, 10)
    hf = f(BOLD, 11.5)
    d.text(((x + 13) * S, (y - 2) * S), text, font=hf, fill=(132, 30, 24))
    lx = x + 18 + d.textlength(text, font=hf) / S
    paste(img, "Altar_HeadingStar.png", lx, y + 3, 8, 8)
    d.line(((lx + 10) * S, (y + 7) * S, line_end * S, (y + 7) * S), fill=(160, 112, 52), width=2)


def ac_page(parent, pair, focus, chosen=True):
    img = Image.open(os.path.join(A, "AltarAC_Backdrop.png")).convert("RGBA")
    d = ImageDraw.Draw(img)
    d.text((294 * S, 119 * S), parent if chosen else "None", font=f(BOLD, 10.5), fill=(140, 26, 20))
    d.text((506 * S, 119 * S), "None", font=f(BOLD, 10.5), fill=(140, 26, 20))
    slots = [(35, 265), (35, 388)]
    for (x, y), ac in zip(slots, pair):
        if parent != "Cleric":
            paste(img, "AltarAC_Card_" + ac.replace(" ", "") + ".png", x, y, 288, 117)
            d = ImageDraw.Draw(img)
            d.text(((x + 104) * S, (y + 38) * S), ac.upper(), font=f(BOLD, 15 if len(ac) > 10 else 18), fill=(30, 22, 24))
            d.text(((x + 104) * S, (y + 63) * S), AC_CARD_ROLE[ac], font=f(REG, 9.5), fill=(30, 22, 24))
        if ac == focus:
            paste(img, "AltarAC_CardHighlight.png", x - 8, y - 8, 304, 133)
    header(img, focus, AC_ROLE[focus], 547, 154)
    description(img, AC_DESC.get(focus, AC_DESC["Priest"]), 212, 462)
    d = ImageDraw.Draw(img)
    body = f(REG, 9.4)
    skills = AC_SKILLS[focus]
    for i, s in enumerate(skills):
        y = 495 + i * 14.2
        skill_icon(img, s, 372, y, 14)
        d = ImageDraw.Draw(img)
        d.text((392 * S, (y + 0.5) * S), s + ("  (Ultimate)" if i == len(skills) - 1 else ""), font=body, fill=(42, 32, 26))
    section_header(img, 633, 478, "PASSIVE", 752)
    mastery, grace = AC_PASSIVE[focus]
    paste(img, "Altar_Emblem_" + focus.replace(" ", "") + ".png", 632, 496, 24, 24)
    d = ImageDraw.Draw(img)
    ms = 9.4
    while ms > 7 and d.textlength(mastery, font=f(BOLD, ms)) / S > 98:
        ms -= 0.4
    d.text((660 * S, 497 * S), mastery, font=f(BOLD, ms), fill=(42, 32, 26))
    d.text((660 * S, 509 * S), "Mastery", font=body, fill=(42, 32, 26))
    skill_icon(img, grace, 632, 530, 24)
    d = ImageDraw.Draw(img)
    d.text((660 * S, 531 * S), grace, font=f(BOLD, 9.4), fill=(42, 32, 26))
    d.text((660 * S, 543 * S), "Grace  (M4 + R)", font=body, fill=(42, 32, 26))
    button(img, 434, 580, 214, 43, ("Choose " + focus) if chosen else ("Choose " + parent + " First"), True, chosen)
    return img


if __name__ == "__main__":
    save(ac_page("Cleric", ["Paladin", "Priest"], "Priest"), "ALTAR_v0.19.3_Advancement_Priest.png")
    save(ac_page("Warrior", ["Sword Master", "Mercenary"], "Sword Master", chosen=False), "ALTAR_v0.19.3_Advancement_SwordMaster.png")
    print("advancement page previews written")
