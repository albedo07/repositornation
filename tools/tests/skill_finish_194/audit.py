from pathlib import Path
import re,json,hashlib,math
base=Path('baseline193');work=Path('working194');out=Path('validation194');checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label);print('PASS:',label)
def text(root,name):return (root/name).read_text(encoding='utf-8-sig')
def region(s,start,end):a=s.index(start);return s[a:s.index(end,a)]
archive=Path('source194/Aethelborn_Ascended_v0.25.193_Sprint_Reload_World_Input.zip')
check(hashlib.sha256(archive.read_bytes()).hexdigest()=='df396d9b93e860f6954b876c36d5dcc48bab289e84ebdcda82d47ab317bc4aa4','Original v193 archive preserved exactly')
changed={p.name for p in work.glob('*.cs') if p.read_bytes()!=(base/p.name).read_bytes()}
check(changed=={'AlbedosCustomClasses.Advanced.cs','AlbedosCustomClasses.Skills.cs','DragonsAltar.CombatRuntime.cs','DragonsAltar.Ranger.cs'},'Only the four requested implementation modules changed')
assets=[p for p in (base/'ImmortalHeroesAssets').rglob('*') if p.is_file()]
check(all(p.read_bytes()==(work/p.relative_to(base)).read_bytes() for p in assets),str(len(assets))+' existing art/animation assets byte-identical')
for name in ['INSTALL.bat','INSTALL.ps1']:
 a=(base/name).read_bytes();check((work/name).read_bytes()==a.replace(b'v0.25.193',b'v0.25.194').replace(b'SPRINT RELOAD AND WORLD INPUT',b'SKILL FINISH AND DODGE PRIORITY'),name+' changes are title text only')
# Compare the first three Config.Bind arguments, ignoring description-only clarifications.
def bindings(s):
 found=[]
 for line in s.splitlines():
  if 'Config.Bind(' not in line:continue
  x=line.split('Config.Bind(',1)[1];args=[];begin=0;depth=0;quoted=False;escape=False
  for i,c in enumerate(x):
   if quoted:
    if escape:escape=False
    elif c=='\\':escape=True
    elif c=='"':quoted=False
   elif c=='"':quoted=True
   elif c in '([{':depth+=1
   elif c in ')]}':depth-=1
   elif c==',' and depth==0:
    args.append(x[begin:i].strip());begin=i+1
    if len(args)==3:break
  found.append(tuple(args))
 return found
for name in changed:
 a=text(base,name);b=text(work,name)
 check(bindings(a)==bindings(b),name+': every configuration section, key and default retained')
 for forbidden in ['.SetPositions(','.GetPositions(','.LoadImage(', 'FindObjectOfType(']:
  check(b.count(forbidden)==a.count(forbidden),name+': no new '+forbidden+' calls')
c=text(work,'DragonsAltar.CombatRuntime.cs');oldc=text(base,'DragonsAltar.CombatRuntime.cs');a=text(work,'AlbedosCustomClasses.Advanced.cs');olda=text(base,'AlbedosCustomClasses.Advanced.cs');r=text(work,'DragonsAltar.Ranger.cs');oldr=text(base,'DragonsAltar.Ranger.cs');k=text(work,'AlbedosCustomClasses.Skills.cs')
for start,end,label in [('        public static string WeaponRuleViolation(', '        // v0.25.186', 'Existing class equipment restrictions retained'),('        public static void GroundFire(', '        // ------------------------------------------------------------------ v0.25.59 skin layer','v193 ground-fire implementation retained'),('    public class DragonSkillClipDriver', '    public static class DragonCombat','Existing shared skill animation and Rigidbody driver retained')]:
 check(region(c,start,end)==region(oldc,start,end),label)
check(c[c.index('    public static class DragonClothGuard'):]==oldc[oldc.index('    public static class DragonClothGuard'):],'Cape/cloth code unchanged')
check(region(r,'        private static bool SprintReload(', '        // v0.25.188')==region(oldr,'        private static bool SprintReload(', '        // v0.25.188'),'v193 sprint-reload implementation retained')
check(region(c,'        public static bool CanUseWorldInput(', '        public static void SetUiInputBlocked(')==region(oldc,'        public static bool CanUseWorldInput(', '        public static void SetUiInputBlocked('),'v193 menu/input guard retained')
bs=region(a,'        private IEnumerator JudgementCutRoutine(', '        // v0.23.5: Blade Storm')
check('GetSphereTargets(player, point, Mathf.Max(0.5f, areaR))' in bs and 'GetSphereTargets(player, center, sphereR)' not in bs,'Blade Storm damage uses the full fixed 3D target area, independently of cosmetic spheres')
for value in ['float budget = ascended','float perHit = budget / (defSpheres * 3f);','int cuts = Mathf.Clamp(_bladeCutsPerSphere.Value, 1, 20);']:
 check(value in bs,'Blade Storm preserves existing per-target damage arithmetic: '+value)
check('cloud.Tint = fog; cloud.Life = cloudLife;' in c and 'fog.a = 0.8f;' in c and 'body.AddComponent<MeshFilter>().sharedMesh = JudgementCloudMesh();' in c,'Blade Storm has a mesh-backed cloud with explicit 0.8 opacity')
cloud=region(c,'        private static Mesh _judgementCloudMesh;', '        // v0.25.185 (user: Clockwork')
check('Amount' not in cloud and 'PolishBudget' not in cloud and 'Restraint' not in cloud,'Cloud silhouette does not depend on particle density/frame budgets')
check('return 0.58f + 0.42f * Fbm(' in cloud and 'renderQueue = 2990' in cloud and 'Color.white' in cloud,'Cloud texture has nonzero density, white mesh vertex colors and draws before the cuts')
check(abs(.8*.58-.464)<1e-9,'Cloud texture/tint has at least 0.464 pre-fade alpha (not an in-game visibility measurement)')
check('StompWindRing(sc, innerVisual, sr, 0.45f)' in a and 'GroundImpact(sc, DragonVfx.Fire, sr' in a,'Stomp retains ground impact/cracks and adds its own thick wind ring')
check('CanInstantSwordMasterDodge(__instance)' in c and '(dodge || (jump && (block || blockHold)))' in c,'Both dedicated dodge and block+jump requests are handled before skill-lock filtering')
check('CancelCurrentAttack(player);' in c and 'driver.CancelAnimation();' in c and 'DragonMixamo.Stop(player, 0f);' in c and 'SkillLockUntil.Remove(id); EndMobileCast(player);' in c,'Dodge releases native attacks, custom animation graphs and skill movement locks')
for name in ['MoonlightRoutine','MoonlightAscendedRoutine','CrescentCleaveRoutine','CrescentCleaveWave','CrescentFireTrailRoutine','JudgementCutRoutine','FrenziedChargeRoutine','EclipseRoutine','EclipseSlashHail','HalfmoonRoutine','HalfmoonAscendedRoutine','HalfmoonTravelingCuts','AscendedImpactWaveRoutine','FrenzyPoseRoutine']:
 check('StartSwordMasterWork('+name+'(' in a,'Dodge owns pending '+name+' work')
for name in ['HeavySlashRoutine','ImpactWaveRoutine','ImpactPunchRoutine']:
 check('TrackSwordMasterSkill(player, '+name+'(player, windup))' in k,'Sword Master base skill '+name+' participates in cancellation')
check('finally { if (visual != null) Destroy(visual); }' in a and 'if (cast != null) cast.Running--;' in a,'Cancelled traveling cuts clean up their generated visuals and wave counters')
check('int strike = 0; strike < 3; strike++' in a and '"battleaxe_attack" + strike.ToString()' in a,'Ascended Heavy Slash selects all three native Battleaxe attacks')
check('DealDamageScaled(player, targets[i], _heavyAscDamage, 1f,' in a,'Each Ascended Heavy Slash strike retains full previous damage')
iw=region(a,'        private IEnumerator AscendedImpactWaveRoutine(', '        // ------------------------------------------------------------------ tooltips')
check(iw.index('HashSet<int> shockHit') > iw.index('for (float s = 0f; s < reached; s += section)'),'Ascended aftershock hit-deduplication resets per shockwave')
pa=region(r,'        private void CastPiercingArrow(', '        // ------------------------------------------------------------------ Tumble Shot')
check(pa.count('BeginSkill(')==1 and 'int count = ascended ? 3 : 1;' in pa and 'new WaitForSeconds(0.2f)' in pa,'Piercing Arrow pays once and fires three Ascended arrows at 0.2-second intervals')
check('(ascended ? 0.6f : 1f)' in pa and abs(3*.6-1.8)<1e-9,'Ascended arrows deal 60% each, 180% combined; normal remains one 100% arrow')
check('if (!ascended) DragonCombat.PlayClip' in pa and 'Shoot(player, shot == 0' in pa,'Ascended Piercing Arrow uses native bow_fire without the custom rg_power cast')
(out/'scope_audit.json').write_text(json.dumps({'passed':len(checks),'checks':checks},indent=2))
print('Passed',len(checks),'static scope checks. Unity rendering/gameplay not exercised.')
