using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AethelbornPerformance
{
    // 0.6.7 UI TRACER (user: the game stutters when opening chests, the inventory or other UI). Diagnostic only - changes
    // nothing. Every time a window opens (inventory, chest, map, trader) it measures every frame for 1 s: the frame time and
    // the time spent inside the game's UI methods (including every other mod's patches on them), plus garbage collections.
    // One log line per open: first open vs repeat opens tell "one-time build" from "every time" costs. At startup it lists
    // which other mods patch those UI methods.
    public static class UiTracer
    {
        private static readonly string[][] Targets =
        {
            new[] { "InventoryGui", "Show" }, new[] { "InventoryGui", "Hide" }, new[] { "InventoryGui", "UpdateInventory" },
            new[] { "InventoryGui", "UpdateContainer" }, new[] { "InventoryGui", "SetupCrafting" }, new[] { "InventoryGui", "UpdateCraftingPanel" },
            new[] { "InventoryGui", "UpdateRecipeList" }, new[] { "InventoryGrid", "UpdateInventory" }, new[] { "InventoryGrid", "UpdateGui" },
            new[] { "Minimap", "SetMapMode" }, new[] { "Minimap", "UpdateMap" }, new[] { "StoreGui", "Show" }
        };
        private const int Window = 120;
        private static readonly Dictionary<MethodBase, int> Index = new Dictionary<MethodBase, int>();
        private static readonly List<string> Names = new List<string>();
        private static long[,] _ticks;
        private static readonly double[] FrameMs = new double[Window];
        private static readonly bool[] Gc = new bool[Window];
        private static readonly long[] TopTicks = new long[Window];
        private static readonly Dictionary<string, int> Opens = new Dictionary<string, int>();
        private static int _depth;
        private static string _label;
        private static int _startFrame = -1, _lastGc;
        private static float _startTime;
        private static long _lastTick;
        private static ManualLogSource _log;
        private static readonly double MsPerTick = 1000.0 / System.Diagnostics.Stopwatch.Frequency;

        public static string Install(Harmony harmony, ManualLogSource log)
        {
            _log = log;
            int ok = 0;
            HarmonyMethod pre = new HarmonyMethod(typeof(UiTracer), "Pre"); pre.priority = Priority.First;
            HarmonyMethod post = new HarmonyMethod(typeof(UiTracer), "Post"); post.priority = Priority.Last;
            List<string> foreign = new List<string>();
            for (int i = 0; i < Targets.Length; i++)
            {
                try
                {
                    Type t = AccessTools.TypeByName(Targets[i][0]);
                    if (t == null) continue;
                    foreach (MethodInfo m in AccessTools.GetDeclaredMethods(t))
                    {
                        if (m.Name != Targets[i][1] || m.IsAbstract || m.ContainsGenericParameters) continue;
                        Patches info = Harmony.GetPatchInfo(m);
                        if (info != null)
                        {
                            HashSet<string> owners = new HashSet<string>();
                            foreach (Patch p in info.Prefixes) owners.Add(p.owner);
                            foreach (Patch p in info.Postfixes) owners.Add(p.owner);
                            foreach (Patch p in info.Transpilers) owners.Add(p.owner);
                            foreach (Patch p in info.Finalizers) owners.Add(p.owner);
                            owners.Remove(harmony.Id);
                            if (owners.Count > 0) foreign.Add(t.Name + "." + m.Name + ": " + string.Join(", ", new List<string>(owners).ToArray()));
                        }
                        if (!Index.ContainsKey(m)) { Index[m] = Names.Count; Names.Add(t.Name + "." + m.Name); }
                        harmony.Patch(m, prefix: pre, postfix: post);
                        ok++;
                    }
                }
                catch (Exception e) { log.LogWarning("UI tracer: could not watch " + Targets[i][0] + "." + Targets[i][1] + ": " + e.Message); }
            }
            _ticks = new long[Window, Math.Max(1, Names.Count)];
            if (foreign.Count > 0) log.LogInfo("[PA UI] other mods patching UI methods (checked at load; mods loading later are not listed): " + string.Join(" | ", foreign.ToArray()));
            return "UI tracer watching " + ok + " UI methods (diagnostic only)";
        }

        private static void Pre(MethodBase __originalMethod, object[] __args, out long __state)
        {
            __state = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
                string n = __originalMethod.DeclaringType.Name + "." + __originalMethod.Name;
                if (n == "InventoryGui.Show")
                {
                    object c = __args != null && __args.Length > 0 ? __args[0] : null;
                    Container ct = c as Container;
                    Start(_warming ? "Inventory warm-up (invisible)" : ct != null ? "Chest/container " + (ct.m_name ?? ct.name) : "Inventory");
                }
                else if (n == "Minimap.SetMapMode" && __args != null && __args.Length > 0 && Convert.ToInt32(__args[0]) == 2) Start("Map");
                else if (n == "StoreGui.Show") Start("Trader");
            }
            catch (Exception) { }
            _depth++;
        }

        private static void Post(MethodBase __originalMethod, long __state)
        {
            if (_depth > 0) _depth--;
            if (_label == null) return;
            long dt = System.Diagnostics.Stopwatch.GetTimestamp() - __state;
            int k = Time.frameCount - _startFrame;
            int idx;
            if (k < 0 || k >= Window || !Index.TryGetValue(__originalMethod, out idx)) return;
            _ticks[k, idx] += dt;
            if (_depth == 0) TopTicks[k] += dt;
        }

        private static void Start(string label)
        {
            if (_label != null) Finish();
            int n; Opens.TryGetValue(label, out n); Opens[label] = n + 1;
            _label = label + " (open #" + (n + 1) + ")";
            _startFrame = Time.frameCount; _startTime = Time.unscaledTime;
            Array.Clear(_ticks, 0, _ticks.Length); Array.Clear(FrameMs, 0, Window); Array.Clear(Gc, 0, Window); Array.Clear(TopTicks, 0, Window);
            _lastGc = GC.CollectionCount(0);
        }

        // 0.6.8 INVENTORY WARM-UP (user: chests/inventory stutter; the 0.6.7 trace showed open #1 = 257 ms, later opens ~50 ms).
        // Once per session, ~8 s after spawning with no window open, the inventory is shown and hidden on consecutive frames with
        // its canvas fully transparent and its open/close sound effects swapped out, so the one-time build of the inventory (and
        // every mod's first-open setup on it) runs then. Valheim's own Show/Hide are used; nothing else is changed.
        public static Func<bool> WarmupEnabled;
        private static int _warmState;
        private static float _seenPlayerAt = -1f, _restoreAt;
        private static CanvasGroup _warmGroup;
        private static float _warmAlpha; private static bool _warmRaycasts;
        private static System.Reflection.FieldInfo _openFx, _closeFx;
        private static object _openSaved, _closeSaved;
        private static long _warmT0;
        private static bool _warming;

        private static void Warmup()
        {
            try
            {
                if (_warmState >= 3) return;
                Player p = Player.m_localPlayer;
                InventoryGui gui = InventoryGui.instance;
                if (p == null || gui == null) { _seenPlayerAt = -1f; return; }
                if (_seenPlayerAt < 0f) _seenPlayerAt = Time.time;
                if (_warmState == 0)
                {
                    if (WarmupEnabled != null && !WarmupEnabled()) { _warmState = 3; return; }
                    if (Time.time - _seenPlayerAt < 8f || InventoryGui.IsVisible() || Menu.IsVisible() || Minimap.IsOpen() || TextInput.IsVisible() || p.IsDead()) return;
                    if (gui.m_inventoryRoot == null) { _warmState = 3; return; }
                    _warmGroup = gui.m_inventoryRoot.GetComponent<CanvasGroup>();
                    if (_warmGroup == null) _warmGroup = gui.m_inventoryRoot.gameObject.AddComponent<CanvasGroup>();
                    _warmAlpha = _warmGroup.alpha; _warmRaycasts = _warmGroup.blocksRaycasts;
                    _warmGroup.alpha = 0f; _warmGroup.blocksRaycasts = false;
                    if (_openFx == null) _openFx = AccessTools.Field(typeof(InventoryGui), "m_openInventoryEffects");
                    if (_closeFx == null) _closeFx = AccessTools.Field(typeof(InventoryGui), "m_closeInventoryEffects");
                    if (_openFx != null) { _openSaved = _openFx.GetValue(gui); _openFx.SetValue(gui, new EffectList()); }
                    if (_closeFx != null) { _closeSaved = _closeFx.GetValue(gui); _closeFx.SetValue(gui, new EffectList()); }
                    _warmT0 = System.Diagnostics.Stopwatch.GetTimestamp();
                    _warming = true;
                    try { gui.Show(null, 1); } finally { _warming = false; }
                    _warmState = 1;
                    return;
                }
                if (_warmState == 1)
                {
                    try { gui.Hide(); }
                    finally
                    {
                        if (_openFx != null && _openSaved != null) _openFx.SetValue(gui, _openSaved);
                        if (_closeFx != null && _closeSaved != null) _closeFx.SetValue(gui, _closeSaved);
                    }
                    _restoreAt = Time.time + 0.6f;   // the close animation runs while still transparent
                    _warmState = 2;
                    return;
                }
                if (_warmState == 2 && (Time.time >= _restoreAt || InventoryGui.IsVisible()))
                {
                    if (_warmGroup != null) { _warmGroup.alpha = _warmAlpha; _warmGroup.blocksRaycasts = _warmRaycasts; }
                    _warmState = 3;
                    if (_log != null) _log.LogInfo("[PA UI] inventory warm-up done (" + ((System.Diagnostics.Stopwatch.GetTimestamp() - _warmT0) * MsPerTick).ToString("F0") + " ms incl. waiting); the first real open should no longer stutter.");
                }
            }
            catch (Exception e)
            {
                _warmState = 3;
                try { if (_warmGroup != null) { _warmGroup.alpha = _warmAlpha; _warmGroup.blocksRaycasts = _warmRaycasts; } } catch (Exception) { }
                if (_log != null) _log.LogWarning("[PA UI] inventory warm-up skipped: " + e.Message);
            }
        }

        // once per frame (plugin Update): closes the previous frame
        public static void Tick()
        {
            Warmup();
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            double prev = _lastTick > 0 ? (now - _lastTick) * MsPerTick : 0;
            _lastTick = now;
            if (_label == null) return;
            int k = Time.frameCount - 1 - _startFrame;
            int gc = GC.CollectionCount(0);
            if (k >= 0 && k < Window) { FrameMs[k] = prev; if (gc != _lastGc) Gc[k] = true; }
            _lastGc = gc;
            if (k >= Window - 1 || Time.unscaledTime - _startTime > 1f) Finish();
        }

        private static void Finish()
        {
            string label = _label; _label = null;
            if (label == null || _log == null) return;
            int n = Math.Min(Window, Math.Max(1, Time.frameCount - 1 - _startFrame));
            int worst = 0;
            for (int k = 0; k < n; k++) if (FrameMs[k] > FrameMs[worst]) worst = k;
            double sum = 0; int cnt = 0;
            for (int k = Math.Max(0, n - 20); k < n; k++) if (k != worst && FrameMs[k] > 0) { sum += FrameMs[k]; cnt++; }
            double normal = cnt > 0 ? sum / cnt : 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("[PA UI] ").Append(label).Append(": worst frame ").Append(FrameMs[worst].ToString("F0")).Append(" ms (frame +").Append(worst)
              .Append(", normal ~").Append(normal.ToString("F0")).Append(" ms). In that frame - UI code (game + all mods' patches) ")
              .Append((TopTicks[worst] * MsPerTick).ToString("F1")).Append(" ms: ");
            bool any = false;
            for (int i = 0; i < Names.Count; i++)
            {
                double ms = _ticks[worst, i] * MsPerTick;
                if (ms < 0.5) continue;
                sb.Append(Names[i]).Append(" ").Append(ms.ToString("F1")).Append(" ms, ");
                any = true;
            }
            if (!any) sb.Append("(none over 0.5 ms), ");
            sb.Append("rest of frame (Unity UI layout/rendering/shaders/other) ").Append(Math.Max(0, FrameMs[worst] - TopTicks[worst] * MsPerTick).ToString("F0")).Append(" ms")
              .Append(Gc[worst] ? ", GARBAGE COLLECTION ran" : "");
            int slow = 0; for (int k = 0; k < n; k++) if (FrameMs[k] > Math.Max(25.0, normal * 2.0)) slow++;
            sb.Append("; slow frames in 1 s: ").Append(slow);
            _log.LogInfo(sb.ToString());
        }
    }
}
