using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scurry.Core;

namespace Scurry.UI
{
    public class BossUI : MonoBehaviour
    {
        private GameObject bossHudPanel;
        private TextMeshProUGUI bossNameText;
        private TextMeshProUGUI bossHPText;
        private TextMeshProUGUI phaseText;
        private Image hpFill;
        private int maxHP;

        private void OnEnable()
        {
            Debug.Log("[BossUI] OnEnable: subscribing to events");
            EventBus.OnBossHPChanged += OnBossHPChanged;
            EventBus.OnBossPhaseChanged += OnPhaseChanged;
            EventBus.OnBossDefeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            Debug.Log("[BossUI] OnDisable: unsubscribing from events");
            EventBus.OnBossHPChanged -= OnBossHPChanged;
            EventBus.OnBossPhaseChanged -= OnPhaseChanged;
            EventBus.OnBossDefeated -= OnBossDefeated;
        }

        private void Awake()
        {
            BuildBossHUD();
        }

        public void ShowBossHUD(string bossName, int hp, int max)
        {
            maxHP = max;
            bossNameText.text = bossName;
            bossHPText.text = $"{hp}/{max}";
            phaseText.text = "";
            UpdateHPBar(hp, max);
            bossHudPanel.SetActive(true);
            Debug.Log($"[BossUI] ShowBossHUD: boss='{bossName}', HP={hp}/{max}");
        }

        private void BuildBossHUD()
        {
            bossHudPanel = new GameObject("BossHUD", typeof(RectTransform), typeof(Image));
            bossHudPanel.transform.SetParent(transform, false);
            var panelRect = bossHudPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1);
            panelRect.anchorMax = new Vector2(0.5f, 1);
            panelRect.pivot = new Vector2(0.5f, 1);
            panelRect.anchoredPosition = new Vector2(0, -10);
            panelRect.sizeDelta = new Vector2(350, 80);
            bossHudPanel.GetComponent<Image>().color = new Color(0.15f, 0.05f, 0.05f, 0.9f);

            // Boss name
            var nameGO = new GameObject("BossName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(bossHudPanel.transform, false);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -5);
            nameRect.sizeDelta = new Vector2(-10, 25);
            bossNameText = nameGO.GetComponent<TextMeshProUGUI>();
            bossNameText.fontSize = 18;
            bossNameText.fontStyle = FontStyles.Bold;
            bossNameText.alignment = TextAlignmentOptions.Center;
            bossNameText.color = new Color(0.9f, 0.3f, 0.3f);

            // HP bar background
            var hpBarBG = new GameObject("HPBarBG", typeof(RectTransform), typeof(Image));
            hpBarBG.transform.SetParent(bossHudPanel.transform, false);
            var bgRect = hpBarBG.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 1);
            bgRect.anchorMax = new Vector2(1, 1);
            bgRect.pivot = new Vector2(0.5f, 1);
            bgRect.anchoredPosition = new Vector2(0, -32);
            bgRect.sizeDelta = new Vector2(-20, 14);
            hpBarBG.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            // HP fill
            var hpFillGO = new GameObject("HPFill", typeof(RectTransform), typeof(Image));
            hpFillGO.transform.SetParent(hpBarBG.transform, false);
            var fillRect = hpFillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            hpFill = hpFillGO.GetComponent<Image>();
            hpFill.color = new Color(0.8f, 0.2f, 0.2f);

            // HP text
            var hpTextGO = new GameObject("HPText", typeof(RectTransform), typeof(TextMeshProUGUI));
            hpTextGO.transform.SetParent(bossHudPanel.transform, false);
            var hpTextRect = hpTextGO.GetComponent<RectTransform>();
            hpTextRect.anchorMin = new Vector2(0, 1);
            hpTextRect.anchorMax = new Vector2(1, 1);
            hpTextRect.pivot = new Vector2(0.5f, 1);
            hpTextRect.anchoredPosition = new Vector2(0, -30);
            hpTextRect.sizeDelta = new Vector2(-20, 16);
            bossHPText = hpTextGO.GetComponent<TextMeshProUGUI>();
            bossHPText.fontSize = 12;
            bossHPText.alignment = TextAlignmentOptions.Center;
            bossHPText.color = Color.white;

            // Phase text
            var phaseGO = new GameObject("PhaseText", typeof(RectTransform), typeof(TextMeshProUGUI));
            phaseGO.transform.SetParent(bossHudPanel.transform, false);
            var phaseRect = phaseGO.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0, 0);
            phaseRect.anchorMax = new Vector2(1, 0);
            phaseRect.pivot = new Vector2(0.5f, 0);
            phaseRect.anchoredPosition = new Vector2(0, 5);
            phaseRect.sizeDelta = new Vector2(-10, 20);
            phaseText = phaseGO.GetComponent<TextMeshProUGUI>();
            phaseText.fontSize = 14;
            phaseText.fontStyle = FontStyles.Italic;
            phaseText.alignment = TextAlignmentOptions.Center;
            phaseText.color = new Color(1f, 0.6f, 0.2f);

            bossHudPanel.SetActive(false);
            Debug.Log("[BossUI] BuildBossHUD: complete (hidden)");
        }

        private void OnBossHPChanged(int current, int max)
        {
            maxHP = max;
            bossHPText.text = $"{current}/{max}";
            UpdateHPBar(current, max);
        }

        private void UpdateHPBar(int current, int max)
        {
            if (hpFill != null && max > 0)
            {
                float ratio = (float)current / max;
                var fillRect = hpFill.GetComponent<RectTransform>();
                fillRect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);

                hpFill.color = ratio > 0.5f ? new Color(0.8f, 0.2f, 0.2f) :
                               ratio > 0.25f ? new Color(0.8f, 0.5f, 0.2f) :
                               new Color(0.5f, 0.1f, 0.1f);
            }
        }

        private void OnPhaseChanged(string phaseName)
        {
            phaseText.text = $"Phase: {phaseName}";
            Debug.Log($"[BossUI] OnPhaseChanged: {phaseName}");
        }

        private void OnBossDefeated()
        {
            Debug.Log("[BossUI] OnBossDefeated: hiding boss HUD");
            bossHudPanel.SetActive(false);
        }
    }
}
