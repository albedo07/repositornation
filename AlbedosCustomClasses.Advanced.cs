using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using AlbedosCustomClassesSkills;
using DragonsAltarCombat;
using AlbedosCustomClasses;

namespace AlbedosCustomClassesAdvanced
{
    public class MoonlightDotTracker : MonoBehaviour
    {
        private AdvancedPlugin _plugin;
        private Player _owner;
        private float _radius;
        private float _spiritPerSecond;
        private float _duration;
        private readonly HashSet<int> _dotted = new HashSet<int>();

        public void Initialize(
            AdvancedPlugin plugin,
            Player owner,
            float radius,
            float spiritPerSecond,
            float duration
        )
        {
            _plugin = plugin;
            _owner = owner;
            _radius = Mathf.Max(0.1f, radius);
            _spiritPerSecond = Mathf.Max(0f, spiritPerSecond);
            _duration = Mathf.Max(0.1f, duration);
        }

        private void Update()
        {
            if (_plugin == null || _owner == null)
                return;

            Collider[] hits = Physics.OverlapSphere(transform.position, _radius);

            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();

                if (target == null)
                    continue;

                int id = target.GetInstanceID();

                if (_dotted.Contains(id))
                    continue;

                if (!_plugin.IsMoonlightEnemy(_owner, target))
                    continue;

                _dotted.Add(id);
                _plugin.ApplyMoonlightSpiritDot(
                    _owner,
                    target,
                    _spiritPerSecond,
                    _duration
                );
            }
        }
    }

    internal class RefreshingDotState
    {
        public Player Attacker;
        public Character Target;
        public float DamagePerSecond;
        public float EndTime;
        public float NextTick;
    }

    internal class JudgementMarkState
    {
        public Character Target;
        public string Source;
        public float EndTime;
    }

    internal class PriestRelicState
    {
        public GameObject Cross;
        public Vector3 Position;
        public float Radius;
        public float EndTime;
        public bool IsLightning;
        public string CooldownId;
        public float CooldownSeconds;
        public bool CooldownStarted;
    }

    public class PriestRelicMarker : MonoBehaviour
    {
        internal PriestRelicState State;
    }

    public class AegisWallMarker : MonoBehaviour
    {
        public Player Caster;
        public bool Shattered;
    }

    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("albedo.customclasses", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.skills", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.combatruntime", BepInDependency.DependencyFlags.HardDependency)]
    public class AdvancedPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "albedo.customclasses.advanced";
        public const string ModName = "Dragon's Altar - Advancements";
        public const string ModVersion = "0.22.1";

        private const string ClassDataKey = "AlbedoCustomClasses.Class";
        private const string AdvancementDataKey = "AlbedoCustomClasses.Advancement";
        private const string PaladinVitalityKey = "AlbedoCustomClasses.Paladin.Vitality";
        private const string PaladinOffenseKey = "AlbedoCustomClasses.Paladin.Offense";
        private const string PaladinPassiveKey = "AlbedoCustomClasses.Paladin.Passive";
        private const string PriestOffenseKey = "AlbedoCustomClasses.Priest.Offense";

        public static AdvancedPlugin Instance;

        private ConfigEntry<KeyCode> _modifier;
        private ConfigEntry<KeyCode> _skill4;
        private ConfigEntry<KeyCode> _skill5;
        private ConfigEntry<KeyCode> _skill7;
        private ConfigEntry<KeyCode> _skill8;
        private ConfigEntry<KeyCode> _skill9;
        private ConfigEntry<KeyCode> _passiveActive;
        private ConfigEntry<KeyCode> _ultimate;
        private ConfigEntry<KeyCode> _skillbookKey;

        private ConfigEntry<bool> _enableVfx;
        private ConfigEntry<bool> _showCombatHud;
        private ConfigEntry<bool> _uiColorSpaceCorrection;
        private ConfigEntry<float> _hudScale;
        private ConfigEntry<float> _hudBottomOffset;
        private ConfigEntry<bool> _testingForceCooldowns;
        private ConfigEntry<float> _testingCooldownSeconds;

        private ConfigEntry<float> _moonCooldown;
        private ConfigEntry<float> _moonStamina;
        private ConfigEntry<float> _moonLength;
        private ConfigEntry<float> _moonWidth;
        private ConfigEntry<float> _moonSpeed;
        private DamageConfig _moonDamage;
        private ConfigEntry<float> _moonSpiritDot;
        private ConfigEntry<float> _moonSpiritDuration;

        private ConfigEntry<float> _crescentCooldown;
        private ConfigEntry<float> _crescentStamina;
        private ConfigEntry<float> _crescentRange;
        private ConfigEntry<float> _crescentTravelTime;
        private ConfigEntry<float> _crescentSlashWidth;
        private ConfigEntry<float> _crescentSlashHeight;
        private ConfigEntry<float> _crescentSpreadAngle;
        private ConfigEntry<float> _crescentPersistentTick;
        private DamageConfig _crescentDamage;

        private ConfigEntry<float> _judgementCooldown;
        private ConfigEntry<float> _judgementStamina;
        private ConfigEntry<float> _judgementRange;
        private ConfigEntry<float> _judgementRadius;
        private ConfigEntry<float> _judgementSlashDamage;
        private ConfigEntry<float> _judgementBuffer;
        private readonly float[] _judgementChargeReadyAt = new float[4];
        private float _judgementNextCastAt;

        private ConfigEntry<float> _severedCooldown;
        private ConfigEntry<float> _severedStamina;
        private ConfigEntry<float> _severedRange;
        private ConfigEntry<float> _severedWidth;
        private ConfigEntry<float> _severedDelay;
        private DamageConfig _severedDamage;

        private ConfigEntry<float> _emptyCooldown;
        private ConfigEntry<float> _emptyStamina;
        private ConfigEntry<float> _emptyCounterWindow;
        private ConfigEntry<float> _emptyBehindDistance;
        private ConfigEntry<int> _emptyCutCount;
        private ConfigEntry<float> _emptyCutInterval;
        private ConfigEntry<float> _emptySlashDamage;
        private float _emptySheathCounterUntil;

        private ConfigEntry<float> _halfmoonCooldown;
        private ConfigEntry<float> _halfmoonStamina;
        private ConfigEntry<float> _halfmoonRadius;
        private DamageConfig _halfmoonDamage;
        private ConfigEntry<float> _halfmoonSpiritDot;
        private ConfigEntry<float> _halfmoonSpiritDuration;
        private ConfigEntry<float> _halfmoonSecondSlashDelay;

        private ConfigEntry<float> _swordSkillBonus;
        private ConfigEntry<float> _swordAttackSpeedPassive;
        private ConfigEntry<float> _swordAttackSpeedActive;
        private ConfigEntry<float> _swordActiveDuration;
        private ConfigEntry<float> _swordActiveCooldown;

        private ConfigEntry<float> _stompCooldown;
        private ConfigEntry<float> _stompStamina;
        private ConfigEntry<float> _stompWindup;
        private ConfigEntry<float> _stompRadius;
        private ConfigEntry<float> _stompAftershockDelay;
        private ConfigEntry<float> _stompAftershockRadius;
        private DamageConfig _stompDamage;

        private ConfigEntry<float> _boneCooldown;
        private ConfigEntry<float> _boneStamina;
        private ConfigEntry<float> _boneWindup;
        private ConfigEntry<float> _boneRadius;
        private DamageConfig _boneDamage;

        private ConfigEntry<float> _circleCooldown;
        private ConfigEntry<float> _circleStamina;
        private ConfigEntry<float> _circleWindup;
        private ConfigEntry<float> _circleRadius;
        private ConfigEntry<float> _circleDamageMultiplier;
        private ConfigEntry<float> _circleWindupTravel;

        private ConfigEntry<float> _seismicCooldown;
        private ConfigEntry<float> _seismicStamina;
        private ConfigEntry<float> _seismicRange;
        private ConfigEntry<float> _seismicWidth;
        private ConfigEntry<float> _seismicTravelTime;
        private ConfigEntry<float> _seismicEndRadius;
        private ConfigEntry<float> _seismicDamageMultiplier;

        private ConfigEntry<float> _reaverCooldown;
        private ConfigEntry<float> _reaverStamina;
        private ConfigEntry<float> _reaverRadius;
        private ConfigEntry<float> _reaverDuration;
        private ConfigEntry<float> _reaverHitRadius;
        private ConfigEntry<float> _reaverDamageMultiplier;

        private ConfigEntry<float> _whirlwindCooldown;
        private ConfigEntry<float> _whirlwindStamina;
        private ConfigEntry<float> _whirlwindRadius;
        private ConfigEntry<float> _whirlwindDuration;
        private ConfigEntry<float> _whirlwindInterval;
        private ConfigEntry<float> _whirlwindWeaponMultiplier;

        private ConfigEntry<float> _mercAxesBonus;
        private ConfigEntry<float> _mercAttackDamage;
        private ConfigEntry<float> _mercHealthBonus;
        private ConfigEntry<float> _mercAggroRadius;
        private ConfigEntry<float> _mercTauntRadius;
        private ConfigEntry<float> _mercTauntDuration;
        private ConfigEntry<float> _mercExposeDuration;
        private ConfigEntry<float> _mercTauntCooldown;
        private ConfigEntry<float> _mercFuryGainPerWeaponHit;
        private ConfigEntry<float> _mercFuryGainPerSkillTarget;
        private ConfigEntry<float> _mercFuryDuration;
        private ConfigEntry<float> _mercFuryCooldown;
        private ConfigEntry<float> _mercFuryBoneConeRange;
        private ConfigEntry<float> _mercFuryBoneConeAngle;
        private ConfigEntry<float> _mercFuryBoneConeMultiplier;
        private float _mercFury;
        private float _mercFuryUntil;
        private float _mercFuryCooldownUntil;
        private bool _mercFuryEndAnnounced;

        private ConfigEntry<float> _goddessCooldown;
        private ConfigEntry<float> _goddessStamina;
        private ConfigEntry<float> _goddessRadius;
        private ConfigEntry<float> _goddessRange;
        private DamageConfig _goddessDamage;
        private ConfigEntry<float> _goddessSpiritDot;
        private ConfigEntry<float> _goddessSpiritDuration;
        private ConfigEntry<float> _goddessWindup;

        private ConfigEntry<float> _rayCooldown;
        private ConfigEntry<float> _rayStamina;
        private ConfigEntry<float> _rayWindup;
        private ConfigEntry<float> _rayHealPercent;
        private ConfigEntry<float> _rayDamageBuff;
        private ConfigEntry<float> _rayBuffDuration;
        private ConfigEntry<float> _rayRadius;
        private ConfigEntry<float> _raySpiritBurnDuration;
        private ConfigEntry<float> _shieldChargeCooldown;
        private ConfigEntry<float> _shieldChargeStamina;
        private ConfigEntry<float> _shieldChargeDistance;
        private ConfigEntry<float> _shieldChargeSpeedMultiplier;
        private ConfigEntry<float> _shieldChargeRadius;
        private ConfigEntry<float> _shieldChargePersistentTick;
        private DamageConfig _shieldChargeDamage;
        private bool _shieldChargeActive;
        private Player _shieldChargePlayer;

        private ConfigEntry<float> _verdictCooldown;
        private ConfigEntry<float> _verdictStamina;
        private ConfigEntry<float> _verdictRange;
        private ConfigEntry<float> _verdictRadius;
        private ConfigEntry<float> _verdictWindup;
        private DamageConfig _verdictDamage;

        private ConfigEntry<float> _aegisCooldown;
        private ConfigEntry<float> _aegisStamina;
        private ConfigEntry<float> _aegisRange;
        private ConfigEntry<float> _aegisWidth;
        private ConfigEntry<float> _aegisHeight;
        private ConfigEntry<float> _aegisDuration;
        private ConfigEntry<float> _aegisWindup;
        private ConfigEntry<float> _aegisImpactRadius;
        private ConfigEntry<float> _aegisShockwaveRadius;
        private DamageConfig _aegisDamage;
        private DamageConfig _aegisShockwaveDamage;

        private ConfigEntry<float> _judgementMarkDuration;
        private ConfigEntry<float> _judgementMarkedMultiplier;
        private ConfigEntry<float> _judgementCrossMultiplier;
        private ConfigEntry<float> _judgementDetonationLightning;
        private ConfigEntry<float> _judgementDetonationSpirit;
        private ConfigEntry<float> _judgementCrippleDuration;
        private readonly Dictionary<int, JudgementMarkState> _judgementMarks = new Dictionary<int, JudgementMarkState>();
        private string _paladinMarkSourceContext = string.Empty;

        // v0.17.0 Paladin rework + temporary Ascended test switch.
        private ConfigEntry<string> _testingAscendedSkills;
        private string _ascendedCacheRaw;
        private readonly HashSet<string> _ascendedCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _judgementDetonatedOnLastHit;
        private ConfigEntry<float> _goddessRadiusV17, _goddessCrossHeight, _goddessCrossWidth, _goddessAscRadius, _goddessAscSizeMultiplier;
        private DamageConfig _goddessAscDamage;
        private ConfigEntry<float> _rayAscBarrier, _rayAscBarrierDuration;
        private ConfigEntry<float> _hammerCooldown, _hammerStamina, _hammerWindup, _hammerRange, _hammerTravelTime, _hammerBaseRadius,
            _hammerStepMeters, _hammerDamagePerStep, _hammerTick, _hammerCrippleDuration, _hammerCatchCooldownCut,
            _hammerStartHeight, _hammerStartWidth, _hammerGrowthInterval, _hammerHeightPerStep, _hammerWidthPerStep, _hammerMaxHeight, _hammerMaxWidth,
            _hammerAscMaxHeight, _hammerAscMaxWidth, _hammerAscWidthPerStep, _hammerHitRadiusPerHeight, _hammerDamageCap;
        private DamageConfig _hammerDamage;
        private ConfigEntry<float> _angelCooldown, _angelStamina, _angelJumpHeight, _angelRiseTime, _angelDiveSpeed, _angelRadius,
            _angelBrokenBones, _angelRingRadius, _angelRingDuration, _angelBurnDuration, _angelFireDot, _angelSpiritDot, _angelHyperAfter;
        private DamageConfig _angelDamage;
        private ConfigEntry<float> _chargeAscDistance, _chargeAscHitRadius, _chargeAscBashRadius, _chargeAscBashAngle;
        private ConfigEntry<float> _smiteStormRadius, _smiteStormDuration, _smiteStormTick, _smiteStormDotDuration, _smiteStormFireDot, _smiteStormSpiritDot;
        private DamageConfig _smiteStormDamage;
        private ConfigEntry<float> _rsAscCooldown, _rsAscStamina, _rsAscWindup, _rsAscRange, _rsAscRadius, _rsAscExpose, _rsAscFollowRadius,
            _rsAscFollowMultiplier, _rsAscTrailRange, _rsAscTrailTime, _rsAscTrailTick, _rsAscSpiritDot, _rsAscSpiritDuration;
        private DamageConfig _rsAscDamage, _rsAscTrailDamage;
        private bool _judgementDetonationInProgress;

        private ConfigEntry<float> _divineCooldown;
        private ConfigEntry<float> _divineStamina;
        private ConfigEntry<float> _divineRadius;
        private DamageConfig _divineDamage;
        private ConfigEntry<float> _divineFireDot;
        private ConfigEntry<float> _divineSpiritDot;
        private ConfigEntry<float> _divineSpiritDuration;
        private ConfigEntry<float> _divineWindup;
        private ConfigEntry<float> _divineTrailRange;
        private ConfigEntry<float> _divineTrailTravelTime;
        private ConfigEntry<float> _divineTrailPersistentTick;
        private DamageConfig _divineTrailDamage;
        private ConfigEntry<float> _divineZapDamage;

        private ConfigEntry<float> _paladinElementalBonus;
        private ConfigEntry<float> _paladinElementalFlatEitr;
        private ConfigEntry<float> _paladinElementalEitrRegen;
        private ConfigEntry<float> _holyKnightMoveSpeed;
        private ConfigEntry<float> _holyKnightFlatHealth;
        private ConfigEntry<float> _holyKnightFlatStamina;
        private ConfigEntry<float> _holyKnightRegen;
        private ConfigEntry<float> _holyKnightAttackSpeed;
        private ConfigEntry<float> _mercTwoHandedAttackSpeed;
        private ConfigEntry<float> _priestArmorBonusPercent;
        private ConfigEntry<float> _clericBlessingHealth;
        private ConfigEntry<float> _clericBlessingRegen;
        private ConfigEntry<float> _holyTrinityClubs;
        private ConfigEntry<float> _holyTrinityMinPercent;
        private ConfigEntry<float> _crucibleArmorPercent;
        private DamageConfig _shockwaveDamage;
        private ConfigEntry<float> _shockwaveRadius;
        private ConfigEntry<float> _shockwaveCooldown;
        private ConfigEntry<float> _parryHyperArmorSeconds;
        private ConfigEntry<float> _parryEmpowerPercent;
        private bool _parryEmpowerPending;
        // v0.21.0 Priest Ascended
        private ConfigEntry<float> _sanctifiedDuration, _bloomHealPercent, _bloomRadius;
        private DamageConfig _bloomDamage, _relicAscBlastDamage, _crossAscBurstDamage, _pillarDamage;
        private ConfigEntry<float> _ahwAllyRange, _diHealAtMax, _angelWindupTotal;
        private ConfigEntry<float> _ahwRadius, _ahwEchoDelay, _ahwEchoRadius, _ahwEchoPercent, _ahwLowHp;
        private ConfigEntry<float> _relicAscRadius, _relicAscChainRange, _relicAscBlastRadius, _holyRelicAscBuff, _holyRelicAscEndHeal;
        private ConfigEntry<float> _diAscBarrier, _crossAscWidth, _crossAscRange, _crossAscBurstRadius;
        private ConfigEntry<float> _hjAscRadius, _hjAscDuration, _hjAscBeamHeal, _hjAscPillarRadius, _tempestAscRadius, _tempestAscDefense;
        private readonly Dictionary<int, float> _sanctifiedUntil = new Dictionary<int, float>();
        private string _empoweredSkill = "";
        private float _empoweredUntil;

        private ConfigEntry<float> _lightningRelicCooldown;
        private ConfigEntry<float> _lightningRelicStamina;
        private ConfigEntry<float> _lightningRelicRadius;
        private ConfigEntry<float> _lightningRelicRange;
        private ConfigEntry<float> _lightningRelicDuration;
        private ConfigEntry<float> _lightningRelicInterval;
        private DamageConfig _lightningRelicDamage;
        private ConfigEntry<float> _lightningRelicSpiritDot;
        private ConfigEntry<float> _lightningRelicSpiritDuration;

        private ConfigEntry<float> _holyRelicCooldown;
        private ConfigEntry<float> _holyRelicStamina;
        private ConfigEntry<float> _holyRelicRadius;
        private ConfigEntry<float> _holyRelicRange;
        private ConfigEntry<float> _holyRelicDuration;
        private ConfigEntry<float> _holyRelicInterval;
        private ConfigEntry<float> _holyRelicHealPercent;
        private ConfigEntry<float> _holyRelicBuffDuration;
        private ConfigEntry<float> _holyRelicDamageBuff;
        private ConfigEntry<float> _holyRelicAttackSpeedBuff;
        private ConfigEntry<float> _holyRelicMoveSpeedBuff;
        private ConfigEntry<float> _holyRelicRegenBuff;
        private ConfigEntry<float> _holyRelicDefenseBuff;

        private ConfigEntry<float> _consecratedConnectRange;
        private ConfigEntry<float> _consecratedRadius;
        private ConfigEntry<float> _consecratedMultiplier;
        private ConfigEntry<float> _consecratedExposeDuration;

        private ConfigEntry<float> _interventionCooldown;
        private ConfigEntry<float> _interventionStamina;
        private ConfigEntry<float> _interventionRange;
        private ConfigEntry<float> _interventionRadius;
        private ConfigEntry<float> _interventionWindup;
        private ConfigEntry<float> _interventionHealPercent;
        private ConfigEntry<float> _interventionBarrierHp, _interventionBarrierArmor;
        private ConfigEntry<float> _interventionBuffDuration;
        private ConfigEntry<float> _interventionExposeDuration;
        private DamageConfig _interventionDamage;

        private ConfigEntry<float> _grandCrossCooldown;
        private ConfigEntry<float> _grandCrossStamina;
        private ConfigEntry<float> _grandCrossWidth;
        private ConfigEntry<float> _grandCrossRange;
        private ConfigEntry<float> _grandCrossTravelTime;
        private ConfigEntry<float> _grandCrossWindup;
        private ConfigEntry<float> _grandCrossTickInterval;
        private ConfigEntry<float> _grandCrossSpiritDot;
        private ConfigEntry<float> _grandCrossSpiritDuration;
        private DamageConfig _grandCrossDamage;

        private ConfigEntry<float> _heavensCooldown;
        private ConfigEntry<float> _heavensStamina;
        private ConfigEntry<float> _heavensRadius;
        private ConfigEntry<float> _heavensWindup;
        private ConfigEntry<float> _heavensDuration;
        private ConfigEntry<float> _heavensStrikeInterval;
        private ConfigEntry<int> _heavensStrikesPerWave;
        private ConfigEntry<float> _heavensStrikeRadius;
        private ConfigEntry<float> _heavensFrostDuration;
        private DamageConfig _heavensDamage;

        private ConfigEntry<float> _tempestCooldown;
        private ConfigEntry<float> _tempestStamina;
        private ConfigEntry<float> _tempestRadius;
        private ConfigEntry<float> _tempestRange;
        private ConfigEntry<float> _tempestDuration;
        private ConfigEntry<float> _tempestStrikeInterval;
        private ConfigEntry<int> _tempestMaxStrikes;
        private ConfigEntry<float> _tempestZapDamage;
        private DamageConfig _tempestDamage;
        private ConfigEntry<float> _tempestSpiritDot;
        private ConfigEntry<float> _tempestFireDot;
        private ConfigEntry<float> _tempestFrostDuration;
        private ConfigEntry<float> _tempestFireDuration;
        private ConfigEntry<float> _tempestSpiritDuration;
        private ConfigEntry<float> _tempestExposeDuration;

        private ConfigEntry<float> _priestMartialSkillBonus;
        private ConfigEntry<float> _priestElementalBonus;
        private ConfigEntry<float> _grandProcChance;
        private ConfigEntry<float> _grandProcReduction;
        private ConfigEntry<float> _grandCooldown;
        private ConfigEntry<float> _grandStamina;
        private ConfigEntry<float> _grandRadius;
        private ConfigEntry<float> _grandBarrierHp;
        private ConfigEntry<float> _grandBarrierDuration;
        private ConfigEntry<float> _grandWindup;

        private ConfigEntry<string> _defaultPaladinVitality;
        private ConfigEntry<string> _defaultPaladinOffense;
        private ConfigEntry<string> _defaultPriestOffense;
        private ConfigEntry<float> _sharedCrossCastRange;
        private ConfigEntry<float> _acrobaticAscentLift;
        private ConfigEntry<float> _acrobaticJumpHeight;

        private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        private readonly Dictionary<int, float> _crossBuffUntil = new Dictionary<int, float>();
        private readonly Dictionary<int, BarrierState> _barriers = new Dictionary<int, BarrierState>();
        private readonly List<PriestRelicState> _priestRelics = new List<PriestRelicState>();
        private readonly Dictionary<int, RefreshingDotState> _refreshingSpiritBurns = new Dictionary<int, RefreshingDotState>();
        private readonly Dictionary<int, RefreshingDotState> _refreshingFireBurns = new Dictionary<int, RefreshingDotState>();
        private bool _lightningRelicCasting;
        private bool _holyRelicCasting;

        private float _swordActiveUntil;
        private float _nextAggroPulse;

        private Harmony _harmony;


        private bool _skillbookOpen;
        private Rect _skillbookRect = new Rect(0f, 0f, 1180f, 772f);
        private bool _savedCursorVisible;
        private CursorLockMode _savedCursorLock;

        private GUIStyle _titleStyle;
        private GUIStyle _slotStyle;
        private GUIStyle _slotLockedStyle;
        private GUIStyle _passiveStyle;
        private GUIStyle _bookHeaderStyle;
        private GUIStyle _bookTextStyle;
        private GUIStyle _hudKeyStyle;
        private GUIStyle _hudCooldownStyle;
        private Texture2D _slotReadyTex;
        private Texture2D _slotCooldownTex;
        private Texture2D _slotLockedTex;

        // v0.14.5 Immortal Heroes Skill Tree reference-asset implementation.
        // Gameplay progression is intentionally not wired yet; this pass establishes the
        // reusable visual language and layout before Project 4 changes skill mechanics.
        private GUIStyle _treeWindowStyle;
        private GUIStyle _treeTitleStyle;
        private GUIStyle _treeHeaderStyle;
        private GUIStyle _treeHeaderEmblemStyle;
        private GUIStyle _treeSubHeaderStyle;
        private GUIStyle _treeNodeIconStyle;
        private GUIStyle _treeUltimateIconStyle;
        private GUIStyle _treeNodeNameStyle;
        private GUIStyle _treeNamePlateDarkStyle;
        private GUIStyle _treeNamePlateLightStyle;
        private GUIStyle _treeRankStyle;
        private GUIStyle _treeTinyStyle;
        private GUIStyle _treeFooterKeyStyle;
        private GUIStyle _treeFooterGraceStyle;
        private GUIStyle _treeFooterConfirmStyle;
        private GUIStyle _treeFooterConfirmHoverStyle;
        private GUIStyle _treeFooterPendingStyle;
        private GUIStyle _treeFooterKeyHoverStyle;
        private GUIStyle _treeFooterKeyCaptureStyle;
        private GUIStyle _treeTinyLeftStyle;
        private GUIStyle _treeTooltipTitleStyle;
        private GUIStyle _treeTooltipBodyStyle;
        private GUIStyle _treeWatermarkStyle;
        private Texture2D _treeMainTex;
        private Texture2D _treeClassAreaTex;
        private Texture2D _treeAdvAreaTex;
        private Texture2D _treeClassHeaderTex;
        private Texture2D _treeAdvHeaderTex;
        private Texture2D _treeNodeInnerTex;
        private Texture2D _treeGoldTex;
        private Texture2D _treeShadowTex;
        private Texture2D _treeHotbarTex;
        private Texture2D _treeGoldGlowTex;
        private Texture2D _treeMagentaGlowTex;
        private Texture2D _treeMaroonGlowTex;
        private Texture2D _treeBlueGlowTex;
        private Texture2D _treeReferenceBackdropTex;
        private bool _treeReferenceBackdropLoadAttempted;
        private Texture2D _treeTierPlusTex;
        private Texture2D _treeTierMinusTex;
        private Texture2D _treeConfirmPlaqueTex;

        // v0.15.0 click-to-assign hotbar keys (Skill Tree hotbar).
        // v0.15.1: every binding is either a single key or Modifier + Key (Modifier None = single key).
        // Index 0-6 = numbered slots 1-7, index 7 = Grace.
        private const int HotbarBindingCount = 8;
        private const int BindGrace = 7;
        private const int BindNone = -1;
        private readonly ConfigEntry<KeyCode>[] _hotbarSlotKeys = new ConfigEntry<KeyCode>[HotbarBindingCount];
        private readonly ConfigEntry<KeyCode>[] _hotbarSlotMods = new ConfigEntry<KeyCode>[HotbarBindingCount];
        private int _bindCaptureTarget = BindNone;
        private int _bindCaptureStartFrame;
        private KeyCode _bindCaptureFirst = KeyCode.None;
        private static KeyCode[] _bindableKeyCodes;

        // v0.16.0 drag & drop hotbar (numbered slots 1-7). Grace keeps its own slot and is never dragged.
        // Permanent (cannot leave the hotbar): Signature, Ascended and Ultimate skills.
        private const string HotbarLayoutKeyPrefix = "ImmortalHeroes.HotbarLayout.";
        private static readonly float[] HotbarSlotCenters = { 229f, 287f, 344f, 402f, 459f, 517f, 574f };
        private static readonly string[] DefaultClericPaladinHotbar =
            { "righteous_strike", "goddess_relic", "judgement_hammer", "shield_charge", "ray_of_hope", "holy_wave", "electric_smite" };
        private string[] _hotbarLayout;
        private string _hotbarLayoutOwnerKey = "";
        private readonly Dictionary<string, Texture2D> _treeSkillIconTex = new Dictionary<string, Texture2D>();
        private Texture2D _treeSlotEmptyTex;
        private Texture2D _treePermanentBadgeTex;
        private string _pressSkillId = "";
        private int _pressSlot = -1;
        private Vector2 _pressPos;
        private bool _dragActive;
        private int _pressReleasedFrame;
        private string _treeHoveredTitle = "";
        private string _treeHoveredBody = "";
        private GUIStyle _treeHotkeyStyle;
        private string _treeSelectedNodeId = "";
        private readonly Dictionary<string, int> _treePrototypePending = new Dictionary<string, int>();

        private void Awake()
        {
            Instance = this;

            _modifier = Config.Bind("Hotkeys", "Modifier", KeyCode.Mouse3, "Thumb mouse button used with advanced skills.");
            _skill4 = Config.Bind("Hotkeys", "Skill4", KeyCode.Alpha4, "First advancement active skill.");
            _skill5 = Config.Bind("Hotkeys", "Skill5", KeyCode.Alpha5, "Second advancement active skill. WIP during framework testing.");
            _skill7 = Config.Bind("Hotkeys", "Skill7", KeyCode.Alpha7, "Additional active skill slot 7.");
            _skill8 = Config.Bind("Hotkeys", "Skill8", KeyCode.Alpha8, "Additional development active skill slot 8.");
            _skill9 = Config.Bind("Hotkeys", "Skill9", KeyCode.Alpha9, "Additional development active skill slot 9 / final Ultimate slot for five-skill advancements.");
            _passiveActive = Config.Bind("Hotkeys", "PassiveActiveKey", KeyCode.R, "Activatable passive key. Dragon's Altar framework reserves Mouse4 + R for activatable passives.");

            // v0.10.0 migration: old configs may still contain PassiveActive = Alpha4.
            // The legacy key is intentionally ignored. Activatable passives are fixed to R.
            if (_passiveActive.Value != KeyCode.R)
            {
                Logger.LogWarning("PassiveActiveKey conflicted with the Dragon's Altar control layout. Resetting activatable passive to R.");
                _passiveActive.Value = KeyCode.R;
            }
            _ultimate = Config.Bind("Hotkeys", "Skill6", KeyCode.Alpha6, "Advancement active skill slot 6. Ultimate only when slot 6 is the class's last numbered skill.");
            _skillbookKey = Config.Bind("Hotkeys", "Skillbook", KeyCode.K, "Open or close the skillbook.");

            // v0.15.0/v0.15.1: Skill Tree hotbar keys. Rebind in-game by clicking the labels under
            // the hotbar slots (right-click resets). Each binding is a single key or Modifier + Key;
            // set a Modifier to None for a single key. Prototype: casting still uses [Hotkeys] until
            // the tree hotbar is wired to the combat runtime.
            for (int i = 0; i < 7; i++)
            {
                string slot = "Slot" + (i + 1).ToString();
                _hotbarSlotKeys[i] = Config.Bind("Hotbar", slot, KeyCode.Alpha1 + i, "Key for numbered hotbar slot " + (i + 1).ToString() + ".");
                _hotbarSlotMods[i] = Config.Bind("Hotbar", slot + "Modifier", KeyCode.Mouse3, "Hold-modifier for slot " + (i + 1).ToString() + " (Mouse3 = M4). None = single key.");
            }
            _hotbarSlotKeys[BindGrace] = Config.Bind("Hotbar", "GraceKey", KeyCode.R, "Key for the Grace slot.");
            _hotbarSlotMods[BindGrace] = Config.Bind("Hotbar", "GraceModifier", KeyCode.Mouse3, "Hold-modifier for the Grace slot (Mouse3 = M4). None = single key.");

            _enableVfx = Config.Bind("Interface", "EnableVFX", true, "Enable advanced-skill visual effects.");
            _showCombatHud = Config.Bind("Interface", "ShowCombatHud", true, "Show the unified class skill HUD.");
            _uiColorSpaceCorrection = Config.Bind("Interface", "SkillTreeColorSpaceCorrection", true, "Compensate the Skill Tree artwork for Valheim's Linear color space so it is not drawn washed out. Disable only if the tree looks too dark.");
            _hudScale = Config.Bind("Interface", "HudScale", 1f, "Unified HUD scale.");
            _hudBottomOffset = Config.Bind("Interface", "HudBottomOffset_v0113", 105f, "Bottom margin for the compact RPG skill HUD. Fresh v0.11.3 key avoids stale 330px development offsets.");
            _testingForceCooldowns = Config.Bind("Testing", "ForceCooldowns", true, "Testing mode: force every advancement cooldown to one value.");
            _testingCooldownSeconds = Config.Bind("Testing", "CooldownSeconds", 5f, "Testing cooldown used while ForceCooldowns is enabled.");

            _moonCooldown = Config.Bind("Sword Master Moonlight Splitter", "Cooldown", 12f, "Seconds.");
            _moonStamina = Config.Bind("Sword Master Moonlight Splitter", "StaminaCost", 24f, "Stamina cost.");
            _moonLength = Config.Bind("Sword Master Moonlight Splitter", "TravelDistanceMeters_v0109", 50f, "Authoritative v0.10.9 travel distance. Fresh key prevents old 25m configs from overriding the 50m specification.");
            _moonWidth = Config.Bind("Sword Master Moonlight Splitter", "WidthMeters_v0109", 9f, "Authoritative v0.10.9 width. Fresh key prevents the previous narrower config from overriding the 1.5x width specification.");
            _moonSpeed = Config.Bind("Sword Master Moonlight Splitter", "ProjectileSpeed", 20f, "Visible Ghost laser travel speed in literal meters per second.");
            _moonDamage = BindDamage("Sword Master Moonlight Damage", 0f, 34f, 0f, 0f, 0f, 0f, 0f, 24f);
            _moonSpiritDot = Config.Bind("Sword Master Moonlight Splitter", "LegacySpiritDotPerSecond", 0f, "Framework says Moonlight Splitter deals Spirit damage WITHOUT Spirit Burn. Kept only for old config compatibility.");
            _moonSpiritDuration = Config.Bind("Sword Master Moonlight Splitter", "LegacySpiritDotDuration", 0f, "Unused in v0.10.0.");

            _crescentCooldown = Config.Bind("Sword Master Crescent Cleave", "Cooldown", 14f, "Seconds.");
            _crescentStamina = Config.Bind("Sword Master Crescent Cleave", "StaminaCost", 30f, "Stamina cost.");
            _crescentRange = Config.Bind("Sword Master Crescent Cleave", "RangeMeters_v0109", 20f, "Authoritative v0.10.9 travel distance. Fresh key prevents old 15m configs from overriding the 20m specification.");
            _crescentTravelTime = Config.Bind("Sword Master Crescent Cleave", "TravelTimeSeconds_v0109", 4f, "Authoritative v0.10.9 travel time. 20m over 4s preserves the previous 5m-per-second wave speed.");
            _crescentSlashWidth = Config.Bind("Sword Master Crescent Cleave", "SlashWidth", 1f, "Width in meters of EACH vertical travelling slash hitbox.");
            _crescentSlashHeight = Config.Bind("Sword Master Crescent Cleave", "SlashHeight", 6.4f, "Doubled vertical cleave height.");
            _crescentSpreadAngle = Config.Bind("Sword Master Crescent Cleave", "ConeSpreadDegrees_v0109", 120f, "Very wide cone matching the supplied second-cone reference: 120 degrees total spread, with the center slash travelling straight ahead.");
            _crescentPersistentTick = Config.Bind("Sword Master Crescent Cleave", "PersistentHitInterval", 0.5f, "Persistent Hitbox: enemies still inside the cleave can be damaged again every 0.5s.");
            _crescentDamage = BindDamage("Sword Master Crescent Cleave Damage", 0f, 28f, 0f, 0f, 0f, 0f, 0f, 20f);

            _judgementCooldown = Config.Bind("Sword Master Judgement Cut", "RechargeSecondsPerStack", 12f, "Independent recharge time for each of the four stacks.");
            _judgementStamina = Config.Bind("Sword Master Judgement Cut", "StaminaCost", 18f, "Stamina cost per stack activation.");
            _judgementRange = Config.Bind("Sword Master Judgement Cut", "CastRange", 15f, "Maximum Ground PAC / Target PAC / Free Aim cast range.");
            _judgementRadius = Config.Bind("Sword Master Judgement Cut", "Radius", 4f, "Sphere radius around the chosen cast point.");
            _judgementSlashDamage = Config.Bind("Sword Master Judgement Cut", "SlashDamagePerCut", 24f, "Pure Slash damage dealt by each of the three cuts. No Spirit, DoT or debuff.");
            _judgementBuffer = Config.Bind("Sword Master Judgement Cut", "ActivationBufferSeconds", 0.5f, "Minimum delay between charge activations. Independent stack recharge remains 12 seconds per spent stack.");

            _severedCooldown = Config.Bind("Sword Master Severed Horizon", "Cooldown", 20f, "Seconds.");
            _severedStamina = Config.Bind("Sword Master Severed Horizon", "StaminaCost", 36f, "Stamina cost.");
            _severedRange = Config.Bind("Sword Master Severed Horizon", "Range", 30f, "Length of the visible world-cut line.");
            _severedWidth = Config.Bind("Sword Master Severed Horizon", "Width", 1.5f, "Damage width around the world-cut line.");
            _severedDelay = Config.Bind("Sword Master Severed Horizon", "TearDelay", 0.65f, "Delay between drawing the line and the full-line tear.");
            _severedDamage = BindDamage("Sword Master Severed Horizon Damage", 0f, 82f, 0f, 0f, 0f, 0f, 0f, 0f);

            _emptyCooldown = Config.Bind("Sword Master Empty Sheath", "Cooldown", 18f, "Seconds.");
            _emptyStamina = Config.Bind("Sword Master Empty Sheath", "StaminaCost", 24f, "Stamina cost.");
            _emptyCounterWindow = Config.Bind("Sword Master Empty Sheath", "CounterWindow", 0.8f, "Brief counter stance duration.");
            _emptyBehindDistance = Config.Bind("Sword Master Empty Sheath", "BehindDistance", 1.6f, "Distance behind the attacker after a successful counter.");
            _emptyCutCount = Config.Bind("Sword Master Empty Sheath", "RetaliationCuts", 4, "Rapid delayed cuts after a successful counter.");
            _emptyCutInterval = Config.Bind("Sword Master Empty Sheath", "RetaliationCutInterval", 0.10f, "Interval between retaliation cuts.");
            _emptySlashDamage = Config.Bind("Sword Master Empty Sheath", "SlashDamagePerCut", 34f, "Pure Slash damage per retaliation cut.");

            _halfmoonCooldown = Config.Bind("Sword Master Halfmoon Slash", "Cooldown", 40f, "Seconds.");
            _halfmoonStamina = Config.Bind("Sword Master Halfmoon Slash", "StaminaCost", 50f, "Stamina cost.");
            _halfmoonRadius = Config.Bind("Sword Master Halfmoon Slash", "Radius", 22f, "Expanded frontal slash radius.");
            _halfmoonDamage = BindDamage("Sword Master Halfmoon Damage v2", 0f, 130f, 0f, 0f, 0f, 0f, 0f, 60f);
            _halfmoonSpiritDot = Config.Bind("Sword Master Halfmoon Slash", "SpiritDotPerSecond", 18f, "Ultimate-strength Spirit Burn damage per second.");
            _halfmoonSpiritDuration = Config.Bind("Sword Master Halfmoon Slash", "SpiritDotDuration", 10f, "Framework Spirit Burn duration.");
            _halfmoonSecondSlashDelay = Config.Bind("Sword Master Halfmoon Slash", "SecondSlashDelay", 0.65f, "Delay in seconds between the primary slash and the 0.5x afterimage slash.");

            _swordSkillBonus = Config.Bind("Sword Master Passive", "LegacySwordSkillBonus", 0f, "Legacy Sword-skill bonus retained for config compatibility. The Way of the Sword no longer grants flat skill levels.");
            _swordAttackSpeedPassive = Config.Bind("Sword Master Passive", "WayOfTheSwordAttackSpeedPercent_v0123", 100f, "The Way of the Sword: +100% Attack Speed while exactly one Sword is equipped and the off-hand is empty.");
            _swordAttackSpeedActive = Config.Bind("Sword Master Passive", "LegacyActiveAttackSpeedPercent", 0f, "Legacy Sword Mastery active setting. The Way of the Sword is passive-only.");
            _swordActiveDuration = Config.Bind("Sword Master Passive", "LegacyActiveDuration", 0f, "Legacy setting. Unused.");
            _swordActiveCooldown = Config.Bind("Sword Master Passive", "LegacyActiveCooldown", 0f, "Legacy setting. Unused.");

            // v0.10.0 migration from all known previous Sword Master defaults.
            if (Mathf.Approximately(_swordAttackSpeedPassive.Value, 15f) ||
                Mathf.Approximately(_swordAttackSpeedPassive.Value, 50f))
                _swordAttackSpeedPassive.Value = 75f;

            if (Mathf.Approximately(_swordAttackSpeedActive.Value, 25f) ||
                Mathf.Approximately(_swordAttackSpeedActive.Value, 50f))
                _swordAttackSpeedActive.Value = 100f;

            if (Mathf.Approximately(_swordActiveDuration.Value, 15f))
                _swordActiveDuration.Value = 10f;

            if (Mathf.Approximately(_halfmoonSpiritDot.Value, 12f))
                _halfmoonSpiritDot.Value = 18f;

            MigrateFloat(_rayCooldown, 24f, 30f);
            MigrateFloat(_rayHealPercent, 80f, 30f);
            MigrateFloat(_rayBuffDuration, 60f, 12f);

            _stompCooldown = Config.Bind("Mercenary Stomp", "Cooldown", 10f, "Seconds.");
            _stompStamina = Config.Bind("Mercenary Stomp", "StaminaCost", 25f, "Stamina cost.");
            _stompWindup = Config.Bind("Mercenary Stomp", "Windup", 0.5f, "First stomp wind-up. The full earthquake sequence lasts 1.5s.");
            _stompRadius = Config.Bind("Mercenary Stomp", "Radius", 3f, "First stomp radius: true 3m center-to-edge.");
            _stompAftershockDelay = Config.Bind("Mercenary Stomp", "AftershockDelay", 1f, "Seconds after the first impact before the aftershock.");
            _stompAftershockRadius = Config.Bind("Mercenary Stomp", "AftershockRadius", 10f, "Aftershock radius: true 10m center-to-edge.");
            _stompDamage = BindDamage("Mercenary Stomp Damage", 42f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            _boneCooldown = Config.Bind("Mercenary Bonecrusher", "Cooldown", 16f, "Seconds.");
            _boneStamina = Config.Bind("Mercenary Bonecrusher", "StaminaCost", 34f, "Stamina cost.");
            _boneWindup = Config.Bind("Mercenary Bonecrusher", "Windup", 2f, "Target flat-ground air sequence: about 2 seconds from takeoff to landing. Cliff falls extend until physical landing.");
            _boneRadius = Config.Bind("Mercenary Bonecrusher", "Radius", 10f, "Framework AoE radius: literal 10m.");
            _boneDamage = BindDamage("Mercenary Bonecrusher Damage", 70f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            _circleCooldown = Config.Bind("Mercenary Circle Swing", "Cooldown", 16f, "Seconds.");
            _circleStamina = Config.Bind("Mercenary Circle Swing", "StaminaCost", 36f, "Stamina cost.");
            _circleWindup = Config.Bind("Mercenary Circle Swing", "Windup", 1.5f, "Heavy steerable wind-up before the violent circular swing.");
            _circleRadius = Config.Bind("Mercenary Circle Swing", "Radius", 7f, "True 7m center-to-edge radius.");
            _circleDamageMultiplier = Config.Bind("Mercenary Circle Swing", "WeaponDamageMultiplier", 1.75f, "Significant burst: multiplier applied to the held weapon damage/elements.");
            _circleWindupTravel = Config.Bind("Mercenary Circle Swing", "WindupTravel", 0.5f, "Maximum steerable movement distance during the wind-up.");

            _seismicCooldown = Config.Bind("Mercenary Seismic Guillotine", "Cooldown", 18f, "Prototype cooldown; developer tunable.");
            _seismicStamina = Config.Bind("Mercenary Seismic Guillotine", "StaminaCost", 38f, "Prototype stamina cost; developer tunable.");
            _seismicRange = Config.Bind("Mercenary Seismic Guillotine", "Range", 15f, "Maximum Ground PAC / Target PAC distance. The fissure stops at the aimed point when it is closer than max range.");
            _seismicWidth = Config.Bind("Mercenary Seismic Guillotine", "Width", 6f, "Base fissure width. Unchained Fury widens and branches it.");
            _seismicTravelTime = Config.Bind("Mercenary Seismic Guillotine", "TravelTime", 1.0f, "Time for Seismic Shocks to cover the full configured range. Default: 15m in 1.0s; shorter casts preserve the same travel speed.");
            _seismicEndRadius = Config.Bind("Mercenary Seismic Guillotine", "EndRuptureRadius", 10f, "Finishing rupture radius.");
            _seismicDamageMultiplier = Config.Bind("Mercenary Seismic Guillotine", "WeaponDamageMultiplier", 1.20f, "Prototype held-weapon damage multiplier for fissure and finishing rupture.");

            _reaverCooldown = Config.Bind("Mercenary Reavers Orbit", "Cooldown", 16f, "Prototype cooldown; developer tunable.");
            _reaverStamina = Config.Bind("Mercenary Reavers Orbit", "StaminaCost", 32f, "Prototype stamina cost; developer tunable.");
            _reaverRadius = Config.Bind("Mercenary Reavers Orbit", "OrbitRadius", 8f, "Maximum outward orbit radius.");
            _reaverDuration = Config.Bind("Mercenary Reavers Orbit", "Duration", 1.6f, "Full outward-and-return orbit duration.");
            _reaverHitRadius = Config.Bind("Mercenary Reavers Orbit", "HitRadius", 1.35f, "Hit radius around each travelling axe point.");
            _reaverDamageMultiplier = Config.Bind("Mercenary Reavers Orbit", "WeaponDamageMultiplier", 0.80f, "Prototype held-weapon multiplier per outward/return pass.");

            _whirlwindCooldown = Config.Bind("Mercenary Whirlwind", "Cooldown", 40f, "Seconds.");
            _whirlwindStamina = Config.Bind("Mercenary Whirlwind", "StaminaCost", 55f, "Stamina cost.");
            _whirlwindRadius = Config.Bind("Mercenary Whirlwind", "Radius", 2f, "Framework Whirlwind radius: literal 2m.");
            _whirlwindDuration = Config.Bind("Mercenary Whirlwind", "Duration", 6f, "Framework duration.");
            _whirlwindInterval = Config.Bind("Mercenary Whirlwind", "HitInterval", 0.5f, "Framework hit interval.");
            _whirlwindWeaponMultiplier = Config.Bind("Mercenary Whirlwind", "WeaponDamageMultiplier", 0.5f, "Framework: each tick is half a normal held-weapon attack.");

            _mercAxesBonus = Config.Bind("Mercenary Passive", "AxesSkillBonus", 20f, "Effective Axes skill bonus.");
            _mercAttackDamage = Config.Bind("Mercenary Passive", "AttackDamagePercent", 8f, "Weapon attack damage bonus.");
            _mercHealthBonus = Config.Bind("Mercenary Passive", "FlatHealthBonus", 50f, "Flat maximum health bonus.");
            _mercAggroRadius = Config.Bind("Mercenary Passive", "AggroRadius", 20f, "Nearby enemies are encouraged to target the Mercenary.");
            _mercTauntRadius = Config.Bind("Mercenary Passive", "TauntRadius", 15f, "Barbaric active taunt radius in meters.");
            _mercTauntDuration = Config.Bind("Mercenary Passive", "TauntDuration", 6f, "Barbaric active taunt duration in seconds.");
            _mercExposeDuration = Config.Bind("Mercenary Passive", "TauntExposeDuration", 10f, "Expose duration for enemies hit by Barbaric.");
            _mercTauntCooldown = Config.Bind("Mercenary Passive", "TauntCooldown", 20f, "Barbaric activation cooldown; testing override still applies.");
            _mercFuryGainPerWeaponHit = Config.Bind("Mercenary Unchained Fury", "FuryGainPerWeaponHit", 1f, "Fury gained per successful normal melee hit while Fury is ready.");
            _mercFuryGainPerSkillTarget = Config.Bind("Mercenary Unchained Fury", "FuryGainPerSkillTarget", 3f, "Fury gained for each enemy hit by a Mercenary skill. Multi-target skills gain this amount once per actual target hit.");
            _mercFuryDuration = Config.Bind("Mercenary Unchained Fury", "Duration", 10f, "Unchained Fury active duration.");
            _mercFuryCooldown = Config.Bind("Mercenary Unchained Fury", "Cooldown", 300f, "Lockout that begins after Unchained Fury ends. Fury cannot build during this lockout.");
            _mercFuryBoneConeRange = Config.Bind("Mercenary Unchained Fury", "BonecrusherConeRange", 12f, "Ascension-style forward shockwave range added to Bonecrusher during Fury.");
            _mercFuryBoneConeAngle = Config.Bind("Mercenary Unchained Fury", "BonecrusherConeAngle", 100f, "Ascension-style Bonecrusher shockwave cone angle during Fury.");
            _mercFuryBoneConeMultiplier = Config.Bind("Mercenary Unchained Fury", "BonecrusherConeDamageMultiplier", 0.75f, "Temporary Fury shockwave damage multiplier relative to Bonecrusher damage.");

            // v0.11.7 migration: existing BepInEx configs keep old values unless explicitly moved.
            // Only migrate the exact v0.11.6 defaults so deliberate custom tuning is preserved.
            if (Mathf.Approximately(_boneRadius.Value, 5f)) _boneRadius.Value = 10f;
            if (Mathf.Approximately(_seismicRange.Value, 18f)) _seismicRange.Value = 15f;
            if (Mathf.Approximately(_seismicWidth.Value, 3f)) _seismicWidth.Value = 6f;
            if (Mathf.Approximately(_seismicEndRadius.Value, 4f)) _seismicEndRadius.Value = 10f;
            if (Mathf.Approximately(_mercFuryGainPerWeaponHit.Value, 5f)) _mercFuryGainPerWeaponHit.Value = 1f;

            // v0.11.8 migration: preserve the new constant Seismic Shock travel speed for existing v0.11.7 configs.
            if (Mathf.Approximately(_seismicTravelTime.Value, 1.6f)) _seismicTravelTime.Value = 1.0f;

            _goddessCooldown = Config.Bind("Paladin Goddess Relic", "Cooldown", 14f, "Seconds.");
            _goddessStamina = Config.Bind("Paladin Goddess Relic", "StaminaCost", 30f, "Stamina cost.");
            _goddessRadius = Config.Bind("Paladin Goddess Relic", "Radius", 7f, "Framework AoE radius: literal 7m. Cross visual size is unchanged.");
            _goddessRange = Config.Bind("Paladin Goddess Relic", "Range", 50f, "Ground PAC cast distance in literal meters. Works indoors.");
            _goddessDamage = BindDamage("Paladin Goddess Relic Damage", 38f, 0f, 0f, 0f, 0f, 42f, 0f, 32f);
            _goddessSpiritDot = Config.Bind("Paladin Goddess Relic", "SpiritDotPerSecond", 8f, "Spirit Burn damage per second.");
            _goddessSpiritDuration = Config.Bind("Paladin Goddess Relic", "SpiritDotDuration", 6f, "Default DoT duration.");
            _goddessWindup = Config.Bind("Paladin Goddess Relic", "Windup", 1f, "Framework windup.");

            _rayCooldown = Config.Bind("Paladin Ray of Hope", "Cooldown", 30f, "Seconds.");
            _rayStamina = Config.Bind("Paladin Ray of Hope", "StaminaCost", 35f, "Stamina cost.");
            _rayWindup = Config.Bind("Paladin Ray of Hope", "ChannelTime", 2f, "Channel time.");
            _rayHealPercent = Config.Bind("Paladin Ray of Hope", "HealPercent", 30f, "Heal 30 percent max HP.");
            _rayDamageBuff = Config.Bind("Paladin Ray of Hope", "AttackDamageBonusPercent", 30f, "Attack Damage Bonus.");
            _rayBuffDuration = Config.Bind("Paladin Ray of Hope", "BuffDuration", 12f, "Buff duration.");
            _rayRadius = Config.Bind("Paladin Ray of Hope", "Radius", 7f, "Wave radius.");
            _raySpiritBurnDuration = Config.Bind("Paladin Ray of Hope", "SpiritBurnDuration", 8f, "Enemy Spirit Burn duration.");
            _shieldChargeCooldown = Config.Bind("Paladin Shield Charge", "Cooldown", 18f, "Seconds.");
            _shieldChargeStamina = Config.Bind("Paladin Shield Charge", "StaminaCost", 28f, "Stamina cost.");
            _shieldChargeDistance = Config.Bind("Paladin Shield Charge", "Distance", 15f, "Literal 15m charge.");
            _shieldChargeSpeedMultiplier = Config.Bind("Paladin Shield Charge", "MovementSpeedMultiplier", 1.5f, "1.5x current run speed.");
            _shieldChargeRadius = Config.Bind("Paladin Shield Charge", "BashRadius", 4f, "4m frontal attack radius.");
            _shieldChargePersistentTick = Config.Bind("Paladin Shield Charge", "PersistentHitInterval", 0.5f, "Persistent Damage interval.");
            _shieldChargeDamage = BindDamage("Paladin Shield Charge Damage", 42f, 0f, 0f, 0f, 0f, 28f, 0f, 0f);

            _verdictCooldown = Config.Bind("Paladin Divine Verdict", "Cooldown", 22f, "Seconds.");
            _verdictStamina = Config.Bind("Paladin Divine Verdict", "StaminaCost", 38f, "Stamina cost.");
            _verdictRange = Config.Bind("Paladin Divine Verdict", "Range", 35f, "Ground PAC / Target PAC range.");
            _verdictRadius = Config.Bind("Paladin Divine Verdict", "Radius", 8f, "Colossal holy hammer impact radius.");
            _verdictWindup = Config.Bind("Paladin Divine Verdict", "Windup", 1f, "Sky-summon windup before the hammer falls.");
            _verdictDamage = BindDamage("Paladin Divine Verdict Damage", 85f, 0f, 0f, 0f, 0f, 30f, 0f, 50f);

            _aegisCooldown = Config.Bind("Paladin Aegis Fall", "Cooldown", 24f, "Seconds.");
            _aegisStamina = Config.Bind("Paladin Aegis Fall", "StaminaCost", 42f, "Stamina cost.");
            _aegisRange = Config.Bind("Paladin Aegis Fall", "Range", 35f, "Ground PAC / Target PAC range.");
            _aegisWidth = Config.Bind("Paladin Aegis Fall", "WallWidth", 7f, "Physical shield-wall width.");
            _aegisHeight = Config.Bind("Paladin Aegis Fall", "WallHeight", 5f, "Physical shield-wall height.");
            _aegisDuration = Config.Bind("Paladin Aegis Fall", "WallDuration", 12f, "Seconds the Physical Aegis remains if not shattered.");
            _aegisWindup = Config.Bind("Paladin Aegis Fall", "Windup", 1f, "Sky-summon windup before the shield falls.");
            _aegisImpactRadius = Config.Bind("Paladin Aegis Fall", "ImpactRadius", 5f, "Damage radius when the Aegis lands.");
            _aegisShockwaveRadius = Config.Bind("Paladin Aegis Fall", "ShatterShockwaveRadius", 10f, "Holy shockwave radius when Shield Charge shatters your own Aegis.");
            _aegisDamage = BindDamage("Paladin Aegis Fall Damage", 62f, 0f, 0f, 0f, 0f, 20f, 0f, 32f);
            _aegisShockwaveDamage = BindDamage("Paladin Aegis Shatter Damage", 48f, 0f, 0f, 0f, 0f, 36f, 0f, 42f);

            _judgementMarkDuration = Config.Bind("Paladin Judgement Mark", "Duration", 8f, "How long a Lightning Zap or Goddess Relic Judgement Mark remains.");
            _judgementMarkedMultiplier = Config.Bind("Paladin Judgement Mark", "MarkedHitMultiplier", 1.5f, "Damage multiplier when hitting a marked enemy.");
            _judgementCrossMultiplier = Config.Bind("Paladin Judgement Mark", "CrossMarkMultiplier", 2f, "Damage multiplier when the opposite marking source hits a marked enemy.");
            _judgementDetonationLightning = Config.Bind("Paladin Judgement Mark", "DetonationLightningDamage", 35f, "Bonus Lightning damage from Judgement Detonation.");
            _judgementDetonationSpirit = Config.Bind("Paladin Judgement Mark", "DetonationSpiritDamage", 35f, "Bonus Spirit damage from Judgement Detonation.");
            _judgementCrippleDuration = Config.Bind("Paladin Judgement Mark", "CrippleDuration", 6f, "Cripple duration caused by a marked non-Lightning hit.");

            _divineCooldown = Config.Bind("Paladin Electric Smite", "Cooldown", 45f, "Seconds.");
            _divineStamina = Config.Bind("Paladin Electric Smite", "StaminaCost", 55f, "Stamina cost.");
            _divineRadius = Config.Bind("Paladin Electric Smite", "Radius", 5f, "Framework impact radius: literal 5m.");
            _divineDamage = BindDamage("Paladin Electric Smite Damage v2", 90f, 0f, 0f, 40f, 0f, 100f, 0f, 70f);
            _divineFireDot = Config.Bind("Paladin Electric Smite", "FireDotPerSecond", 6f, "Fire Burn damage per second.");
            _divineSpiritDot = Config.Bind("Paladin Electric Smite", "SpiritDotPerSecond", 9f, "Spirit Burn damage per second.");
            _divineSpiritDuration = Config.Bind("Paladin Electric Smite", "SpiritDotDuration", 6f, "Default DoT duration.");
            _divineWindup = Config.Bind("Paladin Electric Smite", "Windup", 2f, "Target flat-ground air sequence: about 2 seconds from takeoff to landing. Cliff falls extend until physical landing.");
            _divineTrailRange = Config.Bind("Paladin Electric Smite", "TrailRangeMeters_v0109", 10f, "Sixteen Ground Projectile trails spread in all directions for 10m.");
            _divineTrailTravelTime = Config.Bind("Paladin Electric Smite", "TrailTravelTimeSeconds_v0109", 3f, "Ground Projectile travel time to the full 10m radius.");
            _divineTrailPersistentTick = Config.Bind("Paladin Electric Smite", "TrailPersistentHitInterval", 0.5f, "Persistent Damage interval while an enemy remains in any Electric Smite trail.");
            _divineTrailDamage = BindDamage("Paladin Electric Smite Trail Damage v0109", 0f, 0f, 0f, 0f, 0f, 18f, 0f, 0f);
            _divineZapDamage = Config.Bind("Paladin Electric Smite", "ZapExplosionLightningDamage", 0f, "0 uses Combat Runtime Zap default.");

            // v0.17.0: temporary Ascended test switch + Paladin rework.
            _testingAscendedSkills = Config.Bind("Testing", "AscendedSkills", "", "Temporary until the Ascension system exists: comma list of skill ids treated as Ascended. Paladin: righteous_strike, goddess_relic, judgement_hammer, shield_charge, fallen_angel, ray_of_hope, electric_smite.");

            _goddessRadiusV17 = Config.Bind("Paladin Goddess Relic", "Radius_v017", 5f, "Damage radius in meters.");
            _goddessCrossHeight = Config.Bind("Paladin Goddess Relic", "CrossHeight_v0171", 6.5f, "Cross height in meters (about a 0-star Troll).");
            _goddessCrossWidth = Config.Bind("Paladin Goddess Relic", "CrossWidth_v0171", 3.5f, "Cross arm width in meters.");
            _goddessAscRadius = Config.Bind("Paladin Goddess Relic Ascended", "Radius", 10f, "Ascended damage radius.");
            _goddessAscSizeMultiplier = Config.Bind("Paladin Goddess Relic Ascended", "CrossSizeMultiplier_v0171", 2.12f, "Ascended cross size compared to the normal cross (2.12 keeps the approved 13.8m Ascended cross).");
            _goddessAscDamage = BindDamage("Paladin Goddess Relic Ascended Damage", 120f, 0f, 0f, 0f, 0f, 60f, 0f, 0f);

            _rayAscBarrier = Config.Bind("Paladin Ray of Hope Ascended", "BarrierHP", 150f, "Barrier HP granted to allies in the wave.");
            _rayAscBarrierDuration = Config.Bind("Paladin Ray of Hope Ascended", "BarrierDuration", 12f, "Barrier duration.");

            _hammerCooldown = Config.Bind("Paladin Judgement Hammer", "Cooldown", 16f, "Seconds.");
            _hammerStamina = Config.Bind("Paladin Judgement Hammer", "StaminaCost", 32f, "Stamina cost.");
            _hammerWindup = Config.Bind("Paladin Judgement Hammer", "Windup", 1f, "Framework default wind-up (no wind-up specified).");
            _hammerRange = Config.Bind("Paladin Judgement Hammer", "Range", 20f, "Free Aim Laser Projectile range.");
            _hammerTravelTime = Config.Bind("Paladin Judgement Hammer", "TravelTime", 1.5f, "Seconds to travel the full range.");
            _hammerBaseRadius = Config.Bind("Paladin Judgement Hammer", "BaseHitRadius", 0.9f, "Minimum hit radius.");
            _hammerStepMeters = Config.Bind("Paladin Judgement Hammer", "GrowthStepMeters", 0.5f, "Every this many meters travelled, damage grows (stops when the size cap is reached).");
            _hammerStartHeight = Config.Bind("Paladin Judgement Hammer", "StartHeight_v0172", 2.5f, "Meters tall at launch (about a Greydwarf Brute).");
            _hammerStartWidth = Config.Bind("Paladin Judgement Hammer", "StartWidth_v0172", 0.8f, "Meters wide at launch.");
            _hammerGrowthInterval = Config.Bind("Paladin Judgement Hammer", "GrowthInterval_v0172", 0.2f, "Seconds between size growth steps.");
            _hammerHeightPerStep = Config.Bind("Paladin Judgement Hammer", "HeightPerStep_v0172", 0.7f, "Meters of height per growth step (20m flight = ~7.4m tall).");
            _hammerWidthPerStep = Config.Bind("Paladin Judgement Hammer", "WidthPerStep_v0172", 0.17f, "Meters of width per growth step (20m flight = ~2m wide).");
            _hammerMaxHeight = Config.Bind("Paladin Judgement Hammer", "LegacyMaxHeight_v0172", 8f, "Size cap (meters tall).");
            _hammerMaxWidth = Config.Bind("Paladin Judgement Hammer", "LegacyMaxWidth_v0172", 2f, "Size cap (meters wide).");
            _hammerHitRadiusPerHeight = Config.Bind("Paladin Judgement Hammer", "HitRadiusPerHeight_v0172", 0.35f, "Hit radius = height x this (never below BaseHitRadius).");
            _hammerAscMaxHeight = Config.Bind("Paladin Judgement Hammer Ascended", "LegacyMaxHeight_v0172", 10f, "Ascended size cap (meters tall). Keeps growing on the return flight up to this.");
            _hammerAscMaxWidth = Config.Bind("Paladin Judgement Hammer Ascended", "LegacyMaxWidth_v0172", 3f, "Ascended size cap (meters wide).");
            _hammerAscWidthPerStep = Config.Bind("Paladin Judgement Hammer Ascended", "WidthPerStep_v0172", 0.2f, "Ascended meters of width per growth step.");
            _hammerDamageCap = Config.Bind("Paladin Judgement Hammer", "MaxDamageMultiplier_v0212", 5f, "Damage growth stops at this multiplier. The hammer itself keeps growing (no size cap).");
            _hammerDamagePerStep = Config.Bind("Paladin Judgement Hammer", "DamageGrowthPerStep", 0.3f, "+0.3x damage per step (Framework doc).");
            _hammerTick = Config.Bind("Paladin Judgement Hammer", "PersistentHitInterval", 0.5f, "Persistent Damage interval per enemy.");
            _hammerCrippleDuration = Config.Bind("Paladin Judgement Hammer", "CrippleDuration", 6f, "Cripple duration.");
            _hammerCatchCooldownCut = Config.Bind("Paladin Judgement Hammer Ascended", "CatchCooldownCutPercent", 30f, "Catching the returning hammer cuts its cooldown by this percent.");
            _hammerDamage = BindDamage("Paladin Judgement Hammer Damage", 40f, 0f, 0f, 0f, 0f, 22f, 0f, 0f);

            _angelCooldown = Config.Bind("Paladin Fallen Angel", "Cooldown", 20f, "Seconds.");
            _angelStamina = Config.Bind("Paladin Fallen Angel", "StaminaCost", 40f, "Stamina cost.");
            _angelJumpHeight = Config.Bind("Paladin Fallen Angel", "JumpHeight", 7f, "Leap height in meters.");
            _angelRiseTime = Config.Bind("Paladin Fallen Angel", "RiseTime", 0.55f, "Seconds to reach the top of the leap.");
            _angelDiveSpeed = Config.Bind("Paladin Fallen Angel", "DiveSpeed", 28f, "Downward speed of the head-first dive.");
            _angelRadius = Config.Bind("Paladin Fallen Angel", "ImpactRadius", 10f, "Landing impact radius.");
            _angelBrokenBones = Config.Bind("Paladin Fallen Angel", "BrokenBonesDuration", 6f, "Broken Bones duration.");
            _angelDamage = BindDamage("Paladin Fallen Angel Damage", 95f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            _angelRingRadius = Config.Bind("Paladin Fallen Angel Ascended", "BurnRingRadius", 10f, "Radius of the burning ring left on impact.");
            _angelRingDuration = Config.Bind("Paladin Fallen Angel Ascended", "BurnRingDuration", 6f, "How long the ring lasts.");
            _angelBurnDuration = Config.Bind("Paladin Fallen Angel Ascended", "BurnDuration", 3f, "Spirit Burn + Fire Burn duration, refreshed while inside.");
            _angelFireDot = Config.Bind("Paladin Fallen Angel Ascended", "FireDotPerSecond", 6f, "Fire Burn damage per second.");
            _angelSpiritDot = Config.Bind("Paladin Fallen Angel Ascended", "SpiritDotPerSecond", 6f, "Spirit Burn damage per second.");
            _angelHyperAfter = Config.Bind("Paladin Fallen Angel Ascended", "HyperArmorAfterLanding", 3f, "Hyper Armor kept after landing.");

            _chargeAscDistance = Config.Bind("Paladin Shield Charge Ascended", "Distance", 20f, "Charge budget in meters.");
            _chargeAscHitRadius = Config.Bind("Paladin Shield Charge Ascended", "ChargeHitRadius", 6f, "Charge hitbox radius.");
            _chargeAscBashRadius = Config.Bind("Paladin Shield Charge Ascended", "BashConeRadius", 7f, "Bash cone length.");
            _chargeAscBashAngle = Config.Bind("Paladin Shield Charge Ascended", "BashConeAngle", 120f, "Bash cone total angle in degrees.");

            _smiteStormRadius = Config.Bind("Paladin Electric Smite Ascended", "ThunderstormRadius", 6f, "Thunderstorm radius.");
            _smiteStormDuration = Config.Bind("Paladin Electric Smite Ascended", "ThunderstormDuration", 4f, "Thunderstorm duration.");
            _smiteStormTick = Config.Bind("Paladin Electric Smite Ascended", "ThunderstormHitInterval", 0.5f, "Seconds between Thunderstorm hits.");
            _smiteStormDotDuration = Config.Bind("Paladin Electric Smite Ascended", "DotDuration", 3f, "Fire + Spirit DoT duration, refreshed every hit.");
            _smiteStormFireDot = Config.Bind("Paladin Electric Smite Ascended", "FireDotPerSecond", 5f, "Fire DoT per second.");
            _smiteStormSpiritDot = Config.Bind("Paladin Electric Smite Ascended", "SpiritDotPerSecond", 5f, "Spirit DoT per second.");
            _smiteStormDamage = BindDamage("Paladin Electric Smite Thunderstorm Damage", 0f, 0f, 0f, 0f, 0f, 22f, 0f, 0f);

            _rsAscCooldown = Config.Bind("Paladin Righteous Strike Ascended", "Cooldown", 8f, "Seconds.");
            _rsAscStamina = Config.Bind("Paladin Righteous Strike Ascended", "StaminaCost", 20f, "Stamina cost.");
            _rsAscWindup = Config.Bind("Paladin Righteous Strike Ascended", "Windup", 0.7f, "Wind-up.");
            _rsAscRange = Config.Bind("Paladin Righteous Strike Ascended", "Range", 50f, "Ground PAC range.");
            _rsAscRadius = Config.Bind("Paladin Righteous Strike Ascended", "Radius", 7f, "Strike radius.");
            _rsAscExpose = Config.Bind("Paladin Righteous Strike Ascended", "ExposeDuration", 8f, "Expose duration.");
            _rsAscFollowRadius = Config.Bind("Paladin Righteous Strike Ascended", "FollowUpRadius", 3f, "Second strike radius after a Mark detonation.");
            _rsAscFollowMultiplier = Config.Bind("Paladin Righteous Strike Ascended", "FollowUpDamageMultiplier", 0.5f, "Second strike damage multiplier.");
            _rsAscTrailRange = Config.Bind("Paladin Righteous Strike Ascended", "TrailRange", 7f, "Lightning Trail length.");
            _rsAscTrailTime = Config.Bind("Paladin Righteous Strike Ascended", "TrailTravelTime", 1f, "Seconds for a trail to reach full length (faster than Electric Smite).");
            _rsAscTrailTick = Config.Bind("Paladin Righteous Strike Ascended", "TrailHitInterval", 0.5f, "Persistent hit interval inside trails.");
            _rsAscSpiritDot = Config.Bind("Paladin Righteous Strike Ascended", "SpiritDotPerSecond", 6f, "Trail Spirit DoT per second.");
            _rsAscSpiritDuration = Config.Bind("Paladin Righteous Strike Ascended", "SpiritDotDuration", 6f, "Trail Spirit DoT duration.");
            _rsAscDamage = BindDamage("Paladin Righteous Strike Ascended Damage", 40f, 0f, 0f, 0f, 0f, 48f, 0f, 0f);
            _rsAscTrailDamage = BindDamage("Paladin Righteous Strike Ascended Trail Damage", 0f, 0f, 0f, 0f, 0f, 10f, 0f, 0f);

            _paladinElementalBonus = Config.Bind("Paladin Passive - Elemental Savant", "ElementalDamagePercent", 25f, "+25% Fire/Frost/Lightning/Poison/Spirit damage.");
            _paladinElementalFlatEitr = Config.Bind("Paladin Passive - Elemental Savant", "FlatEitr", 30f, "+30 flat Max Eitr.");
            _paladinElementalEitrRegen = Config.Bind("Paladin Passive - Elemental Savant", "EitrRegenPercent", 30f, "+30% Eitr Regen.");
            _holyKnightMoveSpeed = Config.Bind("Paladin Passive - Holy Knight", "MoveSpeedPercent", 25f, "+25% Movement Speed.");
            _holyKnightFlatHealth = Config.Bind("Paladin Passive - Holy Knight", "FlatHealth", 35f, "+35 flat Max HP.");
            _holyKnightFlatStamina = Config.Bind("Paladin Passive - Holy Knight", "FlatStamina", 35f, "+35 flat Max Stamina.");
            _holyKnightRegen = Config.Bind("Paladin Passive - Holy Knight", "HealthStaminaRegenPercent", 30f, "+30% HP and Stamina Regen.");
            _holyKnightAttackSpeed = Config.Bind("Paladin Passive - Holy Knight", "WeaponShieldAttackSpeedPercent", 75f, "+75% Attack Speed while any weapon and any Shield are equipped together.");
            _mercTwoHandedAttackSpeed = Config.Bind("Mercenary Weapon Mastery - Warfreak", "TwoHandedAttackSpeedPercent", 125f, "+125% Attack Speed while wielding a two-handed weapon.");
            // v0.20.8: the permanent +30% Priest Armor passive is not in the Framework (Heaven's Crucible
            // snapshots 30% of the Priest's Armor onto its Barrier instead). Kept as a hidden Legacy value.
            _priestArmorBonusPercent = Config.Bind("Priest Grand Sigil", "LegacyCurrentArmorBonusPercent", 0f, "Legacy: retired permanent Priest Armor bonus.");
            _clericBlessingHealth = Config.Bind("Cleric Blessing", "FlatHealth", 35f, "Cleric's Blessing: flat Max HP.");
            _clericBlessingRegen = Config.Bind("Cleric Blessing", "HealthRegenPercent", 20f, "Cleric's Blessing: HP Regen bonus in percent.");
            _holyTrinityClubs = Config.Bind("Paladin Holy Trinity", "ClubsBonus", 15f, "Holy Trinity: Clubs skill bonus (effective skill capped at 100).");
            _holyTrinityMinPercent = Config.Bind("Paladin Holy Trinity", "SlashPierceMinPercentOfBlunt", 50f, "Holy Trinity: Slash and Pierce are each raised to at least this percent of the hit's Blunt damage (never lowered).");
            _crucibleArmorPercent = Config.Bind("Priest Grand Sigil", "BarrierArmorPercent", 30f, "Heaven's Crucible: Barrier Armor = this percent of the Priest's current Armor, snapshotted at cast.");
            // v0.20.9 Bless Thy Sinners - Buckler Parry (Framework).
            _shockwaveDamage = BindDamage("Priest Holy Shockwave Damage", 0f, 0f, 0f, 0f, 0f, 0f, 0f, 60f);
            _shockwaveRadius = Config.Bind("Priest Holy Shockwave", "Radius", 10f, "Buckler Parry: Holy Shockwave radius in meters.");
            _shockwaveCooldown = Config.Bind("Priest Holy Shockwave", "Cooldown", 15f, "Seconds between Holy Shockwaves.");
            _parryHyperArmorSeconds = Config.Bind("Priest Holy Shockwave", "HyperArmorSeconds", 5f, "Every Buckler Parry grants Hyper Armor for this long.");
            const string pa = "Priest Ascended";
            _sanctifiedDuration = Config.Bind(pa, "SanctifiedDuration", 10f, "Sanctified lasts this long on an ally.");
            _bloomHealPercent = Config.Bind(pa, "BloomHealPercent", 15f, "Bloom: instant heal, % of the ally's Max HP.");
            _bloomRadius = Config.Bind(pa, "BloomRadius", 4f, "Bloom: holy pulse radius around the ally.");
            _bloomDamage = BindDamage("Priest Sanctified Bloom Damage", 0f, 0f, 0f, 0f, 0f, 0f, 0f, 35f);
            _ahwRadius = Config.Bind("Priest Holy Wave Ascended", "Radius", 10f, "Ascended Holy Wave radius.");
            _ahwEchoDelay = Config.Bind("Priest Holy Wave Ascended", "EchoDelay", 2f, "Seconds before the echo wave.");
            _ahwEchoRadius = Config.Bind("Priest Holy Wave Ascended", "EchoRadius", 5f, "Echo wave radius.");
            _ahwEchoPercent = Config.Bind("Priest Holy Wave Ascended", "EchoHealPercent", 50f, "Echo heal, % of the first wave's heal.");
            _ahwLowHp = Config.Bind("Priest Holy Wave Ascended", "LowHealthPercent", 30f, "Allies below this % HP get double the instant heal.");
            _ahwAllyRange = Config.Bind("Priest Holy Wave Ascended", "AllyCastRange", 30f, "Aim at an ally within this range to cast the wave on them (no wind up).");
            _diHealAtMax = Config.Bind("Priest Divine Intervention", "HealPercentAtMaxTier", 50f, "Heal at Tier 5 (% Max HP); scales evenly from HealPercent at Tier 0.");
            _angelWindupTotal = Config.Bind("Paladin Fallen Angel", "WindUpTime_v0211", 2.5f, "Angel Comet: total seconds from the jump to the dive landing.");
            _relicAscRadius = Config.Bind("Priest Relics Ascended", "Radius", 14f, "Ascended Lightning / Holy Relic pulse radius.");
            _relicAscChainRange = Config.Bind("Priest Relics Ascended", "LightningChainRange", 6f, "Ascended Lightning Relic: each pulse arcs to up to 3 more enemies this far past its radius.");
            _relicAscBlastRadius = Config.Bind("Priest Relics Ascended", "LightningEndBlastRadius", 8f, "Ascended Lightning Relic: blast radius when the Cross ends.");
            _relicAscBlastDamage = BindDamage("Priest Lightning Relic Ascended Blast Damage", 0f, 0f, 0f, 0f, 0f, 60f, 0f, 60f);
            _holyRelicAscBuff = Config.Bind("Priest Relics Ascended", "HolyBuffPercent", 30f, "Ascended Holy Relic buff percent (was 20).");
            _holyRelicAscEndHeal = Config.Bind("Priest Relics Ascended", "HolyEndHealPercent", 25f, "Ascended Holy Relic: final heal, % Max HP, when the Cross ends.");
            _diAscBarrier = Config.Bind("Priest Divine Intervention Ascended", "BarrierHP", 250f, "Ascended Divine Intervention Barrier HP.");
            _crossAscWidth = Config.Bind("Priest Grand Cross Ascended", "Width", 20f, "Ascended Grand Cross width.");
            _crossAscRange = Config.Bind("Priest Grand Cross Ascended", "Range", 35f, "Ascended Grand Cross travel distance.");
            _crossAscBurstRadius = Config.Bind("Priest Grand Cross Ascended", "BurstRadius", 8f, "Ascended Grand Cross end burst radius.");
            _crossAscBurstDamage = BindDamage("Priest Grand Cross Ascended Burst Damage", 0f, 0f, 0f, 0f, 0f, 50f, 0f, 50f);
            _hjAscRadius = Config.Bind("Priest Heavens Judgement Ascended", "Radius", 14f, "Ascended Heaven's Judgement radius.");
            _hjAscDuration = Config.Bind("Priest Heavens Judgement Ascended", "BarrageDuration", 3f, "Ascended barrage duration.");
            _hjAscBeamHeal = Config.Bind("Priest Heavens Judgement Ascended", "BeamHealPercent", 3f, "Each beam heals allies in the circle, % Max HP.");
            _hjAscPillarRadius = Config.Bind("Priest Heavens Judgement Ascended", "PillarRadius", 4f, "Final Pillar of Heaven radius at the centre.");
            _pillarDamage = BindDamage("Priest Heavens Judgement Pillar Damage", 0f, 0f, 0f, 0f, 0f, 120f, 0f, 120f);
            _tempestAscRadius = Config.Bind("Priest Lightning Tempest Ascended", "Radius", 12f, "Ascended Tempest radius (follows the Priest).");
            _tempestAscDefense = Config.Bind("Priest Lightning Tempest Ascended", "AllyDefensePercent", 20f, "Allies inside: Overall Defense bonus (+ Hyper Armor).");
            _parryEmpowerPercent = Config.Bind("Priest Holy Shockwave", "NextSkillDamagePercent_v0212", 35f, "Every Buckler Parry empowers the next damaging skill by this percent for that entire skill instance.");

            _lightningRelicCooldown = Config.Bind("Priest Lightning Relic", "Cooldown", 14f, "Cooldown starts only after the active Relic is relinquished or its 16s lifetime ends.");
            _lightningRelicStamina = Config.Bind("Priest Lightning Relic", "StaminaCost", 30f, "Stamina cost.");
            _lightningRelicRadius = Config.Bind("Priest Lightning Relic", "Radius", 10f, "Pulse radius in literal meters.");
            _lightningRelicRange = Config.Bind("Priest Lightning Relic", "Range", 35f, "Ground PAC cast distance in literal meters. Works indoors.");
            _lightningRelicDuration = Config.Bind("Priest Lightning Relic", "Duration", 16f, "Active lifetime before cooldown begins.");
            _lightningRelicInterval = Config.Bind("Priest Lightning Relic", "HitInterval", 1f, "Pulse interval.");
            _lightningRelicDamage = BindDamage("Priest Lightning Relic Damage", 0f, 0f, 0f, 0f, 0f, 32f, 0f, 22f);
            _lightningRelicSpiritDot = Config.Bind("Priest Lightning Relic", "LegacySpiritDotPerSecond", 0f, "Lightning Relic has no Spirit DoT. Unused.");
            _lightningRelicSpiritDuration = Config.Bind("Priest Lightning Relic", "LegacySpiritDotDuration", 0f, "Unused.");

            _holyRelicCooldown = Config.Bind("Priest Holy Relic", "Cooldown", 18f, "Cooldown starts only after the active Relic is relinquished or its 16s lifetime ends.");
            _holyRelicStamina = Config.Bind("Priest Holy Relic", "StaminaCost", 40f, "Stamina cost.");
            _holyRelicRadius = Config.Bind("Priest Holy Relic", "Radius", 10f, "Pulse radius in literal meters.");
            _holyRelicRange = Config.Bind("Priest Holy Relic", "Range", 35f, "Ground PAC cast distance in literal meters.");
            _holyRelicDuration = Config.Bind("Priest Holy Relic", "Duration", 16f, "Active lifetime before cooldown begins.");
            _holyRelicInterval = Config.Bind("Priest Holy Relic", "PulseInterval", 2f, "Holy Relic keeps the existing 2s pulse interval.");
            _holyRelicHealPercent = Config.Bind("Priest Holy Relic", "HealPercentPerPulse_v0212", 4f, "Max-HP heal per pulse (each Tier adds +10% of it).");
            _holyRelicBuffDuration = Config.Bind("Priest Holy Relic", "BuffDuration", 4f, "Buff refresh duration.");
            _holyRelicDamageBuff = Config.Bind("Priest Holy Relic", "DamageBuffPercent", 20f, "Attack Damage Bonus.");
            _holyRelicAttackSpeedBuff = Config.Bind("Priest Holy Relic", "AttackSpeedPercent", 20f, "Attack Speed Bonus.");
            _holyRelicMoveSpeedBuff = Config.Bind("Priest Holy Relic", "MoveSpeedPercent", 20f, "Movement Speed Bonus.");
            _holyRelicRegenBuff = Config.Bind("Priest Holy Relic", "StaminaRegenPercent", 0f, "Not part of the current Holy Relic design; zero by default.");
            _holyRelicDefenseBuff = Config.Bind("Priest Holy Relic", "DefensePercent", 20f, "Overall Defense Bonus.");

            _consecratedConnectRange = Config.Bind("Priest Consecrated Ground", "ConnectionRange", 15f, "Lightning Relic and Holy Relic must be within this horizontal distance to connect.");
            _consecratedRadius = Config.Bind("Priest Consecrated Ground", "Radius", 15f, "Fixed Consecrated Ground radius regardless of how close the two Relics are.");
            _consecratedMultiplier = Config.Bind("Priest Consecrated Ground", "SignaturePotencyMultiplier", 1.25f, "Multiplier applied to Lightning Relic direct damage and Holy Relic heal/buffs inside Consecrated Ground.");
            _consecratedExposeDuration = Config.Bind("Priest Consecrated Ground", "ExposeDuration", 4f, "Expose duration applied by Lightning Relic inside Consecrated Ground.");

            _interventionCooldown = Config.Bind("Priest Divine Intervention", "Cooldown", 24f, "Seconds.");
            _interventionStamina = Config.Bind("Priest Divine Intervention", "StaminaCost", 45f, "Stamina cost.");
            _interventionRange = Config.Bind("Priest Divine Intervention", "CrossCastRange", 35f, "Maximum distance for selecting an active Priest Cross with the crosshair. If no Cross is selected, the skill self-casts.");
            _interventionRadius = Config.Bind("Priest Divine Intervention", "Radius", 10f, "Self/Cross-centered AoE radius, matching the intended Holy Wave-style cast behavior.");
            _interventionWindup = Config.Bind("Priest Divine Intervention", "Windup", 1f, "Short holy burst windup.");
            _interventionHealPercent = Config.Bind("Priest Divine Intervention", "HealPercent", 20f, "Max-HP heal.");
            _interventionBarrierHp = Config.Bind("Priest Divine Intervention", "BarrierHP", 150f, "Temporary barrier HP.");
            _interventionBarrierArmor = Config.Bind("Priest Divine Intervention", "BarrierArmorPercent", 30f, "Barrier Armor = this percent of the Priest's current Armor (same rule as Heaven's Crucible).");
            _interventionBuffDuration = Config.Bind("Priest Divine Intervention", "SupportDuration", 6f, "Hyper Armor / defense support duration.");
            _interventionExposeDuration = Config.Bind("Priest Divine Intervention", "ExposeDuration", 8f, "Expose duration on enemies hit.");
            _interventionDamage = BindDamage("Priest Divine Intervention Damage", 0f, 0f, 0f, 0f, 0f, 40f, 0f, 40f);

            _grandCrossCooldown = Config.Bind("Priest Grand Cross", "Cooldown", 24f, "Seconds.");
            _grandCrossStamina = Config.Bind("Priest Grand Cross", "StaminaCost", 40f, "Stamina cost.");
            _grandCrossWidth = Config.Bind("Priest Grand Cross", "Width", 15f, "Full width of the travelling X.");
            _grandCrossRange = Config.Bind("Priest Grand Cross", "Range", 25f, "Ghost projectile travel distance.");
            _grandCrossTravelTime = Config.Bind("Priest Grand Cross", "TravelTime", 6f, "Time to complete the full configured range.");
            _grandCrossWindup = Config.Bind("Priest Grand Cross", "Windup", 0.6f, "Two fast sword slashes form the travelling X.");
            _grandCrossTickInterval = Config.Bind("Priest Grand Cross", "PersistentHitInterval", 0.5f, "Persistent Damage interval while an enemy remains inside the travelling X.");
            _grandCrossSpiritDot = Config.Bind("Priest Grand Cross", "SpiritDotPerSecond", 7f, "Spirit Burn damage per second.");
            _grandCrossSpiritDuration = Config.Bind("Priest Grand Cross", "SpiritDotDuration", 4f, "Spirit Burn duration refreshed by Grand Cross.");
            _grandCrossDamage = BindDamage("Priest Grand Cross Damage", 0f, 0f, 0f, 0f, 0f, 30f, 0f, 26f);

            _heavensCooldown = Config.Bind("Priest Heavens Judgement", "Cooldown", 26f, "Seconds.");
            _heavensStamina = Config.Bind("Priest Heavens Judgement", "StaminaCost", 45f, "Stamina cost.");
            _heavensRadius = Config.Bind("Priest Heavens Judgement", "Radius", 10f, "Self/Cross-centered barrage radius.");
            _heavensWindup = Config.Bind("Priest Heavens Judgement", "Windup", 1.5f, "Ground-circle warning time before the holy barrage.");
            _heavensDuration = Config.Bind("Priest Heavens Judgement", "BarrageDuration", 1.5f, "Duration of the Holy Beam barrage.");
            _heavensStrikeInterval = Config.Bind("Priest Heavens Judgement", "StrikeInterval", 0.25f, "Spacing between Holy Beam waves.");
            _heavensStrikesPerWave = Config.Bind("Priest Heavens Judgement", "BeamsPerWave", 4, "Holy Beams per wave.");
            _heavensStrikeRadius = Config.Bind("Priest Heavens Judgement", "BeamImpactRadius", 1.8f, "Damage radius of each Holy Beam.");
            _heavensFrostDuration = Config.Bind("Priest Heavens Judgement", "FrostDuration", 4f, "Frost duration applied by every Holy Beam hit.");
            _heavensDamage = BindDamage("Priest Heavens Judgement Damage", 0f, 0f, 0f, 0f, 0f, 22f, 0f, 28f);

            _tempestCooldown = Config.Bind("Priest Lightning Tempest", "Cooldown", 45f, "Seconds.");
            _tempestStamina = Config.Bind("Priest Lightning Tempest", "StaminaCost", 60f, "Stamina cost.");
            _tempestRadius = Config.Bind("Priest Lightning Tempest", "Radius", 8f, "Framework storm radius: literal 8m.");
            _tempestRange = Config.Bind("Priest Lightning Tempest", "Range", 50f, "Ground PAC cast distance in literal meters.");
            _tempestDuration = Config.Bind("Priest Lightning Tempest", "Duration", 10f, "Framework duration.");
            _tempestStrikeInterval = Config.Bind("Priest Lightning Tempest", "StrikeInterval", 0.35f, "Testing/default spacing between strike batches.");
            _tempestMaxStrikes = Config.Bind("Priest Lightning Tempest", "MaxSimultaneousStrikes", 7, "Framework maximum simultaneous strikes.");
            _tempestZapDamage = Config.Bind("Priest Lightning Tempest", "ZapExplosionLightningDamage", 0f, "0 uses Combat Runtime Zap default.");
            _tempestDamage = BindDamage("Priest Lightning Tempest Damage v2", 0f, 0f, 0f, 0f, 0f, 18f, 0f, 0f);
            _tempestSpiritDot = Config.Bind("Priest Lightning Tempest", "SpiritDotPerSecond", 8f, "Refreshing Spirit Burn damage per second.");
            _tempestFireDot = Config.Bind("Priest Lightning Tempest", "FireDotPerSecond", 5f, "Refreshing Fire Burn damage per second.");
            _tempestFrostDuration = Config.Bind("Priest Lightning Tempest", "FrostDuration", 3f, "Frost duration refreshed by every strike.");
            _tempestFireDuration = Config.Bind("Priest Lightning Tempest", "FireDuration", 4f, "Fire Burn duration refreshed by every strike.");
            _tempestSpiritDuration = Config.Bind("Priest Lightning Tempest", "SpiritDuration", 4f, "Spirit Burn duration refreshed by every strike.");
            _tempestExposeDuration = Config.Bind("Priest Lightning Tempest", "ExposeDuration", 15f, "Expose duration refreshed by every strike.");

            _priestMartialSkillBonus = Config.Bind("Priest Grand Sigil", "MartialSkillBonus", 20f, "Effective Clubs/Maces skill bonus when Martial is chosen.");
            _priestElementalBonus = Config.Bind("Priest Grand Sigil", "ElementalDamagePercent", 20f, "Elemental magic damage bonus.");
            _grandProcChance = Config.Bind("Priest Grand Sigil", "LegacyPassiveBarrierProcChance", 0f, "Legacy v0.6 setting. Grand Sigil now uses the framework death-save behavior instead.");
            _grandProcReduction = Config.Bind("Priest Grand Sigil", "LegacyPassiveBarrierReductionPercent", 0f, "Legacy v0.6 setting. Unused.");
            // v0.20.8 Heaven's Crucible (Grace) per the Framework: 10m snapshot, 250 HP Barrier, 16s, 10 min, no cost.
            _grandCooldown = Config.Bind("Priest Grand Sigil", "ActiveCooldown_v0208", 600f, "Heaven's Crucible cooldown in seconds (10 min), starts on activation.");
            _grandStamina = Config.Bind("Priest Grand Sigil", "ActiveStaminaCost_v0208", 0f, "Graces cost no resources.");
            _grandRadius = Config.Bind("Priest Grand Sigil", "ActiveRadius_v0208", 10f, "Allies within this radius at cast get their own Barrier.");
            _grandBarrierHp = Config.Bind("Priest Grand Sigil", "BarrierHP_v0208", 250f, "Barrier hit points.");
            _grandBarrierDuration = Config.Bind("Priest Grand Sigil", "BarrierDuration_v0208", 16f, "Barrier lasts this long or until broken.");
            _grandWindup = Config.Bind("Priest Grand Sigil", "ActiveWindup", 1.5f, "Framework active windup.");

            _defaultPaladinVitality = Config.Bind("Passive Choices", "LegacyPaladinVitalityDefault", "Health", "Legacy Heart of Glory setting retained for config compatibility.");
            _defaultPaladinOffense = Config.Bind("Passive Choices", "LegacyPaladinOffenseDefault", "Martial", "Legacy Heart of Glory setting retained for config compatibility.");
            _defaultPriestOffense = Config.Bind("Passive Choices", "PriestOffenseDefault", "Martial", "Martial or Elemental.");

            _sharedCrossCastRange = Config.Bind("Targeting", "SharedCrossGroundPACRange", 35f, "Shared cross-selection range used by Priest Cross Cast skills and physical Cross targeting.");
            _acrobaticAscentLift = Config.Bind("Acrobatic Jump Skills", "LegacyAscentHangAcceleration", 4f, "Legacy compatibility value. v0.10.8 uses a guaranteed scripted ascent so Valheim cannot cancel the jump at takeoff.");
            _acrobaticJumpHeight = Config.Bind("Acrobatic Jump Skills", "JumpHeight", 2f, "Guaranteed cinematic jump height above the takeoff point before committed descent.");

            // v0.10.0 scale migration: known old defaults only.
            // Range values are framework center-to-edge meters; VFX now reach and hold the true radius like the DirtyHoe grid reference.
            MigrateFloat(_moonLength, 100f, 25f);
            MigrateFloat(_moonSpeed, 42f, 20f);
            MigrateFloat(_crescentRange, 30f, 15f);
            MigrateFloat(_crescentTravelTime, 1.1f, 3f);
            MigrateFloat(_stompRadius, 16f, 3f);
            MigrateFloat(_stompRadius, 8f, 3f);
            MigrateFloat(_stompRadius, 4f, 3f);
            MigrateFloat(_boneWindup, 2f, 1.5f);
            MigrateFloat(_boneRadius, 10f, 5f);
            MigrateFloat(_whirlwindRadius, 4f, 2f);
            MigrateFloat(_goddessRadius, 14f, 7f);
            MigrateFloat(_goddessRange, 100f, 50f);
            MigrateFloat(_divineRadius, 10f, 5f);
            MigrateFloat(_divineTrailRange, 20f, 10f);
            MigrateFloat(_lightningRelicRadius, 7f, 10f);
            MigrateFloat(_lightningRelicRange, 50f, 35f);
            MigrateFloat(_lightningRelicDuration, 12f, 16f);
            MigrateFloat(_holyRelicRadius, 7f, 10f);
            MigrateFloat(_holyRelicRange, 50f, 35f);
            MigrateFloat(_interventionRadius, 7f, 10f);
            MigrateFloat(_tempestRadius, 16f, 8f);
            MigrateFloat(_tempestRange, 100f, 50f);

            BindImmortalProgression();
            BindTreeHotbar();
            TryInstallPatches();

            Logger.LogInfo(ModName + " v" + ModVersion + " loaded.");
            Logger.LogInfo("Advancement skills, passives, unified HUD and Skillbook are ready.");
        }

        private void OnDestroy()
        {
            foreach(Texture2D texture in _ihAscendedIconArt.Values) if(texture!=null) Destroy(texture);
            foreach(Texture2D texture in _ihPolishTextures) if(texture!=null) Destroy(texture);
            _ihAscendedIconArt.Clear(); _ihPolishTextures.Clear(); _ihFrameSprites.Clear();
            EndShieldCharge();
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

            if (_skillbookOpen)
            {
                RestoreCursor();
                DragonCombat.SetUiInputBlocked(false);
            }
        }

        private void MigrateFloat(ConfigEntry<float> entry, float oldValue, float newValue)
        {
            if (entry != null && Mathf.Approximately(entry.Value, oldValue))
                entry.Value = newValue;
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
            UpdateRefreshingDots();

            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            IhEnsureCommands();

            // While a hotbar key is being captured, Esc / the Skillbook key only serve the capture.
            // Combat runtime ticking below continues normally.
            bool capturingKey = _bindCaptureTarget != BindNone;
            if (capturingKey)
                UpdateHotbarKeyCapture();

            UpdateTreePressRelease();

            if (!capturingKey && _skillbookOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleSkillbook();
                return;
            }

            if (!capturingKey && Input.GetKeyDown(_skillbookKey.Value))
            {
                if (_skillbookOpen)
                    ToggleSkillbook();
                else if (!Plugin.IsClassPanelOpen && !DragonCombat.IsGameplayHudSuppressed())
                    ToggleSkillbook();
            }

            string advancement = GetAdvancement(player);
            string uiOwner = player.GetInstanceID().ToString() + "|" + GetClass(player) + "|" + advancement + "|" + IhGetLevel(player).ToString();
            if (uiOwner != _ihUiOwner)
            {
                _ihUiOwner = uiOwner;
                _treePrototypePending.Clear();
                _treeSelectedNodeId = "";
                _dragActive = false;
                _pressSkillId = "";
                _hotbarLayoutOwnerKey = "";
            }
            UpdateCombatRuntimeState(player, advancement);
            CleanupTimedStates();
            UpdateMercenaryFuryState(player, advancement);

            if (advancement == "Mercenary" && Time.time >= _nextAggroPulse)
            {
                _nextAggroPulse = Time.time + 2f;
                EncourageAggro(player, Mathf.Max(1f, _mercAggroRadius.Value));
            }

            if (_skillbookOpen)
                return;

            // v0.18.1: Cleric / Paladin cast from the Skill Tree hotbar (bindings from [Hotbar]).
            if (IhUsesTreeHotbar(player))
            {
                HandleTreeHotbarInput(player);
                return;
            }

            if (!Input.GetKey(_modifier.Value))
                return;

            if (Input.GetKeyDown(_skill4.Value))
            {
                if (advancement == "Sword Master")
                    CastMoonlightSplitter(player);
                else if (advancement == "Mercenary")
                    CastStomp(player);
                else if (advancement == "Paladin")
                {
                    if (IhCanCast(player, "goddess_relic"))
                        CastGoddessRelic(player);
                }
                else if (advancement == "Priest")
                    CastLightningRelic(player);
            }

            if (Input.GetKeyDown(_skill5.Value))
            {
                if (advancement == "Sword Master")
                    CastCrescentCleave(player);
                else if (advancement == "Mercenary")
                    CastBonecrusher(player);
                else if (advancement == "Paladin")
                {
                    if (IhCanCast(player, "ray_of_hope"))
                        CastRayOfHope(player);
                }
                else if (advancement == "Priest")
                    CastHolyRelic(player);
            }

            if (Input.GetKeyDown(_passiveActive.Value))
            {
                if (advancement == "Mercenary")
                    ActivateBarbaric(player);
                else if (advancement == "Priest")
                    ActivateGrandSigil(player);
            }

            // Advancement classes use five numbered skills plus an Ultimate on the last numbered hotkey.
            if (Input.GetKeyDown(_ultimate.Value))
            {
                if (advancement == "Sword Master")
                    CastJudgementCut(player);
                else if (advancement == "Mercenary")
                    CastCircleSwing(player);
                else if (advancement == "Paladin")
                {
                    // M4+6 only STARTS Shield Charge. It is never a recast button.
                    // Left Click is the dedicated manual Shield Bash input while charging.
                    if (!_shieldChargeActive && IhCanCast(player, "shield_charge"))
                        CastShieldCharge(player);
                }
                else if (advancement == "Priest")
                    CastDivineIntervention(player);
            }

            if (Input.GetKeyDown(_skill7.Value))
            {
                if (advancement == "Sword Master")
                    CastSeveredHorizon(player);
                else if (advancement == "Paladin")
                {
                    if (IhCanCast(player, "judgement_hammer"))
                        CastJudgementHammer(player);
                }
                else if (advancement == "Mercenary")
                    CastSeismicGuillotine(player);
                else if (advancement == "Priest")
                    CastGrandCross(player);
            }

            if (Input.GetKeyDown(_skill8.Value))
            {
                if (advancement == "Sword Master")
                    CastEmptySheath(player);
                else if (advancement == "Paladin")
                {
                    if (IhCanCast(player, "fallen_angel"))
                        CastFallenAngel(player);
                }
                else if (advancement == "Mercenary")
                    CastReaversOrbit(player);
                else if (advancement == "Priest")
                    CastHeavensJudgement(player);
            }

            if (Input.GetKeyDown(_skill9.Value))
            {
                if (advancement == "Sword Master")
                    CastHalfmoonSlash(player);
                else if (advancement == "Paladin")
                {
                    if (IhCanCast(player, "electric_smite"))
                        CastElectricSmite(player);
                }
                else if (advancement == "Mercenary")
                    CastWhirlwind(player);
                else if (advancement == "Priest")
                    CastLightningTempest(player);
            }
        }

        private void CastMoonlightSplitter(Player player)
        {
            const string id = "SwordMaster.MoonlightSplitter";
            if (!BeginCast(player, id, _moonCooldown.Value, _moonStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, 1f);
            DragonCombat.LockSkill(player, windup + 1.05f);
            DragonCombat.PlaySkillPose(player, "Moonlight", windup + 1.10f);
            StartCoroutine(MoonlightRoutine(player, windup));
        }

        private IEnumerator MoonlightRoutine(Player player, float windup)
        {
            ShowMessage("Moonlight Splitter");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            float range = Mathf.Max(1f, _moonLength.Value);
            float width = Mathf.Max(0.5f, _moonWidth.Value);

            for (int slash = 0; slash < 3; slash++)
            {
                if (player == null || player.IsDead())
                    yield break;

                DragonCombat.PlaySkillPose(player, "Moonlight", 0.32f);
                Vector3 origin = player.GetEyePoint() + player.transform.up * -0.25f;
                Vector3 forward = AlbedoAimUtility.GetProjectileDirection(player, origin);
                StartCoroutine(GhostSlashProjectile(player, origin, forward, range, width, _moonSpeed.Value, _moonDamage));

                if (slash < 2)
                    yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator GhostSlashProjectile(Player player, Vector3 origin, Vector3 forward, float range, float width, float speed, DamageConfig damage)
        {
            if (forward.sqrMagnitude < 0.01f)
                forward = player.transform.forward;
            forward.Normalize();
            speed = Mathf.Max(1f, speed);
            range = Mathf.Max(1f, range);
            width = Mathf.Max(0.5f, width);

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < 0.01f)
                right = Vector3.right;

            GameObject visual = null;
            LineRenderer line = null;
            if (_enableVfx.Value)
            {
                visual = new GameObject("DragonsAltarMoonlightGhost");
                line = visual.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.48f;
                line.endWidth = 0.24f;
                line.startColor = new Color(0.45f, 0.78f, 1f, 1f);
                line.endColor = new Color(0.88f, 0.97f, 1f, 0.9f);
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                    line.material = new Material(shader);
            }

            HashSet<Character> hitTargets = new HashSet<Character>();
            float distance = 0f;
            while (distance < range)
            {
                if (player == null)
                    break;

                distance = Mathf.Min(range, distance + speed * Time.deltaTime);
                Vector3 center = origin + forward * distance;

                Collider[] hits = Physics.OverlapBox(center, new Vector3(width * 0.5f, 1.2f, 0.35f), Quaternion.LookRotation(forward, Vector3.up));
                for (int i = 0; i < hits.Length; i++)
                {
                    Character target = hits[i].GetComponentInParent<Character>();
                    if (target == null || hitTargets.Contains(target) || !IsEnemy(player, target))
                        continue;
                    hitTargets.Add(target);
                    DealDamage(player, target, damage, 8f, false);
                }

                if (line != null)
                {
                    line.SetPosition(0, center - right * width * 0.5f);
                    line.SetPosition(1, center + right * width * 0.5f);
                }

                // Ghost projectile: terrain and structures do not shorten its full configured range.
                yield return null;
            }

            if (visual != null)
                Destroy(visual);
        }

        private void CastCrescentCleave(Player player)
        {
            const string id = "SwordMaster.CrescentCleave";
            if (!BeginCast(player, id, _crescentCooldown.Value, _crescentStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, 1f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Crescent", windup + 0.10f);
            StartCoroutine(CrescentCleaveRoutine(player, windup));
        }

        private IEnumerator CrescentCleaveRoutine(Player player, float windup)
        {
            ShowMessage("Crescent Cleave");

            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            if (player == null || player.IsDead())
                yield break;

            Vector3 baseForward = AlbedoAimUtility.GetProjectileDirection(
                player,
                player.transform.position + Vector3.up * 0.5f
            );

            baseForward.y = 0f;

            if (baseForward.sqrMagnitude < 0.01f)
                baseForward = player.transform.forward;

            baseForward.y = 0f;

            if (baseForward.sqrMagnitude < 0.01f)
                baseForward = Vector3.forward;

            baseForward.Normalize();

            float totalSpread = Mathf.Clamp(_crescentSpreadAngle.Value, 8f, 140f);
            float halfSpread = totalSpread * 0.5f;

            // Five rays: one straight center slash plus four spreading slashes.
            float[] angles = new float[]
            {
                -halfSpread,
                -halfSpread * 0.5f,
                 0f,
                 halfSpread * 0.5f,
                 halfSpread
            };

            float range = Mathf.Max(1f, _crescentRange.Value);
            float width = Mathf.Max(0.25f, _crescentSlashWidth.Value);
            float height = Mathf.Max(1f, _crescentSlashHeight.Value);
            float travelTime = Mathf.Max(0.20f, _crescentTravelTime.Value);
            Vector3 origin = player.transform.position + baseForward * 0.45f;

            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(angles[i], Vector3.up) * baseForward;

                StartCoroutine(
                    CrescentVerticalSlashWave(
                        player,
                        origin,
                        direction,
                        range,
                        width,
                        height,
                        travelTime
                    )
                );
            }
        }

        private void CastJudgementCut(Player player)
        {
            if (Time.time < _judgementNextCastAt)
                return;

            int chargeIndex = GetReadyJudgementChargeIndex();
            if (chargeIndex < 0)
            {
                ShowMessage("Judgement Cut recharge: " + GetJudgementNextRecharge().ToString("0.0") + "s");
                return;
            }

            float stamina = Mathf.Max(0f, _judgementStamina.Value);
            if (GetStamina(player) < stamina)
            {
                ShowMessage("Not enough stamina");
                return;
            }

            UseStamina(player, stamina);
            float recharge = Mathf.Max(0f, _judgementCooldown.Value);
            _judgementChargeReadyAt[chargeIndex] = Time.time + recharge;
            _judgementNextCastAt = Time.time + Mathf.Max(0f, _judgementBuffer.Value);

            Vector3 point = GetAimPoint(player, Mathf.Max(1f, _judgementRange.Value));
            DragonCombat.LockSkill(player, 0.40f);
            DragonCombat.PlaySkillPose(player, "Moonlight", 0.45f);
            StartCoroutine(JudgementCutRoutine(player, point));
        }

        private IEnumerator JudgementCutRoutine(Player player, Vector3 point)
        {
            ShowMessage("Judgement Cut");
            float radius = Mathf.Max(0.5f, _judgementRadius.Value);
            float slashDamage = Mathf.Max(0f, _judgementSlashDamage.Value);

            if (player == null || player.IsDead())
                yield break;

            // OG-style Judgement Cut: all three cuts resolve on the same frame.
            // Damage and the three sphere-cut visuals are simultaneous; only the existing
            // per-stack recharge and 0.5s activation buffer govern repeated casts.
            for (int slash = 0; slash < 3; slash++)
            {
                List<Character> targets = GetSphereTargets(player, point, radius);
                for (int i = 0; i < targets.Count; i++)
                {
                    HitData hit = new HitData();
                    hit.m_damage.m_slash = slashDamage;
                    hit.m_point = targets[i].transform.position;
                    hit.m_dir = (targets[i].transform.position - point).normalized;
                    hit.m_pushForce = 0f;
                    hit.SetAttacker(player);
                    targets[i].Damage(hit);
                }

                if (_enableVfx.Value)
                    StartCoroutine(AnimateJudgementSphereCut(point, radius, slash));
            }

            yield break;
        }

        private int GetReadyJudgementChargeIndex()
        {
            for (int i = 0; i < _judgementChargeReadyAt.Length; i++)
            {
                if (Time.time >= _judgementChargeReadyAt[i])
                    return i;
            }
            return -1;
        }

        private int GetJudgementReadyChargeCount()
        {
            int ready = 0;
            for (int i = 0; i < _judgementChargeReadyAt.Length; i++)
            {
                if (Time.time >= _judgementChargeReadyAt[i])
                    ready++;
            }
            return ready;
        }

        private float GetJudgementNextRecharge()
        {
            int ready = GetJudgementReadyChargeCount();
            if (ready >= _judgementChargeReadyAt.Length)
                return 0f;

            float smallest = float.MaxValue;
            for (int i = 0; i < _judgementChargeReadyAt.Length; i++)
            {
                float remaining = _judgementChargeReadyAt[i] - Time.time;
                if (remaining > 0f && remaining < smallest)
                    smallest = remaining;
            }
            return smallest == float.MaxValue ? 0f : Mathf.Max(0f, smallest);
        }

        private IEnumerator AnimateJudgementSphereCut(Vector3 center, float radius, int slash)
        {
            Vector3 axisA = Vector3.right;
            Vector3 axisB = Vector3.forward;
            if (slash == 1)
            {
                axisA = Vector3.right;
                axisB = Vector3.up;
            }
            else if (slash == 2)
            {
                axisA = Vector3.forward;
                axisB = Vector3.up;
            }

            GameObject obj = new GameObject("DragonsAltarJudgementCut");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 49;
            line.startWidth = 0.11f;
            line.endWidth = 0.11f;
            Color color = new Color(0.72f, 0.82f, 1f, 0.95f);
            line.startColor = color;
            line.endColor = color;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            float elapsed = 0f;
            const float duration = 0.34f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float visualRadius = radius * Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(t * 3f));
                Color frame = color;
                frame.a = color.a * (1f - Mathf.Clamp01(t));
                line.startColor = frame;
                line.endColor = frame;

                for (int i = 0; i < line.positionCount; i++)
                {
                    float angle = ((float)i / (float)(line.positionCount - 1)) * Mathf.PI * 2f;
                    line.SetPosition(i, center + axisA * Mathf.Cos(angle) * visualRadius + axisB * Mathf.Sin(angle) * visualRadius);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(obj);
        }

        private void CastSeveredHorizon(Player player)
        {
            const string id = "SwordMaster.SeveredHorizon";
            if (!BeginCast(player, id, _severedCooldown.Value, _severedStamina.Value))
                return;

            Vector3 origin = player.transform.position + Vector3.up * 0.9f;
            Vector3 forward = GetCrosshairDirection(player, origin);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = player.transform.forward;
            forward.Normalize();

            float range = Mathf.Max(1f, _severedRange.Value);
            Vector3 start = player.transform.position + forward * 0.8f + Vector3.up * 0.9f;
            Vector3 end = start + forward * range;

            DragonCombat.LockSkill(player, 0.30f);
            DragonCombat.PlaySkillPose(player, "Moonlight", 0.36f);
            StartCoroutine(SeveredHorizonRoutine(player, start, end));
        }

        private IEnumerator SeveredHorizonRoutine(Player player, Vector3 start, Vector3 end)
        {
            ShowMessage("Severed Horizon");
            float delay = Mathf.Max(0.05f, _severedDelay.Value);
            float width = Mathf.Max(0.25f, _severedWidth.Value);

            if (_enableVfx.Value)
                StartCoroutine(AnimateSeveredHorizonLine(start, end, width, delay));

            yield return new WaitForSeconds(delay);
            if (player == null || player.IsDead())
                yield break;

            Collider[] hits = Physics.OverlapCapsule(start, end, width * 0.5f, ~0, QueryTriggerInteraction.Ignore);
            HashSet<Character> damaged = new HashSet<Character>();
            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();
                if (target == null || damaged.Contains(target) || !IsEnemy(player, target))
                    continue;
                damaged.Add(target);
                DealDamage(player, target, _severedDamage, 0f, false);
            }

            if (_enableVfx.Value)
            {
                Vector3 midpoint = (start + end) * 0.5f;
                StartCoroutine(AnimateRing(midpoint - Vector3.up * 0.82f, 0.2f, Mathf.Max(1f, width * 1.5f), 0.28f,
                    new Color(0.72f, 0.86f, 1f, 0.92f), 0.08f));
            }
        }

        private IEnumerator AnimateSeveredHorizonLine(Vector3 start, Vector3 end, float width, float delay)
        {
            GameObject obj = new GameObject("DragonsAltarSeveredHorizon");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = Mathf.Max(0.05f, width * 0.08f);
            line.endWidth = line.startWidth;
            Color color = new Color(0.70f, 0.86f, 1f, 0.92f);
            line.startColor = color;
            line.endColor = color;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            float elapsed = 0f;
            while (elapsed < delay)
            {
                float t = Mathf.Clamp01(elapsed / delay);
                float pulse = Mathf.Lerp(0.06f, Mathf.Max(0.10f, width * 0.18f), t);
                line.startWidth = pulse;
                line.endWidth = pulse;
                elapsed += Time.deltaTime;
                yield return null;
            }

            line.startWidth = Mathf.Max(0.16f, width * 0.34f);
            line.endWidth = line.startWidth;
            yield return new WaitForSeconds(0.12f);
            Destroy(obj);
        }

        private void CastEmptySheath(Player player)
        {
            const string id = "SwordMaster.EmptySheath";
            if (!BeginCast(player, id, _emptyCooldown.Value, _emptyStamina.Value))
                return;

            _emptySheathCounterUntil = Time.time + Mathf.Max(0.1f, _emptyCounterWindow.Value);
            DragonCombat.LockSkill(player, 0.12f);
            DragonCombat.PlaySkillPose(player, "EmptySheath", Mathf.Max(0.18f, _emptyCounterWindow.Value));
            ShowMessage("Empty Sheath");

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(player.transform.position + Vector3.up * 0.05f, 0.5f, 1.8f,
                    Mathf.Max(0.18f, _emptyCounterWindow.Value), new Color(0.78f, 0.88f, 1f, 0.75f), 0.07f));
        }

        private IEnumerator EmptySheathCounterRoutine(Player player, Character attacker)
        {
            if (player == null || attacker == null || player.IsDead() || attacker.IsDead())
                yield break;

            ShowMessage("Empty Sheath - COUNTER");
            Vector3 attackerForward = attacker.transform.forward;
            attackerForward.y = 0f;
            if (attackerForward.sqrMagnitude < 0.01f)
                attackerForward = (attacker.transform.position - player.transform.position).normalized;
            if (attackerForward.sqrMagnitude < 0.01f)
                attackerForward = Vector3.forward;
            attackerForward.Normalize();

            Vector3 destination = attacker.transform.position - attackerForward * Mathf.Max(0.5f, _emptyBehindDistance.Value);
            RaycastHit groundHit;
            if (Physics.Raycast(destination + Vector3.up * 3f, Vector3.down, out groundHit, 7f, ~0, QueryTriggerInteraction.Ignore))
                destination.y = groundHit.point.y + 0.06f;
            else
                destination.y = player.transform.position.y;

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.position = destination;
            }
            player.transform.position = destination;

            Vector3 face = attacker.transform.position - destination;
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f)
                player.transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);

            yield return new WaitForSeconds(0.18f);

            int cuts = Mathf.Clamp(_emptyCutCount.Value, 1, 12);
            float interval = Mathf.Max(0.03f, _emptyCutInterval.Value);
            float slashDamage = Mathf.Max(0f, _emptySlashDamage.Value);
            for (int i = 0; i < cuts; i++)
            {
                if (player == null || attacker == null || player.IsDead() || attacker.IsDead())
                    yield break;

                HitData hit = new HitData();
                hit.m_damage.m_slash = slashDamage;
                hit.m_point = attacker.transform.position;
                hit.m_dir = (attacker.transform.position - player.transform.position).normalized;
                hit.m_pushForce = 0f;
                hit.SetAttacker(player);
                attacker.Damage(hit);

                if (_enableVfx.Value)
                    StartCoroutine(AnimateJudgementSphereCut(attacker.transform.position + Vector3.up * 0.9f, 2.2f, i % 3));

                if (i < cuts - 1)
                    yield return new WaitForSeconds(interval);
            }
        }

        private void CastHalfmoonSlash(Player player)
        {
            const string id = "SwordMaster.HalfmoonSlash";
            if (!BeginCast(player, id, _halfmoonCooldown.Value, _halfmoonStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, 2f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Halfmoon", windup + 0.12f);
            StartCoroutine(HalfmoonRoutine(player, windup));
        }

        private IEnumerator HalfmoonRoutine(Player player, float windup)
        {
            ShowMessage("Halfmoon Slash");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            if (player == null || player.IsDead())
                yield break;

            float radius = Mathf.Max(2f, _halfmoonRadius.Value);
            Vector3 forward = AlbedoAimUtility.GetProjectileDirection(player, player.transform.position + Vector3.up * 1f);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = player.transform.forward;
            forward.Normalize();

            ApplyHalfmoonHit(player, forward, radius, 1f);
            if (_enableVfx.Value)
                StartCoroutine(AnimateHalfmoonArc(player.transform.position + Vector3.up * 0.9f, forward, radius));

            yield return new WaitForSeconds(Mathf.Clamp(_halfmoonSecondSlashDelay.Value, 0.10f, 2f));
            ApplyHalfmoonHit(player, forward, radius, 0.5f);
            if (_enableVfx.Value)
                StartCoroutine(AnimateHalfmoonArc(player.transform.position + Vector3.up * 1.05f, forward, radius * 0.92f));
        }

        private void ApplyHalfmoonHit(Player player, Vector3 forward, float radius, float multiplier)
        {
            List<Character> targets = GetFrontalTargets(player, player.transform.position + Vector3.up * 0.8f, forward, radius, 170f);
            for (int i = 0; i < targets.Count; i++)
            {
                DealScaledDamage(player, targets[i], _halfmoonDamage, 26f, multiplier);
                StartCoroutine(SpiritDot(player, targets[i], _halfmoonSpiritDot.Value * multiplier, _halfmoonSpiritDuration.Value));
                DragonCombat.Stun(targets[i], player.transform.position);
            }
        }

        private void ActivateSwordMastery(Player player)
        {
            const string id = "SwordMaster.SwordMastery";
            if (!BeginCast(player, id, _swordActiveCooldown.Value, 0f))
                return;

            const float activationTime = 1f;

            // Boss-power style activation: lock for one second, play Valheim's
            // Guardian/Boss Power activation trigger, THEN start the 10s buff.
            DragonCombat.LockSkill(player, activationTime);
            DragonCombat.PlayAnimation(player, "gpower");
            StartCoroutine(SwordMasteryActivationRoutine(player, activationTime));
        }

        private IEnumerator SwordMasteryActivationRoutine(Player player, float activationTime)
        {
            ShowMessage("Sword Mastery - Activating");

            if (activationTime > 0f)
                yield return new WaitForSeconds(activationTime);

            if (player == null || player.IsDead())
                yield break;

            _swordActiveUntil = Time.time + Mathf.Max(0.1f, _swordActiveDuration.Value);
            ShowMessage("Sword Mastery");

            if (_enableVfx.Value)
                StartCoroutine(
                    AnimateAura(
                        player,
                        new Color(0.45f, 0.82f, 1f, 0.9f),
                        Mathf.Max(0.1f, _swordActiveDuration.Value)
                    )
                );
        }

        private void CastStomp(Player player)
        {
            const string id = "Mercenary.Stomp";
            if (!BeginCast(player, id, _stompCooldown.Value, _stompStamina.Value))
                return;

            float windup = Mathf.Max(0f, _stompWindup.Value);
            DragonCombat.LockSkill(player, 1f);
            DragonCombat.PlaySkillPose(player, "Stomp", Mathf.Max(0.65f, windup + 0.15f));
            StartCoroutine(StompRoutine(player, windup));
        }

        private IEnumerator StompRoutine(Player player, float windup)
        {
            ShowMessage("Stomp");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead())
                yield break;

            Vector3 center = player.transform.position;
            float firstRadius = Mathf.Max(0.5f, _stompRadius.Value);
            List<Character> firstTargets = GetSphereTargets(player, center, firstRadius);
            for (int i = 0; i < firstTargets.Count; i++)
            {
                DealDamage(player, firstTargets[i], _stompDamage, 24f, false);
                GainMercenaryFuryFromSkillHit(player);
                if (DragonCombat.IsSmallEnemy(firstTargets[i]))
                    DragonCombat.Stun(firstTargets[i], center);
            }
            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, 0.4f, firstRadius, 0.55f, new Color(0.78f, 0.50f, 0.22f, 1f), 0.14f));

            float delay = Mathf.Max(0f, _stompAftershockDelay.Value);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            if (player == null || player.IsDead())
                yield break;

            float aftershockRadius = Mathf.Max(firstRadius, _stompAftershockRadius.Value);
            List<Character> aftershockTargets = GetSphereTargets(player, center, aftershockRadius);
            for (int i = 0; i < aftershockTargets.Count; i++)
            {
                DealDamage(player, aftershockTargets[i], _stompDamage, 30f, false);
                GainMercenaryFuryFromSkillHit(player);
                if (DragonCombat.IsSmallEnemy(aftershockTargets[i]))
                    DragonCombat.Stun(aftershockTargets[i], center);
            }
            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(center + Vector3.up * 0.10f, firstRadius, aftershockRadius, 0.70f, new Color(0.95f, 0.62f, 0.22f, 1f), 0.18f));
        }

        private void CastBonecrusher(Player player)
        {
            const string id = "Mercenary.Bonecrusher";
            if (!BeginCast(player, id, _boneCooldown.Value, _boneStamina.Value))
                return;

            float takeoffDelay = 0.08f;
            DragonCombat.LockSkill(player, takeoffDelay);
            DragonCombat.PlaySkillPose(player, "Slam", 8f);
            StartCoroutine(BonecrusherRoutine(player, takeoffDelay));
        }

        private IEnumerator BonecrusherRoutine(Player player, float takeoffDelay)
        {
            ShowMessage("Bonecrusher");
            yield return StartCoroutine(AcrobaticJumpUntilLanding(player, takeoffDelay, Mathf.Max(1.5f, _boneWindup.Value)));
            if (player == null || player.IsDead())
                yield break;

            DragonCombat.PlaySkillPose(player, "Slam", 0.35f);
            Vector3 center = player.transform.position;
            float radius = Mathf.Max(0.5f, _boneRadius.Value);
            List<Character> targets = GetSphereTargets(player, center, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                DealDamage(player, targets[i], _boneDamage, 34f, true);
                DragonCombat.ApplyBrokenBones(targets[i], 6f);
                DragonCombat.ApplyCripple(targets[i], 6f);
                ForceStagger(targets[i], player);
                GainMercenaryFuryFromSkillHit(player);
            }
            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, 0.3f, radius, 0.55f, new Color(1f, 0.46f, 0.16f, 1f), 0.17f));

            if (IsUnchainedFuryActive())
                FuryBonecrusherCone(player, center, FlatForward(player));
        }

        private void CastCircleSwing(Player player)
        {
            const string id = "Mercenary.CircleSwing";
            if (!BeginCast(player, id, _circleCooldown.Value, _circleStamina.Value))
                return;

            float windup = Mathf.Max(0.1f, _circleWindup.Value);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.GrantHyperArmor(player, windup + 0.25f);
            DragonCombat.PlaySkillPose(player, "CircleSwing", windup + 0.18f);
            StartCoroutine(CircleSwingRoutine(player, windup));
        }

        private IEnumerator CircleSwingRoutine(Player player, float windup)
        {
            ShowMessage("Circle Swing");
            if (player == null || player.IsDead()) yield break;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            Vector3 forward = FlatForward(player);
            float maxTravel = Mathf.Max(0f, _circleWindupTravel.Value);
            float travelled = 0f;
            float elapsed = 0f;

            // Deliberate heavy-footed wind-up: steerable, but capped to a tiny total shuffle.
            while (elapsed < windup)
            {
                if (player == null || player.IsDead()) yield break;

                float horizontal = Input.GetAxisRaw("Horizontal");
                float vertical = Input.GetAxisRaw("Vertical");
                Vector3 steer = player.transform.forward * vertical + player.transform.right * horizontal;
                steer.y = 0f;
                if (steer.sqrMagnitude < 0.01f)
                    steer = FlatForward(player);
                else
                    steer.Normalize();

                if (steer.sqrMagnitude > 0.01f)
                    forward = steer.normalized;

                float remainingTravel = Mathf.Max(0f, maxTravel - travelled);
                float step = Mathf.Min(remainingTravel, (maxTravel / Mathf.Max(0.05f, windup)) * Time.deltaTime);
                if (step > 0f)
                {
                    Vector3 next = (rb != null ? rb.position : player.transform.position) + forward * step;
                    if (rb != null) rb.MovePosition(next);
                    else player.transform.position = next;
                    travelled += step;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (player == null || player.IsDead()) yield break;
            Vector3 center = player.transform.position;
            float radius = Mathf.Max(0.5f, _circleRadius.Value);
            DamageSnapshot weapon = GetWeaponDamage(player);
            List<Character> targets = GetSphereTargets(player, center, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                DealSnapshotDamage(player, targets[i], weapon, Mathf.Max(1f, _circleDamageMultiplier.Value), 48f, true);
                GainMercenaryFuryFromSkillHit(player);
            }

            if (_enableVfx.Value)
            {
                StartCoroutine(AnimateRing(center + Vector3.up * 0.10f, 0.6f, radius, 0.65f, new Color(1f, 0.56f, 0.18f, 1f), 0.20f));
                StartCoroutine(AnimateHalfmoonArc(center + Vector3.up * 1.0f, forward, radius * 0.95f));
            }
        }

        private void CastSeismicGuillotine(Player player)
        {
            const string id = "Mercenary.SeismicGuillotine";
            if (!BeginCast(player, id, _seismicCooldown.Value, _seismicStamina.Value))
                return;

            DragonCombat.LockSkill(player, 0.45f);
            DragonCombat.PlaySkillPose(player, "Stomp", 0.55f);
            StartCoroutine(SeismicGuillotineRoutine(player));
        }

        private IEnumerator SeismicGuillotineRoutine(Player player)
        {
            ShowMessage(IsUnchainedFuryActive() ? "Seismic Guillotine - UNCHAINED" : "Seismic Guillotine");
            yield return new WaitForSeconds(0.28f);
            if (player == null || player.IsDead())
                yield break;

            Vector3 origin = player.transform.position;
            float maxRange = Mathf.Max(2f, _seismicRange.Value);
            Vector3 aimPoint = GetAimPoint(player, maxRange);
            Vector3 aimDelta = aimPoint - origin;
            aimDelta.y = 0f;
            Vector3 forward = aimDelta.sqrMagnitude > 0.01f ? aimDelta.normalized : GetCrosshairDirection(player, origin);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = FlatForward(player);
            forward.Normalize();
            float range = Mathf.Clamp(aimDelta.magnitude, 0f, maxRange);
            DamageSnapshot weapon = GetWeaponDamage(player);
            float width = Mathf.Max(0.75f, _seismicWidth.Value);
            float fullRangeTravelTime = Mathf.Max(0.05f, _seismicTravelTime.Value);
            float shockSpeed = maxRange / fullRangeTravelTime;
            float multiplier = Mathf.Max(0f, _seismicDamageMultiplier.Value);
            bool fury = IsUnchainedFuryActive();
            HashSet<int> sharedHits = new HashSet<int>();

            if (fury)
                width *= 1.75f;

            StartCoroutine(SeismicFissure(player, origin, forward, range, width, shockSpeed, weapon, multiplier, sharedHits, true));

            if (fury)
            {
                Vector3 left = Quaternion.AngleAxis(-25f, Vector3.up) * forward;
                Vector3 right = Quaternion.AngleAxis(25f, Vector3.up) * forward;
                StartCoroutine(SeismicFissure(player, origin, left, range * 0.82f, width * 0.72f, shockSpeed, weapon, multiplier * 0.75f, sharedHits, false));
                StartCoroutine(SeismicFissure(player, origin, right, range * 0.82f, width * 0.72f, shockSpeed, weapon, multiplier * 0.75f, sharedHits, false));
            }
        }

        private IEnumerator SeismicFissure(Player player, Vector3 origin, Vector3 forward, float range, float width, float shockSpeed, DamageSnapshot weapon, float multiplier, HashSet<int> sharedHits, bool finishingRupture)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            int groundMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");
            const float shockSpacing = 1f;
            const float baseShockRadius = 2f;
            float safeSpeed = Mathf.Max(0.1f, shockSpeed);
            float shockRadius = baseShockRadius * (IsUnchainedFuryActive() ? 1.75f : 1f);
            int shockCount = Mathf.FloorToInt(Mathf.Max(0f, range) / shockSpacing + 0.0001f);
            float elapsed = 0f;

            for (int shockIndex = 1; shockIndex <= shockCount; shockIndex++)
            {
                float shockDistance = shockIndex * shockSpacing;
                float shockTime = shockDistance / safeSpeed;
                while (elapsed < shockTime)
                {
                    if (player == null || player.IsDead()) yield break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                Vector3 shockPoint = GetSeismicGroundPoint(origin + forward * shockDistance, groundMask);
                Collider[] hits = Physics.OverlapSphere(shockPoint + Vector3.up * 0.75f, shockRadius, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits.Length; i++)
                {
                    Character target = hits[i].GetComponentInParent<Character>();
                    if (target == null || !IsEnemy(player, target)) continue;
                    int tid = target.GetInstanceID();
                    if (!sharedHits.Add(tid)) continue;
                    DealSnapshotDamage(player, target, weapon, multiplier, 30f, false);
                    GainMercenaryFuryFromSkillHit(player);
                    if (DragonCombat.IsSmallEnemy(target))
                        ApplyMercenaryDisplacement(target, forward * 2.5f + Vector3.up * 6.5f);
                    else if (!target.IsBoss())
                        target.Stagger(forward);
                }

                if (_enableVfx.Value)
                    StartCoroutine(AnimateRing(shockPoint + Vector3.up * 0.05f, 0.35f, shockRadius, 0.16f, new Color(0.95f, 0.42f, 0.16f, 0.85f), 0.06f));
            }

            float ruptureTime = Mathf.Max(0f, range) / safeSpeed;
            while (elapsed < ruptureTime)
            {
                if (player == null || player.IsDead()) yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!finishingRupture || player == null || player.IsDead()) yield break;

            Vector3 rupturePoint = GetSeismicGroundPoint(origin + forward * Mathf.Max(0f, range), groundMask);
            float endRadius = Mathf.Max(1f, _seismicEndRadius.Value) * (IsUnchainedFuryActive() ? 1.35f : 1f);
            List<Character> endTargets = GetSphereTargets(player, rupturePoint, endRadius);
            for (int i = 0; i < endTargets.Count; i++)
            {
                DealSnapshotDamage(player, endTargets[i], weapon, multiplier, 44f, false);
                GainMercenaryFuryFromSkillHit(player);
                if (DragonCombat.IsSmallEnemy(endTargets[i]))
                    ApplyMercenaryDisplacement(endTargets[i], forward * 2.2f + Vector3.up * 7f);
                else if (!endTargets[i].IsBoss())
                    endTargets[i].Stagger(forward);
            }
            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(rupturePoint + Vector3.up * 0.08f, width * 0.5f, endRadius, 0.46f, new Color(1f, 0.52f, 0.18f, 0.95f), 0.16f));
        }

        private Vector3 GetSeismicGroundPoint(Vector3 projected, int groundMask)
        {
            RaycastHit ground;
            if (Physics.Raycast(projected + Vector3.up * 7f, Vector3.down, out ground, 18f, groundMask))
                return ground.point;
            return projected;
        }

        private void CastReaversOrbit(Player player)
        {
            const string id = "Mercenary.ReaversOrbit";
            if (!BeginCast(player, id, _reaverCooldown.Value, _reaverStamina.Value))
                return;

            DragonCombat.PlaySkillPose(player, "CircleSwing", 0.45f);
            StartCoroutine(ReaversOrbitRoutine(player));
        }

        private IEnumerator ReaversOrbitRoutine(Player player)
        {
            ShowMessage("Reaver's Orbit");
            DamageSnapshot weapon = GetWeaponDamage(player);
            float maxRadius = Mathf.Max(2f, _reaverRadius.Value);
            float duration = Mathf.Max(0.5f, _reaverDuration.Value);
            float hitRadius = Mathf.Max(0.4f, _reaverHitRadius.Value);
            float multiplier = Mathf.Max(0f, _reaverDamageMultiplier.Value);
            HashSet<int> outwardHits = new HashSet<int>();
            HashSet<int> returnHits = new HashSet<int>();
            float elapsed = 0f;
            float nextVisual = 0f;

            while (elapsed <= duration)
            {
                if (player == null || player.IsDead()) yield break;
                float t = Mathf.Clamp01(elapsed / duration);
                bool returning = t >= 0.5f;
                float phase = returning ? (t - 0.5f) * 2f : t * 2f;
                float radius = returning ? Mathf.Lerp(maxRadius, 0.65f, phase) : Mathf.Lerp(0.65f, maxRadius, phase);
                float spin = t * 360f;
                Vector3 center = player.transform.position + Vector3.up * 1.0f;
                Vector3[] points = new Vector3[2];
                points[0] = center + (Quaternion.AngleAxis(spin, Vector3.up) * Vector3.forward) * radius;
                points[1] = center + (Quaternion.AngleAxis(180f - spin, Vector3.up) * Vector3.forward) * radius;
                HashSet<int> phaseHits = returning ? returnHits : outwardHits;

                for (int a = 0; a < points.Length; a++)
                {
                    Collider[] hits = Physics.OverlapSphere(points[a], hitRadius, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        Character target = hits[i].GetComponentInParent<Character>();
                        if (target == null || !IsEnemy(player, target)) continue;
                        int tid = target.GetInstanceID();
                        if (!phaseHits.Add(tid)) continue;
                        DealSnapshotDamage(player, target, weapon, multiplier, returning ? 8f : 24f, false);
                        GainMercenaryFuryFromSkillHit(player);
                        Vector3 radial = target.transform.position - player.transform.position;
                        radial.y = 0f;
                        if (radial.sqrMagnitude < 0.01f) radial = player.transform.forward;
                        radial.Normalize();
                        if (!returning)
                            ApplyMercenaryDisplacement(target, radial * 5f + Vector3.up * 0.8f);
                        else if (DragonCombat.IsSmallEnemy(target))
                            ApplyMercenaryDisplacement(target, -radial * 6f + Vector3.up * 0.4f);
                    }
                }

                if (_enableVfx.Value && elapsed >= nextVisual)
                {
                    nextVisual = elapsed + 0.10f;
                    StartCoroutine(AnimateRing(points[0], 0.18f, hitRadius, 0.16f, new Color(1f, 0.58f, 0.20f, 0.82f), 0.07f));
                    StartCoroutine(AnimateRing(points[1], 0.18f, hitRadius, 0.16f, new Color(1f, 0.58f, 0.20f, 0.82f), 0.07f));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void FuryBonecrusherCone(Player player, Vector3 origin, Vector3 forward)
        {
            float range = Mathf.Max(2f, _mercFuryBoneConeRange.Value);
            float angle = Mathf.Clamp(_mercFuryBoneConeAngle.Value, 20f, 170f);
            float multiplier = Mathf.Max(0f, _mercFuryBoneConeMultiplier.Value);
            List<Character> targets = GetFrontalTargets(player, origin, forward, range, angle);
            for (int i = 0; i < targets.Count; i++)
            {
                DealScaledDamage(player, targets[i], _boneDamage, 34f, multiplier);
                if (DragonCombat.IsSmallEnemy(targets[i]))
                    ApplyMercenaryDisplacement(targets[i], forward * 4f + Vector3.up * 7.5f);
                else if (!targets[i].IsBoss())
                    targets[i].Stagger(forward);
            }
            if (_enableVfx.Value)
                StartCoroutine(AnimateHalfmoonArc(origin + Vector3.up * 0.25f, forward, range));
        }

        private void ApplyMercenaryDisplacement(Character target, Vector3 velocityChange)
        {
            if (target == null || target.IsDead()) return;
            Rigidbody body = target.GetComponent<Rigidbody>();
            if (body != null)
                body.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        private void CastWhirlwind(Player player)
        {
            const string id = "Mercenary.Whirlwind";
            if (!BeginCast(player, id, _whirlwindCooldown.Value, _whirlwindStamina.Value))
                return;

            float duration = Mathf.Max(0.1f, _whirlwindDuration.Value);

            // Framework exception: Whirlwind allows normal movement while spinning.
            // Combat Runtime suppresses sprint/other actions but keeps movedir intact.
            DragonCombat.BeginWhirlwind(player, duration);
            StartCoroutine(WhirlwindRoutine(player));
        }

        private IEnumerator WhirlwindRoutine(Player player)
        {
            ShowMessage("Whirlwind");
            DamageSnapshot weapon = GetWeaponDamage(player);
            float duration = Mathf.Max(0.5f, _whirlwindDuration.Value);
            float interval = Mathf.Max(0.1f, _whirlwindInterval.Value);
            int ticks = Mathf.Max(1, Mathf.RoundToInt(duration / interval));

            for (int tick = 0; tick < ticks; tick++)
            {
                if (player == null || player.IsDead())
                    yield break;

                                if (tick % 2 == 0)
                    DragonCombat.PlaySkillPose(player, "Whirlwind", 0.34f);
                else
                    DragonCombat.PlaySkillPose(player, "Whirlwind", 0.34f);
                List<Character> targets = GetSphereTargets(player, player.transform.position, Mathf.Max(0.5f, _whirlwindRadius.Value));
                for (int i = 0; i < targets.Count; i++)
                {
                    DealSnapshotDamage(player, targets[i], weapon, Mathf.Max(0f, _whirlwindWeaponMultiplier.Value), 14f);
                    GainMercenaryFuryFromSkillHit(player);
                }

                if (_enableVfx.Value)
                    StartCoroutine(AnimateRing(player.transform.position + Vector3.up * 0.9f, 0.4f, Mathf.Max(0.5f, _whirlwindRadius.Value), Mathf.Min(0.32f, interval), new Color(1f, 0.62f, 0.22f, 0.75f), 0.10f));

                yield return new WaitForSeconds(interval);
            }
        }

        private void CastGoddessRelic(Player player)
        {
            const string id = "Paladin.GoddessRelic";
            Vector3 target;
            float range = Mathf.Max(1f, _sharedCrossCastRange.Value);
            if (!TryGetPhysicalAimPoint(player, range, out target))
            {
                ShowMessage("Aim at a physical target");
                return;
            }
            if (!BeginCast(player, id, _goddessCooldown.Value, _goddessStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _goddessWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f);
            StartCoroutine(GoddessRelicRoutine(player, target, windup));
        }

        private IEnumerator GoddessRelicRoutine(Player player, Vector3 target, float windup)
        {
            ShowMessage("Goddess Relic");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            // v0.17.0: normal cross is about a 0-star Troll, 5m radius, no Mark.
            // Ascended: 3x cross, 10m radius, heavy Blunt + Lightning and it applies Judgement Mark.
            bool ascended = IsAscendedSkill("goddess_relic");
            float sizeMultiplier = ascended ? Mathf.Max(1f, _goddessAscSizeMultiplier.Value) : 1f;
            float crossHeight = Mathf.Max(1f, _goddessCrossHeight.Value) * sizeMultiplier;
            float crossWidth = Mathf.Max(0.5f, _goddessCrossWidth.Value) * sizeMultiplier;
            float radius = Mathf.Max(1f, ascended ? _goddessAscRadius.Value : _goddessRadiusV17.Value);

            Vector3 finalCenter = GetGroundedCrossCenter(target, crossHeight);
            Vector3 skyPoint = DragonCombat.GetIndoorSafeSkyPoint(finalCenter, Mathf.Max(7f, crossHeight + 2f));
            GameObject cross = CreateCross(
                skyPoint,
                new Color(1f, 0.82f, 0.35f, 1f),
                crossHeight,
                crossWidth,
                0.12f * sizeMultiplier,
                _enableVfx.Value
            );

            float fallTime = DragonCombat.GetSkySummonDropTime();
            float elapsed = 0f;
            while (elapsed < fallTime)
            {
                float progress = DragonCombat.GetSkySummonFallProgress(elapsed / fallTime);
                if (cross != null)
                    cross.transform.position = Vector3.Lerp(skyPoint, finalCenter, progress);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (cross != null)
            {
                cross.transform.position = finalCenter;
                Vector3 toCaster = player.transform.position - finalCenter;
                toCaster.y = 0f;
                if (toCaster.sqrMagnitude > 0.01f)
                    cross.transform.rotation = Quaternion.LookRotation(toCaster.normalized, Vector3.up);
                SetCrossPhysical(cross, true);
            }

            if (_enableVfx.Value)
            {
                CreateLightning(target, new Color(0.62f, 0.88f, 1f, 1f), 0.32f);
                StartCoroutine(AnimateRing(target + Vector3.up * 0.08f, 0.3f, radius, 0.55f, new Color(1f, 0.82f, 0.35f, 0.92f), 0.12f));
            }

            List<Character> targets = GetSphereTargets(player, target, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                if (ascended)
                {
                    _paladinMarkSourceContext = "GoddessRelic";
                    try
                    {
                        DealDamage(player, targets[i], _goddessAscDamage, 28f, false);
                    }
                    finally
                    {
                        _paladinMarkSourceContext = string.Empty;
                    }
                }
                else
                {
                    DealDamage(player, targets[i], _goddessDamage, 20f, false);
                    StartCoroutine(SpiritDot(player, targets[i], _goddessSpiritDot.Value, _goddessSpiritDuration.Value));
                }
            }
            if (cross != null)
                Destroy(cross, 3.0f);
        }

        private void CastRayOfHope(Player player)
        {
            const string id = "Paladin.RayOfHope";
            if (!BeginCast(player, id, _rayCooldown.Value, _rayStamina.Value))
                return;
            // v0.17.0: instant cast (no channel), 0.5s movement lock, chant animation.
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlaySkillPose(player, "Chant", 0.50f);
            StartCoroutine(RayOfHopeRoutine(player, 0f));
        }

        private IEnumerator RayOfHopeRoutine(Player player, float windup)
        {
            ShowMessage("Ray of Hope");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead())
                yield break;

            float radius = Mathf.Max(1f, _rayRadius.Value);
            float healPercent = Mathf.Clamp(_rayHealPercent.Value, 0f, 100f) / 100f * IhSkillPower(player, "ray_of_hope");
            Collider[] allyHits = Physics.OverlapSphere(player.transform.position, radius);
            HashSet<Player> allies = new HashSet<Player>();
            bool ascended = IsAscendedSkill("ray_of_hope");
            for (int i = 0; i < allyHits.Length; i++)
            {
                Player ally = allyHits[i].GetComponentInParent<Player>();
                if (ally == null || allies.Contains(ally)) continue;
                allies.Add(ally);
                Heal(ally, ally.GetMaxHealth() * healPercent);
                DragonCombat.ApplyTimedBuff(ally, "Paladin.RayOfHope", Mathf.Max(0.1f, _rayBuffDuration.Value), Mathf.Max(0f, _rayDamageBuff.Value) / 100f, 0f, 0f, 0f, 0f, 0f, false);
                if (ascended)
                {
                    CleanseAilments(ally);
                    GrantPriestBarrier(ally, Mathf.Max(1f, _rayAscBarrier.Value), 0f, Mathf.Max(0.5f, _rayAscBarrierDuration.Value));
                }
            }
            List<Character> enemies = GetSphereTargets(player, player.transform.position, radius);
            for (int i = 0; i < enemies.Count; i++) RefreshSpiritBurn(player, enemies[i], 1f, Mathf.Max(0.1f, _raySpiritBurnDuration.Value));
            if (_enableVfx.Value) StartCoroutine(AnimateRing(player.transform.position + Vector3.up * 0.12f, 0.6f, radius, 0.8f, new Color(1f, 0.93f, 0.52f, 0.95f), 0.11f));
        }

        private void CastShieldCharge(Player player)
        {
            if (player == null || player.IsDead() || _shieldChargeActive) return;
            Rigidbody body = player.GetComponent<Rigidbody>();
            CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
            // Never fall back to moving the Transform without a physical body.
            if (body == null || capsule == null || !capsule.enabled || capsule.isTrigger)
            {
                ShowMessage("Shield Charge needs a solid player collider");
                return;
            }
            if (!BeginCast(player, "Paladin.ShieldCharge", _shieldChargeCooldown.Value, _shieldChargeStamina.Value)) return;
            _shieldChargeActive = true;
            _shieldChargePlayer = player;
            StartCoroutine(ShieldChargeRoutine(player, body, capsule));
        }

        private IEnumerator ShieldChargeRoutine(Player player, Rigidbody body, CapsuleCollider capsule)
        {
            ShowMessage("Shield Charge");
            Vector3 forward = player.GetLookDir();
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = player.transform.forward;
            forward.Normalize();
            float lastLookYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            bool ascended = IsAscendedSkill("shield_charge");
            float limit = Mathf.Max(1f, ascended ? _chargeAscDistance.Value : _shieldChargeDistance.Value);
            float radius = Mathf.Max(0.5f, ascended ? _chargeAscHitRadius.Value : _shieldChargeRadius.Value);
            float chargeProgress = 0f;
            Dictionary<int, float> nextHitAt = new Dictionary<int, float>();
            Dictionary<int, int> persistentHitCount = new Dictionary<int, int>();
            bool bigTargetReachedCap = false;
            try
            {
                while (player != null && player == Player.m_localPlayer && !player.IsDead() &&
                       body != null && capsule != null && capsule.enabled && GetAdvancement(player) == "Paladin")
                {
                    // Physical blockers stop position but do not pause the 15m skill budget.
                    // Charging into a wall therefore behaves like running on a treadmill.
                    // Dedicated manual finisher: Left Click triggers Shield Bash.
                    // Pressing M4+6 again does nothing while Shield Charge is active.
                    if (Input.GetMouseButtonDown(0))
                    {
                        ShieldChargeBash(player, forward);
                        break;
                    }
                    Vector3 look = player.GetLookDir();
                    look.y = 0f;
                    float lookYaw = look.sqrMagnitude > 0.01f
                        ? Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg : lastLookYaw;
                    float steer = 0f;
                    if (GetShieldChargeButton("Left", KeyCode.A) || GetShieldChargeButton("JoyLeft", KeyCode.None)) steer -= 1f;
                    if (GetShieldChargeButton("Right", KeyCode.D) || GetShieldChargeButton("JoyRight", KeyCode.None)) steer += 1f;
                    float turn = Mathf.DeltaAngle(lastLookYaw, lookYaw) + steer * 120f * Time.fixedDeltaTime;
                    forward = (Quaternion.AngleAxis(turn, Vector3.up) * forward).normalized;
                    lastLookYaw = lookYaw;

                    // Prevent normal controls adding a second, unswept movement.
                    // Look/steering input and the dedicated Left Click Bash input remain available.
                    DragonCombat.LockSkill(player, 0.1f);
                    if (ascended)
                        DragonCombat.GrantHyperArmor(player, 0.25f);
                    float speed = Mathf.Max(1f, player.m_runSpeed) * Mathf.Max(1f, _shieldChargeSpeedMultiplier.Value);
                    float requested = Mathf.Min(speed * Time.fixedDeltaTime, limit - chargeProgress);
                    float step = GetShieldChargeStep(player, body, capsule, forward, requested);
                    body.velocity = new Vector3(0f, body.velocity.y, 0f);
                    body.MoveRotation(Quaternion.LookRotation(forward, Vector3.up));
                    if (step > 0f) body.MovePosition(body.position + forward * step);
                    chargeProgress += requested;

                    // Damage still runs when allowed movement is zero.
                    Vector3 center = player.transform.position + forward * Mathf.Max(0.8f, radius * 0.55f) + Vector3.up;
                    Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        Character target = hits[i].GetComponentInParent<Character>();
                        if (target == null || !IsEnemy(player, target)) continue;
                        int tid = target.GetInstanceID();
                        float allowed;
                        if (nextHitAt.TryGetValue(tid, out allowed) && Time.time < allowed) continue;
                        if (!CanShieldChargeHit(player, target, hits[i], forward)) continue;
                        nextHitAt[tid] = Time.time + Mathf.Max(0.10f, _shieldChargePersistentTick.Value);
                        DealShieldChargeDamage(player, target, forward, false);

                        // Small enemies may be run over for the whole 15m.
                        // Big/Boss targets cap at four Persistent Damage ticks,
                        // then the charge converts immediately into Shield Bash.
                        if (!DragonCombat.IsSmallEnemy(target))
                        {
                            int count = 0;
                            persistentHitCount.TryGetValue(tid, out count);
                            count++;
                            persistentHitCount[tid] = count;
                            if (count >= 4)
                                bigTargetReachedCap = true;
                        }
                    }

                    if (bigTargetReachedCap || chargeProgress >= limit - 0.01f)
                    {
                        ShieldChargeBash(player, forward);
                        break;
                    }

                    yield return new WaitForFixedUpdate();
                }
            }
            finally
            {
                EndShieldCharge();
            }
        }

        private static bool _shieldChargeInputResolved;
        private static MethodInfo _shieldChargeGetButtonMethod;

        private static bool GetShieldChargeButton(string buttonName, KeyCode fallbackKey)
        {
            if (!_shieldChargeInputResolved)
            {
                _shieldChargeInputResolved = true;
                Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < loaded.Length; i++)
                {
                    Type inputType = loaded[i].GetType("ZInput", false);
                    if (inputType == null) continue;
                    _shieldChargeGetButtonMethod = inputType.GetMethod(
                        "GetButton",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new Type[] { typeof(string) },
                        null
                    );
                    if (_shieldChargeGetButtonMethod != null) break;
                }
            }

            if (_shieldChargeGetButtonMethod != null)
            {
                try
                {
                    object value = _shieldChargeGetButtonMethod.Invoke(null, new object[] { buttonName });
                    if (value is bool && (bool)value) return true;
                }
                catch
                {
                    // Keep the charge steerable even if a Valheim input API changes.
                }
            }

            return fallbackKey != KeyCode.None && Input.GetKey(fallbackKey);
        }

        private bool IsShieldChargeSolid(Player player, Collider collider)
        {
            if (collider == null || !collider.enabled || collider.isTrigger) return false;
            if (collider.transform == player.transform || collider.transform.IsChildOf(player.transform)) return false;
            if (collider.GetComponentInParent<Character>() == player) return false;
            return !Physics.GetIgnoreLayerCollision(player.gameObject.layer, collider.gameObject.layer);
        }

        private float GetShieldChargeStep(Player player, Rigidbody body, CapsuleCollider capsule, Vector3 forward, float requested)
        {
            const float skin = 0.05f;
            float step = requested;
            // Sweeps can miss an object overlapping the starting capsule.
            // Allow movement out of an overlap, but never further into it.
            Bounds bounds = capsule.bounds;
            Collider[] nearby = Physics.OverlapBox(bounds.center, bounds.extents + Vector3.one * skin,
                Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < nearby.Length; i++)
            {
                Collider other = nearby[i];
                if (TryShatterOwnedAegis(player, other)) continue;
                if (!IsShieldChargeSolid(player, other)) continue;
                Vector3 separation;
                float depth;
                if (!Physics.ComputePenetration(capsule, capsule.transform.position, capsule.transform.rotation,
                    other, other.transform.position, other.transform.rotation, out separation, out depth)) continue;
                bool ground = other.GetComponentInParent<Character>() == null && separation.y >= 0.65f;
                if (!ground && depth > 0f && Vector3.Dot(forward, separation) < -0.001f) return 0f;
            }
            RaycastHit[] blockers = body.SweepTestAll(forward, requested + skin, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < blockers.Length; i++)
            {
                RaycastHit hit = blockers[i];
                if (TryShatterOwnedAegis(player, hit.collider)) continue;
                if (!IsShieldChargeSolid(player, hit.collider)) continue;
                bool ground = hit.collider.GetComponentInParent<Character>() == null && hit.normal.y >= 0.65f;
                if (ground) continue;
                // A contact behind us must not prevent steering away.
                if (hit.normal.sqrMagnitude > 0.01f && Vector3.Dot(forward, hit.normal) >= -0.001f) continue;
                step = Mathf.Min(step, Mathf.Max(0f, hit.distance - skin));
            }
            return step;
        }

        private bool CanShieldChargeHit(Player player, Character target, Collider targetCollider, Vector3 forward)
        {
            Vector3 origin = player.transform.position + Vector3.up;
            Vector3 point = targetCollider.ClosestPoint(origin);
            Vector3 offset = point - origin;
            if (Vector3.Dot(targetCollider.bounds.center - origin, forward) < 0f) return false;
            float distance = offset.magnitude;
            if (distance < 0.01f) return true;
            // A wall or another solid enemy stops damage: this skill is not Ghost.
            RaycastHit[] blockers = Physics.RaycastAll(origin, offset / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < blockers.Length; i++)
            {
                Collider other = blockers[i].collider;
                if (!IsShieldChargeSolid(player, other)) continue;
                if (other.GetComponentInParent<Character>() == target) continue;
                if (blockers[i].distance < distance - 0.01f) return false;
            }
            return true;
        }

        private void EndShieldCharge()
        {
            if (_shieldChargePlayer != null)
            {
                Rigidbody body = _shieldChargePlayer.GetComponent<Rigidbody>();
                if (body != null) body.velocity = new Vector3(0f, body.velocity.y, 0f);
                DragonCombat.LockSkill(_shieldChargePlayer, 0f);
            }
            _shieldChargePlayer = null;
            _shieldChargeActive = false;
        }

        private void ShieldChargeBash(Player player, Vector3 forward)
        {
            if (player == null || player.IsDead()) return;
            ShowMessage("Shield Bash");
            bool ascended = IsAscendedSkill("shield_charge");
            float radius = Mathf.Max(0.5f, ascended ? _chargeAscBashRadius.Value : _shieldChargeRadius.Value);
            float halfAngle = Mathf.Clamp(_chargeAscBashAngle.Value, 10f, 360f) * 0.5f;
            Vector3 center = ascended
                ? player.transform.position + Vector3.up
                : player.transform.position + forward * Mathf.Max(1f, radius * 0.65f) + Vector3.up;
            Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            HashSet<Character> damaged = new HashSet<Character>();
            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();
                if (target == null || damaged.Contains(target) || !IsEnemy(player, target)) continue;
                if (ascended)
                {
                    // Ascended Bash is a frontal cone.
                    Vector3 flat = target.transform.position - player.transform.position;
                    flat.y = 0f;
                    if (flat.sqrMagnitude > 0.01f && Vector3.Angle(forward, flat) > halfAngle) continue;
                }
                if (!CanShieldChargeHit(player, target, hits[i], forward)) continue;
                damaged.Add(target);
                DealShieldChargeDamage(player, target, forward, true);
                if (ascended && !target.IsBoss() && !DragonCombat.IsSmallEnemy(target))
                    DragonCombat.Stun(target, player.transform.position);
            }
            if (_enableVfx.Value)
            {
                StartCoroutine(AnimateRing(center - Vector3.up * 0.9f, 0.4f, radius, 0.45f,
                    new Color(1f, 0.90f, 0.38f, 0.95f), 0.16f));
                CreateLightning(center, new Color(0.65f, 0.88f, 1f, 1f), 0.25f);
            }
        }

        private void DealShieldChargeDamage(Player player, Character target, Vector3 forward, bool finalBash)
        {
            HitData hit = new HitData();
            float power = DamagePower(player, _shieldChargeDamage);
            hit.m_damage.m_blunt = _shieldChargeDamage.Blunt.Value * power;
            hit.m_damage.m_lightning = _shieldChargeDamage.Lightning.Value * power;
            hit.m_point = target.transform.position;
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            float sign = Vector3.Dot(target.transform.position - player.transform.position, side) >= 0f ? 1f : -1f;
            bool small = DragonCombat.IsSmallEnemy(target);
            hit.m_dir = small ? side * sign : forward;
            hit.m_pushForce = small ? (finalBash ? 85f : 52f) : 0f;
            hit.SetAttacker(player);
            target.Damage(hit);
        }

        private void CastDivineVerdict(Player player)
        {
            const string id = "Paladin.DivineVerdict";
            Vector3 target;
            if (!TryGetPhysicalAimPoint(player, Mathf.Max(1f, _verdictRange.Value), out target))
            {
                ShowMessage("Aim at a physical target");
                return;
            }
            if (!BeginCast(player, id, _verdictCooldown.Value, _verdictStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _verdictWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f);
            StartCoroutine(DivineVerdictRoutine(player, target, windup));
        }

        private IEnumerator DivineVerdictRoutine(Player player, Vector3 target, float windup)
        {
            ShowMessage("Divine Verdict");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead())
                yield break;

            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(target, 8f);
            GameObject hammer = CreateHolyHammer(sky, new Color(1f, 0.86f, 0.38f, 1f));
            float fallTime = DragonCombat.GetSkySummonDropTime();
            float elapsed = 0f;
            while (elapsed < fallTime)
            {
                float progress = DragonCombat.GetSkySummonFallProgress(elapsed / fallTime);
                if (hammer != null)
                    hammer.transform.position = Vector3.Lerp(sky, target + Vector3.up * 2.6f, progress);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (hammer != null)
                Destroy(hammer);

            float radius = Mathf.Max(1f, _verdictRadius.Value);
            List<Character> targets = GetSphereTargets(player, target, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                Character enemy = targets[i];
                if (HasActiveJudgementMark(enemy))
                {
                    TriggerJudgementDetonation(player, enemy);
                    _judgementMarks.Remove(enemy.GetInstanceID());
                }
                DealDamage(player, enemy, _verdictDamage, 34f, false);
            }

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(target + Vector3.up * 0.08f, 0.5f, radius, 0.65f,
                    new Color(1f, 0.82f, 0.34f, 0.95f), 0.16f));
        }

        private GameObject CreateHolyHammer(Vector3 center, Color color)
        {
            GameObject root = new GameObject("DragonsAltarDivineVerdictHammer");
            root.transform.position = center;
            if (!_enableVfx.Value)
                return root;

            Shader shader = Shader.Find("Sprites/Default");
            GameObject shaftObj = new GameObject("shaft");
            shaftObj.transform.SetParent(root.transform, false);
            LineRenderer shaft = shaftObj.AddComponent<LineRenderer>();
            shaft.useWorldSpace = false;
            shaft.positionCount = 2;
            shaft.startWidth = 0.45f;
            shaft.endWidth = 0.45f;
            shaft.startColor = color;
            shaft.endColor = color;
            shaft.SetPosition(0, new Vector3(0f, -3.0f, 0f));
            shaft.SetPosition(1, new Vector3(0f, 2.0f, 0f));
            if (shader != null) shaft.material = new Material(shader);

            GameObject headObj = new GameObject("head");
            headObj.transform.SetParent(root.transform, false);
            LineRenderer head = headObj.AddComponent<LineRenderer>();
            head.useWorldSpace = false;
            head.positionCount = 2;
            head.startWidth = 1.35f;
            head.endWidth = 1.35f;
            head.startColor = color;
            head.endColor = color;
            head.SetPosition(0, new Vector3(-2.8f, 1.8f, 0f));
            head.SetPosition(1, new Vector3(2.8f, 1.8f, 0f));
            if (shader != null) head.material = new Material(shader);
            return root;
        }

        private void CastAegisFall(Player player)
        {
            const string id = "Paladin.AegisFall";
            Vector3 target;
            if (!TryGetPhysicalAimPoint(player, Mathf.Max(1f, _aegisRange.Value), out target))
            {
                ShowMessage("Aim at a physical target");
                return;
            }
            if (!BeginCast(player, id, _aegisCooldown.Value, _aegisStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _aegisWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f);
            StartCoroutine(AegisFallRoutine(player, target, windup));
        }

        private IEnumerator AegisFallRoutine(Player player, Vector3 target, float windup)
        {
            ShowMessage("Aegis Fall");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead())
                yield break;

            float width = Mathf.Max(2f, _aegisWidth.Value);
            float height = Mathf.Max(2f, _aegisHeight.Value);
            Vector3 finalCenter = target + Vector3.up * (height * 0.5f + 0.05f);
            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(finalCenter, 8f);
            GameObject aegis = CreateAegisWall(sky, player, width, height);

            Vector3 toCaster = player.transform.position - finalCenter;
            toCaster.y = 0f;
            Quaternion facing = toCaster.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(toCaster.normalized, Vector3.up)
                : player.transform.rotation;
            if (aegis != null)
                aegis.transform.rotation = facing;

            float fallTime = DragonCombat.GetSkySummonDropTime();
            float elapsed = 0f;
            while (elapsed < fallTime)
            {
                float progress = DragonCombat.GetSkySummonFallProgress(elapsed / fallTime);
                if (aegis != null)
                    aegis.transform.position = Vector3.Lerp(sky, finalCenter, progress);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (aegis != null)
            {
                aegis.transform.position = finalCenter;
                Collider wallCollider = aegis.GetComponentInChildren<Collider>();
                if (wallCollider != null)
                    wallCollider.enabled = true;
                Destroy(aegis, Mathf.Max(1f, _aegisDuration.Value));
            }

            float radius = Mathf.Max(1f, _aegisImpactRadius.Value);
            List<Character> targets = GetSphereTargets(player, target, radius);
            for (int i = 0; i < targets.Count; i++)
                DealDamage(player, targets[i], _aegisDamage, 36f, false);

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(target + Vector3.up * 0.08f, 0.5f, radius, 0.60f,
                    new Color(0.88f, 0.94f, 1f, 0.94f), 0.14f));
        }

        private GameObject CreateAegisWall(Vector3 center, Player caster, float width, float height)
        {
            GameObject root = new GameObject("DragonsAltarAegisFall");
            root.transform.position = center;
            AegisWallMarker marker = root.AddComponent<AegisWallMarker>();
            marker.Caster = caster;
            marker.Shattered = false;

            GameObject wall = new GameObject("wall");
            wall.transform.SetParent(root.transform, false);
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, height, 0.65f);
            collider.center = Vector3.zero;
            collider.isTrigger = false;
            collider.enabled = false;

            if (_enableVfx.Value)
            {
                Shader shader = Shader.Find("Sprites/Default");
                LineRenderer line = wall.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 5;
                line.startWidth = 0.16f;
                line.endWidth = 0.16f;
                Color color = new Color(0.84f, 0.93f, 1f, 0.96f);
                line.startColor = color;
                line.endColor = color;
                float hw = width * 0.5f;
                float hh = height * 0.5f;
                line.SetPosition(0, new Vector3(-hw, -hh, 0f));
                line.SetPosition(1, new Vector3(-hw, hh, 0f));
                line.SetPosition(2, new Vector3(hw, hh, 0f));
                line.SetPosition(3, new Vector3(hw, -hh, 0f));
                line.SetPosition(4, new Vector3(-hw, -hh, 0f));
                if (shader != null) line.material = new Material(shader);
            }
            return root;
        }

        private bool TryShatterOwnedAegis(Player player, Collider collider)
        {
            if (player == null || collider == null)
                return false;
            AegisWallMarker marker = collider.GetComponentInParent<AegisWallMarker>();
            if (marker == null || marker.Caster != player || marker.Shattered)
                return false;
            ShatterAegis(player, marker);
            return true;
        }

        private void ShatterAegis(Player player, AegisWallMarker marker)
        {
            if (marker == null || marker.Shattered)
                return;
            marker.Shattered = true;
            Vector3 center = marker.transform.position - Vector3.up * (Mathf.Max(2f, _aegisHeight.Value) * 0.5f);
            Collider[] colliders = marker.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = false;

            float radius = Mathf.Max(1f, _aegisShockwaveRadius.Value);
            List<Character> targets = GetSphereTargets(player, center, radius);
            for (int i = 0; i < targets.Count; i++)
                DealDamage(player, targets[i], _aegisShockwaveDamage, 44f, false);

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, 0.8f, radius, 0.55f,
                    new Color(1f, 0.90f, 0.48f, 0.96f), 0.18f));
            ShowMessage("Aegis Shattered");
            Destroy(marker.gameObject);
        }

        private void CastElectricSmite(Player player)
        {
            const string id = "Paladin.ElectricSmite";
            if (!BeginCast(player, id, _divineCooldown.Value, _divineStamina.Value))
                return;

            float takeoffDelay = 0.08f;
            DragonCombat.LockSkill(player, takeoffDelay);
            DragonCombat.PlaySkillPose(player, "Slam", 8f);
            StartCoroutine(ElectricSmiteRoutine(player, takeoffDelay));
        }

        private IEnumerator ElectricSmiteRoutine(Player player, float takeoffDelay)
        {
            ShowMessage("Electric Smite");
            yield return StartCoroutine(AcrobaticJumpUntilLanding(player, takeoffDelay, Mathf.Max(1.5f, _divineWindup.Value)));
            if (player == null || player.IsDead())
                yield break;

            DragonCombat.PlaySkillPose(player, "Slam", 0.35f);
            Vector3 point = player.transform.position;
            float radius = Mathf.Max(1f, _divineRadius.Value);
            List<Character> targets = GetSphereTargets(player, point, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                DealDamage(player, targets[i], _divineDamage, 32f, false);
                DragonCombat.ApplyExpose(targets[i], 6f);
                StartCoroutine(FireDot(player, targets[i], _divineFireDot.Value, 6f));
                StartCoroutine(SpiritDot(player, targets[i], _divineSpiritDot.Value, _divineSpiritDuration.Value));
            }

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(point + Vector3.up * 0.08f, 0.4f, radius, 0.52f, new Color(1f, 0.68f, 0.20f, 1f), 0.14f));

            // Sixteen Ground Projectile trails, evenly spaced every 22.5 degrees.
            // A shared persistent-hit gate prevents targets near the origin from being hit
            // sixteen times at once simply because the radial trails overlap there.
            Dictionary<int, float> sharedTrailNextHitAt = new Dictionary<int, float>();
            for (int i = 0; i < 16; i++)
            {
                Vector3 dir = Quaternion.AngleAxis((float)i * 22.5f, Vector3.up) * Vector3.forward;
                StartCoroutine(SpiritTrail(
                    player,
                    point,
                    dir,
                    Mathf.Max(1f, _divineTrailRange.Value),
                    Mathf.Max(0.2f, _divineTrailTravelTime.Value),
                    sharedTrailNextHitAt
                ));
            }

            if (IsAscendedSkill("electric_smite"))
                StartCoroutine(SmiteThunderstormRoutine(player, point));
        }

        // ===== v0.17.0 Paladin rework + Ascended test switch =====

        private bool IsAscendedSkill(string skillId)
        {
            string raw = _testingAscendedSkills == null ? "" : (_testingAscendedSkills.Value ?? "");
            if (!string.Equals(raw, _ascendedCacheRaw, StringComparison.Ordinal))
            {
                _ascendedCacheRaw = raw;
                _ascendedCache.Clear();
                string[] parts = raw.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i].Trim();
                    if (part.Length > 0)
                        _ascendedCache.Add(part);
                }
            }
            if (_ascendedCache.Contains(skillId))
                return true;
            // v0.18.0: real Ascensions saved on the character (Righteous Strike Ascends with Advancement).
            return IhIsAscended(Player.m_localPlayer, skillId);
        }

        // Same algorithm as Valheim's string.GetStableHashCode(), used for status effect names.
        private static int StableHash(string text)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;
                for (int i = 0; i < text.Length && text[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ text[i];
                    if (i == text.Length - 1 || text[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ text[i + 1];
                }
                return hash1 + hash2 * 1566083941;
            }
        }

        private void CleanseAilments(Player ally)
        {
            if (ally == null)
                return;
            int id = ally.GetInstanceID();
            _refreshingFireBurns.Remove(id);
            _refreshingSpiritBurns.Remove(id);
            try
            {
                object seman = ally.GetSEMan();
                if (seman == null)
                    return;
                MethodInfo remove = seman.GetType().GetMethod("RemoveStatusEffect",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { typeof(int), typeof(bool) }, null);
                if (remove == null)
                    return;
                string[] ailments = { "Burning", "Spirit", "Poison", "Frost" };
                for (int i = 0; i < ailments.Length; i++)
                    remove.Invoke(seman, new object[] { StableHash(ailments[i]), false });
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Ray of Hope cleanse failed: " + ex.Message);
            }
        }

        // Ground Projectile trail with configurable damage and Spirit DoT (Ascended Righteous Strike).
        private IEnumerator PaladinTrail(Player player, Vector3 origin, Vector3 forward, float range, float travelTime,
            Dictionary<int, float> sharedNextHitAt, DamageConfig damage, float spiritDps, float spiritDuration, float tick)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();
            int groundMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");

            float elapsed = 0f;
            while (elapsed <= travelTime)
            {
                if (player == null || player.IsDead())
                    yield break;

                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, travelTime));
                Vector3 projected = origin + forward * (range * t);
                RaycastHit ground;
                if (Physics.Raycast(projected + Vector3.up * 8f, Vector3.down, out ground, 24f, groundMask))
                {
                    Vector3 point = ground.point;
                    Collider[] hits = Physics.OverlapSphere(point + Vector3.up * 0.35f, 0.9f, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < hits.Length; i++)
                    {
                        Character target = hits[i].GetComponentInParent<Character>();
                        if (target == null || !IsEnemy(player, target))
                            continue;
                        int targetId = target.GetInstanceID();
                        float nextAllowed;
                        if (sharedNextHitAt.TryGetValue(targetId, out nextAllowed) && Time.time < nextAllowed)
                            continue;
                        sharedNextHitAt[targetId] = Time.time + Mathf.Max(0.10f, tick);
                        DealDamage(player, target, damage, 0f, false);
                        RefreshSpiritBurn(player, target, spiritDps, Mathf.Max(0.1f, spiritDuration));
                    }

                    if (_enableVfx.Value)
                        StartCoroutine(AnimateRing(point + Vector3.up * 0.06f, 0.12f, 0.80f, 0.20f, new Color(0.62f, 0.86f, 1f, 0.82f), 0.05f));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void CastAscendedRighteousStrike(Player player)
        {
            const string id = "Paladin.AscendedRighteousStrike";
            Vector3 target;
            if (!TryGetPhysicalAimPoint(player, Mathf.Max(1f, _rsAscRange.Value), out target))
            {
                ShowMessage("Aim at a physical target");
                return;
            }
            if (!BeginCast(player, id, _rsAscCooldown.Value, _rsAscStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _rsAscWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f);
            StartCoroutine(AscendedRighteousStrikeRoutine(player, target, windup));
        }

        private IEnumerator AscendedRighteousStrikeRoutine(Player player, Vector3 target, float windup)
        {
            ShowMessage("Righteous Strike");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead())
                yield break;

            float radius = Mathf.Max(1f, _rsAscRadius.Value);
            if (_enableVfx.Value)
            {
                CreateLightning(target, new Color(0.62f, 0.88f, 1f, 1f), 0.32f);
                StartCoroutine(AnimateRing(target + Vector3.up * 0.08f, 0.3f, radius, 0.5f, new Color(0.70f, 0.90f, 1f, 0.95f), 0.12f));
            }

            bool anyDetonation = false;
            List<Character> targets = GetSphereTargets(player, target, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                _judgementDetonatedOnLastHit = false;
                _paladinMarkSourceContext = "RighteousStrike";
                try
                {
                    DealDamage(player, targets[i], _rsAscDamage, 14f, false);
                }
                finally
                {
                    _paladinMarkSourceContext = string.Empty;
                }
                if (_judgementDetonatedOnLastHit)
                    anyDetonation = true;
                DragonCombat.ApplyExpose(targets[i], Mathf.Max(0.1f, _rsAscExpose.Value));
            }

            // 12 Lightning Trails in all directions, faster than Electric Smite's.
            Dictionary<int, float> sharedNextHitAt = new Dictionary<int, float>();
            for (int i = 0; i < 12; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(i * 30f, Vector3.up) * Vector3.forward;
                StartCoroutine(PaladinTrail(player, target, dir, Mathf.Max(1f, _rsAscTrailRange.Value), Mathf.Max(0.1f, _rsAscTrailTime.Value),
                    sharedNextHitAt, _rsAscTrailDamage, _rsAscSpiritDot.Value, _rsAscSpiritDuration.Value, _rsAscTrailTick.Value));
            }

            if (!anyDetonation)
                yield break;

            // A Mark detonation calls a second, smaller strike on the same spot.
            yield return new WaitForSeconds(0.5f);
            if (player == null || player.IsDead())
                yield break;
            float followRadius = Mathf.Max(0.5f, _rsAscFollowRadius.Value);
            if (_enableVfx.Value)
            {
                CreateLightning(target, new Color(0.82f, 0.95f, 1f, 1f), 0.25f);
                StartCoroutine(AnimateRing(target + Vector3.up * 0.08f, 0.2f, followRadius, 0.35f, new Color(0.82f, 0.95f, 1f, 0.95f), 0.10f));
            }
            List<Character> follow = GetSphereTargets(player, target, followRadius);
            for (int i = 0; i < follow.Count; i++)
                DealDamageScaled(player, follow[i], _rsAscDamage, Mathf.Max(0f, _rsAscFollowMultiplier.Value), 8f, false);
        }

        private void CastJudgementHammer(Player player)
        {
            const string id = "Paladin.JudgementHammer";
            if (!BeginCast(player, id, _hammerCooldown.Value, _hammerStamina.Value))
                return;
            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _hammerWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Raise", windup + 0.10f);
            StartCoroutine(JudgementHammerRoutine(player, windup));
        }

        private IEnumerator JudgementHammerRoutine(Player player, float windup)
        {
            const string id = "Paladin.JudgementHammer";
            ShowMessage("Judgement Hammer");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead())
                yield break;

            bool ascended = IsAscendedSkill("judgement_hammer");
            Vector3 dir = player.GetLookDir();
            if (dir.sqrMagnitude < 0.01f)
                dir = player.transform.forward;
            dir.Normalize();
            Vector3 pos = player.transform.position + Vector3.up * 1.4f + dir * 0.8f;
            float range = Mathf.Max(1f, _hammerRange.Value);
            float speed = range / Mathf.Max(0.1f, _hammerTravelTime.Value);
            float stepMeters = Mathf.Max(0.05f, _hammerStepMeters.Value);
            int solidMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            Dictionary<int, float> nextHitAt = new Dictionary<int, float>();
            GameObject hammer = CreateHolyHammer(pos, new Color(1f, 0.86f, 0.38f, 1f));
            float spin = 0f;
            float travelled = 0f;
            float flightTime = 0f;
            float damageMultiplier = 1f;
            float height = Mathf.Max(0.5f, _hammerStartHeight.Value);
            float width = Mathf.Max(0.2f, _hammerStartWidth.Value);

            // Laser Projectile: straight line, no fall-off. Passes through enemies, stops at walls/objects.
            // Grows every GrowthInterval seconds, capped (normal 8m x 2m, Ascended 10m x 3m).
            while (travelled < range && player != null && !player.IsDead())
            {
                float step = Mathf.Min(speed * Time.deltaTime, range - travelled);
                RaycastHit wall;
                bool blocked = Physics.Raycast(pos, dir, out wall, step + 0.05f, solidMask, QueryTriggerInteraction.Ignore) &&
                               wall.collider.GetComponentInParent<Character>() == null;
                pos = blocked ? wall.point - dir * 0.05f : pos + dir * step;
                travelled += blocked ? wall.distance : step;
                flightTime += Time.deltaTime;

                bool capped = UpdateHammerSize(flightTime, ascended, out height, out width);
                if (!capped)
                    damageMultiplier = Mathf.Min(Mathf.Max(1f, _hammerDamageCap.Value), 1f + Mathf.Max(0f, _hammerDamagePerStep.Value) * Mathf.FloorToInt(travelled / stepMeters));
                spin += 360f * Time.deltaTime;
                Vector3 center = HammerCenter(pos, height);
                UpdateHammerVisual(hammer, center, dir, spin, height, width);
                HammerHits(player, center, HammerHitRadius(height), damageMultiplier, nextHitAt, ascended);

                if (blocked)
                    break;
                yield return null;
            }

            if (ascended && player != null && !player.IsDead())
            {
                // Ascended: flies back to the Paladin, still growing up to its cap, hitting everything again.
                nextHitAt.Clear();
                float safety = Time.time + 8f;
                while (player != null && !player.IsDead() && Time.time < safety)
                {
                    Vector3 home = player.transform.position + Vector3.up * 1.2f;
                    Vector3 toHome = home - pos;
                    if (toHome.magnitude < 1.5f)
                    {
                        float end;
                        if (_cooldowns.TryGetValue(id, out end))
                        {
                            float total = _testingForceCooldowns.Value ? _testingCooldownSeconds.Value : _hammerCooldown.Value;
                            float cut = Mathf.Max(0f, total) * Mathf.Clamp01(_hammerCatchCooldownCut.Value / 100f);
                            _cooldowns[id] = Mathf.Max(Time.time, end - cut);
                        }
                        ShowMessage("Hammer caught");
                        break;
                    }
                    Vector3 back = toHome.normalized;
                    float move = Mathf.Min(speed * Time.deltaTime, toHome.magnitude);
                    pos += back * move;
                    travelled += move;
                    flightTime += Time.deltaTime;
                    bool capped = UpdateHammerSize(flightTime, true, out height, out width);
                    if (!capped)
                        damageMultiplier = Mathf.Min(Mathf.Max(1f, _hammerDamageCap.Value), 1f + Mathf.Max(0f, _hammerDamagePerStep.Value) * Mathf.FloorToInt(travelled / stepMeters));
                    spin += 360f * Time.deltaTime;
                    Vector3 center = HammerCenter(pos, height);
                    UpdateHammerVisual(hammer, center, back, spin, height, width);
                    HammerHits(player, center, HammerHitRadius(height), damageMultiplier, nextHitAt, true);
                    yield return null;
                }
            }

            if (hammer != null)
                Destroy(hammer);
        }

        // Returns true once the size cap is reached.
        private bool UpdateHammerSize(float flightTime, bool ascended, out float height, out float width)
        {
            int steps = Mathf.FloorToInt(flightTime / Mathf.Max(0.05f, _hammerGrowthInterval.Value));
            float widthStep = Mathf.Max(0f, ascended ? _hammerAscWidthPerStep.Value : _hammerWidthPerStep.Value);
            float rawHeight = Mathf.Max(0.5f, _hammerStartHeight.Value) + Mathf.Max(0f, _hammerHeightPerStep.Value) * steps;
            float rawWidth = Mathf.Max(0.2f, _hammerStartWidth.Value) + widthStep * steps;
            // v0.21.2: no size cap (the hitbox keeps growing); damage is capped separately.
            height = rawHeight;
            width = rawWidth;
            return false;
        }

        // Lift the spin center so the flipping hammer does not dig into the ground.
        private Vector3 HammerCenter(Vector3 pos, float height)
        {
            return pos + Vector3.up * Mathf.Max(0f, height * 0.5f - 1.4f);
        }

        private float HammerHitRadius(float height)
        {
            return Mathf.Max(Mathf.Max(0.2f, _hammerBaseRadius.Value), height * Mathf.Max(0f, _hammerHitRadiusPerHeight.Value));
        }

        private void UpdateHammerVisual(GameObject hammer, Vector3 pos, Vector3 dir, float spin, float height, float width)
        {
            if (hammer == null)
                return;
            hammer.transform.position = pos;
            Vector3 flat = dir;
            flat.y = 0f;
            Quaternion facing = flat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flat.normalized, Vector3.up) : Quaternion.identity;
            // Upright hammer doing continuous front flips: spin around the axis across the flight path.
            hammer.transform.rotation = facing * Quaternion.AngleAxis(spin, Vector3.right);
            hammer.transform.localScale = Vector3.one;

            // Real meters: shaft along local Y, head across local X (width), head on top.
            float half = height * 0.5f;
            float headThick = height * 0.22f;
            Transform shaftT = hammer.transform.Find("shaft");
            Transform headT = hammer.transform.Find("head");
            LineRenderer shaft = shaftT == null ? null : shaftT.GetComponent<LineRenderer>();
            LineRenderer head = headT == null ? null : headT.GetComponent<LineRenderer>();
            if (shaft != null)
            {
                float shaftWidth = Mathf.Max(0.1f, width * 0.16f);
                shaft.startWidth = shaftWidth;
                shaft.endWidth = shaftWidth;
                shaft.SetPosition(0, new Vector3(0f, -half, 0f));
                shaft.SetPosition(1, new Vector3(0f, half - headThick, 0f));
            }
            if (head != null)
            {
                head.startWidth = headThick;
                head.endWidth = headThick;
                head.SetPosition(0, new Vector3(-width * 0.5f, half - headThick * 0.5f, 0f));
                head.SetPosition(1, new Vector3(width * 0.5f, half - headThick * 0.5f, 0f));
            }
        }

        private void HammerHits(Player player, Vector3 pos, float radius, float damageMultiplier, Dictionary<int, float> nextHitAt, bool marks)
        {
            List<Character> targets = GetSphereTargets(player, pos, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                Character target = targets[i];
                int tid = target.GetInstanceID();
                float allowed;
                if (nextHitAt.TryGetValue(tid, out allowed) && Time.time < allowed)
                    continue;
                nextHitAt[tid] = Time.time + Mathf.Max(0.1f, _hammerTick.Value);
                if (marks)
                    _paladinMarkSourceContext = "JudgementHammer";
                try
                {
                    DealDamageScaled(player, target, _hammerDamage, damageMultiplier, 6f, false);
                }
                finally
                {
                    _paladinMarkSourceContext = string.Empty;
                }
                DragonCombat.ApplyCripple(target, Mathf.Max(0.1f, _hammerCrippleDuration.Value));
            }
        }

        private void CastFallenAngel(Player player)
        {
            const string id = "Paladin.FallenAngel";
            Rigidbody body = player == null ? null : player.GetComponent<Rigidbody>();
            if (body == null)
                return;
            if (!BeginCast(player, id, _angelCooldown.Value, _angelStamina.Value))
                return;
            DragonCombat.LockSkill(player, 0.1f);
            DragonCombat.PlaySkillPose(player, "Raise", Mathf.Max(0.6f, _angelWindupTotal.Value) * 0.56f + 0.2f);
            StartCoroutine(FallenAngelRoutine(player, body));
        }

        private IEnumerator FallenAngelRoutine(Player player, Rigidbody body)
        {
            ShowMessage("Angel Comet");
            bool ascended = IsAscendedSkill("fallen_angel");
            Vector3 forward = player.GetLookDir();
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = player.transform.forward;
            forward.Normalize();

            ResetFallDamageState(player);
            // v0.21.1: jump + nose-dive take WindUpTime (2.5s) in total.
            float total = Mathf.Max(0.6f, _angelWindupTotal.Value);
            float riseTime = total * 0.56f;
            float hangTime = total * 0.06f;
            float height = Mathf.Max(1f, _angelJumpHeight.Value);
            Vector3 start = body.position;
            float riseStart = Time.time;

            // Leap high (slight forward drift so the dive lands ahead of the Paladin).
            while (player != null && !player.IsDead())
            {
                float t = Mathf.Clamp01((Time.time - riseStart) / riseTime);
                float eased = Mathf.Sin(t * Mathf.PI * 0.5f);
                body.MovePosition(start + Vector3.up * (height * eased) + forward * (2.5f * t));
                body.velocity = Vector3.zero;
                ResetFallDamageState(player);
                DragonCombat.LockSkill(player, 0.12f);
                if (ascended)
                    DragonCombat.GrantHyperArmor(player, 0.3f);
                if (t >= 1f)
                    break;
                yield return new WaitForFixedUpdate();
            }

            // Short hang, then a head-first nose-dive until physical landing.
            float hangEnd = Time.time + hangTime;
            while (player != null && !player.IsDead() && Time.time < hangEnd)
            {
                body.velocity = Vector3.zero;
                ResetFallDamageState(player);
                DragonCombat.LockSkill(player, 0.12f);
                yield return new WaitForFixedUpdate();
            }

            DragonCombat.PlaySkillPose(player, "Slam", 3f);
            float diveSpeed = Mathf.Max(3f, height / Mathf.Max(0.1f, total - riseTime - hangTime));
            float safety = Time.time + 10f;
            while (player != null && !player.IsDead() && Time.time < safety)
            {
                body.velocity = forward * 3f + Vector3.down * diveSpeed;
                ResetFallDamageState(player);
                DragonCombat.LockSkill(player, 0.12f);
                if (ascended)
                    DragonCombat.GrantHyperArmor(player, 0.3f);
                if (IsPlayerGrounded(player) && Time.time > hangEnd + 0.05f)
                    break;
                yield return new WaitForFixedUpdate();
            }
            if (player == null || player.IsDead())
                yield break;

            ResetFallDamageState(player);
            body.velocity = Vector3.zero;
            DragonCombat.LockSkill(player, 0.35f);
            DragonCombat.PlaySkillPose(player, "Slam", 0.35f);

            Vector3 point = player.transform.position;
            float radius = Mathf.Max(1f, _angelRadius.Value);
            List<Character> targets = GetSphereTargets(player, point, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                DealDamage(player, targets[i], _angelDamage, 30f, true);
                DragonCombat.ApplyBrokenBones(targets[i], Mathf.Max(0.1f, _angelBrokenBones.Value));
                DragonCombat.Stun(targets[i], point);
            }
            if (_enableVfx.Value)
            {
                CreateLightning(point, new Color(1f, 0.92f, 0.62f, 1f), 0.3f);
                StartCoroutine(AnimateRing(point + Vector3.up * 0.08f, 0.4f, radius, 0.55f, new Color(1f, 0.88f, 0.48f, 1f), 0.16f));
            }

            if (ascended)
            {
                DragonCombat.GrantHyperArmor(player, Mathf.Max(0f, _angelHyperAfter.Value));
                StartCoroutine(FallenAngelBurnRingRoutine(player, point));
            }
        }

        private IEnumerator FallenAngelBurnRingRoutine(Player player, Vector3 center)
        {
            float radius = Mathf.Max(1f, _angelRingRadius.Value);
            float end = Time.time + Mathf.Max(0.5f, _angelRingDuration.Value);
            float burn = Mathf.Max(0.1f, _angelBurnDuration.Value);
            while (Time.time < end && player != null)
            {
                List<Character> targets = GetSphereTargets(player, center, radius);
                for (int i = 0; i < targets.Count; i++)
                {
                    // Spirit Burn + Fire Burn, 3s, refreshed while the enemy stays inside the ring.
                    RefreshSpiritBurn(player, targets[i], _angelSpiritDot.Value, burn);
                    RefreshFireBurn(player, targets[i], _angelFireDot.Value, burn);
                }
                if (_enableVfx.Value)
                    StartCoroutine(AnimateRing(center + Vector3.up * 0.06f, radius * 0.92f, radius, 0.45f, new Color(1f, 0.55f, 0.25f, 0.85f), 0.10f));
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator SmiteThunderstormRoutine(Player player, Vector3 center)
        {
            float radius = Mathf.Max(1f, _smiteStormRadius.Value);
            float end = Time.time + Mathf.Max(0.5f, _smiteStormDuration.Value);
            float tick = Mathf.Max(0.1f, _smiteStormTick.Value);
            float dot = Mathf.Max(0.1f, _smiteStormDotDuration.Value);
            while (Time.time < end && player != null && !player.IsDead())
            {
                List<Character> targets = GetSphereTargets(player, center, radius);
                for (int i = 0; i < targets.Count; i++)
                {
                    DealDamage(player, targets[i], _smiteStormDamage, 0f, false);
                    RefreshFireBurn(player, targets[i], _smiteStormFireDot.Value, dot);
                    RefreshSpiritBurn(player, targets[i], _smiteStormSpiritDot.Value, dot);
                }
                if (_enableVfx.Value)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        Vector2 r = UnityEngine.Random.insideUnitCircle * radius;
                        CreateLightning(center + new Vector3(r.x, 0f, r.y), new Color(0.62f, 0.86f, 1f, 1f), 0.22f);
                    }
                }
                yield return new WaitForSeconds(tick);
            }
        }

        private IEnumerator AcrobaticJumpUntilLanding(Player player, float takeoffDelay, float flatAirTime)
        {
            if (takeoffDelay > 0f)
                yield return new WaitForSeconds(takeoffDelay);
            if (player == null || player.IsDead())
                yield break;

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body == null)
                yield break;

            ResetFallDamageState(player);

            float height = Mathf.Clamp(_acrobaticJumpHeight.Value, 1.5f, 3.0f);
            float targetAir = Mathf.Max(1.5f, flatAirTime);
            float ascentDuration = targetAir * 0.55f;
            float hangDuration = targetAir * 0.20f;
            float startY = body.position.y;
            float peakY = startY + height;
            float ascentStart = Time.time;

            // Guaranteed cinematic takeoff. We directly drive only the Y axis while
            // Valheim keeps normal horizontal movement, so the player can steer during ascent.
            // This avoids the previous bug where Valheim immediately cancelled the velocity launch.
            DragonCombat.BeginMobileCast(player, ascentDuration + hangDuration + 0.10f, false);
            while (player != null && !player.IsDead())
            {
                float elapsed = Time.time - ascentStart;
                if (elapsed >= ascentDuration)
                    break;

                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, ascentDuration));
                float eased = Mathf.Sin(t * Mathf.PI * 0.5f);
                Vector3 pos = body.position;
                float desiredY = Mathf.Lerp(startY, peakY, eased);
                body.MovePosition(new Vector3(pos.x, desiredY, pos.z));
                Vector3 v = body.velocity;
                v.y = Mathf.Max(0f, (peakY - desiredY) / Mathf.Max(0.05f, ascentDuration - elapsed));
                body.velocity = v;
                ResetFallDamageState(player);
                yield return new WaitForFixedUpdate();
            }

            // Hidden Featherfall-like hangtime: hold the peak briefly with no status icon.
            float hangEnd = Time.time + hangDuration;
            while (player != null && !player.IsDead() && Time.time < hangEnd)
            {
                Vector3 pos = body.position;
                body.MovePosition(new Vector3(pos.x, peakY, pos.z));
                Vector3 v = body.velocity;
                v.y = 0f;
                body.velocity = v;
                ResetFallDamageState(player);
                yield return new WaitForFixedUpdate();
            }

            // Apex reached: maneuvering ends. From this point the skill is committed
            // descent/free-fall and can last much longer than two seconds if cast off a cliff.
            DragonCombat.EndMobileCast(player);
            Vector3 releaseVelocity = body.velocity;
            if (releaseVelocity.y > -0.5f)
                releaseVelocity.y = -0.5f;
            body.velocity = releaseVelocity;

            float landingSafety = Time.time + 30f;
            while (player != null && !player.IsDead() && Time.time < landingSafety)
            {
                ResetFallDamageState(player);
                DragonCombat.LockSkill(player, 0.12f);

                if (IsPlayerGrounded(player) && body.velocity.y <= 0.25f)
                    break;

                yield return new WaitForFixedUpdate();
            }

            ResetFallDamageState(player);
            DragonCombat.EndMobileCast(player);
        }

        private bool IsPlayerGrounded(Player player)
        {
            if (player == null)
                return false;

            try
            {
                MethodInfo method = typeof(Character).GetMethod("IsOnGround", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    return Convert.ToBoolean(method.Invoke(player, null));
            }
            catch
            {
            }

            RaycastHit hit;
            return Physics.Raycast(player.transform.position + Vector3.up * 0.2f, Vector3.down, out hit, 0.45f, ~0, QueryTriggerInteraction.Ignore);
        }

        private void ResetFallDamageState(Player player)
        {
            if (player == null)
                return;

            ResetFloatField(player, "m_maxAirAltitude", player.transform.position.y);
            ResetFloatField(player, "m_lastGroundHeight", player.transform.position.y);
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
            catch
            {
            }
        }

        private void ResetBoolField(Player player, string fieldName, bool value)
        {
            try
            {
                FieldInfo field = typeof(Character).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(bool))
                    field.SetValue(player, value);
            }
            catch
            {
            }
        }

        private void CastLightningRelic(Player player)
        {
            const string id = "Priest.LightningRelic";

            PriestRelicState active = FindPriestRelic(true);
            if (active != null)
            {
                RelinquishPriestRelic(active);
                ShowMessage("Lightning Relic relinquished");
                return;
            }

            if (_lightningRelicCasting)
                return;

            Vector3 target;
            float range = Mathf.Max(1f, _lightningRelicRange.Value);
            if (!TryGetPhysicalAimPoint(player, range, out target))
            {
                ShowMessage("Aim at a physical target");
                return;
            }

            if (!BeginPriestRelicCast(player, id, _lightningRelicStamina.Value))
                return;

            _lightningRelicCasting = true;
            float windup = DragonCombat.ScaleWindup(player, 1.5f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f);
            StartCoroutine(LightningRelicRoutine(player, target, windup, id));
        }

        private IEnumerator LightningRelicRoutine(Player player, Vector3 target, float windup, string cooldownId)
        {
            ShowMessage("Lightning Relic");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            if (player == null || player.IsDead())
            {
                _lightningRelicCasting = false;
                SetPriestCooldownNow(cooldownId, _lightningRelicCooldown.Value);
                yield break;
            }

            float duration = Mathf.Max(0.5f, _lightningRelicDuration.Value);
            float interval = Mathf.Max(0.1f, _lightningRelicInterval.Value);
            bool ascended = IsAscendedSkill("lightning_relic");
            float radius = Mathf.Max(1f, ascended ? _relicAscRadius.Value : _lightningRelicRadius.Value);
            const float crossHeight = 4.2f;
            Vector3 finalCenter = GetGroundedCrossCenter(target, crossHeight);
            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(finalCenter, 5f);
            GameObject cross = CreateCross(
                sky,
                new Color(0.55f, 0.88f, 1f, 1f),
                crossHeight,
                2.5f,
                0.09f,
                _enableVfx.Value
            );

            float drop = DragonCombat.GetSkySummonDropTime();
            float e = 0f;
            while (e < drop)
            {
                if (player == null || player.IsDead())
                {
                    if (cross != null)
                        Destroy(cross);
                    _lightningRelicCasting = false;
                    SetPriestCooldownNow(cooldownId, _lightningRelicCooldown.Value);
                    yield break;
                }

                float progress = DragonCombat.GetSkySummonFallProgress(e / drop);
                if (cross != null)
                    cross.transform.position = Vector3.Lerp(sky, finalCenter, progress);
                e += Time.deltaTime;
                yield return null;
            }

            if (cross != null)
            {
                cross.transform.position = finalCenter;
                SetCrossPhysical(cross, true);
            }

            PriestRelicState relic = RegisterPriestRelic(
                cross,
                target,
                radius,
                duration,
                true,
                cooldownId,
                _lightningRelicCooldown.Value
            );
            _lightningRelicCasting = false;

            float nextPulse = Time.time;
            while (relic != null && Time.time < relic.EndTime && relic.Cross != null)
            {
                if (player == null || player.IsDead())
                    break;

                if (Time.time >= nextPulse)
                {
                    List<Character> targets = GetSphereTargets(player, target, radius);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        bool consecrated = IsInsideConsecratedGround(targets[i].transform.position);
                        float multiplier = consecrated ? Mathf.Max(1f, _consecratedMultiplier.Value) : 1f;
                        DealDamageScaled(player, targets[i], _lightningRelicDamage, multiplier, 6f, false);
                        DragonCombat.ApplyCripple(targets[i], 6f);
                        if (consecrated)
                            DragonCombat.ApplyExpose(targets[i], Mathf.Max(0.1f, _consecratedExposeDuration.Value));
                    }
                    if (ascended)
                    {
                        // Arcs to up to 3 more enemies just past the radius; Sanctifies allies inside.
                        List<Character> outer = GetSphereTargets(player, target, radius + Mathf.Max(0f, _relicAscChainRange.Value));
                        int arcs = 0;
                        for (int i = 0; i < outer.Count && arcs < 3; i++)
                        {
                            if (targets.Contains(outer[i]))
                                continue;
                            DealDamageScaled(player, outer[i], _lightningRelicDamage, 1f, 6f, false);
                            if (_enableVfx.Value)
                                CreateTemporaryBeam(target + Vector3.up * 3f, outer[i].transform.position + Vector3.up, new Color(0.55f, 0.88f, 1f, 0.95f), 0.10f, 0.15f);
                            arcs++;
                        }
                        List<Player> blessed = IhAlliesInRadius(player, target, radius);
                        for (int i = 0; i < blessed.Count; i++)
                            IhSanctify(player, blessed[i]);
                    }

                    if (_enableVfx.Value)
                    {
                        CreateLightning(target, new Color(0.48f, 0.82f, 1f, 0.92f), Mathf.Min(0.22f, interval * 0.45f));
                        StartCoroutine(AnimateRing(target + Vector3.up * 0.08f, 0.3f, radius, Mathf.Min(0.32f, interval), new Color(0.58f, 0.90f, 1f, 0.65f), 0.05f));
                        PulseConsecratedGroundVfx();
                    }

                    nextPulse = Time.time + interval;
                }

                yield return null;
            }

            if (ascended && player != null && !player.IsDead())
            {
                // The Cross detonates when it ends or is relinquished.
                float blast = Mathf.Max(1f, _relicAscBlastRadius.Value);
                if (_enableVfx.Value)
                {
                    CreateLightning(target, new Color(0.55f, 0.88f, 1f, 1f), 0.35f);
                    StartCoroutine(AnimateRing(target + Vector3.up * 0.08f, 0.5f, blast, 0.45f, new Color(0.58f, 0.90f, 1f, 0.95f), 0.12f));
                }
                List<Character> hit = GetSphereTargets(player, target, blast);
                for (int i = 0; i < hit.Count; i++)
                {
                    DealDamageScaled(player, hit[i], _relicAscBlastDamage, 1f, 10f, false);
                    if (DragonCombat.IsSmallEnemy(hit[i]))
                        DragonCombat.Stun(hit[i], target);
                }
            }
            FinishPriestRelic(relic);
        }

        private void CastHolyRelic(Player player)
        {
            const string id = "Priest.HolyRelic";

            PriestRelicState active = FindPriestRelic(false);
            if (active != null)
            {
                RelinquishPriestRelic(active);
                ShowMessage("Holy Relic relinquished");
                return;
            }

            if (_holyRelicCasting)
                return;

            Vector3 target;
            if (!TryGetPhysicalAimPoint(player, Mathf.Max(1f, _holyRelicRange.Value), out target))
            {
                ShowMessage("Aim at a physical target");
                return;
            }

            if (!BeginPriestRelicCast(player, id, _holyRelicStamina.Value))
                return;

            _holyRelicCasting = true;
            float windup = DragonCombat.ScaleWindup(player, 1f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "SkyCast", windup + 0.10f);
            StartCoroutine(HolyRelicRoutine(player, target, windup, id));
        }

        private IEnumerator HolyRelicRoutine(Player player, Vector3 target, float windup, string cooldownId)
        {
            ShowMessage("Holy Relic");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            if (player == null || player.IsDead())
            {
                _holyRelicCasting = false;
                SetPriestCooldownNow(cooldownId, _holyRelicCooldown.Value);
                yield break;
            }

            float duration = Mathf.Max(2f, _holyRelicDuration.Value);
            float interval = Mathf.Max(0.5f, _holyRelicInterval.Value);
            bool ascended = IsAscendedSkill("holy_relic");
            float radius = Mathf.Max(1f, ascended ? _relicAscRadius.Value : _holyRelicRadius.Value);
            // Ascended: buffs 30% instead of 20% (same ratio for every buff).
            float buffScale = ascended ? Mathf.Max(0f, _holyRelicAscBuff.Value) / 20f : 1f;
            const float crossHeight = 4.2f;
            Vector3 finalCenter = GetGroundedCrossCenter(target, crossHeight);
            Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(finalCenter, 5f);
            GameObject cross = CreateCross(
                sky,
                new Color(1f, 0.90f, 0.45f, 1f),
                crossHeight,
                2.7f,
                0.10f,
                _enableVfx.Value
            );

            float drop = DragonCombat.GetSkySummonDropTime();
            float e = 0f;
            while (e < drop)
            {
                if (player == null || player.IsDead())
                {
                    if (cross != null)
                        Destroy(cross);
                    _holyRelicCasting = false;
                    SetPriestCooldownNow(cooldownId, _holyRelicCooldown.Value);
                    yield break;
                }

                float progress = DragonCombat.GetSkySummonFallProgress(e / drop);
                if (cross != null)
                    cross.transform.position = Vector3.Lerp(sky, finalCenter, progress);
                e += Time.deltaTime;
                yield return null;
            }

            if (cross != null)
            {
                cross.transform.position = finalCenter;
                SetCrossPhysical(cross, true);
            }

            PriestRelicState relic = RegisterPriestRelic(
                cross,
                target,
                radius,
                duration,
                false,
                cooldownId,
                _holyRelicCooldown.Value
            );
            _holyRelicCasting = false;

            float nextPulse = Time.time;
            while (relic != null && Time.time < relic.EndTime && relic.Cross != null)
            {
                if (player == null || player.IsDead())
                    break;

                if (Time.time >= nextPulse)
                {
                    List<Player> players = GetPlayersInSphere(target, radius);
                    if (!players.Contains(player) && Vector3.Distance(player.transform.position, target) <= radius)
                        players.Add(player);

                    for (int i = 0; i < players.Count; i++)
                    {
                        Player ally = players[i];
                        bool consecrated = IsInsideConsecratedGround(ally.transform.position);
                        float multiplier = consecrated ? Mathf.Max(1f, _consecratedMultiplier.Value) : 1f;
                        Heal(ally, ally.GetMaxHealth() * Mathf.Max(0f, _holyRelicHealPercent.Value) * multiplier * IhSkillPower(player, "holy_relic") / 100f);
                        if (ascended)
                        {
                            CleanseAilments(ally);
                            IhSanctify(player, ally);
                        }
                        DragonCombat.ApplyTimedBuff(
                            ally,
                            "Priest.HolyRelic",
                            Mathf.Max(0.1f, _holyRelicBuffDuration.Value),
                            Mathf.Max(0f, _holyRelicDamageBuff.Value) * multiplier * buffScale / 100f,
                            Mathf.Max(0f, _holyRelicAttackSpeedBuff.Value) * multiplier * buffScale / 100f,
                            Mathf.Max(0f, _holyRelicMoveSpeedBuff.Value) * multiplier * buffScale / 100f,
                            Mathf.Clamp(_holyRelicDefenseBuff.Value * multiplier * buffScale, 0f, 95f) / 100f,
                            Mathf.Max(0f, _holyRelicRegenBuff.Value) / 100f,
                            0f,
                            true
                        );
                    }

                    if (_enableVfx.Value)
                    {
                        StartCoroutine(AnimateRing(target + Vector3.up * 0.10f, 0.6f, radius, 0.70f, new Color(1f, 0.86f, 0.35f, 0.92f), 0.10f));
                        PulseConsecratedGroundVfx();
                    }

                    nextPulse = Time.time + interval;
                }

                yield return null;
            }

            if (ascended && player != null && !player.IsDead())
            {
                // Final blessing when the Cross ends or is relinquished.
                List<Player> blessed = IhAlliesInRadius(player, target, radius);
                for (int i = 0; i < blessed.Count; i++)
                    Heal(blessed[i], blessed[i].GetMaxHealth() * Mathf.Max(0f, _holyRelicAscEndHeal.Value) / 100f);
                if (_enableVfx.Value)
                    StartCoroutine(AnimateRing(target + Vector3.up * 0.08f, 0.5f, radius, 0.6f, new Color(1f, 0.92f, 0.50f, 0.95f), 0.12f));
            }
            FinishPriestRelic(relic);
        }

        private void CastDivineIntervention(Player player)
        {
            const string id = "Priest.DivineIntervention";
            bool crossCast;
            Vector3 center = GetPriestSelfOrCrossCastCenter(player, Mathf.Max(1f, _interventionRange.Value), out crossCast);

            if (!BeginCast(player, id, _interventionCooldown.Value, _interventionStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _interventionWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Wave", windup + 0.10f);
            StartCoroutine(DivineInterventionRoutine(player, center, windup, crossCast));
            // Ascended: a Cross Cast while both Relics stand also fires from the other Relic.
            if (crossCast && IsAscendedSkill("divine_intervention"))
            {
                PriestRelicState lightning = FindPriestRelic(true), holy = FindPriestRelic(false);
                if (lightning != null && holy != null)
                {
                    Vector3 other = Vector3.Distance(lightning.Position, center) < 0.5f ? holy.Position : lightning.Position;
                    StartCoroutine(DivineInterventionRoutine(player, other, windup, true));
                }
            }
        }

        private IEnumerator DivineInterventionRoutine(Player player, Vector3 center, float windup, bool crossCast)
        {
            ShowMessage(crossCast ? "Divine Intervention - Cross Cast" : "Divine Intervention");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead())
                yield break;

            float radius = Mathf.Max(1f, _interventionRadius.Value);

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, 0.8f, radius, 0.65f, new Color(1f, 0.92f, 0.48f, 0.95f), 0.13f));

            List<Character> enemies = GetSphereTargets(player, center, radius);
            for (int i = 0; i < enemies.Count; i++)
            {
                DealDamageScaled(player, enemies[i], _interventionDamage, 1f, 12f, false);
                DragonCombat.ApplyExpose(enemies[i], Mathf.Max(0.1f, _interventionExposeDuration.Value));
                // Ascended: enemies are yanked toward the centre (stagger aimed inward).
                if (IsAscendedSkill("divine_intervention") && !enemies[i].IsBoss())
                    DragonCombat.Stun(enemies[i], enemies[i].transform.position * 2f - center);
            }

            List<Player> allies = GetPlayersInSphere(center, radius);
            if (!allies.Contains(player) && Vector3.Distance(player.transform.position, center) <= radius)
                allies.Add(player);

            for (int i = 0; i < allies.Count; i++)
            {
                Player ally = allies[i];
                // v0.21.1: heal scales from HealPercent (Tier 0) to HealPercentAtMaxTier (Tier 5).
                float diTier = Mathf.Clamp01(IhGetTier(player, "divine_intervention") / (float)Mathf.Max(1, IhMaxTier("divine_intervention")));
                float diHeal = Mathf.Lerp(Mathf.Max(0f, _interventionHealPercent.Value), Mathf.Max(0f, _diHealAtMax.Value), diTier);
                Heal(ally, ally.GetMaxHealth() * diHeal / 100f);
                DragonCombat.ApplyTimedBuff(
                    ally,
                    "Priest.DivineIntervention",
                    Mathf.Max(0.1f, _interventionBuffDuration.Value),
                    0f,
                    0f,
                    0f,
                    0.20f,
                    0f,
                    0f,
                    true
                );
                float barrierHp = IsAscendedSkill("divine_intervention") ? _diAscBarrier.Value : _interventionBarrierHp.Value;
                GrantPriestBarrier(ally, Mathf.Max(1f, barrierHp), GetArmor(player) * Mathf.Max(0f, _interventionBarrierArmor.Value) / 100f, Mathf.Max(1f, _interventionBuffDuration.Value));
            }
        }

        private void CastGrandCross(Player player)
        {
            const string id = "Priest.GrandCross";
            if (!BeginCast(player, id, _grandCrossCooldown.Value, _grandCrossStamina.Value))
                return;

            // Grand Cross is self-cast only. Signature Crosses may reposition
            // Divine Intervention / Heaven's Judgement, but never Grand Cross.
            Vector3 origin = player.transform.position + Vector3.up * 2.5f;
            Vector3 forward = AlbedoAimUtility.GetProjectileDirection(player, player.GetEyePoint());
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = FlatForward(player);
            forward.Normalize();

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _grandCrossWindup.Value));
            DragonCombat.LockSkill(player, windup);
            StartCoroutine(GrandCrossRoutine(player, origin, forward, windup));
        }

        private IEnumerator GrandCrossRoutine(Player player, Vector3 origin, Vector3 forward, float windup)
        {
            ShowMessage("Grand Cross");

            float firstSlash = windup * 0.5f;
            float secondSlash = Mathf.Max(0f, windup - firstSlash);

            DragonCombat.PlaySkillPose(player, "Crescent", firstSlash + 0.08f);
            if (firstSlash > 0f)
                yield return new WaitForSeconds(firstSlash);

            if (player == null || player.IsDead())
                yield break;

            DragonCombat.PlaySkillPose(player, "Crescent", secondSlash + 0.08f);
            if (secondSlash > 0f)
                yield return new WaitForSeconds(secondSlash);

            if (player == null || player.IsDead())
                yield break;

            bool ascended = IsAscendedSkill("grand_cross");
            float width = Mathf.Max(1f, ascended ? _crossAscWidth.Value : _grandCrossWidth.Value);
            float height = Mathf.Max(2f, width * 0.60f);
            float range = Mathf.Max(1f, ascended ? _crossAscRange.Value : _grandCrossRange.Value);
            float travelTime = Mathf.Max(0.1f, _grandCrossTravelTime.Value);
            float tickInterval = Mathf.Max(0.10f, _grandCrossTickInterval.Value);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < 0.01f)
                right = Vector3.right;

            GameObject visualRoot = null;
            LineRenderer slashA = null;
            LineRenderer slashB = null;
            if (_enableVfx.Value)
            {
                visualRoot = new GameObject("DragonsAltarGrandCross");
                slashA = CreatePriestPersistentLine(visualRoot.transform, "GrandCrossSlashA", new Color(0.36f, 0.82f, 1f, 0.98f), 0.42f);
                slashB = CreatePriestPersistentLine(visualRoot.transform, "GrandCrossSlashB", new Color(0.72f, 0.94f, 1f, 0.98f), 0.42f);
            }

            Dictionary<int, float> nextHitAt = new Dictionary<int, float>();
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            float elapsed = 0f;

            while (elapsed <= travelTime)
            {
                if (player == null || player.IsDead())
                    break;

                float t = Mathf.Clamp01(elapsed / travelTime);
                Vector3 center = origin + forward * (range * t);
                float halfWidth = width * 0.5f;
                float halfHeight = height * 0.5f;

                if (slashA != null)
                {
                    slashA.SetPosition(0, center - right * halfWidth - Vector3.up * halfHeight);
                    slashA.SetPosition(1, center + right * halfWidth + Vector3.up * halfHeight);
                }
                if (slashB != null)
                {
                    slashB.SetPosition(0, center - right * halfWidth + Vector3.up * halfHeight);
                    slashB.SetPosition(1, center + right * halfWidth - Vector3.up * halfHeight);
                }

                Collider[] hits = Physics.OverlapBox(
                    center,
                    new Vector3(halfWidth, halfHeight, 0.8f),
                    rotation,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

                HashSet<int> frameTargets = new HashSet<int>();
                for (int i = 0; i < hits.Length; i++)
                {
                    Character enemy = hits[i].GetComponentInParent<Character>();
                    if (enemy == null || !IsEnemy(player, enemy))
                        continue;

                    int enemyId = enemy.GetInstanceID();
                    if (!frameTargets.Add(enemyId))
                        continue;

                    float nextAllowed;
                    if (nextHitAt.TryGetValue(enemyId, out nextAllowed) && Time.time < nextAllowed)
                        continue;

                    nextHitAt[enemyId] = Time.time + tickInterval;
                    DealDamage(player, enemy, _grandCrossDamage, 5f, false);
                    RefreshSpiritBurn(player, enemy, _grandCrossSpiritDot.Value, Mathf.Max(0.1f, _grandCrossSpiritDuration.Value));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (visualRoot != null)
                Destroy(visualRoot);

            if (ascended && player != null && !player.IsDead())
            {
                // Holy cross burst where the X stops: Stuns Small and Big.
                Vector3 end = origin + forward * range;
                float burst = Mathf.Max(1f, _crossAscBurstRadius.Value);
                if (_enableVfx.Value)
                    StartCoroutine(AnimateRing(end, 0.5f, burst, 0.45f, new Color(0.72f, 0.94f, 1f, 0.95f), 0.12f));
                List<Character> hit = GetSphereTargets(player, end, burst);
                for (int i = 0; i < hit.Count; i++)
                {
                    DealDamageScaled(player, hit[i], _crossAscBurstDamage, 1f, 10f, false);
                    if (!hit[i].IsBoss())
                        DragonCombat.Stun(hit[i], end);
                }
            }
        }

        private void CastHeavensJudgement(Player player)
        {
            const string id = "Priest.HeavensJudgement";
            if (!BeginCast(player, id, _heavensCooldown.Value, _heavensStamina.Value))
                return;

            bool crossCast;
            Vector3 center = GetPriestSelfOrCrossCastCenter(player, Mathf.Max(1f, _sharedCrossCastRange.Value), out crossCast);
            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _heavensWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Sigil", windup + 0.10f);
            StartCoroutine(HeavensJudgementRoutine(player, center, windup, crossCast));
        }

        private IEnumerator HeavensJudgementRoutine(Player player, Vector3 center, float windup, bool crossCast)
        {
            ShowMessage(crossCast ? "Heaven's Judgement - Cross Cast" : "Heaven's Judgement");

            bool ascended = IsAscendedSkill("heavens_judgement");
            float radius = Mathf.Max(1f, ascended ? _hjAscRadius.Value : _heavensRadius.Value);
            float duration = Mathf.Max(0.1f, ascended ? _hjAscDuration.Value : _heavensDuration.Value);
            float interval = Mathf.Max(0.1f, _heavensStrikeInterval.Value);
            int beamsPerWave = Mathf.Clamp(_heavensStrikesPerWave.Value, 1, 12);
            float impactRadius = Mathf.Max(0.5f, _heavensStrikeRadius.Value);

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, radius, radius, windup + duration + 0.10f, new Color(1f, 0.92f, 0.52f, 0.82f), 0.14f));

            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            if (player == null || player.IsDead())
                yield break;

            int groundMask = LayerMask.GetMask(
                "Default",
                "static_solid",
                "Default_small",
                "piece_nonsolid",
                "terrain",
                "vehicle",
                "piece",
                "viewblock"
            );

            float elapsed = 0f;
            while (elapsed < duration)
            {
                HashSet<int> hitThisWave = new HashSet<int>();

                for (int beam = 0; beam < beamsPerWave; beam++)
                {
                    Vector2 random = UnityEngine.Random.insideUnitCircle * radius;
                    Vector3 strike = center + new Vector3(random.x, 0f, random.y);
                    RaycastHit ground;
                    if (Physics.Raycast(strike + Vector3.up * 10f, Vector3.down, out ground, 24f, groundMask))
                        strike = ground.point;

                    if (_enableVfx.Value)
                    {
                        Vector3 sky = DragonCombat.GetIndoorSafeSkyPoint(strike + Vector3.up * 0.1f, 7f);
                        CreateTemporaryBeam(sky, strike + Vector3.up * 0.08f, new Color(1f, 0.94f, 0.62f, 0.96f), 0.24f, Mathf.Min(0.20f, interval * 0.8f));
                        StartCoroutine(AnimateRing(strike + Vector3.up * 0.06f, 0.25f, impactRadius, Mathf.Min(0.28f, interval), new Color(1f, 0.90f, 0.46f, 0.70f), 0.07f));
                    }

                    List<Character> targets = GetSphereTargets(player, strike, impactRadius);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        Character enemy = targets[i];
                        int enemyId = enemy.GetInstanceID();
                        if (!hitThisWave.Add(enemyId))
                            continue;

                        DealDamage(player, enemy, _heavensDamage, 7f, false);
                        DragonCombat.ApplyFrost(enemy, Mathf.Max(0.1f, _heavensFrostDuration.Value));
                    }

                    if (ascended)
                    {
                        List<Player> allies = IhAlliesInRadius(player, center, radius);
                        for (int i = 0; i < allies.Count; i++)
                            Heal(allies[i], allies[i].GetMaxHealth() * Mathf.Max(0f, _hjAscBeamHeal.Value) / 100f);
                    }
                }

                elapsed += interval;
                yield return new WaitForSeconds(interval);
            }

            if (ascended && player != null && !player.IsDead())
            {
                // Pillar of Heaven at the centre.
                float pillar = Mathf.Max(0.5f, _hjAscPillarRadius.Value);
                if (_enableVfx.Value)
                {
                    CreateTemporaryBeam(DragonCombat.GetIndoorSafeSkyPoint(center, 9f), center, new Color(1f, 0.96f, 0.70f, 1f), 1.2f, 0.5f);
                    StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, 0.5f, pillar, 0.4f, new Color(1f, 0.94f, 0.62f, 0.95f), 0.14f));
                }
                List<Character> hit = GetSphereTargets(player, center, pillar);
                for (int i = 0; i < hit.Count; i++)
                    DealDamageScaled(player, hit[i], _pillarDamage, 1f, 10f, false);
            }
        }

        private LineRenderer CreatePriestPersistentLine(Transform parent, string name, Color color, float width)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = Mathf.Max(0.02f, width);
            line.endWidth = Mathf.Max(0.02f, width);
            line.startColor = color;
            line.endColor = color;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);
            return line;
        }

        private void CastLightningTempest(Player player)
        {
            const string id = "Priest.LightningTempest";
            Vector3 target;
            if (!TryGetPhysicalAimPoint(player, Mathf.Max(1f, _tempestRange.Value), out target))
            {
                ShowMessage("Aim at a physical target");
                return;
            }
            if (!BeginCast(player, id, _tempestCooldown.Value, _tempestStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, 1f);
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Tempest", windup + 0.10f);
            StartCoroutine(LightningTempestRoutine(player, target, windup));
        }

        private IEnumerator LightningTempestRoutine(Player player, Vector3 center, float windup)
        {
            ShowMessage("Lightning Tempest");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            float duration = Mathf.Max(1f, _tempestDuration.Value);
            float interval = Mathf.Max(0.1f, _tempestStrikeInterval.Value);
            bool ascended = IsAscendedSkill("lightning_tempest");
            float radius = Mathf.Max(1f, ascended ? _tempestAscRadius.Value : _tempestRadius.Value);
            int maxStrikes = Mathf.Clamp(_tempestMaxStrikes.Value, 1, 7);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ascended && player != null && !player.IsDead())
                {
                    // Eye of the Storm: follows the Priest; allies inside get Defense + Hyper Armor.
                    center = player.transform.position;
                    List<Player> allies = IhAlliesInRadius(player, center, radius);
                    for (int i = 0; i < allies.Count; i++)
                        DragonCombat.ApplyTimedBuff(allies[i], "Priest.EyeOfTheStorm", interval + 0.6f, 0f, 0f, 0f, Mathf.Clamp(_tempestAscDefense.Value, 0f, 95f) / 100f, 0f, 0f, true);
                }
                int strikes = UnityEngine.Random.Range(1, maxStrikes + 1);
                for (int sIndex = 0; sIndex < strikes; sIndex++)
                {
                    Vector2 circle = UnityEngine.Random.insideUnitCircle * radius;
                    Vector3 strike = center + new Vector3(circle.x, 0f, circle.y);
                    RaycastHit ground;
                    int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");
                    if (Physics.Raycast(strike + Vector3.up * 6f, Vector3.down, out ground, 12f, mask))
                        strike = ground.point;

                    if (_enableVfx.Value)
                        CreateLightning(strike, new Color(0.55f, 0.86f, 1f, 1f), 0.28f);

                    List<Character> targets = GetSphereTargets(player, strike, 1.6f);
                    for (int i = 0; i < targets.Count; i++)
                    {
                        Character enemy = targets[i];
                        DealDamage(player, enemy, _tempestDamage, 8f, false);
                        DragonCombat.ApplyFrost(enemy, Mathf.Max(0.1f, _tempestFrostDuration.Value));
                        DragonCombat.ApplyExpose(enemy, Mathf.Max(0.1f, _tempestExposeDuration.Value));
                        DragonCombat.ApplyZap(player, enemy, _tempestZapDamage.Value, 0f, 0f);
                        RefreshFireBurn(player, enemy, _tempestFireDot.Value, Mathf.Max(0.1f, _tempestFireDuration.Value));
                        RefreshSpiritBurn(player, enemy, _tempestSpiritDot.Value, Mathf.Max(0.1f, _tempestSpiritDuration.Value));
                    }
                }
                elapsed += interval;
                yield return new WaitForSeconds(interval);
            }

            if (ascended && player != null && !player.IsDead())
            {
                // Heaven's Wrath: every enemy inside takes an instant Zap detonation.
                center = player.transform.position;
                if (_enableVfx.Value)
                    StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, 0.5f, radius, 0.45f, new Color(1f, 0.40f, 0.36f, 0.95f), 0.14f));
                List<Character> hit = GetSphereTargets(player, center, radius);
                for (int i = 0; i < hit.Count; i++)
                    DragonCombat.ApplyZap(player, hit[i], _tempestZapDamage.Value, -1f, 0f);
            }
        }

        private void ActivateGrandSigil(Player player)
        {
            const string id = "Priest.GrandSigil";
            if (!BeginCast(player, id, _grandCooldown.Value, _grandStamina.Value))
                return;

            float windup = DragonCombat.ScaleWindup(player, Mathf.Max(0f, _grandWindup.Value));
            DragonCombat.LockSkill(player, windup);
            DragonCombat.PlaySkillPose(player, "Sigil", windup + 0.10f);
            StartCoroutine(GrandSigilBarrierRoutine(player, windup));
        }

        private IEnumerator GrandSigilBarrierRoutine(Player player, float windup)
        {
            ShowMessage("Heaven's Crucible");
            if (windup > 0f)
                yield return new WaitForSeconds(windup);
            if (player == null || player.IsDead())
                yield break;

            float radius = Mathf.Max(1f, _grandRadius.Value);
            // Snapshot: 30% of the Priest's current Armor, not updated if equipment changes later.
            float armor = GetArmor(player) * Mathf.Max(0f, _crucibleArmorPercent.Value) / 100f;
            List<Player> players = GetPlayersInSphere(player.transform.position, radius);

            if (!players.Contains(player))
                players.Add(player);

            for (int i = 0; i < players.Count; i++)
            {
                Player ally = players[i];
                BarrierState state = new BarrierState();
                state.HP = Mathf.Max(1f, _grandBarrierHp.Value);
                state.Armor = Mathf.Max(0f, armor);
                state.EndTime = Time.time + Mathf.Max(1f, _grandBarrierDuration.Value);
                _barriers[ally.GetInstanceID()] = state;

                if (_enableVfx.Value)
                    StartCoroutine(BarrierVisual(ally));
            }

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(player.transform.position + Vector3.up * 0.10f, 0.5f, radius, 0.65f, new Color(0.56f, 0.90f, 1f, 0.92f), 0.10f));
        }
        private bool BeginPriestRelicCast(Player player, string id, float stamina)
        {
            float remaining = GetCooldownRemaining(id);
            if (remaining > 0f)
            {
                ShowMessage("Cooldown: " + remaining.ToString("0.0") + "s");
                return false;
            }

            stamina = Mathf.Max(0f, stamina);
            if (GetStamina(player) < stamina)
            {
                ShowMessage("Not enough stamina");
                return false;
            }

            UseStamina(player, stamina);
            return true;
        }

        private void SetPriestCooldownNow(string id, float cooldown)
        {
            if (string.IsNullOrEmpty(id))
                return;

            if (_testingForceCooldowns.Value)
                cooldown = Mathf.Max(0f, _testingCooldownSeconds.Value);

            _cooldowns[id] = Time.time + Mathf.Max(0f, cooldown);
        }

        private void StartPriestRelicCooldown(PriestRelicState relic)
        {
            if (relic == null || relic.CooldownStarted)
                return;

            relic.CooldownStarted = true;
            SetPriestCooldownNow(relic.CooldownId, relic.CooldownSeconds);
        }

        private PriestRelicState RegisterPriestRelic(
            GameObject cross,
            Vector3 position,
            float radius,
            float duration,
            bool isLightning,
            string cooldownId,
            float cooldownSeconds
        )
        {
            CleanupPriestRelics();

            PriestRelicState existing = FindPriestRelic(isLightning);
            if (existing != null)
                RelinquishPriestRelic(existing);

            PriestRelicState state = new PriestRelicState();
            state.Cross = cross;
            state.Position = position;
            state.Radius = Mathf.Max(0.5f, radius);
            state.EndTime = Time.time + Mathf.Max(0.5f, duration);
            state.IsLightning = isLightning;
            state.CooldownId = cooldownId;
            state.CooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            state.CooldownStarted = false;
            _priestRelics.Add(state);

            if (cross != null)
            {
                PriestRelicMarker marker = cross.GetComponent<PriestRelicMarker>();
                if (marker == null)
                    marker = cross.AddComponent<PriestRelicMarker>();
                marker.State = state;
            }

            return state;
        }

        private PriestRelicState FindPriestRelic(bool isLightning)
        {
            CleanupPriestRelics();
            for (int i = 0; i < _priestRelics.Count; i++)
            {
                PriestRelicState relic = _priestRelics[i];
                if (relic != null && relic.IsLightning == isLightning && relic.Cross != null && Time.time < relic.EndTime)
                    return relic;
            }
            return null;
        }

        private void RelinquishPriestRelic(PriestRelicState relic)
        {
            if (relic == null)
                return;

            relic.EndTime = Time.time;
            StartPriestRelicCooldown(relic);

            if (relic.Cross != null)
                Destroy(relic.Cross);

            _priestRelics.Remove(relic);
        }

        private void FinishPriestRelic(PriestRelicState relic)
        {
            if (relic == null)
                return;

            StartPriestRelicCooldown(relic);

            if (relic.Cross != null)
                Destroy(relic.Cross);

            _priestRelics.Remove(relic);
        }

        private void CleanupPriestRelics()
        {
            for (int i = _priestRelics.Count - 1; i >= 0; i--)
            {
                PriestRelicState relic = _priestRelics[i];
                if (relic == null)
                {
                    _priestRelics.RemoveAt(i);
                    continue;
                }

                if (Time.time >= relic.EndTime || relic.Cross == null)
                {
                    StartPriestRelicCooldown(relic);
                    if (relic.Cross != null)
                        Destroy(relic.Cross);
                    _priestRelics.RemoveAt(i);
                }
            }
        }

        private List<PriestRelicState> GetActivePriestRelics()
        {
            CleanupPriestRelics();
            return new List<PriestRelicState>(_priestRelics);
        }

        private bool TryGetAimedPriestRelic(Player player, float range, out PriestRelicState relic)
        {
            relic = null;
            if (player == null)
                return false;

            CleanupPriestRelics();

            Vector3 origin = player.GetEyePoint();
            Vector3 direction = AlbedoAimUtility.GetProjectileDirection(player, origin);
            if (direction.sqrMagnitude < 0.01f)
                direction = player.transform.forward;
            direction.Normalize();

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, Mathf.Max(1f, range), ~0, QueryTriggerInteraction.Ignore);
            int nearestIndex = -1;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].distance < nearestDistance)
                {
                    nearestDistance = hits[i].distance;
                    nearestIndex = i;
                }
            }

            if (nearestIndex < 0 || hits[nearestIndex].collider == null)
                return false;

            PriestRelicMarker marker = hits[nearestIndex].collider.GetComponentInParent<PriestRelicMarker>();
            if (marker == null || marker.State == null)
                return false;

            PriestRelicState candidate = marker.State;
            if (candidate.Cross == null || Time.time >= candidate.EndTime)
                return false;

            relic = candidate;
            return true;
        }

        private Vector3 GetPriestSelfOrCrossCastCenter(Player player, float range, out bool crossCast)
        {
            PriestRelicState relic;
            if (TryGetAimedPriestRelic(player, range, out relic))
            {
                crossCast = true;
                return relic.Position;
            }

            crossCast = false;
            return player == null ? Vector3.zero : player.transform.position;
        }

        private bool TryGetConsecratedGround(out Vector3 center, out float radius)
        {
            center = Vector3.zero;
            radius = Mathf.Max(1f, _consecratedRadius.Value);

            PriestRelicState lightning = FindPriestRelic(true);
            PriestRelicState holy = FindPriestRelic(false);
            if (lightning == null || holy == null)
                return false;

            Vector3 delta = lightning.Position - holy.Position;
            delta.y = 0f;
            float connectRange = Mathf.Max(0f, _consecratedConnectRange.Value);
            if (delta.sqrMagnitude > connectRange * connectRange)
                return false;

            center = (lightning.Position + holy.Position) * 0.5f;
            return true;
        }

        private bool IsInsideConsecratedGround(Vector3 point)
        {
            Vector3 center;
            float radius;
            if (!TryGetConsecratedGround(out center, out radius))
                return false;

            Vector3 delta = point - center;
            delta.y = 0f;
            return delta.sqrMagnitude <= radius * radius;
        }

        private void PulseConsecratedGroundVfx()
        {
            if (!_enableVfx.Value)
                return;

            Vector3 center;
            float radius;
            if (!TryGetConsecratedGround(out center, out radius))
                return;

            PriestRelicState lightning = FindPriestRelic(true);
            PriestRelicState holy = FindPriestRelic(false);
            if (lightning == null || holy == null)
                return;

            CreateTemporaryBeam(
                lightning.Position + Vector3.up * 1.2f,
                holy.Position + Vector3.up * 1.2f,
                new Color(1f, 0.90f, 0.42f, 0.78f),
                0.10f,
                0.55f
            );
            StartCoroutine(
                AnimateRing(
                    center + Vector3.up * 0.07f,
                    radius,
                    radius,
                    1.05f,
                    new Color(1f, 0.88f, 0.38f, 0.52f),
                    0.08f
                )
            );
        }

        private void DealDamageScaled(Player attacker, Character target, DamageConfig cfg, float multiplier, float push, bool forceStagger)
        {
            if (attacker == null || target == null || cfg == null)
                return;
            multiplier = Mathf.Max(0f, multiplier) * DamagePower(attacker, cfg);
            HitData hit = new HitData();
            hit.m_damage.m_blunt = cfg.Blunt.Value * multiplier;
            hit.m_damage.m_slash = cfg.Slash.Value * multiplier;
            hit.m_damage.m_pierce = cfg.Pierce.Value * multiplier;
            hit.m_damage.m_fire = cfg.Fire.Value * multiplier;
            hit.m_damage.m_frost = cfg.Frost.Value * multiplier;
            hit.m_damage.m_lightning = cfg.Lightning.Value * multiplier;
            hit.m_damage.m_poison = cfg.Poison.Value * multiplier;
            hit.m_damage.m_spirit = cfg.Spirit.Value * multiplier;
            hit.m_point = target.transform.position;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = Mathf.Max(0f, push);
            hit.SetAttacker(attacker);
            if (forceStagger)
                TrySetForceStagger(hit);
            target.Damage(hit);
        }

        private void GrantPriestBarrier(Player ally, float hp, float armor, float duration)
        {
            if (ally == null)
                return;
            int id = ally.GetInstanceID();
            BarrierState state;
            if (!_barriers.TryGetValue(id, out state) || state == null || Time.time >= state.EndTime)
            {
                state = new BarrierState();
                _barriers[id] = state;
            }
            state.HP = Mathf.Max(state.HP, Mathf.Max(1f, hp));
            state.Armor = Mathf.Max(state.Armor, Mathf.Max(0f, armor));
            state.EndTime = Mathf.Max(state.EndTime, Time.time + Mathf.Max(0.5f, duration));
            if (_enableVfx.Value)
                StartCoroutine(BarrierVisual(ally));
        }

        private void CreateTemporaryBeam(Vector3 start, Vector3 end, Color color, float width, float lifetime)
        {
            GameObject obj = new GameObject("DragonsAltarPriestBeam");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = Mathf.Max(0.02f, width);
            line.endWidth = Mathf.Max(0.02f, width * 0.75f);
            line.startColor = color;
            line.endColor = color;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            Destroy(obj, Mathf.Max(0.05f, lifetime));
        }

        private void RefreshSpiritBurn(Player attacker, Character target, float damagePerSecond, float duration)
        {
            RefreshDot(_refreshingSpiritBurns, attacker, target, damagePerSecond, duration);
        }

        private void RefreshFireBurn(Player attacker, Character target, float damagePerSecond, float duration)
        {
            RefreshDot(_refreshingFireBurns, attacker, target, damagePerSecond, duration);
        }

        private void RefreshDot(Dictionary<int, RefreshingDotState> states, Player attacker, Character target, float damagePerSecond, float duration)
        {
            if (states == null || target == null || target.IsDead()) return;
            int id = target.GetInstanceID();
            RefreshingDotState state;
            if (!states.TryGetValue(id, out state) || state == null)
            {
                state = new RefreshingDotState();
                state.NextTick = Time.time + 1f;
                states[id] = state;
            }
            state.Attacker = attacker;
            state.Target = target;
            state.DamagePerSecond = Mathf.Max(0f, damagePerSecond);
            state.EndTime = Time.time + Mathf.Max(0.1f, duration);
        }

        private void UpdateRefreshingDots()
        {
            UpdateRefreshingDotDictionary(_refreshingSpiritBurns, true);
            UpdateRefreshingDotDictionary(_refreshingFireBurns, false);
        }

        private void UpdateRefreshingDotDictionary(Dictionary<int, RefreshingDotState> states, bool spirit)
        {
            if (states == null || states.Count == 0) return;
            float now = Time.time;
            List<int> remove = null;
            foreach (KeyValuePair<int, RefreshingDotState> pair in states)
            {
                RefreshingDotState state = pair.Value;
                if (state == null || state.Target == null || state.Target.IsDead() || now >= state.EndTime)
                {
                    if (remove == null) remove = new List<int>();
                    remove.Add(pair.Key);
                    continue;
                }
                if (now >= state.NextTick)
                {
                    state.NextTick = now + 1f;
                    if (spirit) DragonCombat.ApplySpiritBurnTick(state.Attacker, state.Target, state.DamagePerSecond);
                    else DragonCombat.ApplyFireBurnTick(state.Attacker, state.Target, state.DamagePerSecond);
                }
            }
            if (remove != null) for (int i=0;i<remove.Count;i++) states.Remove(remove[i]);
        }

        private IEnumerator FireDot(Player attacker, Character target, float damagePerSecond, float duration)
        {
            int ticks = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0.1f, duration)));
            float damage = Mathf.Max(0f, damagePerSecond);
            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(1f);
                if (target == null || target.IsDead())
                    yield break;
                DragonCombat.ApplyFireBurnTick(attacker, target, damage);
            }
        }

        private IEnumerator CrescentVerticalSlashWave(
            Player player,
            Vector3 origin,
            Vector3 forward,
            float range,
            float width,
            float height,
            float travelTime
        )
        {
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)
                forward = player.transform.forward;

            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;

            forward.Normalize();

            Dictionary<int, float> nextHitAt = new Dictionary<int, float>();
            float elapsed = 0f;

            int groundMask = LayerMask.GetMask(
                "Default",
                "static_solid",
                "Default_small",
                "piece_nonsolid",
                "terrain",
                "vehicle",
                "piece",
                "viewblock"
            );

            while (elapsed <= travelTime)
            {
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, travelTime));
                Vector3 point = origin + forward * Mathf.Lerp(0.35f, range, t);

                // Keep each slash riding the terrain/slope as it moves outward.
                RaycastHit ground;

                if (Physics.Raycast(point + Vector3.up * 5f, Vector3.down, out ground, 12f, groundMask))
                    point = ground.point;

                float depth = 2.5f;
                Vector3 center = point + Vector3.up * (height * 0.5f);

                // Vertical blade hitbox:
                // X = ~1m width, Y = tall vertical blade, Z = thin travelling depth.
                Collider[] hits = Physics.OverlapBox(
                    center,
                    new Vector3(width * 0.5f, height * 0.5f, depth * 0.5f),
                    Quaternion.LookRotation(forward, Vector3.up)
                );

                for (int i = 0; i < hits.Length; i++)
                {
                    Character target = hits[i].GetComponentInParent<Character>();

                    if (target == null || !IsEnemy(player, target))
                        continue;

                    int targetId = target.GetInstanceID();
                    float nextAllowed;
                    if (nextHitAt.TryGetValue(targetId, out nextAllowed) && Time.time < nextAllowed)
                        continue;

                    nextHitAt[targetId] = Time.time + Mathf.Max(0.10f, _crescentPersistentTick.Value);
                    DealDamage(player, target, _crescentDamage, 8f, false);
                }

                if (_enableVfx.Value)
                {
                    CreateCrescentVerticalSlashVisual(
                        point,
                        forward,
                        width,
                        height,
                        0.10f
                    );
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator SpiritTrail(
            Player player,
            Vector3 origin,
            Vector3 forward,
            float range,
            float travelTime,
            Dictionary<int, float> sharedNextHitAt
        )
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            if (sharedNextHitAt == null)
                sharedNextHitAt = new Dictionary<int, float>();

            int groundMask = LayerMask.GetMask(
                "Default",
                "static_solid",
                "Default_small",
                "piece_nonsolid",
                "terrain",
                "vehicle",
                "piece",
                "viewblock"
            );

            float elapsed = 0f;
            while (elapsed <= travelTime)
            {
                if (player == null || player.IsDead())
                    yield break;

                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, travelTime));
                Vector3 projected = origin + forward * (range * t);
                RaycastHit ground;

                // Ground Projectile nature: each trail rides the physical surface.
                // If there is no surface under this sample, do not create a floating trail segment.
                if (!Physics.Raycast(projected + Vector3.up * 8f, Vector3.down, out ground, 24f, groundMask))
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                    continue;
                }

                Vector3 point = ground.point;
                Collider[] hits = Physics.OverlapSphere(
                    point + Vector3.up * 0.35f,
                    0.9f,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

                for (int i = 0; i < hits.Length; i++)
                {
                    Character target = hits[i].GetComponentInParent<Character>();
                    if (target == null || !IsEnemy(player, target))
                        continue;

                    int targetId = target.GetInstanceID();
                    float nextAllowed;
                    if (sharedNextHitAt.TryGetValue(targetId, out nextAllowed) && Time.time < nextAllowed)
                        continue;

                    sharedNextHitAt[targetId] = Time.time + Mathf.Max(0.10f, _divineTrailPersistentTick.Value);

                    // Persistent direct Lightning Damage + refreshed Spirit DoT.
                    DealDamage(player, target, _divineTrailDamage, 0f, false);
                    RefreshSpiritBurn(player, target, _divineSpiritDot.Value, Mathf.Max(0.1f, _divineSpiritDuration.Value));
                }

                if (_enableVfx.Value)
                {
                    StartCoroutine(AnimateRing(
                        point + Vector3.up * 0.06f,
                        0.12f,
                        0.80f,
                        0.22f,
                        new Color(0.55f, 0.86f, 1f, 0.82f),
                        0.05f
                    ));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private List<Character> GetFrontalTargets(Player player, Vector3 origin, Vector3 forward, float range, float angle)
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

            Collider[] hits = Physics.OverlapSphere(origin, range);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i];
                Character target = collider.GetComponentInParent<Character>();

                if (target == null || seen.Contains(target) || !IsEnemy(player, target))
                    continue;

                Vector3 closest = collider.ClosestPoint(origin);
                Vector3 horizontalClosest = closest - origin;
                horizontalClosest.y = 0f;

                if (horizontalClosest.sqrMagnitude > range * range)
                    continue;

                Vector3 directionPoint = collider.bounds.center - origin;
                directionPoint.y = 0f;

                if (directionPoint.sqrMagnitude < 0.001f)
                    directionPoint = horizontalClosest;

                if (directionPoint.sqrMagnitude > 0.001f &&
                    Vector3.Angle(forward, directionPoint.normalized) > angle * 0.5f)
                    continue;

                seen.Add(target);
                result.Add(target);
            }

            return result;
        }

        private void DealScaledDamage(Player attacker, Character target, DamageConfig cfg, float push, float multiplier)
        {
            HitData hit = new HitData();
            multiplier = Mathf.Max(0f, multiplier);
            hit.m_damage.m_blunt = cfg.Blunt.Value * multiplier;
            hit.m_damage.m_slash = cfg.Slash.Value * multiplier;
            hit.m_damage.m_pierce = cfg.Pierce.Value * multiplier;
            hit.m_damage.m_fire = cfg.Fire.Value * multiplier;
            hit.m_damage.m_frost = cfg.Frost.Value * multiplier;
            hit.m_damage.m_lightning = cfg.Lightning.Value * multiplier;
            hit.m_damage.m_poison = cfg.Poison.Value * multiplier;
            hit.m_damage.m_spirit = cfg.Spirit.Value * multiplier;
            hit.m_point = target.transform.position;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = push;
            hit.SetAttacker(attacker);
            target.Damage(hit);
        }

        private void DealSnapshotDamage(Player attacker, Character target, DamageSnapshot snapshot, float multiplier, float push)
        {
            HitData hit = new HitData();
            multiplier = Mathf.Max(0f, multiplier);
            hit.m_damage.m_blunt = snapshot.Blunt * multiplier;
            hit.m_damage.m_slash = snapshot.Slash * multiplier;
            hit.m_damage.m_pierce = snapshot.Pierce * multiplier;
            hit.m_damage.m_fire = snapshot.Fire * multiplier;
            hit.m_damage.m_frost = snapshot.Frost * multiplier;
            hit.m_damage.m_lightning = snapshot.Lightning * multiplier;
            hit.m_damage.m_poison = snapshot.Poison * multiplier;
            hit.m_damage.m_spirit = snapshot.Spirit * multiplier;
            hit.m_point = target.transform.position;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = push;
            hit.SetAttacker(attacker);
            target.Damage(hit);
        }

        private void DealSnapshotDamage(Player attacker, Character target, DamageSnapshot snapshot, float multiplier, float push, bool forceStagger)
        {
            HitData hit = new HitData();
            multiplier = Mathf.Max(0f, multiplier);
            hit.m_damage.m_blunt = snapshot.Blunt * multiplier;
            hit.m_damage.m_slash = snapshot.Slash * multiplier;
            hit.m_damage.m_pierce = snapshot.Pierce * multiplier;
            hit.m_damage.m_fire = snapshot.Fire * multiplier;
            hit.m_damage.m_frost = snapshot.Frost * multiplier;
            hit.m_damage.m_lightning = snapshot.Lightning * multiplier;
            hit.m_damage.m_poison = snapshot.Poison * multiplier;
            hit.m_damage.m_spirit = snapshot.Spirit * multiplier;
            hit.m_point = target.transform.position;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = push;
            hit.SetAttacker(attacker);
            if (forceStagger)
                TrySetForceStagger(hit);
            target.Damage(hit);
            if (forceStagger)
                ForceStagger(target, attacker);
        }

        private IEnumerator SpiritDot(Player attacker, Character target, float damagePerSecond, float duration)
        {
            int ticks = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0.1f, duration)));
            float damage = Mathf.Max(0f, damagePerSecond);

            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(1f);

                if (target == null || target.IsDead())
                    yield break;

                DragonCombat.ApplySpiritBurnTick(attacker, target, damage);
            }
        }

        private bool BeginCast(Player player, string id, float cooldown, float stamina)
        {
            if (_testingForceCooldowns.Value)
                cooldown = Mathf.Max(0f, _testingCooldownSeconds.Value);

            float remaining = GetCooldownRemaining(id);
            if (remaining > 0f)
            {
                ShowMessage("Cooldown: " + remaining.ToString("0.0") + "s");
                return false;
            }

            stamina = Mathf.Max(0f, stamina);

            if (GetStamina(player) < stamina)
            {
                ShowMessage("Not enough stamina");
                return false;
            }

            UseStamina(player, stamina);
            _cooldowns[id] = Time.time + Mathf.Max(0f, cooldown);
            return true;
        }

        public float GetCooldownRemaining(string id)
        {
            float end;

            if (!_cooldowns.TryGetValue(id, out end))
                return 0f;

            return Mathf.Max(0f, end - Time.time);
        }

        private void DealDamage(Player attacker, Character target, DamageConfig cfg, float push, bool forceStagger)
        {
            float power = DamagePower(attacker, cfg);
            HitData hit = new HitData();
            hit.m_damage.m_blunt = cfg.Blunt.Value * power;
            hit.m_damage.m_slash = cfg.Slash.Value * power;
            hit.m_damage.m_pierce = cfg.Pierce.Value * power;
            hit.m_damage.m_fire = cfg.Fire.Value * power;
            hit.m_damage.m_frost = cfg.Frost.Value * power;
            hit.m_damage.m_lightning = cfg.Lightning.Value * power;
            hit.m_damage.m_poison = cfg.Poison.Value * power;
            hit.m_damage.m_spirit = cfg.Spirit.Value * power;
            hit.m_point = target.transform.position;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = push;
            hit.SetAttacker(attacker);

            if (forceStagger)
                TrySetForceStagger(hit);

            target.Damage(hit);
        }

        private void DealSnapshotDamage(Player attacker, Character target, DamageSnapshot damage, float push)
        {
            HitData hit = new HitData();
            hit.m_damage.m_blunt = damage.Blunt;
            hit.m_damage.m_slash = damage.Slash;
            hit.m_damage.m_pierce = damage.Pierce;
            hit.m_damage.m_fire = damage.Fire;
            hit.m_damage.m_frost = damage.Frost;
            hit.m_damage.m_lightning = damage.Lightning;
            hit.m_damage.m_poison = damage.Poison;
            hit.m_damage.m_spirit = damage.Spirit;
            hit.m_point = target.transform.position;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = push;
            hit.SetAttacker(attacker);
            target.Damage(hit);
        }

        private void TrySetForceStagger(HitData hit)
        {
            try
            {
                FieldInfo field = typeof(HitData).GetField(
                    "m_forceStagger",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (field != null)
                    field.SetValue(hit, true);
            }
            catch
            {
            }
        }

        private void ForceStagger(Character target, Player attacker)
        {
            try
            {
                MethodInfo method = target.GetType().GetMethod(
                    "Stagger",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(Vector3) },
                    null
                );

                if (method != null)
                {
                    Vector3 direction = (target.transform.position - attacker.transform.position).normalized;
                    method.Invoke(target, new object[] { direction });
                }
            }
            catch
            {
            }
        }

        private List<Character> GetSphereTargets(Player attacker, Vector3 center, float radius)
        {
            List<Character> result = new List<Character>();
            Collider[] hits = Physics.OverlapSphere(center, radius);
            HashSet<int> seen = new HashSet<int>();

            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();

                if (target == null || !IsEnemy(attacker, target))
                    continue;

                int id = target.GetInstanceID();
                if (seen.Add(id))
                    result.Add(target);
            }

            return result;
        }

        private List<Character> GetAimedBoxTargets(Player attacker, Vector3 origin, Vector3 forward, float length, float width)
        {
            List<Character> result = new List<Character>();
            length = Mathf.Max(0.5f, length);
            width = Mathf.Max(0.5f, width);

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = attacker.transform.forward;

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;

            forward.Normalize();

            Vector3 center = origin + forward * (length * 0.5f);
            Vector3 half = new Vector3(width * 0.5f, 2.5f, length * 0.5f);
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            Collider[] hits = Physics.OverlapBox(center, half, rotation);
            HashSet<int> seen = new HashSet<int>();

            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();

                if (target == null || !IsEnemy(attacker, target))
                    continue;

                int id = target.GetInstanceID();

                if (seen.Add(id))
                    result.Add(target);
            }

            return result;
        }

        private List<Character> GetForwardBoxTargets(Player attacker, float length, float width)
        {
            List<Character> result = new List<Character>();
            Vector3 forward = FlatForward(attacker);
            Vector3 center = attacker.transform.position + forward * (length * 0.5f) + Vector3.up * 1.0f;
            Vector3 half = new Vector3(width * 0.5f, 2.5f, length * 0.5f);
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            Collider[] hits = Physics.OverlapBox(center, half, rotation);
            HashSet<int> seen = new HashSet<int>();

            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();

                if (target == null || !IsEnemy(attacker, target))
                    continue;

                int id = target.GetInstanceID();
                if (seen.Add(id))
                    result.Add(target);
            }

            return result;
        }

        private List<Character> GetFrontalTargets(Player attacker, Vector3 forward, float radius, float minimumDot)
        {
            List<Character> all = GetSphereTargets(attacker, attacker.transform.position, radius);
            List<Character> result = new List<Character>();

            for (int i = 0; i < all.Count; i++)
            {
                Vector3 toTarget = all[i].transform.position - attacker.transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude < 0.01f)
                {
                    result.Add(all[i]);
                    continue;
                }

                toTarget.Normalize();

                if (Vector3.Dot(forward, toTarget) >= minimumDot)
                    result.Add(all[i]);
            }

            return result;
        }

        private List<Player> GetPlayersInSphere(Vector3 center, float radius)
        {
            List<Player> result = new List<Player>();
            Collider[] hits = Physics.OverlapSphere(center, radius);
            HashSet<int> seen = new HashSet<int>();

            for (int i = 0; i < hits.Length; i++)
            {
                Player player = hits[i].GetComponentInParent<Player>();

                if (player == null)
                    continue;

                int id = player.GetInstanceID();

                if (seen.Add(id))
                    result.Add(player);
            }

            return result;
        }

        public bool IsMoonlightEnemy(Player attacker, Character target)
        {
            return IsEnemy(attacker, target);
        }

        public void ApplyMoonlightSpiritDot(Player attacker, Character target, float spiritPerSecond, float duration)
        {
            StartCoroutine(SpiritDot(attacker, target, spiritPerSecond, duration));
        }

        private bool IsEnemy(Player attacker, Character target)
        {
            if (target == null || target == attacker || target.IsDead())
                return false;

            if (target is Player)
                return false;

            try
            {
                MethodInfo method = target.GetType().GetMethod(
                    "IsTamed",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method != null && Convert.ToBoolean(method.Invoke(target, null)))
                    return false;
            }
            catch
            {
            }

            return true;
        }

        private Vector3 FlatForward(Player player)
        {
            Vector3 forward = player.transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;

            forward.Normalize();
            return forward;
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

        private void Heal(Character target, float amount)
        {
            if (target == null || amount <= 0f)
                return;

            try
            {
                MethodInfo[] methods = target.GetType().GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

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
                MethodInfo method = typeof(Player).GetMethod(
                    "GetStamina",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method != null)
                    return Convert.ToSingle(method.Invoke(player, null));
            }
            catch
            {
            }

            FieldInfo field = typeof(Player).GetField(
                "m_stamina",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field == null)
                return 0f;

            return Convert.ToSingle(field.GetValue(player));
        }

        private void UseStamina(Player player, float amount)
        {
            try
            {
                MethodInfo[] methods = typeof(Player).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

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

            FieldInfo field = typeof(Player).GetField(
                "m_stamina",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field != null)
            {
                field.SetValue(player, Mathf.Max(0f, GetStamina(player) - amount));
                DragonCombat.BlockStaminaRegen(player, 0.20f);
            }
        }

        private float GetArmor(Player player)
        {
            if (player == null)
                return 0f;

            string[] methodNames = new string[] { "GetBodyArmor", "GetArmor" };
            for (int i = 0; i < methodNames.Length; i++)
            {
                try
                {
                    MethodInfo method = player.GetType().GetMethod(
                        methodNames[i],
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        Type.EmptyTypes,
                        null
                    );

                    if (method != null && method.ReturnType == typeof(float))
                        return Convert.ToSingle(method.Invoke(player, null));
                }
                catch
                {
                }
            }

            return 0f;
        }

        private DamageSnapshot GetWeaponDamage(Player player)
        {
            DamageSnapshot snapshot = new DamageSnapshot();

            try
            {
                MethodInfo currentWeapon = player.GetType().GetMethod(
                    "GetCurrentWeapon",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (currentWeapon == null)
                {
                    snapshot.Blunt = 15f;
                    return snapshot;
                }

                object item = currentWeapon.Invoke(player, null);

                if (item == null)
                {
                    snapshot.Blunt = 15f;
                    return snapshot;
                }

                MethodInfo getDamage = item.GetType().GetMethod(
                    "GetDamage",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null
                );

                if (getDamage == null)
                {
                    snapshot.Blunt = 15f;
                    return snapshot;
                }

                object damage = getDamage.Invoke(item, null);

                if (damage == null)
                {
                    snapshot.Blunt = 15f;
                    return snapshot;
                }

                snapshot.Blunt = ReadFloatField(damage, "m_blunt");
                snapshot.Slash = ReadFloatField(damage, "m_slash");
                snapshot.Pierce = ReadFloatField(damage, "m_pierce");
                snapshot.Fire = ReadFloatField(damage, "m_fire");
                snapshot.Frost = ReadFloatField(damage, "m_frost");
                snapshot.Lightning = ReadFloatField(damage, "m_lightning");
                snapshot.Poison = ReadFloatField(damage, "m_poison");
                snapshot.Spirit = ReadFloatField(damage, "m_spirit");

                if (snapshot.Total() <= 0f)
                    snapshot.Blunt = 15f;
            }
            catch
            {
                snapshot.Blunt = 15f;
            }

            return snapshot;
        }

        private float ReadFloatField(object obj, string name)
        {
            try
            {
                FieldInfo field = obj.GetType().GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (field != null)
                    return Convert.ToSingle(field.GetValue(obj));
            }
            catch
            {
            }

            return 0f;
        }

        private void EncourageAggro(Player player, float radius)
        {
            Type baseAiType = FindTypeByName("BaseAI");

            if (baseAiType == null)
                return;

            Collider[] hits = Physics.OverlapSphere(player.transform.position, radius);
            HashSet<int> seen = new HashSet<int>();

            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();

                if (target == null || !IsEnemy(player, target))
                    continue;

                Component ai = target.GetComponent(baseAiType);

                if (ai == null)
                    continue;

                int id = ai.GetInstanceID();

                if (!seen.Add(id))
                    continue;

                TryInvoke(ai, "SetAlerted", new object[] { true });
                TryInvoke(ai, "SetTarget", new object[] { player });
            }
        }

        private void ActivateBarbaric(Player player)
        {
            if (IsUnchainedFuryActive())
            {
                ShowMessage("Unchained Fury active");
                return;
            }

            if (_mercFury >= 100f && Time.time >= _mercFuryCooldownUntil)
            {
                _mercFury = 0f;
                _mercFuryUntil = Time.time + Mathf.Max(0.5f, _mercFuryDuration.Value);
                _mercFuryCooldownUntil = _mercFuryUntil + Mathf.Max(0f, _mercFuryCooldown.Value);
                _mercFuryEndAnnounced = false;
                DragonCombat.PlaySkillPose(player, "Shout", 0.75f);
                ShowMessage("UNCHAINED FURY");
                if (_enableVfx.Value)
                    StartCoroutine(AnimateAura(player, new Color(1f, 0.22f, 0.08f, 0.92f), Mathf.Max(0.5f, _mercFuryDuration.Value)));
                return;
            }

            if (!BeginCast(player, "Mercenary.Barbaric", _mercTauntCooldown.Value, 0f))
                return;

            DragonCombat.LockSkill(player, 1f);
            DragonCombat.PlaySkillPose(player, "Shout", 1f);
            ShowMessage("Barbaric: Taunt");
            StartCoroutine(BarbaricTauntRoutine(player));
        }

        private bool IsUnchainedFuryActive()
        {
            return Time.time < _mercFuryUntil;
        }

        private void UpdateMercenaryFuryState(Player player, string advancement)
        {
            if (advancement != "Mercenary")
            {
                _mercFury = 0f;
                _mercFuryUntil = 0f;
                _mercFuryCooldownUntil = 0f;
                _mercFuryEndAnnounced = false;
                return;
            }

            if (_mercFuryUntil > 0f && Time.time >= _mercFuryUntil && !_mercFuryEndAnnounced)
            {
                _mercFuryEndAnnounced = true;
                ShowMessage("Unchained Fury ended");
            }
        }

        private void TryBuildMercenaryFury(Player attacker, Character target, HitData hit)
        {
            if (attacker == null || target == null || hit == null || GetAdvancement(attacker) != "Mercenary") return;
            if (!IsEnemy(attacker, target)) return;
            if (!IsNormalMeleeHit(attacker, hit) || TotalDamage(hit) <= 0f) return;
            GainMercenaryFury(Mathf.Max(0f, _mercFuryGainPerWeaponHit.Value));
        }

        private void GainMercenaryFuryFromSkillHit(Player attacker)
        {
            if (attacker == null || GetAdvancement(attacker) != "Mercenary") return;
            GainMercenaryFury(Mathf.Max(0f, _mercFuryGainPerSkillTarget.Value));
        }

        private void GainMercenaryFury(float amount)
        {
            if (amount <= 0f || IsUnchainedFuryActive() || Time.time < _mercFuryCooldownUntil || _mercFury >= 100f)
                return;

            float before = _mercFury;
            _mercFury = Mathf.Clamp(_mercFury + amount, 0f, 100f);
            if (before < 100f && _mercFury >= 100f)
                ShowMessage("FURY 100 - M4+R");
        }

        private bool IsNormalMeleeHit(Player attacker, HitData hit)
        {
            if (attacker == null || hit == null || !IsWeaponHit(hit))
                return false;

            try
            {
                MethodInfo method = attacker.GetType().GetMethod("GetCurrentWeapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null) return true;
                object item = method.Invoke(attacker, null);
                if (item == null) return true;
                FieldInfo sharedField = item.GetType().GetField("m_shared", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sharedField == null) return true;
                object shared = sharedField.GetValue(item);
                if (shared == null) return true;
                FieldInfo skillField = shared.GetType().GetField("m_skillType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (skillField == null) return true;
                object skillValue = skillField.GetValue(shared);
                string skill = skillValue == null ? "" : skillValue.ToString();

                return skill == "Axes" || skill == "Swords" || skill == "Knives" || skill == "Clubs" ||
                       skill == "Polearms" || skill == "Spears" || skill == "Unarmed";
            }
            catch
            {
                return true;
            }
        }

        private IEnumerator BarbaricTauntRoutine(Player player)
        {
            yield return new WaitForSeconds(1f);
            if (player == null || player.IsDead() || GetAdvancement(player) != "Mercenary")
                yield break;

            float radius = Mathf.Max(0f, _mercTauntRadius.Value);
            Collider[] hits = Physics.OverlapSphere(player.transform.position, radius);
            HashSet<Character> targets = new HashSet<Character>();
            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();
                if (target != null && IsEnemy(player, target) && targets.Add(target))
                    DragonCombat.ApplyExpose(target, Mathf.Max(0.1f, _mercExposeDuration.Value));
            }

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(player.transform.position + Vector3.up * 0.1f, 0.5f, radius, 0.6f, new Color(0.95f, 0.35f, 0.18f, 0.9f), 0.15f));

            Type aiType = FindTypeByName("BaseAI");
            float end = Time.time + Mathf.Max(0.1f, _mercTauntDuration.Value);
            while (Time.time < end && player != null && !player.IsDead() && GetAdvancement(player) == "Mercenary")
            {
                if (aiType != null)
                    foreach (Character target in targets)
                    {
                        if (target == null || target.IsDead()) continue;
                        Component ai = target.GetComponent(aiType);
                        if (ai == null) continue;
                        TryInvoke(ai, "SetAlerted", new object[] { true });
                        TryInvoke(ai, "SetTarget", new object[] { player });
                    }
                yield return new WaitForSeconds(0.25f);
            }
        }

        private Type FindTypeByName(string name)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    Type[] types = assemblies[i].GetTypes();

                    for (int j = 0; j < types.Length; j++)
                    {
                        if (types[j] != null && types[j].Name == name)
                            return types[j];
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private void TryInvoke(object target, string methodName, object[] args)
        {
            try
            {
                MethodInfo[] methods = target.GetType().GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Name != methodName)
                        continue;

                    ParameterInfo[] parameters = methods[i].GetParameters();

                    if (parameters.Length != args.Length)
                        continue;

                    methods[i].Invoke(target, args);
                    return;
                }
            }
            catch
            {
            }
        }

        private void UpdateCombatRuntimeState(Player player, string advancement)
        {
            if (player == null)
                return;

            if (advancement == "Sword Master" && HasSingleSwordWithEmptyOffhand(player))
            {
                // The Way of the Sword: one Sword, empty off-hand, no shield.
                float factor = 1f + Mathf.Max(0f, _swordAttackSpeedPassive.Value) / 100f;
                DragonCombat.SetAttackSpeedSource(player, factor, 0.30f);
                return;
            }

            if (advancement == "Mercenary")
            {
                ItemDrop.ItemData weapon = player.GetCurrentWeapon();
                if (DragonCombat.IsTwoHandedWeapon(weapon))
                {
                    float factor = 1f + Mathf.Max(0f, _mercTwoHandedAttackSpeed.Value) / 100f;
                    DragonCombat.SetAttackSpeedSource(player, factor, 0.30f);
                }
                return;
            }

            if (advancement == "Paladin" && IsPaladinPassive(player, "HolyKnight") && HasWeaponAndShield(player))
            {
                float factor = 1f + Mathf.Max(0f, _holyKnightAttackSpeed.Value) / 100f;
                DragonCombat.SetAttackSpeedSource(player, factor, 0.30f);
            }
        }

        private bool HasSingleSwordWithEmptyOffhand(Player player)
        {
            ItemDrop.ItemData right = DragonCombat.GetHandItem(player, "m_rightItem");
            ItemDrop.ItemData left = DragonCombat.GetHandItem(player, "m_leftItem");

            bool rightSword = right != null && right.m_shared != null && right.m_shared.m_skillType == Skills.SkillType.Swords;
            bool leftSword = left != null && left.m_shared != null && left.m_shared.m_skillType == Skills.SkillType.Swords;

            return (rightSword && left == null) || (leftSword && right == null);
        }

        private bool HasWeaponAndShield(Player player)
        {
            ItemDrop.ItemData right = DragonCombat.GetHandItem(player, "m_rightItem");
            ItemDrop.ItemData left = DragonCombat.GetHandItem(player, "m_leftItem");

            return (DragonCombat.IsShield(left) && IsCombatWeapon(right)) ||
                   (DragonCombat.IsShield(right) && IsCombatWeapon(left));
        }

        private bool IsCombatWeapon(ItemDrop.ItemData item)
        {
            return item != null &&
                   !DragonCombat.IsShield(item) &&
                   (DragonCombat.IsOneHandedWeapon(item) ||
                    DragonCombat.IsTwoHandedWeapon(item) ||
                    DragonCombat.IsMagicWeapon(item));
        }

        private bool IsHoldingSkillType(Player player, Skills.SkillType skillType)
        {
            try
            {
                MethodInfo method = player.GetType().GetMethod("GetCurrentWeapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                    return false;
                object item = method.Invoke(player, null);
                if (item == null)
                    return false;
                FieldInfo sharedField = item.GetType().GetField("m_shared", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sharedField == null)
                    return false;
                object shared = sharedField.GetValue(item);
                if (shared == null)
                    return false;
                FieldInfo skillField = shared.GetType().GetField("m_skillType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (skillField == null)
                    return false;
                object value = skillField.GetValue(shared);
                return value is Skills.SkillType && (Skills.SkillType)value == skillType;
            }
            catch
            {
                return false;
            }
        }

        private bool HasCrossBuff(Player player)
        {
            if (player == null)
                return false;

            float end;

            if (!_crossBuffUntil.TryGetValue(player.GetInstanceID(), out end))
                return false;

            return Time.time < end;
        }

        private void CleanupTimedStates()
        {
            List<int> removeBuffs = new List<int>();

            foreach (KeyValuePair<int, float> pair in _crossBuffUntil)
            {
                if (Time.time >= pair.Value)
                    removeBuffs.Add(pair.Key);
            }

            for (int i = 0; i < removeBuffs.Count; i++)
                _crossBuffUntil.Remove(removeBuffs[i]);

            List<int> removeBarriers = new List<int>();

            foreach (KeyValuePair<int, BarrierState> pair in _barriers)
            {
                if (Time.time >= pair.Value.EndTime || pair.Value.HP <= 0f)
                    removeBarriers.Add(pair.Key);
            }

            for (int i = 0; i < removeBarriers.Count; i++)
                _barriers.Remove(removeBarriers[i]);
        }

        private string GetClass(Player player)
        {
            return ReadPlayerData(player, ClassDataKey);
        }

        private string GetAdvancement(Player player)
        {
            return ReadPlayerData(player, AdvancementDataKey);
        }

        private string ReadPlayerData(Player player, string key)
        {
            IDictionary data = GetCustomData(player);

            if (data == null || !data.Contains(key))
                return "";

            object value = data[key];
            return value == null ? "" : value.ToString();
        }

        private string GetChoice(Player player, string key, string fallback)
        {
            string value = ReadPlayerData(player, key);

            if (string.IsNullOrEmpty(value))
                return fallback;

            return value;
        }

        private void SetChoice(Player player, string key, string value)
        {
            IDictionary data = GetCustomData(player);

            if (data == null)
                return;

            if (data.Contains(key))
            {
                object existing = data[key];

                if (existing != null && !string.IsNullOrEmpty(existing.ToString()))
                {
                    ShowMessage("Heart of Glory choice is locked until Class Reset");
                    return;
                }
            }

            data[key] = value;
            ShowMessage("Heart of Glory locked: " + value);
        }

        private IDictionary GetCustomData(Player player)
        {
            if (player == null)
                return null;

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

        // v0.20.8: the Elemental Savant / Holy Knight choice is retired; Holy Trinity is the Paladin Mastery.
        private bool IsPaladinPassive(Player player, string passive)
        {
            return false;
        }

        private void SetPaladinPassiveChoice(Player player, string passive)
        {
            IDictionary data = GetCustomData(player);
            if (data == null)
                return;

            string existing = ReadPlayerData(player, PaladinPassiveKey);
            if (!string.IsNullOrEmpty(existing))
            {
                ShowMessage("Paladin passive is locked until Class Reset");
                return;
            }

            data[PaladinPassiveKey] = passive;
            ShowMessage(passive == "ElementalSavant" ? "Elemental Savant locked" : "Holy Knight locked");
            RefreshFoodStats(player);
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

        private float GetSkillBonus(Player player, Skills.SkillType skillType)
        {
            string advancement = GetAdvancement(player);

            if (advancement == "Mercenary" && skillType == Skills.SkillType.Axes)
                return Mathf.Max(0f, _mercAxesBonus.Value);

            // v0.20.8 Holy Trinity: +15 Clubs (the skill hooks cap the effective level at 100).
            if (skillType == Skills.SkillType.Clubs && DragonCombat.IsHolyTrinityActive(player))
                return Mathf.Max(0f, _holyTrinityClubs.Value);

            return 0f;
        }

        private void PatchWithHarmony(MethodBase original, HarmonyMethod prefix, HarmonyMethod postfix)
        {
            if (_harmony == null || original == null)
                return;

            MethodInfo[] methods = typeof(Harmony).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];

                if (method.Name != "Patch")
                    continue;

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length < 1)
                    continue;

                if (!typeof(MethodBase).IsAssignableFrom(parameters[0].ParameterType))
                    continue;

                bool compatible = true;

                for (int p = 1; p < parameters.Length; p++)
                {
                    if (parameters[p].ParameterType != typeof(HarmonyMethod))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (!compatible)
                    continue;

                object[] args = new object[parameters.Length];
                args[0] = original;

                for (int p = 1; p < parameters.Length; p++)
                    args[p] = null;

                if (parameters.Length > 1)
                    args[1] = prefix;

                if (parameters.Length > 2)
                    args[2] = postfix;

                try
                {
                    method.Invoke(_harmony, args);
                    return;
                }
                catch
                {
                }
            }

            throw new MissingMethodException("Could not find a compatible Harmony Patch overload.");
        }

        private void TryInstallPatches()
        {
            _harmony = new Harmony(ModGuid);

            int count = 0;
            count += PatchSkillQueries("GetSkillLevel", "SkillLevelPostfix");
            count += PatchSkillQueries("GetSkillFactor", "SkillFactorPostfix");
            count += PatchFloatRefSEMan("ModifyHealthRegen", "HealthRegenPrefix");
            count += PatchFloatRefSEMan("ModifyStaminaRegen", "StaminaRegenPrefix");
            count += PatchFloatRefSEMan("ModifyEitrRegen", "EitrRegenPrefix");
            count += PatchFloatRefSEMan("ModifySpeed", "MoveSpeedPrefix");
            count += PatchGetMaxHealth();
            count += PatchPlayerFloatGetter("GetMaxStamina", "MaxStaminaPostfix");
            int armorPatches = PatchPlayerFloatGetter("GetBodyArmor", "ArmorPostfix");
            if (armorPatches == 0)
                armorPatches = PatchPlayerFloatGetter("GetArmor", "ArmorPostfix");
            count += armorPatches;
            count += PatchPlayerFloatSetter("SetMaxEitr", "PaladinSetMaxEitrPrefix");
            count += PatchDamageMethods();
            count += PatchLightningZapContext();
            count += PatchRighteousStrikeAscended();
            count += PatchAltarAdvancement();

            Logger.LogInfo("Advanced passive hooks installed: " + count);
        }

        private int PatchSkillQueries(string methodName, string patchName)
        {
            int count = 0;

            try
            {
                MethodInfo patchMethod = typeof(AdvancedPlugin).GetMethod(
                    patchName,
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                if (patchMethod == null)
                    return 0;

                HarmonyMethod postfix = new HarmonyMethod(patchMethod);
                MethodInfo[] methods = typeof(Skills).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != methodName || method.ReturnType != typeof(float))
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();

                    if (parameters.Length < 1 || parameters[0].ParameterType != typeof(Skills.SkillType))
                        continue;

                    try
                    {
                        PatchWithHarmony(method, null, postfix);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Could not patch " + methodName + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not scan " + methodName + ": " + ex.Message);
            }

            return count;
        }

        private int PatchFloatRefSEMan(string methodName, string patchName)
        {
            int count = 0;

            try
            {
                Type seman = AccessTools.TypeByName("SEMan");

                if (seman == null)
                    return 0;

                MethodInfo patchMethod = typeof(AdvancedPlugin).GetMethod(
                    patchName,
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                if (patchMethod == null)
                    return 0;

                HarmonyMethod prefix = new HarmonyMethod(patchMethod);
                Type floatRef = typeof(float).MakeByRefType();

                MethodInfo[] methods = seman.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != methodName)
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();

                    if (parameters.Length < 1 || parameters[0].ParameterType != floatRef)
                        continue;

                    try
                    {
                        PatchWithHarmony(method, prefix, null);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Could not patch " + methodName + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not scan " + methodName + ": " + ex.Message);
            }

            return count;
        }

        private int PatchGetMaxHealth()
        {
            int count = 0;

            try
            {
                MethodInfo patchMethod = typeof(AdvancedPlugin).GetMethod(
                    "MaxHealthPostfix",
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                if (patchMethod == null)
                    return 0;

                HarmonyMethod postfix = new HarmonyMethod(patchMethod);
                MethodInfo[] methods = typeof(Player).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != "GetMaxHealth" || method.ReturnType != typeof(float))
                        continue;

                    if (method.GetParameters().Length != 0)
                        continue;

                    try
                    {
                        PatchWithHarmony(method, null, postfix);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Could not patch GetMaxHealth: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not scan GetMaxHealth: " + ex.Message);
            }

            return count;
        }

        private int PatchPlayerFloatGetter(string methodName, string patchName)
        {
            int count = 0;
            try
            {
                MethodInfo patchMethod = typeof(AdvancedPlugin).GetMethod(
                    patchName,
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                if (patchMethod == null)
                    return 0;

                HarmonyMethod postfix = new HarmonyMethod(patchMethod);
                MethodInfo[] methods = typeof(Player).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != methodName || method.ReturnType != typeof(float) || method.GetParameters().Length != 0)
                        continue;

                    try
                    {
                        PatchWithHarmony(method, null, postfix);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Could not patch " + methodName + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not scan " + methodName + ": " + ex.Message);
            }
            return count;
        }

        private int PatchPlayerFloatSetter(string methodName, string patchName)
        {
            int count = 0;
            try
            {
                MethodInfo patchMethod = typeof(AdvancedPlugin).GetMethod(
                    patchName,
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                if (patchMethod == null)
                    return 0;

                HarmonyMethod prefix = new HarmonyMethod(patchMethod);
                MethodInfo[] methods = typeof(Player).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != methodName)
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length < 1 || parameters[0].ParameterType != typeof(float))
                        continue;

                    try
                    {
                        PatchWithHarmony(method, prefix, null);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Could not patch " + methodName + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not scan " + methodName + ": " + ex.Message);
            }
            return count;
        }

        private int PatchDamageMethods()
        {
            int count = 0;

            try
            {
                MethodInfo patchMethod = typeof(AdvancedPlugin).GetMethod(
                    "DamagePrefix",
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                if (patchMethod == null)
                    return 0;

                HarmonyMethod prefix = new HarmonyMethod(patchMethod);
                HashSet<MethodBase> seen = new HashSet<MethodBase>();
                Type[] types = new Type[] { typeof(Character), typeof(Player) };

                for (int t = 0; t < types.Length; t++)
                {
                    MethodInfo[] methods = types[t].GetMethods(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];

                        if (method.Name != "Damage")
                            continue;

                        ParameterInfo[] parameters = method.GetParameters();

                        if (parameters.Length < 1 || parameters[0].ParameterType != typeof(HitData))
                            continue;

                        if (!seen.Add(method))
                            continue;

                        try
                        {
                            PatchWithHarmony(method, prefix, null);
                            count++;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning("Could not patch Damage: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not scan Damage: " + ex.Message);
            }

            return count;
        }

        private int PatchLightningZapContext()
        {
            try
            {
                MethodInfo method = typeof(SkillsPlugin).GetMethod(
                    "CastLightningZap",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                MethodInfo prefixMethod = typeof(AdvancedPlugin).GetMethod(
                    "LightningZapContextPrefix",
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                MethodInfo postfixMethod = typeof(AdvancedPlugin).GetMethod(
                    "LightningZapContextPostfix",
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                if (method == null || prefixMethod == null || postfixMethod == null)
                    return 0;

                PatchWithHarmony(method, new HarmonyMethod(prefixMethod), new HarmonyMethod(postfixMethod));
                return 1;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not patch Lightning Zap mark context: " + ex.Message);
                return 0;
            }
        }

        private int PatchRighteousStrikeAscended()
        {
            try
            {
                MethodInfo method = typeof(SkillsPlugin).GetMethod(
                    "CastRighteousStrike",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                MethodInfo prefixMethod = typeof(AdvancedPlugin).GetMethod(
                    "RighteousStrikeAscendedPrefix",
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                if (method == null || prefixMethod == null)
                    return 0;

                PatchWithHarmony(method, new HarmonyMethod(prefixMethod), null);
                return 1;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not patch Righteous Strike (Ascended): " + ex.Message);
                return 0;
            }
        }

        // v0.18.1: the Altar only lets a Cleric Advance to Paladin once the requirements are met.
        private int PatchAltarAdvancement()
        {
            try
            {
                MethodInfo method = typeof(Plugin).GetMethod("ApplyAdvancementSelection", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo prefixMethod = typeof(AdvancedPlugin).GetMethod("AltarAdvancementPrefix", BindingFlags.Static | BindingFlags.NonPublic);
                if (method == null || prefixMethod == null)
                    return 0;
                PatchWithHarmony(method, new HarmonyMethod(prefixMethod), null);
                return 1;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not patch Altar Advancement: " + ex.Message);
                return 0;
            }
        }

        private static bool AltarAdvancementPrefix(object[] __args)
        {
            if (Instance == null || __args == null || __args.Length < 1)
                return true;
            Player player = Player.m_localPlayer;
            // v0.22.0: every Advancement with a universal kit has the same requirements.
            if (player == null || IhKitFor(Instance.GetClass(player), __args[0] as string) == null || !string.IsNullOrEmpty(Instance.GetAdvancement(player)))
                return true;
            if (Instance.GetPrototypeTotalPending() > 0)
            {
                Instance.ShowMessage("Confirm or clear your pending Class Tiers before Advancing.");
                return false;
            }
            List<string> lines = new List<string>();
            if (Instance.IhAdvanceChecklist(player, lines, __args[0] as string))
                return true;
            List<string> missing = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("-"))
                    missing.Add(lines[i].Substring(2));
            }
            Instance.ShowMessage("Not ready to Advance: " + string.Join(", ", missing.ToArray()));
            return false;
        }

        // An Ascended Paladin's Righteous Strike is handled here; the Cleric version is skipped.
        private static bool RighteousStrikeAscendedPrefix(object[] __args)
        {
            if (Instance == null || __args == null || __args.Length < 1)
                return true;
            Player player = __args[0] as Player;
            if (player == null || Instance.GetAdvancement(player) != "Paladin" || !Instance.IsAscendedSkill("righteous_strike"))
                return true;
            Instance.CastAscendedRighteousStrike(player);
            return false;
        }

        private static void LightningZapContextPrefix(object[] __args)
        {
            if (Instance == null)
                return;
            Instance._paladinMarkSourceContext = string.Empty;
            if (__args == null || __args.Length < 1)
                return;
            // v0.17.0: Lightning Zap no longer applies Judgement Mark. Only Ascended Righteous Strike
            // and the Ascended Signature do.
        }

        private static void LightningZapContextPostfix()
        {
            if (Instance != null)
                Instance._paladinMarkSourceContext = string.Empty;
        }

        private static void SkillLevelPostfix(Skills __instance, object[] __args, ref float __result)
        {
            if (Instance == null || __args == null || __args.Length < 1)
                return;

            if (!(__args[0] is Skills.SkillType))
                return;

            Player player = Instance.GetPlayerFromSkills(__instance);

            if (player == null)
                return;

            float bonus = Instance.GetSkillBonus(player, (Skills.SkillType)__args[0]);

            if (bonus > 0f)
                __result = Mathf.Clamp(__result + bonus, 0f, 100f);
        }

        private static void SkillFactorPostfix(Skills __instance, object[] __args, ref float __result)
        {
            if (Instance == null || __args == null || __args.Length < 1)
                return;

            if (!(__args[0] is Skills.SkillType))
                return;

            Player player = Instance.GetPlayerFromSkills(__instance);

            if (player == null)
                return;

            float bonus = Instance.GetSkillBonus(player, (Skills.SkillType)__args[0]);

            if (bonus > 0f)
                __result = Mathf.Clamp01(__result + bonus / 100f);
        }

        private static void HealthRegenPrefix(object __instance, ref float __0)
        {
            if (Instance == null)
                return;

            Player player = Instance.GetPlayerFromSEMan(__instance);
            if (player == null)
                return;

            if (Instance.IsPaladinPassive(player, "HolyKnight"))
                __0 *= 1f + Mathf.Max(0f, Instance._holyKnightRegen.Value) / 100f;

            // v0.20.8 Cleric's Blessing: +20% HP Regen.
            if (Instance.GetClass(player) == "Cleric")
                __0 *= 1f + Mathf.Max(0f, Instance._clericBlessingRegen.Value) / 100f;
        }

        private static void StaminaRegenPrefix(object __instance, ref float __0)
        {
            if (Instance == null)
                return;

            Player player = Instance.GetPlayerFromSEMan(__instance);
            if (player == null)
                return;

            if (Instance.IsPaladinPassive(player, "HolyKnight"))
                __0 *= 1f + Mathf.Max(0f, Instance._holyKnightRegen.Value) / 100f;

            if (Instance.HasCrossBuff(player))
                __0 *= 1f + Mathf.Max(0f, Instance._holyRelicRegenBuff.Value) / 100f;
        }

        private static void EitrRegenPrefix(object __instance, ref float __0)
        {
            if (Instance == null)
                return;

            Player player = Instance.GetPlayerFromSEMan(__instance);
            if (player == null)
                return;

            if (Instance.IsPaladinPassive(player, "ElementalSavant"))
                __0 *= 1f + Mathf.Max(0f, Instance._paladinElementalEitrRegen.Value) / 100f;

            if (Instance.HasCrossBuff(player))
                __0 *= 1f + Mathf.Max(0f, Instance._holyRelicRegenBuff.Value) / 100f;
        }

        private static void MoveSpeedPrefix(object __instance, ref float __0)
        {
            if (Instance == null)
                return;

            Player player = Instance.GetPlayerFromSEMan(__instance);
            if (player == null)
                return;

            if (Instance.IsPaladinPassive(player, "HolyKnight"))
                __0 *= 1f + Mathf.Max(0f, Instance._holyKnightMoveSpeed.Value) / 100f;

            if (Instance.HasCrossBuff(player))
                __0 *= 1f + Mathf.Max(0f, Instance._holyRelicMoveSpeedBuff.Value) / 100f;
        }

        private static void MaxHealthPostfix(Player __instance, ref float __result)
        {
            if (Instance == null || __instance == null)
                return;

            if (Instance.GetAdvancement(__instance) == "Mercenary")
                __result += Mathf.Max(0f, Instance._mercHealthBonus.Value);

            if (Instance.IsPaladinPassive(__instance, "HolyKnight"))
                __result += Mathf.Max(0f, Instance._holyKnightFlatHealth.Value);

            // v0.20.8 Cleric's Blessing: +35 flat HP (Paladin and Priest keep it).
            if (Instance.GetClass(__instance) == "Cleric")
                __result += Mathf.Max(0f, Instance._clericBlessingHealth.Value);
        }

        private static void MaxStaminaPostfix(Player __instance, ref float __result)
        {
            if (Instance == null || __instance == null)
                return;

            if (Instance.IsPaladinPassive(__instance, "HolyKnight"))
                __result += Mathf.Max(0f, Instance._holyKnightFlatStamina.Value);
        }

        private static void ArmorPostfix(Player __instance, ref float __result)
        {
            if (Instance == null || __instance == null)
                return;

            // v0.20.8: the permanent Priest Armor bonus is retired (see Heaven's Crucible).
        }

        private static void PaladinSetMaxEitrPrefix(Player __instance, ref float __0)
        {
            if (Instance == null || __instance == null)
                return;

            if (Instance.IsPaladinPassive(__instance, "ElementalSavant"))
                __0 += Mathf.Max(0f, Instance._paladinElementalFlatEitr.Value);
        }

        private static void DamagePrefix(Character __instance, object[] __args)
        {
            if (Instance == null || __args == null || __args.Length < 1)
                return;

            if (!(__args[0] is HitData))
                return;

            HitData hit = (HitData)__args[0];

            Player targetPlayer = __instance as Player;

            if (targetPlayer != null)
                Instance.ModifyIncomingDamage(targetPlayer, hit);

            Player attacker = hit.GetAttacker() as Player;

            if (attacker != null)
                Instance.ModifyOutgoingDamage(attacker, __instance, hit);
        }

        private void ModifyOutgoingDamage(Player attacker, Character target, HitData hit)
        {
            string advancement = GetAdvancement(attacker);

            if (advancement == "Mercenary" && IsWeaponHit(hit))
                ScaleDamage(hit, 1f + Mathf.Max(0f, _mercAttackDamage.Value) / 100f);

            if (advancement == "Mercenary")
                TryBuildMercenaryFury(attacker, target, hit);

            if (advancement == "Paladin" && !_judgementDetonationInProgress)
                ApplyPaladinJudgementInteraction(attacker, target, hit);

            // v0.20.8 Holy Trinity: Slash and Pierce each rise to at least 50% of the hit's Blunt, never lowered.
            if (advancement == "Paladin" && IsWeaponHit(hit) && DragonCombat.IsHolyTrinityActive(attacker))
            {
                float floor = hit.m_damage.m_blunt * Mathf.Max(0f, _holyTrinityMinPercent.Value) / 100f;
                if (hit.m_damage.m_slash < floor) hit.m_damage.m_slash = floor;
                if (hit.m_damage.m_pierce < floor) hit.m_damage.m_pierce = floor;
            }

            if (advancement == "Paladin" && IsPaladinPassive(attacker, "ElementalSavant"))
            {
                float factor = 1f + Mathf.Max(0f, _paladinElementalBonus.Value) / 100f;
                hit.m_damage.m_fire *= factor;
                hit.m_damage.m_frost *= factor;
                hit.m_damage.m_lightning *= factor;
                hit.m_damage.m_poison *= factor;
                hit.m_damage.m_spirit *= factor;
            }

            if (HasCrossBuff(attacker))
                ScaleDamage(hit, 1f + Mathf.Max(0f, _holyRelicDamageBuff.Value) / 100f);
        }

        private void ModifyIncomingDamage(Player target, HitData hit)
        {
            if (target != null && GetAdvancement(target) == "Sword Master" && Time.time <= _emptySheathCounterUntil)
            {
                Character attacker = hit == null ? null : hit.GetAttacker();
                if (attacker != null && attacker != target && IsEnemy(target, attacker))
                {
                    _emptySheathCounterUntil = 0f;
                    ScaleDamage(hit, 0f);
                    hit.m_pushForce = 0f;
                    StartCoroutine(EmptySheathCounterRoutine(target, attacker));
                    return;
                }
            }

            BarrierState barrier;
            int id = target.GetInstanceID();

            if (_barriers.TryGetValue(id, out barrier) &&
                Time.time < barrier.EndTime &&
                barrier.HP > 0f)
            {
                float raw = TotalDamage(hit);
                float effective = Mathf.Max(0f, raw - barrier.Armor);

                if (effective <= barrier.HP)
                {
                    barrier.HP -= effective;
                    ScaleDamage(hit, 0f);
                    _barriers[id] = barrier;
                }
                else
                {
                    float overflow = effective - barrier.HP;
                    _barriers.Remove(id);

                    float ratio = raw <= 0f ? 0f : Mathf.Clamp01(overflow / raw);
                    ScaleDamage(hit, ratio);
                }

                if (_enableVfx.Value && target == Player.m_localPlayer)
                    StartCoroutine(BarrierHitVisual(target));

                return;
            }

        }

        private void ApplyPaladinJudgementInteraction(Player attacker, Character target, HitData hit)
        {
            if (attacker == null || target == null || hit == null)
                return;

            int id = target.GetInstanceID();
            JudgementMarkState mark;
            bool hasMark = _judgementMarks.TryGetValue(id, out mark) &&
                           mark != null && mark.Target == target && Time.time < mark.EndTime;
            if (!hasMark)
                _judgementMarks.Remove(id);

            string source = _paladinMarkSourceContext;
            bool isMarkingHit = !string.IsNullOrEmpty(source);

            bool detonated = false;
            if (hasMark)
            {
                // v0.17.0: any Mark-applying skill (Ascended Righteous Strike / Ascended Signature)
                // hitting a marked target detonates and consumes the Mark.
                if (isMarkingHit)
                {
                    ScaleDamage(hit, Mathf.Max(1f, _judgementCrossMultiplier.Value));
                    TriggerJudgementDetonation(attacker, target);
                    _judgementMarks.Remove(id);
                    _judgementDetonatedOnLastHit = true;
                    detonated = true;
                }
                else
                {
                    ScaleDamage(hit, Mathf.Max(1f, _judgementMarkedMultiplier.Value));
                    if (hit.m_damage.m_lightning > 0.001f)
                        DragonCombat.ApplyZap(attacker, target, 0f, 0f, 0f);
                    else
                        DragonCombat.ApplyCripple(target, Mathf.Max(0.1f, _judgementCrippleDuration.Value));
                }
            }

            if (isMarkingHit && !detonated)
            {
                JudgementMarkState fresh = new JudgementMarkState();
                fresh.Target = target;
                fresh.Source = source;
                fresh.EndTime = Time.time + Mathf.Max(0.1f, _judgementMarkDuration.Value);
                _judgementMarks[id] = fresh;
            }
        }

        private bool HasActiveJudgementMark(Character target)
        {
            if (target == null)
                return false;
            JudgementMarkState mark;
            int id = target.GetInstanceID();
            if (!_judgementMarks.TryGetValue(id, out mark) || mark == null || mark.Target != target || Time.time >= mark.EndTime)
            {
                _judgementMarks.Remove(id);
                return false;
            }
            return true;
        }

        private void TriggerJudgementDetonation(Player attacker, Character target)
        {
            if (attacker == null || target == null || target.IsDead())
                return;
            _judgementDetonationInProgress = true;
            try
            {
                HitData detonation = new HitData();
                detonation.m_damage.m_lightning = Mathf.Max(0f, _judgementDetonationLightning.Value);
                detonation.m_damage.m_spirit = Mathf.Max(0f, _judgementDetonationSpirit.Value);
                detonation.m_point = target.transform.position;
                detonation.m_dir = (target.transform.position - attacker.transform.position).normalized;
                detonation.m_pushForce = 16f;
                detonation.SetAttacker(attacker);
                target.Damage(detonation);
            }
            finally
            {
                _judgementDetonationInProgress = false;
            }

            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(target.transform.position + Vector3.up * 0.08f, 0.25f, 2.5f, 0.35f,
                    new Color(1f, 0.82f, 0.28f, 0.96f), 0.12f));
        }

        private bool IsWeaponHit(HitData hit)
        {
            try
            {
                FieldInfo field = typeof(HitData).GetField(
                    "m_skill",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (field == null)
                    return false;

                object value = field.GetValue(hit);

                if (value == null)
                    return false;

                return value.ToString() != "None";
            }
            catch
            {
                return false;
            }
        }

        private float TotalDamage(HitData hit)
        {
            return Mathf.Max(
                0f,
                hit.m_damage.m_blunt +
                hit.m_damage.m_slash +
                hit.m_damage.m_pierce +
                hit.m_damage.m_fire +
                hit.m_damage.m_frost +
                hit.m_damage.m_lightning +
                hit.m_damage.m_poison +
                hit.m_damage.m_spirit
            );
        }

        private void ScaleDamage(HitData hit, float factor)
        {
            factor = Mathf.Max(0f, factor);
            hit.m_damage.m_blunt *= factor;
            hit.m_damage.m_slash *= factor;
            hit.m_damage.m_pierce *= factor;
            hit.m_damage.m_fire *= factor;
            hit.m_damage.m_frost *= factor;
            hit.m_damage.m_lightning *= factor;
            hit.m_damage.m_poison *= factor;
            hit.m_damage.m_spirit *= factor;
        }

        private Player GetPlayerFromSkills(Skills skills)
        {
            if (skills == null)
                return null;

            try
            {
                FieldInfo[] fields = typeof(Skills).GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < fields.Length; i++)
                {
                    Player player = fields[i].GetValue(skills) as Player;

                    if (player != null)
                        return player;
                }
            }
            catch
            {
            }

            return null;
        }

        private Player GetPlayerFromSEMan(object seman)
        {
            if (seman == null)
                return null;

            try
            {
                FieldInfo[] fields = seman.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < fields.Length; i++)
                {
                    Player player = fields[i].GetValue(seman) as Player;

                    if (player != null)
                        return player;
                }
            }
            catch
            {
            }

            return null;
        }
        private IEnumerator AnimateHalfmoonArc(Vector3 center, Vector3 forward, float radius)
        {
            GameObject obj = new GameObject("AlbedoHalfmoon");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 28;
            line.startWidth = 0.36f;
            line.endWidth = 0.12f;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float duration = 0.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float currentRadius = Mathf.Lerp(radius * 0.65f, radius, t);
                Color color = new Color(0.52f, 0.83f, 1f, 1f - t);

                line.startColor = color;
                line.endColor = new Color(0.90f, 0.97f, 1f, color.a);

                for (int i = 0; i < line.positionCount; i++)
                {
                    float p = (float)i / (float)(line.positionCount - 1);
                    float angle = Mathf.Lerp(-80f, 80f, p) * Mathf.Deg2Rad;
                    Vector3 point = forward * Mathf.Cos(angle) * currentRadius + right * Mathf.Sin(angle) * currentRadius;
                    point.y = Mathf.Sin(p * Mathf.PI) * 4.6f;
                    line.SetPosition(i, center + point);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(obj);
        }

        private IEnumerator AnimateRing(Vector3 center, float startRadius, float endRadius, float duration, Color color, float width)
        {
            GameObject obj = new GameObject("AlbedoAdvancedRing");
            LineRenderer line = obj.AddComponent<LineRenderer>();
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
                Color frame = color;
                float fade = t <= 0.72f ? 1f : Mathf.Clamp01(1f - (t - 0.72f) / 0.28f);
                frame.a = color.a * fade;
                line.startColor = frame;
                line.endColor = frame;

                for (int i = 0; i < line.positionCount; i++)
                {
                    float angle = ((float)i / (float)(line.positionCount - 1)) * Mathf.PI * 2f;
                    line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(obj);
        }

        private IEnumerator AnimateAura(Player player, Color color, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (player == null)
                    yield break;

                StartCoroutine(
                    AnimateRing(
                        player.transform.position + Vector3.up * 0.8f,
                        0.5f,
                        2.1f,
                        0.35f,
                        color,
                        0.05f
                    )
                );

                yield return new WaitForSeconds(0.28f);
                elapsed += 0.28f;
            }
        }

        private Vector3 GetGroundedCrossCenter(Vector3 targetPoint, float height)
        {
            return targetPoint + Vector3.up * (Mathf.Max(0.1f, height) * 0.5f + 0.04f);
        }

        private GameObject CreateCross(
            Vector3 center,
            Color color,
            float height,
            float width,
            float lineWidth,
            bool visible
        )
        {
            GameObject root = new GameObject("DragonsAltarHolyCross");
            root.transform.position = center;

            float physicalThickness = Mathf.Max(0.40f, lineWidth * 4f);

            GameObject vertical = new GameObject("vertical");
            vertical.transform.SetParent(root.transform, false);

            BoxCollider verticalCollider = vertical.AddComponent<BoxCollider>();
            verticalCollider.center = Vector3.zero;
            verticalCollider.size = new Vector3(physicalThickness, height, physicalThickness);
            verticalCollider.isTrigger = false;
            verticalCollider.enabled = false;

            GameObject horizontal = new GameObject("horizontal");
            horizontal.transform.SetParent(root.transform, false);

            BoxCollider horizontalCollider = horizontal.AddComponent<BoxCollider>();
            horizontalCollider.center = new Vector3(0f, height * 0.12f, 0f);
            horizontalCollider.size = new Vector3(width, physicalThickness, physicalThickness);
            horizontalCollider.isTrigger = false;
            horizontalCollider.enabled = false;

            if (visible)
            {
                Shader shader = Shader.Find("Sprites/Default");

                LineRenderer v = vertical.AddComponent<LineRenderer>();
                v.useWorldSpace = false;
                v.positionCount = 2;
                v.startWidth = lineWidth;
                v.endWidth = lineWidth;
                v.startColor = color;
                v.endColor = color;
                v.SetPosition(0, new Vector3(0f, -height * 0.5f, 0f));
                v.SetPosition(1, new Vector3(0f, height * 0.5f, 0f));

                if (shader != null)
                    v.material = new Material(shader);

                LineRenderer h = horizontal.AddComponent<LineRenderer>();
                h.useWorldSpace = false;
                h.positionCount = 2;
                h.startWidth = lineWidth;
                h.endWidth = lineWidth;
                h.startColor = color;
                h.endColor = color;
                h.SetPosition(0, new Vector3(-width * 0.5f, height * 0.12f, 0f));
                h.SetPosition(1, new Vector3(width * 0.5f, height * 0.12f, 0f));

                if (shader != null)
                    h.material = new Material(shader);

                GameObject lightObject = new GameObject("light");
                lightObject.transform.SetParent(root.transform, false);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.range = Mathf.Max(height, width) * 1.8f;
                light.intensity = 1.4f;
                light.shadows = LightShadows.None;
            }

            return root;
        }

        private void SetCrossPhysical(GameObject cross, bool physical)
        {
            if (cross == null)
                return;

            Collider[] colliders = cross.GetComponentsInChildren<Collider>();

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = physical;
            }
        }

        private void CreateCrescentVerticalSlashVisual(
            Vector3 groundPoint,
            Vector3 forward,
            float width,
            float height,
            float lifetime
        )
        {
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;

            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Color core = new Color(0.72f, 0.94f, 1f, 0.95f);
            Color glow = new Color(0.30f, 0.72f, 1f, 0.50f);
            Shader shader = Shader.Find("Sprites/Default");

            float[] sideOffsets = new float[]
            {
                -width * 0.5f,
                0f,
                width * 0.5f
            };

            for (int i = 0; i < sideOffsets.Length; i++)
            {
                GameObject obj = new GameObject("DragonCrescentVerticalSlash");
                LineRenderer line = obj.AddComponent<LineRenderer>();

                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = i == 1 ? 0.17f : 0.08f;
                line.endWidth = line.startWidth;
                line.startColor = i == 1 ? core : glow;
                line.endColor = i == 1 ? core : glow;

                Vector3 basePoint = groundPoint + right * sideOffsets[i] + Vector3.up * 0.08f;
                line.SetPosition(0, basePoint);
                line.SetPosition(1, basePoint + Vector3.up * height);

                if (shader != null)
                    line.material = new Material(shader);

                Destroy(obj, Mathf.Max(0.04f, lifetime));
            }
        }

        private void CreateLightning(Vector3 point, Color color, float lifetime)
        {
            GameObject obj = new GameObject("AlbedoAdvancedLightning");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 9;
            line.startWidth = 0.12f;
            line.endWidth = 0.28f;
            line.startColor = color;
            line.endColor = color;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            Vector3 top = point + Vector3.up * 11f;

            for (int i = 0; i < line.positionCount; i++)
            {
                float t = (float)i / (float)(line.positionCount - 1);
                Vector3 position = Vector3.Lerp(top, point, t);
                float offset = Mathf.Sin((float)i * 4.91f + Time.time * 10f) * 0.38f;
                position.x += offset;
                position.z -= offset * 0.6f;
                line.SetPosition(i, position);
            }

            Destroy(obj, Mathf.Max(0.05f, lifetime));
        }

        private IEnumerator BarrierVisual(Player player)
        {
            if (player == null)
                yield break;

            Vector3 center = player.transform.position + Vector3.up * 1f;

            yield return AnimateRing(
                center,
                0.5f,
                2.4f,
                0.55f,
                new Color(0.55f, 0.91f, 1f, 0.92f),
                0.10f
            );
        }

        private IEnumerator BarrierHitVisual(Player player)
        {
            if (player == null)
                yield break;

            yield return AnimateRing(
                player.transform.position + Vector3.up * 1f,
                0.7f,
                1.7f,
                0.28f,
                new Color(0.72f, 0.94f, 1f, 0.95f),
                0.09f
            );
        }

        // =====================================================================================
        // v0.18.0 Immortal Heroes progression (Paladin first): Level, Tier Points, Tiers,
        // unlock gates, Ascensions and the /ih command. Everything is saved per character in
        // Player.m_customData. XP is a separate mod later; until then Level comes from /ih.
        // =====================================================================================
        private const string IhLevelKey = "ImmortalHeroes.Level";
        private const string IhTiersKey = "ImmortalHeroes.Tiers";
        private const string IhAscendedKey = "ImmortalHeroes.Ascended";
        private const string IhBonusClassKey = "ImmortalHeroes.BonusClassPoints";
        private const string IhBonusAdvKey = "ImmortalHeroes.BonusAdvancementPoints";
        private const int IhMaxLevel = 80;
        private const string IhUltimate = "electric_smite";
        private const string IhGrace = "heavens_light";
        private const string IhAscendedClassSkill = "righteous_strike";
        private static readonly string[] IhClassSkills = { "lightning_zap", "righteous_strike", "holy_wave" };
        private static readonly string[] IhAdvSkills = { "goddess_relic", "judgement_hammer", "shield_charge", "fallen_angel", "ray_of_hope" };
        private static readonly string[] IhSignatureSkills = { "goddess_relic", "judgement_hammer" };
        private static readonly string[] IhNormalAdvSkills = { "shield_charge", "fallen_angel", "ray_of_hope" };

        private const string IhPriestUltimate = "lightning_tempest";
        private const string IhPriestGrace = "grand_sigil";
        private static readonly string[] IhPriestAdvSkills = { "lightning_relic", "holy_relic", "divine_intervention", "grand_cross", "heavens_judgement" };
        private static readonly string[] IhPriestSignatures = { "lightning_relic", "holy_relic" };
        private static readonly string[] IhPriestNormalAdvSkills = { "divine_intervention", "grand_cross", "heavens_judgement" };
        private const string IhPriestAscendedClassSkill = "holy_wave";

        // v0.22.0 universal kit: every Advancement Class is data. Class skills map 1:1 onto the
        // chassis Class slots (Lightning Zap / Righteous Strike / Holy Wave), the 5 AC skills onto
        // the Paladin slots (2 Signatures, the Lv24 skill, the Lv32 pair), then Ultimate and Grace.
        private sealed class IhKit
        {
            public readonly string Class, Ac, Ultimate, Grace, AscendedClass, SpecialAscension;
            public readonly string[] ClassSkills, Adv, Signatures, Normals;
            public IhKit(string cls, string ac, string[] classSkills, string[] adv, string ultimate, string grace, string ascendedClass, string specialAscension)
            {
                Class = cls; Ac = ac; ClassSkills = classSkills; Adv = adv; Ultimate = ultimate; Grace = grace;
                AscendedClass = ascendedClass; SpecialAscension = specialAscension ?? "";
                Signatures = new string[] { adv[0], adv[1] };
                List<string> normals = new List<string>();
                for (int i = 2; i < adv.Length; i++)
                    if (adv[i] != SpecialAscension) normals.Add(adv[i]);
                Normals = normals.ToArray();
            }
        }
        private static readonly string[] IhClassSlots = { "lightning_zap", "righteous_strike", "holy_wave" };
        private static readonly string[] IhAdvSlots = { "goddess_relic", "judgement_hammer", "shield_charge", "fallen_angel", "ray_of_hope" };
        private static readonly string[] IhWarriorSkills = { "heavy_slash", "impact_wave", "impact_punch" };
        private static readonly string[] IhSorcererSkills = { "flame_burst", "glacial_descent", "stonefang_eruption" };
        private static readonly IhKit[] IhKits =
        {
            new IhKit("Cleric", "Paladin", IhClassSkills, IhAdvSkills, IhUltimate, IhGrace, IhAscendedClassSkill, null),
            new IhKit("Cleric", "Priest", IhClassSkills, IhPriestAdvSkills, IhPriestUltimate, IhPriestGrace, IhPriestAscendedClassSkill, null),
            new IhKit("Warrior", "Sword Master", IhWarriorSkills, new string[] { "moonlight_splitter", "crescent_cleave", "blade_storm", "frenzied_charge", "eclipse" }, "halfmoon_slash", "knights_guidance", "impact_wave", null),
            new IhKit("Warrior", "Mercenary", IhWarriorSkills, new string[] { "stomp", "circle_swing", "bonecrusher", "seismic_guillotine", "punishing_bomb" }, "whirlwind", "battlecry", "heavy_slash", null),
            new IhKit("Sorcerer", "Wizard", IhSorcererSkills, new string[] { "meteor_fall", "gravity_dominion", "astral_railcannon", "astral_greatblade", "frost_nova" }, "elemental_cataclysm", "clockwork", "glacial_descent", null),
            // Spellcaster: Void Step has its own Lv 42 Ascension on top of the normal one (5 total).
            new IhKit("Sorcerer", "Spellcaster", IhSorcererSkills, new string[] { "arcane_phalanx", "afterimage_arsenal", "void_step", "rift_echo", "gravity_blast" }, "arcane_rupture", "rift_walker", "stonefang_eruption", "void_step")
        };
        private static IhKit IhKitFor(string cls, string ac)
        {
            for (int i = 0; i < IhKits.Length; i++)
                if (IhKits[i].Class == cls && IhKits[i].Ac == ac) return IhKits[i];
            return null;
        }
        // The kit that owns an Advancement-side skill (AC skills, Ultimate, Grace are unique per kit).
        private static IhKit IhKitOf(string id)
        {
            for (int i = 0; i < IhKits.Length; i++)
            {
                IhKit k = IhKits[i];
                if (IhContains(k.Adv, id) || k.Ultimate == id || k.Grace == id) return k;
            }
            return null;
        }
        private static string[] IhClassSkillsOf(string cls)
        {
            if (cls == "Cleric") return IhClassSkills;
            if (cls == "Warrior") return IhWarriorSkills;
            if (cls == "Sorcerer") return IhSorcererSkills;
            return new string[0];
        }
        private static string IhClassOfSkill(string id)
        {
            if (IhContains(IhClassSkills, id)) return "Cleric";
            if (IhContains(IhWarriorSkills, id)) return "Warrior";
            if (IhContains(IhSorcererSkills, id)) return "Sorcerer";
            return "";
        }
        private static bool IhIsAnyClassSkill(string id)
        {
            return IhClassOfSkill(id).Length > 0;
        }
        private static string[] IhBranchesOf(string cls)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < IhKits.Length; i++)
                if (IhKits[i].Class == cls) list.Add(IhKits[i].Ac);
            return list.ToArray();
        }
        private IhKit IhPlayerKit(Player player)
        {
            return player == null ? null : IhKitFor(GetClass(player), GetAdvancement(player));
        }
        private IhKit IhTreeKit()
        {
            Player player = Player.m_localPlayer;
            return player == null ? null : IhKitFor(GetClass(player), IhTreeBranch());
        }
        private string[] IhPlayerClassSkills(Player player)
        {
            return player == null ? new string[0] : IhClassSkillsOf(GetClass(player));
        }
        private string _ihPreviewBranch = "Paladin";
        private string _ihUiOwner = "";
        private Texture2D _ihPriestBackdropTex;
        private Texture2D _ihPriestLockedTex;
        private string IhTreeBranch()
        {
            Player player = Player.m_localPlayer;
            if (player == null) return _ihPreviewBranch;
            string cls = GetClass(player);
            string branch = GetAdvancement(player);
            if (IhKitFor(cls, branch) != null) return branch;
            // Before Advancement: the previewed branch of this Class (first branch by default).
            string[] branches = IhBranchesOf(cls);
            if (branches.Length > 0 && Array.IndexOf(branches, _ihPreviewBranch) < 0)
                _ihPreviewBranch = branches[0];
            return _ihPreviewBranch;
        }
        private bool IhIsPriest(Player player)
        {
            return GetClass(player) == "Cleric" && GetAdvancement(player) == "Priest";
        }
        // v0.22.0: any Advanced Class with a universal kit (name kept from the Cleric-only days).
        private bool IhIsClericAdvanced(Player player)
        {
            return IhPlayerKit(player) != null;
        }
        private static bool IhPriestSkill(string id)
        {
            return IhContains(IhPriestAdvSkills, id) || id == IhPriestUltimate || id == IhPriestGrace;
        }
        private static bool IhIsUltimate(string id)
        {
            for (int i = 0; i < IhKits.Length; i++)
                if (IhKits[i].Ultimate == id) return true;
            return false;
        }
        private static bool IhIsSignature(string id)
        {
            IhKit k = IhKitOf(id);
            return k != null && IhContains(k.Signatures, id);
        }
        private string[] IhBranchSkills(Player player)
        {
            IhKit k = IhPlayerKit(player);
            return k == null ? new string[0] : k.Adv;
        }
        // Before Advancement the (locked) Grace of the previewed branch is shown.
        private string IhGraceFor(Player player)
        {
            IhKit k = IhPlayerKit(player);
            if (k == null && player != null && player == Player.m_localPlayer)
                k = IhTreeKit();
            return k == null ? "" : k.Grace;
        }
        private ReferenceNodeUi[] IhTreeNodes()
        {
            string branch = IhTreeBranch();
            if (branch == "Priest") return ClericPriestReferenceNodes;
            if (branch == "Paladin") return ClericPaladinReferenceNodes;
            IhKit k = IhTreeKit();
            return k == null ? ClericPaladinReferenceNodes : IhKitNodes(k);
        }
        // Chassis slot of any skill id (anchors, frames, openings, lock regions).
        private static string IhTemplateSlot(string id)
        {
            for (int i = 0; i < IhKits.Length; i++)
            {
                IhKit k = IhKits[i];
                int c = Array.IndexOf(k.ClassSkills, id);
                if (c >= 0) return IhClassSlots[c];
                int a = Array.IndexOf(k.Adv, id);
                if (a >= 0) return IhAdvSlots[a];
                if (k.Ultimate == id) return IhUltimate;
                if (k.Grace == id) return IhGrace;
            }
            return id;
        }

        private ConfigEntry<float> _ihTierPowerPercent;
        private ConfigEntry<bool> _ihUnlockAll;
        private readonly Dictionary<DamageConfig, string> _damageSkillIds = new Dictionary<DamageConfig, string>();
        private bool _ihCommandRegistered;
        private string _ihTierCacheRaw;
        private readonly Dictionary<string, int> _ihTierCache = new Dictionary<string, int>();
        private string _ihAscCacheRaw;
        private readonly HashSet<string> _ihAscCache = new HashSet<string>();
        private GUIStyle _ihHeaderStyle;
        private GUIStyle _ihCountStyle;
        private GUIStyle _ihLockTextStyle;
        private Texture2D _ihStarFullTex;
        private Texture2D _ihStarPendingTex;
        private Texture2D _ihStarEmptyTex;
        private Texture2D _ihPadlockTex;
        private Texture2D _ihPreAdvanceBackdropTex;
        private Texture2D _ihRsNormalIconTex;
        private Texture2D _ihLockedBackdropTex;

        // v0.18.2: a locked node copies its region from the greyed backdrop (reference px, x0 y0 x1 y1).
        // Must match LOCKED_REGIONS in tools/build_ui_assets.py.
        private static readonly Dictionary<string, Rect> LockedRegions = new Dictionary<string, Rect>
        {
            { "goddess_relic", Rect.MinMaxRect(352f, 140f, 458f, 250f) },
            { "judgement_hammer", Rect.MinMaxRect(352f, 273f, 458f, 378f) },
            { "heavens_light", Rect.MinMaxRect(362f, 393f, 470f, 502f) },
            { "shield_charge", Rect.MinMaxRect(532f, 140f, 634f, 250f) },
            { "fallen_angel", Rect.MinMaxRect(670f, 140f, 772f, 250f) },
            { "ray_of_hope", Rect.MinMaxRect(670f, 273f, 772f, 378f) },
            { "electric_smite", Rect.MinMaxRect(826f, 192f, 980f, 358f) },
            { "grace_slot", Rect.MinMaxRect(660f, 539f, 723f, 601f) }
        };

        private bool IhDrawLockedRegion(string key)
        {
            Rect r;
            Texture2D locked = IhKitBackdrop(IhTreeKit(), true);
            if (locked == null) locked = _ihLockedBackdropTex;
            if (locked == null || !LockedRegions.TryGetValue(IhTemplateSlot(key), out r))
                return false;
            Rect uv = new Rect(r.x / 1011f, 1f - r.yMax / 662f, r.width / 1011f, r.height / 662f);
            GUI.DrawTextureWithTexCoords(IhSnap(ScaleReferenceRect(r.x, r.y, r.width, r.height)), locked, uv);
            return true;
        }

        private static Rect IhSnap(Rect r)
        {
            return new Rect(Mathf.Round(r.x), Mathf.Round(r.y), Mathf.Round(r.width), Mathf.Round(r.height));
        }

        private void BindImmortalProgression()
        {
            _ihTierPowerPercent = Config.Bind("Progression", "TierPowerPercent", 10f, "Each Tier adds this percent to the skill's damage and healing.");
            _ihUnlockAll = Config.Bind("Testing", "UnlockAllSkills", false, "Ignore Level / Tier Point unlock gates (testing only).");

            _damageSkillIds[_goddessDamage] = "goddess_relic";
            _damageSkillIds[_goddessAscDamage] = "goddess_relic";
            _damageSkillIds[_hammerDamage] = "judgement_hammer";
            _damageSkillIds[_shieldChargeDamage] = "shield_charge";
            _damageSkillIds[_angelDamage] = "fallen_angel";
            _damageSkillIds[_divineDamage] = IhUltimate;
            _damageSkillIds[_divineTrailDamage] = IhUltimate;
            _damageSkillIds[_smiteStormDamage] = IhUltimate;
            _damageSkillIds[_rsAscDamage] = IhAscendedClassSkill;
            _damageSkillIds[_rsAscTrailDamage] = IhAscendedClassSkill;

            _damageSkillIds[_lightningRelicDamage] = "lightning_relic";
            _damageSkillIds[_interventionDamage] = "divine_intervention";
            _damageSkillIds[_grandCrossDamage] = "grand_cross";
            _damageSkillIds[_heavensDamage] = "heavens_judgement";
            _damageSkillIds[_tempestDamage] = IhPriestUltimate;
            DragonCombat.SkillPowerProvider = IhSkillPower;
            DragonCombat.BucklerParryHandler = OnBucklerParry;
            _damageSkillIds[_relicAscBlastDamage] = "lightning_relic";
            _damageSkillIds[_crossAscBurstDamage] = "grand_cross";
            _damageSkillIds[_pillarDamage] = "heavens_judgement";
        }

        private float DamagePower(Player attacker, DamageConfig cfg)
        {
            string skillId;
            if (cfg == null || !_damageSkillIds.TryGetValue(cfg, out skillId))
                return 1f;
            return IhSkillPower(attacker, skillId) * IhEmpowerFactor(skillId);
        }

        // +TierPowerPercent per Tier (Ultimate uses its automatic Tier).
        private float IhSkillPower(Player player, string skillId)
        {
            if (player == null || player != Player.m_localPlayer || _ihTierPowerPercent == null)
                return 1f;
            float power = 1f + Mathf.Max(0f, _ihTierPowerPercent.Value) / 100f * IhGetTier(player, skillId);
            // Lightning Zap / Righteous Strike damage is computed in Skills.cs through this provider;
            // they deal no healing, so the parry empowerment can ride on it for them only.
            if (skillId == "lightning_zap" || skillId == "righteous_strike")
                power *= IhEmpowerFactor(skillId);
            return power;
        }

        // ------------------------------------------------------------------ v0.20.9 Buckler Parry
        private float IhEmpowerFactor(string skillId)
        {
            if (skillId != _empoweredSkill || Time.time > _empoweredUntil)
                return 1f;
            return 1f + Mathf.Max(0f, _parryEmpowerPercent.Value) / 100f;
        }

        // Skills the Priest can cast that deal damage (they consume the empowerment).
        private static bool IhPriestDamageSkill(string id)
        {
            switch (id)
            {
                case "lightning_zap": case "righteous_strike": case "lightning_relic": case "divine_intervention":
                case "grand_cross": case "heavens_judgement": case "lightning_tempest":
                    return true;
            }
            return false;
        }

        // Seconds one cast of the skill keeps dealing damage ("that entire skill instance").
        private float IhSkillInstanceSeconds(string id)
        {
            switch (id)
            {
                case "lightning_relic": return Mathf.Max(0f, _lightningRelicDuration.Value) + 3f;
                case "grand_cross": return Mathf.Max(0f, _grandCrossWindup.Value) + Mathf.Max(0f, _grandCrossTravelTime.Value) + 1f;
                case "heavens_judgement": return Mathf.Max(0f, _heavensWindup.Value) + Mathf.Max(0f, _heavensDuration.Value) + 1f;
                case "lightning_tempest": return Mathf.Max(0f, _tempestDuration.Value) + 2f;
                case "divine_intervention": return Mathf.Max(0f, _interventionWindup.Value) + 1.5f;
            }
            return 2.5f; // Lightning Zap / Righteous Strike: wind-up + impact
        }

        // ------------------------------------------------------------------ v0.21.0 Sanctified
        // Applied only by Ascended Holy Wave and the Ascended Signature. Touching an ally who is
        // already Sanctified makes them Bloom (heal + holy pulse) and removes it: it never loops.
        private void IhSanctify(Player caster, Player ally)
        {
            if (caster == null || ally == null || ally.IsDead())
                return;
            int id = ally.GetInstanceID();
            float until;
            if (_sanctifiedUntil.TryGetValue(id, out until) && Time.time < until)
            {
                _sanctifiedUntil.Remove(id);
                Heal(ally, ally.GetMaxHealth() * Mathf.Max(0f, _bloomHealPercent.Value) / 100f);
                float r = Mathf.Max(0.5f, _bloomRadius.Value);
                List<Character> enemies = GetSphereTargets(caster, ally.transform.position, r);
                for (int i = 0; i < enemies.Count; i++)
                    DealDamageScaled(caster, enemies[i], _bloomDamage, 1f, 6f, false);
                if (_enableVfx.Value)
                    StartCoroutine(AnimateRing(ally.transform.position + Vector3.up * 0.1f, 0.3f, r, 0.35f, new Color(1f, 0.95f, 0.65f, 0.95f), 0.10f));
                return;
            }
            _sanctifiedUntil[id] = Time.time + Mathf.Max(0.5f, _sanctifiedDuration.Value);
        }

        private List<Player> IhAlliesInRadius(Player caster, Vector3 center, float radius)
        {
            List<Player> allies = GetPlayersInSphere(center, radius);
            if (caster != null && !allies.Contains(caster) && Vector3.Distance(caster.transform.position, center) <= radius)
                allies.Add(caster);
            return allies;
        }

        // Ascended Holy Wave: 10m, Sanctifies, low-HP allies get double instant heal, echo wave after 2s.
        private void CastAscendedHolyWave(Player player)
        {
            const string sk = "albedo.customclasses.skills";
            if (!BeginCast(player, "Priest.AscendedHolyWave", IhCfg(sk, "Cleric.Holy Wave", "Cooldown", 8f), IhCfg(sk, "Cleric.Holy Wave", "StaminaCost", 25f)))
                return;
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlaySkillPose(player, "Wave", 0.6f);
            ShowMessage("Holy Wave");
            StartCoroutine(AscendedHolyWaveRoutine(player));
        }

        private IEnumerator AscendedHolyWaveRoutine(Player player)
        {
            const string sk = "albedo.customclasses.skills";
            float power = IhSkillPower(player, "holy_wave");
            float instant = IhCfg(sk, "Cleric.Holy Wave", "ImmediateHeal", 25f) * power;
            float perSecond = IhCfg(sk, "Cleric.Holy Wave", "HealPercentPerSecond", 5f) * power;
            float duration = IhCfg(sk, "Cleric.Holy Wave", "Duration", 6f);
            // Aim at an ally (within range) to centre the wave on them; otherwise on yourself.
            Player aimed = IhAimedAlly(player, Mathf.Max(1f, _ahwAllyRange.Value));
            Vector3 center = aimed != null ? aimed.transform.position : player.transform.position;
            float radius = Mathf.Max(1f, _ahwRadius.Value);
            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, 0.6f, radius, 0.6f, new Color(0.62f, 1f, 0.70f, 0.95f), 0.12f));
            List<Player> allies = IhAlliesInRadius(player, center, radius);
            for (int i = 0; i < allies.Count; i++)
            {
                Player ally = allies[i];
                bool low = ally.GetHealth() < ally.GetMaxHealth() * Mathf.Max(0f, _ahwLowHp.Value) / 100f;
                Heal(ally, low ? instant * 2f : instant);
                StartCoroutine(IhHealOverTime(ally, perSecond, duration));
                IhSanctify(player, ally);
            }
            yield return new WaitForSeconds(Mathf.Max(0.1f, _ahwEchoDelay.Value));
            if (player == null || player.IsDead())
                yield break;
            // Echo: smaller and weaker, never Sanctifies (so one cast cannot Bloom by itself).
            center = player.transform.position;
            float echoRadius = Mathf.Max(1f, _ahwEchoRadius.Value);
            float echo = instant * Mathf.Max(0f, _ahwEchoPercent.Value) / 100f;
            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, 0.4f, echoRadius, 0.45f, new Color(0.62f, 1f, 0.70f, 0.80f), 0.09f));
            allies = IhAlliesInRadius(player, center, echoRadius);
            for (int i = 0; i < allies.Count; i++)
                Heal(allies[i], echo);
        }

        private Player IhAimedAlly(Player caster, float range)
        {
            if (caster == null || GameCamera.instance == null)
                return null;
            Transform cam = GameCamera.instance.transform;
            RaycastHit[] hits = Physics.SphereCastAll(cam.position, 0.6f, cam.forward, range + 8f, ~0, QueryTriggerInteraction.Ignore);
            Player best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                Player p = hits[i].collider == null ? null : hits[i].collider.GetComponentInParent<Player>();
                if (p == null || p == caster || p.IsDead())
                    continue;
                float d = Vector3.Distance(caster.transform.position, p.transform.position);
                if (d <= range && hits[i].distance < bestDist)
                {
                    best = p;
                    bestDist = hits[i].distance;
                }
            }
            return best;
        }

        private IEnumerator IhHealOverTime(Player ally, float percentPerSecond, float seconds)
        {
            float end = Time.time + Mathf.Max(0f, seconds);
            while (Time.time < end && ally != null && !ally.IsDead())
            {
                yield return new WaitForSeconds(1f);
                if (ally != null && !ally.IsDead())
                    Heal(ally, ally.GetMaxHealth() * Mathf.Max(0f, percentPerSecond) / 100f);
            }
        }

        private void OnBucklerParry(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead() || !IhIsPriest(player))
                return;

            // Every parry: Hyper Armor + the next damaging skill is empowered.
            DragonCombat.ApplyTimedBuff(player, "Priest.BucklerParry", Mathf.Max(0.1f, _parryHyperArmorSeconds.Value), 0f, 0f, 0f, 0f, 0f, 0f, true);
            _parryEmpowerPending = true;

            const string id = "Priest.HolyShockwave";
            if (GetCooldownRemaining(id) > 0f)
            {
                ShowMessage("Holy Parry - next skill empowered");
                return;
            }
            SetPriestCooldownNow(id, _shockwaveCooldown.Value);
            ShowMessage("Holy Shockwave - next skill empowered");

            Vector3 center = player.transform.position;
            float radius = Mathf.Max(1f, _shockwaveRadius.Value);
            if (_enableVfx.Value)
                StartCoroutine(AnimateRing(center + Vector3.up * 0.08f, 0.6f, radius, 0.45f, new Color(1f, 0.95f, 0.70f, 0.95f), 0.12f));
            List<Character> enemies = GetSphereTargets(player, center, radius);
            for (int i = 0; i < enemies.Count; i++)
            {
                Character enemy = enemies[i];
                // Direct Spirit damage, no DoT; Stuns Small and Big (never Bosses).
                DealDamageScaled(player, enemy, _shockwaveDamage, 1f, 10f, false);
                if (!enemy.IsBoss())
                    DragonCombat.Stun(enemy, center);
            }
        }

        private int IhReadInt(Player player, string key, int fallback)
        {
            int value;
            return int.TryParse(ReadPlayerData(player, key), out value) ? value : fallback;
        }

        private void IhWrite(Player player, string key, string value)
        {
            IDictionary data = GetCustomData(player);
            if (data == null)
                return;
            if (string.IsNullOrEmpty(value))
            {
                if (data.Contains(key))
                    data.Remove(key);
            }
            else
            {
                data[key] = value;
            }
        }

        private int IhGetLevel(Player player)
        {
            return Mathf.Clamp(IhReadInt(player, IhLevelKey, 1), 1, IhMaxLevel);
        }

        private bool IhIsPaladin(Player player)
        {
            return GetClass(player) == "Cleric" && GetAdvancement(player) == "Paladin";
        }

        private static bool IhContains(string[] list, string id)
        {
            return Array.IndexOf(list, id) >= 0;
        }

        private int IhMaxTier(string id)
        {
            if (IhIsAnyClassSkill(id)) return 7;
            if (IhIsUltimate(id)) return 3;
            IhKit k = IhKitOf(id);
            if (k != null && IhContains(k.Adv, id)) return 5;
            return 0;
        }

        private Dictionary<string, int> IhTiers(Player player)
        {
            string raw = player == null ? "" : ReadPlayerData(player, IhTiersKey);
            if (!string.Equals(raw, _ihTierCacheRaw, StringComparison.Ordinal))
            {
                _ihTierCacheRaw = raw;
                _ihTierCache.Clear();
                string[] parts = raw.Split(';');
                for (int i = 0; i < parts.Length; i++)
                {
                    string[] pair = parts[i].Split('=');
                    int tier;
                    if (pair.Length == 2 && int.TryParse(pair[1], out tier) && tier > 0)
                        _ihTierCache[pair[0].Trim()] = tier;
                }
            }
            return _ihTierCache;
        }

        private int IhGetTier(Player player, string id)
        {
            if (player == null)
                return 0;
            if (IhIsUltimate(id))
                return IhUltimateTier(player);
            int tier;
            return IhTiers(player).TryGetValue(id, out tier) ? Mathf.Clamp(tier, 0, IhMaxTier(id)) : 0;
        }

        private void IhSetTiers(Player player, Dictionary<string, int> tiers)
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> kvp in tiers)
            {
                if (kvp.Value > 0)
                    parts.Add(kvp.Key + "=" + kvp.Value.ToString());
            }
            parts.Sort(StringComparer.Ordinal);
            IhWrite(player, IhTiersKey, string.Join(";", parts.ToArray()));
            _ihTierCacheRaw = null;
            _hotbarLayoutOwnerKey = "";
        }

        private void IhClearTiers(Player player, string[] ids)
        {
            Dictionary<string, int> tiers = new Dictionary<string, int>(IhTiers(player));
            for (int i = 0; i < ids.Length; i++)
                tiers.Remove(ids[i]);
            IhSetTiers(player, tiers);
        }

        private int IhUltimateTier(Player player)
        {
            int level = IhGetLevel(player);
            return level >= 48 ? 3 : (level >= 44 ? 2 : (level >= 40 ? 1 : 0));
        }

        private int IhSpent(Player player, string[] ids)
        {
            int total = 0;
            for (int i = 0; i < ids.Length; i++)
                total += IhGetTier(player, ids[i]);
            return total;
        }

        // Class Tier Points: +2 every 2 levels from Lv4 to Lv16 (14).
        private int IhClassPointsEarned(Player player)
        {
            int level = IhGetLevel(player);
            return Mathf.Clamp(2 * (level / 2) - 2, 0, 14) + IhReadInt(player, IhBonusClassKey, 0);
        }

        // Advancement Tier Points: +1 every 2 levels from Lv18 to Lv56 (20), only after Advancement.
        private int IhAdvPointsEarned(Player player)
        {
            if (string.IsNullOrEmpty(GetAdvancement(player)))
                return 0;
            int level = IhGetLevel(player);
            return Mathf.Clamp((level - 16) / 2, 0, 20) + IhReadInt(player, IhBonusAdvKey, 0);
        }

        // Lv24 / Lv32 skills also need 80% of the points earned by that level spent (normal rounding).
        private static int IhGateSpend(int gateLevel)
        {
            int earned = Mathf.Clamp((gateLevel - 16) / 2, 0, 20);
            return Mathf.FloorToInt(earned * 0.8f + 0.5f);
        }

        // AC slot 3 (the Lv24 skill) and slots 4-5 (the Lv32 pair), Ultimate Lv36, the rest Lv16.
        private static int IhGateLevel(string id)
        {
            if (IhIsUltimate(id)) return 36;
            IhKit k = IhKitOf(id);
            int index = k == null ? -1 : Array.IndexOf(k.Adv, id);
            if (index == 2) return 24;
            if (index >= 3) return 32;
            return 16;
        }

        private bool IhIsUnlocked(Player player, string id, out string reason)
        {
            reason = "";
            if (player == null) return false;
            string cls = GetClass(player);
            string skillClass = IhClassOfSkill(id);
            IhKit kit = IhKitOf(id);
            if (skillClass.Length == 0 && kit == null)
            {
                reason = "Unknown skill";
                return false;
            }
            string needClass = skillClass.Length > 0 ? skillClass : kit.Class;
            if (cls != needClass)
            {
                reason = "Choose the " + needClass + " Class at the Altar";
                return false;
            }
            if (skillClass.Length > 0) return true;
            string branch = kit.Ac;
            if (GetAdvancement(player) != branch)
            {
                reason = "Advance to " + branch + " at Lv 16";
                return false;
            }
            if (_ihUnlockAll != null && _ihUnlockAll.Value) return true;
            int gate = IhGateLevel(id);
            if (IhGetLevel(player) < gate)
            {
                reason = "Unlocks at Lv " + gate.ToString();
                return false;
            }
            if (gate <= 16 || IhIsUltimate(id)) return true;
            int need = IhGateSpend(gate);
            if (IhSpent(player, IhBranchSkills(player)) < need)
            {
                reason = "Spend " + need.ToString() + " " + branch + " Tier Points first";
                return false;
            }
            return true;
        }

        private bool IhIsUnlocked(Player player, string id)
        {
            string reason;
            return IhIsUnlocked(player, id, out reason);
        }

        private HashSet<string> IhAscendedSet(Player player)
        {
            string raw = player == null ? "" : ReadPlayerData(player, IhAscendedKey);
            if (!string.Equals(raw, _ihAscCacheRaw, StringComparison.Ordinal))
            {
                _ihAscCacheRaw = raw;
                _ihAscCache.Clear();
                string[] parts = raw.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i].Trim();
                    if (part.Length > 0)
                        _ihAscCache.Add(part);
                }
            }
            return _ihAscCache;
        }

        private void IhSetAscended(Player player, HashSet<string> set)
        {
            List<string> list = new List<string>(set);
            list.Sort(StringComparer.Ordinal);
            IhWrite(player, IhAscendedKey, string.Join(",", list.ToArray()));
            _ihAscCacheRaw = null;
        }

        // The Advancement's Class skill (Righteous Strike for Paladin) is Ascended by Advancing.
        private bool IhIsAscended(Player player, string id)
        {
            if (player == null)
                return false;
            // v0.22.0: the kit's Ascended Class skill Ascends when you Advance (Righteous Strike,
            // Holy Wave, Impact Wave, Heavy Slash, Glacial Descent, Stonefang Eruption).
            IhKit kit = IhPlayerKit(player);
            if (kit == null)
                return false;
            if (id == kit.AscendedClass)
                return true;
            return (IhContains(kit.Adv, id) || id == kit.Ultimate) && IhAscendedSet(player).Contains(id);
        }

        private bool IhTryAscend(Player player, string id, out string message)
        {
            return IhCheckAscend(player, id, true, out message);
        }

        // apply = false only checks the requirements (used by the tree's ASCEND button).
        private bool IhCheckAscend(Player player, string id, bool apply, out string message)
        {
            message = "";
            int level = IhGetLevel(player);
            HashSet<string> set = new HashSet<string>(IhAscendedSet(player));
            IhKit kit = IhPlayerKit(player);
            if (kit == null)
            {
                message = "Only an Advanced character can Ascend skills.";
                return false;
            }
            if (id == kit.AscendedClass)
            {
                message = IhSkillName(id) + " is already Ascended (it Ascends when you Advance).";
                return false;
            }
            string[] group;
            int needLevel;
            int needTier;
            string[] signatures = kit.Signatures;
            string[] normals = kit.Normals;
            string ultimate = kit.Ultimate;
            if (IhContains(signatures, id)) { group = signatures; needLevel = 32; needTier = 5; }
            else if (IhContains(normals, id)) { group = normals; needLevel = 42; needTier = 5; }
            // Spellcaster's Void Step: its own Lv 42 Ascension, separate from the normal-skill one.
            else if (id == kit.SpecialAscension) { group = new string[] { id }; needLevel = 42; needTier = 5; }
            else if (id == ultimate) { group = new string[] { ultimate }; needLevel = 50; needTier = 3; }
            else
            {
                message = "That skill cannot Ascend.";
                return false;
            }
            for (int i = 0; i < group.Length; i++)
            {
                if (set.Contains(group[i]))
                {
                    message = IhSkillName(group[i]) + " already holds this Ascension.";
                    return false;
                }
            }
            if (level < needLevel)
            {
                message = "Requires Lv " + needLevel.ToString() + ".";
                return false;
            }
            if (IhGetTier(player, id) < needTier)
            {
                message = "Requires " + IhSkillName(id) + " at Tier " + needTier.ToString() + ".";
                return false;
            }
            if (set.Contains(id))
            {
                message = IhSkillName(id) + " is already Ascended.";
                return false;
            }
            if (!apply)
                return true;
            set.Add(id);
            IhSetAscended(player, set);
            _hotbarLayoutOwnerKey = "";
            message = IhSkillName(id) + " has Ascended!";
            return true;
        }

        // Level changes (commands) can void Advancement / Ascensions and over-spent pools.
        private void IhApplyLevel(Player player, int level, List<string> notes)
        {
            level = Mathf.Clamp(level, 1, IhMaxLevel);
            IhWrite(player, IhLevelKey, level.ToString());
            HashSet<string> set = new HashSet<string>(IhAscendedSet(player));
            IhKit levelKit = IhPlayerKit(player);
            if (level < 16 && !string.IsNullOrEmpty(GetAdvancement(player)))
            {
                for (int k = 0; k < IhKits.Length; k++)
                    if (IhKits[k].Class == GetClass(player)) IhClearTiers(player, IhKits[k].Adv);
                IhWrite(player, AdvancementDataKey, "");
                set.Clear();
                notes.Add("Below Lv 16: Advancement annulled.");
            }
            if (levelKit != null)
            {
                if (level < 50 && set.Remove(levelKit.Ultimate)) notes.Add("Below Lv 50: Ultimate Ascension annulled.");
                for (int i = 2; i < levelKit.Adv.Length; i++)
                    if (level < 42 && set.Remove(levelKit.Adv[i])) notes.Add("Below Lv 42: " + IhSkillName(levelKit.Adv[i]) + " Ascension annulled.");
                for (int i = 0; i < levelKit.Signatures.Length; i++)
                    if (level < 32 && set.Remove(levelKit.Signatures[i])) notes.Add("Below Lv 32: " + IhSkillName(levelKit.Signatures[i]) + " Ascension annulled.");
            }
            IhSetAscended(player, set);
            IhEnforcePools(player, notes);
            _hotbarLayoutOwnerKey = "";
        }

        private void IhEnforcePools(Player player, List<string> notes)
        {
            string[] classSkills = IhPlayerClassSkills(player);
            if (IhSpent(player, classSkills) > IhClassPointsEarned(player))
            {
                IhClearTiers(player, classSkills);
                notes.Add("Class Tiers reset (more points spent than earned).");
            }
            if (IhSpent(player, IhBranchSkills(player)) > IhAdvPointsEarned(player))
            {
                IhClearTiers(player, IhBranchSkills(player));
                HashSet<string> set = new HashSet<string>(IhAscendedSet(player));
                string[] branchSkills = IhBranchSkills(player);
                for (int i = 0; i < branchSkills.Length; i++)
                    set.Remove(branchSkills[i]);
                IhSetAscended(player, set);
                notes.Add(GetAdvancement(player) + " Tiers reset (more points spent than earned).");
            }
        }

        private bool IhCanCast(Player player, string id)
        {
            string reason;
            if (IhIsUnlocked(player, id, out reason))
                return true;
            ShowMessage(IhSkillName(id) + " is locked: " + reason);
            return false;
        }

        private static string IhSkillName(string id)
        {
            switch (id)
            {
                case "lightning_zap": return "Lightning Zap";
                case "righteous_strike": return "Righteous Strike";
                case "holy_wave": return "Holy Wave";
                case "goddess_relic": return "Goddess Relic";
                case "judgement_hammer": return "Judgement Hammer";
                case "shield_charge": return "Shield Charge";
                case "fallen_angel": return "Angel Comet";
                case "ray_of_hope": return "Ray of Hope";
                case "electric_smite": return "Electric Smite";
                case "heavens_light": return "Heaven's Light";
                case "lightning_relic": return "Lightning Relic";
                case "holy_relic": return "Holy Relic";
                case "divine_intervention": return "Divine Intervention";
                case "grand_cross": return "Grand Cross";
                case "heavens_judgement": return "Heaven's Judgement";
                case "lightning_tempest": return "Lightning Tempest";
                case "grand_sigil": return "Heaven's Crucible";
                case "heavy_slash": return "Heavy Slash";
                case "impact_wave": return "Impact Wave";
                case "impact_punch": return "Impact Punch";
                case "moonlight_splitter": return "Moonlight Splitter";
                case "crescent_cleave": return "Crescent Cleave";
                case "blade_storm": return "Blade Storm";
                case "frenzied_charge": return "Frenzied Charge";
                case "eclipse": return "Eclipse";
                case "halfmoon_slash": return "Halfmoon Slash";
                case "knights_guidance": return "Knight's Guidance";
                case "stomp": return "Stomp";
                case "circle_swing": return "Circle Swing";
                case "bonecrusher": return "Bonecrusher";
                case "seismic_guillotine": return "Seismic Guillotine";
                case "punishing_bomb": return "Punishing Bomb";
                case "whirlwind": return "Whirlwind";
                case "battlecry": return "Battlecry";
                case "flame_burst": return "Flame Burst";
                case "glacial_descent": return "Glacial Descent";
                case "stonefang_eruption": return "Stonefang Eruption";
                case "meteor_fall": return "Meteor Fall";
                case "gravity_dominion": return "Gravity Dominion";
                case "astral_railcannon": return "Astral Railcannon";
                case "astral_greatblade": return "Astral Greatblade";
                case "frost_nova": return "Frost Nova";
                case "elemental_cataclysm": return "Elemental Cataclysm";
                case "clockwork": return "Clockwork";
                case "arcane_phalanx": return "Arcane Phalanx";
                case "afterimage_arsenal": return "Afterimage Arsenal";
                case "void_step": return "Void Step";
                case "rift_echo": return "Rift Echo";
                case "gravity_blast": return "Gravity Blast";
                case "arcane_rupture": return "Arcane Rupture";
                case "rift_walker": return "Rift Walker";
            }
            return id;
        }

        private static string IhNormalizeSkill(string text)
        {
            string s = (text ?? "").Trim().ToLowerInvariant().Replace("'", "").Replace("-", " ");
            while (s.Contains("  "))
                s = s.Replace("  ", " ");
            return s.Replace(" ", "_");
        }

        // ------------------------------------------------------------------ /ih command
        private void IhEnsureCommands()
        {
            if (_ihCommandRegistered)
                return;
            _ihCommandRegistered = true;
            try
            {
                new Terminal.ConsoleCommand("ih", "Immortal Heroes: /ih [player] level|xp|classtierpoints|actierpoints|resetskill|class|advance|ascend|info",
                    new Terminal.ConsoleEvent(IhOnCommand));
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Immortal Heroes: could not register /ih: " + ex.Message);
            }
        }

        private static List<string> IhTokenize(string line)
        {
            List<string> tokens = new List<string>();
            System.Text.StringBuilder current = new System.Text.StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    quoted = !quoted;
                    continue;
                }
                if (char.IsWhiteSpace(c) && !quoted)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Length = 0;
                    }
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0)
                tokens.Add(current.ToString());
            return tokens;
        }

        private static bool IhIsAdminOrSolo()
        {
            try
            {
                if (ZNet.instance == null || ZNet.instance.IsServer())
                    return true;
                MethodInfo method = typeof(ZNet).GetMethod("LocalPlayerIsAdminOrHost", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                    return true;
                return (bool)method.Invoke(ZNet.instance, null);
            }
            catch
            {
                return true;
            }
        }

        private void IhOnCommand(Terminal.ConsoleEventArgs args)
        {
            Terminal output = args.Context;
            List<string> notes = new List<string>();
            try
            {
                IhRunCommand(args.FullLine ?? "", notes);
            }
            catch (Exception ex)
            {
                notes.Add("Immortal Heroes command failed: " + ex.Message);
            }
            for (int i = 0; i < notes.Count; i++)
            {
                if (output != null)
                    output.AddString(notes[i]);
                else
                    Logger.LogInfo(notes[i]);
            }
        }

        private void IhRunCommand(string line, List<string> notes)
        {
            Player player = Player.m_localPlayer;
            List<string> t = IhTokenize(line);
            if (t.Count > 0 && t[0].TrimStart('/').ToLowerInvariant() == "ih")
                t.RemoveAt(0);
            if (player == null)
            {
                notes.Add("Immortal Heroes: no local player.");
                return;
            }
            if (!IhIsAdminOrSolo())
            {
                notes.Add("Immortal Heroes: /ih is for admins on servers.");
                return;
            }

            string[] known = { "level", "xp", "classtierpoints", "actierpoints", "resetskill", "class", "advance", "ascend", "info", "help" };
            if (t.Count > 0 && Array.IndexOf(known, t[0].ToLowerInvariant()) < 0)
            {
                string name = t[0];
                if (!string.Equals(name, player.GetPlayerName(), StringComparison.OrdinalIgnoreCase))
                {
                    notes.Add("Immortal Heroes: other players come with server sync. Use /ih <command> on yourself for now.");
                    return;
                }
                t.RemoveAt(0);
            }
            if (t.Count == 0 || t[0].ToLowerInvariant() == "help")
            {
                notes.Add("/ih level N (+N / -N) | classtierpoints N | actierpoints N | resetskill all|<skill>");
                notes.Add("/ih class <Warrior|Cleric|Sorcerer> | advance <Paladin|Priest> | ascend <skill> | info");
                notes.Add("Skill names with spaces go in quotes: /ih ascend \"Goddess Relic\"");
                return;
            }

            string cmd = t[0].ToLowerInvariant();
            string arg = t.Count > 1 ? t[1] : "";
            int number;
            bool hasNumber = int.TryParse(arg.TrimStart('+'), out number);

            if (cmd == "level")
            {
                if (!hasNumber) { notes.Add("Usage: /ih level N   (or +N / -N)"); return; }
                int level = arg.StartsWith("+") || arg.StartsWith("-") ? IhGetLevel(player) + number : number;
                IhApplyLevel(player, level, notes);
                notes.Add("Level set to " + IhGetLevel(player).ToString() + ".");
            }
            else if (cmd == "xp")
            {
                notes.Add("XP arrives with the Immortal Leveling mod. Use /ih level for now.");
            }
            else if (cmd == "classtierpoints" || cmd == "actierpoints")
            {
                if (!hasNumber) { notes.Add("Usage: /ih " + cmd + " N   (adds bonus points, negative removes)"); return; }
                string key = cmd == "classtierpoints" ? IhBonusClassKey : IhBonusAdvKey;
                IhWrite(player, key, (IhReadInt(player, key, 0) + number).ToString());
                IhEnforcePools(player, notes);
                notes.Add("Bonus " + (cmd == "classtierpoints" ? "Class" : "Advancement") + " Tier Points: " + IhReadInt(player, key, 0).ToString() + ".");
            }
            else if (cmd == "resetskill")
            {
                string id = IhNormalizeSkill(arg);
                if (id == "all" || id.Length == 0)
                {
                    IhSetTiers(player, new Dictionary<string, int>());
                    IhSetAscended(player, new HashSet<string>());
                    notes.Add("All Tiers and Ascensions reset.");
                }
                else if (IhMaxTier(id) > 0 && !IhIsUltimate(id))
                {
                    IhClearTiers(player, new string[] { id });
                    HashSet<string> set = new HashSet<string>(IhAscendedSet(player));
                    set.Remove(id);
                    IhSetAscended(player, set);
                    notes.Add(IhSkillName(id) + " reset to Tier 0.");
                }
                else
                {
                    notes.Add("Unknown skill: " + arg);
                }
            }
            else if (cmd == "class")
            {
                string mc = arg.Length == 0 ? "" : char.ToUpperInvariant(arg[0]) + arg.Substring(1).ToLowerInvariant();
                if (mc != "Warrior" && mc != "Cleric" && mc != "Sorcerer") { notes.Add("Usage: /ih class Warrior|Cleric|Sorcerer"); return; }
                IhWrite(player, ClassDataKey, mc);
                IhWrite(player, AdvancementDataKey, "");
                IhSetTiers(player, new Dictionary<string, int>());
                IhSetAscended(player, new HashSet<string>());
                notes.Add("Class set to " + mc + ". Advancement, Tiers and Ascensions cleared.");
            }
            else if (cmd == "advance")
            {
                string ac = IhNormalizeSkill(arg);
                if (ac != "paladin" && ac != "priest") { notes.Add("Usage: /ih advance Paladin|Priest"); return; }
                IhWrite(player, ClassDataKey, "Cleric");
                IhWrite(player, AdvancementDataKey, ac == "priest" ? "Priest" : "Paladin");
                if (IhGetLevel(player) < 16)
                    IhWrite(player, IhLevelKey, "16");
                _hotbarLayoutOwnerKey = "";
                notes.Add("Advanced to " + GetAdvancement(player) + " (Lv " + IhGetLevel(player).ToString() + ").");
            }
            else if (cmd == "ascend")
            {
                string message;
                IhTryAscend(player, IhNormalizeSkill(arg), out message);
                notes.Add(message);
            }
            else if (cmd == "info")
            {
                notes.Add("Lv " + IhGetLevel(player).ToString() + "  " + (GetClass(player) == "" ? "No Class" : GetClass(player)) +
                          (GetAdvancement(player) == "" ? "" : " > " + GetAdvancement(player)));
                notes.Add("Class Tier Points " + (IhClassPointsEarned(player) - IhSpent(player, IhPlayerClassSkills(player))).ToString() + " left of " + IhClassPointsEarned(player).ToString() +
                          "  |  Advancement " + (IhAdvPointsEarned(player) - IhSpent(player, IhBranchSkills(player))).ToString() + " left of " + IhAdvPointsEarned(player).ToString());
                List<string> tiers = new List<string>();
                List<string> all = new List<string>(IhPlayerClassSkills(player));
                all.AddRange(IhBranchSkills(player));
                IhKit infoKit = IhPlayerKit(player);
                if (infoKit != null) all.Add(infoKit.Ultimate);
                for (int i = 0; i < all.Count; i++)
                    tiers.Add(IhSkillName(all[i]) + " " + IhGetTier(player, all[i]).ToString() + "/" + IhMaxTier(all[i]).ToString() + (IhIsAscended(player, all[i]) ? "*" : ""));
                notes.Add(string.Join(", ", tiers.ToArray()) + "   (* Ascended)");
            }
        }

        // ------------------------------------------------------------------ v0.20.4 F8 Progression tab bridge
        // DevTools calls these by reflection (no compile link). Same rules as the /ih command.

        // { level, class earned, class spent, adv earned, adv spent, bonus class, bonus adv }
        public int[] DevPointSummary()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return null;
            return new int[] {
                IhGetLevel(player),
                IhClassPointsEarned(player), IhSpent(player, IhPlayerClassSkills(player)),
                IhAdvPointsEarned(player), IhSpent(player, IhBranchSkills(player)),
                IhReadInt(player, IhBonusClassKey, 0), IhReadInt(player, IhBonusAdvKey, 0) };
        }

        public string DevClassName()
        {
            Player player = Player.m_localPlayer;
            return player == null ? "" : GetClass(player);
        }

        public string DevCharacterName()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return "";
            string mc = GetClass(player), ac = GetAdvancement(player);
            return (mc == "" ? "No Class" : mc) + (ac == "" ? "" : "  >  " + ac);
        }

        public string DevSetBonusPoints(bool advancement, int value)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return "No character loaded.";
            List<string> notes = new List<string>();
            IhWrite(player, advancement ? IhBonusAdvKey : IhBonusClassKey, Mathf.Max(0, value).ToString());
            IhEnforcePools(player, notes);
            return notes.Count > 0 ? string.Join(" ", notes.ToArray()) : (advancement ? "Advancement" : "Class") + " bonus Tier Points set to " + Mathf.Max(0, value).ToString() + ".";
        }

        public string DevCommand(string line)
        {
            List<string> notes = new List<string>();
            try
            {
                IhRunCommand(line ?? "", notes);
            }
            catch (Exception ex)
            {
                notes.Add("Command failed: " + ex.Message);
            }
            return string.Join(" ", notes.ToArray());
        }

        // ------------------------------------------------------------------ tooltips
        private static string IhNum(float value)
        {
            return value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }

        private string IhHex(float r, float g, float b)
        {
            Color c = UiColor(new Color(r, g, b, 1f));
            return "#" + ((int)(c.r * 255f)).ToString("X2") + ((int)(c.g * 255f)).ToString("X2") + ((int)(c.b * 255f)).ToString("X2");
        }

        // Code-drawn colors get the same Linear color-space compensation as the artwork.
        private Color UiColor(Color c)
        {
            if (_uiColorSpaceCorrection == null || !_uiColorSpaceCorrection.Value || QualitySettings.activeColorSpace != ColorSpace.Linear)
                return c;
            return new Color(Mathf.GammaToLinearSpace(c.r), Mathf.GammaToLinearSpace(c.g), Mathf.GammaToLinearSpace(c.b), c.a);
        }

        private string IhLine(string label, string value)
        {
            string colored = IhColorize(value);
            if (label == "Tier")
            {
                // Both confirmed and pending tier fractions stay white; the bonus is Cyan.
                colored = System.Text.RegularExpressions.Regex.Replace(colored,
                    @"<color=[^>]+>(\d+)</color>/<color=[^>]+>(\d+)</color>", "$1/$2");
            }
            return "<color=" + IhHex(1f, 0.84f, 0.30f) + ">" + label + "</color>" + IhWhite(" - " + colored) + "\n";
        }

        private string IhWhite(string text)
        {
            return "<color=" + IhHex(0.95f, 0.94f, 0.91f) + ">" + text + "</color>";
        }

        private static string IhDamage(float blunt, float slash, float pierce, float fire, float frost, float lightning, float poison, float spirit, float power)
        {
            List<string> parts = new List<string>();
            float[] values = { blunt, slash, pierce, fire, frost, lightning, poison, spirit };
            string[] names = { "Blunt", "Slash", "Pierce", "Fire", "Frost", "Lightning", "Poison", "Spirit" };
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > 0.001f)
                    parts.Add(IhNum(values[i] * power) + " " + names[i]);
            }
            return parts.Count == 0 ? "-" : string.Join(", ", parts.ToArray());
        }

        private static string IhDamage(DamageConfig cfg, float power)
        {
            if (cfg == null)
                return "-";
            return IhDamage(cfg.Blunt.Value, cfg.Slash.Value, cfg.Pierce.Value, cfg.Fire.Value, cfg.Frost.Value, cfg.Lightning.Value, cfg.Poison.Value, cfg.Spirit.Value, power);
        }

        // Reads another Immortal Heroes module's config value (Cleric skills live in the Skills module).
        private static float IhCfg(string guid, string section, string key, float fallback)
        {
            try
            {
                BepInEx.PluginInfo info;
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(guid, out info) || info == null || info.Instance == null)
                    return fallback;
                IDictionary<ConfigDefinition, ConfigEntryBase> entries = info.Instance.Config as IDictionary<ConfigDefinition, ConfigEntryBase>;
                ConfigEntryBase entry;
                if (entries == null || !entries.TryGetValue(new ConfigDefinition(section, key), out entry) || entry == null)
                    return fallback;
                return Convert.ToSingle(entry.BoxedValue, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static string IhSkillsDamage(string section, float power)
        {
            const string g = "albedo.customclasses.skills";
            return IhDamage(IhCfg(g, section, "Blunt", 0f), IhCfg(g, section, "Slash", 0f), IhCfg(g, section, "Pierce", 0f), IhCfg(g, section, "Fire", 0f),
                IhCfg(g, section, "Frost", 0f), IhCfg(g, section, "Lightning", 0f), IhCfg(g, section, "Poison", 0f), IhCfg(g, section, "Spirit", 0f), power);
        }

        private static float IhRuntime(string key, float fallback)
        {
            return IhCfg("albedo.customclasses.combatruntime", "Debuffs", key, fallback);
        }


        private string IhSkillTitle(string id, ReferenceNodeUi node, bool ascended)
        {
            if (ascended)
                return "ASCENDED - " + IhSkillName(id).ToUpperInvariant();
            return node != null ? node.TooltipTitle : IhSkillName(id).ToUpperInvariant();
        }

        // v0.18.2: Valheim-style skill sheet. Description first, then one "Subject - value" line per fact.
        // White = information and Tier fractions, Yellow = subject, Cyan = values with units.
        // v0.20.7: tooltip values take the skill's own frame colour (Cyan, Navy, Green, Magenta,
        // Maroon, Red, Gold) instead of one fixed Cyan.
        private Color _ihTooltipAccent = new Color(0.42f, 0.88f, 1f, 1f);

        private static Color IhAccentFor(string frameColor)
        {
            switch (frameColor)
            {
                case "navy": return new Color(0.52f, 0.68f, 1f, 1f);
                case "green": return new Color(0.46f, 0.95f, 0.56f, 1f);
                case "magenta": return new Color(1f, 0.46f, 0.92f, 1f);
                case "maroon": return new Color(1f, 0.58f, 0.68f, 1f);
                case "red": return new Color(1f, 0.36f, 0.30f, 1f);
                case "gold": return new Color(1f, 0.95f, 0.66f, 1f);
            }
            return new Color(0.42f, 0.88f, 1f, 1f);
        }

        private string IhBuildTooltip(Player player, ReferenceNodeUi node, out string title)
        {
            Color saved = _ihTooltipAccent;
            _ihTooltipAccent = IhAccentFor(IhSkillFrameColor(node));
            try
            {
                return IhBuildTooltipBody(player, node, out title);
            }
            finally
            {
                _ihTooltipAccent = saved;
            }
        }

        private string IhBuildTooltipBody(Player player, ReferenceNodeUi node, out string title)
        {
            string id = node.Id;
            if (IhPriestSkill(id)) return IhBuildPriestTooltip(player, node, out title);
            bool ascended = IsAscendedSkill(id);
            title = IhSkillTitle(id, node, ascended);
            int tier = IhGetTier(player, id);
            int pending = GetPrototypePending(id);
            int maxTier = IhMaxTier(id);
            float pct = _ihTierPowerPercent == null ? 10f : _ihTierPowerPercent.Value;
            float power = 1f + pct / 100f * tier;
            const string sk = "albedo.customclasses.skills";
            System.Text.StringBuilder b = new System.Text.StringBuilder();

            b.Append(IhWhite(IhLore(id, ascended)) + "\n\n");

            if (maxTier > 0)
            {
                string tierText = tier.ToString() + "/" + maxTier.ToString();
                if (pending > 0)
                    tierText += "  →  " + (tier + pending).ToString() + "/" + maxTier.ToString() + " (pending)";
                tierText += "   +" + IhNum(pct * (tier + pending)) + "% " + IhTierBonusLabel(id);
                b.Append(IhLine("Tier", tierText));
                if (id == IhUltimate)
                    b.Append(IhLine("Tier Up", "automatic at Lv 40, 44, 48"));
            }
            string reason;
            if (!IhIsUnlocked(player, id, out reason))
                b.Append(IhLine("Requires", reason));

            switch (id)
            {
                case "lightning_zap":
                    b.Append(IhLine("Damage", IhSkillsDamage("Cleric.Lightning Zap.Damage", power)));
                    b.Append(IhLine("Area", "Cone, " + IhNum(IhCfg(sk, "Cleric.Lightning Zap", "Range", 10f)) + "m, " + IhNum(IhCfg(sk, "Cleric.Lightning Zap", "ConeDegrees", 70f)) + "°"));
                    b.Append(IhLine("Inflicts", "Zap, explodes after " + IhNum(IhRuntime("ZapDelay_v0212", 2f)) + "s"));
                    b.Append(IhLine("Zap Damage", IhNum(IhRuntime("ZapLightningDamage", 25f)) + " Lightning, " + IhNum(IhRuntime("ZapRadius", 1f)) + "m"));
                    IhCosts(b, IhCfg(sk, "Cleric.Lightning Zap", "StaminaCost", 18f), "Instant", IhCfg(sk, "Cleric.Lightning Zap", "Cooldown", 8f));
                    break;
                case "righteous_strike":
                    if (ascended)
                    {
                        b.Append(IhLine("Damage", IhDamage(_rsAscDamage, power)));
                        b.Append(IhLine("Radius", IhNum(_rsAscRadius.Value) + "m"));
                        b.Append(IhLine("Range", IhNum(_rsAscRange.Value) + "m"));
                        b.Append(IhLine("Inflicts", "Expose, " + IhNum(_rsAscExpose.Value) + "s"));
                        b.Append(IhLine("Applies", "Judgement Mark"));
                        b.Append(IhLine("Trails", "12 Lightning Trails, " + IhNum(_rsAscTrailRange.Value) + "m"));
                        b.Append(IhLine("Trail Damage", IhDamage(_rsAscTrailDamage, power) + " every " + IhNum(_rsAscTrailTick.Value) + "s"));
                        b.Append(IhLine("Trail Burn", "Spirit Burn " + IhNum(_rsAscSpiritDot.Value) + "/s, " + IhNum(_rsAscSpiritDuration.Value) + "s"));
                        b.Append(IhLine("Detonation", "second strike, " + IhNum(_rsAscFollowRadius.Value) + "m, " + IhNum(_rsAscFollowMultiplier.Value * 100f) + "% Damage"));
                        IhCosts(b, _rsAscStamina.Value, IhNum(_rsAscWindup.Value) + "s", _rsAscCooldown.Value);
                    }
                    else
                    {
                        b.Append(IhLine("Damage", IhSkillsDamage("Cleric.Righteous Strike.Damage", power)));
                        b.Append(IhLine("Radius", IhNum(IhCfg(sk, "Cleric.Righteous Strike", "Radius", 5f)) + "m"));
                        b.Append(IhLine("Range", IhNum(IhCfg(sk, "Cleric.Righteous Strike", "Range", 50f)) + "m"));
                        b.Append(IhLine("Inflicts", "Expose, " + IhNum(IhCfg(sk, "Cleric.Righteous Strike", "ExposeDuration", 6f)) + "s"));
                        IhCosts(b, IhCfg(sk, "Cleric.Righteous Strike", "StaminaCost", 20f), IhNum(IhCfg(sk, "Cleric.Righteous Strike", "Windup", 0.7f)) + "s", IhCfg(sk, "Cleric.Righteous Strike", "Cooldown", 8f));
                    }
                    break;
                case "holy_wave":
                    if (ascended && IhIsPriest(player))
                        b.Append(IhLine("Ascended", IhPriestAscendedSummary("holy_wave")));
                    b.Append(IhLine("Healing", IhNum(IhCfg(sk, "Cleric.Holy Wave", "ImmediateHeal", 25f) * power) + " HP"));
                    b.Append(IhLine("Regeneration", IhNum(IhCfg(sk, "Cleric.Holy Wave", "HealPercentPerSecond", 5f) * power) + "% of Total HP per second"));
                    b.Append(IhLine("Duration", IhNum(IhCfg(sk, "Cleric.Holy Wave", "Duration", 6f)) + "s"));
                    b.Append(IhLine("Radius", IhNum(IhCfg(sk, "Cleric.Holy Wave", "Radius", 7f)) + "m"));
                    IhCosts(b, IhCfg(sk, "Cleric.Holy Wave", "StaminaCost", 25f), "Instant", IhCfg(sk, "Cleric.Holy Wave", "Cooldown", 8f));
                    break;
                case "goddess_relic":
                    if (ascended)
                    {
                        b.Append(IhLine("Damage", IhDamage(_goddessAscDamage, power)));
                        b.Append(IhLine("Radius", IhNum(_goddessAscRadius.Value) + "m"));
                        b.Append(IhLine("Range", IhNum(_goddessRange.Value) + "m"));
                        b.Append(IhLine("Applies", "Judgement Mark"));
                    }
                    else
                    {
                        b.Append(IhLine("Damage", IhDamage(_goddessDamage, power)));
                        b.Append(IhLine("Radius", IhNum(_goddessRadiusV17.Value) + "m"));
                        b.Append(IhLine("Range", IhNum(_goddessRange.Value) + "m"));
                        b.Append(IhLine("Inflicts", "Spirit Burn " + IhNum(_goddessSpiritDot.Value) + "/s, " + IhNum(_goddessSpiritDuration.Value) + "s"));
                    }
                    IhCosts(b, _goddessStamina.Value, IhNum(_goddessWindup.Value) + "s", _goddessCooldown.Value);
                    break;
                case "judgement_hammer":
                    b.Append(IhLine("Damage", IhDamage(_hammerDamage, power) + ", every " + IhNum(_hammerTick.Value) + "s"));
                    b.Append(IhLine("Growth", "+" + IhNum(_hammerDamagePerStep.Value * 100f) + "% Damage per " + IhNum(_hammerStepMeters.Value) + "m flown, up to " + IhNum(_hammerDamageCap.Value) + "x"));
                    b.Append(IhLine("Range", IhNum(_hammerRange.Value) + "m"));
                    b.Append(IhLine("Size", IhNum(_hammerStartHeight.Value) + "m tall, keeps growing in flight"));
                    b.Append(IhLine("Inflicts", "Cripple, " + IhNum(_hammerCrippleDuration.Value) + "s"));
                    if (ascended)
                    {
                        b.Append(IhLine("Applies", "Judgement Mark"));
                        b.Append(IhLine("Return", "flies back to you, hitting again"));
                        b.Append(IhLine("Catch", "-" + IhNum(_hammerCatchCooldownCut.Value) + "% Cooldown"));
                    }
                    IhCosts(b, _hammerStamina.Value, IhNum(_hammerWindup.Value) + "s", _hammerCooldown.Value);
                    break;
                case "shield_charge":
                    b.Append(IhLine("Damage", IhDamage(_shieldChargeDamage, power) + ", every " + IhNum(_shieldChargePersistentTick.Value) + "s"));
                    b.Append(IhLine("Distance", IhNum(ascended ? _chargeAscDistance.Value : _shieldChargeDistance.Value) + "m"));
                    b.Append(IhLine("Speed", IhNum(_shieldChargeSpeedMultiplier.Value) + "x"));
                    b.Append(IhLine("Hitbox", IhNum(ascended ? _chargeAscHitRadius.Value : _shieldChargeRadius.Value) + "m"));
                    if (ascended)
                    {
                        b.Append(IhLine("Shield Bash", "Left Click, " + IhNum(_chargeAscBashRadius.Value) + "m cone, " + IhNum(_chargeAscBashAngle.Value) + "°"));
                        b.Append(IhLine("Inflicts", "Stun (Big enemies too)"));
                        b.Append(IhLine("Gain", "Hyper Armor while charging"));
                    }
                    else
                    {
                        b.Append(IhLine("Shield Bash", "Left Click while charging"));
                    }
                    IhCosts(b, _shieldChargeStamina.Value, "Instant", _shieldChargeCooldown.Value);
                    break;
                case "fallen_angel":
                    b.Append(IhLine("Damage", IhDamage(_angelDamage, power)));
                    b.Append(IhLine("Leap", IhNum(_angelJumpHeight.Value) + "m"));
                    b.Append(IhLine("Radius", IhNum(_angelRadius.Value) + "m"));
                    b.Append(IhLine("Inflicts", "Stun, Broken Bones " + IhNum(_angelBrokenBones.Value) + "s"));
                    if (ascended)
                    {
                        b.Append(IhLine("Burning Ring", IhNum(_angelRingRadius.Value) + "m, " + IhNum(_angelRingDuration.Value) + "s"));
                        b.Append(IhLine("Ring Burn", "Fire Burn " + IhNum(_angelFireDot.Value) + "/s, Spirit Burn " + IhNum(_angelSpiritDot.Value) + "/s, " + IhNum(_angelBurnDuration.Value) + "s"));
                        b.Append(IhLine("Gain", "Hyper Armor, dive + " + IhNum(_angelHyperAfter.Value) + "s"));
                    }
                    IhCosts(b, _angelStamina.Value, IhNum(_angelWindupTotal.Value) + "s", _angelCooldown.Value);
                    break;
                case "ray_of_hope":
                    b.Append(IhLine("Healing", IhNum(_rayHealPercent.Value * power) + "% of Total HP"));
                    b.Append(IhLine("Radius", IhNum(_rayRadius.Value) + "m"));
                    if (ascended)
                    {
                        b.Append(IhLine("Gain", IhNum(_rayAscBarrier.Value) + " HP Barrier, " + IhNum(_rayAscBarrierDuration.Value) + "s"));
                        b.Append(IhLine("Cleanse", "Burn, Poison, Frost"));
                    }
                    b.Append(IhLine("Buff", IhNum(_rayDamageBuff.Value) + "% Attack Damage, " + IhNum(_rayBuffDuration.Value) + "s"));
                    b.Append(IhLine("Inflicts", "Spirit Burn, " + IhNum(_raySpiritBurnDuration.Value) + "s"));
                    IhCosts(b, _rayStamina.Value, "Instant", _rayCooldown.Value);
                    break;
                case "electric_smite":
                    b.Append(IhLine("Damage", IhDamage(_divineDamage, power)));
                    b.Append(IhLine("Radius", IhNum(_divineRadius.Value) + "m"));
                    b.Append(IhLine("Inflicts", "Fire Burn " + IhNum(_divineFireDot.Value) + "/s, Spirit Burn " + IhNum(_divineSpiritDot.Value) + "/s, " + IhNum(_divineSpiritDuration.Value) + "s"));
                    b.Append(IhLine("Trails", "16 Lightning Trails, " + IhNum(_divineTrailRange.Value) + "m"));
                    b.Append(IhLine("Trail Damage", IhDamage(_divineTrailDamage, power) + " every " + IhNum(_divineTrailPersistentTick.Value) + "s"));
                    if (ascended)
                    {
                        b.Append(IhLine("Thunderstorm", IhNum(_smiteStormRadius.Value) + "m, " + IhNum(_smiteStormDuration.Value) + "s"));
                        b.Append(IhLine("Storm Damage", IhDamage(_smiteStormDamage, power) + " every " + IhNum(_smiteStormTick.Value) + "s"));
                        b.Append(IhLine("Storm Burn", "Fire Burn, Spirit Burn, " + IhNum(_smiteStormDotDuration.Value) + "s"));
                    }
                    IhCosts(b, _divineStamina.Value, IhNum(_divineWindup.Value) + "s leap", _divineCooldown.Value);
                    break;
                case "heavens_light":
                    b.Append(IhLine("Buff", IhNum(_graceLightDefense.Value) + "% Overall Defense"));
                    b.Append(IhLine("Removes", "equipment movement penalties"));
                    b.Append(IhLine("Radius", IhNum(_graceLightRadius.Value) + "m"));
                    b.Append(IhLine("Duration", IhNum(_graceLightDuration.Value) + "s"));
                    b.Append(IhLine("Cost", "None"));
                    b.Append(IhLine("Wind Up Time", "Instant"));
                    b.Append(IhLine("Cooldown", IhNum(_graceLightCooldown.Value / 60f) + " min"));
                    b.Append(IhLine("Key", FormatHotbarBinding(BindGrace)));
                    break;
            }

            // v0.22.0: Warrior / Sorcerer skills list their approved Ascended effect until their
            // full stat tooltips come with each Advancement rework.
            string ascendedText = IhKitAscendedSummary(id);
            if (ascended && ascendedText.Length > 0)
                b.Append(IhLine("Ascended", ascendedText));
            if (IhKitPending(id))
                b.Append(IhLine("Status", "arrives with the " + (IhKitOf(id) != null ? IhKitOf(id).Ac : "Class") + " update"));
            string rule = IhAscensionRule(id);
            if (rule.Length > 0 && !ascended)
                b.Append(IhLine("Ascension", rule) + (ascendedText.Length > 0 ? IhLine("Ascended", ascendedText) : ""));
            return b.ToString().TrimEnd('\n');
        }

        private void IhCosts(System.Text.StringBuilder b, float stamina, string windup, float cooldown)
        {
            b.Append(IhLine("Stamina Cost", IhNum(stamina)));
            b.Append(IhLine("Wind Up Time", windup));
            b.Append(IhLine("Cooldown", IhNum(cooldown) + "s"));
        }

        // v0.20.6: the Tier line names only what the skill actually scales.
        private static string IhTierBonusLabel(string id)
        {
            switch (id)
            {
                case "holy_wave": case "ray_of_hope": case "holy_relic": return "Healing";
                case "divine_intervention": return "Damage & Healing";
            }
            return "Damage";
        }

        private static string IhLore(string id, bool ascended)
        {
            switch (id)
            {
                case "lightning_zap": return "Heaven's wrath leaps from your palm in a cone, branding every foe it touches with a Zap that soon bursts.";
                case "righteous_strike": return ascended
                    ? "Call down a holy pillar at your aim. Judgement falls, and lightning races across the earth in every direction."
                    : "Call down a pillar of holy lightning at your aim, smiting and exposing the wicked.";
                case "holy_wave": return "Release a warm tide of light that heals you and every ally it touches, and keeps mending them.";
                case "goddess_relic": return ascended
                    ? "The Goddess hurls her colossal cross at your aim. All beneath it are crushed and marked for judgement."
                    : "Summon the Goddess's cross from the heavens to crush all who stand beneath it.";
                case "judgement_hammer": return ascended
                    ? "Hurl a hammer of judgement that grows with every meter, then returns to the hand that threw it."
                    : "Hurl a hammer of judgement that grows heavier with every meter it flies, crushing all in its path.";
                case "shield_charge": return "Raise your shield and charge forward, trampling everyone who dares stand in your path.";
                case "fallen_angel": return ascended
                    ? "Leap to the heavens and fall like a burning star. The ground you strike catches holy fire."
                    : "Leap to the heavens, then crash upon your enemies like a blazing comet.";
                case "ray_of_hope": return ascended
                    ? "Unleash a radial wave that heals, shields and purifies you and every ally it touches, while searing the wicked."
                    : "Unleash a radial wave that heals and empowers you and every ally it touches, while searing the wicked.";
                case "electric_smite": return ascended
                    ? "Rise into the storm and strike the earth. Lightning races outward, and a thunderstorm rages where you land."
                    : "Rise into the storm and strike the earth with the fury of the heavens, sending lightning racing outward.";
                case "heavens_light": return "The light of heaven shields you and every ally beside you, lightening their burden.";
                // v0.22.0 Warrior
                case "heavy_slash": return "A heavy horizontal slash that breaks the bones of everything in front of you.";
                case "impact_wave": return "Strike the ground upward and send a shockwave tearing along the earth.";
                case "impact_punch": return "A quick, crushing punch that knocks small foes senseless.";
                case "moonlight_splitter": return "Three crescent waves of moonlight cleave through everything in their path.";
                case "crescent_cleave": return "Five giant crescent cleaves tear across the ground in a wide fan.";
                case "blade_storm": return "Rend space itself: a sphere of blades bursts at your aim, again and again.";
                case "frenzied_charge": return "Pull back, then dash forward with a thrust that launches small foes and stuns the large.";
                case "eclipse": return "Your blade swells with magic for one sweeping slash all around you.";
                case "halfmoon_slash": return "A colossal half-moon slash, followed by its afterimage.";
                case "knights_guidance": return "Lead your allies: faster movement, quicker stamina and less effort for every action.";
                case "stomp": return "Stomp the earth: a crushing impact, then an aftershock rolls outward.";
                case "circle_swing": return "Wind up and swing your weapon in a full circle, staggering everything around you.";
                case "bonecrusher": return "Leap high and crash down, shattering the bones of everything below.";
                case "seismic_guillotine": return "Tear a fissure through the ground to your aim, ending in a seismic explosion.";
                case "punishing_bomb": return "Bat a bomb into the enemy lines. It bursts on the first thing it touches and leaves them burning.";
                case "whirlwind": return "Spin into a whirlwind of steel, carving everything that comes near.";
                case "battlecry": return "A war cry that drives you and your allies to hit harder, in battle and at work.";
                // v0.22.0 Sorcerer
                case "flame_burst": return "A cone of fire bursts from your hands, setting every foe ablaze.";
                case "glacial_descent": return "Drop a massive chunk of ice onto your aim, freezing the ground around it.";
                case "stonefang_eruption": return "Jagged stone fangs erupt at your aim, piercing and crippling all above them.";
                case "meteor_fall": return "Call a meteor down on your aim. Hold to make it bigger.";
                case "gravity_dominion": return "Seize gravity at your aim: small foes are dragged in, every enemy is exposed.";
                case "astral_railcannon": return "Assemble an astral cannon and fire a devastating beam across the battlefield.";
                case "astral_greatblade": return "Summon an astral greatsword and slam it down along your aim.";
                case "frost_nova": return "Release a freezing nova around you.";
                case "elemental_cataclysm": return "Unleash every element at once on your aim. Hold to strengthen it.";
                case "clockwork": return "Bend time for you and your allies: stronger skills and faster cooldowns.";
                case "arcane_phalanx": return "Summon spectral swords around you and launch them at your aim.";
                case "afterimage_arsenal": return "Leave spectral copies of yourself that fight beside you.";
                case "void_step": return "Step through the void to your aim, without stopping what you are doing.";
                case "rift_echo": return "Open rifts behind your target that echo your attacks back through them.";
                case "gravity_blast": return "Launch a ball of darkness that drags small foes in and cripples the rest.";
                case "arcane_rupture": return "Rupture the arcane at your aim, up to three times in a row.";
                case "rift_walker": return "Open two linked portals for you and your allies.";
            }
            return "";
        }

        // Skills of the universal kits whose code is not written yet (cast shows a message).
        private static bool IhKitPending(string id)
        {
            switch (id)
            {
                case "frenzied_charge": case "eclipse": case "knights_guidance":
                case "punishing_bomb": case "battlecry": case "clockwork":
                case "gravity_blast": case "rift_walker":
                    return true;
            }
            return false;
        }

        // Approved Ascended versions (Framework, 2026-10-04) for the Warrior / Sorcerer kits.
        private static string IhKitAscendedSummary(string id)
        {
            switch (id)
            {
                case "impact_wave": return "15m x 3m; an aftershock runs back along the path (35%)";
                case "moonlight_splitter": return "4 fast waves (65%), then a double-size finisher (110%) and its afterimage (55%)";
                case "crescent_cleave": return "13 cleaves in two fans, burning fire trails and stacking Burn";
                case "blade_storm": return "6 stacks; each cast adds an extra cut (25%)";
                case "frenzied_charge": return "0.5s wind up, 12m, double width, 115% damage";
                case "eclipse": return "8m, 110%, reflects enemy projectiles";
                case "halfmoon_slash": return "a main slash and two more travel 20m, hitting every 0.3s";
                case "heavy_slash": return "5m reach, 140% damage, 2s Hyper Armor on hit";
                case "stomp": return "a third impact at 15m (40%)";
                case "circle_swing": return "9m, two swings (90% + 60%), Hyper Armor, launches";
                case "bonecrusher": return "the landing is followed by a ground shock (50%)";
                case "seismic_guillotine": return "25m, endpoint 140% and slows";
                case "punishing_bomb": return "12m, 6s ground fire and stacking Burn";
                case "whirlwind": return "8s with Hyper Armor and a final sweep";
                case "glacial_descent": return "8m; the central 3m deals 135% and Freezes";
                case "meteor_fall": return "3 smaller meteors follow (5 at full charge)";
                case "gravity_dominion": return "10m for 7s, pulls Big too, ends in an explosion";
                case "astral_railcannon": return "a steerable 4s beam";
                case "astral_greatblade": return "three slams, no charging";
                case "frost_nova": return "a 10m Frost Aura on you for 6s, then a freezing explosion";
                case "elemental_cataclysm": return "a second bombardment at 60%";
                case "stonefang_eruption": return "7m; the spikes stay 4s and keep hitting";
                case "arcane_phalanx": return "8 swords; a full volley erupts into Astral Spears";
                case "afterimage_arsenal": return "up to 3 Astral Clones that copy your attacks";
                case "void_step": return "2 charges, keeps momentum";
                case "rift_echo": return "echoes every 0.35s";
                case "gravity_blast": return "25m, bursts for 130% when it stops, pulls Big too";
                case "arcane_rupture": return "4 charges, 120% each";
            }
            return "";
        }

        // ------------------------------------------------------------------ tree drawing helpers
        private void IhEnsureTreeStyles()
        {
            if (_ihHeaderStyle != null)
                return;
            Font serif = FindValheimSerifFont();
            _ihHeaderStyle = new GUIStyle(GUI.skin.label);
            if (serif != null) _ihHeaderStyle.font = serif;
            _ihHeaderStyle.fontStyle = FontStyle.Bold;
            _ihHeaderStyle.alignment = TextAnchor.MiddleCenter;
            _ihHeaderStyle.richText = true;
            _ihHeaderStyle.wordWrap = false;
            _ihHeaderStyle.clipping = TextClipping.Overflow;

            _ihCountStyle = new GUIStyle(_ihHeaderStyle);
            _ihCountStyle.alignment = TextAnchor.MiddleLeft;

            _ihLockTextStyle = new GUIStyle(_ihHeaderStyle);
        }

        private void IhDrawHeaderPoints(Player player)
        {
            IhEnsureTreeStyles();
            bool advanced = IhIsClericAdvanced(player);
            string[] headerClassSkills = IhPlayerClassSkills(player);
            int classLeft = IhClassPointsEarned(player) - IhSpent(player, headerClassSkills) - IhPendingSum(headerClassSkills);
            int advLeft = IhAdvPointsEarned(player) - IhSpent(player, IhBranchSkills(player)) - IhPendingSum(IhBranchSkills(player));
            _ihHeaderStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(ScaleReferenceRect(0f, 0f, 0f, 11.5f).height));
            string ink = IhHex(0.36f, 0.26f, 0.18f);
            string red = IhHex(0.70f, 0.16f, 0.12f);
            string classText = advanced
                ? "<color=" + ink + ">TIER POINTS  0  ·  LOCKED</color>"
                : "<color=" + ink + ">TIER POINTS  </color><color=" + red + ">" + Mathf.Max(0, classLeft).ToString() + "</color>";
            string advText = advanced
                ? "<color=" + ink + ">TIER POINTS  </color><color=" + red + ">" + Mathf.Max(0, advLeft).ToString() + "</color>"
                : "<color=" + ink + ">SEALED  ·  ADVANCE AT LV 16</color>";
            GUI.Label(ScaleReferenceRect(150f, 122f, 108f, 18f), classText, _ihHeaderStyle);
            GUI.Label(ScaleReferenceRect(580f, 122f, 194f, 18f), advText, _ihHeaderStyle); // centred on the painted strip (x 577-776)
        }

        private int IhPendingSum(string[] ids)
        {
            int total = 0;
            for (int i = 0; i < ids.Length; i++)
                total += GetPrototypePending(ids[i]);
            return total;
        }

        // Pool / lock checks for queuing a pending Tier with +.
        private bool IhCanQueueTier(Player player, string id)
        {
            if (player == null || !IhIsUnlocked(player, id))
                return false;
            string[] queueClassSkills = IhPlayerClassSkills(player);
            if (IhContains(queueClassSkills, id))
            {
                if (!string.IsNullOrEmpty(GetAdvancement(player)))
                    return false; // the Class tree locks after Advancement
                return IhClassPointsEarned(player) - IhSpent(player, queueClassSkills) - IhPendingSum(queueClassSkills) > 0;
            }
            if (IhContains(IhBranchSkills(player), id))
                return IhAdvPointsEarned(player) - IhSpent(player, IhBranchSkills(player)) - IhPendingSum(IhBranchSkills(player)) > 0;
            return false;
        }

        // Stars row (Option C): gold = confirmed, glowing cyan = pending, dark = empty; count after the stars.
        private void IhDrawTierRow(ReferenceNodeUi node, int tier, int pending, out Rect rowRect)
        {
            int maxTier = IhMaxTier(node.Id);
            Vector2 anchor = GetReferenceNameplateAnchor(node);
            float size = IhIsUltimate(node.Id) ? 17f : (maxTier == 7 ? 12f : 14f);
            const float gap = 2f;
            float total = maxTier * size + (maxTier - 1) * gap;
            IhEnsureTreeStyles();
            _ihCountStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(ScaleReferenceRect(0f, 0f, 0f, 10f).height));
            string countText = (tier + pending).ToString() + "/" + maxTier.ToString();
            // v0.20.6: stars + count are centred together under the plate (the count used to hang
            // off the right, pushing the group ~13 px off centre). Width measured in reference px.
            float scale = Mathf.Max(0.01f, ScaleReferenceRect(0f, 0f, 100f, 0f).width / 100f);
            float countWidth = Mathf.Ceil(_ihCountStyle.CalcSize(new GUIContent(countText)).x / scale) + 1f;
            float x0 = anchor.x - (total + 3f + countWidth) * 0.5f;
            float y = anchor.y + 3f;
            for (int i = 0; i < maxTier; i++)
            {
                Texture2D tex = i < tier ? _ihStarFullTex : (i < tier + pending ? _ihStarPendingTex : _ihStarEmptyTex);
                if (tex != null)
                    GUI.DrawTexture(IhSnap(ScaleReferenceRect(x0 + i * (size + gap), y, size, size)), tex);
            }
            string color = pending > 0 ? IhHex(0.12f, 0.45f, 0.58f) : IhHex(0.40f, 0.27f, 0.12f);
            GUI.Label(ScaleReferenceRect(x0 + total + 3f, y - 1f, countWidth + 4f, size + 2f),
                "<color=" + color + ">" + countText + "</color>", _ihCountStyle);
            rowRect = new Rect(x0, y, total + 3f + countWidth, size);
        }

        private void IhDrawLockedNode(ReferenceNodeUi node, string reason)
        {
            IhDrawLockedRegion(node.Id);
            IhDrawFrameOverlay(node);
            if (_ihPadlockTex != null)
            {
                float lockSize = IhIsUltimate(node.Id) ? 30f : 24f;
                // Heaven's Light's painted frame sits above/right of its older hit rect.
                // Anchor its lock inside the actual frame, clear of the nameplate.
                // v0.20.6: same spot on every frame: the padlock's lower-right corner sits 4 px past
                // the art opening's corner (it used to vary from 1 to 12 px per slot).
                Rect lockRect = IhCornerLock(IhFieldRect(IhTemplateSlot(node.Id)),lockSize);
                GUI.DrawTexture(IhSnap(ScaleReferenceRect(lockRect.x, lockRect.y, lockRect.width, lockRect.height)), _ihPadlockTex);
            }
            IhEnsureTreeStyles();
            Vector2 anchor = GetReferenceNameplateAnchor(node);
            _ihLockTextStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(ScaleReferenceRect(0f, 0f, 0f, 9.5f).height));
            string shortReason = "Locked";
            if (IhIsUltimate(node.Id) && reason.StartsWith("Unlocks"))
                shortReason = "Ultimate Quest · Lv 36";
            else if (reason.StartsWith("Unlocks at "))
                shortReason = reason;
            else if (reason.StartsWith("Spend "))
                shortReason = reason.Replace(" Paladin", "").Replace(" Priest", "").Replace(" first", "");
            else if (reason.StartsWith("Advance"))
                shortReason = "Advance at Lv 16";
            GUI.Label(ScaleReferenceRect(anchor.x - 70f, anchor.y + 2f, 140f, 14f),
                "<color=" + IhHex(0.36f, 0.26f, 0.18f) + ">" + shortReason + "</color>", _ihLockTextStyle);
        }

        // =====================================================================================
        // v0.18.1 Functional Skill Tree hotbar (Cleric -> Paladin): the 7 numbered slots cast
        // whatever the tree layout holds with the [Hotbar] bindings, the Grace slot casts
        // Heaven's Light, Advance / Ascend happen from the tree.
        // =====================================================================================
        private ConfigEntry<float> _graceLightCooldown;
        private ConfigEntry<float> _graceLightDuration;
        private ConfigEntry<float> _graceLightRadius;
        private ConfigEntry<float> _graceLightDefense;
        private Texture2D _ihGraceIconTex;
        private float _ihAscendArmedUntil;
        private string _ihAscendArmedSkill = "";
        private static System.Text.RegularExpressions.Regex _ihNumberRegex;

        private void BindTreeHotbar()
        {
            _graceLightCooldown = Config.Bind("Paladin Heavens Light", "Cooldown", 600f, "Grace cooldown in seconds (10 min).");
            _graceLightDuration = Config.Bind("Paladin Heavens Light", "Duration", 60f, "Buff duration in seconds.");
            _graceLightRadius = Config.Bind("Paladin Heavens Light", "Radius", 10f, "Players within this radius when cast get the buff (snapshot).");
            _graceLightDefense = Config.Bind("Paladin Heavens Light", "OverallDefensePercent", 40f, "Overall Defense bonus (less damage taken).");
            DragonCombat.TreeHotbarProvider = IhUsesTreeHotbar;
        }

        // Cleric before Advancement and Cleric -> Paladin use the Skill Tree hotbar.
        // v0.22.0: every Class with a universal kit (Cleric, Warrior, Sorcerer) casts from the tree hotbar.
        private bool IhUsesTreeHotbar(Player player)
        {
            if (player == null || IhBranchesOf(GetClass(player)).Length == 0)
                return false;
            string advancement = GetAdvancement(player);
            return string.IsNullOrEmpty(advancement) || IhKitFor(GetClass(player), advancement) != null;
        }

        private bool IhBindingPressed(int index)
        {
            KeyCode key = _hotbarSlotKeys[index].Value;
            KeyCode mod = _hotbarSlotMods[index].Value;
            if (key == KeyCode.None || !Input.GetKeyDown(key))
                return false;
            return mod == KeyCode.None || Input.GetKey(mod);
        }

        private void HandleTreeHotbarInput(Player player)
        {
            if (Plugin.IsClassPanelOpen || DragonCombat.IsGameplayHudSuppressed())
                return;
            string[] layout = GetHotbarLayout();
            for (int i = 0; i < layout.Length && i < BindGrace; i++)
            {
                if (!string.IsNullOrEmpty(layout[i]) && IhBindingPressed(i))
                {
                    IhCastSkill(player, layout[i]);
                    return;
                }
            }
            if (IhBindingPressed(BindGrace))
                IhCastSkill(player, IhGraceFor(player));
        }

        private void IhCastSkill(Player player, string id)
        {
            if (player == null || player.IsDead() || !IhCanCast(player, id))
                return;
            // v0.20.9: a pending Buckler Parry empowerment goes to the next damaging skill that
            // actually starts (cooldown starts, or a Relic / its cast begins), never to a failed press.
            bool tryEmpower = _parryEmpowerPending && IhIsPriest(player) && IhPriestDamageSkill(id) && IhCooldown(player, id) <= 0f;
            bool relicBefore = id == "lightning_relic" && (FindPriestRelic(true) != null || _lightningRelicCasting);
            IhCastSkillNow(player, id);
            if (tryEmpower && !relicBefore)
            {
                bool started = IhCooldown(player, id) > 0f || (id == "lightning_relic" && (FindPriestRelic(true) != null || _lightningRelicCasting));
                if (started)
                {
                    _parryEmpowerPending = false;
                    _empoweredSkill = id;
                    _empoweredUntil = Time.time + IhSkillInstanceSeconds(id);
                }
            }
        }

        private void IhCastSkillNow(Player player, string id)
        {
            switch (id)
            {
                case "holy_wave":
                    if (IsAscendedSkill("holy_wave") && IhIsPriest(player)) { CastAscendedHolyWave(player); break; }
                    if (SkillsPlugin.Instance != null)
                        SkillsPlugin.Instance.CastFromHotbar(player, id);
                    break;
                case "lightning_zap":
                case "righteous_strike":
                    if (SkillsPlugin.Instance != null)
                        SkillsPlugin.Instance.CastFromHotbar(player, id);
                    break;
                case "goddess_relic": CastGoddessRelic(player); break;
                case "judgement_hammer": CastJudgementHammer(player); break;
                case "shield_charge":
                    if (!_shieldChargeActive)
                        CastShieldCharge(player);
                    break;
                case "fallen_angel": CastFallenAngel(player); break;
                case "ray_of_hope": CastRayOfHope(player); break;
                case "electric_smite": CastElectricSmite(player); break;
                case "heavens_light": CastHeavensLight(player); break;
                case "lightning_relic": CastLightningRelic(player); break;
                case "holy_relic": CastHolyRelic(player); break;
                case "divine_intervention": CastDivineIntervention(player); break;
                case "grand_cross": CastGrandCross(player); break;
                case "heavens_judgement": CastHeavensJudgement(player); break;
                case "lightning_tempest": CastLightningTempest(player); break;
                case "grand_sigil": ActivateGrandSigil(player); break;
                // v0.22.0 Warrior / Sword Master / Mercenary (current skill code until each AC rework).
                case "heavy_slash":
                case "impact_wave":
                case "impact_punch":
                case "flame_burst":
                case "glacial_descent":
                case "stonefang_eruption":
                    if (SkillsPlugin.Instance != null)
                        SkillsPlugin.Instance.CastFromHotbar(player, id);
                    break;
                case "moonlight_splitter": CastMoonlightSplitter(player); break;
                case "crescent_cleave": CastCrescentCleave(player); break;
                case "blade_storm": CastJudgementCut(player); break;
                case "halfmoon_slash": CastHalfmoonSlash(player); break;
                case "stomp": CastStomp(player); break;
                case "circle_swing": CastCircleSwing(player); break;
                case "bonecrusher": CastBonecrusher(player); break;
                case "seismic_guillotine": CastSeismicGuillotine(player); break;
                case "whirlwind": CastWhirlwind(player); break;
                default:
                    // Wizard / Spellcaster live in the Sorcerer module; not-yet-built skills say so.
                    if (!DragonCombat.TryExternalCast(player, id))
                        ShowMessage(IhSkillName(id) + " arrives with its " + (IhKitOf(id) != null ? IhKitOf(id).Ac : "Class") + " update.");
                    break;
            }
        }

        // GRACE - Heaven's Light: 10m snapshot, +40% Overall Defense, no equipment movement
        // penalties, 1 minute, 10 minute cooldown, free. Re-casting refreshes (never stacks).
        private void CastHeavensLight(Player player)
        {
            const string id = "Paladin.HeavensLight";
            if (!BeginCast(player, id, _graceLightCooldown.Value, 0f))
                return;
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlaySkillPose(player, "Chant", 0.50f);
            ShowMessage("Heaven's Light");

            float radius = Mathf.Max(1f, _graceLightRadius.Value);
            float duration = Mathf.Max(1f, _graceLightDuration.Value);
            Collider[] hits = Physics.OverlapSphere(player.transform.position, radius);
            HashSet<Player> allies = new HashSet<Player>();
            allies.Add(player);
            for (int i = 0; i < hits.Length; i++)
            {
                Player ally = hits[i].GetComponentInParent<Player>();
                if (ally != null)
                    allies.Add(ally);
            }
            foreach (Player ally in allies)
            {
                DragonCombat.ApplyTimedBuff(ally, "Paladin.HeavensLight", duration, 0f, 0f, 0f, Mathf.Max(0f, _graceLightDefense.Value) / 100f, 0f, 0f, false);
                DragonCombat.GrantNoEquipmentPenalty(ally, duration);
            }
            if (_enableVfx.Value)
            {
                StartCoroutine(AnimateRing(player.transform.position + Vector3.up * 0.10f, 0.6f, radius, 0.9f, new Color(1f, 0.86f, 0.42f, 0.95f), 0.10f));
                StartCoroutine(AnimateRing(player.transform.position + Vector3.up * 0.16f, 0.4f, radius * 0.6f, 0.7f, new Color(1f, 0.97f, 0.80f, 0.85f), 0.05f));
            }
        }

        private float IhCooldown(Player player, string id)
        {
            SkillsPlugin skills = SkillsPlugin.Instance;
            switch (id)
            {
                case "lightning_zap": return skills == null ? 0f : skills.GetCooldownForUi("Cleric.LightningZap");
                case "righteous_strike":
                    return IsAscendedSkill(id) ? GetCooldownRemaining("Paladin.AscendedRighteousStrike") : (skills == null ? 0f : skills.GetCooldownForUi("Cleric.RighteousStrike"));
                case "holy_wave":
                    return IsAscendedSkill(id) && IhIsPriest(player) ? GetCooldownRemaining("Priest.AscendedHolyWave") : (skills == null ? 0f : skills.GetCooldownForUi("Cleric.HolyWave"));
                case "goddess_relic": return GetCooldownRemaining("Paladin.GoddessRelic");
                case "judgement_hammer": return GetCooldownRemaining("Paladin.JudgementHammer");
                case "shield_charge": return GetCooldownRemaining("Paladin.ShieldCharge");
                case "fallen_angel": return GetCooldownRemaining("Paladin.FallenAngel");
                case "ray_of_hope": return GetCooldownRemaining("Paladin.RayOfHope");
                case "electric_smite": return GetCooldownRemaining("Paladin.ElectricSmite");
                case "heavens_light": return GetCooldownRemaining("Paladin.HeavensLight");
                case "lightning_relic": return GetCooldownRemaining("Priest.LightningRelic");
                case "holy_relic": return GetCooldownRemaining("Priest.HolyRelic");
                case "divine_intervention": return GetCooldownRemaining("Priest.DivineIntervention");
                case "grand_cross": return GetCooldownRemaining("Priest.GrandCross");
                case "heavens_judgement": return GetCooldownRemaining("Priest.HeavensJudgement");
                case "lightning_tempest": return GetCooldownRemaining("Priest.LightningTempest");
                case "grand_sigil": return GetCooldownRemaining("Priest.GrandSigil");
                case "heavy_slash": return skills == null ? 0f : skills.GetCooldownForUi("Warrior.HeavySlash");
                case "impact_wave": return skills == null ? 0f : skills.GetCooldownForUi("Warrior.ImpactWave");
                case "impact_punch": return skills == null ? 0f : skills.GetCooldownForUi("Warrior.ImpactPunch");
                case "flame_burst": return skills == null ? 0f : skills.GetCooldownForUi("Sorcerer.FlameBurst");
                case "glacial_descent": return skills == null ? 0f : skills.GetCooldownForUi("Sorcerer.GlacialDescent");
                case "stonefang_eruption": return skills == null ? 0f : skills.GetCooldownForUi("Sorcerer.StonefangEruption");
                case "moonlight_splitter": return GetCooldownRemaining("SwordMaster.MoonlightSplitter");
                case "crescent_cleave": return GetCooldownRemaining("SwordMaster.CrescentCleave");
                case "blade_storm": return GetReadyJudgementChargeIndex() >= 0 ? 0f : GetJudgementNextRecharge();
                case "halfmoon_slash": return GetCooldownRemaining("SwordMaster.HalfmoonSlash");
                case "stomp": return GetCooldownRemaining("Mercenary.Stomp");
                case "circle_swing": return GetCooldownRemaining("Mercenary.CircleSwing");
                case "bonecrusher": return GetCooldownRemaining("Mercenary.Bonecrusher");
                case "seismic_guillotine": return GetCooldownRemaining("Mercenary.SeismicGuillotine");
                case "whirlwind": return GetCooldownRemaining("Mercenary.Whirlwind");
            }
            return DragonCombat.ExternalCooldown(id);
        }

        // In-game HUD for the tree hotbar: same icons, layout and bindings as the Skill Tree.
        private void DrawTreeHotbarHud(Player player)
        {
            if (!EnsureReferenceBackdropLoaded())
                return;
            // v0.22.0: hotbar icons come from the branch canvas; build it even if the tree was never opened.
            IhKit iconKit = IhPlayerKit(player);
            if (iconKit == null)
            {
                string[] branches = IhBranchesOf(GetClass(player));
                if (branches.Length > 0) iconKit = IhKitFor(GetClass(player), branches[0]);
            }
            IhKitBackdrop(iconKit, false);
            string[] layout = GetHotbarLayout();
            float scale = Mathf.Clamp(_hudScale.Value, 0.65f, 1.45f);
            float size = 50f * scale;
            float gap = 6f * scale;
            float graceGap = 16f * scale;
            float totalWidth = size * 7f + gap * 6f + graceGap + size * 1.08f;
            float x = (Screen.width - totalWidth) * 0.5f;
            float reserve = Mathf.Clamp(_hudBottomOffset.Value, 70f, 260f) * scale;
            float y = Screen.height - reserve - size;

            string title = GetClass(player) + (string.IsNullOrEmpty(GetAdvancement(player)) ? "" : "  >  " + GetAdvancement(player)) + "   Lv " + IhGetLevel(player).ToString();
            _titleStyle.normal.textColor = new Color(0.60f, 0.88f, 1f, 1f);
            GUI.Label(new Rect(x, y - 22f * scale, totalWidth, 18f * scale), title.ToUpper(), _titleStyle);

            for (int i = 0; i < 7; i++)
            {
                Rect rect = new Rect(x + i * (size + gap), y, size, size * 1.1f);
                DrawHudTreeSlot(player, rect, i < layout.Length ? layout[i] : "", i, scale);
            }
            Rect grace = new Rect(x + 7f * (size + gap) - gap + graceGap, y - size * 0.04f, size * 1.08f, size * 1.18f);
            if (IhIsUnlocked(player, IhGraceFor(player)))
            {
                DrawHudTreeSlot(player, grace, IhGraceFor(player), BindGrace, scale);
            }
            else
            {
                if (GetSkillIconTex(IhGraceFor(player)) != null)
                {
                    GUI.color = new Color(0.32f, 0.32f, 0.32f, 1f);
                    GUI.DrawTexture(grace, GetSkillIconTex(IhGraceFor(player)));
                    GUI.color = Color.white;
                }
                if (_ihPadlockTex != null)
                    GUI.DrawTexture(new Rect(grace.center.x - grace.width * 0.22f, grace.center.y - grace.width * 0.22f, grace.width * 0.44f, grace.width * 0.44f), _ihPadlockTex);
                GUI.Label(new Rect(grace.x - 10f, grace.yMax - 1f, grace.width + 20f, 16f * scale), FormatHotbarBinding(BindGrace), _hudKeyStyle);
            }
        }

        private void DrawHudTreeSlot(Player player, Rect rect, string id, int binding, float scale)
        {
            Texture2D tex = string.IsNullOrEmpty(id) ? _treeSlotEmptyTex : GetSkillIconTex(id);
            if (tex == null)
                tex = _treeSlotEmptyTex;
            if (tex != null)
                GUI.DrawTexture(rect, tex);
            if (!string.IsNullOrEmpty(id))
            {
                IhDrawPlaceholderInitials(new Rect(rect.x + rect.width * 0.14f, rect.y + rect.height * 0.15f, rect.width * 0.72f, rect.height * 0.70f), id, false);
                if (IsPermanentHotbarSkill(id))
                    DrawPermanentBadge(rect);
                float cooldown = IhCooldown(player, id);
                if (cooldown > 0.05f)
                {
                    Rect inner = new Rect(rect.x + rect.width * 0.14f, rect.y + rect.height * 0.14f, rect.width * 0.72f, rect.height * 0.72f);
                    GUI.color = new Color(0f, 0f, 0f, 0.62f);
                    GUI.DrawTexture(inner, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(inner, cooldown >= 60f ? Mathf.CeilToInt(cooldown / 60f).ToString() + "m" : cooldown.ToString(cooldown >= 10f ? "0" : "0.0"), _hudCooldownStyle);
                }
            }
            GUI.Label(new Rect(rect.x - 10f, rect.yMax - 1f, rect.width + 20f, 16f * scale), FormatHotbarBinding(binding), _hudKeyStyle);
        }

        // ------------------------------------------------------------------ Advance / Ascend
        private bool IhAdvanceChecklist(Player player, List<string> lines)
        {
            return IhAdvanceChecklist(player, lines, IhTreeBranch());
        }

        // v0.21.1: the Class skill to max is the branch's Ascended Class skill
        // (Paladin: Righteous Strike, Priest: Holy Wave).
        private bool IhAdvanceChecklist(Player player, List<string> lines, string branch)
        {
            IhKit branchKit = IhKitFor(GetClass(player), branch);
            string prereq = branchKit != null ? branchKit.AscendedClass : IhAscendedClassSkill;
            int level = IhGetLevel(player);
            int rsTier = IhGetTier(player, prereq);
            int spent = IhSpent(player, IhPlayerClassSkills(player));
            bool lv = level >= 16;
            bool rs = rsTier >= 7;
            bool pts = spent >= 14;
            lines.Add((lv ? "+ " : "- ") + "Lv 16  (" + level.ToString() + ")");
            lines.Add((rs ? "+ " : "- ") + IhSkillName(prereq) + " Tier 7  (" + rsTier.ToString() + "/7)");
            lines.Add((pts ? "+ " : "- ") + "14 Class Tier Points spent  (" + spent.ToString() + "/14)");
            return lv && rs && pts;
        }

        private void IhAdvanceToPaladin(Player player)
        {
            if (player == null || IhBranchesOf(GetClass(player)).Length == 0 || !string.IsNullOrEmpty(GetAdvancement(player))) return;
            List<string> lines = new List<string>();
            if (!IhAdvanceChecklist(player, lines) || GetPrototypeTotalPending() > 0)
            {
                ShowMessage("Confirm your Class Tiers and complete the requirements first.");
                return;
            }
            string branch = IhTreeBranch();
            IhWrite(player, AdvancementDataKey, branch);
            _treePrototypePending.Clear();
            _treeSelectedNodeId = "";
            _hotbarLayoutOwnerKey = "";
            IhKit advancedKit = IhKitFor(GetClass(player), branch);
            ShowMessage("Advanced to " + branch + "!" + (advancedKit != null ? " " + IhSkillName(advancedKit.AscendedClass) + " has Ascended." : ""));
        }

        // Advance plaque inside the sealed Paladin panel (before Advancement).
        private void IhDrawAdvancePanel(Player player)
        {
            if (player == null || IhBranchesOf(GetClass(player)).Length == 0 || !string.IsNullOrEmpty(GetAdvancement(player)))
                return;
            IhEnsureTreeStyles();
            List<string> lines = new List<string>();
            bool ready = IhAdvanceChecklist(player, lines);
            Rect plaque = ScaleReferenceRect(500f, 408f, 160f, 42f);
            bool hover = plaque.Contains(Event.current.mousePosition);
            if (_treeConfirmPlaqueTex != null)
            {
                GUI.color = ready ? (hover ? Color.white : new Color(0.92f, 0.92f, 0.92f, 1f)) : new Color(0.55f, 0.55f, 0.55f, 1f);
                GUI.DrawTexture(plaque, _treeConfirmPlaqueTex);
                GUI.color = Color.white;
            }
            DrawFooterText(new Rect(plaque.x, plaque.y + plaque.height * 0.10f, plaque.width, plaque.height * 0.50f), "ADVANCE", hover && ready ? _treeFooterConfirmHoverStyle : _treeFooterConfirmStyle);
            DrawFooterText(new Rect(plaque.x, plaque.y + plaque.height * 0.58f, plaque.width, plaque.height * 0.28f), "TO " + IhTreeBranch().ToUpperInvariant(), _treeFooterPendingStyle);

            _ihLockTextStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(ScaleReferenceRect(0f, 0f, 0f, 10f).height));
            string ok = IhHex(0.20f, 0.45f, 0.18f);
            string no = IhHex(0.55f, 0.16f, 0.12f);
            for (int i = 0; i < lines.Count; i++)
            {
                bool done = lines[i].StartsWith("+");
                GUI.Label(ScaleReferenceRect(460f, 454f + i * 14f, 240f, 14f),
                    "<color=" + (done ? ok : no) + ">" + (done ? "◆ " : "◇ ") + lines[i].Substring(2) + "</color>", _ihLockTextStyle);
            }
            if (hover)
            {
                _treeHoveredTitle = "ADVANCE - " + IhTreeBranch().ToUpperInvariant();
                _treeHoveredBody = ready
                    ? "Become a " + IhTreeBranch() + ". The shared Class Tiers lock; Advancement Tier Points start at Lv 18."
                    : "Complete every requirement to Advance.";
            }
            if (ready && GUI.Button(plaque, GUIContent.none, GUIStyle.none))
                IhAdvanceToPaladin(player);
        }

        // ASCEND plaque in the footer (where CONFIRM appears) for the selected, eligible skill.
        private void IhDrawAscendButton(Player player)
        {
            if (player == null || string.IsNullOrEmpty(_treeSelectedNodeId) || GetPrototypeTotalPending() > 0)
                return;
            string message;
            if (!IhCheckAscend(player, _treeSelectedNodeId, false, out message))
                return;
            Rect plaque = ScaleReferenceRect(792f, 557f, 160f, 42f);
            bool hover = plaque.Contains(Event.current.mousePosition);
            bool armed = _ihAscendArmedSkill == _treeSelectedNodeId && Time.unscaledTime < _ihAscendArmedUntil;
            if (_treeConfirmPlaqueTex != null)
            {
                GUI.color = hover ? Color.white : new Color(0.92f, 0.92f, 0.92f, 1f);
                GUI.DrawTexture(plaque, _treeConfirmPlaqueTex);
                GUI.color = Color.white;
            }
            DrawFooterText(new Rect(plaque.x, plaque.y + plaque.height * 0.10f, plaque.width, plaque.height * 0.50f), armed ? "CLICK AGAIN" : "ASCEND", hover ? _treeFooterConfirmHoverStyle : _treeFooterConfirmStyle);
            DrawFooterText(new Rect(plaque.x, plaque.y + plaque.height * 0.58f, plaque.width, plaque.height * 0.28f), IhSkillName(_treeSelectedNodeId).ToUpperInvariant(), _treeFooterPendingStyle);
            if (hover)
            {
                _treeHoveredTitle = "ASCEND - " + IhSkillName(_treeSelectedNodeId).ToUpperInvariant();
                _treeHoveredBody = "Ascension is permanent for this character. Click twice to confirm.";
            }
            if (GUI.Button(plaque, GUIContent.none, GUIStyle.none))
            {
                if (!armed)
                {
                    _ihAscendArmedSkill = _treeSelectedNodeId;
                    _ihAscendArmedUntil = Time.unscaledTime + 3f;
                }
                else
                {
                    IhCheckAscend(player, _treeSelectedNodeId, true, out message);
                    ShowMessage(message);
                    _ihAscendArmedSkill = "";
                }
            }
        }

        private string IhAscensionRule(string id)
        {
            IhKit k = IhKitOf(id);
            if (k != null)
            {
                if (IhContains(k.Signatures, id)) return "Lv 32, Tier 5 (one Signature)";
                if (id == k.SpecialAscension) return "Lv 42, Tier 5 (its own Ascension quest)";
                if (IhContains(k.Normals, id))
                {
                    List<string> names = new List<string>();
                    for (int i = 0; i < k.Normals.Length; i++) names.Add(IhSkillName(k.Normals[i]));
                    return "Lv 42, Tier 5 (one of " + string.Join(", ", names.ToArray()) + ")";
                }
                if (id == k.Ultimate) return "Lv 50, Tier 3";
            }
            // Ascended Class skill of a branch of the viewer's Class.
            Player player = Player.m_localPlayer;
            string[] branches = player == null ? new string[0] : IhBranchesOf(GetClass(player));
            for (int i = 0; i < branches.Length; i++)
            {
                IhKit b = IhKitFor(GetClass(player), branches[i]);
                if (b != null && b.AscendedClass == id) return "when you Advance to " + b.Ac;
            }
            return "";
        }

        // ------------------------------------------------------------------ tooltip keywords
        private string IhColorize(string text)
        {
            if (_ihNumberRegex == null)
                _ihNumberRegex = new System.Text.RegularExpressions.Regex(
                    @"(?<![A-Za-z0-9_])[+-]?\d+(?:[.,]\d+)?(?:[ \t]*(?:%|°|(?:m/s|HP/s|HP|min|ms|m|s|x)\b|/s\b))?");
            return _ihNumberRegex.Replace(text, new System.Text.RegularExpressions.MatchEvaluator(IhColorMatch));
        }

        // Color values with their measurement units; descriptive words stay white.
        private string IhColorMatch(System.Text.RegularExpressions.Match match)
        {
            return "<color=" + IhHex(_ihTooltipAccent.r, _ihTooltipAccent.g, _ihTooltipAccent.b) + ">" + match.Value + "</color>";
        }

        private void ToggleSkillbook()
        {
            _skillbookOpen = !_skillbookOpen;

            if (_skillbookOpen)
            {
                _savedCursorVisible = Cursor.visible;
                _savedCursorLock = Cursor.lockState;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                DragonCombat.SetUiInputBlocked(true);
                _skillbookRect.x = (Screen.width - _skillbookRect.width) * 0.5f;
                _skillbookRect.y = (Screen.height - _skillbookRect.height) * 0.5f;
            }
            else
            {
                CancelHotbarKeyCapture();
                ClearTreePress();
                _treePrototypePending.Clear();
                _treeSelectedNodeId = "";
                DragonCombat.SetUiInputBlocked(false);
                RestoreCursor();
            }
        }

        private void RestoreCursor()
        {
            Cursor.visible = _savedCursorVisible;
            Cursor.lockState = _savedCursorLock;
        }

        private void OnGUI()
        {
            Player player = Player.m_localPlayer;

            if (player == null)
                return;

            EnsureUiStyles();

            if (_showCombatHud.Value && !_skillbookOpen && !Plugin.IsClassPanelOpen && !DragonCombat.IsGameplayHudSuppressed())
                DrawCombatHud(player);

            if (_skillbookOpen)
                _skillbookRect = GUI.Window(706060, _skillbookRect, DrawSkillbookWindow, "", _treeWindowStyle);
        }

        private void DrawCombatHud(Player player)
        {
            if (IhUsesTreeHotbar(player))
            {
                DrawTreeHotbarHud(player);
                return;
            }

            string className = GetClass(player);

            // Sorcerer / Wizard / Spellcaster own their HUD in the isolated
            // Sorcerer module. This keeps one HUD on screen at a time.
            if (className != "Warrior" && className != "Cleric")
                return;

            string advancement = GetAdvancement(player);
            List<string> names = new List<string>();
            List<float> cooldowns = new List<float>();

            if (className == "Warrior")
            {
                names.Add("Heavy Slash");
                names.Add("Impact Wave");
                names.Add("Impact Punch");
                cooldowns.Add(SkillsPlugin.Instance == null ? 0f : SkillsPlugin.Instance.GetCooldownForUi("Warrior.HeavySlash"));
                cooldowns.Add(SkillsPlugin.Instance == null ? 0f : SkillsPlugin.Instance.GetCooldownForUi("Warrior.ImpactWave"));
                cooldowns.Add(SkillsPlugin.Instance == null ? 0f : SkillsPlugin.Instance.GetCooldownForUi("Warrior.ImpactPunch"));
            }
            else
            {
                names.Add("Lightning Zap");
                names.Add("Righteous Strike");
                names.Add("Holy Wave");
                cooldowns.Add(SkillsPlugin.Instance == null ? 0f : SkillsPlugin.Instance.GetCooldownForUi("Cleric.LightningZap"));
                cooldowns.Add(SkillsPlugin.Instance == null ? 0f : SkillsPlugin.Instance.GetCooldownForUi("Cleric.RighteousStrike"));
                cooldowns.Add(SkillsPlugin.Instance == null ? 0f : SkillsPlugin.Instance.GetCooldownForUi("Cleric.HolyWave"));
            }

            if (!string.IsNullOrEmpty(advancement))
            {
                names.Add(GetAdvancedSkillName(advancement));
                cooldowns.Add(GetCooldownRemaining(GetAdvancedSkillId(advancement)));
                names.Add(GetAdvancedSkill5Name(advancement));
                cooldowns.Add(GetCooldownRemaining(GetAdvancedSkill5Id(advancement)));

                if (advancement == "Paladin")
                {
                    names.Add("Shield Charge"); cooldowns.Add(GetCooldownRemaining("Paladin.ShieldCharge"));
                    names.Add("Divine Verdict"); cooldowns.Add(GetCooldownRemaining("Paladin.DivineVerdict"));
                    names.Add("Aegis Fall"); cooldowns.Add(GetCooldownRemaining("Paladin.AegisFall"));
                    names.Add("Electric Smite"); cooldowns.Add(GetCooldownRemaining("Paladin.ElectricSmite"));
                }
                else if (advancement == "Mercenary")
                {
                    names.Add("Circle Swing"); cooldowns.Add(GetCooldownRemaining("Mercenary.CircleSwing"));
                    names.Add("Seismic Guillotine"); cooldowns.Add(GetCooldownRemaining("Mercenary.SeismicGuillotine"));
                    names.Add("Reaver's Orbit"); cooldowns.Add(GetCooldownRemaining("Mercenary.ReaversOrbit"));
                    names.Add("Whirlwind"); cooldowns.Add(GetCooldownRemaining("Mercenary.Whirlwind"));
                }
                else if (advancement == "Sword Master")
                {
                    names.Add("Judgement Cut"); cooldowns.Add(GetJudgementNextRecharge());
                    names.Add("Severed Horizon"); cooldowns.Add(GetCooldownRemaining("SwordMaster.SeveredHorizon"));
                    names.Add("Empty Sheath"); cooldowns.Add(GetCooldownRemaining("SwordMaster.EmptySheath"));
                    names.Add("Halfmoon Slash"); cooldowns.Add(GetCooldownRemaining("SwordMaster.HalfmoonSlash"));
                }
                else if (advancement == "Priest")
                {
                    names.Add("Divine Intervention"); cooldowns.Add(GetCooldownRemaining("Priest.DivineIntervention"));
                    names.Add("Grand Cross"); cooldowns.Add(GetCooldownRemaining("Priest.GrandCross"));
                    names.Add("Heaven's Judgement"); cooldowns.Add(GetCooldownRemaining("Priest.HeavensJudgement"));
                    names.Add("Lightning Tempest"); cooldowns.Add(GetCooldownRemaining("Priest.LightningTempest"));
                }
                else
                {
                    names.Add(GetUltimateName(advancement));
                    cooldowns.Add(GetCooldownRemaining(GetUltimateId(advancement)));
                }
            }

            int count = names.Count;
            if (count <= 0)
                return;

            float scale = Mathf.Clamp(_hudScale.Value, 0.65f, 1.45f);
            float size = 54f * scale;
            float gap = 5f * scale;
            float passiveGap = string.IsNullOrEmpty(advancement) ? 0f : 10f * scale;
            float passiveSize = string.IsNullOrEmpty(advancement) ? 0f : size;
            float totalWidth = size * count + gap * Mathf.Max(0, count - 1) + passiveGap + passiveSize;
            float x = (Screen.width - totalWidth) * 0.5f;
            float reserve = Mathf.Clamp(_hudBottomOffset.Value, 70f, 260f) * scale;
            float y = Screen.height - reserve - size;

            Color titleColor = className == "Warrior" ? new Color(1f, 0.66f, 0.30f, 1f) : new Color(0.60f, 0.88f, 1f, 1f);
            _titleStyle.normal.textColor = titleColor;
            string title = className;
            if (!string.IsNullOrEmpty(advancement))
                title += "  >  " + advancement;
            GUI.Label(new Rect(x, y - 21f * scale, totalWidth, 18f * scale), title.ToUpper(), _titleStyle);

            for (int i = 0; i < count; i++)
            {
                Rect rect = new Rect(x + i * (size + gap), y, size, size);
                Texture2D previous = _slotStyle.normal.background;
                float cooldown = cooldowns[i];
                _slotStyle.normal.background = cooldown > 0.05f ? _slotCooldownTex : _slotReadyTex;
                GUI.Box(rect, GetSkillInitials(names[i]), _slotStyle);
                GUI.Label(new Rect(rect.x + 4f * scale, rect.y + 2f * scale, 18f * scale, 16f * scale), (i + 1).ToString(), _hudKeyStyle);
                if (advancement == "Sword Master" && names[i] == "Judgement Cut")
                    GUI.Label(new Rect(rect.x + 3f * scale, rect.y + 30f * scale, rect.width - 6f * scale, 20f * scale), GetJudgementReadyChargeCount().ToString() + "/4", _hudCooldownStyle);
                else if (cooldown > 0.05f)
                    GUI.Label(new Rect(rect.x + 3f * scale, rect.y + 30f * scale, rect.width - 6f * scale, 20f * scale), cooldown.ToString("0.0"), _hudCooldownStyle);
                _slotStyle.normal.background = previous;
            }

            if (!string.IsNullOrEmpty(advancement))
            {
                float px = x + count * (size + gap) - gap + passiveGap;
                Rect passiveRect = new Rect(px, y, size, size);
                GUI.Box(passiveRect, GetSkillInitials(GetAdvancedPassiveName(advancement)), _slotLockedStyle);
                GUI.Label(new Rect(passiveRect.x + 4f * scale, passiveRect.y + 2f * scale, 20f * scale, 16f * scale), "R", _hudKeyStyle);
                if (advancement == "Mercenary")
                {
                    string furyLabel = IsUnchainedFuryActive() ? "FURY!" : Mathf.RoundToInt(_mercFury).ToString();
                    GUI.Label(new Rect(passiveRect.x + 3f * scale, passiveRect.y + 30f * scale, passiveRect.width - 6f * scale, 20f * scale), furyLabel, _hudCooldownStyle);
                }
            }
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

        private void DrawHudSlot(Rect rect, string icon, string name, string hotkey, float cooldown, bool locked)
        {
            GUIStyle style = locked ? _slotLockedStyle : _slotStyle;

            if (!locked)
                style.normal.background = cooldown > 0.01f ? _slotCooldownTex : _slotReadyTex;

            GUI.Box(rect, "", style);

            Color oldColor = GUI.color;
            GUI.color = locked ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.white;

            GUI.Label(
                new Rect(rect.x + 8f, rect.y + 6f, 28f, rect.height - 12f),
                icon,
                _bookHeaderStyle
            );

            GUI.Label(
                new Rect(rect.x + 38f, rect.y + 6f, rect.width - 44f, 24f),
                name,
                _bookTextStyle
            );

            string status = hotkey;

            if (!locked && cooldown > 0.01f)
                status += "   " + cooldown.ToString("0.0") + "s";

            GUI.Label(
                new Rect(rect.x + 38f, rect.y + 31f, rect.width - 44f, 22f),
                status,
                _passiveStyle
            );

            GUI.color = oldColor;
        }

        private void DrawSkillbookWindow(int windowId)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            _treeHoveredTitle = "";
            _treeHoveredBody = "";

            string className = GetClass(player);
            string advancement = GetAdvancement(player);

            // The first live visual prototype is Cleric -> Paladin because that is the
            // branch used to establish the Immortal Heroes UI framework. Clerics may
            // preview Paladin before Advancement; an already-selected Paladin uses the
            // same layout. Every other branch keeps the v0.12.3 Skillbook until its
            // Immortal Heroes tree is authored from this reusable framework.
            if (!(IhBranchesOf(className).Length > 0 && (string.IsNullOrEmpty(advancement) || IhKitFor(className, advancement) != null)))
            {
                DrawLegacySkillbookWindow(windowId);
                return;
            }

            if (EnsureReferenceBackdropLoaded() && (IhTreeBranch() != "Priest" || _ihPriestBackdropTex != null))
            {
                DrawReferenceClericPaladinTree();
                DrawTreeTooltip();
                HandleTreePointer();

                // The close button is already painted into the reference art. Keep its hotspot
                // invisible so the asset stays visually 1:1 instead of receiving a second IMGUI button.
                if (GUI.Button(ScaleReferenceRect(936f, 17f, 38f, 38f), GUIContent.none, GUIStyle.none))
                    ToggleSkillbook();

                GUI.DragWindow(ScaleReferenceRect(0f, 0f, 930f, 62f));
                return;
            }

            // Asset-safe fallback: if the PNG is missing for any reason, the previous procedural
            // renderer remains available instead of breaking the Skill Tree.
            DrawImmortalHeroesBackdrop();
            if (IhTreeBranch() == "Priest")
                DrawLegacySkillbookWindow(windowId);
            else
                DrawClericPaladinTree(player, advancement == "Paladin");
            DrawTreeTooltip();

            if (GUI.Button(new Rect(_skillbookRect.width - 54f, 15f, 34f, 30f), "X"))
                ToggleSkillbook();

            GUI.DragWindow(new Rect(0f, 0f, _skillbookRect.width - 70f, 52f));
        }

        private bool EnsureReferenceBackdropLoaded()
        {
            if (_treeReferenceBackdropTex != null)
                return true;

            if (_treeReferenceBackdropLoadAttempted)
                return false;

            _treeReferenceBackdropLoadAttempted = true;
            _treeReferenceBackdropTex = LoadUiPng("Cleric_Paladin_Reference.png");
            if (_treeReferenceBackdropTex == null)
                return false;

            // v0.15.0 component art. Each is optional: a missing file falls back to code drawing.
            _treeTierPlusTex = LoadUiPng("Tier_Plus.png");
            _treeSlotEmptyTex = LoadUiPng("Slot_Empty.png");
            _treePermanentBadgeTex = LoadUiPng("Badge_Permanent.png");
            for (int i = 0; i < ClericPaladinReferenceNodes.Length; i++)
            {
                ReferenceNodeUi skillNode = ClericPaladinReferenceNodes[i];
                if (skillNode.Kind == TreeNodeKind.Grace)
                    continue;
                Texture2D icon = LoadUiPng("Icon_" + skillNode.Id + ".png");
                if (icon != null)
                    _treeSkillIconTex[skillNode.Id] = icon;
            }
            _treeTierMinusTex = LoadUiPng("Tier_Minus.png");
            _treeConfirmPlaqueTex = LoadUiPng("Confirm_Plaque.png");
            // v0.18.0 progression art.
            _ihPreAdvanceBackdropTex = LoadUiPng("Cleric_Paladin_PreAdvance.png");
            _ihStarFullTex = LoadUiPng("Tier_Star_Full.png", true);
            _ihStarPendingTex = LoadUiPng("Tier_Star_Pending.png", true);
            _ihStarEmptyTex = LoadUiPng("Tier_Star_Empty.png", true);
            _ihPadlockTex = LoadUiPng("Lock_Padlock.png", true);
            _ihLockedBackdropTex = LoadUiPng("Cleric_Paladin_Locked.png");
            _ihRsNormalIconTex = LoadUiPng("Icon_righteous_strike_Normal.png");
            _ihGraceIconTex = LoadUiPng("Icon_heavens_light.png");
            IhRepairLightningZapBackdrop(_treeReferenceBackdropTex);
            IhRepairLightningZapBackdrop(_ihPreAdvanceBackdropTex);
            foreach(Texture2D texture in _ihAscendedIconArt.Values) if(texture!=null) Destroy(texture);
            _ihAscendedIconArt.Clear();
            IhComposeHotbarIcons(ClericPaladinReferenceNodes, _ihPreAdvanceBackdropTex);
            IhPrepareUniversalBanner(_treeReferenceBackdropTex);
            IhPrepareUniversalBanner(_ihPreAdvanceBackdropTex);
            IhPrepareSharedNameplates();
            IhLoadPriestArtwork();
            return true;
        }

        private Texture2D LoadUiPng(string fileName)
        {
            return LoadUiPng(fileName, false);
        }

        // v0.18.2: small icons (stars, padlock) use mipmaps so they stay crisp when drawn small.
        private Texture2D LoadUiPng(string fileName, bool mipmaps)
        {
            try
            {
                // Keep raw-file IO late-bound here. Windows PowerShell Add-Type can otherwise
                // try to resolve Valheim's IO facade through a second mscorlib reference while
                // compiling this staging DLL, which produces a duplicate-identity compiler error.
                string assetPath = Paths.PluginPath + "/ImmortalHeroesAssets/" + fileName;
                Type fileType = typeof(object).Assembly.GetType("System.IO.File");
                if (fileType == null)
                {
                    Logger.LogWarning("Immortal Heroes UI file API unavailable; using procedural fallback.");
                    return null;
                }

                MethodInfo existsMethod = fileType.GetMethod("Exists", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
                MethodInfo readMethod = fileType.GetMethod("ReadAllBytes", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
                if (existsMethod == null || readMethod == null || !(bool)existsMethod.Invoke(null, new object[] { assetPath }))
                {
                    Logger.LogWarning("Immortal Heroes UI asset missing; using procedural fallback: " + assetPath);
                    return null;
                }

                byte[] bytes = (byte[])readMethod.Invoke(null, new object[] { assetPath });
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipmaps);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear;

                // Do NOT call Texture2D.LoadImage(byte[]) directly here. Valheim's current
                // Unity ImageConversion assembly exposes modern span-based overload metadata that
                // Windows PowerShell Add-Type cannot resolve against its own .NET Framework
                // mscorlib. Late-bind the legacy byte[] overload so staging compilation never
                // touches System.ReadOnlySpan<T>.
                Type imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                if (imageConversionType == null)
                {
                    Destroy(texture);
                    Logger.LogWarning("Immortal Heroes UI image decoder unavailable; using procedural fallback.");
                    return null;
                }

                MethodInfo loadImageMethod = imageConversionType.GetMethod(
                    "LoadImage",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
                    null
                );

                if (loadImageMethod == null)
                {
                    Destroy(texture);
                    Logger.LogWarning("Immortal Heroes UI byte-array decoder unavailable; using procedural fallback.");
                    return null;
                }

                object decodeResult = loadImageMethod.Invoke(null, new object[] { texture, bytes, false });
                if (!(decodeResult is bool) || !(bool)decodeResult)
                {
                    Destroy(texture);
                    Logger.LogWarning("Immortal Heroes UI asset could not be decoded; using procedural fallback.");
                    return null;
                }

                texture.name = "ImmortalHeroes_" + fileName;
                ApplyLinearColorSpaceCompensation(texture);
                Logger.LogInfo("Immortal Heroes UI asset loaded once: " + fileName + " " + texture.width + "x" + texture.height);
                return texture;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Immortal Heroes UI asset load failed (" + fileName + "); using procedural fallback. " + ex.Message);
                return null;
            }
        }

        private static Font FindValheimFont(string name)
        {
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            for (int i = 0; i < fonts.Length; i++)
            {
                if (fonts[i] != null && fonts[i].name == name)
                    return fonts[i];
            }
            return null;
        }

        private static Font FindValheimSerifFont()
        {
            // Valheim's own UI serif. Falls back to the IMGUI default if it cannot be found.
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            Font fallback = null;
            for (int i = 0; i < fonts.Length; i++)
            {
                if (fonts[i] == null)
                    continue;
                if (fonts[i].name == "AveriaSerifLibre-Bold")
                    return fonts[i];
                if (fallback == null && fonts[i].name.StartsWith("AveriaSerifLibre"))
                    fallback = fonts[i];
            }
            return fallback;
        }

        private void ApplyLinearColorSpaceCompensation(Texture2D texture)
        {
            // v0.14.7: Valheim renders in Linear color space and IMGUI was presenting this sRGB
            // artwork with an extra gamma lift (measured in-game: displayed = source^(1/2.2)),
            // which made the whole tree look bleached. Pre-compensate the pixels once at load so
            // the approved artwork appears exactly as painted. The artwork file itself is untouched.
            if (texture == null || _uiColorSpaceCorrection == null || !_uiColorSpaceCorrection.Value)
                return;
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
                return;

            Color32[] pixels = texture.GetPixels32();
            byte[] lut = new byte[256 * 4];
            for (int i = 0; i < 256; i++)
            {
                // Four ordered-dither variants per input value keep dark navy gradients from banding.
                float linear = Mathf.GammaToLinearSpace(i / 255f) * 255f;
                for (int d = 0; d < 4; d++)
                {
                    float dither = (d + 0.5f) / 4f - 0.5f;
                    lut[i * 4 + d] = (byte)Mathf.Clamp(Mathf.RoundToInt(linear + dither), 0, 255);
                }
            }

            int width = texture.width;
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                int d = ((x & 1) << 1) | ((x ^ y) & 1);
                Color32 c = pixels[i];
                pixels[i] = new Color32(lut[c.r * 4 + d], lut[c.g * 4 + d], lut[c.b * 4 + d], c.a);
            }

            texture.SetPixels32(pixels);
            texture.Apply(texture.mipmapCount > 1, false);
            Logger.LogInfo("Immortal Heroes Skill Tree: Linear color-space compensation applied to UI artwork.");
        }

        private Rect ScaleReferenceRect(float x, float y, float width, float height)
        {
            const float sourceWidth = 1011f;
            const float sourceHeight = 662f;
            float sx = _skillbookRect.width / sourceWidth;
            float sy = _skillbookRect.height / sourceHeight;
            return new Rect(x * sx, y * sy, width * sx, height * sy);
        }

        private void RegisterReferenceHotspot(Rect rect, string title, string body)
        {
            if (!rect.Contains(Event.current.mousePosition))
                return;

            _treeHoveredTitle = title;
            _treeHoveredBody = body;
        }

        private sealed class ReferenceNodeUi
        {
            public readonly string Id;
            public readonly Rect GroupRect;
            public readonly Rect IconRect;
            public readonly string Hotkey;
            public readonly TreeNodeKind Kind;
            public readonly bool Mandatory;
            public readonly int MaxTier;
            public readonly string TooltipTitle;
            public readonly string TooltipBody;

            public ReferenceNodeUi(string id, Rect groupRect, Rect iconRect, string hotkey, TreeNodeKind kind, bool mandatory, int maxTier, string tooltipTitle, string tooltipBody)
            {
                Id = id;
                GroupRect = groupRect;
                IconRect = iconRect;
                Hotkey = hotkey;
                Kind = kind;
                Mandatory = mandatory;
                MaxTier = maxTier;
                TooltipTitle = tooltipTitle;
                TooltipBody = tooltipBody;
            }
        }

        private static readonly ReferenceNodeUi[] ClericPaladinReferenceNodes =
        {
            new ReferenceNodeUi("lightning_zap", new Rect(151f, 151f, 98f, 105f), new Rect(164f, 158f, 72f, 72f), "", TreeNodeKind.ClassNormal, false, 7,
                "ATTACK - LIGHTNING ZAP", "Cleric Class skill."),
            new ReferenceNodeUi("righteous_strike", new Rect(151f, 287f, 98f, 105f), new Rect(167f, 290f, 69f, 69f), "1", TreeNodeKind.Ascended, true, 7,
                "ATTACK - RIGHTEOUS STRIKE", "Paladin's Ascended Class skill. Permanent on the hotbar after Advancement."),
            new ReferenceNodeUi("holy_wave", new Rect(151f, 413f, 98f, 105f), new Rect(167f, 416f, 69f, 69f), "6", TreeNodeKind.Buff, false, 7,
                "BUFF - HOLY WAVE", "7m pulse: heal 25 HP immediately, then 5% Total HP per second for 6 seconds."),

            new ReferenceNodeUi("goddess_relic", new Rect(357f, 151f, 103f, 105f), new Rect(369f, 158f, 70f, 72f), "2", TreeNodeKind.Signature, true, 5,
                "ATTACK - GODDESS RELIC", "Paladin Signature Skill. Mandatory numbered hotbar skill."),
            new ReferenceNodeUi("judgement_hammer", new Rect(357f, 287f, 103f, 105f), new Rect(369f, 290f, 70f, 69f), "3", TreeNodeKind.Signature, true, 5,
                "ATTACK - JUDGEMENT HAMMER", "Paladin Signature Skill. Mandatory numbered hotbar skill."),
            new ReferenceNodeUi("heavens_light", new Rect(357f, 413f, 103f, 105f), new Rect(370f, 416f, 71f, 71f), "M4 + R", TreeNodeKind.Grace, true, 0,
                "GRACE - HEAVEN'S LIGHT", "10m cast snapshot: +40% Overall Defense and removes equipment Movement Speed penalties for 1 minute. 10 minute cooldown."),

            new ReferenceNodeUi("shield_charge", new Rect(532f, 151f, 103f, 105f), new Rect(548f, 158f, 68f, 72f), "4", TreeNodeKind.AdvancementNormal, false, 5,
                "ATTACK - SHIELD CHARGE", "Paladin Advancement skill. Optional hotbar skill."),
            new ReferenceNodeUi("fallen_angel", new Rect(672f, 151f, 103f, 105f), new Rect(686f, 158f, 69f, 72f), "", TreeNodeKind.AdvancementNormal, false, 5,
                "ATTACK - ANGEL COMET", "Paladin Advancement skill. Optional hotbar skill."),
            new ReferenceNodeUi("ray_of_hope", new Rect(672f, 287f, 103f, 105f), new Rect(686f, 290f, 69f, 69f), "5", TreeNodeKind.Buff, false, 5,
                "BUFF - RAY OF HOPE", "Paladin support skill. Optional hotbar skill."),

            new ReferenceNodeUi("electric_smite", new Rect(831f, 204f, 133f, 165f), new Rect(850f, 214f, 103f, 108f), "7", TreeNodeKind.Ultimate, true, 3,
                "ULTIMATE - ELECTRIC SMITE", "Acrobatic landing followed by sixteen 10m Ground Projectile Lightning Trails with Persistent Damage.")
        };

        private static readonly ReferenceNodeUi[] ClericPriestReferenceNodes =
        {
            ClericPaladinReferenceNodes[0], ClericPaladinReferenceNodes[1], ClericPaladinReferenceNodes[2],
            new ReferenceNodeUi("lightning_relic", ClericPaladinReferenceNodes[3].GroupRect, ClericPaladinReferenceNodes[3].IconRect,"",TreeNodeKind.Signature,true,5,"ATTACK - LIGHTNING RELIC",""),
            new ReferenceNodeUi("holy_relic", ClericPaladinReferenceNodes[4].GroupRect, ClericPaladinReferenceNodes[4].IconRect,"",TreeNodeKind.Signature,true,5,"BUFF - HOLY RELIC",""),
            new ReferenceNodeUi("grand_sigil", ClericPaladinReferenceNodes[5].GroupRect, ClericPaladinReferenceNodes[5].IconRect,"",TreeNodeKind.Grace,true,0,"GRACE - HEAVEN'S CRUCIBLE",""),
            new ReferenceNodeUi("divine_intervention", ClericPaladinReferenceNodes[6].GroupRect, ClericPaladinReferenceNodes[6].IconRect,"",TreeNodeKind.AdvancementNormal,false,5,"SUPPORT - DIVINE INTERVENTION",""),
            new ReferenceNodeUi("grand_cross", ClericPaladinReferenceNodes[7].GroupRect, ClericPaladinReferenceNodes[7].IconRect,"",TreeNodeKind.AdvancementNormal,false,5,"ATTACK - GRAND CROSS",""),
            new ReferenceNodeUi("heavens_judgement", ClericPaladinReferenceNodes[8].GroupRect, ClericPaladinReferenceNodes[8].IconRect,"",TreeNodeKind.AdvancementNormal,false,5,"ATTACK - HEAVEN'S JUDGEMENT",""),
            new ReferenceNodeUi("lightning_tempest", ClericPaladinReferenceNodes[9].GroupRect, ClericPaladinReferenceNodes[9].IconRect,"",TreeNodeKind.Ultimate,true,3,"ULTIMATE - LIGHTNING TEMPEST","")
        };

        private void DrawReferenceClericPaladinTree()
        {
            Rect full = new Rect(0f, 0f, _skillbookRect.width, _skillbookRect.height);
            GUI.color = Color.white;
            Player treePlayer = Player.m_localPlayer;
            // v0.18.0: before Advancement every Class skill is Cyan (no Ascended Righteous Strike).
            // v0.22.0: every branch draws its own universal canvas (same chassis, own skill art).
            string treeClass = GetClass(treePlayer);
            Texture2D backdrop = IhKitBackdrop(IhTreeKit(), false);
            if (backdrop == null)
                backdrop = _ihPreAdvanceBackdropTex != null ? _ihPreAdvanceBackdropTex : _treeReferenceBackdropTex;
            GUI.DrawTexture(full, backdrop, ScaleMode.StretchToFill, true);
            IhDrawHeaderPoints(treePlayer);

            RegisterReferenceHotspot(ScaleReferenceRect(31f, 76f, 286f, 64f),
                IhClassBlessingTitle(treeClass), IhClassBlessingText(treeClass));
            string treeBranch = IhTreeBranch();
            if (treeBranch == "Priest" || treeBranch == "Paladin")
                RegisterReferenceHotspot(ScaleReferenceRect(332f, 76f, 651f, 64f),
                    (treeBranch == "Priest" ? "BLESS THY SINNERS - MASTERY" : "HOLY TRINITY - MASTERY"),
                    treeBranch == "Priest" ? GetAdvancedPassiveDescription(treePlayer, "Priest") : "Club-type melee + Shield: +15 Clubs (effective cap 100), no Armor movement penalties, and the Club's current Blunt damage guarantees Slash and Pierce each reach at least 50% of that Blunt value without lowering existing damage.");
            else
                RegisterReferenceHotspot(ScaleReferenceRect(332f, 76f, 651f, 64f), IhMasteryTitle(treeBranch), IhMasteryText(treeBranch));

            IhDrawBranchSelector(treePlayer);
            ReferenceNodeUi[] nodes = IhTreeNodes();
            for (int i = 0; i < nodes.Length; i++)
                DrawReferenceNode(nodes[i]);
            IhDrawUniversalBranchLabels();
            IhDrawAdvancePanel(treePlayer);

            DrawReferenceHotbarSlots();
            DrawReferenceHotbarHotkeys();
            DrawReferenceFooterUx();

            if (_dragActive)
            {
                _treeHoveredTitle = "";
                _treeHoveredBody = "";
            }
        }

        // v0.18.4: repair the cached backdrop once, rather than drawing a rectangular
        // overlay every frame. Match the repair to the existing boundary with a harmonic
        // color correction so the surrounding Cleric parchment has no pasted edge.
        private void IhRepairLightningZapBackdrop(Texture2D backdrop)
        {
            Texture2D zap = GetSkillIconTex("lightning_zap");
            Texture2D donor = _ihPreAdvanceBackdropTex != null ? _ihPreAdvanceBackdropTex : backdrop;
            if (backdrop == null || donor == null || zap == null ||
                backdrop.width != 1011 || backdrop.height != 662)
                return;
            const int left = 156, top = 146, width = 92, height = 82;
            Color[] art = new Color[width * height];
            Color[] correction = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int px = left + x, py = top + y, i = y * width + x;
                    // Holy Wave's frame is 256 reference pixels below Zap's frame.
                    // Extend the blank top of the donor plaque; never copy its lettering.
                    Color c = donor.GetPixel(px, 661 - Mathf.Min(py + 256, 480));
                    if (px >= 175 && px < 229 && py >= 165 && py < 215)
                    {
                        float u = (7f + (px - 175f + 0.5f) * 35f / 54f) / 49f;
                        float v = 1f - (7f + (py - 165f + 0.5f) * 37f / 50f) / 54f;
                        c = zap.GetPixelBilinear(u, v);
                    }
                    art[i] = c;
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        correction[i] = backdrop.GetPixel(px, 661 - py) - c;
                }
            }
            // Solve the boundary color difference inward. All outside pixels, the
            // nameplate and Tier row remain exactly as supplied in the backdrop.
            for (int pass = 0; pass < 500; pass++)
                for (int y = 1; y < height - 1; y++)
                    for (int x = 1; x < width - 1; x++)
                    {
                        int i = y * width + x;
                        correction[i] = (correction[i - 1] + correction[i + 1] +
                            correction[i - width] + correction[i + width]) * 0.25f;
                    }
            for (int y = 1; y < height - 1; y++)
                for (int x = 1; x < width - 1; x++)
                {
                    int i = y * width + x;
                    Color c = art[i] + correction[i];
                    c.a = 1f;
                    backdrop.SetPixel(left + x, 661 - (top + y), c);
                }
            backdrop.Apply(false, false);
        }

        // Both branches preview the same starter Class. Changing a preview never changes the player.
        private void IhDrawBranchSelector(Player player)
        {
            if (player == null || !string.IsNullOrEmpty(GetAdvancement(player))) return;
            string[] branches = IhBranchesOf(GetClass(player));
            // v0.22.0: one shared font size that fits the longest branch name (SWORD MASTER) inside
            // its plaque, so both plaques match and nothing spills onto the neighbour.
            Rect probe = ScaleReferenceRect(794f, 120f, 88f, 25f);
            GUIStyle measure = new GUIStyle(_treeFooterConfirmStyle);
            measure.fontSize = Mathf.Max(9, Mathf.RoundToInt(probe.height * 0.46f));
            for (int i = 0; i < branches.Length; i++)
                while (measure.fontSize > 7 && measure.CalcSize(new GUIContent(branches[i].ToUpperInvariant())).x > probe.width * 0.84f)
                    measure.fontSize--;
            for (int i = 0; i < branches.Length; i++)
            {
                // v0.20.7: same ornate plaque as CONFIRM / ADVANCE; the unselected branch is dimmed.
                Rect r = ScaleReferenceRect(794f + i * 92f, 120f, 88f, 25f);
                bool selected = _ihPreviewBranch == branches[i];
                bool hover = r.Contains(Event.current.mousePosition);
                if (_treeConfirmPlaqueTex != null)
                {
                    Color old = GUI.color;
                    GUI.color = selected ? Color.white : (hover ? new Color(0.82f, 0.82f, 0.82f, 1f) : new Color(0.58f, 0.58f, 0.58f, 1f));
                    GUI.DrawTexture(r, _treeConfirmPlaqueTex);
                    GUI.color = old;
                }
                else
                    DrawFilledBorder(r, new Color(0.10f,0.12f,0.15f,0.95f),
                        selected ? new Color(1f,0.80f,0.30f,1f) : new Color(0.55f,0.46f,0.30f,1f), 1f);
                GUIStyle style = new GUIStyle(selected ? _treeFooterConfirmStyle : _treeFooterPendingStyle);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = measure.fontSize;
                DrawFooterText(r, branches[i].ToUpperInvariant(), style);
                if (r.Contains(Event.current.mousePosition))
                {
                    _treeHoveredTitle = branches[i].ToUpperInvariant() + " - PREVIEW";
                    _treeHoveredBody = "View the " + branches[i] + " branch. Your " + GetClass(player) + " skills and pending Class Tiers stay the same.";
                }
                if (GUI.Button(r,GUIContent.none,GUIStyle.none) && !selected)
                {
                    _ihPreviewBranch = branches[i];
                    _treeSelectedNodeId = "";
                    _dragActive = false;
                    _pressSkillId = "";
                    _ihAscendArmedSkill = "";
                    // Class pending Tiers are shared; previewing the other branch preserves them.
                }
            }
        }

        private Texture2D IhCopyTexture(Texture2D source, string name)
        {
            Texture2D texture = new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(source.GetPixels());
            texture.Apply(false,false);
            return texture;
        }

        // One set of measured painted-frame bounds, shared by both branches.
        private static Rect IhVisualIconRect(ReferenceNodeUi node)
        {
            switch(IhTemplateSlot(node.Id))
            {
                case "lightning_zap": return new Rect(167f,158f,67f,65f);
                case "righteous_strike": return new Rect(167f,290f,69f,65f);
                case "holy_wave": return new Rect(167f,416f,69f,63f);
                case "goddess_relic": return new Rect(369f,158f,70f,65f);
                case "judgement_hammer": return new Rect(369f,290f,70f,65f);
                case "shield_charge": return new Rect(548f,158f,68f,65f);
                case "fallen_angel": return new Rect(686f,158f,69f,65f);
                case "ray_of_hope": return new Rect(686f,290f,69f,65f);
                case "heavens_light": return new Rect(381f,410f,72f,64f);
                case "electric_smite": return new Rect(850f,214f,103f,108f);
            }
            return node.IconRect;
        }

        private static Rect IhCornerLock(Rect frame, float size)
        {
            // Padlock sits inside the lower-right border, never on the nameplate.
            return new Rect(frame.xMax+4f-size,frame.yMax+4f-size,size,size);
        }

        private readonly Dictionary<string,Texture2D> _ihAscendedIconArt = new Dictionary<string,Texture2D>();
        private readonly List<Texture2D> _ihPolishTextures = new List<Texture2D>();

        private Texture2D IhAscendedArt(string id, Texture2D normal)
        {
            if(normal==null) return null;
            Texture2D result;
            if(_ihAscendedIconArt.TryGetValue(id,out result)) return result;
            result=IhCopyTexture(normal,"ImmortalHeroes_Ascended_"+id);
            for(int y=0;y<result.height;y++) for(int x=0;x<result.width;x++)
            {
                Color c=result.GetPixel(x,y);
                // Preserve neutral glyphs and gold trim; recolor chromatic skill energy.
                float max=Mathf.Max(c.r,Mathf.Max(c.g,c.b)), min=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
                bool energy=c.b>c.r*1.12f || c.g>c.r*1.18f;
                if(energy && max-min>0.035f)
                    result.SetPixel(x,y,new Color(max,Mathf.Min(c.g,max*0.24f),max*0.92f,c.a));
            }
            result.Apply(false,false); _ihAscendedIconArt[id]=result; return result;
        }

        private void IhBlitArt(Texture2D target, Rect targetRect, Texture2D source, Rect sourceRect)
        {
            int x0=Mathf.RoundToInt(targetRect.x), y0=Mathf.RoundToInt(targetRect.y);
            int w=Mathf.RoundToInt(targetRect.width),h=Mathf.RoundToInt(targetRect.height);
            for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            {
                float u=(sourceRect.x+(x+0.5f)/w*sourceRect.width)/1011f;
                float v=1f-(sourceRect.y+(y+0.5f)/h*sourceRect.height)/662f;
                target.SetPixel(x0+x,661-y0-y,source.GetPixelBilinear(u,v));
            }
        }

        private static float IhLabelWidth(string id)
        {
            // v0.20.6: inner text width of each painted plate (plate width - 14 px), measured on the
            // chassis. The old 96-126 px boxes were wider than the plates and smeared their edges.
            switch(IhTemplateSlot(id)) {
                case "goddess_relic": return 74f;
                case "judgement_hammer": return 103f;
                case "heavens_light": return 77f;
                case "shield_charge": return 71f;
                case "fallen_angel": return 69f;
                case "ray_of_hope": return 70f;
                case "electric_smite": return 109f;
                default: return 74f;
            }
        }

        private void IhInpaintText(Texture2D target, Rect area)
        {
            int left=Mathf.RoundToInt(area.x),top=Mathf.RoundToInt(area.y);
            int width=Mathf.RoundToInt(area.width),height=Mathf.RoundToInt(area.height);
            Color[] pixels=new Color[width*height];
            float r=0,g=0,b=0; int count=0;
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
                if(x==0 || y==0 || x==width-1 || y==height-1) {
                    Color c=target.GetPixel(left+x,661-top-y); pixels[y*width+x]=c;
                    r+=c.r;g+=c.g;b+=c.b;count++;
                }
            Color paper=new Color(r/count,g/count,b/count,1f);
            for(int y=1;y<height-1;y++)for(int x=1;x<width-1;x++)pixels[y*width+x]=paper;
            // Boundary-matched fill removes lettering without stripes, pasted patches or frame edits.
            for(int iteration=0;iteration<160;iteration++)
                for(int y=1;y<height-1;y++)for(int x=1;x<width-1;x++) {
                    int i=y*width+x;Color a=pixels[i-1],c=pixels[i+1],d=pixels[i-width],e=pixels[i+width];
                    pixels[i]=new Color((a.r+c.r+d.r+e.r)*0.25f,(a.g+c.g+d.g+e.g)*0.25f,(a.b+c.b+d.b+e.b)*0.25f,1f);
                }
            for(int y=1;y<height-1;y++)for(int x=1;x<width-1;x++)target.SetPixel(left+x,661-top-y,pixels[y*width+x]);
        }

        private void IhBlankPaintedLabel(Texture2D target, Vector2 anchor, float width)
        {
            IhInpaintText(target,new Rect(anchor.x-width/2f,anchor.y-22f,width,21f));
        }

        private void IhPrepareUniversalBanner(Texture2D target)
        {
            if(target==null) return;
            // v0.20.6: only the painted name (x 600-719); the old 594-759 box also erased the gold star.
            IhInpaintText(target,new Rect(596f,92f,138f,27f));
            target.Apply(false,false);
        }

        private void IhDrawUniversalBranchLabels()
        {
            GUIStyle title=new GUIStyle(_treeHeaderStyle);
            title.alignment=TextAnchor.MiddleCenter;
            Font branchSerif=FindValheimFont("AveriaSerifLibre-Regular");
            if(branchSerif!=null) title.font=branchSerif;
            title.fontSize=Mathf.Max(12,Mathf.RoundToInt(ScaleReferenceRect(0,0,0,28).height));
            title.normal.textColor=new Color(1f,0.93f,0.77f,1f);
            // Centred between the emblem and the gold star (x 665), shrunk to fit long AC names.
            Rect titleRect=ScaleReferenceRect(598f,87f,134f,35f);
            string branchName=IhTreeBranch().ToUpperInvariant();
            while(title.fontSize>10 && title.CalcSize(new GUIContent(branchName)).x>titleRect.width) title.fontSize--;
            GUI.Label(titleRect,branchName,title);

            GUIStyle label=new GUIStyle(_treeNodeNameStyle);
            label.fontSize=Mathf.Max(8,Mathf.RoundToInt(ScaleReferenceRect(0,0,0,10.5f).height));
            label.alignment=TextAnchor.MiddleCenter;
            Font serif=FindValheimFont("AveriaSerifLibre-Regular");
            if(serif!=null) label.font=serif;
            label.fontStyle=FontStyle.Bold;
            label.normal.textColor=new Color(0.18f,0.14f,0.11f,1f);
            Player labelPlayer=Player.m_localPlayer;
            string labelClass=labelPlayer==null ? "Cleric" : GetClass(labelPlayer);
            if(labelClass!="Cleric")
            {
                // v0.22.0: the Class title is painted for Cleric only; other Classes draw it live
                // in the blanked banner (between the compass emblem and the gold star).
                GUIStyle classTitle=new GUIStyle(title);
                Rect classRect=ScaleReferenceRect(143f,87f,122f,35f);
                classTitle.fontSize=title.fontSize;
                string className=labelClass.ToUpperInvariant();
                while(classTitle.fontSize>10 && classTitle.CalcSize(new GUIContent(className)).x>classRect.width) classTitle.fontSize--;
                GUI.Label(classRect,className,classTitle);
            }
            foreach(ReferenceNodeUi node in IhTreeNodes())
            {
                // Cleric Class plates are painted on the chassis; every other Class gets live names.
                bool classSkill=IhIsAnyClassSkill(node.Id);
                if(classSkill && labelClass=="Cleric") continue;
                Vector2 anchor=GetReferenceNameplateAnchor(node);
                // Long names (Divine Intervention, Heaven's Judgement) shrink to stay inside the plate.
                float width=classSkill ? IhClassLabelWidth(IhTemplateSlot(node.Id)) : IhLabelWidth(node.Id);
                Rect r=ScaleReferenceRect(anchor.x-width/2f,anchor.y-22f,width,20f);
                string name=IhSkillName(node.Id);
                int size=Mathf.Max(8,Mathf.RoundToInt(ScaleReferenceRect(0,0,0,10.5f).height));
                label.fontSize=size;
                while(label.fontSize>7 && label.CalcSize(new GUIContent(name)).x>r.width) label.fontSize--;
                GUI.Label(r,name,label);
            }
        }

        // v0.20.4: painted opening of every frame (reference px), measured by tools/build_tree_frames.py
        // (FIELDS). Locks grey only this opening; the coloured frame band stays in colour.
        private static Rect IhFieldRect(string slot)
        {
            switch(slot)
            {
                case "lightning_zap": return Rect.MinMaxRect(175f,166f,229f,217f);
                case "righteous_strike": return Rect.MinMaxRect(175f,295f,228f,345f);
                case "holy_wave": return Rect.MinMaxRect(175f,420f,228f,469f);
                case "goddess_relic": return Rect.MinMaxRect(378f,166f,431f,217f);
                case "judgement_hammer": return Rect.MinMaxRect(379f,295f,433f,345f);
                case "heavens_light": return Rect.MinMaxRect(390f,418f,444f,469f);
                case "shield_charge": return Rect.MinMaxRect(553f,166f,605f,217f);
                case "fallen_angel": return Rect.MinMaxRect(690f,166f,743f,217f);
                case "ray_of_hope": return Rect.MinMaxRect(691f,295f,743f,345f);
                case "electric_smite": return Rect.MinMaxRect(863f,227f,952f,314f);
            }
            return new Rect(0f,0f,0f,0f);
        }

        // Frame_<slot>_<color>.png covers the field rect padded by this (FRAME_PAD in the script).
        private static Rect IhFrameSpriteRect(string slot)
        {
            Rect f=IhFieldRect(slot);
            float pad=slot==IhUltimate ? 13f : 10f;
            return Rect.MinMaxRect(f.xMin-pad,f.yMin-pad,f.xMax+pad,f.yMax+pad);
        }

        // Colour painted on the universal chassis for each slot.
        private static string IhPaintedFrameColor(string slot)
        {
            switch(slot)
            {
                case "goddess_relic": case "judgement_hammer": return "navy";
                case "ray_of_hope": return "green";
                case "electric_smite": return "maroon";
                case "heavens_light": return "gold";
            }
            return "cyan";
        }

        // Category colour of a skill: Ascended Magenta, Ascended Ultimate Red, Ultimate Maroon,
        // Signature Navy, Buff / non-damaging Green, everything else (interchangeable) Cyan.
        private string IhSkillFrameColor(ReferenceNodeUi node)
        {
            string id=node.Id;
            if(node.Kind==TreeNodeKind.Grace) return "gold";
            bool ascended=IsAscendedSkill(id);
            if(IhIsUltimate(id)) return ascended ? "red" : "maroon";
            if(ascended) return "magenta";
            return IhBaseFrameColor(node);
        }

        // Category colour without Ascension (Ascension is applied on top at draw time).
        private static string IhBaseFrameColor(ReferenceNodeUi node)
        {
            string id=node.Id;
            if(node.Kind==TreeNodeKind.Grace) return "gold";
            if(IhIsUltimate(id)) return "maroon";
            if(IhIsSignature(id)) return "navy";
            // Non-damaging skills are Green (Holy Wave; Ray of Hope = the Framework's Buff example).
            // A skill that deals direct damage is Cyan (Divine Intervention heals AND damages).
            if(id=="ray_of_hope" || id=="holy_wave" || id=="void_step") return "green";
            return "cyan";
        }

        // v0.20.6: every hotbar icon (every branch) = the hotbar frame of the skill's category colour
        // + that skill's art from its tree opening. Replaces the old mix of baked Paladin icons
        // (some were "ES"/"SC" letter placeholders) and off-centre Priest crops of tree nodes.
        private static string IhHotbarFrameFile(string color)
        {
            switch(color)
            {
                case "navy": return "Icon_goddess_relic.png";
                case "green": return "Icon_ray_of_hope.png";
                case "maroon": return "Icon_electric_smite.png";
            }
            return "Icon_shield_charge.png";
        }

        private Texture2D IhComposeHotbarIcon(Texture2D frame, Texture2D backdrop, Rect field, string name)
        {
            Texture2D icon=IhCopyTexture(frame,name);
            // Inner opening of the hotbar frame: 49x54 skill frames inset 7 / 8 px (same as
            // build_ui_assets.framed_icon), the 59x57 Grace frame (Icon_heavens_light) 9 / 10 px.
            bool grace=frame.width==59 && frame.height==57;
            int ix0=grace ? 9 : Mathf.RoundToInt(7f*frame.width/49f), iy0=grace ? 10 : Mathf.RoundToInt(8f*frame.height/54f);
            int iw=frame.width-2*ix0, ih=frame.height-2*iy0;
            // Centre crop of the tree opening with the hotbar opening's aspect: no stretching.
            float aspect=(float)iw/ih;
            float cw=Mathf.Min(field.width,field.height*aspect), ch=cw/aspect;
            float left=field.center.x-cw*0.5f, top=field.center.y-ch*0.5f;
            for(int y=0;y<ih;y++) for(int x=0;x<iw;x++)
            {
                float rx=left+(x+0.5f)/iw*cw;
                float ry=top+ch-(y+0.5f)/ih*ch;
                icon.SetPixel(ix0+x,iy0+y,backdrop.GetPixelBilinear(rx/1011f,1f-ry/662f));
            }
            icon.Apply(false,false);
            _ihPolishTextures.Add(icon);
            return icon;
        }

        private void IhComposeHotbarIcons(ReferenceNodeUi[] nodes, Texture2D backdrop)
        {
            if(backdrop==null) return;
            Dictionary<string,Texture2D> frames=new Dictionary<string,Texture2D>();
            for(int i=0;i<nodes.Length;i++)
            {
                ReferenceNodeUi node=nodes[i];
                // Heaven's Light keeps its own icon; other Graces (Heaven's Crucible...) use its gold frame.
                if(node.Kind==TreeNodeKind.Grace && node.Id==IhGrace) continue;
                string file=node.Kind==TreeNodeKind.Grace ? "Icon_heavens_light.png" : IhHotbarFrameFile(IhBaseFrameColor(node));
                Texture2D frame;
                if(!frames.TryGetValue(file,out frame)) { frame=LoadUiPng(file); frames[file]=frame; }
                if(frame==null) continue;
                Rect field=IhFieldRect(IhTemplateSlot(node.Id));
                if(field.width<=0f) continue;
                // 3 px inside the opening: keeps the frame rim and the badge tip out of the icon.
                field=new Rect(field.x+3f,field.y+3f,field.width-6f,field.height-6f);
                Texture2D old;
                if(_treeSkillIconTex.TryGetValue(node.Id,out old) && old!=null && !_ihPolishTextures.Contains(old)) Destroy(old);
                _treeSkillIconTex[node.Id]=IhComposeHotbarIcon(frame,backdrop,field,"ImmortalHeroes_Hotbar_"+node.Id);
                if(node.Id==IhAscendedClassSkill)
                {
                    if(_ihRsNormalIconTex!=null && !_ihPolishTextures.Contains(_ihRsNormalIconTex)) Destroy(_ihRsNormalIconTex);
                    _ihRsNormalIconTex=_treeSkillIconTex[node.Id];
                }
            }
            foreach(Texture2D frame in frames.Values) if(frame!=null) Destroy(frame);
        }

        private readonly Dictionary<string,Texture2D> _ihFrameSprites = new Dictionary<string,Texture2D>();

        private Texture2D IhFrameSprite(string slot, string color)
        {
            string key=slot+"_"+color;
            Texture2D texture;
            if(_ihFrameSprites.TryGetValue(key,out texture)) return texture;
            texture=LoadUiPng("Frame_"+key+".png");
            _ihFrameSprites[key]=texture;
            if(texture!=null) _ihPolishTextures.Add(texture);
            return texture;
        }

        // Recolours the frame band when the skill's category differs from the painted slot colour.
        private void IhDrawFrameOverlay(ReferenceNodeUi node)
        {
            string slot=IhTemplateSlot(node.Id);
            string color=IhSkillFrameColor(node);
            if(color!=IhPaintedFrameColor(slot))
            {
                Texture2D sprite=IhFrameSprite(slot,color);
                if(sprite!=null)
                {
                    Rect r=IhFrameSpriteRect(slot);
                    GUI.DrawTexture(IhSnap(ScaleReferenceRect(r.x,r.y,r.width,r.height)),sprite);
                }
            }
            // Ascended skills are permanent: give them the same corner badge Signatures / Ultimates
            // have painted (Goddess Relic, Judgement Hammer and Electric Smite slots already show one).
            if(color=="magenta" && slot!="goddess_relic" && slot!="judgement_hammer" && slot!=IhUltimate && _treePermanentBadgeTex!=null)
            {
                Rect f=IhFieldRect(slot);
                GUI.DrawTexture(IhSnap(ScaleReferenceRect(f.xMax-11.5f,f.yMin-17.5f,23f,23f)),_treePermanentBadgeTex);
            }
        }

        // CPU version used while building the Priest canvas, so its hotbar icons carry the same frame.
        private void IhStampFrame(Texture2D target, string slot, string color)
        {
            Texture2D sprite=IhFrameSprite(slot,color);
            if(target==null || sprite==null) return;
            Rect r=IhFrameSpriteRect(slot);
            int x0=Mathf.RoundToInt(r.x), y0=Mathf.RoundToInt(r.y);
            for(int y=0;y<sprite.height;y++) for(int x=0;x<sprite.width;x++)
            {
                Color s=sprite.GetPixel(x,sprite.height-1-y);
                if(s.a<=0.001f) continue;
                int tx=x0+x, ty=661-(y0+y);
                Color c=target.GetPixel(tx,ty);
                target.SetPixel(tx,ty,new Color(c.r+(s.r-c.r)*s.a,c.g+(s.g-c.g)*s.a,c.b+(s.b-c.b)*s.a,c.a));
            }
        }

        // Same openings on Cleric_Priest_Artwork.png (PRIEST_DONOR_FIELDS): blitted field -> field.
        private static Rect IhPriestArtworkRect(string id)
        {
            switch(id) {
                case "lightning_relic":return Rect.MinMaxRect(378,164,431,215);
                case "holy_relic":return Rect.MinMaxRect(379,294,433,344);
                case "grand_sigil":return Rect.MinMaxRect(389,416,443,467);
                case "divine_intervention":return Rect.MinMaxRect(553,166,605,217);
                case "grand_cross":return Rect.MinMaxRect(690,166,743,217);
                case "heavens_judgement":return Rect.MinMaxRect(691,294,743,344);
                default:return Rect.MinMaxRect(863,226,952,313);
            }
        }

        private void IhLoadPriestArtwork()
        {
            Texture2D source=LoadUiPng("Cleric_Priest_Artwork.png");
            if(source==null || _ihPreAdvanceBackdropTex==null)
            {
                if(source!=null) Destroy(source);
                Logger.LogWarning("Priest art missing. Re-run INSTALL.bat.");
                return;
            }
            // No cathedral, ribbon, divider, footer or perimeter pixels are replaced.
            // Priest uses the exact Paladin canvas. Only skill interiors and text change.
            _ihPriestBackdropTex=IhCopyTexture(_ihPreAdvanceBackdropTex,"ImmortalHeroes_UniversalPriest");
            for(int i=3;i<ClericPriestReferenceNodes.Length;i++)
            {
                ReferenceNodeUi node=ClericPriestReferenceNodes[i];
                // v0.20.4: the painted opening is replaced 1:1, so the art stays centred and
                // never covers the frame band.
                IhBlitArt(_ihPriestBackdropTex,IhFieldRect(IhTemplateSlot(node.Id)),source,IhPriestArtworkRect(node.Id));
                string color=IhSkillFrameColor(node), slot=IhTemplateSlot(node.Id);
                if(color!=IhPaintedFrameColor(slot) && color!="magenta" && color!="red")
                    IhStampFrame(_ihPriestBackdropTex,slot,color);
            }
            IhComposeHotbarIcons(ClericPriestReferenceNodes,_ihPriestBackdropTex);
            // The Grace slot keeps its original frame too.
            IhBlitArt(_ihPriestBackdropTex,new Rect(670,548,44,44),source,new Rect(394.8f,422.4f,44.4f,40.2f));
            _ihPriestBackdropTex.Apply(false,false);
            Destroy(source);
            _ihPriestLockedTex=IhMakeSharedLockedBackdrop(_ihPriestBackdropTex);
        }

        // =====================================================================================
        // v0.22.0 universal kit trees (Warrior / Sorcerer branches). Same chassis, slots, frames,
        // nameplates and locks as Paladin/Priest; only the skill art and text change.
        // Art: ImmortalHeroesAssets/<Class>_<AC>_Artwork.png (1011x662, nodes at the chassis
        // positions, like Cleric_Priest_Artwork). Until it exists each opening is a placeholder
        // and the skill's initials are drawn on it.
        // =====================================================================================
        private static readonly Dictionary<string, ReferenceNodeUi[]> IhKitNodeCache = new Dictionary<string, ReferenceNodeUi[]>();
        private readonly Dictionary<string, Texture2D> _ihKitBackdrops = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> _ihKitLocked = new Dictionary<string, Texture2D>();
        private readonly HashSet<string> _ihPlaceholderArt = new HashSet<string>();

        private static string IhNodeTitle(IhKit k, string id)
        {
            string prefix = id == k.Grace ? "GRACE - " : id == k.Ultimate ? "ULTIMATE - " : id == "void_step" ? "SUPPORT - " : "ATTACK - ";
            return prefix + IhSkillName(id).ToUpperInvariant();
        }

        private static ReferenceNodeUi[] IhKitNodes(IhKit k)
        {
            if (k.Ac == "Paladin") return ClericPaladinReferenceNodes;
            if (k.Ac == "Priest") return ClericPriestReferenceNodes;
            ReferenceNodeUi[] nodes;
            if (IhKitNodeCache.TryGetValue(k.Ac, out nodes)) return nodes;
            ReferenceNodeUi[] p = ClericPaladinReferenceNodes;
            nodes = new ReferenceNodeUi[p.Length];
            for (int i = 0; i < 3; i++)
            {
                string id = k.ClassSkills[i];
                bool asc = id == k.AscendedClass;
                nodes[i] = new ReferenceNodeUi(id, p[i].GroupRect, p[i].IconRect, "", asc ? TreeNodeKind.Ascended : TreeNodeKind.ClassNormal, asc, 7, IhNodeTitle(k, id), "");
            }
            // Same order as the Paladin array: Signature, Signature, Grace, Lv24, Lv32, Lv32, Ultimate.
            string[] ids = { k.Adv[0], k.Adv[1], k.Grace, k.Adv[2], k.Adv[3], k.Adv[4], k.Ultimate };
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                ReferenceNodeUi src = p[i + 3];
                TreeNodeKind kind = id == k.Grace ? TreeNodeKind.Grace : id == k.Ultimate ? TreeNodeKind.Ultimate
                    : IhContains(k.Signatures, id) ? TreeNodeKind.Signature : id == "void_step" ? TreeNodeKind.Buff : TreeNodeKind.AdvancementNormal;
                bool mandatory = kind == TreeNodeKind.Grace || kind == TreeNodeKind.Ultimate || kind == TreeNodeKind.Signature;
                int maxTier = kind == TreeNodeKind.Grace ? 0 : kind == TreeNodeKind.Ultimate ? 3 : 5;
                nodes[i + 3] = new ReferenceNodeUi(id, src.GroupRect, src.IconRect, "", kind, mandatory, maxTier, IhNodeTitle(k, id), "");
            }
            IhKitNodeCache[k.Ac] = nodes;
            return nodes;
        }

        // Painted Class-plate text widths (plate - 14 px), measured like the AC plates.
        private static float IhClassLabelWidth(string slot)
        {
            return slot == "righteous_strike" ? 80f : slot == "holy_wave" ? 74f : 77f;
        }

        private Texture2D IhKitBackdrop(IhKit k, bool locked)
        {
            if (k == null) return null;
            if (k.Ac == "Priest") return locked ? _ihPriestLockedTex : _ihPriestBackdropTex;
            if (k.Ac == "Paladin") return locked ? _ihLockedBackdropTex : (_ihPreAdvanceBackdropTex != null ? _ihPreAdvanceBackdropTex : _treeReferenceBackdropTex);
            if (!_ihKitBackdrops.ContainsKey(k.Ac))
                IhBuildKitCanvas(k);
            Texture2D texture;
            return (locked ? _ihKitLocked : _ihKitBackdrops).TryGetValue(k.Ac, out texture) ? texture : null;
        }

        private void IhFillPlaceholder(Texture2D target, Rect field)
        {
            int x0 = Mathf.RoundToInt(field.x), y0 = Mathf.RoundToInt(field.y);
            int w = Mathf.RoundToInt(field.width), h = Mathf.RoundToInt(field.height);
            Color inner = new Color(0.20f, 0.18f, 0.16f, 1f), outer = new Color(0.09f, 0.08f, 0.07f, 1f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / w - 0.5f, dy = (y + 0.5f) / h - 0.5f;
                    float t = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 1.6f);
                    target.SetPixel(x0 + x, 661 - y0 - y, Color.Lerp(inner, outer, t));
                }
        }

        private void IhBuildKitCanvas(IhKit k)
        {
            _ihKitBackdrops[k.Ac] = null;
            if (_ihPreAdvanceBackdropTex == null) return;
            Texture2D canvas = IhCopyTexture(_ihPreAdvanceBackdropTex, "ImmortalHeroes_Universal_" + k.Ac);
            // The chassis carries the Cleric title and Class plate text: blank them, live text is drawn.
            IhInpaintText(canvas, new Rect(145f, 91f, 118f, 25f));
            for (int i = 0; i < 3; i++)
                IhBlankPaintedLabel(canvas, ReferenceNameplateAnchors[IhClassSlots[i]], IhClassSlots[i] == "righteous_strike" ? 86f : IhClassLabelWidth(IhClassSlots[i]));
            Texture2D art = LoadUiPng(k.Class + "_" + k.Ac.Replace(" ", "") + "_Artwork.png");
            bool hasArt = art != null && art.width == 1011 && art.height == 662;
            ReferenceNodeUi[] nodes = IhKitNodes(k);
            for (int i = 0; i < nodes.Length; i++)
            {
                ReferenceNodeUi node = nodes[i];
                string slot = IhTemplateSlot(node.Id);
                Rect field = IhFieldRect(slot);
                if (hasArt) { IhBlitArt(canvas, field, art, field); _ihPlaceholderArt.Remove(node.Id); }
                else { IhFillPlaceholder(canvas, field); _ihPlaceholderArt.Add(node.Id); }
                string color = IhBaseFrameColor(node);
                if (color != IhPaintedFrameColor(slot))
                    IhStampFrame(canvas, slot, color);
            }
            // Grace box in the footer.
            Rect graceBox = new Rect(670f, 548f, 44f, 44f);
            if (hasArt) IhBlitArt(canvas, graceBox, art, graceBox); else IhFillPlaceholder(canvas, graceBox);
            canvas.Apply(false, false);
            if (art != null) Destroy(art);
            IhComposeHotbarIcons(nodes, canvas);
            _ihKitBackdrops[k.Ac] = canvas;
            _ihKitLocked[k.Ac] = IhMakeSharedLockedBackdrop(canvas);
        }

        // Skill initials on a placeholder opening (tree, hotbar, HUD) until the painting arrives.
        private void IhDrawPlaceholderInitials(Rect rect, string id, bool dim)
        {
            if (string.IsNullOrEmpty(id) || !_ihPlaceholderArt.Contains(id)) return;
            IhEnsureTreeStyles();
            GUIStyle style = new GUIStyle(_ihHeaderStyle);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = Mathf.Max(9, Mathf.RoundToInt(rect.height * 0.36f));
            string text = GetSkillInitials(IhSkillName(id));
            Color old = GUI.color;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = dim ? new Color(0.62f, 0.60f, 0.56f, 1f) : new Color(1f, 0.90f, 0.66f, 1f);
            GUI.Label(rect, text, style);
            GUI.color = old;
        }

        private static string IhClassBlessingTitle(string cls)
        {
            return cls == "Warrior" ? "WARRIOR'S BLESSING" : cls == "Sorcerer" ? "WARLOCK - SORCERER'S BLESSING" : "CLERIC'S BLESSING";
        }

        private static string IhClassBlessingText(string cls)
        {
            if (cls == "Warrior")
                return "Hyper Armor against any hit below 30% of your Total HP. Parry strength x2. +20 Run and +20 Jump skill.";
            if (cls == "Sorcerer")
                return "Creature melee damage -70% (mining and woodcutting are not affected). +65 Max Eitr, +35% Eitr Regen, Eitr starts regenerating twice as fast. Cannot Block, Parry or equip Shields.";
            return "All Shields: 1.5x Block Force + Block Armor. Staff + Shield allowed. No movement penalty from Shields, Staves, or one-handed Club-skill weapons. +35 Max HP and +20% HP Regen.";
        }

        private static string IhMasteryTitle(string ac)
        {
            switch (ac)
            {
                case "Sword Master": return "THE WAY OF THE SWORD - MASTERY";
                case "Mercenary": return "WARFREAK - MASTERY";
                case "Wizard": return "ARCHMAGE - MASTERY";
                case "Spellcaster": return "YIN AND YANG - MASTERY";
            }
            return ac.ToUpperInvariant() + " - MASTERY";
        }

        private static string IhMasteryText(string ac)
        {
            switch (ac)
            {
                case "Sword Master": return "+20 Sword (effective cap 100), +50% Sword Attack Speed, no Sword movement penalty. Blocking or Dodging stops the rest of a Sword Master skill.";
                case "Mercenary": return "Dual-wield any two one-handed physical weapons. +10 Sword, Axe and Clubs (cap 100). +50% Attack Speed with two one-handed or a two-handed physical weapon. No physical weapon movement penalty. +30% Armor and stronger aggro. Unchained Fury: +1 Fury per melee hit, +3 per enemy hit by a skill; at 100 it triggers for 20s (3 min lockout).";
                case "Wizard": return "Charged Staff attacks (Mouse2 + Mouse1): up to 3 stacks, 2s each, 1 Eitr per 0.1s. Stack 1 doubles the size, Stacks 2-3 add damage. Overcharge: after 300 Eitr spent, 12s of +40% wind-up speed, +40% Eitr Regen and +40% Magic Damage.";
                case "Spellcaster": return "Staff / Wand attack interval -50%, Eitr use -50%, +20% Eitr Regen, normal Staff / Wand damage -50%. No skill wind-ups, no Staff / Wand movement penalty. Dual Gun Staves fire together and are 100% accurate.";
            }
            return "";
        }

        private void IhPrepareSharedNameplates()
        {
            // Prepare the chassis ONCE, before either branch is created.
            // Both branches retain exactly the same plaque pixels and live text styling.
            if(_ihPreAdvanceBackdropTex==null) return;
            for(int i=3;i<ClericPaladinReferenceNodes.Length;i++) {
                ReferenceNodeUi node=ClericPaladinReferenceNodes[i];
                IhBlankPaintedLabel(_ihPreAdvanceBackdropTex,GetReferenceNameplateAnchor(node),IhLabelWidth(node.Id));
            }
            _ihPreAdvanceBackdropTex.Apply(false,false);
            if(_ihLockedBackdropTex!=null) Destroy(_ihLockedBackdropTex);
            _ihLockedBackdropTex=IhMakeSharedLockedBackdrop(_ihPreAdvanceBackdropTex);
        }

        private Texture2D IhMakeSharedLockedBackdrop(Texture2D backdrop)
        {
            Texture2D result=IhCopyTexture(backdrop,"ImmortalHeroes_SharedLockedState");
            // v0.20.4: grey only the art inside each frame opening (Tree_LockMask.png alpha);
            // frame bands, corner ornaments and badges keep their colour.
            Texture2D mask=LoadUiPng("Tree_LockMask.png");
            for(int i=0;i<=ClericPaladinReferenceNodes.Length;i++) {
                Rect area=i==ClericPaladinReferenceNodes.Length ? new Rect(670,548,44,44)
                    : IhFieldRect(ClericPaladinReferenceNodes[i].Id);
                for(int y=Mathf.RoundToInt(area.y);y<Mathf.RoundToInt(area.yMax);y++)
                    for(int x=Mathf.RoundToInt(area.x);x<Mathf.RoundToInt(area.xMax);x++) {
                        float weight=mask!=null && mask.width==1011 && mask.height==662 ? mask.GetPixel(x,661-y).a : 1f;
                        if(weight<=0.001f) continue;
                        Color c=result.GetPixel(x,661-y);
                        float grey=(c.r*0.299f+c.g*0.587f+c.b*0.114f)*0.72f;
                        result.SetPixel(x,661-y,new Color(c.r+(grey-c.r)*weight,c.g+(grey-c.g)*weight,c.b+(grey-c.b)*weight,c.a));
                    }
            }
            if(mask!=null) Destroy(mask);
            result.Apply(false,false);return result;
        }

        // v0.21.0: one "Ascended" line per Ascended Priest skill (header ASCENDED - <SKILL>).
        private static string IhPriestAscendedSummary(string id)
        {
            switch (id)
            {
                case "holy_wave": return "no wind up, aim at an ally to cast it on them; 10m, Sanctifies allies, double instant heal below 30% HP, 5m echo wave after 2s at half healing";
                case "lightning_relic": return "14m pulses arc to 3 more enemies and Sanctify allies; the Cross detonates (8m, Stuns Small) when it ends";
                case "holy_relic": return "14m, +30% buffs, pulses cleanse Burn / Poison / Frost and Sanctify; final 25% Max HP heal when it ends";
                case "divine_intervention": return "with both Relics up, a Cross Cast fires from both; 250 HP Barrier; enemies are pulled inward";
                case "grand_cross": return "20m wide, travels 35m, ends in an 8m holy burst that Stuns Small and Big";
                case "heavens_judgement": return "14m circle, 3s barrage, beams heal allies 3% Max HP, ends with a Pillar of Heaven";
                case "lightning_tempest": return "follows you, 12m; allies inside get +20% Defense and Hyper Armor; ends by detonating every Zap";
            }
            return "";
        }

        private string IhBuildPriestTooltip(Player player, ReferenceNodeUi node, out string title)
        {
            title = IsAscendedSkill(node.Id) ? "ASCENDED - " + IhSkillName(node.Id).ToUpperInvariant() : node.TooltipTitle;
            string id = node.Id;
            float power = IhSkillPower(player,id);
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            string lore = "";
            switch(id)
            {
                case "lightning_relic": lore = "Plant a Cross of lightning. Its pulses punish nearby foes; place Holy Relic beside it to consecrate the ground."; break;
                case "holy_relic": lore = "Raise a sacred Cross that restores your allies and strengthens everyone within its light."; break;
                case "divine_intervention": lore = "Answer danger with a burst of holy power, restoring allies and exposing enemies around you or an aimed Relic."; break;
                case "grand_cross": lore = "Carve a radiant X through the battlefield. Its crossing blades travel forward, burning every foe they touch."; break;
                case "heavens_judgement": lore = "Call a barrage of holy beams around yourself or an aimed Relic, chilling the enemies caught beneath them."; break;
                case "lightning_tempest": lore = "Unleash a restless storm, layering lightning and afflictions across the battlefield."; break;
                case "grand_sigil": lore = "Wrap yourself and nearby allies in a holy Barrier that holds until it breaks."; break;
            }
            b.Append(IhWhite(lore)+"\n\n");
            int max = IhMaxTier(id);
            if(max > 0)
            {
                int tier = IhGetTier(player,id), pending = GetPrototypePending(id);
                string value = tier.ToString()+"/"+max.ToString();
                if(pending>0) value += "  →  "+(tier+pending).ToString()+"/"+max.ToString()+" (pending)";
                float pct = _ihTierPowerPercent == null ? 10f : _ihTierPowerPercent.Value;
                b.Append(IhLine("Tier",value+"   +"+IhNum(pct*(tier+pending))+"% "+IhTierBonusLabel(id)));
                if(IhIsUltimate(id)) b.Append(IhLine("Tier Up","automatic at Lv 40, 44, 48"));
            }
            string reason;
            if(!IhIsUnlocked(player,id,out reason)) b.Append(IhLine("Requires",reason));
            switch(id)
            {
                case "lightning_relic":
                    b.Append(IhLine("Damage",IhDamage(_lightningRelicDamage,power)+" per pulse"));
                    b.Append(IhLine("Radius",IhNum(_lightningRelicRadius.Value)+"m"));
                    b.Append(IhLine("Range",IhNum(_lightningRelicRange.Value)+"m"));
                    b.Append(IhLine("Duration",IhNum(_lightningRelicDuration.Value)+"s"));
                    b.Append(IhLine("Pulse",IhNum(_lightningRelicInterval.Value)+"s"));
                    b.Append(IhLine("Inflicts","Cripple"));
                    b.Append(IhLine("Recast","Relinquish the Relic; Cooldown starts when it ends"));
                    b.Append(IhLine("Consecrated Ground","Place both Relics within "+IhNum(_consecratedConnectRange.Value)+"m; "+IhNum(_consecratedRadius.Value)+"m field"));
                    IhCosts(b,_lightningRelicStamina.Value,"1.5s",_lightningRelicCooldown.Value);
                    break;
                case "holy_relic":
                    b.Append(IhLine("Healing",IhNum(_holyRelicHealPercent.Value*power)+"% Total HP per pulse"));
                    b.Append(IhLine("Radius",IhNum(_holyRelicRadius.Value)+"m"));
                    b.Append(IhLine("Range",IhNum(_holyRelicRange.Value)+"m"));
                    b.Append(IhLine("Duration",IhNum(_holyRelicDuration.Value)+"s"));
                    b.Append(IhLine("Pulse",IhNum(_holyRelicInterval.Value)+"s"));
                    b.Append(IhLine("Buff",IhNum(_holyRelicDamageBuff.Value)+"% Attack Damage, "+IhNum(_holyRelicAttackSpeedBuff.Value)+"% Attack Speed"));
                    b.Append(IhLine("Buff",IhNum(_holyRelicMoveSpeedBuff.Value)+"% Movement Speed, "+IhNum(_holyRelicDefenseBuff.Value)+"% Overall Defense"));
                    b.Append(IhLine("Buff Duration",IhNum(_holyRelicBuffDuration.Value)+"s, refreshed by pulses"));
                    b.Append(IhLine("Recast","Relinquish the Relic; Cooldown starts when it ends"));
                    IhCosts(b,_holyRelicStamina.Value,"1s",_holyRelicCooldown.Value);
                    break;
                case "divine_intervention":
                    b.Append(IhLine("Damage",IhDamage(_interventionDamage,power)));
                    b.Append(IhLine("Healing",IhNum(Mathf.Lerp(_interventionHealPercent.Value,_diHealAtMax.Value,Mathf.Clamp01(IhGetTier(player,id)/5f)))+"% Total HP ("+IhNum(_diHealAtMax.Value)+"% at Tier 5)"));
                    b.Append(IhLine("Barrier",IhNum(_interventionBarrierHp.Value)+" HP"));
                    b.Append(IhLine("Radius",IhNum(_interventionRadius.Value)+"m"));
                    b.Append(IhLine("Cross Cast Range",IhNum(_interventionRange.Value)+"m"));
                    b.Append(IhLine("Support",IhNum(_interventionBuffDuration.Value)+"s, Hyper Armor"));
                    b.Append(IhLine("Inflicts","Expose, "+IhNum(_interventionExposeDuration.Value)+"s"));
                    IhCosts(b,_interventionStamina.Value,IhNum(_interventionWindup.Value)+"s",_interventionCooldown.Value);
                    break;
                case "grand_cross":
                    b.Append(IhLine("Damage",IhDamage(_grandCrossDamage,power)+" every "+IhNum(_grandCrossTickInterval.Value)+"s"));
                    b.Append(IhLine("Width",IhNum(_grandCrossWidth.Value)+"m"));
                    b.Append(IhLine("Range",IhNum(_grandCrossRange.Value)+"m"));
                    b.Append(IhLine("Travel Time",IhNum(_grandCrossTravelTime.Value)+"s"));
                    b.Append(IhLine("Inflicts","Spirit Burn, "+IhNum(_grandCrossSpiritDuration.Value)+"s"));
                    b.Append(IhLine("Cast","Self only; cannot Cross Cast"));
                    IhCosts(b,_grandCrossStamina.Value,IhNum(_grandCrossWindup.Value)+"s",_grandCrossCooldown.Value);
                    break;
                case "heavens_judgement":
                    b.Append(IhLine("Damage",IhDamage(_heavensDamage,power)+" per beam"));
                    b.Append(IhLine("Radius",IhNum(_heavensRadius.Value)+"m"));
                    b.Append(IhLine("Barrage",IhNum(_heavensDuration.Value)+"s"));
                    b.Append(IhLine("Beam Interval",IhNum(_heavensStrikeInterval.Value)+"s"));
                    b.Append(IhLine("Inflicts","Frost, "+IhNum(_heavensFrostDuration.Value)+"s"));
                    b.Append(IhLine("Cast","Self or aimed active Relic"));
                    IhCosts(b,_heavensStamina.Value,IhNum(_heavensWindup.Value)+"s",_heavensCooldown.Value);
                    break;
                case "lightning_tempest":
                    b.Append(IhLine("Damage",IhDamage(_tempestDamage,power)+" per strike"));
                    b.Append(IhLine("Radius",IhNum(_tempestRadius.Value)+"m"));
                    b.Append(IhLine("Range",IhNum(_tempestRange.Value)+"m"));
                    b.Append(IhLine("Duration",IhNum(_tempestDuration.Value)+"s"));
                    b.Append(IhLine("Strikes","up to "+_tempestMaxStrikes.Value.ToString()+" simultaneous"));
                    b.Append(IhLine("Inflicts","Zap, Frost, Fire Burn, Spirit Burn, Expose"));
                    IhCosts(b,_tempestStamina.Value,"1s",_tempestCooldown.Value);
                    break;
                case "grand_sigil":
                    // Same lines and order as Heaven's Light (the other Cleric Grace).
                    b.Append(IhLine("Barrier",IhNum(_grandBarrierHp.Value)+" HP each, until broken"));
                    b.Append(IhLine("Barrier Armor",IhNum(_crucibleArmorPercent.Value)+"% of your Armor (snapshot)"));
                    b.Append(IhLine("Radius",IhNum(_grandRadius.Value)+"m"));
                    b.Append(IhLine("Duration",IhNum(_grandBarrierDuration.Value)+"s"));
                    b.Append(IhLine("Cost",_grandStamina.Value>0f ? IhNum(_grandStamina.Value)+" Stamina" : "None"));
                    b.Append(IhLine("Wind Up Time",IhNum(_grandWindup.Value)+"s"));
                    b.Append(IhLine("Cooldown",IhNum(_grandCooldown.Value/60f)+" min"));
                    b.Append(IhLine("Key",FormatHotbarBinding(BindGrace)));
                    break;
            }
            if(IsAscendedSkill(id)) b.Append(IhLine("Ascended",IhPriestAscendedSummary(id)));
            return b.ToString().TrimEnd('\n');
        }

        private void DrawReferenceNode(ReferenceNodeUi node)
        {
            Player player = Player.m_localPlayer;
            Rect group = ScaleReferenceRect(node.GroupRect.x, node.GroupRect.y, node.GroupRect.width, node.GroupRect.height);
            Rect icon = ScaleReferenceRect(node.IconRect.x, node.IconRect.y, node.IconRect.width, node.IconRect.height);

            string lockReason;
            bool unlocked = IhIsUnlocked(player, node.Id, out lockReason);
            int maxTier = IhMaxTier(node.Id);
            int currentTier = GetPrototypeTier(node.Id);
            int pendingTier = GetPrototypePending(node.Id);

            // v0.18.0: locked = dimmed icon + padlock + requirement; unlocked = Tier stars + count.
            Rect row = new Rect();
            if (unlocked)
                IhDrawFrameOverlay(node);
            if (!unlocked)
                IhDrawLockedNode(node, lockReason);
            else if (maxTier > 0)
                IhDrawTierRow(node, currentTier, pendingTier, out row);
            Rect opening = IhFieldRect(IhTemplateSlot(node.Id));
            IhDrawPlaceholderInitials(ScaleReferenceRect(opening.x, opening.y, opening.width, opening.height), node.Id, !unlocked);

            if (group.Contains(Event.current.mousePosition))
            {
                string title;
                _treeHoveredBody = IhBuildTooltip(player, node, out title);
                _treeHoveredTitle = title;
            }

            // v0.16.0: press on the icon starts a click (select) or, once moved, a drag to the hotbar.
            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && icon.Contains(ev.mousePosition))
            {
                BeginTreePress(node.Id, -1, ev.mousePosition);
                ev.Use();
            }

            if (_treeSelectedNodeId != node.Id || !unlocked || maxTier <= 0)
                return;

            // v0.18.0: - and + flank the Tier stars of the selected skill.
            bool canAdd = currentTier + pendingTier < maxTier && IhCanQueueTier(player, node.Id);
            bool canRemove = pendingTier > 0;
            const float buttonRef = 16f;
            float buttonY = row.y + row.height * 0.5f - buttonRef * 0.5f;
            if (canRemove)
            {
                Rect minusRect = ScaleReferenceRect(row.x - buttonRef - 3f, buttonY, buttonRef, buttonRef);
                DrawTierQueueButton(minusRect, "-");
                if (GUI.Button(minusRect, GUIContent.none, GUIStyle.none))
                    RemovePrototypePending(node.Id);
            }
            if (canAdd)
            {
                Rect plusRect = ScaleReferenceRect(row.xMax + 2f, buttonY, buttonRef, buttonRef);
                DrawTierQueueButton(plusRect, "+");
                if (GUI.Button(plusRect, GUIContent.none, GUIStyle.none))
                    AddPrototypePending(node.Id, maxTier);
            }
        }

        // Nameplate anchors measured from the approved artwork (reference px):
        // x = nameplate center, y = nameplate bottom edge.
        private static readonly Dictionary<string, Vector2> ReferenceNameplateAnchors = new Dictionary<string, Vector2>
        {
            // v0.20.6: x = measured plate/frame centre (SC, FA, RoH were 3-4 px right, ES 5 px left).
            { "lightning_zap", new Vector2(201.5f, 247f) },
            { "righteous_strike", new Vector2(201f, 375f) },
            { "holy_wave", new Vector2(201f, 499f) },
            { "goddess_relic", new Vector2(404f, 247f) },
            { "judgement_hammer", new Vector2(405f, 375f) },
            { "heavens_light", new Vector2(416f, 499f) },
            { "shield_charge", new Vector2(579.5f, 247f) },
            { "fallen_angel", new Vector2(716.5f, 247f) },
            { "ray_of_hope", new Vector2(716f, 375f) },
            { "electric_smite", new Vector2(907.5f, 355f) }
        };

        private static Vector2 GetReferenceNameplateAnchor(ReferenceNodeUi node)
        {
            Vector2 anchor;
            if (ReferenceNameplateAnchors.TryGetValue(IhTemplateSlot(node.Id), out anchor))
                return anchor;
            return new Vector2(node.GroupRect.center.x, node.GroupRect.yMax);
        }

        private ReferenceNodeUi FindReferenceNode(string id)
        {
            for (int i = 0; i < ClericPaladinReferenceNodes.Length; i++)
                if (ClericPaladinReferenceNodes[i].Id == id) return ClericPaladinReferenceNodes[i];
            for (int i = 3; i < ClericPriestReferenceNodes.Length; i++)
                if (ClericPriestReferenceNodes[i].Id == id) return ClericPriestReferenceNodes[i];
            for (int k = 2; k < IhKits.Length; k++)
            {
                ReferenceNodeUi[] nodes = IhKitNodes(IhKits[k]);
                for (int i = 0; i < nodes.Length; i++)
                    if (nodes[i].Id == id) return nodes[i];
            }
            return null;
        }

        private bool IsPermanentHotbarSkill(string id)
        {
            // v0.22.0: the kit's Ascended Class skill is permanent after Advancement (before it, every
            // Class skill is interchangeable); Signatures and the Ultimate are permanent once unlocked.
            Player player = Player.m_localPlayer;
            if (IhIsAnyClassSkill(id))
            {
                IhKit kit = IhPlayerKit(player);
                return kit != null && kit.AscendedClass == id && IhIsUnlocked(player, id);
            }
            ReferenceNodeUi node = FindReferenceNode(id);
            if (node == null || !node.Mandatory || node.Kind == TreeNodeKind.Grace)
                return false;
            return IhIsUnlocked(player, id);
        }

        private bool CanSlotSkill(string id)
        {
            ReferenceNodeUi node = FindReferenceNode(id);
            if (node == null || node.Kind == TreeNodeKind.Grace)
                return false;
            // v0.18.0: unlocked skills are usable at Tier 0, so any unlocked skill can be slotted.
            return IhIsUnlocked(Player.m_localPlayer, id);
        }

        private string[] GetHotbarLayout()
        {
            Player player = Player.m_localPlayer;
            string owner = player == null ? "" : player.GetInstanceID().ToString() + "|" + GetClass(player) + "|" + GetAdvancement(player) + "|" +
                IhGetLevel(player).ToString() + "|" + ReadPlayerData(player, IhTiersKey) + "|" + (_ihUnlockAll != null && _ihUnlockAll.Value ? "1" : "0");
            if (_hotbarLayout != null && owner == _hotbarLayoutOwnerKey)
                return _hotbarLayout;

            _hotbarLayoutOwnerKey = owner;
            _hotbarLayout = SanitizeHotbarLayout(LoadHotbarLayout(player));
            return _hotbarLayout;
        }

        // v0.18.0: locked skills leave the hotbar; unlocked permanent skills are always on it.
        private string[] SanitizeHotbarLayout(string[] layout)
        {
            for (int i = 0; i < layout.Length; i++)
            {
                if (!string.IsNullOrEmpty(layout[i]) && !CanSlotSkill(layout[i]))
                    layout[i] = "";
            }
            ReferenceNodeUi[] nodes = IhTreeNodes();
            for (int n = 0; n < nodes.Length; n++)
            {
                string id = nodes[n].Id;
                if (!IsPermanentHotbarSkill(id) || Array.IndexOf(layout, id) >= 0)
                    continue;
                int empty = Array.IndexOf(layout, "");
                if (empty < 0)
                {
                    for (int i = layout.Length - 1; i >= 0; i--)
                    {
                        if (!IsPermanentHotbarSkill(layout[i]))
                        {
                            empty = i;
                            break;
                        }
                    }
                }
                if (empty >= 0)
                    layout[empty] = id;
            }
            return layout;
        }

        private string[] LoadHotbarLayout(Player player)
        {
            // v0.18.1: before Advancement the bar starts with the three Class skills.
            IhKit kit = IhPlayerKit(player);
            string[] classSkills = IhPlayerClassSkills(player);
            string[] defaults = IhIsPaladin(player)
                ? (string[])DefaultClericPaladinHotbar.Clone()
                : kit != null
                ? new string[] { kit.AscendedClass, kit.Adv[0], kit.Adv[1], kit.Adv[2], kit.Adv[3], kit.Adv[4], kit.Ultimate }
                : classSkills.Length == 3
                ? new string[] { classSkills[0], classSkills[1], classSkills[2], "", "", "", "" }
                : new string[] { "", "", "", "", "", "", "" };
            if (player == null)
                return defaults;

            string saved = ReadPlayerData(player, IhHotbarSaveKey(player));
            if (string.IsNullOrEmpty(saved))
                return defaults;

            string[] parts = saved.Split(',');
            if (parts.Length != defaults.Length)
                return defaults;

            string[] loaded = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string id = parts[i].Trim();
                bool valid = id.Length > 0 && FindReferenceNode(id) != null && Array.IndexOf(loaded, id) < 0;
                loaded[i] = valid ? id : "";
            }

            // Missing permanent skills are added back by SanitizeHotbarLayout.
            return loaded;
        }

        private void SaveHotbarLayout()
        {
            Player player = Player.m_localPlayer;
            if (player == null || _hotbarLayout == null)
                return;

            IDictionary data = GetCustomData(player);
            if (data == null)
                return;

            data[IhHotbarSaveKey(player)] = string.Join(",", _hotbarLayout);
        }

        // Cleric keeps its original keys (by Advancement); other Classes add the Class name so a
        // pre-Advancement bar never collides between Classes.
        private string IhHotbarSaveKey(Player player)
        {
            string cls = GetClass(player);
            return HotbarLayoutKeyPrefix + (cls == "Cleric" ? GetAdvancement(player) : cls + "." + GetAdvancement(player));
        }

        private Rect HotbarSlotRect(int slot)
        {
            return ScaleReferenceRect(HotbarSlotCenters[slot] - 24f, 551f, 49f, 54f);
        }

        private int GetHotbarSlotAt(Vector2 position)
        {
            for (int i = 0; i < HotbarSlotCenters.Length; i++)
            {
                if (HotbarSlotRect(i).Contains(position))
                    return i;
            }
            return -1;
        }

        private void DrawReferenceHotbarSlots()
        {
            string[] layout = GetHotbarLayout();
            Event e = Event.current;
            int hoverSlot = GetHotbarSlotAt(e.mousePosition);

            for (int i = 0; i < layout.Length; i++)
            {
                Rect r = HotbarSlotRect(i);
                string id = layout[i];
                bool empty = string.IsNullOrEmpty(id);
                bool draggingFromHere = _dragActive && _pressSlot == i;

                Texture2D tex = null;
                if (!empty && !draggingFromHere)
                    tex = GetSkillIconTex(id);
                if (tex == null)
                    tex = _treeSlotEmptyTex;

                if (tex != null)
                {
                    GUI.color = _dragActive && hoverSlot == i ? new Color(1f, 0.92f, 0.70f, 1f) : Color.white;
                    GUI.DrawTexture(r, tex);
                    GUI.color = Color.white;
                }
                if (!empty && !draggingFromHere)
                    IhDrawPlaceholderInitials(new Rect(r.x + r.width * 0.14f, r.y + r.height * 0.15f, r.width * 0.72f, r.height * 0.70f), id, false);

                // v0.16.1: same badge as the tree marks skills that can't leave the hotbar.
                if (!empty && !draggingFromHere && IsPermanentHotbarSkill(id))
                    DrawPermanentBadge(r);

                if (!_dragActive && hoverSlot == i && !empty)
                {
                    ReferenceNodeUi node = FindReferenceNode(id);
                    _treeHoveredTitle = node != null ? IhSkillTitle(id, node, IsAscendedSkill(id)) : id;
                    if (node != null) _treeHoveredBody = IhBuildTooltip(Player.m_localPlayer,node,out _treeHoveredTitle);
                }

                if (!empty && e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
                {
                    BeginTreePress(id, i, e.mousePosition);
                    e.Use();
                }
            }
        }

        private Texture2D GetSkillIconTex(string id)
        {
            Texture2D texture;
            if(id==IhAscendedClassSkill && _ihRsNormalIconTex!=null) texture=_ihRsNormalIconTex;
            else if(id==IhGrace) texture=_ihGraceIconTex;
            else if(!_treeSkillIconTex.TryGetValue(id,out texture)) return null;
            return IsAscendedSkill(id) ? IhAscendedArt(id,texture) : texture;
        }

        private void DrawPermanentBadge(Rect slotRect)
        {
            if (_treePermanentBadgeTex == null)
                return;

            // Sits over the slot's top-right frame corner, like the badge on the tree node.
            float size = slotRect.width * 0.39f;
            Rect badge = new Rect(slotRect.xMax - size * 0.58f, slotRect.y - size * 0.21f, size, size);
            GUI.DrawTexture(badge, _treePermanentBadgeTex);
        }

        private void BeginTreePress(string skillId, int slot, Vector2 position)
        {
            _pressSkillId = skillId;
            _pressSlot = slot;
            _pressPos = position;
            _dragActive = false;
            _pressReleasedFrame = 0;
        }

        private void ClearTreePress()
        {
            _pressSkillId = "";
            _pressSlot = -1;
            _dragActive = false;
            _pressReleasedFrame = 0;
        }

        private void HandleTreePointer()
        {
            if (string.IsNullOrEmpty(_pressSkillId))
                return;

            Event e = Event.current;
            if (e.type == EventType.MouseDrag)
            {
                if (!_dragActive && (e.mousePosition - _pressPos).sqrMagnitude > 36f && (_pressSlot >= 0 || CanSlotSkill(_pressSkillId)))
                    _dragActive = true;
                if (_dragActive)
                    e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (_dragActive)
                    DropTreeDrag(GetHotbarSlotAt(e.mousePosition));
                else if (_pressSlot < 0)
                    _treeSelectedNodeId = _pressSkillId;

                ClearTreePress();
                e.Use();
                return;
            }

            if (_dragActive && e.type == EventType.Repaint)
            {
                Texture2D tex = GetSkillIconTex(_pressSkillId);
                if (tex != null)
                {
                    Rect slot = HotbarSlotRect(0);
                    Rect ghost = new Rect(e.mousePosition.x - slot.width * 0.5f, e.mousePosition.y - slot.height * 0.5f, slot.width, slot.height);
                    GUI.color = new Color(1f, 1f, 1f, 0.85f);
                    GUI.DrawTexture(ghost, tex);
                    if (IsPermanentHotbarSkill(_pressSkillId))
                        DrawPermanentBadge(ghost);
                    GUI.color = Color.white;
                }
            }
        }

        private void UpdateTreePressRelease()
        {
            // Safety net for a release outside the Skill Tree window (IMGUI never sees that MouseUp).
            // Wait two frames so a normal in-window MouseUp is always handled by OnGUI first.
            if (string.IsNullOrEmpty(_pressSkillId) || Input.GetMouseButton(0))
            {
                _pressReleasedFrame = 0;
                return;
            }

            if (_pressReleasedFrame == 0)
            {
                _pressReleasedFrame = Time.frameCount;
                return;
            }

            if (Time.frameCount - _pressReleasedFrame < 2)
                return;

            if (_dragActive)
                DropTreeDrag(-1);
            ClearTreePress();
        }

        private void DropTreeDrag(int target)
        {
            string[] layout = GetHotbarLayout();
            string id = _pressSkillId;
            int from = _pressSlot >= 0 ? _pressSlot : Array.IndexOf(layout, id);

            if (from >= 0)
            {
                // Already on the hotbar: dropping on a slot swaps; dragging a slot off the hotbar removes it.
                if (target >= 0)
                {
                    if (target != from)
                    {
                        string swap = layout[target];
                        layout[target] = layout[from];
                        layout[from] = swap;
                        SaveHotbarLayout();
                    }
                    return;
                }

                if (_pressSlot >= 0)
                {
                    if (IsPermanentHotbarSkill(id))
                    {
                        ShowMessage("Signature, Ascended and Ultimate skills stay on the hotbar");
                        return;
                    }
                    layout[from] = "";
                    SaveHotbarLayout();
                }
                return;
            }

            if (target < 0)
                return;

            string occupant = layout[target];
            if (string.IsNullOrEmpty(occupant) || !IsPermanentHotbarSkill(occupant))
            {
                // Empty or removable skill: place / replace.
                layout[target] = id;
                SaveHotbarLayout();
                return;
            }

            // Permanent skill there: insert at this slot and push skills right into the nearest empty slot.
            int emptySlot = -1;
            for (int i = target + 1; i < layout.Length; i++)
            {
                if (string.IsNullOrEmpty(layout[i]))
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot >= 0)
            {
                for (int i = emptySlot; i > target; i--)
                    layout[i] = layout[i - 1];
                layout[target] = id;
                SaveHotbarLayout();
                return;
            }

            // No room to the right: use the nearest empty slot on the left instead.
            for (int i = target - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(layout[i]))
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot >= 0)
            {
                for (int i = emptySlot; i < target; i++)
                    layout[i] = layout[i + 1];
                layout[target] = id;
                SaveHotbarLayout();
                return;
            }

            ShowMessage("Hotbar is full - remove a skill first");
        }

        private void DrawReferenceHotbarHotkeys()
        {
            // v0.15.0: labels are live and clickable (click = rebind, right-click = reset).
            float[] centers = { 229f, 287f, 344f, 402f, 459f, 517f, 574f };
            for (int i = 0; i < centers.Length; i++)
            {
                Rect label = ScaleReferenceRect(centers[i] - 26f, 607f, 52f, 15f);
                DrawHotbarKeyLabel(label, i, "HOTBAR SLOT " + (i + 1).ToString() + " - " + FormatHotbarBinding(i));
            }
        }

        private void DrawReferenceRankAccent(Rect icon, TreeNodeKind kind)
        {
            // The painted frames remain untouched. Rank identity uses small jewels only so the
            // tree never picks up debug-looking cyan/pink corner brackets.
            Color accent = GetTreeNodeColor(kind);
            Rect frame = new Rect(icon.x - 3f, icon.y - 3f, icon.width + 6f, icon.height + 6f);

            if (kind == TreeNodeKind.Signature)
                accent = new Color(0.14f, 0.32f, 0.72f, 0.98f);
            else if (kind == TreeNodeKind.AdvancementNormal)
                accent = new Color(0.18f, 0.82f, 0.94f, 0.98f);
            else if (kind == TreeNodeKind.Buff)
                accent = new Color(0.30f, 0.82f, 0.42f, 0.98f);

            if (kind == TreeNodeKind.Ascended)
            {
                DrawDiamond(new Vector2(frame.center.x, frame.y - 3f), 4.5f, accent);
                DrawDiamond(new Vector2(frame.center.x, frame.yMax + 3f), 3.5f, new Color(accent.r, accent.g, accent.b, 0.80f));
            }
            else if (kind == TreeNodeKind.Signature)
            {
                DrawDiamond(new Vector2(frame.center.x, frame.y - 3f), 4f, new Color(0.46f, 0.66f, 1f, 0.98f));
                DrawDiamond(new Vector2(frame.x - 3f, frame.center.y), 2.7f, accent);
                DrawDiamond(new Vector2(frame.xMax + 3f, frame.center.y), 2.7f, accent);
            }
            else if (kind == TreeNodeKind.AdvancementNormal)
            {
                DrawDiamond(new Vector2(frame.x - 3f, frame.center.y), 2.5f, accent);
                DrawDiamond(new Vector2(frame.xMax + 3f, frame.center.y), 2.5f, accent);
            }
            else if (kind == TreeNodeKind.Buff)
            {
                DrawDiamond(new Vector2(frame.x - 3f, frame.center.y), 2.5f, accent);
                DrawDiamond(new Vector2(frame.xMax + 3f, frame.center.y), 2.5f, accent);
            }
            else if (kind == TreeNodeKind.Grace)
            {
                Color gold = new Color(0.96f, 0.78f, 0.24f, 1f);
                DrawDiamond(new Vector2(frame.center.x, frame.y - 4f), 5f, gold);
                DrawDiamond(new Vector2(frame.center.x, frame.yMax + 4f), 3.5f, new Color(gold.r, gold.g, gold.b, 0.78f));
            }
            else if (kind == TreeNodeKind.Ultimate || kind == TreeNodeKind.AscendedUltimate)
            {
                Color gold = new Color(0.96f, 0.76f, 0.30f, 1f);
                DrawDiamond(new Vector2(frame.center.x, frame.y - 6f), 6f, gold);
                DrawDiamond(new Vector2(frame.x - 4f, frame.center.y), 4f, accent);
                DrawDiamond(new Vector2(frame.xMax + 4f, frame.center.y), 4f, accent);
                DrawDiamond(new Vector2(frame.center.x, frame.yMax + 5f), 3f, gold);
            }
        }

        private void DrawReferenceSelectedAccent(Rect icon, TreeNodeKind kind)
        {
            Color gold = new Color(0.98f, 0.82f, 0.36f, 0.88f);
            Rect cue = new Rect(icon.x + 7f, icon.yMax + 2f, icon.width - 14f, 2f);
            GUI.color = gold;
            GUI.DrawTexture(cue, _treeGoldTex);
            GUI.color = Color.white;
        }

        private void DrawTierQueueButton(Rect rect, string symbol)
        {
            Texture2D art = symbol == "+" ? _treeTierPlusTex : _treeTierMinusTex;
            if (art != null)
            {
                bool hover = rect.Contains(Event.current.mousePosition);
                GUI.color = hover ? new Color(1f, 1f, 1f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
                GUI.DrawTexture(rect, art);
                GUI.color = Color.white;
                return;
            }

            DrawFilledBorder(rect, new Color(0.16f, 0.18f, 0.19f, 0.98f), new Color(0.92f, 0.73f, 0.27f, 1f), 1f);
            GUI.Label(rect, symbol, _treeSubHeaderStyle);
        }

        private void DrawReferenceFooterUx()
        {
            // v0.15.0: the Grace slot and the right panel are baked artwork. Code only adds the
            // live Grace key label and the CONFIRM plaque (only while Tiers are pending).
            Player footerPlayer = Player.m_localPlayer;
            IhKit footerKit = IhTreeKit();
            string footerGrace = footerKit != null ? footerKit.Grace : IhGrace;
            bool graceLocked = !IhIsUnlocked(footerPlayer, footerGrace);
            if (graceLocked && IhDrawLockedRegion("grace_slot") && _ihPadlockTex != null)
                GUI.DrawTexture(IhSnap(ScaleReferenceRect(692f, 570f, 26f, 26f)), _ihPadlockTex);
            IhDrawPlaceholderInitials(ScaleReferenceRect(670f, 548f, 44f, 44f), footerGrace, graceLocked);
            Rect graceLabel = ScaleReferenceRect(641f, 607f, 100f, 15f);
            DrawHotbarKeyLabel(graceLabel, BindGrace, "GRACE - " + FormatHotbarBinding(BindGrace));

            int pendingTotal = GetPrototypeTotalPending();
            if (pendingTotal <= 0)
            {
                IhDrawAscendButton(Player.m_localPlayer);
                return;
            }

            Rect plaque = ScaleReferenceRect(792f, 557f, 160f, 42f);
            bool hover = plaque.Contains(Event.current.mousePosition);
            if (_treeConfirmPlaqueTex != null)
            {
                GUI.color = hover ? Color.white : new Color(0.92f, 0.92f, 0.92f, 1f);
                GUI.DrawTexture(plaque, _treeConfirmPlaqueTex);
                GUI.color = Color.white;
            }

            Rect confirmText = new Rect(plaque.x, plaque.y + plaque.height * 0.10f, plaque.width, plaque.height * 0.50f);
            Rect pendingText = new Rect(plaque.x, plaque.y + plaque.height * 0.58f, plaque.width, plaque.height * 0.28f);
            DrawFooterText(confirmText, "CONFIRM", hover ? _treeFooterConfirmHoverStyle : _treeFooterConfirmStyle);
            DrawFooterText(pendingText, pendingTotal.ToString() + " PENDING", _treeFooterPendingStyle);

            if (GUI.Button(plaque, GUIContent.none, GUIStyle.none))
                ConfirmPrototypePending();
        }

        private void DrawHotbarKeyLabel(Rect rect, int bindTarget, string tooltipTitle)
        {
            Event e = Event.current;
            bool hover = rect.Contains(e.mousePosition);
            bool capturing = _bindCaptureTarget == bindTarget;

            if (hover)
            {
                _treeHoveredTitle = tooltipTitle;
                _treeHoveredBody = "Click, then press a single key, or hold a modifier and press a key for a combo (e.g. M4 + 3).\n"
                    + "Right-click resets to default. Esc cancels. A binding already used by another slot is swapped.";
            }

            if (hover && e.type == EventType.MouseDown && e.button == 1)
            {
                ResetHotbarBinding(bindTarget);
                e.Use();
                return;
            }

            string text;
            if (!capturing)
                text = FormatHotbarBinding(bindTarget);
            else if (_bindCaptureFirst != KeyCode.None)
                text = ShortKeyName(_bindCaptureFirst) + " + ...";
            else
                text = "PRESS KEY";

            GUIStyle style = capturing ? _treeFooterKeyCaptureStyle : (hover ? _treeFooterKeyHoverStyle : _treeFooterKeyStyle);
            if (capturing)
            {
                Color c = style.normal.textColor;
                c.a = 0.55f + 0.45f * Mathf.PingPong(Time.unscaledTime * 2.2f, 1f);
                style.normal.textColor = c;
            }
            DrawFooterText(rect, text, style);

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                BeginHotbarKeyCapture(bindTarget);
        }

        private string FormatHotbarBinding(int index)
        {
            KeyCode mod = _hotbarSlotMods[index].Value;
            KeyCode key = _hotbarSlotKeys[index].Value;
            return mod == KeyCode.None ? ShortKeyName(key) : ShortKeyName(mod) + " + " + ShortKeyName(key);
        }

        private void BeginHotbarKeyCapture(int target)
        {
            _bindCaptureTarget = target;
            _bindCaptureStartFrame = Time.frameCount;
            _bindCaptureFirst = KeyCode.None;
        }

        private void CancelHotbarKeyCapture()
        {
            _bindCaptureTarget = BindNone;
            _bindCaptureFirst = KeyCode.None;
        }

        private void UpdateHotbarKeyCapture()
        {
            if (!_skillbookOpen)
            {
                CancelHotbarKeyCapture();
                return;
            }

            // Ignore the frame of the click that started the capture.
            if (Time.frameCount <= _bindCaptureStartFrame)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelHotbarKeyCapture();
                return;
            }

            if (_bindableKeyCodes == null)
            {
                Array values = Enum.GetValues(typeof(KeyCode));
                List<KeyCode> list = new List<KeyCode>();
                foreach (object value in values)
                {
                    KeyCode k = (KeyCode)value;
                    // Left/right click stay UI clicks; joystick codes are excluded.
                    if (k == KeyCode.None || k == KeyCode.Escape || k == KeyCode.Mouse0 || k == KeyCode.Mouse1)
                        continue;
                    if ((int)k >= (int)KeyCode.JoystickButton0)
                        continue;
                    if (!list.Contains(k))
                        list.Add(k);
                }
                _bindableKeyCodes = list.ToArray();
            }

            // Combo detection: the first key pressed is held; if a second key goes down while it is
            // held, the first becomes the modifier. If the first key is released alone, it is a
            // single-key binding. This lets M4 (or Shift/Ctrl/etc.) be either a modifier or a key.
            if (_bindCaptureFirst == KeyCode.None)
            {
                for (int i = 0; i < _bindableKeyCodes.Length; i++)
                {
                    if (Input.GetKeyDown(_bindableKeyCodes[i]))
                    {
                        _bindCaptureFirst = _bindableKeyCodes[i];
                        return;
                    }
                }
                return;
            }

            for (int i = 0; i < _bindableKeyCodes.Length; i++)
            {
                KeyCode k = _bindableKeyCodes[i];
                if (k != _bindCaptureFirst && Input.GetKeyDown(k))
                {
                    AssignHotbarBinding(_bindCaptureTarget, _bindCaptureFirst, k);
                    CancelHotbarKeyCapture();
                    return;
                }
            }

            if (!Input.GetKey(_bindCaptureFirst))
            {
                AssignHotbarBinding(_bindCaptureTarget, KeyCode.None, _bindCaptureFirst);
                CancelHotbarKeyCapture();
            }
        }

        private void AssignHotbarBinding(int index, KeyCode modifier, KeyCode key)
        {
            if (index < 0 || index >= HotbarBindingCount)
                return;

            KeyCode previousMod = _hotbarSlotMods[index].Value;
            KeyCode previousKey = _hotbarSlotKeys[index].Value;
            for (int i = 0; i < HotbarBindingCount; i++)
            {
                if (i != index && _hotbarSlotMods[i].Value == modifier && _hotbarSlotKeys[i].Value == key)
                {
                    _hotbarSlotMods[i].Value = previousMod;
                    _hotbarSlotKeys[i].Value = previousKey;
                }
            }

            // BepInEx saves the .cfg automatically when a ConfigEntry value changes.
            _hotbarSlotMods[index].Value = modifier;
            _hotbarSlotKeys[index].Value = key;
            Logger.LogInfo("Hotbar: " + (index == BindGrace ? "Grace" : "slot " + (index + 1).ToString()) + " bound to " + FormatHotbarBinding(index));
        }

        private void ResetHotbarBinding(int index)
        {
            CancelHotbarKeyCapture();
            if (index < 0 || index >= HotbarBindingCount)
                return;
            AssignHotbarBinding(index, (KeyCode)_hotbarSlotMods[index].DefaultValue, (KeyCode)_hotbarSlotKeys[index].DefaultValue);
        }

        private static string ShortKeyName(KeyCode key)
        {
            if (key == KeyCode.None)
                return "-";
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
                return ((int)(key - KeyCode.Alpha0)).ToString();
            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
                return "Num" + ((int)(key - KeyCode.Keypad0)).ToString();
            if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)
                return "M" + ((int)(key - KeyCode.Mouse0) + 1).ToString();

            switch (key)
            {
                case KeyCode.LeftShift: return "LShift";
                case KeyCode.RightShift: return "RShift";
                case KeyCode.LeftControl: return "LCtrl";
                case KeyCode.RightControl: return "RCtrl";
                case KeyCode.LeftAlt: return "LAlt";
                case KeyCode.RightAlt: return "RAlt";
                case KeyCode.BackQuote: return "`";
                case KeyCode.Minus: return "-";
                case KeyCode.Equals: return "=";
                case KeyCode.Space: return "Space";
                case KeyCode.CapsLock: return "Caps";
                default: return key.ToString();
            }
        }

        private void DrawFooterText(Rect rect, string text, GUIStyle style)
        {
            // One crisp dark drop shadow instead of a same-color double draw.
            Color oldText = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = oldText;
            GUI.Label(rect, text, style);
        }

        // v0.18.0: real Tiers saved on the character (names kept from the prototype).
        private int GetPrototypeTier(string nodeId)
        {
            return IhGetTier(Player.m_localPlayer, nodeId);
        }

        private int GetPrototypePending(string nodeId)
        {
            int pending;
            return _treePrototypePending.TryGetValue(nodeId, out pending) ? pending : 0;
        }

        private int GetPrototypeTotalPending()
        {
            int total = 0;
            foreach (KeyValuePair<string, int> kvp in _treePrototypePending)
                total += kvp.Value;
            return total;
        }

        private void AddPrototypePending(string nodeId, int maxTier)
        {
            int current = GetPrototypeTier(nodeId);
            int pending = GetPrototypePending(nodeId);
            if (current + pending >= maxTier || !IhCanQueueTier(Player.m_localPlayer, nodeId))
                return;

            _treePrototypePending[nodeId] = pending + 1;
        }

        private void RemovePrototypePending(string nodeId)
        {
            int pending = GetPrototypePending(nodeId);
            if (pending <= 0)
                return;

            if (pending == 1)
                _treePrototypePending.Remove(nodeId);
            else
                _treePrototypePending[nodeId] = pending - 1;
        }

        private void ConfirmPrototypePending()
        {
            Player player = Player.m_localPlayer;
            if (player == null) { _treePrototypePending.Clear(); return; }
            string[] confirmClassSkills = IhPlayerClassSkills(player);
            int classSpend = IhPendingSum(confirmClassSkills);
            int advSpend = IhPendingSum(IhBranchSkills(player));
            bool valid = classSpend <= IhClassPointsEarned(player) - IhSpent(player, confirmClassSkills)
                && advSpend <= IhAdvPointsEarned(player) - IhSpent(player, IhBranchSkills(player));
            foreach (KeyValuePair<string,int> kvp in _treePrototypePending)
                if (kvp.Value < 1 || !IhIsUnlocked(player, kvp.Key) || IhIsUltimate(kvp.Key)
                    || (IhContains(confirmClassSkills,kvp.Key) && !string.IsNullOrEmpty(GetAdvancement(player)))
                    || GetPrototypeTier(kvp.Key) + kvp.Value > IhMaxTier(kvp.Key)) valid = false;
            if (!valid)
            {
                _treePrototypePending.Clear();
                ShowMessage("Tier requirements changed. Please choose your Tiers again.");
                return;
            }
            Dictionary<string,int> tiers = new Dictionary<string,int>(IhTiers(player));
            foreach (KeyValuePair<string,int> kvp in _treePrototypePending)
                tiers[kvp.Key] = GetPrototypeTier(kvp.Key) + kvp.Value;
            IhSetTiers(player,tiers);
            _treePrototypePending.Clear();
        }

        private void DrawImmortalHeroesBackdrop()
        {
            Rect full = new Rect(0f, 0f, _skillbookRect.width, _skillbookRect.height);

            // v0.14.5: the live IMGUI shell now follows the approved Immortal Heroes
            // concept more closely using procedural parchment gradients, sacred navy,
            // restrained rose and lightweight gold ornament instead of flat prototype fills.
            GUI.DrawTexture(full, _treeMainTex);

            Rect topBand = new Rect(10f, 10f, full.width - 20f, 72f);
            GUI.DrawTexture(topBand, _treeHotbarTex);
            DrawOrnateFrame(topBand, new Color(0.88f, 0.68f, 0.29f, 0.98f), 2f, true);
            DrawBorder(new Rect(topBand.x + 7f, topBand.y + 7f, topBand.width - 14f, topBand.height - 14f),
                new Color(0.98f, 0.86f, 0.56f, 0.24f), 1f);

            // Thin luminous rails make the title band read as engraved metal rather than a flat bar.
            GUI.color = new Color(0.95f, 0.76f, 0.36f, 0.88f);
            GUI.DrawTexture(new Rect(topBand.x + 18f, topBand.y + 5f, topBand.width - 36f, 2f), _treeGoldTex);
            GUI.DrawTexture(new Rect(topBand.x + 18f, topBand.yMax - 7f, topBand.width - 36f, 2f), _treeGoldTex);
            GUI.color = Color.white;

            Rect frame = new Rect(10f, 10f, full.width - 20f, full.height - 20f);
            DrawOrnateFrame(frame, new Color(0.84f, 0.64f, 0.27f, 1f), 3f, true);
            DrawBorder(new Rect(frame.x + 7f, frame.y + 7f, frame.width - 14f, frame.height - 14f),
                new Color(0.98f, 0.88f, 0.63f, 0.32f), 1f);

            // Stronger corner jewels approximate the ornate concept art without external assets.
            DrawDiamond(new Vector2(frame.x + 10f, frame.y + 10f), 7f, new Color(0.92f, 0.72f, 0.31f, 0.95f));
            DrawDiamond(new Vector2(frame.xMax - 10f, frame.y + 10f), 7f, new Color(0.92f, 0.72f, 0.31f, 0.95f));
            DrawDiamond(new Vector2(frame.x + 10f, frame.yMax - 10f), 7f, new Color(0.92f, 0.72f, 0.31f, 0.95f));
            DrawDiamond(new Vector2(frame.xMax - 10f, frame.yMax - 10f), 7f, new Color(0.92f, 0.72f, 0.31f, 0.95f));

            float cx = full.width * 0.50f;
            Rect crestGlow = new Rect(cx - 48f, 6f, 96f, 82f);
            GUI.DrawTexture(crestGlow, _treeGoldGlowTex);
            DrawDiamond(new Vector2(cx, 48f), 18f, new Color(0.97f, 0.77f, 0.34f, 1f));
            DrawDiamond(new Vector2(cx, 48f), 10f, new Color(0.22f, 0.13f, 0.055f, 1f));
            GUI.Label(new Rect(cx - 22f, 23f, 44f, 48f), "✝", _treeHeaderEmblemStyle);
            GUI.color = new Color(0.94f, 0.73f, 0.31f, 0.86f);
            GUI.DrawTexture(new Rect(cx - 58f, 47f, 36f, 2f), _treeGoldTex);
            GUI.DrawTexture(new Rect(cx + 22f, 47f, 36f, 2f), _treeGoldTex);
            GUI.color = Color.white;

            GUI.Label(new Rect(34f, 17f, 410f, 38f), "IMMORTAL HEROES", _treeTitleStyle);
            GUI.Label(new Rect(36f, 49f, 410f, 20f), "SKILL TREE  •  UI PROTOTYPE", _treeTinyLeftStyle);
        }

        private void DrawClericPaladinTree(Player player, bool advanced)
        {
            const float bodyY = 88f;
            const float bodyH = 558f;
            Rect classArea = new Rect(28f, bodyY, 315f, bodyH);
            Rect advArea = new Rect(353f, bodyY, 799f, bodyH);

            // Distinct identities: Cleric sits on cool sacred parchment while Paladin owns
            // a warmer ivory/rose field. The watermark motifs make each side read as a class,
            // not just as two tinted rectangles.
            GUI.DrawTexture(classArea, _treeClassAreaTex);
            GUI.DrawTexture(advArea, _treeAdvAreaTex);
            DrawOrnateFrame(classArea, new Color(0.36f, 0.58f, 0.75f, 0.94f), 2f, true);
            DrawOrnateFrame(advArea, new Color(0.66f, 0.35f, 0.42f, 0.92f), 2f, true);

            // Concept-art side banners: they visually anchor Class vs Advancement without adding more labels.
            Rect clericBanner = new Rect(classArea.x + 7f, classArea.y + 78f, 22f, classArea.height - 100f);
            DrawFilledBorder(clericBanner, new Color(0.035f, 0.13f, 0.23f, 0.88f), new Color(0.84f, 0.66f, 0.30f, 0.78f), 1f);
            GUI.Label(new Rect(clericBanner.x - 2f, clericBanner.y + 74f, clericBanner.width + 4f, 66f), "✝", _treeHeaderEmblemStyle);
            Rect paladinBanner = new Rect(advArea.xMax - 29f, advArea.y + 78f, 22f, advArea.height - 100f);
            DrawFilledBorder(paladinBanner, new Color(0.37f, 0.085f, 0.14f, 0.78f), new Color(0.84f, 0.66f, 0.30f, 0.72f), 1f);
            GUI.Label(new Rect(paladinBanner.x - 2f, paladinBanner.y + 78f, paladinBanner.width + 4f, 66f), "✦", _treeHeaderEmblemStyle);

            DrawBorder(new Rect(classArea.x + 10f, classArea.y + 10f, classArea.width - 20f, classArea.height - 20f),
                new Color(0.38f, 0.57f, 0.70f, 0.24f), 1f);
            DrawBorder(new Rect(advArea.x + 10f, advArea.y + 10f, advArea.width - 20f, advArea.height - 20f),
                new Color(0.68f, 0.39f, 0.45f, 0.20f), 1f);

            DrawPanelWatermark(new Rect(classArea.x + 14f, classArea.y + 76f, classArea.width - 28f, classArea.height - 96f),
                "✝", new Color(0.22f, 0.47f, 0.68f, 0.075f));
            DrawPanelWatermark(new Rect(advArea.x + 18f, advArea.y + 76f, advArea.width - 36f, advArea.height - 96f),
                "♜", new Color(0.58f, 0.24f, 0.31f, 0.055f));

            // Gold seam between Class and Advancement.
            GUI.color = new Color(0.82f, 0.62f, 0.27f, 0.85f);
            GUI.DrawTexture(new Rect(347f, bodyY + 5f, 2f, bodyH - 10f), _treeGoldTex);
            GUI.color = Color.white;
            DrawDiamond(new Vector2(348f, bodyY + 38f), 6f, new Color(0.90f, 0.70f, 0.32f, 0.95f));
            DrawDiamond(new Vector2(348f, bodyY + bodyH - 38f), 6f, new Color(0.90f, 0.70f, 0.32f, 0.95f));

            Rect clericHeader = new Rect(46f, 100f, 275f, 58f);
            Rect paladinHeader = new Rect(380f, 100f, 742f, 58f);
            DrawHeaderRibbon(clericHeader, _treeClassHeaderTex, new Color(0.95f, 0.77f, 0.38f, 1f), false);
            DrawHeaderRibbon(paladinHeader, _treeAdvHeaderTex, new Color(0.95f, 0.77f, 0.38f, 1f), true);
            GUI.DrawTexture(new Rect(clericHeader.x - 12f, clericHeader.y - 10f, 74f, 74f), _treeGoldGlowTex);
            GUI.DrawTexture(new Rect(paladinHeader.x - 10f, paladinHeader.y - 10f, 74f, 74f), _treeGoldGlowTex);
            DrawDiamond(new Vector2(clericHeader.x + 23f, clericHeader.center.y), 14f, new Color(0.92f, 0.73f, 0.33f, 0.95f));
            DrawDiamond(new Vector2(paladinHeader.x + 24f, paladinHeader.center.y), 14f, new Color(0.92f, 0.73f, 0.33f, 0.95f));

            // Header emblems and centered class names.
            GUI.Label(new Rect(50f, 103f, 42f, 40f), "✦", _treeHeaderEmblemStyle);
            GUI.Label(new Rect(105f, 103f, 205f, 30f), "CLERIC", _treeHeaderStyle);
            GUI.Label(new Rect(105f, 132f, 205f, 17f), "hover for Blessing", _treeTinyStyle);

            GUI.Label(new Rect(384f, 103f, 54f, 40f), "✝", _treeHeaderEmblemStyle);
            GUI.Label(new Rect(446f, 103f, 655f, 30f), "PALADIN", _treeHeaderStyle);
            GUI.Label(new Rect(446f, 132f, 655f, 17f), "hover for Holy Trinity Mastery", _treeTinyStyle);

            if (clericHeader.Contains(Event.current.mousePosition))
            {
                _treeHoveredTitle = "CLERIC'S BLESSING";
                _treeHoveredBody = "All Shields: 1.5x Block Force + Block Armor. Staff + Shield allowed. No movement penalty from Shields, Staves, or one-handed Club-skill weapons. +35 Max HP and +20% HP Regen.";
            }
            if (paladinHeader.Contains(Event.current.mousePosition))
            {
                _treeHoveredTitle = "HOLY TRINITY — MASTERY";
                _treeHoveredBody = "While wielding a Club-type melee weapon + Shield: +15 Clubs, no Armor movement penalty, and Slash/Pierce are each brought up to 50% of current Blunt damage without lowering existing damage.";
            }

            // The only progression connector: Ascended Class prerequisite -> Advancement.

            DrawTreeNode(new Rect(184f, 181f, 72f, 72f), "Lightning Zap", "⚡", TreeNodeKind.Ascended,
                7, 7, true, "ASCENDED", "Prerequisite Class skill. Ascended skills stay on the numbered hotbar and cannot be removed.");
            DrawTreeNode(new Rect(184f, 343f, 64f, 64f), "Righteous Strike", "RS", TreeNodeKind.ClassNormal,
                7, 0, false, "CLASS", "Optional Class skill after Advancement. Holy Ground PAC strike with Blunt + Lightning and Expose.");
            DrawTreeNode(new Rect(184f, 501f, 64f, 64f), "Holy Wave", "≈", TreeNodeKind.ClassNormal,
                7, 0, false, "CLASS", "Optional Class skill after Advancement. Instant healing wave with immediate and sustained healing.");

            DrawTreeNode(new Rect(418f, 181f, 70f, 70f), "Goddess Relic", "✝", TreeNodeKind.Signature,
                5, 0, true, "SIGNATURE", "Signature Skill. A divine Cross drops from the sky. Signature skills are mandatory hotbar skills.");
            DrawTreeNode(new Rect(418f, 343f, 70f, 70f), "Judgement Hammer", "JH", TreeNodeKind.Signature,
                5, 0, true, "SIGNATURE", "Signature Skill. Travelling holy hammer projectile. Signature skills are mandatory hotbar skills.");
            DrawTreeNode(new Rect(438f, 505f, 76f, 76f), "Heaven's Light", "✦", TreeNodeKind.Grace,
                0, 0, false, "GRACE  •  M4+R", "+40% Overall Defense and removes equipment movement penalties. 1 minute duration, 10 minute cooldown. Grace uses its own standalone M4+R hotbox.");

            DrawTreeNode(new Rect(638f, 184f, 64f, 64f), "Shield Charge", "SC", TreeNodeKind.AdvancementNormal,
                5, 0, false, "ADVANCEMENT", "Interchangeable Advancement skill. Normal Advancement nodes use cyan styling.");
            DrawTreeNode(new Rect(824f, 184f, 64f, 64f), "Angel Comet", "↓", TreeNodeKind.AdvancementNormal,
                5, 0, false, "ADVANCEMENT", "Interchangeable Advancement skill. Jump high, nose-dive, then slam a 10m area.");
            DrawTreeNode(new Rect(824f, 343f, 64f, 64f), "Ray of Hope", "+", TreeNodeKind.Buff,
                5, 0, false, "BUFF / SUPPORT", "Interchangeable non-damaging/support Advancement skill.");

            DrawTreeNode(new Rect(1002f, 240f, 112f, 112f), "Electric Smite", "⚡", TreeNodeKind.Ultimate,
                3, 0, true, "ULTIMATE", "Ultimate. Mandatory once unlocked, but freely movable to any of the seven numbered hotbar slots.");

            DrawHotbarPreview();
        }

        private enum TreeNodeKind
        {
            ClassNormal,
            AdvancementNormal,
            Buff,
            Signature,
            Ascended,
            Ultimate,
            AscendedUltimate,
            Grace
        }

        private void DrawTreeNode(Rect rect, string name, string icon, TreeNodeKind kind, int maxTier, int tier, bool mandatory, string rankLabel, string tooltip)
        {
            Color accent = GetTreeNodeColor(kind);
            float border = GetTreeBorderThickness(kind);
            bool ultimate = kind == TreeNodeKind.Ultimate || kind == TreeNodeKind.AscendedUltimate;
            bool grace = kind == TreeNodeKind.Grace;

            Texture2D glow = null;
            if (kind == TreeNodeKind.Ascended) glow = _treeMagentaGlowTex;
            else if (ultimate) glow = _treeMaroonGlowTex;
            else if (grace) glow = _treeGoldGlowTex;
            else if (kind == TreeNodeKind.Signature) glow = _treeBlueGlowTex;
            if (glow != null)
            {
                float glowPad = ultimate ? 28f : grace ? 20f : 14f;
                GUI.DrawTexture(new Rect(rect.x - glowPad, rect.y - glowPad, rect.width + glowPad * 2f, rect.height + glowPad * 2f), glow);
            }

            Rect shadow = new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height);
            GUI.DrawTexture(shadow, _treeShadowTex);

            // Rank-specific frame language: muted gold outer frame + category-colored inner frame.
            float outerPad = ultimate ? 10f : kind == TreeNodeKind.Ascended ? 7f : kind == TreeNodeKind.Signature ? 5f : grace ? 7f : 3f;
            Rect outer = new Rect(rect.x - outerPad, rect.y - outerPad, rect.width + outerPad * 2f, rect.height + outerPad * 2f);
            Color outerGold = ultimate ? new Color(0.95f, 0.70f, 0.28f, 1f) :
                              kind == TreeNodeKind.Ascended ? new Color(0.95f, 0.70f, 0.42f, 0.95f) :
                              grace ? new Color(0.95f, 0.77f, 0.22f, 1f) :
                              new Color(0.78f, 0.62f, 0.31f, 0.82f);
            DrawOrnateFrame(outer, outerGold, ultimate ? 3f : 2f, true);

            Color inner = grace ? new Color(0.20f, 0.14f, 0.035f, 0.99f) :
                          ultimate ? new Color(0.22f, 0.075f, 0.10f, 0.99f) :
                          new Color(0.075f, 0.09f, 0.13f, 0.99f);
            DrawFilledBorder(rect, inner, accent, border);
            DrawBorder(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f),
                new Color(accent.r, accent.g, accent.b, 0.33f), 1f);

            DrawTreeNodeCornerAccents(outer, accent, kind);

            if (kind == TreeNodeKind.Ascended)
            {
                DrawDiamond(new Vector2(outer.center.x, outer.y - 2f), 5f, accent);
                DrawDiamond(new Vector2(outer.center.x, outer.yMax + 2f), 4f, new Color(accent.r, accent.g, accent.b, 0.75f));
            }
            else if (kind == TreeNodeKind.Signature)
            {
                DrawDiamond(new Vector2(outer.center.x, outer.y - 1f), 4f, new Color(0.55f, 0.70f, 1f, 0.95f));
            }
            else if (ultimate)
            {
                DrawDiamond(new Vector2(outer.center.x, outer.y - 4f), 8f, outerGold);
                DrawDiamond(new Vector2(outer.x - 2f, outer.center.y), 5f, new Color(accent.r, accent.g, accent.b, 0.88f));
                DrawDiamond(new Vector2(outer.xMax + 2f, outer.center.y), 5f, new Color(accent.r, accent.g, accent.b, 0.88f));
            }

            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height - 9f), icon, ultimate ? _treeUltimateIconStyle : _treeNodeIconStyle);

            if (mandatory)
            {
                Rect pin = new Rect(outer.xMax - 15f, outer.y - 2f, 22f, 22f);
                DrawFilledBorder(pin, new Color(0.16f, 0.11f, 0.045f, 1f), new Color(0.96f, 0.78f, 0.35f, 1f), 1f);
                GUI.Label(pin, "♦", _treeTinyStyle);
            }

            if (maxTier > 0)
            {
                float pipGap = 2f;
                float pip = Mathf.Clamp((rect.width - 12f - pipGap * (maxTier - 1)) / maxTier, 4f, 7f);
                float total = pip * maxTier + pipGap * (maxTier - 1);
                float px = rect.x + (rect.width - total) * 0.5f;
                float py = rect.yMax - 7f;
                for (int i = 0; i < maxTier; i++)
                {
                    Color pc = i < tier ? accent : new Color(0.37f, 0.34f, 0.31f, 0.78f);
                    GUI.color = pc;
                    GUI.DrawTexture(new Rect(px + i * (pip + pipGap), py, pip, 4f), _treeGoldTex);
                    GUI.color = Color.white;
                }
            }

            // Parchment nameplate mirrors the concept art and separates names from the node color itself.
            float plateW = ultimate ? 176f : 154f;
            Rect plate = new Rect(rect.center.x - plateW * 0.5f, rect.yMax + 7f, plateW, 25f);
            DrawNamePlate(plate, name, ultimate);

            Rect hoverRect = new Rect(outer.x - 8f, outer.y - 8f, outer.width + 16f, outer.height + 56f);
            if (hoverRect.Contains(Event.current.mousePosition))
            {
                _treeHoveredTitle = name.ToUpperInvariant();
                _treeHoveredBody = rankLabel + (mandatory ? "  •  HOTBAR LOCKED" : "  •  OPTIONAL") + "\n" + tooltip;
            }
        }

        private void DrawHeaderRibbon(Rect rect, Texture2D fill, Color gold, bool longRibbon)
        {
            Rect shadow = new Rect(rect.x + 3f, rect.y + 4f, rect.width, rect.height);
            GUI.color = new Color(0f, 0f, 0f, 0.18f);
            GUI.DrawTexture(shadow, _treeGoldTex);
            GUI.color = Color.white;

            GUI.DrawTexture(rect, fill);
            DrawOrnateFrame(rect, gold, 2f, true);
            DrawBorder(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f),
                new Color(gold.r, gold.g, gold.b, 0.26f), 1f);

            float fold = longRibbon ? 18f : 14f;
            GUI.color = new Color(gold.r, gold.g, gold.b, 0.76f);
            GUI.DrawTexture(new Rect(rect.x - fold, rect.center.y - 1f, fold, 2f), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.xMax, rect.center.y - 1f, fold, 2f), _treeGoldTex);
            GUI.color = Color.white;
            DrawDiamond(new Vector2(rect.x, rect.center.y), 5f, gold);
            DrawDiamond(new Vector2(rect.xMax, rect.center.y), 5f, gold);
        }

        private void DrawPanelWatermark(Rect rect, string glyph, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.Label(rect, glyph, _treeWatermarkStyle);
            GUI.color = old;

            Vector2 c = rect.center;
            DrawDiamond(new Vector2(c.x, rect.y + 30f), 5f, color);
            DrawDiamond(new Vector2(c.x, rect.yMax - 30f), 5f, color);
            DrawDiamond(new Vector2(rect.x + 28f, c.y), 4f, color);
            DrawDiamond(new Vector2(rect.xMax - 28f, c.y), 4f, color);
        }

        private void DrawTreeNodeCornerAccents(Rect rect, Color accent, TreeNodeKind kind)
        {
            float reach = kind == TreeNodeKind.Ultimate || kind == TreeNodeKind.AscendedUltimate ? 12f :
                          kind == TreeNodeKind.Ascended ? 9f :
                          kind == TreeNodeKind.Signature || kind == TreeNodeKind.Grace ? 7f : 4f;
            float thickness = kind == TreeNodeKind.Ultimate || kind == TreeNodeKind.AscendedUltimate ? 2f : 1f;
            float offset = 3f;

            GUI.color = new Color(accent.r, accent.g, accent.b, 0.90f);
            // top-left
            GUI.DrawTexture(new Rect(rect.x - offset, rect.y - offset, reach, thickness), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.x - offset, rect.y - offset, thickness, reach), _treeGoldTex);
            // top-right
            GUI.DrawTexture(new Rect(rect.xMax - reach + offset, rect.y - offset, reach, thickness), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.xMax + offset - thickness, rect.y - offset, thickness, reach), _treeGoldTex);
            // bottom-left
            GUI.DrawTexture(new Rect(rect.x - offset, rect.yMax + offset - thickness, reach, thickness), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.x - offset, rect.yMax - reach + offset, thickness, reach), _treeGoldTex);
            // bottom-right
            GUI.DrawTexture(new Rect(rect.xMax - reach + offset, rect.yMax + offset - thickness, reach, thickness), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.xMax + offset - thickness, rect.yMax - reach + offset, thickness, reach), _treeGoldTex);
            GUI.color = Color.white;
        }

        private Color GetTreeNodeColor(TreeNodeKind kind)
        {
            if (kind == TreeNodeKind.ClassNormal) return new Color(0.25f, 0.53f, 0.91f, 1f);      // blue
            if (kind == TreeNodeKind.AdvancementNormal) return new Color(0.16f, 0.78f, 0.88f, 1f); // cyan
            if (kind == TreeNodeKind.Buff) return new Color(0.30f, 0.78f, 0.42f, 1f);             // green
            if (kind == TreeNodeKind.Signature) return new Color(0.16f, 0.31f, 0.70f, 1f);        // navy blue
            if (kind == TreeNodeKind.Ascended) return new Color(0.90f, 0.27f, 0.76f, 1f);         // magenta
            if (kind == TreeNodeKind.Ultimate) return new Color(0.72f, 0.34f, 0.43f, 1f);         // pastel maroon
            if (kind == TreeNodeKind.AscendedUltimate) return new Color(0.92f, 0.18f, 0.22f, 1f); // red
            return new Color(0.94f, 0.73f, 0.18f, 1f);                                            // Grace yellow
        }

        private float GetTreeBorderThickness(TreeNodeKind kind)
        {
            if (kind == TreeNodeKind.Ultimate || kind == TreeNodeKind.AscendedUltimate) return 4f;
            if (kind == TreeNodeKind.Ascended) return 4f;
            if (kind == TreeNodeKind.Signature) return 3f;
            if (kind == TreeNodeKind.Grace) return 3f;
            return 2f;
        }

        private void DrawHotbarPreview()
        {
            Rect band = new Rect(28f, 660f, 1124f, 96f);
            GUI.DrawTexture(band, _treeHotbarTex);
            DrawOrnateFrame(band, new Color(0.82f, 0.63f, 0.29f, 0.98f), 2f, true);
            DrawBorder(new Rect(band.x + 7f, band.y + 7f, band.width - 14f, band.height - 14f),
                new Color(0.90f, 0.72f, 0.36f, 0.24f), 1f);

            // v0.14.5: Color Key removed. The node language should explain itself through
            // borders, color and hover tooltips, so the footer can breathe like the approved reference.
            Rect infoPanel = new Rect(42f, 669f, 168f, 70f);
            DrawBorder(infoPanel, new Color(0.82f, 0.64f, 0.31f, 0.24f), 1f);
            GUI.Label(new Rect(48f, 670f, 156f, 22f), "7-SLOT HOTBAR", _treeSubHeaderStyle);
            GUI.Label(new Rect(48f, 695f, 156f, 40f), "Mandatory skills stay slotted;\nall 7 numbered slots are movable.", _treeTinyStyle);

            string[] icons = { "⚡", "✝", "JH", "SC", "+", "≈", "ES" };
            bool[] pinned = { true, true, true, false, false, false, true };
            TreeNodeKind[] kinds = { TreeNodeKind.Ascended, TreeNodeKind.Signature, TreeNodeKind.Signature, TreeNodeKind.AdvancementNormal, TreeNodeKind.Buff, TreeNodeKind.AdvancementNormal, TreeNodeKind.Ultimate };

            float sx = 228f;
            for (int i = 0; i < 7; i++)
            {
                Rect slot = new Rect(sx + i * 68f, 683f, 54f, 54f);
                Color accent = GetTreeNodeColor(kinds[i]);
                DrawFilledBorder(slot, new Color(0.055f, 0.075f, 0.105f, 1f), accent, 2f);
                DrawBorder(new Rect(slot.x - 3f, slot.y - 3f, slot.width + 6f, slot.height + 6f),
                    new Color(0.82f, 0.64f, 0.31f, 0.52f), 1f);
                DrawTreeNodeCornerAccents(new Rect(slot.x - 2f, slot.y - 2f, slot.width + 4f, slot.height + 4f), accent, kinds[i]);
                GUI.Label(slot, icons[i], _treeNodeIconStyle);
                GUI.Label(new Rect(slot.x, 663f, slot.width, 17f), (i + 1).ToString(), _treeTinyStyle);
                if (pinned[i])
                {
                    Rect pin = new Rect(slot.xMax - 11f, slot.y - 5f, 16f, 16f);
                    DrawFilledBorder(pin, new Color(0.16f, 0.11f, 0.045f, 1f), new Color(0.96f, 0.78f, 0.35f, 1f), 1f);
                    GUI.Label(pin, "♦", _treeTinyStyle);
                }
            }

            // With the legend gone, Grace gets the breathing room it deserves instead of sharing
            // the footer with a dev-style key. It remains visually separate from the 7 numbered slots.
            Rect grace = new Rect(790f, 672f, 300f, 72f);
            Color graceAccent = GetTreeNodeColor(TreeNodeKind.Grace);
            GUI.DrawTexture(new Rect(grace.x - 18f, grace.y - 18f, grace.width + 36f, grace.height + 36f), _treeGoldGlowTex);
            DrawFilledBorder(grace, new Color(0.16f, 0.11f, 0.025f, 1f), graceAccent, 3f);
            DrawOrnateFrame(new Rect(grace.x - 6f, grace.y - 6f, grace.width + 12f, grace.height + 12f),
                new Color(0.93f, 0.73f, 0.25f, 0.94f), 2f, true);
            DrawDiamond(new Vector2(grace.x + 34f, grace.center.y), 6f, graceAccent);
            DrawDiamond(new Vector2(grace.xMax - 34f, grace.center.y), 6f, graceAccent);
            GUI.Label(new Rect(grace.x, grace.y + 6f, grace.width, 27f), "GRACE", _treeSubHeaderStyle);
            GUI.Label(new Rect(grace.x, grace.y + 38f, grace.width, 20f), "M4 + R", _treeRankStyle);
        }

        private void DrawPrototypeUnavailable(string className, string advancement)
        {
            GUI.Label(new Rect(70f, 125f, 1040f, 45f), "IMMORTAL HEROES SKILL TREE FRAMEWORK", _treeHeaderStyle);
            GUI.Label(new Rect(70f, 182f, 1040f, 72f),
                "v0.14.5 ships the reference-asset tree for Cleric → Paladin. Choose Cleric at the Altar to preview the tree before Advancement, or choose Paladin to view it as your active Advancement tree.",
                _treeTooltipBodyStyle);
            GUI.Label(new Rect(70f, 280f, 1040f, 36f),
                "Current character: " + (string.IsNullOrEmpty(className) ? "No Class" : className) +
                (string.IsNullOrEmpty(advancement) ? "" : " → " + advancement), _treeSubHeaderStyle);

            Rect sample = new Rect(70f, 350f, 1040f, 230f);
            DrawFilledBorder(sample, new Color(0.12f, 0.13f, 0.15f, 0.95f), new Color(0.65f, 0.55f, 0.30f, 0.9f), 2f);
            GUI.Label(new Rect(95f, 370f, 990f, 40f), "FRAMEWORK READY FOR THE OTHER CLASS TREES", _treeSubHeaderStyle);
            GUI.Label(new Rect(95f, 420f, 990f, 120f),
                "The reusable node system already supports Class, Advancement, Buff, Signature, Ascended, Ultimate, Ascended Ultimate and Grace visual states, mandatory-hotbar markers, Tier pips, header tooltips and the 7-slot + Grace hotbar preview.",
                _treeTooltipBodyStyle);
        }

        private void DrawTreeTooltip()
        {
            if (string.IsNullOrEmpty(_treeHoveredTitle))
                return;

            // v0.18.0: rich-text skill sheet, height fits the content.
            Vector2 mouse = Event.current.mousePosition;
            _treeTooltipBodyStyle.richText = true;
            float w = 400f;
            float bodyHeight = _treeTooltipBodyStyle.CalcHeight(new GUIContent(_treeHoveredBody), w - 28f);
            float h = Mathf.Max(70f, bodyHeight + 50f);
            float x = Mathf.Min(mouse.x + 18f, _skillbookRect.width - w - 12f);
            float y = Mathf.Min(mouse.y + 18f, _skillbookRect.height - h - 12f);
            y = Mathf.Max(8f, y);
            Rect rect = new Rect(x, y, w, h);
            DrawFilledBorder(rect, new Color(0.055f, 0.06f, 0.075f, 0.98f), new Color(0.84f, 0.67f, 0.31f, 1f), 2f);
            GUI.Label(new Rect(x + 14f, y + 10f, w - 28f, 24f), _treeHoveredTitle, _treeTooltipTitleStyle);
            GUI.Label(new Rect(x + 14f, y + 37f, w - 28f, bodyHeight + 4f), _treeHoveredBody, _treeTooltipBodyStyle);
        }

        private void DrawNamePlate(Rect rect, string text, bool ultimate)
        {
            // v0.14.5: every skill name uses the same clean parchment banner language.
            // Ultimate prestige lives in the node/frame itself rather than a chunky colored label.
            Color border = ultimate ? new Color(0.83f, 0.57f, 0.25f, 1f) : new Color(0.73f, 0.55f, 0.28f, 0.98f);
            Rect shadow = new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height);

            GUI.color = new Color(0f, 0f, 0f, ultimate ? 0.18f : 0.12f);
            GUI.DrawTexture(shadow, _treeGoldTex);
            GUI.color = Color.white;

            // Reuse the cached parchment texture; never allocate textures from OnGUI.
            GUI.color = Color.white;
            GUI.DrawTexture(rect, _treeMainTex);
            DrawBorder(rect, border, ultimate ? 2f : 1f);
            DrawBorder(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f),
                new Color(border.r, border.g, border.b, ultimate ? 0.28f : 0.18f), 1f);

            // Small end-caps keep the banner ornate without turning it back into a colored rank label.
            GUI.color = border;
            GUI.DrawTexture(new Rect(rect.x - 9f, rect.center.y - 1f, 9f, 2f), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.xMax, rect.center.y - 1f, 9f, 2f), _treeGoldTex);
            GUI.color = Color.white;
            DrawDiamond(new Vector2(rect.x - 8f, rect.center.y), ultimate ? 4f : 3f, border);
            DrawDiamond(new Vector2(rect.xMax + 8f, rect.center.y), ultimate ? 4f : 3f, border);

            if (ultimate)
            {
                Color maroon = GetTreeNodeColor(TreeNodeKind.Ultimate);
                GUI.color = new Color(maroon.r, maroon.g, maroon.b, 0.72f);
                GUI.DrawTexture(new Rect(rect.x + 12f, rect.y + 2f, rect.width - 24f, 2f), _treeGoldTex);
                GUI.DrawTexture(new Rect(rect.x + 12f, rect.yMax - 4f, rect.width - 24f, 2f), _treeGoldTex);
                GUI.color = Color.white;
            }

            GUI.Label(rect, text, _treeNamePlateDarkStyle);
        }

        private void DrawOrnateFrame(Rect rect, Color color, float thickness, bool corners)
        {
            DrawBorder(rect, color, thickness);
            if (!corners)
                return;

            float arm = Mathf.Min(14f, Mathf.Min(rect.width, rect.height) * 0.18f);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x - 2f, rect.y + arm, arm, 1f), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.x + arm, rect.y - 2f, 1f, arm), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.xMax - arm + 2f, rect.y + arm, arm, 1f), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.xMax - arm, rect.y - 2f, 1f, arm), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.x - 2f, rect.yMax - arm, arm, 1f), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.x + arm, rect.yMax - arm + 2f, 1f, arm), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.xMax - arm + 2f, rect.yMax - arm, arm, 1f), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.xMax - arm, rect.yMax - arm + 2f, 1f, arm), _treeGoldTex);
            GUI.color = Color.white;

            DrawDiamond(new Vector2(rect.x, rect.y), 3.5f, color);
            DrawDiamond(new Vector2(rect.xMax, rect.y), 3.5f, color);
            DrawDiamond(new Vector2(rect.x, rect.yMax), 3.5f, color);
            DrawDiamond(new Vector2(rect.xMax, rect.yMax), 3.5f, color);
        }

        private void DrawDiamond(Vector2 center, float radius, Color color)
        {
            GUI.color = color;
            int rows = Mathf.Max(2, Mathf.CeilToInt(radius * 2f));
            for (int i = 0; i < rows; i++)
            {
                float half = radius - Mathf.Abs((i + 0.5f) - radius);
                float y = center.y - radius + i;
                GUI.DrawTexture(new Rect(center.x - Mathf.Max(0.5f, half), y, Mathf.Max(1f, half * 2f), 1f), _treeGoldTex);
            }
            GUI.color = Color.white;
        }

        private void DrawFilledBorder(Rect rect, Color fill, Color border, float thickness)
        {
            GUI.color = fill;
            GUI.DrawTexture(rect, _treeGoldTex);
            GUI.color = Color.white;
            DrawBorder(rect, border, thickness);
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), _treeGoldTex);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), _treeGoldTex);
            GUI.color = Color.white;
        }

        private void DrawTreeLine(Vector2 a, Vector2 b, float thickness, Color color)
        {
            // The prototype uses orthogonal connectors to stay clean and lightweight in IMGUI.
            GUI.color = color;
            if (Mathf.Abs(a.y - b.y) < 0.5f)
            {
                float x = Mathf.Min(a.x, b.x);
                GUI.DrawTexture(new Rect(x, a.y - thickness * 0.5f, Mathf.Abs(b.x - a.x), thickness), _treeGoldTex);
            }
            else if (Mathf.Abs(a.x - b.x) < 0.5f)
            {
                float y = Mathf.Min(a.y, b.y);
                GUI.DrawTexture(new Rect(a.x - thickness * 0.5f, y, thickness, Mathf.Abs(b.y - a.y)), _treeGoldTex);
            }
            else
            {
                // L-shaped connector for non-aligned nodes.
                float midX = (a.x + b.x) * 0.5f;
                float x1 = Mathf.Min(a.x, midX);
                float x2 = Mathf.Min(midX, b.x);
                GUI.DrawTexture(new Rect(x1, a.y - thickness * 0.5f, Mathf.Abs(midX - a.x), thickness), _treeGoldTex);
                GUI.DrawTexture(new Rect(midX - thickness * 0.5f, Mathf.Min(a.y, b.y), thickness, Mathf.Abs(b.y - a.y)), _treeGoldTex);
                GUI.DrawTexture(new Rect(x2, b.y - thickness * 0.5f, Mathf.Abs(b.x - midX), thickness), _treeGoldTex);
            }
            GUI.color = Color.white;
        }

        private void DrawArrowHead(Vector2 tip, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(tip.x - 10f, tip.y - 5f, 10f, 3f), _treeGoldTex);
            GUI.DrawTexture(new Rect(tip.x - 7f, tip.y + 2f, 7f, 3f), _treeGoldTex);
            GUI.color = Color.white;
        }

        private void DrawLegacySkillbookWindow(int windowId)
        {
            Player player = Player.m_localPlayer;

            if (player == null)
                return;

            string className = GetClass(player);
            string advancement = GetAdvancement(player);

            GUI.Label(
                new Rect(30f, 34f, 920f, 32f),
                className + (string.IsNullOrEmpty(advancement) ? "" : "  >  " + advancement),
                _bookHeaderStyle
            );

            GUI.Label(
                new Rect(30f, 66f, 920f, 25f),
                "Fixed class kit - skills belong to the class and are not interchangeable.",
                _passiveStyle
            );

            if (className == "Warrior")
            {
                DrawBookCard(new Rect(30f, 105f, 440f, 90f), "Heavy Slash", "M4 + 1", "0.7s heavy horizontal Slash. Inflicts Broken Bones.");
                DrawBookCard(new Rect(30f, 202f, 440f, 90f), "Impact Wave", "M4 + 2", "Ground Projectile: Blunt + Pierce line wave that follows terrain.");
                DrawBookCard(new Rect(30f, 299f, 440f, 90f), "Impact Punch", "M4 + 3", "0.5s punch, 2m x 2m. Blunt damage; Stuns Small enemies.");
                DrawBookCard(new Rect(30f, 396f, 440f, 72f), "Warrior Blessing", "PASSIVE", "Hyper Armor if the incoming hit is below 30% of max HP; timed parry is 2x stronger.");
            }
            else if (className == "Cleric")
            {
                DrawBookCard(new Rect(30f, 105f, 440f, 90f), "Lightning Zap", "M4 + 1", "Instant 10m Ghost cone. Pierce + Lightning and inflicts Zap.");
                DrawBookCard(new Rect(30f, 202f, 440f, 90f), "Righteous Strike", "M4 + 2", "0.7s Ground PAC Sky Summon. Blunt + Lightning, 5m AoE, inflicts Expose.");
                DrawBookCard(new Rect(30f, 299f, 440f, 90f), "Holy Wave", "M4 + 3", "Instant 7m wave: 25 HP now + 5% max HP/sec for 6s. VFX pulses once.");
                DrawBookCard(new Rect(30f, 396f, 440f, 72f), "Cleric Blessings", "PASSIVE", "Shield Weapon Mastery: every Shield gets 1.5x Block Force + Block Power. Divine Duality: wield a Staff + Shield together.");
            }
            else if (className == "Sorcerer")
            {
                DrawBookCard(new Rect(30f, 105f, 440f, 90f), "Flame Burst", "M4 + 1", "10m Fire cone with Fire Burn.");
                DrawBookCard(new Rect(30f, 202f, 440f, 90f), "Glacial Descent", "M4 + 2", "Ground PAC: 5m Blunt + Frost impact.");
                DrawBookCard(new Rect(30f, 299f, 440f, 90f), "Stonefang Eruption", "M4 + 3", "Ground PAC: 5m Blunt + Pierce; Small Stun, Small/Big Cripple.");
                DrawBookCard(new Rect(30f, 396f, 440f, 72f), "Arcane Blood", "PASSIVE", "+30% Eitr Regen, +40 Max Eitr and +30% Magic Damage. Emergency escape remains planned, not active yet.");
            }
            else
            {
                GUI.Label(new Rect(30f, 120f, 900f, 80f), "Choose a class at Dragon's Altar first.", _bookHeaderStyle);
            }

            if (string.IsNullOrEmpty(advancement))
            {
                GUI.Box(new Rect(500f, 105f, 445f, 335f), "");

                GUI.Label(
                    new Rect(520f, 150f, 405f, 80f),
                    "NO ADVANCEMENT SELECTED",
                    _bookHeaderStyle
                );

                GUI.Label(
                    new Rect(520f, 230f, 405f, 110f),
                    "Use Dragon's Altar and choose one of your base-class advancements to unlock its additional active skills and advanced passive.",
                    _bookTextStyle
                );
            }
            else
            {
                DrawBookCard(new Rect(500f, 105f, 445f, 82f), GetAdvancedSkillName(advancement), "M4 + 4", GetAdvancedSkillDescription(advancement));
                DrawBookCard(new Rect(500f, 194f, 445f, 82f), GetAdvancedSkill5Name(advancement), "M4 + 5", GetAdvancedSkill5Description(advancement));
                if (advancement == "Mercenary")
                    DrawBookCard(new Rect(500f, 283f, 445f, 82f), "Circle Swing", "M4 + 6", "1.5s steerable heavy wind-up with only 0.5m total shuffle. 7m radius, 1.75x held-weapon damage, force-Stuns Small/Big/Boss; uninterruptable Hyper Armor.");
                else if (advancement == "Paladin")
                    DrawBookCard(new Rect(500f, 283f, 445f, 82f), "Shield Charge", "M4 + 6", "Steerable physical 15m charge. Big/Boss cap: 4 persistent ticks, then Bash.");
                else if (advancement == "Sword Master")
                    DrawBookCard(new Rect(500f, 283f, 445f, 82f), "Judgement Cut", "M4 + 6", "4 stacks. Ground PAC / Target PAC / Free Aim within 15m. Each cast creates a 4m sphere with 3 pure Slash cuts resolving instantly and simultaneously; each spent stack independently recharges in 12s, with a 0.5s buffer between activations.");
                else if (advancement == "Priest")
                    DrawBookCard(new Rect(500f, 283f, 445f, 82f), "Divine Intervention", "M4 + 6", "10m self-centered Holy Wave-style AoE. Aim directly at Lightning Relic or Holy Relic to Cross Cast the same AoE from that Cross instead.");
                else
                    DrawBookCard(new Rect(500f, 283f, 445f, 82f), GetUltimateName(advancement), "M4 + 6", GetUltimateDescription(advancement));

                float passiveY = 372f;
                if (advancement == "Sword Master")
                {
                    DrawBookCard(new Rect(500f, 372f, 445f, 82f), "Severed Horizon / Empty Sheath", "M4 + 7 / M4 + 8", "Severed Horizon draws a long world-cut line, then tears the whole line open at once. Empty Sheath is a brief full-negation counter that slips behind the attacker and answers with rapid delayed Slash cuts.");
                    DrawBookCard(new Rect(500f, 461f, 445f, 82f), "Halfmoon Slash", "M4 + 9", GetUltimateDescription(advancement));
                    passiveY = 550f;
                }
                else if (advancement == "Mercenary")
                {
                    DrawBookCard(new Rect(500f, 372f, 445f, 82f), "Seismic Guillotine / Reaver's Orbit", "M4 + 7 / M4 + 8", "Seismic: Ground/Target PAC up to 15m. 1 Seismic Shock every 1m (2m radius), constant travel speed, then a 10m endpoint Explosion; point-blank casts skip Shocks. Fury still widens + branches it. Reaver: dual opposing axe arcs; outward push, returning pull on Small enemies.");
                    DrawBookCard(new Rect(500f, 461f, 445f, 82f), "Whirlwind", "M4 + 9", "ULTIMATE. Mobile 6s spin; every 0.5s deals 0.5x held-weapon damage and carries weapon elements. Unchained Fury does not modify it.");
                    passiveY = 550f;
                }
                else if (advancement == "Paladin")
                {
                    DrawBookCard(new Rect(500f, 372f, 445f, 82f), "Divine Verdict / Aegis Fall", "M4 + 7 / M4 + 8", "Verdict drops a colossal holy hammer and immediately detonates marked enemies in the impact. Aegis Fall drops a Physical divine wall; Shield Charge shatters your own Aegis into a holy shockwave.");
                    DrawBookCard(new Rect(500f, 461f, 445f, 82f), "Electric Smite", "M4 + 9", "ULTIMATE. Acrobatic landing into sixteen 10m Ground Projectile Lightning Trails. No vertical/sky Lightning bolt visuals.");
                    passiveY = 550f;
                }
                else if (advancement == "Priest")
                {
                    DrawBookCard(new Rect(500f, 372f, 445f, 82f), "Grand Cross / Heaven's Judgement", "M4 + 7 / M4 + 8", "Grand Cross: SELF-CAST ONLY. Two slashes form a 15m-wide Ghost X that travels 25m in 6s with Persistent Lightning + Spirit damage and Spirit Burn. Heaven's Judgement: self/Cross Cast 10m Holy Beam barrage, 1.5s windup + 1.5s barrage, inflicts Frost.");
                    DrawBookCard(new Rect(500f, 461f, 445f, 82f), "Lightning Tempest", "M4 + 9", GetUltimateDescription(advancement));
                    passiveY = 550f;
                }
                DrawBookCard(
                    new Rect(500f, passiveY, 445f, 96f),
                    GetAdvancedPassiveName(advancement),
                    (advancement == "Paladin" || advancement == "Sword Master") ? "PASSIVE" : "M4 + R + PASSIVE",
                    GetAdvancedPassiveDescription(player, advancement)
                );
            }

            if (advancement == "Paladin")
            {
                GUI.Label(
                    new Rect(30f, 465f, 440f, 24f),
                    "PALADIN PASSIVE - CHOOSE ONCE / LOCKED UNTIL CLASS RESET",
                    _bookHeaderStyle
                );

                string passive = ReadPlayerData(player, PaladinPassiveKey);
                bool locked = !string.IsNullOrEmpty(passive);
                bool oldEnabled = GUI.enabled;
                GUI.enabled = !locked;

                if (GUI.Button(
                    new Rect(30f, 500f, 205f, 54f),
                    passive == "ElementalSavant" ? "Elemental Savant [LOCKED]" : "Elemental Savant"
                ))
                    SetPaladinPassiveChoice(player, "ElementalSavant");

                if (GUI.Button(
                    new Rect(245f, 500f, 205f, 54f),
                    passive == "HolyKnight" ? "Holy Knight [LOCKED]" : "Holy Knight"
                ))
                    SetPaladinPassiveChoice(player, "HolyKnight");

                GUI.enabled = oldEnabled;
            }
            if (GUI.Button(new Rect(815f, 635f, 130f, 38f), "Close"))
                ToggleSkillbook();

            GUI.DragWindow(new Rect(0f, 0f, 980f, 32f));
        }

        private void DrawBookCard(Rect rect, string name, string hotkey, string description)
        {
            GUI.Box(rect, "");

            GUI.Label(
                new Rect(rect.x + 15f, rect.y + 10f, rect.width - 30f, 25f),
                name,
                _bookHeaderStyle
            );

            GUI.Label(
                new Rect(rect.x + 15f, rect.y + 37f, rect.width - 30f, 20f),
                hotkey,
                _passiveStyle
            );

            GUI.Label(
                new Rect(rect.x + 15f, rect.y + 60f, rect.width - 30f, rect.height - 65f),
                description,
                _bookTextStyle
            );
        }

        private string GetAdvancedSkill5Name(string advancement)
        {
            if (advancement == "Sword Master") return "Crescent Cleave";
            if (advancement == "Mercenary") return "Bonecrusher";
            if (advancement == "Paladin") return "Ray of Hope";
            if (advancement == "Priest") return "Holy Relic";
            return "Advancement Skill";
        }

        private string GetAdvancedSkill5Id(string advancement)
        {
            if (advancement == "Sword Master") return "SwordMaster.CrescentCleave";
            if (advancement == "Mercenary") return "Mercenary.Bonecrusher";
            if (advancement == "Paladin") return "Paladin.RayOfHope";
            if (advancement == "Priest") return "Priest.HolyRelic";
            return "";
        }

        private string GetAdvancedSkillName(string advancement)
        {
            if (advancement == "Sword Master") return "Moonlight Splitter";
            if (advancement == "Mercenary") return "Stomp";
            if (advancement == "Paladin") return "Goddess Relic";
            if (advancement == "Priest") return "Lightning Relic";
            return "Advanced Skill";
        }

        private string GetAdvancedSkillDescription(string advancement)
        {
            if (advancement == "Sword Master") return "3 extra-wide Ghost Slash + Spirit waves, 0.5s apart. 50m configured travel; no Spirit Burn.";
            if (advancement == "Mercenary") return "0.5s first stomp: 3m Blunt impact. 1s later: 10m aftershock. Only Small enemies are Stunned.";
            if (advancement == "Paladin") return "1s Ground PAC Sky Summon. Huge cross, 7m AoE: Blunt + Lightning + Spirit Burn. Lands Physical, faces the caster, remains 3s and applies Judgement Mark.";
            if (advancement == "Priest") return "1.5s Ground PAC Signature Cross. 10m pulses every 1s for 16s: Lightning + Spirit, no DoT, inflicts Cripple. Recast relinquishes it; cooldown starts only when it leaves. Within 15m of Holy Relic it creates a fixed 15m Consecrated Ground.";
            return "";
        }

        private string GetAdvancedSkill5Description(string advancement)
        {
            if (advancement == "Sword Master") return "Five widening ground cleaves over 20m at the same travel speed, with a 1.5x wider cone. Slash + Spirit direct damage; no Burn.";
            if (advancement == "Mercenary") return "Acrobatic jump-slam; 10m Blunt AoE, Broken Bones + Cripple, and Stuns all enemy archetypes.";
            if (advancement == "Paladin") return "2s channel. Heal allies 30% max HP; +30% Attack Damage for 12s; enemies receive 8s Spirit Burn. 30s cooldown.";
            if (advancement == "Priest") return "Holy Relic: 10m buff/heal Cross for 16s, retaining its 2s pulse interval. Recast relinquishes it; cooldown starts only when it leaves. Within 15m of Lightning Relic it creates the fixed 15m Consecrated Ground.";
            return "";
        }

        private string GetAdvancedPassiveName(string advancement)
        {
            if (advancement == "Sword Master") return "The Way of the Sword";
            if (advancement == "Mercenary") return "Barbaric / Warfreak";
            if (advancement == "Paladin") return "Holy Trinity";
            if (advancement == "Priest") return "Bless Thy Sinners";
            return "Advanced Passive";
        }

        private string GetAdvancedPassiveDescription(Player player, string advancement)
        {
            if (advancement == "Sword Master")
                return "The Way of the Sword: +100% Attack Speed while exactly one Sword is equipped and the off-hand is empty. Passive only.";

            if (advancement == "Mercenary")
            {
                float lockout = Mathf.Max(0f, _mercFuryCooldownUntil - Time.time);
                string furyState = IsUnchainedFuryActive() ? "UNCHAINED ACTIVE" : (lockout > 0f ? "LOCKOUT " + lockout.ToString("0") + "s" : "FURY " + Mathf.RoundToInt(_mercFury).ToString() + "/100");
                return "Warfreak: dual-wield any two one-handed weapons; +125% Attack Speed with two-handed weapons. Barbaric keeps +20 Axes, +8% Attack Damage, +50 HP, aggro and Unchained Fury. " + furyState + ".";
            }

            if (advancement == "Paladin")
            {
                return "Holy Trinity: with a Club-type weapon and a Shield, +15 Clubs, no Armor movement penalty, and Slash / Pierce at least 50% of Blunt.";
            }

            if (advancement == "Priest")
                return "You and allies within 20m survive a lethal hit at 1 HP, then recover 50% HP over 6s with +50% Move Speed and -70% Stamina use. Self 20 min, each ally 40 min.\n\nBuckler: doubled Parry. A Parry grants 5s Hyper Armor and +35% Damage to your next skill, and releases a 10m Holy Shockwave (Spirit damage, Stuns Small and Big; 15s cooldown).";

            return "";
        }

        private string GetUltimateName(string advancement)
        {
            if (advancement == "Sword Master") return "Halfmoon Slash";
            if (advancement == "Mercenary") return "Whirlwind";
            if (advancement == "Paladin") return "Electric Smite";
            if (advancement == "Priest") return "Lightning Tempest";
            return "Ultimate";
        }

        private string GetUltimateDescription(string advancement)
        {
            if (advancement == "Sword Master") return "2s windup. Huge non-projectile Slash + Spirit hit, 10s Spirit Burn, Stun, then 0.5x afterimage slash.";
            if (advancement == "Mercenary") return "Spin 6s. Every 0.5s deals 0.5x held-weapon damage and carries its damage elements.";
            if (advancement == "Paladin") return "Acrobatic jump-slam followed by sixteen 10m Ground Projectile Lightning Trails. No sky-lightning bolt visuals; trails carry the persistent Lightning/Spirit effect.";
            if (advancement == "Priest") return "ULTIMATE. 10s, 8m random Lightning barrage (max 7 simultaneous). Direct damage is Lightning only; every hit refreshes Frost, Fire Burn, Spirit Burn, Zap and Expose.";
            return "";
        }

        private string GetAdvancedSkillId(string advancement)
        {
            if (advancement == "Sword Master") return "SwordMaster.MoonlightSplitter";
            if (advancement == "Mercenary") return "Mercenary.Stomp";
            if (advancement == "Paladin") return "Paladin.GoddessRelic";
            if (advancement == "Priest") return "Priest.LightningRelic";
            return "";
        }

        private string GetPassiveActiveId(string advancement)
        {
            if (advancement == "Priest") return "Priest.GrandSigil";
            return "";
        }

        private string GetUltimateId(string advancement)
        {
            if (advancement == "Sword Master") return "SwordMaster.HalfmoonSlash";
            if (advancement == "Mercenary") return "Mercenary.Whirlwind";
            if (advancement == "Paladin") return "Paladin.ElectricSmite";
            if (advancement == "Priest") return "Priest.LightningTempest";
            return "";
        }

        private void EnsureUiStyles()
        {
            if (_titleStyle != null)
                return;

            _slotReadyTex = MakeTexture(new Color(0.07f, 0.08f, 0.11f, 0.58f));
            _slotCooldownTex = MakeTexture(new Color(0.06f, 0.06f, 0.08f, 0.72f));
            _slotLockedTex = MakeTexture(new Color(0.03f, 0.03f, 0.05f, 0.62f));

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.fontSize = 17;
            _titleStyle.fontStyle = FontStyle.Bold;

            _slotStyle = new GUIStyle(GUI.skin.box);
            _slotStyle.normal.background = _slotReadyTex;
            _slotStyle.alignment = TextAnchor.MiddleCenter;
            _slotStyle.fontSize = 17;
            _slotStyle.fontStyle = FontStyle.Bold;
            _slotStyle.normal.textColor = Color.white;

            _slotLockedStyle = new GUIStyle(GUI.skin.box);
            _slotLockedStyle.normal.background = _slotLockedTex;
            _slotLockedStyle.alignment = TextAnchor.MiddleCenter;
            _slotLockedStyle.fontSize = 16;
            _slotLockedStyle.fontStyle = FontStyle.Bold;
            _slotLockedStyle.normal.textColor = Color.white;

            _passiveStyle = new GUIStyle(GUI.skin.label);
            _passiveStyle.alignment = TextAnchor.MiddleCenter;
            _passiveStyle.fontSize = 12;
            _passiveStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1f);

            _bookHeaderStyle = new GUIStyle(GUI.skin.label);
            _bookHeaderStyle.fontSize = 17;
            _bookHeaderStyle.fontStyle = FontStyle.Bold;
            _bookHeaderStyle.normal.textColor = Color.white;

            _bookTextStyle = new GUIStyle(GUI.skin.label);
            _bookTextStyle.fontSize = 13;
            _bookTextStyle.wordWrap = true;
            _bookTextStyle.normal.textColor = new Color(0.94f, 0.94f, 0.94f, 1f);

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

            // Immortal Heroes near-final art direction: warm ivory parchment, sacred blue Cleric,
            // muted rose Paladin, dark navy framing, and restrained gold filigree.
            _treeMainTex = MakeVerticalGradient(new Color(0.955f, 0.925f, 0.85f, 0.998f), new Color(0.86f, 0.81f, 0.71f, 0.998f), 72);
            _treeClassAreaTex = MakeVerticalGradient(new Color(0.86f, 0.91f, 0.94f, 0.99f), new Color(0.74f, 0.83f, 0.89f, 0.985f), 72);
            _treeAdvAreaTex = MakeVerticalGradient(new Color(0.95f, 0.89f, 0.84f, 0.99f), new Color(0.89f, 0.80f, 0.78f, 0.985f), 72);
            _treeClassHeaderTex = MakeVerticalGradient(new Color(0.055f, 0.20f, 0.33f, 0.995f), new Color(0.025f, 0.10f, 0.18f, 0.995f), 48);
            _treeAdvHeaderTex = MakeVerticalGradient(new Color(0.50f, 0.14f, 0.22f, 0.995f), new Color(0.31f, 0.065f, 0.12f, 0.995f), 48);
            _treeHotbarTex = MakeVerticalGradient(new Color(0.055f, 0.105f, 0.15f, 0.995f), new Color(0.025f, 0.055f, 0.085f, 0.995f), 48);
            _treeNodeInnerTex = MakeVerticalGradient(new Color(0.095f, 0.115f, 0.155f, 1f), new Color(0.045f, 0.055f, 0.085f, 1f), 32);
            _treeGoldTex = MakeTexture(Color.white);
            _treeShadowTex = MakeTexture(new Color(0f, 0f, 0f, 0.24f));
            _treeGoldGlowTex = MakeRadialGlow(new Color(0.98f, 0.74f, 0.23f, 0.38f), new Color(0.98f, 0.74f, 0.23f, 0f), 72);
            _treeMagentaGlowTex = MakeRadialGlow(new Color(0.95f, 0.22f, 0.73f, 0.30f), new Color(0.95f, 0.22f, 0.73f, 0f), 72);
            _treeMaroonGlowTex = MakeRadialGlow(new Color(0.76f, 0.18f, 0.25f, 0.32f), new Color(0.76f, 0.18f, 0.25f, 0f), 72);
            _treeBlueGlowTex = MakeRadialGlow(new Color(0.18f, 0.45f, 0.95f, 0.22f), new Color(0.18f, 0.45f, 0.95f, 0f), 72);

            _treeWindowStyle = new GUIStyle(GUI.skin.window);
            _treeWindowStyle.normal.background = _treeMainTex;
            _treeWindowStyle.padding = new RectOffset(0, 0, 0, 0);

            _treeTitleStyle = new GUIStyle(GUI.skin.label);
            _treeTitleStyle.fontSize = 28;
            _treeTitleStyle.fontStyle = FontStyle.Bold;
            _treeTitleStyle.alignment = TextAnchor.MiddleLeft;
            _treeTitleStyle.normal.textColor = new Color(0.97f, 0.88f, 0.66f, 1f);

            _treeHeaderStyle = new GUIStyle(GUI.skin.label);
            _treeHeaderStyle.fontSize = 25;
            _treeHeaderStyle.fontStyle = FontStyle.Bold;
            _treeHeaderStyle.alignment = TextAnchor.MiddleCenter;
            _treeHeaderStyle.normal.textColor = new Color(0.97f, 0.91f, 0.77f, 1f);

            _treeHeaderEmblemStyle = new GUIStyle(GUI.skin.label);
            _treeHeaderEmblemStyle.fontSize = 24;
            _treeHeaderEmblemStyle.fontStyle = FontStyle.Bold;
            _treeHeaderEmblemStyle.alignment = TextAnchor.MiddleCenter;
            _treeHeaderEmblemStyle.normal.textColor = new Color(0.95f, 0.76f, 0.34f, 1f);

            _treeSubHeaderStyle = new GUIStyle(GUI.skin.label);
            _treeSubHeaderStyle.fontSize = 15;
            _treeSubHeaderStyle.fontStyle = FontStyle.Bold;
            _treeSubHeaderStyle.alignment = TextAnchor.MiddleCenter;
            _treeSubHeaderStyle.normal.textColor = new Color(0.94f, 0.80f, 0.49f, 1f);

            _treeNodeIconStyle = new GUIStyle(GUI.skin.label);
            _treeNodeIconStyle.fontSize = 21;
            _treeNodeIconStyle.fontStyle = FontStyle.Bold;
            _treeNodeIconStyle.alignment = TextAnchor.MiddleCenter;
            _treeNodeIconStyle.normal.textColor = Color.white;

            _treeUltimateIconStyle = new GUIStyle(_treeNodeIconStyle);
            _treeUltimateIconStyle.fontSize = 31;
            _treeUltimateIconStyle.normal.textColor = new Color(1f, 0.92f, 0.72f, 1f);

            _treeNodeNameStyle = new GUIStyle(GUI.skin.label);
            _treeNodeNameStyle.fontSize = 13;
            _treeNodeNameStyle.fontStyle = FontStyle.Bold;
            _treeNodeNameStyle.alignment = TextAnchor.MiddleCenter;
            _treeNodeNameStyle.normal.textColor = new Color(0.18f, 0.14f, 0.11f, 1f);

            _treeNamePlateDarkStyle = new GUIStyle(_treeNodeNameStyle);
            _treeNamePlateDarkStyle.fontSize = 12;
            _treeNamePlateDarkStyle.normal.textColor = new Color(0.16f, 0.12f, 0.09f, 1f);

            _treeNamePlateLightStyle = new GUIStyle(_treeNodeNameStyle);
            _treeNamePlateLightStyle.fontSize = 13;
            _treeNamePlateLightStyle.normal.textColor = new Color(0.98f, 0.90f, 0.72f, 1f);

            _treeRankStyle = new GUIStyle(GUI.skin.label);
            _treeRankStyle.fontSize = 10;
            _treeRankStyle.fontStyle = FontStyle.Bold;
            _treeRankStyle.alignment = TextAnchor.MiddleCenter;
            _treeRankStyle.normal.textColor = new Color(0.94f, 0.80f, 0.49f, 1f);

            _treeTinyStyle = new GUIStyle(GUI.skin.label);
            _treeTinyStyle.fontSize = 10;
            _treeTinyStyle.alignment = TextAnchor.MiddleCenter;
            _treeTinyStyle.wordWrap = true;
            _treeTinyStyle.normal.textColor = new Color(0.91f, 0.86f, 0.76f, 1f);

            Font serif = FindValheimSerifFont();

            _treeFooterKeyStyle = new GUIStyle(GUI.skin.label);
            if (serif != null) _treeFooterKeyStyle.font = serif;
            _treeFooterKeyStyle.fontSize = 12;
            _treeFooterKeyStyle.fontStyle = FontStyle.Bold;
            _treeFooterKeyStyle.alignment = TextAnchor.MiddleCenter;
            _treeFooterKeyStyle.wordWrap = false;
            _treeFooterKeyStyle.clipping = TextClipping.Overflow;
            _treeFooterKeyStyle.normal.textColor = new Color(0.93f, 0.84f, 0.62f, 1f);

            _treeFooterGraceStyle = new GUIStyle(_treeFooterKeyStyle);
            _treeFooterGraceStyle.fontSize = 13;
            _treeFooterGraceStyle.normal.textColor = new Color(1f, 0.86f, 0.48f, 1f);

            _treeFooterKeyHoverStyle = new GUIStyle(_treeFooterKeyStyle);
            _treeFooterKeyHoverStyle.normal.textColor = new Color(1f, 0.95f, 0.75f, 1f);

            _treeFooterKeyCaptureStyle = new GUIStyle(_treeFooterKeyStyle);
            _treeFooterKeyCaptureStyle.fontSize = 10;
            _treeFooterKeyCaptureStyle.normal.textColor = new Color(1f, 0.80f, 0.32f, 1f);

            _treeFooterPendingStyle = new GUIStyle(_treeFooterKeyStyle);
            _treeFooterPendingStyle.fontSize = 10;
            _treeFooterPendingStyle.normal.textColor = new Color(0.93f, 0.86f, 0.70f, 1f);

            _treeFooterConfirmStyle = new GUIStyle(_treeFooterKeyStyle);
            _treeFooterConfirmStyle.fontSize = 17;
            _treeFooterConfirmStyle.normal.textColor = new Color(0.98f, 0.82f, 0.42f, 1f);

            _treeFooterConfirmHoverStyle = new GUIStyle(_treeFooterConfirmStyle);
            _treeFooterConfirmHoverStyle.normal.textColor = new Color(1f, 0.95f, 0.75f, 1f);

            _treeTinyLeftStyle = new GUIStyle(_treeTinyStyle);
            _treeTinyLeftStyle.alignment = TextAnchor.MiddleLeft;
            _treeTinyLeftStyle.normal.textColor = new Color(0.84f, 0.82f, 0.76f, 1f);

            _treeHotkeyStyle = new GUIStyle(_treeTinyStyle);
            _treeHotkeyStyle.fontSize = 10;
            _treeHotkeyStyle.fontStyle = FontStyle.Bold;
            _treeHotkeyStyle.alignment = TextAnchor.MiddleCenter;
            _treeHotkeyStyle.normal.textColor = new Color(0.23f, 0.17f, 0.11f, 0.96f);

            _treeWatermarkStyle = new GUIStyle(GUI.skin.label);
            _treeWatermarkStyle.fontSize = 150;
            _treeWatermarkStyle.fontStyle = FontStyle.Bold;
            _treeWatermarkStyle.alignment = TextAnchor.MiddleCenter;
            _treeWatermarkStyle.normal.textColor = Color.white;

            _treeTooltipTitleStyle = new GUIStyle(_treeSubHeaderStyle);
            _treeTooltipTitleStyle.alignment = TextAnchor.UpperLeft;
            _treeTooltipTitleStyle.fontSize = 14;

            _treeTooltipBodyStyle = new GUIStyle(GUI.skin.label);
            // v0.18.2: Valheim's own serif for the whole skill sheet.
            Font tooltipSerif = FindValheimFont("AveriaSerifLibre-Regular");
            if (tooltipSerif == null) tooltipSerif = FindValheimSerifFont();
            if (tooltipSerif != null) _treeTooltipBodyStyle.font = tooltipSerif;
            Font titleSerif = FindValheimSerifFont();
            if (titleSerif != null) _treeTooltipTitleStyle.font = titleSerif;
            _treeTooltipBodyStyle.fontSize = 13;
            _treeTooltipBodyStyle.wordWrap = true;
            _treeTooltipBodyStyle.alignment = TextAnchor.UpperLeft;
            _treeTooltipBodyStyle.normal.textColor = new Color(0.92f, 0.92f, 0.89f, 1f);
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private Texture2D MakeVerticalGradient(Color top, Color bottom, int height)
        {
            int h = Mathf.Max(2, height);
            Texture2D texture = new Texture2D(2, h);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color c = Color.Lerp(top, bottom, t);
                texture.SetPixel(0, y, c);
                texture.SetPixel(1, y, c);
            }
            texture.Apply();
            return texture;
        }

        private Texture2D MakeRadialGlow(Color center, Color edge, int size)
        {
            int s = Mathf.Max(8, size);
            Texture2D texture = new Texture2D(s, s);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Vector2 middle = new Vector2((s - 1) * 0.5f, (s - 1) * 0.5f);
            float maxDistance = Mathf.Max(1f, middle.magnitude);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), middle) / maxDistance;
                    float t = Mathf.Clamp01(d);
                    texture.SetPixel(x, y, Color.Lerp(center, edge, t * t));
                }
            }
            texture.Apply();
            return texture;
        }

        private string FormatHotkey(KeyCode modifier, KeyCode key)
        {
            string modifierText = modifier.ToString();

            if (modifier == KeyCode.Mouse3)
                modifierText = "M4";
            else if (modifier == KeyCode.Mouse4)
                modifierText = "M5";

            string keyText = key.ToString();

            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
                keyText = ((int)key - (int)KeyCode.Alpha0).ToString();

            return modifierText + " + " + keyText;
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

        private class DamageSnapshot
        {
            public float Blunt;
            public float Slash;
            public float Pierce;
            public float Fire;
            public float Frost;
            public float Lightning;
            public float Poison;
            public float Spirit;

            public void Scale(float factor)
            {
                Blunt *= factor;
                Slash *= factor;
                Pierce *= factor;
                Fire *= factor;
                Frost *= factor;
                Lightning *= factor;
                Poison *= factor;
                Spirit *= factor;
            }

            public float Total()
            {
                return Blunt + Slash + Pierce + Fire + Frost + Lightning + Poison + Spirit;
            }
        }

        private class BarrierState
        {
            public float HP;
            public float Armor;
            public float EndTime;
        }
    }
}
