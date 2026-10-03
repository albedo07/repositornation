using System;using System.Collections;using System.Collections.Generic;
class Player { public static Player m_localPlayer; public Hashtable data = new Hashtable(); public bool IsDead(){return false;} }
class ConfigEntry<T> { public T Value; public ConfigEntry(T value){Value=value;} }
class Mathf { public static int Clamp(int n,int a,int b){return Math.Max(a,Math.Min(b,n));} public static int FloorToInt(float n){return (int)Math.Floor(n);} public static float Max(float a,float b){return Math.Max(a,b);} }
struct Rect {public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} }
class SkillsPlugin {public static SkillsPlugin Instance=new SkillsPlugin();public void CastFromHotbar(Player p,string id){Harness.Casted=id;}public float GetCooldownForUi(string id){return 4f;} }
class Harness {
public static string Casted="";
private enum TreeNodeKind {ClassNormal,Ascended,Buff,Signature,Grace,AdvancementNormal,Ultimate}
private class ReferenceNodeUi {public string Id;public Rect GroupRect,IconRect;public string Hotkey;public TreeNodeKind Kind;public bool Mandatory;public int MaxTier;public string TooltipTitle,TooltipBody;public ReferenceNodeUi(string i,Rect g,Rect r,string h,TreeNodeKind k,bool m,int t,string tt,string tb){Id=i;GroupRect=g;IconRect=r;Hotkey=h;Kind=k;Mandatory=m;MaxTier=t;TooltipTitle=tt;TooltipBody=tb;}}
const string ClassDataKey="AlbedoCustomClasses.Class",AdvancementDataKey="AlbedoCustomClasses.Advancement";
ConfigEntry<bool> _ihUnlockAll=new ConfigEntry<bool>(false);
string _ihTierCacheRaw,_ihAscCacheRaw,_hotbarLayoutOwnerKey="",_treeSelectedNodeId="",_ihPreviewBranch="Paladin";
Dictionary<string,int> _ihTierCache=new Dictionary<string,int>(),_treePrototypePending=new Dictionary<string,int>();
HashSet<string> _ihAscCache=new HashSet<string>();
static System.Text.RegularExpressions.Regex _ihNumberRegex;
bool _shieldChargeActive=false;
string GetClass(Player p){return ReadPlayerData(p,ClassDataKey);}string GetAdvancement(Player p){return ReadPlayerData(p,AdvancementDataKey);}
string ReadPlayerData(Player p,string k){return p==null?"":(p.data[k] as string ?? "");}IDictionary GetCustomData(Player p){return p==null?null:p.data;}
void ShowMessage(string s){} bool IsAscendedSkill(string id){return IhIsAscended(Player.m_localPlayer,id);} bool IhCanCast(Player p,string id){return IhIsUnlocked(p,id);}
float GetCooldownRemaining(string id){Casted=id;return 5f;}
string IhHex(float a,float b,float c){return "#TEST";}
void CastGoddessRelic(Player p){Casted="CastGoddessRelic";}
void CastJudgementHammer(Player p){Casted="CastJudgementHammer";}
void CastShieldCharge(Player p){Casted="CastShieldCharge";}
void CastFallenAngel(Player p){Casted="CastFallenAngel";}
void CastRayOfHope(Player p){Casted="CastRayOfHope";}
void CastElectricSmite(Player p){Casted="CastElectricSmite";}
void CastHeavensLight(Player p){Casted="CastHeavensLight";}
void CastLightningRelic(Player p){Casted="CastLightningRelic";}
void CastHolyRelic(Player p){Casted="CastHolyRelic";}
void CastDivineIntervention(Player p){Casted="CastDivineIntervention";}
void CastGrandCross(Player p){Casted="CastGrandCross";}
void CastHeavensJudgement(Player p){Casted="CastHeavensJudgement";}
void CastLightningTempest(Player p){Casted="CastLightningTempest";}
void ActivateGrandSigil(Player p){Casted="ActivateGrandSigil";}
private const string HotbarLayoutKeyPrefix = "ImmortalHeroes.HotbarLayout.";
private static readonly string[] DefaultClericPaladinHotbar =
            { "righteous_strike", "goddess_relic", "judgement_hammer", "shield_charge", "ray_of_hope", "holy_wave", "electric_smite" };
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
private string IhTreeBranch()
        {
            string branch = GetAdvancement(Player.m_localPlayer);
            return branch == "Priest" || branch == "Paladin" ? branch : _ihPreviewBranch;
        }
private bool IhIsPriest(Player player)
        {
            return GetClass(player) == "Cleric" && GetAdvancement(player) == "Priest";
        }
private bool IhIsClericAdvanced(Player player)
        {
            return IhIsPaladin(player) || IhIsPriest(player);
        }
private static bool IhPriestSkill(string id)
        {
            return IhContains(IhPriestAdvSkills, id) || id == IhPriestUltimate || id == IhPriestGrace;
        }
private static bool IhIsUltimate(string id)
        {
            return id == IhUltimate || id == IhPriestUltimate;
        }
private string[] IhBranchSkills(Player player)
        {
            return IhIsPriest(player) ? IhPriestAdvSkills : IhAdvSkills;
        }
private string IhGraceFor(Player player)
        {
            return IhIsPriest(player) ? IhPriestGrace : IhGrace;
        }
private ReferenceNodeUi[] IhTreeNodes()
        {
            return IhTreeBranch() == "Priest" ? ClericPriestReferenceNodes : ClericPaladinReferenceNodes;
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
            if (IhContains(IhClassSkills, id)) return 7;
            if (IhContains(IhAdvSkills, id) || IhContains(IhPriestAdvSkills, id)) return 5;
            if (IhIsUltimate(id)) return 3;
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
private int IhClassPointsEarned(Player player)
        {
            int level = IhGetLevel(player);
            return Mathf.Clamp(2 * (level / 2) - 2, 0, 14) + IhReadInt(player, IhBonusClassKey, 0);
        }
private int IhAdvPointsEarned(Player player)
        {
            if (string.IsNullOrEmpty(GetAdvancement(player)))
                return 0;
            int level = IhGetLevel(player);
            return Mathf.Clamp((level - 16) / 2, 0, 20) + IhReadInt(player, IhBonusAdvKey, 0);
        }
private static int IhGateSpend(int gateLevel)
        {
            int earned = Mathf.Clamp((gateLevel - 16) / 2, 0, 20);
            return Mathf.FloorToInt(earned * 0.8f + 0.5f);
        }
private static int IhGateLevel(string id)
        {
            if (id == "shield_charge" || id == "divine_intervention") return 24;
            if (id == "fallen_angel" || id == "ray_of_hope" || id == "grand_cross" || id == "heavens_judgement") return 32;
            if (IhIsUltimate(id)) return 36;
            return 16;
        }
private bool IhIsUnlocked(Player player, string id, out string reason)
        {
            reason = "";
            if (player == null) return false;
            if (GetClass(player) != "Cleric")
            {
                reason = "Choose the Cleric Class at the Altar";
                return false;
            }
            if (IhContains(IhClassSkills, id)) return true;
            bool priest = IhPriestSkill(id);
            if (!priest && !IhContains(IhAdvSkills, id) && id != IhGrace && id != IhUltimate)
            {
                reason = "Unknown skill";
                return false;
            }
            string branch = priest ? "Priest" : "Paladin";
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
private bool IhIsAscended(Player player, string id)
        {
            if (player == null)
                return false;
            if (id == IhAscendedClassSkill && IhIsPaladin(player))
                return true;
            return !IhPriestSkill(id) && IhIsPaladin(player) && IhAscendedSet(player).Contains(id);
        }
private bool IhTryAscend(Player player, string id, out string message)
        {
            return IhCheckAscend(player, id, true, out message);
        }
private bool IhCheckAscend(Player player, string id, bool apply, out string message)
        {
            message = "";
            int level = IhGetLevel(player);
            HashSet<string> set = new HashSet<string>(IhAscendedSet(player));
            if (!IhIsPaladin(player))
            {
                message = IhIsPriest(player) ? "Priest Ascended variants are not defined in this build. Its normal Tiers and Ultimate Tiers remain available."
                    : "Only an Advanced Paladin can Ascend Paladin skills.";
                return false;
            }
            if (id == IhAscendedClassSkill)
            {
                message = "Righteous Strike is already Ascended (it Ascends when you Advance).";
                return false;
            }
            string[] group;
            int needLevel;
            int needTier;
            if (IhContains(IhSignatureSkills, id)) { group = IhSignatureSkills; needLevel = 32; needTier = 5; }
            else if (IhContains(IhNormalAdvSkills, id)) { group = IhNormalAdvSkills; needLevel = 42; needTier = 5; }
            else if (id == IhUltimate) { group = new string[] { IhUltimate }; needLevel = 50; needTier = 3; }
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
private void IhApplyLevel(Player player, int level, List<string> notes)
        {
            level = Mathf.Clamp(level, 1, IhMaxLevel);
            IhWrite(player, IhLevelKey, level.ToString());
            HashSet<string> set = new HashSet<string>(IhAscendedSet(player));
            if (level < 16 && !string.IsNullOrEmpty(GetAdvancement(player)))
            {
                IhClearTiers(player, IhAdvSkills);
                IhClearTiers(player, IhPriestAdvSkills);
                IhWrite(player, AdvancementDataKey, "");
                set.Clear();
                notes.Add("Below Lv 16: Advancement annulled.");
            }
            if (level < 50 && set.Remove(IhUltimate)) notes.Add("Below Lv 50: Ultimate Ascension annulled.");
            for (int i = 0; i < IhNormalAdvSkills.Length; i++)
                if (level < 42 && set.Remove(IhNormalAdvSkills[i])) notes.Add("Below Lv 42: " + IhSkillName(IhNormalAdvSkills[i]) + " Ascension annulled.");
            for (int i = 0; i < IhSignatureSkills.Length; i++)
                if (level < 32 && set.Remove(IhSignatureSkills[i])) notes.Add("Below Lv 32: " + IhSkillName(IhSignatureSkills[i]) + " Ascension annulled.");
            IhSetAscended(player, set);
            IhEnforcePools(player, notes);
            _hotbarLayoutOwnerKey = "";
        }
private void IhEnforcePools(Player player, List<string> notes)
        {
            if (IhSpent(player, IhClassSkills) > IhClassPointsEarned(player))
            {
                IhClearTiers(player, IhClassSkills);
                notes.Add("Class Tiers reset (more points spent than earned).");
            }
            if (IhSpent(player, IhBranchSkills(player)) > IhAdvPointsEarned(player))
            {
                IhClearTiers(player, IhBranchSkills(player));
                HashSet<string> set = new HashSet<string>(IhAscendedSet(player));
                for (int i = 0; i < IhAdvSkills.Length; i++)
                    set.Remove(IhAdvSkills[i]);
                IhSetAscended(player, set);
                notes.Add(GetAdvancement(player) + " Tiers reset (more points spent than earned).");
            }
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
                case "fallen_angel": return "Fallen Angel";
                case "ray_of_hope": return "Ray of Hope";
                case "electric_smite": return "Electric Smite";
                case "heavens_light": return "Heaven's Light";
                case "lightning_relic": return "Lightning Relic";
                case "holy_relic": return "Holy Relic";
                case "divine_intervention": return "Divine Intervention";
                case "grand_cross": return "Grand Cross";
                case "heavens_judgement": return "Heaven's Judgement";
                case "lightning_tempest": return "Lightning Tempest";
                case "grand_sigil": return "Grand Sigil";
            }
            return id;
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
private int IhPendingSum(string[] ids)
        {
            int total = 0;
            for (int i = 0; i < ids.Length; i++)
                total += GetPrototypePending(ids[i]);
            return total;
        }
private bool IhCanQueueTier(Player player, string id)
        {
            if (player == null || !IhIsUnlocked(player, id))
                return false;
            if (IhContains(IhClassSkills, id))
            {
                if (!string.IsNullOrEmpty(GetAdvancement(player)))
                    return false; // the Class tree locks after Advancement
                return IhClassPointsEarned(player) - IhSpent(player, IhClassSkills) - IhPendingSum(IhClassSkills) > 0;
            }
            if (IhContains(IhBranchSkills(player), id))
                return IhAdvPointsEarned(player) - IhSpent(player, IhBranchSkills(player)) - IhPendingSum(IhBranchSkills(player)) > 0;
            return false;
        }
private bool IhUsesTreeHotbar(Player player)
        {
            if (player == null || GetClass(player) != "Cleric")
                return false;
            string advancement = GetAdvancement(player);
            return string.IsNullOrEmpty(advancement) || advancement == "Paladin" || advancement == "Priest";
        }
private void IhCastSkill(Player player, string id)
        {
            if (player == null || player.IsDead() || !IhCanCast(player, id))
                return;
            switch (id)
            {
                case "lightning_zap":
                case "righteous_strike":
                case "holy_wave":
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
                case "holy_wave": return skills == null ? 0f : skills.GetCooldownForUi("Cleric.HolyWave");
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
            }
            return 0f;
        }
private bool IhAdvanceChecklist(Player player, List<string> lines)
        {
            int level = IhGetLevel(player);
            int rsTier = IhGetTier(player, IhAscendedClassSkill);
            int spent = IhSpent(player, IhClassSkills);
            bool lv = level >= 16;
            bool rs = rsTier >= 7;
            bool pts = spent >= 14;
            lines.Add((lv ? "+ " : "- ") + "Lv 16  (" + level.ToString() + ")");
            lines.Add((rs ? "+ " : "- ") + "Righteous Strike Tier 7  (" + rsTier.ToString() + "/7)");
            lines.Add((pts ? "+ " : "- ") + "14 Class Tier Points spent  (" + spent.ToString() + "/14)");
            return lv && rs && pts;
        }
private void IhAdvanceToPaladin(Player player)
        {
            if (player == null || GetClass(player) != "Cleric" || !string.IsNullOrEmpty(GetAdvancement(player))) return;
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
            ShowMessage("Advanced to " + branch + (branch == "Paladin" ? "! Righteous Strike has Ascended." : "!"));
        }
private string IhColorize(string text)
        {
            if (_ihNumberRegex == null)
                _ihNumberRegex = new System.Text.RegularExpressions.Regex(
                    @"(?<![A-Za-z0-9_])[+-]?\d+(?:[.,]\d+)?(?:[ \t]*(?:%|°|(?:m/s|HP/s|HP|min|ms|m|s|x)\b|/s\b))?");
            return _ihNumberRegex.Replace(text, new System.Text.RegularExpressions.MatchEvaluator(IhColorMatch));
        }
private string IhColorMatch(System.Text.RegularExpressions.Match match)
        {
            return "<color=" + IhHex(0.42f, 0.88f, 1f) + ">" + match.Value + "</color>";
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
                "ATTACK - FALLEN ANGEL", "Paladin Advancement skill. Optional hotbar skill."),
            new ReferenceNodeUi("ray_of_hope", new Rect(672f, 287f, 103f, 105f), new Rect(686f, 290f, 69f, 69f), "5", TreeNodeKind.Buff, false, 5,
                "BUFF - RAY OF HOPE", "Paladin support skill. Optional hotbar skill."),

            new ReferenceNodeUi("electric_smite", new Rect(831f, 204f, 133f, 165f), new Rect(850f, 214f, 103f, 108f), "7", TreeNodeKind.Ultimate, true, 3,
                "ULTIMATE - ELECTRIC SMITE", "Acrobatic landing followed by sixteen 10m Ground Projectile Lightning Trails with Persistent Damage.")
        };
private static readonly ReferenceNodeUi[] ClericPriestReferenceNodes =
        {
            ClericPaladinReferenceNodes[0], ClericPaladinReferenceNodes[1], ClericPaladinReferenceNodes[2],
            new ReferenceNodeUi("lightning_relic", new Rect(357f,151f,103f,105f),new Rect(369f,156f,70f,65f),"",TreeNodeKind.Signature,true,5,"ATTACK - LIGHTNING RELIC",""),
            new ReferenceNodeUi("holy_relic",new Rect(357f,287f,103f,105f),new Rect(369f,288f,70f,63f),"",TreeNodeKind.Signature,true,5,"BUFF - HOLY RELIC",""),
            new ReferenceNodeUi("grand_sigil",new Rect(357f,413f,103f,105f),new Rect(380f,409f,74f,67f),"",TreeNodeKind.Grace,true,0,"GRACE - GRAND SIGIL",""),
            new ReferenceNodeUi("divine_intervention",new Rect(520f,151f,126f,105f),new Rect(548f,156f,68f,65f),"",TreeNodeKind.AdvancementNormal,false,5,"SUPPORT - DIVINE INTERVENTION",""),
            new ReferenceNodeUi("grand_cross",new Rect(672f,151f,103f,105f),new Rect(686f,156f,69f,65f),"",TreeNodeKind.AdvancementNormal,false,5,"ATTACK - GRAND CROSS",""),
            new ReferenceNodeUi("heavens_judgement",new Rect(663f,287f,120f,105f),new Rect(686f,288f,69f,63f),"",TreeNodeKind.AdvancementNormal,false,5,"ATTACK - HEAVEN'S JUDGEMENT",""),
            new ReferenceNodeUi("lightning_tempest",new Rect(831f,204f,133f,165f),new Rect(850f,214f,103f,108f),"",TreeNodeKind.Ultimate,true,3,"ULTIMATE - LIGHTNING TEMPEST","")
        };
private ReferenceNodeUi FindReferenceNode(string id)
        {
            for (int i = 0; i < ClericPaladinReferenceNodes.Length; i++)
                if (ClericPaladinReferenceNodes[i].Id == id) return ClericPaladinReferenceNodes[i];
            for (int i = 3; i < ClericPriestReferenceNodes.Length; i++)
                if (ClericPriestReferenceNodes[i].Id == id) return ClericPriestReferenceNodes[i];
            return null;
        }
private bool IsPermanentHotbarSkill(string id)
        {
            ReferenceNodeUi node = FindReferenceNode(id);
            if (node == null || !node.Mandatory || node.Kind == TreeNodeKind.Grace)
                return false;
            // v0.18.0: Signature / Ascended / Ultimate are permanent once unlocked; before
            // Advancement Righteous Strike is a normal, interchangeable Class skill.
            Player player = Player.m_localPlayer;
            if (id == IhAscendedClassSkill && !IhIsPaladin(player))
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
            string[] defaults = IhIsPriest(player)
                ? new string[] { "holy_wave", "lightning_relic", "holy_relic", "divine_intervention", "grand_cross", "heavens_judgement", "lightning_tempest" }
                : IhIsPaladin(player)
                ? (string[])DefaultClericPaladinHotbar.Clone()
                : new string[] { "lightning_zap", "righteous_strike", "holy_wave", "", "", "", "" };
            if (player == null)
                return defaults;

            string saved = ReadPlayerData(player, HotbarLayoutKeyPrefix + GetAdvancement(player));
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
            int classSpend = IhPendingSum(IhClassSkills);
            int advSpend = IhPendingSum(IhBranchSkills(player));
            bool valid = classSpend <= IhClassPointsEarned(player) - IhSpent(player, IhClassSkills)
                && advSpend <= IhAdvPointsEarned(player) - IhSpent(player, IhBranchSkills(player));
            foreach (KeyValuePair<string,int> kvp in _treePrototypePending)
                if (kvp.Value < 1 || !IhIsUnlocked(player, kvp.Key) || IhIsUltimate(kvp.Key)
                    || (IhContains(IhClassSkills,kvp.Key) && !string.IsNullOrEmpty(GetAdvancement(player)))
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
static int assertions;
static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception("FAIL: "+label);}
static Player NewPlayer(string branch,int level){var p=new Player();p.data[ClassDataKey]="Cleric";p.data[AdvancementDataKey]=branch;p.data[IhLevelKey]=level.ToString();Player.m_localPlayer=p;return p;}
static void Main()
{
 var h=new Harness();var p=NewPlayer("",16);
 for(int i=0;i<3;i++)Check(Object.ReferenceEquals(ClericPriestReferenceNodes[i],ClericPaladinReferenceNodes[i]),"shared node instance "+i);
 Check(h.IhClassPointsEarned(p)==14,"14 class points at16");
 Check(h.IhUsesTreeHotbar(p),"Cleric hotbar");
 Check(!h.IhIsUnlocked(p,"lightning_relic"),"Priest locked before advancement");
 Check(!h.IhIsUnlocked(p,"goddess_relic"),"Paladin locked before advancement");
 for(int i=0;i<7;i++){h.AddPrototypePending("righteous_strike",7);h.AddPrototypePending("holy_wave",7);}
 Check(h.GetPrototypeTotalPending()==14,"queue14 class points");
 h.AddPrototypePending("lightning_zap",7);Check(h.GetPrototypeTotalPending()==14,"reject overspend");
 h.ConfirmPrototypePending();Check(h.IhGetTier(p,"holy_wave")==7,"confirm class tiers");
 h._ihPreviewBranch="Priest";Check(h.IhTreeNodes()[0].Id=="lightning_zap","same starter on preview switch");
 h.IhAdvanceToPaladin(p);Check(h.IhIsPriest(p),"tree advances Priest");
 Check(h.IhGetTier(p,"holy_wave")==7,"shared class tiers retained");
 Check(!h.IhCanQueueTier(p,"lightning_zap"),"class tiers frozen after advance");
 Check(h.IhIsUnlocked(p,"lightning_relic")&&h.IhIsUnlocked(p,"holy_relic"),"Priest signatures unlock16");
 Check(h.IhIsUnlocked(p,"grand_sigil"),"Grand Sigil unlock16");
 Check(!h.IhIsUnlocked(p,"goddess_relic"),"cross-branch blocked");
 h._ihUnlockAll.Value=true;Check(!h.IhIsUnlocked(p,"goddess_relic"),"debug unlock cannot cross branch");h._ihUnlockAll.Value=false;
 Check(!h.IhIsUnlocked(p,"divine_intervention"),"DI gated at16");
 p.data[IhLevelKey]="24";Check(h.IhAdvPointsEarned(p)==4,"4 adv points at24");
 Check(!h.IhIsUnlocked(p,"divine_intervention"),"DI spend gate");
 for(int i=0;i<3;i++)h.AddPrototypePending("lightning_relic",5);h.ConfirmPrototypePending();
 Check(h.IhGetTier(p,"lightning_relic")==3,"Priest tiers committed");
 Check(h.IhIsUnlocked(p,"divine_intervention"),"DI unlock after spend3");
 p.data[IhLevelKey]="32";Check(!h.IhIsUnlocked(p,"grand_cross"),"Grand Cross spend gate");
 for(int i=0;i<3;i++)h.AddPrototypePending("holy_relic",5);h.ConfirmPrototypePending();
 Check(h.IhIsUnlocked(p,"grand_cross")&&h.IhIsUnlocked(p,"heavens_judgement"),"Lv32 spend6 gates");
 p.data[IhLevelKey]="36";Check(h.IhIsUnlocked(p,"lightning_tempest"),"ultimate unlock36");Check(h.IhGetTier(p,"lightning_tempest")==0,"ultimate base tier0");
 foreach(var pair in new[]{Tuple.Create(40,1),Tuple.Create(44,2),Tuple.Create(48,3)}){p.data[IhLevelKey]=pair.Item1.ToString();Check(h.IhGetTier(p,"lightning_tempest")==pair.Item2,"automatic ultimate tier");}
 Check(!h.IhCanQueueTier(p,"lightning_tempest"),"ultimate no manual points");
 Check(h.IsPermanentHotbarSkill("lightning_relic")&&h.IsPermanentHotbarSkill("holy_relic")&&h.IsPermanentHotbarSkill("lightning_tempest"),"permanent priest slots");
 Check(!h.IsPermanentHotbarSkill("righteous_strike"),"Priest RS not falsely ascended");
 var bar=h.SanitizeHotbarLayout(new[]{"goddess_relic","lightning_zap","righteous_strike","holy_wave","grand_cross","heavens_judgement","divine_intervention"});
 Check(Array.IndexOf(bar,"goddess_relic")==-1,"sanitize foreign skill");
 foreach(var id in new[]{"lightning_relic","holy_relic","lightning_tempest"})Check(Array.IndexOf(bar,id)>=0,"restore mandatory "+id);
 Check(!h.CanSlotSkill("grand_sigil"),"Grace cannot take numbered slot");
 string[] ids={"lightning_relic","holy_relic","divine_intervention","grand_cross","heavens_judgement","lightning_tempest","grand_sigil"};
 string[] funcs={"CastLightningRelic","CastHolyRelic","CastDivineIntervention","CastGrandCross","CastHeavensJudgement","CastLightningTempest","ActivateGrandSigil"};
 string[] cooldowns={"Priest.LightningRelic","Priest.HolyRelic","Priest.DivineIntervention","Priest.GrandCross","Priest.HeavensJudgement","Priest.LightningTempest","Priest.GrandSigil"};
 for(int i=0;i<ids.Length;i++){Casted="";h.IhCastSkill(p,ids[i]);Check(Casted==funcs[i],"cast dispatch "+ids[i]);h.IhCooldown(p,ids[i]);Check(Casted==cooldowns[i],"cooldown dispatch "+ids[i]);}
 Casted="";h.IhCastSkill(p,"goddess_relic");Check(Casted=="","foreign cast blocked");
 p.data[HotbarLayoutKeyPrefix+"Priest"]="grand_cross,holy_wave,lightning_relic,holy_relic,divine_intervention,heavens_judgement,lightning_tempest";
 Check(h.LoadHotbarLayout(p)[0]=="grand_cross","Priest reordered bar reload");
 p.data[AdvancementDataKey]="Paladin";Check(h.LoadHotbarLayout(p)[0]!="grand_cross","Paladin bar isolated");
 Check(h.IhGetTier(p,"holy_wave")==7,"Class tiers identical in Paladin");
 Check(h.IhIsAscended(p,"righteous_strike"),"Paladin RS ascension preserved");
 Check(h.IhBranchSkills(p)[0]=="goddess_relic","Paladin pool");
 Check(h.IhGetTier(p,"goddess_relic")==0,"Priest tiers not applied to Paladin");
 p.data[AdvancementDataKey]="Priest";Check(h.IhGetTier(p,"lightning_relic")==3,"Priest own tiers persist");
 h.AddPrototypePending("holy_relic",5);p.data[AdvancementDataKey]="Paladin";h.ConfirmPrototypePending();Check(h.IhGetTier(p,"holy_relic")==3,"stale foreign pending rejected");
 p.data[AdvancementDataKey]="Priest";var notes=new List<string>();h.IhApplyLevel(p,15,notes);
 Check(h.GetAdvancement(p)=="","level demotion removes advancement");Check(h.IhGetTier(p,"lightning_relic")==0,"demotion clears Priest advancement points");
 Check(!h.IhIsUnlocked(p,"grand_sigil"),"demotion locks grace");
 string formatted=h.IhLine("Tier","0/7  →  1/7 (pending)   +10% Damage & Healing");
 Check(formatted.Contains("0/7")&&formatted.Contains("1/7"),"Tier fractions plain");
 Check(formatted.Contains(">+10%</color>"),"percentage cyan unit");
 Check(h.IhLine("Range","5m").Contains(">5m</color>"),"cyan measurement");
 Check(h.IhLine("Angle","70°").Contains(">70°</color>"),"cyan degrees");
 // A new character with identical/empty raw saves must not inherit cached tiers.
 p=NewPlayer("Priest",48);Check(h.IhGetTier(p,"holy_relic")==0,"character tier cache isolation");
 Check(h.IhUsesTreeHotbar(p),"Priest tree input active");p.data[ClassDataKey]="Warrior";Check(!h.IhUsesTreeHotbar(p),"noncleric legacy input unchanged");
 Console.WriteLine("PASS: "+assertions+" assertions against 56 extracted production methods. Engine interactions stubbed; not an in-game test.");
}

}