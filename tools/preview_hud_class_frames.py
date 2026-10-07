import sys, math, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops
ROOT=sys.argv[1]; OUT=sys.argv[2]
S=3
SER="/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"; SERR="/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf"
def F(sz,b=True): return ImageFont.truetype(SER if b else SERR,int(sz*S))
def mix(a,b,t): return tuple(int(a[i]+(b[i]-a[i])*t) for i in range(3))
def light(c,t=0.45): return mix(c,(255,255,255),t)
def dark(c,t=0.55): return mix(c,(0,0,0),t)
W,H=520*S,200*S
HP=(178,34,34); STA=(226,160,38); EIT=(110,74,220); EXP=(232,214,140)
CX,CY,R=78*S,108*S,44*S          # medallion
PX0,PY0,PX1,PY1=124*S,74*S,430*S,150*S   # bar panel interior
def P(x,y): return (x*S,y*S)

def scene(path,size):
    im=Image.open(path).convert("RGB"); w,h=im.size; side=min(w,h)*0.6; cx,cy=w*0.5,h*0.42
    return im.crop((int(cx-side/2),int(cy-side/2),int(cx+side/2),int(cy+side/2))).resize((size,size),Image.LANCZOS)
def icon(name,size): return Image.open(os.path.join(ROOT,"ImmortalHeroesAssets",name)).convert("RGBA").resize((size,size),Image.LANCZOS)
def txt(d,xy,s,f,c,anchor="la"):
    d.text((xy[0]+S,xy[1]+S),s,font=f,fill=(0,0,0),anchor=anchor); d.text(xy,s,font=f,fill=c,anchor=anchor)

# ---------- ornament primitives (drawn into a mask) ----------
def feather(d,base,ang,length,width):
    a=math.radians(ang); ux,uy=math.cos(a),math.sin(a); px,py=-uy,ux
    bx,by=base; tip=(bx+ux*length,by+uy*length)
    pts=[(bx+px*width*0.3,by+py*width*0.3),(bx+ux*length*0.55+px*width,by+uy*length*0.55+py*width),tip,(bx+ux*length*0.6-px*width*0.35,by+uy*length*0.6-py*width*0.35),(bx-px*width*0.3,by-py*width*0.3)]
    d.polygon(pts,fill=255)
def leaf(d,c,ang,length,width):
    a=math.radians(ang); ux,uy=math.cos(a),math.sin(a); px,py=-uy,ux; pts=[]
    for i in range(13):
        t=i/12; w=math.sin(t*math.pi)*width
        pts.append((c[0]+ux*length*t+px*w,c[1]+uy*length*t+py*w))
    for i in range(12,-1,-1):
        t=i/12; w=math.sin(t*math.pi)*width*0.35
        pts.append((c[0]+ux*length*t-px*w,c[1]+uy*length*t-py*w))
    d.polygon(pts,fill=255)
def blade(d,base,ang,length,width):
    a=math.radians(ang); ux,uy=math.cos(a),math.sin(a); px,py=-uy,ux; bx,by=base
    d.polygon([(bx+px*width,by+py*width),(bx+ux*length*0.8+px*width*0.8,by+uy*length*0.8+py*width*0.8),(bx+ux*length,by+uy*length),(bx+ux*length*0.8-px*width*0.8,by+uy*length*0.8-py*width*0.8),(bx-px*width,by-py*width)],fill=255)
def arcline(d,c,r,a0,a1,w,taper=True):
    n=40; pts=[]
    for i in range(n+1):
        t=i/n; a=math.radians(a0+(a1-a0)*t); ww=w*(math.sin(t*math.pi) if taper else 1)+S*0.6
        x,y=c[0]+math.cos(a)*r,c[1]+math.sin(a)*r
        d.ellipse([x-ww/2,y-ww/2,x+ww/2,y+ww/2],fill=255)
def spiral(d,c,r0,turns,w,ang0=0,dirn=1):
    n=80
    for i in range(n+1):
        t=i/n; a=math.radians(ang0+dirn*360*turns*t); r=r0*(1-t*0.85); ww=w*(1-t*0.7)+S*0.5
        x,y=c[0]+math.cos(a)*r,c[1]+math.sin(a)*r
        d.ellipse([x-ww/2,y-ww/2,x+ww/2,y+ww/2],fill=255)
def star(d,c,r):
    pts=[]
    for i in range(8):
        rr=r if i%2==0 else r*0.3; a=math.radians(i*45-90)
        pts.append((c[0]+math.cos(a)*rr,c[1]+math.sin(a)*rr))
    d.polygon(pts,fill=255)

# ---------- per-Class frame ornaments ----------
def orn_warrior(d):
    # crown of blades behind the medallion + sword-point end cap + notched top rail
    for ang,L in [(-150,70),(-120,78),(-95,64),(160,58)]:
        blade(d,(CX,CY),ang,(R/S+L)*S,9*S)
    d.polygon([P(430,70),P(470,112),P(430,154),P(442,112)],fill=255)       # sword tip
    d.polygon([P(452,96),P(492,112),P(452,128)],fill=255)
    for x in range(160,420,34): d.polygon([P(x,70),P(x+8,62),P(x+16,70)],fill=255)
    d.rectangle([P(120,152),P(300,157)],fill=255)
def orn_cleric(d):
    for i,ang in enumerate([-175,-160,-145,-130,-115,-100]):     # wing behind the medallion
        feather(d,(CX-8*S,CY-4*S),ang,(R/S+40-i*3)*S,10*S)
    arcline(d,(CX,CY-6*S),R+18*S,200,340,7*S)                      # halo
    d.rectangle([CX-3*S,CY+R+2*S,CX+3*S,CY+R+30*S],fill=255); d.rectangle([CX-12*S,CY+R+10*S,CX+12*S,CY+R+16*S],fill=255)
    for i,ang in enumerate([-20,-5,10]): feather(d,P(430,112),ang,(44-i*6)*S,8*S)  # small wing at the end
def orn_sorcerer(d):
    arcline(d,(CX+10*S,CY),R+22*S,110,290,16*S)                    # crescent swoosh (reference)
    arcline(d,(CX+30*S,CY-34*S),40*S,180,330,8*S)
    spiral(d,P(452,112),22*S,1.3,9*S,90,-1)                        # end curl
    arcline(d,P(395,112),58*S,-60,-10,7*S)
    for c,r in [(P(30,48),9),(P(150,52),6),(P(470,72),7)]: star(d,c,r*S)
def orn_ranger(d):
    # vine with leaves around the medallion + arrow fletching end
    arcline(d,(CX,CY),R+12*S,130,330,6*S,False)
    for ang in [140,170,200,230,260,290,320]:
        a=math.radians(ang); c=(CX+math.cos(a)*(R+12*S),CY+math.sin(a)*(R+12*S))
        leaf(d,c,ang-60,26*S,8*S)
    for k,dy in enumerate([-1,1]):
        for j in range(3):
            x=436+j*14
            d.polygon([P(x,112),P(x+22,112+dy*24),P(x+30,112+dy*24),P(x+12,112)],fill=255)
    d.rectangle([P(430,109),P(488,115)],fill=255)
ORN={"Warrior":orn_warrior,"Cleric":orn_cleric,"Sorcerer":orn_sorcerer,"Ranger":orn_ranger}

def metalize(mask,col):
    # dark outline + vertical metal gradient in the AC colour + bevel light/shadow
    out=Image.new("RGBA",(W,H),(0,0,0,0))
    outline=mask.filter(ImageFilter.MaxFilter(2*S+1))
    out.paste(Image.new("RGBA",(W,H),dark(col,0.75)+(255,)),(0,0),outline)
    grad=Image.new("RGBA",(W,H)); gd=ImageDraw.Draw(grad)
    for y in range(H):
        t=y/H; c=mix(light(col,0.55),dark(col,0.35),t); gd.line([(0,y),(W,y)],fill=c+(255,))
    out.paste(grad,(0,0),mask)
    hi=ImageChops.subtract(mask,ImageChops.offset(mask,S,S)); sh=ImageChops.subtract(mask,ImageChops.offset(mask,-S,-S))
    out.paste(Image.new("RGBA",(W,H),light(col,0.8)+(255,)),(0,0),hi.point(lambda v:v*0.8))
    out.paste(Image.new("RGBA",(W,H),dark(col,0.6)+(255,)),(0,0),sh.point(lambda v:v*0.7))
    return out

def bar(d,x,y,w,h,frac,fill,edge):
    r=h//2; d.rounded_rectangle([x,y,x+w,y+h],r,fill=(14,12,18))
    if frac>0:
        fw=max(h,int(w*frac)); d.rounded_rectangle([x,y,x+fw,y+h],r,fill=fill)
        d.rounded_rectangle([x+2*S,y+S,x+fw-2*S,y+h//2],max(1,r//2),fill=light(fill,0.22))
    d.rounded_rectangle([x,y,x+w,y+h],r,outline=dark(edge,0.2),width=S)

def hud(cls,ac,col,scene_path):
    img=Image.new("RGBA",(W,H),(0,0,0,0))
    m=Image.new("L",(W,H),0); md=ImageDraw.Draw(m)
    ORN[cls](md)
    # medallion ring + panel rails (the shared skeleton)
    md.ellipse([CX-R-9*S,CY-R-9*S,CX+R+9*S,CY+R+9*S],fill=255)
    md.rounded_rectangle([PX0-10*S,PY0-7*S,PX1+6*S,PY1+7*S],18*S,fill=255)
    # cut the openings
    md.ellipse([CX-R,CY-R,CX+R,CY+R],fill=0)
    md.rounded_rectangle([PX0,PY0,PX1,PY1],12*S,fill=0)
    md.ellipse([CX-R-2*S,CY-R-2*S,CX+R+2*S,CY+R+2*S],outline=255,width=2*S)
    # contents
    d=ImageDraw.Draw(img)
    d.rounded_rectangle([PX0,PY0,PX1,PY1],12*S,fill=(16,15,24,238))
    pr=scene(scene_path,2*R); pm=Image.new("L",(2*R,2*R),0); ImageDraw.Draw(pm).ellipse([0,0,2*R-1,2*R-1],fill=255)
    img.paste(pr,(CX-R,CY-R),pm)
    img.alpha_composite(metalize(m,col))
    d=ImageDraw.Draw(img)
    # level badge
    d.ellipse([CX+R-26*S,CY+R-24*S,CX+R+4*S,CY+R+6*S],fill=(18,16,24),outline=light(col,0.3),width=2*S)
    txt(d,(CX+R-11*S,CY+R-9*S),"80",F(12),(245,240,225),"mm")
    bx=PX0+12*S; bw=PX1-PX0-24*S
    bar(d,bx,PY0+8*S,bw,22*S,1.0,HP,col); txt(d,(bx+bw//2,PY0+19*S),"120 / 120",F(12),(255,255,255),"mm")
    bar(d,bx,PY0+35*S,bw,11*S,0.92,STA,col)
    bar(d,bx,PY0+50*S,bw,11*S,0.0,EIT,col)
    bar(d,bx,PY0+65*S,bw,5*S,0.35,EXP,col)
    txt(d,(PX0+6*S,PY0-26*S),ac.upper(),F(14),light(col,0.45)); txt(d,(PX0+12*S+d.textlength(ac.upper(),font=F(14)),PY0-24*S),"  Blipblop",F(12,False),(225,220,205))
    for i,(ic,t) in enumerate([("Buff_heart.png","27m"),("Buff_stamina.png","17m"),("Buff_eitr.png","17m")]):
        x=PX0+12*S+i*76*S; y=PY1+14*S
        d.rounded_rectangle([x,y,x+26*S,y+26*S],5*S,fill=(18,16,24),outline=col,width=2*S)
        img.alpha_composite(icon(ic,22*S),(x+2*S,y+2*S)); txt(d,(x+32*S,y+13*S),t,F(12,False),(235,230,215),"lm")
    for i,ic in enumerate(["Buff_hyper_armor.png","Buff_attack_speed.png"]):
        x=PX1-70*S+i*36*S; y=PY0-36*S
        d.rounded_rectangle([x,y,x+28*S,y+28*S],5*S,fill=(18,16,24),outline=(90,200,110),width=2*S)
        img.alpha_composite(icon(ic,24*S),(x+2*S,y+2*S))
    return img

SA=os.path.join(ROOT,"docs/source_art/art_refresh/scenes"); RK=os.path.join(ROOT,"docs/source_art/ranger_kali/backgrounds")
ROWS=[("Warrior",[("Warrior",(196,52,52),SA+"/warrior.jpg"),("Sword Master",(66,142,226),SA+"/sword_master.jpg"),("Mercenary",(222,110,40),SA+"/mercenary.jpg")]),
      ("Cleric",[("Cleric",(214,178,72),SA+"/cleric.jpg"),("Paladin",(214,96,128),SA+"/paladin.jpg"),("Priest",(64,184,124),SA+"/priest.jpg")]),
      ("Sorcerer",[("Sorcerer",(150,92,224),SA+"/sorcerer.jpg"),("Archmage",(96,110,255),SA+"/archmage.jpg"),("Horizon Walker",(46,196,204),SA+"/horizon_walker.jpg")]),
      ("Ranger",[("Ranger",(104,160,62),RK+"/ranger.jpg"),("Acrobat",(92,206,160),RK+"/acrobat.jpg"),("Bowmaster",(204,148,56),RK+"/bowmaster.jpg")])]
GAP=16*S; top=70*S; rowh=H+34*S
sheet=Image.new("RGBA",(30*S+3*(W+GAP),top+4*rowh+10*S),(13,15,20,255)); sd=ImageDraw.Draw(sheet)
txt(sd,(30*S,24*S),"IMMORTAL HUD  -  one frame design per Base Class, recoloured per Advancement Class",F(20),(240,230,200))
for r,(cls,acs) in enumerate(ROWS):
    y=top+r*rowh
    labels={"Warrior":"WARRIOR - crown of blades, sword-point end","Cleric":"CLERIC - feathered wing, halo, cross","Sorcerer":"SORCERER - crescent swoosh, arcane curl, stars","Ranger":"RANGER - leaf vine, arrow fletching end"}
    txt(sd,(30*S,y),labels[cls],F(14),(230,210,160))
    for c,(ac,col,sp) in enumerate(acs):
        x=20*S+c*(W+GAP)
        bg=scene(sp,12).resize((W,H),Image.BILINEAR).convert("RGBA"); bg=Image.blend(bg,Image.new("RGBA",bg.size,(10,12,16,255)),0.6)
        sheet.alpha_composite(bg,(x,y+22*S)); sheet.alpha_composite(hud(cls,ac,col,sp),(x,y+22*S))
sheet=sheet.resize((sheet.width//S,sheet.height//S),Image.LANCZOS); sheet.convert("RGB").save(OUT); print(sheet.size)
