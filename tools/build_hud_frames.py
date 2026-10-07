"""v0.25.68 HUD frames from the user's four paintings (docs/source_art/hud_frames/HUD_<class>.png).
Cleans each painting (bar fills, values, class name, food items/counts removed -> live), keys the
background to alpha, recolours the class accent per Advancement Class and writes a preview sheet.
Usage: python3 tools/build_hud_frames.py [preview_out.png] [asset_out_dir]"""
import sys, os, math
import numpy as np, cv2
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "source_art", "hud_frames")
SERIF = "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"
HUE = {"red": [(0, 12), (165, 180)], "lime": [(25, 50)], "blue": [(85, 118)], "gold": [(10, 32)]}

# Per Base Class geometry in painting pixels.
# bars: (hue key, y band, search x0, x1, x max of the channel)
CLASSES = {
 "Warrior": dict(file="warrior", accent=[(0, 12), (165, 180)],
    bars=[("red", 245, 300, 590, 1286, 1282), ("lime", 313, 367, 590, 1314, 1310), ("blue", 381, 435, 590, 1314, 1310), ("gold", 451, 507, 590, 1052, 1300)], trough=(1100, 1280), tip=6,
    values=(1292, 245, 1500, 505), value_x=1490,
    name=(330, 176, 600, 222),
    food=[((752, 640), 50), ((968, 640), 50), ((1185, 640), 50)], food_text=[(808, 688), (1024, 688), (1240, 688)], food_digit_r=15,
    keep=[(420, 230, 600, 510)]),
 "Ranger": dict(file="ranger", accent=[(30, 85)],
    bars=[("red", 238, 282, 580, 1292, 1400), ("lime", 304, 352, 580, 1156, 1400), ("blue", 372, 420, 580, 1136, 1400), ("gold", 440, 488, 580, 1082, 1390)], trough=(1150, 1370), tip=6,
    values=None, name=(440, 512, 730, 552),
    food=[((708, 675), 42), ((950, 675), 40), ((1195, 675), 42)], food_text=[(784, 667), (1026, 667), (1272, 667)], food_digit_r=17,
    keep=[(430, 232, 500, 492)]),
 "Sorcerer": dict(file="sorcerer", accent=[(112, 165)],
    bars=[("red", 230, 282, 700, 1462, 1458), ("lime", 298, 352, 700, 1290, 1462), ("blue", 366, 420, 700, 1226, 1465), ("gold", 436, 492, 700, 1124, 1462)], trough=(1150, 1440), tip=10,
    values=None, name=(205, 522, 455, 568),
    food=[((815, 680), 44), ((975, 680), 44), ((1135, 680), 44)], food_text=[(853, 713), (1013, 713), (1173, 713)], food_digit_r=14,
    keep=[(520, 225, 610, 495)]),
 "Cleric": dict(file="cleric", accent=[(12, 34)],
    bars=[("red", 278, 330, 660, 1452, 1448), ("lime", 357, 405, 660, 1400, 1450), ("blue", 434, 484, 660, 1400, 1450), ("gold", 506, 560, 660, 1146, 1450)], trough=(1240, 1440), tip=22,
    values=None, name=(200, 578, 380, 622),
    food=[((760, 672), 46), ((1012, 672), 46), ((1266, 672), 48)], food_text=[(870, 672), (1120, 672), (1374, 672)], food_digit_r=0,
    keep=[(470, 270, 560, 570)], food_erase=[(815, 650, 925, 695), (1065, 650, 1175, 695), (1318, 650, 1430, 695)]),
}
# Advancement Class colours (target hue in OpenCV 0-180, saturation multiplier, value multiplier)
ACS = {
 "Warrior": [("Warrior", None), ("Sword Master", (108, 1.15, 1.1)), ("Mercenary", (12, 1.2, 1.15))],
 "Cleric": [("Cleric", None), ("Paladin", (172, 0.9, 1.0)), ("Priest", (72, 0.9, 1.0))],
 "Sorcerer": [("Sorcerer", None), ("Archmage", (115, 1.0, 1.05)), ("Horizon Walker", (92, 1.0, 1.05))],
 "Ranger": [("Ranger", None), ("Acrobat", (80, 1.0, 1.05)), ("Bowmaster", (20, 1.2, 1.1))],
}

def hue_mask(hsv, ranges, smin, vmin):
    h, s, v = hsv[..., 0], hsv[..., 1], hsv[..., 2]
    m = np.zeros(h.shape, bool)
    for a, b in ranges: m |= (h >= a) & (h <= b)
    return m & (s >= smin) & (v >= vmin)

def build(cls):
    g = CLASSES[cls]
    bgr = cv2.imread(os.path.join(SRC, "HUD_" + g["file"] + ".png"))
    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)
    H, W = bgr.shape[:2]
    erase = np.zeros((H, W), np.uint8)
    strips = []
    base_img = bgr.copy()
    # trough profile: averaged columns of the empty EXP channel, resampled per bar
    gk, gy0, gy1, gx0, gx1, gxm = g["bars"][3]
    gm = hue_mask(hsv[gy0:gy1, gx0:gx1], HUE[gk], 70, 70)
    gys = np.nonzero(gm)[0]
    ty0, ty1 = gy0 + gys.min() - 4, gy0 + gys.max() + 5
    tx0, tx1 = g["trough"]
    profile = bgr[ty0:ty1, tx0:tx1].astype(float).mean(axis=1)   # (h, 3)
    for key, y0, y1, x0, x1, xmax in g["bars"]:
        m = np.zeros((H, W), np.uint8)
        m[y0:y1, x0:x1] = hue_mask(hsv[y0:y1, x0:x1], HUE[key], 70, 70)
        m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
        n, lab, st, _ = cv2.connectedComponentsWithStats(m, 8)
        if n > 1:
            big = 1 + int(np.argmax(st[1:, 4]))
            keepc = [i for i in range(1, n) if st[i, 4] > 150 and abs((st[i, 1] + st[i, 3] / 2) - (st[big, 1] + st[big, 3] / 2)) < 8]
            m = np.isin(lab, keepc).astype(np.uint8)
        ys, xs = np.nonzero(m)
        bx0, bx1, by0, by1 = xs.min(), xs.max(), ys.min(), ys.max()
        pad = 3
        crop = bgr[by0 - pad:by1 + pad + 1, bx0 - pad:bx1 + pad + 1]
        alpha = cv2.GaussianBlur(cv2.dilate(m[by0 - pad:by1 + pad + 1, bx0 - pad:bx1 + pad + 1] * 255, np.ones((3, 3), np.uint8)), (5, 5), 0)
        strips.append(dict(key=key, img=crop, alpha=alpha, x0=bx0 - pad, y0=by0 - pad, xmax=xmax + pad, full=(bx1 + pad)))
        # paint the empty trough over the fill (+ its glow tip), feathered at the ends
        ry0, ry1 = by0 - 4, by1 + 5
        rx0, rx1 = bx0 - 4, min(W - 1, bx1 + g["tip"] + 8)
        hgt = ry1 - ry0
        prof = cv2.resize(profile[None, :, :].astype(np.float32), (hgt, 1), interpolation=cv2.INTER_LINEAR)[0]
        block = np.repeat(prof[:, None, :], rx1 - rx0, axis=1)
        wgt = np.ones((hgt, rx1 - rx0))
        f = 6
        for i in range(f):
            wgt[:, i] *= (i + 1) / (f + 1); wgt[:, -1 - i] *= (i + 1) / (f + 1)
            wgt[i, :] *= (i + 1) / (f + 1); wgt[-1 - i, :] *= (i + 1) / (f + 1)
        reg = base_img[ry0:ry1, rx0:rx1].astype(float)
        base_img[ry0:ry1, rx0:rx1] = (reg * (1 - wgt[..., None]) + block * wgt[..., None]).astype(np.uint8)
    bgr_src = bgr
    bgr = base_img
    def text_erase(rect, smax=90, vmin=120):
        x0, y0, x1, y1 = rect
        sub = hsv[y0:y1, x0:x1]
        t = ((sub[..., 1] <= smax) & (sub[..., 2] >= vmin)).astype(np.uint8) * 255
        erase[y0:y1, x0:x1] |= cv2.dilate(t, np.ones((5, 5), np.uint8))
    if g["values"]: text_erase(g["values"], 90, 110)
    text_erase(g["name"], 110, 120)
    for (c, r) in g["food"]:
        cv2.circle(erase, c, r, 255, -1)
    for (x0, y0, x1, y1) in g.get("food_erase", []):
        text_erase((x0, y0, x1, y1), 140, 110)
    if g["food_digit_r"]:
        for c in g["food_text"]:
            x, y = c; rr = g["food_digit_r"]
            text_erase((x - rr, y - rr - 4, x + rr, y + rr + 4), 80, 110)
    clean = cv2.inpaint(bgr, erase, 6, cv2.INPAINT_TELEA) if erase.any() else bgr
    # alpha: background-like pixels connected to the border become transparent (soft by distance)
    bg = np.median(np.concatenate([bgr_src[:20, :20].reshape(-1, 3), bgr_src[-20:, -20:].reshape(-1, 3), bgr_src[:20, -20:].reshape(-1, 3), bgr_src[-20:, :20].reshape(-1, 3)]), axis=0)
    diff = np.abs(bgr_src.astype(int) - bg).max(axis=2)
    lowm = (diff < 26).astype(np.uint8)
    n, lab = cv2.connectedComponents(lowm, connectivity=4)
    outside = np.isin(lab, np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])))
    outside &= lowm.astype(bool)
    a = np.full((H, W), 255.0)
    a[outside] = np.clip((diff[outside] - 16) / 24.0, 0, 1) * 255
    a = cv2.GaussianBlur(a, (3, 3), 0)
    # un-premultiply the dark background out of soft edges
    af = np.clip(a / 255.0, 1e-3, 1)[..., None]
    rgb = np.clip((clean.astype(float) - bg * (1 - af)) / af, 0, 255)
    rgb = np.where(af > 0.02, rgb, clean)
    keep = np.zeros((H, W), bool)
    for (x0, y0, x1, y1) in g["keep"]: keep[y0:y1, x0:x1] = True
    for key, y0, y1, x0, x1, xmax in g["bars"]: keep[y0:y1, x0:xmax + 10] = True
    return dict(g=g, rgb=rgb.astype(np.uint8), alpha=a.astype(np.uint8), strips=strips, keep=keep)

def recolor(base, target):
    if target is None: return base["rgb"]
    th, sm, vm = target
    hsv = cv2.cvtColor(base["rgb"], cv2.COLOR_BGR2HSV).astype(float)
    acc = hue_mask(hsv.astype(np.uint8), base["g"]["accent"], 45, 30) & ~base["keep"]
    acc = cv2.GaussianBlur(acc.astype(float), (5, 5), 0)
    out = hsv.copy()
    out[..., 0] = th
    out[..., 1] = np.clip(hsv[..., 1] * sm, 0, 255)
    out[..., 2] = np.clip(hsv[..., 2] * vm, 0, 255)
    rgb2 = cv2.cvtColor(out.astype(np.uint8), cv2.COLOR_HSV2BGR).astype(float)
    m = acc[..., None]
    return (base["rgb"] * (1 - m) + rgb2 * m).astype(np.uint8)

def to_pil(bgr, alpha):
    rgba = np.dstack([bgr[..., ::-1], alpha])
    return Image.fromarray(rgba, "RGBA")

def stretch_strip(st, frac):
    img = to_pil(st["img"], st["alpha"])
    w, h = img.size
    target = int(round((st["xmax"] - st["x0"]) * frac))
    if target < 4: return None
    cap = min(int(h * 1.1), w // 3, target // 2)
    left = img.crop((0, 0, cap, h)); right = img.crop((w - cap, 0, w, h)); mid = img.crop((cap, 0, w - cap, h))
    out = Image.new("RGBA", (target, h), (0, 0, 0, 0))
    out.paste(left, (0, 0)); out.paste(right, (target - cap, 0))
    if target - 2 * cap > 0: out.paste(mid.resize((target - 2 * cap, h), Image.LANCZOS), (cap, 0))
    return out

def text(d, xy, s, size, fill=(236, 226, 200), anchor="mm", maxw=None):
    f = ImageFont.truetype(SERIF, size)
    while maxw and d.textlength(s, font=f) > maxw and size > 8:
        size -= 1; f = ImageFont.truetype(SERIF, size)
    d.text((xy[0] + 2, xy[1] + 2), s, font=f, fill=(0, 0, 0, 200), anchor=anchor)
    d.text(xy, s, font=f, fill=fill, anchor=anchor)

FOOD = None
def food_icons():
    global FOOD
    if FOOD: return FOOD
    im = Image.open(os.path.join(SRC, "HUD_warrior.png")).convert("RGBA")
    FOOD = []
    for (cx, cy) in [(752, 640), (968, 640), (1185, 640)]:
        r = 46; ic = im.crop((cx - r, cy - r, cx + r, cy + r))
        m = Image.new("L", (2 * r, 2 * r), 0); ImageDraw.Draw(m).ellipse([0, 0, 2 * r - 1, 2 * r - 1], fill=255)
        ic.putalpha(m.filter(ImageFilter.GaussianBlur(1))); FOOD.append(ic)
    return FOOD

def compose(base, ac, target, values=(1.0, 0.82, 0.55, 0.62)):
    rgb = recolor(base, target)
    img = to_pil(rgb, base["alpha"])
    d = ImageDraw.Draw(img)
    g = base["g"]
    for st, f in zip(base["strips"], values):
        s = stretch_strip(st, f)
        if s: img.alpha_composite(s, (st["x0"], st["y0"]))
    d = ImageDraw.Draw(img)
    if g["values"]:
        nums = ["720 / 720", "123 / 150", "55 / 100", "1,240 / 2,000"]
        for (key, y0, y1, *_), s in zip(g["bars"], nums):
            text(d, (g["value_x"], (y0 + y1) // 2), s, 30, anchor="rm")
    x0, y0, x1, y1 = g["name"]
    text(d, ((x0 + x1) // 2, (y0 + y1) // 2), ac.upper(), 30, maxw=(x1 - x0) - 30)
    icons = food_icons()
    for i, ((c, r), t) in enumerate(zip(g["food"], g["food_text"])):
        ic = icons[i].resize((2 * r - 6, 2 * r - 6), Image.LANCZOS)
        img.alpha_composite(ic, (c[0] - r + 3, c[1] - r + 3))
        text(d, t, ["27m", "17m", "9m"][i], 26 if g["food_digit_r"] else 30, anchor="mm")
    return img

def buffs_at(img, bb):
    d = ImageDraw.Draw(img)
    icons = ["Buff_hyper_armor.png", "Buff_attack_speed.png", "Buff_damage.png", "Buff_fury.png"]
    x = bb[0] + int((bb[2] - bb[0]) * 0.56); y = bb[1] - 150
    for i, n in enumerate(icons):
        bx = x + i * 92
        good = i < 3
        d.rectangle([bx - 4, y - 4, bx + 76, y + 76], fill=(14, 13, 16, 235), outline=(200, 190, 160, 255), width=3)
        d.rectangle([bx + 1, y + 1, bx + 71, y + 71], outline=(90, 200, 110, 255) if good else (210, 70, 60, 255), width=3)
        ic = Image.open(os.path.join(ROOT, "ImmortalHeroesAssets", n)).convert("RGBA").resize((64, 64), Image.LANCZOS)
        img.alpha_composite(ic, (bx + 4, y + 4))
        text(d, (bx + 36, y + 100), ["12s", "45s", "1m", "8s"][i], 24)

AC_KEYS = {"Warrior": "Warrior", "Sword Master": "SwordMaster", "Mercenary": "Mercenary", "Cleric": "Cleric", "Paladin": "Paladin",
           "Priest": "Priest", "Sorcerer": "Sorcerer", "Archmage": "Wizard", "Horizon Walker": "Spellcaster", "Ranger": "Ranger",
           "Acrobat": "Acrobat", "Bowmaster": "Bowmaster"}
EXPORT_SCALE = 0.5

def export(asset_dir):
    """In-game assets: HUD_Frame_<AC>.png (cleaned + recoloured, cropped, 50%), HUD_Bar_<Class>_<i>.png (painted fills)
    and HUD_Layout_<Class>.txt (every live element in frame-texture pixels)."""
    k = EXPORT_SCALE
    for cls in ["Warrior", "Cleric", "Sorcerer", "Ranger"]:
        base = build(cls)
        g = base["g"]
        ys, xs = np.nonzero(base["alpha"] > 8)
        cx0, cy0, cx1, cy1 = max(0, xs.min() - 4), max(0, ys.min() - 4), xs.max() + 5, ys.max() + 5
        def sc(v): return int(round(v * k))
        size = (sc(cx1 - cx0), sc(cy1 - cy0))
        targets = list(ACS[cls])
        if cls == "Warrior": targets.append(("None", (0, 0.0, 0.85)))
        for ac, target in targets:
            img = to_pil(recolor(base, target) if ac != "None" else greyscale(base), base["alpha"]).crop((cx0, cy0, cx1, cy1)).resize(size, Image.LANCZOS)
            img.save(os.path.join(asset_dir, "HUD_Frame_" + (AC_KEYS.get(ac, ac)) + ".png"))
        lines = ["# v0.25.68 HUD layout for " + cls + " (frame texture pixels, origin top-left)", "size %d %d" % size]
        for i, st in enumerate(base["strips"]):
            sim = to_pil(st["img"], st["alpha"])
            sim = sim.resize((max(4, sc(sim.width)), max(4, sc(sim.height))), Image.LANCZOS)
            sim.save(os.path.join(asset_dir, "HUD_Bar_%s_%d.png" % (cls, i)))
            lines.append("bar %d %d %d %d %d %d" % (i, sc(st["x0"] - cx0), sc(st["y0"] - cy0), sim.width, sim.height, sc(st["xmax"] - cx0)))
        x0, y0, x1, y1 = g["name"]
        lines.append("name %d %d %d %d" % (sc(x0 - cx0), sc(y0 - cy0), sc(x1 - x0), sc(y1 - y0)))
        if g["values"]:
            for i, (key, by0, by1, *_r) in enumerate(g["bars"]):
                lines.append("value %d %d %d" % (i, sc(g["value_x"] - cx0), sc((by0 + by1) / 2 - cy0)))
        for i, ((c, r), t) in enumerate(zip(g["food"], g["food_text"])):
            lines.append("food %d %d %d %d %d %d" % (i, sc(c[0] - cx0), sc(c[1] - cy0), sc(r), sc(t[0] - cx0), sc(t[1] - cy0)))
        bars = g["bars"]
        lines.append("buffs %d %d" % (sc((bars[0][3] + bars[0][5]) / 2 - cx0), sc(bars[0][1] - 22 - cy0)))
        open(os.path.join(asset_dir, "HUD_Layout_%s.txt" % cls), "w").write("\n".join(lines) + "\n")
        print("exported", cls, size)

def greyscale(base):
    hsv = cv2.cvtColor(base["rgb"], cv2.COLOR_BGR2HSV).astype(float)
    acc = hue_mask(hsv.astype(np.uint8), base["g"]["accent"], 45, 30) & ~base["keep"]
    acc = cv2.GaussianBlur(acc.astype(float), (5, 5), 0)[..., None]
    grey = cv2.cvtColor(cv2.cvtColor(base["rgb"], cv2.COLOR_BGR2GRAY), cv2.COLOR_GRAY2BGR).astype(float) * 0.85
    return (base["rgb"] * (1 - acc) + grey * acc).astype(np.uint8)

def main():
    if len(sys.argv) > 1 and sys.argv[1] == "--export":
        export(sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "ImmortalHeroesAssets"))
        return
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "docs", "previews", "PREVIEW_v0.25.68_HUD_Frames.png")
    asset_dir = sys.argv[2] if len(sys.argv) > 2 else None
    rows = []
    for cls in ["Warrior", "Cleric", "Sorcerer", "Ranger"]:
        base = build(cls)
        tiles = []
        for ac, target in ACS[cls]:
            hud = compose(base, ac, target)
            if asset_dir:
                to_pil(recolor(base, target), base["alpha"]).save(os.path.join(asset_dir, "HUD_Frame_" + ac.replace(" ", "") + ".png"))
            canvas = Image.new("RGBA", (hud.width, hud.height + 140), (0, 0, 0, 0))
            canvas.alpha_composite(hud, (0, 140))
            hb = canvas.getchannel("A").point(lambda v: 255 if v > 40 else 0).getbbox()
            canvas2 = canvas.copy()
            buffs_at(canvas2, (hb[0], 140 + base["g"]["bars"][0][1] - 22, hb[2], hb[3]))
            canvas = canvas2
            bb = canvas.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
            bb = (max(0, bb[0] - 10), max(0, bb[1] - 10), min(canvas.width, bb[2] + 10), min(canvas.height, bb[3] + 10))
            tiles.append((ac, canvas.crop(bb)))
        rows.append((cls, tiles))
    TW = 600
    scaled = [(cls, [(ac, t.resize((TW, int(t.height * TW / t.width)), Image.LANCZOS)) for ac, t in tiles]) for cls, tiles in rows]
    rowh = [max(t.height for _, t in tiles) + 50 for _, tiles in scaled]
    sheet = Image.new("RGB", (40 + 3 * (TW + 30), 90 + sum(rowh)), (12, 14, 18))
    d = ImageDraw.Draw(sheet)
    text(d, (30, 40), "IMMORTAL HUD - your four Base Class designs, recoloured per Advancement Class (live bars, name, food, buffs)", 22, (240, 230, 200), "lm")
    y = 80
    bgs = {"Warrior": "warrior", "Cleric": "cleric", "Sorcerer": "sorcerer", "Ranger": None}
    for (cls, tiles), rh in zip(scaled, rowh):
        for i, (ac, t) in enumerate(tiles):
            x = 30 + i * (TW + 30)
            scenep = os.path.join(ROOT, "docs/source_art/art_refresh/scenes", (bgs[cls] or "warrior") + ".jpg") if bgs[cls] else os.path.join(ROOT, "docs/source_art/ranger_kali/backgrounds/ranger.jpg")
            bg = Image.open(scenep).convert("RGB").resize((TW, t.height), Image.BILINEAR).filter(ImageFilter.GaussianBlur(6))
            bg = Image.blend(bg, Image.new("RGB", bg.size, (12, 14, 18)), 0.45)
            sheet.paste(bg, (x, y + 30))
            sheet.paste(t, (x, y + 30), t)
            text(d, (x + 4, y + 12), ac.upper(), 18, (230, 210, 160), "lm")
        y += rh
    sheet.save(out)
    print(out, sheet.size)

if __name__ == "__main__":
    main()
