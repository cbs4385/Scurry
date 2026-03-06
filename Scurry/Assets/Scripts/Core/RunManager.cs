using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Colony;
using Scurry.Map;
using Scurry.Encounter;
using Scurry.Gathering;
using Scurry.UI;

namespace Scurry.Core
{
    public class RunManager : MonoBehaviour
    {
        [Header("Level Configs")]
        [SerializeField] private MapConfigSO[] levelConfigs; // Index 0 = Level 1, etc.

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ColonyManager colonyManager;
        [SerializeField] private ColonyBoardManager colonyBoardManager;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private EncounterManager encounterManager;
        [SerializeField] private BossManager bossManager;
        [SerializeField] private ShopManager shopManager;
        [SerializeField] private HealingManager healingManager;
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private DraftManager draftManager;
        [SerializeField] private EventManager eventManager;
        [SerializeField] private RestManager restManager;
        [SerializeField] private MetaProgressionManager metaProgression;
        [SerializeField] private HeroDeckSetAsideUI heroDeckSetAsideUI;

        [Header("Colony Deck")]
        [SerializeField] private List<ColonyCardDefinitionSO> colonyDeck = new List<ColonyCardDefinitionSO>();

        [Header("Hero Deck")]
        [SerializeField] private List<CardDefinitionSO> heroDeck = new List<CardDefinitionSO>();

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

        // Public accessors
        public RunState CurrentRunState => runState;
        public int CurrentLevel => currentLevel;
        public ColonyConfig ActiveColonyConfig => colonyConfig;
        public int FoodStockpile => colonyManager != null ? colonyManager.FoodStockpile : 0;
        public int MaterialsStockpile => colonyManager != null ? colonyManager.MaterialsStockpile : 0;
        public int CurrencyStockpile => colonyManager != null ? colonyManager.CurrencyStockpile : 0;

        // Legacy compatibility
        public ZoneSO CurrentZone => null;
        public int CurrentStageIndex => currentLevel - 1;
        public int CurrentStepIndex => nodesVisited;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
            if (colonyManager == null) colonyManager = FindObjectOfType<ColonyManager>();
            if (colonyBoardManager == null) colonyBoardManager = FindObjectOfType<ColonyBoardManager>();
            if (mapManager == null) mapManager = FindObjectOfType<MapManager>();
            if (encounterManager == null) encounterManager = FindObjectOfType<EncounterManager>();
            if (bossManager == null) bossManager = FindObjectOfType<BossManager>();
            if (shopManager == null) shopManager = FindObjectOfType<ShopManager>();
            if (healingManager == null) healingManager = FindObjectOfType<HealingManager>();
            if (upgradeManager == null) upgradeManager = FindObjectOfType<UpgradeManager>();
            if (draftManager == null) draftManager = FindObjectOfType<DraftManager>();
            if (eventManager == null) eventManager = FindObjectOfType<EventManager>();
            if (restManager == null) restManager = FindObjectOfType<RestManager>();
            if (metaProgression == null) metaProgression = FindObjectOfType<MetaProgressionManager>();
            if (heroDeckSetAsideUI == null) heroDeckSetAsideUI = FindObjectOfType<HeroDeckSetAsideUI>();

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

            Debug.Log($"[RunManager] Awake: levelConfigs={levelConfigs?.Length ?? 0}, colonyDeck={colonyDeck?.Count ?? 0}, heroDeck={heroDeck?.Count ?? 0}, " +
                      $"boss={bossManager != null}, shop={shopManager != null}, healing={healingManager != null}, upgrade={upgradeManager != null}, " +
                      $"draft={draftManager != null}, event={eventManager != null}, rest={restManager != null}");
        }

        private void OnEnable()
        {
            Debug.Log("[RunManager] OnEnable: subscribing to events");
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
        }

        private void OnDisable()
        {
            Debug.Log("[RunManager] OnDisable: unsubscribing from events");
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
        }

        private void Start()
        {
            if (gameManager != null && gameManager.StandaloneMode)
            {
                Debug.Log("[RunManager] Start: GameManager is in standalone mode — RunManager inactive");
                enabled = false;
                return;
            }

            Debug.Log("[RunManager] Start: starting new run");
            StartRun();
        }

        public void StartRun()
        {
            Debug.Log("[RunManager] StartRun: initializing run state");
            runState = RunState.ColonyManagement;
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

            colonyManager.InitializeHP();

            // Clear relics for new run
            var relicMgr = RelicManager.Instance;
            if (relicMgr != null) relicMgr.ClearRelics();

            // Track no-starvation for achievements
            var achMgr = AchievementManager.Instance;
            if (achMgr != null) achMgr.OnRunStarted();

            EventBus.OnRunStarted?.Invoke();
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

            EventBus.OnLevelStarted?.Invoke(level);

            // Start colony management phase
            runState = RunState.ColonyManagement;
            colonyBoardManager.StartColonyManagement(level, config, new List<ColonyCardDefinitionSO>(colonyDeck));
        }

        private void OnColonyManagementComplete(ColonyConfig config)
        {
            colonyConfig = config;
            Debug.Log($"[RunManager] OnColonyManagementComplete: {config}");

            // Apply bonus starting food
            if (config.bonusStartingFood > 0)
            {
                colonyManager.AddFood(config.bonusStartingFood);
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
            Debug.Log($"[RunManager] OnHeroDeckReady: deck size={deck.Count}, starting map traversal");
            runState = RunState.MapTraversal;

            var config = levelConfigs[currentLevel - 1];
            mapManager.InitializeMap(config);
        }

        private void OnMapNodeSelected(MapNode node)
        {
            nodesVisited++;
            lastNodeType = node.nodeType;
            Debug.Log($"[RunManager] OnMapNodeSelected: {node}, totalNodesVisited={nodesVisited}");

            // Consume food
            ConsumeFood();

            // Check colony death
            if (!colonyManager.IsAlive)
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
                    StartEncounterFromNode(node);
                    break;

                case NodeType.Boss:
                    runState = RunState.InBoss;
                    StartBossFromNode(node);
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
                            var randomEvent = evtConfig.eventPool[Random.Range(0, evtConfig.eventPool.Count)];
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
                        Debug.LogWarning("[RunManager] OnMapNodeSelected: no RestManager — healing 30% and completing");
                        int healAmount = Mathf.CeilToInt(colonyManager.MaxHP * 0.3f);
                        colonyManager.Heal(healAmount);
                        EventBus.OnRestComplete?.Invoke();
                    }
                    break;

                default:
                    Debug.LogWarning($"[RunManager] OnMapNodeSelected: unknown node type {node.nodeType}");
                    mapManager.OnNodeComplete();
                    break;
            }
        }

        private void StartEncounterFromNode(MapNode node)
        {
            if (node.encounterDefinition != null)
            {
                Debug.Log($"[RunManager] StartEncounterFromNode: starting encounter '{node.encounterDefinition.encounterName}', difficulty={node.difficulty}");

                // Track enemy discoveries for meta-progression
                if (node.encounterDefinition.enemySpawns != null)
                {
                    foreach (var spawn in node.encounterDefinition.enemySpawns)
                    {
                        if (spawn.enemyDefinition != null && !enemiesEncountered.Contains(spawn.enemyDefinition.name))
                            enemiesEncountered.Add(spawn.enemyDefinition.name);
                    }
                }

                encounterManager.StartEncounter(node.encounterDefinition, new List<CardDefinitionSO>(heroDeck), colonyConfig, woundedHeroes, node.difficulty);
            }
            else
            {
                Debug.LogWarning("[RunManager] StartEncounterFromNode: no encounter definition — auto-completing");
                mapManager.OnNodeComplete();
            }
        }

        private void StartBossFromNode(MapNode node)
        {
            var config = levelConfigs[currentLevel - 1];
            if (config.bossDefinition != null && bossManager != null)
            {
                Debug.Log($"[RunManager] StartBossFromNode: starting boss fight '{config.bossDefinition.bossName}'");
                if (!bossesEncountered.Contains(config.bossDefinition.bossName))
                    bossesEncountered.Add(config.bossDefinition.bossName);
                // Deploy heroes via EncounterManager's auto-deploy, then hand off to BossManager
                // For boss fights, we use the encounter system for hero deployment and the boss system for combat
                if (node.encounterDefinition != null)
                {
                    // If the boss node has an encounter layout, use it for board setup
                    encounterManager.StartEncounter(node.encounterDefinition, new List<CardDefinitionSO>(heroDeck), colonyConfig, woundedHeroes, node.difficulty);
                }
                else
                {
                    // No encounter layout — start boss fight directly with hero data
                    // BossManager handles combat without board
                    Debug.Log("[RunManager] StartBossFromNode: no encounter layout — boss fight runs without board");
                    var result = new EncounterResult { success = false };
                    // Boss fight will fire OnBossDefeated or OnEncounterComplete(failure)
                    bossManager.StartBossFight(config.bossDefinition, new List<HeroAgent>());
                }
            }
            else
            {
                Debug.LogWarning("[RunManager] StartBossFromNode: no boss definition or BossManager — auto-completing level");
                EventBus.OnLevelComplete?.Invoke();
            }
        }

        private void OnEncounterComplete(EncounterResult result)
        {
            Debug.Log($"[RunManager] OnEncounterComplete: {result}");
            encountersCompleted++;

            // Apply resources to stockpile
            if (result.success)
            {
                foreach (var kvp in result.resourcesGathered)
                {
                    switch (kvp.Key)
                    {
                        case ResourceType.Food:
                            colonyManager.AddFood(kvp.Value);
                            break;
                        case ResourceType.Materials:
                            colonyManager.AddMaterials(kvp.Value);
                            break;
                        case ResourceType.Currency:
                            colonyManager.AddCurrency(kvp.Value);
                            break;
                    }
                    totalResourcesGathered += kvp.Value;
                }

                // Elite encounter bonus currency
                if (lastNodeType == NodeType.EliteEncounter)
                {
                    var bc = BalanceConfigSO.Instance;
                    int bonus = bc != null ? bc.eliteBonusCurrency : 3;
                    colonyManager.AddCurrency(bonus);
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
            if (!colonyManager.IsAlive)
            {
                Debug.Log("[RunManager] OnEncounterComplete: colony died — run failed");
                RunFailed();
                return;
            }

            runState = RunState.MapTraversal;
            SaveRunState();
            mapManager.OnNodeComplete();
        }

        private void OnNodeHandlerComplete()
        {
            Debug.Log("[RunManager] OnNodeHandlerComplete: non-combat node done, returning to map");
            runState = RunState.MapTraversal;
            SaveRunState();
            mapManager.OnNodeComplete();
        }

        private void OnLevelComplete()
        {
            Debug.Log($"[RunManager] OnLevelComplete: level {currentLevel} complete!");
            bossesKilled++;

            // For M1, only Level 1 — run complete after boss
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
            var healed = new List<CardDefinitionSO>();
            // Note: wound healing happens per-encounter — heroes sit out ONE encounter then auto-heal
            // This is tracked by the encounter system checking woundedHeroes set
        }

        private void ConsumeFood()
        {
            if (colonyConfig == null) return;

            int consumption = colonyConfig.foodConsumptionPerNode;
            int available = colonyManager.FoodStockpile;
            int shortfall = Mathf.Max(0, consumption - available);

            Debug.Log($"[RunManager] ConsumeFood: consumption={consumption}, available={available}, shortfall={shortfall}");

            int toSpend = Mathf.Min(consumption, available);
            if (toSpend > 0)
                colonyManager.SpendFood(toSpend);

            EventBus.OnFoodConsumed?.Invoke(colonyManager.FoodStockpile);

            if (shortfall > 0)
            {
                var bc = BalanceConfigSO.Instance;
                int dmgPerFood = bc != null ? bc.starvationDamagePerFood : 2;
                int damage = shortfall * dmgPerFood;
                Debug.Log($"[RunManager] ConsumeFood: STARVATION — shortfall={shortfall}, dmgPerFood={dmgPerFood}, damage={damage}");
                colonyManager.TakeDamage(damage);
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

        private void OnBossDefeated()
        {
            Debug.Log("[RunManager] OnBossDefeated: boss defeated — completing encounter as success");
            var result = new EncounterResult
            {
                success = true,
                recalled = false,
                rewardCards = bossManager.GetRewardCards(),
                rewardRelic = bossManager.GetRewardRelic()
            };

            // If there are reward cards, show reward selection
            if (result.rewardCards != null && result.rewardCards.Count > 0)
            {
                Debug.Log($"[RunManager] OnBossDefeated: {result.rewardCards.Count} reward cards available");
                // Reward selection is handled by RewardSelectionUI subscribing to OnBossDefeated
            }

            OnEncounterComplete(result);
        }

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
                var victim = candidates[Random.Range(0, candidates.Count)];
                woundedHeroes.Add(victim);
                Debug.Log($"[RunManager] OnEventWoundHero: wounded '{victim.cardName}' (totalWounded={woundedHeroes.Count})");
            }
            else
            {
                Debug.Log("[RunManager] OnEventWoundHero: no eligible heroes to wound");
            }
        }

        private void SaveRunState()
        {
            var save = new RunSaveData
            {
                currentLevel = currentLevel,
                runState = (int)runState,
                nodesVisited = nodesVisited,
                colonyHP = colonyManager.CurrentHP,
                colonyMaxHP = colonyManager.MaxHP,
                foodStockpile = colonyManager.FoodStockpile,
                materialsStockpile = colonyManager.MaterialsStockpile,
                currencyStockpile = colonyManager.CurrencyStockpile,
                encountersCompleted = encountersCompleted,
                totalResourcesGathered = totalResourcesGathered,
                enemiesDefeated = enemiesDefeated,
                bossesKilled = bossesKilled
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

        private void RunComplete()
        {
            runState = RunState.RunComplete;
            Debug.Log($"[RunManager] RunComplete: Victory! encounters={encountersCompleted}, resources={totalResourcesGathered}, bosses={bossesKilled}, nodes={nodesVisited}");
            SaveManager.DeleteSave();

            // Process meta-progression
            if (metaProgression != null)
            {
                metaProgression.ProcessRunEnd(true, currentLevel, totalResourcesGathered,
                    bossesKilled, nodesVisited, heroDeck, enemiesEncountered, eventsEncountered, bossesEncountered);
            }

            EventBus.OnRunComplete_M1?.Invoke(true);
            EventBus.OnRunComplete?.Invoke();

            string msg = "Victory! The Pack survives!";
            EventBus.OnGatheringNotification?.Invoke(msg, new Color(1f, 0.9f, 0.3f));
        }

        private void RunFailed()
        {
            runState = RunState.GameOver;
            Debug.Log($"[RunManager] RunFailed: Defeat. encounters={encountersCompleted}, resources={totalResourcesGathered}, nodes={nodesVisited}");
            SaveManager.DeleteSave();

            // Process meta-progression
            if (metaProgression != null)
            {
                metaProgression.ProcessRunEnd(false, currentLevel, totalResourcesGathered,
                    bossesKilled, nodesVisited, heroDeck, enemiesEncountered, eventsEncountered, bossesEncountered);
            }

            EventBus.OnRunFailed_M1?.Invoke();
            EventBus.OnRunFailed?.Invoke();

            string msg = "The Colony Has Fallen...";
            EventBus.OnGatheringNotification?.Invoke(msg, new Color(1f, 0.2f, 0.2f));
        }
    }
}
