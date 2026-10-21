using System;
using System.Diagnostics;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace AethelbornPerformance
{
    // Only active during benchmarks. Native counters are optional and independently disposable.
    public sealed class RuntimeDiagnostics : IDisposable
    {
        private readonly ProfilerRecorder[] _recorders = new ProfilerRecorder[8];
        private static readonly string[] Names = { "Draw Calls Count", "Batches Count", "Triangles Count", "Vertices Count", "SetPass Calls Count", "Main Thread", "Render Thread", "GC Allocated In Frame" };
        private Process _process;
        private bool _nativeMemory = true;
        public string ResidentSource { get; private set; }
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MemoryCounters
        {
            public uint Size, PageFaultCount;
            public UIntPtr PeakWorkingSet, WorkingSet, PeakPagedPool, PagedPool, PeakNonPagedPool, NonPagedPool, Pagefile, PeakPagefile;
        }
        private static readonly int MemoryCountersSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MemoryCounters));
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();
        [System.Runtime.InteropServices.DllImport("psapi.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetProcessMemoryInfo(IntPtr process, ref MemoryCounters counters, uint size);
        private long NativeResident()
        {
            if (!_nativeMemory || Environment.OSVersion.Platform != PlatformID.Win32NT) return -1;
            try
            {
                MemoryCounters counters = new MemoryCounters(); counters.Size = (uint)MemoryCountersSize;
                if (GetProcessMemoryInfo(GetCurrentProcess(), ref counters, counters.Size)) return PerformancePolicy.ResidentBytes((long)counters.WorkingSet.ToUInt64());
                _nativeMemory = false;
            }
            catch (Exception) { _nativeMemory = false; }
            return -1;
        }
        private double _lastCpu, _lastSeconds;
        private bool _cpuBaseline, _memoryAvailable = true;
        public void Begin(bool renderCounters)
        {
            Dispose(); _cpuBaseline = false; _memoryAvailable = true; _nativeMemory = true; ResidentSource = "unavailable";
            try { _process = Process.GetCurrentProcess(); } catch (Exception) { _process = null; }
            if (!renderCounters) return;
            for (int i = 0; i < _recorders.Length; i++)
                try { _recorders[i] = ProfilerRecorder.StartNew(i < 5 ? ProfilerCategory.Render : (i < 7 ? ProfilerCategory.Internal : ProfilerCategory.Memory), Names[i], 1); } catch (Exception) { }
        }
        private long Counter(int i)
        { try { return _recorders[i].Valid && _recorders[i].Count > 0 ? _recorders[i].LastValue : -1; } catch (Exception) { return -1; } }
        public DiagnosticSample Read(double seconds)
        {
            DiagnosticSample s = new DiagnosticSample(); s.Seconds = seconds; ResidentSource = "unavailable";
            s.ManagedBytes = GC.GetTotalMemory(false); s.UnityUsedBytes = -1; s.UnityReservedBytes = -1; s.ResidentBytes = -1; s.CpuPercent = -1;
            if (_memoryAvailable)
            {
                try { long used = Profiler.GetTotalAllocatedMemoryLong(), reserved = Profiler.GetTotalReservedMemoryLong(); s.UnityUsedBytes = used > 0 ? used : -1; s.UnityReservedBytes = reserved > 0 ? reserved : -1; }
                catch (Exception) { _memoryAvailable = false; }
            }
            if (_process != null)
            {
                try
                {
                    _process.Refresh(); s.ResidentBytes = PerformancePolicy.ResidentBytes(_process.WorkingSet64);
                    if (s.ResidentBytes > 0) ResidentSource = "managed working set";
                    double cpu = _process.TotalProcessorTime.TotalSeconds;
                    if (_cpuBaseline && seconds > _lastSeconds) s.CpuPercent = Math.Max(0, Math.Min(100, (cpu - _lastCpu) * 100 / ((seconds - _lastSeconds) * Math.Max(1, Environment.ProcessorCount))));
                    _lastCpu = cpu; _lastSeconds = seconds; _cpuBaseline = true;
                }
                catch (Exception) { _process.Dispose(); _process = null; }
            }
            if (s.ResidentBytes <= 0)
            {
                s.ResidentBytes = NativeResident();
                if (s.ResidentBytes > 0) ResidentSource = "Windows GetProcessMemoryInfo";
            }
            s.MainThreadNs = Counter(5); s.RenderThreadNs = Counter(6); s.GCAllocatedBytes = Counter(7);
            s.DrawCalls = Counter(0); s.Batches = Counter(1); s.Triangles = Counter(2); s.Vertices = Counter(3); s.SetPass = Counter(4);
            return s;
        }
        public void ReadFrameCounters(out long mainThreadNs, out long renderThreadNs, out long gcAllocatedBytes)
        { mainThreadNs = Counter(5); renderThreadNs = Counter(6); gcAllocatedBytes = Counter(7); }
        public void Dispose()
        {
            for (int i = 0; i < _recorders.Length; i++)
            { try { _recorders[i].Dispose(); } catch (Exception) { } _recorders[i] = default(ProfilerRecorder); }
            if (_process != null) { _process.Dispose(); _process = null; }
        }
    }
}
