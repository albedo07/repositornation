# Immortal Heroes — Valheim BepInEx Mod

## Working rules
- User wants short responses and real execution, not long explanations.
- **RUSH B** = immediately code the obvious requested task, minimal commentary, use latest approved build as baseline, don't redesign unrelated things, verify and package.
- Latest package: **v0.22.4 — Mercenary** (v0.22.3 Sword Master, v0.22.2 HUD and Altar Fixes, v0.22.1 Kit UI Fixes, v0.22.0 Universal Kits, v0.21.2 Balance Pass, v0.21.1 Cleric Tweaks, v0.21.0 Priest Ascended, v0.20.9 Buckler Parry, v0.20.8 Cleric per Framework, v0.20.7 Cleric Consistency, v0.20.6 Polish, v0.20.5 Config Window, v0.20.4 Tree and Altar Fixes), built on ChatGPT's v0.20.3 (Universal Tree runtime chassis + uGUI Altar with Altar_Background/Altar_ClassCards; it REPLACED our v0.19.2/0.19.3 IMGUI Altar, which the user accepted as the new base). Older: v0.19.x ours, v0.19.0 ChatGPT. Do NOT change combat/progression logic unless explicitly requested.
- Never patch visual problems by drawing random rectangles over the reference asset. Keep approved artwork intact; make layout/components fit properly.

## Framework doc (source of truth for design)
- Google Doc "Immortal Heroes Framework": https://docs.google.com/document/d/1sTpQSP7Gr1kHJ7cdl1b7WtOomv7C7_k5_Bmj2cqw_Xg/edit (fileId `1sTpQSP7Gr1kHJ7cdl1b7WtOomv7C7_k5_Bmj2cqw_Xg`). Read it with Drive `read_file_content`. The user allows edits when we make major changes or add terms; editing needs the Google Docs connector.

## Progression decisions (2026-10-02, not yet in code)
- "MC" = Main Class (formerly "Class").
- Ascended MC skill per AC: Sword Master Impact Wave · Mercenary Heavy Slash · Paladin Righteous Strike · Priest Holy Wave · Wizard Glacial Descent · Spellcaster Stonefang Eruption. (Tree art still shows Lightning Zap as Paladin's Ascended: fix when the universal tree is built.)
- Signatures (unlock at Advancement Lv16) / Lv24 skill / Lv32 pair:
  - Sword Master: Moonlight Splitter + Crescent Cleave / Blade Storm / Frenzied Charge, Eclipse
  - Mercenary: Stomp + Circle Swing / Bonecrusher / Seismic Guillotine, Reaver's Orbit
  - Paladin: Goddess Relic + Judgement Hammer / Shield Charge / Fallen Angel, Ray of Hope
  - Priest: Lightning Relic + Holy Relic / Divine Intervention / Grand Cross, Heaven's Judgement
  - Wizard: Meteor Fall + Gravity Dominion / Astral Railcannon / Astral Greatblade, Frost Nova
  - Spellcaster: Arcane Phalanx + Afterimage Arsenal / Void Step / Rift Echo, Gravity Blast
- Gravity Blast (new Spellcaster skill): Laser Projectile, Free Aim, travels 15m in 4s, Slash + Pierce only, persistent damage every 0.5s, 5m orb radius/pull (orb about Greydwarf Brute size), passes through enemies, stops on walls/physical objects, pulls Small only; Big/Boss get Cripple 3s (refreshes per hit).
- Sorcerer: cannot Block/Parry and cannot equip Shields (auto-unequip with message on becoming Sorcerer). Melee penalty is only Warlock's -70%. Spellcaster = attack interval -50%. Wizard +40% Eitr regen only during Overcharge.
- Judgement Mark: only Paladin's Ascended Righteous Strike and the Ascended Signature (whichever of Goddess Relic / Judgement Hammer is Ascended) apply it; normal Signatures don't. A Mark-applying skill hitting a marked target detonates it.
- Advancement prerequisites: Lv16, the AC's MC skill at Tier 7, AND all 14 MC Tier Points spent. MC points never carry into the AC pool; MC tree locks after Advancement.
- Ascensions (free for now, no quests): limits 1 Ascended MC skill, 1 Signature, 1 normal AC skill, + Ultimate Ascension. Only level + Tier requirements until quests exist. Class Reset before Advancement stays free.
- Losing levels (commands) below 16/32/42/50 voids Advancement/Ascensions ("annulment"); spent > earned points auto-reset that pool.
- XP: own system, EpicMMO-style creature table (saved in docs/reference/epicmmo_xp_tables), x2 per star, auto entries for unknown creatures. See docs/reference/MOD_REFERENCE_NOTES.md.
- Death: -5% total XP, protected floor = 10% of total XP and never below current level; no loss for 10 min after any death.
- Guilds (own system, later): active guildmates within 100m get 80% of kill XP each; quest rewards individual.
- Level-up rewards: Tier Points + message/effect only. Attribute points come later in a separate mod; expose level/XP API + events for it.
- Commands (admins on servers, anyone in single player): `/ih [player] <cmd>`; level N / -N, xp N, classtierpoints N, actierpoints N, resetskill all|<skill>, class <mc>, advance <ac>, ascend <skill>, info. Names with spaces in quotes.
- Tree state B1 (sealed AC panel with checklist) chosen over B2.

## Tier / progression (v0.18.0 implemented for Paladin)
- Each Tier = +10% damage AND healing of that skill (`[Progression] TierPowerPercent`; placeholder until real Tier effects are designed). DoTs are not scaled yet.
- Unlocked skills start at Tier 0 and are usable at Tier 0 (0 = not upgraded). Ultimate too: castable at Tier 0 from Lv36.
- Tier display = Option C (approved): stars (7 Class / 5 AC / 3 Ultimate) + "n/max" count under the nameplate, - / + flank the stars. Tier Points counter replaces the "hover for ..." header line (approved). Tooltips = RPG description + full stats (damage incl. Tier bonus, area, effects, cooldown, cost).
- Data in `m_customData`: `ImmortalHeroes.Level`, `.Tiers` ("id=n;..."), `.Ascended` (comma list), `.BonusClassPoints`, `.BonusAdvancementPoints`. Code block "v0.18.0 Immortal Heroes progression" in Advanced.cs (Ih* methods); `DragonCombat.SkillPowerProvider` lets Skills.cs scale Cleric skills.
- 80% gate rule implemented as: points spent >= round(0.8 x points earned at the gate level) -> Lv24 needs 3, Lv32 needs 6.
- Righteous Strike is Ascended automatically for an advanced Paladin. `[Testing] AscendedSkills` and `[Testing] UnlockAllSkills` still exist for testing.
- `/ih` registered as a Terminal.ConsoleCommand (chat `/ih ...`, F5 `ih ...`); only the local player for now (other players need server sync).
- Backdrops: `Cleric_Paladin_Reference.png` (advanced: RS Magenta + badge, LZ Cyan) and `Cleric_Paladin_PreAdvance.png` (all Class skills Cyan); header subtitle text removed from both (code draws it).
- v0.18.1 done: Cleric (pre-Advancement) + Paladin cast from the tree hotbar (`[Hotbar]` bindings; `DragonCombat.TreeHotbarProvider`, Skills.cs skips its fixed keys, `SkillsPlugin.CastFromHotbar`), new HUD mirrors the tree hotbar, Heaven's Light Grace (`[Paladin Heavens Light]`, `DragonCombat.GrantNoEquipmentPenalty`), ADVANCE plaque + checklist in the sealed Paladin panel, Altar Paladin advancement patched to require Lv16 + RS T7 + 14 spent, ASCEND plaque (double click) in the footer, colored tooltip keywords (vanilla-like).
- v0.18.2: locked nodes copy their region from `Cleric_Paladin_Locked.png` (greyed nodes; `LockedRegions` in code must match `LOCKED_REGIONS` in build_ui_assets.py) + padlock; Grace slot greyed/padlocked before Advancement; stars/padlock load with mipmaps; LZ hotbar icon built with the same Cyan frame as RS/HW.
- Tooltip style (approved direction): Valheim serif, description first, then "Subject - value" lines. White = info, Yellow = subject, Cyan = damage types (incl. Fire Burn / Spirit Burn). No other colors; Radius/Range/Duration/Stamina Cost/Wind Up Time/Cooldown are separate lines.
- Still open: B1 sealed-panel art, DoT Tier scaling, other ACs.
- v0.19.0 (ChatGPT): Priest tree/hotbar/HUD/Tiers, PALADIN/PRIEST preview selector before Advancement, `IhTemplateSlot(id)` maps every Priest id to its Paladin slot (anchors, lock regions). Tooltip rule changed there: Cyan = numeric values + units (not damage names); Tier fractions White; no Hotbar row.
- UNIVERSAL TREE RULE (v0.19.1): every AC tree uses the Paladin chrome and Paladin node slots pixel-for-pixel. AC art from a separate painting is never copied as a panel: `tools/build_priest_assets.py` cuts each node / title / Grace box, auto-aligns it to the Paladin slot (edge correlation) and composites onto the Paladin backdrop -> `Cleric_<AC>_Reference.png`, `Cleric_<AC>_Locked.png`, `Icon_<id>.png` (Paladin slot frames). Copy this for future ACs. Source paintings live in docs/source_art.
- ALTAR (v0.19.2, implemented in Core.cs): the user's concept `docs/source_art/Altar_Concept.png` (802x687) IS the approved reference - do not restyle it like the Skill Tree. `tools/build_altar_assets.py` bakes static parts into `Altar_Backdrop.png` (2x) and cuts sprites (card highlight, dialog, emblems, heading star, bottom-left blank); Core.cs IMGUI (`OnGUI`, Altar* methods, concept-px layout via `AltarR`) draws everything dynamic and reuses Core's existing state/actions (FocusBaseClass, Request*Confirmation, ConfirmPendingSelection, ResetClassSelection...). Jotunn panel = fallback if art missing. Advancement page: emblems are the selector, SKILLS/PASSIVE bottom-left. Compile check now also compiles Core separately (tools/compile_check/StubsCore.cs).
- ALTAR ADVANCEMENT PAGE (v0.19.3): concept `docs/source_art/Altar_AC_Concept.png` (794x675, stretched to 802x687) -> `AltarAC_*.png` via `build_ac()`; Core.cs `DrawAltarAcPage`. User rule: follow the concept's design/art exactly but FIX its content holes (concept SKILLS list was incomplete -> code lists all 6 skills per AC + Mastery and Grace under PASSIVE).
- TREE FRAMES (v0.20.4): frames keep their colour when locked; only the art inside the painted opening greys (`Tree_LockMask.png`). Openings = `IhFieldRect` (= FIELDS in `tools/build_tree_frames.py`); Priest art is blitted donor opening -> slot opening (`IhPriestArtworkRect`). Frame colour = skill category via `IhSkillFrameColor` (Ascended Magenta, Ascended Ultimate Red, Ultimate Maroon, Signature Navy, Buff/support Green: Ray of Hope, Divine Intervention; else Cyan); when it differs from the painted slot colour a `Frame_<slot>_<color>.png` overlay is drawn (and stamped into the Priest canvas so its hotbar icons match).
- ALTAR CARDS (v0.20.4): no dark margin around the cards (`tools/build_altar_cards.py` from docs/source_art/Altar_ClassCards_v0.20.3.png); text veil 30% + soft text glow so card art shows behind the name/role.
- Only Paladin moves to the tree hotbar; other ACs stay on fixed `[Hotkeys]` until Paladin is finished, then copy it (universal tree).
- F8 Config window (DevTools, rewritten v0.18.0): every setting of every module, tabs + search, auto-saves to the .cfg, same Section/Key across modules edited together, Test Cooldowns toggle.
- v0.20.5 F8 layout (user goal: always easy to understand): tabs Warrior | Cleric | Sorcerer | Progression | Testing | General; class tabs have a "SHOW SETTINGS FOR" dropdown (Base Class / its ACs, `GroupFor`). Progression tab = Level, Class/AC Tier Points (bonus via `DevSetBonusPoints`), reset, ASCENDED/NORMAL switches (write `[Testing] AscendedSkills`); Advanced exposes `DevPointSummary` / `DevCharacterName` / `DevSetBonusPoints` / `DevCommand` (reflection, /ih rules). Keys/Hotbar are NOT in the window (set in the Skill Tree).
- v0.20.6 universal tree geometry (measured on the chassis, reuse for every AC): nameplate anchor x = plate/frame centre; AC plate blank widths = plate width - 14 (`IhLabelWidth`, JH 103); live names shrink to fit; stars + count centred as one group; padlock = `IhCornerLock(IhFieldRect)` (corner 4 px past the opening); AC title centred at x 665 (blank 596-734, keeps the gold star); Ascended nodes get the permanent badge. Hotbar icons for every branch = `IhComposeHotbarIcons` (hotbar frame of the category colour + tree opening art, 3 px inset). Tier tooltip line names Damage / Healing per skill (`IhTierBonusLabel`).
- F8 class tabs (v0.20.6): Base Class sections first, then a collapsible header per AC (user missed the v0.20.5 dropdown).
- Working style (user, 2026-10-03): find and fix misalignments / inconsistencies proactively (anything off by even a few px); prefer text over preview images when an image costs a lot.
- SCOPE (user, 2026-10-04): Cleric is done; now Warrior + Sorcerer and their ACs, one Advancement per build (pace builds so a session never burns out). Keep universal uniformity (design, art, placement).
- v0.20.7 decisions: Fallen Angel is renamed **Angel Comet** (display; id stays `fallen_angel`). Priest Grace = **Heaven's Crucible** (id stays `grand_sigil`), Priest Mastery = **Bless Thy Sinners** (GuardianAngel death-save). Tooltip value colour = the skill's frame colour (`IhAccentFor`). Branch selector = plaque. F8 hides retired sections/Legacy keys (`IsRetired`) and shows clean names (`DisplaySection`, spaced keys).
- v0.20.8 (user: "the docs are updated, follow them"): Framework doc = source of truth for mechanics too. Implemented Cleric's Blessing HP/regen/movement, Holy Trinity (`DragonCombat.IsHolyTrinityActive`; old Elemental Savant/Holy Knight retired via `IsPaladinPassive` = false), Heaven's Crucible values (`*_v0208` keys) + 30% Armor snapshot, Bless Thy Sinners 6s / 40 min / -70% stamina use (`DragonCombat.ApplyStaminaUseCut`, Player.UseStamina prefix). v0.20.9 Buckler Parry done: CombatRuntime BlockAttack prefix doubles parry for Priest + `DragonCombat.IsBuckler`, timed-block check `IsInParryWindow` (m_blockTimer < 0.25) -> `DragonCombat.BucklerParryHandler` -> Advanced `OnBucklerParry` (Hyper Armor 5s every parry, `_parryEmpowerPending` consumed by the next damaging skill that really starts in `IhCastSkill`, `IhEmpowerFactor` via DamagePower / LZ+RS provider for `IhSkillInstanceSeconds`; Holy Shockwave 10m Spirit 60 default, Stun non-bosses, 15s CD).
- Colour rule (user): non-damaging skills are Green; any skill that deals damage is Cyan (Holy Wave Green, Divine Intervention Cyan; Ray of Hope Green as the Framework's Buff example).
- Altar skill inspection shows the description only (first paragraph of the tree tooltip), no stats.

## Priest Ascended (approved 2026-10-03, implemented v0.21.0, `Priest * Ascended` config sections)
- Sanctified (ally mark, 10s) from Ascended Holy Wave + Ascended Signature only; re-applying = Bloom (15% heal + 4m Spirit pulse, removes it). Holy Wave = Priest's Ascended MC (auto on Advance, permanent).
- Holy Wave 10m + echo 5m/50% after 2s, x2 instant heal <30% HP · Lightning Relic 14m, 3 arcs, Sanctify, end blast 8m Stun Small · Holy Relic 14m, +30% buffs, cleanse, Sanctify, end heal 25% · DI dual Cross Cast, 250 Barrier, inward stagger · Grand Cross 20m/35m + 8m burst Stun Small/Big · Heaven's Judgement 14m, 3s, beams heal 3%, Pillar · Tempest follows Priest 12m, allies +20% Def + Hyper Armor, end Zap detonation.
- v0.21.1: Advance prereq per branch (Priest = Holy Wave T7); Ascended Holy Wave castable on an aimed ally, no wind up; DI heal 20%->50% at T5; Angel Comet WindUpTime 2.5s (jump+dive); RS/JH/SC icon art via tools/paint_skill_icons.py (originals in docs/source_art/backdrops_v0210).
- User rule: be short, no fluff, min-max tokens.
- QA rule (user, 2026-10-04): scan before sending every pass. Anchor = last user-approved build (currently v0.21.2, commit 6af4931, for Cleric/Paladin/Priest): diff against it and check that approved behaviour/visuals did not change unless asked. Measure screenshots with pixels, never eyeball.

## Balance + Warrior/Sorcerer Ascensions (user decisions 2026-10-04)
- Framework doc updated 2026-10-04 with all of this (Ascended lists for Sword Master / Mercenary / Priest / Wizard / Spellcaster, Punishing Bomb, burns, Zap 2s, Boss rule, Fallen Angel -> Angel Comet).
- Plan file: `docs/design/BALANCE_HANDOFF_v2_RECONCILED.txt`. Authority: Framework + our discussions > user's approved Ascension list > old ChatGPT handoff numbers.
- Universal (done v0.21.2): all burns deal the skill's own damage (no 3% current HP; legacy key off); Zap explodes after 2s (`ZapDelay_v0212`); Bosses never Stunned (central `DragonCombat.Stun` guard), can be slowed (50% strength, max 30%), pulled at 20%.
- Cleric numbers (done v0.21.2): Hammer damage cap 5x (`MaxDamageMultiplier_v0212`), no size cap (hitbox keeps growing); Holy Relic 4%/pulse + Tier; DI Barrier armor 30%; Parry +35%; Ray of Hope heals by recipient Max HP; Ascended Tempest Wrath Zap instant (ApplyZap delay < 0); Ascended Heaven's Judgement beam heal stays 3%.
- Warrior/Sorcerer: Heavy Slash = single strike (Ascended 140%); Ascended Moonlight stays Ghost; Halfmoon Ascended keeps Spirit Burn 10s + Stun; Stomp 0.5s between impacts (Ascended 3m -> 10m -> 15m); Mercenary Reaver's Orbit replaced by Punishing Bomb; Meteor Fall gets a 3-stack charge; Cataclysm Ascended 2nd bombardment = 60% of the charged first; Arcane Phalanx normal 4 swords / Ascended 8; Ascended Afterimage clones 45s cooldown each; Gravity Blast Ascended adds an end burst 130% D; Spellcaster = 5 Ascensions (Void Step has its own Lv42 quest). No marks for these ACs.

## Universal kits (v0.22.0)
- `IhKit` table in Advanced.cs (`IhKits`): Class, AC, 3 Class skills (-> chassis slots LZ/RS/HW), 5 AC skills (-> GR, JH, SC, FA, RoH slots: 2 Signatures, Lv24, Lv32 pair), Ultimate (ES slot), Grace (HL slot), Ascended Class skill, optional SpecialAscension (Spellcaster void_step = own Lv42 Ascension). `IhKitFor/IhKitOf/IhPlayerKit/IhTreeKit/IhClassSkillsOf/IhBranchesOf`, `IhTemplateSlot` is generic. Paladin/Priest keep their hand-made node arrays; other kits build nodes from the Paladin rects (`IhKitNodes`).
- Skill ids: Warrior heavy_slash, impact_wave, impact_punch · SM moonlight_splitter, crescent_cleave, blade_storm, frenzied_charge, eclipse, halfmoon_slash, knights_guidance · Merc stomp, circle_swing, bonecrusher, seismic_guillotine, punishing_bomb, whirlwind, battlecry · Sorcerer flame_burst, glacial_descent, stonefang_eruption · Wizard meteor_fall, gravity_dominion, astral_railcannon, astral_greatblade, frost_nova, elemental_cataclysm, clockwork · Spellcaster arcane_phalanx, afterimage_arsenal, void_step, rift_echo, gravity_blast, arcane_rupture, rift_walker.
- Tree art per kit: `IhBuildKitCanvas` copies the chassis, blanks the CLERIC title (145,91,118,25) and Class plates (text widths LZ 77 / RS 80 / HW 74; RS blank 86 because the painted text spans x 161-239), blits `ImmortalHeroesAssets/<Class>_<ACnoSpaces>_Artwork.png` (1011x662, openings at the chassis IhFieldRect positions) or a dark placeholder + live initials (`IhDrawPlaceholderInitials`). User will provide paintings (like Cleric_Priest_Artwork); align them to the chassis with a build script before shipping.
- Casting: Warrior / Sorcerer use the tree hotbar (`IhUsesTreeHotbar` = any kit Class). Skills.cs `CastFromHotbar` handles all Class skills; Sword Master/Mercenary in Advanced `IhCastSkillNow`; Wizard/Spellcaster via `DragonCombat.RegisterSkillModule` (Sorcerer.cs `CastFromTree` / `CooldownForTree`). Not built yet (message): Frenzied Charge, Eclipse, Punishing Bomb, Gravity Blast, 4 Graces (`IhKitPending`).
- Hotbar save key: Cleric keeps `HotbarLayout.<AC>`; others `HotbarLayout.<Class>.<AC>` (`IhHotbarSaveKey`).
- Compile check now includes Sorcerer.cs (Stubs: Hoverable/Interactable).
- v0.22.1: AC header strip centre x 677 (strip 577-776, rect 580,122,194,18); branch plaques 88 wide at x 794 + i*92 with one shared fitted font; IhGraceFor falls back to the previewed branch before Advancement.
- v0.22.3 Sword Master done (block "v0.22.3 SWORD MASTER REWORK" in Advanced.cs, `BindSwordMasterV0223`): all normal + Ascended skills, Frenzied Charge, Eclipse, Knight's Guidance, Mastery (+20 Sword / +50% AS with exactly one Sword, Block/Dodge interrupt via `_smInterruptAt`), stacking burns (`IhAddBurnStack`, universal 0.5s ticks), tooltips (`IhAppendSwordMasterStats`). Altar cards use measured frame bounds (`AltarCardBounds`) and text centred on the art panel (+78 px).
- v0.22.4 Mercenary done (block "v0.22.4 MERCENARY REWORK", `BindMercenaryV0224`): Warfreak (+10 Sword/Axe/Clubs, +50% AS `HasWarfreakWeapons`, +30% Armor in ArmorPostfix, physical weapon movement penalty removed in CombatRuntime `WeaponMasteryExemptPenalty` (also Sword Master swords)), Fury auto at 100 (`ActivateUnchainedFury`, 20s, 180s lockout, drain), Punishing Bomb, Battlecry (`PatchEnvironmentDamage` + `_battlecryEnvUntil`), Ascended Heavy Slash/Stomp/Circle/Bonecrusher/Seismic/Bomb/Whirlwind. Barbaric extras retired. Next: Wizard, then Spellcaster.
- v0.22.2: HUD key labels use `_hudKeyCenterStyle` (UpperCenter); Altar shows Icon_righteous_strike_Normal.png (Icon_righteous_strike.png is the Ascended magenta frame).

## Ascended designs (approved so far; tooltip header "ASCENDED - <SKILL>", names unchanged)
- Paladin Righteous Strike (Ascended MC): 7m, applies Judgement Mark, Expose 8s, a second smaller strike (3m, 0.5x) when it detonates a Mark; also spawns 12 Lightning Trails in all directions, 7m range, faster than Electric Smite's trails, each applying Spirit DoT for 6s.
- Paladin Ray of Hope (Ascended): cleanses Burn/Poison/Frost from allies + 150 HP Barrier (on top of the instant normal version).
- Testing: until Ascension exists, `[Testing] AscendedSkills` (comma list of skill ids) turns on Ascended versions.
- Paladin Shield Charge (Ascended): 20m budget, Hyper Armor while charging, Bash = 7m cone that also Stuns Big; charge hitbox radius 4m -> 6m.
- Paladin Fallen Angel (Ascended): impact leaves a 10m ring for 6s; inside it enemies get Spirit Burn + Fire Burn (3s, refreshed while inside); Hyper Armor through the dive + 3s.
- Paladin Electric Smite (Ascended Ultimate): keep 16 trails; adds a 6m Thunderstorm for 4s, hits every 0.5s: Lightning damage + Fire DoT + Spirit DoT (3s, refreshed per hit). Smaller/weaker Lightning Tempest.
- Paladin Goddess Relic (Ascended): 10m damage radius, heavy Blunt + Lightning, applies Judgement Mark, cross 3x size.
- Paladin Judgement Hammer (Ascended): after it stops (wall or max range) it flies back to the Paladin keeping its grown size, hitting everything again; catching it cuts its cooldown by 30%; applies Judgement Mark.
- Base changes: Goddess Relic radius 7m -> 5m, cross about the size of a 0-star Troll. Ray of Hope and Holy Wave: no wind-up, instant cast with 0.5s movement lock, animation = main hand raised like chanting.
- Rule: instant-cast skills lock movement for 0.5s (was 0.4s in the Framework doc).
- Range/radius reference: the MyDirtyHoe grid in-game is THE ruler for every meter value we discuss (user's 10m radius screenshot, 2026-10-02). 1 Unity unit = 1m.
- Goddess Relic normal cross 6.5m x 3.5m (0-star Troll); Ascended cross ~13.8m (approved). Judgement Hammer (v0.17.2): upright, front-flips; starts Greydwarf Brute size (2.5m x 0.8m), grows every 0.2s, ~7.4m x 2m at 20m (normal cap 8m x 2m); Ascended keeps growing on the return up to 10m x 3m (hard cap, never infinite).
- Code status: v0.17.0 implemented Judgement Hammer + Fallen Angel (Divine Verdict / Aegis Fall code kept but unused) and all Paladin Ascended versions behind `[Testing] AscendedSkills`. NOT implemented yet: Frenzied Charge, Eclipse (code still has Severed Horizon / Empty Sheath), Gravity Blast. Graces are not implemented as M4+R skills yet (only legacy activatable passives).

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
- Hotbar: 7 numbered slots + Grace (M4+R). Permanent (can't be removed): Signature, Ascended, Ultimate. Everything else removable. Drag & drop rules: slot->slot swaps; removable dragged off = removed; tree->empty = place; tree->removable = replace; tree->permanent = insert + shift right into nearest empty (left if none right); full + permanent target = nothing. Grace slot is separate and never dragged.

## Skill tree UI
- No visible C1–C5 labels. Only skill name under node; category goes in tooltip. No connector arrows.
- Hover Class name = Blessing tooltip; hover AC name = Mastery tooltip.
- Cleric → Paladin layout:
  - Class: Lightning Zap, Righteous Strike, Holy Wave.
  - Paladin: Goddess Relic, Judgement Hammer, Shield Charge, Fallen Angel, Ray of Hope, Electric Smite (Ultimate).
  - Grace: Heaven's Light.
- Node colors: Normal = interchangeable skills (Class AND AC) Cyan — before Advancement all 3 Class skills are Cyan · Buff Green (e.g. Ray of Hope) · Signature Navy · Ascended Magenta · Ultimate Pastel Maroon · Ascended Ultimate Red · Grace Yellow/Gold.
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

## Build & repo layout
- 8 source files at repo root → 8 DLLs, built on the user's Windows PC by `INSTALL.bat` / `INSTALL.ps1` (PowerShell `Add-Type`, staged compile, then installs to `BepInEx/plugins` + `ImmortalHeroesAssets/`).
- `Add-Type` uses the **legacy C# 5 compiler**: no `$"..."` interpolation, no `?.`, no `nameof`, no expression-bodied members, no auto-property initializers, no out-var/tuples/pattern matching. Watch local-variable scope collisions across blocks.
- UI art: `ImmortalHeroesAssets/Cleric_Paladin_Reference.png` (1011×662, the approved reference). Procedural renderer is fallback only.
- Old changelogs in `docs/changelog/`, past UI previews in `docs/previews/`.
- Package releases as `Immortal_Heroes_vX.Y.Z_<Name>.zip` with the same flat layout as the repo root (sources, INSTALL.*, ImmortalHeroesAssets/, CHANGELOG + TEST_THIS_BUILD).
- Valheim uses Linear color space: IMGUI showed the artwork gamma-lifted (in-game = source^(1/2.2)). v0.14.7 compensates the backdrop texture at load (`ApplyLinearColorSpaceCompensation`). Code-drawn colors/textures (tooltips, +/- buttons) are still uncompensated.
- Before packaging ALWAYS run `tools/compile_check/check.sh` (semantic compile of Advanced + CombatRuntime + Skills + DevTools vs a known-good baseline with stubbed Valheim/BepInEx types; any new error signature = real bug; add real Valheim members to Stubs.cs when they show up as noise) plus `mcs --parse -langversion:5` on every edited .cs. The real compile still happens on the user's PC.
- UI art pipeline: `tools/build_ui_assets.py` builds every PNG in `ImmortalHeroesAssets/` from `docs/source_art/` (reference v0.14 + approved footer target). Edit the script, never hand-paint the output. `tools/render_preview.py` renders in-game-size previews into `docs/previews/`.
- Hotbar keys: `[Hotbar]` config (SlotN + SlotNModifier, GraceKey + GraceModifier; Modifier None = single key) is rebindable in the tree; still a prototype — casting uses `[Hotkeys]` until the tree hotbar is wired to the runtime.
- Node control positions come from `ReferenceNameplateAnchors` (nameplate center x, bottom y, reference px).
- Hotbar is dynamic since v0.16.0: icons are `Icon_<skillId>.png` + `Slot_Empty.png`, layout saved per character in `m_customData` key `ImmortalHeroes.HotbarLayout.<Advancement>`.
- Tree states ideation (approved direction): A = no Class (neutral panels, "CHOOSE A CLASS AT THE ALTAR FIRST"), B = Class only (Class skills all Cyan, no badge; AC blank or sealed, "ADVANCE AT LV 16"), C = locked skills (grey + padlock + unlock text). `tools/render_states.py`.
