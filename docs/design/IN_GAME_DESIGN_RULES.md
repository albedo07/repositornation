# How in-game designs are coded (procedural meshes): rules for any coder

The models are built in C# at runtime from vertices, so whoever writes the code never sees the result.
Every rule below exists so the model can be checked with numbers before it ever reaches the game.

## 1. Turn words into a dimension table before writing code
"Rectangle horizontally" is not a spec. Write the numbers down first, in the object's LOCAL axes:

| Part | X (width) | Y (height) | Z (depth) | Centre |
|---|---|---|---|---|
| Hammer head | 1.00 | 0.45 | 0.45 | (0, 0.48, 0) |
| Handle | 0.10 | 0.90 | 0.10 | (0, 0, 0) |

- Fix the axes once: X = left/right, Y = up, Z = toward the viewer. Write them in a comment.
- Check the requested shape against the table. For example, "horizontal rectangle" means X > Y and Y == Z.
  ChatGPT's v0.25.200 hammer multiplied the head's Y by 2.4, which made it a vertical rectangle.
- After building, print the mesh bounds to the log once (`mesh.bounds.size`) and compare them to the table.

## 2. Growth = ONE uniform scale on the root
- Bake the proportions into the mesh. To grow the object, use `root.localScale = Vector3.one * s`.
- Never write `localScale = new Vector3(width, height, width * 0.45f)` with values that change separately.
  The ratios drift as the values change, which is what made the v0.25.200 hammer "grow only tall".
- If the gameplay hitbox needs its own size, keep it on a separate object. Don't scale the visual to match the hitbox.

## 3. "Disappears at some angles": there are only 5 causes; check all of them
1. **Z-fighting:** two surfaces at (almost) the same depth flicker or swap as the camera moves.
   - RULE: no two faces of different parts closer than 5% of the part's thickness.
   - v0.25.200 gems were thin plates placed 0.01 from the panel face, and the centre gem was inside the seal disc. That was the bug.
2. **Back-face culling:** lit/opaque shaders draw only the front side of each triangle.
   - A single flat plane is invisible from behind. Wrong vertex order makes a face invisible from the front.
   - RULE: closed volumes only (extrude every outline with a front, a back and side walls).
   - Unity front face = vertices CLOCKWISE as seen from outside. Self-check on a convex part:
     `Vector3.Dot(Vector3.Cross(b - a, c - a), centroid - partCentre) > 0` for every triangle.
     If it fails, swap two indices.
3. **Transparent sorting:** transparent materials are sorted per OBJECT by centre distance.
   - A transparent gem near an opaque body pops in front of or behind it depending on angle.
   - RULE: solid parts (metal, stone, gems) use OPAQUE materials. Transparency only for glow/aura that floats away from surfaces.
4. **Stale bounds:** after changing vertices, call `mesh.RecalculateBounds()` (and `RecalculateNormals()`).
   - Otherwise Unity's camera culling thinks the object is elsewhere and hides it at the screen edges.
5. **Near clip / too small:** parts under ~2 cm at gameplay distance flicker. Give details a minimum real size.

## 4. "Embedded / one with the object" = overlapping VOLUMES, not touching surfaces
A gem glued on a face, or sunk to exactly the face depth, always reads as a sticker and z-fights.
The correct build (v0.25.201 `IhSetGem`):

```
body:   front at +F,                 back at -F
bezel:  front at +F + 0.06t,          back at -F - 0.06t   (goes THROUGH the body)
stone:  front at +F + 0.14t,          back at -F - 0.14t   (goes THROUGH the bezel)
```
- Every layer pierces the one below and rises a clear step above it on BOTH sides.
- The gold collar (bezel) visually joins the stone to the metal, which is why it looks set and not floating.
- Compute these depths from the real face positions of the parent, never from guessed constants.
  List every face depth of the parent (panel, disc, inlay lines...) and sit on the HIGHEST one.

## 5. Verify without the game
- Write the dimension table plus every face depth as comments next to the code.
- Assert the clearances in a small test or by hand: every pair of stacked faces must be >= 5% of the thickness apart.
- Render a quick preview offline (vertices to an image from 3 angles: front, 45 deg, edge-on) when the shape matters.
  The edge-on view exposes z-fighting and wrong proportions immediately.
- Report honestly: "numbers checked" is not "looks right in game".

## 6. Change only the part asked for
Keep mesh caches (`IhRelicMesh` keys) unique per shape. When geometry changes, change the cache key, or the old mesh is reused.
