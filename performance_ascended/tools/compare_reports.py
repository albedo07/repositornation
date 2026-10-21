"""Offline text-report comparison. No external packages; no causal attribution."""
import argparse,re
from pathlib import Path

def value(text,key):
    m=re.search(r'(?:^|[;\n])\s*'+re.escape(key)+r'=([^;\n]*)',text)
    return m.group(1).strip() if m else None

def compare(before,after):
    lines=['Comparison only; different weather, actions or camera paths can invalidate it.']
    for key in ['Scenario','DetailedDiagnostics','RenderingCounters','Unity','CPU','GPU','Resolution','VSync','TargetFPS','GraphicsAPI','QualityLevel','QualityShadowDistance']:
        a,b=value(before,key),value(after,key)
        if a!=b or a is None:lines.append('CHECK '+key+': '+str(a)+' -> '+str(b))
    # 0.4.0: runs with different building counts are not comparable (the supplied captures ranged 12,900-20,200 pieces)
    pa,pb=value(before,'LoadedPiecesAtStart'),value(after,'LoadedPiecesAtStart')
    try:
        if pa and pb and abs(float(pb)-float(pa))>0.05*max(1.0,float(pa)):lines.append('NOT COMPARABLE: LoadedPiecesAtStart '+pa+' -> '+pb+' (more than 5% apart; record both in the same spot)')
    except ValueError:pass
    def inventory(t):
        mark='Loaded BepInEx plugins (inventory, not measured cost):'
        return sorted(t.split(mark,1)[1].split('\nFrames=',1)[0].strip().splitlines()) if mark in t else None
    if inventory(before)!=inventory(after):lines.append('CHECK: loaded plugin inventories differ.')
    for label in ['WARNING: Building discovery', 'WARNING: No frame samples']:
        if label in before or label in after:lines.append('CHECK: '+label)
    for key in ['AverageFPS','OnePercentLowFPS','P99_ms','Worst_ms','Over100ms','MaxUnloadedInOneTick','MaxUnloadBacklog']:
        a,b=value(before,key),value(after,key)
        if a is None or b is None:lines.append(key+': unavailable');continue
        try:
            x,y=float(a),float(b)
            change='n/a' if x==0 else format((y/x-1)*100,'+.2f')+'%'
            lines.append(key+': '+a+' -> '+b+' ('+change+')')
        except ValueError:lines.append(key+': invalid numeric field')
    lines.append('Higher FPS and lower frame-time milliseconds are better. Repeat OFF/ON/OFF to check variation.')
    return '\n'.join(lines)+'\n'

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('before');parser.add_argument('after');args=parser.parse_args()
    print(compare(Path(args.before).read_text(encoding='utf-8-sig'),Path(args.after).read_text(encoding='utf-8-sig')))
