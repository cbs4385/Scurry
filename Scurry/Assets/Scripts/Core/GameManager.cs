using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Map;
using Scurry.Colony;
using Scurry.Cards;
using Scurry.Interfaces;
using Scurry.Combat;
using Scurry.Logistics;
using Scurry.UI;

namespace Scurry.Core
{
    /// <summary>
    /// GameMap scene initializer. Sets up all gameplay systems when GameMap loads,
    /// either from a fresh run or from a saved state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Map Configuration")]
        [SerializeField] private MapConfigSO mapConfig;

        // Scene-local system references
        private RunManager runManager;
        private MapGraph mapGraph;
        private FogOfWar fogOfWar;

        // State
        private bool isInitialized;
        private bool isRestoringFromSave;
        private bool ownsServiceRegistrations;

        public MapGraph MapGraph => mapGraph;
        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            Debug.Log($"[GameManager] Awake: GameMap scene initializer starting (instanceId={GetInstanceID()})");

            // Resolve RunManager — it lives on PersistentManagers root
            runManager = RunManager.Instance;
            if (runManager == null)
            {
                runManager = ServiceLocator.Get<RunManager>();
            }

            if (runManager == null)
            {
                Debug.LogError("[GameManager] Awake: RunManager not found! Cannot initialize GameMap. Loading Bootstrap...");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Bootstrap");
                return;
            }

            Debug.Log($"[GameManager] Awake: RunManager found (state={runManager.CurrentState}, seed={runManager.RandomSeed}, turn={runManager.CurrentTurn})");

            // If returning from a phase scene, reconnect immediately in Awake.
            // This avoids relying on IEnumerator Start() which can silently fail on scene reload.
            var existingTM = TurnManager.Instance;
            if (existingTM != null && existingTM.RunActive)
            {
                Debug.Log($"[GameManager] Awake: TurnManager already active (turn={existingTM.CurrentTurn}, " +
                          $"phase={existingTM.CurrentPhase}) — reconnecting to existing run");
                ReconnectToExistingRun(existingTM);
                isInitialized = true;
                Debug.Log("[GameManager] Awake: GameMap reconnection COMPLETE");
            }
        }

        private IEnumerator Start()
        {
            Debug.Log($"[GameManager] Start: beginning (instanceId={GetInstanceID()}, isInitialized={isInitialized})");

            // If already reconnected in Awake, skip initialization
            if (isInitialized)
            {
                Debug.Log("[GameManager] Start: already initialized via Awake reconnect — skipping");
                yield break;
            }

            if (runManager == null)
            {
                Debug.LogError("[GameManager] Start: RunManager is null — aborting initialization");
                yield break;
            }

            // Determine if this is a new run or a continue
            isRestoringFromSave = runManager.CurrentTurn > 0;
            Debug.Log($"[GameManager] Start: isRestoringFromSave={isRestoringFromSave} (currentTurn={runManager.CurrentTurn})");

            if (isRestoringFromSave)
            {
                yield return StartCoroutine(RestoreSavedState());
            }
            else
            {
                yield return StartCoroutine(InitializeNewRun());
            }

            isInitialized = true;
            Debug.Log("[GameManager] Start: GameMap initialization COMPLETE");

            // Log final state summary
            LogInitializationSummary();
        }

        /// <summary>
        /// Reconnects scene-local UI and services to an existing TurnManager when returning
        /// from a phase scene (Colony, Deployment, Combat) back to GameMap mid-turn.
        /// Synchronous — no yield, no coroutines. Uses TurnManager's existing data references.
        /// </summary>
        private void ReconnectToExistingRun(TurnManager turnManager)
        {
            Debug.Log("[GameManager] ReconnectToExistingRun: re-registering services and refreshing UI");

            // Store the mapGraph from TurnManager for local use
            mapGraph = turnManager.MapGraph;
            Debug.Log($"[GameManager] ReconnectToExistingRun: mapGraph={mapGraph != null} (nodes={mapGraph?.GetAllNodes().Count ?? 0})");

            // Re-register scene-local services
            RegisterServices();

            // Re-initialize scene-local UI (includes MapRenderer.Initialize + UpdateFog)
            InitializeUI();

            // Update token positions (heroes/enemies on the map)
            var mapRenderer = FindAnyObjectByType<MapRenderer>();
            if (mapRenderer != null && mapGraph != null)
            {
                mapRenderer.UpdateTokenPositions(
                    new List<HeroToken>(turnManager.DeployedHeroes),
                    new List<EnemyToken>(turnManager.AllEnemies));
                Debug.Log($"[GameManager] ReconnectToExistingRun: token positions updated " +
                    $"(heroes={turnManager.DeployedHeroes.Count}, enemies={turnManager.AllEnemies.Count})");
            }
            else
            {
                Debug.LogWarning($"[GameManager] ReconnectToExistingRun: mapRenderer={mapRenderer != null}, mapGraph={mapGraph != null}");
            }

            Debug.Log("[GameManager] ReconnectToExistingRun: complete");
        }

        /// <summary>
        /// Initializes all systems for a brand new run.
        /// </summary>
        private IEnumerator InitializeNewRun()
        {
            Debug.Log("[GameManager] InitializeNewRun: === STARTING NEW RUN INITIALIZATION ===");

            int seed = runManager.RandomSeed;
            Debug.Log($"[GameManager] InitializeNewRun: using seed={seed}");

            // Step 1: Generate map
            Debug.Log("[GameManager] InitializeNewRun: Step 1 — Generating map");
            if (mapConfig == null)
            {
                Debug.LogWarning("[GameManager] InitializeNewRun: mapConfig is null — using default MapConfigSO");
                mapConfig = ScriptableObject.CreateInstance<MapConfigSO>();
            }
            mapGraph = MapGenerator.GenerateMap(mapConfig, seed);
            Debug.Log($"[GameManager] InitializeNewRun: map generated (nodes={mapGraph.GetAllNodes().Count}, colonyNode={mapGraph.ColonyNodeId}, piperNode={mapGraph.PiedPiperNodeId})");

            yield return null; // Let Unity process

            // Step 2: Spawn enemies
            Debug.Log("[GameManager] InitializeNewRun: Step 2 — Spawning enemies");
            SpawnEnemies(seed);

            yield return null;

            // Step 3: Initialize colony with starter cards
            Debug.Log("[GameManager] InitializeNewRun: Step 3 — Initializing colony");
            InitializeColony();

            yield return null;

            // Step 4: Initialize deck manager with constructed deck
            Debug.Log("[GameManager] InitializeNewRun: Step 4 — Initializing deck");
            InitializeDeck();

            yield return null;

            // Step 5: Initialize fog of war
            Debug.Log("[GameManager] InitializeNewRun: Step 5 — Initializing fog of war");
            InitializeFogOfWar();

            yield return null;

            // Step 6: Register services
            Debug.Log("[GameManager] InitializeNewRun: Step 6 — Registering services");
            RegisterServices();

            // Step 7: Start the run
            Debug.Log("[GameManager] InitializeNewRun: Step 7 — Starting the run");
            StartRun();

            Debug.Log("[GameManager] InitializeNewRun: === NEW RUN INITIALIZATION COMPLETE ===");
        }

        /// <summary>
        /// Restores all systems from saved state when continuing a run.
        /// </summary>
        private IEnumerator RestoreSavedState()
        {
            Debug.Log("[GameManager] RestoreSavedState: === RESTORING SAVED STATE ===");

            RunSaveData save = SaveManager.Load();
            if (save == null)
            {
                Debug.LogError("[GameManager] RestoreSavedState: no save data found — falling back to new run");
                yield return StartCoroutine(InitializeNewRun());
                yield break;
            }

            Debug.Log($"[GameManager] RestoreSavedState: loaded save (turn={save.currentTurn}, seed={save.randomSeed}, nodes={save.mapNodes.Count}, enemies={save.enemies.Count})");

            // Step 1: Regenerate map from seed (deterministic)
            Debug.Log("[GameManager] RestoreSavedState: Step 1 — Regenerating map from seed");
            if (mapConfig == null)
            {
                mapConfig = ScriptableObject.CreateInstance<MapConfigSO>();
            }
            mapGraph = MapGenerator.GenerateMap(mapConfig, save.randomSeed);
            Debug.Log($"[GameManager] RestoreSavedState: map regenerated (nodes={mapGraph.GetAllNodes().Count})");

            yield return null;

            // Step 2: Restore map node states (visited, fog, resources)
            Debug.Log($"[GameManager] RestoreSavedState: Step 2 — Restoring {save.mapNodes.Count} map node states");
            foreach (var nodeSave in save.mapNodes)
            {
                var node = mapGraph.GetNode(nodeSave.nodeId);
                if (node != null)
                {
                    node.visited = nodeSave.visited;
                    node.fogState = (FogState)nodeSave.fogState;

                    // Restore resources
                    node.resources.Clear();
                    foreach (var res in nodeSave.resources)
                    {
                        node.resources[(ResourceType)res.resourceType] = res.amount;
                    }

                    Debug.Log($"[GameManager] RestoreSavedState: restored node {nodeSave.nodeId} (visited={node.visited}, fog={node.fogState}, resources={node.TotalResources()})");
                }
                else
                {
                    Debug.LogWarning($"[GameManager] RestoreSavedState: node {nodeSave.nodeId} not found in regenerated map");
                }
            }

            yield return null;

            // Step 3: Restore enemies
            Debug.Log($"[GameManager] RestoreSavedState: Step 3 — Restoring {save.enemies.Count} enemies");
            foreach (var enemySave in save.enemies)
            {
                Debug.Log($"[GameManager] RestoreSavedState: enemy '{enemySave.enemyName}' (tokenId={enemySave.tokenId}, nodeId={enemySave.currentNodeId}, hp={enemySave.currentHP}, defeated={enemySave.isDefeated})");
                // Enemy token spawning will be handled by the enemy system when it reads this data
            }

            yield return null;

            // Step 4: Initialize colony from save
            Debug.Log("[GameManager] RestoreSavedState: Step 4 — Restoring colony state");
            InitializeColony();
            // Colony card placement restoration will be handled by ColonyManager reading save data

            yield return null;

            // Step 5: Initialize deck from save
            Debug.Log("[GameManager] RestoreSavedState: Step 5 — Restoring deck state");
            InitializeDeck();

            yield return null;

            // Step 6: Initialize fog of war with restored states
            Debug.Log("[GameManager] RestoreSavedState: Step 6 — Initializing fog of war with restored states");
            InitializeFogOfWar();

            yield return null;

            // Step 7: Register services
            Debug.Log("[GameManager] RestoreSavedState: Step 7 — Registering services");
            RegisterServices();

            // Step 8: Resume run from saved turn
            Debug.Log($"[GameManager] RestoreSavedState: Step 8 — Resuming run from turn {save.currentTurn}");
            StartRun();

            Debug.Log("[GameManager] RestoreSavedState: === SAVED STATE RESTORATION COMPLETE ===");
        }

        /// <summary>
        /// Spawns enemies on the map based on the EnemyDatabase and map zones.
        /// </summary>
        private void SpawnEnemies(int seed)
        {
            Debug.Log($"[GameManager] SpawnEnemies: spawning enemies with seed={seed}");

            var enemyDb = EnemyDatabase.Instance;
            var allEnemies = enemyDb.AllEnemies;
            Debug.Log($"[GameManager] SpawnEnemies: enemyDatabase has {allEnemies.Count} enemy definitions");

            int totalSpawned = 0;
            NodeType[] zones = { NodeType.Wilderness, NodeType.Farmland, NodeType.Town };
            var rng = new System.Random(seed);
            var balanceConfig = BalanceConfigSO.Instance;
            float enemySpawnChance = balanceConfig != null ? balanceConfig.GetEnemySpawnChance() : 0.33f;
            Debug.Log($"[GameManager] SpawnEnemies: using difficulty-aware spawnChance={enemySpawnChance:F2} (difficulty={balanceConfig?.difficulty})");


            foreach (var zone in zones)
            {
                var zoneEnemies = enemyDb.GetRegularEnemiesByZone(zone);
                Debug.Log($"[GameManager] SpawnEnemies: zone={zone}, available enemy types={zoneEnemies.Count}");

                // Get all nodes in this zone
                var zoneNodes = mapGraph.GetNodesInZone(zone);
                Debug.Log($"[GameManager] SpawnEnemies: zone={zone}, nodes={zoneNodes.Count}");

                // Only ~33% of nodes get an enemy
                int enemyIndex = 0;
                foreach (var node in zoneNodes)
                {
                    if (zoneEnemies.Count == 0)
                    {
                        Debug.LogWarning($"[GameManager] SpawnEnemies: no enemies defined for zone={zone}, skipping");
                        break;
                    }

                    float roll = (float)rng.NextDouble();
                    if (roll > enemySpawnChance)
                    {
                        Debug.Log($"[GameManager] SpawnEnemies: skipped node {node.nodeId} (zone={zone}, roll={roll:F2} > {enemySpawnChance})");
                        continue;
                    }

                    // Place one enemy on this node (round-robin through enemy types)
                    var enemyDef = zoneEnemies[enemyIndex % zoneEnemies.Count];
                    node.enemyTokenIds.Add(totalSpawned);
                    totalSpawned++;
                    enemyIndex++;

                    Debug.Log($"[GameManager] SpawnEnemies: placed '{enemyDef.enemyName}' (tokenId={totalSpawned - 1}) at node {node.nodeId} (zone={zone}, roll={roll:F2})");
                }
            }

            // Spawn bosses
            var bosses = enemyDb.GetBosses();
            Debug.Log($"[GameManager] SpawnEnemies: spawning {bosses.Count} bosses");
            foreach (var boss in bosses)
            {
                // Place bosses at key nodes in their home zone
                var bossZoneNodes = mapGraph.GetNodesInZone(boss.homeZone);
                if (bossZoneNodes.Count > 0)
                {
                    // Place boss at the last node in its zone (furthest from colony)
                    var bossNode = bossZoneNodes[bossZoneNodes.Count - 1];
                    bossNode.enemyTokenIds.Add(totalSpawned);
                    Debug.Log($"[GameManager] SpawnEnemies: placed boss '{boss.enemyName}' (tokenId={totalSpawned}) at node {bossNode.nodeId} (zone={boss.homeZone})");
                    totalSpawned++;
                }
                else
                {
                    Debug.LogWarning($"[GameManager] SpawnEnemies: no nodes found for boss '{boss.enemyName}' in zone={boss.homeZone}");
                }
            }

            Debug.Log($"[GameManager] SpawnEnemies: complete — totalSpawned={totalSpawned}");
        }

        /// <summary>
        /// Initializes the colony with starter cards (Entrance + Basic Burrow per GDD).
        /// </summary>
        private void InitializeColony()
        {
            Debug.Log("[GameManager] InitializeColony: setting up colony");

            var colonyDeck = runManager.ConstructedColonyDeck;
            Debug.Log($"[GameManager] InitializeColony: colony deck has {colonyDeck.Count} cards");

            // Find and log starter cards
            int starterCount = 0;
            foreach (var card in colonyDeck)
            {
                if (card.isStarter)
                {
                    starterCount++;
                    Debug.Log($"[GameManager] InitializeColony: starter card '{card.cardName}' (id={card.cardId}, effect={card.colonyEffect}, value={card.effectValue})");
                }
            }
            Debug.Log($"[GameManager] InitializeColony: found {starterCount} starter cards in colony deck");

            // ColonyManager handles the actual placement; we just verify it exists
            var colonyManager = ColonyManager.Instance;
            if (colonyManager == null)
            {
                colonyManager = FindAnyObjectByType<ColonyManager>();
            }

            if (colonyManager != null)
            {
                Debug.Log($"[GameManager] InitializeColony: ColonyManager found (hp={colonyManager.CurrentHP}/{colonyManager.MaxHP})");
            }
            else
            {
                Debug.LogWarning("[GameManager] InitializeColony: ColonyManager not found — colony features will be unavailable");
            }
        }

        /// <summary>
        /// Initializes the DeckManager with the player's constructed deck.
        /// </summary>
        private void InitializeDeck()
        {
            Debug.Log("[GameManager] InitializeDeck: setting up deck");

            var deck = runManager.ConstructedDeck;
            Debug.Log($"[GameManager] InitializeDeck: constructed deck has {deck.Count} cards");

            // Log deck breakdown by type
            int heroes = 0, equipment = 0, tactical = 0, other = 0;
            foreach (var card in deck)
            {
                switch (card.cardType)
                {
                    case CardType.Hero: heroes++; break;
                    case CardType.Equipment: equipment++; break;
                    case CardType.Tactical: tactical++; break;
                    default: other++; break;
                }
                Debug.Log($"[GameManager] InitializeDeck: card '{card.cardName}' (id={card.cardId}, type={card.cardType})");
            }
            Debug.Log($"[GameManager] InitializeDeck: breakdown — heroes={heroes}, equipment={equipment}, tactical={tactical}, other={other}");

            // DeckManager initialization will be handled by the DeckManager component when it picks up the deck
            var deckManager = FindAnyObjectByType<DeckManager>();
            if (deckManager != null)
            {
                Debug.Log("[GameManager] InitializeDeck: DeckManager found — deck will be initialized by it");
            }
            else
            {
                Debug.LogWarning("[GameManager] InitializeDeck: DeckManager not found in scene");
            }
        }

        /// <summary>
        /// Initializes the fog of war system. Colony node starts visible.
        /// </summary>
        private void InitializeFogOfWar()
        {
            Debug.Log("[GameManager] InitializeFogOfWar: creating FogOfWar system");

            fogOfWar = new FogOfWar();

            // All colony nodes are always visible (colony is home base)
            var colonyNodes = mapGraph.GetColonyNodes();
            Debug.Log($"[GameManager] InitializeFogOfWar: found {colonyNodes.Count} colony nodes to reveal");
            foreach (var cNode in colonyNodes)
            {
                cNode.fogState = FogState.Visible;
                cNode.visited = true;
                Debug.Log($"[GameManager] InitializeFogOfWar: colony node {cNode.nodeId} set to Visible");
            }

            // Also reveal neighbors of colony nodes
            var colonyNode = mapGraph.GetNode(mapGraph.ColonyNodeId);
            if (colonyNode != null)
            {
                foreach (int neighborId in colonyNode.neighborIds)
                {
                    var neighbor = mapGraph.GetNode(neighborId);
                    if (neighbor != null && neighbor.fogState == FogState.Hidden)
                    {
                        neighbor.fogState = FogState.Visible;
                        Debug.Log($"[GameManager] InitializeFogOfWar: colony neighbor node {neighborId} set to Visible");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] InitializeFogOfWar: primary colony node not found in map graph");
            }

            Debug.Log("[GameManager] InitializeFogOfWar: fog of war initialization complete");
        }

        /// <summary>
        /// Registers scene-local services with ServiceLocator.
        /// </summary>
        private void RegisterServices()
        {
            Debug.Log($"[GameManager] RegisterServices: registering scene services (instanceId={GetInstanceID()})");

            // Mark any previous GameManager as no longer owning registrations
            var previous = ServiceLocator.Get<GameManager>();
            if (previous != null && previous != this)
            {
                previous.ownsServiceRegistrations = false;
                Debug.Log($"[GameManager] RegisterServices: revoked ownership from previous instance");
            }

            ownsServiceRegistrations = true;
            ServiceLocator.Register<GameManager>(this);
            Debug.Log("[GameManager] RegisterServices: registered GameManager");

            if (mapGraph != null)
            {
                ServiceLocator.Register<MapGraph>(mapGraph);
                ServiceLocator.Register<IMapGraph>(mapGraph);
                Debug.Log("[GameManager] RegisterServices: registered MapGraph and IMapGraph");
            }

            if (fogOfWar != null)
            {
                ServiceLocator.Register<FogOfWar>(fogOfWar);
                ServiceLocator.Register<IFogOfWar>(fogOfWar);
                Debug.Log("[GameManager] RegisterServices: registered FogOfWar and IFogOfWar");
            }

            ServiceLocator.Register<ICombatResolver>(new CombatResolver());
            Debug.Log("[GameManager] RegisterServices: registered ICombatResolver");

            ServiceLocator.Register<IHeroTokenFactory>(new HeroTokenFactory());
            Debug.Log("[GameManager] RegisterServices: registered IHeroTokenFactory");

            ServiceLocator.Register<IEnemyTokenFactory>(new EnemyTokenFactory());
            Debug.Log("[GameManager] RegisterServices: registered IEnemyTokenFactory");

            Debug.Log("[GameManager] RegisterServices: service registration complete");
        }

        /// <summary>
        /// Initializes TurnManager, UI components, and starts the run.
        /// </summary>
        private void StartRun()
        {
            Debug.Log("[GameManager] StartRun: initiating gameplay");

            // Initialize DeckManager with constructed deck
            var deckManager = DeckManager.Instance ?? FindAnyObjectByType<DeckManager>();
            if (deckManager != null)
            {
                deckManager.InitializeDeck(
                    new List<CardDefinitionSO>(runManager.ConstructedDeck),
                    new List<ColonyCardDefinitionSO>(runManager.ConstructedColonyDeck));
                Debug.Log("[GameManager] StartRun: DeckManager initialized with constructed deck");
            }
            else
            {
                Debug.LogError("[GameManager] StartRun: DeckManager not found — cannot initialize deck");
            }

            // Initialize ResourceManager
            var resourceManager = FindAnyObjectByType<ResourceManager>();
            if (resourceManager != null)
            {
                resourceManager.Initialize();
                ServiceLocator.Register<ResourceManager>(resourceManager);
                Debug.Log("[GameManager] StartRun: ResourceManager initialized and registered");
            }
            else
            {
                Debug.LogError("[GameManager] StartRun: ResourceManager not found");
            }

            // Initialize ColonyGraph
            var colonyManager = ColonyManager.Instance ?? FindAnyObjectByType<ColonyManager>();
            ColonyGraph colonyGraph = null;
            if (colonyManager != null)
            {
                colonyGraph = colonyManager.Graph;
                if (colonyGraph == null)
                {
                    colonyManager.InitializeColonyGraph();
                    colonyGraph = colonyManager.Graph;
                    Debug.Log("[GameManager] StartRun: initialized ColonyGraph via ColonyManager");
                }

                // Place starter colony cards via ColonyGraph.Initialize
                if (deckManager != null)
                {
                    ColonyCardDefinitionSO entranceDef = null;
                    ColonyCardDefinitionSO burrowDef = null;
                    foreach (var card in deckManager.AvailableColonyCards)
                    {
                        if (card.isStarter)
                        {
                            if (entranceDef == null)
                                entranceDef = card;
                            else if (burrowDef == null)
                                burrowDef = card;
                        }
                    }
                    colonyGraph.Initialize(mapGraph);
                    Debug.Log("[GameManager] StartRun: ColonyGraph linked to MapGraph");
                    colonyGraph.Initialize(entranceDef, burrowDef);
                    // Move starters from available to played so they don't appear in colony UI
                    if (entranceDef != null) deckManager.PlayColonyCard(entranceDef);
                    if (burrowDef != null) deckManager.PlayColonyCard(burrowDef);
                    Debug.Log($"[GameManager] StartRun: colony initialized with starters " +
                        $"(entrance={entranceDef?.cardName ?? "NULL"}, burrow={burrowDef?.cardName ?? "NULL"}, " +
                        $"availableColony={deckManager.AvailableColonyCards.Count})");
                }

                Debug.Log("[GameManager] StartRun: ColonyGraph ready");
            }

            // Create enemy tokens from map node data
            var enemyTokens = CreateEnemyTokens();
            Debug.Log($"[GameManager] StartRun: created {enemyTokens.Count} enemy tokens");

            // Initialize TurnManager (persistent singleton)
            var turnManager = TurnManager.Instance;
            if (turnManager != null && deckManager != null && resourceManager != null && colonyGraph != null)
            {
                turnManager.Initialize(mapGraph, colonyGraph, deckManager, resourceManager, enemyTokens);
                Debug.Log("[GameManager] StartRun: TurnManager initialized");
            }
            else
            {
                Debug.LogError($"[GameManager] StartRun: cannot initialize TurnManager — " +
                    $"turnManager={turnManager != null}, deckManager={deckManager != null}, " +
                    $"resourceManager={resourceManager != null}, colonyGraph={colonyGraph != null}");
            }

            // Initialize UI components
            InitializeUI();

            // Start the turn loop
            if (turnManager != null)
            {
                turnManager.StartRun();
                Debug.Log("[GameManager] StartRun: TurnManager.StartRun() called — turn loop started");
            }
            else
            {
                // Fallback: fire event for legacy compatibility
                int startTurn = runManager.CurrentTurn > 0 ? runManager.CurrentTurn : 1;
                Debug.LogWarning($"[GameManager] StartRun: TurnManager not found, firing OnTurnStarted (turn={startTurn})");
                EventBus.OnTurnStarted?.Invoke(startTurn);
            }

            Debug.Log("[GameManager] StartRun: gameplay started");
        }

        /// <summary>
        /// Creates EnemyToken objects from map node enemy data.
        /// </summary>
        private List<EnemyToken> CreateEnemyTokens()
        {
            Debug.Log("[GameManager] CreateEnemyTokens: creating enemy tokens from map data");
            var tokens = new List<EnemyToken>();
            var enemyDb = EnemyDatabase.Instance;

            foreach (var node in mapGraph.GetAllNodes())
            {
                foreach (int tokenId in node.enemyTokenIds)
                {
                    // Find the enemy definition for this token
                    var zoneEnemies = enemyDb.GetRegularEnemiesByZone(node.zone);
                    var bosses = enemyDb.GetBosses();

                    EnemyDefinitionSO enemyDef = null;
                    if (zoneEnemies.Count > 0)
                    {
                        enemyDef = zoneEnemies[tokenId % zoneEnemies.Count];
                    }

                    // Check if this is a boss
                    foreach (var boss in bosses)
                    {
                        if (boss.homeZone == node.zone)
                        {
                            // Bosses are placed at end of zone — check if this tokenId matches
                            var zoneNodes = mapGraph.GetNodesInZone(node.zone);
                            if (zoneNodes.Count > 0 && node.nodeId == zoneNodes[zoneNodes.Count - 1].nodeId)
                            {
                                enemyDef = boss;
                                break;
                            }
                        }
                    }

                    if (enemyDef != null)
                    {
                        var token = new EnemyToken(tokenId, enemyDef.enemyName, enemyDef.strength,
                            enemyDef.hp, enemyDef.speed, enemyDef.behavior, enemyDef.homeZone, node.nodeId);
                        tokens.Add(token);
                        Debug.Log($"[GameManager] CreateEnemyTokens: created token (id={tokenId}, name={enemyDef.enemyName}, nodeId={node.nodeId})");
                    }
                    else
                    {
                        Debug.LogWarning($"[GameManager] CreateEnemyTokens: no enemy def found for tokenId={tokenId} at nodeId={node.nodeId}");
                    }
                }
            }

            Debug.Log($"[GameManager] CreateEnemyTokens: created {tokens.Count} total enemy tokens");
            return tokens;
        }

        /// <summary>
        /// Initializes all UI components in the GameMap scene.
        /// </summary>
        private void InitializeUI()
        {
            Debug.Log("[GameManager] InitializeUI: initializing UI components");

            // Set up isometric perspective camera looking down at the map board
            var mainCam = Camera.main;
            if (mainCam != null && mapGraph != null)
            {
                var allNodes = mapGraph.GetAllNodes();
                if (allNodes.Count > 0)
                {
                    // Calculate bounding box of all nodes
                    float minX = float.MaxValue, maxX = float.MinValue;
                    float minY = float.MaxValue, maxY = float.MinValue;
                    foreach (var node in allNodes)
                    {
                        if (node.worldPosition.x < minX) minX = node.worldPosition.x;
                        if (node.worldPosition.x > maxX) maxX = node.worldPosition.x;
                        if (node.worldPosition.y < minY) minY = node.worldPosition.y;
                        if (node.worldPosition.y > maxY) maxY = node.worldPosition.y;
                    }

                    float cx = (minX + maxX) * 0.5f;
                    float cy = (minY + maxY) * 0.5f;
                    float mapHeight = maxY - minY;
                    float mapWidth = maxX - minX;

                    // Switch to perspective for tabletop isometric look
                    mainCam.orthographic = false;
                    mainCam.fieldOfView = 40f;

                    // Isometric tabletop view: colony (south/Y-) near camera, Town (north/Y+) far
                    // Map is on XY plane (Z=0). Camera above (Z+) looking down, tilted so -Y is near.
                    float tiltAngle = 35f;

                    float fovRad = mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                    float aspect = mainCam.aspect > 0 ? mainCam.aspect : 16f / 9f;
                    float distForHeight = (mapHeight * 0.5f + 3f) / Mathf.Tan(fovRad);
                    float distForWidth = (mapWidth * 0.5f + 3f) / (Mathf.Tan(fovRad) * aspect);
                    float dist = Mathf.Max(distForHeight, distForWidth);

                    // Camera raised above the map (Z+), offset south (-Y) to look north
                    float camZ = dist * Mathf.Sin(tiltAngle * Mathf.Deg2Rad);
                    float camYOffset = dist * Mathf.Cos(tiltAngle * Mathf.Deg2Rad);
                    mainCam.transform.position = new Vector3(cx, cy - camYOffset, camZ);
                    // Look at map center
                    mainCam.transform.LookAt(new Vector3(cx, cy, 0), Vector3.up);

                    Debug.Log($"[GameManager] InitializeUI: isometric camera (center=({cx:F1},{cy:F1}), " +
                        $"bounds=({minX:F1},{minY:F1})-({maxX:F1},{maxY:F1}), dist={dist:F1}, " +
                        $"pos={mainCam.transform.position}, fov={mainCam.fieldOfView})");
                }
                else
                {
                    mainCam.transform.position = new Vector3(0, 10, -15);
                    mainCam.transform.rotation = Quaternion.Euler(35, 0, 0);
                    Debug.Log("[GameManager] InitializeUI: no nodes, default isometric camera");
                }
            }
            else if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 10, -15);
                mainCam.transform.rotation = Quaternion.Euler(35, 0, 0);
                Debug.Log($"[GameManager] InitializeUI: fallback camera position");
            }

            // HUDManager
            var hudManager = FindAnyObjectByType<HUDManager>();
            if (hudManager != null)
            {
                hudManager.Initialize();
                Debug.Log("[GameManager] InitializeUI: HUDManager initialized");
            }
            else
            {
                Debug.LogWarning("[GameManager] InitializeUI: HUDManager not found");
            }

            // MapRenderer
            var mapRenderer = FindAnyObjectByType<MapRenderer>();
            if (mapRenderer != null)
            {
                mapRenderer.Initialize(mapGraph);
                mapRenderer.UpdateFog();
                Debug.Log("[GameManager] InitializeUI: MapRenderer initialized and fog synced");
            }
            else
            {
                Debug.LogWarning("[GameManager] InitializeUI: MapRenderer not found");
            }

            // MapInteraction
            var mapInteraction = FindAnyObjectByType<MapInteraction>();
            if (mapInteraction != null && mapRenderer != null)
            {
                mapInteraction.Initialize(mapGraph, mapRenderer);
                Debug.Log("[GameManager] InitializeUI: MapInteraction initialized");
            }
            else
            {
                Debug.LogWarning($"[GameManager] InitializeUI: MapInteraction not initialized " +
                    $"(mapInteraction={mapInteraction != null}, mapRenderer={mapRenderer != null})");
            }

            Debug.Log("[GameManager] InitializeUI: UI initialization complete");
        }

        /// <summary>
        /// Logs a comprehensive summary of the initialized game state.
        /// </summary>
        private void LogInitializationSummary()
        {
            Debug.Log("[GameManager] === INITIALIZATION SUMMARY ===");
            Debug.Log($"[GameManager] Summary: seed={runManager.RandomSeed}");
            Debug.Log($"[GameManager] Summary: deck={runManager.ConstructedDeck.Count} cards, colonyDeck={runManager.ConstructedColonyDeck.Count} cards");
            Debug.Log($"[GameManager] Summary: mapNodes={mapGraph?.GetAllNodes().Count ?? 0}");
            Debug.Log($"[GameManager] Summary: isRestoringFromSave={isRestoringFromSave}");
            Debug.Log($"[GameManager] Summary: runState={runManager.CurrentState}");
            Debug.Log("[GameManager] === END INITIALIZATION SUMMARY ===");
        }

        /// <summary>
        /// v1.0 compat: checks if a hero card is wounded. Delegates to DeckManager injured pool.
        /// </summary>
        public bool IsHeroWounded(CardDefinitionSO card)
        {
            Debug.Log($"[GameManager] IsHeroWounded: checking card='{card?.cardName ?? "NULL"}'");
            var deckManager = FindAnyObjectByType<DeckManager>();
            if (deckManager != null)
            {
                bool wounded = false;
                foreach (var injured in deckManager.InjuredHeroes)
                {
                    if (injured == card) { wounded = true; break; }
                }
                Debug.Log($"[GameManager] IsHeroWounded: card='{card?.cardName}', wounded={wounded}");
                return wounded;
            }
            Debug.Log("[GameManager] IsHeroWounded: no DeckManager found, returning false");
            return false;
        }

        private void OnDestroy()
        {
            if (!ownsServiceRegistrations)
            {
                Debug.Log($"[GameManager] OnDestroy: stale instance (instanceId={GetInstanceID()}) — skipping unregister");
                return;
            }

            Debug.Log($"[GameManager] OnDestroy: cleaning up scene services (instanceId={GetInstanceID()})");

            ownsServiceRegistrations = false;
            ServiceLocator.Unregister<GameManager>();
            ServiceLocator.Unregister<MapGraph>();
            ServiceLocator.Unregister<IMapGraph>();
            ServiceLocator.Unregister<FogOfWar>();
            ServiceLocator.Unregister<IFogOfWar>();
            ServiceLocator.Unregister<ICombatResolver>();
            ServiceLocator.Unregister<IHeroTokenFactory>();
            ServiceLocator.Unregister<IEnemyTokenFactory>();
            ServiceLocator.Unregister<ResourceManager>();

            Debug.Log("[GameManager] OnDestroy: scene services unregistered");
        }
    }
}
