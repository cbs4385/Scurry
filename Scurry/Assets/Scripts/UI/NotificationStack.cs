using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Core;

namespace Scurry.UI
{
    /// <summary>
    /// Reusable stacked notification display that subscribes to EventBus.OnNotification.
    /// Each scene that needs in-scene notifications attaches this component to a Canvas GO.
    /// Notifications stack vertically, auto-fade, and are cleaned up after expiry.
    /// Built programmatically — no prefabs required.
    /// </summary>
    public class NotificationStack : MonoBehaviour
    {
        // ── Constants ──────────────────────────────────────────────────
        private const float NotificationDuration = 3.0f;
        private const float NotificationFadeDuration = 0.5f;
        private const float NotificationHeight = 40f;
        private const float NotificationSpacing = 4f;
        private const float ContainerWidth = 350f;
        private const float ContainerHeight = 400f;
        private const float ContainerMarginRight = 10f;

        // ── State ──────────────────────────────────────────────────────
        private RectTransform container;
        private readonly List<NotificationEntry> activeNotifications = new List<NotificationEntry>();

        private class NotificationEntry
        {
            public GameObject go;
            public CanvasGroup canvasGroup;
            public float spawnTime;
        }

        // ── Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            Debug.Log("[NotificationStack] Awake: building notification container");
            BuildContainer();
        }

        private void OnEnable()
        {
            Debug.Log("[NotificationStack] OnEnable: subscribing to EventBus.OnNotification");
            EventBus.OnNotification += ShowNotification;
        }

        private void OnDisable()
        {
            Debug.Log("[NotificationStack] OnDisable: unsubscribing from EventBus.OnNotification");
            EventBus.OnNotification -= ShowNotification;
        }

        private void Update()
        {
            UpdateNotifications();
        }

        private void OnDestroy()
        {
            Debug.Log($"[NotificationStack] OnDestroy: cleaning up {activeNotifications.Count} active notifications");
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                if (activeNotifications[i].go != null)
                    Destroy(activeNotifications[i].go);
            }
            activeNotifications.Clear();
        }

        // ── Build ──────────────────────────────────────────────────────

        private void BuildContainer()
        {
            var containerGO = new GameObject("NotificationContainer", typeof(RectTransform));
            containerGO.transform.SetParent(transform, false);
            container = containerGO.GetComponent<RectTransform>();
            container.anchorMin = new Vector2(1, 0.5f);
            container.anchorMax = new Vector2(1, 0.5f);
            container.pivot = new Vector2(1, 0.5f);
            container.anchoredPosition = new Vector2(-ContainerMarginRight, 0);
            container.sizeDelta = new Vector2(ContainerWidth, ContainerHeight);

            // Ensure container renders on top of all siblings (other UI panels on same canvas)
            container.SetAsLastSibling();

            Debug.Log("[NotificationStack] BuildContainer: container created");
        }

        // ── Public API ─────────────────────────────────────────────────

        /// <summary>
        /// Shows a notification with the given message and accent colour.
        /// Called automatically via EventBus.OnNotification.
        /// </summary>
        public void ShowNotification(string message, Color color)
        {
            Debug.Log($"[NotificationStack] ShowNotification: message='{message}', color={color}, activeCount={activeNotifications.Count}");

            if (container == null)
            {
                Debug.LogWarning("[NotificationStack] ShowNotification: container is null, skipping");
                return;
            }

            // Panel
            var panelGO = new GameObject("Notification", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panelGO.transform.SetParent(container, false);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.sizeDelta = new Vector2(0, NotificationHeight);
            panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
            panelGO.GetComponent<Image>().raycastTarget = false;

            // Colour accent bar
            var accentGO = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentGO.transform.SetParent(panelGO.transform, false);
            var accentRect = accentGO.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 0);
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(4, 0);
            accentGO.GetComponent<Image>().color = color;
            accentGO.GetComponent<Image>().raycastTarget = false;

            // Message text
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(panelGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 2);
            textRect.offsetMax = new Vector2(-8, -2);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = color;
            tmp.richText = true;
            tmp.raycastTarget = false;

            var entry = new NotificationEntry
            {
                go = panelGO,
                canvasGroup = panelGO.GetComponent<CanvasGroup>(),
                spawnTime = Time.time
            };
            activeNotifications.Add(entry);
            LayoutNotifications();
            // Ensure notifications render on top of all other UI on this canvas
            container.SetAsLastSibling();

            Debug.Log($"[NotificationStack] ShowNotification: notification spawned (activeCount={activeNotifications.Count})");
        }

        // ── Internal ───────────────────────────────────────────────────

        private void UpdateNotifications()
        {
            float now = Time.time;
            bool dirty = false;

            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                var entry = activeNotifications[i];
                float age = now - entry.spawnTime;

                if (age > NotificationDuration + NotificationFadeDuration)
                {
                    Debug.Log($"[NotificationStack] UpdateNotifications: removing expired notification (age={age:F1}s)");
                    Destroy(entry.go);
                    activeNotifications.RemoveAt(i);
                    dirty = true;
                }
                else if (age > NotificationDuration)
                {
                    float fadeProgress = (age - NotificationDuration) / NotificationFadeDuration;
                    entry.canvasGroup.alpha = 1f - fadeProgress;
                }
            }

            if (dirty)
            {
                LayoutNotifications();
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

            Debug.Log($"[NotificationStack] LayoutNotifications: {activeNotifications.Count} notifications stacked");
        }
    }
}
