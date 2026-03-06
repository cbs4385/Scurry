using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Core;

namespace Scurry.UI
{
    public class RunScreenManager : MonoBehaviour
    {
        private GameObject runStartPanel;
        private GameObject levelTransitionPanel;
        private GameObject victoryPanel;
        private GameObject defeatPanel;
        private TextMeshProUGUI victoryStatsText;
        private TextMeshProUGUI defeatStatsText;
        private TextMeshProUGUI levelTransitionText;

        private RunManager runManager;
        private MetaProgressionManager metaProgression;
        private ScrapbookUI scrapbookUI;

        private void OnEnable()
        {
            Debug.Log("[RunScreenManager] OnEnable: subscribing to events");
            EventBus.OnRunComplete_M1 += OnRunComplete;
            EventBus.OnRunFailed_M1 += OnRunFailed;
            EventBus.OnLevelAdvanced += OnLevelAdvanced;
        }

        private void OnDisable()
        {
            Debug.Log("[RunScreenManager] OnDisable: unsubscribing from events");
            EventBus.OnRunComplete_M1 -= OnRunComplete;
            EventBus.OnRunFailed_M1 -= OnRunFailed;
            EventBus.OnLevelAdvanced -= OnLevelAdvanced;
        }

        private void Awake()
        {
            runManager = FindObjectOfType<RunManager>();
            metaProgression = FindObjectOfType<MetaProgressionManager>();
            scrapbookUI = FindObjectOfType<ScrapbookUI>();

            BuildVictoryPanel();
            BuildDefeatPanel();
            BuildLevelTransitionPanel();
            Debug.Log("[RunScreenManager] Awake: all panels built");
        }

        // --- Victory ---

        private void OnRunComplete(bool victory)
        {
            if (!victory) return;
            Debug.Log("[RunScreenManager] OnRunComplete: showing victory screen");

            string stats = "The Pack is Free!\n\n";
            if (runManager != null)
            {
                stats += $"Level Reached: {runManager.CurrentLevel}\n";
                stats += $"Food: {runManager.FoodStockpile}  Materials: {runManager.MaterialsStockpile}  Currency: {runManager.CurrencyStockpile}\n";
            }
            if (metaProgression != null)
            {
                stats += $"\nReputation Earned: +{CalculateRepEarned(true)}\n";
                stats += $"Scrapbook: {metaProgression.ScrapbookCompletion}%\n";
            }

            victoryStatsText.text = stats;
            victoryPanel.SetActive(true);
        }

        private void OnRunFailed()
        {
            Debug.Log("[RunScreenManager] OnRunFailed: showing defeat screen");

            string stats = "The Colony Has Fallen...\n\n";
            if (runManager != null)
            {
                stats += $"Level Reached: {runManager.CurrentLevel}\n";
                stats += $"Food: {runManager.FoodStockpile}  Materials: {runManager.MaterialsStockpile}  Currency: {runManager.CurrencyStockpile}\n";
            }
            if (metaProgression != null)
            {
                stats += $"\nReputation Earned: +{CalculateRepEarned(false)}\n";
            }

            defeatStatsText.text = stats;
            defeatPanel.SetActive(true);
        }

        private int CalculateRepEarned(bool victory)
        {
            int level = runManager != null ? runManager.CurrentLevel : 1;
            int rep = level * 2;
            if (victory) rep += 10;
            return rep;
        }

        // --- Level Transition ---

        private void OnLevelAdvanced(int newLevel)
        {
            Debug.Log($"[RunScreenManager] OnLevelAdvanced: transitioning to level {newLevel}");
            string[] levelNames = { "", "The Wilderness", "Rural Village", "The Town" };
            string levelName = newLevel <= 3 ? levelNames[newLevel] : $"Level {newLevel}";

            levelTransitionText.text = $"Level Complete!\n\nPreparing for:\n{levelName}\n\nExhausted heroes restored.\nColony rebuilding begins.";
            levelTransitionPanel.SetActive(true);

            // Auto-hide after a delay
            Invoke(nameof(HideLevelTransition), 3f);
        }

        private void HideLevelTransition()
        {
            if (levelTransitionPanel != null)
                levelTransitionPanel.SetActive(false);
        }

        // --- New Run ---

        private void StartNewRun()
        {
            Debug.Log("[RunScreenManager] StartNewRun: starting new run");
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
            if (runManager != null)
                runManager.StartRun();
        }

        private void OpenScrapbook()
        {
            Debug.Log("[RunScreenManager] OpenScrapbook: opening scrapbook");
            if (scrapbookUI != null)
                scrapbookUI.Open();
        }

        // --- Panel Construction ---

        private void BuildVictoryPanel()
        {
            victoryPanel = new GameObject("VictoryPanel", typeof(RectTransform), typeof(Image));
            victoryPanel.transform.SetParent(transform, false);
            var panelRect = victoryPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            victoryPanel.GetComponent<Image>().color = new Color(0.05f, 0.1f, 0.05f, 0.95f);

            // Title
            CreateTMPText(victoryPanel.transform, "VictoryTitle", "VICTORY!", 48, FontStyles.Bold,
                new Color(1f, 0.9f, 0.3f), new Vector2(0, 0.7f), new Vector2(1, 0.9f));

            // Stats
            var statsGO = CreateTMPText(victoryPanel.transform, "VictoryStats", "", 18, FontStyles.Normal,
                Color.white, new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.65f));
            victoryStatsText = statsGO.GetComponent<TextMeshProUGUI>();

            // New Run button
            CreateButton(victoryPanel.transform, "New Run", StartNewRun,
                new Vector2(0.25f, 0.05f), new Vector2(0.5f, 0.15f), new Color(0.2f, 0.5f, 0.2f));

            // Scrapbook button
            CreateButton(victoryPanel.transform, "Scrapbook", OpenScrapbook,
                new Vector2(0.55f, 0.05f), new Vector2(0.8f, 0.15f), new Color(0.3f, 0.3f, 0.5f));

            victoryPanel.SetActive(false);
            Debug.Log("[RunScreenManager] BuildVictoryPanel: complete");
        }

        private void BuildDefeatPanel()
        {
            defeatPanel = new GameObject("DefeatPanel", typeof(RectTransform), typeof(Image));
            defeatPanel.transform.SetParent(transform, false);
            var panelRect = defeatPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            defeatPanel.GetComponent<Image>().color = new Color(0.1f, 0.02f, 0.02f, 0.95f);

            // Title
            CreateTMPText(defeatPanel.transform, "DefeatTitle", "DEFEAT", 48, FontStyles.Bold,
                new Color(0.8f, 0.2f, 0.2f), new Vector2(0, 0.7f), new Vector2(1, 0.9f));

            // Stats
            var statsGO = CreateTMPText(defeatPanel.transform, "DefeatStats", "", 18, FontStyles.Normal,
                new Color(0.8f, 0.7f, 0.7f), new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.65f));
            defeatStatsText = statsGO.GetComponent<TextMeshProUGUI>();

            // Try Again button
            CreateButton(defeatPanel.transform, "Try Again", StartNewRun,
                new Vector2(0.25f, 0.05f), new Vector2(0.5f, 0.15f), new Color(0.5f, 0.2f, 0.2f));

            // Scrapbook button
            CreateButton(defeatPanel.transform, "Scrapbook", OpenScrapbook,
                new Vector2(0.55f, 0.05f), new Vector2(0.8f, 0.15f), new Color(0.3f, 0.3f, 0.5f));

            defeatPanel.SetActive(false);
            Debug.Log("[RunScreenManager] BuildDefeatPanel: complete");
        }

        private void BuildLevelTransitionPanel()
        {
            levelTransitionPanel = new GameObject("LevelTransitionPanel", typeof(RectTransform), typeof(Image));
            levelTransitionPanel.transform.SetParent(transform, false);
            var panelRect = levelTransitionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.2f);
            panelRect.anchorMax = new Vector2(0.85f, 0.8f);
            panelRect.sizeDelta = Vector2.zero;
            levelTransitionPanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.15f, 0.95f);

            var textGO = CreateTMPText(levelTransitionPanel.transform, "TransitionText", "", 22, FontStyles.Normal,
                Color.white, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));
            levelTransitionText = textGO.GetComponent<TextMeshProUGUI>();

            levelTransitionPanel.SetActive(false);
            Debug.Log("[RunScreenManager] BuildLevelTransitionPanel: complete");
        }

        // --- Helpers ---

        private GameObject CreateTMPText(Transform parent, string name, string text, int fontSize,
            FontStyles style, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return go;
        }

        private void CreateButton(Transform parent, string label, System.Action onClick,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var btnGO = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = anchorMin;
            btnRect.anchorMax = anchorMax;
            btnRect.sizeDelta = Vector2.zero;
            btnGO.GetComponent<Image>().color = bgColor;
            btnGO.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());

            CreateTMPText(btnGO.transform, "Label", label, 18, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one);
        }
    }
}
