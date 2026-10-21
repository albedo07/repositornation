using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AethelbornPerformance
{
    // 0.4.0: Valheim already creates at most 10 objects per tick (ZNetScene.m_maxCreatedPerFrame) but removes EVERY
    // out-of-range object in a single tick. Leaving a base area destroyed ~8,500 pieces in one frame (300-475 ms in the
    // supplied captures). This prefix is the same algorithm as ZNetScene.RemoveObjects (read from the supplied
    // assembly_valheim IL) with one difference: at most N removals per tick. Whatever is left over is simply found again
    // on the next tick (the scan is redone every tick, as in vanilla), so an object you walk back to is kept, not removed.
    public static class UnloadBudget
    {
        public static bool Active;
        public static int PerTick = 150;
        private static bool _installed, _faulted;
        private static ManualLogSource _log;
        private static Func<ZNetScene, Dictionary<ZDO, ZNetView>> _instances;   // cached private-field getters (C# 5: no ref returns)
        private static Func<ZNetScene, List<ZNetView>> _tempRemoved;
        // run metrics (reset at capture start)
        public static long RunRemoved, RunDeferredTicks; public static int MaxBacklog, MaxRemovedInTick;

        public static string Install(Harmony harmony, ManualLogSource log)
        {
            _log = log;
            try
            {
                System.Reflection.MethodInfo target = AccessTools.DeclaredMethod(typeof(ZNetScene), "RemoveObjects", new Type[] { typeof(List<ZDO>), typeof(List<ZDO>) });
                if (target == null) return "unavailable (RemoveObjects not found in this Valheim build)";
                Patches existing = Harmony.GetPatchInfo(target);
                if (existing != null && existing.Owners.Count > 0) return "disabled: another mod patches ZNetScene.RemoveObjects (" + string.Join(",", new List<string>(existing.Owners).ToArray()) + ")";
                _instances = Getter<Dictionary<ZDO, ZNetView>>("m_instances");
                _tempRemoved = Getter<List<ZNetView>>("m_tempRemoved");
                if (_instances == null || _tempRemoved == null) return "unavailable (ZNetScene fields changed in this Valheim build)";
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(UnloadBudget), "Prefix"));
                _installed = true;
                return "ready";
            }
            catch (Exception ex) { return "unavailable (" + ex.GetType().Name + ")"; }
        }

        private static Func<ZNetScene, T> Getter<T>(string name) where T : class
        {
            System.Reflection.FieldInfo field = AccessTools.Field(typeof(ZNetScene), name);
            if (field == null || field.FieldType != typeof(T)) return null;
            System.Reflection.Emit.DynamicMethod m = new System.Reflection.Emit.DynamicMethod("PA_Get_" + name, typeof(T), new Type[] { typeof(ZNetScene) }, typeof(ZNetScene), true);
            System.Reflection.Emit.ILGenerator il = m.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); il.Emit(System.Reflection.Emit.OpCodes.Ldfld, field); il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (Func<ZNetScene, T>)m.CreateDelegate(typeof(Func<ZNetScene, T>));
        }

        public static void ResetMetrics() { RunRemoved = 0; RunDeferredTicks = 0; MaxBacklog = 0; MaxRemovedInTick = 0; }

        public static string Report()
        {
            return "SpreadUnloadInstalled=" + _installed + "; SpreadUnloadActiveAtEnd=" + (Active && !_faulted) + "; UnloadPerTick=" + PerTick + "; SpreadUnloadFaulted=" + _faulted +
                "\nRunUnloadedObjects=" + RunRemoved + "; RunDeferredUnloadTicks=" + RunDeferredTicks + "; MaxUnloadBacklog=" + MaxBacklog + "; MaxUnloadedInOneTick=" + MaxRemovedInTick;
        }

        // Returns false = vanilla skipped (we did its work); true = vanilla runs unchanged.
        private static bool Prefix(ZNetScene __instance, List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
        {
            if (!Active || _faulted) return true;
            try
            {
                byte mark = (byte)(Time.frameCount & 0xFF);
                for (int i = 0; i < currentNearObjects.Count; i++) currentNearObjects[i].TempRemoveEarmark = mark;
                for (int i = 0; i < currentDistantObjects.Count; i++) currentDistantObjects[i].TempRemoveEarmark = mark;
                Dictionary<ZDO, ZNetView> instances = _instances(__instance);
                List<ZNetView> removed = _tempRemoved(__instance);
                removed.Clear();
                int limit = Math.Max(1, PerTick), backlog = 0;
                foreach (ZNetView view in instances.Values)
                {
                    if (view.GetZDO().TempRemoveEarmark == mark) continue;
                    if (removed.Count < limit) removed.Add(view); else backlog++;
                }
                for (int i = 0; i < removed.Count; i++)
                {
                    ZNetView view = removed[i];
                    ZDO zdo = view.GetZDO();
                    view.ResetZDO();
                    UnityEngine.Object.Destroy(view.gameObject);
                    if (!zdo.Persistent && zdo.IsOwner()) ZDOMan.instance.DestroyZDO(zdo);
                    instances.Remove(zdo);
                }
                RunRemoved += removed.Count;
                if (removed.Count > MaxRemovedInTick) MaxRemovedInTick = removed.Count;
                if (backlog > 0) { RunDeferredTicks++; if (backlog > MaxBacklog) MaxBacklog = backlog; }
                return false;
            }
            catch (Exception ex)
            {
                // Remembered failure (no retry every tick): vanilla takes over for the rest of the session. Vanilla rescans
                // everything each tick, so any half-finished work is completed by it.
                _faulted = true;
                if (_log != null) _log.LogError("Spread Object Unloading stopped; Valheim's own unloading continues. " + ex);
                return true;
            }
        }
    }
}
