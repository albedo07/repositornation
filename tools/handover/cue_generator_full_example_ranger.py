import sys
d = sys.argv[1]
R = d + '/DragonsAltar.CombatRuntime.cs'
A = d + '/AlbedosCustomClasses.Advanced.cs'
r = open(R, encoding='utf-8-sig').read()
a = open(A, encoding='utf-8-sig').read()

def rep(s, old, new, cnt=1):
    assert s.count(old) == cnt, ('anchor', old[:90], s.count(old))
    return s.replace(old, new)

def find_array(s, decl):
    assert s.count(decl) == 1, ('decl', decl)
    i = s.index(decl); j = s.index('{', i); depth = 0; k = j
    while True:
        c = s[k]
        if c == '"': k = s.index('"', k + 1)
        elif c == '{': depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0: return j, k
        k += 1

def top_count(body):
    depth = 0; n = 1 if body.strip() else 0; k = 0
    while k < len(body):
        c = body[k]
        if c == '"': k = body.index('"', k + 1)
        elif c == '{': depth += 1
        elif c == '}': depth -= 1
        elif c == ',' and depth == 0: n += 1
        k += 1
    return n

def append(s, decl, values, expect):
    j, k = find_array(s, decl)
    n = top_count(s[j + 1:k]); assert n == expect, (decl, n, expect)
    return s[:j + 1] + s[j + 1:k].rstrip() + ', ' + ', '.join(values) + ' ' + s[k:]
import re, math
SKILLS = [('Ranger','Bow Shots'),
          ('Acrobat','Gale Volley'),('Acrobat','Cyclone Arrow'),('Acrobat','Swallow Dive'),('Acrobat','Skyfall Barrage'),('Acrobat','Somersault Dance'),('Acrobat','Furious Winds'),('Acrobat','Tailwind'),
          ('Bowmaster','Ballista Shot'),('Bowmaster','Arrow Rain'),('Bowmaster','Pinning Shot'),('Bowmaster','Explosive Arrow'),('Bowmaster','Splitting Arrow'),('Bowmaster','Starfall Volley'),('Bowmaster','Hawks Vigil')]
S0 = 56
SK = {n: S0 + i for i, (_, n) in enumerate(SKILLS)}; SK['Tumble Shot'] = 10
SEC = {n: c + ' ' + n + ' Audio' for c, n in SKILLS}; SEC['Tumble Shot'] = 'Ranger Tumble Shot Audio'
MOM = {'BigWhoosh.wav':-17.1,'BigWhoosh2.wav':-13.1,'Tornado.wav':-13.2,'SwordSlash2.wav':-21.4,'BoulderCrash.wav':-11.8,'BassDrop.wav':-8.7,'HeavyPunch.wav':-15.5,'MidBoom.wav':-5.4,'SmallBoom.wav':-18.1,'SolidBoom.wav':-11.1,'Buff.wav':-19.4,'ArrowVolley.wav':-18.5,'ArrowRainLoop.wav':-18.6}
MARK = {'BigWhoosh.wav':950,'BigWhoosh2.wav':470,'Tornado.wav':0,'SwordSlash2.wav':260,'BoulderCrash.wav':330,'BassDrop.wav':20,'HeavyPunch.wav':810,'MidBoom.wav':30,'SmallBoom.wav':700,'SolidBoom.wav':380,'Buff.wav':340,'ArrowVolley.wav':1140,'ArrowRainLoop.wav':0}
def pct(f, target):
    g = 10 ** ((target - MOM[f]) / 20.0)
    return round(g * 100 if g <= 1 else math.sqrt(g) * 100)
# name, source (file or native list), skill, label, target dB, gap, pitch, loop, length %
CUES = [
 ('BowShot',  ['sfx_bow_fire'], 'Bow Shots','Bow Release',None,0.05,100,False,100),
 ('BowDraw',  ['sfx_bow_draw'], 'Bow Shots','Bow Draw',None,0.2,100,False,100),
 ('ArrowHit', ['sfx_arrow_hit'],'Bow Shots','Arrow Hit',None,0.08,100,False,100),
 ('TumbleFlip','BigWhoosh2.wav','Tumble Shot','Flip Whoosh',-20,0.1,130,False,40),
 ('GaleGust','BigWhoosh2.wav','Gale Volley','Gale Gust',-18,0.1,125,False,55),
 ('CycloneWind','Tornado.wav','Cyclone Arrow','Cyclone Wind',-21,0.1,115,True,100),
 ('DiveDash','BigWhoosh.wav','Swallow Dive','Dash',-17,0.1,135,False,100),
 ('DiveCut','SwordSlash2.wav','Swallow Dive','Cut',-19,0.1,120,False,100),
 ('SkyfallRise','BigWhoosh2.wav','Skyfall Barrage','Rise',-19,0.1,105,False,50),
 ('SkyfallSlam','BoulderCrash.wav','Skyfall Barrage','Ascended Slam',-11,0.1,100,False,100),
 ('SkyfallBass','BassDrop.wav','Skyfall Barrage','Ascended Slam Bass',-11,0.1,100,False,100),
 ('SomersaultFlip','BigWhoosh2.wav','Somersault Dance','Flip Whoosh',-20,0.1,115,False,40),
 ('SomersaultSlam','HeavyPunch.wav','Somersault Dance','Foot Slam',-12,0.1,100,False,100),
 ('SomersaultCrash','BoulderCrash.wav','Somersault Dance','Ground Crash',-14,0.1,100,False,100),
 ('FuriousStorm','Tornado.wav','Furious Winds','Wind Storm',-21,0.1,125,True,100),
 ('FuriousBurst','BigWhoosh2.wav','Furious Winds','Ascended Burst',-15,0.1,90,False,100),
 ('FuriousBass','BassDrop.wav','Furious Winds','Ascended Burst Bass',-11,0.1,100,False,100),
 ('TailwindGust','BigWhoosh.wav','Tailwind','Gust',-18,0.1,85,False,100),
 ('TailwindBuff','Buff.wav','Tailwind','Buff',-19,0.1,100,False,100),
 ('BallistaLaunch','BigWhoosh.wav','Ballista Shot','Launch',-15,0.1,70,False,100),
 ('BallistaBass','BassDrop.wav','Ballista Shot','Launch Bass',-10,0.1,100,False,100),
 ('BallistaImpact','HeavyPunch.wav','Ballista Shot','Impact',-12,0.3,100,False,100),
 ('BallistaShock','MidBoom.wav','Ballista Shot','Ascended Shockwave',-10,0.1,100,False,100),
 ('RainVolley','ArrowVolley.wav','Arrow Rain','Arrow Volley',-13,0.1,100,False,100),
 ('RainLoop','ArrowRainLoop.wav','Arrow Rain','Arrow Rain',-16,0.1,100,True,100),
 ('PinThud','HeavyPunch.wav','Pinning Shot','Pin',-15,0.06,160,False,50),
 ('PinBoom','SmallBoom.wav','Pinning Shot','Ascended Explosion',-10,0.1,100,False,100),
 ('PinBass','BassDrop.wav','Pinning Shot','Ascended Explosion Bass',-12,0.1,100,False,100),
 ('ExplosiveBoom','MidBoom.wav','Explosive Arrow','Explosion',-8,0.05,100,False,100),
 ('ExplosiveCrack','SmallBoom.wav','Explosive Arrow','Explosion Crack',-12,0.05,100,False,100),
 ('ClusterFall','BigWhoosh.wav','Explosive Arrow','Cluster Fall',-19,0.1,75,False,100),
 ('ClusterBoom','SmallBoom.wav','Explosive Arrow','Cluster Bombs',-12,0.03,90,False,100),
 ('SplitWhoosh','BigWhoosh2.wav','Splitting Arrow','Volley Whoosh',-19,0.08,140,False,40),
 ('GreatLaunch','BigWhoosh.wav','Splitting Arrow','Great Arrow',-15,0.1,75,False,100),
 ('GreatBass','BassDrop.wav','Splitting Arrow','Great Arrow Bass',-11,0.1,100,False,100),
 ('SkyCrash','BoulderCrash.wav','Splitting Arrow','Ascended Sky Arrow Crash',-11,0.1,100,False,100),
 ('SkyPunch','HeavyPunch.wav','Splitting Arrow','Ascended Sky Arrow Impact',-13,0.1,100,False,100),
 ('StarfallChannel','Buff.wav','Starfall Volley','Channel',-19,0.1,85,False,100),
 ('StarfallRain','ArrowRainLoop.wav','Starfall Volley','Star Rain',-18,0.1,108,True,100),
 ('StarfallImpact','HeavyPunch.wav','Starfall Volley','Star Impact',-15,0.15,150,False,50),
 ('StarfallFinal','SolidBoom.wav','Starfall Volley','Ascended Final Star',-9,0.1,100,False,100),
 ('StarfallBass','BassDrop.wav','Starfall Volley','Ascended Final Star Bass',-10,0.1,100,False,100),
 ('VigilWing','BigWhoosh2.wav','Hawks Vigil','Wings',-20,0.1,140,False,40),
 ('VigilBuff','Buff.wav','Hawks Vigil','Buff',-18,0.1,90,False,100),
]
OLD = 164; N = len(CUES); NEW = OLD + N; NS = len(SKILLS)
for c in CUES: assert "'" not in SEC[c[2]] + c[3]
native = lambda c: isinstance(c[1], list)
r = rep(r, ', LaserStart, RuptureWindup }', ', LaserStart, RuptureWindup, ' + ', '.join(c[0] for c in CUES) + ' }')
r = rep(r, 'private const int CueCount = 164;', 'private const int CueCount = %d;' % NEW)
r = rep(r, 'SkillVolumes = new ConfigEntry<float>[56];', 'SkillVolumes = new ConfigEntry<float>[%d];' % (S0 + NS))
r = append(r, 'string[][] Prefabs = {', ['new string[] { %s }' % ', '.join('"%s"' % p for p in c[1]) if native(c) else 'new string[0]' for c in CUES], OLD)
ITEM = {'BowShot': ('"BowHuntsman"', '2'), 'BowDraw': ('"BowHuntsman"', '0'), 'ArrowHit': ('"ArrowWood"', '3')}
r = append(r, 'string[] Items = {', [ITEM[c[0]][0] if c[0] in ITEM else 'null' for c in CUES], OLD)
r = append(r, 'int[] Stages = {', [ITEM[c[0]][1] if c[0] in ITEM else '-1' for c in CUES], OLD)
r = append(r, 'int[] CueSkills = {', [str(SK[c[2]]) for c in CUES], OLD)
r = append(r, 'float[] DefaultMarker = {', ['0f' if native(c) else '%df' % MARK[c[1]] for c in CUES], OLD)
r = append(r, 'DefaultPitch = {', ['%df' % c[6] for c in CUES], OLD)
for decl in ['DefaultSpeed = {', 'DefaultBass = {', 'float[] DefaultLayers = {']:
    r = append(r, decl, ['100f'] * N, OLD)
r = append(r, 'float[] DefaultLength = {', ['%df' % c[8] for c in CUES], OLD)
r = append(r, 'float[] UserLayerGain = {', ['100f' if native(c) else '%df' % pct(c[1], c[4]) for c in CUES], OLD)
r = append(r, 'DefaultSync = {', ['0f'] * N, OLD)
r = append(r, 'bool[] RebalancedLayers = {', ['false' if native(c) else 'true' for c in CUES], OLD)
r = append(r, 'int[] CueLayers = {', [str(OLD + i) for i in range(N)], OLD)
r = append(r, 'float[] PhaseGaps = {', ['%gf' % c[5] for c in CUES], OLD - 14)
r = append(r, 'string[][] CustomFiles = {', ['null' if native(c) else 'new string[] { "%s" }' % c[1] for c in CUES], OLD)
r = append(r, 'string[] sections = { "Warrior Impact Wave Audio", "Warrior Impact', ['"%s"' % SEC[c[2]] for c in CUES], OLD)
r = append(r, 'string[] names = { "Wave Recording', ['"%s Volume (%%)"' % c[3] for c in CUES], OLD)
r = append(r, 'string[] SkillNames = {', ['"%s"' % n for _, n in SKILLS], S0)
r = append(r, 'string[] SkillClasses = {', ['"%s"' % c for c, _ in SKILLS], S0)
r = append(r, 'float[] DefaultVolumes = {', ['100f'] * NS, S0)
r = append(r, 'bool[] Rebalanced = {', ['true'] * NS, S0)
r = append(r, 'float[] PreviousFactoryVolumes = {', ['-1f'] * NS, S0)
r = append(r, 'float[] SweetSpotFrom = {', ['125f'] * NS, S0)
r = append(r, 'float[] UserSkillGain = {', ['100f'] * NS, S0)
r = rep(r, '|| cue == DragonBaseSound.DominionLoop || cue == DragonBaseSound.CataclysmLoop;', '|| cue == DragonBaseSound.DominionLoop || cue == DragonBaseSound.CataclysmLoop ||\n                ' + ' || '.join('cue == DragonBaseSound.%s' % c[0] for c in CUES if c[7]) + ';   // v0.25.291 Ranger loops')
r = rep(r, '|| c == DragonBaseSound.GlacialCreate || c == DragonBaseSound.GlacialDescent;', '|| c == DragonBaseSound.GlacialCreate || c == DragonBaseSound.GlacialDescent ||\n                c == DragonBaseSound.Arrow;   // v0.25.291: every skill shot plays Bow Release (Shoot)')
r = rep(r, '"MagicSword3.wav" };', '"MagicSword3.wav", "ArrowVolley.wav", "ArrowRainLoop.wav" };')
open(R, 'w', encoding='utf-8-sig').write(r)
for c in CUES: print(c[0], 'native' if native(c) else pct(c[1], c[4]))
print('cues', NEW, 'skills', S0 + NS)
