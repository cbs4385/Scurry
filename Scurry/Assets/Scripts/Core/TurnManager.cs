using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Scurry.Data;
using Scurry.Map;
using Scurry.Colony;
using Scurry.Cards;
using Scurry.Combat;
using Scurry.Interfaces;
using Scurry.Logistics;
using Scurry.UI;

namespace Scurry.Core
{
    public class TurnManager : MonoBehaviour, ITurnManager
    {
        public const int MAX_TURNS = 100; // Safety valve only — no fixed turn limit
        public const int PIPER_COUNTDOWN = 15; // Turns after Town zone entered before Pied Piper marches

        private static TurnManager _instance;
        public static TurnManager Instance => _instance;

        private int currentTurn;
        private GamePhase currentPhase;
        private bool waitingForPlayerInput;
        private bool runActive;

        // Pied Piper countdown — triggered when a hero first enters the Town zone
        private bool piperCountdownActive;
        private int piperCountdownRemaining = -1;
        private bool piperMarching; // true once countdown expires and Piper moves to colony

        // References (set via Initialize)
        private MapGraph mapGraph;
        private ColonyGraph colonyGraph;
        private DeckManager deckManager;
        private ResourceManager resourceManager;
        private ICombatResolver combatResolver;
        private List<HeroToken> allHeroes = new List<HeroToken>();
        private List<HeroToken> deployedHeroes = new List<HeroToken>();
        private List<EnemyToken> allEnemies = new List<EnemyToken>();

        // Tracking for score
        private int totalEnemiesDefeated;
        private int totalZoneBossesDefeated;
        private bool piedPiperDefeated;
        private int totalResourcesGathered;
        private int colonyCardsPlayed;
        private HashSet<int> heroesEverInjured = new HashSet<int>();

        // Phase input queue — UI calls Player* methods, coroutine consumes
        private bool phaseEndRequested;
        private bool continuePressed;
        private int heroesDeployedThisTurn;
        private Queue<ColonyCardAction> pendingColonyActions = new Queue<ColonyCardAction>();
        private Queue<DeployAction> pendingDeployActions = new Queue<DeployAction>();
        private Queue<RetargetAction> pendingRetargetActions = new Queue<RetargetAction>();
        private Queue<TacticalCardAction> pendingTacticalActions = new Queue<TacticalCardAction>();

        // Card reward state (shared between TurnManager and CardReward scene)
        private List<CardDefinitionSO> pendingRewardCards;
        private bool rewardSelectionMade;
        private CardDefinitionSO selectedRewardCard;

        public int CurrentTurn => currentTurn;
        public GamePhase CurrentPhase => currentPhase;
        public bool WaitingForInput => waitingForPlayerInput;
        public MapGraph MapGraph => mapGraph;
        public int ColonyNodeId => mapGraph != null ? mapGraph.ColonyNodeId : -1;
        public IReadOnlyList<HeroToken> DeployedHeroes => deployedHeroes;
        public IReadOnlyList<HeroToken> AllHeroes => allHeroes;
        public IReadOnlyList<EnemyToken> AllEnemies => allEnemies;
        public bool RunActive => runActive;
        public bool PiperCountdownActive => piperCountdownActive;
        public int PiperCountdownRemaining => piperCountdownRemaining;
        public int TotalEnemiesDefeated => totalEnemiesDefeated;
        public int TotalZoneBossesDefeated => totalZoneBossesDefeated;
        public bool PiedPiperDefeated => piedPiperDefeated;
        public int TotalResourcesGathered => totalResourcesGathered;
        public int ColonyCardsPlayed => colonyCardsPlayed;
        public int HeroesDeployedThisTurn => heroesDeployedThisTurn;
        public int MaxHeroDeploysPerTurn => BalanceConfigSO.Instance != null ? BalanceConfigSO.Instance.GetMaxHeroDeploysPerTurn() : 2;
        public bool DeployCapReached => heroesDeployedThisTurn >= MaxHeroDeploysPerTurn;

        // ── Structs for queued actions ───────────────────────────────────

        private struct ColonyCardAction
        {
            public ColonyCardDefinitionSO card;
            public int targetNodeId;
        }

        private struct DeployAction
        {
            public CardDefinitionSO heroDef;
            public int targetNodeId;
            public CardDefinitionSO offensive;
            public CardDefinitionSO defensive;
            public CardDefinitionSO utility;
        }

        private struct RetargetAction
        {
            public int heroTokenId;
            public int newTargetNodeId;
        }

        private struct TacticalCardAction
        {
            public CardDefinitionSO card;
            public int targetNodeId;
        }

        // ── Singleton Lifecycle ─────────────────────────────────────────

        private static bool _migrating;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log("[TurnManager] Awake: duplicate instance — destroying self");
                Destroy(this);
                return;
            }

            if (_migrating)
            {
                // This is the migrated instance on the dedicated GO — finalize setup
                _migrating = false;
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[TurnManager] Awake: persistent instance ready on dedicated GO");
                return;
            }

            // First Awake on the scene GO — migrate to a dedicated persistent GO
            // so DontDestroyOnLoad doesn't drag the scene's Managers hierarchy with it
            _migrating = true;
            var persistentGO = new GameObject("TurnManager_Persistent");
            persistentGO.AddComponent<TurnManager>(); // triggers Awake on the new instance
            Debug.Log("[TurnManager] Awake: migrated to dedicated GO, destroying scene-local component");
            Destroy(this);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ── Initialization ───────────────────────────────────────────────

        public void Initialize(MapGraph map, ColonyGraph colony, DeckManager deck,
                               ResourceManager resources, List<EnemyToken> enemies)
        {
            Debug.Log($"[TurnManager] Initialize: setting up turn system (enemyCount={enemies?.Count ?? 0})");

            mapGraph = map;
            colonyGraph = colony;
            deckManager = deck;
            resourceManager = resources;
            combatResolver = new CombatResolver();
            Debug.Log("[TurnManager] Initialize: created CombatResolver (owned by TurnManager, survives scene transitions)");

            allEnemies.Clear();
            if (enemies != null)
            {
                allEnemies.AddRange(enemies);
            }

            allHeroes.Clear();
            deployedHeroes.Clear();
            currentTurn = 0;
            currentPhase = GamePhase.Deploy;
            waitingForPlayerInput = false;
            runActive = false;
            phaseEndRequested = false;

            totalEnemiesDefeated = 0;
            totalZoneBossesDefeated = 0;
            piedPiperDefeated = false;
            totalResourcesGathered = 0;
            colonyCardsPlayed = 0;
            heroesEverInjured.Clear();
            piperCountdownActive = false;
            piperCountdownRemaining = -1;
            piperMarching = false;

            // Register with ServiceLocator so UI components can resolve us
            ServiceLocator.Register<ITurnManager>(this);
            ServiceLocator.Register<TurnManager>(this);
            Debug.Log("[TurnManager] Initialize: registered with ServiceLocator as ITurnManager and TurnManager");

            Debug.Log($"[TurnManager] Initialize: complete (mapGraph={mapGraph != null}, colonyGraph={colonyGraph != null}, " +
                      $"deckManager={deckManager != null}, resourceManager={resourceManager != null}, enemies={allEnemies.Count})");
        }

        /// <summary>
        /// Updates the HUDManager with current turn/phase/resource info.
        /// </summary>
        private void UpdateHUD()
        {
            if (SceneManager.GetActiveScene().name != "GameMap") return;

            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud == null) return;

            hud.UpdateTurnDisplay(currentTurn, currentPhase, piperCountdownRemaining);
            if (resourceManager != null)
            {
                hud.UpdateResources(resourceManager.FoodStockpile, resourceManager.MaterialsStockpile, resourceManager.CurrencyStockpile);
            }
            hud.UpdateHeroCount(deployedHeroes.Count, allHeroes.Count);
        }

        // ── Run lifecycle ────────────────────────────────────────────────

        public void StartRun()
        {
            Debug.Log("[TurnManager] StartRun: beginning new run");

            if (mapGraph == null || colonyGraph == null || deckManager == null || resourceManager == null)
            {
                Debug.LogError("[TurnManager] StartRun: one or more required references are null — aborting");
                return;
            }

            runActive = true;
            currentTurn = 0;

            // Initial map visual sync (update renderer to match current fog states, but don't
            // recalculate fog — GameManager already set initial visibility)
            var mapRenderer = Object.FindAnyObjectByType<MapRenderer>();
            if (mapRenderer != null)
            {
                mapRenderer.UpdateVisuals();
                mapRenderer.UpdateTokenPositions(deployedHeroes, allEnemies);
                Debug.Log("[TurnManager] StartRun: initial map visual sync complete");
            }

            EventBus.OnRunStarted?.Invoke();
            Debug.Log("[TurnManager] StartRun: fired OnRunStarted, starting turn loop coroutine");

            StartCoroutine(RunTurnLoop());
        }

        private IEnumerator RunTurnLoop()
        {
            Debug.Log("[TurnManager] RunTurnLoop: entering main turn loop (no fixed turn limit, " +
                      $"piperCountdown={PIPER_COUNTDOWN} turns after Town zone entered)");

            while (runActive && currentTurn < MAX_TURNS)
            {
                currentTurn++;
                string countdownInfo = piperCountdownActive
                    ? $"piperCountdown={piperCountdownRemaining}"
                    : "piperCountdown=inactive";
                Debug.Log($"[TurnManager] RunTurnLoop: === TURN {currentTurn} START ({countdownInfo}) ===");

                EventBus.OnTurnStarted?.Invoke(currentTurn);
                UpdateHUD();

                // Reset per-turn counters
                colonyGraph.ResetTurnCardCount();
                heroesDeployedThisTurn = 0;

                // Phase 1: Colony Deploy (place colony cards on map)
                yield return StartCoroutine(ExecuteColonyDeployPhase());

                // Phase 2: Hero Deploy (deploy heroes + equipment)
                yield return StartCoroutine(ExecuteDeployPhase());

                // Pre-move pause: let player set hero targets by clicking nodes on the map
                RefreshMapVisuals();
                yield return StartCoroutine(WaitForPlayerContinue("Set Hero Targets"));

                // Phase 3: Hero Move (heroes move toward their targets)
                yield return StartCoroutine(ExecuteHeroMovePhase());

                // Check if any hero entered the Town zone (triggers Pied Piper countdown)
                CheckTownZoneEntry();

                // Pause for player to observe hero positions after movement
                yield return StartCoroutine(WaitForPlayerContinue("Hero Move"));

                // Phase 4: Enemy Move
                yield return StartCoroutine(ExecuteEnemyMovePhase());

                // Pause for player to observe enemy movements
                yield return StartCoroutine(WaitForPlayerContinue("Enemy Move"));

                // Phase 5: Combat
                yield return StartCoroutine(ExecuteCombatPhase());

                // Pause for player to observe combat results
                yield return StartCoroutine(WaitForPlayerContinue("Combat"));

                // Phase 6: Gather
                yield return StartCoroutine(ExecuteGatherPhase());

                // Pause for player to observe gathered resources
                yield return StartCoroutine(WaitForPlayerContinue("Gather"));

                // Phase 7: Cleanup
                yield return StartCoroutine(ExecuteCleanupPhase());

                // Tick the Pied Piper countdown
                if (piperCountdownActive && !piperMarching)
                {
                    piperCountdownRemaining--;
                    Debug.Log($"[TurnManager] RunTurnLoop: Pied Piper countdown tick " +
                              $"(remaining={piperCountdownRemaining})");
                    EventBus.OnPiperCountdownTick?.Invoke(piperCountdownRemaining);

                    if (piperCountdownRemaining <= 0)
                    {
                        Debug.Log("[TurnManager] RunTurnLoop: Pied Piper countdown expired! " +
                                  "The Pied Piper marches on the colony!");
                        piperMarching = true;
                        MovePiperToColony();
                        EventBus.OnPiperMarchesOnColony?.Invoke();
                    }
                }

                countdownInfo = piperCountdownActive
                    ? $"piperCountdown={piperCountdownRemaining}"
                    : "piperCountdown=inactive";
                Debug.Log($"[TurnManager] RunTurnLoop: === TURN {currentTurn} END ({countdownInfo}) ===");
                EventBus.OnTurnEnded?.Invoke();

                // Win/loss checks
                if (CheckVictory())
                {
                    Debug.Log($"[TurnManager] RunTurnLoop: victory detected after turn {currentTurn}");
                    EndRun(true);
                    yield break;
                }

                if (CheckDefeat())
                {
                    Debug.Log($"[TurnManager] RunTurnLoop: defeat detected after turn {currentTurn}");
                    EndRun(false);
                    yield break;
                }
            }

            // Safety valve — should not normally be reached
            if (runActive)
            {
                Debug.Log($"[TurnManager] RunTurnLoop: safety valve ({MAX_TURNS} turns) reached — defeat");
                EndRun(false);
            }
        }

        /// <summary>
        /// Checks if any deployed hero is on a Town zone node. If so, starts the Pied Piper countdown.
        /// </summary>
        private void CheckTownZoneEntry()
        {
            if (piperCountdownActive) return;

            foreach (var hero in deployedHeroes)
            {
                if (hero.isInjured) continue;
                var node = mapGraph.GetNode(hero.currentNodeId);
                if (node != null && node.zone == NodeType.Town)
                {
                    piperCountdownActive = true;
                    piperCountdownRemaining = PIPER_COUNTDOWN;
                    Debug.Log($"[TurnManager] CheckTownZoneEntry: hero {hero.cardDef?.cardName ?? "unknown"} " +
                              $"(tokenId={hero.tokenId}) entered Town zone at node {node.nodeId}! " +
                              $"Pied Piper countdown started: {PIPER_COUNTDOWN} turns");
                    EventBus.OnPiperCountdownStarted?.Invoke(PIPER_COUNTDOWN);
                    EventBus.OnNotification?.Invoke(
                        $"The Pied Piper stirs! {PIPER_COUNTDOWN} turns until he marches on your colony!",
                        new Color(1f, 0.4f, 0.1f));
                    return;
                }
            }
        }

        /// <summary>
        /// Moves the Pied Piper enemy token to the colony node when the countdown expires.
        /// </summary>
        private void MovePiperToColony()
        {
            int colonyNodeId = mapGraph.ColonyNodeId;
            var piperToken = allEnemies.Find(e => e.homeZone == NodeType.PiedPiper && !e.isDefeated);
            if (piperToken == null)
            {
                Debug.LogWarning("[TurnManager] MovePiperToColony: no living Pied Piper token found");
                return;
            }

            // Remove from current node
            var fromNode = mapGraph.GetNode(piperToken.currentNodeId);
            fromNode?.enemyTokenIds.Remove(piperToken.tokenId);

            // Place at colony
            piperToken.currentNodeId = colonyNodeId;
            var colonyNode = mapGraph.GetNode(colonyNodeId);
            colonyNode?.enemyTokenIds.Add(piperToken.tokenId);

            Debug.Log($"[TurnManager] MovePiperToColony: Pied Piper moved from node {fromNode?.nodeId ?? -1} " +
                      $"to colony node {colonyNodeId} (strength={piperToken.strength}, hp={piperToken.currentHP})");

            EventBus.OnNotification?.Invoke(
                "The Pied Piper has arrived at your colony!",
                new Color(1f, 0.1f, 0.1f));

            // Refresh map visuals
            RefreshMapVisuals();
        }

        private void ProcessColonyCardAction(ColonyCardAction action)
        {
            Debug.Log($"[TurnManager] ProcessColonyCardAction: attempting to play '{action.card?.cardName ?? "NULL"}' " +
                      $"(attachToId={action.targetNodeId})");

            if (action.card == null)
            {
                Debug.LogWarning("[TurnManager] ProcessColonyCardAction: card is null — skipping");
                return;
            }

            if (!colonyGraph.CanPlayCard())
            {
                Debug.LogWarning($"[TurnManager] ProcessColonyCardAction: cannot play more colony cards this turn " +
                                 $"(card={action.card.cardName})");
                return;
            }

            int placedId = colonyGraph.AddCard(action.card, action.targetNodeId);
            if (placedId < 0)
            {
                Debug.LogWarning($"[TurnManager] ProcessColonyCardAction: placement failed " +
                                 $"(card={action.card.cardName}, attachToId={action.targetNodeId}, placedId={placedId})");
                return;
            }

            deckManager.PlayColonyCard(action.card);
            colonyCardsPlayed++;

            Debug.Log($"[TurnManager] ProcessColonyCardAction: colony card played successfully " +
                      $"(card={action.card.cardName}, placedId={placedId}, attachToId={action.targetNodeId}, " +
                      $"totalColonyCardsPlayed={colonyCardsPlayed})");

            EventBus.OnColonyCardPlayed?.Invoke(action.card);
        }

        // ── Phase 1: Deploy ──────────────────────────────────────────────

        /// <summary>
        /// Colony Deploy phase: produces colony resources, then lets the player place colony cards
        /// directly on the GameMap by clicking empty colony nodes. Runs on GameMap (no scene transition).
        /// </summary>
        private IEnumerator ExecuteColonyDeployPhase()
        {
            currentPhase = GamePhase.ColonyDeploy;
            Debug.Log($"[TurnManager] ExecuteColonyDeployPhase: entering ColonyDeploy phase (turn={currentTurn})");
            EventBus.OnPhaseChanged?.Invoke(GamePhase.ColonyDeploy);

            // ── Colony resource production ──
            Debug.Log("[TurnManager] ExecuteColonyDeployPhase: producing colony resources");
            resourceManager.ProduceColonyResources(colonyGraph);

            // Scaling food bonus: +1 food per N deployed heroes
            var bc = BalanceConfigSO.Instance;
            int bonusPerN = bc != null ? bc.GetFoodBonusPerNHeroes() : 3;
            if (bonusPerN > 0)
            {
                int activeHeroes = deployedHeroes.Count(h => !h.isInjured);
                int bonus = activeHeroes / bonusPerN;
                if (bonus > 0)
                {
                    resourceManager.AddToStockpile(ResourceType.Food, bonus);
                    Debug.Log($"[TurnManager] ExecuteColonyDeployPhase: hero scaling food bonus " +
                              $"(activeHeroes={activeHeroes}, bonusPerN={bonusPerN}, bonus={bonus})");
                }
            }

            EventBus.OnColonyProductionComplete?.Invoke();

            Debug.Log($"[TurnManager] ExecuteColonyDeployPhase: stockpile after production — " +
                      $"food={resourceManager.FoodStockpile}, materials={resourceManager.MaterialsStockpile}, " +
                      $"currency={resourceManager.CurrencyStockpile}");

            // Always show the map and wait for player to end phase
            // Player can place colony cards if available, or just review the map before hero deployment
            waitingForPlayerInput = true;
            phaseEndRequested = false;
            RefreshMapVisuals();
            UpdateHUD();

            bool hasColonyCards = deckManager.AvailableColonyCards.Count > 0 && colonyGraph.CanPlayCard();
            Debug.Log($"[TurnManager] ExecuteColonyDeployPhase: waiting for player input " +
                      $"(availableColonyCards={deckManager.AvailableColonyCards.Count}, canPlace={hasColonyCards})");

            while (!phaseEndRequested)
            {
                while (pendingColonyActions.Count > 0)
                {
                    var action = pendingColonyActions.Dequeue();
                    ProcessColonyCardAction(action);
                    RefreshMapVisuals();
                    UpdateHUD();
                    // Refresh colony card list in HUD to remove placed card
                    var hud = Object.FindAnyObjectByType<HUDManager>();
                    if (hud != null) hud.RefreshColonyCardList();
                }
                yield return null;
            }

            // Drain remaining colony actions
            while (pendingColonyActions.Count > 0)
            {
                var action = pendingColonyActions.Dequeue();
                ProcessColonyCardAction(action);
            }

            waitingForPlayerInput = false;

            RefreshMapVisuals();
            UpdateHUD();
            Debug.Log($"[TurnManager] ExecuteColonyDeployPhase: ColonyDeploy phase complete (turn={currentTurn})");
        }

        /// <summary>
        /// Hero Deploy phase: transitions to Deployment scene for hero selection, equipment, and targeting.
        /// </summary>
        private IEnumerator ExecuteDeployPhase()
        {
            currentPhase = GamePhase.Deploy;
            Debug.Log($"[TurnManager] ExecuteDeployPhase: entering Deploy phase (turn={currentTurn})");
            EventBus.OnPhaseChanged?.Invoke(GamePhase.Deploy);

            // Transition to Deployment scene
            Debug.Log("[TurnManager] ExecuteDeployPhase: transitioning to Deployment scene");
            yield return StartCoroutine(TransitionToScene("Deployment"));

            // Show Deployment UI
            var deployUI = Object.FindAnyObjectByType<DeploymentUI>();
            if (deployUI != null)
            {
                deployUI.Show(deckManager, this);
                deployUI.PopulateDeployed(deployedHeroes);
                Debug.Log($"[TurnManager] ExecuteDeployPhase: DeploymentUI.Show called (deployedHeroes={deployedHeroes.Count})");
            }
            else
            {
                Debug.LogWarning("[TurnManager] ExecuteDeployPhase: DeploymentUI not found in loaded Deployment scene");
            }

            // Wait for player to deploy heroes, assign equipment, set targets
            waitingForPlayerInput = true;
            phaseEndRequested = false;
            Debug.Log($"[TurnManager] ExecuteDeployPhase: waiting for player input " +
                      $"(availableHeroes={deckManager.AvailableHeroes.Count}, " +
                      $"availableEquipment={deckManager.AvailableEquipment.Count})");

            while (!phaseEndRequested)
            {
                bool actionsProcessed = false;
                while (pendingDeployActions.Count > 0)
                {
                    var action = pendingDeployActions.Dequeue();
                    ProcessDeployAction(action);
                    actionsProcessed = true;
                }

                while (pendingRetargetActions.Count > 0)
                {
                    var action = pendingRetargetActions.Dequeue();
                    ProcessRetargetAction(action);
                    actionsProcessed = true;
                }

                if (actionsProcessed && deployUI != null)
                {
                    deployUI.RefreshDisplay();
                    deployUI.PopulateDeployed(deployedHeroes);
                    Debug.Log("[TurnManager] ExecuteDeployPhase: refreshed DeploymentUI after processing actions");
                }

                yield return null;
            }

            // Drain remaining actions
            while (pendingDeployActions.Count > 0)
                ProcessDeployAction(pendingDeployActions.Dequeue());
            while (pendingRetargetActions.Count > 0)
                ProcessRetargetAction(pendingRetargetActions.Dequeue());

            waitingForPlayerInput = false;

            // Transition back to GameMap
            Debug.Log("[TurnManager] ExecuteDeployPhase: transitioning back to GameMap");
            yield return StartCoroutine(TransitionToScene("GameMap"));

            yield return StartCoroutine(WaitForGameManagerInit());

            RefreshMapVisuals();
            UpdateHUD();
            Debug.Log($"[TurnManager] ExecuteDeployPhase: Deploy phase complete (turn={currentTurn}, " +
                      $"deployedHeroes={deployedHeroes.Count}, totalHeroes={allHeroes.Count})");
        }

        private void ProcessDeployAction(DeployAction action)
        {
            Debug.Log($"[TurnManager] ProcessDeployAction: deploying hero '{action.heroDef?.cardName ?? "NULL"}' " +
                      $"(targetNode={action.targetNodeId}, offensive={action.offensive?.cardName ?? "none"}, " +
                      $"defensive={action.defensive?.cardName ?? "none"}, utility={action.utility?.cardName ?? "none"}, " +
                      $"deployedThisTurn={heroesDeployedThisTurn})");

            int maxPerTurn = BalanceConfigSO.Instance != null ? BalanceConfigSO.Instance.GetMaxHeroDeploysPerTurn() : 2;
            if (heroesDeployedThisTurn >= maxPerTurn)
            {
                Debug.LogWarning($"[TurnManager] ProcessDeployAction: deploy limit reached " +
                                 $"(heroesDeployedThisTurn={heroesDeployedThisTurn}, max={maxPerTurn}) — skipping");
                return;
            }

            if (action.heroDef == null)
            {
                Debug.LogWarning("[TurnManager] ProcessDeployAction: heroDef is null — skipping");
                return;
            }

            if (action.heroDef.cardType != CardType.Hero)
            {
                Debug.LogWarning($"[TurnManager] ProcessDeployAction: card '{action.heroDef.cardName}' is not a hero " +
                                 $"(type={action.heroDef.cardType}) — skipping");
                return;
            }

            // Determine deploy node — colony node by default, or forward deploy if colony has that effect
            int deployNodeId = mapGraph.ColonyNodeId;
            if (colonyGraph.HasEffect(ColonyEffect.ForwardDeploy))
            {
                Debug.Log("[TurnManager] ProcessDeployAction: ForwardDeploy effect active — checking for forward deploy node");
                // Forward deploy allows placement on adjacent-to-colony nodes
                var colonyNeighbors = mapGraph.GetNeighbors(mapGraph.ColonyNodeId);
                if (colonyNeighbors.Count > 0)
                {
                    deployNodeId = colonyNeighbors[0].nodeId;
                    Debug.Log($"[TurnManager] ProcessDeployAction: using forward deploy node (nodeId={deployNodeId})");
                }
            }

            // Create hero token
            int tokenId = allHeroes.Count;
            var heroToken = new HeroToken(action.heroDef, tokenId);
            heroToken.currentNodeId = deployNodeId;
            heroToken.targetNodeId = action.targetNodeId;
            heroToken.isDeployed = true;

            // Apply colony bonuses
            ApplyColonyBonusesToHero(heroToken);

            // Equip items
            if (action.offensive != null)
            {
                heroToken.EquipItem(action.offensive);
                deckManager.AttachEquipment(action.offensive);
                EventBus.OnEquipmentAttached?.Invoke(action.offensive, heroToken);
            }
            if (action.defensive != null)
            {
                heroToken.EquipItem(action.defensive);
                deckManager.AttachEquipment(action.defensive);
                EventBus.OnEquipmentAttached?.Invoke(action.defensive, heroToken);
            }
            if (action.utility != null)
            {
                heroToken.EquipItem(action.utility);
                deckManager.AttachEquipment(action.utility);
                EventBus.OnEquipmentAttached?.Invoke(action.utility, heroToken);
            }

            // Track in lists
            allHeroes.Add(heroToken);
            deployedHeroes.Add(heroToken);
            deckManager.DeployHero(action.heroDef);

            // Add to map node
            MapNode node = mapGraph.GetNode(deployNodeId);
            if (node != null)
            {
                node.heroTokenIds.Add(tokenId);
                Debug.Log($"[TurnManager] ProcessDeployAction: added hero to map node (nodeId={deployNodeId}, " +
                          $"heroTokenIds={node.heroTokenIds.Count})");
            }

            heroesDeployedThisTurn++;
            Debug.Log($"[TurnManager] ProcessDeployAction: hero deployed successfully " +
                      $"(tokenId={tokenId}, name={action.heroDef.cardName}, deployNode={deployNodeId}, " +
                      $"targetNode={action.targetNodeId}, combat={heroToken.EffectiveCombat}, " +
                      $"move={heroToken.EffectiveMove}, hp={heroToken.EffectiveHP}, carry={heroToken.EffectiveCarry}, " +
                      $"deployedThisTurn={heroesDeployedThisTurn})");

            EventBus.OnHeroDeployed?.Invoke(heroToken, deployNodeId);
            EventBus.OnTargetAssigned?.Invoke(heroToken, action.targetNodeId);
        }

        private void ApplyColonyBonusesToHero(HeroToken hero)
        {
            Debug.Log($"[TurnManager] ApplyColonyBonusesToHero: applying colony effects (tokenId={hero.tokenId})");

            hero.colonyBonusCombat = 0;
            hero.colonyBonusMove = 0;
            hero.colonyBonusHP = 0;

            if (colonyGraph.HasEffect(ColonyEffect.AllHeroCombatBuff))
            {
                int bonus = colonyGraph.GetEffectValue(ColonyEffect.AllHeroCombatBuff);
                hero.colonyBonusCombat += bonus;
                Debug.Log($"[TurnManager] ApplyColonyBonusesToHero: AllHeroCombatBuff +{bonus} (tokenId={hero.tokenId})");
            }

            if (colonyGraph.HasEffect(ColonyEffect.AllHeroMoveBuff))
            {
                int bonus = colonyGraph.GetEffectValue(ColonyEffect.AllHeroMoveBuff);
                hero.colonyBonusMove += bonus;
                Debug.Log($"[TurnManager] ApplyColonyBonusesToHero: AllHeroMoveBuff +{bonus} (tokenId={hero.tokenId})");
            }

            if (colonyGraph.HasEffect(ColonyEffect.EquippedHPBuff))
            {
                bool hasEquipment = hero.offensiveEquipment != null ||
                                    hero.defensiveEquipment != null ||
                                    hero.utilityEquipment != null;
                if (hasEquipment)
                {
                    int bonus = colonyGraph.GetEffectValue(ColonyEffect.EquippedHPBuff);
                    hero.colonyBonusHP += bonus;
                    Debug.Log($"[TurnManager] ApplyColonyBonusesToHero: EquippedHPBuff +{bonus} (tokenId={hero.tokenId})");
                }
            }

            if (colonyGraph.HasEffect(ColonyEffect.EquippedCombatBuff))
            {
                bool hasEquipment = hero.offensiveEquipment != null ||
                                    hero.defensiveEquipment != null ||
                                    hero.utilityEquipment != null;
                if (hasEquipment)
                {
                    int bonus = colonyGraph.GetEffectValue(ColonyEffect.EquippedCombatBuff);
                    hero.colonyBonusCombat += bonus;
                    Debug.Log($"[TurnManager] ApplyColonyBonusesToHero: EquippedCombatBuff +{bonus} (tokenId={hero.tokenId})");
                }
            }

            Debug.Log($"[TurnManager] ApplyColonyBonusesToHero: final bonuses (tokenId={hero.tokenId}, " +
                      $"bonusCombat={hero.colonyBonusCombat}, bonusMove={hero.colonyBonusMove}, bonusHP={hero.colonyBonusHP})");
        }

        private void ProcessRetargetAction(RetargetAction action)
        {
            Debug.Log($"[TurnManager] ProcessRetargetAction: retargeting hero (heroTokenId={action.heroTokenId}, " +
                      $"newTargetNodeId={action.newTargetNodeId})");

            HeroToken hero = allHeroes.Find(h => h.tokenId == action.heroTokenId);
            if (hero == null)
            {
                Debug.LogWarning($"[TurnManager] ProcessRetargetAction: hero not found (heroTokenId={action.heroTokenId})");
                return;
            }

            int oldTarget = hero.targetNodeId;
            hero.targetNodeId = action.newTargetNodeId;

            Debug.Log($"[TurnManager] ProcessRetargetAction: target updated (tokenId={hero.tokenId}, " +
                      $"name={hero.cardDef?.cardName ?? "unknown"}, oldTarget={oldTarget}, newTarget={action.newTargetNodeId})");

            EventBus.OnTargetAssigned?.Invoke(hero, action.newTargetNodeId);
        }

        // ── Phase 3: Hero Move ───────────────────────────────────────────

        private IEnumerator ExecuteHeroMovePhase()
        {
            currentPhase = GamePhase.HeroMove;
            Debug.Log($"[TurnManager] ExecuteHeroMovePhase: entering HeroMove phase (turn={currentTurn}, " +
                      $"deployedHeroes={deployedHeroes.Count})");
            EventBus.OnPhaseChanged?.Invoke(GamePhase.HeroMove);

            // Sort heroes by initiative for move order
            var moveOrder = deployedHeroes
                .Where(h => !h.isInjured && h.targetNodeId >= 0)
                .OrderBy(h => h.EffectiveInitiative)
                .ToList();

            Debug.Log($"[TurnManager] ExecuteHeroMovePhase: heroes to move={moveOrder.Count}");

            foreach (var hero in moveOrder)
            {
                if (hero.currentNodeId == hero.targetNodeId)
                {
                    Debug.Log($"[TurnManager] ExecuteHeroMovePhase: hero already at target " +
                              $"(tokenId={hero.tokenId}, nodeId={hero.currentNodeId})");
                    continue;
                }

                // Find path to target
                var path = mapGraph.ShortestPath(hero.currentNodeId, hero.targetNodeId);
                if (path.Count <= 1)
                {
                    Debug.Log($"[TurnManager] ExecuteHeroMovePhase: no path or already at destination " +
                              $"(tokenId={hero.tokenId}, currentNode={hero.currentNodeId}, targetNode={hero.targetNodeId})");
                    continue;
                }

                int movePoints = hero.EffectiveMove;
                Debug.Log($"[TurnManager] ExecuteHeroMovePhase: moving hero (tokenId={hero.tokenId}, " +
                          $"name={hero.cardDef?.cardName ?? "unknown"}, from={hero.currentNodeId}, " +
                          $"to={hero.targetNodeId}, movePoints={movePoints}, pathLength={path.Count - 1})");

                // Move along path up to move points
                int stepsToTake = Mathf.Min(movePoints, path.Count - 1);
                for (int step = 1; step <= stepsToTake; step++)
                {
                    int fromNode = hero.currentNodeId;
                    int toNode = path[step];

                    // Update map node tracking
                    MapNode fromMapNode = mapGraph.GetNode(fromNode);
                    MapNode toMapNode = mapGraph.GetNode(toNode);

                    if (fromMapNode != null)
                    {
                        fromMapNode.heroTokenIds.Remove(hero.tokenId);
                    }
                    if (toMapNode != null)
                    {
                        toMapNode.heroTokenIds.Add(hero.tokenId);
                        toMapNode.visited = true;
                    }

                    hero.currentNodeId = toNode;

                    Debug.Log($"[TurnManager] ExecuteHeroMovePhase: hero stepped (tokenId={hero.tokenId}, " +
                              $"step={step}/{stepsToTake}, from={fromNode}, to={toNode})");

                    EventBus.OnHeroMoved?.Invoke(hero, fromNode, toNode);

                    // Brief yield for animation
                    yield return new WaitForSeconds(0.15f);
                }

                Debug.Log($"[TurnManager] ExecuteHeroMovePhase: hero move complete (tokenId={hero.tokenId}, " +
                          $"finalNode={hero.currentNodeId}, targetNode={hero.targetNodeId}, " +
                          $"atTarget={hero.currentNodeId == hero.targetNodeId})");
            }

            // Refresh map after hero movement (updates fog + token positions)
            RefreshMapVisuals();

            Debug.Log($"[TurnManager] ExecuteHeroMovePhase: HeroMove phase complete (turn={currentTurn})");
        }

        // ── Phase 4: Enemy Move ──────────────────────────────────────────

        private IEnumerator ExecuteEnemyMovePhase()
        {
            currentPhase = GamePhase.EnemyMove;
            Debug.Log($"[TurnManager] ExecuteEnemyMovePhase: entering EnemyMove phase (turn={currentTurn}, " +
                      $"activeEnemies={allEnemies.Count(e => !e.isDefeated)})");
            EventBus.OnPhaseChanged?.Invoke(GamePhase.EnemyMove);

            foreach (var enemy in allEnemies)
            {
                if (enemy.isDefeated)
                {
                    Debug.Log($"[TurnManager] ExecuteEnemyMovePhase: skipping defeated enemy " +
                              $"(tokenId={enemy.tokenId}, name={enemy.enemyName})");
                    continue;
                }

                int fromNode = enemy.currentNodeId;
                int toNode = DetermineEnemyMove(enemy);

                if (toNode == fromNode || toNode < 0)
                {
                    Debug.Log($"[TurnManager] ExecuteEnemyMovePhase: enemy stays put " +
                              $"(tokenId={enemy.tokenId}, name={enemy.enemyName}, nodeId={fromNode}, behavior={enemy.behavior})");
                    continue;
                }

                // Update map node tracking
                MapNode fromMapNode = mapGraph.GetNode(fromNode);
                MapNode toMapNode = mapGraph.GetNode(toNode);

                if (fromMapNode != null)
                {
                    fromMapNode.enemyTokenIds.Remove(enemy.tokenId);
                }
                if (toMapNode != null)
                {
                    toMapNode.enemyTokenIds.Add(enemy.tokenId);
                }

                enemy.currentNodeId = toNode;

                Debug.Log($"[TurnManager] ExecuteEnemyMovePhase: enemy moved (tokenId={enemy.tokenId}, " +
                          $"name={enemy.enemyName}, from={fromNode}, to={toNode}, behavior={enemy.behavior})");

                EventBus.OnEnemyMoved?.Invoke(enemy.tokenId, fromNode, toNode);

                yield return new WaitForSeconds(0.1f);
            }

            // Refresh map after enemy movement (updates token positions on visible nodes)
            RefreshMapVisuals();

            Debug.Log($"[TurnManager] ExecuteEnemyMovePhase: EnemyMove phase complete (turn={currentTurn})");
        }

        private int DetermineEnemyMove(EnemyToken enemy)
        {
            Debug.Log($"[TurnManager] DetermineEnemyMove: calculating move for enemy " +
                      $"(tokenId={enemy.tokenId}, name={enemy.enemyName}, behavior={enemy.behavior}, " +
                      $"currentNode={enemy.currentNodeId}, speed={enemy.speed})");

            MapNode currentNode = mapGraph.GetNode(enemy.currentNodeId);
            if (currentNode == null)
            {
                Debug.LogWarning($"[TurnManager] DetermineEnemyMove: current node is null " +
                                 $"(tokenId={enemy.tokenId}, nodeId={enemy.currentNodeId})");
                return enemy.currentNodeId;
            }

            var neighbors = mapGraph.GetNeighbors(enemy.currentNodeId);
            if (neighbors.Count == 0)
            {
                Debug.Log($"[TurnManager] DetermineEnemyMove: no neighbors, staying put (tokenId={enemy.tokenId})");
                return enemy.currentNodeId;
            }

            switch (enemy.behavior)
            {
                case EnemyBehavior.Guard:
                    Debug.Log($"[TurnManager] DetermineEnemyMove: Guard behavior — staying put (tokenId={enemy.tokenId})");
                    return enemy.currentNodeId;

                case EnemyBehavior.Ambush:
                    // Ambush enemies stay hidden until a hero is adjacent
                    bool heroAdjacent = neighbors.Any(n => n.heroTokenIds.Count > 0);
                    if (heroAdjacent)
                    {
                        var heroNode = neighbors.First(n => n.heroTokenIds.Count > 0);
                        Debug.Log($"[TurnManager] DetermineEnemyMove: Ambush triggered — moving to hero node " +
                                  $"(tokenId={enemy.tokenId}, targetNode={heroNode.nodeId})");
                        return heroNode.nodeId;
                    }
                    Debug.Log($"[TurnManager] DetermineEnemyMove: Ambush — no adjacent heroes, staying put " +
                              $"(tokenId={enemy.tokenId})");
                    return enemy.currentNodeId;

                case EnemyBehavior.Chase:
                    // Chase: move toward nearest hero
                    int nearestHeroNode = FindNearestHeroNode(enemy.currentNodeId);
                    if (nearestHeroNode < 0)
                    {
                        Debug.Log($"[TurnManager] DetermineEnemyMove: Chase — no heroes found, patrol instead " +
                                  $"(tokenId={enemy.tokenId})");
                        goto case EnemyBehavior.Patrol;
                    }
                    var chasePath = mapGraph.ShortestPath(enemy.currentNodeId, nearestHeroNode);
                    if (chasePath.Count > 1)
                    {
                        int stepsToTake = Mathf.Min(enemy.speed, chasePath.Count - 1);
                        int targetNode = chasePath[stepsToTake];
                        Debug.Log($"[TurnManager] DetermineEnemyMove: Chase — moving toward hero " +
                                  $"(tokenId={enemy.tokenId}, nearestHeroNode={nearestHeroNode}, " +
                                  $"steps={stepsToTake}, moveTo={targetNode})");
                        return targetNode;
                    }
                    return enemy.currentNodeId;

                case EnemyBehavior.Patrol:
                default:
                    // Patrol: move to random neighbor within home zone if possible
                    var zoneNeighbors = neighbors.Where(n => n.zone == enemy.homeZone).ToList();
                    var validTargets = zoneNeighbors.Count > 0 ? zoneNeighbors : neighbors;
                    int randomIndex = SeededRandom.Range(0, validTargets.Count);
                    int patrolTarget = validTargets[randomIndex].nodeId;
                    Debug.Log($"[TurnManager] DetermineEnemyMove: Patrol — moving to random neighbor " +
                              $"(tokenId={enemy.tokenId}, targetNode={patrolTarget}, " +
                              $"zoneNeighborCount={zoneNeighbors.Count}, totalNeighborCount={neighbors.Count})");
                    return patrolTarget;
            }
        }

        private int FindNearestHeroNode(int fromNodeId)
        {
            Debug.Log($"[TurnManager] FindNearestHeroNode: searching from nodeId={fromNodeId}");

            int nearestNode = -1;
            int nearestDist = int.MaxValue;

            foreach (var hero in deployedHeroes)
            {
                if (hero.isInjured) continue;

                int dist = mapGraph.GetDistance(fromNodeId, hero.currentNodeId);
                if (dist >= 0 && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestNode = hero.currentNodeId;
                }
            }

            Debug.Log($"[TurnManager] FindNearestHeroNode: result (fromNodeId={fromNodeId}, " +
                      $"nearestNode={nearestNode}, distance={nearestDist})");
            return nearestNode;
        }

        // ── Phase 5: Combat ──────────────────────────────────────────────

        private IEnumerator ExecuteCombatPhase()
        {
            currentPhase = GamePhase.Combat;
            Debug.Log($"[TurnManager] ExecuteCombatPhase: entering Combat phase (turn={currentTurn})");
            EventBus.OnPhaseChanged?.Invoke(GamePhase.Combat);

            // Find all nodes with both heroes and enemies
            var combatNodes = new List<int>();
            foreach (var node in mapGraph.GetAllNodes())
            {
                bool hasHeroes = node.heroTokenIds.Any(id => deployedHeroes.Any(h => h.tokenId == id && !h.isInjured));
                bool hasEnemies = node.enemyTokenIds.Any(id => allEnemies.Any(e => e.tokenId == id && !e.isDefeated));

                if (hasHeroes && hasEnemies)
                {
                    combatNodes.Add(node.nodeId);
                    Debug.Log($"[TurnManager] ExecuteCombatPhase: combat detected at node " +
                              $"(nodeId={node.nodeId}, heroes={node.heroTokenIds.Count}, enemies={node.enemyTokenIds.Count})");
                }
            }

            Debug.Log($"[TurnManager] ExecuteCombatPhase: total combat nodes={combatNodes.Count}");

            if (combatNodes.Count > 0)
            {
                // Transition to Combat scene
                Debug.Log("[TurnManager] ExecuteCombatPhase: transitioning to Combat scene");
                yield return StartCoroutine(TransitionToScene("Combat"));

                var combatUI = Object.FindAnyObjectByType<CombatUI>();
                Debug.Log($"[TurnManager] ExecuteCombatPhase: CombatUI={(combatUI != null ? "found" : "NOT FOUND")}");

                foreach (int nodeId in combatNodes)
                {
                    yield return StartCoroutine(ResolveCombatAtNodeStepped(nodeId, combatUI));
                }

                // Transition back to GameMap
                Debug.Log("[TurnManager] ExecuteCombatPhase: transitioning back to GameMap");
                yield return StartCoroutine(TransitionToScene("GameMap"));
                yield return StartCoroutine(WaitForGameManagerInit());
            }

            // Refresh map after combat (enemies/heroes may have been removed)
            RefreshMapVisuals();
            UpdateHUD();
            Debug.Log($"[TurnManager] ExecuteCombatPhase: Combat phase complete (turn={currentTurn})");
        }

        /// <summary>
        /// Stepped combat: uses CombatResolver's stepped API with tactical card windows between rounds.
        /// </summary>
        private IEnumerator ResolveCombatAtNodeStepped(int nodeId, CombatUI combatUI)
        {
            Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: starting combat (nodeId={nodeId})");

            MapNode node = mapGraph.GetNode(nodeId);
            if (node == null)
            {
                Debug.LogError($"[TurnManager] ResolveCombatAtNodeStepped: node is null (nodeId={nodeId})");
                yield break;
            }

            // Gather participating heroes and enemies
            var heroesInCombat = deployedHeroes
                .Where(h => h.currentNodeId == nodeId && !h.isInjured)
                .OrderBy(h => h.EffectiveInitiative)
                .ToList();
            var enemiesInCombat = allEnemies
                .Where(e => e.currentNodeId == nodeId && !e.isDefeated)
                .ToList();

            Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: participants (nodeId={nodeId}, " +
                      $"heroes={heroesInCombat.Count}, enemies={enemiesInCombat.Count})");

            // Apply first-combat colony buff if applicable
            if (colonyGraph.HasEffect(ColonyEffect.FirstCombatBuff))
            {
                int buff = colonyGraph.GetEffectValue(ColonyEffect.FirstCombatBuff);
                Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: applying FirstCombatBuff +{buff} to all heroes (nodeId={nodeId})");
                foreach (var hero in heroesInCombat)
                {
                    hero.colonyBonusCombat += buff;
                }
            }

            // Use TurnManager's own combat resolver (survives scene transitions)
            if (combatResolver == null)
            {
                Debug.LogError("[TurnManager] ResolveCombatAtNodeStepped: combatResolver is null — was Initialize() called?");
                yield break;
            }

            // Step 1: Begin combat (pre-combat phase)
            bool isAmbush = node.fogState == FogState.Hidden;
            var (ctx, combatResult) = combatResolver.BeginCombat(heroesInCombat, enemiesInCombat, nodeId, isAmbush);

            // Show combat UI
            if (combatUI != null)
            {
                var tacticalCards = deckManager.AvailableTactical;
                Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: showing CombatUI (nodeId={nodeId}, " +
                          $"heroes={heroesInCombat.Count}, enemies={enemiesInCombat.Count}, tacticals={tacticalCards.Count})");
                combatUI.ShowCombat(heroesInCombat, enemiesInCombat, nodeId, tacticalCards,
                    onContinueCallback: () => { tacticalWindowContinuePressed = true; },
                    onTacticalCallback: (card) => { HandleTacticalCardPlayed(card, heroesInCombat, enemiesInCombat, nodeId, ctx, combatUI); });
                combatUI.SetTacticalWindowActive(true);
            }

            // Step 2: Tactical window #1 (before first round) — yield until player clicks Continue or plays cards
            if (combatUI != null && heroesInCombat.Any(h => h.IsAlive) && enemiesInCombat.Any(e => e.IsAlive))
            {
                Debug.Log("[TurnManager] ResolveCombatAtNodeStepped: TACTICAL WINDOW #1 (pre-combat)");
                yield return StartCoroutine(WaitForTacticalWindow(combatUI));
            }

            // Step 3: Combat round loop
            bool combatContinues = heroesInCombat.Any(h => h.IsAlive) && enemiesInCombat.Any(e => e.IsAlive);
            while (combatContinues && combatResult.roundsFought < 100)
            {
                if (ctx.retreatTriggered)
                {
                    Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: retreat triggered (nodeId={nodeId})");
                    break;
                }

                // Execute one round
                var roundResult = combatResolver.ExecuteOneRound(heroesInCombat, enemiesInCombat, ctx, combatResult);

                // Update combat UI with round results
                if (combatUI != null)
                {
                    combatUI.UpdateRound(combatResult.roundsFought, roundResult.heroStrength, roundResult.enemyStrength);
                    // Refresh HP bars from cached token state
                    var damageEvents = BuildDamageEventsFromState(heroesInCombat, enemiesInCombat);
                    combatUI.ShowDamageResult(damageEvents);
                }

                Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: round {combatResult.roundsFought} complete " +
                          $"(heroStr={roundResult.heroStrength}, enemyStr={roundResult.enemyStrength}, continues={roundResult.combatContinues})");

                combatContinues = roundResult.combatContinues;

                if (!combatContinues) break;

                // Between-round tactical window
                if (combatUI != null)
                {
                    combatUI.SetTacticalWindowActive(true);
                    Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: TACTICAL WINDOW (after round {combatResult.roundsFought})");
                    yield return StartCoroutine(WaitForTacticalWindow(combatUI));
                }
                else
                {
                    // No UI — yield one frame to avoid blocking
                    yield return null;
                }
            }

            // Step 4: Finalize combat
            combatResolver.EndCombat(heroesInCombat, enemiesInCombat, combatResult, ctx);

            // Step 5: Process combat outcome (boss tracking, injuries, scores)
            ProcessCombatOutcome(combatResult, heroesInCombat, enemiesInCombat, node);

            // Remove FirstCombatBuff temporary bonus
            if (colonyGraph.HasEffect(ColonyEffect.FirstCombatBuff))
            {
                int buff = colonyGraph.GetEffectValue(ColonyEffect.FirstCombatBuff);
                foreach (var hero in heroesInCombat)
                {
                    hero.colonyBonusCombat -= buff;
                }
                Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: removed FirstCombatBuff (nodeId={nodeId})");
            }

            // Step 6: Card rewards for combat victory (Phase 3)
            if (combatResult.heroesWon && combatResult.defeatedEnemyTokenIds.Count > 0)
            {
                Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: combat victory — offering card reward (nodeId={nodeId})");
                yield return StartCoroutine(OfferCardReward(nodeId, node.zone));
            }

            // Hide combat UI
            if (combatUI != null)
            {
                combatUI.HideCombat();
                Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: hid CombatUI after node {nodeId}");
            }

            Debug.Log($"[TurnManager] ResolveCombatAtNodeStepped: combat resolved (nodeId={nodeId}, " +
                      $"rounds={combatResult.roundsFought}, heroesWon={combatResult.heroesWon})");
        }

        // ── Tactical window helpers ─────────────────────────────────────────

        private bool tacticalWindowContinuePressed;

        /// <summary>
        /// Waits for the player to click Continue or for auto-advance to trigger.
        /// Player can play tactical cards during this window.
        /// Skips instantly if no tactical cards are available.
        /// </summary>
        private IEnumerator WaitForTacticalWindow(CombatUI combatUI)
        {
            // Skip tactical window entirely if no tactical cards available
            if (deckManager == null || deckManager.AvailableTactical.Count == 0)
            {
                Debug.Log("[TurnManager] WaitForTacticalWindow: no tactical cards available — skipping window");
                if (combatUI != null) combatUI.SetTacticalWindowActive(false);
                yield return null; // Just yield one frame
                yield break;
            }

            tacticalWindowContinuePressed = false;
            float autoDelay = 0f;
            float waitMultiplier = GameSettings.Instance?.BattleWaitMultiplier ?? 1f;
            // When auto-advance is ON, use a short delay; when OFF, use a longer delay
            // but always eventually auto-advance to prevent infinite waits
            float autoOnTime = 0.5f * waitMultiplier;
            float autoOffTimeout = 30f; // max 30s wait even without auto-advance

            while (!tacticalWindowContinuePressed)
            {
                autoDelay += Time.deltaTime;

                // Auto-advance: fast if toggle is on, or after long timeout if off
                bool autoOn = combatUI == null || combatUI.IsAutoAdvance;
                float threshold = autoOn ? autoOnTime : autoOffTimeout;
                if (autoDelay >= threshold)
                {
                    Debug.Log($"[TurnManager] WaitForTacticalWindow: auto-advance triggered (autoOn={autoOn}, elapsed={autoDelay:F2}s)");
                    break;
                }

                // Process any queued tactical actions from previous frame
                while (pendingTacticalActions.Count > 0)
                {
                    var action = pendingTacticalActions.Dequeue();
                    Debug.Log($"[TurnManager] WaitForTacticalWindow: processing queued tactical action (card={action.card?.cardName})");
                }

                yield return null;
            }

            if (combatUI != null)
            {
                combatUI.SetTacticalWindowActive(false);
            }
        }

        /// <summary>
        /// Called when a tactical card is played during a combat tactical window.
        /// </summary>
        private void HandleTacticalCardPlayed(CardDefinitionSO card, List<HeroToken> heroes,
            List<EnemyToken> enemies, int nodeId, CombatContext ctx, CombatUI combatUI)
        {
            Debug.Log($"[TurnManager] HandleTacticalCardPlayed: card={card?.cardName}, nodeId={nodeId}");

            if (card == null || card.cardType != CardType.Tactical)
            {
                Debug.LogWarning($"[TurnManager] HandleTacticalCardPlayed: invalid card (card={card?.cardName}, type={card?.cardType})");
                return;
            }

            // Apply the tactical effect
            bool success = TacticalEffectProcessor.ApplyTacticalCard(card, heroes, enemies, nodeId, ctx);

            if (success)
            {
                // Remove from deck manager (permanently used)
                deckManager.UseTacticalCard(card);

                // Remove from UI
                if (combatUI != null)
                {
                    combatUI.RemoveTacticalCard(card);
                }

                // Track in combat result (we don't have a direct ref, but EventBus fires from TacticalEffectProcessor)
                Debug.Log($"[TurnManager] HandleTacticalCardPlayed: successfully applied '{card.cardName}' (type={card.tacticalType})");

                // Notify UI
                EventBus.OnNotification?.Invoke($"Played: {card.cardName}", new Color(0.6f, 0.8f, 1f));
            }
            else
            {
                Debug.LogWarning($"[TurnManager] HandleTacticalCardPlayed: card '{card.cardName}' had no effect");
                EventBus.OnNotification?.Invoke($"{card.cardName} had no effect!", new Color(1f, 0.5f, 0.3f));
            }
        }

        /// <summary>
        /// Processes combat outcome: boss tracking, hero injuries, score updates, map cleanup.
        /// </summary>
        private void ProcessCombatOutcome(CombatResult combatResult, List<HeroToken> heroesInCombat,
            List<EnemyToken> enemiesInCombat, MapNode node)
        {
            Debug.Log($"[TurnManager] ProcessCombatOutcome: processing (nodeId={node.nodeId}, heroesWon={combatResult.heroesWon}, " +
                      $"defeated={combatResult.defeatedEnemyTokenIds.Count}, injured={combatResult.injuredHeroTokenIds.Count})");

            // Track defeated enemies
            foreach (int enemyTokenId in combatResult.defeatedEnemyTokenIds)
            {
                totalEnemiesDefeated++;
                var enemy = allEnemies.Find(e => e.tokenId == enemyTokenId);
                if (enemy != null)
                {
                    bool isBoss = enemy.strength >= 8;
                    bool isPiper = enemy.homeZone == NodeType.PiedPiper;

                    if (isBoss)
                    {
                        totalZoneBossesDefeated++;
                        Debug.Log($"[TurnManager] ProcessCombatOutcome: ZONE BOSS DEFEATED! (tokenId={enemyTokenId}, name={enemy.enemyName}, totalBosses={totalZoneBossesDefeated})");
                    }

                    if (isPiper)
                    {
                        piedPiperDefeated = true;
                        Debug.Log($"[TurnManager] ProcessCombatOutcome: PIED PIPER DEFEATED! (tokenId={enemyTokenId})");
                    }

                    // Notify RunManager
                    var runManager = RunManager.Instance;
                    if (runManager != null)
                    {
                        runManager.RecordEnemyDefeated(isBoss, isPiper);
                    }

                    node.enemyTokenIds.Remove(enemyTokenId);
                }
            }

            // Track injured heroes
            foreach (int heroTokenId in combatResult.injuredHeroTokenIds)
            {
                heroesEverInjured.Add(heroTokenId);
                var hero = allHeroes.Find(h => h.tokenId == heroTokenId);
                if (hero != null)
                {
                    HandleHeroInjury(hero, node);
                }
            }

            Debug.Log($"[TurnManager] ProcessCombatOutcome: complete (totalEnemiesDefeated={totalEnemiesDefeated}, " +
                      $"totalBosses={totalZoneBossesDefeated}, piedPiper={piedPiperDefeated})");
        }

        /// <summary>
        /// Builds CombatUI.DamageEvent list from current token HP state for UI display.
        /// </summary>
        private List<CombatUI.DamageEvent> BuildDamageEventsFromState(List<HeroToken> heroes, List<EnemyToken> enemies)
        {
            var events = new List<CombatUI.DamageEvent>();
            foreach (var hero in heroes)
            {
                events.Add(new CombatUI.DamageEvent
                {
                    targetId = hero.tokenId,
                    damage = 0,
                    isHero = true,
                    isDefeated = !hero.IsAlive
                });
            }
            foreach (var enemy in enemies)
            {
                events.Add(new CombatUI.DamageEvent
                {
                    targetId = enemy.tokenId,
                    damage = 0,
                    isHero = false,
                    isDefeated = !enemy.IsAlive
                });
            }
            return events;
        }

        // ── Card Rewards (Phase 3) ──────────────────────────────────────────

        /// <summary>
        /// Offers a 1-of-3 card reward after combat victory. Player can skip.
        /// </summary>
        private IEnumerator OfferCardReward(int nodeId, NodeType zone)
        {
            Debug.Log($"[TurnManager] OfferCardReward: generating reward options (nodeId={nodeId}, zone={zone})");

            // Generate 3 random reward cards weighted by zone
            var rewardCards = GenerateRewardCards(zone, 3);

            if (rewardCards.Count == 0)
            {
                Debug.Log("[TurnManager] OfferCardReward: no reward cards available — skipping");
                yield break;
            }

            Debug.Log($"[TurnManager] OfferCardReward: offering {rewardCards.Count} cards: " +
                      string.Join(", ", rewardCards.ConvertAll(c => c.cardName)));

            // Store reward cards for the CardReward scene to pick up
            pendingRewardCards = rewardCards;
            rewardSelectionMade = false;
            selectedRewardCard = null;

            // Transition to CardReward scene
            Debug.Log("[TurnManager] OfferCardReward: transitioning to CardReward scene");
            yield return StartCoroutine(TransitionToScene("CardReward"));

            // CardRewardUI.Start() in the scene will find TurnManager and call Show with pending cards
            var rewardUI = Object.FindAnyObjectByType<CardRewardUI>();
            if (rewardUI != null)
            {
                rewardUI.Show(pendingRewardCards,
                    onCardSelected: (card) =>
                    {
                        selectedRewardCard = card;
                        rewardSelectionMade = true;
                        Debug.Log($"[TurnManager] OfferCardReward: player selected '{card.cardName}'");
                    },
                    onSkip: () =>
                    {
                        rewardSelectionMade = true;
                        Debug.Log("[TurnManager] OfferCardReward: player skipped reward");
                    });
            }
            else
            {
                Debug.LogWarning("[TurnManager] OfferCardReward: CardRewardUI not found in CardReward scene — auto-skipping");
                rewardSelectionMade = true;
            }

            // Wait for selection
            while (!rewardSelectionMade)
            {
                yield return null;
            }

            // Apply selection
            if (selectedRewardCard != null)
            {
                AddCardToPool(selectedRewardCard);
                EventBus.OnCardRewardSelected?.Invoke(selectedRewardCard);
                EventBus.OnNotification?.Invoke($"Added: {selectedRewardCard.cardName}", new Color(0.4f, 1f, 0.4f));
            }
            else
            {
                EventBus.OnCardRewardSkipped?.Invoke();
            }

            pendingRewardCards = null;

            // Transition back to Combat scene to continue resolving remaining nodes
            Debug.Log("[TurnManager] OfferCardReward: transitioning back to Combat scene");
            yield return StartCoroutine(TransitionToScene("Combat"));
        }

        /// <summary>
        /// Generates N random reward cards from the card database, weighted by zone.
        /// Prefers cards matching the current zone tier: Wilderness=Common, Farmland=Uncommon, Town=Rare+
        /// </summary>
        private List<CardDefinitionSO> GenerateRewardCards(NodeType zone, int count)
        {
            Debug.Log($"[TurnManager] GenerateRewardCards: zone={zone}, count={count}");

            var cardDb = CardDatabase.Instance;
            if (cardDb == null)
            {
                Debug.LogWarning("[TurnManager] GenerateRewardCards: CardDatabase not available");
                return new List<CardDefinitionSO>();
            }

            // Pool eligible reward cards (heroes, equipment, tactical — not colony)
            var pool = new List<CardDefinitionSO>();
            foreach (var card in cardDb.AllCards)
            {
                if (card.cardType == CardType.Hero || card.cardType == CardType.Equipment || card.cardType == CardType.Tactical)
                {
                    pool.Add(card);
                }
            }

            if (pool.Count == 0)
            {
                Debug.LogWarning("[TurnManager] GenerateRewardCards: no eligible cards in pool");
                return new List<CardDefinitionSO>();
            }

            // Weight by rarity matching zone
            CardRarity preferred;
            switch (zone)
            {
                case NodeType.Wilderness: preferred = CardRarity.Common; break;
                case NodeType.Farmland: preferred = CardRarity.Uncommon; break;
                case NodeType.Town: preferred = CardRarity.Rare; break;
                default: preferred = CardRarity.Common; break;
            }

            // Weighted random selection (preferred rarity = 3x weight)
            var result = new List<CardDefinitionSO>();
            var used = new HashSet<int>();

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                // Build weighted list
                float totalWeight = 0;
                var weights = new List<float>();
                foreach (var card in pool)
                {
                    float w = card.rarity == preferred ? 3f : 1f;
                    if (used.Contains(card.cardId)) w = 0f;
                    weights.Add(w);
                    totalWeight += w;
                }

                if (totalWeight <= 0) break;

                // Pick randomly
                float roll = Random.Range(0f, totalWeight);
                float cumulative = 0;
                for (int j = 0; j < pool.Count; j++)
                {
                    cumulative += weights[j];
                    if (roll <= cumulative)
                    {
                        result.Add(pool[j]);
                        used.Add(pool[j].cardId);
                        Debug.Log($"[TurnManager] GenerateRewardCards: selected '{pool[j].cardName}' (rarity={pool[j].rarity}, type={pool[j].cardType})");
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Adds a reward card to the appropriate deck manager pool.
        /// </summary>
        private void AddCardToPool(CardDefinitionSO card)
        {
            Debug.Log($"[TurnManager] AddCardToPool: adding '{card.cardName}' (type={card.cardType})");
            deckManager.AddCardToPool(card);
        }

        private void HandleHeroInjury(HeroToken hero, MapNode node)
        {
            Debug.Log($"[TurnManager] HandleHeroInjury: processing injury (tokenId={hero.tokenId}, " +
                      $"name={hero.cardDef?.cardName ?? "unknown"}, nodeId={node.nodeId})");

            // Resources were already dropped by HeroToken.Injure()
            // Equipment was already unequipped by HeroToken.Injure()

            // Return equipment to deck manager
            // (Equipment was cleared by UnequipAll, but we need to track in DeckManager)
            deckManager.InjureHero(hero.cardDef);

            // Remove from map node
            node.heroTokenIds.Remove(hero.tokenId);

            // Remove from deployed list
            deployedHeroes.Remove(hero);

            Debug.Log($"[TurnManager] HandleHeroInjury: hero removed from play (tokenId={hero.tokenId}, " +
                      $"turnsUntilRecovery={hero.turnsUntilRecovery})");
        }

        // ── Phase 6: Gather ──────────────────────────────────────────────

        private IEnumerator ExecuteGatherPhase()
        {
            currentPhase = GamePhase.Gather;
            Debug.Log($"[TurnManager] ExecuteGatherPhase: entering Gather phase (turn={currentTurn}, " +
                      $"deployedHeroes={deployedHeroes.Count})");
            EventBus.OnPhaseChanged?.Invoke(GamePhase.Gather);

            var result = GatherPhase.Execute(deployedHeroes, mapGraph, colonyGraph);

            totalResourcesGathered += result.totalGathered;

            Debug.Log($"[TurnManager] ExecuteGatherPhase: gather complete (totalGathered={result.totalGathered}, " +
                      $"runTotalGathered={totalResourcesGathered})");

            // Fire events for each hero's gathering
            foreach (var kvp in result.gatheredByHero)
            {
                int heroTokenId = kvp.Key;
                var hero = allHeroes.Find(h => h.tokenId == heroTokenId);
                if (hero != null)
                {
                    foreach (var resKvp in kvp.Value)
                    {
                        if (resKvp.Value > 0)
                        {
                            EventBus.OnResourceGathered?.Invoke(hero, resKvp.Key, resKvp.Value);
                        }
                    }
                }
            }

            // Auto-deposit for heroes on colony node
            foreach (var hero in deployedHeroes)
            {
                if (hero.currentNodeId == mapGraph.ColonyNodeId && hero.TotalCarried > 0)
                {
                    Debug.Log($"[TurnManager] ExecuteGatherPhase: auto-depositing for hero at colony " +
                              $"(tokenId={hero.tokenId})");
                    resourceManager.DepositHeroResources(hero);
                }
            }

            // Refresh map after gathering (resource indicators change)
            RefreshMapVisuals();
            UpdateHUD();

            yield return new WaitForSeconds(0.15f);

            Debug.Log($"[TurnManager] ExecuteGatherPhase: Gather phase complete (turn={currentTurn})");
        }

        // ── Phase 7: Cleanup ─────────────────────────────────────────────

        private IEnumerator ExecuteCleanupPhase()
        {
            currentPhase = GamePhase.Cleanup;
            Debug.Log($"[TurnManager] ExecuteCleanupPhase: entering Cleanup phase (turn={currentTurn})");
            EventBus.OnPhaseChanged?.Invoke(GamePhase.Cleanup);

            bool hasEmergencyRations = colonyGraph.HasEffect(ColonyEffect.FoodSpoilageImmunity);

            var cleanupResult = CleanupPhase.Execute(
                deployedHeroes, allEnemies, resourceManager, colonyGraph, mapGraph, hasEmergencyRations);

            Debug.Log($"[TurnManager] ExecuteCleanupPhase: cleanup result " +
                      $"(foodConsumed={cleanupResult.foodConsumed}, foodDeficit={cleanupResult.foodDeficit}, " +
                      $"foodHealing={cleanupResult.foodHealing}, healedHeroes={cleanupResult.healedHeroes.Count}, " +
                      $"unfedHeroes={cleanupResult.unfedHeroIds.Count}, " +
                      $"respawnedEnemies={cleanupResult.respawnedEnemyIds.Count}, " +
                      $"recoveredHeroes={cleanupResult.recoveredHeroIds.Count})");

            // Log healed heroes
            foreach (var (tokenId, healed) in cleanupResult.healedHeroes)
            {
                var hero = allHeroes.Find(h => h.tokenId == tokenId);
                Debug.Log($"[TurnManager] ExecuteCleanupPhase: hero healed " +
                          $"(tokenId={tokenId}, name={hero?.cardDef?.cardName ?? "unknown"}, " +
                          $"healed={healed}, hp={hero?.currentHP ?? 0}/{hero?.EffectiveHP ?? 0})");
            }

            // Handle starvation for unfed heroes
            foreach (int heroId in cleanupResult.unfedHeroIds)
            {
                var hero = allHeroes.Find(h => h.tokenId == heroId);
                if (hero != null && !hero.isInjured)
                {
                    Debug.Log($"[TurnManager] ExecuteCleanupPhase: starving hero " +
                              $"(tokenId={heroId}, name={hero.cardDef?.cardName ?? "unknown"})");
                    bool injured = hero.TakeDamage(1);
                    if (injured)
                    {
                        heroesEverInjured.Add(hero.tokenId);
                        MapNode node = mapGraph.GetNode(hero.currentNodeId);
                        if (node != null)
                        {
                            HandleHeroInjury(hero, node);
                        }
                    }
                }
            }

            // Handle hero recovery
            foreach (int heroId in cleanupResult.recoveredHeroIds)
            {
                var hero = allHeroes.Find(h => h.tokenId == heroId);
                if (hero != null)
                {
                    hero.isInjured = false;
                    hero.turnsUntilRecovery = 0;
                    hero.currentHP = hero.EffectiveHP;
                    deckManager.RecoverHero(hero.cardDef);

                    Debug.Log($"[TurnManager] ExecuteCleanupPhase: hero recovered " +
                              $"(tokenId={heroId}, name={hero.cardDef?.cardName ?? "unknown"}, hp={hero.currentHP})");
                }
            }

            // Handle enemy respawns
            foreach (int enemyId in cleanupResult.respawnedEnemyIds)
            {
                var enemy = allEnemies.Find(e => e.tokenId == enemyId);
                if (enemy != null)
                {
                    // Find random unoccupied node in enemy's home zone
                    int respawnNode = FindRespawnNode(enemy.homeZone);
                    if (respawnNode >= 0)
                    {
                        enemy.Respawn(respawnNode);
                        MapNode node = mapGraph.GetNode(respawnNode);
                        if (node != null)
                        {
                            node.enemyTokenIds.Add(enemy.tokenId);
                        }
                        Debug.Log($"[TurnManager] ExecuteCleanupPhase: enemy respawned " +
                                  $"(tokenId={enemyId}, name={enemy.enemyName}, nodeId={respawnNode})");
                    }
                    else
                    {
                        Debug.LogWarning($"[TurnManager] ExecuteCleanupPhase: no valid respawn node for enemy " +
                                         $"(tokenId={enemyId}, name={enemy.enemyName}, zone={enemy.homeZone})");
                    }
                }
            }

            // Refresh map after cleanup (respawned enemies, recovered heroes)
            RefreshMapVisuals();
            UpdateHUD();

            yield return new WaitForSeconds(0.15f);

            Debug.Log($"[TurnManager] ExecuteCleanupPhase: Cleanup phase complete (turn={currentTurn})");
        }

        private int FindRespawnNode(NodeType zone)
        {
            Debug.Log($"[TurnManager] FindRespawnNode: searching for empty node in zone={zone}");

            var zoneNodes = mapGraph.GetNodesInZone(zone);
            var unoccupied = zoneNodes
                .Where(n => n.heroTokenIds.Count == 0 && n.enemyTokenIds.Count == 0)
                .ToList();

            if (unoccupied.Count == 0)
            {
                // Fall back to any node in zone without heroes
                unoccupied = zoneNodes.Where(n => n.heroTokenIds.Count == 0).ToList();
            }

            if (unoccupied.Count == 0)
            {
                Debug.LogWarning($"[TurnManager] FindRespawnNode: no valid nodes found (zone={zone})");
                return -1;
            }

            int index = SeededRandom.Range(0, unoccupied.Count);
            int nodeId = unoccupied[index].nodeId;
            Debug.Log($"[TurnManager] FindRespawnNode: selected node (zone={zone}, nodeId={nodeId}, " +
                      $"candidateCount={unoccupied.Count})");
            return nodeId;
        }

        // ── Player action methods (called by UI) ────────────────────────

        public void PlayerPlayColonyCard(ColonyCardDefinitionSO card, int targetNodeId)
        {
            Debug.Log($"[TurnManager] PlayerPlayColonyCard: queuing action " +
                      $"(card={card?.cardName ?? "NULL"}, targetNodeId={targetNodeId}, currentPhase={currentPhase})");

            if (currentPhase != GamePhase.ColonyDeploy)
            {
                Debug.LogWarning($"[TurnManager] PlayerPlayColonyCard: wrong phase " +
                                 $"(currentPhase={currentPhase}, expected=ColonyDeploy)");
                return;
            }

            pendingColonyActions.Enqueue(new ColonyCardAction { card = card, targetNodeId = targetNodeId });
        }

        public void PlayerDeployHero(CardDefinitionSO heroDef, int targetNodeId,
                                     CardDefinitionSO offensive, CardDefinitionSO defensive,
                                     CardDefinitionSO utility)
        {
            Debug.Log($"[TurnManager] PlayerDeployHero: queuing action " +
                      $"(hero={heroDef?.cardName ?? "NULL"}, targetNode={targetNodeId}, " +
                      $"offensive={offensive?.cardName ?? "none"}, defensive={defensive?.cardName ?? "none"}, " +
                      $"utility={utility?.cardName ?? "none"}, currentPhase={currentPhase})");

            if (currentPhase != GamePhase.Deploy)
            {
                Debug.LogWarning($"[TurnManager] PlayerDeployHero: wrong phase " +
                                 $"(currentPhase={currentPhase}, expected=Deploy)");
                return;
            }

            pendingDeployActions.Enqueue(new DeployAction
            {
                heroDef = heroDef,
                targetNodeId = targetNodeId,
                offensive = offensive,
                defensive = defensive,
                utility = utility
            });
        }

        public void PlayerRetargetHero(int heroTokenId, int newTargetNodeId)
        {
            Debug.Log($"[TurnManager] PlayerRetargetHero: queuing action " +
                      $"(heroTokenId={heroTokenId}, newTargetNodeId={newTargetNodeId}, currentPhase={currentPhase})");

            if (currentPhase != GamePhase.Deploy)
            {
                Debug.LogWarning($"[TurnManager] PlayerRetargetHero: wrong phase " +
                                 $"(currentPhase={currentPhase}, expected=Deploy)");
                return;
            }

            pendingRetargetActions.Enqueue(new RetargetAction
            {
                heroTokenId = heroTokenId,
                newTargetNodeId = newTargetNodeId
            });
        }

        public void PlayerEndPhase()
        {
            Debug.Log($"[TurnManager] PlayerEndPhase: player ending phase " +
                      $"(currentPhase={currentPhase}, waitingForInput={waitingForPlayerInput})");

            if (!waitingForPlayerInput)
            {
                Debug.LogWarning("[TurnManager] PlayerEndPhase: not currently waiting for input — ignoring");
                return;
            }

            phaseEndRequested = true;
            Debug.Log($"[TurnManager] PlayerEndPhase: phase end requested (currentPhase={currentPhase})");
        }

        /// <summary>
        /// Called by HUD Continue button to advance past an auto-resolved phase.
        /// </summary>
        public void PlayerContinue()
        {
            Debug.Log($"[TurnManager] PlayerContinue: continue pressed (currentPhase={currentPhase})");
            continuePressed = true;
        }

        /// <summary>
        /// Pauses the turn loop on GameMap until the player clicks Continue.
        /// Shows the phase result and lets the player observe the map state.
        /// </summary>
        private IEnumerator WaitForPlayerContinue(string phaseCompleteName)
        {
            // Only pause if we're on GameMap
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameMap")
            {
                Debug.Log($"[TurnManager] WaitForPlayerContinue: not on GameMap, skipping pause for '{phaseCompleteName}'");
                yield break;
            }

            continuePressed = false;
            Debug.Log($"[TurnManager] WaitForPlayerContinue: waiting for player to continue after '{phaseCompleteName}'");

            // Notify HUD to show Continue button
            EventBus.OnNotification?.Invoke($"{phaseCompleteName} complete — click Continue", new Color(0.8f, 0.8f, 0.6f));

            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud != null)
            {
                hud.ShowContinueButton(true, phaseCompleteName);
            }

            while (!continuePressed && runActive)
            {
                yield return null;
            }

            if (hud != null)
            {
                hud.ShowContinueButton(false);
            }

            Debug.Log($"[TurnManager] WaitForPlayerContinue: player continued after '{phaseCompleteName}'");
        }

        /// <summary>
        /// ITurnManager.AdvancePhase — delegates to PlayerEndPhase for phase-end flow.
        /// </summary>
        public void AdvancePhase()
        {
            Debug.Log($"[TurnManager] AdvancePhase: delegating to PlayerEndPhase (currentPhase={currentPhase})");
            PlayerEndPhase();
        }

        public void PlayerPlayTacticalCard(CardDefinitionSO card, int targetNodeId)
        {
            Debug.Log($"[TurnManager] PlayerPlayTacticalCard: queuing action " +
                      $"(card={card?.cardName ?? "NULL"}, targetNodeId={targetNodeId}, currentPhase={currentPhase})");

            if (currentPhase != GamePhase.Combat)
            {
                Debug.LogWarning($"[TurnManager] PlayerPlayTacticalCard: wrong phase " +
                                 $"(currentPhase={currentPhase}, expected=Combat)");
                return;
            }

            if (card == null || card.cardType != CardType.Tactical)
            {
                Debug.LogWarning($"[TurnManager] PlayerPlayTacticalCard: invalid card " +
                                 $"(card={card?.cardName ?? "NULL"}, type={card?.cardType})");
                return;
            }

            deckManager.UseTacticalCard(card);
            EventBus.OnTacticalCardPlayed?.Invoke(card);

            Debug.Log($"[TurnManager] PlayerPlayTacticalCard: tactical card played " +
                      $"(card={card.cardName}, targetNodeId={targetNodeId})");

            pendingTacticalActions.Enqueue(new TacticalCardAction
            {
                card = card,
                targetNodeId = targetNodeId
            });
        }

        // ── Win/loss checks ──────────────────────────────────────────────

        public bool CheckVictory()
        {
            bool victory = piedPiperDefeated;
            Debug.Log($"[TurnManager] CheckVictory: (piedPiperDefeated={piedPiperDefeated}, result={victory})");
            return victory;
        }

        public bool CheckDefeat()
        {
            // Defeat if: turn limit exceeded (handled by loop), or colony overrun
            bool colonyOverrun = false;

            if (mapGraph.ColonyNodeId >= 0)
            {
                MapNode colonyNode = mapGraph.GetNode(mapGraph.ColonyNodeId);
                if (colonyNode != null && colonyNode.enemyTokenIds.Count > 0)
                {
                    // Colony is overrun if enemies are present and no defenders
                    bool hasDefenders = colonyNode.heroTokenIds
                        .Any(id => deployedHeroes.Any(h => h.tokenId == id && !h.isInjured));

                    // Check colony defense buildings
                    bool hasWall = colonyGraph.HasEffect(ColonyEffect.ColonyDefenseWall);

                    if (!hasDefenders && !hasWall)
                    {
                        colonyOverrun = true;
                    }
                }
            }

            // Also check if all heroes are injured and no more can be deployed
            bool allHeroesDown = deployedHeroes.Count == 0 &&
                                 deckManager.AvailableHeroes.Count == 0 &&
                                 allHeroes.All(h => h.isInjured);

            bool defeated = colonyOverrun || (allHeroesDown && allHeroes.Count > 0);

            Debug.Log($"[TurnManager] CheckDefeat: (colonyOverrun={colonyOverrun}, allHeroesDown={allHeroesDown}, " +
                      $"deployedCount={deployedHeroes.Count}, availableHeroes={deckManager.AvailableHeroes.Count}, " +
                      $"result={defeated})");

            return defeated;
        }

        // ── End run ──────────────────────────────────────────────────────

        public void EndRun(bool victory)
        {
            Debug.Log($"[TurnManager] EndRun: ending run (victory={victory}, turn={currentTurn})");

            runActive = false;
            StopAllCoroutines();

            // Calculate score
            int heroesNeverInjured = allHeroes.Count(h => !heroesEverInjured.Contains(h.tokenId));

            var scoreInput = new ScoreInput
            {
                turnsUsed = currentTurn,
                deckSize = deckManager.TotalDeckSize,
                enemiesDefeated = totalEnemiesDefeated,
                zoneBossesDefeated = totalZoneBossesDefeated,
                piedPiperDefeated = piedPiperDefeated,
                totalResourcesGathered = totalResourcesGathered,
                colonyCardsPlayed = colonyCardsPlayed,
                heroesNeverInjured = heroesNeverInjured
            };

            var breakdown = ScoreCalculator.CalculateScore(scoreInput);

            Debug.Log($"[TurnManager] EndRun: score calculated (finalScore={breakdown.finalScore}, " +
                      $"turnScore={breakdown.turnScore}, deckMultiplier={breakdown.deckMultiplier:F2}, " +
                      $"enemyScore={breakdown.enemyScore}, resourceScore={breakdown.resourceScore}, " +
                      $"colonyScore={breakdown.colonyScore}, heroBonus={breakdown.heroBonus})");

            EventBus.OnRunComplete?.Invoke(victory);
            EventBus.OnScoreCalculated?.Invoke(breakdown.finalScore);

            Debug.Log($"[TurnManager] EndRun: events fired, run complete (victory={victory})");
        }

        // ── Map Visual Refresh ────────────────────────────────────────────

        /// <summary>
        /// Recalculates fog of war based on current hero positions and updates MapRenderer visuals.
        /// Should be called after any phase that changes hero positions or map state.
        /// </summary>
        private void RefreshMapVisuals()
        {
            if (SceneManager.GetActiveScene().name != "GameMap") return;

            Debug.Log("[TurnManager] RefreshMapVisuals: recalculating fog and updating renderer");

            // Recalculate fog of war
            var fogOfWar = ServiceLocator.Get<FogOfWar>();
            if (fogOfWar != null && mapGraph != null)
            {
                var heroInfos = new List<HeroFogInfo>();
                foreach (var hero in deployedHeroes)
                {
                    if (hero.isInjured) continue;
                    var info = new HeroFogInfo
                    {
                        nodeId = hero.currentNodeId,
                        bonusRevealRange = 0,
                        revealsEntireZone = false
                    };

                    // Check utility equipment for fog-related bonuses
                    // Cards with special abilities like Bead Lantern (+1 reveal) or Lantern of the Deep (zone reveal)
                    if (hero.utilityEquipment != null)
                    {
                        string equipName = hero.utilityEquipment.cardName ?? "";
                        if (equipName.Contains("Lantern") && !equipName.Contains("Deep"))
                        {
                            info.bonusRevealRange += hero.utilityEquipment.effectValue1;
                            Debug.Log($"[TurnManager] RefreshMapVisuals: hero {hero.tokenId} has +{hero.utilityEquipment.effectValue1} reveal range from '{equipName}'");
                        }
                        else if (equipName.Contains("Deep"))
                        {
                            info.revealsEntireZone = true;
                            Debug.Log($"[TurnManager] RefreshMapVisuals: hero {hero.tokenId} has zone reveal from '{equipName}'");
                        }
                    }

                    heroInfos.Add(info);
                }

                var activeEffectsDict = colonyGraph.GetActiveEffects();
                var activeEffects = new HashSet<ColonyEffect>(activeEffectsDict.Keys);
                fogOfWar.RecalculateVisibility(mapGraph, heroInfos, activeEffects);
                Debug.Log($"[TurnManager] RefreshMapVisuals: fog recalculated (heroInfos={heroInfos.Count}, activeEffects={activeEffects.Count})");
            }
            else
            {
                Debug.LogWarning($"[TurnManager] RefreshMapVisuals: cannot recalculate fog " +
                    $"(fogOfWar={fogOfWar != null}, mapGraph={mapGraph != null})");
            }

            // Update MapRenderer visuals (fog, node colors, resource indicators, tokens)
            var mapRenderer = Object.FindAnyObjectByType<MapRenderer>();
            if (mapRenderer != null)
            {
                mapRenderer.UpdateVisuals();
                mapRenderer.UpdateTokenPositions(deployedHeroes, allEnemies);
                Debug.Log("[TurnManager] RefreshMapVisuals: MapRenderer updated (visuals + tokens)");
            }
            else
            {
                Debug.LogWarning("[TurnManager] RefreshMapVisuals: MapRenderer not found");
            }
        }

        // ── Scene Transition Helper ──────────────────────────────────────

        /// <summary>
        /// Transitions to a new scene (full load, not additive). The TurnManager survives
        /// because it is DontDestroyOnLoad.
        /// </summary>
        /// <summary>
        /// Waits until GameManager in the newly loaded scene has finished its Start() coroutine
        /// and reconnected all scene-local services (MapRenderer, FogOfWar, etc.).
        /// </summary>
        private IEnumerator WaitForGameManagerInit()
        {
            var gm = Object.FindAnyObjectByType<GameManager>();
            if (gm == null)
            {
                Debug.LogWarning("[TurnManager] WaitForGameManagerInit: GameManager not found in scene");
                yield break;
            }

            Debug.Log($"[TurnManager] WaitForGameManagerInit: found GameManager (instanceId={gm.GetInstanceID()}, isInitialized={gm.IsInitialized})");

            int waitFrames = 0;
            while (!gm.IsInitialized && waitFrames < 120)
            {
                yield return null;
                waitFrames++;
            }

            if (gm.IsInitialized)
            {
                Debug.Log($"[TurnManager] WaitForGameManagerInit: GameManager ready after {waitFrames} frames");
            }
            else
            {
                Debug.LogWarning($"[TurnManager] WaitForGameManagerInit: timed out after {waitFrames} frames — forcing reconnect");
                // Fallback: force reconnect if Start() coroutine failed
                if (mapGraph != null)
                {
                    var mapRenderer = Object.FindAnyObjectByType<MapRenderer>();
                    if (mapRenderer != null)
                    {
                        mapRenderer.Initialize(mapGraph);
                        mapRenderer.UpdateVisuals();
                        mapRenderer.UpdateTokenPositions(deployedHeroes, allEnemies);
                        Debug.Log("[TurnManager] WaitForGameManagerInit: fallback map refresh complete");
                    }
                }
            }
        }

        private IEnumerator TransitionToScene(string sceneName)
        {
            if (SimulationFlags.SkipSceneTransitions)
            {
                Debug.Log($"[TurnManager] TransitionToScene: SKIPPED (SkipSceneTransitions=true) '{sceneName}'");
                yield return null;
                yield break;
            }

            Debug.Log($"[TurnManager] TransitionToScene: loading '{sceneName}'");
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
                yield return null;
            // Extra frames for Awake/Start to complete
            yield return null;
            yield return null;
            yield return null;
            Debug.Log($"[TurnManager] TransitionToScene: '{sceneName}' loaded, active scene={SceneManager.GetActiveScene().name}");
        }
    }
}
