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

        public List<List<MapNode>> Map => map;
        public MapNode CurrentNode => currentNode;
        public int CurrentRow => currentRow;
        public MapConfigSO Config => config;

        public void InitializeMap(MapConfigSO mapConfig)
        {
            config = mapConfig;
            currentRow = -1;
            currentNode = null;

            map = MapGenerator.GenerateMap(config);

            if (!MapGenerator.ValidateMap(map))
            {
                Debug.LogWarning("[MapManager] InitializeMap: map validation failed — regenerating");
                map = MapGenerator.GenerateMap(config);
            }

            Debug.Log($"[MapManager] InitializeMap: map ready — {map.Count} rows, level={config.levelNumber}");
            EventBus.OnMapReady?.Invoke();
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
            EventBus.OnMapNodeSelected?.Invoke(node);
        }

        public void OnNodeComplete()
        {
            Debug.Log($"[MapManager] OnNodeComplete: row={currentRow}, type={currentNode?.nodeType}");

            // Check if boss was just defeated
            if (currentNode != null && currentNode.nodeType == NodeType.Boss)
            {
                Debug.Log("[MapManager] OnNodeComplete: boss defeated — level complete");
                EventBus.OnLevelComplete?.Invoke();
                return;
            }

            // Check if we've reached the end of the map
            if (currentRow >= map.Count - 1)
            {
                Debug.Log("[MapManager] OnNodeComplete: reached end of map — level complete");
                EventBus.OnLevelComplete?.Invoke();
                return;
            }

            EventBus.OnMapNodeComplete?.Invoke();
        }

        public bool IsMapComplete()
        {
            return currentRow >= map.Count - 1;
        }
    }
}
