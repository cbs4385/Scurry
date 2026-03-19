using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Map
{
    // Forward reference for colony card placement on map nodes
    using ColonyCardDef = Scurry.Data.ColonyCardDefinitionSO;
    public enum FogState { Hidden, Remembered, Visible }

    [System.Serializable]
    public class MapNode
    {
        public int nodeId;
        public NodeType zone; // Wilderness, Farmland, Town, Colony, PiedPiper
        public List<int> neighborIds = new List<int>();
        public Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
        public List<int> enemyTokenIds = new List<int>();
        public List<int> heroTokenIds = new List<int>();
        public bool visited;
        public FogState fogState = FogState.Hidden;
        public Vector2 worldPosition;
        public string displayName;

        // ── Colony card placement ──────────────────────────────────────
        /// <summary>Colony card placed on this node (null if empty or non-colony node).</summary>
        [System.NonSerialized] public ColonyCardDef placedColonyCard;
        /// <summary>Whether this node is part of the colony sub-network.</summary>
        public bool IsColonyNode => zone == NodeType.Colony;
        /// <summary>Whether this colony node has a card placed on it.</summary>
        public bool HasColonyCard => placedColonyCard != null;

        // ── v1.0 compatibility properties ────────────────────────────────
        /// <summary>v1.0 compat: whether this node is currently selectable.</summary>
        [System.NonSerialized] public bool available;
        /// <summary>v1.0 compat: alias for neighborIds (formerly indices into next row).</summary>
        public List<int> connectedNodeIndices => neighborIds;
        /// <summary>v1.0 compat: grid position. x=row, y=column (approximated from worldPosition).</summary>
        public Vector2Int position => new Vector2Int(Mathf.RoundToInt(worldPosition.y), Mathf.RoundToInt(worldPosition.x));
        /// <summary>v1.0 compat: alias for zone.</summary>
        public NodeType nodeType => zone;
        /// <summary>v1.0 compat: difficulty derived from zone.</summary>
        public int difficulty => zone switch
        {
            NodeType.Wilderness => 1,
            NodeType.Farmland => 2,
            NodeType.Town => 3,
            NodeType.PiedPiper => 4,
            _ => 0
        };

        public override string ToString()
        {
            return $"MapNode(id={nodeId}, zone={zone}, pos={worldPosition}, neighbors={neighborIds.Count}, visited={visited}, fog={fogState})";
        }

        /// <summary>
        /// Returns total resource count across all types on this node.
        /// </summary>
        public int TotalResources()
        {
            int total = 0;
            foreach (var kvp in resources)
                total += kvp.Value;
            return total;
        }
    }
}
