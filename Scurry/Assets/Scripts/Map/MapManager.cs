using System.Collections.Generic;
using UnityEngine;
using Scurry.Data;
using Scurry.Core;
using Scurry.Interfaces;

namespace Scurry.Map
{
    public class MapManager : MonoBehaviour, IMapManager
    {
        private List<List<MapNode>> map;
        private MapConfigSO config;
        private MapNode currentNode;
        private int currentRow = -1; // -1 = before first row (start)

        // v2.0 graph-based map
        private MapGraph mapGraph;

        public List<List<MapNode>> Map => map;
        public MapNode CurrentNode => currentNode;
        public int CurrentRow => currentRow;
        public MapConfigSO Config => config;
        public MapGraph Graph => mapGraph;

        private void Awake()
        {
            ServiceLocator.Register<IMapManager>(this);
            Debug.Log("[MapManager] Awake: registered with ServiceLocator");
        }

        // ── IMapManager v2.0 interface methods ─────────────────────────

        public void GenerateMap(int seed)
        {
            Debug.Log($"[MapManager] GenerateMap: generating v2.0 graph map (seed={seed})");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MapConfigSO>();
                Debug.Log("[MapManager] GenerateMap: created default MapConfigSO");
            }
            mapGraph = MapGenerator.GenerateMap(config, seed);
            Debug.Log($"[MapManager] GenerateMap: complete (nodeCount={mapGraph?.GetAllNodes()?.Count ?? 0})");
        }

        public MapNode GetNode(int nodeId)
        {
            if (mapGraph == null)
            {
                Debug.LogWarning($"[MapManager] GetNode: mapGraph is null (nodeId={nodeId})");
                return null;
            }
            return mapGraph.GetNode(nodeId);
        }

        public List<int> GetPath(int fromId, int toId)
        {
            if (mapGraph == null)
            {
                Debug.LogWarning($"[MapManager] GetPath: mapGraph is null (from={fromId}, to={toId})");
                return new List<int>();
            }
            return mapGraph.ShortestPath(fromId, toId);
        }

        public void InitializeMap(MapConfigSO mapConfig)
        {
            config = mapConfig;
            currentRow = -1;
            currentNode = null;

            int seed = UnityEngine.Random.Range(0, int.MaxValue);
            mapGraph = MapGenerator.GenerateMap(config, seed);

            if (!MapGenerator.ValidateMap(mapGraph))
            {
                Debug.LogWarning("[MapManager] InitializeMap: map validation failed — regenerating");
                mapGraph = MapGenerator.GenerateMap(config, seed + 1);
            }

            // Build legacy row-based map from graph for v1.0 compatibility
            map = new List<List<MapNode>>();
            map.Add(new List<MapNode>(mapGraph.GetAllNodes()));

            Debug.Log($"[MapManager] InitializeMap: map ready — {mapGraph.GetAllNodes().Count} nodes");
        }

        public List<MapNode> GetAvailableNodes()
        {
            var available = new List<MapNode>();
            if (map == null) return available;

            if (currentRow < 0)
            {
                // Haven't moved yet — first row is available
                if (map.Count > 0)
                {
                    foreach (var node in map[0])
                    {
                        node.available = true;
                        available.Add(node);
                    }
                }
            }
            else if (currentNode != null)
            {
                int nextRow = currentRow + 1;
                if (nextRow < map.Count)
                {
                    foreach (int colIdx in currentNode.connectedNodeIndices)
                    {
                        if (colIdx >= 0 && colIdx < map[nextRow].Count)
                        {
                            var node = map[nextRow][colIdx];
                            node.available = true;
                            available.Add(node);
                        }
                    }
                }
            }

            Debug.Log($"[MapManager] GetAvailableNodes: {available.Count} nodes available (currentRow={currentRow})");
            return available;
        }

        public void SelectNode(MapNode node)
        {
            Debug.Log($"[MapManager] SelectNode: {node}");

            // Mark previous available nodes as unavailable
            if (map != null)
            {
                int targetRow = currentRow < 0 ? 0 : currentRow + 1;
                if (targetRow < map.Count)
                {
                    foreach (var n in map[targetRow])
                        n.available = false;
                }
            }

            node.visited = true;
            node.available = false;
            currentNode = node;
            currentRow = node.position.x;

            Debug.Log($"[MapManager] SelectNode: moved to row={currentRow}, type={node.nodeType}, difficulty={node.difficulty}");
        }

        public void OnNodeComplete()
        {
            Debug.Log($"[MapManager] OnNodeComplete: row={currentRow}, type={currentNode?.nodeType}, mapInitialized={map != null}");

            if (map == null)
            {
                Debug.LogWarning("[MapManager] OnNodeComplete: map is null — no map initialized, ignoring");
                return;
            }

            // Check if boss was just defeated
            if (currentNode != null && currentNode.nodeType == NodeType.PiedPiper)
            {
                Debug.Log("[MapManager] OnNodeComplete: Pied Piper defeated — run complete");
                EventBus.OnRunComplete?.Invoke(true);
                return;
            }

            // Check if we've reached the end of the map
            if (currentRow >= map.Count - 1)
            {
                Debug.Log("[MapManager] OnNodeComplete: reached end of map — run complete");
                EventBus.OnRunComplete?.Invoke(true);
                return;
            }

            Debug.Log("[MapManager] OnNodeComplete: node complete, continuing");
        }

        public bool IsMapComplete()
        {
            return currentRow >= map.Count - 1;
        }

        /// <summary>
        /// Restore map state from a previously saved snapshot (used after Encounter scene transition).
        /// </summary>
        public void RestoreMapState(MapStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("[MapManager] RestoreMapState: snapshot is null");
                return;
            }
            config = snapshot.config;
            map = snapshot.map;
            currentRow = snapshot.currentRow;
            currentNode = snapshot.currentNode;
            Debug.Log($"[MapManager] RestoreMapState: restored map — rows={map?.Count ?? 0}, currentRow={currentRow}, currentNode={currentNode?.nodeType}");
        }

        /// <summary>
        /// Capture current map state for preservation across scene transitions.
        /// </summary>
        public MapStateSnapshot CaptureMapState()
        {
            var snapshot = new MapStateSnapshot
            {
                config = config,
                map = map,
                currentRow = currentRow,
                currentNode = currentNode
            };
            Debug.Log($"[MapManager] CaptureMapState: captured map — rows={map?.Count ?? 0}, currentRow={currentRow}");
            return snapshot;
        }
    }

    /// <summary>
    /// Holds map state for preservation across scene transitions.
    /// </summary>
    public class MapStateSnapshot
    {
        public MapConfigSO config;
        public List<List<MapNode>> map;
        public int currentRow;
        public MapNode currentNode;
    }
}
