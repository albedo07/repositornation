using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using DragonsAltarCombat;
using AlbedosCustomClassesAdvanced;
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
        System.Console.WriteLine("Passed " + passed + " compiled-method checks. Unity gameplay and rendered visuals not exercised.");
    }
}
