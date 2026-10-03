"""Paladin vs Priest on the universal Skill Tree (Lv 56, same Class Tiers), rendered from the built assets.
Approximation: DejaVu Serif stands in for Valheim's Averia Serif.
Usage: python3 tools/render_branch_preview.py -> docs/previews/PREVIEW_v0.19.1_*.png/.gif
"""
import os
from PIL import Image, ImageDraw

import render_states as rs
import render_tiers as rt

A, OUT = rs.A, rs.OUT
ANCHORS = {  # template slot nameplate anchors (center x, bottom y)
    "lightning_zap": (200, 247), "righteous_strike": (201, 375), "holy_wave": (201, 499),
    "slot_gr": (404, 247), "slot_jh": (405, 375), "slot_sc": (582, 247), "slot_fa": (720, 247),
    "slot_roh": (720, 375), "slot_es": (902, 355),
}
BRANCHES = {
    "Paladin": {
        "backdrop": "Cleric_Paladin_Reference.png",
        "tiers": {"lightning_zap": (4, 7), "righteous_strike": (7, 7), "holy_wave": (3, 7), "slot_gr": (5, 5), "slot_jh": (5, 5),
                  "slot_sc": (5, 5), "slot_fa": (5, 5), "slot_roh": (0, 5), "slot_es": (3, 3)},
        "hotbar": ("righteous_strike", "goddess_relic", "judgement_hammer", "shield_charge", "fallen_angel", "ray_of_hope", "electric_smite"),
        "permanent": ("righteous_strike", "goddess_relic", "judgement_hammer", "electric_smite"),
        "grace": "Icon_heavens_light.png",
    },
    "Priest": {
        "backdrop": "Cleric_Priest_Reference.png",
        "tiers": {"lightning_zap": (0, 7), "righteous_strike": (7, 7), "holy_wave": (7, 7), "slot_gr": (5, 5), "slot_jh": (5, 5),
                  "slot_sc": (5, 5), "slot_fa": (5, 5), "slot_roh": (0, 5), "slot_es": (3, 3)},
        "hotbar": ("holy_wave", "lightning_relic", "holy_relic", "divine_intervention", "grand_cross", "heavens_judgement", "lightning_tempest"),
        "permanent": ("lightning_relic", "holy_relic", "lightning_tempest"),
        "grace": "Icon_grand_sigil.png",
    },
}


def render(branch):
    cfg = BRANCHES[branch]
    img = Image.open(os.path.join(A, cfg["backdrop"])).convert("RGBA")
    badge = Image.open(os.path.join(A, "Badge_Permanent.png")).convert("RGBA").resize((19, 19), Image.LANCZOS)
    for cx, skill in zip(rs.SLOT_CENTERS, cfg["hotbar"]):
        icon = Image.open(os.path.join(A, "Icon_" + skill + ".png")).convert("RGBA").resize((49, 54), Image.LANCZOS)
        img.alpha_composite(icon, (cx - 24, 551))
        if skill in cfg["permanent"]:
            img.alpha_composite(badge, (cx + 14, 547))
    rs.hotkey_labels(img, [(cx, "M4 + " + str(i + 1), (237, 214, 158)) for i, cx in enumerate(rs.SLOT_CENTERS)]
                     + [(691, "M4 + R", (237, 214, 158))])
    d = ImageDraw.Draw(img)
    for key, (done, mx) in cfg["tiers"].items():
        cx, pb = ANCHORS[key]
        size = 17 if key == "slot_es" else (12 if mx == 7 else 14)
        rt.TIERS["_p"] = (done, 0, mx)
        x0, x1 = rt.star_row(img, cx, pb + 3, "_p", size)
        d.text((x1 + 3, pb + 3 + (size - 11) / 2.0), "%d/%d" % (done, mx), font=rs.font(9), fill=(96, 66, 30))
    rt.TIERS.pop("_p", None)
    rs.text_c(d, 204, 124, "TIER POINTS  0  ·  LOCKED", rs.font(10), (96, 84, 70))
    rs.text_c(d, 675, 124, "TIER POINTS  0", rs.font(11), (92, 52, 40))
    return img


def main():
    pal, pri = render("Paladin"), render("Priest")
    W, H = pal.size
    side = Image.new("RGB", (W * 2 + 12, H), (12, 12, 16))
    side.paste(pal.convert("RGB"), (0, 0))
    side.paste(pri.convert("RGB"), (W + 12, 0))
    side.save(os.path.join(OUT, "PREVIEW_v0.19.1_Paladin_vs_Priest.png"))
    pri.convert("RGB").resize((1180, 772), Image.LANCZOS).save(os.path.join(OUT, "PREVIEW_v0.19.1_Priest.png"))
    frames = [pal.convert("RGB").resize((1011, 662)), pri.convert("RGB").resize((1011, 662))]
    frames[0].save(os.path.join(OUT, "PREVIEW_v0.19.1_Paladin_Priest_blink.gif"), save_all=True,
                   append_images=frames[1:], duration=900, loop=0)
    print("branch previews written")


if __name__ == "__main__":
    main()
