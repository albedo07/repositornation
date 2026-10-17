from pathlib import Path
import re,json,hashlib
base=Path('working194');work=Path('working195');out=Path('validation195');checks=[]
def check(ok,label):
 if not ok:raise AssertionError(label)
 checks.append(label);print('PASS:',label)
def read(root,name):return (root/name).read_text(encoding='utf-8-sig')
def method(s,marker):
 a=s.index(marker);b=s.index('{',a);level=1;i=b+1
 # Enough for these C# methods: all string/comment braces in this set are balanced.
 while level:
  if s[i]=='{':level+=1
  elif s[i]=='}':level-=1
  i+=1
 return s[a:i]
archive=Path('output/Aethelborn_Ascended_v0.25.194_Skill_Finish_Dodge_Priority.zip')
check(hashlib.sha256(archive.read_bytes()).hexdigest()=='d366aea591631ac42c5a76b1cece252787303e9457499752dc04a63e4f47b3cf','Original v194 ZIP is preserved exactly')
expected={'AlbedosCustomClasses.Core.cs','AlbedosCustomClasses.Advanced.cs','AlbedosCustomClasses.Skills.cs','DragonsAltar.CombatRuntime.cs','DragonsAltar.Ranger.cs','DragonsAltar.DevTools.cs'}
changed={p.name for p in work.glob('*.cs') if p.read_bytes()!=(base/p.name).read_bytes()}
check(changed==expected,'Exactly the six requested implementation/tooltip/settings modules changed')
assets=[p for p in (base/'ImmortalHeroesAssets').rglob('*') if p.is_file()]
check(all(p.read_bytes()==(work/p.relative_to(base)).read_bytes() for p in assets),str(len(assets))+' original art/animation assets remain byte-identical')
for name in ['INSTALL.bat','INSTALL.ps1']:
 expected=(base/name).read_bytes().replace(b'v0.25.194',b'v0.25.195').replace(b'SKILL FINISH AND DODGE PRIORITY',b'SKILL POLISH AND STATE FIXES').replace(b'Ranger Mobility and Buff Timers',b'Skill Polish and State Fixes')
 check((work/name).read_bytes()==expected,name+': release-banner edits only; encoding and line endings preserved')
# Reuse the prior argument parser without executing the prior audit.
src=Path('tests194/audit.py').read_text();b=src.index('def bindings(s):');e=src.index('for name in changed:',b);exec(src[b:e])
for name in changed:
 old=bindings(read(base,name));new=bindings(read(work,name));extra=list(new)
 for item in old:
  assert item in extra, (name,item)
  extra.remove(item)
 check(all(item in new for item in old),name+': all original config keys, sections and defaults preserved')
 check(not extra or name=='AlbedosCustomClasses.Advanced.cs' and extra==[('h','"StrikeInterval_v025195"','0.7f')],name+': only permitted new setting is the 3SS contact interval')
 for term in ['.SetPositions(','.GetPositions(','.LoadImage(','FindObjectOfType(']:
  check(read(work,name).count(term)==read(base,name).count(term),name+': no new forbidden '+term)
c=read(work,'DragonsAltar.CombatRuntime.cs');oc=read(base,'DragonsAltar.CombatRuntime.cs');a=read(work,'AlbedosCustomClasses.Advanced.cs');oa=read(base,'AlbedosCustomClasses.Advanced.cs');r=read(work,'DragonsAltar.Ranger.cs');orr=read(base,'DragonsAltar.Ranger.cs');s=read(work,'AlbedosCustomClasses.Skills.cs');os=read(base,'AlbedosCustomClasses.Skills.cs')
for marker in ['public static string WeaponRuleViolation(', 'public static bool CanInstantSwordMasterDodge(', 'public static void PrepareSwordMasterDodge(', 'private static IEnumerable<CodeInstruction> SwordMasterDodgeTranspiler(', 'public static float GetAttackSpeedMultiplier(', 'public static void GroundFire(']:
 check(method(c,marker)==method(oc,marker),'Unchanged runtime method: '+marker.split('(')[0])
check(method(c,'public static class DragonClothGuard')==method(oc,'public static class DragonClothGuard'),'Cape and Rigidbody cloth guard remain unchanged')
for marker in ['private IEnumerator JudgementCutRoutine(', 'private IEnumerator WhirlwindRoutine(', 'private IEnumerator WhirlwindAscendedRoutine(', 'private IEnumerator CrescentCleaveWave(', 'private IEnumerator SpiritTrail(', 'private IEnumerator PaladinTrail(', 'private IEnumerator MoonlightRoutine(', 'private IEnumerator MoonlightAscendedRoutine(']:
 check(method(a,marker)==method(oa,marker),'Combat unchanged: '+marker.split('(')[0])
for marker in ['private void CastLightningZap(', 'private IEnumerator HeavySlashRoutine(', 'private IEnumerator ImpactWaveRoutine(', 'private IEnumerator ImpactPunchRoutine(']:
 check(method(s,marker)==method(os,marker),'Base skill damage/hitboxes/timing unchanged: '+marker.split('(')[0])
for marker in ['private static bool SprintReload(', 'private static bool IsCurrentCrossbowReload(', 'private static void ClearActionsUnlessReloading(', 'private static IEnumerable<CodeInstruction> SprintReloadTranspiler(', 'private void CastPiercingArrow(']:
 check(method(r,marker)==method(orr,marker),'Preserved Ranger feature: '+marker.split('(')[0])
block=method(a,'private void UpdateSwordMasterInterrupt(')
check('IsBlocking' not in block and 'InDodge' in block,'Only dodge causes a Sword Master sequence interruption')
heavy=method(a,'private IEnumerator AscendedHeavySlashRoutine(')
check('strike < 3' in heavy and '"battleaxe_attack" + strike' in heavy,'Ascended Heavy Swing retains all three Battleaxe strikes')
check('Mathf.Max(0.7f, _hsAscStrikeInterval.Value)' in heavy and 'lastContact + spacing - Time.time' in heavy and 'contactDelay, 1.25f' in heavy,'Heavy Swing contact interval is at least 0.7s and follow-up animation speed is capped')
check('strike == 0 ? 180f : strike == 1 ? 0f : -90f' in heavy,'Heavy Swing has opposite horizontal strokes followed by a vertical third stroke')
check('DealDamageScaled(player, targets[i], _heavyAscDamage, 1f' in heavy,'Per-strike damage is unchanged')
cloud=method(c,'public static void JudgementSphere(Vector3 center, float radius, float hold, Color tint)')
check('AddComponent<MeshFilter>' not in cloud and 'ParticleSystemShapeType.Sphere' in cloud and 'radiusThickness = 1f' in cloud,'Blade Storm uses soft particle volume, with no mesh shell/barrier')
check('mistMain.maxParticles = 48' in cloud and '18, 36' in cloud,'Cloud visibility floor is bounded to 18-36 puffs and at most 48 live particles')
check('echoHead' not in c and 'GameObject echo = new GameObject("echo")' not in c,'The hammer silhouette echo is removed')
check('Shader RelicShader()' in c and 'GetItemPrefab("SwordBronze")' in c and 'mesh.uv = uv' in method(a,'private void IhTorus('),'Relics use a native lit surface option and the gold ring has real texture coordinates')
check('DestroyImmediate(cosmeticColliders[i])' in method(a,'private GameObject IhRelicTemplate('),'Cached visual templates contain no cosmetic colliders')
check('return 0f;' in method(c,'public static float LightScale') and 'NoEffectLights = !' not in c,'Additional point lights and Alt+F7 toggle are removed')
check('"LightIntensity_v025145"' in read(work,'DragonsAltar.DevTools.cs'),'Obsolete light sliders are hidden from F8')
check('if (p.InAttack() || DragonCombat.IsSkillLocked(p)' in c and 'WarmWarhammer(); break;' in c and 'IhPrewarmRelics(player)' in a,'Missing cold resources are prepared in background, avoiding active cast preparation')
check('new GameObject("bladeWind")' in c and 'orbitalY' in c,'Whirlwind has a wind wake beneath the spinning blade')
check('SwordMasterSkillRunning(__instance)' in c,'Holding block cannot overwrite the ongoing tracked skill pose')
check('IhOut("Ascension Description"' not in a and 'IhOut("Ascension", asc)' in a and 'IhCheckAscend(player, id, false, out ascendReason)' in a,'Ascension label and actual eligibility gate are applied')
check('IhJudgementMarkText()' in a and 'IhOut("Judgement Mark"' in a,'Judgement Mark has a complete explanation in the stat body')
check('string desc = IhPlainDescription(id);' in a and 'effects.Add(new KeyValuePair<string, string>' in a,'Header excludes buff-number tokens and effect stat rows retain those values')
(out/'scope_audit.json').write_text(json.dumps({'passed':len(checks),'checks':checks},indent=2));(out/'scope_audit.txt').write_text('\n'.join('PASS: '+x for x in checks)+'\n'+str(len(checks))+' checks passed. No Unity gameplay/rendering exercised.\n')
print('TOTAL:',len(checks),'scope checks passed')
