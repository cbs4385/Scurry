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
        public TileType OriginalTileType { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public int EnemyStrength { get; private set; }
        public int HazardDamage { get; private set; }
        public bool HasHero { get; set; }
        public bool HasResource { get; set; }
        public ResourceType StoredResourceType { get; set; }
        public int StoredResourceValue { get; set; }
        public bool IsEnemyDefeated { get; set; }

        private SpriteRenderer spriteRenderer;
        private Color baseColor;
        private Color originalColor;
        private int originalEnemyStrength;
        private int originalHazardDamage;

        public void Initialize(Vector2Int gridPos, TileType type, Color color, int enemyStr, int hazardDmg)
        {
            GridPosition = gridPos;
            TileType = type;
            OriginalTileType = type;
            EnemyStrength = enemyStr;
            HazardDamage = hazardDmg;
            originalEnemyStrength = enemyStr;
            originalHazardDamage = hazardDmg;
            baseColor = color;
            originalColor = color;

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
            bool result = !HasResource && (TileType == TileType.Normal || TileType == TileType.ResourceNode);
            Debug.Log($"[Tile] CanPlaceResource: gridPos={GridPosition}, type={TileType}, hasResource={HasResource}, result={result}");
            return result;
        }

        public void SetAsNormal(Color normalTileColor)
        {
            Debug.Log($"[Tile] SetAsNormal: gridPos={GridPosition}, {TileType} -> Normal");
            TileType = TileType.Normal;
            EnemyStrength = 0;
            IsEnemyDefeated = false;
            baseColor = normalTileColor;
            if (spriteRenderer != null)
                spriteRenderer.color = baseColor;
        }

        public void SetAsEnemyOccupied(int enemyStr, Color enemyColor)
        {
            Debug.Log($"[Tile] SetAsEnemyOccupied: gridPos={GridPosition}, originalType={OriginalTileType} -> EnemyPatrol, enemyStr={enemyStr}");
            TileType = TileType.EnemyPatrol;
            EnemyStrength = enemyStr;
            IsEnemyDefeated = false;
            baseColor = enemyColor;
            if (spriteRenderer != null)
                spriteRenderer.color = baseColor;
        }

        public void RestoreOriginalType()
        {
            Debug.Log($"[Tile] RestoreOriginalType: gridPos={GridPosition}, {TileType} -> {OriginalTileType}, enemyStr={originalEnemyStrength}, hazardDmg={originalHazardDamage}");
            TileType = OriginalTileType;
            EnemyStrength = originalEnemyStrength;
            HazardDamage = originalHazardDamage;
            IsEnemyDefeated = false;
            baseColor = originalColor;
            if (spriteRenderer != null)
                spriteRenderer.color = baseColor;
        }

        public void ResetForNewTurn()
        {
            Debug.Log($"[Tile] ResetForNewTurn: gridPos={GridPosition}, hadHero={HasHero}, hadResource={HasResource}, storedType={StoredResourceType}, storedValue={StoredResourceValue}, wasEnemyDefeated={IsEnemyDefeated}");
            HasHero = false;

            // Resources persist until collected or removed via combat — do not clear
            if (HasResource)
            {
                Debug.Log($"[Tile] ResetForNewTurn: preserving resource at ({GridPosition}) — type={StoredResourceType}, value={StoredResourceValue}");
            }
            // IsEnemyDefeated managed by tile transitions in BoardManager.UpdateTilesForEnemyMovement()
        }

        public string GetTooltipText()
        {
            string tileKey = TileType switch
            {
                TileType.Normal => "normal",
                TileType.ResourceNode => "resourcenode",
                TileType.EnemyPatrol => "enemypatrol",
                TileType.Hazard => "hazard",
                _ => "unknown"
            };

            string title = Loc.Get($"tile.{tileKey}.title");

            string desc = TileType switch
            {
                TileType.EnemyPatrol => Loc.Format("tile.enemypatrol.desc", EnemyStrength),
                TileType.Hazard => Loc.Format("tile.hazard.desc", HazardDamage),
                _ => Loc.Get($"tile.{tileKey}.desc")
            };

            string status = "";
            if (HasHero) status += " " + Loc.Get("tile.status.hero");
            if (HasResource) status += " " + Loc.Format("tile.status.resource", StoredResourceType, StoredResourceValue);
            // IsEnemyDefeated tooltip no longer needed — defeated enemy tiles revert to original type

            return $"<b>{title}</b>\n{desc}{(status.Length > 0 ? "\n" + status.Trim() : "")}";
        }
    }
}
