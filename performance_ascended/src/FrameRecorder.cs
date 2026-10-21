using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace AethelbornPerformance
{
    public sealed class FrameReport
    {
        public int Frames, Over33ms, Over50ms, Over100ms;
        public double Seconds, AverageFps, OnePercentLowFps, P95ms, P99ms, WorstMs;
    }
    public sealed class FrameRecorder
    {
        private readonly float[] _frames, _work;
        private readonly int[] _gc;
        private readonly double[] _elapsed;
        private readonly long[] _mainThread, _renderThread, _allocations;
        private readonly FrameContext[] _context;
        public int Count { get; private set; }
        public bool Recording { get; private set; }
        public FrameRecorder(int capacity) { _frames = new float[Math.Max(2, capacity)]; _work = new float[_frames.Length]; _gc = new int[_frames.Length]; _elapsed = new double[_frames.Length]; _mainThread = new long[_frames.Length]; _renderThread = new long[_frames.Length]; _allocations = new long[_frames.Length]; _context = new FrameContext[_frames.Length]; }
        public void Start() { Count = 0; Recording = true; }
        public bool Add(float seconds) { return Add(seconds, 0, 0); }
        public bool Add(float seconds, int gcDelta, float previousWorkMs) { return Add(seconds, gcDelta, previousWorkMs, (Count == 0 ? 0 : _elapsed[Count - 1]) + seconds); }
        public bool Add(float seconds, int gcDelta, float previousWorkMs, double elapsedSeconds)
        {
            if (!Recording || !PerformancePolicy.Finite(seconds) || seconds <= 0f) return false;
            if (Count == _frames.Length) return false;
            _context[Count] = new FrameContext { LoadedPieces = -1, CreatedPieces = -1, DestroyedPieces = -1, DiscoveryReady = -1 }; _mainThread[Count] = -1; _renderThread[Count] = -1; _allocations[Count] = -1; _frames[Count] = seconds; _elapsed[Count] = double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) ? 0 : Math.Max(0, elapsedSeconds); _gc[Count] = Math.Max(0, gcDelta); _work[Count] = PerformancePolicy.Finite(previousWorkMs) ? Math.Max(0f, previousWorkMs) : 0f; Count++;
            return true;
        }
        public void SetLastEngineCounters(long mainThreadNs, long renderThreadNs, long allocatedBytes)
        { if (Count <= 0) return; _mainThread[Count - 1] = mainThreadNs; _renderThread[Count - 1] = renderThreadNs; _allocations[Count - 1] = allocatedBytes; }
        public void SetLastContext(FrameContext context) { if (Count > 0) _context[Count - 1] = context; }
        public string ContextSummary()
        {
            int ready = 0, scanning = 0, loading = 0, unloading = 0, stalledLoading = 0, stalledGC = 0;
            float discovery = 0, shadows = 0, diagnostics = 0;
            for (int i = 0; i < Count; i++)
            {
                FrameContext c = _context[i];
                if (c.DiscoveryReady == 1) ready++; else if (c.DiscoveryReady == 0) scanning++;
                if (c.CreatedPieces > 0) loading++; if (c.DestroyedPieces > 0) unloading++;
                if (_frames[i] >= 0.05f && (c.CreatedPieces > 0 || c.DestroyedPieces > 0)) stalledLoading++;
                if (_frames[i] >= 0.05f && _gc[i] > 0) stalledGC++;
                discovery = Math.Max(discovery, c.PreviousDiscoveryMs); shadows = Math.Max(shadows, c.PreviousShadowMs); diagnostics = Math.Max(diagnostics, c.PreviousDiagnosticsMs);
            }
            return "DiscoveryReadyFrames=" + ready + "; DiscoveryBusyFrames=" + scanning + "; FramesWithPieceAwake=" + loading + "; FramesWithPieceDestroy=" + unloading +
                "; FramesAtLeast50msWithPieceLifecycle=" + stalledLoading + "; FramesAtLeast50msWithGC0=" + stalledGC +
                "\nMaxPreviousDiscovery_ms=" + DiagnosticLog.Number(discovery) + "; MaxPreviousShadow_ms=" + DiagnosticLog.Number(shadows) + "; MaxPreviousDiagnostics_ms=" + DiagnosticLog.Number(diagnostics);
        }
        // 0.4.3: every frame >= 33 ms gets ONE cause, checked in this order (a frame with several events counts once)
        public string CauseSummary()
        {
            string[] names = { "Unload", "ZoneGeneration", "PieceLoad", "MemoryCleanup", "Other" };
            int[] count = new int[5]; float[] worst = new float[5];
            for (int i = 0; i < Count; i++)
            {
                if (_frames[i] < 0.033f) continue;
                FrameContext c = _context[i];
                int k = c.DestroyedPieces > 0 ? 0 : c.ZonesSpawned > 0 ? 1 : c.CreatedPieces > 0 ? 2 : _gc[i] > 0 ? 3 : 4;
                count[k]++; if (_frames[i] > worst[k]) worst[k] = _frames[i];
            }
            string s = "SlowFramesByCause (>=33 ms; count / worst ms):";
            for (int k = 0; k < 5; k++) s += " " + names[k] + "=" + count[k] + "/" + DiagnosticLog.Number(worst[k] * 1000f);
            return s;
        }
        public bool Full { get { return Count >= _frames.Length; } }
        public FrameReport Stop()
        {
            Recording = false;
            FrameReport r = new FrameReport(); r.Frames = Count;
            if (Count == 0) return r;
            float[] sorted = new float[Count]; Array.Copy(_frames, sorted, Count); Array.Sort(sorted);
            for (int i = 0; i < Count; i++)
            {
                float f = _frames[i]; r.Seconds += f;
                if (f > 0.033333334f) r.Over33ms++;
                if (f > 0.05f) r.Over50ms++;
                if (f > 0.1f) r.Over100ms++;
            }
            r.AverageFps = Count / r.Seconds;
            r.P95ms = sorted[Math.Max(0, (int)Math.Ceiling(Count * 0.95) - 1)] * 1000.0;
            r.P99ms = sorted[Math.Max(0, (int)Math.Ceiling(Count * 0.99) - 1)] * 1000.0;
            r.WorstMs = sorted[Count - 1] * 1000.0;
            int worstCount = Math.Max(1, (int)Math.Ceiling(Count * 0.01)); double worstSeconds = 0;
            for (int i = Count - worstCount; i < Count; i++) worstSeconds += sorted[i];
            r.OnePercentLowFps = worstCount / worstSeconds;
            return r;
        }
        public string Csv() { return Export(0f); }
        public string SpikesCsv(float thresholdMs) { return Export(Math.Max(1f, thresholdMs)); }
        private string Export(float minimumMs)
        {
            StringBuilder s = new StringBuilder(Count * 35 + 64); s.Append("frame,frame_time_ms,elapsed_s,gc0_delta,previous_plugin_update_ms,main_thread_last_frame_ns,render_thread_last_frame_ns,gc_allocated_last_frame_bytes,loaded_pieces,piece_awake_since_previous_update,piece_destroy_since_previous_update,discovery_checked,discovery_total,pending_discovery_pieces,discovery_ready,previous_discovery_ms,previous_shadow_ms,previous_diagnostics_ms,previous_entries_examined,previous_pieces_registered,previous_renderer_checks\n");
            for (int i = 0; i < Count; i++)
            {
                if (_frames[i] * 1000.0 < minimumMs) continue;
                s.Append(i + 1).Append(',').Append((_frames[i] * 1000.0).ToString("F4", CultureInfo.InvariantCulture)).Append(',').Append(_elapsed[i].ToString("F4", CultureInfo.InvariantCulture)).Append(',').Append(_gc[i]).Append(',').Append(_work[i].ToString("F4", CultureInfo.InvariantCulture)).Append(',').Append(_mainThread[i]).Append(',').Append(_renderThread[i]).Append(',').Append(_allocations[i]);
                FrameContext c = _context[i];
                s.Append(',').Append(c.LoadedPieces).Append(',').Append(c.CreatedPieces).Append(',').Append(c.DestroyedPieces).Append(',').Append(c.DiscoveryChecked).Append(',').Append(c.DiscoveryTotal).Append(',').Append(c.DiscoveryQueue).Append(',').Append(c.DiscoveryReady).Append(',').Append(DiagnosticLog.Number(c.PreviousDiscoveryMs)).Append(',').Append(DiagnosticLog.Number(c.PreviousShadowMs)).Append(',').Append(DiagnosticLog.Number(c.PreviousDiagnosticsMs)).Append(',').Append(c.PreviousExamined).Append(',').Append(c.PreviousRegistered).Append(',').Append(c.PreviousRendererChecks).Append('\n');
            }
            return s.ToString();
        }
    }
}
