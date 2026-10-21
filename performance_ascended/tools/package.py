from pathlib import Path
import zipfile,hashlib,json,difflib
root=Path(__file__).resolve().parents[1];dest=root/'dist';dest.mkdir(exist_ok=True)
previous=root.parent/'performance_no_shadows/dist/AethelbornPerformance_v0.3.2_NoShadows.zip'
files={n:root/n for n in ['README.txt','TEST_THIS_BUILD.txt','TEST_RESULTS_v0.3.3.txt','CHANGELOG_v0.3.3.txt','FINDINGS_v0.3.3.txt','BASELINE.txt','INSTALL.bat','INSTALL.ps1','COLLECT_REPORTS.bat','COLLECT_REPORTS.ps1','AGENTS.md']}
for folder,patterns in [('src',['*.cs']),('tests',['*.cs','*.py']),('tools',['*.py'])]:
 for pattern in patterns:
  for p in (root/folder).glob(pattern):files[p.relative_to(root).as_posix()]=p
for n in ['compile.txt','installer-reference-compile.txt','reference_hashes.json','reference-probe.txt','tests.txt','default-config.cfg']:
 files['validation/'+n]=root/'validation'/n
files['BepInEx/plugins/AethelbornPerformance/AethelbornPerformance.dll']=root/'build/AethelbornPerformance.dll'
patch=[];changed=[]
assert hashlib.sha256(previous.read_bytes()).hexdigest() in (root/'BASELINE.txt').read_text(), 'Baseline archive hash changed'
with zipfile.ZipFile(previous) as before:
 for name,p in sorted(files.items()):
  if name.startswith('validation/') or p.suffix=='.dll':continue
  a=before.read(name).decode('utf-8') if name in before.namelist() else ''
  b=p.read_text()
  if a!=b:
   changed.append(name);patch.extend(difflib.unified_diff(a.splitlines(True),b.splitlines(True),fromfile='v0.3.2/'+name,tofile='v0.3.3/'+name))
 for name in ['INSTALL.bat','INSTALL.ps1']:
  assert before.read(name).decode('utf-8').replace('v0.3.2','v0.3.3')==files[name].read_text(),name
 for name in ['COLLECT_REPORTS.ps1','COLLECT_REPORTS.bat','src/RuntimeDiagnostics.cs','src/FrameRecorder.cs','src/DiagnosticData.cs']:
  assert before.read(name)==files[name].read_bytes(),name

 def method(text,name):
  start=text.index('        private '+name)
  end=text.find('\n        private ',start+1)
  return text[start:end if end>=0 else len(text)]
 old=before.read('src/PerformancePlugin.cs').decode('utf-8');new=(root/'src/PerformancePlugin.cs').read_text()
 for name in ['void RegisterCosmetic(', 'void UpdateParticles(', 'static void RestorePiece(', 'static void RestoreParticle(', 'void RestoreParticles(', 'void UpdateGlobalShadows(', 'void RestoreGlobalShadows(']:
  assert method(old,name)==method(new,name),name
(root/'changes_from_v0.3.2.diff').write_text(''.join(patch));files['changes_from_v0.3.2.diff']=root/'changes_from_v0.3.2.diff'
(root/'SCOPE.txt').write_text('Changed/added text files:\n'+'\n'.join(changed)+'\n\nScope: native support-cache callback subscription storage, ordered event policy,\ncoverage/cost reporting, bypassed-discovery lifecycle fast path, tests/docs and\nrelease labels. No changes to native unload scheduling, support calculations,\nAA, particle features, shadow mode, memory adapter, physics or saves.\n');files['SCOPE.txt']=root/'SCOPE.txt'
manifest={n:hashlib.sha256(p.read_bytes()).hexdigest() for n,p in sorted(files.items())}
out=dest/'AethelbornPerformance_v0.3.3_CleanupAllocations.zip'
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED) as z:
 for n,p in sorted(files.items()):z.write(p,n)
 z.writestr('SHA256SUMS.json',json.dumps(manifest,indent=2)+'\n')
with zipfile.ZipFile(out) as z:
 assert z.testzip() is None
 for n,h in manifest.items():assert hashlib.sha256(z.read(n)).hexdigest()==h
 assert len([n for n in z.namelist() if n.endswith('.dll')])==1
 assert len([n for n in z.namelist() if n.startswith('src/') and n.endswith('.cs')])==6
print(out);print('Scope, installer/collector preservation and package hashes verified;',out.stat().st_size,'bytes')
