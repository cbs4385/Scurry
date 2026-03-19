using UnityEngine;
using UnityEditor;

public static class ReEnableLogger
{
    [MenuItem("Tools/Re-enable Logger")]
    public static void ReEnable()
    {
        Debug.unityLogger.logEnabled = true;
        Debug.Log("[ReEnableLogger] Logger re-enabled successfully");
    }

    [InitializeOnLoadMethod]
    static void OnEditorLoad()
    {
        // Always ensure logger is enabled when scripts reload
        Debug.unityLogger.logEnabled = true;
    }
}