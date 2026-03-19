using UnityEngine;
using UnityEditor;

public class ModelInspector
{
    [MenuItem("Scurry/Inspect Card Model 001")]
    public static void Inspect001()
    {
        var prefab = Resources.Load<GameObject>("Card Models/001");
        if (prefab == null)
        {
            Debug.LogError("[ModelInspector] Could not load Card Models/001");
            return;
        }

        Debug.Log($"[ModelInspector] Loaded prefab: {prefab.name}");

        var renderers = prefab.GetComponentsInChildren<Renderer>();
        Debug.Log($"[ModelInspector] Renderers: {renderers.Length}");

        foreach (var r in renderers)
        {
            Debug.Log($"[ModelInspector] Renderer: {r.name}, type={r.GetType().Name}");
            var mats = r.sharedMaterials;
            Debug.Log($"[ModelInspector]   Materials: {mats.Length}");

            foreach (var mat in mats)
            {
                if (mat == null)
                {
                    Debug.Log("[ModelInspector]   Material: NULL");
                    continue;
                }

                Debug.Log($"[ModelInspector]   Material: {mat.name}, shader={mat.shader.name}");
                Debug.Log($"[ModelInspector]     color={mat.color}, mainTex={(mat.mainTexture != null ? mat.mainTexture.name : "NULL")}");

                // Check all texture properties
                var texNames = mat.GetTexturePropertyNames();
                var texIds = mat.GetTexturePropertyNameIDs();
                for (int i = 0; i < texNames.Length; i++)
                {
                    var tex = mat.GetTexture(texIds[i]);
                    Debug.Log($"[ModelInspector]     texProp[{i}]: '{texNames[i]}' = {(tex != null ? tex.name + " (" + tex.GetType().Name + ")" : "NULL")}");
                }

                // Check keyword states
                var keywords = mat.shaderKeywords;
                Debug.Log($"[ModelInspector]     keywords: [{string.Join(", ", keywords)}]");
            }
        }

        // Check mesh for vertex colors
        var meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh != null)
            {
                var mesh = mf.sharedMesh;
                Debug.Log($"[ModelInspector] Mesh: {mesh.name}, verts={mesh.vertexCount}, hasColors={mesh.colors.Length > 0}, hasColors32={mesh.colors32.Length > 0}, hasUV={mesh.uv.Length > 0}");
            }
        }
    }
}
