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
        public const string ModVersion = "0.21.2";

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
        internal ConfigEntry<float> ZapDamage;
        internal ConfigEntry<bool> BurnsUseCurrentHpPercent;
        internal ConfigEntry<float> FireBurnCurrentHpPercent;
        internal ConfigEntry<float> SpiritBurnMultiplier;
        internal ConfigEntry<float> MinimumBurnTick;
        internal ConfigEntry<float> WarriorHyperArmorThreshold;
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
            SorcererMagicDamageBonus = Config.Bind("Sorcerer Blessing", "MagicDamagePercent", 30f, "Arcane Blood Magic Damage bonus. Applies to Eitr-based Sorcerer magic, including magical physical portions and magic-weapon attacks.");
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
            ZapDamage = Config.Bind("Debuffs", "ZapLightningDamage", 25f, "Testing/default lightning damage for Zap because the framework does not specify an amount.");
            BurnsUseCurrentHpPercent = Config.Bind("Damage Over Time", "LegacyBurnsUseCurrentHpPercent_v0212", false, "Legacy: burns now deal the skill's own burn damage. True = old 3% CURRENT HP burns.");
            FireBurnCurrentHpPercent = Config.Bind("Damage Over Time", "FireBurnCurrentHpPercentPerTick", 3f, "Fire Burn = 3 percent of CURRENT HP per tick.");
            SpiritBurnMultiplier = Config.Bind("Damage Over Time", "SpiritBurnMultiplierVsFire", 1.5f, "Spirit Burn remains 1.5x stronger than Fire.");
            MinimumBurnTick = Config.Bind("Damage Over Time", "MinimumBurnTickDamage", 1f, "Minimum percentage Burn tick.");
            WarriorHyperArmorThreshold = Config.Bind("Warrior Blessing", "HyperArmorHitThresholdPercent", 30f, "Warrior ignores stagger/pushback when an incoming hit is below this percent of max HP.");
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

            if (DragonCombat.GetClassName(__instance) == "Sorcerer" &&
                (currentAdvancement == "Wizard" || currentAdvancement == "Spellcaster") &&
                DragonCombat.IsStaffWeapon(currentWeapon))
            {
                // Staves occupy the caster's hands and cannot block. Wands are
                // deliberately excluded so a shield + Wand setup can still block.
                block = false;
                blockHold = false;
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
            if (__result >= 0f || Instance == null || __instance == null || DragonCombat.GetClassName(__instance) != "Cleric")
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

    public static class DragonCombat
    {
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
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type type = assemblies[i].GetType("Jotunn.Managers.GUIManager", false);
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

        public static bool IsGameplayHudSuppressed()
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
                Type type = null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < assemblies.Length && type == null; a++)
                    type = assemblies[a].GetType(typeName, false);
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
                Type type = null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < assemblies.Length && type == null; a++)
                    type = assemblies[a].GetType("Hud", false);
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
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type type = assemblies[i].GetType(typeName, false);
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
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type type = assemblies[i].GetType(typeName, false);
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
                radius = DragonCombatPlugin.Instance.ZapRadius.Value;

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
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type type = assemblies[i].GetType(
                        "AlbedosCustomClassesSkills.SkillsPlugin",
                        false
                    );

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

        public static float GetMovementModifier(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null)
                return 0f;

            try
            {
                FieldInfo field = item.m_shared.GetType().GetField("m_movementModifier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

        public static DamagePatchState BeginDamage(Character target, HitData hit)
        {
            DamagePatchState patchState = new DamagePatchState();
            if (target == null || hit == null)
                return patchState;

            Player attacker = hit.GetAttacker() as Player;
            if (attacker != null)
            {
                float outgoingBonus = GetTimedBuffSum(attacker, "AttackDamage");
                if (outgoingBonus != 0f)
                    hit.m_damage.Modify(Mathf.Max(0f, 1f + outgoingBonus));

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
                    float threshold = DragonCombatPlugin.Instance == null ? 0.30f : Mathf.Clamp01(DragonCombatPlugin.Instance.WarriorHyperArmorThreshold.Value / 100f);
                    float raw = Mathf.Max(0f, hit.GetTotalDamage());
                    if (raw > 0f && raw < targetPlayer.GetMaxHealth() * threshold)
                        HitHyperArmorUntil[targetPlayer.GetInstanceID()] = Time.time + 0.20f;
                }
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
                FieldInfo field = typeof(HitData).GetField("m_skill", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

        private static string ReadPlayerData(Player player, string key)
        {
            if (player == null)
                return "";
            try
            {
                FieldInfo field = typeof(Player).GetField("m_customData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
}
