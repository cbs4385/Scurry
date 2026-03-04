using System.Collections;
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

        [Header("Settings")]
        [SerializeField] private int cardsPerTurn = 5;

        private GamePhase currentPhase;
        private int turnNumber;

        private void Awake()
        {
            Debug.Log($"[GameManager] Awake: boardManager={boardManager?.name ?? "NULL"}, deckManager={deckManager?.name ?? "NULL"}, handManager={handManager?.name ?? "NULL"}, placementManager={placementManager?.name ?? "NULL"}, gatheringManager={gatheringManager?.name ?? "NULL"}, colonyManager={colonyManager?.name ?? "NULL"}");
        }

        private void OnEnable()
        {
            Debug.Log("[GameManager] OnEnable: subscribing to EventBus.OnTurnEnded and OnGatheringComplete");
            EventBus.OnTurnEnded += OnEndTurnPressed;
            EventBus.OnGatheringComplete += OnGatheringDone;
        }

        private void OnDisable()
        {
            Debug.Log("[GameManager] OnDisable: unsubscribing from EventBus events");
            EventBus.OnTurnEnded -= OnEndTurnPressed;
            EventBus.OnGatheringComplete -= OnGatheringDone;
        }

        private void Start()
        {
            Debug.Log("[GameManager] Start: beginning run");
            EventBus.LogSubscriberCounts();
            StartRun();
        }

        private void StartRun()
        {
            Debug.Log("[GameManager] StartRun: initializing game");
            turnNumber = 0;
            deckManager.InitializeDeck();
            colonyManager.InitializeHP();
            gatheringManager.SpawnEnemies();
            Debug.Log("[GameManager] StartRun: initialization complete, starting first turn");
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

        private void OnEndTurnPressed()
        {
            Debug.Log($"[GameManager] OnEndTurnPressed: currentPhase={currentPhase}");
            if (currentPhase != GamePhase.Deploy)
            {
                Debug.LogWarning($"[GameManager] OnEndTurnPressed: ignoring — not in Deploy phase (current={currentPhase})");
                return;
            }

            Debug.Log("[GameManager] OnEndTurnPressed: Deploy phase ended. Returning unplayed cards to deck...");
            SetPhase(GamePhase.Gather);

            // Return unplayed cards to deck
            var handCards = handManager.GetHandCards();
            Debug.Log($"[GameManager] OnEndTurnPressed: returning {handCards.Count} unplayed cards to deck");
            foreach (var card in handCards)
            {
                Debug.Log($"[GameManager] OnEndTurnPressed: returning '{card.CardData?.cardName ?? "?"}' to deck");
                deckManager.ReturnToDeck(card.CardData);
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
                return;
            }

            Debug.Log("[GameManager] OnGatheringDone: colony alive, starting resolve and next turn");
            StartCoroutine(ResolveAndNextTurn());
        }

        private IEnumerator ResolveAndNextTurn()
        {
            Debug.Log("[GameManager] ResolveAndNextTurn: waiting 0.5s before cleanup");
            yield return new WaitForSeconds(0.5f);

            Debug.Log("[GameManager] ResolveAndNextTurn: clearing tokens and resetting tiles");
            placementManager.ClearTokens();
            boardManager.ResetAllTiles();
            gatheringManager.SpawnEnemies();

            Debug.Log("[GameManager] ResolveAndNextTurn: cleanup complete, starting new turn");
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
    }
}
