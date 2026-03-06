using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Scurry.Data;
using Scurry.Core;
using Scurry.Colony;

namespace Scurry.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private ColonyManager colonyManager;
        [SerializeField] private RunManager runManager;

        private TextMeshProUGUI phaseLabel;
        private TextMeshProUGUI colonyHPText;
        private Image colonyHPFill;
        private Button endTurnButton;

        private TextMeshProUGUI currencyText;
        private Button undoButton;

        // Run progress
        private TextMeshProUGUI runProgressLabel;

        // Tooltip
        private GameObject tooltipPanel;
        private TextMeshProUGUI tooltipText;

        // Step choice
        private GameObject stepChoicePanel;
        private readonly List<GameObject> stepChoiceButtons = new List<GameObject>();

        // Notification toasts
        private RectTransform notificationContainer;
        private readonly List<NotificationEntry> activeNotifications = new List<NotificationEntry>();
        private const float NotificationDuration = 2.5f;
        private const float NotificationFadeDuration = 0.5f;
        private const float NotificationHeight = 36f;
        private const float NotificationSpacing = 4f;

        private class NotificationEntry
        {
            public GameObject go;
            public CanvasGroup canvasGroup;
            public float spawnTime;
        }

        private void Awake()
        {
            Debug.Log($"[UIManager] Awake: colonyManager={colonyManager?.name ?? "NULL"}");
            BuildUI();
        }

        private void OnEnable()
        {
            Debug.Log("[UIManager] OnEnable: subscribing to EventBus.OnPhaseChanged, OnTileHovered, OnTileUnhovered");
            EventBus.OnPhaseChanged += UpdatePhaseUI;
            EventBus.OnTileHovered += ShowTooltip;
            EventBus.OnTileUnhovered += HideTooltip;
            EventBus.OnGatheringNotification += SpawnNotification;
            EventBus.OnStageProgress += UpdateStageProgress;
            EventBus.OnRunComplete += OnRunComplete;
            EventBus.OnRunFailed += OnRunFailed;
            EventBus.OnStepChoicePresented += ShowStepChoices;
        }

        private void OnDisable()
        {
            Debug.Log("[UIManager] OnDisable: unsubscribing from EventBus events");
            EventBus.OnPhaseChanged -= UpdatePhaseUI;
            EventBus.OnTileHovered -= ShowTooltip;
            EventBus.OnTileUnhovered -= HideTooltip;
            EventBus.OnGatheringNotification -= SpawnNotification;
            EventBus.OnStageProgress -= UpdateStageProgress;
            EventBus.OnRunComplete -= OnRunComplete;
            EventBus.OnRunFailed -= OnRunFailed;
            EventBus.OnStepChoicePresented -= ShowStepChoices;
        }

        private void Update()
        {
            UpdateColonyHP();
            UpdateTooltipPosition();
            UpdateNotifications();
        }

        private void BuildUI()
        {
            Debug.Log("[UIManager] BuildUI: starting UI construction");
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[UIManager] BuildUI: no Canvas component found on this GameObject!");
                return;
            }
            Debug.Log($"[UIManager] BuildUI: canvas found — renderMode={canvas.renderMode}");

            // Phase Label - top center
            var phaseLabelGO = CreateUIText("PhaseLabel", Loc.Get("ui.phase.deploy"), 48, TextAlignmentOptions.Center);
            var phaseRect = phaseLabelGO.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0.5f, 1f);
            phaseRect.anchorMax = new Vector2(0.5f, 1f);
            phaseRect.pivot = new Vector2(0.5f, 1f);
            phaseRect.anchoredPosition = new Vector2(0, -20);
            phaseRect.sizeDelta = new Vector2(400, 60);
            phaseLabel = phaseLabelGO.GetComponent<TextMeshProUGUI>();
            Debug.Log("[UIManager] BuildUI: PhaseLabel created");

            // Run Progress Label - below phase label
            var runProgressGO = CreateUIText("RunProgressLabel", "", 20, TextAlignmentOptions.Center);
            var runRect = runProgressGO.GetComponent<RectTransform>();
            runRect.anchorMin = new Vector2(0.5f, 1f);
            runRect.anchorMax = new Vector2(0.5f, 1f);
            runRect.pivot = new Vector2(0.5f, 1f);
            runRect.anchoredPosition = new Vector2(0, -75);
            runRect.sizeDelta = new Vector2(500, 30);
            runProgressLabel = runProgressGO.GetComponent<TextMeshProUGUI>();
            runProgressLabel.color = new Color(0.8f, 0.8f, 1f);
            Debug.Log("[UIManager] BuildUI: RunProgressLabel created");

            // Colony HP Text - top left
            var hpTextGO = CreateUIText("ColonyHP", Loc.Format("ui.hp.label", 20, 50), 28, TextAlignmentOptions.Left);
            var hpRect = hpTextGO.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0, 1);
            hpRect.anchorMax = new Vector2(0, 1);
            hpRect.pivot = new Vector2(0, 1);
            hpRect.anchoredPosition = new Vector2(20, -20);
            hpRect.sizeDelta = new Vector2(350, 40);
            colonyHPText = hpTextGO.GetComponent<TextMeshProUGUI>();
            Debug.Log("[UIManager] BuildUI: ColonyHP text created");

            // HP Bar Background - below HP text
            var hpBarBG = CreateUIImage("HPBarBG", new Color(0.2f, 0.2f, 0.2f));
            var bgRect = hpBarBG.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 1);
            bgRect.anchorMax = new Vector2(0, 1);
            bgRect.pivot = new Vector2(0, 1);
            bgRect.anchoredPosition = new Vector2(20, -65);
            bgRect.sizeDelta = new Vector2(300, 20);

            // HP Bar Fill
            var hpBarFillGO = CreateUIImage("HPBarFill", new Color(0.2f, 0.8f, 0.2f));
            hpBarFillGO.transform.SetParent(hpBarBG.transform, false);
            var fillRect = hpBarFillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            colonyHPFill = hpBarFillGO.GetComponent<Image>();
            // Use anchors to control fill width instead of Image.Filled (which requires a sprite)
            colonyHPFill.type = Image.Type.Simple;
            // anchorMax.x will be set each frame in UpdateColonyHP()
            Debug.Log("[UIManager] BuildUI: HP fill using anchor-based width scaling");
            Debug.Log("[UIManager] BuildUI: HP bar created");

            // Currency Text - below HP bar
            var currencyTextGO = CreateUIText("CurrencyText", Loc.Format("ui.currency.label", 0), 22, TextAlignmentOptions.Left);
            var currencyRect = currencyTextGO.GetComponent<RectTransform>();
            currencyRect.anchorMin = new Vector2(0, 1);
            currencyRect.anchorMax = new Vector2(0, 1);
            currencyRect.pivot = new Vector2(0, 1);
            currencyRect.anchoredPosition = new Vector2(20, -95);
            currencyRect.sizeDelta = new Vector2(350, 30);
            currencyText = currencyTextGO.GetComponent<TextMeshProUGUI>();
            Debug.Log("[UIManager] BuildUI: Currency text created");

            // End Turn Button - bottom right
            var buttonGO = new GameObject("EndTurnButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(transform, false);
            var btnRect = buttonGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0);
            btnRect.anchorMax = new Vector2(1, 0);
            btnRect.pivot = new Vector2(1, 0);
            btnRect.anchoredPosition = new Vector2(-20, 20);
            btnRect.sizeDelta = new Vector2(200, 60);
            var btnImage = buttonGO.GetComponent<Image>();
            btnImage.color = new Color(0.3f, 0.6f, 0.3f);

            var btnTextGO = CreateUIText("BtnText", Loc.Get("ui.button.endturn"), 24, TextAlignmentOptions.Center);
            btnTextGO.transform.SetParent(buttonGO.transform, false);
            var btnTextRect = btnTextGO.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            endTurnButton = buttonGO.GetComponent<Button>();
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
            Debug.Log("[UIManager] BuildUI: End Turn button created");

            // Undo Button - above End Turn button
            var undoGO = new GameObject("UndoButton", typeof(RectTransform), typeof(Image), typeof(Button));
            undoGO.transform.SetParent(transform, false);
            var undoRect = undoGO.GetComponent<RectTransform>();
            undoRect.anchorMin = new Vector2(1, 0);
            undoRect.anchorMax = new Vector2(1, 0);
            undoRect.pivot = new Vector2(1, 0);
            undoRect.anchoredPosition = new Vector2(-20, 90);
            undoRect.sizeDelta = new Vector2(200, 50);
            var undoImage = undoGO.GetComponent<Image>();
            undoImage.color = new Color(0.6f, 0.4f, 0.2f);

            var undoTextGO = CreateUIText("UndoBtnText", Loc.Get("ui.button.undo"), 22, TextAlignmentOptions.Center);
            undoTextGO.transform.SetParent(undoGO.transform, false);
            var undoTextRect = undoTextGO.GetComponent<RectTransform>();
            undoTextRect.anchorMin = Vector2.zero;
            undoTextRect.anchorMax = Vector2.one;
            undoTextRect.sizeDelta = Vector2.zero;

            undoButton = undoGO.GetComponent<Button>();
            undoButton.onClick.AddListener(OnUndoClicked);
            undoGO.SetActive(false);
            Debug.Log("[UIManager] BuildUI: Undo button created (hidden)");

            // Legend - bottom left
            BuildLegend();

            // Tooltip - hidden by default
            BuildTooltip();

            // Step choice panel - center
            BuildStepChoicePanel();

            // Notification container - right side
            BuildNotificationContainer();

            Debug.Log("[UIManager] BuildUI: complete — all UI elements created");
        }

        private void BuildLegend()
        {
            // Legend panel background
            var legendPanel = new GameObject("LegendPanel", typeof(RectTransform), typeof(Image));
            legendPanel.transform.SetParent(transform, false);
            var panelRect = legendPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = new Vector2(15, 15);
            panelRect.sizeDelta = new Vector2(220, 210);
            var panelImage = legendPanel.GetComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.75f);

            // Title
            var titleGO = new GameObject("LegendTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(legendPanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -6);
            titleRect.sizeDelta = new Vector2(0, 22);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = Loc.Get("ui.legend.title");
            titleTmp.fontSize = 14;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;

            // Legend entries — each gets two lines: name and description
            AddLegendEntry(legendPanel.transform, 0, new Color(0.4f, 0.7f, 0.4f), Loc.Get("ui.legend.normal.name"), Loc.Get("ui.legend.normal.desc"));
            AddLegendEntry(legendPanel.transform, 1, new Color(0.9f, 0.85f, 0.3f), Loc.Get("ui.legend.resource.name"), Loc.Get("ui.legend.resource.desc"));
            AddLegendEntry(legendPanel.transform, 2, new Color(0.85f, 0.3f, 0.3f), Loc.Get("ui.legend.enemy.name"), Loc.Get("ui.legend.enemy.desc"));
            AddLegendEntry(legendPanel.transform, 3, new Color(0.3f, 0.3f, 0.3f), Loc.Get("ui.legend.hazard.name"), Loc.Get("ui.legend.hazard.desc"));

            Debug.Log("[UIManager] BuildLegend: legend panel created with 4 entries");
        }

        private void AddLegendEntry(Transform parent, int index, Color swatchColor, string label, string desc)
        {
            float entryHeight = 40f;
            float yOffset = -32 - index * entryHeight;

            // Color swatch
            var swatchGO = new GameObject($"Swatch_{label}", typeof(RectTransform), typeof(Image));
            swatchGO.transform.SetParent(parent, false);
            var swatchRect = swatchGO.GetComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0, 1);
            swatchRect.anchorMax = new Vector2(0, 1);
            swatchRect.pivot = new Vector2(0, 0.5f);
            swatchRect.anchoredPosition = new Vector2(10, yOffset - 4);
            swatchRect.sizeDelta = new Vector2(14, 14);
            swatchGO.GetComponent<Image>().color = swatchColor;

            // Name (bold, larger)
            var nameGO = new GameObject($"Name_{label}", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(parent, false);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0, 1);
            nameRect.anchoredPosition = new Vector2(30, yOffset + 6);
            nameRect.sizeDelta = new Vector2(-36, 16);
            var nameTmp = nameGO.GetComponent<TextMeshProUGUI>();
            nameTmp.text = label;
            nameTmp.fontSize = 13;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.color = Color.white;

            // Description (smaller, gray)
            var descGO = new GameObject($"Desc_{label}", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGO.transform.SetParent(parent, false);
            var descRect = descGO.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0, 1);
            descRect.anchoredPosition = new Vector2(30, yOffset - 8);
            descRect.sizeDelta = new Vector2(-36, 20);
            var descTmp = descGO.GetComponent<TextMeshProUGUI>();
            descTmp.text = desc;
            descTmp.fontSize = 11;
            descTmp.alignment = TextAlignmentOptions.Left;
            descTmp.color = new Color(0.8f, 0.8f, 0.8f);
            descTmp.overflowMode = TextOverflowModes.Ellipsis;
            descTmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        }

        private void BuildTooltip()
        {
            tooltipPanel = new GameObject("TooltipPanel", typeof(RectTransform), typeof(Image));
            tooltipPanel.transform.SetParent(transform, false);
            var panelRect = tooltipPanel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0, 1);
            panelRect.sizeDelta = new Vector2(240, 70);
            var panelImage = tooltipPanel.GetComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            var textGO = new GameObject("TooltipText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(tooltipPanel.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            tooltipText = textGO.GetComponent<TextMeshProUGUI>();
            tooltipText.fontSize = 14;
            tooltipText.alignment = TextAlignmentOptions.TopLeft;
            tooltipText.color = Color.white;
            tooltipText.richText = true;

            tooltipPanel.SetActive(false);
            Debug.Log("[UIManager] BuildTooltip: tooltip panel created (hidden)");
        }

        private void ShowTooltip(string text)
        {
            if (tooltipPanel == null || tooltipText == null) return;
            Debug.Log($"[UIManager] ShowTooltip: text='{text}'");
            tooltipText.text = text;
            tooltipPanel.SetActive(true);
            UpdateTooltipPosition();
        }

        private void HideTooltip()
        {
            if (tooltipPanel == null) return;
            tooltipPanel.SetActive(false);
        }

        private void UpdateTooltipPosition()
        {
            if (tooltipPanel == null || !tooltipPanel.activeSelf) return;

            var rectTransform = tooltipPanel.GetComponent<RectTransform>();
            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            // Offset so tooltip doesn't cover the cursor
            float offsetX = 20f;
            float offsetY = -10f;

            // Clamp to screen bounds
            float panelW = rectTransform.sizeDelta.x;
            float panelH = rectTransform.sizeDelta.y;

            float x = mousePos.x + offsetX;
            float y = mousePos.y + offsetY;

            // Flip to the left if near right edge
            if (x + panelW > Screen.width)
                x = mousePos.x - panelW - 10f;
            // Flip above if near bottom edge
            if (y - panelH < 0)
                y = mousePos.y + panelH + 10f;

            rectTransform.position = new Vector3(x, y, 0);
        }

        private GameObject CreateUIText(string name, string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return go;
        }

        private GameObject CreateUIImage(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private void UpdatePhaseUI(GamePhase phase)
        {
            Debug.Log($"[UIManager] UpdatePhaseUI: phase={phase}");
            if (phaseLabel != null)
                phaseLabel.text = Loc.Get($"ui.phase.{phase.ToString().ToLower()}");

            bool isDeckBuild = (phase == GamePhase.DeckBuild);
            bool isDeploy = (phase == GamePhase.Deploy);
            if (endTurnButton != null)
            {
                endTurnButton.gameObject.SetActive(!isDeckBuild);
                endTurnButton.interactable = isDeploy;
                Debug.Log($"[UIManager] UpdatePhaseUI: endTurnButton.active={!isDeckBuild}, interactable={isDeploy}");
            }
            if (undoButton != null)
            {
                undoButton.gameObject.SetActive(isDeploy);
                Debug.Log($"[UIManager] UpdatePhaseUI: undoButton.active={isDeploy}");
            }
            // Hide step choice panel when a game phase starts
            if (stepChoicePanel != null && stepChoicePanel.activeSelf)
            {
                stepChoicePanel.SetActive(false);
                Debug.Log("[UIManager] UpdatePhaseUI: hiding step choice panel");
            }
            UpdateRunProgressLabel();
        }

        private void UpdateColonyHP()
        {
            if (colonyManager == null) return;

            if (colonyHPText != null)
                colonyHPText.text = Loc.Format("ui.hp.label", colonyManager.CurrentHP, colonyManager.MaxHP);

            if (colonyHPFill != null)
            {
                var fillRect = colonyHPFill.GetComponent<RectTransform>();
                float ratio = (float)colonyManager.CurrentHP / colonyManager.MaxHP;
                fillRect.anchorMax = new Vector2(ratio, 1f);
            }

            if (currencyText != null)
                currencyText.text = Loc.Format("ui.currency.label", colonyManager.CurrencyStockpile);
        }

        private void BuildNotificationContainer()
        {
            var containerGO = new GameObject("NotificationContainer", typeof(RectTransform));
            containerGO.transform.SetParent(transform, false);
            notificationContainer = containerGO.GetComponent<RectTransform>();
            notificationContainer.anchorMin = new Vector2(1, 0.5f);
            notificationContainer.anchorMax = new Vector2(1, 0.5f);
            notificationContainer.pivot = new Vector2(1, 0.5f);
            notificationContainer.anchoredPosition = new Vector2(-10, 0);
            notificationContainer.sizeDelta = new Vector2(320, 400);
            Debug.Log("[UIManager] BuildNotificationContainer: created right-side notification container");
        }

        private void SpawnNotification(string message, Color color)
        {
            Debug.Log($"[UIManager] SpawnNotification: message='{message}', color={color}");

            var panelGO = new GameObject("Notification", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panelGO.transform.SetParent(notificationContainer, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.sizeDelta = new Vector2(0, NotificationHeight);
            var panelImage = panelGO.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            // Color accent bar on left edge
            var accentGO = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentGO.transform.SetParent(panelGO.transform, false);
            var accentRect = accentGO.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 0);
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(4, 0);
            accentGO.GetComponent<Image>().color = color;

            // Text
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(panelGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 2);
            textRect.offsetMax = new Vector2(-6, -2);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = color;
            tmp.richText = true;

            var entry = new NotificationEntry
            {
                go = panelGO,
                canvasGroup = panelGO.GetComponent<CanvasGroup>(),
                spawnTime = Time.time
            };
            activeNotifications.Add(entry);
            LayoutNotifications();
        }

        private void UpdateNotifications()
        {
            float now = Time.time;
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                var entry = activeNotifications[i];
                float age = now - entry.spawnTime;

                if (age > NotificationDuration + NotificationFadeDuration)
                {
                    Destroy(entry.go);
                    activeNotifications.RemoveAt(i);
                    LayoutNotifications();
                }
                else if (age > NotificationDuration)
                {
                    float fadeProgress = (age - NotificationDuration) / NotificationFadeDuration;
                    entry.canvasGroup.alpha = 1f - fadeProgress;
                }
            }
        }

        private void LayoutNotifications()
        {
            float yOffset = 0;
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                var rect = activeNotifications[i].go.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0, -yOffset);
                yOffset += NotificationHeight + NotificationSpacing;
            }
        }

        private void BuildStepChoicePanel()
        {
            stepChoicePanel = new GameObject("StepChoicePanel", typeof(RectTransform), typeof(Image));
            stepChoicePanel.transform.SetParent(transform, false);
            var panelRect = stepChoicePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(500, 220);
            var panelImage = stepChoicePanel.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // Title
            var titleGO = new GameObject("ChoiceTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(stepChoicePanel.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 40);
            var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
            titleTmp.text = Loc.Get("run.step.choose");
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.9f, 0.3f);

            stepChoicePanel.SetActive(false);
            Debug.Log("[UIManager] BuildStepChoicePanel: created (hidden)");
        }

        private void ShowStepChoices(StepType[] options)
        {
            Debug.Log($"[UIManager] ShowStepChoices: presenting {options.Length} options");

            // Clear old buttons
            foreach (var btn in stepChoiceButtons)
                Destroy(btn);
            stepChoiceButtons.Clear();

            float buttonWidth = 180f;
            float buttonHeight = 60f;
            float spacing = 20f;
            float totalWidth = options.Length * buttonWidth + (options.Length - 1) * spacing;
            float startX = -totalWidth / 2f + buttonWidth / 2f;

            for (int i = 0; i < options.Length; i++)
            {
                StepType step = options[i];
                string stepName = Loc.Get("run.step." + step.ToString().ToLower());

                var btnGO = new GameObject($"StepChoice_{step}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGO.transform.SetParent(stepChoicePanel.transform, false);
                var btnRect = btnGO.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.5f, 0);
                btnRect.anchorMax = new Vector2(0.5f, 0);
                btnRect.pivot = new Vector2(0.5f, 0);
                btnRect.anchoredPosition = new Vector2(startX + i * (buttonWidth + spacing), 30);
                btnRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                var btnImage = btnGO.GetComponent<Image>();
                btnImage.color = GetStepButtonColor(step);

                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGO.transform.SetParent(btnGO.transform, false);
                var textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                var tmp = textGO.GetComponent<TextMeshProUGUI>();
                tmp.text = stepName;
                tmp.fontSize = 18;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                var button = btnGO.GetComponent<Button>();
                StepType captured = step;
                button.onClick.AddListener(() => OnStepChoiceClicked(captured));

                stepChoiceButtons.Add(btnGO);
                Debug.Log($"[UIManager] ShowStepChoices: button {i} = {step} ('{stepName}')");
            }

            stepChoicePanel.SetActive(true);
        }

        private void OnStepChoiceClicked(StepType choice)
        {
            Debug.Log($"[UIManager] OnStepChoiceClicked: player chose {choice}");
            stepChoicePanel.SetActive(false);
            EventBus.OnStepChosen?.Invoke(choice);
        }

        private Color GetStepButtonColor(StepType step)
        {
            return step switch
            {
                StepType.CardPlacement => new Color(0.3f, 0.5f, 0.7f),
                StepType.Shop => new Color(0.6f, 0.5f, 0.2f),
                StepType.Healing => new Color(0.3f, 0.6f, 0.3f),
                StepType.CardAddRemove => new Color(0.5f, 0.3f, 0.6f),
                StepType.BossFight => new Color(0.7f, 0.2f, 0.2f),
                _ => new Color(0.4f, 0.4f, 0.4f)
            };
        }

        private void OnUndoClicked()
        {
            Debug.Log("[UIManager] OnUndoClicked: invoking EventBus.OnUndoPlacement");
            EventBus.OnUndoPlacement?.Invoke();
        }

        private void OnEndTurnClicked()
        {
            Debug.Log("[UIManager] OnEndTurnClicked: invoking EventBus.OnTurnEnded");
            EventBus.OnTurnEnded?.Invoke();
        }

        private void UpdateStageProgress(int currentStep, int totalSteps)
        {
            UpdateRunProgressLabel();
        }

        private void UpdateRunProgressLabel()
        {
            if (runProgressLabel == null || runManager == null) return;
            if (!runManager.enabled) { runProgressLabel.text = ""; return; }

            var zone = runManager.CurrentZone;
            if (zone == null) return;

            int stage = runManager.CurrentStageIndex + 1;
            int totalStages = zone.stagesPerZone;
            string text = $"Stage {stage}/{totalStages}";
            runProgressLabel.text = text;
            Debug.Log($"[UIManager] UpdateRunProgressLabel: '{text}'");
        }

        private void OnRunComplete()
        {
            if (runProgressLabel != null)
            {
                runProgressLabel.text = Loc.Get("run.complete.victory");
                runProgressLabel.color = new Color(1f, 0.9f, 0.3f);
            }
        }

        private void OnRunFailed()
        {
            if (runProgressLabel != null)
            {
                runProgressLabel.text = Loc.Get("run.complete.defeat");
                runProgressLabel.color = new Color(1f, 0.2f, 0.2f);
            }
        }
    }
}
