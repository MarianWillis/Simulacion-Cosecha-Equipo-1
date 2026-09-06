using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// Auto-generates a TMP_FontAsset (dynamic SDF atlas) for every raw .ttf under
// Assets/UI/Fonts/ the moment it's imported, so the UI Toolkit-free/uGUI code
// building the dashboard never needs someone to run Window > TMP > Font Asset
// Creator by hand.
public class TmpFontAssetGenerator : AssetPostprocessor
{
    private const string SourceFolder = "Assets/UI/Fonts";
    // Lives under a "Resources" folder so runtime code can Resources.Load<TMP_FontAsset>
    // it by name without needing a manual Inspector reference anywhere.
    private const string OutputFolder = "Assets/UI/Fonts/Resources/TMP";

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (var path in importedAssets)
        {
            if (!path.StartsWith(SourceFolder) || !path.EndsWith(".ttf")) continue;
            GenerateIfMissing(path);
        }
    }

    private static void GenerateIfMissing(string ttfPath)
    {
        var fontName = Path.GetFileNameWithoutExtension(ttfPath);
        var outputPath = $"{OutputFolder}/{fontName} SDF.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null) return;

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (sourceFont == null)
        {
            Debug.LogWarning($"TmpFontAssetGenerator: could not load Font at {ttfPath}");
            return;
        }

        EnsureFolder(SourceFolder, "Resources");
        EnsureFolder($"{SourceFolder}/Resources", "TMP");

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true
        );
        fontAsset.name = $"{fontName} SDF";

        AssetDatabase.CreateAsset(fontAsset, outputPath);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        AssetDatabase.SaveAssets();

        Debug.Log($"TmpFontAssetGenerator: created {outputPath}");
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
