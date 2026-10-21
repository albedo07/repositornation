using BepInEx.Configuration;
using UnityEngine;

namespace AethelbornPerformance
{
    // Actual production binding path can be validated without creating a Unity component.
    public sealed class PerformanceSettings
    {
        public ConfigEntry<bool> _spreadUnload, _lowShadows, _viewCulling, _gcSlice, _doorSleep, _asyncUpload;
        public ConfigEntry<float> _grassPatch, _fpsX, _fpsY;
        public ConfigEntry<bool> _fpsCounter, _compactIndicator;
        public ConfigEntry<KeyboardShortcut> _indicatorKey, _fpsKey;
        public ConfigEntry<float> _gcSliceMs;
        public ConfigEntry<float> _pieceDrawM, _nonSolidDrawM, _smallDrawM, _lodMax;
        public ConfigEntry<float> _lowShadowDistance;
        public ConfigEntry<int> _lowShadowCascades;
        public ConfigEntry<int> _unloadPerTick;
        public ConfigEntry<bool> _enabled, _shadows, _noShadows, _supportCache, _particles, _diagnostics, _renderCounters;
        public ConfigEntry<string> _scenario;
        public ConfigEntry<bool> _showIndicator, _requireReady;
        public ConfigEntry<float> _indicatorX, _indicatorY, _discoveryMs, _shadowMs;
        public ConfigEntry<int> _configRevision;
        public ConfigEntry<float> _spikeThreshold;
        public ConfigEntry<KeyboardShortcut> _markerKey;
        public ConfigEntry<float> _shadowDistance, _hysteresis, _effectDistance, _seconds;
        public ConfigEntry<int> _piecesPerFrame, _renderersPerFrame, _particleMax, _particleBudget;
        public ConfigEntry<string> _allowList;
        public ConfigEntry<KeyboardShortcut> _recordKey, _toggleKey;
        public PerformanceSettings(ConfigFile config)
        {
                _supportCache = config.Bind("Structures", "Efficient Support Cache Subscriptions", true, "Use ordered constant-time callback removal for native terrain support-cache notifications. Preserves clearing, callback order and native object lifetime. Turning OFF restores native subscriptions.");
                _spreadUnload = config.Bind("Structures", "Spread Object Unloading", true, "When you leave an area Valheim removes every out-of-range object (thousands of building pieces) in ONE frame - the 300-475 ms freezes in the captures. ON removes at most 'Unload Per Tick' objects per unloading tick (Valheim ticks 30 times a second) until done. Same removal steps as Valheim; objects you return to are kept. OFF = Valheim's own unloading.");
                _unloadPerTick = config.Bind("Structures", "Unload Per Tick", 150, new ConfigDescription("Maximum objects removed per unloading tick (30 ticks per second). 150 = about 4,500 per second. Lower = smoother but slower cleanup.", new AcceptableValueRange<int>(25, 2000)));
                _indicatorKey = config.Bind("HUD", "Indicator Hotkey", new KeyboardShortcut(KeyCode.B, KeyCode.LeftControl, KeyCode.LeftAlt), "Show/hide the optimization indicator. Default Left Ctrl + Left Alt + B.");
                _fpsKey = config.Bind("HUD", "FPS Counter Hotkey", new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl, KeyCode.LeftAlt), "Show/hide the FPS counter. Default Left Ctrl + Left Alt + F.");
                _fpsCounter = config.Bind("HUD", "FPS Counter", true, "Small FPS box (current FPS and 1% low over the last ~5 s). Drag it with the mouse while the inventory/menu cursor is visible.");
                _fpsX = config.Bind("HUD", "FPS Counter X", -1f, "Pixels from the left (-1 = top right corner). Saved when you drag it.");
                _fpsY = config.Bind("HUD", "FPS Counter Y", 8f, "Pixels from the top. Saved when you drag it.");
                _compactIndicator = config.Bind("HUD", "Compact Indicator", true, "Show the optimization indicator as one short line instead of the 4-line panel.");
                _doorSleep = config.Bind("World", "Door Sleep", true, "Idle doors stop running their animator every frame (big bases have hundreds). A door wakes the moment it is used, hovered or changed by another player, keeping its open/closed pose.");
                _grassPatch = config.Bind("World", "Grass Patch Size", 20f, new ConfigDescription("Grass is built and drawn in square patches (game: 10 m). Bigger patches = far fewer grass draw calls and rebuilds; same density. Applies when a world loads. 0 = game default.", new AcceptableValueRange<float>(0f, 40f)));
                _asyncUpload = config.Bind("World", "Faster Asset Uploads", true, "Gives Unity more time and memory per frame to upload newly loaded meshes and textures to the GPU (4 ms / 64 MB instead of 2 ms / Unity default), so loading areas causes fewer hitches. Restored when off.");
                _gcSlice = config.Bind("Memory", "Longer Cleanup Slices", false, "EXPERIMENT (off by default). Unity cleans memory in small per-frame slices (3 ms). When the game and mods make garbage faster than that, Unity stops and finishes in one go (the 20-46 ms frames every 7-10 s). ON gives each slice more time so cleanups can finish in steps. Compare captures ON/OFF; restored when turned off.");
                _gcSliceMs = config.Bind("Memory", "Cleanup Slice Milliseconds", 5f, new ConfigDescription("Per-frame cleanup time when the experiment is ON. Unity default: 3.", new AcceptableValueRange<float>(1f, 10f)));
                _viewCulling = config.Bind("Draw Distance", "Enabled", true, "Big bases: stop DRAWING building pieces and small objects beyond the distances below (Unity per-layer culling, no per-object work). They still exist, collide and work; only the picture is skipped. Each building piece is its own draw call, so this is the main FPS lever around large builds. OFF restores the camera.");
                _pieceDrawM = config.Bind("Draw Distance", "Building Pieces Metres", 150f, new ConfigDescription("Walls, floors, roofs (layer piece). 0 = game default.", new AcceptableValueRange<float>(0f, 1000f)));
                _nonSolidDrawM = config.Bind("Draw Distance", "Decor Pieces Metres", 90f, new ConfigDescription("Non-solid pieces: banners, rugs, decorations, torches' meshes (layer piece_nonsolid). 0 = game default.", new AcceptableValueRange<float>(0f, 1000f)));
                _smallDrawM = config.Bind("Draw Distance", "Small Objects Metres", 80f, new ConfigDescription("Dropped items and small props (layers item, Default_small). 0 = game default.", new AcceptableValueRange<float>(0f, 1000f)));
                _lodMax = config.Bind("Draw Distance", "LOD Bias Max", 1.25f, new ConfigDescription("Cap on Unity's LOD bias (trees/rocks switch to simpler models sooner when lower). Never raises the game's value.", new AcceptableValueRange<float>(0.3f, 5f)));
                _lowShadows = config.Bind("Structures", "Low Shadows", true, "Keep real-time shadows at the cheapest look that still reads well: hard shadows (no soft filtering), low shadow-map resolution, short distance, few cascades. Used when 'Disable All Shadows' is false. Turning OFF (or optimization OFF) restores the game's values unless another writer changed them.");
                _lowShadowDistance = config.Bind("Structures", "Low Shadow Distance", 35f, new ConfigDescription("Metres of shadows around the camera in Low Shadows mode (game value in your captures: 80). Shorter = faster.", new AcceptableValueRange<float>(10f, 150f)));
                _lowShadowCascades = config.Bind("Structures", "Low Shadow Cascades", 2, new ConfigDescription("Shadow cascades in Low Shadows mode: 1 = fastest (blockier up close), 2 = sharper near the player for a small cost, 4 = game-like.", new AcceptableValueList<int>(1, 2, 4)));
                _noShadows = config.Bind("Structures", "Disable All Shadows", false, "Disable all Unity real-time shadows while optimization is ON. Skips building discovery and shadow application. Turning OFF restores the previous quality setting unless another writer changed it. Set false to use the previous distant-building shadow mode.");
                _requireReady = config.Bind("Benchmark", "Require Ready For ON Capture", true, "Wait for building discovery and first shadow application before an ON capture. Set false only to investigate startup/loading deliberately.");
                _discoveryMs = config.Bind("Structures", "Discovery Time Budget Milliseconds", 0.5f, new ConfigDescription("Soft per-frame discovery time budget. A single hierarchy cannot be interrupted. Slow frames halve this budget.", new AcceptableValueRange<float>(0.1f, 2f)));
                _shadowMs = config.Bind("Structures", "Shadow Time Budget Milliseconds", 0.5f, new ConfigDescription("Soft per-frame shadow update time limit, in addition to Renderer Budget. Slow frames halve this budget. One engine operation can exceed it.", new AcceptableValueRange<float>(0.1f, 2f)));
                _configRevision = config.Bind("General", "Config Revision", 0, "Internal one-time migration marker for old discovery and screenshot-conflicting defaults.");
                _showIndicator = config.Bind("HUD", "Show Optimization Indicator", true, "Show a persistent ON/OFF status while in a world.");
                _indicatorX = config.Bind("HUD", "Indicator X", 12f, new ConfigDescription("Indicator horizontal position in screen pixels from the left. Drag the indicator with the mouse while the inventory/menu cursor is visible; the position is saved.", new AcceptableValueRange<float>(0f, 8192f)));
                _indicatorY = config.Bind("HUD", "Indicator Y", 12f, new ConfigDescription("Indicator vertical position in screen pixels from the top.", new AcceptableValueRange<float>(0f, 8192f)));
                _diagnostics = config.Bind("Benchmark", "Detailed Diagnostics", true, "Capture memory, process CPU and feature coverage once per second while recording. Adds diagnostic overhead to both ON and OFF captures.");
                _renderCounters = config.Bind("Benchmark", "Rendering Counters", true, "Request optional Unity rendering, main/render thread and allocation counters during recordings. Unavailable counters are reported as -1. Does not enable deep profiling.");
                _scenario = config.Bind("Benchmark", "Scenario", "unlabeled", "Optional short label for the scene being tested, for example base_walk or first_skill_cast. Use the same label for comparable runs.");
                _spikeThreshold = config.Bind("Benchmark", "Spike Threshold Milliseconds", 50f, new ConfigDescription("Frames at or above this duration are also exported to the spikes CSV.", new AcceptableValueRange<float>(16f, 1000f)));
                _markerKey = config.Bind("Benchmark", "Marker Hotkey", KeyboardShortcut.Empty, "Testing only (empty = off in the official build; e.g. LeftCtrl + LeftAlt + F9). During a recording, mark an action or slowdown. Default Left Ctrl + Left Alt + F9; markers are numbered in order.");
                _enabled = config.Bind("General", "Enabled", true, "Enable optimization features. Turning off restores owned instance properties. Benchmarks remain available.");
                _shadows = config.Bind("Structures", "Distant Shadows", true, "Remove shadows cast by distant building MeshRenderers. Buildings remain visible and interactive.");
                _shadowDistance = config.Bind("Structures", "Shadow Distance", 40f, new ConfigDescription("Distance in metres beyond which building pieces stop casting shadows.", new AcceptableValueRange<float>(10f, 250f)));
                _hysteresis = config.Bind("Structures", "Shadow Hysteresis", 5f, new ConfigDescription("Move this much closer before restoring shadows, preventing boundary flicker.", new AcceptableValueRange<float>(0f, 20f)));
                _piecesPerFrame = config.Bind("Structures", "Discovery Budget", 256, new ConfigDescription("Maximum registry entries examined per frame, also constrained by the discovery time budget.", new AcceptableValueRange<int>(1, 512)));
                _renderersPerFrame = config.Bind("Structures", "Renderer Budget", 128, new ConfigDescription("Maximum building renderers checked per frame. Large builds take longer to refresh.", new AcceptableValueRange<int>(16, 1024)));
                _particles = config.Bind("Cosmetics", "Death Particle Budget", true, "Limit particle counts on exact allowlisted cosmetic effects. Unknown effects are unchanged.");
                _particleMax = config.Bind("Cosmetics", "Particles Per System", 96, new ConfigDescription("Maximum particles per tracked cosmetic system; shared budget can lower this further.", new AcceptableValueRange<int>(8, 512)));
                _particleBudget = config.Bind("Cosmetics", "Shared Particle Budget", 1024, new ConfigDescription("Total maximum-particle capacity shared among up to 256 tracked cosmetic systems. A minimum of one slot is retained per system.", new AcceptableValueRange<int>(256, 8192)));
                _effectDistance = config.Bind("Cosmetics", "Far Distance", 30f, new ConfigDescription("Beyond this distance the allocated cosmetic particle cap is halved.", new AcceptableValueRange<float>(5f, 150f)));
                _allowList = config.Bind("Cosmetics", "Exact Effect Names", "vfx_greydwarf_death,vfx_greydwarf_shaman_death,vfx_greydwarf_elite_death,vfx_skeleton_death,vfx_troll_death,vfx_draugr_death", "Comma-separated exact root prefab names, ignoring (Clone). Only pure cosmetic particle systems are eligible. Add reviewed modded cosmetic effects here; skill telegraphs should remain excluded.");
                _seconds = config.Bind("Benchmark", "Duration Seconds", 60f, new ConfigDescription("Automatic benchmark duration. A second hotkey press ends it early.", new AcceptableValueRange<float>(10f, 180f)));
                _recordKey = config.Bind("Benchmark", "Record Hotkey", KeyboardShortcut.Empty, "Testing only (empty = off in the official build; set e.g. LeftCtrl + LeftAlt + F10 to record captures again). Start/stop a benchmark. Default: Left Ctrl + Left Alt + F10.");
                _toggleKey = config.Bind("General", "Toggle Hotkey", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl, KeyCode.LeftAlt), "Toggle optimization features. Default: Left Ctrl + Left Alt + F6. Wait for settings to settle before recording.");
                if (_configRevision.Value < 1)
                {
                    if (_piecesPerFrame.Value == 2) _piecesPerFrame.Value = 256;
                    if (_toggleKey.Value.Equals(new KeyboardShortcut(KeyCode.F11, KeyCode.LeftControl, KeyCode.LeftAlt)))
                        _toggleKey.Value = new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl, KeyCode.LeftAlt);
                    _configRevision.Value = 1;
                }
                if (_configRevision.Value < 2)
                {
                    // 0.4.1 (user): shadows back, at the low setting; old installs had Disable All Shadows=true saved.
                    _noShadows.Value = false;
                    _configRevision.Value = 2;
                }
                if (_configRevision.Value < 4)
                {
                    // 0.6.6 official build (user): the capture recorder/marker are developer tools - their hotkeys go off once
                    if (_recordKey.Value.Equals(new KeyboardShortcut(KeyCode.F10, KeyCode.LeftControl, KeyCode.LeftAlt))) _recordKey.Value = KeyboardShortcut.Empty;
                    if (_markerKey.Value.Equals(new KeyboardShortcut(KeyCode.F9, KeyCode.LeftControl, KeyCode.LeftAlt))) _markerKey.Value = KeyboardShortcut.Empty;
                }
                if (_configRevision.Value < 3)
                {
                    // 0.6.0 (user): the indicator sat on the hotbar at the top left - move it below the hotbar once
                    if (_indicatorX.Value == 12f && _indicatorY.Value == 12f) _indicatorY.Value = 140f;
                    _configRevision.Value = 3;
                }
                if (_configRevision.Value < 4) _configRevision.Value = 4;
        }
    }
}
