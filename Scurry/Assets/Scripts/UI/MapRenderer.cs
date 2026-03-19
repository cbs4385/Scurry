using System.Collections.Generic;
using UnityEngine;
using Scurry.Core;
using Scurry.Map;
using Scurry.Data;

namespace Scurry.UI
{
    /// <summary>
    /// Renders the graph map in world space using SpriteRenderers for nodes,
    /// LineRenderers for edges, and child GameObjects for tokens and fog overlays.
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        // --- Constants ---
        private const float NodeRadius = 0.5f;
        private const float TokenRadius = 0.35f;
        private const float ResourceDotRadius = 0.08f;
        private const float TokenOffsetStep = 0.5f;
        private const float HighlightWidth = 0.12f;
        private const float EdgeWidth = 0.06f;

        // Sorting orders
        private const int SortEdge = 0;
        private const int SortNode = 1;
        private const int SortFog = 2;
        private const int SortToken = 3;
        private const int SortUI = 10;

        // Zone colors
        private static readonly Color WildernessColor = new Color(0.2f, 0.6f, 0.2f);
        private static readonly Color FarmlandColor = new Color(0.6f, 0.4f, 0.2f);
        private static readonly Color TownColor = new Color(0.5f, 0.5f, 0.5f);
        private static readonly Color ColonyColor = new Color(0.9f, 0.75f, 0.1f);
        private static readonly Color ColonyEmptyColor = new Color(0.45f, 0.38f, 0.15f, 0.6f);
        private static readonly Color PiedPiperColor = new Color(0.6f, 0.2f, 0.8f);
        private static readonly Color DefaultNodeColor = new Color(0.4f, 0.4f, 0.4f);

        // Fog colors
        private static readonly Color FogHiddenColor = new Color(0f, 0f, 0f, 0.95f);
        private static readonly Color FogRememberedColor = new Color(0.15f, 0.15f, 0.15f, 0.7f);
        private static readonly Color FogVisibleColor = new Color(0f, 0f, 0f, 0f);

        // --- State ---
        private MapGraph graph;
        private readonly Dictionary<int, GameObject> nodeGameObjects = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, SpriteRenderer> nodeSprites = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, SpriteRenderer> fogOverlays = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, List<GameObject>> resourceIndicators = new Dictionary<int, List<GameObject>>();
        private readonly List<GameObject> edgeObjects = new List<GameObject>();
        private readonly List<GameObject> highlightObjects = new List<GameObject>();

        // Token tracking
        private readonly Dictionary<int, GameObject> heroTokenGOs = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> enemyTokenGOs = new Dictionary<int, GameObject>();

        // Colony card model tracking (nodeId → model GO)
        private readonly Dictionary<int, GameObject> colonyCardModels = new Dictionary<int, GameObject>();

        // Cached white sprite (replaces removed SpriteHelper)
        private static Sprite _cachedWhiteSprite;
        private static Sprite GetWhiteSprite()
        {
            if (_cachedWhiteSprite == null)
            {
                var tex = new Texture2D(4, 4);
                var colors = new Color[16];
                for (int i = 0; i < 16; i++) colors[i] = Color.white;
                tex.SetPixels(colors);
                tex.Apply();
                _cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
            return _cachedWhiteSprite;
        }

        /// <summary>
        /// Initializes the renderer with the given map graph, creating all node and edge GameObjects.
        /// </summary>
        public void Initialize(MapGraph mapGraph)
        {
            Debug.Log($"[MapRenderer] Initialize: starting (graph={mapGraph})");

            if (mapGraph == null)
            {
                Debug.LogError("[MapRenderer] Initialize: mapGraph is null, aborting");
                return;
            }

            graph = mapGraph;

            // Clear any existing visuals
            ClearAll();

            var allNodes = graph.GetAllNodes();
            Debug.Log($"[MapRenderer] Initialize: creating visuals for {allNodes.Count} nodes");

            // Create node GameObjects
            foreach (var node in allNodes)
            {
                CreateNodeVisual(node);
            }

            // Create edges
            var processedEdges = new HashSet<(int, int)>();
            foreach (var node in allNodes)
            {
                foreach (int neighborId in node.neighborIds)
                {
                    int a = Mathf.Min(node.nodeId, neighborId);
                    int b = Mathf.Max(node.nodeId, neighborId);
                    if (!processedEdges.Contains((a, b)))
                    {
                        processedEdges.Add((a, b));
                        CreateEdgeVisual(node.nodeId, neighborId);
                    }
                }
            }

            // Render any colony cards already placed (starters)
            UpdateColonyCardModels();

            Debug.Log($"[MapRenderer] Initialize: complete (nodes={nodeGameObjects.Count}, edges={edgeObjects.Count})");
        }

        /// <summary>
        /// Refreshes all node, edge, token, and fog visuals based on current graph state.
        /// </summary>
        public void UpdateVisuals()
        {
            Debug.Log("[MapRenderer] UpdateVisuals: refreshing all visuals");

            if (graph == null)
            {
                Debug.LogWarning("[MapRenderer] UpdateVisuals: graph is null, skipping");
                return;
            }

            // Update colony card models (picks up newly placed cards)
            UpdateColonyCardModels();

            foreach (var node in graph.GetAllNodes())
            {
                UpdateNodeColor(node);
                UpdateResourceIndicators(node);
            }

            UpdateFog();
            Debug.Log("[MapRenderer] UpdateVisuals: complete");
        }

        /// <summary>
        /// Updates fog overlays on all nodes based on their FogState.
        /// </summary>
        public void UpdateFog()
        {
            Debug.Log("[MapRenderer] UpdateFog: updating fog overlays");

            if (graph == null)
            {
                Debug.LogWarning("[MapRenderer] UpdateFog: graph is null, skipping");
                return;
            }

            foreach (var node in graph.GetAllNodes())
            {
                if (fogOverlays.TryGetValue(node.nodeId, out SpriteRenderer fogSR))
                {
                    Color fogColor = node.fogState switch
                    {
                        FogState.Hidden => FogHiddenColor,
                        FogState.Remembered => FogRememberedColor,
                        FogState.Visible => FogVisibleColor,
                        _ => FogHiddenColor
                    };
                    fogSR.color = fogColor;
                }
            }
        }

        /// <summary>
        /// Updates token positions on the map. Creates/destroys token GameObjects as needed.
        /// Heroes are shown as colored circles; enemies as red circles (visible nodes only).
        /// </summary>
        public void UpdateTokenPositions(List<HeroToken> heroTokens, List<EnemyToken> enemyTokens)
        {
            if (graph == null)
            {
                Debug.LogWarning("[MapRenderer] UpdateTokenPositions: graph is null (not initialized), skipping");
                return;
            }
            Debug.Log($"[MapRenderer] UpdateTokenPositions: heroes={heroTokens?.Count ?? 0}, enemies={enemyTokens?.Count ?? 0}");

            // --- Hero tokens ---
            var activeHeroIds = new HashSet<int>();
            if (heroTokens != null)
            {
                // Group heroes by node for offset calculation
                var heroesByNode = new Dictionary<int, List<HeroToken>>();
                foreach (var hero in heroTokens)
                {
                    if (!hero.IsAlive || !hero.isDeployed) continue;
                    activeHeroIds.Add(hero.tokenId);

                    if (!heroesByNode.ContainsKey(hero.currentNodeId))
                        heroesByNode[hero.currentNodeId] = new List<HeroToken>();
                    heroesByNode[hero.currentNodeId].Add(hero);
                }

                foreach (var kvp in heroesByNode)
                {
                    int nodeId = kvp.Key;
                    var heroes = kvp.Value;
                    Vector3 basePos = GetNodeWorldPosition(nodeId);

                    for (int i = 0; i < heroes.Count; i++)
                    {
                        var hero = heroes[i];
                        float offsetX = (i - (heroes.Count - 1) * 0.5f) * TokenOffsetStep;
                        float offsetZ = i * 0.4f; // stagger depth so models don't clip
                        Vector3 pos = basePos + new Vector3(offsetX, -NodeRadius - TokenRadius - 0.05f, offsetZ);

                        GameObject tokenGO;
                        if (!heroTokenGOs.TryGetValue(hero.tokenId, out tokenGO) || tokenGO == null)
                        {
                            tokenGO = CreateHeroTokenVisual(hero);
                            heroTokenGOs[hero.tokenId] = tokenGO;
                            Debug.Log($"[MapRenderer] UpdateTokenPositions: created hero token GO (tokenId={hero.tokenId}, hero={hero.cardDef.cardName})");
                        }

                        tokenGO.transform.position = pos;
                        tokenGO.SetActive(true);
                    }
                }
            }

            // Hide/destroy hero tokens no longer active
            var staleHeroIds = new List<int>();
            foreach (var kvp in heroTokenGOs)
            {
                if (!activeHeroIds.Contains(kvp.Key))
                {
                    if (kvp.Value != null) kvp.Value.SetActive(false);
                    staleHeroIds.Add(kvp.Key);
                }
            }
            foreach (int id in staleHeroIds)
            {
                if (heroTokenGOs[id] != null) Destroy(heroTokenGOs[id]);
                heroTokenGOs.Remove(id);
                Debug.Log($"[MapRenderer] UpdateTokenPositions: removed stale hero token (tokenId={id})");
            }

            // --- Enemy tokens ---
            var activeEnemyIds = new HashSet<int>();
            if (enemyTokens != null)
            {
                var enemiesByNode = new Dictionary<int, List<EnemyToken>>();
                foreach (var enemy in enemyTokens)
                {
                    if (enemy.isDefeated) continue;
                    activeEnemyIds.Add(enemy.tokenId);

                    // Only show on visible nodes
                    MapNode node = graph.GetNode(enemy.currentNodeId);
                    if (node == null || node.fogState != FogState.Visible) continue;

                    if (!enemiesByNode.ContainsKey(enemy.currentNodeId))
                        enemiesByNode[enemy.currentNodeId] = new List<EnemyToken>();
                    enemiesByNode[enemy.currentNodeId].Add(enemy);
                }

                foreach (var kvp in enemiesByNode)
                {
                    int nodeId = kvp.Key;
                    var enemies = kvp.Value;
                    Vector3 basePos = GetNodeWorldPosition(nodeId);

                    for (int i = 0; i < enemies.Count; i++)
                    {
                        var enemy = enemies[i];
                        float offsetX = (i - (enemies.Count - 1) * 0.5f) * TokenOffsetStep;
                        Vector3 pos = basePos + new Vector3(offsetX, NodeRadius + TokenRadius + 0.05f, 0);

                        GameObject tokenGO;
                        if (!enemyTokenGOs.TryGetValue(enemy.tokenId, out tokenGO) || tokenGO == null)
                        {
                            tokenGO = CreateTokenVisual($"EnemyToken_{enemy.tokenId}", TokenRadius, Color.red, SortToken);
                            enemyTokenGOs[enemy.tokenId] = tokenGO;
                            Debug.Log($"[MapRenderer] UpdateTokenPositions: created enemy token GO (tokenId={enemy.tokenId}, enemy={enemy.definition.enemyName})");
                        }

                        tokenGO.transform.position = pos;
                        tokenGO.SetActive(true);
                    }
                }

                // Hide enemy tokens on non-visible nodes
                foreach (var enemy in enemyTokens)
                {
                    if (enemy.isDefeated) continue;
                    MapNode node = graph.GetNode(enemy.currentNodeId);
                    if (node != null && node.fogState != FogState.Visible)
                    {
                        if (enemyTokenGOs.TryGetValue(enemy.tokenId, out GameObject go) && go != null)
                            go.SetActive(false);
                    }
                }
            }

            // Remove stale enemy tokens
            var staleEnemyIds = new List<int>();
            foreach (var kvp in enemyTokenGOs)
            {
                if (!activeEnemyIds.Contains(kvp.Key))
                {
                    if (kvp.Value != null) kvp.Value.SetActive(false);
                    staleEnemyIds.Add(kvp.Key);
                }
            }
            foreach (int id in staleEnemyIds)
            {
                if (enemyTokenGOs[id] != null) Destroy(enemyTokenGOs[id]);
                enemyTokenGOs.Remove(id);
                Debug.Log($"[MapRenderer] UpdateTokenPositions: removed stale enemy token (tokenId={id})");
            }

            Debug.Log($"[MapRenderer] UpdateTokenPositions: complete (activeHeroes={activeHeroIds.Count}, activeEnemies={activeEnemyIds.Count})");
        }

        /// <summary>
        /// Highlights a path between nodes using colored line segments.
        /// </summary>
        public void HighlightPath(List<int> nodeIds)
        {
            Debug.Log($"[MapRenderer] HighlightPath: highlighting path with {nodeIds?.Count ?? 0} nodes");

            ClearHighlight();

            if (nodeIds == null || nodeIds.Count < 2)
            {
                Debug.Log("[MapRenderer] HighlightPath: path too short, nothing to highlight");
                return;
            }

            for (int i = 0; i < nodeIds.Count - 1; i++)
            {
                Vector3 from = GetNodeWorldPosition(nodeIds[i]);
                Vector3 to = GetNodeWorldPosition(nodeIds[i + 1]);

                var lineGO = new GameObject($"PathHighlight_{i}");
                lineGO.transform.SetParent(transform, false);
                var lr = lineGO.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.SetPosition(0, from);
                lr.SetPosition(1, to);
                lr.startWidth = HighlightWidth;
                lr.endWidth = HighlightWidth;
                lr.startColor = Color.yellow;
                lr.endColor = Color.yellow;
                lr.sortingOrder = SortUI;
                lr.material = new Material(Shader.Find("Sprites/Default"));

                highlightObjects.Add(lineGO);
                Debug.Log($"[MapRenderer] HighlightPath: segment {i} from nodeId={nodeIds[i]} to nodeId={nodeIds[i + 1]}");
            }
        }

        /// <summary>
        /// Removes all path highlight visuals.
        /// </summary>
        public void ClearHighlight()
        {
            Debug.Log($"[MapRenderer] ClearHighlight: removing {highlightObjects.Count} highlight objects");

            foreach (var go in highlightObjects)
            {
                if (go != null) Destroy(go);
            }
            highlightObjects.Clear();
        }

        /// <summary>
        /// Returns the world position of a node by its ID.
        /// </summary>
        public Vector3 GetNodeWorldPosition(int nodeId)
        {
            if (graph == null)
            {
                Debug.LogWarning($"[MapRenderer] GetNodeWorldPosition: graph is null (nodeId={nodeId})");
                return Vector3.zero;
            }

            MapNode node = graph.GetNode(nodeId);
            if (node == null)
            {
                Debug.LogWarning($"[MapRenderer] GetNodeWorldPosition: node not found (nodeId={nodeId})");
                return Vector3.zero;
            }

            return new Vector3(node.worldPosition.x, node.worldPosition.y, 0f);
        }

        // ─── Private Helpers ─────────────────────────────────────────────

        private void CreateNodeVisual(MapNode node)
        {
            Debug.Log($"[MapRenderer] CreateNodeVisual: creating node (nodeId={node.nodeId}, zone={node.zone}, pos={node.worldPosition})");

            // Root GO
            var nodeGO = new GameObject($"Node_{node.nodeId}");
            nodeGO.transform.SetParent(transform, false);
            nodeGO.transform.position = new Vector3(node.worldPosition.x, node.worldPosition.y, 0f);

            // Base circle sprite
            var baseSR = nodeGO.AddComponent<SpriteRenderer>();
            baseSR.sprite = GetWhiteSprite();
            baseSR.color = GetNodeColor(node);
            baseSR.sortingOrder = SortNode;
            // Colony nodes slightly smaller to differentiate from game nodes
            float scale = node.IsColonyNode ? NodeRadius * 1.6f : NodeRadius * 2f;
            nodeGO.transform.localScale = new Vector3(scale, scale, 1f);

            nodeSprites[node.nodeId] = baseSR;

            // Fog overlay (child GO)
            var fogGO = new GameObject("FogOverlay");
            fogGO.transform.SetParent(nodeGO.transform, false);
            fogGO.transform.localPosition = Vector3.zero;
            fogGO.transform.localScale = Vector3.one * 1.05f; // slightly larger than node
            var fogSR = fogGO.AddComponent<SpriteRenderer>();
            fogSR.sprite = GetWhiteSprite();
            fogSR.color = FogHiddenColor;
            fogSR.sortingOrder = SortFog;

            fogOverlays[node.nodeId] = fogSR;

            // Store GO reference
            nodeGameObjects[node.nodeId] = nodeGO;

            // Create initial resource indicators
            UpdateResourceIndicators(node);

            Debug.Log($"[MapRenderer] CreateNodeVisual: created (nodeId={node.nodeId}, color={baseSR.color})");
        }

        private void CreateEdgeVisual(int nodeA, int nodeB)
        {
            Vector3 posA = GetNodeWorldPosition(nodeA);
            Vector3 posB = GetNodeWorldPosition(nodeB);

            var edgeGO = new GameObject($"Edge_{nodeA}_{nodeB}");
            edgeGO.transform.SetParent(transform, false);

            var lr = edgeGO.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, posA);
            lr.SetPosition(1, posB);
            lr.startWidth = EdgeWidth;
            lr.endWidth = EdgeWidth;
            lr.startColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            lr.endColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            lr.sortingOrder = SortEdge;
            lr.material = new Material(Shader.Find("Sprites/Default"));

            edgeObjects.Add(edgeGO);
            Debug.Log($"[MapRenderer] CreateEdgeVisual: edge (nodeA={nodeA}, nodeB={nodeB})");
        }

        private GameObject CreateTokenVisual(string name, float radius, Color color, int sortingOrder)
        {
            var tokenGO = new GameObject(name);
            tokenGO.transform.SetParent(transform, false);

            var sr = tokenGO.AddComponent<SpriteRenderer>();
            sr.sprite = GetWhiteSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            tokenGO.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            Debug.Log($"[MapRenderer] CreateTokenVisual: created (name={name}, radius={radius}, color={color}, sortingOrder={sortingOrder})");
            return tokenGO;
        }

        /// <summary>
        /// Creates a hero token by instantiating the 3D model prefab from Resources/Card Models/{cardId:D3}.
        /// Equipment prefabs are attached as children. Falls back to colored sprite if model not found.
        /// </summary>
        /// <summary>
        /// Loads the 3D model prefab for a card ID. Unity's import pipeline handles materials/textures.
        /// </summary>
        /// <summary>
        /// Sets the model instance's local rotation so it lies flat on the XY map plane
        /// with front facing the camera. Rotation (89, 90, 90) determined empirically
        /// by manual adjustment in the Unity inspector.
        /// </summary>
        private static void OrientModelInstance(GameObject modelInstance)
        {
            modelInstance.transform.localRotation = Quaternion.identity;
            // Offset model up so its bottom sits at Z=0 (the map plane)
            // Get the model's bounds to find the bottom extent
            var renderers = modelInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                // Shift so bottom of bounds is at parent's Z=0
                float bottomZ = bounds.min.z;
                modelInstance.transform.localPosition = new Vector3(0, 0, -bottomZ);
            }
        }

        private static GameObject LoadCardModel(int cardId)
        {
            string path = $"Card Models/{cardId:D3}";
            return Resources.Load<GameObject>(path);
        }

        private GameObject CreateHeroTokenVisual(HeroToken hero)
        {
            var tokenGO = new GameObject($"HeroToken_{hero.tokenId}");
            tokenGO.transform.SetParent(transform, false);

            var modelPrefab = LoadCardModel(hero.cardDef.cardId);
            if (modelPrefab != null)
            {
                var modelInstance = Object.Instantiate(modelPrefab, tokenGO.transform);
                modelInstance.name = "Model";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localScale = Vector3.one * TokenRadius * 2f;
                // Flat on XY plane, front toward camera: compose baked rotation with Z180 + facing
                OrientModelInstance(modelInstance);

                Debug.Log($"[MapRenderer] CreateHeroTokenVisual: instantiated model for hero={hero.cardDef.cardName} " +
                          $"(cardId={hero.cardDef.cardId}, euler={modelInstance.transform.eulerAngles}, " +
                          $"forward={modelInstance.transform.forward}, up={modelInstance.transform.up})");
            }
            else
            {
                var sr = tokenGO.AddComponent<SpriteRenderer>();
                sr.sprite = GetWhiteSprite();
                sr.color = GetHeroColor(hero);
                sr.sortingOrder = SortToken;
                tokenGO.transform.localScale = new Vector3(TokenRadius * 2f, TokenRadius * 2f, 1f);
                Debug.LogWarning($"[MapRenderer] CreateHeroTokenVisual: no model for cardId={hero.cardDef.cardId}, using fallback sprite");
            }

            // Equipment models as children
            CardDefinitionSO[] equips = { hero.offensiveEquipment, hero.defensiveEquipment, hero.utilityEquipment };
            float[] eqOffsetX = { -0.4f, 0.4f, 0f };
            float[] eqOffsetY = { -0.4f, -0.4f, 0.4f };
            int eqCount = 0;

            for (int e = 0; e < equips.Length; e++)
            {
                if (equips[e] == null) continue;

                var eqPrefab = LoadCardModel(equips[e].cardId);
                if (eqPrefab != null)
                {
                    var eqInstance = Object.Instantiate(eqPrefab, tokenGO.transform);
                    eqInstance.name = $"Equipment_{equips[e].cardName}";
                    eqInstance.transform.localPosition = new Vector3(eqOffsetX[e], eqOffsetY[e], 0f);
                    eqInstance.transform.localScale = Vector3.one * TokenRadius * 0.8f;
                    Debug.Log($"[MapRenderer] CreateHeroTokenVisual: equipment model for {equips[e].cardName} (cardId={equips[e].cardId})");
                }
                eqCount++;
            }

            Debug.Log($"[MapRenderer] CreateHeroTokenVisual: hero={hero.cardDef.cardName}, tokenId={hero.tokenId}, equipment={eqCount}");
            return tokenGO;
        }

        // No material upgrade needed — FBX models have baked textures that Unity imports correctly.

        private void UpdateNodeColor(MapNode node)
        {
            if (nodeSprites.TryGetValue(node.nodeId, out SpriteRenderer sr))
            {
                Color newColor = GetNodeColor(node);
                sr.color = newColor;
            }
        }

        private void UpdateResourceIndicators(MapNode node)
        {
            // Clear existing indicators
            if (resourceIndicators.TryGetValue(node.nodeId, out List<GameObject> existing))
            {
                foreach (var go in existing)
                {
                    if (go != null) Destroy(go);
                }
                existing.Clear();
            }
            else
            {
                resourceIndicators[node.nodeId] = new List<GameObject>();
            }

            int totalResources = node.TotalResources();
            if (totalResources <= 0) return;

            if (!nodeGameObjects.TryGetValue(node.nodeId, out GameObject nodeGO)) return;

            // Place small dots below the node
            int dotCount = Mathf.Min(totalResources, 5); // cap visual dots at 5
            float dotSpacing = ResourceDotRadius * 3f;
            float startX = -(dotCount - 1) * dotSpacing * 0.5f;

            int dotIndex = 0;
            foreach (var kvp in node.resources)
            {
                if (dotIndex >= dotCount) break;
                for (int i = 0; i < kvp.Value && dotIndex < dotCount; i++, dotIndex++)
                {
                    var dotGO = new GameObject($"ResourceDot_{node.nodeId}_{dotIndex}");
                    dotGO.transform.SetParent(nodeGO.transform, false);
                    // Position below the node in local space (node is scaled so we use unscaled offset)
                    dotGO.transform.localPosition = new Vector3(
                        (startX + dotIndex * dotSpacing) / (NodeRadius * 2f),
                        -0.7f,
                        0f
                    );
                    dotGO.transform.localScale = new Vector3(
                        ResourceDotRadius * 2f / (NodeRadius * 2f),
                        ResourceDotRadius * 2f / (NodeRadius * 2f),
                        1f
                    );

                    var sr = dotGO.AddComponent<SpriteRenderer>();
                    sr.sprite = GetWhiteSprite();
                    sr.color = GetResourceColor(kvp.Key);
                    sr.sortingOrder = SortToken;

                    resourceIndicators[node.nodeId].Add(dotGO);
                }
            }

            Debug.Log($"[MapRenderer] UpdateResourceIndicators: nodeId={node.nodeId}, totalResources={totalResources}, dotsCreated={dotIndex}");
        }

        private Color GetZoneColor(NodeType zone)
        {
            return zone switch
            {
                NodeType.Wilderness => WildernessColor,
                NodeType.Farmland => FarmlandColor,
                NodeType.Town => TownColor,
                NodeType.Colony => ColonyColor,
                NodeType.PiedPiper => PiedPiperColor,
                _ => DefaultNodeColor
            };
        }

        /// <summary>
        /// Highlights specific nodes with a given color (e.g., valid colony card placement targets).
        /// </summary>
        public void HighlightNodes(List<int> nodeIds, Color color)
        {
            foreach (int id in nodeIds)
            {
                if (nodeSprites.TryGetValue(id, out SpriteRenderer sr))
                    sr.color = color;
            }
            Debug.Log($"[MapRenderer] HighlightNodes: highlighted {nodeIds.Count} nodes");
        }

        /// <summary>
        /// Clears highlights on specified nodes, restoring their original color.
        /// </summary>
        public void ClearNodeHighlights(List<int> nodeIds)
        {
            if (graph == null) return;
            foreach (int id in nodeIds)
            {
                var node = graph.GetNode(id);
                if (node != null && nodeSprites.TryGetValue(id, out SpriteRenderer sr))
                    sr.color = GetNodeColor(node);
            }
            Debug.Log($"[MapRenderer] ClearNodeHighlights: restored {nodeIds.Count} nodes");
        }

        /// <summary>
        /// Creates or updates 3D models for colony nodes that have cards placed on them.
        /// </summary>
        public void UpdateColonyCardModels()
        {
            if (graph == null) return;

            Debug.Log("[MapRenderer] UpdateColonyCardModels: refreshing colony card visuals");
            int created = 0;

            foreach (var node in graph.GetColonyNodes())
            {
                if (node.HasColonyCard)
                {
                    // Already has a model? Skip.
                    if (colonyCardModels.ContainsKey(node.nodeId)) continue;

                    // Create model for this colony card
                    var modelPrefab = LoadCardModel(node.placedColonyCard.cardId);
                    if (modelPrefab != null)
                    {
                        var modelGO = new GameObject($"ColonyCard_{node.nodeId}_{node.placedColonyCard.cardName}");
                        modelGO.transform.SetParent(transform, false);
                        modelGO.transform.position = new Vector3(node.worldPosition.x, node.worldPosition.y, 0);

                        var modelInstance = Object.Instantiate(modelPrefab, modelGO.transform);
                        modelInstance.name = "Model";
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localScale = Vector3.one * TokenRadius * 1.8f;
                        OrientModelInstance(modelInstance);

                        colonyCardModels[node.nodeId] = modelGO;
                        created++;

                        Debug.Log($"[MapRenderer] UpdateColonyCardModels: placed model for '{node.placedColonyCard.cardName}' on node {node.nodeId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MapRenderer] UpdateColonyCardModels: no model found for cardId={node.placedColonyCard.cardId}");
                    }

                    // Update node color to occupied
                    UpdateNodeColor(node);
                }
                else
                {
                    // Remove model if card was removed (undo, etc.)
                    if (colonyCardModels.TryGetValue(node.nodeId, out var existingGO))
                    {
                        Destroy(existingGO);
                        colonyCardModels.Remove(node.nodeId);
                    }
                }
            }

            if (created > 0)
                Debug.Log($"[MapRenderer] UpdateColonyCardModels: created {created} new colony card models");
        }

        private Color GetNodeColor(MapNode node)
        {
            if (node.IsColonyNode)
                return node.HasColonyCard ? ColonyColor : ColonyEmptyColor;
            return GetZoneColor(node.zone);
        }

        private Color GetResourceColor(ResourceType type)
        {
            return type switch
            {
                ResourceType.Food => new Color(0.4f, 0.8f, 0.2f),
                ResourceType.Materials => new Color(0.6f, 0.4f, 0.2f),
                ResourceType.Currency => new Color(1f, 0.85f, 0.1f),
                _ => Color.white
            };
        }

        private Color GetHeroColor(HeroToken hero)
        {
            return hero.cardDef.heroRole switch
            {
                HeroRole.Recon => new Color(0.3f, 0.7f, 0.9f),
                HeroRole.Ranged => new Color(0.9f, 0.6f, 0.2f),
                HeroRole.Fast => new Color(0.9f, 0.9f, 0.3f),
                HeroRole.Melee => new Color(0.8f, 0.2f, 0.2f),
                HeroRole.Tank => new Color(0.5f, 0.5f, 0.7f),
                HeroRole.Gather => new Color(0.4f, 0.8f, 0.4f),
                HeroRole.Support => new Color(0.9f, 0.5f, 0.8f),
                HeroRole.Leader => new Color(1f, 0.85f, 0.1f),
                _ => Color.cyan
            };
        }

        private void ClearAll()
        {
            Debug.Log("[MapRenderer] ClearAll: destroying all visual objects");

            foreach (var kvp in nodeGameObjects)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            nodeGameObjects.Clear();
            nodeSprites.Clear();
            fogOverlays.Clear();

            foreach (var kvp in resourceIndicators)
            {
                foreach (var go in kvp.Value)
                {
                    if (go != null) Destroy(go);
                }
            }
            resourceIndicators.Clear();

            foreach (var go in edgeObjects)
            {
                if (go != null) Destroy(go);
            }
            edgeObjects.Clear();

            ClearHighlight();

            foreach (var kvp in heroTokenGOs)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            heroTokenGOs.Clear();

            foreach (var kvp in enemyTokenGOs)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            enemyTokenGOs.Clear();

            foreach (var kvp in colonyCardModels)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            colonyCardModels.Clear();

            Debug.Log("[MapRenderer] ClearAll: complete");
        }

        private void OnDestroy()
        {
            Debug.Log("[MapRenderer] OnDestroy: cleaning up");
            ClearAll();
        }
    }
}
