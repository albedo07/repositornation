"""v0.24.3 buff indicator icons (Buff_<id>.png, 64 px) shown by DragonCombat.ShowStatus as vanilla
status effects under the minimap. Plain, readable glyphs on a dark round badge (vanilla-like).
Usage: python3 tools/build_buff_icons.py
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "ImmortalHeroesAssets")
K = 4
S = 64 * K


def badge(color):
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse([6, 6, S - 6, S - 6], fill=(18, 20, 26, 235), outline=color + (255,), width=10)
    return im, d


def shield(d, c, cx=128, cy=124, w=80, h=100):
    d.polygon([(cx - w, cy - h * 0.7), (cx, cy - h), (cx + w, cy - h * 0.7), (cx + w * 0.8, cy + h * 0.25), (cx, cy + h), (cx - w * 0.8, cy + h * 0.25)], fill=c)


def arrow_up(d, c, cx, cy, s):
    d.polygon([(cx, cy - s), (cx + s * 0.7, cy - s * 0.1), (cx + s * 0.25, cy - s * 0.1), (cx + s * 0.25, cy + s), (cx - s * 0.25, cy + s), (cx - s * 0.25, cy - s * 0.1), (cx - s * 0.7, cy - s * 0.1)], fill=c)


def make(name, color, draw):
    im, d = badge(color)
    draw(d, color)
    im = im.resize((64, 64), Image.LANCZOS)
    im.save(os.path.join(OUT, "Buff_" + name + ".png"))


GOLD = (240, 200, 90)
CYAN = (110, 220, 255)
RED = (235, 70, 60)
GREEN = (120, 230, 110)
PURPLE = (190, 120, 255)
WHITE = (235, 235, 235)


def hyper(d, c):
    shield(d, c)
    d.rectangle([116, 70, 140, 180], fill=(18, 20, 26))


def barrier(d, c):
    d.ellipse([46, 46, 210, 210], outline=c, width=20)
    d.ellipse([86, 86, 170, 170], fill=c + (120,))


def defense(d, c):
    shield(d, c)


def damage(d, c):
    d.polygon([(128, 40), (148, 70), (140, 170), (116, 170), (108, 70)], fill=c)
    d.rectangle([84, 170, 172, 186], fill=c)
    d.rectangle([118, 186, 138, 222], fill=c)


def haste(d, c):
    for i in range(3):
        x = 60 + i * 48
        d.polygon([(x, 70), (x + 50, 128), (x, 186), (x + 22, 186), (x + 72, 128), (x + 22, 70)], fill=c)


def attack_speed(d, c):
    damage(d, c)
    d.arc([40, 40, 216, 216], 200, 340, fill=c, width=12)


def stamina(d, c):
    d.polygon([(140, 36), (80, 140), (122, 140), (106, 220), (178, 108), (134, 108), (160, 36)], fill=c)


def eitr(d, c):
    d.polygon([(128, 44), (176, 140), (80, 140)], fill=c)
    d.ellipse([80, 104, 176, 200], fill=c)


def clockwork(d, c):
    d.ellipse([56, 56, 200, 200], outline=c, width=14)
    d.line([(128, 128), (128, 76)], fill=c, width=14)
    d.line([(128, 128), (166, 150)], fill=c, width=14)


def overcharge(d, c):
    stamina(d, c)
    d.ellipse([50, 50, 206, 206], outline=c, width=8)


def fury(d, c):
    d.polygon([(128, 34), (176, 120), (162, 214), (128, 186), (94, 214), (80, 120)], fill=c)
    d.polygon([(128, 110), (150, 160), (128, 200), (106, 160)], fill=(255, 220, 120))


def focus(d, c):
    d.ellipse([60, 60, 196, 196], outline=c, width=12)
    d.ellipse([112, 112, 144, 144], fill=c)
    for a in range(4):
        r = math.radians(a * 90)
        d.line([(128 + math.cos(r) * 52, 128 + math.sin(r) * 52), (128 + math.cos(r) * 92, 128 + math.sin(r) * 92)], fill=c, width=12)


def vigil(d, c):
    d.ellipse([46, 86, 210, 170], outline=c, width=14)
    d.ellipse([102, 102, 154, 154], fill=c)


def generic(d, c):
    pts = []
    for i in range(10):
        r = 80 if i % 2 == 0 else 34
        a = math.radians(-90 + i * 36)
        pts.append((128 + math.cos(a) * r, 132 + math.sin(a) * r))
    d.polygon(pts, fill=c)


ICONS = [("hyper_armor", GOLD, hyper), ("barrier", CYAN, barrier), ("defense", CYAN, defense), ("damage", RED, damage),
         ("haste", GREEN, haste), ("attack_speed", (255, 160, 80), attack_speed), ("stamina", (250, 220, 80), stamina),
         ("eitr", PURPLE, eitr), ("clockwork", GOLD, clockwork), ("overcharge", PURPLE, overcharge), ("fury", RED, fury),
         ("focus", GREEN, focus), ("vigil", RED, vigil), ("generic", WHITE, generic)]

if __name__ == "__main__":
    for name, color, fn in ICONS:
        make(name, color, fn)
    print("wrote %d buff icons" % len(ICONS))
