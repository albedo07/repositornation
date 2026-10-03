"""v0.20.4 preview: Altar base-class cards (transparent margin, light text veil) over the Altar background.
Approximation: DejaVu Serif stands in for Averia Serif. Usage: python3 tools/render_altar_cards.py"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
import render_states as rs

A, OUT = rs.A, rs.OUT
CARDS = (("Warrior", "Front-line physical bruiser"), ("Cleric", "Holy hybrid support"), ("Sorcerer", "Eitr-first magic specialist"))


def veil(w, h, alpha):
    yy, xx = np.mgrid[0:h, 0:w]
    edge = np.minimum(np.minimum(xx / (w / 8.0), (w - 1 - xx) / (w / 8.0)), np.minimum(yy / (h / 6.4), (h - 1 - yy) / (h / 6.4)))
    e = np.clip(edge, 0, 1)
    e = e * e * (3 - 2 * e)
    a = (e * alpha * 255).astype(np.uint8)
    return Image.fromarray(np.dstack([np.full((h, w), c, np.uint8) for c in (250, 235, 204)] + [a]), "RGBA")


def main():
    sheet = Image.open(os.path.join(A, "Altar_ClassCards.png")).convert("RGBA")
    cw, ch = sheet.width / 3.0, sheet.height / 3.0
    bg = Image.open(os.path.join(A, "Altar_Background.png")).convert("RGBA").resize((1100, 820))
    canvas = bg.crop((60, 150, 520, 700)).copy()
    for i, (name, role) in enumerate(CARDS):
        cell = sheet.crop((int(i * cw), 0, int((i + 1) * cw), int(ch))).resize((388, 150), Image.LANCZOS)
        x, y = 30, 20 + i * 170
        canvas.alpha_composite(cell, (x, y))
        cx, cy = x + 194 + 82, y + 75
        canvas.alpha_composite(veil(222, 124, 0.30), (cx - 111, cy - 62 + 3))
        layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        rs.text_c(d, cx, cy - 32, name.upper(), rs.font(22), (52, 34, 20, 255))
        rs.text_c(d, cx, cy + 24, role, rs.font(13, False), (52, 34, 20, 255))
        glow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        mask = layer.getchannel("A").filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(0.8))
        glow.putalpha(mask.point(lambda v: int(v * 0.55)))
        glow = Image.composite(Image.new("RGBA", canvas.size, (252, 240, 209, 255)), glow, glow.getchannel("A"))
        glow.putalpha(mask.point(lambda v: int(v * 0.55)))
        canvas.alpha_composite(glow)
        canvas.alpha_composite(layer)
    canvas.convert("RGB").save(os.path.join(OUT, "PREVIEW_v0.20.4_Altar_Cards.png"))
    print("written")


if __name__ == "__main__":
    main()
