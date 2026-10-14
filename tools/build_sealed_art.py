"""v0.25.166 themed backgrounds for the LOCKED Advancement panel of the skill tree (one per Base Class).
Output: ImmortalHeroesAssets/Sealed_<Class>.png (1011x662, only the panel area x 330-990 / y 110-530 is used; the game
blends it in with Tree_SealedMask.png). Dark and low-contrast in the middle so the Altar message stays readable.
  Warrior  - rustic: riveted, weathered iron plates with rust bloom, crimson tint, faint crossed-swords crest
  Cleric   - elegant kingdom: royal blue with a gold damask lattice, gold inner filigree border, faint crown crest
  Sorcerer - arcane: violet night nebula with stars and faint constellations, faint crescent-and-stars crest
  Ranger   - wilds: deep forest green, wood grain and drifting leaf silhouettes, faint bow crest
Usage: python3 tools/build_sealed_art.py [preview_dir]"""
import os, sys, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
W, H = 1011, 662
X0, Y0, X1, Y1 = 330, 110, 990, 530
CX, CY = 658, 322
rng = np.random.default_rng(1664)

def fbm(octaves=5, base=4, seed=0):
    r = np.random.default_rng(seed)
    acc = np.zeros((H, W)); amp = 1.0; tot = 0
    for o in range(octaves):
        n = base * (2 ** o)
        g = r.random((n * H // W + 2, n + 2))
        up = np.array(Image.fromarray((g * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC)) / 255.0
        acc += up * amp; tot += amp; amp *= 0.5
    return acc / tot

def vignette(strength=0.45):
    yy, xx = np.mgrid[0:H, 0:W]
    dx = (xx - CX) / 340.0; dy = (yy - CY) / 220.0
    d = np.clip(np.sqrt(dx * dx + dy * dy), 0, 1.4)
    return 1 - strength * np.clip(d, 0, 1) ** 1.6

def centre_calm(img, k=0.18):
    # slightly darker, smoother centre behind the text block
    yy, xx = np.mgrid[0:H, 0:W]
    e = np.exp(-(((xx - CX) / 230.0) ** 2 + ((yy - 312) / 70.0) ** 2))
    return img * (1 - k * e[..., None])

def emblem(draw_fn, alpha, blur=1.2, size=(260, 260), at=(CX, 440), out=(200, 200)):
    # crest drawn on a 260 canvas, shrunk to 200 and placed under the text block (text ends ~y 365)
    lay = Image.new("L", size, 0)
    draw_fn(ImageDraw.Draw(lay), size)
    lay = lay.filter(ImageFilter.GaussianBlur(blur)).resize(out, Image.LANCZOS)
    size = out
    m = np.zeros((H, W))
    a = np.array(lay) / 255.0
    x = at[0] - size[0] // 2; y = at[1] - size[1] // 2
    m[y:y + size[1], x:x + size[0]] = a
    return m * alpha

def to_img(rgb):
    return Image.fromarray(np.clip(rgb * 255, 0, 255).astype(np.uint8), "RGB")

def warrior():
    base = np.array([0.20, 0.10, 0.085])
    n = fbm(6, 3, 11); fine = fbm(4, 40, 12)
    rust = np.clip((fbm(5, 5, 13) - 0.55) * 3, 0, 1)
    img = base[None, None, :] * (0.75 + 0.5 * n[..., None]) * (0.9 + 0.2 * fine[..., None])
    img = img * (1 - rust[..., None]) + np.array([0.30, 0.13, 0.06])[None, None, :] * rust[..., None] * (0.8 + 0.4 * n[..., None])
    # plate seams + rivets
    im = to_img(img); d = ImageDraw.Draw(im, "RGBA")
    xs = [X0 + 10, 548, 768, X1 - 10]; ys = [Y0 + 10, 320, Y1 - 10]
    for x in xs[1:-1]:
        d.line([(x, Y0), (x, Y1)], fill=(8, 4, 3, 150), width=3); d.line([(x + 2, Y0), (x + 2, Y1)], fill=(120, 70, 55, 40), width=1)
    for y in ys[1:-1]:
        d.line([(X0, y), (X1, y)], fill=(8, 4, 3, 150), width=3); d.line([(X0, y + 2), (X1, y + 2)], fill=(120, 70, 55, 40), width=1)
    for x in xs:
        for y in range(Y0 + 18, Y1 - 10, 34):
            d.ellipse([x - 4 + 8 * (x == xs[0]) - 8 * (x == xs[-1]), y - 4, x + 4 + 8 * (x == xs[0]) - 8 * (x == xs[-1]), y + 4], fill=(70, 40, 32, 200), outline=(150, 95, 75, 120))
    for y in ys:
        for x in range(X0 + 40, X1 - 20, 40):
            yy = y + 8 * (y == ys[0]) - 8 * (y == ys[-1])
            d.ellipse([x - 4, yy - 4, x + 4, yy + 4], fill=(70, 40, 32, 200), outline=(150, 95, 75, 120))
    # scratches
    for i in range(70):
        x = rng.uniform(X0, X1); y = rng.uniform(Y0, Y1); a = rng.uniform(-0.6, 0.6); L = rng.uniform(15, 70)
        d.line([(x, y), (x + L * math.cos(a), y + L * math.sin(a))], fill=(170, 120, 100, int(rng.uniform(15, 40))), width=1)
    img = np.array(im) / 255.0
    def crest(dr, s):
        w, h = s
        for sgn in (1, -1):
            pts = [(w / 2 - sgn * 95, 40), (w / 2 + sgn * 85, h - 50)]
            dr.line(pts, fill=255, width=9)
            dr.line([(w / 2 + sgn * 55, h - 95), (w / 2 + sgn * 100, h - 72)], fill=255, width=11)
        dr.polygon([(w / 2 - 55, 70), (w / 2 + 55, 70), (w / 2 + 50, 150), (w / 2, 200), (w / 2 - 50, 150)], outline=255, width=6)
    e = emblem(crest, 0.10)
    img = img * (1 - e[..., None]) + np.array([0.55, 0.35, 0.28])[None, None, :] * e[..., None]
    return centre_calm(img * vignette(0.5)[..., None])

def cleric():
    yy, xx = np.mgrid[0:H, 0:W]
    t = np.clip((yy - Y0) / (Y1 - Y0), 0, 1)
    base = np.array([0.075, 0.11, 0.22]) * (1.15 - 0.3 * t)[..., None]
    img = base * (0.92 + 0.16 * fbm(5, 3, 21)[..., None])
    # damask lattice: diamond grid of thin gold lines + small fleur dots
    im = to_img(img); d = ImageDraw.Draw(im, "RGBA")
    s = 46
    for k in range(-30, 40):
        d.line([(X0 + k * s, Y0), (X0 + k * s + (Y1 - Y0), Y1)], fill=(205, 170, 95, 22), width=1)
        d.line([(X0 + k * s, Y0), (X0 + k * s - (Y1 - Y0), Y1)], fill=(205, 170, 95, 22), width=1)
    for j in range(0, (Y1 - Y0) // (s // 2) + 2):
        for i in range(0, (X1 - X0) // s + 2):
            x = X0 + i * s + (s // 2 if j % 2 else 0); y = Y0 + j * (s // 2)
            d.polygon([(x, y - 5), (x + 3, y), (x, y + 5), (x - 3, y)], fill=(215, 180, 105, 34))
    # inner filigree border
    for inset, a in ((16, 90), (22, 45)):
        d.rounded_rectangle([X0 + inset, Y0 + inset + 12, X1 - inset, Y1 - inset], radius=14, outline=(220, 185, 110, a), width=1)
    for (x, y) in ((X0 + 16, Y0 + 28), (X1 - 16, Y0 + 28), (X0 + 16, Y1 - 16), (X1 - 16, Y1 - 16)):
        d.ellipse([x - 5, y - 5, x + 5, y + 5], fill=(225, 190, 115, 110))
    img = np.array(im) / 255.0
    def crest(dr, s):
        w, h = s
        dr.polygon([(w / 2 - 80, 120), (w / 2 - 80, 60), (w / 2 - 45, 95), (w / 2, 40), (w / 2 + 45, 95), (w / 2 + 80, 60), (w / 2 + 80, 120)], outline=255, width=6)
        dr.line([(w / 2 - 82, 130), (w / 2 + 82, 130)], fill=255, width=6)
        dr.line([(w / 2, 145), (w / 2, 235)], fill=255, width=8); dr.line([(w / 2 - 35, 175), (w / 2 + 35, 175)], fill=255, width=8)
    e = emblem(crest, 0.12)
    img = img * (1 - e[..., None]) + np.array([0.80, 0.68, 0.42])[None, None, :] * e[..., None]
    return centre_calm(img * vignette(0.4)[..., None])

def sorcerer():
    n = fbm(6, 3, 31); n2 = fbm(5, 4, 32)
    img = np.array([0.07, 0.04, 0.13])[None, None, :] * (0.8 + 0.4 * n[..., None])
    neb = np.clip((n2 - 0.5) * 2.5, 0, 1)
    img = img + np.array([0.16, 0.06, 0.22])[None, None, :] * neb[..., None] * 0.6 + np.array([0.04, 0.05, 0.14])[None, None, :] * (1 - n2)[..., None] * 0.4
    im = to_img(img); d = ImageDraw.Draw(im, "RGBA")
    stars = []
    for i in range(260):
        x = rng.uniform(X0, X1); y = rng.uniform(Y0, Y1); r = rng.choice([0.6, 0.9, 1.3, 2.0], p=[0.5, 0.3, 0.15, 0.05])
        a = int(rng.uniform(60, 190) * (0.7 if r < 1 else 1)); stars.append((x, y))
        d.ellipse([x - r, y - r, x + r, y + r], fill=(225, 210, 255, a))
    for c in range(5):
        k = rng.integers(0, len(stars)); pts = [stars[k]]
        for j in range(4):
            px, py = pts[-1]; pts.append((px + rng.uniform(-60, 60), py + rng.uniform(-40, 40)))
        d.line(pts, fill=(190, 160, 255, 30), width=1)
        for p in pts: d.ellipse([p[0] - 1.6, p[1] - 1.6, p[0] + 1.6, p[1] + 1.6], fill=(230, 215, 255, 120))
    img = np.array(im) / 255.0
    def crest(dr, s):
        w, h = s
        dr.ellipse([w / 2 - 80, 50, w / 2 + 80, 210], fill=255); dr.ellipse([w / 2 - 45, 35, w / 2 + 105, 185], fill=0)
        for (x, y, r) in ((w / 2 + 55, 205, 9), (w / 2 + 85, 165, 6), (w / 2 + 25, 230, 5)):
            dr.polygon([(x, y - r * 2), (x + r * 0.5, y - r * 0.5), (x + r * 2, y), (x + r * 0.5, y + r * 0.5), (x, y + r * 2), (x - r * 0.5, y + r * 0.5), (x - r * 2, y), (x - r * 0.5, y - r * 0.5)], fill=255)
    e = emblem(crest, 0.11)
    img = img * (1 - e[..., None]) + np.array([0.70, 0.58, 0.95])[None, None, :] * e[..., None]
    return centre_calm(img * vignette(0.45)[..., None])

def ranger():
    yy, xx = np.mgrid[0:H, 0:W]
    grain = np.sin((xx + 18 * fbm(4, 3, 41)) * 0.09 + 6 * fbm(3, 2, 42)) * 0.5 + 0.5
    n = fbm(5, 3, 43)
    img = np.array([0.075, 0.15, 0.095])[None, None, :] * (0.78 + 0.3 * n[..., None]) * (0.9 + 0.14 * grain[..., None])
    im = to_img(img)
    leaf = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(leaf)
    for i in range(46):
        x = rng.uniform(X0, X1); y = rng.uniform(Y0, Y1); s = rng.uniform(9, 24); a = rng.uniform(0, math.pi)
        pts = []
        for k in range(24):
            t = k / 23.0 * math.pi * 2
            px = math.cos(t) * s; py = math.sin(t) * s * 0.38 * (1.0 - 0.3 * math.cos(t))
            pts.append((x + px * math.cos(a) - py * math.sin(a), y + px * math.sin(a) + py * math.cos(a)))
        d.polygon(pts, fill=(120, 170, 95, int(rng.uniform(14, 34))))
        d.line([pts[0], pts[12]], fill=(40, 70, 35, 30), width=1)
    leaf = leaf.filter(ImageFilter.GaussianBlur(0.8)); im = im.convert("RGBA"); im.alpha_composite(leaf)
    img = np.array(im.convert("RGB")) / 255.0
    def crest(dr, s):
        w, h = s
        dr.arc([w / 2 - 110, 40, w / 2 + 30, 230], 285, 75, fill=255, width=8)
        dr.line([(w / 2 + 2, 52), (w / 2 + 2, 218)], fill=255, width=2)
        dr.line([(w / 2 - 70, 135), (w / 2 + 95, 135)], fill=255, width=5)
        dr.polygon([(w / 2 + 110, 135), (w / 2 + 88, 124), (w / 2 + 88, 146)], fill=255)
    e = emblem(crest, 0.11)
    img = img * (1 - e[..., None]) + np.array([0.62, 0.78, 0.50])[None, None, :] * e[..., None]
    return centre_calm(img * vignette(0.45)[..., None])

THEMES = {"Warrior": warrior, "Cleric": cleric, "Sorcerer": sorcerer, "Ranger": ranger}

def main():
    for cls, fn in THEMES.items():
        to_img(fn()).save(os.path.join(A, "Sealed_" + cls + ".png"))
        print("Sealed_" + cls + ".png")

if __name__ == "__main__":
    main()
