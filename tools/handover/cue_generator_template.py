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
NEW = 210
def items(body):
    out=[];d=0;cur='';k=0
    while k<len(body):
        c=body[k]
        if c=='"':
            e=body.index('"',k+1); cur+=body[k:e+1]; k=e+1; continue
        if c=='{': d+=1
        if c=='}': d-=1
        if c==',' and d==0: out.append(cur); cur=''
        else: cur+=c
        k+=1
    out.append(cur); return out
def set_elem(s, decl, idx, value, expect=NEW):
    j, k = find_array(s, decl); its = items(s[j+1:k]); assert len(its) == expect, (decl, len(its))
    lead = its[idx][:len(its[idx]) - len(its[idx].lstrip())]; tail = its[idx][len(its[idx].rstrip()):]
    its[idx] = lead + value + tail
    return s[:j+1] + ','.join(its) + s[k:]
import re
OLD = 209
r = rep(r, ', GreatCrash }', ', GreatCrash, StarfallVolley }')
r = rep(r, 'private const int CueCount = 209;', 'private const int CueCount = 210;')
r = append(r, 'string[][] Prefabs = {', ['new string[0]'], OLD)
r = append(r, 'string[] Items = {', ['null'], OLD)
r = append(r, 'int[] Stages = {', ['-1'], OLD)
r = append(r, 'int[] CueSkills = {', ['69'], OLD)   # Starfall Volley (56 + 13)
r = append(r, 'float[] DefaultMarker = {', ['1140f'], OLD)
for decl in ['DefaultPitch = {', 'DefaultSpeed = {', 'DefaultBass = {', 'float[] DefaultLayers = {', 'float[] DefaultLength = {']:
    r = append(r, decl, ['100f'], OLD)
r = append(r, 'float[] UserLayerGain = {', ['137f'], OLD)
r = append(r, 'DefaultSync = {', ['0f'], OLD)
r = append(r, 'bool[] RebalancedLayers = {', ['true'], OLD)
r = append(r, 'int[] CueLayers = {', [str(OLD)], OLD)
r = append(r, 'float[] PhaseGaps = {', ['0.1f'], OLD - 14)
r = append(r, 'string[][] CustomFiles = {', ['new string[] { "ArrowVolley.wav" }'], OLD)
r = append(r, 'string[] sections = { "Warrior Impact Wave Audio", "Warrior Impact', ['"Bowmaster Starfall Volley Audio"'], OLD)
r = append(r, 'string[] names = { "Wave Recording', ['"Arrow Volley Volume (%)"'], OLD)
ENUM = [e.strip() for e in re.search(r'public enum DragonBaseSound \{([^}]*)\}', r).group(1).split(',')]; assert len(ENUM) == NEW
IDX = {n: i for i, n in enumerate(ENUM)}
i = IDX['StarfallRain']
r = set_elem(r, 'DefaultPitch = {', i, '100f')
r = set_elem(r, 'float[] UserLayerGain = {', i, '116f')
r = set_elem(r, 'string[] names = { "Wave Recording', i, '"Arrow Rain Volume (%)"')
r = rep(r, " || c == DragonBaseSound.StarfallChannel || c == DragonBaseSound.StarfallRain || c == DragonBaseSound.StarfallImpact;", " || c == DragonBaseSound.StarfallChannel || c == DragonBaseSound.StarfallImpact;")
open(R, 'w', encoding='utf-8-sig').write(r)
print('ok', NEW)
