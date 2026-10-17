from pathlib import Path
import hashlib,json,re
base=Path('baseline196');work=Path('working197');out=Path('validation197');checks=[]
def check(ok,name):
 if not ok:raise AssertionError(name)
 checks.append(name);print('PASS:',name)
def read(root,name):return (root/name).read_text(encoding='utf-8-sig')
def end_region(s,start):
 p=s.index('{',start);depth=1;i=p+1
 while depth:
  if s.startswith('//',i):
   k=s.find('\n',i);i=len(s) if k<0 else k;continue
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
 return i
def region(s,marker):
 start=s.index(marker);return s[start:end_region(s,start)]
def methods(s):
 pattern=r'^[ \t]*(?:(?:public|private|internal|protected|static|virtual|override|sealed|async|new|extern|unsafe)\s+)+[\w<>,?\[\].]+\s+(\w+)\s*\([^;{}]*?\)\s*(?:where[^{}]+)?\{'
 result={};seen={}
 for m in re.finditer(pattern,s,re.M):
  signature=re.sub(r'\s+',' ',s[m.start():s.index('{',m.start())]).strip();i=seen.get(signature,0);seen[signature]=i+1
  result[(signature,i)]=(m.group(1),s[m.start():end_region(s,m.start())])
 return result

archive=Path('source197/Aethelborn_Ascended_v0.25.196_Hollow_Clouds_Relic_Detail.zip')
check(hashlib.sha256(archive.read_bytes()).hexdigest()=='c236681142278dc8ef07a89e3815d8ce785dbd4467eaee6f24410bd6e0fdb9f7','Original v196 archive is preserved exactly')
changed={p.name for p in base.glob('*.cs') if p.read_bytes()!=(work/p.name).read_bytes()}
expected={'AlbedosCustomClasses.Core.cs','AlbedosCustomClasses.GuardianAngel.cs','AlbedosCustomClasses.Advanced.cs','AlbedosCustomClasses.Skills.cs','DragonsAltar.CombatRuntime.cs','DragonsAltar.DevTools.cs'}
check(changed==expected,'Exactly six modules changed for batch one: four implementation modules plus two display-name-only modules')
assets=[p for p in (base/'ImmortalHeroesAssets').rglob('*') if p.is_file()]
check(all(p.read_bytes()==(work/p.relative_to(base)).read_bytes() for p in assets),str(len(assets))+' existing art/animation assets remain byte-identical')
for p in base.glob('*.cs'):
 if p.name not in expected:check(p.read_bytes()==(work/p.name).read_bytes(),'Byte-identical untouched module: '+p.name)
for name in ['INSTALL.bat','INSTALL.ps1']:
 old=(base/name).read_bytes();new=(work/name).read_bytes()
 check(new==old.replace(b'0.25.196',b'0.25.197').replace(b'HOLLOW CLOUDS AND RELIC DETAIL',b'SAVING GRACE AND HOLY RELIC').replace(b'Hollow Clouds and Relic Detail',b'Saving Grace and Holy Relic'),name+': version/banner text only')
allow={
 'AlbedosCustomClasses.Core.cs':{'GetAltarAdvancementEntries','GetAdvancementPreview'},
 'DragonsAltar.DevTools.cs':{'FriendlySection'},
 'AlbedosCustomClasses.GuardianAngel.cs':{'Awake','Update','OnDestroy','HasNearbyPriest','TriggerProtection','GetCustomData'},
 'AlbedosCustomClasses.Advanced.cs':{'IhBuildHolyRelic','GetAdvancedPassiveName','GetAdvancedPassiveDescription'},
 'AlbedosCustomClasses.Skills.cs':{'AnimateConeLightning'},
 'DragonsAltar.CombatRuntime.cs':{'Awake','TimedBuffLook','PrewarmTick','DamageBuffMist'}
}
added={
 'AlbedosCustomClasses.GuardianAngel.cs':{'ShouldShowSavingGrace','PublishPriestPresence','ClearPriestPresence'},
 'DragonsAltar.CombatRuntime.cs':{'OrientRelicHeartFaces','RelicHeartMesh','HolyHeartFaceDepth','DamageBuffMistSize'}
}
inventory={}
for name in sorted(changed):
 old=read(base,name);new=read(work,name);om=methods(old);nm=methods(new)
 actual={om[key][0] for key in om if key in nm and om[key][1]!=nm[key][1]}
 introduced={nm[key][0] for key in nm if key not in om}
 removed={om[key][0] for key in om if key not in nm}
 inventory[name]={'changed_methods':sorted(actual),'added_methods':sorted(introduced),'removed_methods':sorted(removed)}
 check(not removed,name+': no methods removed or signatures changed')
 # Exact names are confirmed against the actual method inventory below.
 if name not in ['AlbedosCustomClasses.Core.cs','DragonsAltar.DevTools.cs']:
  check(actual<=allow[name],name+': only explicitly scoped method bodies changed: '+', '.join(sorted(actual)))
 check(introduced==added.get(name,set()),name+': only the intended helper methods added')
 def binds(s):
  return [re.sub(r',\s*"(?:[^"\\]|\\.)*"\s*\);',');',line.strip()) for line in s.splitlines() if 'Config.Bind(' in line]
 check(binds(old)==binds(new),name+': all config sections, keys and defaults unchanged')
 for term in ['.SetPositions(','.GetPositions(','.LoadImage(','FindObjectOfType(']:
  check(new.count(term)==old.count(term),name+': no new '+term)
 check((work/name).read_bytes().startswith(b'\xef\xbb\xbf')==(base/name).read_bytes().startswith(b'\xef\xbb\xbf'),name+': UTF-8 BOM preserved')

# Two display-only modules must match exactly after reversing the named text/version changes.
for name in ['AlbedosCustomClasses.Core.cs','DragonsAltar.DevTools.cs']:
 old=read(base,name);new=read(work,name)
 oldVersion=re.search(r'public const string ModVersion = "[^"]+";',old).group(0)
 new=re.sub(r'public const string ModVersion = "[^"]+";',oldVersion,new)
 new=new.replace('Saving Grace (Mastery)','Bless Thy Sinners (Mastery)').replace('MASTERY</b>  Saving Grace','MASTERY</b>  Bless Thy Sinners').replace('"Priest Saving Grace"','"Priest Bless Thy Sinners"')
 check(new==old,name+': exact display-name/version changes only; internal skill IDs retained')

c=read(work,'DragonsAltar.CombatRuntime.cs');oc=read(base,'DragonsAltar.CombatRuntime.cs');a=read(work,'AlbedosCustomClasses.Advanced.cs');oa=read(base,'AlbedosCustomClasses.Advanced.cs');g=read(work,'AlbedosCustomClasses.GuardianAngel.cs');og=read(base,'AlbedosCustomClasses.GuardianAngel.cs');s=read(work,'AlbedosCustomClasses.Skills.cs');os=read(base,'AlbedosCustomClasses.Skills.cs')
for marker in ['public class DragonSkillClipDriver','public static class DragonMixamo','public static class DragonClothGuard','public class DragonWeaponWake','public class DragonCloudShellFade']:
 check(region(c,marker)==region(oc,marker),'Unchanged '+marker)
combat=region(c,'public static class DragonCombat').replace('            if (source == "Priest.GrandSigilRecovery") label = "Saving Grace - Recovery";\n','')
check(combat==region(oc,'public static class DragonCombat'),'Entire DragonCombat gameplay class unchanged except recovery display label')
for marker in ['private IEnumerator AscendedHeavySlashRoutine(', 'private IEnumerator JudgementCutRoutine(', 'private IEnumerator WhirlwindRoutine(', 'private IEnumerator CrescentCleaveWave(', 'private IEnumerator RayOfHopeRoutine(', 'private IEnumerator DivineInterventionRoutine(', 'private IEnumerator LightningRelicRoutine(', 'private IEnumerator HolyRelicRoutine(', 'private IEnumerator HeavensJudgementRoutine(', 'private void UpdateSwordMasterInterrupt(']:
 check(region(a,marker)==region(oa,marker),'Unchanged skill/cancel routine: '+marker.split('(')[0])
for marker in ['public static void JudgementSphere(Vector3 center, float radius, float hold, Color tint)', 'public static void CloudShellGeometry(', 'public static Color CloudShellTexel(', 'public static Vector3 HammerHeadPoint(', 'private static void ThickenHammerHead(', 'public static Mesh ExtrudeBevel(', 'public static Material RelicGold(', 'public static Material RelicGem(', 'public static void SlashArc(Vector3 pos, Vector3 dir, Color c, float radius, float width, float arc, float seconds, float roll, bool followWeaponPlane)']:
 if marker not in c and marker.startswith('public static void SlashArc'):continue
 check(region(c,marker)==region(oc,marker),'Deferred batch visuals untouched: '+marker.split('(')[0])
for marker in ['internal void TryProtectSetHealth(', 'internal void TryProtectCheckDeath(', 'private bool CanProtect(', 'private IEnumerator Recovery(', 'private IEnumerator GrandSigilVisual(']:
 check(region(g,marker)==region(og,marker),'Saving Grace gameplay/recovery unchanged: '+marker.split('(')[0])
check(region(s,'private void CastLightningZap(')==region(os,'private void CastLightningZap('),'Lightning Zap damage, targets, CA, cost and cooldown unchanged')
zap=region(s,'private IEnumerator AnimateConeLightning(')
reversed=zap.replace('Mathf.Lerp(angle * 0.5f, -angle * 0.5f, p)','Mathf.Lerp(-angle * 0.5f, angle * 0.5f, p)').replace('Mathf.Lerp(0.45f, -0.45f, p)','Mathf.Lerp(-0.45f, 0.45f, p)').replace('Trace the release right-to-left too','Trace the release left-to-right too')
check(reversed==region(os,'private IEnumerator AnimateConeLightning('),'Lightning Zap reverses only release and bolt direction; five bolts and 0.04s intervals retained')
check('"priest_life_token", "heart", "Saving Grace"' in g,'Priest and ally share the same Saving Grace name, icon and status ID')
check('GetAdvancement(priest)' not in region(g,'private bool HasNearbyPriest(') and 'PriestPresenceKey' in region(g,'private bool HasNearbyPriest('),'Remote Priest detection does not read private customData')
check('IsOwner()' in region(g,'private void PublishPriestPresence(') and 'zdo.GetBool(PriestPresenceKey, false) != priest' in g,'Only the owner publishes Priest state, and only when it changes')
check('Time.time + 0.5f' in region(g,'private void Update(') and 'Physics.OverlapSphere' not in region(g,'private bool HasNearbyPriest('),'Presence/indicator check is twice per second and avoids collider-array/hash-set allocations')
check('return until > now || (alive && (self || nearby));' in g,'Active cooldown visibility is independent of Priest proximity')
check('ClearPriestPresence();' in region(g,'private void OnDestroy('),'Advertised Priest membership is cleared on plugin shutdown')
heart=region(a,'private void IhBuildHolyRelic(')
check('for (int side = 0; side < 2; side++)' in heart and heart.count('RelicHeartMesh(')==2,'Only Holy Relic heart and its frame use corrected closed geometry on both sides')
check('new Color(0.08f, 0.82f, 0.19f, 1f)' in heart,'Healing-only green tint is more saturated; blue and other relic materials unchanged')
mist=region(c,'public static void DamageBuffMist(')
check('main.maxParticles = 52' in mist and '42, 14f, 0.6f, 1.8f' in mist,'Stronger support mist remains one bounded 52-particle system per cast')
check('smoke.a = 0.64f' in mist and 'radius * 0.25f, radius * 0.38f' in mist,'Mist has higher opacity and larger billows')
check(mist.index('mist.Stop(')<mist.index('main.maxParticles')<mist.index('mist.Play('),'Mist parameters are configured before its visible emission')
check('DamageBuffMistSize(); break;' in region(c,'public static void PrewarmTick(Player p)'),'New cloud growth curve is cached and prewarmed')
check(region(c,'public static float LightScale')==region(oc,'public static float LightScale'),'Removed extra skill lights remain disabled')
record={'passed':len(checks),'checks':checks,'method_inventory':inventory,'source_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(work.glob('*.cs'))}}
(out/'scope_audit.json').write_text(json.dumps(record,indent=2)+'\n');(out/'scope_audit.txt').write_text('\n'.join('PASS: '+n for n in checks)+'\n'+str(len(checks))+' source scope checks passed.\n')
print('TOTAL:',len(checks),'scope checks passed')
