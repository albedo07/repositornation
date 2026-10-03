"""v0.20.4 Altar class cards: removes the dark panel baked around every card's gold frame.

The atlas (3x3 cells) came with a navy margin outside each ornate frame, which showed as a black
panel behind the cards. Every dark pixel connected to a cell edge becomes transparent (soft edge),
so the Altar background shows around the frame. Source: docs/source_art/Altar_ClassCards_v0.20.3.png
Usage: python3 tools/build_altar_cards.py -> ImmortalHeroesAssets/Altar_ClassCards.png
"""
import os
from collections import deque
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "source_art", "Altar_ClassCards_v0.20.3.png")
OUT = os.path.join(ROOT, "ImmortalHeroesAssets", "Altar_ClassCards.png")


def main():
    img = Image.open(SRC).convert("RGB")
    a = np.asarray(img, dtype=np.int32)
    H, W = a.shape[:2]
    mx = a.max(-1)
    dark = mx < 58
    seen = np.zeros((H, W), bool)
    q = deque()
    # Seeds: the outer edge of each cell (cells are W/3 x H/3, not whole numbers).
    xs = sorted({int(round(k * W / 3.0)) + d for k in range(4) for d in (-2, -1, 0, 1)})
    ys = sorted({int(round(k * H / 3.0)) + d for k in range(4) for d in (-2, -1, 0, 1)})
    for x in xs:
        if 0 <= x < W:
            for y in range(H):
                if dark[y, x] and not seen[y, x]:
                    seen[y, x] = True
                    q.append((y, x))
    for y in ys:
        if 0 <= y < H:
            for x in range(W):
                if dark[y, x] and not seen[y, x]:
                    seen[y, x] = True
                    q.append((y, x))
    while q:
        y, x = q.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < H and 0 <= nx < W and dark[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                q.append((ny, nx))
    alpha = Image.fromarray(np.where(seen, 0, 255).astype(np.uint8))
    # Soft edge on the removed side only, so the gold frame itself is never eaten.
    soft = np.asarray(alpha.filter(ImageFilter.GaussianBlur(0.7)), dtype=np.float64)
    soft = np.where(seen, soft * 0.6, 255).astype(np.uint8)
    rgba = img.convert("RGBA")
    rgba.putalpha(Image.fromarray(soft))
    rgba.save(OUT, optimize=True)
    print("transparent px:", int(seen.sum()), "of", W * H)


if __name__ == "__main__":
    main()
