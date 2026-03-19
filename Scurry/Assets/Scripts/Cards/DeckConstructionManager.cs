using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;
using Scurry.UI;

namespace Scurry.Cards
{
    /// <summary>
    /// Manages the pre-game deck building screen. Players select 10-30 cards from the
    /// full card pool, respecting copy limits based on deck cost (1-cost=3 copies,
    /// 2-cost=2 copies, 3+-cost=1 copy).
    /// </summary>
    public class DeckConstructionManager : MonoBehaviour
    {
        public const int MIN_DECK_SIZE = 10;
        public const int MAX_DECK_SIZE = 30;

        // Full card pool (from CardDatabase)
        private List<CardDefinitionSO> cardPool = new List<CardDefinitionSO>();
        private List<ColonyCardDefinitionSO> colonyCardPool = new List<ColonyCardDefinitionSO>();

        // Current deck being built
        private List<CardDefinitionSO> currentDeck = new List<CardDefinitionSO>();
        private List<ColonyCardDefinitionSO> currentColonyDeck = new List<ColonyCardDefinitionSO>();

        // Count tracking for copy limits
        private Dictionary<int, int> cardCopyCounts = new Dictionary<int, int>();
        private Dictionary<int, int> colonyCardCopyCounts = new Dictionary<int, int>();

        // Public accessors
        public IReadOnlyList<CardDefinitionSO> CardPool => cardPool;
        public IReadOnlyList<ColonyCardDefinitionSO> ColonyCardPool => colonyCardPool;
        public IReadOnlyList<CardDefinitionSO> CurrentDeck => currentDeck;
        public IReadOnlyList<ColonyCardDefinitionSO> CurrentColonyDeck => currentColonyDeck;
        public int DeckSize => currentDeck.Count + currentColonyDeck.Count;
        public bool IsDeckValid => DeckSize >= MIN_DECK_SIZE && DeckSize <= MAX_DECK_SIZE;

        private void Awake()
        {
            Debug.Log("[DeckConstructionManager] Awake: initializing deck construction manager");
        }

        private void Start()
        {
            Debug.Log("[DeckConstructionManager] Start: loading card pool from CardDatabase");
            Initialize();

            // Notify the UI to refresh now that initialization is complete
            var ui = FindAnyObjectByType<DeckConstructionUI>();
            if (ui != null)
            {
                Debug.Log("[DeckConstructionManager] Start: notifying DeckConstructionUI to refresh");
                ui.Initialize(this);
            }
        }

        /// <summary>
        /// Loads the full card pool from CardDatabase for browsing.
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[DeckConstructionManager] Initialize: loading card pools");

            var db = CardDatabase.Instance;

            // Load main card pool (Heroes, Equipment, Tactical)
            cardPool.Clear();
            foreach (var card in db.AllCards)
            {
                cardPool.Add(card);
                Debug.Log($"[DeckConstructionManager] Initialize: added to card pool — '{card.cardName}' (id={card.cardId}, type={card.cardType}, cost={card.deckCost}, rarity={card.rarity})");
            }
            Debug.Log($"[DeckConstructionManager] Initialize: card pool loaded (count={cardPool.Count})");

            // Load colony card pool
            colonyCardPool.Clear();
            foreach (var card in db.AllColonyCards)
            {
                colonyCardPool.Add(card);
                Debug.Log($"[DeckConstructionManager] Initialize: added to colony pool — '{card.cardName}' (id={card.cardId}, tier={card.colonyTier}, cost={card.deckCost})");
            }
            Debug.Log($"[DeckConstructionManager] Initialize: colony card pool loaded (count={colonyCardPool.Count})");

            // Auto-add starter colony cards
            int startersAdded = 0;
            foreach (var card in colonyCardPool)
            {
                if (card.isStarter)
                {
                    currentColonyDeck.Add(card);
                    int copyCount = colonyCardCopyCounts.ContainsKey(card.cardId) ? colonyCardCopyCounts[card.cardId] : 0;
                    colonyCardCopyCounts[card.cardId] = copyCount + 1;
                    startersAdded++;
                    Debug.Log($"[DeckConstructionManager] Initialize: auto-added starter colony card '{card.cardName}' (id={card.cardId})");
                }
            }
            Debug.Log($"[DeckConstructionManager] Initialize: added {startersAdded} starter colony cards, deckSize={DeckSize}");

            // Pre-populate from last constructed deck (if available)
            var meta = MetaProgressionManager.Instance;
            if (meta != null && meta.LastDeckCardIds.Count > 0)
            {
                Debug.Log($"[DeckConstructionManager] Initialize: restoring last deck (cards={meta.LastDeckCardIds.Count}, colony={meta.LastColonyDeckCardIds.Count})");

                // Restore main deck cards — verify each is still in the pool
                foreach (int cardId in meta.LastDeckCardIds)
                {
                    var card = cardPool.Find(c => c.cardId == cardId);
                    if (card == null)
                    {
                        Debug.Log($"[DeckConstructionManager] Initialize: skipping card id={cardId} — not in pool (removed or locked)");
                        continue;
                    }
                    if (!CanAddCard(card))
                    {
                        Debug.Log($"[DeckConstructionManager] Initialize: skipping '{card.cardName}' — copy limit or deck full");
                        continue;
                    }
                    currentDeck.Add(card);
                    if (!cardCopyCounts.ContainsKey(card.cardId))
                        cardCopyCounts[card.cardId] = 0;
                    cardCopyCounts[card.cardId]++;
                    Debug.Log($"[DeckConstructionManager] Initialize: restored '{card.cardName}' (id={card.cardId})");
                }

                // Restore colony deck cards — skip starters (already added) and verify unlock
                foreach (int cardId in meta.LastColonyDeckCardIds)
                {
                    var card = colonyCardPool.Find(c => c.cardId == cardId);
                    if (card == null)
                    {
                        Debug.Log($"[DeckConstructionManager] Initialize: skipping colony card id={cardId} — not in pool");
                        continue;
                    }
                    if (card.isStarter)
                        continue; // Already added above
                    if (meta.IsColonyCardUnlocked(card.cardName) == false && card.deckCost > 0)
                    {
                        Debug.Log($"[DeckConstructionManager] Initialize: skipping colony '{card.cardName}' — no longer unlocked");
                        continue;
                    }
                    if (!CanAddColonyCard(card))
                    {
                        Debug.Log($"[DeckConstructionManager] Initialize: skipping colony '{card.cardName}' — copy limit or deck full");
                        continue;
                    }
                    currentColonyDeck.Add(card);
                    if (!colonyCardCopyCounts.ContainsKey(card.cardId))
                        colonyCardCopyCounts[card.cardId] = 0;
                    colonyCardCopyCounts[card.cardId]++;
                    Debug.Log($"[DeckConstructionManager] Initialize: restored colony '{card.cardName}' (id={card.cardId})");
                }

                Debug.Log($"[DeckConstructionManager] Initialize: last deck restored, deckSize={DeckSize}");
            }
        }

        /// <summary>
        /// Returns the maximum number of copies allowed for a given deck cost.
        /// Cost 1 = 3 copies, Cost 2 = 2 copies, Cost 3+ = 1 copy (singleton).
        /// </summary>
        public int GetMaxCopies(int deckCost)
        {
            int maxCopies;
            if (deckCost <= 0)
            {
                maxCopies = 3; // Treat 0 or negative as cost 1
                Debug.Log($"[DeckConstructionManager] GetMaxCopies: deckCost={deckCost} (treated as 1), maxCopies={maxCopies}");
            }
            else if (deckCost == 1)
            {
                maxCopies = 3;
                Debug.Log($"[DeckConstructionManager] GetMaxCopies: deckCost={deckCost}, maxCopies={maxCopies}");
            }
            else if (deckCost == 2)
            {
                maxCopies = 2;
                Debug.Log($"[DeckConstructionManager] GetMaxCopies: deckCost={deckCost}, maxCopies={maxCopies}");
            }
            else
            {
                maxCopies = 1;
                Debug.Log($"[DeckConstructionManager] GetMaxCopies: deckCost={deckCost}, maxCopies={maxCopies} (singleton)");
            }
            return maxCopies;
        }

        /// <summary>
        /// Returns the current number of copies of a card in the deck.
        /// </summary>
        public int GetCurrentCopies(int cardId)
        {
            int copies = cardCopyCounts.ContainsKey(cardId) ? cardCopyCounts[cardId] : 0;
            Debug.Log($"[DeckConstructionManager] GetCurrentCopies: cardId={cardId}, copies={copies}");
            return copies;
        }

        /// <summary>
        /// Returns the current number of copies of a colony card in the deck.
        /// </summary>
        public int GetCurrentColonyCopies(int cardId)
        {
            int copies = colonyCardCopyCounts.ContainsKey(cardId) ? colonyCardCopyCounts[cardId] : 0;
            Debug.Log($"[DeckConstructionManager] GetCurrentColonyCopies: cardId={cardId}, copies={copies}");
            return copies;
        }

        /// <summary>
        /// Checks whether a card can be added to the deck (copy limit and deck size).
        /// </summary>
        public bool CanAddCard(CardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionManager] CanAddCard: checking '{card.cardName}' (id={card.cardId}, cost={card.deckCost})");

            if (DeckSize >= MAX_DECK_SIZE)
            {
                Debug.Log($"[DeckConstructionManager] CanAddCard: DENIED — deck is full (size={DeckSize}/{MAX_DECK_SIZE})");
                return false;
            }

            int currentCopies = GetCurrentCopies(card.cardId);
            int maxCopies = GetMaxCopies(card.deckCost);

            if (currentCopies >= maxCopies)
            {
                Debug.Log($"[DeckConstructionManager] CanAddCard: DENIED — at copy limit (current={currentCopies}, max={maxCopies})");
                return false;
            }

            Debug.Log($"[DeckConstructionManager] CanAddCard: ALLOWED (copies={currentCopies}/{maxCopies}, deckSize={DeckSize}/{MAX_DECK_SIZE})");
            return true;
        }

        /// <summary>
        /// Checks whether a colony card can be added to the deck (copy limit and deck size).
        /// </summary>
        public bool CanAddColonyCard(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionManager] CanAddColonyCard: checking '{card.cardName}' (id={card.cardId}, cost={card.deckCost})");

            if (DeckSize >= MAX_DECK_SIZE)
            {
                Debug.Log($"[DeckConstructionManager] CanAddColonyCard: DENIED — deck is full (size={DeckSize}/{MAX_DECK_SIZE})");
                return false;
            }

            int currentCopies = GetCurrentColonyCopies(card.cardId);
            int maxCopies = GetMaxCopies(card.deckCost);

            if (currentCopies >= maxCopies)
            {
                Debug.Log($"[DeckConstructionManager] CanAddColonyCard: DENIED — at copy limit (current={currentCopies}, max={maxCopies})");
                return false;
            }

            Debug.Log($"[DeckConstructionManager] CanAddColonyCard: ALLOWED (copies={currentCopies}/{maxCopies}, deckSize={DeckSize}/{MAX_DECK_SIZE})");
            return true;
        }

        /// <summary>
        /// Adds a card to the deck. Returns true if successful, false if at copy limit or deck full.
        /// </summary>
        public bool AddCard(CardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionManager] AddCard: attempting to add '{card.cardName}' (id={card.cardId}, type={card.cardType}, cost={card.deckCost})");

            if (!CanAddCard(card))
            {
                Debug.Log($"[DeckConstructionManager] AddCard: FAILED — cannot add '{card.cardName}'");
                return false;
            }

            currentDeck.Add(card);

            if (!cardCopyCounts.ContainsKey(card.cardId))
                cardCopyCounts[card.cardId] = 0;
            cardCopyCounts[card.cardId]++;

            Debug.Log($"[DeckConstructionManager] AddCard: SUCCESS — '{card.cardName}' added (copies={cardCopyCounts[card.cardId]}/{GetMaxCopies(card.deckCost)}, deckSize={DeckSize})");
            return true;
        }

        /// <summary>
        /// Removes a card from the deck. Returns true if found and removed.
        /// </summary>
        public bool RemoveCard(CardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionManager] RemoveCard: attempting to remove '{card.cardName}' (id={card.cardId})");

            int index = currentDeck.IndexOf(card);
            if (index < 0)
            {
                // Try finding by ID if reference doesn't match
                index = currentDeck.FindIndex(c => c.cardId == card.cardId);
            }

            if (index < 0)
            {
                Debug.Log($"[DeckConstructionManager] RemoveCard: FAILED — '{card.cardName}' not found in deck");
                return false;
            }

            currentDeck.RemoveAt(index);

            if (cardCopyCounts.ContainsKey(card.cardId))
            {
                cardCopyCounts[card.cardId]--;
                if (cardCopyCounts[card.cardId] <= 0)
                    cardCopyCounts.Remove(card.cardId);
            }

            Debug.Log($"[DeckConstructionManager] RemoveCard: SUCCESS — '{card.cardName}' removed (remaining copies={GetCurrentCopies(card.cardId)}, deckSize={DeckSize})");
            return true;
        }

        /// <summary>
        /// Adds a colony card to the deck. Returns true if successful.
        /// </summary>
        public bool AddColonyCard(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionManager] AddColonyCard: attempting to add '{card.cardName}' (id={card.cardId}, tier={card.colonyTier}, cost={card.deckCost})");

            if (!CanAddColonyCard(card))
            {
                Debug.Log($"[DeckConstructionManager] AddColonyCard: FAILED — cannot add '{card.cardName}'");
                return false;
            }

            currentColonyDeck.Add(card);

            if (!colonyCardCopyCounts.ContainsKey(card.cardId))
                colonyCardCopyCounts[card.cardId] = 0;
            colonyCardCopyCounts[card.cardId]++;

            Debug.Log($"[DeckConstructionManager] AddColonyCard: SUCCESS — '{card.cardName}' added (copies={colonyCardCopyCounts[card.cardId]}/{GetMaxCopies(card.deckCost)}, deckSize={DeckSize})");
            return true;
        }

        /// <summary>
        /// Removes a colony card from the deck. Returns true if found and removed.
        /// Starter cards cannot be removed.
        /// </summary>
        public bool RemoveColonyCard(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[DeckConstructionManager] RemoveColonyCard: attempting to remove '{card.cardName}' (id={card.cardId}, isStarter={card.isStarter})");

            if (card.isStarter)
            {
                Debug.Log($"[DeckConstructionManager] RemoveColonyCard: DENIED — '{card.cardName}' is a starter card and cannot be removed");
                return false;
            }

            int index = currentColonyDeck.IndexOf(card);
            if (index < 0)
            {
                index = currentColonyDeck.FindIndex(c => c.cardId == card.cardId);
            }

            if (index < 0)
            {
                Debug.Log($"[DeckConstructionManager] RemoveColonyCard: FAILED — '{card.cardName}' not found in colony deck");
                return false;
            }

            currentColonyDeck.RemoveAt(index);

            if (colonyCardCopyCounts.ContainsKey(card.cardId))
            {
                colonyCardCopyCounts[card.cardId]--;
                if (colonyCardCopyCounts[card.cardId] <= 0)
                    colonyCardCopyCounts.Remove(card.cardId);
            }

            Debug.Log($"[DeckConstructionManager] RemoveColonyCard: SUCCESS — '{card.cardName}' removed (remaining copies={GetCurrentColonyCopies(card.cardId)}, deckSize={DeckSize})");
            return true;
        }

        /// <summary>
        /// Validates the deck and sends it to RunManager to proceed to GameMap.
        /// </summary>
        public void ConfirmDeck()
        {
            Debug.Log($"[DeckConstructionManager] ConfirmDeck: validating deck (size={DeckSize}, min={MIN_DECK_SIZE}, max={MAX_DECK_SIZE})");

            if (!IsDeckValid)
            {
                Debug.LogWarning($"[DeckConstructionManager] ConfirmDeck: INVALID deck size={DeckSize} — must be between {MIN_DECK_SIZE} and {MAX_DECK_SIZE}");
                EventBus.OnNotification?.Invoke($"Deck must contain {MIN_DECK_SIZE}-{MAX_DECK_SIZE} cards (current: {DeckSize})", Color.red);
                return;
            }

            // Check minimum hero count
            int heroCount = currentDeck.Count(c => c.cardType == CardType.Hero);
            if (heroCount < 1)
            {
                Debug.LogWarning("[DeckConstructionManager] ConfirmDeck: INVALID — deck must contain at least 1 hero card");
                EventBus.OnNotification?.Invoke("Deck must contain at least 1 hero card!", Color.red);
                return;
            }

            Debug.Log($"[DeckConstructionManager] ConfirmDeck: deck is valid — heroes={heroCount}, equipment={currentDeck.Count(c => c.cardType == CardType.Equipment)}, " +
                       $"tactical={currentDeck.Count(c => c.cardType == CardType.Tactical)}, colony={currentColonyDeck.Count}");

            // Log the full confirmed deck
            Debug.Log("[DeckConstructionManager] ConfirmDeck: === CONFIRMED DECK ===");
            for (int i = 0; i < currentDeck.Count; i++)
            {
                var card = currentDeck[i];
                Debug.Log($"[DeckConstructionManager] ConfirmDeck: [{i}] '{card.cardName}' (id={card.cardId}, type={card.cardType}, cost={card.deckCost})");
            }
            for (int i = 0; i < currentColonyDeck.Count; i++)
            {
                var card = currentColonyDeck[i];
                Debug.Log($"[DeckConstructionManager] ConfirmDeck: [colony {i}] '{card.cardName}' (id={card.cardId}, tier={card.colonyTier}, cost={card.deckCost})");
            }
            Debug.Log("[DeckConstructionManager] ConfirmDeck: === END CONFIRMED DECK ===");

            // Send to RunManager
            var runManager = RunManager.Instance;
            if (runManager == null)
            {
                runManager = ServiceLocator.Get<RunManager>();
            }

            if (runManager != null)
            {
                Debug.Log("[DeckConstructionManager] ConfirmDeck: sending deck to RunManager.OnDeckConstructionComplete");
                runManager.OnDeckConstructionComplete(
                    new List<CardDefinitionSO>(currentDeck),
                    new List<ColonyCardDefinitionSO>(currentColonyDeck)
                );
            }
            else
            {
                Debug.LogError("[DeckConstructionManager] ConfirmDeck: RunManager not found! Cannot proceed to GameMap");
            }
        }

        /// <summary>
        /// Clears the entire deck (except starter colony cards).
        /// </summary>
        public void ClearDeck()
        {
            Debug.Log($"[DeckConstructionManager] ClearDeck: clearing deck (currentSize={DeckSize})");

            currentDeck.Clear();
            cardCopyCounts.Clear();
            Debug.Log("[DeckConstructionManager] ClearDeck: main deck cleared");

            // Re-add starter colony cards only
            var starters = currentColonyDeck.Where(c => c.isStarter).ToList();
            currentColonyDeck.Clear();
            colonyCardCopyCounts.Clear();

            foreach (var starter in starters)
            {
                currentColonyDeck.Add(starter);
                if (!colonyCardCopyCounts.ContainsKey(starter.cardId))
                    colonyCardCopyCounts[starter.cardId] = 0;
                colonyCardCopyCounts[starter.cardId]++;
                Debug.Log($"[DeckConstructionManager] ClearDeck: re-added starter '{starter.cardName}' (id={starter.cardId})");
            }

            Debug.Log($"[DeckConstructionManager] ClearDeck: complete — deckSize={DeckSize} (starters={starters.Count})");
        }

        // ── Filter / Sort Helpers ────────────────────────────────────────

        /// <summary>
        /// Returns all cards in the pool matching the given card type.
        /// </summary>
        public List<CardDefinitionSO> GetCardsByType(CardType type)
        {
            var result = cardPool.Where(c => c.cardType == type).ToList();
            Debug.Log($"[DeckConstructionManager] GetCardsByType: type={type}, found={result.Count}");
            return result;
        }

        /// <summary>
        /// Returns all cards in the pool matching the given rarity.
        /// </summary>
        public List<CardDefinitionSO> GetCardsByRarity(CardRarity rarity)
        {
            var result = cardPool.Where(c => c.rarity == rarity).ToList();
            Debug.Log($"[DeckConstructionManager] GetCardsByRarity: rarity={rarity}, found={result.Count}");
            return result;
        }

        /// <summary>
        /// Returns all cards in the pool sorted by deck cost (ascending), then by name.
        /// </summary>
        public List<CardDefinitionSO> GetCardsSortedByCost()
        {
            var result = cardPool.OrderBy(c => c.deckCost).ThenBy(c => c.cardName).ToList();
            Debug.Log($"[DeckConstructionManager] GetCardsSortedByCost: returning {result.Count} cards sorted by cost");
            return result;
        }

        /// <summary>
        /// Returns all colony cards in the pool matching the given tier.
        /// </summary>
        public List<ColonyCardDefinitionSO> GetColonyCardsByTier(ColonyTier tier)
        {
            var result = colonyCardPool.Where(c => c.colonyTier == tier).ToList();
            Debug.Log($"[DeckConstructionManager] GetColonyCardsByTier: tier={tier}, found={result.Count}");
            return result;
        }

        /// <summary>
        /// Returns all colony cards in the pool sorted by deck cost (ascending), then by name.
        /// </summary>
        public List<ColonyCardDefinitionSO> GetColonyCardsSortedByCost()
        {
            var result = colonyCardPool.OrderBy(c => c.deckCost).ThenBy(c => c.cardName).ToList();
            Debug.Log($"[DeckConstructionManager] GetColonyCardsSortedByCost: returning {result.Count} colony cards sorted by cost");
            return result;
        }
    }
}
