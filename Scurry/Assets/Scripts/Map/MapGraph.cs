using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scurry.Data;
using Scurry.Interfaces;
using Scurry.Core;

namespace Scurry.Map
{
    public class MapGraph : IMapGraph
    {
        private Dictionary<int, MapNode> nodes = new Dictionary<int, MapNode>();

        public int ColonyNodeId { get; private set; } = -1;
        public int PiedPiperNodeId { get; private set; } = -1;

        /// <summary>
        /// Number of zone bosses defeated so far. Updated by TurnManager/GameSimulator.
        /// Used to gate access to the Pied Piper node.
        /// </summary>
        public int ZoneBossesDefeated { get; set; }

        /// <summary>
        /// Number of zone bosses that must be defeated before the Pied Piper node is accessible.
        /// </summary>
        public int BossesRequiredForPiper { get; set; } = 2;

        /// <summary>
        /// Returns true if the Pied Piper node is currently accessible (enough bosses defeated).
        /// </summary>
        public bool IsPiperAccessible => ZoneBossesDefeated >= BossesRequiredForPiper;

        public MapGraph()
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log("[MapGraph] Constructor: creating empty graph");
        }

        /// <summary>
        /// Adds a node to the graph. Sets ColonyNodeId/PiedPiperNodeId if zone matches.
        /// </summary>
        public void AddNode(MapNode node)
        {
            if (node == null)
            {
                Debug.LogError("[MapGraph] AddNode: node is null, skipping");
                return;
            }

            if (nodes.ContainsKey(node.nodeId))
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[MapGraph] AddNode: node already exists (nodeId={node.nodeId}), overwriting");
            }

            nodes[node.nodeId] = node;

            if (node.zone == NodeType.Colony && ColonyNodeId < 0)
            {
                // First colony node added becomes the entrance (hero deployment point)
                ColonyNodeId = node.nodeId;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] AddNode: set ColonyNodeId (entrance)={node.nodeId}");
            }
            else if (node.zone == NodeType.PiedPiper)
            {
                PiedPiperNodeId = node.nodeId;
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] AddNode: set PiedPiperNodeId={node.nodeId}");
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] AddNode: added node (nodeId={node.nodeId}, zone={node.zone}, pos={node.worldPosition}, totalNodes={nodes.Count})");
        }

        /// <summary>
        /// Gets a node by ID. Returns null if not found.
        /// </summary>
        public MapNode GetNode(int nodeId)
        {
            if (nodes.TryGetValue(nodeId, out MapNode node))
            {
                return node;
            }

            if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[MapGraph] GetNode: node not found (nodeId={nodeId})");
            return null;
        }

        /// <summary>
        /// Removes a node and all edges referencing it.
        /// </summary>
        public bool RemoveNode(int nodeId)
        {
            if (!nodes.ContainsKey(nodeId))
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[MapGraph] RemoveNode: node not found (nodeId={nodeId})");
                return false;
            }

            MapNode removed = nodes[nodeId];

            // Remove all edges referencing this node from neighbors
            foreach (int neighborId in removed.neighborIds)
            {
                if (nodes.TryGetValue(neighborId, out MapNode neighbor))
                {
                    neighbor.neighborIds.Remove(nodeId);
                }
            }

            nodes.Remove(nodeId);

            if (ColonyNodeId == nodeId) ColonyNodeId = -1;
            if (PiedPiperNodeId == nodeId) PiedPiperNodeId = -1;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] RemoveNode: removed node (nodeId={nodeId}, zone={removed.zone}, totalNodes={nodes.Count})");
            return true;
        }

        /// <summary>
        /// Adds a bidirectional edge between two nodes.
        /// </summary>
        public void AddEdge(int nodeA, int nodeB)
        {
            if (!nodes.ContainsKey(nodeA))
            {
                Debug.LogError($"[MapGraph] AddEdge: nodeA not found (nodeA={nodeA}, nodeB={nodeB})");
                return;
            }
            if (!nodes.ContainsKey(nodeB))
            {
                Debug.LogError($"[MapGraph] AddEdge: nodeB not found (nodeA={nodeA}, nodeB={nodeB})");
                return;
            }
            if (nodeA == nodeB)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[MapGraph] AddEdge: self-loop ignored (nodeId={nodeA})");
                return;
            }

            MapNode a = nodes[nodeA];
            MapNode b = nodes[nodeB];

            bool added = false;
            if (!a.neighborIds.Contains(nodeB))
            {
                a.neighborIds.Add(nodeB);
                added = true;
            }
            if (!b.neighborIds.Contains(nodeA))
            {
                b.neighborIds.Add(nodeA);
                added = true;
            }

            if (added)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] AddEdge: edge added (nodeA={nodeA}, nodeB={nodeB})");
            }
            else
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] AddEdge: edge already exists (nodeA={nodeA}, nodeB={nodeB})");
            }
        }

        /// <summary>
        /// Checks if an edge exists between two nodes.
        /// </summary>
        public bool HasEdge(int nodeA, int nodeB)
        {
            if (!nodes.ContainsKey(nodeA)) return false;
            return nodes[nodeA].neighborIds.Contains(nodeB);
        }

        /// <summary>
        /// Gets all neighbor MapNode objects for a given node.
        /// </summary>
        public List<MapNode> GetNeighbors(int nodeId)
        {
            var result = new List<MapNode>();

            if (!nodes.TryGetValue(nodeId, out MapNode node))
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[MapGraph] GetNeighbors: node not found (nodeId={nodeId})");
                return result;
            }

            foreach (int neighborId in node.neighborIds)
            {
                // Gate Pied Piper node behind boss defeats
                if (neighborId == PiedPiperNodeId && !IsPiperAccessible)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] GetNeighbors: Pied Piper node blocked (bossesDefeated={ZoneBossesDefeated}, required={BossesRequiredForPiper})");
                    continue;
                }

                if (nodes.TryGetValue(neighborId, out MapNode neighbor))
                {
                    result.Add(neighbor);
                }
                else
                {
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[MapGraph] GetNeighbors: neighbor not found (nodeId={nodeId}, neighborId={neighborId})");
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] GetNeighbors: (nodeId={nodeId}, neighborCount={result.Count})");
            return result;
        }

        /// <summary>
        /// Gets all nodes in a specific zone.
        /// </summary>
        public List<MapNode> GetNodesInZone(NodeType zone)
        {
            var result = new List<MapNode>();
            foreach (var kvp in nodes)
            {
                if (kvp.Value.zone == zone)
                    result.Add(kvp.Value);
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] GetNodesInZone: (zone={zone}, count={result.Count})");
            return result;
        }

        /// <summary>
        /// Returns all colony sub-network nodes.
        /// </summary>
        public List<MapNode> GetColonyNodes()
        {
            return GetNodesInZone(NodeType.Colony);
        }

        /// <summary>
        /// Returns colony nodes that have no colony card placed on them.
        /// </summary>
        public List<MapNode> GetEmptyColonyNodes()
        {
            var result = new List<MapNode>();
            foreach (var kvp in nodes)
            {
                if (kvp.Value.IsColonyNode && !kvp.Value.HasColonyCard)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// Returns colony nodes that have a colony card placed on them.
        /// </summary>
        public List<MapNode> GetOccupiedColonyNodes()
        {
            var result = new List<MapNode>();
            foreach (var kvp in nodes)
            {
                if (kvp.Value.IsColonyNode && kvp.Value.HasColonyCard)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// Returns all nodes in the graph.
        /// </summary>
        public IReadOnlyCollection<MapNode> GetAllNodes()
        {
            // No log — called frequently by HUD/renderer updates
            return nodes.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Dijkstra shortest path. Returns list of node IDs from start to end (inclusive).
        /// Returns empty list if no path exists.
        /// </summary>
        public List<int> ShortestPath(int fromId, int toId)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] ShortestPath: finding path (fromId={fromId}, toId={toId})");

            if (!nodes.ContainsKey(fromId) || !nodes.ContainsKey(toId))
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[MapGraph] ShortestPath: invalid node ID (fromId={fromId}, toId={toId})");
                return new List<int>();
            }

            if (fromId == toId)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] ShortestPath: same node (nodeId={fromId}), returning single-element path");
                return new List<int> { fromId };
            }

            // Dijkstra with uniform weights (equivalent to BFS, but keeping as Dijkstra per spec)
            var dist = new Dictionary<int, int>();
            var prev = new Dictionary<int, int>();
            var visited = new HashSet<int>();
            // Priority queue simulated with sorted set (node count is small, ~51 nodes)
            var unvisited = new SortedSet<(int distance, int nodeId)>();

            foreach (var kvp in nodes)
            {
                dist[kvp.Key] = int.MaxValue;
                prev[kvp.Key] = -1;
            }

            dist[fromId] = 0;
            unvisited.Add((0, fromId));

            while (unvisited.Count > 0)
            {
                var (currentDist, currentId) = unvisited.Min;
                unvisited.Remove(unvisited.Min);

                if (visited.Contains(currentId))
                    continue;

                visited.Add(currentId);

                if (currentId == toId)
                    break;

                MapNode currentNode = nodes[currentId];
                foreach (int neighborId in currentNode.neighborIds)
                {
                    if (visited.Contains(neighborId))
                        continue;

                    // Gate Pied Piper node behind boss defeats
                    if (neighborId == PiedPiperNodeId && !IsPiperAccessible)
                        continue;

                    int newDist = currentDist + 1;
                    if (newDist < dist[neighborId])
                    {
                        dist[neighborId] = newDist;
                        prev[neighborId] = currentId;
                        unvisited.Add((newDist, neighborId));
                    }
                }
            }

            // Reconstruct path
            if (dist[toId] == int.MaxValue)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[MapGraph] ShortestPath: no path exists (fromId={fromId}, toId={toId})");
                return new List<int>();
            }

            var path = new List<int>();
            int step = toId;
            while (step != -1)
            {
                path.Add(step);
                step = prev[step];
            }
            path.Reverse();

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] ShortestPath: path found (fromId={fromId}, toId={toId}, length={path.Count}, path=[{string.Join(",", path)}])");
            return path;
        }

        /// <summary>
        /// Returns edge count between two nodes, or -1 if no path exists.
        /// </summary>
        public int GetDistance(int fromId, int toId)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] GetDistance: (fromId={fromId}, toId={toId})");

            var path = ShortestPath(fromId, toId);
            if (path.Count == 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] GetDistance: no path (fromId={fromId}, toId={toId}, result=-1)");
                return -1;
            }

            int distance = path.Count - 1; // edges = nodes - 1
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGraph] GetDistance: (fromId={fromId}, toId={toId}, distance={distance})");
            return distance;
        }
    }
}
