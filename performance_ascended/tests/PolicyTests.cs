using System;
using System.Globalization;
using System.IO;
using BepInEx.Configuration;
using AethelbornPerformance;

public static class PolicyTests
{
    private static int passed;
    private static void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; }
    public static int Main(string[] args)
    {
        try
        {
            Check(!PerformancePolicy.BuildingShadows(true, true) && !PerformancePolicy.BuildingShadows(true, false), "All-shadows mode bypasses discovery regardless of old setting");
            Check(PerformancePolicy.BuildingShadows(false, true) && !PerformancePolicy.BuildingShadows(false, false), "Disabling all-shadows mode retains previous distant-shadow option");
            OwnedValue<UnityEngine.ShadowQuality> shadow = new OwnedValue<UnityEngine.ShadowQuality>(UnityEngine.ShadowQuality.All);
            Check(shadow.TryApply(UnityEngine.ShadowQuality.All, UnityEngine.ShadowQuality.Disable) && shadow.CanRestore(UnityEngine.ShadowQuality.Disable), "Restore original shadow quality on toggle OFF");
            shadow = new OwnedValue<UnityEngine.ShadowQuality>(UnityEngine.ShadowQuality.Disable);
            Check(!shadow.TryApply(UnityEngine.ShadowQuality.Disable, UnityEngine.ShadowQuality.Disable) && !shadow.CanRestore(UnityEngine.ShadowQuality.Disable), "Already disabled shadows need no write or restore");
            shadow = new OwnedValue<UnityEngine.ShadowQuality>(UnityEngine.ShadowQuality.All);
            shadow.TryApply(UnityEngine.ShadowQuality.All, UnityEngine.ShadowQuality.Disable);
            Check(!shadow.TryApply(UnityEngine.ShadowQuality.HardOnly, UnityEngine.ShadowQuality.Disable) && shadow.Contested && !shadow.CanRestore(UnityEngine.ShadowQuality.HardOnly), "External shadow setting is never overwritten on apply or restore");
            Check(PerformancePolicy.IndicatorState(false, false, true) == 0, "Disabled switch displays OFF");
            Check(PerformancePolicy.IndicatorState(false, true, true) == 1, "Enabled usable features display ON");
            Check(PerformancePolicy.IndicatorState(true, true, true) == 2, "Failed optimizer never displays ON");
            Check(PerformancePolicy.IndicatorState(true, false, false) == 2, "Failure remains visible even if switch is off");
            Check(PerformancePolicy.IndicatorState(false, true, false) == 3, "No usable feature never displays ON");
            Check(PerformancePolicy.IndicatorState(false, false, false) == 0, "Off with no usable feature remains OFF");
            Check(!PerformancePolicy.Far(false, 1600f, 40f, 5f), "Shadow threshold is exclusive");
            Check(PerformancePolicy.Far(false, 1681f, 40f, 5f), "Enter far at 41m");
            Check(PerformancePolicy.Far(true, 1296f, 40f, 5f), "Remain far at 36m");
            Check(!PerformancePolicy.Far(true, 1225f, 40f, 5f), "Restore at 35m");
            for (int count = 1; count <= 256; count++)
            {
                int cap = PerformancePolicy.ParticleCap(500, count, 1024, 96, false);
                Check(cap >= 1 && cap <= 96 && cap * count <= 1024, "Shared capacity bounded for " + count);
                Check(PerformancePolicy.ParticleCap(500, count, 1024, 96, true) <= cap, "Distance cap for " + count);
            }
            Check(PerformancePolicy.ParticleCap(4, 1, 1024, 96, false) == 4, "Never increase original capacity");
            Check(PerformancePolicy.RootName("vfx_troll_death(Clone)") == "vfx_troll_death", "Clone suffix");
            Check(!PerformancePolicy.Names("vfx_troll_death").Contains("vfx_troll_death_extra"), "Allowlist is exact");
            Check(PerformancePolicy.Names(" A, a, ,B ").Count == 2, "Allowlist parsing");
            OwnedValue<int> value = new OwnedValue<int>(100);
            Check(value.TryApply(100, 50), "Claim property");
            Check(value.TryApply(50, 25), "Change owned property");
            Check(value.CanRestore(25), "Restore owned property");
            value = new OwnedValue<int>(100); value.TryApply(100, 50);
            Check(!value.TryApply(70, 25) && value.Contested && !value.CanRestore(70), "External writer wins");
            value = new OwnedValue<int>(100); value.TryApply(100, 50);
            Check(!value.CanRestore(60), "Disable preserves external write");
            value = new OwnedValue<int>(100); value.TryApply(100, 50);
            Check(value.TryApply(50, 100) && !value.Owned, "Returning near releases ownership");
            Check(value.TryApply(100, 50), "Can reacquire after near/far transition");
            FrameRecorder frames = new FrameRecorder(1000);
            Check(frames.Stop().Frames == 0, "Empty recording");
            frames.Start(); frames.Add(float.NaN); frames.Add(float.PositiveInfinity); frames.Add(0f); frames.Add(-1f);
            Check(frames.Count == 0, "Invalid frame intervals rejected");
            for (int i = 0; i < 100; i++) frames.Add(0.01f);
            FrameReport report = frames.Stop();
            Check(report.Frames == 100 && Math.Abs(report.AverageFps - 100) < 0.01 && Math.Abs(report.OnePercentLowFps - 100) < 0.01, "Uniform frames");
            frames.Start(); for (int i = 0; i < 99; i++) frames.Add(0.01f); frames.Add(0.2f); report = frames.Stop();
            Check(Math.Abs(report.OnePercentLowFps - 5) < 0.01 && report.Over100ms == 1 && Math.Abs(report.P99ms - 10) < 0.01, "Slow tail statistics");
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            Check(frames.Csv().Contains("1,10.0000,0.0100,"), "CSV culture invariant");
            frames = new FrameRecorder(2); frames.Start(); frames.Add(0.01f); frames.Add(0.02f);
            Check(frames.Full && !frames.Add(0.03f) && frames.Stop().Frames == 2, "Fixed buffer bounds");
            frames.Start(); Check(frames.Count == 0 && frames.Recording, "Restart clears recording");
            frames = new FrameRecorder(10); frames.Start(); frames.Add(0.049f, 0, 1.5f, 2.0); frames.Add(0.051f, 1, 2.5f, 2.051); frames.Stop();
            Check(frames.SpikesCsv(50).Split('\n').Length == 3 && frames.SpikesCsv(50).Contains("2,51.0000,2.0510,1,2.5000"), "Spike export preserves GC/work/timestamp alignment");
            Check(!frames.SpikesCsv(50).Contains("1,49.0000"), "Spike threshold filters short frames");
            frames.Start(); frames.Add(0.05f, -10, float.NaN, double.NaN); frames.Stop();
            Check(frames.Csv().Contains(",0.0000,0,0.0000"), "Invalid diagnostic frame data normalized");
            DiagnosticLog diagnostic = new DiagnosticLog(2, 2);
            Check(diagnostic.Summary().Contains("SampledPeakManagedBytes=-1"), "Unavailable peaks remain unavailable");
            DiagnosticSample sample = new DiagnosticSample { Frame = 1, Seconds = 0.5, ManagedBytes = 123, ResidentBytes = -1, UnityUsedBytes = -1, DrawCalls = -1, CpuPercent = -1 };
            Check(diagnostic.Add(sample) && diagnostic.Add(sample) && !diagnostic.Add(sample) && diagnostic.DroppedSamples == 1, "Diagnostic samples bounded");
            Check(diagnostic.Mark(0.5, 1) && diagnostic.Mark(0.6, 2) && !diagnostic.Mark(0.7, 3) && diagnostic.DroppedMarkers == 1, "Markers bounded");
            Check(diagnostic.MarkersCsv().Contains("1,0.5000,1") && diagnostic.Csv().Contains("0.5000,1,123,-1"), "Culture invariant diagnostic output");
            Check(DiagnosticLog.Quote("a,\"b\"\n") == "\"a,\"\"b\"\" \"", "Effect CSV strings escaped");
            Check(diagnostic.Csv().Split('\n')[0].Split(',').Length == 27 && diagnostic.Csv().Split('\n')[1].Split(',').Length == 27, "Discovery timeline schema has matching headers and values");
            diagnostic.Reset(); Check(diagnostic.Count == 0 && diagnostic.MarkerCount == 0 && diagnostic.DroppedSamples == 0 && diagnostic.DroppedMarkers == 0, "Capture reset clears bounded logs");
            System.Collections.ArrayList source = new System.Collections.ArrayList();
            for (int i = 0; i < 20000; i++) source.Add(new object());
            DiscoverySweep<object> sweep = new DiscoverySweep<object>();
            sweep.Begin(source, 1, 50000);
            System.Collections.Generic.HashSet<object> visited = new System.Collections.Generic.HashSet<object>();
            long ticks = 0; Func<long> clock = delegate { ticks += 10; return ticks; };
            int first = sweep.Step(delegate(object o) { visited.Add(o); }, 256, 25, clock);
            Check(first == 3 && sweep.Examined == 3, "Soft deadline bounds hierarchy work");
            source.RemoveRange(0, 500);
            for (int i = 0; i < 400; i++) source.Add(new object());
            bool unique = true;
            while (!sweep.Complete) sweep.Step(delegate(object o) { if (!visited.Add(o)) unique = false; }, 256, long.MaxValue, clock);
            Check(unique && visited.Count == 20000, "Mutable live registry cannot cause snapshot skips");
            Check(!sweep.Ready(2, source.Count, 0, true), "Registry mutation invalidates Ready");
            sweep.Begin(source, 2, 50000); visited.Clear();
            while (!sweep.Complete) sweep.Step(delegate(object o) { visited.Add(o); }, 256, long.MaxValue, clock);
            Check(visited.Count == 19900 && sweep.Ready(2, 19900, 0, true), "Reconcile changed registry to Ready");
            Check(!sweep.Ready(2, 19900, 1, true), "Pending first shadow application blocks Ready");
            Check(!sweep.Ready(2, 19900, 0, false), "Loading quiet period blocks premature Ready");
            sweep.Begin(source, 3, 10);
            while (!sweep.Complete) sweep.Step(delegate(object o) { }, 256, long.MaxValue, clock);
            Check(sweep.Limited && !sweep.Ready(3, source.Count, 0, true), "Capacity limit never reports Ready");
            sweep.Clear(); Check(!sweep.Started && !sweep.Complete && !sweep.Ready(3, 0, 0, true), "Disable clears readiness");
            sweep.Begin(new System.Collections.ArrayList(), 4, 50000);
            Check(sweep.Ready(4, 0, 0, true), "Empty area can be Ready");
            Check(PerformancePolicy.ResidentBytes(0) == -1 && PerformancePolicy.ResidentBytes(-1) == -1 && PerformancePolicy.ResidentBytes(12345) == 12345, "Unusable resident memory is unavailable, never fake zero");
            frames.Start(); frames.Add(0.06f); frames.SetLastEngineCounters(12000000, 4000000, 8192); frames.Stop();
            Check(frames.SpikesCsv(50).Contains(",12000000,4000000,8192"), "Engine counters stay aligned with slow frames");
            Check(frames.Csv().Split('\n')[0].Split(',').Length == 21 && frames.Csv().Split('\n')[1].Split(',').Length == 21, "Frame schema includes aligned optional counters");
            DiscoveryQueue<object> queue = new DiscoveryQueue<object>(3);
            object a = new object(), b = new object(), c = new object(), d = new object();
            Check(queue.Ready(true, false, 0, true), "Empty delta queue is ready after initial discovery");
            Check(!queue.Ready(false, false, 0, true) && !queue.Ready(true, true, 0, true), "Initial scan and capacity limit block readiness");
            Check(queue.Add(a) && queue.Add(b) && queue.Add(c) && !queue.Add(d), "Lifecycle queue is bounded and overflow is explicit");
            Check(queue.Take() == a && queue.Add(d), "Lifecycle FIFO wraps without losing pending entries");
            Check(queue.Take() == b && queue.Take() == c && queue.Take() == d && queue.Take() == null, "Wrapped queue retains insertion order");
            Check(queue.Overflowed && !queue.Ready(true, false, 0, true), "Draining cannot conceal a lost lifecycle event");
            queue.Clear(); Check(queue.Total == 0 && queue.Examined == 0 && !queue.Overflowed, "Full snapshot resets lifecycle overflow and progress");
            Check(!queue.Ready(true, false, 1, true) && !queue.Ready(true, false, 0, false), "Pending shadows and loading quiet period block readiness");
            queue.Add(null); Check(queue.Count == 1 && !queue.Ready(true, false, 0, true), "Destroyed queued reference still requires examination");
            queue.Take(); Check(queue.Total == 1 && queue.Examined == 1 && queue.Ready(true, false, 0, true), "Destroyed entry drains normally");
            queue = new DiscoveryQueue<object>(512);
            for (int round = 0; round < 100; round++)
            {
                bool added = true, retained = true;
                for (int j = 0; j < 300; j++) if (!queue.Add(a)) added = false;
                for (int j = 0; j < 300; j++) if (queue.Take() != a) retained = false;
                Check(added && retained, "Repeated burst fits and drains bounded queue in order");
            }
            Check(queue.Total == 30000 && queue.Examined == 30000 && queue.Count == 0 && !queue.Overflowed, "Repeated load bursts avoid accumulating references");
            frames = new FrameRecorder(4); frames.Start();
            frames.Add(0.2f, 1, 0.8f, 0.2);
            frames.SetLastContext(new FrameContext { LoadedPieces = 20000, CreatedPieces = 250, DestroyedPieces = 50, DiscoveryChecked = 20100, DiscoveryTotal = 20200, DiscoveryQueue = 100, DiscoveryReady = 0, PreviousDiscoveryMs = 0.6f, PreviousShadowMs = 0.2f, PreviousDiagnosticsMs = 0.1f, PreviousExamined = 256, PreviousRegistered = 128, PreviousRendererChecks = 100 });
            frames.Add(0.01f); frames.SetLastContext(new FrameContext { DiscoveryReady = 1 }); frames.Stop();
            Check(frames.SpikesCsv(50).Contains(",20000,250,50,20100,20200,100,0,0.6000,0.2000,0.1000,256,128,100"), "Spike retains aligned loading and stage counters");
            Check(frames.ContextSummary().Contains("DiscoveryReadyFrames=1; DiscoveryBusyFrames=1") && frames.ContextSummary().Contains("FramesAtLeast50msWithPieceLifecycle=1; FramesAtLeast50msWithGC0=1"), "Summary separates readiness and overlapping stalls");
            frames.Start(); frames.Add(0.2f); frames.Stop();
            Check(frames.ContextSummary().Contains("DiscoveryReadyFrames=0; DiscoveryBusyFrames=0") && frames.Csv().Contains(",-1,-1,-1,"), "Restart clears old context and unavailable diagnostics stay unavailable");
            OrderedCallbacks ordered = new OrderedCallbacks();
            System.Collections.Generic.List<int> observed = new System.Collections.Generic.List<int>();
            Action ca = delegate { observed.Add(1); }, cb = delegate { observed.Add(2); }, cc = delegate { observed.Add(3); };
            ordered.Add(ca); ordered.Add(cb); ordered.Add(ca); ordered.Add(cc);
            Check(ordered.Remove(ca), "Remove duplicate native handler"); ordered.Invoke();
            Check(string.Join(",", observed) == "1,2,3" && ordered.Count == 3, "Callback removal targets last duplicate without reordering others");
            Check(!ordered.Remove(delegate { }) && !ordered.Remove(null), "Absent/native-null callback removal is a no-op");
            observed.Clear(); OrderedCallbacks.Combine(ordered.Snapshot())();
            Check(string.Join(",", observed) == "1,2,3", "Native restoration retains callback order");
            ordered = new OrderedCallbacks(); observed.Clear(); bool mutated = false;
            Action removeDuringInvoke = delegate { observed.Add(1); if (!mutated) { mutated = true; ordered.Remove(cb); ordered.Add(cc); } };
            ordered.Add(removeDuringInvoke); ordered.Add(cb); ordered.Invoke();
            Check(string.Join(",", observed) == "1,2", "In-flight invocation still calls removed handler and excludes newly added handler");
            observed.Clear(); ordered.Invoke(); Check(string.Join(",", observed) == "1,3", "Next invocation sees callback changes");
            ordered = new OrderedCallbacks(); observed.Clear(); bool nested = false;
            Action nestedInvoke = delegate { observed.Add(1); if (!nested) { nested = true; ordered.Remove(cb); ordered.Add(cc); ordered.Invoke(); } };
            ordered.Add(nestedInvoke); ordered.Add(cb); ordered.Invoke();
            Check(string.Join(",", observed) == "1,1,3,2", "Nested invocation sees new state while outer snapshot remains unchanged");
            ordered = new OrderedCallbacks(); observed.Clear();
            Action throws = delegate { ordered.Remove(cb); throw new InvalidOperationException("expected"); };
            ordered.Add(throws); ordered.Add(cb); bool threw = false;
            try { ordered.Invoke(); } catch (InvalidOperationException) { threw = true; }
            Check(threw && observed.Count == 0 && ordered.Count == 1, "Exceptions stop invocation and release retired bookkeeping");
            ordered.Remove(throws); ordered.Add(cc); ordered.Invoke();
            Check(string.Join(",", observed) == "3", "Registry remains usable after a throwing handler");
            ordered = new OrderedCallbacks(); observed.Clear(); Action native = null;
            Action[] callbacks = new Action[] { ca, cb, cc }; Random random = new Random(127);
            bool sameOrder = true;
            for (int operation = 0; operation < 2000; operation++)
            {
                Action callback = callbacks[random.Next(callbacks.Length)];
                if (random.Next(2) == 0) { native += callback; ordered.Add(callback); } else { native -= callback; ordered.Remove(callback); }
                observed.Clear(); if (native != null) native(); string expected = string.Join(",", observed);
                observed.Clear(); ordered.Invoke(); if (expected != string.Join(",", observed)) sameOrder = false;
            }
            Check(sameOrder, "2000 deterministic duplicate add/remove operations match native invocation order");
            Action[] many = new Action[2048];
            for (int i = 0; i < many.Length; i++) { int identifier = i; many[i] = delegate { if (identifier < 0) throw new Exception(); }; }
            native = OrderedCallbacks.Combine(many); long beforeNative = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < many.Length; i++) native -= many[i];
            long nativeAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeNative;
            ordered = new OrderedCallbacks(); for (int i = 0; i < many.Length; i++) ordered.Add(many[i]);
            long beforeOrdered = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < many.Length; i++) ordered.Remove(many[i]);
            long orderedAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeOrdered;
            Check(ordered.Count == 0 && orderedAllocated < nativeAllocated / 10, "Bulk removal avoids native multicast-array allocation growth");
            Console.WriteLine("HOST allocation comparison only (.NET6, not Valheim/Mono FPS): 2048 removals native=" + nativeAllocated + " bytes; ordered=" + orderedAllocated + " bytes.");
            string path = Path.GetFullPath(args[0]);
            ConfigFile config = new ConfigFile(path, false);
            PerformanceSettings settings = new PerformanceSettings(config);
            Check(config.Count == 28 && settings._enabled.Value && settings._supportCache.Value && settings._noShadows.Value && settings._shadowMs.Value == 0.5f, "Actual BepInEx production bindings");
            Check(settings._showIndicator.Value && settings._indicatorX.Value == 12f && settings._indicatorY.Value == 12f, "HUD visible by default with valid position");
            settings._shadowMs.Value = -10f; Check(settings._shadowMs.Value == 0.1f, "Shadow time limit clamps lower");
            settings._shadowMs.Value = 99f; Check(settings._shadowMs.Value == 2f, "Shadow time limit clamps upper");
            settings._shadowMs.Value = 0.5f;
            settings._shadowDistance.Value = -10f; Check(settings._shadowDistance.Value == 10f, "Config range clamps lower");
            settings._particleMax.Value = 10000; Check(settings._particleMax.Value == 512, "Config range clamps upper");
            settings._shadowDistance.Value = 40f; settings._particleMax.Value = 96;
            settings._supportCache.Value = false; settings._noShadows.Value = false;
            settings._scenario.Value = "base_walk"; settings._spikeThreshold.Value = -1;
            Check(settings._spikeThreshold.Value == 16f && settings._diagnostics.Value && settings._renderCounters.Value, "Pass 2 settings ranges and defaults");
            settings._spikeThreshold.Value = 50f;
            config.Save(); ConfigFile reloaded = new ConfigFile(path, false); PerformanceSettings roundtrip = new PerformanceSettings(reloaded);
            Check(!roundtrip._supportCache.Value && !roundtrip._noShadows.Value && roundtrip._scenario.Value == "base_walk" && roundtrip._shadowDistance.Value == 40f && roundtrip._particleMax.Value == 96 && roundtrip._allowList.Value.Contains("vfx_troll_death"), "Config serialization round trip");
            roundtrip._supportCache.Value = true; roundtrip._noShadows.Value = true; roundtrip._scenario.Value = "unlabeled"; reloaded.Save();
            string upgradePath = path + ".upgrade";
            File.WriteAllText(upgradePath, "[General]\nEnabled = false\n[Structures]\nShadow Distance = 75\n");
            PerformanceSettings upgrade = new PerformanceSettings(new ConfigFile(upgradePath, false));
            Check(!upgrade._enabled.Value && upgrade._supportCache.Value && upgrade._noShadows.Value && upgrade._shadowDistance.Value == 75f && upgrade._diagnostics.Value && upgrade._scenario.Value == "unlabeled", "Existing pass 1 settings preserved while new defaults bind");
            File.WriteAllText(upgradePath, "[Structures]\nDiscovery Budget = 2\n[General]\nToggle Hotkey = F11 + LeftControl + LeftAlt\n");
            ConfigFile migrationFile = new ConfigFile(upgradePath, false); PerformanceSettings migration = new PerformanceSettings(migrationFile);
            Check(migration._piecesPerFrame.Value == 256 && migration._toggleKey.Value.MainKey == UnityEngine.KeyCode.F6 && migration._configRevision.Value == 1, "Old defaults migrate once");
            migration._piecesPerFrame.Value = 2; migrationFile.Save();
            PerformanceSettings intentional = new PerformanceSettings(new ConfigFile(upgradePath, false));
            Check(intentional._piecesPerFrame.Value == 2, "Intentional discovery override retained after migration");
            File.WriteAllText(upgradePath, "[Structures]\nDiscovery Budget = 5\n[General]\nToggle Hotkey = F4 + LeftControl\n");
            PerformanceSettings custom = new PerformanceSettings(new ConfigFile(upgradePath, false));
            Check(custom._piecesPerFrame.Value == 5 && custom._toggleKey.Value.MainKey == UnityEngine.KeyCode.F4, "Custom shortcut and budgets preserved");
            File.Delete(upgradePath);
            Console.WriteLine("PASS: " + passed + " policy, statistics, ownership and real BepInEx configuration assertions.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
