from pathlib import Path
import subprocess,os,sys,json,hashlib
root=Path(__file__).resolve().parents[1];refs=Path(sys.argv[1] if len(sys.argv)>1 else (root/'validation/game-refs' if (root/'validation/game-refs').exists() else root.parent/'compile-refs/lib/net46')).resolve();out=root/'build';out.mkdir(exist_ok=True)
runtime=root/'tools/toolchain/runtime/dotnet';compiler=root/'tools/toolchain/compiler/tasks/netcore/bincore/csc.dll'
files=[x for x in refs.glob('*.dll') if x.name not in ['UnityEngine.UI.dll','System.Xml.dll']]
cmd=[str(runtime),str(compiler),'-nologo','-noconfig','-nostdlib','-langversion:5','-optimize+','-target:library','-out:'+str(out/'AethelbornPerformance.dll')]+['-r:'+str(x) for x in files]+[str(p) for p in sorted((root/'src').glob('*.cs'))]
r=subprocess.run(cmd,env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1'),capture_output=True,text=True);(root/'validation/compile.txt').write_text(r.stdout+r.stderr);print(r.stdout+r.stderr);r.check_returncode();print('C#5 compile passed')
(root/'validation/reference_hashes.json').write_text(json.dumps({p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in files},indent=2))
