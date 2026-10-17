using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using DragonsAltarCombat;
using AlbedosCustomClassesAdvanced;
using DragonsAltarRanger;
using System.Runtime.Serialization;
class UnrelatedQueue { public void ClearActionQueue() { } }
class ScopeTests
{
    static int passed;
    static readonly Dictionary<short, OpCode> opcodes = BuildOpcodes();
    static Dictionary<short, OpCode> BuildOpcodes()
    {
        Dictionary<short, OpCode> result = new Dictionary<short, OpCode>();
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            if (field.FieldType == typeof(OpCode)) { OpCode op = (OpCode)field.GetValue(null); result[op.Value] = op; }
        return result;
    }
    static void Check(bool condition, string name)
    { if (!condition) throw new Exception(name); System.Console.WriteLine("PASS: " + name); passed++; }
    static MethodInfo Method(Type type, string name)
    { return AccessTools.Method(type, name); }
    // Read original managed IL. In particular, retain the DECLARING TYPE of virtual method operands.
    static List<CodeInstruction> ReadIL(MethodInfo method)
    {
        List<CodeInstruction> result = new List<CodeInstruction>();
        byte[] bytes = method.GetMethodBody().GetILAsByteArray(); int p = 0;
        while (p < bytes.Length)
        {
            short value = bytes[p++]; if (value == 0xfe) value = unchecked((short)(0xfe00 | bytes[p++]));
            OpCode op = opcodes[value]; object operand = null; int size = 0;
            switch (op.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(bytes, p); break;
                case OperandType.InlineField: operand = method.Module.ResolveField(BitConverter.ToInt32(bytes, p)); size = 4; break;
                case OperandType.InlineMethod: operand = method.Module.ResolveMethod(BitConverter.ToInt32(bytes, p)); size = 4; break;
                default: size = 4; break;
            }
            if (operand == null && size > 0) operand = BitConverter.ToString(bytes, p, size);
            p += size; result.Add(new CodeInstruction(op, operand));
        }
        return result;
    }
    static int sideEffects, cleanups;
    static System.Collections.IEnumerator PendingSkill()
    {
        try { yield return null; sideEffects++; yield return null; sideEffects++; }
        finally { cleanups++; }
    }
    static System.Collections.IEnumerator FailingSkill()
    {
        try { yield return null; throw new InvalidOperationException("expected"); }
        finally { cleanups++; }
    }
    static void Main()
    {
        int released = 0;
        DragonCombat.SkillExecution work = new DragonCombat.SkillExecution(PendingSkill(), delegate { released++; });
        Check(work.MoveNext(), "Real cancellation wrapper starts a skill and reaches its pending yield");
        work.Dispose();
        Check(cleanups == 1 && released == 1, "Dodge disposal immediately runs finally cleanup and releases ownership");
        Check(!work.MoveNext() && sideEffects == 0, "Cancelled work never resumes its delayed attack");
        work.Dispose(); Check(cleanups == 1 && released == 1, "Repeated cancellation is idempotent");
        work = new DragonCombat.SkillExecution(PendingSkill(), delegate { released++; });
        while (work.MoveNext()) { }
        Check(sideEffects == 2 && cleanups == 2 && released == 2, "Uncancelled work preserves both skill steps and normal cleanup");
        work = new DragonCombat.SkillExecution(FailingSkill(), delegate { released++; });
        work.MoveNext(); bool threw = false;
        try { work.MoveNext(); } catch (InvalidOperationException) { threw = true; }
        Check(threw && cleanups == 3 && released == 3 && !work.MoveNext(), "Faulted work cleans up once and cannot run again");
        work = new DragonCombat.SkillExecution(PendingSkill(), delegate { released++; });
        work.Dispose(); Check(!work.MoveNext() && sideEffects == 2, "Cancellation before the first step prevents the entire attack");

        MethodInfo update = Method(typeof(Player), "UpdateDodge");
        List<CodeInstruction> actual = ReadIL(update), original = new List<CodeInstruction>();
        foreach (CodeInstruction i in actual) original.Add(new CodeInstruction(i.opcode, i.operand));
        MethodInfo transpiler = typeof(DragonCombatPlugin).GetMethod("SwordMasterDodgeTranspiler", BindingFlags.Static | BindingFlags.NonPublic);
        List<CodeInstruction> result = new List<CodeInstruction>((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { actual }));
        int changed = 0; bool attack = false, stagger = false, preserved = result.Count == original.Count;
        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == original[i].opcode && Equals(result[i].operand, original[i].operand)) continue;
            changed++;
            MethodInfo before = original[i].operand as MethodInfo, after = result[i].operand as MethodInfo;
            bool isAttack = before != null && after != null && before.Name == "InAttack" && after.Name == "SwordMasterDodgeAttackGate";
            bool isStagger = before != null && after != null && before.Name == "IsStaggering" && after.Name == "SwordMasterDodgeStaggerGate";
            attack |= isAttack; stagger |= isStagger; preserved &= isAttack || isStagger;
        }
        Check(changed == 2 && attack && stagger, "Actual Valheim dodge IL rewrites exactly the attack and stagger gates");
        Check(preserved, "All native ground, stamina, encumbrance, existing-roll and cost instructions stay intact");
        List<CodeInstruction> controls = ReadIL(typeof(DragonCombatPlugin).GetMethod("SetControlsPrefix", BindingFlags.Static | BindingFlags.NonPublic));
        int prepare = controls.FindIndex(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return m != null && m.Name == "PrepareSwordMasterDodge"; });
        int locked = controls.FindIndex(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return m != null && m.Name == "IsSkillLocked"; });
        Check(prepare >= 0 && locked > prepare, "Dodge cancellation executes before the skill-lock input filter");
        result = new List<CodeInstruction>((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { new List<CodeInstruction> { new CodeInstruction(OpCodes.Ret) } }));
        Check(result.Count == 1 && result[0].opcode == OpCodes.Ret, "Unrelated methods without dodge gates are unchanged");
        MethodInfo speed = typeof(AdvancedPlugin).GetMethod("CsContactSpeed", BindingFlags.Static | BindingFlags.NonPublic);
        float cold = (float)speed.Invoke(null, new object[] { 1.6f, 0.05f, 0.04f, 1f });
        float warm = (float)speed.Invoke(null, new object[] { 1.6f, 0.1f, 0.08f, 1f });
        Check(Math.Abs(0.04f + (1.6f - 0.05f) / cold - 1f) < 0.0001f && Math.Abs(0.08f + (1.6f - 0.1f) / warm - 1f) < 0.0001f,
            "Shipped Halfmoon contact calibration reaches a requested one-second contact on cold and warm timing");
        MethodInfo dot = typeof(AdvancedPlugin).GetMethod("IhDotText", BindingFlags.Static | BindingFlags.NonPublic);
        string[][] burns = {
            new string[] { "Spirit Burn, 6s", "Spirit 6s" },
            new string[] { "Fire Burn + Spirit Burn 4/s each, 6s", "Fire 6s (4 damage/s), Spirit 6s (4 damage/s)" },
            new string[] { "Spirit Burn, a stack per hit (4% per stack), 6s, refreshed", "Spirit 6s (4% per stack); refreshed on hit" },
            new string[] { "Fire Burn, 12s", "Fire 12s" },
            new string[] { "Fire 6s, Poison 12s", "Fire 6s, Poison 12s" },
            new string[] { "Fire Burn 10/s, Spirit Burn 12/s, 6s", "Fire 6s (10 damage/s), Spirit 6s (12 damage/s)" },
            new string[] { "Fire Burn 6% Fire every 0.5s, 6s", "Fire 6s (6% Fire every 0.5s)" }
        };
        for (int i = 0; i < burns.Length; i++)
        {
            string formatted = (string)dot.Invoke(null, new object[] { burns[i][0] });
            Check(formatted == burns[i][1], "Actual tooltip burn formatter: " + burns[i][0] + " -> " + formatted);
        }
        string[] buffs = { "ray_of_hope", "heavens_light", "knights_guidance", "grand_sigil", "tailwind", "hawks_vigil", "pinning_shot", "afterimage_arsenal" };
        for (int i = 0; i < buffs.Length; i++)
        {
            string header = AdvancedPlugin.IhPlainDescription(buffs[i]);
            Check(!header.Contains("{") && !header.Contains("%") && !System.Text.RegularExpressions.Regex.IsMatch(header, @"\(.*\d"),
                "Actual " + buffs[i] + " header contains purpose without buff-number parentheses");
        }
        Type ranger = typeof(RangerPlugin);
        List<CodeInstruction> reload = ReadIL(ranger.GetMethod("IsCurrentCrossbowReload", BindingFlags.Static | BindingFlags.NonPublic));
        Check(reload.Exists(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return i.opcode == OpCodes.Ceq || (m != null && m.Name == "ReferenceEquals"); }),
            "Compiled sprint-reload filter requires the same physical weapon reference");
        Check(AccessTools.Field(typeof(Player), "m_weaponLoaded").FieldType == typeof(ItemDrop.ItemData) &&
            AccessTools.Field(typeof(Attack), "m_weapon").FieldType == typeof(ItemDrop.ItemData), "Crossbow event state uses actual native loaded and attack weapon fields");
        List<CodeInstruction> reset = ReadIL(ranger.GetMethod("CrossbowResetPrefix", BindingFlags.Static | BindingFlags.NonPublic));
        List<CodeInstruction> fired = ReadIL(ranger.GetMethod("CrossbowFirePostfix", BindingFlags.Static | BindingFlags.NonPublic));
        Check(!reset.Exists(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return m != null && m.Name == "Remove"; }) &&
            fired.Exists(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return m != null && m.Name == "Remove"; }),
            "Compiled crossbow hooks consume saved state only after a completed shot, never from a reset/holster alone");
        List<CodeInstruction> restore = ReadIL(ranger.GetMethod("RestoreLoadedCrossbow", BindingFlags.Instance | BindingFlags.NonPublic));
        Check(!restore.Exists(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return m != null && m.Name == "SetValue"; }),
            "Compiled restore uses the native setter delegate rather than unsynchronized field writes");
        List<CodeInstruction> interrupt = ReadIL(typeof(AdvancedPlugin).GetMethod("UpdateSwordMasterInterrupt", BindingFlags.Instance | BindingFlags.NonPublic));
        Check(!interrupt.Exists(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return m != null && m.Name == "IsBlocking"; }) &&
            interrupt.Exists(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return m != null && m.Name == "InDodge"; }),
            "Compiled Sword Master interruption keeps dodge and removes blocking");
        MethodInfo nativeSetLoaded = AccessTools.Method(typeof(Player), "SetWeaponLoaded", new Type[] { typeof(ItemDrop.ItemData) });
        Check(nativeSetLoaded != null && AccessTools.Method(typeof(Humanoid), "UnequipItem", new Type[] { typeof(ItemDrop.ItemData), typeof(bool) }) != null &&
            AccessTools.Method(typeof(Humanoid), "EquipItem", new Type[] { typeof(ItemDrop.ItemData), typeof(bool) }).ReturnType == typeof(bool),
            "All native crossbow event patch signatures exist in supplied Valheim assemblies");
        UnityEngine.Vector3[] shell; UnityEngine.Vector2[] uv; int[] triangles;
        DragonVfx.CloudShellGeometry(out shell, out uv, out triangles);
        bool onSkin = true, curved = true, inRange = true;
        for (int i = 0; i < shell.Length; i++) onSkin &= Math.Abs(shell[i].magnitude - 1f) < 0.0001f;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            inRange &= triangles[i] >= 0 && triangles[i] < shell.Length && triangles[i+1] >= 0 && triangles[i+1] < shell.Length && triangles[i+2] >= 0 && triangles[i+2] < shell.Length;
            UnityEngine.Vector3 center = (shell[triangles[i]] + shell[triangles[i+1]] + shell[triangles[i+2]]) / 3f;
            curved &= center.magnitude > 0.985f;
        }
        Check(onSkin && curved, "Actual cloud geometry is curved skin with an empty interior, including between vertices");
        Check(shell.Length == 1176 && triangles.Length == 10368 && inRange, "Cloud shell is a bounded cached 1176-vertex/3456-triangle mesh with valid indices");
        Check(uv.Length == shell.Length && DragonVfx.CloudShellTexel(0f, 0f).a == 0f && DragonVfx.CloudShellTexel(0.5f, 0.5f).a > 0f,
            "Cloud patches have transparent edges and billowing opacity, not a uniform barrier surface");
        bool gold = true, blue = true, green = true; float low = 10f, high = -10f;
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            float u = (x+0.5f)/32f, v = (y+0.5f)/32f;
            UnityEngine.Color g = DragonVfx.RelicAlbedoTexel(0, u, v, UnityEngine.Color.white);
            UnityEngine.Color b = DragonVfx.RelicAlbedoTexel(2, u, v, new UnityEngine.Color(0.15f,0.48f,0.85f,1f));
            UnityEngine.Color h = DragonVfx.RelicAlbedoTexel(2, u, v, new UnityEngine.Color(0.45f,1f,0.32f,1f));
            gold &= g.r > g.g * 1.2f && g.g > g.b * 1.8f;
            blue &= b.b > b.r * 3f && b.b > b.g * 1.5f;
            green &= h.g > h.r * 1.8f && h.g > h.b * 2f;
            low = Math.Min(low,g.r); high = Math.Max(high,g.r);
        }
        Check(gold && high-low > 0.05f, "Actual gold texture contains warm RGB pigment and measurable grain instead of white/flat tint");
        Check(blue && green, "Actual gem textures retain strong blue and healing-green pigment without Material.color");
        UnityEngine.Vector3 head = DragonVfx.HammerHeadPoint(new UnityEngine.Vector3(0.5f,0.50f,0.43f));
        UnityEngine.Vector3 grip = new UnityEngine.Vector3(0.05f,-0.25f,0f);
        Check(head.x > 0.55f && head.y > 0.56f && head.z > 0.55f && DragonVfx.HammerHeadPoint(grip) == grip,
            "Actual hammer transformation thickens the head and leaves its grip unchanged");
        MethodInfo slash = null, explicitSlash = null;
        foreach (MethodInfo candidate in typeof(DragonVfx).GetMethods(BindingFlags.Static | BindingFlags.Public))
            if (candidate.Name == "SlashArc") { if(candidate.GetParameters().Length==8)slash=candidate;else if(candidate.GetParameters().Length==9)explicitSlash=candidate; }
        Check(slash != null && explicitSlash != null && explicitSlash.GetParameters()[8].ParameterType == typeof(bool),
            "Existing eight-argument SlashArc API is preserved beside the explicit-plane overload");
        List<CodeInstruction> existingArc = ReadIL(slash);
        Check(existingArc.Exists(delegate(CodeInstruction i) { return i.opcode == OpCodes.Ldc_I4_1; }),
            "Existing SlashArc callers retain automatic swing-plane tracking");
        int fixedPlanes = 0;
        System.Action<Type> findFixedPlanes = null;
        findFixedPlanes = delegate(Type type)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
            {
                if(method.GetMethodBody()==null)continue;
                List<CodeInstruction> code = ReadIL(method);
                for(int i=1;i<code.Count;i++)
                {
                    MethodInfo called=code[i].operand as MethodInfo;
                    if(called!=null && called.DeclaringType==typeof(DragonVfx) && called.Name=="SlashArc" && called.GetParameters().Length==9 && code[i-1].opcode==OpCodes.Ldc_I4_0)fixedPlanes++;
                }
            }
            foreach(Type nested in type.GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic))findFixedPlanes(nested);
        };
        findFixedPlanes(typeof(AdvancedPlugin));
        Check(fixedPlanes == 1,"Compiled Heavy Swing effect explicitly bypasses stale sampled planes; no other skill forces this override");
        Check(DragonVfx.HeavensSigilAlpha(0.5f,0.5f) > 0.9f && DragonVfx.HeavensSigilAlpha(0f,0f) == 0f && DragonVfx.HeavensSigilAlpha(0.99f,0.5f) > 0.9f,
            "Actual sigil has a central cross, a readable outer boundary and transparent exterior");
        byte[] pixels = new byte[256*256*4]; int visible = 0;
        for (int y=0;y<256;y++) for(int x=0;x<256;x++)
        {
            float alpha = DragonVfx.HeavensSigilAlpha((x+0.5f)/256f,(y+0.5f)/256f);
            int pixel=(y*256+x)*4; pixels[pixel]=224;pixels[pixel+1]=199;pixels[pixel+2]=122;pixels[pixel+3]=(byte)(alpha*0.72f*255f);
            if(alpha>0.1f)visible++;
        }
        Check(visible > 2000 && visible < 20000, "Sigil has visible detailed linework while most of the ground remains clear");
        System.IO.File.WriteAllBytes("validation197/heavens_sigil.rgba",pixels);
        BatchOneChecks();
        System.Console.WriteLine("Passed " + passed + " compiled-method checks. Unity gameplay and rendered visuals not exercised.");
    }
    static void BatchOneChecks()
    {
        MethodInfo policy = Method(typeof(AlbedosCustomClassesGuardian.GuardianPlugin), "ShouldShowSavingGrace");
        Func<bool,bool,bool,float,float,bool> show = (Func<bool,bool,bool,float,float,bool>)Delegate.CreateDelegate(typeof(Func<bool,bool,bool,float,float,bool>), policy);
        Check(show(true,false,true,0f,100f), "An ally sees Saving Grace ready when a living Priest is nearby");
        Check(show(true,true,false,0f,100f), "A Priest sees their own Saving Grace ready without another Priest");
        Check(!show(true,false,false,0f,100f), "An unprotected ally does not see a false ready icon outside Priest range");
        Check(show(true,false,false,2500f,100f), "Recipient cooldown icon remains after leaving all Priests");
        Check(show(true,false,false,2500f,2499.9f), "Recipient cooldown remains visible until its actual deadline");
        Check(!show(true,false,false,2500f,2500f) && show(true,false,true,2500f,2500f), "Cooldown expiry hides an out-of-range icon or restores ready when a Priest is nearby");
        Check(!show(false,false,true,0f,100f) && show(false,false,false,2500f,100f), "Death never displays false readiness and does not erase an active cooldown indicator");
        Type guardian=typeof(AlbedosCustomClassesGuardian.GuardianPlugin);
        List<CodeInstruction> publish=ReadIL(Method(guardian,"PublishPriestPresence"));
        int owner=publish.FindIndex(delegate(CodeInstruction i){MethodInfo m=i.operand as MethodInfo;return m!=null&&m.Name=="IsOwner";});
        int write=publish.FindIndex(delegate(CodeInstruction i){MethodInfo m=i.operand as MethodInfo;return m!=null&&m.DeclaringType==typeof(ZDO)&&m.Name=="Set";});
        Check(owner>=0 && write>owner && publish.Exists(delegate(CodeInstruction i){MethodInfo m=i.operand as MethodInfo;return m!=null&&m.DeclaringType==typeof(ZDO)&&m.Name=="GetBool";}),
            "Compiled Priest advertisement uses the real owner guard and avoids unchanged network writes");
        List<CodeInstruction> nearby=ReadIL(Method(guardian,"HasNearbyPriest"));
        Check(nearby.Exists(delegate(CodeInstruction i){MethodInfo m=i.operand as MethodInfo;return m!=null&&m.Name=="GetAllPlayers";}) &&
            nearby.Exists(delegate(CodeInstruction i){MethodInfo m=i.operand as MethodInfo;return m!=null&&m.DeclaringType==typeof(ZDO)&&m.Name=="GetBool";}) &&
            !nearby.Exists(delegate(CodeInstruction i){MethodInfo m=i.operand as MethodInfo;return m!=null&&(m.Name=="GetAdvancement"||m.Name=="GetCustomData");}),
            "Compiled ally protection reads the replicated Priest flag rather than another player's private custom data");
        Check(nearby.Exists(delegate(CodeInstruction i){MethodInfo m=i.operand as MethodInfo;return m!=null&&m.Name=="IsDead";}) &&
            nearby.Exists(delegate(CodeInstruction i){MethodInfo m=i.operand as MethodInfo;return m!=null&&m.Name=="get_sqrMagnitude";}),
            "Compiled Priest detection retains living-player and radius checks");
        // Exercise the actual cap/wall orientation correction with both bad caps and a correctly wound side wall.
        UnityEngine.Vector3[] v={new UnityEngine.Vector3(0,0,1),new UnityEngine.Vector3(1,0,1),new UnityEngine.Vector3(0,1,1),
            new UnityEngine.Vector3(0,0,-1),new UnityEngine.Vector3(1,0,-1),new UnityEngine.Vector3(0,1,-1),
            new UnityEngine.Vector3(0,0,1),new UnityEngine.Vector3(0,0,-1),new UnityEngine.Vector3(1,0,1)};
        UnityEngine.Vector3[] n={UnityEngine.Vector3.forward,UnityEngine.Vector3.forward,UnityEngine.Vector3.forward,
            UnityEngine.Vector3.back,UnityEngine.Vector3.back,UnityEngine.Vector3.back,
            UnityEngine.Vector3.down,UnityEngine.Vector3.down,UnityEngine.Vector3.down};
        int[] tri={0,2,1,3,4,5,6,7,8};
        DragonVfx.OrientRelicHeartFaces(v,n,tri);
        bool outward=true;
        for(int i=0;i<tri.Length;i+=3)outward &= UnityEngine.Vector3.Dot(UnityEngine.Vector3.Cross(v[tri[i+1]]-v[tri[i]],v[tri[i+2]]-v[tri[i]]),n[tri[i]])>0f;
        Check(outward, "Actual heart-face correction points front, back and side triangles outward");
        Check(tri[6]==6&&tri[7]==7&&tri[8]==8,"Correctly oriented heart side walls are preserved");
        int[] once=(int[])tri.Clone(); DragonVfx.OrientRelicHeartFaces(v,n,tri);
        Check(System.Linq.Enumerable.SequenceEqual(once,tri),"Heart winding correction is idempotent");
        float front=DragonVfx.HolyHeartFaceDepth(1f,true),back=DragonVfx.HolyHeartFaceDepth(1f,false);
        Check(front-0.16f>0.155f && back+0.16f< -0.50f,"Both raised heart meshes fully clear the actual front/back backing extents");
        bool saturated=true;
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {
            UnityEngine.Color c=DragonVfx.RelicAlbedoTexel(2,(x+0.5f)/32f,(y+0.5f)/32f,new UnityEngine.Color(0.08f,0.82f,0.19f,1f));
            saturated &= c.g>c.r*7f && c.g>c.b*2.4f;
        }
        Check(saturated,"Revised healing-gem albedo keeps strong green contrast across every sampled texel");
    }

}
