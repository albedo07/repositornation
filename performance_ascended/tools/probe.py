from pathlib import Path
import subprocess,os,json,sys
root=Path(__file__).resolve().parents[1]; refs=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else root.parent/'compile-refs/lib/net46';tools=root/'tools/toolchain';framework=tools/'runtime/shared/Microsoft.NETCore.App/6.0.36';out=root/'validation/probe-runtime';out.mkdir(exist_ok=True)
env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1')
cmd=[str(tools/'runtime/dotnet'),str(tools/'compiler/tasks/netcore/bincore/csc.dll'),'-nologo','-noconfig','-nostdlib','-langversion:5','-target:exe','-out:'+str(out/'Probe.dll')]+['-r:'+str(p) for p in framework.glob('*.dll')]+[str(root/'tests/ReferenceProbe.cs'), str(root/'tests/SupportPatchProbe.cs')]
r=subprocess.run(cmd,env=env,capture_output=True,text=True);print(r.stdout+r.stderr)
if r.returncode:raise SystemExit(r.returncode)
(out/'Probe.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':'6.0.36'}}}))
r=subprocess.run([str(tools/'runtime/dotnet'),str(out/'Probe.dll'),str(refs), str(root/'build/AethelbornPerformance.dll')],env=env,capture_output=True,text=True);print(r.stdout+r.stderr);(root/'validation/reference-probe.txt').write_text(r.stdout+r.stderr);raise SystemExit(r.returncode)
