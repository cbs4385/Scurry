using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Map
{
    [System.Serializable]
    public class MapNode
    {
        public NodeType nodeType;
        public Vector2Int position; // (row, column)
        public List<int> connectedNodeIndices = new List<int>(); // indices in next row
        public bool visited;
        public bool available;
        public EncounterDefinitionSO encounterDefinition;
        public int difficulty;

        public override string ToString()
        {
            return $"MapNode({nodeType}, row={position.x}, col={position.y}, visited={visited}, difficulty={difficulty})";
        }
    }
}
