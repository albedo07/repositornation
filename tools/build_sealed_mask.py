"""v0.25.165 Tree_SealedMask.png: alpha mask of the Advancement panel interior of the universal tree chassis (1011x662).
Before Advancement the game paints this area with a plain, subdued fill (no baked-in nodes, nameplates or preview art).
Interior = flood fill inside the panel's gold border (holes filled) + the plain inner rectangle (covers nodes touching
the border, e.g. the Ultimate plate). The header line (SEALED / ADVANCE AT LV 16) is drawn by the game on top.
Usage: python3 tools/build_sealed_mask.py"""
import os, cv2, numpy as np
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(ROOT, "ImmortalHeroesAssets")
SRC = "Warrior_SwordMaster_Artwork.png"   # any kit canvas: every tree shares the chassis geometry

def interior(path):
    im = cv2.imread(path)
    hsv = cv2.cvtColor(im, cv2.COLOR_BGR2HSV).astype(int)
    h, s, v = hsv[..., 0], hsv[..., 1], hsv[..., 2]
    gold = ((h >= 8) & (h <= 35) & (s >= 70) & (v >= 120)).astype(np.uint8)
    wall = gold.copy(); wall[:, :326] = 1; wall[:, 986:] = 1; wall[:118, :] = 1; wall[528:, :] = 1
    ff = (1 - wall).astype(np.uint8) * 255
    cv2.floodFill(ff, np.zeros((ff.shape[0] + 2, ff.shape[1] + 2), np.uint8), (650, 420), 128)
    reg = (ff == 128).astype(np.uint8)
    cnts, _ = cv2.findContours(reg, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    full = np.zeros_like(reg); cv2.drawContours(full, cnts, -1, 1, -1)
    return full

def main():
    m = interior(os.path.join(A, SRC))
    m[150:520, 340:976] = 1
    a = cv2.GaussianBlur(m.astype(np.float32) * 255, (3, 3), 0)
    out = np.dstack([np.full_like(a, 255), np.full_like(a, 255), np.full_like(a, 255), a]).astype(np.uint8)
    cv2.imwrite(os.path.join(A, "Tree_SealedMask.png"), out)
    print("Tree_SealedMask.png", int((m > 0).sum()), "px")

if __name__ == "__main__":
    main()
