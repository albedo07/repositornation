import json,glob,os,math
import numpy as np
rows=[]
def segd(p,a,b):
    ab=b-a; t=np.clip((p-a)@ab/max(1e-6,ab@ab),0,1); return np.linalg.norm(p-(a+ab*t))
for f in sorted(glob.glob('ana/*.json')):
    d=json.load(open(f)); P=d['pos']; n=len(P)
    g=lambda i,k: np.array(P[i][k])
    def yaw(i):
        v=g(i,'RightArm')-g(i,'LeftArm'); return math.degrees(math.atan2(v[1],v[0]))
    tot=0
    for i in range(1,n): tot+=(yaw(i)-yaw(i-1)+180)%360-180
    sh=g(0,'RightArm')-g(0,'LeftArm'); fwd=np.cross([0,0,1],sh); fwd[2]=0; fwd/=max(1e-6,np.linalg.norm(fwd))
    side=np.array([fwd[1],-fwd[0],0])
    h0=g(0,'Hips'); disp=g(n-1,'Hips')-h0
    fw=disp@fwd; sd=disp@side
    zs=[g(i,'Hips')[2] for i in range(n)]; rise=max(zs)-zs[0]; drop=zs[0]-min(zs)
    best=None
    for hand in ['RightHand','LeftHand']:
        sp=[np.linalg.norm(g(i+1,hand)-g(i-1,hand))*15 for i in range(1,n-1)]
        if not sp: continue
        k=int(np.argmax(sp)); 
        if best is None or sp[k]>best[1]: best=(hand,sp[k],k+1)
    clip=0
    for i in range(n):
        H,S,Hd=g(i,'Hips'),g(i,'Spine2'),g(i,'Head')
        bad=False
        for hand,fore in [('RightHand','RightForeArm'),('LeftHand','LeftForeArm')]:
            for t in np.linspace(0,1,5):
                p=g(i,fore)*(1-t)+g(i,hand)*t
                if min(segd(p,H,S),segd(p,S,Hd))<0.09: bad=True
        clip+=bad
    rows.append((os.path.basename(f)[:-5],n,n/30,tot,fw,sd,rise,drop,best[0][0] if best else '-',best[2] if best else 0,best[1] if best else 0,clip))
print("| clip | frames | sec | turn deg (+CCW) | travel fwd/side m | jump up / crouch m | fastest hand @frame (m/s) | hands near torso frames |")
print("|---|---|---|---|---|---|---|---|")
for r in rows:
    print(f"| {r[0]} | {r[1]} | {r[2]:.1f} | {r[3]:+.0f} | {r[4]:+.2f} / {r[5]:+.2f} | {r[6]:.2f} / {r[7]:.2f} | {r[8]} @{r[9]} ({r[10]:.1f}) | {r[11]} |")
