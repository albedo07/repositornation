using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using DragonsAltarCombat;
class T {
  static int fails, mb0;
  static int Mats(){ return Material.LiveMaterials-mb0; }
  static void Tick(float dt){ Time.time+=dt; foreach(var go in GameObject.All.ToArray()){ if(go.Dead)continue; foreach(var c in go.Comps.ToArray()){ var mb=c as MonoBehaviour; if(mb!=null&&!mb.Dead) mb.CallUpdate(); } } GameObject.All.RemoveAll(g=>g.Dead); }
  static int LiveGO(){ return GameObject.All.Count(g=>!g.Dead); }
  static void Check(string name,bool ok,string detail){ Console.WriteLine((ok?"PASS ":"FAIL ")+name+" - "+detail); if(!ok)fails++; }
  static IEnumerator OldStyleGuide(float dur){ // v0.25.152 pattern: cleanup only in finally
    var go=new GameObject("old"); var m=new Material(null); try{ yield return null; } finally{ UnityEngine.Object.Destroy(go); UnityEngine.Object.Destroy(m);} }
  static void Main(){
    // 0. why: a coroutine that is stopped (iterator dropped, never Disposed - what Unity does) skips finally
    int goBefore=LiveGO(), matBefore=Material.LiveMaterials;
    var it=OldStyleGuide(0.2f); it.MoveNext(); it=null;   // StopCoroutine / host destroyed
    Check("0 old finally-pattern leaks on stopped coroutine", LiveGO()==goBefore+1, "live GO +"+(LiveGO()-goBefore)+" (expected leak = confirms the review)");
    foreach(var g in GameObject.All.ToArray()) UnityEngine.Object.Destroy(g); GameObject.All.Clear();
    Console.WriteLine("     (leaked by the OLD pattern: materials="+Mats()+"; baseline for the rest)"); mb0=Material.LiveMaterials;
    // 1. expiry
    Time.time=0; DragonGuideLife.Spawn(new Vector3(0,0,0),5f,new Color(1,.5f,0,.7f),0.5f);
    Check("1a guide alive before expiry", LiveGO()==1 && DragonGuideLife.Alive==1, "GO="+LiveGO()+" Alive="+DragonGuideLife.Alive+" materials="+Mats());
    for(int i=0;i<40;i++) Tick(0.02f);
    Check("1b expired: GO + material freed", LiveGO()==0 && DragonGuideLife.Alive==0 && Mats()==0, "GO="+LiveGO()+" Alive="+DragonGuideLife.Alive+" materials="+Mats());
    // 2. repeated charging 60 s, guide every 0.2 s, 0.22 s each, no coroutine involved
    int peak=0; for(int i=0;i<3000;i++){ if(i%10==0) DragonGuideLife.Spawn(new Vector3(0,0,0),5f,new Color(1,.4f,.1f,.55f),0.22f); Tick(0.02f); peak=Math.Max(peak,LiveGO()); }
    for(int i=0;i<30;i++) Tick(0.02f);
    Check("2 repeated charging 60 s (300 guides)", LiveGO()==0 && DragonGuideLife.Alive==0 && Mats()==0, "peak live="+peak+" final GO="+LiveGO()+" Alive="+DragonGuideLife.Alive+" materials="+Mats());
    // 3. spawner (plugin/coroutine host) disappears mid-life: nothing references it
    DragonGuideLife.Spawn(new Vector3(0,0,0),5f,new Color(1,1,1,1),1f); var spawner=OldStyleGuide(1f); spawner=null;
    for(int i=0;i<10;i++) Tick(0.02f); int mid=LiveGO();
    for(int i=0;i<60;i++) Tick(0.02f);
    Check("3 host gone mid-life: guide still expires", mid>=1 && DragonGuideLife.Alive==0 && Mats()==0, "mid="+mid+" end Alive="+DragonGuideLife.Alive+" materials="+Mats());
    foreach(var g in GameObject.All.ToArray()) UnityEngine.Object.Destroy(g); GameObject.All.Clear();
    // 4. external destroy before expiry (scene unload / cleanup)
    DragonGuideLife.Spawn(new Vector3(0,0,0),5f,new Color(1,1,1,1),30f); DragonGuideLife.Spawn(new Vector3(0,0,0),5f,new Color(1,1,1,1),30f);
    int a=DragonGuideLife.Alive; foreach(var g in GameObject.All.ToArray()) UnityEngine.Object.Destroy(g); GameObject.All.Clear();
    Check("4 destroyed early: OnDestroy runs, count back to 0", a==2 && DragonGuideLife.Alive==0 && Mats()==0, "before="+a+" after="+DragonGuideLife.Alive+" materials="+Mats());
    // 5. flood: 200 in one frame
    for(int i=0;i<200;i++) DragonGuideLife.Spawn(new Vector3(0,0,0),5f,new Color(1,1,1,1),0.3f);
    int cap=DragonGuideLife.Alive; for(int i=0;i<40;i++) Tick(0.02f);
    Check("5 flood capped at 40, all freed", cap==40 && DragonGuideLife.Alive==0 && LiveGO()==0 && Mats()==0, "cap="+cap+" end Alive="+DragonGuideLife.Alive+" GO="+LiveGO()+" materials="+Mats());
    // 6. sliders: model of LimitSpawned (particle = 0.7 * Density * budget; light = min(0.5*I,1.2) * LightScale)
    Console.WriteLine("\nSLIDER MODEL (mirrors LimitSpawned arithmetic; vanilla light 3.0, 100 particles/burst, budget 1 -> 0.25):");
    Console.WriteLine("density  light | burst(b=1) burst(b=.25) | light intensity");
    foreach(float d in new float[]{0.1f,0.35f,1f,3f}) foreach(float l in new float[]{0f,0.15f,1f,2f}){
      float k1=0.7f*d*1f, k2=0.7f*d*0.25f; float li = l<=0.05f?0f:Math.Min(3f*0.5f,1.2f)*l;
      Console.WriteLine(string.Format("{0,6:0.00}  {1,5:0.00} | {2,9:0.0} {3,12:0.0} | {4,8:0.00}{5}",d,l,100*k1,100*k2,li,l<=0.05f?"  (disabled)":""));
    }
    Console.WriteLine(fails==0?"\nALL PASS":"\nFAILURES: "+fails); Environment.Exit(fails);
  }
}
