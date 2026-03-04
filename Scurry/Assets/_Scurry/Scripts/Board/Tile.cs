using UnityEngine;
using UnityEngine.EventSystems;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.Board
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Tile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TileType TileType { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public int EnemyStrength { get; private set; }
        public int HazardDamage { get; private set; }
        public bool HasHero { get; set; }
        public bool HasResource { get; set; }
        public bool IsEnemyDefeated { get; set; }

        private SpriteRenderer spriteRenderer;
        private Color baseColor;

        public void Initialize(Vector2Int gridPos, TileType type, Color color, int enemyStr, int hazardDmg)
        {
            GridPosition = gridPos;
            TileType = type;
            EnemyStrength = enemyStr;
            HazardDamage = hazardDmg;
            baseColor = color;

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                SpriteHelper.EnsureSprite(spriteRenderer);
                spriteRenderer.color = baseColor;
            }
            else
            {
                Debug.LogWarning($"[Tile] Initialize: no SpriteRenderer on tile at gridPos={gridPos}");
            }

            // Ensure collider is sized to match sprite
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size = Vector2.one; // 1x1 unit matches the sprite
                Debug.Log($"[Tile] Initialize: collider size={col.size}, enabled={col.enabled}");
            }
            else
            {
                Debug.LogWarning($"[Tile] Initialize: no BoxCollider2D on tile at gridPos={gridPos}");
            }

            gameObject.name = $"Tile_{gridPos.x}_{gridPos.y}";
            Debug.Log($"[Tile] Initialize: gridPos={gridPos}, type={type}, enemyStr={enemyStr}, hazardDmg={hazardDmg}, color={color}");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"[Tile] OnPointerEnter: gridPos={GridPosition}, type={TileType}");
            SetHighlight(true);
            EventBus.OnTileHovered?.Invoke(GetTooltipText());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log($"[Tile] OnPointerExit: gridPos={GridPosition}");
            SetHighlight(false);
            EventBus.OnTileUnhovered?.Invoke();
        }

        public void SetHighlight(bool on)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = on ? baseColor * 1.3f : baseColor;
        }

        public bool CanPlaceHero()
        {
            bool result = !HasHero && (TileType == TileType.Normal || TileType == TileType.ResourceNode);
            Debug.Log($"[Tile] CanPlaceHero: gridPos={GridPosition}, type={TileType}, hasHero={HasHero}, result={result}");
            return result;
        }

        public bool CanPlaceResource()
        {
            bool result = !HasResource && TileType == TileType.Normal;
            Debug.Log($"[Tile] CanPlaceResource: gridPos={GridPosition}, type={TileType}, hasResource={HasResource}, result={result}");
            return result;
        }

        public void ResetForNewTurn()
        {
            Debug.Log($"[Tile] ResetForNewTurn: gridPos={GridPosition}, hadHero={HasHero}, hadResource={HasResource}, wasEnemyDefeated={IsEnemyDefeated}");
            HasHero = false;
            HasResource = false;
            IsEnemyDefeated = false;
        }

        public string GetTooltipText()
        {
            string title = TileType switch
            {
                TileType.Normal => "Normal Tile",
                TileType.ResourceNode => "Resource Node",
                TileType.EnemyPatrol => "Enemy Patrol",
                TileType.Hazard => "Hazard",
                _ => "Unknown"
            };

            string desc = TileType switch
            {
                TileType.Normal => "Place heroes or resources here.",
                TileType.ResourceNode => "Place heroes here. Bonus gathering spot.",
                TileType.EnemyPatrol => $"Enemy (Str: {EnemyStrength}). Heroes must fight to pass.",
                TileType.Hazard => $"Impassable. Deals {HazardDamage} damage.",
                _ => ""
            };

            string status = "";
            if (HasHero) status += " [Hero]";
            if (HasResource) status += " [Resource]";
            if (TileType == TileType.EnemyPatrol && IsEnemyDefeated) status += " [Defeated]";

            return $"<b>{title}</b>\n{desc}{(status.Length > 0 ? "\n" + status.Trim() : "")}";
        }
    }
}
