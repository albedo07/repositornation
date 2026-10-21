using System;
using System.Globalization;
using System.Text;

namespace AethelbornPerformance
{
    public struct FrameContext
    {
        // Loading events/count are sampled at this Update; work fields describe the previous completed Update.
        public int LoadedPieces, CreatedPieces, DestroyedPieces, DiscoveryChecked, DiscoveryTotal, DiscoveryQueue, DiscoveryReady;
        public int PreviousExamined, PreviousRegistered, PreviousRendererChecks;
        public float PreviousDiscoveryMs, PreviousShadowMs, PreviousDiagnosticsMs;
    }
    public struct DiagnosticSample
    {
        public int Frame, LoadedPieces, TrackedPieces, Systems, Queue, GC0, DiscoveryChecked, DiscoveryTotal, PendingShadows, DiscoveryReady;
        public double Seconds, X, Y, Z, CpuPercent;
        public long ManagedBytes, UnityUsedBytes, UnityReservedBytes, ResidentBytes;
        public long DrawCalls, Batches, Triangles, Vertices, SetPass, MainThreadNs, RenderThreadNs, GCAllocatedBytes;
    }
    public struct DiagnosticMarker { public int Number, Frame; public double Seconds; }
    public sealed class DiagnosticLog
    {
        private readonly DiagnosticSample[] _samples;
        private readonly DiagnosticMarker[] _markers;
        public int Count { get; private set; }
        public int MarkerCount { get; private set; }
        public int DroppedSamples { get; private set; }
        public int DroppedMarkers { get; private set; }
        public DiagnosticLog(int sampleCapacity, int markerCapacity)
        { _samples = new DiagnosticSample[Math.Max(1, sampleCapacity)]; _markers = new DiagnosticMarker[Math.Max(1, markerCapacity)]; }
        public void Reset() { Count = 0; MarkerCount = 0; DroppedSamples = 0; DroppedMarkers = 0; }
        public bool Add(DiagnosticSample sample)
        { if (Count == _samples.Length) { DroppedSamples++; return false; } _samples[Count++] = sample; return true; }
        public bool Mark(double seconds, int frame)
        {
            if (MarkerCount == _markers.Length) { DroppedMarkers++; return false; }
            _markers[MarkerCount] = new DiagnosticMarker { Number = MarkerCount + 1, Frame = frame, Seconds = Math.Max(0, seconds) }; MarkerCount++; return true;
        }
        public static string Number(double n) { return n.ToString("F4", CultureInfo.InvariantCulture); }
        public static string Quote(string text) { return "\"" + (text ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\""; }
        public string Csv()
        {
            StringBuilder b = new StringBuilder();
            b.Append("elapsed_s,frame,managed_bytes,unity_used_bytes,unity_reserved_bytes,resident_bytes,process_cpu_percent_all_cores,draw_calls_last_frame,batches_last_frame,triangles_last_frame,vertices_last_frame,setpass_last_frame,loaded_pieces,tracked_pieces,tracked_cosmetic_systems,pending_effect_roots,gc0_since_capture_start,x,y,z,discovery_checked,discovery_total,pending_shadow_pieces,discovery_ready,main_thread_last_frame_ns,render_thread_last_frame_ns,gc_allocated_last_frame_bytes\n");
            for (int i = 0; i < Count; i++)
            {
                DiagnosticSample s = _samples[i];
                b.Append(Number(s.Seconds)).Append(',').Append(s.Frame).Append(',').Append(s.ManagedBytes).Append(',').Append(s.UnityUsedBytes).Append(',').Append(s.UnityReservedBytes).Append(',').Append(s.ResidentBytes).Append(',').Append(Number(s.CpuPercent)).Append(',').Append(s.DrawCalls).Append(',').Append(s.Batches).Append(',').Append(s.Triangles).Append(',').Append(s.Vertices).Append(',').Append(s.SetPass).Append(',').Append(s.LoadedPieces).Append(',').Append(s.TrackedPieces).Append(',').Append(s.Systems).Append(',').Append(s.Queue).Append(',').Append(s.GC0).Append(',').Append(Number(s.X)).Append(',').Append(Number(s.Y)).Append(',').Append(Number(s.Z)).Append(',').Append(s.DiscoveryChecked).Append(',').Append(s.DiscoveryTotal).Append(',').Append(s.PendingShadows).Append(',').Append(s.DiscoveryReady).Append(',').Append(s.MainThreadNs).Append(',').Append(s.RenderThreadNs).Append(',').Append(s.GCAllocatedBytes).Append('\n');
            }
            return b.ToString();
        }
        public string MarkersCsv()
        {
            StringBuilder b = new StringBuilder("marker,elapsed_s,frame\n");
            for (int i = 0; i < MarkerCount; i++) { DiagnosticMarker m = _markers[i]; b.Append(m.Number).Append(',').Append(Number(m.Seconds)).Append(',').Append(m.Frame).Append('\n'); }
            return b.ToString();
        }
        public string Summary()
        {
            long managed = -1, resident = -1, used = -1, draws = -1; double cpu = -1;
            for (int i = 0; i < Count; i++) { DiagnosticSample s = _samples[i]; managed = Math.Max(managed, s.ManagedBytes); resident = Math.Max(resident, s.ResidentBytes); used = Math.Max(used, s.UnityUsedBytes); draws = Math.Max(draws, s.DrawCalls); cpu = Math.Max(cpu, s.CpuPercent); }
            return "DiagnosticSamples=" + Count + "; DroppedSamples=" + DroppedSamples + "; Markers=" + MarkerCount + "; DroppedMarkers=" + DroppedMarkers + "\nSampledPeakManagedBytes=" + managed + "; SampledPeakResidentBytes=" + resident + "; SampledPeakUnityUsedBytes=" + used + "; SampledPeakDrawCalls=" + draws + "; SampledPeakProcessCPUPercentAllCores=" + Number(cpu);
        }
    }
}
