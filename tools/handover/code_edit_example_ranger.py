import sys
p = sys.argv[1] + '/DragonsAltar.Ranger.cs'
s = open(p, encoding='utf-8-sig').read()
def rep(o, n, cnt=1):
    global s
    assert s.count(o) == cnt, ('anchor', o[:90], s.count(o))
    s = s.replace(o, n)
# --- ArrowFlight carries its own sound: the launch whoosh travels with the arrow, the crash plays where it strikes
rep('''        private bool _nextArrowFlame;   // v0.25.181 the next ArrowFlight carries a flame (Explosive Arrow)''',
'''        private bool _nextArrowFlame;   // v0.25.181 the next ArrowFlight carries a flame (Explosive Arrow)
        // v0.25.294 (user: projectiles carry their sound, not the player): the next ArrowFlight carries this whoosh (follows
        // the arrow, fades out where it ends) and plays this crash where it first strikes a creature or the ground
        private int _nextFlightWhoosh = -1, _nextFlightCrash = -1;
        private float _nextFlightPitch = 1f;''')
rep('''            if (_nextArrowFlame) { _nextArrowFlame = false; if (vfx != null) { GameObject fo = vfx; DragonCombat.RunVfx(delegate { DragonVfx.ProjectileFlame(fo.transform, 0.35f, delegate { return fo != null; }); }); } }   // v0.25.181 burning Explosive Arrow
            HashSet<int> done = new HashSet<int>();''','''            if (_nextArrowFlame) { _nextArrowFlame = false; if (vfx != null) { GameObject fo = vfx; DragonCombat.RunVfx(delegate { DragonVfx.ProjectileFlame(fo.transform, 0.35f, delegate { return fo != null; }); }); } }   // v0.25.181 burning Explosive Arrow
            int crashCue = _nextFlightCrash; _nextFlightCrash = -1;
            GameObject sfx = null;
            if (_nextFlightWhoosh >= 0 && player != null)
            {
                sfx = new GameObject("AA_ArrowSfx"); sfx.transform.position = pos;
                GameObject sa = sfx;
                DragonSkillAudio.BeginSustain(player, (DragonBaseSound)_nextFlightWhoosh, pos, 0.01f, _nextFlightPitch, 1f, delegate { return sa != null; }, false, sfx.transform);
            }
            _nextFlightWhoosh = -1; _nextFlightPitch = 1f;
            HashSet<int> done = new HashSet<int>();''')
rep('''                        DragonSkillAudio.Play(player, DragonBaseSound.ArrowHit, ch.transform.position + Vector3.up);   // v0.25.291
                        onHit(ch);''','''                        DragonSkillAudio.Play(player, DragonBaseSound.ArrowHit, ch.transform.position + Vector3.up);   // v0.25.291
                        if (crashCue >= 0) { DragonSkillAudio.Play(player, (DragonBaseSound)crashCue, ch.transform.position + Vector3.up); crashCue = -1; }   // v0.25.294 first strike
                        onHit(ch);''')
rep('''                        DragonSkillAudio.Play(player, DragonBaseSound.ArrowHit, pos);   // v0.25.291 the arrow thunks into the ground''','''                        DragonSkillAudio.Play(player, DragonBaseSound.ArrowHit, pos);   // v0.25.291 the arrow thunks into the ground
                        if (crashCue >= 0) { DragonSkillAudio.Play(player, (DragonBaseSound)crashCue, pos); crashCue = -1; }   // v0.25.294 a big arrow crashes into the ground''')
rep('''                if (!stop) { pos += dir * step; traveled += step; }''','''                if (!stop) { pos += dir * step; traveled += step; }
                if (sfx != null) sfx.transform.position = pos;''')
rep('''            if (vfx != null) Destroy(vfx);
            if (onEnd != null) onEnd(pos);
        }''','''            if (vfx != null) Destroy(vfx);
            if (sfx != null) Destroy(sfx);   // the whoosh fades out where the arrow ends
            if (onEnd != null) onEnd(pos);
        }''')
# --- Ballista: whoosh rides the bolt; crash (charged) where it strikes - creature or ground
rep('''            DragonSkillAudio.Play(player, DragonBaseSound.BallistaLaunch, origin, 0, 1f - 0.04f * stacks);   // v0.25.291 siege launch, deeper per stack (v0.25.293 no BassDrop)
''','''            _nextFlightWhoosh = (int)DragonBaseSound.BallistaLaunch; _nextFlightPitch = 1f - 0.04f * stacks;   // v0.25.294 the whoosh rides the bolt (deeper per stack)
            if (stacks >= 1) _nextFlightCrash = (int)DragonBaseSound.BallistaImpact;   // charged = big arrow: Boulder Crash where it strikes
''')
rep('''                    if (stacks >= 1) DragonSkillAudio.Play(player, DragonBaseSound.BallistaImpact, enemy.transform.position + Vector3.up);   // v0.25.293 (user): Boulder Crash once the arrow is big (charged); else the vanilla arrow hit only
''', '')
# --- Great Arrow: same
rep('''            DragonSkillAudio.Play(player, DragonBaseSound.GreatLaunch, origin);   // v0.25.291 (v0.25.293 no BassDrop)
''','''            _nextFlightWhoosh = (int)DragonBaseSound.GreatLaunch; _nextFlightCrash = (int)DragonBaseSound.GreatCrash;   // v0.25.294 whoosh rides the arrow, Boulder Crash where it strikes
''')
rep('''                    DragonSkillAudio.Play(player, DragonBaseSound.GreatCrash, enemy.transform.position + Vector3.up);   // v0.25.293 (user) big arrows hit with Boulder Crash
''', '')
# --- Starfall: ArrowVolley sound + many light beams (Heaven's Judgement style)
rep('''            DragonSkillAudio.Play(player, DragonBaseSound.BowDraw, player.transform.position);   // v0.25.293 (user): a normal draw...''',
'''            DragonSkillAudio.Play(player, DragonBaseSound.BowDraw, player.transform.position);   // v0.25.293 (user): a normal draw...
            DragonSkillAudio.Expect(player, DragonBaseSound.StarfallVolley, point, channel);   // v0.25.294 ArrowVolley: its first wave lands as the stars begin''')
rep('''            DragonSkillAudio.Play(player, DragonBaseSound.BowShot, ShotOrigin(player));   // ...and the release as the stars begin''',
'''            DragonSkillAudio.Play(player, DragonBaseSound.BowShot, ShotOrigin(player));   // ...and the release as the stars begin
            StartCoroutine(StarfallRainSound(player, point));   // v0.25.294 then its impacts keep raining (seamless loop) until the barrage ends''')
rep('''                    DragonSkillAudio.Play(player, DragonBaseSound.ArrowHit, GroundAt(sp));   // v0.25.293 vanilla arrow hits
                    // v0.25.293 (user: many arrows falling, like Heaven's Judgement): 3 light arrow bodies per tick (no line
                    // renderers, no particles of their own; each lives 0.35 s), spread over the area
                    if (DragonVfx.Enabled)
                        for (int fa = 0; fa < 3; fa++)
                        {
                            Vector2 fr = UnityEngine.Random.insideUnitCircle * radius;
                            Vector3 fg = GroundAt(point + new Vector3(fr.x, 0f, fr.y)) + Vector3.up * 0.3f;
                            Vector3 ftop = fg + Vector3.up * DragonCombat.M(14f) + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));
                            Color fcol = new Color(0.85f, 1f, 0.70f, 1f);
                            DragonCombat.RunVfx(delegate { DragonVfx.FallingArrow(ftop, fg, fcol, 0.35f); });
                        }''','''                    // v0.25.294 (user: beams like Heaven's Judgement): 5 light beams per tick (20/s) from high in the sky over the
                    // whole area - the same cheap 2-quad Beam Heaven's Judgement uses for most of its beams (no particles) - plus
                    // 2 falling arrow bodies. The ArrowVolley recording carries the impacts (no per-star hit sound).
                    if (DragonVfx.Enabled)
                        for (int fa = 0; fa < 7; fa++)
                        {
                            Vector2 fr = UnityEngine.Random.insideUnitCircle * radius;
                            Vector3 fg = GroundAt(point + new Vector3(fr.x, 0f, fr.y)) + Vector3.up * 0.08f;
                            Color fcol = new Color(0.85f, 1f, 0.70f, 0.95f);
                            if (fa < 5)
                            {
                                Vector3 fsky = DragonCombat.GetIndoorSafeSkyPoint(fg + Vector3.up * 0.1f, 30f); float fh = fsky.y - fg.y;
                                DragonCombat.RunVfx(delegate { DragonVfx.Beam(fg, fcol, 0.45f, fh, 0.45f); });
                            }
                            else
                            {
                                Vector3 ftop = fg + Vector3.up * DragonCombat.M(14f) + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));
                                DragonCombat.RunVfx(delegate { DragonVfx.FallingArrow(ftop, fg + Vector3.up * 0.3f, fcol, 0.35f); });
                            }
                        }''')
rep('''        private void StarVfx(Vector3 at, float radius, float width)''','''        // v0.25.294 Starfall bed: once the volley's own impacts thin out, a seamless loop of them until the barrage ends (fade)
        private IEnumerator StarfallRainSound(Player player, Vector3 point)
        {
            yield return new WaitForSeconds(1.5f);
            if (player == null || !_sfActive) yield break;
            int rain = DragonSkillAudio.BeginSustain(player, DragonBaseSound.StarfallRain, point, Mathf.Max(0.5f, _sfDuration.Value), 1f, 1f, delegate { return _sfActive; });
            DragonSkillAudio.SetSustainFades(rain, 0.4f, 0f);
        }

        private void StarVfx(Vector3 at, float radius, float width)''')
open(p, 'w', encoding='utf-8-sig').write(s)
print('ok')
