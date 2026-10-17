from pathlib import Path
import hashlib,json,re
base=Path('baseline196');work=Path('working197');out=Path('validation197');checks=[]
def check(ok,name):
 if not ok:raise AssertionError(name)
 checks.append(name);print('PASS:',name)
def read(root,name):return (root/name).read_text(encoding='utf-8-sig')
def end_region(s,start):
 p=s.index('{',start);depth=1;i=p+1
 while depth:
  if s.startswith('//',i):
   k=s.find('\n',i);i=len(s) if k<0 else k;continue
  if s.startswith('/*',i):i=s.index('*/',i+2)+2;continue
  if s[i] in ['"',"'"]:
   quote=s[i];verbatim=i>0 and s[i-1]=='@';i+=1
   while True:
    if not verbatim and s[i]=='\\':i+=2;continue
    if s[i]==quote:
     if verbatim and s[i+1]==quote:i+=2;continue
     i+=1;break
    i+=1
   continue
  if s[i]=='{':depth+=1
  elif s[i]=='}':depth-=1
  i+=1
 return i
def region(s,marker):
 start=s.index(marker);return s[start:end_region(s,start)]
def methods(s):
 pattern=r'^[ \t]*(?:(?:public|private|internal|protected|static|virtual|override|sealed|async|new|extern|unsafe)\s+)+[\w<>,?\[\].]+\s+(\w+)\s*\([^;{}]*?\)\s*(?:where[^{}]+)?\{'
 result={};seen={}
 for m in re.finditer(pattern,s,re.M):
  signature=re.sub(r'\s+',' ',s[m.start():s.index('{',m.start())]).strip();i=seen.get(signature,0);seen[signature]=i+1
  result[(signature,i)]=(m.group(1),s[m.start():end_region(s,m.start())])
 return result

