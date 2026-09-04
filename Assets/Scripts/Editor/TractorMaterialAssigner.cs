using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Auto-assigns per-part PBR materials to tractorv2.obj on import, since the
// source mesh has no materials baked in (only named object groups per part).
public class TractorMaterialAssigner : AssetPostprocessor
{
    private const string TargetModelFileName = "tractorv2.obj";
    private const string MaterialFolder = "Assets/obj/tractor/material";

    private static readonly (string NameContains, string MaterialName)[] PartMap =
    {
        ("window", "Tractor_Glass"),
        ("wheel", "Tractor_Tire"),
        ("headlight", "Tractor_Headlight"),
        ("grille_lower", "Tractor_Accent_Yellow"), // checked before the generic "grille" match below
        ("grille", "Tractor_Grille"),
        ("exhaust", "Tractor_Exhaust"),
        ("frame", "Tractor_Frame"),
        // cab, hood, roof, fender_* fall through to the body paint below.
    };

    private const string DefaultMaterialName = "Tractor_Body_Paint";

    void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.EndsWith(TargetModelFileName)) return;

        var cache = new Dictionary<string, Material>();
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materialName = ResolveMaterialName(renderer.gameObject.name);
            var material = LoadMaterial(materialName, cache);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }

    private static string ResolveMaterialName(string partName)
    {
        var lower = partName.ToLowerInvariant();
        foreach (var (nameContains, materialName) in PartMap)
        {
            if (lower.Contains(nameContains)) return materialName;
        }
        return DefaultMaterialName;
    }

    private static Material LoadMaterial(string materialName, Dictionary<string, Material> cache)
    {
        if (cache.TryGetValue(materialName, out var cached)) return cached;

        var path = $"{MaterialFolder}/{materialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Debug.LogWarning($"TractorMaterialAssigner: could not find material at {path}");
        }
        cache[materialName] = material;
        return material;
    }
}
