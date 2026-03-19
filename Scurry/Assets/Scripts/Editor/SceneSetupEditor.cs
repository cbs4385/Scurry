using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class SceneSetupEditor
{
    [MenuItem("Scurry/Setup All Scenes")]
    public static void SetupAllScenes()
    {
        Debug.Log("[SceneSetupEditor] Setting up all v2.0 scenes...");

        SetupBootstrap();
        SetupMainMenu();
        SetupDeckConstruction();
        SetupGameMap();
        SetupCombat();
        SetupCardReward();
        SetupDeployment();
        SetupRunResult();
        UpdateBuildSettings();

        Debug.Log("[SceneSetupEditor] All v2.0 scenes set up successfully!");
    }

    private static void SetupBootstrap()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Remove default objects

        // Camera
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.AddComponent<AudioListener>();
        camGO.tag = "MainCamera";

        // Directional Light
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // PersistentManagers root
        var pmGO = new GameObject("PersistentManagers");
        pmGO.AddComponent<Scurry.Core.PersistentManagersBootstrap>();
        pmGO.AddComponent<Scurry.Core.RunManager>();
        pmGO.AddComponent<Scurry.Colony.ColonyManager>();
        pmGO.AddComponent<Scurry.Core.GameSettings>();
        pmGO.AddComponent<Scurry.Core.RelicManager>();
        pmGO.AddComponent<Scurry.Core.AchievementManager>();
        pmGO.AddComponent<Scurry.Core.MetaProgressionManager>();
        pmGO.AddComponent<Scurry.Core.LocalizationManager>();

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();
        esGO.transform.SetParent(pmGO.transform);

        // No PersistentCanvas — each scene has its own UICanvas.
        // Achievement/notification UI is routed through per-scene NotificationStack.

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Bootstrap.unity");
        Debug.Log("[SceneSetupEditor] Bootstrap scene set up.");
    }

    private static void SetupMainMenu()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.04f, 0.08f);
        camGO.AddComponent<AudioListener>();
        camGO.tag = "MainCamera";

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        var canvasGO = new GameObject("UICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGO.AddComponent<Scurry.UI.MainMenuManager>();
        canvasGO.AddComponent<Scurry.UI.NotificationStack>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        Debug.Log("[SceneSetupEditor] MainMenu scene set up.");
    }

    private static void SetupRunResult()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        camGO.AddComponent<AudioListener>();
        camGO.tag = "MainCamera";

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // UI Canvas
        var canvasGO = new GameObject("UICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // RunScreenManager
        canvasGO.AddComponent<Scurry.UI.RunScreenManager>();

        // Scrapbook UI
        var scrapbookGO = new GameObject("ScrapbookCanvas");
        var scrapCanvas = scrapbookGO.AddComponent<Canvas>();
        scrapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scrapCanvas.sortingOrder = 10;
        scrapbookGO.AddComponent<CanvasScaler>();
        scrapbookGO.AddComponent<GraphicRaycaster>();
        scrapbookGO.AddComponent<Scurry.UI.ScrapbookUI>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/RunResult.unity");
        Debug.Log("[SceneSetupEditor] RunResult scene set up.");
    }

    [MenuItem("Scurry/Update Build Settings (v2.0)")]
    public static void UpdateBuildSettingsV2()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            // v2.0 scenes (Colony removed — merged into GameMap)
            new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),        // 0
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),         // 1
            new EditorBuildSettingsScene("Assets/Scenes/DeckConstruction.unity", true), // 2
            new EditorBuildSettingsScene("Assets/Scenes/GameMap.unity", true),           // 3
            new EditorBuildSettingsScene("Assets/Scenes/Deployment.unity", true),       // 4
            new EditorBuildSettingsScene("Assets/Scenes/Combat.unity", true),            // 5
            new EditorBuildSettingsScene("Assets/Scenes/CardReward.unity", true),        // 6
            new EditorBuildSettingsScene("Assets/Scenes/RunResult.unity", true),         // 7
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[SceneSetupEditor] Build settings updated for v2.0: Bootstrap=0, MainMenu=1, DeckConstruction=2, GameMap=3, Deployment=4, Combat=5, CardReward=6, RunResult=7");
    }

    private static void UpdateBuildSettings()
    {
        UpdateBuildSettingsV2();
    }

    [MenuItem("Scurry/Setup v2.0 Scenes")]
    public static void SetupV2Scenes()
    {
        Debug.Log("[SceneSetupEditor] SetupV2Scenes: setting up DeckConstruction, GameMap, Deployment, Combat, and CardReward scenes");
        SetupDeckConstruction();
        SetupGameMap();
        SetupDeployment();
        SetupCombat();
        SetupCardReward();
        UpdateBuildSettingsV2();
        Debug.Log("[SceneSetupEditor] SetupV2Scenes: complete");
    }

    private static void SetupDeckConstruction()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.05f, 0.08f);
        camGO.AddComponent<AudioListener>();
        camGO.tag = "MainCamera";

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        var canvasGO = new GameObject("UICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGO.AddComponent<Scurry.UI.DeckConstructionUI>();
        canvasGO.AddComponent<Scurry.UI.NotificationStack>();

        var managersGO = new GameObject("Managers");
        managersGO.AddComponent<Scurry.Cards.DeckConstructionManager>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/DeckConstruction.unity");
        Debug.Log("[SceneSetupEditor] DeckConstruction scene set up.");
    }

    private static void SetupGameMap()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.08f);
        cam.orthographic = true;
        cam.orthographicSize = 8;
        camGO.transform.position = new Vector3(0f, 0f, -10f);
        camGO.AddComponent<AudioListener>();
        camGO.tag = "MainCamera";

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // UI Canvas
        var canvasGO = new GameObject("UICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Core managers
        var managersGO = new GameObject("Managers");
        managersGO.AddComponent<Scurry.Core.TurnManager>();
        managersGO.AddComponent<Scurry.Core.GameManager>();
        managersGO.AddComponent<Scurry.Map.MapManager>();
        managersGO.AddComponent<Scurry.Cards.DeckManager>();
        managersGO.AddComponent<Scurry.Logistics.ResourceManager>();

        // World-space map rendering
        var mapRootGO = new GameObject("MapRoot");

        // UI components
        canvasGO.AddComponent<Scurry.UI.HUDManager>();
        canvasGO.AddComponent<Scurry.UI.MapInteraction>();
        canvasGO.AddComponent<Scurry.UI.TooltipUI>();
        canvasGO.AddComponent<Scurry.UI.NotificationStack>();
        // DeploymentUI is in a dedicated scene (Deployment)

        // Map renderer (world-space)
        var mapRendererGO = new GameObject("MapRenderer");
        mapRendererGO.AddComponent<Scurry.UI.MapRenderer>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameMap.unity");
        Debug.Log("[SceneSetupEditor] GameMap scene set up.");
    }

    private static void SetupCombat()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.03f, 0.06f);
        camGO.AddComponent<AudioListener>();
        camGO.tag = "MainCamera";

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        var canvasGO = new GameObject("UICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGO.AddComponent<Scurry.UI.CombatUI>();
        canvasGO.AddComponent<Scurry.UI.NotificationStack>();

        // CombatResolver is a plain class (not MonoBehaviour), instantiated at runtime by TurnManager

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Combat.unity");
        Debug.Log("[SceneSetupEditor] Combat scene set up.");
    }

    private static void SetupCardReward()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
        camGO.AddComponent<AudioListener>();
        camGO.tag = "MainCamera";

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        var canvasGO = new GameObject("UICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGO.AddComponent<Scurry.UI.CardRewardUI>();
        canvasGO.AddComponent<Scurry.UI.NotificationStack>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/CardReward.unity");
        Debug.Log("[SceneSetupEditor] CardReward scene set up.");
    }

    private static void SetupDeployment()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // No camera or light — this scene is loaded additively on GameMap

        // UI Canvas (high sort order so it overlays the map)
        var canvasGO = new GameObject("DeploymentCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGO.AddComponent<Scurry.UI.DeploymentUI>();
        canvasGO.AddComponent<Scurry.UI.NotificationStack>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Deployment.unity");
        Debug.Log("[SceneSetupEditor] Deployment scene set up (additive on GameMap).");
    }

    private static void ClearScene()
    {
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj != null) Object.DestroyImmediate(obj);
        }
    }
}
