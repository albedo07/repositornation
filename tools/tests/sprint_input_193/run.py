from pathlib import Path
import subprocess,os,shutil,json
out=Path('validation193')
# Reuse only runtime dependencies; never overwrite the newly compiled AA modules.
for p in Path('validation192').glob('*.dll'):
 if not (out/p.name).exists() and p.name!='ScopeTests.dll':shutil.copy2(p,out/p.name)
refs=list(Path('compile-refs/lib/net46').glob('*.dll'))
refs=[p for p in refs if p.name not in ['UnityEngine.UI.dll','System.Xml.dll']]+list(Path('build192-tools/repaired-refs').glob('*.dll'))+[out/'DragonsAltar.Ranger.dll',out/'AlbedosCustomClasses.CombatRuntime.dll']
env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1')
cmd=['build192-tools/dotnet/dotnet','build192-tools/compiler/tasks/netcore/bincore/csc.dll','-nologo','-noconfig','-nostdlib','-langversion:5','-target:exe','-out:'+str(out/'ScopeTests.dll')]+['-r:'+str(p) for p in refs]+['tests193/ScopeTests.cs']
r=subprocess.run(cmd,env=env,capture_output=True,text=True);print(r.stdout+r.stderr);assert r.returncode==0
(out/'ScopeTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':'6.0.36'}}}))
r=subprocess.run(['build192-tools/dotnet/dotnet',str(out/'ScopeTests.dll')],env=env,capture_output=True,text=True)
(out/'shipped_method_tests.txt').write_text(r.stdout+r.stderr);print(r.stdout+r.stderr);assert r.returncode==0
