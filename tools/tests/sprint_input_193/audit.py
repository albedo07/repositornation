from pathlib import Path
import hashlib,json,re
base=Path('working192');work=Path('working193');out=Path('validation193')
checks=[]
def check(value, label):
 assert value,label
 checks.append(label);print('PASS:',label)
def source(name):return (work/name).read_text(encoding='utf-8-sig')
anchor=Path('upload/Aethelborn_Ascended_v0.25.191_VFX_Finish.zip')
prev=Path('output/Aethelborn_Ascended_v0.25.192_Ranger_Mobility_Buff_Timers.zip')
check(hashlib.sha256(anchor.read_bytes()).hexdigest()=='2a616dfc4b69484940cfd79e07a08300bc07dc8161416499537b438a95af7783','Original v191 Anchor archive unchanged')
check(hashlib.sha256(prev.read_bytes()).hexdigest()=='e0dc53217533696acb4f3aae75b524c1a97bd7011aeb0504f8616a6db9f66ec4','Original v192 working archive unchanged')
assets=[p for p in (base/'ImmortalHeroesAssets').rglob('*') if p.is_file()]
check(all(p.read_bytes()==(work/p.relative_to(base)).read_bytes() for p in assets),str(len(assets))+' existing asset files byte-identical')
changed={p.name for p in work.glob('*.cs') if p.read_bytes()!=(base/p.name).read_bytes()}
check(changed=={'DragonsAltar.CombatRuntime.cs','DragonsAltar.Ranger.cs','DragonsAltar.Sorcerer.cs','AlbedosCustomClasses.Advanced.cs','AlbedosCustomClasses.Skills.cs'},'Only five scoped C# modules changed')
for name in ['INSTALL.bat','INSTALL.ps1']:
 a=(base/name).read_bytes();expected=a.replace(b'v0.25.192',b'v0.25.193').replace(b'RANGER MOBILITY AND BUFF TIMERS',b'SPRINT RELOAD AND WORLD INPUT')
 check((work/name).read_bytes()==expected,name+' title text only; installation behavior identical')
s=source('DragonsAltar.Sorcerer.cs');c=source('DragonsAltar.CombatRuntime.cs');a=source('AlbedosCustomClasses.Advanced.cs');r=source('DragonsAltar.Ranger.cs');k=source('AlbedosCustomClasses.Skills.cs')
check('AddComponent<HwLegCycle>' not in s and 'legs.ActiveUntil =' not in s,'Custom Horizon Walker leg animation no longer installed or refreshed')
check('if (_hwFiring && __instance != null && __instance == Player.m_localPlayer && __result < 1f) __result = 1f;' in s,'Existing Horizon Walker full-speed shooting factor retained')
check('cls == "Ranger" && kind == "magic"' in c and 'cls == "Sorcerer" && (kind == "bow" || kind == "crossbow")' in c,'Central equip rules reject Ranger magic weapons and Sorcerer bows/crossbows for the whole bloodline')
check('if (string.IsNullOrEmpty(cls)) return null;' in c,'No-class equipment exemption retained')
# The sections below have no visual behavior and are outside the requested scope.
original=(base/'DragonsAltar.CombatRuntime.cs').read_text(encoding='utf-8-sig')
for start,end,label in [('        // ------------------------------------------------------------------ v0.25.169 WEAPON RESTRICTIONS', '        public static string WeaponRuleViolation', 'Existing equipment classifier and utility-axe rules retained'),('    public class DragonSkillClipDriver', '    public static class DragonCombat','Shared skill animation driver retained')]:
 if start in original and end in original[original.index(start):]:
  before=original[original.index(start):original.index(end,original.index(start))];after=c[c.index(start):c.index(end,c.index(start))]
  check(before==after,label)
check('Mathf.Clamp(_crescentAscTrailTime.Value, 0.5f, 2f)' in a and 'Mathf.Min(0.5f, cast.Times[p] + life - Time.time)' in a,'Crescent fire segments keep their existing two-second lifetime cap')
# All damage operations, waits, RNG and gameplay configs stay byte-for-byte at the call-site line level.
for name in changed:
 b=(base/name).read_text(encoding='utf-8-sig');n=source(name)
 extract=lambda t:[l.strip() for l in t.splitlines() if ('Config.Bind(' in l or 'new WaitForSeconds(' in l or 'UnityEngine.Random.' in l or '.Damage(' in l or '.m_damage.' in l or 'ApplyFireBurnTick(' in l or 'IhDealRaw(' in l or 'IhAddBurnStack(' in l)]
 check(extract(b)==extract(n),name+': config bindings, damage statements, wait intervals and gameplay RNG retained')
check('if (_phalanxVolleyArmed && DragonCombat.CanUseWorldInput(player)' in s,'Phalanx all-swords volley has a UI input gate before consuming its armed state')
for signature in ['private void TryTriggerSpellcasterHeldFire(Player player)','public void QueueMimicAttack(Player player, ItemDrop.ItemData weapon)','private void UpdateSpellcasterHeldFire(Player player)']:
 block=s[s.index(signature):s.index(signature)+350]
 check('CanUseWorldInput(player)' in block,signature+' rejects UI input')
check('!Application.isFocused' in c and 'Cursor.visible' in c and '_worldInputCheck(player)' in c and '_explicitUiInputBlocked' in c,'World input guard checks focus, free cursor, mod input blocking and native TakeInput')
check('if (!DragonCombat.CanUseWorldInput(player))' in a[a.index('private void HandleTreeHotbarInput'):a.index('private void HandleTreeHotbarInput')+250],'Universal hotbar input uses the shared world gate')
check('CanUseWorldInput(player) || !Input.GetKey(_modifier.Value)' in k and 'CanUseWorldInput(player) || !Input.GetKey(_modifier.Value)' in s,'Starter and Sorcerer legacy hotkeys use the shared world gate')
check('!DragonCombat.CanUseWorldInput(player)) { DragonCombat.ClipStop(player, 0.1f); yield break; }' in r and '_meteorChargeShown = 0; _meteorChargeNext = 0f;' in s,'Opening UI cancels held charge input instead of releasing a new shot')
# Explicit compatibility blacklist applies to new calls; do not rewrite any unrelated pre-existing code.
for name in changed:
 old=(base/name).read_text(encoding='utf-8-sig');new=source(name)
 for forbidden in ['.SetPositions(','.GetPositions(','.LoadImage(']:check(new.count(forbidden)==old.count(forbidden),name+': no new '+forbidden+' calls')
(out/'scope_audit.json').write_text(json.dumps({'passed':len(checks),'checks':checks},indent=2))
print('Passed',len(checks),'static scope checks; game behavior and rendered appearance not exercised.')
