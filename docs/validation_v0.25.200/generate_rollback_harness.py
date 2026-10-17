from pathlib import Path
ns={};exec(Path('tests200/source_lexer.py').read_text(),ns)
s=Path('working200/DragonsAltar.CombatRuntime.cs').read_text(encoding='utf-8-sig')
methods='\n'.join(ns['region'](s,marker) for marker in ['private class ComboSnap','private static ComboSnap TakeSnap(', 'private static void RollBack('])
header='''using System;
using System.Collections.Generic;
namespace RollbackHarness {
// Exact shipped snapshot/capture/rollback bodies. Only Player and combat native services are replaced.
class Player { public float Stamina, Eitr; public float GetStamina(){return Stamina;} }
static class DragonCombat {
 public static int AttackSerial; public static object NaccAttack;
 public static float Speed=1f, Remaining;
 public static float ChainSpeedFor(Player p){return Speed;}
 public static float ChainSpeedRemaining(Player p){return Remaining;}
 public static void SetChainSpeed(Player p,float speed,float ttl){Speed=speed;Remaining=ttl;}
 public static void RestoreAttackActivation(int serial,object attack){AttackSerial=serial;NaccAttack=attack;}
}
class RollbackSnapshot {
 static int _comboHit,_comboSwing,_comboPrevSwing,_comboCount,_comboGen;
 static float _comboLastEnd,_comboLastStart,_comboClockStart,_chainPrevSpeed,_comboLockUntil,_comboLockBefore,_comboSwingSeenAt;
 static string _comboStartAnim,_comboBase,_comboCarrier;
 static bool _comboDual,_comboSwingSeen; static object _comboFinisher;
 static readonly Dictionary<string,float> _chainNatural=new Dictionary<string,float>();
 static float CallFloat(Player p,string m){return p.Eitr;}
 static void CallAdd(Player p,string m,float amount){if(m=="AddEitr")p.Eitr+=amount;else p.Stamina+=amount;}
'''
footer='''
 public static void Run(Action<bool,string> check) {
  Player p=new Player();p.Stamina=10f;p.Eitr=20f;
  object attack=new object(),finisher=new object();
  _comboHit=2;_comboSwing=1;_comboPrevSwing=0;_comboCount=3;_comboGen=5;
  _comboLastEnd=4f;_comboLastStart=3f;_comboClockStart=2f;_chainPrevSpeed=1.2f;
  _comboLockUntil=9f;_comboLockBefore=8f;_comboStartAnim="start";_comboBase="base";_comboCarrier="carrier";
  _comboDual=true;_comboFinisher=finisher;_comboSwingSeen=true;_comboSwingSeenAt=3.5f;
  DragonCombat.Speed=0.6f;DragonCombat.Remaining=1.7f;DragonCombat.AttackSerial=12;DragonCombat.NaccAttack=attack;
  ComboSnap c=TakeSnap(p);c.MeasureKey="old";c.HadMeasurement=true;c.MeasuredBefore=0.8f;
  _comboHit=4;_comboSwing=2;_comboPrevSwing=1;_comboCount=2;_comboGen=99;
  _comboLastEnd=-1f;_comboLastStart=15f;_comboClockStart=20f;_chainPrevSpeed=0.2f;
  _comboLockUntil=99f;_comboLockBefore=88f;_comboStartAnim="bad";_comboBase="bad";_comboCarrier="bad";
  _comboDual=false;_comboFinisher=null;_comboSwingSeen=false;_comboSwingSeenAt=33f;
  _chainNatural["old"]=3f;DragonCombat.Speed=0.25f;DragonCombat.Remaining=3f;
  DragonCombat.AttackSerial=13;DragonCombat.NaccAttack=new object();p.Stamina=7f;p.Eitr=16f;
  RollBack(p,c,false);
  check(_comboHit==2&&_comboSwing==1&&_comboPrevSwing==0&&_comboCount==3&&_comboGen==5,
   "Shipped rollback source restores all pre-attempt combo indices and generation after failure");
  check(_comboLastEnd==4f&&_comboLastStart==3f&&_comboClockStart==2f&&_chainPrevSpeed==1.2f&&_comboSwingSeen&&_comboSwingSeenAt==3.5f,
   "Shipped rollback source restores end/start/clock and observed-swing state instead of leaving a ghost running attack");
  check(_comboLockUntil==9f&&_comboLockBefore==8f&&ReferenceEquals(_comboFinisher,finisher)&&_comboDual&&_comboBase=="base"&&_comboCarrier=="carrier"&&_comboStartAnim=="start",
   "Shipped rollback source preserves the preceding swing's lock, finisher and paired carrier ownership");
  check(DragonCombat.Speed==0.6f&&DragonCombat.Remaining==1.7f&&DragonCombat.AttackSerial==12&&ReferenceEquals(DragonCombat.NaccAttack,attack),
   "Shipped rollback source restores exact prior speed lifetime and activation identity");
  check(p.Stamina==7f&&p.Eitr==16f,"Native rejected-start rollback does not refund resource costs taken by the game or other mods");
  check(_chainNatural["old"]==0.8f,"Rejected attempt restores its previous learned swing duration");
  c.MeasureKey="new";c.HadMeasurement=false;_chainNatural["new"]=0.25f;
  RollBack(p,c,true);
  check(!_chainNatural.ContainsKey("new"),"Rejected attempt removes a newly learned tentative cadence entry");
  check(p.Stamina==10f&&p.Eitr==20f,"Existing late-controller rejection still refunds only the actual resource difference");
 }
}
}
'''
Path('tests200/RollbackSnapshot.cs').write_text(header+methods+footer)
print('Generated rollback harness directly from the three shipped snapshot/capture/restore bodies.')
