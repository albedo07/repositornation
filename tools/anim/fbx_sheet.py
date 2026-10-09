import json,sys,math
import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
d=json.load(open(sys.argv[1])); P=d["pos"]
segs=[("Hips","Spine2"),("Spine2","Head"),("Spine2","RightArm"),("RightArm","RightForeArm"),("RightForeArm","RightHand"),("Spine2","LeftArm"),("LeftArm","LeftForeArm"),("LeftForeArm","LeftHand"),("Hips","LeftUpLeg"),("LeftUpLeg","LeftLeg"),("LeftLeg","LeftFoot"),("Hips","RightUpLeg"),("RightUpLeg","RightLeg"),("RightLeg","RightFoot")]
idx=[round(i*(len(P)-1)/11) for i in range(12)]
fig,axs=plt.subplots(2,12,figsize=(24,5))
for c,i in enumerate(idx):
    r=P[i]
    for row,(a,b) in enumerate([(0,2),(0,1)]):  # front view x-z, top view x-y
        ax=axs[row][c]
        for s,e in segs:
            if s in r and e in r:
                col='r' if 'Right' in s+e else ('b' if 'Left' in s+e else 'k')
                ax.plot([r[s][a],r[e][a]],[r[s][b],r[e][b]],col,lw=2)
        ax.set_aspect('equal'); ax.set_xticks([]); ax.set_yticks([])
        ax.set_xlim(-1.2,1.2); ax.set_ylim(-0.1 if row==0 else -1.2,2.0 if row==0 else 1.2)
    axs[0][c].set_title(f"f{i+d['frames'][0]}")
axs[0][0].set_ylabel("front (x,z)"); axs[1][0].set_ylabel("top (x,y)")
plt.tight_layout(); plt.savefig(sys.argv[2],dpi=60)
# yaw of hips->right arm over time
yaws=[]
for r in P:
    v=(r["RightArm"][0]-r["LeftArm"][0], r["RightArm"][1]-r["LeftArm"][1]); yaws.append(math.degrees(math.atan2(v[1],v[0])))
tot=0
for i in range(1,len(yaws)):
    dd=(yaws[i]-yaws[i-1]+180)%360-180; tot+=dd
print("shoulder-line total turn deg:",round(tot), "frames", len(P))
