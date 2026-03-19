using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Scurry.Core;

namespace Scurry.UI
{
    /// <summary>
    /// Singleton tooltip panel that follows the mouse cursor.
    /// Shows text content near the mouse position, keeps itself on-screen,
    /// and auto-hides after a configurable delay.
    /// All UI built programmatically (no prefabs).
    /// </summary>
    public class TooltipUI : MonoBehaviour
    {
        // ── Constants ───────────────────────────────────────────────────
        private const float AutoHideDelay = 5.0f;
        private const float OffsetX = 20f;
        private const float OffsetY = -10f;
        private const float MaxWidth = 320f;
        private const float Padding = 12f;

        // ── Singleton ───────────────────────────────────────────────────
        private static TooltipUI instance;

        // ── UI References ───────────────────────────────────────────────
        private Canvas canvas;
        private GameObject panel;
        private RectTransform panelRect;
        private TextMeshProUGUI text;
        private Image backgroundImage;

        // ── State ───────────────────────────────────────────────────────
        private float showTime;
        private bool isShowing;

        private void Awake()
        {
            Debug.Log("[TooltipUI] Awake: initializing singleton tooltip");

            if (instance != null && instance != this)
            {
                Debug.Log("[TooltipUI] Awake: duplicate instance found, destroying component only");
                Destroy(this);
                return;
            }

            instance = this;
            if (gameObject.name == "PersistentManagers")
                DontDestroyOnLoad(gameObject);
            BuildUI();
            Debug.Log("[TooltipUI] Awake: singleton set, DontDestroyOnLoad applied");
        }

        private void OnEnable()
        {
            Debug.Log("[TooltipUI] OnEnable: subscribing to EventBus tooltip events");
            EventBus.OnTooltipShow += HandleTooltipShow;
            EventBus.OnTooltipHide += HandleTooltipHide;
        }

        private void OnDisable()
        {
            Debug.Log("[TooltipUI] OnDisable: unsubscribing from EventBus tooltip events");
            EventBus.OnTooltipShow -= HandleTooltipShow;
            EventBus.OnTooltipHide -= HandleTooltipHide;
        }

        private void OnDestroy()
        {
            Debug.Log("[TooltipUI] OnDestroy: cleaning up singleton reference");
            if (instance == this)
            {
                instance = null;
                Debug.Log("[TooltipUI] OnDestroy: singleton reference cleared");
            }
        }

        private void Update()
        {
            if (!isShowing) return;

            // Auto-hide after delay
            float elapsed = Time.time - showTime;
            if (elapsed > AutoHideDelay)
            {
                Debug.Log($"[TooltipUI] Update: auto-hiding after {elapsed:F1}s (limit={AutoHideDelay}s)");
                HideInternal();
                return;
            }

            // Follow mouse position, keep on screen
            UpdatePosition();
        }

        // ── Build ───────────────────────────────────────────────────────

        private void BuildUI()
        {
            Debug.Log("[TooltipUI] BuildUI: starting construction");

            // Canvas at highest sorting order so tooltip is always on top
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                Debug.Log("[TooltipUI] BuildUI: added new Canvas component");
            }
            else
            {
                Debug.Log("[TooltipUI] BuildUI: reusing existing Canvas component");
            }
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 9999;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // No GraphicRaycaster -- tooltip should not block clicks
            Debug.Log("[TooltipUI] BuildUI: canvas created (sortingOrder=9999, no raycaster)");

            // Tooltip panel with ContentSizeFitter for auto-height
            panel = new GameObject("TooltipPanel", typeof(RectTransform), typeof(Image), typeof(ContentSizeFitter));
            panel.transform.SetParent(transform, false);
            panelRect = panel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0, 1); // top-left pivot for positioning
            panelRect.sizeDelta = new Vector2(MaxWidth, 60);

            backgroundImage = panel.GetComponent<Image>();
            backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            backgroundImage.raycastTarget = false;

            var csf = panel.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Debug.Log("[TooltipUI] BuildUI: panel created with ContentSizeFitter");

            // Tooltip text
            var textGO = new GameObject("TooltipText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(panel.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(Padding, Padding * 0.5f);
            textRect.offsetMax = new Vector2(-Padding, -Padding * 0.5f);

            text = textGO.GetComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white;
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            Debug.Log("[TooltipUI] BuildUI: text element created");

            // Start hidden
            panel.SetActive(false);
            isShowing = false;

            Debug.Log("[TooltipUI] BuildUI: complete (tooltip hidden)");
        }

        // ── Static API ──────────────────────────────────────────────────

        /// <summary>
        /// Shows the tooltip with the given text content. Follows the mouse cursor.
        /// Creates the singleton instance automatically if it does not exist.
        /// </summary>
        public static void Show(string content)
        {
            Debug.Log($"[TooltipUI] Show(static): content='{content}'");

            EnsureInstance();

            if (instance == null)
            {
                Debug.LogWarning("[TooltipUI] Show(static): failed to create or find instance");
                return;
            }

            instance.ShowInternal(content);
        }

        /// <summary>
        /// Shows the tooltip at a specific screen position.
        /// </summary>
        public static void Show(string content, Vector2 screenPos)
        {
            Debug.Log($"[TooltipUI] Show(static): content='{content}', screenPos={screenPos}");

            EnsureInstance();

            if (instance == null)
            {
                Debug.LogWarning("[TooltipUI] Show(static): failed to create or find instance");
                return;
            }

            instance.ShowInternal(content);
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public static void Hide()
        {
            Debug.Log("[TooltipUI] Hide(static): hiding tooltip");

            if (instance != null)
            {
                instance.HideInternal();
            }
            else
            {
                Debug.Log("[TooltipUI] Hide(static): no instance to hide");
            }
        }

        // ── Instance Methods ────────────────────────────────────────────

        private void ShowInternal(string content)
        {
            Debug.Log($"[TooltipUI] ShowInternal: content='{content}', contentLength={content?.Length ?? 0}");

            if (text != null)
            {
                text.text = content;
            }

            showTime = Time.time;
            isShowing = true;

            if (panel != null)
            {
                panel.SetActive(true);
            }

            // Force layout rebuild for ContentSizeFitter
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

            UpdatePosition();
            Debug.Log("[TooltipUI] ShowInternal: tooltip shown and positioned");
        }

        private void HideInternal()
        {
            Debug.Log("[TooltipUI] HideInternal: hiding tooltip panel");

            isShowing = false;

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void UpdatePosition()
        {
            if (panelRect == null) return;

            // Read mouse position from Input System
            Vector2 mousePos = Vector2.zero;
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
            }
            else
            {
                // Mouse.current is null — no mouse connected or Input System not initialized
                Debug.Log("[TooltipUI] UpdatePosition: Mouse.current is null, using zero position");
            }

            float x = mousePos.x + OffsetX;
            float y = mousePos.y + OffsetY;

            float panelW = panelRect.sizeDelta.x;
            float panelH = panelRect.sizeDelta.y;

            // Flip to left side if tooltip would go off right edge
            if (x + panelW > Screen.width)
            {
                x = mousePos.x - panelW - 10f;
            }

            // Flip above cursor if tooltip would go off bottom edge
            if (y - panelH < 0)
            {
                y = mousePos.y + panelH + 10f;
            }

            // Clamp to ensure fully on screen
            x = Mathf.Clamp(x, 0, Screen.width - panelW);
            y = Mathf.Clamp(y, panelH, Screen.height);

            panelRect.position = new Vector3(x, y, 0);
        }

        // ── EventBus Handlers ────────────────────────────────────────────

        private void HandleTooltipShow(string content)
        {
            Debug.Log($"[TooltipUI] HandleTooltipShow: received EventBus.OnTooltipShow (content='{content}', contentLength={content?.Length ?? 0})");
            ShowInternal(content);
        }

        private void HandleTooltipHide()
        {
            Debug.Log("[TooltipUI] HandleTooltipHide: received EventBus.OnTooltipHide");
            HideInternal();
        }

        // ── Singleton Utility ───────────────────────────────────────────

        private static void EnsureInstance()
        {
            if (instance != null) return;

            Debug.Log("[TooltipUI] EnsureInstance: no instance found, creating new TooltipUI GO");

            var go = new GameObject("TooltipUI_Singleton");
            instance = go.AddComponent<TooltipUI>();

            // DontDestroyOnLoad is called in Awake, but we call it here too for safety
            DontDestroyOnLoad(go);

            Debug.Log("[TooltipUI] EnsureInstance: singleton created with DontDestroyOnLoad");
        }
    }
}
