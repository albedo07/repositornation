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
        public const string ModVersion = "0.25.8";

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
                        PatchWithHarmony(method, new HarmonyMethod(prefixMethod), new HarmonyMethod(postfixMethod));
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
                float pulse = 0.78f + Mathf.Sin(phase * Mathf.PI * 6f) * 0.22f;
                Offset(HumanBodyBones.Spine, new Vector3(0f, -22f, -4f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-28f, -48f, -68f), weight * pulse);
                Offset(HumanBodyBones.RightLowerArm, new Vector3(12f, -8f, -34f), weight * pulse);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-12f, 18f, 30f), weight * 0.65f);
                return;
            }

            if (style == "Whirlwind")
            {
                float sway = Mathf.Sin(phase * Mathf.PI * 8f);
                Offset(HumanBodyBones.Spine, new Vector3(0f, sway * 18f, 0f), weight);
                Offset(HumanBodyBones.RightUpperArm, new Vector3(-12f, -28f, -78f), weight);
                Offset(HumanBodyBones.LeftUpperArm, new Vector3(-12f, 28f, 78f), weight);
                return;
            }

            if (style == "CircleSwing")
            {
                float skip = Mathf.Sin(phase * Mathf.PI * 4f);
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

        private void Offset(HumanBodyBones bone, Vector3 euler, float weight)
        {
            Transform target = _animator.GetBoneTransform(bone);
            if (target == null)
                return;

            target.localRotation = target.localRotation * Quaternion.Euler(euler * weight);
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
        // Same pose as another key at a new time (holds / shakes).
        public DragonClipKey Copy(float t)
        {
            DragonClipKey k = new DragonClipKey(t);
            for (int i = 0; i < B.Length; i++) k.B[i] = B[i];
            k.R = R; k.O = O; k.Lin = Lin;
            return k;
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
            _windup = Mathf.Max(0.1f, windup);
            _hold = hold;
            _start = Time.time;
            _impactAt = -1f;
            _holdLimit = Time.time + 12f;
            _visual = visual;
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _token = DragonCombat.ClaimMotionRoot(_visual);
        }

        public bool IsHolding { get { return _keys != null && _hold && _impactAt < 0f; } }

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
            if (t <= k[0].T) { Set(k[0], k[0], 0f); return; }
            if (t >= k[n - 1].T) { Set(k[n - 1], k[n - 1], 0f); return; }
            for (int i = 0; i < n - 1; i++)
            {
                if (t >= k[i].T && t <= k[i + 1].T)
                {
                    float w = (t - k[i].T) / Mathf.Max(0.0001f, k[i + 1].T - k[i].T);
                    Set(k[i], k[i + 1], k[i + 1].Lin ? w : Mathf.SmoothStep(0f, 1f, w));
                    return;
                }
            }
        }

        private void Set(DragonClipKey a, DragonClipKey b, float w)
        {
            for (int i = 0; i < 10; i++) _b[i] = Vector3.Lerp(a.B[i], b.B[i], w);
            _r = Vector3.Lerp(a.R, b.R, w);
            _o = Vector3.Lerp(a.O, b.O, w);
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
                for (int i = 0; i < 10; i++)
                {
                    if (_b[i] == Vector3.zero) continue;
                    Transform bone = _animator.GetBoneTransform(Bones[i]);
                    if (bone != null) bone.localRotation = bone.localRotation * Quaternion.Euler(_b[i]);
                }
            }
            if (_visual != null && DragonCombat.OwnsMotionRoot(_token))
            {
                Quaternion q = Quaternion.Euler(_r);
                _visual.localRotation = DragonCombat.MotionBaseRot * q;
                _visual.localPosition = DragonCombat.MotionBasePos + (Pivot - q * Pivot) + _o;
            }
        }

        private void ReleaseRoot()
        {
            if (_visual != null) DragonCombat.ReleaseMotionRoot(_token, _visual);
        }

        private void OnDestroy()
        {
            if (_keys != null) ReleaseRoot();
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
            k.Add(K(0f).Sp(8f, 0f, 0f).Ch(4f, 0f, 0f).RA(-85f, 0f, -75f).RF(-10f, 0f, 0f).LA(-85f, 0f, 75f).LF(-10f, 0f, 0f).Rot(4f, 0f, 0f).Off(0f, -0.05f, 0f));
            float t = 0f, yaw = 0f, q = Mathf.Max(0.08f, turn) * 0.25f;
            while (t < seconds)
            {
                t += q; yaw += 90f;
                k.Add(K(t).Sp(8f, 0f, 0f).Ch(4f, 0f, 0f).RA(-85f, 0f, -75f).RF(-10f, 0f, 0f).LA(-85f, 0f, 75f).LF(-10f, 0f, 0f).Rot(4f, yaw, 0f).Off(0f, -0.05f, 0f).Linear());
            }
            k.Add(K(t + 0.3f).Rot(0f, yaw, 0f));
            PlayClipKeys(player, k.ToArray(), 0.12f);
        }

        public static void PlayClip(Player player, string clip, float windup)
        {
            PlayClip(player, clip, windup, false);
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
                hmPull.Copy(-0.55f).Rot(6f, 0f, 1.5f), hmPull.Copy(-0.4f).Rot(6f, 0f, -1.5f), hmPull.Copy(-0.25f).Rot(6f, 0f, 1.5f), hmPull.Copy(-0.1f).Rot(6f, 0f, -1.5f),
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
                roar, roar.Copy(0.1f).Rot(0f, 0f, 2f), roar.Copy(0.2f).Rot(0f, 0f, -2f), roar.Copy(0.3f).Rot(0f, 0f, 2f), roar.Copy(0.4f).Rot(0f, 0f, -2f), roar.Copy(0.55f),
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
                        euler.z = 1.5f * Mathf.Sin(k * Mathf.PI * 40f) * hold;
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
                    euler.z = 4f * Mathf.Sin(k * Mathf.PI * 14f) * Bump(k);
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
                        euler.z = 5f * Mathf.Sin(k * Mathf.PI * 12f) * lean;
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
                    euler.z = 6f * Mathf.Sin(k * Mathf.PI * 18f) * strike * (1f - recover);
                    break;
                case "erupt":       // crouch and slam the ground, then rise as it erupts
                    offset.y = -0.32f * windup * (1f - strike) + 0.20f * strike * (1f - recover);
                    euler.x = 26f * windup * (1f - strike) - 10f * strike * (1f - recover);
                    break;
                case "nova":        // curl in, then burst open
                    euler.x = 22f * windup * (1f - strike) - 18f * strike * (1f - recover);
                    offset.y = -0.28f * windup * (1f - strike) + 0.10f * strike * (1f - recover);
                    euler.z = 3f * Mathf.Sin(k * Mathf.PI * 30f) * windup * (1f - strike);
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
                // Sorcerer / Archmage / Horizon Walker
                case "flame_burst": preset = "flick"; duration = 0.4f; break;
                case "glacial_descent": preset = "slam"; duration = 0.55f; break;
                case "stonefang_eruption": preset = "erupt"; duration = 0.6f; break;
                case "meteor_fall": preset = "call_down"; duration = 0.7f; break;
                case "gravity_dominion": preset = "pull"; duration = 0.7f; break;
                case "astral_railcannon": preset = "recoil"; duration = 0.6f; break;
                case "astral_greatblade": preset = "slam"; duration = 0.7f; break;
                case "frost_nova": preset = "nova"; duration = 0.55f; break;
                case "elemental_cataclysm": preset = "grand"; duration = 1f; break;
                case "clockwork": preset = "raise"; duration = 0.6f; break;
                case "arcane_phalanx": preset = "cast"; duration = 0.45f; break;
                case "afterimage_arsenal": preset = "flourish"; duration = 0.5f; break;
                case "void_step": preset = "blink"; duration = 0.3f; break;
                case "rift_echo": preset = "rend"; duration = 0.45f; break;
                case "gravity_blast": preset = "push"; duration = 0.5f; break;
                case "arcane_rupture": preset = "grand"; duration = 0.8f; break;
                case "rift_walker": preset = "raise"; duration = 0.5f; break;
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

        public static void ApplySpiritBurnTick(Player attacker, Character target, float damage)
        {
            float amount = ResolveBurnTickDamage(target, damage, true) * GetSorcererMagicDamageMultiplier(attacker);
            ApplyNativeDelayedDamage(attacker, target, "AddSpiritDamage", amount, true);
        }

        public static void ApplyFireBurnTick(Player attacker, Character target, float damage)
        {
            float amount = ResolveBurnTickDamage(target, damage, false) * GetSorcererMagicDamageMultiplier(attacker);
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
            if (attacker != null)
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
