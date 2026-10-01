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


DEFAULT = ("lightning_zap", "goddess_relic", "judgement_hammer", "shield_charge", "ray_of_hope", "holy_wave", "electric_smite")


def render(pending, out_path, node=(405, 375, (370, 287, 442, 355)), layout=DEFAULT):
    base = Image.open(os.path.join(A, "Cleric_Paladin_Reference.png")).convert("RGBA")
    plus = Image.open(os.path.join(A, "Tier_Plus.png"))
    minus = Image.open(os.path.join(A, "Tier_Minus.png"))
    plaque = Image.open(os.path.join(A, "Confirm_Plaque.png"))
    for cx, skill in zip((229, 287, 344, 402, 459, 517, 574), layout):
        tex = os.path.join(A, ("Icon_" + skill if skill else "Slot_Empty") + ".png")
        base.alpha_composite(Image.open(tex).convert("RGBA").resize((49, 54), Image.LANCZOS), (cx - 24, 551))
        if skill in ("lightning_zap", "goddess_relic", "judgement_hammer", "electric_smite"):
            badge = Image.open(os.path.join(A, "Badge_Permanent.png")).convert("RGBA").resize((19, 19), Image.LANCZOS)
            base.alpha_composite(badge, (cx + 14, 547))
    im = base.resize((1180, 772), Image.LANCZOS)
    cx_ref, plate_bottom, icon = node
    b, cx, y = R(18), R(cx_ref), R(plate_bottom + 3)
    d = ImageDraw.Draw(im)
    fk = ImageFont.truetype(FONT, R(10.5))
    labels = [(c, str(i + 1)) for i, c in enumerate((229, 287, 344, 402, 459, 517, 574))] + [(691, "M4 + R")]
    for cx_l, text in labels:
        w = d.textlength(text, font=fk)
        d.text((R(cx_l) - w / 2 + 1, R(608) + 1), text, font=fk, fill=(0, 0, 0, 220))
        d.text((R(cx_l) - w / 2, R(608)), text, font=fk, fill=(237, 214, 158))
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
    render(True, os.path.join(out, "PREVIEW_v0.15.1_pending.png"))
    render(False, os.path.join(out, "PREVIEW_v0.15.1_selected.png"))
    render(False, os.path.join(out, "PREVIEW_v0.16.0_hotbar_edited.png"),
           layout=("lightning_zap", "righteous_strike", "goddess_relic", "judgement_hammer", "", "fallen_angel", "electric_smite"))
    print("previews written")
