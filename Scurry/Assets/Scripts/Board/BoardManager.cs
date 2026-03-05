using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;

namespace Scurry.Board
{
    public class BoardManager : MonoBehaviour
    {
        [SerializeField] private BoardLayoutSO boardLayout;
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private GameObject resourceTokenPrefab;
        [SerializeField] private float tileSize = 1.1f;

        [Header("Tile Colors")]
        [SerializeField] private Color normalColor = new Color(0.4f, 0.7f, 0.4f);
        [SerializeField] private Color resourceNodeColor = new Color(0.9f, 0.85f, 0.3f);
        [SerializeField] private Color enemyPatrolColor = new Color(0.85f, 0.3f, 0.3f);
        [SerializeField] private Color hazardColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("Enemy/Hazard Settings")]
        [SerializeField] private int defaultEnemyStrength = 2;
        [SerializeField] private int defaultHazardDamage = 2;

        public Tile[,] Grid { get; private set; }
        public int Rows => boardLayout.rows;
        public int Cols => boardLayout.cols;

        private Vector3 boardOrigin;
        private ResourceType[,] nodeResourceTypes;
        private int[,] nodeResourceValues;

        private void Awake()
        {
            Debug.Log($"[BoardManager] Awake: boardLayout={boardLayout?.name ?? "NULL"}, tilePrefab={tilePrefab?.name ?? "NULL"}, tileSize={tileSize}");
            BuildBoard();
        }

        private void BuildBoard()
        {
            int rows = boardLayout.rows;
            int cols = boardLayout.cols;
            Grid = new Tile[rows, cols];
            nodeResourceTypes = new ResourceType[rows, cols];
            nodeResourceValues = new int[rows, cols];
            Debug.Log($"[BoardManager] BuildBoard: creating {rows}x{cols} grid");

            // Center the board
            boardOrigin = new Vector3(
                -(cols - 1) * tileSize * 0.5f,
                -(rows - 1) * tileSize * 0.5f + 1f, // offset up slightly for hand space
                0f
            );
            Debug.Log($"[BoardManager] BuildBoard: boardOrigin={boardOrigin}");

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    TileType type = boardLayout.GetTileType(r, c);
                    Vector3 worldPos = boardOrigin + new Vector3(c * tileSize, r * tileSize, 0f);

                    GameObject tileGO = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                    Tile tile = tileGO.GetComponent<Tile>();

                    if (tile == null)
                    {
                        Debug.LogError($"[BoardManager] BuildBoard: tilePrefab has no Tile component! row={r}, col={c}");
                        continue;
                    }

                    Color color = GetColorForType(type);
                    int enemyStr = type == TileType.EnemyPatrol ? defaultEnemyStrength : 0;
                    int hazardDmg = type == TileType.Hazard ? defaultHazardDamage : 0;

                    tile.Initialize(new Vector2Int(r, c), type, color, enemyStr, hazardDmg);
                    Grid[r, c] = tile;

                    if (type == TileType.ResourceNode)
                    {
                        nodeResourceTypes[r, c] = boardLayout.GetNodeResourceType(r, c);
                        nodeResourceValues[r, c] = boardLayout.GetNodeResourceValue(r, c);
                        Debug.Log($"[BoardManager] BuildBoard: ResourceNode at ({r},{c}) configured — type={nodeResourceTypes[r, c]}, value={nodeResourceValues[r, c]}");
                    }
                }
            }
            Debug.Log($"[BoardManager] BuildBoard: complete — {rows * cols} tiles created");
        }

        private Color GetColorForType(TileType type)
        {
            return type switch
            {
                TileType.Normal => normalColor,
                TileType.ResourceNode => resourceNodeColor,
                TileType.EnemyPatrol => enemyPatrolColor,
                TileType.Hazard => hazardColor,
                _ => Color.white
            };
        }

        public Tile GetTile(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= Rows || pos.y < 0 || pos.y >= Cols)
            {
                Debug.LogWarning($"[BoardManager] GetTile: out of bounds pos={pos}, gridSize={Rows}x{Cols}");
                return null;
            }
            return Grid[pos.x, pos.y];
        }

        public Vector3 GetWorldPosition(Vector2Int gridPos)
        {
            Vector3 result = boardOrigin + new Vector3(gridPos.y * tileSize, gridPos.x * tileSize, 0f);
            return result;
        }

        public Vector2Int? GetGridPosition(Vector3 worldPos)
        {
            Vector3 local = worldPos - boardOrigin;
            int col = Mathf.RoundToInt(local.x / tileSize);
            int row = Mathf.RoundToInt(local.y / tileSize);

            Debug.Log($"[BoardManager] GetGridPosition: worldPos={worldPos}, local={local}, calculated row={row}, col={col}, bounds={Rows}x{Cols}");

            if (row < 0 || row >= Rows || col < 0 || col >= Cols)
            {
                Debug.Log($"[BoardManager] GetGridPosition: out of bounds — returning null");
                return null;
            }
            return new Vector2Int(row, col);
        }

        public void ResetAllTiles()
        {
            Debug.Log($"[BoardManager] ResetAllTiles: resetting {Rows}x{Cols} grid");
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    Grid[r, c].ResetForNewTurn();
            Debug.Log("[BoardManager] ResetAllTiles: complete");
        }

        public void GenerateResourceNodeResources()
        {
            Debug.Log("[BoardManager] GenerateResourceNodeResources: scanning for empty ResourceNode tiles");
            int generated = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    Tile tile = Grid[r, c];
                    if (tile.TileType == TileType.ResourceNode && !tile.HasResource)
                    {
                        tile.HasResource = true;
                        tile.StoredResourceType = nodeResourceTypes[r, c];
                        tile.StoredResourceValue = nodeResourceValues[r, c];
                        generated++;
                        Debug.Log($"[BoardManager] GenerateResourceNodeResources: ({r},{c}) auto-generated {tile.StoredResourceType} (value={tile.StoredResourceValue})");

                        // Spawn visible token
                        if (resourceTokenPrefab != null)
                        {
                            Vector3 worldPos = GetWorldPosition(new Vector2Int(r, c));
                            worldPos.z = -0.05f;
                            GameObject token = Instantiate(resourceTokenPrefab, worldPos, Quaternion.identity, transform);
                            token.name = $"Resource_Auto_{r}_{c}";
                            var sr = token.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                SpriteHelper.EnsureSprite(sr);
                                sr.color = new Color(0.9f, 0.85f, 0.3f);
                                sr.sortingOrder = 4;
                            }
                            SpriteHelper.AddOutline(token, 4);
                            Debug.Log($"[BoardManager] GenerateResourceNodeResources: spawned token '{token.name}' at worldPos={worldPos}");
                        }
                        else
                        {
                            Debug.LogWarning("[BoardManager] GenerateResourceNodeResources: resourceTokenPrefab is null — no visible token spawned");
                        }
                    }
                }
            }
            Debug.Log($"[BoardManager] GenerateResourceNodeResources: complete — generated {generated} resources");
        }

        public bool HasAnyResources()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (Grid[r, c].HasResource)
                    {
                        Debug.Log($"[BoardManager] HasAnyResources: found resource at ({r},{c})");
                        return true;
                    }
            Debug.Log("[BoardManager] HasAnyResources: no resources remaining on board");
            return false;
        }

        public bool HasAnyEnemies()
        {
            var enemies = GetComponentsInChildren<Gathering.EnemyAgent>();
            int alive = 0;
            foreach (var e in enemies)
                if (!e.IsDefeated) alive++;
            Debug.Log($"[BoardManager] HasAnyEnemies: total={enemies.Length}, alive={alive}");
            return alive > 0;
        }

        /// <summary>
        /// Collects all remaining resources on the board, firing events for each.
        /// Used when all enemies are defeated and the player auto-wins.
        /// </summary>
        public void CollectAllRemainingResources()
        {
            Debug.Log("[BoardManager] CollectAllRemainingResources: auto-collecting all resources");
            int collected = 0;
            var tokensToDestroy = new List<GameObject>();

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    Tile tile = Grid[r, c];
                    if (tile.HasResource)
                    {
                        Debug.Log($"[BoardManager] CollectAllRemainingResources: collecting {tile.StoredResourceType} (value={tile.StoredResourceValue}) at ({r},{c})");
                        EventBus.OnResourceCollected?.Invoke(tile.StoredResourceType, tile.StoredResourceValue);

                        // Find and mark resource token for destruction
                        foreach (Transform child in transform)
                        {
                            if (child.name.StartsWith("Resource_"))
                            {
                                Vector2Int? childGrid = GetGridPosition(child.position);
                                if (childGrid.HasValue && childGrid.Value == new Vector2Int(r, c))
                                {
                                    EventBus.OnResourceTokenCollected?.Invoke(child.name);
                                    tokensToDestroy.Add(child.gameObject);
                                    break;
                                }
                            }
                        }

                        tile.HasResource = false;
                        tile.StoredResourceType = default;
                        tile.StoredResourceValue = 0;
                        collected++;
                    }
                }
            }

            foreach (var token in tokensToDestroy)
            {
                Debug.Log($"[BoardManager] CollectAllRemainingResources: destroying token '{token.name}'");
                Destroy(token);
            }

            Debug.Log($"[BoardManager] CollectAllRemainingResources: complete — collected {collected} resources");
        }

        public void UpdateTilesForEnemyMovement()
        {
            Debug.Log("[BoardManager] UpdateTilesForEnemyMovement: scanning for tile transitions");

            // Build set of tiles occupied by living enemies
            var enemies = GetComponentsInChildren<Gathering.EnemyAgent>();
            var enemyPositions = new Dictionary<Vector2Int, int>();
            foreach (var enemy in enemies)
            {
                if (!enemy.IsDefeated)
                {
                    enemyPositions[enemy.GridPosition] = enemy.Strength;
                    Debug.Log($"[BoardManager] UpdateTilesForEnemyMovement: live enemy at {enemy.GridPosition} (str={enemy.Strength})");
                }
            }

            int cleared = 0;
            int occupied = 0;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    Tile tile = Grid[r, c];
                    Vector2Int pos = new Vector2Int(r, c);

                    if (tile.TileType == TileType.EnemyPatrol && !enemyPositions.ContainsKey(pos))
                    {
                        // Red tile with no enemy → always become Normal (green)
                        tile.SetAsNormal(normalColor);
                        cleared++;
                    }
                    else if (tile.TileType != TileType.EnemyPatrol && enemyPositions.ContainsKey(pos))
                    {
                        // Non-red tile with enemy → become red
                        tile.SetAsEnemyOccupied(enemyPositions[pos], enemyPatrolColor);
                        occupied++;
                    }
                    else if (tile.TileType == TileType.EnemyPatrol && enemyPositions.ContainsKey(pos))
                    {
                        // Enemy still on its patrol tile — refresh strength via transition
                        tile.SetAsEnemyOccupied(enemyPositions[pos], enemyPatrolColor);
                    }
                }
            }

            Debug.Log($"[BoardManager] UpdateTilesForEnemyMovement: complete — cleared={cleared}, newEnemyTiles={occupied}, liveEnemies={enemyPositions.Count}");
        }

        public List<Tile> GetAdjacentTiles(Vector2Int pos)
        {
            var result = new List<Tile>();
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in dirs)
            {
                Vector2Int neighbor = pos + dir;
                if (neighbor.x >= 0 && neighbor.x < Rows && neighbor.y >= 0 && neighbor.y < Cols)
                    result.Add(Grid[neighbor.x, neighbor.y]);
            }
            Debug.Log($"[BoardManager] GetAdjacentTiles: pos={pos}, found {result.Count} adjacent tiles");
            return result;
        }
    }
}
