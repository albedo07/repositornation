using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using DragonsAltarCombat;
using AlbedosCustomClassesSkills;

// v0.24.0 RANGER (Immortal Heroes Framework 4): Ranger Class (Wildborn, Piercing Arrow, Tumble Shot,
// Snare Trap) and the Acrobat Advancement (Windstep, Tailwind, 5 skills + Tempest Dance and every
// Ascended version). Bowmaster arrives in the next pass (its skills say so when cast).
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
        public const string ModVersion = "0.24.0";
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
        private ConfigEntry<float> _wbBows, _wbSneak, _wbFall;
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
        private ConfigEntry<float> _tdCooldown, _tdStamina, _tdDuration, _tdTargets, _tdRange, _tdInterval, _tdShot, _tdBurstRadius, _tdBurst, _tdAscDuration;
        private ConfigEntry<float> _twCooldown, _twDuration, _twRadius, _twMove, _twJump;

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
            _wbSneak = Config.Bind(wb, "SneakSkillBonus", 20f, "Wildborn: +Sneak skill.");
            _wbFall = Config.Bind(wb, "FallDamageReductionPercent", 30f, "Wildborn: less fall damage.");

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
            _tsDistance = Config.Bind(ts, "FlipDistance", 6f, "Backflip distance (m).");
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
            _gvLeap = Config.Bind(gv, "LeapDistance", 5f, "Backward leap (m).");
            _gvRange = Config.Bind(gv, "Range", 35f, "Arrow range (m).");
            _gvPush = Config.Bind(gv, "SmallKnockback", 25f, "Push force on Small enemies.");
            _gvAscDelay = Config.Bind("Acrobat Gale Volley Ascended", "SecondFanDelay", 0.25f, "A second fan arcs over the first.");

            const string cy = "Acrobat Cyclone Arrow";
            _cyCooldown = Config.Bind(cy, "Cooldown", 12f, "Seconds.");
            _cyStamina = Config.Bind(cy, "StaminaCost", 25f, "Stamina.");
            _cyDamage = Config.Bind(cy, "DamagePercent", 35f, "Per hit.");
            _cyRange = Config.Bind(cy, "Range", 30f, "Laser Projectile range (m).");
            _cyTravel = Config.Bind(cy, "TravelTime", 3f, "Seconds to cover the range.");
            _cyRadius = Config.Bind(cy, "Radius", 3f, "Pull / hit radius (m).");
            _cyInterval = Config.Bind(cy, "HitInterval", 0.3f, "Seconds.");
            _cyPull = Config.Bind(cy, "PullStrength", 6f, "Pull on Small enemies.");
            const string cya = "Acrobat Cyclone Arrow Ascended";
            _cyAscSplits = Config.Bind(cya, "Splits", 3f, "Smaller cyclones when it ends.");
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

            const string td = "Acrobat Tempest Dance";
            _tdCooldown = Config.Bind(td, "Cooldown", 120f, "Seconds.");
            _tdStamina = Config.Bind(td, "StaminaCost", 40f, "Stamina.");
            _tdDuration = Config.Bind(td, "Duration", 6f, "Seconds, Hyper Armor.");
            _tdTargets = Config.Bind(td, "MaxTargets", 10f, "Blinks.");
            _tdRange = Config.Bind(td, "Range", 25f, "m.");
            _tdInterval = Config.Bind(td, "BlinkInterval", 0.5f, "Seconds.");
            _tdShot = Config.Bind(td, "ShotPercent", 120f, "Each point-blank shot.");
            _tdBurstRadius = Config.Bind(td, "BurstRadius", 10f, "Final gale (m).");
            _tdBurst = Config.Bind(td, "BurstPercent", 150f, "Final gale, launches Small enemies.");
            _tdAscDuration = Config.Bind("Acrobat Tempest Dance Ascended", "Duration", 9f, "Seconds; the gale pulls everything in first.");

            const string tw = "Acrobat Tailwind";
            _twCooldown = Config.Bind(tw, "Cooldown", 600f, "Seconds (Grace, free).");
            _twDuration = Config.Bind(tw, "Duration", 120f, "Seconds.");
            _twRadius = Config.Bind(tw, "Radius", 10f, "Snapshot radius (m).");
            _twMove = Config.Bind(tw, "MoveSpeedPercent", 50f, "+Move Speed.");
            _twJump = Config.Bind(tw, "JumpSkillBonus", 30f, "+Jump skill. No fall damage.");

            DragonCombat.RegisterSkillModule(CastFromTree, CooldownForTree);
            DragonCombat.RegisterStackQuery(StackQuery);
            DragonCombat.RegisterSkillLevelBonus(SkillLevelBonus);
            DragonCombat.RegisterIncomingHitFilter(IncomingHit);
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
                case "tempest_dance": if (adv == "Acrobat") CastTempestDance(player); return adv == "Acrobat";
                case "tailwind": if (adv == "Acrobat") CastTailwind(player); return adv == "Acrobat";
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
                case "tempest_dance": return CooldownRemaining("Acrobat.TempestDance");
                case "tailwind": return CooldownRemaining("Acrobat.Tailwind");
            }
            return 0f;
        }

        private bool StackQuery(string id, out int ready, out int max, out float next)
        {
            ready = 0; max = 0; next = 0f;
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
                if (skill == "Sneak") bonus += Mathf.Max(0f, _wbSneak.Value);
            }
            if (skill == "Jump" && TailwindActive(player)) bonus += Mathf.Max(0f, _twJump.Value);
            return bonus;
        }

        // Fall damage: Wildborn -30%; none under Tailwind or after Skyfall Barrage until you land.
        private void IncomingHit(Character target, HitData hit)
        {
            Player player = target as Player;
            if (player == null || !IsFallHit(hit)) return;
            bool noFall;
            if (TailwindActive(player) || (_noFallUntilGrounded.TryGetValue(player.GetInstanceID(), out noFall) && noFall))
            {
                hit.m_damage.Modify(0f);
                return;
            }
            if (GetClass(player) == "Ranger")
                hit.m_damage.Modify(Mathf.Clamp01(1f - _wbFall.Value / 100f));
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

        private IEnumerator TumbleRoutine(Player player, bool ascended)
        {
            Vector3 start = player.transform.position;
            ShowMessage("Tumble Shot");
            Leap(player, -FlatAim(player), DragonCombat.M(_tsDistance.Value), 0.45f);
            if (ascended && _enableVfx.Value) StartCoroutine(RingVfx(start, DragonCombat.M(_tsAscGust.Value), new Color(0.55f, 1f, 0.90f, 0.9f), 1.0f));
            if (ascended)
            {
                List<Character> gust = GetSphereTargets(player, start, DragonCombat.M(_tsAscGust.Value));
                for (int i = 0; i < gust.Count; i++) DragonCombat.ApplyCripple(gust[i], _tsAscCripple.Value);
            }
            yield return new WaitForSeconds(0.2f);
            if (player == null || player.IsDead()) yield break;
            RangerArrowDamage d = ArrowDamage(player);
            float mult = _tsDamage.Value / 100f * DragonCombat.GetSkillPower(player, "tumble_shot");
            int count = Mathf.Max(1, Mathf.RoundToInt(ascended ? _tsAscArrows.Value : _tsArrows.Value));
            bool[] killed = new bool[1];
            Fan(player, count, _tsFan.Value, DragonCombat.M(_tsRange.Value), 0f, new Color(0.55f, 1f, 0.75f, 1f),
                delegate(Character enemy)
                {
                    Deal(player, enemy, d, mult, 6f, false);
                    if (ascended && enemy.IsDead() && !killed[0])
                    {
                        killed[0] = true;
                        _cooldowns.Remove("Ranger.TumbleShot");
                        ShowMessage("Tumble Shot - reset");
                    }
                    return false;
                });
            Shoot(player, null);
        }

        // ------------------------------------------------------------------ Snare Trap
        private void CastSnareTrap(Player player)
        {
            Vector3 point;
            if (!AlbedoAimUtility.TryGetPhysicalTarget(player, DragonCombat.M(_snRange.Value), out point)) { ShowMessage("Aim at the ground"); return; }
            if (!BeginSkill(player, "Ranger.SnareTrap", _snCooldown.Value, _snStamina.Value)) return;
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlaySkillPose(player, "Punch", 0.4f);
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
            Leap(player, -FlatAim(player), DragonCombat.M(_gvLeap.Value), 0.4f);
            yield return new WaitForSeconds(0.15f);
            int fans = ascended ? 2 : 1;
            for (int f = 0; f < fans; f++)
            {
                if (player == null || player.IsDead()) yield break;
                RangerArrowDamage d = ArrowDamage(player);
                float mult = _gvDamage.Value / 100f * DragonCombat.GetSkillPower(player, "gale_volley");
                Fan(player, Mathf.Max(1, Mathf.RoundToInt(_gvArrows.Value)), _gvFan.Value, DragonCombat.M(_gvRange.Value), f == 0 ? 0f : 8f,
                    new Color(0.55f, 1f, 0.90f, 1f),
                    delegate(Character enemy)
                    {
                        Deal(player, enemy, d, mult, DragonCombat.IsSmallEnemy(enemy) ? _gvPush.Value : 4f, false);
                        return false;
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
                }
                yield return null;
            }
            if (vfx != null) Destroy(vfx);
            if (split && player != null && !player.IsDead())
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(_cyAscSplits.Value));
                for (int i = 0; i < n; i++)
                {
                    float a = n == 1 ? 0f : -30f + 60f * i / (n - 1);
                    Vector3 sdir = Quaternion.AngleAxis(a, Vector3.up) * dir;
                    StartCoroutine(CycloneRoutine(player, pos, sdir, DragonCombat.M(_cyAscRange.Value), Mathf.Max(0.2f, _cyAscTravel.Value), DragonCombat.M(_cyAscRadius.Value), 0.6f, false));
                }
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
            DragonCombat.PlaySkillPose(player, "Crescent", 0.3f);
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
        }

        // ------------------------------------------------------------------ Ricochet Arrow
        private void CastRicochetArrow(Player player)
        {
            if (!RequireBow(player)) return;
            Character first = AimedEnemy(player, DragonCombat.M(_rcRange.Value));
            if (first == null) { ShowMessage("No target in sight"); return; }
            if (!BeginSkill(player, "Acrobat.RicochetArrow", _rcCooldown.Value, _rcStamina.Value)) return;
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

        // ------------------------------------------------------------------ Tempest Dance
        private void CastTempestDance(Player player)
        {
            if (!RequireBow(player)) return;
            if (!BeginSkill(player, "Acrobat.TempestDance", _tdCooldown.Value, _tdStamina.Value)) return;
            StartCoroutine(TempestRoutine(player, DragonCombat.IsSkillAscended(player, "tempest_dance")));
        }

        private IEnumerator TempestRoutine(Player player, bool ascended)
        {
            ShowMessage("TEMPEST DANCE");
            float duration = Mathf.Max(1f, ascended ? _tdAscDuration.Value : _tdDuration.Value);
            DragonCombat.GrantHyperArmor(player, duration + 0.5f);
            int id = player.GetInstanceID();
            _noFallUntilGrounded[id] = true;
            float end = Time.time + duration;
            int blinks = 0, max = Mathf.Max(1, Mathf.RoundToInt(_tdTargets.Value));
            RangerArrowDamage d = ArrowDamage(player);
            float mult = _tdShot.Value / 100f * DragonCombat.GetSkillPower(player, "tempest_dance");
            HashSet<int> visited = new HashSet<int>();
            Rigidbody body = player.GetComponent<Rigidbody>();
            while (Time.time < end && blinks < max && player != null && !player.IsDead())
            {
                Character target = NearestEnemy(player, DragonCombat.M(_tdRange.Value), visited);
                if (target == null) { visited.Clear(); target = NearestEnemy(player, DragonCombat.M(_tdRange.Value), visited); }
                if (target == null) break;
                visited.Add(target.GetInstanceID());
                Vector3 from = player.transform.position;
                Vector3 away = from - target.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = -target.transform.forward;
                Vector3 spot = target.transform.position + away.normalized * DragonCombat.M(2.5f);
                spot = GroundAt(spot);
                player.transform.position = spot;
                if (body != null) { body.position = spot; body.velocity = Vector3.zero; }
                FaceTowards(player, target.transform.position);
                Shoot(player, null);
                Deal(player, target, d, mult, 6f, false);
                if (_enableVfx.Value) LineVfx(from + Vector3.up, spot + Vector3.up, new Color(0.55f, 1f, 0.90f, 0.85f), 0.12f, 0.25f);
                blinks++;
                yield return new WaitForSeconds(Mathf.Max(0.1f, _tdInterval.Value));
            }
            if (player == null || player.IsDead()) yield break;
            Vector3 center = player.transform.position;
            float radius = DragonCombat.M(_tdBurstRadius.Value);
            if (ascended)
            {
                List<Character> pulled = GetSphereTargets(player, center, radius * 1.5f);
                for (int i = 0; i < pulled.Count; i++) PullToward(pulled[i], center, DragonCombat.IsSmallEnemy(pulled[i]) ? 14f : 6f);
                yield return new WaitForSeconds(0.35f);
            }
            List<Character> burst = GetSphereTargets(player, center, radius);
            float bmult = _tdBurst.Value / 100f * DragonCombat.GetSkillPower(player, "tempest_dance");
            for (int i = 0; i < burst.Count; i++)
            {
                Deal(player, burst[i], d, bmult, DragonCombat.IsSmallEnemy(burst[i]) ? 40f : 8f, false);
                if (DragonCombat.IsSmallEnemy(burst[i])) Launch(burst[i], 9f);
            }
            if (_enableVfx.Value) StartCoroutine(RingVfx(center, radius, new Color(0.55f, 1f, 0.90f, 1f), 0.8f));
            ShowMessage("Tempest Dance - gale");
        }

        // ------------------------------------------------------------------ Tailwind (Grace)
        private void CastTailwind(Player player)
        {
            if (CooldownRemaining("Acrobat.Tailwind") > 0f) { ShowCooldown("Acrobat.Tailwind"); return; }
            StartCooldown("Acrobat.Tailwind", _twCooldown.Value);
            DragonCombat.LockSkill(player, 0.5f);
            DragonCombat.PlaySkillPose(player, "Raise", 0.5f);
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

        private void Fan(Player player, int count, float fanDegrees, float range, float pitchUp, Color color, Func<Character, bool> onHit)
        {
            Vector3 origin = ShotOrigin(player);
            Vector3 dir = AimDir(player, origin);
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            if (right.sqrMagnitude < 0.01f) right = player.transform.right;
            for (int i = 0; i < count; i++)
            {
                float a = count == 1 ? 0f : -fanDegrees * 0.5f + fanDegrees * i / (count - 1);
                Vector3 shot = Quaternion.AngleAxis(a, Vector3.up) * dir;
                if (pitchUp != 0f) shot = Quaternion.AngleAxis(-pitchUp, right.normalized) * shot;
                StartCoroutine(ArrowFlight(player, origin, shot.normalized, DragonCombat.M(60f), range, DragonCombat.M(0.3f), false, color, 0f,
                    delegate(Character enemy) { bool keep = onHit(enemy); OnSkillHit(player); return keep; }, null));
            }
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

        // Skill damage = your Bow / Crossbow + the loaded arrow / bolt (no ammo: the bow alone).
        private RangerArrowDamage ArrowDamage(Player player)
        {
            RangerArrowDamage d = new RangerArrowDamage();
            AddItemDamage(d, GetCurrentWeapon(player));
            AddItemDamage(d, GetAmmo(player));
            if (d.Total() <= 0f) d.Pierce = 30f;
            return d;
        }

        private void AddItemDamage(RangerArrowDamage d, ItemDrop.ItemData item)
        {
            if (item == null || !IsRangedWeapon(item) && !IsAmmo(item)) return;
            try
            {
                MethodInfo m = item.GetType().GetMethod("GetDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                object dmg = m == null ? null : m.Invoke(item, null);
                if (dmg == null) return;
                d.Blunt += Field(dmg, "m_blunt"); d.Slash += Field(dmg, "m_slash"); d.Pierce += Field(dmg, "m_pierce");
                d.Fire += Field(dmg, "m_fire"); d.Frost += Field(dmg, "m_frost"); d.Lightning += Field(dmg, "m_lightning");
                d.Poison += Field(dmg, "m_poison"); d.Spirit += Field(dmg, "m_spirit");
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
            float m = Mathf.Max(0f, multiplier);
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
            return true;
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

        private bool IsGrounded(Player player)
        {
            try
            {
                MethodInfo m = typeof(Character).GetMethod("IsOnGround", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
