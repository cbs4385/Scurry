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

        private TextMeshProUGUI phaseLabel;
        private TextMeshProUGUI colonyHPText;
        private Image colonyHPFill;
        private Button endTurnButton;

        // Tooltip
        private GameObject tooltipPanel;
        private TextMeshProUGUI tooltipText;

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
        }

        private void OnDisable()
        {
            Debug.Log("[UIManager] OnDisable: unsubscribing from EventBus events");
            EventBus.OnPhaseChanged -= UpdatePhaseUI;
            EventBus.OnTileHovered -= ShowTooltip;
            EventBus.OnTileUnhovered -= HideTooltip;
        }

        private void Update()
        {
            UpdateColonyHP();
            UpdateTooltipPosition();
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
            var phaseLabelGO = CreateUIText("PhaseLabel", "DEPLOY", 48, TextAlignmentOptions.Center);
            var phaseRect = phaseLabelGO.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0.5f, 1f);
            phaseRect.anchorMax = new Vector2(0.5f, 1f);
            phaseRect.pivot = new Vector2(0.5f, 1f);
            phaseRect.anchoredPosition = new Vector2(0, -20);
            phaseRect.sizeDelta = new Vector2(400, 60);
            phaseLabel = phaseLabelGO.GetComponent<TextMeshProUGUI>();
            Debug.Log("[UIManager] BuildUI: PhaseLabel created");

            // Colony HP Text - top left
            var hpTextGO = CreateUIText("ColonyHP", "Colony HP: 20/50", 28, TextAlignmentOptions.Left);
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
            colonyHPFill.type = Image.Type.Filled;
            colonyHPFill.fillMethod = Image.FillMethod.Horizontal;
            Debug.Log("[UIManager] BuildUI: HP bar created");

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

            var btnTextGO = CreateUIText("BtnText", "END TURN", 24, TextAlignmentOptions.Center);
            btnTextGO.transform.SetParent(buttonGO.transform, false);
            var btnTextRect = btnTextGO.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            endTurnButton = buttonGO.GetComponent<Button>();
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
            Debug.Log("[UIManager] BuildUI: End Turn button created");

            // Legend - bottom left
            BuildLegend();

            // Tooltip - hidden by default
            BuildTooltip();

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
            panelRect.sizeDelta = new Vector2(200, 170);
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
            titleTmp.text = "TILE LEGEND";
            titleTmp.fontSize = 14;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;

            // Legend entries — each gets two lines: name and description
            AddLegendEntry(legendPanel.transform, 0, new Color(0.4f, 0.7f, 0.4f), "Normal", "Place heroes or resources");
            AddLegendEntry(legendPanel.transform, 1, new Color(0.9f, 0.85f, 0.3f), "Resource Node", "Place heroes here");
            AddLegendEntry(legendPanel.transform, 2, new Color(0.85f, 0.3f, 0.3f), "Enemy Patrol", "Heroes must fight to pass");
            AddLegendEntry(legendPanel.transform, 3, new Color(0.3f, 0.3f, 0.3f), "Hazard", "Impassable terrain");

            Debug.Log("[UIManager] BuildLegend: legend panel created with 4 entries");
        }

        private void AddLegendEntry(Transform parent, int index, Color swatchColor, string label, string desc)
        {
            float entryHeight = 32f;
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
            descRect.sizeDelta = new Vector2(-36, 14);
            var descTmp = descGO.GetComponent<TextMeshProUGUI>();
            descTmp.text = desc;
            descTmp.fontSize = 11;
            descTmp.alignment = TextAlignmentOptions.Left;
            descTmp.color = new Color(0.8f, 0.8f, 0.8f);
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
                phaseLabel.text = phase.ToString().ToUpper();

            if (endTurnButton != null)
            {
                bool interactable = (phase == GamePhase.Deploy);
                endTurnButton.interactable = interactable;
                Debug.Log($"[UIManager] UpdatePhaseUI: endTurnButton.interactable={interactable}");
            }
        }

        private void UpdateColonyHP()
        {
            if (colonyManager == null) return;

            if (colonyHPText != null)
                colonyHPText.text = $"Colony HP: {colonyManager.CurrentHP}/{colonyManager.MaxHP}";

            if (colonyHPFill != null)
                colonyHPFill.fillAmount = (float)colonyManager.CurrentHP / colonyManager.MaxHP;
        }

        private void OnEndTurnClicked()
        {
            Debug.Log("[UIManager] OnEndTurnClicked: invoking EventBus.OnTurnEnded");
            EventBus.OnTurnEnded?.Invoke();
        }
    }
}
