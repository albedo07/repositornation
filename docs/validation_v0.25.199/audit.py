from pathlib import Path
import hashlib,json,re
ns={};exec(Path('tests197/audit.py').read_text().split('archive=Path(')[0],ns)
region=ns['region'];methods=ns['methods'];base=Path('baseline198');work=Path('working199');out=Path('validation199');checks=[]
def read(root,n):return(root/n).read_text(encoding='utf-8-sig')
def check(ok,n):
 if not ok:raise AssertionError(n)
 checks.append(n);print('PASS:',n)
archive=Path('output198/Aethelborn_Ascended_v0.25.198_Relic_Faces_Cast_Completion.zip')
check(hashlib.sha256(archive.read_bytes()).hexdigest()=='bdcc445ff23d4b37e69ac74406517d4c67e663efb83580017c7b6953dae13bbe','Original v198 ZIP remains byte-identical')
changed={p.name for p in base.glob('*.cs') if p.read_bytes()!=(work/p.name).read_bytes()}
check(changed=={'DragonsAltar.CombatRuntime.cs','AlbedosCustomClasses.Advanced.cs','DragonsAltar.Sorcerer.cs','Aethelborn.Experience.cs'},'Exactly four source modules changed for pass three and scoped log fixes')
for p in base.glob('*.cs'):
 if p.name not in changed:check(p.read_bytes()==(work/p.name).read_bytes(),'Untouched source module byte-identical: '+p.name)
assets=[p for p in(base/'ImmortalHeroesAssets').rglob('*') if p.is_file()]
check(all(p.read_bytes()==(work/p.relative_to(base)).read_bytes() for p in assets),str(len(assets))+' original art and animation assets byte-identical')
for n in ['INSTALL.bat','INSTALL.ps1']:
 check((work/n).read_bytes()==(base/n).read_bytes().replace(b'0.25.198',b'0.25.199').replace(b'RELIC FACES AND CAST COMPLETION',b'GUIDANCE AND COMBO RECOVERY').replace(b'Relic Faces and Cast Completion',b'Guidance and Combo Recovery'),n+': only version and banner changed')
allowed={
'DragonsAltar.CombatRuntime.cs':{'ApplyTimedBuff','BeginAttackActivation','CloudShellTexel','ComboStartCore','ComboStartPostfix','GetTimedBuffSum','JudgementSphere','OriginalSkillClip','PatchBlockAttack','PatchDamageMethods','TakeSnap'},
'AlbedosCustomClasses.Advanced.cs':{'AscendedHeavySlashRoutine','BindSwordMasterV0223','CastKnightsGuidance','IhAppendSwordMasterStats','IhDrawArtHud','IhLore','IhSkillDescription','IhSkillPower','PatchDamageMethods','PerfUpdateInner','IhFrameSprite'},
'DragonsAltar.Sorcerer.cs':{'InstallPatches'},'Aethelborn.Experience.cs':{'Update'}}
added={'DragonsAltar.CombatRuntime.cs':{'ApplyTimedSkillDamageBuff','ChainSpeedRemaining','CommitAttackActivation','GetSkillDamageMultiplier','Implementation','RestoreAttackActivation','RollBack'},'AlbedosCustomClasses.Advanced.cs':{'ApplyKnightsGuidance','ReceiveKnightsGuidance','RegisterKnightsGuidanceRpc','IhHeavySlashStrokeRoll'},'Aethelborn.Experience.cs':{'Discover'}}
removed={'DragonsAltar.CombatRuntime.cs':{'RollBack'},'Aethelborn.Experience.cs':{'Discover'}}
inv={}
for name in sorted(changed):
 old=read(base,name);new=read(work,name);om=methods(old);nm=methods(new)
 touched={om[k][0] for k in om if k in nm and om[k][1]!=nm[k][1]};extra={nm[k][0] for k in nm if k not in om};gone={om[k][0] for k in om if k not in nm}
 inv[name]={'changed':sorted(touched),'added_or_revised_signature':sorted(extra),'removed_or_revised_signature':sorted(gone)}
 check(touched==allowed[name],name+': only reviewed scoped method bodies changed')
 check(extra==added.get(name,set()) and gone==removed.get(name,set()),name+': only intended helpers/signatures added or revised')
 if name!='AlbedosCustomClasses.Advanced.cs':check([x for x in old.splitlines() if 'Config.Bind(' in x]==[x for x in new.splitlines() if 'Config.Bind(' in x],name+': all config bindings unchanged')
 for term in ['.SetPositions(','.GetPositions(','.LoadImage(','FindObjectOfType(']:check(old.count(term)==new.count(term),name+': no new forbidden API '+term)
 check((base/name).read_bytes().startswith(b'\xef\xbb\xbf')==(work/name).read_bytes().startswith(b'\xef\xbb\xbf'),name+': original BOM preserved')
c=read(work,'DragonsAltar.CombatRuntime.cs');oc=read(base,'DragonsAltar.CombatRuntime.cs');a=read(work,'AlbedosCustomClasses.Advanced.cs');oa=read(base,'AlbedosCustomClasses.Advanced.cs')
oldcfg=[x for x in oa.splitlines() if 'Config.Bind(' in x and '_kg' not in x];newcfg=[x for x in a.splitlines() if 'Config.Bind(' in x and '_kg' not in x]
check(oldcfg==newcfg,'Only Knight\'s Guidance config entries/defaults changed')
for key,value in [('Duration_v025199','90f'),('Cooldown_v025199','600f'),('SkillDamagePercent_v025199','25f')]:check('"'+key+'", '+value in a,'Existing config cannot retain obsolete Guidance defaults: '+key+' = '+value)
check(all(v not in a for v in ['_kgMove','_kgRegen','_kgStaminaCut']),'Old Guidance movement/regen/stamina-use fields and consumers are removed')
for marker in ['public static void DamageBuffMist(', 'public static void CloudShellGeometry(', 'private static Mesh CloudShellMesh(', 'private static void JudgementCloudShell(', 'public class DragonCloudShellFade', 'public static Mesh ExtrudeBevel(', 'public static Vector3 HammerHeadPoint(', 'public static GameObject HolyWarhammer(', 'public static float HolyHeartFaceDepth(', 'public static Mesh RelicHeartMesh(', 'public static Material RelicGold(', 'public static Material RelicGem(', 'public static Mesh RelicDiamondMesh(', 'public static float LightScale']:
 check(region(c,marker)==region(oc,marker),'Unchanged retained visual/model: '+marker.split('(')[0])
for marker in ['public static void PrepareSwordMasterDodge(', 'public static bool CanInstantSwordMasterDodge(', 'public static void RetireSkillAnimations(', 'public static void LockSkill(', 'public static bool IsSkillLocked(', 'public static bool SkillAnimationBusy(', 'public static bool ClipBusy(', 'public static void PlayClip(Player player, string clip, float windup, bool hold, string tuneName = null)', 'public sealed class SkillExecution','public static void SetSkillAnimSpeed(', 'public static bool CanUseWorldInput(', 'public class DragonWeaponWake','public static class DragonClothGuard']:
 check(region(c,marker)==region(oc,marker),'Unchanged animation/input/cape behavior: '+marker.split('(')[0])
for marker in ['private IEnumerator JudgementHammerRoutine(', 'private bool UpdateHammerSize(', 'private float HammerHitRadius(', 'private void UpdateHammerVisual(', 'private IEnumerator JudgementCutRoutine(', 'private IEnumerator MoonlightRoutine(', 'private IEnumerator CrescentCleaveWave(', 'private IEnumerator HeavensJudgementRoutine(', 'private void UpdateSwordMasterInterrupt(', 'private void IhBuildSolidCross(', 'private void IhBuildHolyRelic(', 'private void IhBuildLightningRelic(', 'private bool IhSkillBusy(', 'private bool IhIsActiveSkillControl(', 'private float IhHealPower(']:
 check(region(a,marker)==region(oa,marker),'Unchanged skill/protection behavior: '+marker.split('(')[0])
# Ascended Heavy Slash differs only inside the contact VFX block and its comments.
old=region(oa,'private IEnumerator AscendedHeavySlashRoutine(');new=region(a,'private IEnumerator AscendedHeavySlashRoutine(')
def without_visual_block(text):
 start=text.index('                if (_enableVfx.Value)\n                {');end=start+len(region(text[start:],'                if (_enableVfx.Value)'))
 return text[:start]+'VISUAL_BLOCK'+text[end:]
check(without_visual_block(old)==without_visual_block(new),'Ascended Heavy Slash gameplay, animation triggers, 3SS cadence, damage and interruption remain identical')
new=region(c,'public static void JudgementSphere(Vector3 center, float radius, float hold, Color tint)');old=region(oc,'public static void JudgementSphere(Vector3 center, float radius, float hold, Color tint)')
check(new[new.index('            const int lines = 9;'):]==old[old.index('            const int lines = 9;'):],'Blade Storm slashes and bright cut cores are unchanged')
check('radiusThickness = 0f' in new and 'mistMain.maxParticles = 32' in new and 'fog.a = 0.50f' in new,'Cloud particles remain on a hollow shell with lower opacity and bounded particle count')
check(region(c,'public static float GetSkillPower(')==region(oc,'public static float GetSkillPower('),'Shared skill provider API and fallback behavior are unchanged')
check(region(c,'public static float GetSkillHealPower(')==region(oc,'public static float GetSkillHealPower('),'Shared healing provider is unchanged')
check('return power * DragonCombat.GetSkillDamageMultiplier(player);' in region(a,'private float IhSkillPower('),'Skill damage factor applies once at the provider used by every class module')
post=region(c,'private static void ComboStartPostfix(')
check('RollBack((Player)character, _snap, false);' in post and 'RollBack(lp, _snap, true);' in post,'Native failure does not refund; existing paired-controller late rejection still refunds')
check(post.index('RollBack((Player)character, _snap, false);')<post.index('RetireSkillAnimations'),'Rejected normal attack restores state before success-side skill retirement')
check('if (!refundResources) return;' in region(c,'private static void RollBack('),'Native failed start exits before resource refund logic')
check('DragonCombat.SetChainSpeed(p, c.ChainSpeed, c.ChainRemaining);' in c,'Rollback restores the captured prior speed lifetime')
check('public static void CommitAttackActivation()' in c and 'public static void RestoreAttackActivation(' in c,'Tentative activation learning is committed only after accepted attacks')
core_old=region(oc,'private static void ComboStartCore(');core_new=region(c,'private static void ComboStartCore(')
start=core_new.index('                    if (measured > 0.15f');stop=core_new.index('\n                }',start)
core_new=core_new[:start]+'                    if (measured > 0.15f && (!_chainNatural.TryGetValue(mk, out old) || nat < old)) _chainNatural[mk] = nat;'+core_new[stop:]
check(core_new==core_old,'Successful NACC chain selection, timing, damage and speed calculations are unchanged; only tentative measurement capture added')
for name,marker in [('DragonsAltar.CombatRuntime.cs','private int PatchDamageMethods('),('DragonsAltar.CombatRuntime.cs','private int PatchBlockAttack('),('AlbedosCustomClasses.Advanced.cs','private int PatchDamageMethods('),('DragonsAltar.Sorcerer.cs','private void InstallPatches(')]:
 check('DragonMethodTargets.Implementation' in region(read(work,name),marker),'Canonical declaring method targets: '+name+' '+marker)
check('DragonCombat.FindTypeCached("DualWield.DualWield")' in region(c,'public static AnimationClip OriginalSkillClip('),'Optional SBDW lookup avoids broad Harmony type enumeration')
hud=region(a,'private void IhDrawArtHud(')
check('float[] cur = _ihArtCurrent, max = _ihArtMaximum;' in hud and 'string[] shortNames = IhArtShortNames;' in hud,'HUD reuses numeric and label arrays without changing current stat sampling')
frame=region(a,'private Texture2D IhFrameSprite(')
check(frame.index('TryGetValue')<frame.index('IhAssetExists') and 'IhAssetExists(fileName) ? LoadUiPng(fileName) : null' in frame and '_ihFrameSprites[key]=texture;' in frame,'Optional frame variants check existence only on cache miss; same null fallback is cached')
exp=read(work,'Aethelborn.Experience.cs');oldexp=read(base,'Aethelborn.Experience.cs')
check('Discover(prefab.name, character, true)' in exp and 'Discover(prefab, victim).Value' in exp,'EXP prefab discovery uses base health; live-kill discovery path remains unchanged')
new=region(exp,'private ConfigEntry<int> Discover(').replace(', bool prefab = false','').replace('(prefab ? creature.GetMaxHealthBase() : creature.GetMaxHealth())','creature.GetMaxHealth()')
check(new==region(oldexp,'private ConfigEntry<int> Discover('),'EXP reward formula, configuration keys, custom values and migrations remain unchanged')
record={'passed':len(checks),'checks':checks,'method_inventory':inv,'source_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(work.glob('*.cs'))}}
(out/'scope_audit.json').write_text(json.dumps(record,indent=2)+'\n');(out/'scope_audit.txt').write_text('\n'.join('PASS: '+n for n in checks)+'\n'+str(len(checks))+' scope checks passed.\n');print('TOTAL:',len(checks))
