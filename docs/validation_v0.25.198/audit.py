from pathlib import Path
import hashlib,json,re
# Reuse the already-read C# lexer, without executing earlier release assertions.
helpers={};exec(Path('tests197/audit.py').read_text().split('archive=Path(')[0],helpers)
region=helpers['region'];methods=helpers['methods']
base=Path('baseline197');work=Path('working198');out=Path('validation198');checks=[]
def check(ok,name):
 if not ok:raise AssertionError(name)
 checks.append(name);print('PASS:',name)
def read(root,name):return (root/name).read_text(encoding='utf-8-sig')
archive=Path('output197/Aethelborn_Ascended_v0.25.197_Saving_Grace_Holy_Relic.zip')
check(hashlib.sha256(archive.read_bytes()).hexdigest()=='11930e9f3ffc0e8992feffa7cca9dcb5dbcc9ea7bbdf5d62a333b9dac7afac30','Original v197 archive is preserved exactly')
expected={'DragonsAltar.CombatRuntime.cs','AlbedosCustomClasses.Advanced.cs','AlbedosCustomClasses.Skills.cs','DragonsAltar.Sorcerer.cs','DragonsAltar.Ranger.cs'}
changed={p.name for p in base.glob('*.cs') if p.read_bytes()!=(work/p.name).read_bytes()}
check(changed==expected,'Exactly five source modules changed for items 5-8')
for p in base.glob('*.cs'):
 if p.name not in changed:check(p.read_bytes()==(work/p.name).read_bytes(),'Byte-identical untouched module: '+p.name)
assets=[p for p in (base/'ImmortalHeroesAssets').rglob('*') if p.is_file()]
check(all(p.read_bytes()==(work/p.relative_to(base)).read_bytes() for p in assets),str(len(assets))+' existing art/animation assets byte-identical')
for name in ['INSTALL.bat','INSTALL.ps1']:
 expectedBanner=(base/name).read_bytes().replace(b'0.25.197',b'0.25.198').replace(b'SAVING GRACE AND HOLY RELIC',b'RELIC FACES AND CAST COMPLETION').replace(b'Saving Grace and Holy Relic',b'Relic Faces and Cast Completion')
 check((work/name).read_bytes()==expectedBanner,name+': version/banner text only')
allowed={
 'DragonsAltar.CombatRuntime.cs':{'ExtrudeBevel','HammerHeadPoint','HolyWarhammer','Run','Done'},
 'AlbedosCustomClasses.Advanced.cs':{'IhSkillBusy'},
 'AlbedosCustomClasses.Skills.cs':{'AnimateSlashArc','CastFromHotbar','PerfUpdateInner'},
 'DragonsAltar.Sorcerer.cs':{'PerfUpdateInner'},
 'DragonsAltar.Ranger.cs':{'Awake'}
}
added={'DragonsAltar.CombatRuntime.cs':{'SkillAnimationBusy'},'AlbedosCustomClasses.Advanced.cs':{'IhIsActiveSkillControl'}}
inventory={}
for name in sorted(changed):
 old=read(base,name);new=read(work,name);om=methods(old);nm=methods(new)
 touched={om[k][0] for k in om if k in nm and om[k][1]!=nm[k][1]};extra={nm[k][0] for k in nm if k not in om}
 inventory[name]={'changed':sorted(touched),'added':sorted(extra)}
 check(all(k in nm for k in om),name+': no removed methods or altered existing signatures')
 check(touched==allowed[name],name+': only scoped method bodies changed: '+', '.join(sorted(touched)))
 check(extra==added.get(name,set()),name+': only intended helpers added')
 check([x for x in old.splitlines() if 'Config.Bind(' in x]==[x for x in new.splitlines() if 'Config.Bind(' in x],name+': all config keys/defaults/descriptions unchanged')
 for term in ['.SetPositions(','.GetPositions(','.LoadImage(','FindObjectOfType(']:check(old.count(term)==new.count(term),name+': no new '+term)
 check((work/name).read_bytes().startswith(b'\xef\xbb\xbf')==(base/name).read_bytes().startswith(b'\xef\xbb\xbf'),name+': original BOM preserved')

c=read(work,'DragonsAltar.CombatRuntime.cs');oc=read(base,'DragonsAltar.CombatRuntime.cs');a=read(work,'AlbedosCustomClasses.Advanced.cs');oa=read(base,'AlbedosCustomClasses.Advanced.cs');s=read(work,'AlbedosCustomClasses.Skills.cs');os=read(base,'AlbedosCustomClasses.Skills.cs')
for marker in ['public class DragonCombatPlugin','public class DragonSkillClipDriver','public static class DragonMixamo','public class DragonMixamoPlayer','public static class DragonClothGuard','public class DragonWeaponWake','public class DragonCloudShellFade']:
 new=region(c,marker).replace('ModVersion = "0.25.198"','ModVersion = "0.25.197"')
 check(new==region(oc,marker),'Unchanged '+marker+' (except release version)')
for marker in ['public static void PrepareSwordMasterDodge(', 'public static bool CanInstantSwordMasterDodge(', 'public static void RetireSkillAnimations(', 'public static void LockSkill(', 'public static bool IsSkillLocked(', 'public static bool ClipBusy(', 'public static void PlayClip(Player player, string clip, float windup, bool hold, string tuneName = null)', 'public sealed class SkillExecution', 'public static void SetSkillAnimSpeed(']:
 check(region(c,marker)==region(oc,marker),'Unchanged animation/combat behavior: '+marker.split('(')[0])
for marker in ['private IEnumerator JudgementHammerRoutine(', 'private bool UpdateHammerSize(', 'private float HammerHitRadius(', 'private void UpdateHammerVisual(', 'private IEnumerator AscendedHeavySlashRoutine(', 'private IEnumerator JudgementCutRoutine(', 'private void CastKnightsGuidance(', 'private IEnumerator MoonlightRoutine(', 'private IEnumerator CrescentCleaveWave(', 'private IEnumerator HeavensJudgementRoutine(', 'private void UpdateSwordMasterInterrupt(', 'private void IhBuildSolidCross(', 'private void IhBuildHolyRelic(', 'private void IhBuildLightningRelic(']:
 check(region(a,marker)==region(oa,marker),'Unchanged skill/model logic: '+marker.split('(')[0])
for marker in ['public static void JudgementSphere(Vector3 center, float radius, float hold, Color tint)', 'public static Color CloudShellTexel(', 'public static void DamageBuffMist(', 'public static float HolyHeartFaceDepth(', 'public static Mesh RelicHeartMesh(', 'private static void ThickenHammerHead(', 'private static GameObject BuildWarhammerGeometry(', 'public static Material RelicGold(', 'public static Material RelicGem(', 'public static Mesh RelicDiamondMesh(', 'public static void SlashArc(Vector3 center, Vector3 forward, float radius, float arcDeg, Color c, float width, float seconds, float roll, bool followWeaponPlane)']:
 check(region(c,marker)==region(oc,marker),'Unchanged existing visual design: '+marker.split('(')[0])
for marker in ['private void CastHeavySlash(', 'private IEnumerator HeavySlashRoutine(', 'private IEnumerator AnimateConeLightning(']:
 check(region(s,marker)==region(os,marker),'Unchanged starter gameplay / batch-one Zap: '+marker.split('(')[0])

old=region(oc,'public static Mesh ExtrudeBevel(');new=region(c,'public static Mesh ExtrudeBevel(')
new=new.replace('                    // CCW cap triangles point toward +Z; reverse only the back cap.\n','')
new=new.replace('if (side == 0) { t.Add(b0 + tri[i]); t.Add(b0 + tri[i + 1]); t.Add(b0 + tri[i + 2]); }','if (side == 0) { t.Add(b0 + tri[i]); t.Add(b0 + tri[i + 2]); t.Add(b0 + tri[i + 1]); }').replace('else { t.Add(b0 + tri[i]); t.Add(b0 + tri[i + 2]); t.Add(b0 + tri[i + 1]); }','else { t.Add(b0 + tri[i]); t.Add(b0 + tri[i + 1]); t.Add(b0 + tri[i + 2]); }')
check(new==old,'Shared extrusion changes only cap winding; geometry/UVs/normals/sides/materials unchanged')
check(region(c,'public static Vector3 HammerHeadPoint(').replace('* 2.4f','* 1.6f')==region(oc,'public static Vector3 HammerHeadPoint('),'Hammer changes only head height: accepted width, depth and grip preserved')
new=region(c,'public static GameObject HolyWarhammer(').replace('            // Keep the exact child name used by UpdateHammerVisual; Instantiate appends (Clone).\n            model.name = _warhammerTemplate.name;\n','')
check(new==region(oc,'public static GameObject HolyWarhammer('),'Hammer clone changes only its child name so the existing visual growth updater finds it')
check('id == _ihCastId' not in region(a,'private bool IhSkillBusy('),'Repeat casts cannot bypass animation completion merely by sharing the last skill ID')
check('DragonCombat.SkillAnimationBusy(player)' in region(a,'private bool IhSkillBusy('),'New cast input checks complete animation ownership')
gate=region(c,'public static bool SkillAnimationBusy(')
check('driver.IsPlaying' in gate and 'tail.IsBusy' in gate and 'source.Live' in gate,'Full CA gate includes clip keys, native tail and source graph/fade')
check('DragonCombat.ClipBusy(player)' not in region(a,'private bool IhSkillBusy('),'New cast gate does not use the short 0.25-second post-impact clip window')
control=region(a,'private bool IhIsActiveSkillControl(')
check('_whirlActive' in control and 'FindPriestRelic(true)' in control and 'FindPriestRelic(false)' in control and 'SkillCancelAvailable' in control,'Intentional channel stop/relic dismissal controls remain available')
check('SkillAnimationBusy' not in region(c,'public static void PlayClip(Player player, string clip, float windup, bool hold, string tuneName = null)'),'Internal skill multi-strike calls bypass the input gate')
new=region(s,'private IEnumerator AnimateSlashArc(').replace('0.45f, rightToLeft ? 180f : 0f, !rightToLeft); });   // Heavy Slash keeps its intended horizontal right-to-left sweep.','0.45f, rightToLeft ? 180f : 0f); });   // v0.25.59 skin')
check(new==region(os,'private IEnumerator AnimateSlashArc('),'Normal Heavy Slash only disables sampled-plane replacement for its horizontal sweep')
check('AnimateSlashArc(player.transform.position + Vector3.up * 1.0f, forward, range, angle' in s and '1f), true));' in s,'Normal Heavy Slash still requests the right-to-left horizontal effect')
check('SkillAnimationBusy(player)' in region(s,'public void CastFromHotbar('),'Starter direct entry rejects a pending CA before resources are spent')
for name in ['AlbedosCustomClasses.Skills.cs','DragonsAltar.Sorcerer.cs']:
 check('|| DragonCombat.SkillAnimationBusy(player)' in region(read(work,name),'private void PerfUpdateInner('),name+': legacy input also waits for the CA')
check(region(c,'public static float LightScale')==region(oc,'public static float LightScale'),'Extra effect point lights remain removed')
check('SkillAnimationBusy' not in region(c,'public static bool CanUseWorldInput('),'Menu/world input and ongoing charge-release polling are unchanged')

record={'passed':len(checks),'checks':checks,'method_inventory':inventory,'source_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(work.glob('*.cs'))}}
(out/'scope_audit.json').write_text(json.dumps(record,indent=2)+'\n');(out/'scope_audit.txt').write_text('\n'.join('PASS: '+x for x in checks)+'\n'+str(len(checks))+' scope checks passed.\n')
print('TOTAL:',len(checks),'scope checks passed')
