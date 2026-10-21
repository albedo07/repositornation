"""Compile with the installer's reference selection; does not execute PowerShell."""
from pathlib import Path
import subprocess,os,sys
root=Path(__file__).resolve().parents[1];refs=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else root/'validation/game-refs'
base=['mscorlib.dll','System.dll','System.Core.dll','BepInEx.dll','0Harmony.dll','assembly_valheim.dll','assembly_utils.dll','netstandard.dll','SoftReferenceableAssets.dll']
files=[refs/n for n in base if (refs/n).exists()]+[p for p in refs.glob('UnityEngine*.dll') if p.name!='UnityEngine.UI.dll']
cmd=[str(root/'tools/toolchain/runtime/dotnet'),str(root/'tools/toolchain/compiler/tasks/netcore/bincore/csc.dll'),'-nologo','-noconfig','-nostdlib','-langversion:5','-optimize+','-target:library','-out:'+str(root/'validation/InstallerReferences.dll')]+['-r:'+str(p) for p in files]+[str(p) for p in sorted((root/'src').glob('*.cs'))]
r=subprocess.run(cmd,capture_output=True,text=True,env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1'));(root/'validation/installer-reference-compile.txt').write_text(r.stdout+r.stderr+'\nExit code: '+str(r.returncode));print(r.stdout+r.stderr);print('Installer-equivalent reference compile:',r.returncode);raise SystemExit(r.returncode)
