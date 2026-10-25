# Aethelborn Ascended (AA) — Handover

Written 2026-10-26 by Claude at v0.25.294 (branch `claude/admiring-allen-lddoh3`, pushed).

Read this whole file before you touch anything. Then read `CLAUDE.md`: its dated journal records every pass, and its 53 numbered LESSONS are its most valuable part. `AGENTS.md` holds the older design rules. The user's latest message always overrides older rules.

---

## 1. The user and how to work with them

- **What they make:** a large Valheim mod. They test every build in game and send `LogOutput.log` files, cfg zips, screenshots and short videos.
- **What they expect:**
  - precise work;
  - no repeated mistakes;
  - minimal wasted turns (usage limits matter to them).
  - They get angry, justifiably, when an instruction they already gave is ignored. Re-read their earlier rules (section 4) before every pass.
- **Do only what was asked.** Then read the **entire** skill you touched, from start to finish (cast, cancel, death mid-cast, Normal *and* Ascended). Report any flaw you find, or fix it if it's clearly a bug.
  - Never make design changes they didn't request.
  - If you notice something off that you didn't fix, **tell them**. Silence is a failure ("at least have the decency to tell me").
- **Wait for permission when you've only proposed something.** Example: the Astral Greatblade slam fix (section 8) is still waiting for their "go".
- **Every reply** must list every change per skill, with Normal and Ascended handled separately. Keep static checks (compile) separate from in-game verification. Never claim something works in game.
- **Use they/them.** Their name is unknown.

## 2. Project layout

- **Repository:** `/home/user/repositornation` (GitHub `albedo07/repositornation`). Develop on the designated branch and push.
- **Ten separate C# modules, compiled as C# 5.** Never merge them into one DLL unless asked.
  - `DragonsAltar.CombatRuntime.cs` — **Runtime**, the core, about 25k lines. Contains:
    - DragonCombat: damage, stuns, animation, CA (custom animation);
    - DragonVfx: every visual effect;
    - DragonSkillAudio: the whole sound system;
    - DragonFirstCast / DragonCaTrace / DragonIdleWatch: diagnostics;
    - DragonPerf: F7 monitor.
  - `AlbedosCustomClasses.Advanced.cs` — Warrior/Cleric advancements (Sword Master, Mercenary, Paladin, Priest), HUD, skill tree UI, tooltips, main menu.
  - `AlbedosCustomClasses.Core.cs`, `.Skills.cs`, `.Passives.cs`, `.GuardianAngel.cs`.
  - `DragonsAltar.Sorcerer.cs` — Sorcerer base, Archmage (internal name "Wizard"), Horizon Walker (internal name "Spellcaster").
  - `DragonsAltar.Ranger.cs` — Ranger base, Acrobat, Bowmaster.
  - `DragonsAltar.DevTools.cs` — the F8 config panel and the **UserDefaults** table (the user's cfg values compiled in as defaults).
  - `Aethelborn.Experience.cs` — the EXP system.
- **Assets:** `ImmortalHeroesAssets/` holds PNG art and icons (`Buff_*.png`). `ImmortalHeroesAssets/SFX/*.wav` holds every custom sound.
- **Installer:** `INSTALL.bat` / `INSTALL.ps1` (PowerShell, which compiles with Add-Type on the user's machine).
  - The installer keeps a hard-coded list of required WAVs (`$sfxNames`) and a count text ("66 skill sound files"). Update both whenever a sound file is added or removed.
  - Keep the UTF-8 BOM.
- **Tools:**
  - `tools/realbuild.sh SRC` — compiles all ten modules with `mcs -langversion:5` against the user's real game DLLs, using the installer's exact reference set. The references live in Claude's scratchpad and are **not** in the repo. Ask the user for their DLLs or reproduce the reference set: BepInEx, 0Harmony, assembly_valheim, SoftReferenceableAssets, UnityEngine*.dll, netstandard. Do **not** reference assembly_utils, and use a Jotunn stub for Core.
  - `tools/paint_icons.py` — paints every status icon (PIL).
  - `tools/build_sfx.py`.
  - `tools/handover/` — scripts copied out of Claude's scratchpad for you (see section 6).

## 3. Release procedure (every pass, in this order)

1. **Make the change.**
   - Use scripted edits with **asserted anchors** (`assert s.count(old) == 1`), and compute all files before saving anything (atomic).
   - Parallel tables are only ever extended by a generator that asserts every array's count (section 6).
2. **Bump the version only in these places:**
   - every `public const string ModVersion = "0.25.X";` (nine files);
   - `[BepInPlugin(..., "0.25.X")]` in `Aethelborn.Experience.cs`, line 14;
   - `INSTALL.bat` line 2 and `INSTALL.ps1` line 4 (the banners).
   - **Never use a blanket sed**: one once rewrote comments and config descriptions.
3. **Build:** `bash tools/realbuild.sh .` must show no `error CS` for any module.
   - A CS0012 naming assembly_utils is only the auto-add trigger, but never use assembly_utils extension methods (`GetStableHashCode` and similar). They compile here and fail in the user's installer.
4. **Changelog:** write `CHANGELOG_v0.25.X.txt` (per-skill list, Normal/Ascended, static checks vs not tested in game). `git mv` the previous changelog to `docs/changelog/`. Rewrite `TEST_THIS_BUILD.txt`.
5. **Journal:** append a `CLAUDE.md` journal line, plus new numbered LESSONS if a mistake was made.
6. **Commit** with the attribution trailer. **Push**: `git push -u origin <branch>`.
7. **Zip** (about 75 MB):
   ```
   zip -qr AA_v0.25.X_Name.zip . -x '.git/*' -x '*.dll' -x 'build/*' -x 'performance_ascended/*' -x 'docs/*' -x 'tools/*' -x 'CHANGELOG_v0.25.2[0-7]*' -x 'ANCHOR_*'
   ```
   Then send the zip to the user.

## 4. The user's standing rules (never break these)

**No-touch zones**
- **Warrior, Cleric and Horizon Walker (Spellcaster)** bloodlines are finished. Don't change them unless the user asks.
- A shared Runtime fix that also affects them must be stated explicitly. Example: v0.25.294's emote_stop fix also covers Gravity Dominion and Warrior/Cleric emote skills.

**Sound rules**
- **Never cut a sound or music.** Every stop is a gradual fade: Length slider, caps, interruptions, menu exit.
- **Wind-up/charge sounds fade in.** Instant impacts (explosions, slams) start at full force and are the loudest. Every end fades.
- **Loops:** use a prepared seamless steady section, or the climax section when a charge is held. Never make a loop with an audible cut.
- **Handovers:** one sound hands over to the next with an overlap, and the **climax lands on the event**.
  - Handover math: two fades on the same dB curve crossing at the same time leave a ~15 dB hole. Bring the new sound to full level first, then fade the old one.
- **Projectiles carry their own sound.** It follows the projectile, never the caster. "On hit" sounds must also play where the projectile strikes the ground, not only on creatures.
- **Every stacked sound is its own stem with its own F8 slider.**
- **No reverb.** No SFX on cast animations alone.
- **Native Valheim sounds stay at Valheim's loudness.** Never level them.
- **Measure before timing a recording:** leading silence (trim it, keeping a 5 ms pre-roll and a 3 ms fade-in), the hit marker, and loudness against the existing mix.

**Visual rules**
- **Blast vs Explosion:** a Blast is ground/pressure (dirt jets, no dome). An Explosion is fire (a dome is OK).
- **Named references:** when the user names a reference ("beams like Heaven's Judgement"), reuse that reference's actual technique and helper functions. Don't invent a lookalike.

**Config rules**
- **No apostrophes** in BepInEx config section or key names. They crash Runtime.Awake (v0.25.218), so it's "Hawks Vigil", not "Hawk's Vigil".

**Performance rules**
- **Measure in the user's game first**, using the tracer lines in their log (section 7), not a static guess.

## 5. Sound system (DragonSkillAudio, in Runtime)

- **Cues:** `enum DragonBaseSound` holds 210 cues (`CueCount = 210`).
- **Parallel per-cue arrays** (all length 210), all with a 1:1 cue→layer mapping (`CueLayers[i] = i`):
  - `Prefabs`, `Items`, `Stages`, `CueSkills`, `DefaultMarker`, `DefaultPitch`, `DefaultSpeed`, `DefaultBass`, `DefaultSync`, `DefaultLength`, `DefaultLayers`, `UserLayerGain`, `RebalancedLayers`, `CueLayers`, `CustomFiles`, `sections`, `names`;
  - `PhaseGaps` has length CueCount−14.
- **Per-skill arrays** (length 71): `SkillNames`, `SkillClasses`, `DefaultVolumes`, `Rebalanced`, `PreviousFactoryVolumes`, `SweetSpotFrom`, `UserSkillGain`, plus `SkillVolumes = new ConfigEntry<float>[71]`.
- **Skill index map:**
  - 0–11: base classes (9 Piercing Arrow, 10 Tumble Shot, 11 Snare Trap);
  - 12–18: Sword Master; 19–26: Mercenary; 27–33: Paladin; 34–39 and 41: Priest; 40: Level Up;
  - 42–47: Archmage; 48–53: Horizon Walker; 54: Clockwork; 55: Rift Walker;
  - 56: Bow Shots (shared Ranger); 57–63: Acrobat (Gale Volley, Cyclone Arrow, Swallow Dive, Skyfall Barrage, Somersault Dance, Furious Winds, Tailwind); 64–70: Bowmaster (Ballista Shot, Arrow Rain, Pinning Shot, Explosive Arrow, Splitting Arrow, Starfall Volley, Hawks Vigil).
- **Raw cues** (`RebalancedLayers` = true) use custom WAVs and play through `PlayRaw`.
  - Gain = `BuiltinGain(skill) × Loud(SkillVol) × Loud(LayerVol) × Loud(UserLayerGain)`, where Loud(p) = p/100 if ≤100, else (p/100)².
  - Raw voices are 2D stereo with custom distance falloff/pan from the camera: `Space(source, cue)`. Rift cues use the player-based 2 m → 6 m range.
- **Native cues** (`RebalancedLayers` = false) resolve a Valheim prefab by name (`Prefabs`, e.g. `sfx_bow_fire`) or an item effect list (`Items` + `Stages`).
  - Natives get `LevelProfile` unless `VanillaLevel(cue)` is true.
- **PreLeveled:** files not in the `PreLeveled` HashSet are normalized at load (RMS .40 + soft clip).
  - `Whoosh.wav` and `Buff2.wav` are load-leveled and shared with Warrior/Cleric, so you can't rebalance them for one class.
  - Prefer PreLeveled files and set levels with `UserLayerGain`.
- **API:**
  - `Play(caster, cue, pos, swing, pitchScale)`: the marker (hit point) lands now.
  - `Expect(caster, cue, pos, hitIn)`: called at cast time so the marker lands `hitIn` seconds later. A later `Play` of the same cue within 0.6 s is absorbed.
  - `BeginSustain(caster, cue, pos, seconds, pitch, gain, alive, followCaster, followTransform)`:
    - for **loop cues** (`LoopCue()`), `seconds` is the duration;
    - for **non-loop raw cues**, `seconds` is the **hit time** (the marker lands there) and the whole clip plays;
    - `alive` returning false triggers a 0.3 s fade-out.
  - `SetSustainFades(ticket, fadeIn, fadeOut)`, `SetSustainEnd(ticket, endIn, fadeOut)`, `EndSustain`, `MoveSustain`, `OffsetSustain`.
- **Following moving things:** create an anchor GameObject, move it each frame, pass it as `follow`, and Destroy it when the thing ends (that fades the sound).
  - Example: Ranger `ArrowFlight` with `_nextFlightWhoosh` / `_nextFlightCrash`.
- **Retired cues** are kept in the arrays (never delete entries). They're listed in `RetiredCue()`, so the warm-up skips them and they're hidden in F8.
- **Warm-up:** builds `RawVariant(cue, RawGain(cue, 1))` between casts and logs "Skill sounds warmed".
  - A "Sound copy built during a cast" warning is either a real warm-up gap or a one-time rebuild after the user edited a slider.
- **Loudness targets** (momentary dB, 400 ms window):
  - explosions about −8 to −10; slams/impacts −11 to −13; slashes −16 to −20; whooshes −18 to −20; wind beds/hums −21 to −23; buffs −18 to −19.
  - Compute `UserLayerGain` with `tools/handover/measure_audio.py`.
- **Networking:** raw cues are sent to other players over the `AA_SkillSfx` RPC.

## 6. How to add or change sounds safely

- **Generator template:** `tools/handover/cue_generator_template.py` (adds one cue and edits elements). The full multi-cue + multi-skill example is `cue_generator_full_example_ranger.py`.
  - Its `find_array` / `top_count` / `append` / `set_elem` helpers assert every array's element count before saving.
  - Update `OLD`/`NEW` counts and the enum anchor (the last enum member is currently `StarfallVolley`).
  - Note: `DefaultPitch`/`DefaultSpeed`/`DefaultBass`/`DefaultSync` share one declaration line. Match them with their own `name = {` anchors.
- **Call-site edits:** see `code_edit_example_ranger.py` (an asserted-anchor replace script).
- **New WAVs:**
  1. Decode with ffmpeg at 48 kHz.
  2. Trim leading silence (5 ms pre-roll, 3 ms fade-in, 30 ms end fade).
  3. Measure (marker = first 10 ms window ≥ 70% of the loudest).
  4. Add the file to `PreLeveled`, `CustomFiles`, the installer list and count.
- **Seamless loops:** section length L, with the first `xf` samples mixed with the samples just after L using an equal-power crossfade (sqrt curves). Check the wrap point for dips against the section's own quiet points.
- **User cfg → built-in defaults:**
  1. Unzip their cfg.
  2. `python3 tools/handover/cfgdiff.py <cfgdir>` lists values that differ from "# Default value".
  3. Skip Skill Audio `*_v0xxxxx` one-time flags, Testing `AscendedSkills`/`ForceCooldowns`, and `DefaultsAppliedForVersion`.
  4. Merge the rows into the `UserDefaults` table in DevTools (format `guid|section|key|old|new`), sorted. It currently has 241 rows.
  5. The "# Default value" lines already show the earlier user defaults; never treat them as the code default.

## 7. Diagnostics already in the game (read them in every log)

- **First cast tracer:** `[Aethelborn] First cast <skill>` shows the worst frame split into SFX/VFX/CA/code/GC. `Skill load <skill>` covers the 12 s after a first cast: frames, slow frames, VFX objects, particle systems, GC.
- **CA traces:** `CA trace <weapon state> '<trigger>'` shows which clip really played and at what rate. `CA timing` shows the swing's hit vs the skill's hit.
- **Idle watch:** `Slow period with no skill cast` lists what this mod has alive. All zeros means the slowdown isn't ours. Exactly 33.3 ms on every frame means a 30 fps cap (unfocused window or menu).
- **Fall damage:** `Fall damage: raw -> after (rule, class)`, the first 12 per session.
- **Hand aim:** `Hand aim item '...': staff rule / blade rule`.
- **F7:** Performance monitor overlay. Ctrl+F7 writes a report.
- **Performance Ascended** (a separate mod in `performance_ascended/`, anchor v0.6.5): `[PA UI]` lines time inventory opens (Valheim's crafting rebuild, about 60 ms).
- **Read every Error/Warning line** in a supplied log, group them, and fix or explain each one.
  - "Missing location", "shader platform 18" and "Native clip could not be leveled" are known harmless lines.

## 8. Open items and current state (v0.25.294)

- **Waiting for the user's go-ahead: Astral Greatblade (Archmage) slam timing.**
  - Problem: `Slam.wav` has no clean transient (it's loud from 0 to ~1.4 s). Its marker is 247 ms, so the slam starts ~0.25 s before the blade lands, and the three Ascended slams smear together.
  - Proposed fix: start Slam on the hit (marker 0), shorten it to ~0.6 s with a fade, and let Boulder Crash carry the impact. **Don't do it until they say go.**
- **To verify in game from v0.25.294:**
  - Starfall Volley: ArrowVolley swell → first wave lands at the barrage start → rain loop → fade; HJ-style beams (5 `DragonVfx.Beam` + 2 FallingArrow every 0.25 s); FPS.
  - Big-arrow Boulder Crash on creature OR ground (charged Ballista, Great Arrow); launch whoosh follows the arrow.
  - Tailwind first-cast CA: the emote_stop fix.
- **Not yet confirmed fixed:** the Meteor Fall / Elemental Cataclysm staff charge hand (staffs use the v243 box-centre tip, `ItemTipBoxCentre`). There's been no user feedback since v0.25.290.
- **Acrobat fall damage:** the log proves −75% is working (100 → 25). Raw values are simply big falls.
- **Remaining performance note:** Heaven's Judgement / Lightning Tempest drop to ~120–150 fps from 200, a GPU overdraw type of cost. Already reduced, still open.
- **Anchors** (user-confirmed good builds): AA v0.25.257 (`ANCHOR_v0.25.257.txt`); Performance Ascended v0.6.5. Zips exist for every version, but they were in Claude's scratchpad, so the git history is the record.

## 9. The lessons, condensed (full text in CLAUDE.md, numbered 1–53)

**Process**
- Edit scripts are atomic, with asserted anchors. Parallel tables are only extended by count-asserting generators.
- Always run the real-reference build with the installer's exact references.
- Bump only the version strings.
- Update the installer's file list and count whenever files change.
- Read the native/original code (decompile `assembly_valheim.dll` with `ikdasm`; `monodis` segfaults) before proposing a fix.
- Inserting a line above a statement: check for a braceless `if`/`else` above it (that caused the v240 regression).
- Clear parallel lists through **one** helper (v0.25.293 had a per-frame exception from one list left uncleared).

**Proof**
- When a fix "changes nothing", first prove the code path runs, using a log line.
- Verification logs must measure the visible result, not the value the code just wrote.
- Traces should compare a failing case against a working one in the same log.

**First casts and warm-ups**
- Warm-ups must request the exact cache keys the hot path uses, and must log when they finish.
- There can be two builders for one thing; check both.
- Remember failures: never retry an expensive operation every frame.
- Size caches above the working set.

**Animation**
- Borrowed vanilla triggers need the animator state they were made for (dual wield, greatsword/scythe).
- A self cross-fade has the same hash on current and next; read the *next* state.
- Fire emotes the way Valheim does: `ResetTrigger("emote_stop")` first.
- Use PlayerLoop for code that must truly run last; `DefaultExecutionOrder` isn't reliable in runtime-loaded mods.

**Measurement reuse**
- One proven measurement gets reused everywhere.
- But a measurement change made for one feature silently changes every other caller (Whirlwind tips broke the staff poses), so scope it or list every caller.
- Identify equipment from the equipped `ItemData` (skill type), never from object names ("attach(Clone)").

**UI**
- Never anchor UI to an object found by name without checking its rect.
- To place something exactly where an engine object is, swap the sprite that object already renders.
- Simulate pixel rules on the user's real screenshot first.
- Tooltips are built from config values.
- State icons must refresh their `m_icon`.

**Gates and sources**
- When a feature "never shows anything", log the shared gate's decision.
- Before widening a gate, confirm how the user actually triggered the feature.
- Repeating sources must not count as a "second touch" of their own effect.

**Performance**
- `(int)(t*10)%4==0` spawns objects every frame for part of each period, which is frame-rate dependent. Gate spawns on a next-time field instead.
- A held weapon stream (Gun Staff) is one long attack; copy per real **shot** (the `FireProjectileBurst`/`DoMeleeAttack` hook).
- Independent per-frame features must not share one try/catch.

**Replacements**
- A replacement follows the original's lifecycle and never hands control back to what it replaced (menu music).

## 10. How Claude reduced mistakes (do the same)

1. **Before coding:** re-read the user's rules (section 4) and the LESSONS. Read the real code path end to end, plus the Valheim IL when engine behaviour is involved.
2. **For sounds:** measure every file first (length, onset, marker, loudness). Plan the shape (fade-in/full-force/loop/handover) from the waveform. Check whether the file is PreLeveled and shared.
3. **For bugs reported from a log:** find the exact log line and stack, then the root cause (for example "one of five lists not cleared", or "latched emote_stop trigger"). Fix the cause, not the symptom. Explain it in plain words.
4. **After editing:** read the whole touched skill (cast, cancel, death, Normal, Ascended, multiplayer remote sound), run the real build, and grep for other callers of anything shared that you changed.
5. **In the reply:** give the per-skill change list, what was checked statically vs what needs in-game testing, anything off you noticed but didn't change, and items still waiting for the user's decision.
