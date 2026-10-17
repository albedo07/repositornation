from pathlib import Path
import subprocess,os,sys
root=Path('/workspace/scratch/ce2f23112eca')
src=root/sys.argv[1]; out=root/sys.argv[2];out.mkdir(exist_ok=True)
env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1')
refs=[p for p in (root/'compile-refs/lib/net46').glob('*.dll') if p.name not in ['UnityEngine.UI.dll','System.Xml.dll']]
refs+=list((root/'tooling200/ui/lib').glob('*.dll'))
jotunn=root/'tooling200/jotunn/lib/net462/Jotunn.dll'
entries=[('AlbedosCustomClasses.Core.cs','AlbedosCustomClasses.dll',[jotunn]),('DragonsAltar.CombatRuntime.cs','AlbedosCustomClasses.CombatRuntime.dll',[]),('AlbedosCustomClasses.Skills.cs','AlbedosCustomClasses.Skills.dll',[out/'AlbedosCustomClasses.CombatRuntime.dll']),('AlbedosCustomClasses.Passives.cs','AlbedosCustomClasses.Passives.dll',[out/'AlbedosCustomClasses.CombatRuntime.dll']),('AlbedosCustomClasses.GuardianAngel.cs','AlbedosCustomClasses.GuardianAngel.dll',[out/'AlbedosCustomClasses.CombatRuntime.dll']),('AlbedosCustomClasses.Advanced.cs','AlbedosCustomClasses.Advanced.dll',[out/'AlbedosCustomClasses.dll',out/'AlbedosCustomClasses.CombatRuntime.dll',out/'AlbedosCustomClasses.Skills.dll']),('DragonsAltar.Sorcerer.cs','DragonsAltar.Sorcerer.dll',[out/'AlbedosCustomClasses.dll',out/'AlbedosCustomClasses.CombatRuntime.dll',out/'AlbedosCustomClasses.Skills.dll']),('DragonsAltar.Ranger.cs','DragonsAltar.Ranger.dll',[out/'AlbedosCustomClasses.dll',out/'AlbedosCustomClasses.CombatRuntime.dll',out/'AlbedosCustomClasses.Skills.dll']),('DragonsAltar.DevTools.cs','DragonsAltar.DevTools.dll',[]),('Aethelborn.Experience.cs','Aethelborn.Experience.dll',[])]
logs=[];fail=False
for name,dll,deps in entries:
 args=[str(root/'tooling200/dotnet/dotnet'),str(root/'tooling200/compiler/tasks/netcore/bincore/csc.dll'),'-nologo','-noconfig','-nostdlib','-target:library','-langversion:5','-warn:0','-out:'+str(out/dll)]+['-r:'+str(p) for p in refs+deps]+[str(src/name)]
 r=subprocess.run(args,env=env,capture_output=True,text=True)
 result=name+(': PASS\n' if r.returncode==0 else ': FAIL\n')+r.stdout+r.stderr
 logs.append(result);print(result,flush=True)
 if r.returncode:fail=True;break
(out/'compile.log').write_text('\n'.join(logs))
sys.exit(1 if fail else 0)
