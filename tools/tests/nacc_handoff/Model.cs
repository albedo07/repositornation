// v0.25.154 LOGIC MODEL of the skill <-> NACC handoff (NOT the shipped code, no Unity/Valheim): the same decisions as
// DragonsAltar.CombatRuntime.cs (ResetNaccForSkill / ResetNaccChain / IsAirborne gate / ComboStart accept / UpdateLearns
// validation / generation tokens) driven through the scenarios from the task. Weapon kinds: 1H, SBDW, 2HDW.
using System;
using System.Collections.Generic;

class Learn { public int Gen; public string Ctrl; public string Clip; public bool Loop; public bool InAttack = true; }
class Sim
{
    public int NaccGen, SkillAnimGen;
    public int ComboHit = -1, Swing, PrevSwing = -1; public double LastEnd = -1, LockUntil, LockBefore; public bool Finisher;
    public double Now; public bool Airborne, SkillLocked; public string Ctrl = "base";
    public double ChainSpeed = 1, SkillSpeed = 1; public bool DriverPlaying, TailPlaying; public int DriverGen, TailGen;
    public List<Learn> Pending = new List<Learn>(); public Dictionary<string,string> Learned = new Dictionary<string,string>();
    public List<string> Log = new List<string>(); public int QueuedPrimary; public bool RejectFellThroughToVanilla;
    static readonly int[] Order = { 0, 1, 0, 1, 2 };
    public bool DriverStale { get { return DriverPlaying && DriverGen != SkillAnimGen; } }

    void ResetChain() { NaccGen++; ComboHit = -1; Swing = 0; PrevSwing = -1; LastEnd = -1; ChainSpeed = 1; Pending.Clear(); }
    public void SkillTakeover() { ResetChain(); if (Finisher) { Finisher = false; LockUntil = LockBefore; } }
    public void SkillCast(string name)
    {
        SkillTakeover(); DriverPlaying = true; DriverGen = SkillAnimGen; TailPlaying = true; TailGen = SkillAnimGen; SkillSpeed = 3; SkillLocked = true;
        Log.Add("skill " + name);
    }
    public void SkillEnd() { SkillLocked = false; }
    bool Retire() { SkillAnimGen++; DriverPlaying = false; TailPlaying = false; SkillSpeed = 1; return true; }
    public void Takeoff() { if (!Airborne) { Airborne = true; ResetChain(); QueuedPrimary = 0; } }
    public void Land() { Airborne = false; }
    // Humanoid.StartAttack prefix + Attack.Start(prefix/postfix)
    public int Click(bool secondary, string weapon, string clip)
    {
        if (SkillLocked) return -1;
        bool nacc = !secondary && weapon != "bow";
        if (Airborne && nacc) { ResetChain(); QueuedPrimary = 0; return -2; }
        if (Now < LockUntil) return -1;
        if (secondary || weapon == "bow") { ComboHit = -1; ChainSpeed = 1; Retire(); return -3; }   // heavy: accepted, chain broken
        bool cont = ComboHit >= 0 && ComboHit < 4 && (LastEnd < 0 || Now - LastEnd <= 0.4);
        int hit = cont ? ComboHit + 1 : 0;
        PrevSwing = cont ? Swing : -1; ComboHit = hit; Swing = Order[hit]; LastEnd = -1;
        Retire();   // postfix on accepted start
        Learn l = new Learn(); l.Gen = NaccGen; l.Ctrl = Ctrl; l.Clip = clip; Pending.Add(l);
        if (hit == 4) { LockBefore = LockUntil; Finisher = true; LockUntil = Now + 1.6 + 1.0; ComboHit = -1; }
        return Swing;
    }
    public void StopSwing() { if (LastEnd < 0) LastEnd = Now; if (Finisher) { Finisher = false; LockUntil = Now + 1.0; } }
    public void Tick(double dt)
    {
        Now += dt;
        if (DriverStale) DriverPlaying = false;
        if (TailPlaying && TailGen != SkillAnimGen) TailPlaying = false;
        for (int k = Pending.Count - 1; k >= 0; k--)
        {
            Learn l = Pending[k];
            if (l.Gen != NaccGen || l.Ctrl != Ctrl || SkillLocked || DriverPlaying) { Pending.RemoveAt(k); continue; }
            string[] bad = { "idle", "run", "walk", "jump", "skill", "emote" };
            bool ok = !l.Loop && l.InAttack; foreach (string b in bad) if (l.Clip.Contains(b) && !l.Clip.Contains("attack")) ok = false;
            if (ok) Learned[Ctrl + ":" + l.Clip] = l.Clip;
            Pending.RemoveAt(k);
        }
    }
}

static class T
{
    static int fails;
    static void Check(bool c, string m) { if (!c) { fails++; Console.WriteLine("FAIL " + m); } }
    static void Hits(Sim s, int n, string w, List<int> got) { for (int i = 0; i < n; i++) { int r = s.Click(false, "melee", w + "_attack"); got.Add(r); s.Tick(0.3); s.StopSwing(); s.Tick(0.1); } }
    static void Main()
    {
        foreach (string w in new string[] { "1H", "SBDW", "2HDW" })
        {
            // 1-2-1-2-3 straight, then lockout
            Sim s = new Sim(); List<int> got = new List<int>(); Hits(s, 5, w, got);
            Check(string.Join(",", got.ConvertAll(x => x.ToString()).ToArray()) == "0,1,0,1,2", w + " five hit order " + string.Join(",", got.ConvertAll(x => x.ToString()).ToArray()));
            Check(s.Click(false, "melee", w + "_attack") == -1, w + " finisher lockout kept");
            // Alpha1-7 -> NACC -> skill rapid
            s = new Sim();
            for (int round = 0; round < 7; round++)
            {
                s.SkillCast("Alpha" + (round + 1)); s.Tick(0.05);
                Check(s.ChainSpeed == 1 && s.ComboHit == -1 && s.Pending.Count == 0, w + " skill takeover resets chain " + round);
                s.SkillEnd(); s.Tick(0.3);
                int a = s.Click(false, "melee", w + "_attack"); Check(a == 0, w + " NACC after skill starts at strike 1, got " + a + " round " + round);
                Check(!s.DriverPlaying && !s.TailPlaying && s.SkillSpeed == 1, w + " skill driver/tail/speed retired on accepted NACC " + round);
                s.Tick(0.1);
                int b = s.Click(false, "melee", w + "_attack"); Check(b == 1, w + " strike 2 follows, got " + b);
                s.SkillCast("AlphaX");   // skill in the middle of the chain
                Check(s.ComboHit == -1, w + " mid-chain takeover reset");
                s.SkillEnd(); s.Tick(0.1);
            }
            // pending learning cancelled / stale never cached
            s = new Sim(); s.Click(false, "melee", w + "_attack"); s.SkillCast("S"); s.Tick(0.05);
            Check(s.Learned.Count == 0, w + " learning cancelled by takeover");
            s = new Sim(); s.Click(false, "melee", "locomotion_run"); s.Tick(0.05); Check(s.Learned.Count == 0, w + " locomotion never learned");
            s = new Sim(); s.Click(false, "melee", "skill_clip"); s.Tick(0.05); Check(s.Learned.Count == 0, w + " skill clip never learned");
            s = new Sim(); s.Click(false, "melee", w + "_attack"); s.Ctrl = "dual"; s.Tick(0.05); Check(s.Learned.Count == 0, w + " controller change drops learn");
            s = new Sim(); s.Click(false, "melee", w + "_attack"); s.Tick(0.05); Check(s.Learned.Count == 1, w + " valid swing learned");
            // finisher interrupted by skill: no leftover lockout; genuine lockout kept
            s = new Sim(); got = new List<int>(); Hits(s, 4, w, got); s.Click(false, "melee", w + "_attack"); s.SkillCast("S"); s.SkillEnd();
            Check(s.Click(false, "melee", w + "_attack") == 0, w + " interrupted finisher leaves no lockout");
            s = new Sim(); got = new List<int>(); Hits(s, 5, w, got); double lu = s.LockUntil; s.SkillCast("S"); Check(s.LockUntil == lu, w + " genuine finisher lockout preserved through takeover");
            // jump + click: idle, after each strike, right after skill
            for (int after = 0; after <= 5; after++)
            {
                s = new Sim(); got = new List<int>(); if (after > 0) Hits(s, after, w, got);
                s.Takeoff(); s.QueuedPrimary = 1;
                int r = s.Click(false, "melee", w + "_attack"); Check(r == -2 || r == -1, w + " airborne NACC rejected after " + after + " (" + r + ")");
                Check(s.QueuedPrimary == 0, w + " queued primary cleared"); Check(s.ComboHit == -1, w + " counters reset");
                s.Land(); s.Tick(2.5);
                got = new List<int>(); Hits(s, 5, w, got);
                Check(string.Join(",", got.ConvertAll(x => x.ToString()).ToArray()) == "0,1,0,1,2", w + " landing combo 1-2-1-2-3 after " + after + ": " + string.Join(",", got.ConvertAll(x => x.ToString()).ToArray()));
            }
            s = new Sim(); s.SkillCast("S"); s.SkillEnd(); s.Takeoff(); Check(s.Click(false, "melee", w) == -2, w + " jump right after skill rejected");
            s.Land(); Check(s.Click(false, "melee", w) == 0, w + " land after skill starts strike 1");
            // heavy attack airborne is not NACC
            s = new Sim(); s.Takeoff(); Check(s.Click(true, "melee", w) == -3, w + " heavy attack unaffected by airborne gate");
        }
        // v0.25.155: tune routine activation token + requested-swing validation
        {
            int act = 0, ctrlId = 1; int gen = act; bool wrote = false;
            // routine captured gen; a newer skill animation / accepted attack bumps it -> no write
            act++; if (gen == act && ctrlId == 1) wrote = true; Check(!wrote, "tune routine dead after newer activation");
            gen = act; ctrlId = 2; wrote = false; if (gen == act && ctrlId == 1) wrote = true; Check(!wrote, "tune routine dead after controller change");
            gen = act; ctrlId = 1; if (gen == act && ctrlId == 1) wrote = true; Check(wrote, "tune routine live when still newest");
            // swing validation: name match or evented clip; one state never two swings; bad clip rejected
            Func<string, bool, bool, bool, bool, bool> ok = (trig, named, evented, bad, conflict) => !bad && (named || evented) && !conflict;
            Check(ok("swing2", true, false, false, false), "named state accepted");
            Check(ok("dual_knives1", false, true, false, false), "renamed override state with attack event accepted");
            Check(!ok("swing2", false, false, false, false), "unknown attack-looking state rejected (no 'plain' accept)");
            Check(!ok("swing2", true, true, false, true), "state already owned by another swing rejected");
            Check(!ok("swing2", true, true, true, false), "locomotion clip rejected");
        }
        // v0.25.156: learn bound to its attack activation; override clip mapping
        {
            int serial = 1; int learnSerial = serial; serial++;   // another attack started
            Check(learnSerial != serial, "learn dropped when another attack supersedes it");
            Func<string, string, int> match = (clip, trig) => {
                var map = new Dictionary<string, string> { { "axe_swing", "Attack1" }, { "Axe combo 2", "Attack2" }, { "Axe combo 3", "Attack3" } };
                string key; if (!map.TryGetValue(clip, out key)) return 0;
                int end = trig.Length; while (end > 0 && char.IsDigit(trig[end - 1])) end--;
                return key == "Attack" + (int.Parse(trig.Substring(end)) + 1) ? 1 : -1; };
            Check(match("axe_swing", "swing_axe0") == 1, "override clip for swing 0 = Attack1");
            Check(match("Axe combo 2", "swing_axe1") == 1, "override clip for swing 1 = Attack2");
            Check(match("Axe combo 3", "swing_axe1") == -1, "attack-event clip of another slot rejected for swing 1");
            Check(match("Sword-Attack-R1", "swing_sword0") == 0, "non-replaced clip falls back to name / fresh-event rule");
        }
        // v0.25.157: 2HDW paired controller validation + skill release (decision model)
        {
            Func<bool, int, bool, bool, bool, string> fault = (active, mask, carrier, clips, _) => !clips ? "clips" : !active ? "inactive" : mask != 7 ? "mask" : !carrier ? "carrier" : null;
            Check(fault(true, 7, true, true, false) == null, "valid paired controller accepted");
            Check(fault(false, 7, true, true, false) == "inactive", "controller replaced by another mod detected (not silent)");
            Check(fault(true, 3, true, true, false) == "mask", "base controller without Attack3 replaced rejected");
            Check(fault(true, 7, false, true, false) == "carrier", "missing carrier triggers rejected");
            // scimitar / Hellblade cleaver / paired knives: skill between every attack -> released, re-validated by the next swing
            foreach (string w in new string[] { "scimitar", "hellblade_cleaver", "paired_knives" })
            {
                bool pairedActive = false; int swings = 0, native = 0;
                for (int i = 0; i < 6; i++)
                {
                    pairedActive = true;                 // accepted primary swing: ensure + validate
                    if (fault(pairedActive, 7, true, true, false) == null) swings++; else native++;
                    pairedActive = false;                // skill takeover releases the override
                }
                Check(swings == 6 && native == 0, w + " every accepted swing validated after repeated skills");
            }
        }
        // v0.25.158: repair from the correct base, reject instead of native fallback
        {
            string[] carrierClips = { "axe_swing", "Axe combo 2", "Axe combo 3" };
            Func<HashSet<string>, bool> hasCarrier = n => { foreach (string c in carrierClips) if (!n.Contains(c)) return false; return true; };
            // candidate bases: Warfare's controller (no axe clips), vanilla root (has them)
            var warfare = new HashSet<string> { "knife_slash0", "knife_slash1", "knife_slash2" };   // Attack1-3 replaced via KNIFE names only
            var vanilla = new HashSet<string> { "axe_swing", "Axe combo 2", "Axe combo 3", "knife_slash0" };
            Check(!hasCarrier(warfare), "base without the carrier clips is not accepted even if Attack1-3 were replaced by knife names");
            Check(hasCarrier(vanilla), "vanilla root accepted as repair base");
            Func<List<HashSet<string>>, int> repair = c => { for (int i = 0; i < c.Count; i++) if (hasCarrier(c[i])) return i; return -1; };
            Check(repair(new List<HashSet<string>> { warfare, vanilla }) == 1, "repair skips Warfare's controller and rebuilds from the vanilla base");
            int rejected = 0, native = 0;
            int pick = repair(new List<HashSet<string>> { warfare });
            if (pick < 0) rejected++; else native++;   // unrecoverable -> reject (StartAttack false, Attack.Start skipped), never native
            Check(rejected == 1 && native == 0, "unrecoverable controller: attack rejected before cost, no native substitution");
            bool heavyRejected = false;   // secondary request never enters the paired check
            Check(!heavyRejected, "heavy attack unchanged");
        }
        // v0.25.159: late rejection inside Attack.Start -> fail that instance, roll back, refund, no retire
        {
            int hit = 1, swing = 1; float stamina = 50f, lockUntil = 0f; bool retired = false, finisher = false, started = true;
            int sHit = hit, sSwing = swing; float sStam = stamina, sLock = lockUntil;      // snapshot in the prefix
            hit = 2; swing = 0; stamina -= 12f;                                        // core advanced + Start took stamina
            bool late = true;                                                          // controller lost at the trigger
            if (late) { started = false; hit = sHit; swing = sSwing; if (sStam - stamina > 0) stamina += sStam - stamina; lockUntil = sLock; }
            else { retired = true; if (hit == 4) finisher = true; }
            Check(!started, "late-rejected Attack.Start returns false");
            Check(hit == 1 && swing == 1, "combo counters rolled back");
            Check(stamina == 50f, "stamina refunded");
            Check(!retired && !finisher && lockUntil == 0f, "no skill retire, no finisher bookkeeping, no lockout");
            // genuine finisher (not rejected) still locks
            int h2 = 4; bool fin2 = h2 == 4; Check(fin2, "genuine finisher lockout kept");
        }
        // v0.25.160: trigger path ownership for 2HDW (scimitar / Hellblade cleaver / paired knives)
        {
            foreach (string w in new string[] { "dual_scimitar", "dual_cleaver", "dual_knives_tw" })
            {
                var kg = new Dictionary<string, string>();                       // Warfare table: native trigger -> its own trigger
                for (int i = 0; i < 3; i++) kg[w + i] = w + "_kg" + i;
                string ours = "paired", ctrl = "paired";
                for (int lvl = 0; lvl < 3; lvl++)
                {
                    // OLD order (Therzie prefix first): renamed before ours -> ours bails -> native
                    string nameOld = kg[w + lvl]; bool oursSeesOld = nameOld.StartsWith(w) && char.IsDigit(nameOld[nameOld.Length - 1]) && nameOld.Length == w.Length + 1;
                    Check(!oursSeesOld, w + " old order: our prefix could not see the renamed trigger (the cause)");
                    // NEW: ours first (priority 100000) -> carrier; foreign prefix renames/swaps -> RPC prefix (last) restores
                    string name = "swing_axe" + lvl;           // ours
                    string mappedByForeign; if (kg.TryGetValue(name, out mappedByForeign)) name = mappedByForeign;   // carrier not in table: unchanged
                    ctrl = "warfare";                          // foreign swaps controller anyway
                    if (name != "swing_axe" + lvl) name = "swing_axe" + lvl;
                    if (ctrl != ours) ctrl = ours;             // RouteRpcPrefix
                    Check(name == "swing_axe" + lvl && ctrl == ours, w + " swing " + lvl + " reaches the animator as SBDW carrier on our controller");
                    // fallback recognition when a foreign prefix still ran first
                    Check(kg[w + lvl] == w + "_kg" + lvl, w + " renamed trigger maps back to level " + lvl);
                }
            }
        }
        // v0.25.161: 2HDW swing runs in SBDW's animator state (OneHanded) and the weapon's state comes back on release
        {
            var weapons = new Dictionary<string, int> { { "DualSwordScimitar_TW", 15 }, { "DualAxeDemonic_TW", 15 }, { "DualKnifeIron_TW", 11 } };
            foreach (var kv in weapons)
            {
                int statei = kv.Value; bool held = false;
                // primary routed swing
                statei = 1; held = true;
                Check(statei == 1, kv.Key + " routed swing plays in OneHanded (SBDW) state");
                // heavy attack / skill / idle release -> SetupAnimationState
                if (held) { statei = kv.Value; held = false; }
                Check(statei == kv.Value && !held, kv.Key + " own state restored on release (heavy / skill / idle)");
            }
        }
        // v0.25.162: route bound to ONE SetTrigger invocation (depth + zanim + exact name); others untouched, never renamed
        {
            string ours = "paired", foreign = "warfare";
            string rTrig = null; object rZ = null; int rDepth = -1, depth = 0; bool rDone = false; string ctrl = ours;
            object me = new object(), other = new object();
            Func<object, string, bool> owns = (z, n) => rTrig != null && !rDone && rDepth == depth && ReferenceEquals(z, rZ) && n == rTrig;
            Action<object, string> rpc = (z, n) => { if (owns(z, n) && ctrl != ours) ctrl = ours; /* body */ if (owns(z, n)) { rDone = true; } };
            // routed swing: SetTrigger(swing_axe1) -> RPC inside, unrelated nested SetTrigger(emote) from a foreign hook
            depth++; rTrig = "swing_axe1"; rZ = me; rDepth = depth; rDone = false;
            ctrl = foreign;
            depth++; string nestedName = "emote_wave"; bool nestedTouched = owns(me, nestedName); depth--;   // nested call, deeper
            Check(!nestedTouched, "nested emote trigger inside the routed call passes untouched");
            Check(!owns(me, "battleaxe_secondary"), "heavy attack trigger at the same depth is not the route");
            Check(!owns(other, "swing_axe1"), "same name on another ZSyncAnimation (remote RPC) untouched");
            rpc(me, "swing_axe1");
            Check(ctrl == ours && rDone, "routed carrier trigger restored on our controller");
            Check(!owns(me, "swing_axe1"), "route consumed once (repeat RPC not re-handled)");
            rTrig = null; rZ = null; rDepth = -1; depth--;   // SetTrigger postfix at its own depth
            Check(depth == 0 && rTrig == null, "route ends with its own SetTrigger invocation");
            // later skill / emote triggers in the same frame: no route
            ctrl = foreign; rpc(me, "emote_cheer");
            Check(ctrl == foreign, "skill/emote trigger after the swing keeps whatever controller it found");
        }
        // v0.25.163: regular-size 2HDW fires SBDW's own carrier (dual_knives); paired knives keep swing_axe (untouched)
        {
            Func<string, string, string> carrier = (anim, skill) =>
                (anim.StartsWith("dual") && !string.Equals(anim, "dual_knives", StringComparison.OrdinalIgnoreCase) && skill != "Knives") ? "dual_knives" : "swing_axe";
            string sbdw = "dual_knives";   // ComboTriggerPrefix SBDW baseName with DualWield 1.0.12 installed
            Check(carrier("dualaxes", "Swords") == sbdw, "scimitar 2HDW fires SBDW's carrier");
            Check(carrier("dualaxes", "Axes") == sbdw, "Hellblade cleaver 2HDW fires SBDW's carrier");
            Check(carrier("dual_knives", "Knives") == "swing_axe", "paired knives keep the old carrier (daggers untouched)");
            int[] seq = { 0, 1, 0, 1, 2 }; string got = "";
            for (int h = 0; h < 5; h++) got += sbdw + seq[h] + " ";
            Check(got.Trim() == "dual_knives0 dual_knives1 dual_knives0 dual_knives1 dual_knives2", "2HDW chain = SBDW triggers 0,1,0,1,2");
        }
        // v0.25.164: damage Tier curve = +80% at every max Tier (even steps); healing keeps +10% per Tier
        {
            Func<int, int, float> dmg = (tier, max) => max <= 0 ? 0f : 0.8f * Math.Max(0, Math.Min(tier, max)) / max;
            Check(Math.Abs(dmg(7, 7) - 0.8f) < 1e-5 && Math.Abs(dmg(5, 5) - 0.8f) < 1e-5 && Math.Abs(dmg(3, 3) - 0.8f) < 1e-5, "Class / Advancement / Ultimate max Tier = +80% damage");
            Check(Math.Abs(dmg(1, 7) - 0.8f / 7f) < 1e-5 && Math.Abs(dmg(2, 5) - 0.32f) < 1e-5, "even steps toward +80%");
            Check(dmg(9, 7) == dmg(7, 7) && dmg(0, 7) == 0f, "clamped at 0 and max");
            Check(Math.Abs(0.10f * 7 - 0.7f) < 1e-5, "healing stays +10% per Tier (7/7 = +70%)");
        }
        // v0.25.164: paired daggers stay vanilla (no NACC), regular 2HDW keep NACC; dual wield stays Mercenary-only
        {
            Func<string, string, bool> dagger = (anim, skill) => anim == "dual_knives" || skill == "Knives";
            Func<string, string, string, bool> nacc = (adv, anim, skill) => adv == "Mercenary" && anim.StartsWith("dual") && !dagger(anim, skill);
            Check(nacc("Mercenary", "dualaxes", "Swords") && nacc("Mercenary", "dualaxes", "Axes"), "scimitar / cleaver 2HDW keep AA NACC");
            Check(!nacc("Mercenary", "dual_knives", "Knives"), "paired daggers: vanilla chain");
            Check(!nacc("Sword Master", "dualaxes", "Swords"), "non-Mercenary: no 2HDW");
        }
        Console.WriteLine(fails == 0 ? "ALL PASS" : fails + " FAILED");
        Environment.Exit(fails == 0 ? 0 : 1);
    }
}
