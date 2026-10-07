"""v0.25.6 buff indicator icons (Buff_<id>.png, 64 px) shown by DragonCombat.ShowStatus.
Every icon has its OWN colour AND its own silhouette (user rule: no near-identical icons for different effects).
Usage: python3 tools/build_buff_icons.py
"""
import math
import os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "ImmortalHeroesAssets")
S = 256
BG = (18, 20, 26)


def badge(color):
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([6, 6, S - 6, S - 6], fill=BG + (235,), outline=color + (255,), width=10)
    return im, d


def star_pts(cx, cy, n, r1, r2, rot=-90):
    pts = []
    for i in range(n * 2):
        r = r1 if i % 2 == 0 else r2
        a = math.radians(rot + i * 180.0 / n)
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def shield(d, c, cx=128, cy=124, w=80, h=100):
    d.polygon([(cx - w, cy - h * 0.7), (cx, cy - h), (cx + w, cy - h * 0.7), (cx + w * 0.8, cy + h * 0.25), (cx, cy + h), (cx - w * 0.8, cy + h * 0.25)], fill=c)


def sword(d, c, cx=128, top=40, bot=222, w=20):
    d.polygon([(cx, top), (cx + w, top + 30), (cx + w * 0.6, bot - 52), (cx - w * 0.6, bot - 52), (cx - w, top + 30)], fill=c)
    d.rectangle([cx - 44, bot - 52, cx + 44, bot - 38], fill=c)
    d.rectangle([cx - 10, bot - 38, cx + 10, bot], fill=c)


# --- generic stat buffs -------------------------------------------------------------------
def i_hyper(d, c):  # anvil = unshakable
    d.polygon([(52, 92), (204, 92), (204, 112), (172, 128), (160, 160), (96, 160), (84, 128), (40, 112), (40, 100)], fill=c)
    d.rectangle([92, 160, 164, 176], fill=c)
    d.rectangle([72, 176, 184, 200], fill=c)


def i_barrier(d, c):  # golden bubble
    d.ellipse([44, 44, 212, 212], fill=(200, 160, 40, 255), outline=c, width=12)
    d.ellipse([70, 70, 200, 200], fill=(240, 196, 60, 255))
    d.ellipse([80, 70, 124, 108], fill=(255, 250, 220))


def i_defense(d, c):
    shield(d, c)
    d.line([(128, 50), (128, 200)], fill=BG, width=10)
    d.line([(66, 110), (190, 110)], fill=BG, width=10)


def i_damage(d, c):
    sword(d, c)


def i_haste(d, c):
    for i in range(3):
        x = 52 + i * 48
        d.polygon([(x, 70), (x + 50, 128), (x, 186), (x + 22, 186), (x + 72, 128), (x + 22, 70)], fill=c)


def i_attack_speed(d, c):  # two crossed swords
    for ang in (-35, 35):
        im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        sword(ImageDraw.Draw(im), c, w=16)
        im = im.rotate(ang, resample=Image.BICUBIC, center=(128, 128))
        d._image.alpha_composite(im)


def i_stamina(d, c):
    d.polygon([(140, 36), (80, 140), (122, 140), (106, 220), (178, 108), (134, 108), (160, 36)], fill=c)


def i_eitr(d, c):
    d.polygon([(128, 40), (176, 136), (80, 136)], fill=c)
    d.ellipse([80, 100, 176, 196], fill=c)
    d.ellipse([102, 132, 124, 156], fill=(240, 220, 255))


def i_clockwork(d, c):  # gear
    d.polygon(star_pts(128, 128, 10, 92, 72, rot=-90), fill=c)
    d.ellipse([84, 84, 172, 172], fill=BG)
    d.line([(128, 128), (128, 94)], fill=c, width=10)
    d.line([(128, 128), (152, 142)], fill=c, width=10)


def i_overcharge(d, c):  # burst star
    d.polygon(star_pts(128, 128, 8, 96, 40), fill=c)
    d.ellipse([108, 108, 148, 148], fill=(255, 235, 255))


def i_fury(d, c):  # flame
    d.polygon([(128, 34), (150, 84), (176, 70), (184, 150), (160, 210), (96, 210), (72, 150), (84, 96), (104, 118)], fill=c)
    d.polygon([(128, 116), (150, 162), (138, 204), (118, 204), (106, 162)], fill=(255, 220, 120))


def i_focus(d, c):  # crosshair
    d.ellipse([60, 60, 196, 196], outline=c, width=12)
    d.ellipse([114, 114, 142, 142], fill=c)
    for a in range(4):
        r = math.radians(a * 90)
        d.line([(128 + math.cos(r) * 48, 128 + math.sin(r) * 48), (128 + math.cos(r) * 96, 128 + math.sin(r) * 96)], fill=c, width=12)


def i_vigil(d, c):  # hawk eye
    d.polygon([(36, 128), (90, 84), (166, 84), (220, 128), (166, 172), (90, 172)], fill=c)
    d.ellipse([96, 96, 160, 160], fill=BG)
    d.ellipse([116, 108, 140, 148], fill=(255, 230, 120))


def i_generic(d, c):
    d.polygon(star_pts(128, 132, 5, 80, 34), fill=c)


# --- per-source buffs (each Grace / skill buff gets its own icon) --------------------------
def i_sun(d, c):  # Heaven's Light
    d.polygon(star_pts(128, 128, 12, 98, 60), fill=c)
    d.ellipse([78, 78, 178, 178], fill=(255, 250, 225))


def i_ray(d, c):  # Ray of Hope: beams from above onto a plus
    for x in (78, 128, 178):
        d.polygon([(x - 8, 40), (x + 8, 40), (x + 18, 150), (x - 18, 150)], fill=c + (200,))
    d.rectangle([110, 150, 146, 222], fill=c)
    d.rectangle([92, 168, 164, 204], fill=c)


def i_wing_boot(d, c):  # Knight's Guidance
    d.polygon([(96, 60), (136, 60), (136, 160), (196, 170), (196, 204), (96, 204)], fill=c)
    for i in range(3):
        y = 70 + i * 26
        d.polygon([(96, y), (40 + i * 10, y - 18), (52 + i * 10, y + 14), (96, y + 22)], fill=c)


def i_horn(d, c):  # Battlecry: war horn (trumpet) + sound arcs
    d.polygon([(40, 120), (40, 140), (120, 136), (160, 176), (160, 84), (120, 124)], fill=c)
    d.ellipse([146, 84, 176, 176], fill=c)
    for r in (34, 56):
        d.arc([176 - r, 130 - r, 176 + r, 130 + r], -50, 50, fill=c, width=9)


def i_relic(d, c):  # Holy Relic: cross
    d.rectangle([112, 40, 144, 220], fill=c)
    d.rectangle([66, 84, 190, 116], fill=c)
    d.ellipse([104, 76, 152, 124], fill=(255, 255, 230))


def i_wings(d, c):  # Divine Intervention: pair of angel wings
    for side in (-1, 1):
        for i in range(4):
            w, h = 84 - i * 14, 26
            y = 76 + i * 30
            x0 = 128 + side * 10
            x1 = x0 + side * w
            d.ellipse([min(x0, x1), y, max(x0, x1), y + h], fill=c)
    d.ellipse([116, 60, 140, 84], fill=(255, 230, 140))


def i_storm(d, c):  # Eye of the Storm: cloud + bolt
    for box in ([50, 80, 130, 150], [96, 56, 180, 140], [140, 86, 210, 150]):
        d.ellipse(box, fill=c)
    d.rectangle([76, 120, 186, 150], fill=c)
    d.polygon([(130, 150), (104, 196), (126, 196), (112, 230), (160, 180), (138, 180), (152, 150)], fill=(255, 245, 160))


def i_parry(d, c):  # Buckler parry: round buckler + spark
    d.ellipse([56, 70, 176, 190], fill=c)
    d.ellipse([100, 114, 132, 146], fill=BG)
    d.polygon(star_pts(176, 76, 4, 46, 12, rot=-90), fill=(255, 245, 180))


def i_heart(d, c):  # Bless Thy Sinners recovery
    d.ellipse([52, 64, 132, 144], fill=c)
    d.ellipse([124, 64, 204, 144], fill=c)
    d.polygon([(56, 120), (200, 120), (128, 210)], fill=c)
    d.rectangle([120, 88, 136, 140], fill=(255, 240, 245))
    d.rectangle([102, 106, 154, 122], fill=(255, 240, 245))


def i_wind(d, c):  # Tailwind: swirl lines
    d.arc([40, 60, 190, 150], 180, 360, fill=c, width=14)
    d.line([(40, 105), (40, 105)], fill=c, width=14)
    d.arc([70, 110, 210, 200], 180, 360, fill=c, width=14)
    d.line([(60, 172), (140, 172)], fill=c, width=14)
    d.line([(30, 130), (110, 130)], fill=c, width=14)


def i_portal(d, c):  # Phase Flow
    d.ellipse([60, 40, 196, 216], outline=c, width=16)
    d.arc([92, 80, 164, 176], 0, 270, fill=c, width=12)
    d.ellipse([118, 118, 138, 138], fill=(240, 220, 255))


def i_purity(d, c):  # Ray of Hope debuff immunity: holy shield with a cross
    shield(d, c, 128, 128, 84, 104)
    d.rectangle([118, 70, 138, 186], fill=(255, 252, 235))
    d.rectangle([90, 104, 166, 124], fill=(255, 252, 235))


def i_labor(d, c):  # Battlecry gathering bonus: crossed pickaxe and axe
    d.line([(70, 200), (186, 70)], fill=(120, 80, 45), width=16)
    d.line([(186, 200), (70, 70)], fill=(120, 80, 45), width=16)
    d.polygon([(150, 52), (214, 84), (196, 100), (160, 84), (128, 92)], fill=c)
    d.polygon([(50, 58), (98, 70), (92, 112), (56, 104)], fill=c)


ICONS = [
    ("hyper_armor", (255, 140, 40), i_hyper), ("barrier", (255, 214, 60), i_barrier),
    ("defense", (120, 160, 220), i_defense), ("damage", (235, 60, 50), i_damage),
    ("haste", (110, 230, 100), i_haste), ("attack_speed", (255, 170, 90), i_attack_speed),
    ("stamina", (250, 230, 70), i_stamina), ("eitr", (170, 110, 255), i_eitr),
    ("clockwork", (205, 150, 80), i_clockwork), ("overcharge", (240, 90, 240), i_overcharge),
    ("fury", (200, 30, 30), i_fury), ("focus", (190, 255, 80), i_focus),
    ("vigil", (255, 70, 110), i_vigil), ("generic", (235, 235, 235), i_generic),
    ("sun", (255, 200, 90), i_sun), ("ray", (255, 160, 190), i_ray),
    ("wing_boot", (110, 190, 255), i_wing_boot), ("horn", (230, 110, 40), i_horn),
    ("relic", (90, 220, 200), i_relic), ("wings", (235, 245, 255), i_wings),
    ("storm", (80, 140, 255), i_storm), ("parry", (190, 200, 215), i_parry),
    ("heart", (250, 110, 150), i_heart), ("wind", (150, 245, 210), i_wind),
    ("portal", (130, 80, 255), i_portal), ("purity", (255, 226, 140), i_purity),
    ("labor", (210, 170, 110), i_labor),
]


def make(name, color, draw):
    im, d = badge(color)
    d._image = im
    draw(d, color)
    im = im.resize((64, 64), Image.LANCZOS)
    im.save(os.path.join(OUT, "Buff_" + name + ".png"))
    return im


if __name__ == "__main__":
    sheet = Image.new("RGBA", (72 * 9, 72 * 3), (40, 40, 40, 255))
    for i, (name, color, fn) in enumerate(ICONS):
        sheet.alpha_composite(make(name, color, fn), (4 + (i % 9) * 72, 4 + (i // 9) * 72))
    sheet.save(os.path.join(ROOT, "docs", "previews", "buff_icons_v0256.png"))
    print("wrote %d buff icons" % len(ICONS))
