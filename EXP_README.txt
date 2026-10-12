AETHELBORN ASCENDED - EXP ALPHA 3 (Test)
BASELINE: v0.25.138r EXP Alpha 2 + Seismic CA

INSTALL (ONE INSTALLER)
1. Back up your active Gale profile.
2. Extract this ZIP into a fresh folder.
3. Run INSTALL.bat ONCE, select BepInEx.dll from your active Gale profile's BepInEx/core folder,
   then select UnityEngine.CoreModule.dll from the Valheim game Managed folder.
4. The installer builds ALL 10 DLLs in staging before copying any live DLL. It updates the
   EXP plug-in and blue HUD bar assets with the nine unmodified mod modules.
5. Restart Valheim. Confirm LogOutput.log says:
   'Loading [Aethelborn Ascended - Experience' and 'Experience plugin ready'.
6. Kill a 0-star Troll: default +450 EXP. At level 1, HUD should show 450/600.
   Each extra star adds +50% of base by default.

CHANGES (ONLY EXP + EXP HUD)
- EXP is awarded directly for local kills; multiplayer kills route to the credited player.
- Kill capture reads the last hit before corpse cleanup. Duplicate rewards are blocked.
- Troll default EXP is 450. Old auto-generated Troll reward of 140 migrates to 450;
  other custom creature reward CFG values are preserved.
- EXP HUD displays numbers (0/600 etc.) with blue EXP bar artwork in all four HUD styles.
- +EXP floats above the defeated creature, with the regular notification kept.
- EXP required: 600 at level 1, 1.25x through level 16, 1.05x thereafter. Level cap 80.
- Seismic Guillotine CA is untouched from Alpha 2.
- NO class, skill, combat, keybind, damage, or animation logic changed.

IMPORTANT
The old log (20 plugins loaded) did NOT show the Experience plug-in; killing cannot
award EXP until Experience is loaded. Check this first. This release has not been
Windows-compiled or playtested in Valheim here.
If Experience fails to compile or load, send the installer error and fresh LogOutput.log.
