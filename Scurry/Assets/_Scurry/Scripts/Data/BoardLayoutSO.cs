using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewBoardLayout", menuName = "Scurry/Board Layout")]
    public class BoardLayoutSO : ScriptableObject
    {
        public int rows = 4;
        public int cols = 4;
        public TileType[] tileLayout; // length = rows * cols, row-major

        public TileType GetTileType(int row, int col)
        {
            int index = row * cols + col;
            if (tileLayout == null || index < 0 || index >= tileLayout.Length)
                return TileType.Normal;
            return tileLayout[index];
        }
    }
}