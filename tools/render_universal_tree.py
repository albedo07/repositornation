"""v0.20.4 preview of the universal Skill Tree runtime composition (Python port of the C# steps):
Priest art blitted field -> field, frame overlays per skill colour, locked = mask-greyed openings.
Approximation: DejaVu Serif stands in for Valheim's Averia Serif; stars / tooltips not drawn.
Usage: python3 tools/render_universal_tree.py -> docs/previews/PREVIEW_v0.20.4_*.png
"""
import os
import numpy as np
from PIL import Image, ImageDraw

import build_tree_frames as tf
import render_states as rs

A, OUT = tf.A, rs.OUT
NAMES = {
    "Paladin": {"goddess_relic": "Goddess Relic", "judgement_hammer": "Judgement Hammer", "heavens_light": "Heaven's Light",
                "shield_charge": "Shield Charge", "fallen_angel": "Fallen Angel", "ray_of_hope": "Ray of Hope",
                "electric_smite": "Electric Smite"},
    "Priest": {"goddess_relic": "Lightning Relic", "judgement_hammer": "Holy Relic", "heavens_light": "Grand Sigil",
               "shield_charge": "Divine Intervention", "fallen_angel": "Grand Cross", "ray_of_hope": "Heaven's Judgement",
               "electric_smite": "Lightning Tempest"},
}
PRIEST_SLOT = {v: k for k, v in tf.PRIEST_DONOR_GUESS.items()}
ANCHORS = {"goddess_relic": (404, 247), "judgement_hammer": (405, 375), "heavens_light": (416, 499),
           "shield_charge": (579.5, 247), "fallen_angel": (716.5, 247), "ray_of_hope": (716, 375), "electric_smite": (907.5, 355)}
LABEL_W = {"goddess_relic": 74, "judgement_hammer": 103, "heavens_light": 77, "shield_charge": 71, "fallen_angel": 69,
           "ray_of_hope": 70, "electric_smite": 109}


def inpaint(img, box):
    x, y, w, h = [int(round(v)) for v in box]
    a = img[y:y + h, x:x + w].copy()
    inner = a[1:-1, 1:-1]
    inner[:] = np.concatenate([a[0], a[-1], a[:, 0], a[:, -1]]).mean(0)
    for _ in range(160):
        a[1:-1, 1:-1] = (a[1:-1, :-2] + a[1:-1, 2:] + a[:-2, 1:-1] + a[2:, 1:-1]) * 0.25
    img[y:y + h, x:x + w] = a


def chassis():
    img = np.asarray(Image.open(os.path.join(A, "Cleric_Paladin_PreAdvance.png")).convert("RGB"), dtype=np.float64) / 255.0
    img = img.copy()
    for slot, (cx, by) in ANCHORS.items():
        w = LABEL_W.get(slot, 74)
        inpaint(img, (cx - w / 2.0, by - 22, w, 21))
    return img


def stamp(img, slot, color):
    path = os.path.join(A, "Frame_%s_%s.png" % (slot, color))
    spr = np.asarray(Image.open(path).convert("RGBA"), dtype=np.float64) / 255.0
    x0, y0, x1, y1 = tf.outer(slot)
    al = spr[..., 3:4]
    img[y0:y1, x0:x1] = img[y0:y1, x0:x1] * (1 - al) + spr[..., :3] * al


def priest(img):
    art = Image.open(os.path.join(A, "Cleric_Priest_Artwork.png")).convert("RGB").resize((tf.W, tf.H), Image.LANCZOS)
    for pid, slot in tf.PRIEST_DONOR_GUESS.items():
        d = tf.PRIEST_DONOR_FIELDS[pid]
        f = tf.FIELDS[slot]
        patch = art.crop(d).resize((f[2] - f[0], f[3] - f[1]), Image.LANCZOS)
        img[f[1]:f[3], f[0]:f[2]] = np.asarray(patch, dtype=np.float64) / 255.0
    stamp(img, "shield_charge", "green")
    stamp(img, "ray_of_hope", "cyan")
    return img


def lock(img, slots):
    mask = np.asarray(Image.open(os.path.join(A, "Tree_LockMask.png")).getchannel("A"), dtype=np.float64) / 255.0
    grey = (img[..., 0] * 0.299 + img[..., 1] * 0.587 + img[..., 2] * 0.114) * 0.72
    out = img.copy()
    for slot in slots:
        x0, y0, x1, y1 = tf.outer(slot)
        m = mask[y0:y1, x0:x1, None]
        out[y0:y1, x0:x1] = img[y0:y1, x0:x1] * (1 - m) + grey[y0:y1, x0:x1, None] * m
    return out


def render(branch, locked, overlays=()):
    img = chassis()
    if branch == "Priest":
        img = priest(img)
    img = lock(img, locked)
    for slot, color in overlays:
        stamp(img, slot, color)
    pil = Image.fromarray((img.clip(0, 1) * 255).astype(np.uint8)).convert("RGBA")
    pad = rs.padlock(48)
    d = ImageDraw.Draw(pil)
    for slot, (cx, by) in ANCHORS.items():
        rs.text_c(d, cx, by - 19, NAMES[branch][slot], rs.font(10), (46, 36, 28))
        if slot in locked:
            x0, y0, x1, y1 = tf.FIELDS[slot]
            sz = 30 if slot == "electric_smite" else 24
            fx1, fy1 = x1 + 6, y1 + 6
            pil.alpha_composite(pad.resize((sz, sz), Image.LANCZOS), (fx1 - sz - 1, fy1 - sz - 1))
    return pil


def main():
    tf.donor_fields()
    adv = list(ANCHORS)
    shots = [
        ("Paladin_locked", render("Paladin", adv)),
        ("Priest_locked", render("Priest", adv)),
        ("Priest_unlocked", render("Priest", [])),
        ("Paladin_ascended", render("Paladin", [], [("righteous_strike", "magenta"), ("goddess_relic", "magenta"),
                                                    ("electric_smite", "red")])),
    ]
    for name, img in shots:
        img.convert("RGB").save(os.path.join(OUT, "PREVIEW_v0.20.4_" + name + ".png"))
    print("written", [n for n, _ in shots])


if __name__ == "__main__":
    main()
