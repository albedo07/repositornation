using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AethelbornExperience
{
    [BepInPlugin("albedo.customclasses.exp", "Aethelborn Ascended - Experience", "0.25.276")]
    [BepInDependency("albedo.customclasses.advanced", BepInDependency.DependencyFlags.HardDependency)]
    public class ExperiencePlugin : BaseUnityPlugin
    {
        private const string LevelKey = "ImmortalHeroes.Level"; // Existing progression key. Do not rename.
        private const string ExpKey = "ImmortalHeroes.EXP";
        private const int MaxLevel = 80;
        private const string AwardRpc = "Aethelborn_Experience_Award";
        private static ExperiencePlugin Instance;
        private Harmony _harmony;
        private ConfigEntry<int> _firstExp, _baseCreatureExp, _healthExpDivisor;
        private ConfigEntry<float> _earlyGrowth, _lateGrowth, _starMultiplier, _bossMultiplier;
        private ConfigEntry<bool> _debug, _showMessages;
        private readonly List<FloatingExp> _floating = new List<FloatingExp>();
        private GUIStyle _expText;
        private static FieldInfo _customData = AccessTools.Field(typeof(Player), "m_customData");
        private static readonly FieldInfo _lastHit = AccessTools.Field(typeof(Character), "m_lastHit");
        private readonly Dictionary<int, LastHit> _recent = new Dictionary<int, LastHit>();
        private readonly HashSet<string> _credited = new HashSet<string>();
        private readonly Dictionary<string, ConfigEntry<int>> _creatureRewards = new Dictionary<string, ConfigEntry<int>>();
        private ZNetScene _scannedScene;
        private int _scannedCount;
        private float _nextHudUpdate;
        private ZRoutedRpc _registeredRpc;

        private struct LastHit { public long Owner; public bool Local; public float When; }
        private struct FloatingExp { public Vector3 Position; public string Text; public float Started; }

        private void Awake()
        {
            Instance = this;
            _firstExp = Config.Bind("Level Curve", "EXPLevel1", 600, "EXP needed from level 1 to 2.");
            _earlyGrowth = Config.Bind("Level Curve", "GrowthThroughLevel16", 1.25f, "Multiplier for level requirements up to level 16.");
            _lateGrowth = Config.Bind("Level Curve", "GrowthAfterLevel16", 1.05f, "Multiplier after level 16 (cap: 80).");
            _baseCreatureExp = Config.Bind("Creature EXP", "AutomaticBaseEXP", 20, "Default base EXP for a newly detected creature.");
            _healthExpDivisor = Config.Bind("Creature EXP", "HealthPerExtraEXP", 5, "Extra EXP = max HP divided by this value.");
            _starMultiplier = Config.Bind("Creature EXP", "ExtraEXPPerStar", 0.5f, "Bonus multiplier per star (+50% by default).");
            _bossMultiplier = Config.Bind("Creature EXP", "BossEXPMultiplier", 2f, "Additional multiplier for bosses.");
            _showMessages = Config.Bind("General", "ShowEXPMessage", true, "Show EXP gained and level-up messages.");
            _debug = Config.Bind("General", "Debug", true, "Log creature IDs and EXP awards.");
            _harmony = new Harmony("albedo.customclasses.exp");
            MethodInfo damage = AccessTools.Method(typeof(Character), "RPC_Damage", new Type[] { typeof(long), typeof(HitData) });
            MethodInfo death = AccessTools.Method(typeof(Character), "OnDeath");
            if (death == null)
            {
                Logger.LogError("EXP disabled: Character.OnDeath was not found.");
                return;
            }
            if (damage != null) _harmony.Patch(damage, prefix: new HarmonyMethod(typeof(ExperiencePlugin), "DamagePrefix"));
            else Logger.LogWarning("RPC_Damage hook unavailable; falling back to final-hit attribution.");
            _harmony.Patch(death, prefix: new HarmonyMethod(typeof(ExperiencePlugin), "DeathPrefix"));
            RegisterRpc();
            Logger.LogInfo("Experience plugin ready: 600 EXP, x1.25 to level 16, x1.05 afterwards; level cap 80.");
        }

        private void OnDestroy() { if (_harmony != null) _harmony.UnpatchSelf(); if (Instance == this) Instance = null; }

        private void RegisterRpc()
        {
            if (ZRoutedRpc.instance == null || _registeredRpc == ZRoutedRpc.instance) return;
            ZRoutedRpc.instance.Register<ZPackage>(AwardRpc, new Action<long, ZPackage>(ReceiveAward));
            _registeredRpc = ZRoutedRpc.instance;
        }

        // Register known creature prefabs once, and discover late-added ones on the next update.
        private void Update()
        {
            RegisterRpc();
            if (Time.time >= _nextHudUpdate)
            {
                _nextHudUpdate = Time.time + 0.5f;
                IDictionary progress = Data(Player.m_localPlayer);
                if (progress != null)
                {
                    int level = Math.Max(1, Math.Min(MaxLevel, ReadInt(progress, LevelKey, 1)));
                    double needed = Required(level);
                    float ratio = level >= MaxLevel ? 1f : Mathf.Clamp01((float)(ReadLong(progress, ExpKey) / needed));
                    progress["ImmortalHeroes.EXPRatio"] = ratio.ToString("R", CultureInfo.InvariantCulture);
                    progress["ImmortalHeroes.EXPRequired"] = level >= MaxLevel ? "0" : Required(level).ToString(CultureInfo.InvariantCulture);
                }
            }
            for (int i = _floating.Count - 1; i >= 0; i--)
                if (Time.time - _floating[i].Started > 1.6f) _floating.RemoveAt(i);
            ZNetScene scene = ZNetScene.instance;
            if (scene == null || scene.m_prefabs == null) return;
            if (_scannedScene != scene) { _scannedScene = scene; _scannedCount = 0; _credited.Clear(); _recent.Clear(); }
            if (scene.m_prefabs.Count == _scannedCount) return;
            _scannedCount = scene.m_prefabs.Count;
            foreach (GameObject prefab in scene.m_prefabs)
            {
                if (prefab == null) continue;
                try
                {
                    Character character = prefab.GetComponent<Character>();
                    if (character != null && prefab.GetComponent<Player>() == null) Discover(prefab.name, character, true);
                }
                catch (Exception error)
                {
                    if (_debug.Value) Logger.LogWarning("EXP prefab scan skipped " + prefab.name + ": " + error.Message);
                }
            }
        }

        // Capture the responsible player on the enemy's owning peer before damage is processed.
        private static void DamagePrefix(Character __instance, HitData hit)
        {
            if (Instance == null || __instance == null || __instance is Player || hit == null) return;
            ZNetView view = __instance.GetComponent<ZNetView>();
            if (view != null && view.IsValid() && !view.IsOwner()) return;
            Player attacker = hit.GetAttacker() as Player;
            if (attacker == null) return;
            long owner = 0L;
            bool local = attacker == Player.m_localPlayer;
            if (local && ZNet.instance != null) owner = ZNet.GetUID();
            if (!local)
            {
                ZNetView attackerView = attacker.GetComponent<ZNetView>();
                if (attackerView != null && attackerView.IsValid()) owner = attackerView.GetZDO().GetOwner();
            }
            Instance._recent[__instance.GetInstanceID()] = new LastHit { Owner = owner, Local = local, When = Time.time };
        }

        // Prefix: the last-hit data and the corpse position still exist before Valheim destroys the creature.
        private static void DeathPrefix(Character __instance)
        {
            if (Instance == null || __instance == null || __instance is Player) return;
            try { Instance.AwardDeath(__instance); }
            catch (Exception error) { Instance.Logger.LogError("EXP death handler failed: " + error); }
        }

        private void AwardDeath(Character victim)
        {
            ZNetView view = victim.GetComponent<ZNetView>();
            if (view != null && view.IsValid() && !view.IsOwner()) return;
            string victimId = view != null && view.IsValid() ? view.GetZDO().m_uid.ToString() : victim.GetInstanceID().ToString();
            if (!_credited.Add("kill:" + victimId)) return;
            if (IsTamed(victim)) return;
            LastHit last;
            bool found = _recent.TryGetValue(victim.GetInstanceID(), out last) && Time.time - last.When < 60f;
            _recent.Remove(victim.GetInstanceID());
            // Read Valheim's protected final-hit field without depending on its C# accessibility.
            HitData finalHit = !found && _lastHit != null ? _lastHit.GetValue(victim) as HitData : null;
            if (!found && finalHit != null)
            {
                Player killer = finalHit.GetAttacker() as Player;
                if (killer != null)
                {
                    last.Local = killer == Player.m_localPlayer;
                    ZNetView killerView = killer.GetComponent<ZNetView>();
                    last.Owner = last.Local && ZNet.instance != null ? ZNet.GetUID() :
                        killerView != null && killerView.IsValid() ? killerView.GetZDO().GetOwner() : 0L;
                    found = true;
                }
            }
            string prefab = victim.gameObject.name.Replace("(Clone)", "").Trim();
            if (!found || (!last.Local && last.Owner == 0L))
            {
                if (_debug.Value) Logger.LogWarning("EXP kill not credited: " + prefab + " (player attacker not resolved)");
                return;
            }
            int baseExp = Discover(prefab, victim).Value;
            if (baseExp <= 0) return;
            int stars = Math.Max(0, victim.GetLevel() - 1);
            double reward = baseExp * (1d + stars * Math.Max(0f, _starMultiplier.Value));
            if (victim.IsBoss()) reward *= Math.Max(0f, _bossMultiplier.Value);
            int value = (int)Math.Min(int.MaxValue, Math.Max(1d, Math.Round(reward, MidpointRounding.AwayFromZero)));
            Vector3 position = victim.transform.position + Vector3.up * Math.Max(1.5f, victim.GetMaxHealth() > 500f ? 3f : 1.8f);
            if (_debug.Value) Logger.LogInfo("EXP kill " + prefab + " -> " + value + " (local=" + last.Local + ", owner=" + last.Owner + ")");
            if (last.Local)
            {
                GiveExperience(victimId, value, position); // Local kills never depend on networking/RPC availability.
                return;
            }
            RegisterRpc();
            if (ZRoutedRpc.instance == null)
            {
                Logger.LogWarning("EXP reward not delivered: network RPC unavailable");
                return;
            }
            ZPackage pkg = new ZPackage();
            pkg.Write(victimId);
            pkg.Write(value);
            pkg.Write(position.x); pkg.Write(position.y); pkg.Write(position.z);
            ZRoutedRpc.instance.InvokeRoutedRPC(last.Owner, AwardRpc, pkg);
        }

        private void OnGUI()
        {
            if (useGUILayout) useGUILayout = false;   // v0.25.185 (perf): no GUILayout here - skip IMGUI's layout pass
            if (_floating.Count == 0 || Camera.main == null) return;
            if (_expText == null)
            {
                _expText = new GUIStyle(GUI.skin.label);
                _expText.alignment = TextAnchor.MiddleCenter;
                _expText.fontSize = 21;
                _expText.fontStyle = FontStyle.Bold;
            }
            Camera camera = Camera.main;
            for (int i = 0; i < _floating.Count; i++)
            {
                FloatingExp item = _floating[i];
                float elapsed = Time.time - item.Started;
                Vector3 at = item.Position + Vector3.up * elapsed * 0.55f;
                Vector3 screen = camera.WorldToScreenPoint(at);
                if (screen.z <= 0f) continue;
                Rect rect = new Rect(screen.x - 85f, Screen.height - screen.y - 16f, 170f, 32f);
                _expText.normal.textColor = new Color(0f, 0f, 0f, 0.85f * Mathf.Clamp01(1.6f - elapsed));
                GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), item.Text, _expText);
                _expText.normal.textColor = new Color(0.25f, 0.72f, 1f, Mathf.Clamp01(1.6f - elapsed));
                GUI.Label(rect, item.Text, _expText);
            }
        }

        private ConfigEntry<int> Discover(string name, Character creature, bool prefab = false)
        {
            string key = Regex.Replace(name, "[^a-zA-Z0-9_-]", "_");
            if (key.Length == 0) key = "UnknownCreature";
            ConfigEntry<int> cfg;
            if (_creatureRewards.TryGetValue(key, out cfg)) return cfg;
            int hp = creature == null ? 0 : (int)Math.Max(0f, (prefab ? creature.GetMaxHealthBase() : creature.GetMaxHealth()));
            int rate = Math.Max(1, _healthExpDivisor.Value);
            int oldAuto = Math.Min(100000, Math.Max(1, _baseCreatureExp.Value + hp / rate));
            int biome = CreatureProgression(key);
            int auto = key.Equals("Troll", StringComparison.OrdinalIgnoreCase) ? 450 :
                Math.Min(100000, oldAuto * biome);
            cfg = Config.Bind("Creature Rewards", key, auto, "Base EXP per kill. 0 = no EXP. Stars and bosses have separate multipliers.");
            // Alpha 1 wrote the old auto-generated Troll reward (140). Migrate only that default.
            if (key.Equals("Troll", StringComparison.OrdinalIgnoreCase) && cfg.Value == 140) cfg.Value = 450;
            // Upgrade only the old health-only default; preserve disabled and custom rewards.
            else if (biome > 1 && cfg.Value == oldAuto) cfg.Value = auto;
            _creatureRewards[key] = cfg;
            if (_debug.Value) Logger.LogInfo("EXP prefab discovered: " + key + " = " + cfg.Value);
            return cfg;
        }

        private static int CreatureProgression(string key)
        {
            switch (key.ToLowerInvariant())
            {
                case "greydwarf": case "greydwarf_elite": case "greydwarf_shaman":
                case "skeleton": case "skeleton_poison": case "rancidremains": return 2;
                case "draugr": case "draugr_elite": case "draugr_ranged": case "blob":
                case "blobelite": case "leech": case "wraith": case "surtling": case "abomination": return 3;
                case "wolf": case "fenring": case "hatchling": case "stonegolem":
                case "bat": case "cultist": case "ulv": return 4;
                case "goblin": case "goblinbrute": case "goblinshaman":
                case "deathsquito": case "lox": case "blobtar": return 6;
                case "seeker": case "seekerbrood": case "seekerbrute": case "gjall":
                case "tick": case "dverger": case "dvergermage": case "dvergermagefire":
                case "dvergermageice": case "dvergermageheal": return 8;
                case "charred_melee": case "charred_ranged": case "charred_mage":
                case "charred_twitcher": case "morgen": case "asksvin": case "fallenvalkyrie":
                case "volture": case "bonemawserpent": case "bloblava": return 10;
                default: return 1; // Unknown/modded creatures keep their existing configurable fallback.
            }
        }

        private static bool IsTamed(Character victim)
        {
            MethodInfo check = AccessTools.Method(victim.GetType(), "IsTamed", Type.EmptyTypes);
            return check != null && (bool)check.Invoke(victim, null);
        }

        private static IDictionary Data(Player player)
        {
            return player != null && _customData != null ? _customData.GetValue(player) as IDictionary : null;
        }
        private static string Read(IDictionary data, string key)
        {
            return data != null && data.Contains(key) && data[key] != null ? data[key].ToString() : "";
        }
        private static int ReadInt(IDictionary data, string key, int fallback)
        {
            int value; return int.TryParse(Read(data, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }
        private static long ReadLong(IDictionary data, string key)
        {
            long value; return long.TryParse(Read(data, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? Math.Max(0L, value) : 0L;
        }

        private void ReceiveAward(long sender, ZPackage pkg)
        {
            Player player = Player.m_localPlayer;
            if (player == null || pkg == null) return;
            // Sender owns the defeated creature. Each corpse ID is credited once to this character.
            string victim = pkg.ReadString();
            int amount = pkg.ReadInt();
            Vector3 position = new Vector3(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
            GiveExperience(victim, amount, position);
        }

        private void GiveExperience(string victim, int amount, Vector3 position)
        {
            Player player = Player.m_localPlayer;
            if (player == null || amount <= 0 || amount > 2000000) return;
            IDictionary data = Data(player);
            if (data == null) { Logger.LogError("EXP: player m_customData unavailable; reward not applied"); return; }
            if (!_credited.Add("award:" + victim)) return;
            int level = Math.Max(1, Math.Min(MaxLevel, ReadInt(data, LevelKey, 1)));
            if (level >= MaxLevel) return;
            long exp = ReadLong(data, ExpKey) + amount;
            long gained = 0;
            while (level < MaxLevel && exp >= Required(level))
            {
                exp -= Required(level);
                level++;
                gained++;
            }
            data[LevelKey] = level.ToString(CultureInfo.InvariantCulture);
            data[ExpKey] = (level >= MaxLevel ? "0" : exp.ToString(CultureInfo.InvariantCulture));
            data["ImmortalHeroes.EXPRatio"] = (level >= MaxLevel ? 1f : (float)(exp / (double)Required(level))).ToString("R", CultureInfo.InvariantCulture);
            data["ImmortalHeroes.EXPRequired"] = level >= MaxLevel ? "0" : Required(level).ToString(CultureInfo.InvariantCulture);
            if (_showMessages.Value)
            {
                string message = "+" + amount + " EXP";
                if (gained > 0) message += "  LEVEL UP! Lv " + level;
                player.Message(MessageHud.MessageType.TopLeft, message);
                _floating.Add(new FloatingExp { Position = position, Text = "+" + amount + " EXP", Started = Time.time });
            }
            if (_debug.Value) Logger.LogInfo("EXP award: " + amount + ", level " + level + ", remainder " + exp);
        }

        private long Required(int level)
        {
            // Level 1 -> 2 costs 600; Level 16 -> 17 begins the x1.05 phase.
            double required = Math.Max(1, _firstExp.Value);
            for (int at = 2; at <= level; at++)
                required = Math.Round(required * (at <= 16 ? Math.Max(1f, _earlyGrowth.Value) : Math.Max(1f, _lateGrowth.Value)), MidpointRounding.AwayFromZero);
            return (long)Math.Max(1d, required);
        }
    }
}
