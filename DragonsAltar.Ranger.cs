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

// v0.24.0 RANGER (Immortal Heroes Framework 4): Ranger Class (Wildborn, Piercing Arrow, Tumble Shot,
// Snare Trap), Acrobat (Windstep, Tailwind, 5 skills + Furious Winds) and, since v0.24.1, Bowmaster
// (Deadeye, Hawk's Vigil, 5 skills + Starfall Volley), every Ascended version.
// v0.24.1 Wildborn rework: Left Click = 4-hit quick-shot chain at full-draw range, Right Click = the
// vanilla charged shot (Left Click releases, letting go cancels), no Block / Shields, infinite ammo,
// damage = 50% Bow + 50% of the ammo only with a full stack (100) of it.
// Every skill is cast from the universal tree hotbar: Advanced routes unknown ids here through
// DragonCombat.TryExternalCast, cooldowns come back through ExternalCooldown.
namespace DragonsAltarRanger
{
    internal class RangerArrowDamage
    {
        public float Blunt, Slash, Pierce, Fire, Frost, Lightning, Poison, Spirit;
        public float Total() { return Blunt + Slash + Pierce + Fire + Frost + Lightning + Poison + Spirit; }
    }

    internal class SnareTrap
    {
        public Vector3 Position;
        public float ExpireAt;
        public GameObject Visual;
    }

    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("albedo.customclasses", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.skills", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.combatruntime", BepInDependency.DependencyFlags.HardDependency)]
    public class RangerPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "albedo.customclasses.ranger";
        public const string ModName = "Dragon's Altar - Ranger";
        public const string ModVersion = "0.25.35";
        private const string ClassDataKey = "AlbedoCustomClasses.Class";
        private const string AdvancementDataKey = "AlbedoCustomClasses.Advancement";

        public static RangerPlugin Instance;

        private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        private readonly List<SnareTrap> _traps = new List<SnareTrap>();
        private readonly Dictionary<int, float> _tailwindUntil = new Dictionary<int, float>();
        private readonly Dictionary<int, bool> _noFallUntilGrounded = new Dictionary<int, bool>();

        private ConfigEntry<bool> _enableVfx;
        private ConfigEntry<bool> _testingForceCooldowns;
        private ConfigEntry<float> _testingCooldown;

        // Wildborn (Ranger's Blessing)
        private ConfigEntry<float> _rgIFrames, _wbCharged, _wbInterval, _wbFinisherDelay, _wsFallReduction, _wsStaminaUse, _arAscRadius, _eaAscClusters, _eaAscClusterRadius, _eaAscClusterPercent, _psCone;
        private ConfigEntry<float> _wbBows, _wbDodge, _wbFall, _wbDrawSpeed, _wbBowPercent, _wbAmmoPercent, _wbFullStack, _wbChainBonus, _wbChainReset, _wbQuickStamina;
        // Class skills
        private ConfigEntry<float> _paCooldown, _paStamina, _paDamage, _paRange, _paSpeed, _paCripple;
        private ConfigEntry<float> _paAscRange, _paAscWidth, _paAscExpose;
        private ConfigEntry<float> _tsCooldown, _tsStamina, _tsDamage, _tsArrows, _tsFan, _tsDistance, _tsRange;
        private ConfigEntry<float> _tsAscArrows, _tsAscGust, _tsAscCripple;
        private ConfigEntry<float> _snCooldown, _snStamina, _snDamage, _snRange, _snTrigger, _snLifetime, _snMax, _snCripple;
        // Windstep (Acrobat Mastery)
        private ConfigEntry<float> _wsDodge, _wsJump, _wsJumpStamina, _wsRefund, _wsRefundMax;
        // Acrobat skills
        private ConfigEntry<float> _gvCooldown, _gvStamina, _gvDamage, _gvArrows, _gvFan, _gvLeap, _gvRange, _gvPush, _gvAscDelay;
        private ConfigEntry<float> _cyCooldown, _cyStamina, _cyDamage, _cyRange, _cyTravel, _cyRadius, _cyInterval, _cyPull;
        private ConfigEntry<float> _cyAscSplits, _cyAscRange, _cyAscTravel, _cyAscRadius;
        private ConfigEntry<float> _sdCharges, _sdRecharge, _sdStamina, _sdDamage, _sdDistance, _sdRadius, _sdAscCharges, _sdAscDelay, _sdAscPercent;
        private ConfigEntry<float> _sbCooldown, _sbStamina, _sbDamage, _sbRange, _sbHeight, _sbRadius, _sbDuration, _sbInterval, _sbAscRadius;
        private ConfigEntry<float> _rcCooldown, _rcStamina, _rcDamage, _rcRange, _rcBounces, _rcBounceRange, _rcBonus, _rcAscBounces, _rcAscBlast, _rcAscBlastPercent;
        private ConfigEntry<float> _fwBarrier, _fwSmallPush, _fwBigPush, _fwAscBarrier;
        private ConfigEntry<float> _fwCooldown, _fwStamina, _fwDuration, _fwRadius, _fwInterval, _fwSlash, _fwDot, _fwDotDuration, _fwAscDuration, _fwAscRadius, _fwAscBurst;
        private ConfigEntry<float> _twCooldown, _twDuration, _twRadius, _twMove, _twJump;
        // Bowmaster
        private ConfigEntry<float> _deFocusMax, _deFocusDamage, _deFocusRange, _deFullCharge, _deReload;
        private ConfigEntry<float> _hvCooldown, _hvDuration, _hvRadius, _hvReveal, _hvRanged;
        private ConfigEntry<float> _bsCooldown, _bsStamina, _bsDamage, _bsStack, _bsRange, _bsWidth, _bsWidthStack, _bsAscLine, _bsAscLinePercent;
        private ConfigEntry<float> _arCooldown, _arStamina, _arDamage, _arRange, _arRadius, _arDuration, _arInterval, _arCripple, _arAscFreezeHits, _arAscFreeze;
        private ConfigEntry<float> _psCooldown, _psStamina, _psDamage, _psRange, _psSmall, _psBig, _psBonus, _psAscChains, _psAscChainRange;
        private ConfigEntry<float> _eaCooldown, _eaStamina, _eaDamage, _eaRange, _eaRadius, _eaBurn, _eaBurnDuration, _eaAscField, _eaAscFieldPercent;
        private ConfigEntry<float> _saCooldown, _saStamina, _saDamage, _saRange, _saSplits, _saCone, _saVolleys, _saInterval, _saAscVolleys, _saAscInterval, _saAscBurn, _saAscBurnDuration;
        private ConfigEntry<float> _sfTick;
        private ConfigEntry<float> _sfCooldown, _sfStamina, _sfRange, _sfRadius, _sfChannel, _sfDuration, _sfInterval, _sfImpact, _sfDamage, _sfAscRadius, _sfAscDamage;

        // Runtime state
        private int _diveCharges = -1;
        private float _diveNextCharge;
        private bool _doubleJumpUsed;
        private float _refundLeft;
        private float _dodgeBase = -1f;
        private bool _hovering;

        private void Awake()
        {
            Instance = this;
            _enableVfx = Config.Bind("General", "EnableVfx", true, "Ranger skill VFX.");
            _testingForceCooldowns = Config.Bind("Testing", "ForceCooldowns", false, "Dev Mode: force Ranger cooldowns to the testing value.");
            _testingCooldown = Config.Bind("Testing", "CooldownSeconds", 5f, "Testing cooldown.");

            const string wb = "Ranger Blessing";
            _wbBows = Config.Bind(wb, "BowsSkillBonus", 20f, "Wildborn: +Bows skill.");
            _wbDodge = Config.Bind(wb, "DodgeSkillBonus", 20f, "Wildborn: +Dodge skill (when the game has a Dodge skill).");
            _wbFall = Config.Bind(wb, "FallDamageReductionPercent", 30f, "Wildborn: less fall damage.");
            _wbDrawSpeed = Config.Bind(wb, "DrawSpeedPercent_v0243", 0f, "Charged-shot draw time reduction (removed in v0.24.3 for balance); the Bows skill still shortens the draw (Bows 100 = instant).");
            _wbCharged = Config.Bind(wb, "ChargedShotPercent", 120f, "Right Click charged shots deal this %.");
            _wbInterval = Config.Bind(wb, "ChainInterval", 0.5f, "Left Click: seconds between shots.");
            _wbFinisherDelay = Config.Bind(wb, "FinisherExtraSeconds", 1f, "The 4th shot (finisher) takes this much longer before the next shot.");
            _wbBowPercent = Config.Bind(wb, "BowDamagePercent", 50f, "Rangers deal this % of the Bow / Crossbow damage.");
            _wbAmmoPercent = Config.Bind(wb, "AmmoDamagePercent", 50f, "...plus this % of the arrow / bolt damage, only with a full stack of it.");
            _wbFullStack = Config.Bind(wb, "FullStack", 100f, "Ammo count needed for the ammo damage bonus. Ammo is never consumed.");
            _wbChainBonus = Config.Bind(wb, "ChainFourthHitPercent", 150f, "Left Click chain: the 4th shot deals this %.");
            _wbChainReset = Config.Bind(wb, "ChainResetSeconds", 2f, "The chain resets after this long without a shot.");
            _wbQuickStamina = Config.Bind(wb, "QuickShotStaminaPercent", 50f, "Left Click shots use this % of the normal Stamina.");
            _rgIFrames = Config.Bind(wb, "BackJumpIFrames", 0.5f, "Tumble Shot / Gale Volley back-jumps: seconds of i-frames (no damage, knockback or stagger).");

            const string pa = "Ranger Piercing Arrow";
            _paCooldown = Config.Bind(pa, "Cooldown", 8f, "Seconds.");
            _paStamina = Config.Bind(pa, "StaminaCost", 20f, "Stamina.");
            _paDamage = Config.Bind(pa, "DamagePercent", 160f, "% of your bow + arrow damage.");
            _paRange = Config.Bind(pa, "Range", 40f, "Laser Projectile range (m).");
            _paSpeed = Config.Bind(pa, "Speed", 70f, "m/s.");
            _paCripple = Config.Bind(pa, "CrippleDuration", 3f, "The first enemy hit is Crippled.");
            const string paa = "Ranger Piercing Arrow Ascended";
            _paAscRange = Config.Bind(paa, "Range", 60f, "Bowmaster's Ascended Class skill: range (m).");
            _paAscWidth = Config.Bind(paa, "EndWidth", 3f, "The arrow widens to this width (m) at max range.");
            _paAscExpose = Config.Bind(paa, "ExposeDuration", 6f, "Every enemy hit is Exposed.");

            const string ts = "Ranger Tumble Shot";
            _tsCooldown = Config.Bind(ts, "Cooldown", 7f, "Seconds.");
            _tsStamina = Config.Bind(ts, "StaminaCost", 18f, "Stamina.");
            _tsDamage = Config.Bind(ts, "DamagePercent", 70f, "Per arrow, % of your bow + arrow damage.");
            _tsArrows = Config.Bind(ts, "Arrows", 3f, "Arrows in the fan.");
            _tsFan = Config.Bind(ts, "FanDegrees", 24f, "Fan width.");
            _tsDistance = Config.Bind(ts, "FlipDistance_v0241", 2f, "Swift backflip distance (m), always straight back.");
            _tsRange = Config.Bind(ts, "Range", 35f, "Arrow range (m).");
            const string tsa = "Ranger Tumble Shot Ascended";
            _tsAscArrows = Config.Bind(tsa, "Arrows", 5f, "Acrobat's Ascended Class skill: arrows.");
            _tsAscGust = Config.Bind(tsa, "GustRadius", 4f, "Gust left where you took off (m).");
            _tsAscCripple = Config.Bind(tsa, "GustCrippleDuration", 3f, "Enemies in the gust are Crippled. A kill resets the cooldown.");

            const string sn = "Ranger Snare Trap";
            _snCooldown = Config.Bind(sn, "Cooldown", 12f, "Seconds (from placement).");
            _snStamina = Config.Bind(sn, "StaminaCost", 15f, "Stamina.");
            _snDamage = Config.Bind(sn, "DamagePercent", 50f, "% of your bow + arrow damage when it snaps.");
            _snRange = Config.Bind(sn, "Range", 25f, "Ground PAC range (m).");
            _snTrigger = Config.Bind(sn, "TriggerRadius", 4f, "m.");
            _snLifetime = Config.Bind(sn, "Lifetime", 60f, "Seconds.");
            _snMax = Config.Bind(sn, "MaxTraps", 2f, "Active traps (the oldest is removed).");
            _snCripple = Config.Bind(sn, "CrippleDuration", 4f, "Small: Stunned + Crippled; Big: Crippled; Bosses: Crippled.");

            const string ws = "Acrobat Windstep";
            _wsDodge = Config.Bind(ws, "DodgeStaminaReductionPercent", 50f, "Dodge costs less Stamina.");
            _wsFallReduction = Config.Bind(ws, "FallDamageReductionPercent", 75f, "Acrobat fall damage reduction (replaces Wildborn's). A fall never kills: you are left at 1 HP.");
            _wsStaminaUse = Config.Bind(ws, "StaminaUseReductionPercent", 35f, "All Stamina usage reduced.");
            _wsJump = Config.Bind(ws, "AirJumpSpeed", 9f, "Second jump in mid-air: upward speed (m/s).");
            _wsJumpStamina = Config.Bind(ws, "AirJumpStamina", 10f, "Stamina for the second jump.");
            _wsRefund = Config.Bind(ws, "HitRefundSeconds", 1f, "Each enemy hit by a skill takes this off your shortest running cooldown.");
            _wsRefundMax = Config.Bind(ws, "MaxRefundPerCast", 3f, "Seconds per cast at most.");

            const string gv = "Acrobat Gale Volley";
            _gvCooldown = Config.Bind(gv, "Cooldown", 9f, "Seconds.");
            _gvStamina = Config.Bind(gv, "StaminaCost", 22f, "Stamina.");
            _gvDamage = Config.Bind(gv, "DamagePercent", 60f, "Per arrow.");
            _gvArrows = Config.Bind(gv, "Arrows", 7f, "Arrows in the fan.");
            _gvFan = Config.Bind(gv, "FanDegrees", 60f, "Fan width.");
            _gvLeap = Config.Bind(gv, "LeapDistance_v0242", 2f, "Swift backward leap (m) in 0.3s, with i-frames.");
            _gvRange = Config.Bind(gv, "Range", 35f, "Arrow range (m).");
            _gvPush = Config.Bind(gv, "SmallKnockback", 25f, "Push force on Small enemies.");
            _gvAscDelay = Config.Bind("Acrobat Gale Volley Ascended", "SecondFanDelay", 0.25f, "A second fan arcs over the first.");

            const string cy = "Acrobat Cyclone Arrow";
            _cyCooldown = Config.Bind(cy, "Cooldown", 12f, "Seconds.");
            _cyStamina = Config.Bind(cy, "StaminaCost", 25f, "Stamina.");
            _cyDamage = Config.Bind(cy, "DamagePercent", 35f, "Per hit.");
            _cyRange = Config.Bind(cy, "Range", 30f, "Laser Projectile range (m).");
            _cyTravel = Config.Bind(cy, "TravelTime_v0250", 6f, "Seconds to cover the range (slow, so enemies take several ticks).");
            _cyRadius = Config.Bind(cy, "Radius_v0250", 4f, "Pull / hit radius (m).");
            _cyInterval = Config.Bind(cy, "HitInterval", 0.3f, "Seconds.");
            _cyPull = Config.Bind(cy, "PullStrength", 6f, "Pull on Small enemies.");
            const string cya = "Acrobat Cyclone Arrow Ascended";
            _cyAscSplits = Config.Bind(cya, "Splits", 3f, "Smaller cyclones split off when it first catches an enemy.");
            _cyAscRange = Config.Bind(cya, "SplitRange", 10f, "m.");
            _cyAscTravel = Config.Bind(cya, "SplitTravelTime", 1.5f, "Seconds.");
            _cyAscRadius = Config.Bind(cya, "SplitRadius", 2f, "m.");

            const string sd = "Acrobat Swallow Dive";
            _sdCharges = Config.Bind(sd, "Charges", 2f, "Charges.");
            _sdRecharge = Config.Bind(sd, "RechargeSeconds", 8f, "Per charge.");
            _sdStamina = Config.Bind(sd, "StaminaCost", 15f, "Stamina.");
            _sdDamage = Config.Bind(sd, "DamagePercent", 90f, "Every enemy passed.");
            _sdDistance = Config.Bind(sd, "Distance", 12f, "Free Aim dash distance (m).");
            _sdRadius = Config.Bind(sd, "HitRadius", 2.5f, "m.");
            const string sda = "Acrobat Swallow Dive Ascended";
            _sdAscCharges = Config.Bind(sda, "Charges", 3f, "Charges.");
            _sdAscDelay = Config.Bind(sda, "EchoDelay", 0.5f, "The wind slash hits again after this.");
            _sdAscPercent = Config.Bind(sda, "EchoPercent", 50f, "% of the dash.");

            const string sb = "Acrobat Skyfall Barrage";
            _sbCooldown = Config.Bind(sb, "Cooldown", 16f, "Seconds.");
            _sbStamina = Config.Bind(sb, "StaminaCost", 30f, "Stamina.");
            _sbDamage = Config.Bind(sb, "TickPercent", 30f, "Per tick, every enemy in the circle.");
            _sbRange = Config.Bind(sb, "Range", 35f, "Ground PAC range (m).");
            _sbHeight = Config.Bind(sb, "JumpHeight", 15f, "m.");
            _sbRadius = Config.Bind(sb, "Radius", 10f, "m.");
            _sbDuration = Config.Bind(sb, "BarrageDuration", 2f, "Seconds hovering.");
            _sbInterval = Config.Bind(sb, "TickInterval", 0.25f, "Seconds.");
            _sbAscRadius = Config.Bind("Acrobat Skyfall Barrage Ascended", "Radius", 14f, "m; the circle follows your aim while you hover.");

            const string rc = "Acrobat Ricochet Arrow";
            _rcCooldown = Config.Bind(rc, "Cooldown", 10f, "Seconds.");
            _rcStamina = Config.Bind(rc, "StaminaCost", 20f, "Stamina.");
            _rcDamage = Config.Bind(rc, "DamagePercent", 80f, "First hit.");
            _rcRange = Config.Bind(rc, "Range", 40f, "m.");
            _rcBounces = Config.Bind(rc, "Bounces", 6f, "Enemies after the first.");
            _rcBounceRange = Config.Bind(rc, "BounceRange", 10f, "m.");
            _rcBonus = Config.Bind(rc, "BounceBonusPercent", 10f, "+damage per bounce.");
            const string rca = "Acrobat Ricochet Arrow Ascended";
            _rcAscBounces = Config.Bind(rca, "Bounces", 10f, "Enemies after the first.");
            _rcAscBlast = Config.Bind(rca, "FinalBlastRadius", 4f, "m.");
            _rcAscBlastPercent = Config.Bind(rca, "FinalBlastPercent", 100f, "%.");

            const string fw = "Acrobat Furious Winds";
            _fwCooldown = Config.Bind(fw, "Cooldown", 90f, "Seconds.");
            _fwStamina = Config.Bind(fw, "StaminaCost", 40f, "Stamina.");
            _fwDuration = Config.Bind(fw, "Duration", 3f, "Seconds; you stand in the storm.");
            _fwRadius = Config.Bind(fw, "Radius", 10f, "Attack radius (m): every enemy inside is cut.");
            _fwBarrier = Config.Bind(fw, "BarrierRadius", 7f, "Wind barrier (m): enemies can't come closer and are slowly pushed out to it; enemy projectiles inside are blown away.");
            _fwSmallPush = Config.Bind(fw, "SmallPushSeconds", 5f, "A Small enemy 1m from you reaches the barrier after this long.");
            _fwBigPush = Config.Bind(fw, "BigPushSeconds", 8f, "Big enemies and Bosses: same, slower.");
            _fwInterval = Config.Bind(fw, "TickInterval", 0.25f, "Seconds between leaf cuts.");
            _fwSlash = Config.Bind(fw, "SlashPercent", 25f, "Each tick: % of your Ranger damage as Slash to every enemy at the barrier.");
            _fwDot = Config.Bind(fw, "SpiritDotPercentPerStack", 4f, "Spirit DoT per stack every 0.5s (% of your Ranger damage). Every tick adds a stack.");
            _fwDotDuration = Config.Bind(fw, "SpiritDotDuration", 6f, "Seconds, refreshed on every hit.");
            const string fwa = "Acrobat Furious Winds Ascended";
            _fwAscDuration = Config.Bind(fwa, "Duration", 5f, "Seconds.");
            _fwAscRadius = Config.Bind(fwa, "Radius", 14f, "Attack radius (m).");
            _fwAscBarrier = Config.Bind(fwa, "BarrierRadius", 10f, "Wind barrier (m).");
            _fwAscBurst = Config.Bind(fwa, "FinalGalePercent", 150f, "The storm ends in a gale that launches Small enemies.");

            const string tw = "Acrobat Tailwind";
            _twCooldown = Config.Bind(tw, "Cooldown", 600f, "Seconds (Grace, free).");
            _twDuration = Config.Bind(tw, "Duration", 120f, "Seconds.");
            _twRadius = Config.Bind(tw, "Radius", 10f, "Snapshot radius (m).");
            _twMove = Config.Bind(tw, "MoveSpeedPercent", 50f, "+Move Speed.");
            _twJump = Config.Bind(tw, "JumpSkillBonus", 30f, "+Jump skill. No fall damage.");

            const string de = "Bowmaster Deadeye";
            _deFocusMax = Config.Bind(de, "FocusMax", 5f, "Focus stacks (1 per second standing still; moving drains them over 2s).");
            _deFocusDamage = Config.Bind(de, "DamagePerFocusPercent", 8f, "+damage per stack.");
            _deFocusRange = Config.Bind(de, "RangePerFocusPercent", 10f, "+range per stack.");
            _deFullCharge = Config.Bind(de, "FullChargeDamagePercent", 30f, "Fully charged shots deal this much more.");
            _deReload = Config.Bind(de, "CrossbowReloadReductionPercent", 75f, "Crossbow reload time reduction. Loaded Crossbows stay loaded when unequipped.");
            const string hv = "Bowmaster Hawks Vigil";
            _hvCooldown = Config.Bind(hv, "Cooldown", 600f, "Seconds (Grace, free).");
            _hvDuration = Config.Bind(hv, "Duration", 60f, "Seconds.");
            _hvRadius = Config.Bind(hv, "AllyRadius", 10f, "Snapshot radius for the ally buff (m).");
            _hvReveal = Config.Bind(hv, "RevealRadius", 60f, "Enemies within this range are marked (m).");
            _hvRanged = Config.Bind(hv, "RangedDamagePercent", 20f, "+ranged damage.");
            const string bs = "Bowmaster Ballista Shot";
            _bsCooldown = Config.Bind(bs, "Cooldown", 14f, "Seconds.");
            _bsStamina = Config.Bind(bs, "StaminaCost", 30f, "Stamina.");
            _bsDamage = Config.Bind(bs, "DamagePercent", 200f, "Uncharged shot.");
            _bsStack = Config.Bind(bs, "DamagePerStackPercent", 40f, "+damage per charge stack (3 stacks, 1 per second; hold past max to keep aiming).");
            _bsRange = Config.Bind(bs, "Range", 60f, "Laser Projectile range (m).");
            _bsWidth = Config.Bind(bs, "Width", 1f, "m.");
            _bsWidthStack = Config.Bind(bs, "WidthPerStack", 1f, "m per stack.");
            _bsAscLine = Config.Bind("Bowmaster Ballista Shot Ascended", "ShockwaveRadius_v0242", 5f, "Fully charged: every enemy it pierces erupts in a shockwave of this radius (m).");
            _bsAscLinePercent = Config.Bind("Bowmaster Ballista Shot Ascended", "ShockwavePercent", 60f, "% of the shot to everything around the pierced enemy.");
            const string ar = "Bowmaster Arrow Rain";
            _arCooldown = Config.Bind(ar, "Cooldown", 16f, "Seconds.");
            _arStamina = Config.Bind(ar, "StaminaCost", 30f, "Stamina.");
            _arDamage = Config.Bind(ar, "DamagePercent", 25f, "Per hit.");
            _arRange = Config.Bind(ar, "Range", 45f, "Ground PAC range (m).");
            _arRadius = Config.Bind(ar, "Radius_v0243", 6f, "m.");
            _arAscRadius = Config.Bind("Bowmaster Arrow Rain Ascended", "Radius", 8f, "m.");
            _arDuration = Config.Bind(ar, "Duration", 4f, "Seconds.");
            _arInterval = Config.Bind(ar, "HitInterval", 0.4f, "Seconds.");
            _arCripple = Config.Bind(ar, "CrippleDuration", 2f, "Refreshed per hit.");
            _arAscFreezeHits = Config.Bind("Bowmaster Arrow Rain Ascended", "HitsToFreeze", 3f, "Frozen arrows: Slow, then Freeze after this many hits.");
            _arAscFreeze = Config.Bind("Bowmaster Arrow Rain Ascended", "FreezeDuration", 2f, "Seconds (Bosses are slowed instead).");
            const string ps = "Bowmaster Pinning Shot";
            _psCooldown = Config.Bind(ps, "Cooldown", 12f, "Seconds.");
            _psStamina = Config.Bind(ps, "StaminaCost", 20f, "Stamina.");
            _psDamage = Config.Bind(ps, "DamagePercent", 150f, "%.");
            _psRange = Config.Bind(ps, "Range_v0243", 30f, "Cone range (m).");
            _psSmall = Config.Bind(ps, "SmallPinSeconds", 4f, "Small enemies are nailed in place.");
            _psBig = Config.Bind(ps, "BigPinSeconds_v0243", 2.5f, "Big enemies are nailed in place too (+ Cripple); Bosses are only Crippled.");
            _psCone = Config.Bind(ps, "ConeDegrees", 40f, "3 arrows in a cone: everyone inside the cone is hit (the arrows are cosmetic).");
            _psBonus = Config.Bind(ps, "PinnedSkillDamagePercent", 25f, "Pinned targets take more damage from your skills.");
            _psAscChains = Config.Bind("Bowmaster Pinning Shot Ascended", "ChainTargets", 2f, "Also pins this many nearby enemies.");
            _psAscChainRange = Config.Bind("Bowmaster Pinning Shot Ascended", "ChainRange", 6f, "m.");
            const string ea = "Bowmaster Explosive Arrow";
            _eaCooldown = Config.Bind(ea, "Cooldown", 12f, "Seconds.");
            _eaStamina = Config.Bind(ea, "StaminaCost", 22f, "Stamina.");
            _eaDamage = Config.Bind(ea, "DamagePercent", 180f, "Blast, Fire + Blunt.");
            _eaRange = Config.Bind(ea, "Range", 50f, "m.");
            _eaRadius = Config.Bind(ea, "Radius_v0243", 5f, "m (the Ascended burning field uses the same radius).");
            _eaAscClusters = Config.Bind("Bowmaster Explosive Arrow Ascended", "ClusterBombs", 6f, "After the blast, cluster bombs burst around it.");
            _eaAscClusterRadius = Config.Bind("Bowmaster Explosive Arrow Ascended", "ClusterRadius", 2.5f, "Each cluster blast (m).");
            _eaAscClusterPercent = Config.Bind("Bowmaster Explosive Arrow Ascended", "ClusterPercent", 40f, "% of the main blast per cluster bomb.");
            _eaBurn = Config.Bind(ea, "FireBurnPercent", 10f, "Fire Burn per 0.5s tick (% of the blast).");
            _eaBurnDuration = Config.Bind(ea, "FireBurnDuration", 4f, "Seconds.");
            _eaAscField = Config.Bind("Bowmaster Explosive Arrow Ascended", "FireFieldSeconds", 3f, "A burning field stays.");
            _eaAscFieldPercent = Config.Bind("Bowmaster Explosive Arrow Ascended", "FireFieldPercent", 20f, "Every 0.5s.");
            const string sa = "Bowmaster Splitting Arrow";
            _saCooldown = Config.Bind(sa, "Cooldown", 10f, "Seconds.");
            _saStamina = Config.Bind(sa, "StaminaCost", 25f, "Stamina.");
            _saDamage = Config.Bind(sa, "VolleyPercent_v0242", 90f, "Each volley hits every enemy in the cone for this %.");
            _saRange = Config.Bind(sa, "Range_v0242", 15f, "Cone range (m).");
            _saSplits = Config.Bind(sa, "ArrowsPerVolley", 5f, "Arrows per volley (they pass through everything in the cone).");
            _saCone = Config.Bind(sa, "ConeDegrees_v0242", 120f, "Cone (same as Crescent Cleave).");
            _saVolleys = Config.Bind(sa, "Volleys", 3f, "You stand still and loose this many volleys.");
            _saInterval = Config.Bind(sa, "VolleyInterval", 0.5f, "Seconds between volleys.");
            const string saa = "Bowmaster Splitting Arrow Ascended";
            _saAscVolleys = Config.Bind(saa, "Volleys", 5f, "Volleys.");
            _saAscInterval = Config.Bind(saa, "VolleyInterval", 0.25f, "Seconds between volleys.");
            _saAscBurn = Config.Bind(saa, "FireDotPercentPerStack", 6f, "Fire DoT per stack every 0.5s (% of your Ranger damage). Every volley that hits adds a stack.");
            _saAscBurnDuration = Config.Bind(saa, "FireDotDuration", 6f, "Seconds, refreshed on every hit.");
            const string sf = "Bowmaster Starfall Volley";
            _sfCooldown = Config.Bind(sf, "Cooldown", 150f, "Seconds.");
            _sfStamina = Config.Bind(sf, "StaminaCost", 45f, "Stamina.");
            _sfRange = Config.Bind(sf, "Range", 50f, "Ground PAC range (m).");
            _sfRadius = Config.Bind(sf, "Radius_v0243", 12f, "Area (m).");
            _sfChannel = Config.Bind(sf, "ChannelSeconds", 2f, "Seconds.");
            _sfDuration = Config.Bind(sf, "Duration_v0243", 8f, "Seconds of falling arrows.");
            _sfInterval = Config.Bind(sf, "Interval", 0.25f, "A falling arrow (cosmetic) every...");
            _sfImpact = Config.Bind(sf, "ImpactRadius", 4f, "Cosmetic impact ring (m).");
            _sfTick = Config.Bind(sf, "HitInterval", 0.5f, "Every this often, EVERY enemy in the area is hit.");
            _sfDamage = Config.Bind(sf, "TickPercent_v0242", 45f, "% per hit.");
            _sfAscRadius = Config.Bind("Bowmaster Starfall Volley Ascended", "Radius", 12f, "Area of the final giant arrow (m).");
            _sfAscDamage = Config.Bind("Bowmaster Starfall Volley Ascended", "FinalArrowPercent", 300f, "% to every enemy in the area.");

            DragonCombat.RegisterSkillModule(CastFromTree, CooldownForTree);
            DragonCombat.RegisterStackQuery(StackQuery);
            DragonCombat.RegisterSkillLevelBonus(SkillLevelBonus);
            DragonCombat.RegisterIncomingHitFilter(IncomingHit);
            DragonCombat.ControlsHook = RangerControls;
            InstallPatches();
            Logger.LogInfo(ModName + " v" + ModVersion + " loaded.");
        }

        // ------------------------------------------------------------------ hooks
        private bool CastFromTree(Player player, string id)
        {
            if (player == null || player.IsDead()) return false;
            if (GetClass(player) != "Ranger") return false;
            string adv = GetAdvancement(player);
            _refundLeft = Mathf.Max(0f, _wsRefundMax.Value);
            switch (id)
            {
                case "piercing_arrow": CastPiercingArrow(player, adv == "Bowmaster" && DragonCombat.IsSkillAscended(player, id)); return true;
                case "tumble_shot": CastTumbleShot(player, adv == "Acrobat" && DragonCombat.IsSkillAscended(player, id)); return true;
                case "snare_trap": CastSnareTrap(player); return true;
                case "gale_volley": if (adv == "Acrobat") CastGaleVolley(player); return adv == "Acrobat";
                case "cyclone_arrow": if (adv == "Acrobat") CastCycloneArrow(player); return adv == "Acrobat";
                case "swallow_dive": if (adv == "Acrobat") CastSwallowDive(player); return adv == "Acrobat";
                case "skyfall_barrage": if (adv == "Acrobat") CastSkyfallBarrage(player); return adv == "Acrobat";
                case "ricochet_arrow": if (adv == "Acrobat") CastRicochetArrow(player); return adv == "Acrobat";
                case "furious_winds": if (adv == "Acrobat") CastFuriousWinds(player); return adv == "Acrobat";
                case "tailwind": if (adv == "Acrobat") CastTailwind(player); return adv == "Acrobat";
                case "ballista_shot": if (adv == "Bowmaster") CastBallistaShot(player); return adv == "Bowmaster";
                case "arrow_rain": if (adv == "Bowmaster") CastArrowRain(player); return adv == "Bowmaster";
                case "pinning_shot": if (adv == "Bowmaster") CastPinningShot(player); return adv == "Bowmaster";
                case "explosive_arrow": if (adv == "Bowmaster") CastExplosiveArrow(player); return adv == "Bowmaster";
                case "splitting_arrow": if (adv == "Bowmaster") CastSplittingArrow(player); return adv == "Bowmaster";
                case "starfall_volley": if (adv == "Bowmaster") CastStarfallVolley(player); return adv == "Bowmaster";
                case "hawks_vigil": if (adv == "Bowmaster") CastHawksVigil(player); return adv == "Bowmaster";
            }
            return false;
        }

        private float CooldownForTree(string id)
        {
            switch (id)
            {
                case "piercing_arrow": return CooldownRemaining("Ranger.PiercingArrow");
                case "tumble_shot": return CooldownRemaining("Ranger.TumbleShot");
                case "snare_trap": return CooldownRemaining("Ranger.SnareTrap");
                case "gale_volley": return CooldownRemaining("Acrobat.GaleVolley");
                case "cyclone_arrow": return CooldownRemaining("Acrobat.CycloneArrow");
                case "swallow_dive": return DiveCharges() > 0 ? 0f : Mathf.Max(0f, _diveNextCharge - Time.time);
                case "skyfall_barrage": return CooldownRemaining("Acrobat.SkyfallBarrage");
                case "ricochet_arrow": return CooldownRemaining("Acrobat.RicochetArrow");
                case "furious_winds": return CooldownRemaining("Acrobat.FuriousWinds");
                case "tailwind": return CooldownRemaining("Acrobat.Tailwind");
                case "ballista_shot": return _ballistaCharging ? 0f : CooldownRemaining("Bowmaster.BallistaShot");
                case "arrow_rain": return CooldownRemaining("Bowmaster.ArrowRain");
                case "pinning_shot": return CooldownRemaining("Bowmaster.PinningShot");
                case "explosive_arrow": return CooldownRemaining("Bowmaster.ExplosiveArrow");
                case "splitting_arrow": return CooldownRemaining("Bowmaster.SplittingArrow");
                case "starfall_volley": return CooldownRemaining("Bowmaster.StarfallVolley");
                case "hawks_vigil": return CooldownRemaining("Bowmaster.HawksVigil");
            }
            return 0f;
        }

        private bool StackQuery(string id, out int ready, out int max, out float next)
        {
            ready = 0; max = 0; next = 0f;
            if (id == "ballista_shot" && _ballistaCharging)
            {
                // Charging: live stacks, no cooldown shade (universal charge rule).
                max = 3;
                ready = _ballistaStacks;
                next = -Mathf.Max(0.01f, _ballistaStacks >= 3 ? 0.01f : (_ballistaNextStack - Time.time));
                return true;
            }
            if (id != "swallow_dive") return false;
            Player player = Player.m_localPlayer;
            if (player == null || GetAdvancement(player) != "Acrobat") return false;
            max = DiveMax(player);
            ready = DiveCharges();
            next = ready < max ? Mathf.Max(0f, _diveNextCharge - Time.time) : 0f;
            return true;
        }

        // Wildborn: +20 Bows / +20 Sneak. Tailwind: +30 Jump.
        private float SkillLevelBonus(Player player, string skill)
        {
            if (player == null) return 0f;
            float bonus = 0f;
            if (GetClass(player) == "Ranger")
            {
                if (skill == "Bows") bonus += Mathf.Max(0f, _wbBows.Value);
                if (skill == "Dodge") bonus += Mathf.Max(0f, _wbDodge.Value);
            }
            if (skill == "Jump" && TailwindActive(player)) bonus += Mathf.Max(0f, _twJump.Value);
            return bonus;
        }

        // Fall damage: Wildborn -30%; none under Tailwind or after Skyfall Barrage until you land.
        private void IncomingHit(Character target, HitData hit)
        {
            Player player = target as Player;
            if (player == null) return;
            if (!IsFallHit(hit))
            {
                if (HasIFrames(player))
                {
                    hit.m_damage.Modify(0f);
                    hit.m_pushForce = 0f;
                    SetHitFloat(hit, "m_staggerMultiplier", 0f);
                    SetHitFloat(hit, "m_backstabBonus", 1f);
                }
                return;
            }
            bool noFall;
            if (TailwindActive(player) || (_noFallUntilGrounded.TryGetValue(player.GetInstanceID(), out noFall) && noFall))
            {
                hit.m_damage.Modify(0f);
                return;
            }
            if (GetClass(player) == "Ranger")
            {
                bool acrobat = GetAdvancement(player) == "Acrobat";
                hit.m_damage.Modify(Mathf.Clamp01(1f - (acrobat ? _wsFallReduction.Value : _wbFall.Value) / 100f));
                // v0.24.3 Windstep: a fall never kills an Acrobat (left at 1 HP).
                if (acrobat)
                {
                    float total = hit.GetTotalDamage();
                    float hp = player.GetHealth();
                    if (total >= hp && total > 0f) hit.m_damage.Modify(Mathf.Max(0f, hp - 1f) / total);
                }
            }
        }

        private static void SetHitFloat(HitData hit, string name, float value)
        {
            try
            {
                FieldInfo f = typeof(HitData).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(float)) f.SetValue(hit, value);
            }
            catch { }
        }

        private static bool IsFallHit(HitData hit)
        {
            try
            {
                FieldInfo f = typeof(HitData).GetField("m_hitType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return f != null && f.GetValue(hit) != null && f.GetValue(hit).ToString() == "Fall";
            }
            catch { return false; }
        }

        private bool TailwindActive(Player player)
        {
            float until;
            return player != null && _tailwindUntil.TryGetValue(player.GetInstanceID(), out until) && Time.time < until;
        }

        // ------------------------------------------------------------------ per-frame
        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead()) return;
            bool ranger = GetClass(player) == "Ranger";
            bool acrobat = ranger && GetAdvancement(player) == "Acrobat";
            UpdateDodgeCost(player, acrobat);
            if (acrobat) DragonCombat.ApplyStaminaUseCut(player, Mathf.Clamp01(_wsStaminaUse.Value / 100f), 0.5f);
            // v0.25.2 perf: gear rules 5x per second, not every frame.
            if (ranger && Time.time >= _nextGearCheck) { _nextGearCheck = Time.time + 0.2f; UpdateForbiddenGear(player, acrobat); }
            if (ranger) UpdateQuickShotSpeed(player);
            UpdateFocus(player, ranger && GetAdvancement(player) == "Bowmaster");
            UpdateLoadedCrossbow(player, ranger && GetAdvancement(player) == "Bowmaster");
            UpdateTraps(player);
            UpdateDiveCharges(player, acrobat);
            bool grounded = IsGrounded(player);
            if (grounded)
            {
                _doubleJumpUsed = false;
                if (!_hovering) _noFallUntilGrounded[player.GetInstanceID()] = false;
            }
            else if (acrobat && !_doubleJumpUsed && !_hovering && JumpPressed(player))
                AirJump(player);
        }

        // Windstep: Dodge costs 50% less Stamina (Player.m_dodgeStaminaUsage), restored otherwise.
        private void UpdateDodgeCost(Player player, bool acrobat)
        {
            try
            {
                FieldInfo f = typeof(Player).GetField("m_dodgeStaminaUsage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f == null || f.FieldType != typeof(float)) return;
                float current = (float)f.GetValue(player);
                if (_dodgeBase < 0f) _dodgeBase = current;
                float wanted = acrobat ? _dodgeBase * Mathf.Clamp01(1f - _wsDodge.Value / 100f) : _dodgeBase;
                if (Mathf.Abs(current - wanted) > 0.01f) f.SetValue(player, wanted);
            }
            catch { }
        }

        // Windstep: one more jump in mid-air.
        private void AirJump(Player player)
        {
            if (GetStamina(player) < _wsJumpStamina.Value) return;
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body == null) return;
            UseStamina(player, _wsJumpStamina.Value);
            Vector3 v = body.velocity;
            v.y = DragonCombat.M(Mathf.Max(1f, _wsJump.Value));
            body.velocity = v;
            _doubleJumpUsed = true;
            ResetFloatField(player, "m_maxAirAltitude", player.transform.position.y);
            if (_enableVfx.Value) StartCoroutine(RingVfx(player.transform.position, DragonCombat.M(1.2f), new Color(0.55f, 1f, 0.90f, 0.9f), 0.3f));
        }

        private bool JumpPressed(Player player)
        {
            try
            {
                MethodInfo take = typeof(Player).GetMethod("TakeInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (take != null && !Convert.ToBoolean(take.Invoke(player, null))) return false;
                Type zinput = Type.GetType("ZInput, assembly_valheim");
                MethodInfo down = zinput == null ? null : zinput.GetMethod("GetButtonDown", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
                if (down != null) return Convert.ToBoolean(down.Invoke(null, new object[] { "Jump" }));
            }
            catch { }
            return Input.GetKeyDown(KeyCode.Space);
        }

        // ------------------------------------------------------------------ Piercing Arrow
        private void CastPiercingArrow(Player player, bool ascended)
        {
            if (!RequireBow(player)) return;
            if (!BeginSkill(player, "Ranger.PiercingArrow", _paCooldown.Value, _paStamina.Value)) return;
            RangerArrowDamage d = ArrowDamage(player);
            float power = DragonCombat.GetSkillPower(player, "piercing_arrow");
            float range = DragonCombat.M(ascended ? _paAscRange.Value : _paRange.Value);
            Vector3 origin = ShotOrigin(player);
            Vector3 dir = AimDir(player, origin);
            DragonCombat.PlayClip(player, "rg_power", 0.1f);   // snap draw, strong recoil
            Shoot(player, "Piercing Arrow");
            bool first = true;
            float endWidth = DragonCombat.M(Mathf.Max(0.3f, _paAscWidth.Value));
            StartCoroutine(ArrowFlight(player, origin, dir, DragonCombat.M(_paSpeed.Value), range, DragonCombat.M(0.35f), true,
                new Color(0.70f, 1f, 0.55f, 1f), ascended ? endWidth * 0.5f : 0f,
                delegate(Character enemy)
                {
                    Deal(player, enemy, d, _paDamage.Value / 100f * power, 4f, false);
                    if (first) { DragonCombat.ApplyCripple(enemy, _paCripple.Value); first = false; }
                    if (ascended) DragonCombat.ApplyExpose(enemy, _paAscExpose.Value);
                    return true;
                }, null));
        }

        // ------------------------------------------------------------------ Tumble Shot
        private void CastTumbleShot(Player player, bool ascended)
        {
            if (!RequireBow(player)) return;
            if (!BeginSkill(player, "Ranger.TumbleShot", _tsCooldown.Value, _tsStamina.Value)) return;
            StartCoroutine(TumbleRoutine(player, ascended));
        }

        // v0.24.2: a swift, uncontrolled backflip straight back (2m in 0.3s, no vanilla dodge),
        // 0.5s of i-frames, arrows loosed mid-flip.
        private IEnumerator TumbleRoutine(Player player, bool ascended)
        {
            Vector3 start = player.transform.position;
            ShowMessage("Tumble Shot");
            if (ascended)
            {
                List<Character> gust = GetSphereTargets(player, start, DragonCombat.M(_tsAscGust.Value));
                for (int i = 0; i < gust.Count; i++) DragonCombat.ApplyCripple(gust[i], _tsAscCripple.Value);
                if (_enableVfx.Value) StartCoroutine(RingVfx(start, DragonCombat.M(_tsAscGust.Value), new Color(0.55f, 1f, 0.90f, 0.9f), 1.0f));
            }
            return BackFlip(player, _tsDistance.Value, true, delegate { TumbleVolley(player, ascended); });
        }

        // Shared swift back-jump (Tumble Shot, Gale Volley): kinematic 0.3s, straight back from where you
        // face, body flips backwards (visual only), i-frames, midAction at the apex.
        private IEnumerator BackFlip(Player player, float meters, bool flip, Action midAction)
        {
            Vector3 start = player.transform.position;
            Vector3 back = -FlatAim(player);
            float distance = DragonCombat.M(Mathf.Max(0.5f, meters));
            RaycastHit wall;
            if (Physics.Raycast(start + Vector3.up * 0.6f, back, out wall, distance + 0.4f, SolidMask(), QueryTriggerInteraction.Ignore) &&
                wall.collider.GetComponentInParent<Character>() == null)
                distance = Mathf.Max(0f, wall.distance - 0.4f);
            Vector3 end = start + back * distance;
            Rigidbody body = player.GetComponent<Rigidbody>();
            GrantIFrames(player, _rgIFrames.Value);
            DragonCombat.LockSkill(player, 0.35f);
            if (flip) DragonCombat.PlayClip(player, "rg_tumble", 0.02f);   // v0.25.13: tucked flip, draw upside down, loose
            const float duration = 0.3f;
            bool fired = false;
            float t = 0f;
            while (t < duration && player != null && !player.IsDead())
            {
                t += Time.fixedDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                Vector3 p = Vector3.Lerp(start, end, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * DragonCombat.M(0.6f);
                p.y = Mathf.Max(p.y, GroundAt(p).y);
                player.transform.position = p;
                if (body != null) { body.position = p; body.velocity = Vector3.zero; }
                if (!fired && k >= 0.5f) { fired = true; if (midAction != null) midAction(); }
                yield return new WaitForFixedUpdate();
            }
            if (player == null) yield break;
            if (!fired && midAction != null) midAction();
            ResetFloatField(player, "m_maxAirAltitude", player.transform.position.y);
        }

        // v0.24.4 PROCEDURAL SKILL ANIMATIONS: the body (Visual root) is rotated / offset around its
        // centre on top of the vanilla animation (backflips, dives, spins, leans, recoil). One motion at
        // a time (a new one takes over); the rest pose is restored when the last one ends.
        private int _poseToken;
        private bool _poseActive;

        private IEnumerator PoseMotion(Player player, float duration, Func<float, Quaternion> rot, Func<float, Vector3> offset)
        {
            return PoseRoutine(player, duration, null, rot, offset);
        }

        private IEnumerator PoseRoutine(Player player, float duration, Func<bool> keep, Func<float, Quaternion> rot, Func<float, Vector3> offset)
        {
            Transform v = VisualOf(player);
            if (v == null) yield break;
            // v0.25.10: the Visual root is shared with the skill clip engine (one owner at a time, one
            // rest pose), so a pose and a clip can never leave the body tilted.
            int token = DragonCombat.ClaimMotionRoot(v);
            int local = ++_poseToken;
            _poseActive = true;
            Vector3 pivot = new Vector3(0f, 0.9f, 0f);
            float t = 0f;
            while (player != null && !player.IsDead() && DragonCombat.OwnsMotionRoot(token) && v != null && (keep != null ? keep() : t < duration))
            {
                float k = keep != null ? t : Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
                Quaternion r = rot == null ? Quaternion.identity : rot(k);
                v.localRotation = DragonCombat.MotionBaseRot * r;
                v.localPosition = DragonCombat.MotionBasePos + (pivot - r * pivot) + (offset == null ? Vector3.zero : offset(k));
                t += Time.deltaTime;
                yield return null;
            }
            DragonCombat.ReleaseMotionRoot(token, v);
            if (local == _poseToken) _poseActive = false;
        }

        private static float Bump(float k) { return Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI); }

        private static Transform VisualOf(Player player)
        {
            object v = ReadField(player, "m_visual");
            GameObject go = v as GameObject;
            if (go != null) return go.transform;
            Transform t = player.transform.Find("Visual");
            return t;
        }

        // I-frames: every hit (except fall damage) is nulled while they last.
        private readonly Dictionary<int, float> _iframeUntil = new Dictionary<int, float>();

        private void GrantIFrames(Player player, float seconds)
        {
            if (player == null || seconds <= 0f) return;
            _iframeUntil[player.GetInstanceID()] = Time.time + seconds;
        }

        private bool HasIFrames(Player player)
        {
            float until;
            return player != null && _iframeUntil.TryGetValue(player.GetInstanceID(), out until) && Time.time < until;
        }

        private void TumbleVolley(Player player, bool ascended)
        {
            RangerArrowDamage d = ArrowDamage(player);
            float mult = _tsDamage.Value / 100f * DragonCombat.GetSkillPower(player, "tumble_shot");
            int count = Mathf.Max(1, Mathf.RoundToInt(ascended ? _tsAscArrows.Value : _tsArrows.Value));
            bool[] killed = new bool[1];
            FanPierce(player, count, _tsFan.Value, DragonCombat.M(_tsRange.Value), new Color(0.55f, 1f, 0.75f, 1f),
                delegate(Character enemy)
                {
                    Deal(player, enemy, d, mult, 6f, false);
                    if (ascended && enemy.IsDead() && !killed[0])
                    {
                        killed[0] = true;
                        _cooldowns.Remove("Ranger.TumbleShot");
                        ShowMessage("Tumble Shot - reset");
                    }
                });
        }

        // ------------------------------------------------------------------ Snare Trap
        private void CastSnareTrap(Player player)
        {
            Vector3 point;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_snRange.Value), out point)) { ShowMessage("Aim at the ground"); return; }
            if (!BeginSkill(player, "Ranger.SnareTrap", _snCooldown.Value, _snStamina.Value)) return;
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlayClip(player, "rg_trap", 0.15f);
            int max = Mathf.Max(1, Mathf.RoundToInt(_snMax.Value));
            while (_traps.Count >= max) RemoveTrap(0);
            SnareTrap trap = new SnareTrap();
            trap.Position = point;
            trap.ExpireAt = Time.time + Mathf.Max(1f, _snLifetime.Value);
            if (_enableVfx.Value) trap.Visual = CreateTrapVisual(point, DragonCombat.M(_snTrigger.Value));
            _traps.Add(trap);
            ShowMessage("Snare Trap (" + _traps.Count + "/" + max + ")");
        }

        private void UpdateTraps(Player player)
        {
            for (int i = _traps.Count - 1; i >= 0; i--)
            {
                SnareTrap trap = _traps[i];
                if (Time.time >= trap.ExpireAt) { RemoveTrap(i); continue; }
                List<Character> inside = GetSphereTargets(player, trap.Position, DragonCombat.M(_snTrigger.Value));
                if (inside.Count == 0) continue;
                RangerArrowDamage d = ArrowDamage(player);
                float mult = _snDamage.Value / 100f * DragonCombat.GetSkillPower(player, "snare_trap");
                for (int k = 0; k < inside.Count; k++)
                {
                    Character enemy = inside[k];
                    Deal(player, enemy, d, mult, 0f, false);
                    if (DragonCombat.IsSmallEnemy(enemy)) DragonCombat.Stun(enemy, trap.Position);
                    DragonCombat.ApplyCripple(enemy, _snCripple.Value);
                }
                if (_enableVfx.Value) StartCoroutine(RingVfx(trap.Position, DragonCombat.M(_snTrigger.Value), new Color(0.85f, 0.95f, 0.45f, 1f), 0.5f));
                ShowMessage("Snare Trap sprung");
                RemoveTrap(i);
            }
        }

        private void RemoveTrap(int index)
        {
            if (index < 0 || index >= _traps.Count) return;
            if (_traps[index].Visual != null) Destroy(_traps[index].Visual);
            _traps.RemoveAt(index);
        }

        // ------------------------------------------------------------------ Gale Volley
        private void CastGaleVolley(Player player)
        {
            if (!RequireBow(player)) return;
            if (!BeginSkill(player, "Acrobat.GaleVolley", _gvCooldown.Value, _gvStamina.Value)) return;
            StartCoroutine(GaleRoutine(player, DragonCombat.IsSkillAscended(player, "gale_volley")));
        }

        private IEnumerator GaleRoutine(Player player, bool ascended)
        {
            ShowMessage("Gale Volley");
            // v0.24.2: the same swift 2m back-jump as Tumble Shot (0.3s, i-frames 0.5s); arrows at the apex.
            StartCoroutine(BackFlip(player, _gvLeap.Value, true, null));
            yield return new WaitForSeconds(0.15f);
            int fans = ascended ? 2 : 1;
            for (int f = 0; f < fans; f++)
            {
                if (player == null || player.IsDead()) yield break;
                RangerArrowDamage d = ArrowDamage(player);
                float mult = _gvDamage.Value / 100f * DragonCombat.GetSkillPower(player, "gale_volley");
                FanPierce(player, Mathf.Max(1, Mathf.RoundToInt(_gvArrows.Value)), _gvFan.Value, DragonCombat.M(_gvRange.Value),
                    new Color(0.55f, 1f, 0.90f, 1f),
                    delegate(Character enemy)
                    {
                        Deal(player, enemy, d, mult, DragonCombat.IsSmallEnemy(enemy) ? _gvPush.Value : 4f, false);
                    });
                Shoot(player, null);
                if (f + 1 < fans) yield return new WaitForSeconds(Mathf.Max(0.05f, _gvAscDelay.Value));
            }
        }

        // ------------------------------------------------------------------ Cyclone Arrow
        private void CastCycloneArrow(Player player)
        {
            if (!RequireBow(player)) return;
            if (!BeginSkill(player, "Acrobat.CycloneArrow", _cyCooldown.Value, _cyStamina.Value)) return;
            Vector3 origin = ShotOrigin(player);
            Vector3 dir = AimDir(player, origin);
            DragonCombat.PlayClip(player, "rg_shot", 0.1f);   // v0.25.31 normal bow shot, no spin
            Shoot(player, "Cyclone Arrow");
            bool ascended = DragonCombat.IsSkillAscended(player, "cyclone_arrow");
            StartCoroutine(CycloneRoutine(player, origin, dir, DragonCombat.M(_cyRange.Value), Mathf.Max(0.2f, _cyTravel.Value), DragonCombat.M(_cyRadius.Value), 1f, ascended));
        }

        private IEnumerator CycloneRoutine(Player player, Vector3 pos, Vector3 dir, float range, float travel, float radius, float scale, bool split)
        {
            GameObject vfx = _enableVfx.Value ? CreateSpinRing(pos, radius, new Color(0.55f, 1f, 0.90f, 0.9f)) : null;
            float speed = range / travel;
            float traveled = 0f, nextHit = 0f;
            RangerArrowDamage d = ArrowDamage(player);
            float mult = _cyDamage.Value / 100f * scale * DragonCombat.GetSkillPower(player, "cyclone_arrow");
            while (traveled < range && player != null && !player.IsDead())
            {
                float step = speed * Time.deltaTime;
                RaycastHit wall;
                if (Physics.Raycast(pos, dir, out wall, step + 0.2f, SolidMask(), QueryTriggerInteraction.Ignore) && wall.collider.GetComponentInParent<Character>() == null)
                    break;
                pos += dir * step;
                traveled += step;
                if (vfx != null) { vfx.transform.position = pos; vfx.transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World); }
                if (Time.time >= nextHit)
                {
                    nextHit = Time.time + Mathf.Max(0.1f, _cyInterval.Value);
                    List<Character> hits = GetSphereTargets(player, pos, radius);
                    for (int i = 0; i < hits.Count; i++)
                    {
                        Deal(player, hits[i], d, mult, 0f, false);
                        if (DragonCombat.IsSmallEnemy(hits[i])) PullToward(hits[i], pos, _cyPull.Value);
                        OnSkillHit(player);
                    }
                    // v0.24.2 Ascended: it splits the moment it first catches an enemy (passing-through
                    // skills trigger their after-effect on hit, never at the end of their range).
                    if (split && hits.Count > 0)
                    {
                        split = false;
                        SpawnCycloneSplits(player, pos, dir);
                    }
                }
                yield return null;
            }
            if (vfx != null) Destroy(vfx);
        }

        private void SpawnCycloneSplits(Player player, Vector3 pos, Vector3 dir)
        {
            if (player == null || player.IsDead()) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(_cyAscSplits.Value));
            for (int i = 0; i < n; i++)
            {
                float a = n == 1 ? 0f : -30f + 60f * i / (n - 1);
                Vector3 sdir = Quaternion.AngleAxis(a, Vector3.up) * dir;
                StartCoroutine(CycloneRoutine(player, pos, sdir, DragonCombat.M(_cyAscRange.Value), Mathf.Max(0.2f, _cyAscTravel.Value), DragonCombat.M(_cyAscRadius.Value), 0.6f, false));
            }
        }

        // ------------------------------------------------------------------ Swallow Dive
        private int DiveMax(Player player)
        {
            return Mathf.Max(1, Mathf.RoundToInt(DragonCombat.IsSkillAscended(player, "swallow_dive") ? _sdAscCharges.Value : _sdCharges.Value));
        }

        private int DiveCharges()
        {
            return Mathf.Max(0, _diveCharges);
        }

        private void UpdateDiveCharges(Player player, bool acrobat)
        {
            if (!acrobat) return;
            int max = DiveMax(player);
            if (_diveCharges < 0) _diveCharges = max;
            if (_diveCharges > max) _diveCharges = max;
            if (_diveCharges < max && Time.time >= _diveNextCharge)
            {
                _diveCharges++;
                if (_diveCharges < max) _diveNextCharge = Time.time + RechargeSeconds(player, "Acrobat.SwallowDive", _sdRecharge.Value);
            }
        }

        private void CastSwallowDive(Player player)
        {
            UpdateDiveCharges(player, true);
            if (_diveCharges <= 0) { ShowMessage("Swallow Dive recharging " + Mathf.Max(0f, _diveNextCharge - Time.time).ToString("0.0") + "s"); return; }
            if (GetStamina(player) < _sdStamina.Value) { ShowMessage("Not enough stamina"); return; }
            UseStamina(player, _sdStamina.Value);
            if (_diveCharges == DiveMax(player)) _diveNextCharge = Time.time + RechargeSeconds(player, "Acrobat.SwallowDive", _sdRecharge.Value);
            _diveCharges--;
            StartCoroutine(DiveRoutine(player, DragonCombat.IsSkillAscended(player, "swallow_dive")));
        }

        private IEnumerator DiveRoutine(Player player, bool ascended)
        {
            ShowMessage("Swallow Dive" + " (" + _diveCharges + "/" + DiveMax(player) + ")");
            Vector3 start = player.transform.position;
            Vector3 dir = AimDir(player, player.GetEyePoint());
            float distance = DragonCombat.M(_sdDistance.Value);
            RaycastHit wall;
            if (Physics.Raycast(start + Vector3.up * 0.8f, dir, out wall, distance, SolidMask(), QueryTriggerInteraction.Ignore))
            {
                Character c = wall.collider.GetComponentInParent<Character>();
                if (c == null) distance = Mathf.Max(0f, wall.distance - 0.6f);
            }
            Vector3 end = start + dir * distance;
            Rigidbody body = player.GetComponent<Rigidbody>();
            DragonCombat.PlayClip(player, "rg_dive", 0.08f);
            float t = 0f, duration = 0.25f;
            while (t < duration && player != null)
            {
                t += Time.fixedDeltaTime;
                Vector3 p = Vector3.Lerp(start, end, Mathf.Clamp01(t / duration));
                player.transform.position = p;
                if (body != null) { body.position = p; body.velocity = Vector3.zero; }
                yield return new WaitForFixedUpdate();
            }
            if (player == null) yield break;
            ResetFloatField(player, "m_maxAirAltitude", player.transform.position.y);
            if (body != null) body.velocity = dir * DragonCombat.M(4f);
            float mult = _sdDamage.Value / 100f * DragonCombat.GetSkillPower(player, "swallow_dive");
            List<Character> hits = PathTargets(player, start, end, DragonCombat.M(_sdRadius.Value));
            RangerArrowDamage d = ArrowDamage(player);
            for (int i = 0; i < hits.Count; i++) { Deal(player, hits[i], d, mult, 4f, false); OnSkillHit(player); }
            if (_enableVfx.Value) LineVfx(start + Vector3.up, end + Vector3.up, new Color(0.55f, 1f, 0.90f, 0.95f), 0.25f, 0.35f);
            if (ascended)
            {
                yield return new WaitForSeconds(Mathf.Max(0.05f, _sdAscDelay.Value));
                if (player == null) yield break;
                List<Character> again = PathTargets(player, start, end, DragonCombat.M(_sdRadius.Value));
                for (int i = 0; i < again.Count; i++) Deal(player, again[i], d, mult * _sdAscPercent.Value / 100f, 2f, false);
                if (_enableVfx.Value) LineVfx(start + Vector3.up, end + Vector3.up, new Color(0.85f, 1f, 1f, 0.95f), 0.20f, 0.25f);
            }
        }

        // ------------------------------------------------------------------ Skyfall Barrage
        private void CastSkyfallBarrage(Player player)
        {
            if (!RequireBow(player)) return;
            Vector3 point;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_sbRange.Value), out point)) { ShowMessage("Aim at the ground"); return; }
            if (!BeginSkill(player, "Acrobat.SkyfallBarrage", _sbCooldown.Value, _sbStamina.Value)) return;
            StartCoroutine(SkyfallRoutine(player, point, DragonCombat.IsSkillAscended(player, "skyfall_barrage")));
        }

        private IEnumerator SkyfallRoutine(Player player, Vector3 point, bool ascended)
        {
            ShowMessage("Skyfall Barrage");
            Rigidbody body = player.GetComponent<Rigidbody>();
            int id = player.GetInstanceID();
            _noFallUntilGrounded[id] = true;
            _hovering = true;
            DragonCombat.LockSkill(player, 0.5f + _sbDuration.Value);
            Vector3 start = player.transform.position;
            Vector3 top = start + Vector3.up * DragonCombat.M(_sbHeight.Value);
            RaycastHit ceiling;
            if (Physics.Raycast(start + Vector3.up, Vector3.up, out ceiling, DragonCombat.M(_sbHeight.Value), SolidMask(), QueryTriggerInteraction.Ignore))
                top = start + Vector3.up * Mathf.Max(0f, ceiling.distance - 1.5f);
            DragonCombat.PlayClip(player, "rg_shot", 0.1f);   // v0.25.32 normal bow shot on the way up
            float t = 0f;
            while (t < 0.5f && player != null)
            {
                t += Time.fixedDeltaTime;
                Vector3 p = Vector3.Lerp(start, top, Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI * 0.5f));
                player.transform.position = p;
                if (body != null) { body.position = p; body.velocity = Vector3.zero; }
                yield return new WaitForFixedUpdate();
            }
            float radius = DragonCombat.M(ascended ? _sbAscRadius.Value : _sbRadius.Value);
            float end = Time.time + Mathf.Max(0.2f, _sbDuration.Value);
            DragonCombat.PlayClip(player, "rg_hover", 0.25f, true);   // aimed down while hovering
            float nextTick = 0f;
            RangerArrowDamage d = ArrowDamage(player);
            float mult = _sbDamage.Value / 100f * DragonCombat.GetSkillPower(player, "skyfall_barrage");
            while (Time.time < end && player != null && !player.IsDead())
            {
                player.transform.position = top;
                if (body != null) { body.position = top; body.velocity = Vector3.zero; }
                if (ascended)
                {
                    Vector3 steer;
                    if (AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_sbRange.Value) + DragonCombat.M(_sbHeight.Value), out steer)) point = steer;
                }
                if (Time.time >= nextTick)
                {
                    nextTick = Time.time + Mathf.Max(0.05f, _sbInterval.Value);
                    List<Character> hits = GetSphereTargets(player, point, radius);
                    for (int i = 0; i < hits.Count; i++) { Deal(player, hits[i], d, mult, 2f, false); OnSkillHit(player); }
                    if (_enableVfx.Value)
                    {
                        for (int k = 0; k < 6; k++)
                        {
                            Vector2 r = UnityEngine.Random.insideUnitCircle * radius;
                            Vector3 ground = point + new Vector3(r.x, 0f, r.y);
                            LineVfx(top, ground, new Color(0.55f, 1f, 0.80f, 0.9f), 0.06f, 0.15f);
                        }
                        StartCoroutine(RingVfx(point, radius, new Color(0.55f, 1f, 0.80f, 0.7f), Mathf.Max(0.05f, _sbInterval.Value)));
                    }
                    Shoot(player, null);
                }
                yield return new WaitForFixedUpdate();
            }
            _hovering = false;
            DragonCombat.ClipStop(player, 0.3f);
        }

        // ------------------------------------------------------------------ Ricochet Arrow
        private void CastRicochetArrow(Player player)
        {
            if (!RequireBow(player)) return;
            Character first = AimedEnemy(player, DragonCombat.M(_rcRange.Value));
            if (first == null) { ShowMessage("No target in sight"); return; }
            if (!BeginSkill(player, "Acrobat.RicochetArrow", _rcCooldown.Value, _rcStamina.Value)) return;
            DragonCombat.PlayClip(player, "rg_trick", 0.1f);   // flicked trick shot
            Shoot(player, "Ricochet Arrow");
            StartCoroutine(RicochetRoutine(player, first, DragonCombat.IsSkillAscended(player, "ricochet_arrow")));
        }

        private IEnumerator RicochetRoutine(Player player, Character target, bool ascended)
        {
            RangerArrowDamage d = ArrowDamage(player);
            float baseMult = _rcDamage.Value / 100f * DragonCombat.GetSkillPower(player, "ricochet_arrow");
            int bounces = Mathf.Max(0, Mathf.RoundToInt(ascended ? _rcAscBounces.Value : _rcBounces.Value));
            HashSet<int> hit = new HashSet<int>();
            Vector3 from = ShotOrigin(player);
            for (int i = 0; i <= bounces && target != null && player != null; i++)
            {
                Vector3 to = target.transform.position + Vector3.up;
                if (_enableVfx.Value) LineVfx(from, to, new Color(0.55f, 1f, 0.90f, 1f), 0.07f, 0.18f);
                yield return new WaitForSeconds(Mathf.Clamp(Vector3.Distance(from, to) / DragonCombat.M(60f), 0.03f, 0.25f));
                if (target == null || target.IsDead()) break;
                Deal(player, target, d, baseMult * (1f + _rcBonus.Value / 100f * i), 4f, false);
                OnSkillHit(player);
                hit.Add(target.GetInstanceID());
                from = to;
                Character next = null;
                float best = DragonCombat.M(_rcBounceRange.Value);
                List<Character> near = GetSphereTargets(player, target.transform.position, best);
                for (int k = 0; k < near.Count; k++)
                {
                    if (hit.Contains(near[k].GetInstanceID())) continue;
                    float dist = Vector3.Distance(near[k].transform.position, target.transform.position);
                    if (dist < best) { best = dist; next = near[k]; }
                }
                if (next == null || i == bounces)
                {
                    if (ascended)
                    {
                        float blast = DragonCombat.M(_rcAscBlast.Value);
                        List<Character> boom = GetSphereTargets(player, to, blast);
                        for (int k = 0; k < boom.Count; k++) Deal(player, boom[k], d, baseMult * _rcAscBlastPercent.Value / 100f, 8f, false);
                        if (_enableVfx.Value) StartCoroutine(RingVfx(to, blast, new Color(0.85f, 1f, 0.95f, 1f), 0.5f));
                    }
                    break;
                }
                target = next;
            }
        }

        // ------------------------------------------------------------------ Furious Winds (Ultimate)
        // v0.24.1 (replaces Tempest Dance): a whirlwind of magical leaves around you. No enemy and no
        // enemy projectile gets inside the barrier; everything at its edge is cut (Slash) and takes a
        // stacking Spirit DoT (every tick adds a stack, refreshed to 6s).
        private readonly Dictionary<int, int> _fwStacks = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _fwStackUntil = new Dictionary<int, float>();
        private static Type _projectileType;

        private void CastFuriousWinds(Player player)
        {
            if (!BeginSkill(player, "Acrobat.FuriousWinds", _fwCooldown.Value, _fwStamina.Value)) return;
            StartCoroutine(FuriousWindsRoutine(player, DragonCombat.IsSkillAscended(player, "furious_winds")));
        }

        private IEnumerator FuriousWindsRoutine(Player player, bool ascended)
        {
            ShowMessage("FURIOUS WINDS");
            float duration = Mathf.Max(0.5f, ascended ? _fwAscDuration.Value : _fwDuration.Value);
            float radius = DragonCombat.M(ascended ? _fwAscRadius.Value : _fwRadius.Value);
            float barrier = Mathf.Min(radius, DragonCombat.M(ascended ? _fwAscBarrier.Value : _fwBarrier.Value));
            Dictionary<int, float> held = new Dictionary<int, float>();
            DragonCombat.GrantHyperArmor(player, duration + 0.3f);
            DragonCombat.LockSkill(player, duration);
            DragonCombat.PlaySpinClip(player, duration, 0.5f, false);   // two turns a second inside the leaf storm
            GameObject storm = _enableVfx.Value ? CreateLeafStorm(player.transform.position, radius) : null;
            RangerArrowDamage d = ArrowDamage(player);
            float power = DragonCombat.GetSkillPower(player, "furious_winds");
            float slash = Mathf.Max(0f, _fwSlash.Value) / 100f * power * d.Total();
            float end = Time.time + duration, nextTick = 0f, nextDot = Time.time + 0.5f;
            while (Time.time < end && player != null && !player.IsDead())
            {
                Vector3 c = player.transform.position;
                if (storm != null) { storm.transform.position = c; storm.transform.Rotate(0f, 540f * Time.deltaTime, 0f, Space.World); }
                // v0.24.2 wind barrier: enemies never get closer than they are; inside the barrier they
                // drift out to it gradually (Small 1m -> edge in 5s, Big / Boss in 8s); outside it they
                // can't cross it. Enemy projectiles inside the barrier are blown away.
                List<Character> near = GetSphereTargets(player, c, radius + DragonCombat.M(1.5f));
                for (int i = 0; i < near.Count; i++)
                {
                    Character enemy = near[i];
                    Vector3 off = enemy.transform.position - c;
                    off.y = 0f;
                    if (off.sqrMagnitude < 0.0001f) off = player.transform.forward * 0.01f;
                    float dist = off.magnitude;
                    int key = enemy.GetInstanceID();
                    float prev;
                    if (!held.TryGetValue(key, out prev)) prev = dist;
                    float pushSeconds = Mathf.Max(0.5f, DragonCombat.IsSmallEnemy(enemy) ? _fwSmallPush.Value : _fwBigPush.Value);
                    float speed = Mathf.Max(DragonCombat.M(0.1f), barrier - DragonCombat.M(1f)) / pushSeconds;
                    float minDist = prev < barrier ? Mathf.Min(barrier, prev + speed * Time.deltaTime) : barrier;
                    if (dist < minDist)
                    {
                        Vector3 at = c + off / dist * minDist;
                        at.y = enemy.transform.position.y;
                        enemy.transform.position = at;
                        Rigidbody rb = enemy.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.position = at;
                            Vector3 v = rb.velocity;
                            Vector3 outward = off / dist;
                            float inward = Vector3.Dot(new Vector3(v.x, 0f, v.z), outward);
                            if (inward < 0f) rb.velocity = v - outward * inward;
                        }
                        dist = minDist;
                    }
                    held[key] = dist;
                }
                BlowAwayProjectiles(player, c, barrier);
                if (Time.time >= nextTick)
                {
                    nextTick = Time.time + Mathf.Max(0.05f, _fwInterval.Value);
                    for (int i = 0; i < near.Count; i++)
                    {
                        Character enemy = near[i];
                        Vector3 off = enemy.transform.position - c;
                        off.y = 0f;
                        if (off.magnitude > radius) continue;
                        RangerArrowDamage cut = new RangerArrowDamage();
                        cut.Slash = slash;
                        Deal(player, enemy, cut, 1f, 0f, false);
                        int id = enemy.GetInstanceID();
                        int stacks;
                        _fwStacks.TryGetValue(id, out stacks);
                        _fwStacks[id] = stacks + 1;
                        _fwStackUntil[id] = Time.time + Mathf.Max(0.5f, _fwDotDuration.Value);
                        OnSkillHit(player);
                    }
                }
                if (Time.time >= nextDot) { nextDot = Time.time + 0.5f; FuriousDotTick(player, d.Total() * power); }
                yield return null;
            }
            if (storm != null) Destroy(storm);
            if (player != null && !player.IsDead() && ascended)
            {
                List<Character> burst = GetSphereTargets(player, player.transform.position, radius + DragonCombat.M(1.5f));
                for (int i = 0; i < burst.Count; i++)
                {
                    Deal(player, burst[i], d, _fwAscBurst.Value / 100f * power, DragonCombat.IsSmallEnemy(burst[i]) ? 40f : 8f, false);
                    if (DragonCombat.IsSmallEnemy(burst[i])) Launch(burst[i], 9f);
                }
                if (_enableVfx.Value) StartCoroutine(RingVfx(player.transform.position, radius, new Color(0.55f, 1f, 0.70f, 1f), 0.8f));
            }
            // The DoT keeps ticking after the storm until each enemy's 6s run out.
            StartCoroutine(FuriousDotAfter(player, ArrowDamage(player).Total() * power));
        }

        private void FuriousDotTick(Player player, float baseDamage)
        {
            List<int> expired = new List<int>();
            foreach (KeyValuePair<int, int> pair in _fwStacks)
            {
                float until;
                if (!_fwStackUntil.TryGetValue(pair.Key, out until) || Time.time >= until) { expired.Add(pair.Key); continue; }
                Character target = FindCharacter(pair.Key);
                if (target == null || target.IsDead()) { expired.Add(pair.Key); continue; }
                DragonCombat.ApplySpiritBurnTick(player, target, baseDamage * Mathf.Max(0f, _fwDot.Value) / 100f * pair.Value);
            }
            for (int i = 0; i < expired.Count; i++) { _fwStacks.Remove(expired[i]); _fwStackUntil.Remove(expired[i]); }
        }

        private IEnumerator FuriousDotAfter(Player player, float baseDamage)
        {
            while (_fwStacks.Count > 0 && player != null)
            {
                yield return new WaitForSeconds(0.5f);
                FuriousDotTick(player, baseDamage);
            }
        }

        private Character FindCharacter(int instanceId)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return null;
            List<Character> all = GetSphereTargets(player, player.transform.position, DragonCombat.M(80f));
            for (int i = 0; i < all.Count; i++) if (all[i].GetInstanceID() == instanceId) return all[i];
            return null;
        }

        private void BlowAwayProjectiles(Player player, Vector3 center, float radius)
        {
            if (_projectileType == null) _projectileType = Type.GetType("Projectile, assembly_valheim");
            if (_projectileType == null) return;
            UnityEngine.Object[] all = UnityEngine.Object.FindObjectsOfType(_projectileType);
            FieldInfo ownerField = _projectileType.GetField("m_owner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < all.Length; i++)
            {
                Component p = all[i] as Component;
                if (p == null) continue;
                Character owner = ownerField == null ? null : ownerField.GetValue(p) as Character;
                if (owner == player || owner is Player) continue;
                if ((p.transform.position - center).sqrMagnitude <= radius * radius * 1.1f)
                    Destroy(p.gameObject);
            }
        }

        private GameObject CreateLeafStorm(Vector3 pos, float radius)
        {
            GameObject obj = new GameObject("RangerFuriousWinds");
            obj.transform.position = pos;
            Color[] colors = { new Color(0.45f, 0.95f, 0.45f, 0.9f), new Color(0.70f, 1f, 0.55f, 0.8f), new Color(0.40f, 0.85f, 0.70f, 0.8f) };
            for (int k = 0; k < 6; k++)
            {
                GameObject ring = new GameObject("Leaves" + k);
                ring.transform.SetParent(obj.transform, false);
                LineRenderer line = ring.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 20;
                line.startWidth = 0.18f;
                line.endWidth = 0.02f;
                Color c = colors[k % colors.Length];
                line.startColor = c;
                line.endColor = new Color(c.r, c.g, c.b, 0.05f);
                Material m = VfxMaterial(Color.white);
                if (m != null) line.material = m;
                float r = radius * (0.55f + 0.08f * k);
                for (int i = 0; i < 20; i++)
                {
                    float a = (float)i / 20f * Mathf.PI * 0.9f + k * 1.05f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0.3f + k * 0.45f, Mathf.Sin(a) * r));
                }
            }
            return obj;
        }

        // ------------------------------------------------------------------ Tailwind (Grace)
        private void CastTailwind(Player player)
        {
            if (CooldownRemaining("Acrobat.Tailwind") > 0f) { ShowCooldown("Acrobat.Tailwind"); return; }
            StartCooldown("Acrobat.Tailwind", _twCooldown.Value);
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlayClip(player, "rg_tailwind", 0.06f);   // v0.25.16 release-first
            ShowMessage("Tailwind");
            float duration = Mathf.Max(1f, _twDuration.Value);
            HashSet<Player> allies = new HashSet<Player>();
            allies.Add(player);
            Collider[] hits = Physics.OverlapSphere(player.transform.position, DragonCombat.M(_twRadius.Value));
            for (int i = 0; i < hits.Length; i++)
            {
                Player ally = hits[i].GetComponentInParent<Player>();
                if (ally != null) allies.Add(ally);
            }
            foreach (Player ally in allies)
            {
                DragonCombat.ApplyTimedBuff(ally, "Acrobat.Tailwind", duration, 0f, 0f, Mathf.Max(0f, _twMove.Value) / 100f, 0f, 0f, 0f, false);
                _tailwindUntil[ally.GetInstanceID()] = Time.time + duration;
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(player.transform.position, DragonCombat.M(_twRadius.Value), new Color(0.55f, 1f, 0.90f, 0.95f), 0.9f));
        }

        // ==================================================================================
        // v0.24.1 WILDBORN RUNTIME: input remap, infinite ammo, Ranger damage, gear rules
        // ==================================================================================
        private Harmony _harmony;
        private bool _charging, _releasePending;
        private float _releaseAt, _chargeDraw;
        private int _chain;
        private float _nextQuickShot;
        private float _qsPressedAt = -10f, _qsStart, _qsSpeed = 1.6f;
        private bool _qsWasIn, _qsMeasuring;

        // v0.24.4: the quick-shot animation is sped up until one shot (start -> end of the attack)
        // takes ChainInterval minus a small margin; the speed adapts to the bow's real animation.
        private void UpdateQuickShotSpeed(Player player)
        {
            ItemDrop.ItemData weapon = GetCurrentWeapon(player);
            bool inAtk = InAttack(player);
            bool quick = IsBow(weapon) && !_charging && !_releasePending;
            if (inAtk && !_qsWasIn) { _qsStart = Time.time; _qsMeasuring = quick && Time.time - _qsPressedAt < 0.35f; }
            if (inAtk && _qsMeasuring && quick) DragonCombat.SetAttackSpeedSource(player, _qsSpeed, 0.15f);
            if (!inAtk && _qsWasIn && _qsMeasuring)
            {
                float took = Time.time - _qsStart;
                float target = Mathf.Max(0.15f, _wbInterval.Value - 0.05f);
                if (took > 0.05f) _qsSpeed = Mathf.Clamp(_qsSpeed * took / target, 1f, 5f);
                _qsMeasuring = false;
            }
            _qsWasIn = inAtk;
        }
        private float _lastQuickShot;
        private float _pendingShotMult = 1f;
        private bool _pendingShotActive;
        private object _toggledAttack;

        private void InstallPatches()
        {
            try { _harmony = new Harmony(ModGuid); }
            catch (Exception ex) { Logger.LogWarning("Ranger: Harmony unavailable: " + ex.Message); return; }
            Type attack = typeof(Attack);
            Patch(FindMethod(attack, "UseAmmo", 1), "UseAmmoPrefix", null);
            Patch(FindStaticMethod(attack, "HaveAmmo", 2), null, "HaveAmmoPostfix");
            Patch(FindMethod(attack, "FireProjectileBurst", 0), "FireBurstPrefix", "FireBurstPostfix");
            Patch(FindMethod(typeof(Humanoid), "OnAttackTrigger", 0), "AttackTriggerPrefix", null);
            Patch(FindMethod(attack, "GetAttackStamina", 0), null, "AttackStaminaPostfix");
            Type projectile = Type.GetType("Projectile, assembly_valheim");
            if (projectile != null) Patch(FindMethod(projectile, "Setup", 6), "ProjectileSetupPrefix", null);
            Patch(FindMethod(typeof(Player), "GetAttackDrawPercentage", 0), null, "DrawPercentagePostfix");
            Patch(FindMethod(typeof(ItemDrop.ItemData), "GetWeaponLoadingTime", 0), null, "LoadingTimePostfix");
        }

        private static MethodInfo FindMethod(Type type, string name, int minParams)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < methods.Length; i++)
                    if (methods[i].Name == name && methods[i].GetParameters().Length >= minParams && (minParams > 0 || methods[i].GetParameters().Length == 0))
                        return methods[i];
            }
            return null;
        }

        private static MethodInfo FindStaticMethod(Type type, string name, int paramCount)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
                if (methods[i].Name == name && methods[i].GetParameters().Length == paramCount) return methods[i];
            return null;
        }

        private void Patch(MethodInfo original, string prefix, string postfix)
        {
            if (original == null) { Logger.LogWarning("Ranger: patch target missing for " + (prefix ?? postfix)); return; }
            HarmonyMethod pre = prefix == null ? null : new HarmonyMethod(typeof(RangerPlugin).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic));
            HarmonyMethod post = postfix == null ? null : new HarmonyMethod(typeof(RangerPlugin).GetMethod(postfix, BindingFlags.Static | BindingFlags.NonPublic));
            MethodInfo[] methods = typeof(Harmony).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != "Patch") continue;
                ParameterInfo[] ps = methods[i].GetParameters();
                if (ps.Length < 3 || !typeof(MethodBase).IsAssignableFrom(ps[0].ParameterType)) continue;
                bool ok = true;
                for (int p = 1; p < ps.Length; p++) if (ps[p].ParameterType != typeof(HarmonyMethod)) { ok = false; break; }
                if (!ok) continue;
                object[] args = new object[ps.Length];
                args[0] = original; args[1] = pre; args[2] = post;
                try { methods[i].Invoke(_harmony, args); return; }
                catch (Exception ex) { Logger.LogWarning("Ranger: could not patch " + original.Name + ": " + ex.Message); return; }
            }
        }

        private static bool IsBow(ItemDrop.ItemData item)
        {
            return item != null && item.m_shared != null && item.m_shared.m_skillType == Skills.SkillType.Bows;
        }

        private static bool IsCrossbow(ItemDrop.ItemData item)
        {
            return item != null && item.m_shared != null && item.m_shared.m_skillType == Skills.SkillType.Crossbows;
        }

        private static bool IsLocalRanger(Character c)
        {
            Player p = c as Player;
            return p != null && p == Player.m_localPlayer && Instance != null && Instance.GetClass(p) == "Ranger";
        }

        // Left Click = quick shots (no draw, full-draw range). Right Click held = vanilla charged shot;
        // Left Click releases it, letting go of Right Click cancels it. Rangers can never Block.
        private void RangerControls(Player player, ref bool attack, ref bool attackHold, ref bool block, ref bool blockHold)
        {
            if (player != Player.m_localPlayer || GetClass(player) != "Ranger") { RestoreBowDraw(); return; }
            bool rmb = block || blockHold;
            block = false;
            blockHold = false;
            ItemDrop.ItemData weapon = GetCurrentWeapon(player);
            if (!IsBow(weapon)) { _charging = false; RestoreBowDraw(); return; }   // Crossbows: vanilla Left Click
            if (_releasePending)
            {
                // v0.24.2: a release with no draw (too quick) never fires; free the bow at once.
                float since = Time.time - _releaseAt;
                if (since > 1.5f || (since > 0.2f && !InAttack(player))) { _releasePending = false; SetBowDraw(weapon, false); }
                attack = false; attackHold = false;
                return;
            }
            if (rmb)
            {
                if (!_charging) { _charging = true; SetBowDraw(weapon, true); }
                _chargeDraw = DrawPercent(player);
                if (attack) { _charging = false; _releasePending = true; _releaseAt = Time.time; attackHold = false; }
                else attackHold = true;
                attack = false;
                return;
            }
            if (_charging)
            {
                _charging = false;
                CancelDraw(player, weapon);
                attack = false; attackHold = false;
                return;
            }
            SetBowDraw(weapon, false);
            // v0.24.3: quick shots every 0.5s; the 4th (finisher) adds 1s before the next one.
            if (Time.time < _nextQuickShot || Time.time < _skillCastUntil) { attack = false; attackHold = false; }
            else if (attack || attackHold) _qsPressedAt = Time.time;
        }

        private static MethodInfo _inAttackMethod;

        private static bool InAttack(Player player)
        {
            try
            {
                if (_inAttackMethod == null) _inAttackMethod = typeof(Humanoid).GetMethod("InAttack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                MethodInfo m = _inAttackMethod;
                return m != null && Convert.ToBoolean(m.Invoke(player, null));
            }
            catch { return false; }
        }

        private void SetBowDraw(ItemDrop.ItemData weapon, bool draw)
        {
            if (weapon == null || weapon.m_shared == null) return;
            object atk = weapon.m_shared.m_attack;
            FieldInfo f = atk == null ? null : atk.GetType().GetField("m_bowDraw", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return;
            if (_toggledAttack != null && _toggledAttack != atk) RestoreBowDraw();
            if ((bool)f.GetValue(atk) != draw) f.SetValue(atk, draw);
            _toggledAttack = atk;
        }

        private void RestoreBowDraw()
        {
            if (_toggledAttack == null) return;
            FieldInfo f = _toggledAttack.GetType().GetField("m_bowDraw", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) f.SetValue(_toggledAttack, true);
            _toggledAttack = null;
        }

        private static MethodInfo _drawPctMethod;

        private float DrawPercent(Player player)
        {
            try
            {
                if (_drawPctMethod == null) _drawPctMethod = FindMethod(typeof(Player), "GetAttackDrawPercentage", 0);
                MethodInfo m = _drawPctMethod;
                return m == null ? 0f : Convert.ToSingle(m.Invoke(player, null));
            }
            catch { return 0f; }
        }

        private void CancelDraw(Player player, ItemDrop.ItemData weapon)
        {
            try
            {
                for (Type t = typeof(Player); t != null; t = t.BaseType)
                {
                    FieldInfo f = t.GetField("m_attackDrawTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (f != null) { f.SetValue(player, 0f); break; }
                }
                object atk = weapon.m_shared.m_attack;
                FieldInfo stateField = atk.GetType().GetField("m_drawAnimationState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                string state = stateField == null ? null : stateField.GetValue(atk) as string;
                FieldInfo zf = typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object zanim = zf == null ? null : zf.GetValue(player);
                if (zanim != null && !string.IsNullOrEmpty(state))
                {
                    MethodInfo setBool = zanim.GetType().GetMethod("SetBool", new Type[] { typeof(string), typeof(bool) });
                    if (setBool != null) setBool.Invoke(zanim, new object[] { state, false });
                }
            }
            catch { }
            SetBowDraw(weapon, false);
        }

        // Ammo is infinite for Rangers: the equipped arrow / bolt is used without being consumed;
        // with none at all a basic one is fired (and adds no damage).
        private static bool UseAmmoPrefix(Attack __instance, ref ItemDrop.ItemData __0, ref bool __result)
        {
            try
            {
                Character owner = ReadField(__instance, "m_character") as Character;
                if (Instance == null || !IsLocalRanger(owner)) return true;
                ItemDrop.ItemData weapon = ReadField(__instance, "m_weapon") as ItemDrop.ItemData;
                if (weapon == null || weapon.m_shared == null || string.IsNullOrEmpty(weapon.m_shared.m_ammoType)) return true;
                ItemDrop.ItemData ammo = Instance.FindAmmo((Player)owner, weapon);
                if (ammo == null) ammo = Instance.DefaultAmmo(weapon.m_shared.m_ammoType);
                if (ammo == null) return true;
                __0 = ammo;
                // v0.24.2 fix: FireProjectileBurst takes the arrow prefab from m_ammoItem; without it no arrow flew.
                for (Type t = typeof(Attack); t != null; t = t.BaseType)
                {
                    FieldInfo f = t.GetField("m_ammoItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (f != null) { f.SetValue(__instance, ammo); break; }
                }
                __result = true;
                return false;
            }
            catch { return true; }
        }

        // Infinite ammo: a Ranger can always draw / shoot, even with no arrows in the bag.
        private static void HaveAmmoPostfix(Humanoid __0, ref bool __result)
        {
            try
            {
                if (__result || Instance == null || !IsLocalRanger(__0)) return;
                ItemDrop.ItemData weapon = Instance.GetCurrentWeapon((Player)__0);
                if (IsBow(weapon) || IsCrossbow(weapon)) __result = true;
            }
            catch { }
        }

        private static void FireBurstPrefix(Attack __instance)
        {
            if (Instance == null) return;
            Character owner = ReadField(__instance, "m_character") as Character;
            if (!IsLocalRanger(owner)) return;
            Instance.PrepareShot(__instance);
        }

        private static void FireBurstPostfix()
        {
            if (Instance == null) return;
            Instance._pendingShotActive = false;
            Instance._pendingShotMult = 1f;
            if (Instance._releasePending)
            {
                Instance._releasePending = false;
                Player p = Player.m_localPlayer;
                if (p != null) Instance.SetBowDraw(Instance.GetCurrentWeapon(p), false);
            }
        }

        // Left Click chain: the 4th quick shot deals 150%. Bowmaster: fully charged shots +30%.
        private void PrepareShot(Attack attack)
        {
            Player player = Player.m_localPlayer;
            ItemDrop.ItemData weapon = ReadField(attack, "m_weapon") as ItemDrop.ItemData;
            float mult = 1f;
            object bowDraw = ReadField(attack, "m_bowDraw");
            bool charged = bowDraw is bool && (bool)bowDraw;
            if (IsBow(weapon) && !charged)
            {
                if (Time.time - _lastQuickShot > Mathf.Max(0.2f, _wbChainReset.Value)) _chain = 0;
                _lastQuickShot = Time.time;
                _chain++;
                float wait = Mathf.Max(0.1f, _wbInterval.Value);
                if (_chain >= 4) { mult *= Mathf.Max(0f, _wbChainBonus.Value) / 100f; _chain = 0; wait += Mathf.Max(0f, _wbFinisherDelay.Value); }
                _nextQuickShot = Time.time + wait;
            }
            else if (IsBow(weapon) && charged)
            {
                mult *= Mathf.Max(0f, _wbCharged.Value) / 100f;
                if (_chargeDraw >= 0.99f && GetAdvancement(player) == "Bowmaster") mult *= 1f + Mathf.Max(0f, _deFullCharge.Value) / 100f;
            }
            _pendingShotMult = mult;
            _pendingShotActive = true;
        }

        // Every Ranger projectile: 50% Bow + 50% ammo (full stack only), shot bonus, Focus, Hawk's Vigil;
        // Focus also adds range (faster arrow).
        private static void ProjectileSetupPrefix(object[] __args)
        {
            try
            {
                if (Instance == null || __args == null || __args.Length < 6) return;
                Character owner = __args[0] as Character;
                if (!IsLocalRanger(owner)) return;
                HitData hit = __args[3] as HitData;
                ItemDrop.ItemData weapon = __args[4] as ItemDrop.ItemData;
                ItemDrop.ItemData ammo = __args[5] as ItemDrop.ItemData;
                if (hit == null || !(IsBow(weapon) || IsCrossbow(weapon))) return;
                Player player = (Player)owner;
                bool full = ammo != null && Instance.HasFullStack(player, ammo);
                RangerArrowDamage w = new RangerArrowDamage(), a = new RangerArrowDamage();
                Instance.AddItemDamage(w, weapon, 1f);
                if (ammo != null) Instance.AddItemDamage(a, ammo, 1f);
                float bp = Instance._wbBowPercent.Value / 100f, ap = full ? Instance._wbAmmoPercent.Value / 100f : 0f;
                float extra = (Instance._pendingShotActive ? Instance._pendingShotMult : 1f) * Instance.RangedBonus(player);
                hit.m_damage.m_blunt *= Ratio(w.Blunt, a.Blunt, bp, ap) * extra;
                hit.m_damage.m_slash *= Ratio(w.Slash, a.Slash, bp, ap) * extra;
                hit.m_damage.m_pierce *= Ratio(w.Pierce, a.Pierce, bp, ap) * extra;
                hit.m_damage.m_fire *= Ratio(w.Fire, a.Fire, bp, ap) * extra;
                hit.m_damage.m_frost *= Ratio(w.Frost, a.Frost, bp, ap) * extra;
                hit.m_damage.m_lightning *= Ratio(w.Lightning, a.Lightning, bp, ap) * extra;
                hit.m_damage.m_poison *= Ratio(w.Poison, a.Poison, bp, ap) * extra;
                hit.m_damage.m_spirit *= Ratio(w.Spirit, a.Spirit, bp, ap) * extra;
                float range = Instance.FocusRangeMultiplier(player);
                if (range > 1.001f && __args[1] is Vector3) __args[1] = (Vector3)__args[1] * range;
            }
            catch { }
        }

        private static float Ratio(float weapon, float ammo, float weaponShare, float ammoShare)
        {
            float total = weapon + ammo;
            return total <= 0.0001f ? weaponShare : (weapon * weaponShare + ammo * ammoShare) / total;
        }

        private static void AttackStaminaPostfix(Attack __instance, ref float __result)
        {
            try
            {
                if (Instance == null || !IsLocalRanger(ReadField(__instance, "m_character") as Character)) return;
                object bowDraw = ReadField(__instance, "m_bowDraw");
                if (IsBow(ReadField(__instance, "m_weapon") as ItemDrop.ItemData) && bowDraw is bool && !(bool)bowDraw)
                    __result *= Mathf.Clamp01(Instance._wbQuickStamina.Value / 100f);
            }
            catch { }
        }

        // Charged-shot draw time: -50%, and the Bows skill removes the rest (Bows 100 = instant).
        private static void DrawPercentagePostfix(Humanoid __instance, ref float __result)
        {
            try
            {
                Player player = __instance as Player;
                if (Instance == null || player == null || !IsLocalRanger(player)) return;
                ItemDrop.ItemData weapon = Instance.GetCurrentWeapon(player);
                if (!IsBow(weapon)) return;
                float drawTime = 0f;
                for (Type t = typeof(Player); t != null; t = t.BaseType)
                {
                    FieldInfo f = t.GetField("m_attackDrawTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (f != null) { drawTime = (float)f.GetValue(player); break; }
                }
                if (drawTime <= 0f) return;
                object min = ReadField(weapon.m_shared.m_attack, "m_drawDurationMin");
                float baseTime = min is float ? (float)min : 1f;
                float skill = Mathf.Clamp01(player.GetSkillFactor(Skills.SkillType.Bows));
                float time = baseTime * Mathf.Clamp01(1f - Instance._wbDrawSpeed.Value / 100f) * (1f - skill);
                __result = time <= 0.01f ? 1f : Mathf.Clamp01(drawTime / time);
            }
            catch { }
        }

        // Bowmaster: Crossbow reload time -75%.
        private static void LoadingTimePostfix(ItemDrop.ItemData __instance, ref float __result)
        {
            try
            {
                Player p = Player.m_localPlayer;
                if (Instance == null || p == null || !IsCrossbow(__instance) || Instance.GetClass(p) != "Ranger" || Instance.GetAdvancement(p) != "Bowmaster") return;
                __result *= Mathf.Clamp01(1f - Instance._deReload.Value / 100f);
            }
            catch { }
        }

        private static object ReadField(object o, string name)
        {
            if (o == null) return null;
            for (Type t = o.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(o);
            }
            return null;
        }

        // The equipped / first ammo matching the weapon (arrows for Bows, bolts for Crossbows).
        private ItemDrop.ItemData FindAmmo(Player player, ItemDrop.ItemData weapon)
        {
            if (player == null || weapon == null || weapon.m_shared == null) return null;
            string type = weapon.m_shared.m_ammoType;
            if (string.IsNullOrEmpty(type)) return null;
            ItemDrop.ItemData equipped = GetAmmo(player);
            if (equipped != null && equipped.m_shared != null && equipped.m_shared.m_ammoType == type) return equipped;
            List<ItemDrop.ItemData> items = InventoryItems(player);
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null && items[i].m_shared != null && IsAmmo(items[i]) && items[i].m_shared.m_ammoType == type) return items[i];
            return null;
        }

        private bool HasFullStack(Player player, ItemDrop.ItemData ammo)
        {
            if (ammo == null || ammo.m_shared == null) return false;
            int count = 0;
            List<ItemDrop.ItemData> items = InventoryItems(player);
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null && items[i].m_shared != null && items[i].m_shared.m_name == ammo.m_shared.m_name) count += items[i].m_stack;
            return count >= Mathf.Max(1, Mathf.RoundToInt(_wbFullStack.Value));
        }

        private List<ItemDrop.ItemData> InventoryItems(Player player)
        {
            try
            {
                object inv = FindMethod(typeof(Player), "GetInventory", 0).Invoke(player, null);
                MethodInfo all = inv == null ? null : inv.GetType().GetMethod("GetAllItems", Type.EmptyTypes);
                List<ItemDrop.ItemData> list = all == null ? null : all.Invoke(inv, null) as List<ItemDrop.ItemData>;
                if (list != null) return list;
            }
            catch { }
            return new List<ItemDrop.ItemData>();
        }

        private readonly Dictionary<string, ItemDrop.ItemData> _defaultAmmo = new Dictionary<string, ItemDrop.ItemData>();

        private ItemDrop.ItemData DefaultAmmo(string ammoType)
        {
            ItemDrop.ItemData cached;
            if (_defaultAmmo.TryGetValue(ammoType, out cached)) return cached;
            ItemDrop.ItemData found = null;
            try
            {
                Type odb = Type.GetType("ObjectDB, assembly_valheim");
                object db = odb == null ? null : odb.GetField("instance", BindingFlags.Static | BindingFlags.Public).GetValue(null);
                MethodInfo get = db == null ? null : odb.GetMethod("GetItemPrefab", new Type[] { typeof(string) });
                string[] names = { "ArrowWood", "BoltBone", "ArrowFlint", "BoltIron" };
                for (int i = 0; i < names.Length && found == null && get != null; i++)
                {
                    GameObject prefab = get.Invoke(db, new object[] { names[i] }) as GameObject;
                    ItemDrop drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
                    if (drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null && drop.m_itemData.m_shared.m_ammoType == ammoType)
                        found = drop.m_itemData;
                }
            }
            catch { }
            if (found != null) _defaultAmmo[ammoType] = found;
            return found;
        }

        // No Shields for any Ranger; the Acrobat never wields a Crossbow.
        private float _gearMessageAt;

        private float _nextGearCheck;

        private void UpdateForbiddenGear(Player player, bool acrobat)
        {
            string[] hands = { "m_leftItem", "m_rightItem" };
            for (int i = 0; i < hands.Length; i++)
            {
                ItemDrop.ItemData item = ReadField(player, hands[i]) as ItemDrop.ItemData;
                if (item == null || item.m_shared == null) continue;
                bool shield = DragonCombat.IsShield(item);
                bool crossbow = acrobat && IsCrossbow(item);
                if (!shield && !crossbow) continue;
                try
                {
                    MethodInfo un = FindMethod(typeof(Humanoid), "UnequipItem", 1);
                    if (un != null)
                    {
                        object[] args = new object[un.GetParameters().Length];
                        args[0] = item;
                        for (int k = 1; k < args.Length; k++) args[k] = un.GetParameters()[k].ParameterType == typeof(bool) ? (object)true : null;
                        un.Invoke(player, args);
                    }
                }
                catch { }
                if (Time.time > _gearMessageAt)
                {
                    _gearMessageAt = Time.time + 2f;
                    ShowMessage(shield ? "Rangers cannot use Shields" : "The Acrobat cannot wield Crossbows");
                }
            }
        }

        // Bowmaster: a loaded Crossbow stays loaded when you holster / unequip it.
        private readonly HashSet<ItemDrop.ItemData> _loadedCrossbows = new HashSet<ItemDrop.ItemData>();
        private ItemDrop.ItemData _prevWeapon, _prevLoaded;
        private FieldInfo _loadedField;
        private bool _loadedFieldSearched;

        private void UpdateLoadedCrossbow(Player player, bool bowmaster)
        {
            if (!bowmaster) { _loadedCrossbows.Clear(); return; }
            if (!_loadedFieldSearched)
            {
                _loadedFieldSearched = true;
                for (Type t = typeof(Player); t != null && _loadedField == null; t = t.BaseType)
                {
                    FieldInfo[] fs = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < fs.Length; i++)
                        if (fs[i].FieldType == typeof(ItemDrop.ItemData) && fs[i].Name.IndexOf("Loaded", StringComparison.OrdinalIgnoreCase) >= 0) { _loadedField = fs[i]; break; }
                }
            }
            if (_loadedField == null) return;
            ItemDrop.ItemData weapon = GetCurrentWeapon(player);
            ItemDrop.ItemData loaded = _loadedField.GetValue(player) as ItemDrop.ItemData;
            if (loaded != null && IsCrossbow(loaded)) _loadedCrossbows.Add(loaded);
            if (loaded == null && IsCrossbow(weapon) && _loadedCrossbows.Contains(weapon))
            {
                if (_prevWeapon == weapon && _prevLoaded == weapon) _loadedCrossbows.Remove(weapon);   // it was fired
                else { _loadedField.SetValue(player, weapon); loaded = weapon; }                     // re-equipped: still loaded
            }
            _prevWeapon = weapon;
            _prevLoaded = loaded;
        }

        // ==================================================================================
        // v0.24.1 BOWMASTER
        // ==================================================================================
        private float _focus, _focusClock;
        private int _focusShown;
        private readonly Dictionary<int, float> _vigilUntil = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _pinnedUntil = new Dictionary<int, float>();
        private bool _ballistaCharging;
        private int _ballistaStacks;
        private float _ballistaNextStack;

        // Deadeye: +1 Focus per second standing still (max 5); moving / dodging drains it over 2s.
        private void UpdateFocus(Player player, bool bowmaster)
        {
            if (!bowmaster) { if (_focusShown > 0) DragonCombat.ClearStatus(player, "focus"); _focus = 0f; _focusShown = 0; return; }
            Rigidbody body = player.GetComponent<Rigidbody>();
            Vector3 v = body != null ? body.velocity : Vector3.zero;
            v.y = 0f;
            bool still = v.magnitude < DragonCombat.M(0.4f) && IsGrounded(player);
            float max = Mathf.Max(0f, _deFocusMax.Value);
            if (still) _focus = Mathf.Min(max, _focus + Time.deltaTime);
            else _focus = Mathf.Max(0f, _focus - Time.deltaTime * max / 2f);
            int stacks = Mathf.FloorToInt(_focus);
            if (stacks != _focusShown)
            {
                // v0.24.3: Focus shows as a buff with its stack count.
                if (stacks > 0) DragonCombat.ShowStatus(player, "focus", "focus", "Deadeye Focus", 0f, stacks, "Attack Buff\n+" + Mathf.RoundToInt(stacks * _deFocusDamage.Value).ToString() + "% Arrow Damage\n+" + Mathf.RoundToInt(stacks * _deFocusRange.Value).ToString() + "% Arrow Range\nStand still to build (max " + Mathf.RoundToInt(_deFocusMax.Value).ToString() + "), moving drains it");
                else DragonCombat.ClearStatus(player, "focus");
                if (stacks >= Mathf.RoundToInt(max) && max > 0f) ShowMessage("Deadeye - Focus " + stacks + "/" + Mathf.RoundToInt(max));
                _focusShown = stacks;
            }
        }

        private int FocusStacks(Player player)
        {
            return player != null && GetAdvancement(player) == "Bowmaster" ? Mathf.FloorToInt(_focus) : 0;
        }

        private float FocusRangeMultiplier(Player player)
        {
            return 1f + FocusStacks(player) * Mathf.Max(0f, _deFocusRange.Value) / 100f;
        }

        // Focus (+8% per stack) and Hawk's Vigil (+20% ranged) for every Ranger shot and skill hit.
        private float RangedBonus(Player player)
        {
            float m = 1f + FocusStacks(player) * Mathf.Max(0f, _deFocusDamage.Value) / 100f;
            float until;
            if (player != null && _vigilUntil.TryGetValue(player.GetInstanceID(), out until) && Time.time < until)
                m *= 1f + Mathf.Max(0f, _hvRanged.Value) / 100f;
            return m;
        }

        private bool IsPinned(Character target)
        {
            float until;
            return target != null && _pinnedUntil.TryGetValue(target.GetInstanceID(), out until) && Time.time < until;
        }

        private bool RequireRangedForBowmaster(Player player)
        {
            if (IsBow(GetCurrentWeapon(player)) || IsCrossbow(GetCurrentWeapon(player))) return true;
            ShowMessage("Requires a Bow or Crossbow");
            return false;
        }

        // ------------------------------------------------------------------ Ballista Shot (Signature)
        // Hold to charge (from the key press): 1 stack per second up to 3; at max you can keep holding,
        // rooted, re-aiming every frame. Releasing fires a huge piercing Laser Projectile.
        private void CastBallistaShot(Player player)
        {
            if (_ballistaCharging) return;
            if (!RequireRangedForBowmaster(player)) return;
            if (CooldownRemaining("Bowmaster.BallistaShot") > 0f) { ShowCooldown("Bowmaster.BallistaShot"); return; }
            if (GetStamina(player) < _bsStamina.Value) { ShowMessage("Not enough stamina"); return; }
            UseStamina(player, _bsStamina.Value);
            StartCoroutine(BallistaRoutine(player, DragonCombat.IsSkillAscended(player, "ballista_shot")));
        }

        private IEnumerator BallistaRoutine(Player player, bool ascended)
        {
            _ballistaCharging = true;
            _ballistaStacks = 0;
            _ballistaNextStack = Time.time + 1f;
            ShowMessage("Ballista Shot - hold to charge");
            DragonCombat.PlayClip(player, "rg_ballista", 0.35f, true);   // crouched heavy draw while charging
            float started = Time.time;
            while (player != null && !player.IsDead() && (DragonCombat.IsTreeSkillKeyHeld("ballista_shot") || Time.time - started < 0.15f))
            {
                DragonCombat.LockSkill(player, 0.15f);
                if (_ballistaStacks < 3 && Time.time >= _ballistaNextStack)
                {
                    _ballistaStacks++;
                    _ballistaNextStack = Time.time + 1f;
                    ShowMessage("Ballista Shot " + _ballistaStacks + "/3" + (_ballistaStacks >= 3 ? " - release to fire" : ""));
                }
                yield return null;
            }
            _ballistaCharging = false;
            if (player == null || player.IsDead()) yield break;
            StartCooldown("Bowmaster.BallistaShot", _bsCooldown.Value);
            int stacks = _ballistaStacks;
            RangerArrowDamage d = ArrowDamage(player);
            float mult = (_bsDamage.Value / 100f) * (1f + stacks * _bsStack.Value / 100f) * DragonCombat.GetSkillPower(player, "ballista_shot");
            float width = DragonCombat.M(_bsWidth.Value + stacks * _bsWidthStack.Value);
            float range = DragonCombat.M(_bsRange.Value) * FocusRangeMultiplier(player);
            Vector3 origin = ShotOrigin(player);
            Vector3 dir = AimDir(player, origin);
            Shoot(player, "BALLISTA SHOT" + (stacks > 0 ? " x" + stacks : ""));
            DragonCombat.ClipImpact(player);   // release: siege recoil
            StartCoroutine(ArrowFlight(player, origin, dir, DragonCombat.M(90f), range, width * 0.5f, true, new Color(0.75f, 1f, 0.55f, 1f), 0f,
                delegate(Character enemy)
                {
                    bool small = DragonCombat.IsSmallEnemy(enemy);
                    Deal(player, enemy, d, mult, small ? 30f : 6f, !small);
                    // v0.24.2 Ascended: the after-effect happens on hit (a piercing shot has no one left at its end).
                    if (ascended && stacks >= 3) BallistaShockwave(player, enemy, d, mult * _bsAscLinePercent.Value / 100f);
                    return true;
                }, null));
            if (_enableVfx.Value) LineVfx(origin, origin + dir * range, new Color(0.80f, 1f, 0.60f, 0.6f), width * 0.6f, 0.25f);
        }

        private void BallistaShockwave(Player player, Character source, RangerArrowDamage d, float mult)
        {
            if (player == null || source == null) return;
            Vector3 at = source.transform.position;
            float radius = DragonCombat.M(Mathf.Max(0.5f, _bsAscLine.Value));
            List<Character> hits = GetSphereTargets(player, at, radius);
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i] == source) continue;
                Deal(player, hits[i], d, mult, 10f, false);
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(at, radius, new Color(0.85f, 1f, 0.70f, 0.9f), 0.4f));
        }

        // ------------------------------------------------------------------ Arrow Rain (Signature)
        private void CastArrowRain(Player player)
        {
            if (!RequireRangedForBowmaster(player)) return;
            Vector3 point;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_arRange.Value) * FocusRangeMultiplier(player), out point)) { ShowMessage("Aim at the ground"); return; }
            if (!BeginSkill(player, "Bowmaster.ArrowRain", _arCooldown.Value, _arStamina.Value)) return;
            DragonCombat.PlayClip(player, "rg_sky", 0.15f);   // loose into the sky
            Shoot(player, "Arrow Rain");
            StartCoroutine(ArrowRainRoutine(player, point, DragonCombat.IsSkillAscended(player, "arrow_rain")));
        }

        private IEnumerator ArrowRainRoutine(Player player, Vector3 point, bool ascended)
        {
            float radius = DragonCombat.M(ascended ? _arAscRadius.Value : _arRadius.Value);
            float end = Time.time + Mathf.Max(0.5f, _arDuration.Value);
            RangerArrowDamage d = ArrowDamage(player);
            float mult = _arDamage.Value / 100f * DragonCombat.GetSkillPower(player, "arrow_rain");
            Dictionary<int, int> hits = new Dictionary<int, int>();
            while (Time.time < end && player != null)
            {
                List<Character> inside = GetSphereTargets(player, point, radius);
                for (int i = 0; i < inside.Count; i++)
                {
                    Character enemy = inside[i];
                    Deal(player, enemy, d, mult, 0f, false);
                    DragonCombat.ApplyCripple(enemy, _arCripple.Value);
                    if (ascended)
                    {
                        int n;
                        hits.TryGetValue(enemy.GetInstanceID(), out n);
                        n++;
                        hits[enemy.GetInstanceID()] = n;
                        if (n % Mathf.Max(1, Mathf.RoundToInt(_arAscFreezeHits.Value)) == 0) DragonCombat.Freeze(enemy, _arAscFreeze.Value, 0.3f);
                    }
                }
                if (_enableVfx.Value)
                {
                    Color c = ascended ? new Color(0.60f, 0.90f, 1f, 0.9f) : new Color(0.75f, 1f, 0.55f, 0.9f);
                    for (int k = 0; k < 10; k++)
                    {
                        Vector2 r = UnityEngine.Random.insideUnitCircle * radius;
                        Vector3 g = point + new Vector3(r.x, 0f, r.y);
                        LineVfx(g + Vector3.up * DragonCombat.M(9f) + new Vector3(0.6f, 0f, 0.6f), g, c, 0.05f, 0.18f);
                    }
                    StartCoroutine(RingVfx(point, radius, c, Mathf.Max(0.05f, _arInterval.Value)));
                }
                yield return new WaitForSeconds(Mathf.Max(0.1f, _arInterval.Value));
            }
        }

        // ------------------------------------------------------------------ Pinning Shot (Lv24)
        // v0.24.3: 3 arrows in a 30m cone; everyone inside the cone is hit and pinned (the arrows are
        // cosmetic). Small and Big are nailed in place, Bosses Crippled.
        private void CastPinningShot(Player player)
        {
            if (!RequireRangedForBowmaster(player)) return;
            if (!BeginSkill(player, "Bowmaster.PinningShot", _psCooldown.Value, _psStamina.Value)) return;
            DragonCombat.PlayClip(player, "rg_kneel", 0.15f);   // kneeling shot
            Shoot(player, "Pinning Shot");
            RangerArrowDamage d = ArrowDamage(player);
            float mult = _psDamage.Value / 100f * DragonCombat.GetSkillPower(player, "pinning_shot");
            float range = DragonCombat.M(_psRange.Value) * FocusRangeMultiplier(player);
            List<Character> hits = ConeArea(player, 3, Mathf.Clamp(_psCone.Value, 1f, 179f), range, new Color(0.75f, 1f, 0.55f, 1f));
            for (int i = 0; i < hits.Count; i++) Pin(player, hits[i], d, mult);
            if (DragonCombat.IsSkillAscended(player, "pinning_shot"))
            {
                int chains = Mathf.RoundToInt(_psAscChains.Value);
                for (int h = 0; h < hits.Count && chains > 0; h++)
                {
                    List<Character> near = GetSphereTargets(player, hits[h].transform.position, DragonCombat.M(_psAscChainRange.Value));
                    for (int i = 0; i < near.Count && chains > 0; i++)
                    {
                        if (hits.Contains(near[i])) continue;
                        if (_enableVfx.Value) LineVfx(hits[h].transform.position + Vector3.up, near[i].transform.position + Vector3.up, new Color(0.85f, 1f, 0.70f, 1f), 0.05f, 0.25f);
                        Pin(player, near[i], d, mult * 0.5f);
                        chains--;
                    }
                }
            }
        }

        private void Pin(Player player, Character target, RangerArrowDamage d, float mult)
        {
            Deal(player, target, d, mult, 0f, false);
            OnSkillHit(player);
            if (IsBossTarget(target)) { DragonCombat.ApplyCripple(target, _psBig.Value + 2f); return; }
            bool small = DragonCombat.IsSmallEnemy(target);
            float seconds = small ? _psSmall.Value : _psBig.Value;
            if (!small) DragonCombat.ApplyCripple(target, seconds + 2f);
            _pinnedUntil[target.GetInstanceID()] = Time.time + seconds;
            StartCoroutine(PinRoutine(target, seconds));
        }

        private IEnumerator PinRoutine(Character target, float seconds)
        {
            Vector3 at = target.transform.position;
            Rigidbody body = target.GetComponent<Rigidbody>();
            float end = Time.time + seconds;
            if (_enableVfx.Value) StartCoroutine(RingVfx(at, DragonCombat.M(1f), new Color(0.75f, 1f, 0.55f, 1f), seconds));
            while (Time.time < end && target != null && !target.IsDead())
            {
                target.transform.position = at;
                if (body != null) { body.position = at; body.velocity = Vector3.zero; }
                yield return new WaitForFixedUpdate();
            }
        }

        private static bool IsBossTarget(Character target)
        {
            try
            {
                MethodInfo m = target.GetType().GetMethod("IsBoss", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return m != null && Convert.ToBoolean(m.Invoke(target, null));
            }
            catch { return false; }
        }

        // ------------------------------------------------------------------ Explosive Arrow (Lv32)
        private void CastExplosiveArrow(Player player)
        {
            if (!RequireRangedForBowmaster(player)) return;
            if (!BeginSkill(player, "Bowmaster.ExplosiveArrow", _eaCooldown.Value, _eaStamina.Value)) return;
            Vector3 origin = ShotOrigin(player);
            Vector3 dir = AimDir(player, origin);
            DragonCombat.PlayClip(player, "rg_heavy", 0.12f);   // heavy kick-back
            Shoot(player, "Explosive Arrow");
            bool ascended = DragonCombat.IsSkillAscended(player, "explosive_arrow");
            StartCoroutine(ArrowFlight(player, origin, dir, DragonCombat.M(70f), DragonCombat.M(_eaRange.Value) * FocusRangeMultiplier(player), DragonCombat.M(0.35f), false,
                new Color(1f, 0.55f, 0.25f, 1f), 0f, delegate(Character enemy) { return false; },
                delegate(Vector3 at) { Explode(player, at, ascended); }));
        }

        private void Explode(Player player, Vector3 at, bool ascended)
        {
            if (player == null) return;
            float radius = DragonCombat.M(_eaRadius.Value);
            RangerArrowDamage d = ArrowDamage(player);
            float total = d.Total();
            RangerArrowDamage blast = new RangerArrowDamage();
            blast.Fire = total * 0.5f;
            blast.Blunt = total * 0.5f;
            float mult = _eaDamage.Value / 100f * DragonCombat.GetSkillPower(player, "explosive_arrow");
            List<Character> hits = GetSphereTargets(player, at, radius);
            for (int i = 0; i < hits.Count; i++)
            {
                Deal(player, hits[i], blast, mult, 12f, false);
                OnSkillHit(player);
                if (DragonCombat.IsSmallEnemy(hits[i])) DragonCombat.Stun(hits[i], at);
                StartCoroutine(BurnRoutine(player, hits[i], total * mult * Mathf.Max(0f, _eaBurn.Value) / 100f, _eaBurnDuration.Value));
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(at, radius, new Color(1f, 0.55f, 0.20f, 1f), 0.6f));
            if (ascended)
            {
                StartCoroutine(FireFieldRoutine(player, at, radius, total * mult * Mathf.Max(0f, _eaAscFieldPercent.Value) / 100f));
                StartCoroutine(ClusterRoutine(player, at, radius, blast, mult * Mathf.Max(0f, _eaAscClusterPercent.Value) / 100f));
            }
        }

        // Ascended: cluster bombs scatter from the blast and burst around it 0.35s later.
        private IEnumerator ClusterRoutine(Player player, Vector3 at, float radius, RangerArrowDamage blast, float mult)
        {
            yield return new WaitForSeconds(0.35f);
            int n = Mathf.Max(1, Mathf.RoundToInt(_eaAscClusters.Value));
            float r = DragonCombat.M(Mathf.Max(0.5f, _eaAscClusterRadius.Value));
            for (int i = 0; i < n && player != null; i++)
            {
                float a = (i + 0.5f) / n * Mathf.PI * 2f;
                Vector3 p = GroundAt(at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * 0.75f);
                List<Character> hits = GetSphereTargets(player, p, r);
                for (int k = 0; k < hits.Count; k++) Deal(player, hits[k], blast, mult, 6f, false);
                if (_enableVfx.Value) StartCoroutine(RingVfx(p, r, new Color(1f, 0.55f, 0.20f, 1f), 0.4f));
                yield return new WaitForSeconds(0.06f);
            }
        }

        private IEnumerator BurnRoutine(Player player, Character target, float perTick, float duration)
        {
            float end = Time.time + Mathf.Max(0.5f, duration);
            while (Time.time < end && target != null && !target.IsDead())
            {
                yield return new WaitForSeconds(0.5f);
                if (target != null && !target.IsDead()) DragonCombat.ApplyFireBurnTick(player, target, perTick);
            }
        }

        private IEnumerator FireFieldRoutine(Player player, Vector3 at, float radius, float perTick)
        {
            float end = Time.time + Mathf.Max(0.5f, _eaAscField.Value);
            while (Time.time < end && player != null)
            {
                if (_enableVfx.Value) StartCoroutine(RingVfx(at, radius * 0.9f, new Color(1f, 0.40f, 0.10f, 0.8f), 0.5f));
                List<Character> inside = GetSphereTargets(player, at, radius);
                for (int i = 0; i < inside.Count; i++) DragonCombat.ApplyFireBurnTick(player, inside[i], perTick);
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ------------------------------------------------------------------ Splitting Arrow (Lv32)
        // v0.24.2 (user design): stand still and loose 5-arrow volleys into a 120 degree cone, 15m.
        // The arrows pass through everything: each volley hits every enemy in the cone.
        // Ascended: 5 volleys 0.25s apart, each hit adds a stack of a Fire DoT (6s, refreshed).
        private readonly Dictionary<int, int> _saStacks = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _saStackUntil = new Dictionary<int, float>();

        private void CastSplittingArrow(Player player)
        {
            if (!RequireRangedForBowmaster(player)) return;
            if (!BeginSkill(player, "Bowmaster.SplittingArrow", _saCooldown.Value, _saStamina.Value)) return;
            StartCoroutine(SplittingRoutine(player, DragonCombat.IsSkillAscended(player, "splitting_arrow")));
        }

        private IEnumerator SplittingRoutine(Player player, bool ascended)
        {
            ShowMessage("Splitting Arrow");
            int volleys = Mathf.Max(1, Mathf.RoundToInt(ascended ? _saAscVolleys.Value : _saVolleys.Value));
            float interval = Mathf.Max(0.05f, ascended ? _saAscInterval.Value : _saInterval.Value);
            DragonCombat.LockSkill(player, volleys * interval + 0.2f);
            RangerArrowDamage d = ArrowDamage(player);
            float mult = _saDamage.Value / 100f * DragonCombat.GetSkillPower(player, "splitting_arrow");
            float range = DragonCombat.M(_saRange.Value) * FocusRangeMultiplier(player);
            float cone = Mathf.Clamp(_saCone.Value, 1f, 359f);
            int arrows = Mathf.Max(1, Mathf.RoundToInt(_saSplits.Value));
            bool burning = false;
            for (int v = 0; v < volleys && player != null && !player.IsDead(); v++)
            {
                Rigidbody body = player.GetComponent<Rigidbody>();
                if (body != null) { Vector3 vel = body.velocity; body.velocity = new Vector3(0f, Mathf.Min(0f, vel.y), 0f); }
                Vector3 origin = ShotOrigin(player);
                Vector3 aim = AimDir(player, origin);
                Vector3 flat = new Vector3(aim.x, 0f, aim.z);
                if (flat.sqrMagnitude < 0.001f) flat = player.transform.forward;
                flat.Normalize();
                FaceTowards(player, player.transform.position + flat);
                Shoot(player, null);
                DragonCombat.PlayClip(player, v % 2 == 0 ? "rg_split_a" : "rg_split_b", 0.06f);   // alternating sweep per volley
                if (_enableVfx.Value)
                    for (int i = 0; i < arrows; i++)
                    {
                        float a = arrows == 1 ? 0f : -cone * 0.5f + cone * i / (arrows - 1);
                        Vector3 sdir = Quaternion.AngleAxis(a, Vector3.up) * flat;
                        LineVfx(origin, origin + sdir * range, ascended ? new Color(1f, 0.65f, 0.30f, 0.95f) : new Color(0.75f, 1f, 0.55f, 0.95f), 0.07f, 0.2f);
                    }
                List<Character> all = GetSphereTargets(player, player.transform.position, range);
                for (int i = 0; i < all.Count; i++)
                {
                    Character enemy = all[i];
                    Vector3 to = enemy.transform.position - player.transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.25f && Vector3.Angle(flat, to) > cone * 0.5f) continue;
                    Deal(player, enemy, d, mult, 4f, false);
                    OnSkillHit(player);
                    if (ascended)
                    {
                        int id = enemy.GetInstanceID();
                        int stacks;
                        _saStacks.TryGetValue(id, out stacks);
                        _saStacks[id] = stacks + 1;
                        _saStackUntil[id] = Time.time + Mathf.Max(0.5f, _saAscBurnDuration.Value);
                        burning = true;
                    }
                }
                if (v + 1 < volleys) yield return new WaitForSeconds(interval);
            }
            if (burning && !_saBurnRunning && player != null) StartCoroutine(SplitBurnRoutine(player, ArrowDamage(player).Total() * DragonCombat.GetSkillPower(player, "splitting_arrow")));
        }

        private bool _saBurnRunning;

        private IEnumerator SplitBurnRoutine(Player player, float baseDamage)
        {
            _saBurnRunning = true;
            while (_saStacks.Count > 0 && player != null)
            {
                yield return new WaitForSeconds(0.5f);
                List<int> expired = new List<int>();
                foreach (KeyValuePair<int, int> pair in _saStacks)
                {
                    float until;
                    if (!_saStackUntil.TryGetValue(pair.Key, out until) || Time.time >= until) { expired.Add(pair.Key); continue; }
                    Character target = FindCharacter(pair.Key);
                    if (target == null || target.IsDead()) { expired.Add(pair.Key); continue; }
                    DragonCombat.ApplyFireBurnTick(player, target, baseDamage * Mathf.Max(0f, _saAscBurn.Value) / 100f * pair.Value);
                }
                for (int i = 0; i < expired.Count; i++) { _saStacks.Remove(expired[i]); _saStackUntil.Remove(expired[i]); }
            }
            _saBurnRunning = false;
        }

        // ------------------------------------------------------------------ Starfall Volley (Ultimate)
        private void CastStarfallVolley(Player player)
        {
            if (!RequireRangedForBowmaster(player)) return;
            Vector3 point;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_sfRange.Value) * FocusRangeMultiplier(player), out point)) { ShowMessage("Aim at the ground"); return; }
            if (!BeginSkill(player, "Bowmaster.StarfallVolley", _sfCooldown.Value, _sfStamina.Value)) return;
            StartCoroutine(StarfallRoutine(player, point, DragonCombat.IsSkillAscended(player, "starfall_volley")));
        }

        private IEnumerator StarfallRoutine(Player player, Vector3 point, bool ascended)
        {
            ShowMessage("STARFALL VOLLEY");
            float channel = Mathf.Max(0f, _sfChannel.Value);
            DragonCombat.LockSkill(player, channel);
            DragonCombat.PlayClip(player, "rg_starfall", Mathf.Max(0.2f, channel));
            float radius = DragonCombat.M(_sfRadius.Value);
            if (_enableVfx.Value) StartCoroutine(RingVfx(point, radius, new Color(0.80f, 1f, 0.60f, 0.8f), channel + _sfDuration.Value));
            yield return new WaitForSeconds(channel);
            if (player == null || player.IsDead()) yield break;
            RangerArrowDamage d = ArrowDamage(player);
            float mult = _sfDamage.Value / 100f * DragonCombat.GetSkillPower(player, "starfall_volley");
            float impact = DragonCombat.M(_sfImpact.Value);
            float end = Time.time + Mathf.Max(0.5f, _sfDuration.Value);
            float nextTick = 0f;
            // v0.24.2: every tick hits EVERY enemy in the 20m area; the falling arrows are cosmetic.
            while (Time.time < end && player != null)
            {
                if (Time.time >= nextTick)
                {
                    nextTick = Time.time + Mathf.Max(0.1f, _sfTick.Value);
                    StarHitAll(player, point, radius, d, mult);
                }
                if (_enableVfx.Value)
                {
                    Vector2 r = UnityEngine.Random.insideUnitCircle * radius;
                    StarVfx(GroundAt(point + new Vector3(r.x, 0f, r.y)), impact, 0.4f);
                }
                yield return new WaitForSeconds(Mathf.Max(0.05f, _sfInterval.Value));
            }
            if (ascended && player != null)
            {
                yield return new WaitForSeconds(0.3f);
                if (_enableVfx.Value) StarVfx(GroundAt(point), DragonCombat.M(_sfAscRadius.Value), 1.5f);
                StarHitAll(player, point, DragonCombat.M(_sfAscRadius.Value), d, _sfAscDamage.Value / 100f * DragonCombat.GetSkillPower(player, "starfall_volley"));
            }
        }

        private void StarVfx(Vector3 at, float radius, float width)
        {
            LineVfx(at + Vector3.up * DragonCombat.M(25f), at, new Color(0.85f, 1f, 0.70f, 1f), width, 0.25f);
            StartCoroutine(RingVfx(at, radius, new Color(0.85f, 1f, 0.70f, 0.9f), 0.3f));
        }

        private void StarHitAll(Player player, Vector3 at, float radius, RangerArrowDamage d, float mult)
        {
            List<Character> hits = GetSphereTargets(player, at, radius);
            for (int i = 0; i < hits.Count; i++)
            {
                Deal(player, hits[i], d, mult, 10f, !DragonCombat.IsSmallEnemy(hits[i]));
                OnSkillHit(player);
            }
        }

        // ------------------------------------------------------------------ Hawk's Vigil (Grace)
        private void CastHawksVigil(Player player)
        {
            if (CooldownRemaining("Bowmaster.HawksVigil") > 0f) { ShowCooldown("Bowmaster.HawksVigil"); return; }
            StartCooldown("Bowmaster.HawksVigil", _hvCooldown.Value);
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlayClip(player, "rg_vigil", 0.06f);   // v0.25.16 release-first
            ShowMessage("Hawk's Vigil");
            float duration = Mathf.Max(1f, _hvDuration.Value);
            Collider[] hits = Physics.OverlapSphere(player.transform.position, DragonCombat.M(_hvRadius.Value));
            HashSet<Player> allies = new HashSet<Player>();
            allies.Add(player);
            for (int i = 0; i < hits.Length; i++)
            {
                Player ally = hits[i].GetComponentInParent<Player>();
                if (ally != null) allies.Add(ally);
            }
            foreach (Player ally in allies) _vigilUntil[ally.GetInstanceID()] = Time.time + duration;
            StartCoroutine(VigilMarks(player, duration));
        }

        // v0.24.3: enemies in range glow red (instead of floating markers) while Hawk's Vigil lasts.
        private IEnumerator VigilMarks(Player player, float duration)
        {
            DragonCombat.ShowStatus(player, "hawks_vigil", "vigil", "Hawk's Vigil", duration, 0, "Attack Buff\n+" + Mathf.RoundToInt(_hvRanged.Value).ToString() + "% Ranged Damage (you and allies within " + Mathf.RoundToInt(_hvRadius.Value).ToString() + "m)\nEnemies within " + Mathf.RoundToInt(_hvReveal.Value).ToString() + "m are marked");
            float end = Time.time + duration;
            HashSet<Character> lit = new HashSet<Character>();
            while (Time.time < end && player != null && !player.IsDead())
            {
                List<Character> enemies = GetSphereTargets(player, player.transform.position, DragonCombat.M(_hvReveal.Value));
                HashSet<Character> now = new HashSet<Character>(enemies);
                foreach (Character c in lit) if (c != null && !now.Contains(c)) SetRedHighlight(c, false);
                lit.RemoveWhere(delegate(Character c) { return c == null || !now.Contains(c); });
                for (int i = 0; i < enemies.Count; i++) if (lit.Add(enemies[i])) SetRedHighlight(enemies[i], true);
                yield return new WaitForSeconds(0.5f);
            }
            foreach (Character c in lit) if (c != null) SetRedHighlight(c, false);
        }

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void SetRedHighlight(Character c, bool on)
        {
            if (c == null) return;
            Color red = new Color(0.85f, 0.04f, 0.02f, 1f);
            // Valheim's MaterialMan owns the property blocks when present; fall back to our own block.
            try
            {
                Type mm = Type.GetType("MaterialMan, assembly_valheim");
                object inst = mm == null ? null : mm.GetProperty("instance", BindingFlags.Static | BindingFlags.Public).GetValue(null, null);
                if (inst != null)
                {
                    if (on)
                    {
                        MethodInfo set = mm.GetMethod("SetValue", new Type[] { typeof(GameObject), typeof(int), typeof(Color) });
                        if (set != null) { set.Invoke(inst, new object[] { c.gameObject, EmissionId, red }); return; }
                    }
                    else
                    {
                        MethodInfo reset = mm.GetMethod("ResetValue", new Type[] { typeof(GameObject), typeof(int) });
                        if (reset != null) { reset.Invoke(inst, new object[] { c.gameObject, EmissionId }); return; }
                    }
                }
            }
            catch { }
            Renderer[] rs = c.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] is ParticleSystemRenderer || rs[i] is LineRenderer) continue;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                rs[i].GetPropertyBlock(block);
                block.SetColor(EmissionId, on ? red : Color.black);
                rs[i].SetPropertyBlock(block);
            }
        }

        // ------------------------------------------------------------------ shared skill helpers
        // Windstep: each enemy hit by a skill takes 1s off the shortest running cooldown (max 3s per cast).
        private void OnSkillHit(Player player)
        {
            if (player == null || GetAdvancement(player) != "Acrobat" || _refundLeft <= 0f) return;
            string best = null;
            float bestLeft = float.MaxValue;
            foreach (KeyValuePair<string, float> pair in _cooldowns)
            {
                float left = pair.Value - Time.time;
                if (left > 0f && left < bestLeft) { bestLeft = left; best = pair.Key; }
            }
            if (best == null || best == "Acrobat.Tailwind") return;
            float cut = Mathf.Min(_refundLeft, Mathf.Max(0f, _wsRefund.Value));
            _cooldowns[best] = _cooldowns[best] - cut;
            _refundLeft -= cut;
        }

        // v0.25.33 user (Tumble Shot, Gale Volley): REAL arrows. Every arrow that touches an enemy deals its full
        // damage (several arrows on one target all count), arrows pierce every enemy without limit and stop only
        // on terrain / physical objects.
        private void FanPierce(Player player, int count, float fanDegrees, float range, Color color, Action<Character> onHit)
        {
            Vector3 origin = ShotOrigin(player);
            Vector3 dir = AimDir(player, origin);
            for (int i = 0; i < count; i++)
            {
                float a = count == 1 ? 0f : -fanDegrees * 0.5f + fanDegrees * i / (count - 1);
                Vector3 shot = (Quaternion.AngleAxis(a, Vector3.up) * dir).normalized;
                StartCoroutine(ArrowFlight(player, origin, shot, DragonCombat.M(60f), range, DragonCombat.M(0.35f), true, color, 0f,
                    delegate(Character enemy)
                    {
                        if (enemy == null || enemy.IsDead()) return true;
                        onHit(enemy);
                        OnSkillHit(player);
                        return true;
                    }, null));
            }
        }

        // v0.24.3 universal rule: everyone inside the skill's area is hit; the arrows are cosmetic.
        private void Fan(Player player, int count, float fanDegrees, float range, float pitchUp, Color color, Func<Character, bool> onHit)
        {
            List<Character> hits = ConeArea(player, count, fanDegrees, range, color);
            for (int i = 0; i < hits.Count; i++) { onHit(hits[i]); OnSkillHit(player); }
        }

        // Fires count cosmetic arrows across the cone and returns every enemy inside it (range, half
        // angle = cone / 2, at least 8 degrees so a narrow fan still catches what it is aimed at).
        private List<Character> ConeArea(Player player, int count, float coneDegrees, float range, Color color)
        {
            Vector3 origin = ShotOrigin(player);
            Vector3 dir = AimDir(player, origin);
            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude < 0.001f) flat = player.transform.forward;
            flat.Normalize();
            for (int i = 0; i < count; i++)
            {
                float a = count == 1 ? 0f : -coneDegrees * 0.5f + coneDegrees * i / (count - 1);
                Vector3 shot = Quaternion.AngleAxis(a, Vector3.up) * dir;
                StartCoroutine(ArrowFlight(player, origin, shot.normalized, DragonCombat.M(60f), range, DragonCombat.M(0.3f), true, color, 0f,
                    delegate(Character enemy) { return true; }, null));
            }
            List<Character> result = new List<Character>();
            float half = Mathf.Max(8f, coneDegrees * 0.5f);
            List<Character> all = GetSphereTargets(player, player.transform.position, range);
            for (int i = 0; i < all.Count; i++)
            {
                Vector3 to = all[i].transform.position - player.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.25f && Vector3.Angle(flat, to) > half) continue;
                result.Add(all[i]);
            }
            return result;
        }

        // Laser Projectile: straight, no fall off. pierce = keep flying through creatures; walls and
        // terrain always stop it. growTo > 0 widens the hitbox up to that radius at max range.
        private IEnumerator ArrowFlight(Player player, Vector3 pos, Vector3 dir, float speed, float range, float radius, bool pierce, Color color, float growTo, Func<Character, bool> onHit, Action<Vector3> onEnd)
        {
            GameObject vfx = _enableVfx.Value ? CreateArrowVfx(color) : null;
            HashSet<int> done = new HashSet<int>();
            float traveled = 0f;
            bool stop = false;
            while (traveled < range && !stop)
            {
                float step = Mathf.Min(speed * Time.deltaTime, range - traveled);
                float r = growTo > 0f ? Mathf.Lerp(radius, growTo, traveled / range) : radius;
                RaycastHit[] hits = Physics.SphereCastAll(pos, r, dir, step, ~0, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
                for (int i = 0; i < hits.Length && !stop; i++)
                {
                    Collider c = hits[i].collider;
                    if (c == null || IsPlayerCollider(player, c)) continue;
                    Character ch = c.GetComponentInParent<Character>();
                    if (ch != null)
                    {
                        if (!IsEnemy(player, ch) || !done.Add(ch.GetInstanceID())) continue;
                        onHit(ch);
                        if (!pierce) { stop = true; pos += dir * hits[i].distance; }
                    }
                    else if (IsSolid(c))
                    {
                        stop = true;
                        pos += dir * hits[i].distance;
                    }
                }
                if (!stop) { pos += dir * step; traveled += step; }
                if (vfx != null)
                {
                    vfx.transform.position = pos;
                    vfx.transform.rotation = Quaternion.LookRotation(dir);
                    if (growTo > 0f) vfx.transform.localScale = new Vector3(r * 0.6f + 0.05f, r * 0.6f + 0.05f, 1.2f);
                }
                yield return null;
            }
            if (vfx != null) Destroy(vfx);
            if (onEnd != null) onEnd(pos);
        }

        private void Shoot(Player player, string message)
        {
            MarkSkillCast(player);
            // v0.25.14: no extra body recoil per shot (the vanilla bow animation already recoils; the added
            // tilt read as shaking during quick-shot chains).
            DragonCombat.LockSkill(player, 0.35f);
            DragonCombat.PlayWeaponAnimation(player, false, "bow_fire");
            if (!string.IsNullOrEmpty(message)) ShowMessage(message);
        }

        private void Leap(Player player, Vector3 flatDir, float distance, float duration)
        {
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body == null) return;
            Vector3 v = flatDir.normalized * (distance / Mathf.Max(0.1f, duration));
            v.y = DragonCombat.M(6f);
            body.velocity = v;
            _noFallUntilGrounded[player.GetInstanceID()] = true;
        }

        private void Launch(Character target, float up)
        {
            Rigidbody body = target == null ? null : target.GetComponent<Rigidbody>();
            if (body == null) return;
            body.AddForce(Vector3.up * DragonCombat.M(up), ForceMode.VelocityChange);
        }

        private void PullToward(Character target, Vector3 center, float strength)
        {
            Rigidbody body = target == null ? null : target.GetComponent<Rigidbody>();
            if (body == null) return;
            Vector3 dir = center - target.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.05f) body.AddForce(dir.normalized * strength, ForceMode.VelocityChange);
        }

        private void FaceTowards(Player player, Vector3 point)
        {
            Vector3 to = point - player.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(to.normalized);
        }

        private Vector3 GroundAt(Vector3 point)
        {
            RaycastHit hit;
            if (Physics.Raycast(point + Vector3.up * 6f, Vector3.down, out hit, 20f, SolidMask(), QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.1f;
            return point;
        }

        private List<Character> PathTargets(Player player, Vector3 a, Vector3 b, float radius)
        {
            List<Character> result = new List<Character>();
            HashSet<int> seen = new HashSet<int>();
            float length = Vector3.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / Mathf.Max(0.5f, radius)));
            for (int i = 0; i <= steps; i++)
            {
                List<Character> hits = GetSphereTargets(player, Vector3.Lerp(a, b, (float)i / steps) + Vector3.up * 0.8f, radius);
                for (int k = 0; k < hits.Count; k++)
                    if (seen.Add(hits[k].GetInstanceID())) result.Add(hits[k]);
            }
            return result;
        }

        private Character NearestEnemy(Player player, float range, HashSet<int> skip)
        {
            List<Character> all = GetSphereTargets(player, player.transform.position, range);
            Character best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                if (skip.Contains(all[i].GetInstanceID())) continue;
                float d = Vector3.Distance(player.transform.position, all[i].transform.position);
                if (d < bestDist) { bestDist = d; best = all[i]; }
            }
            return best;
        }

        // Crosshair target (creature hitboxes count), else the enemy closest to the aim within 10°.
        private Character AimedEnemy(Player player, float range)
        {
            Vector3 origin = GameCamera.instance != null ? GameCamera.instance.transform.position : player.GetEyePoint();
            Vector3 dir = GameCamera.instance != null ? GameCamera.instance.transform.forward : player.GetLookDir();
            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.35f, dir, range + 6f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (c == null || IsPlayerCollider(player, c)) continue;
                Character ch = c.GetComponentInParent<Character>();
                if (ch != null && IsEnemy(player, ch)) return ch;
                if (ch == null && IsSolid(c)) break;
            }
            Vector3 eye = player.GetEyePoint();
            Vector3 forward = AimDir(player, eye);
            Character best = null;
            float bestAngle = 10f;
            List<Character> all = GetSphereTargets(player, player.transform.position, range);
            for (int i = 0; i < all.Count; i++)
            {
                Vector3 to = all[i].transform.position + Vector3.up * 0.8f - eye;
                float angle = Vector3.Angle(forward, to);
                if (angle < bestAngle) { bestAngle = angle; best = all[i]; }
            }
            return best;
        }

        // ------------------------------------------------------------------ damage
        private bool RequireBow(Player player)
        {
            if (IsRangedWeapon(GetCurrentWeapon(player))) return true;
            ShowMessage("Requires a Bow or Crossbow");
            return false;
        }

        private static bool IsRangedWeapon(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return false;
            Skills.SkillType s = item.m_shared.m_skillType;
            return s == Skills.SkillType.Bows || s == Skills.SkillType.Crossbows;
        }

        // v0.24.1 Ranger damage: 50% of the Bow / Crossbow + 50% of the equipped ammo, the ammo part
        // only while you carry a full stack (100) of it. Ammo is never consumed.
        private RangerArrowDamage ArrowDamage(Player player)
        {
            RangerArrowDamage d = new RangerArrowDamage();
            ItemDrop.ItemData weapon = GetCurrentWeapon(player);
            AddItemDamage(d, weapon, _wbBowPercent.Value / 100f);
            ItemDrop.ItemData ammo = FindAmmo(player, weapon);
            if (ammo != null && HasFullStack(player, ammo)) AddItemDamage(d, ammo, _wbAmmoPercent.Value / 100f);
            if (d.Total() <= 0f) d.Pierce = 15f;
            return d;
        }

        private void AddItemDamage(RangerArrowDamage d, ItemDrop.ItemData item, float scale)
        {
            if (item == null || !IsRangedWeapon(item) && !IsAmmo(item)) return;
            try
            {
                MethodInfo m = item.GetType().GetMethod("GetDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                object dmg = m == null ? null : m.Invoke(item, null);
                if (dmg == null) return;
                d.Blunt += Field(dmg, "m_blunt") * scale; d.Slash += Field(dmg, "m_slash") * scale; d.Pierce += Field(dmg, "m_pierce") * scale;
                d.Fire += Field(dmg, "m_fire") * scale; d.Frost += Field(dmg, "m_frost") * scale; d.Lightning += Field(dmg, "m_lightning") * scale;
                d.Poison += Field(dmg, "m_poison") * scale; d.Spirit += Field(dmg, "m_spirit") * scale;
            }
            catch { }
        }

        private static bool IsAmmo(ItemDrop.ItemData item)
        {
            try
            {
                FieldInfo f = item.m_shared.GetType().GetField("m_itemType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return f != null && f.GetValue(item.m_shared) != null && f.GetValue(item.m_shared).ToString() == "Ammo";
            }
            catch { return false; }
        }

        private static float Field(object o, string name)
        {
            try
            {
                FieldInfo f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return f == null ? 0f : Convert.ToSingle(f.GetValue(o));
            }
            catch { return 0f; }
        }

        private void Deal(Player attacker, Character target, RangerArrowDamage d, float multiplier, float push, bool stagger)
        {
            if (target == null || target.IsDead() || d == null) return;
            float m = Mathf.Max(0f, multiplier) * RangedBonus(attacker);
            if (IsPinned(target)) m *= 1f + Mathf.Max(0f, _psBonus.Value) / 100f;
            HitData hit = new HitData();
            hit.m_damage.m_blunt = d.Blunt * m;
            hit.m_damage.m_slash = d.Slash * m;
            hit.m_damage.m_pierce = d.Pierce * m;
            hit.m_damage.m_fire = d.Fire * m;
            hit.m_damage.m_frost = d.Frost * m;
            hit.m_damage.m_lightning = d.Lightning * m;
            hit.m_damage.m_poison = d.Poison * m;
            hit.m_damage.m_spirit = d.Spirit * m;
            hit.m_point = target.transform.position + Vector3.up;
            hit.m_dir = (target.transform.position - attacker.transform.position).normalized;
            hit.m_pushForce = push;
            hit.SetAttacker(attacker);
            target.Damage(hit);
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

        private bool IsPlayerCollider(Player player, Collider collider)
        {
            if (player == null || collider == null) return false;
            Transform t = collider.transform;
            if (t == player.transform || t.IsChildOf(player.transform)) return true;
            return collider.GetComponentInParent<Character>() == player;
        }

        private static bool IsSolid(Collider c)
        {
            return c != null && !c.isTrigger && (SolidMask() & (1 << c.gameObject.layer)) != 0;
        }

        private static int SolidMask()
        {
            int mask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece", "Default_small", "vehicle");
            return mask == 0 ? ~0 : mask;
        }

        private Vector3 ShotOrigin(Player player)
        {
            return player.GetEyePoint() + FlatAim(player) * 0.4f - Vector3.up * 0.15f;
        }

        private Vector3 AimDir(Player player, Vector3 origin)
        {
            Vector3 dir = AlbedoAimUtility.GetProjectileDirection(player, origin);
            if (dir.sqrMagnitude < 0.01f) dir = player.transform.forward;
            return dir.normalized;
        }

        private Vector3 FlatAim(Player player)
        {
            Vector3 f = GameCamera.instance != null ? GameCamera.instance.transform.forward : player.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.01f) f = player.transform.forward;
            return f.normalized;
        }

        // ------------------------------------------------------------------ cooldowns / resources
        private bool BeginSkill(Player player, string id, float cooldown, float stamina)
        {
            if (CooldownRemaining(id) > 0f) { ShowCooldown(id); return false; }
            if (GetStamina(player) < stamina) { ShowMessage("Not enough stamina"); return false; }
            UseStamina(player, stamina);
            StartCooldown(id, cooldown);
            MarkSkillCast(player);
            return true;
        }

        // v0.24.3: a skill's animation must never fire the last normal attack again. Valheim keeps
        // the previous Attack as m_currentAttack and the skill's bow animation event re-triggers it
        // (an extra normal arrow on every skill right after normal shots).
        private float _skillCastUntil;

        private void MarkSkillCast(Player player)
        {
            _skillCastUntil = Time.time + 0.8f;
            _charging = false;
            _releasePending = false;
            try
            {
                FieldInfo f = typeof(Humanoid).GetField("m_currentAttack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) f.SetValue(player, null);
            }
            catch { }
        }

        private static bool AttackTriggerPrefix(Humanoid __instance)
        {
            return Instance == null || !IsLocalRanger(__instance) || Time.time >= Instance._skillCastUntil;
        }

        private float RechargeSeconds(Player player, string id, float seconds)
        {
            return _testingForceCooldowns.Value ? _testingCooldown.Value : DragonCombat.ScaleCooldown(player, id, seconds);
        }

        private void StartCooldown(string id, float normal)
        {
            _cooldowns[id] = Time.time + Mathf.Max(0f, RechargeSeconds(Player.m_localPlayer, id, normal));
        }

        private float CooldownRemaining(string id)
        {
            float end;
            return _cooldowns.TryGetValue(id, out end) ? Mathf.Max(0f, end - Time.time) : 0f;
        }

        private void ShowCooldown(string id)
        {
            ShowMessage("Cooldown " + CooldownRemaining(id).ToString("0.0") + "s");
        }

        private float GetStamina(Player player)
        {
            try
            {
                MethodInfo m = typeof(Player).GetMethod("GetStamina", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (m != null) return Convert.ToSingle(m.Invoke(player, null));
            }
            catch { }
            return 999f;
        }

        private void UseStamina(Player player, float amount)
        {
            if (amount <= 0f) return;
            try
            {
                MethodInfo[] methods = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Name != "UseStamina") continue;
                    ParameterInfo[] ps = methods[i].GetParameters();
                    if (ps.Length < 1 || ps[0].ParameterType != typeof(float)) continue;
                    object[] args = new object[ps.Length];
                    args[0] = amount;
                    for (int j = 1; j < ps.Length; j++) args[j] = ps[j].HasDefaultValue ? ps[j].DefaultValue : (ps[j].ParameterType.IsValueType ? Activator.CreateInstance(ps[j].ParameterType) : null);
                    methods[i].Invoke(player, args);
                    return;
                }
            }
            catch { }
        }

        private ItemDrop.ItemData GetCurrentWeapon(Player player)
        {
            return InvokeItem(player, "GetCurrentWeapon");
        }

        private ItemDrop.ItemData GetAmmo(Player player)
        {
            return InvokeItem(player, "GetAmmoItem");
        }

        private ItemDrop.ItemData InvokeItem(Player player, string method)
        {
            if (player == null) return null;
            try
            {
                MethodInfo m = player.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                return m == null ? null : m.Invoke(player, null) as ItemDrop.ItemData;
            }
            catch { return null; }
        }

        private static MethodInfo _onGroundMethod;
        private static FieldInfo _customDataField;

        private bool IsGrounded(Player player)
        {
            try
            {
                if (_onGroundMethod == null) _onGroundMethod = typeof(Character).GetMethod("IsOnGround", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo m = _onGroundMethod;
                if (m != null) return Convert.ToBoolean(m.Invoke(player, null));
            }
            catch { }
            return true;
        }

        private void ResetFloatField(Player player, string fieldName, float value)
        {
            try
            {
                FieldInfo field = typeof(Character).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(float)) field.SetValue(player, value);
            }
            catch { }
        }

        private string GetClass(Player player) { return ReadData(player, ClassDataKey); }
        private string GetAdvancement(Player player) { return ReadData(player, AdvancementDataKey); }

        private string ReadData(Player player, string key)
        {
            if (player == null) return "";
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

        // ------------------------------------------------------------------ VFX
        private static Material VfxMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            Material m = shader != null ? new Material(shader) : null;
            if (m != null) m.color = color;
            return m;
        }

        private GameObject CreateArrowVfx(Color color)
        {
            GameObject obj = new GameObject("RangerArrow");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(0f, 0f, -0.9f));
            line.SetPosition(1, Vector3.zero);
            line.startWidth = 0.02f;
            line.endWidth = 0.09f;
            line.startColor = new Color(color.r, color.g, color.b, 0f);
            line.endColor = color;
            Material m = VfxMaterial(Color.white);
            if (m != null) line.material = m;
            return obj;
        }

        private void LineVfx(Vector3 a, Vector3 b, Color color, float width, float life)
        {
            GameObject obj = new GameObject("RangerStreak");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            line.startWidth = width * 0.4f;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            Material m = VfxMaterial(Color.white);
            if (m != null) line.material = m;
            Destroy(obj, Mathf.Max(0.05f, life));
        }

        private GameObject CreateSpinRing(Vector3 pos, float radius, Color color)
        {
            GameObject obj = new GameObject("RangerCyclone");
            obj.transform.position = pos;
            for (int k = 0; k < 3; k++)
            {
                GameObject ring = new GameObject("Ring" + k);
                ring.transform.SetParent(obj.transform, false);
                LineRenderer line = ring.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = false;
                line.positionCount = 24;
                line.startWidth = 0.08f;
                line.endWidth = 0.02f;
                line.startColor = color;
                line.endColor = new Color(color.r, color.g, color.b, 0.1f);
                Material m = VfxMaterial(Color.white);
                if (m != null) line.material = m;
                float r = radius * (0.45f + 0.25f * k);
                for (int i = 0; i < 24; i++)
                {
                    float a = (float)i / 24f * Mathf.PI * 1.6f + k * 2.1f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, -0.6f + k * 0.6f, Mathf.Sin(a) * r));
                }
            }
            return obj;
        }

        private GameObject CreateTrapVisual(Vector3 pos, float radius)
        {
            GameObject obj = new GameObject("RangerSnare");
            obj.transform.position = pos;
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 32;
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            Color c = new Color(0.85f, 0.95f, 0.45f, 0.55f);
            line.startColor = c;
            line.endColor = c;
            Material m = VfxMaterial(Color.white);
            if (m != null) line.material = m;
            for (int i = 0; i < 32; i++)
            {
                float a = (float)i / 32f * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.06f, Mathf.Sin(a) * radius));
            }
            return obj;
        }

        private IEnumerator RingVfx(Vector3 center, float radius, Color color, float duration)
        {
            GameObject obj = new GameObject("RangerRingVfx");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 40;
            line.startWidth = 0.10f;
            line.endWidth = 0.10f;
            line.startColor = color;
            line.endColor = color;
            Material m = VfxMaterial(Color.white);
            if (m != null) line.material = m;
            for (int i = 0; i < 40; i++)
            {
                float a = (float)i / 40f * Mathf.PI * 2f;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0.08f, Mathf.Sin(a) * radius));
            }
            yield return new WaitForSeconds(Mathf.Max(0.05f, duration));
            Destroy(obj);
        }
    }
}
