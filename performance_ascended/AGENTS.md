# Aethelborn Performance
Separate client plugin. Do not edit AA or any previous archive. C#5, BepInEx5, HarmonyX. No per-frame reflection or scene-wide searches. Do not change damage, simulation timing, physics, loot, building support, world/network saves. Optimizations own instance properties only and restore only values they still own; competing writers win. New third-party compatibility patches require an identified measured bottleneck. Static/runtime-adapter tests are not in-game verification.

2026-10-21: User-authorized global exception: Disable All Shadows owns only QualitySettings.shadows, restoring its original value if still owned. No other global quality settings are changed.

2026-10-21: Requested cleanup pass replaces only native terrain support-cache subscription bookkeeping. Cache clearing, callback order, object destruction scheduling and support calculations must remain equivalent. No forced/suppressed GC.
2026-10-21: v0.4.0 Performance Ascended (Claude): budgeted ZNetScene.RemoveObjects (UnloadBudget.cs) after captures attributed every 200-475 ms stall to one-tick removal of ~8,500 pieces. Rule: comparisons need matching LoadedPiecesAtStart and a scenario label.
2026-10-21: v0.4.1 Low Shadows (user: shadows back, cheapest look): HardOnly/Low res/35 m/2 cascades, owned per value; Disable All Shadows default false (revision 2 migration).
2026-10-21: v0.4.1 hotfix: INSTALL.ps1 required exactly 6 sources (broke on UnloadBudget.cs). Now checks the named list. Rule: adding/removing a source file = update INSTALL.ps1 required list and grep installers for counts.
2026-10-21: v0.4.2 Big Bases: ViewCulling.cs (camera layerCullDistances for piece/piece_nonsolid/item/Default_small, spherical, owned; lodBias cap); GC mode in reports. Installer list = 8 files.
2026-10-21: v0.4.3: CauseSummary (Unload/ZoneGeneration/PieceLoad/MemoryCleanup/Other), ZoneSystem.SpawnZone counter, opt-in GC slice (OwnedValue<ulong>). Correction: creation is already 10/tick shared near+distant - no load budget built. Rule: read the native code BEFORE proposing a fix to the user.
2026-10-21: v0.5.0: WorldTweaks.cs (Door Sleep via Door.Awake/SetState/CanInteract + keepAnimatorStateOnDisable; ClutterSystem.Awake grass patch size; owned async upload 4ms/64MB). FPS_MOD_SURVEY.txt records the 8-mod review; harmful patterns listed there must not be adopted. Installer list = 9 files.
