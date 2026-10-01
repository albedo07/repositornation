# Immortal Heroes — Valheim BepInEx Mod

## Working rules
- User wants short responses and real execution, not long explanations.
- **RUSH B** = immediately code the obvious requested task, minimal commentary, use latest approved build as baseline, don't redesign unrelated things, verify and package.
- Latest package: **v0.14.6 — Footer + Grace Cleanup**. Recent work is almost entirely UI polish; do NOT change combat/progression logic unless explicitly requested.
- Never patch visual problems by drawing random rectangles over the reference asset. Keep approved artwork intact; make layout/components fit properly.

## Core philosophy
- RPG + Survival, never EZ mode. Skills add power/options without trivializing survival, gathering, bosses, terrain, prep, food.
- Standalone first, compatibility second. Optional EpicMMO/EpicLoot/Jewelcrafting/CL&LC support later; must work without them.
- Modular source OK during dev; eventually one `ImmortalHeroes.dll`.

## Terminology
- Class = old Main Class. Advancement Class = AC.
- Blessing = permanent Class trait. Mastery = permanent AC trait.
- Grace = dedicated active on M4+R, separate from 7 numbered slots. Costs 0 resources, does not stack with itself.
- Never use: "Pactive", "Passive-Active", "Weapon Mastery Blessing".

## Classes
### Warrior
- **Warrior's Blessing:** Hyper Armor only if incoming hit <30% Max HP; 2x parry; +20 Run / +20 Jump.
- **Sword Master — The Way of the Sword:** +20 Sword, +50% Sword attack speed, no sword movement penalty, Block/Dodge interrupts remaining skill sequence.
  - **Knight's Guidance (Grace):** 10m snapshot, 1.5x move speed, +40% stamina regen, -30% stamina use, 3 min, 13 min CD.
- **Mercenary — Warfreak:** dual-wield any two 1H physical weapons, +10 Sword/Axe/Clubs, +50% attack speed with dual 1H or 2H physical, no physical weapon movement penalty, +30% current armor, high aggro priority.
  - **Battlecry (Grace):** +15% creature attack damage 1 min, +25% environmental/resource damage 3 min, 10 min CD.
  - **Fury:** auto-activates at 100, lasts 20s, 3 min lockout; +1 per normal melee hit / +3 per Merc skill enemy hit; after 60s out of combat drains 1/sec.

### Cleric
- **Cleric's Blessing:** shields 1.5x Block Force + Block Armor, Shield+Staff allowed, no movement penalty from shields/staves/1H Club-skill weapons, +35 HP, +20% HP regen.
- **Paladin — Holy Trinity:** with Club+Shield: +15 Clubs, remove armor movement penalties, Slash and Pierce each at least 50% of current Blunt without reducing existing values.
  - **Heaven's Light (Grace):** 10m snapshot, +40% Overall Defense, removes equipment movement penalties only, 1 min, 10 min CD.
- **Priest — Bless Thy Sinners:** death-save aura 20m, heal 50% HP over 6s, +50% move speed / -70% stamina use for 6s; self save 20 min CD, ally save 40 min per ally. Buckler parry buffs/shockwave system as previously defined.
  - **Heaven's Crucible (Grace):** 250 Barrier HP, Armor = 30% of Priest current Armor (snapshot), 16s or until broken, 10 min CD.

### Sorcerer
- **Sorcerer's Blessing — Warlock:** creature melee -70% (labor damage unaffected), +65 Max Eitr, +35% Eitr regen, half regen delay, cannot Block/Parry.
- **Wizard — Archmage:** charged normal Staff attacks (Gun Staves excluded), max 6s / 3 stacks, 1 Eitr per 0.1s, first stack doubles size, projectile behaves like normal ballistic Staff projectile. Overcharge: 300 Eitr spent → 12s +40% Wind Up speed / +40% Eitr regen / +40% Magic Damage, then 5s accumulation buffer.
  - **Clockwork (Grace):** 10m snapshot, +30% Skill Damage, 50% CD reduction for non-Grace skills/mechanics that ENTER cooldown while active, 22s, 10 min CD.
- **Spellcaster — Yin and Yang:** Staff/Wand attack interval -50%, Eitr use -50%, +20% Eitr regen, normal staff/wand attack damage -50%, no skill windup, no staff/wand movement penalty, dual Gun Staves fire simultaneously, 100% accurate, inherits no Block/Parry.
  - **Rift Walker (Grace):** Portal A then Portal B within 30s, max 50m Free Aim, two-way E interact, portals operate for 30s after B placement, Feather Falling from airborne exit, 2 min CD from A placement.

## Progression
- Max level 80. Lv2 Class quest.
- Class: 3 skills, max Tier7, 14 Class Tier Points (Lv4–16, +2 every 2 levels).
- Must max the AC prerequisite Class skill to Tier7 before Advancement.
- Lv16 Advancement Quest; prereq Class skill becomes **Ascended** and mandatory.
- AC: 5 skills × Tier5 (25 possible), only 20 AC Tier Points (+1 every 2 levels, Lv18–56).
- C3 unlock Lv24; C4 unlock Lv32; both also require 80% of earned AC points spent (normal rounding).
- Lv32 First Ascension: one Tier5 Signature skill. Lv42 Second Ascension: one Tier5 of the other 3 AC skills.
- Lv36 Ultimate Unlock Quest. Ultimate tiers auto: Lv40 T1 / Lv44 T2 / Lv48 T3. Lv50 Ultimate Ascension Quest (requires T3).
- Free Skill Reset Scroll at Lv32 (or on late Advancement if already past Lv32).
- After Advancement, normal Class Reset unavailable; special Reset Class NPC planned.
- Hotbar: 7 numbered slots + Grace (M4+R). Mandatory numbered: Ascended prereq + both Signatures + Ultimate. Remaining 3 from 5 optional. Ultimate can go in any numbered slot.

## Skill tree UI
- No visible C1–C5 labels. Only skill name under node; category goes in tooltip. No connector arrows.
- Hover Class name = Blessing tooltip; hover AC name = Mastery tooltip.
- Cleric → Paladin layout:
  - Class: Lightning Zap, Righteous Strike, Holy Wave.
  - Paladin: Goddess Relic, Judgement Hammer, Shield Charge, Fallen Angel, Ray of Hope, Electric Smite (Ultimate).
  - Grace: Heaven's Light.
- Node colors: Class normal Blue · AC normal Cyan · Buff Green · Signature Navy · Ascended Magenta · Ultimate Pastel Maroon · Ascended Ultimate Red · Grace Yellow/Gold.
- Prestige: Ultimate > Ascended > Signature > Normal > Buff; Grace unique. Ultimate node larger, Grace slightly larger.
- Style: ornate gold high-fantasy frame, parchment; Cleric side blue sacred/cathedral, Paladin side warm rose/kingdom. Compact, airy node spacing.
- Footer: one unified ornate footer. Left = "7-SLOT HOTBAR" info. Middle = slots 1–7 with hotkey numbers under each box. Grace = own gold box, "HEAVEN'S LIGHT" inside, `M4 + R` under the box (matching numbered slots). Far-right uses same ornate treatment — not a nested/flat/pasted blue rectangle. No Color Key.
- Interaction: click node = select only. Selected node shows `+`; if pending >0 also `-`. `+` adds pending Tier, `-` removes pending Tier only. Confirm appears only if total pending >0 and commits. Closing tree discards unconfirmed pending.
- Tooltip headers: `ASCENDED - LIGHTNING ZAP`, `ATTACK - GODDESS RELIC`, `BUFF - RAY OF HOPE`, `GRACE - HEAVEN'S LIGHT`, `ULTIMATE - ELECTRIC SMITE`.

## Roadmap (after UI is locked)
1. Finish tooltip polish.
2. Wire real persistent Tier/progression state to the tree.
3. Finish Ascension/Ultimate functional logic.
4. Return to combat skill revisions.
5. Standalone/compatibility cleanup; possible Jötunn removal.
6. Merge into one `ImmortalHeroes.dll`.
