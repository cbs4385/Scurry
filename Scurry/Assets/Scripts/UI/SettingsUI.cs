using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Scurry.Core;
using Scurry.Interfaces;

namespace Scurry.UI
{
    public class SettingsUI : MonoBehaviour
    {
        private IGameSettings gameSettings;

        private GameObject panel;
        private TextMeshProUGUI speedLabel;
        private TextMeshProUGUI colorBlindLabel;
        private TextMeshProUGUI textSizeLabel;

        private void Start()
        {
            gameSettings = ServiceLocator.Get<IGameSettings>();
            Debug.Log($"[SettingsUI] Start: gameSettings={(gameSettings != null ? "OK" : "NULL")}");
        }

        private void Update()
        {
            // Toggle settings with Escape key
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (panel != null && panel.activeSelf)
                    Close();
                else
                    Open();
            }
        }

        public void Open()
        {
            Debug.Log("[SettingsUI] Open: building settings panel");
            if (panel != null) Close();
            BuildPanel();
        }

        public void Close()
        {
            Debug.Log("[SettingsUI] Close: closing settings");
            if (panel != null) Destroy(panel);
            panel = null;
        }

        private void BuildPanel()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.15f);
            rect.anchorMax = new Vector2(0.8f, 0.85f);
            rect.sizeDelta = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.97f);

            // Title
            CreateTMP(panel.transform, "Settings", 28, FontStyles.Bold, Color.white,
                new Vector2(0, 0.88f), new Vector2(1, 0.98f));

            var settings = gameSettings;
            float y = 0.75f;
            float rowH = 0.1f;

            // Battle Speed
            CreateTMP(panel.transform, "Battle Speed:", 18, FontStyles.Normal, Color.white,
                new Vector2(0.05f, y), new Vector2(0.45f, y + rowH));
            speedLabel = CreateTMP(panel.transform, settings != null ? settings.BattleSpeedLabel : "Normal", 18, FontStyles.Bold,
                new Color(0.5f, 0.9f, 0.5f), new Vector2(0.5f, y), new Vector2(0.7f, y + rowH)).GetComponent<TextMeshProUGUI>();
            CreateBtn(panel.transform, "Cycle", () => {
                if (settings != null) { settings.CycleBattleSpeed(); speedLabel.text = settings.BattleSpeedLabel; }
            }, new Vector2(0.72f, y + 0.02f), new Vector2(0.92f, y + rowH - 0.02f));

            y -= rowH + 0.02f;

            // Color Blind Mode
            CreateTMP(panel.transform, "Color Blind Mode:", 18, FontStyles.Normal, Color.white,
                new Vector2(0.05f, y), new Vector2(0.45f, y + rowH));
            colorBlindLabel = CreateTMP(panel.transform, settings != null && settings.ColorBlindMode ? "ON" : "OFF", 18, FontStyles.Bold,
                settings != null && settings.ColorBlindMode ? Color.green : Color.gray,
                new Vector2(0.5f, y), new Vector2(0.7f, y + rowH)).GetComponent<TextMeshProUGUI>();
            CreateBtn(panel.transform, "Toggle", () => {
                if (settings != null) {
                    settings.SetColorBlindMode(!settings.ColorBlindMode);
                    colorBlindLabel.text = settings.ColorBlindMode ? "ON" : "OFF";
                    colorBlindLabel.color = settings.ColorBlindMode ? Color.green : Color.gray;
                }
            }, new Vector2(0.72f, y + 0.02f), new Vector2(0.92f, y + rowH - 0.02f));

            y -= rowH + 0.02f;

            // Text Size
            CreateTMP(panel.transform, "Text Size:", 18, FontStyles.Normal, Color.white,
                new Vector2(0.05f, y), new Vector2(0.45f, y + rowH));
            int mod = settings != null ? settings.TextSizeModifier : 0;
            textSizeLabel = CreateTMP(panel.transform, mod == 0 ? "Normal" : (mod > 0 ? $"+{mod}" : $"{mod}"), 18, FontStyles.Bold,
                Color.white, new Vector2(0.5f, y), new Vector2(0.65f, y + rowH)).GetComponent<TextMeshProUGUI>();
            CreateBtn(panel.transform, "-", () => {
                if (settings != null) {
                    settings.SetTextSizeModifier(settings.TextSizeModifier - 1);
                    UpdateTextSizeLabel(settings);
                }
            }, new Vector2(0.67f, y + 0.02f), new Vector2(0.77f, y + rowH - 0.02f));
            CreateBtn(panel.transform, "+", () => {
                if (settings != null) {
                    settings.SetTextSizeModifier(settings.TextSizeModifier + 1);
                    UpdateTextSizeLabel(settings);
                }
            }, new Vector2(0.79f, y + 0.02f), new Vector2(0.89f, y + rowH - 0.02f));

            y -= rowH + 0.04f;

            // Close button
            CreateBtn(panel.transform, "Close (Esc)", Close,
                new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.12f));
        }

        private void UpdateTextSizeLabel(IGameSettings settings)
        {
            int mod = settings.TextSizeModifier;
            textSizeLabel.text = mod == 0 ? "Normal" : (mod > 0 ? $"+{mod}" : $"{mod}");
        }

        private GameObject CreateTMP(Transform parent, string text, int size, FontStyles style,
            Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.sizeDelta = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return go;
        }

        private void CreateBtn(Transform parent, string label, System.Action onClick,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.sizeDelta = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.35f);
            go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
            CreateTMP(go.transform, label, 14, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one);
        }
    }
}
