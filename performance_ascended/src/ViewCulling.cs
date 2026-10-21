using System;
using UnityEngine;

namespace AethelbornPerformance
{
    // 0.4.2 Big Bases: the captures are CPU-bound (median 11-13 ms, ~2,000 draw calls, 0.5-1M triangles at 12-15k pieces).
    // Every building piece is its own draw call. Unity can skip whole layers past a distance per camera (layerCullDistances)
    // with no per-object work: pieces far away are simply not drawn (they still exist, collide and simulate).
    // Owned like everything else: if Valheim or another mod changes the camera's distances or the LOD bias, theirs wins.
    public sealed class ViewCulling
    {
        private Camera _camera;
        private float[] _original, _applied;
        private bool _originalSpherical, _contested;
        private OwnedValue<float> _lodBias;
        private float _nextCheck;
        private int _piece = -2, _pieceNonSolid, _item, _small;
        public string State = "off";

        public void Update(bool on, float pieceM, float nonSolidM, float smallM, float lodMax)
        {
            if (!on) { Restore(); State = "off"; return; }
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 1f;
            if (_piece == -2)
            {
                _piece = LayerMask.NameToLayer("piece"); _pieceNonSolid = LayerMask.NameToLayer("piece_nonsolid");
                _item = LayerMask.NameToLayer("item"); _small = LayerMask.NameToLayer("Default_small");
            }
            Camera cam = Camera.main;
            if (cam != _camera) { Restore(); _camera = cam; _contested = false; }
            if (_camera != null && !_contested)
            {
                float[] current = _camera.layerCullDistances;
                if (_original == null) { _original = (float[])current.Clone(); _originalSpherical = _camera.layerCullSpherical; }
                else if (_applied != null && !Same(current, _applied)) { _contested = true; State = "yielded to another writer"; }
                if (!_contested)
                {
                    float[] want = (float[])_original.Clone();
                    Cap(want, _piece, pieceM); Cap(want, _pieceNonSolid, nonSolidM); Cap(want, _item, smallM); Cap(want, _small, smallM);
                    if (_applied == null || !Same(want, _applied)) { _camera.layerCullDistances = want; _applied = want; }
                    _camera.layerCullSpherical = true;   // distance from the camera, not from its view plane (no edge popping when turning)
                    State = "on";
                }
            }
            if (_lodBias == null) _lodBias = new OwnedValue<float>(QualitySettings.lodBias);
            float bias = Mathf.Min(_lodBias.Original, lodMax);
            if (_lodBias.TryApply(QualitySettings.lodBias, bias)) QualitySettings.lodBias = bias;
        }

        // 0 means "use the far clip plane" in Unity; only ever lower a distance, never raise one
        private static void Cap(float[] a, int layer, float metres)
        {
            if (layer < 0 || layer >= a.Length || metres <= 0f) return;
            if (a[layer] <= 0f || a[layer] > metres) a[layer] = metres;
        }
        private static bool Same(float[] a, float[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (Math.Abs(a[i] - b[i]) > 0.001f) return false;
            return true;
        }

        public void Restore()
        {
            if (_camera != null && _original != null && !_contested && _applied != null && Same(_camera.layerCullDistances, _applied))
            { _camera.layerCullDistances = _original; _camera.layerCullSpherical = _originalSpherical; }
            _original = null; _applied = null;
            if (_lodBias != null && _lodBias.CanRestore(QualitySettings.lodBias)) QualitySettings.lodBias = _lodBias.Original;
            _lodBias = null;
        }

        public string Report()
        {
            return "ViewCulling=" + State + "; LodBias=" + QualitySettings.lodBias.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                "; GCIncremental=" + UnityEngine.Scripting.GarbageCollector.isIncremental + "; GCSlice_ns=" + UnityEngine.Scripting.GarbageCollector.incrementalTimeSliceNanoseconds;
        }
    }
}
