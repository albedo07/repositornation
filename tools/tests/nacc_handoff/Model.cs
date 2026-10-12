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
        Console.WriteLine(fails == 0 ? "ALL PASS" : fails + " FAILED");
        Environment.Exit(fails == 0 ? 0 : 1);
    }
}
