Mixamo "Pro Melee Axe Pack" (user upload 2026-10-09): 47 clips + X Bot, 30 fps, one-handed axe in the RIGHT hand.
Analysed headless with Blender 5.2 (pip bpy) - tools/anim/fbx_extract.py -> fbx_metrics.py / fbx_sheet.py.
Turn sign: + = counter-clockwise seen from above. Hit = peak hand speed. Sheets = stick figure front + top view, 12 moments.
See ANALYSIS_TABLE.md. Attack hit frames (peak hand / foot speed):
  360 high: R hand f32 (1.07 s), 1 full turn CLOCKWISE (-361), 3.2 s
  360 low: R hand f27 (0.9 s), -360 clockwise, 2.5 s
  downward (overhead chop): R f25 (0.83 s), 2.3 s
  horizontal: R f29 (0.97 s), 2.4 s; backhand: R f30 (1.0 s), 3.2 s
  combo v1: R f59/f62/f89 (+L f64), travels 1.35 m; combo v2: R f27/f51/f78; combo v3: R f30/f52
  run jump attack: both hands f50 (1.67 s), hips peak f38 (+1.17 m), travels ~3.7 m (root motion - strip)
  kick v1: R foot f17; kick v2: R foot f17 + f27
  battlecry: L hand f14; standing jump: hips lowest f10 (crouch) -> highest f23 (+0.63 m)
