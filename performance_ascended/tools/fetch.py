from pathlib import Path
import urllib.request,tarfile,zipfile,hashlib,json
root=Path(__file__).resolve().parent/'toolchain';root.mkdir(parents=True,exist_ok=True)
items=[('runtime.tar.gz','https://builds.dotnet.microsoft.com/dotnet/Runtime/6.0.36/dotnet-runtime-6.0.36-linux-x64.tar.gz','runtime'),('compiler.zip','https://api.nuget.org/v3-flatcontainer/microsoft.net.compilers.toolset/4.8.0/microsoft.net.compilers.toolset.4.8.0.nupkg','compiler')]
expected={'runtime.tar.gz': '18f90612188f34d95af8c87557554a156e24dbf83d2878320e3a217e02f5453d', 'compiler.zip': '37333f4f1e2ce55e621355d6da651dc23d4cb5f94a8f76b9478816e87f110ad9'}
results=[]
for name,url,folder in items:
 p=root/name
 if not p.exists():
  with urllib.request.urlopen(url,timeout=25) as response:p.write_bytes(response.read())
 if hashlib.sha256(p.read_bytes()).hexdigest()!=expected[name]:raise RuntimeError('Download SHA256 mismatch: '+name)
 dest=root/folder;dest.mkdir(exist_ok=True)
 if name.endswith('.gz'):
  with tarfile.open(p) as z:z.extractall(dest,filter='data')
 else:
  with zipfile.ZipFile(p) as z:z.extractall(dest)
 print(name,p.stat().st_size,flush=True);results.append({'name':name,'url':url,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(root/'downloads.json').write_text(json.dumps(results,indent=2))
