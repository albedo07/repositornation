// Immortal Heroes - one-click Mixamo -> Valheim animation bundle builder (Unity 2022.3, Editor only).
// Put this file in Assets/Editor/ and the Mixamo FBX files in Assets/IHAnims/.
// Menu: Immortal Heroes > Build Animation Bundle  -> IHBuild/immortalheroes_anims (copy it to ImmortalHeroesAssets).
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class IHAnimBuilder
{
    const string Folder = "Assets/IHAnims";
    const string Bundle = "immortalheroes_anims";

    [MenuItem("Immortal Heroes/Build Animation Bundle")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) { EditorUtility.DisplayDialog("Immortal Heroes", "Make a folder Assets/IHAnims and put the Mixamo .fbx files in it.", "OK"); return; }
        int n = 0;
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
            n++;
        }
        Directory.CreateDirectory("IHBuild");
        BuildPipeline.BuildAssetBundles("IHBuild", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        EditorUtility.RevealInFinder(Path.Combine("IHBuild", Bundle));
        EditorUtility.DisplayDialog("Immortal Heroes", n + " animations built.\nCopy IHBuild/" + Bundle + " into BepInEx/plugins/ImmortalHeroesAssets.", "OK");
    }

    // "standing melee attack 360 high" -> "melee_attack_360_high"
    static string Clean(string s)
    {
        s = s.ToLowerInvariant();
        if (s.StartsWith("standing ")) s = s.Substring(9);
        return Regex.Replace(s, "[^a-z0-9]+", "_").Trim('_');
    }
}
