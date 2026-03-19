using UnityEngine;
using UnityEditor;

public class ModelOrientationTest
{
    [MenuItem("Scurry/Log Model 025 Orientation")]
    public static void LogOrientation()
    {
        var prefab = Resources.Load<GameObject>("Card Models/025");
        if (prefab == null) { Debug.LogError("Model 025 not found"); return; }

        Debug.Log($"[ModelTest] Prefab root: euler={prefab.transform.eulerAngles}, " +
                  $"forward={prefab.transform.forward}, up={prefab.transform.up}, right={prefab.transform.right}");

        // Instantiate temporarily to check
        var instance = Object.Instantiate(prefab);
        instance.name = "TestModel025";

        Debug.Log($"[ModelTest] Instance (no rotation): euler={instance.transform.eulerAngles}, " +
                  $"forward={instance.transform.forward}, up={instance.transform.up}");

        // Log camera info
        var cam = Camera.main;
        if (cam != null)
        {
            Debug.Log($"[ModelTest] Camera: pos={cam.transform.position}, euler={cam.transform.eulerAngles}, " +
                      $"forward={cam.transform.forward}, up={cam.transform.up}");
        }

        // The map is on XY plane (Z=0). Nodes are at (x, y, 0).
        // Models need: base flat on XY plane, front facing toward camera.
        // "Flat on XY" means model's local up = world Z (0,0,1) or (0,0,-1).
        // Try various rotations and log results:
        string[] labels = { "Identity", "Z+180", "X+90", "X-90", "X+90 Z+180", "X-90 Z+180" };
        Quaternion[] rots = {
            Quaternion.identity,
            Quaternion.Euler(0, 0, 180),
            Quaternion.Euler(90, 0, 0),
            Quaternion.Euler(-90, 0, 0),
            Quaternion.Euler(90, 0, 180),
            Quaternion.Euler(-90, 0, 180),
        };

        for (int i = 0; i < labels.Length; i++)
        {
            instance.transform.rotation = rots[i];
            // Model's "visual up" after FBX import rotation is baked into children
            // Check what world direction the model's local axes point
            Debug.Log($"[ModelTest] Rotation '{labels[i]}': euler={instance.transform.eulerAngles}, " +
                      $"forward={instance.transform.forward}, up={instance.transform.up}, right={instance.transform.right}");
        }

        // Also try composing with the prefab's baked rotation
        var prefabRot = prefab.transform.rotation;
        Debug.Log($"[ModelTest] Prefab baked rotation: {prefabRot.eulerAngles}");

        string[] compLabels = { "baked*Z180", "baked*X90", "baked*X-90", "baked*Y180" };
        Quaternion[] compRots = {
            prefabRot * Quaternion.Euler(0, 0, 180),
            prefabRot * Quaternion.Euler(90, 0, 0),
            prefabRot * Quaternion.Euler(-90, 0, 0),
            prefabRot * Quaternion.Euler(0, 180, 0),
        };

        for (int i = 0; i < compLabels.Length; i++)
        {
            instance.transform.rotation = compRots[i];
            Debug.Log($"[ModelTest] Composed '{compLabels[i]}': euler={instance.transform.eulerAngles}, " +
                      $"forward={instance.transform.forward}, up={instance.transform.up}");
        }

        Object.DestroyImmediate(instance);
        Debug.Log("[ModelTest] Done — test object destroyed");
    }
}
