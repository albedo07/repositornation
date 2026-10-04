"""v0.24.2 Ranger / Acrobat / Bowmaster skill paintings (21 icons, 192 px, borderless like the Art
Refresh pack). Each icon is a crop of the user's painted Ranger scenes (docs/source_art/ranger_kali/
backgrounds) pushed into the pack's look (dark navy vignette, lit subject) plus painted-light effects
(arrow streaks, wind, leaves, fire, stars) rendered at 4x with additive glow.
Output: docs/source_art/ranger_kali/icons/Icon_<id>.png (read by tools/build_class_art.py, which
grades them per Class and frames them like every other icon).
Usage: python3 tools/build_ranger_icons.py [--preview FILE]
"""
import math
import os
import random
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "source_art", "ranger_kali", "backgrounds")
OUT = os.path.join(ROOT, "docs", "source_art", "ranger_kali", "icons")
S = 768          # working size (4x of 192)
NAVY = np.array([10, 16, 30], np.float64)

GREEN = (150, 255, 120)
TEAL = (120, 255, 225)
GOLD = (255, 214, 110)
WHITE = (240, 255, 235)
FIRE = (255, 140, 50)


def scene(name):
    return Image.open(os.path.join(SRC, name + ".jpg")).convert("RGB")


def base(name, x, y, size, rot=0.0, flip=False, vignette=0.88, focus=(0.5, 0.45)):
    sc = scene(name)
    if rot:
        # Rotate a larger region, then take the centre: no empty corners.
        m = int(size * 0.25)
        big = sc.crop((x - m, y - m, x + size + m, y + size + m)).rotate(rot, resample=Image.BICUBIC)
        im = big.crop((m, m, m + size, m + size)).resize((S, S), Image.LANCZOS)
    else:
        im = sc.crop((x, y, x + size, y + size)).resize((S, S), Image.LANCZOS)
    if flip:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
    im = ImageEnhance.Contrast(im).enhance(1.15)
    a = np.asarray(im, np.float64)
    yy, xx = np.mgrid[0:S, 0:S] / float(S)
    d = np.hypot(xx - focus[0], (yy - focus[1]) * 1.1)
    k = np.clip((d - 0.22) / 0.48, 0, 1)[..., None] * vignette
    a = (a * (1 - k) + NAVY * k) * 0.82
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def dark(size_hint=None):
    a = np.zeros((S, S, 3)) + NAVY
    return Image.fromarray(a.astype(np.uint8))


class Light(object):
    """Additive light layer: draw shapes, then glow-composite onto the base."""

    def __init__(self):
        self.img = Image.new("RGB", (S, S), (0, 0, 0))
        self.d = ImageDraw.Draw(self.img)

    def line(self, pts, color, width):
        self.d.line(pts, fill=color, width=int(width), joint="curve")

    def poly(self, pts, color):
        self.d.polygon(pts, fill=color)

    def ellipse(self, box, color, width=0):
        if width:
            self.d.ellipse(box, outline=color, width=int(width))
        else:
            self.d.ellipse(box, fill=color)

    def apply(self, im, glow=1.0, core=1.0):
        L = np.asarray(self.img, np.float64)
        g1 = np.asarray(self.img.filter(ImageFilter.GaussianBlur(10)), np.float64)
        g2 = np.asarray(self.img.filter(ImageFilter.GaussianBlur(34)), np.float64)
        b = np.asarray(im, np.float64)
        out = b + L * core + g1 * 1.6 * glow + g2 * 1.4 * glow
        return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def arrow(L, x0, y0, x1, y1, color, w=10, head=1.0, trail=True):
    dx, dy = x1 - x0, y1 - y0
    n = math.hypot(dx, dy) or 1.0
    ux, uy = dx / n, dy / n
    px, py = -uy, ux
    if trail:
        steps = 14
        for i in range(steps):
            t0, t1 = i / float(steps), (i + 1) / float(steps)
            c = tuple(int(v * (0.25 + 0.75 * t1)) for v in color)
            L.line([(x0 + dx * t0, y0 + dy * t0), (x0 + dx * t1, y0 + dy * t1)], c, max(2, w * (0.35 + 0.65 * t1)))
    else:
        L.line([(x0, y0), (x1, y1)], color, w)
    h = 34 * head
    L.poly([(x1 + ux * h * 0.9, y1 + uy * h * 0.9), (x1 - ux * h * 0.3 + px * h * 0.45, y1 - uy * h * 0.3 + py * h * 0.45),
            (x1 - ux * h * 0.3 - px * h * 0.45, y1 - uy * h * 0.3 - py * h * 0.45)], color)
    # fletching
    fx, fy = x0 + ux * 26, y0 + uy * 26
    for s in (1, -1):
        L.line([(fx, fy), (fx - ux * 30 + px * 18 * s, fy - uy * 30 + py * 18 * s)], tuple(int(v * 0.6) for v in color), 5)


def spiral(L, cx, cy, r0, r1, turns, color, w, phase=0.0, squash=1.0):
    pts = []
    n = int(140 * turns)
    for i in range(n + 1):
        t = i / float(n)
        a = phase + t * turns * 2 * math.pi
        r = r0 + (r1 - r0) * t
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r * squash))
    for i in range(len(pts) - 1):
        t = i / float(len(pts))
        c = tuple(int(v * (0.2 + 0.8 * t)) for v in color)
        L.line([pts[i], pts[i + 1]], c, max(2, w * (0.3 + 0.7 * t)))


def leaves(L, cx, cy, rmin, rmax, count, color, seed, size=26, squash=1.0):
    rnd = random.Random(seed)
    for _ in range(count):
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(rmin, rmax)
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r * squash
        s = size * rnd.uniform(0.6, 1.3)
        ang = a + math.pi / 2 + rnd.uniform(-0.6, 0.6)
        ux, uy = math.cos(ang), math.sin(ang)
        px, py = -uy, ux
        pts = [(x + ux * s, y + uy * s), (x + px * s * 0.42, y + py * s * 0.42), (x - ux * s, y - uy * s), (x - px * s * 0.42, y - py * s * 0.42)]
        c = tuple(int(v * rnd.uniform(0.55, 1.0)) for v in color)
        L.poly(pts, c)


def streaks(L, cx, cy, ang, count, length, spread, color, seed, w=4):
    rnd = random.Random(seed)
    ux, uy = math.cos(ang), math.sin(ang)
    px, py = -uy, ux
    for _ in range(count):
        o = rnd.uniform(-spread, spread)
        b = rnd.uniform(-length * 0.5, length * 0.5)
        l = length * rnd.uniform(0.4, 1.0)
        x, y = cx + px * o + ux * b, cy + py * o + uy * b
        c = tuple(int(v * rnd.uniform(0.35, 0.9)) for v in color)
        L.line([(x, y), (x - ux * l, y - uy * l)], c, w)


def star(L, x, y, r, color):
    L.poly([(x, y - r), (x + r * 0.18, y - r * 0.18), (x + r, y), (x + r * 0.18, y + r * 0.18), (x, y + r),
            (x - r * 0.18, y + r * 0.18), (x - r, y), (x - r * 0.18, y - r * 0.18)], color)


def ring(L, cx, cy, r, color, w, squash=0.35):
    L.ellipse((cx - r, cy - r * squash, cx + r, cy + r * squash), color, w)


# ----------------------------------------------------------------------------------------------
def piercing_arrow():
    im = base("bowmaster", 60, 30, 400, focus=(0.4, 0.4))
    L = Light()
    arrow(L, 60, 330, 760, 300, WHITE, w=12, head=1.4)
    for i, x in enumerate((420, 540, 650)):
        ring(L, x, 312 - i * 4, 60 - i * 10, GREEN, 6, squash=1.6)
    streaks(L, 500, 310, math.radians(-2), 26, 260, 40, GREEN, 1)
    return L.apply(im)


def tumble_shot():
    im = base("acrobat", 60, 0, 470, rot=28, focus=(0.45, 0.42))
    L = Light()
    for a in (-14, 0, 14):
        r = math.radians(a - 8)
        arrow(L, 330, 420, 330 + math.cos(r) * 430, 420 + math.sin(r) * 430, GREEN, w=8)
    spiral(L, 300, 380, 120, 300, 0.7, TEAL, 10, phase=math.radians(110))
    return L.apply(im)


def snare_trap():
    im = base("ranger", 330, 300, 210, focus=(0.5, 0.6), vignette=0.6)
    L = Light()
    ring(L, 384, 500, 250, GREEN, 10)
    ring(L, 384, 500, 170, (110, 200, 90), 6)
    for i in range(14):
        a = i / 14.0 * 2 * math.pi
        x, y = 384 + math.cos(a) * 250, 500 + math.sin(a) * 250 * 0.35
        L.poly([(x - 14, y), (x + 14, y), (x + math.cos(a) * -30, y - 70)], (200, 255, 150))
    leaves(L, 384, 420, 40, 300, 22, GREEN, 3, squash=0.5)
    return L.apply(im)


def gale_volley():
    im = base("acrobat", 140, 10, 380, focus=(0.45, 0.4))
    L = Light()
    for a in range(-30, 31, 10):
        r = math.radians(a)
        arrow(L, 420, 520, 420 + math.cos(r) * 360, 520 + math.sin(r) * 360 - 80, TEAL, w=7, head=0.8)
    spiral(L, 260, 520, 60, 260, 0.6, TEAL, 8, phase=math.radians(200), squash=0.5)
    return L.apply(im)


def cyclone_arrow():
    im = base("acrobat", 640, 140, 300, vignette=0.85)
    L = Light()
    for k in range(5):
        spiral(L, 384, 384, 20, 330 - k * 30, 2.2, TEAL, 10 - k, phase=k * 1.25, squash=0.9)
    arrow(L, 120, 420, 650, 350, WHITE, w=10, head=1.2)
    leaves(L, 384, 384, 120, 330, 18, (150, 255, 200), 5)
    return L.apply(im)


def swallow_dive():
    im = base("acrobat", 70, 30, 430, rot=-8, focus=(0.45, 0.45))
    L = Light()
    streaks(L, 420, 400, math.radians(-12), 60, 420, 260, TEAL, 7, w=5)
    L.line([(40, 560), (740, 300)], (210, 255, 240), 10)
    return L.apply(im, glow=1.1)


def skyfall_barrage():
    im = base("acrobat", 620, 0, 340, vignette=0.7, focus=(0.5, 0.6))
    L = Light()
    ring(L, 384, 640, 300, TEAL, 8)
    rnd = random.Random(11)
    for _ in range(16):
        x = rnd.uniform(110, 660)
        y1 = rnd.uniform(450, 690)
        arrow(L, x - 110, y1 - 380, x, y1, (170, 255, 230), w=5, head=0.7)
    return L.apply(im)


def ricochet_arrow():
    im = base("acrobat", 520, 160, 300, vignette=0.85)
    L = Light()
    pts = [(70, 620), (300, 200), (450, 560), (640, 170)]
    for i in range(len(pts) - 1):
        L.line([pts[i], pts[i + 1]], TEAL, 9)
    for p in pts[1:-1]:
        star(L, p[0], p[1], 70, WHITE)
    arrow(L, 560, 330, 690, 120, WHITE, w=10, head=1.2, trail=False)
    return L.apply(im)


def furious_winds():
    im = base("acrobat", 110, 0, 460, focus=(0.47, 0.42))
    L = Light()
    for k in range(4):
        spiral(L, 384, 420, 230 + k * 25, 330 + k * 15, 0.85, (110, 255, 160), 9 - k, phase=k * 1.6, squash=0.55)
    leaves(L, 384, 420, 200, 360, 70, (150, 255, 120), 9, squash=0.6)
    leaves(L, 384, 420, 150, 340, 30, (255, 230, 120), 10, size=18, squash=0.6)
    return L.apply(im)


def tailwind():
    im = base("acrobat", 650, 0, 300, vignette=0.85, focus=(0.5, 0.4))
    L = Light()
    for k in range(3):
        spiral(L, 384, 520 - k * 130, 40, 230 - k * 40, 1.0, GOLD, 10 - k * 2, phase=k * 2.0, squash=0.35)
    leaves(L, 384, 380, 60, 300, 26, (255, 236, 150), 13, size=22)
    for x in (250, 384, 520):
        L.line([(x, 680), (x + 20, 120)], (255, 220, 140), 4)
    return L.apply(im)


def ballista_shot():
    im = base("bowmaster", 40, 40, 420, focus=(0.38, 0.4))
    L = Light()
    L.line([(150, 270), (768, 250)], (200, 255, 170), 54)
    L.line([(150, 270), (768, 250)], WHITE, 18)
    for x in (430, 560, 690):
        ring(L, x, 258, 90, GREEN, 8, squash=1.5)
    return L.apply(im)


def arrow_rain():
    im = base("bowmaster", 600, 0, 360, vignette=0.75, focus=(0.5, 0.5))
    L = Light()
    rnd = random.Random(21)
    for _ in range(26):
        x = rnd.uniform(40, 740)
        y1 = rnd.uniform(380, 740)
        arrow(L, x + 60, y1 - 330, x, y1, GREEN, w=5, head=0.65)
    ring(L, 384, 690, 330, (120, 220, 100), 6)
    return L.apply(im)


def pinning_shot():
    im = base("ranger", 360, 300, 210, vignette=0.75, focus=(0.5, 0.55))
    L = Light()
    ring(L, 384, 600, 200, GREEN, 10)
    ring(L, 384, 600, 120, WHITE, 6)
    arrow(L, 160, 60, 384, 590, WHITE, w=14, head=1.6)
    for a in range(0, 360, 45):
        r = math.radians(a)
        L.line([(384 + math.cos(r) * 130, 600 + math.sin(r) * 45), (384 + math.cos(r) * 300, 600 + math.sin(r) * 105)], (130, 230, 110), 5)
    return L.apply(im)


def explosive_arrow():
    im = base("bowmaster", 560, 180, 320, vignette=0.85)
    L = Light()
    rnd = random.Random(31)
    cx, cy = 470, 330
    for _ in range(46):
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(120, 330)
        L.line([(cx, cy), (cx + math.cos(a) * r, cy + math.sin(a) * r)], (255, int(rnd.uniform(90, 200)), 40), int(rnd.uniform(4, 12)))
    L.ellipse((cx - 70, cy - 70, cx + 70, cy + 70), (230, 150, 70))
    arrow(L, 60, 600, cx - 60, cy + 50, FIRE, w=10, head=1.2)
    return L.apply(im)


def splitting_arrow():
    im = base("bowmaster", 40, 60, 380, focus=(0.35, 0.42))
    L = Light()
    for a in (-60, -30, 0, 30, 60):
        r = math.radians(a * 0.55)
        arrow(L, 330, 330, 330 + math.cos(r) * 430, 330 + math.sin(r) * 430, GREEN, w=8, head=0.9)
    return L.apply(im)


def starfall_volley():
    im = base("bowmaster", 440, 0, 330, vignette=0.8, focus=(0.5, 0.5))
    a = np.asarray(im, np.float64) * 0.65
    im = Image.fromarray(a.astype(np.uint8))
    L = Light()
    rnd = random.Random(41)
    for _ in range(7):
        x = rnd.uniform(100, 680)
        y1 = rnd.uniform(420, 720)
        arrow(L, x - 150, y1 - 420, x, y1, (220, 255, 190), w=14, head=1.4)
        star(L, x, y1, 60, WHITE)
    for _ in range(30):
        star(L, rnd.uniform(0, S), rnd.uniform(0, 300), rnd.uniform(6, 16), (255, 250, 220))
    return L.apply(im)


def hawks_vigil():
    im = base("bowmaster", 150, 40, 200, focus=(0.5, 0.45), vignette=0.8)
    L = Light()
    # Hawk silhouette in light above, and a golden sight ring.
    ring(L, 384, 384, 300, GOLD, 8, squash=1.0)
    ring(L, 384, 384, 210, (255, 200, 90), 4, squash=1.0)
    for s in (1, -1):
        L.poly([(384, 130), (384 + s * 210, 70), (384 + s * 170, 110), (384 + s * 230, 120), (384 + s * 140, 150), (384 + s * 40, 170)], (200, 160, 70))
    L.poly([(374, 130), (394, 130), (402, 200), (384, 220), (366, 200)], (220, 190, 110))
    return L.apply(im, glow=0.9)


ICONS = ["piercing_arrow", "tumble_shot", "snare_trap", "gale_volley", "cyclone_arrow", "swallow_dive", "skyfall_barrage",
         "ricochet_arrow", "furious_winds", "tailwind", "ballista_shot", "arrow_rain", "pinning_shot", "explosive_arrow",
         "splitting_arrow", "starfall_volley", "hawks_vigil"]


def main():
    os.makedirs(OUT, exist_ok=True)
    made = []
    for sid in ICONS:
        im = globals()[sid]().resize((192, 192), Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=1.0, percent=50, threshold=2))
        im.save(os.path.join(OUT, "Icon_" + sid + ".png"))
        made.append(im)
    if "--preview" in sys.argv:
        sheet = Image.new("RGB", (6 * 196, 3 * 196), (0, 0, 0))
        for i, im in enumerate(made):
            sheet.paste(im, ((i % 6) * 196, (i // 6) * 196))
        sheet.save(sys.argv[sys.argv.index("--preview") + 1])
    print("wrote %d Ranger icons" % len(made))


if __name__ == "__main__":
    main()
