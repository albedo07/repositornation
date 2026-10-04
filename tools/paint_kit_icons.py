"""v0.23.8: skill icon art for every kit without a painting (Sword Master, Mercenary, Archmage,
Horizon Walker). Same style as the painted Cleric icons and tools/paint_skill_icons.py: a pale glowing
glyph on a radial field with soft rays, tinted by the node's category colour (Cyan / Navy / Green /
Gold / Maroon), with that colour's bottom bar strip.
Output: ImmortalHeroesAssets/<Class>_<AC>_Artwork.png (1011x662 = the chassis with only the node
openings replaced), read by Advanced.cs IhBuildKitCanvas.
Usage: python3 tools/paint_kit_icons.py [--preview out.png]
"""
import math
import os
import sys
import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from paint_skill_icons import field_background, glow_layer, composite, BAR_ROWS, S  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")

# Chassis openings (= Advanced.cs IhFieldRect).
FIELDS = {"lightning_zap": (175, 166, 229, 217), "righteous_strike": (175, 295, 228, 345), "holy_wave": (175, 420, 228, 469),
          "goddess_relic": (378, 166, 431, 217), "judgement_hammer": (379, 295, 433, 345), "heavens_light": (390, 418, 444, 469),
          "shield_charge": (553, 166, 605, 217), "fallen_angel": (690, 166, 743, 217), "ray_of_hope": (691, 295, 743, 345),
          "electric_smite": (863, 227, 952, 314)}
GRACE_INNER = (393, 421, 442, 467)      # dark opening inside the painted gold bevel (v0.23.6)
FOOTER_INNER = (671, 551, 712, 590)     # footer Grace box opening
# Donor field per category colour (Cleric_Paladin_Reference): background tones + bottom bar strip.
DONOR = {"cyan": "shield_charge", "navy": "goddess_relic", "green": "ray_of_hope", "gold": "heavens_light", "maroon": "electric_smite"}

KITS = [
    ("Warrior", "SwordMaster", ["heavy_slash", "impact_wave", "impact_punch"],
     ["moonlight_splitter", "crescent_cleave", "blade_storm", "frenzied_charge", "eclipse"], "halfmoon_slash", "knights_guidance", ["moonlight_splitter", "crescent_cleave"]),
    ("Warrior", "Mercenary", ["heavy_slash", "impact_wave", "impact_punch"],
     ["stomp", "circle_swing", "bonecrusher", "seismic_guillotine", "punishing_bomb"], "whirlwind", "battlecry", ["stomp", "circle_swing"]),
    ("Sorcerer", "Wizard", ["flame_burst", "glacial_descent", "stonefang_eruption"],
     ["meteor_fall", "gravity_dominion", "astral_railcannon", "astral_greatblade", "frost_nova"], "elemental_cataclysm", "clockwork", ["meteor_fall", "gravity_dominion"]),
    ("Sorcerer", "Spellcaster", ["flame_burst", "glacial_descent", "stonefang_eruption"],
     ["arcane_phalanx", "afterimage_arsenal", "void_step", "rift_echo", "gravity_blast"], "arcane_rupture", "rift_walker", ["arcane_phalanx", "afterimage_arsenal"]),
]
CLASS_SLOTS = ["lightning_zap", "righteous_strike", "holy_wave"]
ADV_SLOTS = ["goddess_relic", "judgement_hammer", "shield_charge", "fallen_angel", "ray_of_hope"]


# ----------------------------------------------------------------------------------------------
# Glyph primitives (normalised 0..1 coordinates, drawn on an L mask; 255 = solid glyph).
# ----------------------------------------------------------------------------------------------
class G(object):
    K = 1.45  # stroke weight (the painted Cleric glyphs are bold)

    def __init__(self, d, w, h):
        self.d, self.w, self.h = d, w, h

    def p(self, x, y):
        return (x * self.w, y * self.h)

    def poly(self, pts, v=255):
        self.d.polygon([self.p(x, y) for x, y in pts], fill=v)

    def ell(self, cx, cy, rx, ry, v=255):
        self.d.ellipse([self.p(cx - rx, cy - ry), self.p(cx + rx, cy + ry)], fill=v)

    def ring(self, cx, cy, rx, ry, t, v=255):
        self.d.ellipse([self.p(cx - rx, cy - ry), self.p(cx + rx, cy + ry)], outline=v, width=max(1, int(t * self.K * self.w)))

    def line(self, pts, t, v=255):
        self.d.line([self.p(x, y) for x, y in pts], fill=v, width=max(1, int(t * self.K * self.w)), joint="curve")

    def arc(self, cx, cy, rx, ry, a0, a1, t, v=255):
        self.d.arc([self.p(cx - rx, cy - ry), self.p(cx + rx, cy + ry)], a0, a1, fill=v, width=max(1, int(t * self.K * self.w)))

    def crescent(self, cx, cy, r, ox, oy, v=255, cut=0):
        self.ell(cx, cy, r, r, v)
        self.ell(cx + ox, cy + oy, r * 0.92, r * 0.92, cut)

    def star(self, cx, cy, r_out, r_in, n=4, v=255, rot=-90):
        pts = []
        for i in range(n * 2):
            r = r_out if i % 2 == 0 else r_in
            a = math.radians(rot + i * 180.0 / n)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        self.poly(pts, v)

    def blade(self, x0, y0, x1, y1, w, guard=True, v=255, hilt=0.16):
        """Sword from hilt end (x0,y0) to tip (x1,y1)."""
        dx, dy = x1 - x0, y1 - y0
        L = math.hypot(dx, dy)
        ux, uy = dx / L, dy / L
        nx, ny = -uy, ux
        hx, hy = x0 + ux * L * hilt, y0 + uy * L * hilt          # guard position
        tip_base = 0.86
        bx, by = x0 + ux * L * tip_base, y0 + uy * L * tip_base
        self.poly([(hx + nx * w, hy + ny * w), (bx + nx * w, by + ny * w), (x1, y1), (bx - nx * w, by - ny * w), (hx - nx * w, hy - ny * w)], v)
        # fuller
        self.line([(hx + ux * L * 0.05, hy + uy * L * 0.05), (bx - ux * L * 0.08, by - uy * L * 0.08)], w * 0.45, int(v * 0.62))
        if guard:
            g = w * 3.2
            self.line([(hx + nx * g, hy + ny * g), (hx - nx * g, hy - ny * g)], w * 0.9, v)
            self.line([(x0, y0), (hx, hy)], w * 0.85, int(v * 0.85))
            self.ell(x0, y0, w * 0.95, w * 0.95, v)

    def comet(self, hx, hy, r, tx, ty, v=255):
        """Fireball at (hx,hy) with a tapered tail to (tx,ty)."""
        a = math.atan2(hy - ty, hx - tx)
        n = a + math.pi / 2
        self.poly([(tx, ty), (hx + r * math.cos(n), hy + r * math.sin(n)), (hx - r * math.cos(n), hy - r * math.sin(n))], int(v * 0.75))
        self.ell(hx, hy, r, r, v)
        self.ell(hx - r * 0.3 * math.cos(a), hy - r * 0.3 * math.sin(a), r * 0.4, r * 0.4, int(v * 0.6))

    def figure(self, cx, cy, s, v=255):
        """Hooded caster silhouette."""
        self.ell(cx, cy - 0.30 * s, 0.11 * s, 0.12 * s, v)
        self.poly([(cx - 0.10 * s, cy - 0.20 * s), (cx + 0.10 * s, cy - 0.20 * s), (cx + 0.26 * s, cy + 0.40 * s), (cx - 0.26 * s, cy + 0.40 * s)], v)


# ----------------------------------------------------------------------------------------------
# One painter per skill.
# ----------------------------------------------------------------------------------------------
def heavy_slash(g):
    g.arc(0.50, 0.55, 0.40, 0.40, 200, 330, 0.035, 150)
    g.arc(0.50, 0.55, 0.34, 0.34, 205, 325, 0.02, 110)
    g.blade(0.22, 0.86, 0.82, 0.16, 0.055)


def impact_wave(g):
    # Ground shockwave driven forward: bold chevrons over a cracked ground line.
    g.line([(0.04, 0.84), (0.96, 0.84)], 0.03, 220)
    for i, x in enumerate((0.18, 0.42, 0.66)):
        v = 150 + i * 52
        g.poly([(x, 0.22), (x + 0.16, 0.22), (x + 0.32, 0.52), (x + 0.16, 0.82), (x, 0.82), (x + 0.16, 0.52)], v)
    g.line([(0.30, 0.84), (0.36, 0.92), (0.46, 0.90)], 0.02, 200)


def impact_punch(g):
    # Gauntlet fist seen from the front: four knuckles, thumb, cuff; impact lines.
    for i in range(4):
        g.ell(0.34 + i * 0.105, 0.38, 0.06, 0.07, 255)
    g.poly([(0.27, 0.38), (0.72, 0.38), (0.72, 0.62), (0.62, 0.70), (0.30, 0.70), (0.27, 0.60)], 255)
    g.poly([(0.27, 0.48), (0.50, 0.50), (0.52, 0.58), (0.30, 0.60)], 150)
    g.poly([(0.33, 0.70), (0.64, 0.70), (0.66, 0.86), (0.31, 0.86)], 220)
    g.line([(0.33, 0.76), (0.65, 0.76)], 0.02, 140)
    for a in (-150, -120, -90, -60, -30):
        r = math.radians(a)
        g.line([(0.50 + 0.36 * math.cos(r), 0.40 + 0.33 * math.sin(r)), (0.50 + 0.46 * math.cos(r), 0.40 + 0.42 * math.sin(r))], 0.03, 230)


def moonlight_splitter(g):
    g.crescent(0.46, 0.42, 0.30, 0.12, -0.06, 210)
    g.blade(0.62, 0.92, 0.62, 0.08, 0.05)


def crescent_cleave(g):
    g.crescent(0.52, 0.50, 0.38, -0.14, 0.0, 255)
    g.crescent(0.52, 0.50, 0.30, -0.12, 0.0, 110)
    g.star(0.80, 0.24, 0.08, 0.02, 4, 255)


def blade_storm(g):
    for i in range(6):
        a = math.radians(-90 + i * 60 + 15)
        cx, cy = 0.5, 0.48
        x0, y0 = cx + 0.10 * math.cos(a), cy + 0.10 * math.sin(a)
        x1, y1 = cx + 0.47 * math.cos(a + 0.35), cy + 0.47 * math.sin(a + 0.35)
        g.blade(x0, y0, x1, y1, 0.04, guard=False, v=235, hilt=0.0)
    g.ring(0.5, 0.48, 0.20, 0.20, 0.02, 160)
    g.star(0.5, 0.48, 0.09, 0.03, 4, 255)


def frenzied_charge(g):
    g.blade(0.18, 0.62, 0.90, 0.40, 0.05)
    for i, yy in enumerate((0.26, 0.46, 0.66, 0.84)):
        g.line([(0.06 + 0.04 * i, yy), (0.34 + 0.02 * i, yy - 0.06)], 0.03, 220 - i * 25)


def eclipse(g):
    for i in range(16):
        a = math.radians(i * 22.5)
        r1 = 0.46 if i % 2 == 0 else 0.40
        g.poly([(0.5 + 0.30 * math.cos(a - 0.10), 0.5 + 0.30 * math.sin(a - 0.10)), (0.5 + r1 * math.cos(a), 0.5 + r1 * math.sin(a)),
                (0.5 + 0.30 * math.cos(a + 0.10), 0.5 + 0.30 * math.sin(a + 0.10))], 220)
    g.ell(0.5, 0.5, 0.32, 0.32, 255)
    g.ell(0.53, 0.47, 0.27, 0.27, 40)


def halfmoon_slash(g):
    g.crescent(0.50, 0.58, 0.42, 0.0, 0.16, 255)
    g.crescent(0.50, 0.58, 0.34, 0.0, 0.14, 120)
    g.blade(0.30, 0.90, 0.72, 0.10, 0.04)
    g.star(0.80, 0.22, 0.07, 0.02, 4, 255)


def knights_guidance(g):
    # Compass star with an upward guiding arrow.
    g.ring(0.5, 0.50, 0.32, 0.32, 0.025, 170)
    g.star(0.5, 0.50, 0.40, 0.07, 4, 255)
    g.star(0.5, 0.50, 0.22, 0.05, 4, 200, rot=-45)
    g.ell(0.5, 0.50, 0.05, 0.05, 120)


def stomp(g):
    g.line([(0.08, 0.80), (0.92, 0.80)], 0.035, 230)
    g.poly([(0.36, 0.10), (0.64, 0.10), (0.64, 0.42), (0.78, 0.42), (0.50, 0.74), (0.22, 0.42), (0.36, 0.42)], 255)
    for sx in (-1, 1):
        g.line([(0.5 + sx * 0.10, 0.80), (0.5 + sx * 0.18, 0.88), (0.5 + sx * 0.26, 0.86)], 0.025, 220)
        for i, r in enumerate((0.30, 0.40)):
            g.arc(0.5, 0.80, r, r * 0.5, 180 if sx < 0 else 300, 240 if sx < 0 else 360, 0.03, 200 - i * 50)


def circle_swing(g):
    g.arc(0.5, 0.5, 0.40, 0.40, 0, 300, 0.035, 200)
    g.poly([(0.88, 0.40), (0.98, 0.52), (0.82, 0.54)], 230)
    # Axe across the circle.
    g.line([(0.28, 0.78), (0.66, 0.26)], 0.045, 235)
    g.poly([(0.58, 0.20), (0.80, 0.18), (0.84, 0.36), (0.74, 0.46), (0.60, 0.34)], 255)


def bonecrusher(g):
    # Spiked mace.
    g.line([(0.22, 0.86), (0.54, 0.46)], 0.05, 230)
    g.ell(0.62, 0.36, 0.17, 0.17, 255)
    for i in range(8):
        a = math.radians(i * 45)
        cx, cy = 0.62 + 0.17 * math.cos(a), 0.36 + 0.17 * math.sin(a)
        g.poly([(cx + 0.05 * math.cos(a + 1.6), cy + 0.05 * math.sin(a + 1.6)), (cx + 0.10 * math.cos(a), cy + 0.10 * math.sin(a)),
                (cx + 0.05 * math.cos(a - 1.6), cy + 0.05 * math.sin(a - 1.6))], 255)
    g.ell(0.62, 0.36, 0.07, 0.07, 150)
    g.line([(0.20, 0.70), (0.30, 0.62), (0.27, 0.54)], 0.02, 180)


def seismic_guillotine(g):
    # Executioner's axe slamming down into the ground.
    g.line([(0.06, 0.82), (0.94, 0.82)], 0.03, 220)
    g.line([(0.58, 0.04), (0.50, 0.56)], 0.05, 230)
    g.poly([(0.52, 0.30), (0.20, 0.40), (0.14, 0.62), (0.20, 0.80), (0.50, 0.66)], 255)
    g.poly([(0.46, 0.42), (0.26, 0.48), (0.24, 0.66), (0.46, 0.60)], 150)
    for x in (0.70, 0.80, 0.90):
        g.line([(x, 0.10 + (x - 0.7)), (x, 0.40 + (x - 0.7))], 0.02, 170)
    for pts in ([(0.30, 0.82), (0.24, 0.90), (0.12, 0.92)], [(0.40, 0.82), (0.52, 0.92), (0.66, 0.90), (0.78, 0.96)]):
        g.line(pts, 0.022, 230)


def punishing_bomb(g):
    g.ell(0.45, 0.58, 0.28, 0.28, 255)
    g.ell(0.38, 0.50, 0.08, 0.07, 150)
    g.poly([(0.58, 0.26), (0.68, 0.30), (0.62, 0.40), (0.54, 0.36)], 230)
    g.line([(0.64, 0.28), (0.72, 0.18), (0.78, 0.16)], 0.025, 220)
    g.star(0.80, 0.14, 0.10, 0.025, 4, 255)


def whirlwind(g):
    for i in range(5):
        y = 0.16 + i * 0.15
        rx = 0.38 - i * 0.065
        g.arc(0.5 + (i % 2) * 0.03, y, rx, 0.06, 10, 330, 0.03, 255 - i * 18)
    g.line([(0.50, 0.80), (0.46, 0.90)], 0.03, 200)
    for s in (-1, 1):
        g.line([(0.5 + s * 0.42, 0.20), (0.5 + s * 0.47, 0.30)], 0.02, 170)


def battlecry(g):
    # War horn (bell top right) with sound waves.
    g.poly([(0.08, 0.80), (0.16, 0.86), (0.40, 0.72), (0.60, 0.52), (0.74, 0.34), (0.80, 0.16), (0.56, 0.20), (0.50, 0.40), (0.32, 0.62), (0.12, 0.74)], 255)
    g.ell(0.68, 0.18, 0.13, 0.07, 255)
    g.ell(0.68, 0.18, 0.09, 0.04, 120)
    for i, yy in enumerate((0.40, 0.56)):
        g.line([(0.30 + i * 0.16, yy + 0.18 - i * 0.04), (0.38 + i * 0.16, yy + 0.08 - i * 0.04)], 0.02, 150)
    for i in range(3):
        r = 0.10 + i * 0.09
        g.arc(0.70, 0.16, r, r, 330, 60, 0.025, 230 - i * 55)


def flame_burst(g):
    g.poly([(0.50, 0.08), (0.62, 0.30), (0.76, 0.24), (0.78, 0.50), (0.72, 0.72), (0.50, 0.86), (0.28, 0.72), (0.22, 0.50),
            (0.30, 0.30), (0.40, 0.38)], 255)
    g.poly([(0.50, 0.40), (0.60, 0.58), (0.58, 0.74), (0.50, 0.80), (0.42, 0.74), (0.40, 0.58)], 130)
    for a in (-160, -20, 200, 340):
        r = math.radians(a)
        g.star(0.5 + 0.40 * math.cos(r), 0.55 + 0.30 * math.sin(r), 0.05, 0.015, 4, 220)


def glacial_descent(g):
    g.poly([(0.50, 0.90), (0.62, 0.40), (0.56, 0.12), (0.44, 0.12), (0.38, 0.40)], 255)
    g.line([(0.50, 0.16), (0.50, 0.80)], 0.02, 150)
    g.poly([(0.30, 0.58), (0.36, 0.30), (0.26, 0.20)], 200)
    g.poly([(0.70, 0.58), (0.64, 0.30), (0.74, 0.20)], 200)
    g.line([(0.14, 0.90), (0.86, 0.90)], 0.025, 200)
    for x, y in ((0.18, 0.30), (0.84, 0.44), (0.22, 0.72)):
        g.star(x, y, 0.05, 0.015, 4, 230)


def stonefang_eruption(g):
    g.line([(0.04, 0.84), (0.96, 0.84)], 0.03, 220)
    for x, top, wdt, v in ((0.50, 0.10, 0.12, 255), (0.30, 0.34, 0.09, 230), (0.70, 0.30, 0.09, 230), (0.14, 0.56, 0.06, 200), (0.86, 0.54, 0.06, 200)):
        g.poly([(x - wdt, 0.84), (x - wdt * 0.3, top + 0.12), (x, top), (x + wdt * 0.4, top + 0.10), (x + wdt, 0.84)], v)
    for x in (0.40, 0.60):
        g.star(x, 0.22, 0.04, 0.012, 4, 230)


def meteor_fall(g):
    g.comet(0.62, 0.60, 0.20, 0.06, 0.04)
    g.line([(0.10, 0.92), (0.94, 0.92)], 0.02, 180)


def gravity_dominion(g):
    g.ell(0.5, 0.5, 0.12, 0.12, 255)
    g.ring(0.5, 0.5, 0.24, 0.24, 0.022, 200)
    g.ring(0.5, 0.5, 0.36, 0.36, 0.018, 150)
    for i in range(4):
        a = math.radians(45 + i * 90)
        ox, oy = 0.5 + 0.46 * math.cos(a), 0.5 + 0.46 * math.sin(a)
        ix, iy = 0.5 + 0.28 * math.cos(a), 0.5 + 0.28 * math.sin(a)
        g.line([(ox, oy), (ix, iy)], 0.03, 255)
        n = a + math.pi / 2
        g.poly([(ix, iy), (ix + 0.07 * math.cos(a) + 0.05 * math.cos(n), iy + 0.07 * math.sin(a) + 0.05 * math.sin(n)),
                (ix + 0.07 * math.cos(a) - 0.05 * math.cos(n), iy + 0.07 * math.sin(a) - 0.05 * math.sin(n))], 255)


def astral_railcannon(g):
    g.ring(0.20, 0.50, 0.08, 0.26, 0.03, 220)
    g.ring(0.32, 0.50, 0.06, 0.19, 0.025, 180)
    g.poly([(0.18, 0.44), (0.96, 0.47), (0.96, 0.53), (0.18, 0.56)], 255)
    g.poly([(0.18, 0.48), (0.96, 0.495), (0.96, 0.505), (0.18, 0.52)], 140)
    g.star(0.18, 0.50, 0.14, 0.03, 4, 255)


def astral_greatblade(g):
    g.blade(0.50, 0.06, 0.50, 0.94, 0.09, hilt=0.18)
    g.star(0.50, 0.24, 0.07, 0.02, 4, 160)


def frost_nova(g):
    for i in range(6):
        a = math.radians(i * 60 - 90)
        x1, y1 = 0.5 + 0.40 * math.cos(a), 0.5 + 0.40 * math.sin(a)
        g.line([(0.5, 0.5), (x1, y1)], 0.04, 255)
        for f in (0.55, 0.78):
            bx, by = 0.5 + 0.40 * f * math.cos(a), 0.5 + 0.40 * f * math.sin(a)
            for s in (-1, 1):
                b = a + s * math.radians(40)
                g.line([(bx, by), (bx + 0.11 * math.cos(b), by + 0.11 * math.sin(b))], 0.028, 235)
    g.star(0.5, 0.5, 0.10, 0.04, 6, 255)


def elemental_cataclysm(g):
    g.comet(0.30, 0.40, 0.11, 0.02, 0.06, 220)
    g.comet(0.80, 0.30, 0.09, 0.58, 0.02, 200)
    g.comet(0.58, 0.70, 0.17, 0.12, 0.16, 255)
    g.line([(0.06, 0.94), (0.94, 0.94)], 0.02, 180)


def clockwork(g):
    for i in range(12):
        a = math.radians(i * 30)
        g.poly([(0.5 + 0.34 * math.cos(a - 0.12), 0.5 + 0.34 * math.sin(a - 0.12)), (0.5 + 0.42 * math.cos(a - 0.09), 0.5 + 0.42 * math.sin(a - 0.09)),
                (0.5 + 0.42 * math.cos(a + 0.09), 0.5 + 0.42 * math.sin(a + 0.09)), (0.5 + 0.34 * math.cos(a + 0.12), 0.5 + 0.34 * math.sin(a + 0.12))], 230)
    g.ell(0.5, 0.5, 0.35, 0.35, 255)
    g.ell(0.5, 0.5, 0.28, 0.28, 110)
    for i in range(12):
        a = math.radians(i * 30)
        g.line([(0.5 + 0.22 * math.cos(a), 0.5 + 0.22 * math.sin(a)), (0.5 + 0.26 * math.cos(a), 0.5 + 0.26 * math.sin(a))], 0.02, 255)
    g.line([(0.5, 0.5), (0.5, 0.30)], 0.035, 255)
    g.line([(0.5, 0.5), (0.64, 0.58)], 0.03, 255)
    g.ell(0.5, 0.5, 0.04, 0.04, 255)


def arcane_phalanx(g):
    for i, a in enumerate((-36, -12, 12, 36)):
        r = math.radians(a - 90)
        bx, by = 0.5 + 0.08 * math.cos(r), 0.86 + 0.08 * math.sin(r)
        tx, ty = 0.5 + 0.80 * math.cos(r), 0.86 + 0.80 * math.sin(r)
        g.blade(bx, by, tx, ty, 0.032, guard=True, v=255 if i in (1, 2) else 215, hilt=0.18)
    g.arc(0.5, 0.86, 0.30, 0.30, 200, 340, 0.02, 160)


def afterimage_arsenal(g):
    # The caster and two astral twins behind, fading.
    g.figure(0.24, 0.52, 0.85, 110)
    g.figure(0.76, 0.52, 0.85, 110)
    g.figure(0.50, 0.50, 1.0, 255)
    g.line([(0.70, 0.16), (0.66, 0.90)], 0.03, 230)
    g.ell(0.70, 0.13, 0.05, 0.05, 255)


def void_step(g):
    g.ring(0.34, 0.52, 0.10, 0.32, 0.035, 220)
    g.ell(0.34, 0.52, 0.06, 0.26, 90)
    g.line([(0.40, 0.52), (0.74, 0.52)], 0.05, 255)
    g.poly([(0.72, 0.38), (0.92, 0.52), (0.72, 0.66)], 255)
    for yy in (0.32, 0.72):
        g.line([(0.50, yy), (0.68, yy)], 0.02, 160)


def rift_echo(g):
    g.poly([(0.50, 0.06), (0.60, 0.50), (0.50, 0.94), (0.40, 0.50)], 255)
    g.poly([(0.50, 0.20), (0.54, 0.50), (0.50, 0.80), (0.46, 0.50)], 110)
    for i in range(3):
        r = 0.20 + i * 0.10
        g.arc(0.50, 0.50, r, r * 1.2, 300, 60, 0.025, 220 - i * 55)
        g.arc(0.50, 0.50, r, r * 1.2, 120, 240, 0.025, 220 - i * 55)


def gravity_blast(g):
    for i in range(3):
        g.line([(0.04, 0.40 + i * 0.10), (0.36, 0.44 + i * 0.06)], 0.025, 200 - i * 40)
    for i in range(4):
        a0 = i * 90
        g.arc(0.62, 0.50, 0.30 - i * 0.02, 0.30 - i * 0.02, a0, a0 + 70, 0.03, 220)
    g.ell(0.62, 0.50, 0.16, 0.16, 255)
    g.ell(0.62, 0.50, 0.07, 0.07, 120)


def arcane_rupture(g):
    g.ell(0.5, 0.5, 0.20, 0.20, 255)
    for i in range(8):
        a = math.radians(i * 45 + 22)
        r0, r1 = 0.24, 0.44 if i % 2 == 0 else 0.36
        g.poly([(0.5 + r0 * math.cos(a - 0.18), 0.5 + r0 * math.sin(a - 0.18)), (0.5 + r1 * math.cos(a), 0.5 + r1 * math.sin(a)),
                (0.5 + r0 * math.cos(a + 0.18), 0.5 + r0 * math.sin(a + 0.18))], 240)
    g.line([(0.44, 0.34), (0.52, 0.48), (0.46, 0.56), (0.56, 0.66)], 0.03, 90)
    g.line([(0.52, 0.48), (0.64, 0.44)], 0.025, 90)


def rift_walker(g):
    g.ring(0.24, 0.56, 0.09, 0.28, 0.035, 255)
    g.ring(0.76, 0.44, 0.09, 0.28, 0.035, 255)
    g.ell(0.24, 0.56, 0.05, 0.22, 100)
    g.ell(0.76, 0.44, 0.05, 0.22, 100)
    g.arc(0.50, 0.62, 0.26, 0.30, 200, 340, 0.03, 230)
    g.poly([(0.70, 0.30), (0.80, 0.42), (0.66, 0.44)], 230)


PAINTERS = dict((f.__name__, f) for f in (heavy_slash, impact_wave, impact_punch, moonlight_splitter, crescent_cleave, blade_storm,
                                         frenzied_charge, eclipse, halfmoon_slash, knights_guidance, stomp, circle_swing, bonecrusher,
                                         seismic_guillotine, punishing_bomb, whirlwind, battlecry, flame_burst, glacial_descent,
                                         stonefang_eruption, meteor_fall, gravity_dominion, astral_railcannon, astral_greatblade, frost_nova,
                                         elemental_cataclysm, clockwork, arcane_phalanx, afterimage_arsenal, void_step, rift_echo,
                                         gravity_blast, arcane_rupture, rift_walker))


def category(sid, kit):
    cls, ac, cskills, adv, ult, grace, sigs = kit
    if sid == grace:
        return "gold"
    if sid == ult:
        return "maroon"
    if sid in sigs:
        return "navy"
    if sid == "void_step":
        return "green"
    return "cyan"


def paint(sid, color, size, donor_ref, strip=True):
    """Art for one opening of size (w, h)."""
    w, h = size
    dbox = FIELDS[DONOR[color]]
    if color == "gold":
        dbox = GRACE_INNER
    donor = donor_ref.crop(dbox).convert("RGB")
    W, H = w * S, h * S
    bg = field_background(donor.resize((W, H), Image.LANCZOS), W, H)
    margin = 0.04
    gw, gh = int(W * (1 - 2 * margin)), int((H - (BAR_ROWS * S if strip else 0)) * (1 - 2 * margin))
    shape_small, glow_small = glow_layer(lambda d: PAINTERS[sid](G(d, gw, gh)), gw, gh, (255, 255, 255), 5 * S)
    shape = Image.new("L", (W, H), 0)
    glow = Image.new("L", (W, H), 0)
    ox, oy = int(W * margin), int((H - (BAR_ROWS * S if strip else 0)) * margin)
    shape.paste(shape_small, (ox, oy))
    glow.paste(glow_small, (ox, oy))
    tint = tuple(int(v) for v in np.percentile(np.asarray(donor).reshape(-1, 3), 90, axis=0))
    glow_col = tuple(int(0.5 * c + 0.5 * 255) for c in tint)
    art = composite(bg, shape, glow, (250, 252, 245), glow_col).convert("RGB").resize((w, h), Image.LANCZOS)
    if strip:
        bar = donor.resize((w, donor.height)).crop((0, donor.height - BAR_ROWS, w, donor.height))
        art.paste(bar, (0, h - BAR_ROWS))
    return art


FRAME_FILE = {"cyan": "Icon_shield_charge.png", "navy": "Icon_goddess_relic.png", "green": "Icon_ray_of_hope.png",
              "maroon": "Icon_electric_smite.png", "gold": "Icon_heavens_light.png"}
FRAME_KEEP = os.path.join(ROOT, "docs", "source_art", "backdrops_v0210")


def write_icon(sid, color, art_canvas, box):
    """Static Icon_<id>.png (Altar): the category's hotbar frame + the opening art, inset 7 / 8 px."""
    keep = os.path.join(FRAME_KEEP, FRAME_FILE[color])
    icon = Image.open(keep if os.path.exists(keep) else os.path.join(A, FRAME_FILE[color])).convert("RGBA")
    iw, ih = icon.width - 14, icon.height - 16
    x0, y0, x1, y1 = box
    fw, fh = x1 - x0 - 6, y1 - y0 - 6
    asp = iw / float(ih)
    cw = min(fw, fh * asp)
    ch = cw / asp
    cx, cy = (x0 + x1) / 2.0, (y0 + y1) / 2.0
    crop = art_canvas.crop((int(round(cx - cw / 2)), int(round(cy - ch / 2)), int(round(cx + cw / 2)), int(round(cy + ch / 2))))
    icon.paste(crop.resize((iw, ih), Image.LANCZOS).convert("RGBA"), (7, 8))
    icon.save(os.path.join(A, "Icon_" + sid + ".png"))


def main():
    preview = None
    if "--preview" in sys.argv:
        preview = sys.argv[sys.argv.index("--preview") + 1]
    chassis = Image.open(os.path.join(A, "Cleric_Paladin_PreAdvance.png")).convert("RGB")
    donor_ref = Image.open(os.path.join(A, "Cleric_Paladin_Reference.png")).convert("RGB")
    previews = []
    for kit in KITS:
        cls, ac, cskills, adv, ult, grace, sigs = kit
        out = chassis.copy()
        slots = list(zip(CLASS_SLOTS, cskills)) + list(zip(ADV_SLOTS, adv)) + [("electric_smite", ult)]
        for slot, sid in slots:
            box = FIELDS[slot]
            out.paste(paint(sid, category(sid, kit), (box[2] - box[0], box[3] - box[1]), donor_ref), box[:2])
        for box in (GRACE_INNER, FOOTER_INNER):
            out.paste(paint(grace, "gold", (box[2] - box[0], box[3] - box[1]), donor_ref, strip=False), box[:2])
        for slot, sid in slots:
            write_icon(sid, category(sid, kit), out, FIELDS[slot])
        write_icon(grace, "gold", out, GRACE_INNER)
        name = "%s_%s_Artwork.png" % (cls, ac)
        out.save(os.path.join(A, name))
        print("wrote", name)
        if preview:
            sheet = Image.new("RGB", (12 * 112, 112), (24, 22, 20))
            items = slots + [("heavens_light", grace)]
            for i, (slot, sid) in enumerate(items):
                b = FIELDS[slot]
                pad = 10
                sheet.paste(out.crop((b[0] - pad, b[1] - pad, b[2] + pad, b[3] + pad)).resize((106, 106), Image.LANCZOS), (i * 112 + 3, 3))
            previews.append(sheet)
    if preview:
        full = Image.new("RGB", (12 * 112, 112 * len(previews)))
        for i, s in enumerate(previews):
            full.paste(s, (0, i * 112))
        full.save(preview)


if __name__ == "__main__":
    main()
