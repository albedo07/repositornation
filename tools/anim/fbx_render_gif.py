import bpy, sys, math
src, out, mirror = sys.argv[1], sys.argv[2], sys.argv[3]=='1'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='axe/X Bot.fbx')
bot=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
bpy.ops.import_scene.fbx(filepath=src)
anim=[o for o in bpy.data.objects if o.type=='ARMATURE' and o!=bot][0]
act=anim.animation_data.action
bot.animation_data_create(); bot.animation_data.action=act
if hasattr(bot.animation_data,'action_slot') and anim.animation_data.action_slot: bot.animation_data.action_slot=anim.animation_data.action_slot
for o in list(bpy.data.objects):
    if o==anim or (o.parent==anim): bpy.data.objects.remove(o)
if mirror: bot.scale.x=-bot.scale.x
sc=bpy.context.scene; a,b=map(int,act.frame_range)
sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=4; sc.cycles.use_denoising=False; sc.render.resolution_x=240; sc.render.resolution_y=300
w=bpy.data.worlds.new('w'); sc.world=w; w.color=(0.25,0.27,0.32)
ld=bpy.data.lights.new('l','SUN'); lo=bpy.data.objects.new('l',ld); sc.collection.objects.link(lo); lo.rotation_euler=(0.8,0.2,0.4); ld.energy=4
cam=bpy.data.cameras.new('c'); co=bpy.data.objects.new('c',cam); sc.collection.objects.link(co); sc.camera=co
co.rotation_euler=(math.radians(84),0,0); cam.lens=55
step=2
hips=bot.pose.bones.get('mixamorig:Hips')
for i,f in enumerate(range(a,b+1,step)):
    sc.frame_set(f); hp=bot.matrix_world@hips.head; co.location=(hp.x,hp.y-4.6,hp.z+0.3)
    sc.render.filepath=f"{out}_{i:03d}.png"; bpy.ops.render.render(write_still=True)
print("rendered",(b-a)//step+1)
