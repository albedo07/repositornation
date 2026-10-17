AETHELBORN ASCENDED v0.25.192 - RANGER MOBILITY & BUFF TIMERS

BASELINE
Only Aethelborn_Ascended_v0.25.191_VFX_Finish.zip.
Anchor SHA-256: 2a616dfc4b69484940cfd79e07a08300bc07dc8161416499537b438a95af7783
Original Anchor archive untouched. v192 is a test pass, not a replacement Anchor.

CHANGES
1. Ranger and Bowmaster bows/greatbows use the vanilla Bow attack movement factor.
   Removed the old >1-only correction/full-speed fallback. No global prefab mutation.
2. Acrobat alone keeps full current walk/jog/sprint speed while drawing/firing bows
   and greatbows. Normal stamina, crouch, encumbrance and dodge restrictions remain.
   Intentionally rooted skills have not been converted into mobile skills.
3. All Ranger-family normal bow firing uses the supplied Warfare greatbow's 2.2x
   animation base speed. This is an absolute base, so Warfare's 2.2x does not stack
   with another 2.2x. Existing configurable ChainInterval, fourth-shot bonus/recovery,
   charge mechanics, arrow counts, damage and ammo behavior remain unchanged.
4. Tailwind: current Valheim uses SEMan.ApplyStatusEffectSpeedMods rather than the
   legacy ModifySpeed name. AA now hooks the current method, with a legacy fallback
   only if it is absent. Configured movement bonuses can reach the movement path.
   Tailwind remains +60% by default, applied to current speed; saved settings are kept.
5. Bowmaster: sprinting preserves a sole reload action for the currently equipped
   crossbow, and that reload does not force walking speed. Vanilla reload progress,
   completion, resources, 75% reduction and saved loaded state remain in charge.
   Other queued actions, other classes and other weapons retain vanilla behavior.
6. Priest death-save: persistent Ready/Cooldown indicator using the real self/ally
   cooldown, separate from the existing recovery buff. No cooldown rebalance/reset.
7. Buff indicator audit: new real-expiry indicators for Overcharge, Phase Flow,
   Rift Echo, Arcane Phantom, Arcane Phalanx (swords + time), Rift Walker portals,
   Furious Winds, Fury recovery, Empty Sheath, Sanctified Bloom and Holy Wave healing.
   Hawk's Vigil is refreshed from its actual local state. Short finite timed buffs
   and Hyper Armor now show indicators, while implementation-only passive regen
   refreshes do not create misleading 0.35-second countdowns.
8. Existing indicators retained: Knight's Guidance, Battlecry combat/labor,
   Heaven's Light, Ray of Hope/mending/Purity, Holy Relic, Divine Intervention,
   Eye of the Storm, Barriers, Clockwork, Tailwind, Focus and Priest Holy Parry.
   Focus is stack-based; indefinite Ready/Active states have words instead of fake timers.

SCOPE
Six C# modules changed: CombatRuntime, Ranger, GuardianAngel, Advanced, Sorcerer,
Starter Skills. Other module sources unchanged. Installer changes are title/banner
text only; staging, reference discovery and installation behavior are unchanged.
All 185 assets, full shared VFX/cape library and every world-VFX call site unchanged.
No VFX redesign, cape fix, animation replacement, combat balance or old-log fix.

INSTALL
Use the included INSTALL.bat/INSTALL.ps1 exactly as for v191. This is the same
source + assets package format; the installer compiles all 10 DLLs in staging
against your installed game/profile references before updating the live profile.
No third-party game/dependency DLLs are bundled in this pass.

VALIDATION PERFORMED
- All 10 modules compile as C# 5 libraries against available game/Unity/BepInEx/
  Harmony/Jotunn references. Final CombatRuntime rebuild also passed.
- Local reference repair: existing UnityEngine.UI.dll and System.Xml.dll were
  invalid/truncated. Compile-only UI reference from Unity3D.UnityEngine.UI
  2018.3.5.1 was used; unused invalid System.Xml reference was excluded.
  This is NOT a compile against an exact copy of your installed Valheim 1.0.20
  Managed folder. Your existing installer compiles against your actual files.
- Six checks executed the shipped Ranger transpiler methods: intended drawing
  call rewritten; unrelated calls preserved; queue clear rewritten; unrelated
  queue calls preserved; no-op paths when either call is absent.
- Actual game IL inspected: Humanoid.CheckRun contains the bow-drawing gate;
  Player.CheckRun clears action queues on sprint; queue enum includes Reload;
  Player.InMinorActionSlowdown forces walking during reload.
- Supplied Warfare DLL: AttackSpeedSystem registers GreatbowModer_TW and
  GreatbowBlackmetal_TW primary attacks at 2.2x.
- Anchor checksum, allowed file list, VFX-call preservation and all 185 asset
  byte comparisons passed. ZIP integrity tested after packaging.
- Unity gameplay and visuals NOT run here. Native Unity initialization prevents
  executing live player predicates outside Valheim. Six transpiler tests do not
  prove movement, firing, network buff delivery or reload behavior in-game.

FOCUSED IN-GAME CHECKLIST
[ ] Base Ranger: compare same standard bow and greatbow while walking/jogging/
    sprinting, both quick shots and held right-click draw/release. Greatbow must
    not grant extra/full firing movement. No change to idle equipment permissions.
[ ] Bowmaster: same bow slowdown and common firing animation speed.
[ ] Compare standard/greatbow quick-shot intervals over several four-shot chains;
    preserve existing finisher pause and charged-shot behavior.
[ ] Acrobat: standard bow and greatbow retain current movement while drawing,
    quick firing and releasing charged shots; no extra speed is granted.
[ ] Tailwind: measure distance over 5 seconds on the same flat route before/after;
    default speed should rise by 60%, then return at expiry. Check walk/jog/sprint.
[ ] Bowmaster crossbow: empty it, sprint while it reloads, release/repress sprint,
    verify progress completes, then fire once; no free/duplicate bolts or reloads.
    Check insufficient resources, weapon swap and saved loaded state.
[ ] Other classes/crossbows retain normal reload/sprint rules.
[ ] Priest self life-token: Ready -> lethal-hit save -> real cooldown countdown ->
    Ready. Recovery has its own short timer. Repeat ally protection near a Priest.
[ ] Activate each audited buff/Grace, inspect timer and tooltip, then let it expire.
    Check cancellation, consumption, death and class change for owned duration skills.
[ ] Check HUD readability with multiple simultaneous buffs.
[ ] Regression: AA NACC/block/parry, skills, Altar/hotbar and all VFX unchanged.
