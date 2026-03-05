using UnityEngine;

namespace Scurry.Data
{
    [CreateAssetMenu(fileName = "NewBoardLayout", menuName = "Scurry/Board Layout")]
    public class BoardLayoutSO : ScriptableObject
    {
        public int rows = 4;
        public int cols = 4;
        public TileType[] tileLayout; // length = rows * cols, row-major

        [Header("Resource Node Configuration")]
        public ResourceType[] nodeResourceTypes; // parallel to tileLayout, only meaningful for ResourceNode entries
        public int[] nodeResourceValues;         // parallel to tileLayout, only meaningful for ResourceNode entries

        public TileType GetTileType(int row, int col)
        {
            int index = row * cols + col;
            if (tileLayout == null || index < 0 || index >= tileLayout.Length)
                return TileType.Normal;
            return tileLayout[index];
        }

        public ResourceType GetNodeResourceType(int row, int col)
        {
            int index = row * cols + col;
            if (nodeResourceTypes == null || index < 0 || index >= nodeResourceTypes.Length)
                return ResourceType.Food;
            return nodeResourceTypes[index];
        }

        public int GetNodeResourceValue(int row, int col)
        {
            int index = row * cols + col;
            if (nodeResourceValues == null || index < 0 || index >= nodeResourceValues.Length)
                return 1;
            return nodeResourceValues[index];
        }
    }
}