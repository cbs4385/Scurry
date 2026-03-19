using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Core;
using Scurry.Data;
using Scurry.Cards;
using Scurry.Interfaces;

namespace Scurry.UI
{
    /// <summary>
    /// Game HUD overlay displayed during the GameMap scene.
    /// Shows turn counter, phase, resources, hero count, end phase button, and notifications.
    /// All UI elements are created programmatically on a ScreenSpaceOverlay canvas.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        // --- UI References ---
        private Canvas canvas;
        private TextMeshProUGUI turnLabel;
        private TextMeshProUGUI phaseLabel;
        private TextMeshProUGUI foodText;
        private TextMeshProUGUI materialsText;
        private TextMeshProUGUI currencyText;
        private TextMeshProUGUI heroCountText;
        private GameObject endPhaseButton;
        private Button endPhaseBtn;
        private GameObject continueButton;
        private Button continueBtn;
        private TextMeshProUGUI continueBtnText;
        // Notifications are handled by NotificationStack (per-scene component)

        // Colony card selection panel (shown during ColonyDeploy phase)
        private GameObject colonyCardPanel;
        private RectTransform colonyCardListContainer;
        private readonly List<GameObject> colonyCardListItems = new List<GameObject>();
        private TextMeshProUGUI colonyInstructionText;
        // Detail sub-panel
        private Image colonyDetailArt;
        private TextMeshProUGUI colonyDetailName;
        private TextMeshProUGUI colonyDetailDesc;
        private TextMeshProUGUI colonyDetailReq;
        // Highlighted valid placement nodes
        private readonly List<int> highlightedColonyNodes = new List<int>();
        /// <summary>Currently selected colony card for placement. Set by HUD, read by MapInteraction.</summary>
        public ColonyCardDefinitionSO SelectedColonyCard { get; private set; }

        /// <summary>
        /// Builds all HUD UI elements programmatically.
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[HUDManager] Initialize: starting HUD construction");

            UIHelper.EnsureEventSystem();
            BuildCanvas();
            BuildTurnDisplay();
            BuildResourcePanel();
            BuildHeroCount();
            BuildEndPhaseButton();
            BuildContinueButton();
            BuildColonyCardPanel();

            // Subscribe to events
            EventBus.OnPhaseChanged += OnPhaseChanged;
            EventBus.OnTurnStarted += OnTurnStarted;

            Debug.Log("[HUDManager] Initialize: complete");
        }

        private void OnDestroy()
        {
            Debug.Log("[HUDManager] OnDestroy: unsubscribing from events");
            EventBus.OnPhaseChanged -= OnPhaseChanged;
            EventBus.OnTurnStarted -= OnTurnStarted;
        }

        // ─── Build Methods ───────────────────────────────────────────────

        private void BuildCanvas()
        {
            Debug.Log("[HUDManager] BuildCanvas: creating ScreenSpaceCamera canvas");

            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 100;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            Debug.Log("[HUDManager] BuildCanvas: canvas created (referenceResolution=1920x1080)");
        }

        private void BuildTurnDisplay()
        {
            Debug.Log("[HUDManager] BuildTurnDisplay: creating turn and phase labels");

            // Turn counter - top center
            var turnGO = new GameObject("TurnLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            turnGO.transform.SetParent(transform, false);
            var turnRect = turnGO.GetComponent<RectTransform>();
            turnRect.anchorMin = new Vector2(0.5f, 1f);
            turnRect.anchorMax = new Vector2(0.5f, 1f);
            turnRect.pivot = new Vector2(0.5f, 1f);
            turnRect.anchoredPosition = new Vector2(0, -10);
            turnRect.sizeDelta = new Vector2(300, 50);
            turnLabel = turnGO.GetComponent<TextMeshProUGUI>();
            turnLabel.text = "Turn 1";
            turnLabel.fontSize = 32;
            turnLabel.fontStyle = FontStyles.Bold;
            turnLabel.alignment = TextAlignmentOptions.Center;
            turnLabel.color = Color.white;
            Debug.Log("[HUDManager] BuildTurnDisplay: turnLabel created");

            // Phase name - below turn counter
            var phaseGO = new GameObject("PhaseLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            phaseGO.transform.SetParent(transform, false);
            var phaseRect = phaseGO.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0.5f, 1f);
            phaseRect.anchorMax = new Vector2(0.5f, 1f);
            phaseRect.pivot = new Vector2(0.5f, 1f);
            phaseRect.anchoredPosition = new Vector2(0, -58);
            phaseRect.sizeDelta = new Vector2(300, 36);
            phaseLabel = phaseGO.GetComponent<TextMeshProUGUI>();
            phaseLabel.text = "Deploy Phase";
            phaseLabel.fontSize = 22;
            phaseLabel.alignment = TextAlignmentOptions.Center;
            phaseLabel.color = new Color(0.85f, 0.85f, 0.5f);
            Debug.Log("[HUDManager] BuildTurnDisplay: phaseLabel created");
        }

        private void BuildResourcePanel()
        {
            Debug.Log("[HUDManager] BuildResourcePanel: creating resource displays");

            // Resource panel background - top left
            var panelGO = new GameObject("ResourcePanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(transform, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(220, 120);
            panelGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            // Food
            foodText = CreateResourceLabel(panelGO.transform, "FoodText", "Food: 0", new Color(0.4f, 0.8f, 0.2f), 0);
            Debug.Log("[HUDManager] BuildResourcePanel: foodText created");

            // Materials
            materialsText = CreateResourceLabel(panelGO.transform, "MaterialsText", "Materials: 0", new Color(0.7f, 0.5f, 0.3f), 1);
            Debug.Log("[HUDManager] BuildResourcePanel: materialsText created");

            // Currency
            currencyText = CreateResourceLabel(panelGO.transform, "CurrencyText", "Currency: 0", new Color(1f, 0.85f, 0.1f), 2);
            Debug.Log("[HUDManager] BuildResourcePanel: currencyText created");
        }

        private TextMeshProUGUI CreateResourceLabel(Transform parent, string name, string defaultText, Color color, int index)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(12, -10 - index * 34);
            rect.sizeDelta = new Vector2(-24, 30);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = color;

            return tmp;
        }

        private void BuildHeroCount()
        {
            Debug.Log("[HUDManager] BuildHeroCount: creating hero count display");

            var heroGO = new GameObject("HeroCount", typeof(RectTransform), typeof(TextMeshProUGUI));
            heroGO.transform.SetParent(transform, false);
            var heroRect = heroGO.GetComponent<RectTransform>();
            heroRect.anchorMin = new Vector2(0, 1);
            heroRect.anchorMax = new Vector2(0, 1);
            heroRect.pivot = new Vector2(0, 1);
            heroRect.anchoredPosition = new Vector2(10, -140);
            heroRect.sizeDelta = new Vector2(220, 30);

            heroCountText = heroGO.GetComponent<TextMeshProUGUI>();
            heroCountText.text = "Heroes: 0 / 0";
            heroCountText.fontSize = 18;
            heroCountText.alignment = TextAlignmentOptions.Left;
            heroCountText.color = new Color(0.7f, 0.85f, 1f);

            Debug.Log("[HUDManager] BuildHeroCount: heroCountText created");
        }

        private void BuildEndPhaseButton()
        {
            Debug.Log("[HUDManager] BuildEndPhaseButton: creating End Phase button");

            endPhaseButton = new GameObject("EndPhaseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            endPhaseButton.transform.SetParent(transform, false);
            var btnRect = endPhaseButton.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0);
            btnRect.anchorMax = new Vector2(1, 0);
            btnRect.pivot = new Vector2(1, 0);
            btnRect.anchoredPosition = new Vector2(-20, 20);
            btnRect.sizeDelta = new Vector2(200, 60);

            var btnImage = endPhaseButton.GetComponent<Image>();
            btnImage.color = new Color(0.3f, 0.6f, 0.3f);

            // Button text
            var textGO = new GameObject("ButtonText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(endPhaseButton.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "End Phase";
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            endPhaseBtn = endPhaseButton.GetComponent<Button>();
            endPhaseBtn.onClick.AddListener(OnEndPhaseClicked);

            // Hidden by default
            endPhaseButton.SetActive(false);

            Debug.Log("[HUDManager] BuildEndPhaseButton: button created (hidden)");
        }

        private void BuildContinueButton()
        {
            Debug.Log("[HUDManager] BuildContinueButton: creating Continue button");

            continueButton = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            continueButton.transform.SetParent(transform, false);
            var btnRect = continueButton.GetComponent<RectTransform>();
            // Center-bottom of screen
            btnRect.anchorMin = new Vector2(0.5f, 0);
            btnRect.anchorMax = new Vector2(0.5f, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            btnRect.anchoredPosition = new Vector2(0, 20);
            btnRect.sizeDelta = new Vector2(300, 70);

            var btnImage = continueButton.GetComponent<Image>();
            btnImage.color = new Color(0.2f, 0.4f, 0.6f);

            var textGO = new GameObject("ButtonText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(continueButton.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            continueBtnText = textGO.GetComponent<TextMeshProUGUI>();
            continueBtnText.text = "Continue";
            continueBtnText.fontSize = 22;
            continueBtnText.fontStyle = FontStyles.Bold;
            continueBtnText.alignment = TextAlignmentOptions.Center;
            continueBtnText.color = Color.white;

            continueBtn = continueButton.GetComponent<Button>();
            continueBtn.onClick.AddListener(OnContinueClicked);

            continueButton.SetActive(false);

            Debug.Log("[HUDManager] BuildContinueButton: button created (hidden)");
        }

        private void OnContinueClicked()
        {
            Debug.Log("[HUDManager] OnContinueClicked: forwarding to TurnManager.PlayerContinue()");
            var tm = TurnManager.Instance;
            if (tm != null) tm.PlayerContinue();
        }


        // ─── Public Methods ──────────────────────────────────────────────

        /// <summary>
        /// Updates the turn counter and phase name display.
        /// </summary>
        private int lastDisplayedTurn = -1;
        private GamePhase lastDisplayedPhase = (GamePhase)(-1);

        public void UpdateTurnDisplay(int turn, GamePhase phase, int piperCountdownRemaining = -1)
        {
            bool changed = (turn != lastDisplayedTurn || phase != lastDisplayedPhase);
            if (changed)
            {
                lastDisplayedTurn = turn;
                lastDisplayedPhase = phase;
                Debug.Log($"[HUDManager] UpdateTurnDisplay: turn={turn}, phase={phase}, piperCountdown={piperCountdownRemaining}");
            }

            if (turnLabel != null)
            {
                if (piperCountdownRemaining > 0)
                {
                    turnLabel.text = $"Turn {turn} | Piper: {piperCountdownRemaining}";
                    turnLabel.color = piperCountdownRemaining <= 3 ? new Color(1f, 0.3f, 0.1f) : Color.white;
                }
                else if (piperCountdownRemaining == 0)
                {
                    turnLabel.text = $"Turn {turn} | PIPER MARCHES!";
                    turnLabel.color = new Color(1f, 0.1f, 0.1f);
                }
                else
                {
                    turnLabel.text = $"Turn {turn}";
                    turnLabel.color = Color.white;
                }
                if (changed) Debug.Log($"[HUDManager] UpdateTurnDisplay: turnLabel updated (text='{turnLabel.text}')");
            }

            if (phaseLabel != null)
            {
                string phaseName = FormatPhaseName(phase);
                phaseLabel.text = phaseName;
                if (changed) Debug.Log($"[HUDManager] UpdateTurnDisplay: phaseLabel updated (text='{phaseName}')");
            }

            // Show End Phase button only during Colony and Deploy phases
            ShowEndPhaseButton(phase == GamePhase.ColonyDeploy || phase == GamePhase.Deploy);
        }

        /// <summary>
        /// Updates the resource stockpile displays.
        /// </summary>
        private int lastFood = -1, lastMaterials = -1, lastCurrency = -1;

        public void UpdateResources(int food, int materials, int currency)
        {
            bool changed = (food != lastFood || materials != lastMaterials || currency != lastCurrency);
            if (changed)
            {
                lastFood = food; lastMaterials = materials; lastCurrency = currency;
                Debug.Log($"[HUDManager] UpdateResources: food={food}, materials={materials}, currency={currency}");
            }

            if (foodText != null)
            {
                foodText.text = $"Food: {food}";
            }
            if (materialsText != null)
            {
                materialsText.text = $"Materials: {materials}";
            }
            if (currencyText != null)
            {
                currencyText.text = $"Currency: {currency}";
            }
        }

        /// <summary>
        /// Updates the deployed hero count display.
        /// </summary>
        private int lastDeployed = -1, lastTotal = -1;

        public void UpdateHeroCount(int deployed, int total)
        {
            bool changed = (deployed != lastDeployed || total != lastTotal);
            if (changed)
            {
                lastDeployed = deployed; lastTotal = total;
                Debug.Log($"[HUDManager] UpdateHeroCount: deployed={deployed}, total={total}");
            }

            if (heroCountText != null)
            {
                heroCountText.text = $"Heroes: {deployed} / {total}";
            }
        }


        /// <summary>
        /// Shows or hides the End Phase button.
        /// </summary>
        private bool lastEndPhaseShow = false;

        /// <summary>
        /// Shows/hides the Continue button between auto-resolved phases.
        /// </summary>
        public void ShowContinueButton(bool show, string phaseCompleteName = "")
        {
            Debug.Log($"[HUDManager] ShowContinueButton: show={show}, phase='{phaseCompleteName}'");
            if (continueButton != null)
            {
                continueButton.SetActive(show);
                if (show && continueBtnText != null)
                {
                    if (phaseCompleteName == "Set Hero Targets")
                        continueBtnText.text = "Click heroes then adjacent nodes\nto set move targets.\nContinue";
                    else
                        continueBtnText.text = $"{phaseCompleteName} Complete\nContinue";
                }
            }
        }

        public void ShowEndPhaseButton(bool show)
        {
            if (show != lastEndPhaseShow)
            {
                lastEndPhaseShow = show;
                Debug.Log($"[HUDManager] ShowEndPhaseButton: show={show}");
            }

            if (endPhaseButton != null)
            {
                endPhaseButton.SetActive(show);
            }
        }

        /// <summary>
        /// Called when the End Phase button is clicked.
        /// </summary>
        public void OnEndPhaseClicked()
        {
            Debug.Log("[HUDManager] OnEndPhaseClicked: calling TurnManager.PlayerEndPhase");
            var tm = TurnManager.Instance;
            if (tm != null)
            {
                tm.PlayerEndPhase();
            }
            else
            {
                Debug.LogWarning("[HUDManager] OnEndPhaseClicked: TurnManager not found, falling back to EventBus");
                EventBus.OnTurnEnded?.Invoke();
            }
        }

        // ─── Event Handlers ──────────────────────────────────────────────

        private void OnPhaseChanged(GamePhase phase)
        {
            Debug.Log($"[HUDManager] OnPhaseChanged: phase={phase}");
            if (phaseLabel != null)
            {
                phaseLabel.text = FormatPhaseName(phase);
            }
            ShowEndPhaseButton(phase == GamePhase.ColonyDeploy || phase == GamePhase.Deploy);

            // Show/hide colony card panel
            bool showColony = phase == GamePhase.ColonyDeploy;
            if (colonyCardPanel != null)
            {
                colonyCardPanel.SetActive(showColony);
                if (showColony) RefreshColonyCardList();
            }
        }

        private void OnTurnStarted(int turn)
        {
            Debug.Log($"[HUDManager] OnTurnStarted: turn={turn}");
            // Turn label is updated via UpdateTurnDisplay with countdown info
        }


        // ─── Utility ─────────────────────────────────────────────────────

        // ─── Colony Card Panel ─────────────────────────────────────────

        private void BuildColonyCardPanel()
        {
            Debug.Log("[HUDManager] BuildColonyCardPanel: creating colony card selection panel");

            colonyCardPanel = new GameObject("ColonyCardPanel", typeof(RectTransform), typeof(Image));
            colonyCardPanel.transform.SetParent(canvas.transform, false);
            var panelRect = colonyCardPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.08f);
            panelRect.anchorMax = new Vector2(0.22f, 0.92f);
            panelRect.sizeDelta = Vector2.zero;
            colonyCardPanel.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.92f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(colonyCardPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.95f);
            titleRect.anchorMax = new Vector2(0.95f, 1f);
            titleRect.sizeDelta = Vector2.zero;
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Colony Cards";
            titleTmp.fontSize = 16;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.9f, 0.75f, 0.1f);
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Instruction text
            var instrGO = new GameObject("Instruction", typeof(RectTransform), typeof(TextMeshProUGUI));
            instrGO.transform.SetParent(colonyCardPanel.transform, false);
            var instrRect = instrGO.GetComponent<RectTransform>();
            instrRect.anchorMin = new Vector2(0.05f, 0.90f);
            instrRect.anchorMax = new Vector2(0.95f, 0.95f);
            instrRect.sizeDelta = Vector2.zero;
            colonyInstructionText = instrGO.GetComponent<TextMeshProUGUI>();
            colonyInstructionText.text = "Select a card, then click a highlighted node";
            colonyInstructionText.fontSize = 11;
            colonyInstructionText.color = new Color(0.7f, 0.7f, 0.7f);
            colonyInstructionText.alignment = TextAlignmentOptions.Center;

            // Scrollable card list (top 50%)
            var scrollGO = new GameObject("ColonyScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(colonyCardPanel.transform, false);
            var scrollRect = scrollGO.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.03f, 0.45f);
            scrollRect.anchorMax = new Vector2(0.97f, 0.89f);
            scrollRect.sizeDelta = Vector2.zero;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(scrollGO.transform, false);
            colonyCardListContainer = contentGO.GetComponent<RectTransform>();
            colonyCardListContainer.anchorMin = new Vector2(0, 1);
            colonyCardListContainer.anchorMax = new Vector2(1, 1);
            colonyCardListContainer.pivot = new Vector2(0.5f, 1);
            colonyCardListContainer.sizeDelta = new Vector2(0, 0);

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.content = colonyCardListContainer;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scrollGO.AddComponent<RectMask2D>();

            // Card detail panel (bottom 42%)
            var detailBg = new GameObject("DetailBg", typeof(RectTransform), typeof(Image));
            detailBg.transform.SetParent(colonyCardPanel.transform, false);
            var detailBgRect = detailBg.GetComponent<RectTransform>();
            detailBgRect.anchorMin = new Vector2(0.03f, 0.02f);
            detailBgRect.anchorMax = new Vector2(0.97f, 0.43f);
            detailBgRect.sizeDelta = Vector2.zero;
            detailBg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.9f);

            // Artwork (left side of detail)
            var artGO = new GameObject("DetailArt", typeof(RectTransform), typeof(Image));
            artGO.transform.SetParent(detailBg.transform, false);
            var artRect = artGO.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0.03f, 0.05f);
            artRect.anchorMax = new Vector2(0.40f, 0.95f);
            artRect.sizeDelta = Vector2.zero;
            colonyDetailArt = artGO.GetComponent<Image>();
            colonyDetailArt.color = new Color(0.12f, 0.12f, 0.15f, 0.5f);
            colonyDetailArt.preserveAspect = true;
            colonyDetailArt.raycastTarget = false;

            // Card name (right of artwork, top)
            var nameGO = new GameObject("DetailName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(detailBg.transform, false);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.43f, 0.72f);
            nameRect.anchorMax = new Vector2(0.97f, 0.95f);
            nameRect.sizeDelta = Vector2.zero;
            colonyDetailName = nameGO.GetComponent<TextMeshProUGUI>();
            colonyDetailName.text = "";
            colonyDetailName.fontSize = 14;
            colonyDetailName.fontStyle = FontStyles.Bold;
            colonyDetailName.color = new Color(0.9f, 0.75f, 0.1f);
            colonyDetailName.alignment = TextAlignmentOptions.TopLeft;

            // Card description (right of artwork, middle)
            var descGO = new GameObject("DetailDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGO.transform.SetParent(detailBg.transform, false);
            var descRect = descGO.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.43f, 0.25f);
            descRect.anchorMax = new Vector2(0.97f, 0.72f);
            descRect.sizeDelta = Vector2.zero;
            colonyDetailDesc = descGO.GetComponent<TextMeshProUGUI>();
            colonyDetailDesc.text = "Select a card to view details";
            colonyDetailDesc.fontSize = 11;
            colonyDetailDesc.color = Color.white;
            colonyDetailDesc.alignment = TextAlignmentOptions.TopLeft;
            colonyDetailDesc.richText = true;

            // Placement requirement (right of artwork, bottom)
            var reqGO = new GameObject("DetailReq", typeof(RectTransform), typeof(TextMeshProUGUI));
            reqGO.transform.SetParent(detailBg.transform, false);
            var reqRect = reqGO.GetComponent<RectTransform>();
            reqRect.anchorMin = new Vector2(0.43f, 0.02f);
            reqRect.anchorMax = new Vector2(0.97f, 0.25f);
            reqRect.sizeDelta = Vector2.zero;
            colonyDetailReq = reqGO.GetComponent<TextMeshProUGUI>();
            colonyDetailReq.text = "";
            colonyDetailReq.fontSize = 10;
            colonyDetailReq.fontStyle = FontStyles.Italic;
            colonyDetailReq.color = new Color(0.6f, 0.9f, 0.6f);
            colonyDetailReq.alignment = TextAlignmentOptions.TopLeft;

            colonyCardPanel.SetActive(false);
            Debug.Log("[HUDManager] BuildColonyCardPanel: complete (hidden)");
        }

        public void RefreshColonyCardList()
        {
            Debug.Log("[HUDManager] RefreshColonyCardList: refreshing colony card list");

            ClearColonyHighlights();

            foreach (var go in colonyCardListItems) if (go != null) Destroy(go);
            colonyCardListItems.Clear();
            SelectedColonyCard = null;

            // Clear detail panel
            if (colonyDetailName != null) colonyDetailName.text = "";
            if (colonyDetailDesc != null) colonyDetailDesc.text = "Select a card to view details";
            if (colonyDetailReq != null) colonyDetailReq.text = "";
            if (colonyDetailArt != null) { colonyDetailArt.sprite = null; colonyDetailArt.color = new Color(0.12f, 0.12f, 0.15f, 0.5f); }

            var deckManager = Object.FindAnyObjectByType<DeckManager>();
            if (deckManager == null)
            {
                Debug.LogWarning("[HUDManager] RefreshColonyCardList: DeckManager not found");
                return;
            }

            var available = deckManager.AvailableColonyCards;
            float y = 0;
            float itemH = 55f;
            float spacing = 4f;

            foreach (var card in available)
            {
                var itemGO = new GameObject($"Colony_{card.cardName}", typeof(RectTransform), typeof(Image), typeof(Button));
                itemGO.transform.SetParent(colonyCardListContainer, false);
                var itemRect = itemGO.GetComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 1);
                itemRect.anchorMax = new Vector2(1, 1);
                itemRect.pivot = new Vector2(0.5f, 1);
                itemRect.anchoredPosition = new Vector2(0, -y);
                itemRect.sizeDelta = new Vector2(-6, itemH);
                itemGO.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.06f, 0.9f);

                // Card artwork thumbnail
                float textLeft = 0.05f;
                if (card.artwork != null)
                {
                    var artGO = new GameObject("Art", typeof(RectTransform), typeof(Image));
                    artGO.transform.SetParent(itemGO.transform, false);
                    var artRect = artGO.GetComponent<RectTransform>();
                    artRect.anchorMin = new Vector2(0.02f, 0.05f);
                    artRect.anchorMax = new Vector2(0.02f, 0.95f);
                    artRect.pivot = new Vector2(0, 0.5f);
                    artRect.anchoredPosition = Vector2.zero;
                    artRect.sizeDelta = new Vector2(45, 0);
                    var artImg = artGO.GetComponent<Image>();
                    artImg.sprite = card.artwork;
                    artImg.preserveAspect = true;
                    artImg.raycastTarget = false;
                    textLeft = 0.30f;
                }

                var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(itemGO.transform, false);
                var txtRect = txtGO.GetComponent<RectTransform>();
                txtRect.anchorMin = new Vector2(textLeft, 0);
                txtRect.anchorMax = new Vector2(0.95f, 1);
                txtRect.sizeDelta = Vector2.zero;
                var tmp = txtGO.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{card.cardName}</b>\n<size=10>{card.colonyEffect}: +{card.effectValue}</size>";
                tmp.fontSize = 12;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.richText = true;

                var captured = card;
                itemGO.GetComponent<Button>().onClick.AddListener(() => SelectColonyCard(captured, itemGO));
                colonyCardListItems.Add(itemGO);
                y += itemH + spacing;
            }
            colonyCardListContainer.sizeDelta = new Vector2(0, y);

            if (colonyInstructionText != null)
            {
                colonyInstructionText.text = available.Count > 0
                    ? "Select a card, then click a colony node"
                    : "No colony cards available";
            }

            Debug.Log($"[HUDManager] RefreshColonyCardList: displayed {available.Count} cards");
        }

        private void SelectColonyCard(ColonyCardDefinitionSO card, GameObject itemGO)
        {
            Debug.Log($"[HUDManager] SelectColonyCard: selected '{card.cardName}' (effect={card.colonyEffect}, value={card.effectValue})");
            SelectedColonyCard = card;

            // Highlight selected card in list, dim others
            foreach (var go in colonyCardListItems)
            {
                var img = go.GetComponent<Image>();
                if (img != null)
                    img.color = go == itemGO
                        ? new Color(0.25f, 0.35f, 0.10f, 1f)
                        : new Color(0.15f, 0.12f, 0.06f, 0.9f);
            }

            // Update detail panel
            if (colonyDetailName != null)
                colonyDetailName.text = card.cardName;
            if (colonyDetailDesc != null)
                colonyDetailDesc.text = $"{card.description}\n\n<size=10>Effect: {card.colonyEffect} +{card.effectValue}\nCost: {card.deckCost}  |  Tier: {card.colonyTier}</size>";
            if (colonyDetailReq != null)
            {
                if (card.placementRequirement == PlacementRequirement.None)
                    colonyDetailReq.text = "Placement: Any empty colony node";
                else
                    colonyDetailReq.text = $"Placement: Adjacent to {card.adjacencyCardName}";
            }
            if (colonyDetailArt != null)
            {
                if (card.artwork != null)
                {
                    colonyDetailArt.sprite = card.artwork;
                    colonyDetailArt.color = Color.white;
                }
                else
                {
                    colonyDetailArt.sprite = null;
                    colonyDetailArt.color = new Color(0.12f, 0.12f, 0.15f, 0.5f);
                }
            }

            if (colonyInstructionText != null)
                colonyInstructionText.text = $"Click a highlighted colony node to place";

            // Highlight valid placement nodes on the map
            HighlightValidColonyNodes(card);
        }

        private void HighlightValidColonyNodes(ColonyCardDefinitionSO card)
        {
            // Clear previous highlights
            ClearColonyHighlights();

            var mapGraph = ServiceLocator.Get<Scurry.Map.MapGraph>();
            var colonyGraph = ServiceLocator.Get<IColonyGraph>();
            if (mapGraph == null || colonyGraph == null) return;

            var mapRenderer = Object.FindAnyObjectByType<MapRenderer>();

            var emptyNodes = mapGraph.GetEmptyColonyNodes();
            foreach (var node in emptyNodes)
            {
                // Must be adjacent to at least one occupied colony node
                bool adjacentToOccupied = false;
                foreach (var nid in node.neighborIds)
                {
                    var neighbor = mapGraph.GetNode(nid);
                    if (neighbor != null && neighbor.IsColonyNode && neighbor.HasColonyCard)
                    {
                        adjacentToOccupied = true;
                        break;
                    }
                }
                if (!adjacentToOccupied) continue;

                // Check AdjacentTo requirement
                if (card.placementRequirement == PlacementRequirement.AdjacentTo &&
                    !string.IsNullOrEmpty(card.adjacencyCardName))
                {
                    bool reqMet = false;
                    foreach (var nid in node.neighborIds)
                    {
                        var neighbor = mapGraph.GetNode(nid);
                        if (neighbor != null && neighbor.HasColonyCard &&
                            neighbor.placedColonyCard.cardName == card.adjacencyCardName)
                        {
                            reqMet = true;
                            break;
                        }
                    }
                    if (!reqMet) continue;
                }

                highlightedColonyNodes.Add(node.nodeId);
            }

            // Apply highlight color on MapRenderer
            if (mapRenderer != null)
            {
                mapRenderer.HighlightNodes(highlightedColonyNodes, new Color(0.4f, 1f, 0.4f, 0.9f));
            }

            Debug.Log($"[HUDManager] HighlightValidColonyNodes: highlighted {highlightedColonyNodes.Count} valid nodes for '{card.cardName}'");
        }

        /// <summary>
        /// Shows info for an already-placed colony card in the detail panel.
        /// Called when the player clicks a colony node that has a card on it.
        /// </summary>
        public void ShowPlacedColonyCardInfo(ColonyCardDefinitionSO card)
        {
            Debug.Log($"[HUDManager] ShowPlacedColonyCardInfo: card={card?.cardName ?? "NULL"}");
            if (card == null) return;

            if (colonyDetailName != null)
                colonyDetailName.text = card.cardName;
            if (colonyDetailDesc != null)
                colonyDetailDesc.text = $"{card.description}\n\n<size=10>Effect: {card.colonyEffect} +{card.effectValue}\nCost: {card.deckCost}  |  Tier: {card.colonyTier}</size>";
            if (colonyDetailReq != null)
                colonyDetailReq.text = "<color=#88aaff>Placed on map</color>";
            if (colonyDetailArt != null)
            {
                if (card.artwork != null)
                {
                    colonyDetailArt.sprite = card.artwork;
                    colonyDetailArt.color = Color.white;
                }
                else
                {
                    colonyDetailArt.sprite = null;
                    colonyDetailArt.color = new Color(0.12f, 0.12f, 0.15f, 0.5f);
                }
            }

            // Also show the panel if it's hidden (e.g., during non-ColonyDeploy phases)
            if (colonyCardPanel != null && !colonyCardPanel.activeSelf)
                colonyCardPanel.SetActive(true);
        }

        private void ClearColonyHighlights()
        {
            if (highlightedColonyNodes.Count == 0) return;

            var mapRenderer = Object.FindAnyObjectByType<MapRenderer>();
            if (mapRenderer != null)
                mapRenderer.ClearNodeHighlights(highlightedColonyNodes);
            highlightedColonyNodes.Clear();
        }

        private string FormatPhaseName(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.ColonyDeploy => "Colony Deploy",
                GamePhase.Deploy => "Deploy Heroes",
                GamePhase.HeroMove => "Hero Move Phase",
                GamePhase.EnemyMove => "Enemy Move Phase",
                GamePhase.Combat => "Combat Phase",
                GamePhase.Gather => "Gathering Phase",
                GamePhase.Cleanup => "Cleanup Phase",
                _ => phase.ToString()
            };
        }
    }
}
