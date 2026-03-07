using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Map;
using Scurry.Encounter;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class MapUI : MonoBehaviour
    {
        private IMapManager mapManager;

        private GameObject mapPanel;
        private RectTransform contentRect;
        private readonly List<GameObject> nodeObjects = new List<GameObject>();
        private readonly List<GameObject> lineObjects = new List<GameObject>();
        private bool mapVisible;

        private void OnEnable()
        {
            Debug.Log("[MapUI] OnEnable: subscribing to events");
            EventBus.OnMapReady += ShowMap;
            EventBus.OnMapNodeComplete += RefreshMap;
            EventBus.OnLevelComplete += HideMap;
            EventBus.OnEncounterComplete += OnEncounterDone;
        }

        private void OnDisable()
        {
            Debug.Log("[MapUI] OnDisable: unsubscribing from events");
            EventBus.OnMapReady -= ShowMap;
            EventBus.OnMapNodeComplete -= RefreshMap;
            EventBus.OnLevelComplete -= HideMap;
            EventBus.OnEncounterComplete -= OnEncounterDone;
        }

        private void Awake()
        {
            BuildMapPanel();
        }

        private void Start()
        {
            mapManager = ServiceLocator.Get<IMapManager>();
            Debug.Log($"[MapUI] Start: mapManager={(mapManager != null ? "OK" : "NULL")}");
        }

        private void BuildMapPanel()
        {
            mapPanel = new GameObject("MapPanel", typeof(RectTransform), typeof(Image));
            mapPanel.transform.SetParent(transform, false);
            var panelRect = mapPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            mapPanel.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.95f);

            // Title
            var titleGO = new GameObject("MapTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(mapPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Map";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.9f, 0.3f);

            // Scrollable content area
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(mapPanel.transform, false);
            var scrollRect = scrollGO.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(20, 20);
            scrollRect.offsetMax = new Vector2(-20, -60);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(scrollGO.transform, false);
            contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 0);
            contentRect.pivot = new Vector2(0.5f, 0);
            contentRect.sizeDelta = new Vector2(0, 800);

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport mask
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var vpRect = viewportGO.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRect;
            contentGO.transform.SetParent(viewportGO.transform, false);

            mapPanel.SetActive(false);
            Debug.Log("[MapUI] BuildMapPanel: map panel created (hidden)");
        }

        private void ShowMap()
        {
            Debug.Log("[MapUI] ShowMap: rendering map");
            ClearMapNodes();
            mapPanel.SetActive(true);
            mapVisible = true;
            RenderMap();
        }

        private void RefreshMap()
        {
            if (!mapVisible) return;
            Debug.Log("[MapUI] RefreshMap: updating map display");
            ClearMapNodes();
            RenderMap();
        }

        private void HideMap()
        {
            Debug.Log("[MapUI] HideMap: hiding map panel");
            mapPanel.SetActive(false);
            mapVisible = false;
        }

        private void OnEncounterDone(EncounterResult result)
        {
            if (mapVisible)
                RefreshMap();
        }

        private void ClearMapNodes()
        {
            foreach (var obj in nodeObjects) Destroy(obj);
            foreach (var obj in lineObjects) Destroy(obj);
            nodeObjects.Clear();
            lineObjects.Clear();
        }

        private void RenderMap()
        {
            var map = mapManager.Map;
            if (map == null || map.Count == 0) return;

            float rowSpacing = 80f;
            float totalHeight = map.Count * rowSpacing + 40;
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);

            float panelWidth = contentRect.rect.width > 0 ? contentRect.rect.width : 600f;

            var available = mapManager.GetAvailableNodes();
            var availableSet = new HashSet<MapNode>(available);

            // Store node positions for drawing lines
            var nodePositions = new Dictionary<MapNode, Vector2>();

            for (int row = 0; row < map.Count; row++)
            {
                var rowNodes = map[row];
                float nodeSpacing = panelWidth / (rowNodes.Count + 1);

                for (int col = 0; col < rowNodes.Count; col++)
                {
                    var node = rowNodes[col];
                    float x = nodeSpacing * (col + 1) - panelWidth / 2f;
                    float y = row * rowSpacing + 20;

                    nodePositions[node] = new Vector2(x, y);

                    // Create node button
                    var nodeGO = new GameObject($"Node_{row}_{col}", typeof(RectTransform), typeof(Image), typeof(Button));
                    nodeGO.transform.SetParent(contentRect, false);
                    var nodeRect = nodeGO.GetComponent<RectTransform>();
                    nodeRect.anchorMin = new Vector2(0.5f, 0);
                    nodeRect.anchorMax = new Vector2(0.5f, 0);
                    nodeRect.pivot = new Vector2(0.5f, 0.5f);
                    nodeRect.anchoredPosition = new Vector2(x, y);
                    nodeRect.sizeDelta = new Vector2(50, 50);

                    // Color by state
                    Color nodeColor;
                    if (node.visited)
                        nodeColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // dimmed
                    else if (availableSet.Contains(node))
                        nodeColor = GetNodeColor(node.nodeType); // bright
                    else
                        nodeColor = GetNodeColor(node.nodeType) * 0.5f; // muted

                    nodeGO.GetComponent<Image>().color = nodeColor;

                    // Button interaction
                    var btn = nodeGO.GetComponent<Button>();
                    if (availableSet.Contains(node) && !node.visited)
                    {
                        MapNode capturedNode = node;
                        btn.onClick.AddListener(() => OnNodeClicked(capturedNode));
                        btn.interactable = true;
                    }
                    else
                    {
                        btn.interactable = false;
                    }

                    // Label
                    var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    labelGO.transform.SetParent(nodeGO.transform, false);
                    var labelRect = labelGO.GetComponent<RectTransform>();
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.sizeDelta = Vector2.zero;
                    var labelTmp = labelGO.GetComponent<TextMeshProUGUI>();
                    labelTmp.text = GetNodeIcon(node.nodeType);
                    labelTmp.fontSize = 16;
                    labelTmp.alignment = TextAlignmentOptions.Center;
                    labelTmp.color = Color.white;

                    nodeObjects.Add(nodeGO);
                }
            }

            // Draw connection lines
            for (int row = 0; row < map.Count - 1; row++)
            {
                foreach (var node in map[row])
                {
                    if (!nodePositions.ContainsKey(node)) continue;
                    Vector2 fromPos = nodePositions[node];

                    foreach (int connIdx in node.connectedNodeIndices)
                    {
                        if (connIdx >= 0 && connIdx < map[row + 1].Count)
                        {
                            var targetNode = map[row + 1][connIdx];
                            if (!nodePositions.ContainsKey(targetNode)) continue;
                            Vector2 toPos = nodePositions[targetNode];

                            DrawLine(fromPos, toPos, node.visited ? Color.gray : new Color(0.5f, 0.5f, 0.5f, 0.3f));
                        }
                    }
                }
            }

            Debug.Log($"[MapUI] RenderMap: rendered {nodeObjects.Count} nodes, {lineObjects.Count} lines");
        }

        private void DrawLine(Vector2 from, Vector2 to, Color color)
        {
            var lineGO = new GameObject("Line", typeof(RectTransform), typeof(Image));
            lineGO.transform.SetParent(contentRect, false);

            var lineRect = lineGO.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0);
            lineRect.anchorMax = new Vector2(0.5f, 0);
            lineRect.pivot = new Vector2(0.5f, 0);

            Vector2 dir = to - from;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            lineRect.anchoredPosition = from;
            lineRect.sizeDelta = new Vector2(distance, 2);
            lineRect.localEulerAngles = new Vector3(0, 0, angle);

            lineGO.GetComponent<Image>().color = color;
            lineObjects.Add(lineGO);
        }

        private void OnNodeClicked(MapNode node)
        {
            Debug.Log($"[MapUI] OnNodeClicked: {node}");
            mapManager.SelectNode(node);
            // Map will hide when encounter starts, show again when encounter completes
            if (node.nodeType == NodeType.ResourceEncounter || node.nodeType == NodeType.EliteEncounter || node.nodeType == NodeType.Boss)
            {
                HideMap();
            }
        }

        private Color GetNodeColor(NodeType type)
        {
            return type switch
            {
                NodeType.ResourceEncounter => new Color(0.3f, 0.6f, 0.3f),
                NodeType.EliteEncounter => new Color(0.8f, 0.5f, 0.2f),
                NodeType.Boss => new Color(0.8f, 0.2f, 0.2f),
                NodeType.Shop => new Color(0.8f, 0.7f, 0.2f),
                NodeType.HealingShrine => new Color(0.2f, 0.7f, 0.2f),
                NodeType.UpgradeShrine => new Color(0.4f, 0.4f, 0.8f),
                NodeType.CardDraft => new Color(0.6f, 0.3f, 0.7f),
                NodeType.Event => new Color(0.5f, 0.5f, 0.5f),
                NodeType.RestSite => new Color(0.2f, 0.5f, 0.7f),
                _ => Color.gray
            };
        }

        private string GetNodeIcon(NodeType type)
        {
            return type switch
            {
                NodeType.ResourceEncounter => "RES",
                NodeType.EliteEncounter => "ELT",
                NodeType.Boss => "BOSS",
                NodeType.Shop => "SHOP",
                NodeType.HealingShrine => "HEAL",
                NodeType.UpgradeShrine => "UP",
                NodeType.CardDraft => "CARD",
                NodeType.Event => "EVT",
                NodeType.RestSite => "REST",
                _ => "?"
            };
        }
    }
}
