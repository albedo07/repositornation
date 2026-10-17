from pathlib import Path
import re,json,hashlib
ns={};exec(Path('tests200/source_lexer.py').read_text(),ns)
end=ns['end_region'];region=ns['region']
def scoped(s):
 classes=[]
 for m in re.finditer(r'^[ \t]*(?:(?:public|private|internal|protected|static|sealed|abstract)\s+)*(?:class|struct)\s+(\w+)[^\n{]*',s,re.M):
  classes.append((m.start(),end(s,m.start()),m.group(1)))
 p=r'^[ \t]*(?:(?:public|private|internal|protected|static|virtual|override|sealed|async|new|extern|unsafe)\s+)+[\w<>,?\[\].]+\s+(\w+)\s*\([^;{}]*?\)\s*(?:where[^{}]+)?\{'
 result={}
 for m in re.finditer(p,s,re.M):
  owner='.'.join(c[2] for c in classes if c[0]<m.start()<c[1]);sig=re.sub(r'\s+',' ',s[m.start():s.index('{',m.start())]).strip();key=owner+'::'+sig
  assert key not in result,key
  result[key]=(m.group(1),s[m.start():end(s,m.start())])
 return result
base=Path('baseline199');work=Path('working200');out=Path('validation200');checks=[]
def read(root,n):return(root/n).read_text(encoding='utf-8-sig')
def check(ok,n):
 if not ok:raise AssertionError(n)
 checks.append(n);print('PASS:',n)
archive=Path('source200/Aethelborn_Ascended_v0.25.199_Guidance_Combo_Recovery.zip')
check(hashlib.sha256(archive.read_bytes()).hexdigest()=='ed887e04aa94fca893b39fe97762a64cd16d926270df2667edcf2d24276346d4','Original v199 archive preserved exactly')
changed={p.name for p in base.glob('*.cs') if p.read_bytes()!=(work/p.name).read_bytes()}
check(changed=={'DragonsAltar.CombatRuntime.cs','AlbedosCustomClasses.Advanced.cs','AlbedosCustomClasses.Skills.cs'},'Exactly the three required source modules changed')
for p in base.glob('*.cs'):
 if p.name not in changed:check(p.read_bytes()==(work/p.name).read_bytes(),'Untouched module byte-identical: '+p.name)
assets=[p for p in(base/'ImmortalHeroesAssets').rglob('*') if p.is_file()]
check(all(p.read_bytes()==(work/p.relative_to(base)).read_bytes() for p in assets),str(len(assets))+' original art and animation assets byte-identical')
allowed={
'DragonsAltar.CombatRuntime.cs':{'DamageBuffMist','HolyHeartFaceDepth','InstallPatches','IsSmallEnemy','PrewarmTick','RelicHeartMesh','ShowStatus','SpinSlash'},
'AlbedosCustomClasses.Advanced.cs':{'AscendedHolyWaveRoutine','CircleSwingHit','CircleSwingRoutine','CircleEruption','DivineInterventionRoutine','DrawImmortalHud','DrawLegacySkillbookWindow','IhBuildHolyRelic','IhBuildSolidCross','IhBuildTooltipBody','IhRelicDiamond','IhSanctify','LightningRelicRoutine','RayOfHopeRoutine','WhirlwindAscendedRoutine','WhirlwindRoutine'},
'AlbedosCustomClasses.Skills.cs':{'Awake','HolyWaveRoutine'}}
added={
'DragonsAltar.CombatRuntime.cs':{'Attach','Awake','BirdAwakePostfix','BirdDestructiblePrefix','BirdFloatPrefix','BirdStaggerPrefix','BirdVectorPrefix','DamagePrefix','FlightVelocity','GetHoverName','InsetGemOutline','IsDead','OnCollisionStay','OnDestroy','OnDisable','OnEnable','PatchBirdTargets','PercentMaxHeal','ReceiveSkillHit','Start','TargetBounds','Update','WindRing','WindRingAlpha','WindRingTexture'},
'AlbedosCustomClasses.Advanced.cs':{'IhDrawCooldownChevrons'},'AlbedosCustomClasses.Skills.cs':{'HealPlayersPercent'}}
removed={'DragonsAltar.CombatRuntime.cs':{'DamagePrefix'},'AlbedosCustomClasses.Skills.cs':{'HealPlayers'}}
inventory={}
for name in sorted(changed):
 old=read(base,name);new=read(work,name);om=scoped(old);nm=scoped(new)
 touched={om[k][0] for k in om if k in nm and om[k][1]!=nm[k][1]};extra={nm[k][0] for k in nm if k not in om};gone={om[k][0] for k in om if k not in nm}
 inventory[name]={'changed':[k for k in om if k in nm and om[k][1]!=nm[k][1]],'added_or_revised_signature':[k for k in nm if k not in om],'removed_or_revised_signature':[k for k in om if k not in nm]}
 check(touched==allowed[name],name+': existing method changes confined to requested scope')
 check(extra==added[name] and gone==removed.get(name,set()),name+': only intended helpers/adapter methods and private signature changes')
 for term in ['.SetPositions(','.GetPositions(','.LoadImage(','FindObjectOfType(']:check(old.count(term)==new.count(term),name+': no new forbidden API '+term)
 check((base/name).read_bytes().startswith(b'\xef\xbb\xbf')==(work/name).read_bytes().startswith(b'\xef\xbb\xbf'),name+': original BOM preserved')
 oldcfg=[x for x in old.splitlines() if 'Config.Bind(' in x and 'ImmediateHeal' not in x];newcfg=[x for x in new.splitlines() if 'Config.Bind(' in x and 'ImmediateHeal' not in x]
 check(oldcfg==newcfg,name+': every unrelated config binding unchanged')
c=read(work,'DragonsAltar.CombatRuntime.cs');oc=read(base,'DragonsAltar.CombatRuntime.cs');a=read(work,'AlbedosCustomClasses.Advanced.cs');oa=read(base,'AlbedosCustomClasses.Advanced.cs');s=read(work,'AlbedosCustomClasses.Skills.cs');os=read(base,'AlbedosCustomClasses.Skills.cs')
for marker in ['private static void ComboStartCore(', 'private static void ComboStartPostfix(', 'private static ComboSnap TakeSnap(', 'private static void RollBack(', 'public static void BeginAttackActivation(', 'public static void RestoreAttackActivation(', 'public static void CommitAttackActivation(', 'public static void PrepareSwordMasterDodge(', 'public static bool CanInstantSwordMasterDodge(', 'public static void RetireSkillAnimations(', 'public static bool SkillAnimationBusy(', 'public static void LockSkill(', 'public static bool CanUseWorldInput(', 'public static DamagePatchState BeginDamage(', 'public static void EndDamage(', 'public static float GetSkillPower(', 'public static float GetSkillHealPower(', 'public static void ApplyTimedSkillDamageBuff(', 'public static float GetSkillDamageMultiplier(', 'public static class DragonDualWield','public static class DragonClothGuard','public class DragonWeaponWake','public class DragonSkillClipDriver','public static void StompWindRing(', 'public static Mesh RelicDiamondMesh(', 'public static Mesh ExtrudeBevel(', 'public static Vector3 HammerHeadPoint(', 'public static GameObject HolyWarhammer(', 'public static void CloudShellGeometry(', 'public static Color CloudShellTexel(', 'public static float LightScale']:
 check(region(c,marker)==region(oc,marker),'Unchanged retained combat/visual system: '+marker.split('(')[0])
for marker in ['private IEnumerator AscendedHeavySlashRoutine(', 'private static float IhHeavySlashStrokeRoll(', 'private void CastKnightsGuidance(', 'private void ApplyKnightsGuidance(', 'private void ReceiveKnightsGuidance(', 'private float IhSkillPower(', 'private float IhHealPower(', 'private IEnumerator JudgementCutRoutine(', 'private IEnumerator JudgementHammerRoutine(', 'private void UpdateHammerVisual(', 'private IEnumerator CrescentCleaveWave(', 'private void UpdateSwordMasterInterrupt(', 'private void UpdateTemporaryBuffIndicators(', 'private bool IhSkillBusy(', 'private bool IhIsActiveSkillControl(', 'private void IhBuildLightningRelic(']:
 check(region(a,marker)==region(oa,marker),'Unchanged retained skill/state behavior: '+marker.split('(')[0])
for marker in ['private void CastHeavySlash(', 'private IEnumerator HeavySlashRoutine(', 'private IEnumerator AnimateSlashArc(', 'private IEnumerator AnimateConeLightning(']:check(region(s,marker)==region(os,marker),'Unchanged starter attack: '+marker.split('(')[0])
check('"ImmediateHealPercent_v025200", 20f' in s and '"ImmediateHeal", 25f' not in a and '25 HP' not in a,'All immediate-heal config/tooltip sources use percent max HP instead of the old flat 25')
check('PercentMaxHeal(ally.GetMaxHealth(), percent, 1f)' in region(s,'private void HealPlayersPercent('),'Normal Holy Wave computes immediate healing separately for each recipient')
asc=region(a,'private IEnumerator AscendedHolyWaveRoutine(')
check('PercentMaxHeal(ally.GetMaxHealth(), instant, 1f)' in asc and 'PercentMaxHeal(allies[i].GetMaxHealth(), echo, 1f)' in asc,'Ascended main heal and echo both use the recipient\'s maximum HP')
check('low ? immediate * 2f : immediate' in asc and '_ahwEchoPercent.Value' in asc and '_ahwLowHp.Value' in asc,'Ascended low-HP multiplier and echo percentage remain intact')
check('HolyHeartFaceDepth(t, true)' in a and '|heart200' in a and 'hframe197' not in a and 'heart197' not in a,'Holy heart is one through-backing insert rather than two raised plates')
check('InsetGemOutline(size)' in region(a,'private void IhRelicDiamond(') and 'depth * 0.49f' in region(a,'private void IhRelicDiamond('),'Cross sapphires are closed beveled inserts whose back halves overlap their backing')
check('"gem200|"' in a,'New inset meshes have their own cache key, preserving unrelated cached gem designs')
check('new Color(0.08f, 0.82f, 0.19f, 1f)' in region(a,'private void IhBuildHolyRelic('),'Healing heart retains its stronger green pigment')
check('ExtrudeBevel(outline, depth, 0f, 1f)' in region(c,'public static Mesh RelicHeartMesh('),'Heart-specific cap geometry avoids the inward cusp miter that drops triangles')
for marker in ['private void CircleSwingHit(', 'private IEnumerator CircleSwingRoutine(']:
 r=region(a,marker);check('AnimateHalfmoonArc' not in r and 'SlashStreaks' not in r and 'DragonVfx.WindRing' in r,marker+': wind shockwaves replace slash VFX')
check('DragonVfx.WindRing' in region(a,'private void CircleEruption('),'Ascended Circle Swing eruption also uses the wind ring while retaining its ground impact')
for marker in ['private IEnumerator WhirlwindRoutine(', 'private IEnumerator WhirlwindAscendedRoutine(']:
 check('AnimateTickRing(player.transform.position + Vector3.up * 0.9f' not in region(a,marker),marker+': removed torso-height pulse ring')
check('DragonGroundWind' in region(c,'public static GameObject SpinSlash(') and 'bladeWind' not in region(c,'public static GameObject SpinSlash('),'Whirlwind under-blade air is now a terrain downwash emitter; blade crescents remain')
check('Mat(WindRingTexture(false), false)' in region(c,'public static void PrewarmTick(Player p)') and 'Mat(WindRingTexture(true), false)' in region(c,'public static void PrewarmTick(Player p)'),'Both cached wind textures/materials are included in existing prewarm')
wind=region(c,'public static void DamageBuffMist(')
check('shape.radiusThickness = 0.06f' in wind and 'Color air = tint' in wind and 'Color.Lerp' not in wind and 'main.maxParticles = 28' in wind,'Damage-buff wind is class-colored, annular and particle-bounded rather than a filled gray cloud')
for name,rgb in [('RayOfHopeRoutine','1f, 0.93f, 0.52f'),('DivineInterventionRoutine','1f, 0.92f, 0.48f'),('LightningRelicRoutine','0.45f, 0.70f, 1f'),('IhSanctify','1f, 0.95f, 0.65f')]:
 marker=('private void ' if name=='IhSanctify' else 'private IEnumerator ')+name+'('
 r=region(a,marker);check('DamageBuffMist' in r and rgb in r,name+': airy damaging support ring shares existing pulse RGB')
check('se.CooldownMarker = key == "fury_recovery";' in c and 'if (se.CooldownMarker) IhDrawCooldownChevrons(ir, s)' in a,'Down chevrons are specific to Fury cooldown, not its active buff')
check('string timer = se.m_ttl > 0f ? Mathf.CeilToInt(se.Remaining()).ToString() + "s" : "";' in a,'Fury\'s seconds countdown remains independently visible below its icon')
bird=region(c,'public class DragonBirdTarget')
check('base.Awake' not in bird and 'base.OnEnable' not in bird and 'base.OnDestroy' not in bird and 'enabled = false;' in bird,'Bird bridge never enters native Character lifecycle/movement setup')
check('AddComponent<Rigidbody>' not in bird and 'AddComponent<CapsuleCollider>' not in bird and 'AddComponent<BoxCollider>' not in bird,'Bird bridge does not change native collision bodies or hitboxes')
check('m_nview.Register' not in bird and 'RPC_Damage' not in bird,'Bird bridge never replaces the native destructible damage RPC')
check('private void OnCollisionStay(Collision collision) { }' in bird,'Bird bridge suppresses Character\'s inherited collision/movement callback')
check('_forwarding = true;' in bird and 'finally { _forwarding = false; }' in bird,'Bird forwarding is guarded against recursion and releases state on failure')
check('RandomFlyingBird.Instances' in region(c,'private int PatchBirdTargets(') and 'BirdAwakePostfix' in c,'Birds are attached once at spawn or from the existing registry, never via world scans per frame')
for name in ['INSTALL.bat','INSTALL.ps1']:
 check((work/name).read_bytes()==(base/name).read_bytes().replace(b'0.25.199',b'0.25.200').replace(b'GUIDANCE AND COMBO RECOVERY',b'WIND RINGS AND BIRD TARGETS').replace(b'Guidance and Combo Recovery',b'Wind Rings and Bird Targets'),name+': only release version and banner changed')
record={'passed':len(checks),'checks':checks,'method_inventory':inventory,'source_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(work.glob('*.cs'))}}
(out/'scope_audit.json').write_text(json.dumps(record,indent=2)+'\n');(out/'scope_audit.txt').write_text('\n'.join('PASS: '+n for n in checks)+'\n'+str(len(checks))+' scope checks passed.\n');print('TOTAL:',len(checks))
