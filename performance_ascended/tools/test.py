from pathlib import Path
import subprocess,os,json,shutil,sys
root=Path(__file__).resolve().parents[1]
refs=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else root.parent/'compile-refs/lib/net46'
tools=root/'tools/toolchain'; out=root/'validation/test-runtime';out.mkdir(parents=True,exist_ok=True)
framework=tools/'runtime/shared/Microsoft.NETCore.App/6.0.36'
real=[refs/'BepInEx.dll',refs/'UnityEngine.dll',refs/'UnityEngine.CoreModule.dll',refs/'UnityEngine.InputLegacyModule.dll']
sources=[root/'tests/PolicyTests.cs']+[root/'src'/s for s in ['PerformancePolicy.cs','FrameRecorder.cs','PerformanceSettings.cs','DiagnosticData.cs']]
cmd=[str(tools/'runtime/dotnet'),str(tools/'compiler/tasks/netcore/bincore/csc.dll'),'-nologo','-noconfig','-nostdlib','-langversion:5','-target:exe','-out:'+str(out/'Tests.dll')]+['-r:'+str(p) for p in list(framework.glob('*.dll'))+real]+list(map(str,sources))
env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='0')
r=subprocess.run(cmd,env=env,capture_output=True,text=True);print(r.stdout+r.stderr);
if r.returncode: raise SystemExit(r.returncode)
for p in real:shutil.copy2(p,out/p.name)
(out/'Tests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':'6.0.36'}}}))
r=subprocess.run([str(tools/'runtime/dotnet'),str(out/'Tests.dll'),str(root/'validation/default-config.cfg')],env=env,capture_output=True,text=True);text=r.stdout+r.stderr;print(text);(root/'validation/tests.txt').write_text(text);
if r.returncode: raise SystemExit(r.returncode)
