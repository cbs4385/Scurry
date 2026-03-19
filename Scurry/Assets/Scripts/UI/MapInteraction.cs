using UnityEngine;
using UnityEngine.InputSystem;
using Scurry.Core;
using Scurry.Data;
using Scurry.Map;
using Scurry.Interfaces;

namespace Scurry.UI
{
    /// <summary>
    /// Handles map click and hover interactions. Routes input based on the current game phase.
    /// Uses distance-based detection against node world positions (no colliders needed).
    /// </summary>
    public class MapInteraction : MonoBehaviour
    {
        private const float NodeClickRadius = 0.6f;
        private const float HoverHysteresis = 0.1f;
        private const float ZoomStep = 1.5f;
        private const float MinZoom = 3f;
        private const float MaxZoom = 25f;
        private const float KeyPanSpeed = 5f;

        private MapGraph graph;
        private MapRenderer mapRenderer;
        private ITurnManager turnManager;

        // Pan state
        private bool isPanning;
        private Vector3 panStartScreenPos;
        private Vector3 panStartCamPos;

        // Camera bounds (computed from map extents)
        private Vector2 boundsMin;
        private Vector2 boundsMax;
        private bool boundsInitialized;

        /// <summary>Currently selected node ID, or -1 if none.</summary>
        public int SelectedNodeId { get; private set; } = -1;

        /// <summary>Currently selected hero token for deployment/retargeting, or null.</summary>
        public HeroToken SelectedHeroToken { get; set; }

        private int hoveredNodeId = -1;
        private float hoverStartTime;

        /// <summary>
        /// Initializes the interaction handler with references to the map graph and renderer.
        /// </summary>
        public void Initialize(MapGraph mapGraph, MapRenderer renderer)
        {
            Debug.Log($"[MapInteraction] Initialize: graph={mapGraph}, renderer={renderer}");
            graph = mapGraph;
            mapRenderer = renderer;

            turnManager = ServiceLocator.Get<ITurnManager>();

            // Compute camera bounds from map node extents with padding
            ComputeCameraBounds();

            Debug.Log($"[MapInteraction] Initialize: turnManager={(turnManager != null ? "OK" : "NULL")}, " +
                      $"bounds=({boundsMin.x:F1},{boundsMin.y:F1})-({boundsMax.x:F1},{boundsMax.y:F1})");
        }

        private void Update()
        {
            if (graph == null || mapRenderer == null) return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            Camera cam = Camera.main;
            if (cam == null) return;

            // --- Scroll wheel zoom ---
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.1f)
                {
                    if (cam.orthographic)
                    {
                        float step = scroll > 0 ? -ZoomStep : ZoomStep;
                        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + step, MinZoom, MaxZoom);
                    }
                    else
                    {
                        // Move camera along its forward axis toward/away from the map
                        float zoomSpeed = Mathf.Sign(scroll) * 3f;
                        Vector3 newPos = cam.transform.position + cam.transform.forward * zoomSpeed;
                        // Clamp: don't go below Z=2 (too close) or above Z=60 (too far)
                        if (newPos.z > 2f && newPos.z < 60f)
                            cam.transform.position = newPos;
                    }
                }
            }

            // --- Right-click or middle-click drag to pan ---
            if (mouse != null)
            {
                bool panButton = mouse.rightButton.isPressed || mouse.middleButton.isPressed;
                Vector2 mouseScreenPos = mouse.position.ReadValue();

                if (panButton)
                {
                    if (!isPanning)
                    {
                        isPanning = true;
                        panStartScreenPos = mouseScreenPos;
                        panStartCamPos = cam.transform.position;
                    }
                    else
                    {
                        // Pan using screen pixel delta mapped to camera right/up axes
                        Vector2 screenDelta = (Vector2)mouseScreenPos - (Vector2)panStartScreenPos;
                        float panScale = 0.01f * (cam.orthographic ? cam.orthographicSize / 10f : cam.fieldOfView / 40f);
                        Vector3 move = -cam.transform.right * screenDelta.x * panScale
                                     - cam.transform.up * screenDelta.y * panScale;
                        cam.transform.position = panStartCamPos + move;
                        ClampCameraPosition(cam);
                    }
                }
                else
                {
                    isPanning = false;
                }
            }

            // --- Node interaction (left click) ---
            if (mouse != null)
            {
                Vector2 mouseScreenPos = mouse.position.ReadValue();

                // --- Node interaction ---
                // Raycast to XY plane (Z=0) for perspective camera support
                Vector2 worldPos2D = ScreenToXYPlane(cam, mouseScreenPos);

                int closestNodeId = FindClosestNode(worldPos2D);
                HandleHover(closestNodeId, mouseScreenPos);

                if (mouse.leftButton.wasPressedThisFrame && !isPanning)
                {
                    HandleClick(closestNodeId);
                }
            }
        }

        private int FindClosestNode(Vector2 worldPos)
        {
            if (graph == null) return -1;

            int closest = -1;
            float closestDist = NodeClickRadius;

            foreach (var node in graph.GetAllNodes())
            {
                float dist = Vector2.Distance(worldPos, node.worldPosition);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = node.nodeId;
                }
            }

            return closest;
        }

        private void HandleHover(int nodeId, Vector2 screenPos)
        {
            if (nodeId != hoveredNodeId)
            {
                // Unhover previous
                if (hoveredNodeId >= 0)
                {
                    OnNodeUnhovered();
                }

                hoveredNodeId = nodeId;

                // Hover new
                if (hoveredNodeId >= 0)
                {
                    hoverStartTime = Time.time;
                    OnNodeHovered(hoveredNodeId, screenPos);
                }
            }
        }

        private void HandleClick(int nodeId)
        {
            if (nodeId < 0)
            {
                Debug.Log("[MapInteraction] HandleClick: clicked empty space, deselecting");
                SelectedNodeId = -1;
                SelectedHeroToken = null;
                return;
            }

            Debug.Log($"[MapInteraction] HandleClick: clicked nodeId={nodeId}");
            OnNodeClicked(nodeId);
        }

        /// <summary>
        /// Processes a node click based on the current game phase.
        /// </summary>
        public void OnNodeClicked(int nodeId)
        {
            Debug.Log($"[MapInteraction] OnNodeClicked: nodeId={nodeId}, previousSelection={SelectedNodeId}");

            MapNode node = graph?.GetNode(nodeId);
            if (node == null)
            {
                Debug.LogWarning($"[MapInteraction] OnNodeClicked: node not found (nodeId={nodeId})");
                return;
            }

            // Lazy resolve turnManager (may be null after scene reload)
            if (turnManager == null)
                turnManager = ServiceLocator.Get<ITurnManager>();
            if (turnManager == null)
                turnManager = TurnManager.Instance;

            GamePhase currentPhase = turnManager != null ? turnManager.CurrentPhase : GamePhase.Deploy;
            Debug.Log($"[MapInteraction] OnNodeClicked: phase={currentPhase}, nodeId={nodeId}, zone={node.zone}, fog={node.fogState}, turnMgr={turnManager != null}");

            SelectedNodeId = nodeId;

            // Show colony card info in HUD when clicking a colony node with a placed card
            if (node.IsColonyNode && node.HasColonyCard)
            {
                var hud = Object.FindAnyObjectByType<HUDManager>();
                if (hud != null)
                {
                    hud.ShowPlacedColonyCardInfo(node.placedColonyCard);
                    Debug.Log($"[MapInteraction] OnNodeClicked: showing colony card info for '{node.placedColonyCard.cardName}' on node {nodeId}");
                }
            }

            // Colony card placement during ColonyDeploy phase
            if (currentPhase == GamePhase.ColonyDeploy && node.IsColonyNode && !node.HasColonyCard)
            {
                HandleColonyCardPlacement(nodeId, node);
                return;
            }

            // Hero targeting works in ALL phases (during Continue pauses on GameMap)
            HandleHeroMoveClick(nodeId, node);
        }

        /// <summary>
        /// Shows a tooltip with node information when hovering.
        /// </summary>
        public void OnNodeHovered(int nodeId, Vector2 screenPos)
        {
            Debug.Log($"[MapInteraction] OnNodeHovered: nodeId={nodeId}");

            MapNode node = graph?.GetNode(nodeId);
            if (node == null)
            {
                Debug.LogWarning($"[MapInteraction] OnNodeHovered: node not found (nodeId={nodeId})");
                return;
            }

            // Only show info for non-hidden nodes
            if (node.fogState == FogState.Hidden)
            {
                Debug.Log($"[MapInteraction] OnNodeHovered: node is hidden, showing minimal info (nodeId={nodeId})");
                TooltipUI.Show("Unknown territory", screenPos);
                return;
            }

            string displayName = !string.IsNullOrEmpty(node.displayName) ? node.displayName : $"Node {node.nodeId}";
            string zoneText = node.zone.ToString();
            string fogText = node.fogState == FogState.Remembered ? " (remembered)" : "";

            string info = $"<b>{displayName}</b>\nZone: {zoneText}{fogText}";

            if (node.TotalResources() > 0)
            {
                info += $"\nResources: {node.TotalResources()}";
                foreach (var kvp in node.resources)
                {
                    if (kvp.Value > 0)
                        info += $"\n  {kvp.Key}: {kvp.Value}";
                }
            }

            if (node.heroTokenIds.Count > 0)
                info += $"\nHeroes: {node.heroTokenIds.Count}";

            if (node.fogState == FogState.Visible && node.enemyTokenIds.Count > 0)
                info += $"\nEnemies: {node.enemyTokenIds.Count}";

            Debug.Log($"[MapInteraction] OnNodeHovered: showing tooltip (nodeId={nodeId}, zone={zoneText})");
            TooltipUI.Show(info, screenPos);
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public void OnNodeUnhovered()
        {
            Debug.Log($"[MapInteraction] OnNodeUnhovered: hiding tooltip (previousNode={hoveredNodeId})");
            TooltipUI.Hide();
            hoveredNodeId = -1;
        }

        private void HandleDeployClick(int nodeId, MapNode node)
        {
            Debug.Log($"[MapInteraction] HandleDeployClick: nodeId={nodeId}, selectedHero={(SelectedHeroToken != null ? SelectedHeroToken.cardDef.cardName : "none")}");

            if (SelectedHeroToken != null)
            {
                // Assign target node for selected hero
                Debug.Log($"[MapInteraction] HandleDeployClick: assigning target nodeId={nodeId} for hero={SelectedHeroToken.cardDef.cardName}");
                EventBus.OnTargetAssigned?.Invoke(SelectedHeroToken, nodeId);
                SelectedHeroToken = null;
            }
            else
            {
                Debug.Log($"[MapInteraction] HandleDeployClick: no hero selected, selecting node only (nodeId={nodeId})");
            }
        }

        private void HandleCombatClick(int nodeId, MapNode node)
        {
            Debug.Log($"[MapInteraction] HandleCombatClick: nodeId={nodeId}, fog={node.fogState}");

            if (node.fogState != FogState.Visible)
            {
                Debug.Log($"[MapInteraction] HandleCombatClick: node not visible, ignoring (nodeId={nodeId})");
                return;
            }

            // During combat phase, clicking a combat node could show tactical card targeting
            if (node.heroTokenIds.Count > 0 && node.enemyTokenIds.Count > 0)
            {
                Debug.Log($"[MapInteraction] HandleCombatClick: combat node with heroes and enemies (nodeId={nodeId})");
                EventBus.OnCombatStarted?.Invoke(nodeId);
            }
            else
            {
                Debug.Log($"[MapInteraction] HandleCombatClick: no combat at this node (nodeId={nodeId}, heroes={node.heroTokenIds.Count}, enemies={node.enemyTokenIds.Count})");
            }
        }

        private void HandleHeroMoveClick(int nodeId, MapNode node)
        {
            Debug.Log($"[MapInteraction] HandleHeroMoveClick: nodeId={nodeId}, selectedHero={(SelectedHeroToken != null ? SelectedHeroToken.cardDef.cardName : "none")}");

            var tm = TurnManager.Instance;
            if (tm == null) return;

            // If we have a selected hero and click a different node — set target
            if (SelectedHeroToken != null && nodeId != SelectedHeroToken.currentNodeId)
            {
                // Check if destination is a neighbor (valid move)
                var currentNode = graph.GetNode(SelectedHeroToken.currentNodeId);
                if (currentNode != null && currentNode.neighborIds.Contains(nodeId))
                {
                    int heroSourceNode = SelectedHeroToken.currentNodeId;
                    string heroName = SelectedHeroToken.cardDef.cardName;
                    SelectedHeroToken.targetNodeId = nodeId;
                    Debug.Log($"[MapInteraction] HandleHeroMoveClick: set target for hero={heroName} to nodeId={nodeId}");

                    // Show path highlight
                    if (mapRenderer != null)
                    {
                        mapRenderer.HighlightPath(new System.Collections.Generic.List<int> { heroSourceNode, nodeId });
                    }

                    EventBus.OnNotification?.Invoke($"{heroName} targeting {node.displayName}", new Color(0.7f, 0.9f, 0.7f));

                    // Auto-select next untargeted hero on the same source node
                    SelectedHeroToken = null;
                    var sourceNode = graph.GetNode(heroSourceNode);
                    if (sourceNode != null)
                    {
                        foreach (var nextHero in tm.DeployedHeroes)
                        {
                            if (nextHero.currentNodeId == heroSourceNode && nextHero.IsAlive && nextHero.isDeployed
                                && (nextHero.targetNodeId < 0 || nextHero.targetNodeId == nextHero.currentNodeId))
                            {
                                SelectedHeroToken = nextHero;
                                EventBus.OnNotification?.Invoke(
                                    $"Auto-selected {nextHero.cardDef.cardName} — click adjacent node",
                                    new Color(0.7f, 0.85f, 1f));
                                Debug.Log($"[MapInteraction] HandleHeroMoveClick: auto-selected next hero={nextHero.cardDef.cardName} at node={heroSourceNode}");
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"[MapInteraction] HandleHeroMoveClick: nodeId={nodeId} is not adjacent to hero's node={SelectedHeroToken.currentNodeId}");
                    EventBus.OnNotification?.Invoke("Must select an adjacent node!", new Color(1f, 0.5f, 0.3f));
                }
                return;
            }

            // First click (or re-click same node): select/cycle UNTARGETED heroes on this node
            if (node.heroTokenIds.Count > 0)
            {
                // If we already have an untargeted hero selected on this node, keep it
                if (SelectedHeroToken != null && SelectedHeroToken.currentNodeId == nodeId
                    && (SelectedHeroToken.targetNodeId < 0 || SelectedHeroToken.targetNodeId == SelectedHeroToken.currentNodeId))
                {
                    Debug.Log($"[MapInteraction] HandleHeroMoveClick: keeping current selection {SelectedHeroToken.cardDef.cardName} (tokenId={SelectedHeroToken.tokenId}) — click adjacent node to set target");
                    EventBus.OnNotification?.Invoke(
                        $"{SelectedHeroToken.cardDef.cardName} selected — click adjacent node to move",
                        new Color(0.7f, 0.85f, 1f));
                    return;
                }

                // Find untargeted heroes on this node
                var untargeted = new System.Collections.Generic.List<HeroToken>();
                foreach (var hero in tm.DeployedHeroes)
                {
                    if (hero.currentNodeId == nodeId && hero.IsAlive && hero.isDeployed
                        && (hero.targetNodeId < 0 || hero.targetNodeId == hero.currentNodeId))
                        untargeted.Add(hero);
                }

                if (untargeted.Count > 0)
                {
                    // Select first untargeted hero (or cycle through untargeted only)
                    int startIdx = 0;
                    if (SelectedHeroToken != null && SelectedHeroToken.currentNodeId == nodeId)
                    {
                        for (int h = 0; h < untargeted.Count; h++)
                        {
                            if (untargeted[h].tokenId == SelectedHeroToken.tokenId)
                            {
                                startIdx = (h + 1) % untargeted.Count;
                                break;
                            }
                        }
                    }

                    SelectedHeroToken = untargeted[startIdx];
                    Debug.Log($"[MapInteraction] HandleHeroMoveClick: selected untargeted hero={SelectedHeroToken.cardDef.cardName} (tokenId={SelectedHeroToken.tokenId}, {startIdx + 1}/{untargeted.Count} untargeted) at nodeId={nodeId}");
                    EventBus.OnNotification?.Invoke(
                        $"Selected {SelectedHeroToken.cardDef.cardName} ({untargeted.Count} untargeted) — click adjacent node",
                        new Color(0.7f, 0.85f, 1f));
                    return;
                }

                // All heroes on this node already have targets — inform the player
                Debug.Log($"[MapInteraction] HandleHeroMoveClick: all heroes at node {nodeId} already have targets");
                EventBus.OnNotification?.Invoke("All heroes here already have targets set", new Color(0.9f, 0.9f, 0.6f));
                return;
            }

            // Click on empty node — deselect
            SelectedHeroToken = null;
            if (mapRenderer != null) mapRenderer.ClearHighlight();
            Debug.Log($"[MapInteraction] HandleHeroMoveClick: no hero at nodeId={nodeId}, deselected");
        }

        /// <summary>
        /// Projects a screen position onto the XY plane (Z=0). Works with both ortho and perspective cameras.
        /// </summary>
        private static Vector2 ScreenToXYPlane(Camera cam, Vector2 screenPos)
        {
            if (cam.orthographic)
            {
                Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
                return new Vector2(wp.x, wp.y);
            }

            // Perspective: cast ray from camera through screen point, intersect with Z=0 plane
            Ray ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0));
            if (Mathf.Abs(ray.direction.z) < 0.0001f)
                return new Vector2(ray.origin.x, ray.origin.y);

            float t = -ray.origin.z / ray.direction.z;
            if (t < 0) t = 0; // camera behind plane edge case
            Vector3 hit = ray.origin + ray.direction * t;
            return new Vector2(hit.x, hit.y);
        }

        private void HandleColonyCardPlacement(int nodeId, MapNode node)
        {
            Debug.Log($"[MapInteraction] HandleColonyCardPlacement: nodeId={nodeId}");

            // Get selected colony card from HUDManager
            var hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud == null || hud.SelectedColonyCard == null)
            {
                Debug.Log("[MapInteraction] HandleColonyCardPlacement: no colony card selected in HUD");
                EventBus.OnNotification?.Invoke("Select a colony card from the panel first", new Color(1f, 0.7f, 0.3f));
                return;
            }

            var card = hud.SelectedColonyCard;
            Debug.Log($"[MapInteraction] HandleColonyCardPlacement: placing '{card.cardName}' on node {nodeId}");

            var tm = TurnManager.Instance;
            if (tm != null)
            {
                tm.PlayerPlayColonyCard(card, nodeId);
                Debug.Log($"[MapInteraction] HandleColonyCardPlacement: queued colony card action (card={card.cardName}, node={nodeId})");

                // Refresh the colony card list to remove the placed card
                hud.RefreshColonyCardList();
            }
            else
            {
                Debug.LogWarning("[MapInteraction] HandleColonyCardPlacement: turnManager is null");
            }
        }

        private void ComputeCameraBounds()
        {
            if (graph == null) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var node in graph.GetAllNodes())
            {
                if (node.worldPosition.x < minX) minX = node.worldPosition.x;
                if (node.worldPosition.x > maxX) maxX = node.worldPosition.x;
                if (node.worldPosition.y < minY) minY = node.worldPosition.y;
                if (node.worldPosition.y > maxY) maxY = node.worldPosition.y;
            }

            float padX = (maxX - minX) * 0.3f;
            float padY = (maxY - minY) * 0.3f;
            boundsMin = new Vector2(minX - padX, minY - padY);
            boundsMax = new Vector2(maxX + padX, maxY + padY);
            boundsInitialized = true;

            Debug.Log($"[MapInteraction] ComputeCameraBounds: bounds=({boundsMin.x:F1},{boundsMin.y:F1})-({boundsMax.x:F1},{boundsMax.y:F1})");
        }

        private void ClampCameraPosition(Camera cam)
        {
            if (!boundsInitialized || cam == null) return;
            var pos = cam.transform.position;
            pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
            pos.y = Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y);
            cam.transform.position = pos;
        }

        private void OnDestroy()
        {
            Debug.Log("[MapInteraction] OnDestroy: cleaning up");
            graph = null;
            mapRenderer = null;
        }
    }
}
