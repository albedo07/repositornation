using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using DragonsAltarRanger;
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
    static List<CodeInstruction> Transpile(string name, List<CodeInstruction> code)
    {
        MethodInfo method = typeof(RangerPlugin).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        return new List<CodeInstruction>((IEnumerable<CodeInstruction>)method.Invoke(null, new object[] { code }));
    }
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
    static void Main()
    {
        List<CodeInstruction> actual = ReadIL(Method(typeof(Player), "CheckRun"));
        int clearIndex = actual.FindIndex(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return m != null && m.Name == "ClearActionQueue"; });
        Check(clearIndex >= 0 && ((MethodInfo)actual[clearIndex].operand).DeclaringType == typeof(Humanoid), "Actual Valheim sprint IL calls the Humanoid virtual base slot");
        List<CodeInstruction> original = new List<CodeInstruction>(); foreach (CodeInstruction i in actual) original.Add(new CodeInstruction(i.opcode, i.operand));
        List<CodeInstruction> result = Transpile("SprintReloadTranspiler", actual);
        Check(((MethodInfo)result[clearIndex].operand).Name == "ClearActionsUnlessReloading" && result[clearIndex].opcode == OpCodes.Call, "Actual Valheim sprint cancellation is intercepted");
        bool unchanged = result.Count == original.Count;
        for (int i = 0; i < result.Count; i++) if (i != clearIndex) unchanged &= result[i].opcode == original[i].opcode && Equals(result[i].operand, original[i].operand);
        Check(unchanged, "All other actual sprint instructions (stamina and run conditions) preserved");
        List<CodeInstruction> input = new List<CodeInstruction> { new CodeInstruction(OpCodes.Callvirt, Method(typeof(Player), "ClearActionQueue")), new CodeInstruction(OpCodes.Callvirt, Method(typeof(UnrelatedQueue), "ClearActionQueue")), new CodeInstruction(OpCodes.Ret) };
        result = Transpile("SprintReloadTranspiler", input);
        Check(((MethodInfo)result[0].operand).Name == "ClearActionsUnlessReloading", "Override-form cancellation is also supported");
        Check(((MethodInfo)result[1].operand).DeclaringType == typeof(UnrelatedQueue) && result[2].opcode == OpCodes.Ret, "Unrelated same-name method is untouched");
        actual = ReadIL(Method(typeof(Humanoid), "CheckRun"));
        result = Transpile("BowSprintTranspiler", actual);
        Check(result.Exists(delegate(CodeInstruction i) { MethodInfo m = i.operand as MethodInfo; return m != null && m.Name == "DrawingBlocksSprint"; }), "Existing Acrobat bow sprint patch still matches actual IL");
        FieldInfo queue = AccessTools.Field(typeof(Player), "m_actionQueue");
        Type actionType = queue.FieldType.GetGenericArguments()[0];
        FieldInfo typeField = AccessTools.Field(actionType, "m_type"), itemField = AccessTools.Field(actionType, "m_item"), timeField = AccessTools.Field(actionType, "m_time");
        Check(typeField != null && itemField != null && timeField != null && Enum.IsDefined(typeField.FieldType, "Reload"), "Actual game reload action exposes the cached type, item and progress fields");
        System.Console.WriteLine("NOT RUN: live-player reload predicates, resource use, animation and speed require Unity. An attempted predicate invocation hit the existing Shader.PropertyToID native initializer outside Unity.");
        Check(Transpile("SprintReloadTranspiler", new List<CodeInstruction> { new CodeInstruction(OpCodes.Ret) })[0].opcode == OpCodes.Ret, "Methods without sprint cancellation are unchanged");
        System.Console.WriteLine("Passed " + passed + " compiled-method checks. Unity gameplay not exercised.");
    }
}
