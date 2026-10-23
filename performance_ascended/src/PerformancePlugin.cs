using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace AethelbornPerformance
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class PerformancePlugin : BaseUnityPlugin
    {
        public const string Guid = "albedo.aethelborn.performance";
        public const string Name = "Performance Ascended";
        public const string Version = "0.6.7";
        public static PerformancePlugin Instance;
        private PerformanceSettings _settings;
        private GUIStyle _indicatorStyle;
        private readonly GUIContent _indicatorContent = new GUIContent();
        private bool _indicatorUnavailable;
        private readonly List<PieceState> _pieces = new List<PieceState>();
        private readonly HashSet<int> _knownPieces = new HashSet<int>();
        private readonly DiscoverySweep<Piece> _sweep = new DiscoverySweep<Piece>();
        private readonly DiscoveryQueue<Piece> _newPieces = new DiscoveryQueue<Piece>(MaximumPieces);
        private bool _reconcileRequested;
        private static int _zoneSpawns; private int _lastZones;
        private int _hookLoadedCount = -1, _createdPieces, _destroyedPieces, _lastCreated, _lastDestroyed, _snapshots, _startSnapshots;
        private FrameContext _activity;
        private int DiscoveryChecked { get { return _sweep.Examined + _newPieces.Examined; } }
        private int DiscoveryTotal { get { return _sweep.Total + _newPieces.Total; } }
        private Action<Piece> _registerPiece;
        private static readonly Func<long> DiscoveryClock = System.Diagnostics.Stopwatch.GetTimestamp;
        private int _registryRevision, _observedRevision, _observedLoaded = -1, _pendingFirstApply, _loadedPieces;
        private float _registryStableSince, _nextFallbackSweep, _nextHud;
        private bool _pieceHooks, _discoveryReady, _registrationLimited;
        private readonly GUIContent _discoveryContent = new GUIContent();
        private readonly GUIContent _discoveryHint = new GUIContent();
        private readonly GUIContent _shortcutContent = new GUIContent();
        private readonly List<ParticleState> _systems = new List<ParticleState>();
        private readonly HashSet<int> _knownSystems = new HashSet<int>();
        private readonly Queue<GameObject> _pending = new Queue<GameObject>();
        private HashSet<string> _allowed;
        private Func<IList> _pieceSource;
        private Harmony _harmony;
        private FrameRecorder _recorder = new FrameRecorder(65536);
        private int _pieceCursor, _rendererCursor, _pendingDropped, _unclassified, _gcStart;
        private int _registeredSystems, _conflicts, _trackingOverflow, _expiredQueued;
        private int _startRegistered, _startConflicts, _startDropped, _startOverflow, _startExpired, _previousGc, _effectNameOverflow;
        private bool _captureStarted;
        private float _nextDiagnostic, _previousWorkMs;
        private readonly RuntimeDiagnostics _runtime = new RuntimeDiagnostics();
        private readonly DiagnosticLog _diagnostics = new DiagnosticLog(512, 128);
        private readonly Dictionary<string, EffectObservation> _observations = new Dictionary<string, EffectObservation>(StringComparer.Ordinal);
        private sealed class EffectObservation { public int Count; public bool Allowlisted; public double FirstSeconds; }
        private SupportCacheStore _supportCache;
        private bool _supportHook;
        private OwnedValue<ShadowQuality> _globalShadows;
        private bool BuildingShadows { get { return PerformancePolicy.BuildingShadows(_settings._noShadows.Value || _settings._lowShadows.Value, _settings._shadows.Value); } }
        private bool _wasShadows, _wasParticles, _failed, _finishing;
        private float _nextParticles, _recordStart;
        private string _recordContext;
        private bool _effectsHook, _recordEnabled;
        private const int MaximumPieces = 50000, MaximumSystems = 256, MaximumPending = 256;
        private sealed class PieceState
        {
            public Piece Piece; public int Id; public ShadowState[] Renderers; public bool Far, InitialApplied;
        }
        private sealed class ShadowState
        {
            public MeshRenderer Renderer;
            public OwnedValue<ShadowCastingMode> Mode;
        }
        private sealed class ParticleState
        {
            public ParticleSystem System; public int Id;
            public OwnedValue<int> Maximum;
            public OwnedValue<float> RateTime, RateDistance;
        }
        private void Awake()
        {
            Instance = this;
            try
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { _failed = true; Logger.LogInfo("Client graphics plugin: skipping headless server."); return; }
                _settings = new PerformanceSettings(Config);
                _allowed = PerformancePolicy.Names(_settings._allowList.Value);
                _settings._allowList.SettingChanged += delegate { _allowed = PerformancePolicy.Names(_settings._allowList.Value); RestoreParticles(); _pending.Clear(); };
                Config.SettingChanged += ConfigChanged;
                ResolvePieceSource();
                _registerPiece = RegisterPiece;
                UpdateShortcutText();
                _harmony = new Harmony(Guid);
                List<MethodInfo> methods = AccessTools.GetDeclaredMethods(typeof(EffectList));
                for (int i = 0; i < methods.Count; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "Create" || method.ReturnType != typeof(GameObject[])) continue;
                    _harmony.Patch(method, postfix: new HarmonyMethod(typeof(PerformancePlugin), "EffectsCreated")); _effectsHook = true;
                }
                MethodInfo awake = AccessTools.DeclaredMethod(typeof(Piece), "Awake");
                MethodInfo destroy = AccessTools.DeclaredMethod(typeof(Piece), "OnDestroy");
                if (awake != null && destroy != null)
                {
                    _harmony.Patch(awake, postfix: new HarmonyMethod(typeof(PerformancePlugin), "PieceCreated"));
                    _harmony.Patch(destroy, postfix: new HarmonyMethod(typeof(PerformancePlugin), "PieceDestroyed"));
                    _pieceHooks = true;
                }
                InitializeSupportCache();
                // 0.4.3: count world-zone generation (terrain, vegetation, locations) to classify heavy load frames
                List<MethodInfo> zoneMethods = AccessTools.GetDeclaredMethods(typeof(ZoneSystem));
                for (int i = 0; i < zoneMethods.Count; i++)
                    if (zoneMethods[i].Name == "SpawnZone") _harmony.Patch(zoneMethods[i], postfix: new HarmonyMethod(typeof(PerformancePlugin), "ZoneSpawned"));
                Logger.LogInfo("Spread Object Unloading: " + UnloadBudget.Install(_harmony, Logger));
                Logger.LogInfo("World tweaks: " + WorldTweaks.Install(_harmony, Logger));
                Logger.LogInfo(UiTracer.Install(_harmony, Logger));   // 0.6.7 diagnostic
                Logger.LogInfo(Name + " " + Version + " ready. Piece registry: " + (_pieceSource != null) + "; cosmetic hook: " + _effectsHook + ". No AA dependency or world/save changes.");
            }
            catch (Exception ex)
            {
                _failed = true; try { RestoreAll(); } catch (Exception) { }
                if (_harmony != null) _harmony.UnpatchSelf();
                Logger.LogError("Performance plugin disabled after initialization failure: " + ex);
            }
        }
        // Cached private-field getter. No FieldInfo.GetValue calls in Update.
        private void ResolvePieceSource()
        {
            try
            {
                FieldInfo field = typeof(Piece).GetField("s_allPieces", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null || !typeof(IList).IsAssignableFrom(field.FieldType))
                { Logger.LogWarning("Building registry unavailable in this Valheim build; structure optimization disabled."); return; }
                DynamicMethod getter = new DynamicMethod("AP_GetPieces", typeof(IList), Type.EmptyTypes, typeof(Piece), true);
                ILGenerator il = getter.GetILGenerator(); il.Emit(OpCodes.Ldsfld, field); il.Emit(OpCodes.Castclass, typeof(IList)); il.Emit(OpCodes.Ret);
                _pieceSource = (Func<IList>)getter.CreateDelegate(typeof(Func<IList>));
            }
            catch (Exception ex) { Logger.LogWarning("Building registry adapter unavailable: " + ex.GetType().Name); }
        }
        private void InitializeSupportCache()
        {
            MethodInfo start = AccessTools.DeclaredMethod(typeof(WearNTear), "Start");
            MethodInfo destroy = AccessTools.DeclaredMethod(typeof(WearNTear), "OnDestroy");
            MethodInfo heightDestroy = AccessTools.DeclaredMethod(typeof(Heightmap), "OnDestroy");
            try
            {
                if (start == null || destroy == null || heightDestroy == null) throw new MissingMethodException("Native support-cache lifecycle methods unavailable");
                MethodInfo add = AccessTools.DeclaredMethod(typeof(Heightmap), "add_m_clearConnectedWearNTearCache");
                MethodInfo remove = AccessTools.DeclaredMethod(typeof(Heightmap), "remove_m_clearConnectedWearNTearCache");
                if (add == null || remove == null) throw new MissingMethodException("Native support-cache event accessors unavailable");
                Patches addPatches = Harmony.GetPatchInfo(add), removePatches = Harmony.GetPatchInfo(remove);
                if ((addPatches != null && addPatches.Owners.Count > 0) || (removePatches != null && removePatches.Owners.Count > 0))
                    throw new InvalidOperationException("Another mod patches terrain event accessors; native event handling retained");
                _supportCache = new SupportCacheStore();
                _harmony.Patch(start, transpiler: new HarmonyMethod(typeof(PerformancePlugin), "SupportCacheTranspiler"));
                _harmony.Patch(destroy, transpiler: new HarmonyMethod(typeof(PerformancePlugin), "SupportCacheTranspiler"));
                _harmony.Patch(heightDestroy, postfix: new HarmonyMethod(typeof(PerformancePlugin), "HeightmapDestroyed"));
                _supportHook = true;
                _supportCache.SetEnabled(_settings._enabled.Value && _settings._supportCache.Value);
                Logger.LogInfo("Native support-cache subscription adapter ready; object removal and support calculations unchanged.");
            }
            catch (Exception ex)
            {
                _supportHook = false;
                if (_supportCache != null) _supportCache.SetEnabled(false);
                if (start != null) _harmony.Unpatch(start, HarmonyPatchType.Transpiler, Guid);
                if (destroy != null) _harmony.Unpatch(destroy, HarmonyPatchType.Transpiler, Guid);
                if (heightDestroy != null) _harmony.Unpatch(heightDestroy, HarmonyPatchType.Postfix, Guid);
                Logger.LogWarning("Support-cache adapter unavailable; native subscriptions retained: " + ex.GetType().Name);
            }
        }
        // Change only the native event accessor call. All other native instructions remain intact.
        private static IEnumerable<CodeInstruction> SupportCacheTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            bool add = __originalMethod.Name == "Start";
            MethodInfo original = AccessTools.DeclaredMethod(typeof(Heightmap), (add ? "add_" : "remove_") + "m_clearConnectedWearNTearCache");
            MethodInfo replacement = AccessTools.DeclaredMethod(typeof(PerformancePlugin), add ? "SubscribeSupportCache" : "UnsubscribeSupportCache");
            List<CodeInstruction> result = new List<CodeInstruction>(); int changed = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && object.Equals(instruction.operand, original))
                {
                    CodeInstruction copy = new CodeInstruction(instruction); copy.opcode = OpCodes.Call; copy.operand = replacement;
                    result.Add(copy); changed++;
                }
                else result.Add(instruction);
            }
            if (original == null || replacement == null || changed != 1) throw new InvalidOperationException("Unsupported native support-cache event call layout");
            return result;
        }
        private static void SubscribeSupportCache(Heightmap map, Action callback)
        { ChangeSupportCache(map, callback, true); }
        private static void UnsubscribeSupportCache(Heightmap map, Action callback)
        { ChangeSupportCache(map, callback, false); }
        private static void ChangeSupportCache(Heightmap map, Action callback, bool add)
        {
            PerformancePlugin p = Instance;
            SupportCacheStore store = p == null ? null : p._supportCache;
            bool trace = p != null && p._recorder.Recording && p._captureStarted && p._settings._diagnostics.Value;
            long start = trace ? DiscoveryClock() : 0;
            try
            {
                if (store != null && store.Enabled) { if (add) store.Add(map, callback); else store.Remove(map, callback); }
                else if (add) map.m_clearConnectedWearNTearCache += callback;
                else map.m_clearConnectedWearNTearCache -= callback;
            }
            finally { if (trace && store != null) store.RecordMutation(add, DiscoveryClock() - start); }
        }
        private static void HeightmapDestroyed(Heightmap __instance)
        { PerformancePlugin p = Instance; if (p != null && p._supportCache != null) p._supportCache.Forget(__instance); }
        private sealed class SupportCacheStore
        {
            private sealed class Hub
            {
                public readonly Heightmap Map;
                public readonly OrderedCallbacks Callbacks = new OrderedCallbacks();
                public readonly Action Dispatch;
                public Hub(Heightmap map) { Map = map; Dispatch = Callbacks.Invoke; }
            }
            private readonly Dictionary<Heightmap, Hub> _hubs = new Dictionary<Heightmap, Hub>();
            private readonly Func<Heightmap, Action> _get;
            private readonly Action<Heightmap, Action> _set;
            private readonly Func<IList> _maps;
            private readonly MethodInfo _nativeCallback;
            private int _adds, _removes, _optimizedRemoves, _fallbacks, _foreignFields;
            private long _mutationTicks, _maxMutationTicks;
            public bool Enabled { get; private set; }
            public int Subscribers { get; private set; }
            public SupportCacheStore()
            {
                FieldInfo callbacks = AccessTools.DeclaredField(typeof(Heightmap), "m_clearConnectedWearNTearCache");
                FieldInfo maps = AccessTools.DeclaredField(typeof(Heightmap), "s_heightmaps");
                _nativeCallback = AccessTools.DeclaredMethod(typeof(WearNTear), "ClearCachedSupport");
                if (callbacks == null || callbacks.FieldType != typeof(Action) || maps == null || !typeof(IList).IsAssignableFrom(maps.FieldType) || _nativeCallback == null) throw new MissingFieldException("Unsupported native terrain subscription storage");
                DynamicMethod get = new DynamicMethod("AP_GetSupportEvent", typeof(Action), new Type[] { typeof(Heightmap) }, typeof(Heightmap), true);
                ILGenerator il = get.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, callbacks); il.Emit(OpCodes.Ret);
                _get = (Func<Heightmap, Action>)get.CreateDelegate(typeof(Func<Heightmap, Action>));
                DynamicMethod set = new DynamicMethod("AP_SetSupportEvent", typeof(void), new Type[] { typeof(Heightmap), typeof(Action) }, typeof(Heightmap), true);
                il = set.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Stfld, callbacks); il.Emit(OpCodes.Ret);
                _set = (Action<Heightmap, Action>)set.CreateDelegate(typeof(Action<Heightmap, Action>));
                DynamicMethod source = new DynamicMethod("AP_GetHeightmaps", typeof(IList), Type.EmptyTypes, typeof(Heightmap), true);
                il = source.GetILGenerator(); il.Emit(OpCodes.Ldsfld, maps); il.Emit(OpCodes.Castclass, typeof(IList)); il.Emit(OpCodes.Ret);
                _maps = (Func<IList>)source.CreateDelegate(typeof(Func<IList>));
            }
            public void SetEnabled(bool enabled)
            {
                if (enabled == Enabled) return;
                if (!enabled)
                {
                    foreach (Hub hub in _hubs.Values) Restore(hub);
                    _hubs.Clear(); Subscribers = 0; Enabled = false; return;
                }
                Enabled = true;
                // Adoption is a mode transition, not per-frame work. Only exact native handlers qualify.
                IList maps = _maps(); if (maps == null) return;
                for (int i = 0; i < maps.Count; i++)
                {
                    Heightmap map = maps[i] as Heightmap; if (map == null) continue;
                    Action current = _get(map); if (current == null) continue;
                    Delegate[] handlers = current.GetInvocationList(); bool native = true;
                    for (int j = 0; j < handlers.Length; j++)
                        if (!(handlers[j].Target is WearNTear) || handlers[j].Method != _nativeCallback) { native = false; break; }
                    if (!native) continue;
                    Hub hub = new Hub(map);
                    for (int j = 0; j < handlers.Length; j++) hub.Callbacks.Add((Action)handlers[j]);
                    _hubs.Add(map, hub); Subscribers += hub.Callbacks.Count; _set(map, hub.Dispatch);
                }
            }
            private bool TryHub(Heightmap map, out Hub hub)
            {
                if (!_hubs.TryGetValue(map, out hub)) return false;
                if (object.ReferenceEquals(_get(map), hub.Dispatch)) return true;
                // Expand our slot in-place if an external subscriber changed the event; retain its order.
                Restore(hub); Subscribers -= hub.Callbacks.Count; _hubs.Remove(map); _foreignFields++; hub = null; return false;
            }
            public void Add(Heightmap map, Action callback)
            {
                if (callback == null) return;
                Hub hub;
                if (!TryHub(map, out hub))
                {
                    if (_get(map) != null) { map.m_clearConnectedWearNTearCache += callback; _fallbacks++; return; }
                    hub = new Hub(map); _hubs.Add(map, hub); _set(map, hub.Dispatch);
                }
                hub.Callbacks.Add(callback); Subscribers++;
            }
            public void Remove(Heightmap map, Action callback)
            {
                Hub hub;
                if (!TryHub(map, out hub)) { map.m_clearConnectedWearNTearCache -= callback; _fallbacks++; return; }
                if (hub.Callbacks.Remove(callback)) { Subscribers--; _optimizedRemoves++; }
                if (hub.Callbacks.Count == 0) { _set(map, null); _hubs.Remove(map); }
            }
            private void Restore(Hub hub)
            {
                Action current = _get(hub.Map); if (current == null) return;
                if (object.ReferenceEquals(current, hub.Dispatch)) { _set(hub.Map, OrderedCallbacks.Combine(hub.Callbacks.Snapshot())); return; }
                Delegate[] existing = current.GetInvocationList(); List<Action> expanded = new List<Action>(); bool found = false;
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i].Equals(hub.Dispatch)) { expanded.AddRange(hub.Callbacks.Snapshot()); found = true; }
                    else expanded.Add((Action)existing[i]);
                }
                // A writer that removed our dispatcher entirely wins; do not resurrect subscriptions.
                if (found) _set(hub.Map, OrderedCallbacks.Combine(expanded.ToArray()));
            }
            public void Forget(Heightmap map)
            {
                Hub hub;
                if (!_hubs.TryGetValue(map, out hub)) return;
                // The terrain object is already being destroyed; release its obsolete callback group once.
                map.m_clearConnectedWearNTearCache -= hub.Dispatch;
                Subscribers -= hub.Callbacks.Count; _hubs.Remove(map);
            }
            public void ResetMetrics() { _adds = 0; _removes = 0; _optimizedRemoves = 0; _fallbacks = 0; _foreignFields = 0; _mutationTicks = 0; _maxMutationTicks = 0; }
            public void RecordMutation(bool add, long ticks)
            { if (add) _adds++; else _removes++; _mutationTicks += ticks; _maxMutationTicks = Math.Max(_maxMutationTicks, ticks); }
            public string Report()
            {
                return "SupportCacheEnabledAtEnd=" + Enabled + "; OptimizedSupportSubscriptionsAtEnd=" + Subscribers + "; SupportTerrainGroups=" + _hubs.Count +
                    "\nRunSupportCallbackAdds=" + _adds + "; RunSupportCallbackRemoves=" + _removes + "; RunOptimizedSupportRemovals=" + _optimizedRemoves + "; RunSupportCallbackFallbacks=" + _fallbacks + "; RunSupportExternalEventChanges=" + _foreignFields +
                    "; RunSupportCallbackBookkeeping_ms=" + DiagnosticLog.Number(_mutationTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency) + "; MaxSupportCallbackBookkeeping_ms=" + DiagnosticLog.Number(_maxMutationTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
            }
        }
        private static void ZoneSpawned() { unchecked { _zoneSpawns++; } }
        private static void PieceCreated(Piece __instance)
        { ObservePieceLifecycle(__instance, true); }
        private static void PieceDestroyed()
        { ObservePieceLifecycle(null, false); }
        private static void ObservePieceLifecycle(Piece piece, bool created)
        {
            PerformancePlugin p = Instance;
            if (p == null || p._failed) return;
            try
            {
                unchecked { p._registryRevision++; if (created) p._createdPieces++; else p._destroyedPieces++; }
                p._discoveryReady = false;
                if (!p.BuildingShadows || p._pieceSource == null) return;
                IList all = p._pieceSource(); int count = all == null ? 0 : all.Count;
                int oldCount = p._hookLoadedCount; p._hookLoadedCount = count;
                if (!created || !p._sweep.Started || !p.isActiveAndEnabled || Player.m_localPlayer == null || !p._settings._enabled.Value || !p.BuildingShadows) return;
                // Queue only a verified registry addition. Preview/non-registered Pieces must not be optimized.
                if (count > 0 && count == oldCount + 1 && object.ReferenceEquals(all[count - 1], piece)) p._newPieces.Add(piece);
                else if (count != oldCount) p._reconcileRequested = true;
            }
            catch (Exception ex) { p.Fail(ex); }
        }
        private static void EffectsCreated(GameObject[] __result)
        {
            PerformancePlugin p = Instance;
            if (p == null || p._failed || !p.isActiveAndEnabled || Player.m_localPlayer == null || __result == null) return;
            bool optimize = p._settings._enabled.Value && p._settings._particles.Value;
            bool observe = p._recorder.Recording && p._captureStarted && p._settings._diagnostics.Value;
            if (!optimize && !observe) return;
            try
            {
                for (int i = 0; i < __result.Length; i++)
                {
                    GameObject go = __result[i]; if (go == null) continue;
                    string name = PerformancePolicy.RootName(go.name); bool allowed = p._allowed.Contains(name);
                    if (observe) p.ObserveEffect(name, allowed);
                    if (!optimize) continue;
                    if (!allowed) { p._unclassified++; continue; }
                    if (p._pending.Count >= MaximumPending) { p._pendingDropped++; continue; }
                    p._pending.Enqueue(go);
                }
            }
            catch (Exception ex) { p.Fail(ex); }
        }
        private void ObserveEffect(string name, bool allowed)
        {
            EffectObservation entry;
            if (!_observations.TryGetValue(name, out entry))
            {
                if (_observations.Count >= 128) { _effectNameOverflow++; return; }
                entry = new EffectObservation { Allowlisted = allowed, FirstSeconds = Math.Max(0, Time.realtimeSinceStartup - _recordStart) };
                _observations.Add(name, entry);
            }
            entry.Count++;
        }
        private void Update()
        {
            if (_failed) return;
            UiTracer.Tick();   // 0.6.7
            SampleFps();
            FrameContext previousActivity = _activity; _activity = new FrameContext();
            long workStart = _recorder.Recording ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            try
            {
                if (_settings._toggleKey.Value.IsDown())
                {
                    if (_recorder.Recording) FinishBenchmark("Optimization setting changed");
                    _settings._enabled.Value = !_settings._enabled.Value;
                }
                if (_settings._indicatorKey.Value.IsDown()) _settings._showIndicator.Value = !_settings._showIndicator.Value;   // 0.6.5 (user)
                if (_settings._fpsKey.Value.IsDown()) _settings._fpsCounter.Value = !_settings._fpsCounter.Value;
                if (_supportCache != null) _supportCache.SetEnabled(_supportHook && _settings._enabled.Value && _settings._supportCache.Value);
                UnloadBudget.Active = _settings._enabled.Value && _settings._spreadUnload.Value; UnloadBudget.PerTick = _settings._unloadPerTick.Value;
                bool active = Player.m_localPlayer != null;
                if (_settings._recordKey.Value.IsDown())
                {
                    if (_recorder.Recording) FinishBenchmark("Stopped by hotkey");
                    else if (active) StartBenchmark();
                    else Logger.LogInfo("Enter a world before starting a benchmark.");
                }
                if (_recorder.Recording)
                {
                    if (!active) FinishBenchmark("World/player left");
                    else if (Time.realtimeSinceStartup >= _recordStart)
                    {
                        if (!_captureStarted)
                        {
                            _lastCreated = _createdPieces; _lastDestroyed = _destroyedPieces; _startSnapshots = _snapshots;
                            if (_supportCache != null) _supportCache.ResetMetrics();
                            UnloadBudget.ResetMetrics();
                            _captureStarted = true; _previousGc = GC.CollectionCount(0); _gcStart = _previousGc;
                            _startRegistered = _registeredSystems; _startConflicts = _conflicts; _startDropped = _pendingDropped;
                            _startOverflow = _trackingOverflow; _startExpired = _expiredQueued;
                        }
                        int gc = GC.CollectionCount(0);
                        bool accepted = _recorder.Add(Time.unscaledDeltaTime, gc - _previousGc, _previousWorkMs, Math.Max(0, Time.realtimeSinceStartup - _recordStart)); _previousGc = gc;
                        if (accepted && _settings._diagnostics.Value)
                        {
                            long diagnosticStart = DiscoveryClock();
                            long main, render, allocated; _runtime.ReadFrameCounters(out main, out render, out allocated); _recorder.SetLastEngineCounters(main, render, allocated);
                            previousActivity.LoadedPieces = LoadedPieceCount();
                            previousActivity.CreatedPieces = _pieceHooks ? unchecked(_createdPieces - _lastCreated) : -1;
                            previousActivity.ZonesSpawned = unchecked(_zoneSpawns - _lastZones);
                            previousActivity.DestroyedPieces = _pieceHooks ? unchecked(_destroyedPieces - _lastDestroyed) : -1;
                            previousActivity.DiscoveryChecked = DiscoveryChecked; previousActivity.DiscoveryTotal = DiscoveryTotal;
                            previousActivity.DiscoveryQueue = _newPieces.Count;
                            previousActivity.DiscoveryReady = _recordEnabled && BuildingShadows && _pieceSource != null ? (_discoveryReady ? 1 : 0) : -1;
                            _recorder.SetLastContext(previousActivity);
                            _activity.PreviousDiagnosticsMs += MillisecondsSince(diagnosticStart);
                        }
                        _lastCreated = _createdPieces; _lastDestroyed = _destroyedPieces; _lastZones = _zoneSpawns;
                        if (_settings._markerKey.Value.IsDown())
                        {
                            if (_diagnostics.Mark(Time.realtimeSinceStartup - _recordStart, _recorder.Count)) Notify("Performance marker " + _diagnostics.MarkerCount + " saved.");
                        }
                        if (_settings._diagnostics.Value && Time.realtimeSinceStartup >= _nextDiagnostic)
                        { long diagnosticStart = DiscoveryClock(); _nextDiagnostic = Time.realtimeSinceStartup + 1f; SampleDiagnostics(); _activity.PreviousDiagnosticsMs += MillisecondsSince(diagnosticStart); }
                        if (_recorder.Full || Time.realtimeSinceStartup - _recordStart >= _settings._seconds.Value) FinishBenchmark(_recorder.Full ? "Frame buffer full" : "Completed");
                    }
                }
                bool enabledNow = active && _settings._enabled.Value;
                bool shadowsNow = enabledNow && BuildingShadows, particlesNow = enabledNow && _settings._particles.Value;
                if (_wasShadows && !shadowsNow) RestoreShadows();
                if (_wasParticles && !particlesNow) { RestoreParticles(); _pending.Clear(); }
                _wasShadows = shadowsNow; _wasParticles = particlesNow;
                UpdateGlobalShadows(enabledNow && _settings._noShadows.Value);
                UpdateLowShadows(enabledNow && !_settings._noShadows.Value && _settings._lowShadows.Value);
                UpdateGcSlice(enabledNow && _settings._gcSlice.Value);
                WorldTweaks.DoorSleepOn = enabledNow && _settings._doorSleep.Value; WorldTweaks.Tick();
                WorldTweaks.GrassPatchSize = _settings._enabled.Value ? _settings._grassPatch.Value : 0f;
                UpdateAsyncUpload(enabledNow && _settings._asyncUpload.Value);
                _view.Update(enabledNow && _settings._viewCulling.Value, _settings._pieceDrawM.Value, _settings._nonSolidDrawM.Value, _settings._smallDrawM.Value, _settings._lodMax.Value);
                if (active) RefreshDiscoveryHud(shadowsNow);
                if (!enabledNow) return;
                Vector3 eye = Player.m_localPlayer.transform.position;
                if (shadowsNow)
                {
                    long stageStart = DiscoveryClock(); DiscoverPieces();
                    if (workStart != 0) _activity.PreviousDiscoveryMs = MillisecondsSince(stageStart);
                    stageStart = DiscoveryClock(); UpdateShadows(eye);
                    if (workStart != 0) _activity.PreviousShadowMs = MillisecondsSince(stageStart);
                }
                if (particlesNow)
                {
                    // At most two queued effect roots examined per frame, never a scene-wide scan.
                    for (int i = 0; i < 2 && _pending.Count > 0; i++) RegisterCosmetic(_pending.Dequeue());
                    if (Time.realtimeSinceStartup >= _nextParticles) { _nextParticles = Time.realtimeSinceStartup + 0.2f; UpdateParticles(eye); }
                }
            }
            catch (Exception ex) { Fail(ex); }
            finally { if (workStart != 0) _previousWorkMs = (float)((System.Diagnostics.Stopwatch.GetTimestamp() - workStart) * 1000.0 / System.Diagnostics.Stopwatch.Frequency); }
        }
        private static float MillisecondsSince(long start)
        { return (float)((DiscoveryClock() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency); }
        private bool DiscoveryIsReady(int loaded)
        {
            bool coherent = _pieceHooks ? (!_reconcileRequested && _hookLoadedCount == loaded) : (_sweep.Revision == _registryRevision && _sweep.SourceCount == loaded);
            return coherent && _newPieces.Ready(_sweep.Complete, _sweep.Limited || _registrationLimited, _pendingFirstApply, _observedRevision == _registryRevision && _observedLoaded == loaded && Time.realtimeSinceStartup - _registryStableSince >= 1f);
        }
        private void DiscoverPieces()
        {
            if (_pieceSource == null) return;
            IList all = _pieceSource(); _loadedPieces = all == null ? 0 : all.Count;
            if (_observedRevision != _registryRevision || _observedLoaded != _loadedPieces)
            {
                _observedRevision = _registryRevision; _observedLoaded = _loadedPieces;
                _registryStableSince = Time.realtimeSinceStartup; _discoveryReady = false;
            }
            if (_sweep.Started && _pieceHooks && _hookLoadedCount != _loadedPieces) _reconcileRequested = true;
            bool stale = _sweep.Revision != _registryRevision || _sweep.SourceCount != _loadedPieces;
            bool fallback = !_pieceHooks && (stale || Time.realtimeSinceStartup >= _nextFallbackSweep);
            bool retryLimit = (_registrationLimited || _sweep.Limited) && Time.realtimeSinceStartup >= _nextFallbackSweep;
            long stageStart = DiscoveryClock();
            double ms = _settings._discoveryMs.Value;
            if (Time.unscaledDeltaTime > 0.0333334f) ms *= 0.5;
            long deadline = stageStart + (long)(ms * System.Diagnostics.Stopwatch.Frequency / 1000.0);
            if (!_sweep.Started || (_sweep.Complete && (_reconcileRequested || _newPieces.Overflowed || fallback || retryLimit)))
            {
                _registrationLimited = false; _reconcileRequested = false; _newPieces.Clear();
                _sweep.Begin(all, _registryRevision, MaximumPieces); _snapshots++; _hookLoadedCount = _loadedPieces;
                _nextFallbackSweep = Time.realtimeSinceStartup + 5f;
            }
            int remaining = _settings._piecesPerFrame.Value;
            if (!_sweep.Complete && DiscoveryClock() < deadline)
            {
                int done = _sweep.Step(_registerPiece, remaining, deadline, DiscoveryClock);
                remaining -= done; _activity.PreviousExamined += done;
            }
            while (_sweep.Complete && _newPieces.Count > 0 && remaining > 0 && DiscoveryClock() < deadline)
            { RegisterPiece(_newPieces.Take()); remaining--; _activity.PreviousExamined++; }
            _discoveryReady = DiscoveryIsReady(_loadedPieces);
        }
        private void RegisterPiece(Piece p)
        {
            if (p == null) return;
            int id = p.GetInstanceID(); if (_knownPieces.Contains(id)) return;
            if (_pieces.Count >= MaximumPieces) { _registrationLimited = true; return; }
            MeshRenderer[] renderers = p.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length > 128) renderers = new MeshRenderer[0];
            ShadowState[] states = new ShadowState[renderers.Length];
            for (int r = 0; r < renderers.Length; r++)
            {
                MeshRenderer mr = renderers[r];
                if (mr == null || mr.GetComponentInParent<Piece>() != p) continue;
                states[r] = new ShadowState { Renderer = mr, Mode = new OwnedValue<ShadowCastingMode>(mr.shadowCastingMode) };
            }
            bool applied = states.Length == 0;
            if (!applied) _pendingFirstApply++;
            _activity.PreviousRegistered++;
            _pieces.Add(new PieceState { Piece = p, Id = id, Renderers = states, InitialApplied = applied }); _knownPieces.Add(id);
        }
        private void UpdateShadows(Vector3 eye)
        {
            int budget = _settings._renderersPerFrame.Value, examined = 0, remaining = _pieces.Count;
            double ms = _settings._shadowMs.Value; if (Time.unscaledDeltaTime > 0.0333334f) ms *= 0.5;
            long deadline = DiscoveryClock() + (long)(ms * System.Diagnostics.Stopwatch.Frequency / 1000.0);
            while (_pieces.Count > 0 && remaining > 0 && examined < budget && DiscoveryClock() < deadline)
            {
                if (_pieceCursor >= _pieces.Count) { _pieceCursor = 0; _rendererCursor = 0; }
                PieceState p = _pieces[_pieceCursor];
                if (p.Piece == null)
                {
                    if (!p.InitialApplied) _pendingFirstApply--;
                    RestorePiece(p); _knownPieces.Remove(p.Id); _pieces[_pieceCursor] = _pieces[_pieces.Count - 1]; _pieces.RemoveAt(_pieces.Count - 1); _rendererCursor = 0; examined++; remaining--; continue;
                }
                p.Far = PerformancePolicy.Far(p.Far, (p.Piece.transform.position - eye).sqrMagnitude, _settings._shadowDistance.Value, _settings._hysteresis.Value);
                while (_rendererCursor < p.Renderers.Length && examined < budget && DiscoveryClock() < deadline)
                {
                    ShadowState state = p.Renderers[_rendererCursor++]; examined++; _activity.PreviousRendererChecks++;
                    if (state == null || state.Renderer == null || state.Mode.Contested) continue;
                    ShadowCastingMode desired = p.Far ? ShadowCastingMode.Off : state.Mode.Original;
                    ShadowCastingMode current = state.Renderer.shadowCastingMode;
                    bool contested = state.Mode.Contested;
                    if (state.Mode.TryApply(current, desired)) state.Renderer.shadowCastingMode = desired;
                    if (!contested && state.Mode.Contested) _conflicts++;
                }
                if (_rendererCursor >= p.Renderers.Length) { if (!p.InitialApplied) { p.InitialApplied = true; _pendingFirstApply--; } _pieceCursor++; _rendererCursor = 0; remaining--; if (p.Renderers.Length == 0) examined++; }
            }
        }
        private void RegisterCosmetic(GameObject root)
        {
            if (root == null) { _expiredQueued++; return; }
            if (root.GetComponentInParent<ZNetView>() != null) return;
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (_systems.Count >= MaximumSystems) { _trackingOverflow++; break; }
                ParticleSystem ps = systems[i]; if (ps == null || ps.GetComponentInParent<ZNetView>() != null) continue;
                int id = ps.GetInstanceID(); if (_knownSystems.Contains(id)) continue;
                ParticleSystem.CollisionModule collision = ps.collision; ParticleSystem.TriggerModule trigger = ps.trigger;
                ParticleSystem.MainModule main = ps.main; ParticleSystem.EmissionModule emission = ps.emission;
                // Collision/trigger systems may drive gameplay. Only positive, finite visual emission is eligible.
                if (collision.enabled || trigger.enabled || !emission.enabled || main.maxParticles <= 0 || !PerformancePolicy.Finite(emission.rateOverTimeMultiplier) || !PerformancePolicy.Finite(emission.rateOverDistanceMultiplier)) continue;
                _systems.Add(new ParticleState { System = ps, Id = id, Maximum = new OwnedValue<int>(main.maxParticles), RateTime = new OwnedValue<float>(emission.rateOverTimeMultiplier), RateDistance = new OwnedValue<float>(emission.rateOverDistanceMultiplier) });
                _knownSystems.Add(id); _registeredSystems++;
            }
        }
        private void UpdateParticles(Vector3 eye)
        {
            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                ParticleState s = _systems[i];
                if (s.System == null || !s.System.IsAlive(true))
                { RestoreParticle(s); _knownSystems.Remove(s.Id); _systems.RemoveAt(i); }
            }
            int count = _systems.Count;
            for (int i = 0; i < count; i++)
            {
                ParticleState s = _systems[i]; ParticleSystem ps = s.System;
                if (ps == null || s.Maximum.Contested || s.RateTime.Contested || s.RateDistance.Contested) continue;
                bool far = (ps.transform.position - eye).sqrMagnitude > _settings._effectDistance.Value * _settings._effectDistance.Value;
                int cap = PerformancePolicy.ParticleCap(s.Maximum.Original, count, _settings._particleBudget.Value, _settings._particleMax.Value, far);
                float ratio = s.Maximum.Original <= 0 ? 1f : Math.Min(1f, (float)cap / s.Maximum.Original);
                ParticleSystem.MainModule main = ps.main; ParticleSystem.EmissionModule em = ps.emission;
                if (s.Maximum.TryApply(main.maxParticles, cap)) main.maxParticles = cap;
                if (s.RateTime.TryApply(em.rateOverTimeMultiplier, s.RateTime.Original * ratio)) em.rateOverTimeMultiplier = s.RateTime.Original * ratio;
                if (s.RateDistance.TryApply(em.rateOverDistanceMultiplier, s.RateDistance.Original * ratio)) em.rateOverDistanceMultiplier = s.RateDistance.Original * ratio;
                if (s.Maximum.Contested || s.RateTime.Contested || s.RateDistance.Contested) { RestoreParticle(s); _conflicts++; }
            }
        }
        private static void RestorePiece(PieceState p)
        {
            for (int i = 0; i < p.Renderers.Length; i++)
            {
                ShadowState s = p.Renderers[i]; if (s == null || s.Renderer == null) continue;
                if (s.Mode.CanRestore(s.Renderer.shadowCastingMode)) s.Renderer.shadowCastingMode = s.Mode.Original;
            }
        }
        private void UpdateGlobalShadows(bool disable)
        {
            if (!disable) { RestoreGlobalShadows(); return; }
            ShadowQuality current = QualitySettings.shadows;
            if (_globalShadows == null) _globalShadows = new OwnedValue<ShadowQuality>(current);
            bool contested = _globalShadows.Contested;
            if (_globalShadows.TryApply(current, ShadowQuality.Disable)) QualitySettings.shadows = ShadowQuality.Disable;
            if (!contested && _globalShadows.Contested)
            {
                _conflicts++;
                Logger.LogWarning("Another writer changed shadow quality; its setting is preserved. All-shadows mode will not override it.");
            }
        }
        // 0.4.1 Low Shadows: four quality values, each owned separately (another writer - e.g. Valheim's graphics menu - wins).
        private readonly ViewCulling _view = new ViewCulling();
        private OwnedValue<ulong> _gcSliceOwned;
        private OwnedValue<int> _uploadSlice, _uploadBuffer;
        private void UpdateAsyncUpload(bool on)
        {
            if (!on)
            {
                if (_uploadSlice != null && _uploadSlice.CanRestore(QualitySettings.asyncUploadTimeSlice)) QualitySettings.asyncUploadTimeSlice = _uploadSlice.Original;
                if (_uploadBuffer != null && _uploadBuffer.CanRestore(QualitySettings.asyncUploadBufferSize)) QualitySettings.asyncUploadBufferSize = _uploadBuffer.Original;
                _uploadSlice = null; _uploadBuffer = null; return;
            }
            if (_uploadSlice == null) { _uploadSlice = new OwnedValue<int>(QualitySettings.asyncUploadTimeSlice); _uploadBuffer = new OwnedValue<int>(QualitySettings.asyncUploadBufferSize); }
            int slice = Math.Max(_uploadSlice.Original, 4), buffer = Math.Max(_uploadBuffer.Original, 64);   // only ever raised; written once (owned), not every frame
            if (_uploadSlice.TryApply(QualitySettings.asyncUploadTimeSlice, slice)) QualitySettings.asyncUploadTimeSlice = slice;
            if (_uploadBuffer.TryApply(QualitySettings.asyncUploadBufferSize, buffer)) QualitySettings.asyncUploadBufferSize = buffer;
        }
        private void UpdateGcSlice(bool on)
        {
            if (!on || !UnityEngine.Scripting.GarbageCollector.isIncremental)
            {
                if (_gcSliceOwned != null && _gcSliceOwned.CanRestore(UnityEngine.Scripting.GarbageCollector.incrementalTimeSliceNanoseconds))
                    UnityEngine.Scripting.GarbageCollector.incrementalTimeSliceNanoseconds = _gcSliceOwned.Original;
                _gcSliceOwned = null; return;
            }
            ulong current = UnityEngine.Scripting.GarbageCollector.incrementalTimeSliceNanoseconds;
            if (_gcSliceOwned == null) _gcSliceOwned = new OwnedValue<ulong>(current);
            ulong want = (ulong)(Mathf.Clamp(_settings._gcSliceMs.Value, 1f, 10f) * 1000000f);
            if (_gcSliceOwned.TryApply(current, want)) UnityEngine.Scripting.GarbageCollector.incrementalTimeSliceNanoseconds = want;
        }
        private OwnedValue<ShadowQuality> _lowQuality;
        private OwnedValue<ShadowResolution> _lowResolution;
        private OwnedValue<float> _lowDistance;
        private OwnedValue<int> _lowCascades;
        private void UpdateLowShadows(bool on)
        {
            if (!on) { RestoreLowShadows(); return; }
            if (_lowQuality == null)
            {
                _lowQuality = new OwnedValue<ShadowQuality>(QualitySettings.shadows);
                _lowResolution = new OwnedValue<ShadowResolution>(QualitySettings.shadowResolution);
                _lowDistance = new OwnedValue<float>(QualitySettings.shadowDistance);
                _lowCascades = new OwnedValue<int>(QualitySettings.shadowCascades);
            }
            // never turn shadows ON if the game has them off; only reduce what is there
            ShadowQuality q = QualitySettings.shadows == ShadowQuality.Disable ? ShadowQuality.Disable : ShadowQuality.HardOnly;
            if (_lowQuality.TryApply(QualitySettings.shadows, q)) QualitySettings.shadows = q;
            if (_lowResolution.TryApply(QualitySettings.shadowResolution, ShadowResolution.Low)) QualitySettings.shadowResolution = ShadowResolution.Low;
            float dist = Mathf.Min(_settings._lowShadowDistance.Value, _lowDistance.Original);
            if (_lowDistance.TryApply(QualitySettings.shadowDistance, dist)) QualitySettings.shadowDistance = dist;
            int cascades = Math.Min(_settings._lowShadowCascades.Value, Math.Max(1, _lowCascades.Original));
            if (_lowCascades.TryApply(QualitySettings.shadowCascades, cascades)) QualitySettings.shadowCascades = cascades;
        }
        private void RestoreLowShadows()
        {
            if (_lowQuality == null) return;
            if (_lowQuality.CanRestore(QualitySettings.shadows)) QualitySettings.shadows = _lowQuality.Original;
            if (_lowResolution.CanRestore(QualitySettings.shadowResolution)) QualitySettings.shadowResolution = _lowResolution.Original;
            if (_lowDistance.CanRestore(QualitySettings.shadowDistance)) QualitySettings.shadowDistance = _lowDistance.Original;
            if (_lowCascades.CanRestore(QualitySettings.shadowCascades)) QualitySettings.shadowCascades = _lowCascades.Original;
            _lowQuality = null; _lowResolution = null; _lowDistance = null; _lowCascades = null;
        }
        private void RestoreGlobalShadows()
        {
            if (_globalShadows == null) return;
            if (_globalShadows.CanRestore(QualitySettings.shadows)) QualitySettings.shadows = _globalShadows.Original;
            _globalShadows = null;
        }
        private void RestoreShadows()
        {
            for (int i = 0; i < _pieces.Count; i++) RestorePiece(_pieces[i]);
            _pieces.Clear(); _knownPieces.Clear(); _pieceCursor = 0; _rendererCursor = 0; _pendingFirstApply = 0; _sweep.Clear(); _discoveryReady = false; _observedLoaded = -1; _loadedPieces = 0; _registrationLimited = false; _newPieces.Clear(); _reconcileRequested = false; _hookLoadedCount = -1;
        }
        private static void RestoreParticle(ParticleState s)
        {
            if (s.System == null) return;
            ParticleSystem.MainModule main = s.System.main; ParticleSystem.EmissionModule em = s.System.emission;
            if (s.Maximum.CanRestore(main.maxParticles)) main.maxParticles = s.Maximum.Original;
            if (s.RateTime.CanRestore(em.rateOverTimeMultiplier)) em.rateOverTimeMultiplier = s.RateTime.Original;
            if (s.RateDistance.CanRestore(em.rateOverDistanceMultiplier)) em.rateOverDistanceMultiplier = s.RateDistance.Original;
        }
        private void RestoreParticles()
        {
            for (int i = 0; i < _systems.Count; i++) RestoreParticle(_systems[i]);
            _systems.Clear(); _knownSystems.Clear();
        }
        private void RestoreAll() { if (_supportCache != null) _supportCache.SetEnabled(false); RestoreGlobalShadows(); RestoreLowShadows(); _view.Restore(); UpdateGcSlice(false); UpdateAsyncUpload(false); WorldTweaks.WakeAll(); RestoreShadows(); RestoreParticles(); _pending.Clear(); }
        private void Fail(Exception error)
        {
            if (_failed) return; _failed = true; _discoveryReady = false; _discoveryContent.text = "Discovery stopped"; _discoveryHint.text = "Check the performance log.";
            if (_recorder.Recording) FinishBenchmark("Optimization error; recording interrupted");
            _runtime.Dispose();
            try { RestoreAll(); } catch (Exception) { }
            Logger.LogError("Performance features disabled after an error; game method results remain unchanged. " + error);
        }
        private void ConfigChanged(object sender, SettingChangedEventArgs args)
        {
            if (_recorder.Recording) FinishBenchmark("Configuration changed; stopped to avoid mixing settings");
            if (args.ChangedSetting == _settings._toggleKey) UpdateShortcutText();
            if (args.ChangedSetting == _settings._enabled) Notify(_settings._enabled.Value ? "Optimization: ON" : "Optimization: OFF");
        }
        private int LoadedPieceCount()
        {
            if (_pieceSource == null) return -1;
            IList all = _pieceSource(); return all == null ? 0 : all.Count;
        }
        private void StartBenchmark()
        {
            if (_pieceSource != null) _discoveryReady = DiscoveryIsReady(LoadedPieceCount());
            _recordEnabled = _settings._enabled.Value;
            if (_recordEnabled && BuildingShadows && _pieceSource != null && _settings._requireReady.Value && !_discoveryReady)
            { Notify("Buildings are still scanning/applying. Wait for Discovery: READY before a comparable ON capture."); return; }
            _runtime.Dispose(); _diagnostics.Reset(); _observations.Clear(); _effectNameOverflow = 0;
            _captureStarted = false; _previousWorkMs = 0f; _activity = new FrameContext();
            if (_settings._diagnostics.Value) _runtime.Begin(_settings._renderCounters.Value);
            _recordContext = "Scenario=" + _settings._scenario.Value.Replace("\r", " ").Replace("\n", " ") + "\nDetailedDiagnostics=" + _settings._diagnostics.Value + "; RenderingCounters=" + _settings._renderCounters.Value + "\nLoadedPiecesAtStart=" + LoadedPieceCount() + "\nOptimizations=" + _settings._enabled.Value + "; Shadows=" + BuildingShadows + "; DisableAllShadows=" + _settings._noShadows.Value + "; LowShadows=" + _settings._lowShadows.Value + "; UnityShadowQuality=" + QualitySettings.shadows + "; ShadowResolution=" + QualitySettings.shadowResolution + "; UnityShadowDistance=" + QualitySettings.shadowDistance.ToString(CultureInfo.InvariantCulture) + "; ShadowCascades=" + QualitySettings.shadowCascades + "; Particles=" + _settings._particles.Value + "; ShadowDistance=" + _settings._shadowDistance.Value.ToString(CultureInfo.InvariantCulture) + "; ParticleMax=" + _settings._particleMax.Value + "; SharedBudget=" + _settings._particleBudget.Value;
            _recordContext += "\nSupportCacheHook=" + _supportHook + "; SupportCacheEnabled=" + (_supportCache != null && _supportCache.Enabled) + "; OptimizedSupportSubscriptionsAtStart=" + (_supportCache == null ? 0 : _supportCache.Subscribers);
            _recordContext += "\nDiscoveryMode=" + (_settings._noShadows.Value ? "Not needed: all-shadows mode" : (_pieceHooks ? "Initial snapshot then lifecycle queue" : "Snapshot reconciliation fallback")) + "; ShadowTimeBudget_ms=" + _settings._shadowMs.Value.ToString(CultureInfo.InvariantCulture) + "\nDiscoveryReadyAtStart=" + _discoveryReady + "; DiscoveryChecked=" + DiscoveryChecked + "; DiscoveryTotal=" + DiscoveryTotal + "; PieceLifecycleHooks=" + _pieceHooks + "\nTrackedPiecesAtStart=" + _pieces.Count + "; BuildingRegistry=" + (_pieceSource != null) + "; CosmeticHook=" + _effectsHook + "\nUnity=" + Application.unityVersion + "; CPU=" + SystemInfo.processorType + "; GPU=" + SystemInfo.graphicsDeviceName + "; RAM_MB=" + SystemInfo.systemMemorySize + "; VRAM_MB=" + SystemInfo.graphicsMemorySize;
            _recordContext += "\nResolution=" + Screen.width + "x" + Screen.height + "; VSync=" + QualitySettings.vSyncCount + "; TargetFPS=" + Application.targetFrameRate;
            _recordContext += "\nGraphicsAPI=" + SystemInfo.graphicsDeviceType + "; QualityLevel=" + QualitySettings.GetQualityLevel() + "; QualityShadowDistance=" + QualitySettings.shadowDistance.ToString(CultureInfo.InvariantCulture) + "; LogicalProcessors=" + Environment.ProcessorCount;
            _recordContext += "\nLoaded BepInEx plugins (inventory, not measured cost):";
            foreach (KeyValuePair<string, PluginInfo> pair in BepInEx.Bootstrap.Chainloader.PluginInfos)
                _recordContext += "\n" + pair.Key + " " + pair.Value.Metadata.Version;
            _recordStart = Time.realtimeSinceStartup + 1f; _nextDiagnostic = _recordStart; _gcStart = GC.CollectionCount(0); _recorder.Start();
            Notify("Performance capture " + (_recordEnabled ? "ON" : "OFF") + " starts in 1s; records " + _settings._seconds.Value + "s. Ctrl+Alt+F9 marks an action.");
        }
        private void FinishBenchmark(string reason)
        {
            if (!_recorder.Recording || _finishing) return; _finishing = true;
            try
            {
                int gcEnd = GC.CollectionCount(0);
                _runtime.Dispose();
                FrameReport r = _recorder.Stop();
                string folder = Path.Combine(Paths.BepInExRootPath, "AethelbornPerformanceReports"); Directory.CreateDirectory(folder);
                string name = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + "_" + (_recordEnabled ? "ON" : "OFF");
                string summary = Name + " " + Version + " benchmark (client frame intervals; not per-mod/CPU/GPU attribution)\n" + reason + "\n" + _recordContext;
                summary += "\nFrames=" + r.Frames + "; Seconds=" + r.Seconds.ToString("F3", CultureInfo.InvariantCulture) + "; AverageFPS=" + r.AverageFps.ToString("F2", CultureInfo.InvariantCulture) + "; OnePercentLowFPS=" + r.OnePercentLowFps.ToString("F2", CultureInfo.InvariantCulture);
                summary += "\nP95_ms=" + r.P95ms.ToString("F3", CultureInfo.InvariantCulture) + "; P99_ms=" + r.P99ms.ToString("F3", CultureInfo.InvariantCulture) + "; Worst_ms=" + r.WorstMs.ToString("F3", CultureInfo.InvariantCulture);
                summary += "\nFramesOver33ms=" + r.Over33ms + "; Over50ms=" + r.Over50ms + "; Over100ms=" + r.Over100ms + "; GC0Collections=" + (_captureStarted ? gcEnd - _gcStart : 0);
                summary += "\nLoadedPiecesAtEnd=" + LoadedPieceCount() + "; TrackedPieces=" + _pieces.Count + "; ActiveCosmeticSystems=" + _systems.Count + "; LifetimeRegisteredCosmeticSystems=" + _registeredSystems + "; LifetimeQueueOverflow=" + _pendingDropped + "; LifetimeUnclassifiedEffectRoots=" + _unclassified + "; LifetimeExternalPropertyChanges=" + _conflicts;
                summary += "\nRunRegisteredCosmeticSystems=" + (_captureStarted ? _registeredSystems - _startRegistered : 0) + "; RunQueueOverflow=" + (_captureStarted ? _pendingDropped - _startDropped : 0) + "; RunTrackingOverflowRoots=" + (_captureStarted ? _trackingOverflow - _startOverflow : 0) + "; RunExpiredQueuedRoots=" + (_captureStarted ? _expiredQueued - _startExpired : 0) + "; RunExternalPropertyChanges=" + (_captureStarted ? _conflicts - _startConflicts : 0);
                summary += "\nResidentMemorySource=" + _runtime.ResidentSource + "\nDiscoveryReadyAtEnd=" + _discoveryReady + "; DiscoveryCheckedAtEnd=" + DiscoveryChecked + "; DiscoveryTotalAtEnd=" + DiscoveryTotal + "; PendingShadowPieces=" + _pendingFirstApply + "; DiscoveryLimited=" + _sweep.Limited;
                if (_supportCache != null) summary += "\n" + _supportCache.Report();
                summary += "\n" + _recorder.CauseSummary();
                summary += "\n" + UnloadBudget.Report();
                summary += "\n" + WorldTweaks.Report();
                summary += "\nGCSliceExperiment=" + _settings._gcSlice.Value + "; RunZoneSpawns=" + _zoneSpawns;
                summary += "\n" + _view.Report() + "; PieceDrawM=" + _settings._pieceDrawM.Value.ToString(CultureInfo.InvariantCulture) + "; DecorDrawM=" + _settings._nonSolidDrawM.Value.ToString(CultureInfo.InvariantCulture) + "; SmallDrawM=" + _settings._smallDrawM.Value.ToString(CultureInfo.InvariantCulture);
                summary += "\nPendingDiscoveryPieces=" + _newPieces.Count + "; DiscoveryQueueOverflow=" + _newPieces.Overflowed + "; DiscoveryReconcilePending=" + _reconcileRequested + "; RunFullDiscoverySnapshots=" + (_captureStarted ? _snapshots - _startSnapshots : 0);
                summary += "\n" + _recorder.ContextSummary();
                summary += "\n" + _diagnostics.Summary() + "; EffectNamesOverflowRoots=" + _effectNameOverflow;
                summary += "\nNotes: -1 means unavailable. Per-frame lifecycle events/counts are sampled at Update entry; previous_* work fields describe the preceding completed plugin Update. Piece Awake/Destroy can include previews; these are lifecycle observations, not proof of asset loading. Discovery checked/total counts include queued checks since the last full snapshot; loaded_pieces is the current live count. Time budgets are soft; individual engine calls, registry snapshot copies and cleanup can exceed them. Timeline snapshots are taken about once per second and are not measurements of each spike. Process CPU is normalized across all logical cores; low overall CPU does not rule out a busy main thread. GC overlap and effect timing are correlations, not proof of cause. Previous plugin update is wall-clock work including diagnostics, not per-mod CPU attribution.";
                if (_recordEnabled && BuildingShadows && _pieceSource == null) summary += "\nWARNING: Structure adapter unavailable; no building shadow optimization.";
                if (_recordEnabled && BuildingShadows && !_discoveryReady) summary += "\nWARNING: Building discovery/applying incomplete, changed during the walk, or capacity exceeded. Check the discovery timeline before comparing.";
                if (_recordEnabled && _settings._particles.Value && (!_captureStarted || _registeredSystems == _startRegistered)) summary += "\nWARNING: No new eligible cosmetic systems registered during this capture; particle optimization coverage is unproven for this run.";
                if (r.Frames == 0) summary += "\nWARNING: No frame samples. Capture stopped before warmup completed.";
                File.WriteAllText(Path.Combine(folder, name + ".txt"), summary);
                File.WriteAllText(Path.Combine(folder, name + "_timeline.csv"), _diagnostics.Csv());
                File.WriteAllText(Path.Combine(folder, name + "_markers.csv"), _diagnostics.MarkersCsv());
                File.WriteAllText(Path.Combine(folder, name + "_spikes.csv"), _recorder.SpikesCsv(_settings._spikeThreshold.Value));
                File.WriteAllText(Path.Combine(folder, name + "_effects.csv"), EffectsCsv());
                File.WriteAllText(Path.Combine(folder, name + ".csv"), _recorder.Csv());
                Logger.LogInfo("Benchmark saved: " + folder);
                Notify("Capture saved: " + name + ". Average " + r.AverageFps.ToString("F1", CultureInfo.InvariantCulture) + " FPS; 1% low " + r.OnePercentLowFps.ToString("F1", CultureInfo.InvariantCulture) + " FPS.");
            }
            catch (Exception ex) { Logger.LogWarning("Benchmark export failed: " + ex.Message); }
            finally { _runtime.Dispose(); _captureStarted = false; _finishing = false; }
        }
        private void SampleDiagnostics()
        {
            // Diagnostics must not disable otherwise working optimization features.
            try
            {
                DiagnosticSample sample = _runtime.Read(Math.Max(0, Time.realtimeSinceStartup - _recordStart));
                sample.Frame = _recorder.Count; sample.LoadedPieces = LoadedPieceCount(); sample.TrackedPieces = _pieces.Count;
                sample.DiscoveryChecked = DiscoveryChecked; sample.DiscoveryTotal = DiscoveryTotal; sample.PendingShadows = _pendingFirstApply; sample.DiscoveryReady = _discoveryReady ? 1 : 0;
                sample.Systems = _systems.Count; sample.Queue = _pending.Count; sample.GC0 = GC.CollectionCount(0) - _gcStart;
                Vector3 position = Player.m_localPlayer.transform.position; sample.X = position.x; sample.Y = position.y; sample.Z = position.z;
                _diagnostics.Add(sample);
            }
            catch (Exception ex) { Logger.LogWarning("Diagnostic sample unavailable: " + ex.GetType().Name); _nextDiagnostic = float.PositiveInfinity; }
        }
        private string EffectsCsv()
        {
            System.Text.StringBuilder b = new System.Text.StringBuilder("effect_name,roots_created,first_elapsed_s,allowlisted\n");
            List<string> keys = new List<string>(_observations.Keys); keys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < keys.Count; i++)
            { EffectObservation entry = _observations[keys[i]]; b.Append(DiagnosticLog.Quote(keys[i])).Append(',').Append(entry.Count).Append(',').Append(DiagnosticLog.Number(entry.FirstSeconds)).Append(',').Append(entry.Allowlisted ? "true" : "false").Append('\n'); }
            return b.ToString();
        }
        private void Notify(string message)
        {
            Logger.LogInfo(message);
            try { if (MessageHud.instance != null) MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message); } catch (Exception) { }
        }
        private void UpdateShortcutText()
        { _shortcutContent.text = "Toggle: " + _settings._toggleKey.Value.ToString(); }
        private void RefreshDiscoveryHud(bool scanning)
        {
            if (!_settings._showIndicator.Value || Time.realtimeSinceStartup < _nextHud) return;
            _nextHud = Time.realtimeSinceStartup + 0.25f;
            if (_failed) { _discoveryContent.text = "Discovery stopped"; _discoveryHint.text = "Check the performance log."; }
            else if (_settings._enabled.Value && _settings._noShadows.Value)
            {
                bool off = QualitySettings.shadows == ShadowQuality.Disable;
                _discoveryContent.text = off ? "Shadows: OFF (all real-time shadows)" : "Shadows: changed by another writer";
                _discoveryHint.text = off ? "No building scan or shadow application needed." : "External shadow setting preserved. Check the log.";
            }
            else if (!scanning) { _discoveryContent.text = "Discovery: PAUSED"; _discoveryHint.text = "Turn building optimization ON to scan."; }
            else if (_pieceSource == null) { _discoveryContent.text = "Discovery: UNAVAILABLE"; _discoveryHint.text = "Building registry could not be read."; }
            else
            {
                string count = DiscoveryChecked.ToString("N0", CultureInfo.InvariantCulture) + "/" + DiscoveryTotal.ToString("N0", CultureInfo.InvariantCulture);
                if (_sweep.Limited || _registrationLimited) { _discoveryContent.text = "Discovery: LIMIT (" + count + ")"; _discoveryHint.text = "Loaded build exceeds the tracking limit."; }
                else if (_discoveryReady) { _discoveryContent.text = "Discovery: READY (" + count + ")"; _discoveryHint.text = "Ready for this area. You can move on."; }
                else if (_sweep.Complete && _pendingFirstApply > 0) { _discoveryContent.text = "Applying shadows: " + _pendingFirstApply + " buildings left"; _discoveryHint.text = "Stay nearby until Discovery: READY."; }
                else { _discoveryContent.text = "Discovery: SCANNING (" + count + ")"; _discoveryHint.text = "Stay nearby while loaded buildings are checked."; }
            }
        }
        private void OnGUI()
        {
            if (_settings == null || Player.m_localPlayer == null) return;
            DrawFpsCounter();
            if (_indicatorUnavailable || !_settings._showIndicator.Value) return;
            if (Event.current.type != EventType.Repaint) { DragIndicator(); return; }
            Color previousColor = GUI.color; int previousDepth = GUI.depth;
            try
            {
                if (_indicatorStyle == null)
                {
                    _indicatorStyle = new GUIStyle(GUI.skin.label);
                    _indicatorStyle.fontStyle = FontStyle.Bold;
                    _indicatorStyle.alignment = TextAnchor.MiddleLeft;
                    _indicatorStyle.padding = new RectOffset(12, 6, 0, 0);
                    _indicatorStyle.richText = false;
                }
                bool usable = (_settings._noShadows.Value && QualitySettings.shadows == ShadowQuality.Disable) || (BuildingShadows && _pieceSource != null) || (_settings._particles.Value && _effectsHook);
                int state = PerformancePolicy.IndicatorState(_failed, _settings._enabled.Value, usable);
                _indicatorContent.text = state == 2 ? "Optimization: ERROR" : (state == 0 ? "Optimization: OFF" : (state == 3 ? "Optimization: NO FEATURES" : "Optimization: ON"));
                Color accent = state == 2 ? new Color(1f, 0.35f, 0.3f) : (state == 1 ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.78f, 0.35f));
                float scale = Mathf.Clamp(Screen.height / 1080f, 0.75f, 1.5f);
                bool compact = _settings._compactIndicator.Value;
                float width = Mathf.Min(Screen.width, (compact ? 230f : 420f) * scale), height = Mathf.Min(Screen.height, (compact ? 30f : 100f) * scale);
                float x = Mathf.Clamp(_settings._indicatorX.Value, 0f, Mathf.Max(0f, Screen.width - width));
                float y = Mathf.Clamp(_settings._indicatorY.Value, 0f, Mathf.Max(0f, Screen.height - height));
                Rect area = new Rect(x, y, width, height);
                _indicatorRect = area;
                GUI.depth = -100;
                GUI.color = new Color(0.025f, 0.035f, 0.045f, 0.94f);
                GUI.DrawTexture(area, Texture2D.whiteTexture);
                GUI.color = accent;
                GUI.DrawTexture(new Rect(x, y, 4f * scale, height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                _indicatorStyle.fontSize = Mathf.RoundToInt(16f * scale);
                _indicatorStyle.normal.textColor = accent;
                GUI.Label(new Rect(x, y, width, 30f * scale), _indicatorContent, _indicatorStyle);
                if (compact) return;
                _indicatorStyle.fontSize = Mathf.RoundToInt(13f * scale);
                _indicatorStyle.normal.textColor = _discoveryReady ? new Color(0.45f, 1f, 0.55f) : Color.white;
                GUI.Label(new Rect(x, y + 30f * scale, width, 22f * scale), _discoveryContent, _indicatorStyle);
                _indicatorStyle.fontSize = Mathf.RoundToInt(12f * scale);
                _indicatorStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
                GUI.Label(new Rect(x, y + 52f * scale, width, 22f * scale), _discoveryHint, _indicatorStyle);
                GUI.Label(new Rect(x, y + 74f * scale, width, 22f * scale), _shortcutContent, _indicatorStyle);
            }
            catch (Exception ex)
            { _indicatorUnavailable = true; Logger.LogWarning("Optimization indicator unavailable: " + ex.GetType().Name); }
            finally { GUI.color = previousColor; GUI.depth = previousDepth; }
        }
        // ---------------- 0.6.0 HUD: draggable indicator + FPS counter (user: the indicator covered the hotbar)
        private Rect _indicatorRect, _fpsRect;
        private int _dragging;   // 0 none, 1 indicator, 2 fps
        private Vector2 _dragOffset;
        private bool Drag(Rect area, int who, ConfigEntry<float> xs, ConfigEntry<float> ys)
        {
            Event e = Event.current;
            if (!Cursor.visible) { if (_dragging == who) _dragging = 0; return false; }
            if (e.type == EventType.MouseDown && e.button == 0 && _dragging == 0 && area.Contains(e.mousePosition)) { _dragging = who; _dragOffset = e.mousePosition - area.position; e.Use(); return true; }
            if (_dragging != who) return false;
            if (e.type == EventType.MouseDrag) { xs.Value = Mathf.Round(e.mousePosition.x - _dragOffset.x); ys.Value = Mathf.Round(e.mousePosition.y - _dragOffset.y); e.Use(); return true; }
            if (e.rawType == EventType.MouseUp) { _dragging = 0; try { Config.Save(); } catch (Exception) { } e.Use(); return true; }
            return false;
        }
        private void DragIndicator() { if (_indicatorRect.width > 0f) Drag(_indicatorRect, 1, _settings._indicatorX, _settings._indicatorY); }

        private readonly float[] _fpsRing = new float[512];
        private readonly float[] _fpsSort = new float[512];
        private int _fpsHead, _fpsFilled;
        private float _fpsNext, _fpsAccum; private int _fpsFrames;
        private int _fpsShown = -1, _lowShown = -1;
        private readonly GUIContent _fpsContent = new GUIContent();
        private GUIStyle _fpsStyle;
        private void SampleFps()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;
            _fpsRing[_fpsHead] = dt; _fpsHead = (_fpsHead + 1) % _fpsRing.Length; if (_fpsFilled < _fpsRing.Length) _fpsFilled++;
            _fpsAccum += dt; _fpsFrames++;
            if (Time.unscaledTime < _fpsNext || _fpsAccum <= 0f) return;
            _fpsNext = Time.unscaledTime + 0.5f;
            int fps = Mathf.RoundToInt(_fpsFrames / _fpsAccum); _fpsAccum = 0f; _fpsFrames = 0;
            Array.Copy(_fpsRing, _fpsSort, _fpsFilled); Array.Sort(_fpsSort, 0, _fpsFilled);
            int k = Math.Max(1, _fpsFilled / 100); float worst = 0f;
            for (int i = _fpsFilled - k; i < _fpsFilled; i++) worst += _fpsSort[i];
            int low = Mathf.RoundToInt(k / Mathf.Max(0.0001f, worst));
            if (fps != _fpsShown || low != _lowShown) { _fpsShown = fps; _lowShown = low; _fpsContent.text = "FPS " + fps + "   1% " + low; }
        }
        private void DrawFpsCounter()
        {
            if (!_settings._fpsCounter.Value || _fpsShown < 0) return;
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.75f, 1.5f);
            float w = 150f * scale, h = 26f * scale;
            float x = _settings._fpsX.Value < 0f ? Screen.width - w - 8f : Mathf.Clamp(_settings._fpsX.Value, 0f, Mathf.Max(0f, Screen.width - w));
            float y = Mathf.Clamp(_settings._fpsY.Value, 0f, Mathf.Max(0f, Screen.height - h));
            _fpsRect = new Rect(x, y, w, h);
            if (Event.current.type != EventType.Repaint) { Drag(_fpsRect, 2, _settings._fpsX, _settings._fpsY); return; }
            if (_fpsStyle == null) { _fpsStyle = new GUIStyle(GUI.skin.label); _fpsStyle.fontStyle = FontStyle.Bold; _fpsStyle.alignment = TextAnchor.MiddleCenter; _fpsStyle.richText = false; }
            _fpsStyle.fontSize = Mathf.RoundToInt(14f * scale);
            int target = 60;
            _fpsStyle.normal.textColor = _lowShown >= target ? new Color(0.45f, 1f, 0.55f) : (_fpsShown >= target ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.4f, 0.35f));
            Color c = GUI.color; GUI.color = new Color(0.025f, 0.035f, 0.045f, 0.85f);
            GUI.DrawTexture(_fpsRect, Texture2D.whiteTexture); GUI.color = c;
            GUI.Label(_fpsRect, _fpsContent, _fpsStyle);
        }

        private void OnDisable()
        {
            if (_recorder.Recording) FinishBenchmark("Plugin disabled");
            _runtime.Dispose();
            try { RestoreAll(); } catch (Exception ex) { Logger.LogWarning("Cleanup failed: " + ex.Message); } _wasShadows = false; _wasParticles = false;
        }
        private void OnDestroy()
        {
            Config.SettingChanged -= ConfigChanged;
            OnDisable(); if (_harmony != null) _harmony.UnpatchSelf(); if (Instance == this) Instance = null;
        }
    }
}
