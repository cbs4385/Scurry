using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public static class FixScene
{
    [MenuItem("Tools/Fix Save MainMenu Scene")]
    public static void SaveMainMenu()
    {
        var scene = EditorSceneManager.GetActiveScene();
        Debug.Log($"[FixScene] Current scene: name={scene.name}, path={scene.path}, isDirty={scene.isDirty}");
        bool result = EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        Debug.Log($"[FixScene] SaveScene result: {result}");
    }

    [MenuItem("Tools/Setup Build Scenes")]
    public static void SetupBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/M0_Prototype.unity", true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[FixScene] SetupBuildScenes: set {scenes.Count} scenes in build settings (MainMenu=0, M0_Prototype=1)");
    }
}
