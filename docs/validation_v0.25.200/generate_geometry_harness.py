from pathlib import Path
import sys
sys.path.insert(0,str(Path('tests197').resolve()))
# Import only the existing lexer helpers, not its previous-version assertions.
ns={};exec(Path('tests200/source_lexer.py').read_text(),ns)
s=Path('working200/DragonsAltar.CombatRuntime.cs').read_text(encoding='utf-8-sig')
method=ns['region'](s,'public static Mesh ExtrudeBevel(')
method=method.replace('public static Mesh ExtrudeBevel(', 'public static MeshSnapshot ExtrudeBevel(').replace('Mesh m = new Mesh();','MeshSnapshot m = new MeshSnapshot();')
header='''using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DragonsAltarCombat;
// Exact shipped extrusion source; only Mesh allocation/bounds are substituted to run without Unity native code.
class MeshSnapshot {
 public Vector3[] vertices, normals; public Vector2[] uv; public int[] triangles;
 public void RecalculateBounds() { }
}
class ActualGeometrySnapshot {
 static List<Vector2> OffsetOutline(List<Vector2> p,float d) { return DragonVfx.OffsetOutline(p,d); }
 static int[] EarClip(List<Vector2> p) { return (int[])typeof(DragonVfx).GetMethod("EarClip",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p}); }
'''
Path('tests200/GeometrySnapshot.cs').write_text(header+method+'\n}\n')
print('Generated geometry harness directly from the shipped extrusion method.')
