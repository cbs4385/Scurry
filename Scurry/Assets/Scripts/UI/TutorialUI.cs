using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Scurry.UI
{
    /// <summary>
    /// Tutorial tooltip overlay with highlight mask. Shows contextual tips during the tutorial.
    /// Built programmatically — same pattern as other Scurry UI.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject tipPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private Button dismissBtn;
        private Action onDismiss;

        private void Awake()
        {
            Debug.Log("[TutorialUI] Awake: initializing");
            BuildUI();
        }

        private void BuildUI()
        {
            Debug.Log("[TutorialUI] BuildUI: starting construction");

            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400; // Above everything

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Tip panel — bottom-center tooltip
            tipPanel = new GameObject("TipPanel", typeof(RectTransform), typeof(Image));
            tipPanel.transform.SetParent(transform, false);
            var tipRect = tipPanel.GetComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.15f, 0.02f);
            tipRect.anchorMax = new Vector2(0.85f, 0.22f);
            tipRect.sizeDelta = Vector2.zero;
            tipPanel.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.15f, 0.95f);

            // Highlight border
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(tipPanel.transform, false);
            var borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            var borderImg = border.GetComponent<Image>();
            borderImg.color = new Color(1f, 0.8f, 0.2f, 0.3f);
            borderImg.raycastTarget = false;

            // Inner panel (smaller to create border effect)
            var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(tipPanel.transform, false);
            var innerRect = inner.GetComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.005f, 0.02f);
            innerRect.anchorMax = new Vector2(0.995f, 0.98f);
            innerRect.sizeDelta = Vector2.zero;
            inner.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.15f, 0.98f);

            // Title
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(inner.transform, false);
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.03f, 0.70f);
            titleRect.anchorMax = new Vector2(0.80f, 0.95f);
            titleRect.sizeDelta = Vector2.zero;
            titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.text = "Tutorial";
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = new Color(1f, 0.9f, 0.3f);

            // Body
            var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGO.transform.SetParent(inner.transform, false);
            var bodyRect = bodyGO.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.03f, 0.10f);
            bodyRect.anchorMax = new Vector2(0.80f, 0.70f);
            bodyRect.sizeDelta = Vector2.zero;
            bodyText = bodyGO.GetComponent<TextMeshProUGUI>();
            bodyText.text = "";
            bodyText.fontSize = 16;
            bodyText.alignment = TextAlignmentOptions.Left;
            bodyText.color = new Color(0.9f, 0.9f, 0.95f);
            bodyText.richText = true;

            // Dismiss button
            var btnGO = new GameObject("DismissBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(inner.transform, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.82f, 0.25f);
            btnRect.anchorMax = new Vector2(0.97f, 0.75f);
            btnRect.sizeDelta = Vector2.zero;
            btnGO.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.3f);
            dismissBtn = btnGO.GetComponent<Button>();
            dismissBtn.onClick.AddListener(OnDismissClicked);

            var btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRect = btnTextGO.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            var btnTmp = btnTextGO.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "Got it!";
            btnTmp.fontSize = 16;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;

            tipPanel.SetActive(false);
            Debug.Log("[TutorialUI] BuildUI: complete (panel hidden)");
        }

        /// <summary>
        /// Shows a tutorial tip with a title, message, and dismiss callback.
        /// </summary>
        public void ShowTip(string title, string message, Action onDismissCallback = null)
        {
            Debug.Log($"[TutorialUI] ShowTip: title='{title}', message length={message?.Length ?? 0}");

            onDismiss = onDismissCallback;

            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = message;

            if (tipPanel != null) tipPanel.SetActive(true);
        }

        /// <summary>
        /// Hides the current tutorial tip.
        /// </summary>
        public void HideTip()
        {
            Debug.Log("[TutorialUI] HideTip: hiding tip panel");
            if (tipPanel != null) tipPanel.SetActive(false);
            onDismiss = null;
        }

        private void OnDismissClicked()
        {
            Debug.Log("[TutorialUI] OnDismissClicked: dismiss button pressed");
            var callback = onDismiss;
            HideTip();
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            Debug.Log("[TutorialUI] OnDestroy: cleaning up");
        }
    }
}
