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

FIELDS = {"lightning_zap": (175, 166, 229, 217), "righteous_strike": (175, 295, 228, 345), "holy_wave": (175, 420, 228, 469),
          "goddess_relic": (378, 166, 431, 217), "judgement_hammer": (379, 295, 433, 345), "heavens_light": (390, 418, 444, 469),
          "shield_charge": (553, 166, 605, 217), "fallen_angel": (690, 166, 743, 217), "ray_of_hope": (691, 295, 743, 345),
          "electric_smite": (863, 227, 952, 314)}
GRACE_INNER = (393, 421, 442, 467)
FOOTER_INNER = (671, 551, 712, 590)
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
]
CLERIC_KITS = [
    ("Paladin", ["lightning_zap", "righteous_strike", "holy_wave"],
     ["goddess_relic", "judgement_hammer", "shield_charge", "fallen_angel", "ray_of_hope"], "electric_smite", "heavens_light"),
    ("Priest", ["lightning_zap", "righteous_strike", "holy_wave"],
     ["lightning_relic", "holy_relic", "divine_intervention", "grand_cross", "heavens_judgement"], "lightning_tempest", "grand_sigil"),
]
SIGNATURES = {"moonlight_splitter", "crescent_cleave", "stomp", "circle_swing", "meteor_fall", "gravity_dominion", "arcane_phalanx",
              "afterimage_arsenal", "goddess_relic", "judgement_hammer", "lightning_relic", "holy_relic"}
GREEN = {"ray_of_hope", "holy_wave", "void_step"}
ULTIMATES = {"halfmoon_slash", "whirlwind", "elemental_cataclysm", "arcane_rupture", "electric_smite", "lightning_tempest"}
GRACES = {"knights_guidance", "battlecry", "clockwork", "rift_walker", "heavens_light", "grand_sigil"}

# Gradient maps (shadow, mid, light, highlight) + blend strength, per identity.
GRADES = {
    "warrior": None,                                                     # amber steel: the pack's own palette
    "cleric": None,                                                      # holy gold + blue lightning: untouched
    "swordmaster": ([(4, 8, 22), (34, 70, 160), (130, 185, 255), (238, 246, 255)], 0.80),
    "mercenary": ([(14, 4, 3), (125, 28, 14), (240, 112, 44), (255, 232, 196)], 0.55),
    "sorcerer": ([(9, 4, 20), (78, 30, 150), (186, 118, 255), (248, 234, 255)], 0.78),
    "archmage": ([(9, 4, 20), (84, 28, 150), (196, 116, 255), (250, 234, 255)], 0.75),
    "horizonwalker": ([(6, 4, 22), (66, 34, 160), (170, 128, 255), (244, 236, 255)], 0.75),
}
# Panel hue per identity (degrees) for the background re-hue and the wash tint.
HUES = {"warrior": 358, "swordmaster": 214, "mercenary": 14, "sorcerer": 276, "archmage": 284, "horizonwalker": 262}


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


def icon_art(sid, w, h, key):
    src = Image.open(os.path.join(PACK, "icons", "Icon_" + sid + ".png")).convert("RGB")
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


def paste_field(canvas, box, sid, key):
    canvas.paste(icon_art(sid, box[2] - box[0], box[3] - box[1], key), box[:2])


# ----------------------------------------------------------------------------------------------
# Background masks
# ----------------------------------------------------------------------------------------------
def frame_mask(size):
    m = Image.new("L", size, 0)
    for slot, f in FIELDS.items():
        pad = 13 if slot == "electric_smite" else 10
        sx, sy = f[0] - pad, f[1] - pad
        sprite = None
        for name in os.listdir(A):
            if name.startswith("Frame_" + slot + "_"):
                sprite = Image.open(os.path.join(A, name)).convert("RGBA")
                break
        if sprite is not None:
            alpha = sprite.split()[3].point(lambda v: 255 if v > 8 else 0)
            m.paste(255, (sx, sy), alpha)
            ImageDraw.Draw(m).rectangle([f[0], f[1], f[2] - 1, f[3] - 1], fill=255)
        else:
            ImageDraw.Draw(m).rectangle([sx, sy, f[2] + pad, f[3] + pad], fill=255)
    d = ImageDraw.Draw(m)
    # Signature / Ultimate badges at the top-right corners.
    for slot in ("goddess_relic", "judgement_hammer", "electric_smite"):
        f = FIELDS[slot]
        d.rectangle([f[2] - 12, f[1] - 16, f[2] + 14, f[1] + 10], fill=255)
    return m.filter(ImageFilter.MaxFilter(3))


def plate_mask(chassis, size):
    a = np.asarray(chassis.convert("RGB"), dtype=np.int32)
    L = a.sum(2) // 3
    m = Image.new("L", size, 0)
    d = ImageDraw.Draw(m)
    for slot, (ax, ay) in ANCHORS.items():
        ax, ay = int(round(ax)), int(ay)
        if slot == "electric_smite":
            # The Ultimate plate is larger and runs into the panel border (measured).
            d.polygon([(843, 322), (982, 322), (982, 358), (843, 358), (831, 340)], fill=255)
            continue
        row = ay - 19
        lx = ax
        while lx > 0 and L[row, lx - 1] >= 200:
            lx -= 1
        rx = ax
        while rx < a.shape[1] - 1 and L[row, rx + 1] >= 200:
            rx += 1
        top, bot, mid = ay - 27, ay + 2, ay - 13
        d.polygon([(lx - 8, top), (rx + 8, top), (rx + 15, mid), (rx + 8, bot), (lx - 8, bot), (lx - 15, mid)], fill=255)
    return m


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


def rehue(img, mask, left_hue, right_hue):
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
    gold = (hue >= 22) & (hue <= 62)
    chroma = (s > 0.10) & ~gold
    h, w = mx.shape
    target = np.where(np.arange(w)[None, :] < 323, left_hue, right_hue).astype(np.float64)
    target = np.broadcast_to(target, (h, w))
    nh = target / 60.0
    c = v * s
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
    sc = Image.open(os.path.join(PACK, "scenes", name + ".jpg")).convert("RGB")
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
    size = chassis.size
    protect = Image.fromarray(np.maximum.reduce([np.asarray(frame_mask(size)), np.asarray(plate_mask(chassis, size))]))
    base = rehue(chassis, rehue_region(size), HUES[class_key], HUES[ac_key])
    # Keep frames / plates exactly as painted (no re-hue on them).
    base.paste(chassis, (0, 0), protect)
    scene_layer = base.copy()
    scene_layer.paste(washed_scene(class_scene, (34, 142, 310, 516), HUES[class_key], 0.16), (34, 142))
    scene_layer.paste(washed_scene(ac_scene, (338, 142, 975, 516), HUES[ac_key], 0.55), (338, 142))
    # Scene fades in over 22 px from the panel edges (no rectangular seam) and hugs frames / plates.
    edge = static_mask(size).filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(11))
    edge = np.clip((np.asarray(edge, dtype=np.float64) - 0) * 1.6, 0, 255)
    near = np.asarray(protect.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(1.5)), dtype=np.float64)
    keep = np.maximum(edge, near)
    scene_alpha = Image.fromarray((255 - keep).astype(np.uint8))
    # Re-assert the hard masks after the feather so no plate / frame pixel changes.
    hard = Image.fromarray(np.maximum(np.asarray(static_mask(size)), np.asarray(protect)))
    out = base.copy()
    out.paste(scene_layer, (0, 0), scene_alpha)
    out.paste(base, (0, 0), hard.point(lambda v: 255 if v > 0 else 0))
    return out


# ----------------------------------------------------------------------------------------------
FRAME_FILE = {"cyan": "Icon_shield_charge.png", "navy": "Icon_goddess_relic.png", "green": "Icon_ray_of_hope.png",
              "maroon": "Icon_electric_smite.png", "gold": "Icon_heavens_light.png", "magenta": "Icon_righteous_strike.png"}


def frame_source(name):
    for folder in (OLD_FRAMES, KEEP):
        p = os.path.join(folder, name)
        if os.path.exists(p):
            return p
    return os.path.join(A, name)


def write_icon(sid, key, out_name=None, color=None):
    icon = Image.open(frame_source(FRAME_FILE[color or category(sid)])).convert("RGBA")
    iw, ih = icon.width - 14, icon.height - 16
    icon.paste(icon_art(sid, iw, ih, key).convert("RGBA"), (7, 8))
    icon.save(os.path.join(A, out_name or ("Icon_" + sid + ".png")))


def main():
    preview = sys.argv[sys.argv.index("--preview") + 1] if "--preview" in sys.argv else None
    if preview:
        os.makedirs(preview, exist_ok=True)
    chassis = Image.open(os.path.join(KEEP, "Cleric_Paladin_PreAdvance.png")).convert("RGB")
    for kit in KITS:
        cls, ac, cskills, adv, ult, grace, ckey, akey, cscene, ascene = kit
        out = build_background(chassis, ckey, akey, cscene, ascene)
        for slot, sid in zip(CLASS_SLOTS, cskills):
            paste_field(out, FIELDS[slot], sid, ckey)
        for slot, sid in zip(ADV_SLOTS, adv):
            paste_field(out, FIELDS[slot], sid, akey)
        paste_field(out, FIELDS["electric_smite"], ult, akey)
        paste_field(out, GRACE_INNER, grace, akey)
        paste_field(out, FOOTER_INNER, grace, akey)
        name = "%s_%s_Artwork.png" % (cls, ac)
        out.save(os.path.join(A, name))
        if preview:
            out.save(os.path.join(preview, name))
        for sid in cskills:
            write_icon(sid, ckey)
        for sid in adv + [ult, grace]:
            write_icon(sid, akey)
        print("wrote", name)

    # Cleric: Paladin canvases (field art + footer Grace) and the Priest donor painting.
    pal_slots = dict(zip(CLASS_SLOTS + ADV_SLOTS + ["electric_smite", "heavens_light"],
                         CLERIC_KITS[0][1] + CLERIC_KITS[0][2] + [CLERIC_KITS[0][3], CLERIC_KITS[0][4]]))
    for name in ("Cleric_Paladin_PreAdvance.png", "Cleric_Paladin_Reference.png"):
        img = Image.open(os.path.join(KEEP, name)).convert("RGBA")
        for slot, sid in pal_slots.items():
            box = GRACE_INNER if slot == "heavens_light" else FIELDS[slot]
            paste_field(img, box, sid, "cleric")
        paste_field(img, FOOTER_INNER, "heavens_light", "cleric")
        img.save(os.path.join(A, name))
        if preview and name.endswith("PreAdvance.png"):
            img.save(os.path.join(preview, name))
    pri = Image.open(os.path.join(KEEP, "Cleric_Priest_Artwork.png")).convert("RGB")
    sx, sy = pri.width / 1011.0, pri.height / 662.0
    donor = {"lightning_relic": (378, 164, 431, 215), "holy_relic": (379, 294, 433, 344), "grand_sigil": (389, 416, 443, 467),
             "divine_intervention": (553, 166, 605, 217), "grand_cross": (690, 166, 743, 217), "heavens_judgement": (691, 294, 743, 344),
             "lightning_tempest": (863, 226, 952, 313)}
    for sid, b in donor.items():
        if sid == "grand_sigil":
            b = (392, 419, 441, 465)   # opening only; the Priest Grace keeps its painted bevel
        box = tuple(int(round(v * s)) for v, s in zip(b, (sx, sy, sx, sy)))
        pri.paste(icon_art(sid, box[2] - box[0], box[3] - box[1], "cleric"), box[:2])
    fb = (394.8, 422.4, 394.8 + 44.4, 422.4 + 40.2)          # footer Grace source rect (IhLoadPriestArtwork)
    box = (int(round(fb[0] * sx)), int(round(fb[1] * sy)), int(round(fb[2] * sx)), int(round(fb[3] * sy)))
    pri.paste(icon_art("grand_sigil", box[2] - box[0], box[3] - box[1], "cleric"), box[:2])
    pri.save(os.path.join(A, "Cleric_Priest_Artwork.png"))
    for _, cskills, adv, ult, grace in CLERIC_KITS:
        for sid in cskills + adv + [ult, grace]:
            write_icon(sid, "cleric")
    write_icon("righteous_strike", "cleric", "Icon_righteous_strike_Normal.png", "cyan")
    write_icon("righteous_strike", "cleric", "Icon_righteous_strike.png", "magenta")
    print("wrote Cleric canvases, Priest artwork and 51 icons")


if __name__ == "__main__":
    main()
