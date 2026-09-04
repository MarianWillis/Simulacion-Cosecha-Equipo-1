using UnityEditor;
using UnityEngine;

// tractorv2.obj declares "usemtl <name>" per part (see patch in the .obj/.mtl),
// with names matching the real material assets below 1:1. AddRemap wires each
// submesh directly to our PBR material on import, instead of Unity generating
// placeholder materials or relying on folder-search-by-name.
public class TractorMaterialAssigner : AssetPostprocessor
{
    private const string TargetModelFileName = "tractorv2.obj";
    private const string MaterialFolder = "Assets/obj/tractor/material";

    private static readonly string[] MaterialNames =
    {
        "Tractor_Body_Paint",
        "Tractor_Tire",
        "Tractor_Frame",
        "Tractor_Glass",
        "Tractor_Grille",
        "Tractor_Headlight",
        "Tractor_Exhaust",
        "Tractor_Accent_Yellow",
    };

    void OnPreprocessModel()
    {
        if (!assetPath.EndsWith(TargetModelFileName)) return;

        var modelImporter = (ModelImporter)assetImporter;
        foreach (var materialName in MaterialNames)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{materialName}.mat");
            if (material == null)
            {
                Debug.LogWarning($"TractorMaterialAssigner: could not find material '{materialName}'");
                continue;
            }
            modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), materialName), material);
        }
    }
}
