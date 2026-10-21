PERFORMANCE ASCENDED 0.6.0 — ALL-IN-ONE + BATCHED BUILDING DRAWING (experiment) + MOVABLE HUD + FPS COUNTER
2026-10-21
See CHANGELOG_v0.6.0.txt, CHANGELOG_v0.5.0.txt and FPS_MOD_SURVEY.txt first; older notes below are from 0.3.3.

AETHELBORN PERFORMANCE 0.3.3 — CLEANUP ALLOCATIONS
2026-10-21 Asia/Manila

INSTALL / KEYS
Close Valheim, extract the WHOLE ZIP and run INSTALL.bat.
Select ACTIVE PROFILE BepInEx/core/BepInEx.dll, then game
valheim_Data/Managed/UnityEngine.CoreModule.dll. Paste paths in the usual file
pickers if easier. Separate Gale/r2modman profiles work. Staged compilation,
own-DLL backup/replacement; existing config preserved. Client BepInEx5/HarmonyX;
no AA/Jotunn dependency. Ten C#5 production files remain separate.
Ctrl+Alt+F6 toggle; Ctrl+Alt+F10 record; Ctrl+Alt+F9 optional marker.
Existing custom shortcuts remain. Badge shows actual toggle binding.

THIS PASS
Native building destruction unregisters support-cache callbacks from terrain
notifications. Repeated multicast-handler removal can create large amounts of
short-lived garbage in dense builds. This pass replaces that bookkeeping with
an ordered callback registry. Original support cache clearing and invocation
order stay; calculations, physics, native destruction and object lifetime stay.
See FINDINGS_v0.3.3.txt for the inspected native methods and mechanism limits.

New Structures / Efficient Support Cache Subscriptions = true.
Optimization OFF restores native subscriptions. ON adopts eligible existing
native subscribers from Valheim's terrain registry. Foreign event handlers are
preserved; mixed/changed fields fall back to native handling. Existing patches
on native event accessors disable this adapter. Unsupported instruction layout
also leaves native handling in place. No scene-wide searches, per-frame field
reflection, permanent world cache, unload delays, forced GC or GC disabling.
Adoption/restoration happens on transitions; these can take time in huge builds.

NO SHADOWS RETAINED
Structures / Disable All Shadows=true remains the default. Optimization ON
disables Unity real-time shadows and bypasses building discovery/application;
no READY wait. OFF restores the previous owned quality value. Other writers
win. Baked/painted shading and mod-specific fake shadows may remain.
If all-shadows mode is explicitly disabled, the older distant-shadow scanner
remains available with its existing count/soft time budgets. A single engine
call can exceed those budgets; no claim that legacy scanning is spike-free.

REPORTS / TEST
See TEST_THIS_BUILD.txt: one ON capture on the usual village/castle route, then
close the game and run COLLECT_REPORTS.bat for the SAME active profile.
Send its ZIP from SupportReports/. No F9 markers or new pre-pass tests required.
No automatic upload. Collection includes config/reports/log, mod/hardware names
and position samples, not saves/binaries.
BepInEx/AethelbornPerformanceReports/: TXT +five CSVs per capture.
Config: BepInEx/config/albedo.aethelborn.performance.cfg

New summary fields:
SupportCacheHook/SupportCacheEnabled: adapter available/active at capture start.
OptimizedSupportSubscriptionsAtStart/End and SupportTerrainGroups: coverage.
RunSupportCallbackAdds/Removes: observed native subscription operations.
RunOptimizedSupportRemovals: operations using the ordered registry.
RunSupportCallbackFallbacks/RunSupportExternalEventChanges: native fallback.
RunSupportCallbackBookkeeping_ms/MaxSupportCallbackBookkeeping_ms: instrumented
subscription bookkeeping, separate from plugin Update; GC overlap may inflate
wall time. Counters reset after the one-second capture preparation.
Existing frame/timeline/effect/marker formats and memory adapter are unchanged.
Lifecycle/GC overlap is correlation, not per-mod or exact engine attribution.
Unavailable counters=-1; previous_* timing describes the previous plugin Update.

VALIDATION / LIMITS
See TEST_RESULTS_v0.3.3.txt, findings and changes_from_v0.3.2.diff.
Host allocation tests show the delegate-copy mechanism, not Valheim FPS gains.
Unity native destruction and other mod callbacks still cost time; no guaranteed
60FPS minimum or claim that all observed stalls are fixed. Actual hook coverage,
restoration, building/terrain behavior and smoothness require in-game checks.
No AA, audio, VFX, combat, loot, save, simulation-clock or physics edits.

DEVELOPER CHECKS
python tools/fetch.py
python tools/build.py /path/to/game-and-bepinex-references
python tools/test.py /path/to/game-and-bepinex-references
python tools/probe.py /path/to/game-and-bepinex-references
python tools/installer_compile_check.py /path/to/references
python tests/test_comparison.py
Reference assemblies, decompiled game sources and toolchains not redistributed.

REMOVE
Close Valheim; remove BepInEx/plugins/AethelbornPerformance/.
Optional: remove only this plugin's config/reports. No save migration.
