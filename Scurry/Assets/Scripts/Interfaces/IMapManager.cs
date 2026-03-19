using System.Collections.Generic;
using Scurry.Map;

namespace Scurry.Interfaces
{
    /// <summary>
    /// v2.0 map manager interface. Provides access to the persistent 30-node graph map
    /// with fog of war, pathfinding, and node queries.
    /// </summary>
    public interface IMapManager
    {
        /// <summary>The underlying map graph containing all nodes and edges.</summary>
        MapGraph Graph { get; }

        /// <summary>
        /// Generates a new map from a seed. Creates 30 nodes across 3 zones
        /// (Wilderness, Farmland, Town) plus Colony and Pied Piper nodes.
        /// </summary>
        /// <param name="seed">Random seed for deterministic map generation.</param>
        void GenerateMap(int seed);

        /// <summary>Retrieves a map node by its unique ID.</summary>
        /// <param name="nodeId">The unique identifier of the node.</param>
        /// <returns>The MapNode, or null if not found.</returns>
        MapNode GetNode(int nodeId);

        /// <summary>
        /// Finds the shortest path between two nodes using the graph edges.
        /// </summary>
        /// <param name="fromId">Starting node ID.</param>
        /// <param name="toId">Destination node ID.</param>
        /// <returns>Ordered list of node IDs from start to destination (inclusive), or empty if no path.</returns>
        List<int> GetPath(int fromId, int toId);

        // --- Legacy v1.0 members ---
        List<List<MapNode>> Map { get; }
        List<MapNode> GetAvailableNodes();
        void SelectNode(MapNode node);
    }
}
