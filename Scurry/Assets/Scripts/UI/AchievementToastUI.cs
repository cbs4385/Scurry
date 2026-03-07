using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Scurry.Core;

namespace Scurry.UI
{
    public class AchievementToastUI : MonoBehaviour
    {
        private GameObject toastPanel;
        private TextMeshProUGUI toastText;
        private CanvasGroup canvasGroup;

        private void OnEnable()
        {
            EventBus.OnAchievementUnlocked += ShowToast;
            Debug.Log("[AchievementToastUI] OnEnable: subscribed to OnAchievementUnlocked");
        }

        private void OnDisable()
        {
            EventBus.OnAchievementUnlocked -= ShowToast;
        }

        private void ShowToast(string achievementKey)
        {
            Debug.Log($"[AchievementToastUI] ShowToast: achievementKey='{achievementKey}'");

            if (toastPanel == null)
                BuildToastPanel();

            string displayName = FormatAchievementName(achievementKey);
            toastText.text = $"Achievement Unlocked!\n<size=16>{displayName}</size>";
            toastPanel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(AnimateToast());
        }

        private IEnumerator AnimateToast()
        {
            // Fade in
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null) canvasGroup.alpha = elapsed / 0.3f;
                yield return null;
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(3f);

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null) canvasGroup.alpha = 1f - elapsed / 0.5f;
                yield return null;
            }

            toastPanel.SetActive(false);
        }

        private void BuildToastPanel()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            toastPanel = new GameObject("AchievementToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            toastPanel.transform.SetParent(canvas.transform, false);
            var rect = toastPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(350, 70);
            toastPanel.GetComponent<Image>().color = new Color(0.15f, 0.1f, 0.0f, 0.95f);
            canvasGroup = toastPanel.GetComponent<CanvasGroup>();

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(toastPanel.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
            toastText = textGO.GetComponent<TextMeshProUGUI>();
            toastText.fontSize = 14;
            toastText.fontStyle = FontStyles.Bold;
            toastText.alignment = TextAlignmentOptions.Center;
            toastText.color = new Color(1f, 0.85f, 0.3f);

            toastPanel.SetActive(false);
            Debug.Log("[AchievementToastUI] BuildToastPanel: complete");
        }

        private string FormatAchievementName(string key)
        {
            // Convert PascalCase to spaced words
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && char.IsUpper(key[i]) && !char.IsUpper(key[i - 1]))
                    sb.Append(' ');
                sb.Append(key[i]);
            }
            return sb.ToString();
        }
    }
}
