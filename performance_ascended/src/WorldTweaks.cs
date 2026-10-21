using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AethelbornPerformance
{
    // 0.5.0 techniques adopted from the FPS-mod survey (re-implemented; each one checked against assembly_valheim IL).
    public static class WorldTweaks
    {
        // ---------------- Door Sleep (idea: FPSDoctor DoorSleep)
        // Valheim: Door.UpdateState (repeating) only calls SetState when the ZDO changed; SetState sets the animator's
        // "state" int; CanInteract reads the animator state. An idle door's Animator still updates every frame.
        // Here an idle door's Animator is disabled (state kept: keepAnimatorStateOnDisable) and woken by SetState/CanInteract.
        public static bool DoorSleepOn;
        private sealed class DoorState { public int Id; public Door Door; public Animator Animator; public float WakeUntil; public bool Slept; }
        private static readonly List<DoorState> _doors = new List<DoorState>();
        private static readonly Dictionary<int, DoorState> _byId = new Dictionary<int, DoorState>();
        private static Func<Door, Animator> _animatorOf;
        private static int _cursor;
        public static int DoorsAsleep;
        private const float AwakeSeconds = 4f;   // a door animation is ~1-2 s

        // ---------------- Grass patch size (idea: vaffle1 FPSPlus; ClutterSystem.m_grassPatchSize, vanilla 10)
        public static float GrassPatchSize;   // 0 = game default

        public static string Install(Harmony harmony, ManualLogSource log)
        {
            string result = "";
            try
            {
                System.Reflection.FieldInfo field = AccessTools.Field(typeof(Door), "m_animator");
                System.Reflection.MethodInfo awake = AccessTools.DeclaredMethod(typeof(Door), "Awake");
                System.Reflection.MethodInfo setState = AccessTools.DeclaredMethod(typeof(Door), "SetState");
                System.Reflection.MethodInfo canInteract = AccessTools.DeclaredMethod(typeof(Door), "CanInteract");
                if (field != null && field.FieldType == typeof(Animator) && awake != null && setState != null && canInteract != null)
                {
                    System.Reflection.Emit.DynamicMethod m = new System.Reflection.Emit.DynamicMethod("PA_DoorAnimator", typeof(Animator), new Type[] { typeof(Door) }, typeof(Door), true);
                    System.Reflection.Emit.ILGenerator il = m.GetILGenerator();
                    il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); il.Emit(System.Reflection.Emit.OpCodes.Ldfld, field); il.Emit(System.Reflection.Emit.OpCodes.Ret);
                    _animatorOf = (Func<Door, Animator>)m.CreateDelegate(typeof(Func<Door, Animator>));
                    harmony.Patch(awake, postfix: new HarmonyMethod(typeof(WorldTweaks), "DoorAwake"));
                    harmony.Patch(setState, prefix: new HarmonyMethod(typeof(WorldTweaks), "DoorWake"));
                    harmony.Patch(canInteract, prefix: new HarmonyMethod(typeof(WorldTweaks), "DoorWake"));
                    result += "door sleep ready";
                }
                else result += "door sleep unavailable (Door changed)";
            }
            catch (Exception ex) { result += "door sleep unavailable (" + ex.GetType().Name + ")"; }
            try
            {
                System.Reflection.MethodInfo clutterAwake = AccessTools.DeclaredMethod(typeof(ClutterSystem), "Awake");
                if (clutterAwake != null && AccessTools.Field(typeof(ClutterSystem), "m_grassPatchSize") != null)
                { harmony.Patch(clutterAwake, prefix: new HarmonyMethod(typeof(WorldTweaks), "ClutterAwake")); result += "; grass patch size ready"; }
                else result += "; grass patch size unavailable";
            }
            catch (Exception ex) { result += "; grass patch size unavailable (" + ex.GetType().Name + ")"; }
            return result;
        }

        private static void DoorAwake(Door __instance)
        {
            try
            {
                Animator a = _animatorOf(__instance);
                if (a == null) return;
                DoorState s = new DoorState(); s.Id = __instance.GetInstanceID(); s.Door = __instance; s.Animator = a; s.WakeUntil = Time.unscaledTime + AwakeSeconds;
                a.keepAnimatorStateOnDisable = true;   // re-enabling must not reset the door to its default (closed) pose
                _byId[__instance.GetInstanceID()] = s; _doors.Add(s);
            }
            catch (Exception) { }
        }
        private static void DoorWake(Door __instance)
        {
            DoorState s;
            if (!_byId.TryGetValue(__instance.GetInstanceID(), out s)) return;
            s.WakeUntil = Time.unscaledTime + AwakeSeconds;
            if (s.Slept && s.Animator != null) { s.Animator.enabled = true; s.Slept = false; DoorsAsleep--; }
        }
        // Called every frame: checks a few doors per frame (no scene scans).
        public static void Tick()
        {
            int n = _doors.Count; if (n == 0) return;
            float now = Time.unscaledTime;
            for (int k = 0; k < 16 && n > 0; k++)
            {
                if (_cursor >= n) _cursor = 0;
                DoorState s = _doors[_cursor];
                if (s.Door == null || s.Animator == null)
                {
                    if (s.Slept) DoorsAsleep--;
                    _byId.Remove(s.Id);
                    _doors[_cursor] = _doors[n - 1]; _doors.RemoveAt(n - 1); n--; continue;
                }
                if (DoorSleepOn && !s.Slept && now > s.WakeUntil && s.Animator.enabled && !s.Animator.IsInTransition(0))
                { s.Animator.enabled = false; s.Slept = true; DoorsAsleep++; }
                else if (!DoorSleepOn && s.Slept) { s.Animator.enabled = true; s.Slept = false; DoorsAsleep--; }
                _cursor++;
            }
        }
        public static void WakeAll()
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Slept && _doors[i].Animator != null) { _doors[i].Animator.enabled = true; _doors[i].Slept = false; }
            DoorsAsleep = 0;
        }

        private static void ClutterAwake(ClutterSystem __instance)
        {
            if (GrassPatchSize > 0f && __instance.m_grassPatchSize < GrassPatchSize) __instance.m_grassPatchSize = GrassPatchSize;
        }

        public static string Report()
        {
            return "DoorSleep=" + DoorSleepOn + "; DoorsTracked=" + _doors.Count + "; DoorsAsleep=" + DoorsAsleep + "; GrassPatchSize=" + (GrassPatchSize > 0f ? GrassPatchSize.ToString(System.Globalization.CultureInfo.InvariantCulture) : "game") +
                "; AsyncUploadSlice_ms=" + QualitySettings.asyncUploadTimeSlice + "; AsyncUploadBuffer_MB=" + QualitySettings.asyncUploadBufferSize;
        }
    }
}
