PERFORMANCE ASCENDED v0.6.8 - official drag-and-drop build + inventory warm-up + UI tracer
Client-side Valheim performance mod (BepInEx 5). No world/save changes. Works alone; no other mod required.

INSTALL
1. Close Valheim.
2. Copy the folder  BepInEx\plugins\AethelbornPerformance  from this zip into your profile's  BepInEx\plugins  folder
   (Gale: Profile -> Open folder). Replace the existing AethelbornPerformance folder if you had the installer version.
3. Start the game. Log line: "Performance Ascended 0.6.8 ready."
Config: BepInEx\config\albedo.aethelborn.performance.cfg (your existing settings are kept).

HOTKEYS
Ctrl+Alt+F6  optimization on/off      Ctrl+Alt+B  indicator on/off      Ctrl+Alt+F  FPS counter on/off
Drag the indicator / FPS box with the mouse while the inventory is open.

WHAT IT DOES
- Leaving a base / teleporting: removes old objects 150 per tick instead of thousands in one frame (no 300-475 ms freeze).
- Draw distance for building pieces (150 m), decor (90 m), small objects (80 m); LOD bias cap 1.25.
- Low shadows: hard, low resolution, 35 m, 2 cascades (shadows kept, cheap).
- Idle doors stop animating every frame; larger grass patches; faster asset uploads.
Everything is in the config with an off switch; optimization OFF restores the game's values.

This DLL was compiled against Valheim 1.0.20 (network version 41) / Unity 6000.0.75 / BepInExPack 5.4.2351 (your current game). If a big Valheim update breaks it,
use the INSTALL.bat version (it recompiles against your installed game) or ask for a new build.
Testing tools (capture recorder, markers) are off; set [Benchmark] Record Hotkey in the config to use them again.
