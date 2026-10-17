from pathlib import Path
import subprocess,os,json,shutil,urllib.request,zipfile
from concurrent.futures import ThreadPoolExecutor
out=Path('validation194');deps=Path('tooling194/testdeps');deps.mkdir(exist_ok=True)
packages=[('monomod.backports','1.1.2'),('monomod.utils','25.0.11'),('monomod.core','1.3.3'),('monomod.runtimedetour','25.3.3'),('mono.cecil','0.11.6')]
def fetch(pair):
 name,ver=pair;d=deps/name
 if not d.exists():
  p=deps/(name+'.nupkg');urllib.request.urlretrieve('https://api.nuget.org/v3-flatcontainer/'+name+'/'+ver+'/'+name+'.'+ver+'.nupkg',p)
  with zipfile.ZipFile(p) as z:z.extractall(d)
 choices=list(d.glob('lib/*'))
 if choices:
  choice=sorted(choices,key=lambda p:(0 if p.name=='net6.0' else 1 if p.name=='netstandard2.0' else 2,str(p)))[0]
  for p in choice.glob('*.dll'):shutil.copy2(p,out/p.name)
with ThreadPoolExecutor(max_workers=5) as pool:list(pool.map(fetch,packages))
refs=[p for p in Path('compile-refs/lib/net46').glob('*.dll') if p.name not in ['UnityEngine.UI.dll','System.Xml.dll']]+[Path('tooling194/ui/lib/UnityEngine.UI.dll')]
for p in refs:
 if not (out/p.name).exists():shutil.copy2(p,out/p.name)
shutil.copy2('tooling194/jotunn/lib/net462/Jotunn.dll',out/'Jotunn.dll')
refs += [out/'AlbedosCustomClasses.CombatRuntime.dll',out/'AlbedosCustomClasses.Advanced.dll',out/'AlbedosCustomClasses.dll']
env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1')
cmd=['tooling194/dotnet/dotnet','tooling194/compiler/tasks/netcore/bincore/csc.dll','-nologo','-noconfig','-nostdlib','-langversion:5','-target:exe','-out:'+str(out/'ScopeTests.dll')]+['-r:'+str(p) for p in refs]+['tests194/ScopeTests.cs']
r=subprocess.run(cmd,env=env,capture_output=True,text=True);print(r.stdout+r.stderr);assert r.returncode==0
(out/'ScopeTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':'6.0.36'}}}))
r=subprocess.run(['tooling194/dotnet/dotnet',str(out/'ScopeTests.dll')],env=env,capture_output=True,text=True)
(out/'shipped_method_tests.txt').write_text(r.stdout+r.stderr);print(r.stdout+r.stderr);assert r.returncode==0
