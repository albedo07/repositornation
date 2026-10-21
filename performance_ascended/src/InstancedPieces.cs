using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace AethelbornPerformance
{
    // 0.6.0 Batched Building Drawing (idea: FPSDoctor PieceRenderer; PUP_FPS without its world-data changes).
    // Client-only, picture-only: repeated building meshes are drawn together with GPU instancing; the original renderers are
    // only hidden (forceRenderingOff) - colliders, support, damage, saves and other players are untouched.
    // Safety rules (all from assembly_valheim IL):
    //  - only materials Valheim itself marked instancing-capable (enableInstancing already true) are batched;
    //  - a piece with a per-object material change (MaterialMan: hover highlight, roof snow, Ashlands damage tint) goes back
    //    to normal drawing for good (hook: MaterialMan.RegisterRenderers, which every MaterialMan.SetValue calls first);
    //  - health-look swaps (WearNTear.SetHealthVisual) rebuild that piece; anything moving/animated/text is never batched;
    //  - every batched renderer is re-checked in the background; any error = everything returns to normal drawing.
    public static class InstancedPieces
    {
        public static bool Active;
        public static float DrawDistance = 150f;
        private const float Cell = 64f;
        private static bool _installed, _faulted;
        private static ManualLogSource _log;

        private sealed class Batch
        {
            public Mesh Mesh; public Material Material; public int SubMesh, Layer; public ShadowCastingMode Shadows; public bool Receive;
            public Vector3 Center;
            public Matrix4x4[] Matrices = new Matrix4x4[16]; public Slot[] Owners = new Slot[16]; public int Count;
        }
        private sealed class Slot { public Batch Batch; public int Index; public Renderer Renderer; }
        private sealed class Rec { public WearNTear Piece; public int Id; public List<Slot> Slots = new List<Slot>(); public List<Renderer> Hidden = new List<Renderer>(); public bool Excluded; }

        private static readonly Dictionary<long, Batch> _batchByKey = new Dictionary<long, Batch>();
        private static readonly List<Batch> _batches = new List<Batch>();
        private static readonly Dictionary<int, Rec> _recs = new Dictionary<int, Rec>();
        private static readonly List<Rec> _recList = new List<Rec>();
        private static readonly Queue<WearNTear> _pending = new Queue<WearNTear>();
        private static readonly Dictionary<int, int> _goToPiece = new Dictionary<int, int>();   // renderer GameObject id -> piece id
        private static readonly Matrix4x4[] _chunk = new Matrix4x4[1023];
        private static readonly List<Renderer> _tmpRenderers = new List<Renderer>();
        private static readonly List<Material> _tmpMats = new List<Material>();
        private static int _verifyCursor;
        private static bool _wasActive;
        private static float _nextPrune;
        private static readonly List<long> _deadKeys = new List<long>();
        // empty groups left behind by areas you walked away from
        private static void Prune()
        {
            for (int i = _batches.Count - 1; i >= 0; i--) if (_batches[i].Count == 0) { _batches[i] = _batches[_batches.Count - 1]; _batches.RemoveAt(_batches.Count - 1); }
            _deadKeys.Clear();
            foreach (KeyValuePair<long, Batch> kv in _batchByKey) if (kv.Value.Count == 0) _deadKeys.Add(kv.Key);
            for (int i = 0; i < _deadKeys.Count; i++) _batchByKey.Remove(_deadKeys[i]);
        }
        public static int DrawCallsLastFrame, InstancesLastFrame, Excluded;

        public static string Install(Harmony harmony, ManualLogSource log)
        {
            _log = log;
            try
            {
                System.Reflection.MethodInfo start = AccessTools.DeclaredMethod(typeof(WearNTear), "Start");
                System.Reflection.MethodInfo destroy = AccessTools.DeclaredMethod(typeof(WearNTear), "OnDestroy");
                System.Reflection.MethodInfo health = AccessTools.DeclaredMethod(typeof(WearNTear), "SetHealthVisual");
                System.Reflection.MethodInfo register = AccessTools.DeclaredMethod(typeof(MaterialMan), "RegisterRenderers", new Type[] { typeof(GameObject), typeof(bool) });
                if (start == null || destroy == null || health == null || register == null) return "unavailable (WearNTear/MaterialMan changed in this Valheim build)";
                harmony.Patch(start, postfix: new HarmonyMethod(typeof(InstancedPieces), "PieceStarted"));
                harmony.Patch(destroy, prefix: new HarmonyMethod(typeof(InstancedPieces), "PieceDestroyed"));
                harmony.Patch(health, postfix: new HarmonyMethod(typeof(InstancedPieces), "PieceLookChanged"));
                harmony.Patch(register, prefix: new HarmonyMethod(typeof(InstancedPieces), "MaterialChanged"));
                _installed = true;
                return "ready";
            }
            catch (Exception ex) { return "unavailable (" + ex.GetType().Name + ")"; }
        }

        private static void PieceStarted(WearNTear __instance) { if (_installed && !_faulted) _pending.Enqueue(__instance); }
        private static void PieceDestroyed(WearNTear __instance) { Rec r; if (_recs.TryGetValue(__instance.GetInstanceID(), out r)) Release(r, true); }
        private static void PieceLookChanged(WearNTear __instance)
        {
            Rec r;
            if (!_recs.TryGetValue(__instance.GetInstanceID(), out r) || r.Excluded) return;
            Release(r, true); _pending.Enqueue(__instance);   // health look swapped (new/worn/broken): rebuild from the visible renderers
        }
        // Every per-object material value (highlight, snow, ash tint, other mods) goes through here: that piece is drawn normally from now on.
        private static void MaterialChanged(GameObject gameObject)
        {
            if (gameObject == null || _recs.Count == 0) return;
            int pid;
            if (!_goToPiece.TryGetValue(gameObject.GetInstanceID(), out pid))
            {
                WearNTear w = gameObject.GetComponentInParent<WearNTear>();
                if (w == null) return;
                pid = w.GetInstanceID();
            }
            Rec r;
            if (_recs.TryGetValue(pid, out r) && !r.Excluded) { Release(r, false); r.Excluded = true; Excluded++; }
        }

        public static void Tick()
        {
            if (!_installed) return;
            if (!Active || _faulted) { if (_recList.Count > 0) ReleaseAll(); _pending.Clear(); _wasActive = false; return; }
            if (!_wasActive)
            {
                // turned on (or world entered with it on): queue every building piece already in the world
                _wasActive = true;
                List<WearNTear> all = WearNTear.GetAllInstances();
                if (all != null) for (int i = 0; i < all.Count; i++) if (all[i] != null) _pending.Enqueue(all[i]);
            }
            try
            {
                for (int n = 0; n < 24 && _pending.Count > 0; n++) Build(_pending.Dequeue());
                Verify(48);
                if (Time.unscaledTime > _nextPrune) { _nextPrune = Time.unscaledTime + 30f; Prune(); }
                Draw();
            }
            catch (Exception ex)
            {
                _faulted = true; ReleaseAll();
                if (_log != null) _log.LogError("Batched Building Drawing stopped; normal drawing restored. " + ex);
            }
        }

        private static bool Eligible(WearNTear w)
        {
            if (w.GetComponentInChildren<Animator>(true) != null || w.GetComponent<Door>() != null || w.GetComponent<Sign>() != null) return false;
            if (w.GetComponent<Fireplace>() != null || w.GetComponent<ItemStand>() != null || w.GetComponent<ArmorStand>() != null) return false;
            Rigidbody body = w.GetComponentInParent<Rigidbody>();
            if (body != null && !body.isKinematic) return false;
            if (w.GetComponent<ZSyncTransform>() != null) return false;
            ZNetView nview = w.GetComponent<ZNetView>();
            return nview != null && nview.IsValid();
        }

        private static void Build(WearNTear w)
        {
            if (w == null) return;
            int id = w.GetInstanceID();
            Rec r;
            if (_recs.TryGetValue(id, out r)) { if (r.Excluded) return; Release(r, true); }
            if (!Eligible(w)) return;
            r = new Rec(); r.Piece = w; r.Id = id;
            _tmpRenderers.Clear();
            w.GetComponentsInChildren<Renderer>(false, _tmpRenderers);
            for (int i = 0; i < _tmpRenderers.Count; i++)
            {
                MeshRenderer mr = _tmpRenderers[i] as MeshRenderer;
                if (mr == null || !mr.enabled || mr.forceRenderingOff || !mr.gameObject.activeInHierarchy) continue;
                if (mr.GetComponentInParent<LODGroup>() != null) continue;   // instancing would skip its LOD switching
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                Mesh mesh = mf.sharedMesh;
                _tmpMats.Clear(); mr.GetSharedMaterials(_tmpMats);
                if (_tmpMats.Count == 0 || _tmpMats.Count > mesh.subMeshCount) continue;
                bool ok = true;
                for (int m = 0; m < _tmpMats.Count; m++) if (_tmpMats[m] == null || !_tmpMats[m].enableInstancing) { ok = false; break; }
                if (!ok) continue;
                Vector3 p = mr.transform.position;
                int cx = Mathf.FloorToInt(p.x / Cell), cz = Mathf.FloorToInt(p.z / Cell);
                for (int m = 0; m < _tmpMats.Count; m++)
                {
                    Batch b = GetBatch(mesh, _tmpMats[m], m, mr.gameObject.layer, mr.shadowCastingMode, mr.receiveShadows, cx, cz);
                    Slot s = new Slot(); s.Batch = b; s.Renderer = mr; Add(b, s, mr.localToWorldMatrix);
                    r.Slots.Add(s);
                }
                mr.forceRenderingOff = true; r.Hidden.Add(mr);
                _goToPiece[mr.gameObject.GetInstanceID()] = id;
            }
            if (r.Slots.Count == 0) return;
            _goToPiece[w.gameObject.GetInstanceID()] = id;
            _recs[id] = r; _recList.Add(r);
        }

        private static Batch GetBatch(Mesh mesh, Material mat, int sub, int layer, ShadowCastingMode shadows, bool receive, int cx, int cz)
        {
            long key = ((long)mesh.GetInstanceID() * 73856093L) ^ ((long)mat.GetInstanceID() * 19349663L) ^ ((long)sub << 50) ^ ((long)layer << 44) ^ ((long)shadows << 42) ^ (receive ? 1L << 41 : 0L) ^ ((long)(cx & 0xFFFFF) << 20) ^ (long)(cz & 0xFFFFF);
            Batch b;
            if (_batchByKey.TryGetValue(key, out b) && b.Mesh == mesh && b.Material == mat && b.SubMesh == sub && b.Layer == layer) return b;
            b = new Batch(); b.Mesh = mesh; b.Material = mat; b.SubMesh = sub; b.Layer = layer; b.Shadows = shadows; b.Receive = receive;
            b.Center = new Vector3((cx + 0.5f) * Cell, 0f, (cz + 0.5f) * Cell);
            if (!_batchByKey.ContainsKey(key)) _batchByKey[key] = b;
            _batches.Add(b);
            return b;
        }
        private static void Add(Batch b, Slot s, Matrix4x4 m)
        {
            if (b.Count == b.Matrices.Length) { Array.Resize(ref b.Matrices, b.Count * 2); Array.Resize(ref b.Owners, b.Count * 2); }
            s.Index = b.Count; b.Matrices[b.Count] = m; b.Owners[b.Count] = s; b.Count++;
        }
        private static void Remove(Slot s)
        {
            Batch b = s.Batch; int last = b.Count - 1, i = s.Index;
            if (i < 0 || i > last || b.Owners[i] != s) return;
            b.Matrices[i] = b.Matrices[last]; b.Owners[i] = b.Owners[last]; b.Owners[i].Index = i;
            b.Owners[last] = null; b.Count--; s.Index = -1;
        }
        private static void Release(Rec r, bool forget)
        {
            for (int i = 0; i < r.Slots.Count; i++) Remove(r.Slots[i]);
            r.Slots.Clear();
            for (int i = 0; i < r.Hidden.Count; i++) { Renderer h = r.Hidden[i]; if (h != null) { h.forceRenderingOff = false; _goToPiece.Remove(h.gameObject.GetInstanceID()); } }
            r.Hidden.Clear();
            if (forget)
            {
                if (r.Excluded) Excluded--;
                _recs.Remove(r.Id); int k = _recList.IndexOf(r); if (k >= 0) { _recList[k] = _recList[_recList.Count - 1]; _recList.RemoveAt(_recList.Count - 1); }
            }
        }
        public static void ReleaseAll()
        {
            for (int i = 0; i < _recList.Count; i++)
                for (int h = 0; h < _recList[i].Hidden.Count; h++) if (_recList[i].Hidden[h] != null) _recList[i].Hidden[h].forceRenderingOff = false;
            _recList.Clear(); _recs.Clear(); _batches.Clear(); _batchByKey.Clear(); _goToPiece.Clear(); Excluded = 0;
        }

        // Background check: a batched renderer that was turned off, deactivated or destroyed by anything = rebuild that piece.
        private static void Verify(int budget)
        {
            for (int n = 0; n < budget && _recList.Count > 0; n++)
            {
                if (_verifyCursor >= _recList.Count) _verifyCursor = 0;
                Rec r = _recList[_verifyCursor++];
                if (r.Excluded) continue;
                if (r.Piece == null) { Release(r, true); continue; }
                for (int i = 0; i < r.Hidden.Count; i++)
                {
                    Renderer h = r.Hidden[i];
                    if (h == null || !h.enabled || !h.gameObject.activeInHierarchy) { WearNTear w = r.Piece; Release(r, true); _pending.Enqueue(w); break; }
                }
            }
        }

        private static void Draw()
        {
            DrawCallsLastFrame = 0; InstancesLastFrame = 0;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 eye = cam.transform.position;
            float reach = DrawDistance + Cell * 0.75f, reach2 = reach * reach;
            for (int i = 0; i < _batches.Count; i++)
            {
                Batch b = _batches[i];
                if (b.Count == 0) continue;
                float dx = b.Center.x - eye.x, dz = b.Center.z - eye.z;
                if (dx * dx + dz * dz > reach2) continue;
                for (int start = 0; start < b.Count; start += 1023)
                {
                    int n = Math.Min(1023, b.Count - start);
                    Matrix4x4[] arr = b.Matrices;
                    if (start > 0) { Array.Copy(b.Matrices, start, _chunk, 0, n); arr = _chunk; }
                    Graphics.DrawMeshInstanced(b.Mesh, b.SubMesh, b.Material, arr, n, null, b.Shadows, b.Receive, b.Layer, null, LightProbeUsage.BlendProbes);
                    DrawCallsLastFrame++; InstancesLastFrame += n;
                }
            }
        }

        public static string Report()
        {
            return "BatchedDrawing=" + (Active && !_faulted) + "; BatchedInstalled=" + _installed + "; BatchedFaulted=" + _faulted + "; BatchedPieces=" + _recList.Count + "; BatchedExcludedPieces=" + Excluded +
                "; BatchGroups=" + _batches.Count + "; BatchedDrawCallsLastFrame=" + DrawCallsLastFrame + "; BatchedInstancesLastFrame=" + InstancesLastFrame;
        }
    }
}
