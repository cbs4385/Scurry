using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Scurry.Core;
using Scurry.Data;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class ScrapbookUI : MonoBehaviour
    {
        private IMetaProgressionManager metaProgression;
        private GameObject panel;
        private GameObject contentParent;
        private Text titleText;
        private Text completionText;
        private string currentTab = "cards";

        private void Start()
        {
            metaProgression = ServiceLocator.Get<IMetaProgressionManager>();
            Debug.Log($"[ScrapbookUI] Start: metaProgression={(metaProgression != null ? "OK" : "NULL")}");
        }

        public void Open()
        {
            Debug.Log("[ScrapbookUI] Open: building scrapbook UI");
            if (panel != null) Close();

            BuildPanel();
            ShowTab("cards");
        }

        public void Close()
        {
            Debug.Log("[ScrapbookUI] Close: closing scrapbook");
            if (panel != null) Destroy(panel);
            panel = null;
        }

        private void BuildPanel()
        {
            // Create main panel
            panel = new GameObject("ScrapbookPanel");
            panel.transform.SetParent(transform, false);

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
                panel.transform.SetParent(canvas.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(40, 40);
            panelRect.offsetMax = new Vector2(-40, -40);

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            var titleObj = CreateText(panel.transform, "The Scrapbook", 28, TextAnchor.UpperCenter);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleText = titleObj.GetComponent<Text>();

            // Completion text
            int completion = metaProgression != null ? metaProgression.ScrapbookCompletion : 0;
            var compObj = CreateText(panel.transform, $"Completion: {completion}%", 18, TextAnchor.UpperCenter);
            var compRect = compObj.GetComponent<RectTransform>();
            compRect.anchorMin = new Vector2(0, 0.85f);
            compRect.anchorMax = new Vector2(1, 0.9f);
            compRect.offsetMin = Vector2.zero;
            compRect.offsetMax = Vector2.zero;
            completionText = compObj.GetComponent<Text>();

            // Tab buttons
            string[] tabs = { "cards", "enemies", "events", "bosses", "relics", "stats" };
            string[] tabLabels = { "Cards", "Bestiary", "Events", "Bosses", "Relics", "Stats" };
            float tabWidth = 1f / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                string tab = tabs[i];
                var btn = CreateButton(panel.transform, tabLabels[i], () => ShowTab(tab));
                var btnRect = btn.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(tabWidth * i, 0.77f);
                btnRect.anchorMax = new Vector2(tabWidth * (i + 1), 0.83f);
                btnRect.offsetMin = new Vector2(2, 0);
                btnRect.offsetMax = new Vector2(-2, 0);
            }

            // Content area
            contentParent = new GameObject("Content");
            contentParent.transform.SetParent(panel.transform, false);
            var contentRect = contentParent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.02f, 0.08f);
            contentRect.anchorMax = new Vector2(0.98f, 0.75f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Close button
            var closeBtn = CreateButton(panel.transform, "Close", Close);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.35f, 0.01f);
            closeRect.anchorMax = new Vector2(0.65f, 0.06f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
        }

        private void ShowTab(string tab)
        {
            currentTab = tab;
            Debug.Log($"[ScrapbookUI] ShowTab: tab='{tab}'");

            // Clear content
            foreach (Transform child in contentParent.transform)
                Destroy(child.gameObject);

            if (metaProgression == null || metaProgression.Data == null)
            {
                CreateText(contentParent.transform, "No data available yet. Complete a run to start filling the Scrapbook!", 16, TextAnchor.MiddleCenter);
                return;
            }

            var data = metaProgression.Data;
            string content = "";

            switch (tab)
            {
                case "cards":
                    content = $"Discovered Cards ({data.discoveredCards.Count}):\n\n";
                    foreach (var card in data.discoveredCards)
                        content += $"  - {card}\n";
                    if (data.discoveredCards.Count == 0) content += "  None discovered yet.\n";
                    content += $"\nUpgraded Hero Unlocks ({data.upgradedHeroUnlocks.Count}):\n\n";
                    foreach (var hero in data.upgradedHeroUnlocks)
                        content += $"  - {hero} (upgraded version available)\n";
                    break;

                case "enemies":
                    content = $"Bestiary ({data.bestiaryCompletion}% complete)\n\nDiscovered Enemies ({data.discoveredEnemies.Count}):\n\n";
                    foreach (var enemy in data.discoveredEnemies)
                        content += $"  - {enemy}\n";
                    if (data.discoveredEnemies.Count == 0) content += "  None discovered yet.\n";
                    break;

                case "events":
                    content = $"Discovered Events ({data.discoveredEvents.Count}):\n\n";
                    foreach (var evt in data.discoveredEvents)
                        content += $"  - {evt}\n";
                    if (data.discoveredEvents.Count == 0) content += "  None discovered yet.\n";
                    break;

                case "bosses":
                    content = $"Discovered Bosses ({data.discoveredBosses.Count}):\n\n";
                    foreach (var boss in data.discoveredBosses)
                        content += $"  - {boss}\n";
                    if (data.discoveredBosses.Count == 0) content += "  None discovered yet.\n";
                    break;

                case "relics":
                    content = $"Discovered Relics ({data.discoveredRelics.Count}):\n\n";
                    foreach (var relic in data.discoveredRelics)
                        content += $"  - {relic}\n";
                    if (data.discoveredRelics.Count == 0) content += "  None discovered yet.\n";
                    break;

                case "stats":
                    content = "Run Statistics\n\n";
                    content += $"  Runs Completed: {data.totalRunsCompleted}\n";
                    content += $"  Runs Failed: {data.totalRunsFailed}\n";
                    content += $"  Total Levels Cleared: {data.totalLevelsCleared}\n";
                    content += $"  Total Resources Gathered: {data.totalResourcesGathered}\n";
                    content += $"  Total Bosses Defeated: {data.totalBossesDefeated}\n";
                    content += $"  Reputation: {data.reputation}\n";
                    content += $"  Colony Deck Bonus: +{data.colonyDeckSizeBonus}\n\n";
                    content += "Best Run Records\n\n";
                    content += $"  Best Level Reached: {data.bestLevelReached}\n";
                    content += $"  Most Resources (single run): {data.bestResourcesInSingleRun}\n";
                    content += $"  Most Bosses (single run): {data.bestBossesKilledInSingleRun}\n";
                    content += $"  Fastest Run: {(data.fastestRunNodes > 0 ? data.fastestRunNodes + " nodes" : "N/A")}\n";
                    break;
            }

            var textObj = CreateText(contentParent.transform, content, 14, TextAnchor.UpperLeft);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
        }

        private GameObject CreateText(Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = Color.white;
            return obj;
        }

        private GameObject CreateButton(Transform parent, string label, System.Action onClick)
        {
            var obj = new GameObject($"Btn_{label}");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            var img = obj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f, 1f);
            var btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtObj = CreateText(obj.transform, label, 14, TextAnchor.MiddleCenter);
            var txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            return obj;
        }
    }
}
