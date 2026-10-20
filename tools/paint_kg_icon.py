# Knight's Guidance (damage buff): a knight's sword raised upright, power surging up it, light bursting behind.
import numpy as np, sys
from PIL import Image, ImageDraw, ImageFilter
N = 384; rng = np.random.default_rng(31)
yy, xx = np.mgrid[0:N, 0:N] / N
cx, cy = 0.5, 0.42
r = np.hypot(xx - cx, (yy - cy) * 1.1)
lum = 0.05 + 0.38 * np.exp(-(r / 0.22) ** 2)                       # glow behind the blade
ang = np.arctan2(yy - cy, xx - cx)
rays = (np.cos(ang * 9) * 0.5 + 0.5) ** 6 * np.exp(-(r / 0.5) ** 2) * 0.16
lum += rays
# painterly texture
noise = np.array(Image.fromarray((rng.random((48, 48)) * 255).astype('uint8')).resize((N, N), Image.BICUBIC)) / 255.0
noise2 = np.array(Image.fromarray((rng.random((12, 12)) * 255).astype('uint8')).resize((N, N), Image.BICUBIC)) / 255.0
lum *= (0.6 + 0.5 * noise) * (0.7 + 0.6 * noise2)
img = Image.fromarray(np.clip(lum * 255, 0, 255).astype('uint8'))
d = ImageDraw.Draw(img)
# rising power tongues around the blade
aura = Image.new('L', (N, N), 0); a = ImageDraw.Draw(aura)
for k in range(26):
    x0 = cx * N + rng.normal(0, 34); y0 = N * rng.uniform(0.55, 0.95); h = rng.uniform(80, 220); w = rng.uniform(5, 14)
    a.polygon([(x0 - w, y0), (x0 + w, y0), (x0 + rng.normal(0, 10), y0 - h)], fill=int(rng.uniform(90, 200)))
aura = aura.filter(ImageFilter.GaussianBlur(7))
img = Image.fromarray(np.clip(np.array(img, float) + np.array(aura, float) * 0.35, 0, 255).astype('uint8'))
d = ImageDraw.Draw(img)
# the sword (tip up)
tip, guard, bot = 0.03 * N, 0.68 * N, 0.97 * N; hw = 0.065 * N; X = cx * N
d.polygon([(X, tip), (X + hw, tip + 0.09 * N), (X + hw * 0.9, guard), (X - hw * 0.9, guard), (X - hw, tip + 0.09 * N)], fill=150)
d.polygon([(X, tip), (X + hw * 0.15, tip + 0.09 * N), (X + hw * 0.15, guard), (X, guard)], fill=245)     # lit edge
d.line([(X - hw * 0.3, tip + 0.12 * N), (X - hw * 0.3, guard - 6)], fill=95, width=4)                      # fuller
d.rounded_rectangle([X - 0.17 * N, guard - 8, X + 0.17 * N, guard + 12], 6, fill=125)
d.line([(X - 0.16 * N, guard - 5), (X + 0.16 * N, guard - 5)], fill=215, width=3)                     # crossguard
d.polygon([(X - 0.17 * N, guard - 8), (X - 0.21 * N, guard - 22), (X - 0.15 * N, guard + 2)], fill=125)
d.polygon([(X + 0.17 * N, guard - 8), (X + 0.21 * N, guard - 22), (X + 0.15 * N, guard + 2)], fill=125)
d.rectangle([X - 9, guard + 12, X + 9, bot - 14], fill=70)                                                # grip
for k in range(5): d.line([(X - 9, guard + 20 + k * 11), (X + 9, guard + 14 + k * 11)], fill=120, width=3)
d.ellipse([X - 16, bot - 22, X + 16, bot + 6], fill=120)
d.arc([X - 16, bot - 22, X + 16, bot + 6], 200, 320, fill=220, width=3)                                                  # pommel
# sparks
for k in range(40):
    sx = X + rng.normal(0, 60); sy = rng.uniform(0.05, 0.9) * N; s = rng.uniform(0.8, 2.2)
    d.ellipse([sx - s, sy - s, sx + s, sy + s], fill=int(rng.uniform(170, 255)))
glow = img.filter(ImageFilter.GaussianBlur(5))
out = np.clip(np.array(img, float) * 0.85 + np.array(glow, float) * 0.3, 0, 255) / 255.0
out = out ** 0.85
# Sword Master grade (build_class_art GRADES['swordmaster'])
stops = np.array([(4, 8, 22), (34, 70, 160), (130, 185, 255), (238, 246, 255)], float); pos = np.linspace(0, 1, len(stops))
rgb = np.stack([np.interp(out, pos, stops[:, c]) for c in range(3)], -1)
Image.fromarray(rgb.astype('uint8')).save(sys.argv[1])
