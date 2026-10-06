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
        public const string ModName = "Dragon's Altar - Combat Runtime";
        public const string ModVersion = "0.25.23";

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

        private void Awake()
        {
            Instance = this;

            EnableRuntime = Config.Bind("Runtime", "Enabled", true, "Enable Dragon's Altar combat runtime patches.");
            EnableSkillAnimations = Config.Bind("Runtime", "EnableSkillAnimations", true, "Use Dragon's Altar procedural skill poses. Class skills do not trigger vanilla weapon attacks.");
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
            if (DragonCombat.HasNoEquipmentPenalty(p)) return "Heaven's Light";
            string adv = DragonCombat.GetAdvancementName(p);
            string cls = DragonCombat.GetClassName(p);
            if ((adv == "Mercenary" || adv == "Sword Master") && WeaponMasteryExemptPenalty(item, adv) < 0f)
                return adv == "Mercenary" ? "Warfreak" : "The Way of the Sword";
            if (cls == "Ranger" && RangedExemptPenalty(item, adv == "Bowmaster") < 0f) return "Wildborn";
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
            count += PatchEquipmentMovement();
            count += PatchUseStamina();
            count += PatchBlockAttack();
            count += PatchEquipItem();
            count += PatchHotbarUse();

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

        private static void BlockAttackPrefix(object __instance, ref BlockPatchState __state)
        {
            __state = new BlockPatchState();
            try
            {
                Player player = __instance as Player;
                if (player == null || player != Player.m_localPlayer) return;
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

        private static void BlockAttackPostfix(BlockPatchState __state, bool __result, object __instance)
        {
            RestoreBlockState(__state);
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

            if (DragonCombat.IsSkillLocked(__instance))
            {
                movedir = Vector3.zero;
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
            return !DragonCombat.HasHyperArmor(__instance);
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
            if (DragonCombat.GetClassName(__instance) != "Cleric")
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
        // Same pose as another key at a new time (holds / shakes).
        public DragonClipKey Copy(float t)
        {
            DragonClipKey k = new DragonClipKey(t);
            for (int i = 0; i < B.Length; i++) k.B[i] = B[i];
            for (int i = 0; i < L.Length; i++) k.L[i] = L[i];
            k.R = R; k.O = O; k.Lin = Lin; k.Spin = Spin;
            k.WD = WD; k.WW = WW; k.SD = SD; k.SW = SW; k.TW = TW; k.TG = TG; k.HD = HD; k.HR = HR; k.HW = HW;
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
        private int _token;
        private Vector3[] _b = new Vector3[10];
        private Vector3 _r, _o;
        private readonly float[] _l = new float[8];
        private float _spin;
        private Vector3 _wd, _sd;
        private float _ww, _sw, _tw, _tg, _env, _hr, _hw;
        private Vector3 _hd;
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

        public void Begin(DragonClipKey[] keys, float windup, bool hold, Transform visual)
        {
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

        private void LateUpdate()
        {
            if (_keys == null || _keys.Length == 0) { Destroy(this); return; }
            if (_owner == null) _owner = GetComponent<Character>();
            float t = Phase();
            if (t > _keys[_keys.Length - 1].T || (_owner != null && _owner.IsDead()))
            {
                ReleaseRoot();
                _keys = null;
                Destroy(this);
                return;
            }
            Sample(t);
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
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
                AimHeldItems();
            }
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
                Vector3 target = ua.position + frame.rotation * _hd.normalized * (len * Mathf.Clamp(_hr, 0.25f, 0.999f));
                Vector3 pole = frame.rotation * new Vector3(0.5f, -1f, -0.4f);
                TwoBoneIK(ua, la, hand, Vector3.Lerp(hand.position, target, w), pole, 1f);
                _written[4] = ua.localRotation; _hasWritten[4] = true;
                _written[5] = la.localRotation; _hasWritten[5] = true;
                _written[6] = hand.localRotation; _hasWritten[6] = true;
            }
            catch (Exception) { }
        }

        private void PlantFeet()
        {
            bool can = _feetCaptured && _visual != null && DragonCombat.OwnsMotionRoot(_token) && Grounded();
            _plantW = Mathf.MoveTowards(_plantW, can ? _env : 0f, Time.deltaTime * 8f);
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
                    Quaternion footRot = parentRot * baseRot * qy * _footLocalRot[f];
                    TwoBoneIK(a, b, c, Vector3.Lerp(c.position, target, _plantW), pole, 1f);
                    c.rotation = Quaternion.Slerp(c.rotation, footRot, _plantW);
                }
                for (int i = 0; i < 6; i++) _legWritten[i] = _leg[i].localRotation;
                _legHas = true;
            }
            catch (Exception) { }
        }

        // Analytic two-bone IK (upper, lower, end) toward t; bends in the current plane, or toward `pole`.
        private static void TwoBoneIK(Transform ua, Transform la, Transform end, Vector3 t, Vector3 pole, float w)
        {
            Vector3 a = ua.position, b = la.position, c = end.position;
            float lab = (b - a).magnitude, lcb = (c - b).magnitude;
            if (lab < 0.0001f || lcb < 0.0001f) return;
            float lat = Mathf.Clamp((t - a).magnitude, 0.01f, lab + lcb - 0.001f);
            float ac_ab_0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((c - a).normalized, (b - a).normalized), -1f, 1f));
            float ba_bc_0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((a - b).normalized, (c - b).normalized), -1f, 1f));
            float ac_at_0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((c - a).normalized, (t - a).normalized), -1f, 1f));
            float ac_ab_1 = Mathf.Acos(Mathf.Clamp((lcb * lcb - lab * lab - lat * lat) / (-2f * lab * lat), -1f, 1f));
            float ba_bc_1 = Mathf.Acos(Mathf.Clamp((lat * lat - lab * lab - lcb * lcb) / (-2f * lab * lcb), -1f, 1f));
            Vector3 axis0 = Vector3.Cross(c - a, b - a);
            if (axis0.sqrMagnitude < 0.00001f || Vector3.Dot(b - (a + c) * 0.5f, pole) < 0f) axis0 = Vector3.Cross(c - a, pole);
            if (axis0.sqrMagnitude < 0.000001f) return;
            axis0.Normalize();
            Vector3 axis1 = Vector3.Cross(c - a, t - a);
            Quaternion ag = ua.rotation, bg = la.rotation;
            Quaternion r0 = Quaternion.AngleAxis((ac_ab_1 - ac_ab_0) * Mathf.Rad2Deg, Quaternion.Inverse(ag) * axis0);
            Quaternion r1 = Quaternion.AngleAxis((ba_bc_1 - ba_bc_0) * Mathf.Rad2Deg, Quaternion.Inverse(bg) * axis0);
            Quaternion r2 = axis1.sqrMagnitude > 0.000001f ? Quaternion.AngleAxis(ac_at_0 * Mathf.Rad2Deg, Quaternion.Inverse(ag) * axis1.normalized) : Quaternion.identity;
            Quaternion ua0 = ua.localRotation, la0 = la.localRotation;
            ua.localRotation = Quaternion.Slerp(ua0, ua0 * r0 * r2, w);
            la.localRotation = Quaternion.Slerp(la0, la0 * r1, w);
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
                Vector3 a = ua.position, b = la.position, c = lh.position;
                float lab = (b - a).magnitude, lcb = (c - b).magnitude;
                if (lab < 0.0001f || lcb < 0.0001f) return;
                float lat = Mathf.Clamp((t - a).magnitude, 0.01f, lab + lcb - 0.01f);
                float ac_ab_0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((c - a).normalized, (b - a).normalized), -1f, 1f));
                float ba_bc_0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((a - b).normalized, (c - b).normalized), -1f, 1f));
                float ac_at_0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((c - a).normalized, (t - a).normalized), -1f, 1f));
                float ac_ab_1 = Mathf.Acos(Mathf.Clamp((lcb * lcb - lab * lab - lat * lat) / (-2f * lab * lat), -1f, 1f));
                float ba_bc_1 = Mathf.Acos(Mathf.Clamp((lat * lat - lab * lab - lcb * lcb) / (-2f * lab * lcb), -1f, 1f));
                Vector3 axis0 = Vector3.Cross(c - a, b - a);
                if (axis0.sqrMagnitude < 0.000001f) axis0 = Vector3.Cross(c - a, -(_visual != null ? _visual : transform).up);
                axis0.Normalize();
                Vector3 axis1 = Vector3.Cross(c - a, t - a);
                Quaternion ag = ua.rotation, bg = la.rotation;
                Quaternion r0 = Quaternion.AngleAxis((ac_ab_1 - ac_ab_0) * Mathf.Rad2Deg, Quaternion.Inverse(ag) * axis0);
                Quaternion r1 = Quaternion.AngleAxis((ba_bc_1 - ba_bc_0) * Mathf.Rad2Deg, Quaternion.Inverse(bg) * axis0);
                Quaternion r2 = axis1.sqrMagnitude > 0.000001f ? Quaternion.AngleAxis(ac_at_0 * Mathf.Rad2Deg, Quaternion.Inverse(ag) * axis1.normalized) : Quaternion.identity;
                Quaternion ua0 = ua.localRotation, la0 = la.localRotation;
                ua.localRotation = Quaternion.Slerp(ua0, ua0 * r0 * r2, w);
                la.localRotation = Quaternion.Slerp(la0, la0 * r1, w);
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
                    w = raise;   // universal rule: the item continues the forearm (envelope = clip in/out)
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
            DragonClipKey[] keys = SkillClip(clip);
            if (keys == null) return;
            DragonSkillPoseDriver legacy = player.GetComponent<DragonSkillPoseDriver>();
            if (legacy != null) UnityEngine.Object.Destroy(legacy);
            DragonSkillClipDriver d = player.GetComponent<DragonSkillClipDriver>();
            if (d == null) d = player.gameObject.AddComponent<DragonSkillClipDriver>();
            d.Begin(keys, windup, hold, BodyVisual(player));
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
            List<DragonClipKey> k = new List<DragonClipKey>();
            k.Add(K(-1f));
            k.Add(K(-0.5f).Sp(10f, 30f, 0f).Ch(4f, 16f, 0f).RA(-60f, 0f, 40f).RF(-60f, 0f, 0f).Off(0f, -0.08f, 0f).LL(0.2f, 0.12f, 0.35f, 0f).RL(0.15f, 0.12f, 0.35f, 0f));   // coil
            k.Add(K(0f).Sp(8f, 0f, 0f).Ch(4f, 0f, 0f).RA(-85f, 0f, -75f).RF(-10f, 0f, 0f).Rot(4f, 0f, 0f).Off(0f, -0.05f, 0f).LL(0.12f, 0.14f, 0.25f, 0f).RL(0.1f, 0.14f, 0.25f, 0f));
            float t = 0f, yaw = 0f, q = Mathf.Max(0.08f, turn) * 0.25f;
            // v0.25.17: whole turns only, so the exit never snaps or unwinds backward.
            while (t < seconds || yaw % 360f != 0f)
            {
                t += q; yaw += 90f;
                k.Add(K(t).Sp(8f, 0f, 0f).Ch(4f, 0f, 0f).RA(-85f, 0f, -75f).RF(-10f, 0f, 0f).Rot(4f, yaw, 0f).Off(0f, -0.05f, 0f).LL(0.12f, 0.14f, 0.25f, 0f).RL(0.1f, 0.14f, 0.25f, 0f).Linear());
            }
            k.Add(K(t + 0.3f).Rot(0f, yaw, 0f));
            PlayClipKeys(player, k.ToArray(), 0.12f);
        }

        public static void PlayClip(Player player, string clip, float windup)
        {
            PlayClip(player, clip, windup, false);
        }

        // v0.25.15: automatic procs (Fury, Overcharge, death-save, parry burst) are low-priority accents:
        // they never replace a skill clip that is playing.
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
        internal static bool ForceBoolOverride(object zanim, object key, ref bool value)
        {
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
            DragonClipKey cock = Ft(K(-0.45f).Sp(4f, 18f * a, 0f).Ch(2f, 10f * a, 0f).Hd(0f, -12f, 0f).Hand(-0.6f, 0.45f, 0.4f, 0.65f).Wp(-0.3f, 0.6f, -0.7f).Two(-0.12f).Off(0f, -0.04f, 0f), 0.2f, 0.1f);
            if (pull) cock = Ft(K(-0.45f).Sp(6f, -16f, 0f).Ch(3f, -8f, 0f).Hd(0f, 14f, 0f).Hand(0.25f, -0.75f, -0.25f, 0.7f).Wp(0f, 0f, 1f).Two(-0.12f).Off(0f, -0.06f, -0.03f), 0.3f, 0.2f);
            DragonClipKey left = Ft(K(-0.12f).Sp(5f, 24f * a, 0f).Ch(3f, 14f * a, 0f).Hd(0f, -16f, 0f).Hand(-0.75f, 0f, 0.65f, 0.9f).Wp(-1f, 0f, 0.35f).Two(-0.12f).Off(0f, -0.05f, 0f), 0.25f, 0.12f);
            DragonClipKey front = Ft(K(0f).Sp(6f, 0f, 0f).Ch(3f, 0f, 0f).Hand(-0.15f, -0.05f, 1f, 0.95f).Wp(0f, 0f, 1f).Two(-0.12f).Off(0f, -0.06f, 0.03f), 0.32f, 0.12f).Linear();
            DragonClipKey right = Ft(K(0.11f).Sp(6f, -24f * a, 0f).Ch(3f, -14f * a, 0f).Hd(0f, 12f, 0f).Hand(0.7f, 0f, 0.7f, 0.95f).Wp(1f, 0f, -0.2f).Two(-0.12f).Off(0f, -0.06f, 0.03f), 0.32f, 0.12f).Linear();
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
            return Ft(K(t).Sp(5f, 0f, 0f).Hand(1f, 0f, 0.2f, 1f).Wp(1f, 0f, 0.1f).Off(0f, -0.04f, 0f), 0.15f, 0.1f);
        }

        private static DragonClipKey[] SbSpin(int turns, float turn)
        {
            List<DragonClipKey> k = new List<DragonClipKey>();
            k.Add(K(-1f));
            k.Add(Ft(K(-0.5f).Sp(6f, 24f, 0f).Ch(3f, 12f, 0f).Hd(0f, -14f, 0f).Hand(-0.6f, 0f, 0.6f, 0.7f).Wp(-1f, 0f, 0f).Off(0f, -0.05f, 0f), 0.2f, 0.1f));
            k.Add(SbSpinPose(0f));
            float t = 0f, yaw = 0f, q = turn * 0.25f;
            for (int i = 0; i < turns * 4; i++)
            {
                t += q; yaw += 90f;
                k.Add(SbSpinPose(t).Rot(0f, yaw, 0f).Linear());
            }
            k.Add(K(t + 0.35f).Rot(0f, yaw, 0f));
            return k.ToArray();
        }

        // Ground cast (GCA). v 0 Stonefang (hand + weapon down at the ground), 1 Stomp (foot only), 2 Snare, 3 mobile flick.
        private static DragonClipKey[] SbGround(int v)
        {
            if (v == 1)
            {
                DragonClipKey lift = K(-0.45f).Sp(3f, 0f, 0f).Hd(4f, 0f, 0f).LL(0f, 0.08f, 0f, 0f).RL(0.25f, 0.08f, 0f, 0f).Off(0f, 0.04f, 0f);
                DragonClipKey hit = Ft(K(0f).Sp(10f, 0f, 0f).Ch(4f, 0f, 0f).Hd(6f, 0f, 0f).Off(0f, -0.08f, 0f), 0.05f, -0.2f).Linear();
                DragonClipKey stAfter = hit.Copy(0.22f); stAfter.Lin = false;
                return new DragonClipKey[] { K(-1f), lift, hit, stAfter, K(0.6f) };
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

        // Olympic Hero (JSAA, storyboard 01): load fist cocked back + crouch, launch fist forward, fast counter-
        // clockwise barrel roll, unwind fist down, main-hand impact (fist on the ground, left foot forward, right
        // knee low behind), recover to guard. Hold at the poised fall until the real landing.
        private static DragonClipKey[] SbOlympic(bool brutal)
        {
            float d = brutal ? 1.15f : 1f;
            DragonClipKey load = Ft(K(-0.93f).Sp(16f * d, -10f, 0f).Ch(6f, -6f, 0f).Hd(-12f, 0f, 0f).Hand(0.45f, 0.1f, -0.45f, 0.6f).Off(0f, -0.15f * d, 0f), 0.2f, 0.1f);
            DragonClipKey launch = K(-0.72f).Sp(4f, 0f, 0f).Hd(-35f, 0f, 0f).Hand(0.05f, 0.9f, 0.45f, 1f).Rot(70f, 0f, 0f).Off(0f, 0.08f, 0f);
            DragonClipKey roll0 = launch.Copy(-0.62f).Rot(72f, 0f, 0f).Sn(25f);
            DragonClipKey roll1 = launch.Copy(-0.36f).Rot(72f, 0f, 0f).Sn(360f);
            DragonClipKey poised = K(0f).Sp(12f, 0f, 0f).Ch(6f, 0f, 0f).Hd(-8f, 0f, 0f).Hand(0.1f, -0.8f, 0.45f, 0.95f).Rot(28f, 0f, 0f).Sn(360f);
            DragonClipKey impact = Ft(K(0.08f).Sp(30f, 0f, 0f).Ch(14f, 0f, 0f).Hd(-28f, 0f, 0f).Hand(0.05f, -1f, 0.35f, 0.97f).Rot(6f, 0f, 0f).Off(0f, -0.38f * d, 0.05f).Sn(360f), 0.75f, 0.8f);
            DragonClipKey settle = impact.Copy(brutal ? 0.3f : 0.2f).Off(0f, -0.4f * d, 0.05f);
            DragonClipKey rec = Ft(K(brutal ? 0.58f : 0.45f).Sp(8f, 0f, 0f).Hd(-6f, 0f, 0f).Hand(0.35f, -0.45f, 0.35f, 0.6f).Off(0f, -0.06f, 0f).Sn(360f), 0.3f, 0.2f);
            return new DragonClipKey[] { K(-1f), load, launch, roll0, roll1, poised, impact, settle, rec, K(brutal ? 0.9f : 0.75f).Sn(360f) };
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
            DragonClipKey[] pullHold = SbSlash(0.6f, 0.1f, false, true);
            c["sm_halfmoon_stance"] = new DragonClipKey[] { K(-1f), pullHold[1].Copy(0f), K(0.15f) };
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
            DragonClipKey[] oh = c["olympic_hero"];
            List<DragonClipKey> land = new List<DragonClipKey>();
            land.Add(oh[5].Copy(-1f));
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

            // No euler arm offsets on the main arm where a hand target drives it (no double bending).
            HashSet<DragonClipKey> seen = new HashSet<DragonClipKey>();
            foreach (KeyValuePair<string, DragonClipKey[]> kv in c)
                for (int i = 0; i < kv.Value.Length; i++)
                {
                    DragonClipKey k = kv.Value[i];
                    if (k.HW > 0f && seen.Add(k)) { k.B[4] = Vector3.zero; k.B[5] = Vector3.zero; }
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
            land.Add(oh[5].Copy(-1f));
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
                    return;
                }
                // v0.25.2 perf: no re-add while the same buff is refreshed every frame.
                IhStatusDisplay se = ScriptableObject.CreateInstance<IhStatusDisplay>();
                se.name = name;
                se.m_name = label;
                se.m_tooltip = label;
                se.m_ttl = seconds;
                se.Stacks = stacks;
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
            if (target == null || target.IsDead()) return;
            DragonCrippleController controller = target.GetComponent<DragonCrippleController>();
            if (controller == null) controller = target.gameObject.AddComponent<DragonCrippleController>();
            if (target.IsBoss())
            {
                controller.Apply(target, 1f - Mathf.Clamp01(bossSlow), Mathf.Max(0.1f, seconds));
                return;
            }
            Stun(target, target.transform.position - target.transform.forward);
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
            ShowStatus(player, "clockwork", "clockwork", "Clockwork", seconds, 0);
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
            SkillLockUntil[player.GetInstanceID()] = Time.time + Mathf.Max(0f, duration);
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
            if (duration >= 1f) ShowStatus(player, "hyper_armor", "hyper_armor", "Hyper Armor", duration, 0);
        }

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
            if (current <= 0.05f)
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
                state.LastFactor > 0.05f &&
                Mathf.Abs(current - state.LastOutputSpeed) <= 0.01f)
            {
                baseSpeed = current / state.LastFactor;
            }

            float factor = 1f;

            if (player.InAttack())
                factor = GetAttackSpeedMultiplier(player);

            factor = Mathf.Max(0.1f, factor);

            float output = Mathf.Clamp(baseSpeed * factor, 0.05f, 5f);

            animator.speed = output;
            state.HasOutput = true;
            state.LastFactor = factor;
            state.LastOutputSpeed = output;
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

            if (DragonCombatPlugin.Instance != null && DragonCombatPlugin.Instance.MasteryComboStyleEnabled.Value)
            {
                int stage = GetMasteryComboStage(player);
                if (stage > 1)
                {
                    float perStage = Mathf.Max(0f, DragonCombatPlugin.Instance.MasteryComboSpeedPerStage.Value) / 100f;
                    factor *= 1f + perStage * (float)(stage - 1);
                }
            }

            float heavyUntil;
            if (MasteryHeavyUntil.TryGetValue(id, out heavyUntil) && Time.time < heavyUntil)
            {
                float heavyBonus = DragonCombatPlugin.Instance == null ? 0.10f : Mathf.Max(0f, DragonCombatPlugin.Instance.MasteryHeavyAttackSpeedBonus.Value / 100f);
                factor *= 1f + heavyBonus;
            }

            return Mathf.Max(0.1f, factor);
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
            if (duration >= 1f) ShowStatus(player, "buff_" + source, statusIcon, statusLabel, duration, 0);

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
            DebuffState state = GetDebuffState(target);
            if (state == null)
                return;
            state.ExposeUntil = Time.time + ResolveDuration(duration);
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
            DebuffState state = GetDebuffState(target);
            if (state == null)
                return;

            float resolved = ResolveDuration(duration);
            state.CrippleUntil = Time.time + resolved;

            DragonCrippleController controller = target.GetComponent<DragonCrippleController>();
            if (controller == null)
                controller = target.gameObject.AddComponent<DragonCrippleController>();

            float slow = DragonCombatPlugin.Instance == null ? 0.50f : Mathf.Clamp01(DragonCombatPlugin.Instance.CrippleSlow.Value / 100f);
            controller.Apply(target, 1f - slow, resolved);
        }

        public static void ApplyFrost(Character target, float duration)
        {
            DebuffState state = GetDebuffState(target);
            if (state == null) return;
            float resolved = ResolveDuration(duration);
            state.FrostUntil = Time.time + resolved;
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
            if (target == null || target.IsDead() || target.IsBoss()) // Bosses are never stunned (global rule)
                return;
            Vector3 dir = target.transform.position - fromPoint;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector3.forward;
            target.Stagger(dir.normalized);
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
        }

        public static void ApplyFireBurnTick(Player attacker, Character target, float damage)
        {
            float amount = ResolveBurnTickDamage(target, damage, false) * GetSorcererMagicDamageMultiplier(attacker) * BurnRamp(target, false);
            ApplyNativeDelayedDamage(attacker, target, "AddFireDamage", amount, false);
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

            Player attacker = hit.GetAttacker() as Player;
            // v0.25.11: split parts of a hit were already fully modified as the original hit.
            if (attacker != null && !SplitHitInFlight)
            {
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

                ApplyMasteryFinisher(attacker, hit);
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

                if (GetClass(targetPlayer) == "Warrior")
                {
                    bool merc = GetAdvancement(targetPlayer) == "Mercenary";
                    float threshold = DragonCombatPlugin.Instance == null ? (merc ? 0.60f : 0.30f)
                        : Mathf.Clamp01((merc ? DragonCombatPlugin.Instance.MercenaryHyperArmorThreshold.Value : DragonCombatPlugin.Instance.WarriorHyperArmorThreshold.Value) / 100f);
                    float raw = Mathf.Max(0f, hit.GetTotalDamage());
                    // v0.24.4: decided by the FIRST hit of an attacker's attack only (follow-up hits within
                    // 1s keep that decision); damage is never accumulated.
                    Character source = hit.GetAttacker();
                    string key = targetPlayer.GetInstanceID() + ":" + (source == null ? 0 : source.GetInstanceID());
                    float last;
                    bool grant;
                    if (!HyperFirstHitLast.TryGetValue(key, out last) || Time.time - last > 1f || !HyperFirstHitGrant.TryGetValue(key, out grant))
                    {
                        grant = raw > 0f && raw < targetPlayer.GetMaxHealth() * threshold;
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

            DebuffState state;
            if (!Debuffs.TryGetValue(target.GetInstanceID(), out state))
                return patchState;

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

        public static void RuntimeUpdate()
        {
            float now = Time.time;

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

            if (state.Stage == 5)
            {
                state.FinisherAttackStartedAt = Time.time;
                state.FinisherUntil = Time.time + 1.25f;

                if (MessageHud.instance != null)
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Weapon Mastery: Finisher");

                if (DragonCombatPlugin.Instance != null)
                    DragonCombatPlugin.Instance.StartCoroutine(MasteryFinisherVisual(player));
            }
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
    public class IhStatusDisplay : StatusEffect
    {
        public int Stacks;

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
