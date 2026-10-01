"""Renders in-game-size (1180x772) previews of the Skill Tree from the built assets.
Approximation only: uses DejaVu Serif in place of Valheim's Averia Serif."""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"
S = 1180 / 1011.0


def R(v):
    return int(round(v * S))


def render(pending, out_path, node=(405, 375, (370, 287, 442, 355))):
    base = Image.open(os.path.join(A, "Cleric_Paladin_Reference.png")).convert("RGBA")
    plus = Image.open(os.path.join(A, "Tier_Plus.png"))
    minus = Image.open(os.path.join(A, "Tier_Minus.png"))
    plaque = Image.open(os.path.join(A, "Confirm_Plaque.png"))
    im = base.resize((1180, 772), Image.LANCZOS)
    cx_ref, plate_bottom, icon = node
    g = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).rounded_rectangle((R(icon[0] - 4), R(icon[1] - 4), R(icon[2] + 4), R(icon[3] + 4)),
                                        radius=R(8), outline=(255, 214, 110, 230), width=R(3))
    im = Image.alpha_composite(im, g.filter(ImageFilter.GaussianBlur(3)))
    b, cx, y = R(18), R(cx_ref), R(plate_bottom + 3)
    if pending:
        im.alpha_composite(minus.resize((b, b), Image.LANCZOS), (cx - R(3) - b, y))
        im.alpha_composite(plus.resize((b, b), Image.LANCZOS), (cx + R(3), y))
        pw, ph = R(160), R(42)
        px, py = R(872) - pw // 2, R(578) - ph // 2
        im.alpha_composite(plaque.resize((pw, ph), Image.LANCZOS), (px, py))
        d = ImageDraw.Draw(im)
        f, f2 = ImageFont.truetype(FONT, R(15)), ImageFont.truetype(FONT, R(9))
        for text, font, ty, col in (("CONFIRM", f, R(6), (250, 214, 120)), ("1 PENDING", f2, R(26), (236, 220, 180))):
            w = d.textlength(text, font=font)
            d.text((px + pw / 2 - w / 2 + 1, py + ty + 1), text, font=font, fill=(0, 0, 0, 200))
            d.text((px + pw / 2 - w / 2, py + ty), text, font=font, fill=col)
    else:
        im.alpha_composite(plus.resize((b, b), Image.LANCZOS), (cx - b // 2, y))
    im.convert("RGB").save(out_path)


if __name__ == "__main__":
    out = os.path.join(ROOT, "docs", "previews")
    render(True, os.path.join(out, "PREVIEW_v0.15.0_pending.png"))
    render(False, os.path.join(out, "PREVIEW_v0.15.0_selected.png"))
    print("previews written")
