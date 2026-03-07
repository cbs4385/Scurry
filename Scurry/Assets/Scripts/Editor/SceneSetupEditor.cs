using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class SceneSetupEditor
{
    [MenuItem("Scurry/Setup All Scenes")]
    public static void SetupAllScenes()
    {
        Debug.Log("[SceneSetupEditor] Setting up all scenes...");

        SetupBootstrap();
        SetupColonyDraft();
        SetupColonyManagement();
        SetupMapTraversal();
        SetupEncounter();
        SetupRunResult();
        UpdateBuildSettings();

        Debug.Log("[SceneSetupEditor] All scenes set up successfully!");
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
        esGO.AddComponent<StandaloneInputModule>();
        esGO.transform.SetParent(pmGO.transform);

        // PersistentCanvas for achievement toasts
        var canvasGO = new GameObject("PersistentCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.transform.SetParent(pmGO.transform);

        // AchievementToastUI on persistent canvas
        canvasGO.AddComponent<Scurry.UI.AchievementToastUI>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Bootstrap.unity");
        Debug.Log("[SceneSetupEditor] Bootstrap scene set up.");
    }

    private static void SetupColonyDraft()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.06f, 0.04f);
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

        // ColonyDraftUI
        canvasGO.AddComponent<Scurry.UI.ColonyDraftUI>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ColonyDraft.unity");
        Debug.Log("[SceneSetupEditor] ColonyDraft scene set up.");
    }

    private static void SetupColonyManagement()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.06f, 0.04f);
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

        // Managers
        var managersGO = new GameObject("Managers");
        managersGO.AddComponent<Scurry.Colony.ColonyBoardManager>();

        // UI
        canvasGO.AddComponent<Scurry.UI.ColonyUI>();

        // HeroDeckSetAsideUI
        var setAsideGO = new GameObject("HeroDeckSetAsideCanvas");
        var setAsideCanvas = setAsideGO.AddComponent<Canvas>();
        setAsideCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        setAsideCanvas.sortingOrder = 10;
        setAsideGO.AddComponent<CanvasScaler>();
        setAsideGO.AddComponent<GraphicRaycaster>();
        setAsideGO.AddComponent<Scurry.UI.HeroDeckSetAsideUI>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ColonyManagement.unity");
        Debug.Log("[SceneSetupEditor] ColonyManagement scene set up.");
    }

    private static void SetupMapTraversal()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.08f, 0.1f);
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

        // Map Managers
        var managersGO = new GameObject("Managers");
        managersGO.AddComponent<Scurry.Map.MapManager>();
        managersGO.AddComponent<Scurry.UI.ShopManager>();
        managersGO.AddComponent<Scurry.UI.HealingManager>();
        managersGO.AddComponent<Scurry.UI.UpgradeManager>();
        managersGO.AddComponent<Scurry.UI.DraftManager>();
        managersGO.AddComponent<Scurry.UI.EventManager>();
        managersGO.AddComponent<Scurry.UI.RestManager>();

        // UI components
        canvasGO.AddComponent<Scurry.UI.MapUI>();
        canvasGO.AddComponent<Scurry.UI.ResourceUI>();

        // Overlay canvases for node handlers
        var overlayGO = new GameObject("OverlayCanvas");
        var overlayCanvas = overlayGO.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 10;
        overlayGO.AddComponent<CanvasScaler>();
        overlayGO.AddComponent<GraphicRaycaster>();

        // Settings UI
        var settingsGO = new GameObject("SettingsCanvas");
        var settingsCanvas = settingsGO.AddComponent<Canvas>();
        settingsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        settingsCanvas.sortingOrder = 50;
        settingsGO.AddComponent<CanvasScaler>();
        settingsGO.AddComponent<GraphicRaycaster>();
        settingsGO.AddComponent<Scurry.UI.SettingsUI>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MapTraversal.unity");
        Debug.Log("[SceneSetupEditor] MapTraversal scene set up.");
    }

    private static void SetupEncounter()
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
        canvas.sortingOrder = 100; // Higher than MapTraversal for additive loading
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Encounter Managers
        var managersGO = new GameObject("Managers");
        managersGO.AddComponent<Scurry.Encounter.EncounterManager>();
        managersGO.AddComponent<Scurry.Encounter.BossManager>();

        // GameManager
        var gameManagerGO = new GameObject("GameManager");
        gameManagerGO.AddComponent<Scurry.Core.GameManager>();

        // Board
        var boardGO = new GameObject("BoardManager");
        boardGO.AddComponent<Scurry.Board.BoardManager>();
        boardGO.AddComponent<Scurry.Placement.PlacementManager>();
        boardGO.AddComponent<Scurry.Cards.DeckManager>();
        boardGO.AddComponent<Scurry.Cards.HandManager>();

        // Gathering
        var gatherGO = new GameObject("GatheringManager");
        gatherGO.AddComponent<Scurry.Gathering.GatheringManager>();

        // UI
        canvasGO.AddComponent<Scurry.UI.UIManager>();

        // Boss UI canvas
        var bossUIGO = new GameObject("BossUICanvas");
        var bossCanvas = bossUIGO.AddComponent<Canvas>();
        bossCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        bossCanvas.sortingOrder = 110;
        bossUIGO.AddComponent<CanvasScaler>();
        bossUIGO.AddComponent<GraphicRaycaster>();
        bossUIGO.AddComponent<Scurry.UI.BossUI>();

        // Reward UI
        var rewardGO = new GameObject("RewardCanvas");
        var rewardCanvas = rewardGO.AddComponent<Canvas>();
        rewardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rewardCanvas.sortingOrder = 120;
        rewardGO.AddComponent<CanvasScaler>();
        rewardGO.AddComponent<GraphicRaycaster>();
        rewardGO.AddComponent<Scurry.UI.RewardSelectionUI>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Encounter.unity");
        Debug.Log("[SceneSetupEditor] Encounter scene set up.");
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

    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/ColonyDraft.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/ColonyManagement.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MapTraversal.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Encounter.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/RunResult.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/M0_Prototype.unity", true),
        };
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[SceneSetupEditor] Build settings updated with 8 scenes (Bootstrap=0).");
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
