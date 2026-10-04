"""v0.23.9 class art: the user's Art Refresh pack (docs/source_art/art_refresh, 51 painted icons +
9 scenes) installed on the universal chassis.

* Skill icons: every tree opening (all 6 ACs + Graces + footer Grace box) gets the painted icon,
  colour-graded to its Class identity (Warrior amber, Sword Master blue, Mercenary ember red,
  Cleric holy gold/blue = untouched, Sorcerer / Archmage / Horizon Walker purple).
* Backgrounds (Warrior / Sorcerer kits only; Cleric keeps its approved panels): panel paint is
  re-hued to the Class (left) / AC (right) colour, and the class scenes are washed into the panel
  interiors like the Cleric watercolours. Frames, plates, badges, borders, header and footer are
  masked out, so every anchor / plate / frame stays pixel-identical.
* Static Icon_<id>.png (Altar, fallback) for all 51 skills with the category frame.

Outputs (ImmortalHeroesAssets): <Class>_<AC>_Artwork.png (full canvas, read by IhBuildKitCanvas),
Cleric_Paladin_PreAdvance.png / Cleric_Paladin_Reference.png (field art), Cleric_Priest_Artwork.png
(donor rects), Icon_<id>.png. Originals: docs/source_art/backdrops_v0238.
Usage: python3 tools/build_class_art.py [--preview DIR]
"""
import colorsys
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
PACK = os.path.join(ROOT, "docs", "source_art", "art_refresh")
KEEP = os.path.join(ROOT, "docs", "source_art", "backdrops_v0238")
OLD_FRAMES = os.path.join(ROOT, "docs", "source_art", "backdrops_v0210")
# v0.24.2 Ranger art: the user's scenes (backgrounds/) + our icons from tools/build_ranger_icons.py.
RANGER = os.path.join(ROOT, "docs", "source_art", "ranger_kali")

FIELDS = {"lightning_zap": (175, 166, 229, 217), "righteous_strike": (175, 295, 228, 345), "holy_wave": (175, 420, 228, 469),
          "goddess_relic": (378, 166, 431, 217), "judgement_hammer": (379, 295, 433, 345), "heavens_light": (390, 418, 444, 469),
          "shield_charge": (553, 166, 605, 217), "fallen_angel": (690, 166, 743, 217), "ray_of_hope": (691, 295, 743, 345),
          "electric_smite": (863, 227, 952, 314)}
ANCHORS = {"lightning_zap": (201.5, 247), "righteous_strike": (201, 375), "holy_wave": (201, 499), "goddess_relic": (404, 247),
           "judgement_hammer": (405, 375), "heavens_light": (416, 499), "shield_charge": (579.5, 247), "fallen_angel": (716.5, 247),
           "ray_of_hope": (716, 375), "electric_smite": (907.5, 355)}
CLASS_SLOTS = ["lightning_zap", "righteous_strike", "holy_wave"]
ADV_SLOTS = ["goddess_relic", "judgement_hammer", "shield_charge", "fallen_angel", "ray_of_hope"]

# (Class, AC file name, class skills, AC skills, ultimate, grace, signatures, class grade, AC grade, class scene, AC scene)
KITS = [
    ("Warrior", "SwordMaster", ["heavy_slash", "impact_wave", "impact_punch"],
     ["moonlight_splitter", "crescent_cleave", "blade_storm", "frenzied_charge", "eclipse"], "halfmoon_slash", "knights_guidance",
     "warrior", "swordmaster", "warrior", "sword_master"),
    ("Warrior", "Mercenary", ["heavy_slash", "impact_wave", "impact_punch"],
     ["stomp", "circle_swing", "bonecrusher", "seismic_guillotine", "punishing_bomb"], "whirlwind", "battlecry",
     "warrior", "mercenary", "warrior", "mercenary"),
    ("Sorcerer", "Wizard", ["flame_burst", "glacial_descent", "stonefang_eruption"],
     ["meteor_fall", "gravity_dominion", "astral_railcannon", "astral_greatblade", "frost_nova"], "elemental_cataclysm", "clockwork",
     "sorcerer", "archmage", "sorcerer", "archmage"),
    ("Sorcerer", "Spellcaster", ["flame_burst", "glacial_descent", "stonefang_eruption"],
     ["arcane_phalanx", "afterimage_arsenal", "void_step", "rift_echo", "gravity_blast"], "arcane_rupture", "rift_walker",
     "sorcerer", "horizonwalker", "sorcerer", "horizon_walker"),
    # v0.24.2 Ranger: the user's Ranger / Acrobat / Bowmaster scenes, icons from build_ranger_icons.py.
    ("Ranger", "Acrobat", ["piercing_arrow", "tumble_shot", "snare_trap"],
     ["gale_volley", "cyclone_arrow", "swallow_dive", "skyfall_barrage", "ricochet_arrow"], "furious_winds", "tailwind",
     "ranger", "acrobat", "ranger", "acrobat"),
    ("Ranger", "Bowmaster", ["piercing_arrow", "tumble_shot", "snare_trap"],
     ["ballista_shot", "arrow_rain", "pinning_shot", "explosive_arrow", "splitting_arrow"], "starfall_volley", "hawks_vigil",
     "ranger", "bowmaster", "ranger", "bowmaster"),
]
CLERIC_KITS = [
    ("Paladin", ["lightning_zap", "righteous_strike", "holy_wave"],
     ["goddess_relic", "judgement_hammer", "shield_charge", "fallen_angel", "ray_of_hope"], "electric_smite", "heavens_light"),
    ("Priest", ["lightning_zap", "righteous_strike", "holy_wave"],
     ["lightning_relic", "holy_relic", "divine_intervention", "grand_cross", "heavens_judgement"], "lightning_tempest", "grand_sigil"),
]
SIGNATURES = {"moonlight_splitter", "crescent_cleave", "stomp", "circle_swing", "meteor_fall", "gravity_dominion", "arcane_phalanx",
              "afterimage_arsenal", "gale_volley", "cyclone_arrow", "ballista_shot", "arrow_rain", "goddess_relic", "judgement_hammer", "lightning_relic", "holy_relic"}
GREEN = {"ray_of_hope", "holy_wave", "void_step"}
ULTIMATES = {"furious_winds", "starfall_volley", "halfmoon_slash", "whirlwind", "elemental_cataclysm", "arcane_rupture", "electric_smite", "lightning_tempest"}
GRACES = {"tailwind", "hawks_vigil", "knights_guidance", "battlecry", "clockwork", "rift_walker", "heavens_light", "grand_sigil"}

# Gradient maps (shadow, mid, light, highlight) + blend strength, per identity.
GRADES = {
    "warrior": None,                                                     # amber steel: the pack's own palette
    "cleric": None,                                                      # holy gold + blue lightning: untouched
    "swordmaster": ([(4, 8, 22), (34, 70, 160), (130, 185, 255), (238, 246, 255)], 0.80),
    "mercenary": ([(14, 4, 3), (125, 28, 14), (240, 112, 44), (255, 232, 196)], 0.55),
    "sorcerer": ([(9, 4, 20), (78, 30, 150), (186, 118, 255), (248, 234, 255)], 0.78),
    "archmage": ([(9, 4, 20), (84, 28, 150), (196, 116, 255), (250, 234, 255)], 0.75),
    "horizonwalker": ([(6, 4, 22), (66, 34, 160), (170, 128, 255), (244, 236, 255)], 0.75),
    "ranger": ([(5, 12, 8), (46, 96, 40), (160, 220, 120), (240, 255, 228)], 0.35),
    "acrobat": ([(4, 12, 14), (30, 100, 92), (130, 235, 205), (236, 255, 250)], 0.40),
    "bowmaster": ([(8, 12, 4), (70, 96, 30), (196, 220, 110), (255, 250, 220)], 0.35),
}
# Panel hue per identity (degrees) for the background re-hue and the wash tint.
SATS = {"priest": 0.45}
HUES = {"ranger": 115, "acrobat": 170, "bowmaster": 95, "priest": 145, "warrior": 358, "swordmaster": 214, "mercenary": 14, "sorcerer": 276, "archmage": 284, "horizonwalker": 262}


def category(sid):
    if sid in GRACES:
        return "gold"
    if sid in ULTIMATES:
        return "maroon"
    if sid in SIGNATURES:
        return "navy"
    if sid in GREEN:
        return "green"
    return "cyan"


# ----------------------------------------------------------------------------------------------
def grade(img, key):
    g = GRADES.get(key)
    if g is None:
        return img
    stops, strength = g
    a = np.asarray(img.convert("RGB"), dtype=np.float64)
    lum = (a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114) / 255.0
    xs = np.array([0.0, 0.40, 0.75, 1.0])
    stops = np.array(stops, dtype=np.float64)
    mapped = np.stack([np.interp(lum, xs, stops[:, c]) for c in range(3)], -1)
    out = a * (1 - strength) + mapped * strength
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def icon_path(sid):
    for folder in (os.path.join(PACK, "icons"), os.path.join(RANGER, "icons")):
        p = os.path.join(folder, "Icon_" + sid + ".png")
        if os.path.exists(p):
            return p
    return None


def scene_path(name):
    p = os.path.join(PACK, "scenes", name + ".jpg")
    return p if os.path.exists(p) else os.path.join(RANGER, "backgrounds", name + ".jpg")


def icon_art(sid, w, h, key):
    src = Image.open(icon_path(sid)).convert("RGB")
    src = grade(src, key)
    # Cover-crop to the opening's aspect (centre), then downscale with a light sharpen.
    sw, sh = src.size
    asp = w / float(h)
    if sw / float(sh) > asp:
        cw, ch = int(round(sh * asp)), sh
    else:
        cw, ch = sw, int(round(sw / asp))
    x0, y0 = (sw - cw) // 2, (sh - ch) // 2
    out = src.crop((x0, y0, x0 + cw, y0 + ch)).resize((w, h), Image.LANCZOS)
    return out.filter(ImageFilter.UnsharpMask(radius=1.0, percent=60, threshold=2))


# ----------------------------------------------------------------------------------------------
# Exact masks (v0.23.10). Every frame / badge / plate keeps its painted pixels: art only fills the
# painted opening, the background only changes where nothing is drawn on top of it.
# ----------------------------------------------------------------------------------------------
HW_TO_LZ = 256   # Advanced.cs IhRepairLightningZapBackdrop: Zap's frame = Holy Wave's frame 256 px up


def sprite_for(slot):
    """(alpha image, origin) of the painted frame shape of a slot (Frame_<slot>_*.png)."""
    src = "holy_wave" if slot == "lightning_zap" else slot
    for name in sorted(os.listdir(A)):
        if name.startswith("Frame_" + src + "_"):
            sp = Image.open(os.path.join(A, name)).convert("RGBA").split()[3]
            f = FIELDS[src]
            pad = 13 if src == "electric_smite" else 10
            ox, oy = f[0] - pad, f[1] - pad
            if slot == "lightning_zap":
                oy -= HW_TO_LZ
            return sp, (ox, oy)
    return None, None


def corner_cut_rect(size, box, cut):
    m = Image.new("L", size, 0)
    x0, y0, x1, y1 = box
    ImageDraw.Draw(m).polygon([(x0 + cut, y0), (x1 - cut, y0), (x1, y0 + cut), (x1, y1 - cut), (x1 - cut, y1), (x0 + cut, y1),
                               (x0, y1 - cut), (x0, y0 + cut)], fill=255)
    return m


# Measured on the anchor chassis (dark opening inside the gold bevel, bevel corner pieces excluded).
GRACE_OPEN = (390, 418, 443, 468)
FOOTER_OPEN = (671, 552, 710, 590)


def badge_mask(size):
    """Painted Signature / Ultimate badges = Badge_Permanent.png at the live badge rect."""
    m = Image.new("L", size, 0)
    b = Image.open(os.path.join(A, "Badge_Permanent.png")).convert("RGBA").split()[3].resize((23, 23), Image.LANCZOS)
    for slot in ("goddess_relic", "judgement_hammer", "electric_smite"):
        f = FIELDS[slot]
        m.paste(255, (int(round(f[2] - 11.5)), int(round(f[1] - 17.5))), b.point(lambda v: 255 if v > 90 else 0))
    return m.filter(ImageFilter.MaxFilter(3))


def opening_mask(size, slot):
    if slot == "heavens_light":
        return corner_cut_rect(size, GRACE_OPEN, 4)
    if slot == "footer":
        return corner_cut_rect(size, FOOTER_OPEN, 3)
    sp, (ox, oy) = sprite_for(slot)
    full = Image.new("L", size, 255)
    full.paste(0, (ox, oy), sp.point(lambda v: 255 if v > 60 else 0))
    if slot == "lightning_zap":
        f = FIELDS["holy_wave"]
        f = (f[0], f[1] - HW_TO_LZ, f[2], f[3] - HW_TO_LZ)
    else:
        f = FIELDS[slot]
    rect = Image.new("L", size, 0)
    ImageDraw.Draw(rect).rectangle([f[0], f[1], f[2] - 1, f[3] - 1], fill=255)
    m = np.minimum(np.asarray(full), np.asarray(rect))
    m = np.minimum(m, 255 - np.asarray(badge_mask(size)))
    return Image.fromarray(m.astype(np.uint8))


def has_icon(sid):
    return icon_path(sid) is not None


def placeholder_art(w, h):
    """Same dark radial fill as Advanced.cs IhFillPlaceholder."""
    yy, xx = np.mgrid[0:h, 0:w]
    t = np.clip(np.hypot((xx + 0.5) / w - 0.5, (yy + 0.5) / h - 0.5) * 1.6, 0, 1)[..., None]
    inner, outer = np.array([51, 46, 41], np.float64), np.array([23, 20, 18], np.float64)
    return Image.fromarray((inner * (1 - t) + outer * t).astype(np.uint8))


def paste_art(canvas, slot, sid, key):
    m = opening_mask(canvas.size, slot)
    a = np.asarray(m)
    ys, xs = np.nonzero(a)
    x0, y0, x1, y1 = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    src = icon_art(sid, x1 - x0, y1 - y0, key) if has_icon(sid) else placeholder_art(x1 - x0, y1 - y0)
    art = np.asarray(src, dtype=np.float64)
    # Inner bevel shade like the painted icons: 2 px darker rim inside the opening.
    sub = m.crop((x0, y0, x1, y1))
    e1 = np.asarray(sub.filter(ImageFilter.MinFilter(3)), dtype=np.float64) / 255.0
    e2 = np.asarray(sub.filter(ImageFilter.MinFilter(5)), dtype=np.float64) / 255.0
    shade = 0.55 + 0.25 * e1 + 0.20 * e2
    art = np.clip(art * shade[..., None], 0, 255).astype(np.uint8)
    layer = Image.new(canvas.mode, canvas.size)
    layer.paste(Image.fromarray(art).convert(canvas.mode), (x0, y0))
    canvas.paste(layer, (0, 0), m)


def repair_lz(img):
    """Same repair the game applies at load (IhRepairLightningZapBackdrop): Zap's broken frame is
    rebuilt from Holy Wave's, boundary colour differences diffused inward."""
    a = np.asarray(img.convert("RGB"), dtype=np.float64)
    left, top, w, h = 156, 146, 92, 82
    ys = np.minimum(np.arange(top, top + h) + HW_TO_LZ, 480)
    art = a[ys][:, left:left + w].copy()
    cur = a[top:top + h, left:left + w]
    corr = np.zeros_like(art)
    edge = np.zeros((h, w), bool)
    edge[0, :] = edge[-1, :] = edge[:, 0] = edge[:, -1] = True
    corr[edge] = cur[edge] - art[edge]
    for _ in range(500):
        inner = (corr[:-2, 1:-1] + corr[2:, 1:-1] + corr[1:-1, :-2] + corr[1:-1, 2:]) * 0.25
        corr[1:-1, 1:-1] = inner
    out = a.copy()
    out[top + 1:top + h - 1, left + 1:left + w - 1] = (art + corr)[1:-1, 1:-1]
    res = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
    return res.convert(img.mode) if img.mode != "RGB" else res


# ----------------------------------------------------------------------------------------------
# Background masks
# ----------------------------------------------------------------------------------------------
def frame_mask(size):
    m = Image.new("L", size, 0)
    for slot in FIELDS:
        if slot == "heavens_light":
            continue
        sp, (ox, oy) = sprite_for(slot)
        m.paste(255, (ox, oy), sp.point(lambda v: 255 if v > 8 else 0))
    # Heaven's Light gold box: measured outer bevel incl. its corner ornaments.
    ImageDraw.Draw(m).rectangle([386, 413, 448, 473], fill=255)
    for slot in FIELDS:
        if slot == "heavens_light":
            continue
        f = FIELDS[slot]
        if slot == "lightning_zap":
            f = FIELDS["holy_wave"]
            f = (f[0], f[1] - HW_TO_LZ, f[2], f[3] - HW_TO_LZ)
        ImageDraw.Draw(m).rectangle([f[0], f[1], f[2] - 1, f[3] - 1], fill=255)
    m = Image.fromarray(np.maximum(np.asarray(m), np.asarray(badge_mask(size))))
    return m.filter(ImageFilter.MaxFilter(3))


def plate_edges(L, ax, ay):
    """Outer side edges of a nameplate: the first column (searching inward from outside) whose
    rows ay-16..ay-8 are mostly the dark plate outline."""
    rows = L[ay - 16:ay - 7]
    def side(rng, inward):
        for x in rng:
            if (rows[:, x] < 168).sum() >= 6 and (rows[:, x + 3 * inward] >= 195).sum() >= 7:
                return x
        return None
    return side(range(ax - 85, ax), 1), side(range(ax + 85, ax, -1), -1)


def plate_polygon(L, slot):
    if slot == "electric_smite":
        return [(842, 325), (982, 325), (982, 355), (842, 355), (835, 349), (835, 331)]
    ax, ay = ANCHORS[slot]
    ax, ay = int(round(ax)), int(ay)
    xl, xr = plate_edges(L, ax, ay)
    top, bot, c = ay - 24, ay, 7
    return [(xl + c, top), (xr - c, top), (xr, top + c), (xr, bot - c), (xr - c, bot), (xl + c, bot), (xl, bot - c), (xl, top + c)]


def plate_mask(chassis, size):
    L = np.asarray(chassis.convert("L"), dtype=np.int32)
    m = Image.new("L", size, 0)
    for slot in ANCHORS:
        ImageDraw.Draw(m).polygon(plate_polygon(L, slot), fill=255)
    return m.filter(ImageFilter.MaxFilter(3))


def static_mask(size):
    """Everything outside the two panel interiors (borders, header, footer, divider, banners)."""
    m = Image.new("L", size, 255)
    d = ImageDraw.Draw(m)
    d.rectangle([34, 142, 310, 516], fill=0)      # Class panel interior
    d.rectangle([338, 142, 975, 516], fill=0)     # AC panel interior
    return m


def rehue_region(size):
    """Where panel paint (incl. banners, tracery, ribbons) is re-hued: both panels, below the title bar."""
    m = Image.new("L", size, 0)
    d = ImageDraw.Draw(m)
    d.rectangle([26, 68, 318, 522], fill=255)
    d.rectangle([328, 52, 988, 522], fill=255)
    return m


def rehue(img, mask, left_hue, right_hue, left_sat=0.8, right_sat=0.8):
    a = np.asarray(img.convert("RGB"), dtype=np.float64) / 255.0
    mx, mn = a.max(2), a.min(2)
    v = mx
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    hue = np.zeros_like(mx)
    dlt = np.maximum(mx - mn, 1e-6)
    hue = np.where(mx == r, ((g - b) / dlt) % 6, hue)
    hue = np.where(mx == g, (b - r) / dlt + 2, hue)
    hue = np.where(mx == b, (r - g) / dlt + 4, hue)
    hue = hue * 60.0
    # Gold ornaments (saturated gold) keep their colour; warm paper / paint gets the class hue.
    gold = (hue >= 22) & (hue <= 62) & (s > 0.40) & (v > 0.45)
    chroma = (s > 0.08) & ~gold
    h, w = mx.shape
    target = np.where(np.arange(w)[None, :] < 323, left_hue, right_hue).astype(np.float64)
    target = np.broadcast_to(target, (h, w))
    nh = target / 60.0
    sat_scale = np.broadcast_to(np.where(np.arange(w)[None, :] < 323, left_sat, right_sat), (h, w))
    c = v * s * sat_scale
    x = c * (1 - np.abs(nh % 2 - 1))
    m0 = v - c
    i = np.floor(nh).astype(int) % 6
    rr = np.choose(i, [c, x, 0 * c, 0 * c, x, c])
    gg = np.choose(i, [x, c, c, x, 0 * c, 0 * c])
    bb = np.choose(i, [0 * c, 0 * c, x, c, c, x])
    new = np.stack([rr + m0, gg + m0, bb + m0], -1)
    sel = (np.asarray(mask, dtype=np.float64) / 255.0)[..., None] * chroma[..., None]
    out = a * (1 - sel) + new * sel
    return Image.fromarray(np.clip(out * 255, 0, 255).astype(np.uint8))


def hue_rgb(hue, s, v):
    return tuple(int(c * 255) for c in colorsys.hsv_to_rgb(hue / 360.0, s, v))


def washed_scene(name, box, hue, focus):
    """Scene cover-cropped into box, washed light like the Cleric watercolour panels."""
    w, h = box[2] - box[0] + 1, box[3] - box[1] + 1
    sc = Image.open(scene_path(name)).convert("RGB")
    sw, sh = sc.size
    asp = w / float(h)
    if sw / float(sh) > asp:
        cw, ch = int(sh * asp), sh
        x0 = int(np.clip(focus * sw - cw / 2.0, 0, sw - cw))
        crop = sc.crop((x0, 0, x0 + cw, ch))
    else:
        cw, ch = sw, int(sw / asp)
        crop = sc.crop((0, sh - ch, cw, sh))
    s = np.asarray(crop.resize((w, h), Image.LANCZOS), dtype=np.float64)
    paper = np.array(hue_rgb(hue, 0.10, 0.93), dtype=np.float64)
    # Screen-lighten towards tinted parchment, keep the painting readable but soft.
    lifted = 255 - (255 - s) * (255 - paper * 0.40) / 255.0
    out = lifted * 0.74 + paper * 0.26
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def build_background(chassis, class_key, ac_key, class_scene, ac_scene):
    """class_key None = keep the Class (left) panel exactly as painted (Priest)."""
    size = chassis.size
    protect = Image.fromarray(np.maximum(np.asarray(frame_mask(size)), np.asarray(plate_mask(chassis, size))))
    region = rehue_region(size)
    if class_key is None:
        ImageDraw.Draw(region).rectangle([0, 0, 322, 661], fill=0)
    base = rehue(chassis, region, HUES.get(class_key, 0), HUES[ac_key], SATS.get(class_key, 0.8), SATS.get(ac_key, 0.8))
    # Frames / plates / badges keep their painted pixels (no re-hue on them).
    base.paste(chassis, (0, 0), protect)
    scene_layer = base.copy()
    if class_key is not None and class_scene is not None:
        scene_layer.paste(washed_scene(class_scene, (34, 142, 310, 516), HUES[class_key], 0.16), (34, 142))
    if ac_scene is not None:
        scene_layer.paste(washed_scene(ac_scene, (338, 142, 975, 516), HUES[ac_key], 0.55), (338, 142))
    # Scene fades in over ~22 px from the panel edges (no rectangular seam) and stops exactly at
    # the frame / plate / badge mattes (1 px soft edge, no halo patches).
    edge = static_mask(size).filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(7))
    edge = np.clip(np.asarray(edge, dtype=np.float64) * 1.6, 0, 255)
    near = np.asarray(protect.filter(ImageFilter.GaussianBlur(0.8)), dtype=np.float64)
    keep = np.maximum(edge, near)
    if class_key is None:
        keep[:, :323] = 255
    out = base.copy()
    out.paste(scene_layer, (0, 0), Image.fromarray((255 - keep).astype(np.uint8)))
    out.paste(base, (0, 0), protect)
    return out


def finish_header(img):
    """v0.23.10: the tree is no longer a prototype. 'SKILL TREE - UI PROTOTYPE' becomes 'SKILL TREE',
    re-centred under IMMORTAL HEROES (title x 107-399)."""
    a = np.asarray(img.convert("RGB"), dtype=np.float64)
    words = a[46:60, 124:219].copy()
    x0, y0, x1, y1 = 120, 46, 364, 60
    region = a[y0:y1, x0:x1].copy()
    edge = np.zeros(region.shape[:2], bool)
    edge[0, :] = edge[-1, :] = edge[:, 0] = edge[:, -1] = True
    fill = region.copy()
    fill[~edge] = region[edge].mean(0)
    for _ in range(400):
        fill[1:-1, 1:-1] = (fill[:-2, 1:-1] + fill[2:, 1:-1] + fill[1:-1, :-2] + fill[1:-1, 2:]) * 0.25
    a[y0:y1, x0:x1] = fill
    # Paste the painted "SKILL TREE" lettering (lighter than the band) at the new centre.
    nx = int(round((107 + 399) / 2.0 - words.shape[1] / 2.0))
    band = a[46:60, nx:nx + words.shape[1]]
    lum_w = words.max(2, keepdims=True)
    lum_b = band.max(2, keepdims=True)
    alpha = np.clip((lum_w - lum_b - 12) / 60.0, 0, 1)
    a[46:60, nx:nx + words.shape[1]] = band * (1 - alpha) + words * alpha
    res = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    return res.convert(img.mode) if img.mode != "RGB" else res


# ----------------------------------------------------------------------------------------------
FRAME_FILE = {"cyan": "Icon_shield_charge.png", "navy": "Icon_goddess_relic.png", "green": "Icon_ray_of_hope.png",
              "maroon": "Icon_electric_smite.png", "gold": "Icon_heavens_light.png", "magenta": "Icon_righteous_strike.png"}
# Measured painted openings of the static frames (x0, y0, x1, y1 exclusive).
ICON_OPEN = {"Icon_heavens_light.png": (9, 10, 49, 49)}
ICON_OPEN_DEFAULT = (5, 6, 44, 48)


def frame_source(name):
    for folder in (OLD_FRAMES, KEEP):
        p = os.path.join(folder, name)
        if os.path.exists(p):
            return p
    return os.path.join(A, name)


def write_icon(sid, key, out_name=None, color=None):
    if not has_icon(sid):
        return   # no painting yet: the game composes placeholder initials
    fname = FRAME_FILE[color or category(sid)]
    icon = Image.open(frame_source(fname)).convert("RGBA")
    x0, y0, x1, y1 = ICON_OPEN.get(fname, ICON_OPEN_DEFAULT)
    icon.paste(icon_art(sid, x1 - x0, y1 - y0, key).convert("RGBA"), (x0, y0))
    icon.save(os.path.join(A, out_name or ("Icon_" + sid + ".png")))


def fill_tree(img, cskills, adv, ult, grace, ckey, akey):
    for slot, sid in zip(CLASS_SLOTS, cskills):
        paste_art(img, slot, sid, ckey)
    for slot, sid in zip(ADV_SLOTS, adv):
        paste_art(img, slot, sid, akey)
    paste_art(img, "electric_smite", ult, akey)
    paste_art(img, "heavens_light", grace, akey)
    paste_art(img, "footer", grace, akey)


def main():
    preview = sys.argv[sys.argv.index("--preview") + 1] if "--preview" in sys.argv else None
    if preview:
        os.makedirs(preview, exist_ok=True)
    raw = Image.open(os.path.join(KEEP, "Cleric_Paladin_PreAdvance.png")).convert("RGB")
    chassis = repair_lz(raw)
    for kit in KITS:
        cls, ac, cskills, adv, ult, grace, ckey, akey, cscene, ascene = kit
        out = finish_header(build_background(chassis, ckey, akey, cscene, ascene))
        fill_tree(out, cskills, adv, ult, grace, ckey, akey)
        name = "%s_%s_Artwork.png" % (cls, ac)
        out.save(os.path.join(A, name))
        if preview:
            out.save(os.path.join(preview, name))
        for sid in cskills:
            write_icon(sid, ckey)
        for sid in adv + [ult, grace]:
            write_icon(sid, akey)
        print("wrote", name)

    # Cleric. Paladin keeps its painted panels; the game repairs Zap's frame itself at load.
    _, pc, pa, pu, pg = CLERIC_KITS[0]
    for name in ("Cleric_Paladin_PreAdvance.png", "Cleric_Paladin_Reference.png"):
        img = finish_header(Image.open(os.path.join(KEEP, name)).convert("RGBA"))
        fill_tree(img, pc, pa, pu, pg, "cleric", "cleric")
        img.save(os.path.join(A, name))
        if preview and name.endswith("PreAdvance.png"):
            img.save(os.path.join(preview, name))
    # Priest gets its own AC panel (priest scene, jade-gold), Cleric panel unchanged.
    _, qc, qa, qu, qg = CLERIC_KITS[1]
    pri_bg = finish_header(build_background(chassis, None, "priest", None, "priest"))
    fill_tree(pri_bg, qc, qa, qu, qg, "cleric", "cleric")
    pri_bg.save(os.path.join(A, "Cleric_Priest_Backdrop.png"))
    if preview:
        pri_bg.save(os.path.join(preview, "Cleric_Priest_Backdrop.png"))
    # The Priest donor painting (still read by IhLoadPriestArtwork) gets the same field art.
    pri = Image.open(os.path.join(KEEP, "Cleric_Priest_Artwork.png")).convert("RGB")
    sx, sy = pri.width / 1011.0, pri.height / 662.0
    donor = {"lightning_relic": "goddess_relic", "holy_relic": "judgement_hammer", "grand_sigil": "heavens_light",
             "divine_intervention": "shield_charge", "grand_cross": "fallen_angel", "heavens_judgement": "ray_of_hope",
             "lightning_tempest": "electric_smite"}
    rects = {"lightning_relic": (378, 164, 431, 215), "holy_relic": (379, 294, 433, 344), "grand_sigil": (389, 416, 443, 467),
             "divine_intervention": (553, 166, 605, 217), "grand_cross": (690, 166, 743, 217), "heavens_judgement": (691, 294, 743, 344),
             "lightning_tempest": (863, 226, 952, 313)}
    for sid, b in rects.items():
        # Render the slot exactly as the backdrop shows it, then resample into the donor rect.
        crop = pri_bg.crop(FIELDS[donor[sid]])   # the game blits donor rect -> this field rect
        box = tuple(int(round(v * s)) for v, s in zip(b, (sx, sy, sx, sy)))
        pri.paste(crop.resize((box[2] - box[0], box[3] - box[1]), Image.LANCZOS), box[:2])
    fb = (394.8, 422.4, 394.8 + 44.4, 422.4 + 40.2)
    box = (int(round(fb[0] * sx)), int(round(fb[1] * sy)), int(round(fb[2] * sx)), int(round(fb[3] * sy)))
    pri.paste(pri_bg.crop((394, 422, 439, 462)).resize((box[2] - box[0], box[3] - box[1]), Image.LANCZOS), box[:2])
    pri.save(os.path.join(A, "Cleric_Priest_Artwork.png"))
    for _, cskills, adv, ult, grace in CLERIC_KITS:
        for sid in cskills + adv + [ult, grace]:
            write_icon(sid, "cleric")
    write_icon("righteous_strike", "cleric", "Icon_righteous_strike_Normal.png", "cyan")
    write_icon("righteous_strike", "cleric", "Icon_righteous_strike.png", "magenta")
    print("wrote Cleric canvases, Priest backdrop + artwork and 51 icons")


if __name__ == "__main__":
    main()
