# Aethelborn Ascended: standing design workflow

Adopted 2026-10-18 (Asia/Manila), at the user's request.
Read this file before any model or VFX design work. The complete supplied guide is preserved below, unchanged.
Original reference: IN_GAME_DESIGN_RULES.md, Library ID libfile_7bd41585b9c08191ae2a440aa3b8392c.

## Required startup checks after the v217 regression

Recorded 2026-10-19 (Asia/Manila), at the user's request.

- v217 broke shared combat initialization because the new audio section `Sword Master Knight's Guidance Audio` contained an apostrophe rejected by BepInEx. The exception occurred before combat/equipment patches were installed. Compilation and fake configuration adapters missed it.
- Validate every added/changed configuration section and key with the real BepInEx ConfigDefinition. Execute the production binding path with the real ConfigFile, including save/reload and preservation of existing values. A permissive adapter is not evidence of startup compatibility.
- Optional audio setup must fail independently: log once, keep audio disabled for that session if binding fails, and continue combat initialization. Never change saved user preferences to disable audio as an error workaround. Exercise both successful setup and injected failures using the production guard.
- Changes inside a shared runtime require checking its initialization order and dependent systems even when their source is unchanged. Exact source scope and successful compilation do not prove those systems still initialize.
- Preserve the user-confirmed Anchor and exact unrelated files. Report separately: real-dependency configuration tests, adapter tests, compilation, and in-game/Windows validation. Do not claim full startup or gameplay verification from an extracted guard or an adapter test.

## Application and precedence

- The user's latest explicit instructions and approved appearances take precedence. Reading this guide does not authorize an additional code pass, anchor promotion, or unrelated fixes.
- Start each pass from the user-designated working build. Preserve the original archive and unrelated behavior. Do not merge .cs files or DLL modules; a single DLL requires the user's explicit instruction when development is complete.
- Before modeling, define silhouette, axes, pivot, every part's dimensions, face depths, overlap, and intended growth ratio. Base the measurements on actual supplied references and current game assets; quoted example heights are estimates to verify, not universal constants. Do not enlarge approved effects or gameplay areas because the guide says to go bigger when unsure.
- Use one uniform root scale for growth unless the user explicitly requests changing proportions. Keep gameplay volumes independent and unchanged unless requested.
- Build embedded solid details as closed opaque volumes with intentional overlap and joining geometry. Check layered-face separation, winding, normals, bounds, and silhouette at front, 45 degrees, edge-on, and from behind. The 5 percent depth rule is a design starting point, not a guarantee against camera-dependent artifacts.
- Use existing mesh tools where suitable, cache generated assets, and change geometry cache keys when geometry changes. Check concave outlines and bevels for self-intersection; do not reintroduce invalid heart bevels merely to satisfy the general bevel rule.
- Validate shader availability, rendering mode, and color space in the current material path. Do not blindly apply Color.linear a second time or assume a fallback shader produces an opaque lit solid.
- Diagnose angle-dependent disappearance by checking depth conflicts, one-sided geometry, transparency sorting, stale bounds, and details too small to resolve. Treat the guide's historical diagnoses as hypotheses until verified for the current code and build. Offline previews can expose geometry problems; they do not reproduce Unity rendering or prove the live cause.
- Slashes follow each actual strike direction and hit frame, including diagonal and vertical swings. A generic left-to-right example does not override an approved animation.
- Preserve explicitly approved ground rings, pulses, and Heaven's Judgement sigil. Do not add generic cast circles. The user's removal of skill light effects takes precedence over older light-budget references; do not restore those lights.
- Use buildup, impact, and fade with bounded readable layers. Reuse cached resources and observe existing budgets. Check performance without promising that code checks guarantee no in-game FPS drops.
- Preserve C# 5 compatibility. Never call LineRenderer.SetPositions/GetPositions or Texture2D.LoadImage. Run required release checks when available; report missing tools honestly, along with any alternative validation. Do not claim unavailable checks were run.
- Inspect offline previews, compare measurements against the intended dimensions, and run appropriate static checks before delivery. Clearly distinguish measured geometry, offline previews, compilation, and in-game validation. Do not call a rendered result verified without observing it.
- Keep documentation concise. Consolidate release notes when packaging future requested passes, preserve history separately, and prioritize quality over speed. Reuse validation tools and avoid redundant builds.

Source SHA-256: 6f8265dc26187f8ba7c8b0a384396af0d176ec1961affe86a256b66b5109a128

---

# Coding in-game designs (models + VFX): how it is done in this mod

The game is never visible while coding. Every object (relics, hammer, crescents, black hole, meteors...) is
built in C# from vertices and particles at runtime. The work therefore follows one principle:
**design is converted into numbers and rules that can be checked without seeing it.**
"Looks right" happens only when those numbers are right.

---------------------------------------------------------------------------------------------------
## PART A: Understanding the design (before any code)

### A1. Read a reference as layers of form, not as a picture
Break every reference image or description into three levels, biggest first:
1. **Silhouette (primary form):** the outline seen from far away, e.g. a cross, a heart-topped scepter, a crescent.
   This is 80% of whether it "reads". Get it right first, using only flat outlines.
2. **Secondary forms:** the big parts that sit on the silhouette, e.g. the frame, inner panel, ring, wings, head and handle.
3. **Details:** gems, filigree, inlay lines, rivets, glow.
   Details are added LAST and must belong to a secondary form (set into it, wrapped around it), never float on top.

Write that hierarchy as a parts list before coding.
- Example, Goddess Relic: silhouette = Celtic cross with flared tips.
  Secondary = gold frame, ivory panel, ring.
  Detail = seal disc, set gems, filigree scrolls.

### A2. Decide the real size from things that exist in the game
Never invent a size. Compare against known objects:
- player height 1.8 m;
- Greydwarf Brute 2.5 m;
- 0-star Troll approx 6.5 m (Goddess Relic normal cross = Troll size);
- skill radius (an effect must cover its real hitbox).

The user wants LARGE battles, so when unsure, go bigger.
All distances go through `DragonCombat.M()`.

### A3. Translate the user's words into properties
The user describes looks in plain words. Map each word to something measurable:

| User says | Means in code |
|---|---|
| "rectangle horizontally" | X > Y, Y == Z (write the ratio) |
| "grows on all sides" | one uniform scale |
| "physical / solid / one with it" | closed opaque meshes, overlapping volumes, lit material |
| "floating / stuck on / sticker" | parts only touch or hover: make them pierce |
| "disappears at angles" | z-fighting, one-sided faces, transparent sort or stale bounds (see C3) |
| "too bright / blinding" | additive alpha, light intensity, particle count too high |
| "flat / no depth" | add thickness, bevels, a second layer at a different depth |
| "shaking / teleporting" | something is written twice per frame, or root motion leaks in |
| "like X (game/anime)" | identify the 2-3 signature traits of X; copy those, not the whole thing |

If a word can mean two shapes, pick the one that matches the reference image.
State the chosen numbers in the changelog so the user can correct them.

### A4. User orders beat any design document
The user's latest instruction overrides the Framework doc, the Art Direction doc and ChatGPT's notes.
Look elements the user approved stay, even when a doc says otherwise. Examples: Sword Master blue, Merc ember, no cast pillars, no ground glyph circles.

---------------------------------------------------------------------------------------------------
## PART B: Building models (procedural meshes)

### B1. Axes and pivot, written once per object
- Local X = width (left/right), Y = up, Z = depth (toward the viewer for flat objects like relics).
- Pivot = where the object is held or planted, e.g. the cross foot, the hammer handle bottom, the gem centre.
  Children are positioned relative to the pivot.
- Put a comment at the top of every builder: `// X width, Y up, Z depth; pivot = foot`.

### B2. Dimension table first
Before coding, list every part: size X/Y/Z, centre, and the depths of its front/back faces.
Code is then a transcription of the table. Afterwards, log `mesh.bounds.size` once and compare.

### B3. Shape tools in the codebase (`DragonVfx` in DragonsAltar.CombatRuntime.cs)
Use these; don't hand-place hundreds of vertices.

| Tool | What it makes | Used for |
|---|---|---|
| `ExtrudeBevel(outline, depth, bevel, uv)` | a 2D outline (counter-clockwise) becomes a solid slab with bevelled edges | crosses, frames, hearts, gems |
| `LatheMesh(profile, segments)` | spins a profile around Y | knobs, pommels, columns, collars |
| `TubeMesh` | a tube along a path | rings, scrolls, filigree |
| `WingMesh` | layered feather wing as one mesh | — |
| `FacetMesh` | flat-shaded crystal or rock | — |
| `SpikeMesh` / `LumpMesh` | tapered horns, irregular rubble | — |
| `DiskMesh` | flat disc | accretion disks |
| Outline helpers | `InsetGemOutline`, `StarOutline`, `FeatherOutline`, heart outline, `ScrollPath` | — |
| Material presets | `RelicGold`, `RelicIvory`, `RelicGem`, `NaturalMat`, `SolidMat`, `ClearMat` | — |

Cache every generated mesh by a key (`IhRelicMesh(key, builder)`).
**When the geometry changes, change the key**, or the old mesh is reused.

### B4. Construction rules
1. **Closed volumes only.** Every part has a front, a back and side walls.
   Lit shaders draw one side of a triangle, so a single plane vanishes from behind.
2. **Winding.** A Unity front face has its vertices clockwise as seen from outside.
   Self-check for convex parts: `Dot(Cross(b - a, c - a), triangleCentre - partCentre) > 0` for every triangle.
3. **Layers pierce, never touch.** A part that sits "in" another part extends THROUGH it and rises a clear step on both sides.
   Minimum gap between any two stacked faces = 5% of the thickness.
   Gem pattern (`IhSetGem`): body ±F, gold bezel ±(F + 0.06t), stone ±(F + 0.14t).
4. **Join parts with a transition piece**, such as a bezel, collar, ring or socket. A part placed straight against another reads as glued on.
5. **Bevel every edge** (small bevel = catches light = reads as metal or stone). Hard 90° edges look like paper.
6. **Proportions live in the mesh. Growth is one uniform scale on the root.**
   The gameplay hitbox is a separate object; don't stretch the visual to fit it.
7. **Opaque materials for anything solid.** Transparency only for glow and aura that sits away from surfaces.
8. After editing vertices: `RecalculateNormals()` and `RecalculateBounds()`.

### B5. Materials in Valheim
- The game renders in Linear colour space: colours picked in sRGB look washed out unless converted (`Color.linear`).
- Unity's `Standard` shader is NOT guaranteed in the player build.
  Reuse a shader from a vanilla prefab (`ObjectDB.instance.GetItemPrefab("SwordBronze")`) or fall back to `Sprites/Default`.
  Always have a fallback.
- Gold = metallic, warm, with dark recesses. Ivory = matte, off-white. Gem = saturated, slightly emissive, opaque.
  Contrast between neighbouring materials is what makes the details read.

---------------------------------------------------------------------------------------------------
## PART C: Building VFX (particles, lines, decals)

### C1. Every effect has a timeline
Anticipation (build-up), then impact (peak, short and bright), then dissipation (slow fade, debris settling).
- Impacts peak within ~0.1 s, then fade over 0.3-1.5 s.
- Use `DragonVfxLife` for lifetime, follow and fade; never leave objects alive.

### C2. Layering
A good effect = 3-5 layers at different sizes and speeds:
1. core shape (blade, orb, bolt);
2. body glow;
3. secondary motion (wake, streaks, swirl);
4. ground contact (dust, cracks, scorch, rubble);
5. small particles (sparks, motes).

One layer alone looks cheap. Ten layers at full strength look like noise.

### C3. Restraint rules the user asked for (keep them)
- Additive (glowing) layers are kept faint. Most of the body is alpha-blended so colour stays readable in daylight.
- Global caps: `Restraint`, `PolishBudget`, Flash light cap, `MaxEffectLights`, shake throttle. Don't bypass them.
- Effects on the ground follow the terrain height (`GroundPoint`, `TryGroundY`, `Conform` meshes). Never place flat decals at the player's Y.
- No cast pillars, no ground glyph circles on cast, no tremble/oscillation.

### C4. Direction and orientation
- Slashes are built in the PLANE OF THE CUT, with the convex edge leading. Horizontal slashes sweep left to right, matching the animation.
- Moving effects are children of a moving carrier, so they travel with the hitbox. Never spawn them once at the origin.
- Effects must line up with the hit frame of the animation (damage time = visual peak).

### C5. Performance (user rule: no FPS drops)
- Build textures, materials and meshes once and share them (prewarm + caches). Use MaterialPropertyBlock for per-instance fades.
- No `FindObjectOfType`, no reflection lookups and no allocations per frame. Throttle scans (0.1 s).
- Cap counts (debris, planted arrows, lights) and reuse.

---------------------------------------------------------------------------------------------------
## PART D: Checking without the game

1. **Numbers:**
   - the dimension table matches the bounds;
   - every stacked-face gap is >= 5% of the thickness;
   - the ratios the user asked for hold (e.g. head X > Y = Z);
   - growth is uniform.
2. **Offline preview:** project the mesh vertices to an image from the front, at 45° and edge-on (Python + numpy/PIL is enough).
   - Edge-on shows z-fighting and floating parts.
   - 45° shows silhouette and depth.
   - Compare it side by side with the reference.
3. **Screenshots from the user:** measure in pixels. Never eyeball.
4. **Compile:** `tools/compile_check/check.sh 3029660` + `mcs --parse -langversion:5`. C# 5 only. No `SetPositions` / `GetPositions` / `LoadImage`.
5. **Report honestly:** state what was checked (numbers / preview / compile) versus what still needs an in-game look.

---------------------------------------------------------------------------------------------------
## PART E: Why it failed before (real examples)

| Request | What was coded | Why it was wrong | Fix |
|---|---|---|---|
| Gems embedded in the relic | thin plates 1% from the face, centre gem inside the disc | z-fighting + no joining piece = floating sticker that vanishes | bezel + stone pierce the body (B4.3, B4.4) |
| Heart "in" the Holy Relic | heart faces 0.025t from disc/sunburst | same | heart frame through the body, heart through the frame |
| Horizontal rectangular hammer head | head Y × 2.4, scaled (w, h, w × 0.45) | words were not turned into a ratio; growth not uniform | dimension table + one uniform scale |
| Lock badges | badge painted under the frame, art drawn over it | draw order: later draws cover earlier ones | stamp the badge LAST, on top |

The rule behind all of them: **write the intended result as numbers, then check that the code produces those numbers.**
