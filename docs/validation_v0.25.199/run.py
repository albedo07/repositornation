from pathlib import Path
import subprocess,os,shutil,json
root=Path('/workspace/scratch/ce2f23112eca');out=root/'validation199'
refs=[p for p in (root/'compile-refs/lib/net46').glob('*.dll') if p.name not in ['UnityEngine.UI.dll','System.Xml.dll']]
refs+=list((root/'tooling197/ui/lib').glob('*.dll'))
for p in refs+[root/'tooling197/jotunn/lib/net462/Jotunn.dll']:
 if not (out/p.name).exists():shutil.copy2(p,out/p.name)
for package in (root/'tooling197/testdeps').iterdir():
 for target in ['net6.0','net5.0','netstandard2.0','net452','net40']:
  path=package/'lib'/target
  if path.exists():
   for p in path.glob('*.dll'):shutil.copy2(p,out/p.name)
   break
refs += [out/name for name in ['AlbedosCustomClasses.CombatRuntime.dll','AlbedosCustomClasses.Advanced.dll','DragonsAltar.Ranger.dll','AlbedosCustomClasses.GuardianAngel.dll','AlbedosCustomClasses.Skills.dll']]
env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1')
args=[str(root/'tooling197/dotnet/dotnet'),str(root/'tooling197/compiler/tasks/netcore/bincore/csc.dll'),'-nologo','-noconfig','-nostdlib','-target:exe','-langversion:5','-warn:0','-out:'+str(out/'ScopeTests.dll')]+['-r:'+str(p) for p in refs]+[str(root/'tests199/ScopeTests.cs'),str(root/'tests199/GeometrySnapshot.cs'),str(root/'tests199/RollbackSnapshot.cs')]
r=subprocess.run(args,env=env,capture_output=True,text=True);print(r.stdout+r.stderr);assert r.returncode==0
(out/'ScopeTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':'6.0.36'}}}))
r=subprocess.run([str(root/'tooling197/dotnet/dotnet'),str(out/'ScopeTests.dll')],env=env,capture_output=True,text=True,cwd=root)
(out/'compiled_method_tests.txt').write_text(r.stdout+r.stderr);print(r.stdout+r.stderr);assert r.returncode==0
