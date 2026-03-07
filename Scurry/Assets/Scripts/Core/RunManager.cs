using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scurry.Data;
using Scurry.Colony;
using Scurry.Map;
using Scurry.Encounter;
using Scurry.Gathering;
using Scurry.UI;
using Scurry.Board;
using Scurry.Cards;
using Scurry.Placement;
using Scurry.Interfaces;

namespace Scurry.Core
{
    public class RunManager : MonoBehaviour, IRunManager
    {
        private static RunManager _instance;
        public static RunManager Instance => _instance;

        [Header("Level Configs")]
        [SerializeField] private MapConfigSO[] levelConfigs; // Index 0 = Level 1, etc.

        [Header("Colony Deck")]
        [SerializeField] private List<ColonyCardDefinitionSO> colonyDeck = new List<ColonyCardDefinitionSO>();

        [Header("Hero Deck")]
        [SerializeField] private List<CardDefinitionSO> heroDeck = new List<CardDefinitionSO>();

        // Scene-specific managers (discovered dynamically after scene loads)
        private GameManager gameManager;
        private ColonyBoardManager colonyBoardManager;
        private MapManager mapManager;
        private EncounterManager encounterManager;
        private BossManager bossManager;
        private ShopManager shopManager;
        private HealingManager healingManager;
        private UpgradeManager upgradeManager;
        private DraftManager draftManager;
        private EventManager eventManager;
        private RestManager restManager;
        private HeroDeckSetAsideUI heroDeckSetAsideUI;

        // Run state
        private RunState runState;
        private int currentLevel; // 1-indexed
        private ColonyConfig colonyConfig;

        // Hero wound tracking
        private HashSet<CardDefinitionSO> woundedHeroes = new HashSet<CardDefinitionSO>();
        private HashSet<CardDefinitionSO> exhaustedHeroes = new HashSet<CardDefinitionSO>();

        // Score
        private int encountersCompleted;
        private int totalResourcesGathered;
        private int enemiesDefeated;
        private int bossesKilled;
        private int nodesVisited;

        // Discovery tracking for meta-progression
        private List<string> enemiesEncountered = new List<string>();
        private List<string> eventsEncountered = new List<string>();
        private List<string> bossesEncountered = new List<string>();

        // Track last node type for encounter rewards
        private NodeType lastNodeType;

        // Pending encounter data (for cross-scene communication)
        private EncounterDefinitionSO pendingEncounterDef;
        private int pendingDifficulty;
        private NodeType pendingNodeType;
        private bool pendingIsBoss;
        private BossDefinitionSO pendingBossDef;

        // Public accessors
        public RunState CurrentRunState => runState;
        public int CurrentLevel => currentLevel;
        public ColonyConfig ActiveColonyConfig => colonyConfig;
        public List<ColonyCardDefinitionSO> ColonyCardPool => colonyDeck;
        public IReadOnlyList<CardDefinitionSO> HeroDeck => heroDeck;
        public IReadOnlyCollection<CardDefinitionSO> WoundedHeroes => woundedHeroes;
        public MapConfigSO CurrentLevelConfig => levelConfigs != null && currentLevel > 0 && currentLevel <= levelConfigs.Length ? levelConfigs[currentLevel - 1] : null;
        public int FoodStockpile => ColonyManager.Instance != null ? ColonyManager.Instance.FoodStockpile : 0;
        public int MaterialsStockpile => ColonyManager.Instance != null ? ColonyManager.Instance.MaterialsStockpile : 0;
        public int CurrencyStockpile => ColonyManager.Instance != null ? ColonyManager.Instance.CurrencyStockpile : 0;

        // Legacy compatibility
        public ZoneSO CurrentZone => null;
        public int CurrentStageIndex => currentLevel - 1;
        public int CurrentStepIndex => nodesVisited;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log("[RunManager] Awake: duplicate instance — destroying self");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            // Auto-load level configs if not set
            if (levelConfigs == null || levelConfigs.Length == 0)
            {
                var allConfigs = new List<MapConfigSO>();
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MapConfigSO"))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var config = UnityEditor.AssetDatabase.LoadAssetAtPath<MapConfigSO>(path);
                    if (config != null) allConfigs.Add(config);
                }
                allConfigs.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
                levelConfigs = allConfigs.ToArray();
                Debug.Log($"[RunManager] Awake: auto-loaded {levelConfigs.Length} level configs from AssetDatabase");
            }

            // Auto-load colony deck if empty
            if (colonyDeck == null || colonyDeck.Count == 0)
            {
                colonyDeck = new List<ColonyCardDefinitionSO>();
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ColonyCardDefinitionSO"))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var card = UnityEditor.AssetDatabase.LoadAssetAtPath<ColonyCardDefinitionSO>(path);
                    if (card != null) colonyDeck.Add(card);
                }
                Debug.Log($"[RunManager] Awake: auto-loaded {colonyDeck.Count} colony cards from AssetDatabase");
            }

            // Auto-load hero deck if empty (exclude old Resource-type cards)
            if (heroDeck == null || heroDeck.Count == 0)
            {
                heroDeck = new List<CardDefinitionSO>();
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CardDefinitionSO"))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                    if (card != null && card.cardType != CardType.Resource)
                        heroDeck.Add(card);
                }
                Debug.Log($"[RunManager] Awake: auto-loaded {heroDeck.Count} hero deck cards from AssetDatabase (excluded Resource-type cards)");
            }
#endif

            SceneManager.sceneLoaded += OnSceneLoaded;
            ServiceLocator.Register<IRunManager>(this);

            Debug.Log($"[RunManager] Awake: levelConfigs={levelConfigs?.Length ?? 0}, colonyDeck={colonyDeck?.Count ?? 0}, heroDeck={heroDeck?.Count ?? 0}");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Debug.Log("[RunManager] OnDestroy: clearing singleton, unsubscribing from SceneManager");
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _instance = null;
            }
        }

        private void OnEnable()
        {
            Debug.Log("[RunManager] OnEnable: subscribing to events");
            EventBus.OnColonyDraftComplete += OnColonyDraftComplete;
            EventBus.OnColonyManagementComplete += OnColonyManagementComplete;
            EventBus.OnMapNodeSelected += OnMapNodeSelected;
            EventBus.OnMapNodeComplete += OnMapNodeComplete;
            EventBus.OnLevelComplete += OnLevelComplete;
            EventBus.OnEncounterComplete += OnEncounterComplete;
            EventBus.OnHeroDeckReady += OnHeroDeckReady;
            EventBus.OnShopComplete += OnNodeHandlerComplete;
            EventBus.OnHealingComplete += OnNodeHandlerComplete;
            EventBus.OnUpgradeComplete += OnNodeHandlerComplete;
            EventBus.OnDraftComplete += OnNodeHandlerComplete;
            EventBus.OnEventComplete += OnNodeHandlerComplete;
            EventBus.OnRestComplete += OnNodeHandlerComplete;
            EventBus.OnBossDefeated += OnBossDefeated;
            EventBus.OnCardPurchased += OnCardPurchased;
            EventBus.OnCardDrafted += OnCardDrafted;
            EventBus.OnCardRemoved += OnCardRemoved;
            EventBus.OnEventWoundHero += OnEventWoundHero;
            EventBus.OnReturnToMainMenu += OnReturnToMainMenu;
        }

        private void OnDisable()
        {
            Debug.Log("[RunManager] OnDisable: unsubscribing from events");
            EventBus.OnColonyDraftComplete -= OnColonyDraftComplete;
            EventBus.OnColonyManagementComplete -= OnColonyManagementComplete;
            EventBus.OnMapNodeSelected -= OnMapNodeSelected;
            EventBus.OnMapNodeComplete -= OnMapNodeComplete;
            EventBus.OnLevelComplete -= OnLevelComplete;
            EventBus.OnEncounterComplete -= OnEncounterComplete;
            EventBus.OnHeroDeckReady -= OnHeroDeckReady;
            EventBus.OnShopComplete -= OnNodeHandlerComplete;
            EventBus.OnHealingComplete -= OnNodeHandlerComplete;
            EventBus.OnUpgradeComplete -= OnNodeHandlerComplete;
            EventBus.OnDraftComplete -= OnNodeHandlerComplete;
            EventBus.OnEventComplete -= OnNodeHandlerComplete;
            EventBus.OnRestComplete -= OnNodeHandlerComplete;
            EventBus.OnBossDefeated -= OnBossDefeated;
            EventBus.OnCardPurchased -= OnCardPurchased;
            EventBus.OnCardDrafted -= OnCardDrafted;
            EventBus.OnCardRemoved -= OnCardRemoved;
            EventBus.OnEventWoundHero -= OnEventWoundHero;
            EventBus.OnReturnToMainMenu -= OnReturnToMainMenu;
        }

        // --- Scene Management ---

        private void LoadGameScene(string sceneName)
        {
            Debug.Log($"[RunManager] LoadGameScene: loading '{sceneName}'");
            SceneManager.LoadScene(sceneName);
        }

        private void LoadEncounterScene()
        {
            Debug.Log("[RunManager] LoadEncounterScene: loading Encounter scene additively");
            SceneManager.LoadScene("Encounter", LoadSceneMode.Additive);
        }

        private void UnloadEncounterScene()
        {
            Debug.Log("[RunManager] UnloadEncounterScene: unloading Encounter scene");
            SceneManager.UnloadSceneAsync("Encounter");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!enabled)
            {
                Debug.Log($"[RunManager] OnSceneLoaded: SKIPPED (disabled) scene='{scene.name}', mode={mode}");
                return;
            }
            Debug.Log($"[RunManager] OnSceneLoaded: scene='{scene.name}', mode={mode}");

            switch (scene.name)
            {
                case "ColonyDraft":
                    // ColonyDraftUI self-initializes via OnRunStarted event
                    Debug.Log("[RunManager] OnSceneLoaded: ColonyDraft — waiting for player to complete draft");
                    break;

                case "ColonyManagement":
                    colonyBoardManager = FindAnyObjectByType<ColonyBoardManager>();
                    heroDeckSetAsideUI = FindAnyObjectByType<HeroDeckSetAsideUI>();
                    Debug.Log($"[RunManager] OnSceneLoaded: ColonyManagement — colonyBoard={colonyBoardManager != null}, setAsideUI={heroDeckSetAsideUI != null}");
                    if (colonyBoardManager != null && runState == RunState.ColonyManagement)
                    {
                        var config = levelConfigs[currentLevel - 1];
                        colonyBoardManager.StartColonyManagement(currentLevel, config, new List<ColonyCardDefinitionSO>(colonyDeck));
                        EventBus.OnLevelStarted?.Invoke(currentLevel);
                    }
                    break;

                case "MapTraversal":
                    DiscoverMapManagers();
                    if (mapManager != null && runState == RunState.MapTraversal)
                    {
                        var config = levelConfigs[currentLevel - 1];
                        mapManager.InitializeMap(config);
                    }
                    break;

                case "Encounter":
                    encounterManager = FindAnyObjectByType<EncounterManager>();
                    bossManager = FindAnyObjectByType<BossManager>();
                    gameManager = FindAnyObjectByType<GameManager>();
                    Debug.Log($"[RunManager] OnSceneLoaded: Encounter — encounter={encounterManager != null}, boss={bossManager != null}, game={gameManager != null}");
                    StartPendingEncounter();
                    break;

                case "RunResult":
                    Debug.Log("[RunManager] OnSceneLoaded: RunResult — screen self-initializes");
                    break;
            }
        }

        private void DiscoverMapManagers()
        {
            mapManager = FindAnyObjectByType<MapManager>();
            shopManager = FindAnyObjectByType<ShopManager>();
            healingManager = FindAnyObjectByType<HealingManager>();
            upgradeManager = FindAnyObjectByType<UpgradeManager>();
            draftManager = FindAnyObjectByType<DraftManager>();
            eventManager = FindAnyObjectByType<EventManager>();
            restManager = FindAnyObjectByType<RestManager>();
            Debug.Log($"[RunManager] DiscoverMapManagers: map={mapManager != null}, shop={shopManager != null}, " +
                      $"healing={healingManager != null}, upgrade={upgradeManager != null}, " +
                      $"draft={draftManager != null}, event={eventManager != null}, rest={restManager != null}");
        }

        private void StartPendingEncounter()
        {
            if (encounterManager == null)
            {
                Debug.LogError("[RunManager] StartPendingEncounter: no EncounterManager found in Encounter scene!");
                return;
            }

            if (pendingIsBoss)
            {
                if (pendingBossDef != null && bossManager != null)
                {
                    Debug.Log($"[RunManager] StartPendingEncounter: starting boss fight '{pendingBossDef.bossName}'");
                    if (pendingEncounterDef != null)
                    {
                        encounterManager.StartEncounter(pendingEncounterDef, new List<CardDefinitionSO>(heroDeck), colonyConfig, woundedHeroes, pendingDifficulty);
                    }
                    else
                    {
                        Debug.Log("[RunManager] StartPendingEncounter: no encounter layout — boss fight runs without board");
                        bossManager.StartBossFight(pendingBossDef, new List<HeroAgent>());
                    }
                }
                else
                {
                    Debug.LogWarning("[RunManager] StartPendingEncounter: no boss definition or BossManager — auto-completing level");
                    EventBus.OnLevelComplete?.Invoke();
                }
            }
            else if (pendingEncounterDef != null)
            {
                Debug.Log($"[RunManager] StartPendingEncounter: starting encounter '{pendingEncounterDef.encounterName}', difficulty={pendingDifficulty}");
                encounterManager.StartEncounter(pendingEncounterDef, new List<CardDefinitionSO>(heroDeck), colonyConfig, woundedHeroes, pendingDifficulty);
            }
            else
            {
                Debug.LogWarning("[RunManager] StartPendingEncounter: no pending encounter data — returning to map");
                UnloadEncounterScene();
                if (mapManager != null) mapManager.OnNodeComplete();
            }

            // Clear pending state
            pendingEncounterDef = null;
            pendingBossDef = null;
        }

        // --- Run Lifecycle ---

        public void StartRun()
        {
            int seed = System.Environment.TickCount;
            SeededRandom.Initialize(seed);
            Debug.Log($"[RunManager] StartRun: initializing run state (seed={seed})");
            runState = RunState.Draft;
            currentLevel = 1;
            encountersCompleted = 0;
            totalResourcesGathered = 0;
            enemiesDefeated = 0;
            bossesKilled = 0;
            nodesVisited = 0;
            woundedHeroes.Clear();
            exhaustedHeroes.Clear();
            enemiesEncountered.Clear();
            eventsEncountered.Clear();
            bossesEncountered.Clear();

            var colMgr = ColonyManager.Instance;
            if (colMgr != null) colMgr.InitializeHP();

            // Clear relics for new run
            var relicMgr = RelicManager.Instance;
            if (relicMgr != null) relicMgr.ClearRelics();

            // Track no-starvation for achievements
            var achMgr = AchievementManager.Instance;
            if (achMgr != null) achMgr.OnRunStarted();

            EventBus.OnRunStarted?.Invoke();

            Debug.Log("[RunManager] StartRun: loading ColonyDraft scene");
            LoadGameScene("ColonyDraft");
        }

        public void ContinueRun()
        {
            Debug.Log("[RunManager] ContinueRun: loading saved run state");
            var save = SaveManager.Load();
            if (save == null)
            {
                Debug.LogWarning("[RunManager] ContinueRun: no save data found — starting new run instead");
                StartRun();
                return;
            }

            // Restore seeded random state
            SeededRandom.Initialize(save.randomSeed);
            Debug.Log($"[RunManager] ContinueRun: restored seed={save.randomSeed}");

            // Restore run state
            currentLevel = save.currentLevel;
            runState = (RunState)save.runState;
            nodesVisited = save.nodesVisited;
            encountersCompleted = save.encountersCompleted;
            totalResourcesGathered = save.totalResourcesGathered;
            enemiesDefeated = save.enemiesDefeated;
            bossesKilled = save.bossesKilled;
            Debug.Log($"[RunManager] ContinueRun: restored runState={runState}, level={currentLevel}, nodes={nodesVisited}, encounters={encountersCompleted}");

            // Restore colony state
            var colMgr = ColonyManager.Instance;
            if (colMgr != null)
            {
                colMgr.RestoreState(save.colonyHP, save.colonyMaxHP, save.currencyStockpile, save.foodStockpile, save.materialsStockpile);
                Debug.Log($"[RunManager] ContinueRun: restored colony HP={save.colonyHP}/{save.colonyMaxHP}, food={save.foodStockpile}, materials={save.materialsStockpile}, currency={save.currencyStockpile}");
            }

            // Restore hero deck from saved card names
            heroDeck.Clear();
            woundedHeroes.Clear();
            exhaustedHeroes.Clear();
#if UNITY_EDITOR
            var allHeroCards = new Dictionary<string, CardDefinitionSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CardDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardDefinitionSO>(path);
                if (card != null && !allHeroCards.ContainsKey(card.cardName))
                    allHeroCards[card.cardName] = card;
            }

            foreach (var name in save.heroDeckCardNames)
            {
                if (allHeroCards.TryGetValue(name, out var card))
                {
                    heroDeck.Add(card);
                }
                else
                {
                    Debug.LogWarning($"[RunManager] ContinueRun: could not find hero card '{name}' — skipping");
                }
            }
            Debug.Log($"[RunManager] ContinueRun: restored {heroDeck.Count} hero cards from {save.heroDeckCardNames.Count} saved names");

            // Restore wounded heroes
            foreach (var name in save.woundedHeroNames)
            {
                if (allHeroCards.TryGetValue(name, out var card))
                {
                    woundedHeroes.Add(card);
                    Debug.Log($"[RunManager] ContinueRun: restored wounded hero '{name}'");
                }
            }

            // Restore exhausted heroes
            foreach (var name in save.exhaustedHeroNames)
            {
                if (allHeroCards.TryGetValue(name, out var card))
                {
                    exhaustedHeroes.Add(card);
                    Debug.Log($"[RunManager] ContinueRun: restored exhausted hero '{name}'");
                }
            }

            // Restore colony deck
            colonyDeck.Clear();
            var allColonyCards = new Dictionary<string, ColonyCardDefinitionSO>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ColonyCardDefinitionSO"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var card = UnityEditor.AssetDatabase.LoadAssetAtPath<ColonyCardDefinitionSO>(path);
                if (card != null && !allColonyCards.ContainsKey(card.cardName))
                    allColonyCards[card.cardName] = card;
            }
            foreach (var name in save.colonyDeckCardNames)
            {
                if (allColonyCards.TryGetValue(name, out var card))
                {
                    colonyDeck.Add(card);
                }
                else
                {
                    Debug.LogWarning($"[RunManager] ContinueRun: could not find colony card '{name}' — skipping");
                }
            }
            Debug.Log($"[RunManager] ContinueRun: restored {colonyDeck.Count} colony cards");
#else
            // Runtime: load from Resources
            var heroCardPool = Resources.LoadAll<CardDefinitionSO>("");
            var heroLookup = new Dictionary<string, CardDefinitionSO>();
            foreach (var c in heroCardPool)
            {
                if (c != null && !heroLookup.ContainsKey(c.cardName))
                    heroLookup[c.cardName] = c;
            }
            foreach (var name in save.heroDeckCardNames)
            {
                if (heroLookup.TryGetValue(name, out var card))
                    heroDeck.Add(card);
                else
                    Debug.LogWarning($"[RunManager] ContinueRun: could not find hero card '{name}' — skipping");
            }
            foreach (var name in save.woundedHeroNames)
            {
                if (heroLookup.TryGetValue(name, out var card))
                    woundedHeroes.Add(card);
            }
            foreach (var name in save.exhaustedHeroNames)
            {
                if (heroLookup.TryGetValue(name, out var card))
                    exhaustedHeroes.Add(card);
            }
            Debug.Log($"[RunManager] ContinueRun: restored {heroDeck.Count} hero cards (runtime)");

            var colonyCardPool = Resources.LoadAll<ColonyCardDefinitionSO>("");
            var colonyLookup = new Dictionary<string, ColonyCardDefinitionSO>();
            foreach (var c in colonyCardPool)
            {
                if (c != null && !colonyLookup.ContainsKey(c.cardName))
                    colonyLookup[c.cardName] = c;
            }
            colonyDeck.Clear();
            foreach (var name in save.colonyDeckCardNames)
            {
                if (colonyLookup.TryGetValue(name, out var card))
                    colonyDeck.Add(card);
                else
                    Debug.LogWarning($"[RunManager] ContinueRun: could not find colony card '{name}' — skipping");
            }
            Debug.Log($"[RunManager] ContinueRun: restored {colonyDeck.Count} colony cards (runtime)");
#endif

            // Restore colony config
            if (save.colonyConfig != null)
            {
                colonyConfig = new ColonyConfig
                {
                    maxHeroDeckSize = save.colonyConfig.maxHeroDeckSize,
                    foodConsumptionPerNode = save.colonyConfig.foodConsumptionPerNode,
                    heroCombatBonus = save.colonyConfig.heroCombatBonus,
                    heroMoveBonus = save.colonyConfig.heroMoveBonus,
                    heroCarryBonus = save.colonyConfig.heroCarryBonus,
                    totalPopulation = save.colonyConfig.totalPopulation,
                    bonusStartingFood = save.colonyConfig.bonusStartingFood
                };
                Debug.Log($"[RunManager] ContinueRun: restored colonyConfig (maxDeck={colonyConfig.maxHeroDeckSize}, foodPerNode={colonyConfig.foodConsumptionPerNode})");
            }

            // Restore relics
            var relicMgr = RelicManager.Instance;
            if (relicMgr != null && save.activeRelicNames != null && save.activeRelicNames.Count > 0)
            {
                relicMgr.RestoreRelics(save.activeRelicNames);
                Debug.Log($"[RunManager] ContinueRun: restored {save.activeRelicNames.Count} relics");
            }

            // Determine which scene to load based on saved state
            string targetScene = save.currentSceneName;
            if (string.IsNullOrEmpty(targetScene))
            {
                // Fallback: infer from runState
                switch (runState)
                {
                    case RunState.Draft:
                        targetScene = "ColonyDraft";
                        break;
                    case RunState.ColonyManagement:
                        targetScene = "ColonyManagement";
                        break;
                    case RunState.MapTraversal:
                        targetScene = "MapTraversal";
                        break;
                    case RunState.InEncounter:
                    case RunState.InBoss:
                        targetScene = "MapTraversal"; // Can't restore mid-encounter, go back to map
                        runState = RunState.MapTraversal;
                        break;
                    default:
                        targetScene = "MapTraversal";
                        runState = RunState.MapTraversal;
                        break;
                }
                Debug.Log($"[RunManager] ContinueRun: no saved scene name — inferred '{targetScene}' from runState={runState}");
            }

            Debug.Log($"[RunManager] ContinueRun: loading scene '{targetScene}'");
            LoadGameScene(targetScene);
        }

        private void OnColonyDraftComplete(List<ColonyCardDefinitionSO> draftedDeck)
        {
            Debug.Log($"[RunManager] OnColonyDraftComplete: received {draftedDeck.Count} colony cards, starting level 1");
            colonyDeck = new List<ColonyCardDefinitionSO>(draftedDeck);
            StartLevel(currentLevel);
        }

        private void StartLevel(int level)
        {
            currentLevel = level;
            Debug.Log($"[RunManager] StartLevel: level={level}");

            if (levelConfigs == null || level - 1 >= levelConfigs.Length || levelConfigs[level - 1] == null)
            {
                Debug.LogError($"[RunManager] StartLevel: no MapConfigSO for level {level}!");
                return;
            }

            var config = levelConfigs[level - 1];
            Debug.Log($"[RunManager] StartLevel: config='{config.levelName}', boardSize={config.boardSize}, colonyBoardSize={config.colonyBoardSize}");

            // Steam Rich Presence
            Steam.SteamManager.SetRichPresenceStatus($"Level {level}: {config.levelName}");

            // Restore exhausted heroes at level start
            if (exhaustedHeroes.Count > 0)
            {
                Debug.Log($"[RunManager] StartLevel: restoring {exhaustedHeroes.Count} exhausted heroes");
                exhaustedHeroes.Clear();
            }

            // Clear wounds at level start
            woundedHeroes.Clear();

            // Load colony management scene (ColonyBoardManager.StartColonyManagement called in OnSceneLoaded)
            runState = RunState.ColonyManagement;
            LoadGameScene("ColonyManagement");
        }

        private void OnColonyManagementComplete(ColonyConfig config)
        {
            colonyConfig = config;
            Debug.Log($"[RunManager] OnColonyManagementComplete: {config}");

            var colMgr = ColonyManager.Instance;

            // Apply bonus starting food
            if (config.bonusStartingFood > 0 && colMgr != null)
            {
                colMgr.AddFood(config.bonusStartingFood);
                Debug.Log($"[RunManager] OnColonyManagementComplete: added {config.bonusStartingFood} bonus starting food");
            }

            // Check if hero deck exceeds max — need set-aside
            int maxDeckSize = config.maxHeroDeckSize;
            int currentDeckSize = CountHeroCards();
            Debug.Log($"[RunManager] OnColonyManagementComplete: heroDeck heroes={currentDeckSize}, maxDeckSize={maxDeckSize}");

            if (currentDeckSize > maxDeckSize)
            {
                Debug.Log($"[RunManager] OnColonyManagementComplete: deck exceeds max — need to set aside {currentDeckSize - maxDeckSize} cards");

                if (heroDeckSetAsideUI != null)
                {
                    heroDeckSetAsideUI.Open(new List<CardDefinitionSO>(heroDeck), maxDeckSize, (trimmedDeck) =>
                    {
                        heroDeck = trimmedDeck;
                        Debug.Log($"[RunManager] OnColonyManagementComplete: set-aside complete, deck now {heroDeck.Count} cards");
                        EventBus.OnHeroDeckReady?.Invoke(new List<CardDefinitionSO>(heroDeck));
                    });
                    return;
                }
                else
                {
                    // Fallback: auto-trim lowest-stat heroes
                    int toRemove = currentDeckSize - maxDeckSize;
                    Debug.Log($"[RunManager] OnColonyManagementComplete: no SetAsideUI — auto-trimming {toRemove} hero cards");
                }
            }

            // Proceed to map
            EventBus.OnHeroDeckReady?.Invoke(new List<CardDefinitionSO>(heroDeck));
        }

        private void OnHeroDeckReady(List<CardDefinitionSO> deck)
        {
            Debug.Log($"[RunManager] OnHeroDeckReady: deck size={deck.Count}, loading MapTraversal scene");
            runState = RunState.MapTraversal;
            LoadGameScene("MapTraversal");
            // mapManager.InitializeMap called in OnSceneLoaded
        }

        // --- Map Node Handling ---

        private void OnMapNodeSelected(MapNode node)
        {
            nodesVisited++;
            lastNodeType = node.nodeType;
            Debug.Log($"[RunManager] OnMapNodeSelected: {node}, totalNodesVisited={nodesVisited}");

            // Steam Rich Presence — show current activity
            Steam.SteamManager.SetRichPresenceStatus($"Level {currentLevel} — {node.nodeType}");

            // Consume food
            ConsumeFood();

            var colMgr = ColonyManager.Instance;

            // Check colony death
            if (colMgr != null && !colMgr.IsAlive)
            {
                Debug.Log("[RunManager] OnMapNodeSelected: colony died from starvation — run failed");
                RunFailed();
                return;
            }

            // Route to node handler
            switch (node.nodeType)
            {
                case NodeType.ResourceEncounter:
                case NodeType.EliteEncounter:
                    runState = RunState.InEncounter;
                    PrepareEncounter(node, false);
                    break;

                case NodeType.Boss:
                    runState = RunState.InBoss;
                    PrepareEncounter(node, true);
                    break;

                case NodeType.Shop:
                    Debug.Log("[RunManager] OnMapNodeSelected: Shop — opening shop");
                    if (shopManager != null)
                    {
                        var config = levelConfigs[currentLevel - 1];
                        shopManager.OpenShop(config.shopCardPool);
                    }
                    else
                    {
                        Debug.LogWarning("[RunManager] OnMapNodeSelected: no ShopManager — auto-completing");
                        EventBus.OnShopComplete?.Invoke();
                    }
                    break;

                case NodeType.HealingShrine:
                    Debug.Log("[RunManager] OnMapNodeSelected: HealingShrine — opening healing");
                    if (healingManager != null)
                    {
                        healingManager.OpenHealing(woundedHeroes);
                    }
                    else
                    {
                        Debug.LogWarning("[RunManager] OnMapNodeSelected: no HealingManager — auto-completing");
                        EventBus.OnHealingComplete?.Invoke();
                    }
                    break;

                case NodeType.UpgradeShrine:
                    Debug.Log("[RunManager] OnMapNodeSelected: UpgradeShrine — opening upgrade");
                    if (upgradeManager != null)
                    {
                        upgradeManager.OpenUpgrade(heroDeck, colonyDeck);
                    }
                    else
                    {
                        Debug.LogWarning("[RunManager] OnMapNodeSelected: no UpgradeManager — auto-completing");
                        EventBus.OnUpgradeComplete?.Invoke();
                    }
                    break;

                case NodeType.CardDraft:
                    Debug.Log("[RunManager] OnMapNodeSelected: CardDraft — opening draft");
                    if (draftManager != null)
                    {
                        var draftConfig = levelConfigs[currentLevel - 1];
                        draftManager.OpenDraft(draftConfig.shopCardPool, heroDeck);
                    }
                    else
                    {
                        Debug.LogWarning("[RunManager] OnMapNodeSelected: no DraftManager — auto-completing");
                        EventBus.OnDraftComplete?.Invoke();
                    }
                    break;

                case NodeType.Event:
                    Debug.Log("[RunManager] OnMapNodeSelected: Event — opening event");
                    if (eventManager != null)
                    {
                        var evtConfig = levelConfigs[currentLevel - 1];
                        if (evtConfig.eventPool != null && evtConfig.eventPool.Count > 0)
                        {
                            var randomEvent = evtConfig.eventPool[SeededRandom.Range(0, evtConfig.eventPool.Count)];
                            Debug.Log($"[RunManager] OnMapNodeSelected: selected event '{randomEvent.eventName}'");
                            eventManager.OpenEvent(randomEvent);
                            if (!eventsEncountered.Contains(randomEvent.eventName))
                                eventsEncountered.Add(randomEvent.eventName);
                        }
                        else
                        {
                            Debug.LogWarning("[RunManager] OnMapNodeSelected: empty event pool — auto-completing");
                            EventBus.OnEventComplete?.Invoke();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[RunManager] OnMapNodeSelected: no EventManager — auto-completing");
                        EventBus.OnEventComplete?.Invoke();
                    }
                    break;

                case NodeType.RestSite:
                    Debug.Log("[RunManager] OnMapNodeSelected: RestSite — opening rest site");
                    if (restManager != null)
                    {
                        restManager.OpenRestSite(heroDeck, colonyDeck);
                    }
                    else
                    {
                        var colMgr2 = ColonyManager.Instance;
                        Debug.LogWarning("[RunManager] OnMapNodeSelected: no RestManager — healing 30% and completing");
                        if (colMgr2 != null)
                        {
                            int healAmount = Mathf.CeilToInt(colMgr2.MaxHP * 0.3f);
                            colMgr2.Heal(healAmount);
                        }
                        EventBus.OnRestComplete?.Invoke();
                    }
                    break;

                default:
                    Debug.LogWarning($"[RunManager] OnMapNodeSelected: unknown node type {node.nodeType}");
                    if (mapManager != null) mapManager.OnNodeComplete();
                    break;
            }
        }

        private void PrepareEncounter(MapNode node, bool isBoss)
        {
            pendingIsBoss = isBoss;
            pendingDifficulty = node.difficulty;
            pendingNodeType = node.nodeType;

            if (isBoss)
            {
                var config = levelConfigs[currentLevel - 1];
                pendingBossDef = config.bossDefinition;
                pendingEncounterDef = node.encounterDefinition;
                if (pendingBossDef != null && !bossesEncountered.Contains(pendingBossDef.bossName))
                    bossesEncountered.Add(pendingBossDef.bossName);
                Debug.Log($"[RunManager] PrepareEncounter: boss='{pendingBossDef?.bossName}', hasEncounterLayout={pendingEncounterDef != null}");
            }
            else
            {
                pendingEncounterDef = node.encounterDefinition;
                pendingBossDef = null;

                // Track enemy discoveries for meta-progression
                if (pendingEncounterDef != null && pendingEncounterDef.enemySpawns != null)
                {
                    foreach (var spawn in pendingEncounterDef.enemySpawns)
                    {
                        if (spawn.enemyDefinition != null && !enemiesEncountered.Contains(spawn.enemyDefinition.name))
                            enemiesEncountered.Add(spawn.enemyDefinition.name);
                    }
                }
                Debug.Log($"[RunManager] PrepareEncounter: encounter='{pendingEncounterDef?.encounterName}', difficulty={pendingDifficulty}");
            }

            // Load encounter scene additively (MapTraversal stays loaded)
            LoadEncounterScene();
        }

        // --- Encounter Results ---

        private void OnEncounterComplete(EncounterResult result)
        {
            Debug.Log($"[RunManager] OnEncounterComplete: {result}");
            encountersCompleted++;

            var colMgr = ColonyManager.Instance;

            // Apply resources to stockpile
            if (result.success && colMgr != null)
            {
                foreach (var kvp in result.resourcesGathered)
                {
                    switch (kvp.Key)
                    {
                        case ResourceType.Food:
                            colMgr.AddFood(kvp.Value);
                            break;
                        case ResourceType.Materials:
                            colMgr.AddMaterials(kvp.Value);
                            break;
                        case ResourceType.Currency:
                            colMgr.AddCurrency(kvp.Value);
                            break;
                    }
                    totalResourcesGathered += kvp.Value;
                }

                // Elite encounter bonus currency
                if (lastNodeType == NodeType.EliteEncounter)
                {
                    var bc = BalanceConfigSO.Instance;
                    int bonus = bc != null ? bc.eliteBonusCurrency : 3;
                    colMgr.AddCurrency(bonus);
                    Debug.Log($"[RunManager] OnEncounterComplete: elite bonus currency +{bonus}");
                }
            }

            // Track wounds
            foreach (var hero in result.woundedHeroes)
            {
                woundedHeroes.Add(hero);
                Debug.Log($"[RunManager] OnEncounterComplete: hero '{hero.cardName}' added to wounded set");
            }

            foreach (var hero in result.exhaustedHeroes)
            {
                exhaustedHeroes.Add(hero);
                woundedHeroes.Remove(hero);
                Debug.Log($"[RunManager] OnEncounterComplete: hero '{hero.cardName}' exhausted — removed from deck this level");
            }

            // Check colony death from encounter
            if (colMgr != null && !colMgr.IsAlive)
            {
                Debug.Log("[RunManager] OnEncounterComplete: colony died — run failed");
                RunFailed();
                return;
            }

            runState = RunState.MapTraversal;
            SaveRunState();

            // Unload encounter scene and return to map
            UnloadEncounterScene();
            if (mapManager != null)
            {
                mapManager.OnNodeComplete();
            }
            else
            {
                Debug.LogWarning("[RunManager] OnEncounterComplete: mapManager is null — cannot advance map");
            }
        }

        private void OnNodeHandlerComplete()
        {
            Debug.Log("[RunManager] OnNodeHandlerComplete: non-combat node done, returning to map");
            runState = RunState.MapTraversal;
            SaveRunState();
            if (mapManager != null) mapManager.OnNodeComplete();
        }

        private void OnLevelComplete()
        {
            Debug.Log($"[RunManager] OnLevelComplete: level {currentLevel} complete!");
            bossesKilled++;

            if (currentLevel >= levelConfigs.Length)
            {
                Debug.Log("[RunManager] OnLevelComplete: final level — run complete!");
                RunComplete();
            }
            else
            {
                Debug.Log($"[RunManager] OnLevelComplete: advancing to level {currentLevel + 1}");
                runState = RunState.LevelComplete;
                EventBus.OnLevelAdvanced?.Invoke(currentLevel + 1);
                StartLevel(currentLevel + 1);
            }
        }

        private void OnMapNodeComplete()
        {
            // Heal heroes who sat out one encounter
            // Note: wound healing happens per-encounter — heroes sit out ONE encounter then auto-heal
            // This is tracked by the encounter system checking woundedHeroes set
        }

        private void OnBossDefeated()
        {
            Debug.Log("[RunManager] OnBossDefeated: boss defeated — completing encounter as success");
            var result = new EncounterResult
            {
                success = true,
                recalled = false,
                rewardCards = bossManager != null ? bossManager.GetRewardCards() : null,
                rewardRelic = bossManager != null ? bossManager.GetRewardRelic() : null
            };

            // If there are reward cards, show reward selection
            if (result.rewardCards != null && result.rewardCards.Count > 0)
            {
                Debug.Log($"[RunManager] OnBossDefeated: {result.rewardCards.Count} reward cards available");
                // Reward selection is handled by RewardSelectionUI subscribing to OnBossDefeated
            }

            OnEncounterComplete(result);
        }

        // --- Card Management ---

        private void OnCardPurchased(CardDefinitionSO card)
        {
            Debug.Log($"[RunManager] OnCardPurchased: adding '{card.cardName}' to hero deck (deckSize={heroDeck.Count} -> {heroDeck.Count + 1})");
            heroDeck.Add(card);
        }

        private void OnCardDrafted(CardDefinitionSO card)
        {
            Debug.Log($"[RunManager] OnCardDrafted: adding '{card.cardName}' to hero deck (deckSize={heroDeck.Count} -> {heroDeck.Count + 1})");
            heroDeck.Add(card);
        }

        private void OnCardRemoved(CardDefinitionSO card)
        {
            bool removed = heroDeck.Remove(card);
            Debug.Log($"[RunManager] OnCardRemoved: '{card.cardName}' removed={removed} (deckSize={heroDeck.Count})");
        }

        private void OnEventWoundHero()
        {
            // Wound a random non-wounded hero
            var candidates = new List<CardDefinitionSO>();
            foreach (var card in heroDeck)
            {
                if (card.cardType == CardType.Hero && !woundedHeroes.Contains(card) && !exhaustedHeroes.Contains(card))
                    candidates.Add(card);
            }

            if (candidates.Count > 0)
            {
                var victim = candidates[SeededRandom.Range(0, candidates.Count)];
                woundedHeroes.Add(victim);
                Debug.Log($"[RunManager] OnEventWoundHero: wounded '{victim.cardName}' (totalWounded={woundedHeroes.Count})");
            }
            else
            {
                Debug.Log("[RunManager] OnEventWoundHero: no eligible heroes to wound");
            }
        }

        // --- Food Consumption ---

        private void ConsumeFood()
        {
            if (colonyConfig == null) return;

            var colMgr = ColonyManager.Instance;
            if (colMgr == null) return;

            int consumption = colonyConfig.foodConsumptionPerNode;
            int available = colMgr.FoodStockpile;
            int shortfall = Mathf.Max(0, consumption - available);

            Debug.Log($"[RunManager] ConsumeFood: consumption={consumption}, available={available}, shortfall={shortfall}");

            int toSpend = Mathf.Min(consumption, available);
            if (toSpend > 0)
                colMgr.SpendFood(toSpend);

            EventBus.OnFoodConsumed?.Invoke(colMgr.FoodStockpile);

            if (shortfall > 0)
            {
                var bc = BalanceConfigSO.Instance;
                int dmgPerFood = bc != null ? bc.starvationDamagePerFood : 2;
                int damage = shortfall * dmgPerFood;
                Debug.Log($"[RunManager] ConsumeFood: STARVATION — shortfall={shortfall}, dmgPerFood={dmgPerFood}, damage={damage}");
                colMgr.TakeDamage(damage);
                EventBus.OnStarvationDamage?.Invoke(damage);

                string msg = $"Starvation! -{damage} Colony HP";
                EventBus.OnGatheringNotification?.Invoke(msg, new Color(1f, 0.2f, 0.2f));
            }
        }

        private int CountHeroCards()
        {
            int count = 0;
            foreach (var card in heroDeck)
            {
                if (card.cardType == CardType.Hero && !exhaustedHeroes.Contains(card))
                    count++;
            }
            return count;
        }

        // --- Navigation ---

        private void OnReturnToMainMenu()
        {
            Debug.Log("[RunManager] OnReturnToMainMenu: returning to main menu");
            LoadGameScene("MainMenu");
        }

        // --- Run End ---

        private void RunComplete()
        {
            runState = RunState.RunComplete;
            Debug.Log($"[RunManager] RunComplete: Victory! encounters={encountersCompleted}, resources={totalResourcesGathered}, bosses={bossesKilled}, nodes={nodesVisited}");
            SaveManager.DeleteSave();
            Steam.SteamManager.SetRichPresenceStatus("Victory!");

            // Process meta-progression
            var metaProg = MetaProgressionManager.Instance;
            if (metaProg != null)
            {
                metaProg.ProcessRunEnd(true, currentLevel, totalResourcesGathered,
                    bossesKilled, nodesVisited, heroDeck, enemiesEncountered, eventsEncountered, bossesEncountered);
            }

            EventBus.OnRunComplete_M1?.Invoke(true);
            EventBus.OnRunComplete?.Invoke();

            string msg = "Victory! The Pack survives!";
            EventBus.OnGatheringNotification?.Invoke(msg, new Color(1f, 0.9f, 0.3f));

            LoadGameScene("RunResult");
        }

        private void RunFailed()
        {
            runState = RunState.GameOver;
            Debug.Log($"[RunManager] RunFailed: Defeat. encounters={encountersCompleted}, resources={totalResourcesGathered}, nodes={nodesVisited}");
            SaveManager.DeleteSave();
            Steam.SteamManager.SetRichPresenceStatus("Defeated...");

            // Process meta-progression
            var metaProg = MetaProgressionManager.Instance;
            if (metaProg != null)
            {
                metaProg.ProcessRunEnd(false, currentLevel, totalResourcesGathered,
                    bossesKilled, nodesVisited, heroDeck, enemiesEncountered, eventsEncountered, bossesEncountered);
            }

            EventBus.OnRunFailed_M1?.Invoke();
            EventBus.OnRunFailed?.Invoke();

            string msg = "The Colony Has Fallen...";
            EventBus.OnGatheringNotification?.Invoke(msg, new Color(1f, 0.2f, 0.2f));

            LoadGameScene("RunResult");
        }

        // --- Save/Load ---

        private void SaveRunState()
        {
            var colMgr = ColonyManager.Instance;
            if (colMgr == null) return;

            var save = new RunSaveData
            {
                currentLevel = currentLevel,
                runState = (int)runState,
                nodesVisited = nodesVisited,
                currentSceneName = SceneManager.GetActiveScene().name,
                colonyHP = colMgr.CurrentHP,
                colonyMaxHP = colMgr.MaxHP,
                foodStockpile = colMgr.FoodStockpile,
                materialsStockpile = colMgr.MaterialsStockpile,
                currencyStockpile = colMgr.CurrencyStockpile,
                encountersCompleted = encountersCompleted,
                totalResourcesGathered = totalResourcesGathered,
                enemiesDefeated = enemiesDefeated,
                bossesKilled = bossesKilled,
                randomSeed = SeededRandom.CurrentSeed
            };

            // Hero deck
            foreach (var card in heroDeck)
            {
                if (card != null)
                    save.heroDeckCardNames.Add(card.cardName);
            }

            // Colony deck
            foreach (var card in colonyDeck)
            {
                if (card != null)
                    save.colonyDeckCardNames.Add(card.cardName);
            }

            // Wounds
            foreach (var hero in woundedHeroes)
            {
                if (hero != null)
                    save.woundedHeroNames.Add(hero.cardName);
            }
            foreach (var hero in exhaustedHeroes)
            {
                if (hero != null)
                    save.exhaustedHeroNames.Add(hero.cardName);
            }

            // Relics
            var relicMgr = RelicManager.Instance;
            if (relicMgr != null)
                save.activeRelicNames = relicMgr.GetRelicNames();

            // Map state
            if (mapManager != null && mapManager.Map != null)
            {
                save.mapCurrentRow = mapManager.CurrentRow;
                save.mapCurrentCol = mapManager.CurrentNode != null ? mapManager.CurrentNode.position.y : -1;

                foreach (var row in mapManager.Map)
                {
                    foreach (var node in row)
                    {
                        var nodeSave = new MapNodeSaveData
                        {
                            row = node.position.x,
                            col = node.position.y,
                            nodeType = (int)node.nodeType,
                            visited = node.visited,
                            difficulty = node.difficulty,
                            encounterName = node.encounterDefinition != null ? node.encounterDefinition.encounterName : "",
                            connectedIndices = new System.Collections.Generic.List<int>(node.connectedNodeIndices)
                        };
                        save.mapNodes.Add(nodeSave);
                    }
                }
            }

            // Colony config
            if (colonyConfig != null)
            {
                save.colonyConfig = new ColonyConfigSaveData
                {
                    maxHeroDeckSize = colonyConfig.maxHeroDeckSize,
                    foodConsumptionPerNode = colonyConfig.foodConsumptionPerNode,
                    heroCombatBonus = colonyConfig.heroCombatBonus,
                    heroMoveBonus = colonyConfig.heroMoveBonus,
                    heroCarryBonus = colonyConfig.heroCarryBonus,
                    totalPopulation = colonyConfig.totalPopulation,
                    bonusStartingFood = colonyConfig.bonusStartingFood
                };
            }

            SaveManager.Save(save);
        }
    }
}
