using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using DragonsAltarRanger;
class ScopeTests
{
    static int passed;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); System.Console.WriteLine("PASS: " + name); passed++; }
    static List<CodeInstruction> Transpile(string name, List<CodeInstruction> code)
    {
        MethodInfo method = typeof(RangerPlugin).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        return new List<CodeInstruction>((IEnumerable<CodeInstruction>)method.Invoke(null, new object[] { code }));
    }
    static void Main()
    {
        MethodInfo draw = AccessTools.Method(typeof(Humanoid), "IsDrawingBow", Type.EmptyTypes);
        MethodInfo clear = AccessTools.Method(typeof(Player), "ClearActionQueue", Type.EmptyTypes);
        MethodInfo unrelated = typeof(string).GetMethod("IsNullOrEmpty");
        List<CodeInstruction> input = new List<CodeInstruction> { new CodeInstruction(OpCodes.Callvirt, draw), new CodeInstruction(OpCodes.Call, unrelated), new CodeInstruction(OpCodes.Ret) };
        var result = Transpile("BowSprintTranspiler", input);
        Check(((MethodInfo)result[0].operand).Name == "DrawingBlocksSprint" && result[0].opcode == OpCodes.Call, "Acrobat drawing gate replaced");
        Check(Equals(result[1].operand, unrelated) && result.Count == 3, "Unrelated sprint checks unchanged");
        input = new List<CodeInstruction> { new CodeInstruction(OpCodes.Call, clear), new CodeInstruction(OpCodes.Call, unrelated) };
        result = Transpile("SprintReloadTranspiler", input);
        Check(((MethodInfo)result[0].operand).Name == "ClearActionsUnlessReloading", "Sprint queue clear redirected to reload guard");
        Check(Equals(result[1].operand, unrelated) && result.Count == 2, "Reload patch preserves unrelated calls");
        input = new List<CodeInstruction> { new CodeInstruction(OpCodes.Ret) };
        Check(Transpile("BowSprintTranspiler", input)[0].opcode == OpCodes.Ret, "No replacement when drawing gate absent");
        Check(Transpile("SprintReloadTranspiler", input)[0].opcode == OpCodes.Ret, "No replacement when queue clear absent");
        System.Console.WriteLine("Passed " + passed + " shipped-method checks. Unity gameplay not exercised.");
    }
}
