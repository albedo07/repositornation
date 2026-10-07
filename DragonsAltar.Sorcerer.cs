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
        public const string ModVersion = "0.25.55";

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
            DragonCombat.RegisterSkillModule(CastFromTree, CooldownForTree);
            DragonCombat.RegisterStackQuery(StackQuery);

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
            _testingForceCooldowns = Config.Bind("Testing", "ForceCooldowns", false, "Force ordinary Sorcerer cooldowns to the testing value.");
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

            _wizardHeldStaffEitrRegen = Config.Bind("Wizard Staff Weapon Mastery", "HeldStaffEitrRegenPercent_v0231", 0f, "Additional Eitr Regen while the Wizard is actually holding a Staff.");
            _wizardHeldStaffFlatEitr = Config.Bind("Wizard Staff Weapon Mastery", "HeldStaffFlatEitr_v0231", 0f, "Flat Max Eitr while the Wizard is actually holding a Staff.");
            _wizardChargeThreshold = Config.Bind("Wizard Staff Charge", "SecondsPerStack_v0111", 1f, "Seconds per charge stack (v0.23.3: 1 stack per second). Maximum three stacks.");
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
            _riftEchoDuration = Config.Bind("Spellcaster Rift Echo", "Duration", 16f, "Echo window (normal and Ascended).");
            _riftEchoDamageMultiplier = Config.Bind("Spellcaster Rift Echo", "EchoDamageMultiplier", 1f, "Each echo = 100% of your attack (Horizon Walker already deals less).");
            _afterimageCooldown = Config.Bind("Spellcaster Afterimage Arsenal", "Cooldown", 18f, "Seconds.");
            _afterimageEitr = Config.Bind("Spellcaster Afterimage Arsenal", "EitrCost", 50f, "Eitr cost before Spellcaster reduction.");
            _afterimageDuration = Config.Bind("Spellcaster Afterimage Arsenal", "Duration", 16f, "Afterimage window (normal and Ascended).");
            _afterimageMax = Config.Bind("Spellcaster Afterimage Arsenal", "MaxAfterimages", 3, "Maximum active afterimages.");
            _afterimageDamageMultiplier = Config.Bind("Spellcaster Afterimage Arsenal", "DamageMultiplier", 1f, "Each afterimage shot = 100% of your attack.");
            _gunStaffFireRateMultiplier = Config.Bind("Spellcaster Gun Staff", "SingleAttackSpeedMultiplier_v0123", 1.5f, "Single Staff/Wand baseline: +50% Attack Speed. Rapid Gun Staff cooldown uses the same 1.5x target.");
            _dualGunStaffAttackSpeedMultiplier = Config.Bind("Spellcaster Gun Staff", "DualAttackSpeedMultiplier_v0123", 2f, "Dual Gun Staves baseline: +100% Attack Speed. Example: 0.50s cadence becomes 0.25s.");
            _gunStaffFiringMoveBonus = Config.Bind("Spellcaster Gun Staff", "LegacyFiringMovementBonusPercent", 0f, "Legacy setting retained only for config compatibility. Firing no longer grants artificial movement speed.");

            BindWizardV0231();
            BindSpellcasterV0232();
            _ruptureCharges = RuptureMax();

            try
            {
                _harmony = new Harmony(ModGuid);
                InstallPatches();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Sorcerer optional patches failed: " + ex.Message);
            }

            Logger.LogInfo(ModName + " v" + ModVersion + " loaded. Sorcerer / Archmage / Horizon Walker alpha is ready.");
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

            // v0.23.6: mimics copy every real shot / swing (one call per projectile burst or melee hit
            // check), so holding Mouse1 with a Gun Staff keeps them firing.
            MethodInfo shotPostfix = typeof(SorcererPlugin).GetMethod("AttackShotPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            int shotHooks = 0;
            for (int i = 0; i < attacks.Length; i++)
            {
                if (attacks[i].Name == "FireProjectileBurst" || attacks[i].Name == "DoMeleeAttack" || attacks[i].Name == "DoAreaAttack")
                {
                    PatchWithHarmony(attacks[i], null, new HarmonyMethod(shotPostfix));
                    shotHooks++;
                }
            }
            _mimicShotHooks = shotHooks > 0;

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
            if (!Instance._mimicShotHooks) Instance.QueueMimicAttack(player, weapon);
            Instance.OnNormalMagicAttack(player, weapon);
        }

        private bool _mimicShotHooks;
        private static FieldInfo _attackCharacterField;
        private static FieldInfo _attackWeaponField;

        private static void AttackShotPostfix(Attack __instance)
        {
            if (Instance == null || __instance == null) return;
            try
            {
                if (_attackCharacterField == null) _attackCharacterField = typeof(Attack).GetField("m_character", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (_attackWeaponField == null) _attackWeaponField = typeof(Attack).GetField("m_weapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Player player = _attackCharacterField == null ? null : _attackCharacterField.GetValue(__instance) as Player;
                if (player == null || player != Player.m_localPlayer) return;
                ItemDrop.ItemData weapon = _attackWeaponField == null ? null : _attackWeaponField.GetValue(__instance) as ItemDrop.ItemData;
                Instance.QueueMimicAttack(player, weapon);
            }
            catch { }
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

            if (GetClass(player) != "Sorcerer" || GetAdvancement(player) != "Spellcaster")
                ClearClones(); // Astral Clones leave on class change
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
                DragonCombat.ApplyTimedBuff(player, "Wizard.Overcharge", 0.35f, 0f, 0f, 0f, 0f, 0f, Mathf.Max(0f, _ocRegen.Value) / 100f, false);

            if (advancement == "Spellcaster")
            {
                bool dualGunStaff = IsDualGunStaffEquipped(player);
                float speed = Mathf.Max(1f, _yyAttackSpeed.Value); // Yin and Yang: attack interval -50%, single or dual

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
            {
                UpdateSpellcasterHeldFire(player);
                UpdateCloneHold(player);
                UpdateVoidCharges(player);
                if (player.IsDead()) ClearClones();
            }
            else
                _nextSpellcasterHeldFire = 0f;

            UpdateWizardStaffCharge(player, advancement);

            // v0.22.0: the universal Skill Tree hotbar owns skill input (CastFromTree below).
            if (DragonCombat.IsTreeHotbarActive(player))
                return;

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
                else if (Input.GetKeyDown(_passive.Value)) ActivateRiftWalker(player);
            }
        }

        // v0.22.0 universal tree hotbar: Wizard / Spellcaster skills by tree id.
        private bool CastFromTree(Player player, string id)
        {
            if (player == null || player.IsDead()) return false;
            string adv = GetAdvancement(player);
            switch (id)
            {
                case "meteor_fall": if (adv == "Wizard") CastMeteorFall(player); return true;
                case "gravity_dominion": if (adv == "Wizard") CastGravityDominion(player); return true;
                case "astral_railcannon": if (adv == "Wizard") CastAstralRailcannon(player); return true;
                case "astral_greatblade": if (adv == "Wizard") CastAstralGreatblade(player); return true;
                case "frost_nova": if (adv == "Wizard") CastFrostNova(player); return true;
                case "elemental_cataclysm": if (adv == "Wizard") CastElementalCataclysm(player); return true;
                case "clockwork": if (adv == "Wizard") CastClockwork(player); return true;
                case "glacial_descent_ascended": if (adv == "Wizard") CastAscendedGlacial(player); return true;
                case "arcane_phalanx": if (adv == "Spellcaster") CastArcanePhalanx(player); return true;
                case "afterimage_arsenal": if (adv == "Spellcaster") CastAfterimageArsenal(player); return true;
                case "void_step": if (adv == "Spellcaster") CastVoidStep(player); return true;
                case "rift_echo": if (adv == "Spellcaster") CastRiftEcho(player); return true;
                case "arcane_rupture": if (adv == "Spellcaster") CastArcaneRupture(player); return true;
                case "gravity_blast": if (adv == "Spellcaster") CastGravityBlast(player); return true;
                case "rift_walker": if (adv == "Spellcaster") ActivateRiftWalker(player); return true;
                case "stonefang_eruption_ascended": if (adv == "Spellcaster") CastAscendedStonefang(player); return true;
            }
            return false;
        }

        private float CooldownForTree(string id)
        {
            switch (id)
            {
                case "meteor_fall": return CooldownRemaining("Wizard.MeteorFall");
                case "gravity_dominion": return CooldownRemaining("Wizard.GravityDominion");
                case "astral_railcannon": return CooldownRemaining("Wizard.AstralRailcannon");
                case "astral_greatblade": return CooldownRemaining("Wizard.AstralGreatblade");
                case "frost_nova": return CooldownRemaining("Wizard.FrostNova");
                case "elemental_cataclysm": return CooldownRemaining("Wizard.ElementalCataclysm");
                case "clockwork": return CooldownRemaining("Wizard.Clockwork");
                case "glacial_descent": return CooldownRemaining("Sorcerer.GlacialDescent");
                case "arcane_phalanx": return CooldownRemaining("Spellcaster.ArcanePhalanx");
                case "afterimage_arsenal":
                    if (Player.m_localPlayer != null && IsSpellAscended(Player.m_localPlayer, "afterimage_arsenal")) return CloneCooldownRemaining();
                    return CooldownRemaining("Spellcaster.AfterimageArsenal");
                case "void_step": return CooldownRemaining("Spellcaster.VoidStep");
                case "rift_echo": return CooldownRemaining("Spellcaster.RiftEcho");
                case "gravity_blast": return CooldownRemaining("Spellcaster.GravityBlast");
                case "rift_walker": return CooldownRemaining("Spellcaster.RiftWalker");
                case "stonefang_eruption": return CooldownRemaining("Sorcerer.StonefangEruption");
                case "arcane_rupture": return _ruptureCharges > 0 ? 0f : GetRuptureNextRecharge();
            }
            return 0f;
        }

        private void CastFlameBurst(Player player)
        {
            if (!BeginSkill(player, "Sorcerer.FlameBurst", _flameCooldown.Value, _flameEitr.Value)) return;
            float windup = ScaleWindup(player, 0.4f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlayClip(player, "sorc_flame", windup);
            StartCoroutine(FlameBurstRoutine(player, windup));
        }

        private IEnumerator FlameBurstRoutine(Player player, float windup)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            Vector3 origin = player.transform.position + Vector3.up;
            Vector3 forward = FlatForward(player);
            List<Character> targets = GetConeTargets(player, origin, forward, DragonCombat.M(_flameRange.Value), _flameAngle.Value);
            for (int i = 0; i < targets.Count; i++)
            {
                Deal(player, targets[i], 0f, 0f, 0f, 34f, 0f, 0f, 0f, 0f, 6f, false);
                StartCoroutine(BurnRoutine(player, targets[i], false, 6f, 2f));
            }
            if (_enableVfx.Value) StartCoroutine(ConeVfx(origin, forward, DragonCombat.M(_flameRange.Value), _flameAngle.Value, new Color(1f, 0.28f, 0.05f, 0.95f)));
            ShowMessage("Flame Burst");
        }

        private void CastGlacialDescent(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_iceRange.Value), out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Sorcerer.GlacialDescent", _iceCooldown.Value, _iceEitr.Value)) return;
            float windup = ScaleWindup(player, 1f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlayClip(player, "sorc_glacial", windup);
            StartCoroutine(GlacialRoutine(player, target, windup));
        }

        private IEnumerator GlacialRoutine(Player player, Vector3 target, float windup)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(target, 8f);
            GameObject chunk = _enableVfx.Value ? CreateIceChunk(sky, DragonCombat.M(_iceRadius.Value)) : null;
            float drop = Mathf.Max(0.12f, DragonCombat.GetSkySummonDropTime());
            float e = 0f;
            while (e < drop)
            {
                if (chunk != null) chunk.transform.position = Vector3.Lerp(sky, target + Vector3.up * 0.8f, e / drop);
                e += Time.deltaTime;
                yield return null;
            }
            if (chunk != null) Destroy(chunk, 0.30f);
            List<Character> targets = GetSphereTargets(player, target, DragonCombat.M(_iceRadius.Value));
            for (int i = 0; i < targets.Count; i++)
            {
                Deal(player, targets[i], 22f, 0f, 0f, 0f, 36f, 0f, 0f, 0f, 12f, false);
                DragonCombat.ApplyFrost(targets[i], 6f);
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(target, DragonCombat.M(_iceRadius.Value), new Color(0.55f, 0.90f, 1f, 0.95f), 0.7f));
            ShowMessage("Glacial Descent");
        }

        private void CastStonefang(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_stoneRange.Value), out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Sorcerer.Stonefang", _stoneCooldown.Value, _stoneEitr.Value)) return;
            float windup = ScaleWindup(player, 0.8f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlayClip(player, "sorc_stonefang", windup);
            StartCoroutine(StonefangRoutine(player, target, windup));
        }

        private IEnumerator StonefangRoutine(Player player, Vector3 target, float windup)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            List<Character> targets = GetSphereTargets(player, target, DragonCombat.M(_stoneRadius.Value));
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
            if (_enableVfx.Value) CreateStoneSpikes(target, DragonCombat.M(_stoneRadius.Value));
            ShowMessage("Stonefang Eruption");
        }

        // =====================================================================================
        // v0.23.1 WIZARD REWORK (Framework 3.1 + approved Ascensions). Damage lives in config
        // sections "Wizard <Skill> Damage"; every skill scales with its Tier (+10% per Tier),
        // Overcharge (+40% Magic Damage) and Clockwork (+30% Skill Damage, central runtime).
        // =====================================================================================
        private sealed class WizDamage
        {
            public ConfigEntry<float> Blunt, Slash, Pierce, Fire, Frost, Lightning, Poison, Spirit;
        }

        private WizDamage _gravityDmg, _bladeDmg, _novaDmg, _meteorDmg, _cataclysmDmg, _glacialAscDmg;
        private ConfigEntry<float> _gravityWindup, _gravityTick, _gravityPull, _gravityExpose, _gravityCripple;
        private ConfigEntry<float> _bladeWindup, _bladeBurnPercent, _bladeBurnSeconds;
        private ConfigEntry<float> _novaWindup, _novaFrost;
        private ConfigEntry<float> _meteorWindup, _meteorBurnPercent, _meteorBurnSeconds;
        private ConfigEntry<float> _railShotMultiplier, _railRecoil;
        private ConfigEntry<float> _cataclysmWindup, _cataclysmMaxMultiplier, _cataclysmExpose;
        private ConfigEntry<float> _ocThreshold, _ocDuration, _ocBuffer, _ocWindup, _ocRegen, _ocMagic;
        private ConfigEntry<float> _clockRadius, _clockDuration, _clockCooldown, _clockSkillDamage, _clockCdr;
        private ConfigEntry<float> _gdAscRadius, _gdAscCore, _gdAscCoreDamage, _gdAscFreeze, _gdAscBossSlow, _gdAscCooldown, _gdAscEitr, _gdAscWindup, _gdAscRange;
        private ConfigEntry<float> _mfAscWindup;
        private ConfigEntry<float> _mfAscPercent, _mfAscRadius, _mfAscGap, _mfAscSpread;
        private ConfigEntry<float> _gvAscRadius, _gvAscDuration, _gvAscBig, _gvAscBoss, _gvAscBlast, _gvAscStun;
        private ConfigEntry<float> _rcAscWindup, _rcAscDuration, _rcAscTick, _rcAscTickPercent;
        private ConfigEntry<float> _gbAscWindup, _gbAscGap, _gbAscPercent;
        private ConfigEntry<float> _fnAscDuration, _fnAscTickPercent, _fnAscBlast, _fnAscFreeze, _fnAscBossSlow;
        private ConfigEntry<float> _ccAscDelay, _ccAscPercent;
        private float _overchargeLockedUntil;

        private WizDamage BindWizDamage(string section, float blunt, float slash, float pierce, float fire, float frost, float lightning, float poison, float spirit)
        {
            WizDamage d = new WizDamage();
            d.Blunt = Config.Bind(section, "Blunt", blunt, "Blunt damage.");
            d.Slash = Config.Bind(section, "Slash", slash, "Slash damage.");
            d.Pierce = Config.Bind(section, "Pierce", pierce, "Pierce damage.");
            d.Fire = Config.Bind(section, "Fire", fire, "Fire damage.");
            d.Frost = Config.Bind(section, "Frost", frost, "Frost damage.");
            d.Lightning = Config.Bind(section, "Lightning", lightning, "Lightning damage.");
            d.Poison = Config.Bind(section, "Poison", poison, "Poison damage.");
            d.Spirit = Config.Bind(section, "Spirit", spirit, "Spirit damage.");
            return d;
        }

        private void BindWizardV0231()
        {
            const string g = "Wizard Gravity Dominion";
            _gravityWindup = Config.Bind(g, "Windup", 1f, "Wind up.");
            _gravityTick = Config.Bind(g, "HitInterval", 1f, "Seconds between pulses.");
            _gravityPull = Config.Bind(g, "PullStrength", 7f, "Pull impulse on Small enemies per pulse.");
            _gravityExpose = Config.Bind(g, "ExposeDuration", 5f, "Expose (refreshed while inside).");
            _gravityCripple = Config.Bind(g, "CrippleDuration", 2f, "Cripple on Big enemies (refreshed while inside).");
            _gravityDmg = BindWizDamage("Wizard Gravity Dominion Damage", 0f, 0f, 0f, 0f, 0f, 14f, 0f, 14f);
            const string b = "Wizard Astral Greatblade";
            _bladeWindup = Config.Bind(b, "Windup", 1f, "Quick release wind up (charging adds up to AdditionalChargeTime).");
            _bladeBurnPercent = Config.Bind(b, "SpiritBurnPercentPerTick", 6f, "Spirit Burn: every 0.5s, % of the slam's Spirit damage.");
            _bladeBurnSeconds = Config.Bind(b, "SpiritBurnDuration", 6f, "Spirit Burn duration.");
            _bladeDmg = BindWizDamage("Wizard Astral Greatblade Damage", 55f, 62f, 0f, 0f, 0f, 0f, 0f, 48f);
            const string n = "Wizard Frost Nova";
            _novaWindup = Config.Bind(n, "Windup", 1f, "Wind up.");
            _novaFrost = Config.Bind(n, "FrostDuration", 8f, "Frost on Small, Big and Boss.");
            _novaDmg = BindWizDamage("Wizard Frost Nova Damage", 0f, 0f, 0f, 0f, 62f, 0f, 0f, 0f);
            const string m = "Wizard Meteor Fall";
            _meteorWindup = Config.Bind(m, "Windup", 1.2f, "Wind up before charging (hold the key to charge, 1 stack per second, max 3).");
            _meteorBurnPercent = Config.Bind(m, "FireBurnPercentPerTick", 6f, "Fire Burn: every 0.5s, % of the meteor's Fire damage.");
            _meteorBurnSeconds = Config.Bind(m, "FireBurnDuration", 6f, "Fire Burn duration.");
            _meteorDmg = BindWizDamage("Wizard Meteor Fall Damage", 80f, 0f, 0f, 80f, 0f, 0f, 0f, 0f);
            const string r = "Wizard Astral Railcannon";
            _railShotMultiplier = Config.Bind(r, "WeaponDamageMultiplier", 1.75f, "The shot deals the held Staff's damage x this.");
            _railRecoil = Config.Bind(r, "Recoil", 4.5f, "Backwards push on the Wizard.");
            const string c = "Wizard Elemental Cataclysm";
            _cataclysmWindup = Config.Bind(c, "MinimumWindup", 0.5f, "Quick release wind up.");
            _cataclysmMaxMultiplier = Config.Bind(c, "FullChargeMultiplier", 3f, "Damage at full charge (scales continuously from 1x).");
            _cataclysmExpose = Config.Bind(c, "ExposeDuration", 15f, "Expose: the only ailment.");
            _cataclysmDmg = BindWizDamage("Wizard Elemental Cataclysm Damage", 55f, 55f, 55f, 55f, 55f, 55f, 55f, 0f);
            const string o = "Wizard Overcharge";
            _ocThreshold = Config.Bind(o, "EitrSpentToTrigger", 300f, "Eitr spent to activate Overcharge.");
            _ocDuration = Config.Bind(o, "Duration", 12f, "Seconds active.");
            _ocBuffer = Config.Bind(o, "AccumulationBuffer", 5f, "Seconds after it ends before Eitr spent counts again.");
            _ocWindup = Config.Bind(o, "WindupSpeedPercent", 40f, "Faster wind up for long-cast skills.");
            _ocRegen = Config.Bind(o, "EitrRegenPercent", 40f, "Eitr Regen while active.");
            _ocMagic = Config.Bind(o, "MagicDamagePercent", 40f, "Magic Damage while active.");
            const string k = "Wizard Clockwork";
            _clockRadius = Config.Bind(k, "Radius", 10f, "Players within this radius at cast get Clockwork (snapshot).");
            _clockDuration = Config.Bind(k, "Duration", 22f, "Seconds.");
            _clockCooldown = Config.Bind(k, "Cooldown", 600f, "Seconds (10 min).");
            _clockSkillDamage = Config.Bind(k, "SkillDamagePercent", 30f, "+Skill Damage.");
            _clockCdr = Config.Bind(k, "CooldownReductionPercent", 50f, "Non-Grace cooldowns that START while active are this much shorter.");
            const string ga = "Wizard Glacial Descent Ascended";
            _gdAscRadius = Config.Bind(ga, "Radius", 8f, "Ascended radius.");
            _gdAscCore = Config.Bind(ga, "CoreRadius", 3f, "Central radius: bigger hit + Freeze.");
            _gdAscCoreDamage = Config.Bind(ga, "CoreDamagePercent", 135f, "Damage inside the core.");
            _gdAscFreeze = Config.Bind(ga, "FreezeDuration", 1.5f, "Freeze inside the core.");
            _gdAscBossSlow = Config.Bind(ga, "BossSlowPercent", 15f, "Bosses are slowed instead of Frozen.");
            _gdAscCooldown = Config.Bind(ga, "Cooldown", 10f, "Seconds.");
            _gdAscEitr = Config.Bind(ga, "EitrCost", 28f, "Eitr cost.");
            _gdAscWindup = Config.Bind(ga, "Windup", 1f, "Wind up.");
            _gdAscRange = Config.Bind(ga, "GroundPACRange", 50f, "Ground PAC range.");
            _glacialAscDmg = BindWizDamage("Wizard Glacial Descent Ascended Damage", 34f, 0f, 0f, 0f, 42f, 0f, 0f, 0f);
            const string ma = "Wizard Meteor Fall Ascended";
            _mfAscPercent = Config.Bind(ma, "SmallMeteorPercent", 12f, "Each small meteor: % of the charged main meteor.");
            _mfAscWindup = Config.Bind(ma, "Windup", 0.5f, "Ascended wind up (the charge timer still starts on the key press).");
            _mfAscRadius = Config.Bind(ma, "SmallMeteorRadius", 3f, "Small meteor radius.");
            _mfAscGap = Config.Bind(ma, "Interval", 0.25f, "Seconds between small meteors (3, or 5 at full charge).");
            _mfAscSpread = Config.Bind(ma, "Spread", 5f, "Distance of the small meteors from the target point.");
            const string gva = "Wizard Gravity Dominion Ascended";
            _gvAscRadius = Config.Bind(gva, "Radius", 10f, "Ascended radius.");
            _gvAscDuration = Config.Bind(gva, "Duration", 7f, "Seconds.");
            _gvAscBig = Config.Bind(gva, "BigPullPercent", 40f, "Pull strength on Big enemies.");
            _gvAscBoss = Config.Bind(gva, "BossPullPercent", 20f, "Pull strength on Bosses.");
            _gvAscBlast = Config.Bind(gva, "EndBlastPercent", 25f, "End explosion: % of the normal skill's full damage.");
            _gvAscStun = Config.Bind(gva, "BigStunSeconds", 1.5f, "End explosion Stuns Big (Small are launched).");
            const string ra = "Wizard Astral Railcannon Ascended";
            _rcAscWindup = Config.Bind(ra, "Windup", 1f, "Ascended wind up.");
            _rcAscDuration = Config.Bind(ra, "BeamDuration", 4f, "Hold the key to keep the beam (release ends it).");
            _rcAscTick = Config.Bind(ra, "TickInterval", 0.2f, "Seconds between beam ticks (20 ticks over 4s).");
            _rcAscTickPercent = Config.Bind(ra, "TickPercent", 15f, "Each tick: % of the normal shot.");
            const string ba = "Wizard Astral Greatblade Ascended";
            _gbAscWindup = Config.Bind(ba, "Windup", 1f, "Ascended wind up (no charging).");
            _gbAscGap = Config.Bind(ba, "SlamInterval", 1f, "Seconds between the three slams.");
            _gbAscPercent = Config.Bind(ba, "SlamPercent", 70f, "Each slam: % of the uncharged slam (re-aimed at the crosshair).");
            const string na = "Wizard Frost Nova Ascended";
            _fnAscDuration = Config.Bind(na, "AuraDuration", 6f, "Frost Aura on the Wizard (12 ticks).");
            _fnAscTickPercent = Config.Bind(na, "TickPercent", 8f, "Each aura tick: % of the normal Nova.");
            _fnAscBlast = Config.Bind(na, "ExplosionPercent", 44f, "Final explosion: % of the normal Nova.");
            _fnAscFreeze = Config.Bind(na, "FreezeDuration", 2f, "Explosion Freezes Small and Big.");
            _fnAscBossSlow = Config.Bind(na, "BossSlowPercent", 15f, "Bosses are slowed instead of Frozen.");
            const string ca = "Wizard Elemental Cataclysm Ascended";
            _ccAscDelay = Config.Bind(ca, "SecondBombardmentDelay", 1.5f, "Seconds after the first.");
            _ccAscPercent = Config.Bind(ca, "SecondBombardmentPercent", 60f, "% of the first (charged) bombardment.");
        }

        private bool IsWizAscended(Player player, string id)
        {
            return DragonCombat.IsSkillAscended(player, id);
        }

        // Hold-to-charge: the tree hotbar key of the skill, or the legacy M4+key.
        private bool SkillKeyHeld(Player player, string id, KeyCode legacy)
        {
            if (DragonCombat.IsTreeHotbarActive(player))
                return DragonCombat.IsTreeSkillKeyHeld(id);
            return Input.GetKey(_modifier.Value) && Input.GetKey(legacy);
        }

        private void DealWiz(Player attacker, Character target, WizDamage d, float multiplier, string skillId, float push, bool stagger)
        {
            if (d == null) return;
            float m = Mathf.Max(0f, multiplier) * DragonCombat.GetSkillPower(attacker, skillId);
            Deal(attacker, target, d.Blunt.Value * m, d.Slash.Value * m, d.Pierce.Value * m, d.Fire.Value * m, d.Frost.Value * m, d.Lightning.Value * m, d.Poison.Value * m, d.Spirit.Value * m, push, stagger);
        }

        private static float WizTotal(WizDamage d)
        {
            return d.Blunt.Value + d.Slash.Value + d.Pierce.Value + d.Fire.Value + d.Frost.Value + d.Lightning.Value + d.Poison.Value + d.Spirit.Value;
        }

        // ------------------------------------------------------------------ Gravity Dominion
        private void CastGravityDominion(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(50f), out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Wizard.GravityDominion", _gravityCooldown.Value, _gravityEitr.Value)) return;
            float windup = ScaleWindup(player, Mathf.Max(0f, _gravityWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlayClip(player, "wiz_gravity", windup);
            StartCoroutine(GravityRoutine(player, target, windup, IsWizAscended(player, "gravity_dominion")));
        }

        private IEnumerator GravityRoutine(Player player, Vector3 center, float windup, bool ascended)
        {
            ShowMessage("Gravity Dominion");
            if (windup > 0f) yield return new WaitForSeconds(windup);
            float radius = DragonCombat.M(ascended ? _gvAscRadius.Value : _gravityRadius.Value);
            float duration = ascended ? _gvAscDuration.Value : _gravityDuration.Value;
            float tick = Mathf.Max(0.2f, _gravityTick.Value);
            float end = Time.time + Mathf.Max(0.5f, duration);
            while (Time.time < end && player != null)
            {
                List<Character> targets = GetSphereTargets(player, center, radius);
                for (int i = 0; i < targets.Count; i++)
                {
                    Character enemy = targets[i];
                    DragonCombat.ApplyExpose(enemy, _gravityExpose.Value);
                    bool small = DragonCombat.IsSmallEnemy(enemy);
                    bool boss = IsBoss(enemy);
                    if (small) PullToward(enemy, center, _gravityPull.Value);
                    else if (ascended) PullToward(enemy, center, _gravityPull.Value * (boss ? _gvAscBoss.Value : _gvAscBig.Value) / 100f);
                    if (!small && !boss) DragonCombat.ApplyCripple(enemy, _gravityCripple.Value);
                    DealWiz(player, enemy, _gravityDmg, 1f, "gravity_dominion", 2f, false);
                }
                if (_enableVfx.Value) StartCoroutine(RingVfx(center, radius, new Color(0.45f, 0.12f, 0.75f, 0.85f), Mathf.Min(0.85f, tick)));
                yield return new WaitForSeconds(tick);
            }
            if (!ascended || player == null) yield break;
            // Ascended end blast: % of the normal skill's FULL damage (every normal pulse together).
            float fullPulses = Mathf.Max(1f, _gravityDuration.Value / tick);
            List<Character> hit = GetSphereTargets(player, center, radius);
            for (int i = 0; i < hit.Count; i++)
            {
                Character enemy = hit[i];
                DealWiz(player, enemy, _gravityDmg, fullPulses * _gvAscBlast.Value / 100f, "gravity_dominion", 12f, true);
                if (DragonCombat.IsSmallEnemy(enemy))
                {
                    Rigidbody body = enemy.GetComponent<Rigidbody>();
                    if (body != null) body.AddForce(Vector3.up * 9f + (enemy.transform.position - center).normalized * 4f, ForceMode.VelocityChange);
                }
                else if (!IsBoss(enemy)) DragonCombat.Freeze(enemy, _gvAscStun.Value, 0f);
            }
            if (_enableVfx.Value) { StartCoroutine(RingVfx(center, radius, new Color(0.75f, 0.30f, 1f, 1f), 0.6f)); CreateLightningBurst(center, radius); }
        }

        // ------------------------------------------------------------------ Astral Greatblade
        private void CastAstralGreatblade(Player player)
        {
            if (!BeginSkill(player, "Wizard.AstralGreatblade", _bladeCooldown.Value, _bladeEitr.Value)) return;
            if (IsWizAscended(player, "astral_greatblade")) StartCoroutine(AscendedGreatbladeRoutine(player));
            else StartCoroutine(AstralGreatbladeRoutine(player));
        }

        private Vector3 GreatbladeDirection(Player player)
        {
            Vector3 aim = AlbedoAimUtility.GetProjectileDirection(player, player.GetEyePoint());
            aim.y = 0f; // slammed down along the crosshair's horizontal aim, not the body's facing
            return aim.sqrMagnitude < 0.01f ? FlatForward(player) : aim.normalized;
        }

        // v0.23.6: Astral Greatblade no longer charges (user): 1s wind up, one slam.
        private IEnumerator AstralGreatbladeRoutine(Player player)
        {
            ShowMessage("Astral Greatblade");
            float windup = ScaleWindup(player, Mathf.Max(0f, _bladeWindup.Value));
            DragonCombat.LockSkill(player, windup + 0.3f);
            DragonCombat.PlayClip(player, "wiz_greatblade", windup);
            if (windup > 0f) yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead()) yield break;
            GreatbladeSlam(player, 1f);
        }

        private void GreatbladeSlam(Player player, float multiplier)
        {
            Vector3 forward = GreatbladeDirection(player);
            float range = DragonCombat.M(_bladeRange.Value);
            float width = DragonCombat.M(_bladeWidth.Value);
            List<Character> targets = GetBoxTargets(player, player.transform.position + Vector3.up, forward, range, width);
            float burn = _bladeDmg.Spirit.Value * multiplier * DragonCombat.GetSkillPower(player, "astral_greatblade") * _bladeBurnPercent.Value / 100f;
            for (int i = 0; i < targets.Count; i++)
            {
                DealWiz(player, targets[i], _bladeDmg, multiplier, "astral_greatblade", 32f, true);
                StartCoroutine(BurnRoutine(player, targets[i], true, _bladeBurnSeconds.Value, burn));
                DragonCombat.Stun(targets[i], player.transform.position);
            }
            if (_enableVfx.Value) StartCoroutine(GreatbladeVfx(player.transform.position, forward, range, width));
        }

        private IEnumerator AscendedGreatbladeRoutine(Player player)
        {
            ShowMessage("Astral Greatblade");
            float windup = ScaleWindup(player, Mathf.Max(0f, _gbAscWindup.Value));
            // you cannot move while the three slams happen
            DragonCombat.LockSkill(player, windup + 2f * Mathf.Max(0.1f, _gbAscGap.Value) + 0.4f);
            DragonCombat.PlayClip(player, "wiz_greatblade", windup);
            if (windup > 0f) yield return new WaitForSeconds(windup);
            for (int slam = 0; slam < 3; slam++)
            {
                if (player == null || player.IsDead()) yield break;
                DragonCombat.LockSkill(player, Mathf.Max(0.1f, _gbAscGap.Value) + 0.3f);
                if (slam > 0)
                {
                    // v0.25.38 (user): slams 2 and 3 get the same big wind up as the first (raise, then slam),
                    // filling the gap between slams instead of a quick 0.2 s chop.
                    float gap = Mathf.Max(0.1f, _gbAscGap.Value);
                    DragonCombat.PlayClip(player, "wiz_greatblade", gap);
                    yield return new WaitForSeconds(gap);
                    if (player == null || player.IsDead()) yield break;
                }
                GreatbladeSlam(player, _gbAscPercent.Value / 100f);
            }
        }

        // ------------------------------------------------------------------ Frost Nova
        private void CastFrostNova(Player player)
        {
            if (!BeginSkill(player, "Wizard.FrostNova", _novaCooldown.Value, _novaEitr.Value)) return;
            bool ascended = IsWizAscended(player, "frost_nova");
            float windup = ScaleWindup(player, Mathf.Max(0f, _novaWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlayClip(player, "wiz_nova", windup);
            StartCoroutine(ascended ? FrostAuraRoutine(player, windup) : FrostNovaRoutine(player, windup));
        }

        private IEnumerator FrostNovaRoutine(Player player, float windup)
        {
            ShowMessage("Frost Nova");
            if (windup > 0f) yield return new WaitForSeconds(windup);
            if (player == null) yield break;
            float radius = DragonCombat.M(_novaRadius.Value);
            List<Character> targets = GetSphereTargets(player, player.transform.position, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                Character enemy = targets[i];
                DealWiz(player, enemy, _novaDmg, 1f, "frost_nova", 18f, false);
                DragonCombat.ApplyFrost(enemy, _novaFrost.Value);
                if (DragonCombat.IsSmallEnemy(enemy)) DragonCombat.Stun(enemy, player.transform.position);
                else if (!IsBoss(enemy)) { ForceStagger(enemy, player.transform.position); DragonCombat.ApplyCripple(enemy, 3f); }
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(player.transform.position, radius, new Color(0.48f, 0.90f, 1f, 0.95f), 0.8f));
        }

        // Ascended: a 10m Frost Aura on the Wizard (12 ticks), then an explosion that Freezes.
        private IEnumerator FrostAuraRoutine(Player player, float windup)
        {
            ShowMessage("Frost Nova - Frost Aura");
            if (windup > 0f) yield return new WaitForSeconds(windup);
            float radius = DragonCombat.M(_novaRadius.Value);
            int ticks = 12;
            float gap = Mathf.Max(0.1f, _fnAscDuration.Value / ticks);
            for (int t = 0; t < ticks; t++)
            {
                if (player == null || player.IsDead()) yield break;
                List<Character> targets = GetSphereTargets(player, player.transform.position, radius);
                for (int i = 0; i < targets.Count; i++)
                {
                    DealWiz(player, targets[i], _novaDmg, _fnAscTickPercent.Value / 100f, "frost_nova", 0f, false);
                    DragonCombat.ApplyFrost(targets[i], Mathf.Max(1f, gap * 2f));
                }
                if (_enableVfx.Value) StartCoroutine(RingVfx(player.transform.position, radius, new Color(0.55f, 0.92f, 1f, 0.55f), gap));
                yield return new WaitForSeconds(gap);
            }
            if (player == null || player.IsDead()) yield break;
            List<Character> hit = GetSphereTargets(player, player.transform.position, radius);
            for (int i = 0; i < hit.Count; i++)
            {
                DealWiz(player, hit[i], _novaDmg, _fnAscBlast.Value / 100f, "frost_nova", 18f, false);
                DragonCombat.Freeze(hit[i], _fnAscFreeze.Value, _fnAscBossSlow.Value / 100f);
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(player.transform.position, radius, new Color(0.70f, 0.97f, 1f, 1f), 0.8f));
        }

        // ------------------------------------------------------------------ Meteor Fall
        private void CastMeteorFall(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_meteorRange.Value), out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Wizard.MeteorFall", _meteorCooldown.Value, _meteorEitr.Value)) return; // cost paid once
            _meteorChargeShown = 0;
            float windup = ScaleWindup(player, Mathf.Max(0f, IsWizAscended(player, "meteor_fall") ? _mfAscWindup.Value : _meteorWindup.Value));
            DragonCombat.LockSkill(player, windup + 0.1f);
            DragonCombat.PlayClip(player, "wiz_meteor", Mathf.Max(0.25f, windup), true);
            StartCoroutine(MeteorRoutine(player, target, windup));
        }

        private IEnumerator MeteorRoutine(Player player, Vector3 target, float windup)
        {
            ShowMessage("Meteor Fall");
            // v0.23.3 universal rule: the charge timer starts the moment the key is pressed (the wind up
            // runs at the same time). 1 stack per full second held, max 3; released before 1s = no stack.
            float pressed = Time.time;
            float heldFor = 0f;
            bool holding = true;
            int stacks = 0;
            float nextMarker = 0f;
            while (player != null && !player.IsDead())
            {
                float elapsed = Time.time - pressed;
                // v0.23.6: at max stacks it can be held as long as you like; it lands where you aim on release.
                if (holding && SkillKeyHeld(player, "meteor_fall", _skill7.Value)) heldFor = elapsed;
                else holding = false;
                Vector3 aimNow;
                if (holding && AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_meteorRange.Value), out aimNow)) target = aimNow;
                _meteorChargeNext = stacks < 3 ? Mathf.Max(0f, Mathf.Floor(heldFor) + 1f - heldFor) : 0f;
                if (_enableVfx.Value && holding && Time.time >= nextMarker)
                {
                    nextMarker = Time.time + 0.2f;
                    StartCoroutine(RingVfx(target, DragonCombat.M(_meteorRadius.Value) * (1f + 0.1f * stacks), new Color(1f, 0.45f, 0.10f, 0.55f), 0.22f));
                }
                int now = Mathf.Min(3, Mathf.FloorToInt(heldFor));
                if (now > stacks) { stacks = now; _meteorChargeShown = stacks; ShowMessage("Meteor Fall " + stacks + "/3" + (stacks >= 3 ? " - release to drop it where you aim" : "")); if (_enableVfx.Value) StartCoroutine(RingVfx(target, DragonCombat.M(_meteorRadius.Value) * (1f + 0.1f * stacks), new Color(1f, 0.45f, 0.10f, 0.75f), 0.4f)); }
                if (elapsed >= windup && !holding) break;
                DragonCombat.LockSkill(player, 0.12f);
                yield return null;
            }
            if (player == null || player.IsDead()) yield break;
            _meteorChargeShown = 0;
            _meteorChargeNext = 0f;
            DragonCombat.ClipImpact(player);   // release: drag the sky down
            float damageMul = 1f + 0.2f * stacks;
            float radius = DragonCombat.M(_meteorRadius.Value) * (1f + 0.1f * stacks);
            yield return StartCoroutine(MeteorImpact(player, target, radius, damageMul, 2.4f * (1f + 0.15f * stacks)));
            if (player == null || !IsWizAscended(player, "meteor_fall")) yield break;
            // Ascended: 3 smaller meteors (5 at full charge) around the target, 12% of the charged main each.
            int count = stacks >= 3 ? 5 : 3;
            float small = DragonCombat.M(_mfAscRadius.Value);
            float spread = DragonCombat.M(_mfAscSpread.Value);
            float start = UnityEngine.Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float a = (start + 360f / count * i) * Mathf.Deg2Rad;
                Vector3 p = target + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spread;
                RaycastHit floor;
                if (Physics.Raycast(p + Vector3.up * 20f, Vector3.down, out floor, 40f, LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain"), QueryTriggerInteraction.Ignore))
                    p = floor.point;
                if (_enableVfx.Value) StartCoroutine(RingVfx(p, small, new Color(1f, 0.35f, 0.05f, 0.85f), 0.5f)); // landing marker
                StartCoroutine(MeteorImpact(player, p, small, damageMul * _mfAscPercent.Value / 100f, 1.1f));
                yield return new WaitForSeconds(Mathf.Max(0.05f, _mfAscGap.Value));
            }
        }

        private IEnumerator MeteorImpact(Player player, Vector3 target, float radius, float multiplier, float size)
        {
            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(target, 12f);
            GameObject meteor = _enableVfx.Value ? CreateOrb(sky, size, new Color(1f, 0.20f, 0.02f, 1f)) : null;
            float drop = 0.45f;
            float e = 0f;
            while (e < drop)
            {
                if (meteor != null) meteor.transform.position = Vector3.Lerp(sky, target, e / drop);
                e += Time.deltaTime;
                yield return null;
            }
            if (meteor != null) Destroy(meteor);
            if (player == null) yield break;
            float burn = _meteorDmg.Fire.Value * multiplier * DragonCombat.GetSkillPower(player, "meteor_fall") * _meteorBurnPercent.Value / 100f;
            List<Character> targets = GetSphereTargets(player, target, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                DealWiz(player, targets[i], _meteorDmg, multiplier, "meteor_fall", 35f, true);
                StartCoroutine(BurnRoutine(player, targets[i], false, _meteorBurnSeconds.Value, burn));
                ForceStagger(targets[i], target);
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(target, radius, new Color(1f, 0.22f, 0.02f, 1f), 0.75f));
        }

        // ------------------------------------------------------------------ Astral Railcannon
        private void CastAstralRailcannon(Player player)
        {
            if (!BeginSkill(player, "Wizard.AstralRailcannon", _railCooldown.Value, _railEitr.Value)) return;
            bool ascended = IsWizAscended(player, "astral_railcannon");
            float windup = ScaleWindup(player, Mathf.Max(0.2f, ascended ? _rcAscWindup.Value : _railWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlayClip(player, ascended ? "wiz_railcannon_hold" : "wiz_railcannon", windup, ascended);
            StartCoroutine(ascended ? AscendedRailcannonRoutine(player, windup) : AstralRailcannonRoutine(player, windup));
        }

        private IEnumerator AstralRailcannonRoutine(Player player, float windup)
        {
            ShowMessage("Astral Railcannon");
            Vector3 origin = player.GetEyePoint();
            Vector3 forward = AlbedoAimUtility.GetProjectileDirection(player, origin);
            if (_enableVfx.Value)
                for (int ring = 0; ring < 4; ring++)
                    StartCoroutine(RingVfx(origin + forward * (1.5f + ring * 1.25f), 0.75f + ring * 0.25f, new Color(0.72f, 0.20f, 1f, 0.82f), Mathf.Max(0.35f, windup)));
            if (windup > 0f) yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead()) yield break;
            origin = player.GetEyePoint();
            forward = AlbedoAimUtility.GetProjectileDirection(player, origin); // Free Aim at release
            RailShot(player, origin, forward, Mathf.Max(0f, _railShotMultiplier.Value), 0.45f);
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(-forward * _railRecoil.Value, ForceMode.VelocityChange);
        }

        private void RailShot(Player player, Vector3 origin, Vector3 forward, float multiplier, float beamLife)
        {
            float range = Mathf.Max(5f, DragonCombat.M(_railRange.Value));
            float width = Mathf.Max(0.8f, DragonCombat.M(_railWidth.Value));
            List<Character> targets = GetBoxTargets(player, origin, forward, range, width);
            MagicDamageSnapshot damage = GetMagicWeaponDamage(GetCurrentWeapon(player));
            float m = multiplier * DragonCombat.GetSkillPower(player, "astral_railcannon");
            for (int i = 0; i < targets.Count; i++)
                DealMagicWeaponDamage(player, targets[i], damage, m);
            if (_enableVfx.Value)
                CreateBeam(origin, origin + forward * range, new Color(0.86f, 0.50f, 1f, 0.98f), width * 0.55f, beamLife);
        }

        // Ascended: a steerable beam for up to 4s (20 ticks x 9% of the normal shot); releasing ends it.
        private IEnumerator AscendedRailcannonRoutine(Player player, float windup)
        {
            ShowMessage("Astral Railcannon");
            if (windup > 0f) yield return new WaitForSeconds(windup);
            float tick = Mathf.Max(0.05f, _rcAscTick.Value);
            float end = Time.time + Mathf.Max(tick, _rcAscDuration.Value);
            float shot = Mathf.Max(0f, _railShotMultiplier.Value) * _rcAscTickPercent.Value / 100f;
            bool first = true;
            while (player != null && !player.IsDead() && Time.time < end)
            {
                if (!first && !SkillKeyHeld(player, "astral_railcannon", _skill8.Value)) break;
                first = false;
                DragonCombat.LockSkill(player, tick + 0.05f);
                Vector3 origin = player.GetEyePoint();
                RailShot(player, origin, AlbedoAimUtility.GetProjectileDirection(player, origin), shot, tick + 0.02f);
                yield return new WaitForSeconds(tick);
            }
            DragonCombat.PlayClip(player, "wiz_railcannon", 0.05f);   // last kick, then rest
        }

        // ------------------------------------------------------------------ Elemental Cataclysm
        private void CastElementalCataclysm(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_cataclysmRange.Value), out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Wizard.ElementalCataclysm", _cataclysmCooldown.Value, _cataclysmEitr.Value)) return;
            StartCoroutine(CataclysmRoutine(player, target));
        }

        private IEnumerator CataclysmRoutine(Player player, Vector3 target)
        {
            ShowMessage("Elemental Cataclysm");
            float radius = DragonCombat.M(_cataclysmRadius.Value);
            float max = Mathf.Max(1f, _cataclysmCharge.Value);
            float minWindup = ScaleWindup(player, Mathf.Max(0f, _cataclysmWindup.Value));
            float charge = 0f;
            float elapsed = 0f;
            bool holding = true;
            float nextMarker = 0f;
            int ccShown = 0;
            DragonCombat.PlayClip(player, "wiz_cataclysm", Mathf.Max(0.3f, minWindup), true);
            while (player != null && !player.IsDead() && (elapsed < minWindup || holding))
            {
                DragonCombat.LockSkill(player, 0.12f);
                elapsed += Time.deltaTime;
                // charge timer from the key press; it stops the moment the key is released
                if (holding && SkillKeyHeld(player, "elemental_cataclysm", _skill9.Value)) charge = Mathf.Min(max, elapsed);
                else holding = false;
                _cataclysmCharge01 = charge / max;
                // v0.25.38 (user): on-screen stack prompt like Meteor Fall (one stack per charge second).
                int ccMax = Mathf.Max(2, Mathf.RoundToInt(_cataclysmCharge.Value));
                int ccNow = Mathf.FloorToInt(_cataclysmCharge01 * ccMax + 0.001f);
                if (holding && ccNow > ccShown)
                {
                    ccShown = ccNow;
                    ShowMessage("Elemental Cataclysm " + ccNow + "/" + ccMax + (ccNow >= ccMax ? " - release to strike where you aim" : ""));
                }
                // v0.23.6: hold as long as you like at full charge; it lands where you aim on release.
                Vector3 aimNow;
                if (holding && AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_cataclysmRange.Value), out aimNow)) target = aimNow;
                if (_enableVfx.Value && holding && Time.time >= nextMarker) { nextMarker = Time.time + 0.2f; StartCoroutine(RingVfx(target, radius, new Color(0.76f, 0.30f, 1f, 0.45f), 0.22f)); }
                if (_enableVfx.Value && ((int)(elapsed * 10f) % 4 == 0)) StartCoroutine(RingVfx(target, radius * Mathf.Clamp01(0.2f + charge / max), new Color(0.76f, 0.30f, 1f, 0.55f), 0.16f));
                yield return null;
            }
            _cataclysmCharge01 = 0f;
            if (player == null) yield break;
            DragonCombat.ClipImpact(player);
            float multiplier = 1f + (Mathf.Max(1f, _cataclysmMaxMultiplier.Value) - 1f) * Mathf.Clamp01(charge / max);
            CataclysmBlast(player, target, radius, multiplier);
            ShowMessage("ELEMENTAL CATACLYSM x" + multiplier.ToString("0.0"));
            if (!IsWizAscended(player, "elemental_cataclysm")) yield break;
            yield return new WaitForSeconds(Mathf.Max(0.1f, _ccAscDelay.Value));
            if (player == null) yield break;
            CataclysmBlast(player, target, radius, multiplier * _ccAscPercent.Value / 100f);
        }

        private void CataclysmBlast(Player player, Vector3 target, float radius, float multiplier)
        {
            List<Character> targets = GetSphereTargets(player, target, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                DealWiz(player, targets[i], _cataclysmDmg, multiplier, "elemental_cataclysm", 55f, true);
                DragonCombat.ApplyExpose(targets[i], _cataclysmExpose.Value);
            }
            if (_enableVfx.Value)
            {
                StartCoroutine(RingVfx(target, radius, new Color(1f, 0.35f, 0.05f, 1f), 1f));
                CreateLightningBurst(target, radius);
            }
        }

        // ------------------------------------------------------------------ Ascended Glacial Descent (Wizard's Ascended MC)
        private void CastAscendedGlacial(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_gdAscRange.Value), out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Sorcerer.GlacialDescent", _gdAscCooldown.Value, _gdAscEitr.Value)) return;
            float windup = ScaleWindup(player, Mathf.Max(0f, _gdAscWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlayClip(player, "sorc_glacial_asc", windup);
            StartCoroutine(AscendedGlacialRoutine(player, target, windup));
        }

        private IEnumerator AscendedGlacialRoutine(Player player, Vector3 target, float windup)
        {
            ShowMessage("Glacial Descent");
            if (windup > 0f) yield return new WaitForSeconds(windup);
            float radius = DragonCombat.M(_gdAscRadius.Value);
            float core = DragonCombat.M(_gdAscCore.Value);
            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(target, 12f);
            GameObject chunk = _enableVfx.Value ? CreateIceChunk(sky, radius * 0.6f) : null;
            float e = 0f;
            while (e < 0.45f)
            {
                if (chunk != null) chunk.transform.position = Vector3.Lerp(sky, target + Vector3.up * 1.4f, e / 0.45f);
                e += Time.deltaTime;
                yield return null;
            }
            if (chunk != null) Destroy(chunk);
            if (player == null) yield break;
            List<Character> targets = GetSphereTargets(player, target, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                Character enemy = targets[i];
                Vector3 flat = enemy.transform.position - target;
                flat.y = 0f;
                bool inCore = flat.magnitude <= core;
                DealWiz(player, enemy, _glacialAscDmg, inCore ? _gdAscCoreDamage.Value / 100f : 1f, "glacial_descent", 18f, false);
                DragonCombat.ApplyFrost(enemy, 6f);
                if (inCore) DragonCombat.Freeze(enemy, _gdAscFreeze.Value, _gdAscBossSlow.Value / 100f);
            }
            if (_enableVfx.Value)
            {
                StartCoroutine(RingVfx(target, radius, new Color(0.50f, 0.90f, 1f, 0.95f), 0.6f));
                StartCoroutine(RingVfx(target, core, new Color(0.85f, 0.98f, 1f, 1f), 0.8f));
            }
        }

        // ------------------------------------------------------------------ Clockwork (Grace)
        private void CastClockwork(Player player)
        {
            if (!BeginSkill(player, "Wizard.Clockwork", _clockCooldown.Value, 0f)) return;
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlayClip(player, "wiz_clockwork", 0.06f);   // v0.25.16 release-first
            ShowMessage("Clockwork");
            Collider[] hits = Physics.OverlapSphere(player.transform.position, DragonCombat.M(_clockRadius.Value));
            HashSet<Player> allies = new HashSet<Player>();
            allies.Add(player);
            for (int i = 0; i < hits.Length; i++)
            {
                Player ally = hits[i].GetComponentInParent<Player>();
                if (ally != null) allies.Add(ally);
            }
            foreach (Player ally in allies)
                DragonCombat.GrantClockwork(ally, _clockDuration.Value, _clockSkillDamage.Value / 100f, 1f - Mathf.Clamp01(_clockCdr.Value / 100f));
            if (_enableVfx.Value) StartCoroutine(RingVfx(player.transform.position, DragonCombat.M(_clockRadius.Value), new Color(1f, 0.82f, 0.35f, 0.95f), 0.8f));
        }

        private void CastRiftEcho(Player player)
        {
            if (!BeginSkill(player, "Spellcaster.RiftEcho", _riftEchoCooldown.Value, _riftEchoEitr.Value))
                return;
            _riftEchoUntil = Time.time + Mathf.Max(1f, _riftEchoDuration.Value);
            DragonCombat.BeginMobileCast(player, 0.30f, true);
            DragonCombat.PlayClip(player, "hw_rift_echo", 0.1f);
            ShowMessage("Rift Echo ACTIVE");
            if (_enableVfx.Value)
                StartCoroutine(RingVfx(player.transform.position + Vector3.up * 0.12f, 2.0f, new Color(0.68f, 0.20f, 1f, 0.90f), 0.45f));
        }

        private void CastAfterimageArsenal(Player player)
        {
            if (IsSpellAscended(player, "afterimage_arsenal")) { CastAscendedAfterimage(player); return; }
            if (!BeginSkill(player, "Spellcaster.AfterimageArsenal", _afterimageCooldown.Value, _afterimageEitr.Value))
                return;
            _afterimageUntil = Time.time + Mathf.Max(1f, _afterimageDuration.Value);
            ClearAfterimages();
            SpawnAfterimage(player.transform.position, player.transform.rotation);
            DragonCombat.PlayClip(player, "hw_afterimage", 0.1f);
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
            if (Time.time >= _riftEchoUntil)
                return;
            Vector3 aimPoint;
            Character target = AimTarget(player, DragonCombat.M(50f), out aimPoint);
            if (target == null)
                return;
            MagicDamageSnapshot damage = GetMagicWeaponDamage(weapon);
            Vector3 away = target.transform.position - player.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = player.transform.forward;
            away.Normalize();
            // Ascended: three rifts behind the target, each 100% of your attack.
            int rifts = IsSpellAscended(player, "rift_echo") ? 3 : 1;
            for (int r = 0; r < rifts; r++)
            {
                Vector3 dir = rifts == 1 ? away : Quaternion.AngleAxis(-35f + 35f * r, Vector3.up) * away;
                Vector3 portal = target.transform.position + dir * 2.6f + Vector3.up * (1.1f + 0.4f * r);
                if (_enableVfx.Value)
                    StartCoroutine(RingVfx(portal, 0.85f, new Color(0.80f, 0.22f, 1f, 0.95f), 0.30f));
                StartCoroutine(RiftEchoProjectile(player, target, damage, portal, aimPoint));
            }
        }

        // v0.23.4: what the crosshair is really on. A creature's hitbox counts (the old cone search
        // aimed at the ground behind big enemies like Trolls); otherwise the nearest enemy in a cone.
        private Character AimTarget(Player player, float range, out Vector3 point)
        {
            point = player == null ? Vector3.zero : player.GetEyePoint();
            if (player == null) return null;
            Vector3 origin = GameCamera.instance != null ? GameCamera.instance.transform.position : player.GetEyePoint();
            Vector3 dir = GameCamera.instance != null ? GameCamera.instance.transform.forward : player.GetLookDir();
            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.35f, dir, range + 6f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (c == null || IsPlayerCollider(player, c)) continue;
                Character ch = c.GetComponentInParent<Character>();
                if (ch != null && !IsEnemy(player, ch)) continue;
                point = hits[i].point.sqrMagnitude > 0.01f ? hits[i].point : origin + dir * hits[i].distance;
                if (ch != null) return ch;
                break; // terrain / object first: no creature under the crosshair
            }
            Character near = FindAimedEnemy(player, range, 8f);
            if (near != null) point = near.transform.position + Vector3.up * 1.0f;
            else if (hits.Length == 0) point = origin + dir * range;
            return near;
        }

        private IEnumerator RiftEchoProjectile(Player player, Character target, MagicDamageSnapshot damage, Vector3 origin, Vector3 aim)
        {
            Vector3 to = aim - origin;
            float distance = Mathf.Max(0.1f, to.magnitude);
            Vector3 dir = to / distance;
            GameObject orb = _enableVfx.Value ? CreateOrb(origin, 0.18f, new Color(0.82f, 0.30f, 1f, 1f)) : null;
            float traveled = 0f;
            float speed = 38f * DragonCombat.UnitsPerMeter();

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

            Vector3 aimPoint;
            Character target = AimTarget(player, DragonCombat.M(50f), out aimPoint);
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
            UpdateVoidCharges(player);
            if (_voidCharges <= 0) { ShowCooldown("Spellcaster.VoidStep"); return; }
            Vector3 dest;
            bool airborne = false;
            if (AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_voidGroundRange.Value), out dest))
                dest += Vector3.up * 0.15f;
            else
            {
                Vector3 origin = player.GetEyePoint();
                Vector3 dir = AlbedoAimUtility.GetProjectileDirection(player, origin);
                dest = player.transform.position + dir * Mathf.Max(1f, DragonCombat.M(_voidFreeRange.Value));
                airborne = true;
            }
            int max = VoidMaxCharges(player);
            if (_voidCharges >= max)
                _voidNextCharge = Time.time + Mathf.Max(0.1f, _testingForceCooldowns.Value ? _testingCooldown.Value : DragonCombat.ScaleCooldown(player, "Spellcaster.VoidStep", _voidCooldown.Value));
            _voidCharges--;
            _cooldowns["Spellcaster.VoidStep"] = _voidCharges > 0 ? 0f : _voidNextCharge;
            if (Time.time < _afterimageUntil)
                SpawnAfterimage(player.transform.position, player.transform.rotation);
            Rigidbody body = player.GetComponent<Rigidbody>();
            Vector3 momentum = body != null ? body.velocity : Vector3.zero;
            DragonCombat.PlayAccent(player, "hw_voidstep", 0.05f);   // v0.25.18 Ghost Step never replaces the running pose
            TeleportPlayer(player, dest);
            if (body != null && IsSpellAscended(player, "void_step"))
                body.velocity = momentum; // Ascended keeps momentum
            // Always clear fall state. A lower Ground PAC destination used to
            // inherit the pre-teleport altitude and deal fall damage on landing.
            ApplyFeatherFall(player);
            TriggerPhaseFlow(player);
            if (_enableVfx.Value) StartCoroutine(RingVfx(dest, 1.6f, new Color(0.58f, 0.12f, 1f, 0.95f), 0.35f));
            ShowMessage((airborne ? "Void Step - Free Aim / Feather Falling" : "Void Step") + (max > 1 ? " (" + _voidCharges + "/" + max + ")" : ""));
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
                float radius = _wizardChargeStacks >= 1 ? DragonCombat.M(_wizardChargeBaseRadius.Value) * 2f : DragonCombat.M(_wizardChargeBaseRadius.Value);
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

            float radius = Mathf.Max(0.5f, DragonCombat.M(_wizardChargeBaseRadius.Value)) * 2f;
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
            float range = Mathf.Max(1f, DragonCombat.M(_wizardChargeRange.Value));
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
            ItemDrop.ItemData weapon = GetCurrentWeapon(player);
            // Rift Echo, Afterimages and Astral Clones pulse on a fixed cadence while Mouse1 is held,
            // regardless of what the Spellcaster has equipped (Ascended Rift Echo: 0.35s).
            // v0.23.4: Rift Echo fires every 0.3s (normal and Ascended), afterimages every 0.5s.
            if (Time.time < _riftEchoUntil && Time.time >= _nextSpellcasterHeldFire)
            {
                _nextSpellcasterHeldFire = Time.time + Mathf.Max(0.05f, _reAscCadence.Value);
                FireRiftEcho(player, weapon);
            }
            if (Time.time < _afterimageUntil && _spellcasterAfterimages.Count > 0 && Time.time >= _nextAfterimageFire)
            {
                _nextAfterimageFire = Time.time + 0.5f;
                FireAfterimages(player, weapon);
            }
            // Arcane Phalanx: each sword has a hard launch buffer that releasing Mouse1 never resets.
            if (_phalanxSwords.Count > 0 && !_phalanxVolleyArmed && Time.time >= _nextPhalanxLaunch)
            {
                _nextPhalanxLaunch = Time.time + Mathf.Max(0.05f, _phalanxBuffer.Value);
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

        // =====================================================================================
        // v0.23.2 SPELLCASTER REWORK (Framework 3.2 + approved Ascensions). Spellcaster skills
        // have no wind up (Yin and Yang); damage sections "Spellcaster <Skill> Damage".
        // =====================================================================================
        private WizDamage _phalanxDmg, _ruptureDmg, _gravityBlastDmg, _stoneAscDmg;
        private ConfigEntry<int> _phalanxCountV;
        private ConfigEntry<float> _phalanxBuffer, _phalanxRange, _phalanxLifetime;
        private ConfigEntry<float> _gbCooldown, _gbEitr, _gbRange, _gbTravel, _gbRadius, _gbTick, _gbPull, _gbCripple, _gbOrbSize;
        private ConfigEntry<float> _riftWalkerCooldown, _riftWalkerRange, _riftWalkerWindow, _riftWalkerLife;
        private ConfigEntry<float> _yyAttackSpeed;
        private ConfigEntry<float> _saAscRadius, _saAscEruption, _saAscTickPercent, _saAscDuration, _saAscCooldown, _saAscEitr, _saAscRange;
        private ConfigEntry<int> _paAscCount;
        private ConfigEntry<float> _paAscPercent, _paAscSpearRadius, _paAscSpearPercent;
        private ConfigEntry<int> _vsAscCharges;
        private ConfigEntry<float> _reAscCadence;
        private ConfigEntry<float> _gbAscRange, _gbAscBurst, _gbAscBig, _gbAscBoss;
        private ConfigEntry<int> _ruAscCharges;
        private ConfigEntry<float> _ruAscDamage;
        private float _nextPhalanxLaunch;
        private int _meteorChargeShown;
        private float _cataclysmCharge01;
        private float _meteorChargeNext;
        private float _nextAfterimageFire;
        private float _phalanxExpireAt;
        private bool _phalanxSpearArmed;
        private int _voidCharges = -1;
        private float _voidNextCharge;
        private readonly GameObject[] _clones = new GameObject[3];

        private void BindSpellcasterV0232()
        {
            const string p = "Spellcaster Arcane Phalanx";
            _phalanxCountV = Config.Bind(p, "SwordCount_v0232", 4, "Hovering swords (Ascended: 8).");
            _phalanxBuffer = Config.Bind(p, "LaunchBuffer", 0.3f, "Hard minimum seconds between two sword launches while Mouse1 is held.");
            _phalanxRange = Config.Bind(p, "Range", 50f, "Sword flight range (Laser Projectile, Free Aim).");
            _phalanxLifetime = Config.Bind(p, "Duration", 16f, "Unlaunched swords vanish after this (normal and Ascended).");
            _phalanxDmg = BindWizDamage("Spellcaster Arcane Phalanx Damage", 0f, 34f, 34f, 0f, 0f, 0f, 0f, 0f);
            const string r = "Spellcaster Arcane Rupture";
            _ruptureDmg = BindWizDamage("Spellcaster Arcane Rupture Damage", 0f, 0f, 0f, 0f, 0f, 55f, 0f, 45f);
            const string g = "Spellcaster Gravity Blast";
            _gbCooldown = Config.Bind(g, "Cooldown", 14f, "Seconds.");
            _gbEitr = Config.Bind(g, "EitrCost", 40f, "Eitr cost (Spellcaster pays half).");
            _gbRange = Config.Bind(g, "Range", 15f, "Travel distance (Laser Projectile, Free Aim).");
            _gbTravel = Config.Bind(g, "TravelTime", 4f, "Seconds to travel the range.");
            _gbRadius = Config.Bind(g, "Radius", 5f, "Damage / pull radius around the orb.");
            _gbTick = Config.Bind(g, "HitInterval", 0.5f, "Persistent damage interval.");
            _gbPull = Config.Bind(g, "PullStrength", 2.5f, "Slow pull of Small enemies toward the orb per tick.");
            _gbCripple = Config.Bind(g, "CrippleDuration", 3f, "Big and Boss: Cripple, refreshed on every hit.");
            _gbOrbSize = Config.Bind(g, "OrbSize", 2.5f, "Orb diameter (about a Greydwarf Brute).");
            _gravityBlastDmg = BindWizDamage("Spellcaster Gravity Blast Damage", 0f, 16f, 16f, 0f, 0f, 0f, 0f, 0f);
            const string w = "Spellcaster Rift Walker";
            _riftWalkerCooldown = Config.Bind(w, "Cooldown", 120f, "Seconds, starting when Portal A is placed (a failed B still uses it).");
            _riftWalkerRange = Config.Bind(w, "PlacementRange", 50f, "Max distance for each portal (physical aim; Free Aim mid-air otherwise).");
            _riftWalkerWindow = Config.Bind(w, "PlacementWindow", 30f, "Seconds to place Portal B after A.");
            _riftWalkerLife = Config.Bind(w, "LinkedLifetime", 30f, "Seconds both portals work once B is placed.");
            _yyAttackSpeed = Config.Bind("Spellcaster Gun Staff", "AttackSpeedMultiplier_v0232", 2f, "Yin and Yang: Staff / Wand attack interval -50% (2x speed), single or dual.");
            const string sa = "Spellcaster Stonefang Eruption Ascended";
            _saAscRadius = Config.Bind(sa, "Radius", 7f, "Ascended radius.");
            _saAscEruption = Config.Bind(sa, "EruptionPercent", 80f, "Eruption damage %.");
            _saAscTickPercent = Config.Bind(sa, "SpikeTickPercent", 20f, "Spikes: % every 0.5s.");
            _saAscDuration = Config.Bind(sa, "SpikeDuration", 4f, "Spikes stay this long (8 ticks).");
            _saAscCooldown = Config.Bind(sa, "Cooldown", 9f, "Seconds.");
            _saAscEitr = Config.Bind(sa, "EitrCost", 24f, "Eitr cost.");
            _saAscRange = Config.Bind(sa, "GroundPACRange", 40f, "Ground PAC range.");
            _stoneAscDmg = BindWizDamage("Spellcaster Stonefang Eruption Ascended Damage", 32f, 0f, 30f, 0f, 0f, 0f, 0f, 0f);
            const string pa = "Spellcaster Arcane Phalanx Ascended";
            _paAscCount = Config.Bind(pa, "SwordCount", 8, "Ascended swords.");
            _paAscPercent = Config.Bind(pa, "SwordDamagePercent", 65f, "Each Ascended sword.");
            _paAscSpearRadius = Config.Bind(pa, "AstralSpearRadius", 4f, "Full 8-sword volley: eruption on the first impact.");
            _paAscSpearPercent = Config.Bind(pa, "AstralSpearPercent", 80f, "Eruption: % of one sword.");
            _vsAscCharges = Config.Bind("Spellcaster Void Step Ascended", "Charges", 2, "Charges, recovered one after the other; keeps momentum.");
            _reAscCadence = Config.Bind("Spellcaster Rift Echo", "EchoInterval_v0234", 0.3f, "Seconds between echoes while Mouse1 is held (normal and Ascended; Ascended opens 3 rifts).");
            const string ga = "Spellcaster Gravity Blast Ascended";
            _gbAscRange = Config.Bind(ga, "Range", 25f, "Ascended travel distance (same speed).");
            _gbAscBurst = Config.Bind(ga, "EndBurstPercent", 130f, "Burst when the orb ends (Ascended) or is detonated by the 2nd recast (normal too): % of one hit.");
            _gbAscBig = Config.Bind(ga, "BigPullPercent", 40f, "Big enemies are pulled at this strength.");
            _gbAscBoss = Config.Bind(ga, "BossPullPercent", 20f, "Bosses are pulled at this strength (they can still act).");
            _ruAscCharges = Config.Bind("Spellcaster Arcane Rupture Ascended", "MaxCharges", 4, "Ascended charges.");
            _ruAscDamage = Config.Bind("Spellcaster Arcane Rupture Ascended", "DamagePercent", 120f, "Ascended explosion damage %.");
        }

        private bool IsSpellAscended(Player player, string id)
        {
            return DragonCombat.IsSkillAscended(player, id);
        }

        // ------------------------------------------------------------------ Arcane Phalanx
        private float _phalanxSwordPercent = 1f;

        private void CastArcanePhalanx(Player player)
        {
            if (_phalanxSwords.Count > 0)
            {
                _phalanxVolleyArmed = true;
                DragonCombat.PlayClip(player, "hw_command", 0.1f);
                ShowMessage("Phalanx Volley READY - aim and Left Click");
                return;
            }
            if (CooldownRemaining("Spellcaster.ArcanePhalanx") > 0f) { ShowCooldown("Spellcaster.ArcanePhalanx"); return; }
            if (!SpendEitr(player, _phalanxEitr.Value)) { ShowMessage("Not enough Eitr"); return; }
            bool ascended = IsSpellAscended(player, "arcane_phalanx");
            int count = Mathf.Max(1, ascended ? _paAscCount.Value : _phalanxCountV.Value);
            _phalanxSwordPercent = ascended ? _paAscPercent.Value / 100f : 1f;
            _phalanxSpearArmed = false;
            DragonCombat.BeginMobileCast(player, 0.3f, false);
            for (int i = 0; i < count; i++) _phalanxSwords.Add(CreateArcaneSword(player.transform.position));
            _nextPhalanxLaunch = Time.time + 0.15f;
            _phalanxExpireAt = Time.time + Mathf.Max(1f, _phalanxLifetime.Value); // swords stay 16s
            DragonCombat.PlayClip(player, "hw_rift_walker", 0.12f);   // both hands open: the swords appear
            ShowMessage("Arcane Phalanx x" + count);
            CloneCopySkill(player, "arcane_phalanx");
        }

        private void LaunchAllPhalanx(Player player)
        {
            List<GameObject> copy = new List<GameObject>(_phalanxSwords);
            _phalanxSwords.Clear();
            // Ascended: all 8 launched together arm one Astral Spear eruption on the first impact.
            _phalanxSpearArmed = IsSpellAscended(player, "arcane_phalanx") && copy.Count >= Mathf.Max(1, _paAscCount.Value);
            for (int i = 0; i < copy.Count; i++) LaunchSword(player, copy[i]);
            DragonCombat.PlayClip(player, "hw_point", 0.06f);   // v0.25.18 Rift Conductor: dispatch point
            ShowMessage("PHALANX VOLLEY x" + copy.Count);
            StartCooldown("Spellcaster.ArcanePhalanx", _phalanxCooldown.Value);
        }

        private void LaunchSword(Player player, GameObject sword)
        {
            if (sword == null) return;
            Vector3 origin = sword.transform.position;
            Vector3 dir = AlbedoAimUtility.GetProjectileDirection(player, origin);
            StartCoroutine(SwordProjectile(player, sword, origin, dir, _phalanxSwordPercent));
            if (_phalanxSwords.Count == 0 && !_phalanxVolleyArmed && CooldownRemaining("Spellcaster.ArcanePhalanx") <= 0f)
                StartCooldown("Spellcaster.ArcanePhalanx", _phalanxCooldown.Value); // cooldown starts once every sword is gone
        }

        private IEnumerator SwordProjectile(Player player, GameObject sword, Vector3 origin, Vector3 dir, float percent)
        {
            dir.Normalize();
            float traveled = 0f;
            float range = DragonCombat.M(Mathf.Max(1f, _phalanxRange.Value));
            Vector3 pos = origin;
            while (traveled < range)
            {
                float step = Mathf.Min(45f * DragonCombat.UnitsPerMeter() * Time.deltaTime, range - traveled);
                RaycastHit[] hits = Physics.RaycastAll(pos, dir, step + 0.25f);
                Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
                bool stop = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null || hits[i].collider.isTrigger || IsPlayerCollider(player, hits[i].collider)) continue;
                    Character target = hits[i].collider.GetComponentInParent<Character>();
                    if (target != null && !IsEnemy(player, target)) continue;
                    if (target != null) DealWiz(player, target, _phalanxDmg, percent, "arcane_phalanx", 8f, false);
                    pos = hits[i].point;
                    stop = true;
                    break;
                }
                if (!stop) pos += dir * step;
                if (sword != null)
                {
                    sword.transform.position = pos;
                    sword.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                }
                traveled += step;
                if (stop)
                {
                    if (_phalanxSpearArmed)
                    {
                        _phalanxSpearArmed = false;
                        AstralSpearEruption(player, pos, percent);
                    }
                    break;
                }
                yield return null;
            }
            if (sword != null) Destroy(sword);
        }

        private void AstralSpearEruption(Player player, Vector3 point, float swordPercent)
        {
            float radius = DragonCombat.M(_paAscSpearRadius.Value);
            List<Character> targets = GetSphereTargets(player, point, radius);
            for (int i = 0; i < targets.Count; i++)
                DealWiz(player, targets[i], _phalanxDmg, swordPercent * _paAscSpearPercent.Value / 100f, "arcane_phalanx", 14f, true);
            if (!_enableVfx.Value) return;
            StartCoroutine(RingVfx(point, radius, new Color(0.72f, 0.40f, 1f, 1f), 0.5f));
            for (int i = 0; i < 4; i++) // four cosmetic spears
            {
                float a = i * Mathf.PI * 0.5f + 0.4f;
                Vector3 p = point + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * 0.45f;
                CreateBeam(p - Vector3.up * 0.5f, p + Vector3.up * 4.5f, new Color(0.80f, 0.55f, 1f, 1f), 0.35f, 0.6f);
            }
        }

        // ------------------------------------------------------------------ Gravity Blast (new)
        // v0.23.6: recast while the orb is out: 1st recast stops it where it is, 2nd makes it explode now.
        private bool _gbOrbActive;
        private int _gbRecasts;

        private void CastGravityBlast(Player player)
        {
            if (_gbOrbActive)
            {
                _gbRecasts++;
                DragonCombat.PlayClip(player, _gbRecasts == 1 ? "hw_stop" : "hw_pinch", 0.06f);   // v0.25.18 open stop / pinch detonate
                ShowMessage(_gbRecasts == 1 ? "Gravity Blast - halted" : "Gravity Blast - detonate");
                return;
            }
            if (!BeginSkill(player, "Spellcaster.GravityBlast", _gbCooldown.Value, _gbEitr.Value)) return;
            _gbOrbActive = true;
            _gbRecasts = 0;
            DragonCombat.BeginMobileCast(player, 0.3f, false);
            DragonCombat.PlayClip(player, "hw_gravity_blast", 0.12f);
            Vector3 origin = player.GetEyePoint() + player.transform.forward * 1.2f;
            Vector3 dir = AlbedoAimUtility.GetProjectileDirection(player, origin);
            StartCoroutine(GravityBlastRoutine(player, origin, dir, IsSpellAscended(player, "gravity_blast"), 1f));
            CloneCopySkill(player, "gravity_blast");
        }

        private IEnumerator GravityBlastRoutine(Player player, Vector3 origin, Vector3 dir, bool ascended, float scale)
        {
            ShowMessage("Gravity Blast");
            dir.Normalize();
            float normalRange = DragonCombat.M(Mathf.Max(1f, _gbRange.Value));
            float range = ascended ? DragonCombat.M(Mathf.Max(1f, _gbAscRange.Value)) : normalRange;
            float speed = normalRange / Mathf.Max(0.5f, _gbTravel.Value);
            float life = range / Mathf.Max(0.1f, speed);
            float radius = DragonCombat.M(_gbRadius.Value);
            float tick = Mathf.Max(0.1f, _gbTick.Value);
            int solid = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            GameObject orb = _enableVfx.Value ? CreateOrb(origin, DragonCombat.M(_gbOrbSize.Value), new Color(0.18f, 0.02f, 0.30f, 0.92f)) : null;
            Vector3 pos = origin;
            bool moving = true;
            float elapsed = 0f;
            float nextTick = 0f;
            bool detonated = false;
            while (elapsed < life && player != null)
            {
                if (scale >= 1f && _gbRecasts >= 1) moving = false;
                if (scale >= 1f && _gbRecasts >= 2) { detonated = true; break; }
                if (moving)
                {
                    float step = speed * Time.deltaTime;
                    RaycastHit wall;
                    // passes through every enemy, stops at walls and other physical objects
                    if (Physics.SphereCast(pos, 0.4f, dir, out wall, step, solid, QueryTriggerInteraction.Ignore) && wall.collider.GetComponentInParent<Character>() == null)
                    {
                        pos += dir * Mathf.Max(0f, wall.distance - 0.1f);
                        moving = false;
                    }
                    else pos += dir * step;
                    if (orb != null) orb.transform.position = pos;
                }
                if (elapsed >= nextTick)
                {
                    nextTick = elapsed + tick;
                    List<Character> targets = GetSphereTargets(player, pos, radius);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        Character enemy = targets[i];
                        DealWiz(player, enemy, _gravityBlastDmg, scale, "gravity_blast", 0f, false);
                        bool small = DragonCombat.IsSmallEnemy(enemy);
                        bool boss = IsBoss(enemy);
                        if (small) PullToward(enemy, pos, _gbPull.Value);
                        else
                        {
                            DragonCombat.ApplyCripple(enemy, _gbCripple.Value);
                            if (ascended) PullToward(enemy, pos, _gbPull.Value * (boss ? _gbAscBoss.Value : _gbAscBig.Value) / 100f);
                        }
                    }
                    if (_enableVfx.Value) StartCoroutine(RingVfx(pos, radius, new Color(0.40f, 0.10f, 0.70f, 0.55f), Mathf.Min(0.4f, tick)));
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (orb != null) Destroy(orb);
            if (scale >= 1f) _gbOrbActive = false;
            // v0.23.7: the orb always detonates when it ends (normal and Ascended) or on the 2nd recast.
            if (player == null) yield break;
            List<Character> hit = GetSphereTargets(player, pos, radius);
            for (int i = 0; i < hit.Count; i++)
                DealWiz(player, hit[i], _gravityBlastDmg, scale * _gbAscBurst.Value / 100f, "gravity_blast", 14f, true);
            if (_enableVfx.Value) StartCoroutine(ExplosionVfx(pos, radius, new Color(0.55f, 0.18f, 0.95f, 0.85f)));
        }

        // A blast at the orb itself (no strikes from the sky): a flash sphere that swells and fades,
        // with shock rings racing out to the radius.
        private IEnumerator ExplosionVfx(Vector3 center, float radius, Color color)
        {
            GameObject flash = CreateOrb(center, radius * 0.3f, color);
            for (int i = 0; i < 3; i++)
                StartCoroutine(RingVfx(center + Vector3.up * (0.3f + i * 0.9f), radius * (0.7f + 0.15f * i), new Color(color.r, color.g, color.b, 0.9f - 0.2f * i), 0.35f + 0.1f * i));
            float t = 0f;
            Renderer r = flash != null ? flash.GetComponent<Renderer>() : null;
            while (t < 0.4f && flash != null)
            {
                float k = t / 0.4f;
                flash.transform.localScale = Vector3.one * Mathf.Lerp(radius * 0.3f, radius * 2f, k);
                if (r != null && r.material != null) r.material.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
                t += Time.deltaTime;
                yield return null;
            }
            if (flash != null) Destroy(flash);
        }

        // ------------------------------------------------------------------ Rift Walker (Grace)
        private bool PlacePortalPoint(Player player, out Vector3 point, out bool airborne)
        {
            float range = DragonCombat.M(_riftWalkerRange.Value);
            airborne = false;
            if (AlbedoAimUtility.TryGetPhysicalTarget(player, range, out point)) { point += Vector3.up * 0.15f; return true; }
            Vector3 origin = player.GetEyePoint();
            Vector3 dir = AlbedoAimUtility.GetProjectileDirection(player, origin);
            point = origin + dir * Mathf.Min(range, DragonCombat.M(15f)); // Free Aim / mid-air placement
            airborne = true;
            return true;
        }

        private void ActivateRiftWalker(Player player)
        {
            if (!_riftAwaitingB || _riftA == null)
            {
                if (CooldownRemaining("Spellcaster.RiftWalker") > 0f) { ShowCooldown("Spellcaster.RiftWalker"); return; }
                Vector3 a; bool airA;
                PlacePortalPoint(player, out a, out airA);
                DestroyRifts();
                _riftA = CreateRift(a);
                _riftAwaitingB = true;
                DragonCombat.PlayClip(player, "hw_rift_walker", 0.12f);
                _riftEndTime = Time.time + Mathf.Max(1f, _riftWalkerWindow.Value);
                StartCooldown("Spellcaster.RiftWalker", _riftWalkerCooldown.Value); // from Portal A placement
                ShowMessage("Portal A placed - aim and press the Grace key again for Portal B");
                return;
            }
            Vector3 b; bool airB;
            if (!PlacePortalPoint(player, out b, out airB)) { ShowMessage("Portal B could not be placed"); return; }
            _riftB = CreateRift(b);
            DragonCombat.PlayClip(player, "hw_rift_walker", 0.12f);
            LinkRifts();
            _riftAwaitingB = false;
            _riftEndTime = Time.time + Mathf.Max(1f, _riftWalkerLife.Value);
            ShowMessage("Rift Walker linked - press E at a portal to travel");
        }

        // ------------------------------------------------------------------ Ascended Stonefang (Spellcaster's Ascended MC)
        private void CastAscendedStonefang(Player player)
        {
            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_saAscRange.Value), out target)) { ShowMessage("Aim at a physical target"); return; }
            if (!BeginSkill(player, "Sorcerer.StonefangEruption", _saAscCooldown.Value, _saAscEitr.Value)) return;
            DragonCombat.BeginMobileCast(player, 0.3f, false); // Spellcaster: no wind up
            DragonCombat.PlayClip(player, "sorc_stonefang_asc", 0.12f);
            StartCoroutine(AscendedStonefangRoutine(player, target));
        }

        private IEnumerator AscendedStonefangRoutine(Player player, Vector3 target)
        {
            ShowMessage("Stonefang Eruption");
            float radius = DragonCombat.M(_saAscRadius.Value);
            List<Character> targets = GetSphereTargets(player, target, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                Character enemy = targets[i];
                DealWiz(player, enemy, _stoneAscDmg, _saAscEruption.Value / 100f, "stonefang_eruption", 20f, false);
                if (DragonCombat.IsSmallEnemy(enemy)) DragonCombat.Stun(enemy, target);
                if (!IsBoss(enemy)) DragonCombat.ApplyCripple(enemy, 6f);
            }
            if (_enableVfx.Value) CreateStoneSpikes(target, radius);
            int ticks = Mathf.Max(1, Mathf.RoundToInt(_saAscDuration.Value / 0.5f));
            for (int t = 0; t < ticks; t++)
            {
                yield return new WaitForSeconds(0.5f);
                if (player == null) yield break;
                List<Character> inside = GetSphereTargets(player, target, radius);
                for (int i = 0; i < inside.Count; i++)
                    DealWiz(player, inside[i], _stoneAscDmg, _saAscTickPercent.Value / 100f, "stonefang_eruption", 0f, false);
                if (_enableVfx.Value && t % 2 == 1) CreateStoneSpikes(target, radius);
            }
        }

        // ------------------------------------------------------------------ Void Step charges (Ascended)
        private int VoidMaxCharges(Player player)
        {
            return IsSpellAscended(player, "void_step") ? Mathf.Max(1, _vsAscCharges.Value) : 1;
        }

        private void UpdateVoidCharges(Player player)
        {
            int max = VoidMaxCharges(player);
            if (_voidCharges < 0 || _voidCharges > max) _voidCharges = max;
            if (_voidCharges < max && Time.time >= _voidNextCharge)
            {
                _voidCharges++;
                if (_voidCharges < max) _voidNextCharge = Time.time + Mathf.Max(0.1f, _testingForceCooldowns.Value ? _testingCooldown.Value : DragonCombat.ScaleCooldown(player, "Spellcaster.VoidStep", _voidCooldown.Value));
            }
            _cooldowns["Spellcaster.VoidStep"] = _voidCharges > 0 ? 0f : _voidNextCharge;
        }

        // ------------------------------------------------------------------ Ascended Afterimage Arsenal (v0.23.4 mimics)
        // Three clones replay your movement 0.2s late (they follow teleports and Rifts too) and copy
        // every weapon attack 0.2s later for 100% of your weapon damage. They never cast skills.
        private readonly List<float> _mimicTimes = new List<float>();
        private readonly List<float> _mimicAttackAt = new List<float>();
        private readonly List<ItemDrop.ItemData> _mimicAttackWeapon = new List<ItemDrop.ItemData>();
        private float _mimicUntil;
        private static readonly Vector3[] MimicOffsets = { new Vector3(-1.4f, 0f, -0.8f), new Vector3(1.4f, 0f, -0.8f), new Vector3(0f, 0f, -1.7f) };

        private int ActiveCloneCount()
        {
            if (Time.time >= _mimicUntil) return 0;
            int n = 0;
            for (int i = 0; i < 3; i++) if (_clones[i] != null) n++;
            return n;
        }

        private void CastAscendedAfterimage(Player player)
        {
            if (!BeginSkill(player, "Spellcaster.AfterimageArsenal", _afterimageCooldown.Value, _afterimageEitr.Value)) return;
            ClearClones();
            _mimicUntil = Time.time + Mathf.Max(1f, _afterimageDuration.Value);
            BuildAstralMimics(player);
            DragonCombat.PlayClip(player, "hw_afterimage", 0.1f);
            ShowMessage("Afterimage Arsenal - 3 Astral twins");
        }

        // v0.23.6 Astral twins: a purple ghost copy of YOUR model (armour, shield, weapons, both Gun
        // Staves...) whose every bone replays your pose 0.2s late, so they run, roll and attack
        // exactly like you. Rebuilt when your equipment changes.
        private Transform _mimicSource;
        private Transform[] _mimicSourceBones;
        private readonly Transform[][] _mimicBones = new Transform[3][];
        private readonly List<Vector3[]> _poseLocalPos = new List<Vector3[]>();
        private readonly List<Quaternion[]> _poseLocalRot = new List<Quaternion[]>();
        private readonly List<Vector3> _poseRootPos = new List<Vector3>();
        private readonly List<Quaternion> _poseRootRot = new List<Quaternion>();
        private float _mimicNextEquipCheck;
        private Material _mimicMaterial;

        private Transform FindPlayerVisual(Player player)
        {
            Transform visual = player.transform.Find("Visual");
            if (visual != null) return visual;
            Animator anim = player.GetComponentInChildren<Animator>();
            return anim != null ? anim.transform : null;
        }

        private void BuildAstralMimics(Player player)
        {
            for (int i = 0; i < 3; i++) { if (_clones[i] != null) Destroy(_clones[i]); _clones[i] = null; _mimicBones[i] = null; }
            _poseLocalPos.Clear(); _poseLocalRot.Clear(); _poseRootPos.Clear(); _poseRootRot.Clear();
            _mimicSource = FindPlayerVisual(player);
            if (_mimicSource == null) return;
            _mimicSourceBones = _mimicSource.GetComponentsInChildren<Transform>(true);
            if (_mimicMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                _mimicMaterial = shader != null ? new Material(shader) : null;
                if (_mimicMaterial != null) _mimicMaterial.color = new Color(0.58f, 0.30f, 1f, 0.42f);
            }
            for (int i = 0; i < 3; i++)
            {
                GameObject copy = null;
                bool wasActive = _mimicSource.gameObject.activeSelf;
                try
                {
                    // Copy while inactive so none of the player's scripts wake up on the twin.
                    _mimicSource.gameObject.SetActive(false);
                    copy = Instantiate(_mimicSource.gameObject);
                }
                finally
                {
                    _mimicSource.gameObject.SetActive(wasActive);
                }
                if (copy == null) continue;
                copy.name = "HorizonWalkerAstralTwin";
                StripToVisual(copy);
                copy.SetActive(true);
                _clones[i] = copy;
                _mimicBones[i] = copy.GetComponentsInChildren<Transform>(true);
            }
            _mimicNextEquipCheck = Time.time + 0.5f;
        }

        // Keep only transforms + renderers; every renderer becomes the astral ghost material.
        private void StripToVisual(GameObject copy)
        {
            Component[] all = copy.GetComponentsInChildren<Component>(true);
            for (int pass = 0; pass < 2; pass++)
                for (int i = all.Length - 1; i >= 0; i--)
                {
                    Component c = all[i];
                    if (c == null || c is Transform || c is Renderer || c is MeshFilter) continue;
                    try { DestroyImmediate(c); } catch { }
                }
            Renderer[] renderers = copy.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (renderers[i] is ParticleSystemRenderer || _mimicMaterial == null) { renderers[i].enabled = renderers[i] is ParticleSystemRenderer ? false : renderers[i].enabled; continue; }
                Material[] mats = new Material[Mathf.Max(1, renderers[i].sharedMaterials.Length)];
                for (int m = 0; m < mats.Length; m++) mats[m] = _mimicMaterial;
                renderers[i].sharedMaterials = mats;
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public void QueueMimicAttack(Player player, ItemDrop.ItemData weapon)
        {
            if (player == null || ActiveCloneCount() == 0) return;
            _mimicAttackAt.Add(Time.time + 0.2f);
            _mimicAttackWeapon.Add(weapon);
        }

        private void UpdateCloneHold(Player player)
        {
            if (ActiveCloneCount() == 0)
            {
                if (Time.time >= _mimicUntil && _clones[0] != null) ClearClones();
                return;
            }
            if (_mimicSource == null || _mimicSourceBones == null) { BuildAstralMimics(player); if (_mimicSource == null) return; }
            // equipment changed (new attachments / removed ones) -> rebuild the twins
            if (Time.time >= _mimicNextEquipCheck)
            {
                _mimicNextEquipCheck = Time.time + 0.5f;
                if (_mimicSource.GetComponentsInChildren<Transform>(true).Length != _mimicSourceBones.Length) { BuildAstralMimics(player); return; }
            }
            int n = _mimicSourceBones.Length;
            Vector3[] lp = new Vector3[n];
            Quaternion[] lr = new Quaternion[n];
            for (int k = 0; k < n; k++)
            {
                Transform t = _mimicSourceBones[k];
                if (t == null) { BuildAstralMimics(player); return; }
                lp[k] = t.localPosition;
                lr[k] = t.localRotation;
            }
            _mimicTimes.Add(Time.time);
            _poseLocalPos.Add(lp);
            _poseLocalRot.Add(lr);
            _poseRootPos.Add(_mimicSource.position);
            _poseRootRot.Add(_mimicSource.rotation);
            while (_mimicTimes.Count > 2 && _mimicTimes[1] <= Time.time - 0.2f)
            {
                _mimicTimes.RemoveAt(0); _poseLocalPos.RemoveAt(0); _poseLocalRot.RemoveAt(0); _poseRootPos.RemoveAt(0); _poseRootRot.RemoveAt(0);
            }
            Vector3[] sp = _poseLocalPos[0];
            Quaternion[] sr = _poseLocalRot[0];
            Vector3 rootPos = _poseRootPos[0];
            Quaternion rootRot = _poseRootRot[0];
            for (int i = 0; i < 3; i++)
            {
                if (_clones[i] == null || _mimicBones[i] == null) continue;
                Transform[] bones = _mimicBones[i];
                bones[0].position = rootPos + rootRot * MimicOffsets[i];
                bones[0].rotation = rootRot;
                int m = Mathf.Min(bones.Length, sp.Length);
                for (int k = 1; k < m; k++)
                {
                    if (bones[k] == null) continue;
                    bones[k].localPosition = sp[k];
                    bones[k].localRotation = sr[k];
                }
            }
            for (int a = _mimicAttackAt.Count - 1; a >= 0; a--)
            {
                if (Time.time < _mimicAttackAt[a]) continue;
                ItemDrop.ItemData weapon = _mimicAttackWeapon[a];
                _mimicAttackAt.RemoveAt(a);
                _mimicAttackWeapon.RemoveAt(a);
                MimicAttack(player, weapon);
            }
        }

        private void MimicAttack(Player player, ItemDrop.ItemData weapon)
        {
            MagicDamageSnapshot damage = GetMagicWeaponDamage(weapon);
            bool ranged = DragonCombat.IsMagicWeapon(weapon);
            Vector3 aimPoint = Vector3.zero;
            Character target = null;
            if (ranged) target = AimTarget(player, DragonCombat.M(50f), out aimPoint);
            for (int i = 0; i < 3; i++)
            {
                if (_clones[i] == null) continue;
                Vector3 origin = _clones[i].transform.position + Vector3.up * 1.3f;
                if (ranged)
                {
                    if (_enableVfx.Value) CreateBeam(origin, aimPoint, new Color(0.40f, 0.70f, 1f, 0.80f), 0.10f, 0.20f);
                    if (target != null) DealMagicWeaponDamage(player, target, damage, 1f);
                }
                else
                {
                    // melee copy: the swing hits in front of the mimic
                    List<Character> hits = GetSphereTargets(player, origin + _clones[i].transform.forward * 1.4f, 1.6f);
                    for (int h = 0; h < hits.Count; h++) DealMagicWeaponDamage(player, hits[h], damage, 1f);
                }
            }
        }

        private float CloneCooldownRemaining()
        {
            return CooldownRemaining("Spellcaster.AfterimageArsenal");
        }

        // Mimics never cast skills (v0.23.4).
        private void CloneCopySkill(Player player, string id)
        {
        }

        private void ClearClones()
        {
            for (int i = 0; i < 3; i++)
            {
                if (_clones[i] != null) Destroy(_clones[i]);
                _clones[i] = null;
                _mimicBones[i] = null;
            }
            _mimicSource = null;
            _mimicSourceBones = null;
            _mimicTimes.Clear(); _poseLocalPos.Clear(); _poseLocalRot.Clear(); _poseRootPos.Clear(); _poseRootRot.Clear();
            _mimicAttackAt.Clear();
            _mimicAttackWeapon.Clear();
        }

        private void CastArcaneRupture(Player player)
        {
            if (Time.time < _ruptureNextCastAt)
                return;

            Vector3 target;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_ruptureRange.Value), out target)) { ShowMessage("Aim at a physical target"); return; }
            if (_ruptureCharges <= 0) { ShowMessage("Arcane Rupture: no charges"); return; }
            CloneCopySkill(player, "arcane_rupture");
            _ruptureCharges--;
            _ruptureRechargeAt.Add(Time.time + Mathf.Max(1f, _ruptureRecharge.Value));
            _ruptureNextCastAt = Time.time + Mathf.Max(0f, _ruptureBuffer.Value);
            StartCoroutine(RuptureRoutine(player, target));
            DragonCombat.PlayClip(player, "hw_rupture", Mathf.Max(0.1f, _ruptureWindup.Value));
            ShowMessage("Arcane Rupture " + _ruptureCharges + "/" + RuptureMax());
        }

        private IEnumerator RuptureRoutine(Player player, Vector3 target)
        {
            if (_enableVfx.Value) StartCoroutine(RingVfx(target, DragonCombat.M(_ruptureRadius.Value), new Color(0.58f, 0.10f, 0.95f, 0.70f), _ruptureWindup.Value));
            yield return new WaitForSeconds(Mathf.Max(0.1f, _ruptureWindup.Value));
            List<Character> targets = GetSphereTargets(player, target, DragonCombat.M(_ruptureRadius.Value));
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
                float asc = IsSpellAscended(player, "arcane_rupture") ? _ruAscDamage.Value / 100f : 1f;
                DealWiz(player, enemy, _ruptureDmg, mult * asc, "arcane_rupture", 16f, false);


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
            if (_enableVfx.Value) CreateLightningBurst(target, DragonCombat.M(_ruptureRadius.Value));
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
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_riftRange.Value), out target)) { ShowMessage("Rift B needs a physical point within 30m"); return; }
            if (Vector3.Distance(_riftA.transform.position, target) > DragonCombat.M(_riftRange.Value)) { ShowMessage("Rift B is too far from Rift A"); return; }
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

        // v0.23.5 universal stack counter (Arcane Rupture charges, Ascended Void Step charges,
        // Arcane Phalanx swords left, Meteor Fall charge stacks while charging).
        private bool StackQuery(string id, out int ready, out int max, out float next)
        {
            ready = 0; max = 0; next = 0f;
            Player player = Player.m_localPlayer;
            if (player == null) return false;
            switch (id)
            {
                case "arcane_rupture":
                    ready = _ruptureCharges; max = RuptureMax(); next = GetRuptureNextRecharge();
                    return true;
                case "void_step":
                    max = VoidMaxCharges(player);
                    if (max <= 1) return false;
                    ready = Mathf.Max(0, _voidCharges < 0 ? max : _voidCharges);
                    next = ready < max ? Mathf.Max(0f, _voidNextCharge - Time.time) : 0f;
                    return true;
                case "arcane_phalanx":
                    if (_phalanxSwords.Count == 0) return false;
                    ready = _phalanxSwords.Count;
                    max = IsSpellAscended(player, "arcane_phalanx") ? Mathf.Max(1, _paAscCount.Value) : Mathf.Max(1, _phalanxCountV.Value);
                    return true;
                case "meteor_fall":
                    if (_meteorChargeShown <= 0 && _meteorChargeNext <= 0f) return false;
                    ready = _meteorChargeShown; max = 3; next = -Mathf.Max(0.01f, _meteorChargeNext); // charging
                    return true;
                case "elemental_cataclysm":
                    if (_cataclysmCharge01 <= 0f) return false;
                    max = Mathf.Max(2, Mathf.RoundToInt(_cataclysmCharge.Value));
                    ready = Mathf.FloorToInt(_cataclysmCharge01 * max + 0.001f);
                    next = -Mathf.Max(0.01f, ready < max ? (1f - (_cataclysmCharge01 * max - ready)) * _cataclysmCharge.Value / max : 0f); // charging
                    return true;
            }
            return false;
        }

        private int RuptureMax()
        {
            Player player = Player.m_localPlayer;
            return player != null && IsSpellAscended(player, "arcane_rupture") ? Mathf.Max(1, _ruAscCharges.Value) : Mathf.Max(1, _ruptureMaxCharges.Value);
        }

        private float GetRuptureNextRecharge()
        {
            int maxCharges = RuptureMax();
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
            if (_ruptureCharges + _ruptureRechargeAt.Count < RuptureMax()) _ruptureCharges++; // Ascended: 4th charge
            if (_ruptureRechargeAt.Count == 0) return;
            for (int i = _ruptureRechargeAt.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _ruptureRechargeAt[i])
                {
                    _ruptureRechargeAt.RemoveAt(i);
                    _ruptureCharges = Mathf.Min(RuptureMax(), _ruptureCharges + 1);
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
            if (Time.time >= _phalanxExpireAt)
            {
                ClearPhalanx();
                _phalanxVolleyArmed = false;
                if (CooldownRemaining("Spellcaster.ArcanePhalanx") <= 0f) StartCooldown("Spellcaster.ArcanePhalanx", _phalanxCooldown.Value);
                ShowMessage("Arcane Phalanx faded");
                return;
            }
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

        // v0.23.1 Overcharge (Framework): 300 Eitr spent -> 12s of +40% wind up speed, +40% Eitr
        // Regen, +40% Magic Damage; nothing accumulates while active nor for the 5s buffer after.
        private void RegisterEitrSpent(float amount)
        {
            if (GetAdvancement(Player.m_localPlayer) != "Wizard") return;
            if (Time.time < _overchargeLockedUntil) return;
            _overchargeEitrSpent += Mathf.Max(0f, amount);
            if (_overchargeEitrSpent >= Mathf.Max(1f, _ocThreshold.Value))
            {
                _overchargeEitrSpent = 0f;
                _overchargeUntil = Time.time + Mathf.Max(0.5f, _ocDuration.Value);
                _overchargeLockedUntil = _overchargeUntil + Mathf.Max(0f, _ocBuffer.Value);
                ShowMessage("OVERCHARGE");
                DragonCombat.PlayAccent(Player.m_localPlayer, "wiz_overcharge", 0.12f);
            }
        }

        public float OverchargeGauge()
        {
            return Time.time < _overchargeUntil ? -1f : _overchargeEitrSpent;
        }

        private float ScaleWindup(Player player, float seconds)
        {
            float value = DragonCombat.ScaleWindup(player, seconds);
            if (Time.time < _overchargeUntil) value /= 1f + Mathf.Max(0f, _ocWindup.Value) / 100f;
            return Mathf.Max(0f, value);
        }

        // Overcharge Magic Damage (Wizard spells are magic; applied to every element of the hit).
        private float ElementMultiplier()
        {
            return Time.time < _overchargeUntil ? 1f + Mathf.Max(0f, _ocMagic.Value) / 100f : 1f;
        }

        private void StartCooldown(string id, float normal)
        {
            float seconds = _testingForceCooldowns.Value ? _testingCooldown.Value : DragonCombat.ScaleCooldown(Player.m_localPlayer, id, normal);
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
            hit.m_damage.m_blunt = blunt * e * magic;
            hit.m_damage.m_slash = slash * e * magic;
            hit.m_damage.m_pierce = pierce * e * magic;
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

        // Universal burn rule (v0.21.2): 0.5s ticks dealing the skill's own damage (perTick).
        private IEnumerator BurnRoutine(Player attacker, Character target, bool spirit, float duration, float perTick)
        {
            float end = Time.time + Mathf.Max(0.1f, duration);
            while (target != null && !target.IsDead() && Time.time < end)
            {
                yield return new WaitForSeconds(0.5f);
                if (target == null || target.IsDead()) yield break;
                if (spirit) DragonCombat.ApplySpiritBurnTick(attacker, target, perTick);
                else DragonCombat.ApplyFireBurnTick(attacker, target, perTick);
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

        private static FieldInfo _customDataField;

        private string ReadData(Player player, string key)
        {
            try
            {
                if (_customDataField == null) _customDataField = typeof(Player).GetField("m_customData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo f = _customDataField;
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

        // v0.25.3 perf: the type is looked up once (it scanned every loaded assembly on every IMGUI event).
        private static Type _advancedPluginType;
        private static bool _advancedPluginSearched;

        private bool IsAdvancedSkillbookOpen()
        {
            try
            {
                if (!_advancedPluginSearched)
                {
                    _advancedPluginSearched = true;
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    for (int i = 0; i < assemblies.Length && _advancedPluginType == null; i++)
                        _advancedPluginType = assemblies[i].GetType("AlbedosCustomClassesAdvanced.AdvancedPlugin", false);
                }
                for (int i = 0; i < 1; i++)
                {
                    Type type = _advancedPluginType;
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
            Player player = Player.m_localPlayer;
            if (player == null || !_showHud.Value || GetClass(player) != "Sorcerer" || DragonCombat.IsTreeHotbarActive(player))
                return;
            if (Plugin.IsClassPanelOpen || IsAdvancedSkillbookOpen() || DragonCombat.IsGameplayHudSuppressed())
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

            GUI.Label(new Rect(x, y - 21f * scale, totalWidth, 18f * scale), (string.IsNullOrEmpty(adv) ? "SORCERER" : (adv == "Wizard" ? "ARCHMAGE" : adv == "Spellcaster" ? "HORIZON WALKER" : adv.ToUpper())), _titleStyle);

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
