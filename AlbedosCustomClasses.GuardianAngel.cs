using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using DragonsAltarCombat;

namespace AlbedosCustomClassesGuardian
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("albedo.customclasses", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.combatruntime", BepInDependency.DependencyFlags.HardDependency)]
    public class GuardianPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "albedo.customclasses.guardian";
        public const string ModName = "Dragon's Altar - Grand Sigil Survival";
        public const string ModVersion = "0.18.1";

        private const string AdvancementDataKey = "AlbedoCustomClasses.Advancement";

        internal static GuardianPlugin Instance;

        private ConfigEntry<float> _selfCooldown;
        private ConfigEntry<float> _allyCooldown;
        private ConfigEntry<float> _allyRadius;
        private ConfigEntry<float> _recoveryPercent;
        private ConfigEntry<float> _recoveryDuration;
        private ConfigEntry<bool> _enableVfx;
        private ConfigEntry<bool> _testingForceCooldowns;
        private ConfigEntry<float> _testingCooldownSeconds;

        private float _selfNextAvailableTime;
        private float _allyNextAvailableTime;
        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;

            _selfCooldown = Config.Bind("Grand Sigil Survival", "PriestSelfCooldownSeconds_v0123", 1200f, "Priest Grand Sigil death-save cooldown: 20 minutes.");
            _allyCooldown = Config.Bind("Grand Sigil Survival", "AllyResurrectionCooldownSeconds_v0123", 2100f, "Ally Grand Sigil death-save cooldown while protected by a nearby Priest: 35 minutes.");
            _allyRadius = Config.Bind("Grand Sigil Survival", "AllyProtectionRadiusMeters_v0123", 20f, "Allies within 20m of a living Priest receive Grand Sigil's death-save.");
            _recoveryPercent = Config.Bind("Grand Sigil Survival", "RecoveryPercent", 50f, "Percent of max HP restored after a lethal hit is prevented.");
            _recoveryDuration = Config.Bind("Grand Sigil Survival", "RecoveryDuration", 7f, "Framework: recovery duration in seconds.");
            _enableVfx = Config.Bind("Visuals", "EnableVFX", true, "Enable Grand Sigil survival visual effects.");
            _testingForceCooldowns = Config.Bind("Testing", "ForceCooldowns", true, "Testing mode: force Grand Sigil survival cooldown to one value.");
            _testingCooldownSeconds = Config.Bind("Testing", "CooldownSeconds", 5f, "Testing cooldown used while ForceCooldowns is enabled.");

            try
            {
                _harmony = new Harmony(ModGuid);
                _harmony.PatchAll();
                Logger.LogInfo(ModName + " v" + ModVersion + " loaded and patch enabled.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Guardian Angel patch failed. Core shrine remains unaffected. " + ex);
            }
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                try
                {
                    _harmony.UnpatchSelf();
                }
                catch
                {
                }
            }
        }

        internal void TryProtectSetHealth(Player player, ref float requestedHealth)
        {
            if (player == null || requestedHealth > 0f)
                return;

            // Require the player to currently be alive so initialization,
            // respawn and cleanup calls cannot consume Grand Sigil.
            if (player.GetHealth() <= 0f)
                return;

            bool allyProtection;
            if (!CanProtect(player, out allyProtection))
                return;

            requestedHealth = 1f;
            TriggerProtection(player, allyProtection);
        }

        internal void TryProtectCheckDeath(Player player)
        {
            if (player == null || player.GetHealth() > 0f)
                return;

            bool allyProtection;
            if (!CanProtect(player, out allyProtection))
                return;

            player.SetHealth(1f);
            TriggerProtection(player, allyProtection);
        }

        private bool CanProtect(Player player, out bool allyProtection)
        {
            allyProtection = false;

            if (GetAdvancement(player) == "Priest")
                return Time.time >= _selfNextAvailableTime;

            if (Time.time < _allyNextAvailableTime)
                return false;

            if (!HasNearbyPriest(player))
                return false;

            allyProtection = true;
            return true;
        }

        private bool HasNearbyPriest(Player player)
        {
            float radius = Mathf.Max(1f, _allyRadius.Value);
            Collider[] hits = Physics.OverlapSphere(player.transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
            HashSet<int> seen = new HashSet<int>();

            for (int i = 0; i < hits.Length; i++)
            {
                Player priest = hits[i].GetComponentInParent<Player>();
                if (priest == null || priest == player || priest.IsDead())
                    continue;

                int id = priest.GetInstanceID();
                if (!seen.Add(id))
                    continue;

                if (GetAdvancement(priest) == "Priest")
                    return true;
            }

            return false;
        }

        private void TriggerProtection(Player player, bool allyProtection)
        {
            float cooldown = allyProtection ? _allyCooldown.Value : _selfCooldown.Value;

            if (_testingForceCooldowns.Value)
                cooldown = _testingCooldownSeconds.Value;

            if (allyProtection)
                _allyNextAvailableTime = Time.time + Mathf.Max(0f, cooldown);
            else
                _selfNextAvailableTime = Time.time + Mathf.Max(0f, cooldown);

            if (_enableVfx.Value)
                StartCoroutine(GrandSigilVisual(player));

            DragonCombat.ApplyTimedBuff(player, "Priest.GrandSigilRecovery", Mathf.Max(0.1f, _recoveryDuration.Value), 0f, 0f, 0.50f, 0f, 1.00f, 0f, false);
            StartCoroutine(Recovery(player));

            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(
                    MessageHud.MessageType.Center,
                    allyProtection ? "Grand Sigil - Resurrection" : "Grand Sigil"
                );

            Logger.LogInfo(allyProtection
                ? "Grand Sigil Resurrection prevented ally lethal damage."
                : "Grand Sigil prevented Priest lethal damage.");
        }

        private IEnumerator GrandSigilVisual(Player player)
        {
            GameObject ringObject = new GameObject("DragonsAltarGrandSigilRing");
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = 49;
            ring.startWidth = 0.09f;
            ring.endWidth = 0.09f;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                ring.material = new Material(shader);

            GameObject beamObject = new GameObject("DragonsAltarGrandSigilBeam");
            LineRenderer beam = beamObject.AddComponent<LineRenderer>();
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            beam.startWidth = 0.15f;
            beam.endWidth = 0.04f;

            if (shader != null)
                beam.material = new Material(shader);

            float duration = 1.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (player == null)
                    break;

                float t = elapsed / duration;
                float radius = Mathf.Lerp(0.55f, 3.1f, t);
                Vector3 center = player.transform.position + Vector3.up * (0.12f + t * 0.75f);

                Color gold = new Color(1f, 0.86f, 0.38f, 1f - t);
                ring.startColor = gold;
                ring.endColor = new Color(0.72f, 0.94f, 1f, gold.a);

                for (int i = 0; i < ring.positionCount; i++)
                {
                    float angle = ((float)i / (float)(ring.positionCount - 1)) * Mathf.PI * 2f;
                    ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                }

                Vector3 beamBase = player.transform.position + Vector3.up * 0.4f;
                beam.SetPosition(0, beamBase);
                beam.SetPosition(1, beamBase + Vector3.up * 6f);
                beam.startColor = new Color(1f, 0.95f, 0.68f, 0.75f * (1f - t));
                beam.endColor = new Color(0.65f, 0.9f, 1f, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(ringObject);
            Destroy(beamObject);
        }

        private IEnumerator Recovery(Player player)
        {
            float total = player.GetMaxHealth() * Mathf.Max(0f, _recoveryPercent.Value) / 100f;
            float duration = Mathf.Max(0.1f, _recoveryDuration.Value);
            int ticks = Mathf.Max(1, Mathf.CeilToInt(duration));
            float interval = duration / ticks;
            float amount = total / ticks;

            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(interval);

                if (player == null || player.IsDead())
                    yield break;

                Heal(player, amount);
            }
        }

        private void Heal(Character target, float amount)
        {
            try
            {
                MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "Heal")
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length < 1 || parameters[0].ParameterType != typeof(float))
                        continue;

                    object[] args = new object[parameters.Length];
                    args[0] = amount;

                    for (int j = 1; j < parameters.Length; j++)
                    {
                        if (parameters[j].HasDefaultValue)
                            args[j] = parameters[j].DefaultValue;
                        else if (parameters[j].ParameterType == typeof(bool))
                            args[j] = true;
                        else if (parameters[j].ParameterType.IsValueType)
                            args[j] = Activator.CreateInstance(parameters[j].ParameterType);
                        else
                            args[j] = null;
                    }

                    method.Invoke(target, args);
                    return;
                }
            }
            catch
            {
            }

            target.SetHealth(Mathf.Min(target.GetMaxHealth(), target.GetHealth() + amount));
        }

        private string GetAdvancement(Player player)
        {
            IDictionary data = GetCustomData(player);
            if (data == null || !data.Contains(AdvancementDataKey))
                return "";

            object value = data[AdvancementDataKey];
            return value == null ? "" : value.ToString();
        }

        private IDictionary GetCustomData(Player player)
        {
            try
            {
                FieldInfo field = typeof(Player).GetField(
                    "m_customData",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (field == null)
                    return null;

                return field.GetValue(player) as IDictionary;
            }
            catch
            {
                return null;
            }
        }
    }

    [HarmonyPatch]
    internal static class GuardianSetHealthPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            HashSet<MethodBase> found = new HashSet<MethodBase>();
            Type[] types = new Type[] { typeof(Character), typeof(Player) };

            for (int t = 0; t < types.Length; t++)
            {
                MethodInfo[] methods = types[t].GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != "SetHealth")
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length < 1)
                        continue;

                    if (parameters[0].ParameterType != typeof(float))
                        continue;

                    if (found.Add(method))
                        yield return method;
                }
            }
        }

        private static void Prefix(Character __instance, ref float __0)
        {
            if (GuardianPlugin.Instance == null)
                return;

            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer)
                return;

            GuardianPlugin.Instance.TryProtectSetHealth(player, ref __0);
        }
    }

    [HarmonyPatch]
    internal static class GuardianCheckDeathPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            HashSet<MethodBase> found = new HashSet<MethodBase>();
            Type[] types = new Type[] { typeof(Character), typeof(Player) };

            for (int t = 0; t < types.Length; t++)
            {
                MethodInfo[] methods = types[t].GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != "CheckDeath")
                        continue;

                    if (found.Add(method))
                        yield return method;
                }
            }
        }

        private static void Prefix(Character __instance)
        {
            if (GuardianPlugin.Instance == null)
                return;

            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer)
                return;

            GuardianPlugin.Instance.TryProtectCheckDeath(player);
        }
    }

}