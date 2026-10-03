"""Builds the Cleric -> Priest Skill Tree art on the SAME chrome as Cleric -> Paladin.

The Priest painting (docs/source_art/Cleric_Priest_Artwork.png, 1549x1015) was generated separately:
its frame, header and footer drift up to ~3 px from the approved Paladin reference, so copying its
whole Advancement panel leaves seams and shifts every node. Instead every Priest node (frame + icon +
nameplate), the PRIEST title and the Grace box are cut from the painting, auto-aligned to the matching
Paladin node slot (edge correlation on the frame) and composited onto the Paladin backdrop.
Result: one universal tree layout - every node slot, nameplate anchor, lock region and footer pixel
is identical between Paladin and Priest.

Usage: python3 tools/build_priest_assets.py   (after tools/build_ui_assets.py)
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_ui_assets as ui  # noqa: E402
import render_states as rs  # noqa: E402

ROOT = ui.ROOT
OUT = ui.OUT
SRC = os.path.join(ROOT, "docs", "source_art", "Cleric_Priest_Artwork.png")
K = 1549 / 1011.0

# Paladin slot -> Priest skill in the same slot.
SLOTS = [
    ("goddess_relic", "lightning_relic", "Icon_goddess_relic.png"),
    ("judgement_hammer", "holy_relic", "Icon_goddess_relic.png"),
    ("heavens_light", "grand_sigil", None),
    ("shield_charge", "divine_intervention", "Icon_holy_wave.png"),
    ("fallen_angel", "grand_cross", "Icon_holy_wave.png"),
    ("ray_of_hope", "heavens_judgement", "Icon_holy_wave.png"),
    ("electric_smite", "lightning_tempest", "Icon_electric_smite.png"),
]
# Half width of the nameplate patch (covers the wider of the two nameplates).
PLATE_HALF = {"goddess_relic": 62, "judgement_hammer": 70, "heavens_light": 62, "shield_charge": 74,
              "fallen_angel": 62, "ray_of_hope": 74, "electric_smite": 68}


def edges(img):
    g = np.asarray(img.convert("L"), dtype=np.float32)
    gx = np.zeros_like(g)
    gy = np.zeros_like(g)
    gx[:, 1:-1] = g[:, 2:] - g[:, :-2]
    gy[1:-1, :] = g[2:, :] - g[:-2, :]
    return np.hypot(gx, gy)


def sample(src, box, s, dx, dy):
    """Reference-space box -> the matching region of the Priest painting under (scale s about the
    box center, offset dx/dy in reference px)."""
    x0, y0, x1, y1 = box
    cx, cy = (x0 + x1) / 2.0, (y0 + y1) / 2.0
    w, h = x1 - x0, y1 - y0
    ext = ((cx + dx - w * s / 2) * K, (cy + dy - h * s / 2) * K, (cx + dx + w * s / 2) * K, (cy + dy + h * s / 2) * K)
    return src.transform((int(w), int(h)), Image.EXTENT, ext, Image.BICUBIC)


def align(pal, src, frame_box, pad=4, band=8):
    x0, y0, x1, y1 = frame_box
    box = (x0 - pad, y0 - pad, x1 + pad, y1 + pad)
    w, h = box[2] - box[0], box[3] - box[1]
    target = edges(pal.crop(box))
    mask = np.zeros((h, w), np.float32)
    b = band + pad
    mask[:b, :] = 1
    mask[-b:, :] = 1
    mask[:, :b] = 1
    mask[:, -b:] = 1
    a = (target * mask).ravel()
    a = a - a.mean()
    best = None
    for s in np.arange(0.96, 1.045, 0.01):
        for dx in np.arange(-4, 4.5, 0.5):
            for dy in np.arange(-5, 3.5, 0.5):
                c = (edges(sample(src, box, s, dx, dy)) * mask).ravel()
                c = c - c.mean()
                score = float((a * c).sum() / (np.sqrt((a * a).sum() * (c * c).sum()) + 1e-6))
                if best is None or score > best[0]:
                    best = (score, s, dx, dy)
    return best


def feather_mask(size, rects, feather):
    m = Image.new("L", size, 0)
    d = ImageDraw.Draw(m)
    for r in rects:
        d.rectangle(r, fill=255)
    return m.filter(ImageFilter.GaussianBlur(feather))


def transplant(dst, src, frame_box, plate, s, dx, dy, ultimate):
    """Paste the aligned Priest node (frame + glow + nameplate) over the Paladin node in the same slot."""
    cx, pb = plate
    half = PLATE_HALF_CUR[0]
    x0, y0, x1, y1 = frame_box
    region = (min(x0 - 12, cx - half), y0 - 14, max(x1 + 12, cx + half), pb + 5)
    patch = sample(src, region, s, dx, dy).convert("RGBA")
    rw, rh = patch.size
    ox, oy = region[0], region[1]
    frame_rect = (x0 - 8 - ox, y0 - 10 - oy, x1 + 8 - ox, y1 + 6 - oy)
    plate_rect = (cx - half - ox, pb - 30 - oy, cx + half - ox, pb + 3 - oy)
    feather = 2.5 if ultimate else 3.5
    mask = feather_mask((rw, rh), [frame_rect, plate_rect], feather)
    dst.paste(patch, (ox, oy), mask)


PLATE_HALF_CUR = [62]


def build():
    src = Image.open(SRC).convert("RGB")
    pre = Image.open(os.path.join(OUT, "Cleric_Paladin_PreAdvance.png")).convert("RGBA")
    out = pre.copy()
    report = []
    for pal_id, pri_id, _ in SLOTS:
        frame_box, plate = rs.NODES[pal_id]
        score, s, dx, dy = align(pre, src, frame_box)
        report.append("%s <- %s  score %.2f  scale %.2f  dx %.1f  dy %.1f" % (pal_id, pri_id, score, s, dx, dy))
        PLATE_HALF_CUR[0] = PLATE_HALF[pal_id]
        transplant(out, src, frame_box, plate, s, dx, dy, pal_id == "electric_smite")

    # Title: PRIEST (aligned on the crest + ribbon around it).
    score, s, dx, dy = align(pre, src, (512, 66, 592, 150))
    report.append("title  score %.2f  dx %.1f  dy %.1f" % (score, dx, dy))
    title_box = (594, 84, 778, 122)
    patch = sample(src, title_box, s, dx, dy).convert("RGBA")
    w, h = patch.size
    out.paste(patch, title_box[:2], feather_mask((w, h), [(4, 3, w - 5, h - 4)], 2.5))

    # Footer Grace box: Grand Sigil.
    score, s, dx, dy = align(pre, src, (663, 542, 721, 599), pad=3, band=6)
    report.append("grace  score %.2f  dx %.1f  dy %.1f" % (score, dx, dy))
    grace_box = (657, 536, 727, 604)
    patch = sample(src, grace_box, s, dx, dy).convert("RGBA")
    w, h = patch.size
    out.paste(patch, grace_box[:2], feather_mask((w, h), [(3, 3, w - 4, h - 4)], 2.0))

    out.save(os.path.join(OUT, "Cleric_Priest_Reference.png"))
    ui.locked_backdrop(out).save(os.path.join(OUT, "Cleric_Priest_Locked.png"))

    # Hotbar icons: the same slot frames as the Paladin icons, Priest interiors.
    for pal_id, pri_id, frame_file in SLOTS:
        frame_box = rs.NODES[pal_id][0]
        if frame_file is None:
            out.crop((662, 542, 721, 599)).save(os.path.join(OUT, "Icon_" + pri_id + ".png"))
            continue
        x0, y0, x1, y1 = frame_box
        inset = 22 if pal_id == "electric_smite" else 15
        ui.framed_icon(out, (x0 + inset, y0 + inset, x1 - inset, y1 - inset), frame_file, "Icon_" + pri_id + ".png")
    print("\n".join(report))
    print("Priest assets written to", OUT)


if __name__ == "__main__":
    build()
