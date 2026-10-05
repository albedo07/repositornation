"""v0.25.4 Immortal HUD art: HUD_Frame.png (9-slice ornate gold frame + navy panel, cut from the
skill tree footer's empty middle box) and HUD_Plaque.png (navy header plaque with gold rim, in the
style of the tree header). Read by Advanced.cs DrawImmortalHud (GUIStyle borders).
Usage: python3 tools/build_hud_assets.py
"""
import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
KEEP = os.path.join(ROOT, "docs", "source_art", "backdrops_v0238")


def main():
    src = os.path.join(KEEP, "Cleric_Paladin_PreAdvance.png")
    if not os.path.exists(src):
        src = os.path.join(A, "Cleric_Paladin_Reference.png")
    ref = Image.open(src).convert("RGBA")
    frame = ref.crop((184, 533, 622, 628))          # empty footer box: navy + gold ornate corners
    frame = frame.resize((frame.width * 2, frame.height * 2), Image.LANCZOS)
    frame.save(os.path.join(A, "HUD_Frame.png"))

    # Header plaque: navy hexagon, double gold rim, small gem ends (4x supersampled).
    k = 4
    w, h = 300, 34
    im = Image.new("RGBA", (w * k, h * k), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    def hexa(inset):
        i = inset * k
        return [(18 * k + i, i), (w * k - 18 * k - i, i), (w * k - i, h * k // 2), (w * k - 18 * k - i, h * k - i),
                (18 * k + i, h * k - i), (i, h * k // 2)]
    d.polygon(hexa(0), fill=(150, 112, 48, 255))
    d.polygon(hexa(2), fill=(232, 196, 112, 255))
    d.polygon(hexa(3.5), fill=(18, 28, 56, 255))
    d.line(hexa(6) + [hexa(6)[0]], fill=(200, 162, 86, 200), width=k)
    for x in (9 * k, w * k - 9 * k):
        d.polygon([(x, h * k // 2 - 5 * k), (x + 4 * k, h * k // 2), (x, h * k // 2 + 5 * k), (x - 4 * k, h * k // 2)], fill=(246, 214, 130, 255))
    im = im.resize((w * 2, h * 2), Image.LANCZOS)
    im.save(os.path.join(A, "HUD_Plaque.png"))
    print("wrote HUD_Frame.png, HUD_Plaque.png")


if __name__ == "__main__":
    main()
