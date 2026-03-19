using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;


namespace Scurry.Map
{
    public static class MapGenerator
    {
        /// <summary>
        /// v1.0 compatibility overload: generates a map with a random seed.
        /// Returns a List of List of MapNode for v1.0 row-based code.
        /// </summary>
        public static List<List<MapNode>> GenerateMap(MapConfigSO config)
        {
            int seed = UnityEngine.Random.Range(0, int.MaxValue);
            var graph = GenerateMap(config, seed);
            // Convert graph to v1.0 row-based format (single row with all nodes)
            var result = new List<List<MapNode>>();
            result.Add(new List<MapNode>(graph.GetAllNodes()));
            return result;
        }

        /// <summary>
        /// v1.0 compatibility overload: validates a v1.0 row-based map.
        /// </summary>
        public static bool ValidateMap(List<List<MapNode>> map)
        {
            if (map == null || map.Count == 0) return false;
            // v1.0 compat: just check that the map has nodes
            foreach (var row in map)
            {
                if (row == null || row.Count == 0)
                {
                    if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[MapGenerator] ValidateMap(v1.0): found empty row");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Colony sub-network node count (19 additional nodes beyond the entrance).
        /// </summary>
        private const int ColonySubNetworkCount = 19;

        /// <summary>
        /// First ID used for colony sub-network nodes.
        /// </summary>
        private const int ColonySubNetworkStartId = 32;

        /// <summary>
        /// Generates a 52-node graph map: Colony Entrance (id=0), 10 Wilderness (1-10),
        /// 10 Farmland (11-20), 10 Town (21-30), 19 Colony sub-network (32-50),
        /// PiedPiper (id=51).
        /// </summary>
        public static MapGraph GenerateMap(MapConfigSO config, int seed)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] GenerateMap: starting generation (seed={seed}, nodesPerZone={config.nodesPerZone})");
            SeededRandom.Initialize(seed);

            var graph = new MapGraph();

            // Step 1: Create Colony Entrance node (id=0) at bottom center
            var colonyNode = new MapNode
            {
                nodeId = 0,
                zone = NodeType.Colony,
                worldPosition = new Vector2(0f, -5f),
                displayName = "Colony Entrance",
                visited = true,
                fogState = FogState.Visible
            };
            graph.AddNode(colonyNode);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] GenerateMap: created Colony Entrance node (id=0, pos={colonyNode.worldPosition})");

            // Step 2: Create Colony sub-network (ids 32-50) around the entrance
            CreateColonySubNetwork(graph, config);

            // Step 3: Create Wilderness nodes (ids 1-10)
            // Zones: yMin/yMax = vertical band, xMin/xMax = horizontal spread
            // Expanded bounds for better spacing (10 nodes need ~2 units between them)
            CreateZoneNodes(graph, config, NodeType.Wilderness, 1, config.nodesPerZone, -4f, 0f, -6f, 6f, "Wilderness");

            // Step 4: Create Farmland nodes (ids 11-20)
            CreateZoneNodes(graph, config, NodeType.Farmland, config.nodesPerZone + 1, config.nodesPerZone, 1.5f, 5.5f, -6f, 6f, "Farmland");

            // Step 5: Create Town nodes (ids 21-30)
            CreateZoneNodes(graph, config, NodeType.Town, config.nodesPerZone * 2 + 1, config.nodesPerZone, 7f, 11f, -6f, 6f, "Town");

            // Step 6: Create PiedPiper node (id=51)
            int piperId = ColonySubNetworkStartId + ColonySubNetworkCount; // 32 + 19 = 51
            var piperNode = new MapNode
            {
                nodeId = piperId,
                zone = NodeType.PiedPiper,
                worldPosition = new Vector2(0f, 13f),
                displayName = "Pied Piper",
                visited = false,
                fogState = FogState.Hidden
            };
            graph.AddNode(piperNode);
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] GenerateMap: created PiedPiper node (id={piperId}, pos={piperNode.worldPosition})");

            // Step 7: Create edges between spatially close nodes, then prune crossings
            // (excludes colony sub-network nodes, which are already wired by CreateColonySubNetwork)
            CreatePlanarEdges(graph, config);

            // Step 8: Assign resources
            AssignResources(graph, config);

            int totalNodes = graph.GetAllNodes().Count;
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] GenerateMap: generation complete (totalNodes={totalNodes}, seed={seed})");
            return graph;
        }

        /// <summary>
        /// Creates 19 colony sub-network nodes (ids 32-50) positioned in an organic cluster
        /// below and around the Colony Entrance (node 0). Connects them with distance-based
        /// edges (max degree 4). Also connects the entrance to 3-4 adjacent colony nodes.
        /// All colony sub-network nodes are always visible (no fog of war).
        /// </summary>
        private static void CreateColonySubNetwork(MapGraph graph, MapConfigSO config)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateColonySubNetwork: creating {ColonySubNetworkCount} colony nodes (startId={ColonySubNetworkStartId})");

            // Grid-jitter layout in a tight cluster below the entrance
            // Y range: -6 to -12, X range: -4 to 4
            float xMin = -4f, xMax = 4f;
            float yMin = -12f, yMax = -6f;
            float rangeX = xMax - xMin; // 8
            float rangeY = yMax - yMin; // 6

            // Calculate grid dimensions for 19 nodes
            int cols, rows;
            if (rangeX >= rangeY)
            {
                cols = Mathf.CeilToInt(Mathf.Sqrt(ColonySubNetworkCount * rangeX / Mathf.Max(rangeY, 0.1f)));
                cols = Mathf.Clamp(cols, 2, ColonySubNetworkCount);
                rows = Mathf.CeilToInt((float)ColonySubNetworkCount / cols);
            }
            else
            {
                rows = Mathf.CeilToInt(Mathf.Sqrt(ColonySubNetworkCount * rangeY / Mathf.Max(rangeX, 0.1f)));
                rows = Mathf.Clamp(rows, 2, ColonySubNetworkCount);
                cols = Mathf.CeilToInt((float)ColonySubNetworkCount / rows);
            }

            float cellW = rangeX / cols;
            float cellH = rangeY / rows;
            float jitterX = cellW * 0.3f;
            float jitterY = cellH * 0.3f;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateColonySubNetwork: grid layout (cols={cols}, rows={rows}, cellW={cellW:F2}, cellH={cellH:F2}, jitter=±{jitterX:F2}/{jitterY:F2})");

            var colonyNodes = new List<MapNode>();

            for (int i = 0; i < ColonySubNetworkCount; i++)
            {
                int nodeId = ColonySubNetworkStartId + i;
                int col = i % cols;
                int row = i / cols;

                float cx = xMin + (col + 0.5f) * cellW;
                float cy = yMin + (row + 0.5f) * cellH;

                float x = cx + SeededRandom.Range(-jitterX, jitterX);
                float y = cy + SeededRandom.Range(-jitterY, jitterY);

                x = Mathf.Clamp(x, xMin + 0.2f, xMax - 0.2f);
                y = Mathf.Clamp(y, yMin + 0.2f, yMax - 0.2f);

                var node = new MapNode
                {
                    nodeId = nodeId,
                    zone = NodeType.Colony,
                    worldPosition = new Vector2(x, y),
                    displayName = $"Colony {i + 1}",
                    visited = false,
                    fogState = FogState.Visible
                };

                graph.AddNode(node);
                colonyNodes.Add(node);
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateColonySubNetwork: created node (id={nodeId}, pos=({x:F2},{y:F2}), grid=({col},{row}))");
            }

            // Build edges between colony sub-network nodes using distance-based approach
            // Include the entrance node (id=0) in the wiring
            var entranceNode = graph.GetNode(0);
            var allColonyNodes = new List<MapNode> { entranceNode };
            allColonyNodes.AddRange(colonyNodes);

            // Generate candidate edges sorted by distance
            var candidates = new List<(int a, int b, float dist)>();
            for (int i = 0; i < allColonyNodes.Count; i++)
            {
                for (int j = i + 1; j < allColonyNodes.Count; j++)
                {
                    float dist = Vector2.Distance(allColonyNodes[i].worldPosition, allColonyNodes[j].worldPosition);
                    candidates.Add((allColonyNodes[i].nodeId, allColonyNodes[j].nodeId, dist));
                }
            }
            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            // Greedily add edges, max degree 4 per colony node
            int maxColonyDegree = 4;
            var colonyDegree = new Dictionary<int, int>();
            foreach (var node in allColonyNodes)
                colonyDegree[node.nodeId] = 0;

            var addedColonyEdges = new List<(int a, int b)>();

            foreach (var (a, b, dist) in candidates)
            {
                // Skip if either node already at max degree
                if (colonyDegree[a] >= maxColonyDegree && colonyDegree[b] >= maxColonyDegree) continue;

                // Skip overly long edges (keep the cluster tight)
                if (dist > 3.5f) continue;

                // Skip if this edge crosses any existing colony edge
                var posA = graph.GetNode(a).worldPosition;
                var posB = graph.GetNode(b).worldPosition;
                bool crosses = false;

                foreach (var (ea, eb) in addedColonyEdges)
                {
                    if (ea == a || ea == b || eb == a || eb == b) continue;
                    var posEA = graph.GetNode(ea).worldPosition;
                    var posEB = graph.GetNode(eb).worldPosition;
                    if (SegmentsIntersect(posA, posB, posEA, posEB))
                    {
                        crosses = true;
                        break;
                    }
                }

                if (crosses) continue;

                graph.AddEdge(a, b);
                addedColonyEdges.Add((a, b));
                colonyDegree[a]++;
                colonyDegree[b]++;

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateColonySubNetwork: edge ({a}-{b}, dist={dist:F1})");
            }

            // Ensure all colony nodes are connected to the entrance via BFS
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(0);
            visited.Add(0);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                var node = graph.GetNode(current);
                if (node == null) continue;
                foreach (int neighbor in node.neighborIds)
                {
                    // Only traverse within colony nodes
                    if (colonyDegree.ContainsKey(neighbor) && visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }

            // Bridge any disconnected colony nodes
            var disconnected = new List<int>();
            foreach (var node in colonyNodes)
            {
                if (!visited.Contains(node.nodeId))
                    disconnected.Add(node.nodeId);
            }

            if (disconnected.Count > 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateColonySubNetwork: {disconnected.Count} disconnected colony nodes, adding bridge edges");
            }

            while (disconnected.Count > 0)
            {
                float bestDist = float.MaxValue;
                int bestDisc = -1, bestVisited = -1;

                foreach (int dId in disconnected)
                {
                    var dNode = graph.GetNode(dId);
                    foreach (int vId in visited)
                    {
                        var vNode = graph.GetNode(vId);
                        float d = Vector2.Distance(dNode.worldPosition, vNode.worldPosition);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestDisc = dId;
                            bestVisited = vId;
                        }
                    }
                }

                if (bestDisc < 0) break;

                graph.AddEdge(bestDisc, bestVisited);
                addedColonyEdges.Add((bestDisc, bestVisited));
                colonyDegree[bestDisc]++;
                colonyDegree[bestVisited]++;

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateColonySubNetwork: bridge edge ({bestDisc}-{bestVisited}, dist={bestDist:F1})");

                queue.Enqueue(bestDisc);
                visited.Add(bestDisc);
                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    var node = graph.GetNode(current);
                    if (node == null) continue;
                    foreach (int neighbor in node.neighborIds)
                    {
                        if (colonyDegree.ContainsKey(neighbor) && visited.Add(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }

                disconnected.RemoveAll(id => visited.Contains(id));
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateColonySubNetwork: complete (colonyNodes={ColonySubNetworkCount + 1}, edges={addedColonyEdges.Count})");
        }

        /// <summary>
        /// Creates nodes for a single zone with jittered positions.
        /// </summary>
        private static void CreateZoneNodes(MapGraph graph, MapConfigSO config, NodeType zone,
            int startId, int count, float yMin, float yMax, float xMin, float xMax, string zoneName)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateZoneNodes: creating zone (zone={zoneName}, startId={startId}, count={count})");

            // Grid-jitter: orient grid so the LONGER axis gets more columns.
            // This ensures cells are as square as possible and nodes don't overlap.
            float rangeX = xMax - xMin;
            float rangeY = yMax - yMin;

            int cols, rows;
            if (rangeX >= rangeY)
            {
                // Wide zone: more columns than rows
                cols = Mathf.CeilToInt(Mathf.Sqrt(count * rangeX / Mathf.Max(rangeY, 0.1f)));
                cols = Mathf.Clamp(cols, 2, count);
                rows = Mathf.CeilToInt((float)count / cols);
            }
            else
            {
                // Tall zone: more rows than columns
                rows = Mathf.CeilToInt(Mathf.Sqrt(count * rangeY / Mathf.Max(rangeX, 0.1f)));
                rows = Mathf.Clamp(rows, 2, count);
                cols = Mathf.CeilToInt((float)count / rows);
            }

            float cellW = rangeX / cols;
            float cellH = rangeY / rows;
            float jitterX = cellW * 0.25f;
            float jitterY = cellH * 0.25f;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateZoneNodes: grid layout (cols={cols}, rows={rows}, cellW={cellW:F2}, cellH={cellH:F2}, jitter=±{jitterX:F2}/{jitterY:F2})");

            for (int i = 0; i < count; i++)
            {
                int nodeId = startId + i;
                int col = i % cols;
                int row = i / cols;

                // Cell center
                float cx = xMin + (col + 0.5f) * cellW;
                float cy = yMin + (row + 0.5f) * cellH;

                // Add jitter
                float x = cx + SeededRandom.Range(-jitterX, jitterX);
                float y = cy + SeededRandom.Range(-jitterY, jitterY);

                // Clamp to zone bounds with margin
                x = Mathf.Clamp(x, xMin + 0.3f, xMax - 0.3f);
                y = Mathf.Clamp(y, yMin + 0.3f, yMax - 0.3f);

                var node = new MapNode
                {
                    nodeId = nodeId,
                    zone = zone,
                    worldPosition = new Vector2(x, y),
                    displayName = $"{zoneName} {i + 1}",
                    visited = false,
                    fogState = FogState.Hidden
                };

                graph.AddNode(node);
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreateZoneNodes: created node (id={nodeId}, zone={zoneName}, pos=({x:F2},{y:F2}), grid=({col},{row}))");
            }
        }

        /// <summary>
        /// Creates edges between spatially close nodes, ensuring:
        /// 1) Graph is connected (all nodes reachable)
        /// 2) Edges are short (nearest-neighbor based)
        /// 3) No edge crosses another edge
        /// 4) No edge passes through a non-connected node
        /// Algorithm: sorted candidate edges by distance → add greedily if no crossing → ensure connectivity
        /// </summary>
        private static void CreatePlanarEdges(MapGraph graph, MapConfigSO config)
        {
            // Exclude colony sub-network nodes (ids 32-50) — they are already wired by CreateColonySubNetwork.
            // The Colony Entrance (id=0) is included so it connects to Wilderness nodes.
            var allNodes = new List<MapNode>();
            foreach (var node in graph.GetAllNodes())
            {
                if (node.nodeId >= ColonySubNetworkStartId && node.nodeId < ColonySubNetworkStartId + ColonySubNetworkCount)
                    continue;
                allNodes.Add(node);
            }
            if (allNodes.Count < 2) return;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreatePlanarEdges: building edges for {allNodes.Count} non-colony-subnet nodes (excluded {ColonySubNetworkCount} colony sub-network nodes)");

            // Step 1: Generate all candidate edges sorted by distance (shortest first)
            var candidates = new List<(int a, int b, float dist)>();
            for (int i = 0; i < allNodes.Count; i++)
            {
                for (int j = i + 1; j < allNodes.Count; j++)
                {
                    float dist = Vector2.Distance(allNodes[i].worldPosition, allNodes[j].worldPosition);
                    candidates.Add((allNodes[i].nodeId, allNodes[j].nodeId, dist));
                }
            }
            candidates.Sort((x, y) => x.dist.CompareTo(y.dist));

            // Step 2: Greedily add edges shortest-first, skip if crossing exists
            var addedEdges = new List<(int a, int b)>();
            var edgeDegree = new Dictionary<int, int>();
            foreach (var node in allNodes)
                edgeDegree[node.nodeId] = 0;

            int maxDegree = config.maxEdgesPerNode > 0 ? config.maxEdgesPerNode : 4;

            foreach (var (a, b, dist) in candidates)
            {
                // Skip if either node already at max degree
                if (edgeDegree[a] >= maxDegree && edgeDegree[b] >= maxDegree) continue;

                // Skip if this edge crosses any existing edge
                var posA = graph.GetNode(a).worldPosition;
                var posB = graph.GetNode(b).worldPosition;
                bool crosses = false;

                foreach (var (ea, eb) in addedEdges)
                {
                    // Edges sharing a vertex can't cross
                    if (ea == a || ea == b || eb == a || eb == b) continue;

                    var posEA = graph.GetNode(ea).worldPosition;
                    var posEB = graph.GetNode(eb).worldPosition;
                    if (SegmentsIntersect(posA, posB, posEA, posEB))
                    {
                        crosses = true;
                        break;
                    }
                }

                if (crosses) continue;

                // Skip if edge passes too close to a non-endpoint node
                bool tooClose = false;
                foreach (var node in allNodes)
                {
                    if (node.nodeId == a || node.nodeId == b) continue;
                    float clearance = PointToSegmentDistance(node.worldPosition, posA, posB);
                    if (clearance < 0.8f) // node radius + small margin
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                // Add the edge
                graph.AddEdge(a, b);
                addedEdges.Add((a, b));
                edgeDegree[a]++;
                edgeDegree[b]++;

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreatePlanarEdges: edge ({a}-{b}, dist={dist:F1})");
            }

            // Step 3: Ensure graph is connected (BFS from colony, add bridge edges if needed)
            EnsureConnectivity(graph, allNodes, addedEdges, edgeDegree);

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] CreatePlanarEdges: complete (edges={addedEdges.Count})");
        }

        /// <summary>
        /// BFS from colony node. For each disconnected component, add the shortest
        /// non-crossing edge that bridges it to the connected component.
        /// </summary>
        private static void EnsureConnectivity(MapGraph graph, List<MapNode> allNodes,
            List<(int a, int b)> addedEdges, Dictionary<int, int> edgeDegree)
        {
            int colonyId = graph.ColonyNodeId;
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(colonyId);
            visited.Add(colonyId);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                var node = graph.GetNode(current);
                if (node == null) continue;
                foreach (int neighbor in node.neighborIds)
                {
                    if (visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }

            // Find disconnected nodes
            var disconnected = new List<int>();
            foreach (var node in allNodes)
            {
                if (!visited.Contains(node.nodeId))
                    disconnected.Add(node.nodeId);
            }

            if (disconnected.Count == 0) return;

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] EnsureConnectivity: {disconnected.Count} disconnected nodes, adding bridge edges");

            // For each disconnected node, find nearest visited node and add edge
            while (disconnected.Count > 0)
            {
                float bestDist = float.MaxValue;
                int bestDisc = -1, bestVisited = -1;

                foreach (int dId in disconnected)
                {
                    var dNode = graph.GetNode(dId);
                    foreach (int vId in visited)
                    {
                        var vNode = graph.GetNode(vId);
                        float dist = Vector2.Distance(dNode.worldPosition, vNode.worldPosition);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestDisc = dId;
                            bestVisited = vId;
                        }
                    }
                }

                if (bestDisc < 0) break;

                graph.AddEdge(bestDisc, bestVisited);
                addedEdges.Add((bestDisc, bestVisited));
                edgeDegree[bestDisc]++;
                edgeDegree[bestVisited]++;

                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] EnsureConnectivity: bridge edge ({bestDisc}-{bestVisited}, dist={bestDist:F1})");

                // BFS again from the newly connected node to find all nodes reachable through it
                queue.Enqueue(bestDisc);
                visited.Add(bestDisc);
                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    var node = graph.GetNode(current);
                    if (node == null) continue;
                    foreach (int neighbor in node.neighborIds)
                    {
                        if (visited.Add(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }

                disconnected.RemoveAll(id => visited.Contains(id));
            }
        }

        /// <summary>
        /// Tests if two line segments AB and CD intersect (proper crossing, not touching at endpoints).
        /// </summary>
        private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float d1 = Cross2D(c, d, a);
            float d2 = Cross2D(c, d, b);
            float d3 = Cross2D(a, b, c);
            float d4 = Cross2D(a, b, d);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            return false;
        }

        private static float Cross2D(Vector2 a, Vector2 b, Vector2 p)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }

        /// <summary>
        /// Returns the shortest distance from point P to line segment AB.
        /// </summary>
        private static float PointToSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return Vector2.Distance(p, a);

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            Vector2 proj = a + ab * t;
            return Vector2.Distance(p, proj);
        }

        /// <summary>
        /// Assigns resources to all map nodes based on zone type and config ranges.
        /// </summary>
        private static void AssignResources(MapGraph graph, MapConfigSO config)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log("[MapGenerator] AssignResources: starting resource assignment");

            foreach (MapNode node in graph.GetAllNodes())
            {
                switch (node.zone)
                {
                    case NodeType.Wilderness:
                        AssignNodeResources(node, config.wildernessResourceMin, config.wildernessResourceMax,
                            new[] { ResourceType.Food, ResourceType.Food, ResourceType.Food, ResourceType.Materials });
                        break;

                    case NodeType.Farmland:
                        AssignNodeResources(node, config.farmlandResourceMin, config.farmlandResourceMax,
                            new[] { ResourceType.Food, ResourceType.Materials, ResourceType.Currency });
                        break;

                    case NodeType.Town:
                        AssignNodeResources(node, config.townResourceMin, config.townResourceMax,
                            new[] { ResourceType.Currency, ResourceType.Currency, ResourceType.Materials });
                        break;

                    // Colony and PiedPiper nodes don't have gatherable resources
                    default:
                        break;
                }
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log("[MapGenerator] AssignResources: resource assignment complete");
        }

        /// <summary>
        /// Assigns a random number of resources to a single node, drawn from a weighted pool.
        /// </summary>
        private static void AssignNodeResources(MapNode node, int minResources, int maxResources, ResourceType[] weightedPool)
        {
            int totalResources = SeededRandom.Range(minResources, maxResources + 1);

            for (int i = 0; i < totalResources; i++)
            {
                ResourceType type = weightedPool[SeededRandom.Range(0, weightedPool.Length)];
                if (node.resources.ContainsKey(type))
                    node.resources[type]++;
                else
                    node.resources[type] = 1;
            }

            if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] AssignNodeResources: (nodeId={node.nodeId}, zone={node.zone}, totalResources={totalResources}, breakdown={ResourceDictToString(node.resources)})");
        }

        /// <summary>
        /// Fisher-Yates shuffle using SeededRandom.
        /// </summary>
        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = SeededRandom.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Helper to format a resource dictionary as a string for logging.
        /// </summary>
        private static string ResourceDictToString(Dictionary<ResourceType, int> resources)
        {
            if (resources.Count == 0) return "{}";

            var parts = new List<string>();
            foreach (var kvp in resources)
                parts.Add($"{kvp.Key}={kvp.Value}");
            return "{" + string.Join(", ", parts) + "}";
        }

        /// <summary>
        /// Validates that the generated graph is fully connected (every node can reach every other node).
        /// </summary>
        public static bool ValidateMap(MapGraph graph)
        {
            if (!SimulationFlags.SuppressLogging) Debug.Log("[MapGenerator] ValidateMap: starting validation");

            var allNodes = graph.GetAllNodes();
            if (allNodes.Count == 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[MapGenerator] ValidateMap: graph is empty");
                return false;
            }

            // BFS from Colony node to check full connectivity
            int startId = graph.ColonyNodeId;
            if (startId < 0)
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning("[MapGenerator] ValidateMap: no Colony node found");
                return false;
            }

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(startId);
            visited.Add(startId);

            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                MapNode current = graph.GetNode(currentId);
                if (current == null) continue;

                foreach (int neighborId in current.neighborIds)
                {
                    if (!visited.Contains(neighborId))
                    {
                        visited.Add(neighborId);
                        queue.Enqueue(neighborId);
                    }
                }
            }

            bool valid = visited.Count == allNodes.Count;

            if (valid)
            {
                if (!SimulationFlags.SuppressLogging) Debug.Log($"[MapGenerator] ValidateMap: map is valid — all {allNodes.Count} nodes reachable from Colony");
            }
            else
            {
                if (!SimulationFlags.SuppressLogging) Debug.LogWarning($"[MapGenerator] ValidateMap: map is NOT fully connected — reached {visited.Count}/{allNodes.Count} nodes from Colony");
            }

            return valid;
        }
    }
}
