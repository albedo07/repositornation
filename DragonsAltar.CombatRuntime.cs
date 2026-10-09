using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DragonsAltarCombat
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("albedo.customclasses", BepInDependency.DependencyFlags.HardDependency)]
    public class DragonCombatPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "albedo.customclasses.combatruntime";
        public const string ModName = "Aethelborn Ascended - Combat Runtime";
        public const string ModVersion = "0.25.115";

        internal static DragonCombatPlugin Instance;

        internal ConfigEntry<float> DefaultDebuffDuration;
        internal ConfigEntry<float> ExposeDamageBonus;
        internal ConfigEntry<float> BrokenBonesDamageBonus;
        internal ConfigEntry<float> CrippleSlow;
        internal ConfigEntry<float> FrostMovementSlow;
        internal ConfigEntry<float> FrostAttackSpeedSlow;
        internal ConfigEntry<float> FrostPhysicalDamageBonus;
        internal ConfigEntry<float> ZapDelay;
        internal ConfigEntry<float> ZapRadius;
        internal ConfigEntry<float> CharacterHeightMeters;
        internal ConfigEntry<float> ClericMasteryMagicDamage;
        internal ConfigEntry<float> UnitsPerMeterOverride;
        internal ConfigEntry<float> ZapDamage;
        internal ConfigEntry<bool> BurnsUseCurrentHpPercent;
        internal ConfigEntry<float> BurnRampPercent, BurnRampMax;
        internal ConfigEntry<float> BulwarkArc, BulwarkRadius;
        internal ConfigEntry<float> FireBurnCurrentHpPercent;
        internal ConfigEntry<float> SpiritBurnMultiplier;
        internal ConfigEntry<float> MinimumBurnTick;
        internal ConfigEntry<float> WarriorHyperArmorThreshold;
        internal ConfigEntry<float> MercenaryHyperArmorThreshold;
        internal ConfigEntry<float> MasteryComboWindow;
        internal ConfigEntry<float> MasteryFinisherMultiplier;
        internal ConfigEntry<float> MasteryHeavyMultiplier;
        internal ConfigEntry<float> MasteryHeavyAttackSpeedBonus;
        internal ConfigEntry<float> MasteryStaminaMultiplier;
        internal ConfigEntry<bool> MasteryComboStyleEnabled;
        internal ConfigEntry<float> MasteryComboSpeedPerStage;
        internal ConfigEntry<bool> EnableSkillAnimations;
        internal ConfigEntry<float> LegMotionScale;
        internal ConfigEntry<string> VanillaAnimationMap;
        internal ConfigEntry<float> SkySummonDropTime;
        internal ConfigEntry<bool> EnableWarfreakDualWield;
        internal ConfigEntry<bool> EnableDivineStaffShield;
        internal ConfigEntry<bool> EnableSpellcasterDualGunStaves;
        internal ConfigEntry<bool> RemoveDivineEquipmentMovementPenalty;
        internal ConfigEntry<bool> ClericBlessingNoPenalty;
        internal ConfigEntry<bool> HolyTrinityNoArmorPenalty;
        internal ConfigEntry<float> SorcererMagicDamageBonus;
        internal ConfigEntry<bool> BlockHotbarWhenSkillModifierHeld;
        internal ConfigEntry<bool> EnableRuntime;

        private Harmony _harmony;

        internal void LogInfo(string message)
        {
            Logger.LogInfo(message);
        }

        private void Awake()
        {
            Instance = this;

            EnableRuntime = Config.Bind("Runtime", "Enabled", true, "Enable Dragon's Altar combat runtime patches.");
            EnableSkillAnimations = Config.Bind("Runtime", "EnableSkillAnimations", true, "Use Dragon's Altar procedural skill poses. Class skills do not trigger vanilla weapon attacks.");
            VanillaAnimationMap = Config.Bind("Runtime", "VanillaAnimationMap_v02593",
                "sm_slash_a=swing_longsword0@0.28;sm_slash_b=swing_longsword1@0.28;sm_halfmoon=battlea" + "xe_attack@0.45;sm_halfmoon_2=battleaxe_attack@0.45;warrior_h" +
                "eavy=battleaxe_attack@0.45;merc_heavy_asc=battleaxe_attack@0.45;sm_moon_finisher=battleaxe_attack@0.45;sm_crescent=swing_sledge@0.9;sm_crescent_asc=swing_sledge@0.9;sm_crescent_asc2=swing_longsword1@0.3;cleric_hammer_slam=axe_secondary@0.45;merc_bomb=battleaxe_attack@0.45;merc_circle" +
                "=atgeir_secondary@0.45;merc_circle_2=atgeir_secondary@0.4;sm_eclipse=atgeir_secondary@0.45;sm_halfmoon_finisher=atgeir_secondary@0.45;warrior_impact_wave=swing_sledge@0.55;merc_sei" +
                "smic=swing_sledge@0.55;wiz_greatblade=swing_sledge@0.55;wiz_greatblade_slam=swing_sledge@0.55;warrior_punch=unarmed_attack@0.2;sm_thrust=spear_poke@0.25;cleric_hammer=spear_throw@0" +
                ".4;cleric_cross_1=swing_longsword0@0.28;cleric_cross_2=swing_longsword1@0.28;cleric_zap=staff_fireball@0.25;sorc_flame=staff_fireball@0.25;hw_gravity_blast=staff_fireball@0.25;cler" +
                "ic_rs=swing_sledge@0.55;cleric_rs_asc=swing_sledge@0.55;cleric_goddess=swing_sledge@0.55;cleric_relic=swing_sledge@0.55;cleric_holy_relic=swing_sledge@0.55;cleric_judgement=swing_sledge" +
                "@0.55;cleric_tempest=swing_sledge@0.55;sorc_glacial=swing_sledge@0.55;sorc_glacial_asc=swing_sledge@0.55;sorc_stonefang=emote_kneel@0.3;cleric_wave=emote_cheer@0.3;cleric_ray=emote_c" +
                "heer@0.3;cleric_light=emote_cheer@0.3;cleric_intervention=emote_cheer@0.3;cleric_crucible=emote_cheer@0.3;cleric_wave_ally=emote_cheer@0.3;wiz_clockwork=emote_cheer@0.3;wiz_n" +
                "ova=staff_shield@0.3;sm_guidance=emote_cheer@0.3;rg_tailwind=emote_cheer@0.3;rg_vigil=emote_cheer@0.3;merc_roar=emote_roar@0.3;merc_fury_accent=emote_flex@0.2;rg_trap=emote_kneel@0.3;hw_point=emote_point@0.2;hw_command=emote_point@0.2;hw_rift_echo=emote_point@0.2;hw_rupture=emote_po" +
                "int@0.2;hw_open=emote_point@0.2;hw_stop=emote_point@0.2;hw_pinch=emote_point@0.2;hw_afterimage=emote_point@0.2;hw_rift_walker=emote_point@0.2;wiz_gravity=emote_comehere@0.3;sorc_stone" +
                "fang_asc=emote_point@0.2",
                "Skill clips that play Valheim's own animation (clip=trigger@seconds before impact). Remove an entry to use the custom pose instead. All animator trigger names of your game are written once to the BepInEx log ('[Immortal Heroes] Animator triggers').");
            LegMotionScale = Config.Bind("Runtime", "LegMotionScale_v02522", 0f, "Strength of the procedural leg poses (Unity humanoid muscles). 0 = legs untouched, -1 = inverted (if knees bend the wrong way on your rig).");
            SkySummonDropTime = Config.Bind("Skills", "SkySummonDropTime", 0.18f, "Seconds for a spawned Sky Summon object to slam from its indoor-safe spawn point to the target AFTER the character wind-up finishes.");
            EnableWarfreakDualWield = Config.Bind("Weapon Mastery", "EnableWarfreakDualWield", true, "Warfreak: Mercenary may equip any two one-handed weapons simultaneously. Dedicated combination animations are a later animation pass.");
            EnableDivineStaffShield = Config.Bind("Weapon Mastery", "EnableDivineStaffShield", true, "Divine Duality: Cleric may equip a Staff and Shield together, including before advancement.");
            EnableSpellcasterDualGunStaves = Config.Bind("Weapon Mastery", "EnableSpellcasterDualGunStaves", true, "Allow Spellcaster to equip two rapid-fire Gun Staves. The Sorcerer module handles cadence and accuracy without a synthetic off-hand projectile.");
            RemoveDivineEquipmentMovementPenalty = Config.Bind("Weapon Mastery", "LegacyRemoveDivineEquipmentMovementPenalty", false, "Legacy option retained for config compatibility. Cleric no longer gets a blanket equipment movement-penalty removal.");
            ClericBlessingNoPenalty = Config.Bind("Cleric Blessing", "NoShieldStaffClubMovementPenalty", true, "Cleric's Blessing: Shields, Staves and one-handed Club weapons have no movement penalty (two-handed Clubs excluded).");
            HolyTrinityNoArmorPenalty = Config.Bind("Paladin Holy Trinity", "NoArmorMovementPenalty", true, "Holy Trinity (Club-type melee weapon + any Shield): Armor has no movement penalty.");
            SorcererMagicDamageBonus = Config.Bind("Sorcerer Blessing", "MagicDamagePercent_v0230", 0f, "Retired Arcane Blood Magic Damage bonus (Warlock has none, v0.23.0). Kept for tuning; 0 = off.");
            BlockHotbarWhenSkillModifierHeld = Config.Bind("Hotkeys", "BlockHotbarWhenSkillModifierHeld", true, "Prevents Alpha1-Alpha0 from also activating Valheim hotbar slots while the Dragon's Altar skill modifier is held.");
            DefaultDebuffDuration = Config.Bind("Debuffs", "DefaultDuration", 6f, "Default debuff duration in seconds.");
            ExposeDamageBonus = Config.Bind("Debuffs", "ExposeDamageTakenPercent", 20f, "Extra damage taken while Exposed. Resistant damage types are also normalized to neutral.");
            BrokenBonesDamageBonus = Config.Bind("Debuffs", "BrokenBonesPhysicalDamageTakenPercent", 20f, "Extra Blunt, Slash and Pierce damage while Broken Bones is active. Resistant physical types are normalized to neutral.");
            CrippleSlow = Config.Bind("Debuffs", "CrippleMovementSlowPercent", 50f, "Movement speed reduction while Crippled.");
            FrostMovementSlow = Config.Bind("Debuffs", "FrostMovementSlowPercent", 30f, "Default movement slow while Frost is active.");
            FrostAttackSpeedSlow = Config.Bind("Debuffs", "FrostAttackSpeedSlowPercent", 30f, "Default attack-animation slow while Frost is active.");
            FrostPhysicalDamageBonus = Config.Bind("Debuffs", "FrostPhysicalDamageTakenPercent", 20f, "Extra physical damage taken while Frost weakens physical defense.");
            ZapDelay = Config.Bind("Debuffs", "ZapDelay_v0212", 2f, "Seconds before Zap explodes (universal rule: 2s).");
            ZapRadius = Config.Bind("Debuffs", "ZapRadius", 1f, "Zap explosion radius.");
            ClericMasteryMagicDamage = Config.Bind("Cleric Mastery", "MagicDamagePercent", 10f, "Heaven's Will (Paladin) and Bless Thy Sinners (Priest): +Magic Damage (Fire, Frost, Lightning, Poison, Spirit) on everything they deal.");
            CharacterHeightMeters = Config.Bind("Measurement", "CharacterHeightMeters", 0.5f, "v0.22.5 ruler (user rule): your character's height counts as this many meters. Every range, radius, width, length and travel speed in every config is in these meters.");
            UnitsPerMeterOverride = Config.Bind("Measurement", "UnitsPerMeterOverride", 1.5f, "Unity units per config meter. 1.5 = the confirmed in-game ruler (user, v0.23.3). 0 = automatic from character height / CharacterHeightMeters.");
            ZapDamage = Config.Bind("Debuffs", "ZapLightningDamage", 25f, "Testing/default lightning damage for Zap because the framework does not specify an amount.");
            BulwarkArc = Config.Bind("Paladin Holy Bulwark", "TowerBlockArcDegrees_v02578", 300f, "Paladin Mastery with a Tower Shield: hits coming from inside this arc (centred on your facing) are blockable. Vanilla blocks only the front half (180).");
            BulwarkRadius = Config.Bind("Paladin Holy Bulwark", "ForceFieldRadius_v02578", 2f, "Radius (m) of the holy force field shown in front of you while blocking with a Tower Shield.");
            BurnRampPercent = Config.Bind("Damage Over Time", "StackingBurnPercentPerTick_v02512", 20f, "Universal: every consecutive Fire Burn / Spirit Burn tick on the same target deals this much MORE than the previous one (percent of the base tick). Resets when the burn stops.");
            BurnRampMax = Config.Bind("Damage Over Time", "StackingBurnMaxMultiplier_v02512", 4f, "Cap for the stacking burn (x base tick damage).");
            BurnsUseCurrentHpPercent = Config.Bind("Damage Over Time", "LegacyBurnsUseCurrentHpPercent_v0212", false, "Legacy: burns now deal the skill's own burn damage. True = old 3% CURRENT HP burns.");
            FireBurnCurrentHpPercent = Config.Bind("Damage Over Time", "FireBurnCurrentHpPercentPerTick", 3f, "Fire Burn = 3 percent of CURRENT HP per tick.");
            SpiritBurnMultiplier = Config.Bind("Damage Over Time", "SpiritBurnMultiplierVsFire", 1.5f, "Spirit Burn remains 1.5x stronger than Fire.");
            MinimumBurnTick = Config.Bind("Damage Over Time", "MinimumBurnTickDamage", 1f, "Minimum percentage Burn tick.");
            WarriorHyperArmorThreshold = Config.Bind("Warrior Blessing", "HyperArmorHitThresholdPercent", 30f, "Warrior ignores stagger/pushback when an incoming hit is below this percent of max HP.");
            MercenaryHyperArmorThreshold = Config.Bind("Mercenary Mastery", "HyperArmorHitThresholdPercent", 60f, "Mercenary: Hyper Armor unless the FIRST hit of an enemy's attack is at least this percent of max HP (never accumulated).");
            MasteryComboWindow = Config.Bind("Weapon Mastery", "ComboWindow", 2f, "Combo progress survives interruption but resets after 2 seconds without another normal mastery attack.");

            if (Mathf.Approximately(MasteryComboWindow.Value, 1.5f))
                MasteryComboWindow.Value = 2f;
            MasteryFinisherMultiplier = Config.Bind("Weapon Mastery", "FifthHitDamageMultiplier", 1.5f, "Framework damage multiplier for the fifth normal mastery hit.");
            MasteryHeavyMultiplier = Config.Bind("Weapon Mastery", "HeavyDamageMultiplier", 1.5f, "Framework heavy attack damage multiplier.");
            MasteryHeavyAttackSpeedBonus = Config.Bind("Weapon Mastery", "HeavyAttackSpeedPercent", 10f, "Testing/default heavy attack speed bonus because the framework only says heavy attacks are a bit faster.");
            MasteryStaminaMultiplier = Config.Bind("Weapon Mastery", "StaminaCostMultiplier", 0.70f, "Framework mastery stamina multiplier. 0.70 = 30 percent reduction.");
            MasteryComboStyleEnabled = Config.Bind("Weapon Mastery", "EnableComboStyleSpeed", true, "Adds a small accelerating animation-speed style across mastery hits 1-5. This is presentation/tuning, not a framework-mandated damage bonus.");
            MasteryComboSpeedPerStage = Config.Bind("Weapon Mastery", "ComboStyleSpeedPerStagePercent", 3f, "Extra animation speed per combo stage after the first. 3 means hit 5 is about 12 percent faster than hit 1.");

            if (!EnableRuntime.Value)
            {
                Logger.LogInfo(ModName + " v" + ModVersion + " loaded with runtime disabled.");
                return;
            }

            try
            {
                _harmony = new Harmony(ModGuid);
                InstallPatches();
                Logger.LogInfo(ModName + " v" + ModVersion + " loaded. Cast lock, Hyper Armor, debuffs, attack-speed runtime and first-pass Weapon Mastery are enabled.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Combat Runtime patch setup failed. Other Dragon's Altar DLLs remain isolated. " + ex);
            }
        }

        private void Update()
        {
            DragonCombat.RuntimeUpdate();
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

        // v0.25.1: our buffs live on the Immortal HUD only; the vanilla status list (top right) keeps
        // showing vanilla / other mods' effects but never ours.
        private int PatchHudStatusList()
        {
            Type hud = Type.GetType("Hud, assembly_valheim");
            MethodInfo m = hud == null ? null : hud.GetMethod("UpdateStatusEffects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) return 0;
            PatchWithHarmony(m, new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("HudStatusPrefix", BindingFlags.Static | BindingFlags.NonPublic)), null);
            return 1;
        }

        // v0.25.4 (EpicMMO's method): while the Immortal HUD is on, Valheim's own health / stamina /
        // eitr / food HUD updates never run, so nothing re-enables the vanilla bars.
        private int PatchHudVitals()
        {
            Type hud = Type.GetType("Hud, assembly_valheim");
            if (hud == null) return 0;
            HarmonyMethod pre = new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("HudVitalsPrefix", BindingFlags.Static | BindingFlags.NonPublic));
            // UpdateFood keeps running: the vanilla food icons live inside the Immortal HUD.
            string[] names = { "UpdateHealth", "UpdateStamina", "UpdateEitr" };
            int n = 0;
            for (int i = 0; i < names.Length; i++)
            {
                MethodInfo m = hud.GetMethod(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null) continue;
                PatchWithHarmony(m, pre, null);
                n++;
            }
            return n;
        }

        // v0.25.11 VANILLA TOOLTIPS: every ItemDrop.ItemData.GetTooltip overload gets a postfix that runs the
        // registered filters (DragonCombat.RegisterTooltipFilter), so Immortal Heroes can show its real changes
        // on vanilla item tooltips. Built in: movement penalties removed by Blessings / Masteries / Graces.
        private int PatchItemTooltips()
        {
            Type itemType = typeof(ItemDrop.ItemData);
            HarmonyMethod post = new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("ItemTooltipPostfix", BindingFlags.Static | BindingFlags.NonPublic));
            int n = 0;
            MethodInfo[] ms = itemType.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i].Name != "GetTooltip" || ms[i].ReturnType != typeof(string)) continue;
                try { PatchWithHarmony(ms[i], null, post); n++; }
                catch (Exception ex) { Logger.LogWarning("Could not patch ItemData.GetTooltip: " + ex.Message); }
            }
            if (n == 0) Logger.LogWarning("ItemData.GetTooltip not found: vanilla tooltips stay unchanged.");
            return n;
        }

        private static void ItemTooltipPostfix(object __instance, object[] __args, ref string __result)
        {
            try
            {
                ItemDrop.ItemData item = null;
                if (__args != null) for (int i = 0; i < __args.Length && item == null; i++) item = __args[i] as ItemDrop.ItemData;
                if (item == null) item = __instance as ItemDrop.ItemData;
                if (item == null || string.IsNullOrEmpty(__result)) return;
                __result = DragonCombat.FilterItemTooltip(item, __result);
                __result = MovementTooltip(item, __result);
            }
            catch { }
        }

        private static readonly System.Text.RegularExpressions.Regex MoveLine =
            new System.Text.RegularExpressions.Regex(@"((?:\$item_movement_modifier|Movement speed):\s*<color=[^>]*>)([+\-]?\d+)(%</color>)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Shows the movement penalty this player really gets from the item (0% when a trait removes it).
        private static string MovementTooltip(ItemDrop.ItemData item, string text)
        {
            Player p = Player.m_localPlayer;
            if (p == null || item.m_shared == null || item.m_shared.m_movementModifier >= 0f) return text;
            string reason = MovementExemptReason(p, item);
            if (reason == null) return text;
            return MoveLine.Replace(text, delegate(System.Text.RegularExpressions.Match m)
            {
                return m.Groups[1].Value + "0" + m.Groups[3].Value + " <color=#80D8FF>(" + m.Groups[2].Value + "% removed: " + reason + ")</color>";
            }, 1);
        }

        private static bool IsArmorPiece(ItemDrop.ItemData item)
        {
            string t = item.m_shared.m_itemType.ToString();
            return t == "Chest" || t == "Legs" || t == "Helmet" || t == "Shoulder";
        }

        private static string MovementExemptReason(Player p, ItemDrop.ItemData item)
        {
            if (DragonCombat.HasNoEquipmentPenalty(p)) return DragonCombat.GetClassName(p) == "Ranger" ? "Tailwind" : "Heaven's Light";
            string adv = DragonCombat.GetAdvancementName(p);
            string cls = DragonCombat.GetClassName(p);
            if ((adv == "Mercenary" || adv == "Sword Master") && WeaponMasteryExemptPenalty(item, adv) < 0f)
                return adv == "Mercenary" ? "Warfreak" : "The Way of the Sword";
            if (cls == "Ranger" && RangedExemptPenalty(item, adv == "Bowmaster") < 0f) return "Wildborn";
            if (cls == "Sorcerer" && ClassFitExemptPenalty(item, cls) < 0f)
                return adv == "Spellcaster" ? "Yin and Yang" : (adv == "Wizard" ? "Archmage" : "Warlock");
            if (cls == "Warrior" && adv != "Mercenary" && adv != "Sword Master" && ClassFitExemptPenalty(item, cls) < 0f) return "Warrior's Blessing";
            if (cls == "Cleric" && Instance != null)
            {
                if (Instance.ClericBlessingNoPenalty.Value && ClericExemptPenalty(item) < 0f) return "Cleric's Blessing";
                if (Instance.HolyTrinityNoArmorPenalty.Value && adv == "Paladin" && IsArmorPiece(item) && DragonCombat.IsHolyTrinityActive(p))
                    return "Heaven's Will";
            }
            return null;
        }

        // v0.25.15: forced run locomotion (DragonCombat.ForceRun) overrides the movement floats Valheim
        // sends to the local player's animator while a skill moves the body itself.
        private int PatchForceRun()
        {
            Type z = Type.GetType("ZSyncAnimation, assembly_valheim");
            if (z == null) return 0;
            HarmonyMethod pre = new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("ZanimFloatPrefix", BindingFlags.Static | BindingFlags.NonPublic));
            HarmonyMethod preBool = new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("ZanimBoolPrefix", BindingFlags.Static | BindingFlags.NonPublic));
            int n = 0;
            MethodInfo[] ms = z.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i].Name != "SetFloat" && ms[i].Name != "SetBool") continue;
                ParameterInfo[] ps = ms[i].GetParameters();
                if (ps.Length != 2) continue;
                if (ms[i].Name == "SetFloat" && ps[1].ParameterType != typeof(float)) continue;
                if (ms[i].Name == "SetBool" && ps[1].ParameterType != typeof(bool)) continue;
                try { PatchWithHarmony(ms[i], ms[i].Name == "SetBool" ? preBool : pre, null); n++; }
                catch (Exception ex) { Logger.LogWarning("Could not patch ZSyncAnimation.SetFloat: " + ex.Message); }
            }
            return n;
        }

        private static void ZanimBoolPrefix(object __instance, object[] __args)
        {
            try
            {
                if (__args == null || __args.Length < 2) return;
                bool v = (bool)__args[1];
                if (DragonCombat.ForceBoolOverride(__instance, __args[0], ref v)) __args[1] = v;
            }
            catch { }
        }

        private static void ZanimFloatPrefix(object __instance, object[] __args)
        {
            try
            {
                if (__args == null || __args.Length < 2) return;
                float v = (float)__args[1];
                if (DragonCombat.ForceRunOverride(__instance, __args[0], ref v)) __args[1] = v;
            }
            catch { }
        }

        private static bool HudVitalsPrefix()
        {
            return !DragonCombat.VanillaVitalsHidden;
        }

        private static void HudStatusPrefix(List<StatusEffect> __0)
        {
            try { if (__0 != null) __0.RemoveAll(delegate(StatusEffect se) { return se is IhStatusDisplay; }); }
            catch { }
        }

        private void PatchWithHarmony(MethodBase original, HarmonyMethod prefix, HarmonyMethod postfix)
        {
            if (_harmony == null || original == null)
                return;

            MethodInfo[] methods = typeof(Harmony).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "Patch")
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 1 || !typeof(MethodBase).IsAssignableFrom(parameters[0].ParameterType))
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

        private void InstallPatches()
        {
            int count = 0;
            count += PatchSetControls();
            count += PatchHudStatusList();
            count += PatchHudVitals();
            count += PatchForceRun();
            count += PatchItemTooltips();
            count += PatchCheckRun();
            count += PatchInterruptMethod("Stagger");
            count += PatchInterruptMethod("AddStaggerDamage");
            count += PatchInterruptMethod("ApplyPushback");
            count += PatchDamageMethods();
            count += PatchAnimationSpeed();
            count += PatchFloatRefSEMan("ModifySpeed", "SpeedPrefix");
            count += PatchFloatRefSEMan("ModifyStaminaRegen", "StaminaRegenPrefix");
            count += PatchFloatRefSEMan("ModifyEitrRegen", "EitrRegenPrefix");
            count += PatchAttackGetStamina();
            count += PatchAttackStart();
            count += PatchComboChains();
            count += PatchEquipmentMovement();
            count += PatchUseStamina();
            count += PatchBlockAttack();
            foreach (MethodInfo rm in typeof(Character).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (rm.Name != "AddRootMotion") continue;
                try { PatchWithHarmony(rm, new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("RootMotionPrefix", BindingFlags.Static | BindingFlags.NonPublic)), null); count++; }
                catch (Exception ex) { Logger.LogWarning("Root motion patch: " + ex.Message); }
            }
            // v0.25.66: Hyper Armor evaluated on the receiving client (servers run the attacker's Damage elsewhere).
            foreach (MethodInfo rd in typeof(Character).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (rd.Name != "RPC_Damage") continue;
                try { PatchWithHarmony(rd, new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("RpcDamagePrefix", BindingFlags.Static | BindingFlags.NonPublic)), null); count++; }
                catch (Exception ex) { Logger.LogWarning("RPC_Damage patch: " + ex.Message); }
            }
            count += PatchEquipItem();
            count += PatchHotbarUse();
            // v0.25.78 Ray of Hope: debuff immunity blocks new debuff status effects.
            Type semanType = DragonCombat.FindTypeCached("SEMan");
            if (semanType != null)
                foreach (MethodInfo am in semanType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (am.Name != "AddStatusEffect") continue;
                    try { PatchWithHarmony(am, new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("SeAddPrefix", BindingFlags.Static | BindingFlags.NonPublic)), null); count++; }
                    catch (Exception ex) { Logger.LogWarning("AddStatusEffect patch: " + ex.Message); }
                }

            Logger.LogInfo("Combat Runtime hooks installed: " + count);
        }

        private int PatchSetControls()
        {
            MethodInfo patch = typeof(DragonCombatPlugin).GetMethod("SetControlsPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            if (patch == null)
                return 0;

            MethodInfo[] methods = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = 0;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "SetControls")
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 12 || parameters[0].ParameterType != typeof(Vector3))
                    continue;

                try
                {
                    PatchWithHarmony(method, new HarmonyMethod(patch), null);
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not patch Player.SetControls: " + ex.Message);
                }
            }

            return count;
        }

        private int PatchCheckRun()
        {
            MethodInfo patch = typeof(DragonCombatPlugin).GetMethod("CheckRunPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            if (patch == null)
                return 0;

            MethodInfo[] methods = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "CheckRun" || method.ReturnType != typeof(bool))
                    continue;
                try
                {
                    PatchWithHarmony(method, null, new HarmonyMethod(patch));
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not patch Player.CheckRun: " + ex.Message);
                }
            }
            return count;
        }

        private int PatchInterruptMethod(string methodName)
        {
            MethodInfo patch = typeof(DragonCombatPlugin).GetMethod("InterruptPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            if (patch == null)
                return 0;

            MethodInfo[] methods = typeof(Character).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = 0;

            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != methodName)
                    continue;

                try
                {
                    PatchWithHarmony(methods[i], new HarmonyMethod(patch), null);
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not patch Character." + methodName + ": " + ex.Message);
                }
            }

            return count;
        }

        private int PatchDamageMethods()
        {
            MethodInfo prefixMethod = typeof(DragonCombatPlugin).GetMethod("DamagePrefix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo postfixMethod = typeof(DragonCombatPlugin).GetMethod("DamagePostfix", BindingFlags.Static | BindingFlags.NonPublic);
            if (prefixMethod == null || postfixMethod == null)
                return 0;

            HashSet<MethodBase> seen = new HashSet<MethodBase>();
            Type[] types = new Type[] { typeof(Character), typeof(Player) };
            int count = 0;

            for (int t = 0; t < types.Length; t++)
            {
                MethodInfo[] methods = types[t].GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                        // v0.25.11: run after every module's Damage prefix (damage split happens here).
                        HarmonyMethod damagePrefix = new HarmonyMethod(prefixMethod);
                        damagePrefix.priority = Priority.Last;
                        PatchWithHarmony(method, damagePrefix, new HarmonyMethod(postfixMethod));
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Could not patch Character.Damage: " + ex.Message);
                    }
                }
            }

            return count;
        }

        private int PatchAnimationSpeed()
        {
            MethodInfo postfix = typeof(DragonCombatPlugin).GetMethod(
                "AnimationSpeedPostfix",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            if (postfix == null)
                return 0;

            MethodInfo method = typeof(CharacterAnimEvent).GetMethod(
                "CustomFixedUpdate",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (method == null)
                return 0;

            try
            {
                PatchWithHarmony(method, null, new HarmonyMethod(postfix));
                return 1;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not patch CharacterAnimEvent.CustomFixedUpdate: " + ex.Message);
                return 0;
            }
        }

        private int PatchFloatRefSEMan(string methodName, string patchName)
        {
            Type seman = AccessTools.TypeByName("SEMan");
            MethodInfo patch = typeof(DragonCombatPlugin).GetMethod(patchName, BindingFlags.Static | BindingFlags.NonPublic);
            if (seman == null || patch == null)
                return 0;

            MethodInfo[] methods = seman.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Type floatRef = typeof(float).MakeByRefType();
            int count = 0;

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
                    PatchWithHarmony(method, new HarmonyMethod(patch), null);
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not patch SEMan." + methodName + ": " + ex.Message);
                }
            }

            return count;
        }

        // v0.20.8: Bless Thy Sinners "-70% Stamina Usage for all actions" (Player.UseStamina amount).
        private int PatchUseStamina()
        {
            MethodInfo prefix = typeof(DragonCombatPlugin).GetMethod("UseStaminaPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            if (prefix == null)
                return 0;
            int count = 0;
            MethodInfo[] methods = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "UseStamina" || parameters.Length < 1 || parameters[0].ParameterType != typeof(float))
                    continue;
                try
                {
                    PatchWithHarmony(method, new HarmonyMethod(prefix), null);
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not patch Player.UseStamina: " + ex.Message);
                }
            }
            return count;
        }

        private static void UseStaminaPrefix(Player __instance, ref float __0)
        {
            if (__0 > 0f)
                __0 *= DragonCombat.GetStaminaUseMultiplier(__instance);
        }

        private int PatchEquipmentMovement()
        {
            MethodInfo postfix = typeof(DragonCombatPlugin).GetMethod("EquipmentMovementPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            if (postfix == null)
                return 0;

            MethodInfo[] methods = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = 0;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "GetEquipmentMovementModifier" || method.ReturnType != typeof(float) || method.GetParameters().Length != 0)
                    continue;

                try
                {
                    PatchWithHarmony(method, null, new HarmonyMethod(postfix));
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not patch Player.GetEquipmentMovementModifier: " + ex.Message);
                }
            }

            return count;
        }

        private int PatchBlockAttack()
        {
            MethodInfo prefix = typeof(DragonCombatPlugin).GetMethod("BlockAttackPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo postfix = typeof(DragonCombatPlugin).GetMethod("BlockAttackPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo restoreOnly = typeof(DragonCombatPlugin).GetMethod("BlockAttackRestorePostfix", BindingFlags.Static | BindingFlags.NonPublic);
            HashSet<MethodBase> seen = new HashSet<MethodBase>();
            int count = 0;
            Type[] types = new Type[] { typeof(Player), typeof(Humanoid) };
            for (int t = 0; t < types.Length; t++)
                foreach (MethodInfo method in types[t].GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.Name != "BlockAttack" || !seen.Add(method)) continue;
                    try
                    {
                        // The parry hook reads the bool result; any other overload only restores the shield.
                        MethodInfo post = method.ReturnType == typeof(bool) ? postfix : restoreOnly;
                        PatchWithHarmony(method, new HarmonyMethod(prefix), new HarmonyMethod(post));
                        count++;
                    }
                    catch (Exception ex) { Logger.LogWarning("Could not patch BlockAttack: " + ex.Message); }
                }
            return count;
        }

        private static void BlockAttackPrefix(object __instance, object[] __args, ref BlockPatchState __state)
        {
            __state = new BlockPatchState();
            try
            {
                Player player = __instance as Player;
                if (player == null || player != Player.m_localPlayer) return;
                // v0.25.63 (user): Hyper Armor covers blocking and parrying too - Warriors (whose Blessing IS Hyper
                // Armor) and anyone under an active Hyper Armor take no pushback / slide from a blocked hit.
                __state.HyperBlock = DragonCombat.GetClassName(player) == "Warrior" || DragonCombat.HasHyperArmor(player);
                if (__state.HyperBlock) ZeroPush(__args);
                ItemDrop.ItemData blocker = DragonCombat.GetHandItem(player, "m_leftItem");
                if (!DragonCombat.IsShield(blocker)) blocker = player.GetCurrentWeapon();
                if (blocker == null || blocker.m_shared == null) return;

                __state.Shared = blocker.m_shared;
                // Valheim applies m_timedBlockBonus only to a successful timed parry.
                if (DragonCombat.GetClassName(player) == "Warrior")
                    __state.ParryField = SetTemporaryBlockMultiplier(blocker.m_shared, "m_timedBlockBonus", 2f, out __state.OriginalParry);
                // v0.20.9 Bless Thy Sinners: a Priest holding a Buckler has doubled Parry strength
                // (Block Force 1.5x comes from Cleric's Blessing below, same as every Cleric shield).
                if (DragonCombat.GetAdvancementName(player) == "Priest" && DragonCombat.IsBuckler(blocker))
                {
                    __state.ParryField = SetTemporaryBlockMultiplier(blocker.m_shared, "m_timedBlockBonus", 2f, out __state.OriginalParry);
                    __state.BucklerParryWindow = DragonCombat.IsInParryWindow(player);
                }
                // v0.25.78 Holy Bulwark (Paladin Mastery + Tower Shield): the block covers 3x the area.
                if (DragonCombat.GetAdvancementName(player) == "Paladin" && DragonCombat.IsTowerShield(blocker))
                    DragonCombat.HolyBulwarkWiden(player, __args);
                if (DragonCombat.GetClassName(player) == "Cleric" && DragonCombat.IsShield(blocker))
                {
                    // Shield Weapon Mastery: every shield gets 1.5x Block Force AND Block Power.
                    __state.ForceField = SetTemporaryBlockMultiplier(blocker.m_shared, "m_blockForce", 1.5f, out __state.OriginalForce);
                    __state.PowerField = SetTemporaryBlockMultiplier(blocker.m_shared, "m_blockPower", 1.5f, out __state.OriginalPower);
                }
            }
            catch (Exception ex)
            {
                RestoreBlockState(__state);
                __state = new BlockPatchState();
                if (Instance != null) Instance.Logger.LogWarning("Block blessing skipped: " + ex.Message);
            }
        }

        private static FieldInfo SetTemporaryBlockMultiplier(object shared, string name, float multiplier, out float original)
        {
            original = 0f;
            FieldInfo field = shared.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(float)) return null;
            original = (float)field.GetValue(shared);
            field.SetValue(shared, original * multiplier);
            return field;
        }

        private static void ZeroPush(object[] args)
        {
            if (args == null) return;
            for (int i = 0; i < args.Length; i++) { HitData h = args[i] as HitData; if (h != null) h.m_pushForce = 0f; }
        }

        private static void BlockAttackPostfix(BlockPatchState __state, bool __result, object __instance, object[] __args)
        {
            RestoreBlockState(__state);
            if (__result && __state.HyperBlock)
            {
                ZeroPush(__args);
                DragonCombat.BlockHyperUntil = Time.time + 0.8f;
            }
            // A successful block that started inside the parry window = a Buckler Parry.
            if (__result && __state.BucklerParryWindow && DragonCombat.BucklerParryHandler != null)
            {
                try { DragonCombat.BucklerParryHandler(__instance as Player); }
                catch (Exception ex) { if (Instance != null) Instance.Logger.LogWarning("Buckler Parry failed: " + ex.Message); }
            }
        }

        private static void BlockAttackRestorePostfix(BlockPatchState __state)
        {
            RestoreBlockState(__state);
        }

        private static void RestoreBlockState(BlockPatchState __state)
        {
            if (__state.Shared == null) return;
            try
            {
                if (__state.ParryField != null) __state.ParryField.SetValue(__state.Shared, __state.OriginalParry);
                if (__state.ForceField != null) __state.ForceField.SetValue(__state.Shared, __state.OriginalForce);
                if (__state.PowerField != null) __state.PowerField.SetValue(__state.Shared, __state.OriginalPower);
            }
            catch (Exception ex)
            {
                if (Instance != null) Instance.Logger.LogWarning("Block blessing restore failed: " + ex.Message);
            }
        }

        private int PatchEquipItem()
        {
            MethodInfo prefix = typeof(DragonCombatPlugin).GetMethod("EquipItemPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo postfix = typeof(DragonCombatPlugin).GetMethod("EquipItemPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            if (prefix == null || postfix == null)
                return 0;

            MethodInfo[] methods = typeof(Humanoid).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = 0;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "EquipItem" || method.ReturnType != typeof(bool))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 1 || parameters[0].ParameterType != typeof(ItemDrop.ItemData))
                    continue;

                try
                {
                    PatchWithHarmony(method, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not patch Humanoid.EquipItem: " + ex.Message);
                }
            }

            return count;
        }

        private int PatchHotbarUse()
        {
            MethodInfo prefix = typeof(DragonCombatPlugin).GetMethod(
                "HotbarUsePrefix",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            if (prefix == null)
                return 0;

            HashSet<MethodBase> seen = new HashSet<MethodBase>();
            Type[] types = new Type[] { typeof(Player), typeof(Humanoid) };
            int count = 0;

            for (int t = 0; t < types.Length; t++)
            {
                MethodInfo[] methods = types[t].GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != "UseHotbarItem" || method.IsStatic)
                        continue;

                    if (!seen.Add(method))
                        continue;

                    try
                    {
                        PatchWithHarmony(method, new HarmonyMethod(prefix), null);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Could not patch hotbar use method: " + ex.Message);
                    }
                }
            }

            return count;
        }

        private int PatchAttackGetStamina()
        {
            MethodInfo patch = typeof(DragonCombatPlugin).GetMethod("AttackStaminaPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            if (patch == null)
                return 0;

            MethodInfo[] methods = typeof(Attack).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != "GetAttackStamina" || methods[i].ReturnType != typeof(float))
                    continue;
                try
                {
                    PatchWithHarmony(methods[i], null, new HarmonyMethod(patch));
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not patch Attack.GetAttackStamina: " + ex.Message);
                }
            }
            return count;
        }

        // ==================================================================================
        // v0.25.38 COMBO CHAINS (user): every chained melee weapon gets a 5-hit chain built from its own vanilla
        // swings (a 3-swing weapon plays 0,1,0,1 then its finisher 2; the finisher keeps Valheim's last-hit
        // damage bonus). Dual wield (Mercenary, two one-handed weapons): the off-hand weapon strikes on every
        // swing (mirrored sweep, like the DualWield mod) and the chain uses Valheim's dual-knife swings when the
        // game has them. After the 5th hit normal attacks are locked for 1 s.
        // ==================================================================================
        internal ConfigEntry<bool> ComboChainsEnabled;
        internal ConfigEntry<float> WhirlwindLoopStart, WhirlwindLoopEnd;
        internal ConfigEntry<int> ComboChainLength;
        internal ConfigEntry<float> ComboFinisherLockout, ComboContinueWindow, ComboSwingSeconds, ComboRepeatOffset, ComboBlend;
        internal ConfigEntry<bool> MeleeHitStop, ComboFixedInterval;
        internal ConfigEntry<bool> EnhancedVfx;
        internal ConfigEntry<float> VfxDensity, VfxLight;
        private static FieldInfo _atkLevels, _atkLevel, _atkAnim, _atkChar, _atkWeapon, _atkAngle, _atkType;
        private static MethodInfo _atkMelee;
        private static bool _comboInStart, _comboDual;
        private static string _comboBase;
        private static object _comboFinisher;
        private static float _comboLockUntil;
        private static bool _offhandSwing;
        private static FieldInfo _atkHitPoint;

        private static void ComboFields()
        {
            if (_atkLevels != null) return;
            Type t = typeof(Attack);
            BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _atkLevels = t.GetField("m_attackChainLevels", f);
            _atkLevel = t.GetField("m_currentAttackCainLevel", f);
            _atkAnim = t.GetField("m_attackAnimation", f);
            _atkChar = t.GetField("m_character", f);
            _atkWeapon = t.GetField("m_weapon", f);
            _atkAngle = t.GetField("m_attackAngle", f);
            _atkType = t.GetField("m_attackType", f);
            _atkMelee = t.GetMethod("DoMeleeAttack", f, null, Type.EmptyTypes, null);
        }

        private int PatchComboChains()
        {
            ComboChainsEnabled = Config.Bind("Combat", "FiveHitCombos_v02538", true, "Normal melee attacks chain into a 5-hit combo built from the weapon's own vanilla swings.");
            ComboChainLength = Config.Bind("Combat", "ComboLength_v02538", 5, "Hits in the normal attack chain.");
            ComboFinisherLockout = Config.Bind("Combat", "ComboFinisherLockout_v02538", 1f, "Seconds after the last hit of the chain before a new normal attack can start.");
            EnhancedVfx = Config.Bind("Visuals", "EnhancedSkillVfx_v02554", true, "Real particle / light / lightning effects on skills (v0.25.54). Off = the old line drawings only.");
            VfxDensity = Config.Bind("Visuals", "ParticleDensity_v02559", 0.7f, "Particle amount multiplier for skill effects (0.1 - 3).");
            VfxLight = Config.Bind("Visuals", "LightIntensity_v02559", 0.45f, "Brightness of the flashes / point lights skills create (0 = none, 1 = the old v0.25.54-58 look).");
            ComboSwingSeconds = Config.Bind("Combat", "ComboSwingSeconds_v02559", 0.6f, "Normal attack chain: seconds per swing for ALL 5 hits (finisher included) with no attack-speed bonus; class attack speed bonuses shorten it.");
            ComboRepeatOffset = Config.Bind("Combat", "ComboRepeatOffset_v02559", 0.22f, "When the chain steps back to an earlier swing (hit 3 = swing 1 again), it starts this far into that swing (0-0.5 of the animation) so it flows out of the previous swing instead of restarting from the rest pose.");
            ComboBlend = Config.Bind("Combat", "ComboBlendSeconds_v02559", 0.12f, "Cross-fade time between chained swings.");
            ComboFixedInterval = Config.Bind("Combat", "ComboFixedInterval_v02565", false, "On = every weapon's chain uses ComboSwingSeconds per hit (all weapons equally fast). Off = each weapon keeps its own speed (its average swing), with even intervals inside the chain; attack speed bonuses multiply it.");
            MeleeHitStop = Config.Bind("Combat", "MeleeHitStop_v02559", false, "Valheim's hit-stop (the swing freezes 0.15 s on every hit) for YOUR melee hits. Off = swings flow through enemies without stopping.");
            ComboContinueWindow = Config.Bind("Combat", "ComboContinueWindow_v02544", 0.4f, "Seconds after a swing ends in which the next normal attack continues the chain (1-2-1-2-3) instead of starting over.");
            WhirlwindLoopStart = Config.Bind("Runtime", "WhirlwindLoopStart_v02542", 0.3f, "Whirlwind: where the looped spin restarts in Valheim's atgeir spin (0-1 of the animation).");
            WhirlwindLoopEnd = Config.Bind("Runtime", "WhirlwindLoopEnd_v02542", 0.72f, "Whirlwind: where the looped spin jumps back (0-1 of the animation).");
            ComboFields();
            int n = 0;
            BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo[] am = typeof(Attack).GetMethods(all);
            for (int i = 0; i < am.Length; i++)
            {
                try
                {
                    if (am[i].Name == "Start" && am[i].ReturnType == typeof(bool))
                    { PatchWithHarmony(am[i], new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("ComboStartPrefix", BindingFlags.Static | BindingFlags.NonPublic)), new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("ComboStartPostfix", BindingFlags.Static | BindingFlags.NonPublic))); n++; }
                    else if (am[i].Name == "Stop" && am[i].GetParameters().Length == 0)
                    { PatchWithHarmony(am[i], null, new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("ComboStopPostfix", BindingFlags.Static | BindingFlags.NonPublic))); n++; }
                    else if (am[i].Name == "OnAttackTrigger" && am[i].GetParameters().Length == 0)
                    { PatchWithHarmony(am[i], new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("OffhandTriggerPrefix", BindingFlags.Static | BindingFlags.NonPublic)), null); n++; }
                }
                catch (Exception ex) { Logger.LogWarning("Combo chain patch " + am[i].Name + ": " + ex.Message); }
            }
            MethodInfo[] hm = typeof(Humanoid).GetMethods(all);
            for (int i = 0; i < hm.Length; i++)
            {
                if (hm[i].Name == "OnAttackTrigger" && hm[i].GetParameters().Length == 0)
                {
                    try { PatchWithHarmony(hm[i], new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("SkillAnimTriggerPrefix", BindingFlags.Static | BindingFlags.NonPublic)), null); n++; }
                    catch (Exception ex) { Logger.LogWarning("Skill anim trigger patch: " + ex.Message); }
                    continue;
                }
                if (hm[i].Name != "StartAttack" || hm[i].ReturnType != typeof(bool)) continue;
                try { PatchWithHarmony(hm[i], new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("ComboLockPrefix", BindingFlags.Static | BindingFlags.NonPublic)), null); n++; }
                catch (Exception ex) { Logger.LogWarning("Combo lock patch: " + ex.Message); }
            }
            // v0.25.59: no hit-stop for the local player's hits (Character.FreezeFrame / old CharacterAnimEvent.FreezeFrame).
            Type[] ffTypes = { typeof(Character), Type.GetType("CharacterAnimEvent, assembly_valheim") };
            for (int ti = 0; ti < ffTypes.Length; ti++)
            {
                if (ffTypes[ti] == null) continue;
                MethodInfo ff = ffTypes[ti].GetMethod("FreezeFrame", all, null, new Type[] { typeof(float) }, null);
                if (ff == null) continue;
                try { PatchWithHarmony(ff, new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("FreezeFramePrefix", BindingFlags.Static | BindingFlags.NonPublic)), null); n++; }
                catch (Exception ex) { Logger.LogWarning("Hit-stop patch: " + ex.Message); }
            }
            Type visEq = Type.GetType("VisEquipment, assembly_valheim");
            MethodInfo trails = visEq == null ? null : visEq.GetMethod("SetWeaponTrails", all, null, new Type[] { typeof(bool) }, null);
            if (trails != null)
            {
                try { PatchWithHarmony(trails, null, new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("OffhandTrailsPostfix", BindingFlags.Static | BindingFlags.NonPublic))); n++; }
                catch (Exception ex) { Logger.LogWarning("Off-hand trail patch: " + ex.Message); }
            }
            Type z = Type.GetType("ZSyncAnimation, assembly_valheim");
            MethodInfo st = z == null ? null : z.GetMethod("SetTrigger", all, null, new Type[] { typeof(string) }, null);
            if (st != null)
            {
                try { PatchWithHarmony(st, new HarmonyMethod(typeof(DragonCombatPlugin).GetMethod("ComboTriggerPrefix", BindingFlags.Static | BindingFlags.NonPublic)), null); n++; }
                catch (Exception ex) { Logger.LogWarning("Combo trigger patch: " + ex.Message); }
            }
            return n;
        }

        private static bool IsLocalMeleeAttack(object attack, out Player player)
        {
            player = null;
            if (attack == null || _atkChar == null) return false;
            player = _atkChar.GetValue(attack) as Player;
            if (player == null || player != Player.m_localPlayer) return false;
            object type = _atkType == null ? null : _atkType.GetValue(attack);
            string ts = type == null ? "" : type.ToString();
            return ts == "Horizontal" || ts == "Vertical";
        }

        // v0.25.48 ROOT CAUSE of the chain never working: Attack.Start is called on a fresh clone whose
        // m_character / m_weapon are only filled INSIDE Start, so the old prefix always saw null and bailed.
        // The prefix now reads Start's own `character` / `weapon` arguments.
        private static bool IsLocalMeleeStart(Attack attack, Humanoid character, out Player player)
        {
            player = character as Player;
            if (attack == null || player == null || player != Player.m_localPlayer) return false;
            object type = _atkType == null ? null : _atkType.GetValue(attack);
            string ts = type == null ? "" : type.ToString();
            return ts == "Horizontal" || ts == "Vertical";
        }

        private static bool IsDualWielding(Player p)
        {
            // v0.25.48: dual wield belongs to the Mercenary only.
            if (p == null || DragonCombat.GetAdvancementName(p) != "Mercenary") return false;
            ItemDrop.ItemData l = DragonCombat.GetHandItem(p, "m_leftItem");
            ItemDrop.ItemData r = DragonCombat.GetHandItem(p, "m_rightItem");
            return l != null && r != null && l != r && DragonCombat.IsOneHandedWeapon(l) && DragonCombat.IsOneHandedWeapon(r);
        }

        // v0.25.44 (user: "1st > 2nd > 1st > 2nd > 3rd"): our own chain counter picks the swing of every hit
        // (0,1,0,1,2) and forces it like GooCombatOverhaul does (level preset, previousAttack = null,
        // timeSinceLastAttack = 0), the weapon keeps its native swings (4-swing weapons are capped at 3 so the
        // 3rd swing stays the finisher with Valheim's last-hit bonus). Hit 3 (swing 1 after swing 2) cross-fades
        // into the learned swing-1 state because Valheim's animator only chains forward.
        private static int _comboHit = -1, _comboSwing, _comboPrevSwing = -1, _comboCount;
        private static FieldInfo _atkNext;

        private static int ComboSwingFor(int hit, int count, int length)
        {
            if (count <= 1) return 0;
            if (hit >= length - 1) return count - 1;
            return count > 2 ? hit % 2 : 0;
        }

        private static float _comboLastEnd = -10f, _comboLastStart = -10f, _chainPrevSpeed = 1f;
        private static string _comboStartAnim;
        private static float _comboClockStart;

        // v0.25.63: dual wield plays different clips under the SAME trigger names, so its swing lengths are kept
        // apart from one-handed ones (sharing them made one-hand swings crawl after dual wielding and vice versa).
        private static float ChainAverage(string anim, bool dual, int length, float fallback)
        {
            float sum = 0f; int n = 0;
            for (int i = 0; i < length; i++)
            {
                float v;
                if (_chainNatural.TryGetValue(ChainKey(anim, dual, i), out v)) { sum += v; n++; }
            }
            return n > 0 ? sum / n : fallback;
        }

        private static string ChainKey(string anim, bool dual, int hit)
        {
            return (dual ? "dw|" : "1h|") + anim + ":" + hit.ToString();
        }
        private static readonly Dictionary<string, float> _chainNatural = new Dictionary<string, float>();

        private static void ComboStartPrefix(Attack __instance, Humanoid character, ItemDrop.ItemData weapon, ref Attack previousAttack, ref float timeSinceLastAttack)
        {
            _comboInStart = false;
            try
            {
                if (Instance == null || !Instance.ComboChainsEnabled.Value) return;
                Player p;
                if (!IsLocalMeleeStart(__instance, character, out p))
                {
                    // v0.25.99: any other local attack (heavy / area / ranged) also resets the chain speed.
                    Player lp = character as Player;
                    if (lp != null && lp == Player.m_localPlayer) { _comboHit = -1; DragonCombat.SetChainSpeed(lp, 1f, 0.05f); }
                    return;
                }
                int levels = (int)_atkLevels.GetValue(__instance);
                // v0.25.99 FIX (user: dual-wield Axe heavy attack stuck in slow motion): a heavy / single-hit attack
                // inherited the last chain swing's speed (dual chains run down to 0.25x for 3 s). Every attack that
                // is not part of our chain plays at normal speed and breaks the chain.
                if (levels < 2) { _comboHit = -1; DragonCombat.SetChainSpeed(p, 1f, 0.05f); return; }   // only weapons that already chain (no spears / single heavy hits)
                // v0.25.45 (user): the 5-hit chain belongs to classes fit for the weapon; everyone else keeps vanilla.
                if (DragonCombat.ComboWeaponProvider == null || !DragonCombat.ComboWeaponProvider(p, weapon)) { _comboHit = -1; DragonCombat.SetChainSpeed(p, 1f, 0.05f); return; }
                int count = Mathf.Min(levels, 3);
                if (levels > count) _atkLevels.SetValue(__instance, count);
                string anim = _atkAnim.GetValue(__instance) as string;
                int length = Mathf.Clamp(Instance.ComboChainLength.Value, 2, 9);
                string prevAnim = previousAttack == null ? null : _atkAnim.GetValue(previousAttack) as string;
                // our own timer: the next attack continues the chain when it starts while the previous swing is
                // still running (queued chain) or within the window after it ended.
                bool prevRunning = previousAttack != null && _comboLastEnd < 0f && Time.time - _comboLastStart < 2.5f;
                bool cont = _comboHit >= 0 && prevAnim == anim && anim == _comboBase && _comboHit < length - 1 && IsDualWielding(p) == _comboDual
                    && (prevRunning || Time.time - _comboLastEnd <= Mathf.Max(0.05f, Instance.ComboContinueWindow.Value));
                // v0.25.55 (user): every swing of the chain takes the same time (ComboSwingSeconds, 0.8s at no
                // attack-speed bonus), the finisher keeps its natural (longest) length. Each swing's natural time is
                // measured live (start -> next start, at the speed it played), so the slow backwards step
                // (swing 2 -> swing 1) is sped up like any other.
                // v0.25.59: keyed by the hit index itself (hit k lasted start(k) -> start(k+1)); the finisher's
                // length is measured start -> Stop in ComboStopPostfix. All 5 hits use ComboSwingSeconds.
                int nextHit = cont ? _comboHit + 1 : 0;
                if (cont)
                {
                    float measured = Time.time - _comboLastStart;
                    string mk = ChainKey(anim, _comboDual, _comboHit);
                    float nat = DragonCombat.ChainNaturalClock - _comboClockStart;
                    float old;
                    if (measured > 0.15f && (!_chainNatural.TryGetValue(mk, out old) || nat < old)) _chainNatural[mk] = nat;
                }
                float chainF = 1f;
                float natNow;
                bool dualNow = IsDualWielding(p);
                if (_chainNatural.TryGetValue(ChainKey(anim, dualNow, nextHit), out natNow))
                {
                    // v0.25.65 (user): intervals stay EVEN inside the chain, but the beat is the weapon's own average
                    // swing (a greatsword stays heavier than a one-handed sword); attack speed bonuses then scale that
                    // base speed (+50% = 1.5x), they no longer collapse every weapon onto one interval.
                    float target = Instance.ComboFixedInterval.Value ? Mathf.Max(0.2f, Instance.ComboSwingSeconds.Value) : ChainAverage(anim, dualNow, length, natNow);
                    // v0.25.67 (user: dual wield attacked like the Flash): the dual wield clips are short knife-speed
                    // swings that also strike with BOTH weapons. A dual swing now lasts at least as long as the same
                    // weapon's one-handed swing (or ComboSwingSeconds before that is known); Warfreak's +50% then
                    // applies on top like for any other weapon.
                    if (dualNow)
                    {
                        float oneHand = ChainAverage(anim, false, length, -1f);
                        target = Mathf.Max(target, oneHand > 0.2f ? oneHand : Mathf.Max(0.2f, Instance.ComboSwingSeconds.Value));
                    }
                    chainF = Mathf.Clamp(natNow / Mathf.Max(0.2f, target), dualNow ? 0.25f : 0.6f, 3f);
                }
                else if (dualNow) chainF = 0.6f;   // v0.25.67: not measured yet - start slower than the raw dual clips
                _comboStartAnim = anim;
                DragonCombat.SetChainSpeed(p, chainF, 3f);
                _chainPrevSpeed = DragonCombat.GetAttackSpeedMultiplier(p) * chainF;
                _comboLastEnd = -1f;
                _comboLastStart = Time.time;
                _comboClockStart = DragonCombat.ChainNaturalClock;
                _comboPrevSwing = cont ? _comboSwing : -1;
                _comboHit = cont ? _comboHit + 1 : 0;
                _comboCount = count;
                _comboSwing = ComboSwingFor(_comboHit, count, length);
                _atkLevel.SetValue(__instance, _comboSwing);
                previousAttack = null;
                timeSinceLastAttack = 0f;
                _comboBase = anim;
                _comboDual = IsDualWielding(p);
                _comboInStart = true;
            }
            catch (Exception) { _comboInStart = false; }
        }

        private static void ComboStartPostfix(Attack __instance, bool __result, Humanoid character)
        {
            if (__result && character != null && character == Player.m_localPlayer) DragonCombat.SkillAnimAttackBlockUntil = 0f;
            bool was = _comboInStart;
            _comboInStart = false;
            if (!was) return;
            if (!__result) { _comboHit = -1; return; }
            try
            {
                _atkLevel.SetValue(__instance, _comboSwing);
                if (_atkNext == null) _atkNext = typeof(Attack).GetField("m_nextAttackChainLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (_atkNext != null) _atkNext.SetValue(__instance, Mathf.Min(_comboSwing + 1, _comboCount - 1));
                int length = Mathf.Clamp(Instance.ComboChainLength.Value, 2, 9);
                if (_comboHit >= length - 1)
                {
                    _comboFinisher = __instance;
                    _comboHit = -1;
                    // safety net in case Stop never reports (interrupted swing)
                    _comboLockUntil = Mathf.Max(_comboLockUntil, Time.time + 1.6f + Mathf.Max(0f, Instance.ComboFinisherLockout.Value));
                }
            }
            catch (Exception) { }
        }

        private static void ComboStopPostfix(Attack __instance)
        {
            Player sp;
            if (IsLocalMeleeAttack(__instance, out sp) && _comboLastEnd < 0f) _comboLastEnd = Time.time;
            if (__instance == null || !ReferenceEquals(__instance, _comboFinisher)) return;
            _comboFinisher = null;
            try
            {
                // finisher length: start -> stop at the speed it played (min of what we have seen)
                float measured = Time.time - _comboLastStart;
                if (Instance != null && measured > 0.15f && !string.IsNullOrEmpty(_comboStartAnim))
                {
                    string fk = ChainKey(_comboStartAnim, _comboDual, Mathf.Clamp(Instance.ComboChainLength.Value, 2, 9) - 1);
                    float nat = DragonCombat.ChainNaturalClock - _comboClockStart, old;
                    if (!_chainNatural.TryGetValue(fk, out old) || nat < old) _chainNatural[fk] = nat;
                }
            }
            catch (Exception) { }
            _comboLockUntil = Time.time + Mathf.Max(0f, Instance == null ? 1f : Instance.ComboFinisherLockout.Value);
        }

        private static bool SkillAnimTriggerPrefix(Humanoid __instance)
        {
            return __instance == null || __instance != Player.m_localPlayer || Time.time >= DragonCombat.SkillAnimAttackBlockUntil;
        }

        private static bool ComboLockPrefix(Humanoid __instance, ref bool __result)
        {
            if (__instance == null || __instance != Player.m_localPlayer) return true;
            // v0.25.55: no normal attack while a skill holds the player (no attack sneaking out of a skill).
            if (DragonCombat.IsSkillLocked((Player)__instance)) { __result = false; return false; }
            if (Time.time >= _comboLockUntil) return true;
            __result = false;
            return false;
        }

        // The swing trigger Valheim fires inside Attack.Start is rewritten to our swing. A backwards step
        // (swing 1 after swing 2) cannot be reached by trigger, so it cross-fades into the learned state.
        private static bool ComboTriggerPrefix(object __instance, ref string __0)
        {
            if (!_comboInStart || string.IsNullOrEmpty(__0) || string.IsNullOrEmpty(_comboBase)) return true;
            try
            {
                if (!__0.StartsWith(_comboBase, StringComparison.Ordinal)) return true;
                string digits = __0.Substring(_comboBase.Length);
                int level;
                if (digits.Length == 0 || !int.TryParse(digits, out level)) return true;
                Component c = __instance as Component;
                Animator a = c == null ? null : c.GetComponentInChildren<Animator>();
                Player p = c == null ? null : c.GetComponent<Player>();
                bool dwClips = _comboDual && DragonDualWield.Apply(p, a, true);
                if (!_comboDual) DragonDualWield.Apply(p, a, false);
                string baseName = _comboBase;
                if (_comboDual && !dwClips && DragonCombat.AnimTriggerCount(a, "dual_knives") >= 2) baseName = "dual_knives";
                int count = DragonCombat.AnimTriggerCount(a, baseName);
                if (count <= 0) return true;
                int index = Mathf.Clamp(_comboSwing, 0, count - 1);
                if (_comboHit >= Mathf.Clamp(Instance.ComboChainLength.Value, 2, 9) - 1) index = count - 1;
                string target = baseName + index.ToString();
                if (a != null)
                {
                    for (int i = 0; i < count; i++) a.ResetTrigger(baseName + i.ToString());
                    if (_comboPrevSwing >= 0 && index <= _comboPrevSwing)
                    {
                        int hash, layer;
                        // v0.25.59: enter the repeated swing past its rest-pose wind up so it flows out of the last one
                        float blend = Mathf.Clamp(Instance.ComboBlend.Value, 0.02f, 0.4f);
                        float offset = Mathf.Clamp(Instance.ComboRepeatOffset.Value, 0f, 0.5f);
                        if (DragonCombat.LearnedState(a, target, out hash, out layer))
                        {
                            a.CrossFade(hash, blend / Mathf.Max(0.1f, StateLength(a, layer)), layer, offset);
                            return false;
                        }
                        int sh = Animator.StringToHash(target);
                        for (int li = 0; li < a.layerCount; li++)
                            if (a.HasState(li, sh)) { a.CrossFade(sh, blend / Mathf.Max(0.1f, StateLength(a, li)), li, offset); return false; }
                    }
                }
                __0 = target;
                DragonCombat.LearnState(a, target);
            }
            catch (Exception) { }
            return true;
        }

        // v0.25.59: the dual-wield animation set is swapped in/out between attacks (on equip), never inside a swing
        // trigger - swapping the controller mid-chain reset the animator and made dual wield stutter.
        internal static void SyncDualController(Player p)
        {
            try
            {
                if (p == null || p.IsDead() || p.InAttack()) return;
                Animator a = p.GetComponentInChildren<Animator>();
                if (a == null) return;
                DragonDualWield.Apply(p, a, IsDualWielding(p));
            }
            catch (Exception) { }
        }

        private static float StateLength(Animator a, int layer)
        {
            try { return a.GetCurrentAnimatorStateInfo(layer).length; } catch (Exception) { return 1f; }
        }

        // v0.25.59 (user: swings must flow through enemies): Valheim freezes the attacker's animation 0.15 s on every
        // melee hit (dual wield did it twice per hit). Skipped for the local player unless MeleeHitStop is on.
        private static bool FreezeFramePrefix(object __instance)
        {
            try
            {
                if (Instance == null || Instance.MeleeHitStop == null || Instance.MeleeHitStop.Value) return true;
                Component c = __instance as Component;
                if (c == null || Player.m_localPlayer == null) return true;
                if (c is Player) return c != Player.m_localPlayer;
                return c.GetComponentInParent<Player>() != Player.m_localPlayer;
            }
            catch (Exception) { return true; }
        }

        private static Type _trailType;
        private static PropertyInfo _trailEmit;

        // Dual wield: the off-hand weapon gets its swing trail too (like the DualWield mod).
        private static void OffhandTrailsPostfix(object __instance, bool enabled)
        {
            try
            {
                Component vc = __instance as Component;
                Player p = vc == null ? null : vc.GetComponent<Player>();
                if (p == null || !IsDualWielding(p)) return;
                FieldInfo f = __instance.GetType().GetField("m_leftItemInstance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                GameObject left = f == null ? null : f.GetValue(__instance) as GameObject;
                if (left == null) return;
                if (_trailType == null) { _trailType = DragonCombat.FindTypeCached("MeleeWeaponTrail"); if (_trailType != null) _trailEmit = _trailType.GetProperty("Emit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); }
                if (_trailType == null || _trailEmit == null) return;
                Component[] trails = left.GetComponentsInChildren(_trailType);
                for (int i = 0; i < trails.Length; i++) _trailEmit.SetValue(trails[i], enabled, null);
            }
            catch (Exception) { }
        }

        // DualWield-style off-hand strike: the left weapon swings the same arc mirrored, on every normal hit.
        private static void OffhandTriggerPrefix(Attack __instance)
        {
            if (_offhandSwing || _atkMelee == null) return;
            try
            {
                Player p;
                if (!IsLocalMeleeAttack(__instance, out p) || !IsDualWielding(p)) return;
                ItemDrop.ItemData left = DragonCombat.GetHandItem(p, "m_leftItem");
                object weapon = _atkWeapon.GetValue(__instance);
                float angle = (float)_atkAngle.GetValue(__instance);
                _offhandSwing = true;
                if (_atkHitPoint == null) _atkHitPoint = typeof(Attack).GetField("m_hitPointtype", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object hitPoint = _atkHitPoint == null ? null : _atkHitPoint.GetValue(__instance);
                try
                {
                    _atkWeapon.SetValue(__instance, left);
                    _atkAngle.SetValue(__instance, -angle);
                    // like the DualWield mod: the off-hand hit lands on the first thing it meets (one clean hit)
                    if (_atkHitPoint != null) { try { _atkHitPoint.SetValue(__instance, Enum.Parse(_atkHitPoint.FieldType, "First")); } catch (Exception) { } }
                    _atkMelee.Invoke(__instance, null);
                }
                finally
                {
                    _atkWeapon.SetValue(__instance, weapon);
                    _atkAngle.SetValue(__instance, angle);
                    if (_atkHitPoint != null && hitPoint != null) _atkHitPoint.SetValue(__instance, hitPoint);
                    _offhandSwing = false;
                }
            }
            catch (Exception) { _offhandSwing = false; }
        }

        private int PatchAttackStart()
        {
            MethodInfo patch = typeof(DragonCombatPlugin).GetMethod("AttackStartPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            if (patch == null)
                return 0;

            MethodInfo[] methods = typeof(Attack).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != "Start" || methods[i].ReturnType != typeof(bool))
                    continue;
                try
                {
                    PatchWithHarmony(methods[i], null, new HarmonyMethod(patch));
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Could not patch Attack.Start: " + ex.Message);
                }
            }
            return count;
        }

        private static void SetControlsPrefix(Player __instance, ref Vector3 movedir, ref bool attack, ref bool attackHold, ref bool secondaryAttack, ref bool secondaryAttackHold, ref bool block, ref bool blockHold, ref bool jump, ref bool crouch, ref bool run, ref bool autoRun, ref bool dodge)
        {
            if (__instance == null)
                return;

            string currentAdvancement = DragonCombat.GetAdvancementName(__instance);
            ItemDrop.ItemData currentWeapon = __instance.GetCurrentWeapon();

            if (DragonCombat.GetClassName(__instance) == "Sorcerer" &&
                currentAdvancement == "Spellcaster" &&
                DragonCombat.IsMagicWeapon(currentWeapon) &&
                (attack || attackHold))
            {
                // Spellcaster weapon mastery keeps sprint input alive while firing
                // normal Staff/Gun Staff attacks, not only while casting class skills.
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    run = true;
                    autoRun = false;
                }
            }

            if (DragonCombat.GetClassName(__instance) == "Sorcerer")
            {
                // v0.23.0 Warlock: a Sorcerer (and both ACs) can never Block or Parry.
                block = false;
                blockHold = false;
            }

            // v0.24.1: skill modules may remap mouse input (Ranger: Left Click quick shots,
            // Right Click = charged shot, no Block). Runs before the skill-lock filters below.
            if (DragonCombat.ControlsHook != null)
            {
                try { DragonCombat.ControlsHook(__instance, ref attack, ref attackHold, ref block, ref blockHold); }
                catch { }
            }

            if (__instance == Player.m_localPlayer && Time.time < DragonCombat.AttackSwallowUntil)
            {
                // v0.25.65: the click belonged to a skill - never let it become a (queued) basic attack
                attack = false; attackHold = false; secondaryAttack = false; secondaryAttackHold = false;
                DragonCombat.ClearAttackQueue(__instance);
            }

            if (DragonCombat.IsSkillLocked(__instance))
            {
                bool keepHold = DragonCombat.SkillBowHoldActive && attackHold;   // v0.25.86 charged bow skills = the vanilla draw hold
                DragonCombat.ClearAttackQueue(__instance);
                movedir = Vector3.zero;
                attack = false;
                attackHold = keepHold;
                secondaryAttack = false;
                secondaryAttackHold = false;
                block = false;
                blockHold = false;
                jump = false;
                crouch = false;
                run = false;
                autoRun = false;
                dodge = false;
                return;
            }

            if (DragonCombat.IsWhirlwindActive(__instance))
            {
                // Keep movedir untouched: movement speed remains Valheim's normal
                // non-sprint movement and therefore naturally scales with Run skill,
                // armor movement modifiers, buffs, etc.
                attack = false;
                attackHold = false;
                secondaryAttack = false;
                secondaryAttackHold = false;
                block = false;
                blockHold = false;
                jump = false;
                crouch = false;
                run = false;
                autoRun = false;
                dodge = false;
            }

            bool allowSprint;
            if (DragonCombat.IsMobileCastActive(__instance, out allowSprint))
            {
                attack = false;
                attackHold = false;
                secondaryAttack = false;
                secondaryAttackHold = false;
                block = false;
                blockHold = false;
                jump = false;
                crouch = false;
                if (!allowSprint)
                {
                    run = false;
                    autoRun = false;
                }
                dodge = false;
            }
        }

        private static void CheckRunPostfix(Player __instance, ref bool __result)
        {
            if (__result || __instance == null || __instance != Player.m_localPlayer)
                return;
            if (DragonCombat.GetClassName(__instance) != "Sorcerer" || DragonCombat.GetAdvancementName(__instance) != "Spellcaster")
                return;
            if (!DragonCombat.IsMagicWeapon(__instance.GetCurrentWeapon()))
                return;
            if (!Input.GetKey(KeyCode.Mouse0) || (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)))
                return;

            try
            {
                MethodInfo method = typeof(Player).GetMethod("GetStamina", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method != null && Convert.ToSingle(method.Invoke(__instance, null)) <= 0.1f)
                    return;
            }
            catch
            {
            }

            __result = true;
        }

        private static bool InterruptPrefix(Character __instance)
        {
            if (__instance != null && __instance == Player.m_localPlayer && Time.time < DragonCombat.BlockHyperUntil) return false;
            return !DragonCombat.HasHyperArmor(__instance);
        }

        // v0.25.63: the blocked-hit reaction animation slides the player back with root motion; under Hyper Armor
        // that slide is dropped (attack lunges are untouched).
        private static bool SeAddPrefix(object __instance, object[] __args)
        {
            try
            {
                if (!DragonCombat.AnyDebuffImmunity || __args == null || __args.Length == 0) return true;
                Character ch = DragonCombat.SemanOwner(__instance);
                if (ch == null || !DragonCombat.IsDebuffImmune(ch)) return true;
                string name = null;
                StatusEffect se = __args[0] as StatusEffect;
                if (se != null) name = se.name;
                else if (__args[0] is int) name = DragonCombat.StatusNameFromHash((int)__args[0]);
                if (name != null && DragonCombat.IsDebuffEffectName(name)) return false;
            }
            catch (Exception) { }
            return true;
        }

        private static void RpcDamagePrefix(Character __instance, object[] __args)
        {
            try
            {
                Player p = __instance as Player;
                if (p == null || p != Player.m_localPlayer || __args == null) return;
                for (int i = 0; i < __args.Length; i++)
                {
                    HitData hit = __args[i] as HitData;
                    if (hit != null) { DragonCombat.EvaluateIncomingHyper(p, hit); return; }
                }
            }
            catch (Exception) { }
        }

        private static bool RootMotionPrefix(Character __instance)
        {
            try
            {
                if (__instance == null || __instance != Player.m_localPlayer || Time.time >= DragonCombat.BlockHyperUntil) return true;
                Humanoid h = __instance as Humanoid;
                return h != null && h.InAttack();
            }
            catch (Exception) { return true; }
        }

        private static void DamagePrefix(Character __instance, object[] __args, ref DamagePatchState __state)
        {
            __state = default(DamagePatchState);
            if (__instance == null || __args == null || __args.Length < 1 || !(__args[0] is HitData))
                return;

            HitData hit = (HitData)__args[0];
            __state = DragonCombat.BeginDamage(__instance, hit);
        }

        private static void DamagePostfix(Character __instance, DamagePatchState __state)
        {
            DragonCombat.EndDamage(__instance, __state);
        }

        private static bool HotbarUsePrefix(object __instance)
        {
            if (Instance == null ||
                Instance.BlockHotbarWhenSkillModifierHeld == null ||
                !Instance.BlockHotbarWhenSkillModifierHeld.Value)
                return true;

            Player player = __instance as Player;

            if (player == null || player != Player.m_localPlayer)
                return true;

            if (!DragonCombat.IsSkillModifierHeld())
                return true;

            bool skillNumberPressed =
                Input.GetKeyDown(KeyCode.Alpha1) ||
                Input.GetKeyDown(KeyCode.Alpha2) ||
                Input.GetKeyDown(KeyCode.Alpha3) ||
                Input.GetKeyDown(KeyCode.Alpha4) ||
                Input.GetKeyDown(KeyCode.Alpha5) ||
                Input.GetKeyDown(KeyCode.Alpha6) ||
                Input.GetKeyDown(KeyCode.Alpha7) ||
                Input.GetKeyDown(KeyCode.Alpha8) ||
                Input.GetKeyDown(KeyCode.Alpha9) ||
                Input.GetKeyDown(KeyCode.Alpha0);

            // Returning false skips Valheim's hotbar action for this frame.
            // Dragon's Altar's skill Update still sees the same key press.
            return !skillNumberPressed;
        }

        private static void EquipmentMovementPostfix(Player __instance, ref float __result)
        {
            // v0.18.1: Heaven's Light (Grace) removes equipment movement penalties while active.
            if (__result < 0f && DragonCombat.HasNoEquipmentPenalty(__instance))
            {
                __result = 0f;
                return;
            }
            if (__result >= 0f || Instance == null || __instance == null)
                return;
            // v0.22.4 Warfreak (Mercenary): no movement penalty from physical weapons.
            // v0.22.3 The Way of the Sword (Sword Master): no movement penalty from Swords.
            string advancement = DragonCombat.GetAdvancementName(__instance);
            if (advancement == "Mercenary" || advancement == "Sword Master")
            {
                ItemDrop.ItemData mainHand = DragonCombat.GetHandItem(__instance, "m_rightItem");
                ItemDrop.ItemData offHand = DragonCombat.GetHandItem(__instance, "m_leftItem");
                __result -= WeaponMasteryExemptPenalty(mainHand, advancement);
                if (offHand != mainHand)
                    __result -= WeaponMasteryExemptPenalty(offHand, advancement);
                if (__result > 0f)
                    __result = 0f;
                return;
            }
            // v0.24.0 Wildborn (Ranger's Blessing): no movement penalty from Bows or Crossbows
            // (bows sit in the left hand, crossbows in the right).
            if (DragonCombat.GetClassName(__instance) == "Ranger")
            {
                // v0.24.1: Crossbows lose their penalty only for the Bowmaster.
                bool crossbows = advancement == "Bowmaster";
                ItemDrop.ItemData r = DragonCombat.GetHandItem(__instance, "m_rightItem");
                ItemDrop.ItemData l = DragonCombat.GetHandItem(__instance, "m_leftItem");
                __result -= RangedExemptPenalty(r, crossbows);
                if (l != r)
                    __result -= RangedExemptPenalty(l, crossbows);
                if (__result > 0f)
                    __result = 0f;
                return;
            }
            // v0.25.66 (user): every class moves freely with its fitting weapons.
            // Sorcerer + all its ACs: Staves, Wands and Gun Staves. Base Warrior: its melee weapons.
            string cls66 = DragonCombat.GetClassName(__instance);
            if (cls66 == "Sorcerer" || cls66 == "Warrior")
            {
                ItemDrop.ItemData r66 = DragonCombat.GetHandItem(__instance, "m_rightItem");
                ItemDrop.ItemData l66 = DragonCombat.GetHandItem(__instance, "m_leftItem");
                __result -= ClassFitExemptPenalty(r66, cls66);
                if (l66 != r66)
                    __result -= ClassFitExemptPenalty(l66, cls66);
                if (__result > 0f)
                    __result = 0f;
                return;
            }
            if (cls66 != "Cleric")
                return;

            // v0.20.8 Cleric's Blessing: no penalty from Shields, Staves and one-handed Club weapons.
            ItemDrop.ItemData right = DragonCombat.GetHandItem(__instance, "m_rightItem");
            ItemDrop.ItemData left = DragonCombat.GetHandItem(__instance, "m_leftItem");
            if (Instance.ClericBlessingNoPenalty.Value)
            {
                __result -= ClericExemptPenalty(right);
                if (left != right)
                    __result -= ClericExemptPenalty(left);
            }

            // v0.20.8 Holy Trinity (Paladin, Club-type melee weapon + any Shield): no Armor penalty.
            if (Instance.HolyTrinityNoArmorPenalty.Value && DragonCombat.IsHolyTrinityActive(__instance))
            {
                string[] armor = { "m_chestItem", "m_legItem", "m_helmetItem", "m_shoulderItem" };
                for (int i = 0; i < armor.Length; i++)
                    __result -= NegativeModifier(DragonCombat.GetHandItem(__instance, armor[i]));
            }
            if (__result > 0f)
                __result = 0f;
        }

        private static float NegativeModifier(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return 0f;
            return Mathf.Min(0f, item.m_shared.m_movementModifier);
        }

        private static float RangedExemptPenalty(ItemDrop.ItemData item, bool crossbows)
        {
            if (item == null || item.m_shared == null)
                return 0f;
            Skills.SkillType s = item.m_shared.m_skillType;
            return s == Skills.SkillType.Bows || (crossbows && s == Skills.SkillType.Crossbows) ? NegativeModifier(item) : 0f;
        }

        private static float WeaponMasteryExemptPenalty(ItemDrop.ItemData item, string advancement)
        {
            if (item == null || item.m_shared == null)
                return 0f;
            Skills.SkillType s = item.m_shared.m_skillType;
            bool exempt = advancement == "Sword Master"
                ? s == Skills.SkillType.Swords
                : (s == Skills.SkillType.Swords || s == Skills.SkillType.Axes || s == Skills.SkillType.Clubs ||
                   s == Skills.SkillType.Knives || s == Skills.SkillType.Polearms || s == Skills.SkillType.Spears);
            return exempt ? NegativeModifier(item) : 0f;
        }

        private static float ClassFitExemptPenalty(ItemDrop.ItemData item, string cls)
        {
            if (item == null || item.m_shared == null)
                return 0f;
            if (cls == "Sorcerer")
                return DragonCombat.IsMagicWeapon(item) ? NegativeModifier(item) : 0f;
            Skills.SkillType s = item.m_shared.m_skillType;
            bool fit = s == Skills.SkillType.Swords || s == Skills.SkillType.Axes || s == Skills.SkillType.Clubs ||
                       s == Skills.SkillType.Polearms || s == Skills.SkillType.Spears;
            return fit ? NegativeModifier(item) : 0f;
        }

        private static float ClericExemptPenalty(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return 0f;
            bool oneHandedClub = item.m_shared.m_skillType == Skills.SkillType.Clubs && !DragonCombat.IsTwoHandedWeapon(item);
            if (DragonCombat.IsShield(item) || DragonCombat.IsStaffWeapon(item) || oneHandedClub)
                return NegativeModifier(item);
            return 0f;
        }

        private static void EquipItemPrefix(Humanoid __instance, object[] __args, ref EquipPatchState __state)
        {
            __state = new EquipPatchState();

            if (Instance == null || __instance == null || __args == null || __args.Length < 1)
                return;

            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer)
                return;

            ItemDrop.ItemData item = __args[0] as ItemDrop.ItemData;
            if (item == null)
                return;

            string advancement = DragonCombat.GetAdvancementName(player);
            ItemDrop.ItemData left = DragonCombat.GetHandItem(player, "m_leftItem");
            ItemDrop.ItemData right = DragonCombat.GetHandItem(player, "m_rightItem");

            if (Instance.EnableWarfreakDualWield.Value && advancement == "Mercenary" && DragonCombat.IsOneHandedWeapon(item))
            {
                ItemDrop.ItemData existingWeapon = null;

                if (DragonCombat.IsOneHandedWeapon(right) && right != item)
                    existingWeapon = right;
                else if (DragonCombat.IsOneHandedWeapon(left) && left != item)
                    existingWeapon = left;

                if (existingWeapon != null)
                {
                    __state.Active = true;
                    __state.MercenaryDualWeapons = true;
                    __state.ExistingWeapon = existingWeapon;
                    __state.NewItem = item;
                    return;
                }
            }

            if (Instance.EnableSpellcasterDualGunStaves.Value && advancement == "Spellcaster" && DragonCombat.IsGunStaff(item))
            {
                ItemDrop.ItemData existingGunStaff = null;

                if (DragonCombat.IsGunStaff(right) && right != item)
                    existingGunStaff = right;
                else if (DragonCombat.IsGunStaff(left) && left != item)
                    existingGunStaff = left;

                if (existingGunStaff != null)
                {
                    __state.Active = true;
                    __state.SpellcasterDualGunStaves = true;
                    __state.ExistingWeapon = existingGunStaff;
                    __state.NewItem = item;
                    return;
                }
            }

            if (Instance.EnableDivineStaffShield.Value && DragonCombat.GetClassName(player) == "Cleric")
            {
                if (DragonCombat.IsStaffWeapon(item))
                {
                    ItemDrop.ItemData shield = DragonCombat.IsShield(left) ? left : (DragonCombat.IsShield(right) ? right : null);
                    if (shield != null)
                    {
                        __state.Active = true;
                        __state.DivineStaffShield = true;
                        __state.ExistingOffhand = shield;
                        __state.NewItem = item;
                        return;
                    }
                }

                if (DragonCombat.IsShield(item))
                {
                    ItemDrop.ItemData staff = DragonCombat.IsStaffWeapon(right) ? right : (DragonCombat.IsStaffWeapon(left) ? left : null);
                    if (staff != null)
                    {
                        __state.Active = true;
                        __state.DivineStaffShield = true;
                        __state.ExistingWeapon = staff;
                        __state.NewItem = item;
                    }
                }
            }
        }

        private static void EquipItemPostfix(Humanoid __instance, ref bool __result, EquipPatchState __state)
        {
            if (!__state.Active || __instance == null)
                return;

            Player player = __instance as Player;
            if (player == null || player != Player.m_localPlayer)
                return;

            try
            {
                if (__state.MercenaryDualWeapons)
                {
                    ItemDrop.ItemData right = __state.ExistingWeapon;
                    ItemDrop.ItemData left = __state.NewItem;

                    if (right != null && left != null && right != left)
                    {
                        DragonCombat.SetHandItem(player, "m_rightItem", right);
                        DragonCombat.SetHandItem(player, "m_leftItem", left);
                        DragonCombat.SetEquipped(right, true);
                        DragonCombat.SetEquipped(left, true);
                        DragonCombat.RefreshEquipment(player);
                        __result = true;
                    }

                    return;
                }

                if (__state.SpellcasterDualGunStaves)
                {
                    ItemDrop.ItemData right = __state.ExistingWeapon;
                    ItemDrop.ItemData left = __state.NewItem;

                    if (right != null && left != null && right != left)
                    {
                        DragonCombat.SetHandItem(player, "m_rightItem", right);
                        DragonCombat.SetHandItem(player, "m_leftItem", left);
                        DragonCombat.SetEquipped(right, true);
                        DragonCombat.SetEquipped(left, true);
                        DragonCombat.RefreshEquipment(player);
                        __result = true;
                    }

                    return;
                }

                if (__state.DivineStaffShield)
                {
                    ItemDrop.ItemData staff = __state.ExistingWeapon;
                    ItemDrop.ItemData shield = __state.ExistingOffhand;

                    if (DragonCombat.IsStaffWeapon(__state.NewItem))
                        staff = __state.NewItem;
                    if (DragonCombat.IsShield(__state.NewItem))
                        shield = __state.NewItem;

                    if (staff != null && shield != null && staff != shield)
                    {
                        DragonCombat.SetHandItem(player, "m_rightItem", staff);
                        DragonCombat.SetHandItem(player, "m_leftItem", shield);
                        DragonCombat.SetEquipped(staff, true);
                        DragonCombat.SetEquipped(shield, true);
                        DragonCombat.RefreshEquipment(player);
                        __result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Instance != null)
                    Instance.Logger.LogWarning("Weapon Mastery equipment restore failed: " + ex.Message);
            }
        }

        private static void AnimationSpeedPostfix(Character ___m_character, Animator ___m_animator)
        {
            if (___m_animator == null || ___m_character == null) return;
            Player player = ___m_character as Player;
            if (player != null && player == Player.m_localPlayer) DragonCombat.ApplyManagedAnimationSpeed(player, ___m_animator);
            DragonCombat.ApplyFrostAnimationSpeed(___m_character, ___m_animator);
        }

        private static void SpeedPrefix(object __instance, ref float __0)
        {
            Player player = DragonCombat.GetPlayerFromSEMan(__instance);
            if (player == null)
                return;
            __0 *= DragonCombat.GetMoveSpeedMultiplier(player);
        }

        private static void StaminaRegenPrefix(object __instance, ref float __0)
        {
            Player player = DragonCombat.GetPlayerFromSEMan(__instance);
            if (player == null)
                return;
            if (DragonCombat.IsStaminaRegenBlocked(player))
            {
                __0 = 0f;
                return;
            }
            __0 *= DragonCombat.GetStaminaRegenMultiplier(player);
        }

        private static void EitrRegenPrefix(object __instance, ref float __0)
        {
            Player player = DragonCombat.GetPlayerFromSEMan(__instance);
            if (player == null)
                return;
            if (DragonCombat.IsEitrRegenBlocked(player))
            {
                __0 = 0f;
                return;
            }
            __0 *= DragonCombat.GetEitrRegenMultiplier(player);
        }

        private static void AttackStaminaPostfix(Attack __instance, ref float __result)
        {
            if (__instance == null || __result <= 0f)
                return;

            Player player = Player.m_localPlayer;
            if (player == null || !DragonCombat.IsWeaponMasteryAttack(player, __instance))
                return;

            float multiplier = Instance == null ? 0.70f : Mathf.Clamp(Instance.MasteryStaminaMultiplier.Value, 0f, 2f);
            __result *= multiplier;
        }

        private static void AttackStartPostfix(Attack __instance, ref bool __result, object[] __args)
        {
            if (!__result || __instance == null)
                return;

            Player player = null;
            ItemDrop.ItemData weapon = null;

            if (__args != null)
            {
                for (int i = 0; i < __args.Length; i++)
                {
                    if (player == null)
                        player = __args[i] as Player;

                    if (weapon == null)
                        weapon = __args[i] as ItemDrop.ItemData;
                }
            }

            if (player == null)
                player = Player.m_localPlayer;

            if (player == null || player != Player.m_localPlayer)
                return;

            DragonCombat.RegisterMasteryAttackStart(player, __instance, weapon);
        }
    }

    public struct EquipPatchState
    {
        public bool Active;
        public bool MercenaryDualWeapons;
        public bool DivineStaffShield;
        public bool SpellcasterDualGunStaves;
        public ItemDrop.ItemData ExistingWeapon;
        public ItemDrop.ItemData ExistingOffhand;
        public ItemDrop.ItemData NewItem;
    }

    public struct BlockPatchState
    {
        public object Shared;
        public FieldInfo ParryField;
        public FieldInfo ForceField;
        public FieldInfo PowerField;
        public float OriginalParry;
        public float OriginalForce;
        public float OriginalPower;
        public bool BucklerParryWindow;
        public bool HyperBlock;
    }

    internal class AnimatorSpeedRuntimeState
    {
        public bool HasOutput;
        public float LastOutputSpeed = 1f;
        public float LastFactor = 1f;
    }

    public struct DamagePatchState
    {
        public bool RestoreModifiers;
        public HitData.DamageModifiers OriginalModifiers;
    }

    internal class TimedBuffState
    {
        public float EndTime;
        public float AttackDamageBonus;
        public float AttackSpeedBonus;
        public float MoveSpeedBonus;
        public float DefenseBonus;
        public float StaminaRegenBonus;
        public float EitrRegenBonus;
        public bool HyperArmor;
    }

    internal class DebuffState
    {
        public Character Target;
        public float ExposeUntil;
        public float BrokenBonesUntil;
        public float CrippleUntil;
        public float FrostUntil;
        public float ZapAt;
        public Player ZapAttacker;
        public float ZapDamage;
        public float ZapRadius;
        public bool ZapPending;
    }

    internal class MasteryComboState
    {
        public int Stage;
        public float LastAttackTime;
        public float FinisherUntil;
        public float FinisherAttackStartedAt;
        public Skills.SkillType SkillType;
    }


    public class DragonSkillPoseDriver : MonoBehaviour
    {
        private Animator _animator;
        private string _style = "";
        private float _startTime;
        private float _endTime;

        public void Begin(string style, float duration)
        {
            _style = style == null ? "" : style;
            _startTime = Time.time;
            _endTime = Time.time + Mathf.Max(0.10f, duration);

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        private void LateUpdate()
        {
            if (Time.time >= _endTime)
            {
                Destroy(this);
                return;
            }

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator == null || !_animator.isHuman)
                return;

            float duration = Mathf.Max(0.10f, _endTime - _startTime);
            float p = Mathf.Clamp01((Time.time - _startTime) / duration);

            // Wind up strongly, then release quickly near the end.
            float weight;
            if (p < 0.78f)
                weight = Mathf.SmoothStep(0f, 1f, p / 0.78f);
            else
                weight = 1f - Mathf.SmoothStep(0f, 1f, (p - 0.78f) / 0.22f);

            ApplyStyle(_style, weight, p);
        }

        private void ApplyStyle(string style, float weight, float phase)
        {
            if (style == "HeavySlash" || style == "Halfmoon")
            {
                Offset(HumanBodyBones.Spine, new Vector3(0f, -28f, -10f), weight);
                Offset(HumanBodyBones.Chest, new Vector3(0f, -24f, -8f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-18f, -42f, -72f), weight);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(8f, -12f, -28f), weight);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-8f, 16f, 28f), weight * 0.55f);
                return;
            }

            if (style == "Uppercut" || style == "Crescent")
            {
                Offset(HumanBodyBones.Spine, new Vector3(14f, 0f, 0f), weight);
                Offset(HumanBodyBones.Chest, new Vector3(10f, 10f, 0f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(58f, -12f, -36f), weight);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(32f, 0f, -18f), weight);
                return;
            }

            if (style == "Punch")
            {
                Offset(HumanBodyBones.Chest, new Vector3(0f, -18f, -6f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-38f, -28f, -56f), weight);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(-62f, 4f, -10f), weight);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-15f, 18f, 22f), weight * 0.6f);
                return;
            }

            if (style == "Raise" || style == "SkyCast" || style == "Tempest")
            {
                Offset(HumanBodyBones.Spine, new Vector3(-8f, 0f, 0f), weight);
                Offset(HumanBodyBones.Chest, new Vector3(-10f, 0f, 0f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-92f, 0f, -12f), weight);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(-18f, 0f, 4f), weight);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-42f, 0f, 18f), weight * 0.55f);
                return;
            }

            if (style == "Chant")
            {
                // v0.17.0: main hand lifted forward and up, like chanting a spell (instant casts).
                Offset(HumanBodyBones.Chest, new Vector3(-4f, 0f, 0f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-78f, -12f, -10f), weight);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(-34f, 0f, 0f), weight);
                Offset(HumanBodyBones.RightHand, new Vector3(-18f, 0f, 0f), weight);
                return;
            }

            if (style == "Wave" || style == "Channel" || style == "Sigil")
            {
                Offset(HumanBodyBones.Chest, new Vector3(-6f, 0f, 0f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-62f, -20f, -12f), weight);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(-28f, 0f, 0f), weight);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-62f, 20f, 12f), weight);
                Offset(HumanBodyBones.LeftLowerArm, new Vector3(-28f, 0f, 0f), weight);
                return;
            }

            if (style == "Slam" || style == "Stomp")
            {
                Offset(HumanBodyBones.Spine, new Vector3(18f, 0f, 0f), weight);
                Offset(HumanBodyBones.Chest, new Vector3(14f, 0f, 0f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(48f, -8f, -28f), weight);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(48f, 8f, 28f), weight * 0.75f);
                return;
            }

            if (style == "Moonlight")
            {
                float pulse = 1f;
                Offset(HumanBodyBones.Spine, new Vector3(0f, -22f, -4f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-28f, -48f, -68f), weight * pulse);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(12f, -8f, -34f), weight * pulse);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-12f, 18f, 30f), weight * 0.65f);
                return;
            }

            if (style == "Whirlwind")
            {
                float sway = 0f;
                Offset(HumanBodyBones.Spine, new Vector3(0f, sway * 18f, 0f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-12f, -28f, -78f), weight);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-12f, 28f, 78f), weight);
                return;
            }

            if (style == "CircleSwing")
            {
                float skip = 0f;
                float sweep = Mathf.SmoothStep(-1f, 1f, Mathf.Clamp01((phase - 0.58f) / 0.42f));
                Offset(HumanBodyBones.Hips, new Vector3(skip * 7f, 0f, 0f), weight * 0.45f);
                Offset(HumanBodyBones.Spine, new Vector3(0f, sweep * 72f, -8f), weight);
                Offset(HumanBodyBones.Chest, new Vector3(0f, sweep * 58f, -10f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-20f, -55f, -92f), weight);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(8f, -18f, -28f), weight);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-12f, 32f, 46f), weight * 0.70f);
                return;
            }

            if (style == "EmptySheath")
            {
                Offset(HumanBodyBones.Spine, new Vector3(0f, -12f, -5f), weight);
                Offset(HumanBodyBones.Chest, new Vector3(0f, -18f, -8f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(20f, -58f, -76f), weight);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(34f, -18f, -20f), weight);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-18f, 36f, 42f), weight * 0.85f);
                Offset(HumanBodyBones.LeftLowerArm, new Vector3(-10f, 18f, 12f), weight * 0.70f);
                return;
            }
        }

        private readonly Dictionary<Transform, Quaternion> _poseBase = new Dictionary<Transform, Quaternion>();
        private readonly Dictionary<Transform, Quaternion> _poseWritten = new Dictionary<Transform, Quaternion>();

        private void Offset(HumanBodyBones bone, Vector3 euler, float weight)
        {
            Transform target = _animator.GetBoneTransform(bone);
            if (target == null)
                return;

            // v0.25.14: never stack the offset on a frame the animator did not rewrite the bone (jitter).
            Quaternion cur = target.localRotation, written = Quaternion.identity, basePose = Quaternion.identity;
            if (_poseWritten.TryGetValue(target, out written) && Quaternion.Angle(cur, written) < 0.01f && _poseBase.TryGetValue(target, out basePose)) cur = basePose;
            _poseBase[target] = cur;
            Quaternion w = cur * Quaternion.Euler(euler * weight);
            target.localRotation = w;
            _poseWritten[target] = w;
        }
    }

    // ==================================================================================
    // v0.25.7 SKILL CLIPS: keyframed bone animation (spine, chest, head, both arms, hands) plus a
    // body-root lean/offset, timed to each skill: keys with T < 0 are fractions of the wind-up
    // (-1 = cast start, 0 = impact), keys with T > 0 are seconds after impact. A "hold" clip stays on
    // its T = 0 pose until DragonCombat.ClipImpact (landing, charge release, bash...).
    // Bone euler vocabulary (as tuned for the Valheim rig in DragonSkillPoseDriver):
    //   upper arm x- = swing forward/up (-90 forward, -160 overhead), x+ = back; right z- / left z+ = out to the side.
    //   forearm x- = bend the elbow. spine/chest x+ = bend forward, y- = twist the right shoulder back.
    //   head x- = look up. Root R.x+ = whole body leans forward; O = body offset (y down < 0).
    // ==================================================================================
    public class DragonClipKey
    {
        public float T;
        public Vector3[] B = new Vector3[10];
        public Vector3 R, O;
        // v0.25.15 legs (Unity humanoid muscle deltas, rig independent): per side lift (thigh forward +),
        // spread (out +), bend (knee bend +), toe (foot up +). L[0..3] left, L[4..7] right.
        public float[] L = new float[8];
        public float Spin;   // v0.25.15 axial roll around the body's own head-to-feet axis (degrees)
        public bool Lin;   // linear (constant speed) blend INTO this key: spins
        public DragonClipKey(float t) { T = t; }
        public DragonClipKey Linear() { Lin = true; return this; }
        public DragonClipKey Hp(float x, float y, float z) { B[0] = new Vector3(x, y, z); return this; }
        public DragonClipKey Sp(float x, float y, float z) { B[1] = new Vector3(x, y, z); return this; }
        public DragonClipKey Ch(float x, float y, float z) { B[2] = new Vector3(x, y, z); return this; }
        public DragonClipKey Hd(float x, float y, float z) { B[3] = new Vector3(x, y, z); return this; }
        public DragonClipKey RA(float x, float y, float z) { B[4] = new Vector3(x, y, z); return this; }
        public DragonClipKey RF(float x, float y, float z) { B[5] = new Vector3(x, y, z); return this; }
        public DragonClipKey RH(float x, float y, float z) { B[6] = new Vector3(x, y, z); return this; }
        public DragonClipKey LA(float x, float y, float z) { B[7] = new Vector3(x, y, z); return this; }
        public DragonClipKey LF(float x, float y, float z) { B[8] = new Vector3(x, y, z); return this; }
        public DragonClipKey LH(float x, float y, float z) { B[9] = new Vector3(x, y, z); return this; }
        public DragonClipKey Rot(float x, float y, float z) { R = new Vector3(x, y, z); return this; }
        public DragonClipKey Off(float x, float y, float z) { O = new Vector3(x, y, z); return this; }
        public DragonClipKey LL(float lift, float spread, float bend, float toe) { L[0] = lift; L[1] = spread; L[2] = bend; L[3] = toe; return this; }
        public DragonClipKey RL(float lift, float spread, float bend, float toe) { L[4] = lift; L[5] = spread; L[6] = bend; L[7] = toe; return this; }
        public DragonClipKey Sn(float degrees) { Spin = degrees; return this; }
        // v0.25.20 held items: where the main-hand item (Wp) / off-hand item (Oi) points, in the body's frame
        // (x right, y up, z forward). The hand turns so the weapon / tool / staff really follows the motion.
        public Vector3 WD, SD;
        public float WW, SW;
        public DragonClipKey Wp(float x, float y, float z) { WD = new Vector3(x, y, z); WW = 1f; return this; }
        public DragonClipKey Oi(float x, float y, float z) { SD = new Vector3(x, y, z); SW = 1f; return this; }
        // v0.25.21 two-handed grip: the off hand reaches the main weapon (arm IK) `grip` metres along its axis
        // (negative = toward the pommel); the off-hand item is hidden render-only meanwhile.
        public float TW, TG;
        public DragonClipKey Two(float grip) { TW = 1f; TG = grip; return this; }
        // v0.25.23 HAND TARGETS (storyboard poses): the main hand is placed by arm IK at `reach` (0..1 of the arm's
        // length) along a direction from the RIGHT SHOULDER in the body frame (x right, y up, z forward). Rig-axis
        // independent: "hand straight up" is always straight up.
        public Vector3 HD;
        public float HR, HW;
        public DragonClipKey Hand(float x, float y, float z, float reach) { HD = new Vector3(x, y, z); HR = reach; HW = 1f; return this; }
        // v0.25.24 off-hand target (from the LEFT shoulder): only where stated (Ranger bow arm).
        public Vector3 LD;
        public float LR, LW;
        public DragonClipKey LHand(float x, float y, float z, float reach) { LD = new Vector3(x, y, z); LR = reach; LW = 1f; return this; }
        // v0.25.85 elbow pole override (both arms, mirrored for the left): Wing() = chicken wing - the upper arm is
        // raised out to the side, elbow out/back, so the forearm can point FORWARD.
        public Vector3 EP;
        public float EW;
        public DragonClipKey Wing() { EP = new Vector3(0.6f, -0.4f, -0.7f); EW = 1f; EM = false; return this; }
        // v0.25.86 main-hand-only chicken wing (Bonecrusher / Electric Smite flight; the off hand stays as it is)
        public bool EM;
        public DragonClipKey WingMain() { EP = new Vector3(0.6f, -0.4f, -0.7f); EW = 1f; EM = true; return this; }
        // v0.25.25 VANILLA LAYER (first key only): Valheim attack animation `VA` fired `VL` seconds before the
        // impact (VR > 0 = repeat every VR seconds while the clip runs); NoAim = the vanilla animation holds the
        // weapon, so the universal "weapon follows the forearm" rule stays off.
        public string VA;
        public float VL, VR;
        // v0.25.30 fraction of the wind up at which a one-shot vanilla animation starts (0 = at once); it is then
        // sped up / slowed down so it ENDS exactly when the wind up ends.
        public float VF;
        // v0.25.26 foot lift (metres above the planted spot): a knee can finally come up (Stomp).
        public float FLh, FRh;
        public DragonClipKey Lift(float left, float right) { FLh = left; FRh = right; return this; }
        public bool NoAim;
        // v0.25.35 legs belong to the animator (forced run under a charge): no foot planting.
        public bool NoPlant;
        // v0.25.40 buff raise (copied from the user's approved ChatGPT pass): QL = the off-hand arm is frozen to
        // the pose it had when the skill began; NoTrack = the vanilla state plays at its natural speed.
        public bool QL, NoTrack;
        // v0.25.85 Ranger vanilla bow layer: the equipped bow's own draw bool is held through the wind up / hold and
        // its own attack trigger fires at the impact (release).
        public string BowTrig, BowBool;
        // v0.25.86 constant-speed vanilla timing (Sword Master): one steady speed per swing (hit frame on the impact),
        // no stretched anticipation, no 6x recovery.
        public bool ConstSpeed;
        // Same pose as another key at a new time (holds / shakes).
        public DragonClipKey Copy(float t)
        {
            DragonClipKey k = new DragonClipKey(t);
            for (int i = 0; i < B.Length; i++) k.B[i] = B[i];
            for (int i = 0; i < L.Length; i++) k.L[i] = L[i];
            k.R = R; k.O = O; k.Lin = Lin; k.Spin = Spin;
            k.WD = WD; k.WW = WW; k.SD = SD; k.SW = SW; k.TW = TW; k.TG = TG; k.HD = HD; k.HR = HR; k.HW = HW; k.LD = LD; k.LR = LR; k.LW = LW; k.FLh = FLh; k.FRh = FRh; k.EP = EP; k.EW = EW; k.EM = EM;
            return k;
        }
    }

    public class DragonForceRun : MonoBehaviour
    {
        private Character _owner;
        private void Update() { Tick(); }
        private void FixedUpdate() { Tick(); }
        private void Tick()
        {
            if (_owner == null) _owner = GetComponent<Character>();
            if (_owner == null || _owner.IsDead()) { DragonCombat.ForceRun(_owner as Player, false); Destroy(this); return; }
            if (!DragonCombat.ForceRunTick()) Destroy(this);
        }
    }

    public class DragonSkillClipDriver : MonoBehaviour
    {
        private static readonly HumanBodyBones[] Bones =
        {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Head,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand
        };
        private static readonly Vector3 Pivot = new Vector3(0f, 0.9f, 0f);

        private Animator _animator;
        private Character _owner;
        private Transform _visual;
        private DragonClipKey[] _keys;
        private float _start, _windup, _impactAt = -1f, _holdLimit;
        private bool _hold;
        // v0.25.88 JSAA touchdown anticipation: only Bonecrusher / Electric Smite.
        // Gameplay still decides the actual impact. These fields only drive visuals.
        private bool _jsaaLanding;
        private float _jsaaApproach;
        private readonly RaycastHit[] _jsaaGroundHits = new RaycastHit[16];
        private int _token;
        private Vector3[] _b = new Vector3[10];
        private Vector3 _r, _o;
        private readonly float[] _l = new float[8];
        private float _spin;
        private Vector3 _wd, _sd;
        private float _ww, _sw, _tw, _tg, _env, _hr, _hw, _lr, _lw, _flh, _frh;
        private bool _highKnee;
        private Vector3 _hd, _ld, _ep;
        private float _ew;
        private bool _em;
        // v0.25.15 legs: Unity humanoid muscles (HumanPoseHandler), applied on the animator's real pose.
        private static readonly HumanBodyBones[] LegBones =
        {
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot
        };
        private static int[] _legMuscles;
        private HumanPoseHandler _hph;
        private HumanPose _hp;
        private bool _legsBroken, _legHas;
        private readonly Quaternion[] _legAnim = new Quaternion[6];
        private readonly Quaternion[] _legWritten = new Quaternion[6];
        private readonly Quaternion[] _animPose = new Quaternion[10];
        private readonly Quaternion[] _written = new Quaternion[10];
        private readonly bool[] _hasWritten = new bool[10];

        private string _va;
        private float _vaAt, _vaRepeat;
        private bool _vaTrack;
        private int _vaLayer = -1, _vaHash;
        private int[] _vaPre;
        private string _vaTrigger;
        // v0.25.77 (user: repeated skill swings skipped their animation): the state each vanilla trigger enters is
        // learned (layer, hash). Firing the same trigger while that state is still playing restarts it with a
        // short cross-fade (the NACC method) instead of a trigger the animator would swallow.
        private static readonly Dictionary<string, int[]> _vaLearned = new Dictionary<string, int[]>();
        private float _vaFiredAt, _vaGuess;
        private bool _noAim, _noPlant, _noTrack, _quietLeft, _qlCaptured;
        private string _vaFiredName;
        private readonly Quaternion[] _qlPose = new Quaternion[4];
        private static readonly HumanBodyBones[] QlBones = { HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand };
        private Rigidbody _body;
        private float _vaLead = 0.3f;

        private string _bowTrig, _bowBool;
        private bool _bowFired;
        private bool _constSpeed;
        private float _constK;
        private float _constWindup;

        private void ReleaseBow(bool fire)
        {
            if (_bowBool == null && _bowTrig == null) return;
            Player p = _owner as Player;
            if (p == null) p = GetComponent<Player>();
            if (_bowBool != null) DragonCombat.SetBowAim(p, _bowBool, false);
            if (fire && _bowTrig != null) DragonCombat.FireVanilla(p, _bowTrig);
            _bowBool = null; _bowTrig = null;
        }

        public void Begin(DragonClipKey[] keys, float windup, bool hold, Transform visual, string clipName = null)
        {
            ReleaseBow(false);
            _jsaaLanding = clipName == "olympic_hero" || clipName == "olympic_hero_brutal";
            _highKnee = clipName == "merc_stomp";   // v0.25.101 vertical-shin knee raise (Stomp only)
            _jsaaApproach = 0f;
            _bowTrig = keys[0].BowTrig;
            _bowBool = keys[0].BowBool;
            _bowFired = false;
            if (_bowBool != null) DragonCombat.SetBowAim(GetComponent<Player>(), _bowBool, true);
            _va = keys[0].VA;
            _vaRepeat = keys[0].VR;
            _vaAt = Time.time + Mathf.Max(0f, windup - keys[0].VL);
            if (_va != null && _vaRepeat <= 0.05f) _vaAt = Time.time + Mathf.Max(0.1f, windup) * Mathf.Clamp01(keys[0].VF);
            _vaGuess = Mathf.Max(0.2f, keys[0].VL * 1.8f);
            _vaTrack = false;
            _noAim = keys[0].NoAim;
            _noPlant = keys[0].NoPlant;
            _noTrack = keys[0].NoTrack;
            _constSpeed = keys[0].ConstSpeed;
            _constK = 0f;
            _constWindup = windup;
            _quietLeft = keys[0].QL;
            _qlCaptured = false;
            _vaLead = Mathf.Max(0.05f, keys[0].VL);
            if (_keys != null)
            {
                // v0.25.8: chained clips start from the CURRENT pose (no snap back to rest in between).
                DragonClipKey from = CurrentPose(keys[0].T);
                ReleaseRoot();
                DragonClipKey[] copy = new DragonClipKey[keys.Length];
                Array.Copy(keys, copy, keys.Length);
                copy[0] = from;
                keys = copy;
            }
            _keys = keys;
            _windup = Mathf.Max(0.1f, windup);   // v0.25.19: 0.1 s minimum blend-in (0.05 popped like a ragdoll)
            _hold = hold;
            _start = Time.time;
            _impactAt = -1f;
            _holdLimit = Time.time + 60f;          // safety only: holds end on their gameplay event
            _visual = visual;
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _token = DragonCombat.ClaimMotionRoot(_visual);
        }

        public bool IsHolding { get { return _keys != null && _hold && _impactAt < 0f; } }
        public bool IsPlaying { get { return _keys != null; } }
        // v0.25.80 one skill at a time: busy through the wind up / hold and a short beat after the impact.
        public bool IsBusy { get { if (_keys == null) return false; return Phase() < 0.25f; } }

        public void Impact()
        {
            if (_keys == null) return;
            _hold = false;
            if (_impactAt < 0f) _impactAt = Time.time;
        }

        // Current pose as a key; root angles folded to -180..180 so a finished spin never unwinds.
        private DragonClipKey CurrentPose(float t)
        {
            DragonClipKey k = new DragonClipKey(t);
            for (int i = 0; i < 10; i++) k.B[i] = _b[i];
            k.R = new Vector3(Mathf.DeltaAngle(0f, _r.x), Mathf.DeltaAngle(0f, _r.y), Mathf.DeltaAngle(0f, _r.z));
            k.O = _o;
            for (int i = 0; i < 8; i++) k.L[i] = _l[i];
            k.Spin = Mathf.DeltaAngle(0f, _spin);
            return k;
        }

        // Blend from the current pose back to rest (charge ended without its finisher).
        public void Stop(float blend)
        {
            if (_keys == null) return;
            DragonClipKey from = CurrentPose(0f);
            _keys = new DragonClipKey[] { from, new DragonClipKey(Mathf.Max(0.05f, blend)) };
            _hold = false;
            _start = Time.time - _windup;
            _impactAt = Time.time;
        }

        private float Phase()
        {
            if (_impactAt >= 0f) return Time.time - _impactAt;
            float p = -1f + (Time.time - _start) / _windup;
            if (p < 0f) return p;
            if (_hold && Time.time < _holdLimit) return 0f;
            _impactAt = _start + _windup;
            return Time.time - _impactAt;
        }

        private void Sample(float t)
        {
            DragonClipKey[] k = _keys;
            int n = k.Length;
            _env = 1f;
            if (n > 1 && t < k[1].T) _env = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(k[0].T, k[1].T, t));
            if (n > 2 && t > k[n - 2].T) _env = Mathf.Min(_env, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(k[n - 2].T, k[n - 1].T, t)));
            if (t <= k[0].T) { Set(k[0], k[0], 0f); return; }
            if (t >= k[n - 1].T) { Set(k[n - 1], k[n - 1], 0f); return; }
            for (int i = 0; i < n - 1; i++)
            {
                if (t >= k[i].T && t <= k[i + 1].T)
                {
                    float w = (t - k[i].T) / Mathf.Max(0.0001f, k[i + 1].T - k[i].T);
                    if (k[i + 1].Lin) { Set(k[i], k[i + 1], w); return; }
                    SetSmooth(i, w);
                    return;
                }
            }
        }

        // v0.25.19 POLISH: monotone cubic (Fritsch-Carlson) through the keys instead of an ease-in/ease-out
        // per segment: the motion flows through the poses (no robotic stop at every key) and never
        // overshoots a hold or an extreme (no wobble / ragdoll look).
        private static float Slope(DragonClipKey[] k, int i, int c, Func<DragonClipKey, int, float> v)
        {
            int n = k.Length;
            if (i <= 0 || i >= n - 1 || k[i].Lin || k[i + 1].Lin) return 0f;
            float d0 = (v(k[i], c) - v(k[i - 1], c)) / Mathf.Max(0.0001f, k[i].T - k[i - 1].T);
            float d1 = (v(k[i + 1], c) - v(k[i], c)) / Mathf.Max(0.0001f, k[i + 1].T - k[i].T);
            if (d0 * d1 <= 0f) return 0f;
            float m = (v(k[i + 1], c) - v(k[i - 1], c)) / Mathf.Max(0.0001f, k[i + 1].T - k[i - 1].T);
            float lim = 3f * Mathf.Min(Mathf.Abs(d0), Mathf.Abs(d1));
            return Mathf.Clamp(m, -lim, lim);
        }

        private static float Cubic(DragonClipKey[] k, int i, float w, int c, Func<DragonClipKey, int, float> v)
        {
            float dt = k[i + 1].T - k[i].T;
            float p0 = v(k[i], c), p1 = v(k[i + 1], c);
            float m0 = Slope(k, i, c, v) * dt, m1 = Slope(k, i + 1, c, v) * dt;
            float w2 = w * w, w3 = w2 * w;
            return (2f * w3 - 3f * w2 + 1f) * p0 + (w3 - 2f * w2 + w) * m0 + (-2f * w3 + 3f * w2) * p1 + (w3 - w2) * m1;
        }

        private static readonly Func<DragonClipKey, int, float> VBone = delegate(DragonClipKey key, int c) { return key.B[c / 3][c % 3]; };
        private static readonly Func<DragonClipKey, int, float> VRoot = delegate(DragonClipKey key, int c) { return c < 3 ? key.R[c] : key.O[c - 3]; };
        private static readonly Func<DragonClipKey, int, float> VLeg = delegate(DragonClipKey key, int c) { return c < 8 ? key.L[c] : key.Spin; };

        private void SetItems(DragonClipKey a, DragonClipKey b, float w)
        {
            _ww = Mathf.Lerp(a.WW, b.WW, w);
            _sw = Mathf.Lerp(a.SW, b.SW, w);
            _tw = Mathf.Lerp(a.TW, b.TW, w);
            _hw = Mathf.Lerp(a.HW, b.HW, w);
            Vector3 ha = a.HW > 0f ? a.HD : b.HD, hb = b.HW > 0f ? b.HD : a.HD;
            _hd = Vector3.Slerp(ha.normalized, hb.normalized, w);
            _hr = a.HW > 0f && b.HW > 0f ? Mathf.Lerp(a.HR, b.HR, w) : (a.HW > 0f ? a.HR : b.HR);
            _lw = Mathf.Lerp(a.LW, b.LW, w);
            _ew = Mathf.Lerp(a.EW, b.EW, w);
            _ep = a.EW > 0f ? a.EP : b.EP;
            _em = a.EW > 0f ? a.EM : b.EM;
            _flh = Mathf.Lerp(a.FLh, b.FLh, w);
            _frh = Mathf.Lerp(a.FRh, b.FRh, w);
            Vector3 la = a.LW > 0f ? a.LD : b.LD, lb = b.LW > 0f ? b.LD : a.LD;
            _ld = Vector3.Slerp(la.normalized, lb.normalized, w);
            _lr = a.LW > 0f && b.LW > 0f ? Mathf.Lerp(a.LR, b.LR, w) : (a.LW > 0f ? a.LR : b.LR);
            _tg = a.TW > 0f && b.TW > 0f ? Mathf.Lerp(a.TG, b.TG, w) : (a.TW > 0f ? a.TG : b.TG);
            _wd = Vector3.Slerp(a.WW > 0f ? a.WD : b.WD, b.WW > 0f ? b.WD : a.WD, w);
            _sd = Vector3.Slerp(a.SW > 0f ? a.SD : b.SD, b.SW > 0f ? b.SD : a.SD, w);
        }

        private void SetSmooth(int i, float w)
        {
            DragonClipKey[] k = _keys;
            for (int b = 0; b < 10; b++)
                _b[b] = new Vector3(Cubic(k, i, w, b * 3, VBone), Cubic(k, i, w, b * 3 + 1, VBone), Cubic(k, i, w, b * 3 + 2, VBone));
            _r = new Vector3(Cubic(k, i, w, 0, VRoot), Cubic(k, i, w, 1, VRoot), Cubic(k, i, w, 2, VRoot));
            _o = new Vector3(Cubic(k, i, w, 3, VRoot), Cubic(k, i, w, 4, VRoot), Cubic(k, i, w, 5, VRoot));
            for (int l = 0; l < 8; l++) _l[l] = Cubic(k, i, w, l, VLeg);
            _spin = Cubic(k, i, w, 8, VLeg);
            SetItems(k[i], k[i + 1], Mathf.SmoothStep(0f, 1f, w));
        }

        private void Set(DragonClipKey a, DragonClipKey b, float w)
        {
            for (int i = 0; i < 10; i++) _b[i] = Vector3.Lerp(a.B[i], b.B[i], w);
            _r = Vector3.Lerp(a.R, b.R, w);
            _o = Vector3.Lerp(a.O, b.O, w);
            for (int i = 0; i < 8; i++) _l[i] = Mathf.Lerp(a.L[i], b.L[i], w);
            _spin = Mathf.Lerp(a.Spin, b.Spin, w);
            SetItems(a, b, w);
        }

        // v0.25.30 user: the vanilla animation must stop when the wind up stops. Remember every layer's state,
        // find the layer that switched after the trigger, then drive the animator speed each frame so the
        // remaining part of that state ends exactly at the impact (closed loop: slow wind ups play slower,
        // short ones faster, 0.3x - 6x). After the impact anything left is played out at full 6x.
        private void StartVanillaTracking()
        {
            _vaTrack = false;
            if (_animator == null) return;
            try
            {
                int n = _animator.layerCount;
                _vaPre = new int[n];
                for (int l = 0; l < n; l++) _vaPre[l] = _animator.GetCurrentAnimatorStateInfo(l).fullPathHash;
                _vaLayer = -1;
                _vaFiredAt = Time.time;
                _vaTrack = true;
            }
            catch (Exception) { }
        }

        private bool RestartVanilla(string trigger)
        {
            int[] lv;
            if (_animator == null || trigger.StartsWith("emote", StringComparison.Ordinal) || !_vaLearned.TryGetValue(trigger, out lv)) return false;
            try
            {
                if (lv[0] >= _animator.layerCount) return false;
                AnimatorStateInfo cs = _animator.GetCurrentAnimatorStateInfo(lv[0]);
                bool inIt = (cs.fullPathHash == lv[1] && !cs.loop)   // v0.25.86: also when finishing (Ascended Greatblade 2nd slam)
                    || (_animator.IsInTransition(lv[0]) && _animator.GetNextAnimatorStateInfo(lv[0]).fullPathHash == lv[1] && !_animator.GetNextAnimatorStateInfo(lv[0]).loop);
                // v0.25.85 ROOT CAUSE of lost skill swings: a looping state (run / idle) learned while moving made every
                // later cast cross-fade into run/idle instead of firing the swing. Only a non-looping swing restarts.
                if (!inIt) return false;
                Player p = _owner as Player;
                if (p != null) DragonCombat.BlockSkillAnimAttack(p, 2.5f);
                _animator.ResetTrigger(trigger);
                _animator.CrossFadeInFixedTime(lv[1], 0.08f, lv[0], 0f);
                if (_vaTrack) { _vaLayer = lv[0]; _vaHash = lv[1]; _vaFiredAt = Time.time; _constK = 0f; }
                return true;
            }
            catch (Exception) { return false; }
        }

        private void TrackVanillaSpeed()
        {
            Player p = _owner as Player;
            if (p == null || _animator == null || _vaPre == null) { _vaTrack = false; return; }
            // v0.25.86: a skill can hold the vanilla swing (jump slams falling further than planned)
            if (Time.time < DragonCombat.VanillaPauseUntil) { DragonCombat.SetSkillAnimSpeed(p, 0.02f, 0.12f); return; }
            try
            {
                float end = _impactAt >= 0f ? _impactAt : _start + _windup;
                float left = Mathf.Max(0.08f, end - Time.time);
                float remain;
                if (_vaLayer < 0)
                {
                    for (int l = 0; l < _vaPre.Length && _vaLayer < 0; l++)
                    {
                        AnimatorStateInfo st = _animator.IsInTransition(l) ? _animator.GetNextAnimatorStateInfo(l) : _animator.GetCurrentAnimatorStateInfo(l);
                        if (st.fullPathHash != _vaPre[l] && !st.loop) { _vaLayer = l; _vaHash = st.fullPathHash; if (_vaTrigger != null) _vaLearned[_vaTrigger] = new int[] { l, _vaHash }; }   // v0.25.85 never a looping (locomotion) state
                    }
                    if (_vaLayer < 0)
                    {
                        if (Time.time - _vaFiredAt > 0.5f) { _vaTrack = false; return; }
                        remain = Mathf.Max(0f, _vaGuess - (Time.time - _vaFiredAt));
                        DragonCombat.SetSkillAnimSpeed(p, Mathf.Clamp(remain / left, 0.3f, 6f), 0.15f);
                        return;
                    }
                }
                AnimatorStateInfo cur = _animator.GetCurrentAnimatorStateInfo(_vaLayer);
                AnimatorStateInfo info;
                if (cur.fullPathHash == _vaHash) info = cur;
                else if (_animator.IsInTransition(_vaLayer) && _animator.GetNextAnimatorStateInfo(_vaLayer).fullPathHash == _vaHash) info = _animator.GetNextAnimatorStateInfo(_vaLayer);
                else { _vaTrack = false; DragonCombat.SetSkillAnimSpeed(p, 1f, 0f); return; }
                float norm = info.normalizedTime;
                if (norm >= 1f) { _vaTrack = false; DragonCombat.SetSkillAnimSpeed(p, 1f, 0f); return; }
                if (_constSpeed)
                {
                    if (_constK <= 0f) _constK = Mathf.Clamp(_vaLead / Mathf.Max(0.05f, _constWindup - 0.08f), 1f, 4f); // v0.25.94 (user) the swing's hit frame lands ~0.08 s BEFORE the GTs release: animation first, then the waves // v0.25.91 one stable speed per swing so the vanilla contact frame lands on the skill hit (never slower than native).
                    DragonCombat.SetSkillAnimSpeed(p, _constK, 0.15f);
                    return;
                }
                // v0.25.35 two phases (user: slow wind up, then a fast swing timed to the skill): the vanilla
                // anticipation (everything up to ~0.22 s before its hit frame) is stretched over the wind up, the
                // swing itself plays fast in the last ~0.25 s so its hit frame lands exactly on the impact;
                // the recovery after the impact is played out at 6x.
                float len = Mathf.Max(0.05f, info.length);
                float hitN = Mathf.Clamp(_vaLead / len, 0.15f, 0.9f);
                float swingN = Mathf.Clamp((_vaLead - 0.22f) / len, 0f, hitN);
                float swingDur = Mathf.Min(0.25f, Mathf.Max(0.04f, (end - _vaFiredAt) * 0.45f));
                float swingAt = end - swingDur;
                float speed;
                if (Time.time > end + 0.02f) speed = 6f;
                else if (Time.time < swingAt) speed = Mathf.Clamp((swingN - norm) * len / Mathf.Max(0.03f, swingAt - Time.time), 0.12f, 6f);
                else speed = Mathf.Clamp((hitN - norm) * len / Mathf.Max(0.03f, end - Time.time), 0.12f, 6f);
                DragonCombat.SetSkillAnimSpeed(p, speed, 0.15f);
            }
            catch (Exception) { _vaTrack = false; }
        }

        private void LateUpdate()
        {
            if (_keys == null || _keys.Length == 0) { Destroy(this); return; }
            if (_owner == null) _owner = GetComponent<Character>();
            float t = Phase();
            // v0.25.49 (user: gliding with the arm still raised): our emotes are fired as raw triggers, so
            // Valheim never cancels them when you walk. A raise ends the moment you move after the lock.
            bool movedOut = false;
            if (_vaFiredName == "emote_cheer" && _owner is Player && !DragonCombat.IsSkillLocked((Player)_owner))
            {
                Rigidbody orb = _owner.GetComponent<Rigidbody>();
                if (orb != null && new Vector3(orb.velocity.x, 0f, orb.velocity.z).magnitude > 0.8f) movedOut = true;
            }
            if (movedOut || t > _keys[_keys.Length - 1].T || (_owner != null && _owner.IsDead()))
            {
                // v0.25.41: kneel loops in Valheim - stand back up when the skill's clip is over.
                // v0.25.49: every emote a skill fired is stopped when its clip ends.
                if (_vaFiredName != null && _vaFiredName.StartsWith("emote", StringComparison.Ordinal)) DragonCombat.StopEmote(_owner as Player);
                ReleaseRoot();
                _keys = null;
                Destroy(this);
                return;
            }
            Sample(t);
            // The original JSAA did not enter its kneel until AFTER the physical landing.
            // Blend toward touchdown while the character is descending near solid terrain;
            // on the contact frame the complete pose is already in place.
            if (_jsaaLanding && _keys.Length > 6)
            {
                if (_impactAt >= 0f && t <= _keys[6].T)
                    Set(_keys[5], _keys[6], 1f);
                else if (_impactAt < 0f && _hold)
                {
                    float approach = JsaLandingApproach();
                    if (approach > 0.001f) Set(_keys[5], _keys[6], approach);
                }
            }
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (!_bowFired && (_bowTrig != null || _bowBool != null) && t >= -0.02f) { _bowFired = true; ReleaseBow(true); }
            if (_va != null && Time.time >= _vaAt)
            {
                _vaTrigger = _va;
                if (_vaRepeat <= 0.05f && !_noTrack) StartVanillaTracking();
                if (!RestartVanilla(_va)) DragonCombat.FireVanilla(_owner as Player, _va);
                _vaFiredName = _va;
                if (_vaRepeat > 0.05f) _vaAt += _vaRepeat; else _va = null;
            }
            if (_vaTrack) TrackVanillaSpeed();
            if (_animator != null && _animator.isHuman)
            {
                // v0.25.14: the animator does not rewrite every bone every frame (physics-rate / culled
                // updates). A bone still holding what we wrote was skipped: put the stored animator pose
                // back first so offsets never stack (arms/torso here, legs in ApplyLegs).
                Transform[] bones = new Transform[10];
                for (int i = 0; i < 10; i++)
                {
                    Transform bone = _animator.GetBoneTransform(Bones[i]);
                    bones[i] = bone;
                    if (bone == null) continue;
                    Quaternion cur = bone.localRotation;
                    if (_hasWritten[i] && Quaternion.Angle(cur, _written[i]) < 0.01f) bone.localRotation = _animPose[i];
                    else _animPose[i] = cur;
                }
                CaptureFeet();
                for (int i = 0; i < 10; i++)
                {
                    Transform bone = bones[i];
                    if (bone == null) continue;
                    Quaternion basePose = _animPose[i];
                    Quaternion w = _b[i] == Vector3.zero ? basePose : basePose * Quaternion.Euler(_b[i]);
                    bone.localRotation = w;
                    _written[i] = w;
                    _hasWritten[i] = true;
                }
            }
            if (_visual != null && DragonCombat.OwnsMotionRoot(_token))
            {
                Quaternion q = Quaternion.Euler(_r) * Quaternion.AngleAxis(_spin, Vector3.up);
                _visual.localRotation = DragonCombat.MotionBaseRot * q;
                _visual.localPosition = DragonCombat.MotionBasePos + (Pivot - q * Pivot) + _o;
            }
            if (_animator != null && _animator.isHuman)
            {
                PlantFeet();
                PlaceMainHand();
                PlaceHand(false, _ld, _lr, _lw);
                AimHeldItems();
                QuietLeftArm();
            }
        }

        // v0.25.88 JSAA ONLY: physically anticipate the superhero kneel in the final
        // ~1.4 metres of descent. Do not emit impacts or move the character rigidbody.
        // RaycastNonAlloc ignores our own capsule, creatures, walls and triggers; a
        // drop off a cliff continues to hold the airborne guard until real terrain.
        private float JsaLandingApproach()
        {
            if (_body == null && _owner != null) _body = _owner.GetComponent<Rigidbody>();
            if (_body == null || _owner == null || _body.velocity.y > -0.25f)
            {
                _jsaaApproach = Mathf.MoveTowards(_jsaaApproach, 0f, Time.deltaTime * 12f);
                return _jsaaApproach;
            }
            float groundGap = float.MaxValue;
            Vector3 origin = _owner.transform.position + Vector3.up * 0.65f;
            int n = Physics.RaycastNonAlloc(origin, Vector3.down, _jsaaGroundHits, 2.5f,
                ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _jsaaGroundHits[i];
                if (h.collider == null || h.normal.y < 0.55f) continue;
                if (h.collider.GetComponentInParent<Character>() != null) continue;
                float gap = _owner.transform.position.y - h.point.y;
                if (gap >= -0.15f && gap < groundGap) groundGap = gap;
            }
            float aim = groundGap < float.MaxValue
                ? Mathf.SmoothStep(0f, 1f, 1f - Mathf.InverseLerp(0.12f, 1.45f, groundGap))
                : 0f;
            _jsaaApproach = Mathf.MoveTowards(_jsaaApproach, aim, Time.deltaTime * 14f);
            return _jsaaApproach;
        }

        // v0.25.40: freeze the off-hand arm (shoulder to hand) to its pose at the start of the skill.
        private void QuietLeftArm()
        {
            if (!_quietLeft || _animator == null) return;
            for (int i = 0; i < QlBones.Length; i++)
            {
                Transform b = _animator.GetBoneTransform(QlBones[i]);
                if (b == null) continue;
                if (!_qlCaptured) _qlPose[i] = b.localRotation;
                else b.localRotation = _qlPose[i];
            }
            _qlCaptured = true;
        }

        // ---------------------------------------------------------------- v0.25.22 FOOT PLANTING
        // (user: legs floating / flailing) The old muscle legs folded the legs while the body only crouched a
        // little, so both feet left the ground. Now the feet stay where Valheim's own animation puts them (they
        // follow the body's YAW only, so turns pivot the feet), the crouch / lean / lunge of the clip is taken by
        // the knees through two-bone leg IK, and a clip's stance (LL / RL lift = step forward/back, spread = out)
        // moves the planted feet. Airborne = legs untouched (vanilla jump / fall).
        private static readonly HumanBodyBones[] LegChain =
        {
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot
        };
        private readonly Transform[] _leg = new Transform[6];
        private readonly Vector3[] _footLocal = new Vector3[2];
        private readonly Quaternion[] _footLocalRot = new Quaternion[2];
        private bool _feetCaptured;
        private float _plantW;
        private static MethodInfo _onGroundMethod;

        private void CaptureFeet()
        {
            _feetCaptured = false;
            Transform frame = _visual;
            if (frame == null) return;
            for (int i = 0; i < 6; i++)
            {
                _leg[i] = _animator.GetBoneTransform(LegChain[i]);
                if (_leg[i] == null) return;
                Quaternion cur = _leg[i].localRotation;
                if (_legHas && Quaternion.Angle(cur, _legWritten[i]) < 0.01f) _leg[i].localRotation = _legAnim[i];
                else _legAnim[i] = cur;
            }
            _legHas = false;
            _footLocal[0] = frame.InverseTransformPoint(_leg[2].position);
            _footLocal[1] = frame.InverseTransformPoint(_leg[5].position);
            _footLocalRot[0] = Quaternion.Inverse(frame.rotation) * _leg[2].rotation;
            _footLocalRot[1] = Quaternion.Inverse(frame.rotation) * _leg[5].rotation;
            _feetCaptured = true;
        }

        private bool Grounded()
        {
            if (_owner == null) return false;
            try
            {
                if (_onGroundMethod == null) _onGroundMethod = typeof(Character).GetMethod("IsOnGround", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return _onGroundMethod != null && (bool)_onGroundMethod.Invoke(_owner, null);
            }
            catch (Exception) { return false; }
        }

        // v0.25.26 ANATOMY (user: "she is not a contortionist"). A human arm reaching across the body goes IN FRONT
        // of the chest, never behind it: a target that crosses the midline is pushed forward. Elbows hang down and
        // a little out; when the hand rises the elbow points forward-out; across the body it points down-forward.
        private static Vector3 ArmDir(Vector3 dir, bool right)
        {
            Vector3 d = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            float side = right ? 1f : -1f;
            if (d.x * side < 0f) d.z = Mathf.Max(d.z, 0.6f * Mathf.Abs(d.x) + 0.25f);
            return d.normalized;
        }

        private static Vector3 ElbowPole(Vector3 d, bool right)
        {
            float side = right ? 1f : -1f;
            float up = Mathf.Clamp01(d.y);
            if (d.x * side < 0f) return new Vector3(0.15f * side, -1f, 0.35f);   // across the chest: elbow down-forward
            if (d.y > 0.4f) return new Vector3(0.6f * side, 0.2f, 0.45f);          // raised / throwing: elbow up-forward-out
            if (d.z < -0.1f) return new Vector3(0.4f * side, -1f, -0.6f);         // hand pulled back (ribs, bowstring): elbow back
            return new Vector3(0.45f * side, -1f + 1.1f * up, 0.15f + 0.3f * up);
        }

        private Vector3 PoleFor(Vector3 d, bool right)
        {
            Vector3 p = ElbowPole(d, right);
            if (_ew <= 0.01f || (!right && _em)) return p;
            Vector3 e = _ep; if (!right) e.x = -e.x;
            return Vector3.Slerp(p.normalized, e.normalized, Mathf.Clamp01(_ew));
        }

        private void PlaceHand(bool right, Vector3 dir, float reach, float weight)
        {
            float w = Mathf.Clamp01(weight);
            if (w <= 0.001f || dir.sqrMagnitude < 0.0001f || (!right && _tw > 0.01f)) return;
            try
            {
                Transform ua = _animator.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
                Transform la = _animator.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
                Transform hand = _animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
                if (ua == null || la == null || hand == null) return;
                Transform frame = _visual != null ? _visual : transform;
                float len = (la.position - ua.position).magnitude + (hand.position - la.position).magnitude;
                Vector3 d = ArmDir(dir, right);
                Vector3 target = ua.position + frame.rotation * d * (len * Mathf.Clamp(reach, 0.25f, 0.999f));
                Vector3 pole = frame.rotation * PoleFor(d, right);
                TwoBoneIK(ua, la, hand, target, pole, w);
                int o = right ? 4 : 7;
                _written[o] = ua.localRotation; _hasWritten[o] = true;
                _written[o + 1] = la.localRotation; _hasWritten[o + 1] = true;
                _written[o + 2] = hand.localRotation; _hasWritten[o + 2] = true;
            }
            catch (Exception) { }
        }

        // Main hand to its storyboard target (arm IK from the right shoulder; elbow hangs down / out / back).
        private void PlaceMainHand()
        {
            float w = Mathf.Clamp01(_hw);
            if (w <= 0.001f || _hd.sqrMagnitude < 0.0001f) return;
            try
            {
                Transform ua = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                Transform la = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                Transform hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (ua == null || la == null || hand == null) return;
                Transform frame = _visual != null ? _visual : transform;
                float len = (la.position - ua.position).magnitude + (hand.position - la.position).magnitude;
                Vector3 d = ArmDir(_hd, true);
                Vector3 target = ua.position + frame.rotation * d * (len * Mathf.Clamp(_hr, 0.25f, 0.999f));
                Vector3 pole = frame.rotation * PoleFor(d, true);
                TwoBoneIK(ua, la, hand, target, pole, w);
                _written[4] = ua.localRotation; _hasWritten[4] = true;
                _written[5] = la.localRotation; _hasWritten[5] = true;
                _written[6] = hand.localRotation; _hasWritten[6] = true;
            }
            catch (Exception) { }
        }

        private void PlantFeet()
        {
            // v0.25.38 (user): casting while running keeps the run - no foot planting while the body really
            // moves (planted feet on a moving body looked like gliding).
            if (_body == null && _owner != null) _body = _owner.GetComponent<Rigidbody>();
            bool moving = false;
            if (_body != null) { Vector3 hv = _body.velocity; hv.y = 0f; moving = hv.magnitude > 1.2f; }
            // A jump slam can retain horizontal momentum for a moment after its
            // terrain hit. Plant its touchdown feet even during that brief slide,
            // otherwise it looks like the legs stay standing under the kneel.
            bool jsaaContact = _jsaaLanding && _impactAt >= 0f && Time.time - _impactAt < 0.45f;
            bool can = !_noPlant && (!moving || jsaaContact) && _feetCaptured && _visual != null && DragonCombat.OwnsMotionRoot(_token) && Grounded();
            float tilt = Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(0f, _r.x)), Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(0f, _r.z)), Mathf.Abs(Mathf.DeltaAngle(0f, _spin))));
            float upright = 1f - Mathf.InverseLerp(25f, 45f, tilt);
            // JSAA touches down already kneeling. Don't spend another 0.125s slowly
            // activating the planted legs AFTER the impact (other clips unchanged).
            if (_jsaaLanding && _impactAt >= 0f && can && upright > 0.95f)
                _plantW = _env;
            else
                _plantW = Mathf.MoveTowards(_plantW, can ? _env * upright : 0f, Time.deltaTime * 8f);
            if (!_feetCaptured || _plantW <= 0.001f || _visual == null) return;
            try
            {
                Transform parent = _visual.parent;
                Quaternion qy = Quaternion.AngleAxis(_r.y + _spin, Vector3.up);
                Quaternion baseRot = DragonCombat.MotionBaseRot;
                Vector3 basePos = DragonCombat.MotionBasePos + (Pivot - qy * Pivot) + new Vector3(_o.x, 0f, _o.z);
                Quaternion parentRot = parent != null ? parent.rotation : Quaternion.identity;
                Vector3 pole = parentRot * baseRot * qy * Vector3.forward;
                for (int f = 0; f < 2; f++)
                {
                    Transform a = _leg[f * 3], b = _leg[f * 3 + 1], c = _leg[f * 3 + 2];
                    float lift = _l[f * 4], spread = _l[f * 4 + 1];
                    Vector3 step = new Vector3((f == 0 ? -1f : 1f) * spread * 0.5f, 0f, lift * 0.45f);
                    Vector3 local = basePos + baseRot * (qy * (Vector3.Scale(_visual.localScale, _footLocal[f]) + step));
                    Vector3 target = parent != null ? parent.TransformPoint(local) : local;
                    float liftH = Mathf.Max(0f, f == 0 ? _flh : _frh);
                    // v0.25.101 (user): a raised knee = high-knee pose with the SHIN VERTICAL under the knee (no quad
                    // stretch). Lift 0..0.6 maps to thigh flexion 0..90 deg (clamped to 95 = realistic hip limit, knee
                    // flexion = the same angle), the foot sits straight below the knee, never under the ground, and
                    // keeps the vanilla foot's side offset so the thigh never crosses into the torso.
                    if (liftH > 0.01f && _highKnee && !_jsaaLanding)
                    {
                        Vector3 hip = a.position;
                        float thigh = (b.position - a.position).magnitude, shin = (c.position - b.position).magnitude;
                        Vector3 fwd = pole; fwd.y = 0f; fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
                        Vector3 sideOff = target - hip; sideOff -= fwd * Vector3.Dot(sideOff, fwd); sideOff.y = 0f;
                        float sideMax = thigh * 0.35f;
                        if (sideOff.magnitude > sideMax) sideOff = sideOff.normalized * sideMax;
                        float flex = Mathf.Clamp(Mathf.Clamp01(liftH / 0.6f) * 90f, 0f, 95f) * Mathf.Deg2Rad;
                        Vector3 knee = hip + sideOff + (Vector3.down * Mathf.Cos(flex) + fwd * Mathf.Sin(flex)) * thigh;
                        Vector3 foot = knee + Vector3.down * shin;
                        foot.y = Mathf.Max(foot.y, target.y);
                        target = foot;
                    }
                    else target += Vector3.up * liftH;
                    Quaternion footRot = parentRot * baseRot * qy * _footLocalRot[f];
                    // With the right foot trailing behind, the right knee has to
                    // fold DOWN toward the floor, not forward like a standing squat.
                    // Left knee bends UP over the forward-planted foot.
                    Vector3 kneePole = pole;
                    if (_jsaaLanding && _impactAt >= 0f)
                    {
                        Vector3 localPole = f == 0
                            ? new Vector3(-0.06f, 0.25f, 1f)
                            : new Vector3(0.08f, -1f, 0.34f);
                        kneePole = (parentRot * baseRot * qy * localPole).normalized;
                    }
                    TwoBoneIK(a, b, c, target, kneePole, _plantW);
                    c.rotation = Quaternion.Slerp(c.rotation, footRot, _plantW);
                }
                for (int i = 0; i < 6; i++) _legWritten[i] = _leg[i].localRotation;
                _legHas = true;
            }
            catch (Exception) { }
        }

        // Analytic two-bone IK (upper, lower, end) toward t; bends in the current plane, or toward `pole`.
        // v0.25.25 positional two-bone IK (replaces the angle solver, which twisted the limb when it started
        // nearly straight - e.g. Heavy Slash's hand never reached the left side). The middle joint is placed
        // exactly (toward `pole`), the upper bone is aimed at it, the lower bone at the target, then the result is
        // blended by w. Always lands the end on the target when it is in reach.
        private static void TwoBoneIK(Transform ua, Transform la, Transform end, Vector3 t, Vector3 pole, float w)
        {
            if (w <= 0.001f) return;
            Vector3 a = ua.position, b = la.position, c = end.position;
            float lab = (b - a).magnitude, lcb = (c - b).magnitude;
            if (lab < 0.0001f || lcb < 0.0001f) return;
            Vector3 toT = t - a;
            float d = Mathf.Clamp(toT.magnitude, Mathf.Abs(lab - lcb) + 0.001f, lab + lcb - 0.001f);
            Vector3 dir = toT.sqrMagnitude > 0.000001f ? toT.normalized : (c - a).normalized;
            float x = (lab * lab - lcb * lcb + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, lab * lab - x * x));
            Vector3 side = pole - dir * Vector3.Dot(pole, dir);
            if (side.sqrMagnitude < 0.000001f) side = (b - a) - dir * Vector3.Dot(b - a, dir);
            if (side.sqrMagnitude < 0.000001f) side = Vector3.Cross(dir, Vector3.right);
            side.Normalize();
            Vector3 elbow = a + dir * x + side * h;
            Vector3 hand = a + dir * d;
            Quaternion ua0 = ua.localRotation, la0 = la.localRotation;
            ua.rotation = Quaternion.FromToRotation(b - a, elbow - a) * ua.rotation;
            Vector3 b1 = la.position, c1 = end.position;
            la.rotation = Quaternion.FromToRotation(c1 - b1, hand - b1) * la.rotation;
            if (w < 0.999f)
            {
                Quaternion ua1 = ua.localRotation, la1 = la.localRotation;
                ua.localRotation = Quaternion.Slerp(ua0, ua1, w);
                la.localRotation = Quaternion.Slerp(la0, la1, w);
            }
        }


        private void ReleaseRoot()
        {
            if (_visual != null) DragonCombat.ReleaseMotionRoot(_token, _visual);
        }

        // ---------------------------------------------------------------- v0.25.20 HELD ITEMS
        // Valheim attaches items rigidly to the hand at the vanilla grip angle, so a raised arm used to hold the
        // weapon sideways. Here the hand turns so each held item (weapon, tool, staff, shield, torch...) points
        // where the clip says (Wp / Oi). Without an explicit direction, an arm raised above the shoulder raises
        // its item along the forearm. The item's own length axis = hand -> centre of its renderers, measured
        // once per equipped instance (rig / item independent).
        private Component _vis;
        private static FieldInfo _visRight, _visLeft;
        private int _tipIdR, _tipIdL;
        private Vector3 _tipR, _tipL;
        private bool _shieldL;

        private GameObject HeldItem(bool right)
        {
            if (_vis == null)
            {
                Component[] cs = GetComponentsInChildren<Component>();
                for (int i = 0; i < cs.Length; i++) if (cs[i] != null && cs[i].GetType().Name == "VisEquipment") { _vis = cs[i]; break; }
                if (_vis == null) return null;
                BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                if (_visRight == null) _visRight = _vis.GetType().GetField("m_rightItemInstance", f);
                if (_visLeft == null) _visLeft = _vis.GetType().GetField("m_leftItemInstance", f);
            }
            FieldInfo fi = right ? _visRight : _visLeft;
            return fi == null ? null : fi.GetValue(_vis) as GameObject;
        }

        private static bool ItemTip(GameObject item, Transform hand, out Vector3 tipLocal)
        {
            tipLocal = Vector3.zero;
            Renderer[] rs = item.GetComponentsInChildren<Renderer>();
            bool any = false;
            Bounds b = new Bounds();
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null || rs[i] is ParticleSystemRenderer) continue;
                if (!any) { b = rs[i].bounds; any = true; } else b.Encapsulate(rs[i].bounds);
            }
            if (!any) return false;
            tipLocal = hand.InverseTransformPoint(b.center);
            return tipLocal.magnitude * hand.lossyScale.x > 0.08f;   // centred grips (bows) keep their vanilla angle
        }

        // v0.25.21 UNIVERSAL (user): the main-hand item points wherever the hand points (along the forearm) for
        // the whole clip unless a key states a direction; the off-hand item stays as Valheim holds it (only an
        // explicit Oi or a two-handed grip moves it).
        private void AimHeldItems()
        {
            AimHand(true, HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm, 6, _wd, _ww, _env);
            if (_sw > 0.01f) AimHand(false, HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm, 9, _sd, _sw, 0f);
            TwoHandGrip();
        }

        private bool _leftHidden;
        private readonly List<Renderer> _leftHiddenList = new List<Renderer>();

        private void SetLeftHidden(bool hide)
        {
            if (hide == _leftHidden) return;
            _leftHidden = hide;
            if (hide)
            {
                _leftHiddenList.Clear();
                GameObject item = HeldItem(false);
                if (item == null) return;
                Renderer[] rs = item.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < rs.Length; i++) if (rs[i] != null && rs[i].enabled) { rs[i].enabled = false; _leftHiddenList.Add(rs[i]); }
            }
            else
            {
                for (int i = 0; i < _leftHiddenList.Count; i++) if (_leftHiddenList[i] != null) _leftHiddenList[i].enabled = true;
                _leftHiddenList.Clear();
            }
        }

        // Off hand on the main weapon's grip: analytic two-bone IK (upper arm + forearm) keeping the arm's bend plane.
        private void TwoHandGrip()
        {
            float w = Mathf.Clamp01(_tw) * _env;
            SetLeftHidden(w > 0.3f);
            if (w <= 0.01f) return;
            try
            {
                Transform ua = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                Transform la = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                Transform lh = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
                Transform rh = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (ua == null || la == null || lh == null || rh == null) return;
                Vector3 axis = (_visual != null ? _visual : transform).forward;
                if (_tipR != Vector3.zero && HeldItem(true) != null) axis = (rh.TransformPoint(_tipR) - rh.position).normalized;
                Vector3 t = rh.position + axis * _tg * Mathf.Max(0.2f, transform.lossyScale.y);
                Transform fr = _visual != null ? _visual : transform;
                TwoBoneIK(ua, la, lh, t, fr.rotation * new Vector3(-0.2f, -1f, 0.35f), w);
                // the off hand wraps the grip like the main hand
                lh.rotation = Quaternion.Slerp(lh.rotation, rh.rotation, w * 0.8f);
                _written[7] = ua.localRotation; _hasWritten[7] = true;
                _written[8] = la.localRotation; _hasWritten[8] = true;
                _written[9] = lh.localRotation; _hasWritten[9] = true;
            }
            catch (Exception) { }
        }

        private void AimHand(bool right, HumanBodyBones handBone, HumanBodyBones foreBone, int slot, Vector3 dir, float weight, float raise)
        {
            try
            {
                Transform hand = _animator.GetBoneTransform(handBone);
                if (hand == null) return;
                GameObject item = HeldItem(right);
                if (item == null) return;
                int id = item.GetInstanceID();
                if ((right ? _tipIdR : _tipIdL) != id)
                {
                    Vector3 tip;
                    bool ok = ItemTip(item, hand, out tip);
                    if (right) { _tipIdR = id; _tipR = ok ? tip : Vector3.zero; }
                    else { _tipIdL = id; _tipL = ok ? tip : Vector3.zero; _shieldL = item.name.ToLowerInvariant().Contains("shield"); }
                }
                Vector3 tipLocal = right ? _tipR : _tipL;
                if (tipLocal == Vector3.zero) return;
                Transform frame = _visual != null ? _visual : transform;
                Vector3 want;
                float w;
                Transform fore = _animator.GetBoneTransform(foreBone);
                Vector3 along = fore != null ? (hand.position - fore.position).normalized : Vector3.zero;
                if (weight > 0.01f && dir.sqrMagnitude > 0.0001f)
                {
                    // explicit direction, blended over the forearm rule
                    Vector3 d = frame.TransformDirection(dir.normalized);
                    float k = Mathf.Clamp01(weight);
                    want = along == Vector3.zero ? d : Vector3.Slerp(along, d, k);
                    w = Mathf.Max(k, raise) * _env;
                }
                else
                {
                    if (!right && _shieldL) return;
                    w = _noAim ? 0f : raise;   // universal rule: the item continues the forearm (envelope = clip in/out)
                    if (w <= 0.001f || along == Vector3.zero) return;
                    want = along;
                }
                Vector3 cur = hand.TransformPoint(tipLocal) - hand.position;
                if (cur.sqrMagnitude < 0.000001f || want.sqrMagnitude < 0.000001f) return;
                Quaternion corr = Quaternion.FromToRotation(cur, want);
                hand.rotation = Quaternion.Slerp(Quaternion.identity, corr, w) * hand.rotation;
                _written[slot] = hand.localRotation;
                _hasWritten[slot] = true;
            }
            catch (Exception) { }
        }

        // Leg muscles on top of the animator pose. Only the six leg bones keep the result: the torso,
        // arms and hips are restored right after SetHumanPose, so nothing else is touched.
        private void ApplyLegs(Transform[] bones)
        {
            if (_legsBroken) return;
            Transform[] legs = new Transform[6];
            for (int i = 0; i < 6; i++)
            {
                legs[i] = _animator.GetBoneTransform(LegBones[i]);
                if (legs[i] == null) continue;
                Quaternion cur = legs[i].localRotation;
                if (_legHas && Quaternion.Angle(cur, _legWritten[i]) < 0.01f) legs[i].localRotation = _legAnim[i];
                else _legAnim[i] = cur;
            }
            float scale = DragonCombatPlugin.Instance != null ? DragonCombatPlugin.Instance.LegMotionScale.Value : 1f;
            bool any = false;
            for (int i = 0; i < 8; i++) if (Mathf.Abs(_l[i] * scale) > 0.001f) any = true;
            if (!any) { _legHas = false; return; }
            try
            {
                if (_legMuscles == null)
                {
                    string[] names = HumanTrait.MuscleName;
                    string[] want =
                    {
                        "Left Upper Leg Front-Back", "Left Upper Leg In-Out", "Left Lower Leg Stretch", "Left Foot Up-Down",
                        "Right Upper Leg Front-Back", "Right Upper Leg In-Out", "Right Lower Leg Stretch", "Right Foot Up-Down"
                    };
                    int[] idx = new int[8];
                    for (int i = 0; i < 8; i++) idx[i] = Array.IndexOf(names, want[i]);
                    _legMuscles = idx;
                }
                if (_hph == null)
                {
                    if (_animator.avatar == null || !_animator.avatar.isHuman) { _legsBroken = true; return; }
                    _hph = new HumanPoseHandler(_animator.avatar, _animator.transform);
                    _hp = new HumanPose();
                }
                Transform root = _animator.transform;
                Vector3 rootPos = root.localPosition;
                Quaternion rootRot = root.localRotation;
                Transform hips = bones[0];
                Vector3 hipsPos = hips != null ? hips.localPosition : Vector3.zero;
                Quaternion[] keep = new Quaternion[10];
                for (int i = 0; i < 10; i++) if (bones[i] != null) keep[i] = bones[i].localRotation;

                _hph.GetHumanPose(ref _hp);
                for (int i = 0; i < 8; i++)
                {
                    int m = _legMuscles[i];
                    if (m < 0 || _hp.muscles == null || m >= _hp.muscles.Length) continue;
                    // Stretch is + when straight: a knee "bend" lowers it.
                    float d = _l[i] * scale * ((i == 2 || i == 6) ? -1f : 1f);
                    _hp.muscles[m] = Mathf.Clamp(_hp.muscles[m] + d, -1.5f, 1.5f);
                }
                _hph.SetHumanPose(ref _hp);

                root.localPosition = rootPos;
                root.localRotation = rootRot;
                if (hips != null) hips.localPosition = hipsPos;
                for (int i = 0; i < 10; i++) if (bones[i] != null) bones[i].localRotation = keep[i];
                for (int i = 0; i < 6; i++) if (legs[i] != null) _legWritten[i] = legs[i].localRotation;
                _legHas = true;
            }
            catch (Exception)
            {
                _legsBroken = true;
                _legHas = false;
            }
        }

        private void OnDestroy()
        {
            ReleaseBow(false);
            if (_keys != null) ReleaseRoot();
            SetLeftHidden(false);
            IDisposable d = _hph as IDisposable;
            if (d != null) { try { d.Dispose(); } catch { } }
            _hph = null;
        }
    }

    public static class DragonCombat
    {
        // v0.25.4: set by the Immortal HUD; blocks Hud.UpdateHealth / Stamina / Eitr / Food.
        public static bool VanillaVitalsHidden;

        // v0.25.3 perf: type lookups by name are resolved once (they scanned every loaded assembly
        // several times per frame for the HUD / UI checks).
        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();

        // v0.25.54: runs a visual effect; a failure in an effect never interrupts the skill that called it.
        public static void RunVfx(Action fx)
        {
            if (fx == null || !DragonVfx.Enabled) return;
            try { fx(); }
            catch (Exception ex) { if (DragonCombatPlugin.Instance != null) DragonCombatPlugin.Instance.LogInfo("[Immortal Heroes] VFX error: " + ex.Message); }
        }

        // v0.25.66 UNIVERSAL RULE (user): "Physical Objects" = environmental objects (trees, rocks, logs, bushes);
        // "Structures" = man-made objects (build pieces, ruins, dungeons). Ground-targeted skills aim only at
        // terrain or Structures, and everything they do to the ground follows the terrain Y level.
        private static int _terrainLayer = -2;
        private static readonly string[] GroundTypes = { "Heightmap", "TerrainModifier" };
        private static readonly string[] StructureTypes = { "Piece", "WearNTear" };
        private static readonly string[] EnvironmentTypes = { "TreeBase", "TreeLog", "MineRock", "MineRock5", "Destructible", "Pickable", "StaticPhysics" };
        private static readonly string[] SiteTypes = { "Room", "DungeonGenerator", "Location" };

        private static bool HasParentOfType(Collider c, string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Type t = FindTypeCached(names[i]);
                if (t != null && c.GetComponentInParent(t) != null) return true;
            }
            return false;
        }

        public static bool IsTerrainOrStructure(Collider c)
        {
            if (c == null || c.isTrigger) return false;
            try
            {
                if (_terrainLayer == -2) _terrainLayer = LayerMask.NameToLayer("terrain");
                if (_terrainLayer >= 0 && c.gameObject.layer == _terrainLayer) return true;
                if (c is TerrainCollider) return true;
                if (c.GetComponentInParent<Character>() != null) return false;
                if (HasParentOfType(c, GroundTypes) || HasParentOfType(c, StructureTypes)) return true;
                if (HasParentOfType(c, EnvironmentTypes)) return false;
                return HasParentOfType(c, SiteTypes);
            }
            catch (Exception) { return false; }
        }

        private static int _groundMask;
        public static int GroundMask()
        {
            if (_groundMask == 0) _groundMask = LayerMask.GetMask("terrain", "Default", "static_solid", "Default_small", "piece", "vehicle");
            return _groundMask;
        }

        // Ground (terrain / Structure) height under a point; environmental objects are passed through.
        public static bool TryGroundY(Vector3 pos, float above, float depth, out float y)
        {
            y = pos.y;
            RaycastHit[] hits = Physics.RaycastAll(pos + Vector3.up * above, Vector3.down, above + depth, GroundMask(), QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return false;
            // the surface nearest the reference height wins (a floor under a roof, the slope under a cliff edge)
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (!IsTerrainOrStructure(hits[i].collider)) continue;
                float d = Mathf.Abs(hits[i].point.y - pos.y);
                if (d >= best) continue;
                best = d;
                y = hits[i].point.y;
                found = true;
            }
            return found;
        }

        public static Type FindTypeCached(string name)
        {
            Type found;
            if (TypeCache.TryGetValue(name, out found)) return found;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length && found == null; i++)
            {
                try { found = assemblies[i].GetType(name, false); } catch { }
            }
            TypeCache[name] = found;
            return found;
        }

        // ==================================================================================
        // v0.25.0 SKILL BODY MOTIONS: procedural action / gesture for every skill (Dragon Nest /
        // Devil May Cry style): the body (Visual root) leans, crouches, spins and snaps around its
        // centre on top of the vanilla pose. One motion at a time; rest pose restored at the end.
        // ==================================================================================
        private static int _motionToken;
        private static bool _motionActive;
        private static Quaternion _motionBaseRot;
        private static Vector3 _motionBasePos;

        // Shared Visual-root ownership: one body motion / clip owns the root at a time (token).
        public static Quaternion MotionBaseRot { get { return _motionBaseRot; } }
        public static Vector3 MotionBasePos { get { return _motionBasePos; } }

        public static int ClaimMotionRoot(Transform v)
        {
            if (v != null && !_motionActive) { _motionBaseRot = v.localRotation; _motionBasePos = v.localPosition; _motionActive = true; }
            return ++_motionToken;
        }

        public static bool OwnsMotionRoot(int token) { return token == _motionToken; }

        public static void ReleaseMotionRoot(int token, Transform v)
        {
            if (token != _motionToken || v == null || !_motionActive) return;
            v.localRotation = _motionBaseRot;
            v.localPosition = _motionBasePos;
            _motionActive = false;
        }

        // v0.25.7: play a keyframed skill clip (see DragonSkillClipDriver). windup = seconds until the
        // impact key; hold = stay on the impact pose until ClipImpact / ClipStop.
        public static void PlayClip(Player player, string clip, float windup, bool hold)
        {
            if (player == null || player != Player.m_localPlayer || string.IsNullOrEmpty(clip)) return;
            if (DragonCombatPlugin.Instance != null && !DragonCombatPlugin.Instance.EnableSkillAnimations.Value) return;
            DragonClipKey[] keys = HorizonWalkerMoving(player) ? MovingCastClip() : BowClip(player, clip);   // v0.25.85 Ranger bow layer, v0.25.86 HW moving casts
            if (keys == null && !hold) keys = VanillaClip(player, clip, 0f);
            if (keys == null) keys = SkillClip(clip);
            if (keys == null) return;
            // v0.25.72: the weapon hand glows in the Class colour through every real wind up / hold
            // v0.25.85 (user): no weapon charge orb / flare
            if (false && (windup >= 0.3f || hold)) { Player wp = player; float ws = hold ? 3f : windup + 0.25f; RunVfx(delegate { DragonVfx.WeaponCharge(wp, ws); }); }
            // v0.25.40 (user): buff raises stand still until the wind up is over (no gliding).
            bool standEmote = keys[0].VA != null && keys[0].VA.StartsWith("emote", StringComparison.Ordinal) &&
                              !clip.StartsWith("hw_", StringComparison.Ordinal) && clip != "merc_fury_accent";
            // v0.25.86 (user: Heaven's Crucible lost its animation): long wind-up raises start late enough that the raise
            // is still up when the skill lands (it used to finish ~1s before the Barrier appeared).
            if (keys[0].QL && windup > 0.8f) keys[0].VF = Mathf.Clamp01(1f - 0.75f / windup);
            if (keys[0].VA != null && (keys[0].QL || standEmote))
            {
                LockSkill(player, Mathf.Max(0.5f, windup) + 0.05f);
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = new Vector3(0f, rb.velocity.y, 0f);   // stop at once, no slide
                if (keys[0].QL) SetSkillAnimSpeed(player, 1.8f, Mathf.Max(0.5f, windup) + 0.3f);   // v0.25.49 faster raise
            }
            DragonSkillPoseDriver legacy = player.GetComponent<DragonSkillPoseDriver>();
            if (legacy != null) UnityEngine.Object.Destroy(legacy);
            DragonSkillClipDriver d = player.GetComponent<DragonSkillClipDriver>();
            if (d == null) d = player.gameObject.AddComponent<DragonSkillClipDriver>();
            d.Begin(keys, windup, hold, BodyVisual(player), clip);
        }

        // Plays caller-built keys (variable-length clips such as Whirlwind).
        public static void PlayClipKeys(Player player, DragonClipKey[] keys, float windup)
        {
            if (player == null || player != Player.m_localPlayer || keys == null || keys.Length == 0) return;
            if (DragonCombatPlugin.Instance != null && !DragonCombatPlugin.Instance.EnableSkillAnimations.Value) return;
            DragonSkillPoseDriver legacy = player.GetComponent<DragonSkillPoseDriver>();
            if (legacy != null) UnityEngine.Object.Destroy(legacy);
            DragonSkillClipDriver d = player.GetComponent<DragonSkillClipDriver>();
            if (d == null) d = player.gameObject.AddComponent<DragonSkillClipDriver>();
            d.Begin(keys, windup, false, BodyVisual(player));
        }

        // Continuous spin for `seconds` (one turn every `turn` s), arms out, then settle.
        public static void PlaySpinClip(Player player, float seconds, float turn)
        {
            PlaySpinClip(player, seconds, turn, true);
        }

        public static void PlaySpinClip(Player player, float seconds, float turn, bool twoHand)
        {
            // v0.25.44 (user): our own continuous spin, no vanilla loop. Modelled on Valheim's heavy atgeir sweep
            // (weapon two-handed, level at waist height, arms long, knees bent, chest over the hips) and on the
            // DualWield mod's spins: the hips lead each turn and the blade trails flat around the body.
            if (twoHand)
            {
                float tq = Mathf.Max(0.18f, turn);
                int tt = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0.1f, seconds) / tq));
                PlayClipKeys(player, SbSpin(tt, tq, true), 0.12f);
                return;
            }
            // v0.25.24 SPAA (storyboard 09 CYCLONE): coil with the weapon across to the left, then turn clockwise
            // with the main arm straight out to the side and the weapon held level, for whole turns only.
            float q = Mathf.Max(0.12f, turn * 0.8f);   // v0.25.26: a whirlwind spins violently and continuously
            int turns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0.1f, seconds) / q));
            DragonClipKey[] v = VanillaClip(player, "spin", Mathf.Max(0.3f, seconds));
            PlayClipKeys(player, v != null ? v : SbSpin(turns, q, twoHand), 0.15f);
        }


        public static void PlayClip(Player player, string clip, float windup)
        {
            PlayClip(player, clip, windup, false);
        }

        // v0.25.15: automatic procs (Fury, Overcharge, death-save, parry burst) are low-priority accents:
        // they never replace a skill clip that is playing.
        // v0.25.86 (user: "Charge = Hold"): while a charged bow skill charges, the Ranger controls hold the bow's REAL
        // vanilla draw (attackHold); the skill cancels the vanilla shot before it fires its own.
        public static float SkillBowHoldUntil;
        public static float VanillaPauseUntil;   // v0.25.86 freezes the playing vanilla skill swing (refresh every frame)
        public static bool SkillBowHoldActive { get { return Time.time < SkillBowHoldUntil; } }

        public static bool ClipBusy(Player player)
        {
            if (player == null) return false;
            DragonSkillClipDriver d = player.GetComponent<DragonSkillClipDriver>();
            return d != null && d.IsBusy;
        }

        public static void PlayAccent(Player player, string clip, float windup)
        {
            if (player == null) return;
            DragonSkillClipDriver d = player.GetComponent<DragonSkillClipDriver>();
            if (d != null && d.IsPlaying) return;
            PlayClip(player, clip, windup, false);
        }

        // Landing / release event for a held clip. False = nothing was holding (caller may play a fallback).
        public static bool ClipImpactIfHolding(Player player)
        {
            if (player == null) return false;
            DragonSkillClipDriver d = player.GetComponent<DragonSkillClipDriver>();
            if (d == null || !d.IsHolding) return false;
            d.Impact();
            return true;
        }

        // v0.25.15 forced run locomotion: skills that move the body themselves (Shield Charge, Frenzied
        // Charge) show Valheim's real running legs. ZSyncAnimation.SetFloat("forward_speed") is overridden
        // for the local player while on (CombatRuntime prefix) and the Animator is set every frame.
        private static object _forceRunZanim;
        private static Animator _forceRunAnimator;
        private static float _forceRunSpeed;
        private static bool _forceRunOn;
        private static float _forceBlockUntil;
        private static readonly int ForwardSpeedHash = Animator.StringToHash("forward_speed");
        private static readonly int SidewaySpeedHash = Animator.StringToHash("sideway_speed");
        private static readonly int OnGroundHash = Animator.StringToHash("onGround");
        private static readonly int BlockingHash = Animator.StringToHash("blocking");

        public static void ForceRun(Player player, bool on)
        {
            ForceRun(player, on, false, 0f);
        }

        // v0.25.19: block = Valheim's own shield-up pose (animator "blocking") on top of the run, so the shield
        // really leads (Shield Charge). blockAfter = seconds the shield stays up after the run stops (the Bash).
        public static void ForceRun(Player player, bool on, bool block, float blockAfter)
        {
            if (player == null || player != Player.m_localPlayer) return;
            DragonForceRun fr = player.GetComponent<DragonForceRun>();
            if (!on)
            {
                _forceRunOn = false;
                _forceBlockUntil = blockAfter > 0f && _forceBlockUntil > Time.time ? Time.time + blockAfter : 0f;
                if (_forceBlockUntil <= 0f)
                {
                    if (_forceRunAnimator != null) _forceRunAnimator.SetBool(BlockingHash, false);
                    _forceRunZanim = null;
                    _forceRunAnimator = null;
                    if (fr != null) UnityEngine.Object.Destroy(fr);
                }
                return;
            }
            _forceRunOn = true;
            _forceBlockUntil = block ? float.MaxValue : 0f;
            if (DragonCombatPlugin.Instance != null && !DragonCombatPlugin.Instance.EnableSkillAnimations.Value) return;
            Component z = null;
            Component[] cs = player.GetComponents<Component>();
            for (int i = 0; i < cs.Length; i++) if (cs[i] != null && cs[i].GetType().Name == "ZSyncAnimation") { z = cs[i]; break; }
            _forceRunZanim = z;
            _forceRunAnimator = player.GetComponentInChildren<Animator>();
            _forceRunSpeed = Mathf.Max(4f, player.m_runSpeed);
            if (fr == null) player.gameObject.AddComponent<DragonForceRun>();
        }

        // false = nothing forced any more (the component removes itself).
        internal static bool ForceRunTick()
        {
            if (_forceRunAnimator == null) return false;
            bool block = Time.time < _forceBlockUntil;
            if (!_forceRunOn && !block)
            {
                _forceRunAnimator.SetBool(BlockingHash, false);
                _forceRunZanim = null;
                _forceRunAnimator = null;
                return false;
            }
            if (_forceRunOn)
            {
                _forceRunAnimator.SetFloat(ForwardSpeedHash, _forceRunSpeed);
                _forceRunAnimator.SetFloat(SidewaySpeedHash, 0f);
                _forceRunAnimator.SetBool(OnGroundHash, true);
            }
            _forceRunAnimator.SetBool(BlockingHash, block);
            return true;
        }

        // ZSyncAnimation.SetFloat prefix body: true = the value was replaced.
        internal static bool ForceRunOverride(object zanim, object key, ref float value)
        {
            if (_forceRunZanim == null || !ReferenceEquals(zanim, _forceRunZanim)) return false;
            bool forward = key is int ? (int)key == ForwardSpeedHash : (key as string) == "forward_speed";
            bool side = key is int ? (int)key == SidewaySpeedHash : (key as string) == "sideway_speed";
            if (!_forceRunOn) return false;
            if (forward) { value = _forceRunSpeed; return true; }
            if (side) { value = 0f; return true; }
            return false;
        }

        // ZSyncAnimation.SetBool prefix body: onGround stays true while a run is forced (kinematic moves made
        // Valheim play the falling legs), blocking stays on while the shield pose is forced.
        // v0.25.85 forced bow draw (Ranger vanilla bow layer).
        private static object _bowAimZanim;
        private static string _bowAimName;
        private static int _bowAimHash;
        private static bool _bowAimOn;

        public static void SetBowAim(Player p, string name, bool on)
        {
            if (p == null || string.IsNullOrEmpty(name)) return;
            try
            {
                Component z = null;
                Component[] cs = p.GetComponents<Component>();
                for (int i = 0; i < cs.Length; i++) if (cs[i] != null && cs[i].GetType().Name == "ZSyncAnimation") { z = cs[i]; break; }
                _bowAimZanim = z; _bowAimName = name; _bowAimHash = Animator.StringToHash(name); _bowAimOn = on;
                Animator a = p.GetComponentInChildren<Animator>();
                if (a != null) a.SetBool(name, on);
            }
            catch (Exception) { }
        }

        // The equipped bow / crossbow's own animations (attack trigger + draw bool), resolved on this animator.
        private static bool BowAnims(Player p, out string trig, out string draw)
        {
            trig = null; draw = null;
            try
            {
                ItemDrop.ItemData w = GetHandItem(p, "m_leftItem");
                if (w == null || w.m_shared == null) w = GetHandItem(p, "m_rightItem");
                if (w == null || w.m_shared == null) return false;
                object atk = w.m_shared.GetType().GetField("m_attack").GetValue(w.m_shared);
                if (atk == null) return false;
                FieldInfo fa = atk.GetType().GetField("m_attackAnimation"), fd = atk.GetType().GetField("m_drawAnimationState"), fb = atk.GetType().GetField("m_bowDraw");
                string an = fa == null ? null : fa.GetValue(atk) as string;
                string dn = fd == null ? null : fd.GetValue(atk) as string;
                if (string.IsNullOrEmpty(an) || an.IndexOf("bow", StringComparison.OrdinalIgnoreCase) < 0) return false;
                Animator a = p.GetComponentInChildren<Animator>();
                trig = ResolveTrigger(a, an);
                if (trig == null) return false;
                // a real draw bow (item type Bow) - not m_bowDraw, which Wildborn's quick shots toggle at runtime
                FieldInfo ft = w.m_shared.GetType().GetField("m_itemType");
                object itype = ft == null ? null : ft.GetValue(w.m_shared);
                bool drawBow = itype != null && itype.ToString() == "Bow";
                if (drawBow && !string.IsNullOrEmpty(dn) && a != null)
                {
                    AnimatorControllerParameter[] ps = a.parameters;
                    for (int i = 0; i < ps.Length; i++) if (ps[i].type == AnimatorControllerParameterType.Bool && ps[i].name == dn) { draw = dn; break; }
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        // Ranger shots that play the vanilla bow draw + release (user): pitch 1 = aim up (Sky shots), -1 = down (hover).
        private static int BowShotPitch(string clip, out bool isShot)
        {
            isShot = true;
            switch (clip)
            {
                case "rg_sky": case "rg_starfall": return 1;
                case "rg_hover": return -1;
                case "rg_shot": case "rg_power": case "rg_heavy": case "rg_ballista": case "rg_kneel":
                case "rg_split_a": case "rg_split_b": return 0;   // v0.25.86 Splitting volleys: vanilla bow, no custom sweep (arm clipped)
            }
            isShot = false;
            return 0;
        }

        private static DragonClipKey[] BowClip(Player player, string clip)
        {
            bool isShot;
            int pitch = BowShotPitch(clip, out isShot);
            if (!isShot) return null;
            string trig, draw;
            if (!BowAnims(player, out trig, out draw)) return null;
            if (clip == "rg_ballista") draw = null;   // v0.25.86 charged shots use the real vanilla draw hold (SkillBowHold)
            DragonClipKey pose = K(0f);
            if (pitch > 0) pose.Hd(-32f, 0f, 0f);       // v0.25.86 (user): Sky shots = the vanilla bow action, only the head looks up (no back bend)
            else if (pitch < 0) pose.Sp(12f, 0f, 0f).Ch(22f, 0f, 0f).Hd(16f, 0f, 0f);     // aimed down from the air
            DragonClipKey[] k = new DragonClipKey[] { K(-1f), pose.Copy(-0.6f), pose, pose.Copy(0.3f), K(0.6f) };
            k[0].NoAim = true; k[0].NoTrack = true;
            k[0].BowTrig = null; k[0].BowBool = draw;   // the release (bow_fire) is fired by the Ranger's own Shoot() at every shot
            return k;
        }

        internal static bool ForceBoolOverride(object zanim, object key, ref bool value)
        {
            if (_bowAimOn && _bowAimZanim != null && ReferenceEquals(zanim, _bowAimZanim) &&
                (key is int ? (int)key == _bowAimHash : (key as string) == _bowAimName)) { value = true; return true; }
            if (_forceRunZanim == null || !ReferenceEquals(zanim, _forceRunZanim)) return false;
            bool ground = key is int ? (int)key == OnGroundHash : (key as string) == "onGround";
            bool blocking = key is int ? (int)key == BlockingHash : (key as string) == "blocking";
            if (ground && _forceRunOn) { value = true; return true; }
            if (blocking && Time.time < _forceBlockUntil) { value = true; return true; }
            return false;
        }

        // v0.25.15 render-only weapon stow (Olympic Hero fist landing): hides the main-hand item's renderers
        // for `seconds`, then restores exactly the ones it hid. Nothing is unequipped, no stats change.
        private static FieldInfo _rightItemField;
        public static void StowMainWeapon(Player player, float seconds)
        {
            if (player == null || DragonCombatPlugin.Instance == null) return;
            Component vis = null;
            Component[] cs = player.GetComponentsInChildren<Component>();
            for (int i = 0; i < cs.Length; i++) if (cs[i] != null && cs[i].GetType().Name == "VisEquipment") { vis = cs[i]; break; }
            if (vis == null) return;
            if (_rightItemField == null) _rightItemField = vis.GetType().GetField("m_rightItemInstance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            GameObject item = _rightItemField == null ? null : _rightItemField.GetValue(vis) as GameObject;
            if (item == null) return;
            Renderer[] rs = item.GetComponentsInChildren<Renderer>();
            List<Renderer> hidden = new List<Renderer>();
            for (int i = 0; i < rs.Length; i++) if (rs[i] != null && rs[i].enabled) { rs[i].enabled = false; hidden.Add(rs[i]); }
            if (hidden.Count > 0) DragonCombatPlugin.Instance.StartCoroutine(RestoreRenderers(hidden, seconds));
        }

        private static IEnumerator RestoreRenderers(List<Renderer> hidden, float seconds)
        {
            try { yield return new WaitForSeconds(Mathf.Max(0.05f, seconds)); }
            finally { for (int i = 0; i < hidden.Count; i++) if (hidden[i] != null) hidden[i].enabled = true; }
        }

        public static void ClipImpact(Player player)
        {
            if (player == null) return;
            DragonSkillClipDriver d = player.GetComponent<DragonSkillClipDriver>();
            if (d != null) d.Impact();
        }

        public static void ClipStop(Player player, float blend)
        {
            if (player == null) return;
            DragonSkillClipDriver d = player.GetComponent<DragonSkillClipDriver>();
            if (d != null) d.Stop(blend);
        }

        public static bool HasClip(string clip) { return SkillClip(clip) != null; }

        // ---------------------------------------------------------------- v0.25.25 vanilla animation layer
        private static string _vanMapSrc;
        private static readonly Dictionary<string, KeyValuePair<string, float>> VanMap = new Dictionary<string, KeyValuePair<string, float>>();
        private static readonly Dictionary<int, HashSet<string>> AnimTriggers = new Dictionary<int, HashSet<string>>();
        private static MethodInfo _zanimSetTrigger;
        private static bool _triggersLogged;

        private static void ParseVanMap()
        {
            string src = DragonCombatPlugin.Instance != null && DragonCombatPlugin.Instance.VanillaAnimationMap != null ? DragonCombatPlugin.Instance.VanillaAnimationMap.Value : "";
            if (src == _vanMapSrc) return;
            _vanMapSrc = src;
            VanMap.Clear();
            string[] parts = (src ?? "").Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string e = parts[i].Trim();
                int eq = e.IndexOf('=');
                if (eq <= 0) continue;
                string clip = e.Substring(0, eq).Trim(), trig = e.Substring(eq + 1).Trim();
                float lead = 0.3f;
                int at = trig.IndexOf('@');
                if (at > 0)
                {
                    float.TryParse(trig.Substring(at + 1).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lead);
                    trig = trig.Substring(0, at).Trim();
                }
                if (clip.Length > 0 && trig.Length > 0) VanMap[clip] = new KeyValuePair<string, float>(trig, lead);
            }
        }

        // The animator trigger that really exists for `name` (name, or name + "0" for combo chains), or null.
        // v0.25.42 state learning: which animator state (layer + full path hash) a trigger leads to.
        private class StateLearn { public Animator A; public string Name; public int[] Pre; public float At; }
        private static readonly Dictionary<string, KeyValuePair<int, int>> LearnedStates = new Dictionary<string, KeyValuePair<int, int>>();
        private static readonly List<StateLearn> PendingLearns = new List<StateLearn>();

        public static bool LearnedState(Animator a, string trigger, out int hash, out int layer)
        {
            hash = 0; layer = 0;
            KeyValuePair<int, int> v;
            if (a == null || !LearnedStates.TryGetValue(a.GetInstanceID() + ":" + trigger, out v)) return false;
            layer = v.Key; hash = v.Value;
            return true;
        }

        public static void LearnState(Animator a, string trigger)
        {
            if (a == null || string.IsNullOrEmpty(trigger)) return;
            if (LearnedStates.ContainsKey(a.GetInstanceID() + ":" + trigger)) return;
            try
            {
                StateLearn l = new StateLearn();
                l.A = a; l.Name = trigger; l.At = Time.time;
                l.Pre = new int[a.layerCount];
                for (int i = 0; i < l.Pre.Length; i++) l.Pre[i] = a.GetCurrentAnimatorStateInfo(i).fullPathHash;
                PendingLearns.Add(l);
            }
            catch (Exception) { }
        }

        private static void UpdateLearns()
        {
            for (int k = PendingLearns.Count - 1; k >= 0; k--)
            {
                StateLearn l = PendingLearns[k];
                if (l.A == null || Time.time - l.At > 0.6f) { PendingLearns.RemoveAt(k); continue; }
                try
                {
                    // v0.25.44: wait until the animator consumed the trigger, then take the layer that is
                    // transitioning into a new state (the attack layer), not one that merely changed by movement.
                    if (l.A.GetBool(l.Name)) continue;
                    int pick = -1;
                    for (int i = 0; i < l.Pre.Length && i < l.A.layerCount; i++)
                        if (l.A.IsInTransition(i) && l.A.GetNextAnimatorStateInfo(i).fullPathHash != l.Pre[i]) { pick = i; break; }
                    for (int i = 0; i < l.Pre.Length && i < l.A.layerCount; i++)
                    {
                        if (pick >= 0 && i != pick) continue;
                        AnimatorStateInfo st = l.A.IsInTransition(i) ? l.A.GetNextAnimatorStateInfo(i) : l.A.GetCurrentAnimatorStateInfo(i);
                        if (st.fullPathHash == l.Pre[i]) continue;
                        LearnedStates[l.A.GetInstanceID() + ":" + l.Name] = new KeyValuePair<int, int>(i, st.fullPathHash);
                        PendingLearns.RemoveAt(k);
                        break;
                    }
                }
                catch (Exception) { PendingLearns.RemoveAt(k); }
            }
        }

        // Number of consecutive chain triggers <name>0, <name>1, ... the player's animator really has.
        public static int AnimTriggerCount(Animator a, string name)
        {
            if (a == null || string.IsNullOrEmpty(name)) return 0;
            ResolveTrigger(a, name);
            HashSet<string> set;
            if (!AnimTriggers.TryGetValue(a.GetInstanceID(), out set)) return 0;
            int n = 0;
            while (n < 10 && set.Contains(name + n.ToString())) n++;
            return n;
        }

        private static string ResolveTrigger(Animator a, string name)
        {
            if (a == null || string.IsNullOrEmpty(name)) return null;
            HashSet<string> set;
            if (!AnimTriggers.TryGetValue(a.GetInstanceID(), out set))
            {
                set = new HashSet<string>();
                try
                {
                    AnimatorControllerParameter[] ps = a.parameters;
                    for (int i = 0; i < ps.Length; i++) if (ps[i].type == AnimatorControllerParameterType.Trigger) set.Add(ps[i].name);
                }
                catch (Exception) { }
                if (set.Count == 0) return null;   // not ready yet: try again next cast
                AnimTriggers[a.GetInstanceID()] = set;
                if (!_triggersLogged && DragonCombatPlugin.Instance != null)
                {
                    _triggersLogged = true;
                    List<string> all = new List<string>(set);
                    all.Sort();
                    DragonCombatPlugin.Instance.LogInfo("[Immortal Heroes] Animator triggers: " + string.Join(", ", all.ToArray()));
                }
            }
            if (set.Contains(name)) return name;
            if (set.Contains(name + "0")) return name + "0";
            string bare = name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            if (bare != name && set.Contains(bare)) return bare;
            return null;
        }

        // Overlay for a vanilla-animated skill: the vanilla attack does the arms / weapon; we only add a planted
        // stance and a slight lean. length > 0 = sustained (repeat the animation every second).
        // v0.25.86 (user: the Sword Master animations were broken / super sped up): calm constant-speed swings.
        public static bool IsConstSpeedClip(string clip)
        {
            switch (clip)
            {
                case "sm_slash_a": case "sm_slash_b": case "sm_moon_finisher":
                case "sm_crescent": case "sm_crescent_asc": case "sm_crescent_asc2":
                case "sm_halfmoon": case "sm_halfmoon_2":
                    return true;
            }
            return false;
        }

        public static bool IsRaiseBuffClip(string clip)
        {
            switch (clip)
            {
                case "cleric_wave": case "cleric_wave_ally": case "cleric_ray": case "cleric_light":
                case "cleric_intervention": case "cleric_crucible": case "wiz_clockwork": case "sm_guidance":
                case "rg_tailwind": case "rg_vigil":
                    return true;
            }
            return false;
        }

        private static DragonClipKey[] VanillaClip(Player player, string clip, float length)
        {
            ParseVanMap();
            KeyValuePair<string, float> m;
            if (!VanMap.TryGetValue(clip, out m)) return null;
            string trig = ResolveTrigger(player.GetComponentInChildren<Animator>(), m.Key);
            if (trig == null) return null;
            if (IsRaiseBuffClip(clip))
            {
                // v0.25.40 (user): ChatGPT's Holy Wave raise, exactly: Valheim's emote_cheer at natural speed from
                // the first frame, the off-hand arm held still, no stance / lean / root motion of our own.
                DragonClipKey q = K(0f);
                DragonClipKey[] rk = new DragonClipKey[] { K(-1f), q.Copy(-0.50f), q, q.Copy(0.12f), K(0.3f) };   // v0.25.49 shorter tail
                for (int i = 0; i < rk.Length; i++) rk[i].QL = true;
                rk[0].VA = trig; rk[0].VL = m.Value; rk[0].VR = 0f; rk[0].VF = 0f; rk[0].NoAim = true; rk[0].NoTrack = true; rk[0].NoPlant = true;
                return rk;
            }
            // v0.25.38 (user): emotes are full-body and freeze the legs; while running use the upper-body
            // custom pose instead so the run animation keeps playing.
            if (trig.StartsWith("emote", StringComparison.Ordinal) && (clip.StartsWith("hw_", StringComparison.Ordinal) || clip == "merc_fury_accent"))
            {
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null) { Vector3 hv = rb.velocity; hv.y = 0f; if (hv.magnitude > 1.2f) return null; }
            }
            DragonClipKey[] k;
            if (length > 0f)
                k = new DragonClipKey[] { K(-1f), Ft(K(0f).Off(0f, -0.03f, 0f), 0.15f, 0.1f), Ft(K(length).Off(0f, -0.03f, 0f), 0.15f, 0.1f), K(length + 0.3f) };
            else
                k = new DragonClipKey[] { K(-1f), Ft(K(-0.3f).Sp(3f, 0f, 0f), 0.15f, 0.08f), Ft(K(0f).Sp(6f, 0f, 0f).Off(0f, -0.03f, 0.03f), 0.22f, 0.1f), Ft(K(0.35f).Sp(4f, 0f, 0f).Off(0f, -0.02f, 0.02f), 0.2f, 0.1f), K(0.75f) };
            if (clip == "merc_circle" && length <= 0f)
            {
                // v0.25.29 Circle Swing wind up = crow hop (thrower's skip): turn side-on, hop on the RIGHT leg with the left
                // knee up and the right arm cocked back, stride the left foot forward and plant, then uncoil into the vanilla spin.
                k = new DragonClipKey[] {
                    K(-1f),
                    Ft(K(-0.93f).Rot(0f, 30f, 0f).Sp(3f, 0f, 0f).Off(0f, -0.06f, 0f), 0.1f, 0.1f).Hand(0.55f, 0.1f, -0.45f, 0.8f),
                    K(-0.85f).Rot(0f, 65f, 0f).Sp(-5f, 0f, 0f).LL(0.35f, 0.05f, 0f, 0f).Lift(0.32f, 0.1f).Off(0f, 0.12f, 0.08f).Hand(0.5f, 0.45f, -0.85f, 0.95f),
                    K(-0.76f).Rot(0f, 65f, 0f).Sp(-4f, 0f, 0f).LL(0.45f, 0.05f, 0f, 0f).Lift(0.26f, 0f).Off(0f, 0.01f, 0.14f).Hand(0.5f, 0.5f, -0.9f, 0.95f),
                    K(-0.66f).Rot(0f, 55f, 0f).Sp(4f, 0f, 0f).LL(0.7f, 0.15f, 0f, 0f).RL(-0.3f, 0.12f, 0f, 0f).Lift(0f, 0f).Off(0f, -0.07f, 0.2f).Hand(0.5f, 0.45f, -0.85f, 0.95f),
                    Ft(K(-0.52f).Rot(0f, 20f, 0f).Sp(6f, 0f, 0f).Off(0f, -0.05f, 0.2f), 0.6f, 0.25f),
                    Ft(K(0f).Sp(6f, 0f, 0f).Off(0f, -0.03f, 0.18f), 0.4f, 0.15f),
                    Ft(K(0.35f).Sp(4f, 0f, 0f).Off(0f, -0.02f, 0.1f), 0.25f, 0.1f),
                    K(0.75f) };
            }
            k[0].VA = trig;
            k[0].VL = m.Value;
            if (trig.StartsWith("emote", StringComparison.Ordinal)) { k[0].NoTrack = true; k[0].NoPlant = true; }   // v0.25.41 emotes: natural speed, their own legs
            if (clip == "merc_circle" && length <= 0f) k[0].VF = 0.4f;   // after the crow hop (v0.25.86: quicker hop, slower spin)
            if (clip == "wiz_nova") k[0].NoTrack = true;
            if (IsConstSpeedClip(clip)) k[0].ConstSpeed = true;   // v0.25.85 (user): Frost Nova's staff raise at natural speed, never fast-forwarded
            k[0].VR = length > 0f ? 1f : 0f;
            k[0].NoAim = true;
            return k;
        }

        public static string ResolveTriggerName(Animator a, string name) { return ResolveTrigger(a, name); }

        // Fires a Valheim animator trigger by (base) name; false when the game has no such animation.
        public static bool PlayVanillaTrigger(Player player, string name)
        {
            if (player == null) return false;
            string t = ResolveTrigger(player.GetComponentInChildren<Animator>(), name);
            if (t == null) return false;
            FireVanilla(player, t);
            return true;
        }

        public static void StopEmote(Player player)
        {
            if (player == null) return;
            string t = ResolveTrigger(player.GetComponentInChildren<Animator>(), "emote_stop");
            if (t != null) FireVanilla(player, t);
        }

        // v0.25.55 (user: a normal attack sometimes follows a skill): a skill's vanilla animation fires attack
        // events; Valheim would run them on the last normal Attack still stored on the player. Clear it and
        // ignore attack events until a new normal attack really starts.
        public static float SkillAnimAttackBlockUntil;

        public static void BlockSkillAnimAttack(Player player, float seconds)
        {
            if (player == null || player != Player.m_localPlayer) return;
            SkillAnimAttackBlockUntil = Mathf.Max(SkillAnimAttackBlockUntil, Time.time + Mathf.Max(0.1f, seconds));
            CancelCurrentAttack(player);
        }

        // v0.25.65: a skill takes over -> the running normal attack is properly aborted (a looping Gun Staff /
        // hold attack used to stay stuck in its fire pose when only m_currentAttack was cleared) and Valheim's
        // queued attack input is dropped, so the click that cast / finished a skill never fires a basic attack.
        public static float AttackSwallowUntil;
        private static FieldInfo _curAttackField;

        public static void CancelCurrentAttack(Player player)
        {
            if (player == null) return;
            try
            {
                if (_curAttackField == null) _curAttackField = typeof(Humanoid).GetField("m_currentAttack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object atk = _curAttackField == null ? null : _curAttackField.GetValue(player);
                if (atk != null)
                {
                    MethodInfo abort = atk.GetType().GetMethod("Abort", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (abort == null) abort = atk.GetType().GetMethod("Stop", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (abort != null) { try { abort.Invoke(atk, null); } catch (Exception) { } }
                }
                if (_curAttackField != null) _curAttackField.SetValue(player, null);
            }
            catch (Exception) { }
            ClearAttackQueue(player);
        }

        public static void ClearAttackQueue(Player player)
        {
            if (player == null) return;
            BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            string[] floats = { "m_queuedAttackTimer", "m_queuedSecondAttackTimer" };
            string[] bools = { "m_attack", "m_attackHold", "m_secondaryAttack", "m_secondaryAttackHold" };
            for (int i = 0; i < floats.Length; i++) { FieldInfo fi = FindField(player.GetType(), floats[i], f); if (fi != null && fi.FieldType == typeof(float)) fi.SetValue(player, 0f); }
            for (int i = 0; i < bools.Length; i++) { FieldInfo fi = FindField(player.GetType(), bools[i], f); if (fi != null && fi.FieldType == typeof(bool)) fi.SetValue(player, false); }
        }

        private static readonly Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();
        private static FieldInfo FindField(Type t, string name, BindingFlags f)
        {
            string key = t.FullName + "." + name;
            FieldInfo fi;
            if (_fieldCache.TryGetValue(key, out fi)) return fi;
            for (Type c = t; c != null && fi == null; c = c.BaseType) fi = c.GetField(name, f | BindingFlags.DeclaredOnly);
            _fieldCache[key] = fi;
            return fi;
        }

        // Swallow the attack buttons for a moment (the click belonged to a skill).
        public static void SwallowAttackInput(Player player, float seconds)
        {
            if (player == null || player != Player.m_localPlayer) return;
            AttackSwallowUntil = Mathf.Max(AttackSwallowUntil, Time.time + Mathf.Max(0.05f, seconds));
            CancelCurrentAttack(player);
        }

        public static void FireVanilla(Player player, string trigger)
        {
            if (player == null || string.IsNullOrEmpty(trigger)) return;
            if (!trigger.StartsWith("emote", StringComparison.Ordinal)) BlockSkillAnimAttack(player, 2.5f);
            try
            {
                Component z = null;
                Component[] cs = player.GetComponents<Component>();
                for (int i = 0; i < cs.Length; i++) if (cs[i] != null && cs[i].GetType().Name == "ZSyncAnimation") { z = cs[i]; break; }
                if (z != null)
                {
                    if (_zanimSetTrigger == null) _zanimSetTrigger = z.GetType().GetMethod("SetTrigger", new Type[] { typeof(string) });
                    if (_zanimSetTrigger != null) { _zanimSetTrigger.Invoke(z, new object[] { trigger }); return; }
                }
                Animator a = player.GetComponentInChildren<Animator>();
                if (a != null) a.SetTrigger(trigger);
            }
            catch (Exception) { }
        }

        private static Dictionary<string, DragonClipKey[]> _clips;

        private static DragonClipKey K(float t) { return new DragonClipKey(t); }

        private static DragonClipKey[] SkillClip(string name)
        {
            if (_clips == null) { _clips = new Dictionary<string, DragonClipKey[]>(); BuildClips(_clips); }
            DragonClipKey[] keys;
            return _clips.TryGetValue(name, out keys) ? keys : null;
        }

        private static void BuildClips(Dictionary<string, DragonClipKey[]> c)
        {
            BuildClericClips(c);
            BuildWarriorClips(c);
            BuildSorcererClips(c);
            BuildRangerClips(c);
            BuildTraitClips(c);
            BuildBlueprintClips(c);
            BuildBlueprintB(c);
            BuildBlueprintC(c);
            BuildBlueprintD(c);
            BuildPolishClips(c);
            BuildGuideClips(c);
            BuildStoryboardClips(c);
        }

        // ================================================================================== v0.25.23
        // STORYBOARD REBUILD (user: "just the storyboard and what I said"). Every frame of the storyboards is a
        // HAND PLACEMENT from the right shoulder (Hand: direction in the body frame x right / y up / z forward,
        // reach 0..1), the weapon direction (Wp), the two-handed grip (Two), a small torso turn / bend, the
        // body drop (Off.y) and the planted feet (Ft: front foot forward, back foot back, metres via LL/RL).
        // ==================================================================================
        private static DragonClipKey Ft(DragonClipKey k, float front, float back)
        {
            // left foot `front` forward, right foot `back` behind (in LL/RL units: x0.45 m), feet a bit apart
            return k.LL(front, 0.12f, 0f, 0f).RL(-back, 0.12f, 0f, 0f);
        }

        // Ranger bow poses: bow arm straight at the target, string hand anchored at the cheek / released past the ear.
        private static DragonClipKey RgDraw(float t)
        {
            return K(t).Sp(2f, -20f, 0f).Ch(0f, -10f, 0f).Hd(0f, 20f, 0f).LHand(0f, 0.05f, 1f, 1f).Hand(-0.6f, 0.12f, 0.25f, 0.38f);
        }

        private static DragonClipKey RgLoose(float t)
        {
            return K(t).Sp(0f, -18f, 0f).Ch(-2f, -10f, 0f).Hd(0f, 20f, 0f).LHand(0f, 0.05f, 1f, 1f).Hand(0.35f, 0.15f, -0.45f, 0.45f);
        }

        private static DragonClipKey RgSky(float t, bool loose)
        {
            DragonClipKey k = K(t).Sp(-5f, -14f, 0f).Ch(-7f, -8f, 0f).Hd(-26f, 14f, 0f).LHand(0f, 0.75f, 0.65f, 1f);
            return loose ? k.Hand(0.3f, 0.5f, -0.3f, 0.45f) : k.Hand(-0.5f, 0.45f, 0.2f, 0.38f);
        }

        private static DragonClipKey[] SbBdca(bool roar)
        {
            DragonClipKey lift = K(-0.5f).Hd(-4f, 0f, 0f).Hand(0.15f, 0.55f, 0.5f, 0.75f);
            DragonClipKey top = K(0f).Ch(roar ? -12f : -5f, 0f, 0f).Hd(roar ? -24f : -12f, 0f, 0f).Hand(0.08f, 1f, 0.08f, 1f).Wp(0.05f, 1f, 0.05f);
            return new DragonClipKey[] { K(-1f), lift, top, top.Copy(0.35f), K(0.75f) };
        }

        private static DragonClipKey[] SbSsca(bool charged)
        {
            DragonClipKey gather = K(-0.65f).Hd(4f, 0f, 0f).Hand(-0.45f, -0.25f, 0.8f, 0.5f);
            DragonClipKey sky = K(charged ? 0f : -0.2f).Ch(-6f, 0f, 0f).Hd(-20f, 0f, 0f).Hand(0.05f, 1f, 0.05f, 1f).Wp(0.03f, 1f, 0.05f);
            float r = charged ? 0.14f : 0f;
            DragonClipKey point = Ft(K(r).Sp(8f, 6f, 0f).Ch(4f, 4f, 0f).Hd(6f, -4f, 0f).Hand(0f, -0.3f, 1f, 1f).Wp(0f, -0.45f, 1f).Off(0f, -0.03f, 0.03f), 0.25f, 0.1f);
            if (charged) return new DragonClipKey[] { K(-1f), gather.Copy(-0.6f), sky, point, point.Copy(r + 0.3f), K(r + 0.8f) };
            return new DragonClipKey[] { K(-1f), gather, sky, point, point.Copy(0.3f), K(0.8f) };
        }

        // Two-handed horizontal slash, LEFT -> RIGHT (cocked high over the left shoulder between hits).
        private static DragonClipKey[] SbSlash(float heavy, float hold, bool spin, bool pull)
        {
            float a = 1f + 0.3f * heavy;
            DragonClipKey cock = Ft(K(-0.45f).Sp(4f, 18f * a, 0f).Ch(2f, 10f * a, 0f).Hd(0f, -12f, 0f).Hand(-0.8f, 0.35f, 0.6f, 0.6f).Wp(-0.3f, 0.75f, -0.5f).Two(-0.12f).Off(0f, -0.04f, 0f), 0.2f, 0.1f);
            if (pull) cock = Ft(K(-0.45f).Sp(6f, -16f, 0f).Ch(3f, -8f, 0f).Hd(0f, 14f, 0f).Hand(0.25f, -0.75f, -0.25f, 0.7f).Wp(0f, 0f, 1f).Two(-0.12f).Off(0f, -0.06f, -0.03f), 0.3f, 0.2f);
            DragonClipKey left = Ft(K(-0.12f).Sp(5f, 24f * a, 0f).Ch(3f, 14f * a, 0f).Hd(0f, -16f, 0f).Hand(-0.85f, -0.05f, 0.6f, 0.95f).Wp(-1f, 0f, 0.35f).Two(-0.12f).Off(0f, -0.05f, 0f), 0.25f, 0.12f);
            DragonClipKey front = Ft(K(0f).Sp(6f, 0f, 0f).Ch(3f, 0f, 0f).Hand(-0.25f, -0.05f, 1f, 0.95f).Wp(0f, 0f, 1f).Two(-0.12f).Off(0f, -0.06f, 0.03f), 0.32f, 0.12f).Linear();
            DragonClipKey right = Ft(K(0.11f).Sp(6f, -24f * a, 0f).Ch(3f, -14f * a, 0f).Hd(0f, 12f, 0f).Hand(1f, 0f, 0.55f, 0.95f).Wp(1f, 0f, -0.2f).Two(-0.12f).Off(0f, -0.06f, 0.03f), 0.32f, 0.12f).Linear();
            if (spin)
            {
                front = front.Rot(0f, 90f, 0f);
                DragonClipKey r2 = right.Copy(0.11f).Rot(0f, 180f, 0f); r2.Lin = true;
                DragonClipKey r3 = right.Copy(0.22f).Rot(0f, 270f, 0f); r3.Lin = true;
                DragonClipKey r4 = right.Copy(0.33f).Rot(0f, 360f, 0f); r4.Lin = true;
                DragonClipKey sh = right.Copy(0.33f + hold).Rot(0f, 360f, 0f); sh.Lin = false;
                return new DragonClipKey[] { K(-1f), cock, left, front, r2, r3, r4, sh, K(0.33f + hold + 0.45f).Rot(0f, 360f, 0f) };
            }
            DragonClipKey after = right.Copy(0.11f + hold); after.Lin = false;
            return new DragonClipKey[] { K(-1f), cock, left, front, right, after, K(0.11f + hold + 0.45f) };
        }

        // Golf (GAA): baseball stance with the hands at the right shoulder, then a U through the ground to the left.
        private static DragonClipKey[] SbGolf(float hold)
        {
            DragonClipKey stance = Ft(K(-0.6f).Sp(6f, -24f, 0f).Ch(2f, -12f, 0f).Hd(0f, 20f, 0f).Hand(0.15f, 0.35f, -0.15f, 0.35f).Wp(0.25f, 0.8f, -0.55f).Two(-0.12f).Off(0f, -0.06f, 0f), 0.3f, 0.15f);
            DragonClipKey load = stance.Copy(-0.12f).Sp(8f, -30f, 0f).Ch(3f, -15f, 0f);
            DragonClipKey bottom = Ft(K(0f).Sp(22f, 0f, 0f).Ch(8f, 0f, 0f).Hd(6f, 0f, 0f).Hand(-0.2f, -0.9f, 0.35f, 1f).Wp(0f, -0.85f, 0.55f).Two(-0.12f).Off(0f, -0.1f, 0.03f), 0.32f, 0.15f).Linear();
            DragonClipKey up = Ft(K(0.14f).Sp(0f, 28f, 0f).Ch(-3f, 15f, 0f).Hd(0f, -10f, 0f).Hand(-0.8f, 0.5f, 0.3f, 0.95f).Wp(-0.6f, 0.7f, -0.3f).Two(-0.12f).Off(0f, -0.04f, 0.02f), 0.32f, 0.15f).Linear();
            DragonClipKey after = up.Copy(0.14f + hold); after.Lin = false;
            return new DragonClipKey[] { K(-1f), stance, load, bottom, up, after, K(0.14f + hold + 0.5f) };
        }

        // Overhead slam (OSA): two hands straight up, then straight down in front.
        private static DragonClipKey[] SbSlam(float hold, bool rechamber)
        {
            DragonClipKey lift = Ft(K(-0.4f).Ch(-5f, 0f, 0f).Hd(-8f, 0f, 0f).Hand(-0.15f, 1f, 0.1f, 0.95f).Wp(0f, 1f, -0.2f).Two(-0.12f), 0.2f, 0.1f);
            DragonClipKey down = Ft(K(0f).Sp(24f, 0f, 0f).Ch(10f, 0f, 0f).Hd(8f, 0f, 0f).Hand(-0.15f, -0.55f, 0.8f, 1f).Wp(0f, -0.7f, 0.7f).Two(-0.12f).Off(0f, -0.12f, 0.04f), 0.3f, 0.12f).Linear();
            DragonClipKey after = down.Copy(hold); after.Lin = false;
            if (rechamber) return new DragonClipKey[] { K(-1f), lift, down, after, lift.Copy(hold + 0.3f), K(hold + 0.55f) };
            return new DragonClipKey[] { K(-1f), lift, down, after, K(hold + 0.6f) };
        }

        // Forward thrust (FTAA): hand to the ribs, then straight at the target, retract.
        private static DragonClipKey[] SbThrust(float hold)
        {
            DragonClipKey ribs = Ft(K(-0.45f).Sp(4f, -12f, 0f).Ch(2f, -6f, 0f).Hd(0f, 8f, 0f).Hand(0.1f, -0.55f, -0.2f, 0.45f).Wp(0f, 0f, 1f), 0.15f, 0.1f);
            DragonClipKey drive = Ft(K(0f).Sp(8f, 10f, 0f).Ch(3f, 6f, 0f).Hd(0f, -5f, 0f).Hand(0f, -0.05f, 1f, 1f).Wp(0f, 0f, 1f).Off(0f, -0.03f, 0.08f), 0.35f, 0.15f).Linear();
            DragonClipKey after = drive.Copy(hold); after.Lin = false;
            return new DragonClipKey[] { K(-1f), ribs, ribs.Copy(-0.08f), drive, after, ribs.Copy(hold + 0.25f), K(hold + 0.55f) };
        }

        // Projectile cast (PCA): hand drawn up by the shoulder, then the arm drives straight at the target.
        private static DragonClipKey[] SbCast()
        {
            DragonClipKey ch = K(-0.6f).Sp(3f, -10f, 0f).Ch(2f, -6f, 0f).Hd(0f, 6f, 0f).Hand(0.35f, 0.25f, -0.15f, 0.35f);
            DragonClipKey ext = Ft(K(0f).Sp(5f, 8f, 0f).Ch(3f, 5f, 0f).Hd(0f, -4f, 0f).Hand(0f, 0f, 1f, 1f).Wp(0f, 0f, 1f), 0.2f, 0.1f);
            return new DragonClipKey[] { K(-1f), ch, ext, ext.Copy(0.15f), K(0.45f) };
        }

        // Throw (TAA): hand behind the shoulder, torso coiled, throw forward, follow-through across the body.
        private static DragonClipKey[] SbThrow()
        {
            DragonClipKey back = Ft(K(-0.55f).Sp(-2f, -24f, 0f).Ch(-4f, -14f, 0f).Hd(0f, 22f, 0f).Hand(0.2f, 0.55f, -0.65f, 0.6f).Wp(0.1f, 0.5f, -1f), 0.25f, 0.1f);
            DragonClipKey coil = back.Copy(-0.12f).Sp(-3f, -30f, 0f).Ch(-5f, -18f, 0f);
            DragonClipKey thr = Ft(K(0f).Sp(10f, 12f, 0f).Ch(6f, 8f, 0f).Hd(0f, -5f, 0f).Hand(0f, 0.15f, 1f, 1f).Wp(0f, 0.2f, 1f).Off(0f, -0.02f, 0.05f), 0.3f, 0.12f).Linear();
            DragonClipKey follow = Ft(K(0.18f).Sp(14f, 20f, 0f).Ch(8f, 12f, 0f).Hd(0f, -8f, 0f).Hand(-0.55f, -0.5f, 0.65f, 0.95f).Wp(-0.4f, -0.4f, 1f).Off(0f, -0.03f, 0.05f), 0.3f, 0.12f);
            return new DragonClipKey[] { K(-1f), back, coil, thr, follow, follow.Copy(0.35f), K(0.75f) };
        }

        // Bat (Punishing Bomb): two hands, cocked behind the right shoulder, level sweep, over the left shoulder.
        private static DragonClipKey[] SbBat()
        {
            DragonClipKey cock = Ft(K(-0.55f).Sp(6f, -28f, 0f).Ch(3f, -15f, 0f).Hd(0f, 24f, 0f).Hand(0.25f, 0.3f, -0.3f, 0.4f).Wp(0.35f, 0.7f, -0.6f).Two(-0.12f).Off(0f, -0.05f, 0f), 0.3f, 0.15f);
            DragonClipKey swing = Ft(K(0f).Sp(6f, 8f, 0f).Ch(3f, 5f, 0f).Hand(-0.1f, -0.1f, 1f, 0.95f).Wp(0f, 0f, 1f).Two(-0.12f).Off(0f, -0.05f, 0.03f), 0.32f, 0.12f).Linear();
            DragonClipKey follow = Ft(K(0.15f).Sp(4f, 30f, 0f).Ch(2f, 16f, 0f).Hd(0f, -10f, 0f).Hand(-0.75f, 0.45f, 0.2f, 0.85f).Wp(-0.7f, 0.6f, -0.4f).Two(-0.12f).Off(0f, -0.04f, 0.03f), 0.32f, 0.12f);
            return new DragonClipKey[] { K(-1f), cock, cock.Copy(-0.1f), swing, follow, follow.Copy(0.35f), K(0.75f) };
        }

        // Spin (SPAA): coil with the weapon across the body, then turn holding it level out to the right side.
        private static DragonClipKey SbSpinPose(float t)
        {
            return Ft(K(t).Sp(5f, 0f, 0f).Hand(1f, -0.05f, 0.35f, 1f).Wp(1f, 0f, 0.3f).Off(0f, -0.04f, 0f), 0.15f, 0.1f);
        }

        // v0.25.32 user: Whirlwind = atgeir spin made continuous. Both hands on the main weapon at waist height,
        // weapon level and sticking out to the right; the left hand grips the shaft nearer the body.
        private static DragonClipKey SbSpinPoseTwo(float t)
        {
            // v0.25.44 heavy atgeir sweep held through the turn: forward lean, low hips, both hands out at waist
            // height on the right, blade level and long, head turned into the spin.
            return Ft(K(t).Sp(14f, 8f, 0f).Ch(4f, 4f, 0f).Hd(-8f, -12f, 0f).Hand(0.75f, -0.32f, 0.5f, 1f).Wp(1f, -0.06f, 0.05f).Two(-0.35f).Off(0f, -0.14f, 0f), 0.28f, 0.16f);
        }

        private static DragonClipKey[] SbSpin(int turns, float turn)
        {
            return SbSpin(turns, turn, false);
        }

        private static DragonClipKey[] SbSpin(int turns, float turn, bool two)
        {
            List<DragonClipKey> k = new List<DragonClipKey>();
            k.Add(K(-1f));
            if (two) k.Add(Ft(K(-0.5f).Sp(12f, 30f, 0f).Ch(4f, 14f, 0f).Hd(0f, -16f, 0f).Hand(-0.55f, -0.3f, 0.45f, 0.85f).Wp(-1f, -0.08f, -0.2f).Two(-0.3f).Off(0f, -0.12f, 0f), 0.28f, 0.16f));   // coil: blade low across the left hip
            else k.Add(Ft(K(-0.5f).Sp(6f, 24f, 0f).Ch(3f, 12f, 0f).Hd(0f, -14f, 0f).Hand(-0.6f, 0f, 0.6f, 0.7f).Wp(-1f, 0f, 0f).Off(0f, -0.05f, 0f), 0.2f, 0.1f));
            k.Add(two ? SbSpinPoseTwo(0f) : SbSpinPose(0f));
            float t = 0f, yaw = 0f, q = turn * 0.25f;
            for (int i = 0; i < turns * 4; i++)
            {
                t += q; yaw += 90f;
                k.Add((two ? SbSpinPoseTwo(t).Rot(5f, yaw, 0f) : SbSpinPose(t).Rot(0f, yaw, 0f)).Linear());
            }
            k.Add(K(t + 0.35f).Rot(0f, yaw, 0f));
            return k.ToArray();
        }

        // Ground cast (GCA). v 0 Stonefang (hand + weapon down at the ground), 1 Stomp (foot only), 2 Snare, 3 mobile flick.
        private static DragonClipKey[] SbGround(int v)
        {
            if (v == 1)
            {
                // Stomp (anatomy): weight shifts onto the left leg, the right knee rises to hip height (thigh level,
                // knee bent ~90), torso stays upright, then the foot is driven flat into the ground and both knees
                // absorb it. Hands quiet.
                // v0.25.63 (user): exaggerated - the knee comes up past the hip, both arms flare out and up like the
                // roar emote, then the foot is driven down with the chest thrown forward and the arms flung wide.
                DragonClipKey raise = K(-0.55f).Sp(-8f, 0f, 0f).Ch(-6f, 0f, 0f).Hd(-6f, 0f, 0f).LL(0.35f, 0.1f, 0f, 0f).RL(0f, 0.06f, 0f, 0f).Lift(0.62f, 0f).Off(0f, 0.04f, 0f)
                    .Hand(0.62f, -0.32f, 0.66f, 0.64f).LHand(-0.62f, -0.32f, 0.66f, 0.64f).Wing();   // v0.25.85 chicken wing: upper arms raised out, forearms forward
                // v0.25.69 (user: flare = chicken wings, not arms stretched out): elbows bent and pushed out, hands at the hips.
                DragonClipKey peak = raise.Copy(-0.15f).Lift(0.6f, 0f).Hand(0.65f, -0.3f, 0.68f, 0.66f).LHand(-0.65f, -0.3f, 0.68f, 0.66f).Wing();
                DragonClipKey hit = K(0f).Sp(20f, 0f, 0f).Ch(10f, 0f, 0f).Hd(-12f, 0f, 0f).LL(0.35f, 0.14f, 0f, 0f).RL(-0.05f, 0.12f, 0f, 0f).Lift(0f, 0f).Off(0f, -0.16f, 0f)
                    .Hand(0.6f, -0.45f, 0.62f, 0.68f).LHand(-0.6f, -0.45f, 0.62f, 0.68f).Wing().Linear();
                DragonClipKey stAfter = hit.Copy(0.3f); stAfter.Lin = false;
                return new DragonClipKey[] { K(-1f), raise, peak, hit, stAfter, K(0.65f) };
            }
            if (v == 2)
            {
                DragonClipKey sn = Ft(K(0f).Sp(18f, 0f, 0f).Ch(8f, 0f, 0f).Hd(8f, 0f, 0f).Hand(0f, -0.9f, 0.45f, 1f).Off(0f, -0.18f, 0f), 0.35f, 0.2f);
                return new DragonClipKey[] { K(-1f), sn, sn.Copy(0.15f), K(0.45f) };
            }
            if (v == 3)
            {
                DragonClipKey f = K(0f).Hd(6f, 0f, 0f).Hand(0f, -0.5f, 0.85f, 0.9f).Wp(0f, -0.6f, 0.8f);
                return new DragonClipKey[] { K(-1f), K(-0.5f).Hand(0.1f, 0f, 0.8f, 0.7f), f, f.Copy(0.12f), K(0.35f) };
            }
            DragonClipKey load = K(-0.6f).Hd(4f, 0f, 0f).Hand(0.1f, 0.1f, 0.7f, 0.6f).Wp(0f, 0.4f, 1f);
            DragonClipKey press = Ft(K(0f).Sp(14f, 0f, 0f).Ch(6f, 0f, 0f).Hd(8f, 0f, 0f).Hand(0f, -0.8f, 0.6f, 1f).Wp(0f, -0.9f, 0.4f).Off(0f, -0.1f, 0f), 0.25f, 0.12f).Linear();
            DragonClipKey after = press.Copy(0.2f); after.Lin = false;
            return new DragonClipKey[] { K(-1f), load, press, after, K(0.6f) };
        }

        // Remote command (RCA): main hand points at the target; v 1 open palm, 2 pinch, 3 pull.
        private static DragonClipKey[] SbCommand(int v)
        {
            DragonClipKey pt = K(0f).Sp(0f, 8f, 0f).Ch(0f, 4f, 0f).Hd(0f, -4f, 0f).Hand(0f, 0f, 1f, 1f).Wp(0f, 0f, 1f);
            if (v == 1) pt = pt.RH(-30f, 0f, 0f);
            if (v == 2) pt = pt.RH(25f, 0f, 0f).Hand(0f, 0f, 1f, 0.85f);
            if (v == 3) pt = pt.Hand(0.1f, -0.1f, 1f, 0.45f);
            return new DragonClipKey[] { K(-1f), K(-0.4f).Hand(0.2f, -0.2f, 0.8f, 0.7f), pt, pt.Copy(0.15f), K(0.45f) };
        }

        // v0.25.88 JSAA ONLY (Bonecrusher + Electric Smite): retain the approved
        // airborne boxing guard and hammer wind-up. Touchdown is visually prepared
        // during the last part of the fall, NOT started after contact. The held
        // Phase=0 pose still remains airborne during long/cliff descents; only
        // JsaLandingApproach() moves it into the landing as terrain approaches.
        private static DragonClipKey[] SbOlympic(bool brutal)
        {
            float d = brutal ? 1.15f : 1f;
            DragonClipKey load = Ft(K(-0.93f).Sp(16f * d, -10f, 0f).Ch(6f, -6f, 0f).Hd(-12f, 0f, 0f)
                .Hand(0.45f, 0.1f, -0.45f, 0.6f).Off(0f, -0.15f * d, 0f), 0.2f, 0.1f);

            // AIR: left fist in a bent chest/face-height boxing guard; right fist
            // and weapon cocked high and slightly right, ready for the bonk.
            DragonClipKey launch = K(-0.72f).Sp(-4f, 0f, 0f).Ch(-4f, 0f, 0f).Hd(-14f, 0f, 0f)
                .Hand(1f, 0.35f, 0.1f, 0.92f).Wp(1f, 0.45f, 0.1f)   // v0.25.100 (user): main arm stretched OUTWARD, elbow only slightly bent
               
                .LHand(-0.17f, 0.83f, 0.57f, 0.69f)
                .Rot(45f, 0f, 0f).Off(0f, 0.06f, 0f);
            DragonClipKey roll0 = launch.Copy(-0.6f).Rot(62f, 0f, 0f).Sn(30f);
            DragonClipKey roll1 = launch.Copy(-0.32f).Rot(62f, 0f, 0f).Sn(360f);
            DragonClipKey poised = launch.Copy(0f).Rot(55f, 0f, 0f).Sn(360f);

            // One continuous movement into a fully formed kneel AT impact:
            // left foot forward/knee up, right knee low and behind, left arm
            // stretched straight left, right hand slamming straight down.
            DragonClipKey impact = Ft(K(0.01f).Sp(32f, 0f, 0f).Ch(16f, 0f, 0f).Hd(-22f, 0f, 0f)
                .Hand(0.08f, -1f, 0.30f, 0.95f).Wp(0.08f, -1f, 0.23f)
                .LHand(-1f, -0.08f, 0.16f, 0.97f)
                .Rot(6f, 0f, 0f).Off(0f, -0.50f * d, 0.06f).Sn(360f), 0.88f, 0.88f).Linear();
            // Hold the actual impact silhouette long enough for the player to
            // SEE it during VFX, then rise (no second preparatory attack).
            DragonClipKey settle = impact.Copy(brutal ? 0.43f : 0.35f).Off(0f, -0.51f * d, 0.06f);
            settle.Lin = false;
            DragonClipKey rec = Ft(K(brutal ? 0.77f : 0.68f).Sp(8f, 0f, 0f).Hd(-6f, 0f, 0f)
                .Hand(0.35f, -0.45f, 0.35f, 0.6f).Off(0f, -0.06f, 0f).Sn(360f), 0.3f, 0.2f);
            return new DragonClipKey[] { K(-1f), load, launch, roll0, roll1, poised,
                impact, settle, rec, K(brutal ? 1.07f : 0.95f).Sn(360f) };
        }

        private static void BuildStoryboardClips(Dictionary<string, DragonClipKey[]> c)
        {
            string[] bdca = { "cleric_wave", "cleric_ray", "cleric_light", "cleric_intervention", "cleric_crucible", "cleric_wave_ally", "wiz_clockwork", "wiz_nova", "sm_guidance", "rg_tailwind", "rg_vigil" };
            for (int i = 0; i < bdca.Length; i++) c[bdca[i]] = SbBdca(false);
            c["merc_roar"] = SbBdca(true);
            string[] ssca = { "cleric_rs", "cleric_rs_asc", "cleric_goddess", "cleric_relic", "cleric_holy_relic", "cleric_judgement", "cleric_tempest", "sorc_glacial", "sorc_glacial_asc" };
            for (int i = 0; i < ssca.Length; i++) c[ssca[i]] = SbSsca(false);
            c["wiz_meteor"] = SbSsca(true);
            c["wiz_cataclysm"] = SbSsca(true);
            c["warrior_heavy"] = SbSlash(1f, 0.15f, false, false);
            c["merc_heavy_asc"] = SbSlash(1.3f, 0.2f, false, false);
            c["sm_slash_a"] = SbSlash(0f, 0.04f, false, false);
            c["sm_slash_b"] = SbSlash(0f, 0.04f, false, false);
            c["sm_moon_finisher"] = SbSlash(1.3f, 0.25f, false, false);
            c["sm_crescent"] = SbSlash(1f, 0.15f, false, false);
            c["sm_blade_storm"] = SbSlash(0f, 0.1f, false, false);
            c["sm_halfmoon"] = SbSlash(0.6f, 0.1f, false, true);
            c["sm_halfmoon_2"] = SbSlash(0.6f, 0.1f, false, false);
            // v0.25.86 (user: arm clipping into the body in the last stance): no two-handed grip behind the hip - the sword is
            // held low and forward on the right, one-handed, the off hand braced in front of the chest.
            // v0.25.89: modest, anatomy-safe finisher anticipation. The vanilla grip owns BOTH arms;
            // only a little torso coil / knee flex is layered so no wrists fold through the rib cage.
            DragonClipKey hmStance = Ft(K(0f).Sp(5f, -10f, 0f).Ch(2f, -5f, 0f)
                .Off(0f, -0.045f, 0f), 0.2f, 0.15f);
            c["sm_halfmoon_stance"] = new DragonClipKey[] { K(-1f), hmStance, hmStance.Copy(0.15f), K(0.35f) };
            c["sm_halfmoon_finisher"] = SbSlash(1.2f, 0.15f, true, true);
            c["warrior_impact_wave"] = SbGolf(0.25f);
            c["merc_seismic"] = SbGolf(0.25f);
            c["wiz_greatblade"] = SbSlam(0.25f, false);
            c["wiz_greatblade_slam"] = SbSlam(0.15f, true);
            c["warrior_punch"] = SbThrust(0.15f);
            c["sm_thrust"] = SbThrust(0.4f);
            c["cleric_zap"] = SbCast();
            c["sorc_flame"] = SbCast();
            c["hw_gravity_blast"] = SbCast();
            c["cleric_hammer"] = SbThrow();
            DragonClipKey catchK = K(0.08f).Hand(0.1f, 0.05f, 1f, 0.8f).Off(0f, -0.02f, -0.04f);
            c["cleric_hammer_call"] = new DragonClipKey[] { K(-1f), K(0f), catchK, catchK.Copy(0.2f), K(0.5f) };
            c["merc_bomb"] = SbBat();
            c["sorc_stonefang"] = SbGround(0);
            c["merc_stomp"] = SbGround(1);
            // v0.25.111 Mixamo overlay helper: no bone offsets, no foot planting, no weapon aim - only the
            // left hand on the main weapon's handle (the Mixamo spin swings one-handed).
            {
                DragonClipKey g0 = K(-1f); g0.NoPlant = true; g0.NoAim = true;
                DragonClipKey g1 = K(0f).Two(-0.13f);
                c["mx_grip"] = new DragonClipKey[] { g0, g1, g1.Copy(1.2f), K(1.4f) };
            }
            c["rg_trap"] = SbGround(2);
            c["sorc_stonefang_asc"] = SbGround(3);
            c["merc_circle"] = SbSpin(1, 0.4f);
            c["merc_circle_2"] = SbSpin(1, 0.35f);
            c["sm_eclipse"] = SbSpin(1, 0.4f);
            DragonClipKey br = Ft(K(-0.6f).Sp(6f, -20f, 0f).Ch(3f, -10f, 0f).Hd(0f, 18f, 0f).Hand(-0.1f, 0f, 1f, 0.85f).Wp(0f, 0f, 1f).Two(0.35f).Off(0f, -0.05f, 0f), 0.3f, 0.15f);
            DragonClipKey fire = br.Copy(0f).Off(0f, -0.05f, -0.06f).Hand(-0.1f, 0.05f, 1f, 0.75f);
            c["wiz_railcannon"] = new DragonClipKey[] { K(-1f), br, br.Copy(-0.1f), fire, br.Copy(0.25f), K(0.6f) };
            c["wiz_railcannon_hold"] = new DragonClipKey[] { K(-1f), br, br.Copy(0f), K(0.25f) };
            c["hw_point"] = SbCommand(0); c["hw_command"] = SbCommand(0); c["hw_rift_echo"] = SbCommand(0); c["hw_rupture"] = SbCommand(2);
            c["hw_open"] = SbCommand(1); c["hw_stop"] = SbCommand(1); c["hw_pinch"] = SbCommand(2); c["hw_afterimage"] = SbCommand(1);
            c["hw_rift_walker"] = SbCommand(1); c["wiz_gravity"] = SbCommand(3);
            c["olympic_hero"] = SbOlympic(false);
            c["olympic_hero_brutal"] = SbOlympic(true);
            c["sm_crescent_asc"] = c["sm_crescent"];
            c["sm_crescent_asc2"] = c["sm_crescent"];   // v0.25.51 follow-up swing (custom fallback)
            // v0.25.41 Blade Storm = Vergil's Judgement Cut: crouched iai stance with the blade held back at the
            // left hip (sheathed), a blink-fast draw that ends with the arm out to the right, a held pose,
            // then the slow sheathe back to the hip and a small "click" settle.
            DragonClipKey jcStance = Ft(K(-0.6f).Sp(14f, -25f, 0f).Ch(4f, -10f, 0f).Hd(-6f, 22f, 0f).Hand(-0.38f, -0.42f, 0.45f, 0.62f).Wp(-0.8f, -0.45f, -0.4f).Off(0f, -0.12f, 0f), 0.35f, 0.25f);
            DragonClipKey jcDraw = Ft(K(0f).Sp(10f, 30f, 0f).Ch(4f, 14f, 0f).Hd(-4f, -10f, 0f).Hand(1f, 0.1f, 0.35f, 1f).Wp(1f, 0.05f, 0.2f).Off(0f, -0.1f, 0.06f), 0.4f, 0.25f).Linear();
            DragonClipKey jcHold = jcDraw.Copy(0.18f); jcHold.Lin = false;
            DragonClipKey jcReturn = Ft(K(0.3f).Sp(8f, 10f, 0f).Ch(3f, 4f, 0f).Hand(0.25f, -0.15f, 0.6f, 0.7f).Wp(0.2f, 0f, 1f).Off(0f, -0.08f, 0f), 0.35f, 0.2f);
            DragonClipKey jcSheathe = Ft(K(0.75f).Sp(6f, -12f, 0f).Ch(2f, -6f, 0f).Hd(-4f, 8f, 0f).Hand(-0.36f, -0.42f, 0.42f, 0.62f).Wp(-0.8f, -0.45f, -0.4f).Off(0f, -0.06f, 0f), 0.3f, 0.2f);
            DragonClipKey jcClick = jcSheathe.Copy(0.83f).Sp(3f, -10f, 0f).Off(0f, -0.03f, 0f);
            c["sm_blade_storm"] = new DragonClipKey[] { K(-1f), jcStance, jcDraw, jcHold, jcReturn, jcSheathe, jcClick, K(1.1f) };
            // v0.25.36 Shield Charge finisher fallback (if the game has no mace_secondary): one-handed overhead
            // hammer slam with the main hand, shield arm untouched.
            DragonClipKey hsUp = Ft(K(-0.55f).Sp(-4f, -6f, 0f).Ch(-6f, -4f, 0f).Hd(-10f, 0f, 0f).Hand(0.25f, 1f, -0.15f, 0.95f).Wp(0.05f, 0.6f, -0.8f), 0.3f, 0.15f);
            DragonClipKey hsDown = Ft(K(0f).Sp(22f, 4f, 0f).Ch(10f, 2f, 0f).Hd(6f, 0f, 0f).Hand(0.05f, -0.5f, 0.85f, 1f).Wp(0f, -0.75f, 0.65f).Off(0f, -0.12f, 0.06f), 0.4f, 0.15f).Linear();
            c["cleric_hammer_slam"] = new DragonClipKey[] { K(-1f), hsUp, hsUp.Copy(-0.15f), hsDown, hsDown.Copy(0.2f), K(0.6f) };
            // v0.25.35 Frenzied Charge (user): sword held out in front, point forward, the whole charge.
            DragonClipKey fcDraw = Ft(K(-0.5f).Sp(6f, -8f, 0f).Hand(0.35f, -0.25f, -0.05f, 0.6f).Wp(0.1f, 0.15f, 1f).Off(0f, -0.06f, 0f), 0.25f, 0.2f);
            DragonClipKey fcHold = K(0f).Sp(12f, 0f, 0f).Ch(4f, 0f, 0f).Hd(-10f, 0f, 0f).Hand(0.12f, 0.05f, 1f, 0.88f).Wp(0f, 0.05f, 1f);
            c["sm_charge"] = new DragonClipKey[] { K(-1f), fcDraw, fcHold, fcHold.Copy(0.15f), K(0.4f) };
            c["sm_charge"][0].NoPlant = true;
            DragonClipKey[] oh = c["olympic_hero"];
            List<DragonClipKey> land = new List<DragonClipKey>();
            land.Add(oh[6].Copy(-1f));
            for (int i = 6; i < oh.Length; i++) land.Add(oh[i]);
            c["olympic_land"] = land.ToArray();

            // Shield Charge (FTAA shield variant): shield up (Valheim block pose), weapon tucked behind it; the
            // finisher whips the mace up and smashes it down at head height.
            DragonClipKey scBrace = Ft(K(-0.5f).Sp(10f, 0f, 0f).Ch(4f, 0f, 0f).Hd(-8f, 0f, 0f).Hand(-0.3f, -0.4f, 0.45f, 0.55f).Wp(0.15f, 0.85f, 0.25f).Off(0f, -0.05f, 0f), 0.2f, 0.1f);
            DragonClipKey scDrive = K(0f).Sp(14f, -4f, 0f).Ch(6f, -4f, 0f).Hd(-14f, 0f, 0f).Hand(-0.3f, -0.4f, 0.45f, 0.55f).Wp(0.15f, 0.85f, 0.25f).Rot(10f, 0f, 0f);
            DragonClipKey scRaise = K(0.07f).Sp(3f, -14f, 0f).Ch(-4f, -8f, 0f).Hd(-8f, 8f, 0f).Hand(0.15f, 1f, -0.25f, 0.95f).Wp(0.1f, 0.4f, -1f).Rot(4f, 0f, 0f);
            DragonClipKey scSmash = Ft(K(0.17f).Sp(20f, 16f, 0f).Ch(10f, 10f, 0f).Hd(-12f, -8f, 0f).Hand(0f, 0.05f, 1f, 1f).Wp(0f, -0.3f, 1f).Rot(10f, 4f, 0f).Off(0f, -0.08f, 0.18f), 0.35f, 0.15f).Linear();
            DragonClipKey scHold = scSmash.Copy(0.32f); scHold.Lin = false;
            DragonClipKey scRec = K(0.62f).Sp(4f, 0f, 0f).Hand(0.3f, -0.6f, 0.3f, 0.55f).Off(0f, -0.02f, 0f);
            c["cleric_charge"] = new DragonClipKey[] { K(-1f), scBrace, scDrive, scRaise, scSmash, scHold, scRec, K(0.95f) };

            // Angel Comet (EMA dive): crouch, jump with the arm thrown up, wings on the rise, tip over into an
            // inverted head-first dive (arm leading to the ground), flip upright into the hero landing.
            DragonClipKey inv = K(0f).Sp(3f, 0f, 0f).Hd(-10f, 0f, 0f).Hand(0f, 1f, 0.1f, 1f).Rot(165f, 0f, 0f);
            DragonClipKey hero = Ft(K(0.16f).Sp(30f, 0f, 0f).Ch(14f, 0f, 0f).Hd(-28f, 0f, 0f).Hand(0.05f, -1f, 0.35f, 0.97f).Rot(366f, 0f, 0f).Off(0f, -0.38f, 0.05f), 0.75f, 0.8f);
            c["angel_comet"] = new DragonClipKey[] {
                K(-1f),
                Ft(K(-0.92f).Sp(14f, 0f, 0f).Ch(6f, 0f, 0f).Hand(0.3f, -0.6f, 0.3f, 0.55f).Off(0f, -0.12f, 0f), 0.15f, 0.1f),
                K(-0.75f).Ch(-8f, 0f, 0f).Hd(-20f, 0f, 0f).Hand(0.05f, 1f, 0.1f, 1f).Rot(-6f, 0f, 0f),
                K(-0.35f).Sp(-10f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-24f, 0f, 0f).Hand(1f, 0.2f, -0.3f, 1f).Rot(-10f, 0f, 0f),
                K(-0.1f).Hd(-20f, 0f, 0f).Hand(0f, 1f, 0.2f, 1f).Rot(70f, 0f, 0f),
                inv, hero, hero.Copy(0.4f).Off(0f, -0.4f, 0.05f),
                Ft(K(0.7f).Sp(8f, 0f, 0f).Hd(-6f, 0f, 0f).Hand(0.35f, -0.45f, 0.35f, 0.6f).Rot(360f, 0f, 0f).Off(0f, -0.06f, 0f), 0.3f, 0.2f),
                K(1.0f).Rot(360f, 0f, 0f)
            };

            // ---- v0.25.24 the remaining clips, rebuilt with hand targets
            DragonClipKey[] cock = SbSlash(0f, 0.04f, false, false);
            c["sm_ready"] = new DragonClipKey[] { K(-1f), cock[1].Copy(-0.4f), cock[1].Copy(0f), cock[1].Copy(0.3f), K(0.6f) };
            // Grand Cross (two diagonal cuts making an X, main hand, shield quiet).
            c["cleric_cross_1"] = new DragonClipKey[] {
                K(-1f),
                K(-0.35f).Sp(-2f, -16f, 0f).Ch(-3f, -8f, 0f).Hd(0f, 8f, 0f).Hand(0.45f, 0.85f, 0.1f, 0.9f).Wp(0.3f, 1f, -0.3f),
                Ft(K(0f).Sp(12f, 14f, 0f).Ch(6f, 8f, 0f).Hand(-0.55f, -0.6f, 0.6f, 1f).Wp(-0.6f, -0.6f, 0.6f).Off(0f, -0.04f, 0.03f), 0.25f, 0.1f).Linear(),
                Ft(K(0.08f).Sp(12f, 14f, 0f).Ch(6f, 8f, 0f).Hand(-0.55f, -0.6f, 0.6f, 1f).Wp(-0.6f, -0.6f, 0.6f).Off(0f, -0.04f, 0.03f), 0.25f, 0.1f)
            };
            c["cleric_cross_2"] = new DragonClipKey[] {
                K(-1f),
                K(-0.4f).Sp(-2f, 14f, 0f).Ch(-3f, 8f, 0f).Hand(-0.45f, 0.85f, 0.15f, 0.9f).Wp(-0.3f, 1f, -0.3f),
                Ft(K(0f).Sp(12f, -16f, 0f).Ch(6f, -10f, 0f).Hand(0.55f, -0.6f, 0.6f, 1f).Wp(0.6f, -0.6f, 0.6f).Off(0f, -0.04f, 0.03f), 0.25f, 0.1f).Linear(),
                Ft(K(0.3f).Sp(12f, -16f, 0f).Ch(6f, -10f, 0f).Hand(0.55f, -0.6f, 0.6f, 1f).Wp(0.6f, -0.6f, 0.6f).Off(0f, -0.04f, 0.03f), 0.25f, 0.1f),
                K(0.65f)
            };
            // Ascended Bonecrusher aftershock: the fist pounds the crater again.
            DragonClipKey af = Ft(K(0f).Sp(26f, 0f, 0f).Ch(12f, 0f, 0f).Hd(-20f, 0f, 0f).Hand(0.05f, -1f, 0.35f, 0.97f).Off(0f, -0.3f, 0.04f), 0.6f, 0.6f).Linear();
            DragonClipKey afHold = af.Copy(0.2f); afHold.Lin = false;
            c["merc_aftershock"] = new DragonClipKey[] { K(-1f), Ft(K(-0.5f).Sp(10f, 0f, 0f).Hand(0.25f, 0.35f, 0.3f, 0.7f).Off(0f, -0.06f, 0f), 0.3f, 0.2f), af, afHold, K(0.55f) };
            // Procs = small torso accents only (never the arms): Holy Shockwave, Overcharge, Fury.
            c["cleric_parry_burst"] = new DragonClipKey[] { K(-1f), K(0f).Sp(5f, -10f, 0f).Ch(3f, -6f, 0f).Off(0f, -0.02f, 0.03f), K(0.35f) };
            c["wiz_overcharge"] = new DragonClipKey[] { K(-1f), K(0f).Ch(-7f, 0f, 0f).Hd(-10f, 0f, 0f), K(0.3f) };
            c["merc_fury_accent"] = new DragonClipKey[] { K(-1f), K(0f).Ch(-7f, 0f, 0f).Hd(-8f, 0f, 0f), K(0.25f) };
            // Phoenix Rise (storyboard 20): brace, rise, restore with the main arm opening a little, continue.
            c["cleric_rise"] = new DragonClipKey[] { K(-1f), K(-0.5f).Sp(5f, 0f, 0f).Ch(7f, 0f, 0f).Hd(6f, 0f, 0f), K(0f).Ch(-8f, 0f, 0f).Hd(-12f, 0f, 0f).Hand(0.65f, -0.45f, 0.3f, 0.9f), K(0.3f) };
            // ---- RANGER (storyboards 17-19): bow in the LEFT hand, string hand at the cheek, side-on torso.
            c["rg_power"] = new DragonClipKey[] { K(-1f), RgDraw(-0.4f), RgDraw(-0.05f), RgLoose(0f).Off(0f, 0f, -0.04f), RgLoose(0.2f), K(0.55f) };
            c["rg_heavy"] = new DragonClipKey[] { K(-1f), Ft(RgDraw(-0.4f).Off(0f, -0.05f, 0f), 0.25f, 0.15f), Ft(RgDraw(-0.05f).Off(0f, -0.05f, 0f), 0.25f, 0.15f), Ft(RgLoose(0f).Ch(-6f, -12f, 0f).Off(0f, -0.05f, -0.06f), 0.25f, 0.15f), Ft(RgLoose(0.22f).Off(0f, -0.04f, 0f), 0.25f, 0.15f), K(0.55f) };
            c["rg_kneel"] = new DragonClipKey[] { K(-1f), Ft(RgDraw(-0.5f).Off(0f, -0.3f, 0f), 0.5f, 0.9f), Ft(RgDraw(-0.05f).Off(0f, -0.3f, 0f), 0.5f, 0.9f), Ft(RgLoose(0f).Off(0f, -0.3f, -0.02f), 0.5f, 0.9f), Ft(RgLoose(0.25f).Off(0f, -0.3f, 0f), 0.5f, 0.9f), K(0.6f) };
            c["rg_ballista"] = new DragonClipKey[] { K(-1f), Ft(RgDraw(-0.5f).Off(0f, -0.08f, 0f), 0.3f, 0.2f), Ft(RgDraw(0f).Hand(-0.45f, 0.12f, -0.15f, 0.42f).Off(0f, -0.1f, -0.02f), 0.3f, 0.2f), Ft(RgLoose(0.06f).Ch(-10f, -12f, 0f).Off(0f, -0.08f, -0.12f), 0.3f, 0.2f), Ft(RgLoose(0.3f).Off(0f, -0.06f, -0.04f), 0.3f, 0.2f), K(0.7f) };
            c["rg_trick"] = new DragonClipKey[] { K(-1f), RgDraw(-0.5f).Sp(2f, -30f, 0f).Ch(0f, -16f, 0f), RgLoose(0f).Sp(0f, -10f, 0f), RgLoose(0.15f), K(0.45f) };
            c["rg_split_a"] = new DragonClipKey[] { K(-1f), Ft(RgDraw(-0.5f).Sp(4f, -36f, 0f).Off(0f, -0.06f, 0f), 0.3f, 0.15f), Ft(RgLoose(0f).Sp(4f, 0f, 0f).Off(0f, -0.06f, 0f), 0.3f, 0.15f), Ft(RgDraw(0.3f).Sp(4f, -10f, 0f).Off(0f, -0.06f, 0f), 0.3f, 0.15f), K(0.6f) };
            c["rg_split_b"] = new DragonClipKey[] { K(-1f), Ft(RgDraw(-0.5f).Sp(4f, -4f, 0f).Off(0f, -0.06f, 0f), 0.3f, 0.15f), Ft(RgLoose(0f).Sp(4f, -36f, 0f).Off(0f, -0.06f, 0f), 0.3f, 0.15f), Ft(RgDraw(0.3f).Sp(4f, -20f, 0f).Off(0f, -0.06f, 0f), 0.3f, 0.15f), K(0.6f) };
            c["rg_sky"] = new DragonClipKey[] { K(-1f), RgSky(-0.5f, false), RgSky(-0.05f, false), RgSky(0f, true), RgSky(0.2f, true), K(0.6f) };
            c["rg_starfall"] = new DragonClipKey[] { K(-1f), RgSky(-0.8f, false), RgSky(-0.05f, false), RgSky(0f, true), K(0.45f) };
            // v0.25.32 user: Skyfall hover = a normal bow shot pose (upright, string hand at the cheek, no arm through the body).
            DragonClipKey hov = RgDraw(0f).Hd(10f, 20f, 0f).Rot(24f, 0f, 0f);   // v0.25.38 leaning over the target zone
            c["rg_hover"] = new DragonClipKey[] { K(-1f), hov, K(0.3f) };
            c["rg_spin"] = Join(K(-1f), RgDraw(-0.3f), Spin360(RgDraw(0f), 0f, 0.4f, 0.0f), RgLoose(0.45f), K(0.75f));
            // v0.25.31 user: Cyclone Arrow = a normal bow shot (no body spin).
            c["rg_shot"] = new DragonClipKey[] { K(-1f), Ft(RgDraw(-0.6f), 0.25f, 0.15f), Ft(RgDraw(-0.05f), 0.25f, 0.15f), Ft(RgLoose(0f), 0.25f, 0.15f), Ft(RgLoose(0.25f), 0.25f, 0.15f), K(0.55f) };
            DragonClipKey sd = K(0f).Hd(-24f, 0f, 0f).Hand(0.2f, -0.6f, -0.8f, 1f).LHand(-0.2f, -0.6f, -0.8f, 1f).Rot(70f, 0f, 0f);
            c["rg_dive"] = new DragonClipKey[] { K(-1f), sd, sd.Copy(0.15f), K(0.45f) };
            DragonClipKey tk = K(0f).Sp(20f, 0f, 0f).Ch(10f, 0f, 0f).Hand(0f, -0.5f, 0.6f, 0.6f).LHand(0f, -0.5f, 0.6f, 0.6f);
            c["rg_backflip"] = new DragonClipKey[] {
                K(-1f), tk.Copy(0f),
                tk.Copy(0.125f).Rot(-90f, 0f, 0f).Linear(), tk.Copy(0.25f).Rot(-180f, 0f, 0f).Linear(),
                tk.Copy(0.375f).Rot(-270f, 0f, 0f).Linear(), tk.Copy(0.5f).Rot(-360f, 0f, 0f).Linear(),
                K(0.65f).Rot(-360f, 0f, 0f)
            };
            c["rg_tumble"] = new DragonClipKey[] {
                K(-1f), tk.Copy(0f),
                tk.Copy(0.075f).Rot(-90f, 0f, 0f).Linear(),
                RgDraw(0.15f).Rot(-180f, 0f, 0f).Linear(),
                RgLoose(0.225f).Rot(-270f, 0f, 0f).Linear(),
                RgLoose(0.3f).Rot(-360f, 0f, 0f).Linear(),
                K(0.5f).Rot(-360f, 0f, 0f)
            };

            // No euler arm offsets on an arm a hand target drives (no double bending).
            HashSet<DragonClipKey> seen = new HashSet<DragonClipKey>();
            foreach (KeyValuePair<string, DragonClipKey[]> kv in c)
                for (int i = 0; i < kv.Value.Length; i++)
                {
                    DragonClipKey k = kv.Value[i];
                    if (!seen.Add(k)) continue;
                    if (k.HW > 0f) { k.B[4] = Vector3.zero; k.B[5] = Vector3.zero; }
                    if (k.LW > 0f) { k.B[7] = Vector3.zero; k.B[8] = Vector3.zero; }
                }
        }

        // ================================================================================== v0.25.21
        // USER ANIMATION GUIDE (overrides every earlier animation decision). Universal: the main-hand item points
        // where the hand points (driver), the off hand / shield stays quiet unless a TWO-HANDED grip is stated,
        // no flailing arms or legs. Family builders below, mapped to every skill clip in BuildGuideClips.
        // Body frame for Wp: x right, y up, z forward. Main hand = RIGHT. Slashes sweep LEFT -> RIGHT.
        // ==================================================================================

        // BDCA - buff / debuff casting: standing, the main hand (and its weapon) rises to cast. roar = Battlecry.
        private static DragonClipKey[] Bdca(bool roar)
        {
            DragonClipKey lift = K(-0.5f).Ch(-2f, 0f, 0f).Hd(-4f, 0f, 0f).RA(-120f, 0f, -10f).RF(-45f, 0f, 0f);
            DragonClipKey top = K(0f).Sp(-3f, 0f, 0f).Ch(-6f, 0f, 0f).Hd(-12f, 0f, 0f).RA(-172f, 0f, -6f).RF(-5f, 0f, 0f);
            if (roar) top = top.Ch(-12f, 0f, 0f).Hd(-24f, 0f, 0f);
            return new DragonClipKey[] { K(-1f), lift, top, top.Copy(0.35f), K(0.75f) };
        }

        // SSCA - sky summon: main hand gathers at the chest, rises straight to the sky, then the arm ends stretched
        // out IN FRONT, the weapon pointing ahead / at the ground where the spell lands. charged = hold at the sky.
        private static DragonClipKey[] Ssca(bool charged)
        {
            DragonClipKey gather = K(-0.65f).Hd(4f, 0f, 0f).RA(-50f, 0f, 25f).RF(-100f, 0f, 0f);
            DragonClipKey sky = K(charged ? 0f : -0.2f).Sp(-4f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-20f, 0f, 0f).RA(-172f, 0f, -6f).RF(-4f, 0f, 0f);
            float r = charged ? 0.14f : 0f;
            DragonClipKey point = K(r).Sp(12f, 8f, 0f).Ch(6f, 4f, 0f).Hd(6f, -4f, 0f).RA(-65f, 0f, -4f).RF(-2f, 0f, 0f).Off(0f, -0.04f, 0.04f).LL(0.2f, 0.04f, 0.2f, 0f).RL(-0.1f, 0.04f, 0.1f, 0f);
            DragonClipKey hold = point.Copy(r + 0.3f);
            if (charged) return new DragonClipKey[] { K(-1f), gather.Copy(-0.6f), sky, point, hold, K(r + 0.8f) };
            return new DragonClipKey[] { K(-1f), gather, sky, point, hold, K(0.8f) };
        }

        // SAA - two-handed slash, LEFT -> RIGHT. The blade is cocked high at the left shoulder (non-attacking
        // return), extends to the left, sweeps across the front at the impact and finishes on the right.
        // heavy = bigger torso turn + deeper stance; spin = CSAFA (spins clockwise with a left->right finish).
        private static DragonClipKey Saa(DragonClipKey k, float depth) { return Stance(k, depth).Two(-0.12f); }

        private static DragonClipKey[] SlashKeys(float heavy, float hold, bool spin)
        {
            float a = 1f + 0.35f * heavy;
            float d = 0.6f + 0.4f * heavy;
            DragonClipKey cock = Saa(K(-0.45f).Sp(6f, 22f * a, 0f).Ch(4f, 14f * a, 0f).Hd(0f, -14f, 0f).RA(-110f, 0f, 45f).RF(-95f, 0f, 0f).Off(0f, -0.06f * a, 0f), d);
            DragonClipKey left = Saa(K(-0.12f).Sp(8f, 30f * a, 0f).Ch(4f, 18f * a, 0f).Hd(0f, -18f, 0f).RA(-85f, 0f, 62f).RF(-18f, 0f, 0f).Off(0f, -0.08f * a, 0f), d);
            DragonClipKey front = Saa(K(0f).Sp(8f, 0f, 0f).Ch(4f, 0f, 0f).RA(-90f, 0f, 0f).RF(-4f, 0f, 0f).Off(0f, -0.08f * a, 0.04f), d).Linear();
            front = front.LL(0.3f * d, 0.12f, 0.33f * d, 0f);
            DragonClipKey right = Saa(K(0.11f).Sp(8f, -30f * a, 0f).Ch(4f, -18f * a, 0f).Hd(0f, 14f, 0f).RA(-84f, 0f, -72f).RF(-10f, 0f, 0f).Off(0f, -0.08f * a, 0.04f), d).Linear();
            right = right.LL(0.3f * d, 0.12f, 0.33f * d, 0f);
            if (spin)
            {
                // CSAFA: the sweep carries the whole body round once, clockwise.
                front = front.Rot(0f, 90f, 0f);
                DragonClipKey r2 = right.Copy(0.11f).Rot(0f, 180f, 0f); r2.Lin = true;
                DragonClipKey r3 = right.Copy(0.22f).Rot(0f, 270f, 0f); r3.Lin = true;
                DragonClipKey r4 = right.Copy(0.33f).Rot(0f, 360f, 0f); r4.Lin = true;
                DragonClipKey sHold = right.Copy(0.33f + hold).Rot(0f, 360f, 0f); sHold.Lin = false;
                return new DragonClipKey[] { K(-1f), cock, left, front, r2, r3, r4, sHold, K(0.33f + hold + 0.45f).Rot(0f, 360f, 0f) };
            }
            DragonClipKey after = right.Copy(0.11f + hold); after.Lin = false;
            return new DragonClipKey[] { K(-1f), cock, left, front, right, after, K(0.11f + hold + 0.45f) };
        }

        // CSAA pull: both hands drawn back at the right hip, blade level and pointing forward like a hard poke.
        private static DragonClipKey CsaaPull(float t)
        {
            return K(t).Sp(10f, -24f, 0f).Ch(4f, -12f, 0f).Hd(0f, 18f, 0f).RA(25f, 0f, -12f).RF(-110f, 0f, 0f).Off(0f, -0.12f, -0.04f).LL(0.35f, 0.1f, 0.4f, 0f).RL(0f, 0.1f, 0.35f, 0f).Two(-0.12f).Wp(0f, 0f, 1f);
        }

        // GAA - golf: two hands, baseball stance (weapon cocked up behind the right shoulder, side-on, knees
        // bent), then the swing makes a U: down to the ground in front at the impact, up and out to the left.
        private static DragonClipKey[] Gaa(float hold)
        {
            DragonClipKey stance = K(-0.6f).Sp(10f, -30f, 0f).Ch(4f, -18f, 0f).Hd(0f, 25f, 0f).RA(-95f, 0f, -55f).RF(-115f, 0f, 0f).Off(0f, -0.1f, 0f).LL(0.3f, 0.3f, 0.3f, 0f).RL(-0.1f, 0.3f, 0.3f, 0f).Two(-0.12f).Wp(0.1f, 1f, -0.5f);
            DragonClipKey load = stance.Copy(-0.12f).Sp(12f, -36f, 0f).Ch(4f, -20f, 0f).Off(0f, -0.1f, 0f);
            DragonClipKey bottom = K(0f).Sp(30f, 0f, 0f).Ch(12f, 0f, 0f).Hd(8f, 0f, 0f).RA(-22f, 0f, 10f).RF(-5f, 0f, 0f).Off(0f, -0.15f, 0.04f).LL(0.35f, 0.12f, 0.45f, 0f).RL(0.15f, 0.12f, 0.4f, 0f).Two(-0.12f).Linear();
            DragonClipKey up = K(0.14f).Sp(0f, 35f, 0f).Ch(-4f, 20f, 0f).Hd(0f, -10f, 0f).RA(-150f, 0f, 55f).RF(-30f, 0f, 0f).Off(0f, -0.05f, 0.02f).LL(0.15f, 0.12f, 0.2f, 0f).RL(0.05f, 0.12f, 0.15f, 0.1f).Two(-0.12f).Linear();
            DragonClipKey after = up.Copy(0.14f + hold); after.Lin = false;
            return new DragonClipKey[] { K(-1f), stance, load, bottom, up, after, K(0.14f + hold + 0.5f) };
        }

        // OSA - overhead slam: two hands lift the weapon straight overhead, then drive it straight down.
        private static DragonClipKey[] Osa(float hold, bool rechamber)
        {
            DragonClipKey lift = K(-0.4f).Sp(-8f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-170f, 0f, -5f).RF(-30f, 0f, 0f).LL(0.1f, 0.08f, 0.12f, 0f).RL(0.05f, 0.08f, 0.1f, 0f).Two(-0.12f);
            DragonClipKey down = K(0f).Sp(32f, 0f, 0f).Ch(14f, 0f, 0f).Hd(10f, 0f, 0f).RA(-45f, 0f, 0f).RF(-5f, 0f, 0f).Off(0f, -0.18f, 0.06f).LL(0.4f, 0.06f, 0.45f, 0f).RL(-0.15f, 0.06f, 0.3f, 0f).Two(-0.12f).Linear();
            DragonClipKey after = down.Copy(hold); after.Lin = false;
            if (rechamber) return new DragonClipKey[] { K(-1f), lift, down, after, lift.Copy(hold + 0.3f), K(hold + 0.55f) };
            return new DragonClipKey[] { K(-1f), lift, down, after, K(hold + 0.6f) };
        }

        // FTAA - forward thrust: main hand pulled to the ribs aiming forward, driven straight at the target,
        // retracted. Stationary (Impact Punch) or carried through a dash (Frenzied Charge).
        private static DragonClipKey[] Ftaa(float hold)
        {
            DragonClipKey ribs = K(-0.45f).Sp(6f, -16f, 0f).Ch(2f, -8f, 0f).Hd(0f, 10f, 0f).RA(25f, 0f, -10f).RF(-115f, 0f, 0f).Off(0f, -0.04f, -0.03f).LL(0.12f, 0.04f, 0.15f, 0f).RL(0f, 0.04f, 0.12f, 0f);
            DragonClipKey drive = K(0f).Sp(10f, 14f, 0f).Ch(4f, 8f, 0f).Hd(0f, -6f, 0f).RA(-90f, 0f, -2f).RF(-2f, 0f, 0f).Off(0f, -0.05f, 0.12f).LL(0.3f, 0.04f, 0.3f, 0f).RL(-0.15f, 0.04f, 0.12f, 0f).Linear();
            DragonClipKey after = drive.Copy(hold); after.Lin = false;
            DragonClipKey back = ribs.Copy(hold + 0.25f);
            return new DragonClipKey[] { K(-1f), ribs, ribs.Copy(-0.08f), drive, after, back, K(hold + 0.55f) };
        }

        // PCA - projectile cast: elbow tucked by the body, then hand + weapon extended at the target; short recovery.
        private static DragonClipKey[] Pca()
        {
            DragonClipKey tuck = K(-0.6f).Sp(4f, -12f, 0f).Ch(2f, -8f, 0f).Hd(0f, 8f, 0f).RA(-45f, 0f, -40f).RF(-120f, 0f, 0f);
            DragonClipKey ext = K(0f).Sp(8f, 10f, 0f).Ch(4f, 6f, 0f).Hd(0f, -4f, 0f).RA(-90f, 0f, -2f).RF(-3f, 0f, 0f).Off(0f, 0f, 0.04f);
            return new DragonClipKey[] { K(-1f), tuck, ext, ext.Copy(0.15f), K(0.45f) };
        }

        // TAA - throw: hand behind the shoulder, torso coiled, thrown forward, follow-through across the body.
        private static DragonClipKey[] Taa()
        {
            DragonClipKey back = K(-0.55f).Sp(-4f, -30f, 0f).Ch(-6f, -18f, 0f).Hd(0f, 25f, 0f).RA(-150f, 0f, -30f).RF(-120f, 0f, 0f).LL(0.15f, 0.06f, 0.15f, 0f);
            DragonClipKey coil = back.Copy(-0.12f).Sp(-6f, -36f, 0f).Ch(-8f, -22f, 0f);
            DragonClipKey thr = K(0f).Sp(14f, 15f, 0f).Ch(8f, 10f, 0f).Hd(0f, -6f, 0f).RA(-100f, 0f, 0f).RF(-8f, 0f, 0f).Off(0f, -0.03f, 0.06f).LL(0.3f, 0.06f, 0.3f, 0f).RL(-0.15f, 0.06f, 0.12f, 0f).Linear();
            DragonClipKey follow = K(0.18f).Sp(18f, 25f, 0f).Ch(10f, 14f, 0f).Hd(0f, -8f, 0f).RA(-60f, 0f, 30f).RF(-10f, 0f, 0f).Off(0f, -0.04f, 0.06f).LL(0.3f, 0.06f, 0.3f, 0f).RL(-0.15f, 0.06f, 0.12f, 0f);
            return new DragonClipKey[] { K(-1f), back, coil, thr, follow, follow.Copy(0.35f), K(0.75f) };
        }

        // TAA batting variant (Punishing Bomb): two hands, bat cocked behind the right shoulder, level sweep
        // sideways through the bomb, finishing over the left shoulder.
        private static DragonClipKey[] Bat()
        {
            DragonClipKey cock = K(-0.55f).Sp(8f, -35f, 0f).Ch(4f, -20f, 0f).Hd(0f, 30f, 0f).RA(-140f, 0f, -50f).RF(-100f, 0f, 0f).Off(0f, -0.06f, 0f).LL(0.2f, 0.1f, 0.25f, 0f).RL(0.05f, 0.1f, 0.25f, 0f).Two(-0.12f);
            DragonClipKey swing = K(0f).Sp(8f, 10f, 0f).Ch(4f, 6f, 0f).Hd(0f, 10f, 0f).RA(-85f, 0f, 0f).RF(-5f, 0f, 0f).Off(0f, -0.06f, 0.03f).LL(0.3f, 0.1f, 0.3f, 0f).RL(-0.05f, 0.1f, 0.2f, 0f).Two(-0.12f).Linear();
            DragonClipKey follow = K(0.15f).Sp(6f, 40f, 0f).Ch(2f, 22f, 0f).Hd(0f, -10f, 0f).RA(-110f, 0f, 60f).RF(-40f, 0f, 0f).Off(0f, -0.05f, 0.03f).LL(0.3f, 0.1f, 0.3f, 0f).RL(-0.05f, 0.1f, 0.2f, 0.1f).Two(-0.12f);
            return new DragonClipKey[] { K(-1f), cock, cock.Copy(-0.1f), swing, follow, follow.Copy(0.35f), K(0.75f) };
        }

        // SPAA - spin: coil, turn with the weapon held out at one height (main arm only), settle in guard.
        private static DragonClipKey SpinPose(float t)
        {
            return K(t).Sp(8f, 0f, 0f).Ch(4f, 0f, 0f).RA(-85f, 0f, -70f).RF(-5f, 0f, 0f).Rot(4f, 0f, 0f).Off(0f, -0.06f, 0f).LL(0.12f, 0.14f, 0.25f, 0f).RL(0.1f, 0.14f, 0.25f, 0f);
        }

        private static DragonClipKey[] Spaa(int turns, float turn)
        {
            List<DragonClipKey> k = new List<DragonClipKey>();
            k.Add(K(-1f));
            k.Add(K(-0.5f).Sp(10f, 30f, 0f).Ch(4f, 16f, 0f).Hd(0f, -16f, 0f).RA(-60f, 0f, 40f).RF(-60f, 0f, 0f).Off(0f, -0.08f, 0f).LL(0.2f, 0.12f, 0.35f, 0f).RL(0.15f, 0.12f, 0.35f, 0f));   // coil
            k.Add(SpinPose(0f));
            float t = 0f, yaw = 0f, q = turn * 0.25f;
            for (int i = 0; i < turns * 4; i++)
            {
                t += q; yaw += 90f;
                DragonClipKey s = SpinPose(t).Rot(4f, yaw, 0f).Linear();
                k.Add(s);
            }
            k.Add(K(t + 0.35f).Rot(0f, yaw, 0f));
            return k.ToArray();
        }

        // GCA - ground cast: bend through the knees, active limb to the ground, contact, rise.
        private static DragonClipKey[] Gca(int v)
        {
            if (v == 1)   // Stomp: foot contact, hands quiet
            {
                DragonClipKey lift = K(-0.45f).Sp(4f, 0f, 0f).Hd(4f, 0f, 0f).LL(0.05f, 0.04f, 0.15f, 0f).RL(0.75f, 0.05f, 0.95f, 0.1f);
                DragonClipKey hit = K(0f).Sp(14f, 0f, 0f).Ch(6f, 0f, 0f).Hd(8f, 0f, 0f).Off(0f, -0.12f, 0f).LL(0.22f, 0.08f, 0.38f, 0f).RL(0.25f, 0.08f, 0.38f, 0f).Linear();
                DragonClipKey stAfter = hit.Copy(0.22f); stAfter.Lin = false;
                return new DragonClipKey[] { K(-1f), lift, lift.Copy(-0.1f).RL(0.85f, 0.05f, 1.0f, 0.1f), hit, stAfter, K(0.6f) };
            }
            if (v == 2)   // Snare: careful low placement with the free hand
            {
                DragonClipKey sn = K(0f).Sp(24f, 0f, 0f).Ch(10f, 0f, 0f).Hd(10f, 0f, 0f).RA(-45f, 0f, -10f).RF(-20f, 0f, 0f).RH(20f, 0f, 0f).Off(0f, -0.22f, 0f).LL(0.45f, 0.06f, 0.6f, 0f).RL(0.25f, 0.06f, 0.75f, 0f);
                return new DragonClipKey[] { K(-1f), sn, sn.Copy(0.15f), K(0.45f) };
            }
            if (v == 3)   // mobile / airborne: shortened downward gesture, no kneel
            {
                DragonClipKey f = K(0f).Hd(8f, 0f, 0f).RA(-50f, 0f, -8f).RF(-10f, 0f, 0f);
                return new DragonClipKey[] { K(-1f), K(-0.5f).RA(-80f, 0f, -8f).RF(-40f, 0f, 0f), f, f.Copy(0.12f), K(0.35f) };
            }
            // Stonefang: knees bend, hand + weapon directed down at the ground, then rise.
            DragonClipKey load = K(-0.6f).Hd(4f, 0f, 0f).RA(-80f, 0f, -8f).RF(-60f, 0f, 0f).LL(0.12f, 0.05f, 0.15f, 0f).RL(0f, 0.05f, 0.12f, 0f);
            DragonClipKey press = K(0f).Sp(20f, 0f, 0f).Ch(8f, 0f, 0f).Hd(10f, 0f, 0f).RA(-40f, 0f, -6f).RF(-5f, 0f, 0f).Off(0f, -0.14f, 0f).LL(0.32f, 0.08f, 0.42f, 0f).RL(0.18f, 0.08f, 0.4f, 0f).Linear();
            DragonClipKey after = press.Copy(0.2f); after.Lin = false;
            return new DragonClipKey[] { K(-1f), load, press, after, K(0.6f) };
        }

        // RCA - remote command: main hand / weapon points at the target, short command, continue.
        private static DragonClipKey[] Rca(int v)
        {
            DragonClipKey pt = K(0f).Sp(0f, 10f, 0f).Ch(0f, 6f, 0f).Hd(0f, -5f, 0f).RA(-95f, 0f, 5f).RF(-3f, 0f, 0f);
            if (v == 1) pt = pt.RH(-30f, 0f, 0f);          // open palm (creation / portal / stop)
            if (v == 2) pt = pt.RF(-20f, 0f, 0f).RH(25f, 0f, 0f);   // pinch / detonate
            if (v == 3) pt = pt.RA(-90f, 0f, 10f).RF(-60f, 0f, 0f); // pull (Gravity Dominion)
            return new DragonClipKey[] { K(-1f), K(-0.4f).RA(-75f, 0f, -10f).RF(-40f, 0f, 0f), pt, pt.Copy(0.15f), K(0.45f) };
        }

        private static void BuildGuideClips(Dictionary<string, DragonClipKey[]> c)
        {
            // 1 BDCA
            string[] bdca = { "cleric_wave", "cleric_ray", "cleric_light", "cleric_intervention", "cleric_crucible", "cleric_wave_ally", "wiz_clockwork", "wiz_nova", "sm_guidance", "rg_tailwind", "rg_vigil" };
            for (int i = 0; i < bdca.Length; i++) c[bdca[i]] = Bdca(false);
            c["merc_roar"] = Bdca(true);
            // 2 SSCA
            string[] ssca = { "cleric_rs", "cleric_rs_asc", "cleric_goddess", "cleric_relic", "cleric_holy_relic", "cleric_judgement", "cleric_tempest", "sorc_glacial", "sorc_glacial_asc" };
            for (int i = 0; i < ssca.Length; i++) c[ssca[i]] = Ssca(false);
            c["wiz_meteor"] = Ssca(true);
            c["wiz_cataclysm"] = Ssca(true);
            // 3 SAA / CSAA / CSAFA
            c["warrior_heavy"] = SlashKeys(1f, 0.15f, false);
            c["merc_heavy_asc"] = SlashKeys(1.3f, 0.2f, false);
            c["sm_slash_a"] = SlashKeys(0f, 0.04f, false);
            c["sm_slash_b"] = SlashKeys(0f, 0.04f, false);
            c["sm_moon_finisher"] = SlashKeys(1.3f, 0.25f, false);
            c["sm_crescent"] = SlashKeys(1f, 0.15f, false);
            c["sm_blade_storm"] = SlashKeys(0f, 0.1f, false);
            DragonClipKey[] slash = SlashKeys(0.6f, 0.1f, false);
            List<DragonClipKey> hm = new List<DragonClipKey>();
            hm.Add(K(-1f)); hm.Add(CsaaPull(-0.6f)); hm.Add(CsaaPull(-0.3f));
            for (int i = 2; i < slash.Length; i++) hm.Add(slash[i]);
            c["sm_halfmoon"] = hm.ToArray();
            c["sm_halfmoon_2"] = SlashKeys(0.6f, 0.1f, false);
            c["sm_halfmoon_stance"] = new DragonClipKey[] { K(-1f), CsaaPull(0f), K(0.15f) };
            DragonClipKey[] fin = SlashKeys(1.2f, 0.15f, true);
            fin[1] = CsaaPull(-0.6f);
            c["sm_halfmoon_finisher"] = fin;
            // 4 GAA / 14 OSA
            c["warrior_impact_wave"] = Gaa(0.25f);
            c["merc_seismic"] = Gaa(0.25f);
            c["wiz_greatblade"] = Osa(0.25f, false);
            c["wiz_greatblade_slam"] = Osa(0.15f, true);
            // 6 FTAA
            c["warrior_punch"] = Ftaa(0.15f);
            c["sm_thrust"] = Ftaa(0.4f);
            // 7 PCA
            c["cleric_zap"] = Pca();
            c["sorc_flame"] = Pca();
            c["hw_gravity_blast"] = Pca();
            // 8 TAA (+ catch only when the hammer really returns: the hold pose is neutral)
            c["cleric_hammer"] = Taa();
            DragonClipKey catchK = K(0.08f).Sp(-6f, 6f, 0f).RA(-95f, 0f, -6f).RF(-60f, 0f, 0f).Off(0f, -0.03f, -0.05f);
            c["cleric_hammer_call"] = new DragonClipKey[] { K(-1f), K(0f), catchK, catchK.Copy(0.2f), K(0.5f) };
            c["merc_bomb"] = Bat();
            // 9 GCA
            c["sorc_stonefang"] = Gca(0);
            c["merc_stomp"] = Gca(1);
            c["rg_trap"] = Gca(2);
            c["sorc_stonefang_asc"] = Gca(3);
            // 10 SPAA (Whirlwind / Furious Winds use PlaySpinClip, same pose)
            c["merc_circle"] = Spaa(1, 0.4f);
            c["merc_circle_2"] = Spaa(1, 0.35f);
            c["sm_eclipse"] = Spaa(1, 0.4f);
            // 11 AFA staff (Railcannon): two hands on the staff, levelled along the crosshair, one recoil.
            DragonClipKey br = K(-0.6f).Sp(8f, -25f, 0f).Ch(4f, -14f, 0f).Hd(0f, 22f, 0f).RA(-85f, 0f, -5f).RF(-20f, 0f, 0f).Off(0f, -0.1f, 0f).LL(0.3f, 0.12f, 0.4f, 0f).RL(0f, 0.12f, 0.35f, 0f).Two(0.35f).Wp(0f, 0f, 1f);
            DragonClipKey fire = br.Copy(0f).Sp(2f, -22f, 0f).Ch(-4f, -12f, 0f).RF(-35f, 0f, 0f).Off(0f, -0.1f, -0.1f);
            c["wiz_railcannon"] = new DragonClipKey[] { K(-1f), br, br.Copy(-0.1f), fire, br.Copy(0.25f), K(0.6f) };
            c["wiz_railcannon_hold"] = new DragonClipKey[] { K(-1f), br, br.Copy(0f), K(0.25f) };
            // 13 RCA
            c["hw_point"] = Rca(0); c["hw_command"] = Rca(0); c["hw_rift_echo"] = Rca(0); c["hw_rupture"] = Rca(2);
            c["hw_open"] = Rca(1); c["hw_stop"] = Rca(1); c["hw_pinch"] = Rca(2); c["hw_afterimage"] = Rca(1);
            c["hw_rift_walker"] = Rca(1); c["wiz_gravity"] = Rca(3);

            // RULES 2 + 3 for everything else: no off-hand motion anywhere (Ranger bow clips and the buckler
            // Holy Shockwave keep theirs - the bow IS their weapon, the buckler IS the skill).
            HashSet<DragonClipKey> done = new HashSet<DragonClipKey>();
            foreach (KeyValuePair<string, DragonClipKey[]> kv in c)
            {
                if (kv.Key.StartsWith("rg_") || kv.Key == "cleric_parry_burst") continue;
                for (int i = 0; i < kv.Value.Length; i++)
                {
                    DragonClipKey k = kv.Value[i];
                    if (!done.Add(k)) continue;
                    if (k.TW > 0f) continue;   // two-handed: the IK owns the off arm
                    k.B[7] = Vector3.zero; k.B[8] = Vector3.zero; k.B[9] = Vector3.zero;
                }
            }
        }

        // ------------------------------------------------------------------ v0.25.19 user polish
        // SKY SUMMON: weapon raised high to the sky, the fist closes on what is up there, then it is yanked
        // down hard toward the ground in front (the summon lands at the target). Charged = hold up high.
        private static DragonClipKey[] SkySummon(bool charged, bool twoArms)
        {
            DragonClipKey lift = K(-0.6f).Ch(-2f, 0f, 0f).Hd(-6f, 0f, 0f).RA(-110f, 0f, -12f).RF(-50f, 0f, 0f).Wp(0f, 0.8f, 0.5f).LL(0.06f, 0.04f, 0.08f, 0f).RL(0f, 0.04f, 0.06f, 0f);
            DragonClipKey up = K(charged ? 0f : -0.16f).Sp(-6f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-22f, 0f, 0f).RA(-172f, 0f, -6f).RF(-6f, 0f, 0f).RH(-10f, 0f, 0f).Off(0f, 0.03f, 0f).Wp(0f, 1f, 0.05f);
            DragonClipKey grab = up.Copy(charged ? 0.04f : -0.06f).RH(20f, 0f, 0f).RF(-14f, 0f, 0f);
            float y = charged ? 0.14f : 0f;
            DragonClipKey yank = K(y).Sp(22f, 6f, 0f).Ch(12f, 4f, 0f).Hd(10f, 0f, 0f).RA(-35f, 0f, -8f).RF(-75f, 0f, 0f).RH(10f, 0f, 0f).Off(0f, -0.12f, 0.04f).LL(0.25f, 0.06f, 0.35f, 0f).RL(0.15f, 0.06f, 0.3f, 0f).Wp(0f, -0.6f, 1f).Linear();
            if (twoArms)
            {
                lift = lift.LA(-110f, 0f, 12f).LF(-50f, 0f, 0f);
                up = up.LA(-172f, 0f, 6f).LF(-6f, 0f, 0f).LH(-10f, 0f, 0f).Oi(0f, 1f, 0.05f);
                grab = grab.LA(-172f, 0f, 6f).LF(-14f, 0f, 0f).LH(20f, 0f, 0f);
                yank = yank.LA(-35f, 0f, 8f).LF(-75f, 0f, 0f).LH(10f, 0f, 0f);
            }
            DragonClipKey press = yank.Copy(y + 0.22f).Sp(24f, 6f, 0f).Off(0f, -0.13f, 0.04f);
            press.Lin = false;
            if (charged) return new DragonClipKey[] { K(-1f), lift, up, grab, yank, press, K(y + 0.75f) };
            return new DragonClipKey[] { K(-1f), lift, up, grab, yank, press, K(0.7f) };
        }

        // HEALING / BLESSING: main-hand weapon raised straight up like a staff; the wave leaves the caster.
        private static DragonClipKey[] RaiseHeal()
        {
            DragonClipKey top = K(0f).Sp(-3f, 0f, 0f).Ch(-6f, 0f, 0f).Hd(-12f, 0f, 0f).RA(-172f, 0f, -6f).RF(-6f, 0f, 0f).Off(0f, 0.02f, 0f).Wp(0f, 1f, 0.05f);
            return new DragonClipKey[] { K(-1f), K(-0.5f).Ch(-2f, 0f, 0f).Hd(-4f, 0f, 0f).RA(-120f, 0f, -12f).RF(-45f, 0f, 0f).Wp(0f, 0.8f, 0.6f), top, top.Copy(0.3f), K(0.7f) };
        }

        // THROW = fishing cast: one hand pulls the "rod" back behind the head, then flicks it out forward.
        private static DragonClipKey[] FishingCast()
        {
            // v0.25.20 (user): pull back = hands BEHIND the head (the weapon tip trailing back like a rod pulled
            // over the shoulder), cast = hands snap out IN FRONT of the head and the weapon whips forward.
            DragonClipKey load = K(-0.6f).Sp(-6f, -6f, 0f).Ch(-8f, -4f, 0f).Hd(-4f, 4f, 0f).RA(-160f, 0f, -12f).RF(-120f, 0f, 0f).LA(-160f, 0f, 12f).LF(-120f, 0f, 0f).Rot(-4f, 0f, 0f).LL(0.1f, 0.03f, 0.1f, 0f).Wp(0f, 0.4f, -1f);
            DragonClipKey cock = K(-0.12f).Sp(-10f, -8f, 0f).Ch(-12f, -6f, 0f).Hd(-6f, 6f, 0f).RA(-168f, 0f, -10f).RF(-130f, 0f, 0f).LA(-168f, 0f, 10f).LF(-130f, 0f, 0f).Rot(-7f, 0f, 0f).LL(0.12f, 0.03f, 0.12f, 0f).Wp(0f, 0.1f, -1f);
            DragonClipKey cast = K(0f).Sp(12f, 6f, 0f).Ch(10f, 4f, 0f).Hd(2f, -4f, 0f).RA(-128f, 0f, -6f).RF(-14f, 0f, 0f).LA(-122f, 0f, 8f).LF(-20f, 0f, 0f).Rot(5f, 0f, 0f).LL(0.22f, 0.03f, 0.22f, 0f).RL(-0.08f, 0.03f, 0.1f, 0f).Wp(0f, 0.35f, 1f).Linear();
            DragonClipKey follow = K(0.15f).Sp(16f, 8f, 0f).Ch(12f, 6f, 0f).Hd(4f, -4f, 0f).RA(-105f, 0f, -4f).RF(-8f, 0f, 0f).LA(-95f, 0f, 8f).LF(-15f, 0f, 0f).Rot(6f, 0f, 0f).LL(0.24f, 0.03f, 0.24f, 0f).RL(-0.08f, 0.03f, 0.1f, 0f).Wp(0f, 0.05f, 1f);
            DragonClipKey hold = follow.Copy(0.35f);
            hold.Lin = false;
            return new DragonClipKey[] { K(-1f), load, cock, cast, follow, hold, K(0.75f) };
        }


        private static void BuildPolishClips(Dictionary<string, DragonClipKey[]> c)
        {
            // 1. Sky summons (Cleric relics / strikes / storms, Glacial; Meteor / Cataclysm charged).
            c["cleric_rs"] = SkySummon(false, false);
            c["cleric_rs_asc"] = SkySummon(false, false);
            c["cleric_goddess"] = SkySummon(false, false);
            c["cleric_relic"] = SkySummon(false, false);
            c["cleric_holy_relic"] = SkySummon(false, false);
            c["cleric_judgement"] = SkySummon(false, false);
            c["cleric_tempest"] = SkySummon(false, false);
            c["sorc_glacial"] = SkySummon(false, false);
            c["sorc_glacial_asc"] = SkySummon(false, false);
            c["wiz_meteor"] = SkySummon(true, false);
            c["wiz_cataclysm"] = SkySummon(true, true);

            // 2. Shield Charge on Valheim's real block pose (shield in front) + run: lean in, weapon held back,
            // then a hard shield bash (forearm punched out, left shoulder through, lunge).
            // v0.25.21 guide (FTAA shield variant): the shield leads, the main weapon stays tucked behind it.
            DragonClipKey scBrace = K(-0.5f).Sp(14f, 0f, 0f).Ch(6f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-35f, 0f, 22f).RF(-100f, 0f, 0f).Off(0f, -0.08f, 0f).LL(0.25f, 0.04f, 0.35f, 0f).RL(0f, 0.04f, 0.3f, 0f).Wp(0.15f, 0.8f, 0.25f);
            DragonClipKey scDrive = K(0f).Sp(18f, -6f, 0f).Ch(8f, -6f, 0f).Hd(-18f, 0f, 0f).RA(-35f, 0f, 22f).RF(-100f, 0f, 0f).Rot(12f, 0f, 0f).Wp(0.15f, 0.8f, 0.25f);
            // v0.25.20 (user): the finisher is a MACE blow to the head with the main hand, shield kept up:
            // mace whipped up over the shoulder, then smashed down-forward at head height with a lunge.
            DragonClipKey scRaise = K(0.07f).Sp(4f, -18f, 0f).Ch(-6f, -12f, 0f).Hd(-10f, 10f, 0f).RA(-165f, 0f, -20f).RF(-70f, 0f, 0f).Rot(4f, 0f, 0f).Off(0f, -0.04f, 0f).LL(0.15f, 0.04f, 0.2f, 0f).Wp(0.1f, 0.6f, -1f);
            DragonClipKey scSmash = K(0.17f).Sp(26f, 22f, 0f).Ch(14f, 14f, 0f).Hd(-14f, -10f, 0f).RA(-80f, 0f, 8f).RF(-10f, 0f, 0f).Rot(14f, 6f, 0f).Off(0f, -0.14f, 0.28f).LL(0.45f, 0.04f, 0.45f, 0f).RL(-0.3f, 0.04f, 0.2f, 0f).Wp(-0.1f, -0.35f, 1f).Linear();
            DragonClipKey scHold = scSmash.Copy(0.32f);
            scHold.Lin = false;
            DragonClipKey scRec = K(0.62f).Sp(6f, 0f, 0f).RA(-20f, 0f, -10f).RF(-40f, 0f, 0f).Off(0f, -0.04f, 0f).LL(0.1f, 0.03f, 0.12f, 0f).RL(0f, 0.03f, 0.1f, 0f);
            c["cleric_charge"] = new DragonClipKey[] { K(-1f), scBrace, scDrive, scRaise, scSmash, scHold, scRec, K(0.95f) };

            // 3. Angel Comet (hold until near the ground): crouch, jump with the arm thrown up, wings on the rise,
            // tip over into a fully INVERTED head-first dive (legs straight up). Near the ground (ClipImpact) the
            // body flips forward upright into the superhero landing, already set when the damage lands.
            DragonClipKey inv = K(0f).Sp(4f, 0f, 0f).Ch(2f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-172f, 0f, -6f).RF(-4f, 0f, 0f).Rot(165f, 0f, 0f).LL(-0.05f, -0.04f, -0.25f, -0.45f).RL(-0.05f, -0.04f, -0.25f, -0.45f);
            DragonClipKey hero = K(0.16f).Sp(34f, 0f, 0f).Ch(16f, 0f, 0f).Hd(-30f, 0f, 0f).RA(-45f, 0f, -10f).RF(-12f, 0f, 0f).RH(10f, 0f, 0f).Rot(366f, 0f, 0f).Off(0f, -0.42f, 0.05f).LL(0.75f, 0.15f, 0.95f, 0.15f).RL(-0.8f, 0.15f, 1.2f, -0.2f);
            c["angel_comet"] = new DragonClipKey[] {
                K(-1f),
                K(-0.92f).Sp(18f, 0f, 0f).Ch(8f, 0f, 0f).RA(-30f, 0f, -15f).RF(-40f, 0f, 0f).Off(0f, -0.15f, 0f).LL(0.4f, 0f, 0.6f, 0f).RL(0.4f, 0f, 0.6f, 0f),
                K(-0.75f).Sp(-10f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-20f, 0f, 0f).RA(-160f, 0f, -15f).RF(-10f, 0f, 0f).Rot(-6f, 0f, 0f).LL(-0.05f, 0f, -0.2f, -0.4f).RL(-0.05f, 0f, -0.2f, -0.4f),
                K(-0.35f).Sp(-14f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-24f, 0f, 0f).RA(20f, 0f, -70f).RF(-10f, 0f, 0f).Rot(-10f, 0f, 0f).LL(0.3f, 0f, 0.55f, 0f).RL(0.2f, 0f, 0.65f, 0f),
                K(-0.1f).Hd(-20f, 0f, 0f).RA(-170f, 0f, -8f).RF(-6f, 0f, 0f).Rot(70f, 0f, 0f).LL(-0.05f, -0.04f, -0.2f, -0.4f).RL(-0.05f, -0.04f, -0.2f, -0.4f),
                inv, hero, hero.Copy(0.4f).Off(0f, -0.44f, 0.05f),
                K(0.7f).Sp(16f, 0f, 0f).Ch(6f, 0f, 0f).Hd(-8f, 0f, 0f).RA(-15f, 0f, -15f).RF(-40f, 0f, 0f).Rot(360f, 0f, 0f).Off(0f, -0.12f, 0f).LL(0.3f, 0f, 0.35f, 0f).RL(0f, 0f, 0.3f, 0f),
                K(1.0f).Rot(360f, 0f, 0f)
            };

            // 5. Healing / blessing spells: main-hand weapon raised like a staff.
            c["cleric_wave"] = RaiseHeal();
            c["cleric_ray"] = RaiseHeal();
            c["cleric_light"] = RaiseHeal();
            c["cleric_intervention"] = RaiseHeal();
            c["cleric_crucible"] = RaiseHeal();
            c["cleric_wave_ally"] = RaiseHeal();

            // 6. Throws: fishing-rod cast (Judgement Hammer).
            c["cleric_hammer"] = FishingCast();

        }

        // ------------------------------------------------------------------ v0.25.18 Blueprint part D
        private static DragonClipKey Archer(DragonClipKey k, float depth)
        {
            // side-on archer stance: front (left) knee soft, feet apart
            return k.LL(0.15f * depth, 0.12f, 0.22f * depth, 0f).RL(0f, 0.12f, 0.2f * depth, 0f);
        }

        private static void TuckLegs(Dictionary<string, DragonClipKey[]> c, string name)
        {
            DragonClipKey[] keys;
            if (!c.TryGetValue(name, out keys)) return;
            for (int i = 1; i < keys.Length - 1; i++) keys[i].LL(0.6f, 0f, 0.9f, 0f).RL(0.55f, 0f, 0.95f, 0f);
        }

        private static void BuildBlueprintD(Dictionary<string, DragonClipKey[]> c)
        {
            // ANIM_14 RIFT CONDUCTOR: short mobile commands, upper body only (no crouch, no root move), so the
            // stride / firing underneath continues. Variants: point (dispatch), open (fan / array / portal),
            // stop (open palm), pinch (detonate), pull (Gravity Dominion keeps its deliberate reach-and-pull).
            DragonClipKey pt = K(0f).Sp(0f, 10f, 0f).Ch(0f, 6f, 0f).Hd(0f, -5f, 0f).RA(-95f, 0f, 5f).RF(-3f, 0f, 0f).RH(-10f, 0f, 0f);
            c["hw_point"] = new DragonClipKey[] { K(-1f), K(-0.4f).RA(-75f, 0f, -10f).RF(-40f, 0f, 0f), pt, pt.Copy(0.15f), K(0.4f) };
            DragonClipKey op = K(0f).Ch(-4f, 0f, 0f).RA(-90f, 0f, -45f).RF(-10f, 0f, 0f).RH(-30f, 0f, 0f);
            c["hw_open"] = new DragonClipKey[] { K(-1f), K(-0.5f).RA(-70f, 0f, 20f).RF(-80f, 0f, 0f), op, op.Copy(0.18f), K(0.45f) };
            DragonClipKey stp = K(0f).Hd(0f, -4f, 0f).RA(-95f, 0f, -5f).RF(-10f, 0f, 0f).RH(-65f, 0f, 0f);
            c["hw_stop"] = new DragonClipKey[] { K(-1f), stp, stp.Copy(0.2f), K(0.45f) };
            DragonClipKey pin = K(0f).Ch(2f, 4f, 0f).RA(-90f, 0f, 0f).RF(-25f, 0f, 0f).RH(25f, 0f, 0f);
            c["hw_pinch"] = new DragonClipKey[] { K(-1f), K(-0.5f).RA(-90f, 0f, -5f).RF(-10f, 0f, 0f).RH(-30f, 0f, 0f), pin, pin.Copy(0.12f), K(0.35f) };
            // Phalanx recast arms the volley with a closing point; the volley release is a dispatch point.
            c["hw_command"] = c["hw_point"];
            // Arcane Rupture: wrist snapped toward the selected point, then carry on.
            DragonClipKey ru = K(0f).Sp(0f, 8f, 0f).RA(-92f, 0f, 0f).RF(-10f, 0f, 0f).RH(30f, 0f, 0f);
            c["hw_rupture"] = new DragonClipKey[] { K(-1f), K(-0.5f).RA(-85f, 0f, -5f).RF(-20f, 0f, 0f).RH(-30f, 0f, 0f), ru, ru.Copy(0.12f), K(0.4f) };
            // Rift Echo: brief finger indication of the target (the rifts open behind it).
            c["hw_rift_echo"] = c["hw_point"];
            // Afterimage Arsenal: free hand flicked outward to leave / summon a copy.
            DragonClipKey fl = K(0f).Sp(0f, -6f, 0f).RA(-70f, 0f, -70f).RF(-10f, 0f, 0f).RH(-30f, 0f, 0f);
            c["hw_afterimage"] = new DragonClipKey[] { K(-1f), K(-0.5f).RA(-60f, 0f, 25f).RF(-90f, 0f, 0f), fl, fl.Copy(0.12f), K(0.4f) };
            // Arcane Phalanx summon / Rift Walker portal: hands open toward the array / placement point.
            DragonClipKey rw = K(0f).Ch(-4f, 0f, 0f).Hd(-4f, 0f, 0f).RA(-95f, 0f, -45f).RF(-12f, 0f, 0f).RH(-25f, 0f, 0f).LA(-85f, 0f, 45f).LF(-12f, 0f, 0f).LH(-25f, 0f, 0f);
            c["hw_rift_walker"] = new DragonClipKey[] { K(-1f), K(-0.5f).RA(-80f, 0f, 15f).RF(-60f, 0f, 0f).LA(-80f, 0f, -15f).LF(-60f, 0f, 0f), rw, rw.Copy(0.2f), K(0.5f) };
            // ANIM_15 GHOST STEP (Void Step): non-owning transition - a tiny lean only (played as an accent,
            // so a running cast / shot keeps its pose through the teleport).
            c["hw_voidstep"] = new DragonClipKey[] { K(-1f), K(0f).Sp(5f, 0f, 0f).Ch(3f, 0f, 0f), K(0.12f) };

            // ANIM_16 SIEGE CASTER (Astral Railcannon): side-on brace with the staff along the crosshair, shoulders
            // load, one restrained recoil on the shot, settle. The Ascended beam holds the brace (no recoil per tick).
            DragonClipKey br = K(-0.6f).Sp(8f, -25f, 0f).Ch(4f, -14f, 0f).Hd(0f, 22f, 0f).RA(-85f, 0f, -5f).RF(-20f, 0f, 0f).LA(-85f, -10f, -25f).LF(-15f, 0f, 0f).Off(0f, -0.1f, 0f).LL(0.3f, 0.12f, 0.4f, 0f).RL(0f, 0.12f, 0.35f, 0f);
            DragonClipKey ld = br.Copy(-0.1f).Sp(10f, -28f, 0f).RF(-25f, 0f, 0f);
            DragonClipKey fire = br.Copy(0f).Sp(2f, -22f, 0f).Ch(-4f, -12f, 0f).RA(-92f, 0f, -5f).RF(-35f, 0f, 0f).LA(-90f, -10f, -25f).LF(-25f, 0f, 0f).Off(0f, -0.1f, -0.12f);
            c["wiz_railcannon"] = new DragonClipKey[] { K(-1f), br, ld, fire, br.Copy(0.25f), K(0.6f) };
            c["wiz_railcannon_hold"] = new DragonClipKey[] { K(-1f), br, br.Copy(0f), K(0.25f) };

            // ANIM_17 DEADEYE DRAW: aim -> draw/hold -> loose -> reset on a side-on archer stance; one small
            // recoil source only (no root kick-back).
            c["rg_power"] = new DragonClipKey[] { K(-1f), Archer(Draw(-0.4f), 1f), Archer(Draw(-0.05f), 1f), Archer(Loose(0f).Ch(-6f, -10f, 0f), 1f), Archer(Loose(0.15f), 1f), K(0.45f) };
            c["rg_heavy"] = new DragonClipKey[] { K(-1f), Archer(Draw(-0.4f).Off(0f, -0.06f, 0f), 1.5f), Archer(Draw(-0.05f).Off(0f, -0.06f, 0f), 1.5f), Archer(Loose(0f).Ch(-10f, -12f, 0f).Off(0f, -0.06f, -0.08f), 1.5f), Archer(Loose(0.22f).Off(0f, -0.05f, -0.04f), 1.3f), K(0.55f) };
            // Pinning Shot: low brace on real legs (front knee forward, back knee down).
            DragonClipKey kn = K(0f).LL(0.55f, 0.05f, 0.85f, 0f).RL(-0.05f, 0.05f, 1.25f, 0f).Off(0f, -0.3f, 0f);
            c["rg_kneel"] = new DragonClipKey[] {
                K(-1f), Draw(-0.5f).LL(0.55f, 0.05f, 0.85f, 0f).RL(-0.05f, 0.05f, 1.25f, 0f).Off(0f, -0.3f, 0f),
                Draw(-0.05f).LL(0.55f, 0.05f, 0.85f, 0f).RL(-0.05f, 0.05f, 1.25f, 0f).Off(0f, -0.3f, 0f),
                Loose(0f).LL(0.55f, 0.05f, 0.85f, 0f).RL(-0.05f, 0.05f, 1.25f, 0f).Off(0f, -0.3f, -0.03f),
                Loose(0.25f).LL(0.55f, 0.05f, 0.85f, 0f).RL(-0.05f, 0.05f, 1.25f, 0f).Off(0f, -0.3f, 0f), K(0.6f)
            };
            // Ballista: wide stable stance, controlled held draw, one heavy shoulder recoil on release.
            c["rg_ballista"] = new DragonClipKey[] {
                K(-1f), Draw(-0.5f).Off(0f, -0.12f, 0f).LL(0.3f, 0.15f, 0.4f, 0f).RL(0f, 0.15f, 0.4f, 0f),
                Draw(0f).RF(-150f, 0f, 0f).Off(0f, -0.14f, -0.03f).LL(0.32f, 0.15f, 0.42f, 0f).RL(0f, 0.15f, 0.42f, 0f),
                Loose(0.06f).Ch(-12f, -12f, 0f).Off(0f, -0.1f, -0.2f).LL(0.3f, 0.15f, 0.4f, 0f).RL(0f, 0.15f, 0.4f, 0f),
                Loose(0.3f).Off(0f, -0.08f, -0.08f).LL(0.25f, 0.15f, 0.3f, 0f).RL(0f, 0.15f, 0.3f, 0f), K(0.7f)
            };
            // Ricochet: relaxed side-on hip trick shot.
            c["rg_trick"] = new DragonClipKey[] { K(-1f), Archer(Draw(-0.5f).Sp(4f, -34f, 0f).Ch(0f, -18f, 0f), 0.7f), Archer(Loose(0f).Sp(0f, -10f, 6f).LA(-70f, 0f, 20f), 0.7f), Archer(Loose(0.15f).Sp(0f, -8f, 4f), 0.7f), K(0.45f) };
            // Splitting Arrow: stable lower stance, the torso (not the root) sweeps across the cone, volleys chain.
            c["rg_split_a"] = new DragonClipKey[] { K(-1f), Archer(Draw(-0.5f).Sp(6f, -40f, 0f).Off(0f, -0.08f, 0f), 1.4f), Archer(Loose(0f).Sp(6f, 2f, 0f).Off(0f, -0.08f, 0f), 1.4f), Archer(Draw(0.3f).Sp(6f, -10f, 0f).Off(0f, -0.08f, 0f), 1.4f), K(0.6f) };
            c["rg_split_b"] = new DragonClipKey[] { K(-1f), Archer(Draw(-0.5f).Sp(6f, -4f, 0f).Off(0f, -0.08f, 0f), 1.4f), Archer(Loose(0f).Sp(6f, -40f, 0f).Off(0f, -0.08f, 0f), 1.4f), Archer(Draw(0.3f).Sp(6f, -22f, 0f).Off(0f, -0.08f, 0f), 1.4f), K(0.6f) };

            // ANIM_18 REBOUND SHOT (Tumble / Gale / Skyfall flip): knees tucked through the backflip.
            TuckLegs(c, "rg_tumble");
            TuckLegs(c, "rg_backflip");
            // ANIM_19 SKY ARCHER: planted stance under the sky release; hover = loosely tucked legs.
            AddStance(c, "rg_sky", 0.6f);
            AddStance(c, "rg_starfall", 0.8f);
            DragonClipKey[] hv;
            if (c.TryGetValue("rg_hover", out hv)) hv[1].LL(0.3f, 0f, 0.5f, 0f).RL(0.1f, 0f, 0.65f, 0f);
        }

        // ------------------------------------------------------------------ v0.25.17 Blueprint part C
        // Wide fighting stance under a cut / sweep (both knees soft, feet apart).
        private static DragonClipKey Stance(DragonClipKey k, float depth)
        {
            return k.LL(0.18f * depth, 0.12f, 0.3f * depth, 0f).RL(0.05f * depth, 0.12f, 0.28f * depth, 0f);
        }

        // ANIM_05 ARC CUTTER, horizontal plane: coil (blade beside the rear hip, hips loaded, head on aim) ->
        // cut (hips lead, shoulders follow, blade crosses at shoulder height) -> follow past the target line ->
        // reset. back = backhand from the left hip to the right. heavy = more amplitude + deeper stance, with a
        // short held coil. hold = seconds the follow pose is kept.
        private static DragonClipKey[] ArcCutter(bool back, float heavy, int prof, float hold)
        {
            // v0.25.20: the MAIN hand carries the whole arc (coil out to the side with the blade pointing back,
            // cut across the front, follow far to the other side); the off hand stays on its vanilla guard.
            float sg = back ? -1f : 1f;
            float a = 1f + 0.35f * heavy;
            DragonClipKey coil = Stance(K(-0.5f).Sp(10f * a, -40f * sg * a, 0f).Ch(4f, -22f * sg * a, 0f).Hd(0f, 28f * sg, 0f).Off(0f, -0.1f * a, 0f), a);
            if (!back) coil = coil.RA(-80f, -40f, -80f).RF(-40f, 0f, 0f).Wp(0.6f, 0.05f, -1f);
            else coil = coil.RA(-80f, 0f, 50f).RF(-95f, 0f, 0f).Wp(-0.6f, 0.05f, -1f);
            coil = OffHand(coil, prof, 0);
            DragonClipKey cut = Stance(K(0f).Hp(0f, 10f * sg, 0f).Sp(8f, 30f * sg * a, 0f).Ch(4f, 18f * sg * a, 0f).Hd(0f, -12f * sg, 0f).RA(-92f, 0f, 0f).RF(-6f, 0f, 0f).Rot(4f, 12f * sg, 0f).Off(0f, -0.1f * a, 0.05f).Wp(0f, 0f, 1f), a);
            cut = cut.LL(0.32f * a, 0.12f, 0.35f * a, 0f);
            cut = OffHand(cut, prof, 2);
            if (prof == 1) cut = cut.LA(-30f, 0f, 45f).LF(-20f, 0f, 0f);
            DragonClipKey follow = cut.Copy(0.12f).Sp(8f, 45f * sg * a, 0f).Ch(4f, 26f * sg * a, 0f).Rot(4f, 18f * sg, 0f);
            if (!back) follow = follow.RA(-85f, 0f, 50f).RF(-30f, 0f, 0f).Wp(-1f, 0f, -0.2f);
            else follow = follow.RA(-80f, -30f, -75f).RF(-15f, 0f, 0f).Wp(1f, 0f, -0.2f);
            follow.Lin = false;
            DragonClipKey reset = OffHand(Stance(K(0.12f + hold + 0.3f).Sp(6f, 0f, 0f).RA(-30f, 0f, -15f).RF(-40f, 0f, 0f).Off(0f, -0.04f, 0f), 0.5f), prof, 0);
            if (heavy > 0f) return new DragonClipKey[] { K(-1f), coil, coil.Copy(-0.08f), cut, follow, follow.Copy(0.12f + hold), reset, K(0.12f + hold + 0.6f) };
            return new DragonClipKey[] { K(-1f), coil, cut, follow, follow.Copy(0.12f + hold), reset, K(0.12f + hold + 0.5f) };
        }


        // ANIM_06 EARTHBREAKER: load (knees bent, weapon close) -> prepare (overhead for the downstroke, low
        // behind the hip for the rising stroke) -> drive through the target plane -> release -> recover.
        private static DragonClipKey[] Earthbreaker(bool rising, float hold, bool rechamber)
        {
            DragonClipKey load = K(-0.8f).Sp(10f, 0f, 0f).Ch(4f, 0f, 0f).RA(-60f, 0f, 10f).RF(-90f, 0f, 0f).LA(-60f, 0f, -10f).LF(-90f, 0f, 0f).Off(0f, -0.08f, 0f).LL(0.25f, 0.06f, 0.3f, 0f).RL(0.2f, 0.06f, 0.3f, 0f);
            if (!rising)
            {
                DragonClipKey raise = K(-0.3f).Sp(-10f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-8f, 0f, 0f).RA(-170f, 0f, -5f).RF(-35f, 0f, 0f).LA(-100f, 0f, 15f).LF(-60f, 0f, 0f).Off(0f, 0.02f, 0f).LL(0.15f, 0.1f, 0.1f, 0f).RL(-0.05f, 0.1f, 0.1f, 0f).Wp(0f, 1f, -0.5f);
                DragonClipKey drive = K(0f).Sp(38f, 0f, 0f).Ch(16f, 0f, 0f).Hd(14f, 0f, 0f).RA(-35f, 0f, -4f).RF(-4f, 0f, 0f).LA(-30f, 0f, 15f).LF(-45f, 0f, 0f).Off(0f, -0.22f, 0.08f).LL(0.55f, 0.08f, 0.55f, 0f).RL(-0.25f, 0.08f, 0.35f, 0f).Wp(0f, -0.7f, 1f);
                if (rechamber) return new DragonClipKey[] { K(-1f), raise, drive, drive.Copy(hold), raise.Copy(hold + 0.35f), K(hold + 0.6f) };
                DragonClipKey rec = K(hold + 0.3f).Sp(10f, 0f, 0f).RA(-30f, 0f, -10f).RF(-40f, 0f, 0f).LA(-30f, 0f, 10f).LF(-40f, 0f, 0f).Off(0f, -0.05f, 0f).LL(0.1f, 0.05f, 0.12f, 0f).RL(0.05f, 0.05f, 0.1f, 0f);
                return new DragonClipKey[] { K(-1f), load, raise, drive, drive.Copy(hold), rec, K(hold + 0.6f) };
            }
            DragonClipKey low = K(-0.3f).Sp(22f, -24f, 0f).Ch(12f, -12f, 0f).Hd(-6f, 18f, 0f).RA(30f, 0f, -25f).RF(-20f, 0f, 0f).Wp(0.3f, -0.6f, -1f).Rot(6f, 0f, 0f).Off(0f, -0.2f, 0f).LL(0.45f, 0.1f, 0.6f, 0f).RL(0.1f, 0.1f, 0.55f, 0f);
            DragonClipKey up = K(0f).Sp(-12f, 16f, 0f).Ch(-10f, 8f, 0f).Hd(-10f, -4f, 0f).RA(-155f, 0f, -12f).RF(-10f, 0f, 0f).Wp(0f, 1f, 0.3f).Rot(-4f, 0f, 0f).Off(0f, 0.03f, 0.06f).LL(0.15f, 0.06f, 0.05f, 0f).RL(-0.15f, 0.06f, 0.05f, -0.2f);
            return new DragonClipKey[] { K(-1f), load.Copy(-0.75f).RA(-20f, 0f, -20f).RF(-60f, 0f, 0f), low, low.Copy(-0.06f), up, up.Copy(hold), K(hold + 0.45f) };
        }

        private static void BuildBlueprintC(Dictionary<string, DragonClipKey[]> c)
        {
            // ARC CUTTER (horizontal). Heavy Slash is now a horizontal heavy cut (blueprint fix).
            c["warrior_heavy"] = ArcCutter(false, 1f, 3, 0.15f);
            c["merc_heavy_asc"] = ArcCutter(false, 1.4f, 3, 0.2f);
            c["sm_slash_a"] = ArcCutter(false, 0f, 3, 0.05f);
            c["sm_slash_b"] = ArcCutter(true, 0f, 3, 0.05f);
            c["sm_moon_finisher"] = ArcCutter(false, 1.6f, 3, 0.25f);   // deeper side chamber, wider horizontal drive
            c["sm_halfmoon"] = ArcCutter(false, 0.6f, 3, 0.1f);
            c["sm_halfmoon_2"] = ArcCutter(true, 0.6f, 3, 0.1f);
            // Crescent Cleave fan: one broad sweep + a small wrist/shoulder accent for the delayed second fan.
            DragonClipKey[] cr = ArcCutter(false, 1f, 3, 0.1f);
            List<DragonClipKey> crl = new List<DragonClipKey>(cr);
            DragonClipKey acc = cr[4].Copy(0.32f).RH(-25f, 0f, 0f).Ch(4f, 34f, 0f);
            crl.Insert(6, acc);
            crl.Sort(delegate(DragonClipKey x, DragonClipKey y) { return x.T.CompareTo(y.T); });
            c["sm_crescent"] = crl.ToArray();
            // Grand Cross (opposed diagonals) keeps its arm paths; a planted stance is added under both cuts.
            AddStance(c, "cleric_cross_1", 0.8f);
            AddStance(c, "cleric_cross_2", 0.8f);

            // EARTHBREAKER: descending (Seismic, Greatblade; Greatblade slams re-chamber overhead) / rising (Impact Wave).
            c["merc_seismic"] = Earthbreaker(false, 0.25f, false);
            c["wiz_greatblade"] = Earthbreaker(false, 0.3f, false);
            c["wiz_greatblade_slam"] = Earthbreaker(false, 0.15f, true);
            c["warrior_impact_wave"] = Earthbreaker(true, 0.3f, false);

            // GROUND SEAL: load onto the support leg, lower through both knees, contact limb per profile, rise.
            // Stomp = right knee raised then a hard foot stamp (one stamp; the aftershocks run on their own).
            DragonClipKey stLift = K(-0.45f).Sp(6f, 0f, 0f).Ch(-4f, 0f, 0f).Hd(4f, 0f, 0f).RA(-30f, 0f, -40f).RF(-30f, 0f, 0f).LA(-30f, 0f, 40f).LF(-30f, 0f, 0f).LL(0.05f, 0.04f, 0.15f, 0f).RL(0.75f, 0.05f, 0.95f, 0.1f);
            DragonClipKey stHit = K(0f).Sp(18f, 0f, 0f).Ch(8f, 0f, 0f).Hd(8f, 0f, 0f).RA(-20f, 0f, -28f).RF(-30f, 0f, 0f).LA(-20f, 0f, 28f).LF(-30f, 0f, 0f).Off(0f, -0.12f, 0f).LL(0.22f, 0.08f, 0.38f, 0f).RL(0.25f, 0.08f, 0.38f, 0f);
            c["merc_stomp"] = new DragonClipKey[] { K(-1f), stLift, stLift.Copy(-0.1f).RL(0.85f, 0.05f, 1.0f, 0.1f), stHit, stHit.Copy(0.22f), K(0.6f) };
            // Stonefang (planted): staff butt pressed into the ground, free palm commands the far eruption.
            DragonClipKey sfLoad = K(-0.6f).Sp(4f, 0f, 0f).Hd(4f, 0f, 0f).RA(-75f, 0f, -10f).RF(-80f, 0f, 0f).LA(-35f, 0f, 20f).LF(-50f, 0f, 0f).LL(0.12f, 0.05f, 0.15f, 0f).RL(-0.04f, 0.05f, 0.1f, 0f);
            DragonClipKey sfPress = K(0f).Sp(20f, 0f, 0f).Ch(8f, 0f, 0f).Hd(10f, 0f, 0f).RA(-30f, 0f, -10f).RF(-70f, 0f, 0f).LA(-60f, 0f, 20f).LF(-10f, 0f, 0f).LH(30f, 0f, 0f).Off(0f, -0.12f, 0f).LL(0.3f, 0.08f, 0.38f, 0f).RL(0.15f, 0.08f, 0.36f, 0f);
            c["sorc_stonefang"] = new DragonClipKey[] { K(-1f), sfLoad, sfPress, sfPress.Copy(0.2f), K(0.55f) };
            // Horizon Walker (mobile, instant): a quick downward wrist command only, no crouch.
            DragonClipKey sfFlick = K(0f).Hd(8f, 0f, 0f).RA(-55f, 0f, -10f).RF(-35f, 0f, 0f).RH(40f, 0f, 0f);
            c["sorc_stonefang_asc"] = new DragonClipKey[] { K(-1f), sfFlick.Copy(-0.5f).RH(-20f, 0f, 0f), sfFlick, K(0.3f) };
            // Snare Trap: careful placement - lower through the knees, free (right) hand sets it, rise.
            DragonClipKey sn = K(0f).Sp(24f, 0f, 0f).Ch(10f, 0f, 0f).Hd(10f, 0f, 0f).RA(-45f, 0f, -10f).RF(-20f, 0f, 0f).RH(20f, 0f, 0f).LA(-20f, 0f, 15f).LF(-30f, 0f, 0f).Off(0f, -0.22f, 0f).LL(0.45f, 0.06f, 0.6f, 0f).RL(0.25f, 0.06f, 0.75f, 0f);
            c["rg_trap"] = new DragonClipKey[] { K(-1f), sn, sn.Copy(0.15f), K(0.45f) };

            // FLASH DRAW (Blade Storm): hand to the opposite hip, one fast outward draw toward the remote aim,
            // reset to the hip guard. Low stance; the player never moves.
            DragonClipKey fdReady = Stance(K(-0.5f).Sp(10f, 18f, 0f).Ch(4f, 10f, 0f).Hd(0f, -10f, 0f).RA(-40f, 10f, 30f).RF(-95f, 0f, 0f).LA(-20f, 0f, 20f).LF(-70f, 0f, 0f).Off(0f, -0.1f, 0f), 0.8f);
            DragonClipKey fdDraw = Stance(K(0f).Sp(10f, -26f, 0f).Ch(6f, -14f, 0f).Hd(0f, 6f, 0f).RA(-88f, 0f, -55f).RF(-4f, 0f, 0f).LA(-20f, 0f, 25f).LF(-40f, 0f, 0f).Rot(4f, -8f, 0f).Off(0f, -0.1f, 0.05f), 0.9f);
            c["sm_blade_storm"] = new DragonClipKey[] { K(-1f), fdReady, fdDraw, fdDraw.Copy(0.1f), fdReady.Copy(0.3f), K(0.5f) };

            // CYCLONE: Eclipse / Circle Swing / Halfmoon finisher / Cyclone Arrow keep their accumulated turn;
            // the turn now stands on a wide pivot stance (Spin360 adds legs).

            // COMET DIVE (Angel Comet): wings open on the rise (legs tucked), streamlined head-first dive (legs
            // together, toes pointed), and the landing brakes with hips rotating under the torso and the
            // shield / weapon guarding first - never head into the ground.
            c["cleric_angel_rise"] = new DragonClipKey[] {
                K(-1f),
                K(-0.5f).Sp(-14f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-24f, 0f, 0f).RA(30f, 0f, -70f).RF(-10f, 0f, 0f).LA(30f, 0f, 70f).LF(-10f, 0f, 0f).Rot(-10f, 0f, 0f).LL(0.35f, 0f, 0.6f, 0f).RL(0.25f, 0f, 0.7f, 0f),
                K(0f).Sp(-16f, 0f, 0f).Ch(-14f, 0f, 0f).Hd(-26f, 0f, 0f).RA(35f, 0f, -80f).RF(-8f, 0f, 0f).LA(35f, 0f, 80f).LF(-8f, 0f, 0f).Rot(-12f, 0f, 0f).LL(0.3f, 0f, 0.55f, 0f).RL(0.2f, 0f, 0.65f, 0f),
                K(0.15f).Sp(10f, 0f, 0f).Ch(8f, 0f, 0f).Hd(-18f, 0f, 0f).RA(-160f, 0f, -10f).LA(-160f, 0f, 10f).Rot(55f, 0f, 0f).LL(-0.1f, -0.05f, 0f, -0.4f).RL(-0.1f, -0.05f, 0f, -0.4f),
                K(0.45f).Sp(14f, 0f, 0f).Ch(10f, 0f, 0f).Hd(-20f, 0f, 0f).RA(-165f, 0f, -8f).LA(-165f, 0f, 8f).Rot(60f, 0f, 0f).LL(-0.12f, -0.05f, 0f, -0.45f).RL(-0.12f, -0.05f, 0f, -0.45f),
                K(3.5f).Sp(14f, 0f, 0f).Ch(10f, 0f, 0f).Hd(-20f, 0f, 0f).RA(-165f, 0f, -8f).LA(-165f, 0f, 8f).Rot(60f, 0f, 0f).LL(-0.12f, -0.05f, 0f, -0.45f).RL(-0.12f, -0.05f, 0f, -0.45f)
            };
            DragonClipKey cl = K(0f).Sp(26f, 0f, 0f).Ch(12f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-60f, 0f, -20f).RF(-30f, 0f, 0f).LA(-55f, 20f, 20f).LF(-70f, 0f, 0f).Off(0f, -0.3f, 0.04f).LL(0.55f, 0.1f, 0.75f, 0f).RL(0.3f, 0.1f, 0.85f, 0f);
            c["cleric_land"] = new DragonClipKey[] { K(-1f), cl, cl.Copy(0.25f).Off(0f, -0.28f, 0.04f), K(0.7f) };
            c["land_crash"] = c["cleric_land"];
            // Swallow Dive: streamlined along the aim, bow tight to the body, legs together.
            DragonClipKey sd = K(0f).Sp(10f, 0f, 0f).Ch(6f, 0f, 0f).Hd(-24f, 0f, 0f).RA(40f, 0f, -20f).RF(-20f, 0f, 0f).LA(-30f, 0f, 15f).LF(-90f, 0f, 0f).Rot(70f, 0f, 0f).LL(-0.1f, -0.05f, 0f, -0.4f).RL(-0.1f, -0.05f, 0f, -0.4f);
            c["rg_dive"] = new DragonClipKey[] { K(-1f), sd, sd.Copy(0.15f), K(0.45f).LL(0.15f, 0f, 0.3f, 0f).RL(0.1f, 0f, 0.3f, 0f), K(0.7f) };
        }

        private static void AddStance(Dictionary<string, DragonClipKey[]> c, string name, float depth)
        {
            DragonClipKey[] keys;
            if (!c.TryGetValue(name, out keys)) return;
            for (int i = 0; i < keys.Length; i++)
            {
                bool rest = keys[i].B[1] == Vector3.zero && keys[i].B[4] == Vector3.zero;
                if (!rest) Stance(keys[i], depth);
            }
        }

        // ------------------------------------------------------------------ v0.25.16 Blueprint part B
        // Off-hand profiles: 0 = shield (Cleric: kept close / guarded), 1 = free hand (Sorcerer: balances),
        // 2 = bow (Ranger: held low in the left hand), 3 = weapon (Warrior: off weapon low).
        private static DragonClipKey OffHand(DragonClipKey k, int prof, int phase)
        {
            return k;   // v0.25.21 (user rule 2): the off hand / shield stays as Valheim holds it unless stated
        }


        // ANIM_02 SKY COMMAND: gather (main hand across the sternum, gaze on the far target) -> call (main arm
        // straight overhead) -> release (pull down and point at the target) -> recover. 0-35% gather,
        // 35-80% lift, 80-100% direct. Charged = hold at CALL until release. grand = both arms reach.
        private static DragonClipKey[] SkyCommand(int prof, bool charged, bool grand)
        {
            DragonClipKey gather = OffHand(K(-0.65f).Sp(4f, 6f, 0f).Ch(2f, 4f, 0f).Hd(6f, -6f, 0f).RA(-50f, 0f, 25f).RF(-100f, 0f, 0f).RH(-10f, 0f, 0f).LL(0.12f, 0.05f, 0.12f, 0f).RL(-0.08f, 0.05f, 0.12f, 0f), prof, 0);
            DragonClipKey call = OffHand(K(charged ? 0f : -0.2f).Sp(-6f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-18f, 0f, 0f).RA(-165f, 0f, -10f).RF(-12f, 0f, 0f).RH(-15f, 0f, 0f).Off(0f, 0.02f, 0f).LL(0.08f, 0.08f, 0.05f, 0f).RL(0f, 0.08f, 0.05f, 0f), prof, 1);
            if (grand) call = call.LA(-150f, 0f, 22f).LF(-12f, 0f, 0f).Hd(-24f, 0f, 0f).Ch(-12f, 0f, 0f);
            float rt = charged ? 0.1f : 0f;
            DragonClipKey rel = OffHand(K(rt).Sp(10f, 10f, 0f).Ch(6f, 6f, 0f).Hd(-4f, -4f, 0f).RA(-95f, 0f, -5f).RF(-4f, 0f, 0f).RH(-10f, 0f, 0f).Off(0f, -0.06f, 0.05f).LL(0.3f, 0f, 0.3f, 0f).RL(-0.2f, 0f, 0.15f, 0f), prof, 2);
            if (grand) rel = rel.LA(-90f, 0f, 10f).LF(-4f, 0f, 0f);
            DragonClipKey guard = OffHand(K(rt + 0.55f).Sp(4f, 0f, 0f).RA(-30f, 0f, -10f).RF(-50f, 0f, 0f).LL(0.08f, 0f, 0.1f, 0f).RL(0f, 0f, 0.08f, 0f), prof, 0);
            if (charged) return new DragonClipKey[] { K(-1f), gather.Copy(-0.6f), call, rel, rel.Copy(rt + 0.25f), guard, K(rt + 0.85f) };
            return new DragonClipKey[] { K(-1f), gather, call, rel, rel.Copy(0.25f), guard, K(0.85f) };
        }

        // ANIM_03 WAVECALLER: gather (hand near the sternum, shoulders in) -> open (chest lifts, main forearm
        // opens outward) -> pulse -> recover 0.2 s, never kneeling. v: 0 blessing palm, 1 barrier forearms,
        // 2 frost snap, 3 salute, 4 clockwork wrist, 5 remote palm (ally / relic source), 6 protective.
        private static DragonClipKey[] Wavecaller(int prof, int v)
        {
            DragonClipKey gather = OffHand(K(-0.6f).Sp(6f, 0f, 0f).Ch(6f, 0f, 0f).Hd(4f, 0f, 0f).RA(-45f, 0f, 30f).RF(-110f, 0f, 0f).RH(-10f, 0f, 0f), prof, 0);
            DragonClipKey open = OffHand(K(0f).Sp(-4f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-6f, 0f, 0f).RA(-80f, 0f, -55f).RF(-15f, 0f, 0f).RH(-25f, 0f, 0f).Off(0f, 0.02f, 0f), prof, 1);
            if (v == 1)
            {
                gather = K(-0.6f).Sp(8f, 0f, 0f).Ch(8f, 0f, 0f).Hd(6f, 0f, 0f).RA(-90f, 0f, 30f).RF(-110f, 0f, 0f).LA(-90f, 0f, -30f).LF(-110f, 0f, 0f).Off(0f, -0.04f, 0f);
                open = K(0f).Sp(-4f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-8f, 0f, 0f).RA(-85f, 0f, -60f).RF(-30f, 0f, 0f).LA(-85f, 0f, 60f).LF(-30f, 0f, 0f).Off(0f, 0.03f, 0f).LL(0.05f, 0.1f, 0.05f, 0f).RL(0.05f, 0.1f, 0.05f, 0f);
            }
            else if (v == 2)
            {
                gather = K(-0.6f).Sp(18f, 0f, 0f).Ch(10f, 0f, 0f).Hd(8f, 0f, 0f).RA(-50f, 0f, 35f).RF(-120f, 0f, 0f).LA(-50f, 0f, -35f).LF(-120f, 0f, 0f).Off(0f, -0.1f, 0f).LL(0.3f, 0f, 0.35f, 0f).RL(0.3f, 0f, 0.35f, 0f);
                open = K(0f).Sp(-6f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-60f, 0f, -78f).RF(-5f, 0f, 0f).RH(-30f, 0f, 0f).LA(-60f, 0f, 78f).LF(-5f, 0f, 0f).LH(-30f, 0f, 0f).Off(0f, 0f, 0f).LL(0.08f, 0.18f, 0.12f, 0f).RL(0.08f, 0.18f, 0.12f, 0f);
            }
            else if (v == 3)
            {
                gather = OffHand(K(-0.6f).Hd(-4f, 0f, 0f).RA(-125f, 0f, 22f).RF(-130f, 0f, 0f).RH(-10f, 0f, 0f), prof, 0);
                open = OffHand(K(0f).Ch(-8f, 0f, 0f).Hd(-8f, 0f, 0f).RA(-95f, 0f, -50f).RF(-10f, 0f, 0f).RH(-20f, 0f, 0f), prof, 1);
            }
            else if (v == 4)
            {
                gather = K(-0.6f).Sp(6f, 0f, 0f).Ch(4f, 0f, 0f).Hd(10f, 0f, 0f).RA(-60f, 0f, 30f).RF(-90f, 0f, 0f).RH(0f, 60f, 0f).LA(-60f, 0f, -20f).LF(-80f, 0f, 0f);
                DragonClipKey turn = gather.Copy(-0.2f).RH(0f, -60f, 0f);
                open = K(0f).Ch(-8f, 0f, 0f).Hd(-6f, 0f, 0f).RA(-80f, 0f, -55f).RF(-15f, 0f, 0f).RH(-25f, 0f, 0f).LA(-40f, 0f, 45f).LF(-15f, 0f, 0f);
                return new DragonClipKey[] { K(-1f), gather, turn, open, open.Copy(0.12f).RA(-85f, 0f, -62f), K(0.4f) };
            }
            else if (v == 5)
            {
                open = OffHand(K(0f).Sp(4f, 0f, 0f).Ch(-2f, 0f, 0f).Hd(-6f, 0f, 0f).RA(-112f, 0f, -15f).RF(-10f, 0f, 0f).RH(-35f, 0f, 0f).Off(0f, 0f, 0.03f), prof, 1);
            }
            else if (v == 6)
            {
                open = K(0f).Sp(-4f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-120f, 0f, -40f).RF(-20f, 0f, 0f).RH(-30f, 0f, 0f).LA(-85f, 15f, 35f).LF(-40f, 0f, 0f).Off(0f, 0.03f, 0f);
            }
            DragonClipKey pulse = open.Copy(0.12f);
            pulse.B[4] = pulse.B[4] + new Vector3(-5f, 0f, v == 5 ? 0f : -8f);
            return new DragonClipKey[] { K(-1f), gather, open, pulse, K(0.4f) };
        }

        // ANIM_13 PULSE DRIVE: chamber (elbow by the ribs, shoulder back) -> drive -> release (hand on the
        // emission plane, lunge legs) -> reset. v: 0 palm, 1 fist, 2 two-hand orb, 3 gentle palm.
        private static DragonClipKey[] PulseDrive(int prof, int v)
        {
            bool two = v == 2;
            DragonClipKey ch = OffHand(K(-0.7f).Sp(6f, -18f, 0f).Ch(4f, -10f, 0f).Hd(0f, 12f, 0f).RA(-35f, 0f, 10f).RF(-120f, 0f, 0f).LL(0.2f, 0f, 0.2f, 0f).RL(-0.1f, 0f, 0.15f, 0f), prof, 0);
            if (two) ch = ch.Sp(8f, 0f, 0f).Ch(6f, 0f, 0f).Hd(0f, 0f, 0f).RA(-45f, 0f, 25f).RF(-115f, 0f, 0f).LA(-45f, 0f, -25f).LF(-115f, 0f, 0f);
            DragonClipKey drive = OffHand(K(-0.25f).Sp(8f, 0f, 0f).Ch(4f, 0f, 0f).RA(-75f, 0f, 0f).RF(-60f, 0f, 0f).LL(0.25f, 0f, 0.25f, 0f).RL(-0.15f, 0f, 0.15f, 0f), prof, 1);
            if (two) drive = drive.LA(-75f, 0f, 0f).LF(-60f, 0f, 0f);
            float palm = v == 1 ? 0f : (v == 3 ? -20f : -35f);
            float lunge = v == 3 ? 0.5f : 1f;
            DragonClipKey rel = OffHand(K(0f).Sp(10f * lunge, two ? 0f : 14f * lunge, 0f).Ch(6f * lunge, two ? 0f : 8f * lunge, 0f).Hd(-4f, two ? 0f : -10f, 0f).RA(-92f, 0f, two ? -8f : -3f).RF(v == 3 ? -15f : -5f, 0f, 0f).RH(palm, 0f, 0f).Off(0f, -0.04f * lunge, 0.06f * lunge).LL(0.3f * lunge, 0f, 0.3f * lunge, 0f).RL(-0.2f * lunge, 0f, 0.15f * lunge, 0f), prof, 2);
            if (two) rel = rel.LA(-92f, 0f, 8f).LF(-5f, 0f, 0f).LH(palm, 0f, 0f);
            else if (prof == 1 && v != 1) rel = rel.LA(-20f, 0f, 30f).LF(-45f, 0f, 0f);
            DragonClipKey reset = OffHand(K(0.35f).Sp(4f, 0f, 0f).RA(-40f, 0f, 10f).RF(-80f, 0f, 0f).LL(0.08f, 0f, 0.08f, 0f), prof, 0);
            return new DragonClipKey[] { K(-1f), ch, drive, rel, rel.Copy(0.12f), reset, K(0.6f) };
        }

        // ANIM_12 POWER PITCH: load (weapon/hand chambered behind the shoulder, off arm sights the target)
        // -> drive (hips first, chest follows) -> release through the launch line -> follow across the body.
        // bat = sideways bat strike (Punishing Bomb) instead of the overarm pitch (Judgement Hammer).
        private static DragonClipKey[] PowerPitch(bool bat)
        {
            DragonClipKey load, drive, rel, follow;
            if (!bat)
            {
                load = K(-0.6f).Sp(4f, -30f, 0f).Ch(-6f, -16f, 0f).Hd(0f, 25f, 0f).RA(-140f, 0f, -35f).RF(-110f, 0f, 0f).LA(-70f, 0f, 20f).LF(-20f, 0f, 0f).LL(0.25f, 0f, 0.1f, 0f).RL(-0.15f, 0f, 0.25f, 0f);
                drive = K(-0.15f).Hp(0f, 10f, 0f).Sp(8f, -12f, 0f).Ch(0f, -6f, 0f).Hd(0f, 12f, 0f).RA(-160f, 0f, -15f).RF(-60f, 0f, 0f).LA(-50f, 0f, 25f).LF(-30f, 0f, 0f).LL(0.32f, 0f, 0.2f, 0f).RL(-0.22f, 0f, 0.2f, 0f);
                rel = K(0f).Hp(0f, 14f, 0f).Sp(22f, 20f, 0f).Ch(10f, 12f, 0f).Hd(-10f, -15f, 0f).RA(-95f, 0f, 0f).RF(-5f, 0f, 0f).RH(-20f, 0f, 0f).LA(-10f, 0f, 30f).LF(-40f, 0f, 0f).Off(0f, -0.05f, 0.08f).LL(0.4f, 0f, 0.3f, 0f).RL(-0.3f, 0f, 0.2f, 0f);
                follow = K(0.2f).Hp(0f, 16f, 0f).Sp(24f, 28f, 0f).Ch(12f, 16f, 0f).Hd(-8f, -18f, 0f).RA(-40f, 0f, 40f).RF(-30f, 0f, 0f).LA(-5f, 0f, 30f).LF(-40f, 0f, 0f).Off(0f, -0.06f, 0.08f).LL(0.4f, 0f, 0.32f, 0f).RL(-0.3f, 0f, 0.22f, 0f);
            }
            else
            {
                load = K(-0.55f).Sp(6f, -45f, 0f).Ch(4f, -25f, 0f).Hd(0f, 35f, 0f).RA(-50f, 0f, -60f).RF(-70f, 0f, 0f).LA(-40f, 0f, 25f).LF(-50f, 0f, 0f).Off(0f, -0.06f, 0f).LL(0.2f, 0.08f, 0.25f, 0f).RL(0.05f, 0.08f, 0.3f, 0f);
                drive = K(-0.12f).Hp(0f, -8f, 0f).Sp(8f, -15f, 0f).Ch(4f, -8f, 0f).Hd(0f, 18f, 0f).RA(-65f, 0f, -55f).RF(-40f, 0f, 0f).LA(-35f, 0f, 25f).LF(-50f, 0f, 0f).Off(0f, -0.06f, 0f).LL(0.25f, 0.08f, 0.25f, 0f).RL(0f, 0.08f, 0.25f, 0f);
                rel = K(0f).Hp(0f, 12f, 0f).Sp(10f, 32f, 0f).Ch(6f, 18f, 0f).Hd(0f, -20f, 0f).RA(-85f, 0f, -20f).RF(-5f, 0f, 0f).LA(-30f, 0f, 30f).LF(-50f, 0f, 0f).Off(0f, -0.06f, 0.04f).LL(0.3f, 0.08f, 0.25f, 0f).RL(-0.1f, 0.08f, 0.2f, 0f);
                follow = K(0.2f).Hp(0f, 16f, 0f).Sp(10f, 50f, 0f).Ch(6f, 28f, 0f).Hd(0f, -26f, 0f).RA(-70f, 0f, 40f).RF(-30f, 0f, 0f).LA(-30f, 0f, 30f).LF(-50f, 0f, 0f).Off(0f, -0.05f, 0.04f).LL(0.3f, 0.08f, 0.25f, 0f).RL(-0.1f, 0.08f, 0.2f, 0f);
            }
            return new DragonClipKey[] { K(-1f), load, drive, rel, follow, K(0.6f) };
        }

        private static void BuildBlueprintB(Dictionary<string, DragonClipKey[]> c)
        {
            // Sky Command: every remote sky summon / bombardment shares one call-and-lower motion.
            c["cleric_rs"] = SkyCommand(0, false, false);
            c["cleric_rs_asc"] = SkyCommand(0, false, false);
            c["cleric_goddess"] = SkyCommand(0, false, false);
            c["cleric_relic"] = SkyCommand(0, false, false);
            c["cleric_holy_relic"] = SkyCommand(0, false, false);
            c["cleric_judgement"] = SkyCommand(0, false, false);
            c["cleric_tempest"] = SkyCommand(0, false, true);
            c["sorc_glacial"] = SkyCommand(1, false, false);
            c["sorc_glacial_asc"] = SkyCommand(1, false, false);
            c["wiz_meteor"] = SkyCommand(1, true, false);
            c["wiz_cataclysm"] = SkyCommand(1, true, true);
            // Wavecaller: radial heals / buffs / barriers / novas.
            c["cleric_wave"] = Wavecaller(0, 0);
            c["cleric_ray"] = Wavecaller(0, 0);
            c["cleric_light"] = Wavecaller(0, 6);
            c["cleric_intervention"] = Wavecaller(0, 5);
            c["cleric_crucible"] = Wavecaller(0, 1);
            c["wiz_nova"] = Wavecaller(1, 2);
            c["wiz_clockwork"] = Wavecaller(1, 4);
            c["sm_guidance"] = Wavecaller(3, 3);
            c["rg_tailwind"] = Wavecaller(2, 0);
            c["rg_vigil"] = Wavecaller(2, 3);
            // Pulse Drive: punches, cones, orbs, directed blessings.
            c["cleric_zap"] = PulseDrive(0, 0);
            c["cleric_wave_ally"] = PulseDrive(0, 3);
            c["sorc_flame"] = PulseDrive(1, 0);
            c["warrior_punch"] = PulseDrive(3, 1);
            c["hw_gravity_blast"] = PulseDrive(1, 2);
            // Power Pitch: thrown / batted summoned projectiles (the hammer catch overlay stays cleric_hammer_call).
            c["cleric_hammer"] = PowerPitch(false);
            c["merc_bomb"] = PowerPitch(true);
            // ANIM_04 WAR CRY: Battlecry = deliberate gather -> open -> roar -> settle (no trembling).
            DragonClipKey wcOpen = K(0f).Sp(-6f, 0f, 0f).Ch(-14f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-40f, 0f, -60f).RF(-70f, 0f, 0f).LA(-40f, 0f, 60f).LF(-70f, 0f, 0f).Off(0f, 0.02f, 0f).LL(0.1f, 0.12f, 0.15f, 0f).RL(0.1f, 0.12f, 0.15f, 0f);
            c["merc_roar"] = new DragonClipKey[] {
                K(-1f),
                K(-0.6f).Sp(14f, 0f, 0f).Ch(10f, 0f, 0f).Hd(6f, 0f, 0f).RA(-30f, 0f, 20f).RF(-100f, 0f, 0f).LA(-30f, 0f, -20f).LF(-100f, 0f, 0f).Off(0f, -0.06f, 0f).LL(0.25f, 0.05f, 0.25f, 0f).RL(0.25f, 0.05f, 0.25f, 0f),
                wcOpen, wcOpen.Copy(0.3f).Hd(-12f, 0f, 0f), K(0.7f)
            };
            // Automatic procs: shoulder / chest accents only (PlayAccent never interrupts a skill clip).
            c["merc_fury_accent"] = new DragonClipKey[] { K(-1f), K(0f).Sp(-3f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-8f, 0f, 0f).RA(-20f, 0f, -25f).LA(-20f, 0f, 25f), K(0.25f) };
            c["wiz_overcharge"] = new DragonClipKey[] { K(-1f), K(0f).Ch(-8f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-30f, 0f, -45f).RF(-10f, 0f, 0f).LA(-30f, 0f, 45f).LF(-10f, 0f, 0f), K(0.3f) };
            // ANIM_20 PHOENIX RISE (Bless Thy Sinners): upper-body accent only (brace, rise, small opening),
            // no root move, no legs, no lock. Survival is applied at once; no knockdown.
            c["cleric_rise"] = new DragonClipKey[] {
                K(-1f),
                K(-0.5f).Sp(6f, 0f, 0f).Ch(8f, 0f, 0f).Hd(6f, 0f, 0f).RA(-15f, 0f, 10f).LA(-15f, 0f, -10f),
                K(0f).Sp(-3f, 0f, 0f).Ch(-9f, 0f, 0f).Hd(-12f, 0f, 0f).RA(-35f, 0f, -30f).RF(-10f, 0f, 0f).LA(-35f, 0f, 30f).LF(-10f, 0f, 0f),
                K(0.25f)
            };
        }

        // ------------------------------------------------------------------ v0.25.15 Animation Blueprint (user storyboards)
        private static void BuildBlueprintClips(Dictionary<string, DragonClipKey[]> c)
        {
            // ANIM_10 VANGUARD, shield profile (Shield Charge, hold while the charge really runs):
            // BRACE low behind the shield -> DRIVE (Valheim's real run legs via ForceRun, torso leaned in,
            // shield leading at chest height, sword trailing low behind) -> CONTACT shove on the Bash event
            // (left shoulder leads, lunge legs) -> RECOVER weight back over the feet.
            DragonClipKey vgBrace = K(-0.5f).Sp(20f, -10f, 0f).Ch(8f, -6f, 0f).Hd(-14f, 0f, 0f).LA(-75f, 15f, 15f).LF(-80f, 0f, 0f).RA(10f, 0f, -25f).RF(-60f, 0f, 0f).Rot(6f, 0f, 0f).Off(0f, -0.12f, 0f).LL(0.3f, 0f, 0.4f, 0f).RL(-0.1f, 0f, 0.3f, 0f);
            DragonClipKey vgDrive = K(0f).Sp(24f, -12f, 0f).Ch(10f, -8f, 0f).Hd(-26f, 8f, 0f).LA(-78f, 18f, 12f).LF(-70f, 0f, 0f).RA(35f, 0f, -18f).RF(-20f, 0f, 0f).Rot(14f, 0f, 0f).Off(0f, -0.04f, 0f);
            DragonClipKey vgContact = K(0.07f).Sp(26f, -26f, 0f).Ch(12f, -14f, 0f).Hd(-22f, 10f, 0f).LA(-95f, 5f, 8f).LF(-12f, 0f, 0f).LH(-15f, 0f, 0f).RA(40f, 0f, -20f).RF(-25f, 0f, 0f).Rot(14f, 0f, 0f).Off(0f, -0.14f, 0.18f).LL(0.5f, 0f, 0.45f, 0f).RL(-0.35f, 0f, 0.2f, 0f);
            DragonClipKey vgRecover = K(0.45f).Sp(8f, -4f, 0f).Ch(2f, 0f, 0f).LA(-60f, 15f, 15f).LF(-70f, 0f, 0f).RA(5f, 0f, -20f).RF(-40f, 0f, 0f).Off(0f, -0.06f, 0f).LL(0.15f, 0f, 0.2f, 0f).RL(0f, 0f, 0.15f, 0f);
            c["cleric_charge"] = new DragonClipKey[] { K(-1f), vgBrace, vgDrive, vgContact, vgContact.Copy(0.2f), vgRecover, K(0.8f) };

            // ANIM_10 VANGUARD, thrust profile (Frenzied Charge): blade drawn back in a braced lunge (free
            // hand guides), then the thrust leads the dash (real run legs while the dash moves the body).
            DragonClipKey fvBack = K(-0.4f).Sp(16f, -30f, 0f).Ch(6f, -16f, 0f).Hd(-10f, 20f, 0f).RA(35f, 0f, -20f).RF(-80f, 0f, 0f).LA(-75f, 10f, 15f).LF(-15f, 0f, 0f).Rot(8f, 0f, 0f).Off(0f, -0.16f, -0.05f).LL(0.4f, 0f, 0.55f, 0f).RL(-0.2f, 0f, 0.4f, 0f);
            DragonClipKey fvThrust = K(0f).Sp(20f, 15f, 0f).Ch(8f, 8f, 0f).Hd(-16f, -6f, 0f).RA(-88f, 0f, -4f).RF(-5f, 0f, 0f).LA(15f, 0f, 30f).LF(-20f, 0f, 0f).Rot(16f, 0f, 0f).Off(0f, -0.06f, 0.1f);
            c["sm_thrust"] = new DragonClipKey[] { K(-1f), fvBack, fvBack.Copy(-0.08f), fvThrust, fvThrust.Copy(0.4f), K(0.75f) };

            // ANIM_01 OLYMPIC HERO (Electric Smite holy / Bonecrusher brutal). Hold clip: wind up = takeoff +
            // ascent + hang; the roll is spread over ascent and apex, then the poised falling pose is held
            // through any descent (cliffs) until GROUND_CONTACT (ClipImpact) -> superhero landing.
            c["olympic_hero"] = OlympicHero(false);
            c["olympic_hero_brutal"] = OlympicHero(true);
            // Fallback landing when nothing was holding (only the contact + recovery part).
            DragonClipKey[] oh = c["olympic_hero"];
            List<DragonClipKey> land = new List<DragonClipKey>();
            land.Add(oh[6].Copy(-1f));
            for (int i = 6; i < oh.Length; i++) land.Add(oh[i]);
            c["olympic_land"] = land.ToArray();
        }

        private static DragonClipKey[] OlympicHero(bool brutal)
        {
            float d = brutal ? 1.15f : 1f;   // brutal: deeper compression, heavier settle
            // 1 LOAD: knees compressed, hips/shoulders coiled, main elbow chambered, shield close.
            DragonClipKey load = K(-0.93f).Sp(22f * d, -8f, 0f).Ch(10f, -6f, 0f).Hd(-14f, 0f, 0f).RA(-25f, 0f, -30f).RF(-115f, 0f, 0f).LA(-25f, 10f, 10f).LF(-35f, 0f, 0f).Off(0f, -0.22f * d, 0f).LL(0.45f * d, 0f, 0.7f * d, 0f).RL(0.45f * d, 0f, 0.7f * d, 0f);
            // 2 LAUNCH: legs extend through the jump, torso inclines almost parallel to the ground.
            DragonClipKey launch = K(-0.72f).Sp(6f, 0f, 0f).Ch(2f, 0f, 0f).Hd(-40f, 0f, 0f).RA(-160f, 0f, -10f).RF(-10f, 0f, 0f).LA(-25f, 10f, 10f).LF(-40f, 0f, 0f).Rot(70f, 0f, 0f).Off(0f, 0.1f, 0f).LL(-0.1f, 0f, 0.05f, -0.3f).RL(-0.1f, 0f, 0.05f, -0.3f);
            // 3 ROLL (v0.25.21 guide: fast, counter-clockwise): one full turn around the head-to-feet axis while horizontal, knees loosely tucked.
            DragonClipKey roll0 = K(-0.62f).Sp(6f, 0f, 0f).Ch(2f, 0f, 0f).Hd(-38f, 0f, 0f).RA(-165f, 0f, -8f).RF(-8f, 0f, 0f).LA(-25f, 10f, 10f).LF(-40f, 0f, 0f).Rot(72f, 0f, 0f).Off(0f, 0.1f, 0f).LL(0.3f, 0f, 0.55f, 0f).RL(0.2f, 0f, 0.65f, 0f).Sn(25f);
            DragonClipKey roll1 = roll0.Copy(-0.36f).Sn(360f);
            // 4 UNWIND: turn complete, legs unfold (opposite foot forward), fist cocked above - not touching.
            DragonClipKey poised = K(0f).Sp(14f, 0f, 0f).Ch(8f, 0f, 0f).Hd(-6f, 0f, 0f).RA(-35f, 0f, -10f).RF(-15f, 0f, 0f).LA(-25f, 15f, 12f).LF(-40f, 0f, 0f).Rot(28f, 0f, 0f).LL(0.6f, 0f, 0.45f, 0.1f).RL(-0.3f, 0f, 0.6f, 0f).Sn(360f);
            // 5 IMPACT: main fist on the ground (elbow slightly bent), left foot planted forward, right knee
            // folded behind near the ground, torso over the fist, head up toward the action.
            DragonClipKey impact = K(0.08f).Sp(34f, 0f, 0f).Ch(16f, 0f, 0f).Hd(-30f, 0f, 0f).RA(-45f, 0f, -10f).RF(-12f, 0f, 0f).RH(10f, 0f, 0f).LA(-20f, 25f, 15f).LF(-30f, 0f, 0f).Rot(6f, 0f, 0f).Off(0f, -0.42f * d, 0.05f).LL(0.75f, 0.15f, 0.95f, 0.15f).RL(-0.8f, 0.15f, 1.2f, -0.2f).Sn(360f);
            DragonClipKey settle = impact.Copy(brutal ? 0.3f : 0.2f).Off(0f, -0.44f * d, 0.05f);
            // 6 RECOVER: push through the forward foot, fist lifts, back to the combat pose.
            DragonClipKey rec = K(brutal ? 0.58f : 0.45f).Sp(16f, 0f, 0f).Ch(6f, 0f, 0f).Hd(-8f, 0f, 0f).RA(-15f, 0f, -15f).RF(-40f, 0f, 0f).LA(-15f, 10f, 10f).LF(-25f, 0f, 0f).Off(0f, -0.12f, 0f).LL(0.3f, 0f, 0.35f, 0f).RL(0f, 0f, 0.3f, 0f).Sn(360f);
            return new DragonClipKey[] { K(-1f), load, launch, roll0, roll1, poised, impact, settle, rec, K(brutal ? 0.9f : 0.75f).Sn(360f) };
        }

        // ------------------------------------------------------------------ v0.25.13 traits / Ascended extras
        private static void BuildTraitClips(Dictionary<string, DragonClipKey[]> c)
        {
            // Ascended Judgement Hammer: hand stretched out calling the hammer back (hold), catch + recoil.
            DragonClipKey call = K(0f).Sp(-6f, 8f, 0f).Ch(-4f, 6f, 0f).Hd(-6f, 0f, 0f).RA(-115f, 0f, -8f).RF(-10f, 0f, 0f).RH(-30f, 0f, 0f).LA(-20f, 0f, 25f);
            c["cleric_hammer_call"] = new DragonClipKey[] {
                K(-1f), call,
                K(0.08f).Sp(-10f, 4f, 0f).Ch(-8f, 2f, 0f).RA(-95f, 0f, -8f).RF(-75f, 0f, 0f).RH(10f, 0f, 0f).LA(-20f, 0f, 25f).Rot(-6f, 0f, 0f).Off(0f, -0.04f, -0.06f),
                K(0.25f).Sp(-6f, 0f, 0f).RA(-90f, 0f, -8f).RF(-70f, 0f, 0f).LA(-20f, 0f, 25f).Rot(-3f, 0f, 0f),
                K(0.6f)
            };
            // Holy Shockwave (Buckler Parry): the buckler is shoved out, the light bursts from it.
            DragonClipKey pb = K(0f).Sp(10f, 14f, 0f).Ch(6f, 10f, 0f).LA(-88f, 15f, 10f).LF(-10f, 0f, 0f).LH(-25f, 0f, 0f).RA(-20f, 0f, -30f).RF(-50f, 0f, 0f).Rot(5f, 6f, 0f).Off(0f, -0.06f, 0.08f);
            c["cleric_parry_burst"] = new DragonClipKey[] {
                K(-1f).LA(-70f, 25f, 20f).LF(-80f, 0f, 0f).Sp(4f, -6f, 0f),
                pb, pb.Copy(0.15f), K(0.5f)
            };
            // Bless Thy Sinners: snatched back from death - crumpled low, then rising with arms opening.
            c["cleric_rise"] = new DragonClipKey[] {
                K(-1f).Sp(34f, 0f, 0f).Ch(16f, 0f, 0f).Hd(24f, 0f, 0f).RA(-30f, 0f, -10f).LA(-30f, 0f, 10f).Rot(12f, 0f, 0f).Off(0f, -0.38f, 0f),
                K(-0.4f).Sp(20f, 0f, 0f).Ch(8f, 0f, 0f).Hd(6f, 0f, 0f).RA(-60f, 0f, -25f).LA(-60f, 0f, 25f).Rot(6f, 0f, 0f).Off(0f, -0.2f, 0f),
                K(0f).Sp(-12f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-24f, 0f, 0f).RA(-120f, 0f, -55f).RF(-10f, 0f, 0f).LA(-120f, 0f, 55f).LF(-10f, 0f, 0f).Off(0f, 0.06f, 0f),
                K(0.5f).Sp(-10f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-20f, 0f, 0f).RA(-115f, 0f, -55f).LA(-115f, 0f, 55f).Off(0f, 0.05f, 0f),
                K(1.0f)
            };
            // Archmage Overcharge: energy bursts out - arms flung wide and down, head back, shaking.
            DragonClipKey oc = K(0f).Sp(-16f, 0f, 0f).Ch(-14f, 0f, 0f).Hd(-24f, 0f, 0f).RA(-45f, 0f, -70f).RF(-10f, 0f, 0f).RH(-30f, 0f, 0f).LA(-45f, 0f, 70f).LF(-10f, 0f, 0f).LH(-30f, 0f, 0f).Off(0f, 0.04f, 0f);
            c["wiz_overcharge"] = new DragonClipKey[] {
                K(-1f).Sp(16f, 0f, 0f).Ch(8f, 0f, 0f).RA(-30f, 20f, 25f).RF(-80f, 0f, 0f).LA(-30f, -20f, -25f).LF(-80f, 0f, 0f).Off(0f, -0.08f, 0f),
                oc, oc.Copy(0.45f), K(0.85f)
            };
            // Ascended Bonecrusher ground shock: a second stamp into the crater.
            DragonClipKey af = K(0f).Sp(30f, 0f, 0f).Ch(14f, 0f, 0f).RA(-35f, 0f, -12f).LA(-30f, 0f, 30f).Rot(10f, 0f, 0f).Off(0f, -0.22f, 0.04f);
            c["merc_aftershock"] = new DragonClipKey[] { K(-1f), K(-0.5f).Sp(14f, 0f, 0f).RA(-80f, 0f, -12f).Off(0f, -0.08f, 0f), af, af.Copy(0.2f), K(0.55f) };
            // Tumble Shot / Gale Volley: tucked backflip, bow drawn upside down at the top, loosed on the way out.
            DragonClipKey tk = K(0f).Sp(26f, 0f, 0f).Ch(12f, 0f, 0f).RA(-70f, 0f, -20f).RF(-110f, 0f, 0f).LA(-70f, 0f, 20f).LF(-70f, 0f, 0f);
            DragonClipKey dr = Draw(0f);
            c["rg_tumble"] = new DragonClipKey[] {
                K(-1f), tk.Copy(0f),
                tk.Copy(0.075f).Rot(-90f, 0f, 0f).Linear(),
                dr.Copy(0.15f).Rot(-180f, 0f, 0f).Linear(),
                Loose(0.225f).Rot(-270f, 0f, 0f).Linear(),
                Loose(0.3f).Rot(-360f, 0f, 0f).Linear(),
                K(0.5f).Rot(-360f, 0f, 0f)
            };
        }

        // ------------------------------------------------------------------ Ranger / Acrobat / Bowmaster
        // Bow in the LEFT hand (arm straight at the target), the right hand draws the string to the cheek;
        // archers stand side-on (spine twisted, head turned back along the arrow).
        private static DragonClipKey Draw(float t)
        {
            return K(t).Sp(4f, -22f, 0f).Ch(0f, -12f, 0f).Hd(0f, 22f, 0f).LA(-88f, 0f, 12f).LF(-4f, 0f, 0f).RA(-85f, 0f, -12f).RF(-140f, 0f, 0f);
        }

        private static DragonClipKey Loose(float t)
        {
            return K(t).Sp(0f, -18f, 0f).Ch(-4f, -10f, 0f).Hd(0f, 20f, 0f).LA(-86f, 0f, 12f).LF(-4f, 0f, 0f).RA(-80f, 0f, -35f).RF(-55f, 0f, 0f).RH(15f, 0f, 0f);
        }

        private static DragonClipKey SkyDraw(float t)
        {
            return K(t).Sp(-16f, -14f, 0f).Ch(-12f, -8f, 0f).Hd(-30f, 14f, 0f).LA(-160f, 0f, 10f).LF(-4f, 0f, 0f).RA(-150f, 0f, -14f).RF(-135f, 0f, 0f).Rot(-8f, 0f, 0f);
        }

        private static void BuildRangerClips(Dictionary<string, DragonClipKey[]> c)
        {
            // Piercing Arrow: snap to full draw, release with a strong recoil.
            c["rg_power"] = new DragonClipKey[] { K(-1f), Draw(-0.4f), Draw(-0.05f), Loose(0f).Rot(-10f, -10f, 0f).Off(0f, 0f, -0.15f), Loose(0.2f).Rot(-6f, -6f, 0f).Off(0f, 0f, -0.1f), K(0.55f) };
            // Explosive Arrow: same, heavier kick-back.
            c["rg_heavy"] = new DragonClipKey[] { K(-1f), Draw(-0.4f).Off(0f, -0.06f, 0f), Draw(-0.05f).Off(0f, -0.06f, 0f), Loose(0f).Rot(-18f, 0f, 0f).Off(0f, -0.04f, -0.28f), Loose(0.25f).Rot(-10f, 0f, 0f).Off(0f, -0.03f, -0.18f), K(0.6f) };
            // Snare Trap: kneel and set the trap with the right hand on the ground.
            DragonClipKey trap = K(0f).Sp(30f, 0f, 0f).Ch(12f, 0f, 0f).Hd(10f, 0f, 0f).RA(-40f, 0f, -10f).RF(-10f, 0f, 0f).RH(-20f, 0f, 0f).LA(-20f, 0f, 30f).LF(-40f, 0f, 0f).Rot(10f, 0f, 0f).Off(0f, -0.35f, 0f);
            c["rg_trap"] = new DragonClipKey[] { K(-1f), trap, trap.Copy(0.25f), K(0.6f) };
            // Cyclone Arrow: drawn, full spin, loosed out of the turn.
            c["rg_spin"] = Join(K(-1f), Draw(-0.3f), Spin360(Draw(0f), 0f, 0.4f, 0.0f), Loose(0.45f), K(0.75f));
            // Swallow Dive: arms swept back, head-first dive.
            DragonClipKey dive = K(0f).Sp(10f, 0f, 0f).Ch(6f, 0f, 0f).Hd(-20f, 0f, 0f).RA(45f, 0f, -30f).LA(45f, 0f, 30f).Rot(70f, 0f, 0f);
            c["rg_dive"] = new DragonClipKey[] { K(-1f), dive, dive.Copy(0.15f), K(0.45f) };
            // Skyfall Barrage: tucked backflip into the air, then hovering aimed down (hold).
            DragonClipKey tuck = K(0f).Sp(24f, 0f, 0f).Ch(12f, 0f, 0f).RA(-60f, 0f, -20f).RF(-90f, 0f, 0f).LA(-60f, 0f, 20f).LF(-90f, 0f, 0f);
            c["rg_backflip"] = new DragonClipKey[] {
                K(-1f), tuck.Copy(0f),
                tuck.Copy(0.125f).Rot(-90f, 0f, 0f).Linear(), tuck.Copy(0.25f).Rot(-180f, 0f, 0f).Linear(),
                tuck.Copy(0.375f).Rot(-270f, 0f, 0f).Linear(), tuck.Copy(0.5f).Rot(-360f, 0f, 0f).Linear(),
                K(0.65f).Rot(-360f, 0f, 0f)
            };
            DragonClipKey hover = Draw(0f).LA(-40f, 0f, 12f).RA(-40f, 0f, -12f).Hd(20f, 22f, 0f).Rot(35f, 0f, 0f);
            c["rg_hover"] = new DragonClipKey[] { K(-1f), hover, K(0.3f) };
            // Ricochet Arrow: flicked trick shot from the hip, body twisting.
            c["rg_trick"] = new DragonClipKey[] { K(-1f), Draw(-0.5f).Rot(0f, -10f, 6f), Loose(0f).Rot(-6f, 30f, -10f), Loose(0.15f).Rot(-4f, 26f, -8f), K(0.45f) };
            // Tailwind: arms rise out to the sides, palms up, calling the wind.
            DragonClipKey wind = K(0f).Sp(-8f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-18f, 0f, 0f).RA(-120f, 0f, -60f).RF(-10f, 0f, 0f).RH(-30f, 0f, 0f).LA(-120f, 0f, 60f).LF(-10f, 0f, 0f).LH(-30f, 0f, 0f).Off(0f, 0.08f, 0f);
            c["rg_tailwind"] = new DragonClipKey[] { K(-1f), wind, wind.Copy(0.3f), K(0.7f) };
            // Hawk's Vigil: right hand at the brow, scanning the horizon left to right.
            DragonClipKey vig = K(0f).Hd(-6f, -25f, 0f).RA(-100f, 0f, 20f).RF(-135f, 0f, 0f).RH(-20f, 0f, 0f).LA(-20f, 0f, 20f);
            c["rg_vigil"] = new DragonClipKey[] { K(-1f), vig, vig.Copy(0.3f).Hd(-6f, 25f, 0f), vig.Copy(0.5f).Hd(-6f, 0f, 0f), K(0.8f) };
            // Arrow Rain: lean back, bow raised to the sky, loose high.
            c["rg_sky"] = new DragonClipKey[] { K(-1f), SkyDraw(-0.5f), SkyDraw(-0.05f), SkyDraw(0f).RF(-60f, 0f, 0f).RA(-145f, 0f, -35f), SkyDraw(0.2f).RF(-60f, 0f, 0f).RA(-145f, 0f, -35f), K(0.6f) };
            // Starfall Volley: sky draw held through the channel, loosed at the end.
            c["rg_starfall"] = new DragonClipKey[] { K(-1f), SkyDraw(-0.8f), SkyDraw(-0.05f), SkyDraw(0f).RF(-60f, 0f, 0f).RA(-145f, 0f, -35f), K(0.45f) };
            // Pinning Shot: drop to one knee, steady draw, release.
            c["rg_kneel"] = new DragonClipKey[] { K(-1f), Draw(-0.5f).Off(0f, -0.3f, 0f).Rot(8f, 0f, 0f), Draw(-0.05f).Off(0f, -0.32f, 0f).Rot(8f, 0f, 0f), Loose(0f).Off(0f, -0.32f, -0.05f).Rot(4f, 0f, 0f), Loose(0.25f).Off(0f, -0.3f, 0f).Rot(4f, 0f, 0f), K(0.6f) };
            // Ballista Shot (hold while charging): crouched heavy draw, released with a siege recoil.
            c["rg_ballista"] = new DragonClipKey[] {
                K(-1f), Draw(-0.5f).Off(0f, -0.12f, 0f).Rot(-4f, 0f, 0f),
                Draw(0f).RF(-150f, 0f, 0f).Off(0f, -0.15f, -0.04f).Rot(-6f, 0f, 0f),
                Loose(0.06f).Rot(-20f, 0f, 0f).Off(0f, -0.08f, -0.35f), Loose(0.3f).Rot(-10f, 0f, 0f).Off(0f, -0.05f, -0.2f), K(0.7f)
            };
            // Splitting Arrow volleys: quick draw sweeping across the cone (alternating sides).
            c["rg_split_a"] = new DragonClipKey[] { K(-1f), Draw(-0.5f).Rot(0f, -18f, 0f), Loose(0f).Rot(-4f, 20f, 0f), K(0.3f).Rot(0f, 10f, 0f) };
            c["rg_split_b"] = new DragonClipKey[] { K(-1f), Draw(-0.5f).Rot(0f, 18f, 0f), Loose(0f).Rot(-4f, -20f, 0f), K(0.3f).Rot(0f, -10f, 0f) };
        }

        // ------------------------------------------------------------------ Sorcerer / Archmage / Horizon Walker
        private static void BuildSorcererClips(Dictionary<string, DragonClipKey[]> c)
        {
            // Flame Burst: staff drawn back to the right shoulder, thrust forward, free palm pushes the fire.
            DragonClipKey fbBack = K(-0.45f).Sp(0f, -20f, 0f).Ch(-4f, -12f, 0f).RA(-50f, 0f, -25f).RF(-90f, 0f, 0f).LA(-30f, 0f, 25f).LF(-60f, 0f, 0f);
            DragonClipKey fbOut = K(0f).Sp(8f, 14f, 0f).Ch(6f, 8f, 0f).RA(-80f, -6f, -10f).RF(-8f, 0f, 0f).LA(-85f, 12f, 10f).LF(-6f, 0f, 0f).LH(-30f, 0f, 0f).Rot(5f, 0f, 0f).Off(0f, -0.03f, 0.06f);
            c["sorc_flame"] = new DragonClipKey[] { K(-1f), fbBack, fbOut, fbOut.Copy(0.18f), K(0.5f) };
            // Glacial Descent: staff to the sky, free hand open; swing down to point at the impact.
            DragonClipKey gdUp = K(-0.55f).Sp(-10f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-18f, 0f, 0f).RA(-160f, 0f, -12f).RF(-15f, 0f, 0f).LA(-110f, 0f, 35f).LH(-30f, 0f, 0f).Off(0f, 0.04f, 0f);
            DragonClipKey gdDown = K(0f).Sp(14f, 0f, 0f).Ch(8f, 0f, 0f).Hd(4f, 0f, 0f).RA(-85f, 0f, -6f).RF(-4f, 0f, 0f).LA(-20f, 0f, 30f).Rot(6f, 0f, 0f).Off(0f, -0.04f, 0.04f);
            c["sorc_glacial"] = new DragonClipKey[] { K(-1f), gdUp, gdUp.Copy(-0.1f).RA(-165f, 0f, -12f), gdDown, gdDown.Copy(0.3f), K(0.65f) };
            // Ascended Glacial: both hands to the sky, the whole body pulls the glacier down.
            DragonClipKey gaUp = K(-0.5f).Sp(-14f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-24f, 0f, 0f).RA(-160f, 0f, -25f).RF(-15f, 0f, 0f).LA(-160f, 0f, 25f).LF(-15f, 0f, 0f).Off(0f, 0.07f, 0f);
            DragonClipKey gaDown = K(0f).Sp(30f, 0f, 0f).Ch(14f, 0f, 0f).Hd(10f, 0f, 0f).RA(-50f, 0f, -20f).LA(-50f, 0f, 20f).Rot(10f, 0f, 0f).Off(0f, -0.16f, 0.06f);
            c["sorc_glacial_asc"] = new DragonClipKey[] { K(-1f), gaUp, gaUp.Copy(-0.1f).Rot(-4f, 0f, 0f), gaDown, gaDown.Copy(0.35f), K(0.8f) };
            // Stonefang Eruption: staff lifted upright, butt driven into the ground in a crouch, rise as fangs erupt.
            DragonClipKey sfUp = K(-0.45f).Sp(-8f, 0f, 0f).Ch(-6f, 0f, 0f).RA(-120f, 0f, -10f).RF(-60f, 0f, 0f).LA(-110f, 0f, 10f).LF(-60f, 0f, 0f).Off(0f, 0.06f, 0f);
            DragonClipKey sfDown = K(0f).Sp(32f, 0f, 0f).Ch(14f, 0f, 0f).Hd(8f, 0f, 0f).RA(-40f, 0f, -10f).RF(-30f, 0f, 0f).LA(-40f, 0f, 10f).LF(-30f, 0f, 0f).Rot(9f, 0f, 0f).Off(0f, -0.2f, 0.04f);
            DragonClipKey sfRise = K(0.25f).Sp(-8f, 0f, 0f).Ch(-6f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-60f, 0f, -45f).LA(-60f, 0f, 45f).Off(0f, 0.04f, 0f);
            c["sorc_stonefang"] = new DragonClipKey[] { K(-1f), sfUp, sfDown, sfDown.Copy(0.1f), sfRise, K(0.7f) };
            c["sorc_stonefang_asc"] = new DragonClipKey[] { K(-1f), sfUp, sfDown, sfDown.Copy(0.15f), sfRise.Copy(0.3f), sfDown.Copy(0.5f), sfRise.Copy(0.8f), sfDown.Copy(1.0f), sfRise.Copy(1.3f), K(1.7f) };
            // Meteor Fall (hold while charging): both arms raised to the sky, trembling; release drags the sky down.
            DragonClipKey mtUp = K(0f).Sp(-14f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-26f, 0f, 0f).RA(-165f, 0f, -20f).RF(-10f, 0f, 0f).LA(-165f, 0f, 20f).LF(-10f, 0f, 0f).Off(0f, 0.06f, 0f);
            DragonClipKey mtDown = K(0.15f).Sp(28f, 0f, 0f).Ch(14f, 0f, 0f).Hd(8f, 0f, 0f).RA(-60f, 0f, -15f).LA(-60f, 0f, 15f).Rot(9f, 0f, 0f).Off(0f, -0.14f, 0.06f);
            c["wiz_meteor"] = new DragonClipKey[] { K(-1f), mtUp.Copy(-0.4f), mtUp, mtDown, mtDown.Copy(0.45f), K(0.85f) };
            // Elemental Cataclysm (hold): arms spread wide to the sky, body arched; release thrusts everything forward.
            DragonClipKey ccUp = K(0f).Sp(-18f, 0f, 0f).Ch(-16f, 0f, 0f).Hd(-28f, 0f, 0f).RA(-140f, 0f, -55f).RF(-10f, 0f, 0f).LA(-140f, 0f, 55f).LF(-10f, 0f, 0f).Rot(-6f, 0f, 0f).Off(0f, 0.08f, 0f);
            DragonClipKey ccOut = K(0.15f).Sp(22f, 0f, 0f).Ch(12f, 0f, 0f).Hd(-4f, 0f, 0f).RA(-85f, -10f, -8f).RF(-4f, 0f, 0f).LA(-85f, 10f, 8f).LF(-4f, 0f, 0f).Rot(10f, 0f, 0f).Off(0f, -0.1f, 0.1f);
            c["wiz_cataclysm"] = new DragonClipKey[] { K(-1f), ccUp.Copy(-0.4f), ccUp, ccOut, ccOut.Copy(0.6f), K(1.0f) };
            // Gravity Dominion: both arms reach out to the target, then fists dragged back to the chest.
            DragonClipKey gvReach = K(-0.5f).Sp(8f, 0f, 0f).Ch(6f, 0f, 0f).RA(-88f, -10f, -10f).RF(-5f, 0f, 0f).RH(-20f, 0f, 0f).LA(-88f, 10f, 10f).LF(-5f, 0f, 0f).LH(-20f, 0f, 0f).Off(0f, 0f, 0.05f);
            DragonClipKey gvPull = K(0f).Sp(-10f, 0f, 0f).Ch(-8f, 0f, 0f).RA(-50f, 20f, 15f).RF(-110f, 0f, 0f).LA(-50f, -20f, -15f).LF(-110f, 0f, 0f).Rot(-4f, 0f, 0f).Off(0f, -0.1f, -0.06f);
            c["wiz_gravity"] = new DragonClipKey[] { K(-1f), gvReach, gvReach.Copy(-0.15f), gvPull, gvPull.Copy(0.5f), K(0.9f) };
            // Astral Railcannon: brace low, staff levelled like a cannon (both hands), recoil on the shot.
            DragonClipKey rcAim = K(-0.4f).Sp(10f, -10f, 0f).Ch(4f, -6f, 0f).RA(-88f, 0f, -6f).RF(-15f, 0f, 0f).LA(-80f, 15f, 15f).LF(-35f, 0f, 0f).Off(0f, -0.1f, 0f);
            DragonClipKey rcKick = K(0f).Sp(-12f, -6f, 0f).Ch(-8f, -4f, 0f).Hd(-6f, 0f, 0f).RA(-100f, 0f, -6f).RF(-20f, 0f, 0f).LA(-92f, 15f, 15f).LF(-40f, 0f, 0f).Rot(-8f, 0f, 0f).Off(0f, -0.06f, -0.3f);
            c["wiz_railcannon"] = new DragonClipKey[] { K(-1f), rcAim, rcAim.Copy(-0.05f), rcKick, rcAim.Copy(0.3f), K(0.7f) };
            c["wiz_railcannon_hold"] = new DragonClipKey[] { K(-1f), rcAim, rcAim.Copy(0f).Off(0f, -0.1f, -0.05f), K(0.25f) };
            // Astral Greatblade: conjured blade raised two-handed overhead, crushing slam.
            DragonClipKey ab = K(0f).Sp(36f, 0f, 0f).Ch(16f, 0f, 0f).Hd(10f, 0f, 0f).RA(-30f, 0f, -4f).RF(-4f, 0f, 0f).LA(-30f, 0f, 6f).LF(-4f, 0f, 0f).Rot(12f, 0f, 0f).Off(0f, -0.18f, 0.08f);
            c["wiz_greatblade"] = new DragonClipKey[] {
                K(-1f),
                K(-0.45f).Sp(-14f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-168f, 0f, -6f).RF(-25f, 0f, 0f).LA(-160f, 0f, 8f).LF(-30f, 0f, 0f).Off(0f, 0.04f, 0f),
                ab, ab.Copy(0.35f), K(0.8f)
            };
            c["wiz_greatblade_slam"] = new DragonClipKey[] {
                K(-1f),
                K(-0.5f).Sp(-12f, 0f, 0f).Ch(-8f, 0f, 0f).RA(-160f, 0f, -6f).RF(-25f, 0f, 0f).LA(-150f, 0f, 8f).LF(-30f, 0f, 0f).Off(0f, 0.04f, 0f),
                ab, ab.Copy(0.25f), K(0.6f)
            };
            // Frost Nova: curl inward with the cold gathering, then burst wide open.
            DragonClipKey fnIn = K(-0.4f).Sp(24f, 0f, 0f).Ch(14f, 0f, 0f).Hd(14f, 0f, 0f).RA(-40f, 20f, 30f).RF(-60f, 0f, 0f).LA(-40f, -20f, -30f).LF(-60f, 0f, 0f).Rot(5f, 0f, 0f).Off(0f, -0.15f, 0f);
            DragonClipKey fnOut = K(0f).Sp(-14f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-90f, 0f, -75f).RF(-5f, 0f, 0f).LA(-90f, 0f, 75f).LF(-5f, 0f, 0f).Rot(-5f, 0f, 0f).Off(0f, 0.05f, 0f);
            c["wiz_nova"] = new DragonClipKey[] { K(-1f), fnIn, fnIn.Copy(-0.06f), fnOut, fnOut.Copy(0.3f), K(0.7f) };
            // Clockwork: hand raised tracing a clock face, then snapped open.
            c["wiz_clockwork"] = new DragonClipKey[] {
                K(-1f),
                K(0f).Hd(-10f, 0f, 0f).RA(-115f, 0f, -20f).RF(-40f, 0f, 0f).RH(-20f, 0f, 0f).LA(-40f, 0f, 30f),
                K(0.15f).Hd(-10f, 0f, 0f).RA(-120f, 0f, 10f).RF(-40f, 0f, 0f).RH(-20f, 0f, 0f).LA(-40f, 0f, 30f),
                K(0.3f).Ch(-6f, 0f, 0f).Hd(-14f, 0f, 0f).RA(-120f, 0f, -55f).RF(-5f, 0f, 0f).LA(-100f, 0f, 55f).Off(0f, 0.04f, 0f),
                K(0.7f)
            };
            // Horizon Walker (no wind up: quick, mobile gestures).
            DragonClipKey phCmd = K(0f).Sp(6f, 18f, 0f).Ch(4f, 10f, 0f).RA(-95f, 0f, 20f).RF(-5f, 0f, 0f).RH(-15f, 0f, 0f).LA(-20f, 0f, 30f).Rot(3f, 10f, 0f);
            c["hw_command"] = new DragonClipKey[] { K(-1f).RA(-120f, 0f, -40f).RF(-30f, 0f, 0f), phCmd, phCmd.Copy(0.15f), K(0.45f) };
            DragonClipKey aaSweep = K(0f).Sp(8f, 30f, 0f).Ch(4f, 18f, 0f).RA(-85f, 0f, 45f).LA(-85f, 0f, 60f).Rot(4f, 20f, 0f);
            c["hw_afterimage"] = new DragonClipKey[] { K(-1f).Sp(4f, -25f, 0f).RA(-80f, 0f, -60f).LA(-60f, 0f, 20f), aaSweep, aaSweep.Copy(0.2f), K(0.5f) };
            DragonClipKey vsDash = K(0f).Sp(24f, 0f, 0f).Ch(12f, 0f, 0f).Hd(-10f, 0f, 0f).RA(30f, 0f, -20f).RF(-20f, 0f, 0f).LA(30f, 0f, 20f).LF(-20f, 0f, 0f).Rot(18f, 0f, 0f).Off(0f, -0.12f, 0.08f);
            c["hw_voidstep"] = new DragonClipKey[] { K(-1f), vsDash, vsDash.Copy(0.12f), K(0.35f) };
            DragonClipKey reCut = K(0f).Sp(14f, 24f, 0f).Ch(8f, 12f, 0f).RA(-40f, 0f, 30f).RF(-6f, 0f, 0f).LA(-20f, 0f, 30f).Rot(6f, 10f, -6f).Off(0f, -0.06f, 0.05f);
            c["hw_rift_echo"] = new DragonClipKey[] { K(-1f).Sp(-4f, -24f, 0f).RA(-150f, 0f, -45f).RF(-30f, 0f, 0f).Rot(0f, 0f, 4f), reCut, reCut.Copy(0.18f), K(0.5f) };
            DragonClipKey ruAim = K(-0.5f).Sp(6f, 0f, 0f).RA(-90f, 0f, -6f).RF(-5f, 0f, 0f).RH(-35f, 0f, 0f).LA(-30f, 0f, 30f);
            DragonClipKey ruClench = K(0f).Sp(10f, 0f, 0f).Ch(6f, 0f, 0f).RA(-88f, 0f, -6f).RF(-40f, 0f, 0f).RH(20f, 0f, 0f).LA(-30f, 0f, 30f).Rot(4f, 0f, 0f).Off(0f, -0.04f, 0.03f);
            c["hw_rupture"] = new DragonClipKey[] { K(-1f), ruAim, ruAim.Copy(-0.1f), ruClench, ruClench.Copy(0.25f), K(0.6f) };
            DragonClipKey gbBack = K(-0.5f).Sp(-6f, 0f, 0f).RA(-60f, 20f, 15f).RF(-100f, 0f, 0f).LA(-60f, -20f, -15f).LF(-100f, 0f, 0f).Off(0f, -0.04f, -0.04f);
            DragonClipKey gbPush = K(0f).Sp(14f, 0f, 0f).Ch(8f, 0f, 0f).RA(-90f, -10f, -8f).RF(-4f, 0f, 0f).RH(-30f, 0f, 0f).LA(-90f, 10f, 8f).LF(-4f, 0f, 0f).LH(-30f, 0f, 0f).Rot(6f, 0f, 0f).Off(0f, -0.05f, 0.1f);
            c["hw_gravity_blast"] = new DragonClipKey[] { K(-1f), gbBack, gbPush, gbPush.Copy(0.2f), K(0.55f) };
            DragonClipKey rwOpen = K(0f).Ch(-6f, 0f, 0f).Hd(-6f, 0f, 0f).RA(-100f, 0f, -60f).RF(-15f, 0f, 0f).LA(-100f, 0f, 60f).LF(-15f, 0f, 0f).Off(0f, 0.03f, 0f);
            c["hw_rift_walker"] = new DragonClipKey[] { K(-1f).RA(-100f, 0f, 10f).RF(-40f, 0f, 0f).LA(-100f, 0f, -10f).LF(-40f, 0f, 0f), rwOpen, rwOpen.Copy(0.3f), K(0.7f) };
        }

        // ------------------------------------------------------------------ Warrior / Sword Master / Mercenary
        private static DragonClipKey[] Spin360(DragonClipKey pose, float start, float turn, float settle)
        {
            DragonClipKey[] k = new DragonClipKey[6];
            for (int i = 0; i < 5; i++)
            {
                k[i] = pose.Copy(start + turn * i / 4f);
                k[i].R = new Vector3(pose.R.x, pose.R.y + 90f * i, pose.R.z);
                k[i].Lin = i > 0;
                // v0.25.17 Cyclone: the turn stands on a wide pivot stance unless the pose has its own legs.
                if (k[i].L[2] == 0f && k[i].L[6] == 0f) k[i].LL(0.12f, 0.14f, 0.25f, 0f).RL(0.1f, 0.14f, 0.25f, 0f);
            }
            k[5] = K(start + turn + settle).Rot(0f, 360f, 0f);
            return k;
        }

        private static DragonClipKey[] Join(params object[] parts)
        {
            List<DragonClipKey> all = new List<DragonClipKey>();
            for (int i = 0; i < parts.Length; i++)
            {
                DragonClipKey one = parts[i] as DragonClipKey;
                if (one != null) { all.Add(one); continue; }
                DragonClipKey[] many = parts[i] as DragonClipKey[];
                if (many != null) all.AddRange(many);
            }
            return all.ToArray();
        }

        private static void BuildWarriorClips(Dictionary<string, DragonClipKey[]> c)
        {
            // Heavy Slash: weapon raised high over the right shoulder, crushing diagonal cut down to the front-left.
            DragonClipKey hsUp = K(-0.35f).Sp(-10f, -30f, 0f).Ch(-6f, -16f, 0f).Hd(-4f, 16f, 0f).RA(-160f, 0f, -30f).RF(-60f, 0f, 0f).LA(-60f, 10f, 20f).LF(-40f, 0f, 0f).Rot(-5f, 0f, 0f).Off(0f, -0.04f, 0f);
            c["warrior_heavy"] = new DragonClipKey[] {
                K(-1f), hsUp, hsUp.Copy(-0.08f).Sp(-12f, -34f, 0f),
                K(0f).Sp(28f, 28f, 0f).Ch(12f, 14f, 0f).Hd(6f, -8f, 0f).RA(-40f, 0f, 20f).RF(-5f, 0f, 0f).LA(10f, 0f, 30f).Rot(10f, 8f, 0f).Off(0f, -0.12f, 0.1f),
                K(0.3f).Sp(25f, 26f, 0f).Ch(10f, 12f, 0f).RA(-36f, 0f, 22f).LA(10f, 0f, 30f).Rot(9f, 8f, 0f).Off(0f, -0.11f, 0.1f),
                K(0.7f)
            };
            // Mercenary Ascended Heavy Slash: weapon wound far back at the waist, one huge horizontal sweep.
            DragonClipKey mhBack = K(-0.3f).Sp(10f, -48f, 0f).Ch(4f, -26f, 0f).Hd(0f, 30f, 0f).RA(-20f, -40f, -80f).RF(-15f, 0f, 0f).LA(-40f, 20f, 30f).LF(-30f, 0f, 0f).Off(0f, -0.12f, 0f);
            c["merc_heavy_asc"] = new DragonClipKey[] {
                K(-1f), mhBack, mhBack.Copy(-0.06f).Sp(12f, -52f, 0f),
                K(0f).Sp(14f, 48f, 0f).Ch(6f, 26f, 0f).Hd(0f, -20f, 0f).RA(-85f, 0f, 40f).RF(-5f, 0f, 0f).LA(10f, 0f, 35f).Rot(6f, 22f, 0f).Off(0f, -0.1f, 0.08f),
                K(0.35f).Sp(12f, 50f, 0f).Ch(6f, 26f, 0f).RA(-70f, 0f, 60f).LA(10f, 0f, 35f).Rot(5f, 24f, 0f).Off(0f, -0.09f, 0.08f),
                K(0.8f)
            };
            // Impact Wave: crouch with the blade dragged low behind, then a rising slash that throws the wave.
            DragonClipKey iwLow = K(-0.3f).Sp(22f, -24f, 0f).Ch(12f, -12f, 0f).RA(30f, 0f, -25f).RF(-20f, 0f, 0f).LA(-50f, 10f, 25f).LF(-40f, 0f, 0f).Rot(8f, 0f, 0f).Off(0f, -0.16f, 0f);
            c["warrior_impact_wave"] = new DragonClipKey[] {
                K(-1f), iwLow, iwLow.Copy(-0.06f),
                K(0f).Sp(-12f, 16f, 0f).Ch(-10f, 8f, 0f).Hd(-10f, 0f, 0f).RA(-155f, 0f, -12f).RF(-10f, 0f, 0f).LA(-20f, 0f, 30f).Rot(-6f, 0f, 0f).Off(0f, 0.05f, 0.06f),
                K(0.3f).Sp(-10f, 14f, 0f).Ch(-8f, 8f, 0f).RA(-150f, 0f, -12f).LA(-20f, 0f, 30f).Rot(-5f, 0f, 0f).Off(0f, 0.04f, 0.05f),
                K(0.7f)
            };
            // Impact Punch: fist chambered at the hip, guard up, then a full-body straight punch.
            DragonClipKey ipCh = K(-0.35f).Sp(4f, -30f, 0f).Ch(0f, -16f, 0f).Hd(0f, 18f, 0f).RA(20f, 0f, -15f).RF(-100f, 0f, 0f).LA(-65f, 15f, 15f).LF(-80f, 0f, 0f).Off(0f, -0.06f, -0.04f);
            c["warrior_punch"] = new DragonClipKey[] {
                K(-1f), ipCh, ipCh.Copy(-0.05f),
                K(0f).Sp(10f, 26f, 0f).Ch(6f, 16f, 0f).Hd(0f, -10f, 0f).RA(-88f, -5f, -5f).RF(-4f, 0f, 0f).LA(-30f, 10f, 20f).LF(-90f, 0f, 0f).Rot(7f, 8f, 0f).Off(0f, -0.04f, 0.16f),
                K(0.22f).Sp(10f, 24f, 0f).Ch(6f, 14f, 0f).RA(-86f, -5f, -5f).RF(-6f, 0f, 0f).LA(-30f, 10f, 20f).LF(-90f, 0f, 0f).Rot(7f, 8f, 0f).Off(0f, -0.04f, 0.15f),
                K(0.55f)
            };
            // Sword Master: ready stance (blade low at the right, crouched), then alternating cuts.
            c["sm_ready"] = new DragonClipKey[] {
                K(-1f),
                K(-0.4f).Sp(12f, -22f, 0f).Ch(6f, -10f, 0f).Hd(0f, 14f, 0f).RA(10f, 0f, -30f).RF(-30f, 0f, 0f).LA(-40f, 10f, 25f).LF(-50f, 0f, 0f).Rot(5f, 0f, 0f).Off(0f, -0.1f, 0f),
                K(0f).Sp(14f, -24f, 0f).Ch(6f, -12f, 0f).Hd(0f, 14f, 0f).RA(12f, 0f, -32f).RF(-30f, 0f, 0f).LA(-40f, 10f, 25f).LF(-50f, 0f, 0f).Rot(6f, 0f, 0f).Off(0f, -0.12f, 0f),
                K(0.25f)
            };
            DragonClipKey slashAEnd = K(0f).Sp(14f, 26f, 0f).Ch(8f, 14f, 0f).RA(-50f, 0f, 35f).RF(-6f, 0f, 0f).LA(-20f, 0f, 30f).Rot(6f, 12f, 0f).Off(0f, -0.06f, 0.06f);
            c["sm_slash_a"] = new DragonClipKey[] {
                K(-1f).Sp(-4f, -26f, 0f).Ch(-4f, -14f, 0f).RA(-125f, 0f, -50f).RF(-30f, 0f, 0f).LA(-30f, 0f, 30f),
                slashAEnd, slashAEnd.Copy(0.2f), K(0.5f)
            };
            DragonClipKey slashBEnd = K(0f).Sp(12f, -28f, 0f).Ch(8f, -14f, 0f).RA(-55f, 0f, -60f).RF(-6f, 0f, 0f).LA(-20f, 0f, 30f).Rot(6f, -12f, 0f).Off(0f, -0.06f, 0.06f);
            c["sm_slash_b"] = new DragonClipKey[] {
                K(-1f).Sp(-4f, 26f, 0f).Ch(-4f, 14f, 0f).RA(-130f, 0f, 30f).RF(-40f, 0f, 0f).LA(-30f, 0f, 30f),
                slashBEnd, slashBEnd.Copy(0.2f), K(0.5f)
            };
            // Ascended Moonlight finisher: two-handed blade raised to the sky, colossal vertical cut.
            DragonClipKey mfUp = K(-0.45f).Sp(-14f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-168f, 0f, -6f).RF(-25f, 0f, 0f).LA(-160f, 0f, 8f).LF(-30f, 0f, 0f).Off(0f, 0.04f, 0f);
            c["sm_moon_finisher"] = new DragonClipKey[] {
                K(-1f), mfUp, mfUp.Copy(-0.08f).Rot(-5f, 0f, 0f),
                K(0f).Sp(36f, 0f, 0f).Ch(16f, 0f, 0f).Hd(10f, 0f, 0f).RA(-30f, 0f, -4f).RF(-4f, 0f, 0f).LA(-30f, 0f, 6f).LF(-4f, 0f, 0f).Rot(12f, 0f, 0f).Off(0f, -0.18f, 0.08f),
                K(0.4f).Sp(32f, 0f, 0f).Ch(14f, 0f, 0f).RA(-28f, 0f, -4f).LA(-28f, 0f, 6f).Rot(11f, 0f, 0f).Off(0f, -0.16f, 0.08f),
                K(0.85f)
            };
            // Crescent Cleave: blade pulled back low at the right hip (twisted, crouched), wide sweep to the left.
            DragonClipKey ccPull = K(-0.35f).Sp(16f, -42f, 0f).Ch(6f, -22f, 0f).Hd(0f, 26f, 0f).RA(25f, -20f, -45f).RF(-20f, 0f, 0f).LA(-45f, 10f, 30f).LF(-40f, 0f, 0f).Off(0f, -0.13f, 0f);
            c["sm_crescent"] = new DragonClipKey[] {
                K(-1f), ccPull, ccPull.Copy(-0.06f).Sp(18f, -46f, 0f),
                K(0f).Sp(8f, 36f, 0f).Ch(4f, 20f, 0f).Hd(0f, -12f, 0f).RA(-85f, 0f, 35f).RF(-4f, 0f, 0f).LA(5f, 0f, 30f).Rot(5f, 15f, 0f).Off(0f, -0.06f, 0.06f),
                K(0.3f).Sp(8f, 34f, 0f).Ch(4f, 18f, 0f).RA(-75f, 0f, 45f).LA(5f, 0f, 30f).Rot(5f, 15f, 0f).Off(0f, -0.05f, 0.06f),
                K(0.7f)
            };
            // Blade Storm: hand on the hilt at the left hip, a lightning-fast draw, then the blade is sheathed.
            DragonClipKey bsDraw = K(-0.5f).Sp(10f, 18f, 0f).Ch(4f, 10f, 0f).RA(-40f, 10f, 30f).RF(-95f, 0f, 0f).LA(-20f, 0f, 20f).LF(-70f, 0f, 0f).Off(0f, -0.08f, 0f);
            DragonClipKey bsCut = K(0f).Sp(10f, -26f, 0f).Ch(6f, -14f, 0f).RA(-85f, 0f, -65f).RF(-4f, 0f, 0f).LA(-20f, 0f, 20f).Rot(4f, -10f, 0f).Off(0f, -0.08f, 0.05f);
            c["sm_blade_storm"] = new DragonClipKey[] { K(-1f), bsDraw, bsCut, bsCut.Copy(0.12f), bsDraw.Copy(0.32f), K(0.55f) };
            // Frenzied Charge: blade drawn back for a thrust (free hand guides), then the lunging dash.
            DragonClipKey fcBack = K(-0.4f).Sp(12f, -30f, 0f).Ch(6f, -16f, 0f).RA(35f, 0f, -20f).RF(-80f, 0f, 0f).LA(-75f, 10f, 15f).LF(-15f, 0f, 0f).Rot(8f, 0f, 0f).Off(0f, -0.1f, -0.05f);
            DragonClipKey fcThrust = K(0f).Sp(18f, 15f, 0f).Ch(8f, 8f, 0f).Hd(-8f, 0f, 0f).RA(-88f, 0f, -4f).RF(-5f, 0f, 0f).LA(15f, 0f, 30f).Rot(16f, 0f, 0f).Off(0f, -0.08f, 0.1f);
            c["sm_thrust"] = new DragonClipKey[] { K(-1f), fcBack, fcBack.Copy(-0.08f), fcThrust, fcThrust.Copy(0.4f), K(0.75f) };
            // Eclipse: blade raised upright before the face swelling with magic, then a full 360 spin slash.
            DragonClipKey ecUp = K(-0.5f).Ch(-6f, 0f, 0f).Hd(-6f, 0f, 0f).RA(-115f, 0f, -6f).RF(-45f, 0f, 0f).LA(-110f, 0f, 6f).LF(-45f, 0f, 0f).Off(0f, 0.03f, 0f);
            DragonClipKey ecOut = K(0f).Sp(10f, 0f, 0f).Ch(6f, 0f, 0f).RA(-90f, 0f, -70f).RF(-5f, 0f, 0f).LA(-60f, 0f, 60f).Rot(6f, 0f, 0f).Off(0f, -0.06f, 0f);
            c["sm_eclipse"] = Join(K(-1f), ecUp, Spin360(ecOut, 0f, 0.36f, 0.35f));
            // Halfmoon: long trembling pull (blade low behind, crouched), wide release; second slash = backhand.
            DragonClipKey hmPull = K(-0.75f).Sp(18f, -46f, 0f).Ch(8f, -24f, 0f).Hd(0f, 28f, 0f).RA(25f, -20f, -50f).RF(-20f, 0f, 0f).LA(-45f, 10f, 30f).LF(-40f, 0f, 0f).Rot(6f, 0f, 0f).Off(0f, -0.16f, 0f);
            DragonClipKey hmRelease = K(0f).Sp(8f, 40f, 0f).Ch(4f, 22f, 0f).Hd(0f, -14f, 0f).RA(-88f, 0f, 38f).RF(-4f, 0f, 0f).LA(5f, 0f, 30f).Rot(6f, 16f, 0f).Off(0f, -0.07f, 0.06f);
            c["sm_halfmoon"] = new DragonClipKey[] {
                K(-1f), hmPull,
                hmPull.Copy(-0.1f),
                hmRelease, hmRelease.Copy(0.45f)
            };
            DragonClipKey hm2 = K(0f).Sp(10f, -34f, 0f).Ch(6f, -18f, 0f).Hd(0f, 12f, 0f).RA(-80f, 0f, -60f).RF(-4f, 0f, 0f).LA(5f, 0f, 30f).Rot(6f, -14f, 0f).Off(0f, -0.07f, 0.06f);
            c["sm_halfmoon_2"] = new DragonClipKey[] { K(-1f), hm2, hm2.Copy(0.25f), K(0.65f) };
            // Ascended Halfmoon stance (hold until Left Click), then the spinning finisher.
            c["sm_halfmoon_stance"] = new DragonClipKey[] { K(-1f), hmPull.Copy(0f).Rot(6f, 0f, 0f), K(0.1f) };
            c["sm_halfmoon_finisher"] = Join(K(-1f), hmPull.Copy(-0.3f), Spin360(hmRelease.Copy(0f), 0f, 0.4f, 0.4f));
            // Knight's Guidance: blade raised before the face in a salute, then thrust to the sky.
            c["sm_guidance"] = new DragonClipKey[] {
                K(-1f),
                K(0f).Hd(4f, 0f, 0f).RA(-100f, 0f, 10f).RF(-85f, 0f, 0f).LA(-10f, 0f, 20f),
                K(0.2f).Sp(-6f, 0f, 0f).Ch(-6f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-172f, 0f, -4f).RF(-2f, 0f, 0f).LA(-30f, 0f, 40f).Off(0f, 0.05f, 0f),
                K(0.45f).Sp(-6f, 0f, 0f).Ch(-6f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-172f, 0f, -4f).LA(-30f, 0f, 40f).Off(0f, 0.05f, 0f),
                K(0.8f)
            };
            // Stomp: everything lifted, crash down, then two aftershock bounces 0.5s apart.
            DragonClipKey stDown = K(0f).Sp(26f, 0f, 0f).Ch(12f, 0f, 0f).Hd(8f, 0f, 0f).RA(-35f, 0f, -25f).LA(-35f, 0f, 25f).Rot(8f, 0f, 0f).Off(0f, -0.18f, 0f);
            c["merc_stomp"] = new DragonClipKey[] {
                K(-1f),
                K(-0.25f).Sp(-10f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-8f, 0f, 0f).RA(-130f, 0f, -25f).RF(-30f, 0f, 0f).LA(-130f, 0f, 25f).LF(-30f, 0f, 0f).Off(0f, 0.12f, 0f),
                stDown,
                stDown.Copy(0.3f).Off(0f, -0.1f, 0f), stDown.Copy(0.5f).Off(0f, -0.2f, 0f),
                stDown.Copy(0.8f).Off(0f, -0.1f, 0f), stDown.Copy(1.0f).Off(0f, -0.2f, 0f),
                K(1.45f)
            };
            // Circle Swing: weapon wound far back while walking (the wind up), then a full spin swing.
            DragonClipKey csWound = K(0f).Sp(8f, -50f, 0f).Ch(4f, -26f, 0f).Hd(0f, 30f, 0f).RA(-20f, -40f, -82f).RF(-20f, 0f, 0f).LA(-30f, 20f, 40f).LF(-30f, 0f, 0f).Off(0f, -0.08f, 0f);
            DragonClipKey csOut = K(0f).Sp(10f, 10f, 0f).Ch(4f, 6f, 0f).RA(-85f, 0f, -70f).RF(-5f, 0f, 0f).LA(-80f, 0f, 70f).Rot(5f, 0f, 0f).Off(0f, -0.08f, 0f);
            c["merc_circle"] = Join(K(-1f), csWound.Copy(-0.75f), csWound.Copy(-0.02f), Spin360(csOut, 0f, 0.35f, 0.35f));
            c["merc_circle_2"] = Join(K(-1f), csWound.Copy(-0.5f), Spin360(csOut, 0f, 0.35f, 0.4f));
            // Jump attacks (Bonecrusher; Electric Smite uses the same): weapon overhead in the air, crushing landing.
            c["air_overhead"] = new DragonClipKey[] {
                K(-1f),
                K(0f).Sp(-16f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-168f, 0f, -6f).RF(-40f, 0f, 0f).LA(-160f, 0f, 8f).LF(-40f, 0f, 0f).Rot(-8f, 0f, 0f),
                K(0.1f)
            };
            c["land_crash"] = c["cleric_land"];
            // Seismic Guillotine: overhead chop straight into the ground (the fissure starts at impact).
            c["merc_seismic"] = new DragonClipKey[] {
                K(-1f),
                K(-0.4f).Sp(-14f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-172f, 0f, -6f).RF(-30f, 0f, 0f).LA(-162f, 0f, 8f).LF(-30f, 0f, 0f).Rot(-6f, 0f, 0f).Off(0f, 0.06f, 0f),
                K(0f).Sp(42f, 0f, 0f).Ch(16f, 0f, 0f).Hd(12f, 0f, 0f).RA(-20f, 0f, -4f).RF(-4f, 0f, 0f).LA(-20f, 0f, 6f).LF(-4f, 0f, 0f).Rot(14f, 0f, 0f).Off(0f, -0.2f, 0.1f),
                K(0.35f).Sp(38f, 0f, 0f).Ch(14f, 0f, 0f).RA(-18f, 0f, -4f).LA(-18f, 0f, 6f).Rot(13f, 0f, 0f).Off(0f, -0.18f, 0.1f),
                K(0.8f)
            };
            // Punishing Bomb: two-handed bat cocked at the right shoulder, full swing through the bomb.
            DragonClipKey pbCock = K(-0.3f).Sp(4f, -44f, 0f).Ch(0f, -22f, 0f).Hd(0f, 28f, 0f).RA(-100f, -10f, -70f).RF(-90f, 0f, 0f).LA(-80f, 10f, -20f).LF(-90f, 0f, 0f).Off(0f, -0.06f, 0f);
            c["merc_bomb"] = new DragonClipKey[] {
                K(-1f), pbCock, pbCock.Copy(-0.05f),
                K(0f).Sp(8f, 46f, 0f).Ch(4f, 26f, 0f).Hd(0f, -10f, 0f).RA(-90f, 0f, 40f).RF(-10f, 0f, 0f).LA(-80f, 0f, 30f).LF(-30f, 0f, 0f).Rot(5f, 16f, 0f).Off(0f, -0.04f, 0.06f),
                K(0.25f).Sp(8f, 52f, 0f).Ch(4f, 30f, 0f).RA(-60f, 0f, 70f).LA(-60f, 0f, 50f).Rot(5f, 18f, 0f).Off(0f, -0.04f, 0.06f),
                K(0.65f)
            };
            // Battlecry / Unchained Fury: chest out, arms flexed wide, head thrown back, shaking roar.
            DragonClipKey roar = K(0f).Sp(-14f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-22f, 0f, 0f).RA(-40f, 0f, -60f).RF(-95f, 0f, 0f).LA(-40f, 0f, 60f).LF(-95f, 0f, 0f).Off(0f, -0.06f, 0f);
            c["merc_roar"] = new DragonClipKey[] {
                K(-1f),
                K(-0.5f).Sp(14f, 0f, 0f).Ch(8f, 0f, 0f).Hd(10f, 0f, 0f).RA(-30f, 0f, -30f).RF(-60f, 0f, 0f).LA(-30f, 0f, 30f).LF(-60f, 0f, 0f).Off(0f, -0.1f, 0f),
                roar, roar.Copy(0.55f),
                K(0.95f)
            };
        }

        // ------------------------------------------------------------------ Cleric / Paladin / Priest
        private static void BuildClericClips(Dictionary<string, DragonClipKey[]> c)
        {
            // Lightning Zap: draw the mace back to the shoulder, thrust it forward, the cone bursts.
            c["cleric_zap"] = new DragonClipKey[] {
                K(-1f),
                K(-0.45f).Sp(0f, -16f, 0f).Ch(-4f, -10f, 0f).RA(-40f, 0f, -20f).RF(-95f, 0f, 0f).LA(-20f, 0f, 18f),
                K(0f).Sp(6f, 12f, 0f).Ch(6f, 8f, 0f).RA(-88f, -8f, -6f).RF(-8f, 0f, 0f).RH(-10f, 0f, 0f).LA(10f, 0f, 22f).Rot(5f, 0f, 0f).Off(0f, 0f, 0.05f),
                K(0.16f).Sp(6f, 12f, 0f).Ch(6f, 8f, 0f).RA(-86f, -8f, -6f).RF(-10f, 0f, 0f).LA(10f, 0f, 22f).Rot(5f, 0f, 0f).Off(0f, 0f, 0.05f),
                K(0.45f)
            };
            // Righteous Strike: raise the mace to the sky (head up), hold trembling, snap it down at the target.
            DragonClipKey rsUp = K(-0.55f).Sp(-10f, -6f, 0f).Ch(-8f, 0f, 0f).Hd(-18f, 0f, 0f).RA(-155f, 0f, -12f).RF(-18f, 0f, 0f).LA(-25f, 0f, 24f).Off(0f, 0.04f, 0f);
            c["cleric_rs"] = new DragonClipKey[] {
                K(-1f), rsUp,
                rsUp.Copy(-0.12f).RA(-160f, 0f, -14f),
                K(0f).Sp(16f, 6f, 0f).Ch(10f, 0f, 0f).Hd(6f, 0f, 0f).RA(-78f, 0f, -6f).RF(-4f, 0f, 0f).LA(10f, 0f, 22f).Rot(7f, 0f, 0f).Off(0f, -0.06f, 0.04f),
                K(0.25f).Sp(14f, 6f, 0f).Ch(8f, 0f, 0f).RA(-76f, 0f, -6f).LA(10f, 0f, 22f).Rot(6f, 0f, 0f).Off(0f, -0.05f, 0.04f),
                K(0.6f)
            };
            // Ascended Righteous Strike: both hands on the mace overhead, leap-light crouch slam.
            DragonClipKey rsaUp = K(-0.5f).Sp(-14f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-20f, 0f, 0f).RA(-165f, 0f, -6f).RF(-30f, 0f, 0f).LA(-160f, 0f, 8f).LF(-35f, 0f, 0f).Off(0f, 0.08f, 0f);
            c["cleric_rs_asc"] = new DragonClipKey[] {
                K(-1f), rsaUp,
                rsaUp.Copy(-0.1f).Rot(-6f, 0f, 0f),
                K(0f).Sp(30f, 0f, 0f).Ch(16f, 0f, 0f).Hd(10f, 0f, 0f).RA(-50f, 0f, -4f).RF(-6f, 0f, 0f).LA(-48f, 0f, 6f).LF(-8f, 0f, 0f).Rot(12f, 0f, 0f).Off(0f, -0.16f, 0.08f),
                K(0.3f).Sp(28f, 0f, 0f).Ch(14f, 0f, 0f).RA(-46f, 0f, -4f).LA(-44f, 0f, 6f).Rot(11f, 0f, 0f).Off(0f, -0.15f, 0.08f),
                K(0.75f)
            };
            // Holy Wave: chant with the main hand raised, then sweep both arms open as the ring leaves.
            c["cleric_wave"] = new DragonClipKey[] {
                K(-1f),
                K(0f).Ch(-6f, 0f, 0f).Hd(-8f, 0f, 0f).RA(-100f, -10f, -10f).RF(-38f, 0f, 0f).RH(-20f, 0f, 0f).LA(-35f, 0f, 30f).LF(-30f, 0f, 0f).Off(0f, 0.03f, 0f),
                K(0.14f).Ch(-10f, 0f, 0f).Hd(-10f, 0f, 0f).RA(-70f, 0f, -55f).RF(-10f, 0f, 0f).LA(-70f, 0f, 55f).LF(-10f, 0f, 0f).Rot(-4f, 0f, 0f).Off(0f, 0.04f, 0f),
                K(0.32f).Ch(-8f, 0f, 0f).RA(-65f, 0f, -55f).LA(-65f, 0f, 55f).Rot(-3f, 0f, 0f),
                K(0.6f)
            };
            // Ascended Holy Wave (aimed at an ally): reach both hands toward them, a quick blessing push.
            c["cleric_wave_ally"] = new DragonClipKey[] {
                K(-1f),
                K(-0.4f).Ch(-6f, 0f, 0f).RA(-60f, -10f, -10f).RF(-60f, 0f, 0f).LA(-60f, 10f, 10f).LF(-60f, 0f, 0f),
                K(0f).Sp(6f, 0f, 0f).Ch(6f, 0f, 0f).RA(-88f, -14f, -8f).RF(-6f, 0f, 0f).RH(-20f, 0f, 0f).LA(-88f, 14f, 8f).LF(-6f, 0f, 0f).LH(-20f, 0f, 0f).Rot(4f, 0f, 0f).Off(0f, 0f, 0.05f),
                K(0.25f).Sp(6f, 0f, 0f).RA(-85f, -14f, -8f).LA(-85f, 14f, 8f).Rot(4f, 0f, 0f),
                K(0.6f)
            };
            // Goddess Relic: lift both hands to the sky calling the cross, then pull it down to the ground.
            DragonClipKey grUp = K(-0.45f).Sp(-12f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-22f, 0f, 0f).RA(-150f, 0f, -22f).RF(-12f, 0f, 0f).LA(-150f, 0f, 22f).LF(-12f, 0f, 0f).Off(0f, 0.05f, 0f);
            c["cleric_goddess"] = new DragonClipKey[] {
                K(-1f), grUp,
                grUp.Copy(-0.08f).Hd(-26f, 0f, 0f).Off(0f, 0.07f, 0f),
                K(0f).Sp(26f, 0f, 0f).Ch(14f, 0f, 0f).Hd(8f, 0f, 0f).RA(-45f, 0f, -18f).LA(-45f, 0f, 18f).Rot(8f, 0f, 0f).Off(0f, -0.12f, 0.04f),
                K(0.35f).Sp(22f, 0f, 0f).Ch(12f, 0f, 0f).RA(-40f, 0f, -16f).LA(-40f, 0f, 16f).Rot(7f, 0f, 0f).Off(0f, -0.10f, 0.04f),
                K(0.8f)
            };
            // Judgement Hammer: cock the hammer behind the head (shield arm aims), then hurl it.
            DragonClipKey jhBack = K(-0.25f).Sp(-12f, -26f, 0f).Ch(-8f, -16f, 0f).Hd(-6f, 18f, 0f).RA(-165f, -10f, -20f).RF(-75f, 0f, 0f).RH(20f, 0f, 0f).LA(-85f, 10f, 10f).LF(-10f, 0f, 0f).Rot(-5f, 0f, 0f);
            c["cleric_hammer"] = new DragonClipKey[] {
                K(-1f), jhBack,
                jhBack.Copy(-0.06f).Sp(-14f, -30f, 0f),
                K(0f).Sp(22f, 22f, 0f).Ch(12f, 14f, 0f).Hd(0f, -6f, 0f).RA(-78f, 0f, -4f).RF(-4f, 0f, 0f).LA(15f, 0f, 25f).Rot(9f, 0f, 0f).Off(0f, -0.04f, 0.08f),
                K(0.2f).Sp(24f, 26f, 0f).Ch(12f, 16f, 0f).RA(-35f, 0f, 10f).LA(15f, 0f, 25f).Rot(9f, 0f, 0f).Off(0f, -0.05f, 0.08f),
                K(0.6f)
            };
            // Shield Charge (hold): shield forward at chest height, mace drawn back, low forward lean;
            // impact = the Bash: shove the shield out and stomp in.
            c["cleric_charge"] = new DragonClipKey[] {
                K(-1f),
                K(0f).Sp(18f, 10f, 0f).Ch(10f, 8f, 0f).Hd(-12f, 0f, 0f).LA(-80f, 20f, 18f).LF(-75f, 0f, 0f).RA(25f, 0f, -20f).RF(-40f, 0f, 0f).Rot(14f, 0f, 0f).Off(0f, -0.06f, 0f),
                K(0.08f).Sp(24f, 16f, 0f).Ch(12f, 12f, 0f).Hd(-12f, 0f, 0f).LA(-92f, 10f, 10f).LF(-25f, 0f, 0f).RA(30f, 0f, -20f).RF(-40f, 0f, 0f).Rot(16f, 0f, 0f).Off(0f, -0.08f, 0.14f),
                K(0.3f).Sp(20f, 12f, 0f).Ch(10f, 10f, 0f).LA(-88f, 10f, 10f).LF(-25f, 0f, 0f).RA(20f, 0f, -20f).Rot(12f, 0f, 0f).Off(0f, -0.06f, 0.1f),
                K(0.6f)
            };
            // Angel Comet: arms swept back like wings on the rise (hold until the dive), arched back;
            // dive = arms forward overhead, body pitched head-first; landing clip handles the crash.
            c["cleric_angel_rise"] = new DragonClipKey[] {
                K(-1f),
                K(-0.5f).Sp(-14f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-24f, 0f, 0f).RA(30f, 0f, -70f).RF(-10f, 0f, 0f).LA(30f, 0f, 70f).LF(-10f, 0f, 0f).Rot(-10f, 0f, 0f),
                K(0f).Sp(-16f, 0f, 0f).Ch(-14f, 0f, 0f).Hd(-26f, 0f, 0f).RA(35f, 0f, -80f).RF(-8f, 0f, 0f).LA(35f, 0f, 80f).LF(-8f, 0f, 0f).Rot(-12f, 0f, 0f),
                K(0.15f).Sp(10f, 0f, 0f).Ch(8f, 0f, 0f).Hd(10f, 0f, 0f).RA(-160f, 0f, -10f).LA(-160f, 0f, 10f).Rot(55f, 0f, 0f),
                K(0.45f).Sp(14f, 0f, 0f).Ch(10f, 0f, 0f).Hd(12f, 0f, 0f).RA(-165f, 0f, -8f).LA(-165f, 0f, 8f).Rot(60f, 0f, 0f),
                K(3.5f).Sp(14f, 0f, 0f).Ch(10f, 0f, 0f).Hd(12f, 0f, 0f).RA(-165f, 0f, -8f).LA(-165f, 0f, 8f).Rot(60f, 0f, 0f)
            };
            // Heavy landing (Angel Comet / Electric Smite): crash into a deep crouch, mace in the ground.
            c["cleric_land"] = new DragonClipKey[] {
                K(-1f).Sp(14f, 0f, 0f).Ch(10f, 0f, 0f).RA(-120f, 0f, -10f).LA(-120f, 0f, 10f).Rot(30f, 0f, 0f),
                K(0f).Sp(34f, 0f, 0f).Ch(18f, 0f, 0f).Hd(12f, 0f, 0f).RA(-40f, 0f, -10f).RF(-4f, 0f, 0f).LA(-30f, 0f, 35f).Rot(12f, 0f, 0f).Off(0f, -0.2f, 0.06f),
                K(0.35f).Sp(30f, 0f, 0f).Ch(16f, 0f, 0f).Hd(8f, 0f, 0f).RA(-38f, 0f, -10f).LA(-28f, 0f, 35f).Rot(11f, 0f, 0f).Off(0f, -0.18f, 0.06f),
                K(0.8f)
            };
            // Electric Smite (hold while airborne): two-handed mace raised overhead, back arched.
            c["cleric_smite_air"] = new DragonClipKey[] {
                K(-1f),
                K(0f).Sp(-16f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-168f, 0f, -6f).RF(-40f, 0f, 0f).LA(-160f, 0f, 8f).LF(-40f, 0f, 0f).Rot(-8f, 0f, 0f),
                K(0.1f)
            };
            // Ray of Hope: both palms up to the sky, head lifted, slight rise.
            c["cleric_ray"] = new DragonClipKey[] {
                K(-1f),
                K(0f).Sp(-8f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-24f, 0f, 0f).RA(-130f, 0f, -30f).RF(-15f, 0f, 0f).RH(-25f, 0f, 0f).LA(-130f, 0f, 30f).LF(-15f, 0f, 0f).LH(-25f, 0f, 0f).Off(0f, 0.05f, 0f),
                K(0.3f).Sp(-8f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-24f, 0f, 0f).RA(-135f, 0f, -30f).LA(-135f, 0f, 30f).Off(0f, 0.06f, 0f),
                K(0.65f)
            };
            // Heaven's Light: thrust the mace straight up like a torch, shield arm out.
            c["cleric_light"] = new DragonClipKey[] {
                K(-1f),
                K(0f).Sp(-6f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-20f, 0f, 0f).RA(-172f, 0f, -4f).RF(-2f, 0f, 0f).LA(-40f, 0f, 45f).Off(0f, 0.06f, 0f),
                K(0.35f).Sp(-6f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-20f, 0f, 0f).RA(-174f, 0f, -4f).LA(-40f, 0f, 45f).Off(0f, 0.06f, 0f),
                K(0.7f)
            };
            // Relics: lift the relic high, then drive it down into the ground in a crouch.
            DragonClipKey relUp = K(-0.4f).Sp(-8f, 0f, 0f).Ch(-6f, 0f, 0f).Hd(-12f, 0f, 0f).RA(-140f, 0f, -10f).RF(-25f, 0f, 0f).LA(-30f, 0f, 30f).Off(0f, 0.04f, 0f);
            c["cleric_relic"] = new DragonClipKey[] {
                K(-1f), relUp,
                relUp.Copy(-0.1f).RA(-146f, 0f, -10f),
                K(0f).Sp(32f, 0f, 0f).Ch(16f, 0f, 0f).Hd(10f, 0f, 0f).RA(-30f, 0f, -6f).RF(-6f, 0f, 0f).LA(-20f, 0f, 30f).Rot(10f, 0f, 0f).Off(0f, -0.18f, 0.06f),
                K(0.35f).Sp(28f, 0f, 0f).Ch(14f, 0f, 0f).RA(-28f, 0f, -6f).LA(-20f, 0f, 30f).Rot(9f, 0f, 0f).Off(0f, -0.16f, 0.06f),
                K(0.8f)
            };
            // Holy Relic: same plant, the free hand opens in blessing as it lands.
            c["cleric_holy_relic"] = new DragonClipKey[] {
                K(-1f), relUp,
                relUp.Copy(-0.1f).LA(-60f, 0f, 30f).LH(-20f, 0f, 0f),
                K(0f).Sp(30f, 0f, 0f).Ch(14f, 0f, 0f).Hd(4f, 0f, 0f).RA(-30f, 0f, -6f).RF(-6f, 0f, 0f).LA(-95f, 0f, 45f).LH(-30f, 0f, 0f).Rot(9f, 0f, 0f).Off(0f, -0.16f, 0.06f),
                K(0.4f).Sp(26f, 0f, 0f).Ch(12f, 0f, 0f).RA(-28f, 0f, -6f).LA(-95f, 0f, 45f).Rot(8f, 0f, 0f).Off(0f, -0.14f, 0.06f),
                K(0.85f)
            };
            // Divine Intervention: hands drawn to the chest in prayer, head bowed, then arms flung open wide.
            DragonClipKey diPray = K(-0.35f).Sp(10f, 0f, 0f).Ch(6f, 0f, 0f).Hd(22f, 0f, 0f).RA(-45f, 20f, 18f).RF(-110f, 0f, 0f).LA(-45f, -20f, -18f).LF(-110f, 0f, 0f).Off(0f, -0.08f, 0f);
            c["cleric_intervention"] = new DragonClipKey[] {
                K(-1f), diPray,
                diPray.Copy(-0.06f).Off(0f, -0.1f, 0f),
                K(0f).Sp(-14f, 0f, 0f).Ch(-14f, 0f, 0f).Hd(-18f, 0f, 0f).RA(-100f, 0f, -65f).RF(-8f, 0f, 0f).LA(-100f, 0f, 65f).LF(-8f, 0f, 0f).Rot(-5f, 0f, 0f).Off(0f, 0.05f, 0f),
                K(0.4f).Sp(-12f, 0f, 0f).Ch(-12f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-98f, 0f, -65f).LA(-98f, 0f, 65f).Rot(-4f, 0f, 0f).Off(0f, 0.04f, 0f),
                K(0.8f)
            };
            // Grand Cross, two slashes forming an X: high right -> low left, then high left -> low right.
            c["cleric_cross_1"] = new DragonClipKey[] {
                K(-1f),
                K(-0.35f).Sp(-6f, -28f, 0f).Ch(-6f, -16f, 0f).RA(-150f, 0f, -45f).RF(-30f, 0f, 0f).LA(-20f, 0f, 25f),
                K(0f).Sp(18f, 24f, 0f).Ch(10f, 16f, 0f).RA(-40f, 0f, 25f).RF(-10f, 0f, 0f).LA(5f, 0f, 25f).Rot(6f, 15f, 0f).Off(0f, -0.05f, 0.05f),
                K(0.08f).Sp(16f, 26f, 0f).Ch(10f, 16f, 0f).RA(-40f, 0f, 30f).LA(5f, 0f, 25f).Rot(6f, 16f, 0f).Off(0f, -0.05f, 0.05f)
            };
            c["cleric_cross_2"] = new DragonClipKey[] {
                K(-1f).Sp(16f, 26f, 0f).Ch(10f, 16f, 0f).RA(-40f, 0f, 30f).LA(5f, 0f, 25f).Rot(6f, 16f, 0f).Off(0f, -0.05f, 0.05f),
                K(-0.4f).Sp(-6f, 24f, 0f).Ch(-6f, 14f, 0f).RA(-150f, 0f, 20f).RF(-40f, 0f, 0f).LA(-20f, 0f, 25f).Rot(0f, 10f, 0f),
                K(0f).Sp(20f, -24f, 0f).Ch(12f, -16f, 0f).RA(-45f, 0f, -55f).RF(-6f, 0f, 0f).LA(5f, 0f, 25f).Rot(8f, -12f, 0f).Off(0f, -0.06f, 0.06f),
                K(0.3f).Sp(18f, -22f, 0f).Ch(10f, -14f, 0f).RA(-42f, 0f, -55f).LA(5f, 0f, 25f).Rot(7f, -10f, 0f).Off(0f, -0.05f, 0.06f),
                K(0.65f)
            };
            // Heaven's Judgement: mace to the sky, free hand points at the circle, trembling hold; the barrage
            // starts as the mace swings down to point at it.
            DragonClipKey hjUp = K(-0.5f).Sp(-10f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-16f, 0f, 0f).RA(-170f, 0f, -8f).RF(-6f, 0f, 0f).LA(-88f, 8f, 10f).LF(-4f, 0f, 0f).LH(-10f, 0f, 0f);
            c["cleric_judgement"] = new DragonClipKey[] {
                K(-1f), hjUp,
                hjUp.Copy(-0.3f).RA(-174f, 0f, -10f).Off(0f, 0.02f, 0f),
                hjUp.Copy(-0.12f).RA(-170f, 0f, -6f),
                K(0f).Sp(14f, 0f, 0f).Ch(8f, 0f, 0f).Hd(4f, 0f, 0f).RA(-92f, 0f, -4f).RF(-2f, 0f, 0f).LA(-30f, 0f, 25f).Rot(6f, 0f, 0f).Off(0f, -0.04f, 0.04f),
                K(0.5f).Sp(12f, 0f, 0f).Ch(6f, 0f, 0f).RA(-90f, 0f, -4f).LA(-30f, 0f, 25f).Rot(5f, 0f, 0f),
                K(0.9f)
            };
            // Lightning Tempest: crouch gathering power with arms crossed low, then explode upward, arms wide to the sky.
            DragonClipKey tpLow = K(-0.4f).Sp(24f, 0f, 0f).Ch(14f, 0f, 0f).Hd(14f, 0f, 0f).RA(-40f, 20f, 30f).RF(-50f, 0f, 0f).LA(-40f, -20f, -30f).LF(-50f, 0f, 0f).Rot(6f, 0f, 0f).Off(0f, -0.16f, 0f);
            c["cleric_tempest"] = new DragonClipKey[] {
                K(-1f), tpLow,
                tpLow.Copy(-0.06f).Off(0f, -0.18f, 0f),
                K(0f).Sp(-20f, 0f, 0f).Ch(-16f, 0f, 0f).Hd(-28f, 0f, 0f).RA(-150f, 0f, -40f).RF(-6f, 0f, 0f).LA(-150f, 0f, 40f).LF(-6f, 0f, 0f).Rot(-8f, 0f, 0f).Off(0f, 0.1f, 0f),
                K(0.6f).Sp(-18f, 0f, 0f).Ch(-14f, 0f, 0f).Hd(-26f, 0f, 0f).RA(-152f, 0f, -40f).LA(-152f, 0f, 40f).Rot(-7f, 0f, 0f).Off(0f, 0.08f, 0f),
                K(1.0f)
            };
            // Heaven's Crucible: kneel-like bow in prayer, then rise lifting the shield high as the barrier forms.
            DragonClipKey crPray = K(-0.4f).Sp(22f, 0f, 0f).Ch(10f, 0f, 0f).Hd(24f, 0f, 0f).RA(-45f, 20f, 18f).RF(-110f, 0f, 0f).LA(-45f, -20f, -18f).LF(-110f, 0f, 0f).Rot(6f, 0f, 0f).Off(0f, -0.2f, 0f);
            c["cleric_crucible"] = new DragonClipKey[] {
                K(-1f), crPray,
                crPray.Copy(-0.08f),
                K(0f).Sp(-10f, 0f, 0f).Ch(-10f, 0f, 0f).Hd(-18f, 0f, 0f).LA(-165f, 0f, 10f).LF(-20f, 0f, 0f).RA(-60f, 0f, -45f).RF(-10f, 0f, 0f).Off(0f, 0.05f, 0f),
                K(0.45f).Sp(-8f, 0f, 0f).Ch(-8f, 0f, 0f).Hd(-16f, 0f, 0f).LA(-165f, 0f, 10f).RA(-58f, 0f, -45f).Off(0f, 0.04f, 0f),
                K(0.85f)
            };
        }

        public static void PlayBodyMotion(Player player, string preset, float duration)
        {
            if (player == null || player != Player.m_localPlayer || DragonCombatPlugin.Instance == null || string.IsNullOrEmpty(preset)) return;
            DragonCombatPlugin.Instance.StartCoroutine(BodyMotionRoutine(player, preset, Mathf.Max(0.1f, duration)));
        }

        private static Transform BodyVisual(Player player)
        {
            for (Type t = player.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField("m_visual", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) { GameObject g = f.GetValue(player) as GameObject; if (g != null) return g.transform; break; }
            }
            return player.transform.Find("Visual");
        }

        private static float Ease(float a, float b, float k) { return Mathf.Clamp01((k - a) / Mathf.Max(0.0001f, b - a)); }
        private static float Bump(float k) { return Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI); }

        // pitch (+ = forward), yaw, roll in degrees; y / z offsets in metres-ish (Visual local units).
        private static void MotionPose(string preset, float k, out Vector3 euler, out Vector3 offset)
        {
            euler = Vector3.zero; offset = Vector3.zero;
            float windup = Ease(0f, 0.35f, k), strike = Ease(0.35f, 0.55f, k), recover = Ease(0.55f, 1f, k);
            switch (preset)
            {
                case "slam":        // overhead smash: lean back, snap down into a crouch, recover
                    euler.x = -18f * windup * (1f - strike) + 32f * strike * (1f - recover);
                    offset.y = -0.30f * strike * (1f - recover);
                    break;
                case "sword_pull":  // pull the sword back low at the side (twist away), then let it go
                    {
                        float pull = Ease(0f, 0.45f, k), go = Ease(0.45f, 0.62f, k), back = Ease(0.62f, 1f, k);
                        euler.y = -40f * pull * (1f - go) + 25f * go * (1f - back);
                        euler.x = 12f * pull * (1f - go) + 8f * go * (1f - back);
                        offset.y = -0.22f * pull * (1f - back);
                    }
                    break;
                case "charge_release": // long charged pull (held, trembling with power), then a big release
                    {
                        float pull = Ease(0f, 0.25f, k), go = Ease(0.68f, 0.80f, k), back = Ease(0.80f, 1f, k);
                        float hold = pull * (1f - go);
                        euler.y = -50f * hold + 30f * go * (1f - back);
                        euler.x = 16f * hold + 12f * go * (1f - back);
                        offset.y = -0.32f * hold - 0.10f * go * (1f - back);
                    }
                    break;
                case "lunge":       // step-in thrust
                    euler.x = 22f * Bump(k);
                    offset.z = 0.35f * Bump(k);
                    offset.y = -0.10f * Bump(k);
                    break;
                case "spin":        // full horizontal spin slash
                    euler.y = 360f * Mathf.SmoothStep(0f, 1f, k);
                    euler.x = 10f * Bump(k);
                    break;
                case "double_spin":
                    euler.y = 720f * Mathf.SmoothStep(0f, 1f, k);
                    euler.x = 12f * Bump(k);
                    break;
                case "iai":         // low draw, then a fast upright cut with a body twist
                    euler.x = 18f * windup * (1f - strike);
                    offset.y = -0.25f * windup * (1f - strike);
                    euler.y = 35f * strike * (1f - recover);
                    euler.z = -8f * strike * (1f - recover);
                    break;
                case "cross":       // X-shaped double cut: roll left then right
                    euler.z = 22f * Mathf.Sin(k * Mathf.PI * 2f) * (1f - k * 0.3f);
                    euler.x = 10f * Bump(k);
                    break;
                case "cast":        // spell push: small lean back, thrust forward
                    euler.x = -8f * windup * (1f - strike) + 14f * strike * (1f - recover);
                    offset.z = 0.12f * strike * (1f - recover);
                    break;
                case "raise":       // chant to the sky, slight lift
                    euler.x = -16f * Bump(k);
                    offset.y = 0.08f * Bump(k);
                    break;
                case "grand":       // ultimate invocation: deep crouch, then rise and arch back
                    euler.x = 20f * windup * (1f - strike) - 24f * strike * (1f - recover);
                    offset.y = -0.35f * windup * (1f - strike) + 0.15f * strike * (1f - recover);
                    break;
                case "roar":        // war cry: arch back with a shake
                    euler.x = -20f * Bump(k);
                    break;
                case "plant":       // raise an object, then plant it in the ground
                    euler.x = -12f * windup * (1f - strike) + 26f * strike * (1f - recover);
                    offset.y = -0.22f * strike * (1f - recover);
                    break;
                case "throw":       // big wind-up throw
                    euler.x = -22f * windup * (1f - strike) + 24f * strike * (1f - recover);
                    euler.y = -25f * windup * (1f - strike) + 20f * strike * (1f - recover);
                    break;
                case "blink":       // quick lean into a dash
                    euler.x = 30f * Bump(k);
                    offset.y = -0.12f * Bump(k);
                    break;
                // v0.25.6 additional presets (one look per skill family)
                case "stomp":       // lift the body, drop hard (no tilt)
                    offset.y = 0.18f * windup * (1f - strike) - 0.28f * strike * (1f - recover);
                    euler.x = 6f * strike * (1f - recover);
                    break;
                case "leap_slam":   // jump up leaning back, crash down into a deep crouch
                    offset.y = 0.55f * windup * (1f - strike) - 0.35f * strike * (1f - recover);
                    euler.x = -20f * windup * (1f - strike) + 40f * strike * (1f - recover);
                    break;
                case "uppercut":    // dip low, then rise and arch back
                    offset.y = -0.25f * windup * (1f - strike) + 0.12f * strike * (1f - recover);
                    euler.x = 14f * windup * (1f - strike) - 26f * strike * (1f - recover);
                    euler.y = 20f * strike * (1f - recover);
                    break;
                case "punch":       // twist back, drive the shoulder forward
                    euler.y = -30f * windup * (1f - strike) + 25f * strike * (1f - recover);
                    euler.x = 18f * strike * (1f - recover);
                    offset.z = 0.30f * strike * (1f - recover);
                    break;
                case "bash":        // shield charge: low forward lean held, shoulder pop at the end
                    {
                        float lean = Ease(0f, 0.15f, k), pop = Ease(0.80f, 0.90f, k), back = Ease(0.90f, 1f, k);
                        euler.x = 24f * lean * (1f - back) + 10f * pop * (1f - back);
                        offset.y = -0.18f * lean * (1f - back);
                        offset.z = 0.25f * pop * (1f - back);
                    }
                    break;
                case "rush":        // frenzied charge: forward lean with a running wobble
                    {
                        float lean = Ease(0f, 0.1f, k) * (1f - Ease(0.9f, 1f, k));
                        euler.x = 28f * lean;
                        offset.y = -0.12f * lean;
                    }
                    break;
                case "dive":        // Angel Comet: spring up arched, then dive head-first into the ground
                    {
                        float up = Ease(0f, 0.45f, k), down = Ease(0.45f, 0.75f, k), back = Ease(0.75f, 1f, k);
                        euler.x = -25f * up * (1f - down) + 55f * down * (1f - back);
                        offset.y = 0.45f * up * (1f - down) - 0.30f * down * (1f - back);
                    }
                    break;
                case "kneel":       // bow down in prayer, rise slowly
                    euler.x = 24f * Bump(Ease(0f, 0.8f, k));
                    offset.y = -0.38f * Bump(Ease(0f, 0.8f, k));
                    break;
                case "pull":        // reach out, then drag everything in (lean back, crouch)
                    euler.x = 16f * windup * (1f - strike) - 22f * strike * (1f - recover);
                    offset.y = -0.18f * strike * (1f - recover);
                    offset.z = 0.15f * windup * (1f - strike) - 0.15f * strike * (1f - recover);
                    break;
                case "recoil":      // brace and aim, big kick-back on release
                    euler.x = 10f * windup * (1f - strike) - 20f * strike * (1f - recover);
                    offset.z = -0.35f * strike * (1f - recover);
                    offset.y = -0.12f * windup * (1f - recover);
                    break;
                case "push":        // two-handed shove of force
                    euler.x = -14f * windup * (1f - strike) + 20f * strike * (1f - recover);
                    offset.z = 0.22f * strike * (1f - recover);
                    break;
                case "erupt":       // crouch and slam the ground, then rise as it erupts
                    offset.y = -0.32f * windup * (1f - strike) + 0.20f * strike * (1f - recover);
                    euler.x = 26f * windup * (1f - strike) - 10f * strike * (1f - recover);
                    break;
                case "nova":        // curl in, then burst open
                    euler.x = 22f * windup * (1f - strike) - 18f * strike * (1f - recover);
                    offset.y = -0.28f * windup * (1f - strike) + 0.10f * strike * (1f - recover);
                    break;
                case "flick":       // quick half twist (flame / small burst)
                    euler.y = -35f * windup * (1f - strike) + 30f * strike * (1f - recover);
                    euler.x = 10f * strike * (1f - recover);
                    break;
                case "call_down":   // raise to the sky, then drag the sky down
                    euler.x = -24f * windup * (1f - strike) + 30f * strike * (1f - recover);
                    offset.y = 0.12f * windup * (1f - strike) - 0.20f * strike * (1f - recover);
                    break;
                case "rend":        // diagonal tear through space
                    euler.z = -18f * windup * (1f - strike) + 22f * strike * (1f - recover);
                    euler.y = -20f * windup * (1f - strike) + 30f * strike * (1f - recover);
                    euler.x = 10f * strike * (1f - recover);
                    break;
                case "flourish":    // half spin and back
                    euler.y = 180f * Bump(k);
                    euler.x = 8f * Bump(k);
                    break;
                case "eclipse":     // rise with a full spin, then a crushing downward cut
                    {
                        float spin = Ease(0f, 0.55f, k), cut = Ease(0.55f, 0.72f, k), back = Ease(0.72f, 1f, k);
                        euler.y = 360f * Mathf.SmoothStep(0f, 1f, spin);
                        offset.y = 0.35f * spin * (1f - cut) - 0.30f * cut * (1f - back);
                        euler.x = 36f * cut * (1f - back);
                    }
                    break;
            }
        }

        private static IEnumerator BodyMotionRoutine(Player player, string preset, float duration)
        {
            Transform v = BodyVisual(player);
            if (v == null) yield break;
            int token = ++_motionToken;
            if (!_motionActive) { _motionBaseRot = v.localRotation; _motionBasePos = v.localPosition; _motionActive = true; }
            Vector3 pivot = new Vector3(0f, 0.9f, 0f);
            float t = 0f;
            while (t < duration && player != null && !player.IsDead() && token == _motionToken && v != null)
            {
                Vector3 e, o;
                MotionPose(preset, t / duration, out e, out o);
                Quaternion r = Quaternion.Euler(e);
                v.localRotation = _motionBaseRot * r;
                v.localPosition = _motionBasePos + (pivot - r * pivot) + o;
                t += Time.deltaTime;
                yield return null;
            }
            if (token == _motionToken && v != null) { v.localRotation = _motionBaseRot; v.localPosition = _motionBasePos; _motionActive = false; }
        }

        // Preset + duration for every non-Ranger skill (Ranger animates its own skills).
        public static bool SkillMotion(string id, out string preset, out float duration)
        {
            preset = null; duration = 0.5f;
            switch (id)
            {
                // Warrior / Sword Master / Mercenary: keyframed clips (PlayClip) at the real wind-up / impact.
                // Cleric / Paladin / Priest: keyframed clips (PlayClip) at the real wind-up / impact.
                // Sorcerer / Archmage / Horizon Walker: keyframed clips (PlayClip).
            }
            return preset != null;
        }

        private static readonly Dictionary<string, float> HyperFirstHitLast = new Dictionary<string, float>();
        private static readonly Dictionary<string, bool> HyperFirstHitGrant = new Dictionary<string, bool>();

        // ==================================================================================
        // v0.24.3 BUFF INDICATORS: every Immortal Heroes buff on the local player is shown as a
        // display-only vanilla StatusEffect (icon + timer under the minimap, like Rested / Wet).
        // icon = Buff_<icon>.png in ImmortalHeroesAssets; stacks > 0 replaces the timer text.
        // ==================================================================================
        private static readonly Dictionary<string, Sprite> StatusSprites = new Dictionary<string, Sprite>();
        private static MethodInfo _seGet;

        public static void ShowStatus(Player player, string key, string icon, string label, float seconds, int stacks)
        {
            ShowStatus(player, key, icon, label, seconds, stacks, null);
        }

        public static void ShowStatus(Player player, string key, string icon, string label, float seconds, int stacks, string detail)
        {
            try
            {
                if (player == null || player != Player.m_localPlayer || string.IsNullOrEmpty(key)) return;
                object seman = player.GetSEMan();
                if (seman == null) return;
                string name = "IH_" + key;
                int hash = StableHash(name);
                if (_seGet == null) _seGet = seman.GetType().GetMethod("GetStatusEffect", new Type[] { typeof(int) });
                MethodInfo get = _seGet;
                IhStatusDisplay existing = get == null ? null : get.Invoke(seman, new object[] { hash }) as IhStatusDisplay;
                if (existing != null)
                {
                    existing.m_ttl = seconds;
                    SetStatusTime(existing, 0f);
                    existing.Stacks = stacks;
                    if (detail != null) existing.Detail = detail;
                    return;
                }
                // v0.25.2 perf: no re-add while the same buff is refreshed every frame.
                IhStatusDisplay se = ScriptableObject.CreateInstance<IhStatusDisplay>();
                se.name = name;
                se.m_name = label;
                se.m_tooltip = label;
                se.m_ttl = seconds;
                se.Stacks = stacks;
                se.Detail = detail;
                se.m_icon = StatusSprite(icon);
                MethodInfo[] methods = seman.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < methods.Length; i++)
                {
                    ParameterInfo[] ps = methods[i].GetParameters();
                    if (methods[i].Name != "AddStatusEffect" || ps.Length < 1 || ps[0].ParameterType != typeof(StatusEffect)) continue;
                    object[] args = new object[ps.Length];
                    args[0] = se;
                    for (int a = 1; a < ps.Length; a++)
                        args[a] = ps[a].HasDefaultValue ? ps[a].DefaultValue : (ps[a].ParameterType.IsValueType ? Activator.CreateInstance(ps[a].ParameterType) : null);
                    if (ps.Length > 1 && ps[1].ParameterType == typeof(bool)) args[1] = true;
                    methods[i].Invoke(seman, args);
                    return;
                }
            }
            catch { }
        }

        public static void ClearStatus(Player player, string key)
        {
            try
            {
                if (player == null || player != Player.m_localPlayer) return;
                object seman = player.GetSEMan();
                if (seman == null) return;
                int hash = StableHash("IH_" + key);
                MethodInfo[] methods = seman.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < methods.Length; i++)
                {
                    ParameterInfo[] ps = methods[i].GetParameters();
                    if (methods[i].Name != "RemoveStatusEffect" || ps.Length < 1 || ps[0].ParameterType != typeof(int)) continue;
                    object[] args = new object[ps.Length];
                    args[0] = hash;
                    for (int a = 1; a < ps.Length; a++) args[a] = ps[a].ParameterType == typeof(bool) ? (object)true : null;
                    methods[i].Invoke(seman, args);
                    return;
                }
            }
            catch { }
        }

        private static void SetStatusTime(StatusEffect se, float value)
        {
            FieldInfo f = typeof(StatusEffect).GetField("m_time", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(float)) f.SetValue(se, value);
        }

        // Same as Valheim's string.GetStableHashCode() (StatusEffect.NameHash uses it on the asset name).
        private static int StableHash(string str)
        {
            unchecked
            {
                int num = 5381;
                int num2 = num;
                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    num = ((num << 5) + num) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0') break;
                    num2 = ((num2 << 5) + num2) ^ str[i + 1];
                }
                return num + num2 * 1566083941;
            }
        }

        private static Sprite StatusSprite(string icon)
        {
            Sprite sprite;
            if (StatusSprites.TryGetValue(icon, out sprite)) return sprite;
            sprite = null;
            try
            {
                string path = Paths.PluginPath + "/ImmortalHeroesAssets/Buff_" + icon + ".png";
                Type fileType = typeof(object).Assembly.GetType("System.IO.File");
                MethodInfo exists = fileType.GetMethod("Exists", new Type[] { typeof(string) });
                if (!(bool)exists.Invoke(null, new object[] { path }))
                    path = Paths.PluginPath + "/ImmortalHeroesAssets/Buff_generic.png";
                MethodInfo read = fileType.GetMethod("ReadAllBytes", new Type[] { typeof(string) });
                byte[] bytes = (byte[])read.Invoke(null, new object[] { path });
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                MethodInfo load = null;
                Type conv = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                if (conv != null) load = conv.GetMethod("LoadImage", new Type[] { typeof(Texture2D), typeof(byte[]) });
                if (load != null) load.Invoke(null, new object[] { tex, bytes });
                tex.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            catch { sprite = null; }
            StatusSprites[icon] = sprite;
            return sprite;
        }

        // Icon + label for a timed buff source ("Acrobat.Tailwind" -> "Tailwind").
        private static void TimedBuffLook(string source, float damage, float speed, float move, float defense, float stamina, float eitr, bool hyper, out string icon, out string label)
        {
            string tail = source;
            int dot = tail.LastIndexOf('.');
            if (dot >= 0) tail = tail.Substring(dot + 1);
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            for (int i = 0; i < tail.Length; i++)
            {
                if (i > 0 && char.IsUpper(tail[i]) && !char.IsUpper(tail[i - 1])) b.Append(' ');
                b.Append(tail[i]);
            }
            label = b.ToString();
            if (defense > 0f) icon = "defense";
            else if (damage > 0f) icon = "damage";
            else if (move > 0f) icon = "haste";
            else if (speed > 0f) icon = "attack_speed";
            else if (eitr > 0f) icon = "eitr";
            else if (stamina > 0f) icon = "stamina";
            else if (hyper) icon = "hyper_armor";
            else icon = "generic";
            if (source.EndsWith("Overcharge")) icon = "overcharge";
            // v0.25.6: every named buff source has its own icon (no look-alikes for different effects).
            string own = SourceIcon(source);
            if (own != null) icon = own;
        }

        private static string SourceIcon(string source)
        {
            switch (source)
            {
                case "Paladin.HeavensLight": return "sun";
                case "Paladin.RayOfHope": return "ray";
                case "SwordMaster.KnightsGuidance": return "wing_boot";
                case "Mercenary.Battlecry": return "horn";
                case "Priest.HolyRelic": return "relic";
                case "Priest.DivineIntervention": return "wings";
                case "Priest.EyeOfTheStorm": return "storm";
                case "Priest.BucklerParry": return "parry";
                case "Priest.GrandSigilRecovery": return "heart";
                case "Acrobat.Tailwind": return "wind";
                case "Spellcaster.PhaseFlow": return "portal";
            }
            return null;
        }

        // v0.22.5 universal ruler: config meters -> world units. 1 m = (character height / 0.5),
        // i.e. two stacked characters. Height measured once from the standing local player (1.85 until then).
        private static float _measuredHeight;

        public static float UnitsPerMeter()
        {
            DragonCombatPlugin plugin = DragonCombatPlugin.Instance;
            if (plugin != null && plugin.UnitsPerMeterOverride != null && plugin.UnitsPerMeterOverride.Value > 0f)
                return plugin.UnitsPerMeterOverride.Value;
            float meters = plugin != null && plugin.CharacterHeightMeters != null ? Mathf.Max(0.05f, plugin.CharacterHeightMeters.Value) : 0.5f;
            if (_measuredHeight <= 0f)
            {
                Player player = Player.m_localPlayer;
                if (player != null)
                {
                    CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
                    if (capsule != null && capsule.height > 1f)
                        _measuredHeight = capsule.height * Mathf.Abs(player.transform.lossyScale.y);
                }
            }
            float height = _measuredHeight > 0f ? _measuredHeight : 1.85f;
            return height / meters;
        }

        public static float M(float meters)
        {
            return meters * UnitsPerMeter();
        }

        private const string ClassDataKey = "AlbedoCustomClasses.Class";
        private const string AdvancementDataKey = "AlbedoCustomClasses.Advancement";

        // v0.18.0: Immortal Heroes Tier power (+10% damage/healing per Tier). The Advanced module
        // registers the provider; skills in other modules ask for their multiplier by skill id.
        public static Func<Player, string, float> SkillPowerProvider;

        public static float GetSkillPower(Player player, string skillId)
        {
            if (SkillPowerProvider == null || player == null || string.IsNullOrEmpty(skillId))
                return 1f;
            try
            {
                return Mathf.Max(0f, SkillPowerProvider(player, skillId));
            }
            catch
            {
                return 1f;
            }
        }

        // v0.18.1: the Immortal Heroes Skill Tree hotbar owns skill input for this player
        // (Cleric -> Paladin first). Class skill modules skip their fixed hotkeys when true.
        public static Func<Player, bool> TreeHotbarProvider;

        public static bool IsTreeHotbarActive(Player player)
        {
            if (TreeHotbarProvider == null || player == null)
                return false;
            try
            {
                return TreeHotbarProvider(player);
            }
            catch
            {
                return false;
            }
        }

        // v0.23.5 universal stack counter: any module reports a skill's stacks / charges
        // (ready, max, seconds until the next one) and the one HUD draws them all the same way.
        public delegate bool SkillStackQuery(string skillId, out int ready, out int max, out float nextSeconds);
        private static readonly List<SkillStackQuery> StackQueries = new List<SkillStackQuery>();

        public static void RegisterStackQuery(SkillStackQuery query)
        {
            if (query != null && !StackQueries.Contains(query)) StackQueries.Add(query);
        }

        public static bool TryGetSkillStacks(string skillId, out int ready, out int max, out float nextSeconds)
        {
            ready = 0; max = 0; nextSeconds = 0f;
            for (int i = 0; i < StackQueries.Count; i++)
            {
                try { if (StackQueries[i](skillId, out ready, out max, out nextSeconds) && max > 1) return true; }
                catch { }
            }
            ready = 0; max = 0; nextSeconds = 0f;
            return false;
        }

        // v0.23.1: hold-to-charge skills ask whether the hotbar key of a skill is still held, and
        // modules without the progression data ask whether a skill is Ascended (Advanced registers both).
        public static Func<string, bool> TreeSkillKeyHeldProvider;
        public static Func<Player, string, bool> AscendedProvider;
        // v0.25.45: may this player's class chain the given weapon? (Advanced registers it; none = no chains)
        public static Func<Player, ItemDrop.ItemData, bool> ComboWeaponProvider;

        public static bool IsTreeSkillKeyHeld(string skillId)
        {
            if (TreeSkillKeyHeldProvider == null) return false;
            try { return TreeSkillKeyHeldProvider(skillId); }
            catch { return false; }
        }

        public static bool IsSkillAscended(Player player, string skillId)
        {
            if (AscendedProvider == null || player == null) return false;
            try { return AscendedProvider(player, skillId); }
            catch { return false; }
        }

        // v0.23.1 Freeze: Small/Big are stopped (Stun + 98% slow + Frost) for the duration;
        // Bosses are never frozen, only slowed by bossSlow.
        public static void Freeze(Character target, float seconds, float bossSlow)
        {
            if (IsDebuffImmune(target)) return;
            if (target == null || target.IsDead()) return;
            DragonCrippleController controller = target.GetComponent<DragonCrippleController>();
            if (controller == null) controller = target.gameObject.AddComponent<DragonCrippleController>();
            if (target.IsBoss())
            {
                controller.Apply(target, 1f - Mathf.Clamp01(bossSlow), Mathf.Max(0.1f, seconds));
                return;
            }
            Stun(target, target.transform.position - target.transform.forward);
            RunVfx(delegate { DragonVfx.Status(target, "freeze", seconds); });
            controller.Apply(target, 0.02f, Mathf.Max(0.1f, seconds));
            ApplyFrost(target, Mathf.Max(0.1f, seconds));
        }

        // v0.23.1 Clockwork (Wizard Grace): +Skill Damage (hits without a weapon skill = skill hits)
        // and a cooldown multiplier for every non-Grace skill that ENTERS cooldown while active.
        private static readonly Dictionary<int, float> ClockworkUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> ClockworkDamage = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> ClockworkCooldown = new Dictionary<int, float>();

        public static void GrantClockwork(Player player, float seconds, float skillDamageBonus, float cooldownMultiplier)
        {
            if (player == null) return;
            int id = player.GetInstanceID();
            ClockworkUntil[id] = Time.time + Mathf.Max(0.1f, seconds);
            ClockworkDamage[id] = Mathf.Max(0f, skillDamageBonus);
            ClockworkCooldown[id] = Mathf.Clamp(cooldownMultiplier, 0f, 1f);
            ShowStatus(player, "clockwork", "clockwork", "Clockwork", seconds, 0,
                "Attack Buff\n" + BuffPct(Mathf.Max(0f, skillDamageBonus), "Skill Damage") + "\nSkill cooldowns that start now: -" + Mathf.RoundToInt((1f - Mathf.Clamp01(cooldownMultiplier)) * 100f).ToString() + "%");
        }

        public static bool IsClockworkActive(Player player)
        {
            float until;
            return player != null && ClockworkUntil.TryGetValue(player.GetInstanceID(), out until) && Time.time < until;
        }

        // Called by every module when a cooldown starts: Graces are never shortened.
        public static float ScaleCooldown(Player player, string cooldownId, float seconds)
        {
            if (string.IsNullOrEmpty(cooldownId)) return seconds;
            string[] graces = { "KnightsGuidance", "Battlecry", "HeavensLight", "GrandSigil", "Clockwork", "RiftWalker", "TwinRift" };
            for (int i = 0; i < graces.Length; i++)
                if (cooldownId.IndexOf(graces[i], StringComparison.Ordinal) >= 0) return seconds;
            return seconds * CooldownMultiplier(player);
        }

        // Every module multiplies a NON-Grace cooldown by this when it starts.
        public static float CooldownMultiplier(Player player)
        {
            if (!IsClockworkActive(player)) return 1f;
            float value;
            return ClockworkCooldown.TryGetValue(player.GetInstanceID(), out value) ? value : 1f;
        }

        private static float ClockworkSkillDamage(Player player)
        {
            if (!IsClockworkActive(player)) return 0f;
            float value;
            return ClockworkDamage.TryGetValue(player.GetInstanceID(), out value) ? value : 0f;
        }

        // v0.22.0: skill modules that are not linked to Advanced (Sorcerer) register here so the
        // universal tree hotbar can cast them and show their cooldowns by skill id.
        private static readonly List<Func<Player, string, bool>> ExternalCasters = new List<Func<Player, string, bool>>();
        private static readonly List<Func<string, float>> ExternalCooldowns = new List<Func<string, float>>();

        // v0.24.1: input remap hook for skill modules (one module: Ranger).
        public delegate void ControlsFilter(Player player, ref bool attack, ref bool attackHold, ref bool block, ref bool blockHold);
        public static ControlsFilter ControlsHook;

        // v0.24.0: generic hooks for skill modules (Ranger). Skill level bonuses by Skills.SkillType
        // name (read by Advanced GetSkillBonus) and filters that see every hit before it lands.
        private static readonly List<Func<Player, string, float>> SkillLevelBonusProviders = new List<Func<Player, string, float>>();
        public static readonly List<Action<Character, HitData>> IncomingHitFilters = new List<Action<Character, HitData>>();

        public static void RegisterSkillLevelBonus(Func<Player, string, float> provider)
        {
            if (provider != null && !SkillLevelBonusProviders.Contains(provider)) SkillLevelBonusProviders.Add(provider);
        }

        public static float ExternalSkillLevelBonus(Player player, string skillType)
        {
            float total = 0f;
            for (int i = 0; i < SkillLevelBonusProviders.Count; i++)
            {
                try { total += SkillLevelBonusProviders[i](player, skillType); } catch { }
            }
            return total;
        }

        public static void RegisterIncomingHitFilter(Action<Character, HitData> filter)
        {
            if (filter != null && !IncomingHitFilters.Contains(filter)) IncomingHitFilters.Add(filter);
        }

        public static void RegisterSkillModule(Func<Player, string, bool> cast, Func<string, float> cooldown)
        {
            if (cast != null && !ExternalCasters.Contains(cast)) ExternalCasters.Add(cast);
            if (cooldown != null && !ExternalCooldowns.Contains(cooldown)) ExternalCooldowns.Add(cooldown);
        }

        public static bool TryExternalCast(Player player, string skillId)
        {
            for (int i = 0; i < ExternalCasters.Count; i++)
            {
                try { if (ExternalCasters[i](player, skillId)) return true; }
                catch { }
            }
            return false;
        }

        public static float ExternalCooldown(string skillId)
        {
            for (int i = 0; i < ExternalCooldowns.Count; i++)
            {
                try
                {
                    float value = ExternalCooldowns[i](skillId);
                    if (value > 0f) return value;
                }
                catch { }
            }
            return 0f;
        }

        // v0.20.9: Bless Thy Sinners Buckler Parry. Advanced (Priest skills) handles the effects.
        public static Action<Player> BucklerParryHandler;

        // Bucklers by name (vanilla + most mods), or a shield with a buckler-class parry bonus.
        public static bool IsBuckler(ItemDrop.ItemData item)
        {
            if (!IsShield(item) || IsTowerShield(item))
                return false;
            string name = item.m_shared.m_name == null ? "" : item.m_shared.m_name.ToLowerInvariant();
            string prefab = "";
            try
            {
                if (item.m_dropPrefab != null && item.m_dropPrefab.name != null)
                    prefab = item.m_dropPrefab.name.ToLowerInvariant();
            }
            catch
            {
            }
            return name.Contains("buckler") || prefab.Contains("buckler") || item.m_shared.m_timedBlockBonus >= 2.5f;
        }

        // Same test Valheim uses for a timed block: the block started less than 0.25s ago.
        public static bool IsInParryWindow(Humanoid humanoid)
        {
            try
            {
                FieldInfo field = typeof(Humanoid).GetField("m_blockTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                    return false;
                float timer = (float)field.GetValue(humanoid);
                return timer >= 0f && timer < 0.25f;
            }
            catch
            {
                return false;
            }
        }

        // v0.20.8: Holy Trinity condition, shared by CombatRuntime (movement) and Advanced (Clubs, damage).
        public static bool IsHolyTrinityActive(Player player)
        {
            if (player == null || GetAdvancementName(player) != "Paladin" || GetClassName(player) != "Cleric")
                return false;
            ItemDrop.ItemData right = GetHandItem(player, "m_rightItem");
            ItemDrop.ItemData left = GetHandItem(player, "m_leftItem");
            bool club = right != null && right.m_shared != null && right.m_shared.m_skillType == Skills.SkillType.Clubs && !IsMagicWeapon(right);
            return club && IsShield(left);
        }

        // v0.20.8: timed stamina-usage reduction (Bless Thy Sinners emergency window).
        private static readonly Dictionary<int, KeyValuePair<float, float>> StaminaUseCuts = new Dictionary<int, KeyValuePair<float, float>>();

        public static void ApplyStaminaUseCut(Player player, float fraction, float seconds)
        {
            if (player == null)
                return;
            StaminaUseCuts[player.GetInstanceID()] = new KeyValuePair<float, float>(Mathf.Clamp01(fraction), Time.time + Mathf.Max(0.1f, seconds));
        }

        public static float GetStaminaUseMultiplier(Player player)
        {
            KeyValuePair<float, float> cut;
            if (player == null || !StaminaUseCuts.TryGetValue(player.GetInstanceID(), out cut))
                return 1f;
            if (Time.time > cut.Value)
            {
                StaminaUseCuts.Remove(player.GetInstanceID());
                return 1f;
            }
            return 1f - cut.Key;
        }

        // v0.18.1: timed removal of equipment movement penalties (Heaven's Light Grace).
        private static readonly Dictionary<int, float> NoEquipmentPenaltyUntil = new Dictionary<int, float>();

        public static void GrantNoEquipmentPenalty(Player player, float seconds)
        {
            if (player == null)
                return;
            NoEquipmentPenaltyUntil[player.GetInstanceID()] = Time.time + Mathf.Max(0.1f, seconds);
        }

        public static bool HasNoEquipmentPenalty(Player player)
        {
            float until;
            return player != null && NoEquipmentPenaltyUntil.TryGetValue(player.GetInstanceID(), out until) && Time.time < until;
        }

        public static void SetUiInputBlocked(bool blocked)
        {
            try
            {
                Type cachedType = FindTypeCached("Jotunn.Managers.GUIManager");
                for (int i = 0; i < 1; i++)
                {
                    Type type = cachedType;
                    if (type == null)
                        continue;
                    MethodInfo method = type.GetMethod("BlockInput", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                        method.Invoke(null, new object[] { blocked });
                    return;
                }
            }
            catch
            {
            }
        }

        // v0.25.3 perf: this walks ~10 UI types by reflection; it was called on every IMGUI event of
        // every module. The answer is now computed once per frame.
        private static int _hudSuppressedFrame = -1;
        private static bool _hudSuppressedValue;

        // v0.25.8: the HUDs stay up (and draggable) while only the inventory is open.
        private static int _invOpenFrame = -1;
        private static bool _invOpenValue;
        public static bool IsInventoryOpen()
        {
            if (_invOpenFrame == Time.frameCount) return _invOpenValue;
            _invOpenValue = IsReflectedUiVisible("InventoryGui", new string[] { "IsVisible" }, new string[] { "m_inventoryRoot" });
            _invOpenFrame = Time.frameCount;
            return _invOpenValue;
        }

        public static bool IsGameplayHudSuppressed()
        {
            if (_hudSuppressedFrame == Time.frameCount) return _hudSuppressedValue;
            _hudSuppressedValue = IsGameplayHudSuppressedUncached();
            _hudSuppressedFrame = Time.frameCount;
            return _hudSuppressedValue;
        }

        private static bool IsGameplayHudSuppressedUncached()
        {
            // IMPORTANT: if a UI exposes a real IsVisible/IsOpen method, that
            // result is authoritative. Do not fall through to a broad root such
            // as InventoryGui.m_root, because several Valheim/modded UI roots
            // stay active even while the actual window is closed. That mistake
            // made the combat skill HUD disappear permanently in v0.11.0/0.11.1.
            if (IsReflectedUiVisible("InventoryGui", new string[] { "IsVisible" }, new string[] { "m_inventoryRoot" }))
                return true;
            if (IsReflectedUiVisible("Menu", new string[] { "IsVisible" }, new string[] { "m_root" }))
                return true;
            if (IsReflectedUiVisible("Minimap", new string[] { "IsOpen" }, new string[] { "m_largeRoot" }))
                return true;
            if (IsReflectedUiVisible("StoreGui", new string[] { "IsVisible" }, new string[] { "m_root" }))
                return true;
            if (IsReflectedUiVisible("TextViewer", new string[] { "IsVisible" }, new string[] { "m_root" }))
                return true;
            if (IsReflectedUiVisible("BarberGui", new string[] { "IsVisible" }, new string[] { "m_root" }))
                return true;
            if (IsReflectedUiVisible("Console", new string[] { "IsVisible" }, new string[0]))
                return true;
            if (IsHudPieceSelectionVisible())
                return true;

            if (IsCustomUiOpen("AlbedosCustomClasses.Plugin", "IsClassPanelOpen"))
                return true;
            if (IsPrivateBoolFieldTrue("AlbedosCustomClassesAdvanced.AdvancedPlugin", "Instance", "_skillbookOpen"))
                return true;
            if (IsCustomUiOpen("DragonsAltarDevTools.DeveloperToolsPlugin", "IsDeveloperPanelOpen"))
                return true;

            return false;
        }

        private static bool IsReflectedUiVisible(string typeName, string[] methodNames, string[] rootFields)
        {
            try
            {
                Type type = FindTypeCached(typeName);
                if (type == null)
                    return false;

                object instance = null;
                FieldInfo instanceField = type.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (instanceField == null)
                    instanceField = type.GetField("m_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (instanceField != null)
                    instance = instanceField.GetValue(null);

                if (instance == null)
                {
                    PropertyInfo instanceProperty = type.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (instanceProperty == null)
                        instanceProperty = type.GetProperty("m_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (instanceProperty != null)
                        instance = instanceProperty.GetValue(null, null);
                }

                bool visibilityMethodResolved = false;
                for (int i = 0; i < methodNames.Length; i++)
                {
                    MethodInfo method = type.GetMethod(methodNames[i], BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (method != null && method.ReturnType == typeof(bool))
                    {
                        visibilityMethodResolved = true;
                        object target = method.IsStatic ? null : instance;
                        if (method.IsStatic || target != null)
                        {
                            object result = method.Invoke(target, null);
                            if (result is bool && (bool)result)
                                return true;
                        }
                    }

                    PropertyInfo property = type.GetProperty(methodNames[i], BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null && property.PropertyType == typeof(bool))
                    {
                        MethodInfo getter = property.GetGetMethod(true);
                        if (getter != null)
                        {
                            visibilityMethodResolved = true;
                            object target = getter.IsStatic ? null : instance;
                            if (getter.IsStatic || target != null)
                            {
                                object result = property.GetValue(target, null);
                                if (result is bool && (bool)result)
                                    return true;
                            }
                        }
                    }
                }

                // If a real visibility method/property exists and reported false, trust it.
                if (visibilityMethodResolved)
                    return false;

                if (instance == null)
                    return false;

                for (int i = 0; i < rootFields.Length; i++)
                {
                    FieldInfo field = type.GetField(rootFields[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null)
                        continue;
                    GameObject root = field.GetValue(instance) as GameObject;
                    if (root != null && root.activeInHierarchy)
                        return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private static bool IsHudPieceSelectionVisible()
        {
            try
            {
                Type type = FindTypeCached("Hud");
                if (type == null)
                    return false;

                object instance = null;
                FieldInfo instanceField = type.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (instanceField != null)
                    instance = instanceField.GetValue(null);
                if (instance == null)
                    return false;

                MethodInfo visible = type.GetMethod("IsPieceSelectionVisible", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (visible != null && visible.ReturnType == typeof(bool))
                {
                    object result = visible.Invoke(instance, null);
                    if (result is bool)
                        return (bool)result;
                }

                // Older/future builds may not expose IsPieceSelectionVisible. In
                // that case only inspect the actual selection window, never the
                // broad m_buildHud root which can stay active while merely holding
                // a Hammer and would hide our combat HUD during normal gameplay.
                FieldInfo field = type.GetField("m_pieceSelectionWindow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    GameObject root = field.GetValue(instance) as GameObject;
                    if (root != null && root.activeInHierarchy)
                        return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private static bool IsCustomUiOpen(string typeName, string propertyName)
        {
            try
            {
                Type cachedType = FindTypeCached(typeName);
                for (int i = 0; i < 1; i++)
                {
                    Type type = cachedType;
                    if (type == null)
                        continue;
                    PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property == null || property.PropertyType != typeof(bool))
                        return false;
                    object value = property.GetValue(null, null);
                    return value is bool && (bool)value;
                }
            }
            catch
            {
            }
            return false;
        }

        private static bool IsPrivateBoolFieldTrue(string typeName, string instanceFieldName, string boolFieldName)
        {
            try
            {
                Type cachedType = FindTypeCached(typeName);
                for (int i = 0; i < 1; i++)
                {
                    Type type = cachedType;
                    if (type == null)
                        continue;
                    FieldInfo instanceField = type.GetField(instanceFieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    object instance = instanceField == null ? null : instanceField.GetValue(null);
                    if (instance == null)
                        return false;
                    FieldInfo boolField = type.GetField(boolFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (boolField == null)
                        return false;
                    object value = boolField.GetValue(instance);
                    return value is bool && (bool)value;
                }
            }
            catch
            {
            }
            return false;
        }

        private static readonly Dictionary<int, float> SkillLockUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> ExplicitHyperArmorUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> HitHyperArmorUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> AttackSpeedSource = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> SkillAnimSpeed = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> SkillAnimSpeedUntil = new Dictionary<int, float>();

        // v0.25.30 skill animation timing: overrides the animator speed factor while a skill's vanilla animation plays.
        public static void SetSkillAnimSpeed(Player player, float multiplier, float ttl)
        {
            if (player == null) return;
            int id = player.GetInstanceID();
            SkillAnimSpeed[id] = Mathf.Clamp(multiplier, 0.01f, 6f);
            SkillAnimSpeedUntil[id] = Time.time + Mathf.Max(0f, ttl);
        }
        private static readonly Dictionary<int, float> AttackSpeedSourceUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> StaminaRegenBlockedUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> EitrRegenBlockedUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, Dictionary<string, TimedBuffState>> TimedBuffs = new Dictionary<int, Dictionary<string, TimedBuffState>>();
        private static readonly Dictionary<int, DebuffState> Debuffs = new Dictionary<int, DebuffState>();
        private static readonly Dictionary<int, MasteryComboState> MasteryCombos = new Dictionary<int, MasteryComboState>();
        private static readonly Dictionary<int, float> MasteryHeavyUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> WhirlwindUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> MobileCastUntil = new Dictionary<int, float>();
        private static readonly Dictionary<int, bool> MobileCastAllowSprint = new Dictionary<int, bool>();
        private static readonly Dictionary<int, AnimatorSpeedRuntimeState> AnimatorSpeedStates = new Dictionary<int, AnimatorSpeedRuntimeState>();
        private static readonly Dictionary<int, AnimatorSpeedRuntimeState> FrostAnimatorSpeedStates = new Dictionary<int, AnimatorSpeedRuntimeState>();

        public static void BeginMobileCast(Player player, float duration, bool allowSprint)
        {
            if (player == null)
                return;

            int id = player.GetInstanceID();
            MobileCastUntil[id] = Time.time + Mathf.Max(0.05f, duration);
            MobileCastAllowSprint[id] = allowSprint;
        }

        public static void EndMobileCast(Player player)
        {
            if (player == null)
                return;

            int id = player.GetInstanceID();
            MobileCastUntil.Remove(id);
            MobileCastAllowSprint.Remove(id);
        }

        public static bool IsMobileCastActive(Player player, out bool allowSprint)
        {
            allowSprint = false;

            if (player == null)
                return false;

            int id = player.GetInstanceID();
            float end;
            if (!MobileCastUntil.TryGetValue(id, out end) || Time.time >= end)
                return false;

            MobileCastAllowSprint.TryGetValue(id, out allowSprint);
            return true;
        }

        public static void BeginWhirlwind(Player player, float duration)
        {
            if (player == null)
                return;

            WhirlwindUntil[player.GetInstanceID()] = Time.time + Mathf.Max(0.1f, duration);
        }

        public static bool IsWhirlwindActive(Player player)
        {
            if (player == null)
                return false;

            float end;
            return WhirlwindUntil.TryGetValue(player.GetInstanceID(), out end) && Time.time < end;
        }

        public static void LockSkill(Player player, float duration)
        {
            if (player == null)
                return;
            // v0.25.86 (user): a Horizon Walker casts EVERY skill on the move and never stops (momentum, sprint kept).
            if (duration > 0f && GetAdvancementName(player) == "Spellcaster")
            {
                BeginMobileCast(player, duration, true);
                return;
            }
            SkillLockUntil[player.GetInstanceID()] = Time.time + Mathf.Max(0f, duration);
        }

        // v0.25.86 Horizon Walker casting while moving: the casting hand rises forward (staff up), upper body only,
        // legs keep running.
        private static bool HorizonWalkerMoving(Player player)
        {
            if (player == null || GetAdvancementName(player) != "Spellcaster") return false;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null) return false;
            Vector3 v = rb.velocity; v.y = 0f;
            return v.magnitude > 1.2f;
        }

        private static DragonClipKey[] MovingCastClip()
        {
            DragonClipKey up = K(0f).Hand(0.3f, 0.75f, 0.6f, 0.95f).Hd(-6f, 0f, 0f);
            DragonClipKey[] k = new DragonClipKey[] { K(-1f), up.Copy(-0.45f), up, up.Copy(0.22f), K(0.55f) };
            k[0].NoPlant = true;
            return k;
        }

        public static bool IsSkillLocked(Player player)
        {
            if (player == null)
                return false;
            float end;
            return SkillLockUntil.TryGetValue(player.GetInstanceID(), out end) && Time.time < end;
        }

        public static float ScaleWindup(Player player, float seconds)
        {
            float factor = GetAttackSpeedMultiplier(player);
            if (factor < 0.05f)
                factor = 1f;
            return Mathf.Max(0f, seconds) / factor;
        }

        public static void GrantHyperArmor(Player player, float duration)
        {
            if (player == null)
                return;
            ExplicitHyperArmorUntil[player.GetInstanceID()] = Time.time + Mathf.Max(0f, duration);
            if (duration >= 1f) RunVfx(delegate { DragonVfx.HyperShimmer(player, duration); });
            if (duration >= 1f) ShowStatus(player, "hyper_armor", "hyper_armor", "Hyper Armor", duration, 0, "Defense Buff\nNo stagger or knockback from hits");
        }

        public static float BlockHyperUntil = -1f;

        public static bool HasHyperArmor(Character character)
        {
            Player player = character as Player;
            if (player == null)
                return false;

            int id = player.GetInstanceID();
            float end;
            if (ExplicitHyperArmorUntil.TryGetValue(id, out end) && Time.time < end)
                return true;
            if (HitHyperArmorUntil.TryGetValue(id, out end) && Time.time < end)
                return true;

            Dictionary<string, TimedBuffState> buffs;
            if (TimedBuffs.TryGetValue(id, out buffs))
            {
                foreach (KeyValuePair<string, TimedBuffState> pair in buffs)
                {
                    TimedBuffState buff = pair.Value;
                    if (buff != null && Time.time < buff.EndTime && buff.HyperArmor)
                        return true;
                }
            }
            return false;
        }

        public static void ApplyManagedAnimationSpeed(Player player, Animator animator)
        {
            if (player == null || animator == null)
                return;

            int id = animator.GetInstanceID();
            AnimatorSpeedRuntimeState state;

            if (!AnimatorSpeedStates.TryGetValue(id, out state))
            {
                state = new AnimatorSpeedRuntimeState();
                AnimatorSpeedStates[id] = state;
            }

            float current = animator.speed;

            // Valheim owns hit-stop. Never touch zero/near-zero speed.
            // Also forget our last multiplier so recovery starts from fresh vanilla state.
            // v0.25.42: unless the near-zero speed is our own skill freeze (Frenzied Charge pose hold).
            bool ownFreeze = state.HasOutput && state.LastFactor < 0.05f && Mathf.Abs(current - state.LastOutputSpeed) <= 0.005f;
            if (current <= 0.05f && !ownFreeze)
            {
                state.HasOutput = false;
                state.LastOutputSpeed = current;
                state.LastFactor = 1f;
                return;
            }

            float baseSpeed = current;

            // If the animator still carries our exact previous output, strip our
            // old factor first. If vanilla changed the speed, accept it as fresh base.
            if (state.HasOutput &&
                state.LastFactor > 0.001f &&
                Mathf.Abs(current - state.LastOutputSpeed) <= (state.LastFactor < 0.05f ? 0.005f : 0.01f))
            {
                baseSpeed = current / state.LastFactor;
            }

            float factor = 1f;

            if (player.InAttack())
                factor = GetAttackSpeedMultiplier(player) * ChainSpeedFor(player);
            float skillSpeed, skillUntil;
            int pid = player.GetInstanceID();
            bool skillTimed = SkillAnimSpeed.TryGetValue(pid, out skillSpeed) && SkillAnimSpeedUntil.TryGetValue(pid, out skillUntil) && Time.time < skillUntil;
            if (skillTimed) factor = skillSpeed;

            factor = Mathf.Max(skillTimed ? 0.01f : 0.1f, factor);

            float output = Mathf.Clamp(baseSpeed * factor, skillTimed ? 0.01f : 0.05f, skillTimed ? 6f : 5f);

            // v0.25.63 chain clock: natural time = real time x the factor WE actually applied (whatever Valheim's own
            // base speed is), integrated per call - the chain timer no longer assumes one constant factor per swing.
            if (player == Player.m_localPlayer)
            {
                float nowT = Time.time;
                float dtc = Mathf.Clamp(nowT - _chainClockLast, 0f, 0.1f);
                ChainNaturalClock += dtc * (baseSpeed > 0.05f ? output / baseSpeed : 1f);
                _chainClockLast = nowT;
            }

            animator.speed = output;
            state.HasOutput = true;
            state.LastFactor = factor;
            state.LastOutputSpeed = output;
        }

        public static float ChainNaturalClock;
        private static float _chainClockLast;
        private static readonly Dictionary<int, float> ChainSpeed = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> ChainSpeedUntil = new Dictionary<int, float>();

        public static void SetChainSpeed(Player player, float multiplier, float ttl)
        {
            if (player == null) return;
            int id = player.GetInstanceID();
            ChainSpeed[id] = Mathf.Clamp(multiplier, 0.2f, 4f);
            ChainSpeedUntil[id] = Time.time + Mathf.Max(0.05f, ttl);
        }

        public static float ChainSpeedFor(Player player)
        {
            if (player == null) return 1f;
            int id = player.GetInstanceID();
            float v, until;
            if (ChainSpeed.TryGetValue(id, out v) && ChainSpeedUntil.TryGetValue(id, out until) && Time.time < until) return v;
            return 1f;
        }

        public static void SetAttackSpeedSource(Player player, float multiplier, float ttl)
        {
            if (player == null)
                return;
            int id = player.GetInstanceID();
            AttackSpeedSource[id] = Mathf.Max(0.1f, multiplier);
            AttackSpeedSourceUntil[id] = Time.time + Mathf.Max(0.05f, ttl);
        }

        public static float GetAttackSpeedMultiplier(Player player)
        {
            if (player == null)
                return 1f;

            int id = player.GetInstanceID();
            float factor = 1f;
            float source;
            float sourceUntil;
            if (AttackSpeedSource.TryGetValue(id, out source) && AttackSpeedSourceUntil.TryGetValue(id, out sourceUntil) && Time.time < sourceUntil)
                factor *= Mathf.Max(0.1f, source);

            float buffBonus = GetTimedBuffSum(player, "AttackSpeed");
            factor *= Mathf.Max(0.1f, 1f + buffBonus);

            // v0.25.66 (user): the old Weapon Mastery combo speed-up per chain stage (it burst at the "Finisher")
            // and the post-heavy speed bonus are gone - only real attack speed sources remain.

            return Mathf.Max(0.1f, factor);
        }

        // v0.25.34 buff tooltip text: type (biggest share) + every non-zero bonus as its own line.
        public static string BuffPct(float fraction, string what)
        {
            int p = Mathf.RoundToInt(fraction * 100f);
            return (p >= 0 ? "+" : "") + p.ToString() + "% " + what;
        }

        private static string TimedBuffDetail(float damage, float speed, float move, float defense, float stamina, float eitr, bool hyper)
        {
            string type = "Buff";
            float atk = Mathf.Abs(damage) + Mathf.Abs(speed), def = Mathf.Abs(defense) + (hyper ? 0.01f : 0f), mob = Mathf.Abs(move), sus = Mathf.Abs(stamina) + Mathf.Abs(eitr);
            float best = Mathf.Max(Mathf.Max(atk, def), Mathf.Max(mob, sus));
            if (best > 0f) type = best == atk ? "Attack Buff" : best == def ? "Defense Buff" : best == mob ? "Movement Buff" : "Recovery Buff";
            System.Text.StringBuilder b = new System.Text.StringBuilder(type);
            if (Mathf.Abs(damage) > 0.0001f) b.Append("\n").Append(BuffPct(damage, "Attack Damage"));
            if (Mathf.Abs(speed) > 0.0001f) b.Append("\n").Append(BuffPct(speed, "Attack Speed"));
            if (Mathf.Abs(defense) > 0.0001f) b.Append("\n").Append(BuffPct(defense, "Defense"));
            if (Mathf.Abs(move) > 0.0001f) b.Append("\n").Append(BuffPct(move, "Movement Speed"));
            if (Mathf.Abs(stamina) > 0.0001f) b.Append("\n").Append(BuffPct(stamina, "Stamina Regen"));
            if (Mathf.Abs(eitr) > 0.0001f) b.Append("\n").Append(BuffPct(eitr, "Eitr Regen"));
            if (hyper) b.Append("\nHyper Armor (no stagger or knockback)");
            return b.ToString();
        }

        public static void ApplyTimedBuff(Player player, float duration, float attackDamageBonus, float attackSpeedBonus, float moveSpeedBonus, float defenseBonus, float staminaRegenBonus, float eitrRegenBonus, bool hyperArmor)
        {
            ApplyTimedBuff(player, "Generic", duration, attackDamageBonus, attackSpeedBonus, moveSpeedBonus, defenseBonus, staminaRegenBonus, eitrRegenBonus, hyperArmor);
        }

        public static void ApplyTimedBuff(Player player, string source, float duration, float attackDamageBonus, float attackSpeedBonus, float moveSpeedBonus, float defenseBonus, float staminaRegenBonus, float eitrRegenBonus, bool hyperArmor)
        {
            if (player == null)
                return;

            if (string.IsNullOrEmpty(source))
                source = "Generic";

            string statusIcon, statusLabel;
            TimedBuffLook(source, attackDamageBonus, attackSpeedBonus, moveSpeedBonus, defenseBonus, staminaRegenBonus, eitrRegenBonus, hyperArmor, out statusIcon, out statusLabel);
            if (duration >= 1f)
            {
                string det = TimedBuffDetail(attackDamageBonus, attackSpeedBonus, moveSpeedBonus, defenseBonus, staminaRegenBonus, eitrRegenBonus, hyperArmor);
                string note;
                if (BuffNotes.TryGetValue(source, out note) && !string.IsNullOrEmpty(note)) det += "\n" + note;
                ShowStatus(player, "buff_" + source, statusIcon, statusLabel, duration, 0, det);
            }

            TimedBuffState state = new TimedBuffState();
            state.EndTime = Time.time + Mathf.Max(0.1f, duration);
            state.AttackDamageBonus = attackDamageBonus;
            state.AttackSpeedBonus = attackSpeedBonus;
            state.MoveSpeedBonus = moveSpeedBonus;
            state.DefenseBonus = defenseBonus;
            state.StaminaRegenBonus = staminaRegenBonus;
            state.EitrRegenBonus = eitrRegenBonus;
            state.HyperArmor = hyperArmor;

            int id = player.GetInstanceID();
            Dictionary<string, TimedBuffState> buffs;
            if (!TimedBuffs.TryGetValue(id, out buffs))
            {
                buffs = new Dictionary<string, TimedBuffState>();
                TimedBuffs[id] = buffs;
            }
            buffs[source] = state;
        }

        public static float GetMoveSpeedMultiplier(Player player)
        {
            return Mathf.Max(0.1f, 1f + GetTimedBuffSum(player, "MoveSpeed"));
        }

        public static void BlockStaminaRegen(Player player, float seconds)
        {
            if (player == null)
                return;
            int id = player.GetInstanceID();
            float until = Time.time + Mathf.Max(0.05f, seconds);
            float oldUntil;
            if (!StaminaRegenBlockedUntil.TryGetValue(id, out oldUntil) || until > oldUntil)
                StaminaRegenBlockedUntil[id] = until;
        }

        public static void BlockEitrRegen(Player player, float seconds)
        {
            if (player == null)
                return;
            int id = player.GetInstanceID();
            float until = Time.time + Mathf.Max(0.05f, seconds);
            float oldUntil;
            if (!EitrRegenBlockedUntil.TryGetValue(id, out oldUntil) || until > oldUntil)
                EitrRegenBlockedUntil[id] = until;
        }

        public static bool IsStaminaRegenBlocked(Player player)
        {
            if (player == null)
                return false;
            float until;
            return StaminaRegenBlockedUntil.TryGetValue(player.GetInstanceID(), out until) && Time.time < until;
        }

        public static bool IsEitrRegenBlocked(Player player)
        {
            if (player == null)
                return false;
            float until;
            return EitrRegenBlockedUntil.TryGetValue(player.GetInstanceID(), out until) && Time.time < until;
        }

        public static float GetStaminaRegenMultiplier(Player player)
        {
            return Mathf.Max(0f, 1f + GetTimedBuffSum(player, "StaminaRegen"));
        }

        public static float GetEitrRegenMultiplier(Player player)
        {
            return Mathf.Max(0f, 1f + GetTimedBuffSum(player, "EitrRegen"));
        }

        private static float GetTimedBuffSum(Player player, string stat)
        {
            if (player == null)
                return 0f;

            Dictionary<string, TimedBuffState> buffs;
            if (!TimedBuffs.TryGetValue(player.GetInstanceID(), out buffs))
                return 0f;

            float total = 0f;
            foreach (KeyValuePair<string, TimedBuffState> pair in buffs)
            {
                TimedBuffState buff = pair.Value;
                if (buff == null || Time.time >= buff.EndTime)
                    continue;

                if (stat == "AttackDamage") total += buff.AttackDamageBonus;
                else if (stat == "AttackSpeed") total += buff.AttackSpeedBonus;
                else if (stat == "MoveSpeed") total += buff.MoveSpeedBonus;
                else if (stat == "Defense") total += buff.DefenseBonus;
                else if (stat == "StaminaRegen") total += buff.StaminaRegenBonus;
                else if (stat == "EitrRegen") total += buff.EitrRegenBonus;
            }
            return total;
        }

        public static void ApplyExpose(Character target, float duration)
        {
            if (IsDebuffImmune(target)) return;
            DebuffState state = GetDebuffState(target);
            if (state == null)
                return;
            state.ExposeUntil = Time.time + ResolveDuration(duration);
            RunVfx(delegate { DragonVfx.Status(target, "expose", ResolveDuration(duration)); });
        }

        public static void ApplyBrokenBones(Character target, float duration)
        {
            DebuffState state = GetDebuffState(target);
            if (state == null)
                return;
            state.BrokenBonesUntil = Time.time + ResolveDuration(duration);
        }

        public static void ApplyCripple(Character target, float duration)
        {
            if (IsDebuffImmune(target)) return;
            DebuffState state = GetDebuffState(target);
            if (state == null)
                return;

            float resolved = ResolveDuration(duration);
            state.CrippleUntil = Time.time + resolved;
            RunVfx(delegate { DragonVfx.Status(target, "cripple", resolved); });

            DragonCrippleController controller = target.GetComponent<DragonCrippleController>();
            if (controller == null)
                controller = target.gameObject.AddComponent<DragonCrippleController>();

            float slow = DragonCombatPlugin.Instance == null ? 0.50f : Mathf.Clamp01(DragonCombatPlugin.Instance.CrippleSlow.Value / 100f);
            controller.Apply(target, 1f - slow, resolved);
        }

        public static void ApplyFrost(Character target, float duration)
        {
            if (IsDebuffImmune(target)) return;
            DebuffState state = GetDebuffState(target);
            if (state == null) return;
            float resolved = ResolveDuration(duration);
            state.FrostUntil = Time.time + resolved;
            RunVfx(delegate { DragonVfx.Status(target, "frost", resolved); });
            DragonFrostController controller = target.GetComponent<DragonFrostController>();
            if (controller == null) controller = target.gameObject.AddComponent<DragonFrostController>();
            float slow = DragonCombatPlugin.Instance == null ? 0.30f : Mathf.Clamp01(DragonCombatPlugin.Instance.FrostMovementSlow.Value / 100f);
            controller.Apply(target, 1f - slow, resolved);
        }

        public static bool IsFrosted(Character target)
        {
            if (target == null) return false;
            DebuffState state;
            if (!Debuffs.TryGetValue(target.GetInstanceID(), out state) || state == null) return false;
            return Time.time < state.FrostUntil;
        }

        public static void ApplyFrostAnimationSpeed(Character target, Animator animator)
        {
            if (target == null || animator == null) return;
            int id = animator.GetInstanceID();
            AnimatorSpeedRuntimeState state;
            if (!FrostAnimatorSpeedStates.TryGetValue(id, out state))
            {
                state = new AnimatorSpeedRuntimeState();
                FrostAnimatorSpeedStates[id] = state;
            }
            float current = animator.speed;
            if (current <= 0.05f)
            {
                state.HasOutput = false;
                state.LastOutputSpeed = current;
                state.LastFactor = 1f;
                return;
            }
            float baseSpeed = current;
            if (state.HasOutput && state.LastFactor > 0.05f && Mathf.Abs(current - state.LastOutputSpeed) <= 0.01f) baseSpeed = current / state.LastFactor;
            float factor = 1f;
            if (IsFrosted(target))
            {
                float slow = DragonCombatPlugin.Instance == null ? 0.30f : Mathf.Clamp01(DragonCombatPlugin.Instance.FrostAttackSpeedSlow.Value / 100f);
                factor = Mathf.Max(0.10f, 1f - slow);
            }
            float output = Mathf.Clamp(baseSpeed * factor, 0.05f, 5f);
            animator.speed = output;
            state.HasOutput = true;
            state.LastFactor = factor;
            state.LastOutputSpeed = output;
        }

        public static void ApplyZap(Player attacker, Character target, float damage, float delay, float radius)
        {
            DebuffState state = GetDebuffState(target);
            if (state == null)
                return;

            if (damage <= 0f && DragonCombatPlugin.Instance != null)
                damage = DragonCombatPlugin.Instance.ZapDamage.Value;
            if (delay < 0f)
                delay = 0.01f; // negative = instant detonation
            else if (delay <= 0f && DragonCombatPlugin.Instance != null)
                delay = DragonCombatPlugin.Instance.ZapDelay.Value;
            if (radius <= 0f && DragonCombatPlugin.Instance != null)
                radius = M(DragonCombatPlugin.Instance.ZapRadius.Value);

            state.ZapPending = true;
            state.ZapAt = Time.time + Mathf.Max(0.1f, delay);
            state.ZapAttacker = attacker;
            state.ZapDamage = Mathf.Max(0f, damage);
            state.ZapRadius = Mathf.Max(0.1f, radius);
        }

        public static void Stun(Character target, Vector3 fromPoint)
        {
            if (IsDebuffImmune(target)) return;
            if (target == null || target.IsDead() || target.IsBoss()) // Bosses are never stunned (global rule)
                return;
            Vector3 dir = target.transform.position - fromPoint;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector3.forward;
            target.Stagger(dir.normalized);
            DragonCombat.RunVfx(delegate { DragonVfx.Status(target, "stun", 1.4f); });
        }

        public static bool IsSmallEnemy(Character target)
        {
            if (target == null || target.IsDead() || target.IsBoss())
                return false;

            string n = target.name == null ? "" : target.name.ToLowerInvariant();
            if (n.Contains("greyling") || n.Contains("greydwarf") || n.Contains("boar") || n.Contains("neck") || n.Contains("deer"))
                return true;

            try
            {
                Collider collider = target.GetCollider();
                if (collider != null)
                    return collider.bounds.size.y <= 2.35f;
            }
            catch
            {
            }

            return false;
        }

        public static Vector3 GetIndoorSafeSkyPoint(Vector3 target, float desiredHeight)
        {
            float height = Mathf.Max(0.5f, desiredHeight);
            int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");
            RaycastHit hit;
            Vector3 origin = target + Vector3.up * 0.12f;
            if (Physics.Raycast(origin, Vector3.up, out hit, height, mask))
                height = Mathf.Max(0.45f, hit.distance - 0.25f);
            return target + Vector3.up * height;
        }

        public static bool IsSkillModifierHeld()
        {
            KeyCode modifier = GetConfiguredSkillModifier();
            return Input.GetKey(modifier);
        }

        private static KeyCode GetConfiguredSkillModifier()
        {
            try
            {
                Type cachedSkills = FindTypeCached("AlbedosCustomClassesSkills.SkillsPlugin");

                for (int i = 0; i < 1; i++)
                {
                    Type type = cachedSkills;

                    if (type == null)
                        continue;

                    FieldInfo instanceField = type.GetField(
                        "Instance",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                    );

                    if (instanceField == null)
                        continue;

                    object plugin = instanceField.GetValue(null);

                    if (plugin == null)
                        continue;

                    MethodInfo getter = type.GetMethod(
                        "GetModifierKeyForUi",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                    if (getter == null)
                        continue;

                    object value = getter.Invoke(plugin, null);

                    if (value is KeyCode)
                        return (KeyCode)value;
                }
            }
            catch
            {
            }

            // User's established Mouse4 mapping in Unity is KeyCode.Mouse3.
            return KeyCode.Mouse3;
        }

        public static string GetAdvancementName(Player player)
        {
            return GetAdvancement(player);
        }

        public static string GetClassName(Player player)
        {
            return GetClass(player);
        }

        public static float GetSorcererMagicDamageMultiplier(Player player)
        {
            if (player == null || GetClass(player) != "Sorcerer")
                return 1f;

            float bonus = DragonCombatPlugin.Instance == null || DragonCombatPlugin.Instance.SorcererMagicDamageBonus == null
                ? 0.30f
                : Mathf.Max(0f, DragonCombatPlugin.Instance.SorcererMagicDamageBonus.Value) / 100f;
            return 1f + bonus;
        }

        public static ItemDrop.ItemData GetHandItem(Humanoid humanoid, string fieldName)
        {
            if (humanoid == null || string.IsNullOrEmpty(fieldName))
                return null;

            try
            {
                FieldInfo field = typeof(Humanoid).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field.GetValue(humanoid) as ItemDrop.ItemData;
            }
            catch
            {
            }

            return null;
        }

        public static void SetHandItem(Humanoid humanoid, string fieldName, ItemDrop.ItemData item)
        {
            if (humanoid == null || string.IsNullOrEmpty(fieldName))
                return;

            try
            {
                FieldInfo field = typeof(Humanoid).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    field.SetValue(humanoid, item);
            }
            catch
            {
            }
        }

        public static void SetEquipped(ItemDrop.ItemData item, bool equipped)
        {
            if (item == null)
                return;

            try
            {
                FieldInfo field = typeof(ItemDrop.ItemData).GetField("m_equipped", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    field.SetValue(item, equipped);
            }
            catch
            {
            }
        }

        public static void RefreshEquipment(Humanoid humanoid)
        {
            if (humanoid == null)
                return;

            try
            {
                MethodInfo method = typeof(Humanoid).GetMethod("SetupEquipment", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method != null)
                    method.Invoke(humanoid, null);
            }
            catch
            {
            }
        }

        public static bool IsOneHandedAxe(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return false;

            return item.m_shared.m_skillType.ToString() == "Axes" &&
                   item.m_shared.m_itemType.ToString() == "OneHandedWeapon";
        }

        public static bool IsOneHandedWeapon(ItemDrop.ItemData item)
        {
            return item != null &&
                   item.m_shared != null &&
                   item.m_shared.m_itemType.ToString() == "OneHandedWeapon";
        }

        public static bool IsTwoHandedWeapon(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return false;

            string itemType = item.m_shared.m_itemType.ToString();
            return itemType.IndexOf("TwoHandedWeapon", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsMagicWeapon(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return false;

            string skill = item.m_shared.m_skillType.ToString();
            return skill == "ElementalMagic" || skill == "BloodMagic";
        }

        public static bool IsWand(ItemDrop.ItemData item)
        {
            if (!IsMagicWeapon(item) || item == null || item.m_shared == null)
                return false;

            string sharedName = item.m_shared.m_name == null ? "" : item.m_shared.m_name.ToLowerInvariant();
            string prefabName = "";
            try
            {
                if (item.m_dropPrefab != null && item.m_dropPrefab.name != null)
                    prefabName = item.m_dropPrefab.name.ToLowerInvariant();
            }
            catch
            {
            }
            return sharedName.Contains("wand") || prefabName.Contains("wand");
        }

        public static bool IsStaffWeapon(ItemDrop.ItemData item)
        {
            if (!IsMagicWeapon(item) || item == null || item.m_shared == null || IsWand(item))
                return false;

            string sharedName = item.m_shared.m_name == null ? "" : item.m_shared.m_name.ToLowerInvariant();
            string prefabName = "";
            try
            {
                if (item.m_dropPrefab != null && item.m_dropPrefab.name != null)
                    prefabName = item.m_dropPrefab.name.ToLowerInvariant();
            }
            catch
            {
            }
            return sharedName.Contains("staff") || prefabName.Contains("staff");
        }

        public static bool IsGunStaff(ItemDrop.ItemData item)
        {
            if (!IsMagicWeapon(item) || item.m_shared == null || item.m_shared.m_attack == null)
                return false;

            try
            {
                object attack = item.m_shared.m_attack;
                Type type = attack.GetType();
                string[] floatFields = new string[] { "m_attackChargeTime", "m_attackDrawTime", "m_burstInterval" };
                for (int i = 0; i < floatFields.Length; i++)
                {
                    FieldInfo field = type.GetField(floatFields[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null || field.FieldType != typeof(float))
                        continue;
                    float value = Convert.ToSingle(field.GetValue(attack));
                    if (value > 0.01f && value <= 1.0f)
                        return true;
                }

                FieldInfo burstsField = type.GetField("m_projectileBursts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (burstsField != null)
                {
                    object value = burstsField.GetValue(attack);
                    if (value is int && (int)value > 1)
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool IsShield(ItemDrop.ItemData item)
        {
            return item != null &&
                   item.m_shared != null &&
                   item.m_shared.m_itemType.ToString() == "Shield";
        }

        private static FieldInfo _moveModField, _hitSkillField;

        public static float GetMovementModifier(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return 0f;

            try
            {
                if (_moveModField == null) _moveModField = item.m_shared.GetType().GetField("m_movementModifier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo field = _moveModField;
                if (field != null && field.FieldType == typeof(float))
                    return (float)field.GetValue(item.m_shared);
            }
            catch
            {
            }

            return 0f;
        }

        public static bool IsTowerShield(ItemDrop.ItemData item)
        {
            if (!IsShield(item))
                return false;

            string name = "";
            try
            {
                name = item.m_shared.m_name == null ? "" : item.m_shared.m_name.ToLowerInvariant();
            }
            catch
            {
            }

            if (name.Contains("tower"))
                return true;

            // Vanilla tower shields have a substantially larger movement penalty
            // than ordinary round/buckler shields. This also catches modded tower shields.
            return GetMovementModifier(item) <= -0.10f;
        }

        // v0.25.12 universal stacking burns: consecutive Fire / Spirit Burn ticks on one target ramp up
        // (+20% of the base tick per tick, x4 cap). Ticks less than 0.2s apart share a step (several burns
        // ticking together); a gap of more than 1.6s resets the chain.
        private class BurnChain { public float Last; public int Count; }
        private static readonly Dictionary<string, BurnChain> BurnChains = new Dictionary<string, BurnChain>();

        private static float BurnRamp(Character target, bool spirit)
        {
            if (target == null) return 1f;
            string key = target.GetInstanceID().ToString() + (spirit ? "s" : "f");
            BurnChain c;
            float now = Time.time;
            if (!BurnChains.TryGetValue(key, out c))
            {
                if (BurnChains.Count > 512) BurnChains.Clear();
                c = new BurnChain();
                c.Last = -100f;
                BurnChains[key] = c;
            }
            float gap = now - c.Last;
            if (gap > 1.6f) c.Count = 0;
            else if (gap >= 0.2f) c.Count++;
            c.Last = now;
            DragonCombatPlugin plugin = DragonCombatPlugin.Instance;
            float pct = plugin == null || plugin.BurnRampPercent == null ? 20f : Mathf.Max(0f, plugin.BurnRampPercent.Value);
            float cap = plugin == null || plugin.BurnRampMax == null ? 4f : Mathf.Max(1f, plugin.BurnRampMax.Value);
            return Mathf.Min(cap, 1f + pct / 100f * c.Count);
        }

        public static void ApplySpiritBurnTick(Player attacker, Character target, float damage)
        {
            float amount = ResolveBurnTickDamage(target, damage, true) * GetSorcererMagicDamageMultiplier(attacker) * BurnRamp(target, true);
            ApplyNativeDelayedDamage(attacker, target, "AddSpiritDamage", amount, true);
            if (amount > 0f) RunVfx(delegate { DragonVfx.Status(target, "spiritburn", 0.5f); });
        }

        public static void ApplyFireBurnTick(Player attacker, Character target, float damage)
        {
            float amount = ResolveBurnTickDamage(target, damage, false) * GetSorcererMagicDamageMultiplier(attacker) * BurnRamp(target, false);
            ApplyNativeDelayedDamage(attacker, target, "AddFireDamage", amount, false);
            if (amount > 0f) RunVfx(delegate { DragonVfx.Status(target, "fireburn", 0.5f); });
        }

        private static float ResolveBurnTickDamage(Character target, float fallbackDamage, bool spirit)
        {
            if (target == null || target.IsDead()) return 0f;
            DragonCombatPlugin plugin = DragonCombatPlugin.Instance;
            if (plugin == null || plugin.BurnsUseCurrentHpPercent == null || !plugin.BurnsUseCurrentHpPercent.Value) return Mathf.Max(0f, fallbackDamage);
            float firePercent = plugin.FireBurnCurrentHpPercent == null ? 3f : Mathf.Max(0f, plugin.FireBurnCurrentHpPercent.Value);
            float multiplier = spirit ? (plugin.SpiritBurnMultiplier == null ? 1.5f : Mathf.Max(1f, plugin.SpiritBurnMultiplier.Value)) : 1f;
            float amount = Mathf.Max(0f, target.GetHealth()) * (firePercent * multiplier) / 100f;
            float minimum = plugin.MinimumBurnTick == null ? 1f : Mathf.Max(0f, plugin.MinimumBurnTick.Value);
            return Mathf.Max(minimum, amount);
        }

        private static void ApplyNativeDelayedDamage(Player attacker, Character target, string methodName, float damage, bool spirit)
        {
            if (target == null || target.IsDead() || damage <= 0f)
                return;

            bool applied = false;

            try
            {
                MethodInfo[] methods = typeof(Character).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != methodName)
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length < 1 || parameters[0].ParameterType != typeof(float))
                        continue;

                    object[] args = new object[parameters.Length];
                    args[0] = damage;

                    for (int p = 1; p < parameters.Length; p++)
                    {
                        Type type = parameters[p].ParameterType;

                        if (type == typeof(short))
                            args[p] = (short)0;
                        else if (type == typeof(int))
                            args[p] = 0;
                        else if (type == typeof(float))
                            args[p] = 0f;
                        else if (type == typeof(bool))
                            args[p] = false;
                        else if (type.IsEnum)
                            args[p] = Activator.CreateInstance(type);
                        else if (type.IsValueType)
                            args[p] = Activator.CreateInstance(type);
                        else
                            args[p] = null;
                    }

                    method.Invoke(target, args);
                    applied = true;
                    break;
                }
            }
            catch
            {
                applied = false;
            }

            if (!applied)
            {
                HitData hit = new HitData();

                if (spirit)
                    hit.m_damage.m_spirit = damage;
                else
                    hit.m_damage.m_fire = damage;

                hit.m_point = target.transform.position;
                hit.m_dir = Vector3.up;

                if (attacker != null)
                    hit.SetAttacker(attacker);

                target.Damage(hit);
            }
        }

        public static float GetSkySummonDropTime()
        {
            if (DragonCombatPlugin.Instance == null || DragonCombatPlugin.Instance.SkySummonDropTime == null)
                return 0.18f;

            return Mathf.Clamp(DragonCombatPlugin.Instance.SkySummonDropTime.Value, 0.06f, 0.50f);
        }

        public static float GetSkySummonFallProgress(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);

            // Gravity-like acceleration: brief hang at the top, hard slam at the end.
            return t * t;
        }

        public static void PlaySkillPose(Player player, string style, float duration)
        {
            if (player == null)
                return;

            if (DragonCombatPlugin.Instance != null && !DragonCombatPlugin.Instance.EnableSkillAnimations.Value)
                return;

            DragonSkillPoseDriver driver = player.GetComponent<DragonSkillPoseDriver>();
            if (driver == null)
                driver = player.gameObject.AddComponent<DragonSkillPoseDriver>();

            driver.Begin(style, Mathf.Max(0.10f, duration));
        }

        public static void PlayAnimation(Player player, string trigger)
        {
            if (player == null || string.IsNullOrEmpty(trigger))
                return;
            if (DragonCombatPlugin.Instance != null && !DragonCombatPlugin.Instance.EnableSkillAnimations.Value)
                return;

            try
            {
                FieldInfo field = typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                    return;
                object zanim = field.GetValue(player);
                if (zanim == null)
                    return;
                MethodInfo method = zanim.GetType().GetMethod("SetTrigger", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
                if (method != null)
                    method.Invoke(zanim, new object[] { trigger });
            }
            catch
            {
            }
        }

        public static void PlayWeaponAnimation(Player player, bool preferSecondary, string fallbackTrigger)
        {
            if (player == null)
                return;

            string trigger = "";

            try
            {
                ItemDrop.ItemData weapon = player.GetCurrentWeapon();
                if (weapon != null && weapon.m_shared != null)
                {
                    Attack attack = preferSecondary ? weapon.m_shared.m_secondaryAttack : weapon.m_shared.m_attack;
                    trigger = GetAttackAnimation(attack);

                    if (string.IsNullOrEmpty(trigger))
                    {
                        attack = preferSecondary ? weapon.m_shared.m_attack : weapon.m_shared.m_secondaryAttack;
                        trigger = GetAttackAnimation(attack);
                    }
                }
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(trigger))
                trigger = fallbackTrigger;

            PlayAnimation(player, trigger);
        }

        private static bool IsWarlockMeleeSkill(Skills.SkillType skill)
        {
            return skill == Skills.SkillType.Swords || skill == Skills.SkillType.Knives || skill == Skills.SkillType.Clubs ||
                   skill == Skills.SkillType.Polearms || skill == Skills.SkillType.Spears || skill == Skills.SkillType.Axes ||
                   skill == Skills.SkillType.Unarmed || skill == Skills.SkillType.Pickaxes;
        }

        // v0.25.11: modules rewrite vanilla item tooltips (input = the tooltip text, $tokens not yet localized).
        private static readonly List<Func<ItemDrop.ItemData, string, string>> TooltipFilters = new List<Func<ItemDrop.ItemData, string, string>>();

        public static void RegisterTooltipFilter(Func<ItemDrop.ItemData, string, string> filter)
        {
            if (filter != null && !TooltipFilters.Contains(filter)) TooltipFilters.Add(filter);
        }

        public static string FilterItemTooltip(ItemDrop.ItemData item, string text)
        {
            for (int i = 0; i < TooltipFilters.Count; i++)
            {
                try { string r = TooltipFilters[i](item, text); if (r != null) text = r; } catch { }
            }
            return text;
        }

        public static bool SplitHitInFlight;
        private static HitData _splitRequest;
        private static MethodInfo _memberwiseClone;

        // Called by any module's Damage prefix (which runs before this one): split this hit per damage type.
        public static void RequestDamageSplit(HitData hit)
        {
            _splitRequest = hit;
        }

        private static void SplitPhysicalTypes(Character target, HitData hit)
        {
            float blunt = hit.m_damage.m_blunt, slash = hit.m_damage.m_slash, pierce = hit.m_damage.m_pierce;
            List<KeyValuePair<string, float>> parts = new List<KeyValuePair<string, float>>();
            bool kept = false;
            if (blunt > 0f) kept = true;
            if (slash > 0f) { if (kept) { parts.Add(new KeyValuePair<string, float>("m_slash", slash)); hit.m_damage.m_slash = 0f; } else kept = true; }
            if (pierce > 0f) { if (kept) { parts.Add(new KeyValuePair<string, float>("m_pierce", pierce)); hit.m_damage.m_pierce = 0f; } else kept = true; }
            if (parts.Count == 0 || DragonCombatPlugin.Instance == null) return;
            List<HitData> hits = new List<HitData>();
            for (int i = 0; i < parts.Count; i++)
            {
                HitData part = CloneHitOnly(hit, parts[i].Key, parts[i].Value);
                if (part != null) hits.Add(part);
            }
            DragonCombatPlugin.Instance.StartCoroutine(DealSplitParts(target, hits));
        }

        private static HitData CloneHitOnly(HitData hit, string type, float value)
        {
            try
            {
                if (_memberwiseClone == null) _memberwiseClone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
                HitData part = _memberwiseClone.Invoke(hit, null) as HitData;
                if (part == null) return null;
                FieldInfo df = typeof(HitData).GetField("m_damage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object dmg = df.GetValue(part);   // boxed copy of the DamageTypes struct
                FieldInfo[] fs = dmg.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fs.Length; i++)
                    if (fs[i].FieldType == typeof(float)) fs[i].SetValue(dmg, fs[i].Name == type ? value : 0f);
                df.SetValue(part, dmg);
                part.m_pushForce = 0f;   // one knockback per swing (the original keeps it)
                FieldInfo se = typeof(HitData).GetField("m_statusEffectHash", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (se != null && se.FieldType == typeof(int)) se.SetValue(part, 0);
                return part;
            }
            catch { return null; }
        }

        private static IEnumerator DealSplitParts(Character target, List<HitData> parts)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                yield return new WaitForSeconds(0.06f);
                if (target == null || target.IsDead()) yield break;
                SplitHitInFlight = true;
                try { target.Damage(parts[i]); }
                catch { }
                finally { SplitHitInFlight = false; }
            }
        }

        public static DamagePatchState BeginDamage(Character target, HitData hit)
        {
            DamagePatchState patchState = new DamagePatchState();
            if (target == null || hit == null)
                return patchState;

            // v0.24.0: modules can adjust any incoming hit first (Ranger fall damage, Tailwind).
            for (int f = 0; f < IncomingHitFilters.Count; f++)
            {
                try { IncomingHitFilters[f](target, hit); } catch { }
            }
            // v0.25.85 launch fall rule (after the module filters, so Acrobat / Wildborn reductions stack on top).
            if (target is Player && LaunchFalls.Count > 0 && IsFallHit(hit))
            {
                float lf = LaunchFallFactor((Player)target);
                if (lf < 1f) hit.m_damage.Modify(lf);
            }

            Player attacker = hit.GetAttacker() as Player;
            // v0.25.11: split parts of a hit were already fully modified as the original hit.
            if (attacker != null && !SplitHitInFlight)
            {
                // v0.25.72 universal hit sparks for every skill hit on a creature (skill hits carry no weapon skill)
                if (attacker == Player.m_localPlayer && !(target is Player) && ReadHitSkill(hit) == Skills.SkillType.None)
                {
                    Character impTarget = target; HitData impHit = hit;
                    RunVfx(delegate { DragonVfx.HitImpact(impTarget, impHit); });
                }
                float outgoingBonus = GetTimedBuffSum(attacker, "AttackDamage");
                if (outgoingBonus != 0f)
                    hit.m_damage.Modify(Mathf.Max(0f, 1f + outgoingBonus));

                // v0.23.7 Heaven's Will / Bless Thy Sinners: Cleric hybrid guardians, +10% Magic Damage.
                if (GetClass(attacker) == "Cleric" && DragonCombatPlugin.Instance != null)
                {
                    string ac = GetAdvancementName(attacker);
                    if (ac == "Paladin" || ac == "Priest")
                    {
                        float m = 1f + Mathf.Max(0f, DragonCombatPlugin.Instance.ClericMasteryMagicDamage.Value) / 100f;
                        hit.m_damage.m_fire *= m;
                        hit.m_damage.m_frost *= m;
                        hit.m_damage.m_lightning *= m;
                        hit.m_damage.m_poison *= m;
                        hit.m_damage.m_spirit *= m;
                    }
                }

                // Clockwork: skill hits carry no weapon skill type.
                float clockwork = ClockworkSkillDamage(attacker);
                if (clockwork > 0f && ReadHitSkill(hit) == Skills.SkillType.None)
                    hit.m_damage.Modify(1f + clockwork);

                if (GetClass(attacker) == "Sorcerer")
                {
                    ItemDrop.ItemData current = attacker.GetCurrentWeapon();
                    Skills.SkillType hitSkill = ReadHitSkill(hit);
                    bool normalMagicWeaponHit =
                        current != null &&
                        current.m_shared != null &&
                        IsMagicWeapon(current) &&
                        hitSkill != Skills.SkillType.None &&
                        hitSkill == current.m_shared.m_skillType;

                    if (!normalMagicWeaponHit && IsWarlockMeleeSkill(hitSkill))
                    {
                        // v0.23.0 Warlock: creature-facing melee -70%. Trees, rocks and ore are not
                        // Characters, so labor damage never reaches this and stays unpenalised.
                        hit.m_damage.Modify(0.30f);
                    }

                    if (normalMagicWeaponHit)
                    {
                        // Arcane Blood: Magic Damage covers Eitr-based weapon attacks.
                        hit.m_damage.Modify(GetSorcererMagicDamageMultiplier(attacker));

                        // Rapid Casting trades individual normal Staff/Gun Staff damage
                        // for mobility, efficiency and throughput.
                        if (GetAdvancement(attacker) == "Spellcaster")
                            hit.m_damage.Modify(0.50f);
                    }
                }

                // v0.25.66 (user): Mastery Finisher damage bonus removed
                ApplyMasteryHeavy(attacker, target, hit);
            }

            // v0.25.11 damage split: a hit a module asked to split (e.g. Holy Trinity adds Slash + Pierce to a
            // Blunt mace) keeps its first physical type; every other physical type becomes its own hit a
            // moment later, so each shows its own damage number and meets the target's own resistance.
            if (_splitRequest != null && _splitRequest == hit && !SplitHitInFlight)
            {
                _splitRequest = null;
                SplitPhysicalTypes(target, hit);
            }

            Player targetPlayer = target as Player;
            if (targetPlayer != null)
            {
                float defenseBonus = Mathf.Clamp(GetTimedBuffSum(targetPlayer, "Defense"), 0f, 0.95f);
                if (defenseBonus != 0f)
                    hit.m_damage.Modify(Mathf.Max(0f, 1f - defenseBonus));

                EvaluateIncomingHyper(targetPlayer, hit);
            }

            DebuffState state;
            if (!Debuffs.TryGetValue(target.GetInstanceID(), out state))
                return patchState;
            return ContinueDebuffs(target, hit, patchState, state);
        }

        // v0.25.66 (user: a Troll chipping 8 of 125 HP still pushed the Sword Master): Warrior Hyper Armor is decided
        // by the damage you would really TAKE (after your armor), and it is evaluated on YOUR client too (RPC_Damage),
        // because on a server the attacker's Damage() runs on another machine. Hyper Armor = immovable.
        public static void EvaluateIncomingHyper(Player targetPlayer, HitData hit)
        {
            if (targetPlayer == null || hit == null) return;
                if (GetClass(targetPlayer) == "Warrior")
                {
                    bool merc = GetAdvancement(targetPlayer) == "Mercenary";
                    float threshold = DragonCombatPlugin.Instance == null ? (merc ? 0.60f : 0.30f)
                        : Mathf.Clamp01((merc ? DragonCombatPlugin.Instance.MercenaryHyperArmorThreshold.Value : DragonCombatPlugin.Instance.WarriorHyperArmorThreshold.Value) / 100f);
                    float raw = TakenDamageEstimate(targetPlayer, hit);
                    // v0.24.4: decided by the FIRST hit of an attacker's attack only (follow-up hits within
                    // 1s keep that decision); damage is never accumulated.
                    Character source = hit.GetAttacker();
                    string key = targetPlayer.GetInstanceID() + ":" + (source == null ? 0 : source.GetInstanceID());
                    float last;
                    bool grant;
                    if (!HyperFirstHitLast.TryGetValue(key, out last) || Time.time - last > 1f || !HyperFirstHitGrant.TryGetValue(key, out grant))
                    {
                        grant = raw < targetPlayer.GetMaxHealth() * threshold;
                        if (HyperFirstHitLast.Count > 256) { HyperFirstHitLast.Clear(); HyperFirstHitGrant.Clear(); }
                        HyperFirstHitGrant[key] = grant;
                    }
                    HyperFirstHitLast[key] = Time.time;
                    if (grant) HitHyperArmorUntil[targetPlayer.GetInstanceID()] = Time.time + 0.20f;
                    else HitHyperArmorUntil.Remove(targetPlayer.GetInstanceID());
                }

                // v0.23.8 rule: Hyper Armor = no knockback (push force zeroed on the hit itself,
                // on top of the ApplyPushback / Stagger prefixes).
                if (HasHyperArmor(targetPlayer))
                    hit.m_pushForce = 0f;
        }

        private static MethodInfo _hitClone, _hitApplyArmor, _bodyArmor;

        private static float TakenDamageEstimate(Player p, HitData hit)
        {
            float raw = Mathf.Max(0f, hit.GetTotalDamage());
            try
            {
                if (_hitClone == null) _hitClone = typeof(HitData).GetMethod("Clone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (_hitApplyArmor == null) _hitApplyArmor = typeof(HitData).GetMethod("ApplyArmor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(float) }, null);
                if (_bodyArmor == null) _bodyArmor = typeof(Player).GetMethod("GetBodyArmor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (_hitClone == null || _hitApplyArmor == null || _bodyArmor == null) return raw;
                HitData c = _hitClone.Invoke(hit, null) as HitData;
                if (c == null) return raw;
                _hitApplyArmor.Invoke(c, new object[] { Convert.ToSingle(_bodyArmor.Invoke(p, null)) });
                return Mathf.Max(0f, c.GetTotalDamage());
            }
            catch (Exception) { return raw; }
        }

        private static DamagePatchState ContinueDebuffs(Character target, HitData hit, DamagePatchState patchState, DebuffState state)
        {

            bool expose = Time.time < state.ExposeUntil;
            bool broken = Time.time < state.BrokenBonesUntil;
            bool frost = Time.time < state.FrostUntil;
            if (!expose && !broken && !frost) return patchState;

            patchState.RestoreModifiers = true;
            patchState.OriginalModifiers = target.m_damageModifiers;
            HitData.DamageModifiers modified = target.m_damageModifiers;

            if (expose)
            {
                NeutralizeResistance(ref modified.m_blunt);
                NeutralizeResistance(ref modified.m_slash);
                NeutralizeResistance(ref modified.m_pierce);
                NeutralizeResistance(ref modified.m_fire);
                NeutralizeResistance(ref modified.m_frost);
                NeutralizeResistance(ref modified.m_lightning);
                NeutralizeResistance(ref modified.m_poison);
                NeutralizeResistance(ref modified.m_spirit);

                float bonus = DragonCombatPlugin.Instance == null ? 0.20f : Mathf.Max(0f, DragonCombatPlugin.Instance.ExposeDamageBonus.Value / 100f);
                hit.m_damage.Modify(1f + bonus);
            }

            if (broken)
            {
                NeutralizeResistance(ref modified.m_blunt);
                NeutralizeResistance(ref modified.m_slash);
                NeutralizeResistance(ref modified.m_pierce);
                float bonus = DragonCombatPlugin.Instance == null ? 0.20f : Mathf.Max(0f, DragonCombatPlugin.Instance.BrokenBonesDamageBonus.Value / 100f);
                hit.m_damage.m_blunt *= 1f + bonus;
                hit.m_damage.m_slash *= 1f + bonus;
                hit.m_damage.m_pierce *= 1f + bonus;
            }

            if (frost)
            {
                NeutralizeResistance(ref modified.m_blunt);
                NeutralizeResistance(ref modified.m_slash);
                NeutralizeResistance(ref modified.m_pierce);
                float frostBonus = DragonCombatPlugin.Instance == null ? 0.20f : Mathf.Max(0f, DragonCombatPlugin.Instance.FrostPhysicalDamageBonus.Value / 100f);
                hit.m_damage.m_blunt *= 1f + frostBonus;
                hit.m_damage.m_slash *= 1f + frostBonus;
                hit.m_damage.m_pierce *= 1f + frostBonus;
            }

            target.m_damageModifiers = modified;
            return patchState;
        }

        public static void EndDamage(Character target, DamagePatchState state)
        {
            if (target != null && state.RestoreModifiers)
                target.m_damageModifiers = state.OriginalModifiers;
        }

        private static void NeutralizeResistance(ref HitData.DamageModifier modifier)
        {
            if (modifier == HitData.DamageModifier.Resistant || modifier == HitData.DamageModifier.VeryResistant)
                modifier = HitData.DamageModifier.Normal;
        }

        private static float ResolveDuration(float duration)
        {
            if (duration > 0f)
                return duration;
            if (DragonCombatPlugin.Instance != null)
                return Mathf.Max(0.1f, DragonCombatPlugin.Instance.DefaultDebuffDuration.Value);
            return 6f;
        }

        private static DebuffState GetDebuffState(Character target)
        {
            if (target == null || target.IsDead())
                return null;

            int id = target.GetInstanceID();
            DebuffState state;
            if (!Debuffs.TryGetValue(id, out state))
            {
                state = new DebuffState();
                state.Target = target;
                Debuffs[id] = state;
            }
            return state;
        }

        // v0.25.48 (user: dual wielding with no class): two one-handed weapons stay equipped only for a
        // Mercenary (two Gun Staves only for Horizon Walker). Anyone else - e.g. after a class reset - gets the
        // off-hand weapon unequipped.
        private static float _nextDualCheck;

        private static void EnforceDualWieldOwner(Player p)
        {
            if (p == null || p.IsDead()) return;
            try
            {
                ItemDrop.ItemData l = GetHandItem(p, "m_leftItem");
                ItemDrop.ItemData r = GetHandItem(p, "m_rightItem");
                if (l == null || r == null || l == r) return;
                string adv = GetAdvancementName(p);
                bool dualMelee = IsOneHandedWeapon(l) && IsOneHandedWeapon(r) && !IsGunStaff(l);
                bool dualGun = IsGunStaff(l) && IsGunStaff(r);
                if ((dualMelee && adv != "Mercenary") || (dualGun && adv != "Spellcaster"))
                {
                    p.UnequipItem(l, true);
                    if (MessageHud.instance != null)
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, dualGun ? "Only a Horizon Walker can dual wield Gun Staves" : "Only a Mercenary can dual wield");
                }
            }
            catch (Exception) { }
        }

        // ------------------------------------------------------------------ v0.25.85 LAUNCH FALL RULE (user, universal)
        // Every skill that sends you up protects you from fall damage: none for a fall up to 2x the launch height H,
        // then 75% / 50% / 25% less damage for falls up to 3H / 4H / 5H, full damage beyond (H = 5m: 10m free,
        // 15m -75%, 20m -50%, 25m -25%, 30m+ full). H = the real rise (peak - launch), at least the skill's height.
        private class LaunchFallState { public float LaunchY, PeakY, MinRise, LandedAt = -1f, Granted; }
        private static readonly Dictionary<int, LaunchFallState> LaunchFalls = new Dictionary<int, LaunchFallState>();
        private static MethodInfo _lfGround;

        public static void GrantLaunchFall(Player p, float riseMeters)
        {
            if (p == null) return;
            LaunchFallState st;
            float y = p.transform.position.y;
            if (!LaunchFalls.TryGetValue(p.GetInstanceID(), out st) || st == null || st.LandedAt >= 0f)
            {
                st = new LaunchFallState();
                st.LaunchY = y; st.PeakY = y;
                LaunchFalls[p.GetInstanceID()] = st;
            }
            st.MinRise = Mathf.Max(st.MinRise, M(Mathf.Max(0f, riseMeters)));
            st.Granted = Time.time;
            st.LandedAt = -1f;
        }

        private static void UpdateLaunchFalls()
        {
            if (LaunchFalls.Count == 0) return;
            Player p = Player.m_localPlayer;
            List<int> drop = null;
            foreach (KeyValuePair<int, LaunchFallState> kv in LaunchFalls)
            {
                LaunchFallState st = kv.Value;
                if (p == null || kv.Key != p.GetInstanceID() || st == null) { if (drop == null) drop = new List<int>(); drop.Add(kv.Key); continue; }
                float y = p.transform.position.y;
                if (y > st.PeakY) st.PeakY = y;
                bool grounded = false;
                try { if (_lfGround == null) _lfGround = typeof(Character).GetMethod("IsOnGround", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); grounded = _lfGround != null && (bool)_lfGround.Invoke(p, null); } catch (Exception) { }
                if (grounded && Time.time - st.Granted > 0.4f && st.LandedAt < 0f) st.LandedAt = Time.time;
                if (st.LandedAt >= 0f && Time.time - st.LandedAt > 0.4f) { if (drop == null) drop = new List<int>(); drop.Add(kv.Key); }
                else if (Time.time - st.Granted > 60f) { if (drop == null) drop = new List<int>(); drop.Add(kv.Key); }
            }
            if (drop != null) for (int i = 0; i < drop.Count; i++) LaunchFalls.Remove(drop[i]);
        }

        // Fraction of the fall damage that is still taken (1 = untouched).
        private static float LaunchFallFactor(Player p)
        {
            LaunchFallState st;
            if (p == null || !LaunchFalls.TryGetValue(p.GetInstanceID(), out st) || st == null) return 1f;
            float h = Mathf.Max(st.MinRise, st.PeakY - st.LaunchY);
            if (h <= 0.05f) return 1f;
            float fall = Mathf.Max(0f, st.PeakY - p.transform.position.y);
            if (fall <= 2f * h) return 0f;
            if (fall <= 3f * h) return 0.25f;
            if (fall <= 4f * h) return 0.5f;
            if (fall <= 5f * h) return 0.75f;
            return 1f;
        }

        private static FieldInfo _hitTypeField;
        public static bool IsFallHit(HitData hit)
        {
            try
            {
                if (_hitTypeField == null) _hitTypeField = typeof(HitData).GetField("m_hitType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object v = _hitTypeField == null ? null : _hitTypeField.GetValue(hit);
                return v != null && v.ToString() == "Fall";
            }
            catch (Exception) { return false; }
        }

        // ------------------------------------------------------------------ v0.25.78
        // Buff tooltip notes (user: "put everything"): extra effect lines per timed-buff source, appended to the
        // hover tooltip (stamina use cuts, no movement penalty, skill bonuses... anything ApplyTimedBuff can't carry).
        private static readonly Dictionary<string, string> BuffNotes = new Dictionary<string, string>();
        public static void SetBuffNote(string source, string note)
        {
            if (source == null) return;
            if (string.IsNullOrEmpty(note)) BuffNotes.Remove(source); else BuffNotes[source] = note;
        }

        // Ray of Hope: debuff cleanse + immunity.
        private static readonly Dictionary<int, float> DebuffImmuneUntil = new Dictionary<int, float>();
        public static bool AnyDebuffImmunity { get { return DebuffImmuneUntil.Count > 0; } }
        private static readonly string[] DebuffWords = { "burn", "frost", "poison", "lightning", "spirit", "smoke", "wet", "tared", "slime", "slow", "weak", "bleed", "curse", "corrupt", "rot", "shock", "stagger", "expose", "cripple" };
        public static bool IsDebuffEffectName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.StartsWith("IH_", StringComparison.Ordinal)) return false;
            string n = name.ToLowerInvariant();
            if (n.Contains("freezing") || n.Contains("cold")) return false;   // weather, re-applied by the environment
            for (int i = 0; i < DebuffWords.Length; i++) if (n.Contains(DebuffWords[i])) return true;
            return false;
        }

        public static bool IsDebuffImmune(Character c)
        {
            if (c == null || DebuffImmuneUntil.Count == 0) return false;
            float until;
            if (!DebuffImmuneUntil.TryGetValue(c.GetInstanceID(), out until)) return false;
            if (Time.time < until) return true;
            DebuffImmuneUntil.Remove(c.GetInstanceID());
            return false;
        }

        public static void GrantDebuffImmunity(Player p, float seconds)
        {
            if (p == null || seconds <= 0f) return;
            DebuffImmuneUntil[p.GetInstanceID()] = Time.time + seconds;
            ShowStatus(p, "purity", "purity", "Purity", seconds, 0, "Defense Buff\nImmune to every debuff:\nBurning, Poison, Frost, Shock, Spirit Burn,\nWet, Smoked, Tar, Slow, Stun, Expose, Cripple\nAll debuffs were removed when it started");
            Player pv = p;
            RunVfx(delegate { DragonVfx.Aura(pv.transform, pv.transform.position, new Color(1f, 0.92f, 0.6f, 1f), 0.5f, seconds, 10f, 0.8f); });
        }

        private static FieldInfo _semanChar;
        public static Character SemanOwner(object seman)
        {
            if (seman == null) return null;
            if (_semanChar == null) _semanChar = seman.GetType().GetField("m_character", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return _semanChar == null ? null : _semanChar.GetValue(seman) as Character;
        }

        private static MethodInfo _odbGetSe;
        private static object OdbInstance()
        {
            Type t = FindTypeCached("ObjectDB");
            if (t == null) return null;
            PropertyInfo pi = t.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null) return pi.GetValue(null, null);
            FieldInfo fi = t.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? t.GetField("m_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return fi == null ? null : fi.GetValue(null);
        }

        public static string StatusNameFromHash(int hash)
        {
            try
            {
                object db = OdbInstance();
                if (db == null) return null;
                if (_odbGetSe == null) _odbGetSe = db.GetType().GetMethod("GetStatusEffect", new Type[] { typeof(int) });
                StatusEffect se = _odbGetSe == null ? null : _odbGetSe.Invoke(db, new object[] { hash }) as StatusEffect;
                return se == null ? null : se.name;
            }
            catch (Exception) { return null; }
        }

        public static void CleanseDebuffs(Character c)
        {
            if (c == null) return;
            DebuffState st;
            if (Debuffs.TryGetValue(c.GetInstanceID(), out st) && st != null)
            {
                st.ExposeUntil = 0f; st.BrokenBonesUntil = 0f; st.CrippleUntil = 0f; st.FrostUntil = 0f; st.ZapPending = false;
            }
            try
            {
                object seman = c.GetSEMan();
                if (seman == null) return;
                MethodInfo get = seman.GetType().GetMethod("GetStatusEffects", Type.EmptyTypes);
                System.Collections.IList list = get == null ? null : get.Invoke(seman, null) as System.Collections.IList;
                if (list == null) return;
                MethodInfo remove = seman.GetType().GetMethod("RemoveStatusEffect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(bool) }, null);
                if (remove == null) return;
                List<string> names = new List<string>();
                for (int i = 0; i < list.Count; i++) { StatusEffect se = list[i] as StatusEffect; if (se != null && !(se is IhStatusDisplay) && IsDebuffEffectName(se.name)) names.Add(se.name); }
                for (int i = 0; i < names.Count; i++) remove.Invoke(seman, new object[] { StableHash(names[i]), false });
            }
            catch (Exception) { }
        }

        // Holy Bulwark: hits from inside the Tower Shield arc are turned into frontal hits so the block catches them.
        public static void HolyBulwarkWiden(Player player, object[] args)
        {
            if (player == null || args == null) return;
            HitData hit = null; Character attacker = null;
            for (int i = 0; i < args.Length; i++) { if (hit == null) hit = args[i] as HitData; if (attacker == null) attacker = args[i] as Character; }
            if (hit == null) return;
            DragonCombatPlugin plugin = DragonCombatPlugin.Instance;
            float arc = plugin == null || plugin.BulwarkArc == null ? 300f : Mathf.Clamp(plugin.BulwarkArc.Value, 0f, 360f);
            Vector3 fwd = player.transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 src = attacker != null ? attacker.transform.position : hit.m_point - hit.m_dir * 3f;
            Vector3 to = src - player.transform.position; to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;
            if (Vector3.Dot(to.normalized, fwd) < Mathf.Cos(arc * 0.5f * Mathf.Deg2Rad)) return;
            Vector3 d = hit.m_dir;
            if (Vector3.Dot(d, fwd) > -0.1f)
            {
                d = -fwd; d.y = hit.m_dir.y * 0.5f;
                hit.m_dir = d.normalized;
            }
            _bulwarkFlashAt = Time.time;
            Vector3 fp = player.transform.position + Vector3.up * 1f + to.normalized * 1.2f;
            RunVfx(delegate { DragonVfx.Burst(fp, new Color(1f, 0.92f, 0.6f, 1f), 18, 5f, 0.15f, 0.4f, 0.2f); DragonVfx.Flash(fp, new Color(1f, 0.9f, 0.6f, 1f), 1.5f, 4f, 0.15f); });
        }

        private static float _bulwarkFlashAt = -10f;
        private static GameObject _bulwark;
        private static float _bulwarkAlpha;
        public static float BulwarkFlashAt { get { return _bulwarkFlashAt; } }

        // The holy force field shown in front of a Paladin blocking with a Tower Shield (fades in / out).
        private static void UpdateHolyBulwark(Player p)
        {
            bool on = false;
            try
            {
                if (p != null && !p.IsDead() && GetAdvancementName(p) == "Paladin" && p.IsBlocking())
                {
                    ItemDrop.ItemData sh = GetHandItem(p, "m_leftItem");
                    on = IsShield(sh) && IsTowerShield(sh);
                }
            }
            catch (Exception) { on = false; }
            _bulwarkAlpha = Mathf.MoveTowards(_bulwarkAlpha, on ? 1f : 0f, Time.deltaTime * 6f);
            if (_bulwarkAlpha <= 0.001f)
            {
                if (_bulwark != null) { UnityEngine.Object.Destroy(_bulwark); _bulwark = null; }
                return;
            }
            if (_bulwark == null)
            {
                DragonCombatPlugin plugin = DragonCombatPlugin.Instance;
                float r = plugin == null || plugin.BulwarkRadius == null ? 2f : Mathf.Max(0.5f, plugin.BulwarkRadius.Value);
                _bulwark = DragonVfx.HolyDome(r);
                if (_bulwark == null) return;
            }
            _bulwark.transform.position = p.transform.position + Vector3.up * 0.05f;
            Vector3 f = p.transform.forward; f.y = 0f;
            if (f.sqrMagnitude > 0.001f) _bulwark.transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
            float flash = Mathf.Clamp01(1f - (Time.time - _bulwarkFlashAt) / 0.25f);
            DragonVfx.SetDomeAlpha(_bulwark, _bulwarkAlpha * (0.75f + 0.25f * Mathf.Sin(Time.time * 4f)) + flash * 0.8f);
        }

        public static void RuntimeUpdate()
        {
            float now = Time.time;
            if (Player.m_localPlayer != null || _bulwark != null) UpdateHolyBulwark(Player.m_localPlayer);
            if (PendingLearns.Count > 0) UpdateLearns();
            UpdateLaunchFalls();
            if (now >= _nextDualCheck) { _nextDualCheck = now + 0.5f; EnforceDualWieldOwner(Player.m_localPlayer); DragonCombatPlugin.SyncDualController(Player.m_localPlayer); }

            List<int> removeBuffPlayers = null;
            foreach (KeyValuePair<int, Dictionary<string, TimedBuffState>> outer in TimedBuffs)
            {
                List<string> removeSources = null;
                foreach (KeyValuePair<string, TimedBuffState> pair in outer.Value)
                {
                    if (pair.Value == null || now >= pair.Value.EndTime)
                    {
                        if (removeSources == null) removeSources = new List<string>();
                        removeSources.Add(pair.Key);
                    }
                }
                if (removeSources != null)
                    for (int i = 0; i < removeSources.Count; i++) outer.Value.Remove(removeSources[i]);
                if (outer.Value.Count == 0)
                {
                    if (removeBuffPlayers == null) removeBuffPlayers = new List<int>();
                    removeBuffPlayers.Add(outer.Key);
                }
            }
            if (removeBuffPlayers != null)
                for (int i = 0; i < removeBuffPlayers.Count; i++) TimedBuffs.Remove(removeBuffPlayers[i]);

            List<int> removeDebuffs = null;

            foreach (KeyValuePair<int, DebuffState> pair in Debuffs)
            {
                DebuffState state = pair.Value;
                if (state == null || state.Target == null || state.Target.IsDead())
                {
                    if (removeDebuffs == null)
                        removeDebuffs = new List<int>();
                    removeDebuffs.Add(pair.Key);
                    continue;
                }

                if (state.ZapPending && now >= state.ZapAt)
                {
                    state.ZapPending = false;
                    TriggerZap(state);
                }

                if (!state.ZapPending && now >= state.ExposeUntil && now >= state.BrokenBonesUntil && now >= state.CrippleUntil && now >= state.FrostUntil)
                {
                    if (removeDebuffs == null)
                        removeDebuffs = new List<int>();
                    removeDebuffs.Add(pair.Key);
                }
            }

            if (removeDebuffs != null)
            {
                for (int i = 0; i < removeDebuffs.Count; i++)
                    Debuffs.Remove(removeDebuffs[i]);
            }
        }

        private static void TriggerZap(DebuffState state)
        {
            Character centerTarget = state.Target;
            if (centerTarget == null)
                return;

            Vector3 center = centerTarget.transform.position;
            Collider[] hits = Physics.OverlapSphere(center, Mathf.Max(0.1f, state.ZapRadius));
            HashSet<Character> damaged = new HashSet<Character>();

            for (int i = 0; i < hits.Length; i++)
            {
                Character target = hits[i].GetComponentInParent<Character>();
                if (target == null || damaged.Contains(target) || target.IsDead())
                    continue;
                if (state.ZapAttacker != null && !IsEnemy(state.ZapAttacker, target))
                    continue;

                damaged.Add(target);
                HitData hit = new HitData();
                hit.m_damage.m_lightning = Mathf.Max(0f, state.ZapDamage);
                hit.m_point = target.transform.position;
                hit.m_dir = (target.transform.position - center).normalized;
                hit.m_pushForce = 4f;
                if (state.ZapAttacker != null)
                    hit.SetAttacker(state.ZapAttacker);
                target.Damage(hit);
            }

            if (DragonCombatPlugin.Instance != null)
                DragonCombatPlugin.Instance.StartCoroutine(ZapVisual(center, state.ZapRadius));
        }

        private static IEnumerator ZapVisual(Vector3 center, float radius)
        {
            GameObject obj = new GameObject("DragonsAltarZap");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 33;
            line.startWidth = 0.07f;
            line.endWidth = 0.07f;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            float duration = 0.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                Color c = new Color(0.55f, 0.86f, 1f, 1f - t);
                line.startColor = c;
                line.endColor = c;
                float r = Mathf.Lerp(0.15f, Mathf.Max(0.3f, radius), t);
                for (int i = 0; i < line.positionCount; i++)
                {
                    float a = ((float)i / (float)(line.positionCount - 1)) * Mathf.PI * 2f;
                    line.SetPosition(i, center + Vector3.up * 0.12f + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            UnityEngine.Object.Destroy(obj);
        }

        private static bool IsEnemy(Player attacker, Character target)
        {
            if (target == null || attacker == null || target == attacker || target.IsDead() || target is Player)
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

        public static Player GetPlayerFromSEMan(object seman)
        {
            if (seman == null)
                return null;
            try
            {
                FieldInfo[] fields = seman.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

        public static bool IsWeaponMasteryAttack(Player player, Attack attack)
        {
            if (player == null || attack == null)
                return false;

            ItemDrop.ItemData weapon = GetAttackWeapon(attack);
            if (weapon == null)
                weapon = player.GetCurrentWeapon();

            if (weapon == null || weapon.m_shared == null)
                return false;

            Skills.SkillType skill = weapon.m_shared.m_skillType;
            string advancement = GetAdvancement(player);

            return (advancement == "Sword Master" && skill == Skills.SkillType.Swords) ||
                   (advancement == "Mercenary" && skill == Skills.SkillType.Axes) ||
                   ((advancement == "Paladin" || advancement == "Priest") && skill == Skills.SkillType.Clubs);
        }

        public static void RegisterMasteryAttackStart(Player player, Attack attack, ItemDrop.ItemData startWeapon)
        {
            if (player == null || attack == null)
                return;

            ItemDrop.ItemData weapon = startWeapon;
            if (weapon == null)
                weapon = GetAttackWeapon(attack);
            if (weapon == null)
                weapon = player.GetCurrentWeapon();

            if (weapon == null || weapon.m_shared == null)
                return;

            Skills.SkillType skill = weapon.m_shared.m_skillType;
            string advancement = GetAdvancement(player);

            bool mastery =
                (advancement == "Sword Master" && skill == Skills.SkillType.Swords) ||
                (advancement == "Mercenary" && skill == Skills.SkillType.Axes) ||
                ((advancement == "Paladin" || advancement == "Priest") && skill == Skills.SkillType.Clubs);

            if (!mastery)
                return;

            int id = player.GetInstanceID();
            if (IsSecondaryAttack(attack, weapon))
            {
                MasteryHeavyUntil[id] = Time.time + 1.25f;
                return;
            }

            MasteryComboState state;
            if (!MasteryCombos.TryGetValue(id, out state))
            {
                state = new MasteryComboState();
                MasteryCombos[id] = state;
            }

            float window = DragonCombatPlugin.Instance == null ? 1.5f : Mathf.Max(0.2f, DragonCombatPlugin.Instance.MasteryComboWindow.Value);
            if (Time.time - state.LastAttackTime > window)
                state.Stage = 0;

            state.Stage++;
            if (state.Stage > 5)
                state.Stage = 1;

            state.LastAttackTime = Time.time;
            state.SkillType = skill;

            // v0.25.66 (user): no "Weapon Mastery: Finisher" - the last hit of a chain is simply Valheim's own last swing.
        }

        private static IEnumerator MasteryFinisherVisual(Player player)
        {
            if (player == null)
                yield break;

            GameObject obj = new GameObject("DragonsAltarMasteryFinisher");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 33;
            line.startWidth = 0.06f;
            line.endWidth = 0.06f;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            float elapsed = 0f;
            float duration = 0.32f;
            while (elapsed < duration)
            {
                if (player == null)
                    break;

                float t = elapsed / duration;
                float radius = Mathf.Lerp(0.45f, 1.75f, t);
                Vector3 center = player.transform.position + Vector3.up * 0.85f;
                Color c = new Color(1f, 0.72f, 0.24f, 1f - t);
                line.startColor = c;
                line.endColor = new Color(1f, 0.94f, 0.60f, c.a);

                for (int i = 0; i < line.positionCount; i++)
                {
                    float a = ((float)i / (float)(line.positionCount - 1)) * Mathf.PI * 2f;
                    line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            UnityEngine.Object.Destroy(obj);
        }

        public static int GetMasteryComboStage(Player player)
        {
            if (player == null)
                return 0;
            MasteryComboState state;
            if (!MasteryCombos.TryGetValue(player.GetInstanceID(), out state))
                return 0;
            float window = DragonCombatPlugin.Instance == null ? 1.5f : Mathf.Max(0.2f, DragonCombatPlugin.Instance.MasteryComboWindow.Value);
            if (Time.time - state.LastAttackTime > window)
                return 0;
            return state.Stage;
        }

        private static bool IsSecondaryAttack(Attack attack, ItemDrop.ItemData weapon)
        {
            try
            {
                if (attack == null || weapon == null || weapon.m_shared == null || weapon.m_shared.m_secondaryAttack == null)
                    return false;

                if (object.ReferenceEquals(attack, weapon.m_shared.m_secondaryAttack))
                    return true;

                string secondary = GetAttackAnimation(weapon.m_shared.m_secondaryAttack);
                string current = GetAttackAnimation(attack);

                return !string.IsNullOrEmpty(secondary) &&
                       !string.IsNullOrEmpty(current) &&
                       current == secondary;
            }
            catch
            {
                return false;
            }
        }

        private static ItemDrop.ItemData GetAttackWeapon(Attack attack)
        {
            if (attack == null)
                return null;

            try
            {
                FieldInfo field = typeof(Attack).GetField(
                    "m_weapon",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (field != null)
                    return field.GetValue(attack) as ItemDrop.ItemData;
            }
            catch
            {
            }

            return null;
        }

        private static string GetAttackAnimation(Attack attack)
        {
            if (attack == null)
                return "";

            try
            {
                FieldInfo field = typeof(Attack).GetField(
                    "m_attackAnimation",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (field != null)
                {
                    object value = field.GetValue(attack);
                    if (value != null)
                        return value.ToString();
                }
            }
            catch
            {
            }

            return "";
        }

        private static void ApplyMasteryHeavy(Player attacker, Character target, HitData hit)
        {
            if (attacker == null || target == null || hit == null)
                return;

            float end;
            if (!MasteryHeavyUntil.TryGetValue(attacker.GetInstanceID(), out end) || Time.time >= end)
                return;

            Skills.SkillType hitSkill = ReadHitSkill(hit);
            ItemDrop.ItemData weapon = attacker.GetCurrentWeapon();
            if (weapon == null || weapon.m_shared == null || hitSkill != weapon.m_shared.m_skillType)
                return;

            float multiplier = DragonCombatPlugin.Instance == null ? 1.5f : Mathf.Max(0f, DragonCombatPlugin.Instance.MasteryHeavyMultiplier.Value);
            hit.m_damage.Modify(multiplier);

            if (IsSmallEnemy(target))
                Stun(target, attacker.transform.position);
        }

        private static void ApplyMasteryFinisher(Player attacker, HitData hit)
        {
            if (attacker == null || hit == null)
                return;

            MasteryComboState state;
            if (!MasteryCombos.TryGetValue(attacker.GetInstanceID(), out state) || Time.time >= state.FinisherUntil)
                return;

            if (state.Stage != 5 || state.LastAttackTime > state.FinisherAttackStartedAt + 0.02f)
                return;

            Skills.SkillType skill = ReadHitSkill(hit);
            if (skill == Skills.SkillType.None || skill != state.SkillType)
                return;

            float multiplier = DragonCombatPlugin.Instance == null ? 1.5f : Mathf.Max(0f, DragonCombatPlugin.Instance.MasteryFinisherMultiplier.Value);
            hit.m_damage.Modify(multiplier);
        }

        private static Skills.SkillType ReadHitSkill(HitData hit)
        {
            try
            {
                if (_hitSkillField == null) _hitSkillField = typeof(HitData).GetField("m_skill", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo field = _hitSkillField;
                if (field == null)
                    return Skills.SkillType.None;
                object value = field.GetValue(hit);
                if (value is Skills.SkillType)
                    return (Skills.SkillType)value;
            }
            catch
            {
            }
            return Skills.SkillType.None;
        }

        private static string GetClass(Player player)
        {
            return ReadPlayerData(player, ClassDataKey);
        }

        private static string GetAdvancement(Player player)
        {
            return ReadPlayerData(player, AdvancementDataKey);
        }

        private static FieldInfo _customDataField;   // v0.25.2 perf: read on every hit / speed / regen call

        private static string ReadPlayerData(Player player, string key)
        {
            if (player == null)
                return "";
            try
            {
                if (_customDataField == null) _customDataField = typeof(Player).GetField("m_customData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo field = _customDataField;
                if (field == null)
                    return "";
                IDictionary data = field.GetValue(player) as IDictionary;
                if (data == null || !data.Contains(key))
                    return "";
                object value = data[key];
                return value == null ? "" : value.ToString();
            }
            catch
            {
                return "";
            }
        }
    }

    public class DragonFrostController : MonoBehaviour
    {
        private Character _target;
        private float _endTime;
        private bool _applied;
        private float _acceleration, _speed, _runSpeed, _flyFastSpeed, _swimSpeed;
        private float _appliedAcceleration, _appliedSpeed, _appliedRunSpeed, _appliedFlyFastSpeed, _appliedSwimSpeed;

        public void Apply(Character target, float multiplier, float duration)
        {
            if (target == null) return;
            if (!_applied)
            {
                _target = target; _acceleration = target.m_acceleration; _speed = target.m_speed; _runSpeed = target.m_runSpeed; _flyFastSpeed = target.m_flyFastSpeed; _swimSpeed = target.m_swimSpeed;
            }
            else RestoreValues();
            float m = Mathf.Clamp(multiplier, 0.1f, 1f);
            _endTime = Time.time + Mathf.Max(0.1f, duration);
            _appliedAcceleration = _acceleration*m; _appliedSpeed=_speed*m; _appliedRunSpeed=_runSpeed*m; _appliedFlyFastSpeed=_flyFastSpeed*m; _appliedSwimSpeed=_swimSpeed*m;
            target.m_acceleration=_appliedAcceleration; target.m_speed=_appliedSpeed; target.m_runSpeed=_appliedRunSpeed; target.m_flyFastSpeed=_appliedFlyFastSpeed; target.m_swimSpeed=_appliedSwimSpeed;
            _applied=true;
        }
        private void Update(){ if (_applied && Time.time >= _endTime) Destroy(this); }
        private void OnDestroy(){ RestoreValues(); }
        private void RestoreValues()
        {
            if (!_applied || _target == null) return;
            if (Mathf.Approximately(_target.m_acceleration,_appliedAcceleration)) _target.m_acceleration=_acceleration;
            if (Mathf.Approximately(_target.m_speed,_appliedSpeed)) _target.m_speed=_speed;
            if (Mathf.Approximately(_target.m_runSpeed,_appliedRunSpeed)) _target.m_runSpeed=_runSpeed;
            if (Mathf.Approximately(_target.m_flyFastSpeed,_appliedFlyFastSpeed)) _target.m_flyFastSpeed=_flyFastSpeed;
            if (Mathf.Approximately(_target.m_swimSpeed,_appliedSwimSpeed)) _target.m_swimSpeed=_swimSpeed;
            _applied=false;
        }
    }

    public class DragonCrippleController : MonoBehaviour
    {
        private Character _target;
        private float _endTime;
        private float _multiplier = 1f;
        private bool _applied;
        private float _acceleration;
        private float _speed;
        private float _runSpeed;
        private float _flyFastSpeed;
        private float _swimSpeed;
        private float _appliedAcceleration;
        private float _appliedSpeed;
        private float _appliedRunSpeed;
        private float _appliedFlyFastSpeed;
        private float _appliedSwimSpeed;

        public void Apply(Character target, float multiplier, float duration)
        {
            if (target == null)
                return;

            if (!_applied)
            {
                _target = target;
                _acceleration = target.m_acceleration;
                _speed = target.m_speed;
                _runSpeed = target.m_runSpeed;
                _flyFastSpeed = target.m_flyFastSpeed;
                _swimSpeed = target.m_swimSpeed;
            }
            else
            {
                RestoreValues();
            }

            _multiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
            _endTime = Time.time + Mathf.Max(0.1f, duration);
            _appliedAcceleration = _acceleration * _multiplier;
            _appliedSpeed = _speed * _multiplier;
            _appliedRunSpeed = _runSpeed * _multiplier;
            _appliedFlyFastSpeed = _flyFastSpeed * _multiplier;
            _appliedSwimSpeed = _swimSpeed * _multiplier;

            target.m_acceleration = _appliedAcceleration;
            target.m_speed = _appliedSpeed;
            target.m_runSpeed = _appliedRunSpeed;
            target.m_flyFastSpeed = _appliedFlyFastSpeed;
            target.m_swimSpeed = _appliedSwimSpeed;
            _applied = true;
        }

        private void Update()
        {
            if (_applied && Time.time >= _endTime)
                Destroy(this);
        }

        private void OnDestroy()
        {
            RestoreValues();
        }

        private void RestoreValues()
        {
            if (!_applied || _target == null)
                return;

            if (Mathf.Approximately(_target.m_acceleration, _appliedAcceleration))
                _target.m_acceleration = _acceleration;
            if (Mathf.Approximately(_target.m_speed, _appliedSpeed))
                _target.m_speed = _speed;
            if (Mathf.Approximately(_target.m_runSpeed, _appliedRunSpeed))
                _target.m_runSpeed = _runSpeed;
            if (Mathf.Approximately(_target.m_flyFastSpeed, _appliedFlyFastSpeed))
                _target.m_flyFastSpeed = _flyFastSpeed;
            if (Mathf.Approximately(_target.m_swimSpeed, _appliedSwimSpeed))
                _target.m_swimSpeed = _swimSpeed;

            _applied = false;
        }
    }

    // Display-only status effect (no gameplay effect): stacks replace the timer text.
    // v0.25.42 DUAL WIELD CLIPS: the DualWield mod (Smoothbrain) ships its dual-wield attack animations as an
    // asset bundle embedded in DualWield.dll. If the user drops that DLL into ImmortalHeroesAssets/ (NOT into
    // plugins), its bundle is read and an AnimatorOverrideController swaps the one-handed attack clips for the
    // dual-wield ones while two one-handed weapons are held - the same clip map the mod itself uses.
    // ---------------------------------------------------------------- v0.25.111 MIXAMO CLIPS
    // Real animation clips (Mixamo FBX -> Unity Humanoid -> Asset Bundle 'immortalheroes_anims' in
    // ImmortalHeroesAssets). A clip is played on the player's own Animator through a Playables output that is
    // blended over Valheim's controller (fade in / out), between two clip times at a chosen speed, so its hit
    // frame lands on the skill's damage. Root motion is baked into the pose in Unity, so the clip never moves
    // the character: the skill code keeps owning movement. No bundle = the old animation plays (fallback).
    public static class DragonMixamo
    {
        private static bool _tried;
        private static Dictionary<string, AnimationClip> _clips;

        private static void Load()
        {
            if (_tried) return;
            _tried = true;
            try
            {
                string path = Paths.PluginPath + "/ImmortalHeroesAssets/immortalheroes_anims";
                if (!System.IO.File.Exists(path)) { DragonCombatPlugin.Instance.LogInfo("Mixamo clips: no bundle at " + path + " (old animations used)."); return; }
                AssetBundle bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null) { DragonCombatPlugin.Instance.LogInfo("Mixamo clips: bundle failed to load (built with a different Unity version?)."); return; }
                _clips = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
                AnimationClip[] all = bundle.LoadAllAssets<AnimationClip>();
                List<string> names = new List<string>();
                for (int i = 0; i < all.Length; i++)
                    if (all[i] != null && !all[i].name.StartsWith("__preview__", StringComparison.Ordinal)) { _clips[all[i].name] = all[i]; names.Add(all[i].name); }
                DragonCombatPlugin.Instance.LogInfo("Mixamo clips loaded (" + names.Count + "): " + string.Join(", ", names.ToArray()));
            }
            catch (Exception e) { DragonCombatPlugin.Instance.LogInfo("Mixamo clips: " + e.Message); }
        }

        public static bool Has(string name)
        {
            Load();
            return _clips != null && _clips.ContainsKey(name);
        }

        // Plays clip time 'from' -> 'to' at 'speed'; returns false (nothing happens) when the clip is missing.
        public static bool Play(Player player, string name, float from, float to, float speed, float fadeIn, float fadeOut)
        {
            Load();
            AnimationClip clip;
            if (player == null || _clips == null || !_clips.TryGetValue(name, out clip)) return false;
            Animator an = player.GetComponentInChildren<Animator>();
            if (an == null || !an.isHuman) return false;
            DragonMixamoPlayer p = an.GetComponent<DragonMixamoPlayer>();
            if (p == null) p = an.gameObject.AddComponent<DragonMixamoPlayer>();
            p.Begin(an, clip, from, to, speed, fadeIn, fadeOut);
            return true;
        }
    }

    public class DragonMixamoPlayer : MonoBehaviour
    {
        private UnityEngine.Playables.PlayableGraph _graph;
        private UnityEngine.Animations.AnimationPlayableOutput _output;
        private UnityEngine.Animations.AnimationClipPlayable _clip;
        private bool _live, _stopping;
        private float _to, _fadeIn, _fadeOut, _age, _stopAt;

        public void Begin(Animator an, AnimationClip clip, float from, float to, float speed, float fadeIn, float fadeOut)
        {
            Kill();
            _graph = UnityEngine.Playables.PlayableGraph.Create("IH Mixamo " + clip.name);
            _graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.GameTime);
            _output = UnityEngine.Animations.AnimationPlayableOutput.Create(_graph, "IH Mixamo", an);
            _clip = UnityEngine.Animations.AnimationClipPlayable.Create(_graph, clip);
            _clip.SetApplyFootIK(true);
            UnityEngine.Playables.PlayableExtensions.SetTime(_clip, Mathf.Max(0f, from));
            UnityEngine.Playables.PlayableExtensions.SetSpeed(_clip, Mathf.Clamp(speed, 0.1f, 6f));
            UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(_output, _clip);
            UnityEngine.Playables.PlayableOutputExtensions.SetWeight(_output, 0f);
            _graph.Play();
            _to = Mathf.Max(from + 0.05f, Mathf.Min(to, clip.length));
            _fadeIn = Mathf.Max(0.01f, fadeIn);
            _fadeOut = Mathf.Max(0.01f, fadeOut);
            _age = 0f; _stopping = false; _live = true;
        }

        private void Update()
        {
            if (!_live) return;
            if (!_graph.IsValid()) { _live = false; return; }
            _age += Time.deltaTime;
            double t = UnityEngine.Playables.PlayableExtensions.GetTime(_clip);
            if (t >= _to && !_stopping)
            {
                // hold the last frame (never play the clip's own settle-back) while fading out
                UnityEngine.Playables.PlayableExtensions.SetSpeed(_clip, 0.0);
                UnityEngine.Playables.PlayableExtensions.SetTime(_clip, _to);
                _stopping = true; _stopAt = _age;
            }
            float w = Mathf.Clamp01(_age / _fadeIn);
            if (_stopping) w *= 1f - Mathf.Clamp01((_age - _stopAt) / _fadeOut);
            UnityEngine.Playables.PlayableOutputExtensions.SetWeight(_output, w);
            if (_stopping && _age - _stopAt >= _fadeOut) Kill();
        }

        private void Kill()
        {
            if (_live && _graph.IsValid()) _graph.Destroy();
            _live = false;
        }

        private void OnDestroy() { Kill(); }
    }

    public static class DragonDualWield
    {
        private static bool _tried;
        private static Dictionary<string, AnimationClip> _clips;
        private static readonly Dictionary<int, RuntimeAnimatorController> _original = new Dictionary<int, RuntimeAnimatorController>();
        private static readonly Dictionary<int, RuntimeAnimatorController> _dual = new Dictionary<int, RuntimeAnimatorController>();
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            { "Attack1", "Attack1" }, { "Attack2", "Attack2" }, { "Attack3", "Attack3" },
            { "axe_swing", "Attack1" }, { "Axe combo 2", "Attack2" }, { "Axe combo 3", "Attack3" },
            { "knife_slash0", "Attack1" }, { "knife_slash1", "Attack2" }, { "knife_slash2", "Attack3" },
            { "fight idle", "DWblock" }, { "Block idle", "DWblock" },
            { "Sword-Attack-R4", "DWspecial" }, { "Knife JumpAttack", "DWspecial" }, { "MaceAltAttack", "DWspecial" }, { "Axe Secondary Attack", "DWspecial2" }
        };

        private static void Load()
        {
            if (_tried) return;
            _tried = true;
            try
            {
                // The real DualWield mod already handles everything when it is installed.
                if (DragonCombat.FindTypeCached("DualWield.DualWield") != null) return;
                string path = System.IO.Path.Combine(System.IO.Path.Combine(BepInEx.Paths.PluginPath, "ImmortalHeroesAssets"), "DualWield.dll");
                if (!System.IO.File.Exists(path)) return;
                System.Reflection.Assembly asm = System.Reflection.Assembly.LoadFile(path);
                string res = null;
                string[] names = asm.GetManifestResourceNames();
                for (int i = 0; i < names.Length; i++) if (names[i].EndsWith("dwanimations", StringComparison.Ordinal)) res = names[i];
                if (res == null) return;
                AssetBundle bundle;
                using (System.IO.Stream st = asm.GetManifestResourceStream(res)) bundle = AssetBundle.LoadFromStream(st);
                if (bundle == null) return;
                _clips = new Dictionary<string, AnimationClip>();
                string[] want = { "Attack1", "Attack2", "Attack3", "DWblock", "DWspecial", "DWspecial2" };
                for (int i = 0; i < want.Length; i++)
                {
                    AnimationClip clip = bundle.LoadAsset<AnimationClip>(want[i]);
                    if (clip != null) _clips[want[i]] = clip;
                }
                if (_clips.Count == 0) _clips = null;
                else if (DragonCombatPlugin.Instance != null) DragonCombatPlugin.Instance.LogInfo("[Immortal Heroes] DualWield animations loaded: " + _clips.Count);
            }
            catch (Exception ex)
            {
                _clips = null;
                if (DragonCombatPlugin.Instance != null) DragonCombatPlugin.Instance.LogInfo("[Immortal Heroes] DualWield.dll could not be read: " + ex.Message);
            }
        }

        // dual = true: use the dual-wield controller (returns whether it is available); false: restore.
        public static bool Apply(Player player, Animator animator, bool dual)
        {
            if (player == null || animator == null) return false;
            int id = animator.GetInstanceID();
            if (!dual)
            {
                RuntimeAnimatorController orig;
                if (_original.TryGetValue(id, out orig) && animator.runtimeAnimatorController != orig) { animator.runtimeAnimatorController = orig; animator.Update(0f); }
                return false;
            }
            Load();
            if (_clips == null) return false;
            try
            {
                RuntimeAnimatorController baseCtrl;
                if (!_original.TryGetValue(id, out baseCtrl)) { baseCtrl = animator.runtimeAnimatorController; _original[id] = baseCtrl; }
                RuntimeAnimatorController dualCtrl;
                if (!_dual.TryGetValue(id, out dualCtrl))
                {
                    AnimatorOverrideController aoc = new AnimatorOverrideController(baseCtrl);
                    List<KeyValuePair<AnimationClip, AnimationClip>> list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    AnimationClip[] all = aoc.animationClips;
                    for (int i = 0; i < all.Length; i++)
                    {
                        string key; AnimationClip ext;
                        if (all[i] != null && Map.TryGetValue(all[i].name, out key) && _clips.TryGetValue(key, out ext))
                        {
                            AnimationClip copy = UnityEngine.Object.Instantiate(ext);
                            copy.name = all[i].name;
                            list.Add(new KeyValuePair<AnimationClip, AnimationClip>(all[i], copy));
                        }
                        else list.Add(new KeyValuePair<AnimationClip, AnimationClip>(all[i], all[i]));
                    }
                    aoc.ApplyOverrides(list);
                    dualCtrl = aoc;
                    _dual[id] = dualCtrl;
                }
                if (animator.runtimeAnimatorController != dualCtrl) { animator.runtimeAnimatorController = dualCtrl; animator.Update(0f); }
                return true;
            }
            catch (Exception) { return false; }
        }
    }

    // v0.25.42 (user): Whirlwind = Circle Swing's spin (atgeir_secondary) repeated with no visible cut: once the
    // spin state is known, every time it reaches the end of the turn it is cross-faded back to the start of the
    // turn (skipping the wind up and the recovery) until the skill ends.
    public class DragonVanillaLoop : MonoBehaviour
    {
        private Animator _a;
        private float _end, _startN, _endN, _fired;
        private string _trigger;
        private int _layer = -1, _hash;
        private int[] _pre;

        public static bool Play(Player player, string trigger, float seconds, float startN, float endN)
        {
            if (player == null) return false;
            Animator a = player.GetComponentInChildren<Animator>();
            string t = DragonCombat.ResolveTriggerName(a, trigger);
            if (t == null) return false;
            DragonVanillaLoop l = player.GetComponent<DragonVanillaLoop>();
            if (l == null) l = player.gameObject.AddComponent<DragonVanillaLoop>();
            l._a = a; l._trigger = t; l._end = Time.time + Mathf.Max(0.3f, seconds);
            l._startN = Mathf.Clamp01(startN); l._endN = Mathf.Clamp(endN, l._startN + 0.05f, 1f);
            l._layer = -1;
            try
            {
                l._pre = new int[a.layerCount];
                for (int i = 0; i < l._pre.Length; i++) l._pre[i] = a.GetCurrentAnimatorStateInfo(i).fullPathHash;
            }
            catch (Exception) { l._pre = new int[0]; }
            DragonCombat.FireVanilla(player, t);
            l._fired = Time.time;
            return true;
        }

        private void Update()
        {
            if (_a == null) { Destroy(this); return; }
            Character owner = GetComponent<Character>();
            if (owner == null || owner.IsDead() || Time.time >= _end) { Destroy(this); return; }
            try
            {
                if (_layer < 0)
                {
                    for (int i = 0; i < _pre.Length && _layer < 0; i++)
                    {
                        AnimatorStateInfo st = _a.IsInTransition(i) ? _a.GetNextAnimatorStateInfo(i) : _a.GetCurrentAnimatorStateInfo(i);
                        if (st.fullPathHash != _pre[i]) { _layer = i; _hash = st.fullPathHash; }
                    }
                    if (_layer < 0 && Time.time - _fired > 0.6f) Destroy(this);
                    return;
                }
                if (_a.IsInTransition(_layer)) return;
                AnimatorStateInfo cur = _a.GetCurrentAnimatorStateInfo(_layer);
                bool left = cur.fullPathHash != _hash;
                float n = cur.normalizedTime - Mathf.Floor(cur.normalizedTime);
                if (left || n >= _endN) _a.CrossFade(_hash, 0.06f, _layer, _startN);
            }
            catch (Exception) { Destroy(this); }
        }
    }

    public class IhStatusDisplay : StatusEffect
    {
        public int Stacks;
        // v0.25.34 hover tooltip: first line = buff type ("Attack Buff"...), then the numbers; {stacks} = Stacks.
        public string Detail;

        // v0.25.9: StatusEffect.m_time is protected in Valheim; outside code reads the time left here.
        public float Remaining()
        {
            return m_ttl > 0f ? Mathf.Max(0f, m_ttl - m_time) : 0f;
        }

        // v0.25.4: timers always in seconds (never "2m" that jumps to 59s).
        public override string GetIconText()
        {
            if (Stacks > 0) return Stacks.ToString();
            if (m_ttl <= 0f) return "";
            return Mathf.CeilToInt(Mathf.Max(0f, m_ttl - m_time)).ToString() + "s";
        }
    }
}

namespace DragonsAltarCombat
{
    // ==================================================================================
    // v0.25.54 SKILL VFX LIBRARY (user: skills must come to life, not prototype drawings). Every module can call
    // these. Real ParticleSystems (soft additive glow sprites generated at runtime), point-light flashes, jagged
    // flickering lightning, light pillars, shockwaves, auras that follow a target, and Valheim's own effect
    // prefabs (looked up by name in ZNetScene, network-free ones only). All objects clean themselves up.
    // ==================================================================================
    public class DragonVfxLife : MonoBehaviour
    {
        public float Life = 1f, Age;
        public Light FlashLight;
        public float LightPeak, LightHold;
        public Transform Follow;
        public Vector3 FollowOffset;
        public LineRenderer[] Bolts;
        public Vector3 BoltA, BoltB;
        public float BoltJitter, NextFlicker, BoltWidth;
        public float StopEmitAt = -1f;
        public ParticleSystem[] Systems;
        public bool Stopped;
        public Color BoltCore, BoltGlow;
        public Func<bool> Keep;
        private bool _hadFollow;

        private void Update()
        {
            Age += Time.deltaTime;
            if (Keep != null)
            {
                bool on = false;
                try { on = Keep(); } catch (Exception) { }
                if (on) { Life = Age + 1.2f; StopEmitAt = -1f; }
                else { Keep = null; StopEmitAt = Age; Life = Age + 1.2f; }
            }
            if (Follow != null) { _hadFollow = true; transform.position = Follow.position + FollowOffset; }
            else if (_hadFollow)
            {
                // v0.25.61: the thing it followed is gone (tornado, orb) - stop emitting and fade out
                _hadFollow = false;
                if (StopEmitAt < 0f || StopEmitAt > Age) StopEmitAt = Age;
                Life = Mathf.Min(Life, Age + 1.2f);
            }
            if (FlashLight != null)
            {
                float k = Age <= LightHold ? 1f : 1f - Mathf.Clamp01((Age - LightHold) / Mathf.Max(0.01f, Life - LightHold));
                FlashLight.intensity = LightPeak * k * k;
            }
            if (Bolts != null)
            {
                float fade = 1f - Mathf.Clamp01(Age / Mathf.Max(0.01f, Life));
                if (Time.time >= NextFlicker)
                {
                    NextFlicker = Time.time + 0.045f;
                    for (int i = 0; i < Bolts.Length; i++) DragonVfx.Jag(Bolts[i], BoltA, BoltB, BoltJitter * (i == 0 ? 1f : 0.7f));
                }
                for (int i = 0; i < Bolts.Length; i++)
                {
                    if (Bolts[i] == null) continue;
                    Color c = i == 0 ? BoltGlow : BoltCore;
                    c.a *= fade;
                    Bolts[i].startColor = c;
                    Bolts[i].endColor = c;
                }
            }
            if (!Stopped && StopEmitAt >= 0f && Age >= StopEmitAt && Systems != null)
            {
                Stopped = true;
                for (int i = 0; i < Systems.Length; i++) if (Systems[i] != null) Systems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (Age >= Life) Destroy(gameObject);
        }
    }

    public class DragonCrackFade : MonoBehaviour
    {
        public LineRenderer[] Lines;
        public Color Color;
        public float Life = 3f, Age;
        private void Update()
        {
            Age += Time.deltaTime;
            float k = Age < 0.15f ? Age / 0.15f : 1f - Mathf.Clamp01((Age - 0.15f) / Mathf.Max(0.1f, Life - 0.15f));
            if (Lines == null) return;
            for (int i = 0; i < Lines.Length; i++)
            {
                if (Lines[i] == null) continue;
                Color a = Color; a.a = k;
                Color b = Color; b.a = k * 0.3f;
                Lines[i].startColor = a;
                Lines[i].endColor = b;
            }
        }
    }

    public class DragonDebris : MonoBehaviour
    {
        public Vector3 Velocity, Spin;
        public float Life = 2f, Age;
        private bool _landed;
        private Vector3 _baseScale;
        private void Start() { _baseScale = transform.localScale; }
        private void Update()
        {
            float dt = Time.deltaTime;
            Age += dt;
            if (!_landed)
            {
                Velocity += Physics.gravity * dt;
                Vector3 next = transform.position + Velocity * dt;
                RaycastHit hit;
                if (Velocity.y < 0f && Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Max(0.2f, -Velocity.y * dt + 0.1f), LayerMask.GetMask("terrain", "Default", "static_solid", "piece"), QueryTriggerInteraction.Ignore))
                {
                    if (Velocity.y < -4f) { Velocity = new Vector3(Velocity.x * 0.4f, -Velocity.y * 0.3f, Velocity.z * 0.4f); Spin *= 0.4f; }
                    else { _landed = true; next = hit.point + Vector3.up * transform.localScale.y * 0.3f; }
                }
                transform.position = next;
                transform.Rotate(Spin * dt, Space.World);
            }
            if (Age > Life * 0.7f) transform.localScale = _baseScale * Mathf.Clamp01(1f - (Age - Life * 0.7f) / (Life * 0.3f));
            if (Age >= Life) Destroy(gameObject);
        }
    }

    // v0.25.59 VFX skin: a textured mesh (ground decal, light column) that scales and fades by vertex colour.
    public class DragonMeshFx : MonoBehaviour
    {
        public Mesh Mesh;
        public Color Color = Color.white;
        public float Life = 1f, Age, FadeIn = 0.06f, Hold;
        public Vector3 ScaleFrom = Vector3.one, ScaleTo = Vector3.one;
        public float ScaleTime = 0.3f, Spin;
        public bool Conform;   // v0.25.66: flat ground decal follows the terrain / Structure height under every vertex
        private Color[] _cols;
        private const int Cells = 16, HN = 9;
        private float[] _hf;
        private float _ext, _lift;
        private Vector3 _center;
        private Vector3[] _gv;
        private Vector3 _lastScale;
        private Quaternion _lastRot;

        private void Start()
        {
            if (!Conform || Mesh == null) return;
            try { BuildConform(); } catch (Exception) { _hf = null; }
        }

        private void BuildConform()
        {
            _center = transform.position;
            _ext = Mathf.Max(Mathf.Max(ScaleFrom.x, ScaleTo.x), Mathf.Max(ScaleFrom.z, ScaleTo.z)) * 0.5f * 1.05f;
            if (_ext < 1.2f) return;
            float cy;
            if (!DragonCombat.TryGroundY(_center, 6f, 30f, out cy)) return;
            _lift = _center.y - cy;
            // flat ground: five probes within 12 cm -> keep the plain quad
            bool flat = true;
            float[] px = { -1f, 1f, -1f, 1f }, pz = { -1f, -1f, 1f, 1f };
            for (int i = 0; i < 4 && flat; i++)
            {
                float y;
                if (DragonCombat.TryGroundY(new Vector3(_center.x + px[i] * _ext * 0.7f, cy, _center.z + pz[i] * _ext * 0.7f), 12f, 40f, out y) && Mathf.Abs(y - cy) > 0.12f) flat = false;
            }
            if (flat) return;
            _hf = new float[HN * HN];
            for (int j = 0; j < HN; j++)
                for (int i = 0; i < HN; i++)
                {
                    float x = _center.x - _ext + 2f * _ext * i / (HN - 1), z = _center.z - _ext + 2f * _ext * j / (HN - 1);
                    float y;
                    _hf[j * HN + i] = DragonCombat.TryGroundY(new Vector3(x, cy, z), 12f, 40f, out y) ? y : cy;
                }
            int n = Cells + 1;
            _gv = new Vector3[n * n];
            Vector2[] uv = new Vector2[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    _gv[j * n + i] = new Vector3(i / (float)Cells - 0.5f, 0f, j / (float)Cells - 0.5f);
                    uv[j * n + i] = new Vector2(i / (float)Cells, j / (float)Cells);
                }
            int[] tri = new int[Cells * Cells * 12];
            int k = 0;
            for (int j = 0; j < Cells; j++)
                for (int i = 0; i < Cells; i++)
                {
                    int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
                    tri[k++] = a; tri[k++] = c; tri[k++] = b; tri[k++] = b; tri[k++] = c; tri[k++] = d;
                    tri[k++] = a; tri[k++] = b; tri[k++] = c; tri[k++] = b; tri[k++] = d; tri[k++] = c;
                }
            Mesh.Clear();
            Mesh.vertices = (Vector3[])_gv.Clone();
            Mesh.uv = uv;
            Mesh.colors = new Color[n * n];
            Mesh.triangles = tri;
            _lastScale = Vector3.zero;
        }

        private float SampleH(float wx, float wz)
        {
            float fx = Mathf.Clamp((wx - _center.x + _ext) / (2f * _ext) * (HN - 1), 0f, HN - 1.001f);
            float fz = Mathf.Clamp((wz - _center.z + _ext) / (2f * _ext) * (HN - 1), 0f, HN - 1.001f);
            int ix = (int)fx, iz = (int)fz;
            float tx = fx - ix, tz = fz - iz;
            float h0 = Mathf.Lerp(_hf[iz * HN + ix], _hf[iz * HN + ix + 1], tx);
            float h1 = Mathf.Lerp(_hf[(iz + 1) * HN + ix], _hf[(iz + 1) * HN + ix + 1], tx);
            return Mathf.Lerp(h0, h1, tz);
        }

        private void UpdateConform()
        {
            Vector3 sc = transform.localScale;
            Quaternion rot = transform.rotation;
            if (sc == _lastScale && rot == _lastRot) return;
            _lastScale = sc; _lastRot = rot;
            float sy = Mathf.Max(0.01f, Mathf.Abs(sc.y));
            Vector3 pos = transform.position;
            Vector3[] v = new Vector3[_gv.Length];
            for (int i = 0; i < _gv.Length; i++)
            {
                Vector3 w = rot * new Vector3(_gv[i].x * sc.x, 0f, _gv[i].z * sc.z);
                float h = SampleH(pos.x + w.x, pos.z + w.z) + _lift;
                v[i] = new Vector3(_gv[i].x, (h - pos.y) / sy, _gv[i].z);
            }
            Mesh.vertices = v;
            Mesh.RecalculateBounds();
        }

        private void Update()
        {
            Age += Time.deltaTime;
            float t = Mathf.Clamp01(Age / Mathf.Max(0.01f, ScaleTime));
            float e = 1f - (1f - t) * (1f - t) * (1f - t);
            transform.localScale = Vector3.LerpUnclamped(ScaleFrom, ScaleTo, e);
            if (Spin != 0f) transform.Rotate(Vector3.up, Spin * Time.deltaTime, Space.Self);
            if (_hf != null && Mesh != null) UpdateConform();
            float k = Age < FadeIn ? Age / Mathf.Max(0.01f, FadeIn) : 1f - Mathf.Clamp01((Age - FadeIn - Hold) / Mathf.Max(0.01f, Life - FadeIn - Hold));
            if (Mesh != null)
            {
                if (_cols == null || _cols.Length != Mesh.vertexCount) _cols = new Color[Mesh.vertexCount];
                Color c = Color; c.a = Color.a * k;
                for (int i = 0; i < _cols.Length; i++) _cols[i] = c;
                Mesh.colors = _cols;
            }
            if (Age >= Life) Destroy(gameObject);
        }

        private void OnDestroy() { if (Mesh != null) Destroy(Mesh); }
    }

    // v0.25.59 VFX skin: a crescent ribbon of light that sweeps left -> right through an arc and fades
    // (the visible blade trail of every slash skill).
    public class DragonSlashArc : MonoBehaviour
    {
        public float Radius = 3f, Arc = 150f, Width = 0.8f, Sweep = 0.16f, Life = 0.5f, Age;
        public Color Color = Color.white;
        private Mesh _mesh;
        private Vector3[] _v;
        private Color[] _c;
        private Vector2[] _uv;
        private const int N = 28;

        public void Init(Mesh mesh)
        {
            _mesh = mesh;
            _v = new Vector3[(N + 1) * 2];
            _c = new Color[_v.Length];
            _uv = new Vector2[_v.Length];
            int[] tri = new int[N * 6];
            for (int i = 0; i < N; i++)
            {
                int a = i * 2;
                tri[i * 6] = a; tri[i * 6 + 1] = a + 2; tri[i * 6 + 2] = a + 1;
                tri[i * 6 + 3] = a + 1; tri[i * 6 + 4] = a + 2; tri[i * 6 + 5] = a + 3;
            }
            Build();
            _mesh.vertices = _v;
            _mesh.uv = _uv;
            _mesh.colors = _c;
            _mesh.triangles = tri;
            _mesh.RecalculateBounds();
        }

        private void Build()
        {
            float half = Arc * 0.5f;
            float head = Mathf.Lerp(-half, half, Mathf.Clamp01(Age / Mathf.Max(0.01f, Sweep)));
            float tail = Mathf.Max(-half, head - Arc * 0.75f);
            float fade = Age <= Sweep ? 1f : 1f - Mathf.Clamp01((Age - Sweep) / Mathf.Max(0.01f, Life - Sweep));
            for (int i = 0; i <= N; i++)
            {
                float t = (float)i / N;
                float a = Mathf.Lerp(tail, head, t) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                float w = Width * (0.25f + 0.75f * Mathf.Sin(t * Mathf.PI * 0.5f + 0.0001f));
                _v[i * 2] = dir * (Radius - w);
                _v[i * 2 + 1] = dir * Radius;
                _uv[i * 2] = new Vector2(t, 0f);
                _uv[i * 2 + 1] = new Vector2(t, 1f);
                Color c = Color; c.a = Color.a * fade * t * t;
                _c[i * 2] = c; _c[i * 2 + 1] = c;
            }
        }

        private void Update()
        {
            Age += Time.deltaTime;
            if (_mesh != null)
            {
                Build();
                _mesh.vertices = _v;
                _mesh.colors = _c;
                _mesh.RecalculateBounds();
            }
            if (Age >= Life) Destroy(gameObject);
        }

        private void OnDestroy() { if (_mesh != null) Destroy(_mesh); }
    }

    // v0.25.60: lightning that crawls along the ground (Lightning Trails). The path is the list of ground
    // points the trail has reached; every 0.05 s it is re-jagged sideways (never into the ground), the tail
    // dims, the head crackles with small arcs, and when the trail ends it fades leaving a scorched scar.
    public class DragonGroundBolt : MonoBehaviour
    {
        public LineRenderer Glow, Core, Fork;
        public Light HeadLight;
        public readonly List<Vector3> Pts = new List<Vector3>();
        public Color Color = Color.white;
        public float Width = 0.15f, MaxLife = 10f, TailLength = 1.2f;
        private float _age, _next, _fadeAge, _fadeLen = 0.45f, _nextArc;
        private bool _done;
        private Vector3[] _buf = new Vector3[0];

        public void Add(Vector3 p)
        {
            if (_done) return;
            if (Pts.Count == 0 || (Pts[Pts.Count - 1] - p).sqrMagnitude > 0.04f) Pts.Add(p);
            // v0.25.63 (user): a short bolt (~1 m) CRAWLING along the ground - the tail is cut behind the head,
            // the path is never one continuous line from the impact.
            while (Pts.Count > 2 && TailLength > 0f && PathLength() > TailLength) Pts.RemoveAt(0);
        }

        private float PathLength()
        {
            float l = 0f;
            for (int i = 1; i < Pts.Count; i++) l += Vector3.Distance(Pts[i - 1], Pts[i]);
            return l;
        }

        public void Finish(float fade)
        {
            if (_done) return;
            _done = true;
            _fadeLen = Mathf.Max(0.1f, fade);
            if (Pts.Count >= 1)
            {
                Vector3 b = Pts[Pts.Count - 1];
                Color sc = Color;
                DragonCombat.RunVfx(delegate { DragonVfx.Burst(b + Vector3.up * 0.15f, sc, 10, 4f, 0.14f, 0.3f, 0.5f); });   // fizzles out at the end
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (!_done && _age >= MaxLife) Finish(0.4f);
            if (_done)
            {
                _fadeAge += Time.deltaTime;
                if (_fadeAge >= _fadeLen) { Destroy(gameObject); return; }
            }
            if (Time.time >= _next) { _next = Time.time + 0.05f; Rebuild(); }
            float k = _done ? 1f - _fadeAge / _fadeLen : 1f;
            float flick = UnityEngine.Random.Range(0.65f, 1f) * k;
            Paint(Glow, Color, 0.55f * flick);
            Paint(Core, new Color(0.92f, 0.97f, 1f, 1f), flick);
            Paint(Fork, Color, 0.8f * flick);
            if (HeadLight != null && Pts.Count > 0)
            {
                HeadLight.transform.position = Pts[Pts.Count - 1] + Vector3.up * 0.5f;
                HeadLight.intensity = 1.6f * DragonVfx.LightScale * flick;
            }
            if (!_done && Pts.Count > 0 && Time.time >= _nextArc)
            {
                _nextArc = Time.time + UnityEngine.Random.Range(0.08f, 0.16f);
                Vector3 head = Pts[Pts.Count - 1];
                Color hc = Color;
                DragonCombat.RunVfx(delegate { DragonVfx.Burst(head + Vector3.up * 0.1f, hc, 5, 4f, 0.12f, 0.25f, 0.6f); });
            }
        }

        private static void Paint(LineRenderer l, Color c, float a)
        {
            if (l == null) return;
            Gradient g = new Gradient();
            g.SetKeys(new GradientColorKey[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new GradientAlphaKey[] { new GradientAlphaKey(a * 0.25f, 0f), new GradientAlphaKey(a * 0.8f, 0.6f), new GradientAlphaKey(a, 1f) });
            l.colorGradient = g;
        }

        private Vector3 Jit(Vector3 p, Vector3 side, float amount)
        {
            return p + side * UnityEngine.Random.Range(-amount, amount) + Vector3.up * UnityEngine.Random.Range(0.06f, 0.22f);
        }

        private void Rebuild()
        {
            int n = Pts.Count;
            if (n < 2) { if (Glow != null) Glow.positionCount = 0; if (Core != null) Core.positionCount = 0; if (Fork != null) Fork.positionCount = 0; return; }
            int m = n * 2 - 1;
            if (_buf.Length != m) _buf = new Vector3[m];
            for (int i = 0; i < n; i++)
            {
                Vector3 dir = (i < n - 1 ? Pts[i + 1] - Pts[i] : Pts[i] - Pts[i - 1]); dir.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward);
                _buf[i * 2] = Jit(Pts[i], side, Width * 1.2f);
                if (i < n - 1) _buf[i * 2 + 1] = Jit((Pts[i] + Pts[i + 1]) * 0.5f, side, Width * 1.8f);
            }
            // per-point SetPosition: SetPositions has a Span overload the game's compiler cannot resolve (v0.25.62)
            Glow.positionCount = m;
            Core.positionCount = m;
            for (int i = 0; i < m; i++) { Glow.SetPosition(i, _buf[i]); Core.SetPosition(i, _buf[i]); }
            if (Fork != null)
            {
                // one short fork off a random point
                int at = UnityEngine.Random.Range(0, m);
                Vector3 o = _buf[at];
                Vector3 d = Quaternion.Euler(0f, UnityEngine.Random.Range(-70f, 70f), 0f) * (Pts[n - 1] - Pts[0]).normalized;
                Fork.positionCount = 3;
                Fork.SetPosition(0, o);
                Fork.SetPosition(1, o + d * UnityEngine.Random.Range(0.15f, 0.3f) + Vector3.up * 0.08f + Vector3.Cross(Vector3.up, d) * UnityEngine.Random.Range(-0.12f, 0.12f));
                Fork.SetPosition(2, o + d * UnityEngine.Random.Range(0.35f, 0.55f) + Vector3.up * 0.04f);
            }
        }
    }

    // v0.25.61 signature VFX: a rock / ice spike that bursts out of the ground, holds, then sinks back.
    public class DragonSpike : MonoBehaviour
    {
        public float Height = 1f, Rise = 0.12f, Hold = 0.5f, Sink = 0.4f, Age;
        public Vector3 Base;
        private void Update()
        {
            Age += Time.deltaTime;
            float k;
            if (Age < Rise) { float t = Age / Rise; k = 1f - (1f - t) * (1f - t); }
            else if (Age < Rise + Hold) k = 1f;
            else k = 1f - Mathf.Clamp01((Age - Rise - Hold) / Mathf.Max(0.01f, Sink));
            transform.position = Base - transform.up * Height * (1f - k) * 1.05f;
            if (Age >= Rise + Hold + Sink) Destroy(gameObject);
        }
    }

    // v0.25.61: a moving wall of light / force (Impact Wave crest, wave fronts). Curved vertical strip, moved by
    // its owner; Finish() fades it out.
    public class DragonCrest : MonoBehaviour
    {
        public Mesh Mesh;
        public Color Color = Color.white;
        public float FadeLen = 0.3f, Age, MaxLife = 10f;
        private float _fade = -1f;
        private Color[] _cols;
        public void Finish(float fade) { if (_fade < 0f) { _fade = 0f; FadeLen = Mathf.Max(0.05f, fade); } }
        private void Update()
        {
            Age += Time.deltaTime;
            if (_fade < 0f && Age >= MaxLife) Finish(0.3f);
            float k = Mathf.Clamp01(Age / 0.08f);
            if (_fade >= 0f) { _fade += Time.deltaTime; k *= 1f - Mathf.Clamp01(_fade / FadeLen); if (_fade >= FadeLen) { Destroy(gameObject); return; } }
            float flick = 0.85f + 0.15f * Mathf.Sin(Age * 40f);
            if (Mesh != null)
            {
                if (_cols == null) _cols = new Color[Mesh.vertexCount];
                Color c = Color; c.a = Color.a * k * flick;
                for (int i = 0; i < _cols.Length; i++) _cols[i] = c;
                Mesh.colors = _cols;
            }
        }
        private void OnDestroy() { if (Mesh != null) Destroy(Mesh); }
    }

    // v0.25.65: holds, then fades every child mesh's vertex colours out and destroys the object (crescent flashes).
    public class DragonFadeOut : MonoBehaviour
    {
        public float Hold = 0.3f, Fade = 0.3f, Grow = 0f;
        private float _age;
        private MeshFilter[] _mf;
        private Color[][] _base;
        private LineRenderer[] _lr;
        private Color[] _lrBase;
        private Vector3 _scale;
        private void Start()
        {
            _lr = GetComponentsInChildren<LineRenderer>();
            _lrBase = new Color[_lr.Length];
            for (int i = 0; i < _lr.Length; i++) _lrBase[i] = _lr[i].startColor;
            _mf = GetComponentsInChildren<MeshFilter>();
            _base = new Color[_mf.Length][];
            for (int i = 0; i < _mf.Length; i++) _base[i] = _mf[i].sharedMesh != null ? _mf[i].sharedMesh.colors : new Color[0];
            _scale = transform.localScale;
        }
        private void Update()
        {
            _age += Time.deltaTime;
            if (Grow != 0f) transform.localScale = _scale * (1f + Grow * Mathf.Clamp01(_age / (Hold + Fade)));
            if (_age > Hold && _mf != null)
            {
                float k = 1f - Mathf.Clamp01((_age - Hold) / Mathf.Max(0.01f, Fade));
                for (int i = 0; _lr != null && i < _lr.Length; i++)
                {
                    if (_lr[i] == null) continue;
                    Color lc = _lrBase[i]; lc.a *= k;
                    _lr[i].startColor = lc; _lr[i].endColor = lc;
                }
                for (int i = 0; i < _mf.Length; i++)
                {
                    if (_mf[i] == null || _mf[i].sharedMesh == null || _base[i].Length == 0) continue;
                    Color[] c = new Color[_base[i].Length];
                    for (int j = 0; j < c.Length; j++) { c[j] = _base[i][j]; c[j].a *= k; }
                    _mf[i].sharedMesh.colors = c;
                }
            }
            if (_age >= Hold + Fade) Destroy(gameObject);
        }
    }

    // v0.25.65 Judgement Cut sphere: a faded, distorted-looking dome that pops in, holds while the cuts land,
    // then collapses inward and fades (material colour, never the shared built-in sphere mesh).
    public class DragonSphereFx : MonoBehaviour
    {
        public Renderer[] Rends;
        public Color[] Cols;
        public float Radius = 3f, PopIn = 0.08f, Hold = 0.6f, Out = 0.25f;
        private float _age;
        private void Update()
        {
            _age += Time.deltaTime;
            float s, a;
            if (_age < PopIn) { float t = _age / PopIn; s = Mathf.Lerp(0.2f, 1.05f, t); a = t; }
            else if (_age < PopIn + Hold) { s = 1.05f - 0.05f * ((_age - PopIn) / Hold); a = 1f; }
            else { float t = Mathf.Clamp01((_age - PopIn - Hold) / Out); s = Mathf.Lerp(1f, 0.6f, t); a = 1f - t; }
            transform.localScale = Vector3.one * Radius * 2f * s;
            if (Rends != null)
                for (int i = 0; i < Rends.Length; i++)
                    if (Rends[i] != null) { Color c = Cols[i]; c.a *= a; Rends[i].material.color = c; }
            if (_age >= PopIn + Hold + Out) Destroy(gameObject);
        }
    }

    // v0.25.65: a planted arrow stays, then sinks into the ground and is removed.
    public class DragonPlanted : MonoBehaviour
    {
        public float Life = 3f, Sink = 0.5f;
        private float _age;
        private Vector3 _start;
        private void Start() { _start = transform.position; }
        private void Update()
        {
            _age += Time.deltaTime;
            if (_age > Life) transform.position = _start + transform.forward * Sink * Mathf.Clamp01((_age - Life) / 0.6f);
            if (_age > Life + 0.6f) Destroy(gameObject);
        }
    }

    // Destroys a generated mesh together with its object.
    public class DragonSpinner : MonoBehaviour
    {
        public float DegPerSecond = 900f;
        private void Update() { transform.Rotate(Vector3.up, DegPerSecond * Time.deltaTime, Space.Self); }
    }

    public class DragonMeshOwner : MonoBehaviour
    {
        public Mesh Mesh;
        private void OnDestroy() { if (Mesh != null) Destroy(Mesh); }
    }

    public static class DragonVfx
    {
        private static Material _add, _alpha;
        private static Texture2D _glow;
        private static bool _catalogLogged;

        public static bool Enabled
        {
            get { return DragonCombatPlugin.Instance == null || DragonCombatPlugin.Instance.EnhancedVfx == null || DragonCombatPlugin.Instance.EnhancedVfx.Value; }
        }

        public static float Amount
        {
            get { return DragonCombatPlugin.Instance == null || DragonCombatPlugin.Instance.VfxDensity == null ? 1f : Mathf.Clamp(DragonCombatPlugin.Instance.VfxDensity.Value, 0.1f, 3f); }
        }

        private static Texture2D Glow()
        {
            if (_glow != null) return _glow;
            const int n = 64;
            _glow = new Texture2D(n, n, TextureFormat.RGBA32, false);
            _glow.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (0.55f + 0.45f * Mathf.Clamp01(1f - d * 2.2f) * 2f);
                    _glow.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            _glow.Apply();
            return _glow;
        }

        // Additive glow material (falls back to alpha-blended Sprites/Default when the game strips the shaders).
        public static Material Additive()
        {
            if (_add != null) return _add;
            string[] names = { "Legacy Shaders/Particles/Additive", "Particles/Additive", "Mobile/Particles/Additive", "Legacy Shaders/Particles/Additive (Soft)", "Sprites/Default" };
            Shader s = null;
            for (int i = 0; i < names.Length && s == null; i++) s = Shader.Find(names[i]);
            if (s == null) return null;
            _add = new Material(s);
            _add.mainTexture = Glow();
            return _add;
        }

        public static Material Alpha()
        {
            if (_alpha != null) return _alpha;
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) return Additive();
            _alpha = new Material(s);
            _alpha.mainTexture = SmokeTex();   // v0.25.59 dust / clouds are soft smoke puffs, not glow dots
            return _alpha;
        }

        private static DragonVfxLife Host(Vector3 pos, float life, string name)
        {
            GameObject go = new GameObject("IH_Vfx_" + name);
            go.transform.position = pos;
            DragonVfxLife l = go.AddComponent<DragonVfxLife>();
            l.Life = Mathf.Max(0.05f, life);
            return l;
        }

        // One configured particle system on its own child object.
        public static ParticleSystem Particles(Transform parent, Color c, int burst, float rate, float emitSeconds, float life, float speedMin, float speedMax,
            float sizeMin, float sizeMax, float gravity, ParticleSystemShapeType shape, float radius, Vector3 shapeEuler, bool stretch, bool additive)
        {
            if (additive) c.a *= 0.8f;   // v0.25.59 toned down
            GameObject go = new GameObject("ps");
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.duration = Mathf.Max(0.05f, emitSeconds);
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            Color c2 = Color.Lerp(c, Color.white, 0.45f);
            c2.a = c.a;
            main.startColor = new ParticleSystem.MinMaxGradient(c, c2);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1200;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = true;
            em.rateOverTime = rate * Amount;
            int b = Mathf.Max(0, Mathf.RoundToInt(burst * Amount));
            if (b > 0) em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)Mathf.Min(b, 30000)) });
            else em.SetBursts(new ParticleSystem.Burst[0]);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = shape;
            sh.radius = Mathf.Max(0.01f, radius);
            sh.rotation = shapeEuler;
            if (shape == ParticleSystemShapeType.Cone) sh.angle = 4f;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.25f)));
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            Material m = additive ? Additive() : Alpha();
            if (m != null) r.sharedMaterial = m;
            if (stretch)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.lengthScale = 2.5f;
                r.velocityScale = 0.06f;
            }
            ps.Play(true);
            return ps;
        }

        public static void Flash(Vector3 pos, Color c, float intensity, float range, float seconds)
        {
            if (!Enabled) return;
            intensity *= LightScale;
            if (intensity <= 0.05f) return;
            DragonVfxLife l = Host(pos, seconds, "flash");
            Light light = l.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = c;
            light.range = Mathf.Max(1f, range);
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            l.FlashLight = light;
            l.LightPeak = intensity;
            l.LightHold = seconds * 0.15f;
        }

        // Burst of glowing motes in all directions (impact sparks / holy embers).
        public static void Burst(Vector3 pos, Color c, int count, float speed, float size, float life, float gravity)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(pos, life + 0.3f, "burst");
            Particles(l.transform, c, count, 0f, 0.1f, life, speed * 0.4f, speed, size * 0.5f, size, gravity, ParticleSystemShapeType.Sphere, 0.2f, Vector3.zero, speed > 6f, true);
        }

        // Expanding ground shockwave: a ring of glow particles racing outward + an inner dust flash + light.
        public static void Shockwave(Vector3 pos, Color c, float radius, float seconds)
        {
            if (!Enabled) return;
            radius = Mathf.Max(0.5f, radius);
            seconds = Mathf.Clamp(seconds, 0.15f, 2f);
            DragonVfxLife l = Host(pos + Vector3.up * 0.15f, seconds + 0.5f, "shock");
            int n = Mathf.Clamp(Mathf.RoundToInt(radius * 12f), 30, 260);
            GroundRing(pos, c, radius, seconds);   // v0.25.59 skin
            float speed = radius / seconds;
            Particles(l.transform, c, n, 0f, 0.05f, seconds, speed * 0.85f, speed, 0.35f + radius * 0.03f, 0.7f + radius * 0.05f, 0f,
                ParticleSystemShapeType.Circle, 0.25f, new Vector3(90f, 0f, 0f), true, true);
            Color dust = Color.Lerp(c, Color.white, 0.6f);
            dust.a = 0.5f;
            Particles(l.transform, dust, Mathf.RoundToInt(n * 0.35f), 0f, 0.05f, seconds * 1.4f, speed * 0.25f, speed * 0.55f, 0.6f, 1.4f, -0.05f,
                ParticleSystemShapeType.Circle, 0.3f, new Vector3(90f, 0f, 0f), false, true);
            Flash(pos + Vector3.up * 1.2f, c, 4f + radius * 0.25f, radius * 1.6f, seconds + 0.25f);
        }

        // Column of light: rising streaks + a bright core + flash.
        public static void Pillar(Vector3 pos, Color c, float radius, float height, float seconds)
        {
            if (!Enabled) return;
            radius = Mathf.Max(0.3f, radius);
            DragonVfxLife l = Host(pos, seconds + 0.8f, "pillar");
            float life = Mathf.Clamp(seconds * 0.6f, 0.3f, 1.2f);
            float speed = Mathf.Max(4f, height / life);
            // Cone pointing up (shape +Z rotated onto +Y): streaks rise from a disc of the pillar's radius.
            Particles(l.transform, c, Mathf.RoundToInt(30 + radius * 20f), 60f * radius, seconds, life, speed * 0.6f, speed, 0.25f, 0.6f, 0f,
                ParticleSystemShapeType.Cone, radius * 0.6f, new Vector3(-90f, 0f, 0f), true, true);
            Color core = Color.Lerp(c, Color.white, 0.7f);
            Particles(l.transform, core, 6, 14f, seconds, life * 0.8f, speed * 0.9f, speed * 1.1f, radius * 1.2f, radius * 1.8f, 0f,
                ParticleSystemShapeType.Cone, radius * 0.1f, new Vector3(-90f, 0f, 0f), true, true);
            l.StopEmitAt = seconds;
            l.Systems = l.GetComponentsInChildren<ParticleSystem>();
            Flash(pos + Vector3.up * 2f, c, 6f + radius, 8f + radius * 3f, seconds + 0.4f);
            Beam(pos, c, radius, height, seconds + 0.3f);   // v0.25.59 skin
        }

        // Jagged, flickering lightning bolt (glow + core), sparks and a flash where it lands.
        public static void Bolt(Vector3 a, Vector3 b, Color glow, float width, float seconds)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(b, Mathf.Max(0.08f, seconds), "bolt");
            LineRenderer outer = BoltLine(l.transform, width * 2.6f);
            LineRenderer inner = BoltLine(l.transform, width * 0.8f);
            l.Bolts = new LineRenderer[] { outer, inner };
            l.BoltA = a; l.BoltB = b;
            l.BoltJitter = Mathf.Clamp(Vector3.Distance(a, b) * 0.06f, 0.15f, 1.4f);
            Color g = glow; g.a = 0.55f;
            l.BoltGlow = g;
            l.BoltCore = new Color(0.95f, 0.98f, 1f, 1f);
            Jag(outer, a, b, l.BoltJitter);
            Jag(inner, a, b, l.BoltJitter * 0.7f);
            Burst(b, glow, 26, 9f, 0.18f, 0.45f, 0.6f);
            Flash(b + Vector3.up * 0.8f, glow, 5f, 9f, seconds + 0.2f);
            // a couple of short branches
            for (int i = 0; i < 2; i++)
            {
                Vector3 mid = Vector3.Lerp(a, b, UnityEngine.Random.Range(0.35f, 0.75f));
                Vector3 end = mid + new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1.2f, -0.2f), UnityEngine.Random.Range(-1f, 1f)) * Vector3.Distance(a, b) * 0.18f;
                DragonVfxLife br = Host(end, Mathf.Max(0.06f, seconds * 0.6f), "branch");
                LineRenderer bl = BoltLine(br.transform, width * 0.6f);
                br.Bolts = new LineRenderer[] { bl };
                br.BoltA = mid; br.BoltB = end; br.BoltJitter = l.BoltJitter * 0.5f;
                br.BoltGlow = l.BoltCore; br.BoltCore = l.BoltCore;
                Jag(bl, mid, end, br.BoltJitter);
            }
        }

        private static LineRenderer BoltLine(Transform parent, float width)
        {
            GameObject go = new GameObject("line");
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 14;
            line.startWidth = Mathf.Max(0.02f, width);
            line.endWidth = Mathf.Max(0.02f, width * 0.7f);
            line.numCapVertices = 2;
            Material m = Mat(LineTex(), true);   // v0.25.61 soft-edged bolts
            if (m == null) m = Additive();
            if (m != null) line.sharedMaterial = m;
            return line;
        }

        public static void Jag(LineRenderer line, Vector3 a, Vector3 b, float jitter)
        {
            if (line == null) return;
            int n = line.positionCount;
            Vector3 dir = b - a;
            Vector3 side = Vector3.Cross(dir.normalized, Vector3.up);
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            side.Normalize();
            Vector3 side2 = Vector3.Cross(dir.normalized, side).normalized;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                Vector3 p = Vector3.Lerp(a, b, t);
                if (i > 0 && i < n - 1)
                {
                    float env = Mathf.Sin(t * Mathf.PI);
                    p += side * UnityEngine.Random.Range(-jitter, jitter) * env + side2 * UnityEngine.Random.Range(-jitter, jitter) * env;
                }
                line.SetPosition(i, p);
            }
        }

        // Looping motes around a point or following a transform (relics, buffs, hammers, comets).
        public static GameObject Aura(Transform follow, Vector3 pos, Color c, float radius, float seconds, float rate, float rise)
        {
            if (!Enabled) return null;
            DragonVfxLife l = Host(follow != null ? follow.position : pos, seconds + 1.2f, "aura");
            l.Follow = follow;
            Particles(l.transform, c, 0, rate, seconds, 1.0f, rise * 0.5f, rise, 0.12f, 0.35f, -0.05f,
                ParticleSystemShapeType.Sphere, Mathf.Max(0.1f, radius), Vector3.zero, false, true);
            l.StopEmitAt = seconds;
            l.Systems = l.GetComponentsInChildren<ParticleSystem>();
            return l.gameObject;
        }

        // Particle trail behind a moving transform (charges, dives, thrown hammers).
        public static GameObject Trail(Transform follow, Color c, float size, float seconds)
        {
            if (!Enabled || follow == null) return null;
            DragonVfxLife l = Host(follow.position, seconds + 1f, "trail");
            l.Follow = follow;
            l.FollowOffset = Vector3.up * 0.9f;
            Particles(l.transform, c, 0, 90f, seconds, 0.6f, 0f, 0.6f, size * 0.5f, size, 0f, ParticleSystemShapeType.Sphere, size * 0.6f, Vector3.zero, false, true);
            l.StopEmitAt = seconds;
            l.Systems = l.GetComponentsInChildren<ParticleSystem>();
            return l.gameObject;
        }

        // Trail that keeps emitting while keep() is true (charges, dives).
        public static GameObject TrailWhile(Transform follow, Color c, float size, Func<bool> keep)
        {
            GameObject go = Trail(follow, c, size, 1f);
            if (go == null) return null;
            DragonVfxLife l = go.GetComponent<DragonVfxLife>();
            ParticleSystem[] ps = go.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < ps.Length; i++) { ParticleSystem.MainModule m = ps[i].main; m.loop = true; }
            l.Keep = keep;
            return go;
        }

        // Looping glow motes + a soft light parented to an object (relic crosses, hammers); dies with it.
        public static void AttachGlow(Transform parent, Color c, float radius, float rate, float lightRange)
        {
            if (!Enabled || parent == null) return;
            GameObject go = new GameObject("IH_Glow");
            go.transform.SetParent(parent, false);
            ParticleSystem ps = Particles(go.transform, c, 0, rate, 1f, 1.1f, 0.2f, 1.2f, 0.12f, 0.4f, -0.05f,
                ParticleSystemShapeType.Sphere, Mathf.Max(0.1f, radius), Vector3.zero, false, true);
            ParticleSystem.MainModule m = ps.main;
            m.loop = true;
            if (lightRange > 0f)
            {
                Light light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = c;
                light.range = lightRange;
                light.intensity = 2.2f * LightScale;
                light.shadows = LightShadows.None;
            }
        }

        // Holy healing sparkle on a character: rising golden motes + a soft light.
        public static void Heal(Character target, Color c)
        {
            if (!Enabled || target == null) return;
            Aura(target.transform, target.transform.position, c, 0.6f, 0.6f, 60f, 1.6f);
            Flash(target.transform.position + Vector3.up, c, 2.5f, 4f, 0.6f);
        }

        // Valheim's own effect prefab (first name that exists). Only prefabs without a ZNetView are spawned.
        public static GameObject Vanilla(string[] names, Vector3 pos, Quaternion rot, float scale, float life)
        {
            if (!Enabled || names == null) return null;
            try
            {
                Type zns = DragonCombat.FindTypeCached("ZNetScene");
                if (zns == null) return null;
                object inst = null;
                PropertyInfo ip = zns.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (ip != null) inst = ip.GetValue(null, null);
                if (inst == null)
                {
                    FieldInfo f = zns.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f == null) f = zns.GetField("s_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null) inst = f.GetValue(null);
                }
                if (inst == null) return null;
                LogCatalog(inst);
                MethodInfo get = zns.GetMethod("GetPrefab", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
                if (get == null) return null;
                Type znv = DragonCombat.FindTypeCached("ZNetView");
                for (int i = 0; i < names.Length; i++)
                {
                    GameObject prefab = get.Invoke(inst, new object[] { names[i] }) as GameObject;
                    if (prefab == null) continue;
                    if (znv != null && prefab.GetComponentInChildren(znv, true) != null) continue;
                    GameObject go = UnityEngine.Object.Instantiate(prefab, pos, rot);
                    if (scale > 0f && Mathf.Abs(scale - 1f) > 0.01f) go.transform.localScale = prefab.transform.localScale * scale;
                    UnityEngine.Object.Destroy(go, Mathf.Max(0.5f, life));
                    return go;
                }
            }
            catch (Exception) { }
            return null;
        }

        // Writes every fx_/vfx_ prefab name to the log once, so effects can be picked by name later.
        private static void LogCatalog(object znetScene)
        {
            if (_catalogLogged) return;
            _catalogLogged = true;
            try
            {
                FieldInfo f = znetScene.GetType().GetField("m_prefabs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                List<GameObject> list = f == null ? null : f.GetValue(znetScene) as List<GameObject>;
                if (list == null) return;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null) continue;
                    string n = list[i].name;
                    if (n.StartsWith("fx_", StringComparison.OrdinalIgnoreCase) || n.StartsWith("vfx_", StringComparison.OrdinalIgnoreCase))
                    { if (sb.Length > 0) sb.Append(", "); sb.Append(n); }
                }
                if (DragonCombatPlugin.Instance != null) DragonCombatPlugin.Instance.LogInfo("[Immortal Heroes] Effect prefabs: " + sb.ToString());
            }
            catch (Exception) { }
        }

        // ------------------------------------------------------------------ v0.25.57 detail layer
        // Camera shake near `pos` (Valheim's own GameCamera.AddShake).
        public static void Shake(Vector3 pos, float range, float strength)
        {
            if (!Enabled) return;
            try
            {
                Type gc = DragonCombat.FindTypeCached("GameCamera");
                if (gc == null) return;
                object inst = null;
                FieldInfo f = gc.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) inst = f.GetValue(null);
                if (inst == null) { PropertyInfo pi = gc.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); if (pi != null) inst = pi.GetValue(null, null); }
                if (inst == null) return;
                MethodInfo m = gc.GetMethod("AddShake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null) return;
                ParameterInfo[] ps = m.GetParameters();
                object[] args = new object[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                {
                    Type t = ps[i].ParameterType;
                    if (t == typeof(Vector3)) args[i] = pos;
                    else if (t == typeof(float)) args[i] = i == 1 ? range : strength;
                    else if (t == typeof(bool)) args[i] = false;
                    else args[i] = t.IsValueType ? Activator.CreateInstance(t) : null;
                }
                m.Invoke(inst, args);
            }
            catch (Exception) { }
        }

        // Glowing jagged cracks radiating from an impact, fading out.
        public static void Cracks(Vector3 center, Color c, float radius, int count, float seconds)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(center, seconds, "cracks");
            List<LineRenderer> lines = new List<LineRenderer>();
            for (int i = 0; i < count; i++)
            {
                float a = (i + UnityEngine.Random.Range(-0.3f, 0.3f)) / count * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float len = radius * UnityEngine.Random.Range(0.55f, 1f);
                GameObject go = new GameObject("crack");
                go.transform.SetParent(l.transform, false);
                LineRenderer line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                int n = 9;
                line.positionCount = n;
                line.startWidth = 0.22f + radius * 0.02f;
                line.endWidth = 0.03f;
                Material m = Additive();
                if (m != null) line.sharedMaterial = m;
                Vector3 side = Vector3.Cross(Vector3.up, dir);
                for (int k = 0; k < n; k++)
                {
                    float t = (float)k / (n - 1);
                    Vector3 p = center + dir * len * t + side * (k == 0 ? 0f : UnityEngine.Random.Range(-0.35f, 0.35f) * radius * 0.12f);
                    p = GroundPoint(p);   // v0.25.66 terrain / Structures
                    line.SetPosition(k, p + Vector3.up * 0.04f);
                }
                lines.Add(line);
            }
            DragonCrackFade fade = l.gameObject.AddComponent<DragonCrackFade>();
            fade.Lines = lines.ToArray();
            fade.Color = c;
            fade.Life = seconds;
        }

        // Chunks of earth thrown out of an impact: real little rocks that arc, bounce once and sink away.
        public static void Debris(Vector3 center, Color c, int count, float speed, float size, float seconds)
        {
            if (!Enabled) return;
            Shader sh = Shader.Find("Sprites/Default");
            count = Mathf.Clamp(Mathf.RoundToInt(count * Amount), 1, 60);
            for (int i = 0; i < count; i++)
            {
                GameObject rock = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere);
                rock.name = "IH_Debris";
                Collider col = rock.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.Destroy(col);
                Renderer r = rock.GetComponent<Renderer>();
                if (r != null && sh != null)
                {
                    r.material = new Material(sh);
                    float shade = UnityEngine.Random.Range(0.7f, 1.1f);
                    r.material.color = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                float sz = size * UnityEngine.Random.Range(0.5f, 1.2f);
                rock.transform.localScale = new Vector3(sz, sz * UnityEngine.Random.Range(0.6f, 1f), sz);
                rock.transform.position = center + Vector3.up * 0.3f;
                rock.transform.rotation = UnityEngine.Random.rotation;
                DragonDebris d = rock.AddComponent<DragonDebris>();
                Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                float sp = speed * UnityEngine.Random.Range(0.4f, 1f);
                d.Velocity = new Vector3(dir.x * sp, speed * UnityEngine.Random.Range(0.6f, 1.3f), dir.y * sp);
                d.Spin = UnityEngine.Random.insideUnitSphere * 540f;
                d.Life = seconds * UnityEngine.Random.Range(0.8f, 1.2f);
            }
        }

        // Soft feathers / embers drifting down over an area.
        public static void Feathers(Vector3 center, Color c, float radius, float seconds, float rate)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(center + Vector3.up * 6f, seconds + 3f, "feathers");
            ParticleSystem ps = Particles(l.transform, c, 0, rate, seconds, 3f, 0f, 0.4f, 0.12f, 0.3f, 0.06f,
                ParticleSystemShapeType.Circle, radius, new Vector3(90f, 0f, 0f), false, false);
            SkinFeathers(ps);
            ParticleSystem.NoiseModule nz = ps.noise;
            nz.enabled = true;
            nz.strength = 0.8f;
            nz.frequency = 0.6f;
            l.StopEmitAt = seconds;
            l.Systems = new ParticleSystem[] { ps };
        }

        // A ring of dust rolling out along the ground.
        public static void DustRing(Vector3 pos, float radius)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(pos + Vector3.up * 0.2f, 2.5f, "dust");
            Color dust = new Color(0.55f, 0.48f, 0.40f, 0.55f);
            Particles(l.transform, dust, Mathf.RoundToInt(40 + radius * 10f), 0f, 0.05f, 1.6f, radius * 0.8f, radius * 1.4f, 0.8f, 1.8f, -0.03f,
                ParticleSystemShapeType.Circle, 0.5f, new Vector3(90f, 0f, 0f), false, false);
        }

        // A dark, churning storm cloud hanging over an area, flickering from inside.
        public static void StormCloud(Vector3 ground, float radius, float height, float seconds)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(ground + Vector3.up * height, seconds + 2f, "storm");
            Particles(l.transform, new Color(0.18f, 0.20f, 0.26f, 0.75f), 30, 25f, seconds, 2.5f, 0.1f, 0.6f, radius * 0.35f, radius * 0.6f, 0f,
                ParticleSystemShapeType.Circle, radius, new Vector3(90f, 0f, 0f), false, false);
            Particles(l.transform, Storm, 0, 6f, seconds, 0.25f, 0f, 0.1f, radius * 0.3f, radius * 0.5f, 0f,
                ParticleSystemShapeType.Circle, radius * 0.8f, new Vector3(90f, 0f, 0f), false, true);
            l.StopEmitAt = seconds;
            l.Systems = l.GetComponentsInChildren<ParticleSystem>();
        }

        // Bright blade streaks cutting through a sphere (Blade Storm, slash hails): thin additive lines that flash
        // and fade, each a random chord through the centre.
        public static void SlashStreaks(Vector3 center, Color c, float radius, int count, float seconds)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(center, seconds, "slashes");
            List<LineRenderer> lines = new List<LineRenderer>();
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = UnityEngine.Random.onUnitSphere;
                dir.y *= 0.5f;
                dir.Normalize();
                Vector3 off = UnityEngine.Random.insideUnitSphere * radius * 0.3f;
                GameObject go = new GameObject("slash");
                go.transform.SetParent(l.transform, false);
                LineRenderer line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 3;
                line.startWidth = 0.02f;
                line.endWidth = 0.02f;
                line.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
                line.widthMultiplier = 0.12f + radius * 0.03f;
                Material m = Additive();
                if (m != null) line.sharedMaterial = m;
                line.SetPosition(0, center + off - dir * radius);
                line.SetPosition(1, center + off);
                line.SetPosition(2, center + off + dir * radius);
                lines.Add(line);
            }
            DragonCrackFade fade = l.gameObject.AddComponent<DragonCrackFade>();
            fade.Lines = lines.ToArray();
            fade.Color = Color.Lerp(c, Color.white, 0.4f);
            fade.Life = seconds;
        }

        // A straight scar torn along the ground (wave paths, fissures, blade drags).
        public static void CrackLine(Vector3 a, Vector3 b, Color c, float width, float seconds)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(a, seconds, "scar");
            GameObject go = new GameObject("scar");
            go.transform.SetParent(l.transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            int n = Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(a, b) / 0.8f), 3, 40);
            line.positionCount = n;
            line.startWidth = width;
            line.endWidth = width * 0.3f;
            Material m = Additive();
            if (m != null) line.sharedMaterial = m;
            Vector3 dir = b - a; dir.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, dir.normalized);
            int mask = LayerMask.GetMask("terrain", "Default", "static_solid", "piece");
            for (int k = 0; k < n; k++)
            {
                float t = (float)k / (n - 1);
                Vector3 p = Vector3.Lerp(a, b, t) + side * UnityEngine.Random.Range(-0.25f, 0.25f) * width * 2f;
                p = GroundPoint(p);   // v0.25.66 terrain / Structures
                line.SetPosition(k, p + Vector3.up * 0.04f);
            }
            DragonCrackFade fade = l.gameObject.AddComponent<DragonCrackFade>();
            fade.Lines = new LineRenderer[] { line };
            fade.Color = c;
            fade.Life = seconds;
        }

        // Particles sucked inward toward a point (gravity orbs, black holes, pulls).
        public static GameObject Vortex(Transform follow, Vector3 pos, Color c, float radius, float seconds)
        {
            if (!Enabled) return null;
            DragonVfxLife l = Host(follow != null ? follow.position : pos, seconds + 1f, "vortex");
            l.Follow = follow;
            float life = 0.8f;
            ParticleSystem ps = Particles(l.transform, c, 0, 90f + radius * 20f, seconds, life, -radius / life * 1.1f, -radius / life * 0.8f, 0.1f, 0.3f, 0f,
                ParticleSystemShapeType.Sphere, radius, Vector3.zero, true, true);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.radiusThickness = 0.05f;   // spawn on the shell, fly to the centre
            l.StopEmitAt = seconds;
            l.Systems = new ParticleSystem[] { ps };
            return l.gameObject;
        }

        // Ice shards / crystals thrown out of an impact.
        public static void IceBurst(Vector3 pos, float radius)
        {
            Debris(pos, new Color(0.70f, 0.92f, 1f, 1f), Mathf.RoundToInt(8 + radius * 2f), 6f + radius * 0.5f, 0.3f, 2.2f);
            Burst(pos + Vector3.up * 0.4f, new Color(0.85f, 0.97f, 1f, 1f), Mathf.RoundToInt(40 + radius * 8f), 8f + radius, 0.3f, 0.9f, 0.6f);
            Cracks(pos, new Color(0.55f, 0.88f, 1f, 1f), radius * 0.8f, 8, 3f);
        }

        // The full "something heavy hit the ground" package.
        public static void HeavyLanding(Vector3 pos, Color c, float radius, float shake)
        {
            Shockwave(pos, c, radius, 0.5f);
            DustRing(pos, radius);
            Cracks(pos, c, radius * 0.8f, 9, 3.5f);
            Debris(pos, new Color(0.42f, 0.36f, 0.30f, 1f), Mathf.RoundToInt(10 + radius * 2f), 6f + radius * 0.6f, 0.25f + radius * 0.02f, 2.5f);
            Burst(pos + Vector3.up * 0.4f, Color.Lerp(c, Color.white, 0.5f), Mathf.RoundToInt(40 + radius * 8f), 9f + radius, 0.3f, 0.8f, 0.5f);
            Shake(pos, 25f + radius * 2f, shake);
            Scorch(pos, radius * 0.55f, 6f);   // v0.25.59 skin
            Smoke(pos, new Color(0.45f, 0.40f, 0.35f, 0.55f), radius, 2.2f, Mathf.RoundToInt(6 + radius));
        }

        // v0.25.58 ground hit without the shockwave ring (the caller already draws one): dust, cracks, rocks, sparks, shake.
        public static void GroundImpact(Vector3 pos, Color c, float radius, float shake)
        {
            DustRing(pos, radius);
            Cracks(pos, c, radius * 0.75f, 7, 3f);
            Debris(pos, new Color(0.42f, 0.36f, 0.30f, 1f), Mathf.RoundToInt(6 + radius * 1.5f), 5f + radius * 0.5f, 0.22f + radius * 0.02f, 2.2f);
            Burst(pos + Vector3.up * 0.4f, Color.Lerp(c, Color.white, 0.4f), Mathf.RoundToInt(25 + radius * 6f), 8f + radius, 0.28f, 0.7f, 0.5f);
            if (shake > 0f) Shake(pos, 20f + radius * 2f, shake);
            Scorch(pos, radius * 0.45f, 5f);   // v0.25.59 skin
            Smoke(pos, new Color(0.45f, 0.40f, 0.35f, 0.5f), radius * 0.8f, 2f, Mathf.RoundToInt(4 + radius * 0.8f));
        }

        // Rising embers / sparks over an area for a while (burning ground, auras).
        public static void Embers(Vector3 pos, Color c, float radius, float seconds, float rate)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(pos, seconds + 1.5f, "embers");
            Particles(l.transform, c, 0, rate, seconds, 1.2f, 0.6f, 2.2f, 0.1f, 0.3f, -0.3f,
                ParticleSystemShapeType.Circle, Mathf.Max(0.3f, radius), new Vector3(90f, 0f, 0f), false, true);
            l.StopEmitAt = seconds;
            l.Systems = l.GetComponentsInChildren<ParticleSystem>();
        }

        // ------------------------------------------------------------------ v0.25.59 skin layer
        // Generated textures (no asset files): soft ring, scorch, smoke puff, light column, slash ribbon, feather.
        private static Texture2D _ringTex, _scorchTex, _smokeTex, _beamTex, _streakTex, _featherTex;
        private static readonly Dictionary<int, Material> _mats = new Dictionary<int, Material>();

        public static float LightScale
        {
            get { return DragonCombatPlugin.Instance == null || DragonCombatPlugin.Instance.VfxLight == null ? 0.45f : Mathf.Clamp(DragonCombatPlugin.Instance.VfxLight.Value, 0f, 2f); }
        }

        private static float Hash(int x, int y, int seed)
        {
            int h = x * 374761393 + y * 668265263 + seed * 2147483;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }

        private static float Noise(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Mathf.Lerp(Hash(xi, yi, seed), Hash(xi + 1, yi, seed), fx);
            float b = Mathf.Lerp(Hash(xi, yi + 1, seed), Hash(xi + 1, yi + 1, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        private static float Fbm(float x, float y, int seed)
        {
            return Noise(x, y, seed) * 0.55f + Noise(x * 2.1f, y * 2.1f, seed + 7) * 0.3f + Noise(x * 4.3f, y * 4.3f, seed + 13) * 0.15f;
        }

        private delegate float TexFn(float u, float v);

        private static Texture2D MakeTex(int w, int h, TexFn fn, TextureWrapMode wrap)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, true);
            t.wrapMode = wrap;
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(fn((x + 0.5f) / w, (y + 0.5f) / h)));
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        private static float Rad(float u, float v) { float dx = u * 2f - 1f, dy = v * 2f - 1f; return Mathf.Sqrt(dx * dx + dy * dy); }

        private static Texture2D RingTex()
        {
            if (_ringTex == null) _ringTex = MakeTex(128, 128, delegate(float u, float v)
            {
                float d = Rad(u, v);
                float ring = Mathf.Exp(-Mathf.Pow((d - 0.86f) / 0.07f, 2f));
                float inner = Mathf.Clamp01(1f - d) * 0.18f * (0.6f + 0.4f * Fbm(u * 8f, v * 8f, 3));
                return d > 1f ? 0f : ring + inner;
            }, TextureWrapMode.Clamp);
            return _ringTex;
        }

        private static Texture2D ScorchTex()
        {
            if (_scorchTex == null) _scorchTex = MakeTex(128, 128, delegate(float u, float v)
            {
                float d = Rad(u, v) + (Fbm(u * 6f, v * 6f, 11) - 0.5f) * 0.45f;
                return Mathf.Pow(Mathf.Clamp01(1f - d), 0.8f) * (0.55f + 0.45f * Fbm(u * 14f, v * 14f, 5));
            }, TextureWrapMode.Clamp);
            return _scorchTex;
        }

        private static Texture2D SmokeTex()
        {
            if (_smokeTex == null) _smokeTex = MakeTex(64, 64, delegate(float u, float v)
            {
                float d = Rad(u, v);
                float n = Fbm(u * 4f, v * 4f, 21);
                return Mathf.Clamp01(1f - d * (1.15f - n * 0.5f)) * (0.35f + 0.65f * n);
            }, TextureWrapMode.Clamp);
            return _smokeTex;
        }

        private static Texture2D BeamTex()
        {
            if (_beamTex == null) _beamTex = MakeTex(64, 128, delegate(float u, float v)
            {
                float across = Mathf.Exp(-Mathf.Pow((u - 0.5f) / 0.2f, 2f)) * 0.7f + Mathf.Exp(-Mathf.Pow((u - 0.5f) / 0.06f, 2f)) * 0.5f;
                float along = Mathf.Pow(1f - v, 0.6f) * Mathf.Clamp01(v * 12f);
                return across * along * (0.8f + 0.2f * Noise(u * 6f, v * 20f, 9));
            }, TextureWrapMode.Clamp);
            return _beamTex;
        }

        private static Texture2D StreakTex()
        {
            if (_streakTex == null) _streakTex = MakeTex(64, 64, delegate(float u, float v)
            {
                // v = 0 inner edge, 1 = blade edge: bright cutting edge with a soft wake behind it
                float edge = Mathf.Exp(-Mathf.Pow((v - 0.82f) / 0.06f, 2f)) + Mathf.Exp(-Mathf.Pow((v - 0.82f) / 0.16f, 2f)) * 0.35f;
                float wake = Mathf.Clamp01(v) * 0.2f * (0.7f + 0.3f * Noise(u * 10f, v * 3f, 17));
                return Mathf.Clamp01(edge + wake);
            }, TextureWrapMode.Clamp);
            return _streakTex;
        }

        private static Texture2D FeatherTex()
        {
            if (_featherTex == null) _featherTex = MakeTex(32, 64, delegate(float u, float v)
            {
                float x = (u - 0.5f) * 2f, y = v * 2f - 1f;
                float body = Mathf.Clamp01(1f - (x * x / 0.25f + y * y));
                float spine = Mathf.Exp(-Mathf.Pow(x / 0.06f, 2f)) * 0.4f;
                return Mathf.Clamp01(body * (0.75f + 0.25f * Mathf.Abs(Mathf.Sin(y * 18f))) + spine * body);
            }, TextureWrapMode.Clamp);
            return _featherTex;
        }

        public static Material Mat(Texture2D tex, bool additive)
        {
            int key = tex.GetInstanceID() * 2 + (additive ? 1 : 0);
            Material m;
            if (_mats.TryGetValue(key, out m) && m != null) return m;
            string[] names = additive
                ? new string[] { "Legacy Shaders/Particles/Additive", "Particles/Additive", "Mobile/Particles/Additive", "Sprites/Default" }
                : new string[] { "Legacy Shaders/Particles/Alpha Blended", "Particles/Alpha Blended", "Sprites/Default" };
            Shader s = null;
            for (int i = 0; i < names.Length && s == null; i++) s = Shader.Find(names[i]);
            if (s == null) return null;
            m = new Material(s);
            m.mainTexture = tex;
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            _mats[key] = m;
            return m;
        }

        private static Mesh QuadMesh(bool flat)
        {
            Mesh m = new Mesh();
            if (flat) m.vertices = new Vector3[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f) };
            else m.vertices = new Vector3[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f) };
            m.uv = new Vector2[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            m.triangles = new int[] { 0, 2, 1, 1, 2, 3, 0, 1, 2, 1, 3, 2 };   // both faces
            m.colors = new Color[] { Color.white, Color.white, Color.white, Color.white };
            m.RecalculateBounds();
            return m;
        }

        private static DragonMeshFx MeshFx(Vector3 pos, Quaternion rot, bool flat, Material mat, Color c, float life)
        {
            if (mat == null) return null;
            GameObject go = new GameObject("IH_Skin");
            go.transform.position = pos;
            go.transform.rotation = rot;
            Mesh mesh = QuadMesh(flat);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            DragonMeshFx fx = go.AddComponent<DragonMeshFx>();
            fx.Mesh = mesh;
            fx.Color = c;
            fx.Life = Mathf.Max(0.1f, life);
            return fx;
        }

        public static Vector3 GroundPoint(Vector3 pos)
        {
            float gy;
            if (DragonCombat.TryGroundY(pos, 12f, 40f, out gy)) return new Vector3(pos.x, gy, pos.z);   // v0.25.66 terrain / Structures
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out hit, 6f, LayerMask.GetMask("terrain", "Default", "static_solid", "piece"), QueryTriggerInteraction.Ignore))
                return hit.point;
            return pos;
        }

        // Expanding glowing ring painted on the ground + a soft glow disc under the impact.
        public static void GroundRing(Vector3 pos, Color c, float radius, float seconds)
        {
            if (!Enabled) return;
            radius = Mathf.Max(0.5f, radius);
            Vector3 g = GroundPoint(pos) + Vector3.up * 0.07f;
            Quaternion yaw = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            Color rc = c; rc.a = 0.9f;
            DragonMeshFx ring = MeshFx(g, yaw, true, Mat(RingTex(), true), rc, seconds + 0.15f); if (ring != null) ring.Conform = true;
            if (ring != null) { ring.ScaleFrom = Vector3.one * radius * 0.4f; ring.ScaleTo = Vector3.one * radius * 2.05f; ring.ScaleTime = seconds; }
            Color gc = Color.Lerp(c, Color.white, 0.2f); gc.a = 0.55f;
            DragonMeshFx disc = MeshFx(g + Vector3.up * 0.01f, yaw, true, Mat(Glow(), true), gc, seconds * 0.8f + 0.1f); if (disc != null) disc.Conform = true;
            if (disc != null) { disc.ScaleFrom = Vector3.one * radius * 0.8f; disc.ScaleTo = Vector3.one * radius * 1.6f; disc.ScaleTime = seconds * 0.5f; }
        }

        // v0.25.63: a fixed-size glowing circle on the ground (area markers / telegraphs; replaces line rings).
        public static void AreaRing(Vector3 pos, Color c, float radius, float seconds)
        {
            if (!Enabled) return;
            radius = Mathf.Max(0.3f, radius);
            Vector3 g = GroundPoint(pos) + Vector3.up * 0.07f;
            Color rc = c; rc.a = Mathf.Clamp01(c.a) * 0.85f;
            DragonMeshFx ring = MeshFx(g, Quaternion.identity, true, Mat(RingTex(), true), rc, Mathf.Max(0.15f, seconds)); if (ring != null) ring.Conform = true;
            if (ring != null) { ring.FadeIn = Mathf.Min(0.12f, seconds * 0.3f); ring.Hold = seconds * 0.5f; ring.ScaleFrom = ring.ScaleTo = Vector3.one * radius * 2.05f; }
        }

        // Burnt / broken ground that lingers after a heavy hit.
        public static void Scorch(Vector3 pos, float radius, float seconds)
        {
            if (!Enabled) return;
            Vector3 g = GroundPoint(pos) + Vector3.up * 0.05f;
            DragonMeshFx s = MeshFx(g, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), true, Mat(ScorchTex(), false), new Color(0.08f, 0.06f, 0.05f, 0.75f), seconds); if (s != null) s.Conform = true;
            if (s != null) { s.FadeIn = 0.05f; s.Hold = seconds * 0.6f; s.ScaleFrom = Vector3.one * radius * 1.6f; s.ScaleTo = Vector3.one * radius * 2f; s.ScaleTime = 0.25f; }
        }

        // Solid-looking column of light: two crossed textured planes (pillars, sky strikes, pulls).
        public static void Beam(Vector3 pos, Color c, float radius, float height, float seconds)
        {
            if (!Enabled) return;
            Color bc = Color.Lerp(c, Color.white, 0.25f); bc.a = 0.85f;
            for (int i = 0; i < 2; i++)
            {
                DragonMeshFx b = MeshFx(pos, Quaternion.Euler(0f, i * 90f + 20f, 0f), false, Mat(BeamTex(), true), bc, seconds);
                if (b == null) continue;
                b.FadeIn = 0.05f;
                b.Hold = seconds * 0.25f;
                b.ScaleFrom = new Vector3(radius * 2.4f, height * 0.6f, 1f);
                b.ScaleTo = new Vector3(radius * 1.1f, height, 1f);
                b.ScaleTime = seconds * 0.6f;
            }
        }

        // Rolling smoke / dust puffs (alpha blended, slow, rising).
        public static void Smoke(Vector3 pos, Color c, float radius, float seconds, int count)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(pos + Vector3.up * 0.3f, seconds + 2.5f, "smoke");
            ParticleSystem ps = Particles(l.transform, c, count, 0f, 0.1f, seconds, 0.4f, 1.2f + radius * 0.2f, 0.9f + radius * 0.25f, 1.8f + radius * 0.4f, -0.04f,
                ParticleSystemShapeType.Sphere, Mathf.Max(0.3f, radius * 0.5f), Vector3.zero, false, false);
            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            Material m = Mat(SmokeTex(), false);
            if (m != null) r.sharedMaterial = m;
            ParticleSystem.MainModule main = ps.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.4f)));
        }

        // The visible blade trail of a slash: a crescent ribbon sweeping left -> right. roll = tilt of the arc
        // around the forward axis (0 = horizontal, 90 = vertical, negative = the other diagonal).
        public static void SlashArc(Vector3 center, Vector3 forward, float radius, float arcDeg, Color c, float width, float seconds, float roll)
        {
            if (!Enabled) return;
            Vector3 f = forward; f.y = 0f;
            if (f.sqrMagnitude < 0.001f) f = Vector3.forward;
            GameObject go = new GameObject("IH_SlashArc");
            go.transform.position = center;
            go.transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up) * Quaternion.AngleAxis(roll, Vector3.forward);
            Mesh mesh = new Mesh();
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mat(StreakTex(), true);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObject body = new GameObject("body");
            body.transform.SetParent(go.transform, false);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer br = body.AddComponent<MeshRenderer>();
            br.sharedMaterial = Mat(StreakTex(), false);   // v0.25.65 opaque-ish body so the arc reads in daylight
            br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            DragonSlashArc a = go.AddComponent<DragonSlashArc>();
            a.Radius = Mathf.Max(0.5f, radius);
            a.Arc = Mathf.Clamp(arcDeg, 20f, 360f);
            a.Width = Mathf.Max(0.15f, width);
            Color cc = Color.Lerp(c, Color.white, 0.3f); cc.a = 1f;
            a.Color = cc;
            a.Life = Mathf.Max(0.2f, seconds);
            a.Sweep = Mathf.Min(0.18f, a.Life * 0.4f);
            a.Init(mesh);
        }

        private static void SkinFeathers(ParticleSystem ps)
        {
            if (ps == null) return;
            Material m = Mat(FeatherTex(), false);
            if (m != null) ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
            ParticleSystem.MainModule main = ps.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        }

        private static Texture2D _lineTex;

        // Soft line across its width, flat along its length (ribbons drawn with LineRenderers).
        private static Texture2D LineTex()
        {
            if (_lineTex == null) _lineTex = MakeTex(16, 32, delegate(float u, float v)
            {
                return Mathf.Exp(-Mathf.Pow((v - 0.5f) / 0.22f, 2f)) * 0.75f + Mathf.Exp(-Mathf.Pow((v - 0.5f) / 0.07f, 2f)) * 0.4f;
            }, TextureWrapMode.Clamp);
            return _lineTex;
        }

        private static LineRenderer SoftLine(Transform parent, float width)
        {
            GameObject go = new GameObject("line");
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.widthMultiplier = Mathf.Max(0.02f, width);
            line.numCapVertices = 2;
            line.numCornerVertices = 1;
            line.textureMode = LineTextureMode.Stretch;
            Material m = Mat(LineTex(), true);
            if (m != null) line.sharedMaterial = m;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }

        // Lightning crawling along the ground; feed it points with Add() as the trail advances, then Finish().
        public static DragonGroundBolt GroundLightning(Color c, float width, float maxLife)
        {
            if (!Enabled) return null;
            GameObject go = new GameObject("IH_GroundBolt");
            DragonGroundBolt b = go.AddComponent<DragonGroundBolt>();
            b.Color = c;
            b.Width = Mathf.Max(0.05f, width);
            b.MaxLife = Mathf.Max(0.5f, maxLife);
            b.Glow = SoftLine(go.transform, width * 3f);
            b.Core = SoftLine(go.transform, width * 1.4f);
            b.Fork = SoftLine(go.transform, width * 1.6f);
            if (LightScale > 0.05f)
            {
                GameObject lg = new GameObject("head");
                lg.transform.SetParent(go.transform, false);
                Light l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = c;
                l.range = 3f;
                l.intensity = 0f;
                l.shadows = LightShadows.None;
                b.HeadLight = l;
            }
            return b;
        }

        // ------------------------------------------------------------------ v0.25.61 signature layer
        private static Texture2D _flameTex;
        private static readonly Dictionary<int, Material> _solidMats = new Dictionary<int, Material>();
        public static readonly Color Rock = new Color(0.36f, 0.31f, 0.27f, 1f);
        public static readonly Color Ice = new Color(0.62f, 0.90f, 1f, 0.85f);
        public static readonly Color Arcane = new Color(0.72f, 0.36f, 1f, 1f);
        public static readonly Color Ember = new Color(1f, 0.55f, 0.20f, 1f);
        public static readonly Color Steel = new Color(0.55f, 0.82f, 1f, 1f);
        public static readonly Color Wind = new Color(0.62f, 1f, 0.85f, 1f);

        // Flame tongue: bright bottom, ragged top (fire particles).
        private static Texture2D FlameTex()
        {
            if (_flameTex == null) _flameTex = MakeTex(64, 64, delegate(float u, float v)
            {
                float x = (u - 0.5f) * 2f;
                float w = Mathf.Lerp(0.85f, 0.15f, v) * (0.8f + 0.4f * Fbm(u * 5f, v * 3f, 31));
                float body = Mathf.Clamp01(1f - Mathf.Abs(x) / Mathf.Max(0.05f, w));
                return body * Mathf.Clamp01(v * 6f) * Mathf.Pow(1f - v, 0.5f);
            }, TextureWrapMode.Clamp);
            return _flameTex;
        }

        // Solid (lit when possible) material for rocks / ice.
        public static Material SolidMat(Color c)
        {
            int key = Mathf.RoundToInt(c.r * 255f) | (Mathf.RoundToInt(c.g * 255f) << 8) | (Mathf.RoundToInt(c.b * 255f) << 16) | (Mathf.RoundToInt(c.a * 15f) << 24);
            Material m;
            if (_solidMats.TryGetValue(key, out m) && m != null) return m;
            bool clear = c.a < 0.99f;
            Shader s = clear ? Shader.Find("Sprites/Default") : Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s == null) return null;
            m = new Material(s);
            m.color = c;
            if (!clear && m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            _solidMats[key] = m;
            return m;
        }

        // A jagged spike bursting out of the ground (rock or ice), tilted outward from `away`.
        public static void Spike(Vector3 pos, Color c, float height, float radius, float hold, Vector3 away)
        {
            if (!Enabled) return;
            Vector3 g = GroundPoint(pos);
            Vector3 tilt = away; tilt.y = 0f;
            Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            if (tilt.sqrMagnitude > 0.001f) rot = Quaternion.AngleAxis(UnityEngine.Random.Range(10f, 28f), Vector3.Cross(Vector3.up, tilt.normalized)) * rot;
            else rot = Quaternion.Euler(UnityEngine.Random.Range(-12f, 12f), 0f, UnityEngine.Random.Range(-12f, 12f)) * rot;
            GameObject root = new GameObject("IH_Spike");
            root.transform.position = g;
            root.transform.rotation = rot;
            Material mat = SolidMat(c);
            for (int i = 0; i < 2; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Collider col = shard.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.Destroy(col);
                shard.transform.SetParent(root.transform, false);
                float h = height * (i == 0 ? 1f : 0.55f);
                float r = radius * (i == 0 ? 1f : 0.6f);
                // a cube rotated 45 deg and stretched reads as a faceted shard; the second is a smaller leaning chip
                shard.transform.localScale = new Vector3(r, h, r * 0.8f);
                shard.transform.localRotation = Quaternion.Euler(i == 0 ? 0f : 18f, 45f + i * 30f, i == 0 ? 6f : -14f);
                shard.transform.localPosition = new Vector3(i == 0 ? 0f : r * 0.7f, h * 0.45f, 0f);
                Renderer rr = shard.GetComponent<Renderer>();
                if (rr != null && mat != null) { rr.sharedMaterial = mat; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            }
            root.transform.position = g - root.transform.up * height * 1.05f;   // starts buried, bursts out
            DragonSpike sp = root.AddComponent<DragonSpike>();
            sp.Base = g;
            sp.Height = height;
            sp.Hold = Mathf.Max(0f, hold);
            if (c.a < 0.99f) AttachGlow(root.transform, new Color(0.75f, 0.95f, 1f, 1f), radius * 0.6f, 10f, 0f);
        }

        // A ring of spikes around a point (Frost Nova, Stonefang, landings).
        public static void SpikeRing(Vector3 center, Color c, float radius, int count, float height, float hold)
        {
            if (!Enabled) return;
            count = Mathf.Max(3, Mathf.RoundToInt(count * Mathf.Clamp(Amount, 0.5f, 1.5f)));
            for (int i = 0; i < count; i++)
            {
                float a = (i + UnityEngine.Random.Range(-0.3f, 0.3f)) / count * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float rr = radius * UnityEngine.Random.Range(0.55f, 1f);
                Spike(center + d * rr, c, height * UnityEngine.Random.Range(0.6f, 1.1f), height * 0.28f, hold, d);
            }
        }

        // A vertical ring of force facing `normal` (punches, launches, sonic booms).
        public static void AirRing(Vector3 pos, Vector3 normal, Color c, float radius, float seconds)
        {
            if (!Enabled) return;
            if (normal.sqrMagnitude < 0.001f) normal = Vector3.forward;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal.normalized);
            Color rc = c; rc.a = 0.85f;
            DragonMeshFx ring = MeshFx(pos, rot, true, Mat(RingTex(), true), rc, seconds);
            if (ring != null) { ring.ScaleFrom = Vector3.one * radius * 0.3f; ring.ScaleTo = Vector3.one * radius * 2f; ring.ScaleTime = seconds; }
        }

        // Rotating rune circle on the ground (Clockwork, Gravity Dominion, Rupture, traps).
        public static void Glyph(Vector3 pos, Color c, float radius, float seconds, float spin)
        {
            if (!Enabled) return;
            Vector3 g = GroundPoint(pos) + Vector3.up * 0.08f;
            Color gc = c; gc.a = 0.8f;
            DragonMeshFx a = MeshFx(g, Quaternion.identity, true, Mat(RingTex(), true), gc, seconds); if (a != null) a.Conform = true;
            if (a != null) { a.FadeIn = 0.2f; a.Hold = seconds * 0.6f; a.ScaleFrom = Vector3.one * radius * 1.7f; a.ScaleTo = Vector3.one * radius * 2.05f; a.ScaleTime = 0.3f; a.Spin = spin; }
            DragonMeshFx b = MeshFx(g + Vector3.up * 0.01f, Quaternion.identity, true, Mat(RingTex(), true), gc, seconds); if (b != null) b.Conform = true;
            if (b != null) { b.FadeIn = 0.25f; b.Hold = seconds * 0.6f; b.ScaleFrom = Vector3.one * radius * 1.1f; b.ScaleTo = Vector3.one * radius * 1.25f; b.ScaleTime = 0.3f; b.Spin = -spin * 1.6f; }
            Color dc = c; dc.a = 0.3f;
            DragonMeshFx d = MeshFx(g + Vector3.up * 0.02f, Quaternion.identity, true, Mat(Glow(), true), dc, seconds); if (d != null) d.Conform = true;
            if (d != null) { d.FadeIn = 0.2f; d.Hold = seconds * 0.6f; d.ScaleFrom = d.ScaleTo = Vector3.one * radius * 2f; }
        }

        // Soft glowing streak a -> b that fades (arrows, rails, blades from the sky).
        public static void Streak(Vector3 a, Vector3 b, Color c, float width, float seconds)
        {
            if (!Enabled) return;
            DragonVfxLife l = Host(a, Mathf.Max(0.05f, seconds), "streak");
            LineRenderer glow = SoftLine(l.transform, width * 3.5f);
            LineRenderer core = SoftLine(l.transform, width);
            glow.positionCount = 2; glow.SetPosition(0, a); glow.SetPosition(1, b);
            core.positionCount = 2; core.SetPosition(0, a); core.SetPosition(1, b);
            l.gameObject.AddComponent<DragonCrackFade>().Lines = new LineRenderer[] { glow, core };
            DragonCrackFade f = l.GetComponent<DragonCrackFade>();
            f.Color = Color.Lerp(c, Color.white, 0.25f);
            f.Life = seconds;
        }

        // Thick energy rail with rings of force along it (Railcannon, beams).
        public static void Rail(Vector3 a, Vector3 b, Color c, float width, float seconds)
        {
            if (!Enabled) return;
            Streak(a, b, c, width, seconds);
            Streak(a, b, Color.white, width * 0.35f, seconds * 0.8f);
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.5f) return;
            int n = Mathf.Clamp(Mathf.RoundToInt(len / 4f), 1, 8);
            for (int i = 1; i <= n; i++) AirRing(a + d * (i / (n + 1f)), d, c, width * 1.6f, 0.3f + i * 0.03f);
        }

        // Real flames rushing out in a cone.
        public static void FlameCone(Vector3 origin, Vector3 dir, float range, float angle, float seconds)
        {
            if (!Enabled) return;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            DragonVfxLife l = Host(origin, seconds + 1.2f, "flames");
            l.transform.rotation = Quaternion.LookRotation(dir.normalized);
            float life = 0.55f;
            float speed = range / life;
            ParticleSystem ps = Particles(l.transform, new Color(1f, 0.62f, 0.18f, 1f), 0, 260f, seconds, life, speed * 0.7f, speed, 0.6f, 1.3f + range * 0.06f, -0.15f,
                ParticleSystemShapeType.Cone, 0.25f, Vector3.zero, false, true);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.angle = Mathf.Clamp(angle * 0.5f, 5f, 80f);
            FireLook(ps);
            l.StopEmitAt = seconds;
            l.Systems = new ParticleSystem[] { ps };
            Smoke(origin + dir.normalized * range * 0.6f, new Color(0.25f, 0.22f, 0.2f, 0.45f), range * 0.3f, 1.6f, 6);
        }

        private static void FireLook(ParticleSystem ps)
        {
            Material m = Mat(FlameTex(), true);
            if (m != null) ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
            ParticleSystem.MainModule main = ps.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            Gradient g = new Gradient();
            g.SetKeys(new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.12f), 0.35f), new GradientColorKey(new Color(0.75f, 0.15f, 0.05f), 0.8f), new GradientColorKey(new Color(0.2f, 0.1f, 0.08f), 1f) },
                      new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.6f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.3f)));
        }

        // A ball of fire: flame core, smoke column, embers, scorch.
        public static void FireBlast(Vector3 pos, float radius)
        {
            if (!Enabled) return;
            radius = Mathf.Max(0.8f, radius);
            DragonVfxLife l = Host(pos + Vector3.up * 0.4f, 2.5f, "fireblast");
            ParticleSystem ps = Particles(l.transform, new Color(1f, 0.6f, 0.2f, 1f), Mathf.RoundToInt(40 + radius * 10f), 0f, 0.1f, 0.7f, radius * 0.8f, radius * 1.6f, radius * 0.4f, radius * 0.9f, -0.3f,
                ParticleSystemShapeType.Sphere, radius * 0.3f, Vector3.zero, false, true);
            FireLook(ps);
            Smoke(pos + Vector3.up * radius * 0.5f, new Color(0.18f, 0.15f, 0.13f, 0.6f), radius, 2.6f, Mathf.RoundToInt(8 + radius * 1.5f));
            Embers(pos, Fire, radius * 0.8f, 1.2f, 80f);
            Scorch(pos, radius * 0.6f, 7f);
        }

        // A vertical curved wall of force that the caller moves (Impact Wave crest). width = across the path.
        public static DragonCrest Crest(Color c, float width, float height, float maxLife)
        {
            if (!Enabled) return null;
            GameObject go = new GameObject("IH_Crest");
            Mesh m = new Mesh();
            const int n = 12;
            Vector3[] v = new Vector3[(n + 1) * 2];
            Vector2[] uv = new Vector2[v.Length];
            int[] tri = new int[n * 12];
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                float x = (t - 0.5f) * width;
                float z = -Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 2f) * width * 0.25f;   // edges trail behind the centre
                float h = height * (0.55f + 0.45f * Mathf.Sin(t * Mathf.PI));
                v[i * 2] = new Vector3(x, 0f, z);
                v[i * 2 + 1] = new Vector3(x, h, z - height * 0.15f);
                uv[i * 2] = new Vector2(t, 0f);
                uv[i * 2 + 1] = new Vector2(t, 1f);
            }
            for (int i = 0; i < n; i++)
            {
                int a = i * 2, k = i * 12;
                tri[k] = a; tri[k + 1] = a + 1; tri[k + 2] = a + 2; tri[k + 3] = a + 2; tri[k + 4] = a + 1; tri[k + 5] = a + 3;
                tri[k + 6] = a; tri[k + 7] = a + 2; tri[k + 8] = a + 1; tri[k + 9] = a + 2; tri[k + 10] = a + 3; tri[k + 11] = a + 1;
            }
            m.vertices = v; m.uv = uv; m.triangles = tri;
            m.colors = new Color[v.Length];
            m.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = m;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mat(BeamTex(), true);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            DragonCrest cr = go.AddComponent<DragonCrest>();
            cr.Mesh = m;
            cr.Color = Color.Lerp(c, Color.white, 0.2f);
            cr.MaxLife = Mathf.Max(0.3f, maxLife);
            return cr;
        }

        // A static crescent blade of light parented to a moving object (Moonlight / Crescent / Halfmoon waves).
        // roll 90 = vertical blade.
        public static void CrescentBlade(Transform parent, Color c, float radius, float width, float roll)
        {
            if (!Enabled || parent == null) return;
            // v0.25.67 (user: "do you know what a Getsuga Tenshou is?"): a solid crescent wave of energy.
            // v0.25.69 (user: sleek, sharp, dangerous; the cutting edge must face where it travels): the crescent lies in the
            // plane of the cut with its convex edge LEADING (tips swept back), tilted 25 deg toward the camera so it reads
            // from behind; thin razor body, tight halo, white cutting edge on the leading side.
            // roll 0 = horizontal slash, roll 90 = vertical slash (two blades 14 deg apart so it reads from behind too).
            float span = Mathf.Max(1f, radius * 1.9f);
            float thick = Mathf.Max(0.2f, width * 0.85f);
            Color halo = Color.Lerp(c, new Color(0.40f, 0.78f, 1f, 1f), 0.6f); halo.a = 0.35f;
            Color body = Color.Lerp(c, new Color(0.62f, 0.88f, 1f, 1f), 0.5f); body.a = 0.92f;
            Color edge = Color.Lerp(c, Color.white, 0.92f); edge.a = 1f;
            // v0.25.77 (user: "stop giving me the facing-the-camera shit"): the blade lies EXACTLY in the plane of its cut,
            // convex edge leading in the travel direction - no tilt toward the camera, no second blade. Vertical slashes
            // (Crescent Cleave) are one slightly thicker blade. Wake trails stream back from the apex and both tips.
            bool vertical = Mathf.Abs(roll) >= 60f;
            if (vertical) thick *= 1.35f;
            Quaternion q = Quaternion.AngleAxis(roll, Vector3.forward) * Quaternion.Euler(90f, 0f, 0f);
            GetsugaLayer(parent, halo, span * 1.03f, thick * 1.5f, q, 0f, Mat(LineTex(), true));
            GetsugaLayer(parent, body, span, thick, q, 0f, Mat(BladeTex(), false));
            GetsugaLayer(parent, edge, span * 0.98f, thick * 0.26f, q, thick * 0.32f, Mat(BladeTex(), true));
            float w = span * 0.5f, sag = span * 0.24f;
            Color wake = Color.Lerp(c, new Color(0.55f, 0.85f, 1f, 1f), 0.5f); wake.a = 0.55f;
            BladeWake(parent, q * new Vector3(0f, sag * 0.5f, 0f), wake, thick * 1.1f);
            BladeWake(parent, q * new Vector3(-w * 0.92f, -sag * 0.35f, -span * 0.03f), wake, thick * 0.45f);
            BladeWake(parent, q * new Vector3(w * 0.92f, -sag * 0.35f, -span * 0.03f), wake, thick * 0.45f);
        }

        // A short glowing wake left behind a moving blade point (only shows while the carrier travels).
        private static void BladeWake(Transform parent, Vector3 local, Color c, float width)
        {
            Material m = Mat(LineTex(), true);
            if (m == null) return;
            GameObject go = new GameObject("IH_BladeWake");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            TrailRenderer tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.28f;
            tr.minVertexDistance = 0.08f;
            tr.widthMultiplier = Mathf.Max(0.05f, width);
            AnimationCurve wc = new AnimationCurve();
            wc.AddKey(0f, 1f); wc.AddKey(1f, 0f);
            tr.widthCurve = wc;
            Gradient g = new Gradient();
            Color c0 = c; Color c1 = c; c1.a = 0f;
            g.SetKeys(new GradientColorKey[] { new GradientColorKey(c0, 0f), new GradientColorKey(c1, 1f) },
                      new GradientAlphaKey[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
            tr.sharedMaterial = m;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.numCapVertices = 2;
        }

        private static Texture2D _bladeTex;
        private static Texture2D BladeTex()
        {
            // v = 0 inner (concave) edge, 1 = outer cutting edge: solid body with crisp edges and a brighter rim.
            if (_bladeTex == null) _bladeTex = MakeTex(16, 64, delegate(float u, float v)
            {
                // v0.25.69 sharp: hard edges, brightest along the cutting (outer) edge
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v / 0.06f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - v) / 0.035f));
                return Mathf.Clamp01(a * (0.7f + 0.3f * v * v));
            }, TextureWrapMode.Clamp);
            return _bladeTex;
        }

        private static void GetsugaLayer(Transform parent, Color col, float span, float thick, Quaternion rot, float outerShift, Material mat)
        {
            if (mat == null) return;
            GameObject go = new GameObject("IH_Getsuga");
            go.transform.SetParent(parent, false);
            go.transform.localRotation = rot;
            const int n = 40;
            float w = span * 0.5f, sag = span * 0.24f, sweep = span * 0.04f;
            Vector3[] v = new Vector3[(n + 1) * 2];
            Vector2[] uv = new Vector2[v.Length];
            Color[] cols = new Color[v.Length];
            for (int i = 0; i <= n; i++)
            {
                float t = Mathf.Lerp(-1f, 1f, (float)i / n);
                float y = sag * (1f - t * t) - sag * 0.5f;
                Vector2 nrm = new Vector2(2f * sag * t, w).normalized;   // outward (convex side) normal in the blade plane
                float th = thick * Mathf.Pow(Mathf.Max(0f, 1f - t * t), 1.1f) + thick * 0.01f;
                float z = -sweep * t * t;
                Vector3 mid = new Vector3(w * t, y, z) + new Vector3(nrm.x, nrm.y, 0f) * outerShift;
                v[i * 2] = mid - new Vector3(nrm.x, nrm.y, 0f) * th * 0.35f;
                v[i * 2 + 1] = mid + new Vector3(nrm.x, nrm.y, 0f) * th * 0.65f;
                float u = (float)i / n;
                uv[i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = new Vector2(u, 1f);
                Color k = col; k.a = col.a * (1f - Mathf.Pow(Mathf.Abs(t), 6f));
                cols[i * 2] = k; cols[i * 2 + 1] = k;
            }
            int[] tri = new int[n * 12];
            for (int i = 0; i < n; i++)
            {
                int a = i * 2, k = i * 12;
                tri[k] = a; tri[k + 1] = a + 2; tri[k + 2] = a + 1; tri[k + 3] = a + 1; tri[k + 4] = a + 2; tri[k + 5] = a + 3;
                tri[k + 6] = a; tri[k + 7] = a + 1; tri[k + 8] = a + 2; tri[k + 9] = a + 1; tri[k + 10] = a + 3; tri[k + 11] = a + 2;
            }
            Mesh mesh = new Mesh();
            mesh.vertices = v; mesh.uv = uv; mesh.colors = cols; mesh.triangles = tri;
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.AddComponent<DragonMeshOwner>().Mesh = mesh;
        }

        private static void CrescentArc(Transform parent, Color col, float radius, float width, float roll, float span, Quaternion plane, bool additive)
        {
            CrescentArc(parent, col, radius, width, roll, span, plane, additive, 0.75f);
        }

        private static void CrescentArc(Transform parent, Color col, float radius, float width, float roll, float span, Quaternion plane, bool additive, float back)
        {
            GameObject go = new GameObject("IH_CrescentArc");
            go.transform.SetParent(parent, false);
            go.transform.localRotation = plane * Quaternion.AngleAxis(roll, Vector3.forward);
            go.transform.localPosition = go.transform.localRotation * (Vector3.back * radius * back);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            const int n = 24;
            line.positionCount = n + 1;
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(-span, span, (float)i / n) * Mathf.Deg2Rad;
                line.SetPosition(i, new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius);
            }
            AnimationCurve curve = new AnimationCurve();
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f;
                curve.AddKey(t, Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.7f) + 0.04f);
            }
            line.widthCurve = curve;
            line.widthMultiplier = Mathf.Max(0.05f, width);
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            Material m = Mat(LineTex(), additive);
            if (m != null) line.sharedMaterial = m;
            line.startColor = col;
            line.endColor = col;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        // v0.25.66 (user: Whirlwind needs an obvious circular slash): two glowing blade crescents sweeping around
        // the spinner at chest height, one full turn every turnSeconds, for the whole channel; fade at the end.
        public static GameObject SpinSlash(Transform follow, Color c, float radius, float width, float seconds, float turnSeconds)
        {
            if (!Enabled || follow == null) return null;
            GameObject go = new GameObject("IH_SpinSlash");
            go.transform.SetParent(follow, false);
            go.transform.localPosition = Vector3.up * 1.0f;
            Color glow = c; glow.a = 0.85f;
            Color body = Color.Lerp(c, Color.white, 0.35f); body.a = 0.9f;
            Color core = Color.Lerp(c, Color.white, 0.85f); core.a = 1f;
            for (int k = 0; k < 2; k++)
            {
                Quaternion half = Quaternion.Euler(0f, k * 180f, 0f);
                CrescentArc(go.transform, glow, radius, width * 3.5f, 0f, 75f, half, true, 0f);
                CrescentArc(go.transform, body, radius, width * 1.8f, 0f, 70f, half, false, 0f);
                CrescentArc(go.transform, core, radius, width * 0.7f, 0f, 66f, half, true, 0f);
            }
            DragonSpinner sp = go.AddComponent<DragonSpinner>();
            sp.DegPerSecond = 360f / Mathf.Max(0.1f, turnSeconds);
            DragonFadeOut fo = go.AddComponent<DragonFadeOut>();
            fo.Hold = Mathf.Max(0.05f, seconds); fo.Fade = 0.25f;
            return go;
        }

        // Vergil's Judgement Cut: a pale, faded sphere over the area (outer veil + darker core), cuts flash inside.
        public static void JudgementSphere(Vector3 center, float radius, float hold)
        {
            if (!Enabled) return;
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) return;
            GameObject root = new GameObject("IH_JudgementSphere");
            root.transform.position = center;
            Color[] cols = { new Color(0.62f, 0.78f, 1f, 0.22f), new Color(0.08f, 0.10f, 0.20f, 0.28f) };
            float[] sizes = { 1f, 0.82f };
            Renderer[] rends = new Renderer[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Collider c = s.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.Destroy(c);
                s.transform.SetParent(root.transform, false);
                s.transform.localScale = Vector3.one * sizes[i];
                Renderer r = s.GetComponent<Renderer>();
                r.material = new Material(sh);
                r.material.color = cols[i];
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rends[i] = r;
            }
            DragonSphereFx fx = root.AddComponent<DragonSphereFx>();
            fx.Rends = rends; fx.Cols = cols; fx.Radius = Mathf.Max(0.5f, radius); fx.Hold = Mathf.Max(0.1f, hold);
            // the rim glows faintly + a soft light inside
            Color rim = new Color(0.70f, 0.85f, 1f, 0.6f);
            DragonMeshFx ring = MeshFx(center, Quaternion.LookRotation(Vector3.up), true, Mat(RingTex(), true), rim, hold + 0.35f);
            if (ring != null) { ring.ScaleFrom = Vector3.one * radius * 2.2f; ring.ScaleTo = Vector3.one * radius * 2.05f; ring.ScaleTime = 0.1f; }
            Flash(center, new Color(0.55f, 0.75f, 1f, 1f), 3f, radius * 2f, hold + 0.3f);
        }

        // A standing crescent that appears, holds and fades (big swings: Halfmoon, Eclipse afterimage slashes).
        public static GameObject CrescentFlash(Vector3 pos, Vector3 forward, Color c, float radius, float width, float roll, float hold, float fade, float grow)
        {
            if (!Enabled) return null;
            Vector3 f = forward; if (f.sqrMagnitude < 0.001f) f = Vector3.forward;
            GameObject go = new GameObject("IH_CrescentFlash");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
            CrescentBlade(go.transform, c, radius, width, roll);
            DragonFadeOut fo = go.AddComponent<DragonFadeOut>();
            fo.Hold = hold; fo.Fade = fade; fo.Grow = grow;
            return go;
        }

        private static void CrescentLayer(Transform parent, Color col, float radius, float width, float roll, float span, Quaternion plane, bool additive)
        {
            GameObject go = new GameObject("IH_CrescentBlade");
            go.transform.SetParent(parent, false);
            go.transform.localRotation = plane * Quaternion.AngleAxis(roll, Vector3.forward);
            go.transform.localPosition = go.transform.localRotation * (Vector3.back * radius * 0.75f);
            Mesh mesh = new Mesh();
            const int n = 32;
            Vector3[] v = new Vector3[(n + 1) * 2];
            Vector2[] uv = new Vector2[v.Length];
            Color[] cols = new Color[v.Length];
            int[] tri = new int[n * 12];
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                float a = Mathf.Lerp(-span, span, t) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                // crescent: thickest in the middle, needle-thin tips; the band is centred on the arc
                float w = width * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.8f) + width * 0.04f;
                v[i * 2] = d * (radius - w * 0.5f);
                v[i * 2 + 1] = d * (radius + w * 0.5f);
                uv[i * 2] = new Vector2(t, 0f); uv[i * 2 + 1] = new Vector2(t, 1f);
                Color k = col; k.a = col.a * Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 1.6f);
                cols[i * 2] = k; cols[i * 2 + 1] = k;
            }
            for (int i = 0; i < n; i++)
            {
                int a = i * 2, k = i * 12;
                tri[k] = a; tri[k + 1] = a + 2; tri[k + 2] = a + 1; tri[k + 3] = a + 1; tri[k + 4] = a + 2; tri[k + 5] = a + 3;
                tri[k + 6] = a; tri[k + 7] = a + 1; tri[k + 8] = a + 2; tri[k + 9] = a + 1; tri[k + 10] = a + 3; tri[k + 11] = a + 2;
            }
            mesh.vertices = v; mesh.uv = uv; mesh.colors = cols; mesh.triangles = tri;
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mat(LineTex(), additive);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<DragonMeshOwner>().Mesh = mesh;
        }

        // ------------------------------------------------------------------ v0.25.65 planted arrows + wind dome
        private static readonly Queue<GameObject> _planted = new Queue<GameObject>();

        // A real arrow stuck in the ground (Ranger volleys / big shots): wooden shaft, steel head, white fletching,
        // driven in along its flight direction with the head buried. Big arrows (length >= 2.5) also glow.
        public static GameObject PlantedArrow(Vector3 ground, Vector3 dir, float length, Color glow, float life)
        {
            if (!Enabled) return null;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.down;
            dir.Normalize();
            if (dir.y > -0.25f) { dir.y = -0.25f; dir.Normalize(); }   // always angled INTO the ground
            length = Mathf.Max(0.5f, length);
            while (_planted.Count > 0 && (_planted.Count >= 90 || _planted.Peek() == null)) { GameObject old = _planted.Dequeue(); if (old != null) UnityEngine.Object.Destroy(old); }
            GameObject root = new GameObject("IH_PlantedArrow");
            root.transform.position = ground + dir * length * 0.28f;
            root.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 90f));
            float s = length;
            Material wood = SolidMat(new Color(0.40f, 0.27f, 0.15f, 1f));
            Material steel = SolidMat(new Color(0.55f, 0.56f, 0.60f, 1f));
            Material fletch = SolidMat(new Color(0.88f, 0.88f, 0.82f, 1f));
            ArrowPart(root.transform, PrimitiveType.Cylinder, wood, new Vector3(0f, 0f, -s * 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.028f * s, s * 0.5f, 0.028f * s));
            ArrowPart(root.transform, PrimitiveType.Cube, steel, new Vector3(0f, 0f, -0.07f * s), Quaternion.Euler(0f, 0f, 45f), new Vector3(0.06f * s, 0.06f * s, 0.16f * s));
            ArrowPart(root.transform, PrimitiveType.Cube, fletch, new Vector3(0f, 0f, -s * 0.9f), Quaternion.identity, new Vector3(0.006f * s, 0.075f * s, 0.16f * s));
            ArrowPart(root.transform, PrimitiveType.Cube, fletch, new Vector3(0f, 0f, -s * 0.9f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.006f * s, 0.075f * s, 0.16f * s));
            if (length >= 2.5f)
            {
                AttachGlow(root.transform, glow, length * 0.12f, 14f, 0f);
                Burst(ground + Vector3.up * 0.3f, glow, 30, 6f, 0.25f, 0.5f, 0.5f);
                DustRing(ground, length * 0.5f);
                Cracks(ground, glow, length * 0.45f, 6, 3f);
            }
            else Burst(ground + Vector3.up * 0.1f, new Color(0.5f, 0.44f, 0.36f, 0.6f), 5, 2f, 0.25f, 0.4f, 0.6f);   // a puff of dirt
            DragonPlanted p = root.AddComponent<DragonPlanted>();
            p.Life = Mathf.Max(0.5f, life);
            p.Sink = length * 0.6f;
            _planted.Enqueue(root);
            return root;
        }

        private static void ArrowPart(Transform root, PrimitiveType type, Material m, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            Collider c = g.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.Destroy(c);
            g.transform.SetParent(root, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = rot;
            g.transform.localScale = scale;
            Renderer r = g.GetComponent<Renderer>();
            if (r != null && m != null) { r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        }

        // Neji's Rotation for Furious Winds: a spinning dome of green wind (veil + tilted crescent rings).
        public static void WindDome(Transform parent, Color c, float radius)
        {
            if (!Enabled || parent == null) return;
            Shader sh = Shader.Find("Sprites/Default");
            if (sh != null)
            {
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Collider col = s.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.Destroy(col);
                s.transform.SetParent(parent, false);
                s.transform.localPosition = Vector3.up * radius * 0.15f;
                s.transform.localScale = new Vector3(radius * 2f, radius * 1.5f, radius * 2f);
                Renderer r = s.GetComponent<Renderer>();
                r.material = new Material(sh);
                Color veil = c; veil.a = 0.16f;
                r.material.color = veil;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Color body = c; body.a = 0.7f;
            Color core = Color.Lerp(c, Color.white, 0.6f); core.a = 1f;
            float[] tilts = { 0f, 35f, -35f, 70f };
            for (int i = 0; i < tilts.Length; i++)
            {
                GameObject ring = new GameObject("windRing");
                ring.transform.SetParent(parent, false);
                ring.transform.localPosition = Vector3.up * (0.4f + i * 0.35f);
                ring.transform.localRotation = Quaternion.Euler(tilts[i], i * 47f, 0f);
                CrescentLayer(ring.transform, body, radius * (0.85f - i * 0.08f), radius * 0.12f, 0f, 170f, Quaternion.identity, false);
                CrescentLayer(ring.transform, core, radius * (0.85f - i * 0.08f), radius * 0.04f, 0f, 165f, Quaternion.identity, true);
            }
        }

        // ------------------------------------------------------------------ themed presets
        // ------------------------------------------------------------------ v0.25.72 universal VFX layer
        // (user: "go all out with the VFX"): every skill now gets, on top of its own effects,
        //  - HitImpact: a hit spark on every enemy a skill hits, shaped by the hit's main damage type,
        //  - CastFlare: an activation rune circle under the caster when a skill really starts (bigger for
        //    Ultimates / Graces, with a light column and a short shake),
        //  - WeaponCharge: the weapon hand glows in the Class colour through every wind up / hold.
        private static readonly Dictionary<int, float> _impactNext = new Dictionary<int, float>();

        public static Color ClassColor(Player p)
        {
            string cls = p == null ? "" : DragonCombat.GetClassName(p), ac = p == null ? "" : DragonCombat.GetAdvancementName(p);
            switch (ac)
            {
                case "Sword Master": return new Color(0.30f, 0.62f, 1f, 1f);
                case "Mercenary": return new Color(1f, 0.45f, 0.14f, 1f);
                case "Paladin": return new Color(1f, 0.55f, 0.66f, 1f);
                case "Priest": return new Color(0.35f, 0.95f, 0.62f, 1f);
                case "Wizard": return new Color(0.45f, 0.52f, 1f, 1f);
                case "Spellcaster": return new Color(0.25f, 0.92f, 0.95f, 1f);
                case "Acrobat": return new Color(0.45f, 1f, 0.75f, 1f);
                case "Bowmaster": return new Color(1f, 0.72f, 0.28f, 1f);
            }
            switch (cls)
            {
                case "Warrior": return new Color(1f, 0.30f, 0.26f, 1f);
                case "Cleric": return new Color(1f, 0.84f, 0.42f, 1f);
                case "Sorcerer": return new Color(0.72f, 0.40f, 1f, 1f);
                case "Ranger": return new Color(0.55f, 0.90f, 0.35f, 1f);
            }
            return new Color(0.85f, 0.85f, 0.9f, 1f);
        }

        public static void HitImpact(Character target, HitData hit)
        {
            if (!Enabled || target == null || hit == null) return;
            int id = target.GetInstanceID();
            float next;
            if (_impactNext.TryGetValue(id, out next) && Time.time < next) return;
            if (_impactNext.Count > 400) _impactNext.Clear();
            _impactNext[id] = Time.time + 0.15f;
            float sl = hit.m_damage.m_slash, bl = hit.m_damage.m_blunt, pi = hit.m_damage.m_pierce, fi = hit.m_damage.m_fire,
                  fr = hit.m_damage.m_frost, li = hit.m_damage.m_lightning, po = hit.m_damage.m_poison, sp = hit.m_damage.m_spirit;
            float total = Mathf.Max(0.01f, bl + sl + pi + fi + fr + li + po + sp);
            if (total < 0.5f) return;
            Vector3 pos = target.transform.position + Vector3.up * 1.0f;
            float size = Mathf.Clamp(Mathf.Sqrt(total) * 0.12f, 0.5f, 2.2f);   // bigger hits, bigger sparks
            float best = sl; int kind = 0;
            if (bl > best) { best = bl; kind = 1; }
            if (pi > best) { best = pi; kind = 2; }
            if (fi > best) { best = fi; kind = 3; }
            if (fr > best) { best = fr; kind = 4; }
            if (li > best) { best = li; kind = 5; }
            if (po > best) { best = po; kind = 6; }
            if (sp > best) { best = sp; kind = 7; }
            int n = Mathf.RoundToInt(12f * size * Amount);
            switch (kind)
            {
                case 0:   // slash: white-hot cut streaks
                    SlashStreaks(pos, new Color(1f, 0.95f, 0.85f, 1f), 0.7f * size, 2, 0.18f);
                    Burst(pos, new Color(1f, 0.9f, 0.7f, 1f), n, 7f, 0.12f, 0.35f, 0.6f);
                    break;
                case 1:   // blunt: dust puff + rock chips
                    Burst(pos, new Color(0.85f, 0.78f, 0.65f, 1f), n, 4f, 0.25f, 0.5f, 0.8f);
                    Smoke(pos - Vector3.up * 0.5f, new Color(0.55f, 0.5f, 0.45f, 0.45f), 0.6f * size, 0.8f, 4);
                    if (size > 1.4f) Shake(pos, 12f, 0.4f);
                    break;
                case 2:   // pierce: a short sharp spray straight through
                    Burst(pos, new Color(1f, 0.95f, 0.8f, 1f), n, 10f, 0.08f, 0.25f, 0.2f);
                    break;
                case 3:   // fire: ember spray + flare
                    Burst(pos, new Color(1f, 0.55f, 0.15f, 1f), n + 6, 6f, 0.2f, 0.6f, -0.4f);
                    Flash(pos, new Color(1f, 0.5f, 0.2f, 1f), 1.6f, 3f + size, 0.25f);
                    break;
                case 4:   // frost: crystal shards
                    Burst(pos, new Color(0.8f, 0.95f, 1f, 1f), n, 6f, 0.15f, 0.5f, 1.2f);
                    Debris(pos, new Color(0.7f, 0.92f, 1f, 1f), Mathf.Max(2, Mathf.RoundToInt(2 * size)), 4f, 0.08f, 1.2f);
                    break;
                case 5:   // lightning: arcs jumping off the target
                    for (int i = 0; i < 2; i++)
                    {
                        Vector3 o = UnityEngine.Random.onUnitSphere * (0.9f + size * 0.4f);
                        Bolt(pos, pos + o, new Color(0.55f, 0.9f, 1f, 1f), 0.06f, 0.15f);
                    }
                    Flash(pos, new Color(0.55f, 0.85f, 1f, 1f), 1.8f, 3f + size, 0.15f);
                    break;
                case 6:   // poison: green bubbling puff
                    Smoke(pos, new Color(0.45f, 0.85f, 0.3f, 0.5f), 0.5f * size, 1f, 5);
                    Burst(pos, new Color(0.6f, 1f, 0.35f, 1f), n, 2.5f, 0.15f, 0.8f, -0.3f);
                    break;
                default:  // spirit: holy motes rising + soft ring
                    Burst(pos, new Color(1f, 0.93f, 0.7f, 1f), n, 4f, 0.15f, 0.7f, -0.6f);
                    Flash(pos, new Color(1f, 0.9f, 0.6f, 1f), 1.4f, 3f + size, 0.2f);
                    break;
            }
        }

        // v0.25.73 STATUS VISUALS: what a debuff/buff does is visible on the character for as long as it lasts.
        // One aura per (character, kind) at a time; re-applying while it still shows does not stack new ones.
        private static readonly Dictionary<string, float> _statusUntil = new Dictionary<string, float>();

        private static float BodyHeight(Character t)
        {
            float h = 1.8f;
            try { Collider col = t.GetCollider(); if (col != null) h = Mathf.Clamp(col.bounds.size.y, 0.6f, 8f); } catch (Exception) { }
            return h;
        }

        private static bool StatusGate(Character t, string kind, float seconds)
        {
            if (!Enabled || t == null || t.IsDead()) return false;
            string key = t.GetInstanceID().ToString() + kind;
            float until;
            if (_statusUntil.TryGetValue(key, out until) && Time.time < until) return false;
            if (_statusUntil.Count > 600) _statusUntil.Clear();
            _statusUntil[key] = Time.time + Mathf.Max(0.2f, seconds * 0.85f);
            return true;
        }

        private static void StatusAura(Character t, Color c, float seconds, float heightFrac, float radius, float rate, float rise)
        {
            GameObject go = Aura(t.transform, t.transform.position, c, radius, seconds, rate * Amount, rise);
            if (go == null) return;
            DragonVfxLife l = go.GetComponent<DragonVfxLife>();
            if (l != null) l.FollowOffset = Vector3.up * BodyHeight(t) * heightFrac;
        }

        // kind: stun, frost, freeze, expose, cripple, fireburn, spiritburn
        public static void Status(Character t, string kind, float seconds)
        {
            if (!Enabled || t == null) return;
            seconds = Mathf.Clamp(seconds, 0.3f, 8f);
            float h = BodyHeight(t);
            float w = Mathf.Clamp(h * 0.3f, 0.35f, 2.5f);
            switch (kind)
            {
                case "stun":
                    if (!StatusGate(t, kind, 1.4f)) return;
                    // dazed halo of golden stars circling over the head
                    StatusAura(t, new Color(1f, 0.92f, 0.45f, 1f), 1.4f, 1.08f, w * 0.7f, 40f, 0.05f);
                    Burst(t.transform.position + Vector3.up * h * 1.05f, new Color(1f, 0.95f, 0.6f, 1f), Mathf.RoundToInt(10 * Amount), 2.5f, 0.12f, 0.4f, 0.2f);
                    break;
                case "frost":
                case "freeze":
                    if (!StatusGate(t, "frost", seconds)) return;
                    // rime coat: icy motes clinging to the body, snow drifting down; a freeze also cracks out a shell
                    StatusAura(t, new Color(0.75f, 0.93f, 1f, 1f), seconds, 0.5f, w, 26f, -0.25f);
                    if (kind == "freeze")
                    {
                        IceBurst(t.transform.position, Mathf.Clamp(w * 1.4f, 0.8f, 3f));
                        Flash(t.transform.position + Vector3.up * h * 0.5f, new Color(0.6f, 0.9f, 1f, 1f), 1.6f, 4f, 0.4f);
                    }
                    break;
                case "expose":
                    if (!StatusGate(t, kind, seconds)) return;
                    // broken guard: crimson shards falling off the chest
                    StatusAura(t, new Color(1f, 0.25f, 0.2f, 1f), seconds, 0.6f, w * 0.8f, 14f, -0.6f);
                    Burst(t.transform.position + Vector3.up * h * 0.6f, new Color(1f, 0.35f, 0.25f, 1f), Mathf.RoundToInt(12 * Amount), 3.5f, 0.14f, 0.45f, 1f);
                    break;
                case "cripple":
                    if (!StatusGate(t, kind, seconds)) return;
                    // heavy legs: dark violet haze around the feet
                    StatusAura(t, new Color(0.55f, 0.3f, 0.75f, 1f), seconds, 0.08f, w, 18f, 0.15f);
                    break;
                case "judgement":
                    if (!StatusGate(t, kind, seconds)) return;
                    // Judgement Mark: a rose-gold sigil of motes hovering over the head
                    StatusAura(t, new Color(1f, 0.6f, 0.7f, 1f), seconds, 1.15f, w * 0.5f, 22f, 0.1f);
                    break;
                case "sanctified":
                    if (!StatusGate(t, kind, seconds)) return;
                    // Sanctified ally: soft emerald-gold motes rising around the body
                    StatusAura(t, new Color(0.65f, 1f, 0.7f, 1f), seconds, 0.45f, w, 14f, 0.7f);
                    break;
                case "fireburn":
                    if (!StatusGate(t, kind, 0.5f)) return;
                    Embers(t.transform.position + Vector3.up * h * 0.4f, new Color(1f, 0.5f, 0.12f, 1f), w, 0.55f, 40f * Amount);
                    Flash(t.transform.position + Vector3.up * h * 0.5f, new Color(1f, 0.45f, 0.15f, 1f), 0.9f, 2.5f + w, 0.25f);
                    break;
                case "spiritburn":
                    if (!StatusGate(t, kind, 0.5f)) return;
                    Embers(t.transform.position + Vector3.up * h * 0.4f, new Color(1f, 0.95f, 0.75f, 1f), w, 0.55f, 34f * Amount);
                    Flash(t.transform.position + Vector3.up * h * 0.5f, new Color(1f, 0.9f, 0.6f, 1f), 0.8f, 2.5f + w, 0.25f);
                    break;
            }
        }

        // Hyper Armor: a steady golden shimmer on the player while it lasts.
        public static void HyperShimmer(Player p, float seconds)
        {
            if (!Enabled || p == null || !StatusGate(p, "hyper", seconds)) return;
            StatusAura(p, new Color(1f, 0.82f, 0.35f, 1f), Mathf.Min(seconds, 20f), 0.55f, 0.45f, 12f, 0.5f);
        }

        // ------------------------------------------------------------------ v0.25.75 solid spell objects
        private static Texture2D _whiteTex;
        private static Texture2D WhiteTex()
        {
            if (_whiteTex == null) _whiteTex = MakeTex(4, 4, delegate(float u, float v) { return 1f; }, TextureWrapMode.Clamp);
            return _whiteTex;
        }

        // Flat-shaded mesh: every triangle gets its own vertices, coloured by a fixed key light so facets read
        // even with the unlit Sprites/Default shader (vertex colours carry the shading).
        private static Mesh FacetMesh(List<Vector3> v, List<int> tri, Color c, float rim)
        {
            Vector3 key = new Vector3(0.35f, 0.8f, -0.45f).normalized;
            Vector3[] vv = new Vector3[tri.Count];
            Color[] cc = new Color[tri.Count];
            int[] tt = new int[tri.Count];
            Vector2[] uv = new Vector2[tri.Count];
            for (int i = 0; i + 2 < tri.Count; i += 3)
            {
                Vector3 a = v[tri[i]], b = v[tri[i + 1]], d = v[tri[i + 2]];
                Vector3 n = Vector3.Cross(b - a, d - a).normalized;
                float lit = 0.45f + 0.55f * Mathf.Max(0f, Vector3.Dot(n, key)) + rim * Mathf.Pow(1f - Mathf.Abs(n.y), 3f);
                Color fc = new Color(Mathf.Clamp01(c.r * lit), Mathf.Clamp01(c.g * lit), Mathf.Clamp01(c.b * lit), c.a);
                for (int k = 0; k < 3; k++) { vv[i + k] = v[tri[i + k]]; cc[i + k] = fc; tt[i + k] = i + k; uv[i + k] = new Vector2(0.5f, 0.5f); }
            }
            Mesh m = new Mesh();
            m.vertices = vv; m.colors = cc; m.triangles = tt; m.uv = uv;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static GameObject MeshObject(string name, Mesh mesh, Material mat, Transform parent)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        private static Material _clearMat;
        private static Material ClearMat()
        {
            if (_clearMat == null) { Shader s = Shader.Find("Sprites/Default"); if (s != null) _clearMat = new Material(s); }
            return _clearMat;
        }

        // Cut-gem diamond (crown + girdle + pavilion), point DOWN. Size = girdle diameter.
        private static Mesh DiamondMesh(float height, float width, Color c)
        {
            List<Vector3> v = new List<Vector3>();
            List<int> t = new List<int>();
            int n = 8;
            float rG = width * 0.5f, rC = width * 0.3f, yC = height * 0.26f, yT = height * 0.3f, yB = -height * 0.7f;
            v.Add(new Vector3(0f, yT, 0f));   // 0 table centre
            for (int i = 0; i < n; i++) { float a = (i + 0.5f) / n * Mathf.PI * 2f; v.Add(new Vector3(Mathf.Cos(a) * rC, yC, Mathf.Sin(a) * rC)); }   // 1..8 crown
            for (int i = 0; i < n; i++) { float a = (float)i / n * Mathf.PI * 2f; v.Add(new Vector3(Mathf.Cos(a) * rG, 0f, Mathf.Sin(a) * rG)); }   // 9..16 girdle
            v.Add(new Vector3(0f, yB, 0f));   // 17 culet
            for (int i = 0; i < n; i++)
            {
                int c0 = 1 + i, c1 = 1 + (i + 1) % n, g0 = 9 + i, g1 = 9 + (i + 1) % n;
                t.Add(0); t.Add(c1); t.Add(c0);                       // table
                t.Add(c0); t.Add(g1); t.Add(g0);                      // crown facets
                t.Add(c0); t.Add(c1); t.Add(g1);
                t.Add(17); t.Add(g0); t.Add(g1);                      // pavilion
            }
            // double-sided: add the reversed triangles so the inner facets show through the translucent ice
            int count = t.Count;
            for (int i = 0; i < count; i += 3) { t.Add(t[i]); t.Add(t[i + 2]); t.Add(t[i + 1]); }
            return FacetMesh(v, t, c, 0.35f);
        }

        // Glacial Descent: a huge cut diamond of ice, point down, spinning slowly, frost streaming behind it.
        public static GameObject IceDiamond(Vector3 pos, float size)
        {
            if (!Enabled) return null;
            GameObject root = new GameObject("IH_IceDiamond");
            root.transform.position = pos;
            float h = size * 1.35f, w = size * 0.8f;
            MeshObject("body", DiamondMesh(h, w, new Color(0.70f, 0.93f, 1f, 0.78f)), ClearMat(), root.transform);
            GameObject core = MeshObject("core", DiamondMesh(h * 0.55f, w * 0.55f, new Color(0.85f, 0.97f, 1f, 0.55f)), Mat(WhiteTex(), true), root.transform);
            core.transform.localPosition = new Vector3(0f, -h * 0.05f, 0f);
            DragonRotate rot = root.AddComponent<DragonRotate>();
            rot.Speed = new Vector3(0f, 55f, 0f);
            AttachGlow(root.transform, new Color(0.70f, 0.94f, 1f, 1f), w * 0.45f, 50f, Mathf.Max(6f, size * 1.6f));
            Transform tr = root.transform;
            GameObject keep = root;
            TrailWhile(tr, new Color(0.80f, 0.96f, 1f, 1f), Mathf.Max(0.5f, w * 0.35f), delegate { return keep != null; });
            return root;
        }

        // The diamond hits: it shatters into ice shards.
        public static void ShatterIce(Vector3 pos, float size)
        {
            if (!Enabled) return;
            Debris(pos + Vector3.up * 0.6f, new Color(0.72f, 0.94f, 1f, 1f), Mathf.RoundToInt(16 * Mathf.Clamp(Amount, 0.5f, 1.5f)), 6f + size, Mathf.Clamp(size * 0.09f, 0.18f, 0.6f), 1.8f);
            Burst(pos + Vector3.up * 0.8f, new Color(0.85f, 0.98f, 1f, 1f), Mathf.RoundToInt(50 * Amount), 7f + size, 0.25f, 0.8f, 0.9f);
            Flash(pos + Vector3.up, new Color(0.65f, 0.92f, 1f, 1f), 4f, 6f + size * 2f, 0.4f);
        }

        // Gravity Dominion: a black hole hanging over the well - a pitch-black core, a shimmering event-horizon
        // rim and two tilted accretion disks spiralling around it.
        public static GameObject BlackHole(Vector3 ground, float radius, float seconds)
        {
            if (!Enabled) return null;
            float core = Mathf.Clamp(radius * 0.14f, 0.7f, 2.2f);
            GameObject root = new GameObject("IH_BlackHole");
            root.transform.position = ground + Vector3.up * (core * 1.9f + 0.4f);
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider col = ball.GetComponent<Collider>(); if (col != null) UnityEngine.Object.Destroy(col);
            ball.transform.SetParent(root.transform, false);
            ball.transform.localScale = Vector3.one * core * 2f;
            Renderer br = ball.GetComponent<Renderer>();
            Material dark = SolidMat(new Color(0.01f, 0f, 0.02f, 1f));
            if (br != null && dark != null) { br.sharedMaterial = dark; br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            for (int d = 0; d < 2; d++)
            {
                GameObject disk = MeshObject("disk", DiskMesh(core * 1.2f, core * (d == 0 ? 3.2f : 2.4f), d), Mat(WhiteTex(), true), root.transform);
                disk.transform.localRotation = Quaternion.Euler(d == 0 ? 18f : -28f, 0f, d == 0 ? 6f : 14f);
                DragonRotate r = disk.AddComponent<DragonRotate>();
                r.Speed = new Vector3(0f, d == 0 ? 160f : -220f, 0f);
            }
            // event horizon: purple-white motes clinging to the surface
            ParticleSystem rim = Particles(root.transform, new Color(0.75f, 0.45f, 1f, 1f), 0, 80f * Amount, seconds, 0.35f, 0f, 0.15f, core * 0.12f, core * 0.25f, 0f,
                ParticleSystemShapeType.Sphere, core * 1.05f, Vector3.zero, false, true);
            ParticleSystem.ShapeModule sh = rim.shape; sh.radiusThickness = 0f;
            Light l = root.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(0.6f, 0.25f, 1f, 1f); l.range = radius * 1.2f; l.intensity = 2.4f * LightScale; l.shadows = LightShadows.None;
            DragonBlackHole bh = root.AddComponent<DragonBlackHole>();
            bh.Life = seconds;
            return root;
        }

        // Accretion disk: annulus, hot white-orange inside, violet outside, broken into swirling bands.
        private static Mesh DiskMesh(float r0, float r1, int seed)
        {
            int n = 64, rings = 4;
            List<Vector3> v = new List<Vector3>();
            List<Color> cs = new List<Color>();
            List<int> t = new List<int>();
            for (int j = 0; j <= rings; j++)
            {
                float f = (float)j / rings;
                float r = Mathf.Lerp(r0, r1, f);
                for (int i = 0; i <= n; i++)
                {
                    float a = (float)i / n * Mathf.PI * 2f + f * 1.6f;
                    v.Add(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                    float band = 0.55f + 0.45f * Mathf.Sin(i * 0.9f + seed * 2.1f + j * 1.7f) * Mathf.Sin(i * 0.23f + seed);
                    Color hot = new Color(1f, 0.85f, 0.6f, 1f), cold = new Color(0.55f, 0.2f, 1f, 1f);
                    Color c = Color.Lerp(hot, cold, Mathf.Pow(f, 0.6f));
                    c.a = Mathf.Clamp01(band * (1f - f) * (j == 0 ? 0.3f : 1f) * 0.9f);
                    cs.Add(c);
                }
            }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = j * (n + 1) + i, b = a + 1, c = a + n + 1, d = c + 1;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                    t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
                }
            Mesh m = new Mesh();
            m.vertices = v.ToArray(); m.colors = cs.ToArray(); m.triangles = t.ToArray();
            Vector2[] uv = new Vector2[v.Count]; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            m.uv = uv;
            m.RecalculateBounds();
            return m;
        }

        // Astral Greatblade: a giant blade of starlight grows OUT OF THE STAFF (the staff is its hilt), follows the
        // hand through the lift and the slam, then fades. Blade along the held item's axis.
        private static FieldInfo _bladeVisRight;
        public static GameObject StaffBlade(Player p, float length, float width, Color c, float life, float grow)
        {
            if (!Enabled || p == null) return null;
            Animator an = p.GetComponentInChildren<Animator>();
            Transform hand = an == null ? null : an.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null) return null;
            Vector3 dir = p.transform.up, tip = hand.position + p.transform.up * 0.9f;
            try
            {
                Component vis = null;
                Component[] comps = p.GetComponentsInChildren<Component>();
                for (int i = 0; i < comps.Length; i++) if (comps[i] != null && comps[i].GetType().Name == "VisEquipment") { vis = comps[i]; break; }
                if (vis != null && _bladeVisRight == null) _bladeVisRight = vis.GetType().GetField("m_rightItemInstance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                GameObject item = vis == null || _bladeVisRight == null ? null : _bladeVisRight.GetValue(vis) as GameObject;
                if (item != null)
                {
                    Renderer[] rs = item.GetComponentsInChildren<Renderer>();
                    bool any = false; Bounds b = new Bounds();
                    for (int i = 0; i < rs.Length; i++)
                    {
                        if (rs[i] == null || rs[i] is ParticleSystemRenderer) continue;
                        if (!any) { b = rs[i].bounds; any = true; } else b.Encapsulate(rs[i].bounds);
                    }
                    if (any)
                    {
                        Vector3 off = b.center - hand.position;
                        if (off.magnitude > 0.1f)
                        {
                            dir = off.normalized;
                            tip = hand.position + dir * Mathf.Clamp(off.magnitude * 2f, 0.3f, 2.5f);
                        }
                    }
                }
            }
            catch (Exception) { }
            GameObject root = new GameObject("IH_StaffBlade");
            root.transform.position = tip;
            Vector3 upRef = Mathf.Abs(Vector3.Dot(dir, p.transform.right)) > 0.9f ? p.transform.forward : p.transform.right;
            root.transform.rotation = Quaternion.LookRotation(dir, upRef);
            root.transform.SetParent(hand, true);
            GameObject scaler = new GameObject("grow");
            scaler.transform.SetParent(root.transform, false);
            Color body = c; body.a = 0.72f;
            MeshObject("blade", BladeMesh(length, width, width * 0.13f, body, false), ClearMat(), scaler.transform);
            Color halo = Color.Lerp(c, Color.white, 0.35f); halo.a = 0.4f;
            MeshObject("halo", BladeMesh(length * 1.03f, width * 1.35f, width * 0.3f, halo, true), Mat(WhiteTex(), true), scaler.transform);
            // guard: a bar of light where the staff becomes the blade
            GameObject guard = MeshObject("guard", BladeMesh(width * 1.8f, width * 0.35f, width * 0.2f, halo, true), Mat(WhiteTex(), true), root.transform);
            guard.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            guard.transform.localPosition = new Vector3(-width * 0.9f, 0f, 0f);
            for (int k = 1; k <= 3; k++)
            {
                GameObject g = new GameObject("glow");
                g.transform.SetParent(scaler.transform, false);
                g.transform.localPosition = new Vector3(0f, 0f, length * (k * 0.27f));
                AttachGlow(g.transform, Color.Lerp(c, Color.white, 0.4f), width * 0.5f, 18f, k == 2 ? Mathf.Max(5f, length * 0.6f) : 0f);
            }
            Burst(tip, Color.Lerp(c, Color.white, 0.4f), Mathf.RoundToInt(30 * Amount), 4f, 0.15f, 0.5f, -0.5f);
            DragonGrowBlade gb = root.AddComponent<DragonGrowBlade>();
            gb.Scaler = scaler.transform;
            gb.Grow = Mathf.Max(0.05f, grow);
            gb.Life = Mathf.Max(gb.Grow + 0.1f, life);
            return root;
        }

        // Blade along +Z: diamond cross-section, wide near the base, long taper to the point.
        private static Mesh BladeMesh(float length, float width, float thick, Color c, bool soft)
        {
            float[] z = { 0f, 0.07f, 0.8f, 1f };
            float[] wf = { 0.55f, 1f, 0.82f, 0f };
            List<Vector3> v = new List<Vector3>();
            List<int> t = new List<int>();
            for (int i = 0; i < z.Length; i++)
            {
                float hw = width * 0.5f * wf[i], ht = thick * 0.5f * Mathf.Max(0.05f, wf[i]);
                float zz = z[i] * length;
                v.Add(new Vector3(hw, 0f, zz)); v.Add(new Vector3(0f, ht, zz)); v.Add(new Vector3(-hw, 0f, zz)); v.Add(new Vector3(0f, -ht, zz));
            }
            for (int i = 0; i < z.Length - 1; i++)
                for (int k = 0; k < 4; k++)
                {
                    int a = i * 4 + k, b = i * 4 + (k + 1) % 4, cc = a + 4, d = b + 4;
                    t.Add(a); t.Add(cc); t.Add(b); t.Add(b); t.Add(cc); t.Add(d);
                }
            t.Add(0); t.Add(1); t.Add(2); t.Add(0); t.Add(2); t.Add(3);
            if (soft) { int count = t.Count; for (int i = 0; i < count; i += 3) { t.Add(t[i]); t.Add(t[i + 2]); t.Add(t[i + 1]); } }
            return FacetMesh(v, t, c, 0.6f);
        }

        // v0.25.75 Meteor Fall / Cataclysm: a tumbling faceted boulder of basalt with molten seams, wrapped in fire.
        public static GameObject MeteorRock(Vector3 pos, float size)
        {
            if (!Enabled) return null;
            GameObject root = new GameObject("IH_Meteor");
            root.transform.position = pos;
            int lat = 6, lon = 9;
            float r = Mathf.Max(0.3f, size * 0.55f);
            List<Vector3> v = new List<Vector3>();
            List<int> t = new List<int>();
            System.Random rng = new System.Random(UnityEngine.Random.Range(0, 100000));
            v.Add(new Vector3(0f, r * 0.9f, 0f));
            for (int j = 1; j < lat; j++)
            {
                float th = Mathf.PI * j / lat;
                for (int i = 0; i < lon; i++)
                {
                    float ph = Mathf.PI * 2f * i / lon + j * 0.35f;
                    float jr = r * (0.78f + 0.36f * (float)rng.NextDouble());
                    v.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(ph) * jr, Mathf.Cos(th) * jr, Mathf.Sin(th) * Mathf.Sin(ph) * jr));
                }
            }
            v.Add(new Vector3(0f, -r * 0.85f, 0f));
            int bottom = v.Count - 1;
            for (int i = 0; i < lon; i++) { t.Add(0); t.Add(1 + (i + 1) % lon); t.Add(1 + i); }
            for (int j = 0; j < lat - 2; j++)
                for (int i = 0; i < lon; i++)
                {
                    int a = 1 + j * lon + i, b = 1 + j * lon + (i + 1) % lon, c = a + lon, d = b + lon;
                    t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
                }
            int last = 1 + (lat - 2) * lon;
            for (int i = 0; i < lon; i++) { t.Add(bottom); t.Add(last + i); t.Add(last + (i + 1) % lon); }
            Mesh rock = FacetMesh(v, t, new Color(0.30f, 0.24f, 0.22f, 1f), 0.1f);
            // molten seams: some facets glow ember-hot
            Color[] cs = rock.colors;
            for (int i = 0; i + 2 < cs.Length; i += 3)
            {
                if (rng.NextDouble() < 0.28)
                {
                    Color hot = Color.Lerp(new Color(1f, 0.35f, 0.05f, 1f), new Color(1f, 0.75f, 0.3f, 1f), (float)rng.NextDouble());
                    cs[i] = hot; cs[i + 1] = hot; cs[i + 2] = hot;
                }
            }
            rock.colors = cs;
            MeshObject("rock", rock, ClearMat(), root.transform);
            DragonRotate rot = root.AddComponent<DragonRotate>();
            rot.Speed = new Vector3(70f, 110f, 40f);
            // fire shell + ember light, and a burning tail
            GameObject shell = new GameObject("fire");
            shell.transform.SetParent(root.transform, false);
            ParticleSystem ps = Particles(shell.transform, new Color(1f, 0.45f, 0.1f, 1f), 0, 140f * Amount, 1f, 0.45f, 0.5f, 2f, r * 0.35f, r * 0.8f, -0.8f,
                ParticleSystemShapeType.Sphere, r * 0.9f, Vector3.zero, false, true);
            ParticleSystem.MainModule mm = ps.main; mm.loop = true; mm.simulationSpace = ParticleSystemSimulationSpace.World;
            AttachGlow(root.transform, new Color(1f, 0.5f, 0.15f, 1f), r, 30f, Mathf.Max(6f, size * 4f));
            GameObject keep = root;
            TrailWhile(root.transform, new Color(1f, 0.42f, 0.08f, 1f), Mathf.Max(0.4f, r * 1.4f), delegate { return keep != null; });
            return root;
        }

        // v0.25.75 Arcane Phalanx: a real spectral sword (blade, guard, grip, pommel) instead of a line.
        public static void SpectralSword(Transform parent, float length, Color c)
        {
            if (!Enabled || parent == null) return;
            float w = length * 0.14f;
            Color body = c; body.a = 0.8f;
            Color halo = Color.Lerp(c, Color.white, 0.4f); halo.a = 0.45f;
            GameObject blade = MeshObject("blade", BladeMesh(length * 0.72f, w, w * 0.18f, body, false), ClearMat(), parent);
            blade.transform.localPosition = new Vector3(0f, 0f, -length * 0.25f + length * 0.2f);
            GameObject glow = MeshObject("halo", BladeMesh(length * 0.74f, w * 1.6f, w * 0.4f, halo, true), Mat(WhiteTex(), true), parent);
            glow.transform.localPosition = blade.transform.localPosition;
            GameObject guard = MeshObject("guard", BladeMesh(w * 2.6f, w * 0.5f, w * 0.3f, halo, true), Mat(WhiteTex(), true), parent);
            guard.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            guard.transform.localPosition = new Vector3(-w * 1.3f, 0f, blade.transform.localPosition.z);
            GameObject grip = MeshObject("grip", BladeMesh(length * 0.22f, w * 0.35f, w * 0.35f, body, false), ClearMat(), parent);
            grip.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            grip.transform.localPosition = blade.transform.localPosition;
        }

        // v0.25.77 Punishing Bomb: a black iron bomb with a brass fuse cap and a spitting fuse, tumbling in flight.
        public static void BombLook(GameObject bomb)
        {
            if (!Enabled || bomb == null) return;
            Renderer r = bomb.GetComponent<Renderer>();
            Material iron = SolidMat(new Color(0.12f, 0.11f, 0.12f, 1f));
            if (r != null && iron != null) r.sharedMaterial = iron;
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Collider cc = cap.GetComponent<Collider>(); if (cc != null) UnityEngine.Object.Destroy(cc);
            cap.transform.SetParent(bomb.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            cap.transform.localScale = new Vector3(0.32f, 0.08f, 0.32f);
            Renderer crr = cap.GetComponent<Renderer>();
            Material brass = SolidMat(new Color(0.62f, 0.46f, 0.20f, 1f));
            if (crr != null && brass != null) crr.sharedMaterial = brass;
            GameObject fuse = new GameObject("fuse");
            fuse.transform.SetParent(bomb.transform, false);
            fuse.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            ParticleSystem ps = Particles(fuse.transform, new Color(1f, 0.75f, 0.3f, 1f), 0, 90f * Amount, 1f, 0.25f, 1.5f, 3.5f, 0.04f, 0.09f, 0.6f,
                ParticleSystemShapeType.Sphere, 0.05f, Vector3.zero, true, true);
            ParticleSystem.MainModule mm = ps.main; mm.loop = true; mm.simulationSpace = ParticleSystemSimulationSpace.World;
            DragonRotate rot = bomb.AddComponent<DragonRotate>();
            rot.Speed = new Vector3(260f, 40f, 90f);
        }

        // v0.25.78 Holy Bulwark: a white-gold force field curving around your front (shell of a half dome).
        public static GameObject HolyDome(float radius)
        {
            if (!Enabled) return null;
            GameObject root = new GameObject("IH_HolyBulwark");
            int na = 28, ne = 12;
            List<Vector3> v = new List<Vector3>();
            List<Color> cs = new List<Color>();
            List<int> t = new List<int>();
            for (int j = 0; j <= ne; j++)
            {
                float el = Mathf.Lerp(-8f, 82f, (float)j / ne) * Mathf.Deg2Rad;
                for (int i = 0; i <= na; i++)
                {
                    float az = Mathf.Lerp(-105f, 105f, (float)i / na) * Mathf.Deg2Rad;
                    v.Add(new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el)) * radius + Vector3.up * 0.1f);
                    float edgeA = 1f - Mathf.Abs((float)i / na * 2f - 1f);           // 0 at the side edges
                    float rim = Mathf.Pow(1f - edgeA, 3f) * 0.7f + Mathf.Pow((float)j / ne, 4f) * 0.4f;
                    float lattice = 0.12f * Mathf.Max(0f, Mathf.Sin(i * 1.6f)) * Mathf.Max(0f, Mathf.Sin(j * 1.6f + 0.8f));
                    float fadeSide = Mathf.SmoothStep(0f, 1f, edgeA * 4f);
                    float fadeTop = Mathf.SmoothStep(0f, 1f, (1f - (float)j / ne) * 3f);
                    Color c = Color.Lerp(new Color(1f, 0.86f, 0.5f, 1f), new Color(1f, 1f, 0.92f, 1f), rim);
                    c.a = Mathf.Clamp01((0.10f + rim * 0.5f + lattice) * fadeSide * fadeTop);
                    cs.Add(c);
                }
            }
            for (int j = 0; j < ne; j++)
                for (int i = 0; i < na; i++)
                {
                    int a = j * (na + 1) + i, b = a + 1, c = a + na + 1, d = c + 1;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                    t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
                }
            Mesh m = new Mesh();
            m.vertices = v.ToArray(); m.colors = cs.ToArray(); m.triangles = t.ToArray();
            Vector2[] uv = new Vector2[v.Count]; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            m.uv = uv;
            m.RecalculateBounds();
            MeshObject("glow", m, Mat(WhiteTex(), true), root.transform);
            DragonDome dd = root.AddComponent<DragonDome>();
            dd.Mesh = m; dd.Base = cs.ToArray();
            Light l = root.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.88f, 0.6f, 1f); l.range = radius * 2.2f; l.intensity = 0f; l.shadows = LightShadows.None;
            dd.Light = l;
            return root;
        }

        public static void SetDomeAlpha(GameObject dome, float a)
        {
            if (dome == null) return;
            DragonDome dd = dome.GetComponent<DragonDome>();
            if (dd != null) dd.SetAlpha(a);
        }

        // v0.25.81 solid-object helpers ------------------------------------------------------------
        // Faceted tube from a to b (n sides) with elliptical radii (ru along u, rw along w) at each end; capped.
        private static void Tube(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 u, Vector3 w, float ru0, float rw0, float ru1, float rw1, int n)
        {
            int s = v.Count;
            for (int i = 0; i < n; i++) { float ang = (i + 0.5f) / n * Mathf.PI * 2f; v.Add(a + u * Mathf.Cos(ang) * ru0 + w * Mathf.Sin(ang) * rw0); }
            for (int i = 0; i < n; i++) { float ang = (i + 0.5f) / n * Mathf.PI * 2f; v.Add(b + u * Mathf.Cos(ang) * ru1 + w * Mathf.Sin(ang) * rw1); }
            v.Add(a); v.Add(b);
            int ca = s + 2 * n, cb = ca + 1;
            for (int i = 0; i < n; i++)
            {
                int p0 = s + i, p1 = s + (i + 1) % n, q0 = p0 + n, q1 = p1 + n;
                t.Add(p0); t.Add(q0); t.Add(p1); t.Add(p1); t.Add(q0); t.Add(q1);
                t.Add(ca); t.Add(p0); t.Add(p1);
                t.Add(cb); t.Add(q1); t.Add(q0);
            }
        }

        private static void DoubleSide(List<int> t)
        {
            int count = t.Count;
            for (int i = 0; i < count; i += 3) { t.Add(t[i]); t.Add(t[i + 2]); t.Add(t[i + 1]); }
        }

        // Judgement Hammer: a holy warhammer in UNIT space (haft along Y -0.5..0.5, head across X -0.5..0.5 at the top,
        // depth along Z -0.5..0.5). The caller scales the returned child to (width, height, depth) every frame.
        // Gold head with ivory striking faces, ivory haft with gold bands, gold pommel gem, a radiant halo shell and
        // light ribbons from both striking faces (they draw arcs while it flips).
        public static GameObject HolyWarhammer(Transform parent)
        {
            if (!Enabled || parent == null) return null;
            GameObject unit = new GameObject("IH_Warhammer");
            unit.transform.SetParent(parent, false);
            Color gold = new Color(1f, 0.80f, 0.34f, 1f), ivory = new Color(1f, 0.96f, 0.86f, 1f), deep = new Color(0.78f, 0.55f, 0.20f, 1f);
            Vector3 X = Vector3.right, Y = Vector3.up, Z = Vector3.forward;

            List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
            Tube(v, t, new Vector3(0f, -0.44f, 0f), new Vector3(0f, 0.28f, 0f), X, Z, 0.07f, 0.11f, 0.07f, 0.11f, 8);   // haft
            Mesh haft = FacetMesh(v, t, ivory, 0.3f);
            MeshObject("haft", haft, ClearMat(), unit.transform);

            v = new List<Vector3>(); t = new List<int>();
            Tube(v, t, new Vector3(0f, -0.50f, 0f), new Vector3(0f, -0.44f, 0f), X, Z, 0.04f, 0.06f, 0.13f, 0.20f, 8);    // pommel
            Tube(v, t, new Vector3(0f, -0.20f, 0f), new Vector3(0f, -0.15f, 0f), X, Z, 0.10f, 0.16f, 0.10f, 0.16f, 8);    // grip band
            Tube(v, t, new Vector3(0f, 0.10f, 0f), new Vector3(0f, 0.16f, 0f), X, Z, 0.10f, 0.16f, 0.10f, 0.16f, 8);      // upper band
            Tube(v, t, new Vector3(0f, 0.16f, 0f), new Vector3(0f, 0.29f, 0f), X, Z, 0.08f, 0.13f, 0.16f, 0.30f, 8);      // collar flare
            MeshObject("gold", FacetMesh(v, t, gold, 0.5f), ClearMat(), unit.transform);

            v = new List<Vector3>(); t = new List<int>();
            Tube(v, t, new Vector3(-0.36f, 0.39f, 0f), new Vector3(0.36f, 0.39f, 0f), Y, Z, 0.10f, 0.42f, 0.10f, 0.42f, 8);   // head block
            Tube(v, t, new Vector3(0f, 0.49f, 0f), new Vector3(0f, 0.56f, 0f), X, Z, 0.12f, 0.22f, 0.02f, 0.03f, 4);          // crown spike
            MeshObject("head", FacetMesh(v, t, gold, 0.6f), ClearMat(), unit.transform);

            v = new List<Vector3>(); t = new List<int>();
            Tube(v, t, new Vector3(-0.36f, 0.39f, 0f), new Vector3(-0.50f, 0.39f, 0f), Y, Z, 0.12f, 0.50f, 0.105f, 0.44f, 8);  // left face
            Tube(v, t, new Vector3(0.36f, 0.39f, 0f), new Vector3(0.50f, 0.39f, 0f), Y, Z, 0.12f, 0.50f, 0.105f, 0.44f, 8);    // right face
            Tube(v, t, new Vector3(0f, 0.39f, 0.40f), new Vector3(0f, 0.39f, 0.50f), X, Y, 0.09f, 0.06f, 0.03f, 0.02f, 4);     // front sigil stud
            Tube(v, t, new Vector3(0f, 0.39f, -0.40f), new Vector3(0f, 0.39f, -0.50f), X, Y, 0.09f, 0.06f, 0.03f, 0.02f, 4);   // back sigil stud
            MeshObject("faces", FacetMesh(v, t, ivory, 0.4f), ClearMat(), unit.transform);

            // radiant halo around the head (additive, soft, double-sided)
            v = new List<Vector3>(); t = new List<int>();
            Tube(v, t, new Vector3(-0.56f, 0.39f, 0f), new Vector3(0.56f, 0.39f, 0f), Y, Z, 0.17f, 0.62f, 0.17f, 0.62f, 10);
            DoubleSide(t);
            Color halo = new Color(1f, 0.85f, 0.45f, 0.22f);
            MeshObject("halo", FacetMesh(v, t, halo, 0.2f), Mat(WhiteTex(), true), unit.transform);
            v = new List<Vector3>(); t = new List<int>();
            Tube(v, t, new Vector3(0f, -0.48f, 0f), new Vector3(0f, 0.30f, 0f), X, Z, 0.13f, 0.2f, 0.13f, 0.2f, 8);
            DoubleSide(t);
            MeshObject("haftGlow", FacetMesh(v, t, new Color(1f, 0.9f, 0.6f, 0.12f), 0.2f), Mat(WhiteTex(), true), unit.transform);

            // light ribbons from both striking faces + a holy gleam at the crown
            GameObject keep = unit;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject tip = new GameObject(side < 0 ? "tipL" : "tipR");
                tip.transform.SetParent(unit.transform, false);
                tip.transform.localPosition = new Vector3(0.5f * side, 0.39f, 0f);
                TrailWhile(tip.transform, new Color(1f, 0.88f, 0.50f, 1f), 0.45f, delegate { return keep != null; });
            }
            return unit;
        }

        // Double-pointed lens blade along local X (centre at the origin), flat in local XY, thin in Z.
        private static Mesh LensMesh(float length, float width, float thick, Color c, bool soft)
        {
            float[] x = { -0.5f, -0.38f, -0.12f, 0f, 0.12f, 0.38f, 0.5f };
            float[] wf = { 0f, 0.55f, 0.92f, 1f, 0.92f, 0.55f, 0f };
            List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
            for (int i = 0; i < x.Length; i++)
            {
                float hw = width * 0.5f * wf[i], ht = thick * 0.5f * Mathf.Max(0.05f, wf[i]), xx = x[i] * length;
                v.Add(new Vector3(xx, hw, 0f)); v.Add(new Vector3(xx, 0f, ht)); v.Add(new Vector3(xx, -hw, 0f)); v.Add(new Vector3(xx, 0f, -ht));
            }
            for (int i = 0; i < x.Length - 1; i++)
                for (int k = 0; k < 4; k++)
                {
                    int a = i * 4 + k, b = i * 4 + (k + 1) % 4, cc = a + 4, d = b + 4;
                    t.Add(a); t.Add(b); t.Add(cc); t.Add(b); t.Add(d); t.Add(cc);
                }
            if (soft) DoubleSide(t);
            return FacetMesh(v, t, c, 0.6f);
        }

        // Grand Cross: one holy light blade (lens) with a white-hot core and a soft halo, lying along local X of `parent`
        // rotated by `rollDeg` around local Z. Both tips leave light ribbons as the cross travels.
        public static void LightBlade(Transform parent, float length, float width, float rollDeg, Color c)
        {
            if (!Enabled || parent == null) return;
            GameObject root = new GameObject("IH_LightBlade");
            root.transform.SetParent(parent, false);
            root.transform.localRotation = Quaternion.AngleAxis(rollDeg, Vector3.forward);
            Color body = c; body.a = 0.75f;
            Color halo = Color.Lerp(c, Color.white, 0.3f); halo.a = 0.30f;
            Color core = new Color(1f, 1f, 1f, 0.9f);
            MeshObject("body", LensMesh(length, width, width * 0.25f, body, true), ClearMat(), root.transform);
            MeshObject("halo", LensMesh(length * 1.06f, width * 2.2f, width * 0.6f, halo, true), Mat(WhiteTex(), true), root.transform);
            MeshObject("core", LensMesh(length * 0.92f, width * 0.32f, width * 0.12f, core, true), Mat(WhiteTex(), true), root.transform);
            GameObject keep = root;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject tip = new GameObject("tip");
                tip.transform.SetParent(root.transform, false);
                tip.transform.localPosition = new Vector3(length * 0.46f * side, 0f, 0f);
                TrailWhile(tip.transform, Color.Lerp(c, Color.white, 0.35f), Mathf.Max(0.3f, width * 0.6f), delegate { return keep != null; });
            }
        }

        // Barrier: a geodesic crystal shell (subdivided icosahedron, radius 0.5 like the primitive sphere) with lit
        // facets and brighter lattice rims, so the Barrier reads as a faceted holy shield, not a plain bubble.
        public static Mesh GeodesicShell(Color c)
        {
            float p = (1f + Mathf.Sqrt(5f)) * 0.5f;
            List<Vector3> v = new List<Vector3>
            {
                new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0),
                new Vector3(0, -1, p), new Vector3(0, 1, p), new Vector3(0, -1, -p), new Vector3(0, 1, -p),
                new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1)
            };
            int[] f = { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                        3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized * 0.5f;
            List<int> t = new List<int>();
            for (int i = 0; i < f.Length; i += 3)
            {
                Vector3 a = v[f[i]], b = v[f[i + 1]], d = v[f[i + 2]];
                Vector3 ab = ((a + b) * 0.5f).normalized * 0.5f, bd = ((b + d) * 0.5f).normalized * 0.5f, da = ((d + a) * 0.5f).normalized * 0.5f;
                int s = v.Count; v.Add(ab); v.Add(bd); v.Add(da);
                t.Add(f[i]); t.Add(s); t.Add(s + 2);
                t.Add(s); t.Add(f[i + 1]); t.Add(s + 1);
                t.Add(s + 2); t.Add(s + 1); t.Add(f[i + 2]);
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
            }
            DoubleSide(t);
            Mesh m = FacetMesh(v, t, c, 0.9f);
            // lattice: every third facet a touch brighter, so the shell sparkles as it turns
            Color[] cs = m.colors;
            for (int i = 0; i + 2 < cs.Length; i += 3)
                if ((i / 3) % 3 == 0) { Color k = Color.Lerp(cs[i], Color.white, 0.45f); k.a = cs[i].a; cs[i] = k; cs[i + 1] = k; cs[i + 2] = k; }
            m.colors = cs;
            return m;
        }

        // v0.25.82 WARRIOR HERO OBJECTS -----------------------------------------------------------------
        private static Mesh ColoredMesh(List<Vector3> v, List<Color> c, List<int> t)
        {
            Mesh m = new Mesh();
            m.vertices = v.ToArray(); m.colors = c.ToArray(); m.triangles = t.ToArray();
            Vector2[] uv = new Vector2[v.Count]; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            m.uv = uv;
            m.RecalculateBounds();
            return m;
        }

        // Eclipse: a black sun with a blazing violet corona (spiky rays) and a white-hot rim, hovering over you,
        // always facing the camera; it swells in, pulses, then collapses.
        public static GameObject EclipseSun(Vector3 pos, float radius, float seconds)
        {
            if (!Enabled) return null;
            GameObject root = new GameObject("IH_EclipseSun");
            root.transform.position = pos;
            root.transform.localScale = Vector3.one * radius;
            int n = 64;
            // black core (alpha)
            List<Vector3> v = new List<Vector3>(); List<Color> c = new List<Color>(); List<int> t = new List<int>();
            v.Add(Vector3.zero); c.Add(new Color(0.02f, 0f, 0.05f, 1f));
            for (int i = 0; i < n; i++) { float a = (float)i / n * Mathf.PI * 2f; v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f)); c.Add(new Color(0.05f, 0.01f, 0.10f, 1f)); }
            for (int i = 0; i < n; i++) { t.Add(0); t.Add(1 + i); t.Add(1 + (i + 1) % n); }
            MeshObject("core", ColoredMesh(v, c, t), ClearMat(), root.transform);
            // corona with rays (additive)
            v = new List<Vector3>(); c = new List<Color>(); t = new List<int>();
            System.Random rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float a = (float)i / n * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                float ray = 1.45f + 0.9f * Mathf.Pow(Mathf.Abs(Mathf.Sin(a * 4f + 0.4f)), 10f) + 0.25f * (float)rng.NextDouble();
                v.Add(d * 0.98f); c.Add(new Color(0.78f, 0.62f, 1f, 1f));
                v.Add(d * 1.12f); c.Add(new Color(0.62f, 0.42f, 1f, 0.85f));
                v.Add(d * ray); c.Add(new Color(0.35f, 0.15f, 0.9f, 0f));
            }
            for (int i = 0; i < n; i++)
            {
                int a0 = i * 3, b0 = ((i + 1) % n) * 3;
                for (int k = 0; k < 2; k++) { t.Add(a0 + k); t.Add(a0 + k + 1); t.Add(b0 + k); t.Add(b0 + k); t.Add(a0 + k + 1); t.Add(b0 + k + 1); }
            }
            MeshObject("corona", ColoredMesh(v, c, t), Mat(WhiteTex(), true), root.transform);
            // white-hot rim
            v = new List<Vector3>(); c = new List<Color>(); t = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float a = (float)i / n * Mathf.PI * 2f; Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                v.Add(d * 0.97f); c.Add(new Color(1f, 0.95f, 1f, 1f));
                v.Add(d * 1.04f); c.Add(new Color(0.9f, 0.8f, 1f, 0.6f));
            }
            for (int i = 0; i < n; i++) { int a0 = i * 2, b0 = ((i + 1) % n) * 2; t.Add(a0); t.Add(a0 + 1); t.Add(b0); t.Add(b0); t.Add(a0 + 1); t.Add(b0 + 1); }
            MeshObject("rim", ColoredMesh(v, c, t), Mat(WhiteTex(), true), root.transform);
            root.AddComponent<DragonFaceCamera>();
            DragonPop pop = root.AddComponent<DragonPop>();
            pop.Grow = 0.25f; pop.Life = Mathf.Max(0.3f, seconds); pop.Fade = 0.3f; pop.Pulse = 0.05f;
            Light l = root.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(0.62f, 0.45f, 1f, 1f); l.range = radius * 8f; l.intensity = 1.6f * LightScale; l.shadows = LightShadows.None;
            return root;
        }

        // Seismic Guillotine: a giant ember-forged guillotine blade drops out of the sky onto the rupture and bites
        // into the ground (slanted molten edge, weight block on top), then sinks away.
        public static void GuillotineDrop(Vector3 ground, Vector3 forward, float size)
        {
            if (!Enabled) return;
            forward.y = 0f; if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward; forward.Normalize();
            GameObject root = new GameObject("IH_Guillotine");
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            root.transform.rotation = Quaternion.LookRotation(side, Vector3.up);   // blade plane = local XY contains the fissure line
            root.transform.localScale = Vector3.one * size;
            int n = 10; float th = 0.05f;
            List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
            for (int i = 0; i <= n; i++)
            {
                float x = -0.5f + (float)i / n;
                float yb = -0.5f + 0.32f * (x + 0.5f);   // slanted cutting edge
                v.Add(new Vector3(x, 0.45f, th)); v.Add(new Vector3(x, 0.45f, -th)); v.Add(new Vector3(x, yb, 0f));
            }
            for (int i = 0; i < n; i++)
            {
                int a = i * 3, b = (i + 1) * 3;
                t.Add(a); t.Add(a + 2); t.Add(b); t.Add(b); t.Add(a + 2); t.Add(b + 2);             // front face
                t.Add(a + 1); t.Add(b + 1); t.Add(a + 2); t.Add(b + 1); t.Add(b + 2); t.Add(a + 2); // back face
                t.Add(a); t.Add(b); t.Add(a + 1); t.Add(b); t.Add(b + 1); t.Add(a + 1);             // top
            }
            Tube(v, t, new Vector3(-0.56f, 0.53f, 0f), new Vector3(0.56f, 0.53f, 0f), Vector3.up, Vector3.forward, 0.09f, 0.12f, 0.09f, 0.12f, 4);   // weight block
            MeshObject("blade", FacetMesh(v, t, new Color(0.30f, 0.27f, 0.26f, 1f), 0.5f), ClearMat(), root.transform);
            // molten edge (additive band along the cutting edge, both sides)
            List<Vector3> ev = new List<Vector3>(); List<Color> ec = new List<Color>(); List<int> et = new List<int>();
            for (int i = 0; i <= n; i++)
            {
                float x = -0.5f + (float)i / n, yb = -0.5f + 0.32f * (x + 0.5f);
                ev.Add(new Vector3(x, yb - 0.01f, 0f)); ec.Add(new Color(1f, 0.85f, 0.45f, 1f));
                ev.Add(new Vector3(x, yb + 0.12f, 0f)); ec.Add(new Color(1f, 0.35f, 0.05f, 0f));
            }
            for (int i = 0; i < n; i++) { int a = i * 2, b = a + 2; et.Add(a); et.Add(a + 1); et.Add(b); et.Add(b); et.Add(a + 1); et.Add(b + 1); }
            DoubleSide(et);
            GameObject edge = MeshObject("edge", ColoredMesh(ev, ec, et), Mat(WhiteTex(), true), root.transform);
            edge.transform.localScale = new Vector3(1f, 1f, 1f);
            AttachGlow(root.transform, Fire, size * 0.15f, 25f, size * 1.5f);
            DragonPop pop = root.AddComponent<DragonPop>();
            pop.From = ground + Vector3.up * (size * 3f + 8f);
            pop.To = ground + Vector3.up * (size * 0.25f);   // edge buried ~25%
            pop.Move = 0.18f; pop.EaseIn = true; pop.Life = 0.9f; pop.Fade = 0.35f; pop.Sink = size * 0.8f;
            root.transform.position = pop.From;
            TrailWhile(root.transform, new Color(1f, 0.5f, 0.15f, 1f), size * 0.3f, delegate { return pop != null && pop.Age < pop.Move; });
        }

        // Impact Punch: a giant spectral fist (palm, four knuckles, curled fingers, thumb) punches forward and bursts.
        public static void SpectralFist(Vector3 from, Vector3 forward, float distance, float size, Color c)
        {
            if (!Enabled) return;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            GameObject root = new GameObject("IH_SpectralFist");
            root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            root.transform.localScale = Vector3.one * size;
            List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
            Tube(v, t, new Vector3(0f, 0f, -0.6f), new Vector3(0f, 0f, 0.25f), Vector3.right, Vector3.up, 0.42f, 0.30f, 0.50f, 0.36f, 6);   // back of the hand + wrist
            for (int k = 0; k < 4; k++)
            {
                float x = -0.33f + k * 0.22f;
                Tube(v, t, new Vector3(x, 0.12f, 0.2f), new Vector3(x, 0.10f, 0.52f), Vector3.right, Vector3.up, 0.11f, 0.13f, 0.10f, 0.12f, 6);   // knuckles
                Tube(v, t, new Vector3(x, -0.02f, 0.5f), new Vector3(x, -0.22f, 0.42f), Vector3.right, Vector3.forward, 0.1f, 0.1f, 0.09f, 0.09f, 6); // curled fingers
            }
            Tube(v, t, new Vector3(-0.45f, -0.18f, 0.05f), new Vector3(-0.05f, -0.26f, 0.42f), Vector3.up, Vector3.forward, 0.11f, 0.12f, 0.09f, 0.1f, 6);   // thumb
            Color body = c; body.a = 0.7f;
            Color halo = Color.Lerp(c, Color.white, 0.35f); halo.a = 0.25f;
            MeshObject("fist", FacetMesh(v, t, body, 0.8f), ClearMat(), root.transform);
            GameObject h = MeshObject("halo", FacetMesh(v, new List<int>(t), halo, 0.3f), Mat(WhiteTex(), true), root.transform);
            h.transform.localScale = Vector3.one * 1.18f;
            AttachGlow(root.transform, c, size * 0.3f, 50f, size * 2.5f);
            DragonPop pop = root.AddComponent<DragonPop>();
            pop.From = from; pop.To = from + forward * distance;
            pop.Move = 0.14f; pop.Grow = 0.08f; pop.Life = 0.22f; pop.Fade = 0.18f;
            root.transform.position = from;
            TrailWhile(root.transform, c, size * 0.6f, delegate { return pop != null && pop.Age < pop.Life; });
        }

        // Knight's Guidance: dim spectral wings unfold from your back (two fans of light feathers), then fold away.
        public static void SpiritWings(Transform follow, Color c, float seconds)
        {
            if (!Enabled || follow == null) return;
            GameObject root = new GameObject("IH_SpiritWings");
            root.transform.SetParent(follow, false);
            root.transform.localPosition = new Vector3(0f, 1.35f, -0.25f);
            Color body = c; body.a = 0.38f;
            Color core = Color.Lerp(c, Color.white, 0.6f); core.a = 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject wing = new GameObject(side < 0 ? "wingL" : "wingR");
                wing.transform.SetParent(root.transform, false);
                for (int f = 0; f < 7; f++)
                {
                    float ang = 15f + f * 17f;                       // fan from up-out to down-out
                    float len = 1.5f - Mathf.Abs(f - 2.5f) * 0.12f;
                    GameObject feather = new GameObject("f");
                    feather.transform.SetParent(wing.transform, false);
                    float e = (60f - ang) * Mathf.Deg2Rad;            // +45 deg (up-out) .. -57 deg (down-out)
                    Vector3 d = new Vector3(side * Mathf.Cos(e), Mathf.Sin(e), -0.25f);
                    feather.transform.localRotation = Quaternion.LookRotation(d, Vector3.forward);   // flat in the back plane
                    MeshObject("b", BladeMesh(len, 0.32f, 0.04f, body, true), Mat(WhiteTex(), true), feather.transform);
                    MeshObject("c", BladeMesh(len * 0.95f, 0.06f, 0.02f, core, true), Mat(WhiteTex(), true), feather.transform);
                }
            }
            DragonPop pop = root.AddComponent<DragonPop>();
            pop.Grow = 0.35f; pop.Life = Mathf.Max(0.5f, seconds); pop.Fade = 0.45f;
            Feathers(follow.position + Vector3.up * 1.3f, c, 1.2f, 0.8f, 30f);
        }

        // v0.25.83 SORCERER HERO OBJECTS ---------------------------------------------------------------
        // Flat annulus in local XZ (radius 0.5 scale) broken into glowing rune dashes.
        private static Mesh RuneBandMesh(Color c, int segments, float inner, float outer)
        {
            List<Vector3> v = new List<Vector3>(); List<Color> cs = new List<Color>(); List<int> t = new List<int>();
            int n = segments * 4;
            for (int i = 0; i <= n; i++)
            {
                float a = (float)i / n * Mathf.PI * 2f;
                bool lit = (i / 2) % 2 == 0;
                Color k = c; k.a = lit ? c.a : c.a * 0.25f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * inner); cs.Add(k);
                v.Add(d * outer); cs.Add(k);
            }
            for (int i = 0; i < n; i++) { int a = i * 2, b = a + 2; t.Add(a); t.Add(a + 1); t.Add(b); t.Add(b); t.Add(a + 1); t.Add(b + 1); }
            DoubleSide(t);
            return ColoredMesh(v, cs, t);
        }

        // Turns a primitive sphere orb into an arcane energy orb: faceted crystal shell (keeps the orb's material,
        // so callers can still tint/fade it), a white-hot core and two rune bands spinning on different axes.
        // Dark orbs (Gravity Blast) get a black core and violet bands instead.
        public static void EnergyOrb(GameObject orb, Color c, bool full)
        {
            if (!Enabled || orb == null) return;
            MeshFilter mf = orb.GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = GeodesicShell(new Color(1f, 1f, 1f, 1f));
            if (!full) return;
            bool dark = c.grayscale < 0.2f;
            Color band = dark ? new Color(0.70f, 0.40f, 1f, 0.9f) : Color.Lerp(c, Color.white, 0.35f);
            band.a = 0.9f;
            GameObject core = MeshObject("core", GeodesicShell(dark ? new Color(0.02f, 0f, 0.05f, 1f) : new Color(1f, 0.97f, 1f, 0.9f)), dark ? ClearMat() : Mat(WhiteTex(), true), orb.transform);
            core.transform.localScale = Vector3.one * 0.55f;
            for (int k = 0; k < 2; k++)
            {
                GameObject pivot = new GameObject("bandPivot");
                pivot.transform.SetParent(orb.transform, false);
                pivot.transform.localRotation = Quaternion.Euler(k == 0 ? 20f : 75f, k * 60f, k == 0 ? -15f : 30f);
                GameObject ring = MeshObject("band", RuneBandMesh(band, 8, 0.62f, 0.72f), Mat(WhiteTex(), true), pivot.transform);
                ring.AddComponent<DragonRotate>().Speed = new Vector3(0f, k == 0 ? 220f : -160f, 0f);
            }
        }

        // Arcane lightning burst: real jagged bolts from the sky with branches, instead of straight lines.
        public static void ArcaneBoltRain(Vector3 center, float radius, int count)
        {
            if (!Enabled) return;
            for (int i = 0; i < count; i++)
            {
                float a = (float)i / count * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                Vector3 p = GroundPoint(center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * UnityEngine.Random.Range(0.45f, 0.8f));
                Color col = i % 2 == 0 ? new Color(0.72f, 0.36f, 1f, 1f) : new Color(0.50f, 0.85f, 1f, 1f);
                Bolt(p + Vector3.up * 11f + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)), p, col, 0.22f, 0.3f);
                Burst(p + Vector3.up * 0.2f, col, 10, 5f, 0.2f, 0.35f, 0f);
            }
        }

        // v0.25.84 RANGER HERO OBJECTS -----------------------------------------------------------------
        // Skill arrow: a glowing spirit arrow pointing +Z with the head at the origin (faceted head, shaft, 3 fletches).
        public static void SpiritArrow(Transform parent, Color c, float length)
        {
            if (!Enabled || parent == null) return;
            List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
            Tube(v, t, new Vector3(0f, 0f, -length), new Vector3(0f, 0f, -length * 0.12f), Vector3.right, Vector3.up, 0.018f, 0.018f, 0.018f, 0.018f, 5);   // shaft
            Mesh shaft = FacetMesh(v, t, new Color(0.92f, 0.86f, 0.70f, 1f), 0.3f);
            MeshObject("shaft", shaft, ClearMat(), parent);
            v = new List<Vector3>(); t = new List<int>();
            Tube(v, t, new Vector3(0f, 0f, -length * 0.14f), new Vector3(0f, 0f, 0f), Vector3.right, Vector3.up, 0.06f, 0.025f, 0.002f, 0.002f, 4);   // broadhead
            Color head = Color.Lerp(c, Color.white, 0.5f); head.a = 1f;
            MeshObject("head", FacetMesh(v, t, head, 0.8f), ClearMat(), parent);
            Color fl = c; fl.a = 0.85f;
            for (int k = 0; k < 3; k++)
            {
                GameObject f = new GameObject("fletch");
                f.transform.SetParent(parent, false);
                f.transform.localRotation = Quaternion.AngleAxis(k * 120f, Vector3.forward);
                List<Vector3> fv = new List<Vector3> { new Vector3(0f, 0.015f, -length * 0.98f), new Vector3(0f, 0.09f, -length * 0.95f), new Vector3(0f, 0.015f, -length * 0.72f) };
                List<int> ft = new List<int> { 0, 1, 2 };
                DoubleSide(ft);
                MeshObject("v", FacetMesh(fv, ft, fl, 0.2f), Mat(WhiteTex(), true), f.transform);
            }
            Color halo = c; halo.a = 0.3f;
            v = new List<Vector3>(); t = new List<int>();
            Tube(v, t, new Vector3(0f, 0f, -length * 0.5f), new Vector3(0f, 0f, 0.05f), Vector3.right, Vector3.up, 0.07f, 0.07f, 0.02f, 0.02f, 6);
            DoubleSide(t);
            MeshObject("halo", FacetMesh(v, t, halo, 0.1f), Mat(WhiteTex(), true), parent);
        }

        // Spiral wind funnel (radius r0 at the bottom -> r1 at the top), two counter-rotating additive layers with
        // streaked bands. Used by the Cyclone tornado and the Cyclone Arrow.
        private static Mesh FunnelMesh(float r0, float r1, float height, float twist, Color c, int bands)
        {
            int n = 40, h = 14;
            List<Vector3> v = new List<Vector3>(); List<Color> cs = new List<Color>(); List<int> t = new List<int>();
            for (int j = 0; j <= h; j++)
            {
                float y = (float)j / h;
                float r = Mathf.Lerp(r0, r1, Mathf.Pow(y, 0.8f));
                float fade = Mathf.SmoothStep(0f, 1f, y * 5f) * Mathf.SmoothStep(0f, 1f, (1f - y) * 4f);
                for (int i = 0; i <= n; i++)
                {
                    float a = (float)i / n * Mathf.PI * 2f + y * twist;
                    v.Add(new Vector3(Mathf.Cos(a) * r, y * height, Mathf.Sin(a) * r));
                    float stripe = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((float)i / n * Mathf.PI * 2f * bands - y * 6f)), 3f);
                    Color k = c; k.a = c.a * (0.12f + 0.88f * stripe) * fade;
                    cs.Add(k);
                }
            }
            for (int j = 0; j < h; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = j * (n + 1) + i, b = a + 1, cc = a + n + 1, d = cc + 1;
                    t.Add(a); t.Add(cc); t.Add(b); t.Add(b); t.Add(cc); t.Add(d);
                }
            DoubleSide(t);
            return ColoredMesh(v, cs, t);
        }

        public static void WindFunnel(Transform parent, Vector3 localBase, float r0, float r1, float height, Color c)
        {
            if (!Enabled || parent == null) return;
            Color outer = c; outer.a = 0.55f;
            Color inner = Color.Lerp(c, Color.white, 0.4f); inner.a = 0.45f;
            GameObject a = MeshObject("funnelA", FunnelMesh(r0, r1, height, 4f, outer, 4), Mat(WhiteTex(), true), parent);
            a.transform.localPosition = localBase;
            a.AddComponent<DragonRotate>().Speed = new Vector3(0f, -320f, 0f);
            GameObject b = MeshObject("funnelB", FunnelMesh(r0 * 0.6f, r1 * 0.7f, height * 0.95f, -3f, inner, 3), Mat(WhiteTex(), true), parent);
            b.transform.localPosition = localBase;
            b.AddComponent<DragonRotate>().Speed = new Vector3(0f, -480f, 0f);
        }

        // Furious Winds: real leaves (green lens blades) whirling around you on three tilted orbits.
        public static void LeafOrbit(Transform parent, float radius)
        {
            if (!Enabled || parent == null) return;
            Color[] cols = { new Color(0.40f, 0.80f, 0.30f, 0.95f), new Color(0.62f, 0.90f, 0.35f, 0.95f), new Color(0.85f, 0.75f, 0.30f, 0.95f) };
            System.Random rng = new System.Random(3);
            for (int k = 0; k < 3; k++)
            {
                GameObject pivot = new GameObject("leafOrbit");
                pivot.transform.SetParent(parent, false);
                pivot.transform.localPosition = new Vector3(0f, 0.6f + k * 0.7f, 0f);
                pivot.transform.localRotation = Quaternion.Euler(k * 7f - 7f, 0f, k * 5f);
                pivot.AddComponent<DragonRotate>().Speed = new Vector3(0f, 260f + k * 70f, 0f);
                float r = radius * (0.6f + 0.15f * k);
                for (int i = 0; i < 10; i++)
                {
                    float a = (i + (float)rng.NextDouble() * 0.6f) / 10f * 360f;
                    GameObject leaf = MeshObject("leaf", LensMesh(0.32f, 0.16f, 0.02f, cols[(i + k) % 3], true), ClearMat(), pivot.transform);
                    leaf.transform.localPosition = Quaternion.Euler(0f, a, 0f) * Vector3.forward * r + Vector3.up * ((float)rng.NextDouble() - 0.5f) * 0.6f;
                    leaf.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 360f, a, (float)rng.NextDouble() * 360f);
                    leaf.AddComponent<DragonRotate>().Speed = new Vector3(400f, 0f, 250f);
                }
            }
        }

        // Snare Trap: a real hunter's snare - a rope noose loop on the ground, wooden stakes leaning out around it,
        // and a faint green rune band so it stays readable.
        public static void SnareTrap(Transform parent, float radius)
        {
            if (!Enabled || parent == null) return;
            List<Vector3> v = new List<Vector3>(); List<int> t = new List<int>();
            int n = 18;
            float rr = radius * 0.8f;
            for (int i = 0; i < n; i++)
            {
                float a0 = (float)i / n * Mathf.PI * 2f, a1 = (float)(i + 1) / n * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * rr, 0.06f, Mathf.Sin(a0) * rr), p1 = new Vector3(Mathf.Cos(a1) * rr, 0.06f, Mathf.Sin(a1) * rr);
                Vector3 dir = (p1 - p0).normalized; Vector3 side = Vector3.Cross(Vector3.up, dir);
                Tube(v, t, p0, p1, Vector3.up, side, 0.05f, 0.05f, 0.05f, 0.05f, 5);
            }
            MeshObject("rope", FacetMesh(v, t, new Color(0.62f, 0.50f, 0.32f, 1f), 0.3f), ClearMat(), parent);
            v = new List<Vector3>(); t = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                float a = (i + 0.5f) / 6f * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 baseP = d * radius * 0.95f;
                Vector3 top = baseP + d * 0.25f + Vector3.up * 0.65f;
                Tube(v, t, baseP, top, Vector3.Cross(Vector3.up, d), d, 0.06f, 0.06f, 0.01f, 0.01f, 5);
            }
            MeshObject("stakes", FacetMesh(v, t, new Color(0.42f, 0.30f, 0.18f, 1f), 0.4f), ClearMat(), parent);
            GameObject band = MeshObject("rune", RuneBandMesh(new Color(0.75f, 1f, 0.45f, 0.5f), 10, 0.9f, 1f), Mat(WhiteTex(), true), parent);
            band.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            band.transform.localScale = Vector3.one * radius;
            band.AddComponent<DragonRotate>().Speed = new Vector3(0f, 25f, 0f);
        }

        // v0.25.80 Rift Walker portal: a standing oval of swirling void (dark core, spiral bands) with a blazing rim.
        public static void RiftPortal(Transform parent, Vector3 centre, float rx, float ry)
        {
            if (!Enabled || parent == null) return;
            GameObject root = new GameObject("IH_RiftPortal");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = centre;
            root.transform.localScale = new Vector3(rx, ry, 1f);
            GameObject swirl = MeshObject("swirl", SwirlDisk(false), ClearMat(), root.transform);
            DragonRotate r1 = swirl.AddComponent<DragonRotate>(); r1.Speed = new Vector3(0f, 0f, 90f);
            GameObject glow = MeshObject("glow", SwirlDisk(true), Mat(WhiteTex(), true), root.transform);
            DragonRotate r2 = glow.AddComponent<DragonRotate>(); r2.Speed = new Vector3(0f, 0f, -150f);
            ParticleSystem ps = Particles(root.transform, new Color(0.85f, 0.45f, 1f, 1f), 0, 60f * Amount, 1f, 0.6f, 0.2f, 0.8f, 0.04f, 0.12f, 0f,
                ParticleSystemShapeType.Circle, 1f, new Vector3(0f, 0f, 0f), false, true);
            ParticleSystem.MainModule mm = ps.main; mm.loop = true;
            ParticleSystem.ShapeModule sh = ps.shape; sh.radiusThickness = 0.05f; sh.rotation = new Vector3(0f, 0f, 0f);
        }

        // Unit disk in XY (radius 1): spiral arms; rim = bright edge ring only (additive), else dark void body.
        private static Mesh SwirlDisk(bool rim)
        {
            int na = 48, nr = 6;
            List<Vector3> v = new List<Vector3>();
            List<Color> cs = new List<Color>();
            List<int> t = new List<int>();
            for (int j = 0; j <= nr; j++)
            {
                float f = (float)j / nr;
                for (int i = 0; i <= na; i++)
                {
                    float a = (float)i / na * Mathf.PI * 2f;
                    float tw = a + f * 2.4f;   // spiral twist
                    v.Add(new Vector3(Mathf.Cos(a) * f, Mathf.Sin(a) * f, 0f));
                    float arm = 0.5f + 0.5f * Mathf.Sin(tw * 3f);
                    Color c;
                    if (rim) { c = Color.Lerp(new Color(0.75f, 0.3f, 1f, 1f), new Color(1f, 0.85f, 1f, 1f), f); c.a = Mathf.Pow(f, 6f) * 0.95f + arm * f * 0.15f; }
                    else { c = Color.Lerp(new Color(0.03f, 0f, 0.08f, 1f), new Color(0.35f, 0.08f, 0.6f, 1f), arm * f); c.a = Mathf.Lerp(0.92f, 0.55f, f); }
                    cs.Add(c);
                }
            }
            for (int j = 0; j < nr; j++)
                for (int i = 0; i < na; i++)
                {
                    int a = j * (na + 1) + i, b = a + 1, c = a + na + 1, d = c + 1;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                    t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
                }
            Mesh m = new Mesh();
            m.vertices = v.ToArray(); m.colors = cs.ToArray(); m.triangles = t.ToArray();
            Vector2[] uv = new Vector2[v.Count]; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            m.uv = uv; m.RecalculateBounds();
            return m;
        }

        public static void CastFlare(Player p, bool big)
        {
            if (!Enabled || p == null) return;
            Color c = ClassColor(p);
            Vector3 pos = p.transform.position;
            // v0.25.75 (user): no rune circle on the ground when a skill starts.
            Burst(pos + Vector3.up * 0.2f, Color.Lerp(c, Color.white, 0.3f), Mathf.RoundToInt((big ? 40 : 14) * Amount), big ? 3.5f : 2f, 0.12f, 0.7f, -1.2f);
            Flash(pos + Vector3.up * 1.1f, c, big ? 3f : 1.2f, big ? 8f : 4f, big ? 0.6f : 0.3f);
            if (big)
            {
                // v0.25.77 (user): no light column on cast
                Shake(pos, 20f, 0.6f);
            }
        }

        public static void WeaponCharge(Player p, float seconds)
        {
            if (!Enabled || p == null || seconds <= 0.05f) return;
            Animator a = p.GetComponentInChildren<Animator>();
            Transform hand = a == null ? null : a.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null) return;
            GameObject go = new GameObject("IH_WeaponCharge");
            go.transform.SetParent(hand, false);
            Color c = ClassColor(p);
            AttachGlow(go.transform, c, 0.18f, 45f * Amount, 2.5f);
            UnityEngine.Object.Destroy(go, Mathf.Min(seconds, 4f) + 0.2f);
        }

        public static readonly Color Holy = new Color(1f, 0.84f, 0.42f, 1f);
        public static readonly Color HolyWhite = new Color(1f, 0.96f, 0.82f, 1f);
        public static readonly Color Storm = new Color(0.45f, 0.78f, 1f, 1f);
        public static readonly Color Spirit = new Color(0.70f, 1f, 0.95f, 1f);
        public static readonly Color Fire = new Color(1f, 0.52f, 0.18f, 1f);

        public static void HolyImpact(Vector3 pos, float radius)
        {
            Shockwave(pos, Holy, radius, 0.45f);
            Burst(pos + Vector3.up * 0.4f, HolyWhite, Mathf.RoundToInt(24 + radius * 6f), 6f + radius, 0.25f, 0.8f, -0.2f);
            Vanilla(new string[] { "fx_DvergerMage_Support_start", "vfx_HealthUpgrade", "fx_guardstone_activate" }, pos, Quaternion.identity, 1f, 3f);
        }

        public static void LightningImpact(Vector3 pos, float radius)
        {
            Shockwave(pos, Storm, radius, 0.3f);
            Burst(pos + Vector3.up * 0.3f, Storm, Mathf.RoundToInt(30 + radius * 6f), 10f + radius, 0.2f, 0.5f, 0.8f);
            Vanilla(new string[] { "fx_himminafl_aoe", "fx_himminafl_hit", "fx_Lightning", "vfx_lightning" }, pos, Quaternion.identity, Mathf.Clamp(radius / 4f, 0.6f, 3f), 3f);
        }

        public static void SkyStrike(Vector3 ground, Color c, float radius, float height)
        {
            Bolt(ground + Vector3.up * height + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)), ground, c, 0.35f + radius * 0.04f, 0.35f);
            LightningImpact(ground, radius);
        }
    }
    // v0.25.75 helpers
    public class DragonRotate : MonoBehaviour
    {
        public Vector3 Speed;
        private void Update() { transform.Rotate(Speed * Time.deltaTime, Space.Self); }
    }

    public class DragonBlackHole : MonoBehaviour
    {
        public float Life = 3f;
        private float _age;
        private Vector3 _scale;
        private Light _light;
        private void Start() { _scale = transform.localScale; _light = GetComponent<Light>(); transform.localScale = _scale * 0.05f; }
        private void Update()
        {
            _age += Time.deltaTime;
            float k;
            if (_age < 0.35f) k = Mathf.SmoothStep(0.05f, 1f, _age / 0.35f);
            else if (_age < Life) k = 1f + 0.04f * Mathf.Sin(_age * 9f);
            else k = Mathf.Lerp(1f, 0f, (_age - Life) / 0.3f);
            transform.localScale = _scale * Mathf.Max(0.001f, k);
            if (_light != null) _light.intensity = Mathf.Max(0f, k) * 2.4f * DragonVfx.LightScale;
            if (_age >= Life + 0.3f) Destroy(gameObject);
        }
    }

    public class DragonGrowBlade : MonoBehaviour
    {
        public Transform Scaler;
        public float Grow = 0.4f, Life = 1.5f;
        private float _age;
        private void Update()
        {
            _age += Time.deltaTime;
            if (Scaler == null) { Destroy(gameObject); return; }
            float z;
            if (_age < Grow) { float t = _age / Grow; z = 1f - (1f - t) * (1f - t) * (1f - t); }
            else if (_age < Life) z = 1f;
            else z = Mathf.Clamp01(1f - (_age - Life) / 0.35f);
            float xy = _age < Life ? 1f : Mathf.Max(0.05f, z);
            Scaler.localScale = new Vector3(xy, xy, Mathf.Max(0.01f, z));
            if (_age >= Life + 0.35f) Destroy(gameObject);
        }
    }
    public class DragonDome : MonoBehaviour
    {
        public Mesh Mesh;
        public Color[] Base;
        public Light Light;
        private float _last = -1f;
        private Color[] _buf;
        public void SetAlpha(float a)
        {
            a = Mathf.Clamp(a, 0f, 1.8f);
            if (Light != null) Light.intensity = 1.2f * a * DragonVfx.LightScale;
            if (Mesh == null || Base == null || Mathf.Abs(a - _last) < 0.02f) return;
            _last = a;
            if (_buf == null || _buf.Length != Base.Length) _buf = new Color[Base.Length];
            for (int i = 0; i < Base.Length; i++) { Color c = Base[i]; c.a = Mathf.Clamp01(c.a * a); _buf[i] = c; }
            Mesh.colors = _buf;
        }
        private void OnDestroy() { if (Mesh != null) Destroy(Mesh); }
    }
    // v0.25.82: always faces the camera (billboard).
    public class DragonFaceCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        }
    }

    // v0.25.82: generic life of a hero object: optional move From -> To (EaseIn = accelerating, like a falling blade),
    // scale pop-in (Grow), optional pulse, then shrink (or Sink into the ground) over Fade and destroy.
    public class DragonPop : MonoBehaviour
    {
        public Vector3 From, To;
        public float Move, Grow, Life = 1f, Fade = 0.3f, Pulse, Sink;
        public bool EaseIn;
        public float Age;
        private Vector3 _scale;
        private bool _moves;
        private void Start()
        {
            _scale = transform.localScale;
            _moves = Move > 0f;
            if (Grow > 0f) transform.localScale = _scale * 0.05f;
        }
        private void Update()
        {
            Age += Time.deltaTime;
            if (_moves)
            {
                float m = Mathf.Clamp01(Age / Move);
                m = EaseIn ? m * m : 1f - (1f - m) * (1f - m);
                transform.position = Vector3.Lerp(From, To, m);
            }
            float k = 1f;
            if (Grow > 0f && Age < Grow) k = Mathf.SmoothStep(0.05f, 1f, Age / Grow);
            else if (Age < Life) k = 1f + Pulse * Mathf.Sin(Age * 9f);
            else
            {
                float f = Mathf.Clamp01((Age - Life) / Mathf.Max(0.01f, Fade));
                if (Sink > 0f) { transform.position = To - Vector3.up * Sink * f * f; k = 1f; }
                else k = 1f - f;
            }
            transform.localScale = _scale * Mathf.Max(0.001f, k);
            if (Age >= Life + Fade) Destroy(gameObject);
        }
    }
}
