"""Builds the Dragon's Altar UI art from the approved concept (docs/source_art/Altar_Concept.png, 802x687).

The concept is the reference, exactly like Cleric_Paladin_Reference is for the Skill Tree: everything that
stays the same (frame, DRAGON'S ALTAR header, Reset/Close, status plate, BASE CLASSES panel and its three
class cards, right panel, STARTER KIT / ADVANCEMENTS labels, button plates) stays baked. Everything that
changes per class is erased and drawn live by AlbedosCustomClasses.Core.cs: status values, class title /
role, description, starter-kit icons, advancement emblems, button labels, the selected-card highlight.

All coordinates below are concept pixels (802x687). Output is upscaled 2x for in-game sharpness and must
stay in sync with the Altar* rects in AlbedosCustomClasses.Core.cs.
Usage: python3 tools/build_altar_assets.py
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "source_art", "Altar_Concept.png")
OUT = os.path.join(ROOT, "ImmortalHeroesAssets")
UP = 2  # output scale

CARDS = {"Warrior": (49, 240, 323, 350), "Cleric": (49, 360, 323, 470), "Sorcerer": (49, 482, 324, 593)}
SELECTED_FRAME = (39, 352, 337, 478)  # the Cleric card's highlighted frame incl. side diamonds
MEDALLIONS = {"Warrior": (60, 247, 137, 343), "Cleric": (58, 367, 137, 464), "Sorcerer": (58, 489, 137, 586)}
CARD_TEXT = (140, 266, 300, 330)  # name + role area inside a card (relative to the Warrior card row)
EMBLEMS = {"Paladin": (602, 508, 648, 554), "Priest": (681, 508, 727, 554)}
BUTTON_CHOOSE = (365, 575, 567, 622)
BUTTON_VIEW = (577, 576, 750, 620)
BOTTOM_LEFT = (356, 480, 566, 572)
# Confirmation dialog = right panel header + parchment (y 152-330) joined to its button row + bottom frame (y 566-632).
DIALOG_TOP = (350, 152, 775, 330)
DIALOG_BOTTOM = (350, 566, 775, 632)
DIALOG_JOIN = 6


def lum(a):
    return a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114


def erase_text(img, box, dark=True, size=9, thresh=0.07, grow=2, blur=3):
    """Remove text from a box: dark text on light paper (dark=True) or light text on a dark band."""
    x0, y0, x1, y1 = box
    reg = img.crop(box).convert("RGB")
    bg = reg.filter(ImageFilter.MaxFilter(size) if dark else ImageFilter.MinFilter(size))
    bg = bg.filter(ImageFilter.MinFilter(3) if dark else ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(blur))
    a = np.asarray(reg, dtype=np.float32) / 255.0
    b = np.asarray(bg, dtype=np.float32) / 255.0
    diff = (lum(b) - lum(a)) if dark else (lum(a) - lum(b))
    m = Image.fromarray(((diff > thresh) * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(grow * 2 + 1)).filter(ImageFilter.GaussianBlur(1.2))
    m = np.asarray(m, dtype=np.float32)[..., None] / 255.0
    out = a * (1 - m) + b * m
    img.paste(Image.fromarray((out.clip(0, 1) * 255).astype(np.uint8)).convert(img.mode), (x0, y0))
    return img


def inpaint_h(img, box, grain=1.0, seed=3):
    x0, y0, x1, y1 = box
    a = np.asarray(img.convert("RGB"), dtype=np.float32).copy()
    left = a[y0:y1, x0 - 2:x0].mean(axis=1)
    right = a[y0:y1, x1:x1 + 2].mean(axis=1)
    t = (np.arange(x1 - x0, dtype=np.float32) + 0.5) / (x1 - x0)
    fill = left[:, None, :] * (1 - t[None, :, None]) + right[:, None, :] * t[None, :, None]
    fill += np.random.default_rng(seed).normal(0, grain, fill.shape[:2])[:, :, None]
    a[y0:y1, x0:x1] = fill.clip(0, 255)
    return Image.fromarray(a.astype(np.uint8)).convert(img.mode)


def parchment_fill(img, box, feather=8):
    """Text-free parchment for a text block: the area's own tone (text removed, heavily blurred) + paper noise."""
    x0, y0, x1, y1 = box
    pad = 24
    big = (x0 - pad, y0 - pad, x1 + pad, y1 + pad)
    tmp = erase_text(img.copy(), big, dark=True, size=11, thresh=0.05, grow=3)
    tone = tmp.crop(big).convert("RGB").filter(ImageFilter.GaussianBlur(14)).crop((pad, pad, pad + x1 - x0, pad + y1 - y0))
    t = np.asarray(tone, dtype=np.float32)
    h, w = t.shape[:2]
    rng = np.random.default_rng(7)
    noise = rng.normal(0, 1, (h, w)).astype(np.float32)
    noise = np.asarray(Image.fromarray(((noise * 40) + 128).clip(0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7)),
                       dtype=np.float32) - 128.0
    out = (t + noise[..., None] * 0.10).clip(0, 255)
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).rectangle((feather, feather, w - 1 - feather, h - 1 - feather), fill=255)
    m = m.filter(ImageFilter.GaussianBlur(feather / 2.0))
    img.paste(Image.fromarray(out.astype(np.uint8)).convert(img.mode), (x0, y0), m)
    return erase_text(img, box, dark=True, size=9)


def ring_mask(size, inset, feather=0.8):
    w, h = size
    m = Image.new("L", (w, h), 255)
    ImageDraw.Draw(m).rectangle((inset, inset, w - 1 - inset, h - 1 - inset), fill=0)
    return m.filter(ImageFilter.GaussianBlur(feather))


def unselect_cleric(img):
    """The concept shows Cleric selected. Rebuild its plain frame from the Warrior card (identical frame art)."""
    wx0, wy0, wx1, wy1 = CARDS["Warrior"]
    cx0, cy0, cx1, cy1 = CARDS["Cleric"]
    dy = cy0 - wy0
    # 1) margin around the card (glow + side diamonds) <- the clean background beside the Warrior card
    sx0, sy0, sx1, sy1 = SELECTED_FRAME
    margin = img.crop((sx0, sy0 - dy, sx1, sy1 - dy))
    m = Image.new("L", margin.size, 255)
    ImageDraw.Draw(m).rectangle((cx0 - sx0 + 2, cy0 - sy0 + 2, cx1 - sx0 - 2, cy1 - sy0 - 2), fill=0)
    img.paste(margin, (sx0, sy0), m.filter(ImageFilter.GaussianBlur(0.8)))
    # 2) frame band <- Warrior's frame band
    band = img.crop(CARDS["Warrior"])
    img.paste(band, (cx0, cy0), ring_mask(band.size, 9))
    return img


def highlight_sprite(src):
    """Selected-card frame (gold glow, thick frame, side diamonds) as a transparent overlay."""
    sx0, sy0, sx1, sy1 = SELECTED_FRAME
    cx0, cy0, cx1, cy1 = CARDS["Cleric"]
    spr = src.crop(SELECTED_FRAME).convert("RGBA")
    m = Image.new("L", spr.size, 255)
    ImageDraw.Draw(m).rectangle((cx0 - sx0 + 10, cy0 - sy0 + 10, cx1 - sx0 - 10, cy1 - sy0 - 10), fill=0)
    # keep the background between the frame and the sprite edge only where it glows (bright, gold)
    a = np.asarray(spr.convert("RGB"), dtype=np.float32) / 255.0
    mx, mn = a.max(2), a.min(2)
    gold = (a[..., 0] > a[..., 2] + 0.12) & (a[..., 1] > a[..., 2] + 0.02) & (mx > 0.45)
    inner = np.zeros(gold.shape, bool)
    inner[cy0 - sy0 - 1:cy1 - sy0 + 1, cx0 - sx0 - 1:cx1 - sx0 + 1] = True
    keep = (np.asarray(m) > 0) & gold
    alpha = Image.fromarray((keep * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.7))
    spr.putalpha(alpha)
    return spr


def circle_sprite(img, box):
    spr = img.crop(box).convert("RGBA").resize(((box[2] - box[0]) * 4, (box[3] - box[1]) * 4), Image.LANCZOS)
    m = Image.new("L", spr.size, 0)
    ImageDraw.Draw(m).ellipse((2, 2, spr.width - 3, spr.height - 3), fill=255)
    spr.putalpha(m.filter(ImageFilter.GaussianBlur(2)))
    return spr.resize((box[2] - box[0], box[3] - box[1]), Image.LANCZOS)


def hue_shift(spr, degrees, sat=1.0):
    rgba = np.asarray(spr.convert("RGBA"), dtype=np.float32)
    hsv = np.asarray(Image.fromarray(rgba[..., :3].astype(np.uint8)).convert("HSV"), dtype=np.float32)
    hsv[..., 0] = (hsv[..., 0] + degrees / 360.0 * 255.0) % 255.0
    hsv[..., 1] = (hsv[..., 1] * sat).clip(0, 255)
    rgb = np.asarray(Image.fromarray(hsv.astype(np.uint8), "HSV").convert("RGB"), dtype=np.float32)
    out = np.concatenate([rgb, rgba[..., 3:4]], axis=2)
    return Image.fromarray(out.astype(np.uint8), "RGBA")


def dialog(img):
    top = img.crop(DIALOG_TOP).convert("RGBA")
    bot = img.crop(DIALOG_BOTTOM).convert("RGBA")
    out = Image.new("RGBA", (top.width, top.height + bot.height - DIALOG_JOIN), (0, 0, 0, 0))
    out.paste(top, (0, 0))
    m = Image.new("L", bot.size, 255)
    d = ImageDraw.Draw(m)
    for y in range(DIALOG_JOIN):
        d.line((0, y, bot.width, y), fill=int(255 * (y + 1) / (DIALOG_JOIN + 1)))
    out.paste(bot, (0, top.height - DIALOG_JOIN), m)
    return out


def emblem_glow(size):
    """Gold halo ring drawn behind the selected advancement emblem."""
    S = size * 4
    yy, xx = np.mgrid[0:S, 0:S]
    r = np.sqrt((xx - S / 2) ** 2 + (yy - S / 2) ** 2) / (S / 2)
    a = np.exp(-((r - 0.78) / 0.14) ** 2) * 255
    rgba = np.zeros((S, S, 4), np.float32)
    rgba[..., 0], rgba[..., 1], rgba[..., 2] = 255, 214, 120
    rgba[..., 3] = a
    return Image.fromarray(rgba.clip(0, 255).astype(np.uint8), "RGBA").resize((size, size), Image.LANCZOS)


def up(img):
    return img.resize((img.width * UP, img.height * UP), Image.LANCZOS).filter(ImageFilter.UnsharpMask(1.2, 60, 2))


def save(img, name):
    up(img).save(os.path.join(OUT, name))


def build():
    src = Image.open(SRC).convert("RGB")
    img = src.copy()
    img = unselect_cleric(img)

    # Dynamic text -> erased (drawn live).
    img = inpaint_h(img, (302, 126, 346, 145), grain=0.8)            # Base class value
    img = inpaint_h(img, (512, 126, 556, 145), grain=0.8)            # Advancement value
    img = inpaint_h(img, (493, 163, 603, 191), grain=0.8)            # right header title (between the stars)
    img = inpaint_h(img, (462, 191, 642, 207), grain=0.8)            # right header role line
    img = parchment_fill(img, (358, 220, 650, 468), feather=12)                  # description (headings + body)
    img = erase_text(img, (640, 220, 674, 468), dark=True, size=9)     # line ends over the statue
    img = inpaint_h(img, (368, 506, 562, 568), grain=1.2)            # starter kit icons + labels
    img = inpaint_h(img, (596, 504, 734, 570), grain=1.2)            # advancement emblems + labels
    img = inpaint_h(img, (398, 587, 536, 611), grain=0.8)            # "Choose Cleric"
    img = inpaint_h(img, (600, 587, 727, 610), grain=0.8)            # "View Advancements"
    img = inpaint_h(img, (306, 648, 488, 662), grain=0.6)            # "VISUAL CONCEPT - PREVIEW ONLY"
    save(img.convert("RGBA"), "Altar_Backdrop.png")

    # Patches / sprites drawn by code.
    bottom = inpaint_h(img.copy(), (398, 481, 534, 504), grain=0.8)  # STARTER KIT label (Advancement page)
    save(bottom.crop(BOTTOM_LEFT).convert("RGBA"), "Altar_BottomLeftBlank.png")
    save(highlight_sprite(src), "Altar_CardHighlight.png")
    save(img.crop(BUTTON_CHOOSE).convert("RGBA"), "Altar_ButtonNavy.png")
    save(img.crop(BUTTON_VIEW).convert("RGBA"), "Altar_ButtonLight.png")
    save(dialog(img), "Altar_Dialog.png")
    save(emblem_glow(64), "Altar_EmblemGlow.png")

    # Advancement emblems: the concept's Paladin / Priest, the others recolored class medallions.
    for name, box in EMBLEMS.items():
        save(circle_sprite(src, box), "Altar_Emblem_" + name + ".png")
    med = {cls: circle_sprite(img, box) for cls, box in MEDALLIONS.items()}
    save(hue_shift(med["Warrior"], 200, 0.8), "Altar_Emblem_Sword Master.png".replace(" ", ""))
    save(med["Warrior"], "Altar_Emblem_Mercenary.png")
    save(med["Sorcerer"], "Altar_Emblem_Wizard.png")
    save(hue_shift(med["Sorcerer"], -90), "Altar_Emblem_Spellcaster.png")
    for cls in med:
        save(med[cls], "Altar_Emblem_" + cls + ".png")

    # Heading ornament (the small star before IDENTITY / CORE MECHANICS / PLAYSTYLE).
    star = src.crop((365, 224, 377, 236)).convert("RGBA")
    a = np.asarray(star.convert("RGB"), dtype=np.float32) / 255.0
    alpha = ((a[..., 0] > a[..., 2] + 0.10) & (a.max(2) < 0.85)).astype(np.uint8) * 255
    star.putalpha(Image.fromarray(alpha).filter(ImageFilter.GaussianBlur(0.4)))
    save(star, "Altar_HeadingStar.png")
    print("Altar assets written to", OUT)


if __name__ == "__main__":
    build()
