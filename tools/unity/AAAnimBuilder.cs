// Aethelborn Ascended - one-click Mixamo -> Valheim animation bundle builder (Unity 2022.3, Editor only).
// Put this file in Assets/Editor/ and the Mixamo FBX files in Assets/AAanims/.
// Menu: Aethelborn Ascended > Build Animation Bundle  -> AABuild/aethelborn_anims, then copied straight into
// Valheim/BepInEx/plugins/ImmortalHeroesAssets (Steam default folder, or the folder you pick once with
// Aethelborn Ascended > Set Valheim Folder). INSTALL.bat also picks it up from AABuild automatically.
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class AAAnimBuilder
{
    const string Folder = "Assets/AAanims";
    const string Bundle = "aethelborn_anims";
    const string PrefKey = "AethelbornValheimFolder";

    [MenuItem("Aethelborn Ascended/Set Valheim Folder")]
    public static void SetValheimFolder()
    {
        string picked = EditorUtility.OpenFolderPanel("Pick your Valheim folder (the one with valheim.exe)", EditorPrefs.GetString(PrefKey, ""), "");
        if (!string.IsNullOrEmpty(picked)) EditorPrefs.SetString(PrefKey, picked);
    }

    static string PluginsAssetDir()
    {
        List<string> roots = new List<string>();
        string saved = EditorPrefs.GetString(PrefKey, "");
        if (!string.IsNullOrEmpty(saved)) roots.Add(saved);
        roots.Add(@"C:\Program Files (x86)\Steam\steamapps\common\Valheim");
        roots.Add(@"C:\Program Files\Steam\steamapps\common\Valheim");
        roots.Add(@"D:\SteamLibrary\steamapps\common\Valheim");
        roots.Add(@"E:\SteamLibrary\steamapps\common\Valheim");
        foreach (string r in roots)
        {
            string plugins = Path.Combine(Path.Combine(r, "BepInEx"), "plugins");
            if (Directory.Exists(plugins)) return Path.Combine(plugins, "ImmortalHeroesAssets");
        }
        return null;
    }

    [MenuItem("Aethelborn Ascended/Build Animation Bundle")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) { EditorUtility.DisplayDialog("Aethelborn Ascended", "Make a folder Assets/AAanims and put the Mixamo .fbx files in it.", "OK"); return; }
        int n = 0;
        List<string> badAvatars = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) continue;
            imp.animationType = ModelImporterAnimationType.Human;          // retargets onto Valheim's humanoid body
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            imp.SaveAndReimport();
            ModelImporterClipAnimation[] src = imp.defaultClipAnimations;
            if (src == null || src.Length == 0) continue;
            string name = Clean(Path.GetFileNameWithoutExtension(path));
            List<ModelImporterClipAnimation> clips = new List<ModelImporterClipAnimation>();
            foreach (bool mirror in new[] { false, true })
            {
                ModelImporterClipAnimation a = src[0];
                ModelImporterClipAnimation c = new ModelImporterClipAnimation();
                c.name = name + (mirror ? "_mirror" : "");
                c.takeName = a.takeName;
                c.firstFrame = a.firstFrame;
                c.lastFrame = a.lastFrame;
                c.loopTime = false;
                // Bake ALL root motion into the pose: the clip never moves or turns the real character
                // (the skill code keeps owning movement); a spin still turns the body visibly.
                c.lockRootRotation = true; c.keepOriginalOrientation = true;
                c.lockRootHeightY = true; c.keepOriginalPositionY = true;
                c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
                c.mirror = mirror;
                clips.Add(c);
            }
            imp.clipAnimations = clips.ToArray();
            imp.assetBundleName = Bundle;
            imp.SaveAndReimport();
            // a clip whose humanoid avatar failed plays as nothing in game - report it
            bool avatarOk = false;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                Avatar av = o as Avatar;
                if (av != null && av.isValid && av.isHuman) avatarOk = true;
            }
            if (!avatarOk) badAvatars.Add(Path.GetFileName(path));
            n++;
        }
        Directory.CreateDirectory("AABuild");
        BuildPipeline.BuildAssetBundles("AABuild", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        string built = Path.Combine("AABuild", Bundle);
        string msg = n + " animations built (" + (n * 2) + " clips incl. mirrors).";
        if (badAvatars.Count > 0) msg += "\n\nHUMANOID SETUP FAILED for " + badAvatars.Count + " file(s) (they will not play): " + string.Join(", ", badAvatars.ToArray());
        string dest = PluginsAssetDir();
        if (dest != null)
        {
            Directory.CreateDirectory(dest);
            File.Copy(built, Path.Combine(dest, Bundle), true);
            msg += "\n\nCopied into " + dest + " - just start Valheim.";
        }
        else
        {
            EditorUtility.RevealInFinder(built);
            msg += "\n\nValheim folder not found: run INSTALL.bat again (it picks the bundle up from AABuild), or use Aethelborn Ascended > Set Valheim Folder and build again.";
        }
        EditorUtility.DisplayDialog("Aethelborn Ascended", msg, "OK");
    }

    // "standing melee attack 360 high" -> "melee_attack_360_high"
    static string Clean(string s)
    {
        s = s.ToLowerInvariant();
        if (s.StartsWith("standing ")) s = s.Substring(9);
        return Regex.Replace(s, "[^a-z0-9]+", "_").Trim('_');
    }
}
