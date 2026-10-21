using System;
using System.Collections;
using System.IO;
using System.Reflection;
public static class ReferenceProbe
{
    public static int Main(string[] args)
    {
        string refs = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs a) {
            string path = Path.Combine(refs, new AssemblyName(a.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        Assembly game = Assembly.LoadFrom(Path.Combine(refs, "assembly_valheim.dll"));
        Type piece = game.GetType("Piece", true);
        FieldInfo field = piece.GetField("s_allPieces", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null || !typeof(IList).IsAssignableFrom(field.FieldType)) throw new Exception("Unsupported Piece registry");
        Console.WriteLine("PASS: supplied Piece.s_allPieces is " + field.FieldType + ", implementing IList.");
        Type effect = game.GetType("EffectList", true); int hooks = 0;
        foreach (MethodInfo m in effect.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            if (m.Name == "Create" && m.ReturnType.IsArray && m.ReturnType.GetElementType().FullName == "UnityEngine.GameObject") hooks++;
        if (hooks == 0) throw new Exception("No compatible EffectList.Create hook");
        Console.WriteLine("PASS: supplied EffectList has " + hooks + " compatible Create method(s).");
        Assembly unity = Assembly.LoadFrom(Path.Combine(refs, "UnityEngine.CoreModule.dll"));
        Type recorder = unity.GetType("Unity.Profiling.ProfilerRecorder", true);
        if (recorder.GetProperty("LastValue") == null || recorder.GetMethod("Dispose") == null) throw new Exception("Missing profiler counter API");
        Console.WriteLine("PASS: supplied Unity includes ProfilerRecorder LastValue and Dispose APIs.");
        if (piece.GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null || piece.GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null) throw new Exception("Piece lifecycle methods unavailable");
        Console.WriteLine("PASS: supplied Piece includes Awake and OnDestroy lifecycle methods.");
        SupportPatchProbe.Run(refs, args[1]);
        Console.WriteLine("Metadata/instruction-adapter inspection only; no Unity objects or Harmony patches executed.");
        return 0;
    }
}
