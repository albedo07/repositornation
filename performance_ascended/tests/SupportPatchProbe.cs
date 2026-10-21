using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

public static class SupportPatchProbe
{
    public static void Run(string refs, string pluginPath)
    {
        Assembly harmony = Assembly.LoadFrom(Path.Combine(refs, "0Harmony.dll"));
        Assembly game = Assembly.LoadFrom(Path.Combine(refs, "assembly_valheim.dll"));
        Assembly plugin = Assembly.LoadFrom(pluginPath);
        Type owner = plugin.GetType("AethelbornPerformance.PerformancePlugin", true);
        Type store = owner.GetNestedType("SupportCacheStore", BindingFlags.NonPublic);
        Activator.CreateInstance(store, true);
        Console.WriteLine("PASS: native terrain registry/event field adapters construct against supplied assemblies without Unity object creation.");
        MethodInfo transpile = owner.GetMethod("SupportCacheTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
        Type code = harmony.GetType("HarmonyLib.CodeInstruction", true);
        FieldInfo opcode = code.GetField("opcode"), operand = code.GetField("operand");
        foreach (string name in new string[] { "Start", "OnDestroy" })
        {
            MethodInfo method = game.GetType("WearNTear", true).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            object instructions = ReadInstructions(method, code);
            List<object> before = new List<object>(); foreach (object instruction in (IEnumerable)instructions) before.Add(instruction);
            object rewritten = transpile.Invoke(null, new object[] { instructions, method });
            List<object> after = new List<object>(); foreach (object instruction in (IEnumerable)rewritten) after.Add(instruction);
            if (before.Count != after.Count) throw new Exception("Native instruction count changed");
            int changed = 0;
            for (int i = 0; i < before.Count; i++)
            {
                if (object.ReferenceEquals(before[i], after[i])) continue;
                MethodInfo target = operand.GetValue(after[i]) as MethodInfo;
                string expected = name == "Start" ? "SubscribeSupportCache" : "UnsubscribeSupportCache";
                if (!object.Equals(opcode.GetValue(after[i]), OpCodes.Call) || target == null || target.Name != expected) throw new Exception("Unexpected native instruction rewrite");
                changed++;
            }
            if (changed != 1) throw new Exception("Expected exactly one native event call replacement");
            Console.WriteLine("PASS: actual WearNTear." + name + " instruction stream changes exactly one event call; every other instruction is retained.");
        }
        Type listType = typeof(List<>).MakeGenericType(code);
        object empty = Activator.CreateInstance(listType);
        bool rejected = false;
        try { transpile.Invoke(null, new object[] { empty, game.GetType("WearNTear").GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic) }); }
        catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; }
        if (!rejected) throw new Exception("Unsupported native layout must be rejected");
        Console.WriteLine("PASS: unsupported native instruction layout is rejected rather than partially patched.");
    }
    // Decode actual supplied method IL without executing native code or requiring a MonoMod runtime.
    private static object ReadInstructions(MethodInfo method, Type codeType)
    {
        Dictionary<short, OpCode> opcodes = new Dictionary<short, OpCode>();
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.FieldType == typeof(OpCode)) { OpCode op = (OpCode)field.GetValue(null); opcodes[op.Value] = op; }
        IList result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(codeType));
        byte[] bytes = method.GetMethodBody().GetILAsByteArray(); int offset = 0;
        while (offset < bytes.Length)
        {
            short value = bytes[offset++]; if (value == 0xfe) value = (short)(0xfe00 | bytes[offset++]);
            OpCode op = opcodes[value]; object operand = null;
            switch (op.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineI: case OperandType.ShortInlineBrTarget: operand = (sbyte)bytes[offset++]; break;
                case OperandType.ShortInlineVar: operand = bytes[offset++]; break;
                case OperandType.InlineVar: operand = BitConverter.ToUInt16(bytes, offset); offset += 2; break;
                case OperandType.InlineI8: operand = BitConverter.ToInt64(bytes, offset); offset += 8; break;
                case OperandType.ShortInlineR: operand = BitConverter.ToSingle(bytes, offset); offset += 4; break;
                case OperandType.InlineR: operand = BitConverter.ToDouble(bytes, offset); offset += 8; break;
                case OperandType.InlineSwitch:
                    int count = BitConverter.ToInt32(bytes, offset); offset += 4; int[] targets = new int[count];
                    for (int i = 0; i < count; i++) { targets[i] = BitConverter.ToInt32(bytes, offset); offset += 4; } operand = targets; break;
                case OperandType.InlineMethod: case OperandType.InlineField: case OperandType.InlineType: case OperandType.InlineTok:
                    operand = method.Module.ResolveMember(BitConverter.ToInt32(bytes, offset), method.DeclaringType.GetGenericArguments(), method.GetGenericArguments()); offset += 4; break;
                case OperandType.InlineString: operand = method.Module.ResolveString(BitConverter.ToInt32(bytes, offset)); offset += 4; break;
                default: operand = BitConverter.ToInt32(bytes, offset); offset += 4; break;
            }
            result.Add(Activator.CreateInstance(codeType, new object[] { op, operand }));
        }
        return result;
    }

}
