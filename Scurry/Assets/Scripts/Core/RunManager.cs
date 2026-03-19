using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scurry.Data;
using Scurry.Interfaces;
using Scurry.Map;

namespace Scurry.Core
{
    public class RunManager : MonoBehaviour, IRunManager
    {
        private static RunManager _instance;
        public static RunManager Instance => _instance;

        // Run state
        private RunState currentState = RunState.GameOver;
        private int randomSeed;
        private List<CardDefinitionSO> constructedDeck = new List<CardDefinitionSO>();
        private List<ColonyCardDefinitionSO> constructedColonyDeck = new List<ColonyCardDefinitionSO>();

        // Current turn (1-based, no fixed limit)
        private int currentTurn;

        // Stats tracked across run
        private int totalResourcesGathered;
        private int totalEnemiesDefeated;
        private int zoneBossesDefeated;
        private bool piedPiperDefeated;
        private int colonyCardsPlayed;
        private HashSet<int> heroesInjured = new HashSet<int>();

        // Stockpiles
        private int foodStockpile;
        private int materialsStockpile;
        private int currencyStockpile;

        // --- Public properties ---
        public RunState CurrentState => currentState;
        public RunState CurrentRunState => currentState;
        public int RandomSeed => randomSeed;
        public IReadOnlyList<CardDefinitionSO> ConstructedDeck => constructedDeck;
        public IReadOnlyList<ColonyCardDefinitionSO> ConstructedColonyDeck => constructedColonyDeck;
        public int CurrentTurn => currentTurn;
        public int TotalResourcesGathered => totalResourcesGathered;
        public int TotalEnemiesDefeated => totalEnemiesDefeated;
        public int ZoneBossesDefeated => zoneBossesDefeated;
        public bool PiedPiperDefeated => piedPiperDefeated;
        public int ColonyCardsPlayed => colonyCardsPlayed;

        // IRunManager interface properties (legacy compat)
        public int CurrentLevel => currentTurn;
        public int FoodStockpile => foodStockpile;
        public int MaterialsStockpile => materialsStockpile;
        public int CurrencyStockpile => currencyStockpile;
        public List<ColonyCardDefinitionSO> ColonyCardPool => new List<ColonyCardDefinitionSO>(constructedColonyDeck);
        public int CurrentStageIndex => 0;
        public int CurrentStepIndex => currentTurn;

        private void Awake()
        {
            Debug.Log($"[RunManager] Awake: checking for existing instance (instance={(_instance != null ? _instance.GetInstanceID().ToString() : "NULL")})");

            if (_instance != null && _instance != this)
            {
                // Re-register existing instance in case ServiceLocator was cleared
                ServiceLocator.Register<IRunManager>(_instance);
                ServiceLocator.Register<RunManager>(_instance);
                Debug.Log($"[RunManager] Awake: duplicate instance detected — re-registered existing and destroying component only (self={GetInstanceID()}, existing={_instance.GetInstanceID()})");
                Destroy(this);
                return;
            }

            _instance = this;
            if (gameObject.name == "PersistentManagers")
                DontDestroyOnLoad(gameObject);

            ServiceLocator.Register<IRunManager>(this);
            ServiceLocator.Register<RunManager>(this);
            Debug.Log($"[RunManager] Awake: registered with ServiceLocator (instanceId={GetInstanceID()})");
        }

        private void OnEnable()
        {
            Debug.Log("[RunManager] OnEnable: subscribing to EventBus events");
            EventBus.OnRunComplete += HandleRunComplete;
            EventBus.OnTurnStarted += HandleTurnStarted;
            EventBus.OnColonyCardPlayed += HandleColonyCardPlayed;
            EventBus.OnReturnToMainMenu += HandleReturnToMainMenu;
            EventBus.OnResourceGathered += HandleResourceGathered;
            EventBus.OnResourceDeposited += HandleResourceDeposited;
            EventBus.OnCombatEnded += HandleCombatEnded;
        }

        private void OnDisable()
        {
            Debug.Log("[RunManager] OnDisable: unsubscribing from EventBus events");
            EventBus.OnRunComplete -= HandleRunComplete;
            EventBus.OnTurnStarted -= HandleTurnStarted;
            EventBus.OnColonyCardPlayed -= HandleColonyCardPlayed;
            EventBus.OnReturnToMainMenu -= HandleReturnToMainMenu;
            EventBus.OnResourceGathered -= HandleResourceGathered;
            EventBus.OnResourceDeposited -= HandleResourceDeposited;
            EventBus.OnCombatEnded -= HandleCombatEnded;
        }

        private void OnDestroy()
        {
            Debug.Log($"[RunManager] OnDestroy: instanceId={GetInstanceID()}");
            if (_instance == this)
            {
                _instance = null;
                ServiceLocator.Unregister<IRunManager>();
                ServiceLocator.Unregister<RunManager>();
                Debug.Log("[RunManager] OnDestroy: unregistered from ServiceLocator");
            }
        }

        // ── Scene Flow ───────────────────────────────────────────────────

        /// <summary>
        /// Starts a brand new run. Deletes any existing save and loads the DeckConstruction scene.
        /// </summary>
        public void StartNewRun()
        {
            Debug.Log("[RunManager] StartNewRun: deleting save and transitioning to DeckConstruction");

            // Stop any active TurnManager coroutines from a previous run
            var turnManager = FindAnyObjectByType<TurnManager>();
            if (turnManager != null && turnManager.RunActive)
            {
                Debug.Log("[RunManager] StartNewRun: stopping active TurnManager from previous run");
                turnManager.EndRun(false);
            }

            // Reset event bus to clear stale subscriptions from destroyed scene objects
            EventBus.Reset();
            Debug.Log("[RunManager] StartNewRun: EventBus reset");

            // Re-subscribe RunManager events (we just cleared them)
            OnEnable();

            SaveManager.DeleteSave();
            ResetRunState();

            currentState = RunState.DeckConstruction;
            Debug.Log($"[RunManager] StartNewRun: state set to {currentState}");

            SceneManager.LoadScene("DeckConstruction");
            Debug.Log("[RunManager] StartNewRun: LoadScene('DeckConstruction') called");
        }

        /// <summary>
        /// IRunManager compat: alias for StartNewRun.
        /// </summary>
        public void StartRun()
        {
            Debug.Log("[RunManager] StartRun: delegating to StartNewRun");
            StartNewRun();
        }

        /// <summary>
        /// Continues a saved run. Loads save data and transitions to GameMap scene.
        /// </summary>
        public void ContinueRun()
        {
            Debug.Log("[RunManager] ContinueRun: loading save data");

            RunSaveData save = SaveManager.Load();
            if (save == null)
            {
                Debug.LogWarning("[RunManager] ContinueRun: no save data found — cannot continue, returning to main menu");
                ReturnToMainMenu();
                return;
            }

            RestoreFromSave(save);

            currentState = RunState.InRun;
            Debug.Log($"[RunManager] ContinueRun: state set to {currentState} (turn={currentTurn}, seed={randomSeed})");

            SceneManager.LoadScene("GameMap");
            Debug.Log("[RunManager] ContinueRun: LoadScene('GameMap') called");
        }

        /// <summary>
        /// Called by DeckConstructionManager when the player confirms their deck.
        /// Stores the decks, generates a seed, and transitions to GameMap.
        /// </summary>
        public void OnDeckConstructionComplete(List<CardDefinitionSO> deck, List<ColonyCardDefinitionSO> colonyDeck)
        {
            Debug.Log($"[RunManager] OnDeckConstructionComplete: received deck (cards={deck.Count}, colonyCards={colonyDeck.Count})");

            constructedDeck.Clear();
            constructedDeck.AddRange(deck);

            constructedColonyDeck.Clear();
            constructedColonyDeck.AddRange(colonyDeck);

            // Log each card in the constructed deck
            for (int i = 0; i < constructedDeck.Count; i++)
            {
                var card = constructedDeck[i];
                Debug.Log($"[RunManager] OnDeckConstructionComplete: deck[{i}] = '{card.cardName}' (id={card.cardId}, type={card.cardType}, cost={card.deckCost})");
            }
            for (int i = 0; i < constructedColonyDeck.Count; i++)
            {
                var card = constructedColonyDeck[i];
                Debug.Log($"[RunManager] OnDeckConstructionComplete: colonyDeck[{i}] = '{card.cardName}' (id={card.cardId}, tier={card.colonyTier}, cost={card.deckCost})");
            }

            // Save last deck to meta-progression for next run pre-population
            var meta = MetaProgressionManager.Instance;
            if (meta != null)
            {
                var deckIds = deck.ConvertAll(c => c.cardId);
                var colonyIds = colonyDeck.ConvertAll(c => c.cardId);
                meta.SaveLastDeck(deckIds, colonyIds);
            }

            // Generate random seed for this run
            randomSeed = System.Environment.TickCount;
            Debug.Log($"[RunManager] OnDeckConstructionComplete: generated seed={randomSeed}");

            currentState = RunState.InRun;
            currentTurn = 0;
            Debug.Log($"[RunManager] OnDeckConstructionComplete: state={currentState}, transitioning to GameMap");

            // Fire run started event
            EventBus.OnRunStarted?.Invoke();
            Debug.Log("[RunManager] OnDeckConstructionComplete: fired EventBus.OnRunStarted");

            SceneManager.LoadScene("GameMap");
            Debug.Log("[RunManager] OnDeckConstructionComplete: LoadScene('GameMap') called");
        }

        /// <summary>
        /// Called when the run ends (victory or defeat). Calculates score and transitions to RunResult.
        /// </summary>
        public void OnRunComplete(bool victory, int turnsUsed)
        {
            Debug.Log($"[RunManager] OnRunComplete: victory={victory}, turnsUsed={turnsUsed}");

            currentState = victory ? RunState.RunComplete : RunState.GameOver;

            // Calculate score: fewer turns + smaller deck = higher score (per GDD)
            int score = CalculateScore(victory, turnsUsed);
            Debug.Log($"[RunManager] OnRunComplete: finalScore={score}");

            EventBus.OnScoreCalculated?.Invoke(score);
            Debug.Log("[RunManager] OnRunComplete: fired EventBus.OnScoreCalculated");

            // Save final stats before transitioning
            SaveRunState();
            Debug.Log("[RunManager] OnRunComplete: saved final run state");

            SceneManager.LoadScene("RunResult");
            Debug.Log("[RunManager] OnRunComplete: LoadScene('RunResult') called");
        }

        /// <summary>
        /// Returns to the main menu scene.
        /// </summary>
        public void ReturnToMainMenu()
        {
            Debug.Log("[RunManager] ReturnToMainMenu: transitioning to MainMenu");

            currentState = RunState.GameOver;
            Debug.Log($"[RunManager] ReturnToMainMenu: state set to {currentState}");

            SceneManager.LoadScene("MainMenu");
            Debug.Log("[RunManager] ReturnToMainMenu: LoadScene('MainMenu') called");
        }

        // ── Stat Tracking ────────────────────────────────────────────────

        /// <summary>
        /// Records resources gathered by heroes (before deposit to colony).
        /// </summary>
        public void RecordResourceGathered(int amount)
        {
            int previousTotal = totalResourcesGathered;
            totalResourcesGathered += amount;
            Debug.Log($"[RunManager] RecordResourceGathered: amount={amount}, total={previousTotal}->{totalResourcesGathered}");
        }

        /// <summary>
        /// Records an enemy defeat. Tracks boss and Pied Piper defeats separately.
        /// </summary>
        public void RecordEnemyDefeated(bool isBoss, bool isPiper)
        {
            totalEnemiesDefeated++;
            Debug.Log($"[RunManager] RecordEnemyDefeated: isBoss={isBoss}, isPiper={isPiper}, totalDefeated={totalEnemiesDefeated}");

            if (isBoss)
            {
                zoneBossesDefeated++;
                Debug.Log($"[RunManager] RecordEnemyDefeated: zoneBossesDefeated={zoneBossesDefeated}");
            }

            if (isPiper)
            {
                piedPiperDefeated = true;
                Debug.Log("[RunManager] RecordEnemyDefeated: PIED PIPER DEFEATED!");
            }
        }

        /// <summary>
        /// Records a colony card being played during the Colony phase.
        /// </summary>
        public void RecordColonyCardPlayed()
        {
            colonyCardsPlayed++;
            Debug.Log($"[RunManager] RecordColonyCardPlayed: total={colonyCardsPlayed}");
        }

        /// <summary>
        /// Records a hero being injured. Tracked by card ID.
        /// </summary>
        public void RecordHeroInjured(int heroCardId)
        {
            bool wasNew = heroesInjured.Add(heroCardId);
            Debug.Log($"[RunManager] RecordHeroInjured: heroCardId={heroCardId}, wasNew={wasNew}, totalUniqueInjured={heroesInjured.Count}");
        }

        /// <summary>
        /// Updates stockpile values. Called by ResourceManager when stockpiles change.
        /// </summary>
        public void UpdateStockpiles(int food, int materials, int currency)
        {
            Debug.Log($"[RunManager] UpdateStockpiles: food={foodStockpile}->{food}, materials={materialsStockpile}->{materials}, currency={currencyStockpile}->{currency}");
            foodStockpile = food;
            materialsStockpile = materials;
            currencyStockpile = currency;
        }

        // ── Save / Load ──────────────────────────────────────────────────

        /// <summary>
        /// Saves the current run state to disk.
        /// </summary>
        public void SaveRunState()
        {
            Debug.Log("[RunManager] SaveRunState: building save data");

            var save = new RunSaveData
            {
                randomSeed = randomSeed,
                currentTurn = currentTurn,
                runState = (int)currentState,
                foodStockpile = foodStockpile,
                materialsStockpile = materialsStockpile,
                currencyStockpile = currencyStockpile,
                totalResourcesGathered = totalResourcesGathered,
                totalEnemiesDefeated = totalEnemiesDefeated,
                zoneBossesDefeated = zoneBossesDefeated,
                piedPiperDefeated = piedPiperDefeated,
                colonyCardsPlayed = colonyCardsPlayed
            };

            // Save deck card IDs
            foreach (var card in constructedDeck)
            {
                save.deckCardIds.Add(card.cardId);
                Debug.Log($"[RunManager] SaveRunState: saved deck card '{card.cardName}' (id={card.cardId})");
            }

            // Save colony deck card IDs
            foreach (var card in constructedColonyDeck)
            {
                save.colonyDeckCardIds.Add(card.cardId);
                Debug.Log($"[RunManager] SaveRunState: saved colony card '{card.cardName}' (id={card.cardId})");
            }

            // Save injured hero IDs
            foreach (int heroId in heroesInjured)
            {
                save.heroesEverInjuredIds.Add(heroId);
                Debug.Log($"[RunManager] SaveRunState: saved injured heroId={heroId}");
            }

            SaveManager.Save(save);
            Debug.Log($"[RunManager] SaveRunState: save complete (turn={currentTurn}, deckSize={save.deckCardIds.Count}, colonyDeckSize={save.colonyDeckCardIds.Count})");
        }

        /// <summary>
        /// Restores run state from saved data.
        /// </summary>
        private void RestoreFromSave(RunSaveData save)
        {
            Debug.Log($"[RunManager] RestoreFromSave: restoring (seed={save.randomSeed}, turn={save.currentTurn}, state={save.runState})");

            randomSeed = save.randomSeed;
            currentTurn = save.currentTurn;
            currentState = (RunState)save.runState;
            foodStockpile = save.foodStockpile;
            materialsStockpile = save.materialsStockpile;
            currencyStockpile = save.currencyStockpile;
            totalResourcesGathered = save.totalResourcesGathered;
            totalEnemiesDefeated = save.totalEnemiesDefeated;
            zoneBossesDefeated = save.zoneBossesDefeated;
            piedPiperDefeated = save.piedPiperDefeated;
            colonyCardsPlayed = save.colonyCardsPlayed;

            Debug.Log($"[RunManager] RestoreFromSave: stockpiles — food={foodStockpile}, materials={materialsStockpile}, currency={currencyStockpile}");
            Debug.Log($"[RunManager] RestoreFromSave: stats — resources={totalResourcesGathered}, enemies={totalEnemiesDefeated}, bosses={zoneBossesDefeated}, piper={piedPiperDefeated}");

            // Restore decks from card IDs via CardDatabase
            constructedDeck.Clear();
            var cardDb = CardDatabase.Instance;
            foreach (int cardId in save.deckCardIds)
            {
                var card = cardDb.GetCard(cardId);
                if (card != null)
                {
                    constructedDeck.Add(card);
                    Debug.Log($"[RunManager] RestoreFromSave: restored deck card '{card.cardName}' (id={cardId})");
                }
                else
                {
                    Debug.LogWarning($"[RunManager] RestoreFromSave: deck card not found in database (id={cardId})");
                }
            }

            constructedColonyDeck.Clear();
            foreach (int cardId in save.colonyDeckCardIds)
            {
                var card = cardDb.GetColonyCard(cardId);
                if (card != null)
                {
                    constructedColonyDeck.Add(card);
                    Debug.Log($"[RunManager] RestoreFromSave: restored colony card '{card.cardName}' (id={cardId})");
                }
                else
                {
                    Debug.LogWarning($"[RunManager] RestoreFromSave: colony card not found in database (id={cardId})");
                }
            }

            // Restore injured heroes
            heroesInjured.Clear();
            foreach (int heroId in save.heroesEverInjuredIds)
            {
                heroesInjured.Add(heroId);
                Debug.Log($"[RunManager] RestoreFromSave: restored injured heroId={heroId}");
            }

            Debug.Log($"[RunManager] RestoreFromSave: complete (deck={constructedDeck.Count}, colonyDeck={constructedColonyDeck.Count}, injured={heroesInjured.Count})");
        }

        // ── Score Calculation ────────────────────────────────────────────

        /// <summary>
        /// Calculates the final score. Per GDD: fewer turns + smaller deck = higher score.
        /// </summary>
        private int CalculateScore(bool victory, int turnsUsed)
        {
            Debug.Log($"[RunManager] CalculateScore: victory={victory}, turnsUsed={turnsUsed}");

            int score = 0;

            // Base score for victory
            if (victory)
            {
                score += 1000;
                Debug.Log("[RunManager] CalculateScore: +1000 for victory");
            }

            // Turn bonus: fewer turns = higher score (baseline 30 turns)
            int turnBonus = Mathf.Max(0, (30 - turnsUsed) * 50);
            score += turnBonus;
            Debug.Log($"[RunManager] CalculateScore: +{turnBonus} turn bonus (turnsUsed={turnsUsed})");

            // Deck efficiency: smaller deck = higher score
            int totalDeckSize = constructedDeck.Count + constructedColonyDeck.Count;
            int deckBonus = Mathf.Max(0, (30 - totalDeckSize) * 20);
            score += deckBonus;
            Debug.Log($"[RunManager] CalculateScore: +{deckBonus} deck efficiency bonus (deckSize={totalDeckSize})");

            // Resource bonus
            int resourceBonus = totalResourcesGathered * 5;
            score += resourceBonus;
            Debug.Log($"[RunManager] CalculateScore: +{resourceBonus} resource bonus (totalGathered={totalResourcesGathered})");

            // Enemy bonus
            int enemyBonus = totalEnemiesDefeated * 10;
            score += enemyBonus;
            Debug.Log($"[RunManager] CalculateScore: +{enemyBonus} enemy bonus (totalDefeated={totalEnemiesDefeated})");

            // Boss bonus
            int bossBonus = zoneBossesDefeated * 100;
            score += bossBonus;
            Debug.Log($"[RunManager] CalculateScore: +{bossBonus} boss bonus (bossesDefeated={zoneBossesDefeated})");

            // Pied Piper bonus
            if (piedPiperDefeated)
            {
                score += 500;
                Debug.Log("[RunManager] CalculateScore: +500 Pied Piper defeated bonus");
            }

            // Colony card bonus
            int colonyBonus = colonyCardsPlayed * 15;
            score += colonyBonus;
            Debug.Log($"[RunManager] CalculateScore: +{colonyBonus} colony bonus (cardsPlayed={colonyCardsPlayed})");

            Debug.Log($"[RunManager] CalculateScore: finalScore={score}");
            return score;
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void HandleRunComplete(bool victory)
        {
            Debug.Log($"[RunManager] HandleRunComplete: victory={victory}, currentTurn={currentTurn}");
            OnRunComplete(victory, currentTurn);
        }

        private void HandleTurnStarted(int turnNumber)
        {
            currentTurn = turnNumber;
            Debug.Log($"[RunManager] HandleTurnStarted: turn={currentTurn}");

            // Auto-save at the start of each turn
            SaveRunState();
            Debug.Log($"[RunManager] HandleTurnStarted: auto-saved at turn={currentTurn}");
        }

        private void HandleColonyCardPlayed(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[RunManager] HandleColonyCardPlayed: card='{card.cardName}' (id={card.cardId})");
            RecordColonyCardPlayed();
        }

        private void HandleReturnToMainMenu()
        {
            Debug.Log("[RunManager] HandleReturnToMainMenu: saving and returning to menu");
            if (currentState == RunState.InRun)
            {
                SaveRunState();
                Debug.Log("[RunManager] HandleReturnToMainMenu: saved in-progress run");
            }
            ReturnToMainMenu();
        }

        private void HandleResourceGathered(HeroToken hero, ResourceType type, int amount)
        {
            Debug.Log($"[RunManager] HandleResourceGathered: type={type}, amount={amount}");
            RecordResourceGathered(amount);
        }

        private void HandleResourceDeposited(HeroToken hero, ResourceType type, int amount)
        {
            Debug.Log($"[RunManager] HandleResourceDeposited: type={type}, amount={amount}");
            // Stockpile updates come from the ResourceManager/ColonyManager, not tracked here directly
        }

        private void HandleCombatEnded(int nodeId, bool heroesWon)
        {
            Debug.Log($"[RunManager] HandleCombatEnded: nodeId={nodeId}, heroesWon={heroesWon}");
            // Individual enemy defeat tracking happens via RecordEnemyDefeated called by CombatResolver
        }

        // ── Internal Helpers ─────────────────────────────────────────────

        /// <summary>
        /// Resets all run state to defaults for a new run.
        /// </summary>
        private void ResetRunState()
        {
            Debug.Log("[RunManager] ResetRunState: clearing all run data");

            randomSeed = 0;
            currentTurn = 0;
            totalResourcesGathered = 0;
            totalEnemiesDefeated = 0;
            zoneBossesDefeated = 0;
            piedPiperDefeated = false;
            colonyCardsPlayed = 0;
            heroesInjured.Clear();
            constructedDeck.Clear();
            constructedColonyDeck.Clear();
            foodStockpile = 0;
            materialsStockpile = 0;
            currencyStockpile = 0;

            Debug.Log("[RunManager] ResetRunState: complete — all state zeroed");
        }
    }
}
