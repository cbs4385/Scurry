using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;

namespace Scurry.Map
{
    public static class MapGenerator
    {
        public static List<List<MapNode>> GenerateMap(MapConfigSO config)
        {
            Debug.Log($"[MapGenerator] GenerateMap: level={config.levelNumber}, rows={config.numRows}, nodes/row={config.minNodesPerRow}-{config.maxNodesPerRow}");

            var map = new List<List<MapNode>>();

            for (int row = 0; row < config.numRows; row++)
            {
                var rowNodes = new List<MapNode>();
                bool isFirstRow = (row == 0);
                bool isLastRow = (row == config.numRows - 1);

                int nodeCount;
                if (isLastRow)
                    nodeCount = 1; // Boss node
                else
                    nodeCount = Random.Range(config.minNodesPerRow, config.maxNodesPerRow + 1);

                for (int col = 0; col < nodeCount; col++)
                {
                    var node = new MapNode
                    {
                        position = new Vector2Int(row, col),
                        visited = false,
                        available = false
                    };

                    // Assign node type
                    if (isLastRow)
                    {
                        node.nodeType = NodeType.Boss;
                    }
                    else if (isFirstRow)
                    {
                        node.nodeType = config.firstRowType;
                    }
                    else
                    {
                        node.nodeType = PickNodeType(config.nodeTypeWeights);
                    }

                    // Assign difficulty based on row
                    node.difficulty = Mathf.CeilToInt((float)(row + 1) / config.numRows * 10f);
                    node.difficulty = Mathf.Clamp(node.difficulty, 1, 10);

                    // Assign encounter definition for encounter nodes
                    if (node.nodeType == NodeType.ResourceEncounter && config.encounterPool.Count > 0)
                    {
                        node.encounterDefinition = config.encounterPool[Random.Range(0, config.encounterPool.Count)];
                    }
                    else if (node.nodeType == NodeType.EliteEncounter && config.eliteEncounterPool.Count > 0)
                    {
                        node.encounterDefinition = config.eliteEncounterPool[Random.Range(0, config.eliteEncounterPool.Count)];
                    }

                    rowNodes.Add(node);
                    Debug.Log($"[MapGenerator] GenerateMap: node ({row},{col}) type={node.nodeType}, difficulty={node.difficulty}");
                }

                map.Add(rowNodes);
            }

            // Generate connections (each node connects to 1-2 nodes in the next row)
            GenerateConnections(map);

            // Mark first row as available
            foreach (var node in map[0])
                node.available = true;

            Debug.Log($"[MapGenerator] GenerateMap: complete — {config.numRows} rows generated");
            return map;
        }

        private static void GenerateConnections(List<List<MapNode>> map)
        {
            for (int row = 0; row < map.Count - 1; row++)
            {
                var currentRow = map[row];
                var nextRow = map[row + 1];

                // Ensure every node in current row has at least one connection
                foreach (var node in currentRow)
                {
                    if (nextRow.Count == 1)
                    {
                        // Only one node in next row — everyone connects to it
                        node.connectedNodeIndices.Add(0);
                    }
                    else
                    {
                        // Connect to nearest node(s) in next row
                        float relativePos = (float)node.position.y / Mathf.Max(1, currentRow.Count - 1);
                        int primaryTarget = Mathf.RoundToInt(relativePos * (nextRow.Count - 1));
                        primaryTarget = Mathf.Clamp(primaryTarget, 0, nextRow.Count - 1);

                        node.connectedNodeIndices.Add(primaryTarget);

                        // 50% chance to also connect to an adjacent node for branching
                        if (Random.value > 0.5f)
                        {
                            int secondary = primaryTarget + (Random.value > 0.5f ? 1 : -1);
                            secondary = Mathf.Clamp(secondary, 0, nextRow.Count - 1);
                            if (secondary != primaryTarget && !node.connectedNodeIndices.Contains(secondary))
                            {
                                node.connectedNodeIndices.Add(secondary);
                            }
                        }
                    }
                }

                // Ensure every node in next row has at least one incoming connection
                for (int col = 0; col < nextRow.Count; col++)
                {
                    bool hasIncoming = false;
                    foreach (var node in currentRow)
                    {
                        if (node.connectedNodeIndices.Contains(col))
                        {
                            hasIncoming = true;
                            break;
                        }
                    }

                    if (!hasIncoming)
                    {
                        // Connect nearest node in current row
                        float relativePos = (float)col / Mathf.Max(1, nextRow.Count - 1);
                        int nearestIdx = Mathf.RoundToInt(relativePos * (currentRow.Count - 1));
                        nearestIdx = Mathf.Clamp(nearestIdx, 0, currentRow.Count - 1);
                        currentRow[nearestIdx].connectedNodeIndices.Add(col);
                        Debug.Log($"[MapGenerator] GenerateConnections: forced connection from row {row} col {nearestIdx} -> row {row + 1} col {col}");
                    }
                }
            }
        }

        private static NodeType PickNodeType(List<NodeTypeWeight> weights)
        {
            if (weights == null || weights.Count == 0)
                return NodeType.ResourceEncounter;

            float totalWeight = 0f;
            foreach (var w in weights)
                totalWeight += w.weight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var w in weights)
            {
                cumulative += w.weight;
                if (roll <= cumulative)
                    return w.nodeType;
            }

            return weights[weights.Count - 1].nodeType;
        }

        public static bool ValidateMap(List<List<MapNode>> map)
        {
            if (map == null || map.Count == 0) return false;

            // Check all first-row nodes can reach the boss
            foreach (var startNode in map[0])
            {
                if (!CanReachBoss(map, startNode))
                {
                    Debug.LogWarning($"[MapGenerator] ValidateMap: node ({startNode.position}) cannot reach boss!");
                    return false;
                }
            }

            Debug.Log("[MapGenerator] ValidateMap: all paths reach boss — map is valid");
            return true;
        }

        private static bool CanReachBoss(List<List<MapNode>> map, MapNode start)
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<MapNode>();
            queue.Enqueue(start);
            visited.Add(start.position);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.nodeType == NodeType.Boss)
                    return true;

                int nextRow = current.position.x + 1;
                if (nextRow >= map.Count) continue;

                foreach (int colIdx in current.connectedNodeIndices)
                {
                    if (colIdx >= 0 && colIdx < map[nextRow].Count)
                    {
                        var nextNode = map[nextRow][colIdx];
                        if (!visited.Contains(nextNode.position))
                        {
                            visited.Add(nextNode.position);
                            queue.Enqueue(nextNode);
                        }
                    }
                }
            }

            return false;
        }
    }
}
