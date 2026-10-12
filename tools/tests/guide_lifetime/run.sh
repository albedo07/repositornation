#!/bin/bash
# Mock-Unity harness for DragonGuideLife (extracts the real class from DragonsAltar.CombatRuntime.cs). Needs mono-mcs + mono.
cd "$(dirname "$0")"
python3 - <<'PY'
s=open('../../../DragonsAltar.CombatRuntime.cs',encoding='utf-8-sig').read().replace('\r\n','\n')
a=s.index('    public class DragonGuideLife : MonoBehaviour'); b=s.index('    public class DragonVfxLife : MonoBehaviour')
open('/tmp/GuideLife.cs','w').write('using UnityEngine;\nnamespace DragonsAltarCombat\n{\n'+s[a:b]+'}\n')
PY
mcs -out:/tmp/guide_t.exe Mock.cs /tmp/GuideLife.cs Test.cs 2>&1 | grep -v warning; mono /tmp/guide_t.exe
