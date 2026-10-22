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
        public const string ModName = "Aethelborn Ascended - Saving Grace";
        public const string ModVersion = "0.25.250";

        private const string AdvancementDataKey = "AlbedoCustomClasses.Advancement";
        // Each owner advertises only Priest membership; cooldowns remain on the protected player's client.
        private const string PriestPresenceKey = "AA.SavingGracePriest_v1";
        private static readonly FieldInfo CustomDataField = typeof(Player).GetField(
            "m_customData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private Player _presencePlayer;
        private ZNetView _presenceView;

        internal static GuardianPlugin Instance;

        private ConfigEntry<float> _selfCooldown;
        private ConfigEntry<float> _allyCooldown;
        private ConfigEntry<float> _allyRadius;
        private ConfigEntry<float> _recoveryPercent;
        private ConfigEntry<float> _recoveryDuration;
        private ConfigEntry<float> _staminaUseCut;
        private ConfigEntry<bool> _enableVfx;
        private ConfigEntry<bool> _testingForceCooldowns;
        private ConfigEntry<float> _testingCooldownSeconds;

        private float _selfNextAvailableTime;
        private float _allyNextAvailableTime;
        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;

            _selfCooldown = Config.Bind("Grand Sigil Survival", "PriestSelfCooldownSeconds_v0123", 1200f, "Saving Grace: Priest death-save cooldown: 20 minutes.");
            _allyCooldown = Config.Bind("Grand Sigil Survival", "AllyResurrectionCooldownSeconds_v0208", 2400f, "Saving Grace: ally death-save cooldown, 40 minutes per ally.");
            _allyRadius = Config.Bind("Grand Sigil Survival", "AllyProtectionRadiusMeters_v0123", 20f, "Allies within 20m of a living Priest receive Saving Grace.");
            _recoveryPercent = Config.Bind("Grand Sigil Survival", "RecoveryPercent", 50f, "Percent of max HP restored after a lethal hit is prevented.");
            _recoveryDuration = Config.Bind("Grand Sigil Survival", "RecoveryDuration_v0208", 6f, "Saving Grace: 50% HP recovered over this many seconds; the speed / stamina window lasts as long.");
            _staminaUseCut = Config.Bind("Grand Sigil Survival", "StaminaUseReductionPercent", 70f, "Saving Grace: less Stamina used for all actions during the recovery window.");
            _enableVfx = Config.Bind("Visuals", "EnableVFX", true, "Enable Grand Sigil survival visual effects.");
            _testingForceCooldowns = Config.Bind("Testing", "ForceCooldowns", false, "Testing mode: force Grand Sigil survival cooldown to one value.");
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

        private float _indicatorAt;
        private bool _indicatorShown;
        private void Update()
        {
            if (Time.time < _indicatorAt) return;
            _indicatorAt = Time.time + 0.5f;
            Player player = Player.m_localPlayer;
            if (player == null) return;
            bool self = GetAdvancement(player) == "Priest";
            PublishPriestPresence(player, self);
            float until = self ? _selfNextAvailableTime : _allyNextAvailableTime;
            bool nearby = !self && until <= Time.time && !player.IsDead() && HasNearbyPriest(player);
            bool eligible = ShouldShowSavingGrace(!player.IsDead(), self, nearby, until, Time.time);
            if (!eligible)
            {
                if (_indicatorShown) DragonCombat.ClearStatus(player, "priest_life_token");
                _indicatorShown = false;
                return;
            }
            _indicatorShown = true;
            float left = Mathf.Max(0f, until - Time.time);
            DragonCombat.ShowStatusState(player, "priest_life_token", "heart", "Saving Grace" + (left > 0f ? " - Cooldown" : " - Ready"), left,
                left > 0f ? "Saving Grace is on cooldown for " + Mathf.CeilToInt(left) + "s. This timer continues outside Priest range."
                    : (self ? "Survive a lethal hit at 1 HP, then recover."
                        : "A living Priest is nearby. Survive a lethal hit at 1 HP, then recover. Stay within Priest range."));
        }

        // The cooldown state takes priority over range (and death); never hide an unexpired recipient timer.
        internal static bool ShouldShowSavingGrace(bool alive, bool self, bool nearby, float until, float now)
        {
            return until > now || (alive && (self || nearby));
        }

        private void PublishPriestPresence(Player player, bool priest)
        {
            if (_presencePlayer != player)
            {
                ClearPriestPresence();
                _presencePlayer = player;
                _presenceView = player.GetComponent<ZNetView>();
            }
            if (_presenceView == null || !_presenceView.IsValid() || !_presenceView.IsOwner()) return;
            ZDO zdo = _presenceView.GetZDO();
            if (zdo.GetBool(PriestPresenceKey, false) != priest) zdo.Set(PriestPresenceKey, priest);
        }

        private void ClearPriestPresence()
        {
            if (_presenceView != null && _presenceView.IsValid() && _presenceView.IsOwner())
            {
                ZDO zdo = _presenceView.GetZDO();
                if (zdo.GetBool(PriestPresenceKey, false)) zdo.Set(PriestPresenceKey, false);
            }
        }

        private void OnDestroy()
        {
            ClearPriestPresence();
            if (Player.m_localPlayer != null) DragonCombat.ClearStatus(Player.m_localPlayer, "priest_life_token");
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
            float radius = Mathf.Max(1f, DragonCombat.M(_allyRadius.Value));
            float radiusSquared = radius * radius;
            List<Player> players = Player.GetAllPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                Player priest = players[i];
                if (priest == null || priest == player || priest.IsDead()
                    || (priest.transform.position - player.transform.position).sqrMagnitude > radiusSquared) continue;
                ZNetView view = priest.GetComponent<ZNetView>();
                if (view != null && view.IsValid() && view.GetZDO().GetBool(PriestPresenceKey, false)) return true;
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

            _indicatorAt = 0f;
            Update();

            if (_enableVfx.Value)
                StartCoroutine(GrandSigilVisual(player));

            // Framework: +50% Movement Speed and -70% Stamina Usage for the 6s emergency window.
            DragonCombat.SetBuffNote("Priest.GrandSigilRecovery", "-" + Mathf.RoundToInt(_staminaUseCut.Value).ToString() + "% Stamina used by every action\nRecovering 50% of Max HP over " + Mathf.RoundToInt(_recoveryDuration.Value).ToString() + "s");
            DragonCombat.ApplyTimedBuff(player, "Priest.GrandSigilRecovery", Mathf.Max(0.1f, _recoveryDuration.Value), 0f, 0f, 0.50f, 0f, 0f, 0f, false);
            DragonCombat.ApplyStaminaUseCut(player, Mathf.Max(0f, _staminaUseCut.Value) / 100f, Mathf.Max(0.1f, _recoveryDuration.Value));
            StartCoroutine(Recovery(player));
            DragonCombat.PlayAccent(player, "cleric_rise", 0.12f);   // v0.25.13: rises back from the brink

            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(
                    MessageHud.MessageType.Center,
                    "Saving Grace - Saved"
                );

            Logger.LogInfo(allyProtection
                ? "Saving Grace prevented ally lethal damage."
                : "Saving Grace prevented Priest lethal damage.");
        }

        private IEnumerator GrandSigilVisual(Player player)
        {
            if (DragonVfx.Enabled && player != null)
            {
                // v0.25.190 rescue: a short upward holy recovery motif at the actual proc (no persistent cross)
                Player rp = player; Vector3 rpos = player.transform.position;
                DragonCombat.RunVfx(delegate
                {
                    DragonVfx.SoftWave(rpos, new Color(1f, 0.92f, 0.62f, 1f), 3f, 0.6f);
                    DragonVfx.Feathers(rpos + Vector3.up * 1.2f, new Color(1f, 0.97f, 0.88f, 1f), 1.2f, 1.2f, 18f);
                    DragonVfx.Aura(rp.transform, rpos, new Color(1f, 0.90f, 0.60f, 1f), 0.6f, 1.6f, 22f, 1.4f);
                });
                yield break;
            }
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
                if (CustomDataField == null) return null;
                return CustomDataField.GetValue(player) as IDictionary;
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