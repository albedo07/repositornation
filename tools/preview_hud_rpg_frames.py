import sys, math, os, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops
ROOT=sys.argv[1]; OUT=sys.argv[2]
S=3
SER="/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"; SERR="/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf"
def F(sz,b=True): return ImageFont.truetype(SER if b else SERR,int(sz*S))
def mix(a,b,t): return tuple(int(a[i]+(b[i]-a[i])*t) for i in range(3))
def light(c,t=0.45): return mix(c,(255,255,255),t)
def dark(c,t=0.55): return mix(c,(0,0,0),t)
W,H=560*S,215*S
GOLD=(206,166,88)
HP=(170,28,30); STA=(214,150,30); EIT=(104,70,214); EXP=(226,206,128)
def P(x,y): return (int(x*S),int(y*S))
DX,DY,DR=84,114,58          # diamond portrait (1x units)
PX0,PY0,PX1,PY1=146,74,474,160   # stat panel
random.seed(7)

def scene(path,size):
    im=Image.open(path).convert("RGB"); w,h=im.size; side=min(w,h)*0.6; cx,cy=w*0.5,h*0.42
    return im.crop((int(cx-side/2),int(cy-side/2),int(cx+side/2),int(cy+side/2))).resize((size,size),Image.LANCZOS)
def icon(name,size): return Image.open(os.path.join(ROOT,"ImmortalHeroesAssets",name)).convert("RGBA").resize((size,size),Image.LANCZOS)
def txt(d,xy,s,f,c,anchor="la"):
    d.text((xy[0]+S,xy[1]+S),s,font=f,fill=(0,0,0),anchor=anchor); d.text(xy,s,font=f,fill=c,anchor=anchor)

NOISE=Image.effect_noise((W,H),40).filter(ImageFilter.GaussianBlur(S*0.6))
def metal(mask,col,shine=0.28,deep=0.62):
    """Hammered metal: dark rim, gradient body, noise grain, bevel light from top-left."""
    out=Image.new("RGBA",(W,H),(0,0,0,0))
    rim=mask.filter(ImageFilter.MaxFilter(2*S+1))
    out.paste(Image.new("RGBA",(W,H),(12,10,10,255)),(0,0),rim)
    grad=Image.new("RGB",(W,H)); gd=ImageDraw.Draw(grad)
    for y in range(0,H,2):
        t=y/H; band=0.5+0.5*math.sin(t*math.pi*3.2)
        gd.rectangle([0,y,W,y+2],fill=mix(dark(col,deep),light(col,shine),0.25+0.6*band*(1-t*0.5)))
    grain=Image.merge("RGB",[NOISE]*3)
    grad=ImageChops.overlay(grad,grain)
    out.paste(grad.convert("RGBA"),(0,0),mask)
    bl=mask.filter(ImageFilter.GaussianBlur(S*1.6))
    hi=ImageChops.subtract(bl,ImageChops.offset(bl,int(S*1.5),int(S*1.5)))
    sh=ImageChops.subtract(bl,ImageChops.offset(bl,-int(S*1.5),-int(S*1.5)))
    hi=ImageChops.multiply(hi,mask).point(lambda v:min(255,v*3)); sh=ImageChops.multiply(sh,mask).point(lambda v:min(255,v*3))
    out.paste(Image.new("RGBA",(W,H),light(col,0.85)+(255,)),(0,0),hi)
    out.paste(Image.new("RGBA",(W,H),(10,8,8,255)),(0,0),sh.point(lambda v:v*0.8))
    return out

def poly(d,pts,fill=255): d.polygon([P(x,y) for x,y in pts],fill=fill)
def chamfer(x0,y0,x1,y1,c): return [(x0+c,y0),(x1-c,y0),(x1,y0+c),(x1,y1-c),(x1-c,y1),(x0+c,y1),(x0,y1-c),(x0,y0+c)]
def thick_line(d,pts,w,fill=255):
    d.line([P(x,y) for x,y in pts],fill=fill,width=int(w*S),joint="curve")
    for x,y in (pts[0],pts[-1]): d.ellipse([P(x-w/2,y-w/2),P(x+w/2,y+w/2)],fill=fill)
def scroll(d,x,y,r,turns,w,ang0=0,dirn=1,fill=255):
    pts=[]
    for i in range(60):
        t=i/59; a=math.radians(ang0+dirn*360*turns*t); rr=r*(1-0.8*t)
        pts.append((x+math.cos(a)*rr,y+math.sin(a)*rr))
    thick_line(d,pts,w,fill)
def leaf(d,c,ang,L,Wd,fill=255):
    a=math.radians(ang); ux,uy=math.cos(a),math.sin(a); px,py=-uy,ux; pts=[]
    for i in range(13): t=i/12; w=math.sin(t*math.pi)*Wd; pts.append((c[0]+ux*L*t+px*w,c[1]+uy*L*t+py*w))
    for i in range(12,-1,-1): t=i/12; w=math.sin(t*math.pi)*Wd*0.45; pts.append((c[0]+ux*L*t-px*w,c[1]+uy*L*t-py*w))
    poly(d,pts,fill)
def feather(d,base,ang,L,Wd,fill=255):
    a=math.radians(ang); ux,uy=math.cos(a),math.sin(a); px,py=-uy,ux; bx,by=base
    pts=[(bx+px*Wd*0.3,by+py*Wd*0.3)]
    for i in range(1,8):
        t=i/8; w=Wd*(0.6+0.4*math.sin(t*math.pi))*(1-t*0.3)
        pts.append((bx+ux*L*t+px*w,by+uy*L*t+py*w)); pts.append((bx+ux*L*(t+0.04)+px*w*0.82,by+uy*L*(t+0.04)+py*w*0.82))
    pts.append((bx+ux*L,by+uy*L)); pts.append((bx+ux*L*0.5-px*Wd*0.35,by+uy*L*0.5-py*Wd*0.35)); pts.append((bx-px*Wd*0.3,by-py*Wd*0.3))
    poly(d,pts,fill)
def blade(d,base,ang,L,Wd,fill=255):
    a=math.radians(ang); ux,uy=math.cos(a),math.sin(a); px,py=-uy,ux; bx,by=base
    poly(d,[(bx+px*Wd,by+py*Wd),(bx+ux*L*0.78+px*Wd*0.9,by+uy*L*0.78+py*Wd*0.9),(bx+ux*L,by+uy*L),(bx+ux*L*0.78-px*Wd*0.9,by+uy*L*0.78-py*Wd*0.9),(bx-px*Wd,by-py*Wd)],fill)
def gem(img,c,r,col):
    g=Image.new("RGBA",(W,H),(0,0,0,0)); d=ImageDraw.Draw(g)
    x,y=P(*c); rr=int(r*S)
    d.polygon([(x,y-rr-2*S),(x+rr+2*S,y),(x,y+rr+2*S),(x-rr-2*S,y)],fill=GOLD+(255,),outline=(20,14,8,255),width=S)
    d.polygon([(x,y-rr),(x+rr,y),(x,y+rr),(x-rr,y)],fill=dark(col,0.3)+(255,))
    d.polygon([(x,y-rr),(x+rr,y),(x,y)],fill=light(col,0.35)+(255,))
    d.ellipse([x-rr//3-S,y-rr//2-S,x-rr//3+S,y-rr//2+S],fill=(255,255,255,230))
    img.alpha_composite(g)
def rivet(d,c,r=2.2):
    x,y=P(*c); rr=r*S
    d.ellipse([x-rr,y-rr,x+rr,y+rr],fill=(30,24,18),outline=None)
    d.ellipse([x-rr+S*0.6,y-rr+S*0.6,x+rr-S*0.8,y+rr-S*0.8],fill=light(GOLD,0.2))

# ---------------- Base Class ornaments (class metal mask, gold mask) ----------------
def orn_warrior(m,g):
    for ang,L in [(-158,96),(-128,104),(-100,92),(-62,70),(148,84)]:
        blade(m,(DX,DY),ang,L,7)
        a=math.radians(ang); blade(g,(DX+math.cos(a)*(L-16),DY+math.sin(a)*(L-16)),ang,16,3)   # gold blade tips
    # crossguards
    for ang in (-128,-100):
        a=math.radians(ang); cx,cy=DX+math.cos(a)*64,DY+math.sin(a)*64; px,py=-math.sin(a),math.cos(a)
        thick_line(g,[(cx-px*11,cy-py*11),(cx+px*11,cy+py*11)],4)
    # pauldron flange under the panel + spiked end (greatsword point with fuller)
    poly(m,[(140,158),(300,158),(286,172),(156,172)])
    poly(m,[(470,68),(520,114),(470,162),(482,114)])
    poly(m,[(492,98),(548,114),(492,130)])
    thick_line(g,[(496,114),(540,114)],2)
    for x in range(176,452,30): poly(m,[(x,70),(x+7,58),(x+14,70)])
def orn_cleric(m,g):
    for i,ang in enumerate([-178,-165,-152,-139,-126,-113,-100]):
        feather(m,(DX-4,DY-2),ang,DR+50-i*4,9)
    for i,ang in enumerate([-172,-150,-128,-106]):
        feather(g,(DX-4,DY-2),ang,DR+18,4)      # gold inner covert feathers
    # halo with rays
    for k in range(13):
        a=math.radians(200+k*11.5); r0,r1=DR+16,DR+(30 if k%2==0 else 24)
        thick_line(g,[(DX+math.cos(a)*r0,DY-8+math.sin(a)*r0),(DX+math.cos(a)*r1,DY-8+math.sin(a)*r1)],2.2)
    pts=[(DX+math.cos(math.radians(a))*(DR+14),DY-8+math.sin(math.radians(a))*(DR+14)) for a in range(196,346,4)]
    thick_line(g,pts,3.5)
    # hanging cross
    poly(m,[(DX-4,DY+DR+6),(DX+4,DY+DR+6),(DX+4,DY+DR+16),(DX+14,DY+DR+16),(DX+14,DY+DR+23),(DX+4,DY+DR+23),(DX+4,DY+DR+40),(DX-4,DY+DR+40),(DX-4,DY+DR+23),(DX-14,DY+DR+23),(DX-14,DY+DR+16),(DX-4,DY+DR+16)])
    # small wing at the end
    for i,ang in enumerate([-30,-14,2,18]): feather(m,(470,114),ang,48-i*5,8)
    for x in range(180,450,48): thick_line(g,[(x,66),(x+12,62),(x+24,66)],2)
def orn_sorcerer(m,g):
    pts=[(DX+14+math.cos(math.radians(a))*(DR+26),DY+math.sin(math.radians(a))*(DR+26)) for a in range(108,300,3)]
    for i in range(len(pts)-1):
        t=i/(len(pts)-1); thick_line(m,[pts[i],pts[i+1]],2+14*math.sin(t*math.pi))
    # orbiting rune circle (gold) with rune ticks
    pts=[(DX+math.cos(math.radians(a))*(DR+12),DY+math.sin(math.radians(a))*(DR+12)) for a in range(0,361,4)]
    thick_line(g,pts,1.6)
    for k in range(16):
        a=math.radians(k*22.5+8); x,y=DX+math.cos(a)*(DR+12),DY+math.sin(a)*(DR+12)
        thick_line(g,[(x-math.sin(a)*3,y+math.cos(a)*3),(x+math.sin(a)*3,y-math.cos(a)*3)],1.4)
    scroll(m,500,114,26,1.35,8,90,-1)
    scroll(g,500,114,26,1.35,2.2,90,-1)
    pts=[(420+math.cos(math.radians(a))*64,114+math.sin(math.radians(a))*64) for a in range(-64,-8,3)]
    thick_line(m,pts,6)
    for c,r in [((30,44),9),((150,46),6),((520,64),8),((532,160),5)]:
        x,y=c; poly(g,[(x,y-r),(x+r*0.28,y-r*0.28),(x+r,y),(x+r*0.28,y+r*0.28),(x,y+r),(x-r*0.28,y+r*0.28),(x-r,y),(x-r*0.28,y-r*0.28)])
def orn_ranger(m,g):
    # antler branches + leafy vine around the portrait, arrow fletching end
    for side in (-1,1):
        base=(DX+side*20,DY-DR+6)
        thick_line(m,[base,(DX+side*36,DY-DR-24),(DX+side*44,DY-DR-44)],6)
        thick_line(m,[(DX+side*34,DY-DR-20),(DX+side*58,DY-DR-30)],4.5)
        thick_line(m,[(DX+side*40,DY-DR-36),(DX+side*28,DY-DR-52)],4)
    pts=[(DX+math.cos(math.radians(a))*(DR+10),DY+math.sin(math.radians(a))*(DR+10)) for a in range(120,300,4)]
    thick_line(g,pts,3)
    for ang in range(128,300,22):
        a=math.radians(ang); c=(DX+math.cos(a)*(DR+10),DY+math.sin(a)*(DR+10))
        leaf(m,c,ang-70,28,8); leaf(g,c,ang-70,14,1.4)
    thick_line(m,[(468,114),(548,114)],6)
    for dy in (-1,1):
        for j in range(4):
            x=480+j*15
            poly(m,[(x,114),(x+20,114+dy*28),(x+30,114+dy*28),(x+13,114)])
            thick_line(g,[(x+4,114+dy*2),(x+22,114+dy*24)],1.2)
ORN={"Warrior":orn_warrior,"Cleric":orn_cleric,"Sorcerer":orn_sorcerer,"Ranger":orn_ranger}

def bar(img,d,x,y,w,h,frac,fill,col,label=None,value=None):
    c=h*0.45
    sock=chamfer(x-2,y-2,x+w+2,y+h+2,c+1)
    d.polygon([P(a,b) for a,b in sock],fill=(8,7,9))
    d.polygon([P(a,b) for a,b in chamfer(x,y,x+w,y+h,c)],fill=(26,22,26))
    if frac>0:
        fw=max(h,w*frac); fp=chamfer(x,y,x+fw,y+h,c)
        d.polygon([P(a,b) for a,b in fp],fill=dark(fill,0.15))
        d.polygon([P(a,b) for a,b in chamfer(x+1,y+1,x+fw-1,y+h*0.5,c*0.6)],fill=light(fill,0.12))
        d.line([P(x+c,y+1),P(x+fw-c,y+1)],fill=light(fill,0.55),width=S)
    for k in range(1,10):                                 # 10% ticks
        tx=x+w*k/10; d.line([P(tx,y+h*0.62),P(tx,y+h)],fill=(0,0,0),width=S)
    d.polygon([P(a,b) for a,b in sock],outline=dark(col,0.1)+(255,),width=S)
    if label: txt(d,P(x+6,y+h/2),label,F(min(11,h*0.62)),(240,228,200),"lm")
    if value: txt(d,P(x+w-6,y+h/2),value,F(min(11,h*0.62)),(255,255,255),"rm")

def hud(cls,ac,col,scene_path):
    img=Image.new("RGBA",(W,H),(0,0,0,0))
    mm=Image.new("L",(W,H),0); m=ImageDraw.Draw(mm)
    gm=Image.new("L",(W,H),0); g=ImageDraw.Draw(gm)
    ORN[cls](m,g)
    # skeleton: faceted panel frame + diamond crest frame + name banner + level shield
    poly(m,chamfer(PX0-9,PY0-9,PX1+9,PY1+9,16))
    poly(m,[(DX,DY-DR-12),(DX+DR+12,DY),(DX,DY+DR+12),(DX-DR-12,DY)])
    poly(m,[(DX,DY-DR-4),(DX+DR+4,DY),(DX,DY+DR+4),(DX-DR-4,DY)],0)
    poly(m,chamfer(PX0,PY0,PX1,PY1,10),0)
    # gold inner trims
    for pts in ([(DX,DY-DR-8),(DX+DR+8,DY),(DX,DY+DR+8),(DX-DR-8,DY)], chamfer(PX0-4,PY0-4,PX1+4,PY1+4,13)):
        thick_line(g,pts+[pts[0]],1.6)
    # banner (ribbon with folded tails)
    by0,by1=PY0-34,PY0-12; bx0,bx1=PX0+6,PX0+206
    poly(m,[(bx0-14,by0+4),(bx0,by0),(bx1,by0),(bx1+14,by0+4),(bx1+6,(by0+by1)/2+2),(bx1+14,by1+2),(bx1,by1),(bx0,by1),(bx0-14,by1+2),(bx0-6,(by0+by1)/2+2)])
    # level shield
    lx,ly=DX,DY+DR+2
    poly(m,[(lx-16,ly-4),(lx+16,ly-4),(lx+16,ly+10),(lx,ly+22),(lx-16,ly+10)])
    img.alpha_composite(metal(mm,col))
    img.alpha_composite(metal(gm,GOLD,0.42,0.5))
    d=ImageDraw.Draw(img)
    # panel interior: dark leather with tooled border + faint filigree
    d.polygon([P(a,b) for a,b in chamfer(PX0,PY0,PX1,PY1,10)],fill=(22,19,24))
    tex=Image.effect_noise((W,H),18).filter(ImageFilter.GaussianBlur(S))
    leather=Image.new("RGBA",(W,H),(40,30,30,70)); lm=Image.new("L",(W,H),0); ImageDraw.Draw(lm).polygon([P(a,b) for a,b in chamfer(PX0,PY0,PX1,PY1,10)],fill=255)
    img.paste(Image.merge("RGBA",[tex,tex.point(lambda v:v*0.8),tex.point(lambda v:v*0.8),lm.point(lambda v:v*0.25)]),(0,0),lm.point(lambda v:v*0.35))
    d=ImageDraw.Draw(img)
    d.polygon([P(a,b) for a,b in chamfer(PX0+3,PY0+3,PX1-3,PY1-3,8)],outline=dark(GOLD,0.45)+(255,),width=S)
    # diamond portrait
    s=2*DR*S; pr=scene(scene_path,s).convert("RGBA"); pmask=Image.new("L",(s,s),0)
    ImageDraw.Draw(pmask).polygon([(s/2,0),(s,s/2),(s/2,s),(0,s/2)],fill=255)
    img.paste(pr,(P(DX-DR,DY-DR)),pmask)
    vig=Image.new("L",(s,s),0); ImageDraw.Draw(vig).polygon([(s/2,0),(s,s/2),(s/2,s),(0,s/2)],outline=255,width=8*S)
    img.paste(Image.new("RGBA",(s,s),(0,0,0,255)),P(DX-DR,DY-DR),vig.filter(ImageFilter.GaussianBlur(4*S)).point(lambda v:int(v*0.7)))
    # gems + rivets
    for c in [(DX,DY-DR-8),(DX-DR-8,DY),(DX+DR+8,DY)]: gem(img,c,4,col)
    gem(img,(PX1+2,PY0+(PY1-PY0)/2),5,col)
    d=ImageDraw.Draw(img)
    for x in range(PX0+20,PX1-10,36): rivet(d,(x,PY0-5)); rivet(d,(x,PY1+5))
    # texts
    txt(d,P((bx0+bx1)/2,(by0+by1)/2+1),ac.upper()+"   ·   Blipblop",F(12),(250,240,214),"mm")
    txt(d,P(lx,ly+6),"80",F(12),(250,240,214),"mm")
    # bars
    bx=PX0+12; bw=PX1-PX0-24
    bar(img,d,bx,PY0+9,bw,24,1.0,HP,col,"HP","120 / 120")
    bar(img,d,bx,PY0+39,bw,13,0.92,STA,col,"STA","309 / 335")
    bar(img,d,bx,PY0+56,bw,13,0.0,EIT,col,"EIT","0 / 0")
    bar(img,d,bx,PY0+74,bw,6,0.35,EXP,col)
    # food sockets
    for i,(ic,t) in enumerate([("Buff_heart.png","27m"),("Buff_stamina.png","17m"),("Buff_eitr.png","17m")]):
        x=PX0+14+i*80; y=PY1+16
        d.polygon([P(a,b) for a,b in chamfer(x-2,y-2,x+28,y+28,5)],fill=dark(col,0.5),outline=GOLD+(255,),width=S)
        img.alpha_composite(icon(ic,24*S),P(x+1,y+1)); d=ImageDraw.Draw(img)
        txt(d,P(x+34,y+13),t,F(12,False),(238,230,210),"lm")
    for i,ic in enumerate(["Buff_hyper_armor.png","Buff_attack_speed.png","Buff_fury.png"]):
        x=PX1-104+i*36; y=PY0-44
        d.polygon([P(a,b) for a,b in chamfer(x-2,y-2,x+30,y+30,6)],fill=(16,14,18),outline=(90,200,110) if i<2 else (200,70,60),width=2*S)
        img.alpha_composite(icon(ic,26*S),P(x+1,y+1)); d=ImageDraw.Draw(img)
    return img

SA=os.path.join(ROOT,"docs/source_art/art_refresh/scenes"); RK=os.path.join(ROOT,"docs/source_art/ranger_kali/backgrounds")
ROWS=[("Warrior",[("Warrior",(186,46,46),SA+"/warrior.jpg"),("Sword Master",(60,128,214),SA+"/sword_master.jpg"),("Mercenary",(208,100,36),SA+"/mercenary.jpg")]),
      ("Cleric",[("Cleric",(200,168,80),SA+"/cleric.jpg"),("Paladin",(200,90,120),SA+"/paladin.jpg"),("Priest",(60,170,116),SA+"/priest.jpg")]),
      ("Sorcerer",[("Sorcerer",(140,86,212),SA+"/sorcerer.jpg"),("Archmage",(92,104,236),SA+"/archmage.jpg"),("Horizon Walker",(44,180,190),SA+"/horizon_walker.jpg")]),
      ("Ranger",[("Ranger",(98,150,58),RK+"/ranger.jpg"),("Acrobat",(84,190,148),RK+"/acrobat.jpg"),("Bowmaster",(190,138,52),RK+"/bowmaster.jpg")])]
which=sys.argv[3] if len(sys.argv)>3 else "all"
rows=[r for r in ROWS if which=="all" or r[0]==which]
GAP=14*S; top=64*S; rowh=H+30*S
sheet=Image.new("RGBA",(28*S+3*(W+GAP),top+len(rows)*rowh+8*S),(13,15,20,255)); sd=ImageDraw.Draw(sheet)
txt(sd,(28*S,22*S),"IMMORTAL HUD  -  RPG frames: one design per Base Class, metal recoloured per Advancement Class",F(19),(240,230,200))
LAB={"Warrior":"WARRIOR - blade crown with gold edges & crossguards, spiked rail, greatsword point","Cleric":"CLERIC - layered feather wing, rayed gold halo, hanging cross, wing end","Sorcerer":"SORCERER - crescent, gold rune circle, arcane scroll end, stars","Ranger":"RANGER - antlers, veined leaf vine, fletched arrow end"}
for r,(cls,acs) in enumerate(rows):
    y=top+r*rowh; txt(sd,(28*S,y),LAB[cls],F(13),(230,210,160))
    for c,(ac,col,sp) in enumerate(acs):
        x=18*S+c*(W+GAP)
        bg=scene(sp,12).resize((W,H),Image.BILINEAR).convert("RGBA"); bg=Image.blend(bg,Image.new("RGBA",bg.size,(10,12,16,255)),0.55)
        sheet.alpha_composite(bg,(x,y+20*S)); sheet.alpha_composite(hud(cls,ac,col,sp),(x,y+20*S))
sheet=sheet.resize((sheet.width//S,sheet.height//S),Image.LANCZOS); sheet.convert("RGB").save(OUT); print(sheet.size)
