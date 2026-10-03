using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using DragonsAltarCombat;

namespace AlbedosCustomClassesSkills
{
    public static class AlbedoAimUtility
    {
        private static readonly int CrosshairMask = LayerMask.GetMask(
            "Default",
            "static_solid",
            "Default_small",
            "piece_nonsolid",
            "terrain",
            "vehicle",
            "piece",
            "viewblock",
            "character",
            "character_net",
            "character_ghost"
        );

        private static bool IsLocalPlayerCollider(Player player, Collider collider)
        {
            if (player == null || collider == null)
                return false;

            Transform hitTransform = collider.transform;

            if (hitTransform == player.transform || hitTransform.IsChildOf(player.transform))
                return true;

            Rigidbody attached = collider.attachedRigidbody;

            if (attached != null && attached.gameObject == player.gameObject)
                return true;

            Character character = collider.GetComponentInParent<Character>();

            return character == player;
        }

        private static bool TryGetFirstCrosshairHit(Player player, out RaycastHit worldHit)
        {
            worldHit = default(RaycastHit);

            if (player == null)
                return false;

            Vector3 rayOrigin;
            Vector3 rayDirection;

            if (GameCamera.instance != null)
            {
                rayOrigin = GameCamera.instance.transform.position;
                rayDirection = GameCamera.instance.transform.forward;
            }
            else
            {
                rayOrigin = player.GetEyePoint();
                rayDirection = player.GetLookDir();
            }

            if (rayDirection.sqrMagnitude < 0.01f)
                return false;

            rayDirection.Normalize();

            RaycastHit[] hits = Physics.RaycastAll(
                rayOrigin,
                rayDirection,
                1000f,
                CrosshairMask
            );

            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(
                hits,
                delegate(RaycastHit a, RaycastHit b)
                {
                    return a.distance.CompareTo(b.distance);
                }
            );

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;

                if (collider == null)
                    continue;

                if (collider.isTrigger)
                    continue;

                if (IsLocalPlayerCollider(player, collider))
                    continue;

                worldHit = hits[i];
                return true;
            }

            return false;
        }

        public static bool TryGetPhysicalTarget(Player player, float range, out Vector3 point)
        {
            point = Vector3.zero;

            RaycastHit hit;

            if (!TryGetFirstCrosshairHit(player, out hit))
                return false;

            float maxRange = Mathf.Max(0.1f, range);
            float distanceFromPlayer = Vector3.Distance(player.transform.position, hit.point);

            if (distanceFromPlayer > maxRange)
                return false;

            point = hit.point;
            return true;
        }

        public static Vector3 GetProjectileDirection(Player player, Vector3 spawnPosition)
        {
            if (player == null)
                return Vector3.forward;

            RaycastHit hit;

            if (TryGetFirstCrosshairHit(player, out hit))
            {
                Vector3 toHit = hit.point - spawnPosition;

                if (toHit.sqrMagnitude > 0.01f)
                    return toHit.normalized;
            }

            Vector3 nativeAim = player.GetAimDir(spawnPosition);

            if (nativeAim.sqrMagnitude > 0.01f)
                return nativeAim.normalized;

            Vector3 look = player.GetLookDir();

            if (look.sqrMagnitude > 0.01f)
                return look.normalized;

            Vector3 fallback = player.transform.forward;

            if (fallback.sqrMagnitude < 0.01f)
                fallback = Vector3.forward;

            return fallback.normalized;
        }

        public static Vector3 GetFarCrosshairPoint(Player player, Vector3 spawnPosition, float distance)
        {
            RaycastHit hit;

            if (player != null && TryGetFirstCrosshairHit(player, out hit))
                return hit.point;

            return spawnPosition + GetProjectileDirection(player, spawnPosition) * Mathf.Max(1f, distance);
        }
    }

    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("albedo.customclasses", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.combatruntime", BepInDependency.DependencyFlags.HardDependency)]
    public class SkillsPlugin : BaseUnityPlugin
    {
        public static SkillsPlugin Instance;
        public const string ModGuid = "albedo.customclasses.skills";
        public const string ModName = "Dragon's Altar - Starter Skills";
        public const string ModVersion = "0.20.5";

        private const string ClassDataKey = "AlbedoCustomClasses.Class";
        private const string WarriorRunBonusKey = "AlbedoCustomClasses.WarriorRunBonus";

        private ConfigEntry<KeyCode> _modifier;
        private ConfigEntry<KeyCode> _skill1;
        private ConfigEntry<KeyCode> _skill2;
        private ConfigEntry<KeyCode> _skill3;
        private ConfigEntry<KeyCode> _ultimate;

        private ConfigEntry<float> _heavyCooldown;
        private ConfigEntry<float> _heavyStamina;
        private ConfigEntry<float> _heavyWindup;
        private ConfigEntry<float> _heavyRange;
        private ConfigEntry<float> _heavyAngle;
        private DamageConfig _heavyDamage;

        private ConfigEntry<float> _impactCooldown;
        private ConfigEntry<float> _impactStamina;
        private ConfigEntry<float> _impactWindup;
        private ConfigEntry<float> _impactLength;
        private ConfigEntry<float> _impactWidth;
        private ConfigEntry<float> _impactTravelTime;
        private DamageConfig _impactDamage;

        private ConfigEntry<float> _punchCooldown;
        private ConfigEntry<float> _punchStamina;
        private ConfigEntry<float> _punchWindup;
        private ConfigEntry<float> _punchRange;
        private ConfigEntry<float> _punchWidth;
        private DamageConfig _punchDamage;

        private ConfigEntry<float> _zapCooldown;
        private ConfigEntry<float> _zapStamina;
        private ConfigEntry<float> _zapRange;
        private ConfigEntry<float> _zapConeAngle;
        private ConfigEntry<float> _zapExplosionDamage;
        private DamageConfig _zapDamage;

        private ConfigEntry<float> _righteousCooldown;
        private ConfigEntry<float> _righteousStamina;
        private ConfigEntry<float> _righteousWindup;
        private ConfigEntry<float> _righteousRadius;
        private ConfigEntry<float> _righteousRange;
        private ConfigEntry<float> _righteousExposeDuration;
        private DamageConfig _righteousDamage;

        private ConfigEntry<float> _holyCooldown;
        private ConfigEntry<float> _holyStamina;
        private ConfigEntry<float> _holyRadius;
        private ConfigEntry<float> _holyImmediate;
        private ConfigEntry<float> _holyPercent;
        private ConfigEntry<float> _holyDuration;

        private ConfigEntry<float> _flameCooldown;
        private ConfigEntry<float> _flameEitr;
        private ConfigEntry<float> _flameRange;
        private ConfigEntry<float> _flameConeAngle;
        private ConfigEntry<float> _flameBurnDuration;
        private DamageConfig _flameDamage;
        private ConfigEntry<float> _glacialCooldown;
        private ConfigEntry<float> _glacialEitr;
        private ConfigEntry<float> _glacialWindup;
        private ConfigEntry<float> _glacialRange;
        private ConfigEntry<float> _glacialRadius;
        private ConfigEntry<float> _glacialFrostDuration;
        private DamageConfig _glacialDamage;
        private ConfigEntry<float> _stoneCooldown;
        private ConfigEntry<float> _stoneEitr;
        private ConfigEntry<float> _stoneWindup;
        private ConfigEntry<float> _stoneRange;
        private ConfigEntry<float> _stoneRadius;
        private ConfigEntry<float> _stoneCrippleDuration;
        private DamageConfig _stoneDamage;
        private ConfigEntry<float> _sorcererEitrRegenBonus;
        private ConfigEntry<float> _sorcererMaxEitrBonus;

        private ConfigEntry<bool> _showSkillHud;
        private ConfigEntry<bool> _enableVfx;
        private ConfigEntry<float> _hudScale;
        private ConfigEntry<bool> _testingForceCooldowns;
        private ConfigEntry<float> _testingCooldownSeconds;

        private GUIStyle _hudTitleStyle;
        private GUIStyle _hudSkillStyle;
        private GUIStyle _hudSmallStyle;
        private Texture2D _hudBackground;
        private Texture2D _hudReadyBackground;
        private Texture2D _hudCooldownBackground;

        private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

        private string _lastClass = "";
        private float _lastStamina = -1f;
        private float _lastHealth = -1f;
        private Harmony _eitrHarmony;

        private void Awake()
        {
            Instance = this;
            _modifier = Config.Bind("Hotkeys", "Modifier", KeyCode.Mouse3, "Thumb mouse button 1 on most mice.");
            _skill1 = Config.Bind("Hotkeys", "Skill1", KeyCode.Alpha1, "Hold Modifier + this key.");
            _skill2 = Config.Bind("Hotkeys", "Skill2", KeyCode.Alpha2, "Hold Modifier + this key.");
            _skill3 = Config.Bind("Hotkeys", "Skill3", KeyCode.Alpha3, "Third starter-class active skill.");
            _ultimate = Config.Bind("Hotkeys", "UltimateLegacy", KeyCode.Alpha6, "Legacy field retained for config compatibility. Advanced ultimates use Slot 6.");

            _heavyCooldown = Config.Bind("Warrior.Heavy Slash", "Cooldown", 8f, "Seconds. Testing override is applied while ForceCooldowns is enabled.");
            _heavyStamina = Config.Bind("Warrior.Heavy Slash", "StaminaCost", 20f, "Stamina cost.");
            _heavyWindup = Config.Bind("Warrior.Heavy Slash", "Windup", 0.7f, "Base windup before Attack Speed modifiers.");
            _heavyRange = Config.Bind("Warrior.Heavy Slash", "Range", 3.5f, "Literal Valheim-meter reach. Heavy Slash has no exact framework distance, so 3.5m remains the prototype default.");
            _heavyAngle = Config.Bind("Warrior.Heavy Slash", "ArcDegrees", 120f, "Horizontal slash arc.");
            _heavyDamage = BindDamage("Warrior.Heavy Slash.Damage", 0f, 55f, 0f, 0f, 0f, 0f, 0f, 0f);

            _impactCooldown = Config.Bind("Warrior.Impact Wave", "Cooldown", 8f, "Seconds.");
            _impactStamina = Config.Bind("Warrior.Impact Wave", "StaminaCost", 18f, "Stamina cost.");
            _impactWindup = Config.Bind("Warrior.Impact Wave", "Windup", 1f, "Framework default windup because this skill does not specify one.");
            _impactLength = Config.Bind("Warrior.Impact Wave", "Length", 10f, "Framework Ground Projectile range: literal 10m.");
            _impactWidth = Config.Bind("Warrior.Impact Wave", "Width", 2f, "Framework Ground Projectile width: literal 2m.");
            _impactTravelTime = Config.Bind("Warrior.Impact Wave", "TravelTime", 0.65f, "Testing/default travel time across full range.");
            _impactDamage = BindDamage("Warrior.Impact Wave.Damage", 35f, 0f, 18f, 0f, 0f, 0f, 0f, 0f);

            _punchCooldown = Config.Bind("Warrior.Impact Punch", "Cooldown", 8f, "Seconds.");
            _punchStamina = Config.Bind("Warrior.Impact Punch", "StaminaCost", 12f, "Stamina cost.");
            _punchWindup = Config.Bind("Warrior.Impact Punch", "Windup", 0.5f, "Base windup before Attack Speed modifiers.");
            _punchRange = Config.Bind("Warrior.Impact Punch", "Range", 2f, "Framework reach: literal 2m.");
            _punchWidth = Config.Bind("Warrior.Impact Punch", "Width", 2f, "Framework hitbox width: literal 2m.");
            _punchDamage = BindDamage("Warrior.Impact Punch.Damage", 42f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            _zapCooldown = Config.Bind("Cleric.Lightning Zap", "Cooldown", 8f, "Seconds.");
            _zapStamina = Config.Bind("Cleric.Lightning Zap", "StaminaCost", 18f, "Stamina cost.");
            _zapRange = Config.Bind("Cleric.Lightning Zap", "Range", 10f, "Framework cone range: literal 10m.");
            _zapConeAngle = Config.Bind("Cleric.Lightning Zap", "ConeDegrees", 70f, "Testing/default cone width.");
            _zapExplosionDamage = Config.Bind("Cleric.Lightning Zap", "ZapExplosionLightningDamage", 0f, "0 uses Combat Runtime Zap default because the framework does not specify an explosion damage amount.");
            _zapDamage = BindDamage("Cleric.Lightning Zap.Damage", 0f, 0f, 18f, 0f, 0f, 40f, 0f, 0f);

            _righteousCooldown = Config.Bind("Cleric.Righteous Strike", "Cooldown", 8f, "Seconds.");
            _righteousStamina = Config.Bind("Cleric.Righteous Strike", "StaminaCost", 20f, "Stamina cost.");
            _righteousWindup = Config.Bind("Cleric.Righteous Strike", "Windup", 0.7f, "Framework windup.");
            _righteousRadius = Config.Bind("Cleric.Righteous Strike", "Radius", 5f, "Framework AoE radius: literal 5m.");
            _righteousRange = Config.Bind("Cleric.Righteous Strike", "Range", 50f, "Ground PAC cast range in literal world meters. Works indoors.");
            _righteousExposeDuration = Config.Bind("Cleric.Righteous Strike", "ExposeDuration", 6f, "Framework default debuff duration.");
            _righteousDamage = BindDamage("Cleric.Righteous Strike.Damage", 35f, 0f, 0f, 0f, 0f, 42f, 0f, 0f);

            _holyCooldown = Config.Bind("Cleric.Holy Wave", "Cooldown", 8f, "Seconds.");
            _holyStamina = Config.Bind("Cleric.Holy Wave", "StaminaCost", 25f, "Stamina cost.");
            _holyRadius = Config.Bind("Cleric.Holy Wave", "Radius", 7f, "Framework healing radius: literal 7m.");
            _holyImmediate = Config.Bind("Cleric.Holy Wave", "ImmediateHeal", 25f, "Framework immediate HP heal.");
            _holyPercent = Config.Bind("Cleric.Holy Wave", "HealPercentPerSecond", 5f, "Percent max HP healed each second.");
            _holyDuration = Config.Bind("Cleric.Holy Wave", "Duration", 6f, "Heal-over-time duration.");

            _flameCooldown = Config.Bind("Sorcerer.Flame Burst", "Cooldown", 7f, "Seconds.");
            _flameEitr = Config.Bind("Sorcerer.Flame Burst", "EitrCost", 18f, "Prototype Eitr cost.");
            _flameRange = Config.Bind("Sorcerer.Flame Burst", "Range", 10f, "Literal 10m cone.");
            _flameConeAngle = Config.Bind("Sorcerer.Flame Burst", "ConeDegrees", 75f, "Cone angle.");
            _flameBurnDuration = Config.Bind("Sorcerer.Flame Burst", "FireBurnDuration", 6f, "Fire Burn duration.");
            _flameDamage = BindDamage("Sorcerer.Flame Burst.Damage", 0f, 0f, 0f, 34f, 0f, 0f, 0f, 0f);
            _glacialCooldown = Config.Bind("Sorcerer.Glacial Descent", "Cooldown", 10f, "Seconds.");
            _glacialEitr = Config.Bind("Sorcerer.Glacial Descent", "EitrCost", 28f, "Prototype Eitr cost.");
            _glacialWindup = Config.Bind("Sorcerer.Glacial Descent", "Windup", 1f, "Cast lock.");
            _glacialRange = Config.Bind("Sorcerer.Glacial Descent", "GroundPACRange", 50f, "Ground PAC range.");
            _glacialRadius = Config.Bind("Sorcerer.Glacial Descent", "Radius", 5f, "Literal 5m impact radius.");
            _glacialFrostDuration = Config.Bind("Sorcerer.Glacial Descent", "FrostDuration", 6f, "Frost duration.");
            _glacialDamage = BindDamage("Sorcerer.Glacial Descent.Damage", 34f, 0f, 0f, 0f, 42f, 0f, 0f, 0f);
            _stoneCooldown = Config.Bind("Sorcerer.Stonefang Eruption", "Cooldown", 9f, "Seconds.");
            _stoneEitr = Config.Bind("Sorcerer.Stonefang Eruption", "EitrCost", 24f, "Prototype Eitr cost.");
            _stoneWindup = Config.Bind("Sorcerer.Stonefang Eruption", "Windup", 0.8f, "Cast windup.");
            _stoneRange = Config.Bind("Sorcerer.Stonefang Eruption", "GroundPACRange", 40f, "Ground PAC range.");
            _stoneRadius = Config.Bind("Sorcerer.Stonefang Eruption", "Radius", 5f, "Literal 5m radius.");
            _stoneCrippleDuration = Config.Bind("Sorcerer.Stonefang Eruption", "CrippleDuration", 6f, "Cripple duration.");
            _stoneDamage = BindDamage("Sorcerer.Stonefang Eruption.Damage", 32f, 0f, 30f, 0f, 0f, 0f, 0f, 0f);
            _sorcererEitrRegenBonus = Config.Bind("Sorcerer Blessing", "EitrRegenPercent_v0123", 30f, "Arcane Blood: +30% Eitr Regen.");
            _sorcererMaxEitrBonus = Config.Bind("Sorcerer Blessing", "FlatMaxEitr_v0123", 40f, "Arcane Blood: +40 flat maximum Eitr.");
            InstallArcaneBloodMaxEitr();

            _showSkillHud = Config.Bind("Interface", "ShowSkillHud", true, "Show the current class skill HUD.");
            _hudScale = Config.Bind("Interface", "HudScale", 1f, "Skill HUD scale.");
            _enableVfx = Config.Bind("Visuals", "EnableVFX", true, "Enable class ability visual effects.");
            _testingForceCooldowns = Config.Bind("Testing", "ForceCooldowns", true, "Testing mode: force all starter active cooldowns to one value.");
            _testingCooldownSeconds = Config.Bind("Testing", "CooldownSeconds", 5f, "Testing cooldown used while ForceCooldowns is enabled.");

            // v0.10.0 scale migration: only known old defaults are upgraded.
            // Range values are framework center-to-edge meters; VFX now reach and hold the true radius like the DirtyHoe grid reference.
            MigrateFloat(_heavyRange, 7f, 3.5f);
            MigrateFloat(_impactLength, 20f, 10f);
            MigrateFloat(_impactWidth, 4f, 2f);
            MigrateFloat(_punchRange, 4f, 2f);
            MigrateFloat(_punchWidth, 4f, 2f);
            MigrateFloat(_zapRange, 20f, 10f);
            MigrateFloat(_righteousRadius, 10f, 5f);
            MigrateFloat(_righteousRange, 100f, 50f);
            MigrateFloat(_holyRadius, 14f, 7f);

            Logger.LogInfo(ModName + " v" + ModVersion + " loaded.");
            Logger.LogInfo("Starter Skills config loaded successfully. Hotkeys are ready.");
        }

        private void MigrateFloat(ConfigEntry<float> entry, float oldValue, float newValue)
        {
            if (entry != null && Mathf.Approximately(entry.Value, oldValue))
                entry.Value = newValue;
        }

        private void InstallArcaneBloodMaxEitr()
        {
            try
            {
                MethodInfo patch = typeof(SkillsPlugin).GetMethod("SetMaxEitrPrefix", BindingFlags.Static | BindingFlags.NonPublic);
                _eitrHarmony = new Harmony(ModGuid + ".arcaneblood");
                MethodInfo[] methods = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                int count = 0;
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "SetMaxEitr")
                        continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length < 1 || parameters[0].ParameterType != typeof(float))
                        continue;

                    MethodInfo[] overloads = typeof(Harmony).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    bool installed = false;
                    for (int h = 0; h < overloads.Length; h++)
                    {
                        MethodInfo overload = overloads[h];
                        if (overload.Name != "Patch") continue;
                        ParameterInfo[] patchParameters = overload.GetParameters();
                        if (patchParameters.Length < 2 || !typeof(MethodBase).IsAssignableFrom(patchParameters[0].ParameterType)) continue;
                        bool compatible = true;
                        for (int k = 1; k < patchParameters.Length; k++)
                            if (patchParameters[k].ParameterType != typeof(HarmonyMethod)) compatible = false;
                        if (!compatible) continue;
                        object[] args = new object[patchParameters.Length];
                        args[0] = method;
                        args[1] = new HarmonyMethod(patch);
                        overload.Invoke(_eitrHarmony, args);
                        installed = true;
                        break;
                    }
                    if (!installed) throw new MissingMethodException("Harmony Patch overload unavailable");
                    count++;
                }
                Logger.LogInfo("Arcane Blood SetMaxEitr hooks installed: " + count);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Arcane Blood maximum Eitr hook failed: " + ex.Message);
            }
        }

        private static void SetMaxEitrPrefix(Player __instance, ref float __0)
        {
            if (__instance != null && Instance != null && Instance.GetSelectedClass(__instance) == "Sorcerer")
                __0 += Mathf.Max(0f, Instance._sorcererMaxEitrBonus.Value);
        }

        private void RefreshFoodStats(Player player)
        {
            if (player == null)
                return;
            try
            {
                MethodInfo method = player.GetType().GetMethod("UpdateFood", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(float), typeof(bool) }, null);
                if (method != null)
                    method.Invoke(player, new object[] { 0f, true });
            }
            catch
            {
            }
        }

        private DamageConfig BindDamage(string section, float blunt, float slash, float pierce, float fire, float frost, float lightning, float poison, float spirit)
        {
            DamageConfig cfg = new DamageConfig();
            cfg.Blunt = Config.Bind(section, "Blunt", blunt, "Blunt damage.");
            cfg.Slash = Config.Bind(section, "Slash", slash, "Slash damage.");
            cfg.Pierce = Config.Bind(section, "Pierce", pierce, "Pierce damage.");
            cfg.Fire = Config.Bind(section, "Fire", fire, "Fire damage.");
            cfg.Frost = Config.Bind(section, "Frost", frost, "Frost damage.");
            cfg.Lightning = Config.Bind(section, "Lightning", lightning, "Lightning damage.");
            cfg.Poison = Config.Bind(section, "Poison", poison, "Poison damage.");
            cfg.Spirit = Config.Bind(section, "Spirit", spirit, "Spirit damage.");
            return cfg;
        }

        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            string selectedClass = GetSelectedClass(player);

            if (_lastClass != selectedClass)
            {
                _lastClass = selectedClass;
                _lastStamina = GetStamina(player);
                _lastHealth = player.GetHealth();

                if (selectedClass == "Warrior")
                    ShowMessage("Warrior Blessing: Hyper Armor");
                else if (selectedClass == "Cleric")
                    ShowMessage("Cleric ready");
                else if (selectedClass == "Sorcerer")
                    ShowMessage("Sorcerer Blessing: Arcane Blood");

                RefreshFoodStats(player);
            }

            if (selectedClass == "Sorcerer")
                DragonCombat.ApplyTimedBuff(player, "Sorcerer.ArcaneBlood", 0.35f, 0f, 0f, 0f, 0f, 0f, Mathf.Max(0f, _sorcererEitrRegenBonus.Value) / 100f, false);

            // v0.18.1: Cleric (Paladin tree) skills are cast from the Skill Tree hotbar instead.
            if (DragonCombat.IsTreeHotbarActive(player))
                return;

            if (!Input.GetKey(_modifier.Value))
                return;

            if (Input.GetKeyDown(_skill1.Value))
            {
                if (selectedClass == "Warrior")
                    CastHeavySlash(player);
                else if (selectedClass == "Cleric")
                    CastLightningZap(player);
                else if (selectedClass == "Sorcerer")
                    CastFlameBurst(player);
            }

            if (Input.GetKeyDown(_skill2.Value))
            {
                if (selectedClass == "Warrior")
                    CastImpactWave(player);
                else if (selectedClass == "Cleric")
                    CastRighteousStrike(player);
                else if (selectedClass == "Sorcerer")
                    CastGlacialDescent(player);
            }

            if (Input.GetKeyDown(_skill3.Value))
            {
                if (selectedClass == "Warrior")
                    CastImpactPunch(player);
                else if (selectedClass == "Cleric")
                    CastHolyWave(player);
                else if (selectedClass == "Sorcerer")
                    CastStonefangEruption(player);
            }
        }

        private void CastHeavySlash(Player player)
        {
            const string id = "Warrior.HeavySlash";
            if (!BeginCast(player, id, _heavyCooldown.Value, _heavyStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _heavyWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "HeavySlash", windup + 0.10f);
            StartCoroutine(HeavySlashRoutine(player, windup));
        }

        private IEnumerator HeavySlashRoutine(Player player, float windup)
        {
            ShowMessage("Heavy Slash");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            if (player == null || player.IsDead())
                yield break;

            Vector3 origin = player.transform.position + Vector3.up * 1.0f;
            Vector3 forward = GetCrosshairDirection(player, origin);
            float range = Mathf.Max(0.5f, _heavyRange.Value);
            float angle = Mathf.Clamp(_heavyAngle.Value, 20f, 180f);
            List<Character> targets = GetConeTargets(player, origin, forward, range, angle);

            for (int i = 0; i < targets.Count; i++)
            {
                DealDamage(player, targets[i], _heavyDamage, 18f);
                DragonCombat.ApplyBrokenBones(targets[i], 6f);
            }

            if (_enableVfx.Value)
                StartCoroutine(AnimateSlashArc(player.transform.position + Vector3.up * 1.0f, forward, range, angle, new Color(1f, 0.58f, 0.18f, 1f)));
        }

        private void CastImpactWave(Player player)
        {
            const string id = "Warrior.ImpactWave";
            if (!BeginCast(player, id, _impactCooldown.Value, _impactStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _impactWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Uppercut", windup + 0.10f);
            StartCoroutine(ImpactWaveRoutine(player, windup));
        }

        private IEnumerator ImpactWaveRoutine(Player player, float windup)
        {
            ShowMessage("Impact Wave");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            if (player == null || player.IsDead())
                yield break;

            float length = Mathf.Max(1f, _impactLength.Value);
            float width = Mathf.Max(0.5f, _impactWidth.Value);
            float travel = Mathf.Max(0.10f, _impactTravelTime.Value);
            Vector3 origin = player.transform.position + Vector3.up * 0.35f;
            Vector3 forward = GetCrosshairDirection(player, origin);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = player.transform.forward;
            forward.Normalize();

            HashSet<Character> damaged = new HashSet<Character>();
            float elapsed = 0f;
            int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");

            while (elapsed <= travel)
            {
                float t = Mathf.Clamp01(elapsed / travel);
                Vector3 probe = origin + forward * Mathf.Lerp(0.5f, length, t);
                RaycastHit ground;
                if (Physics.Raycast(probe + Vector3.up * 3f, Vector3.down, out ground, 8f, mask))
                    probe = ground.point + Vector3.up * 0.18f;

                Collider[] hits = Physics.OverlapBox(probe + Vector3.up * 0.6f, new Vector3(width * 0.5f, 1.5f, 0.55f), Quaternion.LookRotation(forward, Vector3.up));
                for (int i = 0; i < hits.Length; i++)
                {
                    Character target = hits[i].GetComponentInParent<Character>();
                    if (target == null || damaged.Contains(target) || !IsEnemy(player, target))
                        continue;
                    damaged.Add(target);
                    DealDamage(player, target, _impactDamage, 16f);
                }

                if (_enableVfx.Value)
                    StartCoroutine(AnimateRing(probe, 0.12f, Mathf.Max(0.35f, width * 0.55f), 0.18f, new Color(1f, 0.62f, 0.20f, 0.78f), 0.05f, 0f));

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void CastImpactPunch(Player player)
        {
            const string id = "Warrior.ImpactPunch";
            if (!BeginCast(player, id, _punchCooldown.Value, _punchStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _punchWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Punch", windup + 0.10f);
            StartCoroutine(ImpactPunchRoutine(player, windup));
        }

        private IEnumerator ImpactPunchRoutine(Player player, float windup)
        {
            ShowMessage("Impact Punch");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            float range = Mathf.Max(0.5f, _punchRange.Value);
            float width = Mathf.Max(0.5f, _punchWidth.Value);
            Vector3 center = player.transform.position + Vector3.up * 1f + forward * (range * 0.5f);
            Collider[] hits = Physics.OverlapBox(center, new Vector3(width * 0.5f, 1.4f, range * 0.5f), Quaternion.LookRotation(forward, Vector3.up));
            HashSet<Character> damaged = new HashSet<Character>();

            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();
                if (target == null || damaged.Contains(target) || !IsEnemy(player, target))
                    continue;
                damaged.Add(target);
                DealDamage(player, target, _punchDamage, 24f);
                if (DragonCombat.IsSmallEnemy(target))
                    DragonCombat.Stun(target, player.transform.position);
            }

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(player.transform.position + forward * range + Vector3.up * 0.08f, 0.2f, width, 0.24f, new Color(1f, 0.78f, 0.30f, 0.92f), 0.09f, 0f));
        }

        // v0.18.1: entry point for the Skill Tree hotbar (Advanced module).
        public void CastFromHotbar(Player player, string skillId)
        {
            if (player == null || player.IsDead())
                return;
            if (skillId == "lightning_zap")
                CastLightningZap(player);
            else if (skillId == "righteous_strike")
                CastRighteousStrike(player);
            else if (skillId == "holy_wave")
                CastHolyWave(player);
        }

        private void CastLightningZap(Player player)
        {
            const string id = "Cleric.LightningZap";
            if (!BeginCast(player, id, _zapCooldown.Value, _zapStamina.Value))
                return;

            DragonCombat.LockSkill(player, 0.4f);
            DragonCombat.PlaySkillPose(player, "Raise", 0.40f);
            ShowMessage("Lightning Zap");

            Vector3 origin = player.transform.position + Vector3.up * 1.1f;
            Vector3 forward = GetCrosshairDirection(player, origin);
            float range = Mathf.Max(1f, _zapRange.Value);
            float angle = Mathf.Clamp(_zapConeAngle.Value, 10f, 170f);
            List<Character> targets = GetConeTargets(player, origin, forward, range, angle);

            for (int i = 0; i < targets.Count; i++)
            {
                DealDamage(player, targets[i], _zapDamage, 8f);
                DragonCombat.ApplyZap(player, targets[i], _zapExplosionDamage.Value, 0f, 0f);
            }

            if (_enableVfx.Value)
                StartCoroutine(AnimateConeLightning(origin, forward, range, angle));
        }

        private void CastRighteousStrike(Player player)
        {
            const string id = "Cleric.RighteousStrike";
            Vector3 target;
            if (!TryGetPhysicalAimPoint(player, Mathf.Max(1f, _righteousRange.Value), out target))
            {
                ShowMessage("Aim at a physical target");
                return;
            }

            if (!BeginCast(player, id, _righteousCooldown.Value, _righteousStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _righteousWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f);
            StartCoroutine(RighteousStrikeRoutine(player, target, windup));
        }

        private IEnumerator RighteousStrikeRoutine(Player player, Vector3 target, float windup)
        {
            ShowMessage("Righteous Strike");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(target, 8f);
            if (_enableVfx.Value)
                PlayLightningVfx(sky, target, Mathf.Max(0.5f, _righteousRadius.Value));

            List<Character> targets = GetSphereTargets(player, target, Mathf.Max(0.5f, _righteousRadius.Value));
            for (int i = 0; i < targets.Count; i++)
            {
                DealDamage(player, targets[i], _righteousDamage, 14f);
                DragonCombat.ApplyExpose(targets[i], Mathf.Max(0.1f, _righteousExposeDuration.Value));
            }
        }

        private void CastHolyWave(Player player)
        {
            const string id = "Cleric.HolyWave";
            if (!BeginCast(player, id, _holyCooldown.Value, _holyStamina.Value))
                return;

            // v0.17.0: instant cast, 0.5s movement lock, chant animation.
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlaySkillPose(player, "Chant", 0.50f);
            ShowMessage("Holy Wave");
            StartCoroutine(HolyWaveRoutine(player));
        }

        private IEnumerator HolyWaveRoutine(Player caster)
        {
            if (_enableVfx.Value)
            {
                StartCoroutine(AnimateRing(caster.transform.position + Vector3.up * 0.10f, 0.6f, Mathf.Max(1f, _holyRadius.Value), 0.75f, new Color(1f, 0.82f, 0.32f, 0.95f), 0.085f, 0f));
                StartCoroutine(AnimateRing(caster.transform.position + Vector3.up * 0.16f, 0.4f, Mathf.Max(1f, _holyRadius.Value) * 0.72f, 0.60f, new Color(0.75f, 0.95f, 1f, 0.85f), 0.045f, 0.08f));
            }

            float holyPower = DragonCombat.GetSkillPower(caster, "holy_wave");
            HealPlayers(caster.transform.position, _holyRadius.Value, _holyImmediate.Value * holyPower);

            int ticks = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1f, _holyDuration.Value)));
            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(1f);
                Collider[] hits = Physics.OverlapSphere(caster.transform.position, _holyRadius.Value);
                HashSet<Player> healed = new HashSet<Player>();
                for (int j = 0; j < hits.Length; j++)
                {
                    Player ally = hits[j].GetComponentInParent<Player>();
                    if (ally == null || healed.Contains(ally))
                        continue;
                    healed.Add(ally);
                    Heal(ally, ally.GetMaxHealth() * Mathf.Max(0f, _holyPercent.Value) / 100f * holyPower);
                }
            }
        }

        private void CastFlameBurst(Player player)
        {
            const string id = "Sorcerer.FlameBurst";
            if (!BeginCastEitr(player, id, _flameCooldown.Value, _flameEitr.Value)) return;
            DragonCombat.LockSkill(player, 0.4f);
            DragonCombat.PlaySkillPose(player, "Wave", 0.40f);
            ShowMessage("Flame Burst");
            Vector3 origin = player.transform.position + Vector3.up * 1.1f;
            Vector3 forward = GetCrosshairDirection(player, origin);
            List<Character> targets = GetConeTargets(player, origin, forward, Mathf.Max(1f, _flameRange.Value), Mathf.Clamp(_flameConeAngle.Value, 10f, 170f));
            for (int i = 0; i < targets.Count; i++) { DealDamage(player, targets[i], _flameDamage, 7f); StartCoroutine(FireBurnRoutine(player, targets[i], Mathf.Max(0.1f, _flameBurnDuration.Value))); }
            if (_enableVfx.Value) StartCoroutine(AnimateSlashArc(player.transform.position + Vector3.up * 0.9f, forward, Mathf.Max(1f, _flameRange.Value), _flameConeAngle.Value, new Color(1f, 0.28f, 0.05f, 1f)));
        }

        private void CastGlacialDescent(Player player)
        {
            Vector3 target; if (!TryGetPhysicalAimPoint(player, Mathf.Max(1f, _glacialRange.Value), out target)) { ShowMessage("Aim at a physical target"); return; }
            const string id = "Sorcerer.GlacialDescent"; if (!BeginCastEitr(player, id, _glacialCooldown.Value, _glacialEitr.Value)) return;
            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _glacialWindup.Value)); DragonCombat.LockSkill(player, windup); DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f); StartCoroutine(GlacialDescentRoutine(player, target, windup));
        }

        private IEnumerator GlacialDescentRoutine(Player player, Vector3 target, float windup)
        {
            ShowMessage("Glacial Descent"); if (windup > 0f) yield return new WaitForSeconds(windup); if (player == null || player.IsDead()) yield break;
            Vector3 start = DragonCombat.GetIndoorSafeSkyPoint(target, 10f); GameObject chunk = null;
            if (_enableVfx.Value) { chunk = GameObject.CreatePrimitive(PrimitiveType.Cube); chunk.name = "DragonsAltarGlacialDescent"; chunk.transform.position = start; chunk.transform.localScale = new Vector3(4.8f, 3.6f, 4.8f); chunk.transform.rotation = Quaternion.Euler(18f, 28f, 12f); Collider c = chunk.GetComponent<Collider>(); if (c != null) Destroy(c); Renderer r = chunk.GetComponent<Renderer>(); if (r != null) { Shader s = Shader.Find("Sprites/Default"); if (s != null) r.material = new Material(s); if (r.material != null) r.material.color = new Color(0.48f, 0.86f, 1f, 0.86f); } }
            float elapsed = 0f; while (elapsed < 0.45f) { float t = Mathf.Clamp01(elapsed / 0.45f); if (chunk != null) { chunk.transform.position = Vector3.Lerp(start, target + Vector3.up * 1.4f, t); chunk.transform.Rotate(new Vector3(38f, 22f, 17f) * Time.deltaTime); } elapsed += Time.deltaTime; yield return null; } if (chunk != null) Destroy(chunk);
            float radius = Mathf.Max(0.5f, _glacialRadius.Value); List<Character> targets = GetSphereTargets(player, target, radius); for (int i = 0; i < targets.Count; i++) { DealDamage(player, targets[i], _glacialDamage, 18f); DragonCombat.ApplyFrost(targets[i], Mathf.Max(0.1f, _glacialFrostDuration.Value)); }
            if (_enableVfx.Value) StartCoroutine(AnimateRing(target + Vector3.up * 0.08f, 0.4f, radius, 0.55f, new Color(0.50f, 0.90f, 1f, 0.95f), 0.14f, 0f));
        }

        private void CastStonefangEruption(Player player)
        {
            Vector3 target; if (!TryGetPhysicalAimPoint(player, Mathf.Max(1f, _stoneRange.Value), out target)) { ShowMessage("Aim at a physical target"); return; }
            const string id = "Sorcerer.StonefangEruption"; if (!BeginCastEitr(player, id, _stoneCooldown.Value, _stoneEitr.Value)) return; float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _stoneWindup.Value)); DragonCombat.LockSkill(player, windup); DragonCombat.PlaySkillPose(player, "Raise", windup + 0.10f); StartCoroutine(StonefangEruptionRoutine(player, target, windup));
        }

        private IEnumerator StonefangEruptionRoutine(Player player, Vector3 target, float windup)
        {
            ShowMessage("Stonefang Eruption"); if (windup > 0f) yield return new WaitForSeconds(windup); if (player == null || player.IsDead()) yield break; float radius = Mathf.Max(0.5f, _stoneRadius.Value); List<Character> targets = GetSphereTargets(player, target, radius);
            for (int i = 0; i < targets.Count; i++) { Character enemy = targets[i]; DealDamage(player, enemy, _stoneDamage, 20f); if (DragonCombat.IsSmallEnemy(enemy)) DragonCombat.Stun(enemy, player.transform.position); if (!enemy.IsBoss()) DragonCombat.ApplyCripple(enemy, Mathf.Max(0.1f, _stoneCrippleDuration.Value)); }
            if (_enableVfx.Value) { for (int i = 0; i < 9; i++) { float a = ((float)i / 9f) * Mathf.PI * 2f; float d = i == 0 ? 0f : radius * (0.35f + 0.55f * ((float)(i % 3) / 2f)); StartCoroutine(AnimateStoneSpike(target + new Vector3(Mathf.Cos(a)*d, 0.1f, Mathf.Sin(a)*d), 0.30f + 0.04f*i)); } StartCoroutine(AnimateRing(target + Vector3.up*0.06f, 0.3f, radius, 0.45f, new Color(0.62f,0.48f,0.32f,0.90f),0.12f,0f)); }
        }

        private IEnumerator AnimateStoneSpike(Vector3 point, float duration)
        {
            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube); spike.name = "DragonsAltarStonefangSpike"; spike.transform.position = point + Vector3.down * 2f; spike.transform.localScale = new Vector3(0.75f,3.4f,0.75f); spike.transform.rotation = Quaternion.Euler(0f,UnityEngine.Random.Range(0f,360f),12f); Collider c = spike.GetComponent<Collider>(); if (c != null) Destroy(c); Renderer r = spike.GetComponent<Renderer>(); if (r != null) { Shader s = Shader.Find("Sprites/Default"); if (s != null) r.material = new Material(s); if (r.material != null) r.material.color = new Color(0.42f,0.34f,0.27f,0.94f); } float e = 0f; float safe = Mathf.Max(0.12f,duration); while (e < safe) { float t = Mathf.Clamp01(e/safe); spike.transform.position = Vector3.Lerp(point+Vector3.down*2f, point+Vector3.up*1.3f,t); e += Time.deltaTime; yield return null; } yield return new WaitForSeconds(0.22f); Destroy(spike);
        }

        private IEnumerator FireBurnRoutine(Player attacker, Character target, float duration)
        {
            float end = Time.time + Mathf.Max(0.1f,duration); while (Time.time < end) { if (target == null || target.IsDead()) yield break; DragonCombat.ApplyFireBurnTick(attacker,target,1f); yield return new WaitForSeconds(1f); }
        }

        private List<Character> GetSphereTargets(Player player, Vector3 center, float radius)
        {
            List<Character> result = new List<Character>(); HashSet<Character> seen = new HashSet<Character>(); Collider[] hits = Physics.OverlapSphere(center, Mathf.Max(0.1f,radius)); for (int i=0;i<hits.Length;i++) { Character target = hits[i].GetComponentInParent<Character>(); if (target == null || seen.Contains(target) || !IsEnemy(player,target)) continue; seen.Add(target); result.Add(target); } return result;
        }

        private List<Character> GetConeTargets(Player player, Vector3 origin, Vector3 forward, float range, float angle)
        {
            List<Character> result = new List<Character>();
            HashSet<Character> seen = new HashSet<Character>();

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = player.transform.forward;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;

            forward.Normalize();

            // Unity/Valheim world units are meters. Query exactly the configured reach.
            Collider[] hits = Physics.OverlapSphere(origin, range);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i];
                Character target = collider.GetComponentInParent<Character>();

                if (target == null || seen.Contains(target) || !IsEnemy(player, target))
                    continue;

                // Measure reach to the target's COLLIDER edge, not its transform pivot.
                // This matters hugely for Troll-sized enemies.
                Vector3 closest = collider.ClosestPoint(origin);
                Vector3 horizontalClosest = closest - origin;
                horizontalClosest.y = 0f;

                if (horizontalClosest.sqrMagnitude > range * range)
                    continue;

                // Use the collider's actual visible center for the frontal arc test.
                Vector3 directionPoint = collider.bounds.center - origin;
                directionPoint.y = 0f;

                if (directionPoint.sqrMagnitude < 0.001f)
                    directionPoint = horizontalClosest;

                if (directionPoint.sqrMagnitude < 0.001f)
                {
                    seen.Add(target);
                    result.Add(target);
                    continue;
                }

                if (Vector3.Angle(forward, directionPoint.normalized) > angle * 0.5f)
                    continue;

                seen.Add(target);
                result.Add(target);
            }

            return result;
        }

        private IEnumerator AnimateSlashArc(Vector3 center, Vector3 forward, float radius, float angle, Color color)
        {
            GameObject obj = new GameObject("DragonsAltarSlashArc");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 31;
            line.startWidth = 0.16f;
            line.endWidth = 0.05f;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            Vector3 flat = forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f)
                flat = Vector3.forward;
            flat.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flat).normalized;
            float elapsed = 0f;
            float duration = 0.28f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                Color frame = color;
                frame.a = 1f - t;
                line.startColor = frame;
                line.endColor = frame;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float p = (float)i / (float)(line.positionCount - 1);
                    float a = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, p) * Mathf.Deg2Rad;
                    Vector3 dir = flat * Mathf.Cos(a) + right * Mathf.Sin(a);
                    line.SetPosition(i, center + dir * radius + Vector3.up * Mathf.Sin(p * Mathf.PI) * 0.35f);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(obj);
        }

        private IEnumerator AnimateConeLightning(Vector3 origin, Vector3 forward, float range, float angle)
        {
            int bolts = 5;
            for (int b = 0; b < bolts; b++)
            {
                float p = bolts <= 1 ? 0.5f : (float)b / (float)(bolts - 1);
                float yaw = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, p);
                Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * forward.normalized;
                Vector3 end = origin + dir * range;
                PlayLightningVfx(origin + Vector3.up * 0.4f, end, 0.8f);
                yield return new WaitForSeconds(0.025f);
            }
        }

        private void PlayLightningVfx(Vector3 start, Vector3 end, float radius)
        {
            GameObject flash = new GameObject("DragonsAltarLightning");
            LineRenderer line = flash.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 8;
            line.startWidth = 0.12f;
            line.endWidth = 0.20f;
            line.startColor = new Color(0.92f, 0.98f, 1f, 1f);
            line.endColor = new Color(0.42f, 0.74f, 1f, 1f);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = (float)i / (float)(line.positionCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                float offset = Mathf.Sin((float)i * 5.37f + Time.time * 9f) * 0.25f;
                point.x += offset;
                point.z -= offset * 0.55f;
                line.SetPosition(i, point);
            }
            Destroy(flash, 0.22f);
            StartCoroutine(AnimateRing(end + Vector3.up * 0.08f, 0.2f, Mathf.Max(0.4f, radius), 0.30f, new Color(0.55f, 0.84f, 1f, 0.92f), 0.07f, 0f));
        }

        private void HealPlayers(Vector3 center, float radius, float amount)
        {
            radius = Mathf.Max(0.1f, radius);
            Collider[] hits = Physics.OverlapSphere(center, radius);
            HashSet<Player> healed = new HashSet<Player>();

            for (int i = 0; i < hits.Length; i++)
            {
                Player ally = hits[i].GetComponentInParent<Player>();
                if (ally == null || healed.Contains(ally))
                    continue;

                healed.Add(ally);
                Heal(ally, amount);
            }
        }

        private bool BeginCastEitr(Player player, string id, float cooldown, float eitrCost)
        {
            if (player == null || player.IsDead()) return false;
            if (_testingForceCooldowns.Value)
                cooldown = Mathf.Max(0f, _testingCooldownSeconds.Value);

            float remaining = CooldownRemaining(id);
            if (remaining > 0f) { ShowMessage("Cooldown: " + remaining.ToString("0.0") + "s"); return false; }

            float cost = Mathf.Max(0f, eitrCost);
            try
            {
                MethodInfo getEitr = typeof(Player).GetMethod("GetEitr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo useEitr = typeof(Player).GetMethod("UseEitr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (getEitr == null || useEitr == null)
                {
                    Logger.LogWarning("Could not resolve GetEitr/UseEitr; Sorcerer cast cancelled.");
                    return false;
                }
                float current = Convert.ToSingle(getEitr.Invoke(player, null));
                if (current < cost) { ShowMessage("Not enough Eitr"); return false; }
                useEitr.Invoke(player, new object[] { cost });
                DragonCombat.BlockEitrRegen(player, 0.20f);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Sorcerer Eitr cast cancelled: " + ex.Message);
                return false;
            }

            _cooldowns[id] = Time.time + Mathf.Max(0f, cooldown);
            return true;
        }

        private bool BeginCast(Player player, string id, float cooldown, float staminaCost)
        {
            if (_testingForceCooldowns.Value)
                cooldown = Mathf.Max(0f, _testingCooldownSeconds.Value);

            cooldown = Mathf.Max(0f, cooldown);
            staminaCost = Mathf.Max(0f, staminaCost);

            float remaining = CooldownRemaining(id);
            if (remaining > 0f)
            {
                ShowMessage("Cooldown: " + remaining.ToString("0.0") + "s");
                return false;
            }

            if (GetStamina(player) < staminaCost)
            {
                ShowMessage("Not enough stamina");
                return false;
            }

            UseStamina(player, staminaCost);
            _cooldowns[id] = Time.time + Mathf.Max(0f, cooldown);
            return true;
        }

        private float CooldownRemaining(string id)
        {
            float endTime;
            if (!_cooldowns.TryGetValue(id, out endTime))
                return 0f;

            return Mathf.Max(0f, endTime - Time.time);
        }

        private void DamageSphere(Player attacker, Vector3 center, float radius, DamageConfig damage, float push)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius);
            HashSet<Character> damaged = new HashSet<Character>();

            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();
                if (target == null || damaged.Contains(target) || !IsEnemy(attacker, target))
                    continue;

                damaged.Add(target);
                DealDamage(attacker, target, damage, push);
            }
        }

        private bool IsEnemy(Player attacker, Character target)
        {
            if (target == null || target == attacker || target.IsDead())
                return false;

            if (target is Player)
                return false;

            try
            {
                MethodInfo isTamed = target.GetType().GetMethod("IsTamed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (isTamed != null && Convert.ToBoolean(isTamed.Invoke(target, null)))
                    return false;
            }
            catch
            {
            }

            return true;
        }

        private void DealDamage(Player attacker, Character target, DamageConfig cfg, float push)
        {
            float magic = DragonCombat.GetSorcererMagicDamageMultiplier(attacker);
            // v0.18.0: Tier power for the Cleric Class skills.
            if (cfg == _zapDamage)
                magic *= DragonCombat.GetSkillPower(attacker, "lightning_zap");
            else if (cfg == _righteousDamage)
                magic *= DragonCombat.GetSkillPower(attacker, "righteous_strike");
            HitData hit = new HitData();
            hit.m_damage.m_blunt = cfg.Blunt.Value * magic;
            hit.m_damage.m_slash = cfg.Slash.Value * magic;
            hit.m_damage.m_pierce = cfg.Pierce.Value * magic;
            hit.m_damage.m_fire = cfg.Fire.Value * magic;
            hit.m_damage.m_frost = cfg.Frost.Value * magic;
            hit.m_damage.m_lightning = cfg.Lightning.Value * magic;
            hit.m_damage.m_poison = cfg.Poison.Value * magic;
            hit.m_damage.m_spirit = cfg.Spirit.Value * magic;
            hit.m_point = target.transform.position;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = push;
            hit.SetAttacker(attacker);
            target.Damage(hit);
        }

        private bool TryGetPhysicalAimPoint(Player player, float range, out Vector3 point)
        {
            return AlbedoAimUtility.TryGetPhysicalTarget(player, range, out point);
        }

        private Vector3 GetCrosshairDirection(Player player, Vector3 origin)
        {
            return AlbedoAimUtility.GetProjectileDirection(player, origin);
        }

        private Vector3 GetAimPoint(Player player, float range)
        {
            Vector3 target;

            if (AlbedoAimUtility.TryGetPhysicalTarget(player, range, out target))
                return target;

            Vector3 origin = player == null ? Vector3.zero : player.GetEyePoint();
            return AlbedoAimUtility.GetFarCrosshairPoint(player, origin, range);
        }

        private void PlaySmiteVfx(Vector3 point, float radius)
        {
            GameObject flash = new GameObject("DragonsAltarLegacyLightning");
            LineRenderer line = flash.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 8;
            line.startWidth = 0.13f;
            line.endWidth = 0.28f;
            line.startColor = new Color(0.92f, 0.98f, 1f, 1f);
            line.endColor = new Color(0.45f, 0.75f, 1f, 1f);

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            Vector3 top = point + Vector3.up * 10f;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = (float)i / (float)(line.positionCount - 1);
                float offset = Mathf.Sin((float)i * 5.37f + Time.time * 9f) * 0.34f;
                Vector3 position = Vector3.Lerp(top, point, t);
                position.x += offset;
                position.z -= offset * 0.55f;
                line.SetPosition(i, position);
            }

            Destroy(flash, 0.22f);

            StartCoroutine(AnimateRing(point + Vector3.up * 0.08f, 0.25f, radius, 0.35f, new Color(0.55f, 0.84f, 1f, 1f), 0.09f, 0f));
            StartCoroutine(AnimateRing(point + Vector3.up * 0.11f, 0.18f, radius * 0.68f, 0.27f, new Color(1f, 0.86f, 0.38f, 0.9f), 0.045f, 0.05f));
        }

        private IEnumerator AnimateRing(Vector3 center, float startRadius, float endRadius, float duration, Color color, float width, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            GameObject ringObject = new GameObject("AlbedoAbilityRing");
            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 49;
            line.startWidth = width;
            line.endWidth = width;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.05f, duration);

            while (elapsed < safeDuration)
            {
                float t = elapsed / safeDuration;
                float grow = Mathf.Clamp01(t / 0.22f);
                float radius = Mathf.Lerp(startRadius, endRadius, Mathf.SmoothStep(0f, 1f, grow));
                Color frameColor = color;
                float fade = t <= 0.72f ? 1f : Mathf.Clamp01(1f - (t - 0.72f) / 0.28f);
                frameColor.a = color.a * fade;
                line.startColor = frameColor;
                line.endColor = frameColor;

                for (int i = 0; i < line.positionCount; i++)
                {
                    float angle = ((float)i / (float)(line.positionCount - 1)) * Mathf.PI * 2f;
                    line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(ringObject);
        }

        private IEnumerator AnimateImpactWave(Vector3 origin, Vector3 forward, float length, float width)
        {
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;

            forward.Normalize();

            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;

            if (side.sqrMagnitude < 0.01f)
                side = Vector3.right;

            GameObject waveObject = new GameObject("AlbedoImpactWave");
            LineRenderer line = waveObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 7;
            line.startWidth = 0.13f;
            line.endWidth = 0.13f;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            float duration = 0.33f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float distance = Mathf.Lerp(0.8f, length, t);
                float currentWidth = Mathf.Lerp(width * 0.45f, width, t);
                Vector3 center = origin + forward * distance;

                Color color = new Color(1f, 0.62f, 0.22f, 1f - t * 0.72f);
                line.startColor = color;
                line.endColor = new Color(1f, 0.93f, 0.62f, color.a);

                for (int i = 0; i < line.positionCount; i++)
                {
                    float p = ((float)i / (float)(line.positionCount - 1)) * 2f - 1f;
                    float height = (1f - Mathf.Abs(p)) * 1.25f;
                    line.SetPosition(i, center + side * (p * currentWidth * 0.5f) + Vector3.up * height);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(waveObject);
        }

        private void OnGUI()
        {
            // Unified v0.6 HUD is rendered by the isolated advancement plugin.
        }

        public float GetCooldownForUi(string id)
        {
            return CooldownRemaining(id);
        }

        public KeyCode GetModifierKeyForUi()
        {
            return _modifier.Value;
        }

        public KeyCode GetSkill1KeyForUi()
        {
            return _skill1.Value;
        }

        public KeyCode GetSkill2KeyForUi()
        {
            return _skill2.Value;
        }

        public KeyCode GetSkill3KeyForUi()
        {
            return _skill3.Value;
        }

        private void EnsureHudStyles()
        {
            if (_hudTitleStyle != null)
                return;

            _hudBackground = MakeTexture(new Color(0.04f, 0.045f, 0.055f, 0.86f));
            _hudReadyBackground = MakeTexture(new Color(0.12f, 0.16f, 0.15f, 0.92f));
            _hudCooldownBackground = MakeTexture(new Color(0.10f, 0.10f, 0.12f, 0.94f));

            _hudTitleStyle = new GUIStyle(GUI.skin.label);
            _hudTitleStyle.alignment = TextAnchor.MiddleCenter;
            _hudTitleStyle.fontStyle = FontStyle.Bold;
            _hudTitleStyle.fontSize = 18;

            _hudSkillStyle = new GUIStyle(GUI.skin.box);
            _hudSkillStyle.alignment = TextAnchor.MiddleLeft;
            _hudSkillStyle.fontStyle = FontStyle.Bold;
            _hudSkillStyle.fontSize = 15;
            _hudSkillStyle.normal.textColor = Color.white;
            _hudSkillStyle.padding = new RectOffset(12, 10, 7, 6);
            _hudSkillStyle.normal.background = _hudBackground;

            _hudSmallStyle = new GUIStyle(GUI.skin.label);
            _hudSmallStyle.alignment = TextAnchor.MiddleCenter;
            _hudSmallStyle.fontSize = 13;
            _hudSmallStyle.normal.textColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void DrawSkillSlot(Rect rect, string hotkey, string skillName, float cooldown)
        {
            Texture2D oldBackground = _hudSkillStyle.normal.background;

            if (cooldown <= 0.01f)
                _hudSkillStyle.normal.background = _hudReadyBackground;
            else
                _hudSkillStyle.normal.background = _hudCooldownBackground;

            string status = cooldown <= 0.01f ? "READY" : cooldown.ToString("0.0") + "s";
            GUI.Box(rect, hotkey + "   " + skillName + "\\n" + status, _hudSkillStyle);

            _hudSkillStyle.normal.background = oldBackground;
        }

        private string FormatHotkey(KeyCode modifier, KeyCode key)
        {
            string modifierText = modifier.ToString();

            // Unity names the first two side buttons Mouse3 and Mouse4.
            // Most players call those physical buttons Mouse4 and Mouse5.
            if (modifier == KeyCode.Mouse3)
                modifierText = "M4";
            else if (modifier == KeyCode.Mouse4)
                modifierText = "M5";

            string keyText = key.ToString();
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
                keyText = ((int)key - (int)KeyCode.Alpha0).ToString();

            return modifierText + " + " + keyText;
        }

        private void Heal(Character target, float amount)
        {
            if (target == null || amount <= 0f)
                return;

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

        private float GetStamina(Player player)
        {
            try
            {
                MethodInfo method = typeof(Player).GetMethod("GetStamina", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    return Convert.ToSingle(method.Invoke(player, null));
            }
            catch
            {
            }

            FieldInfo field = typeof(Player).GetField("m_stamina", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return 0f;

            return Convert.ToSingle(field.GetValue(player));
        }

        private float GetMaxStamina(Player player)
        {
            try
            {
                MethodInfo method = typeof(Player).GetMethod("GetMaxStamina", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    return Convert.ToSingle(method.Invoke(player, null));
            }
            catch
            {
            }

            return 100f;
        }

        private void SetStamina(Player player, float value)
        {
            FieldInfo field = typeof(Player).GetField("m_stamina", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(player, value);
        }

        private void UseStamina(Player player, float amount)
        {
            try
            {
                MethodInfo[] methods = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "UseStamina")
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
                            args[j] = false;
                        else if (parameters[j].ParameterType.IsValueType)
                            args[j] = Activator.CreateInstance(parameters[j].ParameterType);
                        else
                            args[j] = null;
                    }

                    method.Invoke(player, args);
                    DragonCombat.BlockStaminaRegen(player, 0.20f);
                    return;
                }
            }
            catch
            {
            }

            SetStamina(player, Mathf.Max(0f, GetStamina(player) - amount));
            DragonCombat.BlockStaminaRegen(player, 0.20f);
        }

        private string GetSelectedClass(Player player)
        {
            IDictionary data = GetCustomData(player);
            if (data == null || !data.Contains(ClassDataKey))
                return "";

            object value = data[ClassDataKey];
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

        private void ShowMessage(string message)
        {
            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
        }

        private class DamageConfig
        {
            public ConfigEntry<float> Blunt;
            public ConfigEntry<float> Slash;
            public ConfigEntry<float> Pierce;
            public ConfigEntry<float> Fire;
            public ConfigEntry<float> Frost;
            public ConfigEntry<float> Lightning;
            public ConfigEntry<float> Poison;
            public ConfigEntry<float> Spirit;
        }
    }
}
