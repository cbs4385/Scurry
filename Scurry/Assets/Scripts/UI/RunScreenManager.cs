using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Core;
using Scurry.Data;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class RunScreenManager : MonoBehaviour
    {
        private IRunManager runManager;
        private IMetaProgressionManager metaProgression;

        private GameObject victoryPanel;
        private GameObject defeatPanel;
        private TextMeshProUGUI victoryStatsText;
        private TextMeshProUGUI defeatStatsText;

        private ScrapbookUI scrapbookUI;

        private void OnEnable()
        {
            Debug.Log("[RunScreenManager] OnEnable: subscribing to events");
            EventBus.OnRunComplete += OnRunComplete;
        }

        private void OnDisable()
        {
            Debug.Log("[RunScreenManager] OnDisable: unsubscribing from events");
            EventBus.OnRunComplete -= OnRunComplete;
        }

        private void Awake()
        {
            scrapbookUI = FindAnyObjectByType<ScrapbookUI>();

            BuildVictoryPanel();
            BuildDefeatPanel();
            Debug.Log("[RunScreenManager] Awake: all panels built");
        }

        private void Start()
        {
            runManager = ServiceLocator.Get<IRunManager>();
            metaProgression = ServiceLocator.Get<IMetaProgressionManager>();
            Debug.Log($"[RunScreenManager] Start: runManager={(runManager != null ? "OK" : "NULL")}, metaProgression={(metaProgression != null ? "OK" : "NULL")}");

            // If the run already ended before this scene loaded, show the appropriate panel
            if (runManager != null)
            {
                var rm = runManager as RunManager;
                if (rm != null && rm.CurrentRunState == RunState.GameOver)
                {
                    Debug.Log("[RunScreenManager] Start: run already ended (GameOver) — showing defeat");
                    OnRunFailed();
                }
                else if (rm != null && rm.CurrentRunState == RunState.RunComplete)
                {
                    Debug.Log("[RunScreenManager] Start: run already ended (RunComplete) — showing victory");
                    OnRunComplete(true);
                }
            }
        }

        // --- Victory ---

        private void OnRunComplete(bool victory)
        {
            Debug.Log($"[RunScreenManager] OnRunComplete: victory={victory}");
            if (!victory)
            {
                Debug.Log("[RunScreenManager] OnRunComplete: defeat — showing defeat screen");
                OnRunFailed();
                return;
            }
            Debug.Log("[RunScreenManager] OnRunComplete: showing victory screen");

            string stats = "The Pack is Free!\n\n";
            if (runManager != null)
            {
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
            int rep = victory ? 10 : 2;
            return rep;
        }

        // --- New Run ---

        private void StartNewRun()
        {
            Debug.Log("[RunScreenManager] StartNewRun: starting new run via runManager");
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
            if (runManager != null)
                runManager.StartRun();
        }

        private void ReturnToMainMenu()
        {
            Debug.Log("[RunScreenManager] ReturnToMainMenu: firing OnReturnToMainMenu");
            EventBus.OnReturnToMainMenu?.Invoke();
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
                new Vector2(0.5f, 0.05f), new Vector2(0.7f, 0.15f), new Color(0.3f, 0.3f, 0.5f));

            // Main Menu button
            CreateButton(victoryPanel.transform, "Main Menu", ReturnToMainMenu,
                new Vector2(0.75f, 0.05f), new Vector2(0.95f, 0.15f), new Color(0.4f, 0.3f, 0.2f));

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
                new Vector2(0.5f, 0.05f), new Vector2(0.7f, 0.15f), new Color(0.3f, 0.3f, 0.5f));

            // Main Menu button
            CreateButton(defeatPanel.transform, "Main Menu", ReturnToMainMenu,
                new Vector2(0.75f, 0.05f), new Vector2(0.95f, 0.15f), new Color(0.4f, 0.3f, 0.2f));

            defeatPanel.SetActive(false);
            Debug.Log("[RunScreenManager] BuildDefeatPanel: complete");
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
