using UnityEngine;
using UnityEditor;
using System.IO;

public class AssignModelTextures
{
    [MenuItem("Scurry/Assign Textures To Card Models")]
    public static void AssignAll()
    {
        string modelsDir = "Assets/Resources/Card Models";
        string texturesDir = modelsDir + "/Textures";

        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { modelsDir });
        Debug.Log($"[AssignModelTextures] Found {fbxGuids.Length} models");

        int assigned = 0;

        foreach (string guid in fbxGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.EndsWith(".fbx")) continue;

            string basename = Path.GetFileNameWithoutExtension(assetPath);

            // Find the diffuse texture
            string diffusePath = $"{texturesDir}/{basename}_texture_diffuse.png";
            var diffuseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            if (diffuseTex == null)
            {
                Debug.Log($"[AssignModelTextures] No diffuse texture for {basename} at {diffusePath}");
                continue;
            }

            // Find normal map
            string normalPath = $"{texturesDir}/{basename}_texture_normal.png";
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            // Find metallic/roughness
            string metalPath = $"{texturesDir}/{basename}_texture_metallic-texture_roughness.png";
            var metalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metalPath);

            // Get the model's material — it's a sub-asset of the FBX
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) continue;

            // We need to remap the material to an external one with textures assigned
            // First check if we already have an external material
            string matDir = modelsDir + "/Materials";
            if (!AssetDatabase.IsValidFolder(matDir))
                AssetDatabase.CreateFolder(modelsDir, "Materials");

            string matPath = $"{matDir}/{basename}_mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (mat == null)
            {
                // Create new URP Lit material
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            // Assign textures
            mat.SetTexture("_BaseMap", diffuseTex);
            mat.SetColor("_BaseColor", Color.white);

            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (metalTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", metalTex);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            EditorUtility.SetDirty(mat);

            // Remap the model's material to use our external material
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();

            // Now assign material via renderer on the prefab
            // We'll do this at runtime instead — the model loads with default material,
            // and we assign via code. But better: use externalObjects to remap.

            // Actually, let's use the SearchAndRemap approach
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            var map = new AssetImporter.SourceAssetIdentifier(typeof(Material), "model");
            importer.AddRemap(map, mat);
            importer.SaveAndReimport();

            assigned++;
            Debug.Log($"[AssignModelTextures] Assigned textures to {basename} (diffuse={diffuseTex.name}, normal={normalTex?.name ?? "none"}, metal={metalTex?.name ?? "none"})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AssignModelTextures] Done: {assigned} models updated");
    }
}
