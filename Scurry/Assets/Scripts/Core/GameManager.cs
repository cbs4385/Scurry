using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Cards;
using Scurry.Board;
using Scurry.Placement;
using Scurry.Gathering;
using Scurry.Colony;

namespace Scurry.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField] private bool standaloneMode = true;

        [Header("References")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private DeckManager deckManager;
        [SerializeField] private HandManager handManager;
        [SerializeField] private PlacementManager placementManager;
        [SerializeField] private GatheringManager gatheringManager;
        [SerializeField] private ColonyManager colonyManager;
        [SerializeField] private LocalizationManager localizationManager;

        private GamePhase currentPhase;
        private int turnNumber;
        private readonly HashSet<int> woundedHeroIndices = new HashSet<int>(); // indices into the currentRunDeck list
        private List<CardDefinitionSO> currentRunDeck; // reference to RunManager's deck for index mapping
        private readonly List<CardDefinitionSO> exhaustedResources = new List<CardDefinitionSO>();
        private bool suppressExhaustion; // true during auto-collect so cards aren't exhausted

        private void Awake()
        {
            Debug.Log($"[GameManager] Awake: boardManager={boardManager?.name ?? "NULL"}, deckManager={deckManager?.name ?? "NULL"}, handManager={handManager?.name ?? "NULL"}, placementManager={placementManager?.name ?? "NULL"}, gatheringManager={gatheringManager?.name ?? "NULL"}, colonyManager={colonyManager?.name ?? "NULL"}");
        }

        private void OnEnable()
        {
            Debug.Log($"[GameManager] OnEnable: subscribing to EventBus events (standaloneMode={standaloneMode})");
            EventBus.OnTurnEnded += OnEndTurnPressed;
            EventBus.OnGatheringComplete += OnGatheringDone;
            EventBus.OnResourceTokenCollected += OnResourceTokenCollected;
            if (standaloneMode)
                EventBus.OnDeckBuildComplete += OnDeckBuildDone;
        }

        private void OnDisable()
        {
            Debug.Log("[GameManager] OnDisable: unsubscribing from EventBus events");
            EventBus.OnTurnEnded -= OnEndTurnPressed;
            EventBus.OnGatheringComplete -= OnGatheringDone;
            EventBus.OnResourceTokenCollected -= OnResourceTokenCollected;
            if (standaloneMode)
                EventBus.OnDeckBuildComplete -= OnDeckBuildDone;
        }

        private void Start()
        {
            Debug.Log($"[GameManager] Start: standaloneMode={standaloneMode}");
            EventBus.LogSubscriberCounts();

            if (!standaloneMode)
            {
                Debug.Log("[GameManager] Start: external mode — waiting for RunManager");
                return;
            }

            if (SaveManager.HasSave())
            {
                Debug.Log("[GameManager] Start: save file found — loading saved run");
                LoadRun();
            }
            else
            {
                Debug.Log("[GameManager] Start: no save file — starting new run");
                StartRun();
            }
        }

        private void StartRun()
        {
            Debug.Log("[GameManager] StartRun: entering DeckBuild phase");
            turnNumber = 0;
            SetPhase(GamePhase.DeckBuild);
        }

        public void StartCardPlacementGame(List<CardDefinitionSO> deck)
        {
            Debug.Log($"[GameManager] StartCardPlacementGame: received {deck.Count} cards from RunManager, woundedHeroIndices={woundedHeroIndices.Count}");

            // Clean up any leftover tokens and tiles from previous CardPlacement game
            placementManager.ClearTokens(keepHealthyHeroes: false);
            boardManager.ResetBoardForNewGame();
            Debug.Log("[GameManager] StartCardPlacementGame: cleared board from previous game");

            // Store deck reference for wound index tracking
            currentRunDeck = deck;

            // Filter out wounded heroes by deck index — only the specific copy is excluded
            var activeDeck = new List<CardDefinitionSO>();
            for (int i = 0; i < deck.Count; i++)
            {
                if (woundedHeroIndices.Contains(i))
                {
                    Debug.Log($"[GameManager] StartCardPlacementGame: excluding wounded hero index={i} '{deck[i].cardName}'");
                }
                else
                {
                    activeDeck.Add(deck[i]);
                }
            }
            Debug.Log($"[GameManager] StartCardPlacementGame: active deck={activeDeck.Count} (excluded {deck.Count - activeDeck.Count} wounded)");
            exhaustedResources.Clear();
            deckManager.InitializeDeck(activeDeck);
            turnNumber = 0;
            boardManager.GenerateResourceNodeResources();
            gatheringManager.SpawnEnemies();
            StartNewTurn();
        }

        private void EndCardPlacementGame(string reason, bool heroesLost = false)
        {
            Debug.Log($"[GameManager] EndCardPlacementGame: reason='{reason}', heroesLost={heroesLost}, standaloneMode={standaloneMode}");
            if (standaloneMode)
            {
                SaveManager.DeleteSave();
            }
            EventBus.OnCardPlacementGameComplete?.Invoke(heroesLost);
        }

        private void OnDeckBuildDone(List<CardDefinitionSO> selectedCards)
        {
            Debug.Log($"[GameManager] OnDeckBuildDone: received {selectedCards.Count} cards, initializing game");
            deckManager.InitializeDeck(selectedCards);
            colonyManager.InitializeHP();
            boardManager.GenerateResourceNodeResources();
            gatheringManager.SpawnEnemies();
            Debug.Log("[GameManager] OnDeckBuildDone: initialization complete, starting first turn");
            StartNewTurn();
        }

        private void StartNewTurn()
        {
            turnNumber++;
            Debug.Log($"[GameManager] ========== Turn {turnNumber} ==========");

            // Check if deck is empty — auto-battler mode
            if (!deckManager.CanDraw())
            {
                Debug.Log("[GameManager] StartNewTurn: no cards to draw — auto-battler mode, skipping Draw/Deploy");
                handManager.ClearHand();
                SetPhase(GamePhase.Gather);
                string autoMsg = Loc.Get("gather.autobattler");
                EventBus.OnGatheringNotification?.Invoke(autoMsg, new Color(1f, 0.7f, 0.3f));
                StartCoroutine(gatheringManager.RunGathering());
                return;
            }

            // Draw phase
            SetPhase(GamePhase.Draw);
            handManager.ClearHand();
            Debug.Log($"[GameManager] StartNewTurn: drawing hand from deck (drawPile={deckManager.DrawPileCount}, discardPile={deckManager.DiscardPileCount})");
            var drawn = deckManager.DrawHand();
            Debug.Log($"[GameManager] StartNewTurn: drew {drawn.Count} cards");
            handManager.AddCardsToHand(drawn);

            // Move to Deploy
            SetPhase(GamePhase.Deploy);
            Debug.Log($"[GameManager] StartNewTurn: Deploy phase active — waiting for player input");
        }

        private void OnResourceTokenCollected(string tokenName)
        {
            // Auto-generated resource tokens have no card to track
            if (tokenName.StartsWith("Resource_Auto_"))
            {
                Debug.Log($"[GameManager] OnResourceTokenCollected: '{tokenName}' is auto-generated — no card to recycle");
                return;
            }

            // Extract card name from token name (format: "Resource_CardName")
            string cardName = tokenName.Substring("Resource_".Length);
            var card = deckManager.FindCardByName(cardName);
            if (card != null)
            {
                if (suppressExhaustion)
                {
                    Debug.Log($"[GameManager] OnResourceTokenCollected: '{cardName}' auto-collected — suppressing exhaustion (card stays in deck)");
                }
                else if (standaloneMode)
                {
                    deckManager.DiscardCard(card);
                    Debug.Log($"[GameManager] OnResourceTokenCollected: '{cardName}' returned to discard pile (standalone mode)");
                }
                else
                {
                    exhaustedResources.Add(card);
                    Debug.Log($"[GameManager] OnResourceTokenCollected: '{cardName}' exhausted (removed from deck until next stage, exhausted count={exhaustedResources.Count})");
                }
            }
            else
            {
                Debug.LogWarning($"[GameManager] OnResourceTokenCollected: could not find card for token '{tokenName}' (extracted name='{cardName}')");
            }
        }

        private void OnEndTurnPressed()
        {
            Debug.Log($"[GameManager] OnEndTurnPressed: currentPhase={currentPhase}");
            if (currentPhase != GamePhase.Deploy)
            {
                Debug.LogWarning($"[GameManager] OnEndTurnPressed: ignoring — not in Deploy phase (current={currentPhase})");
                return;
            }

            Debug.Log("[GameManager] OnEndTurnPressed: Deploy phase ended. Discarding unplayed cards...");
            SetPhase(GamePhase.Gather);

            // Discard unplayed cards (not return to draw pile — prevents infinite loop)
            var handCards = handManager.GetHandCards();
            Debug.Log($"[GameManager] OnEndTurnPressed: discarding {handCards.Count} unplayed cards");
            foreach (var card in handCards)
            {
                Debug.Log($"[GameManager] OnEndTurnPressed: discarding '{card.CardData?.cardName ?? "?"}'");
                deckManager.DiscardCard(card.CardData);
            }
            handManager.ClearHand();

            Debug.Log("[GameManager] OnEndTurnPressed: starting gathering coroutine");
            StartCoroutine(gatheringManager.RunGathering());
        }

        private void OnGatheringDone()
        {
            Debug.Log($"[GameManager] OnGatheringDone: gathering complete. ColonyHP={colonyManager.CurrentHP}/{colonyManager.MaxHP}, isAlive={colonyManager.IsAlive}");
            SetPhase(GamePhase.Resolve);

            // Check colony HP
            if (!colonyManager.IsAlive)
            {
                Debug.Log("[GameManager] ===== GAME OVER — Colony defeated! =====");
                EndCardPlacementGame("Colony defeated");
                return;
            }

            Debug.Log("[GameManager] OnGatheringDone: colony alive, starting resolve and next turn");
            StartCoroutine(ResolveAndNextTurn());
        }

        private IEnumerator ResolveAndNextTurn()
        {
            Debug.Log("[GameManager] ResolveAndNextTurn: waiting 0.5s before cleanup");
            yield return new WaitForSeconds(0.5f);

            // Track wounds: scan all HeroAgents on the board and map to deck indices
            var heroes = boardManager.GetComponentsInChildren<HeroAgent>();
            Debug.Log($"[GameManager] ResolveAndNextTurn: scanning {heroes.Length} heroes for wound tracking");
            foreach (var hero in heroes)
            {
                if (hero.IsHealing)
                {
                    // Hero sat out this turn to heal — find and remove its deck index
                    int healIdx = FindDeckIndex(hero.CardData, true);
                    if (healIdx >= 0)
                    {
                        woundedHeroIndices.Remove(healIdx);
                        Debug.Log($"[GameManager] ResolveAndNextTurn: hero='{hero.CardData.cardName}' healed (sat out gathering), removed index={healIdx}");
                    }
                }
                else if (hero.IsWounded)
                {
                    // Hero got freshly wounded this turn — find an unwounded deck index for this card
                    int woundIdx = FindDeckIndex(hero.CardData, false);
                    if (woundIdx >= 0)
                    {
                        woundedHeroIndices.Add(woundIdx);
                        Debug.Log($"[GameManager] ResolveAndNextTurn: hero='{hero.CardData.cardName}' freshly wounded, added index={woundIdx}");
                    }
                }
                else
                {
                    Debug.Log($"[GameManager] ResolveAndNextTurn: hero='{hero.CardData.cardName}' healthy");
                }
            }
            Debug.Log($"[GameManager] ResolveAndNextTurn: woundedHeroIndices count={woundedHeroIndices.Count}");

            // Played cards stay out of the deck — their tokens are on the board
            var placedCards = placementManager.GetPlacedCardDefinitions();
            Debug.Log($"[GameManager] ResolveAndNextTurn: {placedCards.Count} played cards remain out of deck (tokens on board)");

            Debug.Log("[GameManager] ResolveAndNextTurn: clearing tokens (keeping healthy heroes) and resetting tiles");
            placementManager.ClearTokens(keepHealthyHeroes: true);
            boardManager.ResetAllTiles();

            // Wait one frame so Destroy() calls are processed before re-scanning
            yield return null;

            // Re-mark tiles that still have hero tokens on them
            var remainingHeroes = boardManager.GetComponentsInChildren<HeroAgent>();
            Debug.Log($"[GameManager] ResolveAndNextTurn: re-marking {remainingHeroes.Length} hero tiles after reset");
            foreach (var hero in remainingHeroes)
            {
                var tile = boardManager.GetTile(hero.GridPosition);
                if (tile != null)
                {
                    tile.HasHero = true;
                    Debug.Log($"[GameManager] ResolveAndNextTurn: tile ({hero.GridPosition}) re-marked HasHero=true for '{hero.CardData.cardName}'");
                }
            }

            // Update tile types: empty patrol tiles → normal, enemy-occupied tiles → patrol
            boardManager.UpdateTilesForEnemyMovement();

            Debug.Log("[GameManager] ResolveAndNextTurn: cleanup complete");

            // --- End condition checks ---
            bool hasResources = boardManager.HasAnyResources();
            bool hasEnemies = boardManager.HasAnyEnemies();
            bool hasHeroesOnBoard = remainingHeroes.Length > 0;
            bool hasHeroCardsInDeck = deckManager.HasHeroCards();
            Debug.Log($"[GameManager] ResolveAndNextTurn: endConditionCheck — hasResources={hasResources}, hasEnemies={hasEnemies}, heroesOnBoard={hasHeroesOnBoard}, heroCardsInDeck={hasHeroCardsInDeck}");

            // Condition B: All resources collected from game tiles
            if (!hasResources)
            {
                Debug.Log("[GameManager] ===== CARD PLACEMENT COMPLETE — All resources collected! =====");
                EndCardPlacementGame("All resources collected");
                yield break;
            }

            // Condition C: No enemies remain — auto-collect all remaining resources
            if (!hasEnemies)
            {
                Debug.Log("[GameManager] ===== CARD PLACEMENT COMPLETE — All enemies defeated! Auto-collecting remaining resources =====");
                suppressExhaustion = true;
                boardManager.CollectAllRemainingResources();
                suppressExhaustion = false;
                EndCardPlacementGame("All enemies defeated");
                yield break;
            }

            // Condition A: No heroes in play AND no playable hero cards in deck — heroes lost
            if (!hasHeroesOnBoard && !hasHeroCardsInDeck)
            {
                Debug.Log("[GameManager] ===== CARD PLACEMENT COMPLETE — No heroes available (none on board, none in deck)! =====");
                EndCardPlacementGame("No heroes available", heroesLost: true);
                yield break;
            }

            // No end condition met — continue to next turn
            Debug.Log("[GameManager] ResolveAndNextTurn: continuing to next turn");
            if (standaloneMode)
            {
                SaveRunState();
            }
            StartNewTurn();
        }

        private void SaveRunState()
        {
            var saveData = new RunSaveData
            {
                turnNumber = turnNumber,
                colonyHP = colonyManager.CurrentHP,
                currencyStockpile = colonyManager.CurrencyStockpile,
                foodStockpile = colonyManager.FoodStockpile
            };

            // Save draw pile
            foreach (var card in deckManager.DrawPile)
                saveData.drawPileCardNames.Add(card.cardName);

            // Save discard pile
            foreach (var card in deckManager.DiscardPile)
                saveData.discardPileCardNames.Add(card.cardName);

            // Save wounded heroes (by name — standalone mode doesn't use index tracking)
            foreach (int idx in woundedHeroIndices)
            {
                if (currentRunDeck != null && idx < currentRunDeck.Count)
                    saveData.woundedHeroCardNames.Add(currentRunDeck[idx].cardName);
            }

            // Save living enemy positions and strengths
            var enemies = boardManager.GetComponentsInChildren<Gathering.EnemyAgent>();
            foreach (var enemy in enemies)
            {
                if (!enemy.IsDefeated)
                    saveData.livingEnemyPositions.Add($"{enemy.GridPosition.x},{enemy.GridPosition.y},{enemy.Strength}");
            }

            Debug.Log($"[GameManager] SaveRunState: turn={saveData.turnNumber}, hp={saveData.colonyHP}, draw={saveData.drawPileCardNames.Count}, discard={saveData.discardPileCardNames.Count}, wounded={saveData.woundedHeroCardNames.Count}, livingEnemies={saveData.livingEnemyPositions.Count}");
            SaveManager.Save(saveData);
        }

        private void LoadRun()
        {
            var saveData = SaveManager.Load();
            if (saveData == null)
            {
                Debug.LogWarning("[GameManager] LoadRun: save data was null — starting new run instead");
                StartRun();
                return;
            }

            Debug.Log($"[GameManager] LoadRun: restoring turn={saveData.turnNumber}, hp={saveData.colonyHP}");

            // Restore turn number (will be incremented by StartNewTurn)
            turnNumber = saveData.turnNumber - 1;

            // Restore colony state
            colonyManager.RestoreState(saveData.colonyHP, saveData.currencyStockpile, saveData.foodStockpile);

            // Restore deck piles
            var drawPile = new List<CardDefinitionSO>();
            foreach (var name in saveData.drawPileCardNames)
            {
                var card = deckManager.FindCardByName(name);
                if (card != null) drawPile.Add(card);
            }
            var discardPile = new List<CardDefinitionSO>();
            foreach (var name in saveData.discardPileCardNames)
            {
                var card = deckManager.FindCardByName(name);
                if (card != null) discardPile.Add(card);
            }
            deckManager.RestorePiles(drawPile, discardPile);

            // Restore wounded heroes (standalone mode — approximate by SO reference since we don't have deck indices)
            woundedHeroIndices.Clear();
            foreach (var name in saveData.woundedHeroCardNames)
            {
                Debug.Log($"[GameManager] LoadRun: restored wounded hero '{name}' (standalone mode, SO-based)");
            }

            // Restore living enemies at saved positions
            foreach (var posStr in saveData.livingEnemyPositions)
            {
                var parts = posStr.Split(',');
                if (parts.Length == 3 && int.TryParse(parts[0], out int r) && int.TryParse(parts[1], out int c) && int.TryParse(parts[2], out int str))
                {
                    gatheringManager.SpawnEnemyAt(new Vector2Int(r, c), str);
                    Debug.Log($"[GameManager] LoadRun: restored living enemy at ({r},{c}) str={str}");
                }
            }

            // Generate board resources and update tile types for enemy positions
            boardManager.GenerateResourceNodeResources();
            boardManager.UpdateTilesForEnemyMovement();

            Debug.Log("[GameManager] LoadRun: state restored, starting next turn");
            StartNewTurn();
        }

        private void SetPhase(GamePhase phase)
        {
            var previousPhase = currentPhase;
            currentPhase = phase;
            Debug.Log($"[GameManager] SetPhase: {previousPhase} -> {phase}");
            EventBus.OnPhaseChanged?.Invoke(phase);
        }

        public GamePhase CurrentPhase => currentPhase;
        public int TurnNumber => turnNumber;
        public bool StandaloneMode => standaloneMode;
        public List<CardDefinitionSO> ExhaustedResources => exhaustedResources;

        public bool IsHeroWounded(CardDefinitionSO card)
        {
            // Check if any wounded index matches this card SO
            if (currentRunDeck != null)
            {
                foreach (int idx in woundedHeroIndices)
                {
                    if (idx < currentRunDeck.Count && currentRunDeck[idx] == card)
                    {
                        Debug.Log($"[GameManager] IsHeroWounded: card='{card.cardName}', wounded=True (index={idx})");
                        return true;
                    }
                }
            }
            Debug.Log($"[GameManager] IsHeroWounded: card='{card.cardName}', wounded=False");
            return false;
        }

        public void ClearWoundedHeroes()
        {
            Debug.Log($"[GameManager] ClearWoundedHeroes: clearing {woundedHeroIndices.Count} wounded hero indices");
            woundedHeroIndices.Clear();
        }

        /// <summary>
        /// Find a deck index for the given card. If findWounded=true, finds a wounded index. Otherwise finds an unwounded one.
        /// </summary>
        private int FindDeckIndex(CardDefinitionSO card, bool findWounded)
        {
            if (currentRunDeck == null) return -1;
            for (int i = 0; i < currentRunDeck.Count; i++)
            {
                if (currentRunDeck[i] == card)
                {
                    bool isWounded = woundedHeroIndices.Contains(i);
                    if (findWounded == isWounded)
                        return i;
                }
            }
            return -1;
        }
    }
}
