IMMORTAL HEROES v0.19.0 - CLERIC BLOODLINE

Cleric -> Paladin and Cleric -> Priest now use one shared Skill Tree system.
The Cleric nodes, saved starter Tiers, tooltips, stars and hotbar styling are shared.
Priest changes only the advancement panel, its skills and its Grace icon.

INSTALL
Extract the entire ZIP, close Valheim, then run INSTALL.bat.
The existing installer compiles all 8 modules against your installed game/mod DLLs
in staging before replacing the live set. Keep the ImmortalHeroesAssets folder.

HOW TO USE
Open K as Cleric. Before Advancement, PALADIN / PRIEST buttons preview each route.
Previewing preserves your pending Class Tiers and does not change your class.
Spend and confirm Class points, then use the ADVANCE plaque for the selected route.
Both routes require Lv 16, Righteous Strike Tier 7, and 14 Class points spent.
These checks also apply at the Altar. After advancing, the Class Tiers stay shared
and locked; the advancement branch stays on your chosen class.

Priest uses Lightning Relic, Holy Relic, Divine Intervention, Grand Cross,
Heaven's Judgement, Lightning Tempest and Grand Sigil. Its existing targeting,
Relic recasts, Consecrated Ground and Grand Sigil mechanics are retained.
Drag skills to rearrange the 7 slots. Signature skills and the unlocked Ultimate
stay slotted. Grand Sigil uses the separate M4 + R / Grace binding.
Click key labels to rebind; the HUD and casting follow the saved slot order.

PROGRESSION
- Shared Class skills: Tier 0-7; 14 earned points by Lv 16.
- Advancement skills: Tier 0-5; points every 2 levels from Lv 18 through Lv 56.
- Priest signatures and Grand Sigil: unlock on Advancement at Lv 16.
- Divine Intervention: Lv 24 plus 3 advancement points spent.
- Grand Cross / Heaven's Judgement: Lv 32 plus 6 advancement points spent.
- Lightning Tempest: available at Lv 36; automatic Tiers at Lv 40 / 44 / 48.
- Existing Paladin Ascensions remain functional.
- Priest-specific Ascended variants are NOT defined in the supplied build; this
  pass does not invent them. Priest normal Tiers and Ultimate Tiers work.
- XP remains the existing /ih level testing system; no new XP mod is included.

SAVES
Existing Paladin save keys and loadouts are preserved. Cleric starter Tiers stay
shared. Priest advancement skill IDs and saved loadout are separate from Paladin.
An explicit base-Class reset/change clears chosen Tiers, Ascensions and loadouts,
while preserving Level and earned/bonus point totals for reassignment.

CHECKED HERE
71 assertions passed against 56 extracted production C# methods, with engine
interactions stubbed. All 8 source files passed C# syntax parsing. Artwork checks
confirmed identical shared Cleric panel pixels and inspected Priest layouts.
This is NOT a full eight-DLL compile or an in-game test. Valheim assemblies are
not available here; INSTALL.bat performs the real compile on your PC.
See TEST_THIS_BUILD.txt for the short in-game check.
