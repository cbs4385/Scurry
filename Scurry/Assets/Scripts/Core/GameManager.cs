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
        private readonly HashSet<CardDefinitionSO> woundedHeroes = new HashSet<CardDefinitionSO>();

        private void Awake()
        {
            Debug.Log($"[GameManager] Awake: boardManager={boardManager?.name ?? "NULL"}, deckManager={deckManager?.name ?? "NULL"}, handManager={handManager?.name ?? "NULL"}, placementManager={placementManager?.name ?? "NULL"}, gatheringManager={gatheringManager?.name ?? "NULL"}, colonyManager={colonyManager?.name ?? "NULL"}");
        }

        private void OnEnable()
        {
            Debug.Log("[GameManager] OnEnable: subscribing to EventBus events");
            EventBus.OnTurnEnded += OnEndTurnPressed;
            EventBus.OnGatheringComplete += OnGatheringDone;
            EventBus.OnDeckBuildComplete += OnDeckBuildDone;
            EventBus.OnResourceTokenCollected += OnResourceTokenCollected;
        }

        private void OnDisable()
        {
            Debug.Log("[GameManager] OnDisable: unsubscribing from EventBus events");
            EventBus.OnTurnEnded -= OnEndTurnPressed;
            EventBus.OnGatheringComplete -= OnGatheringDone;
            EventBus.OnDeckBuildComplete -= OnDeckBuildDone;
            EventBus.OnResourceTokenCollected -= OnResourceTokenCollected;
        }

        private void Start()
        {
            Debug.Log("[GameManager] Start: beginning run");
            EventBus.LogSubscriberCounts();

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
            // Player-placed resource tokens (not auto-generated) return their card to the discard pile
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
                deckManager.DiscardCard(card);
                Debug.Log($"[GameManager] OnResourceTokenCollected: '{cardName}' returned to discard pile");
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
                SaveManager.DeleteSave();
                return;
            }

            Debug.Log("[GameManager] OnGatheringDone: colony alive, starting resolve and next turn");
            StartCoroutine(ResolveAndNextTurn());
        }

        private IEnumerator ResolveAndNextTurn()
        {
            Debug.Log("[GameManager] ResolveAndNextTurn: waiting 0.5s before cleanup");
            yield return new WaitForSeconds(0.5f);

            // Track wounds: scan all HeroAgents on the board
            var heroes = boardManager.GetComponentsInChildren<HeroAgent>();
            Debug.Log($"[GameManager] ResolveAndNextTurn: scanning {heroes.Length} heroes for wound tracking");
            foreach (var hero in heroes)
            {
                if (hero.IsHealing)
                {
                    // Hero sat out this turn to heal — remove from wounded set
                    woundedHeroes.Remove(hero.CardData);
                    Debug.Log($"[GameManager] ResolveAndNextTurn: hero='{hero.CardData.cardName}' healed (sat out gathering), removed from woundedHeroes");
                }
                else if (hero.IsWounded)
                {
                    // Hero got freshly wounded this turn — add to wounded set
                    woundedHeroes.Add(hero.CardData);
                    Debug.Log($"[GameManager] ResolveAndNextTurn: hero='{hero.CardData.cardName}' freshly wounded, added to woundedHeroes");
                }
                else
                {
                    Debug.Log($"[GameManager] ResolveAndNextTurn: hero='{hero.CardData.cardName}' healthy");
                }
            }
            Debug.Log($"[GameManager] ResolveAndNextTurn: woundedHeroes count={woundedHeroes.Count}");

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
                Debug.Log("[GameManager] ===== RUN COMPLETE — All resources collected! =====");
                SaveManager.DeleteSave();
                yield break;
            }

            // Condition C: No enemies remain — auto-collect all remaining resources
            if (!hasEnemies)
            {
                Debug.Log("[GameManager] ===== RUN COMPLETE — All enemies defeated! Auto-collecting remaining resources =====");
                boardManager.CollectAllRemainingResources();
                SaveManager.DeleteSave();
                yield break;
            }

            // Condition A: No heroes in play AND no playable hero cards in deck
            if (!hasHeroesOnBoard && !hasHeroCardsInDeck)
            {
                Debug.Log("[GameManager] ===== RUN COMPLETE — No heroes available (none on board, none in deck)! =====");
                SaveManager.DeleteSave();
                yield break;
            }

            // No end condition met — continue to next turn
            Debug.Log("[GameManager] ResolveAndNextTurn: continuing to next turn");
            SaveRunState();
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

            // Save wounded heroes
            foreach (var card in woundedHeroes)
                saveData.woundedHeroCardNames.Add(card.cardName);

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

            // Restore wounded heroes
            woundedHeroes.Clear();
            foreach (var name in saveData.woundedHeroCardNames)
            {
                var card = deckManager.FindCardByName(name);
                if (card != null)
                {
                    woundedHeroes.Add(card);
                    Debug.Log($"[GameManager] LoadRun: restored wounded hero '{name}'");
                }
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

        public bool IsHeroWounded(CardDefinitionSO card)
        {
            bool wounded = woundedHeroes.Contains(card);
            Debug.Log($"[GameManager] IsHeroWounded: card='{card.cardName}', wounded={wounded}");
            return wounded;
        }
    }
}
