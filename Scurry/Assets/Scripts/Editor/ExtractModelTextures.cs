using UnityEngine;
using UnityEditor;

public class ExtractModelTextures
{
    [MenuItem("Scurry/Extract All Card Model Textures")]
    public static void ExtractAll()
    {
        string modelsDir = "Assets/Resources/Card Models";
        string texturesDir = modelsDir + "/Textures";

        if (!AssetDatabase.IsValidFolder(texturesDir))
            AssetDatabase.CreateFolder(modelsDir, "Textures");

        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { modelsDir });
        Debug.Log($"[ExtractModelTextures] Found {fbxGuids.Length} model assets");

        int extracted = 0;
        int noTextures = 0;

        foreach (string guid in fbxGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.EndsWith(".fbx")) continue;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) continue;

            // Check for embedded textures as sub-assets
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            bool hasTexture = false;
            foreach (var sub in subAssets)
            {
                if (sub is Texture2D tex)
                {
                    hasTexture = true;
                    Debug.Log($"[ExtractModelTextures] Found texture '{tex.name}' ({tex.width}x{tex.height}) in {assetPath}");
                }
            }

            if (!hasTexture)
            {
                noTextures++;
                continue;
            }

            // Extract
            bool ok = importer.ExtractTextures(texturesDir);
            if (ok)
            {
                extracted++;
                Debug.Log($"[ExtractModelTextures] Extracted textures from {assetPath}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ExtractModelTextures] Done: extracted={extracted}, noTextures={noTextures}");
    }
}
