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
        [SerializeField] private GameObject heroTokenPrefab;
        [SerializeField] private GameObject resourceTokenPrefab;

        private bool placementEnabled;

        private void Awake()
        {
            Debug.Log($"[PlacementManager] Awake: boardManager={boardManager?.name ?? "NULL"}, handManager={handManager?.name ?? "NULL"}, heroTokenPrefab={heroTokenPrefab?.name ?? "NULL"}, resourceTokenPrefab={resourceTokenPrefab?.name ?? "NULL"}");
        }

        private void OnEnable()
        {
            Debug.Log("[PlacementManager] OnEnable: subscribing to EventBus.OnPhaseChanged");
            EventBus.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            Debug.Log("[PlacementManager] OnDisable: unsubscribing from EventBus.OnPhaseChanged");
            EventBus.OnPhaseChanged -= OnPhaseChanged;
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
            Debug.Log($"[PlacementManager] OnPhaseChanged: phase={phase}, placementEnabled={placementEnabled}");
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

            if (card.cardType == CardType.Hero && tile.CanPlaceHero())
            {
                valid = true;
                PlaceHeroToken(card, tile);
                Debug.Log($"[PlacementManager] HandleCardDrop: HERO placed — card='{card.cardName}', tile=({tile.GridPosition})");
            }
            else if (card.cardType == CardType.Resource && tile.CanPlaceResource())
            {
                valid = true;
                PlaceResourceToken(card, tile);
                Debug.Log($"[PlacementManager] HandleCardDrop: RESOURCE placed — card='{card.cardName}', tile=({tile.GridPosition})");
            }

            if (valid)
            {
                handManager.RemoveCardFromHand(cardView);
                EventBus.OnCardPlaced?.Invoke(card, gridPos.Value);
                Debug.Log($"[PlacementManager] HandleCardDrop: card placed successfully, removed from hand, event fired");
            }
            else
            {
                Debug.Log($"[PlacementManager] HandleCardDrop: INVALID placement — card type={card.cardType}, tileType={tile.TileType} — snapping back");
                cardView.SnapBack();
            }
        }

        private void PlaceHeroToken(CardDefinitionSO card, Tile tile)
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

            var heroAgent = token.GetComponent<Gathering.HeroAgent>();
            if (heroAgent != null)
            {
                heroAgent.Initialize(card, tile.GridPosition);
                Debug.Log($"[PlacementManager] PlaceHeroToken: HeroAgent initialized at gridPos={tile.GridPosition}");
            }
            else
            {
                Debug.LogWarning($"[PlacementManager] PlaceHeroToken: no HeroAgent component on heroTokenPrefab");
            }

            tile.HasHero = true;
        }

        private void PlaceResourceToken(CardDefinitionSO card, Tile tile)
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

            tile.HasResource = true;
        }

        public void ClearTokens()
        {
            int count = 0;
            foreach (Transform child in boardManager.transform)
            {
                if (child.name.StartsWith("Hero_") || child.name.StartsWith("Resource_") || child.name.StartsWith("Enemy_"))
                {
                    Debug.Log($"[PlacementManager] ClearTokens: destroying '{child.name}'");
                    Destroy(child.gameObject);
                    count++;
                }
            }
            Debug.Log($"[PlacementManager] ClearTokens: destroyed {count} tokens");
        }
    }
}
