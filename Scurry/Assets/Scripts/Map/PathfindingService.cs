using System.Collections.Generic;
using UnityEngine;
using Scurry.Core;

namespace Scurry.Map
{
    public static class PathfindingService
    {
        /// <summary>
        /// BFS shortest path from fromId to toId. Returns list of node IDs from start to end (inclusive).
        /// Returns empty list if no path exists.
        /// </summary>
        public static List<int> FindPath(MapGraph graph, int fromId, int toId)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[PathfindingService] FindPath: starting BFS (fromId={fromId}, toId={toId})");

            if (graph.GetNode(fromId) == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[PathfindingService] FindPath: fromId not found in graph (fromId={fromId})");
                return new List<int>();
            }
            if (graph.GetNode(toId) == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[PathfindingService] FindPath: toId not found in graph (toId={toId})");
                return new List<int>();
            }

            if (fromId == toId)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[PathfindingService] FindPath: same node (nodeId={fromId}), returning single-element path");
                return new List<int> { fromId };
            }

            var visited = new HashSet<int>();
            var prev = new Dictionary<int, int>();
            var queue = new Queue<int>();

            queue.Enqueue(fromId);
            visited.Add(fromId);
            prev[fromId] = -1;

            bool found = false;

            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                MapNode current = graph.GetNode(currentId);

                if (current == null)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[PathfindingService] FindPath: encountered null node during BFS (nodeId={currentId})");
                    continue;
                }

                foreach (int neighborId in current.neighborIds)
                {
                    if (visited.Contains(neighborId))
                        continue;

                    visited.Add(neighborId);
                    prev[neighborId] = currentId;
                    queue.Enqueue(neighborId);

                    if (neighborId == toId)
                    {
                        found = true;
                        break;
                    }
                }

                if (found) break;
            }

            if (!found)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[PathfindingService] FindPath: no path exists (fromId={fromId}, toId={toId})");
                return new List<int>();
            }

            // Reconstruct path
            var path = new List<int>();
            int step = toId;
            while (step != -1)
            {
                path.Add(step);
                step = prev[step];
            }
            path.Reverse();

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[PathfindingService] FindPath: path found (fromId={fromId}, toId={toId}, length={path.Count}, edges={path.Count - 1}, path=[{string.Join(",", path)}])");
            return path;
        }

        /// <summary>
        /// Returns edge count between two nodes, or -1 if no path exists.
        /// </summary>
        public static int GetDistance(MapGraph graph, int fromId, int toId)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[PathfindingService] GetDistance: (fromId={fromId}, toId={toId})");

            var path = FindPath(graph, fromId, toId);
            if (path.Count == 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[PathfindingService] GetDistance: no path (fromId={fromId}, toId={toId}, result=-1)");
                return -1;
            }

            int distance = path.Count - 1;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[PathfindingService] GetDistance: (fromId={fromId}, toId={toId}, distance={distance})");
            return distance;
        }

        /// <summary>
        /// Returns all node IDs reachable within N edges from the given start node (inclusive of start).
        /// </summary>
        public static HashSet<int> GetNodesWithinRange(MapGraph graph, int fromId, int range)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[PathfindingService] GetNodesWithinRange: (fromId={fromId}, range={range})");

            var result = new HashSet<int>();

            if (graph.GetNode(fromId) == null)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[PathfindingService] GetNodesWithinRange: fromId not found (fromId={fromId})");
                return result;
            }

            if (range < 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[PathfindingService] GetNodesWithinRange: negative range (fromId={fromId}, range={range})");
                return result;
            }

            // BFS with depth tracking
            var visited = new HashSet<int>();
            var queue = new Queue<(int nodeId, int depth)>();

            queue.Enqueue((fromId, 0));
            visited.Add(fromId);
            result.Add(fromId);

            while (queue.Count > 0)
            {
                var (currentId, depth) = queue.Dequeue();

                if (depth >= range)
                    continue;

                MapNode current = graph.GetNode(currentId);
                if (current == null) continue;

                foreach (int neighborId in current.neighborIds)
                {
                    if (visited.Contains(neighborId))
                        continue;

                    visited.Add(neighborId);
                    result.Add(neighborId);
                    queue.Enqueue((neighborId, depth + 1));
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[PathfindingService] GetNodesWithinRange: (fromId={fromId}, range={range}, nodesFound={result.Count})");
            return result;
        }
    }
}
