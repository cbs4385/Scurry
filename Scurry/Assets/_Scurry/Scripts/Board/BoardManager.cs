using UnityEngine;
using Scurry.Data;

namespace Scurry.Board
{
    public class BoardManager : MonoBehaviour
    {
        [SerializeField] private BoardLayoutSO boardLayout;
        [SerializeField] private GameObject tilePrefab;
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
    }
}
