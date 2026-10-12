using System;
using System.Collections.Generic;
namespace UnityEngine {
  public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} public static Vector3 operator+(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);} }
  public struct Color { public float r,g,b,a; public Color(float R,float G,float B,float A){r=R;g=G;b=B;a=A;} }
  public static class Mathf { public const float PI=3.14159265f; public static float Min(float a,float b){return a<b?a:b;} public static float Max(float a,float b){return a>b?a:b;} public static float Clamp01(float v){return v<0?0:v>1?1:v;} public static float Cos(float a){return (float)Math.Cos(a);} public static float Sin(float a){return (float)Math.Sin(a);} }
  public static class Time { public static float time; }
  public class Object { public bool Dead; public static int LiveObjects; public Object(){LiveObjects++;}
    public static void Destroy(Object o){ if(o==null||o.Dead)return; o.Dead=true; LiveObjects--; if(o is Material) Material.LiveMaterials--; var go=o as GameObject; if(go!=null){ foreach(var c in go.Comps.ToArray()){ if(!c.Dead){ c.Dead=true; LiveObjects--; var mb=c as MonoBehaviour; if(mb!=null) mb.CallOnDestroy(); } } } else { var mb2=o as MonoBehaviour; if(mb2!=null) mb2.CallOnDestroy(); } } }
  public class Shader { public static Shader Find(string n){ return new Shader(); } }
  public class Material : Object { public static int LiveMaterials; public Material(Shader s){LiveMaterials++;} ~Material(){} public void Release(){ } }
  public class Component : Object { public GameObject gameObject; }
  public class LineRenderer : Component { public bool useWorldSpace,loop; public int positionCount; public float startWidth,endWidth; public Color startColor,endColor; public Material material; public void SetPosition(int i,Vector3 v){} }
  public class MonoBehaviour : Component { public void CallOnDestroy(){ var m=GetType().GetMethod("OnDestroy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public); if(m!=null)m.Invoke(this,null);} public void CallUpdate(){ var m=GetType().GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public); if(m!=null)m.Invoke(this,null);} public void CallAwake(){ var m=GetType().GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public); if(m!=null)m.Invoke(this,null);} }
  public class GameObject : Object { public List<Component> Comps=new List<Component>(); public static List<GameObject> All=new List<GameObject>(); public GameObject(string n){ gameObject=this; All.Add(this);} public GameObject gameObject;
    public T AddComponent<T>() where T:Component,new(){ var c=new T(); c.gameObject=this; Comps.Add(c); var mb=c as MonoBehaviour; if(mb!=null) mb.CallAwake(); return c; } }
}
