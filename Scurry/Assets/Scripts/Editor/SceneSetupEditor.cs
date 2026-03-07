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
        var gameManager = gameManagerGO.AddComponent<Scurry.Core.GameManager>();

        // Board
        var boardGO = new GameObject("BoardManager");
        var boardManager = boardGO.AddComponent<Scurry.Board.BoardManager>();
        var placementManager = boardGO.AddComponent<Scurry.Placement.PlacementManager>();
        boardGO.AddComponent<Scurry.Cards.DeckManager>();
        var handManager = boardGO.AddComponent<Scurry.Cards.HandManager>();

        // Wire up PlacementManager SerializeField references
        var pmSO = new SerializedObject(placementManager);
        pmSO.FindProperty("boardManager").objectReferenceValue = boardManager;
        pmSO.FindProperty("handManager").objectReferenceValue = handManager;
        pmSO.FindProperty("gameManager").objectReferenceValue = gameManager;

        // Load and assign prefabs
        var heroTokenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HeroToken.prefab");
        var resourceTokenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ResourceToken.prefab");
        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Card.prefab");

        if (heroTokenPrefab != null)
            pmSO.FindProperty("heroTokenPrefab").objectReferenceValue = heroTokenPrefab;
        else
            Debug.LogWarning("[SceneSetupEditor] HeroToken.prefab not found at Assets/Prefabs/HeroToken.prefab");
        if (resourceTokenPrefab != null)
            pmSO.FindProperty("resourceTokenPrefab").objectReferenceValue = resourceTokenPrefab;
        else
            Debug.LogWarning("[SceneSetupEditor] ResourceToken.prefab not found at Assets/Prefabs/ResourceToken.prefab");
        pmSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire up HandManager cardPrefab
        var hmSO = new SerializedObject(handManager);
        if (cardPrefab != null)
            hmSO.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
        else
            Debug.LogWarning("[SceneSetupEditor] Card.prefab not found at Assets/Prefabs/Card.prefab");
        hmSO.ApplyModifiedPropertiesWithoutUndo();

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
        Debug.Log("[SceneSetupEditor] Encounter scene set up (with prefab references wired).");
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

    [MenuItem("Scurry/Fix Encounter Scene Wiring")]
    public static void FixEncounterSceneWiring()
    {
        // Load the Encounter scene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Encounter.unity", OpenSceneMode.Single);

        // Find existing components
        var boardManager = Object.FindAnyObjectByType<Scurry.Board.BoardManager>();
        var handManager = Object.FindAnyObjectByType<Scurry.Cards.HandManager>();
        var placementManager = Object.FindAnyObjectByType<Scurry.Placement.PlacementManager>();
        var gameManager = Object.FindAnyObjectByType<Scurry.Core.GameManager>();

        // Load prefabs
        var heroTokenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HeroToken.prefab");
        var resourceTokenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ResourceToken.prefab");
        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Card.prefab");
        var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BoardTile.prefab");
        var boardLayout = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Data/Board/BoardLayout_Wilds_4x4.asset");

        // Wire up BoardManager references
        if (boardManager != null)
        {
            var bmSO = new SerializedObject(boardManager);
            if (tilePrefab != null) bmSO.FindProperty("tilePrefab").objectReferenceValue = tilePrefab;
            if (resourceTokenPrefab != null) bmSO.FindProperty("resourceTokenPrefab").objectReferenceValue = resourceTokenPrefab;
            if (boardLayout != null) bmSO.FindProperty("boardLayout").objectReferenceValue = boardLayout;
            bmSO.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[SceneSetupEditor] BoardManager wired: tilePrefab={tilePrefab != null}, resourceToken={resourceTokenPrefab != null}, boardLayout={boardLayout != null}");
        }

        // Wire up PlacementManager references
        if (placementManager != null)
        {
            var pmSO = new SerializedObject(placementManager);
            if (boardManager != null) pmSO.FindProperty("boardManager").objectReferenceValue = boardManager;
            if (handManager != null) pmSO.FindProperty("handManager").objectReferenceValue = handManager;
            if (gameManager != null) pmSO.FindProperty("gameManager").objectReferenceValue = gameManager;
            if (heroTokenPrefab != null) pmSO.FindProperty("heroTokenPrefab").objectReferenceValue = heroTokenPrefab;
            if (resourceTokenPrefab != null) pmSO.FindProperty("resourceTokenPrefab").objectReferenceValue = resourceTokenPrefab;
            pmSO.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[SceneSetupEditor] PlacementManager wired.");
        }

        // Wire up HandManager cardPrefab
        if (handManager != null)
        {
            var hmSO = new SerializedObject(handManager);
            if (cardPrefab != null) hmSO.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            hmSO.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[SceneSetupEditor] HandManager wired.");
        }

        // Wire up GatheringManager references
        var gatheringManager = Object.FindAnyObjectByType<Scurry.Gathering.GatheringManager>();
        if (gatheringManager != null)
        {
            var gmSO = new SerializedObject(gatheringManager);
            if (boardManager != null) gmSO.FindProperty("boardManager").objectReferenceValue = boardManager;
            var enemyTokenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyToken.prefab");
            if (enemyTokenPrefab != null) gmSO.FindProperty("enemyTokenPrefab").objectReferenceValue = enemyTokenPrefab;
            gmSO.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[SceneSetupEditor] GatheringManager wired: boardManager={boardManager != null}, enemyTokenPrefab={enemyTokenPrefab != null}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetupEditor] Encounter scene wiring fixed — all prefab and component references set.");
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
