# VFX completion list - v0.25.190 (against Aethelborn_Ascended_VFX_Art_Direction.md)

Static audit of every skill's runtime path (4 parallel code audits) + fixes in this pass.
NOTHING here is verified in game. "Done" = the code builds the effect the direction asks for.
User decisions override the doc where they differ (SM blue palette, Merc ember wake, Blade Storm visible slashes,
Glacial/Stonefang spikes, Furious Winds Neji dome, Relic designs from the user's own refs, no light pillars on casts,
Ultimate tri-colour strikes).

## Fixed in v0.25.190 (were invisible or missing)
- Crescent Cleave Ascended fire trail (tick ring drew nothing) -> low embers on the cracks
- Angel Comet Ascended burning ring (drew nothing) -> grounded flames on the ring edge
- Heaven's Crucible / Barrier hit response (drew nothing) -> faceted shimmer burst
- Zap delayed detonation (legacy line ring) -> ivory-blue filaments + spark crackle
- Bless Thy Sinners rescue (legacy line ring + beam) -> upward holy recovery motif
- Bonecrusher descent air, Frenzied Charge thrust streak + backward air, Punishing Bomb smoke tail
- Knight's Guidance / Heaven's Light crest above every recipient, Battlecry warm accent on allies
- Ray of Hope / Holy Relic cleanse wisps; Holy Relic pulse drifts relic -> ally; Lightning Relic discharges FROM the relic
- Warrior defensive Hyper Armor proc: muted steel edge burst
- Wizard Overcharge activation surge; Archmage staff charge motes per stack (ring drew nothing)
- Astral Railcannon wind up: spectral cannon silhouette (rings drew nothing)
- Glacial Descent Ascended cold mist/rime; Stonefang dirt at every spike base; Rift Walker portals face the caster
- Ranger: Acrobat air-jump wisps, Tumble Shot Ascended gust, Somersault flip wind arcs, Swallow Dive contact slices + leaves,
  Cyclone arrow visible inside the corkscrew + sparse leaves, big arrow body for Ballista / Great Arrow, Starfall arrows land
  on real enemies, Pinning boss pin arrows, subtle Hawk's Vigil marks, Focus bow accent, Tailwind streamer, Skyfall dust puffs,
  Furious Winds leaves halved (dome kept)

## Done (matches direction or the user's approved look)
Heavy Slash, Impact Punch, Crescent Cleave, Blade Storm (user look), Halfmoon Slash, Stomp, Seismic Guillotine, Bonecrusher,
Righteous Strike, Goddess Relic, Holy Relic, Lightning Relic, Judgement Hammer (normal), Holy Bulwark, Electric Smite (user look),
Flame Burst, Glacial Descent, Stonefang Eruption, Gravity Dominion, Frost Nova, Meteor Fall, Arcane Phalanx, Void Step, Rift Echo,
Gravity Blast, Rift Walker, Piercing Arrow, Tumble Shot, Snare Trap, Gale Volley, Cyclone Arrow, Swallow Dive, Somersault Dance,
Pinning Shot, Explosive Arrow, Starfall Volley, Ballista Shot, Splitting Arrow, Tailwind, Hawk's Vigil, Knight's Guidance,
Battlecry, Frenzied Charge, Punishing Bomb (blast still wide), Heaven's Light, Ray of Hope, Lightning Zap, Fury, Overcharge.

## Still open (not built - say which to do next)
- Impact Wave: cracks are straight lines, not branching
- Moonlight Splitter Ascended finisher: scaled-up blade, no distinct wide slow flow
- Eclipse: full 360 rings + shockwave ring instead of one sweeping arc with gaps
- Whirlwind (normal): one continuous spinning ring, not repeated short wakes; no foot dust
- Circle Swing / Stomp / Punishing Bomb: extra shockwave ring + small flash on impact
- Holy Wave: no body sheen on healed allies (needs a sheen material)
- Shield Charge: no light on the shield face / side air streaks; slam adds a sky bolt
- Angel Comet impact: extra sky bolt
- Divine Intervention: no focused burst / outward damage streaks
- Heaven's Judgement: no light drifting down the beams; shockwave ring per beam
- Heaven's Crucible: no fresnel look, no fracture on break
- Grand Cross: both blades on one plane (no front/back depth)
- Lightning Tempest: canopy 12 m high, thick strikes
- Judgement Hammer Ascended return: no separate receding trail
- Astral Greatblade: no seams / swing wake; contact is wide
- Elemental Cataclysm: all elements fire at once, no phases, no charge build-up
- Clockwork: static face, no rotation / unwinding
- Arcane Phantom: purple tint, not desaturated
- Arcane Rupture: tear is huge and floating; extra purple bolts
- Arrow Rain: streaks only, no arrow bodies in flight

## Never verified
In-game look, day/night, slopes, overlapping casts, performance of the new pieces. Needs your screenshots / F7 report.
