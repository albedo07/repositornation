IMMORTAL HEROES — MECHANICS-INFORMED ART REFRESH
Scope: Framework sections 1–3.2 and attached v0.23.8 Icons source.

OPEN_PREVIEW.html is the offline review. Extract the whole ZIP first, then open
that file in a browser. Choose a class; click an icon for its visual intent.
Locked and Ascended toggles demonstrate independent art/frame styling.
These are review controls, not working game mechanics or an unlock simulation.

CONTENTS
51 individual normal-state skill illustrations, including six Graces.
9 class scenes: Warrior, Sword Master, Mercenary, Cleric, Paladin, Priest,
Sorcerer, Archmage, and Horizon Walker.
3 Base Class scenes double as default pre-advancement skill-tree backgrounds.
3 labeled skill review sheets, 3 Base Class tree previews, exact asset map,
and generation prompts. Mastery/Blessing passives are represented by the class
scenes; this pack does not add new passive hotbar nodes.
No duplicate grayscale or magenta raster variants; those states belong in UI.
No original large generated sheets, old mod source, DLLs, or redundant art.

IMPORTANT: This is an ART PACK / INTEGRATION HANDOFF, not an installed mod patch.
Do not overwrite the existing whole-screen Artwork.png assets with these scenes:
the old images contain labels and node layouts; the new scenes intentionally do not.
No combat values, skill actions, selection mechanics or unlock rules were changed.
Use the current framework/code for exact numerical mechanics; illustrations are
visual summaries, not literal hitbox, timing, projectile-count or VFX specifications.

VISUAL DIRECTION
Warrior: weathered martial stone, steel, amber physical impact. Default Warrior
has no advanced ghost-blade powers. Sword Master adds silver-blue spirit cuts;
Mercenary adds weight, battered iron, fractured ground and bomb fire.
Cleric: preserve the ivory/gold/navy sacred direction. Base Cleric has staff and
shield, healing wave and forward lightning. Paladin is a physical club-and-shield
frontliner. Priest uses distinct planted healing/electric crosses and barriers.
Sorcerer: arcane masonry, amethyst, ice and embers. Default Sorcerer shows its
three actual starting elements. Archmage is deliberate elemental artillery;
Horizon Walker is mobile dual-staff combat, spectral swords, copies and portals.

TREE AND SELECTION ART
Use scenes/warrior.jpg, scenes/cleric.jpg or scenes/sorcerer.jpg for the entire
corresponding tree, including its pre-advancement state. Keep that image and
its UV coordinates FIXED when switching between the two advancements.
Only the node artwork, titles and skill content change. All frames, node sizes,
anchors, margins and clipping rules must come from the same UI prefab/layout.
The three default backdrops depict Base Classes before they gain advanced powers.
Use the other six scenes as advancement selection artwork, not a replacement
background every time the advancement tab changes. Selection scenes have useful
quiet space to their right for existing live text. Do not bake text into them.
The HTML preview is a layout demonstration, not an instruction to replace your
approved live layout. Apply these assets to the existing universal layout.

FRAMES AND STATES
The icons are borderless, 192x192 PNG. Preserve their aspect ratio; the atlas
illustrations are contained inside a consistent square without stretching them.
Blue: signatures. Cyan: interchangeable attacks. Green: support/non-damaging.
Gold: Grace. Maroon: Ultimate. Magenta: Ascended override for eligible skills.
Support color takes priority for support skills even when they occupy a signature
slot (e.g. Holy Relic). Color does not alter skill category or slot eligibility.
Apply grayscale ONLY to the inner skill image. Keep the frame at full saturation
and full opacity. Never put a grayscale/alpha modifier on the parent node.
Ascended override affects the frame/glow, not the skill's damage-element identity.
Use existing ornate frames as separate overlays; do not add new per-class shapes.
Place a real padlock overlay at the bottom-right frame corner, outside name text.
The small square lock indicator in the HTML is only a placement demonstration.
No extra black backing panel behind the Base Class border. No opaque white
parchment label rectangle over the artwork. Keep titles centered independently.
Retain cyan numeric values together with their units; keep Tier fractions white.

ASCENDED ART POLICY
This compact pack delivers the NORMAL action icon for every skill. It does not
claim to include separately painted depictions of every Ascended sequence.
Reuse that recognizable icon with a separate magenta frame at runtime. Put exact
Ascended changes in live skill descriptions, not tiny baked labels or new symbols.
The preview toggle applies a visual state broadly to eligible skills; it does not
model mutually exclusive choices, quests, level requirements or loadout limits.
Especially preserve these framework distinctions if expanding the art later:
- Normal Halfmoon is frontal melee plus afterimage. Ascended adds its later
  optional giant ghost projectile after the opening slashes; do not replace the
  entire action with the superseded three-projectile proposal.
- Normal Crescent Cleave has five cleaves and no fire trails. Ascended adds
  interleaved cleaves and fire. Normal Moonlight has no burn.
- Normal Arcane Phalanx has four swords; Ascended has eight and its spear effect.
- Normal Afterimage Arsenal leaves stationary copies; Ascended has following
  identical astral twins. Current framework says clones cannot cast skills.
- Normal Meteor Fall shows one charged meteor; secondary meteors are Ascended.
- Normal Electric Smite is ground lightning. The Ascended storm is an addition.
- Ascended Grand Cross has no endpoint explosion in the current framework/build.

KNOWN LOADER WORK REQUIRED
AlbedosCustomClasses.Core.cs: AltarSkillIcon currently extracts Priest icons from
hard-coded rectangles in Cleric_Priest_Artwork.png. Replace that special path
with the same independent Icon_<id>.png lookup used by other classes.
Righteous Strike currently uses Icon_righteous_strike_Normal.png in the Altar.
This pack has one borderless Icon_righteous_strike.png. Point normal/Ascended
states to it and apply the respective frame separately, or supply the alias
during integration. Do not treat the new icon as an already-framed old image.
Find and update other tree/HUD/hotbar whole-screen crop paths consistently.
The following display names retain legacy code identities:
  Angel Comet -> fallen_angel -> icons/Icon_fallen_angel.png
  Heaven's Crucible -> grand_sigil -> icons/Icon_grand_sigil.png
  Archmage -> legacy class key Wizard
  Horizon Walker -> legacy class key Spellcaster
Preserve those code/save/config keys; do not rename them merely to install art.
scene names use current display names. Use manifest.json for explicit mappings.
Do not render default Base Class scenes as advanced unlocked states.
The preview preserves v0.23.8's AC slot order: its first two advancement skills
are signatures. In particular, Mercenary has Stomp/Circle Swing and Archmage
has Meteor Fall/Gravity Dominion. Do not infer signature status from the order
of paragraphs in the framework. The manifest lists the explicit runtime order.

FORMAT / PERFORMANCE
PNG icons: 192x192, optimized indexed color. Scene JPGs: 1024x512, quality 80.
These are UI resolutions, not world-texture or 4K wallpaper masters.
Unity image loading must select the actual .jpg filename for scenes; no WebP
decoder is required. Cache decoded textures/sprites. Do not allocate them on
every tab switch. Reuse the same family background texture across both ACs.
Packaging uses atlas extraction, proportional resizing and compression only;
artwork was generated through the built-in image-generation tool. Prompts are
included for reproducibility and future higher-resolution regeneration.

IN-GAME ACCEPTANCE CHECKS FOR THE IMPLEMENTER
Compare both advancements at the same resolution: all frame and node anchors
must be identical, with no background jump. Test locked, selected, unlocked and
Ascended states without fading or covering the colored frames. Check all 51
icons in tree, Altar, hotbar and HUD. Verify Priest uses the new independent
icons. Check names and locks at small UI scales. Use the live game to verify
interactions and legibility; this art handoff does not certify an in-game test.
