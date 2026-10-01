using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using DragonsAltarCombat;
using AlbedosCustomClassesSkills;
using AlbedosCustomClasses;

namespace DragonsAltarSorcerer
{
    internal class RuptureState
    {
        public int DamageStack;
        public int StunCycleHits;
    }

    internal class GunStaffTimingState
    {
        public Attack Attack;
        public FieldInfo ChargeField;
        public float ChargeValue;
        public FieldInfo DrawField;
        public float DrawValue;
        public FieldInfo WarmupField;
        public float WarmupValue;
        public FieldInfo CooldownField;
        public float CooldownValue;
        public FieldInfo SpeedFactorField;
        public float SpeedFactorValue;
        public FieldInfo SpeedFactorRotationField;
        public float SpeedFactorRotationValue;
        public FieldInfo AccuracyField;
        public float AccuracyValue;
        public FieldInfo AccuracyMinField;
        public float AccuracyMinValue;
    }

    internal class MagicDamageSnapshot
    {
        public float Blunt;
        public float Slash;
        public float Pierce;
        public float Fire;
        public float Frost;
        public float Lightning;
        public float Poison;
        public float Spirit;

        public float Total()
        {
            return Blunt + Slash + Pierce + Fire + Frost + Lightning + Poison + Spirit;
        }
    }

    internal class PortalLink : MonoBehaviour, Hoverable, Interactable
    {
        public SorcererPlugin Plugin;
        public PortalLink Other;
        public float Radius = 1.25f;

        public float GetHoverOffset()
        {
            return 0f;
        }

        public string GetHoverName()
        {
            return "Twin Rift";
        }

        public string GetHoverText()
        {
            if (Plugin == null || Other == null)
                return "Twin Rift";
            return "[<color=yellow><b>E</b></color>] Use Rift";
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || Plugin == null || Other == null)
                return false;

            Player player = user as Player;
            if (player == null || player != Player.m_localPlayer)
                return false;

            if (Time.time < Plugin.PortalReentryUntil)
                return false;

            Plugin.TraversePortal(player, Other.transform.position);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }

    internal class FeatherFallController : MonoBehaviour
    {
        private Player _player;
        private Rigidbody _body;
        private FieldInfo _maxAirAltitude;
        private MethodInfo _isOnGround;
        private float _started;

        public void Begin(Player player)
        {
            _player = player;
            _body = player == null ? null : player.GetComponent<Rigidbody>();
            _started = Time.time;
            _maxAirAltitude = typeof(Character).GetField("m_maxAirAltitude", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _isOnGround = typeof(Character).GetMethod("IsOnGround", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private void FixedUpdate()
        {
            if (_player == null || _player.IsDead())
            {
                Destroy(this);
                return;
            }

            if (_body != null)
            {
                Vector3 velocity = _body.velocity;
                if (velocity.y < -2.5f)
                {
                    velocity.y = -2.5f;
                    _body.velocity = velocity;
                }
            }

            if (_maxAirAltitude != null)
            {
                try { _maxAirAltitude.SetValue(_player, _player.transform.position.y); }
                catch { }
            }

            if (Time.time - _started < 0.35f)
                return;

            bool grounded = false;
            if (_isOnGround != null)
            {
                try { grounded = Convert.ToBoolean(_isOnGround.Invoke(_player, null)); }
                catch { }
            }

            if (grounded)
                Destroy(this);
        }
    }

    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("albedo.customclasses", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.skills", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.combatruntime", BepInDependency.DependencyFlags.HardDependency)]
    public class SorcererPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "albedo.customclasses.sorcerer";
        public const string ModName = "Dragon's Altar - Sorcerer Advancements";
        public const string ModVersion = "0.14.7";

        private const string ClassDataKey = "AlbedoCustomClasses.Class";
        private const string AdvancementDataKey = "AlbedoCustomClasses.Advancement";

        public static SorcererPlugin Instance;

        private ConfigEntry<KeyCode> _modifier;
        private ConfigEntry<KeyCode> _skill1;
        private ConfigEntry<KeyCode> _skill2;
        private ConfigEntry<KeyCode> _skill3;
        private ConfigEntry<KeyCode> _skill4;
        private ConfigEntry<KeyCode> _skill5;
        private ConfigEntry<KeyCode> _skill6;
        private ConfigEntry<KeyCode> _skill7;
        private ConfigEntry<KeyCode> _skill8;
        private ConfigEntry<KeyCode> _skill9;
        private ConfigEntry<KeyCode> _passive;

        private ConfigEntry<bool> _enableVfx;
        private ConfigEntry<bool> _showHud;
        private ConfigEntry<float> _hudScale;
        private ConfigEntry<float> _hudBottomOffset;
        private ConfigEntry<bool> _testingForceCooldowns;
        private ConfigEntry<float> _testingCooldown;

        private ConfigEntry<float> _flameCooldown;
        private ConfigEntry<float> _flameEitr;
        private ConfigEntry<float> _flameRange;
        private ConfigEntry<float> _flameAngle;

        private ConfigEntry<float> _iceCooldown;
        private ConfigEntry<float> _iceEitr;
        private ConfigEntry<float> _iceRadius;
        private ConfigEntry<float> _iceRange;

        private ConfigEntry<float> _stoneCooldown;
        private ConfigEntry<float> _stoneEitr;
        private ConfigEntry<float> _stoneRadius;
        private ConfigEntry<float> _stoneRange;

        private ConfigEntry<float> _gravityCooldown;
        private ConfigEntry<float> _gravityEitr;
        private ConfigEntry<float> _gravityRadius;
        private ConfigEntry<float> _gravityDuration;

        private ConfigEntry<float> _bladeCooldown;
        private ConfigEntry<float> _bladeEitr;
        private ConfigEntry<float> _bladeRange;
        private ConfigEntry<float> _bladeWidth;
        private ConfigEntry<float> _bladeChargeMax;

        private ConfigEntry<float> _novaCooldown;
        private ConfigEntry<float> _novaEitr;
        private ConfigEntry<float> _novaRadius;

        private ConfigEntry<float> _meteorCooldown;
        private ConfigEntry<float> _meteorEitr;
        private ConfigEntry<float> _meteorRadius;
        private ConfigEntry<float> _meteorRange;

        private ConfigEntry<float> _cataclysmCooldown;
        private ConfigEntry<float> _cataclysmEitr;
        private ConfigEntry<float> _cataclysmRadius;
        private ConfigEntry<float> _cataclysmRange;
        private ConfigEntry<float> _cataclysmCharge;
        private ConfigEntry<float> _railCooldown;
        private ConfigEntry<float> _railEitr;
        private ConfigEntry<float> _railRange;
        private ConfigEntry<float> _railWidth;
        private ConfigEntry<float> _railWindup;

        private ConfigEntry<float> _wizardHeldStaffEitrRegen;
        private ConfigEntry<float> _wizardHeldStaffFlatEitr;
        private ConfigEntry<float> _wizardChargeThreshold;
        private ConfigEntry<float> _wizardChargeBaseRadius;
        private ConfigEntry<float> _wizardChargeExtraEitrPerSecond;
        private ConfigEntry<float> _wizardChargeMaxDamageMultiplier;
        private ConfigEntry<float> _wizardChargeRange;

        private ConfigEntry<float> _voidCooldown;
        private ConfigEntry<float> _voidGroundRange;
        private ConfigEntry<float> _voidFreeRange;

        private ConfigEntry<float> _phalanxCooldown;
        private ConfigEntry<float> _phalanxEitr;
        private ConfigEntry<int> _phalanxCount;

        private ConfigEntry<int> _ruptureMaxCharges;
        private ConfigEntry<float> _ruptureRecharge;
        private ConfigEntry<float> _ruptureRadius;
        private ConfigEntry<float> _ruptureRange;
        private ConfigEntry<float> _ruptureWindup;
        private ConfigEntry<float> _ruptureBuffer;

        private ConfigEntry<float> _riftDuration;
        private ConfigEntry<float> _riftRange;
        private ConfigEntry<float> _phaseDuration;
        private ConfigEntry<float> _riftEchoCooldown;
        private ConfigEntry<float> _riftEchoEitr;
        private ConfigEntry<float> _riftEchoDuration;
        private ConfigEntry<float> _riftEchoDamageMultiplier;
        private ConfigEntry<float> _afterimageCooldown;
        private ConfigEntry<float> _afterimageEitr;
        private ConfigEntry<float> _afterimageDuration;
        private ConfigEntry<int> _afterimageMax;
        private ConfigEntry<float> _afterimageDamageMultiplier;
        private ConfigEntry<float> _gunStaffFireRateMultiplier;
        private ConfigEntry<float> _dualGunStaffAttackSpeedMultiplier;
        private ConfigEntry<float> _gunStaffFiringMoveBonus;

        private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        private readonly Dictionary<int, RuptureState> _ruptureStates = new Dictionary<int, RuptureState>();
        private readonly List<float> _ruptureRechargeAt = new List<float>();
        private readonly List<GameObject> _phalanxSwords = new List<GameObject>();
        private readonly List<GameObject> _spellcasterAfterimages = new List<GameObject>();
        private readonly Dictionary<Attack, GunStaffTimingState> _gunStaffTimingStates = new Dictionary<Attack, GunStaffTimingState>();

        private Harmony _harmony;
        private float _overchargeEitrSpent;
        private float _overchargeUntil;
        private bool _wizardChargeActive;
        private float _wizardChargeStarted;
        private float _wizardChargeNextSpend;
        private float _wizardChargeExtraEitrSpent;
        private ItemDrop.ItemData _wizardChargeWeapon;
        private bool _phalanxVolleyArmed;
        private int _ruptureCharges;
        private float _ruptureNextCastAt;
        private GameObject _riftA;
        private GameObject _riftB;
        private bool _riftAwaitingB;
        private float _riftEndTime;
        private float _phaseFlowUntil;
        private float _riftEchoUntil;
        private float _afterimageUntil;
        private float _lastEchoShot;
        private float _nextSpellcasterHeldFire;
        private int _wizardChargeStacks;
        private float _suppressWizardVanillaUntil;
        private bool _wizardStaffBonusActive;

        public float PortalReentryUntil;

        private GUIStyle _hudStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _hudIconStyle;
        private GUIStyle _hudKeyStyle;
        private GUIStyle _hudCooldownStyle;

        private void Awake()
        {
            Instance = this;

            _modifier = Config.Bind("Hotkeys", "Modifier", KeyCode.Mouse3, "Mouse4 modifier.");
            _skill1 = Config.Bind("Hotkeys", "Skill1", KeyCode.Alpha1, "Sorcerer base skill 1.");
            _skill2 = Config.Bind("Hotkeys", "Skill2", KeyCode.Alpha2, "Sorcerer base skill 2.");
            _skill3 = Config.Bind("Hotkeys", "Skill3", KeyCode.Alpha3, "Sorcerer base skill 3.");
            _skill4 = Config.Bind("Hotkeys", "Skill4", KeyCode.Alpha4, "Advancement skill 4.");
            _skill5 = Config.Bind("Hotkeys", "Skill5", KeyCode.Alpha5, "Advancement skill 5.");
            _skill6 = Config.Bind("Hotkeys", "Skill6", KeyCode.Alpha6, "Advancement skill 6.");
            _skill7 = Config.Bind("Hotkeys", "Skill7", KeyCode.Alpha7, "Advancement skill 7.");
            _skill8 = Config.Bind("Hotkeys", "Skill8", KeyCode.Alpha8, "Spellcaster Ultimate or Wizard fifth advancement skill.");
            _skill9 = Config.Bind("Hotkeys", "Skill9", KeyCode.Alpha9, "Wizard Ultimate while all development skills are exposed.");
            _passive = Config.Bind("Hotkeys", "PassiveActiveKey", KeyCode.R, "Advancement activatable passive.");

            _enableVfx = Config.Bind("General", "EnableVfx", true, "Enable prototype Sorcerer VFX.");
            _showHud = Config.Bind("General", "ShowSorcererHud", true, "Show the clean dynamic Sorcerer / Wizard / Spellcaster HUD.");
            _hudScale = Config.Bind("Interface", "HudScale", 1f, "Unified Dragon's Altar skill HUD scale.");
            _hudBottomOffset = Config.Bind("Interface", "HudBottomOffset_v0113", 105f, "Bottom margin for the compact RPG skill HUD. Fresh v0.11.3 key avoids stale 330px development offsets.");
            _testingForceCooldowns = Config.Bind("Testing", "ForceCooldowns", true, "Force ordinary Sorcerer cooldowns to the testing value.");
            _testingCooldown = Config.Bind("Testing", "CooldownSeconds", 5f, "Testing cooldown.");

            _flameCooldown = Config.Bind("Sorcerer Flame Burst", "Cooldown", 7f, "Seconds.");
            _flameEitr = Config.Bind("Sorcerer Flame Burst", "EitrCost", 18f, "Eitr cost.");
            _flameRange = Config.Bind("Sorcerer Flame Burst", "Range", 10f, "Cone range in meters.");
            _flameAngle = Config.Bind("Sorcerer Flame Burst", "ConeDegrees", 70f, "Cone width.");

            _iceCooldown = Config.Bind("Sorcerer Glacial Descent", "Cooldown", 9f, "Seconds.");
            _iceEitr = Config.Bind("Sorcerer Glacial Descent", "EitrCost", 24f, "Eitr cost.");
            _iceRadius = Config.Bind("Sorcerer Glacial Descent", "Radius", 5f, "Literal 5m radius.");
            _iceRange = Config.Bind("Sorcerer Glacial Descent", "CastRange", 50f, "Ground PAC cast range.");

            _stoneCooldown = Config.Bind("Sorcerer Stonefang Eruption", "Cooldown", 9f, "Seconds.");
            _stoneEitr = Config.Bind("Sorcerer Stonefang Eruption", "EitrCost", 22f, "Eitr cost.");
            _stoneRadius = Config.Bind("Sorcerer Stonefang Eruption", "Radius", 5f, "Literal 5m radius.");
            _stoneRange = Config.Bind("Sorcerer Stonefang Eruption", "CastRange", 35f, "Ground PAC cast range.");

            _gravityCooldown = Config.Bind("Wizard Gravity Dominion", "Cooldown", 14f, "Seconds.");
            _gravityEitr = Config.Bind("Wizard Gravity Dominion", "EitrCost", 35f, "Eitr cost.");
            _gravityRadius = Config.Bind("Wizard Gravity Dominion", "Radius", 7f, "Literal 7m radius.");
            _gravityDuration = Config.Bind("Wizard Gravity Dominion", "Duration", 5f, "Seconds.");

            _bladeCooldown = Config.Bind("Wizard Astral Greatblade", "Cooldown", 12f, "Seconds.");
            _bladeEitr = Config.Bind("Wizard Astral Greatblade", "EitrCost", 35f, "Eitr cost.");
            _bladeRange = Config.Bind("Wizard Astral Greatblade", "Range", 15f, "Literal 15m range.");
            _bladeWidth = Config.Bind("Wizard Astral Greatblade", "Width", 2f, "Literal 2m width.");
            _bladeChargeMax = Config.Bind("Wizard Astral Greatblade", "AdditionalChargeTime", 2f, "Additional charge after the mandatory 1s windup.");

            _novaCooldown = Config.Bind("Wizard Frost Nova", "Cooldown", 14f, "Seconds.");
            _novaEitr = Config.Bind("Wizard Frost Nova", "EitrCost", 38f, "Eitr cost.");
            _novaRadius = Config.Bind("Wizard Frost Nova", "Radius", 10f, "Literal 10m radius.");

            _meteorCooldown = Config.Bind("Wizard Meteor Fall", "Cooldown", 18f, "Seconds.");
            _meteorEitr = Config.Bind("Wizard Meteor Fall", "EitrCost", 45f, "Eitr cost.");
            _meteorRadius = Config.Bind("Wizard Meteor Fall", "Radius", 7f, "Toned-down meteor radius.");
            _meteorRange = Config.Bind("Wizard Meteor Fall", "CastRange", 50f, "Ground PAC range.");

            _cataclysmCooldown = Config.Bind("Wizard Elemental Cataclysm", "Cooldown", 75f, "Seconds.");
            _cataclysmEitr = Config.Bind("Wizard Elemental Cataclysm", "EitrCost", 80f, "Eitr cost.");
            _cataclysmRadius = Config.Bind("Wizard Elemental Cataclysm", "Radius", 10f, "Ultimate radius.");
            _cataclysmRange = Config.Bind("Wizard Elemental Cataclysm", "CastRange", 50f, "Ground PAC range.");
            _cataclysmCharge = Config.Bind("Wizard Elemental Cataclysm", "MaxChargeTime", 6f, "Maximum total charge time.");
            _railCooldown = Config.Bind("Wizard Astral Railcannon", "Cooldown", 22f, "Seconds.");
            _railEitr = Config.Bind("Wizard Astral Railcannon", "EitrCost", 65f, "Eitr cost.");
            _railRange = Config.Bind("Wizard Astral Railcannon", "Range", 50f, "Laser AoE range.");
            _railWidth = Config.Bind("Wizard Astral Railcannon", "Width", 4f, "Beam width.");
            _railWindup = Config.Bind("Wizard Astral Railcannon", "Windup", 2.5f, "Deliberate cannon assembly time.");

            _wizardHeldStaffEitrRegen = Config.Bind("Wizard Staff Weapon Mastery", "HeldStaffEitrRegenPercent_v0123", 20f, "Additional Eitr Regen while the Wizard is actually holding a Staff.");
            _wizardHeldStaffFlatEitr = Config.Bind("Wizard Staff Weapon Mastery", "HeldStaffFlatEitr_v0123", 30f, "Flat Max Eitr while the Wizard is actually holding a Staff.");
            _wizardChargeThreshold = Config.Bind("Wizard Staff Charge", "SecondsPerStack_v0111", 2f, "Two seconds per charge stack. Maximum three stacks.");
            _wizardChargeBaseRadius = Config.Bind("Wizard Staff Charge", "BaseExplosionRadius", 2.5f, "Quick-release impact radius. Radius becomes exactly 2x at stack 1 and does not grow further.");
            _wizardChargeExtraEitrPerSecond = Config.Bind("Wizard Staff Charge", "EitrPerSecond_v0111", 10f, "Development rule: 1 Eitr per 0.1s while charging, stopping at 3 stacks.");
            _wizardChargeMaxDamageMultiplier = Config.Bind("Wizard Staff Charge", "ThreeStackDamageMultiplier_v0111", 2.5f, "Prototype full-charge damage multiplier. Radius does not increase after stack 1.");
            _wizardChargeRange = Config.Bind("Wizard Staff Charge", "Range", 50f, "Charged projectile range.");

            _voidCooldown = Config.Bind("Spellcaster Void Step", "Cooldown", 5f, "Seconds.");
            _voidGroundRange = Config.Bind("Spellcaster Void Step", "GroundPacRange", 50f, "Ground PAC teleport range.");
            _voidFreeRange = Config.Bind("Spellcaster Void Step", "FreeAimRange", 15f, "Free Aim teleport range.");

            _phalanxCooldown = Config.Bind("Spellcaster Arcane Phalanx", "Cooldown", 15f, "Seconds after the swords are consumed.");
            _phalanxEitr = Config.Bind("Spellcaster Arcane Phalanx", "EitrCost", 40f, "Eitr cost.");
            _phalanxCount = Config.Bind("Spellcaster Arcane Phalanx", "SwordCount", 8, "Hovering arcane swords.");

            _ruptureMaxCharges = Config.Bind("Spellcaster Arcane Rupture", "MaxCharges", 3, "Ultimate charge count.");
            _ruptureRecharge = Config.Bind("Spellcaster Arcane Rupture", "RechargeSeconds", 20f, "Recharge time per spent charge.");
            _ruptureRadius = Config.Bind("Spellcaster Arcane Rupture", "Radius", 10f, "Literal 10m radius.");
            _ruptureRange = Config.Bind("Spellcaster Arcane Rupture", "CastRange", 50f, "Ground PAC range.");
            _ruptureWindup = Config.Bind("Spellcaster Arcane Rupture", "LocationWindup", 1f, "The spell location winds up; caster stays mobile and may sprint.");
            _ruptureBuffer = Config.Bind("Spellcaster Arcane Rupture", "ActivationBufferSeconds", 0.5f, "Minimum delay between consecutive Arcane Rupture charge activations.");

            _riftDuration = Config.Bind("Spellcaster Riftwalker", "RiftDuration", 30f, "Seconds both portals remain.");
            _riftRange = Config.Bind("Spellcaster Riftwalker", "RiftPairRange", 30f, "Maximum Rift B distance from Rift A.");
            _phaseDuration = Config.Bind("Spellcaster Riftwalker", "PhaseFlowDuration", 4f, "Passive mobility buff duration after Void Step or Rift travel.");
            _riftEchoCooldown = Config.Bind("Spellcaster Rift Echo", "Cooldown", 16f, "Seconds.");
            _riftEchoEitr = Config.Bind("Spellcaster Rift Echo", "EitrCost", 45f, "Eitr cost before Spellcaster reduction.");
            _riftEchoDuration = Config.Bind("Spellcaster Rift Echo", "Duration", 8f, "Echo window.");
            _riftEchoDamageMultiplier = Config.Bind("Spellcaster Rift Echo", "EchoDamageMultiplier", 0.45f, "Weaker duplicate shot damage.");
            _afterimageCooldown = Config.Bind("Spellcaster Afterimage Arsenal", "Cooldown", 18f, "Seconds.");
            _afterimageEitr = Config.Bind("Spellcaster Afterimage Arsenal", "EitrCost", 50f, "Eitr cost before Spellcaster reduction.");
            _afterimageDuration = Config.Bind("Spellcaster Afterimage Arsenal", "Duration", 8f, "Afterimage window.");
            _afterimageMax = Config.Bind("Spellcaster Afterimage Arsenal", "MaxAfterimages", 3, "Maximum active afterimages.");
            _afterimageDamageMultiplier = Config.Bind("Spellcaster Afterimage Arsenal", "DamageMultiplier", 0.35f, "Each afterimage shot damage multiplier.");
            _gunStaffFireRateMultiplier = Config.Bind("Spellcaster Gun Staff", "SingleAttackSpeedMultiplier_v0123", 1.5f, "Single Staff/Wand baseline: +50% Attack Speed. Rapid Gun Staff cooldown uses the same 1.5x target.");
            _dualGunStaffAttackSpeedMultiplier = Config.Bind("Spellcaster Gun Staff", "DualAttackSpeedMultiplier_v0123", 2f, "Dual Gun Staves baseline: +100% Attack Speed. Example: 0.50s cadence becomes 0.25s.");
            _gunStaffFiringMoveBonus = Config.Bind("Spellcaster Gun Staff", "LegacyFiringMovementBonusPercent", 0f, "Legacy setting retained only for config compatibility. Firing no longer grants artificial movement speed.");

            _ruptureCharges = Mathf.Max(1, _ruptureMaxCharges.Value);

            try
            {
                _harmony = new Harmony(ModGuid);
                InstallPatches();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Sorcerer optional patches failed: " + ex.Message);
            }

            Logger.LogInfo(ModName + " v" + ModVersion + " loaded. Sorcerer / Wizard / Spellcaster alpha is ready.");
        }

        private void InstallPatches()
        {
            // Arcane Blood Max Eitr is owned by the starter Skills DLL.
            // Do not patch GetMaxEitr here or the +30 bonus would stack twice.
            MethodInfo attackPrefix = typeof(SorcererPlugin).GetMethod("AttackStartPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo attackPostfix = typeof(SorcererPlugin).GetMethod("AttackStartPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo[] attacks = typeof(Attack).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < attacks.Length; i++)
            {
                if (attacks[i].Name == "Start" && attacks[i].ReturnType == typeof(bool))
                    PatchWithHarmony(attacks[i], new HarmonyMethod(attackPrefix), new HarmonyMethod(attackPostfix));
            }

            MethodInfo eitrPrefix = typeof(SorcererPlugin).GetMethod("TryUseEitrPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo[] playerMethods = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < playerMethods.Length; i++)
            {
                MethodInfo method = playerMethods[i];
                if (method.Name != "TryUseEitr")
                    continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 1 || parameters[0].ParameterType != typeof(float))
                    continue;
                PatchWithHarmony(method, new HarmonyMethod(eitrPrefix), null);
            }

            MethodInfo wizardMaxEitrPrefix = typeof(SorcererPlugin).GetMethod("WizardSetMaxEitrPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            for (int i = 0; i < playerMethods.Length; i++)
            {
                MethodInfo method = playerMethods[i];
                if (method.Name != "SetMaxEitr")
                    continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 1 || parameters[0].ParameterType != typeof(float))
                    continue;
                PatchWithHarmony(method, new HarmonyMethod(wizardMaxEitrPrefix), null);
            }
        }

        private static void WizardSetMaxEitrPrefix(Player __instance, ref float __0)
        {
            if (__instance == null || Instance == null)
                return;
            if (Instance.GetClass(__instance) != "Sorcerer" || Instance.GetAdvancement(__instance) != "Wizard")
                return;
            if (!Instance.IsWizardStaff(Instance.GetCurrentWeapon(__instance)))
                return;

            __0 += Mathf.Max(0f, Instance._wizardHeldStaffFlatEitr.Value);
        }

        private void PatchWithHarmony(MethodBase original, HarmonyMethod prefix, HarmonyMethod postfix)
        {
            MethodInfo[] methods = typeof(Harmony).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "Patch") continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 1 || !typeof(MethodBase).IsAssignableFrom(parameters[0].ParameterType)) continue;
                bool okay = true;
                for (int p = 1; p < parameters.Length; p++)
                    if (parameters[p].ParameterType != typeof(HarmonyMethod)) okay = false;
                if (!okay) continue;
                object[] args = new object[parameters.Length];
                args[0] = original;
                for (int p = 1; p < args.Length; p++) args[p] = null;
                if (args.Length > 1) args[1] = prefix;
                if (args.Length > 2) args[2] = postfix;
                method.Invoke(_harmony, args);
                return;
            }
        }

        private static bool AttackStartPrefix(Attack __instance, ref bool __result, object[] __args)
        {
            if (Instance == null || __args == null)
                return true;

            Player player = null;
            ItemDrop.ItemData weapon = null;
            for (int i = 0; i < __args.Length; i++)
            {
                if (player == null) player = __args[i] as Player;
                if (weapon == null) weapon = __args[i] as ItemDrop.ItemData;
            }
            if (player == null) player = Player.m_localPlayer;
            if (player == null || player != Player.m_localPlayer)
                return true;
            if (Instance.GetClass(player) != "Sorcerer" || Instance.GetAdvancement(player) != "Wizard")
                return true;
            if (weapon == null) weapon = Instance.GetCurrentWeapon(player);
            if (!Instance.IsWizardStaff(weapon))
                return true;

            // Normal Mouse1 is always left to Valheim. Wizard Charged Staff is
            // a separate Mouse2 + Mouse1 command so it never fights the Staff's
            // ordinary primary attack.
            if (Instance._wizardChargeActive || Time.time < Instance._suppressWizardVanillaUntil)
            {
                __result = false;
                return false;
            }

            if (!Input.GetKey(KeyCode.Mouse0) || !Input.GetKey(KeyCode.Mouse1))
                return true;

            Instance.BeginWizardStaffCharge(player, weapon);
            __result = false;
            return false;
        }

        private static void TryUseEitrPrefix(Character __instance, ref float __0)
        {
            if (Instance == null || __instance == null)
                return;
            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer)
                return;
            if (Instance.GetClass(player) == "Sorcerer" && Instance.GetAdvancement(player) == "Spellcaster")
                __0 *= 0.5f;
        }

        private static void AttackStartPostfix(Attack __instance, ref bool __result, object[] __args)
        {
            if (!__result || Instance == null || __args == null)
                return;

            Player player = null;
            ItemDrop.ItemData weapon = null;
            for (int i = 0; i < __args.Length; i++)
            {
                if (player == null) player = __args[i] as Player;
                if (weapon == null) weapon = __args[i] as ItemDrop.ItemData;
            }
            if (player == null) player = Player.m_localPlayer;
            if (player == null || player != Player.m_localPlayer) return;
            Instance.OnNormalMagicAttack(player, weapon);
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                try { _harmony.UnpatchSelf(); }
                catch { }
            }
            DestroyRifts();
            ClearPhalanx();
            ClearAfterimages();
            RestoreGunStaffTimings();
        }

        private void Update()
        {
            UpdateRuptureCharges();
            UpdatePhalanxVisuals();
            UpdateRifts();

            Player player = Player.m_localPlayer;
            if (player == null)
            {
                RestoreGunStaffTimings();
                return;
            }

            if (GetClass(player) != "Sorcerer")
            {
                RestoreGunStaffTimings();
                if (_wizardStaffBonusActive)
                {
                    _wizardStaffBonusActive = false;
                    RefreshFoodStats(player);
                }
                return;
            }

            string advancement = GetAdvancement(player);
            UpdateSpellcasterWeaponMastery(player, advancement);

            // Starter Skills DLL owns Arcane Blood's base +30% Eitr Regen.
            // Wizard Weapon Mastery adds +20% Regen and +30 Max Eitr only while
            // a real Staff (not a Wand/Gun Staff) is currently held.
            bool wizardStaffHeld = advancement == "Wizard" && IsWizardStaff(GetCurrentWeapon(player));
            if (wizardStaffHeld != _wizardStaffBonusActive)
            {
                _wizardStaffBonusActive = wizardStaffHeld;
                RefreshFoodStats(player);
            }

            float advancementRegen = 0f;
            if (wizardStaffHeld)
                advancementRegen = Mathf.Max(0f, _wizardHeldStaffEitrRegen.Value) / 100f;
            else if (advancement == "Spellcaster")
                advancementRegen = 0.20f;

            if (advancementRegen > 0f)
                DragonCombat.ApplyTimedBuff(player, "Sorcerer.AdvancementEitrRegen", 0.35f, 0f, 0f, 0f, 0f, 0f, advancementRegen, false);

            if (Time.time < _overchargeUntil)
                DragonCombat.ApplyTimedBuff(player, "Wizard.Overcharge", 0.35f, 0f, 0f, 0f, 0f, 0f, 0.30f, false);

            if (advancement == "Spellcaster")
            {
                bool dualGunStaff = IsDualGunStaffEquipped(player);
                float speed = dualGunStaff
                    ? Mathf.Max(1f, _dualGunStaffAttackSpeedMultiplier.Value)
                    : Mathf.Max(1f, _gunStaffFireRateMultiplier.Value);

                if (Time.time < _phaseFlowUntil)
                    speed *= 1.133333f;

                if (IsHoldingMagicWeapon(player))
                    DragonCombat.SetAttackSpeedSource(player, speed, 0.30f);

                // No artificial movement-speed buff while firing. Spellcaster's
                // identity is preserving its current walk/run/sprint speed while
                // attacking, not gaining extra speed from the Gun Staff itself.
                if (Time.time < _phaseFlowUntil)
                    DragonCombat.ApplyTimedBuff(player, "Spellcaster.PhaseFlow", 0.35f, 0f, 0f, 0.15f, 0f, 0f, 0.25f, false);
            }

            if (_phalanxVolleyArmed && Input.GetKeyDown(KeyCode.Mouse0))
            {
                _phalanxVolleyArmed = false;
                LaunchAllPhalanx(player);
            }

            // Spellcaster follow-up skills listen to raw Mouse1 hold instead of
            // Valheim attack callbacks. This keeps Arcane Phalanx, Rift Echo and
            // Afterimage Arsenal responsive with Gun Staves, regular Staves,
            // melee weapons, tools, shields, or even bare fists.
            if (advancement == "Spellcaster")
                UpdateSpellcasterHeldFire(player);
            else
                _nextSpellcasterHeldFire = 0f;

            UpdateWizardStaffCharge(player, advancement);

            if (!Input.GetKey(_modifier.Value))
                return;

            // M4+1/2/3 starter skills are owned by AlbedosCustomClasses.Skills.dll.
            if (advancement == "Wizard")
            {
                if (Input.GetKeyDown(_skill4.Value)) CastGravityDominion(player);
                else if (Input.GetKeyDown(_skill5.Value)) CastAstralGreatblade(player);
                else if (Input.GetKeyDown(_skill6.Value)) CastFrostNova(player);
                else if (Input.GetKeyDown(_skill7.Value)) CastMeteorFall(player);
                else if (Input.GetKeyDown(_skill8.Value)) CastAstralRailcannon(player);
                else if (Input.GetKeyDown(_skill9.Value)) CastElementalCataclysm(player);
            }
            else if (advancement == "Spellcaster")
            {
                if (Input.GetKeyDown(_skill4.Value)) CastRiftEcho(player);
                else if (Input.GetKeyDown(_skill5.Value)) CastVoidStep(player);
                else if (Input.GetKeyDown(_skill6.Value)) CastArcanePhalanx(player);
                else if (Input.GetKeyDown(_skill7.Value)) CastAfterimageArsenal(player);
                else if (Input.GetKeyDown(_skill8.Value)) CastArcaneRupture(player);
                else if (Input.GetKeyDown(_passive.Value)) ActivateTwinRift(player);
            }
        }

        private void CastFlameBurst(Player player)
        {
            if (!BeginSkill(player, "Sorcerer.FlameBurst", _flameCooldown.Value, _flameEitr.Value)) return;
            float windup = ScaleWindup(player, 0.4f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Raise", windup + 0.08f);
            StartCoroutine(FlameBurstRoutine(player, windup));
        }

        private IEnumerator FlameBurstRoutine(Player player, float windup)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            Vector3 origin = player.transform.position + Vector3.up;
            Vector3 forward = FlatForward(player);
            List<Character> targets = GetConeTargets(player, origin, forward, _flameRange.Value, _flameAngle.Value);
            for (int i = 0; i < targets.Count; i++)
            {
                Deal(player, targets[i], 0f, 0f, 0f, 34f, 0f, 0f, 0f, 0f, 6f, false);
                StartCoroutine(BurnRoutine(player, targets[i], false, 6f));
            }
            if (_enableVfx.Value) StartCoroutine(ConeVfx(origin, forward, _flameRange.Value, _flameAngle.Value, new Color(1f, 0.28f, 0.05f, 0.95f)));
            ShowMessage("Flame Burst");
        }

        private void CastGlacialDescent(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, _iceRange.Value, out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Sorcerer.GlacialDescent", _iceCooldown.Value, _iceEitr.Value)) return;
            float windup = ScaleWindup(player, 1f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f);
            StartCoroutine(GlacialRoutine(player, target, windup));
        }

        private IEnumerator GlacialRoutine(Player player, Vector3 target, float windup)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(target, 8f);
            GameObject chunk = _enableVfx.Value ? CreateIceChunk(sky, _iceRadius.Value) : null;
            float drop = Mathf.Max(0.12f, DragonCombat.GetSkySummonDropTime());
            float e = 0f;
            while (e < drop)
            {
                if (chunk != null) chunk.transform.position = Vector3.Lerp(sky, target + Vector3.up * 0.8f, e / drop);
                e += Time.deltaTime;
                yield return null;
            }
            if (chunk != null) Destroy(chunk, 0.30f);
            List<Character> targets = GetSphereTargets(player, target, _iceRadius.Value);
            for (int i = 0; i < targets.Count; i++)
            {
                Deal(player, targets[i], 22f, 0f, 0f, 0f, 36f, 0f, 0f, 0f, 12f, false);
                DragonCombat.ApplyFrost(targets[i], 6f);
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(target, _iceRadius.Value, new Color(0.55f, 0.90f, 1f, 0.95f), 0.7f));
            ShowMessage("Glacial Descent");
        }

        private void CastStonefang(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, _stoneRange.Value, out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Sorcerer.Stonefang", _stoneCooldown.Value, _stoneEitr.Value)) return;
            float windup = ScaleWindup(player, 0.8f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Slam", windup + 0.10f);
            StartCoroutine(StonefangRoutine(player, target, windup));
        }

        private IEnumerator StonefangRoutine(Player player, Vector3 target, float windup)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            List<Character> targets = GetSphereTargets(player, target, _stoneRadius.Value);
            for (int i = 0; i < targets.Count; i++)
            {
                Character enemy = targets[i];
                Deal(player, enemy, 30f, 0f, 28f, 0f, 0f, 0f, 0f, 0f, 18f, false);
                if (DragonCombat.IsSmallEnemy(enemy))
                {
                    DragonCombat.Stun(enemy, target);
                    DragonCombat.ApplyCripple(enemy, 6f);
                }
                else if (!IsBoss(enemy))
                    DragonCombat.ApplyCripple(enemy, 6f);
            }
            if (_enableVfx.Value) CreateStoneSpikes(target, _stoneRadius.Value);
            ShowMessage("Stonefang Eruption");
        }

        private void CastGravityDominion(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, 50f, out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Wizard.GravityDominion", _gravityCooldown.Value, _gravityEitr.Value)) return;
            float windup = ScaleWindup(player, 1f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Channel", windup + 0.10f);
            StartCoroutine(GravityRoutine(player, target, windup));
        }

        private IEnumerator GravityRoutine(Player player, Vector3 center, float windup)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            float end = Time.time + _gravityDuration.Value;
            while (Time.time < end)
            {
                List<Character> targets = GetSphereTargets(player, center, _gravityRadius.Value);
                for (int i = 0; i < targets.Count; i++)
                {
                    Character enemy = targets[i];
                    DragonCombat.ApplyExpose(enemy, 5f);
                    if (DragonCombat.IsSmallEnemy(enemy)) PullToward(enemy, center, 7f);
                    else if (!IsBoss(enemy)) DragonCombat.ApplyCripple(enemy, 6f);
                    Deal(player, enemy, 0f, 0f, 0f, 0f, 0f, 14f, 0f, 14f, 2f, false);
                }
                if (_enableVfx.Value) StartCoroutine(RingVfx(center, _gravityRadius.Value, new Color(0.45f, 0.12f, 0.75f, 0.85f), 0.85f));
                yield return new WaitForSeconds(1f);
            }
            ShowMessage("Gravity Dominion");
        }

        private void CastAstralGreatblade(Player player)
        {
            if (CooldownRemaining("Wizard.AstralGreatblade") > 0f) { ShowCooldown("Wizard.AstralGreatblade"); return; }
            if (!SpendEitr(player, _bladeEitr.Value)) { ShowMessage("Not enough Eitr"); return; }
            StartCooldown("Wizard.AstralGreatblade", _bladeCooldown.Value);
            StartCoroutine(AstralGreatbladeRoutine(player));
        }

        private IEnumerator AstralGreatbladeRoutine(Player player)
        {
            float baseWindup = ScaleWindup(player, 1f);
            DragonCombat.LockSkill(player, baseWindup);
            DragonCombat.PlaySkillPose(player, "HeavySlash", baseWindup + 0.10f);
            yield return new WaitForSeconds(baseWindup);

            float charged = 0f;
            float max = Mathf.Max(0.1f, _bladeChargeMax.Value);
            while (Input.GetKey(_modifier.Value) && Input.GetKey(_skill5.Value) && charged < max)
            {
                DragonCombat.LockSkill(player, 0.12f);
                charged += Time.deltaTime;
                if (_enableVfx.Value && ((int)(charged * 10f) % 3 == 0)) StartCoroutine(RingVfx(player.transform.position, 1.5f + charged, new Color(0.62f, 0.18f, 1f, 0.65f), 0.15f));
                yield return null;
            }

            float multiplier = 1f + Mathf.Clamp01(charged / max);
            Vector3 forward = FlatForward(player);
            List<Character> targets = GetBoxTargets(player, player.transform.position + Vector3.up, forward, _bladeRange.Value, _bladeWidth.Value);
            for (int i = 0; i < targets.Count; i++)
            {
                Deal(player, targets[i], 55f * multiplier, 62f * multiplier, 0f, 0f, 0f, 0f, 0f, 48f * multiplier, 32f, true);
                StartCoroutine(BurnRoutine(player, targets[i], true, 6f));
                DragonCombat.Stun(targets[i], player.transform.position);
            }
            if (_enableVfx.Value) StartCoroutine(GreatbladeVfx(player.transform.position, forward, _bladeRange.Value, _bladeWidth.Value));
            ShowMessage("Astral Greatblade x" + multiplier.ToString("0.0"));
        }

        private void CastFrostNova(Player player)
        {
            if (!BeginSkill(player, "Wizard.FrostNova", _novaCooldown.Value, _novaEitr.Value)) return;
            float windup = ScaleWindup(player, 1f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Wave", windup + 0.10f);
            StartCoroutine(FrostNovaRoutine(player, windup));
        }

        private IEnumerator FrostNovaRoutine(Player player, float windup)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            List<Character> targets = GetSphereTargets(player, player.transform.position, _novaRadius.Value);
            for (int i = 0; i < targets.Count; i++)
            {
                Character enemy = targets[i];
                Deal(player, enemy, 0f, 0f, 0f, 0f, 62f, 0f, 0f, 0f, 18f, false);
                DragonCombat.ApplyFrost(enemy, 8f);
                if (DragonCombat.IsSmallEnemy(enemy)) DragonCombat.Stun(enemy, player.transform.position);
                else ForceStagger(enemy, player.transform.position);
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(player.transform.position, _novaRadius.Value, new Color(0.48f, 0.90f, 1f, 0.95f), 0.8f));
            ShowMessage("Frost Nova");
        }

        private void CastMeteorFall(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, _meteorRange.Value, out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Wizard.MeteorFall", _meteorCooldown.Value, _meteorEitr.Value)) return;
            float windup = ScaleWindup(player, 1.2f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f);
            StartCoroutine(MeteorRoutine(player, target, windup));
        }

        private IEnumerator MeteorRoutine(Player player, Vector3 target, float windup)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(target, 12f);
            GameObject meteor = _enableVfx.Value ? CreateMeteor(sky) : null;
            float drop = 0.45f;
            float e = 0f;
            while (e < drop)
            {
                if (meteor != null) meteor.transform.position = Vector3.Lerp(sky, target, e / drop);
                e += Time.deltaTime;
                yield return null;
            }
            if (meteor != null) Destroy(meteor);
            List<Character> targets = GetSphereTargets(player, target, _meteorRadius.Value);
            for (int i = 0; i < targets.Count; i++)
            {
                Deal(player, targets[i], 80f, 0f, 0f, 80f, 0f, 0f, 0f, 0f, 35f, true);
                StartCoroutine(BurnRoutine(player, targets[i], false, 6f));
                ForceStagger(targets[i], target);
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(target, _meteorRadius.Value, new Color(1f, 0.22f, 0.02f, 1f), 0.75f));
            ShowMessage("Meteor Fall");
        }


        private void CastAstralRailcannon(Player player)
        {
            if (!BeginSkill(player, "Wizard.AstralRailcannon", _railCooldown.Value, _railEitr.Value))
                return;
            float windup = ScaleWindup(player, Mathf.Max(0.5f, _railWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Channel", windup + 0.15f);
            StartCoroutine(AstralRailcannonRoutine(player, windup));
        }

        private IEnumerator AstralRailcannonRoutine(Player player, float windup)
        {
            ShowMessage("Astral Railcannon");
            Vector3 origin = player.GetEyePoint();
            Vector3 forward = AlbedoAimUtility.GetProjectileDirection(player, origin);
            forward.Normalize();

            if (_enableVfx.Value)
            {
                for (int ring = 0; ring < 4; ring++)
                {
                    float d = 1.5f + ring * 1.25f;
                    StartCoroutine(RingVfx(origin + forward * d, 0.75f + ring * 0.25f, new Color(0.72f, 0.20f, 1f, 0.82f), Mathf.Max(0.35f, windup)));
                }
            }

            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            float range = Mathf.Max(5f, _railRange.Value);
            float width = Mathf.Max(0.8f, _railWidth.Value);
            List<Character> targets = GetBoxTargets(player, origin, forward, range, width);
            MagicDamageSnapshot damage = GetMagicWeaponDamage(GetCurrentWeapon(player));
            for (int i = 0; i < targets.Count; i++)
                DealMagicWeaponDamage(player, targets[i], damage, 1.75f);

            if (_enableVfx.Value)
                CreateBeam(origin, origin + forward * range, new Color(0.86f, 0.50f, 1f, 0.98f), width * 0.55f, 0.45f);

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(-forward * 4.5f, ForceMode.VelocityChange);
        }

        private void CastElementalCataclysm(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, _cataclysmRange.Value, out target)) { ShowMessage("Aim at a physical target"); return; }
            if (CooldownRemaining("Wizard.ElementalCataclysm") > 0f) { ShowCooldown("Wizard.ElementalCataclysm"); return; }
            if (!SpendEitr(player, _cataclysmEitr.Value)) { ShowMessage("Not enough Eitr"); return; }
            StartCooldown("Wizard.ElementalCataclysm", _cataclysmCooldown.Value);
            StartCoroutine(CataclysmRoutine(player, target));
        }

        private IEnumerator CataclysmRoutine(Player player, Vector3 target)
        {
            float max = Mathf.Max(1f, _cataclysmCharge.Value);
            float charge = 0f;
            while (Input.GetKey(_modifier.Value) && Input.GetKey(_skill8.Value) && charge < max)
            {
                DragonCombat.LockSkill(player, 0.12f);
                charge += Time.deltaTime;
                if (_enableVfx.Value && ((int)(charge * 10f) % 4 == 0)) StartCoroutine(RingVfx(target, _cataclysmRadius.Value * Mathf.Clamp01(0.2f + charge / max), new Color(0.76f, 0.30f, 1f, 0.55f), 0.16f));
                yield return null;
            }
            float multiplier = 1f + 2f * Mathf.Clamp01(charge / max);
            List<Character> targets = GetSphereTargets(player, target, _cataclysmRadius.Value);
            for (int i = 0; i < targets.Count; i++)
            {
                Character enemy = targets[i];
                Deal(player, enemy, 55f * multiplier, 55f * multiplier, 55f * multiplier, 55f * multiplier, 55f * multiplier, 55f * multiplier, 55f * multiplier, 0f, 55f, true);
                DragonCombat.ApplyExpose(enemy, 15f);
            }
            if (_enableVfx.Value)
            {
                StartCoroutine(RingVfx(target, _cataclysmRadius.Value, new Color(1f, 0.35f, 0.05f, 1f), 1f));
                CreateLightningBurst(target, _cataclysmRadius.Value);
            }
            ShowMessage("ELEMENTAL CATACLYSM x" + multiplier.ToString("0.0"));
        }


        private void CastRiftEcho(Player player)
        {
            if (!BeginSkill(player, "Spellcaster.RiftEcho", _riftEchoCooldown.Value, _riftEchoEitr.Value))
                return;
            _riftEchoUntil = Time.time + Mathf.Max(1f, _riftEchoDuration.Value);
            DragonCombat.BeginMobileCast(player, 0.30f, true);
            ShowMessage("Rift Echo ACTIVE");
            if (_enableVfx.Value)
                StartCoroutine(RingVfx(player.transform.position + Vector3.up * 0.12f, 2.0f, new Color(0.68f, 0.20f, 1f, 0.90f), 0.45f));
        }

        private void CastAfterimageArsenal(Player player)
        {
            if (!BeginSkill(player, "Spellcaster.AfterimageArsenal", _afterimageCooldown.Value, _afterimageEitr.Value))
                return;
            _afterimageUntil = Time.time + Mathf.Max(1f, _afterimageDuration.Value);
            ClearAfterimages();
            SpawnAfterimage(player.transform.position, player.transform.rotation);
            ShowMessage("Afterimage Arsenal ACTIVE");
        }

        private void SpawnAfterimage(Vector3 position, Quaternion rotation)
        {
            if (Time.time >= _afterimageUntil)
                return;

            int max = Mathf.Max(1, _afterimageMax.Value);
            while (_spellcasterAfterimages.Count >= max)
            {
                GameObject old = _spellcasterAfterimages[0];
                _spellcasterAfterimages.RemoveAt(0);
                if (old != null)
                    Destroy(old);
            }

            GameObject root = new GameObject("SpellcasterAfterimage");
            root.transform.position = position;
            root.transform.rotation = rotation;

            if (_enableVfx.Value)
            {
                CreateAfterimagePart(root.transform, PrimitiveType.Capsule, new Vector3(0f, 1.0f, 0f), new Vector3(0.52f, 0.90f, 0.42f));
                CreateAfterimagePart(root.transform, PrimitiveType.Sphere, new Vector3(0f, 2.05f, 0f), new Vector3(0.42f, 0.42f, 0.42f));
                CreateAfterimagePart(root.transform, PrimitiveType.Capsule, new Vector3(-0.48f, 1.35f, 0.02f), new Vector3(0.16f, 0.62f, 0.16f));
                CreateAfterimagePart(root.transform, PrimitiveType.Capsule, new Vector3(0.48f, 1.35f, 0.02f), new Vector3(0.16f, 0.62f, 0.16f));
                StartCoroutine(RingVfx(position + Vector3.up * 0.05f, 1.15f, new Color(0.64f, 0.16f, 1f, 0.72f), 0.45f));
            }

            _spellcasterAfterimages.Add(root);
            Destroy(root, Mathf.Max(1f, _afterimageDuration.Value));
        }

        private void CreateAfterimagePart(Transform parent, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider col = part.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                    renderer.material = new Material(shader);
                renderer.material.color = new Color(0.54f, 0.12f, 1f, 0.34f);
            }
        }

        private void ClearAfterimages()
        {
            for (int i = 0; i < _spellcasterAfterimages.Count; i++)
            {
                if (_spellcasterAfterimages[i] != null)
                    Destroy(_spellcasterAfterimages[i]);
            }
            _spellcasterAfterimages.Clear();
        }

        private void FireRiftEcho(Player player, ItemDrop.ItemData weapon)
        {
            if (Time.time >= _riftEchoUntil || Time.time - _lastEchoShot < 0.12f)
                return;

            Character target = FindAimedEnemy(player, 50f, 10f);
            if (target == null)
                return;

            _lastEchoShot = Time.time;
            MagicDamageSnapshot damage = GetMagicWeaponDamage(weapon);
            Vector3 away = target.transform.position - player.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = player.transform.forward;
            away.Normalize();
            Vector3 portal = target.transform.position + away * 2.2f + Vector3.up * 1.1f;
            Vector3 aim = target.transform.position + Vector3.up * 1.0f;

            if (_enableVfx.Value)
                StartCoroutine(RingVfx(portal, 0.85f, new Color(0.80f, 0.22f, 1f, 0.95f), 0.35f));

            StartCoroutine(RiftEchoProjectile(player, target, damage, portal, aim));
        }

        private IEnumerator RiftEchoProjectile(Player player, Character target, MagicDamageSnapshot damage, Vector3 origin, Vector3 aim)
        {
            Vector3 to = aim - origin;
            float distance = Mathf.Max(0.1f, to.magnitude);
            Vector3 dir = to / distance;
            GameObject orb = _enableVfx.Value ? CreateOrb(origin, 0.18f, new Color(0.82f, 0.30f, 1f, 1f)) : null;
            float traveled = 0f;
            float speed = 38f;

            while (traveled < distance)
            {
                float step = Mathf.Min(speed * Time.deltaTime, distance - traveled);
                origin += dir * step;
                traveled += step;
                if (orb != null)
                    orb.transform.position = origin;
                yield return null;
            }

            if (target != null && !target.IsDead())
                DealMagicWeaponDamage(player, target, damage, Mathf.Max(0.05f, _riftEchoDamageMultiplier.Value));

            if (_enableVfx.Value)
                CreateBeam(aim + dir * 1.5f, aim - dir * 1.5f, new Color(0.82f, 0.36f, 1f, 0.88f), 0.10f, 0.20f);
            if (orb != null)
                Destroy(orb);
        }

        private void FireAfterimages(Player player, ItemDrop.ItemData weapon)
        {
            if (Time.time >= _afterimageUntil || _spellcasterAfterimages.Count == 0)
                return;

            Character target = FindAimedEnemy(player, 50f, 14f);
            Vector3 aimPoint = target == null ? player.GetEyePoint() + AlbedoAimUtility.GetProjectileDirection(player, player.GetEyePoint()) * 35f : target.transform.position + Vector3.up * 1.0f;
            MagicDamageSnapshot damage = GetMagicWeaponDamage(weapon);

            for (int i = 0; i < _spellcasterAfterimages.Count; i++)
            {
                GameObject echo = _spellcasterAfterimages[i];
                if (echo == null)
                    continue;
                Vector3 origin = echo.transform.position + Vector3.up * 1.2f;
                if (_enableVfx.Value)
                    CreateBeam(origin, aimPoint, new Color(0.60f, 0.18f, 1f, 0.72f), 0.10f, 0.22f);
                if (target != null)
                    DealMagicWeaponDamage(player, target, damage, Mathf.Max(0.05f, _afterimageDamageMultiplier.Value));
            }
        }

        private void CastVoidStep(Player player)
        {
            if (CooldownRemaining("Spellcaster.VoidStep") > 0f) { ShowCooldown("Spellcaster.VoidStep"); return; }
            Vector3 dest;
            bool airborne = false;
            if (AlbedoAimUtility.TryGetPhysicalTarget(player, _voidGroundRange.Value, out dest))
                dest += Vector3.up * 0.15f;
            else
            {
                Vector3 origin = player.GetEyePoint();
                Vector3 dir = AlbedoAimUtility.GetProjectileDirection(player, origin);
                dest = player.transform.position + dir * Mathf.Max(1f, _voidFreeRange.Value);
                airborne = true;
            }
            StartCooldown("Spellcaster.VoidStep", _voidCooldown.Value);
            if (Time.time < _afterimageUntil)
                SpawnAfterimage(player.transform.position, player.transform.rotation);
            TeleportPlayer(player, dest);
            // Always clear fall state. A lower Ground PAC destination used to
            // inherit the pre-teleport altitude and deal fall damage on landing.
            ApplyFeatherFall(player);
            TriggerPhaseFlow(player);
            if (_enableVfx.Value) StartCoroutine(RingVfx(dest, 1.6f, new Color(0.58f, 0.12f, 1f, 0.95f), 0.35f));
            ShowMessage(airborne ? "Void Step - Free Aim / Feather Falling" : "Void Step");
        }

        private void BeginWizardStaffCharge(Player player, ItemDrop.ItemData weapon)
        {
            if (_wizardChargeActive || player == null)
                return;

            _wizardChargeActive = true;
            _wizardChargeStarted = Time.time;
            _wizardChargeNextSpend = Time.time + 0.10f;
            _wizardChargeExtraEitrSpent = 0f;
            _wizardChargeStacks = 0;
            _wizardChargeWeapon = weapon;
            DragonCombat.BeginMobileCast(player, 0.35f, true);
            ShowMessage("Wizard Staff Charge 0/3");
        }

        private void UpdateWizardStaffCharge(Player player, string advancement)
        {
            if (!_wizardChargeActive)
                return;

            if (player == null || player.IsDead() || advancement != "Wizard" || !IsWizardStaff(GetCurrentWeapon(player)))
            {
                CancelWizardStaffCharge(player);
                return;
            }

            DragonCombat.BeginMobileCast(player, 0.35f, true);
            DragonCombat.BlockEitrRegen(player, 0.20f);

            float held = Mathf.Max(0f, Time.time - _wizardChargeStarted);
            float secondsPerStack = Mathf.Max(0.25f, _wizardChargeThreshold.Value);
            int stacks = Mathf.Clamp(Mathf.FloorToInt(held / secondsPerStack), 0, 3);
            if (stacks != _wizardChargeStacks)
            {
                _wizardChargeStacks = stacks;
                ShowMessage("Wizard Staff Charge " + _wizardChargeStacks.ToString() + "/3");
            }

            float fullChargeTime = secondsPerStack * 3f;
            bool holdingChargeCombo = Input.GetKey(KeyCode.Mouse0) && Input.GetKey(KeyCode.Mouse1);
            if (held < fullChargeTime && holdingChargeCombo && Time.time >= _wizardChargeNextSpend)
            {
                const float tick = 0.10f;
                float cost = Mathf.Max(0f, _wizardChargeExtraEitrPerSecond.Value) * tick;
                if (cost > 0f)
                {
                    if (!SpendEitr(player, cost))
                    {
                        ReleaseWizardStaffCharge(player);
                        return;
                    }
                    _wizardChargeExtraEitrSpent += cost;
                }
                _wizardChargeNextSpend = Time.time + tick;
            }

            if (_enableVfx.Value && held > 0.15f && ((int)(held * 10f) % 3 == 0))
            {
                float radius = _wizardChargeStacks >= 1 ? _wizardChargeBaseRadius.Value * 2f : _wizardChargeBaseRadius.Value;
                StartCoroutine(RingVfx(player.transform.position, Mathf.Max(0.5f, radius), new Color(0.70f, 0.24f, 1f, 0.48f), 0.12f));
            }

            if (Input.GetKeyUp(KeyCode.Mouse0))
                ReleaseWizardStaffCharge(player);
            else if (!Input.GetKey(KeyCode.Mouse1))
                CancelWizardStaffCharge(player);
        }

        private void CancelWizardStaffCharge(Player player)
        {
            _wizardChargeActive = false;
            _wizardChargeWeapon = null;
            _wizardChargeExtraEitrSpent = 0f;
            _wizardChargeStacks = 0;
            if (player != null)
                DragonCombat.EndMobileCast(player);
        }

        private void ReleaseWizardStaffCharge(Player player)
        {
            if (!_wizardChargeActive || player == null)
                return;

            int releasedStacks = _wizardChargeStacks;
            ItemDrop.ItemData weapon = _wizardChargeWeapon;

            _wizardChargeActive = false;
            _wizardChargeWeapon = null;
            _wizardChargeExtraEitrSpent = 0f;
            _wizardChargeStacks = 0;
            DragonCombat.EndMobileCast(player);

            // The combo is a dedicated charged command. Releasing before one
            // completed stack simply cancels; normal Mouse1 remains untouched.
            if (releasedStacks < 1)
            {
                ShowMessage("STAFF CHARGE CANCELLED");
                return;
            }

            float radius = Mathf.Max(0.5f, _wizardChargeBaseRadius.Value) * 2f;
            float maxMultiplier = Mathf.Max(1f, _wizardChargeMaxDamageMultiplier.Value);
            float t = Mathf.Clamp01((float)releasedStacks / 3f);
            float multiplier = Mathf.Lerp(1f, maxMultiplier, t);

            // Stop Valheim from also emitting the normal Staff projectile on the
            // same release. The charged projectile is the replacement shot.
            _suppressWizardVanillaUntil = Time.time + 0.20f;

            Vector3 origin = player.GetEyePoint() + player.transform.forward * 0.45f;
            Vector3 dir = AlbedoAimUtility.GetProjectileDirection(player, origin);
            StartCoroutine(WizardChargedProjectile(player, weapon, origin, dir, radius, multiplier));
            ShowMessage("STAFF CHARGE " + releasedStacks.ToString() + "/3");
        }

        private void TriggerVanillaPrimaryAttack(Player player)
        {
            if (player == null)
                return;

            try
            {
                MethodInfo[] methods = player.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "StartAttack")
                        continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 2 || parameters[0].ParameterType != typeof(Character) || parameters[1].ParameterType != typeof(bool))
                        continue;

                    object[] args = new object[2];
                    args[0] = null;
                    args[1] = false;
                    method.Invoke(player, args);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Wizard normal Staff fallback failed: " + ex.Message);
            }
        }

        private IEnumerator WizardChargedProjectile(Player player, ItemDrop.ItemData weapon, Vector3 origin, Vector3 dir, float radius, float multiplier)
        {
            if (dir.sqrMagnitude < 0.01f)
                dir = player.transform.forward;
            dir.Normalize();

            GameObject orb = _enableVfx.Value ? CreateOrb(origin, Mathf.Max(0.24f, radius * 0.11f), new Color(0.72f, 0.22f, 1f, 1f)) : null;
            Vector3 pos = origin;
            float traveled = 0f;
            float range = Mathf.Max(1f, _wizardChargeRange.Value);
            float speed = Mathf.Max(5f, GetPrimaryAttackFloat(weapon, "m_projectileVel", 20f));
            float gravity = Mathf.Max(0f, GetPrimaryProjectileFloat(weapon, "m_gravity", 5f));
            float drag = Mathf.Max(0f, GetPrimaryProjectileFloat(weapon, "m_drag", 0f));
            Vector3 velocity = dir * speed;
            Vector3 impact = origin;

            while (traveled < range)
            {
                float dt = Mathf.Max(0.001f, Time.deltaTime);
                velocity += Vector3.down * gravity * dt;
                if (drag > 0f)
                    velocity *= Mathf.Clamp01(1f - drag * dt);

                Vector3 stepVector = velocity * dt;
                float step = stepVector.magnitude;
                if (step <= 0.0001f)
                {
                    yield return null;
                    continue;
                }
                if (traveled + step > range)
                {
                    float remaining = range - traveled;
                    stepVector = stepVector.normalized * remaining;
                    step = remaining;
                }

                Vector3 travelDir = stepVector.normalized;
                RaycastHit[] hits = Physics.RaycastAll(pos, travelDir, step + 0.35f);
                Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
                bool stop = false;
                Vector3 next = pos + stepVector;
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider collider = hits[i].collider;
                    if (collider == null || collider.isTrigger || IsPlayerCollider(player, collider))
                        continue;
                    next = hits[i].point;
                    stop = true;
                    break;
                }

                pos = next;
                impact = pos;
                if (orb != null)
                    orb.transform.position = pos;
                traveled += step;
                if (stop)
                    break;
                yield return null;
            }

            MagicDamageSnapshot damage = GetMagicWeaponDamage(weapon);
            Collider[] area = Physics.OverlapSphere(impact, radius);
            HashSet<Character> seen = new HashSet<Character>();
            for (int i = 0; i < area.Length; i++)
            {
                Character target = area[i].GetComponentInParent<Character>();
                if (target == null || seen.Contains(target) || !IsEnemy(player, target))
                    continue;
                seen.Add(target);
                DealMagicWeaponDamage(player, target, damage, multiplier);
            }

            if (_enableVfx.Value)
                StartCoroutine(RingVfx(impact + Vector3.up * 0.08f, radius, new Color(0.78f, 0.34f, 1f, 0.95f), 0.55f));
            if (orb != null)
                Destroy(orb);
        }

        private float GetPrimaryAttackFloat(ItemDrop.ItemData weapon, string fieldName, float fallback)
        {
            try
            {
                if (weapon == null || weapon.m_shared == null || weapon.m_shared.m_attack == null)
                    return fallback;
                FieldInfo field = weapon.m_shared.m_attack.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(float))
                    return Convert.ToSingle(field.GetValue(weapon.m_shared.m_attack));
            }
            catch
            {
            }
            return fallback;
        }

        private float GetPrimaryProjectileFloat(ItemDrop.ItemData weapon, string fieldName, float fallback)
        {
            try
            {
                if (weapon == null || weapon.m_shared == null || weapon.m_shared.m_attack == null || weapon.m_shared.m_attack.m_attackProjectile == null)
                    return fallback;
                Projectile projectile = weapon.m_shared.m_attack.m_attackProjectile.GetComponent<Projectile>();
                if (projectile == null)
                    return fallback;
                FieldInfo field = projectile.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(float))
                    return Convert.ToSingle(field.GetValue(projectile));
            }
            catch
            {
            }
            return fallback;
        }

        private float GetWeaponAttackEitrCost(ItemDrop.ItemData weapon)
        {
            try
            {
                if (weapon == null)
                    return 0f;

                FieldInfo sharedField = weapon.GetType().GetField("m_shared", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object shared = sharedField == null ? null : sharedField.GetValue(weapon);
                if (shared == null)
                    return 0f;

                FieldInfo attackField = shared.GetType().GetField("m_attack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object attack = attackField == null ? null : attackField.GetValue(shared);
                if (attack == null)
                    return 0f;

                FieldInfo eitrField = attack.GetType().GetField("m_attackEitr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (eitrField != null)
                    return Mathf.Max(0f, Convert.ToSingle(eitrField.GetValue(attack)));
            }
            catch
            {
            }
            return 0f;
        }

        private MagicDamageSnapshot GetMagicWeaponDamage(ItemDrop.ItemData weapon)
        {
            MagicDamageSnapshot snapshot = new MagicDamageSnapshot();
            try
            {
                if (weapon == null)
                    weapon = GetCurrentWeapon(Player.m_localPlayer);
                if (weapon == null)
                {
                    snapshot.Spirit = 25f;
                    return snapshot;
                }
                MethodInfo method = weapon.GetType().GetMethod("GetDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                object damage = method == null ? null : method.Invoke(weapon, null);
                if (damage == null)
                {
                    snapshot.Spirit = 25f;
                    return snapshot;
                }
                snapshot.Blunt = ReadDamageField(damage, "m_blunt");
                snapshot.Slash = ReadDamageField(damage, "m_slash");
                snapshot.Pierce = ReadDamageField(damage, "m_pierce");
                snapshot.Fire = ReadDamageField(damage, "m_fire");
                snapshot.Frost = ReadDamageField(damage, "m_frost");
                snapshot.Lightning = ReadDamageField(damage, "m_lightning");
                snapshot.Poison = ReadDamageField(damage, "m_poison");
                snapshot.Spirit = ReadDamageField(damage, "m_spirit");
                if (snapshot.Total() <= 0f)
                    snapshot.Spirit = 25f;
            }
            catch
            {
                snapshot.Spirit = 25f;
            }
            return snapshot;
        }

        private float ReadDamageField(object damage, string name)
        {
            try
            {
                FieldInfo field = damage.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return Convert.ToSingle(field.GetValue(damage));
            }
            catch
            {
            }
            return 0f;
        }

        private void DealMagicWeaponDamage(Player attacker, Character target, MagicDamageSnapshot damage, float multiplier)
        {
            HitData hit = new HitData();
            multiplier = Mathf.Max(0f, multiplier) * DragonCombat.GetSorcererMagicDamageMultiplier(attacker);
            hit.m_damage.m_blunt = damage.Blunt * multiplier;
            hit.m_damage.m_slash = damage.Slash * multiplier;
            hit.m_damage.m_pierce = damage.Pierce * multiplier;
            hit.m_damage.m_fire = damage.Fire * multiplier;
            hit.m_damage.m_frost = damage.Frost * multiplier;
            hit.m_damage.m_lightning = damage.Lightning * multiplier;
            hit.m_damage.m_poison = damage.Poison * multiplier;
            hit.m_damage.m_spirit = damage.Spirit * multiplier;
            hit.m_point = target.transform.position;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = 10f;
            hit.SetAttacker(attacker);
            target.Damage(hit);
        }

        private void CastArcanePhalanx(Player player)
        {
            if (_phalanxSwords.Count > 0)
            {
                _phalanxVolleyArmed = true;
                ShowMessage("Phalanx Volley READY - aim and Left Click");
                return;
            }
            if (!BeginSkill(player, "Spellcaster.ArcanePhalanx", _phalanxCooldown.Value, _phalanxEitr.Value)) return;
            DragonCombat.BeginMobileCast(player, 0.4f, false);
            int count = Mathf.Max(1, _phalanxCount.Value);
            for (int i = 0; i < count; i++) _phalanxSwords.Add(CreateArcaneSword(player.transform.position));
            ShowMessage("Arcane Phalanx x" + count);
        }

        private void OnNormalMagicAttack(Player player, ItemDrop.ItemData weapon)
        {
            // Keep the attack postfix only as a low-latency assist. The actual
            // trigger is raw Mouse1 hold, so it no longer depends on a weapon
            // being classified as Magic or on Attack.Start repeating correctly.
            if (player == null || GetAdvancement(player) != "Spellcaster" || !Input.GetKey(KeyCode.Mouse0))
                return;
            TryTriggerSpellcasterHeldFire(player);
        }

        private void UpdateSpellcasterHeldFire(Player player)
        {
            if (player == null || !Input.GetKey(KeyCode.Mouse0))
            {
                // A fresh press always gets an immediate first trigger.
                _nextSpellcasterHeldFire = 0f;
                return;
            }

            TryTriggerSpellcasterHeldFire(player);
        }

        private void TryTriggerSpellcasterHeldFire(Player player)
        {
            if (player == null)
                return;

            bool hasHeldFollowUp = (_phalanxSwords.Count > 0 && !_phalanxVolleyArmed) ||
                                   Time.time < _riftEchoUntil ||
                                   (Time.time < _afterimageUntil && _spellcasterAfterimages.Count > 0);
            if (!hasHeldFollowUp)
            {
                _nextSpellcasterHeldFire = 0f;
                return;
            }

            if (Time.time < _nextSpellcasterHeldFire)
                return;

            _nextSpellcasterHeldFire = Time.time + 0.50f;
            ItemDrop.ItemData weapon = GetCurrentWeapon(player);

            // These active follow-ups now pulse every 0.5s while Mouse1 is held,
            // regardless of what the Spellcaster currently has equipped.
            FireRiftEcho(player, weapon);
            FireAfterimages(player, weapon);

            if (_phalanxSwords.Count > 0 && !_phalanxVolleyArmed)
            {
                GameObject sword = _phalanxSwords[0];
                _phalanxSwords.RemoveAt(0);
                LaunchSword(player, sword);
            }
        }

        private void UpdateSpellcasterWeaponMastery(Player player, string advancement)
        {
            if (player == null || advancement != "Spellcaster")
            {
                RestoreGunStaffTimings();
                return;
            }

            ItemDrop.ItemData right = DragonCombat.GetHandItem(player, "m_rightItem");
            ItemDrop.ItemData left = DragonCombat.GetHandItem(player, "m_leftItem");
            bool dual = IsDualGunStaffEquipped(player);
            float cadenceMultiplier = dual
                ? Mathf.Max(1f, _dualGunStaffAttackSpeedMultiplier.Value)
                : Mathf.Max(1f, _gunStaffFireRateMultiplier.Value);
            ApplyInstantGunStaffTiming(right, cadenceMultiplier);
            ApplyInstantGunStaffTiming(left, cadenceMultiplier);
        }

        private void ApplyInstantGunStaffTiming(ItemDrop.ItemData item, float cadenceMultiplier)
        {
            if (!DragonCombat.IsGunStaff(item) || item == null || item.m_shared == null || item.m_shared.m_attack == null)
                return;

            Attack attack = item.m_shared.m_attack;
            GunStaffTimingState state;
            if (!_gunStaffTimingStates.TryGetValue(attack, out state))
            {
                state = new GunStaffTimingState();
                state.Attack = attack;
                Type type = attack.GetType();
                state.ChargeField = type.GetField("m_attackChargeTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                state.DrawField = type.GetField("m_attackDrawTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                state.WarmupField = type.GetField("m_attackWarmup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                state.CooldownField = type.GetField("m_attackCooldown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                state.SpeedFactorField = type.GetField("m_speedFactor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                state.SpeedFactorRotationField = type.GetField("m_speedFactorRotation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                state.AccuracyField = type.GetField("m_projectileAccuracy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                state.AccuracyMinField = type.GetField("m_projectileAccuracyMin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (state.ChargeField != null && state.ChargeField.FieldType == typeof(float))
                    state.ChargeValue = Convert.ToSingle(state.ChargeField.GetValue(attack));
                if (state.DrawField != null && state.DrawField.FieldType == typeof(float))
                    state.DrawValue = Convert.ToSingle(state.DrawField.GetValue(attack));
                if (state.WarmupField != null && state.WarmupField.FieldType == typeof(float))
                    state.WarmupValue = Convert.ToSingle(state.WarmupField.GetValue(attack));
                if (state.CooldownField != null && state.CooldownField.FieldType == typeof(float))
                    state.CooldownValue = Convert.ToSingle(state.CooldownField.GetValue(attack));
                if (state.SpeedFactorField != null && state.SpeedFactorField.FieldType == typeof(float))
                    state.SpeedFactorValue = Convert.ToSingle(state.SpeedFactorField.GetValue(attack));
                if (state.SpeedFactorRotationField != null && state.SpeedFactorRotationField.FieldType == typeof(float))
                    state.SpeedFactorRotationValue = Convert.ToSingle(state.SpeedFactorRotationField.GetValue(attack));
                if (state.AccuracyField != null && state.AccuracyField.FieldType == typeof(float))
                    state.AccuracyValue = Convert.ToSingle(state.AccuracyField.GetValue(attack));
                if (state.AccuracyMinField != null && state.AccuracyMinField.FieldType == typeof(float))
                    state.AccuracyMinValue = Convert.ToSingle(state.AccuracyMinField.GetValue(attack));
                _gunStaffTimingStates[attack] = state;
            }

            try
            {
                if (state.ChargeField != null && state.ChargeField.FieldType == typeof(float) && state.ChargeValue > 0f && state.ChargeValue <= 1.0f)
                    state.ChargeField.SetValue(attack, 0f);
                if (state.DrawField != null && state.DrawField.FieldType == typeof(float) && state.DrawValue > 0f && state.DrawValue <= 1.0f)
                    state.DrawField.SetValue(attack, 0f);
                if (state.WarmupField != null && state.WarmupField.FieldType == typeof(float) && state.WarmupValue > 0f && state.WarmupValue <= 1.0f)
                    state.WarmupField.SetValue(attack, 0f);
                if (state.CooldownField != null && state.CooldownField.FieldType == typeof(float) && state.CooldownValue > 0f)
                    state.CooldownField.SetValue(attack, state.CooldownValue / Mathf.Max(1f, cadenceMultiplier));
                if (state.SpeedFactorField != null && state.SpeedFactorField.FieldType == typeof(float))
                    state.SpeedFactorField.SetValue(attack, 1f);
                if (state.SpeedFactorRotationField != null && state.SpeedFactorRotationField.FieldType == typeof(float))
                    state.SpeedFactorRotationField.SetValue(attack, 1f);
                if (state.AccuracyField != null && state.AccuracyField.FieldType == typeof(float))
                    state.AccuracyField.SetValue(attack, 0f);
                if (state.AccuracyMinField != null && state.AccuracyMinField.FieldType == typeof(float))
                    state.AccuracyMinField.SetValue(attack, 0f);
            }
            catch
            {
            }
        }

        private void RestoreGunStaffTimings()
        {
            foreach (KeyValuePair<Attack, GunStaffTimingState> pair in _gunStaffTimingStates)
            {
                GunStaffTimingState state = pair.Value;
                if (state == null || state.Attack == null)
                    continue;
                try
                {
                    if (state.ChargeField != null && state.ChargeField.FieldType == typeof(float))
                        state.ChargeField.SetValue(state.Attack, state.ChargeValue);
                    if (state.DrawField != null && state.DrawField.FieldType == typeof(float))
                        state.DrawField.SetValue(state.Attack, state.DrawValue);
                    if (state.WarmupField != null && state.WarmupField.FieldType == typeof(float))
                        state.WarmupField.SetValue(state.Attack, state.WarmupValue);
                    if (state.CooldownField != null && state.CooldownField.FieldType == typeof(float))
                        state.CooldownField.SetValue(state.Attack, state.CooldownValue);
                    if (state.SpeedFactorField != null && state.SpeedFactorField.FieldType == typeof(float))
                        state.SpeedFactorField.SetValue(state.Attack, state.SpeedFactorValue);
                    if (state.SpeedFactorRotationField != null && state.SpeedFactorRotationField.FieldType == typeof(float))
                        state.SpeedFactorRotationField.SetValue(state.Attack, state.SpeedFactorRotationValue);
                    if (state.AccuracyField != null && state.AccuracyField.FieldType == typeof(float))
                        state.AccuracyField.SetValue(state.Attack, state.AccuracyValue);
                    if (state.AccuracyMinField != null && state.AccuracyMinField.FieldType == typeof(float))
                        state.AccuracyMinField.SetValue(state.Attack, state.AccuracyMinValue);
                }
                catch
                {
                }
            }
            _gunStaffTimingStates.Clear();
        }

        private bool IsDualGunStaffEquipped(Player player)
        {
            if (player == null)
                return false;
            ItemDrop.ItemData right = DragonCombat.GetHandItem(player, "m_rightItem");
            ItemDrop.ItemData left = DragonCombat.GetHandItem(player, "m_leftItem");
            return DragonCombat.IsGunStaff(right) && DragonCombat.IsGunStaff(left) && right != left;
        }

        private Color GetMagicProjectileColor(MagicDamageSnapshot damage)
        {
            if (damage == null)
                return new Color(0.62f, 0.20f, 1f, 1f);
            if (damage.Frost >= damage.Fire && damage.Frost >= damage.Lightning && damage.Frost >= damage.Spirit)
                return new Color(0.35f, 0.78f, 1f, 1f);
            if (damage.Fire >= damage.Lightning && damage.Fire >= damage.Spirit)
                return new Color(1f, 0.34f, 0.08f, 1f);
            if (damage.Lightning >= damage.Spirit)
                return new Color(0.55f, 0.82f, 1f, 1f);
            return new Color(0.72f, 0.26f, 1f, 1f);
        }

        private void LaunchAllPhalanx(Player player)
        {
            List<GameObject> copy = new List<GameObject>(_phalanxSwords);
            _phalanxSwords.Clear();
            for (int i = 0; i < copy.Count; i++) LaunchSword(player, copy[i]);
            ShowMessage("PHALANX VOLLEY x" + copy.Count);
        }

        private void LaunchSword(Player player, GameObject sword)
        {
            if (sword == null) return;
            Vector3 origin = sword.transform.position;
            Vector3 dir = AlbedoAimUtility.GetProjectileDirection(player, origin);
            StartCoroutine(SwordProjectile(player, sword, origin, dir));
        }

        private IEnumerator SwordProjectile(Player player, GameObject sword, Vector3 origin, Vector3 dir)
        {
            dir.Normalize();
            float traveled = 0f;
            float range = 50f;
            Vector3 pos = origin;
            while (traveled < range)
            {
                float step = Mathf.Min(45f * Time.deltaTime, range - traveled);
                RaycastHit[] hits = Physics.RaycastAll(pos, dir, step + 0.25f);
                Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
                bool stop = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null || hits[i].collider.isTrigger || IsPlayerCollider(player, hits[i].collider)) continue;
                    Character target = hits[i].collider.GetComponentInParent<Character>();
                    if (target != null && IsEnemy(player, target)) Deal(player, target, 0f, 34f, 34f, 0f, 0f, 0f, 0f, 0f, 8f, false);
                    pos = hits[i].point;
                    stop = true;
                    break;
                }
                if (!stop) pos += dir * step;
                sword.transform.position = pos;
                sword.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                traveled += step;
                if (stop) break;
                yield return null;
            }
            Destroy(sword);
        }

        private void CastArcaneRupture(Player player)
        {
            if (Time.time < _ruptureNextCastAt)
                return;

            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, _ruptureRange.Value, out target)) { ShowMessage("Aim at a physical target"); return; }
            if (_ruptureCharges <= 0) { ShowMessage("Arcane Rupture: no charges"); return; }
            _ruptureCharges--;
            _ruptureRechargeAt.Add(Time.time + Mathf.Max(1f, _ruptureRecharge.Value));
            _ruptureNextCastAt = Time.time + Mathf.Max(0f, _ruptureBuffer.Value);
            StartCoroutine(RuptureRoutine(player, target));
            ShowMessage("Arcane Rupture " + _ruptureCharges + "/" + Mathf.Max(1, _ruptureMaxCharges.Value));
        }

        private IEnumerator RuptureRoutine(Player player, Vector3 target)
        {
            if (_enableVfx.Value) StartCoroutine(RingVfx(target, _ruptureRadius.Value, new Color(0.58f, 0.10f, 0.95f, 0.70f), _ruptureWindup.Value));
            yield return new WaitForSeconds(Mathf.Max(0.1f, _ruptureWindup.Value));
            List<Character> targets = GetSphereTargets(player, target, _ruptureRadius.Value);
            for (int i = 0; i < targets.Count; i++)
            {
                Character enemy = targets[i];
                int id = enemy.GetInstanceID();
                RuptureState state;
                if (!_ruptureStates.TryGetValue(id, out state))
                {
                    state = new RuptureState();
                    _ruptureStates[id] = state;
                }
                state.DamageStack++;
                float mult = state.DamageStack >= 3 ? 2f : 1f;
                if (state.DamageStack >= 3) state.DamageStack = 0;
                Deal(player, enemy, 0f, 0f, 0f, 0f, 0f, 55f * mult, 0f, 45f * mult, 16f, false);

                if (DragonCombat.IsSmallEnemy(enemy)) DragonCombat.Stun(enemy, target);
                else
                {
                    if (state.StunCycleHits <= 0)
                    {
                        DragonCombat.Stun(enemy, target);
                        state.StunCycleHits = 2;
                    }
                    else state.StunCycleHits--;
                }
            }
            if (_enableVfx.Value) CreateLightningBurst(target, _ruptureRadius.Value);
        }

        private void ActivateTwinRift(Player player)
        {
            if (!_riftAwaitingB || _riftA == null)
            {
                DestroyRifts();
                _riftA = CreateRift(player.transform.position);
                _riftAwaitingB = true;
                _riftEndTime = Time.time + Mathf.Max(1f, _riftDuration.Value);
                ShowMessage("Rift A opened - aim within 30m and press M4+R again");
                return;
            }

            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, _riftRange.Value, out target)) { ShowMessage("Rift B needs a physical point within 30m"); return; }
            if (Vector3.Distance(_riftA.transform.position, target) > _riftRange.Value) { ShowMessage("Rift B is too far from Rift A"); return; }
            _riftB = CreateRift(target + Vector3.up * 0.15f);
            LinkRifts();
            _riftAwaitingB = false;
            _riftEndTime = Time.time + Mathf.Max(1f, _riftDuration.Value);
            ShowMessage("Twin Rift linked - look at a Rift and press E to traverse");
        }

        public void TraversePortal(Player player, Vector3 destination)
        {
            PortalReentryUntil = Time.time + 0.65f;
            if (Time.time < _afterimageUntil)
                SpawnAfterimage(player.transform.position, player.transform.rotation);
            TeleportPlayer(player, destination + Vector3.up * 0.20f);
            ApplyFeatherFall(player);
            TriggerPhaseFlow(player);
            ShowMessage("Rift Traversed");
        }

        private void TriggerPhaseFlow(Player player)
        {
            _phaseFlowUntil = Time.time + Mathf.Max(0.5f, _phaseDuration.Value);
            DragonCombat.ApplyTimedBuff(player, "Spellcaster.PhaseFlow", _phaseDuration.Value, 0f, 0f, 0.15f, 0f, 0f, 0.25f, false);
        }

        private float GetRuptureNextRecharge()
        {
            int maxCharges = Mathf.Max(1, _ruptureMaxCharges.Value);
            if (_ruptureCharges >= maxCharges || _ruptureRechargeAt.Count == 0)
                return 0f;
            float next = float.MaxValue;
            for (int i = 0; i < _ruptureRechargeAt.Count; i++)
            {
                float remaining = Mathf.Max(0f, _ruptureRechargeAt[i] - Time.time);
                if (remaining < next)
                    next = remaining;
            }
            return next == float.MaxValue ? 0f : next;
        }

        private void UpdateRuptureCharges()
        {
            if (_ruptureRechargeAt.Count == 0) return;
            for (int i = _ruptureRechargeAt.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _ruptureRechargeAt[i])
                {
                    _ruptureRechargeAt.RemoveAt(i);
                    _ruptureCharges = Mathf.Min(Mathf.Max(1, _ruptureMaxCharges.Value), _ruptureCharges + 1);
                }
            }
        }

        private void UpdateRifts()
        {
            if ((_riftA != null || _riftB != null) && Time.time >= _riftEndTime) DestroyRifts();
        }

        private void UpdatePhalanxVisuals()
        {
            Player player = Player.m_localPlayer;
            if (player == null || _phalanxSwords.Count == 0) return;
            int count = _phalanxSwords.Count;
            for (int i = 0; i < count; i++)
            {
                GameObject sword = _phalanxSwords[i];
                if (sword == null) continue;
                float angle = (360f / (float)Mathf.Max(1, count)) * i + Time.time * 18f;
                Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(0f, 0.35f, 1.6f);
                offset.y = 1.6f + Mathf.Sin(Time.time * 2f + i) * 0.18f;
                sword.transform.position = player.transform.position + offset;
                sword.transform.rotation = Quaternion.LookRotation(player.transform.forward, Vector3.up);
            }
        }

        private bool BeginSkill(Player player, string id, float cooldown, float eitr)
        {
            if (CooldownRemaining(id) > 0f) { ShowCooldown(id); return false; }
            if (!SpendEitr(player, eitr)) { ShowMessage("Not enough Eitr"); return false; }
            StartCooldown(id, cooldown);
            return true;
        }

        private bool SpendEitr(Player player, float amount)
        {
            if (amount <= 0f) return true;
            try
            {
                MethodInfo method = typeof(Player).GetMethod("TryUseEitr", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(float) }, null);
                if (method != null)
                {
                    object result = method.Invoke(player, new object[] { amount });
                    bool success = result == null || Convert.ToBoolean(result);
                    if (success)
                    {
                        DragonCombat.BlockEitrRegen(player, 0.20f);
                        RegisterEitrSpent(amount);
                    }
                    return success;
                }
            }
            catch { }
            return false;
        }

        private void RegisterEitrSpent(float amount)
        {
            if (GetAdvancement(Player.m_localPlayer) != "Wizard") return;
            _overchargeEitrSpent += Mathf.Max(0f, amount);
            if (_overchargeEitrSpent >= 100f)
            {
                _overchargeEitrSpent -= 100f;
                _overchargeUntil = Time.time + 6f;
                ShowMessage("OVERCHARGE");
            }
        }

        private float ScaleWindup(Player player, float seconds)
        {
            float value = DragonCombat.ScaleWindup(player, seconds);
            if (Time.time < _overchargeUntil) value *= 0.80f;
            return Mathf.Max(0f, value);
        }

        private float ElementMultiplier()
        {
            return Time.time < _overchargeUntil ? 1.30f : 1f;
        }

        private void StartCooldown(string id, float normal)
        {
            float seconds = _testingForceCooldowns.Value ? _testingCooldown.Value : normal;
            _cooldowns[id] = Time.time + Mathf.Max(0f, seconds);
        }

        private float CooldownRemaining(string id)
        {
            float end;
            if (!_cooldowns.TryGetValue(id, out end)) return 0f;
            return Mathf.Max(0f, end - Time.time);
        }

        private void ShowCooldown(string id)
        {
            ShowMessage("Cooldown " + CooldownRemaining(id).ToString("0.0") + "s");
        }

        private void Deal(Player attacker, Character target, float blunt, float slash, float pierce, float fire, float frost, float lightning, float poison, float spirit, float push, bool forceStagger)
        {
            if (target == null || target.IsDead()) return;
            float magic = DragonCombat.GetSorcererMagicDamageMultiplier(attacker);
            float e = ElementMultiplier();
            HitData hit = new HitData();
            hit.m_damage.m_blunt = blunt * magic;
            hit.m_damage.m_slash = slash * magic;
            hit.m_damage.m_pierce = pierce * magic;
            hit.m_damage.m_fire = fire * e * magic;
            hit.m_damage.m_frost = frost * e * magic;
            hit.m_damage.m_lightning = lightning * e * magic;
            hit.m_damage.m_poison = poison * e * magic;
            hit.m_damage.m_spirit = spirit * e * magic;
            hit.m_point = target.transform.position;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = push;
            hit.SetAttacker(attacker);
            if (forceStagger) SetForceStagger(hit);
            target.Damage(hit);
        }

        private IEnumerator BurnRoutine(Player attacker, Character target, bool spirit, float duration)
        {
            float end = Time.time + Mathf.Max(0.1f, duration);
            while (target != null && !target.IsDead() && Time.time < end)
            {
                if (spirit) DragonCombat.ApplySpiritBurnTick(attacker, target, 1f);
                else DragonCombat.ApplyFireBurnTick(attacker, target, 1f);
                yield return new WaitForSeconds(1f);
            }
        }

        private List<Character> GetSphereTargets(Player attacker, Vector3 center, float radius)
        {
            List<Character> result = new List<Character>();
            HashSet<int> seen = new HashSet<int>();
            Collider[] hits = Physics.OverlapSphere(center, Mathf.Max(0.1f, radius));
            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();
                if (target == null || !IsEnemy(attacker, target)) continue;
                int id = target.GetInstanceID();
                if (seen.Add(id)) result.Add(target);
            }
            return result;
        }

        private List<Character> GetConeTargets(Player attacker, Vector3 origin, Vector3 forward, float range, float angle)
        {
            List<Character> all = GetSphereTargets(attacker, origin, range);
            List<Character> result = new List<Character>();
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = attacker.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            for (int i = 0; i < all.Count; i++)
            {
                Vector3 to = all[i].transform.position - origin;
                to.y = 0f;
                if (to.sqrMagnitude < 0.01f || Vector3.Angle(forward, to.normalized) <= angle * 0.5f) result.Add(all[i]);
            }
            return result;
        }

        private List<Character> GetBoxTargets(Player attacker, Vector3 origin, Vector3 forward, float length, float width)
        {
            List<Character> result = new List<Character>();
            HashSet<int> seen = new HashSet<int>();
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = attacker.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 center = origin + forward * (length * 0.5f);
            Collider[] hits = Physics.OverlapBox(center, new Vector3(width * 0.5f, 3.5f, length * 0.5f), Quaternion.LookRotation(forward, Vector3.up));
            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();
                if (target == null || !IsEnemy(attacker, target)) continue;
                if (seen.Add(target.GetInstanceID())) result.Add(target);
            }
            return result;
        }

        private bool IsEnemy(Player attacker, Character target)
        {
            if (target == null || target == attacker || target.IsDead() || target is Player) return false;
            try
            {
                MethodInfo m = target.GetType().GetMethod("IsTamed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null && Convert.ToBoolean(m.Invoke(target, null))) return false;
            }
            catch { }
            return true;
        }

        private bool IsBoss(Character target)
        {
            if (target == null) return false;
            try
            {
                MethodInfo m = target.GetType().GetMethod("IsBoss", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null) return Convert.ToBoolean(m.Invoke(target, null));
            }
            catch { }
            return false;
        }

        private void PullToward(Character target, Vector3 center, float strength)
        {
            Rigidbody body = target == null ? null : target.GetComponent<Rigidbody>();
            if (body == null) return;
            Vector3 dir = center - target.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.05f) body.AddForce(dir.normalized * strength, ForceMode.VelocityChange);
        }

        private void ForceStagger(Character target, Vector3 from)
        {
            try
            {
                MethodInfo m = target.GetType().GetMethod("Stagger", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Vector3) }, null);
                if (m != null) m.Invoke(target, new object[] { (target.transform.position - from).normalized });
            }
            catch { }
        }

        private void SetForceStagger(HitData hit)
        {
            try
            {
                FieldInfo f = typeof(HitData).GetField("m_forceStagger", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) f.SetValue(hit, true);
            }
            catch { }
        }

        private Vector3 FlatForward(Player player)
        {
            Vector3 f = player.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            return f.normalized;
        }

        private bool IsHoldingMagicWeapon(Player player)
        {
            return DragonCombat.IsMagicWeapon(GetCurrentWeapon(player));
        }

        private bool IsWizardStaff(ItemDrop.ItemData item)
        {
            if (!DragonCombat.IsMagicWeapon(item) || item == null || item.m_shared == null)
                return false;
            if (DragonCombat.IsGunStaff(item))
                return false;

            string sharedName = item.m_shared.m_name == null ? "" : item.m_shared.m_name.ToLowerInvariant();
            string prefabName = "";
            try
            {
                if (item.m_dropPrefab != null)
                    prefabName = item.m_dropPrefab.name == null ? "" : item.m_dropPrefab.name.ToLowerInvariant();
            }
            catch
            {
            }

            return sharedName.Contains("staff") || prefabName.Contains("staff");
        }

        private ItemDrop.ItemData GetCurrentWeapon(Player player)
        {
            try
            {
                MethodInfo m = player.GetType().GetMethod("GetCurrentWeapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null) return m.Invoke(player, null) as ItemDrop.ItemData;
            }
            catch { }
            return null;
        }

        private bool IsPlayerCollider(Player player, Collider collider)
        {
            if (player == null || collider == null) return false;
            Transform t = collider.transform;
            if (t == player.transform || t.IsChildOf(player.transform)) return true;
            Character c = collider.GetComponentInParent<Character>();
            return c == player;
        }

        private void TeleportPlayer(Player player, Vector3 destination)
        {
            if (player == null)
                return;

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = destination;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            player.transform.position = destination;

            // Reset Valheim fall-state bookkeeping at the destination.
            ResetFloatField(player, "m_maxAirAltitude", destination.y);
            ResetFloatField(player, "m_lastGroundHeight", destination.y);
            ResetFloatField(player, "m_fallSpeed", 0f);
            ResetFloatField(player, "m_fallTimer", 0f);
            ResetBoolField(player, "m_fall", false);
            ResetBoolField(player, "m_falling", false);
        }

        private void ResetFloatField(Player player, string fieldName, float value)
        {
            try
            {
                FieldInfo field = typeof(Character).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(float))
                    field.SetValue(player, value);
            }
            catch { }
        }

        private void ResetBoolField(Player player, string fieldName, bool value)
        {
            try
            {
                FieldInfo field = typeof(Character).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(bool))
                    field.SetValue(player, value);
            }
            catch { }
        }

        private void ApplyFeatherFall(Player player)
        {
            FeatherFallController ff = player.GetComponent<FeatherFallController>();
            if (ff == null) ff = player.gameObject.AddComponent<FeatherFallController>();
            ff.Begin(player);
        }

        private void LinkRifts()
        {
            if (_riftA == null || _riftB == null) return;
            PortalLink a = _riftA.GetComponent<PortalLink>();
            PortalLink b = _riftB.GetComponent<PortalLink>();
            if (a != null && b != null) { a.Other = b; b.Other = a; }
        }

        private Character FindAimedEnemy(Player player, float range, float maxAngle)
        {
            if (player == null)
                return null;
            Vector3 origin = player.GetEyePoint();
            Vector3 forward = AlbedoAimUtility.GetProjectileDirection(player, origin);
            if (forward.sqrMagnitude < 0.01f)
                forward = player.transform.forward;
            forward.Normalize();

            List<Character> all = GetSphereTargets(player, player.transform.position, Mathf.Max(1f, range));
            Character best = null;
            float bestScore = 999f;
            for (int i = 0; i < all.Count; i++)
            {
                Vector3 to = all[i].transform.position + Vector3.up * 0.8f - origin;
                float dist = to.magnitude;
                if (dist <= 0.01f || dist > range)
                    continue;
                float angle = Vector3.Angle(forward, to / dist);
                if (angle > maxAngle)
                    continue;
                float score = angle + dist * 0.01f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = all[i];
                }
            }
            return best;
        }

        private void CreateBeam(Vector3 start, Vector3 end, Color color, float width, float lifetime)
        {
            GameObject obj = new GameObject("DragonsAltarArcaneBeam");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = Mathf.Max(0.03f, width);
            line.endWidth = Mathf.Max(0.03f, width * 0.75f);
            line.startColor = color;
            line.endColor = color;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            Destroy(obj, Mathf.Max(0.05f, lifetime));
        }

        private GameObject CreateRift(Vector3 position)
        {
            GameObject root = new GameObject("DragonsAltarRift");
            root.transform.position = position;
            int riftLayer = LayerMask.NameToLayer("piece_nonsolid");
            if (riftLayer >= 0) root.layer = riftLayer;
            PortalLink link = root.AddComponent<PortalLink>();
            link.Plugin = this;
            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 1.30f, 0f);
            col.radius = 1.45f;
            if (_enableVfx.Value)
            {
                LineRenderer line = root.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 32;
                line.startWidth = 0.12f;
                line.endWidth = 0.12f;
                line.startColor = new Color(0.60f, 0.10f, 1f, 1f);
                line.endColor = new Color(0.95f, 0.25f, 1f, 1f);
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) line.material = new Material(shader);
                for (int i = 0; i < 32; i++)
                {
                    float a = (float)i / 32f * Mathf.PI * 2f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * 1.15f, 1.35f + Mathf.Sin(a) * 1.35f, 0f));
                }
            }
            return root;
        }

        private void DestroyRifts()
        {
            if (_riftA != null) Destroy(_riftA);
            if (_riftB != null) Destroy(_riftB);
            _riftA = null;
            _riftB = null;
            _riftAwaitingB = false;
        }

        private void ClearPhalanx()
        {
            for (int i = 0; i < _phalanxSwords.Count; i++) if (_phalanxSwords[i] != null) Destroy(_phalanxSwords[i]);
            _phalanxSwords.Clear();
            _phalanxVolleyArmed = false;
        }

        private GameObject CreateArcaneSword(Vector3 position)
        {
            GameObject obj = new GameObject("ArcanePhalanxSword");
            obj.transform.position = position;
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 3;
            line.startWidth = 0.18f;
            line.endWidth = 0.04f;
            line.startColor = new Color(0.70f, 0.18f, 1f, 1f);
            line.endColor = new Color(0.95f, 0.55f, 1f, 1f);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) line.material = new Material(shader);
            line.SetPosition(0, new Vector3(0f, 0f, -0.75f));
            line.SetPosition(1, new Vector3(0f, 0f, 0.55f));
            line.SetPosition(2, new Vector3(0f, 0f, 0.95f));
            return obj;
        }

        private GameObject CreateOrb(Vector3 pos, float size, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "ArcaneProjectile";
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * size;
            Collider c = obj.GetComponent<Collider>();
            if (c != null) Destroy(c);
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) r.material = new Material(shader);
                r.material.color = color;
            }
            return obj;
        }

        private GameObject CreateIceChunk(Vector3 pos, float radius)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "GlacialDescentChunk";
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(radius * 1.2f, radius * 1.4f, radius * 1.2f);
            obj.transform.rotation = Quaternion.Euler(18f, 35f, 12f);
            Collider c = obj.GetComponent<Collider>(); if (c != null) Destroy(c);
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null) { Shader s = Shader.Find("Sprites/Default"); if (s != null) r.material = new Material(s); r.material.color = new Color(0.55f, 0.90f, 1f, 0.78f); }
            return obj;
        }

        private GameObject CreateMeteor(Vector3 pos)
        {
            return CreateOrb(pos, 2.4f, new Color(1f, 0.20f, 0.02f, 1f));
        }

        private void CreateStoneSpikes(Vector3 center, float radius)
        {
            for (int i = 0; i < 10; i++)
            {
                float a = (float)i / 10f * Mathf.PI * 2f;
                Vector3 pos = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * 0.65f;
                GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spike.name = "StonefangSpike";
                spike.transform.position = pos + Vector3.up * 1.5f;
                spike.transform.localScale = new Vector3(0.55f, 3f, 0.55f);
                spike.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 35f);
                Collider c = spike.GetComponent<Collider>(); if (c != null) Destroy(c);
                Destroy(spike, 0.75f);
            }
        }

        private IEnumerator GreatbladeVfx(Vector3 origin, Vector3 forward, float length, float width)
        {
            GameObject obj = new GameObject("AstralGreatbladeVfx");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = Mathf.Max(0.6f, width);
            line.endWidth = 0.25f;
            line.startColor = new Color(0.70f, 0.20f, 1f, 1f);
            line.endColor = new Color(0.95f, 0.65f, 1f, 1f);
            Shader shader = Shader.Find("Sprites/Default"); if (shader != null) line.material = new Material(shader);
            line.SetPosition(0, origin + Vector3.up * 7f);
            line.SetPosition(1, origin + forward * length + Vector3.up * 0.2f);
            yield return new WaitForSeconds(0.35f);
            Destroy(obj);
        }

        private IEnumerator ConeVfx(Vector3 origin, Vector3 forward, float range, float angle, Color color)
        {
            float end = Time.time + 0.35f;
            while (Time.time < end)
            {
                float t = 1f - (end - Time.time) / 0.35f;
                StartCoroutine(RingVfx(origin + forward * range * t, Mathf.Max(0.4f, range * t * Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad)), color, 0.12f));
                yield return new WaitForSeconds(0.07f);
            }
        }

        private IEnumerator RingVfx(Vector3 center, float radius, Color color, float duration)
        {
            GameObject obj = new GameObject("SorcererRingVfx");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 40;
            line.startWidth = 0.10f;
            line.endWidth = 0.10f;
            line.startColor = color;
            line.endColor = color;
            Shader shader = Shader.Find("Sprites/Default"); if (shader != null) line.material = new Material(shader);
            for (int i = 0; i < 40; i++)
            {
                float a = (float)i / 40f * Mathf.PI * 2f;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0.08f, Mathf.Sin(a) * radius));
            }
            yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
            Destroy(obj);
        }

        private void CreateLightningBurst(Vector3 center, float radius)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = (float)i / 8f * Mathf.PI * 2f;
                Vector3 p = center + new Vector3(Mathf.Cos(a) * radius * 0.7f, 0f, Mathf.Sin(a) * radius * 0.7f);
                GameObject obj = new GameObject("ArcaneLightning");
                LineRenderer line = obj.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.12f;
                line.endWidth = 0.28f;
                line.startColor = new Color(0.65f, 0.25f, 1f, 1f);
                line.endColor = new Color(0.45f, 0.85f, 1f, 1f);
                Shader shader = Shader.Find("Sprites/Default"); if (shader != null) line.material = new Material(shader);
                line.SetPosition(0, p + Vector3.up * 10f);
                line.SetPosition(1, p);
                Destroy(obj, 0.25f);
            }
        }

        private void RefreshFoodStats(Player player)
        {
            if (player == null)
                return;
            try
            {
                MethodInfo method = player.GetType().GetMethod(
                    "UpdateFood",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(float), typeof(bool) },
                    null
                );
                if (method != null)
                    method.Invoke(player, new object[] { 0f, true });
            }
            catch
            {
            }
        }

        private string GetClass(Player player) { return ReadData(player, ClassDataKey); }
        private string GetAdvancement(Player player) { return ReadData(player, AdvancementDataKey); }

        private string ReadData(Player player, string key)
        {
            try
            {
                FieldInfo f = typeof(Player).GetField("m_customData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                IDictionary data = f == null ? null : f.GetValue(player) as IDictionary;
                if (data == null || !data.Contains(key) || data[key] == null) return "";
                return data[key].ToString();
            }
            catch { return ""; }
        }

        private void ShowMessage(string text)
        {
            if (MessageHud.instance != null) MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text);
        }

        private void EnsureHudStyles()
        {
            if (_hudStyle != null) return;

            _hudStyle = new GUIStyle(GUI.skin.box);
            _hudStyle.fontSize = 17;
            _hudStyle.fontStyle = FontStyle.Bold;
            _hudStyle.alignment = TextAnchor.MiddleCenter;
            _hudStyle.normal.textColor = Color.white;
            _hudStyle.normal.background = MakeTexture(new Color(0.055f, 0.060f, 0.075f, 0.86f));

            _hudIconStyle = new GUIStyle(GUI.skin.box);
            _hudIconStyle.fontSize = 17;
            _hudIconStyle.fontStyle = FontStyle.Bold;
            _hudIconStyle.alignment = TextAnchor.MiddleCenter;
            _hudIconStyle.normal.textColor = Color.white;
            _hudIconStyle.normal.background = MakeTexture(new Color(0.09f, 0.10f, 0.13f, 0.92f));

            _hudKeyStyle = new GUIStyle(GUI.skin.label);
            _hudKeyStyle.fontSize = 10;
            _hudKeyStyle.fontStyle = FontStyle.Bold;
            _hudKeyStyle.alignment = TextAnchor.UpperLeft;
            _hudKeyStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f, 1f);

            _hudCooldownStyle = new GUIStyle(GUI.skin.label);
            _hudCooldownStyle.fontSize = 11;
            _hudCooldownStyle.fontStyle = FontStyle.Bold;
            _hudCooldownStyle.alignment = TextAnchor.LowerCenter;
            _hudCooldownStyle.normal.textColor = new Color(1f, 0.82f, 0.35f, 1f);

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 13;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.normal.textColor = new Color(0.78f, 0.45f, 1f, 1f);
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private string GetSkillInitials(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "?";
            string[] words = name.Split(new char[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
            string result = "";
            for (int i = 0; i < words.Length && result.Length < 3; i++)
            {
                string word = words[i];
                if (string.IsNullOrEmpty(word) || char.IsDigit(word[0]))
                    continue;
                result += char.ToUpperInvariant(word[0]);
            }
            return string.IsNullOrEmpty(result) ? "?" : result;
        }

        private bool IsAdvancedSkillbookOpen()
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type type = assemblies[i].GetType("AlbedosCustomClassesAdvanced.AdvancedPlugin", false);
                    if (type == null)
                        continue;
                    FieldInfo instanceField = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    object instance = instanceField == null ? null : instanceField.GetValue(null);
                    if (instance == null)
                        return false;
                    FieldInfo openField = type.GetField("_skillbookOpen", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (openField != null)
                    {
                        object value = openField.GetValue(instance);
                        if (value is bool)
                            return (bool)value;
                    }
                    return false;
                }
            }
            catch
            {
            }
            return false;
        }

        private void OnGUI()
        {
            if (!_showHud.Value || Plugin.IsClassPanelOpen || IsAdvancedSkillbookOpen() || DragonCombat.IsGameplayHudSuppressed())
                return;

            Player player = Player.m_localPlayer;
            if (player == null || GetClass(player) != "Sorcerer")
                return;

            EnsureHudStyles();
            string adv = GetAdvancement(player);

            List<string> names = new List<string>();
            List<float> cooldowns = new List<float>();
            names.Add("Flame Burst"); cooldowns.Add(0f);
            names.Add("Glacial Descent"); cooldowns.Add(0f);
            names.Add("Stonefang Eruption"); cooldowns.Add(0f);

            if (adv == "Wizard")
            {
                names.Add("Gravity Dominion"); cooldowns.Add(CooldownRemaining("Wizard.GravityDominion"));
                names.Add("Astral Greatblade"); cooldowns.Add(CooldownRemaining("Wizard.AstralGreatblade"));
                names.Add("Frost Nova"); cooldowns.Add(CooldownRemaining("Wizard.FrostNova"));
                names.Add("Meteor Fall"); cooldowns.Add(CooldownRemaining("Wizard.MeteorFall"));
                names.Add("Astral Railcannon"); cooldowns.Add(CooldownRemaining("Wizard.AstralRailcannon"));
                names.Add("Elemental Cataclysm"); cooldowns.Add(CooldownRemaining("Wizard.ElementalCataclysm"));
            }
            else if (adv == "Spellcaster")
            {
                names.Add("Rift Echo"); cooldowns.Add(CooldownRemaining("Spellcaster.RiftEcho"));
                names.Add("Void Step"); cooldowns.Add(CooldownRemaining("Spellcaster.VoidStep"));
                names.Add("Arcane Phalanx"); cooldowns.Add(CooldownRemaining("Spellcaster.ArcanePhalanx"));
                names.Add("Afterimage Arsenal"); cooldowns.Add(CooldownRemaining("Spellcaster.AfterimageArsenal"));
                names.Add("Arcane Rupture"); cooldowns.Add(GetRuptureNextRecharge());
            }

            int count = names.Count;
            float scale = Mathf.Clamp(_hudScale.Value, 0.65f, 1.45f);
            float size = 54f * scale;
            float gap = 5f * scale;
            float passiveGap = string.IsNullOrEmpty(adv) ? 0f : 10f * scale;
            float passiveSize = string.IsNullOrEmpty(adv) ? 0f : size;
            float totalWidth = size * count + gap * Mathf.Max(0, count - 1) + passiveGap + passiveSize;
            float x = (Screen.width - totalWidth) * 0.5f;
            float reserve = Mathf.Clamp(_hudBottomOffset.Value, 70f, 260f) * scale;
            float y = Screen.height - reserve - size;

            GUI.Label(new Rect(x, y - 21f * scale, totalWidth, 18f * scale), "SORCERER" + (string.IsNullOrEmpty(adv) ? "" : "  >  " + adv.ToUpper()), _titleStyle);

            for (int i = 0; i < count; i++)
            {
                Rect rect = new Rect(x + i * (size + gap), y, size, size);
                GUI.Box(rect, GetSkillInitials(names[i]), _hudIconStyle);
                GUI.Label(new Rect(rect.x + 4f * scale, rect.y + 2f * scale, 18f * scale, 16f * scale), (i + 1).ToString(), _hudKeyStyle);
                float cooldown = cooldowns[i];
                if (cooldown > 0.05f)
                    GUI.Label(new Rect(rect.x + 3f * scale, rect.y + 30f * scale, rect.width - 6f * scale, 20f * scale), cooldown.ToString("0.0"), _hudCooldownStyle);
            }

            if (!string.IsNullOrEmpty(adv))
            {
                float px = x + count * (size + gap) - gap + passiveGap;
                Rect passiveRect = new Rect(px, y, size, size);
                string passiveName = adv == "Wizard" ? "Overcharge" : "Riftwalker";
                GUI.Box(passiveRect, GetSkillInitials(passiveName), _hudStyle);
                GUI.Label(new Rect(passiveRect.x + 4f * scale, passiveRect.y + 2f * scale, 20f * scale, 16f * scale), "R", _hudKeyStyle);
            }

            if (adv == "Wizard" && _wizardChargeActive)
                GUI.Label(new Rect(x, y + size + 3f * scale, totalWidth, 18f * scale), "STAFF CHARGE  " + _wizardChargeStacks.ToString() + "/3", _titleStyle);
        }
    }
}
