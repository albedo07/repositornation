"""v0.20.5 mock of the F8 Config window layout (same coordinates as DragonsAltar.DevTools.cs).
Approximation only: Unity IMGUI default skin replaced by flat boxes, DejaVu Sans as the font.
Usage: python3 tools/render_config_window.py -> docs/previews/PREVIEW_v0.20.5_Config_*.png"""
import os
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "docs", "previews")
F = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
FB = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
BG, BTN, SEL, ON, TXT, DIM = (9, 10, 14), (26, 31, 41), (51, 107, 133), (46, 122, 66), (235, 238, 242), (158, 168, 184)


def f(size, bold=False):
    return ImageFont.truetype(FB if bold else F, size)


def box(d, r, fill, outline=(70, 76, 88)):
    d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], fill=fill, outline=outline)


def btn(d, r, text, fill=BTN, size=12, bold=True, left=False):
    box(d, r, fill)
    fo = f(size, bold)
    w = d.textlength(text, font=fo)
    x = r[0] + 8 if left else r[0] + (r[2] - w) / 2
    d.text((x, r[1] + (r[3] - size) / 2 - 1), text, font=fo, fill=TXT)


def chrome(d, tab):
    d.rectangle([0, 0, 1120, 22], fill=(150, 155, 165))
    d.text((480, 4), "IMMORTAL HEROES - CONFIG", font=f(12), fill=(20, 20, 20))
    d.text((22, 30), "Immortal Heroes Config", font=f(21, True), fill=TXT)
    d.text((22, 60), "All changes saved   |   F8 / Esc closes", font=f(12), fill=DIM)
    btn(d, (440, 34, 190, 34), "TEST COOLDOWNS: ON", ON)
    d.text((646, 42), "[x] Auto-save", font=f(12), fill=TXT)
    btn(d, (755, 34, 100, 34), "SAVE"); btn(d, (862, 34, 120, 34), "RELOAD .CFG"); btn(d, (990, 34, 108, 34), "CLOSE")
    tabs = ("Warrior", "Cleric", "Sorcerer", "Progression", "Testing", "General")
    for i, t in enumerate(tabs):
        btn(d, (22 + i * 130, 84, 124, 28), t, SEL if t == tab else BTN, 13)
    d.text((812, 90), "Search", font=f(12), fill=DIM)
    box(d, (868, 85, 230, 26), (20, 22, 28))


def cleric(dropdown):
    img = Image.new("RGB", (1120, 700), BG)
    d = ImageDraw.Draw(img)
    chrome(d, "Cleric")
    box(d, (18, 122, 300, 562), (16, 18, 24)); d.text((140, 126), "SECTIONS", font=f(11), fill=TXT)
    box(d, (328, 122, 774, 562), (16, 18, 24))
    d.text((30, 146), "SHOW SETTINGS FOR", font=f(12), fill=DIM)
    btn(d, (28, 166, 282, 30), "Paladin  (Advancement)   " + ("^" if dropdown else "v"), SEL, 13)
    if dropdown:
        for i, g in enumerate(("Cleric  (Base Class)", "Paladin  (Advancement)", "Priest  (Advancement)")):
            btn(d, (36, 204 + i * 34, 266, 30), g, SEL if i == 1 else BTN, 12, False, True)
    else:
        secs = ("Aegis Fall", "Divine Verdict", "Electric Smite", "Electric Smite Ascended", "Electric Smite Damage v2",
                "Fallen Angel", "Fallen Angel Ascended", "Goddess Relic", "Goddess Relic Ascended", "Heavens Light",
                "Judgement Hammer", "Judgement Hammer Ascended", "Judgement Mark", "Passive - Holy Knight")
        for i, sname in enumerate(secs):
            btn(d, (32, 208 + i * 32, 252, 28), sname, SEL if sname == "Goddess Relic" else BTN, 12, False, True)
    d.text((344, 128), "Paladin Goddess Relic", font=f(21, True), fill=TXT)
    btn(d, (918, 130, 170, 26), "SECTION DEFAULTS")
    d.text((344, 158), "[x] Show range / radius preview in the world", font=f(12), fill=TXT)
    rows = (("Radius", "5", "Damage radius in meters."), ("CrossHeight", "6.5", "Cross height (0-star Troll)."),
            ("Cooldown", "30", "Seconds."), ("StaminaCost", "30", "Stamina cost."))
    for i, (k, v, desc) in enumerate(rows):
        y = 186 + i * 58
        btn(d, (342, y + 3, 210, 28), k, SEL if i == 0 else BTN, 12, False, True)
        d.line([564, y + 18, 864, y + 18], fill=(70, 76, 88), width=4)
        d.rectangle([600 + i * 40, y + 10, 612 + i * 40, y + 26], fill=(200, 200, 200))
        box(d, (878, y + 3, 90, 28), (20, 22, 28)); d.text((915, y + 9), v, font=f(13), fill=TXT)
        btn(d, (978, y + 3, 78, 28), "Default")
        d.text((346, y + 35), "Default " + v + "   |   " + desc, font=f(11), fill=DIM)
    return img


def progression():
    img = Image.new("RGB", (1120, 700), BG)
    d = ImageDraw.Draw(img)
    chrome(d, "Progression")
    box(d, (18, 122, 540, 562), (16, 18, 24)); d.text((250, 126), "TIER POINTS", font=f(11), fill=TXT)
    box(d, (568, 122, 534, 562), (16, 18, 24)); d.text((795, 126), "ASCENSIONS", font=f(11), fill=TXT)
    d.text((40, 150), "Lv 24   Cleric  >  Paladin", font=f(21, True), fill=TXT)
    d.text((40, 182), "Class points come from Lv 4-16, Advancement points from Lv 18 after Advancing.", font=f(11), fill=DIM)
    d.text((40, 197), "Bonus points are added on top of what your level gives.", font=f(11), fill=DIM)

    def stepper(y, label, v):
        d.text((40, y + 7), label, font=f(12), fill=TXT)
        btn(d, (236, y, 34, 30), "-"); box(d, (276, y, 70, 30), (20, 22, 28)); d.text((303, y + 7), v, font=f(13), fill=TXT)
        btn(d, (352, y, 34, 30), "+"); btn(d, (394, y, 64, 30), "SET")
    stepper(230, "Level", "24")
    btn(d, (40, 290, 500, 24), "CLASS TIER POINTS")
    d.text((40, 320), "Left 0   |   Earned 14   |   Spent 14", font=f(12), fill=TXT)
    stepper(346, "Bonus Class points", "0")
    btn(d, (40, 406, 500, 24), "ADVANCEMENT TIER POINTS")
    d.text((40, 436), "Left 1   |   Earned 4   |   Spent 3", font=f(12), fill=TXT)
    stepper(462, "Bonus Advancement points", "0")
    d.text((40, 498), "Advancement points only count once the character has Advanced.", font=f(11), fill=DIM)
    btn(d, (40, 620, 260, 34), "RESET ALL TIERS & ASCENSIONS")
    d.text((310, 630), "Gives every spent point back.", font=f(11), fill=DIM)

    d.text((588, 152), "Switch a skill to its Ascended version right away (testing: no level or Tier rules).", font=f(11), fill=DIM)
    d.text((588, 167), "The Skill Tree shows it with the Magenta frame.", font=f(11), fill=DIM)
    btn(d, (588, 196, 500, 22), "PALADIN")
    skills = ("Righteous Strike", "Goddess Relic", "Judgement Hammer", "Shield Charge", "Fallen Angel", "Ray of Hope", "Electric Smite  (Ultimate)")
    for i, sk in enumerate(skills):
        y = 226 + i * 40
        d.text((596, y + 7), sk, font=f(12), fill=TXT)
        on = sk in ("Righteous Strike", "Goddess Relic")
        btn(d, (920, y, 160, 30), "ASCENDED" if on else "NORMAL", ON if on else BTN)
    d.text((596, 508), "Righteous Strike is always Ascended for an Advanced Paladin.", font=f(11), fill=DIM)
    btn(d, (588, 540, 500, 22), "PRIEST")
    d.text((596, 572), "No Ascended versions are designed for Priest yet.", font=f(11), fill=DIM)
    return img


def main():
    a, b, c = cleric(False), cleric(True), progression()
    out = Image.new("RGB", (1120, 700 * 3 + 16), (40, 40, 40))
    for i, im in enumerate((a, b, c)):
        out.paste(im, (0, i * 708))
    out.save(os.path.join(OUT, "PREVIEW_v0.20.5_Config_Window.png"))
    print("written")


if __name__ == "__main__":
    main()
