from pathlib import Path
ns={};exec(Path('tests200/source_lexer.py').read_text(),ns)
s=Path('working200/DragonsAltar.CombatRuntime.cs').read_text(encoding='utf-8-sig')
reg=ns['region'];prefixes='\n'.join(reg(s,m) for m in ['private static bool BirdDestructiblePrefix(', 'private static bool DamagePrefix('])
receiver=reg(s,'public void ReceiveSkillHit(')
text='''using System;
namespace BirdDispatchHarness {
// Shipped bridge prefixes and forwarding method, with native Character/Destructible damage ports substituted.
class HitData { public float Amount; public HitData(float amount){Amount=amount;} }
struct DamagePatchState { }
class Character {
 public int BodyCalls;
 public void Damage(HitData hit) {
  DamagePatchState state=new DamagePatchState();
  if(DispatchSnapshot.CharacterPrefix(this,new object[]{hit},ref state)) BodyCalls++;
 }
}
class Destructible {
 public DragonBirdTarget Bird; public int Calls; public float Delivered; public HitData LastHit; public bool Throw;
 public T GetComponent<T>()where T:class {return Bird as T;}
 public void Damage(HitData hit){
  if(!DispatchSnapshot.NativePrefix(this,hit))return;
  if(Throw)throw new InvalidOperationException("native damage failure");
  Calls++;Delivered=hit.Amount;LastHit=hit;
 }
}
static class DragonCombat {
 public static int Modifiers;
 public static DamagePatchState BeginDamage(Character c,HitData hit){Modifiers++;hit.Amount*=1.25f;return new DamagePatchState();}
}
class DragonBirdTarget:Character {
 private Destructible _damageable;private bool _forwarding;public bool Dead;
 public bool Forwarding {get{return _forwarding;}}
 public DragonBirdTarget(Destructible d){_damageable=d;d.Bird=this;}
 public bool IsDead(){return Dead;}
'''+receiver+'''
}
class DispatchSnapshot {
'''+prefixes+'''
 public static bool NativePrefix(Destructible d,HitData hit){return BirdDestructiblePrefix(d,hit);}
 public static bool CharacterPrefix(Character c,object[] args,ref DamagePatchState state){return DamagePrefix(c,args,ref state);}
 public static void Run(Action<bool,string> check){
  Destructible normal=new Destructible();DragonCombat.Modifiers=0;normal.Damage(new HitData(80f));
  check(normal.Calls==1&&normal.Delivered==80f&&DragonCombat.Modifiers==0,"Shipped bird-only native prefix leaves ordinary destructibles unchanged");
  Character creature=new Character();creature.Damage(new HitData(80f));
  check(creature.BodyCalls==1&&DragonCombat.Modifiers==1,"Shipped character prefix still permits the normal creature damage body");
  Destructible native=new Destructible();DragonBirdTarget bird=new DragonBirdTarget(native);HitData hit=new HitData(80f);DragonCombat.Modifiers=0;
  native.Damage(hit);
  check(native.Calls==1&&DragonCombat.Modifiers==1&&native.Delivered==100f&&bird.BodyCalls==0,
   "Shipped native-bird path applies creature modifiers once and calls native destructible damage exactly once");
  check(ReferenceEquals(native.LastHit,hit)&&!bird.Forwarding,"Shipped bridge retains the exact HitData and releases its recursion guard");
  hit=new HitData(80f);DragonCombat.Modifiers=0;bird.Damage(hit);
  check(native.Calls==2&&DragonCombat.Modifiers==1&&native.Delivered==100f&&bird.BodyCalls==0,
   "Shipped AA skill-bird path reaches native damage once without falling through to Character RPC damage");
  native.Throw=true;bool threw=false;
  try{bird.Damage(new HitData(80f));}catch(InvalidOperationException){threw=true;}
  check(threw&&!bird.Forwarding,"Shipped forwarding guard resets in finally when native damage fails");
  native.Throw=false;bird.Damage(new HitData(80f));
  check(native.Calls==3&&!bird.Forwarding,"A subsequent bird hit still reaches native damage after an exception");
  bird.Dead=true;bird.ReceiveSkillHit(new HitData(80f));bird.Dead=false;bird.ReceiveSkillHit(null);
  check(native.Calls==3&&!bird.Forwarding,"Dead birds and null direct hits do not forward native damage");
 }
}
}
'''
Path('tests200/BirdDispatchSnapshot.cs').write_text(text)
print('Generated bird dispatch harness from the three shipped prefix/forwarding bodies.')
