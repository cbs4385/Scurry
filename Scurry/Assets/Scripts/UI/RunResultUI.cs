using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Scurry.Core;
using Scurry.Data;

namespace Scurry.UI
{
    /// <summary>
    /// Victory/defeat results screen showing score breakdown and navigation buttons.
    /// Displayed in the RunResult scene after a run completes.
    /// All UI built programmatically (no prefabs).
    /// </summary>
    public class RunResultUI : MonoBehaviour
    {
        // ── UI References ───────────────────────────────────────────────
        private Canvas canvas;
        private GameObject mainPanel;

        // Banner
        private TextMeshProUGUI bannerText;
        private TextMeshProUGUI turnInfoText;
        private TextMeshProUGUI deckInfoText;

        // Score rows
        private TextMeshProUGUI turnScoreValue;
        private TextMeshProUGUI deckMultValue;
        private TextMeshProUGUI enemiesValue;
        private TextMeshProUGUI resourcesValue;
        private TextMeshProUGUI colonyValue;
        private TextMeshProUGUI heroBonusValue;
        private TextMeshProUGUI finalScoreText;

        // Buttons
        private Button newRunBtn;
        private Button mainMenuBtn;

        // ── State ──────────────────────────────────────────────────────
        private bool victory;
        private int turnsUsed;
        private int deckSize;

        private void Awake()
        {
            Debug.Log("[RunResultUI] Awake: initializing");
            BuildUI();
        }

        private void OnEnable()
        {
            Debug.Log("[RunResultUI] OnEnable: subscribing to EventBus.OnRunComplete");
            EventBus.OnRunComplete += HandleRunComplete;
        }

        private void OnDisable()
        {
            Debug.Log("[RunResultUI] OnDisable: unsubscribing from EventBus.OnRunComplete");
            EventBus.OnRunComplete -= HandleRunComplete;
        }

        private void Start()
        {
            Debug.Log("[RunResultUI] Start: checking RunManager for pending result data");

            // If RunManager already has a completed run, show it immediately
            var runManager = RunManager.Instance;
            if (runManager != null && runManager.CurrentState == RunState.RunComplete)
            {
                Debug.Log($"[RunResultUI] Start: RunManager state is RunComplete, building score from RunManager data");
                bool isVictory = runManager.PiedPiperDefeated;
                int turns = runManager.CurrentTurn;
                int deck = runManager.ConstructedDeck.Count;

                var input = new ScoreInput
                {
                    turnsUsed = turns,
                    deckSize = deck,
                    enemiesDefeated = runManager.TotalEnemiesDefeated,
                    zoneBossesDefeated = runManager.ZoneBossesDefeated,
                    piedPiperDefeated = runManager.PiedPiperDefeated,
                    totalResourcesGathered = runManager.TotalResourcesGathered,
                    colonyCardsPlayed = runManager.ColonyCardsPlayed,
                    heroesNeverInjured = 0 // TODO: derive from RunManager when hero count is available
                };

                var score = ScoreCalculator.CalculateScore(input);
                Debug.Log($"[RunResultUI] Start: calculated score from RunManager (finalScore={score.finalScore})");
                Show(isVictory, score, turns, deck);
            }
            else
            {
                Debug.Log("[RunResultUI] Start: no completed run data found, waiting for EventBus.OnRunComplete");
            }
        }

        // ── Build ───────────────────────────────────────────────────────

        private void BuildUI()
        {
            Debug.Log("[RunResultUI] BuildUI: starting construction");
            UIHelper.EnsureEventSystem();

            // Canvas
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Full screen background
            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(transform, false);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.04f, 0.95f);

            // Main centered panel
            mainPanel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
            mainPanel.transform.SetParent(transform, false);
            var panelRect = mainPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(620, 720);
            mainPanel.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.1f, 0.98f);

            // ── Banner ──────────────────────────────────────────────────
            bannerText = CreateText(mainPanel.transform, "Banner",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -20), new Vector2(0, 70),
                "VICTORY!", 54, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.1f));

            // Turn info
            turnInfoText = CreateText(mainPanel.transform, "TurnInfo",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -95), new Vector2(-40, 30),
                "", 18, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));

            // Deck info
            deckInfoText = CreateText(mainPanel.transform, "DeckInfo",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -120), new Vector2(-40, 24),
                "", 16, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));

            // ── Separator ───────────────────────────────────────────────
            CreateSeparator(mainPanel.transform, -150);

            // ── Score breakdown table ───────────────────────────────────
            float startY = -170f;
            float rowH = 36f;

            turnScoreValue = CreateScoreRow(mainPanel.transform, "Turn Score:", startY);
            deckMultValue = CreateScoreRow(mainPanel.transform, "Deck Multiplier:", startY - rowH);
            enemiesValue = CreateScoreRow(mainPanel.transform, "Enemies Defeated:", startY - rowH * 2);
            resourcesValue = CreateScoreRow(mainPanel.transform, "Resources Gathered:", startY - rowH * 3);
            colonyValue = CreateScoreRow(mainPanel.transform, "Colony Cards:", startY - rowH * 4);
            heroBonusValue = CreateScoreRow(mainPanel.transform, "Hero Bonus:", startY - rowH * 5);

            Debug.Log("[RunResultUI] BuildUI: score rows created");

            // ── Separator before final score ────────────────────────────
            float separatorY = startY - rowH * 5 - 20;
            CreateSeparator(mainPanel.transform, separatorY);

            // ── Final score ─────────────────────────────────────────────
            finalScoreText = CreateText(mainPanel.transform, "FinalScore",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, separatorY - 15), new Vector2(-60, 50),
                "FINAL SCORE: 0", 32, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.3f));

            // ── Navigation buttons ──────────────────────────────────────
            float buttonY = -620f;

            // New Run button
            var newRunGO = CreateNavButton(mainPanel.transform, "NewRunButton", "New Run",
                new Vector2(-110, buttonY), new Color(0.2f, 0.45f, 0.2f));
            newRunBtn = newRunGO.GetComponent<Button>();
            newRunBtn.onClick.AddListener(OnNewRunClicked);
            Debug.Log("[RunResultUI] BuildUI: New Run button created");

            // Main Menu button
            var mainMenuGO = CreateNavButton(mainPanel.transform, "MainMenuButton", "Main Menu",
                new Vector2(110, buttonY), new Color(0.35f, 0.25f, 0.15f));
            mainMenuBtn = mainMenuGO.GetComponent<Button>();
            mainMenuBtn.onClick.AddListener(OnMainMenuClicked);
            Debug.Log("[RunResultUI] BuildUI: Main Menu button created");

            mainPanel.SetActive(false);
            Debug.Log("[RunResultUI] BuildUI: complete (panel hidden)");
        }

        private TextMeshProUGUI CreateScoreRow(Transform parent, string label, float yPos)
        {
            Debug.Log($"[RunResultUI] CreateScoreRow: label='{label}', yPos={yPos:F0}");

            // Label (left aligned)
            var labelGO = new GameObject($"Label_{label}", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(parent, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(0.6f, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = new Vector2(40, yPos);
            labelRect.sizeDelta = new Vector2(0, 32);
            var labelTmp = labelGO.GetComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 18;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.color = new Color(0.8f, 0.8f, 0.8f);

            // Value (right aligned)
            var valueGO = new GameObject($"Value_{label}", typeof(RectTransform), typeof(TextMeshProUGUI));
            valueGO.transform.SetParent(parent, false);
            var valueRect = valueGO.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.6f, 1);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.pivot = new Vector2(1, 1);
            valueRect.anchoredPosition = new Vector2(-40, yPos);
            valueRect.sizeDelta = new Vector2(0, 32);
            var valueTmp = valueGO.GetComponent<TextMeshProUGUI>();
            valueTmp.text = "0";
            valueTmp.fontSize = 18;
            valueTmp.fontStyle = FontStyles.Bold;
            valueTmp.alignment = TextAlignmentOptions.MidlineRight;
            valueTmp.color = Color.white;

            return valueTmp;
        }

        private void CreateSeparator(Transform parent, float yPos)
        {
            Debug.Log($"[RunResultUI] CreateSeparator: yPos={yPos:F0}");

            var sepGO = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            sepGO.transform.SetParent(parent, false);
            var sepRect = sepGO.GetComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0.1f, 1);
            sepRect.anchorMax = new Vector2(0.9f, 1);
            sepRect.pivot = new Vector2(0.5f, 1);
            sepRect.anchoredPosition = new Vector2(0, yPos);
            sepRect.sizeDelta = new Vector2(0, 2);
            sepGO.GetComponent<Image>().color = new Color(0.4f, 0.35f, 0.2f, 0.5f);
        }

        private GameObject CreateNavButton(Transform parent, string name, string label, Vector2 position, Color color)
        {
            Debug.Log($"[RunResultUI] CreateNavButton: name={name}, label={label}, pos={position}");

            var btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 1);
            btnRect.anchorMax = new Vector2(0.5f, 1);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = position;
            btnRect.sizeDelta = new Vector2(200, 55);
            btnGO.GetComponent<Image>().color = color;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnGO;
        }

        // ── Public Methods ──────────────────────────────────────────────

        /// <summary>
        /// Shows the run result screen with victory/defeat state and score breakdown.
        /// Accepts the ScoreBreakdown from Scurry.Core.ScoreCalculator.
        /// </summary>
        public void Show(bool victory, ScoreBreakdown score, int turnsUsed, int deckSize)
        {
            Debug.Log($"[RunResultUI] Show: victory={victory}, turnsUsed={turnsUsed}, deckSize={deckSize}, " +
                      $"turnScore={score.turnScore}, deckMult={score.deckMultiplier:F2}, enemyScore={score.enemyScore}, " +
                      $"resourceScore={score.resourceScore}, colonyScore={score.colonyScore}, heroBonus={score.heroBonus}, " +
                      $"finalScore={score.finalScore}");

            mainPanel.SetActive(true);

            // Banner
            if (bannerText != null)
            {
                bannerText.text = victory ? "VICTORY!" : "DEFEAT";
                bannerText.color = victory ? new Color(1f, 0.85f, 0.1f) : new Color(0.9f, 0.2f, 0.2f);
                Debug.Log($"[RunResultUI] Show: banner='{bannerText.text}', color={(victory ? "gold" : "red")}");
            }

            // Turn and deck info
            if (turnInfoText != null)
            {
                turnInfoText.text = $"Turns Used: {turnsUsed}";
                Debug.Log($"[RunResultUI] Show: turnInfo='{turnInfoText.text}'");
            }
            if (deckInfoText != null)
            {
                deckInfoText.text = $"Deck Size: {deckSize} cards";
                Debug.Log($"[RunResultUI] Show: deckInfo='{deckInfoText.text}'");
            }

            // Score rows
            if (turnScoreValue != null)
            {
                turnScoreValue.text = score.turnScore.ToString();
                Debug.Log($"[RunResultUI] Show: turnScore={score.turnScore}");
            }
            if (deckMultValue != null)
            {
                deckMultValue.text = $"x{score.deckMultiplier:F2}";
                Debug.Log($"[RunResultUI] Show: deckMultiplier={score.deckMultiplier:F2}");
            }
            if (enemiesValue != null)
            {
                enemiesValue.text = $"+{score.enemyScore}";
                Debug.Log($"[RunResultUI] Show: enemyScore=+{score.enemyScore}");
            }
            if (resourcesValue != null)
            {
                resourcesValue.text = $"+{score.resourceScore}";
                Debug.Log($"[RunResultUI] Show: resourceScore=+{score.resourceScore}");
            }
            if (colonyValue != null)
            {
                colonyValue.text = $"+{score.colonyScore}";
                Debug.Log($"[RunResultUI] Show: colonyScore=+{score.colonyScore}");
            }
            if (heroBonusValue != null)
            {
                heroBonusValue.text = $"+{score.heroBonus}";
                Debug.Log($"[RunResultUI] Show: heroBonus=+{score.heroBonus}");
            }
            if (finalScoreText != null)
            {
                finalScoreText.text = $"FINAL SCORE: {score.finalScore}";
                finalScoreText.color = victory ? new Color(1f, 0.9f, 0.3f) : new Color(0.7f, 0.7f, 0.7f);
                Debug.Log($"[RunResultUI] Show: finalScore={score.finalScore}");
            }

            // Fire score event
            Debug.Log($"[RunResultUI] Show: firing EventBus.OnScoreCalculated with score={score.finalScore}");
            EventBus.OnScoreCalculated?.Invoke(score.finalScore);
        }

        /// <summary>
        /// Called when the New Run button is clicked.
        /// </summary>
        public void OnNewRunClicked()
        {
            Debug.Log("[RunResultUI] OnNewRunClicked: resetting state via RunManager.StartNewRun()");
            mainPanel.SetActive(false);

            var runManager = RunManager.Instance;
            if (runManager != null)
            {
                runManager.StartNewRun();
            }
            else
            {
                Debug.LogError("[RunResultUI] OnNewRunClicked: RunManager not found — falling back to MainMenu");
                SceneManager.LoadScene("MainMenu");
            }
        }

        /// <summary>
        /// Called when the Main Menu button is clicked.
        /// </summary>
        public void OnMainMenuClicked()
        {
            Debug.Log("[RunResultUI] OnMainMenuClicked: transitioning to MainMenu scene");
            mainPanel.SetActive(false);
            EventBus.OnReturnToMainMenu?.Invoke();
            SceneManager.LoadScene("MainMenu");
        }

        // ── Event Handlers ───────────────────────────────────────────────

        /// <summary>
        /// Handles the EventBus.OnRunComplete event. Receives the victory bool,
        /// queries RunManager for stats, computes score via ScoreCalculator, and displays.
        /// </summary>
        private void HandleRunComplete(bool isVictory)
        {
            Debug.Log($"[RunResultUI] HandleRunComplete: received event (victory={isVictory})");

            victory = isVictory;
            var runManager = RunManager.Instance;

            if (runManager == null)
            {
                Debug.LogWarning("[RunResultUI] HandleRunComplete: RunManager.Instance is null, cannot compute score");
                return;
            }

            turnsUsed = runManager.CurrentTurn;
            deckSize = runManager.ConstructedDeck.Count;

            Debug.Log($"[RunResultUI] HandleRunComplete: retrieved RunManager data (turnsUsed={turnsUsed}, deckSize={deckSize}, " +
                      $"enemiesDefeated={runManager.TotalEnemiesDefeated}, zoneBosses={runManager.ZoneBossesDefeated}, " +
                      $"piedPiper={runManager.PiedPiperDefeated}, resources={runManager.TotalResourcesGathered}, " +
                      $"colonyCards={runManager.ColonyCardsPlayed})");

            var input = new ScoreInput
            {
                turnsUsed = turnsUsed,
                deckSize = deckSize,
                enemiesDefeated = runManager.TotalEnemiesDefeated,
                zoneBossesDefeated = runManager.ZoneBossesDefeated,
                piedPiperDefeated = runManager.PiedPiperDefeated,
                totalResourcesGathered = runManager.TotalResourcesGathered,
                colonyCardsPlayed = runManager.ColonyCardsPlayed,
                heroesNeverInjured = 0 // TODO: derive from RunManager when total hero count is tracked
            };

            var score = ScoreCalculator.CalculateScore(input);
            Debug.Log($"[RunResultUI] HandleRunComplete: score calculated (finalScore={score.finalScore})");

            Show(victory, score, turnsUsed, deckSize);
        }

        // ── UI Factory Helper ───────────────────────────────────────────

        private TextMeshProUGUI CreateText(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta,
            string text, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            return tmp;
        }
    }
}
