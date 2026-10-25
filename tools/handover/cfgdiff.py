import glob,os,re,sys
rows=[]
for f in sorted(glob.glob(sys.argv[1]+'/albedo.customclasses*.cfg')):
    guid=os.path.basename(f)[:-4]; sec=None; dflt=None; typ=None
    for line in open(f,encoding='utf-8-sig'):
        line=line.rstrip('\n')
        m=re.match(r'^\[(.*)\]$',line)
        if m: sec=m.group(1); dflt=None; continue
        if line.startswith('# Default value:'): dflt=line[len('# Default value:'):].strip(); continue
        if line.startswith('# Setting type:'): typ=line.split(':',1)[1].strip(); continue
        m=re.match(r'^([^#=][^=]*?) = ?(.*)$',line)
        if m and sec is not None:
            k,v=m.group(1).strip(),m.group(2).strip()
            if dflt is not None and v!=dflt: rows.append((guid,sec,k,dflt,v,typ))
            dflt=None
for r in rows: print('|'.join(r))
