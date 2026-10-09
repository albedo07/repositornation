import bpy, sys, json, glob, os
files=sorted(glob.glob('axe/*.fbx'))
names=["Hips","Spine2","Head","RightArm","RightForeArm","RightHand","LeftArm","LeftForeArm","LeftHand","LeftUpLeg","LeftLeg","LeftFoot","RightUpLeg","RightLeg","RightFoot"]
for f in files:
    if 'X Bot' in f: continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try: bpy.ops.import_scene.fbx(filepath=f)
    except Exception as e: print("FAIL",f,e); continue
    arms=[o for o in bpy.data.objects if o.type=='ARMATURE']
    if not arms: continue
    arm=arms[0]; act=arm.animation_data.action if arm.animation_data else None
    if not act: continue
    a,b=map(int,act.frame_range); sc=bpy.context.scene; P=[]
    for i in range(a,b+1):
        sc.frame_set(i); row={}
        for n in names:
            bn=arm.pose.bones.get("mixamorig:"+n)
            if bn: p=arm.matrix_world@bn.head; row[n]=[p.x,p.y,p.z]
        P.append(row)
    json.dump({"frames":[a,b],"pos":P},open('ana/'+os.path.basename(f)[:-4]+'.json','w'))
print("done")
