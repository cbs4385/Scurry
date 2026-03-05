using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Board;
using Scurry.Cards;
using Scurry.Core;

namespace Scurry.Placement
{
    public class PlacementManager : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private HandManager handManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject heroTokenPrefab;
        [SerializeField] private GameObject resourceTokenPrefab;

        private bool placementEnabled;

        private struct PlacementRecord
        {
            public CardDefinitionSO card;
            public Vector2Int gridPos;
            public GameObject token;
        }
        private readonly Stack<PlacementRecord> placementHistory = new Stack<PlacementRecord>();

        private void Awake()
        {
            Debug.Log($"[PlacementManager] Awake: boardManager={boardManager?.name ?? "NULL"}, handManager={handManager?.name ?? "NULL"}, gameManager={gameManager?.name ?? "NULL"}, heroTokenPrefab={heroTokenPrefab?.name ?? "NULL"}, resourceTokenPrefab={resourceTokenPrefab?.name ?? "NULL"}");
        }

        private void OnEnable()
        {
            Debug.Log("[PlacementManager] OnEnable: subscribing to EventBus.OnPhaseChanged, OnUndoPlacement");
            EventBus.OnPhaseChanged += OnPhaseChanged;
            EventBus.OnUndoPlacement += UndoLastPlacement;
        }

        private void OnDisable()
        {
            Debug.Log("[PlacementManager] OnDisable: unsubscribing from EventBus events");
            EventBus.OnPhaseChanged -= OnPhaseChanged;
            EventBus.OnUndoPlacement -= UndoLastPlacement;
        }

        private void Start()
        {
            Debug.Log($"[PlacementManager] Start: handManager={handManager?.name ?? "NULL"}, wiring OnCardDropped");
            if (handManager != null)
            {
                handManager.OnCardDropped += HandleCardDrop;
                Debug.Log("[PlacementManager] Start: OnCardDropped wired successfully");
            }
            else
            {
                Debug.LogError("[PlacementManager] Start: handManager is NULL — card drops will not be handled!");
            }
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            placementEnabled = (phase == GamePhase.Deploy);
            if (phase == GamePhase.Deploy)
                placementHistory.Clear();
            Debug.Log($"[PlacementManager] OnPhaseChanged: phase={phase}, placementEnabled={placementEnabled}, historyCleared={phase == GamePhase.Deploy}");
        }

        private void HandleCardDrop(CardView cardView, Vector3 worldPos)
        {
            Debug.Log($"[PlacementManager] HandleCardDrop: card='{cardView?.CardData?.cardName ?? "?"}', type={cardView?.CardData?.cardType}, worldPos={worldPos}, placementEnabled={placementEnabled}");

            if (!placementEnabled)
            {
                Debug.Log("[PlacementManager] HandleCardDrop: placement not enabled — snapping back");
                cardView.SnapBack();
                return;
            }

            Vector2Int? gridPos = boardManager.GetGridPosition(worldPos);
            if (!gridPos.HasValue)
            {
                Debug.Log($"[PlacementManager] HandleCardDrop: worldPos={worldPos} is off the grid — snapping back");
                cardView.SnapBack();
                return;
            }

            Tile tile = boardManager.GetTile(gridPos.Value);
            if (tile == null)
            {
                Debug.LogWarning($"[PlacementManager] HandleCardDrop: tile at gridPos={gridPos.Value} is null — snapping back");
                cardView.SnapBack();
                return;
            }

            Debug.Log($"[PlacementManager] HandleCardDrop: gridPos={gridPos.Value}, tileType={tile.TileType}, hasHero={tile.HasHero}, hasResource={tile.HasResource}");

            CardDefinitionSO card = cardView.CardData;
            bool valid = false;

            GameObject token = null;
            if (card.cardType == CardType.Hero && tile.CanPlaceHero())
            {
                valid = true;
                token = PlaceHeroToken(card, tile);
                Debug.Log($"[PlacementManager] HandleCardDrop: HERO placed — card='{card.cardName}', tile=({tile.GridPosition})");
            }
            else if (card.cardType == CardType.Resource && tile.CanPlaceResource())
            {
                valid = true;
                token = PlaceResourceToken(card, tile);
                Debug.Log($"[PlacementManager] HandleCardDrop: RESOURCE placed — card='{card.cardName}', tile=({tile.GridPosition})");
            }

            if (valid)
            {
                placementHistory.Push(new PlacementRecord { card = card, gridPos = gridPos.Value, token = token });
                handManager.RemoveCardFromHand(cardView);
                EventBus.OnCardPlaced?.Invoke(card, gridPos.Value);
                Debug.Log($"[PlacementManager] HandleCardDrop: card placed successfully, removed from hand, event fired, history count={placementHistory.Count}");
            }
            else
            {
                Debug.Log($"[PlacementManager] HandleCardDrop: INVALID placement — card type={card.cardType}, tileType={tile.TileType} — snapping back");
                cardView.SnapBack();
            }
        }

        private GameObject PlaceHeroToken(CardDefinitionSO card, Tile tile)
        {
            Vector3 worldPos = boardManager.GetWorldPosition(tile.GridPosition);
            worldPos.z = -0.1f;
            GameObject token = Instantiate(heroTokenPrefab, worldPos, Quaternion.identity, boardManager.transform);
            token.name = $"Hero_{card.cardName}";
            Debug.Log($"[PlacementManager] PlaceHeroToken: instantiated '{token.name}' at worldPos={worldPos}");

            var sr = token.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                SpriteHelper.EnsureSprite(sr);
                sr.color = card.placeholderColor;
                sr.sortingOrder = 5;
            }
            else
            {
                Debug.LogWarning($"[PlacementManager] PlaceHeroToken: no SpriteRenderer on heroTokenPrefab");
            }

            SpriteHelper.AddOutline(token, 5);

            var heroAgent = token.GetComponent<Gathering.HeroAgent>();
            if (heroAgent != null)
            {
                heroAgent.Initialize(card, tile.GridPosition);
                Debug.Log($"[PlacementManager] PlaceHeroToken: HeroAgent initialized at gridPos={tile.GridPosition}");

                // Check persistent wound status
                if (gameManager != null && gameManager.IsHeroWounded(card))
                {
                    heroAgent.SetHealing();
                    Debug.Log($"[PlacementManager] PlaceHeroToken: hero='{card.cardName}' is wounded — set to healing (will skip gathering)");
                }
            }
            else
            {
                Debug.LogWarning($"[PlacementManager] PlaceHeroToken: no HeroAgent component on heroTokenPrefab");
            }

            tile.HasHero = true;
            return token;
        }

        private GameObject PlaceResourceToken(CardDefinitionSO card, Tile tile)
        {
            Vector3 worldPos = boardManager.GetWorldPosition(tile.GridPosition);
            worldPos.z = -0.05f;
            GameObject token = Instantiate(resourceTokenPrefab, worldPos, Quaternion.identity, boardManager.transform);
            token.name = $"Resource_{card.cardName}";
            Debug.Log($"[PlacementManager] PlaceResourceToken: instantiated '{token.name}' at worldPos={worldPos}");

            var sr = token.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                SpriteHelper.EnsureSprite(sr);
                sr.color = card.placeholderColor;
                sr.sortingOrder = 4;
            }
            else
            {
                Debug.LogWarning($"[PlacementManager] PlaceResourceToken: no SpriteRenderer on resourceTokenPrefab");
            }

            SpriteHelper.AddOutline(token, 4);

            tile.HasResource = true;
            tile.StoredResourceType = card.resourceType;
            tile.StoredResourceValue = card.value;
            Debug.Log($"[PlacementManager] PlaceResourceToken: stored resource data on tile — type={card.resourceType}, value={card.value}");
            return token;
        }

        public void UndoLastPlacement()
        {
            if (!placementEnabled || placementHistory.Count == 0)
            {
                Debug.Log($"[PlacementManager] UndoLastPlacement: nothing to undo (enabled={placementEnabled}, history={placementHistory.Count})");
                return;
            }

            var record = placementHistory.Pop();
            Debug.Log($"[PlacementManager] UndoLastPlacement: undoing '{record.card.cardName}' at ({record.gridPos}), history remaining={placementHistory.Count}");

            // Destroy the token
            if (record.token != null)
            {
                Debug.Log($"[PlacementManager] UndoLastPlacement: destroying token '{record.token.name}'");
                Destroy(record.token);
            }

            // Revert tile state
            Tile tile = boardManager.GetTile(record.gridPos);
            if (tile != null)
            {
                if (record.card.cardType == CardType.Hero)
                {
                    tile.HasHero = false;
                    Debug.Log($"[PlacementManager] UndoLastPlacement: tile ({record.gridPos}) HasHero=false");
                }
                else if (record.card.cardType == CardType.Resource)
                {
                    tile.HasResource = false;
                    tile.StoredResourceType = default;
                    tile.StoredResourceValue = 0;
                    Debug.Log($"[PlacementManager] UndoLastPlacement: tile ({record.gridPos}) HasResource=false, resource data cleared");
                }
            }

            // Return card to hand
            handManager.AddCardBackToHand(record.card);
            Debug.Log($"[PlacementManager] UndoLastPlacement: returned '{record.card.cardName}' to hand");
        }

        public int PlacementHistoryCount => placementHistory.Count;

        /// <summary>
        /// Returns all card definitions that were placed this turn (for returning to the discard pile).
        /// </summary>
        public List<CardDefinitionSO> GetPlacedCardDefinitions()
        {
            var cards = new List<CardDefinitionSO>();
            foreach (var record in placementHistory)
                cards.Add(record.card);
            Debug.Log($"[PlacementManager] GetPlacedCardDefinitions: returning {cards.Count} placed cards");
            return cards;
        }

        public void ClearTokens(bool keepHealthyHeroes = false)
        {
            int destroyed = 0;
            int kept = 0;
            var toDestroy = new List<GameObject>();

            foreach (Transform child in boardManager.transform)
            {
                if (child.name.StartsWith("Hero_"))
                {
                    if (keepHealthyHeroes)
                    {
                        var hero = child.GetComponent<Gathering.HeroAgent>();
                        if (hero != null && !hero.IsWounded)
                        {
                            hero.ResetForNextTurn();
                            kept++;
                            Debug.Log($"[PlacementManager] ClearTokens: keeping healthy hero '{child.name}' on board (reset for next turn)");
                            continue;
                        }
                    }
                    toDestroy.Add(child.gameObject);
                }
                else if (child.name.StartsWith("Resource_"))
                {
                    // Resource tokens persist until collected or removed via combat
                    kept++;
                    Debug.Log($"[PlacementManager] ClearTokens: keeping resource token '{child.name}'");
                    continue;
                }
            }

            foreach (var go in toDestroy)
            {
                Debug.Log($"[PlacementManager] ClearTokens: destroying '{go.name}'");
                Destroy(go);
                destroyed++;
            }
            Debug.Log($"[PlacementManager] ClearTokens: destroyed={destroyed}, kept={kept}");
        }

        /// <summary>
        /// Returns card definitions for heroes currently on the board (healthy, not wounded).
        /// These should NOT be discarded back to the deck between turns.
        /// </summary>
        public HashSet<CardDefinitionSO> GetActiveHeroCards()
        {
            var active = new HashSet<CardDefinitionSO>();
            foreach (Transform child in boardManager.transform)
            {
                if (child.name.StartsWith("Hero_"))
                {
                    var hero = child.GetComponent<Gathering.HeroAgent>();
                    if (hero != null && !hero.IsWounded)
                    {
                        active.Add(hero.CardData);
                        Debug.Log($"[PlacementManager] GetActiveHeroCards: hero='{hero.CardData.cardName}' is active on board");
                    }
                }
            }
            Debug.Log($"[PlacementManager] GetActiveHeroCards: {active.Count} active heroes on board");
            return active;
        }
    }
}
