using System;
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
public static MeshSnapshot ExtrudeBevel(List<Vector2> outline, float depth, float bevel, float uvScale)
        {
            int n = outline == null ? 0 : outline.Count;
            if (n < 3) return null;
            float hd = depth * 0.5f;
            bevel = Mathf.Clamp(bevel, 0f, hd * 0.9f);
            List<Vector2> inner = OffsetOutline(outline, -bevel);
            List<Vector3> v = new List<Vector3>();
            List<Vector3> nr = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            int[] tri = EarClip(inner);
            // front (+z faces the viewer side +Z) and back
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? hd : -hd;
                Vector3 nz = side == 0 ? Vector3.forward : Vector3.back;
                int b0 = v.Count;
                for (int i = 0; i < n; i++) { v.Add(new Vector3(inner[i].x, inner[i].y, z)); nr.Add(nz); uv.Add(inner[i] * uvScale); }
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    // CCW cap triangles point toward +Z; reverse only the back cap.
                    if (side == 0) { t.Add(b0 + tri[i]); t.Add(b0 + tri[i + 1]); t.Add(b0 + tri[i + 2]); }
                    else { t.Add(b0 + tri[i]); t.Add(b0 + tri[i + 2]); t.Add(b0 + tri[i + 1]); }
                }
            }
            // walls + bevels: quads around the perimeter
            float per = 0f;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector2 a = outline[i], b = outline[j], ai = inner[i], bi = inner[j];
                Vector2 e = b - a;
                float len = e.magnitude;
                Vector3 wn = new Vector3(e.y, -e.x, 0f).normalized;   // outward for a CCW outline
                // rows: front inner (z=hd), front outer (z=hd-bevel), back outer (z=-hd+bevel), back inner (z=-hd)
                Vector3[] ra = { new Vector3(ai.x, ai.y, hd), new Vector3(a.x, a.y, hd - bevel), new Vector3(a.x, a.y, -hd + bevel), new Vector3(ai.x, ai.y, -hd) };
                Vector3[] rb = { new Vector3(bi.x, bi.y, hd), new Vector3(b.x, b.y, hd - bevel), new Vector3(b.x, b.y, -hd + bevel), new Vector3(bi.x, bi.y, -hd) };
                Vector3[] nn = { (wn + Vector3.forward).normalized, wn, (wn + Vector3.back).normalized };
                float[] zz = { hd, hd - bevel, -hd + bevel, -hd };
                for (int q = 0; q < 3; q++)
                {
                    int b0 = v.Count;
                    v.Add(ra[q]); v.Add(rb[q]); v.Add(ra[q + 1]); v.Add(rb[q + 1]);
                    for (int k = 0; k < 4; k++) nr.Add(nn[q]);
                    uv.Add(new Vector2(per, zz[q]) * uvScale); uv.Add(new Vector2(per + len, zz[q]) * uvScale);
                    uv.Add(new Vector2(per, zz[q + 1]) * uvScale); uv.Add(new Vector2(per + len, zz[q + 1]) * uvScale);
                    t.Add(b0); t.Add(b0 + 2); t.Add(b0 + 1); t.Add(b0 + 1); t.Add(b0 + 2); t.Add(b0 + 3);
                }
                per += len;
            }
            MeshSnapshot m = new MeshSnapshot();
            m.vertices = v.ToArray(); m.normals = nr.ToArray(); m.uv = uv.ToArray(); m.triangles = t.ToArray();
            m.RecalculateBounds();
            return m;
        }
}
