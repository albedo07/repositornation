# VFX completion list - v0.25.191 (was v0.25.190) (against Aethelborn_Ascended_VFX_Art_Direction.md)

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

## Built in v0.25.191 (user's answers to the open list)
- Impact Wave: branching cracks (`BranchCrackLine`) - normal + SM Ascended scar
- Moonlight Splitter: ONE crescent blade like the Ascended Halfmoon GT (no second "sewn" echo blade); Ascended finisher kept
- Eclipse (my pick): one 300 deg sweeping arc + thin inner trailing arc, no closed rings, no shockwave ring
- Whirlwind (normal): dust curling at the feet
- Circle Swing / Stomp / Punishing Bomb: extra shockwave ring + flash removed
- Heals glow: brief warm body sheen + small light on healed characters (gated 1 s per target)
- Shield Charge: ivory light on the shield face + sideward air streaks
- Angel Comet: no sky bolt; more destruction (fractures, displaced dirt, rubble, chunks)
- Divine Intervention: focused ivory-gold burst, upward healing strands, outward streaks to damaged enemies
- Heaven's Judgement: light drifting down every beam
- Heaven's Crucible / Barriers: shell shatters into shards when broken (expiry keeps the soft fade)
- Grand Cross: blades on separate planes + faint echo X behind
- Lightning Tempest: denser, darker, bigger canopy; strikes leave the cloud + sheet flash inside it
- Judgement Hammer Ascended return: its own receding trail
- Astral Greatblade: carved seams on both faces + swing wake (tip + mid)
- Elemental Cataclysm: elemental build-up while charging; blast in phases fire -> frost -> lightning (Ascended 2nd blast reversed)
- Clockwork: face turns, hands unwind (fast spin easing to a stop)
- Arcane Phantom: kept purple (user)
- Arcane Rupture: compact tear at the cast point, stray purple bolts removed, lighter burst
- Arrow Rain: real arrow bodies falling with the streaks

## Still open
- Nothing from the audit list. Remaining work is in-game review (look, day/night, overlap, FPS) and your feedback.

## Never verified
In-game look, day/night, slopes, overlapping casts, performance of the new pieces. Needs your screenshots / F7 report.
