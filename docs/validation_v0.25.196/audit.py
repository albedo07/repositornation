from pathlib import Path
import hashlib,json,re
base=Path('baseline195');work=Path('working196');out=Path('validation196');checks=[]
def check(ok,name):
 if not ok:raise AssertionError(name)
 checks.append(name);print('PASS:',name)
def read(root,name):return (root/name).read_text(encoding='utf-8-sig')
def region(s,marker):
 start=s.index(marker);p=s.index('{',start);depth=1;i=p+1
 # Lex brace structure while skipping string/character literals and comments.
 while depth:
  if s.startswith('//',i):i=s.index('\n',i);continue
  if s.startswith('/*',i):i=s.index('*/',i+2)+2;continue
  if s[i] in ['"',"'"]:
   quote=s[i];verbatim=i>0 and s[i-1]=='@';i+=1
   while True:
    if not verbatim and s[i]=='\\':i+=2;continue
    if s[i]==quote:
     if verbatim and s[i+1]==quote:i+=2;continue
     i+=1;break
    i+=1
   continue
  if s[i]=='{':depth+=1
  elif s[i]=='}':depth-=1
  i+=1
 return s[start:i]
oldArchive=Path('source196/Aethelborn_Ascended_v0.25.195_Skill_Polish_State_Fixes.zip')
check(hashlib.sha256(oldArchive.read_bytes()).hexdigest()=='3b97bd27259d1c1012aac307598433f40d2e40b23a597d67dacc0d5676e0ab07','Original v195 archive is preserved exactly')
changed={p.name for p in work.glob('*.cs') if p.read_bytes()!=(base/p.name).read_bytes()}
check(changed=={'DragonsAltar.CombatRuntime.cs','AlbedosCustomClasses.Advanced.cs'},'Only the two requested visual implementation modules changed')
assets=[p for p in (base/'ImmortalHeroesAssets').rglob('*') if p.is_file()]
check(all(p.read_bytes()==(work/p.relative_to(base)).read_bytes() for p in assets),str(len(assets))+' existing art/animation assets unchanged')
for p in base.glob('*.cs'):
 if p.name not in changed:check(p.read_bytes()==(work/p.name).read_bytes(),'Untouched module remains byte-identical: '+p.name)
for name in ['INSTALL.bat','INSTALL.ps1']:
 expected=(base/name).read_bytes().replace(b'v0.25.195',b'v0.25.196').replace(b'SKILL POLISH AND STATE FIXES',b'HOLLOW CLOUDS AND RELIC DETAIL').replace(b'Skill Polish and State Fixes',b'Hollow Clouds and Relic Detail')
 check((work/name).read_bytes()==expected,name+' changes only the version/banner; byte encoding and line endings preserved')
for name in changed:
 old=read(base,name);new=read(work,name)
 oldbind=[x for x in old.splitlines() if 'Config.Bind(' in x];newbind=[x for x in new.splitlines() if 'Config.Bind(' in x]
 check(oldbind==newbind,name+': all config keys/defaults/descriptions unchanged')
 for term in ['.SetPositions(','.GetPositions(','.LoadImage(','FindObjectOfType(']:check(old.count(term)==new.count(term),name+': no new '+term+' calls')
c=read(work,'DragonsAltar.CombatRuntime.cs');oc=read(base,'DragonsAltar.CombatRuntime.cs');a=read(work,'AlbedosCustomClasses.Advanced.cs');oa=read(base,'AlbedosCustomClasses.Advanced.cs')
for marker in ['public static class DragonCombat','public class DragonSkillClipDriver','public static class DragonMixamo','public static class DragonClothGuard','public class DragonWeaponWake','public class DragonCombatPlugin']:
 old=region(oc,marker);new=region(c,marker).replace('ModVersion = "0.25.196"','ModVersion = "0.25.195"')
 check(old==new,'Unchanged '+marker+' (apart from release version)')
for marker in ['private IEnumerator JudgementCutRoutine(', 'private IEnumerator WhirlwindRoutine(', 'private IEnumerator WhirlwindAscendedRoutine(', 'private IEnumerator CrescentCleaveWave(', 'private IEnumerator SpiritTrail(', 'private IEnumerator PaladinTrail(', 'private IEnumerator MoonlightRoutine(', 'private IEnumerator MoonlightAscendedRoutine(', 'private IEnumerator GrandSigilBarrierRoutine(', 'private string IhComposeTooltip(', 'private void UpdateSwordMasterInterrupt(']:
 check(region(a,marker)==region(oa,marker),'Unchanged gameplay/UI method: '+marker.split('(')[0])
heavy=region(a,'private IEnumerator AscendedHeavySlashRoutine(');oldheavy=region(oa,'private IEnumerator AscendedHeavySlashRoutine(')
check(heavy.replace('0.4f, strokeRoll, false);','0.4f, strokeRoll);')==oldheavy,'Ascended Heavy Swing changes only the visual-plane override: cadence, native CAs, damage and targets retained')
ray=region(a,'private IEnumerator RayOfHopeRoutine(');oldray=region(oa,'private IEnumerator RayOfHopeRoutine(')
rayline='            if (_enableVfx.Value) { Vector3 fog = player.transform.position; DragonCombat.RunVfx(delegate { DragonVfx.DamageBuffMist(fog, radius, new Color(0.85f, 0.70f, 0.46f, 1f)); }); }\n'
check(ray.replace(rayline,'')==oldray,'Ray of Hope changes only by adding one visual callback')
di=region(a,'private IEnumerator DivineInterventionRoutine(');olddi=region(oa,'private IEnumerator DivineInterventionRoutine(')
check(di.replace('                    DragonVfx.DamageBuffMist(diC, diR, DragonVfx.HolyWhite);\n','')==olddi,'Divine Intervention changes only by adding mist to its existing VFX callback')
lr=region(a,'private IEnumerator LightningRelicRoutine(');oldlr=region(oa,'private IEnumerator LightningRelicRoutine(')
extra='            if (ascended && _enableVfx.Value) { Vector3 fog = target; DragonCombat.RunVfx(delegate { DragonVfx.DamageBuffMist(fog, radius, DragonVfx.Storm); }); }\n'
check(lr.replace(extra,'')==oldlr,'Ascended Lightning Relic adds mist once on landing; pulses/healing/damage unchanged')
hj=region(a,'private IEnumerator HeavensJudgementRoutine(');oldhj=region(oa,'private IEnumerator HeavensJudgementRoutine(')
new='''            if (_enableVfx.Value)
            {
                if (DragonVfx.Enabled) DragonCombat.RunVfx(delegate { DragonVfx.HeavensJudgementSigil(center, radius, windup + duration + 0.10f); });
                else StartCoroutine(AnimateGuideRing(center + Vector3.up * 0.08f, radius, radius, windup + duration + 0.10f, new Color(1f, 0.92f, 0.52f, 0.82f), 0.14f));
            }'''
old='''            if (_enableVfx.Value)
                StartCoroutine(AnimateGuideRing(center + Vector3.up * 0.08f, radius, radius, windup + duration + 0.10f, new Color(1f, 0.92f, 0.52f, 0.82f), 0.14f));'''
check(hj.replace(new,old)==oldhj,'Heavens Judgement changes only the radius visual: duration, beam timing, damage and hit areas retained')
check('strokeRoll, false' in heavy and 'followWeaponPlane && local' in c,'Third strike cannot be overwritten by old sampled motion')
check('mistShape.radiusThickness = 0f' in c and 'r * 0.20f, r * 0.34f' in c,'Wisps stay on the surface with small puff sizes, leaving a hollow center')
cloud=region(c,'public static void JudgementSphere(Vector3 center, float radius, float hold, Color tint)')
check(cloud.index('mist.Stop(')<cloud.index('mistShape.radiusThickness')<cloud.index('mist.Play()'),'All volume particles are cleared and shell emission configured before playback')
check('public class DragonCloudShellFade' in c and 'Surface.SetPropertyBlock' in c,'Curved cloud fade uses cached renderer/properties instead of mutating shared meshes')
check('[DefaultExecutionOrder(31000)]\n    public class DragonWeaponWake' in c,'Existing weapon wake update order is preserved')
check('SetRelicAlbedo(_relicGold, RelicAlbedo(0' in c and 'SetRelicAlbedo(_relicIvory, RelicAlbedo(1' in c and 'SetRelicAlbedo(m, RelicAlbedo(2' in c,'Gold/ivory/gem colors are in RGB albedo pixels instead of relying on a material tint')
check('IhRelicMeshObject(root, panel, gold' in a and 'MeshObject("head", Uvd(FacetMesh(v, t, white, 0f), 1.5f), bronze' in c,'Goddess and hammer bodies are gold; ivory remains on channels/striking faces')
gem=region(a,'private void IhRelicDiamond(')
check('depth * 0.28f' in gem and 'pos + Vector3.forward' in gem and 'pos - Vector3.forward' in gem,'Relic gems are mounted on both visible faces instead of buried inside the body')
check('ThickenHammerHead(unit)' in c and 'RelicDiamondMesh(0.26f, 0.07f)' in c,'Hammer has a larger head and larger blue gems, with cached geometry')
check(region(c,'public static float LightScale')==region(oc,'public static float LightScale'),'Extra point lights stay removed')
check('HeavensSigilAlpha' in c and 'if (!Enabled || !Guides) return' in region(c,'public static void HeavensJudgementSigil('),'Sigil honors the existing guide toggle')
check('case 40: Mat(CloudShellTexture(), false);' in c and 'case 42: Mat(HeavensSigilTexture(), false);' in c,'New shell and sigil textures/materials are prepared before casts')
check('Mesh Mesh' not in region(c,'public class DragonCloudShellFade'),'Per-cast cloud fade does not own or destroy the shared cached shell mesh')
(out/'scope_audit.json').write_text(json.dumps({'passed':len(checks),'checks':checks},indent=2));(out/'scope_audit.txt').write_text('\n'.join('PASS: '+n for n in checks)+'\n'+str(len(checks))+' source scope checks passed.\n')
print('TOTAL:',len(checks),'scope checks passed')
