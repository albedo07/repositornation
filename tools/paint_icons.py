# v0.25.282 unified status icon painter: one symbol language for every buff/debuff/cooldown icon.
# Base symbol (sword = damage, shield = defense, winged boot = speed, ...) + optional corner badge naming the source.
# Buffs: dark round plate, coloured ring. Cooldowns: darker plate, grey ring, dimmed symbol, hourglass badge.
# Enemy debuffs: dark-red diamond plate.  Usage: python3 tools/paint_icons.py <outdir> [sheet.png]
import sys, math, os
from PIL import Image, ImageDraw, ImageFilter
S = 512
RED=(232,64,52); BLUE=(70,150,255); GOLD=(250,200,70); GREEN=(90,215,110); VIOLET=(185,95,255)
ORANGE=(255,140,40); CYAN=(80,220,210); WHITE=(240,240,240); PINK=(245,110,150); YELLOW=(250,225,60)
GREY=(120,124,132); ICE=(150,220,255); POISON=(130,220,60); BROWN=(190,140,90)

def dim(c, k=0.45): return tuple(int(v * k + 40 * (1 - k)) for v in c)
def lighter(c, k=0.45): return tuple(int(v + (255 - v) * k) for v in c)

def plate(d, ring, cooldown=False, debuff=False):
    if debuff:
        pts=[(S/2,10),(S-10,S/2),(S/2,S-10),(10,S/2)]
        d.polygon(pts, fill=(34,12,14)); d.line(pts+[pts[0]], fill=ring, width=26, joint='curve')
        return
    d.ellipse((8,8,S-8,S-8), fill=(10,11,14) if cooldown else (18,20,26))
    d.ellipse((8,8,S-8,S-8), outline=(70,72,78) if cooldown else ring, width=26)

# ---- symbols (drawn in a 512 box, centre 256) ----
def sword(d, c, cx=256, cy=256, k=1.0, ang=0):
    def P(x,y):
        a=math.radians(ang); x-=256; y-=256
        return (cx + (x*math.cos(a)-y*math.sin(a))*k, cy + (x*math.sin(a)+y*math.cos(a))*k)
    blade=[P(256,92),P(284,140),P(284,300),P(228,300),P(228,140)]
    d.polygon(blade, fill=c); d.polygon([P(256,100),P(270,140),P(270,292),P(256,292)], fill=lighter(c))
    d.polygon([P(176,300),P(336,300),P(336,330),P(176,330)], fill=lighter(c,0.2))
    d.polygon([P(240,330),P(272,330),P(272,400),P(240,400)], fill=dim(c,0.7))
    d.ellipse([P(232,396)[0]-0,P(232,396)[1]-0,P(232,396)[0]+48*k,P(232,396)[1]+48*k], fill=lighter(c,0.2))

def shield(d, c, cx=256, cy=256, k=1.0, cross=False):
    pts=[(cx-130*k,cy-130*k),(cx+130*k,cy-130*k),(cx+130*k,cy+10*k),(cx,cy+150*k),(cx-130*k,cy+10*k)]
    d.polygon(pts, fill=c); d.polygon([(cx,cy-130*k),(cx+130*k,cy-130*k),(cx+130*k,cy+10*k),(cx,cy+150*k)], fill=lighter(c,0.25))
    if cross:
        d.rectangle((cx-14*k,cy-110*k,cx+14*k,cy+110*k), fill=(250,250,240)); d.rectangle((cx-90*k,cy-50*k,cx+90*k,cy-22*k), fill=(250,250,240))

def boot(d, c, cx=256, cy=256, k=1.0):
    b=[(cx-60*k,cy-130*k),(cx+20*k,cy-130*k),(cx+20*k,cy+40*k),(cx+120*k,cy+70*k),(cx+120*k,cy+130*k),(cx-60*k,cy+130*k)]
    d.polygon(b, fill=c)
    for i in range(3):
        y=cy-110*k+i*42*k; d.polygon([(cx-60*k,y),(cx-170*k+i*25*k,y-30*k),(cx-150*k+i*25*k,y+18*k),(cx-60*k,y+30*k)], fill=lighter(c,0.5))

def roundbuckler(d, c, cx=256, cy=256, r=120):
    d.ellipse((cx-r,cy-r,cx+r,cy+r), fill=c); d.ellipse((cx-r*.72,cy-r*.72,cx+r*.72,cy+r*.72), outline=lighter(c,.4), width=int(r*.12))
    d.ellipse((cx-r*.25,cy-r*.25,cx+r*.25,cy+r*.25), fill=lighter(c,.55))

def rings(d, c, cx=256, cy=256):
    for i,r in enumerate((150,195)):
        d.arc((cx-r,cy-r,cx+r,cy+r), 200, 340, fill=c, width=20-i*5); d.arc((cx-r,cy-r,cx+r,cy+r), 20, 160, fill=c, width=20-i*5)

def armor(d, c):
    pts=[(150,140),(210,120),(256,150),(302,120),(362,140),(390,220),(345,240),(340,380),(172,380),(167,240),(122,220)]
    d.polygon(pts, fill=c); d.line([(256,155),(256,375)], fill=lighter(c,.5), width=14)
    d.line([(190,270),(322,270)], fill=dim(c,.7), width=12); d.line([(195,320),(317,320)], fill=dim(c,.7), width=12)

def star(d, c, cx=256, cy=256, r=150, n=5, inner=0.42):
    pts=[]
    for i in range(n*2):
        a=-math.pi/2+i*math.pi/n; rr=r if i%2==0 else r*inner; pts.append((cx+rr*math.cos(a), cy+rr*math.sin(a)))
    d.polygon(pts, fill=c)

def drop(d, c, cx=256, cy=256, k=1.0):
    d.ellipse((cx-95*k,cy-10*k,cx+95*k,cy+180*k-90*k+90*k), fill=c); d.polygon([(cx,cy-160*k),(cx+88*k,cy+40*k),(cx-88*k,cy+40*k)], fill=c)
    d.ellipse((cx-40*k,cy+30*k,cx-5*k,cy+70*k), fill=lighter(c,.6))

def bolt(d, c, cx=256, cy=256, k=1.0):
    d.polygon([(cx+30*k,cy-160*k),(cx-80*k,cy+20*k),(cx-5*k,cy+20*k),(cx-40*k,cy+160*k),(cx+90*k,cy-30*k),(cx+10*k,cy-30*k)], fill=c)

def heart(d, c, cx=256, cy=256, k=1.0):
    d.ellipse((cx-130*k,cy-110*k,cx+5*k,cy+20*k), fill=c); d.ellipse((cx-5*k,cy-110*k,cx+130*k,cy+20*k), fill=c)
    d.polygon([(cx-125*k,cy-20*k),(cx+125*k,cy-20*k),(cx,cy+140*k)], fill=c)
    d.rectangle((cx-14*k,cy-70*k,cx+14*k,cy+40*k), fill=WHITE); d.rectangle((cx-50*k,cy-34*k,cx+50*k,cy+4*k), fill=WHITE)

def hourglass(d, c, cx, cy, k):
    d.polygon([(cx-50*k,cy-60*k),(cx+50*k,cy-60*k),(cx,cy),(cx+50*k,cy+60*k),(cx-50*k,cy+60*k),(cx,cy)], fill=c)
    d.rectangle((cx-60*k,cy-72*k,cx+60*k,cy-58*k), fill=c); d.rectangle((cx-60*k,cy+58*k,cx+60*k,cy+72*k), fill=c)

def sun(d, c, cx, cy, r):
    for i in range(12):
        a=i*math.pi/6; d.line([(cx+r*1.25*math.cos(a),cy+r*1.25*math.sin(a)),(cx+r*1.75*math.cos(a),cy+r*1.75*math.sin(a))], fill=c, width=int(r*.28))
    d.ellipse((cx-r,cy-r,cx+r,cy+r), fill=c)

def flame(d, c, cx, cy, k):
    d.polygon([(cx,cy-150*k),(cx+80*k,cy-20*k),(cx+70*k,cy+100*k),(cx-70*k,cy+100*k),(cx-85*k,cy-10*k),(cx-35*k,cy-60*k)], fill=c)
    d.polygon([(cx,cy-40*k),(cx+40*k,cy+40*k),(cx+30*k,cy+100*k),(cx-30*k,cy+100*k),(cx-38*k,cy+30*k)], fill=YELLOW)

def eye(d, c, cx, cy, k):
    d.ellipse((cx-130*k,cy-70*k,cx+130*k,cy+70*k), fill=c); d.ellipse((cx-45*k,cy-45*k,cx+45*k,cy+45*k), fill=(20,20,20)); d.ellipse((cx-18*k,cy-30*k,cx+8*k,cy-5*k), fill=WHITE)

def crosshair(d, c, cx, cy, k):
    d.ellipse((cx-100*k,cy-100*k,cx+100*k,cy+100*k), outline=c, width=int(22*k))
    for a,b in (((0,-140),(0,-50)),((0,50),(0,140)),((-140,0),(-50,0)),((50,0),(140,0))):
        d.line([(cx+a[0]*k,cy+a[1]*k),(cx+b[0]*k,cy+b[1]*k)], fill=c, width=int(22*k))

def horn(d, c, cx, cy, k):
    d.polygon([(cx-120*k,cy-20*k),(cx+40*k,cy-70*k),(cx+40*k,cy+70*k),(cx-120*k,cy+20*k)], fill=c)
    for i in range(3):
        r=(80+i*45)*k; d.arc((cx-r+40*k,cy-r,cx+r+40*k,cy+r), -45, 45, fill=c, width=int(20*k))

def cloud(d, c, cx, cy, k):
    for e in ((-110,-20,10,80),(-50,-80,80,50),(20,-40,130,70)): d.ellipse((cx+e[0]*k,cy+e[1]*k,cx+e[2]*k,cy+e[3]*k), fill=c)
    d.rectangle((cx-110*k,cy+20*k,cx+130*k,cy+80*k), fill=c)

def snowflake(d, c, cx, cy, k):
    for i in range(3):
        a=i*math.pi/3
        for s in (1,-1):
            ex,ey=cx+s*150*k*math.cos(a),cy+s*150*k*math.sin(a); d.line([(cx,cy),(ex,ey)], fill=c, width=int(26*k))
            for t in (0.6,):
                mx,my=cx+s*150*k*t*math.cos(a),cy+s*150*k*t*math.sin(a)
                for da in (0.6,-0.6): d.line([(mx,my),(mx+s*55*k*math.cos(a+da),my+s*55*k*math.sin(a+da))], fill=c, width=int(20*k))

def skull_drop(d, c, cx, cy, k):
    drop(d, c, cx, cy-10*k, k*0.95)
    d.ellipse((cx-35*k,cy+15*k,cx-5*k,cy+45*k), fill=(20,30,10)); d.ellipse((cx+5*k,cy+15*k,cx+35*k,cy+45*k), fill=(20,30,10))

def broken_shield(d, c, cx, cy, k):
    shield(d, c, cx, cy, k)
    d.line([(cx-20*k,cy-135*k),(cx+30*k,cy-40*k),(cx-30*k,cy+20*k),(cx+15*k,cy+150*k)], fill=(34,12,14), width=int(26*k))

def cripple(d, c, cx, cy, k):
    boot(d, c, cx, cy, k)
    d.line([(cx-150*k,cy-150*k),(cx+150*k,cy+150*k)], fill=(250,80,60), width=int(34*k))

def stun_stars(d, c, cx, cy, k):
    d.ellipse((cx-150*k,cy-60*k,cx+150*k,cy+60*k), outline=c, width=int(18*k))
    for a in (200, 330, 90):
        r=math.radians(a); star(d, c, cx+150*k*math.cos(r), cy+60*k*math.sin(r), 60*k, 5, 0.45)

def badge(img, fn, ring, cooldown=False):
    b=Image.new('RGBA',(S,S),(0,0,0,0)); d=ImageDraw.Draw(b)
    cx,cy,r=372,372,108
    d.ellipse((cx-r,cy-r,cx+r,cy+r), fill=(14,15,19), outline=(90,92,98) if cooldown else ring, width=16)
    fn(d, cx, cy, 0.48); img.alpha_composite(b)

def render(sym, ring, badge_fn=None, cooldown=False, debuff=False, glow=None):
    img=Image.new('RGBA',(S,S),(0,0,0,0)); d=ImageDraw.Draw(img); plate(d, ring, cooldown, debuff)
    layer=Image.new('RGBA',(S,S),(0,0,0,0)); sym(ImageDraw.Draw(layer))
    if glow:
        g=layer.copy().filter(ImageFilter.GaussianBlur(18)); img.alpha_composite(g)
    if cooldown:
        px=layer.load()
        for y in range(S):
            for x in range(S):
                p=px[x,y]
                if p[3]: l=int((p[0]*.3+p[1]*.59+p[2]*.11)); px[x,y]=(int(l*.5+p[0]*.15),int(l*.5+p[1]*.15),int(l*.5+p[2]*.15),p[3])
    img.alpha_composite(layer)
    if badge_fn: badge(img, badge_fn, ring, cooldown)
    if cooldown: badge(img, lambda d,cx,cy,k: hourglass(d,(230,150,70),cx,cy,k), ring, True)
    return img.resize((64,64), Image.LANCZOS)

I = {}
# ---- Attack damage family: red sword ----
I['damage']      = render(lambda d: sword(d,RED), RED)
I['battlecry']   = render(lambda d: sword(d,RED,230,250,.9), RED, lambda d,cx,cy,k: horn(d,ORANGE,cx-10,cy,k))
I['ray']         = render(lambda d: sword(d,RED,230,250,.9), RED, lambda d,cx,cy,k: sun(d,GOLD,cx,cy,45*k/.48*.48))
I['focus']       = render(lambda d: sword(d,RED,230,250,.9), RED, lambda d,cx,cy,k: crosshair(d,GREEN,cx,cy,k*.8))
I['vigil']       = render(lambda d: sword(d,RED,230,250,.9), RED, lambda d,cx,cy,k: eye(d,GOLD,cx,cy,k*.75))
I['fury']        = render(lambda d: (flame(d,(255,90,30),256,250,1.35), sword(d,(255,235,190),256,270,.8)), (255,90,30), glow=True)
I['fury_cd']     = render(lambda d: (flame(d,(255,90,30),256,250,1.35), sword(d,(255,235,190),256,270,.8)), (255,90,30), cooldown=True)
# ---- Skill damage family: blue sword ----
I['skill_damage']= render(lambda d: sword(d,BLUE), BLUE)
I['enhanced']    = render(lambda d: sword(d,BLUE,230,250,.9), BLUE, lambda d,cx,cy,k: star(d,GOLD,cx,cy,70,4,.35), glow=True)
# ---- Defense family: shield ----
I['defense']     = render(lambda d: shield(d,BLUE), BLUE)
I['sun']         = render(lambda d: shield(d,BLUE,230,240,.9), GOLD, lambda d,cx,cy,k: sun(d,GOLD,cx,cy,40))
I['storm']       = render(lambda d: shield(d,BLUE,230,240,.9), CYAN, lambda d,cx,cy,k: (cloud(d,(200,210,230),cx,cy-10,k*.8), bolt(d,YELLOW,cx+10,cy+40,k*.45)))
I['barrier']     = render(lambda d: (d.ellipse((96,96,416,416),outline=GOLD,width=24), shield(d,GOLD,256,256,.75)), GOLD)
I['purity']      = render(lambda d: shield(d,(250,235,190),256,256,1,True), WHITE)
I['hyper_armor'] = render(lambda d: armor(d,ORANGE), ORANGE)
# ---- Speed family: winged boot ----
I['haste']       = render(lambda d: boot(d,GREEN), GREEN)
I['wind']        = render(lambda d: boot(d,GREEN,240,240,.9), CYAN, lambda d,cx,cy,k: [d.arc((cx-70,cy-50+i*40,cx+70,cy+10+i*40),190,350,fill=CYAN,width=14) for i in range(3)])
I['sigil_haste'] = render(lambda d: boot(d,GREEN,240,240,.9), PINK, lambda d,cx,cy,k: heart(d,PINK,cx,cy+5,k*.75))
I['attack_speed']= render(lambda d: (sword(d,ORANGE,256,256,.9,-30), sword(d,ORANGE,256,256,.9,30)), ORANGE)
# ---- Resources ----
I['eitr']        = render(lambda d: drop(d,VIOLET), VIOLET)
I['stamina']     = render(lambda d: bolt(d,YELLOW), YELLOW)
I['heart']       = render(lambda d: heart(d,PINK), PINK)
I['overcharge']  = render(lambda d: (star(d,VIOLET,256,256,175,8,.45), d.ellipse((216,216,296,296),fill=(250,230,255))), VIOLET, glow=True)
I['portal']      = render(lambda d: [d.ellipse((256-r,256-r*1.25,256+r,256+r*1.25),outline=VIOLET,width=22) for r in (140,95,50)], VIOLET)
# ---- Priest ----
I['relic']       = render(lambda d: (d.rectangle((228,110,284,410),fill=CYAN), d.rectangle((150,180,362,236),fill=CYAN), d.ellipse((226,178,286,238),fill=WHITE)), CYAN)
I['wings']       = render(lambda d: [(d.ellipse((256-40-60*i-90,150+50*i,256-40,190+50*i),fill=WHITE), d.ellipse((256+40,150+50*i,256+40+60*i+90,190+50*i),fill=WHITE)) for i in range(4)] and d.ellipse((236,110,276,150),fill=GOLD), GOLD)
I['shockwave']   = render(lambda d: (rings(d,GOLD), roundbuckler(d,GOLD,256,256,100)), GOLD, glow=True)
I['shockwave_cd']= render(lambda d: (rings(d,GOLD), roundbuckler(d,GOLD,256,256,100)), GOLD, cooldown=True)
I['parry_cd']    = render(lambda d: (roundbuckler(d,BLUE,256,256,120), sword(d,WHITE,256,256,.75,40)), BLUE, cooldown=True)
I['life_token']  = render(lambda d: heart(d,GOLD), GOLD)
I['life_token_cd']=render(lambda d: heart(d,GOLD), GOLD, cooldown=True)
# ---- Mercenary / misc ----
I['labor']       = render(lambda d: (d.line([(150,380),(330,170)],fill=BROWN,width=30), d.polygon([(290,120),(390,170),(360,210),(270,160)],fill=GREY), d.line([(180,170),(360,380)],fill=BROWN,width=30), d.polygon([(130,170),(220,120),(240,160),(160,215)],fill=GREY)), BROWN)
I['clockwork']   = render(lambda d: (star(d,GOLD,256,256,170,12,.8), d.ellipse((150,150,362,362),fill=(30,30,30)), d.line([(256,256),(256,180)],fill=GOLD,width=20), d.line([(256,256),(310,280)],fill=GOLD,width=20)), GOLD)
I['generic']     = render(lambda d: star(d,WHITE), WHITE)
# ---- Enemy debuffs (diamond plate) ----
DR=(200,50,50)
I['debuff_poisoned']=render(lambda d: skull_drop(d,POISON,256,256,1), POISON, debuff=True)
I['debuff_burning'] =render(lambda d: flame(d,ORANGE,256,256,1.1), ORANGE, debuff=True)
I['debuff_frost']   =render(lambda d: snowflake(d,ICE,256,256,1), ICE, debuff=True)
I['debuff_crippled']=render(lambda d: cripple(d,(200,180,150),256,256,.9), (200,180,150), debuff=True)
I['debuff_exposed'] =render(lambda d: broken_shield(d,(240,90,90),256,256,.9), (240,90,90), debuff=True)
I['debuff_zapped']  =render(lambda d: bolt(d,YELLOW,256,256,1.05), YELLOW, debuff=True)
I['debuff_stunned'] =render(lambda d: stun_stars(d,GOLD,256,256,1), GOLD, debuff=True)

out=sys.argv[1]; os.makedirs(out, exist_ok=True)
for k,v in I.items(): v.save(os.path.join(out,'Buff_'+k+'.png'))
if len(sys.argv)>2:
    W=96; cols=8; keys=list(I); rows=(len(keys)+cols-1)//cols
    sh=Image.new('RGBA',(cols*W*2,rows*(W*2+28)),(28,28,32,255)); d=ImageDraw.Draw(sh)
    for i,k in enumerate(keys):
        x=(i%cols)*W*2; y=(i//cols)*(W*2+28); sh.alpha_composite(I[k].resize((W*2-24,W*2-24),Image.NEAREST),(x+12,y+8))
        sh.alpha_composite(I[k],(x+W*2-70,y+W*2-70)); d.text((x+12,y+W*2-10),k,fill='white')
    sh.save(sys.argv[2])
print(len(I))
